using UnityEngine;

/// <summary>
/// PURPOSE:
/// Phase covering Compliance Audit. Per the design doc: "Have the case
/// reviewed by the auditor." Entered either from StampAssessmentState
/// (Not Ready path) or PrepareReturnState (Ready path) — both branches from
/// Milestone 12 converge here, matching your gameplay loop diagram where
/// Compliance Audit is a single shared step regardless of prior branch.
///
/// TRANSITIONS TO:
/// - RewardsState (Milestone 14), once AuditorDialogueUI reports a pass.
///
/// CONNECTS WITH:
/// - AuditorInteractable: requests entry into this state
/// - AuditorDialogueUI: requests the exit transition on pass
/// </summary>
public class ComplianceAuditState : IGameState
{
    public string StateName => "Compliance Audit";
    public void Enter() => Debug.Log("[ComplianceAuditState] Entered. Objective: Have the case reviewed by the auditor.");
    public void Exit() => Debug.Log("[ComplianceAuditState] Exited.");
    public void Tick() { }
}