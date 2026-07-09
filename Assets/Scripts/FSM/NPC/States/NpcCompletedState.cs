using UnityEngine;

/// <summary>
/// PURPOSE:
/// NPC's interaction for this phase is done (interview finished). Matches
/// design brief's "Completed" state. An NPC in this state can still be
/// clicked again later (e.g. "Meet Client for Case Outcome" phase reuses
/// the same Client NPC) — that later milestone will decide what clicking a
/// Completed NPC does; for now it's a terminal state with no further
/// scripted transition.
/// </summary>
public class NpcCompletedState : INpcState
{
    public string StateName => "Completed";
    public void Enter() => Debug.Log("[NpcCompletedState] Entered.");
    public void Exit() => Debug.Log("[NpcCompletedState] Exited.");
    public void Tick() { }
}