using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// The Receptionist NPC. Reuses the EXACT SAME presentation-line mechanism
/// added to InterviewClientUI in Milestone 14 (ShowPresentation) — no new
/// dialogue system. On conversation end, begins ReceiveCaseState and
/// raises GameplayEvents.NotesUnlockRequested so the HUD's Notes icon
/// unlocks, per Phase 1's event-driven design.
///
/// STRUCTURE MIRRORS ClientInteractable / AuditorInteractable EXACTLY:
/// - HighlightEffect for focus highlight
/// - NpcStateMachine for Idle/Waiting/Interact/Dialogue/Completed
/// - CameraController.LockPlayerControls() during the conversation
/// - A dedicated InterviewClientUI instance (own panel, own presentation
///   lines) for its dialogue — same script, separate instance, per the
///   reasoning in this phase's architecture notes.
///
/// CONNECTS WITH:
/// - HighlightEffect, NpcStateMachine (same GameObject)
/// - InterviewClientUI (Receptionist's own instance): shows presentation lines
/// - GameStateMachine: starts ReceiveCaseState once conversation concludes
/// - GameplayEvents: raises NotesUnlockRequested once conversation concludes
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
[RequireComponent(typeof(NpcStateMachine))]
public class ReceptionistInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private InterviewClientUI receptionistDialogueUI;

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
        // Once already spoken to, re-clicking the Receptionist does nothing
        // further this level — she's a one-time gate into Receive Case,
        // not a repeatable conversation.
        if (hasSpokenToReceptionist) return;

        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());

        CameraController.Instance.LockPlayerControls();

        var lines = new List<string>
        {
            "Good day!",
            "Welcome back.",
            "Today you have two scheduled client consultations.",
            "I already prepared the case folders and placed them on your desk.",
            "You may review them whenever you're ready.",
            "Good luck!"
        };

        receptionistDialogueUI.ShowPresentation(
            lines,
            OnConversationConcluded,
            false
        );
    }

    private void OnConversationConcluded()
    {
        hasSpokenToReceptionist = true;
        npcState.ChangeState(new NpcCompletedState());

        GameStateMachine.Instance.ChangeState(new ReceiveCaseState());
        GameplayEvents.RaiseNotesUnlockRequested();
    }

    public string GetPromptText()
    {
        return hasSpokenToReceptionist ? "Receptionist" : "Click to talk to Receptionist";
    }
}