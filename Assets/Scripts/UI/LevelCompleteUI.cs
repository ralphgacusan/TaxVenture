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
///
/// Level 3 T14: ShowLevelThreeChapter shows Bea's one-case chapter result on
/// the same card (no LevelCompleteState: the chapter's guard refuses it). It
/// only presents committed facts (Level3ClosingRules.Result, the Level_03
/// ledger). Its shared Continue button is
/// the explicit acknowledgment: Level3Checkpointer.CommitAcknowledgment saves
/// the chapter's completion before anything moves on; a failed write keeps the
/// card with a retry state. Nothing is paid here.
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
    // T14: the card shows Level 3's pending chapter result (its buttons acknowledge it).
    private bool levelThreeChapter;

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
        // T18: a story Level 2 result is committed once here too, before any celebration (case claim,
        // mastery record and the end of its checkpoint in one write); a failed write stays pending for Continue.
        bool levelTwoStory = manager != null && manager.IsLevelTwoStoryResult(result);
        bool levelTwoSaved = false;
        if (levelTwoStory)
        {
            levelTwoSaved = manager.CommitLevelTwoResult(result, out bool wroteNow);
            if (wroteNow) HudFeedback.AnnounceCampaign(manager.LastLevelTwoGrants.Trust, manager.LastLevelTwoGrants.Coins, null);
        }
        // Level 2 consistency pass: Rosa's approved review ends with her Client Copy, as Marie's does
        // (after the case result is saved; isolated runs too). The card shows after she has it.
        bool levelTwoCase = result.LevelId == Level2CampaignRules.LevelId;
        if (levelTwoCase && (!levelTwoStory || levelTwoSaved) &&
            ClientCopyHandover.Instance != null && ClientCopyHandover.Instance.BeginAfterApproval(result))
        {
            if (!levelTwoStory && manager != null) manager.SetPendingResult(result);
            Hide();
            return;
        }
        // Saved even if the player never presses Continue (app paused or closed).
        // A story Level 1 result is committed above instead (once, before any celebration).
        if (manager != null && result != null && !storyCaseResult && !levelTwoStory)
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

        if (levelTwoStory && !levelTwoSaved) congratsText.text = "Your result could not be saved. Press Continue to try again.";

        if (levelTwoStory)
        {
            reputationText.text = "";
            congratsText.text = levelTwoSaved && Level2CampaignRules.ChapterComplete(manager.CurrentProgress)
                ? "Rosa has her approved Client Copy. Chapter 2 is complete. Return to Career."
                : "Rosa still needs her approved Client Copy. Continue to retry.";
        }

        // No fanfare for a case result that is not saved yet.
        if (AudioManager.Instance != null && (!storyCaseResult || caseResultSaved) && (!levelTwoStory || levelTwoSaved))
        {
            AudioManager.Instance.PlayAchievementSFX();
        }

        panelRoot.SetActive(true);
        HudRewardScreen.Present(panelRoot, storyCaseResult ? BuildCaseResultScreen(result, caseResultSaved) : BuildScreen(result));

        Debug.Log("[LevelCompleteUI] panelRoot activated successfully.");
    }

    // =========================================================
    // LEVEL 3 CHAPTER RESULT (T14)
    // =========================================================

    private const string LevelThreeSaveFailedText = "Your chapter result could not be saved. Your progress is safe. Try again.";

    /// <summary>
    /// T14: Bea's pending chapter result (WrapUpPending). Presentation only:
    /// the frozen results and the Level_03 ledger; Continue acknowledges it.
    /// False when the card cannot be shown yet (the caller tries again later).
    /// </summary>
    public bool ShowLevelThreeChapter()
    {
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null || owner.State == null || panelRoot == null ||
            Level3ClosingRules.StageOf(owner.State) != Level3ClosingRules.Stage.Result) return false;
        if (ChapterEndingSequence.TryPlay("Level_03", () => ShowLevelThreeChapter())) return true;
        levelThreeChapter = true;
        storyCaseResult = false;
        currentResult = null;
        leaving = false;
        panelRoot.SetActive(true);
        CaseFinale.Instance?.PlayAfterHandover();
        if (!HudRewardScreen.Present(panelRoot, BuildChapterThreeScreen(owner, null)))
        {
            panelRoot.SetActive(false);
            levelThreeChapter = false;
            return false;
        }
        AudioManager.Instance?.PlayAchievementSFX();
        return true;
    }

    /// <summary>
    /// The acknowledgment is saved before returning to Career. A failed or
    /// refused write keeps the pending card with one retry line.
    /// </summary>
    private void ContinueLevelThree()
    {
        if (leaving) return;
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null) return;
        leaving = true;
        Level3Checkpointer.TapeCommit outcome = owner.CommitAcknowledgment(out _, out string hint);
        if (outcome == Level3Checkpointer.TapeCommit.SaveFailed || outcome == Level3Checkpointer.TapeCommit.Refused)
        {
            leaving = false;
            Debug.LogWarning("[LevelCompleteUI] Chapter 3 result not acknowledged (" + outcome + "); the card stays for a retry.");
            HudRewardScreen.Present(panelRoot, BuildChapterThreeScreen(owner, hint ?? LevelThreeSaveFailedText));
            return;
        }
        levelThreeChapter = false;
        owner.PrepareSettingsExit(false);
        LeaveForCareer();
    }

    /// <summary>What this chapter's activities earn from its own saved facts: the same rewards the story claims. Presentation for unsaved test runs.</summary>
    public static void LevelThreeAttemptRewards(Level3ChapterCheckpoint state, out int coins, out int trust)
    {
        coins = trust = 0;
        Level3CaseProgress bea = state?.Case(Level3ChapterRules.BeaCaseId);
        if (Level3WorksheetCheck.Collected(state))
        {
            coins += CampaignRewards.Amount(CampaignRewards.Level3, CampaignRewards.BeaWorksheet, CampaignRewards.CoinsKind);
            trust += CampaignRewards.Amount(CampaignRewards.Level3, CampaignRewards.BeaWorksheet, CampaignRewards.TrustKind);
        }
        if (bea?.review?.approved == true) coins += CampaignRewards.Amount(CampaignRewards.Level3, CampaignRewards.BeaCaseApproved, CampaignRewards.CoinsKind);
        if (Level3DeliveryRules.Delivered(state)) trust += Level3DeliveryRules.FollowThroughTrust(bea?.balanceWrongAnswers ?? 0);
    }

    /// <summary>The Chapter 3 card: aggregated XP and badge, the chapter's coins/Trust from the ledger, the shared Continue button.</summary>
    private RewardScreenData BuildChapterThreeScreen(Level3Checkpointer owner, string failure)
    {
        Level3ChapterResult result = Level3ClosingRules.Result(owner.State);
        PlayerProgressData career = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.CurrentProgress : null;
        CaseResultRules.ChapterSummary summary = CaseResultRules.BuildChapterSummary(career, CampaignRewards.Level3);
        LevelRewardRules rules = CaseProgressionManager.Instance != null ? CaseProgressionManager.Instance.Score.Rules : null;
        BadgeTier tier = result.WorksheetKnown ? (BadgeTier)result.WorksheetTier : BadgeTier.None;
        bool failed = !string.IsNullOrEmpty(failure);
        // T16: a replay/practice/Editor run shows its own XP and badge only; the story's coins and Trust are not its rewards.
        bool story = owner.SavesStory;
        CampaignRunMode mode = PlayerProgressManager.CurrentRunMode;
        var data = ChapterTemplate("Chapter 3 Complete", "The Numbers Tell a Story", result.Exp, Level3ChapterResult.MaxExp,
            HudBadgeArt.TierColor(tier == BadgeTier.None ? BadgeTier.Bronze : tier));
        data.StatusGood = !failed;
        data.StatusText = failed ? "NOT SAVED YET" : story ? CaseResultRules.CompletedStatus : mode == CampaignRunMode.MasteryReplay ? "REPLAY  ·  BEST ONLY" : CampaignRun.Label(mode);
        data.StatusDetail = failed ? failure : CaseResultRules.CompletedDetail;
        data.Summary = story ? CaseResultRules.CompletedSummary : CampaignRun.WhatChanges(mode);
        data.ButtonLabel = failed ? "TRY AGAIN" : "CONTINUE";
        data.Footer = CareerResultFooter(career, Level3ChapterRules.LevelId, result.Exp, Level3ChapterResult.MaxExp);
        if (tier != BadgeTier.None) data.Medals.Add(HudBadgeArt.For(rules, tier));
        if (story && career != null && career.HasCampaign)
        {
            data.RewardTile("Coins", HudTheme.Coin, HudTheme.CoinGold, summary.CoinsEarned, "Wallet", summary.Wallet);
            data.RewardTile("Public Trust", HudTheme.Heart, HudTheme.Reputation, summary.TrustEarned, "Trust", summary.TrustNow, CampaignRecord.MaxTrust);
        }
        else if (mode == CampaignRunMode.DirectEditor)
        {
            // An Editor test run shows what this attempt would earn (nothing here is saved); replays and drills keep their own card.
            LevelThreeAttemptRewards(owner.State, out int coins, out int trust);
            int walletBefore = career != null && career.HasCampaign ? career.campaign.coins : 0;
            int trustBefore = career != null && career.HasCampaign ? career.campaign.publicTrust : 0;
            data.RewardTile("Coins", HudTheme.Coin, HudTheme.CoinGold, coins, "Wallet", walletBefore + coins);
            data.RewardTile("Public Trust", HudTheme.Heart, HudTheme.Reputation, trust, "Trust", Mathf.Min(trustBefore + trust, CampaignRecord.MaxTrust), CampaignRecord.MaxTrust);
        }
        if (result.WorksheetKnown)
            data.Fact("Worksheet first check", $"{result.WorksheetCorrect}/{result.WorksheetTotal}  ·  {tier}");
        if (result.CaseKnown)
            data.Fact("Computation review", $"{result.CaseXp}/{Level3ReviewRules.MaxXp} XP");
        if (LevelTimer.Instance != null) data.Fact("Time", LevelTimer.Instance.GetFormattedTime());
        return data;
    }

    /// <summary>Restores the approved result card from its durable case-result record.</summary>
    public void ShowAfterHandover(LevelTotalResult result)
    {
        result ??= CreateSavedCaseResult();
        if (result == null)
        {
            Debug.LogWarning("[LevelCompleteUI] No saved level result was available after the handover.");
            return;
        }
        string chapter = result.LevelId;
        if (ChapterEndingSequence.TryPlay(chapter, () => ShowAfterHandover(result))) return;
        CaseFinale.Instance?.PlayAfterHandover();
        Show(result);
    }

    /// <summary>
    /// Level 2: the level total from this run's ledger (a resumed approved review restores its case
    /// line first), for the card after Rosa has her copy. Presentation only; claims are once-only.
    /// </summary>
    public static LevelTotalResult CreateLevelResult()
    {
        CaseProgressionManager progression = CaseProgressionManager.Instance;
        if (progression == null) return null;
        LevelTotalResult result = LevelTotalResult.FromLedger(progression.Score, progression.RewardAccumulator);
        result.LevelId = progression.CurrentLevelId;
        return result;
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
        var data = ChapterTemplate(CaseResultRules.CompletedTitle,
            progression != null ? progression.CurrentLevelName : null, result.TotalExp, result.HasScore ? result.MaxExp : 0,
            HudBadgeArt.TierColor(tier == BadgeTier.None ? BadgeTier.Bronze : tier));
        for (int i = 0; i < result.BadgeTiers.Count; i++)
            if (result.BadgeTiers[i] != BadgeTier.None) data.Medals.Add(HudBadgeArt.For(rules, result.BadgeTiers[i]));
        data.RewardTile("Coins", HudTheme.Coin, HudTheme.CoinGold, summary.CoinsEarned, "Wallet", summary.Wallet);
        data.RewardTile("Public Trust", HudTheme.Heart, HudTheme.Reputation, summary.TrustEarned,
            "Trust", summary.TrustNow, CampaignRecord.MaxTrust);
        if (caseResult != null && caseResult.firstCheckCorrect >= 0)
            data.Fact("Board first check", $"{caseResult.firstCheckCorrect}/4  ·  {tier}");
        data.Fact("Time", result.FormattedCompletionTime);
        data.Footer = CareerResultFooter(career, CampaignRecord.FirstChapterId, result.TotalExp, result.MaxExp);
        return data;
    }

    private static RewardScreenData ChapterTemplate(string title, string subtitle, int exp, int maximum, Color accent) =>
        new RewardScreenData
        {
            Title = title, Subtitle = subtitle, Exp = exp, MaxExp = maximum,
            Hearts = 0, MaxHearts = 0, PassingPercent = 0, HasStatus = true, StatusGood = true,
            StatusText = CaseResultRules.CompletedStatus, StatusDetail = CaseResultRules.CompletedDetail,
            Accent = accent, Summary = CaseResultRules.CompletedSummary, ButtonLabel = "CONTINUE", Large = true
        };

    private static string CareerResultFooter(PlayerProgressData career, string levelId, int earned, int maximum)
    {
        LevelRecord best = career?.levelRecords?.Find(record => record.levelId == levelId);
        string current = "This run " + earned + " / " + maximum + " EXP";
        if (career == null) return current;
        return current + "  ·  Career " + career.totalExp + " EXP" +
            "\nBest on this level " + Mathf.Max(best?.bestExp ?? 0, earned) + " / " + maximum + " EXP";
    }

    /// <summary>The reward card for the level (Office HUD); same values as the text fields.</summary>
    private static RewardScreenData BuildScreen(LevelTotalResult result)
    {
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        if (manager != null && manager.IsLevelTwoStoryResult(result)) return BuildChapterTwoScreen(result, manager);
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

    /// <summary>Story completion follows delivery, independently of the frozen mastery grade.</summary>
    private static RewardScreenData BuildChapterTwoScreen(LevelTotalResult result, PlayerProgressManager manager)
    {
        PlayerProgressData progress = manager.CurrentProgress;
        bool complete = Level2CampaignRules.ChapterComplete(progress);
        var data = new RewardScreenData
        {
            Title = complete ? "Chapter 2 Complete" : "Rosa’s Review",
            Subtitle = "Something Does Not Match",
            Exp = result.TotalExp,
            MaxExp = result.HasScore ? result.MaxExp : 0,
            Hearts = 0, MaxHearts = 0, PassingPercent = 0,
            HasStatus = true, StatusGood = complete,
            StatusText = complete ? "CLIENT COPY DELIVERED" : "DELIVERY PENDING",
            Summary = complete ? "Rosa has her approved copy and understands her corrected May summary. Thank you for seeing her case through."
                : "The review must be saved and Rosa must receive her approved Client Copy. Continue to retry.",
            Footer = "Return to Career to continue your story."
        };
        LevelRewardRules rules = CaseProgressionManager.Instance != null ? CaseProgressionManager.Instance.Score.Rules : null;
        foreach (BadgeTier tier in result.BadgeTiers)
            if (tier != BadgeTier.None) data.Medals.Add(HudBadgeArt.For(rules, tier));
        data.Fact("Time", result.FormattedCompletionTime);
        if (progress != null && progress.HasCampaign)
        {
            data.Fact("Public Trust", $"{progress.campaign.publicTrust}/100");
            data.Fact("Wallet", $"{progress.campaign.coins} coins");
            RosaEthicsRecord ethics = progress.campaign.rosaEthics;
            if (ethics != null && ethics.committed) data.Fact("Rosa’s review", Level2CampaignRules.EthicsStatus(progress, ethics.caseId));
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
        if (ChapterEndingSequence.BlocksGameplay) return;
        // T14: Level 3's chapter result: this button acknowledges it, then goes to the career board.
        if (levelThreeChapter) { ContinueLevelThree(); return; }
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

        if (PlayerProgressManager.Instance != null && !PlayerProgressManager.Instance.AcknowledgeLevelTwoEnding())
        {
            leaving = false;
            if (congratsText != null) congratsText.text = "Your result could not be saved. Press Continue to try again.";
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
