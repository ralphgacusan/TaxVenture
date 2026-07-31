using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(HighlightEffect))]
[RequireComponent(typeof(NpcStateMachine))]
public class AuditorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private AuditSummaryPopupUI summaryPopupUI; // kept as its own script

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

    public void OnInteract()
    {
        CaseData data = CaseManager.Instance.CurrentCase;

        if (data.filingStatus == FilingStatus.ReadyForFiling && !data.isCarryingPrintedReturn)
        {
            // TODO: small floating warning text, unchanged from before
            return;
        }

        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());

        if (GameStateMachine.Instance.CurrentState is PrepareReturnState
            || GameStateMachine.Instance.CurrentState is StampAssessmentState)
        {
            GameStateMachine.Instance.ChangeState(new ComplianceAuditState());
        }

        RunAudit(data);
    }

    private void RunAudit(CaseData data)
    {
        List<ComplianceIssue> issues = ComplianceChecker.RunCheck(data);
        data.auditMistakeCount = issues.Count;
        data.auditPassed = issues.Count == 0;

        var builder = new DialogueBuilder();

        if (issues.Count == 0)
        {
            builder.Npc("I've reviewed everything, and I found no issues. Well done.");
        }
        else
        {
            foreach (var issue in issues)
            {
                builder.Npc(issue.ShortLabel);
            }
        }
        builder.Npc("That concludes my review. Please check the audit summary before making your corrections.");

        dialogueUI.StartDialogue(builder.Build(), () => OnDialogueConcluded(issues));
    }

    private void OnDialogueConcluded(List<ComplianceIssue> issues)
    {
        summaryPopupUI.Show(issues, OnSummaryClosed);
    }

    private void OnSummaryClosed()
    {
        // Player controls already unlocked by DialogueUI.ConcludeDialogue();
        // the summary popup is a separate, non-locking popup layered after.
        CaseData data = CaseManager.Instance.CurrentCase;

        if (data.auditPassed)
        {
            npcState.ChangeState(new NpcCompletedState());
            if (GameStateMachine.Instance.CurrentState is ComplianceAuditState)
            {
                GameStateMachine.Instance.ChangeState(new CaseOutcomeState());
            }
        }
        else
        {
            npcState.ChangeState(new NpcIdleState());
        }
    }

    public string GetPromptText() => "Click to hand over case to Auditor";
}