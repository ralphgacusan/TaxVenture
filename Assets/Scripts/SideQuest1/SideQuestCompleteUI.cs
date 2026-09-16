using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// "SIDE QUEST COMPLETE" panel, shown right after the tutorial NPC's
/// congratulation dialogue finishes for the corkboard sidequest.
/// Displays the reward EXP/Reputation for THIS sidequest only, sourced
/// from CorkboardSideQuestManager. This UI does NOT calculate anything
/// itself; it only displays what it's given and closes when the player
/// presses the button.
///
/// CONNECTS WITH:
/// - TutorialNpcController calls SideQuestCompleteUI.Instance.Show(...)
///   as the dialogue's onComplete callback, so the panel appears right
///   after the NPC finishes talking.
/// - CorkboardSideQuestManager supplies the SideQuestResult data.
/// </summary>
public class SideQuestCompleteUI : MonoBehaviour
{
    public static SideQuestCompleteUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;

    [Header("Result Fields")]
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI reputationText;
    [SerializeField] private TextMeshProUGUI congratsText;

    private SideQuestResult currentResult;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    /// <summary>
    /// Shows the sidequest's reward summary. Call this once the
    /// congratulation dialogue with the tutorial NPC has finished.
    /// </summary>
    public void Show(SideQuestResult result)
    {
        currentResult = result;

        expText.text = $"EXP Earned: {result.RewardExp}";
        reputationText.text = $"Reputation Earned: {result.RewardReputation}";
        congratsText.text = "Side Quest Complete! You now know your taxpayers.";

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAchievementSFX();
        }

        panelRoot.SetActive(true);
    }

    private void Hide() => panelRoot.SetActive(false);

    public void OnContinuePressed()
    {
        Hide();
    }
}

/// <summary>
/// Simple reward payload for a single sidequest, analogous to
/// LevelTotalResult but scoped to one sidequest instead of the whole level.
/// </summary>
[System.Serializable]
public class SideQuestResult
{
    public int RewardExp;
    public int RewardReputation;

    public SideQuestResult(int rewardExp, int rewardReputation)
    {
        RewardExp = rewardExp;
        RewardReputation = rewardReputation;
    }
}