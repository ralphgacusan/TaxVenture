using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

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
    public void BringToFront()
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
        popElapsed = 0f;
    }

    // =========================================================
    // RENDER
    // =========================================================

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

        RenderDocument(main);
        SetComposited(true);
        AnimatePop();
    }

    /// <summary>True when an open paper window under this document overlaps it.</summary>
    private bool CoversAPaperBelow()
    {
        for (int i = 0; i < FloatingWindow.Instances.Count; i++)
        {
            FloatingWindow w = FloatingWindow.Instances[i];
            if (w == null || !w.IsOpen() || w.SortingOrder >= sortingOrder) continue;
            if (ScreenRect(w.WindowRect).Overlaps(footprint)) return true;
        }
        return false;
    }

    private void SetComposited(bool on)
    {
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
        docCamera.enabled = false;
        docCamera.cullingMask = 1 << PrivateLayer;
        docCamera.clearFlags = CameraClearFlags.SolidColor;
        docCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        docCamera.targetTexture = texture;
        var data = docCamera.GetUniversalAdditionalCameraData();
        data.renderType = CameraRenderType.Base;
        data.renderPostProcessing = false;
        data.antialiasing = AntialiasingMode.None;

        SwapLayers();
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
            RestoreLayers();
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
