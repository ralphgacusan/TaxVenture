using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Milestone T02 pilot entry points in the Editor menu (user-run only):
/// - Open Review Scene: opens Assets/Pilot/T02/T02_PilotReview.unity; press
///   Play and the fixture assembles everything.
/// - Build Android Pilot APK: a separate APK holding ONLY the pilot scene, for
///   the current-phone measurement. It never edits EditorBuildSettings. So
///   that installing it cannot replace the game, the Android application id
///   and product name get a pilot suffix for the build and are restored
///   afterwards (also on failure).
/// </summary>
public static class T02PilotMenu
{
    public const string ScenePath = "Assets/Pilot/T02/T02_PilotReview.unity";
    public const string ApkPath = "Builds/T02Pilot/TaxVenture_T02Pilot.apk";
    public const string IdSuffix = ".t02pilot";

    [MenuItem("TaxVenture/T02 Pilot/Validate Imported Character Geometry (T02 + T03)")]
    public static void ValidateImportedCharacters()
    {
        string[] paths = {
            "Assets/3D Models/npc_tutorial_static/npc_tutorial_static1@Standing W_Briefcase Idle.fbx",
            "Assets/Pilot/T02/Derived/Arthur_T02_Shading.fbx",
            "Assets/Pilot/T02/Derived/Arthur_T02_Silhouette.fbx"
        };
        foreach (string path in paths) ValidateCharacter(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
        var assets = Resources.Load<ExplorationBodyAssets>(ExplorationBody.AssetsPath);
        if (assets == null || assets.idle == null || assets.walk == null)
            throw new InvalidOperationException("T03 Resources body/idle/walk binding is missing");
        ValidateCharacter(assets.model, "T03 Resources/" + ExplorationBody.AssetsPath);
    }

    private static void ValidateCharacter(GameObject prefab, string label)
    {
        if (prefab == null) throw new InvalidOperationException(label + ": model reference is missing");
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        instance.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            Animator animator = instance.GetComponentInChildren<Animator>(true);
            string problem = CharacterBodyGeometry.Problem(instance.transform, animator);
            if (!string.IsNullOrEmpty(problem)) throw new InvalidOperationException(label + ": " + problem);
            int vertices = 0;
            foreach (SkinnedMeshRenderer skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                vertices += skin.sharedMesh.vertexCount;
                long indices = 0;
                for (int sub = 0; sub < skin.sharedMesh.subMeshCount; sub++) indices += skin.sharedMesh.GetIndexCount(sub);
                if (indices == 0) throw new InvalidOperationException(label + ": no indices: " + skin.name);
                foreach (Transform bone in skin.bones)
                    if (bone == null || !bone.IsChildOf(instance.transform))
                        throw new InvalidOperationException(label + ": unresolved/outside bone: " + skin.name);
            }
            Debug.Log($"[Character validation] {label}: {vertices} vertices; renderers/materials/bones/Humanoid avatar valid (not rendered acceptance).");
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }

    [MenuItem("TaxVenture/T02 Pilot/Open Review Scene")]
    public static void OpenReviewScene()
    {
        if (!File.Exists(ScenePath)) { Debug.LogError("[T02 pilot] " + ScenePath + " is missing."); return; }
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("TaxVenture/T02 Pilot/Build Android Pilot APK (pilot scene only)")]
    public static void BuildAndroidPilot()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUtility.DisplayDialog("T02 pilot", "Switch the active platform to Android first (File > Build Profiles). Nothing was built or changed.", "OK");
            return;
        }
        string id = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
        string product = PlayerSettings.productName;
        if (!EditorUtility.DisplayDialog("T02 pilot APK",
                $"Builds {ApkPath} with only the pilot scene (release build, for frame pacing).\n\n" +
                $"For this build only: application id {id}{IdSuffix}, product \"{product} T02 Pilot\" (restored afterwards), so it installs beside the game.",
                "Build", "Cancel")) return;
        Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
        try
        {
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, id + IdSuffix);
            PlayerSettings.productName = product + " T02 Pilot";
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            Debug.Log($"[T02 pilot] APK build {report.summary.result}: {ApkPath}, {report.summary.totalSize / 1048576f:0.0} MB, {report.summary.totalTime.TotalSeconds:0} s.");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }
        finally
        {
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, id);
            PlayerSettings.productName = product;
        }
    }
}
