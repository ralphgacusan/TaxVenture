using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Where a level reward came from.</summary>
public enum ScoreSource
{
    Conversation,
    SideQuest,
    Case,
    /// <summary>
    /// Extra reward (e.g. on-time bonus). It can win back points lost
    /// elsewhere but never lifts the level score above its maximum.
    /// </summary>
    Bonus,
    /// <summary>A deduction (e.g. a late case); never takes the totals below zero.</summary>
    Penalty
}

/// <summary>One reward added to the level's score.</summary>
public sealed class ScoreEntry
{
    public ScoreSource Source;
    public string Label;
    public int Exp;
    public int Hearts;
    public int MaxExp;
    public int MaxHearts;
    public string BadgeId;
    public string BadgeName;
    public BadgeTier BadgeTier;
    /// <summary>World point the reward comes from (an NPC), for the HUD effect.</summary>
    public Vector3? Origin;
}

/// <summary>
/// PURPOSE:
/// The level's running score: every reward by source (client conversation
/// bonuses, the side quest, each case), the badges earned, and the totals
/// against the level maximum and the passing line. Owned by
/// CaseProgressionManager (reset when a level starts). Replaces the
/// case-only total for the HUD and the level result.
///
/// EntryAdded is a presentation hook (HUD notices / effects); listeners must
/// not change rewards.
///
/// LIMITS (the score can never read like 170 / 150):
/// - TotalExp / TotalHearts always stay between 0 and MaxExp / MaxHearts.
/// - A bonus only fills the room left below the maximum; when there is no
///   room it adds nothing (no entry, no notice).
/// - Client-talk hearts stop after conversation.count different clients.
/// - Every amount is clamped to a sane range, so a broken rewards.json or
///   a bad caller can't overflow the totals.
/// </summary>
public sealed class LevelScoreLedger
{
    public static event Action<ScoreEntry> EntryAdded;

    private readonly List<ScoreEntry> entries = new List<ScoreEntry>();
    private readonly HashSet<string> talkedTo = new HashSet<string>(StringComparer.Ordinal);
    private readonly List<string> badges = new List<string>();
    private readonly List<string> badgeNames = new List<string>();
    private readonly List<BadgeTier> badgeTiers = new List<BadgeTier>();

    /// <summary>Largest amount any single reward may carry (guards int overflow).</summary>
    public const int MaxSingleAmount = 1000000;

    public LevelRewardRules Rules { get; private set; } = new LevelRewardRules();
    public int TotalExp { get; private set; }
    public int TotalHearts { get; private set; }
    public int MaxExp { get; private set; }
    public int MaxHearts { get; private set; }
    /// <summary>EXP / hearts that bonuses actually added (after the cap).</summary>
    public int BonusExp { get; private set; }
    public int BonusHearts { get; private set; }
    public IReadOnlyList<ScoreEntry> Entries => entries;
    public IReadOnlyList<string> Badges => badges;
    public IReadOnlyList<string> BadgeNames => badgeNames;
    /// <summary>Tier of each earned badge (same order as Badges).</summary>
    public IReadOnlyList<BadgeTier> BadgeTiers => badgeTiers;
    public bool SideQuestScored { get; private set; }

    // Run timing (unscaled seconds) for the career board: the whole level from
    // load to completion, and the side quest from its first collected profile
    // (or the level start) until its reward.
    private float runStartedAt = -1f;
    private float sideQuestStartedAt = -1f;
    public float SideQuestSeconds { get; private set; }
    /// <summary>Seconds source (the game sets Time.unscaledTime); null = untimed, as in the regression tests.</summary>
    public Func<float> Clock { get; set; }
    private float Now => Clock != null ? Clock() : -1f;
    public float ElapsedSeconds
    {
        get
        {
            float now = Now;
            return runStartedAt < 0f || now < 0f ? 0f : Mathf.Max(0f, now - runStartedAt);
        }
    }

    /// <summary>The side quest has begun (first profile collected). Only the first call counts.</summary>
    public void MarkSideQuestStarted()
    {
        if (sideQuestStartedAt < 0f && !SideQuestScored) sideQuestStartedAt = Now;
    }

    public int ExpPercent => LevelScoring.Percent(TotalExp, MaxExp);
    public int HeartsPercent => LevelScoring.Percent(TotalHearts, MaxHearts);
    public bool Passed => LevelScoring.Passed(Rules, TotalExp, TotalHearts, MaxExp, MaxHearts);

    public void Reset(LevelRewardRules rules, int cases)
    {
        Rules = rules ?? new LevelRewardRules();
        entries.Clear();
        talkedTo.Clear();
        badges.Clear();
        badgeNames.Clear();
        badgeTiers.Clear();
        TotalExp = 0;
        TotalHearts = 0;
        BonusExp = 0;
        BonusHearts = 0;
        SideQuestScored = false;
        runStartedAt = Now;
        sideQuestStartedAt = -1f;
        SideQuestSeconds = 0f;
        MaxExp = LevelScoring.MaxExp(Rules, cases);
        MaxHearts = LevelScoring.MaxHearts(Rules, cases);
    }

    /// <summary>+hearts for finishing a client conversation (once per client).</summary>
    public ScoreEntry AddConversation(string npcId, string label, Vector3? origin)
    {
        if (string.IsNullOrEmpty(npcId) || talkedTo.Contains(npcId)) return null;
        // Only as many clients as the level counts toward its maximum.
        if (talkedTo.Count >= Rules.conversation.count) return null;
        talkedTo.Add(npcId);
        return Add(new ScoreEntry
        {
            Source = ScoreSource.Conversation,
            Label = label,
            Hearts = Rules.conversation.hearts,
            MaxHearts = Rules.conversation.hearts,
            Origin = origin
        });
    }

    /// <summary>
    /// Side quest result (once per level), with its badge. A level with an
    /// accuracy badge (rewards.json goldAtCorrect) passes the frozen tier in
    /// <paramref name="accuracyTier"/>; otherwise the tier comes from the EXP.
    /// </summary>
    public ScoreEntry AddSideQuest(int wrongPlacements, Vector3? origin, BadgeTier? accuracyTier = null)
    {
        if (SideQuestScored) return null;
        SideQuestScored = true;
        float sideQuestStart = sideQuestStartedAt >= 0f ? sideQuestStartedAt : runStartedAt;
        float sideQuestEnd = Now;
        SideQuestSeconds = sideQuestStart < 0f || sideQuestEnd < 0f ? 0f : Mathf.Max(0f, sideQuestEnd - sideQuestStart);
        LevelScoring.Award award = LevelScoring.SideQuest(Rules, wrongPlacements);
        BadgeTier tier = accuracyTier.HasValue && accuracyTier.Value != BadgeTier.None
            ? accuracyTier.Value
            : LevelScoring.SideQuestBadge(Rules, award);
        if (!string.IsNullOrEmpty(Rules.sideQuest.badgeId) && !badges.Contains(Rules.sideQuest.badgeId))
        {
            badges.Add(Rules.sideQuest.badgeId);
            badgeNames.Add(Rules.sideQuest.badgeName);
            badgeTiers.Add(tier);
        }

        return Add(new ScoreEntry
        {
            Source = ScoreSource.SideQuest,
            Label = "Side Quest",
            Exp = award.Exp,
            Hearts = award.Hearts,
            MaxExp = award.MaxExp,
            MaxHearts = award.MaxHearts,
            BadgeId = Rules.sideQuest.badgeId,
            BadgeName = Rules.sideQuest.badgeName,
            BadgeTier = string.IsNullOrEmpty(Rules.sideQuest.badgeId) ? BadgeTier.None : tier,
            Origin = origin
        });
    }

    /// <summary>Case result after the Auditor approves it.</summary>
    public ScoreEntry AddCase(string label, int mistakes, int retries, int extraExpLoss = 0, int extraHeartLoss = 0)
    {
        LevelScoring.Award award = LevelScoring.Case(Rules, mistakes, retries, extraExpLoss, extraHeartLoss);
        return Add(new ScoreEntry
        {
            Source = ScoreSource.Case,
            Label = label,
            Exp = award.Exp,
            Hearts = award.Hearts,
            MaxExp = award.MaxExp,
            MaxHearts = award.MaxHearts
        });
    }

    /// <summary>
    /// A bonus that fills only the room left below the level maximum (the
    /// maximum is not raised). Returns null when it adds nothing.
    /// </summary>
    public ScoreEntry AddBonus(string label, int exp, int hearts)
    {
        int addExp = Math.Min(Clamp(exp), Math.Max(0, MaxExp - TotalExp));
        int addHearts = Math.Min(Clamp(hearts), Math.Max(0, MaxHearts - TotalHearts));
        if (addExp == 0 && addHearts == 0) return null;
        BonusExp += addExp;
        BonusHearts += addHearts;
        return Add(new ScoreEntry
        {
            Source = ScoreSource.Bonus,
            Label = label,
            Exp = addExp,
            Hearts = addHearts
        });
    }

    /// <summary>A deduction, capped so the totals never go below zero. Exp/Hearts are stored negative.</summary>
    public ScoreEntry AddPenalty(string label, int exp, int hearts)
    {
        int lostExp = Math.Min(Clamp(exp), TotalExp);
        int lostHearts = Math.Min(Clamp(hearts), TotalHearts);
        if (lostExp == 0 && lostHearts == 0) return null;
        return Add(new ScoreEntry
        {
            Source = ScoreSource.Penalty,
            Label = label,
            Exp = -lostExp,
            Hearts = -lostHearts
        });
    }

    /// <summary>Case result scored elsewhere (a case without an Auditor audit).</summary>
    public ScoreEntry AddCaseValues(string label, int exp, int hearts)
    {
        return Add(new ScoreEntry
        {
            Source = ScoreSource.Case,
            Label = label,
            Exp = Math.Min(Clamp(exp), Rules.caseFile.exp),
            Hearts = Math.Min(Clamp(hearts), Rules.caseFile.hearts),
            MaxExp = Rules.caseFile.exp,
            MaxHearts = Rules.caseFile.hearts
        });
    }

    private ScoreEntry Add(ScoreEntry entry)
    {
        entries.Add(entry);
        // Totals never leave 0..max, whatever order rewards arrive in.
        TotalExp = Math.Max(0, Math.Min(MaxExp, TotalExp + entry.Exp));
        TotalHearts = Math.Max(0, Math.Min(MaxHearts, TotalHearts + entry.Hearts));
        try
        {
            EntryAdded?.Invoke(entry);
        }
        catch (Exception e)
        {
            // Presentation must never break scoring.
            Debug.LogException(e);
        }
        return entry;
    }

    private static int Clamp(int amount) => Math.Max(0, Math.Min(MaxSingleAmount, amount));
}
