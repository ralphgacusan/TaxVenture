using System;
using System.Collections.Generic;

[Serializable]
public class PlayerProgressData
{
    public int saveVersion;
    public int totalExp;
    public int totalReputation;
    public int levelsCompleted;
    public List<string> completedLevelIds = new List<string>();

    public bool IsLevelUnlocked(int levelNumber)
    {
        if (levelNumber == 1)
            return true;

        if (levelNumber < 1 || completedLevelIds == null)
            return false;

        return completedLevelIds.Contains($"Level_{levelNumber - 1:00}");
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

        if (saveVersion == 0 && levelsCompleted > 0 && completedLevelIds.Count == 0)
        {
            completedLevelIds.Add("Level_01");
            changed = true;
        }

        var cleanedIds = new List<string>(completedLevelIds.Count);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (string levelId in completedLevelIds)
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

        if (cleanedIds.Count != completedLevelIds.Count)
            changed = true;

        if (changed)
            completedLevelIds = cleanedIds;

        int uniqueLevelCount = completedLevelIds.Count;
        if (levelsCompleted != uniqueLevelCount)
        {
            levelsCompleted = uniqueLevelCount;
            changed = true;
        }

        if (saveVersion < 1)
        {
            saveVersion = 1;
            changed = true;
        }

        return changed;
    }
}
