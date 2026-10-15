using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>Depth-tested, screen-space silhouettes. Never changes the object's materials.</summary>
public sealed class InteractionOutlineFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader outlineShader;
    [SerializeField] private Color outlineColor = new Color(0.68f, 0.70f, 0.72f, 0.58f);
    [SerializeField, Range(1f, 8f)] private float widthPixels = 5f;

    private Material material;
    private OutlinePass pass;

    public override void Create()
    {
        CoreUtils.Destroy(material);
        if (outlineShader != null) material = CoreUtils.CreateEngineMaterial(outlineShader);
        pass = new OutlinePass { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing };
        pass.ConfigureInput(ScriptableRenderPassInput.Depth);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null || HighlightEffect.ActiveEffects.Count == 0 ||
            renderingData.cameraData.cameraType != CameraType.Game ||
            renderingData.cameraData.renderType == CameraRenderType.Overlay) return;
        // Conversation framing suppresses focus feedback, but keeps object materials intact.
        if (DialogueUI.IsAnyDialogueActive) return;
        material.SetColor("_OutlineColor", outlineColor);
        material.SetFloat("_OutlineWidth", widthPixels);
        pass.Material = material;
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(material);

    private sealed class OutlinePass : ScriptableRenderPass
    {
        public Material Material;
        private static readonly int DepthId = Shader.PropertyToID("_InteractionOutlineDepth");
        private static readonly int StrengthId = Shader.PropertyToID("_InteractionOutlineStrength");

        private sealed class MaskData
        {
            public Material material;
            public TextureHandle depth;
            public int cameraMask;
        }

        private sealed class CompositeData
        {
            public Material material;
            public TextureHandle mask;
        }

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            var camera = frameData.Get<UniversalCameraData>();
            if (!resources.cameraDepthTexture.IsValid()) return;
            var desc = graph.GetTextureDesc(resources.activeColorTexture);
            desc.name = "Interaction silhouette mask";
            desc.colorFormat = GraphicsFormat.R8_UNorm;
            desc.depthBufferBits = DepthBits.None;
            desc.msaaSamples = MSAASamples.None;
            desc.clearBuffer = true;
            desc.clearColor = Color.clear;
            TextureHandle mask = graph.CreateTexture(desc);

            using (var builder = graph.AddRasterRenderPass<MaskData>("Interaction silhouettes", out var data))
            {
                data.material = Material;
                data.depth = resources.cameraDepthTexture;
                data.cameraMask = camera.camera.cullingMask;
                builder.UseTexture(data.depth, AccessFlags.Read);
                builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (MaskData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(DepthId, data.depth);
                    var effects = HighlightEffect.ActiveEffects;
                    for (int i = 0; i < effects.Count; i++)
                    {
                        HighlightEffect effect = effects[i];
                        if (effect == null || !effect.isActiveAndEnabled) continue;
                        context.cmd.SetGlobalFloat(StrengthId, effect.Strength);
                        foreach (Renderer renderer in effect.TargetRenderers)
                        {
                            if (renderer == null || !renderer.enabled || renderer.forceRenderingOff ||
                                !renderer.gameObject.activeInHierarchy ||
                                (data.cameraMask & (1 << renderer.gameObject.layer)) == 0) continue;
                            Mesh mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh :
                                renderer.GetComponent<MeshFilter>()?.sharedMesh;
                            if (mesh == null) continue;
                            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                                context.cmd.DrawRenderer(renderer, data.material, submesh, 0);
                        }
                    }
                });
            }

            // Blend only the ring onto camera color; no camera-color copy or material replacement.
            using (var builder = graph.AddRasterRenderPass<CompositeData>("Soft interaction outline", out var data))
            {
                data.material = Material;
                data.mask = mask;
                builder.UseTexture(mask, AccessFlags.Read);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.SetRenderFunc(static (CompositeData data, RasterGraphContext context) =>
                    Blitter.BlitTexture(context.cmd, data.mask, new Vector4(1, 1, 0, 0), data.material, 1));
            }
        }
    }
}
