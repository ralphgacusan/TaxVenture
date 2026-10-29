using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>A removable finding slip. The folder owns it until lifted; its existing
/// FloatingWindow then shares focus, dragging and compositing with the other papers.</summary>
public sealed class Level2FindingsSheet : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler, ICanvasRaycastFilter
{
    private static readonly Color Ink = new Color32(41, 53, 67, 255);
    private static readonly Color Muted = new Color32(110, 112, 106, 255);
    private static readonly Color Tape = new Color32(230, 212, 159, 218);
    private const float LooseScale = 0.8f;
    // Attached, the paper takes a little over half of the folder page so its title and edges stay visible.
    private const float AttachedWidthFit = 0.6f, AttachedHeightFit = 0.58f;
    // Record papers print fewer, larger rows than the three Findings fields.
    internal const float RecordRowTop = 183f, RecordRowStep = 104f, RecordRowHeight = 96f;
    private int recordRows;
    private Quaternion looseTilt = Quaternion.identity;
    private float looseScale = LooseScale;
    private float paperHeight = 780f;
    private CaseFolderUI.PageBuilder page;
    private Action<CaseFolderUI.PageBuilder, RectTransform> draw;
    private Action<Level2FindingsSheet> lifted;
    private Action<Level2FindingsSheet> check;
    private Action<Level2FindingsSheet> filed;
    private Level2SupportingStack supportingStack;
    private CaseFolderUI[] folders;
    private string paperTitle = "Findings";
    private string paperId;
    private string returnPageTitle = Level2FindingsPage.PageTitle;
    private bool recordPaper;
    private RectTransform paper, rows, tape, visual;
    private TextMeshProUGUI hint;
    private TextMeshProUGUI comparisonStatus, observation;
    private Image comparisonMark;
    private Coroutine writing;
    private FloatingWindow window;
    private Canvas overlay;
    private CanvasGroup group;
    private CanvasGroup fieldGroup;
    private CaseData boundCase;
    private Coroutine motion;
    private bool returning;
    private bool pointerDragging;
    private float presentationScale = 1f;
    private int renderedRevision = -1;
    private bool renderedPreliminary;
    private Image returnGlow;
    private Shadow castShadow;
    private static readonly Vector2 RestShadow = new Vector2(8f, -10f), LiftedShadow = new Vector2(16f, -22f);
    // T18: a source record prints its fixed supporting-document type (SupportingPaperTypes): header band,
    // owner/context lines, its rows, then a total band, payment status or signatures; more rows than one
    // face holds continue on another face of the same paper.
    private const float SignatureSpace = 92f;
    private SupportingPaperFace face;
    private int faceIndex;
    private int faceCount = 1;
    private RectTransform totalBand;
    private TextMeshProUGUI faceTab;
    private Coroutine faceTurn;
    public bool IsDetached { get; private set; }
    /// <summary>The face of a long record now showing (0 = the first).</summary>
    public int FaceIndex => faceIndex;
    public int FaceCount => faceCount;
    /// <summary>What this record prints (null for the Findings slip).</summary>
    public SupportingPaperFace Face => face;
    public string PaperId => paperId;
    public bool CanTransfer => IsDetached && !returning && gameObject.activeInHierarchy;
    internal bool ReadyToCompleteGroupFile => IsDetached && returning && motion == null;
    internal float LooseTilt => visual != null ? visual.localEulerAngles.z : 0f;
    internal bool IsRecord => recordPaper;

    public static Level2FindingsSheet Create(CaseFolderUI.PageBuilder page, Level2SupportingStack stack,
        Action<CaseFolderUI.PageBuilder, RectTransform> draw, Action<Level2FindingsSheet> lifted,
        Action<Level2FindingsSheet> check) =>
        CreatePaper(page, stack, "Findings", null, Level2FindingsPage.PageTitle, false,
            draw, lifted, check, null);

    /// <summary>
    /// Creates a detachable source record using the findings paper's lift, focus and return motion.
    /// T18: face is the record's supporting-document type print; rowCount is the rows one face shows
    /// and faceCount how many faces its table needs.
    /// </summary>
    public static Level2FindingsSheet CreateRecord(CaseFolderUI.PageBuilder page, Level2SupportingStack stack,
        string id, string title,
        Action<CaseFolderUI.PageBuilder, RectTransform> draw, Action<Level2FindingsSheet> lifted,
        Action<Level2FindingsSheet> filed, int rowCount = 0, SupportingPaperFace face = null, int faceCount = 1) =>
        CreatePaper(page, stack, title, id, Level2FindingsPage.PageTitle, true, draw, lifted, null, filed, rowCount, face, faceCount);

    private static Level2FindingsSheet CreatePaper(CaseFolderUI.PageBuilder page, Level2SupportingStack stack,
        string title, string id,
        string returnPage, bool isRecord, Action<CaseFolderUI.PageBuilder, RectTransform> draw,
        Action<Level2FindingsSheet> lifted, Action<Level2FindingsSheet> check,
        Action<Level2FindingsSheet> filed, int rowCount = 0, SupportingPaperFace face = null, int faceCount = 1)
    {
        RectTransform root = HudTheme.CreateRect(isRecord ? "Attached Record Paper " + id : "Removable Findings Sheet", page.Paper);
        root.sizeDelta = new Vector2(480f, 624f);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(0f, -12f);
        float scale = Mathf.Min(page.Paper.rect.width * AttachedWidthFit / 480f, page.Paper.rect.height * AttachedHeightFit / 624f);
        root.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        root.localRotation = Quaternion.Euler(0f, 0f, -1.5f);
        var sheet = root.gameObject.AddComponent<Level2FindingsSheet>();
        sheet.paper = root;
        sheet.page = page;
        sheet.supportingStack = stack;
        sheet.draw = draw;
        sheet.lifted = lifted;
        sheet.check = check;
        sheet.filed = filed;
        sheet.paperTitle = title;
        sheet.paperId = id;
        sheet.returnPageTitle = returnPage;
        sheet.recordPaper = isRecord;
        sheet.recordRows = isRecord ? Mathf.Max(0, rowCount) : 0;
        sheet.face = isRecord ? face : null;
        sheet.faceCount = Mathf.Max(1, faceCount);
        sheet.boundCase = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        sheet.Build();
        stack?.RegisterPaper(sheet);
        root.sizeDelta = new Vector2(480f, sheet.paperHeight * 0.8f);
        root.localScale = Vector3.one * Mathf.Min(page.Paper.rect.width * AttachedWidthFit / 480f,
            page.Paper.rect.height * AttachedHeightFit / root.sizeDelta.y);
        page.AddOverlay(root.gameObject);
        return sheet;
    }

    private void Build()
    {
        // A full working sheet when loose; a smaller preview while taped into the folder.
        visual = HudTheme.CreateRect("Printed Findings Slip", paper);
        visual.sizeDelta = new Vector2(600f, 780f);
        visual.localScale = Vector3.one * 0.8f;
        Color sheetColor = face != null ? Hex(face.Style.Paper) : (Color)new Color32(253, 247, 226, 255);
        Image background = NotesPaperView.BuildPaperStack(visual,
            new Color32(224, 214, 190, 255), sheetColor);
        foreach (Image sheet in visual.GetComponentsInChildren<Image>())
            sheet.gameObject.AddComponent<FindingsPaperPerspective>();
        background.raycastTarget = true;
        castShadow = background.GetComponent<Shadow>();
        castShadow.effectColor = new Color(0.08f, 0.07f, 0.05f, 0.28f);
        castShadow.effectDistance = new Vector2(8f, -10f);
        Rule(visual, "Lit top edge", 2f, 1f, 596f, 3f, new Color(1f, 1f, 1f, 0.85f));
        Rule(visual, "Paper thickness", 2f, 777f, 596f, 3f, new Color32(178, 160, 126, 255));
        if (face != null) BuildRecordFace();
        else
        {
            Label(visual, paperTitle, 56f, 38f, 488f, 57f, 44f, Ink, FontStyles.Bold);
            var guide = Label(visual, recordPaper ? "Original record. Compare it with the summary." : "A clue may explain something on another page.",
                56f, 108f, 488f, 52f, 27f, Muted);
            guide.textWrappingMode = TextWrappingModes.Normal;
        }
        Rule(visual, "Header rule", 56f, 166f, 488f, 2f, Ink);
        rows = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Finding Fields", visual), 56f, 183f, 488f, 275f);
        fieldGroup = rows.gameObject.AddComponent<CanvasGroup>();
        fieldGroup.blocksRaycasts = false;
        fieldGroup.interactable = false;
        comparisonMark = HudTheme.AddImage(HudTheme.PlaceTopLeft(HudTheme.CreateRect("Review ink", visual),
            56f, 480f, 4f, 228f), null, Muted);
        comparisonStatus = Label(visual, "", 70f, 475f, 475f, 37f, 28f, Muted, FontStyles.Bold);
        observation = Label(visual, "", 70f, 519f, 475f, 198f, 28f, Ink);
        observation.textWrappingMode = TextWrappingModes.Normal;
        observation.enableAutoSizing = false;
        observation.font = HudTheme.HandwritingFont;
        string conclusion = CaseManager.Instance != null
            ? CaseManager.Instance.CurrentDefinition?.review?.preliminaryRule?.supportedObservation : null;
        float extra = 0f;
        if (!recordPaper)
        {
            float noteHeight = Mathf.Max(198f, Mathf.Ceil(observation.GetPreferredValues(conclusion ?? "", 475f, Mathf.Infinity).y) + 24f);
            extra = noteHeight - 198f;
            paperHeight += extra;
            observation.rectTransform.sizeDelta = new Vector2(475f, noteHeight);
            ((RectTransform)visual.Find("Paper thickness")).anchoredPosition += Vector2.down * extra;
            comparisonMark.rectTransform.sizeDelta += Vector2.up * extra;
        }
        else
        {
            comparisonMark.gameObject.SetActive(false);
            comparisonStatus.gameObject.SetActive(false);
            observation.gameObject.SetActive(false);
            // A record is as tall as its rows plus the footer hint: no empty lower half.
            if (recordRows > 0)
            {
                float footer = face != null && face.Style.HasSignatures ? SignatureSpace : 0f;
                extra = RecordRowTop + recordRows * RecordRowStep + 74f + footer - paperHeight;
                paperHeight += extra;
                ((RectTransform)visual.Find("Paper thickness")).anchoredPosition += Vector2.down * extra;
                if (footer > 0f) BuildSignatures(RecordRowTop + recordRows * RecordRowStep + 6f);
            }
        }
        visual.sizeDelta = new Vector2(600f, paperHeight);
        hint = Label(visual, recordPaper ? "Lift the stack to read" : "Lift to edit", 56f, 738f + extra, 488f, 30f, 22f, Muted);
        if (face != null && faceCount > 1) BuildFaceTab(738f + extra);
        tape = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Removable tape", visual), 208f, -17f, 184f, 49f);
        tape.localRotation = Quaternion.Euler(0f, 0f, 4f);
        HudTheme.AddImage(tape, null, Tape);
        Rule(tape, "Tape grain", 7f, 10f, 170f, 1f, new Color(1f, 1f, 1f, 0.32f));
        group = gameObject.AddComponent<CanvasGroup>();
        TaperPrintedDetails();
        RefreshContent();
    }

    private void TaperPrintedDetails()
    {
        foreach (RectTransform detail in visual)
        {
            if (detail == rows || detail.name.StartsWith("Sheet")) continue;
            float width = FindingsPaperPerspective.WidthAt(-detail.anchoredPosition.y / paperHeight);
            detail.anchoredPosition = new Vector2(300f + (detail.anchoredPosition.x - 300f) * width, detail.anchoredPosition.y);
            detail.localScale = new Vector3(width, 1f, 1f);
        }
    }

    public void RefreshContent()
    {
        if (rows == null) return;
        if (recordPaper)
        {
            draw(page, rows);
            PlaceTotalBand();
            return;
        }
        bool stamped = boundCase?.review?.preliminary != null;
        if (stamped != renderedPreliminary)
        {
            for (int i = rows.childCount - 1; i >= 0; i--)
            {
                Transform child = rows.GetChild(i);
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }
        draw(page, rows);
        int revision = boundCase?.review?.packetRevision ?? 0;
        if (revision != renderedRevision || stamped != renderedPreliminary)
        {
            PreliminaryRuleDefinition rule = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition?.review?.preliminaryRule : null;
            ReviewFindingState finding = rule != null ? boundCase?.review?.FindFinding(rule.findingId) : null;
            if (finding != null && (finding.comparisonConfirmed || stamped))
                ShowComparison(FindingComparison.Result.Supported, rule.supportedObservation, false);
            else ResetComparison();
        }
        renderedRevision = boundCase?.review?.packetRevision ?? 0;
        renderedPreliminary = boundCase?.review?.preliminary != null;
    }

    // Long picks such as "Payment for: Blanket washing" shrink to fit their box instead of being cut off.
    public static void StyleRow(GameObject row, int index) => StyleRow(row, index, 183f, 94f, 86f, 34f, 21f);

    /// <summary>A record paper row: fewer, larger lines than the Findings fields.</summary>
    public static void StyleRecordRow(GameObject row, int index) =>
        StyleRow(row, index, RecordRowTop, RecordRowStep, RecordRowHeight, 40f, 33f);

    private static void StyleRow(GameObject row, int index, float top, float step, float height, float size, float minSize)
    {
        // These are the same source/destination components and prefab as the folder.
        // Fixed paper spacing avoids inheriting the folder list's layout controller.
        var rect = row.transform as RectTransform;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        // The prefab normally sizes itself in the folder's vertical list.
        // On the slip, fixed boxes own the layout instead.
        foreach (LayoutGroup layout in row.GetComponents<LayoutGroup>()) layout.enabled = false;
        foreach (ContentSizeFitter fitter in row.GetComponentsInChildren<ContentSizeFitter>(true)) fitter.enabled = false;
        HudTheme.PlaceTopLeft(rect, 0f, index * step, 488f, height);
        Level2FindingsSheet sheet = row.GetComponentInParent<Level2FindingsSheet>();
        float taper = FindingsPaperPerspective.WidthAt((top + index * step + height * 0.5f) / (sheet != null ? sheet.paperHeight : 780f));
        rect.localScale = new Vector3(taper, 1f, 1f);
        rect.anchoredPosition += Vector2.right * (488f * (1f - taper) * 0.5f);
        foreach (TMP_Text text in row.GetComponentsInChildren<TMP_Text>(true))
        {
            HudTheme.Stretch(text.rectTransform, 8f);
            text.font = HudTheme.Font;
            text.fontSize = size;
            text.fontSizeMin = minSize;
            text.fontSizeMax = size;
            text.enableAutoSizing = true;
            text.textWrappingMode = TextWrappingModes.Normal;
        }
    }

    public static void DrawEmptyMount(CaseFolderUI.PageBuilder page)
    {
        DrawEmptyMount(page, "Slip on desk\n\nDrop it here to file it.");
    }

    public static void DrawEmptyMount(CaseFolderUI.PageBuilder page, string message)
    {
        RectTransform mount = HudTheme.CreateRect("Findings sheet mount", page.Paper);
        mount.sizeDelta = new Vector2(page.Paper.rect.width * 0.78f, page.Paper.rect.height * 0.68f);
        Image tint = HudTheme.AddImage(mount, null, new Color(0.55f, 0.48f, 0.32f, 0.07f));
        tint.raycastTarget = false;
        var text = HudTheme.CreateLabel("Empty mount hint", mount,
            message,
            Mathf.Max(16f, mount.sizeDelta.x * 0.045f), Muted, TextAlignmentOptions.Center);
        HudTheme.Stretch(text.rectTransform, 20f);
        page.AddOverlay(mount.gameObject);
    }

    // Stop the attached slip's grip from starting a drag of its parent folder.
    public void OnPointerDown(PointerEventData e)
    {
        if (IsDetached)
            supportingStack?.PaperPressed(this, e);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!IsDetached && !e.dragging && e.button == PointerEventData.InputButton.Left)
        {
            if (supportingStack != null) supportingStack.LiftStack(e.position);
            else Lift(e.position);
        }
        else if (IsDetached)
        {
            window?.BringToFront();
        }
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (returning || e.button != PointerEventData.InputButton.Left) return;
        pointerDragging = true;
        if (!IsDetached)
        {
            if (supportingStack != null) supportingStack.BeginCoverDrag(e.position);
            else Lift(e.position);
        }
        if (supportingStack != null && supportingStack.BeginPaperDrag(this, e)) return;
        // Attached paper did not have a FloatingWindow on pointer down yet.
        if (window != null) window.OnPointerDown(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!IsDetached || returning || window == null) return;
        if (supportingStack != null && supportingStack.IsGroupMoving)
        {
            supportingStack.DragGroup(this, e.position);
            return;
        }
        window.OnDrag(e);
        if (motion == null)
            visual.localRotation = Quaternion.Slerp(visual.localRotation,
                looseTilt * Quaternion.Euler(0f, Mathf.Clamp(e.delta.x * 0.02f, -2f, 2f), 0f), 0.3f);
        supportingStack?.PaperDragged(this, e.position);
    }

    public void OnEndDrag(PointerEventData e)
    {
        pointerDragging = false;
        if (!IsDetached || returning || window == null) return;
        if (supportingStack != null && supportingStack.PaperReleased(this, e.position)) return;
        if (supportingStack == null)
        {
            CaseFolderUI target = FindFolder(e.position);
            if (target != null)
            {
                window.OnPointerUp(e);
                FileIntoFolder(target);
                return;
            }
        }
        window.OnPointerUp(e);
        if (motion != null) StopCoroutine(motion);
        motion = StartCoroutine(SettleOnDesk());
        Sound();
        KeepReachable();
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (pointerDragging || !IsDetached || returning) return;
        supportingStack?.PaperReleased(this, e.position);
    }

    private IEnumerator SettleOnDesk()
    {
        Quaternion fromTilt = visual.localRotation;
        Vector3 fromScale = visual.localScale;
        for (float elapsed = 0f; elapsed < 0.28f; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(elapsed / 0.28f);
            float ease = t * t * (3f - 2f * t);
            visual.localRotation = Quaternion.Slerp(fromTilt, looseTilt, ease);
            visual.localScale = Vector3.Lerp(fromScale, Vector3.one * looseScale, ease) *
                (1f + 0.012f * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
        visual.localRotation = looseTilt;
        visual.localScale = Vector3.one * looseScale;
        motion = null;
    }

    private void Lift(Vector2 position, bool refreshPages = true)
    {
        if (IsDetached || page.Owner == null) return;
        // Capture the world-space sheet's projected pose before leaving its page.
        Camera camera = PageCamera(page.Owner);
        Quaternion attachedTilt = DeskTilt(paper, camera);
        looseTilt = attachedTilt * Quaternion.Euler(-3f, 0f, 0f);
        Vector2 centre = RectTransformUtility.WorldToScreenPoint(camera, paper.position);
        Vector3[] corners = new Vector3[4];
        paper.GetWorldCorners(corners);
        float startHeight = Vector2.Distance(RectTransformUtility.WorldToScreenPoint(camera, corners[0]),
            RectTransformUtility.WorldToScreenPoint(camera, corners[1]));
        page.ReleaseOverlay(gameObject); // page turns must never destroy the lifted paper
        IsDetached = true;
        lifted?.Invoke(this);
        if (!recordPaper) GameplayEvents.RaiseGuideCue("FindingsPaperLifted");
        supportingStack?.PaperDetached(this);
        folders = FindObjectsByType<CaseFolderUI>(FindObjectsInactive.Include);
        RectTransform host = HudTheme.CreateRect("Level 2 Findings Paper Canvas", null);
        host.gameObject.layer = gameObject.layer;
        overlay = host.gameObject.AddComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = host.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        host.gameObject.AddComponent<GraphicRaycaster>();
        paper.SetParent(host, false);
        paper.sizeDelta = new Vector2(600f, paperHeight) * looseScale;
        paper.anchorMin = paper.anchorMax = paper.pivot = new Vector2(0.5f, 0.5f);
        paper.localRotation = Quaternion.identity;
        Canvas.ForceUpdateCanvases();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(host, centre, null, out Vector2 local);
        paper.anchoredPosition = local;
        paper.localScale = Vector3.one;
        window = gameObject.AddComponent<FloatingWindow>();
        window.UsePaperMotion(0f);
        window.UsesPointRaycastFiltering = true;
        gameObject.AddComponent<GraphicRaycaster>();
        window.BringToFront();
        fieldGroup.blocksRaycasts = true;
        fieldGroup.interactable = true;
        tape.anchoredPosition += new Vector2(0f, -23f);
        tape.localRotation = Quaternion.Euler(0f, 0f, -11f);
        hint.text = supportingStack != null ? "Hold still for 3 seconds to gather all papers" : "Drop it on the folder to file it";
        RectTransform glow = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Return highlight", visual), 34f, paperHeight - 52f, 532f, 3f);
        returnGlow = HudTheme.AddImage(glow, null, Color.clear);
        // Animate the printed face, leaving the shared window's drag coordinates stable.
        float startScale = Mathf.Clamp(startHeight / Mathf.Max(1f, paperHeight * overlay.scaleFactor * ProjectedHeight(attachedTilt)), 0.2f, 1.8f);
        // More writing room must not indirectly shrink the loose note through
        // the folder preview's fit scale.
        looseScale = Mathf.Max(startScale * 1.06f, LooseScale);
        paper.sizeDelta = new Vector2(600f, paperHeight) * looseScale;
        motion = StartCoroutine(Peel(startScale, attachedTilt));
        Sound();
        if (refreshPages) RefreshShowingFolders();
        KeepReachable();
        check?.Invoke(this);
    }

    internal void LiftForStack(Vector2 position) => Lift(position, false);

    internal void RefreshShowingFolders()
    {
        if (folders == null) folders = FindObjectsByType<CaseFolderUI>(FindObjectsInactive.Include);
        foreach (CaseFolderUI folder in folders)
            if (folder != null && folder.IsShowing) folder.RefreshCurrentPage();
    }

    internal Vector2 LooseSize => paper != null ? paper.rect.size : Vector2.zero;
    internal Vector2 LoosePosition => paper != null ? paper.anchoredPosition : Vector2.zero;

    internal Vector2 ScreenToLoosePosition(Vector2 screen)
    {
        RectTransform host = overlay != null ? overlay.transform as RectTransform : null;
        Vector2 local = Vector2.zero;
        if (host != null) RectTransformUtility.ScreenPointToLocalPointInRectangle(host, screen, null, out local);
        return local;
    }

    internal Vector2 LooseToScreenPosition(Vector2 local)
    {
        RectTransform host = overlay != null ? overlay.transform as RectTransform : null;
        return host != null ? RectTransformUtility.WorldToScreenPoint(null, host.TransformPoint(local)) : Vector2.zero;
    }

    internal Rect LooseCanvasRect => overlay != null && overlay.transform is RectTransform host ? host.rect : default;

    internal void SetLoosePose(Vector2 localPosition, float scale, float tilt)
    {
        if (paper == null || visual == null) return;
        paper.anchoredPosition = localPosition;
        presentationScale = scale;
        paper.localScale = Vector3.one * scale;
        window?.SetPaperRestScale(scale);
        visual.localRotation = Quaternion.Euler(0f, 0f, tilt);
    }

    internal void FocusLoosePaper() => window?.BringToFront();

    internal CaseFolderUI FindFolderAt(Vector2 screen) => FindFolder(screen);

    /// <summary>
    /// Files this paper: it glides toward the folder and hovers while the
    /// folder turns to its page, then settles onto that page and attaches.
    /// If the folder closes first, it stays loose where it was.
    /// </summary>
    private void FileIntoFolder(CaseFolderUI folder)
    {
        if (!CanTransfer || folder == null) return;
        BeginGroupFile();
        motion = StartCoroutine(FileWhenPageReady(folder));
    }

    private IEnumerator FileWhenPageReady(CaseFolderUI folder)
    {
        int index = folder.FindPageIndex(returnPageTitle);
        if (index < 0) { CancelReturn(); yield break; }
        RectTransform host = overlay.transform as RectTransform;
        Vector2 start = paper.anchoredPosition;
        Vector3 startScale = visual.localScale;
        bool turned = folder.CurrentPageIndex == index;
        if (!turned) folder.TurnToPage(index, () => turned = true);
        float waited = 0f;
        Sound();
        // Hover: drift most of the way to the folder, shrink a little and bob while the pages turn.
        while ((!turned || waited < 0.12f) && waited < 4f)
        {
            if (folder == null || !folder.IsShowing) { CancelReturn(); yield break; }
            waited += Time.unscaledDeltaTime;
            float t = DeskMotion.EaseOutCubic(Mathf.Clamp01(waited / 0.32f));
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(PageCamera(folder), folder.PaperRect.TransformPoint(Vector3.zero));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(host, screen, null, out Vector2 centre);
            paper.anchoredPosition = Vector2.Lerp(start, centre, 0.7f * t) + Vector2.up * (10f * Mathf.Sin(waited * 7f) * t + 24f * t);
            visual.localScale = Vector3.Lerp(startScale, startScale * 0.86f, t);
            if (castShadow != null) castShadow.effectDistance = Vector2.Lerp(RestShadow, LiftedShadow, t);
            yield return null;
        }
        if (!turned || folder == null || !folder.IsShowing) { CancelReturn(); yield break; }
        motion = null;
        bool done = false;
        motion = StartCoroutine(FileInto(folder, false, () => done = true));
        while (!done) yield return null;
        if (castShadow != null) castShadow.effectDistance = RestShadow;
        if (!ReadyToCompleteGroupFile) yield break;
        CompleteGroupFile();
        if (folder != null) folder.RefreshCurrentPage();
    }

    internal void BeginGroupFile()
    {
        if (!CanTransfer || returning) return;
        ClearSheetSelection();
        returning = true;
        fieldGroup.blocksRaycasts = false;
        fieldGroup.interactable = false;
        group.blocksRaycasts = false;
        GetComponent<GraphicRaycaster>().enabled = false;
        window.enabled = false;
        if (motion != null) StopCoroutine(motion);
        motion = null;
    }

    internal void FileAsPartOfStack(CaseFolderUI folder, Action completed) =>
        motion = StartCoroutine(FileInto(folder, false, completed));

    internal void CancelGroupFile()
    {
        if (!returning) return;
        if (motion != null) StopCoroutine(motion);
        CancelReturn();
    }

    internal void CompleteGroupFile()
    {
        if (!IsDetached || !returning) return;
        IsDetached = false;
        if (!recordPaper) GameplayEvents.RaiseGuideCue("FindingsPaperFiled");
        Dispose();
    }

    private IEnumerator Peel(float from, Quaternion attachedTilt)
    {
        // Peel up from the page: a quick lift with a soft overshoot, the shadow
        // growing as it rises, then settling to its resting pose.
        const float duration = 0.46f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float grow = DeskMotion.EaseOutBack(t, 0.9f);
            float rise = Mathf.Sin(t * Mathf.PI);
            visual.localScale = Vector3.one * Mathf.LerpUnclamped(from, looseScale, grow);
            visual.localRotation = Quaternion.Slerp(attachedTilt, looseTilt, DeskMotion.EaseOutCubic(t)) *
                Quaternion.Euler(rise * -3f, 0f, rise * -1.5f);
            visual.anchoredPosition = Vector2.up * (14f * rise);
            if (castShadow != null) castShadow.effectDistance = Vector2.Lerp(RestShadow, LiftedShadow, rise);
            yield return null;
        }
        visual.localScale = Vector3.one * looseScale;
        visual.localRotation = looseTilt;
        visual.anchoredPosition = Vector2.zero;
        if (castShadow != null) castShadow.effectDistance = RestShadow;
        motion = null;
    }

    private IEnumerator FileInto(CaseFolderUI folder, bool turnToPage, Action completed)
    {
        int index = folder.FindPageIndex(returnPageTitle);
        if (index < 0) { CancelReturn(); completed?.Invoke(); yield break; }
        if (turnToPage)
        {
            bool turned = false;
            folder.TurnToPage(index, () => turned = true);
            float waited = 0f;
            while (!turned && folder != null && folder.IsShowing && waited < 4f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (folder == null || !folder.IsShowing || !turned) { CancelReturn(); completed?.Invoke(); yield break; }
        }
        else if (folder == null || !folder.IsShowing || folder.CurrentPageIndex != index)
        {
            CancelReturn();
            completed?.Invoke();
            yield break;
        }
        RectTransform host = overlay.transform as RectTransform;
        Vector2 start = paper.anchoredPosition;
        Vector3 initialScale = visual.localScale;
        Vector3 initialPaperScale = paper.localScale;
        Quaternion initialTilt = visual.localRotation;
        for (float elapsed = 0f; elapsed < 0.36f; elapsed += Time.unscaledDeltaTime)
        {
            if (folder == null || !folder.IsShowing) { CancelReturn(); completed?.Invoke(); yield break; }
            float t = Mathf.Clamp01(elapsed / 0.36f);
            float ease = t * t * (3f - 2f * t);
            RectTransform target = folder.PaperRect;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(PageCamera(folder), target.TransformPoint(new Vector3(0f, -12f, 0f)));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(host, screen, null, out Vector2 destination);
            paper.anchoredPosition = Vector2.Lerp(start, destination, ease) + Vector2.up * (28f * Mathf.Sin(t * Mathf.PI));
            // Match the actual attached paper's projected size, including perspective.
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            float height = Vector2.Distance(RectTransformUtility.WorldToScreenPoint(PageCamera(folder), corners[0]),
                RectTransformUtility.WorldToScreenPoint(PageCamera(folder), corners[1]));
            float fit = Mathf.Min(target.rect.width * AttachedWidthFit / 480f, target.rect.height * AttachedHeightFit / (paperHeight * 0.8f));
            Quaternion targetTilt = DeskTilt(target, PageCamera(folder));
            float endScale = height * fit / Mathf.Max(1f, target.rect.height * overlay.scaleFactor * ProjectedHeight(targetTilt));
            visual.localScale = Vector3.Lerp(initialScale, Vector3.one * (0.8f * endScale), ease);
            paper.localScale = Vector3.Lerp(initialPaperScale, Vector3.one, ease);
            visual.localRotation = Quaternion.Slerp(initialTilt, targetTilt * Quaternion.Euler(0f, 0f, -1.5f), ease);
            yield return null;
        }
        motion = null;
        completed?.Invoke();
    }

    private void CancelReturn()
    {
        returning = false;
        group.blocksRaycasts = true;
        fieldGroup.blocksRaycasts = true;
        fieldGroup.interactable = true;
        window.enabled = true;
        GetComponent<GraphicRaycaster>().enabled = true;
        paper.localScale = Vector3.one * presentationScale;
        visual.localScale = Vector3.one * looseScale;
        visual.localRotation = looseTilt;
        KeepReachable();
        motion = null;
    }

    private CaseFolderUI FindFolder(Vector2 screen)
    {
        return FindFolderAt(screen, null);
    }

    internal CaseFolderUI FindFolderAtForStack(Vector2 screen, IList<Level2FindingsSheet> stack)
    {
        return FindFolderAt(screen, stack);
    }

    private CaseFolderUI FindFolderAt(Vector2 screen, IList<Level2FindingsSheet> stack)
    {
        if (folders == null) folders = FindObjectsByType<CaseFolderUI>(FindObjectsInactive.Include);
        foreach (CaseFolderUI folder in folders)
        {
            if (folder == null || !folder.IsShowing || folder.PaperRect == null || folder.FindPageIndex(returnPageTitle) < 0) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(folder.PaperRect, screen, PageCamera(folder)) && !Covered(folder, screen, stack)) return folder;
        }
        return null;
    }

    private bool Covered(CaseFolderUI folder, Vector2 screen, IList<Level2FindingsSheet> stack)
    {
        int folderOrder = 0;
        foreach (Doc3DLayer layer in Doc3DLayer.All)
            if (layer != null && layer.IsVisible && folder.PaperRect.IsChildOf(layer.Source))
                folderOrder = Mathf.Max(folderOrder, layer.SortingOrder);
        foreach (FloatingWindow other in FloatingWindow.Instances)
            if (other != null && other != window && folder.PaperRect.IsChildOf(other.transform))
                folderOrder = Mathf.Max(folderOrder, other.SortingOrder);
        foreach (FloatingWindow other in FloatingWindow.Instances)
            if (other != null && other != window && !IsInStack(other, stack) && other.IsOpen() && other.SortingOrder > folderOrder &&
                !folder.PaperRect.IsChildOf(other.transform) &&
                RectTransformUtility.RectangleContainsScreenPoint(other.WindowRect, screen, null)) return true;
        foreach (Doc3DLayer layer in Doc3DLayer.All)
            if (layer != null && layer.IsVisible && !IsInStack(layer, stack) && layer.SortingOrder > folderOrder &&
                !folder.PaperRect.IsChildOf(layer.Source) && layer.TryGetFootprint(out Rect footprint) && footprint.Contains(screen)) return true;
        return false;
    }

    private static bool IsInStack(FloatingWindow other, IList<Level2FindingsSheet> stack)
    {
        if (stack == null || other == null) return false;
        Level2FindingsSheet paper = other.GetComponent<Level2FindingsSheet>();
        return paper != null && stack.Contains(paper);
    }

    private static bool IsInStack(Doc3DLayer layer, IList<Level2FindingsSheet> stack)
    {
        if (stack == null || layer == null || layer.Source == null) return false;
        foreach (Level2FindingsSheet paper in stack)
            if (paper != null && (layer.Source == paper.transform || layer.Source.IsChildOf(paper.transform) || paper.transform.IsChildOf(layer.Source)))
                return true;
        return false;
    }

    public bool IsRaycastLocationValid(Vector2 screen, Camera eventCamera)
    {
        if (!IsDetached) return true;
        if (!CanTransfer || window == null) return false;
        if (!RectTransformUtility.RectangleContainsScreenPoint(visual, screen, null)) return false;
        foreach (Doc3DLayer layer in Doc3DLayer.All)
            if (layer != null && layer.IsVisible && layer.SortingOrder > window.SortingOrder &&
                layer.TryGetFootprint(out Rect footprint) && footprint.Contains(screen)) return false;
        return true;
    }

    private static Quaternion DeskTilt(RectTransform source, Camera camera)
    {
        if (camera == null) return Quaternion.identity;
        Vector3 right = camera.transform.InverseTransformDirection(source.TransformVector(Vector3.right).normalized);
        Vector3 up = camera.transform.InverseTransformDirection(source.TransformVector(Vector3.up).normalized);
        Vector3 normal = Vector3.Cross(right, up);
        Quaternion sourceTilt = normal.z > 0f ? Quaternion.LookRotation(normal, up) : Quaternion.identity;
        // Preserve the source plane rather than snapping to an unrelated desk angle.
        // The surface taper supplies the subtle perspective in overlay space.
        return sourceTilt;
    }

    private static float ProjectedHeight(Quaternion tilt)
    {
        Vector3 up = tilt * Vector3.up;
        return Mathf.Max(0.25f, new Vector2(up.x, up.y).magnitude);
    }

    private static Camera PageCamera(CaseFolderUI folder)
    {
        Canvas canvas = folder.PaperRect.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        if (canvas.worldCamera != null) return canvas.worldCamera;
        return CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
    }

    private void KeepReachable()
    {
        if (overlay == null) return;
        RectTransform host = overlay.transform as RectTransform;
        float halfX = Mathf.Max(0f, (host.rect.width - paper.rect.width) * 0.5f);
        float halfY = Mathf.Max(0f, (host.rect.height - paper.rect.height) * 0.5f);
        paper.anchoredPosition = new Vector2(Mathf.Clamp(paper.anchoredPosition.x, -halfX, halfX),
            Mathf.Clamp(paper.anchoredPosition.y, -halfY, halfY));
    }

    public void SyncVisibility()
    {
        if (!IsDetached) return;
        if (CaseManager.Instance == null || CaseManager.Instance.CurrentCase != boundCase) { Dispose(); return; }
        bool visible = false;
        foreach (CaseFolderUI folder in folders)
            if (folder != null && folder.IsShowing) { visible = true; break; }
        visible &= !InGameSettings.IsOpen;
        if (!visible && returning) { StopAllCoroutines(); CancelReturn(); }
        if (!visible && motion != null)
        {
            StopCoroutine(motion);
            motion = null;
            visual.localScale = Vector3.one * looseScale;
            visual.localRotation = looseTilt;
        }
        // Tuck away with the folder and pop back when it reopens (no instant blink).
        if (!visible && gameObject.activeSelf && !hiding)
        {
            if (gameObject.activeInHierarchy) tuck = StartCoroutine(TuckAway());
            else gameObject.SetActive(false);
        }
        else if (visible && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            StartCoroutine(PopBack());
        }
        else if (visible && hiding)
        {
            if (tuck != null) StopCoroutine(tuck);
            tuck = null;
            hiding = false;
            StartCoroutine(PopBack());
        }
        supportingStack?.OnOwnerVisibilityChanged(visible);
        if (visible && ((boundCase.review?.packetRevision ?? 0) != renderedRevision ||
            (boundCase.review?.preliminary != null) != renderedPreliminary)) RefreshContent();
    }

    private bool hiding;
    private Coroutine tuck;

    private IEnumerator TuckAway()
    {
        hiding = true;
        Vector3 from = visual.localScale;
        Vector2 start = visual.anchoredPosition;
        const float seconds = 0.2f;
        for (float elapsed = 0f; elapsed < seconds; elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float t = Mathf.Clamp01(elapsed / seconds);
            float e = t * t;
            visual.localScale = Vector3.Lerp(from, from * 0.7f, e);
            visual.anchoredPosition = start + Vector2.down * (40f * e);
            if (group != null) group.alpha = 1f - e;
            yield return null;
        }
        visual.localScale = Vector3.one * looseScale;
        visual.anchoredPosition = Vector2.zero;
        if (group != null) group.alpha = 1f;
        hiding = false;
        gameObject.SetActive(false);
    }

    private IEnumerator PopBack()
    {
        Vector3 rest = Vector3.one * looseScale;
        const float seconds = 0.3f;
        for (float elapsed = 0f; elapsed < seconds; elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            float t = Mathf.Clamp01(elapsed / seconds);
            visual.localScale = rest * Mathf.LerpUnclamped(0.75f, 1f, DeskMotion.EaseOutBack(t, 1.2f));
            visual.anchoredPosition = Vector2.down * (30f * (1f - DeskMotion.EaseOutCubic(t)));
            if (group != null) group.alpha = Mathf.Clamp01(t * 2.5f);
            yield return null;
        }
        visual.localScale = rest;
        visual.anchoredPosition = Vector2.zero;
        if (group != null) group.alpha = 1f;
    }

    public void Dispose()
    {
        ClearSheetSelection();
        supportingStack?.PaperDisposed(this);
        Action<Level2FindingsSheet> onFiled = filed;
        filed = null;
        onFiled?.Invoke(this);
        gameObject.SetActive(false);
        if (overlay != null) Destroy(overlay.gameObject);
        else Destroy(gameObject);
    }

    private void ClearSheetSelection()
    {
        ValueTransferManager manager = ValueTransferManager.Instance;
        if (manager != null && manager.SelectedSourceComponent is Component source &&
            source != null && source.transform.IsChildOf(transform)) manager.ClearSelection();
    }

    private void ResetComparison()
    {
        StopWriting();
        if (comparisonStatus == null || comparisonMark == null || observation == null) return;
        comparisonStatus.text = "Add three details";
        comparisonStatus.color = comparisonMark.color = Muted;
        observation.font = HudTheme.Font;
        observation.text = "";
    }

    public void ShowTransferHint(string part)
    {
        if (!CanTransfer) return;
        hint.text = part == ReviewFindingState.Reason ? "Reasons: Tax Code Book, Section 7" :
            part == ReviewFindingState.Concern ? "Entry: a line from the Summary page" : "Clue: a line from the Payment Note";
    }

    public void ShowComparison(FindingComparison.Result result, string conclusion, bool animate)
    {
        if (recordPaper || comparisonStatus == null || comparisonMark == null || observation == null) return;
        StopWriting();
        if (IsDetached) hint.text = supportingStack != null ? "Hold still for 3 seconds to gather all papers" : "Drop it on the folder to file it";
        bool supported = result == FindingComparison.Result.Supported;
        comparisonStatus.text = supported ? "Needs checking" :
            result == FindingComparison.Result.Incomplete ? "Add three details" : "No link yet";
        comparisonStatus.color = comparisonMark.color = result == FindingComparison.Result.Incomplete ? Muted :
            supported ? new Color32(47, 104, 78, 255) : new Color32(143, 78, 49, 255);
        observation.font = supported ? HudTheme.HandwritingFont : HudTheme.Font;
        observation.text = supported ? (string.IsNullOrWhiteSpace(conclusion)
            ? "The records raise a question. The original record is still needed." : conclusion)
            : result == FindingComparison.Result.Incomplete ? ""
            : string.IsNullOrWhiteSpace(conclusion) ? "These don't point to the same issue yet. Swap one box and compare again." : conclusion;
        if (animate && gameObject.activeInHierarchy) writing = StartCoroutine(RevealObservation(supported));
    }

    private IEnumerator RevealObservation(bool handwritten)
    {
        observation.ForceMeshUpdate();
        int letters = observation.textInfo.characterCount;
        observation.maxVisibleCharacters = handwritten ? 0 : int.MaxValue;
        float duration = handwritten ? Mathf.Clamp(letters / 65f, 0.5f, 2.8f) : 0.24f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            if (handwritten) observation.maxVisibleCharacters = Mathf.FloorToInt(letters * t);
            comparisonStatus.rectTransform.localScale = Vector3.one * (1f + 0.035f * Mathf.Sin(Mathf.Min(1f, elapsed / 0.24f) * Mathf.PI));
            yield return null;
        }
        observation.maxVisibleCharacters = int.MaxValue;
        comparisonStatus.rectTransform.localScale = Vector3.one;
        writing = null;
    }

    private void StopWriting()
    {
        if (writing != null) StopCoroutine(writing);
        writing = null;
        if (observation != null) observation.maxVisibleCharacters = int.MaxValue;
        if (comparisonStatus != null) comparisonStatus.rectTransform.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        StopWriting();
        // An interrupted face turn leaves the face readable (its rows were already swapped or never faded).
        if (faceTurn != null)
        {
            StopCoroutine(faceTurn);
            faceTurn = null;
        }
        if (fieldGroup != null) fieldGroup.alpha = 1f;
        if (rows != null) rows.anchoredPosition = new Vector2(56f, -RecordRowTop);
    }

    // =========================================================
    // T18: SUPPORTING-DOCUMENT TYPE PRINT (Types 4-7)
    // =========================================================

    private static Color Hex(uint rgb) =>
        new Color32((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF), 255);

    /// <summary>Header (icon + short title), then who it is from, its reference and date: the fixed reading order of every type.</summary>
    private void BuildRecordFace()
    {
        SupportingPaperStyle style = face.Style;
        Color accent = Hex(style.Header);
        if (style.Shape == SupportingPaperShape.Slip)
        {
            // A slip's torn-off top edge: a short perforation, no extra print.
            Color perforation = new Color(Muted.r, Muted.g, Muted.b, 0.35f);
            for (int i = 0; i < 22; i++) Rule(visual, "Perforation", 34f + i * 24f, 10f, 12f, 2f, perforation);
        }
        RectTransform band = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Type header", visual), 40f, 22f, 520f, 66f);
        HudTheme.AddImage(band, HudTheme.Rounded, accent, 10f).raycastTarget = false;
        Image icon = HudTheme.AddImage(HudTheme.PlaceTopLeft(HudTheme.CreateRect("Type icon", band), 14f, 11f, 44f, 44f),
            HudIconArt.Get("papers"), Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        TextMeshProUGUI title = Label(band, face.Title, 70f, 9f, 436f, 48f, 34f, Color.white, FontStyles.Bold);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        Fit(title, 24f, wrap: false);

        // Owner / context: long names wrap inside their own box (the box keeps its place; the text never turns into microprint).
        TextMeshProUGUI from = Label(visual, style.IssuerLabel + ": " + face.Issuer, 56f, 96f, 318f, 36f, 26f, Ink, FontStyles.Bold);
        Fit(from, 20f, wrap: true);
        TextMeshProUGUI date = Label(visual, face.Date, 380f, 96f, 164f, 36f, 24f, Muted);
        date.alignment = TextAlignmentOptions.TopRight;
        Fit(date, 18f, wrap: true);
        bool party = style.PartyLabel != null && face.Party.Length > 0;
        TextMeshProUGUI context = Label(visual, party ? style.PartyLabel + ": " + face.Party : "Ref " + face.Reference,
            56f, 132f, 318f, 32f, 23f, party ? Ink : Muted);
        Fit(context, 18f, wrap: true);
        if (face.Status.Length > 0) BuildStatus(accent);
        else if (style.HasColumns) RightNote("MONEY IN", FontStyles.Bold);
        else if (party) RightNote("Ref " + face.Reference, FontStyles.Normal);
        if (style.HasTotalBand)
        {
            // Behind the rows (made before them): the recorded total is the client's claim, not a confirmation.
            totalBand = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Total band", visual), 44f, RecordRowTop, 512f, RecordRowStep - 4f);
            HudTheme.AddImage(totalBand, HudTheme.Rounded, new Color(accent.r, accent.g, accent.b, 0.2f), 8f).raycastTarget = false;
            totalBand.gameObject.SetActive(false);
        }
    }

    private void RightNote(string text, FontStyles style)
    {
        TextMeshProUGUI note = Label(visual, text, 380f, 134f, 164f, 30f, 21f, Muted, style);
        note.alignment = TextAlignmentOptions.TopRight;
        Fit(note, 17f, wrap: true);
    }

    /// <summary>Type 5: a large, plain status the record itself supports ("PAID"), outlined in the type's colour.</summary>
    private void BuildStatus(Color accent)
    {
        RectTransform box = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Payment status", visual), 424f, 128f, 120f, 36f);
        Image frame = HudTheme.AddImage(box, HudTheme.Rounded, new Color(accent.r, accent.g, accent.b, 0.16f), 6f);
        frame.raycastTarget = false;
        Outline rim = frame.gameObject.AddComponent<Outline>();
        rim.effectColor = accent;
        rim.effectDistance = new Vector2(2f, -2f);
        TextMeshProUGUI label = Label(box, face.Status, 0f, 0f, 120f, 36f, 24f, Ink, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 6f;
        Fit(label, 16f, wrap: false);
    }

    /// <summary>Type 7: two party blocks with simple signature strokes under the terms.</summary>
    private void BuildSignatures(float top)
    {
        Color ink = new Color(Ink.r, Ink.g, Ink.b, 0.78f);
        string[] names = { face.Issuer, face.Party.Length > 0 ? face.Party : SupportingPaperTypes.Dash };
        for (int side = 0; side < 2; side++)
        {
            float x = 56f + side * 256f;
            // A short hand-drawn stroke: three slanted segments, never a real signature.
            for (int s = 0; s < 3; s++)
            {
                RectTransform stroke = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Signature stroke", visual), x + 12f + s * 52f, top + 20f, 62f, 3f);
                HudTheme.AddImage(stroke, null, ink).raycastTarget = false;
                stroke.localRotation = Quaternion.Euler(0f, 0f, s % 2 == 0 ? 14f : -12f);
            }
            Rule(visual, "Signature line", x, top + 44f, 216f, 2f, new Color(Muted.r, Muted.g, Muted.b, 0.6f));
            TextMeshProUGUI caption = Label(visual, names[side], x, top + 50f, 216f, 30f, 20f, Muted);
            Fit(caption, 16f, wrap: true);
        }
    }

    /// <summary>A long record's face tab ("Page 1 of 2"): a tap turns to its next face of the same type.</summary>
    private void BuildFaceTab(float y)
    {
        RectTransform tab = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Face tab", visual), 388f, y - 6f, 156f, 40f);
        Image tabImage = HudTheme.AddImage(tab, HudTheme.Rounded, new Color(Muted.r, Muted.g, Muted.b, 0.16f), 8f);
        tabImage.raycastTarget = true;
        Button button = tab.gameObject.AddComponent<Button>();
        button.targetGraphic = tabImage;
        button.onClick.AddListener(TurnFace);
        faceTab = Label(tab, "", 0f, 0f, 156f, 40f, 22f, Ink, FontStyles.Bold);
        faceTab.alignment = TextAlignmentOptions.Center;
        UpdateFaceTab();
    }

    private void UpdateFaceTab()
    {
        if (faceTab != null) faceTab.text = $"Page {faceIndex + 1} of {faceCount}  ›";
    }

    /// <summary>Turns a long record to its next face: the rows slide out and back in (0.24 s), one page sound.</summary>
    public void TurnFace()
    {
        if (faceCount <= 1 || faceTurn != null || !isActiveAndEnabled) return;
        faceTurn = StartCoroutine(TurnFaceRoutine());
    }

    private IEnumerator TurnFaceRoutine()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaper2SFX();
        var home = new Vector2(56f, -RecordRowTop);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.12f)
        {
            fieldGroup.alpha = 1f - t;
            rows.anchoredPosition = home + Vector2.left * (24f * t);
            yield return null;
        }
        faceIndex = (faceIndex + 1) % faceCount;
        RefreshContent();
        UpdateFaceTab();
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.12f)
        {
            fieldGroup.alpha = t;
            rows.anchoredPosition = home + Vector2.right * (24f * (1f - t));
            yield return null;
        }
        fieldGroup.alpha = 1f;
        rows.anchoredPosition = home;
        faceTurn = null;
    }

    /// <summary>Type 4: the total band sits behind the row holding the recorded total, on the face that shows it.</summary>
    private void PlaceTotalBand()
    {
        if (totalBand == null || face == null) return;
        int index = face.TotalKey.Length > 0 ? face.RowKeys.IndexOf(face.TotalKey) : -1;
        SupportingPaperTypes.FaceRange(face.RowKeys.Count, face.Style.RowsPerFace, faceIndex, out int start, out int count);
        bool shown = index >= start && index < start + count;
        totalBand.gameObject.SetActive(shown);
        if (!shown) return;
        float y = RecordRowTop + (index - start) * RecordRowStep - 4f;
        float width = FindingsPaperPerspective.WidthAt((y + RecordRowStep * 0.5f) / Mathf.Max(1f, paperHeight));
        totalBand.anchoredPosition = new Vector2(300f + (44f - 300f) * width, -y);
        totalBand.localScale = new Vector3(width, 1f, 1f);
    }

    /// <summary>Long values shrink only to a readable floor, wrapping inside their own box first.</summary>
    private static void Fit(TextMeshProUGUI text, float minSize, bool wrap)
    {
        text.enableAutoSizing = true;
        text.fontSizeMax = text.fontSize;
        text.fontSizeMin = minSize;
        text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
    }

    private void OnDestroy() => supportingStack?.PaperDisposed(this);

    private static void Sound() { if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX(); }

    private static void Rule(RectTransform parent, string name, float x, float y, float w, float h, Color color)
        => HudTheme.AddImage(HudTheme.PlaceTopLeft(HudTheme.CreateRect(name, parent), x, y, w, h), null, color);

    private static TextMeshProUGUI Label(RectTransform parent, string text, float x, float y,
        float w, float h, float size, Color color, FontStyles style = FontStyles.Normal)
    {
        var label = HudTheme.CreateLabel(text, parent, text, size, color, TextAlignmentOptions.TopLeft, style);
        HudTheme.PlaceTopLeft(label.rectTransform, x, y, w, h);
        return label;
    }
}
