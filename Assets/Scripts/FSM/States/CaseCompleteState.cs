
using UnityEngine;

public class CaseCompleteState : IGameState
{
    public string StateName => "Case Complete";

    public void Enter()
    {
        Debug.Log("========== CASE COMPLETE REACHED ==========");
        Debug.Log($"[CaseCompleteState] CaseManager.Instance == null: {CaseManager.Instance == null}");
        Debug.Log($"[CaseCompleteState] Current Case: {CaseManager.Instance?.CurrentCase?.caseNumber}");
        Debug.Log($"[CaseCompleteState] Current Definition: {CaseManager.Instance?.CurrentDefinition?.caseId}");
        Debug.Log($"[CaseCompleteState] Progression Manager == null: {CaseProgressionManager.Instance == null}");

        // Tell HUD systems that the current case is officially complete.
        // The Case Folder icon will immediately become locked.
        GameplayEvents.RaiseCaseCompleted();

        if (CaseProgressionManager.Instance == null)
        {
            Debug.LogError("[CaseCompleteState] CaseProgressionManager INSTANCE IS NULL!");
            return;
        }

        CaseProgressionManager.Instance.OnCaseFinished();
    }

    public void Exit()
    {
        Debug.Log("[CaseCompleteState] Exited.");
    }

    public void Tick()
    {
    }
}

