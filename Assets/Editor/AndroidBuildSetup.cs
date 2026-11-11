using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Readies the project for a release Android APK so Build is the only step left: a plain APK
/// (not an App Bundle or Gradle export), IL2CPP on ARM64, and Android as the active
/// platform. Goes through the Editor's API because the open Editor overwrites outside
/// edits to the settings files. Runs once when Library/AndroidBuildSetup.request exists, or from
/// Tools > TaxVenture > Prepare Android APK Build.
/// </summary>
public static class AndroidBuildSetup
{
    private const string RequestPath = "Library/AndroidBuildSetup.request";

    [InitializeOnLoadMethod]
    private static void RunIfRequested()
    {
        if (Application.isBatchMode || !File.Exists(RequestPath)) return;
        // Stopping Play Mode does not always reload scripts, so wait for it here as well.
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) RunRequest();
        };
        EditorApplication.delayCall += RunRequest;
    }

    private static void RunRequest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        Prepare();
    }

    [MenuItem("Tools/TaxVenture/Prepare Android APK Build")]
    private static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Stop Play mode before preparing the Android build.");
            return;
        }
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("The Android Build Support module is missing from this Unity install.");
            return;
        }

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorUserBuildSettings.development = false;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        AssetDatabase.SaveAssets();

        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            Debug.Log("TaxVenture is ready for an Android APK: press Build in Build Profiles.");
            return;
        }
        Debug.Log("TaxVenture APK settings applied. Switching the platform to Android (reimports assets, this can take a while).");
        EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android);
    }
}
