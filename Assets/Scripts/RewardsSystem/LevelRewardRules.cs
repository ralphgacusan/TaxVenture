using System;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The editable reward rules of one level, from
/// Assets/Resources/Levels/&lt;levelId&gt;/rewards.json (same data-in-JSON
/// pattern as level.json and objectives.json): maximum EXP / hearts per
/// source, deductions, the passing percentage and the side-quest badge.
/// A missing or unreadable file falls back to the built-in defaults below
/// (the Level 1 values) with a warning.
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
    }

    public int passingPercent = 80;
    public bool requireSideQuestBeforeOffice = true;
    /// <summary>Door objects (scene names) kept shut until the side quest is done.</summary>
    public string[] officeDoors = { "Office_Entrance_Door_1", "Office_Entrance_Door_2" };
    public ConversationRules conversation = new ConversationRules();
    public SideQuestRules sideQuest = new SideQuestRules();
    public CaseRules caseFile = new CaseRules();

    public static string ResourcePath(string levelId) => $"Levels/{levelId}/rewards";

    public static LevelRewardRules Load(string levelId)
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
            rules.Sanitize();
            return rules;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LevelRewardRules] {path}.json is not valid JSON ({e.Message}). Using built-in reward rules.");
            return new LevelRewardRules();
        }
    }

    /// <summary>Clamps every value into a sensible range (never negative rewards or a 0% pass).</summary>
    public void Sanitize()
    {
        passingPercent = Mathf.Clamp(passingPercent, 1, 100);
        if (conversation == null) conversation = new ConversationRules();
        if (sideQuest == null) sideQuest = new SideQuestRules();
        if (caseFile == null) caseFile = new CaseRules();
        conversation.hearts = Mathf.Max(0, conversation.hearts);
        conversation.count = Mathf.Max(0, conversation.count);
        sideQuest.exp = Mathf.Max(0, sideQuest.exp);
        sideQuest.hearts = Mathf.Max(0, sideQuest.hearts);
        sideQuest.expPerWrong = Mathf.Max(0, sideQuest.expPerWrong);
        if (sideQuest.heartsLostAtWrong == null) sideQuest.heartsLostAtWrong = new int[0];
        caseFile.exp = Mathf.Max(0, caseFile.exp);
        caseFile.hearts = Mathf.Max(0, caseFile.hearts);
        caseFile.expPerMistake = Mathf.Max(0, caseFile.expPerMistake);
        caseFile.heartsPerMistake = Mathf.Max(0, caseFile.heartsPerMistake);
        caseFile.expPerRetry = Mathf.Max(0, caseFile.expPerRetry);
        caseFile.heartsPerRetry = Mathf.Max(0, caseFile.heartsPerRetry);
    }
}
