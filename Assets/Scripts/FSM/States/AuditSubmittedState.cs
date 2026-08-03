using UnityEngine;

/// <summary>
/// PURPOSE:
/// Entered the moment the player submits documents to the Auditor for
/// final review. Locks the FSM — no further backward navigation is
/// possible from this point on, per R11's "no corrections allowed" rule.
/// </summary>
public class AuditSubmittedState : IGameState
{
    public string StateName => "Case Submitted for Audit";
    public void Enter()
    {
        Debug.Log("[AuditSubmittedState] Entered. Case has been submitted — no further changes possible.");
        GameStateMachine.Instance.LockProgression();
    }
    public void Exit() => Debug.Log("[AuditSubmittedState] Exited.");
    public void Tick() { }
}