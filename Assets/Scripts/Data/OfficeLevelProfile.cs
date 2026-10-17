using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// The scene gate for the self-bootstrapping office systems (HUD, minimap,
/// guide, fade). They used to build only in Office_Level1; now each asks
/// Allows(scene, feature). Office_Level1 keeps every feature; Office_Level2
/// (and later levels) opt in through "officeFeatures" in
/// Resources/Levels/&lt;levelId&gt;/level.json. Menu, cutscene and training
/// scenes are unmapped and build nothing. See OfficeLevelCatalog.
/// </summary>
public static class OfficeLevelProfile
{
    private static readonly Dictionary<string, HashSet<OfficeFeature>> cache =
        new Dictionary<string, HashSet<OfficeFeature>>(System.StringComparer.Ordinal);

    /// <summary>The level this scene plays (Level_01 for Office_Level1), or null.</summary>
    public static string LevelIdFor(Scene scene) =>
        scene.IsValid() ? OfficeLevelCatalog.LevelIdForScene(scene.name) : null;

    /// <summary>The level of the scene this object lives in, or Level_01 when unmapped.</summary>
    public static string LevelIdFor(Component owner)
    {
        string levelId = owner != null ? LevelIdFor(owner.gameObject.scene) : null;
        return string.IsNullOrEmpty(levelId) ? OfficeLevelCatalog.Level1Id : levelId;
    }

    /// <summary>True when this loaded scene should build the given office system.</summary>
    public static bool Allows(Scene scene, OfficeFeature feature)
    {
        if (!scene.IsValid()) return false;
        if (OfficeLevelCatalog.UsesAllFeatures(scene.name)) return true;
        string levelId = OfficeLevelCatalog.LevelIdForScene(scene.name);
        if (levelId == null) return false;

        if (!cache.TryGetValue(scene.name, out HashSet<OfficeFeature> features))
        {
            features = Load(scene.name, levelId);
            cache[scene.name] = features;
        }
        return features.Contains(feature);
    }

    /// <summary>Forgets cached level.json features (e.g. after editing it in Play Mode).</summary>
    public static void ClearCache() => cache.Clear();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => cache.Clear();

    private static HashSet<OfficeFeature> Load(string sceneName, string levelId)
    {
        TextAsset file = Resources.Load<TextAsset>($"Levels/{levelId}/level");
        List<string> names = null;
        if (file != null)
        {
            try
            {
                LevelDefinition level = JsonUtility.FromJson<LevelDefinition>(file.text);
                names = level != null ? level.officeFeatures : null;
            }
            catch (System.Exception error)
            {
                Debug.LogWarning($"[OfficeLevelProfile] Levels/{levelId}/level.json is not valid JSON ({error.Message}); {sceneName} builds no office systems.");
            }
        }

        HashSet<OfficeFeature> features = OfficeLevelCatalog.FeaturesForScene(sceneName, names, out string problems);
        if (problems != null) Debug.LogWarning($"[OfficeLevelProfile] Levels/{levelId}/level.json: {problems}");
        return features;
    }
}
