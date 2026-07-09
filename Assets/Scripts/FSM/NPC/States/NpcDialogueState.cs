using UnityEngine;

/// <summary>
/// PURPOSE:
/// NPC is actively in conversation with the player (interview dialogue UI
/// is open). Matches design brief's "Dialogue" state.
///
/// TRANSITIONS TO:
/// - NpcCompletedState, when the player clicks Finish Interview.
/// </summary>
public class NpcDialogueState : INpcState
{
    public string StateName => "Dialogue";
    public void Enter() => Debug.Log("[NpcDialogueState] Entered.");
    public void Exit() => Debug.Log("[NpcDialogueState] Exited.");
    public void Tick() { }
}