using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Milestone T02 pilot import step, scoped to the derived copies under
/// Assets/Pilot/T02/Derived/ only. Every other model in the project is
/// ignored (the originals keep their own import settings and meshes).
///
/// - "t02:shading" (importer userData): the .meta already asks the importer to
///   calculate normals by angle; this step only logs the imported counts.
/// - "t02:silhouette": additionally applies T02PilotSilhouette once to each
///   mesh (no blend shapes), keeping bind poses, submeshes and materials.
///
/// The baked result is the derived asset itself (no runtime smoothing). Each
/// import writes one "[T02 pilot]" line per mesh to the Editor log with the
/// imported vertex/triangle counts, the evidence the pilot sheet asks for.
/// </summary>
public sealed class T02PilotTreatment : AssetPostprocessor
{
    public const string DerivedFolder = "Assets/Pilot/T02/Derived/";
    public const string ShadingFlag = "t02:shading";
    public const string SilhouetteFlag = "t02:silhouette";

    public override uint GetVersion() => 3;

    private void OnPreprocessModel()
    {
        if (InScope(assetPath) && assetImporter.userData.Contains(SilhouetteFlag))
            ((ModelImporter)assetImporter).isReadable = true;
    }

    public static bool InScope(string path) => !string.IsNullOrEmpty(path) && path.Replace('\\', '/').StartsWith(DerivedFolder);

    private void OnPostprocessModel(GameObject root)
    {
        if (!InScope(assetPath) || root == null) return;
        string flags = assetImporter != null ? assetImporter.userData ?? "" : "";
        bool silhouette = flags.Contains(SilhouetteFlag);
        if (!silhouette && !flags.Contains(ShadingFlag)) return;
        var seen = new HashSet<Mesh>();
        foreach (SkinnedMeshRenderer skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            Treat(skinned.sharedMesh, silhouette, seen, skinned.name);
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            Treat(filter.sharedMesh, silhouette, seen, filter.name);
    }

    private void Treat(Mesh mesh, bool silhouette, HashSet<Mesh> seen, string owner) =>
        RefineImportedMesh(mesh, silhouette, seen, owner, assetPath);

    internal static void RefineImportedMesh(Mesh mesh, bool silhouette, HashSet<Mesh> seen, string owner, string assetPath)
    {
        if (mesh == null || !seen.Add(mesh)) return;
        string before = $"{mesh.vertexCount} vertices / {Triangles(mesh)} triangles";
        if (!silhouette)
        {
            Debug.Log($"[T02 pilot] {assetPath} :: {owner}: shading only, imported {before}.");
            return;
        }
        if (mesh.blendShapeCount > 0)
        {
            Debug.Log($"[T02 pilot] {assetPath} :: {owner}: {mesh.blendShapeCount} blend shape(s); silhouette step skipped, imported {before}.");
            return;
        }
        if (!mesh.isReadable || mesh.vertexCount == 0)
            throw new System.InvalidOperationException("[T02 pilot] Refusing to clear an unreadable/empty mesh: " + assetPath + " :: " + owner);
        var data = new T02PilotSilhouette.MeshData
        {
            Positions = mesh.vertices,
            Normals = mesh.normals,
            Colors = mesh.colors != null && mesh.colors.Length == mesh.vertexCount ? mesh.colors : null,
            Weights = mesh.boneWeights != null && mesh.boneWeights.Length == mesh.vertexCount ? mesh.boneWeights : null
        };
        for (int channel = 0; channel < 4; channel++)
        {
            var uv = new List<Vector2>();
            mesh.GetUVs(channel, uv);
            if (uv.Count == mesh.vertexCount) data.Uvs.Add(uv.ToArray());
            else break;
        }
        for (int s = 0; s < mesh.subMeshCount; s++) data.Submeshes.Add(mesh.GetTriangles(s));

        T02PilotSilhouette.MeshData refined = T02PilotSilhouette.Refine(data, T02PilotSilhouette.Settings.Default, out T02PilotSilhouette.Report report);
        if (refined.Positions.Length < data.Positions.Length || refined.TriangleCount < data.TriangleCount ||
            refined.Weights != null && refined.Weights.Length != refined.Positions.Length)
            throw new System.InvalidOperationException("[T02 pilot] Invalid refinement: " + assetPath + " :: " + owner);
        Matrix4x4[] bindposes = mesh.bindposes;
        int subMeshes = mesh.subMeshCount;
        mesh.Clear();
        if (refined.Positions.Length > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = refined.Positions;
        mesh.normals = refined.Normals;
        for (int channel = 0; channel < refined.Uvs.Count; channel++)
            if (refined.Uvs[channel] != null) mesh.SetUVs(channel, refined.Uvs[channel]);
        if (refined.Colors != null) mesh.colors = refined.Colors;
        if (refined.Weights != null) mesh.boneWeights = refined.Weights;
        if (bindposes != null && bindposes.Length > 0) mesh.bindposes = bindposes;
        mesh.subMeshCount = subMeshes;
        for (int s = 0; s < subMeshes; s++) mesh.SetTriangles(refined.Submeshes[s], s);
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        if (mesh.vertexCount != refined.Positions.Length || Triangles(mesh) != refined.TriangleCount)
            throw new System.InvalidOperationException("[T02 pilot] Imported geometry write failed: " + assetPath + " :: " + owner);
        Debug.Log($"[T02 pilot] {assetPath} :: {owner}: silhouette {report}; imported before {before}.");
    }

    private static int Triangles(Mesh mesh)
    {
        long indices = 0;
        for (int s = 0; s < mesh.subMeshCount; s++) indices += mesh.GetIndexCount(s);
        return (int)(indices / 3);
    }
}
