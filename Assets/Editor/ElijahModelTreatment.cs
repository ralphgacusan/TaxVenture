using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Only the two explicitly adopted Elijah copies are treated (originals and every other model untouched).
/// - Normals: calculated at 70 degrees (smooth jacket, face and hair without faceting).
/// - Silhouette refinement: OUTER layers only (coat, head, hair, ears). The shirt layer under the
///   coat, and the belt/tie lying on it, keep their source geometry: refining them bulged white
///   shirt triangles through the jacket (the reported bright angular patches).
/// - The shirt is inset just beneath the coat where they touch (ElijahClothingInset).
/// </summary>
public sealed class ElijahModelTreatment : AssetPostprocessor
{
    public override uint GetVersion() => 4;

    private static readonly HashSet<string> Refined = new HashSet<string> { "coat", "head", "hair", "ears" };

    private bool InScope => assetPath == "Assets/Resources/Player/Elijah_Exploration_Smooth.fbx" ||
                            assetPath == "Assets/Resources/Player/Elijah_Conversation_Smooth.fbx";

    private void OnPreprocessModel()
    {
        if (!InScope) return;
        var importer = (ModelImporter)assetImporter;
        importer.isReadable = true;
        importer.importNormals = ModelImporterNormals.Calculate;
        importer.normalSmoothingAngle = 70f;
        importer.normalSmoothingSource = ModelImporterNormalSmoothingSource.FromAngle;
    }

    private void OnPostprocessModel(GameObject root)
    {
        if (!InScope || root == null) return;
        // Moving a derived FBX away from its texture folder must not change its materials.
        string sourcePath = assetPath.Contains("Exploration")
            ? "Assets/3D Models/npc_elijah/Elijah_Walking.fbx"
            : "Assets/3D Models/npc_elijah/npc_elijah_Idle.fbx";
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null) throw new System.InvalidOperationException("Elijah source model is unavailable: " + sourcePath);
        var materials = new Dictionary<string, Material[]>();
        foreach (SkinnedMeshRenderer original in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            materials[original.name] = original.sharedMaterials;
        var seen = new HashSet<Mesh>();
        SkinnedMeshRenderer coat = null, shirt = null;
        foreach (SkinnedMeshRenderer skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!materials.TryGetValue(skin.name, out Material[] slots))
                throw new System.InvalidOperationException("Elijah material source not found: " + skin.name);
            skin.sharedMaterials = slots;
            if (Refined.Contains(skin.name)) T02PilotTreatment.RefineImportedMesh(skin.sharedMesh, true, seen, skin.name, assetPath);
            if (skin.name == "coat") coat = skin;
            if (skin.name == "torso") shirt = skin;
        }
        if (coat == null || shirt == null || coat.sharedMesh == null || shirt.sharedMesh == null)
            throw new System.InvalidOperationException("Elijah coat/torso layers not found in " + assetPath);

        // Both layers in the model root's space (their own node transforms), inset, then back.
        Matrix4x4 coatToRoot = root.transform.worldToLocalMatrix * coat.transform.localToWorldMatrix;
        Matrix4x4 shirtToRoot = root.transform.worldToLocalMatrix * shirt.transform.localToWorldMatrix;
        Vector3[] outer = coat.sharedMesh.vertices;
        for (int i = 0; i < outer.Length; i++) outer[i] = coatToRoot.MultiplyPoint3x4(outer[i]);
        Vector3[] inner = shirt.sharedMesh.vertices;
        for (int i = 0; i < inner.Length; i++) inner[i] = shirtToRoot.MultiplyPoint3x4(inner[i]);
        int moved = ElijahClothingInset.Apply(inner, outer, coat.sharedMesh.triangles);
        Matrix4x4 rootToShirt = shirtToRoot.inverse;
        for (int i = 0; i < inner.Length; i++) inner[i] = rootToShirt.MultiplyPoint3x4(inner[i]);
        shirt.sharedMesh.vertices = inner;
        shirt.sharedMesh.RecalculateBounds();
        Debug.Log($"[ElijahModelTreatment] {assetPath}: refined {string.Join("/", Refined)}; shirt kept under the coat ({moved} vertices inset).");
    }
}
