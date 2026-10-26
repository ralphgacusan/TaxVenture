using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Controls the normal Case Folder UI.
///
/// Handles:
/// - Case folder pages, including the compact Level 2 review packet
/// - Page navigation
/// - Case information
/// - Editable fields
/// - Supporting documents
///
/// This UI is opened by CaseFolderInteractable.
///
/// IMPORTANT:
/// This class contains NO stamping logic.
/// Stamping is handled separately by StampCaseFolderUI.
///
/// VISIBILITY:
/// The Case Folder is controlled by FloatingWindow.
/// WorkspaceLayoutManager is NOT used here.
/// </summary>
public class CaseFolderUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject folderPanelRoot;

    [Header("The Paper")]
    [SerializeField] private TextMeshProUGUI headingText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;

    [Header("Document Links (a page with the document-links role)")]
    [SerializeField] private SupportingDocumentsPageLink supportingDocumentsPageLink;

    [Header("Page Edge Navigation")]
    [SerializeField] private PageEdgeTapZone leftEdgeZone;
    [SerializeField] private PageEdgeTapZone rightEdgeZone;
    [SerializeField] private GameObject leftEdgeIndicator;
    [SerializeField] private GameObject rightEdgeIndicator;

    [Header("Swipe to Close")]
    [SerializeField] private SwipeDownToClose swipeToClose;

    [Header("Editable Page Rows")]
    [SerializeField] private Transform page2FieldRowListRoot;
    [SerializeField] private Transform page3FieldRowListRoot;
    [SerializeField] private Transform page4FieldRowListRoot;
    [SerializeField] private Transform page5FieldRowListRoot;

    [SerializeField] private CaseFolderFieldRow caseFolderFieldRowPrefab;

    [SerializeField] private GameObject page2FieldRowsContainer;
    [SerializeField] private GameObject page3FieldRowsContainer;
    [SerializeField] private GameObject page4FieldRowsContainer;
    [SerializeField] private GameObject page5FieldRowsContainer;

    [Header("Additional Row Containers")]
    [SerializeField] private Transform page1FieldRowListRoot;
    [SerializeField] private GameObject page1FieldRowsContainer;

    [SerializeField] private Transform page2FixedFieldRowListRoot;
    [SerializeField] private GameObject page2FixedFieldRowsContainer;

    [SerializeField] private Transform page7FieldRowListRoot;
    [SerializeField] private GameObject page7FieldRowsContainer;

    [Header("Page Layout")]
    [Tooltip("Width of the centred content column (heading, rows, document links) on the paper.")]
    [SerializeField, Min(100f)] private float contentWidth = 760f;

    private bool layoutCentred;

    [Header("Page Turn")]
    [Tooltip("Rect flipped when the page changes. Defaults to the heading's parent (the paper).")]
    [SerializeField] private RectTransform pageTurnRoot;
    [SerializeField, Min(0f)] private float pageTurnDuration = 0.26f;

    private Coroutine pageTurnRoutine;
    private bool hasPageTurnRest;
    private Quaternion pageTurnRestRotation;
    private Vector3 pageTurnRestScale;
    private Vector3 pageTurnRestPosition;

    private List<CaseFolderFieldRow> page1Rows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page2FixedRows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page2Rows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page3Rows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page4Rows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page5Rows = new List<CaseFolderFieldRow>();
    private List<CaseFolderFieldRow> page7Rows = new List<CaseFolderFieldRow>();

    private const int Page1Index = 0;
    public const string PreparedPacketPageTitle = "Prepared Packet";
    public const string PreparedSummaryPageTitle = "Summary";
    public const string PaymentNotePageTitle = "Payment Note";
    public const string TaxpayerProfilePageTitle = "Taxpayer Profile";
    private const float ProfileRowHeight = 250f;

    /// <summary>
    /// Optional level pages added after the case's own pages (Level 2's
    /// Findings page). Null in Level 1. Set and cleared by the level's code.
    /// </summary>
    public static System.Func<CaseData, List<CaseFolderPageContent>> ExtraPages;

    /// <summary>
    /// Optional level drawing on top of any page after it renders (Level 2's
    /// stamp ink on the front page). (page index, builder). Null in Level 1.
    /// </summary>
    public static System.Action<int, PageBuilder> PageDecorator;

    /// <summary>
    /// Optional pages of THIS folder instance only, after the case's own pages
    /// (T25: Marie's "Supporting Documents" page on the Level 1 desk folder).
    /// Never copied to another instance (a reference copy ignores it), and
    /// found by CaseFolderPageContent.Role rather than a page number.
    /// </summary>
    [System.NonSerialized] public System.Func<CaseData, List<CaseFolderPageContent>> InstancePages;

    // ============================================================
    // REFERENCE COPY (T30 revision, 2026-10-26: Arthur's Reviewed File)
    // ============================================================

    private CaseData referenceData;
    private List<CaseFolderPageContent> referencePages;

    /// <summary>True for a reference copy of this folder: it prints its own finished data, never the active case.</summary>
    public bool IsReference => referenceData != null;

    /// <summary>
    /// Binds this folder instance to a finished reference instead of CaseManager's
    /// active case: the same pages, layout, typography and page turns, printed
    /// from data, with extraPages after the front page. A reference copy runs no
    /// level hooks (ExtraPages, PageDecorator), review pages, document links or
    /// game-state change; its document layer makes every value read-only
    /// (Doc3DLayer.AlwaysReadOnly). Marie's folder never calls this.
    /// </summary>
    public void UseReference(CaseData data, List<CaseFolderPageContent> extraPages)
    {
        referenceData = data;
        referencePages = extraPages;
        BindPageNavigation();
    }

    /// <summary>Raised after a page has been drawn (its index).</summary>
    public event System.Action<int> PageRendered;

    /// <summary>The front page's rows as drawn now.</summary>
    public IReadOnlyList<CaseFolderFieldRow> FrontPageRows => page1Rows;

    /// <summary>What this folder prints: its reference, or the active case.</summary>
    private CaseData CurrentData =>
        referenceData ?? (CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null);

    /// <summary>The paper the pages print on (also what flips on a page turn).</summary>
    public RectTransform PaperRect => GetPageTurnRoot();

    /// <summary>True while this folder's paper is visible (desk or HUD view).</summary>
    public bool IsShowing => folderPanelRoot != null && folderPanelRoot.activeInHierarchy;
    /// <summary>Raised as closing starts so floating desk tools can return immediately.</summary>
    public event System.Action Closing;
    public void NotifyClosing() => Closing?.Invoke();

    /// <summary>The page currently shown (0 = Taxpayer Information, the front page).</summary>
    public int CurrentPageIndex => currentPageIndex;

    /// <summary>Find a level page by its printed heading; -1 when unavailable.</summary>
    public int FindPageIndex(string heading) => pages == null ? -1 : pages.FindIndex(p => p.Heading == heading);

    /// <summary>Find a page by its stable role (CaseFolderPageContent.Role) in this folder's current pages; -1 when it has none.</summary>
    public int FindPageIndexByRole(string role) =>
        pages == null || string.IsNullOrEmpty(role) ? -1 : pages.FindIndex(p => p.Role == role);

    /// <summary>True for the desk folder's world-space paper (not the flat HUD copy).</summary>
    public bool IsWorldPaper
    {
        get
        {
            Canvas canvas = GetComponentInParent<Canvas>(true);
            return canvas != null && canvas.renderMode == RenderMode.WorldSpace;
        }
    }

    private Coroutine leafThroughRoutine;

    /// <summary>
    /// Turns back (or forward) page by page to <paramref name="target"/> with
    /// quick page-turn flips, then calls <paramref name="done"/>.
    /// </summary>
    public void TurnToPage(int target, System.Action done = null)
    {
        if (pages == null || pages.Count == 0 || !isActiveAndEnabled)
        {
            done?.Invoke();
            return;
        }
        target = Mathf.Clamp(target, 0, pages.Count - 1);
        StopPageTurn();
        if (target == currentPageIndex) { RenderPage(target); done?.Invoke(); return; }
        leafThroughRoutine = StartCoroutine(LeafThrough(target, done));
    }

    private System.Collections.IEnumerator LeafThrough(int target, System.Action done)
    {
        const float flipSeconds = 0.32f;   // quicker than a single turn: several pages in a row
        while (currentPageIndex != target)
        {
            int direction = target < currentPageIndex ? -1 : 1;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();

            RectTransform root = GetPageTurnRoot();
            StopPageTurn(false);
            if (root == null)
            {
                currentPageIndex += direction;
                RenderPage(currentPageIndex);
                continue;
            }
            int shown = currentPageIndex + direction;
            yield return DeskMotion.TurnPage(root, pageTurnRestRotation, pageTurnRestScale, direction, flipSeconds,
                () => { currentPageIndex = shown; RenderPage(shown); });
        }
        leafThroughRoutine = null;
        done?.Invoke();
    }

    // Objects a level page created (slots, ink); removed on every page render.
    private readonly List<GameObject> levelPageObjects = new List<GameObject>();


    private List<CaseFolderPageContent> pages;
    private int currentPageIndex = 0;

    private bool hasOpenedBefore = false;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        BindPageNavigation();

        if (swipeToClose != null)
            swipeToClose.OnSwipeClosed += Hide;
    }

    private void BindPageNavigation()
    {
        if (leftEdgeZone != null)
        {
            leftEdgeZone.OnTapped -= PreviousPage;
            leftEdgeZone.OnTapped += PreviousPage;
        }

        if (rightEdgeZone != null)
        {
            rightEdgeZone.OnTapped -= NextPage;
            rightEdgeZone.OnTapped += NextPage;
        }
    }

    private void OnDisable()
    {
        NotifyClosing();
        StopPageTurn();
        ReleaseWindowHold();
        ReleaseDeskHold();
        System.Action close = close3DView;
        close3DView = null;
        close?.Invoke();
    }

    // ============================================================
    // SHOW
    // ============================================================

    /// <summary>
    /// Opens the normal Case Folder through FloatingWindow.
    /// Called by CaseFolderInteractable.
    /// </summary>
    public void Show()
    {
        bool wasShowing = IsShowing;
        DebugTrace.Log("[CaseFolderUI] Show() was called");
        DebugTrace.Log("========== CASE FOLDER SHOW ==========");

        if (!IsReference && CaseManager.Instance == null)
        {
            Debug.LogError(
                "[CaseFolderUI] CaseManager.Instance is NULL."
            );
            return;
        }

        if (CurrentData == null)
        {
            Debug.LogError(
                "[CaseFolderUI] CurrentCase is NULL."
            );
            return;
        }

        if (folderPanelRoot == null)
        {
            Debug.LogError(
                "[CaseFolderUI] folderPanelRoot reference is NULL!"
            );
            return;
        }

        DebugTrace.Log(
            $"[CaseFolderUI] Opening panel: {folderPanelRoot.name}"
        );

        DebugTrace.Log(
            $"[CaseFolderUI] Before Open -> " +
            $"activeSelf={folderPanelRoot.activeSelf}, " +
            $"activeInHierarchy={folderPanelRoot.activeInHierarchy}"
        );

        // A scoped hold also releases when storage disables this world paper.
        // Do not leave a legacy LockPlayerControls call orphaned after storage.
        if (!deskHold)
        {
            deskHold = true;
            HudWindowLock.Acquire(this);
            if (IsWorldPaper && HudWindowLock.IsHeld && CameraController.Instance != null)
            {
                FloatingModel3D cover = GetComponentInParent<FloatingModel3D>();
                if (cover != null)
                {
                    deskRaycaster = CameraController.Instance.MainCamera.gameObject.AddComponent<DocumentPhysicsRaycaster>();
                    deskRaycaster.Document = cover.transform;
                }
            }
        }

        // Build case pages.
        pages = BuildPages(
            CurrentData
        );

        // Reset to the first page.
        StopPageTurn();
        currentPageIndex = Page1Index;

        // Render the first page before showing the panel.
        RenderPage(currentPageIndex);

        // Directly show the UI panel.
        folderPanelRoot.SetActive(true);
        PrintedPaperStyle.Apply(GetPageTurnRoot(), headingText, bodyText, pageIndicatorText);
        CentreContentColumn();
        if (!wasShowing && AudioManager.Instance != null)
            AudioManager.Instance.PlayDocumentSFX();

        DebugTrace.Log(
            $"[CaseFolderUI] After Open -> " +
            $"activeSelf={folderPanelRoot.activeSelf}, " +
            $"activeInHierarchy={folderPanelRoot.activeInHierarchy}"
        );

        // Change the game state only on the first opening (never for a reference copy).
        if (!hasOpenedBefore && !IsReference)
        {
            hasOpenedBefore = true;

            if (GameStateMachine.Instance != null &&
                GameStateMachine.Instance.CurrentState is ReceiveCaseState)
            {
                GameStateMachine.Instance.ChangeState(
                    new ReviewDocumentsState()
                );
            }
        }

        DebugTrace.Log("========== CASE FOLDER SHOW COMPLETE ==========");
    }

    // ============================================================
    // HIDE
    // ============================================================

    /// <summary>
    /// Closes ONLY the normal Case Folder.
    /// FloatingWindow handles the actual visibility.
    /// </summary>
    public void Hide()
    {
        if (close3DView != null) { close3DView(); return; }
        bool wasShowing = IsShowing;
        NotifyClosing();
        // Opened from the HUD icon as a window: close it the window way, so
        // the shared HUD hold is released instead of UnlockPlayerControls.
        if (openedAsWindow)
        {
            HideAsWindow();
            return;
        }

        StopPageTurn();

        if (folderPanelRoot != null)
        {
            folderPanelRoot.SetActive(false);
        }

        ReleaseDeskHold();
        if (wasShowing && AudioManager.Instance != null)
            AudioManager.Instance.PlayDocumentSFX(true);

        DebugTrace.Log("[CaseFolderUI] Case Folder hidden.");
    }

    // ============================================================
    // HUD WINDOW (Office HUD: opened from the Case Folder icon)
    // ============================================================

    private bool deskHold;
    private DocumentPhysicsRaycaster deskRaycaster;
    private bool openedAsWindow;
    private System.Action close3DView;
    private bool savedWindowEnabled;
    private bool openedAs3DView;
    private FloatingWindow hudWindow;

    /// <summary>True while the folder panel is open (from the icon or the desk).</summary>
    public bool IsPanelOpen =>
        folderPanelRoot != null && folderPanelRoot.activeSelf &&
        (close3DView != null || FolderWindow == null || FolderWindow.IsOpen());

    private FloatingWindow FolderWindow =>
        hudWindow != null ? hudWindow :
        (hudWindow = folderPanelRoot != null ? folderPanelRoot.GetComponent<FloatingWindow>() : null);

    /// <summary>Renders the desk paper; the 3D view alone owns its position and animation.</summary>
    public bool ShowFor3DView(System.Action close)
    {
        if (close == null || CurrentData == null || folderPanelRoot == null)
            return false;
        FloatingWindow window = FolderWindow;
        savedWindowEnabled = window != null && window.enabled;
        if (window != null)
        {
            window.StopAllCoroutines();
            window.enabled = false;
        }
        close3DView = close;
        openedAs3DView = true;
        pages = BuildPages(CurrentData);
        StopPageTurn();
        currentPageIndex = Page1Index;
        RenderPage(currentPageIndex);
        folderPanelRoot.SetActive(true);
        PrintedPaperStyle.Apply(GetPageTurnRoot(), headingText, bodyText, pageIndicatorText);
        CentreContentColumn();
        Canvas.ForceUpdateCanvases();
        openedAsWindow = true;
        HudWindowLock.Acquire(this);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDocumentSFX();
        if (!hasOpenedBefore && !IsReference)
        {
            hasOpenedBefore = true;
            if (GameStateMachine.Instance != null && GameStateMachine.Instance.CurrentState is ReceiveCaseState)
                GameStateMachine.Instance.ChangeState(new ReviewDocumentsState());
        }
        return true;
    }

    public void HideFor3DView()
    {
        if (!openedAs3DView) return;
        openedAs3DView = false;
        close3DView = null;
        NotifyClosing();
        StopPageTurn();
        ReleaseWindowHold();
        if (folderPanelRoot != null) folderPanelRoot.SetActive(false);
        FloatingWindow window = FolderWindow;
        if (window != null) window.enabled = savedWindowEnabled;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDocumentSFX(true);
    }

    /// <summary>
    /// Opens the same Case Folder pages as a HUD paper window: anywhere (not
    /// only at the desk), flying out of the Case Folder icon, or growing out
    /// of dropScreenPosition when the folder was dragged out and dropped.
    /// Holds the player like Notes/profiles instead of LockPlayerControls,
    /// so several windows can be open together.
    /// </summary>
    public bool ShowAsWindow(Vector2? dropScreenPosition)
    {
        bool wasOpen = IsPanelOpen;
        if (CurrentData == null || folderPanelRoot == null)
        {
            return false;
        }

        pages = BuildPages(CurrentData);
        StopPageTurn();
        currentPageIndex = Page1Index;
        RenderPage(currentPageIndex);

        // Activate first so the FloatingWindow initialises, then open it.
        folderPanelRoot.SetActive(true);
        PrintedPaperStyle.Apply(GetPageTurnRoot(), headingText, bodyText, pageIndicatorText);
        CentreContentColumn();

        FloatingWindow window = FolderWindow;
        if (window != null)
        {
            window.Closed -= HandleWindowClosed;
            window.Closed += HandleWindowClosed;
            if (dropScreenPosition.HasValue)
            {
                window.SetPaperOpenOrigin(dropScreenPosition.Value);
                window.OpenWindowAtScreenPosition(dropScreenPosition.Value);
            }
            else
            {
                window.OpenWindow();
            }
        }

        openedAsWindow = true;
        HudWindowLock.Acquire(this);

        if (!wasOpen && AudioManager.Instance != null)
            AudioManager.Instance.PlayDocumentSFX();

        if (!hasOpenedBefore && !IsReference)
        {
            hasOpenedBefore = true;

            if (GameStateMachine.Instance != null &&
                GameStateMachine.Instance.CurrentState is ReceiveCaseState)
            {
                GameStateMachine.Instance.ChangeState(
                    new ReviewDocumentsState()
                );
            }
        }

        return true;
    }

    /// <summary>Closes the HUD window (flies back into the icon).</summary>
    public void HideAsWindow()
    {
        if (close3DView != null) { close3DView(); return; }
        bool wasShowing = IsShowing;
        NotifyClosing();
        StopPageTurn();

        FloatingWindow window = FolderWindow;
        if (window != null && folderPanelRoot.activeInHierarchy)
        {
            window.CloseWindow();
        }
        else if (folderPanelRoot != null)
        {
            folderPanelRoot.SetActive(false);
        }

        ReleaseWindowHold();
        if (wasShowing && AudioManager.Instance != null)
            AudioManager.Instance.PlayDocumentSFX(true);
    }

    /// <summary>Case Folder icon tap: open as a window, or close whatever is open.</summary>
    public void ToggleFromHud()
    {
        if (IsPanelOpen)
        {
            Hide();
        }
        else
        {
            ShowAsWindow(null);
        }
    }

    private void HandleWindowClosed()
    {
        // Also covers closing by dragging the window onto the icon.
        ReleaseWindowHold();
    }

    private void ReleaseDeskHold()
    {
        if (!deskHold) return;
        deskHold = false;
        if (deskRaycaster != null)
        {
            deskRaycaster.enabled = false;
            Destroy(deskRaycaster);
            deskRaycaster = null;
        }
        HudWindowLock.Release(this);
    }

    private void ReleaseWindowHold()
    {
        if (!openedAsWindow)
        {
            return;
        }

        openedAsWindow = false;
        HudWindowLock.Release(this);
    }

    // ============================================================
    // PAGE NAVIGATION
    // ============================================================

    public void NextPage()
    {
        if (leafThroughRoutine != null) return;
        // Changing page rebuilds the rows/chips; never do it under a dragged value.
        if (ValueDragSource.IsAnyDragActive)
            return;

        if (pages == null || pages.Count == 0)
            return;

        if (currentPageIndex < pages.Count - 1)
        {
            currentPageIndex++;

            // Play paper/page-turn SFX.
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPaper2SFX();
            }

            TurnToCurrentPage(1);
        }
    }

    public void PreviousPage()
    {
        if (leafThroughRoutine != null) return;
        // Changing page rebuilds the rows/chips; never do it under a dragged value.
        if (ValueDragSource.IsAnyDragActive)
            return;

        if (pages == null || pages.Count == 0)
            return;

        if (currentPageIndex > 0)
        {
            currentPageIndex--;

            // Play paper/page-turn SFX.
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPaper2SFX();
            }

            TurnToCurrentPage(-1);
        }
    }

    // ============================================================
    // PAGE LAYOUT
    // ============================================================

    /// <summary>
    /// Puts the heading, every row list and the document links in one wider
    /// column centred on the paper, and mirrors the left page-turn strip to
    /// the right one so it does not overlap the wider column. Positions only;
    /// the rows, data and page flow are unchanged.
    /// </summary>
    private void CentreContentColumn()
    {
        if (layoutCentred)
            return;
        layoutCentred = true;

        // Only the open 3D folder's paper; the HUD copy keeps its own layout.
        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
            return;

        Transform[] columns =
        {
            page1FieldRowListRoot, page2FixedFieldRowListRoot, page2FieldRowListRoot,
            page3FieldRowListRoot, page4FieldRowListRoot, page5FieldRowListRoot,
            page7FieldRowListRoot,
            supportingDocumentsPageLink != null ? supportingDocumentsPageLink.transform : null,
            pageIndicatorText != null ? pageIndicatorText.transform : null
        };

        foreach (Transform column in columns)
        {
            RectTransform rect = column as RectTransform;
            if (rect == null || !Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x))
                continue;

            rect.anchorMin = new Vector2(0.5f, rect.anchorMin.y);
            rect.anchorMax = new Vector2(0.5f, rect.anchorMax.y);
            rect.pivot = new Vector2(0.5f, rect.pivot.y);
            rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
            if (column != (pageIndicatorText != null ? pageIndicatorText.transform : null))
                rect.sizeDelta = new Vector2(contentWidth, rect.sizeDelta.y);
        }

        if (headingText != null)
        {
            RectTransform heading = headingText.rectTransform;
            heading.anchorMin = new Vector2(0.5f, heading.anchorMin.y);
            heading.anchorMax = new Vector2(0.5f, heading.anchorMax.y);
            heading.pivot = new Vector2(0.5f, heading.pivot.y);
            heading.anchoredPosition = new Vector2(0f, heading.anchoredPosition.y);
            heading.sizeDelta = new Vector2(contentWidth, heading.sizeDelta.y);
            headingText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        }

        // Previous / next page buttons sit in the bottom corners, level with
        // the "Page n / m" line, instead of full-height strips at the sides.
        PlaceBottomPageButton(leftEdgeZone, false);
        PlaceBottomPageButton(rightEdgeZone, true);
    }

    private const float PageButtonSize = 150f;
    private const float PageButtonInset = 70f;
    private const float PageButtonBottom = 10f;

    private static void PlaceBottomPageButton(PageEdgeTapZone zone, bool right)
    {
        RectTransform rect = zone != null ? zone.transform as RectTransform : null;
        if (rect == null)
            return;

        float x = right ? 1f : 0f;
        rect.anchorMin = rect.anchorMax = new Vector2(x, 0f);
        rect.pivot = new Vector2(x, 0f);
        rect.anchoredPosition = new Vector2(right ? -PageButtonInset : PageButtonInset, PageButtonBottom);
        rect.sizeDelta = new Vector2(PageButtonSize, PageButtonSize);

        // The arrow (indicator) is the zone's child: centre it in the button.
        foreach (Transform child in rect)
        {
            RectTransform arrow = child as RectTransform;
            if (arrow == null || !child.name.EndsWith("EdgeIndicator"))
                continue;
            arrow.anchorMin = arrow.anchorMax = new Vector2(0.5f, 0.5f);
            arrow.pivot = new Vector2(0.5f, 0.5f);
            arrow.anchoredPosition = Vector2.zero;
            arrow.sizeDelta = new Vector2(PageButtonSize, PageButtonSize);
        }
    }

    // ============================================================
    // PAGE TURN
    // ============================================================

    private void TurnToCurrentPage(int direction)
    {
        RectTransform root = GetPageTurnRoot();
        if (root == null || pageTurnDuration <= 0f || !isActiveAndEnabled)
        {
            RenderPage(currentPageIndex);
            return;
        }

        StopPageTurn();
        pageTurnRoutine = StartCoroutine(DeskMotion.TurnPage(root, pageTurnRestRotation,
            pageTurnRestScale, direction, Mathf.Max(pageTurnDuration, DeskMotion.PageFlipSeconds), () =>
            {
                RenderPage(currentPageIndex);
            }));
    }

    /// <summary>Stops a page turn and puts the paper back at rest.</summary>
    private void StopPageTurn(bool cancelLeafThrough = true)
    {
        if (cancelLeafThrough && leafThroughRoutine != null)
        {
            StopCoroutine(leafThroughRoutine);
            leafThroughRoutine = null;
        }
        if (pageTurnRoutine != null)
        {
            StopCoroutine(pageTurnRoutine);
            pageTurnRoutine = null;
        }

        if (hasPageTurnRest && pageTurnRoot != null)
        {
            pageTurnRoot.localRotation = pageTurnRestRotation;
            pageTurnRoot.localScale = pageTurnRestScale;
            pageTurnRoot.localPosition = pageTurnRestPosition;
        }

    }

    private RectTransform GetPageTurnRoot()
    {
        if (pageTurnRoot == null && headingText != null)
            pageTurnRoot = headingText.transform.parent as RectTransform;

        if (pageTurnRoot != null && !hasPageTurnRest)
        {
            pageTurnRestRotation = pageTurnRoot.localRotation;
            pageTurnRestScale = pageTurnRoot.localScale;
            pageTurnRestPosition = pageTurnRoot.localPosition;
            hasPageTurnRest = true;
        }

        return pageTurnRoot;
    }

    // ============================================================
    // RENDER PAGE
    // ============================================================

    private void RenderPage(int index)
    {
        if (pages == null ||
            index < 0 ||
            index >= pages.Count)
        {
            return;
        }

        CaseFolderPageContent page = pages[index];

        if (headingText != null)
        {
            headingText.text = page.Heading;
        }

        if (pageIndicatorText != null)
        {
            pageIndicatorText.text =
                $"Page {index + 1} / {pages.Count}";
        }

        // Arrows only when there is a page that way (a one-page folder
        // shows none), like the Tax Code Book.
        if (leftEdgeIndicator != null)
        {
            leftEdgeIndicator.SetActive(index > 0);
        }

        if (rightEdgeIndicator != null)
        {
            rightEdgeIndicator.SetActive(index < pages.Count - 1);
        }

        bool isReviewCase = !IsReference && CaseManager.Instance != null && CaseManager.Instance.CurrentDefinition != null &&
            CaseManager.Instance.CurrentDefinition.HasReview;
        // Document buttons only on a page with that role (no fixed page number); a reference copy never shows the active case's links.
        supportingDocumentsPageLink?.OnFolderPageChanged(!isReviewCase && !IsReference && page.Role == CaseFolderPageContent.DocumentLinksRole);

        CaseData data = CurrentData;

        if (page1FieldRowsContainer != null)
        {
            page1FieldRowsContainer.SetActive(false);
        }

        if (page2FieldRowsContainer != null)
        {
            page2FieldRowsContainer.SetActive(false);
        }

        if (page2FixedFieldRowsContainer != null)
        {
            page2FixedFieldRowsContainer.SetActive(false);
        }

        if (page3FieldRowsContainer != null)
        {
            page3FieldRowsContainer.SetActive(false);
        }

        if (page4FieldRowsContainer != null)
        {
            page4FieldRowsContainer.SetActive(false);
        }

        if (page5FieldRowsContainer != null)
        {
            page5FieldRowsContainer.SetActive(false);
        }

        if (page7FieldRowsContainer != null)
        {
            page7FieldRowsContainer.SetActive(false);
        }

        if (bodyText != null)
        {
            bodyText.gameObject.SetActive(false);
        }

        ClearLevelPageObjects();

        if (index == Page1Index)
        {
            if (page1FieldRowsContainer != null)
            {
                page1FieldRowsContainer.SetActive(true);
            }

            BuildPage1Rows(data);
        }
        else if (page.Render != null)
        {
            if (page1FieldRowsContainer != null)
            {
                page1FieldRowsContainer.SetActive(true);
            }

            ClearRows(page1Rows);
            page.Render(new PageBuilder(this));
        }
        else if (page.Document != null)
        {
            // Review pages print in page 1's rows, so they look like it.
            if (page1FieldRowsContainer != null)
            {
                page1FieldRowsContainer.SetActive(true);
            }

            BuildReviewPageRows(data, page.Document);
        }

        if (!IsReference) PageDecorator?.Invoke(index, new PageBuilder(this));
        FitRowHeights();
        PageRendered?.Invoke(index);
    }

    /// <summary>Tallest a value row may grow to (an easier drag source / drop target).</summary>
    public const float RowTargetHeight = 110f;

    /// <summary>
    /// Makes the page's value rows taller, sharing the page's spare room, up
    /// to RowTargetHeight: bigger drag sources and drop targets that still
    /// end inside the page. Tall custom rows (profile, summaries) keep their size.
    /// </summary>
    private void FitRowHeights()
    {
        RectTransform list = page1FieldRowListRoot as RectTransform;
        if (list == null || !list.gameObject.activeInHierarchy) return;
        Canvas.ForceUpdateCanvases();
        float available = list.rect.height;
        if (available <= 1f) return;
        var group = list.GetComponent<HorizontalOrVerticalLayoutGroup>();
        float spacing = group != null ? group.spacing : 0f;
        float padding = group != null ? group.padding.top + group.padding.bottom : 0f;

        var small = new List<LayoutElement>();
        float fixedHeight = 0f;
        int count = 0;
        foreach (Transform child in list)
        {
            if (!child.gameObject.activeSelf || !(child is RectTransform rect)) continue;
            LayoutElement element = child.GetComponent<LayoutElement>();
            if (element != null && element.ignoreLayout) continue;
            count++;
            float natural = LayoutUtility.GetPreferredHeight(rect);
            if (element != null && element.preferredHeight >= 150f) { fixedHeight += element.preferredHeight; continue; }
            if (element == null) element = child.gameObject.AddComponent<LayoutElement>();
            if (element.minHeight < 0f || element.minHeight > RowTargetHeight) element.minHeight = -1f;
            fixedHeight += 0f;
            small.Add(element);
        }
        if (small.Count == 0) return;
        float room = available - padding - spacing * Mathf.Max(0, count - 1) - fixedHeight;
        float each = Mathf.Min(RowTargetHeight, room / small.Count);
        foreach (LayoutElement element in small)
        {
            float natural = LayoutUtility.GetPreferredHeight((RectTransform)element.transform);
            // Never shorter than the row's own text needs.
            element.minHeight = Mathf.Max(each, Mathf.Min(natural, RowTargetHeight));
        }
        LayoutRebuilder.MarkLayoutForRebuild(list);
    }

    // ============================================================
    // BUILD PAGES
    // ============================================================

    private List<CaseFolderPageContent> BuildPages(CaseData data)
    {
        var list = new List<CaseFolderPageContent>();

        list.Add(
            new CaseFolderPageContent(
                "Taxpayer Information",
                ""
            )
        );

        if (IsReference)
        {
            if (referencePages != null) list.AddRange(referencePages);
            return list;
        }

        AddReviewPages(list, data);

        if (InstancePages != null)
        {
            List<CaseFolderPageContent> own = InstancePages(data);
            if (own != null) list.AddRange(own);
        }

        if (ExtraPages != null)
        {
            List<CaseFolderPageContent> extra = ExtraPages(data);
            if (extra != null) list.AddRange(extra);
        }

        return list;
    }

    // ============================================================
    // LEVEL PAGES
    // ============================================================

    /// <summary>What a level page may draw with: page-1 rows and paper overlays.</summary>
    public sealed class PageBuilder
    {
        private readonly CaseFolderUI ui;

        internal PageBuilder(CaseFolderUI owner)
        {
            ui = owner;
        }

        /// <summary>The page's paper (for overlays such as stamp ink).</summary>
        public RectTransform Paper => ui.GetPageTurnRoot();

        public CaseFolderUI Owner => ui;

        /// <summary>A row owned by a removable sheet, rather than the current folder page.</summary>
        public CaseFolderFieldRow CreateSheetRow(Transform parent) => Instantiate(ui.caseFolderFieldRowPrefab, parent);

        /// <summary>Release an overlay before lifting it out of the folder.</summary>
        public void ReleaseOverlay(GameObject overlay) => ui.levelPageObjects.Remove(overlay);

        /// <summary>A read-only page-1 row (it can still be picked up as a value).</summary>
        public CaseFolderFieldRow AddSourceRow(string label, object value, DataValueType type, string key)
        {
            CaseFolderFieldRow row = Instantiate(ui.caseFolderFieldRowPrefab, ui.page1FieldRowListRoot);
            row.InitializeSourceOnly(label, value, type, key);
            ui.page1Rows.Add(row);
            return row;
        }

        /// <summary>A fresh page-1 row object for the level to turn into its own control.</summary>
        public GameObject AddRowObject()
        {
            CaseFolderFieldRow row = Instantiate(ui.caseFolderFieldRowPrefab, ui.page1FieldRowListRoot);
            ui.levelPageObjects.Add(row.gameObject);
            return row.gameObject;
        }

        /// <summary>Puts an object on the paper; it is removed when the page changes.</summary>
        public void AddOverlay(GameObject overlay)
        {
            if (overlay == null) return;
            overlay.transform.SetParent(Paper, false);
            ui.levelPageObjects.Add(overlay);
        }

        /// <summary>Redraws the current page (after the level changed its data).</summary>
        public void Refresh() => ui.RefreshCurrentPage();
    }

    /// <summary>Rebuilds the pages and redraws the current one, if the folder is open.</summary>
    public void RefreshCurrentPage()
    {
        if (folderPanelRoot == null || !folderPanelRoot.activeInHierarchy || CurrentData == null)
            return;

        pages = BuildPages(CurrentData);
        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, pages.Count - 1);
        RenderPage(currentPageIndex);
    }

    private void ClearLevelPageObjects()
    {
        foreach (GameObject item in levelPageObjects)
        {
            if (item == null) continue;
            item.SetActive(false);
            Destroy(item);
        }
        levelPageObjects.Clear();
    }

    /// <summary>Prints each prepared document on its own case-folder page.</summary>
    private void AddReviewPages(List<CaseFolderPageContent> list, CaseData data)
    {
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        if (definition == null || !definition.HasReview || data == null || data.review == null ||
            definition.documents == null || definition.review.documents == null)
            return;

        // One-sentence client profile, right after Taxpayer Information.
        if (!string.IsNullOrWhiteSpace(definition.review.profileText))
        {
            string profile = definition.review.profileText.Trim();
            list.Add(new CaseFolderPageContent(TaxpayerProfilePageTitle, "")
            {
                Render = page => BuildProfileRows(data, profile)
            });
        }

        var preparedDocuments = new List<ReviewDocumentDefinition>();
        foreach (ReviewDocumentDefinition document in definition.review.documents)
        {
            if (document == null || (document.kind != "summary" && document.kind != "note") ||
                !data.review.HasDocument(document.id))
                continue;
            if (definition.documents.Exists(d => d != null && d.documentName == document.documentName))
                preparedDocuments.Add(document);
        }

        foreach (ReviewDocumentDefinition reviewDocument in preparedDocuments)
        {
            ReviewDocumentDefinition captured = reviewDocument;
            DocumentDefinition document = definition.documents.Find(d => d != null &&
                d.documentName == captured.documentName);
            if (document == null) continue;

            string title = captured.kind == "summary" ? PreparedSummaryPageTitle : PaymentNotePageTitle;
            list.Add(new CaseFolderPageContent(title, "")
            {
                Render = page => BuildReviewPageRows(data, document)
            });
        }
    }

    /// <summary>The Taxpayer Profile page: the client's name, then the profile sentence as a printed paragraph.</summary>
    private void BuildProfileRows(CaseData data, string profile)
    {
        ClearRows(page1Rows);
        AddSourceOnlyRow(page1Rows, page1FieldRowListRoot, "Name", data.fullName, DataValueType.Text, "FullName");

        GameObject row = new PageBuilder(this).AddRowObject();
        foreach (MonoBehaviour behaviour in row.GetComponents<MonoBehaviour>())
            if (behaviour is CaseFolderFieldRow || behaviour is ValueDragSource ||
                behaviour is ValueSelectionHighlighter || behaviour is ValueTypeValidationFeedback ||
                behaviour is ValueHoverFeedback)
                Destroy(behaviour);
        TMP_Text text = row.GetComponentInChildren<TMP_Text>(true);
        if (text == null) return;
        Image background = row.GetComponent<Image>();
        if (background == null) background = row.GetComponentInChildren<Image>(true);
        PaperInkStyle.StyleFieldRow(background, text, false, null, null);
        if (background != null) background.raycastTarget = false;
        text.text = PaperInkStyle.FieldTextStacked("About the taxpayer", profile, false);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        // Large and readable, but auto-sized to the space left on the page so it never runs off the paper.
        text.enableAutoSizing = true;
        // A fixed height the page's row list always has room for (taller rows overflowed onto the Name row).
        text.fontSizeMin = 24f;
        text.fontSizeMax = 38f;
        text.lineSpacing = 4f;
        text.overflowMode = TextOverflowModes.Truncate;
        LayoutElement layout = row.GetComponent<LayoutElement>();
        if (layout == null) layout = row.AddComponent<LayoutElement>();
        layout.minHeight = ProfileRowHeight;
        layout.preferredHeight = ProfileRowHeight;
        layout.flexibleHeight = 0f;
    }

    /// <summary>Marks a held review document as read when its printed paper is opened.</summary>
    public static void MarkReviewDocumentRead(string documentName)
    {
        CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        if (data?.supportingDocuments == null) return;
        foreach (SupportingDocument supporting in data.supportingDocuments)
            if (supporting != null && supporting.documentName == documentName)
                supporting.isReviewed = true;
    }

    /// <summary>"₱12,000" / "12000.50" -> the amount; false when there is no number.</summary>
    public static bool TryParseAmount(string text, out float amount)
    {
        amount = 0f;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var digits = new System.Text.StringBuilder();
        foreach (char c in text)
            if (char.IsDigit(c) || c == '.') digits.Append(c);
        return digits.Length > 0 &&
               float.TryParse(digits.ToString(), System.Globalization.NumberStyles.Float,
                   System.Globalization.CultureInfo.InvariantCulture, out amount);
    }

    /// <summary>Prints a review page's fields as page-1-style rows and marks the page read.</summary>
    private void BuildReviewPageRows(CaseData data, DocumentDefinition document)
    {
        ClearRows(page1Rows);

        if (document.fields != null)
        {
            foreach (DocumentFieldDefinition field in document.fields)
            {
                if (field == null) continue;
                DataValueType type = System.Enum.TryParse(field.type, out DataValueType parsed) ? parsed : DataValueType.Text;
                object value = field.displayValue;
                // Number rows print and transfer a number ("₱12,000" -> 12000).
                if (type == DataValueType.Number)
                {
                    if (TryParseAmount(field.displayValue, out float amount)) value = amount;
                    else type = DataValueType.Text;
                }
                CaseFolderFieldRow row = Instantiate(caseFolderFieldRowPrefab, page1FieldRowListRoot);
                row.InitializeSourceOnly(field.label, value, type, field.semanticKey);
                page1Rows.Add(row);
            }
        }

        MarkReviewDocumentRead(document.documentName);
    }

    // ============================================================
    // PAGE ROWS
    // ============================================================

    private void BuildPage1Rows(CaseData data)
    {
        ClearRows(page1Rows);

        // Name
        AddSourceOnlyRow(
            page1Rows,
            page1FieldRowListRoot,
            "Name",
            data.fullName,
            DataValueType.Text,
            "FullName"
        );

        // TIN ID
        AddSourceOnlyRow(
            page1Rows,
            page1FieldRowListRoot,
            "TIN ID",
            data.tin,
            DataValueType.Text,
            "TIN"
        );

        // Taxpayer Classification
        // Uses data.residencyStatus internally,
        // but displays "Taxpayer Classification" in the UI.
        AddEnumRow(
            page1Rows,
            page1FieldRowListRoot,
            "Taxpayer Classification",
            "ResidencyStatus",
            data,
            () => data.residencyStatus?.ToString(),
            v => data.residencyStatus =
                (ResidencyStatus)System.Enum.Parse(
                    typeof(ResidencyStatus),
                    v
                )
        );



        // Taxpayer Type
        AddEnumRow(
            page1Rows,
            page1FieldRowListRoot,
            "Taxpayer Type",
            "TaxpayerType",
            data,
            () => data.taxpayerType?.ToString(),
            v => data.taxpayerType =
                (TaxpayerType)System.Enum.Parse(
                    typeof(TaxpayerType),
                    v
                )
        );

        // Income Sources
        AddEnumRow(
            page1Rows,
            page1FieldRowListRoot,
            "Income Sources",
            "IncomeSource",
            data,
            () => data.incomeSource?.ToString(),
            v => data.incomeSource =
                (IncomeSource)System.Enum.Parse(
                    typeof(IncomeSource),
                    v
                )
        );
    }

    private void BuildPage2FixedRows(CaseData data)
    {
        ClearRows(page2FixedRows);

        AddSourceOnlyRow(
            page2FixedRows,
            page2FixedFieldRowListRoot,
            "Full Name",
            data.fullName,
            DataValueType.Text,
            "FullName"
        );

        AddSourceOnlyRow(
            page2FixedRows,
            page2FixedFieldRowListRoot,
            "TIN",
            data.tin,
            DataValueType.Text,
            "TIN"
        );

        AddSourceOnlyRow(
            page2FixedRows,
            page2FixedFieldRowListRoot,
            "Birthdate",
            data.birthdate,
            DataValueType.Text,
            "Birthdate"
        );

        AddSourceOnlyRow(
            page2FixedRows,
            page2FixedFieldRowListRoot,
            "Address",
            data.address,
            DataValueType.Text,
            "Address"
        );

        AddSourceOnlyRow(
            page2FixedRows,
            page2FixedFieldRowListRoot,
            "Contact Number",
            data.contactNumber,
            DataValueType.Text,
            "ContactNumber"
        );

        AddSourceOnlyRow(
            page2FixedRows,
            page2FixedFieldRowListRoot,
            "Civil Status",
            data.civilStatus.ToString(),
            DataValueType.Text,
            "CivilStatus"
        );

        if (data.civilStatus == CivilStatus.Married)
        {
            AddSourceOnlyRow(
                page2FixedRows,
                page2FixedFieldRowListRoot,
                "Spouse Name",
                data.spouseName,
                DataValueType.Text,
                "SpouseName"
            );

            AddSourceOnlyRow(
                page2FixedRows,
                page2FixedFieldRowListRoot,
                "Spouse TIN",
                data.spouseTin,
                DataValueType.Text,
                "SpouseTIN"
            );
        }

        AddSourceOnlyRow(
            page2FixedRows,
            page2FixedFieldRowListRoot,
            "Citizenship",
            data.citizenship,
            DataValueType.Text,
            "Citizenship"
        );
    }

    private void BuildPage2Rows(CaseData data)
    {
        ClearRows(page2Rows);

        AddEnumRow(
            page2Rows,
            page2FieldRowListRoot,
            "Residency Status",
            "ResidencyStatus",
            data,
            () => data.residencyStatus?.ToString(),
            v => data.residencyStatus =
                (ResidencyStatus)System.Enum.Parse(
                    typeof(ResidencyStatus),
                    v
                )
        );

        AddEnumRow(
            page2Rows,
            page2FieldRowListRoot,
            "Taxpayer Type",
            "TaxpayerType",
            data,
            () => data.taxpayerType?.ToString(),
            v => data.taxpayerType =
                (TaxpayerType)System.Enum.Parse(
                    typeof(TaxpayerType),
                    v
                )
        );
    }

    private void BuildPage3Rows(CaseData data)
    {
        ClearRows(page3Rows);

        AddEnumRow(
            page3Rows,
            page3FieldRowListRoot,
            "Income Source",
            "IncomeSource",
            data,
            () => data.incomeSource?.ToString(),
            v => data.incomeSource =
                (IncomeSource)System.Enum.Parse(
                    typeof(IncomeSource),
                    v
                )
        );

        AddEnumRow(
            page3Rows,
            page3FieldRowListRoot,
            "Number of Employers",
            "EmployerCount",
            data,
            () => data.numberOfEmployers?.ToString(),
            v => data.numberOfEmployers =
                (EmployerCount)System.Enum.Parse(
                    typeof(EmployerCount),
                    v
                )
        );

        AddEnumRow(
            page3Rows,
            page3FieldRowListRoot,
            "Business Registration",
            "BusinessRegistration",
            data,
            () => data.businessRegistration?.ToString(),
            v => data.businessRegistration =
                (BusinessRegistration)System.Enum.Parse(
                    typeof(BusinessRegistration),
                    v
                )
        );

        AddEnumRow(
            page3Rows,
            page3FieldRowListRoot,
            "Tax Option",
            "TaxOption",
            data,
            () => data.taxOption?.ToString(),
            v => data.taxOption =
                (TaxOption)System.Enum.Parse(
                    typeof(TaxOption),
                    v
                )
        );
    }

    private void BuildPage4Rows(CaseData data)
    {
        ClearRows(page4Rows);

        AddNumberRow(
            page4Rows,
            page4FieldRowListRoot,
            "Gross Income",
            "GrossIncome",
            data,
            () => data.grossIncome,
            v => data.grossIncome = v
        );

        AddNumberRow(
            page4Rows,
            page4FieldRowListRoot,
            "Allowable Expenses",
            "AllowableExpenses",
            data,
            () => data.allowableExpenses,
            v => data.allowableExpenses = v
        );

        AddNumberRow(
            page4Rows,
            page4FieldRowListRoot,
            "Taxable Income",
            "TaxableIncome",
            data,
            () => data.taxableIncome,
            v => data.taxableIncome = v
        );

        AddNumberRow(
            page4Rows,
            page4FieldRowListRoot,
            "Tax Due",
            "TaxDue",
            data,
            () => data.taxDue,
            v => data.taxDue = v
        );

        AddNumberRow(
            page4Rows,
            page4FieldRowListRoot,
            "Tax Credits",
            "TaxCredits",
            data,
            () => data.taxWithheldOrCredits,
            v => data.taxWithheldOrCredits = v
        );

        AddNumberRow(
            page4Rows,
            page4FieldRowListRoot,
            "Final Tax Payable",
            "FinalTaxPayable",
            data,
            () => data.finalTaxPayable,
            v => data.finalTaxPayable = v
        );
    }

    private void BuildPage5Rows(CaseData data)
    {
        ClearRows(page5Rows);

        AddEnumRow(
            page5Rows,
            page5FieldRowListRoot,
            "Required Form",
            "RequiredForm",
            data,
            () => data.requiredForm?.ToString(),
            v => data.requiredForm =
                (RequiredForm)System.Enum.Parse(
                    typeof(RequiredForm),
                    v
                )
        );
    }

    private void BuildPage7Rows(CaseData data)
    {
        ClearRows(page7Rows);

        AddSourceOnlyRow(
            page7Rows,
            page7FieldRowListRoot,
            "Residency Status (on file)",
            data.residencyStatus.HasValue
                ? data.residencyStatus.ToString()
                : "?",
            DataValueType.Text,
            "FindingsResidencyStatus"
        );

        AddSourceOnlyRow(
            page7Rows,
            page7FieldRowListRoot,
            "Taxpayer Type (on file)",
            data.taxpayerType.HasValue
                ? data.taxpayerType.ToString()
                : "?",
            DataValueType.Text,
            "FindingsTaxpayerType"
        );

        for (int i = 0;
             i < data.potentialIssuesIdentified.Count;
             i++)
        {
            AddSourceOnlyRow(
                page7Rows,
                page7FieldRowListRoot,
                $"Issue {i + 1}",
                data.potentialIssuesIdentified[i],
                DataValueType.Text,
                $"Issue{i}"
            );
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private void AddEnumRow(
        List<CaseFolderFieldRow> rowList,
        Transform parent,
        string label,
        string key,
        CaseData data,
        System.Func<string> getCurrent,
        System.Action<string> setValue)
    {
        bool known =
            data.folderTransferredFields.Contains(key);

        string current =
            getCurrent();

        var rowObj =
            Instantiate(
                caseFolderFieldRowPrefab,
                parent
            );

        rowObj.Initialize(
            label,
            current,
            known,
            DataValueType.Enum,
            key,
            v =>
            {
                setValue(v.ToString());

                data.folderTransferredFields.Add(key);

                DebugTrace.Log(
                    $"[CaseData Updated] " +
                    $"Field: {key} | Value: {v}"
                );
            }
        );

        rowList.Add(rowObj);
    }

    private void AddNumberRow(
        List<CaseFolderFieldRow> rowList,
        Transform parent,
        string label,
        string key,
        CaseData data,
        System.Func<float> getCurrent,
        System.Action<float> setValue)
    {
        bool known =
            data.folderTransferredFields.Contains(key);

        float current =
            getCurrent();

        var rowObj =
            Instantiate(
                caseFolderFieldRowPrefab,
                parent
            );

        rowObj.Initialize(
            label,
            current,
            known,
            DataValueType.Number,
            key,
            v =>
            {
                float value =
                    System.Convert.ToSingle(v);

                setValue(value);

                data.folderTransferredFields.Add(key);

                DebugTrace.Log(
                    $"[CaseData Updated] " +
                    $"Field: {key} | Value: {value}"
                );
            }
        );

        rowList.Add(rowObj);
    }

    private void AddSourceOnlyRow(
        List<CaseFolderFieldRow> rowList,
        Transform parent,
        string label,
        string value,
        DataValueType type,
        string key)
    {
        var rowObj =
            Instantiate(
                caseFolderFieldRowPrefab,
                parent
            );

        rowObj.InitializeSourceOnly(
            label,
            value,
            type,
            key
        );

        rowList.Add(rowObj);
    }

    private void ClearRows(
        List<CaseFolderFieldRow> rowList)
    {
        foreach (var row in rowList)
        {
            if (row != null)
            {
                // Destroy is deferred; clear feedback and raycasts immediately.
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }
        }

        rowList.Clear();
    }
}
