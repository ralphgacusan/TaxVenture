using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The glowing arrows on the ground that lead to the current objective.
/// One dynamic mesh (a single draw call) rebuilt every frame: chevrons
/// spaced along the route, flowing forward, with a brightness wave that
/// travels toward the goal, fading in near the player and out at the end.
/// Unlit, transparent (built-in Sprites/Default, always in the build).
/// </summary>
public sealed class ObjectiveTrail : MonoBehaviour
{
    private const float StartOffset = 1.1f;
    private const float Lift = 0.06f;
    private const float ArrowSize = 0.8f;

    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private Material material;
    private readonly List<Vector3> verts = new List<Vector3>();
    private readonly List<Color32> colors = new List<Color32>();
    private readonly List<int> tris = new List<int>();
    private readonly List<Vector3> route = new List<Vector3>();
    private readonly List<float> routeDistance = new List<float>();
    private float flow;
    private float fade;
    private bool wanted;

    public float Spacing = 1.1f;
    public float Length = 26f;
    public float Speed = 1.6f;
    public Color Color = new Color32(100, 215, 255, 255);

    public static ObjectiveTrail Create(Transform parent)
    {
        var go = new GameObject("Objective Trail");
        go.transform.SetParent(parent, false);
        go.layer = 0;
        var trail = go.AddComponent<ObjectiveTrail>();
        trail.Init();
        return trail;
    }

    private void Init()
    {
        mesh = new Mesh { name = "Objective Trail" };
        mesh.MarkDynamic();
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("UI/Default");
        if (shader == null)
        {
            Debug.LogWarning("[Guide] No unlit shader for the objective trail; ground arrows disabled.");
            enabled = false;
            return;
        }
        material = new Material(shader) { name = "Objective Trail", renderQueue = 3100 };
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    /// <summary>Sets the route (world points, player first). Pass an empty list to clear.</summary>
    public void SetRoute(List<Vector3> points)
    {
        route.Clear();
        routeDistance.Clear();
        float total = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            if (i > 0) total += Flat(points[i] - points[i - 1]);
            route.Add(points[i]);
            routeDistance.Add(total);
        }
    }

    public void SetVisible(bool visible) => wanted = visible;

    private static float Flat(Vector3 v)
    {
        v.y = 0f;
        return v.magnitude;
    }

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        fade = Mathf.MoveTowards(fade, wanted && route.Count >= 2 ? 1f : 0f, dt * 3f);
        verts.Clear();
        colors.Clear();
        tris.Clear();

        if (fade > 0f && route.Count >= 2)
        {
            flow = Mathf.Repeat(flow + Speed * dt, Spacing);
            float total = routeDistance[routeDistance.Count - 1];
            float end = Mathf.Min(total - 0.6f, Length);
            float now = Time.unscaledTime;
            for (float d = StartOffset + flow; d <= end; d += Spacing)
            {
                Sample(d, out Vector3 p, out Vector3 forward);
                p = SnapToGround(p);
                float inFade = Mathf.Clamp01((d - StartOffset) / 1.2f);
                float outFade = Mathf.Clamp01((end - d) / 3f);
                float wave = 0.55f + 0.45f * Mathf.Max(0f, Mathf.Sin((d - now * 6f) * 0.45f));
                float alpha = fade * inFade * Mathf.Lerp(0.25f, 1f, outFade) * wave;
                if (alpha <= 0.01f) continue;
                GuideArt.AddGroundChevron(verts, colors, tris, p + Vector3.up * Lift, forward,
                    ArrowSize * (0.9f + 0.15f * wave), Color, alpha);
            }
        }

        mesh.Clear();
        if (verts.Count > 0)
        {
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0, true);
        }
        meshRenderer.enabled = verts.Count > 0;
    }

    private readonly RaycastHit[] groundHits = new RaycastHit[8];

    /// <summary>Puts an arrow on the visible ground under it (the route is a straight-line simplification).</summary>
    private Vector3 SnapToGround(Vector3 p)
    {
        int n = Physics.RaycastNonAlloc(p + Vector3.up * 1.2f, Vector3.down, groundHits, 2.6f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Transform player = GuideTargets.Player;
        float best = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            RaycastHit h = groundHits[i];
            if (h.normal.y < 0.5f) continue;
            if (player != null && h.transform.IsChildOf(player)) continue;
            Rigidbody body = h.collider.attachedRigidbody;
            if (body != null && !body.isKinematic) continue;
            if (h.point.y > best) best = h.point.y;
        }
        return best > float.MinValue ? new Vector3(p.x, best, p.z) : p;
    }

    private void Sample(float distance, out Vector3 point, out Vector3 forward)
    {
        for (int i = 1; i < route.Count; i++)
        {
            if (routeDistance[i] < distance && i < route.Count - 1) continue;
            float segment = routeDistance[i] - routeDistance[i - 1];
            float t = segment > 0.0001f ? Mathf.Clamp01((distance - routeDistance[i - 1]) / segment) : 0f;
            point = Vector3.Lerp(route[i - 1], route[i], t);
            forward = route[i] - route[i - 1];
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            return;
        }
        point = route[route.Count - 1];
        forward = Vector3.forward;
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }
}
