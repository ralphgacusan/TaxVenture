using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TaxVentureAssetOptimizer : EditorWindow
{
    private const string SYNTY_ROOT = "Assets/Synty";

    private Vector2 scroll;

    private List<AssetInfo> usedAssets = new List<AssetInfo>();
    private List<AssetInfo> dependencyOnlyAssets = new List<AssetInfo>();
    private List<AssetInfo> unusedAssets = new List<AssetInfo>();
    private List<AssetInfo> largeTextures = new List<AssetInfo>();
    private List<AssetInfo> largeModels = new List<AssetInfo>();

    private bool scanAllScenes = false;

    private int textureSizeWarning = 2048;
    private long largeFileWarningMB = 5;

    private bool showUsed = false;
    private bool showDependencyOnly = false;
    private bool showUnused = true;
    private bool showLargeTextures = true;
    private bool showLargeModels = true;

    private enum AssetCategory
    {
        Unknown,
        Texture,
        Model,
        Material,
        Prefab,
        Animation,
        Audio,
        Shader
    }

    [MenuItem("Tools/TaxVenture/Asset Optimizer")]
    public static void OpenWindow()
    {
        TaxVentureAssetOptimizer window =
            GetWindow<TaxVentureAssetOptimizer>(
                "TaxVenture Asset Optimizer"
            );

        window.minSize = new Vector2(850, 600);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField(
            "TaxVenture Asset Optimizer",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Analyzes Synty assets used by your game scenes, " +
            "finds assets that are not reachable from those scenes, " +
            "and identifies oversized textures/models.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField(
            "SCAN SETTINGS",
            EditorStyles.boldLabel
        );

        scanAllScenes = EditorGUILayout.Toggle(
            "Scan ALL project scenes",
            scanAllScenes
        );

        if (!scanAllScenes)
        {
            EditorGUILayout.HelpBox(
                "Recommended for your current TaxVenture build. " +
                "Only enabled Build Settings scenes are scanned.",
                MessageType.None
            );
        }

        EditorGUILayout.Space(5);

        textureSizeWarning = EditorGUILayout.IntField(
            "Large texture threshold",
            textureSizeWarning
        );

        largeFileWarningMB = EditorGUILayout.LongField(
            "Large file threshold (MB)",
            largeFileWarningMB
        );

        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.65f, 0.9f, 0.65f);

        if (GUILayout.Button(
            "ANALYZE TAXVENTURE",
            GUILayout.Height(45)
        ))
        {
            Analyze();
        }

        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        if (usedAssets.Count > 0 ||
            dependencyOnlyAssets.Count > 0 ||
            unusedAssets.Count > 0)
        {
            DrawResults();
        }
        else
        {
            EditorGUILayout.LabelField(
                "Run ANALYZE TAXVENTURE to begin.",
                EditorStyles.centeredGreyMiniLabel
            );
        }
    }

    private void Analyze()
    {
        ClearResults();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string[] scenes = scanAllScenes
            ? FindAllScenes()
            : GetBuildScenes();

        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No Scenes",
                "No scenes were found to scan.",
                "OK"
            );

            return;
        }

        HashSet<string> directSceneAssets =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        HashSet<string> allUsedAssets =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        // ------------------------------------------------------------
        // STEP 1
        // Find direct scene dependencies.
        // ------------------------------------------------------------

        try
        {
            for (int i = 0; i < scenes.Length; i++)
            {
                string scene = scenes[i];

                EditorUtility.DisplayProgressBar(
                    "TaxVenture Asset Optimizer",
                    "Reading scene: " + scene,
                    (float)i / scenes.Length
                );

                string[] direct =
                    AssetDatabase.GetDependencies(
                        scene,
                        false
                    );

                foreach (string path in direct)
                {
                    if (IsValidAsset(path))
                        directSceneAssets.Add(path);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // ------------------------------------------------------------
        // STEP 2
        // Recursively follow dependencies of actual scene roots.
        // ------------------------------------------------------------

        Queue<string> queue =
            new Queue<string>(directSceneAssets);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();

            if (!allUsedAssets.Add(current))
                continue;

            string[] dependencies =
                AssetDatabase.GetDependencies(
                    current,
                    false
                );

            foreach (string dependency in dependencies)
            {
                if (!IsValidAsset(dependency))
                    continue;

                if (!allUsedAssets.Contains(dependency))
                    queue.Enqueue(dependency);
            }
        }

        // ------------------------------------------------------------
        // STEP 3
        // Analyze every Synty asset.
        // ------------------------------------------------------------

        string[] syntyAssets = GetSyntyAssets();

        try
        {
            for (int i = 0; i < syntyAssets.Length; i++)
            {
                string path = syntyAssets[i];

                EditorUtility.DisplayProgressBar(
                    "Analyzing Synty Assets",
                    path,
                    (float)i / syntyAssets.Length
                );

                if (!IsAnalyzable(path))
                    continue;

                AssetInfo info = CreateInfo(path);

                if (directSceneAssets.Contains(path))
                {
                    usedAssets.Add(info);
                }
                else if (allUsedAssets.Contains(path))
                {
                    dependencyOnlyAssets.Add(info);
                }
                else
                {
                    unusedAssets.Add(info);
                }

                AnalyzeLargeAsset(info);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        SortResults();

        string report = GenerateReport(scenes);

        Debug.Log(
            "[TaxVenture Asset Optimizer]\n" +
            $"Directly used: {usedAssets.Count}\n" +
            $"Dependency only: {dependencyOnlyAssets.Count}\n" +
            $"Not reachable: {unusedAssets.Count}\n" +
            $"Large textures: {largeTextures.Count}\n" +
            $"Large models: {largeModels.Count}\n" +
            $"Report: {report}"
        );

        EditorUtility.DisplayDialog(
            "Analysis Complete",
            $"Analysis complete.\n\n" +
            $"Directly used: {usedAssets.Count}\n" +
            $"Dependency only: {dependencyOnlyAssets.Count}\n" +
            $"Not reachable: {unusedAssets.Count}\n" +
            $"Large textures: {largeTextures.Count}\n" +
            $"Large models: {largeModels.Count}\n\n" +
            $"Report saved to:\n{report}",
            "OK"
        );

        Repaint();
    }

    private void DrawResults()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawSummary();

        EditorGUILayout.Space(10);

        showUnused = EditorGUILayout.Foldout(
            showUnused,
            $"🔴 NOT REACHED FROM BUILD SCENES ({unusedAssets.Count})",
            true
        );

        if (showUnused)
        {
            DrawAssetList(unusedAssets, true);
        }

        EditorGUILayout.Space(10);

        showLargeTextures = EditorGUILayout.Foldout(
            showLargeTextures,
            $"🟠 LARGE TEXTURES ({largeTextures.Count})",
            true
        );

        if (showLargeTextures)
        {
            DrawAssetList(largeTextures, false);
        }

        EditorGUILayout.Space(10);

        showLargeModels = EditorGUILayout.Foldout(
            showLargeModels,
            $"🟠 LARGE MODELS ({largeModels.Count})",
            true
        );

        if (showLargeModels)
        {
            DrawAssetList(largeModels, false);
        }

        EditorGUILayout.Space(10);

        showDependencyOnly = EditorGUILayout.Foldout(
            showDependencyOnly,
            $"🟡 DEPENDENCY ONLY ({dependencyOnlyAssets.Count})",
            true
        );

        if (showDependencyOnly)
        {
            DrawAssetList(dependencyOnlyAssets, false);
        }

        EditorGUILayout.Space(10);

        showUsed = EditorGUILayout.Foldout(
            showUsed,
            $"🟢 DIRECTLY USED ({usedAssets.Count})",
            true
        );

        if (showUsed)
        {
            DrawAssetList(usedAssets, false);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSummary()
    {
        long unusedSize =
            unusedAssets.Sum(x => x.fileSize);

        long largeTextureSize =
            largeTextures.Sum(x => x.fileSize);

        long largeModelSize =
            largeModels.Sum(x => x.fileSize);

        EditorGUILayout.LabelField(
            "RESULT SUMMARY",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            $"🟢 Directly used: {usedAssets.Count}"
        );

        EditorGUILayout.LabelField(
            $"🟡 Dependency only: {dependencyOnlyAssets.Count}"
        );

        EditorGUILayout.LabelField(
            $"🔴 Not reachable: {unusedAssets.Count} " +
            $"({FormatBytes(unusedSize)})"
        );

        EditorGUILayout.LabelField(
            $"🟠 Large textures: {largeTextures.Count} " +
            $"({FormatBytes(largeTextureSize)})"
        );

        EditorGUILayout.LabelField(
            $"🟠 Large models: {largeModels.Count} " +
            $"({FormatBytes(largeModelSize)})"
        );

        EditorGUILayout.Space(8);

        if (unusedAssets.Count > 0)
        {
            GUI.backgroundColor =
                new Color(1f, 0.75f, 0.55f);

            if (GUILayout.Button(
                "SELECT ALL NOT-REACHED ASSETS",
                GUILayout.Height(30)
            ))
            {
                SelectAssets(unusedAssets);
            }

            if (GUILayout.Button(
                "MOVE NOT-REACHED ASSETS TO TRASH",
                GUILayout.Height(35)
            ))
            {
                DeleteUnusedAssets();
            }

            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.HelpBox(
            "IMPORTANT: Not-reached does not automatically mean " +
            "safe to delete. Check assets loaded dynamically by " +
            "Resources, Addressables, scripts, or custom systems.",
            MessageType.Warning
        );
    }

    private void DrawAssetList(
        List<AssetInfo> assets,
        bool allowDelete
    )
    {
        if (assets.Count == 0)
        {
            EditorGUILayout.LabelField(
                "None found.",
                EditorStyles.centeredGreyMiniLabel
            );

            return;
        }

        foreach (AssetInfo asset in assets)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                asset.path,
                GUILayout.ExpandWidth(true)
            );

            EditorGUILayout.LabelField(
                FormatBytes(asset.fileSize),
                GUILayout.Width(85)
            );

            if (asset.category == AssetCategory.Texture &&
                asset.textureWidth > 0)
            {
                EditorGUILayout.LabelField(
                    $"{asset.textureWidth}x{asset.textureHeight}",
                    GUILayout.Width(100)
                );
            }

            if (GUILayout.Button(
                "Ping",
                GUILayout.Width(50)
            ))
            {
                PingAsset(asset.path);
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void AnalyzeLargeAsset(AssetInfo info)
    {
        if (info.category == AssetCategory.Texture)
        {
            Texture texture =
                AssetDatabase.LoadAssetAtPath<Texture>(
                    info.path
                );

            if (texture != null)
            {
                string path =
                    AssetDatabase.GetAssetPath(texture);

                TextureImporter importer =
                    AssetImporter.GetAtPath(path)
                    as TextureImporter;

                if (importer != null)
                {
                    info.textureWidth =
                        texture.width;

                    info.textureHeight =
                        texture.height;
                }

                if (texture.width >= textureSizeWarning ||
                    texture.height >= textureSizeWarning ||
                    info.fileSize >=
                    largeFileWarningMB *
                    1024L *
                    1024L)
                {
                    largeTextures.Add(info);
                }
            }
        }

        if (info.category == AssetCategory.Model)
        {
            if (info.fileSize >=
                largeFileWarningMB *
                1024L *
                1024L)
            {
                largeModels.Add(info);
            }
        }
    }

    private AssetInfo CreateInfo(string path)
    {
        AssetInfo info = new AssetInfo();

        info.path = path;
        info.fileSize = GetFileSize(path);
        info.category = GetCategory(path);
        info.type =
            AssetDatabase.GetMainAssetTypeAtPath(path);

        return info;
    }

    private AssetCategory GetCategory(string path)
    {
        string ext =
            Path.GetExtension(path)
            .ToLowerInvariant();

        switch (ext)
        {
            case ".png":
            case ".jpg":
            case ".jpeg":
            case ".tga":
            case ".psd":
            case ".tif":
            case ".tiff":
            case ".exr":
            case ".bmp":
            case ".webp":
                return AssetCategory.Texture;

            case ".fbx":
            case ".obj":
            case ".blend":
            case ".dae":
            case ".3ds":
            case ".max":
                return AssetCategory.Model;

            case ".mat":
                return AssetCategory.Material;

            case ".prefab":
                return AssetCategory.Prefab;

            case ".anim":
                return AssetCategory.Animation;

            case ".wav":
            case ".mp3":
            case ".ogg":
            case ".aif":
            case ".aiff":
                return AssetCategory.Audio;

            case ".shader":
            case ".shadergraph":
                return AssetCategory.Shader;

            default:
                return AssetCategory.Unknown;
        }
    }

    private bool IsAnalyzable(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        if (!path.StartsWith(
            SYNTY_ROOT + "/",
            StringComparison.OrdinalIgnoreCase
        ))
            return false;

        if (path.EndsWith(".meta"))
            return false;

        if (path.EndsWith(".cs"))
            return false;

        if (path.EndsWith(".dll"))
            return false;

        if (path.Contains("/Resources/"))
            return false;

        if (path.Contains("/StreamingAssets/"))
            return false;

        return true;
    }

    private bool IsValidAsset(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        if (!path.StartsWith(
            "Assets/",
            StringComparison.OrdinalIgnoreCase
        ))
            return false;

        if (path.EndsWith(".unity"))
            return true;

        if (path.EndsWith(".cs"))
            return false;

        return AssetDatabase.LoadMainAssetAtPath(path) != null;
    }

    private string[] GetSyntyAssets()
    {
        return AssetDatabase.GetAllAssetPaths()
            .Where(path =>
                path.StartsWith(
                    SYNTY_ROOT + "/",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .Where(path =>
                !AssetDatabase.IsValidFolder(path)
            )
            .ToArray();
    }

    private string[] GetBuildScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .Where(File.Exists)
            .ToArray();
    }

    private string[] FindAllScenes()
    {
        return AssetDatabase.FindAssets(
                "t:Scene",
                new[] { "Assets" }
            )
            .Select(
                AssetDatabase.GUIDToAssetPath
            )
            .Where(
                path => path.EndsWith(
                    ".unity",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .ToArray();
    }

    private void SelectAssets(
        List<AssetInfo> assets
    )
    {
        List<UnityEngine.Object> objects =
            new List<UnityEngine.Object>();

        foreach (AssetInfo asset in assets)
        {
            UnityEngine.Object obj =
                AssetDatabase.LoadMainAssetAtPath(
                    asset.path
                );

            if (obj != null)
                objects.Add(obj);
        }

        Selection.objects = objects.ToArray();

        Debug.Log(
            $"Selected {objects.Count} assets."
        );
    }

    private void PingAsset(string path)
    {
        UnityEngine.Object obj =
            AssetDatabase.LoadMainAssetAtPath(path);

        if (obj == null)
            return;

        Selection.activeObject = obj;

        EditorGUIUtility.PingObject(obj);
    }

    private void DeleteUnusedAssets()
    {
        if (unusedAssets.Count == 0)
            return;

        long total =
            unusedAssets.Sum(x => x.fileSize);

        bool confirm =
            EditorUtility.DisplayDialog(
                "Move Assets to Trash?",
                $"This will move:\n\n" +
                $"{unusedAssets.Count} assets\n" +
                $"{FormatBytes(total)}\n\n" +
                "to the OS/Unity Trash.\n\n" +
                "Make sure you have a backup.",
                "Move to Trash",
                "Cancel"
            );

        if (!confirm)
            return;

        int moved = 0;

        foreach (AssetInfo asset in
                 unusedAssets.ToList())
        {
            if (AssetDatabase.MoveAssetToTrash(
                asset.path
            ))
            {
                moved++;
            }
        }

        AssetDatabase.Refresh();

        unusedAssets.Clear();

        EditorUtility.DisplayDialog(
            "Finished",
            $"Moved {moved} assets to Trash.",
            "OK"
        );

        Repaint();
    }

    private void SortResults()
    {
        usedAssets =
            usedAssets
                .OrderBy(x => x.path)
                .ToList();

        dependencyOnlyAssets =
            dependencyOnlyAssets
                .OrderByDescending(x => x.fileSize)
                .ToList();

        unusedAssets =
            unusedAssets
                .OrderByDescending(x => x.fileSize)
                .ToList();

        largeTextures =
            largeTextures
                .OrderByDescending(x => x.fileSize)
                .ToList();

        largeModels =
            largeModels
                .OrderByDescending(x => x.fileSize)
                .ToList();
    }

    private string GenerateReport(
        string[] scenes
    )
    {
        string path =
            "Assets/TaxVenture_Asset_Optimization_Report.txt";

        List<string> lines =
            new List<string>();

        lines.Add(
            "TAXVENTURE ASSET OPTIMIZATION REPORT"
        );

        lines.Add(
            "===================================="
        );

        lines.Add(
            $"Generated: {DateTime.Now}"
        );

        lines.Add("");

        lines.Add(
            "SCANNED SCENES"
        );

        lines.Add(
            "--------------"
        );

        foreach (string scene in scenes)
            lines.Add(scene);

        lines.Add("");

        lines.Add(
            $"Directly used Synty assets: {usedAssets.Count}"
        );

        lines.Add(
            $"Dependency-only Synty assets: {dependencyOnlyAssets.Count}"
        );

        lines.Add(
            $"Not-reached Synty assets: {unusedAssets.Count}"
        );

        lines.Add("");

        lines.Add(
            "NOT-REACHED ASSETS"
        );

        lines.Add(
            "-----------------"
        );

        foreach (AssetInfo asset in unusedAssets)
        {
            lines.Add(
                $"{FormatBytes(asset.fileSize),10} | " +
                $"{asset.category,-10} | " +
                asset.path
            );
        }

        lines.Add("");

        lines.Add(
            "LARGE TEXTURES"
        );

        lines.Add(
            "--------------"
        );

        foreach (AssetInfo asset in largeTextures)
        {
            lines.Add(
                $"{FormatBytes(asset.fileSize),10} | " +
                $"{asset.textureWidth}x{asset.textureHeight} | " +
                asset.path
            );
        }

        lines.Add("");

        lines.Add(
            "LARGE MODELS"
        );

        lines.Add(
            "------------"
        );

        foreach (AssetInfo asset in largeModels)
        {
            lines.Add(
                $"{FormatBytes(asset.fileSize),10} | " +
                asset.path
            );
        }

        File.WriteAllLines(
            Path.GetFullPath(path),
            lines
        );

        AssetDatabase.Refresh();

        return path;
    }

    private long GetFileSize(string path)
    {
        try
        {
            string fullPath =
                Path.GetFullPath(path);

            if (File.Exists(fullPath))
            {
                return new FileInfo(
                    fullPath
                ).Length;
            }
        }
        catch
        {
            // Ignore inaccessible files.
        }

        return 0;
    }

    private string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        if (bytes < 1024 * 1024)
            return $"{bytes / 1024f:0.0} KB";

        if (bytes < 1024L * 1024L * 1024L)
            return $"{bytes / (1024f * 1024f):0.0} MB";

        return $"{bytes / (1024f * 1024f * 1024f):0.00} GB";
    }

    private void ClearResults()
    {
        usedAssets.Clear();
        dependencyOnlyAssets.Clear();
        unusedAssets.Clear();
        largeTextures.Clear();
        largeModels.Clear();
    }

    [Serializable]
    private class AssetInfo
    {
        public string path;
        public long fileSize;
        public Type type;
        public AssetCategory category;

        public int textureWidth;
        public int textureHeight;
    }
}