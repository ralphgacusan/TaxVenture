using UnityEngine;

/// <summary>
/// PURPOSE:
/// Milestone T03 (import time, Elijah's two derived copies only): keep the white shirt layer
/// UNDER the jacket. In Elijah's source the `torso` layer (shirt, sleeves) lies 0.002-0.004
/// model units (about 0.5-1 mm in the world) below the `coat`; 72 shirt vertices touch it.
/// The earlier silhouette refinement bulged shirt edges outward, so white triangles showed
/// through the brown jacket. Shirt vertices within <c>reach</c> of the coat are moved to sit
/// <c>inset</c> beneath its outward face; everything farther away is untouched (the visible
/// shirt front, collar and hands keep their shape). Pure data so the regression runs it.
/// </summary>
public static class ElijahClothingInset
{
    /// <summary>Model-space reach (about 7 mm in the world at Elijah's calibrated scale).</summary>
    public const float Reach = 0.03f;
    /// <summary>Model-space depth beneath the coat (about 4.7 mm in the world).</summary>
    public const float Inset = 0.02f;

    /// <summary>Moves <paramref name="inner"/> vertices (in the coat's space) under the outer surface; returns how many moved.</summary>
    public static int Apply(Vector3[] inner, Vector3[] outer, int[] outerTriangles, float reach = Reach, float inset = Inset)
    {
        if (inner == null || outer == null || outerTriangles == null) return 0;
        int moved = 0;
        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = -min;
        foreach (Vector3 p in outer) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        Vector3 pad = Vector3.one * reach;
        min -= pad; max += pad;
        for (int v = 0; v < inner.Length; v++)
        {
            Vector3 p = inner[v];
            if (p.x < min.x || p.y < min.y || p.z < min.z || p.x > max.x || p.y > max.y || p.z > max.z) continue;
            float best = reach;
            Vector3 normal = Vector3.zero, closest = Vector3.zero;
            for (int t = 0; t + 2 < outerTriangles.Length; t += 3)
            {
                Vector3 a = outer[outerTriangles[t]], b = outer[outerTriangles[t + 1]], c = outer[outerTriangles[t + 2]];
                Vector3 n = Vector3.Cross(b - a, c - a);
                if (n.sqrMagnitude < 1e-12f) continue;
                Vector3 q = ClosestPoint(p, a, b, c);
                float d = Vector3.Distance(p, q);
                if (d >= best) continue;
                best = d;
                closest = q;
                normal = n.normalized; // Unity front face: Cross(b - a, c - a) points outward.
            }
            if (normal == Vector3.zero) continue;
            float above = Vector3.Dot(p - closest, normal);
            if (above <= -inset) continue;
            inner[v] = p - normal * (above + inset);
            moved++;
        }
        return moved;
    }

    public static Vector3 ClosestPoint(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a, ac = c - a, ap = p - a;
        float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f) return a;
        Vector3 bp = p - b;
        float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3) return b;
        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f) return a + ab * (d1 / (d1 - d3));
        Vector3 cp = p - c;
        float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6) return c;
        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f) return a + ac * (d2 / (d2 - d6));
        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f) return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
        float denom = 1f / (va + vb + vc);
        return a + ab * (vb * denom) + ac * (vc * denom);
    }
}
