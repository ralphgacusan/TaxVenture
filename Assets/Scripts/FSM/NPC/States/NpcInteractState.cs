using UnityEngine;

/// <summary>
/// PURPOSE:
/// Brief transitional state the moment the player clicks the NPC, before the
/// dialogue panel is actually up. Matches design brief's "Interact" state.
/// Kept separate from NpcDialogueState so future NPCs could add a short
/// "turning to face the player" behavior here without touching dialogue logic.
///
/// TRANSITIONS TO:
/// - NpcDialogueState, immediately after entering (this milestone has no
///   animation/delay to wait on, so the transition is instant — but the
///   state still exists as its own step for future expansion).
/// </summary>
public class NpcInteractState : INpcState
{
    public string StateName => "Interact";
    public void Enter() => Debug.Log("[NpcInteractState] Entered.");
    public void Exit() => Debug.Log("[NpcInteractState] Exited.");
    public void Tick() { }
}