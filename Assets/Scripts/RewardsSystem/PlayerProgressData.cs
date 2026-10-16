using System;
using System.Collections.Generic;

/// <summary>Best result saved for one level (save version 2).</summary>
[Serializable]
public class LevelRecord
{
    public string levelId;
    public int bestExp;
    public int bestHearts;
    public int maxExp;
    public int maxHearts;
    public bool passed;
    public int attempts;
    public List<string> badges = new List<string>();
    // Time spent in whole seconds (0 = never measured; older saves have none).
    public int bestSeconds;
    public int lastSeconds;
    public int totalSeconds;
    public int sideQuestBestSeconds;
    public int sideQuestLastSeconds;
}

/// <summary>Best tier of one collected badge (0 none, 1 bronze, 2 silver, 3 gold).</summary>
[Serializable]
public class BadgeRecord
{
    public string badgeId;
    public int tier;
}

/// <summary>
/// Saved progression. A level is COMPLETED when finished at any score, and
/// PASSED when finished at or above its passing line; only a passed level
/// unlocks the next one. Each level keeps its best result, and the career
/// totals grow only by improvements on that best (replays never farm EXP).
///
/// VERSIONS: 1 had no pass line (every completed level counts as passed);
/// 2 added best-result records and badges; 3 is written signed and
/// crash-safe by PlayerProgressManager (same fields). Normalize() upgrades
/// any older save in place and repairs impossible values (negative totals,
/// a best above its maximum, duplicate ids, badge tiers out of range), so
/// whatever is on disk always loads as a consistent save.
/// </summary>
[Serializable]
public class PlayerProgressData
{
    public const int CurrentSaveVersion = 3;
    /// <summary>Highest badge tier (gold).</summary>
    public const int MaxBadgeTier = 3;

    public int saveVersion;
    public int totalExp;
    public int totalReputation;
    public int levelsCompleted;
    public List<string> completedLevelIds = new List<string>();
    public List<string> passedLevelIds = new List<string>();
    public List<LevelRecord> levelRecords = new List<LevelRecord>();
    public List<BadgeRecord> badgeRecords = new List<BadgeRecord>();

    /// <summary>Best tier collected for a badge (0 = not collected).</summary>
    public int GetBadgeTier(string badgeId)
    {
        if (string.IsNullOrWhiteSpace(badgeId) || badgeRecords == null) return 0;
        foreach (BadgeRecord record in badgeRecords)
            if (record != null && record.badgeId == badgeId.Trim()) return record.tier;
        return 0;
    }

    /// <summary>Keeps the best tier of a badge; returns true when it is new or upgraded.</summary>
    public bool RecordBadge(string badgeId, int tier)
    {
        if (string.IsNullOrWhiteSpace(badgeId) || tier <= 0) return false;
        tier = Math.Min(tier, MaxBadgeTier);
        if (badgeRecords == null) badgeRecords = new List<BadgeRecord>();
        string id = badgeId.Trim();
        foreach (BadgeRecord record in badgeRecords)
        {
            if (record == null || record.badgeId != id) continue;
            if (tier <= record.tier) return false;
            record.tier = tier;
            return true;
        }
        badgeRecords.Add(new BadgeRecord { badgeId = id, tier = tier });
        return true;
    }

    public bool IsLevelUnlocked(int levelNumber)
    {
        if (levelNumber == 1)
            return true;

        if (levelNumber < 1 || passedLevelIds == null)
            return false;

        return passedLevelIds.Contains($"Level_{levelNumber - 1:00}");
    }

    public bool IsLevelPassed(string levelId) =>
        !string.IsNullOrWhiteSpace(levelId) && passedLevelIds != null && passedLevelIds.Contains(levelId.Trim());

    public LevelRecord GetRecord(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId) || levelRecords == null) return null;
        string id = levelId.Trim();
        foreach (LevelRecord record in levelRecords)
            if (record != null && record.levelId == id) return record;
        return null;
    }

    /// <summary>
    /// Saves a finished run: keeps the best EXP / hearts, marks the level
    /// passed (never un-passes it), adds badges, and returns how much the
    /// career totals grew (only the improvement over the previous best).
    /// </summary>
    public (int expGain, int heartsGain, bool newBest) RecordLevelResult(
        string levelId, int exp, int hearts, int maxExp, int maxHearts, bool passed, IEnumerable<string> badges)
    {
        if (string.IsNullOrWhiteSpace(levelId))
            return (0, 0, false);

        Normalize();
        string id = levelId.Trim();

        // A run can't score above its own maximum (bad caller or bad data).
        maxExp = Math.Max(0, maxExp);
        maxHearts = Math.Max(0, maxHearts);
        exp = Math.Max(0, maxExp > 0 ? Math.Min(exp, maxExp) : exp);
        hearts = Math.Max(0, maxHearts > 0 ? Math.Min(hearts, maxHearts) : hearts);

        if (!completedLevelIds.Contains(id))
            completedLevelIds.Add(id);
        levelsCompleted = completedLevelIds.Count;
        if (passed && !passedLevelIds.Contains(id))
            passedLevelIds.Add(id);

        LevelRecord record = GetRecord(id);
        bool first = record == null;
        if (first)
        {
            record = new LevelRecord { levelId = id };
            levelRecords.Add(record);
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

        totalExp += expGain;
        totalReputation += heartsGain;
        return (expGain, heartsGain, newBest);
    }

    /// <summary>
    /// Adds a finished run's time to that level's record (call after RecordLevelResult):
    /// fastest run, last run and total time played; the side quest keeps its fastest
    /// and last time. Times of 0 or less are ignored. Returns false without a record.
    /// </summary>
    public bool RecordLevelTime(string levelId, float runSeconds, float sideQuestSeconds)
    {
        LevelRecord record = GetRecord(levelId);
        if (record == null) return false;
        int run = ToSeconds(runSeconds);
        if (run > 0)
        {
            record.lastSeconds = run;
            record.bestSeconds = record.bestSeconds > 0 ? Math.Min(record.bestSeconds, run) : run;
            record.totalSeconds = (int)Math.Min(int.MaxValue, (long)record.totalSeconds + run);
        }
        int side = ToSeconds(sideQuestSeconds);
        if (side > 0)
        {
            record.sideQuestLastSeconds = side;
            record.sideQuestBestSeconds = record.sideQuestBestSeconds > 0 ? Math.Min(record.sideQuestBestSeconds, side) : side;
        }
        return run > 0 || side > 0;
    }

    private static int ToSeconds(float seconds) =>
        float.IsNaN(seconds) || seconds <= 0f ? 0 : seconds >= int.MaxValue ? int.MaxValue : (int)Math.Round(seconds);

    public bool RecordLevelCompletion(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
            return false;

        Normalize();

        string normalizedLevelId = levelId.Trim();
        if (completedLevelIds.Contains(normalizedLevelId))
            return false;

        completedLevelIds.Add(normalizedLevelId);
        if (!passedLevelIds.Contains(normalizedLevelId))
            passedLevelIds.Add(normalizedLevelId);
        levelsCompleted = completedLevelIds.Count;
        return true;
    }

    public bool Normalize()
    {
        bool changed = false;

        if (completedLevelIds == null)
        {
            completedLevelIds = new List<string>();
            changed = true;
        }

        if (passedLevelIds == null)
        {
            passedLevelIds = new List<string>();
            changed = true;
        }

        if (levelRecords == null)
        {
            levelRecords = new List<LevelRecord>();
            changed = true;
        }

        if (badgeRecords == null)
        {
            badgeRecords = new List<BadgeRecord>();
            changed = true;
        }

        if (saveVersion == 0 && levelsCompleted > 0 && completedLevelIds.Count == 0)
        {
            completedLevelIds.Add("Level_01");
            changed = true;
        }

        completedLevelIds = CleanIds(completedLevelIds, ref changed);

        // Version 1 had no pass line: every level finished then counts as passed.
        if (saveVersion < 2)
        {
            foreach (string levelId in completedLevelIds)
            {
                if (!passedLevelIds.Contains(levelId))
                {
                    passedLevelIds.Add(levelId);
                    changed = true;
                }
            }
        }

        passedLevelIds = CleanIds(passedLevelIds, ref changed);

        // A passed level is always a completed one.
        foreach (string levelId in passedLevelIds)
        {
            if (!completedLevelIds.Contains(levelId))
            {
                completedLevelIds.Add(levelId);
                changed = true;
            }
        }

        if (levelRecords.RemoveAll(r => r == null || string.IsNullOrWhiteSpace(r.levelId)) > 0)
            changed = true;
        changed |= RepairRecords();
        changed |= RepairBadges();

        if (totalExp < 0) { totalExp = 0; changed = true; }
        if (totalReputation < 0) { totalReputation = 0; changed = true; }

        int uniqueLevelCount = completedLevelIds.Count;
        if (levelsCompleted != uniqueLevelCount)
        {
            levelsCompleted = uniqueLevelCount;
            changed = true;
        }

        if (saveVersion < CurrentSaveVersion)
        {
            saveVersion = CurrentSaveVersion;
            changed = true;
        }

        return changed;
    }

    /// <summary>One record per level, values inside their own maximum.</summary>
    private bool RepairRecords()
    {
        bool changed = false;
        var seen = new Dictionary<string, LevelRecord>(StringComparer.Ordinal);
        var kept = new List<LevelRecord>(levelRecords.Count);
        foreach (LevelRecord record in levelRecords)
        {
            string id = record.levelId.Trim();
            if (id != record.levelId) { record.levelId = id; changed = true; }

            if (record.maxExp < 0) { record.maxExp = 0; changed = true; }
            if (record.maxHearts < 0) { record.maxHearts = 0; changed = true; }
            int exp = Math.Max(0, record.maxExp > 0 ? Math.Min(record.bestExp, record.maxExp) : record.bestExp);
            int hearts = Math.Max(0, record.maxHearts > 0 ? Math.Min(record.bestHearts, record.maxHearts) : record.bestHearts);
            if (exp != record.bestExp || hearts != record.bestHearts)
            {
                record.bestExp = exp;
                record.bestHearts = hearts;
                changed = true;
            }
            if (record.attempts < 0) { record.attempts = 0; changed = true; }
            if (record.bestSeconds < 0) { record.bestSeconds = 0; changed = true; }
            if (record.lastSeconds < 0) { record.lastSeconds = 0; changed = true; }
            if (record.totalSeconds < 0) { record.totalSeconds = 0; changed = true; }
            if (record.sideQuestBestSeconds < 0) { record.sideQuestBestSeconds = 0; changed = true; }
            if (record.sideQuestLastSeconds < 0) { record.sideQuestLastSeconds = 0; changed = true; }
            if (record.badges == null) { record.badges = new List<string>(); changed = true; }
            record.badges = CleanIds(record.badges, ref changed);

            if (seen.TryGetValue(id, out LevelRecord first))
            {
                // Duplicate entries for one level merge into the best of both.
                first.bestExp = Math.Max(first.bestExp, record.bestExp);
                first.bestHearts = Math.Max(first.bestHearts, record.bestHearts);
                first.passed |= record.passed;
                first.attempts += record.attempts;
                first.bestSeconds = MinPositive(first.bestSeconds, record.bestSeconds);
                first.sideQuestBestSeconds = MinPositive(first.sideQuestBestSeconds, record.sideQuestBestSeconds);
                first.totalSeconds = (int)Math.Min(int.MaxValue, (long)first.totalSeconds + record.totalSeconds);
                foreach (string badge in record.badges)
                    if (!first.badges.Contains(badge)) first.badges.Add(badge);
                changed = true;
                continue;
            }
            seen[id] = record;
            kept.Add(record);

            if (record.passed && !passedLevelIds.Contains(id)) { passedLevelIds.Add(id); changed = true; }
            if (!completedLevelIds.Contains(id)) { completedLevelIds.Add(id); changed = true; }
        }
        levelRecords = kept;
        return changed;
    }

    private static int MinPositive(int a, int b) => a <= 0 ? b : b <= 0 ? a : Math.Min(a, b);

    /// <summary>One entry per badge, tier 0..MaxBadgeTier; empty ids removed.</summary>
    private bool RepairBadges()
    {
        bool changed = false;
        var best = new Dictionary<string, BadgeRecord>(StringComparer.Ordinal);
        var kept = new List<BadgeRecord>(badgeRecords.Count);
        foreach (BadgeRecord badge in badgeRecords)
        {
            if (badge == null || string.IsNullOrWhiteSpace(badge.badgeId)) { changed = true; continue; }
            string id = badge.badgeId.Trim();
            int tier = Math.Max(0, Math.Min(MaxBadgeTier, badge.tier));
            if (id != badge.badgeId || tier != badge.tier) { badge.badgeId = id; badge.tier = tier; changed = true; }
            if (best.TryGetValue(id, out BadgeRecord first))
            {
                first.tier = Math.Max(first.tier, tier);
                changed = true;
                continue;
            }
            best[id] = badge;
            kept.Add(badge);
        }
        badgeRecords = kept;
        return changed;
    }

    private static List<string> CleanIds(List<string> ids, ref bool changed)
    {
        var cleanedIds = new List<string>(ids.Count);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (string levelId in ids)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                changed = true;
                continue;
            }

            string cleanedId = levelId.Trim();
            if (cleanedId != levelId)
                changed = true;

            if (!seenIds.Add(cleanedId))
            {
                changed = true;
                continue;
            }

            cleanedIds.Add(cleanedId);
        }

        return cleanedIds;
    }
}
