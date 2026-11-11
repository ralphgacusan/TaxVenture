using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Level 3's real Case Folder (revised T03), under Level3Gameplay/CaseFolder.
/// The same desk folder, HUD store, floating 3D view, pages, page turns,
/// attached records and document layer as Levels 1-2; only what this case
/// prints is Level 3's.
///
/// CARRIED: from Bea's assignment on (and only when the campaign owns its
/// Case Folder; a direct Editor attempt may carry a local one), the desk
/// folder goes quietly into its HUD store (FolderToHUDIcon.RestoreStored, the
/// resume path Levels 1-2 use). The bar / Bag then opens the real folder
/// anywhere (CaseFolder3DView); dropping it on the desk stack and taking it
/// back use the existing paths. A folder the player left on the desk stays
/// there until they take it. Away from the desk it is read-only
/// (Doc3DLayer.BlocksValueTransfer).
///
/// PAGES: page 1 is "Client" (CaseFolderUI.FrontPage): the client's name,
/// work (once her identity or background paper is acquired), request, tax
/// year and case number, read only: no classification is redone. Page 2 is
/// "Supporting Documents" (InstancePages, its role): the papers this case
/// actually acquired, built by Level3RecordRules and held by Level 1-2's
/// AttachedRecordsPage (one taps out to read large; nothing fans out). The
/// set is rebuilt only while the folder is closed and nothing is out.
/// Revised T06: once collected, Bea's Income & Expense Summary (the existing
/// B-WORKSHEET, Level3WorksheetCheck.SummaryRecord) is among them, first of her
/// sheets: front = her two exact totals, back = entries, supporting originals
/// and the left-out records. It is the player's own checked work.
/// Revised T07/T08 (5 November 2026): page three is "Computation"
/// (Level3ComputationPage, Role level3-computation), the computation's only
/// view. The summary's two totals and the certificate's two printed amounts
/// are shared transfer sources there (Level3AmountSource, exact decimals from
/// the chapter/content); at Bea's computation the folder stays out on the left
/// while the desk calculator is up on the right.
///
/// OWNERSHIP: a value selected from this folder is cleared when the folder
/// closes, the player leaves the desk or the active case changes; a value
/// selected elsewhere is never touched. Opening the folder while the desk
/// calculator is up puts the calculator away (one tool at a time).
/// Nothing here saves, scores or changes the chapter.
/// </summary>
public sealed class Level3CaseFolder : MonoBehaviour, Level2DeskStamps.ICaseStamp
{
    public const string ChildName = "CaseFolder";
    public const string ClientHeading = "Taxpayer Information";
    private const string DeskFolderPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/InteractiveFolderObjects/OpenCaseFolder";
    private const float BindEvery = 0.25f;

    /// <summary>Level 2's review layout (sheets three to a row, slips on their own row) with a smaller ID card, for up to ten papers.</summary>
    public static RecordPageLayout Layout => new RecordPageLayout
    {
        topShare = 0.15f, bottomShare = 0.88f, cardWidth = 0.36f, sheetWidth = 0.4f, slipWidth = 0.2f,
        sheetsInRow = true, maxPerRow = 2, oneShapePerRow = true
    };

    public static Level3CaseFolder Active { get; private set; }

    private CaseFolderUI pages;
    private Transform folder;
    private FolderToHUDIcon store;
    private AttachedRecordsPage records;
    private string builtKey;
    private string boundCase;
    private bool carried;
    private bool viewWasOut;
    private float nextBind;
    private Level2DeskStamps stamps;
    private GameObject folderInk;
    // Revised T07/T08: the folder's Computation page (the computation's only view; the clipboard leaf is retired).
    private readonly Level3ComputationPage computation = new Level3ComputationPage();
    private bool computationWasShowing;
    private bool copyReadOnly;
    private bool priorReadOnly;
    private RectTransform copyTag;

    /// <summary>The real folder is in the bar (stored, or out of it for a view): what the Auditor can be handed.</summary>
    public bool Carried => store != null && store.IsStoredOrViewing;
    public bool Bound => pages != null;
    public AttachedRecordsPage Records => records;
    public Level3ComputationPage Computation => computation;

    private void OnEnable()
    {
        Active = this;
        CaseFolderUI.PageDecorator += DecorateCopy;
        CaseFolderFieldRow.ValueReceiving += SaveFrontPage;
        GameplayEvents.OnCaseFolderStored += FolderStored;
        GameplayEvents.OnCaseFolderReturned += FolderReturned;
    }

    private void FolderStored() => SaveFolderLocation(false);
    private void FolderReturned()
    {
        if (store != null && store.IsStoredOrViewing) return; // opening a carried view also withdraws its badge
        SaveFolderLocation(true);
    }
    private void SaveFolderLocation(bool onDesk)
    {
        Level3Checkpointer owner = Level3Checkpointer.Active;
        Level3FolderPacket packet = owner?.State?.Case(Level3ChapterRules.BeaCaseId)?.folder;
        if (packet == null || packet.onDesk == onDesk || owner.Phase != Level3Phase.BeaComputation) return;
        owner.CommitComputation((Level3ChapterCheckpoint candidate, out string why) =>
        {
            why = null;
            candidate.Case(Level3ChapterRules.BeaCaseId).folder.onDesk = onDesk;
            return Level3CommandOutcome.Applied;
        }, out _);
    }

    private bool SaveFrontPage(CaseFolderFieldRow row, string key, object value)
    {
        if (folder == null || row == null || !row.transform.IsChildOf(folder) || Level3Checkpointer.Active == null) return true;
        var outcome = Level3Checkpointer.Active.CommitComputation((Level3ChapterCheckpoint c, out string why) =>
        {
            why = null;
            Level3ReviewRules.CaptureFolder(c, CaseManager.Instance?.CurrentCase);
            Level3FolderPacket packet = c.Case(Level3ChapterRules.BeaCaseId).folder;
            switch (key)
            {
                case "IncomeSource": packet.incomeSource = value?.ToString() ?? ""; break;
                case "ResidencyStatus": packet.classification = value?.ToString() ?? ""; break;
                case "TaxpayerType": packet.taxpayerType = value?.ToString() ?? ""; break;
                default: return Level3CommandOutcome.Refused;
            }
            packet.onDesk = !Carried;
            c.Case(Level3ChapterRules.BeaCaseId).computation.revision++;
            return Level3CommandOutcome.Applied;
        }, out string hint);
        bool saved = outcome == Level3Checkpointer.TapeCommit.Attached || outcome == Level3Checkpointer.TapeCommit.AlreadyAttached;
        if (!saved && !string.IsNullOrEmpty(hint)) Level3BeaTable.Level3Say(hint);
        return saved;
    }

    private void RestoreFrontPage()
    {
        Level3FolderPacket saved = Level3Checkpointer.Active?.State?.Case(Level3ChapterRules.BeaCaseId)?.folder;
        CaseData data = CaseManager.Instance?.CurrentCase;
        if (data == null || saved == null) return;
        data.fullName = saved.fullName ?? "";
        data.tin = saved.tin ?? "";
        data.residencyStatus = Enum.TryParse(saved.classification, out ResidencyStatus classification) ? classification : (ResidencyStatus?)null;
        data.taxpayerType = Enum.TryParse(saved.taxpayerType, out TaxpayerType taxpayerType) ? taxpayerType : (TaxpayerType?)null;
        data.incomeSource = Enum.TryParse(saved.incomeSource, out IncomeSource value) ? value : (IncomeSource?)null;
        if (data.incomeSource == null) data.folderTransferredFields.Remove("IncomeSource");
    }

    private void DecorateCopy(int index, CaseFolderUI.PageBuilder page)
    {
        if (page?.Owner == pages && Level3Checkpointer.Active?.State?.Case(Level3ChapterRules.BeaCaseId)?.copyPhase == "carried")
            ClientCopyHandover.DecorateCopyPage(index, page);
    }

    private void SyncCopy()
    {
        bool approved = Level3Checkpointer.Active?.State?.Case(Level3ChapterRules.BeaCaseId)?.review?.approved == true;
        if (approved && !copyReadOnly)
        { priorReadOnly = CaseFolderFieldRow.ReadOnly; copyReadOnly = true; CaseFolderFieldRow.ReadOnly = true; }
        bool collected = Level3Checkpointer.Active?.State?.Case(Level3ChapterRules.BeaCaseId)?.copyPhase == "carried";
        RectTransform icon = OfficeHudRuntime.Instance?.CaseFolderIcon;
        if (collected && copyTag == null && icon != null) copyTag = ClientCopyStamp.BuildTag(icon);
        if (copyTag != null) copyTag.gameObject.SetActive(collected);
    }

    private void OnDisable()
    {
        if (Active == this) Active = null;
        CaseFolderUI.PageDecorator -= DecorateCopy;
        CaseFolderFieldRow.ValueReceiving -= SaveFrontPage;
        GameplayEvents.OnCaseFolderStored -= FolderStored;
        GameplayEvents.OnCaseFolderReturned -= FolderReturned;
        if (copyReadOnly) CaseFolderFieldRow.ReadOnly = priorReadOnly;
        copyReadOnly = false;
        if (copyTag != null) Destroy(copyTag.gameObject);
        if (CaseFolderUI.FrontPage == (Func<CaseData, CaseFolderPageContent>)ClientPage) CaseFolderUI.FrontPage = null;
        if (pages != null)
        {
            pages.PageRendered -= HandlePageRendered;
            if (pages.InstancePages == (Func<CaseData, List<CaseFolderPageContent>>)SupportingPages) pages.InstancePages = null;
        }
        if (stamps != null && ReferenceEquals(stamps.CaseBinding, this))
        { stamps.PutAway(); stamps.CaseBinding = null; }
        if (folderInk != null) Destroy(folderInk);
        DisposeRecords();
        pages = null;
    }

    private void Update()
    {
        Level3Checkpointer chapter = Level3Checkpointer.Active;
        if (chapter == null || !chapter.IsReady || chapter.State == null) return;
        if (pages == null)
        {
            if (Time.unscaledTime < nextBind) return;
            nextBind = Time.unscaledTime + BindEvery;
            if (!TryBind()) return;
        }
        SyncCopy();
        SyncCarried(chapter);
        SyncRecords(chapter.State);
        SyncDockedPapers();
        ReleaseOwnedSelection(chapter.State);
        KeepOneToolUp();
        SyncComputation();
    }

    // =========================================================
    // COMPUTATION PAGE (revised T07/T08)
    // =========================================================

    /// <summary>Redraws the Computation page only when what it shows changed (never every frame); closing resets its view.</summary>
    private void SyncComputation()
    {
        bool showing = pages.IsShowing && ComputationIndex >= 0 && (pages.CurrentPageIndex == ComputationIndex || pages.CurrentPageIndex == pages.FindPageIndexByRole(Level3ComputationPage.PaymentRole));
        // A strip flying to its slot lands first; the page then shows the saved result (ComputationLanded).
        bool flying = Level3DeskCalculator.Instance != null && Level3DeskCalculator.Instance.FlyingDestination != null;
        if (showing && !flying && computation.NeedsRedraw) pages.RefreshCurrentPage();
        if (showing) computation.Tick();
        if (!pages.IsShowing && computationWasShowing) computation.Reset();
        computationWasShowing = pages.IsShowing;
    }

    private int ComputationIndex => pages != null ? pages.FindPageIndexByRole(Level3ComputationPage.Role) : -1;


    /// <summary>"Open summary" / "Read certificate": turn to Supporting Documents and take that paper out to read (nothing saved).</summary>
    public bool ShowSource(string source)
    {
        if (source == Level3ChapterRules.BeaCreditSource) return Level3CertificatePaper.Instance != null && Level3CertificatePaper.Instance.Open();
        if (source == Level3WorksheetCheck.WorksheetWorkId) return Level3SummaryDocument.Instance != null && Level3SummaryDocument.Instance.Open();
        if (pages == null || records == null || !pages.IsShowing) return false;
        ClientRecordPaper paper = null;
        foreach (ClientRecordPaper candidate in records.Papers)
            if (candidate != null && candidate.Record != null && candidate.Record.source == source) paper = candidate;
        if (paper == null) return false;
        ClientRecordPaper shown = paper;
        pages.TurnToPage(records.PageIndex, () => { if (shown != null && shown.IsAttached) shown.TakeOut(); });
        return true;
    }

    /// <summary>The printed strip clipped into a Computation slot (after its save): the page settles on it.</summary>
    public void ComputationLanded(string field)
    {
        computation.Landed(field);
        if (pages != null && pages.IsShowing && (pages.CurrentPageIndex == ComputationIndex || pages.CurrentPageIndex == pages.FindPageIndexByRole(Level3ComputationPage.PaymentRole))) pages.RefreshCurrentPage();
    }

    public void ComputationNote(string text) => computation.Note(text);

    private bool TryBind()
    {
        CaseDefinition definition = Definition;
        if (definition?.level3 == null || !definition.level3.IsSet) return false;
        folder = PlayerPlacement.Find(gameObject.scene, DeskFolderPath);
        CaseFolderUI ui = folder != null ? folder.GetComponentInChildren<CaseFolderUI>(true) : null;
        if (ui == null || ui.IsReference || ui.PaperRect == null) return false;
        store = Level2FolderHandoff.FindStore(gameObject.scene);
        if (store == null) return false;
        pages = ui;
        CaseFolderUI.FrontPage = ClientPage;
        pages.InstancePages = SupportingPages;
        pages.PageRendered += HandlePageRendered;
        stamps = transform.parent.GetComponentInChildren<Level2DeskStamps>(true);
        if (stamps != null) stamps.CaseBinding = this;
        Transform stand = PlayerPlacement.Find(gameObject.scene, Level2PreliminaryStamp.StampSetPath);
        if (stand != null) stand.gameObject.SetActive(true);
        return true;
    }

    private static CaseDefinition Definition => CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;

    // =========================================================
    // CARRIED (the existing store's quiet resume path)
    // =========================================================

    /// <summary>The folder belongs in the bar: the active case is assigned, and the campaign owns its folder (or this attempt saves nothing).</summary>
    public static bool ShouldCarry(Level3ChapterCheckpoint state, bool ownsFolder, bool savesStory) =>
        state != null && Level3ChapterRules.PhaseOf(state) >= Level3Phase.BeaRecords &&
        state.Case(state.activeCaseId)?.admitted == true && (ownsFolder || !savesStory);

    private void SyncCarried(Level3Checkpointer chapter)
    {
        if (carried || chapter.Blocked) return;
        string copy = chapter.State.Case(Level3ChapterRules.BeaCaseId)?.copyPhase;
        // The copy is with the Auditor, or (T10) Bea has it: the icon stays empty, as in Levels 1-2.
        if (copy == Level3ReviewRules.CopyAtAuditor || copy == Level3DeliveryRules.CopyDone || chapter.State.Case(Level3ChapterRules.BeaCaseId)?.folder?.onDesk == true) return;
        HudHotbar hotbar = HudHotbar.Instance;
        bool owns = hotbar == null || InventoryRules.Owns(hotbar.Inventory, InventoryCatalogue.CaseFolder);
        if (!ShouldCarry(chapter.State, owns, chapter.SavesStory)) return;
        carried = true;
        if (store.IsStoredOrViewing) return;
        store.RestoreStored();
        Transform icon = Level2FolderHandoff.FindFolderIcon(gameObject.scene);
        if (icon != null) icon.GetComponent<HudIconButton>()?.RefreshFolderState();
        Debug.Log("[Level3] Bea's Case Folder is in the bar (the desk folder, carried).");
    }

    // =========================================================
    // PAGES
    // =========================================================

    private CaseFolderPageContent ClientPage(CaseData data)
    {
        // Same five rows, field widgets and transfer keys as Level 2. These
        // supplied facts are already established; no answer-key/float bridge.
        RestoreFrontPage();
        FillKnownClientData(data, Definition, Level3Checkpointer.Active?.State);
        return new CaseFolderPageContent(ClientHeading, "");
    }

    public static void FillKnownClientData(CaseData data, CaseDefinition definition, Level3ChapterCheckpoint state)
    {
        if (data == null || definition == null) return;
        bool known = state?.Case(definition.caseId)?.acquiredSourceIds.Contains("B-BG") == true;
        if (known && definition.caseId == Level3ChapterRules.BeaCaseId)
        {
            data.taxpayerType ??= TaxpayerType.Professional;
            data.residencyStatus ??= ResidencyStatus.ResidentCitizen;
            data.folderTransferredFields.Add("TaxpayerType");
            data.folderTransferredFields.Add("ResidencyStatus");
        }
        if (definition.caseId == Level3ChapterRules.BeaCaseId && state?.Case(definition.caseId)?.acquiredSourceIds.Contains("B-ID") == true)
        {
            if (string.IsNullOrEmpty(data.fullName)) data.fullName = definition.fullName;
            DocumentDefinition identity = definition.documents?.Find(d => d.documentName == "B-ID");
            string id = identity?.fields?.Find(f => f.semanticKey == "TIN" || f.label == "TIN" || f.label == "TIN ID")?.displayValue;
            if (string.IsNullOrEmpty(data.tin) && !string.IsNullOrEmpty(id)) data.tin = id;
        }
    }

    public bool StampsBusy => stamps != null && stamps.Busy;
    public CaseFolderUI PaperFolder => pages;
    public bool Available => Level3Checkpointer.Active?.IsReady == true &&
        Level3Checkpointer.Active.Phase == Level3Phase.BeaComputation &&
        Level3Checkpointer.Active.State.calculatorTaught && !Level3ReviewRules.CaseDone(Level3Checkpointer.Active.State);
    public int Revision => Level3Checkpointer.Active?.State?.Case(Level3ChapterRules.BeaCaseId)?.computation?.revision ?? -1;
    public string InkText(StampType type) => type == StampType.ReadyForFiling
        ? "READY" : "NOT READY";
    public void Opening() { }

    public bool Commit(StampType type, Vector2 local, float tilt, int page)
    {
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (!Available || owner == null) return false;
        string kind = type == StampType.ReadyForFiling ? Level3ReviewRules.StampReady : Level3ReviewRules.StampHold;
        Level3Checkpointer.TapeCommit result = owner.CommitComputation((Level3ChapterCheckpoint c, out string reason) =>
        {
            Level3ReviewRules.CaptureFolder(c, CaseManager.Instance?.CurrentCase);
            return Level3ReviewRules.StampAt(c, kind, page, local.x, local.y, tilt, out reason);
        }, out string hint);
        if (result != Level3Checkpointer.TapeCommit.Attached)
        { if (!string.IsNullOrEmpty(hint)) Level2Say.Line(hint); return false; }
        DrawSavedInk();
        GameplayEvents.RaiseGuideCue(Level3DeskCalculator.CueSheetStamped);
        return true;
    }

    private void DrawSavedInk()
    {
        if (folderInk != null) Destroy(folderInk);
        Level3ChapterCheckpoint state = Level3Checkpointer.Active?.State;
        Level3StampRecord mark = state?.Case(state.activeCaseId)?.stamp;
        if (pages == null || mark == null || !mark.placed || pages.CurrentPageIndex != mark.page) return;
        string text = InkText(mark.type == Level3ReviewRules.StampReady ? StampType.ReadyForFiling : StampType.NotReadyForFiling);
        folderInk = ReviewInkMark.CreateAt(pages.PaperRect, text, new Vector2(mark.x, mark.y), mark.tilt);
        ReviewInkMark.SetStyle(folderInk, mark.type == Level3ReviewRules.StampReady ? ReviewInkMark.InkGreen : ReviewInkMark.InkRed, mark.revision == Revision ? 1f : 0.3f);
        Doc3DLayer.For(pages.transform)?.InvalidateText();
        Doc3DLayer.For(pages.transform)?.MarkDirty();
    }

    private void DrawClient(CaseFolderUI.PageBuilder page)
    {
        Level3Checkpointer chapter = Level3Checkpointer.Active;
        foreach (KeyValuePair<string, string> line in ClientLines(Definition, chapter != null ? chapter.State : null))
            page.AddSourceRow(line.Key, line.Value, DataValueType.Text, "Level3/Client/" + line.Key);
    }

    /// <summary>
    /// The Client page's read-only lines: who, what work (from her acquired
    /// identity or background paper only), what she asked for, the tax year and
    /// period, and the case number. Never a classification to redo or an answer.
    /// </summary>
    public static List<KeyValuePair<string, string>> ClientLines(CaseDefinition definition, Level3ChapterCheckpoint state)
    {
        var lines = new List<KeyValuePair<string, string>>();
        Level3CaseContent content = definition?.level3;
        if (content == null || !content.IsSet) return lines;
        lines.Add(new KeyValuePair<string, string>("Client", content.taxpayer));
        string work = AcquiredWork(definition, state);
        if (!string.IsNullOrEmpty(work)) lines.Add(new KeyValuePair<string, string>("Work", work));
        string title = definition.caseTitle ?? "";
        int dash = title.LastIndexOf(" - ", StringComparison.Ordinal);
        string request = dash >= 0 ? title.Substring(dash + 3) : title;
        if (!string.IsNullOrWhiteSpace(request)) lines.Add(new KeyValuePair<string, string>("Request", request.Trim()));
        lines.Add(new KeyValuePair<string, string>("Tax year", Period(content)));
        lines.Add(new KeyValuePair<string, string>("Case", definition.caseId));
        return lines;
    }

    private static string AcquiredWork(CaseDefinition definition, Level3ChapterCheckpoint state)
    {
        List<string> acquired = state?.Case(definition.caseId)?.acquiredSourceIds;
        if (acquired == null || definition.level3.sources == null) return null;
        foreach (Level3SourceDefinition source in definition.level3.sources)
        {
            if (source == null || (source.paperType != 1 && source.paperType != 2) || !acquired.Contains(source.sourceId)) continue;
            DocumentDefinition document = definition.documents?.Find(d => d != null && d.documentName == source.documentName);
            DocumentFieldDefinition work = document?.fields?.Find(f => f != null && (f.label == "Work" || f.label == "Business"));
            if (!string.IsNullOrWhiteSpace(work?.displayValue)) return work.displayValue;
        }
        return null;
    }

    /// <summary>"2026 · 1 Jan – 31 Dec" from the content's period (the year alone when the dates are unreadable).</summary>
    public static string Period(Level3CaseContent content)
    {
        string year = content.taxYear.ToString(CultureInfo.InvariantCulture);
        if (!DateTime.TryParseExact(content.periodStart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime from) ||
            !DateTime.TryParseExact(content.periodEnd, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime to))
            return year;
        return year + " · " + from.ToString("d MMM", CultureInfo.InvariantCulture) + " – " + to.ToString("d MMM", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Page two of this folder: the attached records draw themselves (no rows). Revised T07/T08: page three is
    /// Computation once Bea's summary is collected; her summary and certificate offer their amounts to it
    /// (Level3AmountSource), the other papers stay read-only.
    /// </summary>
    private List<CaseFolderPageContent> SupportingPages(CaseData data)
    {
        var list = new List<CaseFolderPageContent>
        {
            new CaseFolderPageContent(CaseFolderPageContent.SupportingRecordsHeading, "")
            {
                Render = DrawSupportingNote,
                Role = CaseFolderPageContent.SupportingRecordsRole
            }
        };
        if (Level3ComputationPage.Shown(Level3Checkpointer.Active?.State))
        {
            list.Add(computation.Content());
            list.Add(computation.PaymentContent());
        }
        return list;
    }

    /// <summary>Before any paper is acquired, one small printed line says so.</summary>
    private void DrawSupportingNote(CaseFolderUI.PageBuilder page)
    {
        if (records != null || page?.Paper == null) return;
        string client = Definition?.level3?.taxpayer ?? "The client";
        TextMeshProUGUI note = HudTheme.CreateLabel("Originals Note", page.Paper, client + "'s records: not handed over yet",
            26f, new Color32(94, 84, 70, 255), TextAlignmentOptions.Center, FontStyles.Italic);
        note.raycastTarget = false;
        page.AddOverlay(note.gameObject);
        RectTransform rect = note.rectTransform;
        rect.anchorMin = new Vector2(0.08f, 0.08f);
        rect.anchorMax = new Vector2(0.92f, 0.16f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    /// <summary>"caseId|id,id,..": the active case and the papers it acquired (the record set's identity).</summary>
    public static string RecordKey(Level3ChapterCheckpoint state)
    {
        Level3CaseProgress active = state?.Case(state.activeCaseId);
        if (active == null) return "";
        var ids = new List<string>(active.acquiredSourceIds);
        ids.Sort(StringComparer.Ordinal);
        // Revised T06: the collected summary (B-WORKSHEET) is one more paper of the set.
        string work = active.workIds != null && active.workIds.Contains(Level3WorksheetCheck.WorksheetWorkId) ? "+" + Level3WorksheetCheck.WorksheetWorkId : "";
        return active.caseId + "|" + string.Join(",", ids) + work;
    }

    /// <summary>
    /// The Supporting Documents papers: the acquired originals, plus (revised T06) Bea's collected
    /// Income &amp; Expense Summary first among her sheets. The summary is the player's own checked work
    /// as an existing plain record; it is never a value source here.
    /// </summary>
    public static List<ClientRecord> SupportingRecords(CaseDefinition definition, Level3ChapterCheckpoint state)
    {
        Level3CaseProgress progress = state?.Case(definition?.caseId);
        if (progress == null) return new List<ClientRecord>();
        List<ClientRecord> built = Level3RecordRules.Build(definition, progress.acquiredSourceIds);
        built.RemoveAll(record => record.source != "B-ID" && record.source != "B-BG");
        if (Level3WorksheetCheck.Collected(state))
        {
            ClientRecord summary = Level3WorksheetCheck.SummaryRecord(definition, state);
            if (summary != null) built.Add(summary);
        }
        built.AddRange(Level3RecordRules.Build(definition, progress.acquiredSourceIds).FindAll(record => record.source == Level3ChapterRules.BeaCreditSource));
        return built;
    }

    private void SyncRecords(Level3ChapterCheckpoint state)
    {
        string key = RecordKey(state);
        if (key == builtKey) return;
        // Only on an idle folder: nothing is out, nothing is mid-flight, and (revised T08) the Supporting
        // Documents page is not the one showing, so a certificate handed over while the folder is open
        // reaches it before the player turns back there.
        if (records != null && records.AnyOut) return;
        if (pages.IsShowing && (records == null || pages.CurrentPageIndex == records.PageIndex ||
            pages.CurrentPageIndex == pages.FindPageIndexByRole(CaseFolderPageContent.SupportingRecordsRole))) return;
        builtKey = key;
        DisposeRecords();
        CaseDefinition definition = Definition;
        if (definition == null || definition.caseId != state.activeCaseId) return;
        List<ClientRecord> built = SupportingRecords(definition, state);
        if (built.Count == 0) return;
        records = AttachedRecordsPage.Create(gameObject, folder, pages, -1, definition.caseId, built, Layout,
            () => pages != null && pages.IsShowing, CaseFolderPageContent.SupportingRecordsRole,
            new System.Collections.Generic.HashSet<string> { Level3WorksheetCheck.WorksheetWorkId, Level3ChapterRules.BeaCreditSource });
        if (records == null) { Debug.LogWarning("[Level3] The acquired papers could not be attached to the Case Folder."); return; }
        int amounts = 0;
        foreach (ClientRecordPaper paper in records.Papers) amounts += Level3AmountSource.AttachTo(paper, definition);
        Debug.Log($"[Level3] Supporting Documents: {built.Count} paper(s) ({string.Join(", ", built.ConvertAll(r => r.tab))}), {amounts} transferable amount(s).");
    }

    private void DisposeRecords()
    {
        Level3SummaryDocument.Instance?.ReleaseFolderSlot();
        Level3CertificatePaper.Instance?.ReleaseFolderSlot();
        if (records == null) return;
        records.Dispose();
        records = null;
    }

    public static bool Attached(string source)
    {
        Level3FolderPacket packet = Level3Checkpointer.Active?.State?.Case(Level3ChapterRules.BeaCaseId)?.folder;
        return source == Level3WorksheetCheck.WorksheetWorkId ? packet?.summaryAttached == true : packet?.certificateAttached == true;
    }

    public static bool SetAttached(string source, bool attached)
    {
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null) return false;
        // Milestone T01: unclipping needs an ordinary slot for the paper; a full bar and Bag refuses and keeps it clipped.
        CampaignInventoryRecord carried = HudHotbar.Instance != null ? HudHotbar.Instance.Inventory : null;
        var result = owner.CommitComputation((Level3ChapterCheckpoint c, out string why) =>
            Level3ReviewRules.SetAttachment(c, source, attached, out why, carried), out string hint);
        if (result == Level3Checkpointer.TapeCommit.Attached || result == Level3Checkpointer.TapeCommit.AlreadyAttached) return true;
        if (!string.IsNullOrEmpty(hint)) Level3BeaTable.Level3Say(hint);
        return false;
    }

    public bool TryAttach(ClientRecordPaper paper, Vector2 screen)
    {
        string source = paper?.Record?.source;
        if (records == null || paper == null || !records.OverOwnPage(screen) || pages.CurrentPageIndex != records.PageIndex ||
            records.SlotFor(source) == null || !SetAttached(source, true)) return false;
        DockPaper(source, true);
        return true;
    }

    private void DockPaper(string source, bool animate)
    {
        Transform target = records?.SlotFor(source);
        if (target == null) return;
        if (source == Level3WorksheetCheck.WorksheetWorkId)
            Level3SummaryDocument.Instance?.DockTo(target, records.SlotSizeFor(source).y, animate);
        else Level3CertificatePaper.Instance?.DockTo(target, records.SlotSizeFor(source).y, animate);
    }

    private void SyncDockedPapers()
    {
        if (records == null) return;
        string summary = Level3WorksheetCheck.WorksheetWorkId, certificate = Level3ChapterRules.BeaCreditSource;
        if (Attached(summary)) DockPaper(summary, false); else Level3SummaryDocument.Instance?.ReleaseFolderSlot();
        if (Attached(certificate)) DockPaper(certificate, false); else Level3CertificatePaper.Instance?.ReleaseFolderSlot();
    }

    private void HandlePageRendered(int index)
    {
        DrawSavedInk();
        if (records != null && index == records.PageIndex) records.SetEventCamera(HeldOnScreen.ActiveCamera(Camera.main));
    }

    // =========================================================
    // OWNERSHIP AND ONE TOOL AT A TIME
    // =========================================================

    /// <summary>A value picked from this folder lasts while the folder is open at the desk for the same case.</summary>
    private void ReleaseOwnedSelection(Level3ChapterCheckpoint state)
    {
        bool caseChanged = boundCase != null && boundCase != state.activeCaseId;
        boundCase = state.activeCaseId;
        ValueTransferManager manager = ValueTransferManager.Instance;
        if (manager == null || !manager.HasSelection || !Level3ValueTransfer.OwnsSelection(manager.SelectedSourceComponent, folder)) return;
        CameraController cc = CameraController.Instance;
        bool atDesk = cc != null && cc.CurrentMode == CameraController.CameraMode.Workstation;
        if (caseChanged || !pages.IsShowing || !atDesk) manager.ClearSelection();
    }

    private void KeepOneToolUp()
    {
        bool viewOut = OfficeHudRuntime.Instance != null && OfficeHudRuntime.Instance.CaseFolderViewOut;
        // Revised T07: during Bea's computation the folder (left) and the calculator (right) are used together.
        if (viewOut && !viewWasOut && Level3DeskCalculator.Instance != null && Level3DeskCalculator.Instance.Lifted &&
            !Level3DeskCalculator.Instance.SharesDeskWithFolder)
            Level3DeskCalculator.Instance.PutAway();
        viewWasOut = viewOut;
    }
}
