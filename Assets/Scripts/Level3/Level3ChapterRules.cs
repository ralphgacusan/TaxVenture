using System;
using System.Collections.Generic;

/// <summary>
/// Level 3's saved chapter phase (SPEC "Deterministic chapter phases"). Saved
/// by NAME in Level3ChapterCheckpoint.phase, so the numbers below are only an
/// order; never renumber or reuse a name.
/// </summary>
public enum Level3Phase
{
    Reception = 0,
    ToolPractice = 1,
    BeaRecords = 2,
    WorksheetDraft = 3,
    WorksheetCorrections = 4,
    WorksheetReady = 5,
    BeaComputation = 6,
    BeaReview = 7,
    BeaDelivery = 8,
    CompanyAppointment = 9,
    CompanyComputation = 10,
    CompanyReview = 11,
    CompanyDelivery = 12,
    ClosingFinding = 13,
    WrapUpPending = 14,
    CompletedOffice = 15
}

/// <summary>What a chapter command did.</summary>
public enum Level3CommandOutcome
{
    /// <summary>The candidate changed; write it, then publish it.</summary>
    Applied,
    /// <summary>The same action was already applied (a repeat, a restore): nothing changes, nothing is shown.</summary>
    Duplicate,
    /// <summary>Not allowed now (wrong phase, unknown transition, unusable record): nothing changes.</summary>
    Refused
}

/// <summary>How a Level 3 attempt starts from what is saved.</summary>
public enum Level3EntryDecision
{
    /// <summary>No chapter snapshot: a fresh chapter at reception.</summary>
    Fresh,
    /// <summary>A usable snapshot: restore its phase quietly (no intro, acquisition or reward replays).</summary>
    Resume,
    /// <summary>A snapshot this build cannot use (newer version, unknown phase, other content revision): kept untouched, this attempt runs without saving.</summary>
    Blocked,
    /// <summary>A usable pre-revision (version 1) snapshot: continued once (TryContinueLegacy), written, then restored quietly.</summary>
    Continue
}

/// <summary>
/// PURPOSE:
/// Level 3's chapter rules (Level3 T02), pure C# so the regression suite runs
/// them directly. The GameStateMachine stays the coarse game-phase owner;
/// this is saved domain data with a bounded transition validator (the Level
/// 2 review-phase pattern), not a second ticking state machine.
///
/// - Entry: a fresh chapter starts at Reception with Bea's case active and
///   BOTH case subrecords present; a usable saved snapshot always wins over
///   the introduction; an unusable one is kept and never overwritten.
/// - Commands (TryAdvance / AssignBea) change a COPY: validate the phase,
///   the allowed transition and the action id, then the caller writes the
///   candidate and publishes it only after the write succeeded.
/// - Legacy guard: the old single-case completion route (CaseComplete ->
///   LevelComplete, ArchiveCase, PrepareReturn) never runs in Level 3.
/// - Story gate: only a story run of a campaign that completed Chapter 2
///   writes the chapter; Editor, replay and practice attempts stay local.
/// - Revision (3 November 2026, snapshot version 2): the reception assignment
///   leads straight to Bea's records; the budget practice (ToolPractice) is
///   retired from fresh play and kept only as a legacy identity. A version 1
///   snapshot is continued once (TryContinueLegacy) without resetting,
///   regrading or paying anything again.
///
/// T02 implements entry and the reception assignment only; later tickets
/// add their commands on these transitions. T14 (Level3ClosingRules) closes
/// the Bea-only chapter: ClosingFinding -> WrapUpPending -> CompletedOffice.
/// </summary>
public static class Level3ChapterRules
{
    public const string LevelId = "Level_03";
    public const string PreviousLevelId = "Level_02";
    public const string ContentRevision = "L3-2026-v1";
    public const string BeaCaseId = "L3-BEA-001";
    public const string CompanyCaseId = "L3-MARINE-001";
    public const string BeaResource = "case_bea_001";
    public const string CompanyResource = "case_marine_001";

    /// <summary>The reception assignment (once per campaign chapter).</summary>
    public const string AssignBeaAction = "Level_03/reception-assigned";
    public const string ReceptionAssignedMilestone = "reception-assigned";
    /// <summary>Revision: a version 1 chapter left waiting in the retired practice moved on to Bea's records (no practice completion, teaching or reward).</summary>
    public const string PracticeRetiredAction = "Level_03/practice-retired";
    public const string PracticeRetiredMilestone = "practice-retired";

    // Objective triggers (Levels/Level_03/objectives.json) and guide lessons (Tutorials/Level_03/guide.json).
    public const string AssignmentTrigger = "event:ChapterAssignmentReceived";
    public const string ReceptionistTrigger = "event:ReceptionistTalkedTo";
    public const string CalculatorTrigger = "event:CalculatorPracticeAttached";
    /// <summary>T04: Bea's records were handed over and saved.</summary>
    public const string RecordsTrigger = "event:ClientRecordsCollected";
    /// <summary>T05: the worksheet draft was first filled in and saved (never a grade).</summary>
    public const string WorksheetTrigger = "event:WorksheetDrafted";
    /// <summary>T06: a saved check found every decision right and both totals supported (ready to collect).</summary>
    public const string WorksheetReadyTrigger = "event:WorksheetReady";
    /// <summary>T06: Bea's checked worksheet was collected, saved together with its rewards.</summary>
    public const string WorksheetCollectedTrigger = "event:WorksheetCollected";
    /// <summary>T07: Bea's taxable income and tax due both first had current tapes (saved; structural, never a grade).</summary>
    public const string TaxDueTrigger = "event:TaxDueAttached";
    /// <summary>T08: Bea handed over her withholding certificate (saved); a credit was linked and the remaining payable first had a current tape (saved).</summary>
    public const string CertificateTrigger = "event:WithholdingCertificateReceived";
    public const string PayableTrigger = "event:RemainingPayableAttached";
    /// <summary>T09: Bea's computation was approved by the Auditor (saved with its 100 coins).</summary>
    public const string ApprovedTrigger = "event:BeaCaseApproved";
    /// <summary>T10: Bea's copy collected from the Auditor, and handed to her after the explanation (both saved first).</summary>
    public const string CopyPickedUpTrigger = "event:ClientCopyPickedUp";
    public const string CopyDeliveredTrigger = "event:ClientCopyHandedOver";
    /// <summary>T14: Bea's supported finding (the existing FindingRecorded event), Quax's message and the acknowledged chapter result (each saved first).</summary>
    public const string FindingTrigger = "event:FindingRecorded";
    public const string MessageTrigger = "event:ChapterMessageRead";
    public const string WrapUpTrigger = "event:ChapterWrapUpConfirmed";

    /// <summary>T04: what Bea hands over at her table (B-WHT waits for the office credit step, T08: Level3CertificateRules).</summary>
    public static readonly string[] BeaHandoverSources = { "B-ID", "B-BG", "B-REV", "B-EXP", "B-RENT", "B-SUB", "B-PRINT", "B-PERSONAL", "B-DRAFT" };
    public const string BeaCreditSource = "B-WHT";
    public const string BeaRecordsAction = "Level_03/bea-records";
    public const string BeaRecordsMilestone = "bea-records-collected";
    /// <summary>T04: the handover's lesson (records on the table and in the folder).</summary>
    public const string BeaRecordsLesson = "bea-records";
    public const string WelcomeLesson = "welcome";
    /// <summary>Revision: after the assignment, Arthur points to Bea's records (replaces the calculator task).</summary>
    public const string AssignmentLesson = "bea-assignment";
    /// <summary>Revised T03: the calculator's first-use example at Bea's computation (shown until calculatorTaught is saved).</summary>
    public const string CalculatorLessonId = "calculator-lesson";
    public const string CalculatorLesson = "calculator-intro";
    /// <summary>T03: the guided first tape (waits on the player's own source tap, = and attach).</summary>
    public const string PracticeLesson = "calculator-practice";
    /// <summary>T03: the hint for the second, independent tape.</summary>
    public const string OwnTurnLesson = "calculator-own-turn";
    /// <summary>T03: practice finished; Bea is next.</summary>
    public const string PracticeDoneLesson = "calculator-done";
    /// <summary>T05 (revised): the machine's hands-on steps (feed, choose, pull), its Why? hint and its all-filed hint (same ids as the old board's).</summary>
    public const string WorksheetLesson = "worksheet-intro";
    public const string WorksheetNotesLesson = "worksheet-notes";
    public const string WorksheetDraftedLesson = "worksheet-drafted";
    /// <summary>T06: the calculator at Bea's table (subtotal tapes), the deliberate check, corrections, ready, collected.</summary>
    public const string SubtotalsLesson = "worksheet-subtotals";
    /// <summary>Revised T06: the open batch is filed, Check batch is the primary action (T05's lesson id, now waiting for the check).</summary>
    public const string BatchFiledLesson = "machine-batch-filed";
    public const string CheckLesson = "worksheet-check";
    public const string CorrectionsLesson = "worksheet-corrections";
    public const string ReadyLesson = "worksheet-ready";
    public const string CollectedLesson = "worksheet-collected";
    /// <summary>T07: the computation sheet at the office desk, step by step (each waits for the player's own action).</summary>
    public const string ComputationIntroLesson = "computation-intro";
    public const string ComputationTotalsLesson = "computation-totals";
    public const string CalculatorLiftLesson = "calculator-lift";
    public const string ComputationTaxableLesson = "computation-taxable";
    public const string ComputationRuleLesson = "computation-rule";
    public const string ComputationTaxLesson = "computation-tax";
    public const string ComputationDoneLesson = "computation-done";
    /// <summary>T08: Bea's certificate in the conference room, linking the credit, the remaining payable, done.</summary>
    public const string CreditCertificateLesson = "credit-certificate";
    public const string CreditLinkLesson = "credit-link";
    public const string CreditPayableLesson = "credit-payable";
    public const string CreditDoneLesson = "credit-done";
    /// <summary>T09: stamping the sheet, handing the folder to the Auditor, a returned review, the approval.</summary>
    public const string ReviewStampLesson = "review-stamp";
    public const string ReviewHandInLesson = "review-handin";
    public const string ReviewReturnedLesson = "review-returned";
    public const string ReviewApprovedLesson = "review-approved";
    /// <summary>T14: Bea's question about her draft, then the receptionist's message.</summary>
    public const string ClosingFindingLesson = "closing-finding";
    public const string ClosingMessageLesson = "closing-message";
    /// <summary>Teaching pass (6 November 2026): Worth the Tax?'s practice packet, the tax-table example and the withholding example.</summary>
    public const string SortingPracticeLesson = "sorting-practice";
    public const string TaxTableLesson = "tax-table-lesson";
    public const string WithholdingLesson = "withholding-lesson";

    /// <summary>
    /// T07: inherited Level 1 flows that would leave ComputeTaxesState (the old
    /// stamp set, the office corkboard). Refused while Bea's computation is open;
    /// Level 3's own review (T09) decides what may follow it.
    /// </summary>
    public static readonly string[] ComputationForeignStates = { "StampAssessmentState", "AnalyzeEvidenceState" };

    /// <summary>
    /// Revised T03: Level 1's first-folder-open step (ReceiveCase -> ReviewDocuments). The
    /// restored Case Folder opens through the same code; Level 3's coarse state stays the
    /// chapter's, so this step never runs here.
    /// </summary>
    public static readonly string[] FolderForeignStates = { "ReviewDocumentsState" };

    /// <summary>The old single-case end route; Level 3 completes through its own chapter commands (T14: Level3ClosingRules).</summary>
    public static readonly string[] LegacyCompletionStates =
        { "CaseCompleteState", "LevelCompleteState", "ArchiveCaseState", "PrepareReturnState" };

    private static readonly Dictionary<Level3Phase, Level3Phase[]> Next = new Dictionary<Level3Phase, Level3Phase[]>
    {
        // Revision: the assignment leads straight to Bea's records; ToolPractice is never entered again.
        [Level3Phase.Reception] = new[] { Level3Phase.BeaRecords },
        [Level3Phase.ToolPractice] = new[] { Level3Phase.BeaRecords },
        [Level3Phase.BeaRecords] = new[] { Level3Phase.WorksheetDraft },
        [Level3Phase.WorksheetDraft] = new[] { Level3Phase.WorksheetCorrections, Level3Phase.WorksheetReady },
        [Level3Phase.WorksheetCorrections] = new[] { Level3Phase.WorksheetReady },
        [Level3Phase.WorksheetReady] = new[] { Level3Phase.BeaComputation },
        [Level3Phase.BeaComputation] = new[] { Level3Phase.BeaReview },
        // A returned review sends the work back to computation; approval moves to delivery.
        [Level3Phase.BeaReview] = new[] { Level3Phase.BeaComputation, Level3Phase.BeaDelivery },
        // T10: the Bea-only release closes after her explained delivery; the company appointment stays deferred.
        [Level3Phase.BeaDelivery] = new[] { Level3Phase.CompanyAppointment, Level3Phase.ClosingFinding },
        [Level3Phase.CompanyAppointment] = new[] { Level3Phase.CompanyComputation },
        [Level3Phase.CompanyComputation] = new[] { Level3Phase.CompanyReview },
        [Level3Phase.CompanyReview] = new[] { Level3Phase.CompanyComputation, Level3Phase.CompanyDelivery },
        [Level3Phase.CompanyDelivery] = new[] { Level3Phase.ClosingFinding },
        [Level3Phase.ClosingFinding] = new[] { Level3Phase.WrapUpPending },
        [Level3Phase.WrapUpPending] = new[] { Level3Phase.CompletedOffice },
        [Level3Phase.CompletedOffice] = new Level3Phase[0]
    };

    // =========================================================
    // IDENTITY
    // =========================================================

    /// <summary>"case_bea_001" -> "L3-BEA-001" (level.json lists resource stems); null for anything else.</summary>
    public static string CaseIdForResource(string resource) =>
        resource == BeaResource ? BeaCaseId : resource == CompanyResource ? CompanyCaseId : null;

    /// <summary>
    /// Bea is active through her delivery and the company appointment; the company binds at its admission.
    /// T10: the closing phases keep Bea active when the company case was never admitted (the Bea-only release).
    /// </summary>
    public static string ExpectedActiveCase(Level3Phase phase, bool companyAdmitted = true) =>
        phase >= Level3Phase.CompanyComputation && (phase <= Level3Phase.CompanyDelivery || companyAdmitted) ? CompanyCaseId : BeaCaseId;

    private static bool CompanyAdmitted(Level3ChapterCheckpoint state) => state?.Case(CompanyCaseId)?.admitted == true;

    /// <summary>A saved phase name -> the phase (exact, defined names only).</summary>
    public static bool TryParsePhase(string name, out Level3Phase phase)
    {
        phase = Level3Phase.Reception;
        if (string.IsNullOrWhiteSpace(name) || name.Trim() != name) return false;
        foreach (Level3Phase candidate in (Level3Phase[])Enum.GetValues(typeof(Level3Phase)))
        {
            if (candidate.ToString() != name) continue;
            phase = candidate;
            return true;
        }
        return false;
    }

    public static bool IsAllowed(Level3Phase from, Level3Phase to) =>
        Next.TryGetValue(from, out Level3Phase[] targets) && Array.IndexOf(targets, to) >= 0;

    // =========================================================
    // ENTRY / RESUME
    // =========================================================

    /// <summary>A fresh chapter: reception, Bea's case active, both case subrecords present, nothing acquired.</summary>
    public static Level3ChapterCheckpoint CreateEntry(string contentRevision = ContentRevision) => new Level3ChapterCheckpoint
    {
        version = Level3ChapterCheckpoint.CurrentVersion,
        contentRevision = contentRevision ?? "",
        phase = Level3Phase.Reception.ToString(),
        activeCaseId = BeaCaseId,
        cases = new List<Level3CaseProgress>
        {
            new Level3CaseProgress { caseId = BeaCaseId },
            new Level3CaseProgress { caseId = CompanyCaseId }
        }
    };

    /// <summary>Null when the snapshot can be restored by this build; otherwise why not. A version 1 snapshot is usable only after TryContinueLegacy.</summary>
    public static string Problem(Level3ChapterCheckpoint state, string contentRevision = ContentRevision)
    {
        if (state == null || !state.IsSet) return "no chapter snapshot";
        if (state.version > Level3ChapterCheckpoint.CurrentVersion) return $"chapter snapshot version {state.version} is newer than this game ({Level3ChapterCheckpoint.CurrentVersion})";
        if (state.version == Level3ChapterCheckpoint.LegacyVersion) return "chapter snapshot version 1 must be continued into the revision first";
        if (state.version < Level3ChapterCheckpoint.CurrentVersion) return $"chapter snapshot version {state.version} is unknown";
        string shape = ShapeProblem(state, contentRevision);
        if (shape != null) return shape;
        if (PhaseOf(state) == Level3Phase.ToolPractice) return "a revised chapter never waits in the retired calculator practice";
        return null;
    }

    /// <summary>The phase, case and acquisition invariants every snapshot version shares.</summary>
    private static string ShapeProblem(Level3ChapterCheckpoint state, string contentRevision)
    {
        if (!TryParsePhase(state.phase, out Level3Phase phase)) return $"unknown chapter phase '{state.phase}'";
        if (state.contentRevision != contentRevision) return $"made against content '{state.contentRevision}', current '{contentRevision}'";
        if (state.cases == null || state.cases.Count != 2 || state.Case(BeaCaseId) == null || state.Case(CompanyCaseId) == null)
            return "the snapshot must hold exactly Bea's and the company's case";
        if (state.activeCaseId != ExpectedActiveCase(phase, CompanyAdmitted(state))) return $"active case '{state.activeCaseId}' does not match phase {phase}";
        if (phase >= Level3Phase.ToolPractice && !state.Case(BeaCaseId).admitted) return "Bea's case is past reception but was never assigned";
        if (phase < Level3Phase.CompanyAppointment && state.Case(CompanyCaseId).admitted) return "the company case was admitted before Bea's delivery";
        if (phase < Level3Phase.BeaComputation && state.Case(BeaCaseId).acquiredSourceIds.Contains(BeaCreditSource))
            return "Bea's withholding certificate was acquired before the credit step";
        return null;
    }

    /// <summary>Fresh with nothing saved; resume a usable snapshot; continue a usable version 1 one; otherwise keep it untouched (Blocked).</summary>
    public static Level3EntryDecision Decide(Level3ChapterCheckpoint saved, string contentRevision = ContentRevision)
    {
        if (saved == null || !saved.IsSet) return Level3EntryDecision.Fresh;
        if (saved.version == Level3ChapterCheckpoint.LegacyVersion)
            return TryContinueLegacy(saved, out _, out _, contentRevision) ? Level3EntryDecision.Continue : Level3EntryDecision.Blocked;
        return Problem(saved, contentRevision) == null ? Level3EntryDecision.Resume : Level3EntryDecision.Blocked;
    }

    /// <summary>
    /// Revision: a usable version 1 snapshot as a version 2 COPY (the saved one
    /// is never touched; the caller writes the copy, then restores it quietly).
    /// Everything is carried exactly: phase, actions, milestones, both cases
    /// (acquired papers, collected work, computation choices, stamp, review,
    /// approval, frozen values, copy phase, review clock), every tape with its
    /// provenance, the worksheet draft and its checks. Only these change:
    /// - a chapter waiting in the retired ToolPractice moves to Bea's records
    ///   under its own action id and milestone, never the practice's own
    ///   completion (no teaching, mastery or reward is invented);
    /// - a frozen first check is labelled with the twelve-decision assessment
    ///   it was graded under, so it is never regraded or graded again;
    /// - calculatorTaught records whether the player already learned the
    ///   calculator (both practice tapes, or any tape of a case).
    /// False (continued null, reason set) for anything else: the snapshot is
    /// then kept untouched and Blocked. Wallet, trust, claims and possessions
    /// live outside the snapshot and are never part of this.
    /// </summary>
    public static bool TryContinueLegacy(Level3ChapterCheckpoint legacy, out Level3ChapterCheckpoint continued, out string reason,
        string contentRevision = ContentRevision)
    {
        continued = null;
        if (legacy == null || !legacy.IsSet) { reason = "no chapter snapshot"; return false; }
        if (legacy.version != Level3ChapterCheckpoint.LegacyVersion) { reason = $"chapter snapshot version {legacy.version} is not a version 1 snapshot"; return false; }
        reason = ShapeProblem(legacy, contentRevision);
        if (reason != null) return false;

        Level3ChapterCheckpoint copy = legacy.Copy();
        copy.version = Level3ChapterCheckpoint.CurrentVersion;
        if (PhaseOf(copy) == Level3Phase.ToolPractice)
        {
            copy.phase = Level3Phase.BeaRecords.ToString();
            copy.activeCaseId = ExpectedActiveCase(Level3Phase.BeaRecords);
            if (!copy.actionIds.Contains(PracticeRetiredAction)) copy.actionIds.Add(PracticeRetiredAction);
            if (!copy.milestones.Contains(PracticeRetiredMilestone)) copy.milestones.Add(PracticeRetiredMilestone);
        }
        Level3WorksheetResult check = copy.worksheetCheck;
        if (check.firstChecked && string.IsNullOrEmpty(check.assessmentVersion)) check.assessmentVersion = Level3WorksheetCheck.AssessmentVersion;
        copy.calculatorTaught = copy.calculatorTaught || Level3PracticeRules.NextSlot(legacy) == null ||
                                copy.tapes.Exists(t => t != null && t.caseId != Level3PracticeRules.CaseId);

        reason = Problem(copy, contentRevision);
        if (reason != null) return false;
        continued = copy;
        return true;
    }

    /// <summary>
    /// Bea's record work is independent of its compulsory first lesson: the
    /// first SAVED filing (T05: the machine's lever, whose last lesson step is
    /// "Pull to file"; a pre-revision draft's placed row counts the same), or
    /// the chapter is past the draft. Loading a record or turning the selector
    /// saves nothing, so it never starts her review clock.
    /// </summary>
    public static bool BeaWorkIndependent(Level3ChapterCheckpoint state)
    {
        Level3Phase phase = PhaseOf(state);
        if (state == null || phase < Level3Phase.WorksheetDraft) return false;
        if (phase > Level3Phase.WorksheetDraft) return true;
        return state.worksheet?.strips != null && state.worksheet.strips.Exists(s => s != null && Level3WorksheetRules.RevisedDestination(s.section) != "");
    }

    public static Level3Phase PhaseOf(Level3ChapterCheckpoint state) =>
        state != null && TryParsePhase(state.phase, out Level3Phase phase) ? phase : Level3Phase.Reception;

    // =========================================================
    // COMMANDS (on a copy; the caller writes, then publishes)
    // =========================================================

    /// <summary>
    /// One allowed phase step under a stable action id. Refused (no change)
    /// unless the record is usable, it is in 'from' and 'from -> to' is a
    /// listed transition; a repeated action id is a Duplicate (no change).
    /// </summary>
    public static Level3CommandOutcome TryAdvance(Level3ChapterCheckpoint candidate, Level3Phase from, Level3Phase to, string actionId, out string reason)
    {
        reason = null;
        if (string.IsNullOrWhiteSpace(actionId)) { reason = "missing action id"; return Level3CommandOutcome.Refused; }
        if (candidate != null && candidate.actionIds != null && candidate.actionIds.Contains(actionId.Trim()))
        {
            reason = "already applied";
            return Level3CommandOutcome.Duplicate;
        }
        reason = Problem(candidate);
        if (reason != null) return Level3CommandOutcome.Refused;
        Level3Phase current = PhaseOf(candidate);
        if (current != from) { reason = $"the chapter is at {current}, not {from}"; return Level3CommandOutcome.Refused; }
        if (!IsAllowed(from, to)) { reason = $"{from} -> {to} is not a chapter transition"; return Level3CommandOutcome.Refused; }

        candidate.phase = to.ToString();
        candidate.activeCaseId = ExpectedActiveCase(to, CompanyAdmitted(candidate));
        candidate.actionIds.Add(actionId.Trim());
        return Level3CommandOutcome.Applied;
    }

    /// <summary>
    /// T04: Bea hands her records over at her table (BeaRecords -> WorksheetDraft):
    /// the nine original/working papers become acquired sources of her case, in
    /// the same candidate as the phase step. Never B-WHT; never a reward.
    /// </summary>
    public static Level3CommandOutcome CollectBeaRecords(Level3ChapterCheckpoint candidate, out string reason)
    {
        Level3CommandOutcome outcome = TryAdvance(candidate, Level3Phase.BeaRecords, Level3Phase.WorksheetDraft, BeaRecordsAction, out reason);
        if (outcome != Level3CommandOutcome.Applied) return outcome;
        Level3CaseProgress bea = candidate.Case(BeaCaseId);
        foreach (string sourceId in BeaHandoverSources)
            if (!bea.acquiredSourceIds.Contains(sourceId)) bea.acquiredSourceIds.Add(sourceId);
        if (!candidate.milestones.Contains(BeaRecordsMilestone)) candidate.milestones.Add(BeaRecordsMilestone);
        return outcome;
    }

    /// <summary>The receptionist's assignment: Bea's case is admitted and the next task is her records (Reception -> BeaRecords; no budget practice).</summary>
    public static Level3CommandOutcome AssignBea(Level3ChapterCheckpoint candidate, out string reason)
    {
        Level3CommandOutcome outcome = TryAdvance(candidate, Level3Phase.Reception, Level3Phase.BeaRecords, AssignBeaAction, out reason);
        if (outcome != Level3CommandOutcome.Applied) return outcome;
        candidate.Case(BeaCaseId).admitted = true;
        if (!candidate.milestones.Contains(ReceptionAssignedMilestone)) candidate.milestones.Add(ReceptionAssignedMilestone);
        return outcome;
    }

    // =========================================================
    // QUIET RESTORE (observers catch up; nothing is replayed)
    // =========================================================

    /// <summary>Objective triggers a saved phase has already passed.</summary>
    public static List<string> ImpliedTriggers(Level3Phase phase)
    {
        var triggers = new List<string>();
        if (phase >= Level3Phase.ToolPractice) { triggers.Add(ReceptionistTrigger); triggers.Add(AssignmentTrigger); }
        if (phase >= Level3Phase.BeaRecords) triggers.Add(CalculatorTrigger);
        if (phase >= Level3Phase.WorksheetDraft) triggers.Add(RecordsTrigger);
        if (phase >= Level3Phase.WorksheetCorrections) triggers.Add(WorksheetTrigger);
        if (phase >= Level3Phase.WorksheetReady) triggers.Add(WorksheetReadyTrigger);
        if (phase >= Level3Phase.BeaComputation) triggers.Add(WorksheetCollectedTrigger);
        return triggers;
    }

    /// <summary>As above, plus the worksheet step once the saved draft was filled in.</summary>
    public static List<string> ImpliedTriggers(Level3ChapterCheckpoint state)
    {
        List<string> triggers = ImpliedTriggers(PhaseOf(state));
        if (state?.worksheet != null && state.worksheet.drafted && !triggers.Contains(WorksheetTrigger)) triggers.Add(WorksheetTrigger);
        if (state?.Case(BeaCaseId)?.computation != null && state.Case(BeaCaseId).computation.taxDueNoted) triggers.Add(TaxDueTrigger);
        // T08: a saved certificate handover and a saved first current remaining payable.
        if (Level3CertificateRules.Acquired(state)) triggers.Add(CertificateTrigger);
        if (state?.Case(BeaCaseId)?.computation != null && state.Case(BeaCaseId).computation.payableNoted) triggers.Add(PayableTrigger);
        // T09: a saved approval.
        if (Level3ReviewRules.CaseDone(state)) triggers.Add(ApprovedTrigger);
        // T10: a saved pickup and a saved delivery of her copy.
        string copy = state?.Case(BeaCaseId)?.copyPhase;
        if (copy == Level3DeliveryRules.CopyCarried || copy == Level3DeliveryRules.CopyDone) triggers.Add(CopyPickedUpTrigger);
        if (copy == Level3DeliveryRules.CopyDone) triggers.Add(CopyDeliveredTrigger);
        // T14: the saved finding, message and acknowledged result.
        if (Level3ClosingRules.FindingRecorded(state)) triggers.Add(FindingTrigger);
        if (Level3ClosingRules.MessageRead(state)) triggers.Add(MessageTrigger);
        if (Level3ClosingRules.Completed(state)) triggers.Add(WrapUpTrigger);
        return triggers;
    }

    /// <summary>Guide lessons a resumed phase must not show again (the welcome card and the task that was already presented).</summary>
    public static List<string> ImpliedLessons(Level3Phase phase)
    {
        var lessons = new List<string>();
        if (phase >= Level3Phase.ToolPractice) { lessons.Add(WelcomeLesson); lessons.Add(CalculatorLesson); }
        if (phase >= Level3Phase.BeaRecords) { lessons.Add(PracticeLesson); lessons.Add(OwnTurnLesson); lessons.Add(PracticeDoneLesson); }
        if (phase >= Level3Phase.WorksheetDraft) { lessons.Add(BeaRecordsLesson); lessons.Add(AssignmentLesson); }
        return lessons;
    }

    /// <summary>As above, plus the guided practice lesson once its first tape is attached (a resume mid-practice).</summary>
    public static List<string> ImpliedLessons(Level3ChapterCheckpoint state)
    {
        Level3Phase phase = PhaseOf(state);
        List<string> lessons = ImpliedLessons(phase);
        if (phase == Level3Phase.ToolPractice && state.Tape(Level3PracticeRules.FirstSlot) != null) lessons.Add(PracticeLesson);
        // T05: a draft with strips on it has been taught; a filled-in draft has had its hint.
        Level3WorksheetDraft draft = state?.worksheet;
        // T05: the machine lesson once a record was filed; its Why? hint once a reason was saved (a pre-revision note counts).
        if (phase >= Level3Phase.WorksheetCorrections || (draft != null && draft.strips.Count > 0)) lessons.Add(WorksheetLesson);
        if (phase >= Level3Phase.WorksheetCorrections || (draft?.notes != null && draft.notes.Exists(n => n != null && !string.IsNullOrEmpty(n.reason))))
            lessons.Add(WorksheetNotesLesson);
        if (phase >= Level3Phase.WorksheetCorrections || (draft != null && draft.drafted)) lessons.Add(WorksheetDraftedLesson);
        // T06: a tape already attached has been taught; a checked worksheet has had its check and correction hints.
        if (phase >= Level3Phase.WorksheetCorrections || (state?.tapes != null && state.tapes.Exists(t => t != null && t.caseId == BeaCaseId)))
            lessons.Add(SubtotalsLesson);
        // Revised T06: the machine's batch-check lessons. Its first saved check has been taught; "look
        // again" once a check found a mistake (more checks than accepted batches); "batch accepted" once
        // one was accepted.
        Level3WorksheetResult check = state?.worksheetCheck;
        bool machineChecked = Level3WorksheetCheck.MachineChecked(state);
        int accepted = check?.acceptedBatches != null ? check.acceptedBatches.Count : 0;
        if (machineChecked) lessons.Add(BatchFiledLesson);
        if (phase >= Level3Phase.WorksheetCorrections || (machineChecked && check.checks > accepted)) lessons.Add(CheckLesson);
        if (phase >= Level3Phase.WorksheetReady || accepted > 0) lessons.Add(CorrectionsLesson);
        if (phase >= Level3Phase.BeaComputation) { lessons.Add(ReadyLesson); lessons.Add(CollectedLesson); }
        // T07: each computation step already done on the saved sheet has been taught.
        Level3ComputationRecord sheet = state?.Case(BeaCaseId)?.computation;
        if (phase > Level3Phase.BeaComputation || (sheet != null && sheet.revision > 0)) lessons.Add(ComputationIntroLesson);
        if (phase > Level3Phase.BeaComputation || state?.calculatorTaught == true) lessons.Add(CalculatorLiftLesson);
        if (phase > Level3Phase.BeaComputation || (sheet != null && !string.IsNullOrEmpty(sheet.grossChoice) && !string.IsNullOrEmpty(sheet.expensesChoice))) lessons.Add(ComputationTotalsLesson);
        if (phase > Level3Phase.BeaComputation || state?.Tape(Level3ComputationRules.Destination(BeaCaseId, Level3ComputationRules.Taxable)) != null) lessons.Add(ComputationTaxableLesson);
        if (phase > Level3Phase.BeaComputation || (sheet != null && !string.IsNullOrEmpty(sheet.ruleId))) lessons.Add(ComputationRuleLesson);
        if (phase > Level3Phase.BeaComputation || (sheet != null && sheet.taxDueNoted)) { lessons.Add(ComputationTaxLesson); lessons.Add(ComputationDoneLesson); }
        // T08: each credit step already on the saved sheet has been taught.
        if (phase > Level3Phase.BeaComputation || Level3CertificateRules.Acquired(state)) lessons.Add(CreditCertificateLesson);
        if (phase > Level3Phase.BeaComputation || (sheet != null && !string.IsNullOrEmpty(sheet.creditChoice))) lessons.Add(CreditLinkLesson);
        if (phase > Level3Phase.BeaComputation || (sheet != null && sheet.payableNoted)) { lessons.Add(CreditPayableLesson); lessons.Add(CreditDoneLesson); }
        // T09: a stamp already on the sheet, a hand-in already decided, an approval.
        Level3CaseProgress bea = state?.Case(BeaCaseId);
        if (phase > Level3Phase.BeaComputation || (bea?.stamp != null && bea.stamp.type.Length > 0)) lessons.Add(ReviewStampLesson);
        if (phase > Level3Phase.BeaComputation || (bea?.review != null && bea.review.submissions > 0)) lessons.Add(ReviewHandInLesson);
        if (phase > Level3Phase.BeaComputation) { lessons.Add(ReviewReturnedLesson); lessons.Add(ReviewApprovedLesson); }
        // T14: the closing hints once their step is saved.
        if (Level3ClosingRules.FindingRecorded(state)) lessons.Add(ClosingFindingLesson);
        if (Level3ClosingRules.MessageRead(state)) lessons.Add(ClosingMessageLesson);
        // Revised T03: the calculator's example is taught once (saved); a taught chapter never shows it again.
        if (state != null && state.calculatorTaught) lessons.Add(CalculatorLessonId);
        // Teaching pass: each demonstration once its marker is saved, or once the chapter is past its step
        // (an older save never replays one). The packet also taught the machine's feed/choose/pull hint.
        if (Level3SortingPractice.Taught(state) || phase > Level3Phase.WorksheetDraft || (draft != null && draft.strips.Count > 0))
        {
            lessons.Add(SortingPracticeLesson);
            if (Level3SortingPractice.Taught(state) && !lessons.Contains(WorksheetLesson)) lessons.Add(WorksheetLesson);
        }
        bool reviewed = bea?.review != null && bea.review.submissions > 0;
        if (Level3PracticeRules.TaxTaught(state) || phase > Level3Phase.BeaComputation || reviewed || (sheet != null && (!string.IsNullOrEmpty(sheet.ruleId) || sheet.taxDueNoted)))
            lessons.Add(TaxTableLesson);
        if (Level3PracticeRules.CreditTaught(state) || phase > Level3Phase.BeaComputation || reviewed || (sheet != null && (!string.IsNullOrEmpty(sheet.creditChoice) || sheet.payableNoted)))
            lessons.Add(WithholdingLesson);
        return lessons;
    }

    /// <summary>The coarse GameStateMachine state a restored phase sits in (null: owned by a later ticket).</summary>
    public static string CoarseState(Level3Phase phase) =>
        phase <= Level3Phase.WorksheetReady ? "ReceiveCaseState" : phase == Level3Phase.BeaComputation ? "ComputeTaxesState" : null;

    /// <summary>The chapter's transition guard: never the legacy completion route or Level 1's folder-review step (T03); from Bea's computation to her delivery (T09), no inherited Level 1 stamp/corkboard flow.</summary>
    public static bool TransitionAllowed(Level3Phase phase, string stateName) =>
        LegacyTransitionAllowed(stateName) && Array.IndexOf(FolderForeignStates, stateName) < 0 &&
        !(phase >= Level3Phase.BeaComputation && phase <= Level3Phase.BeaDelivery && Array.IndexOf(ComputationForeignStates, stateName) >= 0);

    /// <summary>False for the old single-case completion route in every Level 3 phase.</summary>
    public static bool LegacyTransitionAllowed(string stateName) =>
        string.IsNullOrEmpty(stateName) || Array.IndexOf(LegacyCompletionStates, stateName) < 0;

    // =========================================================
    // CAMPAIGN
    // =========================================================

    /// <summary>The chapter is a story continuation of a completed Chapter 2.</summary>
    public static bool StoryEntryAllowed(PlayerProgressData data) =>
        data != null && data.HasCampaign && data.campaign.IsChapterCompleted(PreviousLevelId);

    /// <summary>Only a story run (not practice) of a campaign that completed Chapter 2 writes the chapter snapshot.</summary>
    public static bool UsesStoryCheckpoint(CampaignRunMode mode, bool practicing, PlayerProgressData data) =>
        CampaignRun.AllowsStoryCheckpoint(mode) && !practicing && StoryEntryAllowed(data);

    /// <summary>Direct Level 3 development can inspect a local copy of completed Chapter 2 possessions.</summary>
    public static bool UsesInventoryPreview(CampaignRunMode mode, bool practicing, PlayerProgressData data, string levelId) =>
        mode == CampaignRunMode.DirectEditor && !practicing && levelId == LevelId && StoryEntryAllowed(data);

    /// <summary>
    /// The candidate save holding this chapter snapshot (one more write);
    /// nothing else of the campaign changes. Null when it is not storable.
    /// </summary>
    public static PlayerProgressData WithCheckpoint(PlayerProgressData candidate, Level3ChapterCheckpoint state)
    {
        if (candidate == null || !candidate.HasCampaign || state == null || Problem(state) != null) return null;
        Level3ChapterCheckpoint previous = candidate.campaign.levelThree;
        Level3ChapterCheckpoint copy = state.Copy();
        copy.writes = (previous != null && previous.IsSet ? previous.writes : 0) + 1;
        candidate.campaign.levelThree = copy;
        return candidate;
    }

    /// <summary>
    /// Which reception line opens the chapter, from the saved Rosa history:
    /// "responsible", "unsupported-resolved", "dismissive" or "neutral".
    /// Read only; no Rosa consequence is applied again.
    /// </summary>
    public static string HistoryVariant(PlayerProgressData data)
    {
        RosaEthicsRecord ethics = data?.campaign?.rosaEthics;
        if (data == null || !data.HasCampaign || ethics == null || !ethics.committed || string.IsNullOrEmpty(ethics.choiceId)) return "neutral";
        if (ethics.choiceId.EndsWith("/responsible", StringComparison.Ordinal)) return "responsible";
        if (ethics.choiceId.EndsWith("/unsupported-promise", StringComparison.Ordinal))
            return ethics.followThroughDone ? "unsupported-resolved" : "neutral";
        if (ethics.choiceId.EndsWith("/dismissive-hold", StringComparison.Ordinal)) return "dismissive";
        return "neutral";
    }
}
