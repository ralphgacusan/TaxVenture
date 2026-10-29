using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Rosa's market stall (ticket T04), built at runtime under
/// Level2Gameplay/RosaStall from the user-selected originals
/// (Level2SceneAssets): the Building_4 store (mirrored on X like
/// Building_4 (1)) and Rosa (SM_Chr_Business_Female_03, humanoid idle).
/// Adds only what the stall needs: a records desk with a tray, the envelope
/// and four record sheets, a "Rosa's Laundry / Stall 04" sign and a few
/// cloth bundles, from Unity primitives.
///
/// PLACEMENT (measured, not guessed):
/// - Store height is fitted to StoreHeight (shop scale next to a person),
///   keeping its proportions; its awning side (the side that sticks out more
///   at mid height than at the base, found by ray-casting its own collider)
///   is its front.
/// - Candidate spots on rings around the plaza: the store and the space in
///   front of it must be free of solid colliders, on ground near plaza
///   height, and at least RouteClearance from the office-to-plaza route.
///   The spot nearest that route wins, front facing the plaza.
/// The Console logs the chosen transform; PROGRESS records it.
/// </summary>
public sealed class Level2RosaStall : MonoBehaviour
{
    public const string ChildName = "RosaStall";
    public const string PlazaPath = "Environment/Outside/Plaza";
    public const string OfficeDoorsPath = "Environment/OfficeBuilding/Rooms/Reception/Doors";
    public const string ReceptionistPath = "Environment/OfficeBuilding/Rooms/Reception/Receptionist";
    /// <summary>
    /// Optional scene marker (placed by TaxVenture > Level 2 > Copy Outside Environment From Level 1,
    /// where Zorbix stands in Level 1). When present, Rosa stands on it with her records desk and
    /// sign beside her, and no shop building is placed (user direction 2026-10-20).
    /// </summary>
    public const string AnchorPath = "Environment/Outside/Plaza/RosaStallAnchor";
    /// <summary>
    /// The town's own Building_4 shop (Level 1's environment, copied into Level 2). When the scene
    /// has it, Rosa's stall is set up in front of it (user direction 2026-10-20); it wins over the anchor.
    /// </summary>
    public const string SceneStorePath = "Env/Town/Building_4";
    /// <summary>Half size of the stall sign (metres), for the free-spot check.</summary>
    private static readonly Vector3 SignHalfExtents = new Vector3(0.75f, 0.95f, 0.2f);
    /// <summary>Rosa's model is fitted to this height (metres), whatever its import scale.</summary>
    public const float RosaHeight = 1.62f;

    // Layout targets (metres), tunable after the user's visual check.
    public const float StoreHeight = 3.8f;
    public const float MaxStoreHalfWidth = 5f;
    public const float RouteClearance = 2.5f;
    public const float ApronDepth = 3.2f;
    private static readonly float[] Radii = { 12f, 15f, 18f, 21f, 24f };
    private const float AngleStep = 15f;

    private static readonly Color Wood = new Color32(150, 104, 66, 255);
    private static readonly Color DarkWood = new Color32(92, 62, 40, 255);
    private static readonly Color Cream = new Color32(244, 236, 214, 255);
    private static readonly Color Manila = new Color32(222, 186, 120, 255);
    private static readonly Color Ink = new Color32(40, 34, 30, 255);
    private static readonly Color[] ClothColors =
    {
        new Color32(196, 64, 64, 255), new Color32(64, 120, 186, 255), new Color32(236, 196, 80, 255), new Color32(110, 168, 96, 255)
    };
    private static readonly Color[] SheetColors =
    {
        new Color32(246, 238, 212, 255), new Color32(214, 230, 246, 255), new Color32(250, 250, 244, 255),
        new Color32(240, 232, 200, 255), new Color32(236, 236, 236, 255), new Color32(252, 244, 220, 255)
    };

    public Transform Store { get; private set; }
    public RosaInteractable Rosa { get; private set; }
    public MarketRecordsDesk Desk { get; private set; }
    public bool Built { get; private set; }

    private readonly List<Material> materials = new List<Material>();
    private Transform envelope;
    private Transform envelopeFlap;
    private readonly List<Transform> sheets = new List<Transform>();
    private Transform tray;
    private int interactLayer = 3;

    private IEnumerator Start()
    {
        // Wait until the level (and Rosa's case) has started.
        float until = Time.unscaledTime + 6f;
        while (Time.unscaledTime < until && (CaseProgressionManager.Instance == null || !CaseProgressionManager.Instance.LevelStarted))
            yield return null;

        Level2SceneAssets assets = Level2SceneAssets.Load();
        if (assets == null || assets.storePrefab == null || assets.rosaPrefab == null)
        {
            Debug.LogWarning($"[Level2] Resources/{Level2SceneAssets.ResourcePath} is missing a prefab; Rosa's stall was not built.");
            yield break;
        }

        Transform receptionist = PlayerPlacement.Find(gameObject.scene, ReceptionistPath);
        if (receptionist != null) interactLayer = receptionist.gameObject.layer;

        Build(assets);
        CaseReviewState review = Level2OfficeStart.Review;
        if (review != null && review.HandoverDone) ShowRecordsOnDesk(instant: true);
    }

    // =========================================================
    // BUILD
    // =========================================================

    private void Build(Level2SceneAssets assets)
    {
        Transform sceneStore = PlayerPlacement.Find(gameObject.scene, SceneStorePath);
        if (sceneStore != null && sceneStore.gameObject.activeInHierarchy)
        {
            BuildAtSceneStore(assets, sceneStore);
            return;
        }

        Transform anchor = PlayerPlacement.Find(gameObject.scene, AnchorPath);
        if (anchor != null)
        {
            BuildAtAnchor(assets, anchor);
            return;
        }

        Transform plaza = PlayerPlacement.Find(gameObject.scene, PlazaPath);
        Vector3 plazaCenter = plaza != null ? plaza.position : Vector3.zero;
        Vector3 doors = PlayerPlacement.TryCenter(gameObject.scene, OfficeDoorsPath, out Vector3 d) ? d : plazaCenter + Vector3.back * 30f;
        float groundY = GroundNear(plazaCenter, plazaCenter.y, out float g) ? g : plazaCenter.y;

        // --- Store: instantiate out of the way, fit size, find its front. ---
        GameObject store = Instantiate(assets.storePrefab, transform);
        store.name = "RosaStore";
        Store = store.transform;
        Store.SetPositionAndRotation(plazaCenter + Vector3.down * 500f, Quaternion.identity);
        Store.localScale = new Vector3(-1f, 1f, 1f);
        Physics.SyncTransforms();
        Bounds raw = RendererBounds(store);
        float scale = raw.size.y > 0.01f ? StoreHeight / raw.size.y : 1f;
        float halfWidthAtScale = Mathf.Max(raw.extents.x, raw.extents.z) * scale;
        if (halfWidthAtScale > MaxStoreHalfWidth) scale *= MaxStoreHalfWidth / halfWidthAtScale;
        Store.localScale = new Vector3(-scale, scale, scale);
        Physics.SyncTransforms();

        Bounds b = RendererBounds(store);
        Vector3 pivotToCenter = b.center - Store.position;        // at identity rotation
        float pivotToBottom = b.min.y - Store.position.y;
        Vector3 localFront = DetectFront(store, b);
        float halfDepth = Extent(b, localFront);
        float halfWidth = Extent(b, Vector3.Cross(Vector3.up, localFront));

        // --- Spot: free, on ground, clear of the route, nearest the route. ---
        bool found = FindSpot(plazaCenter, doors, groundY, halfWidth, halfDepth, b.size.y, out Vector3 spot, out Vector3 front, out float spotGround);
        if (!found)
        {
            front = (plazaCenter - doors).normalized;
            front.y = 0f;
            front = front.sqrMagnitude > 0.01f ? -front.normalized : Vector3.forward;
            spot = plazaCenter - front * (Radii[0] + halfDepth);
            spotGround = groundY;
            Debug.LogWarning("[Level2] No free plaza-side spot passed every check; Rosa's stall uses the fallback beside the plaza. Check it in Play Mode.");
        }

        float yaw = Vector3.SignedAngle(localFront, front, Vector3.up);
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 pivot = spot - rotation * new Vector3(pivotToCenter.x, 0f, pivotToCenter.z);
        pivot.y = spotGround - pivotToBottom;
        Store.SetPositionAndRotation(pivot, rotation);
        Physics.SyncTransforms();

        // --- Front of the store: Rosa, desk, sign, cloth. ---
        Vector3 side = Vector3.Cross(Vector3.up, front);
        Vector3 face = spot + front * halfDepth;
        face.y = spotGround;
        BuildRosa(assets, Ground(face + front * 1.2f - side * Mathf.Min(1.4f, halfWidth * 0.3f), spotGround), front);
        BuildDesk(Ground(face + front * 1.5f + side * Mathf.Min(1.6f, halfWidth * 0.35f), spotGround), front, side);
        BuildSign(Ground(face + front * 0.9f - side * Mathf.Min(3f, halfWidth * 0.7f), spotGround), front);

        Built = true;
        Debug.Log($"[Level2] Rosa's stall: store at {pivot:F2}, yaw {yaw:F0}, scale {scale:F3} (mirrored X), " +
                  $"footprint {halfWidth * 2f:F1} x {halfDepth * 2f:F1} m, front {front:F2}, spot found: {found}.");
    }

    /// <summary>
    /// Rosa on the scene marker, facing where it faces; the records desk to her
    /// right and the stall sign to her left, both close by. No shop building.
    /// </summary>
    private void BuildAtAnchor(Level2SceneAssets assets, Transform anchor)
    {
        Vector3 front = anchor.forward;
        front.y = 0f;
        front = front.sqrMagnitude > 0.01f ? front.normalized : Vector3.forward;
        Vector3 side = Vector3.Cross(Vector3.up, front);
        float groundY = GroundNear(anchor.position, anchor.position.y, out float g) ? g : anchor.position.y;

        Vector3 rosa = Ground(anchor.position, groundY);
        Vector3 desk = Ground(anchor.position + side * 1.7f + front * 0.5f, groundY);
        BuildDesk(desk, front, side);
        BuildRosa(assets, rosa, FacingTo(rosa, desk, front));
        BuildSign(FreeSignSpot(anchor.position, front, side, groundY, new[]
        {
            -side * 1.5f - front * 0.2f, -side * 1.6f + front * 1.2f, side * 3.4f + front * 0.5f,
            front * 2.4f, -side * 2.6f + front * 1.8f, side * 1.7f + front * 2.6f
        }, rosa, desk), front);

        Built = true;
        Debug.Log($"[Level2] Rosa's stall on {AnchorPath} at {anchor.position:F2}, front {front:F2} (no shop building).");
    }

    /// <summary>
    /// The stall in front of the town's own shop, on its real front (the awning
    /// side, with the window and door): Rosa stands just beside the door, her
    /// records table in front of the window facing her, and the sign on the
    /// first free spot on the window side. Seen from the street, Building_4's
    /// door is on the right (mirrored instances: on the left).
    /// </summary>
    private void BuildAtSceneStore(Level2SceneAssets assets, Transform store)
    {
        Store = store;
        Bounds b = RendererBounds(store.gameObject);
        Vector3 front = DetectFrontAround(store, b);
        Vector3 side = Vector3.Cross(Vector3.up, front);

        // Measure along the shop's own axes (its world box is wider when it is turned).
        MeshFilter mesh = store.GetComponentInChildren<MeshFilter>();
        float halfDepth = Extent(b, front), halfWidth = Extent(b, side);
        Vector3 center = b.center;
        if (mesh != null && mesh.sharedMesh != null)
        {
            Bounds local = mesh.sharedMesh.bounds;
            Transform t = mesh.transform;
            Vector3 scale = t.lossyScale;
            bool alongForward = Mathf.Abs(Vector3.Dot(front, Flat(t.forward))) > 0.7f;
            halfDepth = alongForward ? local.extents.z * Mathf.Abs(scale.z) : local.extents.x * Mathf.Abs(scale.x);
            halfWidth = alongForward ? local.extents.x * Mathf.Abs(scale.x) : local.extents.z * Mathf.Abs(scale.z);
            center = t.TransformPoint(local.center);
        }
        float groundY = GroundNear(center + front * (halfDepth + 1f), b.min.y, out float g) ? g : b.min.y;
        Vector3 face = new Vector3(center.x, groundY, center.z) + front * halfDepth;

        // The door is on the street-right of the front (-side); a mirrored shop has it on the left.
        Vector3 doorSide = store.lossyScale.x < 0f ? side : -side;
        Vector3 door = face + doorSide * (halfWidth * 0.6f);
        Vector3 rosa = Ground(door + front * 1.1f - doorSide * 1.1f, groundY);
        Vector3 desk = Ground(door + front * 2.0f - doorSide * 2.7f, groundY);
        BuildDesk(desk, front, side);
        BuildRosa(assets, rosa, FacingTo(rosa, desk, front));
        BuildSign(FreeSignSpot(face, front, side, groundY, new[]
        {
            front * 1.2f - doorSide * (halfWidth * 0.75f), front * 2.4f - doorSide * (halfWidth * 0.9f),
            front * 3.4f - doorSide * (halfWidth * 0.4f), front * 1.2f - doorSide * (halfWidth + 1.2f),
            front * 3.6f + doorSide * (halfWidth * 0.2f), front * 4.4f
        }, rosa, desk, door + front * 0.6f), front);

        Built = true;
        Debug.Log($"[Level2] Rosa's stall at the front of {SceneStorePath} {store.position:F2}: front {front:F2}, " +
                  $"{halfWidth * 2f:F1} x {halfDepth * 2f:F1} m, Rosa by the door.");
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward;
    }

    /// <summary>
    /// The store's awning side among its own four sides: the side whose surface sticks out
    /// furthest at awning height (60-72% up) compared with its base.
    /// </summary>
    private static Vector3 DetectFrontAround(Transform store, Bounds b)
    {
        Collider[] colliders = store.GetComponentsInChildren<Collider>();
        Vector3 f = Flat(store.forward);
        Vector3 r = Vector3.Cross(Vector3.up, f);
        Vector3[] sides = { f, -f, r, -r };
        if (colliders.Length == 0) return f;
        Vector3 best = f;
        float bestOverhang = float.MinValue;
        foreach (Vector3 side in sides)
        {
            float extent = Extent(b, side);
            float baseOut = SurfaceOut(colliders, b, side, extent, 0.08f);
            float awning = Mathf.Max(SurfaceOut(colliders, b, side, extent, 0.6f),
                Mathf.Max(SurfaceOut(colliders, b, side, extent, 0.66f), SurfaceOut(colliders, b, side, extent, 0.72f)));
            if (awning - baseOut > bestOverhang) { bestOverhang = awning - baseOut; best = side; }
        }
        return best;
    }

    /// <summary>Rosa looks at her records table (flat); she still turns to the player while talking.</summary>
    private static Vector3 FacingTo(Vector3 from, Vector3 to, Vector3 fallback)
    {
        Vector3 d = to - from;
        d.y = 0f;
        return d.sqrMagnitude > 0.04f ? d.normalized : fallback;
    }

    /// <summary>The first candidate (offset from origin) where the sign touches no solid collider; the first one otherwise.</summary>
    private Vector3 FreeSignSpot(Vector3 origin, Vector3 front, Vector3 side, float groundY, Vector3[] offsets, params Vector3[] keepClearOf)
    {
        Quaternion rotation = Quaternion.LookRotation(front, Vector3.up);
        foreach (Vector3 offset in offsets)
        {
            Vector3 spot = Ground(origin + offset, groundY);
            if (Mathf.Abs(spot.y - groundY) > 0.6f) continue;
            bool crowded = false;
            foreach (Vector3 other in keepClearOf)
                if (new Vector2(spot.x - other.x, spot.z - other.z).magnitude < 1.4f) crowded = true;
            if (crowded) continue;
            if (!Blocked(spot + Vector3.up * (SignHalfExtents.y + 0.15f), SignHalfExtents, rotation)) return spot;
        }
        Debug.LogWarning("[Level2] No free spot for Rosa's sign; it uses the first one. Check it in Play Mode.");
        return Ground(origin + offsets[0], groundY);
    }

    private static Bounds RendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(root.transform.position, Vector3.one);
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static float Extent(Bounds b, Vector3 direction) =>
        Mathf.Abs(direction.x) * b.extents.x + Mathf.Abs(direction.z) * b.extents.z;

    /// <summary>The side whose surface sticks out more at mid height than at the base (the awning).</summary>
    private static Vector3 DetectFront(GameObject store, Bounds b)
    {
        Collider[] colliders = store.GetComponentsInChildren<Collider>();
        Vector3[] sides = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
        Vector3 best = Vector3.forward;
        float bestOverhang = float.MinValue;
        float bestMid = float.MinValue;
        Vector3 bestMidSide = Vector3.forward;
        foreach (Vector3 side in sides)
        {
            float extent = Extent(b, side);
            float baseOut = SurfaceOut(colliders, b, side, extent, 0.08f);
            float midOut = SurfaceOut(colliders, b, side, extent, 0.55f);
            if (midOut > bestMid) { bestMid = midOut; bestMidSide = side; }
            float overhang = midOut - baseOut;
            if (overhang > bestOverhang) { bestOverhang = overhang; best = side; }
        }
        return bestOverhang > 0.1f ? best : bestMidSide;
    }

    /// <summary>How far out from the centre the store's surface is on this side, at a height fraction (several rays, the furthest).</summary>
    private static float SurfaceOut(Collider[] colliders, Bounds b, Vector3 side, float extent, float heightFraction)
    {
        Vector3 across = Vector3.Cross(Vector3.up, side);
        float width = Extent(b, across);
        float best = float.MinValue;
        for (int i = -2; i <= 2; i++)
        {
            Vector3 origin = b.center + side * (extent + 2f) + across * (width * 0.35f * i);
            origin.y = b.min.y + b.size.y * heightFraction;
            var ray = new Ray(origin, -side);
            foreach (Collider c in colliders)
                if (c.Raycast(ray, out RaycastHit hit, extent * 2f + 4f))
                    best = Mathf.Max(best, extent + 2f - hit.distance);
        }
        return best;
    }

    private bool FindSpot(Vector3 plazaCenter, Vector3 doors, float groundY, float halfWidth, float halfDepth, float height,
        out Vector3 spot, out Vector3 front, out float spotGround)
    {
        spot = default;
        front = Vector3.forward;
        spotGround = groundY;
        float bestScore = float.MaxValue;
        float radius = Mathf.Sqrt(halfWidth * halfWidth + halfDepth * halfDepth);
        Vector3 a = new Vector3(doors.x, 0f, doors.z), c = new Vector3(plazaCenter.x, 0f, plazaCenter.z);

        foreach (float r in Radii)
            for (float angle = 0f; angle < 360f; angle += AngleStep)
            {
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 p = plazaCenter + dir * (r + radius);
                if (!GroundNear(p, groundY, out float py) || Mathf.Abs(py - groundY) > 0.6f) continue;
                Vector3 f = -dir;                                   // front faces the plaza
                Vector3 flatP = new Vector3(p.x, 0f, p.z);
                float routeDistance = DistanceToSegment(flatP, a, c);
                if (routeDistance < radius + ApronDepth + RouteClearance) continue;

                Quaternion rot = Quaternion.LookRotation(f, Vector3.up);
                Vector3 bodyCenter = new Vector3(p.x, py + 0.35f + height * 0.5f, p.z);
                if (Blocked(bodyCenter, new Vector3(halfWidth, height * 0.5f, halfDepth), rot)) continue;
                Vector3 apronCenter = new Vector3(p.x, py + 1.2f, p.z) + f * (halfDepth + ApronDepth * 0.5f);
                if (Blocked(apronCenter, new Vector3(halfWidth * 0.8f, 0.85f, ApronDepth * 0.5f), rot)) continue;

                float score = routeDistance;
                if (score >= bestScore) continue;
                bestScore = score;
                spot = new Vector3(p.x, py, p.z);
                front = f;
                spotGround = py;
            }
        return bestScore < float.MaxValue;
    }

    private bool Blocked(Vector3 center, Vector3 halfExtents, Quaternion rotation)
    {
        Transform player = CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;
        foreach (Collider hit in Physics.OverlapBox(center, halfExtents, rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit is TerrainCollider) continue;
            if (hit.transform.IsChildOf(transform)) continue;
            if (player != null && hit.transform.IsChildOf(player)) continue;
            return true;
        }
        return false;
    }

    private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
        return Vector3.Distance(p, a + ab * t);
    }

    /// <summary>Ground height just below a point (within 2.5 m up / 5 m down), ignoring the stall itself.</summary>
    private bool GroundNear(Vector3 point, float nearY, out float y)
    {
        y = nearY;
        Vector3 origin = new Vector3(point.x, nearY + 2.5f, point.z);
        float best = float.MinValue;
        foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, 7.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.normal.y < 0.7f) continue;
            if (hit.point.y > best) best = hit.point.y;
        }
        if (best == float.MinValue) return false;
        y = best;
        return true;
    }

    private Vector3 Ground(Vector3 point, float nearY)
    {
        point.y = GroundNear(point, nearY, out float y) ? y : nearY;
        return point;
    }

    // =========================================================
    // ROSA
    // =========================================================

    private void BuildRosa(Level2SceneAssets assets, Vector3 position, Vector3 front)
    {
        var holder = new GameObject("Rosa");
        holder.layer = interactLayer;
        holder.transform.SetParent(transform, false);
        holder.transform.SetPositionAndRotation(position, Quaternion.LookRotation(front, Vector3.up));

        GameObject model = Instantiate(assets.rosaPrefab, holder.transform);
        model.name = "RosaModel";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        FitHeight(model, position.y);
        Animator animator = model.GetComponentInChildren<Animator>();
        // T32: Level 1's shared Humanoid controller (her own Idle Relaxed clip, plus walking) when available.
        RuntimeAnimatorController controller = assets.npcController != null ? assets.npcController : assets.rosaIdleController;
        if (animator != null && controller != null)
        {
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
        }
        else
        {
            Debug.LogWarning("[Level2] Rosa has no Animator or idle controller; she may hold her rest pose.");
        }

        var capsule = holder.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.9f, 0f);
        capsule.height = 1.8f;
        capsule.radius = 0.35f;

        if (animator != null && assets.npcController != null)
        {
            // One owner for her body (T32): NpcLife stands her at the stall, turns her to the player in a
            // conversation and walks her to the office (Level2RosaConsultation asks it); the driver adds
            // idle variation and restrained speaking/listening motion over the clip.
            holder.SetActive(false);
            string idle = Level2Town.Instance != null ? Level2Town.Instance.Rosa.idle : null;
            holder.AddComponent<NpcAnimationDriver>().ConfigureStates(string.IsNullOrWhiteSpace(idle) ? "Idle Relaxed" : idle,
                Level2Town.WalkState, Level2Town.StrollState);
            holder.AddComponent<NpcLife>().ConfigureRuntime(NpcLife.Activity.Stand, null, null, null, new Vector2(3f, 6f), false, true);
            holder.SetActive(true);
        }

        Rosa = holder.AddComponent<RosaInteractable>();
        Rosa.Init(this);
    }

    /// <summary>Scales the model to RosaHeight and stands its feet on groundY, whatever its import scale.</summary>
    private static void FitHeight(GameObject model, float groundY)
    {
        Bounds b = RendererBounds(model);
        if (b.size.y < 0.01f) return;
        model.transform.localScale *= RosaHeight / b.size.y;
        b = RendererBounds(model);
        model.transform.position += Vector3.up * (groundY - b.min.y);
    }

    // =========================================================
    // DESK, ENVELOPE, RECORDS
    // =========================================================

    private void BuildDesk(Vector3 position, Vector3 front, Vector3 side)
    {
        var root = new GameObject("MarketRecordsDesk");
        root.layer = interactLayer;
        root.transform.SetParent(transform, false);
        // Long side along the store; the player stands on the plaza side.
        root.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-front, Vector3.up));

        Material wood = MaterialFor(Wood, "Desk");
        Material dark = MaterialFor(DarkWood, "Desk Dark");
        const float top = 0.78f, w = 1.4f, depth = 0.82f;
        Box("Top", root.transform, new Vector3(0f, top - 0.025f, 0f), new Vector3(w, 0.05f, depth), wood);
        foreach (float x in new[] { -w * 0.45f, w * 0.45f })
            foreach (float z in new[] { -depth * 0.4f, depth * 0.4f })
                Box("Leg", root.transform, new Vector3(x, (top - 0.05f) * 0.5f, z), new Vector3(0.06f, top - 0.05f, 0.06f), dark);

        // The collected records form a stack beneath the toolkit's active paper.
        tray = Child("RecordsTray", root.transform, new Vector3(0.04f, top, -0.015f));
        for (int i = 0; i < 3; i++)
        {
            Transform towel = Child("Folded Laundry " + (i + 1), root.transform,
                new Vector3(0.53f + (i % 2) * 0.008f, top + 0.013f + i * 0.022f, 0.255f));
            Material cloth = MaterialFor(ClothColors[i], "Folded Cloth " + i);
            Box("Fold", towel, Vector3.zero, new Vector3(0.19f, 0.022f, 0.115f), cloth);
            Box("Folded Edge", towel, new Vector3(0f, 0.009f, -0.049f), new Vector3(0.175f, 0.006f, 0.012f), cloth);
        }

        // Envelope on Rosa's end of the desk; her four record sheets wait inside it.
        envelope = Child("Envelope", root.transform, new Vector3(-w * 0.32f, top + 0.008f, 0.05f));
        Material manila = MaterialFor(Manila, "Envelope");
        Box("Body", envelope, Vector3.zero, new Vector3(0.26f, 0.014f, 0.18f), manila);
        envelopeFlap = Child("FlapHinge", envelope, new Vector3(0f, 0.008f, 0.09f));
        Box("Flap", envelopeFlap, new Vector3(0f, 0f, -0.045f), new Vector3(0.26f, 0.004f, 0.09f), manila);
        Label(envelope, new Vector3(0f, 0.009f, -0.02f), new Vector2(0.22f, 0.05f), "ROSA SANTOS\nMAY RECORDS", Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 180f, 0f));
        string[] recordIds = HandedRecordIds();
        for (int i = 0; i < recordIds.Length; i++)
        {
            Transform sheet = Child("Record " + recordIds[i], envelope, new Vector3(0f, 0.002f, 0f));
            Box("Paper", sheet, Vector3.zero, new Vector3(0.4f, 0.003f, 0.47f), MaterialFor(SheetColors[i % SheetColors.Length], "Sheet " + (i + 1)));
            Label(sheet, new Vector3(0f, 0.0025f, 0.06f), new Vector2(0.13f, 0.04f), recordIds[i], Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 180f, 0f));
            sheet.gameObject.SetActive(false);
            sheets.Add(sheet);
        }

        BoxCollider hit = root.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, top * 0.5f + 0.05f, 0f);
        hit.size = new Vector3(w, top + 0.1f, depth);

        // Where the camera sits: on the plaza side, looking down at the desk.
        // Close framing: roughly 90% of the view's width at 16:9, ten degrees off vertical.
        Transform viewpoint = Child("DeskViewpoint", root.transform, new Vector3(0f, 1.49f, -0.195f));
        viewpoint.localRotation = Quaternion.Euler(80f, 0f, 0f);

        Desk = root.AddComponent<MarketRecordsDesk>();
        Desk.Init(viewpoint, IncomeToolkit.Create(root.transform, top));
    }

    /// <summary>
    /// The handover on the desk: envelope slides in, its flap opens and the four
    /// records fan into the tray. Presentation only (the records were already
    /// collected); instant on a resumed review.
    /// </summary>
    /// <summary>The record ids Rosa hands over (case data), for the sheet labels on the desk.</summary>
    private static string[] HandedRecordIds()
    {
        var ids = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition?.review?.handoverDocumentIds : null;
        return ids != null && ids.Count > 0 ? ids.ToArray() : new[] { "D1", "D3", "D4", "D5" };
    }

    public void ShowRecordsOnDesk(bool instant)
    {
        if (envelope == null || tray == null) return;
        if (instant) PlaceSheetsInTray();
        else StartCoroutine(PlayHandover());
    }

    private IEnumerator PlayHandover()
    {
        Vector3 from = envelope.localPosition;
        Vector3 to = from + new Vector3(0.3f, 0f, 0f);
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f)
        {
            envelope.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        envelope.localPosition = to;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f)
        {
            envelopeFlap.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -150f, t), 0f, 0f);
            yield return null;
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
        for (int i = 0; i < sheets.Count; i++)
        {
            Transform sheet = sheets[i];
            sheet.gameObject.SetActive(true);
            sheet.SetParent(tray, true);
            Vector3 start = sheet.localPosition;
            Quaternion startRot = sheet.localRotation;
            SheetPose(i, out Vector3 endPos, out Quaternion endRot);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.18f)
            {
                float e = Mathf.SmoothStep(0f, 1f, t);
                sheet.localPosition = Vector3.Lerp(start, endPos, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.05f;
                sheet.localRotation = Quaternion.Slerp(startRot, endRot, e);
                yield return null;
            }
            sheet.localPosition = endPos;
            sheet.localRotation = endRot;
        }
        envelope.gameObject.SetActive(false);
    }

    private void PlaceSheetsInTray()
    {
        for (int i = 0; i < sheets.Count; i++)
        {
            sheets[i].gameObject.SetActive(true);
            sheets[i].SetParent(tray, false);
            SheetPose(i, out Vector3 pos, out Quaternion rot);
            sheets[i].localPosition = pos;
            sheets[i].localRotation = rot;
        }
        envelope.gameObject.SetActive(false);
    }

    private static void SheetPose(int index, out Vector3 position, out Quaternion rotation)
    {
        position = new Vector3((index % 3 - 1) * 0.002f, 0.002f + index * 0.003f, (index % 2) * 0.002f);
        rotation = Quaternion.identity;
    }

    // =========================================================
    // SIGN
    // =========================================================

    private void BuildSign(Vector3 position, Vector3 front)
    {
        Transform sign = Child("StallSign", transform, Vector3.zero);
        sign.SetPositionAndRotation(position, Quaternion.LookRotation(front, Vector3.up));
        Material dark = MaterialFor(DarkWood, "Sign Post");
        Box("PostLeft", sign, new Vector3(-0.6f, 0.9f, 0f), new Vector3(0.07f, 1.8f, 0.07f), dark);
        Box("PostRight", sign, new Vector3(0.6f, 0.9f, 0f), new Vector3(0.07f, 1.8f, 0.07f), dark);
        Box("Board", sign, new Vector3(0f, 1.55f, 0f), new Vector3(1.4f, 0.5f, 0.05f), MaterialFor(Cream, "Sign Board"));
        // Readable from the plaza (front) and from behind.
        Label(sign, new Vector3(0f, 1.55f, 0.03f), new Vector2(1.3f, 0.44f), "<b>ROSA'S LAUNDRY</b>\nStall 04 · Plaza Market", Quaternion.identity);
        Label(sign, new Vector3(0f, 1.55f, -0.03f), new Vector2(1.3f, 0.44f), "<b>ROSA'S LAUNDRY</b>\nStall 04 · Plaza Market", Quaternion.Euler(0f, 180f, 0f));
    }

    // =========================================================
    // PRIMITIVES
    // =========================================================

    private Transform Child(string name, Transform parent, Vector3 localPosition)
    {
        var go = new GameObject(name);
        go.layer = interactLayer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go.transform;
    }

    private void Box(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material) =>
        Prim(PrimitiveType.Cube, name, parent, localPosition, size, material);

    private GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 size, Material material)
    {
        GameObject prim = GameObject.CreatePrimitive(type);
        prim.name = name;
        prim.layer = interactLayer;
        DestroyImmediate(prim.GetComponent<Collider>());
        prim.transform.SetParent(parent, false);
        prim.transform.localPosition = localPosition;
        prim.transform.localScale = size;
        prim.GetComponent<MeshRenderer>().sharedMaterial = material;
        return prim;
    }

    private void Label(Transform parent, Vector3 localPosition, Vector2 size, string text, Quaternion localRotation)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.layer = interactLayer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        // TextMeshPro reads from its -z side.
        go.transform.localRotation = localRotation * Quaternion.Euler(0f, 180f, 0f);
        var label = go.AddComponent<TextMeshPro>();
        label.rectTransform.sizeDelta = size;
        label.font = HudTheme.Font;
        label.text = text;
        label.color = Ink;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 0.05f;
        label.fontSizeMax = 2f;
        label.richText = true;
        label.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private Material MaterialFor(Color color, string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = "Level2 " + name, color = color };
        materials.Add(material);
        return material;
    }

    private void OnDestroy()
    {
        foreach (Material material in materials)
            if (material != null) Destroy(material);
        materials.Clear();
    }
}
