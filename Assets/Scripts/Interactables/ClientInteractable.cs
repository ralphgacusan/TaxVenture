using UnityEngine;

/// <summary>
/// PURPOSE:
/// The Client NPC placeholder (capsule) in the conference room. Interacting
/// with it opens the interview dialogue and, if the game is currently in
/// ReviewDocumentsState, advances the FSM to InterviewClientState.
///
/// PER DESIGN DOC:
/// "The player approaches the client... clicks the client... interview
/// scene begins." We simplify the sitting animation and camera panel setup
/// described in the doc into an instant dialogue panel appearing, consistent
/// with the no-animation greybox scope of this phase.
///
/// FUTURE (Milestone 8):
/// This script's responsibilities will likely be absorbed into a proper NPC
/// FSM (Idle -> Waiting -> Interact -> Dialogue -> Completed). For now, it's
/// a simple, testable IInteractable exactly like Desk/CaseFolder.
///
/// CONNECTS WITH:
/// - HighlightEffect (same GameObject)
/// - InterviewClientUI: calls Show() on interact
/// - GameStateMachine: requests ReviewDocuments -> InterviewClient transition
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
[RequireComponent(typeof(NpcStateMachine))]
public class ClientInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private InterviewClientUI interviewClientUI;
    [SerializeField] private Transform interviewViewpoint; // camera position/rotation for "sitting across the table"

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
        // Only fall back to Idle if we were merely Waiting (looked at but
        // not clicked). Do NOT interrupt an active Dialogue just because
        // the player's cursor drifted off the NPC mid-conversation.
        if (npcState.CurrentState is NpcWaitingState)
        {
            npcState.ChangeState(new NpcIdleState());
        }
    }

    public void OnInteract()
    {
        npcState.ChangeState(new NpcInteractState());
        npcState.ChangeState(new NpcDialogueState());

        CameraController.Instance.LockPlayerControls(); // NEW

        interviewClientUI.Show();


        if (GameStateMachine.Instance.CurrentState is ReviewDocumentsState)
        {
            GameStateMachine.Instance.ChangeState(new InterviewClientState());
        }

    }

    public string GetPromptText() => "Click to talk to Client";


}