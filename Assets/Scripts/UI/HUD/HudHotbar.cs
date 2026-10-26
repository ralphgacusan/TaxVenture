using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Shared drop target for world items carried onto the HUD hotbar (T10).
/// The Case Folder (closed or open) and the Tax Code Book use the same test:
/// any unlocked hotbar slot, or anywhere on the row. Active only while a
/// level uses the hotbar (HudHotbar sets Test); otherwise callers keep their
/// own icon test.
/// </summary>
public static class HudSlotDrop
{
    public static Func<Vector2, bool> Test;
    /// <summary>The unlocked hotbar slot under a screen point (-1 = the row or none).</summary>
    public static Func<Vector2, int> SlotUnder;

    private static string pendingItem;
    private static int pendingSlot = -1;
    private static float pendingTime = -10f;
    private const float PendingSeconds = 2f;

    public static bool Active => Test != null;

    public static bool IsOver(Vector2 screen) => Test != null && Test(screen);

    /// <summary>A world item was released over the hotbar: remember which slot (the inventory sync places it there).</summary>
    public static void Note(string itemId, Vector2 screen)
    {
        if (!Active) return;
        pendingItem = itemId;
        pendingSlot = SlotUnder != null ? SlotUnder(screen) : -1;
        pendingTime = Time.unscaledTime;
    }

    /// <summary>The slot a just-dropped item was released on (-1: the row, or no recent drop). Clears it.</summary>
    public static int TakePending(string itemId)
    {
        bool fresh = pendingItem == itemId && Time.unscaledTime - pendingTime < PendingSeconds;
        int slot = fresh ? pendingSlot : -1;
        if (pendingItem == itemId) { pendingItem = null; pendingSlot = -1; }
        return slot;
    }

    public static void Reset()
    {
        Test = null;
        SlotUnder = null;
        pendingItem = null;
        pendingSlot = -1;
    }
}

/// <summary>The inventory catalogue from Resources/Campaign/inventory.json, or InventoryCatalogue.BuiltIn() when it is missing or broken.</summary>
public static class InventoryCatalogueAsset
{
    public const string ResourcePath = "Campaign/inventory";
    private static InventoryCatalogue cached;

    public static InventoryCatalogue Current => cached ?? (cached = Load());

    private static InventoryCatalogue Load()
    {
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogWarning("[Inventory] Resources/" + ResourcePath + ".json not found; using the built-in catalogue.");
            return InventoryCatalogue.BuiltIn();
        }
        try
        {
            InventoryCatalogue parsed = JsonUtility.FromJson<InventoryCatalogue>(asset.text);
            List<string> problems = parsed != null ? parsed.Problems() : new List<string> { "empty file" };
            if (parsed != null && parsed.items.Count > 0 && problems.Count == 0) return parsed;
            Debug.LogWarning("[Inventory] inventory.json has problems; using the built-in catalogue: " + string.Join("; ", problems));
        }
        catch (Exception error)
        {
            Debug.LogWarning("[Inventory] inventory.json could not be read; using the built-in catalogue. " + error.Message);
        }
        return InventoryCatalogue.BuiltIn();
    }
}

/// <summary>
/// PURPOSE:
/// Level 1's HUD icon row as a hotbar (Campaign Foundation T10). The three
/// slots keep today's icon places, size and spacing (x 10, y -20 / -160 /
/// -300, 120 units, 140 apart) and hold whatever item the inventory puts
/// there; a Bag icon always sits after the last unlocked slot.
///
/// HUD-SLOT ADAPTER: each item keeps its existing icon GameObject with all its
/// behaviour (Notes toggle, Case Folder tap/drag-out/drop resolver and badge,
/// Profiles cards and drag-out, the carried Tax Code Book). The icon is placed
/// inside a runtime wrapper that this class moves to the item's slot and
/// shows, or hides (in the bag or not owned: parked at the Bag icon or the
/// slot it will take, so flights still land in the right place). An empty
/// slot is a dim tile with a dashed rim and "+"; tapping it opens the bag
/// with that slot targeted.
///
/// OWNERSHIP: the inventory (PlayerProgressManager.CommitInventory) is the
/// only record of which slot holds what. World owners stay authoritative for
/// whether the item is carried: a stored Case Folder (FolderToHUDIcon), the
/// receptionist's Notes, collected profiles; the book is the inventory's own.
/// This class only observes them and asks the inventory to follow.
///
/// DOES NOT: change cases, trust, coins, clocks or objectives; pause time.
/// </summary>
[DefaultExecutionOrder(150)]
public sealed class HudHotbar : MonoBehaviour, Level2TaxCodeBookIcon.IBookCarrier
{
    public const float SlotSize = 120f;
    public const float SlotStep = 140f;
    public static readonly Vector2 FirstSlot = new Vector2(10f, -20f);
    public const string CueOverflow = "BagOverflow";
    public const string CueOverflowKnown = "BagOverflowKnown";
    public const string CueToBar = "BagItemToBar";
    private const float DropPad = 0.3f;
    private const float RetrySeconds = 2f;
    private const float ReadyTimeout = 10f;

    public static HudHotbar Instance { get; private set; }

    /// <summary>The Tax Code Book was opened at least once in this run (first-open event raised once; Level 1 checkpoint).</summary>
    public static bool BookOpenedOnce { get; private set; }

    /// <summary>Levels whose HUD row is a hotbar (T18 adds Level 2).</summary>
    public static bool UsesHotbar(string levelId) => levelId == OfficeLevelCatalog.Level1Id;

    private sealed class ItemView
    {
        public string ItemId;
        public RectTransform Wrapper;
        public CanvasGroup Group;
        public RectTransform Icon;
        public Transform OriginalParent;
        public Vector2 OriginalPosition;
        public int OriginalSibling;
        public bool Generic;
        public TextMeshProUGUI Count;
        /// <summary>When its paper last left or landed in it (a short pop).</summary>
        public float PunchAt = -10f;
        /// <summary>Dims the icon itself while its item is out (the wrapper's group shows or hides the slot).</summary>
        public CanvasGroup IconGroup;
    }

    private sealed class SlotTile
    {
        public RectTransform Rect;
        public Image Face;
        public Image Glow;
        public Button Button;
    }

    private Transform canvasRoot;
    private RectTransform buttons;
    private InventoryCatalogue catalogue;
    private readonly SlotTile[] tiles = new SlotTile[CampaignInventoryRecord.MaxHotbar];
    private readonly Dictionary<string, ItemView> views = new Dictionary<string, ItemView>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> lastSlot = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<string, float> retryAfter = new Dictionary<string, float>(StringComparer.Ordinal);
    private RectTransform bagIcon;
    private Image bagGlow;
    private TextMeshProUGUI plusOne;
    private float bagPunchStarted = -10f;
    private InventoryScreen screen;
    private HudTakeOutPrompt prompt;
    private WorldCollectibles collectibles;
    private SupplyShopScreen shop;
    private FloatingWindow notesWindow;
    private SupplyCounter counter;
    private OfficeDecor decor;
    // T14: the voucher card.
    private VoucherUseCard voucherCard;
    private Level2TaxCodeBookIcon book;
    private int shownCapacity = -1;
    private float tilesUnlockedAt = -10f;
    private int tilesUnlockedFrom = -1;
    private Vector2 bagShown;
    private bool bagPlaced;

    private bool ready;
    private float readyDeadline;
    private bool resumedRun;
    private bool dirty = true;
    private bool wasAwaiting;
    private bool bookWasShowing;
    private int syncedProfiles = -1;
    private bool syncedCopy;
    private bool syncedFolder;
    private float nextRefresh;

    private FolderToHUDIcon[] folders = new FolderToHUDIcon[0];
    private CaseFolder3DDrag[] closedFolders = new CaseFolder3DDrag[0];
    private FloatingModel3D[] openFolders = new FloatingModel3D[0];
    private TaxCodeBookFloatingModel3D[] openBooks = new TaxCodeBookFloatingModel3D[0];
    private TaxCodeBook3DDrag deskBook;

    private static readonly Vector3[] Corners = new Vector3[4];

    public RectTransform BagIcon => bagIcon;
    public InventoryCatalogue Catalogue => catalogue;
    public InventoryScreen Screen => screen;
    public bool IsReady => ready;

    private static PlayerProgressManager Manager => PlayerProgressManager.Instance;

    /// <summary>The live inventory of this run (story: saved; other runs: temporary).</summary>
    public CampaignInventoryRecord Inventory => Manager != null ? Manager.Inventory : fallback;
    private readonly CampaignInventoryRecord fallback = new CampaignInventoryRecord();
    private bool fallbackNotesChestClaimed;

    // =========================================================
    // BUILD
    // =========================================================

    public static HudHotbar Create(GameObject host, Transform canvasRoot)
    {
        RectTransform row = canvasRoot != null ? canvasRoot.Find("PermanentHUD/IconButtons") as RectTransform : null;
        if (row == null)
        {
            Debug.LogWarning("[Hotbar] PermanentHUD/IconButtons not found; the HUD keeps its fixed icons.");
            return null;
        }
        HudHotbar hotbar = host.AddComponent<HudHotbar>();
        hotbar.canvasRoot = canvasRoot;
        hotbar.buttons = row;
        hotbar.Build();
        return hotbar;
    }

    private void Build()
    {
        Instance = this;
        BookOpenedOnce = false;
        catalogue = InventoryCatalogueAsset.Current;
        resumedRun = Manager != null && Manager.GetLevelOneCheckpoint() != null;
        readyDeadline = Time.unscaledTime + ReadyTimeout;

        for (int i = 0; i < tiles.Length; i++) tiles[i] = CreateTile(i);

        // The carried Tax Code Book (Level 2's icon, stored in this inventory instead of the review).
        Level2TaxCodeBookIcon.Carrier = this;
        book = gameObject.AddComponent<Level2TaxCodeBookIcon>();

        Wrap(InventoryCatalogue.ProfileBinder, buttons.Find("TaxpayerProfileIcon"));
        Wrap(InventoryCatalogue.Notes, buttons.Find("NotesIconButton"));
        Wrap(InventoryCatalogue.CaseFolder, buttons.Find("CaseFolderIconButton"));
        Wrap(InventoryCatalogue.TaxCodeBook, buttons.Find(Level2TaxCodeBookIcon.IconName));
        CreateBagIcon();

        screen = InventoryScreen.Create(canvasRoot, this);
        prompt = HudTakeOutPrompt.Create(canvasRoot, this);
        // T11: optional world pickups, the keepsake display stand, and profile cards committed before they fly.
        collectibles = WorldCollectibles.Create(gameObject, canvasRoot, this);
        TaxpayerProfileWorldPickup.CollectGate = CommitProfile;
        // T12: the Supply Counter beside the office and its catalogue (one shop domain for counter and menu).
        shop = SupplyShopScreen.Create(canvasRoot, this);
        counter = SupplyCounter.Create(gameObject.scene, shop);
        decor = OfficeDecor.Create(gameObject.scene);
        voucherCard = VoucherUseCard.Create(canvasRoot, this);

        HudSlotDrop.Test = OverDropTarget;
        HudSlotDrop.SlotUnder = SlotIndexUnder;
        TaxpayerProfileCollectionHUD.CollectionPointOverride = BinderLanding;
        AuditorInteractable.BagReminder = AuditorBagReminder;
        PlayerProgressManager.InventoryChanged += MarkDirty;
        GameplayEvents.OnNotesUnlockRequested += HandleNotesGranted;
        HookNotesPaper();
        Refresh();
    }

    /// <summary>
    /// The Notes paper flies out of and back into wherever Notes sits now:
    /// its bar slot, or the Bag icon while it is in the bag. That spot pops
    /// as the paper leaves and again as it lands.
    /// </summary>
    private void HookNotesPaper()
    {
        foreach (NotesPanelUI panel in FindObjectsByType<NotesPanelUI>(FindObjectsInactive.Include))
        {
            if (panel.gameObject.scene != gameObject.scene) continue;
            notesWindow = panel.GetComponent<FloatingWindow>();
            break;
        }
        if (notesWindow == null) return;
        notesWindow.PaperIconResolver = NotesAnchor;
        notesWindow.PaperLeftIcon += PunchPaperIcon;
        notesWindow.PaperReachedIcon += PunchPaperIcon;
    }

    private RectTransform NotesAnchor() => ItemAnchor(InventoryCatalogue.Notes);

    /// <summary>Where an owned item shows on the HUD: its bar slot, or the Bag icon (null when not owned).</summary>
    public RectTransform ItemAnchor(string itemId)
    {
        CampaignInventoryRecord r = Inventory;
        if (r == null || !InventoryRules.Owns(r, itemId)) return null;
        SlotRef at = InventoryRules.Find(r, itemId);
        if (at.Area == InventoryArea.Hotbar && at.Index < r.hotbarCapacity && at.Index < tiles.Length && tiles[at.Index] != null)
            return views.TryGetValue(itemId, out ItemView view) ? view.Wrapper : tiles[at.Index].Rect;
        return bagIcon;
    }

    private void PunchPaperIcon(RectTransform icon)
    {
        if (icon == null) return;
        if (icon == bagIcon) { bagPunchStarted = Time.unscaledTime; return; }
        foreach (ItemView view in views.Values)
            if (view.Wrapper == icon || view.Icon == icon) view.PunchAt = Time.unscaledTime;
    }

    /// <summary>
    /// A floating folder is out of its slot: its icon waits dimmed (still tappable to call it
    /// back) and lights up again once the folder is back. The Tax Code Book's icon dims itself.
    /// </summary>
    private void DimWhileOut(ItemView view)
    {
        if (view.Icon == null) return;
        bool reviewed = view.ItemId == InventoryCatalogue.ReviewedFile;
        if (!reviewed && view.ItemId != InventoryCatalogue.CaseFolder) return;
        bool isOut = reviewed
            ? (ReviewedFileViewer.Instance != null && ReviewedFileViewer.Instance.IsOut) ||
              (view.Icon.GetComponent<HudIconDragOpen>() != null && view.Icon.GetComponent<HudIconDragOpen>().IsDragging)
            : OfficeHudRuntime.Instance != null && OfficeHudRuntime.Instance.CaseFolderViewOut;
        if (view.Generic)
        {
            // Its wrapper group owns slot visibility; fade only the picture, never reveal a hidden Bag item.
            Image picture = view.Icon.GetComponent<Image>();
            if (picture != null)
            {
                Color color = picture.color;
                color.a = Mathf.MoveTowards(color.a, isOut ? OutAlpha : 1f, Time.unscaledDeltaTime * 4f);
                picture.color = color;
            }
            return;
        }
        // Gone from the inventory (on the desk, with the Auditor): no leftover dim when it comes back.
        if (!InventoryRules.Owns(Inventory, InventoryCatalogue.CaseFolder) && view.IconGroup != null) { view.IconGroup.alpha = 1f; return; }
        if (view.IconGroup == null)
        {
            if (!isOut) return;
            view.IconGroup = view.Icon.GetComponent<CanvasGroup>();
            if (view.IconGroup == null) view.IconGroup = view.Icon.gameObject.AddComponent<CanvasGroup>();
        }
        float target = isOut ? OutAlpha : 1f;
        view.IconGroup.alpha = Mathf.MoveTowards(view.IconGroup.alpha, target, Time.unscaledDeltaTime * 4f);
    }

    private const float OutAlpha = 0.38f;

    /// <summary>An item's icon squashes and springs back when its paper leaves or lands in it.</summary>
    private void AnimateViews()
    {
        foreach (ItemView view in views.Values)
        {
            DimWhileOut(view);
            float t = Time.unscaledTime - view.PunchAt;
            if (t > 0.5f)
            {
                if (view.Wrapper.localScale != Vector3.one) view.Wrapper.localScale = Vector3.one;
                continue;
            }
            float k = t / 0.5f;
            float squash = t < 0.08f ? 1f - 0.18f * (t / 0.08f) : 1f + 0.2f * Mathf.Sin((k - 0.16f) / 0.84f * Mathf.PI) * (1f - k);
            view.Wrapper.localScale = Vector3.one * squash;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        PlayerProgressManager.InventoryChanged -= MarkDirty;
        GameplayEvents.OnNotesUnlockRequested -= HandleNotesGranted;
        if (notesWindow != null)
        {
            if (notesWindow.PaperIconResolver == (Func<RectTransform>)NotesAnchor) notesWindow.PaperIconResolver = null;
            notesWindow.PaperLeftIcon -= PunchPaperIcon;
            notesWindow.PaperReachedIcon -= PunchPaperIcon;
        }
        if (HudSlotDrop.Test == (Func<Vector2, bool>)OverDropTarget) HudSlotDrop.Reset();
        if (TaxpayerProfileCollectionHUD.CollectionPointOverride == (Func<Vector2?>)BinderLanding) TaxpayerProfileCollectionHUD.CollectionPointOverride = null;
        if (AuditorInteractable.BagReminder == (Func<bool>)AuditorBagReminder) AuditorInteractable.BagReminder = null;
        if (TaxpayerProfileWorldPickup.CollectGate == (Func<TaxpayerProfile, bool>)CommitProfile) TaxpayerProfileWorldPickup.CollectGate = null;
        if (collectibles != null) Destroy(collectibles);
        if (shop != null) Destroy(shop.gameObject);
        if (counter != null) Destroy(counter.gameObject);
        if (decor != null) Destroy(decor.gameObject);
        if (voucherCard != null) Destroy(voucherCard.gameObject);
        if (Level2TaxCodeBookIcon.Carrier == (Level2TaxCodeBookIcon.IBookCarrier)this) Level2TaxCodeBookIcon.Carrier = null;
        foreach (ItemView view in views.Values)
        {
            if (view.Generic || view.Icon == null || view.OriginalParent == null) continue;
            view.Icon.SetParent(view.OriginalParent, false);
            view.Icon.anchoredPosition = view.OriginalPosition;
            view.Icon.SetSiblingIndex(Mathf.Min(view.OriginalSibling, view.OriginalParent.childCount - 1));
        }
        if (screen != null) Destroy(screen.gameObject);
        if (prompt != null) Destroy(prompt.gameObject);
    }

    private SlotTile CreateTile(int index)
    {
        RectTransform rect = HudTheme.CreateRect("Hotbar Slot " + (index + 1), buttons);
        PlaceSlotRect(rect, index);
        rect.SetSiblingIndex(index);
        var tile = new SlotTile { Rect = rect };

        RectTransform glow = HudTheme.CreateRect("Drop Glow", rect);
        HudTheme.Stretch(glow, -10f);
        tile.Glow = HudTheme.AddImage(glow, HudTheme.Rounded, new Color(HudTheme.BrassSoft.r, HudTheme.BrassSoft.g, HudTheme.BrassSoft.b, 0.55f), 26f);
        glow.gameObject.SetActive(false);

        RectTransform face = HudTheme.CreateRect("Face", rect);
        HudTheme.Stretch(face);
        tile.Face = face.gameObject.AddComponent<Image>();
        tile.Face.sprite = HudIconArt.Get("slot");
        tile.Face.preserveAspect = true;
        tile.Face.raycastTarget = true;

        tile.Button = face.gameObject.AddComponent<Button>();
        tile.Button.targetGraphic = tile.Face;
        OfficeHudRuntime.StyleIconButton(tile.Button);
        int slot = index;
        tile.Button.onClick.AddListener(() => OnEmptySlotTapped(slot));
        return tile;
    }

    private static void PlaceSlotRect(RectTransform rect, int index)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(SlotSize, SlotSize);
        rect.anchoredPosition = SlotPosition(index);
    }

    /// <summary>Slot i's place in the row (index = capacity gives the Bag icon's place).</summary>
    public static Vector2 SlotPosition(int index) => FirstSlot + new Vector2(0f, -SlotStep * index);

    /// <summary>Puts an existing icon inside a wrapper this class moves and shows/hides; its own behaviour is untouched.</summary>
    private void Wrap(string itemId, Transform iconTransform)
    {
        var icon = iconTransform as RectTransform;
        if (icon == null)
        {
            Debug.LogWarning($"[Hotbar] The icon for '{itemId}' was not found; that item shows a plain picture.");
            return;
        }
        RectTransform wrapper = HudTheme.CreateRect("Carried " + itemId, buttons);
        wrapper.anchorMin = wrapper.anchorMax = new Vector2(1f, 1f);
        wrapper.pivot = new Vector2(1f, 1f);
        wrapper.sizeDelta = new Vector2(SlotSize, SlotSize);
        wrapper.anchoredPosition = icon.anchoredPosition;
        wrapper.SetSiblingIndex(icon.GetSiblingIndex());
        var view = new ItemView
        {
            ItemId = itemId,
            Wrapper = wrapper,
            Group = wrapper.gameObject.AddComponent<CanvasGroup>(),
            Icon = icon,
            OriginalParent = icon.parent,
            OriginalPosition = icon.anchoredPosition,
            OriginalSibling = icon.GetSiblingIndex()
        };
        icon.SetParent(wrapper, false);
        icon.anchoredPosition = Vector2.zero;
        SetShown(view, false);
        views[itemId] = view;
    }

    /// <summary>A plain picture for an item without its own HUD icon (later tickets' supplies and keepsakes).</summary>
    private ItemView GenericView(string itemId)
    {
        if (views.TryGetValue(itemId, out ItemView existing)) return existing;
        InventoryItemDefinition def = catalogue.Find(itemId);
        RectTransform wrapper = HudTheme.CreateRect("Carried " + itemId, buttons);
        PlaceSlotRect(wrapper, 0);
        var image = wrapper.gameObject.AddComponent<Image>();
        image.sprite = HudIconArt.Get(def != null && !string.IsNullOrEmpty(def.icon) ? def.icon : "papers");
        image.preserveAspect = true;
        var button = wrapper.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        OfficeHudRuntime.StyleIconButton(button);
        // Usable at once: an item with a printed card opens it (T11 postcard, voucher); otherwise the Bag shows it.
        button.onClick.AddListener(() => { if (!CanOpenPanels() || !OpenViewer(itemId)) OpenInventory(SlotRef.None, itemId); });
        if (itemId == InventoryCatalogue.ReviewedFile) AddFolderDragOut(wrapper);
        TextMeshProUGUI count = HudTheme.CreateLabel("Count", wrapper, "", 24f, HudTheme.TextPrimary, TextAlignmentOptions.BottomRight, FontStyles.Bold);
        HudTheme.Stretch(count.rectTransform, 12f);
        count.outlineWidth = 0.25f;
        count.outlineColor = new Color32(10, 16, 26, 255);
        var view = new ItemView { ItemId = itemId, Wrapper = wrapper, Group = wrapper.gameObject.AddComponent<CanvasGroup>(), Icon = wrapper, Generic = true, Count = count };
        SetShown(view, false);
        views[itemId] = view;
        return view;
    }

    /// <summary>
    /// T30 revision: like the Case Folder icon, Arthur's file can be dragged out of its slot (a teal closed-folder
    /// ghost) and opens where it is dropped.
    /// </summary>
    private void AddFolderDragOut(RectTransform slotIcon)
    {
        Canvas canvas = slotIcon.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var drag = slotIcon.gameObject.AddComponent<HudIconDragOpen>();
        drag.Visual = HudDragItem.Create(canvas.rootCanvas.transform, "Reviewed File", slotIcon, new Vector2(300f, 220f), -6f,
            (root, size) => HudDragItem.BuildClosedFolder(root, size, reviewed: true));
        drag.CanDrag = () => ReviewedFileViewer.Instance != null && !ReviewedFileViewer.Instance.IsOut && CanOpenPanels();
        drag.OpenAt = screen => ReviewedFileViewer.Instance != null && ReviewedFileViewer.Instance.Open(screen);
    }

    private void CreateBagIcon()
    {
        bagIcon = HudTheme.CreateRect("BagIconButton", buttons);
        PlaceSlotRect(bagIcon, CampaignInventoryRecord.StartHotbar);

        RectTransform glow = HudTheme.CreateRect("Drop Glow", bagIcon);
        HudTheme.Stretch(glow, -10f);
        bagGlow = HudTheme.AddImage(glow, HudTheme.Rounded, new Color(HudTheme.BrassSoft.r, HudTheme.BrassSoft.g, HudTheme.BrassSoft.b, 0.55f), 26f);
        glow.gameObject.SetActive(false);

        RectTransform face = HudTheme.CreateRect("Face", bagIcon);
        HudTheme.Stretch(face);
        var image = face.gameObject.AddComponent<Image>();
        image.sprite = HudIconArt.Get("bag");
        image.preserveAspect = true;
        image.raycastTarget = true;
        var button = face.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        OfficeHudRuntime.StyleIconButton(button);
        button.onClick.AddListener(OnBagTapped);

        TextMeshProUGUI label = HudTheme.CreateLabel("Label", bagIcon, "BAG", 18f, HudTheme.TextPrimary, TextAlignmentOptions.Bottom, FontStyles.Bold);
        HudTheme.Stretch(label.rectTransform, 6f);
        label.rectTransform.offsetMin = new Vector2(6f, -20f);
        label.outlineWidth = 0.3f;
        label.outlineColor = new Color32(10, 16, 26, 255);

        plusOne = HudTheme.CreateLabel("Plus One", bagIcon, "+1", 30f, HudTheme.Exp, TextAlignmentOptions.Center, FontStyles.Bold);
        plusOne.rectTransform.anchorMin = plusOne.rectTransform.anchorMax = new Vector2(0f, 1f);
        plusOne.rectTransform.sizeDelta = new Vector2(80f, 44f);
        plusOne.rectTransform.anchoredPosition = new Vector2(-10f, 0f);
        plusOne.outlineWidth = 0.3f;
        plusOne.outlineColor = new Color32(10, 16, 26, 255);
        plusOne.gameObject.SetActive(false);
    }

    // =========================================================
    // LAYOUT
    // =========================================================

    private void MarkDirty() => dirty = true;

    private void LateUpdate()
    {
        if (dirty || Time.unscaledTime >= nextRefresh) Refresh();
        AnimateBag();
        AnimateViews();
        UpdateDropGlow();
    }

    /// <summary>Places every slot, item view and the Bag icon from the inventory snapshot.</summary>
    private void Refresh()
    {
        dirty = false;
        nextRefresh = Time.unscaledTime + 0.5f;
        if (buttons == null) return;
        CampaignInventoryRecord r = Inventory;
        bool awaiting = ClientCopyHandover.AwaitingHandoverAction;
        int capacity = Mathf.Clamp(r.hotbarCapacity, CampaignInventoryRecord.StartHotbar, CampaignInventoryRecord.MaxHotbar);

        for (int i = 0; i < tiles.Length; i++)
        {
            SlotTile tile = tiles[i];
            if (tile == null) continue;
            bool unlocked = i < capacity;
            if (tile.Rect.gameObject.activeSelf != unlocked) tile.Rect.gameObject.SetActive(unlocked);
            if (!unlocked) continue;
            tile.Rect.anchoredPosition = SlotPosition(i);
            bool empty = i >= r.hotbar.Count || r.hotbar[i].IsEmpty;
            tile.Face.enabled = empty;
            tile.Button.interactable = empty;
        }
        // A bought slot pops in and the Bag icon glides to its new place (T12).
        if (shownCapacity >= 0 && capacity > shownCapacity)
        {
            tilesUnlockedFrom = shownCapacity;
            tilesUnlockedAt = Time.unscaledTime;
        }
        shownCapacity = capacity;
        if (bagIcon != null && !bagPlaced)
        {
            bagShown = SlotPosition(capacity);
            bagIcon.anchoredPosition = bagShown;
            bagPlaced = true;
        }

        // During Marie's handover step the HUD gate owns the other icons; only the folder follows its slot.
        if (awaiting)
        {
            if (views.TryGetValue(InventoryCatalogue.CaseFolder, out ItemView folder)) Place(folder, r);
            return;
        }
        for (int i = 0; i < r.hotbar.Count && i < capacity; i++)
            if (!r.hotbar[i].IsEmpty && !views.ContainsKey(r.hotbar[i].itemId) && catalogue.Knows(r.hotbar[i].itemId))
                GenericView(r.hotbar[i].itemId);
        foreach (ItemView view in views.Values) Place(view, r);
    }

    private void Place(ItemView view, CampaignInventoryRecord r)
    {
        SlotRef at = InventoryRules.Find(r, view.ItemId);
        bool inBar = at.Area == InventoryArea.Hotbar && at.Index < r.hotbarCapacity;
        view.Wrapper.anchoredPosition = inBar ? SlotPosition(at.Index) : HiddenPosition(view.ItemId, r);
        if (inBar) lastSlot[view.ItemId] = at.Index;
        if (view.Generic && view.Count != null)
        {
            int quantity = inBar ? r.hotbar[at.Index].quantity : 0;
            view.Count.text = quantity > 1 ? "×" + quantity : "";
        }
        SetShown(view, inBar);
    }

    private static void SetShown(ItemView view, bool shown)
    {
        if (view.Group == null) return;
        view.Group.alpha = shown ? 1f : 0f;
        view.Group.blocksRaycasts = shown;
        view.Group.interactable = shown;
    }

    /// <summary>Where a hidden item waits: at the Bag icon when owned, else at the slot it would take.</summary>
    private Vector2 HiddenPosition(string itemId, CampaignInventoryRecord r)
    {
        int capacity = Mathf.Clamp(r.hotbarCapacity, CampaignInventoryRecord.StartHotbar, CampaignInventoryRecord.MaxHotbar);
        if (InventoryRules.Owns(r, itemId)) return SlotPosition(capacity);
        int landing = LandingIndex(itemId, r);
        return landing >= 0 ? SlotPosition(landing) : SlotPosition(capacity);
    }

    /// <summary>The slot a new item would take: its last slot when that is free, else the first free slot (-1: the bag).</summary>
    private int LandingIndex(string itemId, CampaignInventoryRecord r)
    {
        if (lastSlot.TryGetValue(itemId, out int last) && last < r.hotbarCapacity && last < r.hotbar.Count && r.hotbar[last].IsEmpty)
            return last;
        return InventoryRules.FirstEmpty(r.hotbar, r.hotbarCapacity);
    }

    // =========================================================
    // DROP TARGET (shared with world drags)
    // =========================================================

    private bool OverDropTarget(Vector2 screen)
    {
        if (!isActiveAndEnabled || buttons == null) return false;
        if (SlotIndexUnder(screen) >= 0 || OverRect(bagIcon, screen, DropPad)) return true;
        // Anywhere on the row: from the first slot to the Bag icon, padded sideways.
        if (tiles[0] == null || bagIcon == null) return false;
        Rect top = ScreenRect(tiles[0].Rect), bottom = ScreenRect(bagIcon);
        float padX = top.width * DropPad;
        Rect row = Rect.MinMaxRect(Mathf.Min(top.xMin, bottom.xMin) - padX, bottom.yMin, Mathf.Max(top.xMax, bottom.xMax) + padX, top.yMax);
        return row.Contains(screen);
    }

    private int SlotIndexUnder(Vector2 screen)
    {
        CampaignInventoryRecord r = Inventory;
        for (int i = 0; i < tiles.Length && i < r.hotbarCapacity; i++)
            if (tiles[i] != null && tiles[i].Rect.gameObject.activeInHierarchy && OverRect(tiles[i].Rect, screen, DropPad * 0.5f))
                return i;
        return -1;
    }

    private static bool OverRect(RectTransform rect, Vector2 screen, float pad)
    {
        if (rect == null) return false;
        Rect area = ScreenRect(rect);
        area.xMin -= area.width * pad; area.xMax += area.width * pad;
        area.yMin -= area.height * pad; area.yMax += area.height * pad;
        return area.Contains(screen);
    }

    private static Rect ScreenRect(RectTransform rect)
    {
        rect.GetWorldCorners(Corners);
        return Rect.MinMaxRect(Corners[0].x, Corners[0].y, Corners[2].x, Corners[2].y);
    }

    /// <summary>The slot (or the Bag icon) a world item released at screen will land in.</summary>
    private RectTransform LandingRect(string itemId, Vector2? screen)
    {
        CampaignInventoryRecord r = Inventory;
        int under = screen.HasValue ? SlotIndexUnder(screen.Value) : -1;
        if (under >= 0) return tiles[under].Rect;
        int landing = LandingIndex(itemId, r);
        return landing >= 0 && tiles[landing] != null ? tiles[landing].Rect : bagIcon;
    }

    /// <summary>The slot that lights up while a world item is carried over the row.</summary>
    private void UpdateDropGlow()
    {
        int lit = -1;
        bool bagLit = false;
        if (AnyWorldCarry() && TryPointer(out Vector2 pointer) && OverDropTarget(pointer))
        {
            lit = SlotIndexUnder(pointer);
            if (lit < 0)
            {
                lit = InventoryRules.FirstEmpty(Inventory.hotbar, Inventory.hotbarCapacity);
                bagLit = lit < 0;
            }
        }
        for (int i = 0; i < tiles.Length; i++)
            if (tiles[i] != null && tiles[i].Glow.gameObject.activeSelf != (i == lit)) tiles[i].Glow.gameObject.SetActive(i == lit);
        if (bagGlow != null && bagGlow.gameObject.activeSelf != bagLit) bagGlow.gameObject.SetActive(bagLit);
    }

    private bool AnyWorldCarry()
    {
        foreach (CaseFolder3DDrag drag in closedFolders) if (drag != null && drag.IsCarrying) return true;
        foreach (FloatingModel3D model in openFolders) if (model != null && model.isActiveAndEnabled && model.IsDragging()) return true;
        if (deskBook != null && deskBook.IsCarrying) return true;
        foreach (TaxCodeBookFloatingModel3D model in openBooks) if (model != null && model.isActiveAndEnabled && model.IsDragging()) return true;
        return false;
    }

    private static bool TryPointer(out Vector2 pointer)
    {
        if (Input.touchCount > 0)
        {
            pointer = Input.GetTouch(0).position;
            return true;
        }
        pointer = Input.mousePosition;
        return Input.GetMouseButton(0);
    }

    // =========================================================
    // WORLD SYNC
    // =========================================================

    private void Update()
    {
        if (!ready)
        {
            TryBecomeReady();
            return;
        }
        SyncWorld();
        UpdateHandoverPrompt();
        UpdateBookFirstOpen();
    }

    /// <summary>After the Level 1 resume finished: apply the catalogue, then make the inventory agree with the world (quietly).</summary>
    private void TryBecomeReady()
    {
        Level1FieldCheckpointer checkpointer = Level1FieldCheckpointer.Active;
        bool levelStarted = CaseProgressionManager.Instance == null || CaseProgressionManager.Instance.LevelStarted;
        if (Time.unscaledTime < readyDeadline && ((checkpointer != null && !checkpointer.IsReady) || !levelStarted)) return;

        CacheWorld();
        if (Manager != null) Manager.NormalizeInventory(catalogue);
        Reconcile();
        ready = true;
        SetNotesAvailable(NotesGranted());
        dirty = true;
    }

    private void CacheWorld()
    {
        Scene scene = gameObject.scene;
        var folderList = new List<FolderToHUDIcon>();
        var closedList = new List<CaseFolder3DDrag>();
        var openList = new List<FloatingModel3D>();
        foreach (FolderToHUDIcon folder in FindObjectsByType<FolderToHUDIcon>(FindObjectsInactive.Include))
        {
            if (folder.gameObject.scene != scene) continue;
            folderList.Add(folder);
            FloatingModel3D model = folder.GetComponent<FloatingModel3D>();
            if (model != null) openList.Add(model);
        }
        foreach (CaseFolder3DDrag drag in FindObjectsByType<CaseFolder3DDrag>(FindObjectsInactive.Include))
            if (drag.gameObject.scene == scene) closedList.Add(drag);
        folders = folderList.ToArray();
        closedFolders = closedList.ToArray();
        openFolders = openList.ToArray();
        var books = new List<TaxCodeBookFloatingModel3D>();
        foreach (TaxCodeBookFloatingModel3D model in FindObjectsByType<TaxCodeBookFloatingModel3D>(FindObjectsInactive.Include))
            if (model.gameObject.scene == scene) books.Add(model);
        openBooks = books.ToArray();
        foreach (TaxCodeBook3DDrag drag in FindObjectsByType<TaxCodeBook3DDrag>(FindObjectsInactive.Include))
            if (drag.gameObject.scene == scene) { deskBook = drag; break; }
    }

    /// <summary>
    /// A fresh run of the level (no saved run at its start) drops the level
    /// items an abandoned attempt left behind; then the world-owned items
    /// follow the world. Quiet: no bounce, no lesson, no sound.
    /// </summary>
    private void Reconcile()
    {
        CampaignInventoryRecord r = Inventory;
        string levelId = OfficeLevelCatalog.Level1Id;
        bool bookDropped = false;
        if (!resumedRun)
            foreach (InventoryItemDefinition def in catalogue.items)
            {
                // Fresh/restarted attempts recollect Arthur's reference too. Continue never enters this reset.
                // A Notes left in the bag is not proof the new run's world gave it.
                if (def == null || (def.scope != levelId && def.id != InventoryCatalogue.ReviewedFile) ||
                    !InventoryRules.Owns(r, def.id) || WorldOwns(def.id, trustInventory: false) == true) continue;
                Commit(rec => InventoryRules.Remove(rec, catalogue, def.id, NewAction("reset/" + def.id)));
                bookDropped |= def.id == InventoryCatalogue.TaxCodeBook;
            }
        foreach (string id in new[] { InventoryCatalogue.ProfileBinder, InventoryCatalogue.Notes, InventoryCatalogue.CaseFolder })
        {
            bool? world = WorldOwns(id);
            if (!world.HasValue) continue;
            bool owned = InventoryRules.Owns(Inventory, id);
            if (world.Value && !owned) AcquireWorld(id, null, Info(id), quiet: true);
            else if (!world.Value && owned) RemoveWorld(id);
        }
        SyncContents();
        if (bookDropped && deskBook != null) deskBook.UnparkClosedBook();
    }

    /// <summary>What the world says about an item: carried (true), not carried (false) or not the world's to say (null: the book).</summary>
    private bool? WorldOwns(string itemId, bool trustInventory = true)
    {
        switch (itemId)
        {
            case InventoryCatalogue.ProfileBinder:
                // 2026-10-27: once the IDs are on the corkboard (or the side quest is done) they are gone from the bar and Bag.
                return TaxpayerProfileCollectionHUD.Instance != null && TaxpayerProfileCollectionHUD.Instance.CollectedProfiles.Count > 0 &&
                       !IdsRetired;
            case InventoryCatalogue.Notes:
                return NotesGranted(trustInventory);
            case InventoryCatalogue.CaseFolder:
                return FolderCarried();
            default:
                return null;
        }
    }

    /// <summary>
    /// The world has given Notes (the chest or the receptionist). trustInventory:
    /// Notes already in the bag counts too (during a run: the chest grant lives
    /// in the inventory); a fresh run's reset passes false so a replay starts
    /// without the previous attempt's Notes.
    /// </summary>
    private bool NotesGranted(bool trustInventory = true)
    {
        if (trustInventory && InventoryRules.Owns(Inventory, InventoryCatalogue.Notes)) return true;
        if (ClientCopyHandover.Instance != null && ClientCopyHandover.Instance.Phase != ClientCopyPhase.None) return true;
        string caseId = CaseManager.Instance != null && CaseManager.Instance.CurrentDefinition != null ? CaseManager.Instance.CurrentDefinition.caseId : null;
        ReceptionistInteractable receptionist = FindAnyObjectByType<ReceptionistInteractable>(FindObjectsInactive.Include);
        return receptionist != null && receptionist.HasSpokenFor(caseId);
    }

    private static bool ReceptionistHasReceivedCase()
    {
        string caseId = CaseManager.Instance != null && CaseManager.Instance.CurrentDefinition != null
            ? CaseManager.Instance.CurrentDefinition.caseId : null;
        ReceptionistInteractable receptionist = FindAnyObjectByType<ReceptionistInteractable>(FindObjectsInactive.Include);
        return receptionist != null && receptionist.HasSpokenFor(caseId);
    }

    private bool FolderCarried()
    {
        if (ClientCopyHandover.CopyActive) return true;
        foreach (FolderToHUDIcon folder in folders)
            if (folder != null && folder.IsStoredOrViewing) return true;
        return false;
    }

    /// <summary>The clients' IDs went onto the corkboard for good (or the side quest is done): no Client IDs item any more.</summary>
    private static bool IdsRetired => CorkboardSideQuestManager.IdsOnBoard || LevelGate.SideQuestDone;

    /// <summary>Per frame: profiles collected, the Case Folder stored or returned (Notes arrive by event).</summary>
    private void SyncWorld()
    {
        CampaignInventoryRecord r = Inventory;
        if (WorldOwns(InventoryCatalogue.ProfileBinder) == true && !InventoryRules.Owns(r, InventoryCatalogue.ProfileBinder))
            AcquireWorld(InventoryCatalogue.ProfileBinder, null, Info(InventoryCatalogue.ProfileBinder), quiet: false);
        else if (IdsRetired && InventoryRules.Owns(r, InventoryCatalogue.ProfileBinder))
            RemoveWorld(InventoryCatalogue.ProfileBinder);

        bool folderCarried = FolderCarried();
        bool folderOwned = InventoryRules.Owns(r, InventoryCatalogue.CaseFolder);
        if (folderCarried && !folderOwned)
        {
            int dropped = HudSlotDrop.TakePending(InventoryCatalogue.CaseFolder);
            SlotRef? preferred = dropped >= 0 ? SlotRef.Hotbar(dropped) : (SlotRef?)null;
            AcquireWorld(InventoryCatalogue.CaseFolder, preferred, Info(InventoryCatalogue.CaseFolder), quiet: false);
        }
        else if (!folderCarried && folderOwned) RemoveWorld(InventoryCatalogue.CaseFolder);
        SyncContents();
    }

    /// <summary>Containers list what they hold (the binder's profiles; the folder's papers and Client Copy). Only when something changed.</summary>
    private void SyncContents()
    {
        CampaignInventoryRecord r = Inventory;
        int profiles = TaxpayerProfileCollectionHUD.Instance != null ? TaxpayerProfileCollectionHUD.Instance.CollectedProfiles.Count : 0;
        bool copy = ClientCopyHandover.CopyActive;
        bool folder = InventoryRules.Owns(r, InventoryCatalogue.CaseFolder);
        if (profiles == syncedProfiles && copy == syncedCopy && folder == syncedFolder) return;
        syncedProfiles = profiles;
        syncedCopy = copy;
        syncedFolder = folder;
        if (InventoryRules.Owns(r, InventoryCatalogue.ProfileBinder) && TaxpayerProfileCollectionHUD.Instance != null)
        {
            var ids = new List<string>(InventoryRules.Record(r, InventoryCatalogue.ProfileBinder)?.contents ?? new List<string>());
            foreach (TaxpayerProfile profile in TaxpayerProfileCollectionHUD.Instance.CollectedProfiles)
                if (profile != null && !string.IsNullOrEmpty(profile.ProfileId) && !ids.Contains(profile.ProfileId)) ids.Add(profile.ProfileId);
            if (!SameContents(r, InventoryCatalogue.ProfileBinder, ids))
                Commit(rec => InventoryRules.SetContents(rec, InventoryCatalogue.ProfileBinder, ids));
        }
        if (InventoryRules.Owns(r, InventoryCatalogue.CaseFolder))
        {
            List<string> papers = FolderContents();
            if (!SameContents(r, InventoryCatalogue.CaseFolder, papers))
                Commit(rec => InventoryRules.SetContents(rec, InventoryCatalogue.CaseFolder, papers));
        }
    }

    private static bool SameContents(CampaignInventoryRecord r, string itemId, List<string> ids)
    {
        InventoryItemRecord record = InventoryRules.Record(r, itemId);
        if (record == null) return true;
        if (record.contents.Count != ids.Count) return false;
        for (int i = 0; i < ids.Count; i++) if (record.contents[i] != ids[i]) return false;
        return true;
    }

    private static string CurrentCaseId() =>
        CaseManager.Instance != null && CaseManager.Instance.CurrentDefinition != null ? CaseManager.Instance.CurrentDefinition.caseId : "case";

    private static List<string> FolderContents()
    {
        string caseId = CurrentCaseId();
        var papers = new List<string> { caseId + "/papers" };
        if (ClientCopyHandover.CopyActive) papers.Add(caseId + "/client-copy");
        return papers;
    }

    /// <summary>The acquisition facts saved with an item (never an answer).</summary>
    private static InventoryItemRecord Info(string itemId)
    {
        switch (itemId)
        {
            case InventoryCatalogue.CaseFolder:
            {
                string caseId = CurrentCaseId();
                return new InventoryItemRecord
                {
                    itemId = itemId,
                    source = ClientCopyHandover.CopyActive ? "auditor" : "desk",
                    originalDocumentId = caseId + "/original",
                    workingDocumentId = caseId + "/working",
                    contents = FolderContents(),
                    factIds = new List<string> { "case-received" }
                };
            }
            case InventoryCatalogue.Notes:
                return new InventoryItemRecord { itemId = itemId, source = "receptionist" };
            case InventoryCatalogue.ProfileBinder:
                return new InventoryItemRecord { itemId = itemId, source = "plaza" };
            case InventoryCatalogue.TaxCodeBook:
                return new InventoryItemRecord { itemId = itemId, source = "desk" };
            default:
                return new InventoryItemRecord { itemId = itemId };
        }
    }

    private void HandleNotesGranted()
    {
        if (!ready) return;
        InventoryResult result = AcquireWorld(InventoryCatalogue.Notes, null, Info(InventoryCatalogue.Notes), quiet: false);
        if (result != null && result.Ok) SetNotesAvailable(true);
    }

    /// <summary>
    /// T16: the chest's pickup claim and Notes item are committed together;
    /// presentation unlocks only after PlayerProgressManager publishes success.
    /// The same command uses the manager's temporary claim/inventory for replay,
    /// practice and direct Editor runs.
    /// </summary>
    public InventoryResult CollectNotesFromChest()
    {
        if (!ready) return InventoryResult.Reject("The inventory is still loading");
        if (InventoryRules.Owns(Inventory, InventoryCatalogue.Notes))
        {
            SetNotesAvailable(true);
            return new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = InventoryCatalogue.Notes };
        }

        InventoryResult result;
        if (Manager != null)
        {
            string claimId = Manager.PickupClaimId(OfficeLevelCatalog.Level1Id, "notes-chest");
            // Not once per campaign: Notes is a Level 1 item every fresh run (replay) takes away again, so the
            // chest must give it again; an owned Notes is a no-change, so it is never granted twice.
            result = Manager.CommitPickup(claimId, onceOnly: false, inventory =>
                InventoryRules.Acquire(inventory, catalogue, InventoryCatalogue.Notes, 1,
                    NewAction("acquire/notes-chest"), null,
                    new InventoryItemRecord { itemId = InventoryCatalogue.Notes, source = "chest" }));
        }
        else
        {
            if (fallbackNotesChestClaimed)
                return new InventoryResult { Outcome = InventoryOutcome.AlreadyApplied, ItemId = InventoryCatalogue.Notes };
            result = Commit(inventory => InventoryRules.Acquire(inventory, catalogue, InventoryCatalogue.Notes, 1,
                NewAction("acquire/notes-chest"), null,
                new InventoryItemRecord { itemId = InventoryCatalogue.Notes, source = "chest" }));
            if (result != null && result.Changed) fallbackNotesChestClaimed = true;
        }

        if (result != null && result.Changed)
        {
            Refresh();
            SetNotesAvailable(true);
            BounceBag();
            if (result.Outcome == InventoryOutcome.NeedsRoom && screen != null) screen.OpenMakeRoom();
        }
        return result;
    }

    /// <summary>
    /// Arthur's handover saves one required reference before its pickup flies. Continue keeps it;
    /// a fresh/restarted attempt recollects it, like Notes, even if an earlier attempt claimed it.
    /// Ownership makes repeated taps a no-change; a full inventory uses the existing Make room path.
    /// </summary>
    public InventoryResult CollectReviewedFile()
    {
        if (!ready) return InventoryResult.Reject("The inventory is still loading");
        string itemId = InventoryCatalogue.ReviewedFile;
        if (InventoryRules.Owns(Inventory, itemId)) return new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = itemId };

        string action = NewAction("acquire/reviewed-file");
        var info = new InventoryItemRecord { itemId = itemId, source = "arthur" };
        Func<CampaignInventoryRecord, InventoryResult> acquire = r => InventoryRules.Acquire(r, catalogue, itemId, 1, action, null, info);
        InventoryResult result = Manager != null
            ? Manager.CommitPickup(Manager.PickupClaimId(OfficeLevelCatalog.Level1Id, ReviewedFileRules.PickupId), false, acquire)
            : Commit(acquire);
        if (result != null && result.Changed)
        {
            dirty = true;
            Refresh();
            BounceBag();
            if (result.Outcome == InventoryOutcome.NeedsRoom && screen != null) screen.OpenMakeRoom();
        }
        return result;
    }

    private void SetNotesAvailable(bool available)
    {
        if (views.TryGetValue(InventoryCatalogue.Notes, out ItemView notes) && notes.Icon != null)
        {
            HudIconButton icon = notes.Icon.GetComponent<HudIconButton>();
            if (icon == null) icon = notes.Icon.GetComponentInChildren<HudIconButton>(true);
            if (icon != null) icon.SetAvailable(available);
        }

        OfficeHudRuntime runtime = FindAnyObjectByType<OfficeHudRuntime>(FindObjectsInactive.Include);
        if (runtime != null && runtime.gameObject.scene == gameObject.scene && runtime.Objectives != null)
            runtime.Objectives.SetNotesAvailable(available);
    }

    // =========================================================
    // COMMANDS
    // =========================================================

    public static string NewAction(string what) => "hotbar/" + what + "/" + Guid.NewGuid().ToString("N");

    /// <summary>Runs one inventory command through the save owner (a temporary inventory without one).</summary>
    public InventoryResult Commit(Func<CampaignInventoryRecord, InventoryResult> command)
    {
        InventoryResult result;
        if (Manager != null) result = Manager.CommitInventory(command);
        else
        {
            result = command(fallback);
        }
        if (result.Changed) dirty = true;
        return result;
    }

    /// <summary>
    /// A world item arrives (quiet during a resume). Success bounces the Bag
    /// icon with "+1"; a full inventory opens "Make room"; the first time an
    /// item lands in the bag because the bar is full, the bag lesson cue is
    /// raised (once per campaign). A failed write is retried a little later.
    /// </summary>
    private InventoryResult AcquireWorld(string itemId, SlotRef? preferred, InventoryItemRecord info, bool quiet)
    {
        if (retryAfter.TryGetValue(itemId, out float until) && Time.unscaledTime < until)
            return new InventoryResult { Outcome = InventoryOutcome.SaveFailed, ItemId = itemId };
        bool lesson = false, iconKnown = false;
        string action = NewAction("acquire/" + itemId);
        InventoryResult result = Commit(r =>
        {
            InventoryResult res = InventoryRules.Acquire(r, catalogue, itemId, 1, action, preferred, info);
            if (!quiet && res.WentToBagBecauseBarFull && !r.bagLessonShown)
            {
                r.bagLessonShown = true;
                lesson = true;
                iconKnown = r.bagIconUsed;
            }
            return res;
        });
        if (result.Outcome == InventoryOutcome.SaveFailed)
        {
            retryAfter[itemId] = Time.unscaledTime + RetrySeconds;
            return result;
        }
        retryAfter.Remove(itemId);
        if (!result.Changed) return result;
        if (!quiet) BounceBag();
        if (result.Outcome == InventoryOutcome.NeedsRoom && screen != null) screen.OpenMakeRoom();
        if (lesson) GameplayEvents.RaiseGuideCue(iconKnown ? CueOverflowKnown : CueOverflow);
        return result;
    }

    private void RemoveWorld(string itemId)
    {
        if (retryAfter.TryGetValue(itemId, out float until) && Time.unscaledTime < until) return;
        SlotRef was = InventoryRules.Find(Inventory, itemId);
        if (was.Area == InventoryArea.Hotbar) lastSlot[itemId] = was.Index;
        InventoryResult result = Commit(r => InventoryRules.Remove(r, catalogue, itemId, NewAction("remove/" + itemId)));
        if (result.Outcome == InventoryOutcome.SaveFailed) retryAfter[itemId] = Time.unscaledTime + RetrySeconds;
        else retryAfter.Remove(itemId);
    }

    // =========================================================
    // T11: PROFILE CARDS, WORLD PICKUPS, DISPLAY
    // =========================================================

    /// <summary>
    /// TaxpayerProfileWorldPickup gate: before a tapped card flies, the
    /// Profile Binder (acquired if needed) records the profile id and its
    /// claim in one save. A card the binder already holds just flies (no new
    /// copy). False only when the save failed (the card stays to retry).
    /// </summary>
    private bool CommitProfile(TaxpayerProfile profile)
    {
        if (profile == null || string.IsNullOrEmpty(profile.ProfileId) || Manager == null) return true;
        string id = profile.ProfileId;
        CampaignInventoryRecord current = Inventory;
        InventoryItemRecord held = InventoryRules.Record(current, InventoryCatalogue.ProfileBinder);
        if (held != null && held.contents.Contains(id) && InventoryRules.Owns(current, InventoryCatalogue.ProfileBinder)) return true;

        bool lesson = false, iconKnown = false, binderNew = false;
        string action = NewAction("profile/" + id);
        InventoryResult result = Manager.CommitPickup(Manager.ProfileClaimId(OfficeLevelCatalog.Level1Id, id), false, r =>
        {
            InventoryResult acquired = new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = InventoryCatalogue.ProfileBinder };
            if (!InventoryRules.Owns(r, InventoryCatalogue.ProfileBinder))
            {
                acquired = InventoryRules.Acquire(r, catalogue, InventoryCatalogue.ProfileBinder, 1, action, null, Info(InventoryCatalogue.ProfileBinder));
                if (!acquired.Changed) return acquired;
                binderNew = true;
                if (acquired.WentToBagBecauseBarFull && !r.bagLessonShown)
                {
                    r.bagLessonShown = true;
                    lesson = true;
                    iconKnown = r.bagIconUsed;
                }
            }
            var contents = new List<string>(InventoryRules.Record(r, InventoryCatalogue.ProfileBinder)?.contents ?? new List<string>());
            if (!contents.Contains(id)) contents.Add(id);
            InventoryResult filled = InventoryRules.SetContents(r, InventoryCatalogue.ProfileBinder, contents);
            return acquired.Changed ? acquired : filled;
        });
        if (result.Outcome == InventoryOutcome.SaveFailed || result.Outcome == InventoryOutcome.Rejected)
        {
            Notice(result.Outcome == InventoryOutcome.SaveFailed
                ? "That card could not be saved just now. Tap it again."
                : "Make room in your bag first, then tap the card again.");
            return false;
        }
        dirty = true;
        syncedProfiles = -1;
        if (binderNew) BounceBag();
        if (result.Outcome == InventoryOutcome.NeedsRoom && screen != null) screen.OpenMakeRoom();
        if (lesson) GameplayEvents.RaiseGuideCue(iconKnown ? CueOverflowKnown : CueOverflow);
        return true;
    }

    /// <summary>
    /// An optional world pickup (postcard, voucher): the item and its
    /// once-per-campaign claim in one save. Past the voucher carry limit it
    /// is refused and stays in the world (unclaimed). Success bounces the Bag;
    /// a full bar and bag open Make room. No coins, trust, XP or badge.
    /// </summary>
    public InventoryResult CollectPickup(PickupDefinition pickup)
    {
        if (pickup == null || Manager == null) return InventoryResult.Reject("No pickup");
        bool lesson = false, iconKnown = false;
        string action = NewAction("pickup/" + pickup.id);
        InventoryResult result = Manager.CommitPickup(Manager.PickupClaimId(pickup.levelId, pickup.id), true, r =>
        {
            InventoryResult res = InventoryRules.Acquire(r, catalogue, pickup.itemId, Math.Max(1, pickup.quantity), action, null,
                new InventoryItemRecord { itemId = pickup.itemId, source = "pickup:" + pickup.id });
            if (res.WentToBagBecauseBarFull && !r.bagLessonShown)
            {
                r.bagLessonShown = true;
                lesson = true;
                iconKnown = r.bagIconUsed;
            }
            return res;
        });
        if (!result.Changed) return result;
        dirty = true;
        BounceBag();
        if (result.Outcome == InventoryOutcome.NeedsRoom && screen != null) screen.OpenMakeRoom();
        if (lesson) GameplayEvents.RaiseGuideCue(iconKnown ? CueOverflowKnown : CueOverflow);
        return result;
    }

    /// <summary>How many of a pickup are past the carry limit (vouchers: two), for the notice before collecting.</summary>
    public int OverCarryLimit(PickupDefinition pickup) =>
        pickup == null ? 0 : InventoryRules.OverCarryLimit(Inventory, catalogue, pickup.itemId, Math.Max(1, pickup.quantity));

    public InventoryResult DisplayKeepsake(string itemId, string anchorId) =>
        Commit(r => InventoryRules.Display(r, catalogue, itemId, anchorId, NewAction("display/" + anchorId)));

    public InventoryResult TakeDownKeepsake(string anchorId) =>
        Commit(r => InventoryRules.TakeDown(r, catalogue, anchorId, NewAction("takedown/" + anchorId)));

    /// <summary>A bought item reached the bar or Bag (the catalogue's flight landed): the Bag bounces with "+1".</summary>
    public void CelebrateArrival()
    {
        dirty = true;
        BounceBag();
    }

    /// <summary>A short notice beside the bar (no button).</summary>
    public void Notice(string text)
    {
        if (prompt != null) prompt.Show(null, text, duringDialogue: true, showButton: false);
    }

    /// <summary>Observers of a committed move (the inventory screen): the bag lesson completes on a real bag-to-bar move.</summary>
    public void NoteResult(InventoryResult result)
    {
        if (result == null || !result.Changed) return;
        if (result.MovedBagToHotbar) GameplayEvents.RaiseGuideCue(CueToBar);
        dirty = true;
    }

    /// <summary>A "Make room" item went back to its world source (the desk book returns to its stand).</summary>
    public void ReturnedToWorld(string itemId)
    {
        if (itemId == InventoryCatalogue.TaxCodeBook && deskBook != null) deskBook.UnparkClosedBook();
    }

    // =========================================================
    // TAPS, BAG ICON, PROMPTS
    // =========================================================

    private void OnBagTapped()
    {
        if (!Inventory.bagIconUsed) Commit(r => { r.bagIconUsed = true; return new InventoryResult { Outcome = InventoryOutcome.Applied }; });
        OpenInventory(SlotRef.None, null);
    }

    private void OnEmptySlotTapped(int index) => OpenInventory(SlotRef.Hotbar(index), null);

    public void OpenInventory(SlotRef target, string selectItem)
    {
        if (screen == null || !CanOpenPanels()) return;
        screen.Open(target, selectItem);
    }

    /// <summary>Panels open only in calm moments: no dialogue, result card, settings, splash or blocking lesson.</summary>
    public static bool CanOpenPanels()
    {
        GuideDirector guide = GuideRuntime.Instance != null ? GuideRuntime.Instance.Director : null;
        return !DialogueUI.IsAnyDialogueActive && !HudRewardScreen.AnyOpen && !InGameSettings.IsOpen && !SideQuestSplash.IsShowing &&
               (guide == null || !guide.IsBlocking) && (CaseResultPopupUI.Instance == null || !CaseResultPopupUI.Instance.IsVisible);
    }

    private void BounceBag()
    {
        bagPunchStarted = Time.unscaledTime;
        if (plusOne != null) plusOne.gameObject.SetActive(true);
    }

    private void AnimateBag()
    {
        if (bagIcon == null) return;
        Vector2 bagTarget = SlotPosition(Mathf.Clamp(Inventory.hotbarCapacity, CampaignInventoryRecord.StartHotbar, CampaignInventoryRecord.MaxHotbar));
        bagShown = Vector2.Lerp(bagShown, bagTarget, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
        bagIcon.anchoredPosition = bagShown;
        if (tilesUnlockedFrom >= 0)
        {
            float u = Time.unscaledTime - tilesUnlockedAt;
            for (int i = tilesUnlockedFrom; i < tiles.Length; i++)
            {
                if (tiles[i] == null) continue;
                float local = Mathf.Clamp01((u - (i - tilesUnlockedFrom) * 0.08f) / 0.35f);
                tiles[i].Rect.localScale = Vector3.one * HudTheme.EaseOutBack(local);
            }
            if (u > 0.6f) tilesUnlockedFrom = -1;
        }
        float t = Time.unscaledTime - bagPunchStarted;
        float scale = t < 0.45f ? 1f + 0.22f * Mathf.Sin(t / 0.45f * Mathf.PI) * (1f - t / 0.45f) : 1f;
        bagIcon.localScale = Vector3.one * scale;
        if (plusOne == null || !plusOne.gameObject.activeSelf) return;
        if (t > 0.9f)
        {
            plusOne.gameObject.SetActive(false);
            return;
        }
        plusOne.rectTransform.anchoredPosition = new Vector2(-10f, 10f + 46f * (t / 0.9f));
        plusOne.alpha = 1f - Mathf.Clamp01((t - 0.45f) / 0.45f);
    }

    /// <summary>The Auditor asks for the folder while it is only in the bag: one "Take it out" button instead of the reminder.</summary>
    private bool AuditorBagReminder()
    {
        CampaignInventoryRecord r = Inventory;
        if (!InventoryRules.Owns(r, InventoryCatalogue.CaseFolder) || InventoryRules.InHotbar(r, InventoryCatalogue.CaseFolder)) return false;
        return prompt != null && prompt.Show(InventoryCatalogue.CaseFolder, "Your Case Folder is in your bag.");
    }

    /// <summary>Marie waits for her copy while the folder is only in the bag: the prompt appears beside the bar.</summary>
    private void UpdateHandoverPrompt()
    {
        bool awaiting = ClientCopyHandover.AwaitingHandoverAction;
        if (awaiting != wasAwaiting)
        {
            wasAwaiting = awaiting;
            dirty = true;
            if (!awaiting && prompt != null) prompt.HideFor(InventoryCatalogue.CaseFolder);
        }
        if (!awaiting || prompt == null || prompt.Showing) return;
        CampaignInventoryRecord r = Inventory;
        if (!InventoryRules.Owns(r, InventoryCatalogue.CaseFolder) || InventoryRules.InHotbar(r, InventoryCatalogue.CaseFolder)) return;
        // The Bag icon is hidden during this step, so a closed prompt comes back shortly: never stuck.
        if (Time.unscaledTime - prompt.HiddenAt < 1.5f) return;
        prompt.Show(InventoryCatalogue.CaseFolder, ClientCopyHandover.AwaitingClientCopy
            ? "Your Case Folder is in your bag. Marie's copy is inside." : "Your Case Folder is in your bag.", duringDialogue: true);
    }

    /// <summary>"Take it out" (prompt button): the item swaps into the bar in one tap.</summary>
    public InventoryResult TakeOut(string itemId)
    {
        InventoryResult result = Commit(r => InventoryRules.TakeOut(r, catalogue, itemId, NewAction("takeout/" + itemId)));
        if (result.Changed)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_paper_pickup_01");
            NoteResult(result);
        }
        return result;
    }

    // =========================================================
    // TAX CODE BOOK (carrier for Level2TaxCodeBookIcon)
    // =========================================================

    public bool Carried => InventoryRules.Owns(Inventory, InventoryCatalogue.TaxCodeBook);

    public bool IsOverStoreTarget(Vector2 screen) => ready && OverDropTarget(screen);

    public RectTransform LandingFor(Vector2 screen) => LandingRect(InventoryCatalogue.TaxCodeBook, screen);

    public void SetCarried(bool carried, Vector2? dropScreen)
    {
        if (carried)
        {
            int under = dropScreen.HasValue ? SlotIndexUnder(dropScreen.Value) : -1;
            InventoryResult result = AcquireWorld(InventoryCatalogue.TaxCodeBook, under >= 0 ? SlotRef.Hotbar(under) : (SlotRef?)null,
                Info(InventoryCatalogue.TaxCodeBook), quiet: false);
            // Never lose the book: a refused or unsaved pickup leaves it on the desk.
            if (!result.Ok && result.Outcome != InventoryOutcome.NeedsRoom && deskBook != null) deskBook.UnparkClosedBook();
            return;
        }
        retryAfter.Remove(InventoryCatalogue.TaxCodeBook);
        RemoveWorld(InventoryCatalogue.TaxCodeBook);
    }

    public void Say(string line)
    {
        // The slot landing and the Bag bounce are the feedback in Level 1.
    }

    /// <summary>Opens a carried item's viewer (read-only reading is always allowed, from the bar or the bag).</summary>
    public bool OpenViewer(string itemId)
    {
        switch (itemId)
        {
            case InventoryCatalogue.Notes:
                foreach (NotesPanelUI notes in FindObjectsByType<NotesPanelUI>(FindObjectsInactive.Include))
                    if (notes.gameObject.scene == gameObject.scene) { if (!notes.IsOpen) notes.Show(); return true; }
                return false;
            case InventoryCatalogue.ProfileBinder:
                if (TaxpayerProfileCollectionHUD.Instance == null) return false;
                TaxpayerProfileCollectionHUD.Instance.OnHudIconClicked();
                return true;
            case InventoryCatalogue.CaseFolder:
                return OfficeHudRuntime.Instance != null && OfficeHudRuntime.Instance.OpenCaseFolderView();
            case InventoryCatalogue.TaxCodeBook:
                if (book == null || book.IsFloating) return false;
                book.Toggle();
                return true;
            case InventoryCatalogue.ReviewedFile:
                // T30: Arthur's file is a teal copy of the Case Folder: it flies out of its slot or the Bag (a second tap puts it away).
                if (ReviewedFileViewer.Instance == null) return false;
                ReviewedFileViewer.Instance.Toggle();
                return true;
            case InventoryCatalogue.ReviewVoucher:
                // T14: the voucher opens its own card (status, USE ON THIS CASE, the new cutoff).
                if (voucherCard == null) return collectibles != null && collectibles.ShowRead(itemId);
                voucherCard.Open();
                return true;
            default:
                // T11: the postcard is read on its printed inspect card.
                return collectibles != null && collectibles.ShowRead(itemId);
        }
    }

    /// <summary>The first time the book's pages show in this run (desk or slot), its first-open event is raised once.</summary>
    private void UpdateBookFirstOpen()
    {
        bool showing = deskBook != null && deskBook.BookUI != null && deskBook.BookUI.IsShowing;
        if (showing && !bookWasShowing && !BookOpenedOnce)
        {
            BookOpenedOnce = true;
            GameplayEvents.RaiseTaxCodeBookFirstOpened();
        }
        bookWasShowing = showing;
    }

    /// <summary>Resume (Level 1 checkpoint): the book was opened before; its first-open event is not raised again.</summary>
    public static void RestoreBookOpened() => BookOpenedOnce = true;

    // =========================================================
    // GUIDE AND FLIGHT TARGETS
    // =========================================================

    /// <summary>Profiles fly to the binder's slot, the slot it will take, or the Bag icon.</summary>
    private Vector2? BinderLanding()
    {
        RectTransform target = ItemTarget(InventoryCatalogue.ProfileBinder);
        if (target == null) return null;
        return RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center));
    }

    /// <summary>What the guide points at for an item: its icon in the bar, else the Bag icon (owned) or the slot it will take.</summary>
    public RectTransform ItemTarget(string itemId)
    {
        CampaignInventoryRecord r = Inventory;
        if (InventoryRules.InHotbar(r, itemId) && views.TryGetValue(itemId, out ItemView view)) return view.Icon;
        if (InventoryRules.Owns(r, itemId)) return bagIcon;
        return LandingRect(itemId, null);
    }

    /// <summary>The first empty slot (or the Bag icon when the bar is full): where a carried world item can be dropped.</summary>
    public RectTransform LandingSlot => LandingRect("", null);

    /// <summary>The Notes icon's live landing point for the chest paper flight.</summary>
    public bool TryGetNotesLandingScreenPoint(out Vector2 point)
    {
        point = Vector2.zero;
        if (buttons == null) return false;
        Refresh();
        RectTransform target = ItemTarget(InventoryCatalogue.Notes);
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        Canvas canvas = target.GetComponentInParent<Canvas>();
        if (canvas != null) canvas = canvas.rootCanvas;
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        point = RectTransformUtility.WorldToScreenPoint(camera, target.TransformPoint(target.rect.center));
        return true;
    }

    /// <summary>
    /// Neutral facts learned so far, for the evidence journal: taxpayers met
    /// whose profile card was taken, the case handed over, the Client Copy
    /// received, plus saved story facts. Reading the journal never adds one.
    /// </summary>
    public HashSet<string> LearnedFacts()
    {
        var learned = new HashSet<string>(StringComparer.Ordinal);
        PlayerProgressData data = Manager != null ? Manager.CurrentProgress : null;
        if (data != null && data.storyFacts != null)
            foreach (string fact in data.storyFacts) if (!string.IsNullOrEmpty(fact)) learned.Add(fact);
        var collected = new HashSet<string>(StringComparer.Ordinal);
        if (TaxpayerProfileCollectionHUD.Instance != null)
            foreach (TaxpayerProfile profile in TaxpayerProfileCollectionHUD.Instance.CollectedProfiles)
                if (profile != null && !string.IsNullOrEmpty(profile.NpcId)) collected.Add(profile.NpcId);
        foreach (TaxpayerNpcInteractable npc in FindObjectsByType<TaxpayerNpcInteractable>(FindObjectsInactive.Include))
            if (npc.gameObject.scene == gameObject.scene && npc.HasBeenTalkedTo && collected.Contains(npc.NpcId)) learned.Add("met-" + npc.NpcId);
        if (ReceptionistHasReceivedCase()) learned.Add("case-received");
        ClientCopyPhase phase = ClientCopyHandover.Instance != null ? ClientCopyHandover.Instance.Phase : ClientCopyPhase.None;
        if (phase == ClientCopyPhase.Carried || phase == ClientCopyPhase.Done) learned.Add("client-copy");
        return learned;
    }
}
