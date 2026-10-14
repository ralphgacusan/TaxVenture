using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// T04 setup with Undo: Tools > TaxVenture > Configure NPC Markers.
/// Adds an NpcStatusMarker to every NPC whose interactable reports
/// availability (INpcAvailabilitySource) in the open scene, and assigns one
/// shared unlit material asset (created once so it is included in builds).
/// Changes no NPC transform, collider, quest rule or existing indicator.
/// </summary>
public static class NpcMarkerSetup
{
    private const string MaterialFolder = "Assets/Materials";
    private const string MaterialPath = "Assets/Materials/NpcStatusMarker.mat";

    [MenuItem("Tools/TaxVenture/Configure NPC Markers")]
    private static void Configure()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Stop Play mode before configuring NPC markers.");
            return;
        }

        Material material = EnsureMaterial(out string materialNote);
        if (material == null) return;

        Scene scene = SceneManager.GetActiveScene();
        var report = new StringBuilder();
        var sources = new List<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (behaviour is INpcAvailabilitySource && behaviour.gameObject.scene == scene)
                sources.Add(behaviour);

        if (sources.Count == 0)
        {
            Debug.LogError("No NPC interactables found in the active scene. Open Office_Level1 first. No changes applied.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure TaxVenture NPC markers");

        int added = 0;
        foreach (MonoBehaviour source in sources)
        {
            NpcStatusMarker marker = source.GetComponent<NpcStatusMarker>();
            bool isNew = marker == null;
            if (isNew)
            {
                marker = Undo.AddComponent<NpcStatusMarker>(source.gameObject);
                added++;
            }
            var data = new SerializedObject(marker);
            data.FindProperty("availabilitySource").objectReferenceValue = source;
            data.FindProperty("markerMaterial").objectReferenceValue = material;
            data.ApplyModifiedProperties();

            bool legacyTint = source.GetComponent<NpcVisualIndicator>() != null;
            report.AppendLine($"  {source.GetType().Name} '{source.name}': {(isNew ? "marker added" : "marker updated")}{(legacyTint ? " (also has the old NpcVisualIndicator body tint, left unchanged)" : "")}");
        }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"NPC markers configured on {sources.Count} NPCs ({added} new). {materialNote} Save the scene (Ctrl+S), then test in Play mode.\n" +
                  "Available = amber downward triangle (pulsing); Completed = green check; nothing while unavailable, in conversation, in another camera mode, out of range or occluded.\n" +
                  report);
    }

    private static Material EnsureMaterial(out string note)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null)
        {
            note = $"Using existing {MaterialPath}.";
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            note = null;
            Debug.LogError("No unlit shader found (URP Unlit or Unlit/Color). No changes applied.");
            return null;
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets", "Materials");
        var material = new Material(shader) { name = "NpcStatusMarker", enableInstancing = true };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        AssetDatabase.CreateAsset(material, MaterialPath);
        AssetDatabase.SaveAssets();
        note = $"Created {MaterialPath} ({shader.name}).";
        return material;
    }
}
