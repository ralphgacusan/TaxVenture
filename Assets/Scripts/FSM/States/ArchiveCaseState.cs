using UnityEngine;

/// <summary>
/// PURPOSE:
/// Phase covering "Archive Case Files" — filing the completed Case Folder
/// into storage. Entered once the client presentation concludes.
///
/// TRANSITIONS TO:
/// - RewardsState, once the Case Folder has been successfully filed.
///
/// CONNECTS WITH:
/// - ClientInteractable: requests entry into this state
/// - FilingCabinetInteractable: requests the exit transition on successful filing
/// </summary>
public class ArchiveCaseState : IGameState
{
    public string StateName => "Archive Case";
    public void Enter() => Debug.Log("[ArchiveCaseState] Entered. Objective: travel to the File Storage Room and archive the completed case.");
    public void Exit() => Debug.Log("[ArchiveCaseState] Exited.");
    public void Tick() { }
}