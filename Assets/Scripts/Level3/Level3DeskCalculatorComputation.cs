using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Bea's computation at the office desk (Level 3 T07), the second half of
/// Level3DeskCalculator (one component, one input owner). From the moment her
/// worksheet is collected, her computation sheet (Level3ComputationLeafModel)
/// lies on the clipboard where the practice card was; it lifts with the
/// calculator (sheet left, calculator right) exactly like the practice did.
///
/// - PICK on Gross income / Allowable expenses opens a chooser with the
///   collected worksheet's two subtotals and Bea's draft total, all alike; the
///   chosen total is a shared transfer source: select then tap the display,
///   or drag onto it, preserving exact amount and provenance.
/// - PICK on Rate band opens the year's annual table (every row, never
///   pre-selected); the chosen row's floor, base tax and rate become source chips
///   (the rate goes on as a percentage).
/// - A printed tape attaches by drag/drop or tap-select then tap-field. Level3Checkpointer.CommitComputation saves first; only
///   then does the strip tear off and fly into its slot. Tapping an attached
///   tape with nothing printed offers its result and shows its working.
/// - T08: once Bea has handed over her certificate and the player explained
///   it, PICK on Tax credits offers its two printed amounts alike; the linked
///   one is a calculator source like the totals. Remaining payable takes a
///   tape (tax due − credits). The blue certificate tab holds the certificate
///   up in front of the tools (Level3CertificatePaper); while it is up the
///   tools take no touches and a tap anywhere lowers it.
/// Nothing here says whether a choice or a tape is right (T09 reviews it).
/// Guide cues: ComputationTotalChosen, ComputationRuleChosen,
/// ComputationCreditChosen, ComputationSourceUsed, ComputationTapeAttached.
/// </summary>
public sealed partial class Level3DeskCalculator
{
    public const string CueTotalChosen = "ComputationTotalChosen";
    public const string CueRuleChosen = "ComputationRuleChosen";
    public const string CueSheetSource = "ComputationSourceUsed";
    public const string CueSheetTape = "ComputationTapeAttached";
    public const string CueCreditChosen = "ComputationCreditChosen";
    /// <summary>Bea's sheet lifts a little taller than the practice card (more lines to read).</summary>
    public const float LeafScreenHeight = 0.66f;

    private Level3ComputationLeafModel leaf;
    private Level3CertificatePaper certificate;
    private string leafPicker;
    private string armedField;
    private int shownLeafRevision = -1, shownLeafTapes = -1;
    private Level3Phase shownLeafPhase = (Level3Phase)(-1);
    private string shownLeafHide;
    private int shownLeafCertificate = -1;

    private static string BeaCase => Level3ChapterRules.BeaCaseId;
    private static CaseDefinition Definition => CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;

    /// <summary>Bea's computation sheet is on the clipboard (her worksheet is collected and, revised T03, the calculator's example is done).</summary>
    private static bool ComputationOpen => Chapter != null && Level3ChapterRules.PhaseOf(Chapter) >= Level3Phase.BeaComputation && !LessonOpen;

    /// <summary>The paper that lifts with the calculator: Bea's sheet once it is out, else the card (the first-use example or earlier practice tapes).</summary>
    private Transform PaperRoot => ComputationOpen && leaf != null ? leaf.Root : card.Root;
    private Bounds PaperBounds => ComputationOpen && leaf != null ? leaf.UnitBounds : card.UnitBounds;
    private float PaperScreenHeight => ComputationOpen && leaf != null ? LeafScreenHeight : PracticeScreenHeight;

    private void BuildLeaf()
    {
        leaf = Level3ComputationLeafModel.Build(transform, props, "BEA MENDOZA · 2026 COMPUTATION", "Individual · graduated rates");
        leaf.Root.gameObject.SetActive(false);
        // T08: her withholding certificate, held by the player (built once she has handed it over).
        certificate = Level3CertificatePaper.Ensure(transform);
    }

    /// <summary>T08: the certificate tab was tapped (on the desk or on the lifted sheet).</summary>
    private bool IsCertificateTab(Collider target) =>
        LeafOwns(target) && leaf.TryHit(target, out Level3ComputationLeafModel.Hit hit) && hit.Kind == Level3ComputationLeafModel.HitKind.Certificate;

    private void RaiseCertificate()
    {
        if (certificate == null || !certificate.Raise(Level3CertificatePaper.Context.Desk))
        {
            leaf?.SetMessage("Bea's certificate isn't here yet.");
            return;
        }
        leaf.SetMessage("Drag the certificate back to its icon to put it away.");
    }

    private bool LeafOwns(Collider collider) =>
        leaf != null && leaf.Root.gameObject.activeInHierarchy && collider != null && collider.transform.IsChildOf(leaf.Root);

    private static string LeafField(string destinationId)
    {
        string prefix = Level3ComputationRules.CaseKey(BeaCase) + "/";
        return destinationId != null && destinationId.StartsWith(prefix, System.StringComparison.Ordinal) ? destinationId.Substring(prefix.Length) : null;
    }

    /// <summary>The draft as printed on the calculator's tape: the whole chain at Bea's sheet, the last step for the practice.</summary>
    private List<string> DraftLines() => ComputationOpen
        ? Level3Calculator.ChainLines(Calculator.DraftChain)
        : Level3Calculator.TapeLines(Calculator.DraftExpression, Calculator.DraftResult);

    // =========================================================
    // THE SHEET FROM THE CHAPTER (quiet: restores never animate or announce)
    // =========================================================

    private void SyncLeaf(bool force)
    {
        if (leaf == null) return;
        Level3ChapterCheckpoint chapter = Chapter;
        // Revised T07 (5 November 2026): the Case Folder's Computation page is the sheet. The clipboard
        // leaf is retired from play (never shown, so it never owns input); its code stays for T17's removal.
        bool open = false;
        if (leaf.Root.gameObject.activeSelf != open)
        {
            leaf.Root.gameObject.SetActive(open);
            if (open && mode == Mode.Resting) Apply(leaf.Root, cardRest);
            if (open) leaf.SetMessage("Bea's computation. Tap it or the calculator to start, then PICK Gross income.");
            force = true;
        }
        if (!open) return;
        Level3Phase phase = Level3ChapterRules.PhaseOf(chapter);
        Level3ComputationRecord record = Level3ComputationRules.Record(chapter, BeaCase);
        int revision = record != null ? record.revision : 0;
        int tapes = chapter.tapes != null ? chapter.tapes.Count : 0;
        string hide = LeafField(flyDestination);
        int certificateState = !Level3CertificateRules.Acquired(chapter) ? 0 : Level3CertificateRules.Explained(chapter) ? 2 : 1;
        if (!force && revision == shownLeafRevision && tapes == shownLeafTapes && phase == shownLeafPhase && hide == shownLeafHide &&
            certificateState == shownLeafCertificate) return;
        shownLeafCertificate = certificateState;
        bool animate = !force;
        shownLeafRevision = revision;
        shownLeafTapes = tapes;
        shownLeafPhase = phase;
        shownLeafHide = hide;
        leaf.Show(chapter, Definition, hide, animate);
    }

    // =========================================================
    // ACTIONS (presentation; the chapter command decides)
    // =========================================================

    /// <summary>A tap on Bea's sheet while the tools are up. False when the tap was not the sheet's.</summary>
    private bool ActivateLeaf(Collider target)
    {
        if (!LeafOwns(target) || !leaf.TryHit(target, out Level3ComputationLeafModel.Hit hit)) return false;
        // Reading the certificate is never an edit (it works whatever the sheet's state).
        if (hit.Kind == Level3ComputationLeafModel.HitKind.Certificate)
        {
            CloseLeafPicker();
            RaiseCertificate();
            return true;
        }
        // T09 revision: stamping is the shared folder stamp set's (Level3CaseFolder); the retired leaf's box does nothing.
        if (hit.Kind == Level3ComputationLeafModel.HitKind.StampBox && !leaf.PickerOpen) return true;
        if (leaf.PickerOpen)
        {
            if (hit.Kind == Level3ComputationLeafModel.HitKind.Chip) ChooseLeaf(hit.Id);
            else if (hit.Kind != Level3ComputationLeafModel.HitKind.None) CloseLeafPicker();
            return true;
        }
        if (hit.Kind == Level3ComputationLeafModel.HitKind.Body) return true;
        if (!Level3ComputationRules.Editable(Chapter, BeaCase))
        {
            leaf.SetMessage("Bea's computation is set for now.");
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
            return true;
        }
        Level3ComputationRecord record = Level3ComputationRules.Record(Chapter, BeaCase);
        switch (hit.Kind)
        {
            case Level3ComputationLeafModel.HitKind.Total:
                string chosen = hit.Id == Level3ComputationRules.Gross ? record?.grossChoice
                    : hit.Id == Level3ComputationRules.Credits ? record?.creditChoice : record?.expensesChoice;
                if (!string.IsNullOrEmpty(chosen)) break;
                else if (hit.Id == Level3ComputationRules.Credits) OpenCredits();
                else OpenTotals(hit.Id);
                break;
            case Level3ComputationLeafModel.HitKind.Pick:
                if (hit.Id == Level3ComputationRules.Credits) OpenCredits();
                else OpenTotals(hit.Id);
                break;
            case Level3ComputationLeafModel.HitKind.Band: OpenBands(); break;
            case Level3ComputationLeafModel.HitKind.Constant:
                leaf.SetMessage("PICK a rate band first, then transfer its printed value to the display.");
                break;
            case Level3ComputationLeafModel.HitKind.Slot:
                leaf.SetMessage("Calculate a result, then drag its tape here; or select tape then tap here.");
                break;
        }
        return true;
    }

    private void OpenTotals(string field)
    {
        var choices = new List<KeyValuePair<string, string>>();
        foreach (Level3TotalChoice choice in Level3ComputationRules.TotalChoices(Chapter, Definition))
            choices.Add(new KeyValuePair<string, string>(choice.ChoiceId, choice.Label + "  " + Level3Calculator.Format(choice.Amount)));
        Level3ComputationRecord record = Level3ComputationRules.Record(Chapter, BeaCase);
        leafPicker = field;
        DisarmLeaf();
        leaf.OpenPicker("Which total is " + Level3ComputationRules.SectionLabel(field) + "?", choices,
            field == Level3ComputationRules.Gross ? record?.grossChoice : record?.expensesChoice);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
    }

    private void OpenBands()
    {
        var choices = new List<KeyValuePair<string, string>>();
        foreach (Level3RuleDefinition rule in Level3ComputationRules.Rules(Definition))
            choices.Add(new KeyValuePair<string, string>(rule.ruleId, Level3ComputationRules.BandLabel(rule) + ": " + Level3ComputationRules.BandFormula(rule)));
        leafPicker = Level3ComputationRules.Rule;
        DisarmLeaf();
        leaf.OpenPicker("Annual table, 2023 onwards. Which row holds Bea's taxable income?", choices, Level3ComputationRules.Record(Chapter, BeaCase)?.ruleId);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
    }

    /// <summary>T08: Tax credits takes one of her certificate's printed amounts (both offered alike), once it is handed over and explained.</summary>
    private void OpenCredits()
    {
        Level3ChapterCheckpoint chapter = Chapter;
        string refusal = !Level3CertificateRules.Acquired(chapter)
            ? (Level3CertificateRules.CreditStepOpen(chapter) ? Level3ComputationRules.RefuseNoCertificate : "Tax credits come from Bea's withholding certificate, after the tax due.")
            : !Level3CertificateRules.Explained(chapter) ? Level3ComputationRules.RefuseExplainFirst : null;
        if (refusal != null)
        {
            leaf.SetMessage(refusal);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
            return;
        }
        var choices = new List<KeyValuePair<string, string>>();
        foreach (Level3TotalChoice choice in Level3ComputationRules.CreditChoices(chapter, Definition))
            choices.Add(new KeyValuePair<string, string>(choice.ChoiceId, choice.Label + "  " + Level3Calculator.Format(choice.Amount)));
        leafPicker = Level3ComputationRules.Credits;
        DisarmLeaf();
        leaf.OpenPicker("Bea's certificate. Which amount goes on Tax credits?", choices, Level3ComputationRules.Record(chapter, BeaCase)?.creditChoice);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
    }

    private void CloseLeafPicker()
    {
        leafPicker = null;
        leaf?.ClosePicker();
    }

    private void ChooseLeaf(string id)
    {
        string field = leafPicker;
        CloseLeafPicker();
        if (field == null || id == null) return;
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null) return;
        bool[] staleBefore = SheetStale();
        CaseDefinition definition = Definition;
        Level3Checkpointer.TapeCommit outcome = owner.CommitComputation((Level3ChapterCheckpoint c, out string r) => field == Level3ComputationRules.Rule
            ? Level3ComputationRules.SelectRule(c, definition, id, out r)
            : field == Level3ComputationRules.Credits ? Level3ComputationRules.SelectCredit(c, definition, id, out r)
            : Level3ComputationRules.SelectTotal(c, definition, field, id, out r), out string hint);
        SyncLeaf(force: false);
        switch (outcome)
        {
            case Level3Checkpointer.TapeCommit.Attached:
                leaf.SetMessage(Level3ComputationRules.SectionLabel(field) + " chosen. You can change it with PICK. " + StaleNote(staleBefore) + NextStep());
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
                GameplayEvents.RaiseGuideCue(field == Level3ComputationRules.Rule ? CueRuleChosen : field == Level3ComputationRules.Credits ? CueCreditChosen : CueTotalChosen);
                break;
            case Level3Checkpointer.TapeCommit.AlreadyAttached:
                leaf.SetMessage("That is already chosen.");
                break;
            default:
                leaf.SetMessage(string.IsNullOrEmpty(hint) ? "That can't be changed right now." : hint);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
                break;
        }
    }

    /// <summary>Resolves the current authored/chosen source, never the printed string.</summary>
    private Level3CalculatorTransferValue SheetTransferSource(string id)
    {
        Level3ComputationRecord record = Level3ComputationRules.Record(Chapter, BeaCase);
        if (record == null) return null;
        string source = Level3ComputationRules.FieldSource(BeaCase, id);
        if (id == Level3ComputationRules.Gross && !string.IsNullOrEmpty(record.grossChoice))
            return new Level3CalculatorTransferValue(TransferContext, source, "Gross income", record.grossAmount);
        if (id == Level3ComputationRules.Expenses && !string.IsNullOrEmpty(record.expensesChoice))
            return new Level3CalculatorTransferValue(TransferContext, source, "Expenses", record.expensesAmount);
        if (id == Level3ComputationRules.Credits && !string.IsNullOrEmpty(record.creditChoice))
            return new Level3CalculatorTransferValue(TransferContext, source, "Credits", record.creditAmount);
        // A source id in a selected payload is resolved through the same catalogue.
        foreach (string field in new[] { Level3ComputationRules.Gross, Level3ComputationRules.Expenses, Level3ComputationRules.Credits })
            if (id == Level3ComputationRules.FieldSource(BeaCase, field)) return SheetTransferSource(field);
        Level3RuleDefinition rule = Level3ComputationRules.FindRule(Definition, record.ruleId);
        if (rule != null)
            foreach (string part in new[] { Level3ComputationRules.Lower, Level3ComputationRules.Base, Level3ComputationRules.Rate })
                if (id == part || id == Level3ComputationRules.RuleSource(rule.ruleId, part))
                    return new Level3CalculatorTransferValue(TransferContext, Level3ComputationRules.RuleSource(rule.ruleId, part),
                        part == Level3ComputationRules.Rate ? "Rate" : part == Level3ComputationRules.Lower ? "Floor" : "Base tax",
                        part == Level3ComputationRules.Rate ? rule.rate : part == Level3ComputationRules.Lower ? rule.lower : rule.baseTax,
                        part == Level3ComputationRules.Rate);
        foreach (string field in Level3ComputationRules.TapeFields)
        {
            Level3TapeRecord tape = Level3ComputationRules.TapeOf(Chapter, BeaCase, field);
            if (id == Level3ComputationRules.SheetSource(BeaCase, field) && tape != null)
                return new Level3CalculatorTransferValue(TransferContext, id, Level3ComputationRules.SectionLabel(field), tape.result);
        }
        return null;
    }

    private void AttachLeaf(string field)
    {
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null) return;
        Level3Calculator draftOwner = Calculator;
        List<Level3CalcToken> chain = draftOwner.DraftChain;
        string result = draftOwner.DraftResult;
        CaseDefinition definition = Definition;
        Level3Checkpointer.TapeCommit outcome = owner.CommitComputation((Level3ChapterCheckpoint c, out string r) =>
            Level3ComputationRules.AttachTape(c, definition, field, chain, result, out r), out string hint);
        DisarmLeaf();
        switch (outcome)
        {
            case Level3Checkpointer.TapeCommit.Attached:
                // Saved first; only now does the strip tear off and clip into the line.
                StartAttachFlight(Level3ComputationRules.Destination(BeaCase, field), chain, result);
                // The case's own draft (a demonstration that becomes due with this save has its own calculator).
                draftOwner.ConsumeDraft();
                leaf.SetMessage("Tape attached to " + Level3ComputationRules.SectionLabel(field) + ". " + NextStep());
                GameplayEvents.RaiseGuideCue(CueSheetTape);
                break;
            case Level3Checkpointer.TapeCommit.AlreadyAttached:
                leaf.SetMessage("That tape is already on " + Level3ComputationRules.SectionLabel(field) + ".");
                break;
            default:
                // Refused or not saved: the tape stays on the calculator, nothing is attached, no grade.
                leaf.SetMessage(hint);
                break;
        }
        RefreshDisplay();
    }

    private void LeafPrinted() => Say("Result printed");

    // =========================================================
    // REVISED T07/T08: THE FOLDER'S COMPUTATION PAGE (Level3ComputationPage)
    // =========================================================

    /// <summary>Bea's computation: the folder (left) and this calculator (right) are up together.</summary>
    public bool SharesDeskWithFolder => ComputationOpen;

    /// <summary>The sheet destination the printed strip is flying to (the page waits to show it until it lands).</summary>
    public string FlyingDestination => flyDestination;

    /// <summary>A printed result is on the calculator, ready to attach.</summary>
    public bool HasPrintedTape => ComputationOpen && CanDragPrintedTape;

    /// <summary>One short neutral line on the folder's Computation page.</summary>
    private static void Say(string text) => Level3CaseFolder.Active?.ComputationNote(text);

    private RectTransform folderSlot;

    /// <summary>
    /// The page's Calculate: lifts the calculator if it rests, then loads the stage's formula from the chosen
    /// inputs/band (Level3ComputationRules.LoadStage) and prints, exactly like pressing = (one action). Null on a
    /// printed tape, else a plain reason. Nothing is saved: the tape is a draft until it is attached.
    /// </summary>
    public string Calculate(string stage, string op)
    {
        if (!AtDesk) return "Sit at your desk first.";
        if (LessonOpen) return "Try the calculator example first.";
        if (!ComputationOpen) return "Bea's computation cannot be changed right now.";
        if (mode == Mode.Resting) Lift();
        Level3TapeTransfer.Cancel(this);
        if (flyStart >= 0f) FinishAttachFlight();
        RestorePrintedStrip();
        Disarm();
        string refusal = Level3ComputationRules.LoadStage(Calculator, Chapter, Definition, stage, op);
        RefreshDisplay();
        if (refusal != null) { calculator.SetTape(null, animate: false); return refusal; }
        calculator.SetTape(DraftLines(), animate: true, TapeFeedSeconds);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayKeyboardSFX();
        GameplayEvents.RaiseGuideCue(CueTapePrinted);
        return null;
    }

    /// <summary>
    /// The printed tape placed on a Computation result slot (tap tape then slot, or drag it there): the chapter
    /// command saves first (Level3ComputationRules.AttachTape); only then does the strip fly and clip into the
    /// slot. Refused or unsaved: the tape stays on the calculator and the page says why.
    /// </summary>
    public bool AttachPrintedToFolder(string field, RectTransform slot, DataValue value)
    {
        if (!ComputationOpen || slot == null || !CanPlaceTransfer(value, true)) return false;
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null) return false;
        Level3Calculator draftOwner = Calculator;
        List<Level3CalcToken> chain = draftOwner.DraftChain;
        string result = draftOwner.DraftResult;
        CaseDefinition definition = Definition;
        Level3Checkpointer.TapeCommit outcome = owner.CommitComputation((Level3ChapterCheckpoint c, out string r) =>
            Level3ComputationRules.AttachTape(c, definition, field, chain, result, out r), out string hint);
        switch (outcome)
        {
            case Level3Checkpointer.TapeCommit.Attached:
                folderSlot = slot;
                StartAttachFlight(Level3ComputationRules.Destination(BeaCase, field), chain, result);
                // Teaching pass: attaching Bea's taxable income can make the tax-table example due in this
                // same save; the case's own draft is the one that leaves the calculator.
                draftOwner.ConsumeDraft();
                DragVisualManager.Instance?.Hide();
                Say("Result attached");
                GameplayEvents.RaiseGuideCue(CueSheetTape);
                RefreshDisplay();
                return true;
            case Level3Checkpointer.TapeCommit.AlreadyAttached:
                DragVisualManager.Instance?.Hide();
                Say("Already attached");
                return true;
            default:
                // Refused or not saved: the tape stays on the calculator, nothing is attached, no grade.
                Say(string.IsNullOrEmpty(hint) ? "That result can't go here." : hint);
                return false;
        }
    }

    /// <summary>Where a strip flying to a folder slot lands, facing the reader, fitted to the slot.</summary>
    private bool FolderFlightTarget(out Vector3 to, out Quaternion rotation, out Vector3 up, out Vector3 scale)
    {
        to = folderLastTo; rotation = folderLastRotation; up = folderLastUp; scale = folderLastScale;
        // A slot redrawn away mid-flight (the page waits, but a close can rebuild it) keeps its last pose.
        if (folderSlot == null) return folderFlight;
        folderFlight = true;
        Rect rect = folderSlot.rect;
        up = -folderSlot.forward;
        rotation = Quaternion.LookRotation(folderSlot.up, up);
        Vector2 size = new Vector2(rect.width * Mathf.Abs(folderSlot.lossyScale.x), rect.height * Mathf.Abs(folderSlot.lossyScale.y)) * 0.9f;
        // Uniform: the short strip keeps its own proportions and lies wholly inside the slot.
        Vector2 printed = calculator.PrintedTapeSize;
        float fit = Mathf.Min(size.x / Mathf.Max(1e-5f, printed.x), size.y / Mathf.Max(1e-5f, printed.y));
        scale = Vector3.one * fit;
        to = folderSlot.TransformPoint(rect.center) + up * 0.002f;
        folderLastTo = to; folderLastRotation = rotation; folderLastUp = up; folderLastScale = scale;
        return true;
    }

    private bool folderFlight;
    private Vector3 folderLastTo, folderLastUp = Vector3.up, folderLastScale = Vector3.one;
    private Quaternion folderLastRotation = Quaternion.identity;

    private void LeafLanded(string destinationId)
    {
        SyncLeaf(force: true);
        leaf.PopSlot(LeafField(destinationId));
    }

    private void DisarmLeaf()
    {
        if (armedField == null) return;
        armedField = null;
        leaf?.SetArmed(null);
    }

    private bool[] SheetStale()
    {
        var stale = new bool[Level3ComputationRules.TapeFields.Length];
        for (int i = 0; i < stale.Length; i++)
            stale[i] = Level3ComputationRules.IsStale(Chapter, Definition, BeaCase, Level3ComputationRules.TapeFields[i]);
        return stale;
    }

    /// <summary>Lines a change left out of date (their tapes stay, tagged, until replaced).</summary>
    private string StaleNote(bool[] before)
    {
        bool[] now = SheetStale();
        var names = new List<string>();
        for (int i = 0; i < now.Length; i++)
            if (now[i] && !before[i]) names.Add(Level3ComputationRules.SectionLabel(Level3ComputationRules.TapeFields[i]));
        return names.Count == 0 ? "" : string.Join(", ", names) + (names.Count == 1 ? " needs" : " need") + " recalculating now. ";
    }

    /// <summary>What the sheet still waits for, in order (structural only: never whether anything is right).</summary>
    private static string NextStep()
    {
        Level3ChapterCheckpoint chapter = Chapter;
        CaseDefinition definition = Definition;
        Level3ComputationRecord record = Level3ComputationRules.Record(chapter, BeaCase);
        if (record == null) return "";
        if (string.IsNullOrEmpty(record.grossChoice) || string.IsNullOrEmpty(record.expensesChoice)) return "PICK both totals.";
        if (!Level3ComputationRules.IsCurrent(chapter, definition, BeaCase, Level3ComputationRules.Taxable))
            return "Next: Taxable income is Gross income − Allowable expenses.";
        if (string.IsNullOrEmpty(record.ruleId)) return "Next: PICK the rate band (Tax Code Book, Section 7).";
        if (!Level3ComputationRules.IsCurrent(chapter, definition, BeaCase, Level3ComputationRules.TaxDue))
            return "Next: Tax due, with the band's formula (Section 8).";
        // T08: the credit step (structural only, like every line above).
        if (!Level3CertificateRules.Acquired(chapter)) return "Next: Bea is waiting in the conference room with one more paper.";
        if (!Level3CertificateRules.Explained(chapter)) return "Next: answer Bea's question about her certificate.";
        if (string.IsNullOrEmpty(record.creditChoice)) return "Next: PICK Tax credits from her certificate (Section 9).";
        if (!Level3ComputationRules.IsCurrent(chapter, definition, BeaCase, Level3ComputationRules.Payable))
            return "Next: Remaining payable is Tax due − Tax credits.";
        // T09: stamp, then the Auditor (structural only).
        if (Level3ReviewRules.StampStale(chapter, BeaCase)) return "The sheet changed after its stamp: stamp it again.";
        if (!Level3ReviewRules.StampCurrent(chapter, BeaCase)) return "Next: stamp the sheet: READY for computation review, or NOT READY if something is open.";
        return "Stamped. Hand your Case Folder to the Auditor.";
    }
}
