using UnityEngine;

/// <summary>
/// PURPOSE:
/// Final phase of the Phase 1 gameplay loop. Per the original brief's scope:
/// "Only ONE playable case... Show 'Congratulations!'... Then return to
/// Main Menu." Entered once the Rewards screen is dismissed.
///
/// TRANSITIONS TO:
/// - Nothing further within this scene — RewardsUI/CaseCompleteUI instead
///   loads the Main Menu scene directly, since there is no "next case" in
///   Phase 1 scope.
///
/// CONNECTS WITH:
/// - RewardsUI: requests entry into this state after Continue is pressed
/// - CaseCompleteUI: displays the congratulations message, loads Main Menu
/// </summary>
public class CaseCompleteState : IGameState
{
    public string StateName => "Case Complete";
    public void Enter() => Debug.Log("[CaseCompleteState] Entered. Congratulations! You completed your first case.");
    public void Exit() => Debug.Log("[CaseCompleteState] Exited.");
    public void Tick() { }
}