using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Turns real game events into HUD notifications and reward effects.
/// Every notice corresponds to something that actually happened, with the
/// real value — nothing is invented here.
///
/// - Level score entries (LevelScoreLedger.EntryAdded):
///   * client conversation: "+1 Heart", and the heart floats from that NPC
///     into the hearts bar once the dialogue is over;
///   * side quest: "+EXP", "+Hearts", "Badge earned" and, when wrong
///     placements cost reward, a deduction notice; hearts and stars fly from
///     the guide, the new badge medallion pulses;
///   * case: "+EXP" / "+Hearts" with stars and hearts from the screen centre.
///   Bars fill as the effects land. Reaching the passing line shows
///   "Passing score reached" once.
/// - Failed audit (CaseProgressionManager.OnAuditFailed): what the mistakes
///   and the extra submission cost the case reward.
/// - Locked actions (LevelGate.OnBlocked) and, once, entering the reception
///   while the side quest is unfinished: "Finish Side Quest 1 first".
/// - Objectives (OfficeObjectives.Changed): "Objective complete" per newly
///   completed step, "Main objective complete" per finished main objective.
///   Steps already done at load are not announced.
/// - Taxpayer profile collected (TownEvents.OnProfileCollected): toast with
///   the name and n/4; the profile icon pulses as the paper arrives.
/// </summary>
public sealed class HudFeedback : MonoBehaviour
{
    private const int MaxHearts = 8;
    private const int MaxStars = 10;
    private const float ProfileArrivalDelay = 0.6f;
    private const float AreaCheckInterval = 0.3f;

    private OfficeHudRuntime host;
    private HudToastQueue toasts;
    private HudRewardFlyFX fx;
    private HudProgressionPanel progression;
    private OfficeObjectives objectives;
    private RectTransform profileIcon;

    private TaskRowStatus[] lastStatus;
    private readonly List<ScoreEntry> pendingRewards = new List<ScoreEntry>();
    private int pendingProfilePulses;
    private bool ready;
    private bool passingAnnounced;

    // One-time "side quest first" notice when walking into the reception.
    private bool receptionWarned;
    private bool receptionResolved;
    private bool receptionValid;
    private Bounds receptionBounds;
    private float nextAreaCheck;

    public static HudFeedback Create(GameObject hostObject, OfficeHudRuntime host, HudToastQueue toasts,
        HudRewardFlyFX fx, HudProgressionPanel progression, OfficeObjectives objectives, RectTransform profileIcon)
    {
        var feedback = hostObject.AddComponent<HudFeedback>();
        feedback.host = host;
        feedback.toasts = toasts;
        feedback.fx = fx;
        feedback.progression = progression;
        feedback.objectives = objectives;
        feedback.profileIcon = profileIcon;
        feedback.Subscribe();
        return feedback;
    }

    private void Subscribe()
    {
        LevelScoreLedger.EntryAdded += HandleScoreEntry;
        CaseProgressionManager.OnAuditFailed += HandleAuditFailed;
        LevelGate.OnBlocked += HandleBlocked;
        TownEvents.OnProfileCollected += HandleProfileCollected;
        if (objectives != null)
        {
            objectives.Changed += HandleObjectivesChanged;
            lastStatus = Copy(objectives.Snapshot);
        }
    }

    private void OnDestroy()
    {
        LevelScoreLedger.EntryAdded -= HandleScoreEntry;
        CaseProgressionManager.OnAuditFailed -= HandleAuditFailed;
        LevelGate.OnBlocked -= HandleBlocked;
        TownEvents.OnProfileCollected -= HandleProfileCollected;
        if (objectives != null) objectives.Changed -= HandleObjectivesChanged;
    }

    private void Start()
    {
        // Steps seeded as already done on load are not announced.
        if (objectives != null) lastStatus = Copy(objectives.Snapshot);
        LevelScoreLedger score = CurrentScore();
        passingAnnounced = score != null && score.Passed && score.Entries.Count > 0;
        ready = true;
    }

    private static LevelScoreLedger CurrentScore() =>
        CaseProgressionManager.Instance != null ? CaseProgressionManager.Instance.Score : null;

    // =========================================================
    // REWARDS
    // =========================================================

    private void HandleScoreEntry(ScoreEntry entry)
    {
        if (entry == null) return;

        switch (entry.Source)
        {
            case ScoreSource.Conversation:
                if (entry.Hearts > 0)
                    toasts.Show(HudToastKind.Reputation, "Client conversation", null, "talk", entry.Hearts);
                break;

            case ScoreSource.SideQuest:
                if (entry.Exp > 0) toasts.Show(HudToastKind.Exp, "Side quest reward", null, "exp", entry.Exp);
                if (entry.Hearts > 0) toasts.Show(HudToastKind.Reputation, "Side quest reward", null, "rep", entry.Hearts);
                if (!string.IsNullOrEmpty(entry.BadgeName))
                {
                    LevelScoreLedger ledger = CurrentScore();
                    toasts.Show(HudToastKind.Badge, $"{entry.BadgeTier} badge earned", entry.BadgeName, null, 0,
                        HudBadgeArt.For(ledger != null ? ledger.Rules : null, entry.BadgeTier));
                }
                ShowShortfall("Wrong placements", entry);
                break;

            case ScoreSource.Penalty:
                toasts.Show(HudToastKind.Deduction, entry.Label, FormatLoss(-entry.Exp, -entry.Hearts));
                break;

            case ScoreSource.Bonus:
                if (entry.Exp > 0) toasts.Show(HudToastKind.Exp, entry.Label, null, "bonus", entry.Exp);
                if (entry.Hearts > 0) toasts.Show(HudToastKind.Reputation, entry.Label, null, "bonusrep", entry.Hearts);
                break;

            default:
                if (entry.Exp > 0) toasts.Show(HudToastKind.Exp, "EXP earned", null, "exp", entry.Exp);
                if (entry.Hearts > 0) toasts.Show(HudToastKind.Reputation, "Hearts earned", null, "rep", entry.Hearts);
                break;
        }

        if (entry.Exp > 0 || entry.Hearts > 0 || !string.IsNullOrEmpty(entry.BadgeName))
        {
            pendingRewards.Add(entry);
            // The bars wait for the effect to land (counted while the HUD is shown).
            if (progression != null) progression.HoldFillFor(0.85f);
        }

        LevelScoreLedger score = CurrentScore();
        if (score != null && !passingAnnounced && score.Passed)
        {
            passingAnnounced = true;
            toasts.Show(HudToastKind.Passing, "Passing score reached",
                $"{score.Rules.passingPercent}% of the EXP and hearts - next level unlocks");
        }
    }

    /// <summary>How much below the maximum a reward came out, as a deduction notice.</summary>
    private void ShowShortfall(string reason, ScoreEntry entry)
    {
        int lostExp = Mathf.Max(0, entry.MaxExp - entry.Exp);
        int lostHearts = Mathf.Max(0, entry.MaxHearts - entry.Hearts);
        if (lostExp == 0 && lostHearts == 0) return;
        toasts.Show(HudToastKind.Deduction, reason, FormatLoss(lostExp, lostHearts));
    }

    private void HandleAuditFailed(int mistakes, LevelScoring.Award cost)
    {
        string title = mistakes > 0
            ? $"Auditor found {mistakes} mistake{(mistakes == 1 ? "" : "s")}"
            : "Case sent back";
        toasts.Show(HudToastKind.Deduction, title, "Case reward " + FormatLoss(cost.Exp, cost.Hearts));
    }

    private static string FormatLoss(int exp, int hearts)
    {
        var parts = new List<string>(2);
        if (exp > 0) parts.Add($"-{exp} EXP");
        if (hearts > 0) parts.Add($"-{hearts} {(hearts == 1 ? "heart" : "hearts")}");
        return string.Join("   ", parts);
    }

    private void HandleBlocked(string message)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_ui_unavailable_01", 0.65f);
        toasts.Show(HudToastKind.Locked, "Locked", message, "locked");
    }

    private void Update()
    {
        WatchReception();

        if (host == null || !host.IsShown) return;

        if (pendingRewards.Count > 0 && progression != null)
        {
            Vector2 centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            for (int i = 0; i < pendingRewards.Count; i++)
            {
                ScoreEntry r = pendingRewards[i];
                Vector2 from = ScreenOrigin(r, centre);
                int hearts = Mathf.Clamp(r.Hearts, 0, MaxHearts);
                int stars = r.Exp > 0 ? Mathf.Clamp(Mathf.CeilToInt(r.Exp / 12f), 3, MaxStars) : 0;
                bool single = r.Source == ScoreSource.Conversation;
                if ((stars > 0 || hearts > 0) && AudioManager.Instance != null)
                    AudioManager.Instance.PlayEffect("sfx_reward_points_01", 0.65f);
                fx.Burst(HudTheme.Star, HudTheme.Exp, stars, from + new Vector2(-40f, 0f), progression.ExpBar, 34f);
                fx.Burst(HudTheme.Heart, HudTheme.Reputation, hearts, from + new Vector2(single ? 0f : 40f, 0f),
                    progression.ReputationBar, single ? 44f : 38f);
            }
            pendingRewards.Clear();

            RectTransform medal = progression.TakeNewBadge();
            if (medal != null)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_reward_badge_01", 0.7f);
                fx.PulseAfter(medal, 0.9f);
            }
        }

        if (pendingProfilePulses > 0)
        {
            fx.PulseAfter(profileIcon, ProfileArrivalDelay);
            pendingProfilePulses = 0;
        }
    }

    /// <summary>The NPC the reward came from, on screen; the screen centre otherwise.</summary>
    private static Vector2 ScreenOrigin(ScoreEntry entry, Vector2 fallback)
    {
        if (!entry.Origin.HasValue) return fallback;
        Camera cam = Camera.main;
        if (cam == null) return fallback;
        Vector3 p = cam.WorldToScreenPoint(entry.Origin.Value);
        if (p.z <= 0f) return fallback;
        return new Vector2(Mathf.Clamp(p.x, 40f, Screen.width - 40f), Mathf.Clamp(p.y, 40f, Screen.height - 40f));
    }

    // =========================================================
    // LEVEL ORDER
    // =========================================================

    private void WatchReception()
    {
        if (receptionWarned || LevelGate.CaseAllowed || Time.unscaledTime < nextAreaCheck) return;
        nextAreaCheck = Time.unscaledTime + AreaCheckInterval;

        if (!receptionResolved)
        {
            receptionResolved = true;
            receptionValid = OfficeObjectives.ResolveArea(gameObject.scene,
                ObjectiveTriggers.AreaPrefix + TaskListProvider.ReceptionArea, out receptionBounds);
        }
        if (!receptionValid) return;

        Transform player = CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;
        if (player == null || !OfficeObjectives.IsInsideArea(receptionBounds, player.position)) return;

        receptionWarned = true;
        toasts.Show(HudToastKind.Locked, "Side Quest 1 first",
            "Finish Side Quest 1 before starting the case", "locked");
    }

    // =========================================================
    // OBJECTIVES
    // =========================================================

    private void HandleObjectivesChanged()
    {
        TaskProgressSnapshot snapshot = objectives.Snapshot;
        if (snapshot == null) return;
        // A resumed run's steps were announced the first time (T04).
        if (!ready || objectives.Restoring)
        {
            lastStatus = Copy(snapshot);
            return;
        }
        IReadOnlyList<TaskDefinition> tasks = objectives.Tasks;

        for (int i = 0; i < snapshot.Status.Length && i < tasks.Count; i++)
        {
            bool nowDone = snapshot.Status[i] == TaskRowStatus.Done;
            bool wasDone = lastStatus != null && i < lastStatus.Length && lastStatus[i] == TaskRowStatus.Done;
            if (!nowDone || wasDone || tasks[i] == null) continue;

            if (tasks[i].IsHeader)
                toasts.Show(HudToastKind.MainObjective, "Main objective complete", tasks[i].TaskDescription);
            else
                toasts.Show(HudToastKind.Objective, "Objective complete", Shorten(tasks[i].TaskDescription));
        }

        lastStatus = Copy(snapshot);
    }

    private static string Shorten(string text)
    {
        const int max = 52;
        if (string.IsNullOrEmpty(text) || text.Length <= max) return text;
        int cut = text.LastIndexOf(' ', max - 3);
        return text.Substring(0, cut > 20 ? cut : max - 3) + "...";
    }

    private static TaskRowStatus[] Copy(TaskProgressSnapshot snapshot)
    {
        if (snapshot == null || snapshot.Status == null) return null;
        return (TaskRowStatus[])snapshot.Status.Clone();
    }

    // =========================================================
    // PROFILES
    // =========================================================

    private void HandleProfileCollected(string profileId)
    {
        TaxpayerProfile profile = TaxpayerProfileDatabase.GetById(profileId);
        TaxpayerProfileCollectionHUD hud = TaxpayerProfileCollectionHUD.Instance;
        string name = profile != null ? profile.TaxpayerName : "Taxpayer profile";
        string count = hud != null ? $"  ({hud.CollectedProfiles.Count}/{hud.RequiredProfiles})" : string.Empty;
        toasts.Show(HudToastKind.Profile, "Profile collected", name + count);
        pendingProfilePulses++;
    }
}
