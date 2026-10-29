using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(NpcStatusMarker))]
[RequireComponent(typeof(NpcStateMachine))]
public class AuditorInteractable : MonoBehaviour, IInteractable, INpcAvailabilitySource
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private AuditSummaryPopupUI summaryPopupUI;
    [SerializeField] private AuditorSubmissionTray submissionTray;

    private NpcStateMachine npcState;
    private bool dialogueSessionActive;
    private bool auditPending;
    private CaseData pendingAuditCase;

    private void Awake()
    {
        npcState = GetComponent<NpcStateMachine>();
        Current = this;
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    private void OnDisable()
    {
        if (dialogueSessionActive && dialogueUI != null)
        {
            dialogueUI.CancelDialogueFor(this);
        }
    }

    public void OnFocus()
    {
        if (npcState.CurrentState is NpcIdleState) npcState.ChangeState(new NpcWaitingState());
    }

    public void OnUnfocus()
    {
        if (npcState.CurrentState is NpcWaitingState) npcState.ChangeState(new NpcIdleState());
    }

    /// <summary>
    /// Per R11, clicking the Auditor no longer starts the audit directly —
    /// the audit only begins once the required documents (Case Folder,
    /// and Tax Return if Ready For Filing) have been submitted via the
    /// AuditorSubmissionTray. Clicking him now just acknowledges his
    /// presence / prompts the player toward the submission step.
    /// </summary>
    /// <summary>T08: the Client Copy handover may answer a tap on the Auditor (true = handled).</summary>
    public static System.Func<AuditorInteractable, bool> TalkOverride;

    /// <summary>
    /// T10 (Level 1 hotbar): asked before the hand-in reminder. True = the
    /// Case Folder is only in the bag and the "Take it out" prompt was shown.
    /// </summary>
    public static System.Func<bool> BagReminder;

    /// <summary>T08: the Client Copy handover's marker state for the Auditor (null = the case state).</summary>
    public static System.Func<NpcAvailability?> AvailabilityOverride;

    /// <summary>
    /// T08: the Auditor says a few lines outside the audit (the Client Copy),
    /// through the shared dialogue flow; he stays completed afterwards.
    /// </summary>
    public bool TrySay(List<DialogueLine> lines, System.Action done)
    {
        if (dialogueSessionActive || dialogueUI == null || lines == null || lines.Count == 0) return false;
        if (!dialogueUI.TryStartDialogue(lines,
                () => { dialogueSessionActive = false; npcState.ChangeState(new NpcCompletedState()); done?.Invoke(); },
                () => { dialogueSessionActive = false; npcState.ChangeState(new NpcCompletedState()); done?.Invoke(); },
                this))
            return false;
        dialogueSessionActive = true;
        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());
        return true;
    }

    public void OnInteract()
    {
        if (dialogueSessionActive || dialogueUI == null) return;
        if (TalkOverride != null && TalkOverride(this)) return;

        CaseData data = CaseManager.Instance.CurrentCase;
        if (auditPending && System.Object.ReferenceEquals(pendingAuditCase, data))
        {
            BeginFinalAudit();
            return;
        }
        auditPending = false;

        bool requiresTaxReturn = data.filingStatus == FilingStatus.ReadyForFiling;
        bool alreadySubmitted = GameStateMachine.Instance.CurrentState is AuditSubmittedState
            || GameStateMachine.Instance.CurrentState is CaseOutcomeState
            || GameStateMachine.Instance.CurrentState is ArchiveCaseState
            || GameStateMachine.Instance.CurrentState is CaseCompleteState;

        if (alreadySubmitted)
        {
            return; // audit already happened, nothing further to say here
        }

        // T10: the Case Folder is carried but only in the bag: offer "Take it out" instead of the reminder.
        if (BagReminder != null && BagReminder())
        {
            return;
        }

        // Level 1 (2026-10-26): with the folder in hand, the hand-in is a conversation.
        // Level 2 (consistency pass): the same conversation; the level's review decides.
        if (TalkFirst && (LevelHandIn != null || !requiresTaxReturn) && FolderInHand() != null)
        {
            string refusal = LevelHandIn != null ? LevelHandIn.Refusal() : null;
            if (refusal != null) SayNotYet(refusal);
            else StartHandInConversation();
            return;
        }

        string reminder = requiresTaxReturn
            ? "Please submit both the Case Folder and the Tax Return when you're ready."
            : TalkFirst ? "Bring me the Case Folder when your case is ready, and we'll go through it together."
            : "Please submit the Case Folder when you're ready.";

        var builder = new DialogueBuilder("Auditor")
            .Npc(reminder, "Auditor_Default")
            .Build();

        INpcState priorNpcState = npcState.CurrentState;
        if (!dialogueUI.TryStartDialogue(
            builder,
            OnReminderConcluded,
            () => OnDialogueCancelled(priorNpcState),
            this)) return;

        dialogueSessionActive = true;
        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());
    }

    private void OnReminderConcluded()
    {
        dialogueSessionActive = false;
        npcState.ChangeState(new NpcIdleState());
    }

    /// <summary>The folder is in hand but the level's review cannot be handed in yet: he says why and takes nothing.</summary>
    private void SayNotYet(string reason)
    {
        CaseDefinition def = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        string client = def != null && def.clientFinale != null && !string.IsNullOrWhiteSpace(def.clientFinale.name) ? def.clientFinale.name : "my client";
        List<DialogueLine> lines = new DialogueBuilder("Auditor")
            .Player($"Hi! I have {client}'s folder.")
            .Npc(reason, "Auditor_Default")
            .Build();
        INpcState priorNpcState = npcState.CurrentState;
        if (!dialogueUI.TryStartDialogue(lines, OnReminderConcluded, () => OnDialogueCancelled(priorNpcState), this)) return;
        dialogueSessionActive = true;
        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());
    }

    /// <summary>
    /// Called by AuditorSubmissionTray once the required documents for this
    /// case's filing status have been dropped on it. This is the REAL entry
    /// point into the final audit, per R11 — locks the FSM, evaluates verdict
    /// correctness + compliance issues, and always proceeds forward
    /// regardless of outcome. No "go back and fix it" path exists anymore.
    /// </summary>
    // Brute-force guard (Level 1 Phase 3): the answers of the last failed
    // audit, and how many audits have failed for this case.
    private string lastFailedAnswers;
    private int failedAudits;

    /// <summary>How many audits of the current case failed (read-only; the case finale uses it).</summary>
    public int FailedAudits => failedAudits;

    /// <summary>Signature of the last answers that failed (the retry guard), for the Level 1 checkpoint.</summary>
    public string LastFailedAnswers => lastFailedAnswers;

    /// <summary>Resume (T05): the failed audits and the retry guard of the saved run (no dialogue, card or charge).</summary>
    public void RestoreRetryGuard(int failed, string lastFailed)
    {
        failedAudits = Mathf.Max(0, failed);
        lastFailedAnswers = failedAudits > 0 && !string.IsNullOrEmpty(lastFailed) ? lastFailed : null;
    }

    /// <summary>The three required answers as one comparable string.</summary>
    private static string AnswerSignature(CaseData data)
    {
        CaseDefinition def = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        return CaseFieldRules.Signature(data, def != null ? def.GradedFields() : CaseFieldRules.Defaults());
    }

    /// <summary>
    /// The same answers that already failed come back: the Auditor refuses
    /// to re-check them and hands the folder back (it must be stored again).
    /// </summary>
    private void RefuseSameAnswers()
    {
        CaseDefinition def = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        string line = def != null && !string.IsNullOrWhiteSpace(def.auditorSameAnswersLine)
            ? def.auditorSameAnswersLine
            : "These are the exact same answers you gave me last time. Change what's wrong, then come back.";
        var lines = new DialogueBuilder("Auditor").Npc(line, "Auditor_Disappointed").Build();
        System.Action handBack = () =>
        {
            dialogueSessionActive = false;
            auditPending = false;
            pendingAuditCase = null;
            if (AuditorSubmissionTray.Instance != null) AuditorSubmissionTray.Instance.ResetForRetryWithoutNewCase();
        };
        if (dialogueUI.TryStartDialogue(lines, handBack, handBack, this)) dialogueSessionActive = true;
        else handBack();
    }

    public void BeginFinalAudit()
    {
        if (CaseManager.Instance == null || CaseManager.Instance.CurrentCase == null ||
            GameStateMachine.Instance == null ||
            GameStateMachine.Instance.CurrentState is AuditSubmittedState ||
            GameStateMachine.Instance.CurrentState is CaseOutcomeState ||
            GameStateMachine.Instance.CurrentState is ArchiveCaseState ||
            GameStateMachine.Instance.CurrentState is CaseCompleteState)
        {
            return;
        }

        // A level with its own review (Level 2) judges the hand-in itself.
        if (LevelAudit != null && LevelAudit(this)) return;
        // Its folder is handed over only in his conversation (never Level 1's check on its case).
        if (LevelHandIn != null) return;

        CaseData data = CaseManager.Instance.CurrentCase;
        if (lastFailedAnswers != null && AnswerSignature(data) == lastFailedAnswers)
        {
            if (!dialogueSessionActive && dialogueUI != null && !DialogueUI.IsAnyDialogueActive) RefuseSameAnswers();
            return;
        }
        // The tray has already accepted the folder. Keep a retry route on this
        // NPC when admission is rejected or the conversation is cancelled.
        pendingAuditCase = data;
        auditPending = true;
        if (dialogueSessionActive || dialogueUI == null || DialogueUI.IsAnyDialogueActive) return;
        SubmissionResult result = EvaluateSubmission(data);
        List<DialogueLine> lines = BuildAuditDialogue(result);

        GameStateMachine gameStateMachine = GameStateMachine.Instance;
        IGameState priorGameState = gameStateMachine.CurrentState;
        INpcState priorNpcState = npcState.CurrentState;
        var submittedState = new AuditSubmittedState();

        if (!dialogueUI.TryStartDialogue(
            lines,
            () => OnDialogueConcluded(result),
            () => OnAuditCancelled(
                priorNpcState,
                gameStateMachine,
                priorGameState,
                submittedState
            ),
            this)) return;

        dialogueSessionActive = true;
        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());
        gameStateMachine.ChangeState(submittedState);
    }

    // =========================================================
    // LEVEL AUDIT HOOK (Level 2 review cases)
    // =========================================================

    /// <summary>
    /// Optional level rule: handles a final hand-in itself (returns true).
    /// Null or false keeps the Level 1 audit unchanged. (Level 2 now uses LevelHandIn.)
    /// </summary>
    public static System.Func<AuditorInteractable, bool> LevelAudit;

    /// <summary>
    /// A level whose review decides the hand-in (Level 2, consistency pass), inside the same
    /// talk-first conversation as Level 1: the player drags the folder onto him when he asks,
    /// the level decides once, the Audit Summary shows, and approval or the return follows.
    /// </summary>
    public interface ILevelHandIn
    {
        /// <summary>Null when he can take the folder now; otherwise his line saying why (nothing is decided or charged).</summary>
        string Refusal();
        /// <summary>The folder is in his hands: decide and record once. Null when nothing could be decided.</summary>
        LevelHandInDecision Decide();
        /// <summary>The summary of an approval was read: complete the review through the existing states.</summary>
        void Approve();
        /// <summary>The folder went back (the level keeps every bit of the work).</summary>
        void Returned();
    }

    /// <summary>One decided hand-in: his explanation, the Audit Summary card and whether it was approved.</summary>
    public sealed class LevelHandInDecision
    {
        public List<DialogueLine> Lines = new List<DialogueLine>();
        public RewardScreenData Summary;
        public bool Approved;
    }

    /// <summary>Set by the level whose review decides hand-ins (Level2Audit); null in Level 1.</summary>
    public static ILevelHandIn LevelHandIn;

    /// <summary>Plays a level's audit lines with the usual NPC dialogue states. False when a dialogue is already running.</summary>
    public bool RunLevelDialogue(List<DialogueLine> lines, System.Action onDone)
    {
        if (dialogueSessionActive || dialogueUI == null || DialogueUI.IsAnyDialogueActive || lines == null || lines.Count == 0) return false;
        INpcState priorNpcState = npcState.CurrentState;
        if (!dialogueUI.TryStartDialogue(
            lines,
            () =>
            {
                dialogueSessionActive = false;
                npcState.ChangeState(new NpcIdleState());
                onDone?.Invoke();
            },
            () =>
            {
                OnDialogueCancelled(priorNpcState);
                onDone?.Invoke();
            },
            this)) return false;
        dialogueSessionActive = true;
        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());
        return true;
    }

    /// <summary>Shows Level 1's Audit Summary card with a level's own content (Level 2 review cases). False without the card.</summary>
    public bool ShowLevelSummary(RewardScreenData data, string plainText, System.Action closed)
    {
        if (summaryPopupUI == null || data == null) return false;
        summaryPopupUI.ShowScreen(data, plainText, closed);
        return true;
    }

    /// <summary>Gives the folder back for more work (the review itself is kept by the level).</summary>
    public void ReturnFolderForReview()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_audit_returned_01");
        if (submissionTray != null) submissionTray.ResetForRetryWithoutNewCase();
        if (GameStateMachine.Instance != null) GameStateMachine.Instance.UnlockProgression();
    }

    /// <summary>An accepted review: the Auditor is done and the case completes through the existing states.</summary>
    public void CompleteLevelReview()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_audit_approved_01");
        npcState.ChangeState(new NpcCompletedState());
        GameStateMachine machine = GameStateMachine.Instance;
        if (machine == null) return;
        if (!(machine.CurrentState is AuditSubmittedState)) machine.ChangeState(new AuditSubmittedState());
        machine.ChangeState(new CaseCompleteState());
    }

    private SubmissionResult EvaluateSubmission(CaseData data)
    {
        CaseAssessment correctVerdict = CaseVerdictEvaluator.DetermineCorrectVerdict(data);
        List<ComplianceIssue> issues = ComplianceChecker.RunCheck(data);

        // Single source of truth: zero issues == pass. This also implies the
        // verdict is correct in Level 1 since both checks key off the same
        // three facts — but we compute VerdictWasCorrect for display purposes
        // without letting it independently decide pass/fail.
        bool passed = issues.Count == 0;

        var result = new SubmissionResult
        {
            PlayerVerdict = data.caseAssessment,
            CorrectVerdict = correctVerdict,
            VerdictWasCorrect = passed,
            MissedIssues = issues,
            FalseIssues = new List<ComplianceIssue>()
        };

        return result;
    }

    private List<DialogueLine> BuildAuditDialogue(SubmissionResult result)
    {
        var builder = new DialogueBuilder("Auditor");

        if (result.MissedIssues == null || result.MissedIssues.Count == 0)
        {
            // The case JSON may give its own approval lines.
            CaseDefinition approvedDef = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
            List<string> approved = approvedDef != null ? approvedDef.auditorApprovedLines : null;
            bool said = false;
            if (approved != null)
                foreach (string line in approved)
                    if (!string.IsNullOrWhiteSpace(line)) { builder.Npc(line, "Auditor_Happy"); said = true; }

            if (!said)
            {
                builder.Npc(
                    "Excellent work! I've reviewed your case, and everything is correct.",
                    "Auditor_Happy");

                builder.Npc(
                    "Congratulations! You know your taxpayer. You correctly identified their residency, classification, and how they earn their income — that's the foundation every accurate filing is built on.",
                    "Auditor_Happy");
            }
        }
        else
        {
            foreach (var issue in result.MissedIssues)
            {
                if (issue == null) continue;
                builder.Npc(issue.ShortLabel, "Auditor_Disappointed");
            }

            // Gentle the first time, stricter on every later try.
            CaseDefinition def = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
            List<string> retry = def != null ? def.auditorRetryLines : null;
            string closing = retry != null && retry.Count > 0
                ? retry[Mathf.Min(failedAudits, retry.Count - 1)]
                : "This case isn't quite ready yet. Take another look, and don't worry — you can go back and work through it again.";
            builder.Npc(closing, failedAudits == 0 ? "Auditor_Default" : "Auditor_Disappointed");
        }

        return builder.Build();
    }

    private void OnDialogueConcluded(SubmissionResult result)
    {
        dialogueSessionActive = false;
        RecordAuditOutcome(result);

        // The Audit Summary card always shows the outcome: an APPROVED card
        // that continues by itself (tap to skip), or the findings to fix.
        summaryPopupUI.Show(result.MissedIssues, () => OnSummaryClosed(result));
    }

    /// <summary>The audit's result becomes the case's (sound, retry guard, counts); then the summary card shows it.</summary>
    private void RecordAuditOutcome(SubmissionResult result)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEffect(result.MissedIssues.Count == 0 ? "sfx_audit_approved_01" : "sfx_audit_returned_01");
        auditPending = false;
        pendingAuditCase = null;

        CaseData data = CaseManager.Instance.CurrentCase;
        if (result.MissedIssues.Count > 0)
        {
            // Remember what failed, before the retry clears the answers.
            lastFailedAnswers = AnswerSignature(data);
            failedAudits++;
        }
        data.auditMistakeCount = result.MissedIssues.Count;
        data.auditPassed = result.VerdictWasCorrect;
        data.finalVerdictWasCorrect = result.VerdictWasCorrect;
        data.finalMissedIssueCount = result.MissedIssues.Count;
        // The guide explains the card that is actually showing (2026-10-26).
        GameplayEvents.RaiseGuideCue(result.MissedIssues.Count == 0 ? CueApproved : failedAudits > 1 ? CueNeedsFixAgain : CueNeedsFix);
    }

    private void OnAuditCancelled(
        INpcState priorNpcState,
        GameStateMachine gameStateMachine,
        IGameState priorGameState,
        IGameState submittedState)
    {
        dialogueSessionActive = false;
        RestoreNpcState(priorNpcState);

        if (gameStateMachine != null &&
            System.Object.ReferenceEquals(
                gameStateMachine.CurrentState,
                submittedState
            ))
        {
            gameStateMachine.UnlockProgression();
            gameStateMachine.ChangeState(priorGameState);
        }
    }

    private void OnDialogueCancelled(INpcState priorNpcState)
    {
        dialogueSessionActive = false;
        RestoreNpcState(priorNpcState);
    }

    private void RestoreNpcState(INpcState priorNpcState)
    {
        if (npcState == null) return;

        npcState.ChangeState(priorNpcState is NpcCompletedState
            ? priorNpcState
            : new NpcIdleState());
    }

    private void OnSummaryClosed(SubmissionResult result)
    {
        bool passed = result.VerdictWasCorrect && result.MissedIssues.Count == 0;

        // Level score: every audit counts; a failed one costs the case reward
        // (mistakes found + one more submission, per rewards.json).
        if (CaseProgressionManager.Instance != null)
            CaseProgressionManager.Instance.RecordAuditAttempt(result.MissedIssues.Count, passed);

        if (passed)
        {
            npcState.ChangeState(new NpcCompletedState());

            if (GameStateMachine.Instance.CurrentState is AuditSubmittedState)
            {
                CaseData data = CaseManager.Instance.CurrentCase;

                // Single approved call site for reward calculation, per
                // CaseProgressionManager's contract.
                CaseProgressionManager.Instance.CalculateAndStoreCaseReward(data);

                GameStateMachine.Instance.ChangeState(new CaseCompleteState());
            }
        }
        else
        {
            // Failed — auditor is NOT done, he's ready for another submission.
            npcState.ChangeState(new NpcIdleState());
            RetryCase();
        }
    }

    /// <summary>
    /// Per the retry loop requirement: a failed submission is NOT terminal.
    /// Resets the case's player-entered facts, restores the folder to its
    /// original position/state, unlocks player progression, and returns the
    /// FSM to ReceiveCaseState so the player can re-investigate and resubmit.
    /// </summary>
    private void RetryCase()
    {
        CaseData data = CaseManager.Instance.CurrentCase;

        // Clear player-entered answers — full reset per design.
        CaseManager.Instance.ResetCurrentCaseForRetry();

        // Bring the folder back into play at its original position.
        submissionTray.ResetForRetryWithoutNewCase();

        // Unlock and return control to the player at the start of the case.
        GameStateMachine.Instance.UnlockProgression();
        GameStateMachine.Instance.ChangeState(new ReceiveCaseState());
    }



    /// <summary>
    /// T04 marker state: no marker for the pre-submission reminder; available
    /// to resume an interrupted audit; completed once the audit is submitted.
    /// </summary>
    public NpcAvailability Availability
    {
        get
        {
            NpcAvailability? copy = AvailabilityOverride != null ? AvailabilityOverride() : null;
            if (copy.HasValue) return copy.Value;
            CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
            IGameState state = GameStateMachine.Instance != null ? GameStateMachine.Instance.CurrentState : null;
            bool submitted = state is AuditSubmittedState || state is CaseOutcomeState ||
                             state is ArchiveCaseState || state is CaseCompleteState;
            return NpcAvailabilityRules.Auditor(data != null,
                auditPending && System.Object.ReferenceEquals(pendingAuditCase, data), submitted);
        }
    }

    public string GetPromptText() => auditPending ? "Tap to resume the audit" : "Tap to talk to the Auditor";

    // =========================================================
    // LEVEL 1 HAND-IN CONVERSATION (2026-10-26)
    // =========================================================
    //
    // Talk first: one line from the player, one from the Auditor asking for
    // the folder, and the conversation waits (like Marie's Client Copy step)
    // until the Case Folder icon is dragged onto him (or tapped). The icon
    // empties (he has the folder). He goes through it, the conversation waits
    // while the Audit Summary card is open, then goes on: approved, he signs
    // it off and drops Marie's Client Copy while still talking (the
    // ClientCopyHandover fills that part in); otherwise he hands the folder
    // back (to the desk, for another go).

    public const string SubmitAction = "auditor-submit";
    public const string SummaryAction = "audit-summary";
    public const string ApprovalAction = "auditor-approval";
    public const string CueApproved = "AuditApproved";
    public const string CueNeedsFix = "AuditNeedsFix";
    public const string CueNeedsFixAgain = "AuditNeedsFixAgain";
    public const string CueFolderBack = "AuditFolderHandedBack";
    public const string CueAsksForFolder = "AuditorAsksForFolder";

    /// <summary>The Auditor of the loaded office (one per scene).</summary>
    public static AuditorInteractable Current { get; private set; }

    /// <summary>The folder is handed over in the conversation: Level 1 with the hotbar, and Level 2's review (LevelHandIn).</summary>
    public static bool TalkFirst => LevelAudit == null && (HudHotbar.Instance != null || LevelHandIn != null);

    /// <summary>The Auditor's conversation is waiting for the Case Folder.</summary>
    public static bool AwaitingFolder =>
        Current != null && Current.handInConversation && DialogueUI.ActiveAwaitingAction == SubmitAction;

    private bool handInConversation;
    private bool folderTaken;
    private bool summaryOpened;
    private bool summaryClosed;
    private bool sameAnswers;
    private SubmissionResult handInResult;
    private FolderToHUDIcon handedFolder;
    private IGameState handInPriorState;
    private AuditSubmittedState handInSubmittedState;
    private int dropLineIndex = -1;
    private System.Action dropAction;

    private static FolderToHUDIcon FolderInHand()
    {
        foreach (FolderToHUDIcon folder in FindObjectsByType<FolderToHUDIcon>(FindObjectsInactive.Include))
            if (folder != null && folder.IsFolderStored()) return folder;
        return null;
    }

    private void StartHandInConversation()
    {
        CaseDefinition def = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        string client = def != null && def.clientFinale != null && !string.IsNullOrWhiteSpace(def.clientFinale.name) ? def.clientFinale.name : "my client";
        List<DialogueLine> lines = new DialogueBuilder("Auditor")
            .Player($"Hi! {client}'s case is ready for your review.")
            .Npc("Good. Hand me the Case Folder, and I'll go through it with you.", "Auditor_Default")
            .AwaitAction(SubmitAction, "Drag the Case Folder to the Auditor")
            .Build();

        folderTaken = summaryOpened = summaryClosed = sameAnswers = false;
        handInResult = null;
        levelDecision = null;
        handedFolder = null;
        dropLineIndex = -1;
        dropAction = null;
        INpcState priorNpcState = npcState.CurrentState;
        if (!dialogueUI.TryStartDialogue(lines, OnHandInEnded, () => OnHandInCancelled(priorNpcState), this)) return;
        handInConversation = true;
        dialogueSessionActive = true;
        dialogueUI.LineChanged -= OnHandInLine;
        dialogueUI.LineChanged += OnHandInLine;
        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());
    }

    /// <summary>Folder icon tap while he waits for it: the same as dragging it onto him.</summary>
    public bool TryIconHandIn() => AwaitingFolder && TryHandInFolder();

    /// <summary>
    /// A Case Folder drag released (OfficeHudRuntime). Level 1 only: on the
    /// Auditor (or his tray) while he asks for it, he takes it; on him before
    /// that, or anywhere else while he waits, it goes back to its icon. Null =
    /// not his drop (the icon decides as before).
    /// </summary>
    public static HudDragOutcome? ResolveFolderDrop(Vector2 screen)
    {
        if (!TalkFirst || Current == null) return null;
        bool onHim = Current.IsOver(screen);
        if (AwaitingFolder) return onHim && Current.TryHandInFolder() ? HudDragOutcome.Consumed : HudDragOutcome.Returned;
        if (!onHim) return null;
        // Not yet: he takes the folder in a conversation (a tap on him starts it). No icon outside a conversation.
        return HudDragOutcome.Returned;
    }

    private bool IsOver(Vector2 screen)
    {
        Camera cam = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(screen);
        Transform tray = submissionTray != null ? submissionTray.transform : null;
        foreach (RaycastHit hit in Physics.RaycastAll(ray, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            if (hit.collider == null) continue;
            Transform t = hit.collider.transform;
            if (t.IsChildOf(transform) || (tray != null && t.IsChildOf(tray))) return true;
        }
        return false;
    }

    /// <summary>He takes the folder (the icon empties) and goes through it, all in the running conversation.</summary>
    private bool TryHandInFolder()
    {
        if (!AwaitingFolder || CaseManager.Instance == null || CaseManager.Instance.CurrentCase == null) return false;
        FolderToHUDIcon folder = FolderInHand();
        if (folder == null) return false;
        if (submissionTray != null && !submissionTray.AcceptFromConversation()) return false;
        if (OfficeHudRuntime.Instance != null) OfficeHudRuntime.Instance.CloseCaseFolderView();
        folder.ReleaseQuietly();
        handedFolder = folder;
        folderTaken = true;

        CaseData data = CaseManager.Instance.CurrentCase;
        if (LevelHandIn != null)
        {
            // The level's review decides once, now that the folder is in his hands (a cancelled
            // conversation can never decide again: the summary and its result still follow).
            levelDecision = LevelHandIn.Decide();
            if (levelDecision == null)
            {
                // Nothing could be decided (the review changed under him): the folder goes straight back.
                dialogueUI.ContinueWith(SubmitAction, new DialogueBuilder("Auditor")
                    .Npc("Let me give this back for now. Bring it again when it's ready.", "Auditor_Default").Build());
                sameAnswers = true;
                return true;
            }
            List<DialogueLine> levelLines = new List<DialogueLine>(levelDecision.Lines);
            levelLines.AddRange(new DialogueBuilder("Auditor")
                .Npc(levelDecision.Approved ? "Here's my summary." : "Here's my summary. It shows where to look again.",
                    levelDecision.Approved ? "Auditor_Happy" : "Auditor_Default")
                .AwaitAction(SummaryAction, "Read the Audit Summary")
                .Build());
            dialogueUI.ContinueWith(SubmitAction, levelLines);
            return true;
        }
        if (lastFailedAnswers != null && AnswerSignature(data) == lastFailedAnswers)
        {
            // The same answers that already failed: he does not check them again.
            sameAnswers = true;
            CaseDefinition def = CaseManager.Instance.CurrentDefinition;
            string line = def != null && !string.IsNullOrWhiteSpace(def.auditorSameAnswersLine)
                ? def.auditorSameAnswersLine
                : "These are the exact same answers you gave me last time. Change what's wrong, then come back.";
            dialogueUI.ContinueWith(SubmitAction, new DialogueBuilder("Auditor")
                .Npc(line, "Auditor_Disappointed")
                .Npc("Here's your folder back.", "Auditor_Default")
                .Build());
            return true;
        }

        pendingAuditCase = data;
        auditPending = true;
        handInResult = EvaluateSubmission(data);
        bool passed = handInResult.MissedIssues == null || handInResult.MissedIssues.Count == 0;
        List<DialogueLine> lines = BuildAuditDialogue(handInResult);
        lines.AddRange(new DialogueBuilder("Auditor")
            .Npc(passed ? "Here's my summary." : "Here's my summary. It shows where to look again.", passed ? "Auditor_Happy" : "Auditor_Default")
            .AwaitAction(SummaryAction, "Read the Audit Summary")
            .Build());

        GameStateMachine machine = GameStateMachine.Instance;
        if (machine != null)
        {
            handInPriorState = machine.CurrentState;
            handInSubmittedState = new AuditSubmittedState();
            machine.ChangeState(handInSubmittedState);
        }
        dialogueUI.ContinueWith(SubmitAction, lines);
        return true;
    }

    /// <summary>Each new line: the summary card opens on its waiting line; the Client Copy drops on the hand-over line.</summary>
    private void OnHandInLine(DialogueLine line)
    {
        if (!handInConversation || line == null) return;
        if (line.AwaitAction == SubmitAction) GameplayEvents.RaiseGuideCue(CueAsksForFolder);
        if (line.AwaitAction == SummaryAction && !summaryOpened && levelDecision != null)
        {
            summaryOpened = true;
            OpenLevelSummary();
        }
        if (line.AwaitAction == SummaryAction && !summaryOpened && handInResult != null)
        {
            summaryOpened = true;
            RecordAuditOutcome(handInResult);
            summaryPopupUI.Show(handInResult.MissedIssues, OnHandInSummaryClosed);
        }
        if (dropAction != null && dropLineIndex >= 0 && dialogueUI.CurrentLineIndex >= dropLineIndex) DropNow();
    }

    // The level's decided hand-in (Level 2): kept until its summary was read and its result applied.
    private LevelHandInDecision levelDecision;

    /// <summary>The level's Audit Summary card (the same card as Level 1's); without the card the result applies at once.</summary>
    private void OpenLevelSummary()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEffect(levelDecision.Approved ? "sfx_audit_approved_01" : "sfx_audit_returned_01");
        GameplayEvents.RaiseGuideCue(levelDecision.Approved ? CueApproved : CueNeedsFix);
        if (!ShowLevelSummary(levelDecision.Summary, "Audit Summary", OnHandInSummaryClosed)) OnHandInSummaryClosed();
    }

    /// <summary>The level's summary was read: an approval signs off and completes; a return waits for the end of his lines.</summary>
    private void OnLevelSummaryClosed()
    {
        bool talking = handInConversation && DialogueUI.ActiveAwaitingAction == SummaryAction;
        if (!levelDecision.Approved)
        {
            if (talking)
                dialogueUI.ContinueWith(SummaryAction, new DialogueBuilder("Auditor")
                    .Npc("Here's your folder back. Your work is all still in it: fix what I marked, stamp again, then bring it to me.", "Auditor_Default")
                    .Build());
            else HandFolderBack(retry: true);
            return;
        }
        if (talking)
            dialogueUI.ContinueWith(SummaryAction, new DialogueBuilder("Auditor")
                .Npc("Let me sign this off.", "Auditor_Happy")
                .AwaitAction(ApprovalAction, null)
                .Build());
        handedFolder = null;
        LevelHandIn.Approve();
        if (handInConversation && DialogueUI.ActiveAwaitingAction == ApprovalAction) dialogueUI.ContinueWith(ApprovalAction, null);
    }

    private void OnHandInSummaryClosed()
    {
        if (summaryClosed) return;
        summaryClosed = true;
        if (levelDecision != null)
        {
            OnLevelSummaryClosed();
            return;
        }
        bool passed = handInResult.VerdictWasCorrect && handInResult.MissedIssues.Count == 0;
        if (!passed)
        {
            // He hands the folder back at the end of the conversation (OnHandInEnded).
            dialogueUI.ContinueWith(SummaryAction, new DialogueBuilder("Auditor")
                .Npc("Here's your folder back. Fix what I marked, then bring it to me again.", "Auditor_Default")
                .Build());
            return;
        }
        // Approved: he signs it off; the case result and Marie's Client Copy continue this conversation.
        dialogueUI.ContinueWith(SummaryAction, new DialogueBuilder("Auditor")
            .Npc("Let me sign this off.", "Auditor_Happy")
            .AwaitAction(ApprovalAction, null)
            .Build());
        OnSummaryClosed(handInResult);
        // Nothing continued it (no Client Copy in this run): the conversation just ends.
        if (handInConversation && DialogueUI.ActiveAwaitingAction == ApprovalAction) dialogueUI.ContinueWith(ApprovalAction, null);
    }

    /// <summary>
    /// The Client Copy handover's approval lines continue his conversation
    /// (instead of a new one); drop runs when the last line shows. False when
    /// he is not waiting to sign off (the handover then talks as before).
    /// </summary>
    public bool TryContinueApproval(List<DialogueLine> lines, System.Action drop)
    {
        if (!handInConversation || DialogueUI.ActiveAwaitingAction != ApprovalAction) return false;
        int first = dialogueUI.ContinueWith(ApprovalAction, lines);
        if (first == -2) return false;
        dropAction = drop;
        dropLineIndex = first >= 0 && lines != null ? first + lines.Count - 1 : -1;
        if (dropLineIndex < 0 || dialogueUI.CurrentLineIndex >= dropLineIndex) DropNow();
        return true;
    }

    private void DropNow()
    {
        System.Action drop = dropAction;
        dropAction = null;
        dropLineIndex = -1;
        drop?.Invoke();
    }

    private void OnHandInEnded()
    {
        FinishHandIn();
        if (sameAnswers)
        {
            HandFolderBack(retry: false);
            npcState.ChangeState(new NpcIdleState());
            return;
        }
        if (!folderTaken)
        {
            npcState.ChangeState(new NpcIdleState());
            return;
        }
        if (levelDecision != null)
        {
            // The level's decision stands: its summary (if still unread) applies it when closed.
            if (!summaryClosed)
            {
                if (!summaryOpened) { summaryOpened = true; OpenLevelSummary(); }
                return;
            }
            if (levelDecision.Approved) npcState.ChangeState(new NpcCompletedState());
            else
            {
                HandFolderBack(retry: true);
                npcState.ChangeState(new NpcIdleState());
            }
            return;
        }
        if (!summaryClosed)
        {
            // Should not happen (the summary waits): treat as an interrupted audit.
            UndoHandIn();
            npcState.ChangeState(new NpcIdleState());
            return;
        }
        bool passed = handInResult.VerdictWasCorrect && handInResult.MissedIssues.Count == 0;
        if (passed)
        {
            npcState.ChangeState(new NpcCompletedState());
            return;
        }
        HandFolderBack(retry: true);
    }

    private void OnHandInCancelled(INpcState priorNpcState)
    {
        FinishHandIn();
        if (sameAnswers) HandFolderBack(retry: false);
        else if (levelDecision != null)
        {
            // Interrupted after the level decided: the summary still shows and applies the result; a read return gives the folder back.
            if (!summaryOpened) { summaryOpened = true; OpenLevelSummary(); }
            else if (summaryClosed && !levelDecision.Approved) HandFolderBack(retry: true);
        }
        else if (folderTaken && !summaryClosed) UndoHandIn();
        else if (folderTaken && summaryClosed && !(handInResult.VerdictWasCorrect && handInResult.MissedIssues.Count == 0)) HandFolderBack(retry: true);
        RestoreNpcState(priorNpcState);
    }

    private void FinishHandIn()
    {
        if (dialogueUI != null) dialogueUI.LineChanged -= OnHandInLine;
        handInConversation = false;
        dialogueSessionActive = false;
        if (dropAction != null) DropNow();
    }

    /// <summary>The folder goes back (into the icon first, then the retry puts it back on the desk as before).</summary>
    private void HandFolderBack(bool retry)
    {
        if (handedFolder != null) handedFolder.RestoreStored();
        handedFolder = null;
        if (retry && levelDecision != null)
        {
            // The level keeps all of the work; the folder goes back like Level 1's (to the desk).
            levelDecision = null;
            if (LevelHandIn != null) LevelHandIn.Returned();
        }
        else if (retry) OnSummaryClosed(handInResult);
        else
        {
            auditPending = false;
            pendingAuditCase = null;
            if (submissionTray != null) submissionTray.ResetForRetryWithoutNewCase();
        }
        GameplayEvents.RaiseGuideCue(CueFolderBack);
    }

    /// <summary>The conversation stopped before the summary: the folder is back in hand and nothing was checked.</summary>
    private void UndoHandIn()
    {
        if (handedFolder != null) handedFolder.RestoreStored();
        handedFolder = null;
        auditPending = false;
        pendingAuditCase = null;
        if (submissionTray != null) submissionTray.ResetSubmissionKeysOnly();
        GameStateMachine machine = GameStateMachine.Instance;
        if (machine != null && handInSubmittedState != null && System.Object.ReferenceEquals(machine.CurrentState, handInSubmittedState))
        {
            machine.UnlockProgression();
            machine.ChangeState(handInPriorState);
        }
        handInSubmittedState = null;
    }
}
