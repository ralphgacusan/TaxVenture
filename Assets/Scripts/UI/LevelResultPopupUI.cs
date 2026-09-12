
using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Level Result popup — full version per R13. Displays Completion Time,
/// Verdict, Issues Found, EXP, Reputation, and a Summary, all sourced from
/// a single LevelResultData built by RewardsCalculator.
/// </summary>
public class LevelResultPopupUI : MonoBehaviour
{
    public static LevelResultPopupUI Instance { get; private set; }

    [SerializeField] private GameObject popupRoot;

    [Header("Result Fields")]
    [SerializeField] private TextMeshProUGUI completionTimeText;
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

    /// <summary>
    /// Now takes CaseData directly (rather than just a branch enum) so it
    /// can build the full LevelResultData itself via RewardsCalculator.
    /// </summary>
    public void Show(CaseData data, System.Action onClosedCallback)
    {
        onClosed = onClosedCallback;

        LevelResultData result = RewardsCalculator.BuildResult(data);

        completionTimeText.text =
            $"Completion Time: {result.FormattedCompletionTime}";

        verdictText.text = result.VerdictWasCorrect
            ? $"Verdict: Correct ({EnumDisplayFormatter.Format(result.PlayerVerdict.ToString())})"
            : $"Verdict: Incorrect (You said {EnumDisplayFormatter.Format(result.PlayerVerdict.ToString())})";

        issuesFoundText.text =
            $"Issues Found: {result.IssuesFound}";

        expText.text =
            $"EXP Earned: {result.ExpEarned}";

        reputationText.text =
            $"Reputation Earned: {result.ReputationEarned}";

        summaryText.text =
            result.SummaryText;

        // Play achievement/reward SFX when the result popup is shown.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAchievementSFX();
        }

        // Lock player movement and camera.
        CameraController.Instance?.LockPlayerControls();

        popupRoot.SetActive(true);
    }

    public void OnClosePressed()
    {
        popupRoot.SetActive(false);

        // Restore movement and camera.
        CameraController.Instance?.UnlockPlayerControls();

        onClosed?.Invoke();
    }

    private void Hide()
    {
        popupRoot.SetActive(false);
    }
}
