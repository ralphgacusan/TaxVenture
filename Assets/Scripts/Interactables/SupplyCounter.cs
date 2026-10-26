using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>The Supply Counter catalogue from Resources/Campaign/shop.json, or ShopCatalogue.BuiltIn() when it is missing or broken.</summary>
public static class ShopCatalogueAsset
{
    public const string ResourcePath = "Campaign/shop";
    private static ShopCatalogue cached;

    public static ShopCatalogue Current => cached ?? (cached = Load());

    private static ShopCatalogue Load()
    {
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogWarning("[Shop] Resources/" + ResourcePath + ".json not found; using the built-in catalogue.");
            return ShopCatalogue.BuiltIn();
        }
        try
        {
            ShopCatalogue parsed = JsonUtility.FromJson<ShopCatalogue>(asset.text);
            List<string> problems = parsed != null ? parsed.Problems(InventoryCatalogueAsset.Current) : new List<string> { "empty file" };
            if (parsed != null && problems.Count == 0) return parsed;
            Debug.LogWarning("[Shop] shop.json has problems; using the built-in catalogue: " + string.Join("; ", problems));
        }
        catch (System.Exception error)
        {
            Debug.LogWarning("[Shop] shop.json could not be read; using the built-in catalogue. " + error.Message);
        }
        return ShopCatalogue.BuiltIn();
    }
}

/// <summary>
/// PURPOSE:
/// The physical Supply Counter (Campaign Foundation T12): a scoped copy of
/// the Synty shop counter beside the office's plaza-side entrance, dressed
/// with a shelf, crates, an "OFFICE SUPPLIES" board, a brass "SUPPLIES"
/// nameplate, the open sample catalogue, price cards and the tied bookmark
/// packet ("40 COINS / THREE PINS"). One interaction target: a tap (the
/// usual "TAP TO BROWSE" prompt) holds the player, turns the view smoothly
/// to the counter and opens the catalogue panel (SupplyShopScreen).
///
/// The first visit in a story run introduces the shop (one save); from then
/// on the menu's SHOP entry is unlocked too. Practice, replays and Editor
/// runs can browse but never spend. Placement comes from shop.json
/// ("counter") and is the same on every load; nothing in the scene file is
/// edited and the source prefabs are never modified.
///
/// DOES NOT: decide prices, ownership or purchases (ShopRules /
/// PlayerProgressManager.CommitPurchase do).
/// </summary>
[DefaultExecutionOrder(170)]
public sealed class SupplyCounter : MonoBehaviour, IInteractable
{
    public const string CueOpened = "ShopOpened";
    public const string CueClosed = "ShopClosed";
    private const float TurnSeconds = 0.45f;

    public static SupplyCounter Instance { get; private set; }

    private SupplyShopScreen screen;
    private Transform lookTarget;
    private HighlightEffect highlight;
    private GameObject packet;
    private readonly Dictionary<string, GameObject> ownedStamps = new Dictionary<string, GameObject>(System.StringComparer.Ordinal);
    private bool opening;

    /// <summary>The point the view turns to when the counter opens (the sample catalogue).</summary>
    public Transform LookTarget => lookTarget;

    // =========================================================
    // BUILD
    // =========================================================

    public static SupplyCounter Create(Scene scene, SupplyShopScreen screen)
    {
        ShopCatalogue catalogue = ShopCatalogueAsset.Current;
        CampaignShopAssets assets = CampaignShopAssets.Load();
        if (assets == null || assets.counterPrefab == null)
        {
            Debug.LogWarning("[Shop] Resources/Campaign/shop_assets is missing its counter prefab; the Supply Counter is not built.");
            return null;
        }

        var root = new GameObject("Supply Counter (T12)");
        // The player's Interactor raycasts only this layer, including the root collider below.
        root.layer = LayerMask.NameToLayer("Interactable");
        SceneManager.MoveGameObjectToScene(root, scene);
        GameObject counter = Instantiate(assets.counterPrefab, root.transform);
        counter.name = "Counter";
        counter.transform.localPosition = Vector3.zero;
        counter.transform.localRotation = Quaternion.identity;
        // The long side runs along the office front: turn the copy when its long axis is local Z.
        Bounds body = LocalBounds(root.transform, counter.transform);
        if (body.size.z > body.size.x)
        {
            counter.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            body = LocalBounds(root.transform, counter.transform);
        }
        // flip turns only the counter model round (its customer side), never the sign or nameplate.
        if (catalogue.counter != null && catalogue.counter.flip)
        {
            counter.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * counter.transform.localRotation;
            body = LocalBounds(root.transform, counter.transform);
        }
        counter.transform.localPosition = new Vector3(-body.center.x, -body.min.y, -body.center.z);
        body = LocalBounds(root.transform, counter.transform);

        if (!Place(scene, catalogue.counter, body, root.transform, out string problem))
        {
            Debug.LogWarning("[Shop] No clear spot for the Supply Counter (" + problem + "); tune \"counter\" in shop.json. It is not built.");
            Destroy(root);
            return null;
        }

        SupplyCounter supply = root.AddComponent<SupplyCounter>();
        supply.screen = screen;
        supply.Dress(assets, catalogue, body);
        var collider = root.AddComponent<BoxCollider>();
        collider.center = body.center;
        collider.size = body.size + new Vector3(0.2f, 0.1f, 0.3f);
        supply.highlight = HighlightEffect.For((IInteractable)supply);
        Instance = supply;
        Debug.Log($"[Shop] Supply Counter at {root.transform.position} facing {root.transform.forward}.");
        return supply;
    }

    private void OnEnable()
    {
        PlayerProgressManager.ProgressChanged += HandleProgress;
        PlayerProgressManager.InventoryChanged += RefreshDressing;
    }

    private void OnDisable()
    {
        PlayerProgressManager.ProgressChanged -= HandleProgress;
        PlayerProgressManager.InventoryChanged -= RefreshDressing;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start() => RefreshDressing();

    private void HandleProgress(PlayerProgressData data) => RefreshDressing();

    /// <summary>
    /// Finds the spot: fixed, or stepping out from the entrance toward the
    /// plaza and along the office front until the footprint (plus room for a
    /// customer) is clear open ground. The customer side faces the plaza.
    /// </summary>
    private static bool Place(Scene scene, ShopCounterPlacement placement, Bounds body, Transform root, out string problem)
    {
        problem = "";
        // A scene placement is authoritative; keep automatic placement for scenes without one.
        SupplyCounterPlacement authored = SupplyCounterPlacement.Find(scene);
        if (authored != null)
        {
            root.SetPositionAndRotation(authored.transform.position,
                Quaternion.Euler(0f, authored.transform.eulerAngles.y, 0f));
            return true;
        }
        if (placement == null) { problem = "no placement"; return false; }
        if (placement.useFixed && placement.fixedPosition != null && placement.fixedPosition.Count == 3)
        {
            root.SetPositionAndRotation(new Vector3(placement.fixedPosition[0], placement.fixedPosition[1], placement.fixedPosition[2]),
                Quaternion.Euler(0f, placement.fixedYaw, 0f));
            return true;
        }

        var doors = new List<Vector3>();
        foreach (string path in placement.entrance)
        {
            Transform door = PlayerPlacement.Find(scene, path);
            if (door != null) doors.Add(door.position);
        }
        if (doors.Count == 0)
        {
            Transform fallback = PlayerPlacement.Find(scene, placement.fallback);
            if (fallback == null) { problem = "the office entrance was not found"; return false; }
            doors.Add(fallback.position);
        }
        Vector3 entrance = Vector3.zero;
        foreach (Vector3 door in doors) entrance += door;
        entrance /= doors.Count;

        Transform toward = PlayerPlacement.Find(scene, placement.toward);
        Vector3 outward = toward != null ? toward.position - entrance : Vector3.forward;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.01f) outward = Vector3.forward;
        outward.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, outward).normalized;
        Quaternion facing = Quaternion.LookRotation(outward, Vector3.up);

        foreach (float distance in placement.distances)
            foreach (float offset in placement.offsets)
            {
                Vector3 candidate = entrance + outward * distance + side * offset;
                if (!Physics.Raycast(candidate + Vector3.up * 4f, Vector3.down, out RaycastHit ground, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    continue;
                if (Mathf.Abs(ground.point.y - entrance.y) > 1.2f || ground.normal.y < 0.85f) continue;
                // The counter plus a strip in front for the customer.
                Vector3 half = new Vector3(body.extents.x + 0.5f, body.extents.y, body.extents.z + 0.5f);
                Vector3 centre = ground.point + Vector3.up * (body.extents.y + 0.08f) + outward * 0.35f;
                bool blocked = false;
                foreach (Collider hit in Physics.OverlapBox(centre, half, facing, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    if (hit != ground.collider) { blocked = true; break; }
                if (blocked) continue;
                root.SetPositionAndRotation(ground.point, facing);
                return true;
            }
        problem = "every candidate spot was blocked or off the ground";
        return false;
    }

    // =========================================================
    // DRESSING
    // =========================================================

    private void Dress(CampaignShopAssets assets, ShopCatalogue catalogue, Bounds body)
    {
        float top = body.max.y, width = body.size.x, depth = body.size.z;
        Material wood = DecorModels.Solid(new Color32(112, 78, 46, 255), 0f, 0.3f);

        // The sample catalogue, open toward the customer.
        if (assets.bookPrefab != null)
        {
            GameObject book = Instantiate(assets.bookPrefab, transform);
            book.name = "Sample Catalogue";
            book.transform.localPosition = new Vector3(-width * 0.04f, top, depth * 0.05f);
            book.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            StripColliders(book);
        }
        lookTarget = new GameObject("Look Target").transform;
        lookTarget.SetParent(transform, false);
        lookTarget.localPosition = new Vector3(-width * 0.04f, top + 0.05f, depth * 0.05f);

        // Price cards along the front edge, each with its picture.
        ShopProduct satchel = catalogue.Find(ShopCatalogue.Satchel);
        ShopProduct pouch = catalogue.Find(ShopCatalogue.BeltPouch);
        ShopProduct bookmarks = catalogue.Find(ShopCatalogue.BookmarkSet);
        if (satchel != null) PriceCard(satchel, new Vector3(-width * 0.34f, top, depth * 0.3f));
        if (pouch != null) PriceCard(pouch, new Vector3(width * 0.2f, top, depth * 0.3f));

        // The tied packet of three leather ribbon bookmarks with its paper tag.
        if (bookmarks != null)
        {
            packet = new GameObject("Bookmark Packet");
            packet.transform.SetParent(transform, false);
            packet.transform.localPosition = new Vector3(width * 0.38f, top + 0.004f, depth * 0.02f);
            packet.transform.localRotation = Quaternion.Euler(0f, 160f, 0f);
            RectTransform face = WorldFace(packet.transform, "Packet", new Vector2(300f, 420f), 0.11f);
            face.localRotation = Quaternion.Euler(90f, 0f, 0f);
            BuildPacket(face);
            RectTransform tag = WorldFace(packet.transform, "Price Tag", new Vector2(420f, 200f), 0.12f);
            tag.localPosition = new Vector3(0f, 0.04f, 0.09f);
            tag.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            Box(tag, "Tag", 0f, 0f, 420f, 200f, new Color32(246, 238, 214, 255), 18f);
            Label(tag, "Text", $"{bookmarks.price} COINS\nTHREE PINS", 20f, 20f, 380f, 160f, 54f, new Color32(60, 44, 26, 255), FontStyles.Bold);
            ownedStamps[bookmarks.id] = Stamp(tag, 420f, 200f);
        }

        // Brass nameplate on the customer face.
        RectTransform plate = WorldFace(transform, "Nameplate", new Vector2(640f, 150f), Mathf.Min(0.9f, width * 0.45f));
        plate.localPosition = new Vector3(0f, top * 0.72f, body.max.z + 0.006f);
        plate.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Box(plate, "Rim", 0f, 0f, 640f, 150f, new Color32(150, 116, 58, 255), 22f);
        Box(plate, "Face", 8f, 8f, 624f, 134f, new Color32(226, 192, 124, 255), 18f);
        Label(plate, "Text", "SUPPLIES", 0f, 8f, 640f, 134f, 96f, new Color32(70, 52, 24, 255), FontStyles.Bold).characterSpacing = 12f;

        // The board above: two posts in the counter's wood, the HUD's navy and brass.
        float postX = width * 0.5f - 0.06f;
        Block(transform, "Sign Post L", new Vector3(-postX, top + 0.55f, -depth * 0.32f), new Vector3(0.06f, 1.1f, 0.06f), wood);
        Block(transform, "Sign Post R", new Vector3(postX, top + 0.55f, -depth * 0.32f), new Vector3(0.06f, 1.1f, 0.06f), wood);
        RectTransform board = WorldFace(transform, "Office Supplies Board", new Vector2(1200f, 300f), width * 0.95f);
        board.localPosition = new Vector3(0f, top + 1.0f, -depth * 0.32f + 0.04f);
        board.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Box(board, "Rim", 0f, 0f, 1200f, 300f, HudTheme.Brass, 34f);
        Box(board, "Face", 10f, 10f, 1180f, 280f, new Color32(18, 28, 41, 255), 28f);
        RectTransform coin = Box(board, "Coin", 60f, 70f, 160f, 160f, HudTheme.Exp);
        coin.GetComponent<Image>().sprite = HudTheme.Circle;
        RectTransform inner = Box(coin, "Inner", 26f, 26f, 108f, 108f, new Color32(214, 160, 70, 255));
        inner.GetComponent<Image>().sprite = HudTheme.Circle;
        Label(board, "Title", "OFFICE SUPPLIES", 250f, 40f, 900f, 140f, 110f, HudTheme.TextPrimary, FontStyles.Bold).characterSpacing = 8f;
        Label(board, "Subtitle", "Spend the coins you earn on jobs", 250f, 180f, 900f, 80f, 52f, HudTheme.BrassSoft, FontStyles.Italic);

        // A shelf behind and crates at one end, only where there is room.
        if (assets.shelfPrefab != null) TryProp(assets.shelfPrefab, "Shelf", new Vector3(0f, 0f, body.min.z - 0.45f), 0f, body);
        if (assets.cratePrefab != null)
        {
            GameObject crate = TryProp(assets.cratePrefab, "Crate", new Vector3(body.max.x + 0.45f, 0f, 0f), 12f, body);
            if (crate != null)
            {
                Bounds lower = LocalBounds(transform, crate.transform);
                GameObject upper = Instantiate(assets.cratePrefab, transform);
                upper.name = "Crate Top";
                upper.transform.localPosition = new Vector3(body.max.x + 0.45f, lower.max.y, 0.02f);
                upper.transform.localRotation = Quaternion.Euler(0f, -14f, 0f);
                upper.transform.localScale = Vector3.one * 0.8f;
                StripColliders(upper);
            }
        }
    }

    private void PriceCard(ShopProduct product, Vector3 local)
    {
        var holder = new GameObject("Price Card " + product.id);
        holder.transform.SetParent(transform, false);
        holder.transform.localPosition = local;
        RectTransform card = WorldFace(holder.transform, "Card", new Vector2(360f, 420f), 0.13f);
        card.pivot = new Vector2(0.5f, 0f);
        card.localPosition = Vector3.zero;
        card.localRotation = Quaternion.Euler(-12f, 180f, 0f);
        Box(card, "Paper", 0f, 0f, 360f, 420f, new Color32(250, 246, 234, 255), 20f);
        Box(card, "Band", 0f, 0f, 360f, 70f, new Color32(18, 28, 41, 255), 20f);
        Label(card, "Name", product.title.ToUpperInvariant(), 10f, 6f, 340f, 60f, 40f, HudTheme.BrassSoft, FontStyles.Bold);
        RectTransform icon = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Icon", card), 90f, 90f, 180f, 180f);
        Image image = icon.gameObject.AddComponent<Image>();
        image.sprite = HudIconArt.Get(product.icon);
        image.preserveAspect = true;
        image.raycastTarget = false;
        Label(card, "Price", product.price + " COINS", 10f, 290f, 340f, 70f, 54f, new Color32(60, 44, 26, 255), FontStyles.Bold);
        Label(card, "Effect", product.tagline, 16f, 356f, 328f, 56f, 28f, new Color32(110, 88, 60, 255), FontStyles.Italic);
        ownedStamps[product.id] = Stamp(card, 360f, 420f);
    }

    private static void BuildPacket(RectTransform face)
    {
        Color[] leather = { new Color32(150, 40, 46, 255), new Color32(46, 112, 74, 255), new Color32(44, 70, 128, 255) };
        for (int i = 0; i < 3; i++)
        {
            RectTransform ribbon = Box(face, "Ribbon " + i, 70f + i * 60f, 20f + i * 16f, 44f, 380f - i * 16f, leather[i], 6f);
            ribbon.localRotation = Quaternion.Euler(0f, 0f, -4f + i * 4f);
        }
        Box(face, "Paper Band", 30f, 150f, 240f, 80f, new Color32(236, 226, 200, 255), 8f);
        Box(face, "String", 30f, 184f, 240f, 10f, new Color32(150, 116, 58, 255));
    }

    private static GameObject Stamp(RectTransform parent, float width, float height)
    {
        RectTransform stamp = HudTheme.CreateRect("Owned Stamp", parent);
        stamp.anchorMin = stamp.anchorMax = new Vector2(0.5f, 0.5f);
        stamp.sizeDelta = new Vector2(width * 0.82f, Mathf.Min(height * 0.36f, 120f));
        stamp.localRotation = Quaternion.Euler(0f, 0f, -12f);
        Image rim = HudTheme.AddImage(stamp, HudTheme.Rounded, new Color32(46, 140, 84, 230), 14f);
        RectTransform face = HudTheme.CreateRect("Face", stamp);
        HudTheme.Stretch(face, 6f);
        HudTheme.AddImage(face, HudTheme.Rounded, new Color32(246, 240, 224, 235), 10f);
        TextMeshProUGUI text = HudTheme.CreateLabel("Text", face, "OWNED", 60f, new Color32(46, 140, 84, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(text.rectTransform, 4f);
        text.characterSpacing = 8f;
        rim.raycastTarget = false;
        stamp.gameObject.SetActive(false);
        return stamp.gameObject;
    }

    /// <summary>Owned products show their stamp; the packet leaves the counter once the set is bought.</summary>
    private void RefreshDressing()
    {
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        PlayerProgressData data = manager != null ? manager.CurrentProgress : null;
        ShopCatalogue catalogue = ShopCatalogueAsset.Current;
        bool story = manager != null && manager.UsesStoryInventory;
        foreach (KeyValuePair<string, GameObject> stamp in ownedStamps)
            if (stamp.Value != null) stamp.Value.SetActive(story && ShopRules.IsOwned(data, catalogue.Find(stamp.Key)));
        if (packet != null)
        {
            Transform ribbons = packet.transform.Find("Packet");
            if (ribbons != null) ribbons.gameObject.SetActive(!(story && ShopRules.IsOwned(data, catalogue.Find(ShopCatalogue.BookmarkSet))));
        }
    }

    // =========================================================
    // INTERACTION
    // =========================================================

    public void OnFocus()
    {
        if (highlight != null) highlight.SetFocused(true);
    }

    public void OnUnfocus()
    {
        if (highlight != null) highlight.SetFocused(false);
    }

    public string GetPromptText() => "Click to browse supplies";

    public void OnInteract()
    {
        if (!opening && isActiveAndEnabled) StartCoroutine(OpenRoutine());
    }

    /// <summary>Hold still, introduce the shop on the first story visit, turn the view to the counter, then open the catalogue.</summary>
    private IEnumerator OpenRoutine()
    {
        if (screen == null || screen.IsOpen || InventoryScreen.AnyOpen || !HudHotbar.CanOpenPanels()) yield break;
        opening = true;
        HudWindowLock.Acquire(this);
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        bool firstVisit = manager != null && manager.UsesStoryInventory && !manager.ShopIntroduced && manager.MarkShopIntroduced();
        if (lookTarget != null) ViewTurn.Toward(lookTarget.position, TurnSeconds);
        float until = Time.unscaledTime + TurnSeconds * 0.75f;
        while (Time.unscaledTime < until) yield return null;
        if (HudHotbar.CanOpenPanels()) screen.Open(firstVisit);
        HudWindowLock.Release(this);
        opening = false;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private GameObject TryProp(GameObject prefab, string name, Vector3 local, float yaw, Bounds counter)
    {
        GameObject prop = Instantiate(prefab, transform);
        prop.name = name;
        prop.transform.localPosition = local;
        prop.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Bounds b = LocalBounds(transform, prop.transform);
        Vector3 worldCentre = transform.TransformPoint(b.center + Vector3.up * 0.05f);
        foreach (Collider hit in Physics.OverlapBox(worldCentre, b.extents * 0.95f, transform.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.bounds.max.y < transform.position.y + 0.06f) continue; // the ground itself
            Destroy(prop);
            return null;
        }
        StripColliders(prop);
        return prop;
    }

    private static void StripColliders(GameObject prop)
    {
        foreach (Collider collider in prop.GetComponentsInChildren<Collider>(true)) Destroy(collider);
    }

    /// <summary>Mesh bounds of `part` expressed in `space` (renderers only, no colliders).</summary>
    public static Bounds LocalBounds(Transform space, Transform part)
    {
        bool any = false;
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        foreach (MeshFilter filter in part.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 m = space.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mesh.center + Vector3.Scale(mesh.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 p = m.MultiplyPoint3x4(corner);
                if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                else bounds.Encapsulate(p);
            }
        }
        if (!any) bounds = new Bounds(new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 1f, 0.7f));
        return bounds;
    }

    private static void Block(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        Destroy(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = size;
        Renderer renderer = part.GetComponent<Renderer>();
        if (material != null) renderer.sharedMaterial = material;
    }

    private static RectTransform WorldFace(Transform parent, string name, Vector2 size, float widthMetres)
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

    private static RectTransform Box(RectTransform parent, string name, float x, float y, float w, float h, Color color, float radius = 0f)
    {
        RectTransform rect = HudTheme.PlaceTopLeft(HudTheme.CreateRect(name, parent), x, y, w, h);
        HudTheme.AddImage(rect, radius > 0f ? HudTheme.Rounded : null, color, radius);
        return rect;
    }

    private static TextMeshProUGUI Label(RectTransform parent, string name, string text, float x, float y, float w, float h, float size, Color color, FontStyles style)
    {
        TextMeshProUGUI label = HudTheme.CreateLabel(name, parent, text, size, color, TextAlignmentOptions.Center, style);
        HudTheme.PlaceTopLeft(label.rectTransform, x, y, w, h);
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }
}
