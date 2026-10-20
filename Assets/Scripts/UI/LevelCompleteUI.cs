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
        // Saved even if the player never presses Continue (app paused or closed).
        if (PlayerProgressManager.Instance != null && result != null)
            PlayerProgressManager.Instance.SetPendingResult(result);

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

        if (result.HasScore)
        {
            expText.text =
                $"EXP: {result.TotalExp} / {result.MaxExp} ({result.ExpPercent}%)";

            reputationText.text =
                $"Hearts: {result.TotalReputation} / {result.MaxReputation} ({result.ReputationPercent}%)";

            string badges = string.Empty;
            if (result.BadgeNames != null)
            {
                var names = new System.Collections.Generic.List<string>();
                for (int i = 0; i < result.BadgeNames.Count; i++)
                {
                    BadgeTier tier = result.BadgeTiers != null && i < result.BadgeTiers.Count ? result.BadgeTiers[i] : BadgeTier.None;
                    names.Add(tier == BadgeTier.None ? result.BadgeNames[i] : $"{result.BadgeNames[i]} ({tier})");
                }
                if (names.Count > 0) badges = $"\nBadge: {string.Join(", ", names)}";
            }

            congratsText.text = result.Passed
                ? $"PASSED! You reached the {result.PassingPercent}% passing line. The next level is unlocked.{badges}"
                : $"Below passing: you need {result.PassingPercent}% of the EXP and hearts. Replay this level to unlock the next one.{badges}";
        }
        else
        {
            expText.text =
                $"Total EXP Earned: {result.TotalExp}";

            reputationText.text =
                $"Total Reputation Earned: {result.TotalReputation}";

            congratsText.text =
                "Congratulations! You've completed the level.";
        }

        Debug.Log("[LevelCompleteUI] All text fields updated.");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAchievementSFX();
        }

        panelRoot.SetActive(true);
        HudRewardScreen.Present(panelRoot, BuildScreen(result));

        Debug.Log("[LevelCompleteUI] panelRoot activated successfully.");
    }

    /// <summary>The reward card for the level (Office HUD); same values as the text fields.</summary>
    private static RewardScreenData BuildScreen(LevelTotalResult result)
    {
        CaseProgressionManager progression = CaseProgressionManager.Instance;
        LevelRewardRules rules = progression != null ? progression.Score.Rules : null;
        var data = new RewardScreenData
        {
            Title = "Level Complete",
            Subtitle = progression != null ? progression.CurrentLevelName : null,
            Exp = result.TotalExp,
            MaxExp = result.HasScore ? result.MaxExp : 0,
            Hearts = result.TotalReputation,
            MaxHearts = result.HasScore ? result.MaxReputation : 0,
            PassingPercent = result.HasScore ? result.PassingPercent : 0
        };

        if (result.HasScore)
        {
            data.HasStatus = true;
            data.StatusGood = result.Passed;
            data.Accent = result.Passed ? new Color32(241, 194, 108, 255) : new Color32(150, 160, 175, 255);
            data.StatusText = result.Passed ? "PASSED" : "BELOW PASSING";
            data.StatusDetail = $"{result.ExpPercent}% EXP  ·  {result.ReputationPercent}% hearts  ·  {result.PassingPercent}% needed";
            data.Summary = result.Passed
                ? "Great work! The next level is unlocked."
                : $"You need {result.PassingPercent}% of the EXP and the hearts. Replay this level to unlock the next one.";
            for (int i = 0; i < result.BadgeTiers.Count; i++)
                if (result.BadgeTiers[i] != BadgeTier.None) data.Medals.Add(HudBadgeArt.For(rules, result.BadgeTiers[i]));
        }
        else
        {
            data.Summary = "Congratulations! You've completed the level.";
        }

        data.Fact("Time", result.FormattedCompletionTime);
        data.Fact("Cases", result.CasesCompleted.ToString());
        OfficeHours hours = OfficeHours.Instance;
        if (hours != null && hours.Announced)
            data.Fact("Office hours", hours.BonusAwarded ? "On time ✓"
                : hours.LateExpLost > 0 || hours.LateHeartsLost > 0
                    ? $"Late (day {hours.Day})  -{hours.LateExpLost} EXP  -{hours.LateHeartsLost} hearts"
                    : $"Finished on day {hours.Day}");

        PlayerProgressData career = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.CurrentProgress : null;
        if (career != null)
        {
            LevelRecord best = career.GetRecord(result.LevelId);
            data.Footer = best != null
                ? $"Career {career.totalExp} EXP  ·  Best on this level {best.bestExp} / {best.maxExp} EXP"
                : $"Career {career.totalExp} EXP";
        }
        return data;
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

    private bool leaving;

    public void OnReturnToMainModulePressed()
    {
        if (leaving) return;   // one press only (the screen fades out first)
        leaving = true;

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

        Debug.Log($"[LevelCompleteUI] Returning to the level select in {MenuEntry.MenuSceneName} (was '{mainModuleSceneName}').");
        // The main menu and the progress / level select are one scene now
        // ("3D Menu"); open it directly on the level select.
        MenuEntry.OpenLevelSelect = true;
        SceneFade.FadeOutThenLoad(MenuEntry.MenuSceneName, fadeInDestination: true);
    }
}