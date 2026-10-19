using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// A printed stamp impression on a world-space paper: a slightly tilted red
/// box with capital ink text (e.g. "INITIAL REVIEW / NOT READY"). Created
/// once per paper as a child named "ReviewInk", shown or hidden on demand.
/// Presentation only; ignores raycasts so rows and page buttons stay usable.
/// </summary>         
public static class ReviewInkMark
{
    public const string ObjectName = "ReviewInk";

    /// <summary>Size of one impression in paper units (the desk stamp's pad is built to match it).</summary>
    public static readonly Vector2 Size = new Vector2(440f, 160f);
    private static readonly Color Ink = new Color(0.72f, 0.12f, 0.14f, 0.88f);

    /// <summary>Not Ready ink.</summary>
    public static readonly Color InkRed = Ink;
    /// <summary>Ready ink.</summary>
    public static readonly Color InkGreen = new Color(0.12f, 0.52f, 0.28f, 0.88f);

    /// <summary>When true, the next CreateAt mark pops in (a freshly pressed stamp). Consumed once.</summary>
    public static bool PopNext;

    /// <summary>Recolours a mark (frame, outline, text) and sets how see-through it is (1 = solid ink).</summary>
    public static void SetStyle(GameObject mark, Color color, float alpha)
    {
        if (mark == null) return;
        var c = new Color(color.r, color.g, color.b, color.a * alpha);
        Image frame = mark.GetComponent<Image>();
        if (frame != null) frame.color = new Color(color.r, color.g, color.b, 0.06f * alpha);
        Outline edge = mark.GetComponent<Outline>();
        if (edge != null) edge.effectColor = c;
        TextMeshProUGUI text = mark.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null) text.color = c;
    }

    /// <summary>Shows <paramref name="text"/> as ink on the paper, or hides it when text is empty.</summary>
    public static GameObject Show(RectTransform paper, string text)
    {
        if (paper == null) return null;
        Transform existing = paper.Find(ObjectName);
        if (string.IsNullOrEmpty(text))
        {
            if (existing != null) existing.gameObject.SetActive(false);
            return existing != null ? existing.gameObject : null;
        }

        GameObject mark = existing != null ? existing.gameObject : Create(paper);
        mark.SetActive(true);
        mark.GetComponentInChildren<TextMeshProUGUI>(true).text = text;
        mark.transform.SetAsLastSibling();
        return mark;
    }

    /// <summary>A new mark on the paper that its caller owns (e.g. removed when a folder page changes).</summary>
    public static GameObject CreateOn(RectTransform paper, string text)
    {
        if (paper == null) return null;
        GameObject mark = Create(paper);
        mark.name = ObjectName + "Page";
        mark.GetComponentInChildren<TextMeshProUGUI>(true).text = text;
        mark.transform.SetAsLastSibling();
        return mark;
    }

    /// <summary>
    /// A new mark centred on a point of the paper (paper-local units, as from
    /// InverseTransformPoint), tilted by <paramref name="angle"/> degrees.
    /// </summary>
    public static GameObject CreateAt(RectTransform paper, string text, Vector2 localPoint, float angle)
    {
        GameObject mark = CreateOn(paper, text);
        if (mark == null) return null;
        var rect = (RectTransform)mark.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = localPoint - paper.rect.center;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        if (PopNext)
        {
            PopNext = false;
            ReviewInkMotion.Play(mark, ReviewInkMotion.Mode.Pop);
        }
        return mark;
    }

    private static GameObject Create(RectTransform paper)
    {
        var go = new GameObject(ObjectName, typeof(RectTransform));
        go.layer = paper.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(paper, false);
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-70f, -60f);
        rect.sizeDelta = Size;
        rect.localRotation = Quaternion.Euler(0f, 0f, -9f);

        Image frame = go.AddComponent<Image>();
        frame.sprite = HudTheme.Rounded;
        frame.type = Image.Type.Sliced;
        frame.color = new Color(Ink.r, Ink.g, Ink.b, 0.06f);
        frame.raycastTarget = false;
        Outline edge = go.AddComponent<Outline>();
        edge.effectColor = Ink;
        edge.effectDistance = new Vector2(6f, -6f);

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.layer = go.layer;
        var textRect = (RectTransform)textGo.transform;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 6f);
        textRect.offsetMax = new Vector2(-10f, -6f);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.font = HudTheme.Font;
        text.fontStyle = FontStyles.Bold;
        text.color = Ink;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 64f;
        text.characterSpacing = 4f;
        text.raycastTarget = false;
        return go;
    }
}
