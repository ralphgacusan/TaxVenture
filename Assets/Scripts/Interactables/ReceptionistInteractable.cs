using UnityEngine;

[RequireComponent(typeof(HighlightEffect))]
[RequireComponent(typeof(NpcStateMachine))]
public class ReceptionistInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueUI dialogueUI; // CHANGED: was InterviewClientUI

    private HighlightEffect highlight;
    private NpcStateMachine npcState;
    private bool hasSpokenToReceptionist = false;

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
        if (hasSpokenToReceptionist) return;

        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());

        var lines = new DialogueBuilder()
            .Npc("Good day!")
            .Npc("Welcome back.")
            .Npc("Today you have two scheduled client consultations.")
            .Npc("I already prepared the case folders and placed them on your desk.")
            .Npc("You may review them whenever you're ready.")
            .Npc("Good luck!")
            .Build();

        dialogueUI.StartDialogue(lines, OnConversationConcluded);
    }

    private void OnConversationConcluded()
    {
        hasSpokenToReceptionist = true;
        npcState.ChangeState(new NpcCompletedState());

        GameStateMachine.Instance.ChangeState(new ReceiveCaseState());
        GameplayEvents.RaiseNotesUnlockRequested();
    }

    public string GetPromptText() => hasSpokenToReceptionist ? "Receptionist" : "Click to talk to Receptionist";
}