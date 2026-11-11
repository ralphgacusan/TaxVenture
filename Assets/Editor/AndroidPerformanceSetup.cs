using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Android-only import and player settings for low and mid-range phones. Only assets that actually
/// ship are touched (build scenes and Resources, with their dependencies), and only through Android
/// overrides, so source files stay intact and other platforms are unaffected. Safe to run again.
/// Runs once when Library/AndroidPerformanceSetup.request exists, or from
/// Tools > TaxVenture > Optimize For Android.
/// </summary>
public static class AndroidPerformanceSetup
{
    private const string RequestPath = "Library/AndroidPerformanceSetup.request";
    private const string Android = "Android";
    private const int MaxWorldTexture = 2048;
    private const int SmallestCompressedSide = 32;
    private const float LongClipSeconds = 6f;

    [InitializeOnLoadMethod]
    private static void RunIfRequested()
    {
        if (Application.isBatchMode || !File.Exists(RequestPath)) return;
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
        Optimize();
    }

    [MenuItem("Tools/TaxVenture/Optimize For Android")]
    private static void Optimize()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Stop Play mode before optimizing for Android.");
            return;
        }

        ApplyPlayerSettings();
        string[] shipped = ShippedAssets();
        int textures = 0, clips = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string path in shipped)
            {
                AssetImporter importer = AssetImporter.GetAtPath(path);
                if (importer is TextureImporter texture && OptimizeTexture(texture))
                {
                    texture.SaveAndReimport();
                    textures++;
                }
                else if (importer is AudioImporter audio && OptimizeAudio(audio, path))
                {
                    audio.SaveAndReimport();
                    clips++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[AndroidPerformanceSetup] Done: {textures} textures and {clips} audio clips updated out of {shipped.Length} shipped assets. Build a new APK to get the changes.");
    }

    private static void ApplyPlayerSettings()
    {
        PlayerSettings.Android.optimizedFramePacing = true;
        // Logging stays available for errors, without building a stack trace for every message.
        PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);

        // Keeps compiled shaders across scene loads, so a second visit does not stall recompiling them.
        var settings = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
        SerializedProperty keepShaders = settings.FindProperty("keepLoadedShadersAlive");
        if (keepShaders != null)
        {
            keepShaders.boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        else Debug.LogWarning("[AndroidPerformanceSetup] keepLoadedShadersAlive was not found; left as it was.");
    }

    private static string[] ShippedAssets()
    {
        var roots = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled && File.Exists(scene.path)) roots.Add(scene.path);
        foreach (string path in AssetDatabase.GetAllAssetPaths())
            if (path.StartsWith("Assets/", StringComparison.Ordinal) && path.Contains("/Resources/") &&
                !path.Contains("/Editor/") && !AssetDatabase.IsValidFolder(path))
                roots.Add(path);
        return AssetDatabase.GetDependencies(roots.ToArray(), true)
            .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Portraits and UI art were stored uncompressed (16 MB each) and scenery atlases at 4096.
    /// UI gets the sharpest ASTC, normal maps the next, colour atlases the standard 6x6.
    /// </summary>
    private static bool OptimizeTexture(TextureImporter importer)
    {
        if (importer.textureShape != TextureImporterShape.Texture2D || importer.isReadable) return false;
        TextureImporterType type = importer.textureType;
        bool ui = type == TextureImporterType.Sprite || type == TextureImporterType.GUI;
        bool normal = type == TextureImporterType.NormalMap;
        if (!ui && !normal && type != TextureImporterType.Default) return false;

        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (Mathf.Min(width, height) < SmallestCompressedSide) return false;

        TextureImporterPlatformSettings current = importer.GetPlatformTextureSettings(Android);
        int shownSize = current.overridden ? current.maxTextureSize : importer.maxTextureSize;
        bool uncompressed = current.overridden
            ? current.format == TextureImporterFormat.RGBA32 || current.format == TextureImporterFormat.RGB24
            : importer.textureCompression == TextureImporterCompression.Uncompressed;
        bool oversized = !ui && Mathf.Min(shownSize, Mathf.Max(width, height)) > MaxWorldTexture;
        if (!uncompressed && !oversized) return false;

        current.overridden = true;
        current.name = Android;
        current.maxTextureSize = ui ? shownSize : Mathf.Min(shownSize, MaxWorldTexture);
        current.format = ui ? TextureImporterFormat.ASTC_4x4 : normal ? TextureImporterFormat.ASTC_5x5 : TextureImporterFormat.ASTC_6x6;
        current.compressionQuality = 50;
        importer.SetPlatformTextureSettings(current);
        return true;
    }

    /// <summary>
    /// Long music and ambience were fully decoded on the main thread the first time they played,
    /// freezing the game. They now stay compressed in memory, and every clip loads with its scene.
    /// Compressed clips are ready at once, so AudioManager's playlist checks behave the same.
    /// </summary>
    private static bool OptimizeAudio(AudioImporter importer, string path)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) return false;

        AudioImporterSampleSettings current = importer.ContainsSampleSettingsOverride(Android)
            ? importer.GetOverrideSampleSettings(Android)
            : importer.defaultSampleSettings;
        AudioClipLoadType load = clip.length >= LongClipSeconds
            ? AudioClipLoadType.CompressedInMemory
            : AudioClipLoadType.DecompressOnLoad;
        if (current.loadType == load && current.preloadAudioData) return false;

        current.loadType = load;
        current.preloadAudioData = true;
        return importer.SetOverrideSampleSettings(Android, current);
    }
}
