using UnityEngine;
using UnityEngine.UI;
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

        bool scored = result.MaxExp > 0 || result.MaxReputation > 0;
        expText.text = scored
            ? $"EXP Earned: {result.RewardExp} / {result.MaxExp}"
            : $"EXP Earned: {result.RewardExp}";
        reputationText.text = scored
            ? $"Hearts Earned: {result.RewardReputation} / {result.MaxReputation}"
            : $"Reputation Earned: {result.RewardReputation}";

        string text = "Side Quest Complete! You now know your taxpayers.";
        if (!string.IsNullOrEmpty(result.BadgeName))
            text += result.BadgeTier != BadgeTier.None
                ? $"\n{result.BadgeTier} badge earned: {result.BadgeName}"
                : $"\nBadge earned: {result.BadgeName}";
        if (scored)
        {
            text += result.WrongPlacements == 0
                ? "\nPerfect board: no wrong placements."
                : $"\n{result.WrongPlacements} wrong placement{(result.WrongPlacements == 1 ? "" : "s")} cost some reward.";
        }
        congratsText.text = text;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAchievementSFX();
        }

        panelRoot.SetActive(true);
        if (!HudRewardScreen.Present(panelRoot, BuildScreen(result))) ShowMedal(result);
    }

    /// <summary>The reward card for this result (Office HUD); same values as the text fields.</summary>
    private static RewardScreenData BuildScreen(SideQuestResult result)
    {
        bool scored = result.MaxExp > 0 || result.MaxReputation > 0;
        LevelRewardRules rules = CaseProgressionManager.Instance != null ? CaseProgressionManager.Instance.Score.Rules : null;
        var data = new RewardScreenData
        {
            Title = "Side Quest Complete",
            Subtitle = string.IsNullOrEmpty(result.BadgeName) ? "Know Your Taxpayer" : result.BadgeName,
            Exp = result.RewardExp,
            MaxExp = scored ? result.MaxExp : 0,
            Hearts = result.RewardReputation,
            MaxHearts = scored ? result.MaxReputation : 0,
            Summary = "You now know your taxpayers: residency, classification and how they earn their income."
        };
        if (result.BadgeTier != BadgeTier.None)
        {
            data.Medals.Add(HudBadgeArt.For(rules, result.BadgeTier));
            data.MedalLabel = result.BadgeTier + " badge";
            data.Accent = HudBadgeArt.TierColor(result.BadgeTier);
        }
        if (scored)
        {
            data.Fact("Wrong placements", result.WrongPlacements.ToString());
            data.Fact("Badge", result.BadgeTier == BadgeTier.None ? "-" : result.BadgeTier.ToString());
            if (result.BadgeTier != BadgeTier.Gold)
                data.Footer = "Replay with no wrong placements to earn Gold.";
        }
        return data;
    }

    private void Hide() => panelRoot.SetActive(false);

    // =========================================================
    // MEDAL (runtime-built: the collectible badge pops in)
    // =========================================================

    private const float MedalSize = 210f;
    private Image medalImage;
    private TextMeshProUGUI medalLabel;

    private void ShowMedal(SideQuestResult result)
    {
        bool show = result.BadgeTier != BadgeTier.None;
        if (!show)
        {
            if (medalImage != null) medalImage.gameObject.SetActive(false);
            if (medalLabel != null) medalLabel.gameObject.SetActive(false);
            return;
        }

        if (medalImage == null)
        {
            RectTransform medal = HudTheme.CreateRect("Badge Medal", panelRoot.transform);
            medal.anchorMin = medal.anchorMax = new Vector2(0.5f, 1f);
            medal.pivot = new Vector2(0.5f, 0.5f);
            medal.anchoredPosition = new Vector2(0f, -390f);
            medal.sizeDelta = new Vector2(MedalSize, MedalSize);
            medalImage = HudTheme.AddImage(medal, null, Color.white);
            medalImage.preserveAspect = true;

            medalLabel = HudTheme.CreateLabel("Badge Tier", panelRoot.transform, string.Empty, 26f, HudTheme.BrassSoft,
                TextAlignmentOptions.Center, FontStyles.Bold);
            medalLabel.characterSpacing = 6f;
            RectTransform label = (RectTransform)medalLabel.transform;
            label.anchorMin = label.anchorMax = new Vector2(0.5f, 1f);
            label.pivot = new Vector2(0.5f, 1f);
            label.anchoredPosition = new Vector2(0f, -390f - MedalSize * 0.5f - 2f);
            label.sizeDelta = new Vector2(600f, 36f);
        }

        LevelRewardRules rules = CaseProgressionManager.Instance != null ? CaseProgressionManager.Instance.Score.Rules : null;
        medalImage.sprite = HudBadgeArt.For(rules, result.BadgeTier);
        medalImage.gameObject.SetActive(true);
        medalLabel.text = result.BadgeTier.ToString().ToUpperInvariant() + " BADGE";
        medalLabel.gameObject.SetActive(true);

        StopAllCoroutines();
        if (isActiveAndEnabled) StartCoroutine(PopMedal(medalImage.rectTransform));
    }

    private System.Collections.IEnumerator PopMedal(RectTransform medal)
    {
        const float duration = 0.55f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = Mathf.LerpUnclamped(0.3f, 1f, HudTheme.EaseOutBack(k));
            medal.localScale = new Vector3(s, s, 1f);
            medal.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-14f, 0f, HudTheme.EaseOutCubic(k)));
            yield return null;
        }
        medal.localScale = Vector3.one;
        medal.localRotation = Quaternion.identity;
    }

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
    // Level-score details (0 / empty when the legacy inspector values are used).
    public int MaxExp;
    public int MaxReputation;
    public int WrongPlacements;
    public string BadgeName;
    public BadgeTier BadgeTier;

    public SideQuestResult(int rewardExp, int rewardReputation)
    {
        RewardExp = rewardExp;
        RewardReputation = rewardReputation;
    }
}