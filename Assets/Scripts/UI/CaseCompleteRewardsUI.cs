using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// PURPOSE:
/// Single combined popup showing both the "Congratulations!" message and
/// the Reward numbers together, per updated design: one panel, congrats
/// heading on top, EXP/Reputation below, one button to close and return
/// to Main Menu. Replaces the separate RewardsUI + CaseCompleteUI split.
///
/// RESPONSIBILITIES:
/// - Auto-show when GameStateMachine enters RewardsState
/// - Calculate rewards via RewardsCalculator
/// - Display congrats text + EXP + Reputation + mistake summary together
/// - On button press: advance FSM to CaseCompleteState, then load Main Menu
///
/// CONNECTS WITH:
/// - GameStateMachine: subscribes to OnStateChanged
/// - RewardsCalculator: pure calculation
/// - CaseManager.Instance.CurrentCase: reads auditMistakeCount
/// </summary>
public class CaseCompleteRewardsUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Top - Congrats")]
    [SerializeField] private TextMeshProUGUI congratsText;

    [Header("Bottom - Rewards")]
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI reputationText;
    [SerializeField] private TextMeshProUGUI mistakeSummaryText;

    [Header("Scene Transition")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        Hide();
    }

    // private void Start()
    // {
    //     if (GameStateMachine.Instance != null)
    //     {
    //         GameStateMachine.Instance.OnStateChanged += HandleStateChanged;
    //     }
    // }

    // private void OnDestroy()
    // {
    //     if (GameStateMachine.Instance != null)
    //     {
    //         GameStateMachine.Instance.OnStateChanged -= HandleStateChanged;
    //     }
    // }

    // private void HandleStateChanged(IGameState newState)
    // {
    //     Debug.Log("State changed to: " + newState.GetType().Name);

    //     if (newState is RewardsState)
    //     {
    //         Debug.Log("Showing rewards popup");
    //         Show();
    //     }
    // }

    public void Show()
    {
        congratsText.text = "Congratulations! You completed your first case.";

        CaseData data = CaseManager.Instance.CurrentCase;
        RewardResult result = RewardsCalculator.Calculate(data.auditMistakeCount);

        expText.text = $"EXP Earned: {result.ExpEarned}";
        reputationText.text = $"Reputation Hearts Earned: {result.ReputationHeartsEarned}";
        mistakeSummaryText.text = result.MistakeCount == 0
            ? "No mistakes were found during your audit — excellent work!"
            : $"{result.MistakeCount} mistake(s) were found during your audit, reducing your rewards.";

        panelRoot.SetActive(true);
    }

    private void Hide()
    {
        panelRoot.SetActive(false);
    }

    /// <summary>Wired to the single button (e.g. "Continue" / "Return to Main Menu").</summary>
    public void OnContinuePressed()
    {
        Hide();
        GameStateMachine.Instance.ChangeState(new CaseCompleteState());
        SceneManager.LoadScene(mainMenuSceneName);
    }
}