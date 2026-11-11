using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Milestone T03: one small, optional practice curb in Level 1's plaza near
/// the start, for the first JUMP. Purely optional: it gates nothing, awards
/// nothing, and a clear flat route of the same width runs beside it.
///
/// - Placement is deterministic: around the player's saved start pose (Level 1
///   never saves a player pose, so it is the same every load), the first
///   candidate whose ground is flat outdoor plaza at the start's height and
///   whose curb volume AND flat-route volume beside it are free of colliders.
///   If none is free, no curb is built (logged); nothing else changes.
/// - 0.18 m high (below the 0.25 m target), 1.6 m long, a solid collider;
///   a thin yellow edge marks it. Materials are runtime copies of the ground's
///   own material (tinted), so no new asset is added.
/// - Built once per scene under one root, "T03 Exploration".
/// </summary>
public static class ExplorationPracticeCurb
{
    public const string RootName = "T03 Exploration";
    public const string CurbName = "Practice Curb";
    public const float Height = 0.18f;
    public const float Length = 1.6f;
    public const float Depth = 0.45f;
    public const float RouteGap = 0.4f;
    public const float RouteWidth = 1.2f;

    private static readonly Collider[] overlap = new Collider[16];

    /// <summary>The curb in this scene (built near <paramref name="start"/> on first call), or null when no clear spot exists.</summary>
    public static Transform Ensure(Scene scene, Transform player)
    {
        if (!scene.IsValid() || player == null) return null;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == RootName) return root.transform.Find(CurbName);

        Vector3 start = player.position;
        if (!Ground(start, player, out Vector3 startGround, out Renderer startFloor)) { Debug.Log("[T03] No outdoor ground under the start: no practice curb."); return null; }
        float[] distances = { 4f, 5.5f, 7f };
        for (int d = 0; d < distances.Length; d++)
            for (int a = 0; a < 12; a++)
            {
                float angle = a * 30f;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 centre = startGround + dir * distances[d];
                Quaternion facing = Quaternion.LookRotation(dir, Vector3.up);   // the curb runs across the way from the start
                if (!Clear(centre, facing, player, startGround.y, out Renderer floor)) continue;
                return Build(scene, centre, facing, floor != null ? floor : startFloor);
            }
        Debug.Log("[T03] No clear flat spot near the start: no practice curb was placed.");
        return null;
    }

    /// <summary>Flat outdoor plaza ground at the start's height for the curb and for a route beside it; nothing solid in either volume.</summary>
    private static bool Clear(Vector3 centre, Quaternion facing, Transform player, float groundY, out Renderer floor)
    {
        floor = null;
        Vector3 side = facing * Vector3.right;
        foreach (Vector3 probe in new[] { centre, centre + side * (Length * 0.5f + RouteGap + RouteWidth * 0.5f), centre - side * (Length * 0.5f) })
        {
            if (!Ground(probe, player, out Vector3 g, out Renderer r) || Mathf.Abs(g.y - groundY) > 0.12f) return false;
            if (probe == centre) floor = r;
        }
        // The curb and the flat route beside it must be free of anything solid (people, props, walls).
        Vector3 halfCurb = new Vector3(Length * 0.5f + 0.3f, 0.9f, Depth * 0.5f + 0.6f);
        Vector3 routeCentre = centre + side * (Length * 0.5f + RouteGap + RouteWidth * 0.5f);
        Vector3 halfRoute = new Vector3(RouteWidth * 0.5f, 0.9f, Depth * 0.5f + 0.6f);
        return Empty(centre + Vector3.up * 1.0f, halfCurb, facing, player) && Empty(routeCentre + Vector3.up * 1.0f, halfRoute, facing, player);
    }

    private static bool Empty(Vector3 centre, Vector3 half, Quaternion rotation, Transform player)
    {
        int count = Physics.OverlapBoxNonAlloc(centre, half, overlap, rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (overlap[i] != null && !overlap[i].transform.IsChildOf(player)) return false;
        return true;
    }

    /// <summary>The outdoor floor (under "Outside"/"Env", like AudioEnvironment) at a point, its height and renderer.</summary>
    private static bool Ground(Vector3 at, Transform player, out Vector3 point, out Renderer renderer)
    {
        point = at;
        renderer = null;
        if (!Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
        if (hit.collider.transform.IsChildOf(player) || hit.normal.y < 0.97f) return false;
        bool outdoor = false;
        for (Transform part = hit.collider.transform; part != null; part = part.parent)
            if (part.name == "Outside" || part.name == "Env") { outdoor = true; break; }
        if (!outdoor) return false;
        point = hit.point;
        renderer = hit.collider.GetComponent<Renderer>();
        return true;
    }

    private static Transform Build(Scene scene, Vector3 centre, Quaternion facing, Renderer floor)
    {
        var root = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        curb.name = CurbName;
        curb.transform.SetParent(root.transform, false);
        curb.transform.SetPositionAndRotation(centre + Vector3.up * (Height * 0.5f), facing);
        curb.transform.localScale = new Vector3(Length, Height, Depth);
        GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        edge.name = "Edge Marking";
        Object.Destroy(edge.GetComponent<Collider>());
        edge.transform.SetParent(curb.transform, false);
        edge.transform.localPosition = new Vector3(0f, 0.5f, -0.38f);
        edge.transform.localScale = new Vector3(1f, 0.04f, 0.18f);
        Material source = floor != null ? floor.sharedMaterial : null;
        Tint(curb.GetComponent<Renderer>(), source, new Color(0.74f, 0.73f, 0.70f));
        Tint(edge.GetComponent<Renderer>(), source, new Color(0.93f, 0.78f, 0.25f));
        Debug.Log($"[T03] Practice curb placed at {centre} ({Height:0.00} m high), flat route beside it.");
        return curb.transform;
    }

    private static void Tint(Renderer renderer, Material source, Color color)
    {
        if (renderer == null || source == null) return;
        var material = new Material(source) { name = "Practice curb (runtime copy)" };
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", null);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", null);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        renderer.sharedMaterial = material;
    }
}
