using UnityEngine;

/// <summary>
/// PURPOSE:
/// Phase covering Analyze Evidence — reviewing documents at the Corkboard
/// and making the Ready/Not-Ready assessment decision. Entered when the
/// Corkboard is opened.
///
/// TRANSITIONS TO:
/// - StampAssessmentState, when an assessment card is clicked at the board
///   (already wired via AssessmentCardInteractable's existing guard).
/// </summary>
public class AnalyzeEvidenceState : IGameState
{
    public string StateName => "Analyze Evidence";
    public void Enter() => Debug.Log("[AnalyzeEvidenceState] Entered. Objective: determine whether the case is ready for filing.");
    public void Exit() => Debug.Log("[AnalyzeEvidenceState] Exited.");
    public void Tick() { }
}