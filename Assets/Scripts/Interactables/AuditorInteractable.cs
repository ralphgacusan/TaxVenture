using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(HighlightEffect))]
[RequireComponent(typeof(NpcStateMachine))]
public class AuditorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private AuditSummaryPopupUI summaryPopupUI;
    [SerializeField] private AuditorSubmissionTray submissionTray;

    private HighlightEffect highlight;
    private NpcStateMachine npcState;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
        npcState = GetComponent<NpcStateMachine>();
    }

    public void OnFocus()
    {
        highlight.Highlight();
        if (npcState.CurrentState is NpcIdleState) npcState.ChangeState(new NpcWaitingState());
    }

    public void OnUnfocus()
    {
        highlight.Unhighlight();
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
        CaseData data = CaseManager.Instance.CurrentCase;

        bool requiresTaxReturn = data.filingStatus == FilingStatus.ReadyForFiling;
        bool alreadySubmitted = GameStateMachine.Instance.CurrentState is AuditSubmittedState
            || GameStateMachine.Instance.CurrentState is CaseOutcomeState
            || GameStateMachine.Instance.CurrentState is ArchiveCaseState
            || GameStateMachine.Instance.CurrentState is CaseCompleteState;

        if (alreadySubmitted)
        {
            return; // audit already happened, nothing further to say here
        }

        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());

        string reminder = requiresTaxReturn
            ? "Please submit both the Case Folder and the Tax Return when you're ready."
            : "Please submit the Case Folder when you're ready.";

        var builder = new DialogueBuilder("Auditor")
            .Npc(reminder, "Auditor_Default")
            .Build();

        dialogueUI.StartDialogue(builder, () => npcState.ChangeState(new NpcIdleState()));
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
        CaseData data = CaseManager.Instance.CurrentCase;

        GameStateMachine.Instance.ChangeState(new AuditSubmittedState());

        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());

        SubmissionResult result = EvaluateSubmission(data);
        RunAuditDialogue(result);
    }

    private SubmissionResult EvaluateSubmission(CaseData data)
    {
        CaseAssessment correctVerdict = CaseVerdictEvaluator.DetermineCorrectVerdict(data);
        List<ComplianceIssue> issues = ComplianceChecker.RunCheck(data);

        data.auditMistakeCount = issues.Count;

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

        data.auditPassed = passed;
        data.finalVerdictWasCorrect = passed;
        data.finalMissedIssueCount = issues.Count;

        return result;
    }

    private void RunAuditDialogue(SubmissionResult result)
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

        dialogueUI.StartDialogue(builder.Build(), () => OnDialogueConcluded(result));
    }

    private void OnDialogueConcluded(SubmissionResult result)
    {
        summaryPopupUI.Show(result.MissedIssues, () => OnSummaryClosed(result));
    }

    private void OnSummaryClosed(SubmissionResult result)
    {
        bool passed = result.VerdictWasCorrect && result.MissedIssues.Count == 0;

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



    public string GetPromptText() => "Click to talk to the Auditor";
}