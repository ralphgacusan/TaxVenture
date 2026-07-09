using UnityEngine;

/// <summary>
/// PURPOSE:
/// Default NPC state — standing around, not yet focused on or interacted
/// with by the player. Matches design brief's NPC FSM "Idle" state.
///
/// TRANSITIONS TO:
/// - NpcWaitingState, when the player focuses/highlights this NPC
///   (triggered by the NPC's *Interactable script's OnFocus())
/// </summary>
public class NpcIdleState : INpcState
{
    public string StateName => "Idle";
    public void Enter() => Debug.Log("[NpcIdleState] Entered.");
    public void Exit() => Debug.Log("[NpcIdleState] Exited.");
    public void Tick() { }
}