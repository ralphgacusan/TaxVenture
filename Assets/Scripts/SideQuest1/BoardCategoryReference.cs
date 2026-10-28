using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The corkboard's category reference (Campaign Foundation T26): a small
/// CATEGORIES control in the corner of the board view that opens the four
/// formal category names, each with one neutral sentence
/// (CorkboardTeaching.Categories, from the board's own two distinctions).
///
/// It explains vocabulary only: it never names a client, an answer-key
/// assignment or a destination for a held profile, and never changes with
/// the draft. Available before and after the first check.
///
/// INPUT: it never swallows a drag in progress or a pending automatic check
/// (the control does nothing then: CorkboardSideQuestManager.ReferenceMayOpen).
/// While open, a backdrop takes every touch, so neither a card nor the world
/// behind it can be touched; CLOSE, a tap on the backdrop or the control
/// closes it without moving a card or checking the board. A conversation,
/// Settings or a result card closes it. Nothing here is saved or rewarded.
/// </summary>
public sealed class BoardCategoryReference : MonoBehaviour
{
    public const string ControlName = "Board Categories";
    public const string CloseName = "Board Categories Close";

    private static readonly Color Paper = new Color32(246, 240, 224, 255);
    private static readonly Color Ink = new Color32(46, 40, 34, 255);
    private static readonly Color LabelInk = new Color32(110, 78, 30, 255);

    private CorkboardSideQuestManager board;
    private RectTransform panel;
    private RectTransform backdrop;
    private CanvasGroup panelGroup, backdropGroup;
    private const float OpenSeconds = 0.26f, CloseSeconds = 0.18f;
    private bool targetOpen, animating;
    private float motionTime, fromAlpha;
    private Vector3 fromScale;

    /// <summary>True through opening, display and closing, so board input stays blocked.</summary>
    public bool IsOpen => panel != null && panel.gameObject.activeSelf;

    public static BoardCategoryReference Create(CorkboardSideQuestManager board)
    {
        var host = new GameObject("Board Category Reference", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(host, board.gameObject.scene);
        host.layer = LayerMask.NameToLayer("UI");
        var canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;   // above the board's feedback line (30), under the guide and Settings
        var scaler = host.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        host.AddComponent<GraphicRaycaster>();
        var reference = host.AddComponent<BoardCategoryReference>();
        reference.board = board;
        reference.Build((RectTransform)host.transform);
        return reference;
    }

    // =========================================================
    // BUILD
    // =========================================================

    private void Build(RectTransform root)
    {
        // The corner control: lower right, clear of the tray (left) and the board line (bottom centre).
        RectTransform control = HudTheme.CreatePanel(ControlName, root, 18f);
        RectTransform frame = (RectTransform)control.parent;
        frame.anchorMin = frame.anchorMax = new Vector2(1f, 0f);
        frame.pivot = new Vector2(1f, 0f);
        frame.anchoredPosition = new Vector2(-28f, 132f);
        frame.sizeDelta = new Vector2(268f, 76f);
        Image hit = frame.GetComponent<Image>();
        hit.raycastTarget = true;
        Button button = frame.gameObject.AddComponent<Button>();
        button.targetGraphic = hit;
        button.onClick.AddListener(Toggle);
        RectTransform info = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Info", control), 14f, 13f, 46f, 46f);
        HudTheme.AddImage(info, HudTheme.Circle, HudTheme.Brass);
        TextMeshProUGUI i = HudTheme.CreateLabel("Info Mark", info, "i", 32f, HudTheme.PanelInner, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(i.rectTransform);
        TextMeshProUGUI label = HudTheme.CreateLabel("Label", control, "CATEGORIES", 28f, HudTheme.TextPrimary, TextAlignmentOptions.Left, FontStyles.Bold);
        HudTheme.PlaceTopLeft(label.rectTransform, 72f, 12f, 180f, 48f);
        label.characterSpacing = 3f;

        // While open: a backdrop takes every touch (a tap on it closes), then the paper with the four meanings.
        backdrop = HudTheme.CreateRect("Board Categories Backdrop", root);
        HudTheme.Stretch(backdrop);
        Image shade = HudTheme.AddImage(backdrop, null, new Color(0.02f, 0.035f, 0.07f, 0.45f));
        shade.raycastTarget = true;
        Button outside = backdrop.gameObject.AddComponent<Button>();
        outside.transition = Selectable.Transition.None;
        outside.targetGraphic = shade;
        outside.onClick.AddListener(Close);
        backdrop.gameObject.SetActive(false);

        RectTransform paperFrame = HudTheme.CreateRect("Board Categories Paper", root);
        paperFrame.anchorMin = paperFrame.anchorMax = new Vector2(0.5f, 0.5f);
        paperFrame.pivot = new Vector2(0.5f, 0.5f);
        paperFrame.sizeDelta = new Vector2(1040f, 760f);
        Image paperEdge = HudTheme.AddImage(paperFrame, HudTheme.Rounded, HudTheme.Brass, 22f);
        paperEdge.raycastTarget = true;   // touches on the paper never reach the backdrop or the board
        RectTransform paper = HudTheme.CreateRect("Paper", paperFrame);
        HudTheme.Stretch(paper, 4f);
        HudTheme.AddImage(paper, HudTheme.Rounded, Paper, 18f);

        TextMeshProUGUI title = HudTheme.CreateLabel("Title", paper, "BOARD CATEGORIES", 40f, Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        HudTheme.PlaceTopLeft(title.rectTransform, 48f, 30f, 700f, 56f);
        title.characterSpacing = 4f;
        RectTransform close = HudTheme.CreatePanel(CloseName, paper, 16f);
        RectTransform closeFrame = (RectTransform)close.parent;
        HudTheme.PlaceTopLeft(closeFrame, 1032f - 48f - 200f, 26f, 200f, 64f);
        Image closeHit = closeFrame.GetComponent<Image>();
        closeHit.raycastTarget = true;
        Button closeButton = closeFrame.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeHit;
        closeButton.onClick.AddListener(Close);
        TextMeshProUGUI closeLabel = HudTheme.CreateLabel("Label", close, "CLOSE", 28f, HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(closeLabel.rectTransform);
        RectTransform rule = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Rule", paper), 48f, 104f, 936f, 3f);
        HudTheme.AddImage(rule, null, new Color(HudTheme.Brass.r, HudTheme.Brass.g, HudTheme.Brass.b, 0.6f));

        // The four corners, always in the same order: formal name, then one sentence.
        float y = 130f;
        foreach (CorkboardTeaching.CategoryMeaning category in CorkboardTeaching.Categories)
        {
            TextMeshProUGUI name = HudTheme.CreateLabel("Name " + category.Category, paper, category.Name.ToUpperInvariant(), 30f, LabelInk,
                TextAlignmentOptions.Left, FontStyles.Bold);
            HudTheme.PlaceTopLeft(name.rectTransform, 48f, y, 936f, 42f);
            name.characterSpacing = 3f;
            TextMeshProUGUI meaning = HudTheme.CreateLabel("Meaning " + category.Category, paper, category.Meaning, 34f, Ink, TextAlignmentOptions.TopLeft);
            HudTheme.PlaceTopLeft(meaning.rectTransform, 48f, y + 44f, 936f, 90f);
            meaning.textWrappingMode = TextWrappingModes.Normal;
            y += 150f;
        }
        panel = paperFrame;
        panelGroup = panel.gameObject.AddComponent<CanvasGroup>();
        backdropGroup = backdrop.gameObject.AddComponent<CanvasGroup>();
        FinishClose();
    }

    // =========================================================
    // OPEN / CLOSE (never moves a card, never checks the board)
    // =========================================================

    public void Toggle()
    {
        if (targetOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (targetOpen || board == null || !board.ReferenceMayOpen) return;
        if (!IsOpen)
        {
            panel.localScale = Vector3.one * 0.9f;
            panelGroup.alpha = backdropGroup.alpha = 0f;
        }
        backdrop.gameObject.SetActive(true);
        panel.gameObject.SetActive(true);
        BeginMotion(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_paper_pickup_01", 0.45f);
    }

    public void Close()
    {
        if (!IsOpen || !targetOpen) return;
        // Keep the backdrop and IsOpen until the exit finishes: no touch-through.
        BeginMotion(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_paper_place_01", 0.4f);
    }

    private void BeginMotion(bool opening)
    {
        targetOpen = opening;
        animating = true;
        motionTime = 0f;
        fromAlpha = panelGroup.alpha;
        fromScale = panel.localScale;
        panelGroup.interactable = opening;
        panelGroup.blocksRaycasts = backdropGroup.blocksRaycasts = true;
    }

    private void LateUpdate()
    {
        if (IsOpen && (board == null || !board.CanInteract)) { FinishClose(); return; }
        if (IsOpen && (DialogueUI.IsAnyDialogueActive || InGameSettings.IsOpen || HudRewardScreen.AnyOpen)) Close();
        if (!animating) return;
        motionTime += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(motionTime / (targetOpen ? OpenSeconds : CloseSeconds));
        float fade = t * t * (3f - 2f * t);
        panelGroup.alpha = backdropGroup.alpha = Mathf.Lerp(fromAlpha, targetOpen ? 1f : 0f, fade);
        float pop = targetOpen ? HudTheme.EaseOutBack(t) : t * t;
        panel.localScale = Vector3.LerpUnclamped(fromScale, Vector3.one * (targetOpen ? 1f : 0.9f), pop);
        if (t < 1f) return;
        animating = false;
        if (!targetOpen) FinishClose();
        else
        {
            panel.localScale = Vector3.one;
            panelGroup.alpha = backdropGroup.alpha = 1f;
        }
    }

    private void FinishClose()
    {
        targetOpen = animating = false;
        if (panel != null)
        {
            panel.localScale = Vector3.one;
            panel.gameObject.SetActive(false);
        }
        if (backdrop != null) backdrop.gameObject.SetActive(false);
    }

    private void OnDisable() => FinishClose();
}
