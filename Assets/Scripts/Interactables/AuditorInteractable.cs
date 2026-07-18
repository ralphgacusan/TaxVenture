using UnityEngine;

/// <summary>
/// PURPOSE:
/// The Auditor NPC. Mirrors ClientInteractable's structure exactly (Milestone
/// 8's NpcStateMachine pattern applied to a second NPC), proving the FSM
/// template is reusable without modification.
///
/// PER DESIGN DOC:
/// "The player clicks the auditor... consultant hands over the Case
/// Folder... auditor reviews... results."
///
/// CONNECTS WITH:
/// - HighlightEffect, NpcStateMachine (same GameObject)
/// - AuditorDialogueUI: opened on interact
/// - GameStateMachine: requests transition into ComplianceAuditState if not
///   already there (e.g. arriving here straight from PrepareReturnState)
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
[RequireComponent(typeof(NpcStateMachine))]
public class AuditorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private AuditorDialogueUI auditorDialogueUI;
    [SerializeField] private Transform interviewViewpoint;

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
        if (npcState.CurrentState is NpcIdleState)
        {
            npcState.ChangeState(new NpcWaitingState());
        }
    }

    public void OnUnfocus()
    {
        highlight.Unhighlight();
        if (npcState.CurrentState is NpcWaitingState)
        {
            npcState.ChangeState(new NpcIdleState());
        }
    }

    public void OnInteract()
    {
        CaseData data = CaseManager.Instance.CurrentCase;

        // If the case was stamped Ready For Filing, the Auditor now requires
        // the player to have physically picked up the printed return before
        // the audit can begin — matches "hand over the printed BIR Form
        // instead of magically reviewing invisible paperwork."
        if (data.filingStatus == FilingStatus.ReadyForFiling && !data.isCarryingPrintedReturn)
        {
            auditorDialogueUI.ShowMissingFormWarning();
            return;
        }

        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());

        if (GameStateMachine.Instance.CurrentState is PrepareReturnState
            || GameStateMachine.Instance.CurrentState is StampAssessmentState)
        {
            GameStateMachine.Instance.ChangeState(new ComplianceAuditState());
        }

        auditorDialogueUI.Show(npcState);
    }
    public string GetPromptText() => "Click to hand over case to Auditor";
}