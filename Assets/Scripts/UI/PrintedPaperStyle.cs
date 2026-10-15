using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presentation-only "printed document" treatment for a world-space paper:
/// a soft drop shadow, two slightly offset sheets underneath (siblings, so
/// they stay put while the top page flips), a thin printed border, and
/// ink-coloured text instead of pure black. Applied once at runtime from the
/// document UIs, so no scene edit is required. Every decoration ignores
/// raycasts, so value rows, chips and page-edge zones stay clickable.
/// </summary>
[DisallowMultipleComponent]
public sealed class PrintedPaperStyle : MonoBehaviour
{
    private static readonly Color InkColor = new Color(0.12f, 0.13f, 0.17f, 1f);
    private static readonly Color BorderColor = new Color(0.2f, 0.22f, 0.28f, 0.35f);
    private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.28f);

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

    private static bool IsPureBlack(Color color) =>
        color.r < 0.01f && color.g < 0.01f && color.b < 0.01f;
}
