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

        var lines = new DialogueBuilder()
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

        // Apply the same CaseData writes the old InterviewQuestion system
        // performed — done immediately, matching a scripted conversation
        // where the "answers" are fixed and always given in full.
        data.residencyStatus = ResidencyStatus.ResidentCitizen;
        data.taxpayerType = TaxpayerType.MixedIncomeEarner;
        data.incomeSource = IncomeSource.MixedIncome;
        data.numberOfEmployers = EmployerCount.OneEmployer;
        data.businessRegistration = BusinessRegistration.Registered;
        data.taxOption = TaxOption.EightPercentTaxRate;

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

        var lines = new DialogueBuilder()
            .Npc("I've completed the review of your tax documents.")
            .Player("Your tax return has been prepared and reviewed.")
            .Player("The compliance audit has also been completed.")
            .Player("Everything is now ready for filing.")
            .Npc("Thank you for handling my case.")
            .Npc("I appreciate your assistance.")
            .Player("This concludes your consultation.")
            .Build();

        dialogueUI.StartDialogue(lines, OnPresentationConcluded);
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