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

        LevelTotalResult levelTotal = LevelTotalResult.FromAccumulator(
            CaseProgressionManager.Instance.RewardAccumulator
        );

        LevelCompleteUI.Instance.Show(levelTotal);
    }

    public void Exit()
    {
        Debug.Log("[LevelCompleteState] Exited.");
    }

    public void Tick()
    {
    }
}