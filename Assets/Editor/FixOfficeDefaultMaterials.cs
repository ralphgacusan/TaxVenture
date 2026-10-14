#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FixOfficeDefaultMaterials
{
    private const string MaterialPath = "Assets/White_URP.mat";
    private const string SessionKey = "TaxVenture.WhiteUrpFix.Applied";

    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        if (!SessionState.GetBool(SessionKey, false))
            EditorApplication.delayCall += Apply;
    }

    [MenuItem("Tools/TaxVenture/Fix Office White Materials")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/Office_Level1.unity");
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogError("White material fix: URP/Lit is unavailable or unsupported.");
            return;
        }

        Material white = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (white == null)
        {
            white = new Material(shader) { name = "White_URP" };
            white.SetColor("_BaseColor", Color.white);
            white.SetFloat("_Smoothness", 0.5f);
            AssetDatabase.CreateAsset(white, MaterialPath);
        }

        int changed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            bool replace = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null || material.name != "Default-Material" ||
                    !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)) &&
                    AssetDatabase.GetAssetPath(material) != "Resources/unity_builtin_extra")
                    continue;
                materials[i] = white;
                replace = true;
            }
            if (!replace)
                continue;
            Undo.RecordObject(renderer, "Fix white URP material");
            renderer.sharedMaterials = materials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            changed++;
        }

        if (changed > 0)
            EditorSceneManager.MarkSceneDirty(scene);
        SessionState.SetBool(SessionKey, true);
        string report = $"{DateTime.Now:O}: Replaced default materials on {changed} renderers. Save Office_Level1 to keep the changes.";
        File.WriteAllText("Library/WhiteUrpFixReport.txt", report);
        Debug.Log(report);
        SceneView.RepaintAll();
    }
}
#endif
