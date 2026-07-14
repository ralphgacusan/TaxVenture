using UnityEngine;

/// <summary>
/// PURPOSE:
/// Phase covering the Stamp Case Assessment step. Per the design doc:
/// "Apply the final case assessment." Entered once the player has completed
/// Compute Taxes (Milestone 10) and Analyze Evidence (Milestone 11), and
/// begins interacting with the Stamp Set on the Desk.
///
/// TRANSITIONS TO:
/// - PrepareReturnState, if stamped Ready For Filing (per design doc's
///   conditional: "If Ready for Filing: Tax Return Preparation / Case
///   Report Encoding")
/// - ComplianceAuditState (Milestone 13), if stamped Not Ready For Filing —
///   per your gameplay loop, Compliance Audit still runs regardless of
///   filing readiness; it's the auditor who catches whether the consultant's
///   assessment was actually correct.
///
/// CONNECTS WITH:
/// - StampSetInteractable / StampUI: requests transitions once stamping completes
/// </summary>
public class StampAssessmentState : IGameState
{
    public string StateName => "Stamp Case Assessment";

    public void Enter()
    {
        Debug.Log("[StampAssessmentState] Entered. Objective: Apply the final case assessment.");
    }

    public void Exit()
    {
        Debug.Log("[StampAssessmentState] Exited.");
    }

    public void Tick() { }
}