using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Keeps the Level 3 build routing right (Documentation/Level3TheNumbersTellAStory/SCENE_REUSE.md):
/// Assets/Scenes/Office_Level3.unity (a one-time copy of the saved
/// Office_Level2, its own GUID) gets one enabled build entry, APPENDED after
/// every existing entry, so no existing index-based load shifts. Nothing loads
/// it by name until T18 enables Chapter 3 in Career (still "coming soon"); the
/// entry lets a direct Editor attempt replay and leave like Levels 1 and 2.
///
/// SAFE TO RE-RUN: it only acts while the scene has no build entry. It never
/// copies, creates, moves, saves or deletes a scene, and never touches
/// Office_Level1/Office_Level2. Tools > TaxVenture > Level 3 > Check Scene
/// Setup prints an inventory without changing anything.
/// </summary>
[InitializeOnLoad]
public static class Level3SceneSetup
{
    public const string SourceScene = "Assets/Scenes/Office_Level2.unity";
    public const string Level3Scene = "Assets/Scenes/Office_Level3.unity";
    public const string Level1Scene = "Assets/Scenes/Office_Level1.unity";

    static Level3SceneSetup()
    {
        EditorApplication.delayCall += EnsureBuildSettings;
    }

    private static void EnsureBuildSettings()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(Level3Scene, AssetPathToGUIDOptions.OnlyExistingAssets))) return;

        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene scene in scenes)
            if (scene.path == Level3Scene) return;

        scenes.Add(new EditorBuildSettingsScene(Level3Scene, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[Level3SceneSetup] Added {Level3Scene} to the end of the build list (existing entries keep their order).");
    }

    [MenuItem("Tools/TaxVenture/Level 3/Check Scene Setup")]
    private static void CheckSceneSetup()
    {
        string level1Guid = AssetDatabase.AssetPathToGUID(Level1Scene, AssetPathToGUIDOptions.OnlyExistingAssets);
        string sourceGuid = AssetDatabase.AssetPathToGUID(SourceScene, AssetPathToGUIDOptions.OnlyExistingAssets);
        string level3Guid = AssetDatabase.AssetPathToGUID(Level3Scene, AssetPathToGUIDOptions.OnlyExistingAssets);

        var report = new System.Text.StringBuilder("[Level3SceneSetup] Inventory (no changes made):\n");
        report.AppendLine($"- {Level1Scene}: {(string.IsNullOrEmpty(level1Guid) ? "MISSING" : level1Guid)}");
        report.AppendLine($"- {SourceScene}: {(string.IsNullOrEmpty(sourceGuid) ? "MISSING" : sourceGuid)}");
        report.AppendLine($"- {Level3Scene}: {(string.IsNullOrEmpty(level3Guid) ? "MISSING" : level3Guid)}");
        bool distinct = !string.IsNullOrEmpty(level3Guid) && level3Guid != sourceGuid && level3Guid != level1Guid;
        report.AppendLine($"- Office_Level3 has its own GUID: {(distinct ? "yes" : "NO")}");
        report.AppendLine($"- Office_Level3 bytes: {SizeOf(Level3Scene)}, Office_Level2 bytes: {SizeOf(SourceScene)}");

        int level3Entries = 0;
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            report.AppendLine($"- build: {(scene.enabled ? "on " : "off")} {scene.path}");
            if (scene.path == Level3Scene && scene.enabled) level3Entries++;
        }
        report.AppendLine($"- enabled Office_Level3 build entries: {level3Entries} (expected 1)");
        report.AppendLine("- Career Chapter 3 stays 'coming soon' until T18 (Resources/Career/chapters.json).");
        Debug.Log(report.ToString());
    }

    private static string SizeOf(string assetPath)
    {
        string full = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, assetPath);
        return File.Exists(full) ? new FileInfo(full).Length.ToString() : "missing";
    }
}
