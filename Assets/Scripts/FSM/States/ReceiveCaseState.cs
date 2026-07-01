using UnityEngine;

/// <summary>
/// PURPOSE:
/// First phase of the gameplay loop. Per the design document, this covers
/// the player walking to their desk and clicking the Case Folder for the
/// first time. For this milestone, entering first-person at the Desk is
/// treated as "receiving the case" — the Case Folder UI itself arrives in
/// Milestone 5.
///
/// RESPONSIBILITIES:
/// - Log entry/exit for debug visibility
/// - (Future) Enable any "new case available" UI indicator
///
/// TRANSITIONS TO:
/// - ReviewDocumentsState, once the player has interacted with the Desk
///   (this milestone: any Desk interaction triggers the transition, since
///   the Case Folder isn't opened yet — will be refined in Milestone 5)
///
/// CONNECTS WITH:
/// - GameStateMachine: Enter()/Exit()/Tick() called by the machine
/// - DeskInteractable: requests the transition to ReviewDocumentsState
/// </summary>
public class ReceiveCaseState : IGameState
{
    public string StateName => "Receive Case";

    public void Enter()
    {
        Debug.Log("[ReceiveCaseState] Entered. Objective: Obtain the client's tax case and supporting documents.");
    }

    public void Exit()
    {
        Debug.Log("[ReceiveCaseState] Exited.");
    }

    public void Tick()
    {
        // No per-frame logic needed for this state yet.
    }
}