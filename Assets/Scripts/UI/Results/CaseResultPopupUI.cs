using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Per-case result popup, shown immediately after presenting findings
/// to the client (for any non-perfect outcome). Displays a single
/// case's verdict, issues, EXP, and Reputation.
///
/// Takes an already-built LevelResultData — it does NOT call
/// RewardsCalculator itself. The result must be calculated exactly
/// once via CaseProgressionManager.CalculateAndStoreCaseReward,
/// then passed here purely for display.
/// </summary>
public class CaseResultPopupUI : MonoBehaviour
{
    public static CaseResultPopupUI Instance { get; private set; }

    [SerializeField] private GameObject popupRoot;

    /// <summary>Read-only visibility for noninteractive HUD presentation.</summary>
    public bool IsVisible => popupRoot != null && popupRoot.activeInHierarchy;

    [Header("Result Fields")]
    [SerializeField] private TextMeshProUGUI completionTimeText; // add this back
    [SerializeField] private TextMeshProUGUI verdictText;
    [SerializeField] private TextMeshProUGUI issuesFoundText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI reputationText;
    [SerializeField] private TextMeshProUGUI summaryText;

    private System.Action onClosed;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show(LevelResultData result, string caseFormattedTime, System.Action onClosedCallback)
    {
        onClosed = onClosedCallback;

        completionTimeText.text = $"Time Taken: {caseFormattedTime}";

        verdictText.text = result.VerdictWasCorrect
            ? $"Verdict: Correct ({EnumDisplayFormatter.Format(result.PlayerVerdict.ToString())})"
            : $"Verdict: Incorrect (You said {EnumDisplayFormatter.Format(result.PlayerVerdict.ToString())})";

        issuesFoundText.text = $"Issues Found: {result.IssuesFound}";
        LevelRewardRules rules = CaseProgressionManager.Instance != null
            ? CaseProgressionManager.Instance.Score.Rules
            : null;
        expText.text = rules != null
            ? $"EXP Earned: {result.ExpEarned} / {rules.caseFile.exp}"
            : $"EXP Earned: {result.ExpEarned}";
        reputationText.text = rules != null
            ? $"Hearts Earned: {result.ReputationEarned} / {rules.caseFile.hearts}"
            : $"Reputation Earned: {result.ReputationEarned}";
        summaryText.text = result.SummaryText;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAchievementSFX();
        }

        CameraController.Instance?.LockPlayerControls();

        popupRoot.SetActive(true);
        HudRewardScreen.Present(popupRoot, BuildScreen(result, caseFormattedTime, rules));
    }

    /// <summary>The reward card for this case (Office HUD); same values as the text fields.</summary>
    private static RewardScreenData BuildScreen(LevelResultData result, string time, LevelRewardRules rules)
    {
        CaseProgressionManager progression = CaseProgressionManager.Instance;
        var data = new RewardScreenData
        {
            Title = "Case Results",
            Subtitle = progression != null ? $"Case {progression.CurrentCaseNumber} of {progression.TotalCasesInLevel}" : null,
            Exp = result.ExpEarned,
            MaxExp = rules != null ? rules.caseFile.exp : RewardsCalculator.MaxExp,
            Hearts = result.ReputationEarned,
            MaxHearts = rules != null ? rules.caseFile.hearts : RewardsCalculator.MaxReputation,
            HasVerdict = true,
            VerdictCorrect = result.VerdictWasCorrect,
            VerdictText = EnumDisplayFormatter.Format(result.PlayerVerdict.ToString()),
            Flawless = result.Branch == ClientOutcomeBranch.CorrectNoIssues,
            Accent = result.VerdictWasCorrect ? new Color32(110, 201, 140, 255) : new Color32(226, 112, 96, 255),
            Summary = result.SummaryText
        };
        data.Fact("Time", time);
        data.Fact("Issues", result.IssuesFound.ToString());
        return data;
    }

    public void OnClosePressed()
    {
        popupRoot.SetActive(false);

        CameraController.Instance?.UnlockPlayerControls();

        onClosed?.Invoke();
    }

    private void Hide()
    {
        popupRoot.SetActive(false);
    }
}