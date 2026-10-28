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
///
/// T07 (Campaign Foundation): a STORY run of Level 1 shows a case result
/// instead ("Case Approved"): it is committed once (PlayerProgressManager.
/// CommitLevelOneCaseResult) BEFORE the card celebrates; the card shows the
/// frozen first-check tier, XP and badge as mastery, no pass line, no score
/// hearts, and the next step (Marie's Client Copy handover). A failed save
/// shows a retry state without fanfare. The result card waits until Marie has
/// received the copy.
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
    // T07: Level 1 story run (case result path) and whether its result is saved.
    private bool storyCaseResult;
    private bool caseResultSaved;

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
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        storyCaseResult = manager != null && manager.IsStoryCaseResult(result);
        caseResultSaved = storyCaseResult && TryCommitCaseResult();
        if (storyCaseResult && caseResultSaved && manager.ClientCopyHandoverExpected &&
            ClientCopyHandover.Instance != null && ClientCopyHandover.Instance.BeginAfterApproval(result))
        {
            Hide();
            return;
        }
        // Saved even if the player never presses Continue (app paused or closed).
        // A story Level 1 result is committed above instead (once, before any celebration).
        if (manager != null && result != null && !storyCaseResult)
            manager.SetPendingResult(result);

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

        if (storyCaseResult)
        {
            // T07: mastery numbers only; no pass line and no score hearts on the story's case result.
            expText.text = $"EXP: {result.TotalExp} / {result.MaxExp}";
            reputationText.text = "";
            congratsText.text = caseResultSaved
                ? $"Case approved. {CaseResultRules.NextStepText}"
                : CaseResultSaveFailedText;
        }
        else if (result.HasScore)
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

        // No fanfare for a case result that is not saved yet.
        if (AudioManager.Instance != null && (!storyCaseResult || caseResultSaved))
        {
            AudioManager.Instance.PlayAchievementSFX();
        }

        panelRoot.SetActive(true);
        HudRewardScreen.Present(panelRoot, storyCaseResult ? BuildCaseResultScreen(result, caseResultSaved) : BuildScreen(result));

        Debug.Log("[LevelCompleteUI] panelRoot activated successfully.");
    }

    /// <summary>Restores the approved result card from its durable case-result record.</summary>
    public void ShowAfterHandover(LevelTotalResult result)
    {
        result ??= CreateSavedCaseResult();
        if (result == null)
        {
            Debug.LogWarning("[LevelCompleteUI] No saved Level 1 result was available after the handover.");
            return;
        }
        Show(result);
    }

    /// <summary>Rebuilds a result presentation from the once-only saved result, including its original run time.</summary>
    public static LevelTotalResult CreateSavedCaseResult()
    {
        PlayerProgressData progress = PlayerProgressManager.Instance != null
            ? PlayerProgressManager.Instance.CurrentProgress : null;
        Level1CaseResult saved = progress != null && progress.HasCampaign ? progress.campaign.levelOneCaseResult : null;
        if (saved == null || !saved.recorded) return null;

        int seconds = Mathf.Max(0, Mathf.RoundToInt(saved.runSeconds));
        int hours = seconds / 3600;
        int minutes = seconds % 3600 / 60;
        int remainder = seconds % 60;
        var result = new LevelTotalResult
        {
            LevelId = CampaignRecord.FirstChapterId,
            TotalExp = saved.exp,
            MaxExp = saved.maxExp,
            TotalReputation = saved.scoreHearts,
            MaxReputation = saved.maxScoreHearts,
            CasesCompleted = 1,
            FormattedCompletionTime = $"{hours:00}:{minutes:00}:{remainder:00}",
            RunSeconds = saved.runSeconds,
            HasScore = true
        };
        result.BadgeTiers.Add((BadgeTier)saved.tier);
        return result;
    }

    private const string CaseResultSaveFailedText = "Your case result could not be saved. Press Continue to try again.";

    /// <summary>Commits the story case result (once); true when it is saved (now or earlier).</summary>
    private bool TryCommitCaseResult()
    {
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        LevelTotalResult result = currentResult;
        if (manager == null || result == null) return false;
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        PlayerProgressManager.CaseResultCommit commit = manager.CommitLevelOneCaseResult(result,
            definition != null ? definition.caseId : null, definition != null ? Level1CaseCheckpoint.Fingerprint(definition) : null);
        if (commit == PlayerProgressManager.CaseResultCommit.Committed)
            HudFeedback.AnnounceCampaign(default, manager.LastCaseCoins, null);
        return commit == PlayerProgressManager.CaseResultCommit.Committed || commit == PlayerProgressManager.CaseResultCommit.AlreadyRecorded;
    }

    /// <summary>
    /// T07 case result card: "Case Approved", the frozen first-check tier and
    /// mastery XP/badge, no pass line or score hearts, and the next step.
    /// Not labelled "Chapter Complete" or "Level Complete".
    /// </summary>
    private static RewardScreenData BuildCaseResultScreen(LevelTotalResult result, bool saved)
    {
        CaseProgressionManager progression = CaseProgressionManager.Instance;
        LevelRewardRules rules = progression != null ? progression.Score.Rules : null;
        PlayerProgressData career = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.CurrentProgress : null;
        Level1CaseResult caseResult = career != null && career.HasCampaign ? career.campaign.levelOneCaseResult : null;
        bool known = saved && caseResult != null && caseResult.recorded;
        BadgeTier tier = known ? (BadgeTier)caseResult.tier : BadgeTier.None;
        // T08/T16: after the handover this is the Chapter 1 card; its Continue completes the chapter.
        bool delivered = saved && career != null &&
            CampaignRewards.IsActivityCompleted(career, CampaignRewards.Level1, CampaignRewards.FollowThrough);
        if (delivered) return BuildChapterScreen(result, career, caseResult, rules);

        var data = new RewardScreenData
        {
            Title = delivered ? CaseResultRules.CompletedTitle : "Case Approved",
            Subtitle = progression != null ? progression.CurrentLevelName : null,
            Exp = result.TotalExp,
            MaxExp = result.HasScore ? result.MaxExp : 0,
            Hearts = 0,
            MaxHearts = 0,          // score hearts are mastery data, never shown as trust (T08 owns the trust hearts)
            PassingPercent = 0,     // no pass line: score never gates the story
            HasStatus = true,
            StatusGood = saved,
            StatusText = delivered ? CaseResultRules.CompletedStatus : saved ? "CASE APPROVED" : "NOT SAVED YET",
            StatusDetail = delivered ? CaseResultRules.CompletedDetail
                : saved ? "Classification corrected  ·  Marie's return approved" : "Your progress is safe. Try again.",
            Accent = saved ? HudBadgeArt.TierColor(tier == BadgeTier.None ? BadgeTier.Bronze : tier) : new Color32(150, 160, 175, 255),
            Summary = delivered ? CaseResultRules.CompletedSummary : saved ? CaseResultRules.NextStepText : CaseResultSaveFailedText,
            ButtonLabel = saved ? "CONTINUE" : "TRY AGAIN"
        };
        if (known && caseResult.firstCheckCorrect >= 0)
            data.Fact("Board first check", $"{caseResult.firstCheckCorrect}/4  ·  {tier}");
        // T09: the case's coins, saved with the result (a pending, unsaved result shows none).
        CampaignRewards.Change coins = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.LastCaseCoins : default;
        if (saved && coins.Applied)
            data.RewardTile("Coins", HudTheme.Coin, HudTheme.CoinGold, coins.Amount, "Wallet", coins.After);
        for (int i = 0; i < result.BadgeTiers.Count; i++)
            if (result.BadgeTiers[i] != BadgeTier.None) data.Medals.Add(HudBadgeArt.For(rules, result.BadgeTiers[i]));
        data.Fact("Time", result.FormattedCompletionTime);
        OfficeHours hours = OfficeHours.Instance;
        if (hours != null && hours.Announced)
            data.Fact("Office hours", hours.OnTime ? "On time ✓"
                : hours.LateExpLost > 0 ? $"Late (day {hours.Day})  -{hours.LateExpLost} EXP" : $"Finished on day {hours.Day}");
        if (career != null) data.Footer = $"Career {career.totalExp} EXP";
        return data;
    }

    /// <summary>
    /// The Chapter 1 card (2026-10-26): the case card's style, bigger, with only
    /// the important facts: EXP and medals, the chapter's coins and Public Trust
    /// (from the committed ledger), the first board check and the time.
    /// </summary>
    private static RewardScreenData BuildChapterScreen(LevelTotalResult result, PlayerProgressData career,
        Level1CaseResult caseResult, LevelRewardRules rules)
    {
        CaseProgressionManager progression = CaseProgressionManager.Instance;
        BadgeTier tier = caseResult != null && caseResult.recorded ? (BadgeTier)caseResult.tier : BadgeTier.None;
        CaseResultRules.ChapterSummary summary = CaseResultRules.BuildChapterSummary(career);
        var data = new RewardScreenData
        {
            Title = CaseResultRules.CompletedTitle,
            Subtitle = progression != null ? progression.CurrentLevelName : null,
            Exp = result.TotalExp,
            MaxExp = result.HasScore ? result.MaxExp : 0,
            Hearts = 0,
            MaxHearts = 0,
            PassingPercent = 0,
            HasStatus = true,
            StatusGood = true,
            StatusText = CaseResultRules.CompletedStatus,
            StatusDetail = CaseResultRules.CompletedDetail,
            Accent = HudBadgeArt.TierColor(tier == BadgeTier.None ? BadgeTier.Bronze : tier),
            Summary = CaseResultRules.CompletedSummary,
            ButtonLabel = "CONTINUE",
            Large = true
        };
        for (int i = 0; i < result.BadgeTiers.Count; i++)
            if (result.BadgeTiers[i] != BadgeTier.None) data.Medals.Add(HudBadgeArt.For(rules, result.BadgeTiers[i]));
        data.RewardTile("Coins", HudTheme.Coin, HudTheme.CoinGold, summary.CoinsEarned, "Wallet", summary.Wallet);
        data.RewardTile("Public Trust", HudTheme.Heart, HudTheme.Reputation, summary.TrustEarned,
            "Trust", summary.TrustNow, CampaignRecord.MaxTrust);
        if (caseResult != null && caseResult.firstCheckCorrect >= 0)
            data.Fact("Board first check", $"{caseResult.firstCheckCorrect}/4  ·  {tier}");
        data.Fact("Time", result.FormattedCompletionTime);
        return data;
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
            data.Fact("Office hours", hours.OnTime ? "On time ✓"
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

        if (storyCaseResult && PlayerProgressManager.Instance != null && currentResult != null)
        {
            // T07: the case result must be saved before anything moves on (retry here on a failed save).
            if (!caseResultSaved) caseResultSaved = TryCommitCaseResult();
            if (!caseResultSaved)
            {
                leaving = false;
                Debug.LogWarning("[LevelCompleteUI] Case result not saved; stay on the card and allow retry.");
                if (congratsText != null) congratsText.text = CaseResultSaveFailedText;
                return;
            }
            PlayerProgressManager manager = PlayerProgressManager.Instance;
            if (manager.FollowThroughPending)
            {
                if (ClientCopyHandover.Instance != null && ClientCopyHandover.Instance.BeginAfterApproval(currentResult))
                {
                    Hide();
                    leaving = false;
                    return;
                }
                leaving = false;
                if (congratsText != null) congratsText.text = "The Client Copy is not ready yet. Press Continue to try again.";
                return;
            }
            if (manager.PendingLevelOneEndFlow)
            {
                // T16: Chapter 1 completes here, in one write; only a saved wrap-up leaves for the career board.
                if (!manager.CommitLevelOneWrapUp())
                {
                    leaving = false;
                    Debug.LogWarning("[LevelCompleteUI] Chapter 1 wrap-up could not be saved; stay on the card and allow retry.");
                    if (congratsText != null) congratsText.text = "Your result could not be saved. Press Continue to try again.";
                    return;
                }
                // The card itself was the Chapter 1 card: straight to the career board.
            }
        }
        else if (PlayerProgressManager.Instance != null && currentResult != null)
        {
            PlayerProgressManager.Instance.ApplyLevelResult(currentResult);
            if (!PlayerProgressManager.Instance.SaveBeforeLeaving())
            {
                leaving = false;
                Debug.LogWarning("[LevelCompleteUI] Result not saved; stay on results and allow retry.");
                if (congratsText != null) congratsText.text = "Your result could not be saved. Please try again.";
                return;
            }
        }
        else
        {
            Debug.LogError(
                "[LevelCompleteUI] Could not apply level result — " +
                $"PlayerProgressManager null: {PlayerProgressManager.Instance == null}, " +
                $"currentResult null: {currentResult == null}"
            );
            leaving = false;
            return;
        }

        LeaveForCareer();
    }

    private void LeaveForCareer()
    {
        Debug.Log($"[LevelCompleteUI] Returning to the level select in {MenuEntry.MenuSceneName} (was '{mainModuleSceneName}').");
        // The main menu and the progress / level select are one scene now
        // ("3D Menu"); open it directly on the level select.
        MenuEntry.OpenLevelSelect = true;
        SceneFade.FadeOutThenLoad(MenuEntry.MenuSceneName, fadeInDestination: true);
    }
}
