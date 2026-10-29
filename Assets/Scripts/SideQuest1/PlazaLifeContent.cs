using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One short optional conversation of an ambient plaza NPC (Living Plaza).</summary>
[Serializable]
public class AmbientTalkDefinition
{
    public List<DialogueLineDefinition> lines = new List<DialogueLineDefinition>();
}

/// <summary>An ambient plaza NPC: who they are and the small talks they rotate through.</summary>
[Serializable]
public class AmbientNpcDefinition
{
    public string id;
    public string name;
    public string fullName;
    public string role;
    public string background;
    public string attitude;
    public List<AmbientTalkDefinition> talks = new List<AmbientTalkDefinition>();
}

/// <summary>Marie's one-liners while she is on her way to her case (talk only).</summary>
[Serializable]
public class MarieTravelDefinition
{
    public List<DialogueLineDefinition> toOffice = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> atReception = new List<DialogueLineDefinition>();
    public List<DialogueLineDefinition> toRoom = new List<DialogueLineDefinition>();
}

[Serializable]
public class PlazaLifeFile
{
    public List<AmbientNpcDefinition> npcs = new List<AmbientNpcDefinition>();
    public MarieTravelDefinition marieTravel = new MarieTravelDefinition();
}

/// <summary>
/// PURPOSE:
/// The editable words of Level 1's Living Plaza: the four ambient NPCs'
/// names, roles and rotating small talk, and Marie's lines on her way to
/// the office. Read from Resources/Levels/Level_01/plaza_life.json; a missing
/// or broken file (or entry) falls back to a short built-in line with one
/// warning, so nothing is ever silent or blocking.
///
/// These words are flavour only: nothing here can give an item, a profile,
/// EXP, coins, trust or progress (AmbientNpcInteractable only talks).
/// </summary>
public static class PlazaLifeContent
{
    public const string ResourcePath = "Levels/Level_01/plaza_life";
    /// <summary>Level 2's own townspeople (T32): the same format, its own people and words.</summary>
    public const string Level2ResourcePath = "Levels/Level_02/plaza_life";
    public const string PlayerPortrait = "Auditor_Default";

    private static PlazaLifeFile cached;
    private static string cachedPath;
    private static bool warned;

    /// <summary>The file of the level being played (read once per level; Level 1 unless Level 2's office is active).</summary>
    public static PlazaLifeFile Current
    {
        get
        {
            string path = PathFor(OfficeLevelProfile.LevelIdFor(UnityEngine.SceneManagement.SceneManager.GetActiveScene()));
            if (cached == null || cachedPath != path)
            {
                cachedPath = path;
                warned = false;
                cached = Load(path);
            }
            return cached;
        }
    }

    /// <summary>The plaza_life file of a level (Level 1's for anything but Level 2).</summary>
    public static string PathFor(string levelId) => levelId == OfficeLevelCatalog.Level2Id ? Level2ResourcePath : ResourcePath;

    /// <summary>Forget the loaded copy (tests and the next level load read the file again).</summary>
    public static void Clear() => cached = null;

    /// <summary>Parses the JSON text; null when it is not a usable file.</summary>
    public static PlazaLifeFile Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            PlazaLifeFile file = JsonUtility.FromJson<PlazaLifeFile>(json);
            if (file == null || file.npcs == null) return null;
            if (file.marieTravel == null) file.marieTravel = new MarieTravelDefinition();
            return file;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static PlazaLifeFile Load(string path)
    {
        TextAsset asset = Resources.Load<TextAsset>(path);
        PlazaLifeFile file = asset != null ? Parse(asset.text) : null;
        if (file == null)
        {
            Warn("Resources/" + path + ".json is missing or unreadable; ambient NPCs use a short built-in line.");
            return new PlazaLifeFile();
        }
        List<string> problems = Problems(file);
        if (problems.Count > 0) Warn("plaza_life.json needs a look: " + string.Join("; ", problems));
        return file;
    }

    /// <summary>The definition for an ambient NPC id, or null.</summary>
    public static AmbientNpcDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (AmbientNpcDefinition npc in Current.npcs)
            if (npc != null && npc.id == id) return npc;
        return null;
    }

    /// <summary>The usable talks of an NPC (each has at least one line with text).</summary>
    public static List<AmbientTalkDefinition> Talks(AmbientNpcDefinition npc)
    {
        var usable = new List<AmbientTalkDefinition>();
        if (npc == null || npc.talks == null) return usable;
        foreach (AmbientTalkDefinition talk in npc.talks)
            if (talk != null && HasText(talk.lines)) usable.Add(talk);
        return usable;
    }

    /// <summary>
    /// Dialogue lines for the talk at <paramref name="index"/> (wrapping), spoken
    /// by <paramref name="fallbackName"/> when the file gives no name.
    /// </summary>
    public static List<DialogueLine> BuildTalk(AmbientNpcDefinition npc, int index, string fallbackName)
    {
        string speaker = npc != null && !string.IsNullOrWhiteSpace(npc.name) ? npc.name : fallbackName;
        List<AmbientTalkDefinition> talks = Talks(npc);
        if (talks.Count == 0)
            return new DialogueBuilder(speaker).Npc("Good day!").Build();
        AmbientTalkDefinition talk = talks[((index % talks.Count) + talks.Count) % talks.Count];
        return Build(talk.lines, speaker);
    }

    /// <summary>Lines from definitions with the right portraits (empty or null text skipped).</summary>
    public static List<DialogueLine> Build(List<DialogueLineDefinition> lines, string speaker)
    {
        var clean = new List<DialogueLineDefinition>();
        if (lines != null)
            foreach (DialogueLineDefinition line in lines)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.text)) continue;
                bool player = line.speaker == "Player";
                clean.Add(new DialogueLineDefinition
                {
                    speaker = player ? "Player" : "Npc",
                    text = line.text.Trim(),
                    portraitId = string.IsNullOrWhiteSpace(line.portraitId) ? (player ? PlayerPortrait : "default") : line.portraitId,
                    emote = line.emote,
                    burst = line.burst
                });
            }
        return CaseFactory.BuildDialogue(clean, speaker);
    }

    /// <summary>Content checks (shared with the regression suite): ids, names, talks.</summary>
    public static List<string> Problems(PlazaLifeFile file)
    {
        var problems = new List<string>();
        if (file == null || file.npcs == null)
        {
            problems.Add("no npcs list");
            return problems;
        }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (AmbientNpcDefinition npc in file.npcs)
        {
            if (npc == null) { problems.Add("an empty npc entry"); continue; }
            if (string.IsNullOrWhiteSpace(npc.id)) problems.Add("an npc without id");
            else if (!ids.Add(npc.id)) problems.Add("duplicate id " + npc.id);
            if (string.IsNullOrWhiteSpace(npc.name)) problems.Add(npc.id + " has no name");
            else if (!names.Add(npc.name)) problems.Add("duplicate name " + npc.name);
            if (Talks(npc).Count == 0) problems.Add(npc.id + " has no talk with text");
        }
        return problems;
    }

    private static bool HasText(List<DialogueLineDefinition> lines)
    {
        if (lines == null) return false;
        foreach (DialogueLineDefinition line in lines)
            if (line != null && !string.IsNullOrWhiteSpace(line.text)) return true;
        return false;
    }

    private static void Warn(string message)
    {
        if (warned) return;
        warned = true;
        Debug.LogWarning("[PlazaLife] " + message);
    }
}
