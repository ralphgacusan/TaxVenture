using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>World pickups and display anchors from Resources/Campaign/pickups.json, or PickupCatalogue.BuiltIn() when it is missing or broken.</summary>
public static class PickupCatalogueAsset
{
    public const string ResourcePath = "Campaign/pickups";
    private static PickupCatalogue cached;

    public static PickupCatalogue Current => cached ?? (cached = Load());

    private static PickupCatalogue Load()
    {
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogWarning("[Collectibles] Resources/" + ResourcePath + ".json not found; using the built-in pickups.");
            return PickupCatalogue.BuiltIn();
        }
        try
        {
            PickupCatalogue parsed = JsonUtility.FromJson<PickupCatalogue>(asset.text);
            List<string> problems = parsed != null ? parsed.Problems(InventoryCatalogueAsset.Current) : new List<string> { "empty file" };
            if (parsed != null && problems.Count == 0) return parsed;
            Debug.LogWarning("[Collectibles] pickups.json has problems; using the built-in pickups: " + string.Join("; ", problems));
        }
        catch (Exception error)
        {
            Debug.LogWarning("[Collectibles] pickups.json could not be read; using the built-in pickups. " + error.Message);
        }
        return PickupCatalogue.BuiltIn();
    }
}

/// <summary>
/// The printed faces of the T11 objects, built from UI pieces so the same
/// face appears on the world object (a world-space canvas) and large on the
/// inspect card: the postcard front (coastal blue border, sea, sand, sun and
/// a palm, "COAST TALES") and back (the short note), and the folded green
/// voucher (T14: "REVIEW VOUCHER", a large "+180 REVIEW SECONDS", "ONE PER
/// CASE", a copper clock seal on the stub and a carry tab). Sizes are in
/// canvas units of a 600 x 400 face.
/// </summary>
public static class WorldCardFace
{
    public static readonly Vector2 Size = new Vector2(600f, 400f);
    private static readonly Color Ink = new Color32(46, 36, 24, 255);

    public static void Build(RectTransform root, PickupDefinition pickup, bool back)
    {
        if (pickup != null && pickup.itemId == InventoryCatalogue.ReviewVoucher) BuildVoucher(root, pickup);
        else if (back) BuildPostcardBack(root, pickup);
        else BuildPostcardFront(root, pickup);
    }

    private static RectTransform Box(RectTransform parent, string name, float x, float y, float w, float h, Color color, float radius = 0f)
    {
        RectTransform rect = HudTheme.PlaceTopLeft(HudTheme.CreateRect(name, parent), x, y, w, h);
        HudTheme.AddImage(rect, radius > 0f ? HudTheme.Rounded : null, color, radius);
        return rect;
    }

    private static TextMeshProUGUI Text(RectTransform parent, string name, string text, float x, float y, float w, float h, float size, Color color,
        TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
    {
        TextMeshProUGUI label = HudTheme.CreateLabel(name, parent, text, size, color, align, style);
        HudTheme.PlaceTopLeft(label.rectTransform, x, y, w, h);
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private static void BuildPostcardFront(RectTransform root, PickupDefinition pickup)
    {
        Box(root, "Border", 0f, 0f, Size.x, Size.y, new Color32(58, 132, 196, 255), 18f);
        Box(root, "Sky", 24f, 24f, 552f, 190f, new Color32(150, 208, 242, 255));
        Box(root, "Sea", 24f, 214f, 552f, 70f, new Color32(38, 124, 186, 255));
        Box(root, "Sand", 24f, 284f, 552f, 92f, new Color32(236, 210, 150, 255));
        RectTransform sun = Box(root, "Sun", 430f, 50f, 90f, 90f, new Color32(255, 214, 90, 255));
        sun.GetComponent<Image>().sprite = HudTheme.Circle;
        RectTransform trunk = Box(root, "Palm Trunk", 150f, 150f, 18f, 170f, new Color32(120, 84, 48, 255));
        trunk.localRotation = Quaternion.Euler(0f, 0f, -8f);
        for (int i = 0; i < 4; i++)
        {
            RectTransform leaf = Box(root, "Leaf " + i, 110f + i * 22f, 128f + (i % 2) * 10f, 90f, 22f, new Color32(46, 140, 82, 255), 10f);
            leaf.localRotation = Quaternion.Euler(0f, 0f, -30f + i * 22f);
        }
        Text(root, "Title", pickup != null && pickup.title.Length > 0 ? pickup.title : "COAST TALES", 40f, 300f, 520f, 70f, 46f,
            new Color32(255, 250, 236, 255), TextAlignmentOptions.Right, FontStyles.Bold).outlineWidth = 0.2f;
    }

    private static void BuildPostcardBack(RectTransform root, PickupDefinition pickup)
    {
        Box(root, "Card", 0f, 0f, Size.x, Size.y, new Color32(246, 240, 224, 255), 18f);
        Box(root, "Divider", 380f, 40f, 4f, 320f, new Color32(190, 176, 150, 255));
        RectTransform stamp = Box(root, "Stamp", 470f, 34f, 96f, 110f, new Color32(58, 132, 196, 255), 6f);
        Text(stamp, "Stamp Text", "ISLAND\nPOST", 0f, 0f, 96f, 110f, 22f, new Color32(255, 250, 236, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        string note = pickup != null && pickup.lines != null ? string.Join("\n\n", pickup.lines) : "";
        TextMeshProUGUI body = Text(root, "Note", note, 30f, 36f, 330f, 330f, 26f, Ink, TextAlignmentOptions.TopLeft);
        body.font = HudTheme.HandwritingFont;
        body.enableAutoSizing = true;
        body.fontSizeMin = 14f;
        for (int i = 0; i < 3; i++) Box(root, "Address Line " + i, 404f, 220f + i * 44f, 170f, 3f, new Color32(190, 176, 150, 255));
    }

    private static void BuildVoucher(RectTransform root, PickupDefinition pickup) =>
        BuildVoucherFace(root, pickup != null && pickup.title.Length > 0 ? pickup.title : "REVIEW VOUCHER", -1, 2);

    /// <summary>
    /// The folded green ticket (T14): title, "+180 REVIEW SECONDS", "ONE PER
    /// CASE", a copper clock seal on the perforated stub and a carry tab
    /// ("1/2" when carried is known, otherwise "MAX 2").
    /// </summary>
    public static void BuildVoucherFace(RectTransform root, string title, int carried, int limit)
    {
        var paper = new Color32(250, 252, 244, 255);
        var copper = new Color32(190, 110, 58, 255);
        Box(root, "Ticket", 0f, 0f, Size.x, Size.y, new Color32(52, 150, 92, 255), 22f);
        Box(root, "Fold", 0f, 196f, Size.x, 8f, new Color32(36, 112, 66, 255));
        for (int i = 0; i < 14; i++) Box(root, "Perforation " + i, 420f, 20f + i * 27f, 6f, 14f, new Color32(214, 238, 220, 255));
        Text(root, "Title", title, 30f, 22f, 380f, 56f, 42f, paper, TextAlignmentOptions.Left, FontStyles.Bold).characterSpacing = 2f;
        Text(root, "Benefit", "+180", 26f, 70f, 390f, 140f, 124f, new Color32(255, 246, 196, 255), TextAlignmentOptions.Left, FontStyles.Bold);
        Text(root, "Unit", "REVIEW SECONDS", 30f, 212f, 380f, 48f, 36f, paper, TextAlignmentOptions.Left, FontStyles.Bold).characterSpacing = 2f;
        Box(root, "Band", 26f, 286f, 300f, 64f, new Color32(240, 232, 204, 255), 12f);
        Text(root, "One Per Case", "ONE PER CASE", 26f, 286f, 300f, 64f, 30f, new Color32(36, 112, 66, 255), TextAlignmentOptions.Center, FontStyles.Bold).characterSpacing = 3f;

        // The copper clock seal on the stub.
        RectTransform seal = Box(root, "Clock Seal", 448f, 30f, 128f, 128f, copper);
        seal.GetComponent<Image>().sprite = HudTheme.Circle;
        RectTransform dial = Box(seal, "Dial", 14f, 14f, 100f, 100f, new Color32(242, 222, 182, 255));
        dial.GetComponent<Image>().sprite = HudTheme.Circle;
        for (int i = 0; i < 12; i++)
        {
            RectTransform tick = HudTheme.CreateRect("Tick " + i, dial);
            tick.anchorMin = tick.anchorMax = new Vector2(0.5f, 0.5f);
            tick.sizeDelta = new Vector2(i % 3 == 0 ? 6f : 3f, i % 3 == 0 ? 14f : 9f);
            tick.localRotation = Quaternion.Euler(0f, 0f, -i * 30f);
            tick.anchoredPosition = (Vector2)(tick.localRotation * new Vector3(0f, 40f, 0f));
            HudTheme.AddImage(tick, null, new Color32(120, 66, 34, 255));
        }
        foreach ((string name, float angle, float length) hand in new[] { ("Hour Hand", -60f, 26f), ("Minute Hand", 0f, 38f) })
        {
            RectTransform h = HudTheme.CreateRect(hand.name, dial);
            h.anchorMin = h.anchorMax = new Vector2(0.5f, 0.5f);
            h.pivot = new Vector2(0.5f, 0f);
            h.sizeDelta = new Vector2(6f, hand.length);
            h.localRotation = Quaternion.Euler(0f, 0f, hand.angle);
            HudTheme.AddImage(h, null, new Color32(110, 56, 26, 255));
        }

        // The carry tab: how many you carry of the two allowed.
        Box(root, "Carry Tab", 448f, 270f, 128f, 92f, new Color32(240, 232, 204, 255), 14f);
        Text(root, "Carry Label", "CARRY", 448f, 276f, 128f, 30f, 22f, new Color32(36, 112, 66, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        Text(root, "Carry Count", carried >= 0 ? carried + "/" + limit : "MAX " + limit, 448f, 304f, 128f, 52f, carried >= 0 ? 40f : 32f,
            new Color32(36, 112, 66, 255), TextAlignmentOptions.Center, FontStyles.Bold);
    }
}

/// <summary>
/// PURPOSE:
/// T11 world pickups and keepsake display for the Level 1 office (created by
/// HudHotbar). Each optional pickup in pickups.json that is not yet claimed
/// in this campaign appears on its own small tray once its introduction has
/// happened (the voucher after the receptionist hands over the case). A tap
/// within reach lifts it and opens its printed inspect card; COLLECT commits
/// the item and its claim in one save (HudHotbar.CollectPickup), then it
/// flies to its slot or the Bag; LEAVE IT puts it back. A failed save leaves
/// it in place for another try. Claimed pickups never appear again, and
/// restoring a level raises nothing. The desk keepsake stand shows a
/// displayed keepsake; tapping it displays or takes back the postcard.
///
/// Placement is deterministic: each tray rests on the named surface's top at
/// the spot with the most room (PickupRules.BestSpot), facing outward.
/// No coins, trust, XP or answers come from any of this.
/// </summary>
[DefaultExecutionOrder(160)]
public sealed class WorldCollectibles : MonoBehaviour
{
    private const float CheckEvery = 0.5f;

    public static WorldCollectibles Instance { get; private set; }

    private HudHotbar hotbar;
    private PickupCatalogue catalogue;
    private CollectibleInspectCard card;
    private readonly Dictionary<string, WorldCollectible> spawned = new Dictionary<string, WorldCollectible>(StringComparer.Ordinal);
    private readonly List<KeepsakeStand> stands = new List<KeepsakeStand>();
    private readonly HashSet<string> unplaceable = new HashSet<string>(StringComparer.Ordinal);
    private bool standsBuilt;
    private float nextCheck;
    private WorldCollectible inspecting;

    public PickupCatalogue Catalogue => catalogue;
    public HudHotbar Hotbar => hotbar;

    public static WorldCollectibles Create(GameObject host, Transform canvasRoot, HudHotbar hotbar)
    {
        WorldCollectibles director = host.AddComponent<WorldCollectibles>();
        director.hotbar = hotbar;
        director.catalogue = PickupCatalogueAsset.Current;
        director.card = CollectibleInspectCard.Create(canvasRoot, director);
        Instance = director;
        return director;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        foreach (WorldCollectible pickup in spawned.Values) if (pickup != null) Destroy(pickup.gameObject);
        foreach (KeepsakeStand stand in stands) if (stand != null) Destroy(stand.gameObject);
        if (card != null) Destroy(card.gameObject);
    }

    private string LevelId => OfficeLevelProfile.LevelIdFor(this);

    private void Update()
    {
        if (hotbar == null || !hotbar.IsReady || Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + CheckEvery;
        if (!standsBuilt) BuildStands();
        RefreshPickups();
        foreach (KeepsakeStand stand in stands) if (stand != null) stand.Refresh();
    }

    /// <summary>Spawns available, unclaimed pickups of this level and removes any claimed meanwhile.</summary>
    private void RefreshPickups()
    {
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        if (manager == null) return;
        ICollection<string> reached = OfficeObjectives.Instance != null ? (ICollection<string>)OfficeObjectives.Instance.ReachedKeys() : new List<string>();
        foreach (PickupDefinition pickup in catalogue.pickups)
        {
            if (pickup == null || pickup.levelId != LevelId || unplaceable.Contains(pickup.id)) continue;
            bool claimed = manager.IsPickupClaimed(manager.PickupClaimId(pickup.levelId, pickup.id));
            spawned.TryGetValue(pickup.id, out WorldCollectible existing);
            if (claimed)
            {
                if (existing != null && !existing.Busy) { Destroy(existing.gameObject); spawned.Remove(pickup.id); }
                continue;
            }
            if (existing != null || !PickupRules.IsAvailable(pickup, false, reached)) continue;
            WorldCollectible built = WorldCollectible.Create(gameObject.scene, pickup, this, out string problem);
            if (built == null)
            {
                unplaceable.Add(pickup.id);
                Debug.LogWarning($"[Collectibles] '{pickup.id}' has no place to rest ({problem}); it is not shown.");
                continue;
            }
            spawned[pickup.id] = built;
        }
    }

    private void BuildStands()
    {
        standsBuilt = true;
        foreach (DisplayDefinition display in catalogue.displays)
        {
            if (display == null || display.levelId != LevelId) continue;
            KeepsakeStand stand = KeepsakeStand.Create(gameObject.scene, display, this, out string problem);
            if (stand == null) Debug.LogWarning($"[Collectibles] Display '{display.id}' has no place ({problem}).");
            else stands.Add(stand);
        }
    }

    /// <summary>The pickup definition whose item this is (for faces and text), or null.</summary>
    public PickupDefinition DefinitionFor(string itemId) => catalogue.pickups.Find(p => p != null && p.itemId == itemId);

    // =========================================================
    // PICKUP FLOW
    // =========================================================

    internal void Tapped(WorldCollectible pickup)
    {
        if (inspecting != null || card == null || !HudHotbar.CanOpenPanels() || InventoryScreen.AnyOpen) return;
        inspecting = pickup;
        pickup.Lift(true);
        int over = hotbar.OverCarryLimit(pickup.Definition);
        string notice = over > 0 ? "You already carry two vouchers. Use one, then come back for this one." : "";
        card.Show(pickup.Definition, CollectibleInspectCard.Mode.Pickup, notice, () => Collect(pickup), () => Leave(pickup));
    }

    private void Collect(WorldCollectible pickup)
    {
        InventoryResult result = hotbar.CollectPickup(pickup.Definition);
        if (result.Outcome == InventoryOutcome.SaveFailed)
        {
            card.SetMessage("That could not be saved just now. Try again.");
            return;
        }
        if (result.Outcome == InventoryOutcome.Rejected)
        {
            card.SetMessage(string.IsNullOrEmpty(result.Message) ? "That can't be collected right now." : result.Message + ".");
            return;
        }
        card.Hide();
        inspecting = null;
        if (result.Outcome == InventoryOutcome.AlreadyApplied)
        {
            // Claimed elsewhere already: nothing is minted, the object just goes.
            pickup.FlyTo(null, keepsake: false);
            return;
        }
        RectTransform target = result.Location.Area == InventoryArea.Hotbar ? hotbar.ItemTarget(pickup.Definition.itemId) : hotbar.BagIcon;
        bool keepsake = hotbar.Catalogue.Find(pickup.Definition.itemId)?.category == InventoryCatalogue.Keepsakes;
        pickup.FlyTo(target, keepsake);
    }

    private void Leave(WorldCollectible pickup)
    {
        card.Hide();
        inspecting = null;
        pickup.Lift(false);
    }

    internal void Forget(WorldCollectible pickup)
    {
        foreach (KeyValuePair<string, WorldCollectible> entry in spawned)
            if (entry.Value == pickup) { spawned.Remove(entry.Key); break; }
    }

    // =========================================================
    // READING AND DISPLAY
    // =========================================================

    /// <summary>Reads a carried postcard or voucher on its printed card (from the Bag's READ). False without a printed face.</summary>
    public bool ShowRead(string itemId)
    {
        PickupDefinition pickup = DefinitionFor(itemId);
        if (pickup == null || card == null || !HudHotbar.CanOpenPanels()) return false;
        card.Show(pickup, CollectibleInspectCard.Mode.Read, "", card.Hide, card.Hide);
        return true;
    }

    internal void StandTapped(KeepsakeStand stand)
    {
        if (card == null || !HudHotbar.CanOpenPanels() || InventoryScreen.AnyOpen) return;
        CampaignInventoryRecord r = hotbar.Inventory;
        string shown = InventoryRules.DisplayedAt(r, stand.Definition.id);
        if (shown.Length > 0)
        {
            card.Show(DefinitionFor(shown), CollectibleInspectCard.Mode.Displayed, "", () =>
            {
                InventoryResult back = hotbar.TakeDownKeepsake(stand.Definition.id);
                if (!back.Changed) { card.SetMessage("That could not be saved just now. Try again."); return; }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_pickup_01");
                card.Hide();
                stand.Refresh();
            }, card.Hide);
            return;
        }
        string keepsake = OwnedKeepsake(r, stand.Definition.category);
        if (string.IsNullOrEmpty(keepsake))
        {
            hotbar.Notice("A spot for a keepsake. Anything you find on the island can stand here.");
            return;
        }
        card.Show(DefinitionFor(keepsake), CollectibleInspectCard.Mode.Display, "", () =>
        {
            InventoryResult put = hotbar.DisplayKeepsake(keepsake, stand.Definition.id);
            if (!put.Changed) { card.SetMessage(put.Outcome == InventoryOutcome.Rejected ? put.Message + "." : "That could not be saved just now. Try again."); return; }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_place_01");
            card.Hide();
            stand.Refresh();
        }, card.Hide);
    }

    /// <summary>An owned keepsake of the category that is not on display yet (a printed one first).</summary>
    private string OwnedKeepsake(CampaignInventoryRecord r, string category)
    {
        foreach (InventoryItemDefinition item in hotbar.Catalogue.items)
            if (item != null && item.category == category && InventoryRules.Owns(r, item.id) && !InventoryRules.IsDisplayed(r, item.id) &&
                DefinitionFor(item.id) != null)
                return item.id;
        return null;
    }

    // =========================================================
    // PLACEMENT (deterministic)
    // =========================================================

    /// <summary>
    /// Finds the spot's surface and the resting point on its top with the
    /// most room (footprint in metres). The rotation faces outward from the
    /// surface centre, or toward a seated viewpoint when the surface has one.
    /// </summary>
    public static bool ResolveSpot(Scene scene, PickupSpot spot, Vector2 footprint, out Vector3 position, out Quaternion rotation,
        out Transform surface, out string problem)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        surface = null;
        problem = "";
        Transform parent = spot != null ? PlayerPlacement.Find(scene, spot.parent) : null;
        if (parent == null) { problem = "parent not found"; return false; }
        var candidates = new List<Transform>();
        foreach (Transform child in parent) if (child.name == spot.surface) candidates.Add(child);
        if (candidates.Count == 0) { problem = $"no '{spot.surface}' under {spot.parent}"; return false; }
        surface = candidates[0];
        if (candidates.Count > 1 && !string.IsNullOrEmpty(spot.near))
        {
            Transform near = null;
            foreach (Transform child in parent) if (child.name == spot.near) { near = child; break; }
            if (near != null)
            {
                float best = float.MaxValue;
                foreach (Transform candidate in candidates)
                {
                    float d = (candidate.position - near.position).sqrMagnitude;
                    if (d < best) { best = d; surface = candidate; }
                }
            }
        }

        if (!LocalTop(surface, out Bounds local)) { problem = "the surface has no mesh"; return false; }
        float top = local.max.y;
        var obstacles = new List<PickupRules.Box>();
        Matrix4x4 toLocal = surface.worldToLocalMatrix;
        Bounds worldTop = TransformBounds(surface.localToWorldMatrix, local);
        // Hidden props count too (a stored folder's desk stack), so the spot is the same on every load.
        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            if (renderer == null || renderer.gameObject.scene != scene || renderer is ParticleSystemRenderer) continue;
            if (renderer.gameObject.layer == LayerMask.NameToLayer("UI")) continue;
            // T13 decor is placed after (and around) the stand; it never moves the stand.
            if (renderer.GetComponentInParent<OfficeDecorPiece>() != null) continue;
            Bounds b = renderer.bounds;
            if (b.size.sqrMagnitude < 1e-8f) continue;
            if (b.max.y <= worldTop.max.y + 0.005f || b.min.y > worldTop.max.y + 0.6f) continue;
            if (b.max.x < worldTop.min.x || b.min.x > worldTop.max.x || b.max.z < worldTop.min.z || b.min.z > worldTop.max.z) continue;
            Bounds inLocal = TransformBounds(toLocal, b);
            obstacles.Add(new PickupRules.Box(inLocal.min.x, inLocal.min.z, inLocal.max.x, inLocal.max.z));
        }
        Vector3 scale = surface.lossyScale;
        float halfX = footprint.x * 0.5f / Mathf.Max(0.0001f, Mathf.Abs(scale.x));
        float halfZ = footprint.y * 0.5f / Mathf.Max(0.0001f, Mathf.Abs(scale.z));
        var box = new PickupRules.Box(local.min.x, local.min.z, local.max.x, local.max.z);
        if (!PickupRules.BestSpot(box, halfX, halfZ, obstacles, 0.02f / Mathf.Max(0.0001f, Mathf.Abs(scale.x)), out float x, out float z))
        {
            problem = "no free room on the surface";
            return false;
        }
        position = surface.TransformPoint(new Vector3(x, top, z));
        Vector3 centre = surface.TransformPoint(new Vector3(local.center.x, top, local.center.z));
        Transform viewpoint = surface.Find("DeskViewpoint");
        Vector3 facing = viewpoint != null ? viewpoint.position - position : position - centre;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.0001f) facing = -surface.forward;
        rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
        return true;
    }

    /// <summary>
    /// The surface's own mesh bounds in its local space. The office desk keeps
    /// its body under "Visual" (its document stacks are children too), so only
    /// that counts when present; a plain prop (a coffee table) uses all its meshes.
    /// </summary>
    public static bool LocalTop(Transform surface, out Bounds local)
    {
        local = new Bounds();
        bool any = false;
        Transform body = surface.Find("Visual");
        if (body == null || body.GetComponentInChildren<MeshFilter>(true) == null) body = surface;
        foreach (MeshFilter filter in body.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 toSurface = surface.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            Bounds inSurface = TransformBounds(toSurface, mesh);
            if (!any) { local = inSurface; any = true; }
            else local.Encapsulate(inSurface);
        }
        return any;
    }

    public static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
    {
        var result = new Bounds(matrix.MultiplyPoint3x4(bounds.center), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
            result.Encapsulate(matrix.MultiplyPoint3x4(corner));
        }
        return result;
    }

    /// <summary>A material that renders in this pipeline: the surface's own (never a primitive's default).</summary>
    public static Material SurfaceMaterial(Transform surface)
    {
        Renderer renderer = surface != null ? surface.GetComponentInChildren<Renderer>() : null;
        return renderer != null ? renderer.sharedMaterial : null;
    }

    /// <summary>A plain block for trays and stands, using the surface material, no collider, no shadow.</summary>
    public static GameObject Block(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material, Quaternion? localRotation = null)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation ?? Quaternion.identity;
        part.transform.localScale = size;
        Renderer renderer = part.GetComponent<Renderer>();
        if (material != null) renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return part;
    }

    /// <summary>A small world-space canvas (no raycasts) sized width metres wide for a face of `size` canvas units.</summary>
    public static RectTransform WorldFace(Transform parent, string name, Vector2 size, float widthMetres)
    {
        var host = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)host.transform;
        rect.SetParent(parent, false);
        var canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one * (widthMetres / size.x);
        return rect;
    }

    /// <summary>A plaque label (world-space text) on a tray.</summary>
    public static void Plaque(Transform parent, string text, Vector3 localPosition, float widthMetres)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        RectTransform face = WorldFace(parent, "Plaque", new Vector2(600f, 120f), widthMetres);
        face.localPosition = localPosition;
        face.localRotation = Quaternion.Euler(30f, 180f, 0f);
        HudTheme.AddImage(face, HudTheme.Rounded, new Color32(236, 206, 140, 255), 16f);
        TextMeshProUGUI label = HudTheme.CreateLabel("Text", face, text, 54f, new Color32(70, 52, 24, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(label.rectTransform, 8f);
    }
}

/// <summary>
/// One optional pickup resting on its own small tray (T11): the printed card
/// stands in the tray, tilted back, with a plaque in front. A tap within
/// reach (no dialogue, no panel) lifts it toward the camera and opens the
/// inspect card; after a committed collect it flies to its slot or the Bag
/// (WorldPickupMotion) with one paper cue, one card cue on landing and, for
/// a keepsake, one soft sparkle. Before a successful commit nothing moves away.
/// </summary>
public sealed class WorldCollectible : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private const float LiftSeconds = 0.25f;
    private const float FlightSeconds = 0.45f;
    private static readonly Vector2 Footprint = new Vector2(0.24f, 0.18f);

    private WorldCollectibles owner;
    private Transform cardPivot;
    private Vector3 restLocal;
    private Quaternion restLocalRotation;
    private float liftTarget, lift;
    private bool flying;
    private float flightElapsed;
    private RectTransform flightTarget;
    private Camera flightCamera;
    private Vector2 flightFrom;
    private float flightFromDepth, flightToDepth;
    private Quaternion flightFromRotation;
    private Vector3 flightFromScale;
    private bool flightKeepsake;
    private HighlightEffect highlight;

    public PickupDefinition Definition { get; private set; }
    public bool Busy => flying || lift > 0.01f;

    public static WorldCollectible Create(Scene scene, PickupDefinition pickup, WorldCollectibles owner, out string problem)
    {
        if (!WorldCollectibles.ResolveSpot(scene, pickup.spot, Footprint, out Vector3 position, out Quaternion rotation, out Transform surface, out problem))
            return null;
        var root = new GameObject("Pickup " + pickup.id + " (T11)");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.SetPositionAndRotation(position, rotation);
        Material material = WorldCollectibles.SurfaceMaterial(surface);

        // Tray: a shallow base with a back lip, in the surface's own wood.
        WorldCollectibles.Block(root.transform, "Tray", new Vector3(0f, 0.006f, 0f), new Vector3(Footprint.x, 0.012f, Footprint.y), material);
        WorldCollectibles.Block(root.transform, "Tray Lip", new Vector3(0f, 0.02f, -Footprint.y * 0.42f), new Vector3(Footprint.x, 0.03f, 0.012f), material);
        WorldCollectibles.Plaque(root.transform, pickup.plaque, new Vector3(0f, 0.025f, Footprint.y * 0.5f + 0.005f), Footprint.x * 0.9f);

        // The card stands in the tray, tilted back, its face toward the room.
        bool voucher = pickup.itemId == InventoryCatalogue.ReviewVoucher;
        float width = voucher ? 0.15f : 0.18f;
        var pivot = new GameObject("Card");
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 0.012f, -Footprint.y * 0.15f);
        pivot.transform.localRotation = Quaternion.Euler(-20f, 180f, 0f);
        RectTransform face = WorldCollectibles.WorldFace(pivot.transform, "Face", WorldCardFace.Size, width);
        face.pivot = new Vector2(0.5f, 0f);
        face.localPosition = Vector3.zero;
        WorldCardFace.Build(face, pickup, back: false);
        RectTransform rear = WorldCollectibles.WorldFace(pivot.transform, "Back", WorldCardFace.Size, width);
        rear.pivot = new Vector2(0.5f, 0f);
        rear.localPosition = new Vector3(0f, 0f, 0.002f);
        rear.localRotation = Quaternion.Euler(0f, 180f, 0f);
        WorldCardFace.Build(rear, pickup, back: true);

        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.07f, 0f);
        collider.size = new Vector3(Footprint.x + 0.06f, 0.16f, Footprint.y + 0.06f);
        WorldCollectible collectible = root.AddComponent<WorldCollectible>();
        collectible.owner = owner;
        collectible.Definition = pickup;
        collectible.cardPivot = pivot.transform;
        collectible.restLocal = pivot.transform.localPosition;
        collectible.restLocalRotation = pivot.transform.localRotation;
        collectible.highlight = HighlightEffect.For(collectible);
        return collectible;
    }

    public void Lift(bool up) => liftTarget = up ? 1f : 0f;

    /// <summary>After a committed collect: flies to the slot / Bag (null: just goes) and removes itself.</summary>
    public void FlyTo(RectTransform target, bool keepsake)
    {
        Camera camera = Camera.main;
        flightKeepsake = keepsake;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_paper_pickup_01", 0.6f);
        if (target == null || camera == null || cardPivot == null)
        {
            Land();
            return;
        }
        Vector3 start = camera.WorldToScreenPoint(cardPivot.position);
        if (start.z <= camera.nearClipPlane)
        {
            Land();
            return;
        }
        cardPivot.SetParent(null, true);
        flightCamera = camera;
        flightTarget = target;
        flightFrom = start;
        flightFromDepth = start.z;
        flightToDepth = WorldPickupMotion.FlightEndDepth(camera, start.z);
        flightFromRotation = cardPivot.rotation;
        flightFromScale = cardPivot.localScale;
        flightElapsed = 0f;
        flying = true;
        // The tray and plaque stay behind, hidden; only the card flies.
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        foreach (Canvas canvas in GetComponentsInChildren<Canvas>()) canvas.enabled = false;
        GetComponent<Collider>().enabled = false;
    }

    private void Update()
    {
        if (flying)
        {
            flightElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(flightElapsed / FlightSeconds);
            Vector2 to = RectTransformUtility.WorldToScreenPoint(null, flightTarget.TransformPoint(flightTarget.rect.center));
            WorldPickupMotion.Fly(cardPivot, flightCamera, flightFrom, to, flightFromDepth, flightToDepth, flightFromRotation, flightFromScale, t);
            if (t >= 1f) Land();
            return;
        }
        float step = Time.unscaledDeltaTime / LiftSeconds;
        lift = Mathf.MoveTowards(lift, liftTarget, step);
        if (cardPivot == null) return;
        float eased = WorldPickupMotion.Smooth(lift);
        cardPivot.localPosition = restLocal + Vector3.up * (0.08f * eased);
        cardPivot.localRotation = Quaternion.Slerp(restLocalRotation, Quaternion.Euler(-70f, 180f, 0f), eased);
    }

    private void Land()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEffect("sfx_card_place_01", 0.8f);
            if (flightKeepsake) AudioManager.Instance.PlayEffect("sfx_hint_received_01", 0.4f);
        }
        flying = false;
        if (owner != null) owner.Forget(this);
        Destroy(gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlight != null) highlight.SetFocused(!flying);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlight != null) highlight.SetFocused(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (flying || owner == null) return;
        if (eventData != null && (eventData.button != PointerEventData.InputButton.Left || MobileInputGuard.IsDragOrCancelled(eventData) ||
                                  VirtualJoystick.BlocksWorldInput || MobileInputGuard.IsPointerOverUI(eventData.position))) return;
        if (DialogueUI.IsAnyDialogueActive) return;
        if (eventData != null && !InteractionRange.PlayerCanReach(this)) return;
        owner.Tapped(this);
    }

    private void OnDestroy()
    {
        if (cardPivot != null && cardPivot.parent != transform) Destroy(cardPivot.gameObject);
    }
}

/// <summary>
/// The keepsake stand at the player's desk (T11 display anchor): a small
/// easel built from the desk's own wood, showing the displayed keepsake's
/// printed face, or empty. A tap (at the desk or within reach) displays an
/// owned keepsake or takes it back (WorldCollectibles.StandTapped).
/// Placeholder shape: final art is not part of this ticket.
/// </summary>
public sealed class KeepsakeStand : MonoBehaviour, IPointerClickHandler
{
    public static readonly Vector2 Footprint = new Vector2(0.2f, 0.12f);
    private WorldCollectibles owner;
    private RectTransform face;
    private string shownItem = "";

    public DisplayDefinition Definition { get; private set; }

    public static KeepsakeStand Create(Scene scene, DisplayDefinition display, WorldCollectibles owner, out string problem)
    {
        if (!WorldCollectibles.ResolveSpot(scene, display.spot, Footprint, out Vector3 position, out Quaternion rotation, out Transform surface, out problem))
            return null;
        var root = new GameObject("Keepsake Stand " + display.id + " (T11)");
        root.transform.SetParent(surface, true);
        root.transform.SetPositionAndRotation(position, rotation);
        Material material = WorldCollectibles.SurfaceMaterial(surface);
        WorldCollectibles.Block(root.transform, "Base", new Vector3(0f, 0.006f, 0f), new Vector3(Footprint.x, 0.012f, Footprint.y), material);
        WorldCollectibles.Block(root.transform, "Easel", new Vector3(0f, 0.06f, -0.03f), new Vector3(Footprint.x * 0.9f, 0.11f, 0.01f), material,
            Quaternion.Euler(-15f, 0f, 0f));
        WorldCollectibles.Plaque(root.transform, display.title, new Vector3(0f, 0.02f, Footprint.y * 0.5f), Footprint.x * 0.6f);

        var pivot = new GameObject("Shown");
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 0.014f, -0.02f);
        pivot.transform.localRotation = Quaternion.Euler(-15f, 180f, 0f);

        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.07f, 0f);
        collider.size = new Vector3(Footprint.x + 0.05f, 0.15f, Footprint.y + 0.05f);
        KeepsakeStand stand = root.AddComponent<KeepsakeStand>();
        stand.owner = owner;
        stand.Definition = display;
        stand.face = WorldCollectibles.WorldFace(pivot.transform, "Face", WorldCardFace.Size, Footprint.x * 0.85f);
        stand.face.pivot = new Vector2(0.5f, 0f);
        stand.face.localPosition = Vector3.zero;
        stand.face.gameObject.SetActive(false);
        return stand;
    }

    /// <summary>Shows what the inventory says is on this anchor (restored on load without any event).</summary>
    public void Refresh()
    {
        if (owner == null || owner.Hotbar == null) return;
        string item = InventoryRules.DisplayedAt(owner.Hotbar.Inventory, Definition.id);
        if (item == shownItem) return;
        shownItem = item;
        for (int i = face.childCount - 1; i >= 0; i--) Destroy(face.GetChild(i).gameObject);
        PickupDefinition printed = item.Length > 0 ? owner.DefinitionFor(item) : null;
        face.gameObject.SetActive(printed != null);
        if (printed != null) WorldCardFace.Build(face, printed, back: false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner == null || DialogueUI.IsAnyDialogueActive) return;
        if (eventData != null && (eventData.button != PointerEventData.InputButton.Left || MobileInputGuard.IsDragOrCancelled(eventData) ||
                                  MobileInputGuard.IsPointerOverUI(eventData.position))) return;
        // Seated at the desk only: from first person a tap on the desk sits the player down, as before.
        CameraController cc = CameraController.Instance;
        bool seated = cc != null && cc.CurrentMode == CameraController.CameraMode.Workstation;
        if (eventData != null && !seated) return;
        owner.StandTapped(this);
    }
}

/// <summary>
/// The printed inspect card (T11): the object's face large (a postcard shows
/// front and back), its title and lines, an optional notice (for example
/// "You already carry two vouchers") and big buttons: COLLECT / LEAVE IT
/// for a pickup, DISPLAY IT / CLOSE at the stand, TAKE IT BACK / CLOSE for a
/// displayed keepsake, CLOSE when reading. Holds the player still while open.
/// </summary>
public sealed class CollectibleInspectCard : MonoBehaviour
{
    public enum Mode { Pickup, Read, Display, Displayed }

    private const int SortingOrder = 2570;
    private RectTransform card;
    private RectTransform front, back;
    private TextMeshProUGUI title, body, message;
    private Button primary, secondary;
    private TextMeshProUGUI primaryLabel, secondaryLabel;
    private Action onPrimary, onSecondary;
    private bool showing;
    private float shownAt;

    public static CollectibleInspectCard Create(Transform canvasRoot, WorldCollectibles owner)
    {
        RectTransform layer = HudTheme.CreateRect("Collectible Card (T11)", canvasRoot);
        HudTheme.Stretch(layer);
        var canvas = layer.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;
        layer.gameObject.AddComponent<GraphicRaycaster>();
        CollectibleInspectCard view = layer.gameObject.AddComponent<CollectibleInspectCard>();
        view.Build(layer);
        layer.gameObject.SetActive(false);
        return view;
    }

    private void Build(RectTransform layer)
    {
        RectTransform shade = HudTheme.CreateRect("Backdrop", layer);
        HudTheme.Stretch(shade);
        HudTheme.AddImage(shade, null, new Color(0.02f, 0.03f, 0.05f, 0.55f)).raycastTarget = true;

        card = HudTheme.CreateRect("Card", layer);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(1240f, 640f);
        RectTransform body0 = HudTheme.CreatePanel("Panel", card, 20f);
        HudTheme.Stretch((RectTransform)body0.parent);

        front = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Front", body0), 60f, 30f, 420f, 280f);
        back = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Back", body0), 60f, 330f, 420f, 280f);

        title = HudTheme.CreateLabel("Title", body0, "", 38f, HudTheme.BrassSoft, TextAlignmentOptions.Left, FontStyles.Bold);
        HudTheme.PlaceTopLeft(title.rectTransform, 560f, 40f, 640f, 56f);
        body = HudTheme.CreateLabel("Body", body0, "", 26f, HudTheme.TextPrimary, TextAlignmentOptions.TopLeft);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.enableAutoSizing = true;
        body.fontSizeMin = 16f;
        HudTheme.PlaceTopLeft(body.rectTransform, 560f, 110f, 640f, 260f);
        message = HudTheme.CreateLabel("Message", body0, "", 24f, new Color32(255, 196, 120, 255), TextAlignmentOptions.TopLeft, FontStyles.Bold);
        message.textWrappingMode = TextWrappingModes.Normal;
        HudTheme.PlaceTopLeft(message.rectTransform, 560f, 380f, 640f, 80f);

        primary = Button(body0, "Primary", 560f, 480f, 360f, 110f, out primaryLabel, () => onPrimary?.Invoke());
        secondary = Button(body0, "Secondary", 940f, 480f, 260f, 110f, out secondaryLabel, () => onSecondary?.Invoke());
    }

    private static Button Button(RectTransform parent, string name, float x, float y, float w, float h, out TextMeshProUGUI label, UnityEngine.Events.UnityAction click)
    {
        RectTransform rect = HudTheme.PlaceTopLeft(HudTheme.CreateRect(name, parent), x, y, w, h);
        Image face = HudTheme.AddImage(rect, HudTheme.Rounded, name == "Primary" ? (Color)new Color32(120, 92, 44, 255) : (Color)new Color32(16, 25, 38, 245), 18f);
        face.raycastTarget = true;
        var rim = rect.gameObject.AddComponent<Outline>();
        rim.effectColor = HudTheme.Brass;
        rim.effectDistance = new Vector2(3f, -3f);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = face;
        button.onClick.AddListener(click);
        label = HudTheme.CreateLabel("Label", rect, "", 34f, HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(label.rectTransform, 10f);
        label.characterSpacing = 3f;
        return button;
    }

    public void Show(PickupDefinition pickup, Mode mode, string notice, Action primaryAction, Action secondaryAction)
    {
        if (pickup == null) return;
        Clear(front);
        Clear(back);
        bool voucher = pickup.itemId == InventoryCatalogue.ReviewVoucher;
        RectTransform frontFace = Face(front);
        WorldCardFace.Build(frontFace, pickup, back: false);
        back.gameObject.SetActive(!voucher);
        if (!voucher) WorldCardFace.Build(Face(back), pickup, back: true);
        front.anchoredPosition = new Vector2(60f, voucher ? -180f : -30f);
        title.text = pickup.title;
        body.text = pickup.lines != null ? string.Join("\n\n", pickup.lines) : "";
        message.text = notice ?? "";
        onPrimary = primaryAction;
        onSecondary = secondaryAction;
        switch (mode)
        {
            case Mode.Pickup: primaryLabel.text = "COLLECT"; secondaryLabel.text = "LEAVE IT"; break;
            case Mode.Display: primaryLabel.text = "DISPLAY IT"; secondaryLabel.text = "CLOSE"; break;
            case Mode.Displayed: primaryLabel.text = "TAKE IT BACK"; secondaryLabel.text = "CLOSE"; break;
            default: primaryLabel.text = "CLOSE"; secondaryLabel.text = ""; break;
        }
        secondary.gameObject.SetActive(mode != Mode.Read);
        if (!showing)
        {
            showing = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            HudWindowLock.Acquire(this);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_pickup_01", 0.6f);
        }
        shownAt = Time.unscaledTime;
    }

    public void SetMessage(string text) => message.text = text ?? "";

    public void Hide()
    {
        if (!showing) return;
        showing = false;
        onPrimary = onSecondary = null;
        gameObject.SetActive(false);
        HudWindowLock.Release(this);
    }

    private void OnDisable()
    {
        if (!showing) return;
        showing = false;
        HudWindowLock.Release(this);
    }

    private void Update()
    {
        if (!showing) return;
        float t = Time.unscaledTime - shownAt;
        card.localScale = Vector3.one * Mathf.LerpUnclamped(0.88f, 1f, HudTheme.EaseOutBack(Mathf.Clamp01(t / 0.25f)));
        // A conversation or card taking over closes it as LEAVE IT would.
        if (DialogueUI.IsAnyDialogueActive || HudRewardScreen.AnyOpen || InGameSettings.IsOpen) onSecondary?.Invoke();
        if (showing && (DialogueUI.IsAnyDialogueActive || HudRewardScreen.AnyOpen || InGameSettings.IsOpen)) Hide();
    }

    private static void Clear(RectTransform rect)
    {
        for (int i = rect.childCount - 1; i >= 0; i--) Destroy(rect.GetChild(i).gameObject);
    }

    /// <summary>A face root scaled from the 600 x 400 design to the slot it fills.</summary>
    private static RectTransform Face(RectTransform slot)
    {
        RectTransform face = HudTheme.CreateRect("Face", slot);
        face.anchorMin = face.anchorMax = new Vector2(0f, 1f);
        face.pivot = new Vector2(0f, 1f);
        face.sizeDelta = WorldCardFace.Size;
        face.anchoredPosition = Vector2.zero;
        face.localScale = Vector3.one * (slot.sizeDelta.x / WorldCardFace.Size.x);
        return face;
    }
}
