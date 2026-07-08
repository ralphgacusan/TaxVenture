using UnityEngine;

/// <summary>
/// PURPOSE:
/// Third phase of the gameplay loop. Per the design doc, this covers the
/// interview: verifying information, gathering missing details, clarifying
/// discrepancies with the client.
///
/// TRANSITIONS TO:
/// - ResearchTaxState (Milestone 9), once the interview is complete and the
///   player returns to the desk / opens the Tax Code Book. Not yet
///   implemented — this state currently has no exit trigger, matching the
///   pattern established by ReviewDocumentsState in Milestone 4.
///
/// CONNECTS WITH:
/// - GameStateMachine: Enter()/Exit()/Tick() called by the machine
/// - ClientInteractable: requests the transition into this state
/// </summary>
public class InterviewClientState : IGameState
{
    public string StateName => "Interview Client";

    public void Enter()
    {
        Debug.Log("[InterviewClientState] Entered. Objective: Verify information, gather missing details, and clarify discrepancies.");
    }

    public void Exit()
    {
        Debug.Log("[InterviewClientState] Exited.");
    }

    public void Tick()
    {
        // No per-frame logic needed for this state yet.
    }
}