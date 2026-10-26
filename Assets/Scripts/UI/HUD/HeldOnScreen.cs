using UnityEngine;

/// <summary>
/// PURPOSE:
/// Keeps a held readable (a paper window, the floating Case Folder, an open
/// Tax Code Book) inside the screen while it is dragged. On grab it measures
/// the object's on-screen box around the finger; while dragging, Clamp()
/// turns the finger position into the one the drag code moves the object
/// with.
///
/// FEEL (2026-10-26): the object stays exactly under the finger. It only
/// stops where going further would take part of it off screen, and it is
/// under the finger again as soon as the finger is back inside that range.
/// An object wider or taller than the screen keeps its centre on screen.
///
/// Also places just-opened documents: CentreUnder puts a point (the printed
/// page's centre) under the finger, FitOnScreen moves the whole document in.
///
/// Drop tests (closing into a HUD icon, storing the book in the hotbar, the
/// desk) keep using the real finger position, so they work exactly as before.
/// </summary>
public struct HeldOnScreen
{
    /// <summary>Gap kept between the held object and the screen edge (pixels).</summary>
    public const float Margin = 12f;

    // Extents of the held object from the grab point, in screen pixels.
    private float left, right, below, above;

    public bool IsSet { get; private set; }

    private static readonly Vector3[] Corners = new Vector3[4];

    /// <summary>A UI window grabbed at screen point grab (camera: null for an overlay canvas).</summary>
    public static HeldOnScreen ForRect(RectTransform rect, Vector2 grab, Camera camera)
    {
        if (rect == null) return default;
        rect.GetWorldCorners(Corners);
        Rect box = new Rect(RectTransformUtility.WorldToScreenPoint(camera, Corners[0]), Vector2.zero);
        for (int i = 1; i < 4; i++) box = Grow(box, RectTransformUtility.WorldToScreenPoint(camera, Corners[i]));
        return FromScreenBox(box, grab);
    }

    /// <summary>
    /// A 3D model grabbed at screen point grab, measured by its visible meshes' own (oriented)
    /// boxes, which hug a tilted folder or book far more closely than a world-aligned box seen in
    /// perspective.
    /// </summary>
    public static HeldOnScreen ForModel(Transform model, Camera camera, Vector2 grab)
    {
        return TryScreenBox(model, camera, out Rect box) ? FromScreenBox(box, grab) : default;
    }

    /// <summary>A screen box held at grab.</summary>
    public static HeldOnScreen FromScreenBox(Rect box, Vector2 grab)
    {
        return new HeldOnScreen
        {
            left = Mathf.Max(0f, grab.x - box.xMin),
            right = Mathf.Max(0f, box.xMax - grab.x),
            below = Mathf.Max(0f, grab.y - box.yMin),
            above = Mathf.Max(0f, box.yMax - grab.y),
            IsSet = true
        };
    }

    /// <summary>The finger position to move the object with (unchanged when not set).</summary>
    public Vector2 Clamp(Vector2 pointer)
    {
        if (!IsSet) return pointer;
        return new Vector2(Axis(pointer.x, left, right, Screen.width), Axis(pointer.y, below, above, Screen.height));
    }

    private static float Axis(float pointer, float before, float after, float size)
    {
        float min = Margin + before, max = size - Margin - after;
        if (min <= max) return Mathf.Clamp(pointer, min, max);
        // Bigger than the screen: its centre stays on screen.
        float centre = (after - before) * 0.5f;
        return Mathf.Clamp(pointer, -centre, size - centre);
    }

    /// <summary>The on-screen box of a model's visible meshes (UI canvases and text on it are ignored).</summary>
    public static bool TryScreenBox(Transform model, Camera camera, out Rect box)
    {
        box = default;
        if (model == null || camera == null) return false;
        bool any = false;
        foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>())
        {
            if (!renderer.enabled || renderer.GetComponentInParent<Canvas>() != null || renderer.GetComponent<TMPro.TMP_Text>() != null) continue;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            bool oriented = filter != null && filter.sharedMesh != null;
            Bounds local = oriented ? filter.sharedMesh.bounds : default;
            Bounds world = renderer.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 sign = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
                Vector3 corner = oriented
                    ? renderer.transform.TransformPoint(local.center + Vector3.Scale(local.extents, sign))
                    : world.center + Vector3.Scale(world.extents, sign);
                Vector3 screen = camera.WorldToScreenPoint(corner);
                if (screen.z <= 0f) continue;
                if (!any) { box = new Rect(screen, Vector2.zero); any = true; }
                else box = Grow(box, screen);
            }
        }
        return any;
    }

    /// <summary>
    /// Moves a just-placed 3D document sideways / up-down (keeping its depth) so all of it shows,
    /// Margin pixels inside the screen; one bigger than the screen is centred. Returns the new position.
    /// </summary>
    public static Vector3 FitOnScreen(Transform model, Camera camera)
    {
        if (model == null || camera == null) return model != null ? model.position : Vector3.zero;
        // Twice: perspective changes the box a little once it has moved.
        for (int pass = 0; pass < 2; pass++)
        {
            if (!TryScreenBox(model, camera, out Rect box)) break;
            Rect view = camera.pixelRect;
            Vector2 shift = new Vector2(FitAxis(box.xMin, box.xMax, view.xMin, view.xMax), FitAxis(box.yMin, box.yMax, view.yMin, view.yMax));
            if (shift.sqrMagnitude < 0.25f) break;
            float depth = Vector3.Dot(model.position - camera.transform.position, camera.transform.forward);
            if (depth <= camera.nearClipPlane) break;
            Vector2 from = box.center;
            model.position += camera.ScreenToWorldPoint(new Vector3(from.x + shift.x, from.y + shift.y, depth)) -
                              camera.ScreenToWorldPoint(new Vector3(from.x, from.y, depth));
        }
        return model.position;
    }

    /// <summary>The camera the player is looking through now (the gameplay camera), else the given one, else Camera.main.</summary>
    public static Camera ActiveCamera(Camera fallback = null)
    {
        Camera active = CameraController.Instance != null ? CameraController.Instance.MainCamera : null;
        if (active != null && active.isActiveAndEnabled) return active;
        return fallback != null ? fallback : Camera.main;
    }

    /// <summary>
    /// Moves a document sideways / up-down (keeping the depth of point) so
    /// point (e.g. its printed page's centre) sits exactly under the screen
    /// point: an opened item lands where the finger let go. Returns the new position.
    /// </summary>
    public static Vector3 CentreUnder(Transform model, Vector3 point, Camera camera, Vector2 screen)
    {
        if (model == null || camera == null) return model != null ? model.position : Vector3.zero;
        float depth = Vector3.Dot(point - camera.transform.position, camera.transform.forward);
        if (depth <= camera.nearClipPlane) return model.position;
        model.position += camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth)) - point;
        return model.position;
    }

    private static float FitAxis(float min, float max, float viewMin, float viewMax)
    {
        if (max - min + 2f * Margin > viewMax - viewMin) return (viewMin + viewMax) * 0.5f - (min + max) * 0.5f;
        if (min < viewMin + Margin) return viewMin + Margin - min;
        if (max > viewMax - Margin) return viewMax - Margin - max;
        return 0f;
    }

    private static Rect Grow(Rect box, Vector2 point) =>
        Rect.MinMaxRect(Mathf.Min(box.xMin, point.x), Mathf.Min(box.yMin, point.y), Mathf.Max(box.xMax, point.x), Mathf.Max(box.yMax, point.y));
}
