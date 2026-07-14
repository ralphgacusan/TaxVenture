using UnityEngine;

/// <summary>
/// PURPOSE:
/// Phase covering Tax Return Preparation / Case Report Encoding — only
/// reached if the case was stamped Ready For Filing. Per your design doc,
/// this is where the Filing sub-tab of the Computer (already built in
/// Milestone 10 Part 2's OnFileFormPressed) gets used in earnest.
///
/// TRANSITIONS TO:
/// - ComplianceAuditState (Milestone 13)
///
/// CONNECTS WITH:
/// - StampUI: requests this transition after a successful Ready-for-Filing stamp
/// </summary>
public class PrepareReturnState : IGameState
{
    public string StateName => "Prepare Return";

    public void Enter()
    {
        Debug.Log("[PrepareReturnState] Entered. Objective: Complete tax return preparation / case report encoding.");
    }

    public void Exit()
    {
        Debug.Log("[PrepareReturnState] Exited.");
    }

    public void Tick() { }
}