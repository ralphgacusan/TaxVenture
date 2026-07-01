using UnityEngine;

/// <summary>
/// PURPOSE:
/// Second phase of the gameplay loop. Per the design document, this covers
/// opening the Case Folder and reviewing its pages/documents for
/// inconsistencies. Full folder UI arrives in Milestone 5 — this is
/// currently a stub proving the FSM transition works end-to-end.
///
/// TRANSITIONS TO:
/// - InterviewClientState (Milestone 7), once folder review is complete.
///   Not yet implemented — this state currently has no exit trigger.
///
/// CONNECTS WITH:
/// - GameStateMachine: Enter()/Exit()/Tick() called by the machine
/// </summary>
public class ReviewDocumentsState : IGameState
{
    public string StateName => "Review Documents";

    public void Enter()
    {
        Debug.Log("[ReviewDocumentsState] Entered. Objective: Identify inconsistencies, missing information, and areas requiring further investigation.");
    }

    public void Exit()
    {
        Debug.Log("[ReviewDocumentsState] Exited.");
    }

    public void Tick()
    {
        // No per-frame logic needed for this state yet.
    }
}