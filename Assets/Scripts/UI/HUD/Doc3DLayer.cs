using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// PURPOSE:
/// Lets a 3D document (the open Case Folder or Tax Code Book on the desk, or
/// the Case Folder floating in front of the camera) take part in the same
/// front-to-back order as the screen paper windows (Notes, profile cards).
///
/// WHY: screen UI always draws over the 3D world, so a 3D document can
/// never be drawn above a paper window directly. When this document is in
/// front of a paper window it overlaps, a hidden camera that copies the main
/// camera renders ONLY this document into a transparent texture, and that
/// texture is shown as a full-screen layer at this document's place in the
/// window order. The real document is still drawn by the main camera, so the
/// copy lines up exactly on top of it.
///
/// ISOLATION: the document is moved onto a private layer only for the
/// duration of that one render call and restored immediately, so physics,
/// clicks, outlines and desk placement never see the change.
///
/// COST: nothing unless the document is visible AND in front of an open
/// paper window it overlaps.
/// </summary>
[DefaultExecutionOrder(500)]
public sealed class Doc3DLayer : MonoBehaviour
{
    public static readonly List<Doc3DLayer> All = new List<Doc3DLayer>();

    private const int PrivateLayer = 31;
    private const float PopDuration = 0.2f;

    private Transform source;
    private Camera docCamera;
    private RenderTexture texture;
    private Canvas overlay;
    private RawImage image;
    private int sortingOrder;
    private Rect footprint;
    private bool footprintValid;
    private bool composited;
    private float popElapsed = -1f;

    private readonly List<Transform> nodes = new List<Transform>();
    private readonly List<int> savedLayers = new List<int>();
    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<RectTransform> canvases = new List<RectTransform>();
    private static readonly Vector3[] Corners = new Vector3[4];

    public Transform Source => source;
    public int SortingOrder => sortingOrder;
    public bool IsVisible => source != null && source.gameObject.activeInHierarchy;
    public bool IsComposited => composited;
    /// <summary>Opt-in for held tools that must also cover composited desk documents.</summary>
    public bool CompositeOverDocuments { get; set; }
    /// <summary>Held tools remain visible even where world geometry would occlude them.</summary>
    public bool AlwaysComposite { get; set; }
    /// <summary>Optional stable layer for runtime tools; ordinary documents use temporary isolation.</summary>
    public int PersistentRenderLayer { get; set; } = -1;
    /// <summary>
    /// Opt-in: re-render only when the document moved relative to the camera
    /// (or the screen size changed). For still documents pinned to the
    /// screen (HUD papers) the last capture is reused, so only a paper being
    /// dragged costs a render.
    /// </summary>
    public bool RenderOnlyWhenChanged { get; set; }
    /// <summary>Opt-in: the footprint is this screen-overlay rect (the paper's window) instead of a scan of every renderer.</summary>
    public RectTransform FootprintRect { get; set; }

    private bool hasRenderedPose;
    private Matrix4x4 renderedPose;
    private Vector2Int renderedSize;
    private float renderedFov;

    /// <summary>Screen rectangle the document covers (valid while visible).</summary>
    public bool TryGetFootprint(out Rect rect)
    {
        rect = footprint;
        return footprintValid && IsVisible;
    }

    public static Doc3DLayer Create(Transform hudCanvas, Transform document, string name)
    {
        if (document == null) return null;

        RectTransform root = HudTheme.CreateRect("3D Document Layer " + name, hudCanvas);
        HudTheme.Stretch(root);
        var canvas = root.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;

        RectTransform imageRect = HudTheme.CreateRect("Document", root);
        HudTheme.Stretch(imageRect);
        var raw = imageRect.gameObject.AddComponent<RawImage>();
        raw.raycastTarget = false;
        raw.enabled = false;

        var cameraObject = new GameObject("3D Document Camera " + name);
        cameraObject.transform.SetParent(root, false);
        var camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;

        var layer = root.gameObject.AddComponent<Doc3DLayer>();
        layer.source = document;
        layer.overlay = canvas;
        layer.image = raw;
        layer.docCamera = camera;
        layer.AssignSortingOrder(1100);
        All.Add(layer);
        return layer;
    }

    private void OnDestroy()
    {
        All.Remove(this);
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
    }

    // =========================================================
    // ORDER
    // =========================================================

    public void AssignSortingOrder(int order)
    {
        sortingOrder = order;
        if (overlay != null) overlay.sortingOrder = order;
    }

    /// <summary>Puts this document on top of everything, with a small pop.</summary>
    public void BringToFront(bool animate = true)
    {
        bool alreadyTop = true;
        for (int i = 0; i < FloatingWindow.Instances.Count; i++)
        {
            FloatingWindow w = FloatingWindow.Instances[i];
            if (w != null && w.IsOpen() && w.SortingOrder > sortingOrder) alreadyTop = false;
        }
        for (int i = 0; i < All.Count; i++)
            if (All[i] != this && All[i].IsVisible && All[i].sortingOrder > sortingOrder) alreadyTop = false;
        if (alreadyTop) return;

        AssignSortingOrder(FloatingWindow.TakeTopSortingOrder());
        popElapsed = animate ? 0f : -1f;
    }

    // =========================================================
    // RENDER
    // =========================================================

    // =========================================================
    // THE TOUCHED DOCUMENT COMES TO THE FRONT (2026-10-26)
    // =========================================================

    /// <summary>
    /// Level 1 (2026-10-26): away from the desk an open Case Folder or Tax Code Book is read-only.
    /// It reads and turns pages as usual, but none of its values can be held, dragged or placed.
    /// </summary>
    public static bool BlocksValueTransfer(Transform t)
    {
        CameraController cc = CameraController.Instance;
        if (HudHotbar.Instance == null || cc == null || cc.CurrentMode == CameraController.CameraMode.Workstation) return false;
        return For(t) != null;
    }

    /// <summary>The visible document layer whose document contains t (null when t is not part of one).</summary>
    public static Doc3DLayer For(Transform t)
    {
        if (t == null) return null;
        for (int i = 0; i < All.Count; i++)
        {
            Doc3DLayer layer = All[i];
            if (layer != null && layer.source != null && layer.IsVisible && t.IsChildOf(layer.source)) return layer;
        }
        return null;
    }

    /// <summary>
    /// True when the document itself is drawn at this screen point: the ray from the camera passes
    /// through one of its meshes (each tested in its own oriented box) or one of its page canvases.
    /// known is false when the document has nothing to test (e.g. a tool drawn by HUD UI only).
    /// 2026-10-26: touch-to-front used raycast hits, and a world canvas's hits sort ahead of a nearer
    /// book, so touching the Tax Code Book kept bringing the Case Folder forward.
    /// </summary>
    public bool ShowsAt(Vector2 screen, out bool known)
    {
        known = false;
        Camera main = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        if (!IsVisible || main == null) return false;
        Ray ray = main.ScreenPointToRay(screen);

        renderers.Clear();
        source.GetComponentsInChildren(false, renderers);
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled || r is ParticleSystemRenderer) continue;
            known = true;
            Matrix4x4 toLocal = r.transform.worldToLocalMatrix;
            var local = new Ray(toLocal.MultiplyPoint3x4(ray.origin), toLocal.MultiplyVector(ray.direction));
            if (r.localBounds.IntersectRay(local)) return true;
        }

        foreach (Canvas c in source.GetComponentsInChildren<Canvas>(false))
        {
            var rect = (RectTransform)c.transform;
            if (c.renderMode != RenderMode.WorldSpace) continue;
            known = true;
            var plane = new Plane(rect.forward, rect.position);
            if (!plane.Raycast(ray, out float distance)) continue;
            Vector3 onCanvas = rect.InverseTransformPoint(ray.GetPoint(distance));
            if (rect.rect.Contains(new Vector2(onCanvas.x, onCanvas.y))) return true;
        }
        return false;
    }

    private void LateUpdate()
    {
        Camera main = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        if (!IsVisible || main == null)
        {
            footprintValid = false;
            SetComposited(false);
            return;
        }

        footprintValid = ComputeFootprint(main, out footprint);
        if (!footprintValid || !CoversAPaperBelow())
        {
            SetComposited(false);
            return;
        }

        if (!RenderOnlyWhenChanged || NeedsRender(main)) RenderDocument(main);
        SetComposited(true);
        AnimatePop();
    }

    /// <summary>True when an open paper window under this document overlaps it.</summary>
    private bool CoversAPaperBelow()
    {
        if (AlwaysComposite) return true;
        for (int i = 0; i < FloatingWindow.Instances.Count; i++)
        {
            FloatingWindow w = FloatingWindow.Instances[i];
            if (w == null || !w.IsOpen() || w.SortingOrder >= sortingOrder) continue;
            if (ScreenRect(w.WindowRect).Overlaps(footprint)) return true;
        }
        if (CompositeOverDocuments)
            for (int i = 0; i < All.Count; i++)
            {
                Doc3DLayer other = All[i];
                if (other != this && other != null && other.IsComposited && other.sortingOrder < sortingOrder &&
                    other.TryGetFootprint(out Rect rect) && rect.Overlaps(footprint)) return true;
            }
        return false;
    }

    private bool NeedsRender(Camera main)
    {
        Matrix4x4 pose = main.worldToCameraMatrix * source.localToWorldMatrix;
        var size = new Vector2Int(main.pixelWidth, main.pixelHeight);
        bool same = hasRenderedPose && texture != null && size == renderedSize && Mathf.Approximately(main.fieldOfView, renderedFov);
        for (int i = 0; same && i < 16; i++)
            if (Mathf.Abs(pose[i] - renderedPose[i]) > 1e-5f) same = false;
        if (same) return false;
        hasRenderedPose = true;
        renderedPose = pose;
        renderedSize = size;
        renderedFov = main.fieldOfView;
        return true;
    }

    /// <summary>Forces the next frame to capture again (content changed without moving).</summary>
    public void MarkDirty() => hasRenderedPose = false;

    private void SetComposited(bool on)
    {
        if (!on) hasRenderedPose = false;
        composited = on;
        if (image.enabled != on) image.enabled = on;
    }

    private void RenderDocument(Camera main)
    {
        int width = Mathf.Max(1, main.pixelWidth);
        int height = Mathf.Max(1, main.pixelHeight);
        if (texture == null || texture.width != width || texture.height != height)
        {
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }
            texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = "3D Document " + name };
            texture.Create();
            image.texture = texture;
        }

        docCamera.CopyFrom(main);
        // Render the copied view into the whole texture, then place that
        // texture in the original viewport (including letterboxed views).
        docCamera.rect = new Rect(0f, 0f, 1f, 1f);
        image.rectTransform.anchorMin = main.rect.min;
        image.rectTransform.anchorMax = main.rect.max;
        image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
        docCamera.enabled = false;
        docCamera.cullingMask = 1 << (PersistentRenderLayer >= 0 ? PersistentRenderLayer : PrivateLayer);
        docCamera.clearFlags = CameraClearFlags.SolidColor;
        docCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        docCamera.targetTexture = texture;
        var data = docCamera.GetUniversalAdditionalCameraData();
        data.renderType = CameraRenderType.Base;
        data.renderPostProcessing = false;
        data.antialiasing = AntialiasingMode.None;

        bool temporaryLayers = PersistentRenderLayer < 0;
        if (temporaryLayers) SwapLayers();
        try
        {
            var request = new RenderPipeline.StandardRequest { destination = texture };
            if (RenderPipeline.SupportsRenderRequest(docCamera, request))
                RenderPipeline.SubmitRenderRequest(docCamera, request);
            else
                docCamera.Render();
        }
        finally
        {
            if (temporaryLayers) RestoreLayers();
        }
    }

    private void SwapLayers()
    {
        nodes.Clear();
        savedLayers.Clear();
        source.GetComponentsInChildren(true, nodes);
        for (int i = 0; i < nodes.Count; i++)
        {
            GameObject go = nodes[i].gameObject;
            savedLayers.Add(go.layer);
            go.layer = PrivateLayer;
        }
    }

    private void RestoreLayers()
    {
        for (int i = 0; i < nodes.Count && i < savedLayers.Count; i++)
            if (nodes[i] != null) nodes[i].gameObject.layer = savedLayers[i];
    }

    // =========================================================
    // FOOTPRINT AND POP
    // =========================================================

    private bool ComputeFootprint(Camera main, out Rect rect)
    {
        rect = default;
        if (FootprintRect != null)
        {
            if (!FootprintRect.gameObject.activeInHierarchy) return false;
            rect = ScreenRect(FootprintRect);
            return rect.width > 1f && rect.height > 1f;
        }
        bool any = false;
        Vector2 min = Vector2.zero, max = Vector2.zero;

        renderers.Clear();
        source.GetComponentsInChildren(false, renderers);
        for (int i = 0; i < renderers.Count; i++)
        {
            Bounds b = renderers[i].bounds;
            for (int c = 0; c < 8; c++)
            {
                Vector3 p = new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y,
                    (c & 4) == 0 ? b.min.z : b.max.z);
                Include(main, p, ref any, ref min, ref max);
            }
        }

        canvases.Clear();
        foreach (Canvas c in source.GetComponentsInChildren<Canvas>(false))
            canvases.Add((RectTransform)c.transform);
        for (int i = 0; i < canvases.Count; i++)
        {
            canvases[i].GetWorldCorners(Corners);
            for (int c = 0; c < 4; c++) Include(main, Corners[c], ref any, ref min, ref max);
        }

        if (!any) return false;
        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return rect.width > 1f && rect.height > 1f;
    }

    private static void Include(Camera main, Vector3 world, ref bool any, ref Vector2 min, ref Vector2 max)
    {
        Vector3 s = main.WorldToScreenPoint(world);
        if (s.z <= 0f) return;
        Vector2 p = new Vector2(s.x, s.y);
        if (!any)
        {
            min = max = p;
            any = true;
        }
        else
        {
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
    }

    public static Rect ScreenRect(RectTransform rect)
    {
        if (rect == null) return default;
        rect.GetWorldCorners(Corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(null, Corners[0]);
        Vector2 max = min;
        for (int i = 1; i < 4; i++)
        {
            Vector2 p = RectTransformUtility.WorldToScreenPoint(null, Corners[i]);
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void AnimatePop()
    {
        RectTransform rt = image.rectTransform;
        if (popElapsed < 0f)
        {
            if (rt.localScale != Vector3.one) rt.localScale = Vector3.one;
            return;
        }

        // Scale about the document's centre on screen.
        var root = (RectTransform)overlay.transform;
        Vector2 size = root.rect.size;
        Vector2 screen = new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        Vector2 pivot = new Vector2(footprint.center.x / screen.x, footprint.center.y / screen.y);
        if (rt.pivot != pivot)
        {
            rt.pivot = pivot;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        popElapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(popElapsed / PopDuration);
        float scale = 1f + 0.04f * Mathf.Sin(t * Mathf.PI);
        rt.localScale = new Vector3(scale, scale, 1f);
        if (t >= 1f)
        {
            popElapsed = -1f;
            rt.localScale = Vector3.one;
        }
        if (size.x <= 0f) rt.localScale = Vector3.one;
    }
}
