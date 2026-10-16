using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The editable reward rules of one level, from
/// Assets/Resources/Levels/&lt;levelId&gt;/rewards.json (same data-in-JSON
/// pattern as level.json and objectives.json): maximum EXP / hearts per
/// source, deductions, the passing percentage and the side-quest badge.
/// A missing or unreadable file falls back to the built-in defaults below
/// (the Level 1 values) with a warning.
///
/// SAFETY: after loading, Sanitize() clamps every number into a sane range
/// (no negative rewards, nothing above MaxAmount, passingPercent 1..100),
/// cleans the lists and logs ONE warning naming every value it had to fix,
/// so a typo in the JSON never breaks scoring or saves silently. The file is
/// read once per level id; every caller gets its own copy.
/// </summary>
[Serializable]
public class LevelRewardRules
{
    [Serializable]
    public class ConversationRules
    {
        public int hearts = 1;
        public int count = 4;
    }

    [Serializable]
    public class SideQuestRules
    {
        public int exp = 50;
        public int hearts = 3;
        public int expPerWrong = 5;
        public int[] heartsLostAtWrong = { 1, 3, 5 };
        public string badgeId = "know_your_taxpayer_1";
        public string badgeName = "Know Your Taxpayer";
        /// <summary>Centre emblem of the medal (HudBadgeArt): taxpayer_id, receipt, star.</summary>
        public string badgeEmblem = "taxpayer_id";
        /// <summary>Ribbon colour of the medal, as #RRGGBB.</summary>
        public string badgeRibbon = "#2F8FD8";
    }

    [Serializable]
    public class CaseRules
    {
        public int exp = 100;
        public int hearts = 5;
        public int expPerMistake = 15;
        public int heartsPerMistake = 1;
        public int expPerRetry = 10;
        public int heartsPerRetry = 1;
        /// <summary>
        /// EXP for a case finished WITHOUT an Auditor audit whose verdict was
        /// wrong (no hearts). Cases that go through the Auditor ignore this.
        /// </summary>
        public int wrongVerdictExp = 20;
    }

    /// <summary>Highest value any single reward number may have.</summary>
    public const int MaxAmount = 100000;

    public int passingPercent = 80;
    public bool requireSideQuestBeforeOffice = true;
    /// <summary>Door objects (scene names) kept shut until the side quest is done.</summary>
    public string[] officeDoors = { "Office_Entrance_Door_1", "Office_Entrance_Door_2" };
    public ConversationRules conversation = new ConversationRules();
    public SideQuestRules sideQuest = new SideQuestRules();
    public CaseRules caseFile = new CaseRules();

    public static string ResourcePath(string levelId) => $"Levels/{levelId}/rewards";

    private static readonly Dictionary<string, LevelRewardRules> cache =
        new Dictionary<string, LevelRewardRules>(StringComparer.Ordinal);

    /// <summary>Forgets cached rules (e.g. after editing rewards.json in Play Mode).</summary>
    public static void ClearCache() => cache.Clear();

    /// <summary>
    /// The level's rules: read from rewards.json once per level id,
    /// validated, and returned as a fresh copy the caller may keep.
    /// </summary>
    public static LevelRewardRules Load(string levelId)
    {
        string key = levelId ?? string.Empty;
        if (!cache.TryGetValue(key, out LevelRewardRules rules))
        {
            rules = LoadFromResources(levelId);
            cache[key] = rules;
        }
        return rules.Clone();
    }

    /// <summary>A deep copy (JSON round trip).</summary>
    public LevelRewardRules Clone()
    {
        LevelRewardRules copy = JsonUtility.FromJson<LevelRewardRules>(JsonUtility.ToJson(this));
        copy.Sanitize();
        return copy;
    }

    private static LevelRewardRules LoadFromResources(string levelId)
    {
        string path = ResourcePath(levelId);
        TextAsset file = Resources.Load<TextAsset>(path);
        if (file == null)
        {
            Debug.LogWarning($"[LevelRewardRules] Resources/{path}.json not found. Using built-in reward rules.");
            return new LevelRewardRules();
        }

        try
        {
            LevelRewardRules rules = JsonUtility.FromJson<LevelRewardRules>(file.text);
            if (rules == null) return new LevelRewardRules();
            List<string> fixes = rules.Sanitize();
            if (fixes.Count > 0)
                Debug.LogWarning($"[LevelRewardRules] {path}.json had values out of range; using: " + string.Join("; ", fixes));
            return rules;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LevelRewardRules] {path}.json is not valid JSON ({e.Message}). Using built-in reward rules.");
            return new LevelRewardRules();
        }
    }

    /// <summary>
    /// Clamps every value into a sensible range (never negative rewards, a
    /// 0% pass or an overflowing total) and cleans the lists. Returns a
    /// description of each value it changed (empty when the file was fine).
    /// </summary>
    public List<string> Sanitize()
    {
        var fixes = new List<string>();
        passingPercent = Fix(fixes, "passingPercent", passingPercent, 1, 100);
        if (conversation == null) { conversation = new ConversationRules(); fixes.Add("conversation restored"); }
        if (sideQuest == null) { sideQuest = new SideQuestRules(); fixes.Add("sideQuest restored"); }
        if (caseFile == null) { caseFile = new CaseRules(); fixes.Add("caseFile restored"); }

        conversation.hearts = Fix(fixes, "conversation.hearts", conversation.hearts, 0, MaxAmount);
        conversation.count = Fix(fixes, "conversation.count", conversation.count, 0, 1000);

        sideQuest.exp = Fix(fixes, "sideQuest.exp", sideQuest.exp, 0, MaxAmount);
        sideQuest.hearts = Fix(fixes, "sideQuest.hearts", sideQuest.hearts, 0, MaxAmount);
        sideQuest.expPerWrong = Fix(fixes, "sideQuest.expPerWrong", sideQuest.expPerWrong, 0, MaxAmount);
        sideQuest.heartsLostAtWrong = CleanThresholds(sideQuest.heartsLostAtWrong, fixes);
        sideQuest.badgeId = (sideQuest.badgeId ?? string.Empty).Trim();
        sideQuest.badgeName = string.IsNullOrWhiteSpace(sideQuest.badgeName) ? sideQuest.badgeId : sideQuest.badgeName.Trim();

        caseFile.exp = Fix(fixes, "caseFile.exp", caseFile.exp, 0, MaxAmount);
        caseFile.hearts = Fix(fixes, "caseFile.hearts", caseFile.hearts, 0, MaxAmount);
        caseFile.expPerMistake = Fix(fixes, "caseFile.expPerMistake", caseFile.expPerMistake, 0, MaxAmount);
        caseFile.heartsPerMistake = Fix(fixes, "caseFile.heartsPerMistake", caseFile.heartsPerMistake, 0, MaxAmount);
        caseFile.expPerRetry = Fix(fixes, "caseFile.expPerRetry", caseFile.expPerRetry, 0, MaxAmount);
        caseFile.heartsPerRetry = Fix(fixes, "caseFile.heartsPerRetry", caseFile.heartsPerRetry, 0, MaxAmount);
        caseFile.wrongVerdictExp = Fix(fixes, "caseFile.wrongVerdictExp", caseFile.wrongVerdictExp, 0, caseFile.exp);

        officeDoors = CleanNames(officeDoors);
        return fixes;
    }

    private static int Fix(List<string> fixes, string name, int value, int min, int max)
    {
        int fixedValue = Mathf.Clamp(value, min, Mathf.Max(min, max));
        if (fixedValue != value) fixes.Add($"{name} {value} -> {fixedValue}");
        return fixedValue;
    }

    /// <summary>Positive, distinct, ascending wrong-placement counts.</summary>
    private static int[] CleanThresholds(int[] values, List<string> fixes)
    {
        if (values == null) return new int[0];
        var set = new SortedSet<int>();
        foreach (int v in values)
            if (v > 0) set.Add(v);
        var cleaned = new int[set.Count];
        set.CopyTo(cleaned);
        bool same = cleaned.Length == values.Length;
        for (int i = 0; same && i < cleaned.Length; i++) same = cleaned[i] == values[i];
        if (!same) fixes.Add("sideQuest.heartsLostAtWrong -> [" + string.Join(", ", cleaned) + "]");
        return cleaned;
    }

    private static string[] CleanNames(string[] names)
    {
        var list = new List<string>();
        if (names != null)
            foreach (string n in names)
                if (!string.IsNullOrWhiteSpace(n) && !list.Contains(n.Trim())) list.Add(n.Trim());
        return list.ToArray();
    }
}
