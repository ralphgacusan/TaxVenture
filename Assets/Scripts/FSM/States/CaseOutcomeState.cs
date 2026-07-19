using UnityEngine;

/// <summary>
/// PURPOSE:
/// Phase covering "Meet Client for Case Outcome" — presenting the completed
/// case findings to the client, per design doc. Entered once the Compliance
/// Audit passes.
///
/// TRANSITIONS TO:
/// - ArchiveCaseState, once the presentation conversation concludes.
///
/// CONNECTS WITH:
/// - AuditorDialogueUI: requests entry into this state on audit pass
/// - ClientInteractable: requests the exit transition once presentation ends
/// </summary>
public class CaseOutcomeState : IGameState
{
    public string StateName => "Present Findings to Client";
    public void Enter() => Debug.Log("[CaseOutcomeState] Entered. Objective: return to the Conference Room and present the completed case to the client.");
    public void Exit() => Debug.Log("[CaseOutcomeState] Exited.");
    public void Tick() { }
}