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
    // Hold any open paper this long (finger still) to gather them all into one stack.
    public const float StackHoldSeconds = 2f;
    private const float StackHoldSlop = 34f;
    private const float StackRingDelay = 0.3f;
    private static readonly Vector2[] StackOffsets = { new Vector2(0f, 0f), new Vector2(9f, -7f), new Vector2(-8f, -13f), new Vector2(6f, -19f) };

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

    // Hold-to-stack gesture.
    private int holdPaper = -1;
    private float holdStarted;
    private Vector2 holdOrigin;
    private bool stacked;
    private readonly List<Vector2> scatterSpots = new List<Vector2>();
    private Coroutine scatterMotion;
    private RectTransform holdRing;
    private Image holdRingFill;
    private TextMeshProUGUI holdLabel;
    private Vector3 iconRestScale = Vector3.one;

    /// <summary>The papers are gathered in one stack under the finger.</summary>
    public bool IsStacked => stacked;

    /// <summary>A long press just ended: the release is not a tap on an amount.</summary>
    public bool SwallowTap => Time.unscaledTime < swallowTapUntil;
    private float swallowTapUntil = -1f;

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

    // After the EventSystem moved the touched window, so the stack wins.
    private void LateUpdate() => UpdateStackHold();

    private void OnDisable()
    {
        GameplayEvents.OnIncomeQuestCompleted -= HandleIncomeQuestCompleted;
        StopAllCoroutines();
        scatterMotion = null;
        EndHold(false);
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
        if (!IsCollecting) GameplayEvents.RaiseGuideCue("AnnotatedPapersOpened");
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

    // =========================================================
    // HOLD TO STACK
    // =========================================================

    /// <summary>
    /// Hold any open paper still for StackHoldSeconds: every open paper glides
    /// into one stack under the finger. Drop the stack on the icon to put them
    /// away; let go anywhere else and they scatter back to where they were.
    /// </summary>
    private void UpdateStackHold()
    {
        if (paperLayer == null || papers.Count == 0) return;
        bool pressed = Input.GetMouseButton(0) || Input.touchCount > 0;
        Vector2 finger = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;

        if (!pressed)
        {
            if (stacked) DropStack(finger);
            else if (holdPaper >= 0) EndHold(false);
            return;
        }

        if (stacked)
        {
            FollowFinger(finger, Time.unscaledDeltaTime);
            return;
        }

        if (holdPaper < 0)
        {
            bool began = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            if (!began || IsCollecting || scatterMotion != null || ValueDragSource.IsAnyDragActive) return;
            holdPaper = TopPaperAt(finger);
            if (holdPaper < 0) return;
            holdStarted = Time.unscaledTime;
            holdOrigin = finger;
            return;
        }

        // A drag (the paper follows the finger) or another drag cancels the hold.
        if (Vector2.Distance(finger, holdOrigin) > StackHoldSlop || ValueDragSource.IsAnyDragActive || IsCollecting ||
            holdPaper >= papers.Count || papers[holdPaper] == null || !papers[holdPaper].IsOpen())
        {
            EndHold(false);
            return;
        }

        float held = Time.unscaledTime - holdStarted;
        ShowHoldRing(finger, Mathf.Clamp01((held - StackRingDelay) / (StackHoldSeconds - StackRingDelay)), held >= StackRingDelay);
        if (held >= StackHoldSeconds) Gather(finger);
    }

    private int TopPaperAt(Vector2 screen)
    {
        int best = -1;
        int bestOrder = int.MinValue;
        for (int i = 0; i < papers.Count; i++)
        {
            FloatingWindow paper = papers[i];
            if (paper == null || !paper.IsOpen() || !paper.ContainsScreenPoint(screen)) continue;
            if (paper.SortingOrder > bestOrder)
            {
                bestOrder = paper.SortingOrder;
                best = i;
            }
        }
        return best;
    }

    private void Gather(Vector2 finger)
    {
        stacked = true;
        scatterSpots.Clear();
        for (int i = 0; i < papers.Count; i++)
            scatterSpots.Add(papers[i] != null ? papers[i].WindowRect.anchoredPosition : Vector2.zero);
        // The touched paper ends on top of the stack.
        for (int i = 0; i < papers.Count; i++)
            if (i != holdPaper && papers[i] != null && papers[i].IsOpen()) papers[i].BringToFront();
        papers[holdPaper].BringToFront();
        if (icon != null) iconRestScale = icon.localScale;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
        ShowHoldRing(finger, 1f, false);
        SetHoldLabel(finger, "Drop on the icon to put them away");
        GameplayEvents.RaiseGuideCue("AnnotatedPapersStacked");
    }

    private void FollowFinger(Vector2 finger, float dt)
    {
        Vector2 centre = LocalAnchored(finger);
        float follow = 1f - Mathf.Exp(-16f * dt);
        int order = 0;
        for (int i = 0; i < papers.Count; i++)
        {
            FloatingWindow paper = papers[i];
            if (paper == null || !paper.IsOpen()) continue;
            Vector2 offset = i == holdPaper ? Vector2.zero : StackOffsets[(++order) % StackOffsets.Length];
            RectTransform rect = paper.WindowRect;
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, centre + offset, follow);
        }
        bool overIcon = OverIcon(finger);
        if (icon != null)
            icon.localScale = iconRestScale * (overIcon ? 1.18f : 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 8f));
        SetHoldLabel(finger, overIcon ? "Let go to put them away" : "Drop on the icon to put them away");
    }

    private void DropStack(Vector2 finger)
    {
        bool intoIcon = OverIcon(finger);
        stacked = false;
        EndHold(false);
        if (icon != null) icon.localScale = iconRestScale;
        if (intoIcon)
        {
            CloseAllPapers();
            GameplayEvents.RaiseGuideCue("AnnotatedPapersPutAway");
            return;
        }
        if (scatterMotion != null) StopCoroutine(scatterMotion);
        scatterMotion = StartCoroutine(Scatter());
    }

    private IEnumerator Scatter()
    {
        var from = new List<Vector2>();
        for (int i = 0; i < papers.Count; i++)
            from.Add(papers[i] != null ? papers[i].WindowRect.anchoredPosition : Vector2.zero);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
        const float seconds = 0.42f;
        for (float elapsed = 0f; elapsed < seconds + 0.12f; elapsed += Time.unscaledDeltaTime)
        {
            for (int i = 0; i < papers.Count && i < scatterSpots.Count; i++)
            {
                FloatingWindow paper = papers[i];
                if (paper == null || !paper.IsOpen()) continue;
                float t = Mathf.Clamp01((elapsed - i * 0.03f) / seconds);
                paper.WindowRect.anchoredPosition = Vector2.LerpUnclamped(from[i], scatterSpots[i], DeskMotion.EaseOutBack(t, 0.8f));
            }
            yield return null;
        }
        for (int i = 0; i < papers.Count && i < scatterSpots.Count; i++)
            if (papers[i] != null && papers[i].IsOpen()) papers[i].WindowRect.anchoredPosition = scatterSpots[i];
        scatterMotion = null;
    }

    private void EndHold(bool keepStack)
    {
        if (holdPaper >= 0 && Time.unscaledTime - holdStarted >= StackRingDelay) swallowTapUntil = Time.unscaledTime + 0.2f;
        holdPaper = -1;
        if (!keepStack && stacked)
        {
            stacked = false;
            if (icon != null) icon.localScale = iconRestScale;
        }
        if (holdRing != null) holdRing.gameObject.SetActive(false);
        if (holdLabel != null) holdLabel.gameObject.SetActive(false);
    }

    private bool OverIcon(Vector2 screen)
    {
        if (icon == null) return false;
        Camera camera = HudEventCamera();
        Vector2 centre = RectTransformUtility.WorldToScreenPoint(camera, icon.TransformPoint(icon.rect.center));
        Vector3[] corners = new Vector3[4];
        icon.GetWorldCorners(corners);
        float radius = Vector2.Distance(RectTransformUtility.WorldToScreenPoint(camera, corners[0]),
            RectTransformUtility.WorldToScreenPoint(camera, corners[2])) * 0.5f;
        return Vector2.Distance(screen, centre) <= Mathf.Max(70f, radius * 1.4f);
    }

    private Camera HudEventCamera() =>
        hudCanvas != null && hudCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? hudCanvas.worldCamera != null ? hudCanvas.worldCamera : Camera.main
            : null;

    /// <summary>Screen point -> anchoredPosition of a centre-anchored paper in the paper layer.</summary>
    private Vector2 LocalAnchored(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(paperLayer, screen, HudEventCamera(), out Vector2 local);
        return local - paperLayer.rect.center;
    }

    private void ShowHoldRing(Vector2 finger, float fill, bool visible)
    {
        if (overlay == null) return;
        if (holdRing == null)
        {
            holdRing = HudTheme.CreateRect("Stack Hold Ring", overlay);
            holdRing.anchorMin = holdRing.anchorMax = holdRing.pivot = new Vector2(0.5f, 0.5f);
            holdRing.sizeDelta = new Vector2(96f, 96f);
            Image back = HudTheme.AddImage(holdRing, HudTheme.Circle, new Color(0.04f, 0.06f, 0.1f, 0.45f));
            back.raycastTarget = false;
            RectTransform fillRect = HudTheme.CreateRect("Fill", holdRing);
            HudTheme.Stretch(fillRect, 6f);
            holdRingFill = HudTheme.AddImage(fillRect, HudTheme.Circle, new Color32(255, 214, 92, 230));
            holdRingFill.type = Image.Type.Filled;
            holdRingFill.fillMethod = Image.FillMethod.Radial360;
            holdRingFill.fillOrigin = (int)Image.Origin360.Top;
            holdRingFill.fillClockwise = true;
            holdRingFill.raycastTarget = false;
            Canvas top = holdRing.gameObject.AddComponent<Canvas>();
            top.overrideSorting = true;
            top.sortingOrder = 32000;
        }
        holdRing.gameObject.SetActive(visible || stacked);
        holdRing.anchoredPosition = LocalOverlay(finger);
        holdRingFill.fillAmount = fill;
        holdRing.localScale = Vector3.one * (stacked ? 0.6f : 1f);
        if (!stacked && visible) SetHoldLabel(finger, "Keep holding to stack the papers");
    }

    private void SetHoldLabel(Vector2 finger, string text)
    {
        if (overlay == null) return;
        if (holdLabel == null)
        {
            holdLabel = HudTheme.CreateLabel("Stack Hold Label", overlay, text, 26f, Color.white,
                TextAlignmentOptions.Center, FontStyles.Bold);
            holdLabel.rectTransform.anchorMin = holdLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            holdLabel.rectTransform.sizeDelta = new Vector2(560f, 44f);
            holdLabel.raycastTarget = false;
            holdLabel.outlineWidth = 0.22f;
            holdLabel.outlineColor = new Color32(10, 14, 22, 255);
            Canvas top = holdLabel.gameObject.AddComponent<Canvas>();
            top.overrideSorting = true;
            top.sortingOrder = 32000;
        }
        holdLabel.gameObject.SetActive(true);
        holdLabel.text = text;
        holdLabel.rectTransform.anchoredPosition = LocalOverlay(finger) + new Vector2(0f, 82f);
    }

    private Vector2 LocalOverlay(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen, HudEventCamera(), out Vector2 local);
        return local - overlay.rect.center;
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
