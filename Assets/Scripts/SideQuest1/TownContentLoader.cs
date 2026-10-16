using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Loads the editable town content for a level,
/// Resources/Levels/&lt;levelId&gt;/town.json (Level 1: Levels/Level_01/town.json),
/// and applies it over the built-in text:
/// - each plaza NPC's conversation (TownNpcDialogueLibrary),
/// - each taxpayer profile's description (TaxpayerProfileDatabase),
/// - Arthur's corkboard introduction / wrap-up and his reaction to every
///   card placed (CorkboardTeaching).
///
/// Missing file or invalid entries: the built-in text is kept and a Console
/// warning says why. Runs automatically when a level scene loads; other
/// scenes go back to the built-in text. Classifications (the answer key)
/// stay in code on purpose.
/// </summary>
public static class TownContentLoader
{
    // Filled by JsonUtility at runtime, so the compiler sees no assignments.
#pragma warning disable 0649
    [Serializable]
    private class TownFile
    {
        public TownTaxpayer[] taxpayers;
        public TownLine[] arthurBoardIntro;
        public TownLine[] arthurBoardDone;
        public SideQuestSplash.SplashContent sideQuestSplash;
    }

    [Serializable]
    private class TownTaxpayer
    {
        public string npcId;
        public string description;
        public TownLine[] talk;
        public string placedRight;
        public string hintCitizenship;
        public string hintResidence;
        public string hintBoth;
    }

    [Serializable]
    private class TownLine
    {
        public string who;
        public string text;
    }
#pragma warning restore 0649

    private static readonly Dictionary<string, string> SceneLevels = new Dictionary<string, string>
    {
        ["Office_Level1"] = "Level_01"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        if (SceneLevels.TryGetValue(scene.name, out string levelId)) Load(levelId);
        else Clear();
    }

    public static string ResourcePath(string levelId) => $"Levels/{levelId}/town";

    /// <summary>Back to the built-in text everywhere.</summary>
    public static void Clear()
    {
        SideQuestSplash.Content = new SideQuestSplash.SplashContent();
        TownNpcDialogueLibrary.ClearOverrides();
        TaxpayerProfileDatabase.ClearOverrides();
        CorkboardTeaching.ClearOverrides();
    }

    public static void Load(string levelId)
    {
        Clear();
        string path = ResourcePath(levelId);
        TextAsset file = Resources.Load<TextAsset>(path);
        if (file == null)
        {
            Debug.Log($"[TownContent] Resources/{path}.json not found; using the built-in town text.");
            return;
        }

        TownFile parsed;
        try
        {
            parsed = JsonUtility.FromJson<TownFile>(file.text);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TownContent] {path}.json is not valid JSON ({e.Message}). Using the built-in town text.");
            return;
        }
        if (parsed == null) return;

        var problems = new System.Text.StringBuilder();

        if (parsed.taxpayers != null)
        {
            foreach (TownTaxpayer t in parsed.taxpayers)
            {
                if (t == null || string.IsNullOrWhiteSpace(t.npcId)) continue;
                TaxpayerProfile profile = TaxpayerProfileDatabase.GetByNpcId(t.npcId);
                if (profile == null || !TownNpcDialogueLibrary.HasDialogueFor(t.npcId))
                {
                    problems.AppendLine($"- unknown npcId \"{t.npcId}\"; skipped.");
                    continue;
                }

                List<KeyValuePair<bool, string>> talk = ToLines(t.talk, out string talkProblem);
                if (talkProblem == null && (!talk.Exists(l => l.Key) || !talk.Exists(l => !l.Key)))
                    talkProblem = "needs at least one \"you\" line and one NPC line";
                if (talkProblem != null) problems.AppendLine($"- {t.npcId} talk: {talkProblem}; built-in talk kept.");
                else TownNpcDialogueLibrary.SetOverride(t.npcId, talk);

                if (!string.IsNullOrWhiteSpace(t.description))
                    TaxpayerProfileDatabase.SetDescription(profile.ProfileId, t.description.Trim());

                if (!string.IsNullOrWhiteSpace(t.placedRight) && !string.IsNullOrWhiteSpace(t.hintCitizenship) &&
                    !string.IsNullOrWhiteSpace(t.hintResidence) && !string.IsNullOrWhiteSpace(t.hintBoth))
                {
                    CorkboardTeaching.SetReaction(profile.ProfileId, new CorkboardTeaching.Reaction
                    {
                        PlacedRight = t.placedRight.Trim(),
                        HintCitizenship = t.hintCitizenship.Trim(),
                        HintResidence = t.hintResidence.Trim(),
                        HintBoth = t.hintBoth.Trim()
                    });
                }
                else if (!string.IsNullOrWhiteSpace(t.placedRight) || !string.IsNullOrWhiteSpace(t.hintCitizenship))
                {
                    problems.AppendLine($"- {t.npcId}: corkboard reactions need placedRight, hintCitizenship, hintResidence and hintBoth; built-in kept.");
                }
            }
        }

        List<KeyValuePair<bool, string>> intro = ToLines(parsed.arthurBoardIntro, out string introProblem);
        if (parsed.arthurBoardIntro != null && parsed.arthurBoardIntro.Length > 0)
        {
            if (introProblem != null) problems.AppendLine($"- arthurBoardIntro: {introProblem}; built-in kept.");
            else CorkboardTeaching.SetIntro(intro);
        }

        List<KeyValuePair<bool, string>> done = ToLines(parsed.arthurBoardDone, out string doneProblem);
        if (parsed.arthurBoardDone != null && parsed.arthurBoardDone.Length > 0)
        {
            if (doneProblem != null) problems.AppendLine($"- arthurBoardDone: {doneProblem}; built-in kept.");
            else CorkboardTeaching.SetDone(done);
        }

        // The Side Quest 1 title card (missing fields keep their defaults).
        if (parsed.sideQuestSplash != null && !string.IsNullOrWhiteSpace(parsed.sideQuestSplash.title))
            SideQuestSplash.Content = parsed.sideQuestSplash;

        if (problems.Length > 0) Debug.LogWarning($"[TownContent] {path}.json:\n{problems}");
    }

    /// <summary>"who": "you" (or "player") = the player; anything else = the NPC.</summary>
    private static List<KeyValuePair<bool, string>> ToLines(TownLine[] lines, out string problem)
    {
        problem = null;
        var result = new List<KeyValuePair<bool, string>>();
        if (lines == null || lines.Length == 0)
        {
            problem = "no lines";
            return result;
        }
        for (int i = 0; i < lines.Length; i++)
        {
            TownLine line = lines[i];
            if (line == null || string.IsNullOrWhiteSpace(line.text))
            {
                problem = $"line {i + 1} has no text";
                return result;
            }
            string who = (line.who ?? "").Trim().ToLowerInvariant();
            result.Add(new KeyValuePair<bool, string>(who == "you" || who == "player", line.text.Trim()));
        }
        return result;
    }
}
