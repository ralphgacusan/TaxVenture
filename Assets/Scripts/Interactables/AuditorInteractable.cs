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
            || GameStateMachine.Instance.CurrentState is RewardsState
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

        var builder = new DialogueBuilder("Auditor").Npc(reminder).Build();
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

        var result = new SubmissionResult
        {
            PlayerVerdict = data.caseAssessment,
            CorrectVerdict = correctVerdict,
            VerdictWasCorrect = data.caseAssessment == correctVerdict,
            MissedIssues = issues,
            FalseIssues = new List<ComplianceIssue>()
        };

        data.auditPassed = result.VerdictWasCorrect && issues.Count == 0;

        // NEW: persist for the Client outcome conversation to read later.
        data.finalVerdictWasCorrect = result.VerdictWasCorrect;
        data.finalMissedIssueCount = issues.Count;

        return result;
    }

    private void RunAuditDialogue(SubmissionResult result)
    {
        var builder = new DialogueBuilder("Auditor");

        if (result.VerdictWasCorrect && result.MissedIssues.Count == 0)
        {
            builder.Npc("I've reviewed everything, and I found no issues. Well done.");
        }
        else
        {
            if (!result.VerdictWasCorrect)
            {
                builder.Npc("Your verdict on this case does not match my findings.");
            }

            foreach (var issue in result.MissedIssues)
            {
                builder.Npc(issue.ShortLabel);
            }
        }

        builder.Npc("This concludes the final review. No further changes can be made to this case.");

        dialogueUI.StartDialogue(builder.Build(), () => OnDialogueConcluded(result));
    }

    private void OnDialogueConcluded(SubmissionResult result)
    {
        summaryPopupUI.Show(result.MissedIssues, () => OnSummaryClosed(result));
    }

    private void OnSummaryClosed(SubmissionResult result)
    {
        // Always proceeds forward now, per R11 — pass or fail, there is no
        // return path. Compare with the old NpcIdleState fallback on failure,
        // which is intentionally removed.
        npcState.ChangeState(NpcCompletedStateFor(result));

        if (GameStateMachine.Instance.CurrentState is AuditSubmittedState)
        {
            GameStateMachine.Instance.ChangeState(new CaseOutcomeState());
        }
    }

    private NpcCompletedState NpcCompletedStateFor(SubmissionResult result)
    {
        // Single Completed state regardless of pass/fail — the Auditor's
        // job is done either way once the final review has happened.
        return new NpcCompletedState();
    }

    public string GetPromptText() => "Click to talk to the Auditor";
}