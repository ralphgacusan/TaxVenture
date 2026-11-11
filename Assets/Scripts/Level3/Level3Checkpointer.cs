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
/// CONTINUE (revision, 3 November 2026): a pre-revision (version 1) snapshot
/// is continued once on a copy (Level3ChapterRules.TryContinueLegacy), the
/// copy is written, and only then restored quietly like a resume. A failed
/// write keeps the saved snapshot exactly as it was: the chapter waits (not
/// ready, nothing published) and the write is retried; leaving writes nothing.
///
/// RECEPTION -> BEA'S RECORDS: when the receptionist's assignment ends, one
/// command (AssignBea) changes a copy; a story run writes it first and only
/// a saved copy becomes live (then ChapterAssignmentReceived is raised for
/// Notes/objectives). A failed write keeps reception as the phase and is
/// retried; nothing is announced until it is saved. The old budget practice
/// is no longer part of the chapter.
///
/// PRACTICE TAPES (T03, legacy): Level3DeskCalculator's CommitPracticeTape
/// still guards the retired ToolPractice phase, which no chapter reaches now;
/// tapes attached before the revision stay on the card as references.
///
/// FIRST-USE LESSON (revised T03): CommitCalculatorLesson saves the one
/// required action of the calculator's example (Level3PracticeRules.
/// CompleteLesson: calculatorTaught and its action id only) the same way:
/// candidate, write, publish. A failed write changes nothing; the example
/// stays and can be attached again. Nothing is scored or paid.
///
/// EDITOR FIXTURE: a direct Editor attempt (which never saves) may start from
/// EditorFixture, a disposable chapter an Editor menu builds with the real
/// rule commands (Assets/Editor/Level3EditorFixture.cs). Story, replay and
/// practice runs never read it.
///
/// WORKSHEET (T05): each draft edit runs through CommitWorksheet: the edit
/// runs on a copy, a story run writes it, and only then is it shown. The
/// revised paper machine (Level3BeaTable, 4 November 2026) sends exactly one
/// edit per lever pull, Level3WorksheetRules.File: a row's treatment and its
/// R6/R7 reason together, so half a classification is never saved or shown.
/// A failed write leaves the draft as it was and the record half-fed with its
/// choices (pull again). Loading a record or turning the selector is not an
/// edit. The first time every row is filed with both reasons, WorksheetDrafted
/// ticks the objective; nothing here grades the draft (T06). The old board's
/// edits (lift, place, reason, link, undo) remain for legacy compatibility.
///
/// CHECK AND COLLECT (T06): CommitWorksheetTape attaches a subtotal tape,
/// CheckWorksheet runs the deliberate Check (the first complete one freezes
/// the grade; an incomplete one saves nothing) and CollectWorksheet collects
/// the ready worksheet: chapter, 60 coins, +2 Public Trust and the worksheet
/// mastery award (its once-only claim and badge tier) in one save. Each publishes only after its write;
/// WorksheetReady / WorksheetCollected tick the objectives from those saved
/// transitions, and a resume restores the side-quest score quietly.
/// Revised T06 (5 November 2026): the paper machine checks one batch at a time
/// through CheckBatch (Level3WorksheetCheck.CheckBatch: first outcomes frozen
/// once per batch, all-right batches accepted, the last one -> WorksheetReady);
/// the tape commands and CheckWorksheet remain for the retired board only.
/// CollectWorksheet collects Bea's Income & Expense Summary (B-WORKSHEET).
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
/// CLOSING (T14): after Bea's explained delivery, CommitFinding saves the
/// supported statement about her file (Level3ClosingRules.RecordFinding; the
/// existing FindingRecorded event ticks Notes), the receptionist passes on
/// Quax's message (this owner is her opt-in ReceptionistInteractable.
/// ILevelMessage; CommitMessage: ClosingFinding -> WrapUpPending), and the
/// pending chapter result shows on the existing LevelCompleteUI card once no
/// dialogue is running (again after a reload). CommitAcknowledgment saves its
/// Continue: CompletedOffice with the chapter's completion and aggregated
/// mastery in one write (PlayerProgressManager.CommitLevelThreeWrapUp). Each
/// is candidate -> signed write -> publish; a failed write keeps the previous
/// step actionable. The completed office restarts no clock, copy or reward.
///
/// GUARD: while enabled, the old single-case end route (CaseComplete ->
/// LevelComplete, ArchiveCase, PrepareReturn) is refused, so no legacy
/// completion can fire after a single case (Level 3 completes in T14).
///
/// PRACTICE AND REPLAY (T16): a Tutorial Hub drill for Level 3 starts from
/// its TrainingDrill.fixture (Level3EditorFixtures, real commands, memory
/// only). A mastery replay plays the whole chapter locally; its acknowledged
/// result writes only the best-only mastery record
/// (PlayerProgressManager.CommitLevelThreeReplay), never the story snapshot,
/// wallet, Trust, claims or possessions.
///
/// MODES: only a story run of a campaign that completed Chapter 2 writes.
/// A level opened directly in the Editor, a replay or practice keeps the
/// chapter in memory only and never touches the saved snapshot.
/// </summary>
public sealed class Level3Checkpointer : MonoBehaviour, ReceptionistInteractable.ILevelMessage
{
    public const string ChildName = "Checkpoint";
    private const float RetryEvery = 3f;
    private const string SaveFailedLine = "Couldn't save your check-in yet. Trying again...";
    private const string ContinueFailedLine = "Couldn't open your saved chapter yet. Your work is safe. Trying again...";
    private const string ResumeLine = "Welcome back. Bea's computation is right where you left it.";
    /// <summary>T14: the resume line once her copy is delivered, and once the chapter is complete.</summary>
    private const string ClosingResumeLine = "Welcome back. Bea's file is nearly closed.";
    private const string CompletedResumeLine = "Welcome back. Chapter 3 is complete; the office is yours.";
    /// <summary>T14: after Stay in office (Arthur, once): what the completed office offers.</summary>
    private const string CompletedOfficeLine = "Chapter 3 is complete. Look around or visit the supply counter. Settings takes you back to Career.";

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

    /// <summary>
    /// Editor only: builds a disposable chapter for a direct Editor attempt (null:
    /// a normal fresh start). Set by Assets/Editor/Level3EditorFixture.cs; never
    /// read by a story, replay or practice run, and such an attempt never saves.
    /// </summary>
    public static System.Func<CaseDefinition, Level3ChapterCheckpoint> EditorFixture;

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
    /// <summary>A continued version 1 chapter whose first write has not succeeded yet (never live).</summary>
    private Level3ChapterCheckpoint pendingContinuation;
    private float nextRetry;
    private bool settingsExit;
    private bool assignmentHeard;
    /// <summary>T14: the pending chapter result is on screen this visit (shown once; again after a reload).</summary>
    private bool resultShown;

    private void OnEnable()
    {
        Active = this;
        GameStateMachine.TransitionGuard = AllowsTransition;
        GameplayEvents.OnNotesUnlockRequested += HandleReceptionistDone;
        GuideFlags.Add(Flag);
        ReceptionistInteractable.LevelMessage = this;
    }

    private void OnDisable()
    {
        if (ReferenceEquals(ReceptionistInteractable.LevelMessage, this)) ReceptionistInteractable.LevelMessage = null;
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
            pendingContinuation = null;
            Blocked = true;
            State = Level3ChapterRules.CreateEntry();
            Debug.LogError("[Level3] Could not resume the chapter; this attempt saves nothing. " + error.Message);
        }
        // A continued chapter becomes live only once it is saved; until then nothing is ready or published.
        bool told = false;
        while (pendingContinuation != null)
        {
            if (!told && !GuideVoice.TrySay(ContinueFailedLine, null, 5f)) LevelGate.NotifyBlocked(ContinueFailedLine);
            told = true;
            yield return new WaitForSecondsRealtime(RetryEvery);
            if (settingsExit) yield break;
            TryContinue(pendingContinuation);
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
        if (IsDirectEditorAttempt && EditorFixture != null)
        {
            Level3ChapterCheckpoint fixture = EditorFixture(ActiveDefinition);
            if (fixture != null && Level3ChapterRules.Problem(fixture) == null)
            {
                State = fixture;
                Resumed = true;
                RestoreQuietly();
                Debug.Log($"[Level3] Editor fixture: this direct attempt starts at {Phase} (nothing is saved).");
                return;
            }
            Debug.LogWarning("[Level3] The Editor fixture could not be built for this case; a normal fresh attempt starts.");
        }
        if (!story && TryPracticeStart()) return;
        switch (Level3ChapterRules.Decide(saved))
        {
            case Level3EntryDecision.Resume:
                State = saved;
                Resumed = true;
                RestoreQuietly();
                Debug.Log($"[Level3] Resumed the chapter at {Phase} (write {saved.writes}).");
                break;
            case Level3EntryDecision.Continue:
                Level3ChapterRules.TryContinueLegacy(saved, out Level3ChapterCheckpoint continued, out _);
                TryContinue(continued);
                break;
            case Level3EntryDecision.Blocked:
                Blocked = true;
                State = Level3ChapterRules.CreateEntry();
                string why = Level3ChapterRules.Problem(saved);
                if (saved.version == Level3ChapterCheckpoint.LegacyVersion) Level3ChapterRules.TryContinueLegacy(saved, out _, out why);
                Debug.LogWarning($"[Level3] The saved chapter is kept untouched ({why}); this attempt starts at reception and saves nothing.");
                break;
            default:
                State = Level3ChapterRules.CreateEntry();
                Debug.Log(story
                    ? "[Level3] Fresh chapter at reception (story run)."
                    : "[Level3] Fresh chapter at reception. This attempt is not a story run of a campaign that finished Chapter 2, so nothing is saved.");
                break;
        }
    }

    /// <summary>
    /// T16: a Level 3 Tutorial Hub drill starts at its practice chapter (built with the real commands,
    /// held in memory; this run never saves). False: no drill for this level, or no usable start.
    /// </summary>
    private bool TryPracticeStart()
    {
        TrainingDrill drill = TrainingSession.IsPracticing ? TrainingSession.Drill : null;
        if (drill == null || drill.levelId != Level3ChapterRules.LevelId || string.IsNullOrWhiteSpace(drill.fixture)) return false;
        Level3ChapterCheckpoint start = Level3EditorFixtures.Build(drill.fixture.Trim(), ActiveDefinition);
        if (start == null || Level3ChapterRules.Problem(start) != null)
        {
            Debug.LogWarning($"[Level3] Practice start \"{drill.fixture}\" could not be built; the drill starts at reception (nothing is saved).");
            return false;
        }
        State = start;
        Resumed = true;
        RestoreQuietly(welcomeBack: false);
        Debug.Log($"[Level3] Practice \"{drill.id}\" starts at {Phase} (practice: nothing is saved).");
        return true;
    }

    /// <summary>A continued version 1 chapter: written first, then restored quietly (no event, reward or lesson). False keeps it pending.</summary>
    private bool TryContinue(Level3ChapterCheckpoint continued)
    {
        if (!PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(continued))
        {
            if (pendingContinuation == null) Debug.LogWarning("[Level3] The saved chapter could not be continued into the revision yet; it is kept exactly as it was and the write is retried.");
            pendingContinuation = continued;
            return false;
        }
        pendingContinuation = null;
        State = continued;
        Resumed = true;
        RestoreQuietly();
        Debug.Log($"[Level3] Continued the saved chapter into revision {Level3ChapterCheckpoint.CurrentVersion} at {Phase} (write {continued.writes + 1}).");
        return true;
    }

    /// <summary>Observers catch up from the saved phase; nothing is replayed or announced as new.</summary>
    private void RestoreQuietly(bool welcomeBack = true)
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
        // T16: a practice start is not a "welcome back" (the drill's own guide speaks).
        if (!welcomeBack) return;
        string line = phase == Level3Phase.CompletedOffice ? CompletedResumeLine : phase >= Level3Phase.ClosingFinding ? ClosingResumeLine : ResumeLine;
        if (!GuideVoice.TrySay(line, null, 5f)) LevelGate.NotifyBlocked(line);
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
        bool wasDelivered = Level3DeliveryRules.Delivered(State);
        bool wasPayable = State?.Case(Level3ChapterRules.BeaCaseId)?.computation != null && State.Case(Level3ChapterRules.BeaCaseId).computation.payableNoted;
        bool hadFinding = Level3ClosingRules.FindingRecorded(State);
        State = committed;
        Debug.Log($"[Level3] Chapter phase {before} -> {Phase}{(SavesStory ? " (saved)" : " (this attempt only)")}.");
        if (before == Level3Phase.Reception && Phase > Level3Phase.Reception) GameplayEvents.RaiseChapterAssignmentReceived();
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
        if (!wasDelivered && Level3DeliveryRules.Delivered(committed)) GameplayEvents.RaiseClientCopyHandedOver();
        // T14: the saved finding (the existing Notes finding event), message and acknowledged result.
        if (!hadFinding && Level3ClosingRules.FindingRecorded(committed)) GameplayEvents.RaiseFindingRecorded();
        if (before == Level3Phase.ClosingFinding && Phase == Level3Phase.WrapUpPending) GameplayEvents.RaiseChapterMessageRead();
        if (before == Level3Phase.WrapUpPending && Phase == Level3Phase.CompletedOffice) GameplayEvents.RaiseChapterWrapUpConfirmed();
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
        CaseData actualFolder = CaseManager.Instance?.CurrentCase;
        if (actualFolder == null) { hint = "Bea's folder is not ready to review yet."; return ReviewCommit.Refused; }
        Level3ReviewRules.CaptureFolder(candidate, actualFolder);
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
        else
        {
            if (HudHotbar.Instance != null)
            {
                HudHotbar bar = HudHotbar.Instance;
                InventoryResult folder = bar.Commit(inv => Level3ReviewRules.SyncFolderInventory(inv, bar.Catalogue, candidate));
                if (!folder.Ok) { hint = folder.Message; decision = null; return ReviewCommit.SaveFailed; }
            }
            if (approved) grants.Xp = decision.Xp;
        }
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
        if (!SavesStory && HudHotbar.Instance != null)
        {
            HudHotbar bar = HudHotbar.Instance;
            InventoryResult papers = bar.Commit(inv => Level3ReviewRules.SyncFolderInventory(inv, bar.Catalogue, candidate));
            if (!papers.Ok) { hint = papers.Message; return TapeCommit.SaveFailed; }
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
        // T13: a full bar and Bag is not a save failure: say so; the paper stays beside Bea, nothing is written.
        if (HudHotbar.Instance != null && !Level3CertificateRules.HasRoom(HudHotbar.Instance.Inventory)) { hint = Level3CertificateRules.RefuseFull; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCertificate(candidate))
        {
            hint = "Couldn't save. Bea still has her certificate: take it again.";
            Debug.LogWarning("[Level3] Bea's withholding certificate could not be saved; nothing was handed over.");
            return TapeCommit.SaveFailed;
        }
        if (!SavesStory)
        {
            HudHotbar bar = HudHotbar.Instance;
            InventoryResult item = bar != null ? bar.Commit(inv => Level3CertificateRules.AcquireItem(inv, bar.Catalogue, candidate, ActiveDefinition)) : InventoryResult.Reject("Inventory is not ready.");
            if (!item.Ok) { hint = "Make room in your Bag, then take the certificate again."; return TapeCommit.SaveFailed; }
        }
        Publish(candidate);
        return TapeCommit.Attached;
    }

    /// <summary>
    /// Milestone T01: a full bar and Bag at Bea's certificate. The player deliberately clips it into her
    /// carried Case Folder instead (Level3CertificateRules.ClipToFolder): the handover and the folder
    /// attachment are ONE candidate, then (story) the signed write, then publish. Attached = clipped now;
    /// AlreadyAttached = handed over before (quiet); Refused/SaveFailed change nothing, so the paper stays
    /// beside Bea to clip or take again. No slot is used, nothing is evicted and no credit is entered.
    /// </summary>
    public TapeCommit CommitCertificateToFolder(bool folderCarried, out string hint)
    {
        hint = null;
        if (!IsReady || State == null) { hint = Level3CertificateRules.RefuseNotHere; return TapeCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3CertificateRules.ClipToFolder(candidate, folderCarried, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) return TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCertificate(candidate))
        {
            hint = "Couldn't save. The certificate is still beside Bea: clip it again.";
            Debug.LogWarning("[Level3] Clipping Bea's certificate to her folder could not be saved; nothing was handed over.");
            return TapeCommit.SaveFailed;
        }
        if (!SavesStory && HudHotbar.Instance != null)
        {
            HudHotbar bar = HudHotbar.Instance;
            InventoryResult pages = bar.Commit(inv => Level3CertificateRules.ApplyToInventory(inv, bar.Catalogue, candidate, ActiveDefinition, requireFolder: false));
            if (!pages.Ok) { hint = "Couldn't clip it just now. The certificate is still beside Bea: try again."; return TapeCommit.SaveFailed; }
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

    /// <summary>
    /// T10: the player pointed to the figure that answers Bea's balance
    /// question (Level3DeliveryRules.Explain): candidate, then (story) the
    /// signed write, then publish. A wrong figure saves an idempotent attempt;
    /// a failed write keeps the question open with the copy carried.
    /// </summary>
    public TapeCommit CommitBalanceExplanation(string evidenceId, out string hint, int attempt = 0)
    {
        hint = null;
        if (!IsReady || State == null) { hint = Level3DeliveryRules.RefuseNotNow; return TapeCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        string reason;
        Level3CommandOutcome outcome = Level3DeliveryRules.Answers(evidenceId)
            ? Level3DeliveryRules.Explain(candidate, evidenceId, out reason)
            : Level3DeliveryRules.RecordWrongAnswer(candidate, evidenceId, attempt, out reason);
        if (outcome == Level3CommandOutcome.Duplicate) return TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            hint = "Couldn't save. Tap the figure again.";
            Debug.LogWarning("[Level3] Bea's balance explanation could not be saved; the question stays open.");
            return TapeCommit.SaveFailed;
        }
        Publish(candidate);
        return TapeCommit.Attached;
    }

    /// <summary>
    /// T10: the copy reached Bea after the saved explanation: candidate (copy
    /// done, ClosingFinding), then (story) the signed write holding the
    /// once-only earned +1..4 Public Trust follow-through, then publish. Other attempts
    /// publish locally with no campaign grant. Refused/SaveFailed change
    /// nothing: the copy stays carried and can be handed over again.
    /// </summary>
    public TapeCommit CommitDelivery(out Level3DeliveryGrants grants, out string hint)
    {
        grants = default;
        hint = null;
        if (!IsReady || State == null) { hint = Level3DeliveryRules.RefuseNotNow; return TapeCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3DeliveryRules.Deliver(candidate, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) return TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeDelivery(candidate, out grants))
        {
            grants = default;
            hint = "It didn't save. Give Bea her copy again.";
            Debug.LogWarning("[Level3] Bea's copy delivery could not be saved; the copy stays in the folder icon.");
            return TapeCommit.SaveFailed;
        }
        if (!SavesStory && HudHotbar.Instance != null)
        {
            HudHotbar bar = HudHotbar.Instance;
            InventoryResult copy = bar.Commit(inv => Level3DeliveryRules.ReleaseCopyInventory(inv, bar.Catalogue, candidate));
            if (!copy.Ok) { hint = copy.Message; return TapeCommit.SaveFailed; }
        }
        Publish(candidate);
        return TapeCommit.Attached;
    }

    // =========================================================
    // T14: THE SUPPORTED FINDING, QUAX'S MESSAGE, THE CHAPTER RESULT
    // =========================================================

    /// <summary>
    /// T14: the player stated what Bea's file shows (Level3ClosingRules.
    /// RecordFinding): candidate, then (story) the signed write, then publish.
    /// An unsupported statement is Refused (nothing saved, no penalty); a
    /// failed write keeps her question open.
    /// </summary>
    public TapeCommit CommitFinding(string statementId, out string hint)
    {
        hint = null;
        if (!IsReady || State == null) { hint = Level3ClosingRules.RefuseNotNow; return TapeCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3ClosingRules.RecordFinding(candidate, statementId, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) return TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            hint = "Couldn't save. Choose it again.";
            Debug.LogWarning("[Level3] Bea's finding could not be saved; her question stays open.");
            return TapeCommit.SaveFailed;
        }
        Publish(candidate);
        return TapeCommit.Attached;
    }

    /// <summary>
    /// T14: the receptionist finished Quax's message after the saved finding:
    /// ClosingFinding -> WrapUpPending, saved first; then the result card
    /// follows (TickWrapUp). A failed write keeps the message waiting at reception.
    /// </summary>
    public TapeCommit CommitMessage(out string hint)
    {
        hint = null;
        if (!IsReady || State == null) { hint = Level3ClosingRules.RefuseNotNow; return TapeCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3ClosingRules.ReadMessage(candidate, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) return TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            hint = "Couldn't save. Talk to the receptionist again for the message.";
            Debug.LogWarning("[Level3] Quax's message could not be saved; it waits at reception.");
            return TapeCommit.SaveFailed;
        }
        Publish(candidate);
        return TapeCommit.Attached;
    }

    /// <summary>
    /// T14: Continue on the pending chapter result: candidate (CompletedOffice),
    /// then (story) ONE signed write with the chapter's completion and its
    /// aggregated mastery record (no coins or Trust), then publish. Other
    /// attempts publish locally and write nothing. Refused/SaveFailed change
    /// nothing: the result stays pending and Continue can be pressed again.
    /// </summary>
    public TapeCommit CommitAcknowledgment(out Level3WrapUpGrants grants, out string hint)
    {
        grants = default;
        hint = null;
        if (!IsReady || State == null) { hint = Level3ClosingRules.RefuseNotNow; return TapeCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3ClosingRules.Acknowledge(candidate, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) return TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeWrapUp(candidate, out grants))
        {
            grants = default;
            hint = "Your chapter result could not be saved. Your progress is safe. Try again.";
            Debug.LogWarning("[Level3] The chapter result could not be saved; it stays pending.");
            return TapeCommit.SaveFailed;
        }
        if (!SavesStory)
        {
            grants.Result = Level3ClosingRules.Result(candidate);
            // T16: a mastery replay keeps only its best mastery record (nothing else of the campaign).
            PlayerProgressManager manager = PlayerProgressManager.Instance;
            Level3WrapUpGrants best = default;
            if (manager != null && manager.CommitLevelThreeReplay(candidate, out best) == PlayerProgressManager.ReplayCommit.SaveFailed)
            {
                grants = default;
                hint = "Your best result could not be saved. Try again.";
                Debug.LogWarning("[Level3] The replay's best result could not be saved; the result stays pending.");
                return TapeCommit.SaveFailed;
            }
            else if (best.Changed) grants = best;
        }
        Publish(candidate);
        return TapeCommit.Attached;
    }

    /// <summary>T14: Stay in office after the saved acknowledgment: one short line (no clock, copy, case or reward restarts).</summary>
    public void EnterCompletedOffice()
    {
        if (!Level3ClosingRules.Completed(State)) return;
        if (!GuideVoice.TrySay(CompletedOfficeLine, null, 6f)) LevelGate.NotifyBlocked(CompletedOfficeLine);
    }

    // ReceptionistInteractable.ILevelMessage: Quax's message, passed on by the receptionist.

    public bool MessagePending => IsReady && !Blocked && pendingCandidate == null && Level3ClosingRules.StageOf(State) == Level3ClosingRules.Stage.Message;

    public System.Collections.Generic.List<DialogueLine> MessageLines() => Level3TownContent.MessageDialogue(Reception);

    public void MessageHeard()
    {
        TapeCommit outcome = CommitMessage(out string hint);
        if ((outcome == TapeCommit.SaveFailed || outcome == TapeCommit.Refused) && !string.IsNullOrEmpty(hint) && !GuideVoice.TrySay(hint, null, 4f))
            LevelGate.NotifyBlocked(hint);
    }

    private static Level3ReceptionContent reception;

    /// <summary>town_life.json's reception block (built-in lines when it is missing or broken).</summary>
    private static Level3ReceptionContent Reception
    {
        get
        {
            if (reception != null) return reception;
            TextAsset file = Resources.Load<TextAsset>(Level3TownContent.ResourcePath);
            if (file != null)
            {
                try
                {
                    Level3TownFile parsed = JsonUtility.FromJson<Level3TownFile>(file.text);
                    if (parsed?.reception != null && Level3TownContent.ValidateReception(parsed.reception).Count == 0) reception = parsed.reception;
                }
                catch (System.Exception error) { Debug.LogWarning($"[Level3] Resources/{Level3TownContent.ResourcePath}.json is not valid JSON ({error.Message}). Using the built-in message."); }
            }
            return reception ??= new Level3ReceptionContent();
        }
    }

    /// <summary>T14: the pending result card appears once nothing else holds the screen (a fresh message or a reload).</summary>
    private void TickWrapUp()
    {
        if (resultShown || !IsReady || Level3ClosingRules.StageOf(State) != Level3ClosingRules.Stage.Result) return;
        if (DialogueUI.IsAnyDialogueActive || HudRewardScreen.AnyOpen || LevelCompleteUI.Instance == null) return;
        resultShown = LevelCompleteUI.Instance.ShowLevelThreeChapter();
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

    /// <summary>
    /// Revised T06: the paper machine's deliberate Check batch (the open batch, made on draft revision
    /// basedOn). Incomplete/Unchanged/Refused save nothing (evaluation says what is missing). A complete
    /// check is written first (the batch's first outcomes frozen once, its marks, an accepted batch, maybe
    /// WorksheetReady), then published. A failed write grades nothing, accepts nothing and returns no
    /// evaluation, so no mark can be shown; Check batch can be repeated.
    /// </summary>
    public CheckCommit CheckBatch(string batch, int basedOn, out Level3BatchEvaluation evaluation, out string hint)
    {
        evaluation = null;
        hint = null;
        if (!IsReady || State == null) { hint = "The batch cannot be checked right now."; return CheckCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3WorksheetCheck.CheckOutcome outcome = Level3WorksheetCheck.CheckBatch(candidate, ActiveDefinition, batch, basedOn, out evaluation, out string reason);
        hint = reason;
        if (outcome == Level3WorksheetCheck.CheckOutcome.Incomplete) return CheckCommit.Incomplete;
        if (outcome == Level3WorksheetCheck.CheckOutcome.Unchanged) return CheckCommit.Unchanged;
        if (outcome != Level3WorksheetCheck.CheckOutcome.Checked) { evaluation = null; return CheckCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            evaluation = null;
            hint = "Couldn't save the check, so nothing was graded. Your records are as they were: tap CHECK BATCH again.";
            Debug.LogWarning("[Level3] A batch check could not be saved; nothing was graded or accepted.");
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
    public CollectCommit CollectWorksheet(out Level3WorksheetGrants grants, out string hint, SlotRef? target = null)
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
            if (!PlayerProgressManager.Instance.CommitLevelThreeWorksheet(candidate, out grants, target))
            {
                grants = default;
                hint = "Couldn't store the summary. Make room and drag it to your Bag or a slot to try again.";
                Debug.LogWarning("[Level3] Bea's worksheet could not be saved; nothing was collected or paid.");
                return CollectCommit.SaveFailed;
            }
        }
        else
        {
            // This attempt only: the grade shows, the campaign's wallet and trust are never touched.
            HudHotbar bar = HudHotbar.Instance;
            InventoryResult item = bar != null ? bar.Commit(r => Level3WorksheetCheck.AcquireSummary(r, bar.Catalogue, candidate, target, ActiveDefinition)) : InventoryResult.Reject("Inventory is not ready.");
            if (!item.Ok) { hint = item.Message; return CollectCommit.Refused; }
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

    /// <summary>
    /// Revised T03: the calculator's first-use example is attached (its one
    /// required action): candidate, then (story) the signed write, then
    /// publish. Attached = taught now; AlreadyAttached = taught before (quiet);
    /// Refused/SaveFailed change nothing and the example stays to try again.
    /// </summary>
    public TapeCommit CommitCalculatorLesson(System.Collections.Generic.IReadOnlyList<Level3CalcToken> expression, string result, out string hint) =>
        CommitCalculatorLesson(Level3PracticeRules.LessonCard, expression, result, out hint);

    /// <summary>
    /// Teaching pass (6 November 2026): any of the three desk demonstrations (taxable income, tax table,
    /// withholding) is attached: candidate (its own marker only), then (story) the signed write, then
    /// publish. Practice/replay/Editor runs keep it in memory (never saved). Same outcomes as above.
    /// </summary>
    public TapeCommit CommitCalculatorLesson(Level3CardSpec lesson, System.Collections.Generic.IReadOnlyList<Level3CalcToken> expression, string result, out string hint)
    {
        hint = null;
        if (!IsReady || State == null) { hint = Level3PracticeRules.RefuseNotNow; return TapeCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3PracticeRules.CompleteLesson(candidate, lesson, expression, result, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) return TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            hint = "Couldn't save. The example tape is still on the calculator: tap the slot again.";
            Debug.LogWarning("[Level3] The calculator lesson could not be saved; nothing changed.");
            return TapeCommit.SaveFailed;
        }
        Publish(candidate);
        GameplayEvents.RaiseGuideCue(Level3DeskCalculator.CueTapeAttached);
        return TapeCommit.Attached;
    }

    /// <summary>
    /// Teaching pass: Worth the Tax?'s practice packet is done (all four practice slips right this visit):
    /// candidate (its milestone only), then (story) the signed write, then publish. Attached = saved now;
    /// AlreadyAttached = done before (quiet); Refused/SaveFailed change nothing (the last slip stays loaded
    /// for another pull). Nothing of Bea's worksheet, grade, reward, Trust, inventory or clock changes.
    /// </summary>
    public TapeCommit CommitSortingPractice(out string hint)
    {
        hint = null;
        if (!IsReady || State == null) { hint = Level3SortingPractice.RefuseNotNow; return TapeCommit.Refused; }
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = Level3SortingPractice.Complete(candidate, out string reason);
        if (outcome == Level3CommandOutcome.Duplicate) return TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) { hint = reason; return TapeCommit.Refused; }
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            hint = "Couldn't save. Pull the lever again.";
            Debug.LogWarning("[Level3] The practice packet could not be saved; nothing changed.");
            return TapeCommit.SaveFailed;
        }
        Publish(candidate);
        GameplayEvents.RaiseGuideCue(SortingPracticeDoneCue);
        return TapeCommit.Attached;
    }

    /// <summary>
    /// Follow-up (Arthur first): Arthur's demonstration was watched to the end, so it is not shown again.
    /// lesson null = the practice packet's demonstration, else that desk example's. Candidate (its milestone
    /// only), then (story) the signed write, then publish. A failed write changes nothing: the demonstration
    /// simply plays again next time (it is presentation; nothing else waits on this save).
    /// </summary>
    public TapeCommit CommitDemoSeen(Level3CardSpec lesson)
    {
        if (!IsReady || State == null) return TapeCommit.Refused;
        Level3ChapterCheckpoint candidate = State.Copy();
        Level3CommandOutcome outcome = lesson == null
            ? Level3SortingPractice.MarkDemoSeen(candidate, out _)
            : Level3PracticeRules.MarkDemoSeen(candidate, lesson, out _);
        if (outcome == Level3CommandOutcome.Duplicate) return TapeCommit.AlreadyAttached;
        if (outcome != Level3CommandOutcome.Applied) return TapeCommit.Refused;
        if (SavesStory && !PlayerProgressManager.Instance.CommitLevelThreeCheckpoint(candidate))
        {
            Debug.LogWarning("[Level3] Arthur's demonstration could not be marked as seen; it will play again next time.");
            return TapeCommit.SaveFailed;
        }
        Publish(candidate);
        return TapeCommit.Attached;
    }

    /// <summary>Guide cue: the practice packet's completion was saved (the guide only observes).</summary>
    public const string SortingPracticeDoneCue = "PracticePacketDone";

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
        TickWrapUp();
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
            // Revised T03: the first-use example is up at the desk; the marker once its tape is attached (saved).
            // Follow-up: the example's "your turn" hints wait until Arthur's demonstration of it has finished.
            case "calculatorLesson": return Level3PracticeRules.CalculatorLessonDue(State) && Level3DeskCalculator.Instance != null && Level3DeskCalculator.Instance.Lifted && !Level3DeskCalculator.Instance.DemoRunning;
            case "calculatorTaught": return Level3PracticeRules.Taught(State);
            // Teaching pass (6 November 2026): the tax-table and withholding examples at the desk (saved markers),
            // and Worth the Tax?'s practice packet at Bea's table (seated, slips on the stand; saved marker).
            case "taxLesson": return Level3PracticeRules.TaxLessonDue(State) && Level3DeskCalculator.Instance != null && Level3DeskCalculator.Instance.Lifted && !Level3DeskCalculator.Instance.DemoRunning;
            case "taxTaught": return Level3PracticeRules.TaxTaught(State);
            case "creditLesson": return Level3PracticeRules.CreditLessonDue(State) && Level3DeskCalculator.Instance != null && Level3DeskCalculator.Instance.Lifted && !Level3DeskCalculator.Instance.DemoRunning;
            case "creditTaught": return Level3PracticeRules.CreditTaught(State);
            case "bandStep": return Level3ComputationRules.TapeOf(State, Level3ChapterRules.BeaCaseId, Level3ComputationRules.Taxable) != null && !Level3PracticeRules.LessonDue(State);
            case "creditStep": return Level3CertificateRules.Acquired(State) && !Level3PracticeRules.LessonDue(State);
            // The first lift at the desk (the hand points only while it rests there; later visits have the example done).
            case "calculatorToLift": return Level3PracticeRules.CalculatorLessonDue(State) && Level3DeskCalculator.Instance?.GuideBody != null;
            case "sortingPractice": return Level3SortingPractice.Due(State) && Level3BeaTable.Instance != null && Level3BeaTable.Instance.Seated && !Level3BeaTable.Instance.IntroPending && !Level3BeaTable.Instance.DemoRunning;
            case "sortingDemoSeen": return Level3SortingPractice.DemoSeen(State);
            case "sortingTaught": return Level3SortingPractice.Taught(State);
            case "beaRecordsCollected": return Phase >= Level3Phase.WorksheetDraft;
            // Follow-up: not while Arthur demonstrates the practice slips (the feed/choose/pull hints are for the player's turn).
            case "atBeaTable": return Level3BeaTable.Instance != null && Level3BeaTable.Instance.Seated && !Level3BeaTable.Instance.IntroPending && !Level3BeaTable.Instance.DemoRunning;
            // T04 buildup: Bea's organizer offer was made (or a restore skipped her first-encounter steps).
            case "beaIntroDone": return Level3BeaTable.Instance != null && Level3BeaTable.Instance.IntroDone;
            // T05: the paper machine (local presentation facts; the filing itself is the saved draft).
            case "machineLoaded": return Level3BeaTable.Instance != null && Level3BeaTable.Instance.Flow.Step == Level3TableStaging.MachineStep.Loaded;
            case "machineWhy": return Level3BeaTable.Instance != null && Level3BeaTable.Instance.Flow.CanAskWhy && Level3BeaTable.Instance.Flow.Reason.Length == 0;
            case "machineBatchFiled": return Level3BeaTable.Instance != null && Level3BeaTable.Instance.BatchFiledHere;
            case "worksheetUp": return Level3WorksheetDesk.Instance != null && Level3WorksheetDesk.Instance.Lifted;
            case "worksheetAllPlaced": return Level3WorksheetRules.AllPlaced(State.worksheet);
            case "worksheetDrafted": return State.worksheet != null && State.worksheet.drafted;
            case "worksheetCalcUp": return Level3WorksheetDesk.Instance != null && Level3WorksheetDesk.Instance.CalculatorUp;
            case "worksheetTapes":
                return Level3WorksheetCheck.IsCurrent(State, Level3WorksheetRules.Income) && Level3WorksheetCheck.IsCurrent(State, Level3WorksheetRules.Expenses);
            case "worksheetChecked": return State.worksheetCheck != null && State.worksheetCheck.firstChecked;
            case "worksheetCorrecting": return Phase == Level3Phase.WorksheetCorrections;
            // Revised T06: the machine's batch checks (saved facts) and the table's local review/summary view.
            case "machineChecked": return Level3WorksheetCheck.MachineChecked(State);
            case "machineAccepted": return State.worksheetCheck?.acceptedBatches != null && State.worksheetCheck.acceptedBatches.Count > 0 && Phase < Level3Phase.WorksheetReady;
            case "machineLookAgain": return Level3BeaTable.Instance != null && Level3BeaTable.Instance.LookAgainHere;
            case "machineSummary": return Level3BeaTable.Instance != null && Level3BeaTable.Instance.SummaryOut;
            case "worksheetReady": return Phase == Level3Phase.WorksheetReady;
            case "worksheetCollected": return Level3WorksheetCheck.Collected(State);
            case "beaComputation": return Phase == Level3Phase.BeaComputation;
            case "computationTotals": return Level3WorksheetCheck.Collected(State);
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
            // T10: her copy and its explanation (saved facts).
            case "beaCopyCarried": return Level3DeliveryRules.StageOf(State) == Level3DeliveryRules.Stage.Explain || Level3DeliveryRules.StageOf(State) == Level3DeliveryRules.Stage.HandOver;
            case "beaBalanceExplained": return State.Case(Level3ChapterRules.BeaCaseId)?.balanceExplained == true;
            case "beaCopyDelivered": return Level3DeliveryRules.Delivered(State);
            case "reviewClockOn": return State.Case(Level3ChapterRules.BeaCaseId)?.clock?.started == true && !Level3ReviewRules.CaseDone(State);
            // T14: the closing steps (saved facts).
            case "closingFinding": return Level3ClosingRules.StageOf(State) == Level3ClosingRules.Stage.Finding;
            case "findingRecorded": return Level3ClosingRules.FindingRecorded(State);
            case "messageRead": return Level3ClosingRules.MessageRead(State);
            case "chapterResultPending": return Level3ClosingRules.StageOf(State) == Level3ClosingRules.Stage.Result;
            case "chapterComplete": return Level3ClosingRules.Completed(State);
            default: return false;
        }
    }
}
