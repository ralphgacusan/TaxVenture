using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Milestone T02 pilot, import-time silhouette treatment (Editor only; never
/// runs in a build or at runtime). One bounded refinement level for a
/// derived pilot mesh whose normals were already smoothed by the importer:
///
/// - Only DIAGNOSED edges are split: manifold, smooth-shaded (the same normal
///   on both sides at both ends, so not an intentional crease or hard edge)
///   and visibly faceted (adjacent face normals between minDihedral and
///   maxDihedral degrees). Flat areas, hard edges, open/boundary and
///   non-manifold edges are left exactly as they were.
/// - A split point sits on the curved PN-triangle edge built from the two end
///   normals, so a coarse curved outline becomes rounder; on a flat surface
///   it reduces to the straight midpoint. Results depend only on the welded
///   end positions/normals, so both sides of a UV seam get identical points
///   (no cracks).
/// - A triangle splits 1-to-2, 1-to-3 or 1-to-4 by how many of its edges were
///   split, so there are no T-junctions. UVs and colours interpolate; skin
///   weights blend (top four kept, renormalised); bind poses are untouched.
///
/// Pure data in/out (no UnityEditor types) so the pilot regression can run it
/// on parsed source geometry outside Unity.
/// </summary>
public static class T02PilotSilhouette
{
    public struct Settings
    {
        public float MinDihedral;
        public float MaxDihedral;
        public static Settings Default => new Settings { MinDihedral = 12f, MaxDihedral = 50f };
    }

    public sealed class MeshData
    {
        public Vector3[] Positions = new Vector3[0];
        public Vector3[] Normals = new Vector3[0];
        /// <summary>UV channels present on the mesh (each the vertex count long).</summary>
        public List<Vector2[]> Uvs = new List<Vector2[]>();
        public Color[] Colors;
        public BoneWeight[] Weights;
        public List<int[]> Submeshes = new List<int[]>();

        public int TriangleCount
        {
            get { int n = 0; foreach (int[] s in Submeshes) n += s.Length / 3; return n; }
        }
    }

    public struct Report
    {
        public int VerticesBefore, VerticesAfter, TrianglesBefore, TrianglesAfter;
        public int EdgesTotal, EdgesSplit, CreaseEdges, FlatEdges, SteepEdges, OpenOrNonManifoldEdges;
        public override string ToString() =>
            $"vertices {VerticesBefore}->{VerticesAfter}, triangles {TrianglesBefore}->{TrianglesAfter}; edges {EdgesTotal}: split {EdgesSplit}, " +
            $"kept crease {CreaseEdges}, flat {FlatEdges}, steep {SteepEdges}, open/non-manifold {OpenOrNonManifoldEdges}";
    }

    private const float WeldStep = 1e-5f;
    private const float SameNormal = 0.9999f;

    public static MeshData Refine(MeshData source, Settings settings, out Report report)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        report = new Report { VerticesBefore = source.Positions.Length, TrianglesBefore = source.TriangleCount };
        int vertexCount = source.Positions.Length;

        // Weld by position only: the edge identity across UV seams and hard edges.
        var weldIds = new int[vertexCount];
        var weld = new Dictionary<(long, long, long), int>();
        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 p = source.Positions[i];
            var key = ((long)Math.Round(p.x / WeldStep), (long)Math.Round(p.y / WeldStep), (long)Math.Round(p.z / WeldStep));
            if (!weld.TryGetValue(key, out int id)) { id = weld.Count; weld.Add(key, id); }
            weldIds[i] = id;
        }

        // Every corner's edge, keyed by welded end positions.
        var edges = new Dictionary<(int, int), List<(int sub, int tri, int corner)>>();
        for (int s = 0; s < source.Submeshes.Count; s++)
        {
            int[] tris = source.Submeshes[s];
            for (int t = 0; t + 2 < tris.Length; t += 3)
                for (int c = 0; c < 3; c++)
                {
                    int a = weldIds[tris[t + c]], b = weldIds[tris[t + (c + 1) % 3]];
                    if (a == b) continue;
                    var key = a < b ? (a, b) : (b, a);
                    if (!edges.TryGetValue(key, out var list)) edges[key] = list = new List<(int, int, int)>(2);
                    list.Add((s, t, c));
                }
        }

        var split = new HashSet<(int, int)>();
        foreach (var pair in edges)
        {
            report.EdgesTotal++;
            List<(int sub, int tri, int corner)> uses = pair.Value;
            if (uses.Count != 2) { report.OpenOrNonManifoldEdges++; continue; }
            int[] t0 = source.Submeshes[uses[0].sub], t1 = source.Submeshes[uses[1].sub];
            if (!SmoothAcross(source, weldIds, t0, uses[0].tri, t1, uses[1].tri, pair.Key)) { report.CreaseEdges++; continue; }
            float angle = Vector3.Angle(FaceNormal(source, t0, uses[0].tri), FaceNormal(source, t1, uses[1].tri));
            if (angle < settings.MinDihedral) { report.FlatEdges++; continue; }
            if (angle > settings.MaxDihedral) { report.SteepEdges++; continue; }
            split.Add(pair.Key);
            report.EdgesSplit++;
        }

        var result = new MeshData
        {
            Positions = null,
            Colors = source.Colors != null && source.Colors.Length == vertexCount ? source.Colors : null,
            Weights = source.Weights != null && source.Weights.Length == vertexCount ? source.Weights : null
        };
        var positions = new List<Vector3>(source.Positions);
        var normals = new List<Vector3>(source.Normals.Length == vertexCount ? source.Normals : new Vector3[vertexCount]);
        var uvs = new List<List<Vector2>>();
        foreach (Vector2[] channel in source.Uvs) uvs.Add(channel != null && channel.Length == vertexCount ? new List<Vector2>(channel) : null);
        var colors = result.Colors != null ? new List<Color>(result.Colors) : null;
        var weights = result.Weights != null ? new List<BoneWeight>(result.Weights) : null;

        // One new vertex per split (vertex pair): seams get their own vertex at the same position.
        var midpoints = new Dictionary<(int, int), int>();
        var edgePositions = new Dictionary<(int, int), Vector3>();
        int Midpoint(int a, int b)
        {
            var key = a < b ? (a, b) : (b, a);
            if (midpoints.TryGetValue(key, out int existing)) return existing;
            // Canonical order by welded id (then index), so both sides of a seam compute the same point.
            int first = a, second = b;
            if (weldIds[b] < weldIds[a] || weldIds[b] == weldIds[a] && b < a) { first = b; second = a; }
            Vector3 p0 = source.Positions[first], p1 = source.Positions[second];
            Vector3 n0 = SafeNormal(normals[first]), n1 = SafeNormal(normals[second]);
            // Smoothing permits nearly equal normals and welded positions. Recomputing
            // each UV seam's midpoint separately can open a crack after quantization.
            var edge = weldIds[first] < weldIds[second] ? (weldIds[first], weldIds[second]) : (weldIds[second], weldIds[first]);
            if (!edgePositions.TryGetValue(edge, out Vector3 point))
                edgePositions[edge] = point = CurvedMidpoint(p0, n0, p1, n1);
            positions.Add(point);
            normals.Add(SafeNormal(n0 + n1));
            for (int ch = 0; ch < uvs.Count; ch++) uvs[ch]?.Add((uvs[ch][a] + uvs[ch][b]) * 0.5f);
            colors?.Add(Color.Lerp(colors[a], colors[b], 0.5f));
            weights?.Add(Blend(weights[a], weights[b]));
            int index = positions.Count - 1;
            midpoints[key] = index;
            return index;
        }

        foreach (int[] tris in source.Submeshes)
        {
            var output = new List<int>(tris.Length * 2);
            for (int t = 0; t + 2 < tris.Length; t += 3)
            {
                int v0 = tris[t], v1 = tris[t + 1], v2 = tris[t + 2];
                bool s0 = IsSplit(split, weldIds, v0, v1), s1 = IsSplit(split, weldIds, v1, v2), s2 = IsSplit(split, weldIds, v2, v0);
                int count = (s0 ? 1 : 0) + (s1 ? 1 : 0) + (s2 ? 1 : 0);
                if (count == 0) { output.Add(v0); output.Add(v1); output.Add(v2); continue; }
                if (count == 3)
                {
                    int m0 = Midpoint(v0, v1), m1 = Midpoint(v1, v2), m2 = Midpoint(v2, v0);
                    Add(output, v0, m0, m2); Add(output, m0, v1, m1); Add(output, m2, m1, v2); Add(output, m0, m1, m2);
                    continue;
                }
                // Rotate so the pattern starts at edge (a, b).
                int a = v0, b = v1, c = v2; bool sa = s0, sb = s1, sc = s2;
                for (int r = 0; r < 3 && !(count == 1 ? sa : sa && sb); r++)
                {
                    (a, b, c) = (b, c, a);
                    (sa, sb, sc) = (sb, sc, sa);
                }
                if (count == 1)
                {
                    int m = Midpoint(a, b);
                    Add(output, a, m, c); Add(output, m, b, c);
                }
                else
                {
                    // Edges (a,b) and (b,c) split: corner triangle at b, the quad a-m0-m1-c split on its shorter diagonal.
                    int m0 = Midpoint(a, b), m1 = Midpoint(b, c);
                    Add(output, m0, b, m1);
                    if ((positions[a] - positions[m1]).sqrMagnitude <= (positions[m0] - positions[c]).sqrMagnitude)
                    { Add(output, a, m0, m1); Add(output, a, m1, c); }
                    else
                    { Add(output, a, m0, c); Add(output, m0, m1, c); }
                }
            }
            result.Submeshes.Add(output.ToArray());
        }

        result.Positions = positions.ToArray();
        result.Normals = normals.ToArray();
        foreach (List<Vector2> channel in uvs) result.Uvs.Add(channel?.ToArray());
        result.Colors = colors?.ToArray();
        result.Weights = weights?.ToArray();
        report.VerticesAfter = result.Positions.Length;
        report.TrianglesAfter = result.TriangleCount;
        return result;
    }

    private static void Add(List<int> output, int a, int b, int c) { output.Add(a); output.Add(b); output.Add(c); }

    private static bool IsSplit(HashSet<(int, int)> split, int[] weldIds, int a, int b)
    {
        int wa = weldIds[a], wb = weldIds[b];
        return wa != wb && split.Contains(wa < wb ? (wa, wb) : (wb, wa));
    }

    /// <summary>Both triangles carry the same normal at both shared ends (the importer did not split them as a hard edge).</summary>
    private static bool SmoothAcross(MeshData mesh, int[] weldIds, int[] t0, int i0, int[] t1, int i1, (int, int) edge)
    {
        foreach (int end in new[] { edge.Item1, edge.Item2 })
        {
            int a = CornerAt(weldIds, t0, i0, end), b = CornerAt(weldIds, t1, i1, end);
            if (a < 0 || b < 0) return false;
            if (a == b) continue;
            if (Vector3.Dot(SafeNormal(mesh.Normals[a]), SafeNormal(mesh.Normals[b])) < SameNormal) return false;
        }
        return true;
    }

    private static int CornerAt(int[] weldIds, int[] tris, int t, int weldId)
    {
        for (int c = 0; c < 3; c++) if (weldIds[tris[t + c]] == weldId) return tris[t + c];
        return -1;
    }

    private static Vector3 FaceNormal(MeshData mesh, int[] tris, int t)
    {
        Vector3 a = mesh.Positions[tris[t]], b = mesh.Positions[tris[t + 1]], c = mesh.Positions[tris[t + 2]];
        return SafeNormal(Vector3.Cross(b - a, c - a));
    }

    private static Vector3 SafeNormal(Vector3 v)
    {
        float m = v.magnitude;
        return m > 1e-12f ? v / m : Vector3.up;
    }

    /// <summary>The PN-triangle cubic edge's midpoint: (P0 + 3 b210 + 3 b120 + P1) / 8, symmetric in its ends.</summary>
    public static Vector3 CurvedMidpoint(Vector3 p0, Vector3 n0, Vector3 p1, Vector3 n1)
    {
        Vector3 b210 = (2f * p0 + p1 - Vector3.Dot(p1 - p0, n0) * n0) / 3f;
        Vector3 b120 = (2f * p1 + p0 - Vector3.Dot(p0 - p1, n1) * n1) / 3f;
        return (p0 + 3f * b210 + 3f * b120 + p1) / 8f;
    }

    /// <summary>Half of each end's influences, merged by bone, top four kept and renormalised.</summary>
    public static BoneWeight Blend(BoneWeight a, BoneWeight b)
    {
        var sum = new Dictionary<int, float>();
        void Take(int bone, float w) { if (w <= 0f) return; sum.TryGetValue(bone, out float s); sum[bone] = s + w * 0.5f; }
        Take(a.boneIndex0, a.weight0); Take(a.boneIndex1, a.weight1); Take(a.boneIndex2, a.weight2); Take(a.boneIndex3, a.weight3);
        Take(b.boneIndex0, b.weight0); Take(b.boneIndex1, b.weight1); Take(b.boneIndex2, b.weight2); Take(b.boneIndex3, b.weight3);
        var ordered = new List<KeyValuePair<int, float>>(sum);
        ordered.Sort((x, y) => y.Value != x.Value ? y.Value.CompareTo(x.Value) : x.Key.CompareTo(y.Key));
        if (ordered.Count > 4) ordered.RemoveRange(4, ordered.Count - 4);
        float total = 0f;
        foreach (var kv in ordered) total += kv.Value;
        var result = new BoneWeight();
        if (total <= 0f) return a;
        for (int i = 0; i < ordered.Count; i++)
        {
            int bone = ordered[i].Key;
            float w = ordered[i].Value / total;
            switch (i)
            {
                case 0: result.boneIndex0 = bone; result.weight0 = w; break;
                case 1: result.boneIndex1 = bone; result.weight1 = w; break;
                case 2: result.boneIndex2 = bone; result.weight2 = w; break;
                default: result.boneIndex3 = bone; result.weight3 = w; break;
            }
        }
        return result;
    }
}
