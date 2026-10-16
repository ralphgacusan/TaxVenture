using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(NpcStatusMarker))]
[RequireComponent(typeof(NpcStateMachine))]
public class ClientInteractable : MonoBehaviour, IInteractable, INpcAvailabilitySource
{
    [SerializeField] private DialogueUI dialogueUI; // CHANGED: was InterviewClientUI

    private NpcStateMachine npcState;
    private bool hasInterviewed = false;
    private bool dialogueSessionActive;

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

    public void OnInteract()
    {
        if (dialogueSessionActive || dialogueUI == null || npcState == null) return;

        if (GameStateMachine.Instance.CurrentState is CaseOutcomeState)
        {
            if (CaseManager.Instance == null ||
                CaseManager.Instance.CurrentCase == null ||
                CaseManager.Instance.CurrentCase.clientPresentationCompleted) return;

            PresentFindings();
            return;
        }

        if (hasInterviewed) return; // interview only happens once, matching the scripted-conversation model

        RunInterview();
    }

    /// <summary>
    /// Fixed sequential interview, per R4's "no dialogue choices" rule.
    /// Each exchange still writes into CaseData exactly like the old
    /// question-button system did — just without the player picking order.
    /// </summary>
    private void RunInterview()
    {
        if (CaseManager.Instance == null ||
            CaseManager.Instance.CurrentDefinition == null ||
            CaseManager.Instance.CurrentCase == null) return;

        CaseDefinition def = CaseManager.Instance.CurrentDefinition;
        List<DialogueLine> lines = CaseFactory.BuildDialogue(
            def.interviewLines,
            CaseManager.Instance.CurrentCase.fullName
        );

        GameStateMachine gameStateMachine = GameStateMachine.Instance;
        IGameState priorGameState = gameStateMachine != null
            ? gameStateMachine.CurrentState
            : null;
        INpcState priorNpcState = npcState.CurrentState;
        bool shouldEnterInterviewState = priorGameState is ReviewDocumentsState;
        IGameState enteredGameState = null;

        if (!dialogueUI.TryStartDialogue(
            lines,
            OnInterviewConcluded,
            () => OnInterviewCancelled(
                priorNpcState,
                gameStateMachine,
                priorGameState,
                enteredGameState
            ),
            this)) return;

        dialogueSessionActive = true;
        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());

        if (shouldEnterInterviewState && gameStateMachine != null)
        {
            enteredGameState = new InterviewClientState();
            gameStateMachine.ChangeState(enteredGameState);
        }
    }
    private void OnInterviewConcluded()
    {
        dialogueSessionActive = false;
        hasInterviewed = true;
        npcState.ChangeState(new NpcCompletedState());

        if (TutorialController.Instance != null)
            TutorialController.Instance.ReportInteraction("interview_client");
    }

    private void OnInterviewCancelled(
        INpcState priorNpcState,
        GameStateMachine gameStateMachine,
        IGameState priorGameState,
        IGameState enteredGameState)
    {
        dialogueSessionActive = false;
        RestoreNpcState(priorNpcState);

        if (gameStateMachine != null &&
            enteredGameState != null &&
            System.Object.ReferenceEquals(
                gameStateMachine.CurrentState,
                enteredGameState
            ))
        {
            gameStateMachine.ChangeState(priorGameState);
        }
    }

    private void PresentFindings()
    {
        if (dialogueSessionActive || CaseManager.Instance == null) return;
        CaseData data = CaseManager.Instance.CurrentCase;
        if (data == null || data.clientPresentationCompleted) return;

        ClientOutcomeBranch branch = ClientOutcomeEvaluator.Determine(data);

        var lines = BuildOutcomeDialogue(branch);
        INpcState priorNpcState = npcState.CurrentState;

        if (!dialogueUI.TryStartDialogue(
            lines,
            () => OnPresentationConcluded(branch),
            () => OnPresentationCancelled(priorNpcState),
            this)) return;

        dialogueSessionActive = true;
        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());
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
        dialogueSessionActive = false;

        CaseData data = CaseManager.Instance != null
            ? CaseManager.Instance.CurrentCase
            : null;
        if (data == null || data.clientPresentationCompleted) return;

        data.clientPresentationCompleted = true;
        npcState.ChangeState(new NpcCompletedState());

        if (TutorialController.Instance != null)
            TutorialController.Instance.ReportInteraction("outcome_presented");

        if (!(GameStateMachine.Instance is CaseOutcomeState) &&
            !(GameStateMachine.Instance.CurrentState is CaseOutcomeState)) return;

        // Calculate and store the reward exactly ONCE, regardless of branch.
        LevelResultData caseResult =
            CaseProgressionManager.Instance.CalculateAndStoreCaseReward(data);

        if (branch == ClientOutcomeBranch.CorrectNoIssues)
        {
            // Perfect case: reward is now stored, but we skip the popup
            // and go straight to archiving, as before.
            GameStateMachine.Instance.ChangeState(new ArchiveCaseState());
        }
        else
        {
            // All other branches: show the per-case result popup.
            CaseResultPopupUI.Instance.Show(
                caseResult,
                CaseProgressionManager.Instance.GetCurrentCaseFormattedTime(),
                OnCaseResultClosed
            );
        }
    }

    private void OnPresentationCancelled(INpcState priorNpcState)
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

    private void OnCaseResultClosed()
    {
        GameStateMachine.Instance.ChangeState(new ArchiveCaseState());
    }


    private void OnLevelResultClosed()
    {
        GameStateMachine.Instance.ChangeState(new ArchiveCaseState());
    }

    /// <summary>
    /// T04 marker state: interview once, then available again for the
    /// findings presentation in the case-outcome phase.
    /// </summary>
    public NpcAvailability Availability
    {
        get
        {
            CaseManager cases = CaseManager.Instance;
            bool caseLoaded = cases != null && cases.CurrentDefinition != null && cases.CurrentCase != null;
            bool outcome = GameStateMachine.Instance != null && GameStateMachine.Instance.CurrentState is CaseOutcomeState;
            return NpcAvailabilityRules.Client(caseLoaded, outcome,
                caseLoaded && cases.CurrentCase.clientPresentationCompleted, hasInterviewed);
        }
    }

    public string GetPromptText()
    {
        if (GameStateMachine.Instance.CurrentState is CaseOutcomeState)
        {
            if (CaseManager.Instance != null &&
                CaseManager.Instance.CurrentCase != null &&
                CaseManager.Instance.CurrentCase.clientPresentationCompleted)
            {
                return "Client";
            }

            return "Click to present findings to Client";
        }

        if (hasInterviewed) return "Client";
        return "Click to talk to Client";
    }
}
