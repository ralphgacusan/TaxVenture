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
        CaseData data = CaseManager.Instance.CurrentCase;

        var lines = new DialogueBuilder(data.fullName)
            .Npc("Good morning! Thank you for taking my case.")
            .Player("Of course. Let's start with a few questions.")
            .Npc("Sure, go ahead.")
            .Player("Where do you currently reside and work?")
            .Npc("I live and work here in the Philippines full-time.")
            .Player("How would you describe how you earn your income?")
            .Npc("I have a regular job, but I also run a small online business on the side.")
            .Player("Can you walk me through all your sources of income this year?")
            .Npc("I earn a salary from my employer, and additional income from my online business.")
            .Player("How many employers did you have this year?")
            .Npc("Just the one — I've been with the same company all year.")
            .Player("Is your business formally registered with the BIR?")
            .Npc("Yes, I registered it last year — I have the Certificate of Registration.")
            .Player("For your business income, are you using the graduated rates or the 8% option?")
            .Npc("I opted for the 8% flat rate — it was simpler for my situation.")
            .Player("I noticed your bank deposits seem higher than your declared sales. Can you clarify?")
            .Npc("Some of those deposits were personal transfers from my spouse, not business income.")
            .Player("Thank you, that's everything I need for now.")
            .Npc("Happy to help.")
            .Build();

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
        var builder = new DialogueBuilder(data.fullName);

        switch (branch)
        {
            case ClientOutcomeBranch.CorrectNoIssues:
                builder.Npc("I've completed the review of your tax documents.")
                       .Player("Everything checked out cleanly. Your case is fully compliant.")
                       .Npc("That's wonderful news. Thank you for handling this so carefully.")
                       .Player("Your return has been filed and the case is now closed.")
                       .Npc("I really appreciate your thoroughness.");
                break;

            case ClientOutcomeBranch.CorrectWithIssues:
                builder.Npc("I've completed the review of your tax documents.")
                       .Player("Your overall filing assessment was correct, but a few details need your attention.")
                       .Npc("Oh — what kind of details?")
                       .Player("A few figures and entries weren't fully accurate, but nothing that changes your filing status.")
                       .Npc("I see. I'll be more careful with my records next time.")
                       .Player("Your return has still been filed successfully.");
                break;

            case ClientOutcomeBranch.WrongVerdict:
                builder.Npc("I've completed the review of your tax documents.")
                       .Player("Unfortunately, the case assessment I gave you was incorrect.")
                       .Npc("What does that mean for me?")
                       .Player("It means this case could not be properly finalized as filed.")
                       .Npc("That's disappointing to hear.");
                break;

            case ClientOutcomeBranch.WrongVerdictWithIssues:
                builder.Npc("I've completed the review of your tax documents.")
                       .Player("I have to be honest — the assessment was incorrect, and several details in the filing were wrong as well.")
                       .Npc("That's... not what I was hoping to hear.")
                       .Player("I understand. I take full responsibility for these errors.")
                       .Npc("I'll need to find someone else to help me sort this out.");
                break;
        }

        builder.Player("This concludes your consultation.");
        return builder.Build();
    }

    private void OnPresentationConcluded(ClientOutcomeBranch branch)
    {
        CaseManager.Instance.CurrentCase.clientPresentationCompleted = true;
        npcState.ChangeState(new NpcCompletedState());

        if (GameStateMachine.Instance.CurrentState is CaseOutcomeState)
        {
            LevelResultPopupUI.Instance.Show(branch, OnLevelResultClosed);
        }
    }

    private void OnLevelResultClosed()
    {
        GameStateMachine.Instance.ChangeState(new ArchiveCaseState());
    }

    private void OnPresentationConcluded()
    {
        CaseManager.Instance.CurrentCase.clientPresentationCompleted = true;
        npcState.ChangeState(new NpcCompletedState());

        if (GameStateMachine.Instance.CurrentState is CaseOutcomeState)
        {
            GameStateMachine.Instance.ChangeState(new ArchiveCaseState());
        }
    }

    public string GetPromptText()
    {
        if (GameStateMachine.Instance.CurrentState is CaseOutcomeState) return "Click to present findings to Client";
        if (hasInterviewed) return "Client";
        return "Click to talk to Client";
    }
}