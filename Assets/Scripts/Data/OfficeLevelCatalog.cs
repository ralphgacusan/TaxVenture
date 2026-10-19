using System;
using System.Collections.Generic;

/// <summary>Shared office presentation a level scene may opt into (level.json officeFeatures).</summary>
public enum OfficeFeature
{
    /// <summary>OfficeHudRuntime: progression panel, objective tracker, Notes paper, prompts.</summary>
    Hud,
    /// <summary>OfficeMinimap.</summary>
    Minimap,
    /// <summary>GuideRuntime: lessons, objective arrows and minimap pin.</summary>
    Guide,
    /// <summary>SceneFade: fade in on load, fade out before leaving.</summary>
    Fade,
    /// <summary>CaseFinale: the client's ending in the conference room before Level Complete.</summary>
    Finale
}

/// <summary>
/// PURPOSE:
/// Which level each office scene plays and which shared office systems it
/// uses. Pure data/validation (no Unity calls), so it can be checked outside
/// the editor; OfficeLevelProfile reads level.json and asks this class.
///
/// RULES:
/// - Office_Level1 always gets every feature, whatever its level.json says,
///   so Level 1 keeps its exact behaviour.
/// - Any other mapped scene gets only the features its level.json lists in
///   "officeFeatures" (e.g. ["hud", "minimap", "guide", "fade"]).
/// - Unmapped scenes (menu, cutscenes, training) get nothing.
/// </summary>
public static class OfficeLevelCatalog
{
    public const string Level1Scene = "Office_Level1";
    public const string Level2Scene = "Office_Level2";
    public const string Level1Id = "Level_01";
    public const string Level2Id = "Level_02";

    private static readonly Dictionary<string, string> SceneLevels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Level1Scene] = Level1Id,
            [Level2Scene] = Level2Id
        };

    private static readonly OfficeFeature[] AllFeatures =
        (OfficeFeature[])Enum.GetValues(typeof(OfficeFeature));

    /// <summary>The level an office scene plays, or null for any other scene.</summary>
    public static string LevelIdForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        return SceneLevels.TryGetValue(sceneName, out string levelId) ? levelId : null;
    }

    /// <summary>True for the scene whose features are fixed (Level 1).</summary>
    public static bool UsesAllFeatures(string sceneName) => sceneName == Level1Scene;

    public static IReadOnlyList<OfficeFeature> Level1Features => AllFeatures;

    /// <summary>"hud" / "Minimap" / " guide " -> the feature (case and spaces ignored).</summary>
    public static bool TryParseFeature(string name, out OfficeFeature feature)
    {
        feature = default;
        if (string.IsNullOrWhiteSpace(name)) return false;
        string trimmed = name.Trim();
        foreach (OfficeFeature candidate in AllFeatures)
        {
            if (!string.Equals(candidate.ToString(), trimmed, StringComparison.OrdinalIgnoreCase)) continue;
            feature = candidate;
            return true;
        }
        return false;
    }

    /// <summary>Parses a level.json officeFeatures list. Unknown names are reported and skipped.</summary>
    public static HashSet<OfficeFeature> ParseFeatures(IEnumerable<string> names, out string problems)
    {
        var features = new HashSet<OfficeFeature>();
        var unknown = new List<string>();
        if (names != null)
            foreach (string name in names)
            {
                if (TryParseFeature(name, out OfficeFeature feature)) features.Add(feature);
                else unknown.Add(name ?? "(null)");
            }
        problems = unknown.Count == 0
            ? null
            : $"unknown officeFeatures ignored: {string.Join(", ", unknown)} (use: {string.Join(", ", AllFeatures)})";
        return features;
    }

    /// <summary>The features a scene uses, given its level.json officeFeatures list.</summary>
    public static HashSet<OfficeFeature> FeaturesForScene(string sceneName, IEnumerable<string> levelFeatures, out string problems)
    {
        problems = null;
        if (UsesAllFeatures(sceneName)) return new HashSet<OfficeFeature>(AllFeatures);
        if (LevelIdForScene(sceneName) == null) return new HashSet<OfficeFeature>();
        return ParseFeatures(levelFeatures, out problems);
    }
}
