using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The office scenes hold about 1,650 separate renderers and none is marked static, so a phone
/// issues a draw call for almost every bush, road tile and wall. While building an Android player,
/// this marks scenery that never moves for static batching, on the build's own copy of the scene.
/// Scene files, the Editor and Play Mode are unchanged.
/// </summary>
public sealed class AndroidStaticBatchingBuildStep : IProcessSceneWithReport
{
    private static readonly string[] SceneryMeshPrefixes = { "SM_Env_", "SM_Generic_", "SM_Bld_", "rpgpp_lt_" };
    private static readonly string[] MovingNameParts = { "door", "gate", "ocean", "water", "lid", "chest" };

    public int callbackOrder => 1000;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null || report.summary.platform != BuildTarget.Android) return;

        HashSet<Transform> referenced = CollectReferencedTransforms(scene);
        int marked = 0, renderers = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderers++;
                if (!CanBatch(renderer, referenced)) continue;
                GameObject target = renderer.gameObject;
                GameObjectUtility.SetStaticEditorFlags(target,
                    GameObjectUtility.GetStaticEditorFlags(target) | StaticEditorFlags.BatchingStatic);
                marked++;
            }
        Debug.Log($"[AndroidStaticBatching] {scene.name}: {marked} of {renderers} renderers batched.");
    }

    private static bool CanBatch(MeshRenderer renderer, HashSet<Transform> referenced)
    {
        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        if (mesh == null || !IsScenery(mesh.name) || NamesSomethingMoving(mesh.name)) return false;
        foreach (Material material in renderer.sharedMaterials)
            if (material == null || material.shader == null || !KeepsLookWhenBatched(material.shader.name))
                return false;

        for (Transform t = renderer.transform; t != null; t = t.parent)
        {
            if (referenced.Contains(t) || NamesSomethingMoving(t.name)) return false;
            foreach (Component component in t.GetComponents<Component>())
                if (component == null || !IsPlainSceneryComponent(component)) return false;
        }
        return true;
    }

    /// <summary>Other shaders can rely on object space (wind, water), which batching changes.</summary>
    private static bool KeepsLookWhenBatched(string shaderName) =>
        shaderName.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal) ||
        shaderName == "Synty/Generic_Basic" || shaderName == "Synty/Generic_Standard";

    private static bool IsScenery(string meshName)
    {
        foreach (string prefix in SceneryMeshPrefixes)
            if (meshName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static bool NamesSomethingMoving(string name)
    {
        foreach (string part in MovingNameParts)
            if (name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    private static bool IsPlainSceneryComponent(Component component) =>
        component is Transform || component is MeshFilter || component is MeshRenderer || component is Collider;

    /// <summary>Anything a script holds a reference to may be moved by it, so it and its children stay dynamic.</summary>
    private static HashSet<Transform> CollectReferencedTransforms(Scene scene)
    {
        var result = new HashSet<Transform>();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                using (var serialized = new SerializedObject(behaviour))
                {
                    SerializedProperty property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (property.objectReferenceValue is GameObject go) result.Add(go.transform);
                        else if (property.objectReferenceValue is Component component) result.Add(component.transform);
                    }
                }
            }
        return result;
    }
}
