using UnityEngine;

/// <summary>
/// PURPOSE:
/// Full milestone covering Prepare Tax Return / Case Report Encoding, only
/// reached if stamped Ready For Filing. Covers: Computer home screen ->
/// Prepare Tax Return app -> form selection -> manual encoding -> confirm
/// -> print -> pick up printed form -> walk to Auditor.
///
/// TRANSITIONS TO:
/// - ComplianceAuditState, once the player has physically picked up the
///   printed form AND interacts with the Auditor (see AuditorInteractable).
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