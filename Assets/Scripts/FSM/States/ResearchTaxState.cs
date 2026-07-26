using UnityEngine;

/// <summary>
/// PURPOSE:
/// Phase covering Research Tax Code — the player consulting the Tax Code
/// Book at the desk. Entered when the Tax Code Book is opened, if the
/// player is currently in a state where this is a meaningful progression
/// step (see TaxCodeBookInteractable's guard).
///
/// TRANSITIONS TO:
/// - ComputeTaxesState, when the Computer's Calculate Taxes is opened.
/// </summary>
public class ResearchTaxState : IGameState
{
    public string StateName => "Research Tax Code";
    public void Enter() => Debug.Log("[ResearchTaxState] Entered. Objective: determine applicable tax laws and regulations.");
    public void Exit() => Debug.Log("[ResearchTaxState] Exited.");
    public void Tick() { }
}