using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The floating goal marker over the current objective: a bobbing pin with
/// a ripple, the goal's name and the distance. When the goal is off screen
/// or behind the player it sticks to the screen edge with an arrow pointing
/// the way. Fades out close to the goal so it never clutters a conversation.
/// </summary>
public sealed class ObjectiveBeacon : MonoBehaviour
{
    private const float EdgeMargin = 90f;
    private const float NearFadeStart = 6f;
    private const float NearFadeEnd = 3f;

    private RectTransform root;
    private RectTransform marker;
    private CanvasGroup group;
    private RectTransform pin;
    private Image pinImage;
    private RectTransform ripple;
    private Image rippleImage;
    private RectTransform edgeArrow;
    private Image edgeArrowImage;
    private TextMeshProUGUI label;
    private Color accent = new Color32(100, 215, 255, 255);
    private float alpha;
    private float flash;
    private string lastLabel;
    private string labelFormat = "{0}m";

    public static ObjectiveBeacon Create(RectTransform parent)
    {
        RectTransform rect = HudTheme.CreateRect("Objective Beacon", parent);
        HudTheme.Stretch(rect);
        var beacon = rect.gameObject.AddComponent<ObjectiveBeacon>();
        beacon.root = rect;
        beacon.Build();
        return beacon;
    }

    private void Build()
    {
        marker = HudTheme.CreateRect("Marker", root);
        marker.anchorMin = marker.anchorMax = new Vector2(0.5f, 0.5f);
        marker.sizeDelta = new Vector2(80f, 80f);
        group = marker.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;

        ripple = HudTheme.CreateRect("Ripple", marker);
        ripple.sizeDelta = new Vector2(90f, 90f);
        rippleImage = HudTheme.AddImage(ripple, GuideArt.Ring, Color.clear);

        pin = HudTheme.CreateRect("Pin", marker);
        pin.sizeDelta = new Vector2(64f, 64f);
        pin.pivot = new Vector2(0.5f, 0.04f);
        pinImage = HudTheme.AddImage(pin, GuideArt.Pin, accent);

        edgeArrow = HudTheme.CreateRect("Edge Arrow", marker);
        edgeArrow.sizeDelta = new Vector2(40f, 40f);
        edgeArrowImage = HudTheme.AddImage(edgeArrow, GuideArt.Arrow, accent);

        RectTransform plate = HudTheme.CreateRect("Plate", marker);
        plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 0.5f);
        plate.pivot = new Vector2(0.5f, 1f);
        plate.anchoredPosition = new Vector2(0f, -8f);
        plate.sizeDelta = new Vector2(230f, 40f);
        HudTheme.AddImage(plate, HudTheme.Rounded, new Color32(16, 25, 38, 215), 20f);
        label = HudTheme.CreateLabel("Label", plate, "", 20f, HudTheme.TextPrimary, TextAlignmentOptions.Center,
            FontStyles.Bold);
        HudTheme.Stretch(label.rectTransform, 6f);
    }

    public void SetAccent(Color color)
    {
        accent = color;
        pinImage.color = color;
        edgeArrowImage.color = color;
    }

    /// <summary>Big pop when the goal changes.</summary>
    public void Flash() => flash = 1f;

    public void Hide()
    {
        alpha = Mathf.MoveTowards(alpha, 0f, Time.unscaledDeltaTime * 5f);
        group.alpha = alpha;
    }

    public void Tick(Camera cam, Vector3 world, float distance, string goalLabel)
    {
        float dt = Time.unscaledDeltaTime;
        float now = Time.unscaledTime;
        Rect r = root.rect;
        Vector3 sp = cam.WorldToScreenPoint(world);
        bool behind = sp.z < 0.1f;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, null, out Vector2 local);
        if (behind) local = -local; // mirrored when behind the camera

        Rect inner = new Rect(r.xMin + EdgeMargin, r.yMin + EdgeMargin + 40f, r.width - EdgeMargin * 2f, r.height - EdgeMargin * 2f - 220f);
        bool offScreen = behind || !inner.Contains(local);
        Vector2 placed = local;
        if (offScreen)
        {
            Vector2 dir = local - inner.center;
            if (behind && dir.sqrMagnitude < 1f) dir = Vector2.down;
            float sx = dir.x != 0f ? (inner.width * 0.5f) / Mathf.Abs(dir.x) : float.MaxValue;
            float sy = dir.y != 0f ? (inner.height * 0.5f) / Mathf.Abs(dir.y) : float.MaxValue;
            placed = inner.center + dir * Mathf.Min(sx, sy);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            edgeArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
            edgeArrow.anchoredPosition = dir.normalized * 58f;
        }

        // On screen and close: step aside.
        float near = offScreen ? 1f : Mathf.Clamp01(Mathf.InverseLerp(NearFadeEnd, NearFadeStart, distance));
        alpha = Mathf.MoveTowards(alpha, near, dt * 4f);
        group.alpha = alpha;

        marker.anchoredPosition = Vector2.Lerp(marker.anchoredPosition, placed, alpha < 0.05f ? 1f : 1f - Mathf.Exp(-18f * dt));
        edgeArrow.gameObject.SetActive(offScreen);

        flash = Mathf.MoveTowards(flash, 0f, dt * 1.2f);
        float bob = Mathf.Sin(now * 3f) * 6f;
        pin.anchoredPosition = new Vector2(0f, 6f + bob);
        pin.localScale = Vector3.one * (1f + 0.5f * Mathf.Sin(flash * Mathf.PI) * flash);

        float rk = Mathf.Repeat(now, 1.4f) / 1.4f;
        ripple.anchoredPosition = new Vector2(0f, 4f);
        ripple.localScale = new Vector3(Mathf.Lerp(0.3f, 1.3f, rk), Mathf.Lerp(0.15f, 0.55f, rk), 1f);
        rippleImage.color = new Color(accent.r, accent.g, accent.b, (1f - rk) * 0.8f);

        if (goalLabel != lastLabel)
        {
            lastLabel = goalLabel;
            // Braces in a label would break the format string.
            string safe = string.IsNullOrEmpty(goalLabel) ? "" : goalLabel.Replace("{", "(").Replace("}", ")");
            labelFormat = safe.Length == 0 ? "{0}m" : safe + "  ·  {0}m";
        }
        label.SetText(labelFormat, Mathf.Clamp(Mathf.RoundToInt(distance), 0, 9999));
    }
}
