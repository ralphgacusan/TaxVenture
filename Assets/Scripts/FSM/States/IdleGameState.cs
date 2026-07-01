using UnityEngine;

/// <summary>
/// PURPOSE:
/// Neutral "nothing is happening yet" state. Used as a safe default before
/// the first real case is loaded (e.g. while a main menu or loading screen
/// is showing), so GameStateMachine.CurrentState is never null.
///
/// TRANSITIONS TO:
/// - ReceiveCaseState, once the player starts/loads a case.
///
/// CONNECTS WITH:
/// - GameStateMachine: can be set as the machine's very first state instead
///   of jumping straight to ReceiveCaseState, if you want an explicit
///   "waiting to start" moment (e.g. main menu).
/// </summary>
public class IdleGameState : IGameState
{
    public string StateName => "Idle";

    public void Enter()
    {
        Debug.Log("[IdleGameState] Entered. Waiting for case to start.");
    }

    public void Exit()
    {
        Debug.Log("[IdleGameState] Exited.");
    }

    public void Tick()
    {
        // Intentionally empty — nothing happens while idle.
    }
}