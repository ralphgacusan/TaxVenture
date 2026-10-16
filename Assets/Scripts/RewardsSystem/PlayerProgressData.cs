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
}

/// <summary>Best tier of one collected badge (0 none, 1 bronze, 2 silver, 3 gold).</summary>
[Serializable]
public class BadgeRecord
{
    public string badgeId;
    public int tier;
}

/// <summary>
/// Saved progression. Save version 2: a level is COMPLETED when finished at
/// any score, and PASSED when finished at or above its passing line; only a
/// passed level unlocks the next one. Each level keeps its best result, and
/// the career totals grow only by improvements on that best (replays never
/// farm EXP). Saves from version 1 treat every completed level as passed.
/// </summary>
[Serializable]
public class PlayerProgressData
{
    public const int CurrentSaveVersion = 2;

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
        if (levelRecords.RemoveAll(r => r == null || string.IsNullOrWhiteSpace(r.levelId)) > 0)
            changed = true;

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
