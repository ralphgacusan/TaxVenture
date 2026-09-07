#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SyntyShaderUsageAnalyzer : EditorWindow
{
    private Vector2 scrollPosition;
    private string report = "";

    [MenuItem("Tools/TaxVenture/Synty Shader Usage Analyzer")]
    public static void ShowWindow()
    {
        GetWindow<SyntyShaderUsageAnalyzer>(
            "Synty Shader Analyzer"
        );
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField(
            "TaxVenture - Synty Shader Usage Analyzer",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Analyzes Synty materials and determines which shaders they use. " +
            "It also checks whether the materials are referenced by the scenes in Build Settings.",
            MessageType.Info
        );

        GUILayout.Space(10);

        if (GUILayout.Button("ANALYZE SYNTY SHADERS", GUILayout.Height(35)))
        {
            Analyze();
        }

        GUILayout.Space(10);

        if (!string.IsNullOrEmpty(report))
        {
            EditorGUILayout.LabelField(
                "Analysis Result",
                EditorStyles.boldLabel
            );

            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition
            );

            EditorGUILayout.TextArea(
                report,
                GUILayout.ExpandHeight(true)
            );

            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);

            if (GUILayout.Button("Save Report"))
            {
                SaveReport();
            }
        }
    }

    private void Analyze()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("TAXVENTURE SYNTY SHADER USAGE REPORT");
        sb.AppendLine("====================================");
        sb.AppendLine();
        sb.AppendLine("Generated: " + System.DateTime.Now);
        sb.AppendLine();

        // ---------------------------------------------------------
        // BUILD SCENES
        // ---------------------------------------------------------

        List<string> scenePaths = new List<string>();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenePaths.Add(scene.path);
            }
        }

        sb.AppendLine("SCANNED BUILD SCENES");
        sb.AppendLine("--------------------");

        foreach (string scene in scenePaths)
        {
            sb.AppendLine(scene);
        }

        sb.AppendLine();

        // ---------------------------------------------------------
        // FIND SYNTY MATERIALS
        // ---------------------------------------------------------

        string[] materialGuids = AssetDatabase.FindAssets(
            "t:Material",
            new[] { "Assets/Synty" }
        );

        List<Material> syntyMaterials = new List<Material>();

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null)
            {
                syntyMaterials.Add(material);
            }
        }

        sb.AppendLine("SYNTY MATERIALS");
        sb.AppendLine("----------------");
        sb.AppendLine(
            "Total Synty materials found: " +
            syntyMaterials.Count
        );
        sb.AppendLine();

        // ---------------------------------------------------------
        // GROUP MATERIALS BY SHADER
        // ---------------------------------------------------------

        Dictionary<string, List<Material>> shaderMaterials =
            new Dictionary<string, List<Material>>();

        foreach (Material material in syntyMaterials)
        {
            if (material.shader == null)
                continue;

            string shaderName = material.shader.name;

            if (!shaderMaterials.ContainsKey(shaderName))
            {
                shaderMaterials[shaderName] =
                    new List<Material>();
            }

            shaderMaterials[shaderName].Add(material);
        }

        sb.AppendLine("SHADER SUMMARY");
        sb.AppendLine("--------------");

        foreach (var pair in shaderMaterials.OrderByDescending(x => x.Value.Count))
        {
            sb.AppendLine(
                pair.Key +
                " : " +
                pair.Value.Count +
                " materials"
            );
        }

        sb.AppendLine();

        // ---------------------------------------------------------
        // SPECIFIC SYNTY SHADERS
        // ---------------------------------------------------------

        AnalyzeShader(
            "Synty/Generic_Basic",
            shaderMaterials,
            scenePaths,
            sb
        );

        AnalyzeShader(
            "Synty/Generic_Standard",
            shaderMaterials,
            scenePaths,
            sb
        );

        // ---------------------------------------------------------
        // OTHER SHADERS
        // ---------------------------------------------------------

        sb.AppendLine("OTHER SYNTY SHADERS");
        sb.AppendLine("-------------------");

        foreach (var pair in shaderMaterials
                     .Where(x =>
                         x.Key != "Synty/Generic_Basic" &&
                         x.Key != "Synty/Generic_Standard")
                     .OrderByDescending(x => x.Value.Count))
        {
            sb.AppendLine();
            sb.AppendLine(
                pair.Key +
                " (" +
                pair.Value.Count +
                " materials)"
            );
        }

        sb.AppendLine();

        // ---------------------------------------------------------
        // WARNINGS
        // ---------------------------------------------------------

        sb.AppendLine("IMPORTANT");
        sb.AppendLine("---------");
        sb.AppendLine();

        sb.AppendLine(
            "This tool does NOT delete anything."
        );

        sb.AppendLine(
            "It only analyzes material/shader usage."
        );

        sb.AppendLine();

        sb.AppendLine(
            "A material being unused by the scanned scenes does NOT " +
            "necessarily mean it is safe to delete."
        );

        sb.AppendLine(
            "Runtime-loaded assets, Resources, Addressables, scripts, " +
            "or dynamically instantiated prefabs may not appear in " +
            "the scene dependency analysis."
        );

        report = sb.ToString();

        Debug.Log(report);
    }

    private void AnalyzeShader(
        string shaderName,
        Dictionary<string, List<Material>> shaderMaterials,
        List<string> scenePaths,
        StringBuilder sb
    )
    {
        sb.AppendLine();
        sb.AppendLine(
            "================================================"
        );

        sb.AppendLine(
            shaderName
        );

        sb.AppendLine(
            "================================================"
        );

        if (!shaderMaterials.ContainsKey(shaderName))
        {
            sb.AppendLine(
                "No materials using this shader were found."
            );

            return;
        }

        List<Material> materials =
            shaderMaterials[shaderName];

        sb.AppendLine(
            "Total materials: " +
            materials.Count
        );

        sb.AppendLine();

        sb.AppendLine("MATERIALS");
        sb.AppendLine("---------");

        foreach (Material material in materials.OrderBy(x => x.name))
        {
            string path =
                AssetDatabase.GetAssetPath(material);

            sb.AppendLine(
                "• " +
                material.name +
                " | " +
                path
            );
        }

        sb.AppendLine();

        // ---------------------------------------------------------
        // FIND SCENE REFERENCES
        // ---------------------------------------------------------

        HashSet<string> usedMaterialPaths =
            new HashSet<string>();

        foreach (string scenePath in scenePaths)
        {
            string[] dependencies =
                AssetDatabase.GetDependencies(
                    scenePath,
                    true
                );

            foreach (string dependency in dependencies)
            {
                if (dependency.EndsWith(".mat"))
                {
                    usedMaterialPaths.Add(
                        dependency
                    );
                }
            }
        }

        List<Material> usedMaterials =
            materials
                .Where(m =>
                    usedMaterialPaths.Contains(
                        AssetDatabase.GetAssetPath(m)
                    ))
                .ToList();

        List<Material> unusedMaterials =
            materials
                .Where(m =>
                    !usedMaterialPaths.Contains(
                        AssetDatabase.GetAssetPath(m)
                    ))
                .ToList();

        sb.AppendLine("SCENE USAGE");
        sb.AppendLine("-----------");

        sb.AppendLine(
            "Materials referenced by build scenes: " +
            usedMaterials.Count
        );

        sb.AppendLine(
            "Materials NOT referenced by build scenes: " +
            unusedMaterials.Count
        );

        sb.AppendLine();

        if (unusedMaterials.Count > 0)
        {
            sb.AppendLine(
                "POTENTIALLY UNUSED MATERIALS"
            );

            sb.AppendLine(
                "----------------------------"
            );

            foreach (Material material in unusedMaterials)
            {
                sb.AppendLine(
                    "• " +
                    material.name +
                    " | " +
                    AssetDatabase.GetAssetPath(material)
                );
            }
        }

        sb.AppendLine();

        sb.AppendLine(
            "NOTE: Scene usage only means the material is reachable " +
            "through the currently enabled build scenes. It does " +
            "not prove that the material is safe to delete."
        );
    }

    private void SaveReport()
    {
        string path = EditorUtility.SaveFilePanel(
            "Save Synty Shader Report",
            "Assets",
            "TaxVenture_Synty_Shader_Report",
            "txt"
        );

        if (string.IsNullOrEmpty(path))
            return;

        File.WriteAllText(
            path,
            report,
            Encoding.UTF8
        );

        AssetDatabase.Refresh();

        Debug.Log(
            "Synty shader report saved to: " +
            path
        );
    }
}

#endif