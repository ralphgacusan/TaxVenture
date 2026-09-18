using UnityEngine;

public class LevelCompleteState : IGameState
{
    public string StateName => "Level Complete";

    public void Enter()
    {
        Debug.Log("[LevelCompleteState] Entered — all cases in this level are finished.");

        if (CaseProgressionManager.Instance == null)
        {
            Debug.LogError("[LevelCompleteState] CaseProgressionManager.Instance is NULL.");
            return;
        }

        if (LevelCompleteUI.Instance == null)
        {
            Debug.LogError("[LevelCompleteState] LevelCompleteUI.Instance is NULL!");
            return;
        }

        Debug.Log("[LevelCompleteState] LevelCompleteUI.Instance FOUND.");

        LevelTotalResult levelTotal =
            LevelTotalResult.FromAccumulator(
                CaseProgressionManager.Instance.RewardAccumulator
            );

        Debug.Log("[LevelCompleteState] LevelTotalResult created.");
        Debug.Log("[LevelCompleteState] Calling LevelCompleteUI.Show()...");

        LevelCompleteUI.Instance.Show(levelTotal);

        Debug.Log("[LevelCompleteState] LevelCompleteUI.Show() finished.");
    }

    public void Exit()
    {
        Debug.Log("[LevelCompleteState] Exited.");
    }

    public void Tick()
    {
    }
}