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

        Debug.Log("[LevelCompleteUI] Awake() called.");
        Debug.Log($"[LevelCompleteUI] panelRoot assigned: {panelRoot != null}");
        Debug.Log($"[LevelCompleteUI] completionTimeText assigned: {completionTimeText != null}");
        Debug.Log($"[LevelCompleteUI] casesCompletedText assigned: {casesCompletedText != null}");
        Debug.Log($"[LevelCompleteUI] expText assigned: {expText != null}");
        Debug.Log($"[LevelCompleteUI] reputationText assigned: {reputationText != null}");
        Debug.Log($"[LevelCompleteUI] congratsText assigned: {congratsText != null}");

        Hide();
    }

    /// <summary>
    /// Shows the level-wide total.
    /// </summary>
    public void Show(LevelTotalResult result)
    {
        Debug.Log("[LevelCompleteUI] Show() CALLED.");

        if (result == null)
        {
            Debug.LogError("[LevelCompleteUI] Show() received a NULL LevelTotalResult!");
            return;
        }

        currentResult = result;

        Debug.Log($"[LevelCompleteUI] Time: {result.FormattedCompletionTime}");
        Debug.Log($"[LevelCompleteUI] Cases: {result.CasesCompleted}");
        Debug.Log($"[LevelCompleteUI] EXP: {result.TotalExp}");
        Debug.Log($"[LevelCompleteUI] Reputation: {result.TotalReputation}");

        if (completionTimeText == null)
        {
            Debug.LogError("[LevelCompleteUI] completionTimeText is NULL!");
            return;
        }

        if (casesCompletedText == null)
        {
            Debug.LogError("[LevelCompleteUI] casesCompletedText is NULL!");
            return;
        }

        if (expText == null)
        {
            Debug.LogError("[LevelCompleteUI] expText is NULL!");
            return;
        }

        if (reputationText == null)
        {
            Debug.LogError("[LevelCompleteUI] reputationText is NULL!");
            return;
        }

        if (congratsText == null)
        {
            Debug.LogError("[LevelCompleteUI] congratsText is NULL!");
            return;
        }

        if (panelRoot == null)
        {
            Debug.LogError("[LevelCompleteUI] panelRoot is NULL!");
            return;
        }

        completionTimeText.text =
            $"Time Taken: {result.FormattedCompletionTime}";

        casesCompletedText.text =
            $"Cases Completed: {result.CasesCompleted}";

        expText.text =
            $"Total EXP Earned: {result.TotalExp}";

        reputationText.text =
            $"Total Reputation Earned: {result.TotalReputation}";

        congratsText.text =
            "Congratulations! You've completed the level.";

        Debug.Log("[LevelCompleteUI] All text fields updated.");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAchievementSFX();
        }

        panelRoot.SetActive(true);

        Debug.Log("[LevelCompleteUI] panelRoot activated successfully.");
    }

    private void Hide()
    {
        if (panelRoot == null)
        {
            Debug.LogError("[LevelCompleteUI] Cannot Hide() — panelRoot is NULL!");
            return;
        }

        panelRoot.SetActive(false);
        Debug.Log("[LevelCompleteUI] Panel hidden.");
    }

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