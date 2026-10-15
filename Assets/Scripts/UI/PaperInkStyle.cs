using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presentation-only "printed on paper" look for the interactive values on
/// the open case folder and tax code book. Rows become printed form lines
/// (drop targets get a fill-in box), tax-code values become highlighter
/// marks with a grip. Only colours, rich text and raycast-free decoration
/// children change; data, layout ownership and drag/transfer components
/// are untouched.
/// </summary>
public static class PaperInkStyle
{
    // Ink palette.
    public static readonly Color PrintInk = new Color(0.12f, 0.13f, 0.17f, 1f);
    private const string CaptionInkHex = "#4B5568";
    private const string PenInkHex = "#1E3A8A";
    private const string BlankInkHex = "#8A94A6";

    // Row (case folder field).
    private static readonly Color FillInBox = new Color(0.84f, 0.9f, 1f, 0.55f);
    private static readonly Color FillInBoxEdge = new Color(0.2f, 0.32f, 0.6f, 0.55f);
    private static readonly Color PrintedLine = new Color(0.2f, 0.22f, 0.28f, 0.45f);
    private static readonly Color RowRest = new Color(1f, 1f, 1f, 0f);

    // Chip (tax code value).
    private static readonly Color MarkerRest = new Color(1f, 0.88f, 0.38f, 0.42f);
    private static readonly Color MarkerEdge = new Color(0.55f, 0.42f, 0.08f, 0.35f);

    private static readonly Color Selected = new Color(1f, 0.8f, 0.25f, 0.85f);
    private static readonly Color GripInk = new Color(0.2f, 0.22f, 0.28f, 0.45f);

    private const string DecorName = "PaperInkDecor";

    private const float ChipHeight = 58f;
    private const float ChipFontMax = 40f;
    private const float ChipFontMin = 24f;

    /// <summary>Rich text for a printed field caption and its pen-ink value.</summary>
    public static string FieldText(string label, string value, bool isBlank)
    {
        string valueHex = isBlank ? BlankInkHex : PenInkHex;
        return $"<color={CaptionInkHex}><size=82%>{label}:</size></color>  <color={valueHex}>{value}</color>";
    }

    /// <summary>Styles a case folder field row as a printed form line.</summary>
    public static void StyleFieldRow(Image background, TMP_Text label, bool isDropTarget,
        ValueSelectionHighlighter highlighter, ValueTypeValidationFeedback feedback)
    {
        if (label != null)
        {
            label.color = PrintInk;
            label.fontStyle = FontStyles.Normal;
            Vector4 margin = label.margin;
            margin.x = Mathf.Max(margin.x, 12f);
            margin.z = Mathf.Max(margin.z, 40f); // room for the grip dots
            label.margin = margin;
        }

        if (background == null)
            return;

        Color rest = isDropTarget ? FillInBox : RowRest;
        ApplyRestColor(background, rest, highlighter, feedback);

        Outline edge = background.GetComponent<Outline>();
        if (isDropTarget)
        {
            if (edge == null)
                edge = background.gameObject.AddComponent<Outline>();
            edge.effectColor = FillInBoxEdge;
            edge.effectDistance = new Vector2(2f, -2f);
            edge.enabled = true;
        }
        else if (edge != null)
        {
            edge.enabled = false;
        }

        RectTransform decor = EnsureDecor(background.rectTransform);
        SetActive(decor, "Underline", !isDropTarget, () => CreateUnderline(decor));
        SetActive(decor, "Grip", true, () => CreateGrip(decor, new Vector2(1f, 0.5f), new Vector2(-18f, 0f)));
    }

    /// <summary>Styles a tax-code value chip as a draggable highlighter mark.</summary>
    public static void StyleValueChip(Image background, TMP_Text text, ValueSelectionHighlighter highlighter)
    {
        if (text != null)
        {
            text.color = PrintInk;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            Vector4 margin = text.margin;
            margin.x = Mathf.Max(margin.x, 44f);
            margin.z = Mathf.Max(margin.z, 10f);
            text.margin = margin;

            // One line that shrinks to fit, instead of wrapping out of the strip.
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMax = ChipFontMax;
            text.fontSizeMin = ChipFontMin;
        }

        LayoutElement chipLayout = background.GetComponent<LayoutElement>();
        if (chipLayout != null)
        {
            chipLayout.minHeight = Mathf.Max(chipLayout.minHeight, ChipHeight);
            chipLayout.preferredHeight = Mathf.Max(chipLayout.preferredHeight, ChipHeight);
        }

        if (background == null)
            return;

        ApplyRestColor(background, MarkerRest, highlighter, null);

        Outline edge = background.GetComponent<Outline>();
        if (edge == null)
            edge = background.gameObject.AddComponent<Outline>();
        edge.effectColor = MarkerEdge;
        edge.effectDistance = new Vector2(2f, -2f);

        RectTransform decor = EnsureDecor(background.rectTransform);
        SetActive(decor, "Grip", true, () => CreateGrip(decor, new Vector2(0f, 0.5f), new Vector2(20f, 0f)));
    }

    /// <summary>
    /// Splits text into paragraphs separated by blank (or whitespace-only)
    /// lines, keeping the lines inside each paragraph.
    /// </summary>
    public static List<string> Paragraphs(string body)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(body))
            return result;

        var current = new List<string>();
        foreach (string raw in body.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length > 0)
            {
                current.Add(line);
            }
            else if (current.Count > 0)
            {
                result.Add(string.Join("\n", current));
                current.Clear();
            }
        }

        if (current.Count > 0)
            result.Add(string.Join("\n", current));
        return result;
    }

    /// <summary>A printed description line placed under a chip in a layout group.</summary>
    public static TextMeshProUGUI CreateCaption(Transform parent, TMP_FontAsset font, string text, float fontSize)
    {
        GameObject go = new GameObject("PrintedCaption", typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        TextMeshProUGUI caption = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
            caption.font = font;
        caption.text = text;
        caption.fontSize = fontSize;
        caption.color = new Color(0.25f, 0.27f, 0.33f, 1f);
        caption.alignment = TextAlignmentOptions.TopLeft;
        caption.margin = new Vector4(44f, 0f, 8f, 0f);
        caption.raycastTarget = false;
        caption.textWrappingMode = TextWrappingModes.Normal;
        return caption;
    }

    /// <summary>Fixed vertical gap for a layout group.</summary>
    public static GameObject CreateSpacer(Transform parent, float height)
    {
        GameObject go = new GameObject("PrintedSpacer", typeof(RectTransform), typeof(LayoutElement));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        LayoutElement element = go.GetComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        return go;
    }

    private static void ApplyRestColor(Image background, Color rest, ValueSelectionHighlighter highlighter,
        ValueTypeValidationFeedback feedback)
    {
        if (feedback != null)
            feedback.SetRestingColor(background, rest);

        if (highlighter != null)
            highlighter.SetColors(rest, Selected);
        else
            background.color = rest;
    }

    private static RectTransform EnsureDecor(RectTransform owner)
    {
        Transform existing = owner.Find(DecorName);
        if (existing != null)
            return (RectTransform)existing;

        GameObject go = new GameObject(DecorName, typeof(RectTransform), typeof(LayoutElement));
        go.layer = owner.gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(owner, false);
        rect.SetAsFirstSibling();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        // Decoration must not take part in the owner's layout group.
        go.GetComponent<LayoutElement>().ignoreLayout = true;
        return rect;
    }

    private static void SetActive(RectTransform decor, string childName, bool active, System.Action create)
    {
        Transform child = decor.Find(childName);
        if (child == null && active)
        {
            create();
            child = decor.Find(childName);
        }

        if (child != null)
            child.gameObject.SetActive(active);
    }

    private static void CreateUnderline(RectTransform decor)
    {
        Image line = NewImage(decor, "Underline", PrintedLine);
        RectTransform rect = line.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(0f, 3f);
        rect.anchoredPosition = Vector2.zero;
    }

    private static void CreateGrip(RectTransform decor, Vector2 anchor, Vector2 position)
    {
        GameObject root = new GameObject("Grip", typeof(RectTransform));
        root.layer = decor.gameObject.layer;
        RectTransform grip = (RectTransform)root.transform;
        grip.SetParent(decor, false);
        grip.anchorMin = anchor;
        grip.anchorMax = anchor;
        grip.pivot = new Vector2(0.5f, 0.5f);
        grip.sizeDelta = new Vector2(16f, 26f);
        grip.anchoredPosition = position;

        // Two columns of three dots: the usual "you can drag this" mark.
        for (int column = 0; column < 2; column++)
        for (int row = 0; row < 3; row++)
        {
            Image dot = NewImage(grip, "Dot", GripInk);
            RectTransform rect = dot.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(5f, 5f);
            rect.anchoredPosition = new Vector2(column == 0 ? -4f : 4f, (row - 1) * 9f);
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
}
