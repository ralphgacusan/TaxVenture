using UnityEngine;

public class LevelCompleteState : IGameState
{
    private readonly bool restoredApprovedResult;

    public LevelCompleteState() { }

    /// <summary>Resume an already committed approval without replaying its card or reward presentation.</summary>
    public LevelCompleteState(bool restoredApprovedResult)
    {
        this.restoredApprovedResult = restoredApprovedResult;
    }

    public string StateName => "Level Complete";

    public void Enter()
    {
        if (restoredApprovedResult)
        {
            Debug.Log("[LevelCompleteState] Restored an approved case; its saved Client Copy/end flow owns presentation.");
            return;
        }
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
            LevelTotalResult.FromLedger(
                CaseProgressionManager.Instance.Score,
                CaseProgressionManager.Instance.RewardAccumulator
            );
        levelTotal.LevelId = CaseProgressionManager.Instance.CurrentLevelId;

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
