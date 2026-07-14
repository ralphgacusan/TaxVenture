using UnityEngine;

/// <summary>
/// PURPOSE: Stub for Milestone 13. Full implementation (Auditor NPC
/// interaction, mistake penalties) arrives next milestone.
/// </summary>
public class ComplianceAuditState : IGameState
{
    public string StateName => "Compliance Audit";
    public void Enter() => Debug.Log("[ComplianceAuditState] Entered (stub — full logic in Milestone 13).");
    public void Exit() => Debug.Log("[ComplianceAuditState] Exited.");
    public void Tick() { }
}