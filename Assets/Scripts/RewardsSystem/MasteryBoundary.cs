using System;
using System.Collections.Generic;

/// <summary>
/// One finished level run as the mastery records see it (plain data, no scene
/// references). Badge tiers are ints (0 none .. 3 gold) so this stays free of
/// the Unity-side reward rules.
/// </summary>
public struct MasteryRun
{
    public string LevelId;
    public bool HasScore;
    public int Exp;
    public int Hearts;
    public int MaxExp;
    public int MaxHearts;
    public bool Passed;
    public IList<string> Badges;
    public IList<int> BadgeTiers;
    public float RunSeconds;
    public float SideQuestSeconds;
}

/// <summary>
/// PURPOSE:
/// The mastery boundary (Campaign Foundation T01). Everything here measures
/// LEARNING PERFORMANCE: EXP, accuracy "score hearts" (saved as
/// totalReputation / bestHearts), badges, best results, the legacy 80% pass
/// line and the legacy "previous level passed" unlock rule.
///
/// Pure C# (no UnityEngine) so the regression suites compile it directly. The
/// older entry points (LevelScoring.Percent / Passed,
/// PlayerProgressData.RecordLevelResult / IsLevelUnlocked, CareerProgress
/// chapter unlocks, PlayerProgressManager.ApplyLevelResult) forward here and
/// behave exactly as before.
///
/// SEAM FOR THE CAMPAIGN: score hearts are accuracy, never Public Trust; and
/// the Legacy* unlock answers are the CURRENT story gate only. Later tickets
/// keep using this class for mastery records but replace the story gate:
/// T02 adds the version-5 campaign record beside these fields, T07 moves
/// active-story unlocks to corrected chapter completion (this pass line
/// becomes descriptive), and T08 adds Public Trust as separate data. Do not
/// feed score hearts into trust or coins.
/// </summary>
public static class MasteryBoundary
{
    // =========================================================
    // PASS LINE (legacy: EXP and score hearts both at passingPercent)
    // =========================================================

    /// <summary>Whole percent of a maximum, rounded down; an empty maximum counts as 100.</summary>
    public static int Percent(int earned, int max) =>
        max <= 0 ? 100 : (int)Math.Floor(100.0 * Math.Max(0, earned) / max);

    /// <summary>Legacy pass: earned EXP and earned score hearts are BOTH at least passingPercent of their maximum.</summary>
    public static bool MeetsPassLine(int passingPercent, int exp, int hearts, int maxExp, int maxHearts) =>
        Percent(exp, maxExp) >= passingPercent && Percent(hearts, maxHearts) >= passingPercent;

    // =========================================================
    // RECORDS
    // =========================================================

    /// <summary>
    /// Saves a finished run: keeps the best EXP / score hearts, marks the level
    /// passed (never un-passes it), adds badges, and returns how much the
    /// career totals grew (only the improvement over the previous best).
    /// </summary>
    public static (int expGain, int heartsGain, bool newBest) RecordResult(PlayerProgressData data,
        string levelId, int exp, int hearts, int maxExp, int maxHearts, bool passed, IEnumerable<string> badges)
    {
        if (data == null || string.IsNullOrWhiteSpace(levelId))
            return (0, 0, false);

        data.Normalize();
        string id = levelId.Trim();

        // A run can't score above its own maximum (bad caller or bad data).
        maxExp = Math.Max(0, maxExp);
        maxHearts = Math.Max(0, maxHearts);
        exp = Math.Max(0, maxExp > 0 ? Math.Min(exp, maxExp) : exp);
        hearts = Math.Max(0, maxHearts > 0 ? Math.Min(hearts, maxHearts) : hearts);

        if (!data.completedLevelIds.Contains(id))
            data.completedLevelIds.Add(id);
        data.levelsCompleted = data.completedLevelIds.Count;
        if (passed && !data.passedLevelIds.Contains(id))
            data.passedLevelIds.Add(id);

        LevelRecord record = data.GetRecord(id);
        bool first = record == null;
        if (first)
        {
            record = new LevelRecord { levelId = id };
            data.levelRecords.Add(record);
        }

        int expGain = Math.Max(0, exp - record.bestExp);
        int heartsGain = Math.Max(0, hearts - record.bestHearts);
        bool newBest = first || exp > record.bestExp || hearts > record.bestHearts;

        record.bestExp = Math.Max(record.bestExp, Math.Max(0, exp));
        record.bestHearts = Math.Max(record.bestHearts, Math.Max(0, hearts));
        record.maxExp = maxExp;
        record.maxHearts = maxHearts;
        record.passed = record.passed || passed;
        record.attempts++;
        if (record.badges == null) record.badges = new List<string>();
        if (badges != null)
            foreach (string badge in badges)
                if (!string.IsNullOrWhiteSpace(badge) && !record.badges.Contains(badge))
                    record.badges.Add(badge);

        data.totalExp += expGain;
        data.totalReputation += heartsGain;
        return (expGain, heartsGain, newBest);
    }

    /// <summary>
    /// Folds one finished run into the mastery records in memory (no saving):
    /// a scored run keeps its best result, badge tiers and times; a run without
    /// a level score counts as a passed completion whose rewards become that
    /// level's best. The caller saves afterwards, as before.
    /// </summary>
    public static (int expGain, int heartsGain, bool newBest) ApplyRun(PlayerProgressData data, MasteryRun run)
    {
        if (data == null || string.IsNullOrWhiteSpace(run.LevelId))
            return (0, 0, false);

        if (!run.HasScore)
        {
            data.RecordLevelCompletion(run.LevelId);
            return RecordResult(data, run.LevelId, run.Exp, run.Hearts, 0, 0, true, run.Badges);
        }

        var gain = RecordResult(data, run.LevelId, run.Exp, run.Hearts, run.MaxExp, run.MaxHearts, run.Passed, run.Badges);

        // Collected badges keep their best tier (bronze / silver / gold).
        if (run.Badges != null && run.BadgeTiers != null)
            for (int i = 0; i < run.Badges.Count && i < run.BadgeTiers.Count; i++)
                data.RecordBadge(run.Badges[i], run.BadgeTiers[i]);

        // Time spent (career board: best / last / total time, side-quest time).
        data.RecordLevelTime(run.LevelId, run.RunSeconds, run.SideQuestSeconds);
        return gain;
    }

    // =========================================================
    // LEGACY UNLOCKS (current story gate; T07 replaces it for the campaign)
    // =========================================================

    /// <summary>Level 1 is always open; Level N opens once Level N-1 is PASSED.</summary>
    public static bool LegacyLevelUnlocked(PlayerProgressData data, int levelNumber)
    {
        if (levelNumber == 1)
            return true;

        if (levelNumber < 1 || data == null || data.passedLevelIds == null)
            return false;

        return data.passedLevelIds.Contains($"Level_{levelNumber - 1:00}");
    }

    /// <summary>A career chapter opens when it has no previous chapter or that chapter is PASSED.</summary>
    public static bool LegacyChapterUnlocked(PlayerProgressData data, string previousLevelId) =>
        previousLevelId == null || (data != null && data.IsLevelPassed(previousLevelId));
}
