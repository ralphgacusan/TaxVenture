using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The one taxpayer profile card design (blue "TAXPAYER PROFILE" header,
/// paperclip, number, name, rule, description, blank classification line,
/// stacked paper sheets underneath). Laid out proportionally so the same
/// card is used for the HUD profile windows (540x330), the dragged profile
/// stack, the NPC-dropped papers and the corkboard cards in the world.
/// Presentation only.
/// </summary>
public static class ProfileCardFace
{
    public static readonly Color CardFront = new Color32(250, 247, 238, 255);
    public static readonly Color CardBack = new Color32(222, 214, 196, 255);
    public static readonly Color HeaderBlue = new Color32(58, 86, 136, 255);
    public static readonly Color Ink = new Color32(38, 44, 70, 255);
    public static readonly Color InkFaded = new Color32(38, 44, 70, 140);
    private static readonly Color Clip = new Color32(150, 158, 170, 255);

    /// <summary>Reference design size; everything scales from it.</summary>
    public static readonly Vector2 DesignSize = new Vector2(540f, 330f);

    /// <summary>
    /// Builds the card inside root (stretched to size). Returns the front
    /// sheet so callers can tint it. closeButton is null when not requested.
    /// </summary>
    /// <param name="textBoost">Enlarges the name and description (world cards use ~1.6 so they read at a distance).</param>
    /// <param name="showFooter">The blank "Classification" line (dropped on world cards to give the text room).</param>
    public static Image Build(RectTransform root, string name, string description, int number, Vector2 size,
        bool withCloseButton, out Button closeButton, float textBoost = 1f, bool showFooter = true, bool compact = false)
    {
        closeButton = null;
        float fs = Mathf.Min(size.x / DesignSize.x, size.y / DesignSize.y);
        float sy = size.y / DesignSize.y;
        float pad = 30f * fs;

        Image front = NotesPaperView.BuildPaperStack(root, CardBack, CardFront, fs);

        // Header band.
        // Compact (world cards): a slimmer header and no paperclip, so the
        // name and description get the space.
        float headerH = compact ? 0.13f * size.y : Mathf.Max(54f * fs, 0.16f * size.y);
        RectTransform header = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Header", root), 0f, 0f, size.x, headerH);
        HudTheme.AddImage(header, HudTheme.Rounded, HeaderBlue, 8f * fs);
        TextMeshProUGUI title = HudTheme.CreateLabel("Title", header, "TAXPAYER PROFILE", 20f * fs, Color.white,
            TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        title.characterSpacing = 6f;
        HudTheme.PlaceTopLeft((RectTransform)title.transform, compact ? pad : 64f * fs, 0f, size.x * 0.62f, headerH);
        float numberRight = withCloseButton ? 60f * fs : pad;
        TextMeshProUGUI no = HudTheme.CreateLabel("Number", header, number > 0 ? "No. 0" + number : string.Empty,
            18f * fs, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.MidlineRight);
        HudTheme.PlaceTopLeft((RectTransform)no.transform, size.x - numberRight - 110f * fs, 0f, 110f * fs, headerH);

        // Paperclip peeking over the top-left corner.
        if (!compact)
        {
        RectTransform clip = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Clip", root), 22f * fs, -18f * fs, 26f * fs, 70f * fs);
        clip.localRotation = Quaternion.Euler(0f, 0f, 8f);
        HudTheme.AddImage(clip, HudTheme.Rounded, Clip, 12f * fs);
        RectTransform clipInner = HudTheme.CreateRect("Inner", clip);
        HudTheme.Stretch(clipInner, 5f * fs);
        HudTheme.AddImage(clipInner, HudTheme.Rounded, HeaderBlue, 8f * fs);
        }

        if (withCloseButton)
        {
            RectTransform close = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Close", root), size.x - 50f * fs,
                (headerH - 36f * fs) * 0.5f, 36f * fs, 36f * fs);
            Image ring = HudTheme.AddImage(close, HudTheme.Circle, new Color(1f, 1f, 1f, 0.9f));
            ring.raycastTarget = true;
            TextMeshProUGUI x = HudTheme.CreateLabel("X", close, "X", 18f * fs, HeaderBlue,
                TextAlignmentOptions.Center, FontStyles.Bold);
            HudTheme.Stretch((RectTransform)x.transform);
            closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = ring;
        }

        // Name, rule, description, classification line.
        float tb = Mathf.Max(1f, textBoost);
        float nameY = headerH + (compact ? 6f : 12f) * sy;
        TextMeshProUGUI nameLabel = HudTheme.CreateLabel("Name", root, name, 34f * fs * tb, Ink,
            TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        HudTheme.PlaceTopLeft((RectTransform)nameLabel.transform, pad, nameY, size.x - pad * 2f, 46f * fs * tb);

        float ruleY = nameY + 48f * fs * tb;
        RectTransform rule = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Rule", root), pad, ruleY, size.x - pad * 2f,
            Mathf.Max(1f, 2f * fs));
        HudTheme.AddImage(rule, null, InkFaded);

        float footerH = showFooter ? 32f * fs : 0f;
        float footerY = size.y - footerH - 18f * fs;
        float descY = ruleY + 12f * fs;
        TextMeshProUGUI desc = HudTheme.CreateLabel("Description", root, description, 26f * fs * tb, Ink,
            TextAlignmentOptions.TopLeft);
        desc.textWrappingMode = TextWrappingModes.Normal;
        desc.fontSizeMin = 16f * fs * tb;
        desc.lineSpacing = -4f;
        HudTheme.PlaceTopLeft((RectTransform)desc.transform, pad, descY, size.x - pad * 2f,
            Mathf.Max(20f * fs, footerY - descY - 6f * fs));

        if (showFooter)
        {
            TextMeshProUGUI footer = HudTheme.CreateLabel("Classification", root, "Classification: ______________",
                19f * fs, InkFaded, TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
            HudTheme.PlaceTopLeft((RectTransform)footer.transform, pad, footerY, size.x - pad * 2f, footerH);
        }

        return front;
    }

    /// <summary>1-based position of the profile in the database (for "No. 0X"), or 0.</summary>
    public static int NumberOf(string taxpayerName)
    {
        var all = TaxpayerProfileDatabase.All;
        for (int i = 0; i < all.Count; i++)
            if (all[i].TaxpayerName == taxpayerName) return i + 1;
        return 0;
    }
}
