using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The "CLIENT COPY" rubber stamp of Marie's approved copy (T08), drawn the
/// same way everywhere it appears: the floating folder's cover, the folder's
/// front page, the drag item under the finger, and a small tag on the HUD
/// folder icon. Coral ink in a rounded frame, slightly rotated. Overlays only:
/// nothing here takes touches (raycastTarget off).
/// </summary>
public static class ClientCopyStamp
{
    public const string Text = "CLIENT COPY";
    public static readonly Color Ink = new Color32(206, 58, 64, 255);
    private static readonly Color Paper = new Color32(252, 246, 236, 230);

    /// <summary>A stamp `width` units wide, centred on its parent (offset and angle in parent units / degrees).</summary>
    public static RectTransform Build(Transform parent, float width, Vector2 offset, float angle = -8f)
    {
        RectTransform stamp = HudTheme.CreateRect("Client Copy Stamp", parent);
        stamp.anchorMin = stamp.anchorMax = new Vector2(0.5f, 0.5f);
        stamp.pivot = new Vector2(0.5f, 0.5f);
        stamp.sizeDelta = new Vector2(width, width * 0.3f);
        stamp.anchoredPosition = offset;
        stamp.localRotation = Quaternion.Euler(0f, 0f, angle);
        HudTheme.AddImage(stamp, HudTheme.Rounded, Ink, width * 0.07f);

        RectTransform inner = HudTheme.CreateRect("Paper", stamp);
        HudTheme.Stretch(inner, width * 0.025f);
        HudTheme.AddImage(inner, HudTheme.Rounded, Paper, width * 0.05f);

        TextMeshProUGUI label = HudTheme.CreateLabel("Text", inner, Text, width * 0.15f, Ink,
            TextAlignmentOptions.Center, FontStyles.Bold);
        label.characterSpacing = 6f;
        HudTheme.Stretch(label.rectTransform, width * 0.03f);
        Quiet(stamp);
        return stamp;
    }

    /// <summary>A small "COPY" tag on a HUD icon's top-left corner.</summary>
    public static RectTransform BuildTag(RectTransform icon)
    {
        RectTransform tag = HudTheme.CreateRect("Client Copy Tag", icon);
        tag.anchorMin = tag.anchorMax = new Vector2(0f, 1f);
        tag.pivot = new Vector2(0.5f, 0.5f);
        tag.sizeDelta = new Vector2(68f, 26f);
        tag.anchoredPosition = new Vector2(10f, -6f);
        tag.localRotation = Quaternion.Euler(0f, 0f, 10f);
        HudTheme.AddImage(tag, HudTheme.Rounded, Ink, 10f);
        TextMeshProUGUI label = HudTheme.CreateLabel("Text", tag, "COPY", 17f, HudTheme.TextPrimary,
            TextAlignmentOptions.Center, FontStyles.Bold);
        label.characterSpacing = 3f;
        HudTheme.Stretch(label.rectTransform, 2f);
        Quiet(tag);
        return tag;
    }

    private static void Quiet(RectTransform root)
    {
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
    }
}
