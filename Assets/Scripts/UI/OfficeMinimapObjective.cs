using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Shows the current objective on the minimap (data from ObjectiveGuide):
/// - a goal pin that keeps pulsing with a ripple, so it always catches the
///   eye; it sticks to the map edge (with an arrow) when the goal is off-map;
/// - dots along the walking route;
/// - an attention "pop" of the whole map with a glow when the objective
///   changes, repeated as a gentle reminder while the goal is still far.
/// Read-only; same placement maths as OfficeMinimapMarkers.
/// </summary>
public sealed class OfficeMinimapObjective
{
    private const int MaxDots = 26;
    private const float DotSpacing = 1.6f;
    private const float PopSeconds = 1.6f;

    private readonly RectTransform viewport;
    private readonly RectTransform frame;
    private readonly RectTransform root;
    private readonly RectTransform pin;
    private readonly Image pinImage;
    private readonly RectTransform ripple;
    private readonly Image rippleImage;
    private readonly RectTransform edgeArrow;
    private readonly Image edgeArrowImage;
    private readonly RectTransform glow;
    private readonly Image glowImage;
    private readonly List<RectTransform> dots = new List<RectTransform>();
    private readonly List<Image> dotImages = new List<Image>();

    private int seenVersion = -1;
    private float popAge = 99f;
    private float nextReminder;

    public OfficeMinimapObjective(RectTransform mapViewport, RectTransform panel)
    {
        viewport = mapViewport;
        frame = mapViewport.parent as RectTransform;

        // Glow behind the panel (the panel's own CanvasGroup fades it with the map).
        glow = NewRect("Objective Glow", panel, Vector2.zero);
        glow.anchorMin = Vector2.zero;
        glow.anchorMax = Vector2.one;
        glow.offsetMin = new Vector2(-34f, -34f);
        glow.offsetMax = new Vector2(34f, 34f);
        glow.SetAsFirstSibling();
        glowImage = glow.gameObject.AddComponent<Image>();
        glowImage.sprite = HudTheme.Rounded;
        glowImage.type = Image.Type.Sliced;
        glowImage.pixelsPerUnitMultiplier = 0.4f;
        glowImage.raycastTarget = false;
        glowImage.color = Color.clear;

        root = NewRect("Objective", viewport, Vector2.zero);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;

        for (int i = 0; i < MaxDots; i++)
        {
            RectTransform dot = NewRect("Route " + i, root, new Vector2(6f, 6f));
            Image image = dot.gameObject.AddComponent<Image>();
            image.sprite = GuideArt.Dot;
            image.raycastTarget = false;
            dots.Add(dot);
            dotImages.Add(image);
            dot.gameObject.SetActive(false);
        }

        ripple = NewRect("Goal Ripple", root, new Vector2(40f, 40f));
        rippleImage = ripple.gameObject.AddComponent<Image>();
        rippleImage.sprite = GuideArt.Ring;
        rippleImage.raycastTarget = false;

        pin = NewRect("Goal Pin", root, new Vector2(26f, 26f));
        pin.pivot = new Vector2(0.5f, 0.04f);
        pinImage = pin.gameObject.AddComponent<Image>();
        pinImage.sprite = GuideArt.Pin;
        pinImage.raycastTarget = false;

        edgeArrow = NewRect("Goal Edge Arrow", root, new Vector2(16f, 16f));
        edgeArrowImage = edgeArrow.gameObject.AddComponent<Image>();
        edgeArrowImage.sprite = GuideArt.Arrow;
        edgeArrowImage.raycastTarget = false;

        SetActive(false);
    }

    public bool IsValid => root != null;

    public void Tick(Camera camera)
    {
        ObjectiveGuide guide = ObjectiveGuide.Instance;
        bool show = guide != null && guide.MinimapEnabled && guide.HasTarget;
        if (!show)
        {
            SetActive(false);
            if (frame != null) frame.localScale = Vector3.one;
            glowImage.color = Color.clear;
            return;
        }
        SetActive(true);

        float now = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;
        Color accent = guide.Accent;

        // Attention pops: on a new objective, then as reminders while far.
        if (guide.Version != seenVersion)
        {
            seenVersion = guide.Version;
            popAge = 0f;
            nextReminder = now + Mathf.Max(3f, guide.ReminderSeconds);
        }
        else if (guide.ReminderSeconds > 0f && now >= nextReminder)
        {
            nextReminder = now + guide.ReminderSeconds;
            if (guide.IsGuiding && guide.Distance > 12f) popAge = 0.6f; // a softer pop
        }
        popAge += dt;
        float pop = popAge < PopSeconds ? Mathf.Sin(popAge / PopSeconds * Mathf.PI * 3f) * (1f - popAge / PopSeconds) : 0f;
        if (frame != null) frame.localScale = Vector3.one * (1f + 0.07f * Mathf.Abs(pop));
        float glowAlpha = popAge < PopSeconds ? (1f - popAge / PopSeconds) * 0.75f : 0f;
        glowImage.color = new Color(accent.r, accent.g, accent.b, Mathf.Max(glowAlpha, 0.12f + 0.08f * Mathf.Sin(now * 3f)));

        // Goal pin (clamped to the map edge when outside it).
        Vector2 size = root.rect.size;
        Vector2 goal = MapPoint(camera, guide.TargetPosition, size, out bool valid);
        float half = Mathf.Min(size.x, size.y) * 0.5f - 12f;
        bool outside = !valid || Mathf.Abs(goal.x) > half || Mathf.Abs(goal.y) > half;
        if (outside && goal.sqrMagnitude > 0.01f)
        {
            float scale = half / Mathf.Max(Mathf.Abs(goal.x), Mathf.Abs(goal.y));
            Vector2 clamped = goal * scale;
            edgeArrow.gameObject.SetActive(true);
            edgeArrow.anchoredPosition = clamped;
            edgeArrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(goal.y, goal.x) * Mathf.Rad2Deg - 90f);
            edgeArrowImage.color = accent;
            goal = clamped - clamped.normalized * 14f;
        }
        else
        {
            edgeArrow.gameObject.SetActive(false);
        }

        float beat = Mathf.Abs(Mathf.Sin(now * 3.4f));
        pin.anchoredPosition = goal;
        pin.localScale = Vector3.one * (1f + 0.28f * beat + 0.6f * Mathf.Abs(pop));
        pinImage.color = accent;
        float rk = Mathf.Repeat(now, 1.1f) / 1.1f;
        ripple.anchoredPosition = goal;
        ripple.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.6f, rk);
        rippleImage.color = new Color(accent.r, accent.g, accent.b, (1f - rk) * 0.9f);
        pin.SetAsLastSibling();
        edgeArrow.SetAsLastSibling();

        PlaceRouteDots(camera, guide, size, accent, now);
    }

    private void PlaceRouteDots(Camera camera, ObjectiveGuide guide, Vector2 size, Color accent, float now)
    {
        IReadOnlyList<Vector3> route = guide.Route;
        int used = 0;
        if (guide.IsGuiding && route != null && route.Count >= 2)
        {
            float carry = DotSpacing;
            for (int i = 1; i < route.Count && used < MaxDots; i++)
            {
                Vector3 a = route[i - 1], b = route[i];
                Vector3 ab = b - a;
                ab.y = 0f;
                float length = ab.magnitude;
                float d = carry;
                while (d <= length && used < MaxDots)
                {
                    Vector2 p = MapPoint(camera, a + ab * (d / length), size, out bool valid);
                    if (!valid || Mathf.Abs(p.x) > size.x * 0.5f - 4f || Mathf.Abs(p.y) > size.y * 0.5f - 4f) break;
                    RectTransform dot = dots[used];
                    dot.gameObject.SetActive(true);
                    dot.anchoredPosition = p;
                    float wave = 0.5f + 0.5f * Mathf.Sin(used * 0.9f - now * 6f);
                    dotImages[used].color = new Color(accent.r, accent.g, accent.b, 0.55f + 0.45f * wave);
                    dot.localScale = Vector3.one * (0.85f + 0.35f * wave);
                    used++;
                    d += DotSpacing;
                }
                carry = d - length;
            }
        }
        for (int i = used; i < dots.Count; i++)
            if (dots[i].gameObject.activeSelf) dots[i].gameObject.SetActive(false);
    }

    private static Vector2 MapPoint(Camera camera, Vector3 world, Vector2 size, out bool valid)
    {
        world.y = 0f;
        Vector3 vp = camera.WorldToViewportPoint(world);
        valid = vp.z > 0f && size.x > 0f;
        return new Vector2((vp.x - 0.5f) * size.x, (vp.y - 0.5f) * size.y);
    }

    private void SetActive(bool on)
    {
        if (root.gameObject.activeSelf != on) root.gameObject.SetActive(on);
        if (glow.gameObject.activeSelf != on) glow.gameObject.SetActive(on);
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(LayoutElement));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        go.GetComponent<LayoutElement>().ignoreLayout = true;
        return rect;
    }

    public void Dispose()
    {
        if (frame != null) frame.localScale = Vector3.one;
        if (root != null) Object.Destroy(root.gameObject);
        if (glow != null) Object.Destroy(glow.gameObject);
        dots.Clear();
    }
}
