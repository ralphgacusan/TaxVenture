using System.Collections.Generic;
using TMPro;
using UnityEngine.TextCore;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Turns the existing Notes panel into a paper note page: cream paper with
/// tape, a red margin and ruled lines, the main objectives with large
/// checkboxes and their steps with small checkboxes and drawn checkmarks.
/// Done steps are struck through, the current step is highlighted, and
/// steps with a live counter show it (e.g. "(2/4)").
///
/// Built at runtime on the scene's NotesPanel by OfficeHudRuntime (no scene
/// edits). The original Notes text objects are hidden, not deleted, and are
/// restored if this view is destroyed.
///
/// DATA: renders OfficeObjectives' snapshot — the same one the HUD tracker
/// shows — so the paper and the tracker always agree.
/// </summary>
public sealed class NotesPaperView : MonoBehaviour
{
    private static readonly Color Paper = new Color32(246, 238, 216, 255);
    private static readonly Color Ink = new Color32(38, 44, 70, 255);
    private static readonly Color InkFaded = new Color32(38, 44, 70, 130);
    private static readonly Color CheckInk = new Color32(40, 128, 78, 255);
    private static readonly Color Highlighter = new Color32(255, 222, 84, 150);
    private static readonly Color RuledLine = new Color32(150, 180, 214, 110);
    private static readonly Color MarginLine = new Color32(222, 110, 110, 150);
    private static readonly Color Tape = new Color32(236, 222, 170, 215);

    private const float ContentLeft = 140f;
    private const float ContentTop = 190f;
    private const float RightPadding = 60f;
    private const float BottomPadding = 80f;
    private const float LineSpacing = 56f;

    private NotesPanelUI notes;
    private OfficeObjectives objectives;
    private RectTransform panel;
    private RectTransform content;
    private TextMeshProUGUI subtitle;
    private RectTransform chapterNavigation;
    private TextMeshProUGUI chapterLabel;
    private Button previousChapter, nextChapter;
    private readonly List<int> chapterStarts = new List<int>();
    private int shownChapter = -1;
    private int lastActiveChapter = -1;

    private Image panelImage;
    private Sprite originalSprite;
    private Color originalColor;
    private Image.Type originalType;
    private float originalPixelsPerUnit;
    private readonly List<GameObject> hiddenOriginals = new List<GameObject>();

    // =========================================================
    // BUILD
    // =========================================================

    public static NotesPaperView Attach(NotesPanelUI notes, OfficeObjectives objectives)
    {
        var panel = notes.transform as RectTransform;
        if (panel == null || objectives == null) return null;

        RectTransform root = HudTheme.CreateRect("Notes Paper", panel);
        HudTheme.Stretch(root);
        var view = root.gameObject.AddComponent<NotesPaperView>();
        view.notes = notes;
        view.objectives = objectives;
        view.panel = panel;
        view.Build(root);
        if (view.isActiveAndEnabled)
        {
            objectives.Changed += view.Render;
            view.Render();
        }
        return view;
    }

    private void Build(RectTransform root)
    {
        // Hide the old plain text children (kept for restore).
        foreach (Transform child in panel)
        {
            if (child == root || !child.gameObject.activeSelf) continue;
            if (child.GetComponent<TMP_Text>() == null) continue;
            child.gameObject.SetActive(false);
            hiddenOriginals.Add(child.gameObject);
        }

        // The panel's own Image stays as the invisible hit area (dragging,
        // tap-to-front); the paper itself is drawn as a small stack of sheets.
        panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
        {
            originalSprite = panelImage.sprite;
            originalColor = panelImage.color;
            originalType = panelImage.type;
            originalPixelsPerUnit = panelImage.pixelsPerUnitMultiplier;
            panelImage.color = new Color(1f, 1f, 1f, 0f);
        }

        float width = panel.rect.width > 0f ? panel.rect.width : 900f;
        float height = panel.rect.height > 0f ? panel.rect.height : 950f;

        BuildPaperStack(root, PaperBack, Paper);

        // Ruled lines and the red margin.
        RectTransform lines = HudTheme.CreateRect("Ruled Lines", root);
        HudTheme.Stretch(lines);
        for (float y = ContentTop + LineSpacing; y < height - 40f; y += LineSpacing)
        {
            RectTransform line = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Line", lines), 36f, y, width - 72f, 2f);
            HudTheme.AddImage(line, null, RuledLine);
        }
        RectTransform margin = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Margin", lines), 112f, 0f, 3f, height);
        HudTheme.AddImage(margin, null, MarginLine);

        // Tape strip at the top.
        RectTransform tape = HudTheme.CreateRect("Tape", root);
        tape.anchorMin = tape.anchorMax = new Vector2(0.5f, 1f);
        tape.pivot = new Vector2(0.5f, 0.5f);
        tape.anchoredPosition = new Vector2(0f, -4f);
        tape.sizeDelta = new Vector2(210f, 50f);
        tape.localRotation = Quaternion.Euler(0f, 0f, -3f);
        HudTheme.AddImage(tape, null, Tape);

        // Title, subtitle and close button.
        TextMeshProUGUI title = InkLabel("Title", root, "Objectives", 58f, Ink, FontStyles.Bold);
        HudTheme.PlaceTopLeft((RectTransform)title.transform, ContentLeft, 44f, 520f, 72f);
        subtitle = InkLabel("Subtitle", root, string.Empty, 26f, InkFaded, FontStyles.Normal);
        HudTheme.PlaceTopLeft((RectTransform)subtitle.transform, ContentLeft, 116f, 560f, 36f);
        BuildCloseButton(root, width);

        // Footer hint.
        TextMeshProUGUI hint = InkLabel("Hint", root, "Tap X or the notes icon to close", 22f, InkFaded, FontStyles.Italic);
        hint.alignment = TextAlignmentOptions.Center;
        HudTheme.PlaceTopLeft((RectTransform)hint.transform, 0f, height - 52f, width, 32f);

        chapterNavigation = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Chapter navigation", root),
            ContentLeft, height - 115f, width - ContentLeft - RightPadding, 54f);
        previousChapter = ChapterButton(chapterNavigation, "Previous", "<", 0f, -1);
        nextChapter = ChapterButton(chapterNavigation, "Next", ">", chapterNavigation.sizeDelta.x - 64f, 1);
        chapterLabel = InkLabel("Chapter", chapterNavigation, "", 26f, Ink, FontStyles.Bold);
        chapterLabel.alignment = TextAlignmentOptions.Center;
        HudTheme.PlaceTopLeft(chapterLabel.rectTransform, 70f, 8f, chapterNavigation.sizeDelta.x - 140f, 40f);
        chapterNavigation.gameObject.SetActive(false);

        content = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Objectives", root), ContentLeft, ContentTop,
            width - ContentLeft - RightPadding, height - ContentTop - BottomPadding);
    }

    private Button ChapterButton(RectTransform parent, string name, string caption, float x, int direction)
    {
        RectTransform rect = HudTheme.PlaceTopLeft(HudTheme.CreateRect(name, parent), x, 0f, 64f, 54f);
        Image face = HudTheme.AddImage(rect, HudTheme.Rounded, new Color32(226, 214, 186, 255), 12f);
        face.raycastTarget = true;
        TextMeshProUGUI text = InkLabel("Arrow", rect, caption, 34f, Ink, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        HudTheme.Stretch(text.rectTransform);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.onClick.AddListener(() =>
        {
            shownChapter = Mathf.Clamp(shownChapter + direction, 0, chapterStarts.Count - 1);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
            Render();
        });
        return button;
    }

    private static readonly Color PaperBack = new Color32(226, 214, 186, 255);
    private static readonly Color PaperEdge = new Color32(196, 182, 152, 255);

    /// <summary>
    /// Two sheets peeking out underneath, a darker edge for thickness, and
    /// the front sheet with a soft shadow, so the page reads as real paper.
    /// Shared by the Notes paper and the taxpayer profile cards.
    /// </summary>
    public static Image BuildPaperStack(RectTransform root, Color back, Color front, float unit = 1f)
    {
        // Just two pages: one sheet peeking out underneath and a thin edge
        // for thickness (kept subtle on purpose).
        float r = 8f * Mathf.Max(0.25f, unit);
        AddSheet(root, "Sheet Back", new Vector2(6f, -7f) * unit, 1.2f, back, r);
        AddSheet(root, "Sheet Edge", new Vector2(2f, -3f) * unit, 0f, PaperEdge, r);
        Image sheet = AddSheet(root, "Sheet", Vector2.zero, 0f, front, r);
        var shadow = sheet.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        shadow.effectDistance = new Vector2(5f, -6f) * unit;
        return sheet;
    }

    private static Image AddSheet(RectTransform root, string name, Vector2 offset, float tilt, Color color, float radius)
    {
        RectTransform sheet = HudTheme.CreateRect(name, root);
        HudTheme.Stretch(sheet);
        sheet.offsetMin += offset;
        sheet.offsetMax += offset;
        sheet.localRotation = Quaternion.Euler(0f, 0f, tilt);
        return HudTheme.AddImage(sheet, HudTheme.Rounded, color, radius);
    }

    private void BuildCloseButton(RectTransform root, float width)
    {
        RectTransform close = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Close", root), width - 40f - 64f, 40f, 64f, 64f);
        Image ring = HudTheme.AddImage(close, HudTheme.Circle, Ink);
        ring.raycastTarget = true;
        RectTransform inner = HudTheme.CreateRect("Inner", close);
        HudTheme.Stretch(inner, 4f);
        HudTheme.AddImage(inner, HudTheme.Circle, Paper);
        TextMeshProUGUI x = InkLabel("X", close, "X", 34f, Ink, FontStyles.Bold);
        x.alignment = TextAlignmentOptions.Center;
        HudTheme.Stretch((RectTransform)x.transform);

        var button = close.gameObject.AddComponent<Button>();
        button.targetGraphic = ring;
        button.onClick.AddListener(() => notes.Hide());
    }

    private static TextMeshProUGUI InkLabel(string name, Transform parent, string text, float size, Color color,
        FontStyles style)
    {
        TextMeshProUGUI label = HudTheme.CreateLabel(name, parent, text, size, color, TextAlignmentOptions.TopLeft, style);
        label.font = HudTheme.HandwritingFont;
        label.enableAutoSizing = false;
        label.overflowMode = TextOverflowModes.Overflow;
        return label;
    }

    // =========================================================
    // RENDER
    // =========================================================

    private void OnEnable()
    {
        if (objectives != null) objectives.Changed += Render;
        Render();
    }

    private void OnDisable()
    {
        if (objectives != null) objectives.Changed -= Render;
    }

    public void Render()
    {
        if (content == null || objectives == null || objectives.Snapshot == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            content.GetChild(i).gameObject.SetActive(false);
            Destroy(content.GetChild(i).gameObject);
        }

        CaseProgressionManager manager = CaseProgressionManager.Instance;
        string levelName = manager != null ? manager.CurrentLevelName : null;
        int levelNumber = HudProgressionPanel.ParseLevelNumber(manager != null ? manager.CurrentLevelId : null);
        TaskProgressSnapshot snapshot = objectives.Snapshot;
        subtitle.text = levelNumber > 0
            ? $"Level {levelNumber}{(string.IsNullOrEmpty(levelName) ? string.Empty : " - " + levelName)}   |   {snapshot.DoneCount} of {snapshot.StepCount} done"
            : $"{snapshot.DoneCount} of {snapshot.StepCount} done";

        IReadOnlyList<TaskDefinition> tasks = objectives.Tasks;
        bool chapters = manager != null && manager.CurrentLevelId == OfficeLevelCatalog.Level2Id;
        chapterStarts.Clear();
        if (chapters)
            for (int i = 0; i < tasks.Count; i++)
                if (tasks[i] != null && tasks[i].IsHeader) chapterStarts.Add(i);
        chapters = chapterStarts.Count > 1;
        chapterNavigation.gameObject.SetActive(chapters);
        int first = 0, end = tasks.Count;
        if (chapters)
        {
            int active = 0;
            for (int i = 0; i < chapterStarts.Count; i++)
                if (chapterStarts[i] <= (snapshot.ActiveIndex >= 0 ? snapshot.ActiveIndex : tasks.Count - 1)) active = i;
            if (shownChapter < 0 || active != lastActiveChapter) shownChapter = active;
            lastActiveChapter = active;
            shownChapter = Mathf.Clamp(shownChapter, 0, chapterStarts.Count - 1);
            first = chapterStarts[shownChapter];
            end = shownChapter + 1 < chapterStarts.Count ? chapterStarts[shownChapter + 1] : tasks.Count;
            chapterLabel.text = $"{shownChapter + 1} / {chapterStarts.Count}";
            previousChapter.interactable = shownChapter > 0;
            nextChapter.interactable = shownChapter + 1 < chapterStarts.Count;
            subtitle.text = $"Level {levelNumber}   ·   {snapshot.DoneCount} of {snapshot.StepCount} done";
        }
        content.localScale = Vector3.one;
        float width = content.rect.width;
        float y = 0f;
        for (int i = first; i < end; i++)
        {
            TaskDefinition task = tasks[i];
            if (task == null) continue;
            TaskRowStatus status = snapshot.Status[i];
            if (task.IsHeader)
            {
                y += BuildHeaderRow(task, status, y, width);
            }
            else
            {
                y += BuildStepRow(task, status, y, width);
            }
        }

        // Shrink the list to fit the page if a long edited list overflows it.
        float available = content.rect.height;
        float scale = !chapters && y > available && y > 0f ? available / y : 1f;
        content.localScale = new Vector3(scale, scale, 1f);
    }

    private float BuildHeaderRow(TaskDefinition task, TaskRowStatus status, float y, float width)
    {
        const float size = 38f;
        bool done = status == TaskRowStatus.Done;
        BuildCheckbox(content, 0f, y + LineSpacing - BaselineGap - 40f, 40f, done);

        TextMeshProUGUI title = InkLabel("Objective", content, task.TaskDescription, size, Ink, FontStyles.Bold);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.enableAutoSizing = true;
        title.fontSizeMin = 24f;
        title.fontSizeMax = size;
        HudTheme.PlaceTopLeft((RectTransform)title.transform, 58f, y + TextTop(title, size), width - 58f - 150f, size * 1.3f);

        if (done)
        {
            TextMeshProUGUI stamp = InkLabel("Stamp", content, "DONE!", 30f, CheckInk, FontStyles.Bold);
            stamp.alignment = TextAlignmentOptions.Center;
            stamp.characterSpacing = 6f;
            RectTransform stampRect = HudTheme.PlaceTopLeft((RectTransform)stamp.transform, width - 140f, y + 4f, 130f, 44f);
            stampRect.localRotation = Quaternion.Euler(0f, 0f, 8f);
        }

        // Headings get a bold ink rule on top of the ruled line.
        RectTransform underline = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Underline", content), 58f,
            y + LineSpacing - 2f, width - 58f, 3f);
        HudTheme.AddImage(underline, null, done ? CheckInk : Ink);

        return LineSpacing;
    }

    private float BuildStepRow(TaskDefinition task, TaskRowStatus status, float y, float width)
    {
        const float size = 28f;
        const float indent = 40f;
        const float textX = indent + 46f;
        float textWidth = width - textX;

        string text = task.TaskDescription;
        if (objectives.TryGetProgress(task, out int current, out int total))
            text += $"  ({current}/{total})";

        bool done = status == TaskRowStatus.Done;
        bool active = status == TaskRowStatus.Active;

        TextMeshProUGUI label = InkLabel("Step", content, text, size, done ? InkFaded : Ink,
            done ? FontStyles.Strikethrough : active ? FontStyles.Bold : FontStyles.Normal);
        label.textWrappingMode = TextWrappingModes.Normal;
        // Wrapped lines advance exactly one ruled line.
        label.lineSpacing = LineSpacingFor(label, size);

        Vector2 preferred = label.GetPreferredValues(text, textWidth, 0f);
        // First line is one natural line tall; each extra wrapped line adds one ruled line.
        int lines = 1 + Mathf.Max(0, Mathf.RoundToInt((preferred.y - NaturalLineHeight(label, size)) / LineSpacing));
        float rowHeight = lines * LineSpacing;
        RectTransform labelRect = HudTheme.PlaceTopLeft((RectTransform)label.transform, textX, y + TextTop(label, size),
            textWidth, rowHeight);

        if (active)
        {
            // Highlighter swipe behind the current step.
            float lineWidth = Mathf.Min(textWidth, preferred.x) + 16f;
            RectTransform mark = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Highlight", content), textX - 8f, y + 10f,
                lineWidth, rowHeight - 14f);
            HudTheme.AddImage(mark, HudTheme.Rounded, Highlighter, 6f);
            mark.SetSiblingIndex(labelRect.GetSiblingIndex());
            mark.localRotation = Quaternion.Euler(0f, 0f, -0.6f);

            TextMeshProUGUI arrow = InkLabel("Arrow", content, ">", 30f, MarginLine, FontStyles.Bold);
            HudTheme.PlaceTopLeft((RectTransform)arrow.transform, indent - 30f, y + TextTop(arrow, 30f), 24f, 40f);
        }

        BuildCheckbox(content, indent, y + LineSpacing - BaselineGap - 30f, 30f, done);
        return rowHeight;
    }

    // =========================================================
    // NOTEBOOK METRICS (text sits on the ruled lines)
    // =========================================================

    /// <summary>Distance between a line's baseline and the ruled line below it.</summary>
    private const float BaselineGap = 9f;

    /// <summary>Top offset that puts the first baseline BaselineGap above the row's ruled line.</summary>
    private static float TextTop(TMP_Text label, float size)
    {
        FaceInfo face = label.font != null ? label.font.faceInfo : default;
        float ascent = face.pointSize > 0f ? face.ascentLine / face.pointSize * size : size * 0.8f;
        return LineSpacing - BaselineGap - ascent;
    }

    private static float NaturalLineHeight(TMP_Text label, float size)
    {
        FaceInfo face = label.font != null ? label.font.faceInfo : default;
        return face.pointSize > 0f ? face.lineHeight / face.pointSize * size : size * 1.2f;
    }

    /// <summary>TMP lineSpacing (1/100 em) that makes wrapped lines advance one ruled line.</summary>
    private static float LineSpacingFor(TMP_Text label, float size)
        => (LineSpacing - NaturalLineHeight(label, size)) / size * 100f;

    private static void BuildCheckbox(RectTransform parent, float x, float y, float size, bool done)
    {
        RectTransform box = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Checkbox", parent), x, y, size, size);
        HudTheme.AddImage(box, HudTheme.Rounded, Ink, size * 0.18f);
        RectTransform inner = HudTheme.CreateRect("Inner", box);
        HudTheme.Stretch(inner, 3f);
        HudTheme.AddImage(inner, HudTheme.Rounded, Paper, size * 0.14f);

        if (!done) return;
        // Hand-drawn style: the check overshoots the box a little.
        RectTransform check = HudTheme.CreateRect("Check", box);
        check.anchorMin = Vector2.zero;
        check.anchorMax = Vector2.one;
        check.offsetMin = new Vector2(-2f, -2f);
        check.offsetMax = new Vector2(size * 0.35f, size * 0.3f);
        HudTheme.AddImage(check, HudTheme.Check, CheckInk);
    }

    private void OnDestroy()
    {
        if (panelImage != null)
        {
            panelImage.sprite = originalSprite;
            panelImage.color = originalColor;
            panelImage.type = originalType;
            panelImage.pixelsPerUnitMultiplier = originalPixelsPerUnit;
        }
        // The hidden originals are not re-activated here: this view only goes
        // away with its scene, and activating objects that are being destroyed
        // is an error.
    }
}
