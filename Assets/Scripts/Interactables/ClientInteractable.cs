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
            .Npc("Good morning! Thank you for taking my case.", "Client1_Marie_Default")
            .Player("Of course. Let's start with a few questions.", "Auditor_Happy")
            .Npc("Sure, go ahead.", "Client1_Marie_Default")
            .Player("Where do you currently reside and work?", "Auditor_Happy")
            .Npc("I live and work here in the Philippines full-time.", "Client1_Marie_Default")
            .Player("How would you describe how you earn your income?", "Auditor_Happy")
            .Npc("I have a regular job, but I also run a small online business on the side.", "Client1_Marie_Default")
            .Player("Can you walk me through all your sources of income this year?", "Auditor_Happy")
            .Npc("I earn a salary from my employer, and additional income from my online business.", "Client1_Marie_Default")
            .Player("How many employers did you have this year?", "Auditor_Happy")
            .Npc("Just the one — I've been with the same company all year.", "Client1_Marie_Default")
            .Player("Is your business formally registered with the BIR?", "Auditor_Happy")
            .Npc("Yes, I registered it last year — I have the Certificate of Registration.", "Client1_Marie_Default")
            .Player("For your business income, are you using the graduated rates or the 8% option?", "Auditor_Happy")
            .Npc("I opted for the 8% flat rate — it was simpler for my situation.", "Client1_Marie_Default")
            .Player("I noticed your bank deposits seem higher than your declared sales. Can you clarify?", "Auditor_Happy")
            .Npc("Some of those deposits were personal transfers from my spouse, not business income.", "Client1_Marie_Default")
            .Player("Thank you, that's everything I need for now.", "Auditor_Happy")
            .Npc("Happy to help.", "Client1_Marie_Happy")
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
                builder.Npc("I've completed the review of your tax documents.", "Client1_Marie_Default")
                        .Player("Everything checked out cleanly. Your case is fully compliant.", "Auditor_Happy")
                        .Npc("That's wonderful news. Thank you for handling this so carefully.", "Client1_Marie_Happy")
                        .Player("Your return has been filed and the case is now closed.", "Auditor_Happy")
                        .Npc("I really appreciate your thoroughness.", "Client1_Marie_Happy");
                break;

            case ClientOutcomeBranch.CorrectWithIssues:
                builder.Npc("I've completed the review of your tax documents.", "Client1_Marie_Default")
                       .Player("Your overall filing assessment was correct, but a few details need your attention.", "Auditor_Happy")
                       .Npc("Oh — what kind of details?", "Client1_Marie_Default")
                       .Player("A few figures and entries weren't fully accurate, but nothing that changes your filing status.", "Auditor_Happy")
                       .Npc("I see. I'll be more careful with my records next time.", "Client1_Marie_Default")
                       .Player("Your return has still been filed successfully.", "Auditor_Happy");
                break;

            case ClientOutcomeBranch.WrongVerdict:
                builder.Npc("I've completed the review of your tax documents.", "Client1_Marie_Default")
                       .Player("Unfortunately, the case assessment I gave you was incorrect.", "Auditor_Happy")
                       .Npc("What does that mean for me?", "Client1_Marie_Sad")
                       .Player("It means this case could not be properly finalized as filed.", "Auditor_Happy")
                       .Npc("That's disappointing to hear.", "Client1_Marie_Sad");
                break;

            case ClientOutcomeBranch.WrongVerdictWithIssues:
                builder.Npc("I've completed the review of your tax documents.", "Client1_Marie_Default")
                       .Player("I have to be honest — the assessment was incorrect, and several details in the filing were wrong as well.", "Auditor_Happy")
                       .Npc("That's... not what I was hoping to hear.", "Client1_Marie_Sad")
                       .Player("I understand. I take full responsibility for these errors.", "Auditor_Happy")
                       .Npc("I'll need to find someone else to help me sort this out.", "Client1_Marie_Sad");
                break;
        }

        builder.Player(
            "This concludes your consultation.",
            "Auditor_Happy");

        return builder.Build();
    }

    private void OnPresentationConcluded(ClientOutcomeBranch branch)
    {
        CaseManager.Instance.CurrentCase.clientPresentationCompleted = true;
        npcState.ChangeState(new NpcCompletedState());

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