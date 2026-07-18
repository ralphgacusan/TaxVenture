using UnityEngine;

/// <summary>
/// PURPOSE: Stub for Milestone 14. Full reward screen (EXP, Reputation,
/// mistake-based penalty math) arrives next milestone.
/// </summary>
public class RewardsState : IGameState
{
    public string StateName => "Receive Rewards";
    public void Enter() => Debug.Log("[RewardsState] Entered (stub — full logic in Milestone 14).");
    public void Exit() => Debug.Log("[RewardsState] Exited.");
    public void Tick() { }
}