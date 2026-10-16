using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Where a level reward came from.</summary>
public enum ScoreSource
{
    Conversation,
    SideQuest,
    Case,
    /// <summary>Extra reward on top of the level maximum (e.g. on-time bonus).</summary>
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
/// </summary>
public sealed class LevelScoreLedger
{
    public static event Action<ScoreEntry> EntryAdded;

    private readonly List<ScoreEntry> entries = new List<ScoreEntry>();
    private readonly HashSet<string> talkedTo = new HashSet<string>(StringComparer.Ordinal);
    private readonly List<string> badges = new List<string>();
    private readonly List<string> badgeNames = new List<string>();
    private readonly List<BadgeTier> badgeTiers = new List<BadgeTier>();

    public LevelRewardRules Rules { get; private set; } = new LevelRewardRules();
    public int TotalExp { get; private set; }
    public int TotalHearts { get; private set; }
    public int MaxExp { get; private set; }
    public int MaxHearts { get; private set; }
    public IReadOnlyList<ScoreEntry> Entries => entries;
    public IReadOnlyList<string> Badges => badges;
    public IReadOnlyList<string> BadgeNames => badgeNames;
    /// <summary>Tier of each earned badge (same order as Badges).</summary>
    public IReadOnlyList<BadgeTier> BadgeTiers => badgeTiers;
    public bool SideQuestScored { get; private set; }

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
        SideQuestScored = false;
        MaxExp = LevelScoring.MaxExp(Rules, cases);
        MaxHearts = LevelScoring.MaxHearts(Rules, cases);
    }

    /// <summary>+hearts for finishing a client conversation (once per client).</summary>
    public ScoreEntry AddConversation(string npcId, string label, Vector3? origin)
    {
        if (string.IsNullOrEmpty(npcId) || !talkedTo.Add(npcId)) return null;
        return Add(new ScoreEntry
        {
            Source = ScoreSource.Conversation,
            Label = label,
            Hearts = Rules.conversation.hearts,
            MaxHearts = Rules.conversation.hearts,
            Origin = origin
        });
    }

    /// <summary>Side quest result (once per level), with its badge.</summary>
    public ScoreEntry AddSideQuest(int wrongPlacements, Vector3? origin)
    {
        if (SideQuestScored) return null;
        SideQuestScored = true;
        LevelScoring.Award award = LevelScoring.SideQuest(Rules, wrongPlacements);
        BadgeTier tier = LevelScoring.SideQuestBadge(Rules, award);
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
    public ScoreEntry AddCase(string label, int mistakes, int retries)
    {
        LevelScoring.Award award = LevelScoring.Case(Rules, mistakes, retries);
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

    /// <summary>A bonus on top of the level maximum (the maximum is not raised).</summary>
    public ScoreEntry AddBonus(string label, int exp, int hearts)
    {
        return Add(new ScoreEntry
        {
            Source = ScoreSource.Bonus,
            Label = label,
            Exp = Math.Max(0, exp),
            Hearts = Math.Max(0, hearts)
        });
    }

    /// <summary>A deduction, capped so the totals never go below zero. Exp/Hearts are stored negative.</summary>
    public ScoreEntry AddPenalty(string label, int exp, int hearts)
    {
        int lostExp = Math.Min(Math.Max(0, exp), TotalExp);
        int lostHearts = Math.Min(Math.Max(0, hearts), TotalHearts);
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
            Exp = Math.Max(0, exp),
            Hearts = Math.Max(0, hearts),
            MaxExp = Rules.caseFile.exp,
            MaxHearts = Rules.caseFile.hearts
        });
    }

    private ScoreEntry Add(ScoreEntry entry)
    {
        entries.Add(entry);
        TotalExp += entry.Exp;
        TotalHearts += entry.Hearts;
        EntryAdded?.Invoke(entry);
        return entry;
    }
}
