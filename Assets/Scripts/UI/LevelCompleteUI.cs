using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// PURPOSE:
/// "LEVEL COMPLETE / Return to Main Module" screen, shown once the FSM
/// enters LevelCompleteState. Displays the level's total EXP, Reputation,
/// time taken, and cases completed — all sourced from a LevelTotalResult
/// built once from LevelRewardAccumulator. This UI does NOT calculate
/// anything itself; it only displays what it's given and applies it to
/// permanent progress when the player presses the button.
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    public static LevelCompleteUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private string mainModuleSceneName = "Progress";

    [Header("Result Fields")]
    [SerializeField] private TextMeshProUGUI completionTimeText;
    [SerializeField] private TextMeshProUGUI casesCompletedText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI reputationText;
    [SerializeField] private TextMeshProUGUI congratsText;

    private LevelTotalResult currentResult;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    /// <summary>
    /// Shows the level-wide total. Call this instead of the old
    /// parameterless Show() once LevelCompleteState builds the total.
    /// </summary>
    public void Show(LevelTotalResult result)
    {
        currentResult = result;

        completionTimeText.text = $"Time Taken: {result.FormattedCompletionTime}";
        casesCompletedText.text = $"Cases Completed: {result.CasesCompleted}";
        expText.text = $"Total EXP Earned: {result.TotalExp}";
        reputationText.text = $"Total Reputation Earned: {result.TotalReputation}";
        congratsText.text = "Congratulations! You've completed the level.";

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAchievementSFX();
        }

        panelRoot.SetActive(true);
    }

    private void Hide() => panelRoot.SetActive(false);

    public void OnReturnToMainModulePressed()
    {
        if (PlayerProgressManager.Instance != null && currentResult != null)
        {
            PlayerProgressManager.Instance.ApplyLevelResult(currentResult);
        }
        else
        {
            Debug.LogError(
                "[LevelCompleteUI] Could not apply level result — " +
                $"PlayerProgressManager null: {PlayerProgressManager.Instance == null}, " +
                $"currentResult null: {currentResult == null}"
            );
        }

        SceneManager.LoadScene(mainModuleSceneName);
    }
}