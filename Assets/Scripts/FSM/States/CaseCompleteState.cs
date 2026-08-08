using UnityEngine;

/// <summary>
/// PURPOSE:
/// Final phase of a SINGLE case's loop. Now the FSM integration point for
/// multi-case progression: on entering this state, CaseProgressionManager
/// decides whether to load the next case or transition to LevelCompleteState.
/// </summary>
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

        if (CaseProgressionManager.Instance == null)
        {
            Debug.LogError("[CaseCompleteState] CaseProgressionManager INSTANCE IS NULL!");
            return;
        }

        CaseProgressionManager.Instance.OnCaseFinished();
    }
    public void Exit() => Debug.Log("[CaseCompleteState] Exited.");
    public void Tick() { }
}