using UnityEngine;

/// <summary>
/// PURPOSE:
/// Terminal FSM state reached when the LAST case in the current level's
/// caseIds list has been completed. Mirrors CaseCompleteState's structure
/// exactly — same pattern, different destination UI.
/// </summary>
public class LevelCompleteState : IGameState
{
    public string StateName => "Level Complete";

    public void Enter()
    {
        Debug.Log("[LevelCompleteState] Entered — all cases in this level are finished.");
        LevelCompleteUI.Instance.Show();
    }

    public void Exit() => Debug.Log("[LevelCompleteState] Exited.");
    public void Tick() { }
}