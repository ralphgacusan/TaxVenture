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
        expText.text = $"EXP Earned: {result.ExpEarned}";
        reputationText.text = $"Reputation Earned: {result.ReputationEarned}";
        summaryText.text = result.SummaryText;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAchievementSFX();
        }

        CameraController.Instance?.LockPlayerControls();

        popupRoot.SetActive(true);
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