using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// A client's 3D ID card shown inside a HUD window (user revision
/// 2026-10-27: the binder shows the clients' IDs, not flat profile cards).
/// The real card model (IdCardModel: bevelled, laminated, printed front and
/// profile back) sits in a private studio far below the world on the spare
/// layer 15 and a small perspective camera films it, slightly turned so its
/// thickness reads, into this RawImage. The window keeps every existing
/// behaviour (open/close flight, drag, front order, hold-to-gather, drop on the
/// icon); a tap turns the card over with CardFlip's animation, rendered live.
///
/// Renders only when something changed (opening, a flip in progress, a lost
/// render texture), never every frame at rest. Presentation only.
/// </summary>
public sealed class IdCardPortrait : MonoBehaviour
{
    /// <summary>Spare layer 15 (unnamed, unused by scenes): only the portrait cameras draw it.</summary>
    public const int Layer = 15;
    private const float FieldOfView = 16f;
    private static int nextStudio;

    private RawImage image;
    private RenderTexture texture;
    private Transform studio;
    private Camera filmCamera;
    private CardFlip flip;
    private bool dirty = true;
    private Vector2Int pixels;

    public CardFlip Flip => flip;

    /// <summary>Fills rect with the record's card, pixelScale texels per UI unit.</summary>
    public static IdCardPortrait Create(RectTransform rect, ClientRecord record, float pixelScale = 2f)
    {
        var portrait = rect.gameObject.AddComponent<IdCardPortrait>();
        portrait.Build(rect, record, pixelScale);
        return portrait;
    }

    private void Build(RectTransform rect, ClientRecord record, float pixelScale)
    {
        image = gameObject.GetComponent<RawImage>() ?? gameObject.AddComponent<RawImage>();
        image.raycastTarget = false;
        image.color = Color.white;
        Vector2 size = rect.rect.size;
        if (size.x < 1f || size.y < 1f) size = rect.sizeDelta;
        pixels = new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(size.x * pixelScale), 64, 2048), Mathf.Clamp(Mathf.RoundToInt(size.y * pixelScale), 64, 2048));

        // Each portrait has its own spot in the studio, far from the world and from each other.
        int spot = nextStudio++ % 64;
        studio = new GameObject("ID Portrait Studio").transform;
        studio.position = new Vector3(spot * 5f, -20000f - (spot / 8) * 5f, 0f);
        // The card face-on, front toward the camera (-Z), its top up: the same reading pose as an ID
        // taken out of Marie's or Arthur's folder (2026-10-27), with its soft shadow behind it.
        const float cardWidth = 0.86f;
        var card = new GameObject("Card");
        card.layer = Layer;
        card.transform.SetParent(studio, false);
        card.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        var cardSize = new Vector2(cardWidth, cardWidth / 1.6f);
        flip = IdCardModel.Build(card.transform, record, cardSize, Layer, out _, out _);
        IdCardModel.AddDropShadow(card.transform, record, cardSize, Layer);
        foreach (Transform node in card.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = Layer;
        flip.Changed += MarkDirty;

        var cameraObject = new GameObject("Portrait Camera");
        cameraObject.transform.SetParent(studio, false);
        float aspect = (float)pixels.x / pixels.y;
        // Frame the card (plus room for its lift and pop while turning).
        float halfHeight = Mathf.Max(cardWidth / 1.6f, cardWidth / aspect) * 0.5f * 1.1f;
        float distance = halfHeight / Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -distance);
        filmCamera = cameraObject.AddComponent<Camera>();
        filmCamera.enabled = false;
        filmCamera.fieldOfView = FieldOfView;
        filmCamera.nearClipPlane = 0.05f;
        filmCamera.farClipPlane = distance + 2f;
        filmCamera.cullingMask = 1 << Layer;
        filmCamera.clearFlags = CameraClearFlags.SolidColor;
        filmCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        filmCamera.allowHDR = false;
        var data = filmCamera.GetUniversalAdditionalCameraData();
        data.renderType = CameraRenderType.Base;
        data.renderPostProcessing = false;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.renderShadows = false;
    }

    public void MarkDirty() => dirty = true;

    private void OnEnable() => dirty = true;

    private void OnApplicationFocus(bool focused)
    {
        if (focused) dirty = true;
    }

    private void LateUpdate()
    {
        if (filmCamera == null) return;
        // The studio is far below the world; keep the game camera from ever drawing it.
        Camera main = Camera.main;
        if (main != null) main.cullingMask &= ~(1 << Layer);
        if (texture == null || !texture.IsCreated())
        {
            if (texture != null) Destroy(texture);
            texture = new RenderTexture(pixels.x, pixels.y, 24, RenderTextureFormat.ARGB32) { name = "ID Portrait", antiAliasing = 4 };
            texture.Create();
            image.texture = texture;
            dirty = true;
        }
        if (!dirty && (flip == null || !flip.IsTurning)) return;
        dirty = false;
        filmCamera.targetTexture = texture;
        Canvas.ForceUpdateCanvases();
        var request = new RenderPipeline.StandardRequest { destination = texture };
        if (RenderPipeline.SupportsRenderRequest(filmCamera, request)) RenderPipeline.SubmitRenderRequest(filmCamera, request);
        else filmCamera.Render();
    }

    private void OnDestroy()
    {
        if (flip != null) flip.Changed -= MarkDirty;
        if (studio != null) Destroy(studio.gameObject);
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
    }
}
