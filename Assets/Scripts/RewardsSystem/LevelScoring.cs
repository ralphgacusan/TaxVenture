using System;

/// <summary>
/// PURPOSE:
/// Pure reward maths for a level (no Unity calls, unit-testable). All
/// numbers come from LevelRewardRules (rewards.json).
///
/// - Conversation bonus: rules.conversation.hearts per finished client talk.
/// - Side quest: full EXP minus expPerWrong per wrong placement (never below
///   0); hearts minus one at each threshold in heartsLostAtWrong.
/// - Case: full EXP / hearts minus, per mistake the Auditor found and per
///   extra submission, the configured amounts (never below 0). Perfect =
///   first submission with no mistakes.
/// - Level maximum: every source at its perfect value.
/// - Passing: earned EXP and earned hearts are BOTH at least passingPercent
///   of their maximum.
/// </summary>
/// <summary>Side-quest badge tier (higher is better). None = not earned.</summary>
public enum BadgeTier
{
    None = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3
}

public static class LevelScoring
{
    /// <summary>
    /// Badge tier for a finished side quest: Gold for a perfect run, Silver
    /// when its EXP reaches the passing percent, Bronze otherwise (finishing
    /// always earns the badge).
    /// </summary>
    public static BadgeTier SideQuestBadge(LevelRewardRules rules, Award award)
    {
        if (award.Perfect) return BadgeTier.Gold;
        return Percent(award.Exp, award.MaxExp) >= rules.passingPercent ? BadgeTier.Silver : BadgeTier.Bronze;
    }

    /// <summary>
    /// Accuracy badge from correct decisions (Level 2): Gold at goldAtCorrect,
    /// Silver at silverAtCorrect or more, otherwise Bronze. Never inferred
    /// from the forgiving EXP.
    /// </summary>
    public static BadgeTier AccuracyBadge(LevelRewardRules rules, int correct)
    {
        var r = rules.sideQuest;
        if (r.goldAtCorrect > 0 && correct >= r.goldAtCorrect) return BadgeTier.Gold;
        if (r.silverAtCorrect > 0 && correct >= r.silverAtCorrect) return BadgeTier.Silver;
        return BadgeTier.Bronze;
    }

    /// <summary>True when this level's side quest uses the accuracy badge.</summary>
    public static bool UsesAccuracyBadge(LevelRewardRules rules) => rules != null && rules.sideQuest.goldAtCorrect > 0;

    public struct Award
    {
        public int Exp;
        public int Hearts;
        public int MaxExp;
        public int MaxHearts;
        public bool Perfect => Exp == MaxExp && Hearts == MaxHearts;
    }

    public static Award SideQuest(LevelRewardRules rules, int wrongPlacements)
    {
        var r = rules.sideQuest;
        int wrong = Math.Max(0, wrongPlacements);
        int heartsLost = 0;
        if (r.heartsLostAtWrong != null)
            for (int i = 0; i < r.heartsLostAtWrong.Length; i++)
                if (wrong >= r.heartsLostAtWrong[i] && r.heartsLostAtWrong[i] > 0) heartsLost++;

        return new Award
        {
            Exp = Math.Max(Math.Max(0, r.minExp), r.exp - wrong * r.expPerWrong),
            Hearts = Math.Max(0, r.hearts - heartsLost),
            MaxExp = r.exp,
            MaxHearts = r.hearts
        };
    }

    /// <summary>
    /// Case result. <paramref name="extraExpLoss"/>/<paramref name="extraHeartLoss"/>
    /// add a level's own consequence (Level 2: the first missed cutoff); the
    /// rules' maxExpLoss/maxHeartLoss cap the combined deduction when set.
    /// </summary>
    public static Award Case(LevelRewardRules rules, int mistakes, int retries, int extraExpLoss = 0, int extraHeartLoss = 0)
    {
        var r = rules.caseFile;
        int m = Math.Max(0, mistakes);
        int t = Math.Max(0, retries);
        int expLoss = m * r.expPerMistake + t * r.expPerRetry + Math.Max(0, extraExpLoss);
        int heartLoss = m * r.heartsPerMistake + t * r.heartsPerRetry + Math.Max(0, extraHeartLoss);
        if (r.maxExpLoss > 0) expLoss = Math.Min(expLoss, r.maxExpLoss);
        if (r.maxHeartLoss > 0) heartLoss = Math.Min(heartLoss, r.maxHeartLoss);
        return new Award
        {
            Exp = Math.Max(0, r.exp - expLoss),
            Hearts = Math.Max(0, r.hearts - heartLoss),
            MaxExp = r.exp,
            MaxHearts = r.hearts
        };
    }

    /// <summary>What one failed audit with this many mistakes costs (for the notice).</summary>
    public static Award FailedAuditCost(LevelRewardRules rules, int mistakes)
    {
        var r = rules.caseFile;
        int m = Math.Max(0, mistakes);
        return new Award
        {
            Exp = m * r.expPerMistake + r.expPerRetry,
            Hearts = m * r.heartsPerMistake + r.heartsPerRetry
        };
    }

    public static int MaxExp(LevelRewardRules rules, int cases) =>
        rules.sideQuest.exp + Math.Max(0, cases) * rules.caseFile.exp;

    public static int MaxHearts(LevelRewardRules rules, int cases) =>
        rules.conversation.hearts * rules.conversation.count + rules.sideQuest.hearts +
        Math.Max(0, cases) * rules.caseFile.hearts;

    public static int Percent(int earned, int max) =>
        max <= 0 ? 100 : (int)Math.Floor(100.0 * Math.Max(0, earned) / max);

    public static bool Passed(LevelRewardRules rules, int exp, int hearts, int maxExp, int maxHearts) =>
        Percent(exp, maxExp) >= rules.passingPercent && Percent(hearts, maxHearts) >= rules.passingPercent;
}
