using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(HighlightEffect))]
[RequireComponent(typeof(NpcStateMachine))]
public class ClientInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueUI dialogueUI; // CHANGED: was InterviewClientUI

    private HighlightEffect highlight;
    private NpcStateMachine npcState;
    private bool hasInterviewed = false;

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
        if (GameStateMachine.Instance.CurrentState is CaseOutcomeState)
        {
            PresentFindings();
            return;
        }

        if (hasInterviewed) return; // interview only happens once, matching the scripted-conversation model

        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());

        RunInterview();
    }

    /// <summary>
    /// Fixed sequential interview, per R4's "no dialogue choices" rule.
    /// Each exchange still writes into CaseData exactly like the old
    /// question-button system did — just without the player picking order.
    /// </summary>
    private void RunInterview()
    {
        CaseDefinition def = CaseManager.Instance.CurrentDefinition;
        var lines = CaseFactory.BuildDialogue(def.interviewLines, CaseManager.Instance.CurrentCase.fullName);

        dialogueUI.StartDialogue(lines, OnInterviewConcluded);

        if (GameStateMachine.Instance.CurrentState is ReviewDocumentsState)
        {
            GameStateMachine.Instance.ChangeState(new InterviewClientState());
        }
    }
    private void OnInterviewConcluded()
    {
        hasInterviewed = true;
        npcState.ChangeState(new NpcCompletedState());

        if (TutorialController.Instance != null)
            TutorialController.Instance.ReportInteraction("interview_client");
    }

    private void PresentFindings()
    {
        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());

        CaseData data = CaseManager.Instance.CurrentCase;
        ClientOutcomeBranch branch = ClientOutcomeEvaluator.Determine(data);

        var lines = BuildOutcomeDialogue(branch);
        dialogueUI.StartDialogue(lines, () => OnPresentationConcluded(branch));
    }

    private List<DialogueLine> BuildOutcomeDialogue(ClientOutcomeBranch branch)
    {
        CaseData data = CaseManager.Instance.CurrentCase;
        CaseDefinition def = CaseManager.Instance.CurrentDefinition;

        List<DialogueLineDefinition> jsonLines = branch switch
        {
            ClientOutcomeBranch.CorrectNoIssues => def.outcomeCorrectNoIssues,
            ClientOutcomeBranch.CorrectWithIssues => def.outcomeCorrectWithIssues,
            ClientOutcomeBranch.WrongVerdict => def.outcomeWrongVerdict,
            _ => def.outcomeWrongVerdictWithIssues
        };

        return CaseFactory.BuildDialogue(jsonLines, data.fullName);
    }

    private void OnPresentationConcluded(ClientOutcomeBranch branch)
    {
        CaseManager.Instance.CurrentCase.clientPresentationCompleted = true;
        npcState.ChangeState(new NpcCompletedState());

        if (TutorialController.Instance != null)
            TutorialController.Instance.ReportInteraction("outcome_presented");

        if (!(GameStateMachine.Instance.CurrentState is CaseOutcomeState)) return;

        if (branch == ClientOutcomeBranch.CorrectNoIssues)
        {
            // Perfect case: skip the popup here entirely, go straight to
            // archiving. Level Result shows AFTER the archive confirmation
            // is closed instead — see FilingCabinetInteractable.
            GameStateMachine.Instance.ChangeState(new ArchiveCaseState());
        }
        else
        {
            // All other branches: show Level Result immediately, as before.
            LevelResultPopupUI.Instance.Show(CaseManager.Instance.CurrentCase, OnLevelResultClosed);
        }
    }


    private void OnLevelResultClosed()
    {
        GameStateMachine.Instance.ChangeState(new ArchiveCaseState());
    }

    public string GetPromptText()
    {
        if (GameStateMachine.Instance.CurrentState is CaseOutcomeState) return "Click to present findings to Client";
        if (hasInterviewed) return "Client";
        return "Click to talk to Client";
    }
}