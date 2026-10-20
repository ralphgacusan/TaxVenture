using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presentation-only "printed document" treatment for a world-space paper:
/// warm paper with a soft drop shadow, two slightly offset sheets underneath
/// (siblings, so they stay put while the top page flips), a thin printed
/// border, a tinted header strip under the title (clean HUD print font),
/// round page-turn arrow buttons, a red bookmark ribbon on the Tax Code Book
/// and a "CASE FILE" tab on the case folder, and ink-coloured text instead
/// of pure black. Applied once at runtime from the
/// document UIs, so no scene edit is required. Every decoration ignores
/// raycasts, so value rows, chips and page-edge zones stay clickable.
/// </summary>
[DisallowMultipleComponent]
public sealed class PrintedPaperStyle : MonoBehaviour
{
    private static readonly Color InkColor = new Color(0.12f, 0.13f, 0.17f, 1f);
    private static readonly Color BorderColor = new Color(0.2f, 0.22f, 0.28f, 0.35f);
    private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.28f);
    private static readonly Color PaperColor = new Color(0.988f, 0.968f, 0.925f, 1f);
    private static readonly Color HeaderColor = new Color(0.93f, 0.885f, 0.78f, 1f);
    private static readonly Color HeaderLine = new Color(0.62f, 0.48f, 0.2f, 0.85f);
    private static readonly Color HeadingInk = new Color(0.1f, 0.16f, 0.28f, 1f);
    private static readonly Color Navy = new Color(0.07f, 0.11f, 0.17f, 1f);
    private static readonly Color Brass = new Color(0.79f, 0.66f, 0.4f, 1f);
    private static readonly Color Cream = new Color(0.96f, 0.94f, 0.89f, 1f);
    private static readonly Color Ribbon = new Color(0.78f, 0.25f, 0.24f, 1f);

    private const float BorderInset = 28f;
    private const float BorderThickness = 3f;

    private bool applied;

    /// <summary>
    /// Styles <paramref name="paper"/> once. Text passed in is re-coloured
    /// to ink only when it is still pure black, so deliberate colours stay.
    /// </summary>
    public static void Apply(RectTransform paper, params TMP_Text[] inkTexts)
    {
        if (paper == null)
            return;

        PrintedPaperStyle style = paper.GetComponent<PrintedPaperStyle>();
        if (style == null)
            style = paper.gameObject.AddComponent<PrintedPaperStyle>();
        style.ApplyOnce(inkTexts);
    }

    private void ApplyOnce(TMP_Text[] inkTexts)
    {
        if (applied)
            return;
        applied = true;

        RectTransform paper = (RectTransform)transform;
        Image paperImage = GetComponent<Image>();
        if (paperImage != null) paperImage.color = PaperColor;

        if (paperImage != null && GetComponent<Shadow>() == null)
        {
            Shadow shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = ShadowColor;
            shadow.effectDistance = new Vector2(10f, -12f);
        }

        if (paperImage != null && paper.parent != null)
        {
            Color sheet = paperImage.color * 0.94f;
            sheet.a = paperImage.color.a;
            CreateUnderSheet(paper, paperImage, "PrintedSheetBack", new Vector2(18f, -22f), -1.2f,
                sheet * 0.97f);
            CreateUnderSheet(paper, paperImage, "PrintedSheetMiddle", new Vector2(9f, -11f), 0.7f, sheet);
        }

        CreateBorder(paper);

        TMP_Text heading = inkTexts != null && inkTexts.Length > 0 ? inkTexts[0] : null;
        TMP_Text indicator = inkTexts != null && inkTexts.Length > 2 ? inkTexts[2] : null;
        CreateHeader(paper, heading);
        StyleArrows(paper);
        if (indicator != null)
        {
            indicator.font = HudTheme.Font;
            indicator.color = new Color(0.36f, 0.4f, 0.48f, 1f);
            indicator.characterSpacing = 2f;
        }
        if (GetComponentInParent<TaxCodeBookUI>(true) != null) CreateBookmark(paper);
        else if (GetComponentInParent<CaseFolderUI>(true) != null) CreateCaseTab(paper);

        if (inkTexts != null)
        {
            for (int i = 0; i < inkTexts.Length; i++)
            {
                TMP_Text text = inkTexts[i];
                if (text != null && IsPureBlack(text.color))
                    text.color = InkColor;
            }
        }
    }

    private static void CreateUnderSheet(RectTransform paper, Image paperImage, string sheetName,
        Vector2 offset, float tiltDegrees, Color color)
    {
        if (paper.parent.Find(sheetName) != null)
            return;

        GameObject go = new GameObject(sheetName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = paper.gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(paper.parent, false);
        rect.SetSiblingIndex(paper.GetSiblingIndex());

        rect.anchorMin = paper.anchorMin;
        rect.anchorMax = paper.anchorMax;
        rect.pivot = paper.pivot;
        rect.sizeDelta = paper.sizeDelta;
        rect.localScale = paper.localScale;
        rect.localRotation = paper.localRotation * Quaternion.Euler(0f, 0f, tiltDegrees);
        // Offset within the paper's own plane (papers can be rotated inside
        // their canvas); hierarchy order keeps the sheet drawn behind.
        Vector3 paperLocalOffset = Vector3.Scale(paper.localScale, new Vector3(offset.x, offset.y, 0f));
        rect.localPosition = paper.localPosition + paper.localRotation * paperLocalOffset;

        Image image = go.GetComponent<Image>();
        image.sprite = paperImage.sprite;
        image.type = paperImage.type;
        image.color = color;
        image.raycastTarget = false;

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = ShadowColor * 0.6f;
        shadow.effectDistance = new Vector2(6f, -6f);
    }

    private static void CreateBorder(RectTransform paper)
    {
        if (paper.Find("PrintedBorder") != null)
            return;

        GameObject root = new GameObject("PrintedBorder", typeof(RectTransform));
        root.layer = paper.gameObject.layer;
        RectTransform frame = (RectTransform)root.transform;
        frame.SetParent(paper, false);
        frame.SetAsFirstSibling();
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.one;
        frame.offsetMin = new Vector2(BorderInset, BorderInset);
        frame.offsetMax = new Vector2(-BorderInset, -BorderInset);

        AddLine(frame, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, BorderThickness));
        AddLine(frame, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, BorderThickness));
        AddLine(frame, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(BorderThickness, 0f));
        AddLine(frame, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(BorderThickness, 0f));
    }

    private static void AddLine(RectTransform parent, string lineName, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 size)
    {
        GameObject go = new GameObject(lineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image image = go.GetComponent<Image>();
        image.color = BorderColor;
        image.raycastTarget = false;
    }

    // ------------------------------------------------------------
    // Header strip, arrows, bookmark / case tab
    // ------------------------------------------------------------

    /// <summary>A tinted strip with a brass rule behind the page title, which uses the print font.</summary>
    private static void CreateHeader(RectTransform paper, TMP_Text heading)
    {
        if (paper.Find("PrintedHeader") != null)
            return;

        float bottom = -170f;
        if (heading != null)
        {
            heading.font = HudTheme.Font;
            heading.color = HeadingInk;
            heading.fontStyle = FontStyles.Bold;
            heading.characterSpacing = 1.5f;
            heading.ForceMeshUpdate();
            Bounds b = heading.textBounds;
            if (b.size.y > 0.01f)
            {
                Vector3 low = paper.InverseTransformPoint(heading.transform.TransformPoint(new Vector3(b.center.x, b.min.y, 0f)));
                // Measured from the paper's top edge.
                float top = paper.rect.yMax;
                bottom = Mathf.Clamp(low.y - top - 30f, -320f, -110f);
            }
        }

        Image strip = NewImage(paper, "PrintedHeader", HeaderColor);
        RectTransform rect = strip.rectTransform;
        rect.SetSiblingIndex(1);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(BorderInset + 6f, bottom);
        rect.offsetMax = new Vector2(-BorderInset - 6f, -BorderInset - 6f);

        Image rule = NewImage(rect, "Rule", HeaderLine);
        RectTransform r = rule.rectTransform;
        r.anchorMin = new Vector2(0f, 0f);
        r.anchorMax = new Vector2(1f, 0f);
        r.pivot = new Vector2(0.5f, 0f);
        r.sizeDelta = new Vector2(0f, 4f);
        r.anchoredPosition = Vector2.zero;
    }

    /// <summary>The "&lt;" / "&gt;" page-turn hints become round navy buttons with brass rims.</summary>
    private static void StyleArrows(RectTransform paper)
    {
        foreach (TMP_Text text in paper.GetComponentsInChildren<TMP_Text>(true))
        {
            bool left = text.name == "LeftEdgeIndicator";
            if (!left && text.name != "RightEdgeIndicator") continue;
            if (text.transform.Find("ArrowDisc") != null) continue;

            // The indicator keeps its object (the UIs show / hide it); its own glyph is hidden.
            text.color = new Color(0f, 0f, 0f, 0f);

            RectTransform disc = NewImage((RectTransform)text.transform, "ArrowDisc", Brass).rectTransform;
            disc.GetComponent<Image>().sprite = HudTheme.Circle;
            disc.anchorMin = disc.anchorMax = new Vector2(0.5f, 0.5f);
            disc.sizeDelta = new Vector2(98f, 98f); // big enough to hit with a thumb
            disc.anchoredPosition = Vector2.zero;
            Shadow shadow = disc.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
            shadow.effectDistance = new Vector2(3f, -4f);

            Image face = NewImage(disc, "Face", Navy);
            face.sprite = HudTheme.Circle;
            RectTransform f = face.rectTransform;
            f.anchorMin = Vector2.zero;
            f.anchorMax = Vector2.one;
            f.offsetMin = new Vector2(4f, 4f);
            f.offsetMax = new Vector2(-4f, -4f);

            GameObject go = new GameObject("Chevron", typeof(RectTransform));
            go.layer = paper.gameObject.layer;
            go.transform.SetParent(f, false);
            TextMeshProUGUI chevron = go.AddComponent<TextMeshProUGUI>();
            chevron.font = HudTheme.Font;
            chevron.text = left ? "<" : ">";
            chevron.fontSize = 56f;
            chevron.fontStyle = FontStyles.Bold;
            chevron.color = Cream;
            chevron.alignment = TextAlignmentOptions.Center;
            chevron.raycastTarget = false;
            RectTransform c = (RectTransform)go.transform;
            c.anchorMin = Vector2.zero;
            c.anchorMax = Vector2.one;
            c.offsetMin = new Vector2(left ? -3f : 3f, 2f);
            c.offsetMax = new Vector2(left ? -3f : 3f, 2f);
        }
    }

    /// <summary>A red ribbon bookmark hanging over the top edge of the Tax Code Book page.</summary>
    private static void CreateBookmark(RectTransform paper)
    {
        if (paper.Find("Bookmark") != null)
            return;
        Image ribbon = NewImage(paper, "Bookmark", Ribbon);
        ribbon.sprite = BookmarkSprite;
        RectTransform rect = ribbon.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(52f, 170f);
        rect.anchoredPosition = new Vector2(-62f, 26f);
        Shadow shadow = ribbon.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
        shadow.effectDistance = new Vector2(3f, -3f);
    }

    /// <summary>A navy "CASE FILE" tab on the case folder page.</summary>
    private static void CreateCaseTab(RectTransform paper)
    {
        if (paper.Find("CaseTab") != null)
            return;
        Image tab = NewImage(paper, "CaseTab", Navy);
        tab.sprite = HudTheme.Rounded;
        tab.type = Image.Type.Sliced;
        tab.pixelsPerUnitMultiplier = 20f / 12f;
        RectTransform rect = tab.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(210f, 54f);
        rect.anchoredPosition = new Vector2(-60f, -60f);
        rect.localRotation = Quaternion.Euler(0f, 0f, -3f);

        Image rim = NewImage(rect, "Rim", Brass);
        rim.sprite = HudTheme.Rounded;
        rim.type = Image.Type.Sliced;
        rim.pixelsPerUnitMultiplier = 20f / 12f;
        rim.fillCenter = false;
        RectTransform rr = rim.rectTransform;
        rr.anchorMin = Vector2.zero;
        rr.anchorMax = Vector2.one;
        rr.offsetMin = new Vector2(4f, 4f);
        rr.offsetMax = new Vector2(-4f, -4f);

        GameObject go = new GameObject("Label", typeof(RectTransform));
        go.layer = paper.gameObject.layer;
        go.transform.SetParent(rect, false);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = HudTheme.Font;
        label.text = "CASE FILE";
        label.fontSize = 26f;
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 6f;
        label.color = Cream;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        RectTransform lr = (RectTransform)go.transform;
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
    }

    private static Sprite bookmarkSprite;

    /// <summary>Ribbon strip with a V-notch at the bottom and a soft centre fold.</summary>
    private static Sprite BookmarkSprite
    {
        get
        {
            if (bookmarkSprite != null) return bookmarkSprite;
            const int w = 32, h = 104;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Bookmark" };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                    float notch = 0.14f * (1f - Mathf.Abs(u - 0.5f) * 2f);
                    bool inside = v > notch;
                    float shade = 0.82f + 0.18f * (1f - Mathf.Abs(u - 0.5f) * 2f);
                    byte c = (byte)(255 * shade);
                    px[y * w + x] = new Color32(c, c, c, (byte)(inside ? 255 : 0));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            bookmarkSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            bookmarkSprite.name = "Bookmark";
            return bookmarkSprite;
        }
    }

    private static Image NewImage(RectTransform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static bool IsPureBlack(Color color) =>
        color.r < 0.01f && color.g < 0.01f && color.b < 0.01f;
}
