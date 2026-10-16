using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Turns an icon's plain counter text (FolderCountText, the profile
/// CountText) into a pill badge in the icon's top-right corner: brass edge,
/// navy body, cream bold number. It pulses when the value changes, and hides
/// while the value is "0" (nothing to count).
///
/// The scene's own TMP text stays the one the game writes to; this only
/// moves and restyles it at runtime (a background is added behind it), and
/// puts every original value back when destroyed.
/// </summary>
public sealed class HudIconBadge : MonoBehaviour
{
    public const float BadgeHeight = 36f;
    private const float MinWidth = 36f;
    private const float SidePad = 11f;
    private const float FontSize = 21f;
    private const float Inset = 2f;
    private const float TopOffset = 14f;
    private const float PulseDuration = 0.35f;

    private TextMeshProUGUI text;
    private RectTransform textRect;
    private RectTransform badge;
    private string lastText;
    private float pulse = -1f;

    // Original state, restored on destroy.
    private float oFontSize;
    private Color oColor;
    private FontStyles oStyle;
    private TextAlignmentOptions oAlignment;
    private TMP_FontAsset oFont;
    private bool oAutoSize;
    private TextOverflowModes oOverflow;

    /// <summary>Restyles `counter` (a child of `icon`) as a corner badge.</summary>
    public static HudIconBadge Attach(RectTransform icon, TextMeshProUGUI counter)
    {
        if (icon == null || counter == null) return null;
        // Lives on the icon (always active), so it still sees the count change while the badge is hidden.
        var badge = icon.gameObject.AddComponent<HudIconBadge>();
        badge.Setup(icon, counter);
        return badge;
    }

    private void Setup(RectTransform icon, TextMeshProUGUI counter)
    {
        text = counter;
        textRect = (RectTransform)counter.transform;
        oFontSize = text.fontSize; oColor = text.color; oStyle = text.fontStyle; oAlignment = text.alignment;
        oFont = text.font; oAutoSize = text.enableAutoSizing; oOverflow = text.overflowMode;

        // Pill anchored to the icon's top-right, growing left so it stays on screen.
        badge = HudTheme.CreateRect("Count Badge", icon);
        badge.anchorMin = badge.anchorMax = new Vector2(1f, 1f);
        badge.pivot = new Vector2(1f, 0.5f);
        badge.anchoredPosition = new Vector2(-Inset, -TopOffset);
        badge.sizeDelta = new Vector2(MinWidth, BadgeHeight);
        RectTransform body = HudTheme.CreatePanel("Pill", badge, BadgeHeight * 0.5f);
        HudTheme.Stretch((RectTransform)body.parent);

        textRect.SetParent(badge, false);
        HudTheme.Stretch(textRect);
        textRect.localScale = Vector3.one;
        text.font = HudTheme.Font;
        text.fontSize = FontSize;
        text.enableAutoSizing = false;
        text.fontStyle = FontStyles.Bold;
        text.color = HudTheme.TextPrimary;
        text.alignment = TextAlignmentOptions.Center;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        Refresh(animate: false);
    }

    private void LateUpdate()
    {
        if (text.text != lastText) Refresh(animate: true);

        if (pulse >= 0f)
        {
            pulse += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(pulse / PulseDuration);
            float s = 1f + 0.3f * Mathf.Sin(k * Mathf.PI);
            badge.localScale = new Vector3(s, s, 1f);
            if (k >= 1f)
            {
                badge.localScale = Vector3.one;
                pulse = -1f;
            }
        }
    }

    private void Refresh(bool animate)
    {
        lastText = text.text;
        bool show = IsShown(lastText);
        badge.gameObject.SetActive(show);
        if (!show) return;
        float width = Mathf.Max(MinWidth, text.GetPreferredValues(lastText).x + SidePad * 2f);
        badge.sizeDelta = new Vector2(width, BadgeHeight);
        if (animate) pulse = 0f;
    }

    /// <summary>Hidden for empty text and a plain zero count ("0/4" still shows).</summary>
    public static bool IsShown(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return value.Trim() != "0";
    }

    private void OnDestroy()
    {
        // The HUD only goes away with its scene, while the icon and its text
        // are being destroyed too: re-parenting then is an error, so the
        // counter is left inside the (dying) badge. Only plain styling is reset.
        if (text != null)
        {
            text.font = oFont; text.fontSize = oFontSize; text.color = oColor; text.fontStyle = oStyle;
            text.alignment = oAlignment; text.enableAutoSizing = oAutoSize; text.overflowMode = oOverflow;
        }
        if (badge != null) Destroy(badge.gameObject);
    }
}
