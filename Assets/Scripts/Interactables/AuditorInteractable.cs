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
    public void OnInteract()
    {
        if (dialogueSessionActive || dialogueUI == null) return;

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

        string reminder = requiresTaxReturn
            ? "Please submit both the Case Folder and the Tax Return when you're ready."
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

    /// <summary>
    /// Called by AuditorSubmissionTray once the required documents for this
    /// case's filing status have been dropped on it. This is the REAL entry
    /// point into the final audit, per R11 — locks the FSM, evaluates verdict
    /// correctness + compliance issues, and always proceeds forward
    /// regardless of outcome. No "go back and fix it" path exists anymore.
    /// </summary>
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

        CaseData data = CaseManager.Instance.CurrentCase;
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
            builder.Npc(
                "Excellent work! I've reviewed your case, and everything is correct.",
                "Auditor_Happy");

            builder.Npc(
                "Congratulations! You know your taxpayer. You correctly identified their residency, classification, and how they earn their income — that's the foundation every accurate filing is built on.",
                "Auditor_Happy");
        }
        else
        {
            foreach (var issue in result.MissedIssues)
            {
                if (issue == null) continue;
                builder.Npc(issue.ShortLabel, "Auditor_Disappointed");
            }

            builder.Npc(
                "This case isn't quite ready yet. Take another look, and don't worry — you can go back and work through it again.",
                "Auditor_Default");
        }

        return builder.Build();
    }

    private void OnDialogueConcluded(SubmissionResult result)
    {
        dialogueSessionActive = false;
        auditPending = false;
        pendingAuditCase = null;

        CaseData data = CaseManager.Instance.CurrentCase;
        data.auditMistakeCount = result.MissedIssues.Count;
        data.auditPassed = result.VerdictWasCorrect;
        data.finalVerdictWasCorrect = result.VerdictWasCorrect;
        data.finalMissedIssueCount = result.MissedIssues.Count;

        // The Audit Summary card always shows the outcome: an APPROVED card
        // that continues by itself (tap to skip), or the findings to fix.
        summaryPopupUI.Show(result.MissedIssues, () => OnSummaryClosed(result));
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
            CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
            IGameState state = GameStateMachine.Instance != null ? GameStateMachine.Instance.CurrentState : null;
            bool submitted = state is AuditSubmittedState || state is CaseOutcomeState ||
                             state is ArchiveCaseState || state is CaseCompleteState;
            return NpcAvailabilityRules.Auditor(data != null,
                auditPending && System.Object.ReferenceEquals(pendingAuditCase, data), submitted);
        }
    }

    public string GetPromptText() => auditPending ? "Click to resume the audit" : "Click to talk to the Auditor";
}
