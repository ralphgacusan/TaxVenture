using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Keeps the Level 2 build routing right (Documentation/Level2SomethingDoesNotMatch/SCENE_REUSE.md):
/// the new Assets/Scenes/Office_Level2.unity is in the build, and the old
/// Office_Level2_OLD stays on disk but out of the player build, so the
/// career board's "Office_Level2" never loads the old scene.
///
/// SAFE TO RE-RUN: it only acts while the new scene has no build entry
/// (adds it in the old scene's slot and switches the old entry off, once).
/// It never copies, creates, moves or deletes a scene: the one-time copy of
/// Office_Level1 already happened, and an existing Office_Level2 is never
/// overwritten. Tools > TaxVenture > Level 2 > Check Scene Setup prints an
/// inventory without changing anything.
/// </summary>
[InitializeOnLoad]
public static class Level2SceneSetup
{
    public const string SourceScene = "Assets/Scenes/Office_Level1.unity";
    public const string Level2Scene = "Assets/Scenes/Office_Level2.unity";
    public const string OldScene = "Assets/Scenes/Office_Level2_OLD.unity";

    static Level2SceneSetup()
    {
        EditorApplication.delayCall += EnsureBuildSettings;
    }

    private static void EnsureBuildSettings()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(Level2Scene, AssetPathToGUIDOptions.OnlyExistingAssets))) return;

        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int oldIndex = -1;
        int level1Index = -1;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == Level2Scene) return;
            if (scenes[i].path == OldScene) oldIndex = i;
            if (scenes[i].path == SourceScene) level1Index = i;
        }

        int insertAt = oldIndex >= 0 ? oldIndex : (level1Index >= 0 ? level1Index + 1 : scenes.Count);
        scenes.Insert(insertAt, new EditorBuildSettingsScene(Level2Scene, true));
        if (oldIndex >= 0) scenes[oldIndex + 1].enabled = false;
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[Level2SceneSetup] Added {Level2Scene} to the build" +
                  (oldIndex >= 0 ? $" and switched {OldScene} off (kept on disk)." : "."));
    }

    [MenuItem("Tools/TaxVenture/Level 2/Check Scene Setup")]
    private static void CheckSceneSetup()
    {
        string level1Guid = AssetDatabase.AssetPathToGUID(SourceScene, AssetPathToGUIDOptions.OnlyExistingAssets);
        string level2Guid = AssetDatabase.AssetPathToGUID(Level2Scene, AssetPathToGUIDOptions.OnlyExistingAssets);
        string oldGuid = AssetDatabase.AssetPathToGUID(OldScene, AssetPathToGUIDOptions.OnlyExistingAssets);

        var report = new System.Text.StringBuilder("[Level2SceneSetup] Inventory (no changes made):\n");
        report.AppendLine($"- {SourceScene}: {(string.IsNullOrEmpty(level1Guid) ? "MISSING" : level1Guid)}");
        report.AppendLine($"- {Level2Scene}: {(string.IsNullOrEmpty(level2Guid) ? "MISSING" : level2Guid)}");
        report.AppendLine($"- {OldScene}: {(string.IsNullOrEmpty(oldGuid) ? "MISSING" : oldGuid)}");
        bool distinct = !string.IsNullOrEmpty(level2Guid) && level2Guid != level1Guid && level2Guid != oldGuid;
        report.AppendLine($"- Office_Level2 has its own GUID: {(distinct ? "yes" : "NO")}");
        report.AppendLine($"- Office_Level2 bytes: {SizeOf(Level2Scene)}, Office_Level1 bytes: {SizeOf(SourceScene)}");

        int level2Entries = 0;
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            report.AppendLine($"- build: {(scene.enabled ? "on " : "off")} {scene.path}");
            if (scene.path == Level2Scene && scene.enabled) level2Entries++;
        }
        report.AppendLine($"- enabled Office_Level2 build entries: {level2Entries} (expected 1)");
        Debug.Log(report.ToString());
    }

    private static string SizeOf(string assetPath)
    {
        string full = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, assetPath);
        return File.Exists(full) ? new FileInfo(full).Length.ToString() : "missing";
    }
}
