using UnityEngine;

/// <summary>
/// Handles the reward phase after a case has been successfully archived.
/// Once rewards are processed, the FSM enters CaseCompleteState.
/// CaseCompleteState then delegates to CaseProgressionManager to determine
/// whether another case should load or the level should end.
/// </summary>
public class RewardsState : IGameState
{
    public string StateName => "Receive Rewards";

    public void Enter()
    {
        Debug.Log("[RewardsState] Entered.");

        // For now, rewards are processed immediately.
        // A full reward UI can be inserted here later.
        CompleteRewards();
    }

    private void CompleteRewards()
    {
        Debug.Log(
            "[RewardsState] Rewards complete → CaseCompleteState."
        );

        GameStateMachine.Instance.ChangeState(
            new CaseCompleteState()
        );
    }

    public void Exit()
    {
        Debug.Log("[RewardsState] Exited.");
    }

    public void Tick()
    {
    }
}
