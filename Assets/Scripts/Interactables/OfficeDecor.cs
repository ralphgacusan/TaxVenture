using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Marks a runtime-built decor piece (T13) so placement searches - the keepsake stand, other decor - ignore it.</summary>
public sealed class OfficeDecorPiece : MonoBehaviour
{
    public string ItemId = "";
}

/// <summary>
/// PURPOSE:
/// Installs owned office decor in the Level 1 office (Campaign Foundation
/// T13): each piece at the display anchor DecorRules.Resolve gives it, the
/// same on every load. The pencil cup rests on the right corner of the desk
/// as seen from the seat (the free spot nearest the far corner, clear of the
/// lamp, plant, stamps, stacks and the keepsake stand); the island print
/// hangs on the wall behind the filing shelf, above its top and below the
/// ceiling, nudged along the wall if a wall prop is in the way.
///
/// FEEL: a piece bought since the last visit waits out of sight and arrives
/// the first time the player looks at it: a short 0.3 s lift-and-settle
/// with one soft placement sound, then it is remembered as arrived (story
/// runs; other runs replay the arrival once per visit). Nothing moves before
/// the purchase is saved: pieces only follow the committed save.
///
/// T18: the same pieces follow the player into Level 2's office (a copy of
/// Level 1's, so the authored anchors resolve there too). A piece with no
/// usable spot in the loaded office stays owned (nothing is sold, refunded or
/// moved in the save) and the player is told once per visit.
///
/// DOES NOT: decide ownership or anchors (ShopRules / DecorRules), or move
/// anything in the scene file.
/// </summary>
[DefaultExecutionOrder(175)]
public sealed class OfficeDecor : MonoBehaviour
{
    private const float ArrivalSeconds = 0.3f;
    private const float ArrivalRange = 7f;
    private const float DeskMargin = 0.03f;
    private const float DeskGap = 0.015f;
    private static readonly float[] WallShifts = { 0f, 0.3f, -0.3f, 0.6f, -0.6f };

    private sealed class Piece
    {
        public string ItemId, AnchorId;
        public GameObject Root;
        public Transform Surface;
        public Bounds Bounds;
        public Vector3 Rest;
        public bool Waiting;
        public float ArriveAt = -1f;
    }

    private Scene scene;
    private readonly List<Piece> pieces = new List<Piece>();
    private readonly HashSet<string> arrivedThisVisit = new HashSet<string>(System.StringComparer.Ordinal);
    private readonly HashSet<string> warned = new HashSet<string>(System.StringComparer.Ordinal);
    private readonly HashSet<string> told = new HashSet<string>(System.StringComparer.Ordinal);
    private readonly List<string> untold = new List<string>();
    private bool dirty = true;

    public static OfficeDecor Instance { get; private set; }

    public static OfficeDecor Create(Scene scene)
    {
        var host = new GameObject("Office Decor (T13)");
        if (scene.IsValid()) SceneManager.MoveGameObjectToScene(host, scene);
        OfficeDecor decor = host.AddComponent<OfficeDecor>();
        decor.scene = scene;
        Instance = decor;
        return decor;
    }

    private void OnEnable() => PlayerProgressManager.ProgressChanged += OnProgress;

    private void OnDisable() => PlayerProgressManager.ProgressChanged -= OnProgress;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnProgress(PlayerProgressData _) => dirty = true;

    // =========================================================
    // INSTALL
    // =========================================================

    private void Refresh()
    {
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        PlayerProgressData data = manager != null ? manager.CurrentProgress : null;
        ShopCatalogue catalogue = ShopCatalogueAsset.Current;
        var unplaced = new List<string>();
        List<DecorPlacement> shown = data != null && data.HasCampaign ? DecorRules.Resolve(data.campaign, catalogue, unplaced) : new List<DecorPlacement>();
        foreach (string id in unplaced)
        {
            Warn(id, "has no free display spot in the office");
            Unplaced(id);
        }

        // Remove pieces that are gone or moved anchor; keep the rest exactly where they are.
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            Piece piece = pieces[i];
            DecorPlacement now = shown.Find(p => p.itemId == piece.ItemId);
            if (now != null && now.anchorId == piece.AnchorId) continue;
            if (piece.Root != null) Destroy(piece.Root);
            pieces.RemoveAt(i);
        }
        // Install new ones in save order, so a fresh load builds them in the same order a purchase did.
        foreach (DecorPlacement placement in shown)
        {
            if (pieces.Exists(p => p.ItemId == placement.itemId)) continue;
            Piece piece = Install(catalogue, placement);
            if (piece != null) pieces.Add(piece);
        }
    }

    private Piece Install(ShopCatalogue catalogue, DecorPlacement placement)
    {
        ShopDecorAnchor anchor = catalogue.FindAnchor(placement.anchorId);
        if (anchor == null) return null;
        Vector3 position;
        Quaternion rotation;
        Transform surface = null;
        string problem;
        bool placed = anchor.kind == ShopDecorAnchor.KindWall
            ? ResolveWall(anchor, out position, out rotation, out problem)
            : ResolveSurface(anchor, out position, out rotation, out surface, out problem);
        if (!placed)
        {
            Warn(placement.itemId + "/" + anchor.id, problem + "; tune \"anchors\" in shop.json");
            Unplaced(placement.itemId);
            return null;
        }
        GameObject root = DecorModels.Build(placement.itemId, transform);
        if (root == null) return null;
        root.name = "Decor " + placement.itemId + " (T13)";
        root.AddComponent<OfficeDecorPiece>().ItemId = placement.itemId;
        root.transform.SetPositionAndRotation(position, rotation);
        var piece = new Piece
        {
            ItemId = placement.itemId,
            AnchorId = anchor.id,
            Root = root,
            Surface = surface,
            Rest = position,
            Bounds = DecorModels.WorldBounds(root),
            Waiting = !placement.arrived && !arrivedThisVisit.Contains(placement.itemId)
        };
        SetVisible(piece, !piece.Waiting);
        return piece;
    }

    private void Warn(string key, string problem)
    {
        if (warned.Add(key)) Debug.LogWarning("[OfficeDecor] " + key + " " + problem + ".");
    }

    /// <summary>T18: an owned piece this office cannot show: the player hears once per visit that it is still theirs.</summary>
    private void Unplaced(string itemId)
    {
        if (!string.IsNullOrEmpty(itemId) && !told.Contains(itemId) && !untold.Contains(itemId)) untold.Add(itemId);
    }

    /// <summary>The notice waits until the bar can show it (never during loading).</summary>
    private void TellUnplaced()
    {
        HudHotbar hotbar = HudHotbar.Instance;
        if (untold.Count == 0 || hotbar == null || !hotbar.IsReady || !HudHotbar.CanOpenPanels()) return;
        string itemId = untold[0];
        untold.RemoveAt(0);
        told.Add(itemId);
        ShopProduct product = ShopCatalogueAsset.Current.products?.Find(p => p != null && (p.id == itemId || p.itemId == itemId));
        string name = product != null && !string.IsNullOrWhiteSpace(product.title) ? product.title : "decoration";
        hotbar.Notice($"Your {name} is still yours. This office has no spot to display it.");
    }

    // =========================================================
    // ANCHORS (deterministic)
    // =========================================================

    private Transform Target(ShopDecorAnchor anchor)
    {
        Transform parent = PlayerPlacement.Find(scene, anchor.parent);
        if (parent == null) return null;
        foreach (Transform child in parent) if (child.name == anchor.target) return child;
        return null;
    }

    /// <summary>The desk corner: the free spot on the seat's side nearest the far corner (DecorRules.CornerSpot).</summary>
    private bool ResolveSurface(ShopDecorAnchor anchor, out Vector3 position, out Quaternion rotation, out Transform surface, out string problem)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        problem = "";
        surface = Target(anchor);
        if (surface == null) { problem = $"no '{anchor.target}' under {anchor.parent}"; return false; }
        if (!WorldCollectibles.LocalTop(surface, out Bounds local)) { problem = "the surface has no mesh"; return false; }
        float top = local.max.y;
        Matrix4x4 toLocal = surface.worldToLocalMatrix;
        Bounds worldTop = WorldCollectibles.TransformBounds(surface.localToWorldMatrix, local);
        Vector3 scale = surface.lossyScale;
        float unit = 1f / Mathf.Max(0.0001f, Mathf.Abs(scale.x));

        // Everything standing on the top counts (hidden props too, so the answer never changes); runtime props are added explicitly below.
        var obstacles = new List<PickupRules.Box>();
        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            if (renderer == null || renderer.gameObject.scene != scene || renderer is ParticleSystemRenderer) continue;
            if (renderer.GetComponentInParent<OfficeDecorPiece>() != null || renderer.GetComponentInParent<KeepsakeStand>() != null) continue;
            Bounds b = renderer.bounds;
            if (b.size.sqrMagnitude < 1e-8f) continue;
            if (b.max.y <= worldTop.max.y + 0.005f || b.min.y > worldTop.max.y + 0.3f) continue;
            if (b.max.x < worldTop.min.x || b.min.x > worldTop.max.x || b.max.z < worldTop.min.z || b.min.z > worldTop.max.z) continue;
            Bounds inLocal = WorldCollectibles.TransformBounds(toLocal, b);
            obstacles.Add(new PickupRules.Box(inLocal.min.x, inLocal.min.z, inLocal.max.x, inLocal.max.z));
        }
        // The keepsake stand keeps its own spot (it may be built before or after this piece).
        PickupCatalogue pickups = PickupCatalogueAsset.Current;
        foreach (DisplayDefinition display in pickups != null ? pickups.displays : new List<DisplayDefinition>())
        {
            if (display == null || !WorldCollectibles.ResolveSpot(scene, display.spot, KeepsakeStand.Footprint, out Vector3 standAt, out _, out Transform standOn, out _)) continue;
            if (standOn != surface) continue;
            Vector3 c = toLocal.MultiplyPoint3x4(standAt);
            float half = Mathf.Max(KeepsakeStand.Footprint.x, KeepsakeStand.Footprint.y) * 0.5f * unit + 0.02f * unit;
            obstacles.Add(new PickupRules.Box(c.x - half, c.z - half, c.x + half, c.z + half));
        }
        // Decor already on this surface (built earlier in save order).
        foreach (Piece other in pieces)
        {
            if (other.Surface != surface) continue;
            Bounds inLocal = WorldCollectibles.TransformBounds(toLocal, other.Bounds);
            obstacles.Add(new PickupRules.Box(inLocal.min.x, inLocal.min.z, inLocal.max.x, inLocal.max.z));
        }

        Transform seat = surface.Find("DeskViewpoint");
        Vector3 forward = seat != null ? surface.InverseTransformDirection(seat.forward) : Vector3.forward;
        float side = anchor.side == "left" ? -1f : 1f;
        var box = new PickupRules.Box(local.min.x, local.min.z, local.max.x, local.max.z);
        float halfX = DecorModels.PencilCupFootprint.x * 0.5f * unit, halfZ = DecorModels.PencilCupFootprint.y * 0.5f * unit;
        if (!DecorRules.CornerSpot(box, halfX, halfZ, obstacles, DeskMargin * unit, DeskGap * unit, forward.x, forward.z, side, out float x, out float z))
        {
            problem = "no free room on that side of the surface";
            return false;
        }
        position = surface.TransformPoint(new Vector3(x, top, z));
        Vector3 facing = seat != null ? seat.position - position : -surface.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.0001f) facing = -surface.forward;
        rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
        return true;
    }

    /// <summary>The wall behind the shelf: above its top (below the ceiling), facing into the room, shifted along the wall past wall props.</summary>
    private bool ResolveWall(ShopDecorAnchor anchor, out Vector3 position, out Quaternion rotation, out string problem)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        problem = "";
        Transform shelf = Target(anchor);
        if (shelf == null) { problem = $"no '{anchor.target}' under {anchor.parent}"; return false; }
        Renderer[] shelfRenderers = shelf.GetComponentsInChildren<Renderer>(true);
        if (shelfRenderers.Length == 0) { problem = "the shelf has no mesh"; return false; }
        Bounds shelfBounds = shelfRenderers[0].bounds;
        foreach (Renderer renderer in shelfRenderers) shelfBounds.Encapsulate(renderer.bounds);

        float halfHeight = DecorModels.PrintSize.y * 0.5f;
        var probe = new Vector3(shelfBounds.center.x, shelfBounds.max.y + anchor.above + halfHeight, shelfBounds.center.z);
        if (Physics.Raycast(new Vector3(probe.x, shelfBounds.max.y + 0.02f, probe.z), Vector3.up, out RaycastHit ceiling, 3f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            probe.y = Mathf.Min(probe.y, ceiling.point.y - 0.08f - halfHeight);
        probe.y = Mathf.Max(probe.y, shelfBounds.max.y + 0.02f + halfHeight);

        // The wall: the nearest vertical surface around the shelf's four sides.
        Vector3[] directions = { -shelf.forward, shelf.forward, shelf.right, -shelf.right };
        bool found = false;
        RaycastHit wall = default;
        foreach (Vector3 raw in directions)
        {
            Vector3 direction = new Vector3(raw.x, 0f, raw.z).normalized;
            if (direction.sqrMagnitude < 0.5f) continue;
            if (!Physics.Raycast(probe, direction, out RaycastHit hit, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            if (Mathf.Abs(hit.normal.y) > 0.3f || hit.transform.IsChildOf(shelf) || hit.collider is CharacterController) continue;
            if (hit.collider.GetComponentInParent<OfficeDecorPiece>() != null) continue;
            if (!found || hit.distance < wall.distance) { wall = hit; found = true; }
        }
        Vector3 normal, at;
        if (found)
        {
            normal = new Vector3(wall.normal.x, 0f, wall.normal.z).normalized;
            at = wall.point + normal * 0.002f;
        }
        else
        {
            // No wall collider: hang it level with the shelf's back edge instead (said in the Console).
            Vector3 back = new Vector3(-shelf.forward.x, 0f, -shelf.forward.z).normalized;
            normal = -back;
            float reach = Mathf.Abs(Vector3.Dot(shelfBounds.extents, new Vector3(Mathf.Abs(back.x), 0f, Mathf.Abs(back.z))));
            at = probe + back * reach;
            Warn(anchor.id, "found no wall behind the shelf; the print hangs at the shelf's back edge");
        }
        rotation = Quaternion.LookRotation(normal, Vector3.up);

        // Wall props (a clock, a switch) win: slide along the wall to the first clear place.
        Vector3 along = Vector3.Cross(Vector3.up, normal);
        Vector3 chosen = at;
        foreach (float shift in WallShifts)
        {
            Vector3 candidate = at + along * shift;
            if (!WallBlocked(candidate, normal, along, shelf, found ? wall.collider : null)) { chosen = candidate; break; }
        }
        position = chosen;
        return true;
    }

    private bool WallBlocked(Vector3 back, Vector3 normal, Vector3 along, Transform shelf, Collider wall)
    {
        Vector3 centre = back + normal * 0.03f;
        Vector3 extents = along * (DecorModels.PrintSize.x * 0.5f + 0.04f);
        var frame = new Bounds(centre, new Vector3(Mathf.Abs(extents.x) * 2f + 0.06f, DecorModels.PrintSize.y + 0.08f, Mathf.Abs(extents.z) * 2f + 0.06f));
        foreach (Renderer renderer in FindObjectsByType<Renderer>())
        {
            if (renderer == null || renderer.gameObject.scene != scene || !renderer.enabled || renderer is ParticleSystemRenderer) continue;
            if (renderer.transform.IsChildOf(shelf) || renderer.GetComponentInParent<OfficeDecorPiece>() != null) continue;
            if (wall != null && renderer.transform == wall.transform) continue;
            Bounds b = renderer.bounds;
            // Walls, floors and ceilings are architecture, not props.
            int large = (b.size.x > 1.2f ? 1 : 0) + (b.size.y > 1.2f ? 1 : 0) + (b.size.z > 1.2f ? 1 : 0);
            if (large >= 2) continue;
            if (b.Intersects(frame)) return true;
        }
        return false;
    }

    // =========================================================
    // ARRIVAL
    // =========================================================

    private void Update()
    {
        if (dirty)
        {
            dirty = false;
            Refresh();
        }
        if (untold.Count > 0) TellUnplaced();
        Camera view = null;
        Plane[] planes = null;
        float now = Time.unscaledTime;
        foreach (Piece piece in pieces)
        {
            if (piece.Root == null) continue;
            if (piece.Waiting)
            {
                if (view == null)
                {
                    view = Camera.main;
                    if (view == null) continue;
                    planes = GeometryUtility.CalculateFrustumPlanes(view);
                }
                if (Seen(view, planes, piece)) BeginArrival(piece, now);
            }
            if (piece.ArriveAt >= 0f) Animate(piece, now);
        }
    }

    private static bool Seen(Camera view, Plane[] planes, Piece piece)
    {
        Vector3 eye = view.transform.position;
        Bounds b = piece.Bounds;
        if ((b.center - eye).sqrMagnitude > ArrivalRange * ArrivalRange) return false;
        if (!GeometryUtility.TestPlanesAABB(planes, b)) return false;
        // Line of sight to its upper half: walls and closed doors hide it.
        Vector3 target = b.center + Vector3.up * b.extents.y * 0.5f;
        float distance = Vector3.Distance(eye, target);
        return !Physics.Raycast(eye, (target - eye) / Mathf.Max(0.001f, distance), out RaycastHit hit, distance - 0.12f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || hit.collider is CharacterController;
    }

    private void BeginArrival(Piece piece, float now)
    {
        piece.Waiting = false;
        piece.ArriveAt = now;
        SetVisible(piece, true);
        Animate(piece, now);
    }

    /// <summary>A short lift-and-settle: in from just above, a small overshoot, then rest; one soft placement sound as it lands.</summary>
    private void Animate(Piece piece, float now)
    {
        float t = Mathf.Clamp01((now - piece.ArriveAt) / ArrivalSeconds);
        float settle = HudTheme.EaseOutBack(t);
        piece.Root.transform.position = piece.Rest + Vector3.up * (1f - settle) * 0.12f;
        piece.Root.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, settle);
        if (t < 1f) return;
        piece.ArriveAt = -1f;
        piece.Root.transform.position = piece.Rest;
        piece.Root.transform.localScale = Vector3.one;
        arrivedThisVisit.Add(piece.ItemId);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_place_02", 0.8f);
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        if (manager != null) manager.MarkDecorArrived(piece.ItemId);
    }

    private static void SetVisible(Piece piece, bool visible)
    {
        foreach (Renderer renderer in piece.Root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = visible;
        foreach (Canvas canvas in piece.Root.GetComponentsInChildren<Canvas>(true)) canvas.enabled = visible;
    }
}
