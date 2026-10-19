using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Level 2's four saved annotation papers. Each challenge entry is its own
/// FloatingWindow, built from the same AnnotationPaper definition and saved
/// work used at the market desk, showing only its annotated front (highlight
/// and attached cards). They open in a single row across the screen (slight
/// overlap on narrow screens); each is a normal paper window: tap to bring it
/// to the front, drag it anywhere. During the office review they are the
/// reference and the value source for the Reviewed Schedule.
/// </summary>
public sealed class Level2AnnotatedPapers : MonoBehaviour
{
    public const string ChildName = "AnnotatedPapers";
    private const string IconName = "AnnotatedPapersIcon";
    /// <summary>Fourth slot of the right-side column: below Notes, Case Folder and the Tax Code Book.</summary>
    public static readonly Vector2 IconPosition = new Vector2(10f, -440f);
    private const float CollectionPreviewSeconds = 2.2f;
    private const float PaperWidth = 462f;
    private const float PaperHeight = 539f;
    private const float ScreenInset = 28f;
    private const float RowLift = 30f;
    private const float PaperGap = 28f;
    private static readonly float[] Tilts = { -1.6f, 1.4f, 1.9f, -1.2f };

    public static Level2AnnotatedPapers Instance { get; private set; }
    public bool IsCollecting { get; private set; }

    private Canvas hudCanvas;
    private RectTransform icon;
    private bool ownsIcon;
    private HudIconDragOpen iconDrag;
    private HudDragItem dragVisual;
    private TextMeshProUGUI countText;
    private RectTransform overlay;
    private RectTransform blocker;
    private RectTransform paperLayer;
    private Vector2 paperSize;
    private float rowWidth;
    // Retained from PresentAwarded's existing callback; the four full faces use the saved entry state below.
    private readonly List<string> rows = new List<string>();
    private readonly List<FloatingWindow> papers = new List<FloatingWindow>();
    private readonly List<AnnotationPaperCollectionFace> faces = new List<AnnotationPaperCollectionFace>();
    private readonly List<Vector2> slots = new List<Vector2>();
    private bool holdingWindows;
    private bool hasAwardedState;
    private bool collectionPresented;
    private bool papersBuilt;
    private string builtReviewId;
    private int builtEntryCount;

    private void OnEnable()
    {
        Instance = this;
        GameplayEvents.OnIncomeQuestCompleted += HandleIncomeQuestCompleted;
        InitializeHud();
        RestoreAwardedRows();
    }

    private void Start() => RestoreAwardedRows();

    private void Update()
    {
        if (icon == null || hudCanvas == null)
        {
            InitializeHud();
            UpdateCount();
        }
        if (!hasAwardedState || !papersBuilt) RestoreAwardedRows();
        if (IsCollecting) PlayerControlHold.Refresh();
        if (holdingWindows && !AnyPaperOpen()) ReleaseWindowHold();
    }

    private void OnDisable()
    {
        GameplayEvents.OnIncomeQuestCompleted -= HandleIncomeQuestCompleted;
        StopAllCoroutines();
        IsCollecting = false;
        PlayerControlHold.Release(this);
        ReleaseWindowHold();
        CleanupRuntimeHud();
        if (Instance == this) Instance = null;
    }

    private void OnDestroy()
    {
        PlayerControlHold.Release(this);
        ReleaseWindowHold();
        CleanupRuntimeHud();
    }

    /// <summary>Called after the reward card closes; only a new award is shown automatically.</summary>
    public void PresentAwarded(List<string> annotatedRows)
    {
        if (!isActiveAndEnabled || collectionPresented || IsCollecting) return;
        SetRows(annotatedRows);
        if (!hasAwardedState) RestoreAwardedRows();
        CaseReviewState review = Level2OfficeStart.Review;
        if (review?.challenge == null || !review.challenge.awarded || papers.Count != 4) return;

        if (!InitializeHud()) return;
        EnsureViewer();
        collectionPresented = true;
        StartCoroutine(CollectPapers());
    }

    private void HandleIncomeQuestCompleted() => RestoreAwardedRows();

    private void RestoreAwardedRows()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        if (review?.challenge == null || !review.challenge.awarded) return;
        AnnotationDefinition annotation = CurrentAnnotation();
        if (annotation?.entries == null || annotation.entries.Count == 0) return;

        if (!InitializeHud()) return;
        EnsureViewer();
        if (overlay == null) return;
        SetRows(IncomeChallenge.AnnotatedList(review.challenge, annotation));
        hasAwardedState = true;
        if (!papersBuilt || builtReviewId != review.reviewId || builtEntryCount != annotation.entries.Count)
            BuildPapers(review, annotation);
        UpdateCount();
    }

    private AnnotationDefinition CurrentAnnotation() =>
        CaseManager.Instance != null && CaseManager.Instance.CurrentDefinition != null
            ? CaseManager.Instance.CurrentDefinition.review?.annotation
            : null;

    private void SetRows(List<string> source)
    {
        rows.Clear();
        if (source == null) return;
        for (int i = 0; i < source.Count; i++)
            if (!string.IsNullOrWhiteSpace(source[i])) rows.Add(source[i]);
    }

    private bool InitializeHud()
    {
        if (icon != null && hudCanvas != null) return true;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null || canvases[i].gameObject.scene != gameObject.scene || canvases[i].name != "HUD_Canvas") continue;
            hudCanvas = canvases[i];
            break;
        }
        if (hudCanvas == null) return false;

        Transform buttons = hudCanvas.transform.Find("PermanentHUD/IconButtons");
        if (buttons == null) return false;
        Transform existing = buttons.Find(IconName);
        if (existing != null) icon = existing as RectTransform;
        if (icon == null)
        {
            var iconObject = new GameObject(IconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            iconObject.layer = buttons.gameObject.layer;
            icon = (RectTransform)iconObject.transform;
            icon.SetParent(buttons, false);
            icon.anchorMin = icon.anchorMax = new Vector2(1f, 1f);
            icon.pivot = new Vector2(1f, 1f);
            icon.sizeDelta = new Vector2(120f, 120f);
            icon.anchoredPosition = IconPosition;
            icon.localScale = Vector3.one;
            ownsIcon = true;
        }

        icon.anchoredPosition = IconPosition;
        Image image = icon.GetComponent<Image>();
        if (image == null) image = icon.gameObject.AddComponent<Image>();
        image.sprite = HudIconArt.Get("papers");
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = true;

        Button button = icon.GetComponent<Button>();
        if (button == null) button = icon.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.RemoveListener(ToggleViewer);
        button.onClick.AddListener(ToggleViewer);

        Transform count = icon.Find("CountText");
        if (count == null)
        {
            TextMeshProUGUI label = HudTheme.CreateLabel("CountText", icon, "0", 22f, HudTheme.TextPrimary,
                TextAlignmentOptions.Center, FontStyles.Bold);
            countText = label;
            RectTransform r = label.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(1f, 1f);
            r.sizeDelta = new Vector2(42f, 32f);
            r.anchoredPosition = new Vector2(-8f, -12f);
        }
        else countText = count.GetComponent<TextMeshProUGUI>();

        if (countText != null && icon.GetComponent<HudIconBadge>() == null)
            HudIconBadge.Attach(icon, countText);

        iconDrag = icon.GetComponent<HudIconDragOpen>();
        if (iconDrag == null) iconDrag = icon.gameObject.AddComponent<HudIconDragOpen>();
        if (iconDrag.Visual == null)
        {
            dragVisual = HudDragItem.Create(hudCanvas.transform, "Annotated Papers", icon,
                new Vector2(280f, 350f), -1.5f, AnnotationPaperCollectionFace.BuildStack);
            iconDrag.Visual = dragVisual;
        }
        else dragVisual = iconDrag.Visual as HudDragItem;
        iconDrag.CanDrag = CanOpenFromIcon;
        iconDrag.OpenAt = OpenFromDrop;
        if (button != null) button.interactable = hasAwardedState && papers.Count == 4;
        return true;
    }

    private bool CanOpenFromIcon()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        GuideDirector guide = GuideRuntime.Instance != null ? GuideRuntime.Instance.Director : null;
        return CanToggleIcon() && !AnyPaperOpen() &&
               !SideQuestSplash.IsShowing && Time.timeScale > 0f && !IncomeToolkit.TeachingPaused &&
               !HudRewardScreen.AnyOpen && (guide == null || !guide.IsBlocking) &&
               (review?.clock == null || !review.clock.cutoffPending);
    }

    private bool CanToggleIcon() => isActiveAndEnabled && hasAwardedState && papers.Count == 4 && !IsCollecting;

    private void ToggleViewer()
    {
        if (!CanToggleIcon()) return;
        if (AnyPaperOpen())
        {
            CloseAllPapers();
            return;
        }
        if (!CanOpenFromIcon() || !InitializeHud()) return;
        EnsureViewer();
        OpenPapers(null);
    }

    private bool OpenFromDrop(Vector2 screenPosition)
    {
        if (!CanOpenFromIcon() || !InitializeHud()) return false;
        EnsureViewer();
        OpenPapers(screenPosition);
        return true;
    }

    private void EnsureViewer()
    {
        if (overlay != null || !InitializeHud()) return;
        overlay = HudTheme.CreateRect("Annotated Papers Overlay", hudCanvas.transform);
        HudTheme.Stretch(overlay);
        Canvas overlayCanvas = overlay.gameObject.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 1000;
        overlay.gameObject.AddComponent<GraphicRaycaster>();

        blocker = HudTheme.CreateRect("Collection Backdrop", overlay);
        HudTheme.Stretch(blocker);
        Image shade = HudTheme.AddImage(blocker, null, new Color(0.02f, 0.03f, 0.05f, 0.55f));
        shade.raycastTarget = true;
        blocker.gameObject.SetActive(false);

        paperLayer = HudTheme.CreateRect("Four Annotated Papers", overlay);
        HudTheme.Stretch(paperLayer);
        RectTransform host = hudCanvas.transform as RectTransform;
        float availableWidth = host != null ? host.rect.width : Screen.width;
        float availableHeight = host != null ? host.rect.height : Screen.height;
        if (availableWidth <= 0f) availableWidth = Screen.width;
        if (availableHeight <= 0f) availableHeight = Screen.height;
        // Readable, not oversized: at most 60% of the screen height; the row may overlap slightly.
        float scale = Mathf.Min(1f, availableHeight * 0.62f / PaperHeight,
            (availableWidth - ScreenInset * 2f) / (PaperWidth * 3.2f));
        paperSize = new Vector2(PaperWidth * scale, PaperHeight * scale);
        rowWidth = availableWidth - ScreenInset * 2f;
    }

    private void BuildPapers(CaseReviewState review, AnnotationDefinition annotation)
    {
        for (int i = papers.Count - 1; i >= 0; i--)
        {
            if (papers[i] != null) papers[i].Closed -= HandleWindowClosed;
            if (papers[i] != null) Destroy(papers[i].gameObject);
        }
        papers.Clear();
        faces.Clear();
        slots.Clear();

        int count = annotation.entries.Count;
        // One row: full gaps when there is room, otherwise an even slight overlap.
        float step = count > 1 ? Mathf.Min(paperSize.x + PaperGap, (rowWidth - paperSize.x) / (count - 1)) : 0f;
        for (int i = 0; i < count; i++)
        {
            AnnotationEntryDefinition entry = annotation.entries[i];
            if (entry == null) continue;
            AnnotationEntryState saved = review.challenge.Find(entry.id);
            FloatingWindow window = BuildPaper(entry, saved, annotation, i);
            papers.Add(window);
            slots.Add(new Vector2((i - (count - 1) * 0.5f) * step, RowLift));
        }
        papersBuilt = papers.Count > 0;
        builtReviewId = review.reviewId;
        builtEntryCount = count;
        UpdateCount();
    }

    private FloatingWindow BuildPaper(AnnotationEntryDefinition entry, AnnotationEntryState saved,
        AnnotationDefinition annotation, int index)
    {
        RectTransform card = HudTheme.CreateRect("Annotated Paper " + entry.id, paperLayer);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = paperSize;

        Image hit = card.gameObject.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        hit.raycastTarget = true;

        AnnotationPaperCollectionFace face = AnnotationPaper.BuildCollectionFace(card, hudCanvas.transform,
            entry, saved, annotation, index);
        FloatingWindow window = card.gameObject.AddComponent<FloatingWindow>();
        card.gameObject.AddComponent<GraphicRaycaster>();
        window.UsesPointRaycastFiltering = true;
        if (icon != null) window.SetReturnIcon(icon);
        window.UsePaperMotion(Tilts[index % Tilts.Length], icon);
        face.SetCloseAction(window.CloseWindow);
        window.Closed += HandleWindowClosed;
        faces.Add(face);
        card.gameObject.SetActive(false);
        return window;
    }

    /// <summary>
    /// Brings the four papers out as a reference (Reviewed Schedule boxes, or
    /// the icon). True when they are open afterwards.
    /// </summary>
    public bool OpenForReference()
    {
        if (!CanToggleIcon()) return false;
        if (AnyPaperOpen())
        {
            ShowAnnotationsForReview();
            for (int i = 0; i < papers.Count; i++)
                if (papers[i] != null && papers[i].IsOpen()) papers[i].BringToFront();
            return true;
        }
        if (!CanOpenFromIcon() || !InitializeHud()) return false;
        EnsureViewer();
        OpenPapers(null);
        return true;
    }

    private void OpenPapers(Vector2? origin)
    {
        HudWindowLock.Acquire(this);
        holdingWindows = true;
        Vector2 shift = Vector2.zero;
        for (int i = 0; i < papers.Count; i++)
        {
            FloatingWindow paper = papers[i];
            if (paper == null) continue;
            if (origin.HasValue) paper.SetPaperOpenOrigin(origin.Value);
            paper.SetPaperOpenDelay(i * 0.07f);
            paper.OpenWindowWithOffset((i < slots.Count ? slots[i] : Vector2.zero) + shift);
        }
        if (!IsCollecting) ShowAnnotationsForReview();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
    }

    /// <summary>During the office review the highlighted amounts are what the player needs: show them at once.</summary>
    private void ShowAnnotationsForReview()
    {
        if (!OfficeReview.CanEdit(Level2OfficeStart.Review)) return;
        for (int i = 0; i < faces.Count; i++)
            if (faces[i] != null) faces[i].SetAnnotations(true, false);
    }

    private void CloseAllPapers()
    {
        for (int i = 0; i < papers.Count; i++)
            if (papers[i] != null && papers[i].IsOpen()) papers[i].CloseWindow();
        if (!AnyPaperOpen()) ReleaseWindowHold();
    }

    private IEnumerator CollectPapers()
    {
        IsCollecting = true;
        PlayerControlHold.Acquire(this);
        if (blocker != null) blocker.gameObject.SetActive(true);
        for (int i = 0; i < faces.Count; i++)
            if (faces[i] != null) faces[i].SetInputLocked(true);

        Camera eventCamera = hudCanvas != null && hudCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? hudCanvas.worldCamera != null ? hudCanvas.worldCamera : Camera.main
            : null;
        Vector2 iconOrigin = icon != null
            ? RectTransformUtility.WorldToScreenPoint(eventCamera, icon.TransformPoint(icon.rect.center))
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        OpenPapers(iconOrigin);

        float started = Time.unscaledTime;
        float showUntil = started + CollectionPreviewSeconds;
        while (IsCollecting && Time.unscaledTime < showUntil) yield return null;
        if (IsCollecting) CloseAllPapers();

        float closeUntil = Time.unscaledTime + 1f;
        while (IsCollecting && Time.unscaledTime < closeUntil) yield return null;
        if (IsCollecting) FinishCollection();
    }

    private void HandleWindowClosed()
    {
        if (AnyPaperOpen()) return;
        ReleaseWindowHold();
        if (IsCollecting && AllPapersHidden()) FinishCollection();
    }

    private bool AnyPaperOpen()
    {
        for (int i = 0; i < papers.Count; i++)
            if (papers[i] != null && papers[i].IsOpen()) return true;
        return false;
    }

    private bool AllPapersHidden()
    {
        for (int i = 0; i < papers.Count; i++)
            if (papers[i] != null && papers[i].gameObject.activeSelf) return false;
        return true;
    }

    private void ReleaseWindowHold()
    {
        if (!holdingWindows) return;
        holdingWindows = false;
        HudWindowLock.Release(this);
    }

    private void FinishCollection()
    {
        IsCollecting = false;
        if (blocker != null) blocker.gameObject.SetActive(false);
        for (int i = 0; i < faces.Count; i++)
            if (faces[i] != null) faces[i].SetInputLocked(false);
        PlayerControlHold.Release(this);
        if (!AnyPaperOpen()) ReleaseWindowHold();
    }

    private void UpdateCount()
    {
        if (countText != null) countText.text = hasAwardedState ? papers.Count.ToString() : "0";
        Button button = icon != null ? icon.GetComponent<Button>() : null;
        if (button != null) button.interactable = hasAwardedState && papers.Count == 4;
    }

    private void CleanupRuntimeHud()
    {
        for (int i = 0; i < papers.Count; i++)
            if (papers[i] != null) papers[i].Closed -= HandleWindowClosed;
        papers.Clear();
        faces.Clear();
        slots.Clear();
        papersBuilt = false;
        if (iconDrag != null)
        {
            iconDrag.enabled = false;
            iconDrag.Visual = null;
            iconDrag.CanDrag = null;
            iconDrag.OpenAt = null;
            Destroy(iconDrag);
        }
        if (dragVisual != null) Destroy(dragVisual.gameObject);
        if (icon != null)
        {
            Button button = icon.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(ToggleViewer);
                button.interactable = false;
            }
            if (ownsIcon) Destroy(icon.gameObject);
        }
        if (overlay != null)
        {
            overlay.gameObject.SetActive(false);
            Destroy(overlay.gameObject);
        }
        hudCanvas = null;
        icon = null;
        countText = null;
        iconDrag = null;
        dragVisual = null;
        overlay = blocker = paperLayer = null;
        ownsIcon = false;
    }
}
