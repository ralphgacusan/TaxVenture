using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Level 3's chapter state in the running scene (T02), under
/// Level3Gameplay/Checkpoint. One chapter snapshot inside the existing signed
/// save (PlayerProgressManager.CommitLevelThreeCheckpoint), never a second
/// file or framework; Level3ChapterRules decides every transition.
///
/// ENTRY: waits for the level to start, then decides from what is saved
/// (Level3ChapterRules.Decide): no snapshot = a fresh chapter at reception;
/// a usable snapshot = resume its phase quietly; an unusable one (newer
/// version, unknown phase, other content revision) is kept untouched and
/// this attempt saves nothing.
///
/// RESUME is quiet: the receptionist stays done, Notes is available, the
/// objectives and guide catch up from the saved phase. No conversation,
/// acquisition, reward, lesson or event replays.
///
/// RECEPTION -> TOOL PRACTICE: when the receptionist's assignment ends, one
/// command (AssignBea) changes a copy; a story run writes it first and only
/// a saved copy becomes live (then ChapterAssignmentReceived is raised for
/// Notes/objectives). A failed write keeps reception as the phase and is
/// retried; nothing is announced until it is saved.
///
/// PRACTICE TAPES (T03): Level3DeskCalculator asks CommitPracticeTape; the
/// same candidate write keeps the tape and, for the second tape, advances
/// ToolPractice -> BeaRecords. Only after the save is the tape published
/// (then CalculatorPracticeAttached ticks the objective). A failed write
/// changes nothing and the player can attach again.
///
/// WORKSHEET (T05): Level3WorksheetDesk sends each draft edit (lift, place,
/// reason, link, undo) through CommitWorksheet: the edit runs on a copy, a
/// story run writes it, and only then does the desk show it. A failed write
/// leaves the board as it was (the player can repeat the edit). The first
/// time every strip is placed and both notes are answered, WorksheetDrafted
/// ticks the objective; nothing here grades the draft (T06).
///
/// CHECK AND COLLECT (T06): CommitWorksheetTape attaches a subtotal tape,
/// CheckWorksheet runs the deliberate Check (the first complete one freezes
/// the grade; an incomplete one saves nothing) and CollectWorksheet collects
/// the ready worksheet: chapter, 60 coins, +2 Public Trust and the worksheet
/// mastery award (its once-only claim and badge tier) in one save. Each publishes only after its write;
/// WorksheetReady / WorksheetCollected tick the objectives from those saved
/// transitions, and a resume restores the side-quest score quietly.
///
/// COMPUTATION (T07): CommitComputation applies one change to Bea's sheet
/// at the office desk (a chosen total or band, an attached tape) the same
/// way; TaxDueAttached ticks the objective the first time taxable income and
/// tax due both have current tapes. From her collected worksheet on the game
/// state shows Compute Taxes; nothing here grades or approves (T09).
///
/// CREDIT STEP (T08): CommitCertificate saves Bea's handover of B-WHT (the
/// acquired source, one action id) before the paper can be seen or used;
/// CommitCreditExplanation saves her question's right answer (it fills in
/// nothing). WithholdingCertificateReceived and RemainingPayableAttached tick
/// the objectives only from those saved transitions; a resume raises neither.
///
/// REVIEW (T09): the stamp is one computation command (Level3ReviewRules.Stamp).
/// CommitReview decides one hand-in to the Auditor: a returned review or a
/// justified hold is written like any chapter change; an approval is written
/// together with its 100 coins and case mastery claim
/// (PlayerProgressManager.CommitLevelThreeApproval). Only after the write is
/// it published (BeaCaseApproved ticks the objective). Bea's review clock
/// lives in her case: its active time runs on the live snapshot and is
/// written with the next save (every command, app pause/quit); its start,
/// rollover and voucher extension are saved commands.
///
/// GUARD: while enabled, the old single-case end route (CaseComplete ->
/// LevelComplete, ArchiveCase, PrepareReturn) is refused, so no legacy
/// completion can fire after a single case (Level 3 completes in T14).
///
/// MODES: only a story run of a campaign that completed Chapter 2 writes.
/// A level opened directly in the Editor, a replay or practice keeps the
/// chapter in memory only and never touches the saved snapshot.
/// </summary>
public sealed class Level3Checkpointer : MonoBehaviour
{
    public const string ChildName = "Checkpoint";
    private const float RetryEvery = 3f;
    private const string SaveFailedLine = "Couldn't save your check-in yet. Trying again...";
    private const string ResumeLine = "Welcome back. Bea's computation is right where you left it.";

    private static Scene directEditorScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPlaySession() => directEditorScene = default;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CaptureInitialScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isEditor && OfficeLevelProfile.LevelIdFor(scene) == OfficeLevelCatalog.Level3Id)
            directEditorScene = scene;
    }

    /// <summary>Office_Level3 was opened directly in the Editor (a fresh temporary attempt).</summary>
    public static bool IsDirectEditorAttempt => Application.isEditor && directEditorScene.IsValid() && directEditorScene.isLoaded;

    /// <summary>The chapter of the loaded Level 3 (null elsewhere).</summary>
    public static Level3Checkpointer Active { get; private set; }

    /// <summary>The entry decision has been made and applied.</summary>
    public bool IsReady { get; private set; }

    /// <summary>This attempt resumed a saved chapter phase.</summary>
    public bool Resumed { get; private set; }

    /// <summary>A saved snapshot this build cannot use; it is kept and this attempt saves nothing.</summary>
    public bool Blocked { get; private set; }

    /// <summary>The live (published) chapter state; a copy is changed by each command.</summary>
    public Level3ChapterCheckpoint State { get; private set; }

    public Level3Phase Phase => Level3ChapterRules.PhaseOf(State);

    /// <summary>This attempt writes the story's chapter snapshot.</summary>
    public bool SavesStory =>
        !IsDirectEditorAttempt && !Blocked && PlayerProgressManager.Instance != null && PlayerProgressManager.Instance.UsesLevelThreeStory;

    private Level3ChapterCheckpoint pendingCandidate;
    private float nextRetry;
    private bool settingsExit;
    private bool assignmentHeard;

    private void OnEnable()
    {
        Active = this;
        GameStateMachine.TransitionGuard = AllowsTransition;
        GameplayEvents.OnNotesUnlockRequested += HandleReceptionistDone;
        GuideFlags.Add(Flag);
    }

    private void OnDisable()
    {
        if (Active == this) Active = null;
        if (GameStateMachine.TransitionGuard == (System.Func<IGameState, bool>)AllowsTransition) GameStateMachine.TransitionGuard = null;
        GameplayEvents.OnNotesUnlockRequested -= HandleReceptionistDone;
        GuideFlags.Remove(Flag);
    }

    private IEnumerator Start()
    {
        float until = Time.unscaledTime + 8f;
        while (Time.unscaledTime < until && (CaseProgressionManager.Instance == null || !CaseProgressionManager.Instance.LevelStarted))
            yield return null;
        // Let the HUD, objectives and guide build first.
        yield return null;
        yield return null;
        try
        {
            Enter();
        }
        catch (System.Exception error)
        {
            // Never overwrite the saved chapter after a failed restore.
            Blocked = true;
            State = Level3ChapterRules.CreateEntry();
            Debug.LogError("[Level3] Could not resume the chapter; this attempt saves nothing. " + error.Message);
        }
        IsReady = true;
        // The receptionist finished before the chapter was ready (very early tap): apply it now.
        if (assignmentHeard) HandleReceptionistDone();
    }

    private void Enter()
    {
        PlayerProgressManager progress = PlayerProgressManager.Instance;
        bool story = !IsDirectEditorAttempt && progress != null && progress.UsesLevelThreeStory;
        Level3ChapterCheckpoint saved = story ? progress.GetLevelThreeCheckpoint() : null;
        switch (Level3ChapterRules.Decide(saved))
        {
            case Level3EntryDecision.Resume:
                State = saved;
                Resumed = true;
                RestoreQuietly();
                Debug.Log($"[Level3] Resumed the chapter at {Phase} (write {saved.writes}).");
                break;
            case Level3EntryDecision.Blocked:
                Blocked = true;
                State = Level3ChapterRules.CreateEntry();
                Debug.LogWarning($"[Level3] The saved chapter is kept untouched ({Level3ChapterRules.Problem(saved)}); this attempt starts at reception and saves nothing.");
                break;
            default:
                State = Level3ChapterRules.CreateEntry();
                Debug.Log(story
                    ? "[Level3] Fresh chapter at reception (story run)."
                    : "[Level3] Fresh chapter at reception. This attempt is not a story run of a campaign that finished Chapter 2, so nothing is saved.");
                break;
        }
    }

    /// <summary>Observers catch up from the saved phase; nothing is replayed or announced as new.</summary>
    private void RestoreQuietly()
    {
        Level3Phase phase = Phase;
        if (phase >= Level3Phase.ToolPractice)
        {
            foreach (ReceptionistInteractable receptionist in FindObjectsByType<ReceptionistInteractable>(FindObjectsInactive.Include))
                if (receptionist.gameObject.scene == gameObject.scene) receptionist.RestoreSpoken(ActiveDefinitionId);
            Transform notes = PlayerPlacement.Find(gameObject.scene, "UI/HUD_Canvas/PermanentHUD/IconButtons/NotesIconButton");
            HudIconButton notesIcon = notes != null ? notes.GetComponent<HudIconButton>() : null;
            if (notesIcon != null) notesIcon.SetAvailable(true);
        }
        if (OfficeObjectives.Instance != null)
            foreach (string trigger in Level3ChapterRules.ImpliedTriggers(State)) OfficeObjectives.Instance.MarkDone(trigger);
        // T06: a collected worksheet's frozen side-quest result is back in this run's score, without a toast or flight.
        CaseProgressionManager progression = CaseProgressionManager.Instance;
        if (Level3WorksheetCheck.Collected(State) && progression != null && progression.Score != null)
        {
            bool restoring = progression.Score.Restoring;
            progression.Score.Restoring = true;
            progression.Score.AddSideQuest(State.worksheetCheck.firstWrong.Count, null, (BadgeTier)State.worksheetCheck.firstTier);
            progression.Score.Restoring = restoring;
        }
        GameStateMachine machine = GameStateMachine.Instance;
        if (machine != null && Level3ChapterRules.CoarseState(phase) == nameof(ReceiveCaseState) && !(machine.CurrentState is ReceiveCaseState))
            machine.ChangeState(new ReceiveCaseState());
        EnterComputeTaxes();
        if (!GuideVoice.TrySay(ResumeLine, null, 5f)) LevelGate.NotifyBlocked(ResumeLine);
    }

    private static string ActiveDefinitionId =>
        CaseManager.Instance != null && CaseManager.Instance.CurrentDefinition != null ? CaseManager.Instance.CurrentDefinition.caseId : null;

    // =========================================================
    // COMMANDS
    // =========================================================

    /// <summary>The receptionist's assignment ended (GameplayEvents.OnNotesUnlockRequested).</summary>
    private void HandleReceptionistDone()
    {
        if (!IsReady)
        {
            assignmentHeard = true;
            return;
        }
        assignmentHeard = false;
        if (pendingCandidate != null || ActiveDefinitionId != Level3ChapterRules.BeaCaseId) return;
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3ChapterRules.AssignBea(candidate, out string reason);
        if (outcome != Level3CommandOutcome.Applied)
        {
            if (outcome == Level3CommandOutcome.Refused) Debug.LogWarning("[Level3] Reception assignment refused: " + reason);
            return;
        }
        TryCommit(candidate);
    }

    /// <summary>Story: write, then publish. Other attempts publish the local copy. False when the write failed (retried).</summary>
    private bool TryCommit(Level3ChapterCheckpoint candidate)
    {
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            if (pendingCandidate == null && !GuideVoice.TrySay(SaveFailedLine, null, 4f)) LevelGate.NotifyBlocked(SaveFailedLine);
            pendingCandidate = candidate;
            nextRetry = Time.unscaledTime + RetryEvery;
            Debug.LogWarning("[Level3] The chapter could not be saved; it stays at " + Phase + " and the write is retried.");
            return false;
        }
        pendingCandidate = null;
        Publish(candidate);
        return true;
    }

    private void Publish(Level3ChapterCheckpoint committed)
    {
        Level3Phase before = Phase;
        bool wasDrafted = State?.worksheet != null && State.worksheet.drafted;
        bool wasTaxDue = State?.Case(Level3ChapterRules.BeaCaseId)?.computation != null && State.Case(Level3ChapterRules.BeaCaseId).computation.taxDueNoted;
        bool hadCertificate = Level3CertificateRules.Acquired(State);
        bool wasApproved = Level3ReviewRules.CaseDone(State);
        bool wasPayable = State?.Case(Level3ChapterRules.BeaCaseId)?.computation != null && State.Case(Level3ChapterRules.BeaCaseId).computation.payableNoted;
        State = committed;
        Debug.Log($"[Level3] Chapter phase {before} -> {Phase}{(SavesStory ? " (saved)" : " (this attempt only)")}.");
        if (before == Level3Phase.Reception && Phase == Level3Phase.ToolPractice) GameplayEvents.RaiseChapterAssignmentReceived();
        if (before == Level3Phase.ToolPractice && Phase == Level3Phase.BeaRecords) GameplayEvents.RaiseCalculatorPracticeAttached();
        if (before == Level3Phase.BeaRecords && Phase == Level3Phase.WorksheetDraft) GameplayEvents.RaiseClientRecordsCollected();
        if (!wasDrafted && committed.worksheet != null && committed.worksheet.drafted) GameplayEvents.RaiseWorksheetDrafted();
        if (before < Level3Phase.WorksheetReady && Phase == Level3Phase.WorksheetReady) GameplayEvents.RaiseWorksheetReady();
        if (before == Level3Phase.WorksheetReady && Phase == Level3Phase.BeaComputation)
        {
            GameplayEvents.RaiseWorksheetCollected();
            EnterComputeTaxes();
        }
        if (!wasTaxDue && committed.Case(Level3ChapterRules.BeaCaseId)?.computation != null && committed.Case(Level3ChapterRules.BeaCaseId).computation.taxDueNoted)
            GameplayEvents.RaiseTaxDueAttached();
        if (!hadCertificate && Level3CertificateRules.Acquired(committed)) GameplayEvents.RaiseWithholdingCertificateReceived();
        if (!wasPayable && committed.Case(Level3ChapterRules.BeaCaseId)?.computation?.payableNoted == true) GameplayEvents.RaiseRemainingPayableAttached();
        if (!wasApproved && Level3ReviewRules.CaseDone(committed)) GameplayEvents.RaiseBeaCaseApproved();
    }

    // =========================================================
    // T09: REVIEW AND REVIEW CLOCK
    // =========================================================

    private static Level3ReviewContent reviewContent;

    /// <summary>Levels/Level_03/review.json (the built-in content when it is missing or broken).</summary>
    public static Level3ReviewContent ReviewContent
    {
        get
        {
            if (reviewContent != null) return reviewContent;
            TextAsset file = Resources.Load<TextAsset>(Level3ReviewRules.ResourcePath);
            if (file != null)
            {
                try { reviewContent = JsonUtility.FromJson<Level3ReviewContent>(file.text); }
                catch (System.Exception error) { Debug.LogWarning($"[Level3] Resources/{Level3ReviewRules.ResourcePath}.json is not valid JSON ({error.Message}). Using the built-in review."); }
            }
            if (reviewContent == null || reviewContent.Clock(Level3ChapterRules.BeaCaseId) == null) reviewContent = Level3ReviewRules.BuiltIn();
            return reviewContent;
        }
    }

    /// <summary>What a hand-in did (presentation follows it; it never decides).</summary>
    public enum ReviewCommit { Decided, Refused, SaveFailed }

    /// <summary>
    /// T09: the Auditor decides one hand-in of Bea's computation: candidate,
    /// then (story) the signed write (an approval with its 100 coins and case
    /// mastery in the same write), then publish. Refused/SaveFailed change
    /// nothing, record nothing and the folder can be handed in again.
    /// </summary>
    public ReviewCommit CommitReview(out Level3ReviewDecision decision, out Level3ReviewGrants grants, out string hint)
    {
        decision = null;
        grants = default;
        hint = null;
        if (!IsReady || State == null) { hint = Level3ReviewRules.RefuseNotNow; return ReviewCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3ReviewRules.Submit(candidate, ActiveDefinition, ReviewContent, out decision, out string reason);
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; decision = null; return ReviewCommit.Refused; }
        bool approved = decision.Outcome == Level3ReviewOutcome.Approved;
        if (SavesStory)
        {
            bool saved = approved ? PlayerProgressManager.Instance.CommitLevelThreeApproval(candidate, out grants)
                : PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate);
            if (!saved)
            {
                grants = default;
                decision = null;
                hint = "Couldn't save the review, so nothing was decided. Your folder is as it was: hand it in again.";
                Debug.LogWarning("[Level3] A review hand-in could not be saved; nothing was decided or paid.");
                return ReviewCommit.SaveFailed;
            }
        }
        else if (approved) grants.Xp = decision.Xp;
        Publish(candidate);
        return ReviewCommit.Decided;
    }

    /// <summary>Bea's clock settings (review.json).</summary>
    public static ReviewClockDefinition BeaClock => ReviewContent.Clock(Level3ChapterRules.BeaCaseId);

    /// <summary>
    /// T09: advances Bea's active review time on the LIVE snapshot (a clock is
    /// continuous; the next write of any kind carries it). Warnings and the
    /// cutoff come back once; a paused, stopped or approved clock never moves.
    /// </summary>
    public ReviewClock.Tick AdvanceClock(float seconds, bool paused, out float mark)
    {
        mark = 0f;
        if (!IsReady || Blocked || State == null || Level3ReviewRules.CaseDone(State)) return ReviewClock.Tick.None;
        ReviewClockState clock = State.Case(Level3ChapterRules.BeaCaseId)?.clock;
        return ReviewClock.Advance(clock, BeaClock, seconds, paused, out mark);
    }

    /// <summary>T09: a candidate that something else wrote with the save (a review voucher) becomes live.</summary>
    public void PublishCommitted(Level3ChapterCheckpoint committed)
    {
        if (committed == null || Level3ChapterRules.Problem(committed) != null) return;
        pendingCandidate = null;
        Publish(committed);
    }

    /// <summary>T07: Bea's computation shows as Compute Taxes on the game state (it never advances to filing or a legacy completion).</summary>
    private void EnterComputeTaxes()
    {
        GameStateMachine machine = GameStateMachine.Instance;
        if (machine != null && Level3ChapterRules.CoarseState(Phase) == nameof(ComputeTaxesState) && !(machine.CurrentState is ComputeTaxesState))
            machine.ChangeState(new ComputeTaxesState());
    }

    /// <summary>One computation command, run on a copy (Level3ComputationRules).</summary>
    public delegate Level3CommandOutcome ComputationEdit(Level3ChapterCheckpoint candidate, out string reason);

    /// <summary>
    /// T07: one change to Bea's computation sheet (a chosen total or band, an
    /// attached tape): candidate, then (story) the signed write, then publish.
    /// Attached = saved and live; AlreadyAttached = nothing to change;
    /// Refused/SaveFailed change nothing and the hint is a plain sentence.
    /// </summary>
    public TapeCommit CommitComputation(ComputationEdit edit, out string hint)
    {
        hint = null;
        if (!IsReady || State == null || edit == null) { hint = "Bea's computation cannot be changed right now."; return TapeCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = edit(candidate, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) { hint = reason; return TapeCommit.AlreadyAttached; }
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            hint = "Couldn't save that change. Bea's sheet is as it was: try again.";
            Debug.LogWarning("[Level3] A computation change could not be saved; nothing changed.");
            return TapeCommit.SaveFailed;
        }
        Publish(candidate);
        return TapeCommit.Attached;
    }

    /// <summary>
    /// T08: Bea hands over her withholding certificate at the credit step:
    /// candidate, then (story) the signed write, then publish. Attached = saved
    /// now (the paper may show); AlreadyAttached = handed over before (quiet);
    /// Refused/SaveFailed change nothing, so she still has it and the next talk
    /// offers it again.
    /// </summary>
    public TapeCommit CommitCertificate(out string hint)
    {
        hint = null;
        if (!IsReady || State == null) { hint = Level3CertificateRules.RefuseNotHere; return TapeCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3CertificateRules.HandOver(candidate, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) return TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            hint = "Couldn't save. Bea still has her certificate: take it again.";
            Debug.LogWarning("[Level3] Bea's withholding certificate could not be saved; nothing was handed over.");
            return TapeCommit.SaveFailed;
        }
        Publish(candidate);
        return TapeCommit.Attached;
    }

    /// <summary>
    /// T08: the right answer to Bea's certificate question, saved the same way.
    /// It never fills in the credit. A failed write keeps the question open.
    /// </summary>
    public TapeCommit CommitCreditExplanation(out string hint)
    {
        hint = null;
        if (!IsReady || State == null) { hint = Level3CertificateRules.RefuseNotHere; return TapeCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3CertificateRules.Explain(candidate, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) return TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            hint = "Couldn't save your answer. Choose it again.";
            Debug.LogWarning("[Level3] The certificate explanation could not be saved; the question stays open.");
            return TapeCommit.SaveFailed;
        }
        Publish(candidate);
        return TapeCommit.Attached;
    }

    /// <summary>One worksheet command, run on a copy (Level3WorksheetRules).</summary>
    public delegate Level3CommandOutcome WorksheetEdit(Level3ChapterCheckpoint candidate, out string reason);

    /// <summary>
    /// T05: one worksheet edit: candidate, then (story) the signed write, then
    /// publish. Attached = saved and live; AlreadyAttached = nothing to change
    /// (hint says why, e.g. "already on the worksheet"); Refused/SaveFailed
    /// change nothing and the hint is a plain sentence, never a grade.
    /// </summary>
    public TapeCommit CommitWorksheet(WorksheetEdit edit, out string hint)
    {
        hint = null;
        if (!IsReady || State == null || edit == null) { hint = "The worksheet cannot be changed right now."; return TapeCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = edit(candidate, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) { hint = reason; return TapeCommit.AlreadyAttached; }
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            hint = "Couldn't save that change. The worksheet is as it was: try again.";
            Debug.LogWarning("[Level3] A worksheet edit could not be saved; nothing changed.");
            return TapeCommit.SaveFailed;
        }
        Publish(candidate);
        return TapeCommit.Attached;
    }

    /// <summary>
    /// T06: attaches the calculator's printed tape to a worksheet subtotal
    /// (Income or Allowable expenses): candidate, then (story) the signed write,
    /// then publish. Attached = saved and live; AlreadyAttached = the same tape
    /// is already there; Refused/SaveFailed change nothing (hint says why).
    /// </summary>
    public TapeCommit CommitWorksheetTape(string section, System.Collections.Generic.IReadOnlyList<Level3CalcToken> expression, string result, out string hint)
    {
        hint = null;
        if (!IsReady || State == null) { hint = "The worksheet cannot be changed right now."; return TapeCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3WorksheetCheck.AttachSubtotal(candidate, Level3WorksheetRules.RowsOf(ActiveDefinition), section, expression, result, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) { hint = reason; return TapeCommit.AlreadyAttached; }
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            hint = "Couldn't save the tape. It is still on the calculator: tap the subtotal again to try again.";
            Debug.LogWarning("[Level3] A worksheet subtotal tape could not be saved; nothing was attached.");
            return TapeCommit.SaveFailed;
        }
        Publish(candidate);
        return TapeCommit.Attached;
    }

    /// <summary>What a deliberate Check of the worksheet did (presentation follows it; it never decides).</summary>
    public enum CheckCommit { Incomplete, Unchanged, Checked, Refused, SaveFailed }

    /// <summary>
    /// T06: the deliberate Check. Incomplete/Unchanged/Refused save nothing
    /// (evaluation lists what is missing). A complete check is written first
    /// (the frozen first grade once, the latest marks, maybe WorksheetReady),
    /// then published; a failed write grades nothing and Check can be repeated.
    /// </summary>
    public CheckCommit CheckWorksheet(out Level3WorksheetEvaluation evaluation, out string hint)
    {
        evaluation = null;
        hint = null;
        if (!IsReady || State == null) { hint = "The worksheet cannot be checked right now."; return CheckCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3WorksheetCheck.CheckOutcome outcome = Level3WorksheetCheck.Check(candidate, ActiveDefinition, out evaluation, out string reason);
        hint = reason;
        if (outcome == Level3WorksheetCheck.CheckOutcome.Incomplete) return CheckCommit.Incomplete;
        if (outcome == Level3WorksheetCheck.CheckOutcome.Unchanged) return CheckCommit.Unchanged;
        if (outcome != Level3WorksheetCheck.CheckOutcome.Checked) return CheckCommit.Refused;
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            hint = "Couldn't save the check, so nothing was graded. Your worksheet is as it was: tap CHECK to try again.";
            Debug.LogWarning("[Level3] A worksheet check could not be saved; nothing was graded.");
            return CheckCommit.SaveFailed;
        }
        Publish(candidate);
        return CheckCommit.Checked;
    }

    /// <summary>What collecting the worksheet did (presentation follows it).</summary>
    public enum CollectCommit { Collected, AlreadyCollected, Refused, SaveFailed }

    /// <summary>
    /// T06: the player collects the ready worksheet. A story run writes the
    /// collected chapter, the once-only 60 coins / +2 Public Trust and the
    /// worksheet's mastery award in ONE save (PlayerProgressManager.CommitLevelThreeWorksheet),
    /// then publishes; success effects may follow only then. Other attempts
    /// publish locally with no campaign grant. A failed write keeps the ready
    /// worksheet on the table and the player can collect again.
    /// </summary>
    public CollectCommit CollectWorksheet(out Level3WorksheetGrants grants, out string hint)
    {
        grants = default;
        hint = null;
        if (!IsReady || State == null) { hint = "The worksheet cannot be collected right now."; return CollectCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3WorksheetCheck.Acquire(candidate, ActiveDefinition, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) return CollectCommit.AlreadyCollected;
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return CollectCommit.Refused; }
        if (SavesStory)
        {
            if (!PlayerProgressManager.Instance.CommitLevelThreeWorksheet(candidate, out grants))
            {
                grants = default;
                hint = "Couldn't save. Bea's worksheet is still here: tap COLLECT WORKSHEET to try again.";
                Debug.LogWarning("[Level3] Bea's worksheet could not be saved; nothing was collected or paid.");
                return CollectCommit.SaveFailed;
            }
        }
        else
        {
            // This attempt only: the grade shows, the campaign's wallet and trust are never touched.
            grants.Xp = candidate.worksheetCheck.firstXp;
            grants.Tier = candidate.worksheetCheck.firstTier;
        }
        Publish(candidate);
        return CollectCommit.Collected;
    }

    private static CaseDefinition ActiveDefinition => CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;

    /// <summary>
    /// T04: Bea's handover (Level3BeaInteractable, when her introduction
    /// completes): candidate, then (story) the signed write, then publish.
    /// Attached = saved now; AlreadyAttached = handed over before (quiet);
    /// Refused/SaveFailed change nothing and the next talk offers it again.
    /// </summary>
    public TapeCommit CommitBeaRecords(out string hint)
    {
        hint = null;
        if (!IsReady || State == null) return TapeCommit.Refused;
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3ChapterRules.CollectBeaRecords(candidate, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) return TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            hint = "Couldn't save. Bea still has her records: talk to her again to take them.";
            Debug.LogWarning("[Level3] Bea's records could not be saved; nothing was handed over.");
            return TapeCommit.SaveFailed;
        }
        Publish(candidate);
        return TapeCommit.Attached;
    }

    /// <summary>What attaching a practice tape did (presentation follows it; it never decides).</summary>
    public enum TapeCommit { Attached, AlreadyAttached, Refused, SaveFailed }

    /// <summary>
    /// Attaches the calculator's printed tape to a practice slot: candidate,
    /// then (story) the signed write, then publish. Refused/SaveFailed change
    /// nothing; hint is a plain sentence for the card (never a grade).
    /// </summary>
    public TapeCommit CommitPracticeTape(string destinationId, System.Collections.Generic.IReadOnlyList<Level3CalcToken> expression, string result, out string hint)
    {
        hint = null;
        if (!IsReady || State == null) { hint = Level3PracticeRules.RefuseNotNow; return TapeCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3PracticeRules.AttachTape(candidate, destinationId, expression, result, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) return TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            hint = "Couldn't save. Your tape is still here: tap the slot to try again.";
            Debug.LogWarning("[Level3] The practice tape could not be saved; nothing was attached.");
            return TapeCommit.SaveFailed;
        }
        Publish(candidate);
        GameplayEvents.RaiseGuideCue(Level3DeskCalculator.CueTapeAttached);
        return TapeCommit.Attached;
    }

    private void Update()
    {
        if (pendingCandidate == null || Time.unscaledTime < nextRetry) return;
        nextRetry = Time.unscaledTime + RetryEvery;
        TryCommit(pendingCandidate);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveCurrent();
    }

    private void OnApplicationQuit() => SaveCurrent();

    /// <summary>The published phase again (an unchanged snapshot; a pending one is retried first).</summary>
    private bool SaveCurrent()
    {
        if (settingsExit || !IsReady || !SavesStory) return true;
        if (pendingCandidate != null) return TryCommit(pendingCandidate);
        if (Phase == Level3Phase.Reception && !Resumed) return true;
        return PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(State);
    }

    /// <summary>Settings board exit: save the chapter (a replay clears it instead). False keeps the player here.</summary>
    public bool PrepareSettingsExit(bool replay)
    {
        if (!SavesStory) { settingsExit = true; return true; }
        bool ok = replay ? PlayerProgressManager.Instance.ClearLevelThreeCheckpoint() : SaveCurrent();
        if (ok) settingsExit = true;
        return ok;
    }

    // =========================================================
    // GUARD AND GUIDE FLAGS
    // =========================================================

    private bool AllowsTransition(IGameState next) =>
        next == null || Level3ChapterRules.TransitionAllowed(Phase, next.GetType().Name);

    /// <summary>Level 3 facts for guide.json "flag:" conditions (read only).</summary>
    public bool Flag(string name)
    {
        if (!IsReady || State == null) return false;
        switch (name)
        {
            case "assignmentReceived": return Phase >= Level3Phase.ToolPractice;
            case "toolPractice": return Phase == Level3Phase.ToolPractice;
            case "practiceFirstTape": return State.Tape(Level3PracticeRules.FirstSlot) != null;
            case "practiceDone": return Level3PracticeRules.IsComplete(State);
            case "calculatorUp": return Level3DeskCalculator.Instance != null && Level3DeskCalculator.Instance.Lifted;
            case "beaRecordsCollected": return Phase >= Level3Phase.WorksheetDraft;
            case "atBeaTable": return Level3BeaTable.Instance != null && Level3BeaTable.Instance.Seated;
            case "worksheetUp": return Level3WorksheetDesk.Instance != null && Level3WorksheetDesk.Instance.Lifted;
            case "worksheetAllPlaced": return Level3WorksheetRules.AllPlaced(State.worksheet);
            case "worksheetDrafted": return State.worksheet != null && State.worksheet.drafted;
            case "worksheetCalcUp": return Level3WorksheetDesk.Instance != null && Level3WorksheetDesk.Instance.CalculatorUp;
            case "worksheetTapes":
                return Level3WorksheetCheck.IsCurrent(State, Level3WorksheetRules.Income) && Level3WorksheetCheck.IsCurrent(State, Level3WorksheetRules.Expenses);
            case "worksheetChecked": return State.worksheetCheck != null && State.worksheetCheck.firstChecked;
            case "worksheetCorrecting": return Phase == Level3Phase.WorksheetCorrections;
            case "worksheetReady": return Phase == Level3Phase.WorksheetReady;
            case "worksheetCollected": return Level3WorksheetCheck.Collected(State);
            case "beaComputation": return Phase == Level3Phase.BeaComputation;
            case "computationTotals":
            {
                Level3ComputationRecord sheet = Level3ComputationRules.Record(State, Level3ChapterRules.BeaCaseId);
                return sheet != null && sheet.grossChoice.Length > 0 && sheet.expensesChoice.Length > 0;
            }
            case "computationTaxable": return Level3ComputationRules.TapeOf(State, Level3ChapterRules.BeaCaseId, Level3ComputationRules.Taxable) != null;
            case "computationRule": return Level3ComputationRules.Record(State, Level3ChapterRules.BeaCaseId)?.ruleId.Length > 0;
            case "computationTaxDue": return Level3ComputationRules.Record(State, Level3ChapterRules.BeaCaseId)?.taxDueNoted == true;
            // T08: the credit step.
            case "beaInOffice": return Level3BeaOfficeVisit.Instance != null && Level3BeaOfficeVisit.Instance.InRoom;
            case "certificateReceived": return Level3CertificateRules.Acquired(State);
            case "creditExplained": return Level3CertificateRules.Explained(State);
            case "computationCredit": return Level3ComputationRules.Record(State, Level3ChapterRules.BeaCaseId)?.creditChoice.Length > 0;
            case "computationPayable": return Level3ComputationRules.Record(State, Level3ChapterRules.BeaCaseId)?.payableNoted == true;
            case "certificateUp": return Level3CertificatePaper.Instance != null && Level3CertificatePaper.Instance.IsUp;
            // T09: the stamp, the hand-in and the clock.
            case "beaStamped": return Level3ReviewRules.StampCurrent(State, Level3ChapterRules.BeaCaseId);
            case "beaStampStale": return Level3ReviewRules.StampStale(State, Level3ChapterRules.BeaCaseId);
            case "beaReviewReturned":
            {
                Level3ReviewRecord review = State.Case(Level3ChapterRules.BeaCaseId)?.review;
                return review != null && !review.approved && review.lastOutcome.Length > 0 && review.lastOutcome != "hold";
            }
            case "beaHeld": return State.Case(Level3ChapterRules.BeaCaseId)?.review?.lastOutcome == "hold";
            case "beaApproved": return Level3ReviewRules.CaseDone(State);
            case "reviewClockOn": return State.Case(Level3ChapterRules.BeaCaseId)?.clock?.started == true && !Level3ReviewRules.CaseDone(State);
            default: return false;
        }
    }
}
