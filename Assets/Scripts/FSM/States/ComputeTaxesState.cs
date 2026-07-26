using UnityEngine;

/// <summary>
/// PURPOSE:
/// Phase covering Compute Taxes — using the Computer's Tax Calculator.
/// Entered when the player opens Calculate Taxes from the Computer home screen.
///
/// TRANSITIONS TO:
/// - AnalyzeEvidenceState, when the Corkboard is opened.
/// </summary>
public class ComputeTaxesState : IGameState
{
    public string StateName => "Compute Taxes";
    public void Enter() => Debug.Log("[ComputeTaxesState] Entered. Objective: compute tax liabilities and validate calculations.");
    public void Exit() => Debug.Log("[ComputeTaxesState] Exited.");
    public void Tick() { }
}