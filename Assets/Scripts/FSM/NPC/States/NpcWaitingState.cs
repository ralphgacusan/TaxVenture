using UnityEngine;

/// <summary>
/// PURPOSE:
/// NPC is aware the player is looking at / near them and available to
/// interact, but the conversation hasn't started yet. Matches design
/// brief's "Waiting" state.
///
/// TRANSITIONS TO:
/// - NpcInteractState, when the player clicks the NPC
/// - NpcIdleState, if the player looks away without clicking (handled by
///   the *Interactable script's OnUnfocus())
/// </summary>
public class NpcWaitingState : INpcState
{
    public string StateName => "Waiting";
    public void Enter() => Debug.Log("[NpcWaitingState] Entered.");
    public void Exit() => Debug.Log("[NpcWaitingState] Exited.");
    public void Tick() { }
}