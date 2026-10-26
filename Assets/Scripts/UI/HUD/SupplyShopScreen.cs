using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The shop catalogue (Campaign Foundation T12/T13): an open leather-bound
/// book drawn in the HUD's language (cream paper, ink, brass, navy). A
/// leather segmented tab turns the left page between SUPPLIES (bookmarks,
/// Satchel, Belt Pouch and their locked second tiers) and OFFICE (the pencil
/// cup, the island print and the locked comparison stand and computer). The
/// left page is a two-column grid of cards: picture, name, and a price pill,
/// an OWNED stamp, or a padlock with its requirement. The right page shows
/// the chosen one large on a lit stage - office decor as a live miniature
/// that slowly turns toward you - with what it does, a slot preview for
/// upgrades (or how many you carry for supplies), the wallet and one big BUY.
///
/// The Supply Counter (Level 1) and the 3D Menu's SHOP open this same book
/// over the same catalogue and wallet (CreateStandalone for the menu).
///
/// FEEL: the book rises and opens (the right page unfolds from the spine,
/// cards fan in), a brass selector glides to the chosen card, the tab's
/// leather thumb slides, and the right page turns when another item or tab is
/// chosen. Buttons and cards squash under a finger (HudPress). BUY asks once
/// in a short line (price, where it goes, coins left) - the button splits
/// into NOT NOW / YES - and only after the purchase is saved does anything
/// celebrate: the stamp slams down, the balance counts down, new slot
/// padlocks pop, the ribbon bookmarks above the book flip for the bookmark
/// set and the packet flies to your bar or Bag, or the miniature lifts and
/// settles as it goes to your office. A refusal shakes the button and says
/// why; nothing is spent. One sound per completed action, through the
/// effects channel. Back (Escape / Android back) cancels a confirmation,
/// then closes.
///
/// OWNERSHIP: purchases go through PlayerProgressManager.CommitPurchase
/// (ShopRules) only; this panel renders snapshots. It holds the player
/// (HudWindowLock), blocks world taps behind it and pauses the review clock
/// while open (OfficeHours reads PausesClock), like other menus.
/// </summary>
public sealed class SupplyShopScreen : MonoBehaviour
{
    private const int SortingOrder = 2550;
    private static readonly Vector2 BookSize = new Vector2(1600f, 900f);
    /// <summary>How far the ribbon bookmarks stand above the book (kept on screen by the fit).</summary>
    private const float RibbonRise = 70f;
    private const float CardWidth = 335f, CardHeight = 176f, CardGap = 20f, GridLeft = 40f, GridTop = 190f;
    private const float TabWidth = 345f;
    private static readonly Color Paper = new Color32(244, 236, 214, 255);
    private static readonly Color CardPaper = new Color32(252, 248, 238, 255);
    private static readonly Color StagePaper = new Color32(232, 222, 198, 255);
    private static readonly Color TabTrack = new Color32(222, 208, 178, 255);
    private static readonly Color Ink = new Color32(52, 40, 26, 255);
    private static readonly Color InkSoft = new Color32(118, 94, 64, 255);
    private static readonly Color Leather = new Color32(92, 58, 34, 255);
    private static readonly Color Navy = new Color32(18, 28, 41, 245);
    private static readonly Color StampGreen = new Color32(46, 140, 84, 255);
    private static readonly Color StampRed = new Color32(176, 40, 42, 255);
    private static readonly Color Warn = new Color32(176, 96, 42, 255);
    private static readonly Color ShortPrice = new Color32(255, 170, 140, 255);

    public static SupplyShopScreen Instance { get; private set; }

    /// <summary>The review clock waits while the catalogue is open (T12: shop menus pause it).</summary>
    public static bool PausesClock => Instance != null && Instance.isOpen;

    private HudHotbar hotbar;
    private bool menuMode;
    private RectTransform root, book, leftPage, rightPage, rightContent, cardsRoot;
    private CanvasGroup group, rightGroup;
    private readonly List<ProductCard> cards = new List<ProductCard>();
    private readonly List<SectionTab> tabs = new List<SectionTab>();
    private RectTransform tabThumb, selector;
    private CanvasGroup selectorGroup;
    private Vector2 selectorGoal;
    private bool selectorSnap = true;
    private RectTransform walletChip, bannerPill;
    private Image bannerFace;
    private TextMeshProUGUI walletText, banner;
    private Image previewIcon, previewShadow;
    private RectTransform previewRect, previewPlate, stamp, slotRow, countChip;
    private RawImage previewMiniature;
    private ShopPreviewStage stage;
    private TextMeshProUGUI titleText, taglineText, effectText, countText, messageText, primaryLabel;
    private Button primaryButton, secondaryButton;
    private RectTransform primaryRect, secondaryRect, shine;
    private CanvasGroup secondaryGroup;
    private readonly List<RectTransform> ribbons = new List<RectTransform>();
    private readonly List<SlotPreview> slotTiles = new List<SlotPreview>();
    private RectTransform flyer;

    private bool isOpen;
    private bool firstVisit;
    private float openedAt, closingAt = -1f, sectionAt = -10f;
    private string selected;
    private string section = ShopCatalogue.SectionSupplies;
    private bool confirming;
    private float pageTurnAt = -10f, stampAt = -10f, shakeAt = -10f, ribbonsAt = -10f, padlocksAt = -10f, flyAt = -10f;
    private int walletShown, walletFrom, walletTo;
    private float walletAt = -10f;
    private int unlockFrom, unlockTo, capacityBefore;
    private Vector2 flyFrom, flyTo;
    private Action afterFlight;

    private sealed class ProductCard
    {
        public string Id, Section;
        /// <summary>Top-left corner on the page (the selector's goal).</summary>
        public Vector2 Origin;
        public RectTransform Rect, Lift;
        public TextMeshProUGUI Title, Price, Note;
        public RectTransform PricePill, Owned, Lock;
        public CanvasGroup Group;
        public Image Icon;
    }

    private sealed class SectionTab
    {
        public string Section;
        public RectTransform Rect;
        public Image Face;
        public TextMeshProUGUI Label;
    }

    private sealed class SlotPreview
    {
        public RectTransform Rect;
        public Image Face, Padlock;
    }

    public bool IsOpen => isOpen;

    private static PlayerProgressManager Manager => PlayerProgressManager.Instance;
    private ShopCatalogue Catalogue => ShopCatalogueAsset.Current;
    private InventoryCatalogue Items => hotbar != null ? hotbar.Catalogue : InventoryCatalogueAsset.Current;
    private PlayerProgressData Data => Manager != null ? Manager.CurrentProgress : null;
    /// <summary>Real coins can be spent: a story run, or the menu between runs (T13).</summary>
    private bool CanSpend => Manager != null && Manager.CanSpendCoins;

    /// <summary>The inventory a purchase lands in: the run's (HUD) or, from the menu, the campaign's.</summary>
    private CampaignInventoryRecord CurrentInventory
    {
        get
        {
            if (hotbar != null) return hotbar.Inventory;
            PlayerProgressData data = Data;
            return data != null && data.HasCampaign ? data.campaign.inventory : new CampaignInventoryRecord();
        }
    }

    // =========================================================
    // BUILD
    // =========================================================

    /// <summary>The Supply Counter's catalogue, on the Level 1 HUD canvas.</summary>
    public static SupplyShopScreen Create(Transform canvasRoot, HudHotbar hotbar)
    {
        RectTransform layer = HudTheme.CreateRect("Supply Catalogue (T12)", canvasRoot);
        HudTheme.Stretch(layer);
        var canvas = layer.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;
        layer.gameObject.AddComponent<GraphicRaycaster>();
        return Finish(layer, hotbar, menu: false);
    }

    /// <summary>
    /// The same catalogue for the 3D Menu (T13): its own screen-space canvas
    /// (1920x1080 reference, like the HUD) over the menu view. Items bought
    /// here go to the campaign's bar or bag (refused when both are full).
    /// </summary>
    public static SupplyShopScreen CreateStandalone()
    {
        var host = new GameObject("Shop Catalogue Canvas (T13)", typeof(RectTransform));
        var canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        var scaler = host.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        host.AddComponent<GraphicRaycaster>();
        if (EventSystem.current == null && FindAnyObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();
        }
        RectTransform layer = HudTheme.CreateRect("Supply Catalogue (T13 menu)", host.transform);
        HudTheme.Stretch(layer);
        SupplyShopScreen screen = Finish(layer, null, menu: true);
        return screen;
    }

    private static SupplyShopScreen Finish(RectTransform layer, HudHotbar hotbar, bool menu)
    {
        SupplyShopScreen screen = layer.gameObject.AddComponent<SupplyShopScreen>();
        screen.hotbar = hotbar;
        screen.menuMode = menu;
        screen.root = layer;
        screen.Build();
        layer.gameObject.SetActive(false);
        Instance = screen;
        return screen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (stage != null) Destroy(stage.gameObject);
        if (menuMode && root != null && root.parent != null) Destroy(root.parent.gameObject);
    }

    private void Build()
    {
        group = root.gameObject.AddComponent<CanvasGroup>();
        RectTransform backdrop = HudTheme.CreateRect("Backdrop", root);
        HudTheme.Stretch(backdrop);
        HudTheme.AddImage(backdrop, null, new Color(0.02f, 0.03f, 0.05f, 0.66f)).raycastTarget = true;

        book = HudTheme.CreateRect("Book", root);
        book.anchorMin = book.anchorMax = new Vector2(0.5f, 0.5f);
        book.sizeDelta = BookSize;
        RectTransform shadow = Box(book, "Shadow", 14f, 22f, BookSize.x, BookSize.y, new Color(0f, 0f, 0f, 0.45f), 34f);
        shadow.SetAsFirstSibling();
        Box(book, "Cover", 0f, 0f, BookSize.x, BookSize.y, Leather, 34f);
        Box(book, "Cover Edge", 8f, 8f, BookSize.x - 16f, BookSize.y - 16f, new Color32(118, 76, 44, 255), 28f);
        foreach (Vector2 corner in new[] { new Vector2(0f, 0f), new Vector2(BookSize.x - 70f, 0f), new Vector2(0f, BookSize.y - 70f), new Vector2(BookSize.x - 70f, BookSize.y - 70f) })
            Box(book, "Brass Corner", corner.x, corner.y, 70f, 70f, HudTheme.Brass, 16f);

        // Three ribbon bookmarks stand up out of the top of the book, behind the
        // pages, so they never cover anything printed on a page.
        Color[] leather = { new Color32(150, 40, 46, 255), new Color32(46, 112, 74, 255), new Color32(44, 70, 128, 255) };
        for (int i = 0; i < leather.Length; i++)
        {
            RectTransform ribbon = Box(book, "Ribbon " + i, 0f, 0f, 26f, 112f + i * 10f, leather[i], 5f);
            ribbon.pivot = new Vector2(0.5f, 0f);
            ribbon.anchoredPosition = new Vector2(1412f + i * 40f, -64f);
            Box(ribbon, "Fold", 0f, 0f, 26f, 10f, new Color(0f, 0f, 0f, 0.18f), 4f);
            ribbons.Add(ribbon);
        }

        leftPage = Box(book, "Left Page", 30f, 30f, 770f, 840f, Paper, 14f);
        rightPage = Box(book, "Right Page", 800f, 30f, 770f, 840f, Paper, 14f);
        rightPage.pivot = new Vector2(0f, 1f);
        // A soft gutter shadow on each page beside the spine.
        Box(leftPage, "Gutter", 730f, 0f, 40f, 840f, new Color(0f, 0f, 0f, 0.05f));
        Box(rightPage, "Gutter", 0f, 0f, 34f, 840f, new Color(0f, 0f, 0f, 0.05f));
        RectTransform spine = Box(book, "Spine", 786f, 30f, 28f, 840f, new Color(0f, 0f, 0f, 0.22f), 0f);
        spine.SetSiblingIndex(rightPage.GetSiblingIndex() + 1);

        BuildLeftPage();
        BuildRightPage();

        flyer = HudTheme.CreateRect("Flyer", root);
        flyer.sizeDelta = new Vector2(150f, 150f);
        Image flyImage = flyer.gameObject.AddComponent<Image>();
        flyImage.preserveAspect = true;
        flyImage.raycastTarget = false;
        flyer.gameObject.SetActive(false);
    }

    private void BuildLeftPage()
    {
        TextMeshProUGUI title = Label(leftPage, "Title", "SUPPLY CATALOGUE", 40f, 26f, 690f, 62f, 42f, Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        title.characterSpacing = 4f;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.enableAutoSizing = true;
        title.fontSizeMin = 28f;
        title.fontSizeMax = 42f;

        // Two leather index tabs as one segmented strip: the leather thumb
        // slides under the open one (the name is on each, never colour alone).
        RectTransform track = Box(leftPage, "Tabs", 40f, 102f, TabWidth * 2f, 70f, TabTrack, 35f);
        tabThumb = Box(track, "Thumb", 5f, 5f, TabWidth - 10f, 60f, Leather, 30f);
        var thumbShadow = tabThumb.gameObject.AddComponent<Shadow>();
        thumbShadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
        thumbShadow.effectDistance = new Vector2(0f, -3f);
        for (int i = 0; i < ShopCatalogue.Sections.Length; i++)
        {
            string id = ShopCatalogue.Sections[i];
            RectTransform rect = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Tab " + id, track), i * TabWidth, 0f, TabWidth, 70f);
            var tab = new SectionTab { Section = id, Rect = rect };
            tab.Face = HudTheme.AddImage(rect, null, Color.clear);
            tab.Face.raycastTarget = true;
            tab.Label = HudTheme.CreateLabel("Label", rect, ShopCatalogue.SectionTitle(id), 28f, HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            HudTheme.Stretch(tab.Label.rectTransform, 6f);
            tab.Label.characterSpacing = 4f;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = tab.Face;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => SelectSection(id, playSound: true));
            HudPress.Add(rect, tab.Label.rectTransform, 1.05f, 0.92f);
            tabs.Add(tab);
        }

        cardsRoot = HudTheme.CreateRect("Cards", leftPage);
        HudTheme.Stretch(cardsRoot);
        // The brass selector glides behind the chosen card.
        selector = Box(cardsRoot, "Selector", 0f, 0f, CardWidth + 26f, CardHeight + 26f, new Color(HudTheme.Brass.r, HudTheme.Brass.g, HudTheme.Brass.b, 0.28f), 30f);
        RectTransform ring = HudTheme.CreateRect("Ring", selector);
        HudTheme.Stretch(ring, 7f);
        HudTheme.AddImage(ring, HudTheme.Rounded, HudTheme.Brass, 24f);
        selectorGroup = selector.gameObject.AddComponent<CanvasGroup>();
        selectorGroup.blocksRaycasts = false;
        ShopCatalogue catalogue = Catalogue;
        foreach (string id in ShopCatalogue.Sections)
        {
            List<ShopProduct> products = catalogue.InSection(id);
            for (int i = 0; i < products.Count && i < ShopCatalogue.CardsPerSection; i++)
                cards.Add(BuildCard(products[i], GridLeft + (i % 2) * (CardWidth + CardGap), GridTop + (i / 2) * (CardHeight + CardGap)));
        }

        MakeButton(leftPage, "Back", 40f, 772f, 220f, 64f, "BACK", Navy, () => Close());
    }

    private ProductCard BuildCard(ShopProduct product, float x, float y)
    {
        RectTransform rect = HudTheme.PlaceCentered(HudTheme.CreateRect("Card " + product.id, cardsRoot), x, y, CardWidth, CardHeight);
        var card = new ProductCard { Id = product.id, Section = product.section, Rect = rect, Origin = new Vector2(x, y), Group = rect.gameObject.AddComponent<CanvasGroup>() };
        card.Lift = HudTheme.CreateRect("Lift", rect);
        HudTheme.Stretch(card.Lift);
        Image face = HudTheme.AddImage(card.Lift, HudTheme.Rounded, CardPaper, 18f);
        face.raycastTarget = true;
        var cardShadow = card.Lift.gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.16f);
        cardShadow.effectDistance = new Vector2(2f, -5f);

        RectTransform well = Box(card.Lift, "Icon Well", 16f, 16f, 100f, 100f, StagePaper, 20f);
        RectTransform icon = HudTheme.CreateRect("Icon", well);
        HudTheme.Stretch(icon, 4f);
        card.Icon = icon.gameObject.AddComponent<Image>();
        card.Icon.sprite = HudIconArt.Get(product.icon);
        card.Icon.preserveAspect = true;
        card.Icon.raycastTarget = false;
        card.Title = Label(card.Lift, "Name", product.title, 130f, 16f, 192f, 100f, 30f, Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        card.Title.enableAutoSizing = true;
        card.Title.fontSizeMin = 20f;

        // Bottom row: a price pill, an OWNED stamp, or a padlock with what unlocks it.
        card.PricePill = Box(card.Lift, "Price", 16f, 124f, 150f, 40f, Navy, 20f);
        Coin(card.PricePill, 6f, 5f, 30f).name = "Coin";
        card.Price = Label(card.PricePill, "Amount", product.price.ToString(), 40f, 0f, 102f, 40f, 28f, HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        card.Note = Label(card.Lift, "Note", "", 174f, 124f, 148f, 40f, 20f, Warn, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

        card.Owned = Box(card.Lift, "Owned", 16f, 122f, 150f, 44f, StampGreen, 10f);
        card.Owned.pivot = new Vector2(0.5f, 0.5f);
        card.Owned.anchoredPosition = new Vector2(91f, -144f);
        card.Owned.localRotation = Quaternion.Euler(0f, 0f, -6f);
        Box(card.Owned, "Face", 4f, 4f, 142f, 36f, new Color32(246, 240, 224, 255), 8f);
        Label(card.Owned, "Text", "OWNED", 4f, 4f, 142f, 36f, 26f, StampGreen, TextAlignmentOptions.Center, FontStyles.Bold).characterSpacing = 5f;

        card.Lock = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Lock", card.Lift), 16f, 120f, 306f, 48f);
        RectTransform padlock = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Padlock", card.Lock), 0f, 6f, 36f, 36f);
        HudTheme.AddImage(padlock, HudTheme.Lock, InkSoft);
        TextMeshProUGUI requires = Label(card.Lock, "Requires", product.requiresLabel, 44f, 0f, 262f, 48f, 20f, InkSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        requires.enableAutoSizing = true;
        requires.fontSizeMin = 14f;

        var button = card.Lift.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.98f, 0.92f, 1f);
        colors.pressedColor = new Color(0.9f, 0.86f, 0.78f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        string id = product.id;
        button.onClick.AddListener(() => Select(id, playSound: true));
        HudPress.Add(card.Lift, rect, 1.03f, 0.96f);
        return card;
    }

    private void BuildRightPage()
    {
        rightContent = HudTheme.CreateRect("Content", rightPage);
        HudTheme.Stretch(rightContent);
        rightGroup = rightContent.gameObject.AddComponent<CanvasGroup>();

        // Header: a status pill (practice / welcome) on the left, the wallet on the right.
        bannerPill = Box(rightPage, "Banner", 40f, 30f, 440f, 56f, StagePaper, 28f);
        bannerFace = bannerPill.GetComponent<Image>();
        banner = Label(bannerPill, "Text", "", 22f, 0f, 400f, 56f, 24f, Warn, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        banner.textWrappingMode = TextWrappingModes.NoWrap;
        banner.enableAutoSizing = true;
        banner.fontSizeMin = 15f;
        walletChip = Box(rightPage, "Shop Wallet", 496f, 26f, 244f, 64f, Navy, 32f);
        Coin(walletChip, 10f, 8f, 48f);
        walletText = Label(walletChip, "Coins", "0", 64f, 4f, 104f, 56f, 36f, HudTheme.TextPrimary, TextAlignmentOptions.Right, FontStyles.Bold);
        Label(walletChip, "Unit", "COINS", 174f, 20f, 62f, 28f, 18f, HudTheme.BrassSoft, TextAlignmentOptions.Left, FontStyles.Bold);

        // The stage: a lit panel with the picture floating over its shadow.
        Box(rightContent, "Stage", 40f, 106f, 690f, 262f, StagePaper, 26f);
        RectTransform glow = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Glow", rightContent), 225f, 112f, 320f, 250f);
        HudTheme.AddImage(glow, HudTheme.Circle, new Color(1f, 0.99f, 0.95f, 0.55f));
        RectTransform floor = HudTheme.CreateRect("Floor Shadow", rightContent);
        floor.anchorMin = floor.anchorMax = new Vector2(0f, 1f);
        floor.pivot = new Vector2(0.5f, 0.5f);
        floor.anchoredPosition = new Vector2(385f, -350f);
        floor.sizeDelta = new Vector2(170f, 22f);
        previewShadow = HudTheme.AddImage(floor, HudTheme.Circle, new Color(0f, 0f, 0f, 0.16f));

        previewRect = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Preview", rightContent), 275f, 118f, 220f, 220f);
        previewIcon = previewRect.gameObject.AddComponent<Image>();
        previewIcon.preserveAspect = true;
        previewIcon.raycastTarget = false;

        // T13: office decor shows its live miniature in a brass-rimmed display window.
        previewPlate = Box(rightContent, "Miniature", 225f, 112f, 320f, 250f, HudTheme.Brass, 20f);
        RectTransform window = HudTheme.CreateRect("Window", previewPlate);
        HudTheme.Stretch(window, 7f);
        HudTheme.AddImage(window, HudTheme.Rounded, StagePaper, 14f).raycastTarget = false;
        RectTransform view = HudTheme.CreateRect("View", window);
        HudTheme.Stretch(view, 6f);
        view.anchorMin = view.anchorMax = new Vector2(0.5f, 0.5f);
        view.sizeDelta = new Vector2(232f, 232f);
        previewMiniature = view.gameObject.AddComponent<RawImage>();
        previewMiniature.raycastTarget = false;
        previewPlate.gameObject.SetActive(false);

        titleText = Label(rightContent, "Name", "", 40f, 380f, 690f, 58f, 46f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 30f;
        taglineText = Label(rightContent, "Line", "", 40f, 438f, 690f, 38f, 26f, InkSoft, TextAlignmentOptions.Center, FontStyles.Italic);
        taglineText.textWrappingMode = TextWrappingModes.NoWrap;
        taglineText.enableAutoSizing = true;
        taglineText.fontSizeMin = 18f;
        effectText = Label(rightContent, "Effect", "", 56f, 482f, 658f, 102f, 26f, Ink, TextAlignmentOptions.Top);
        effectText.enableAutoSizing = true;
        effectText.fontSizeMin = 19f;

        // Upgrades: the area's slots (new ones padlocked until bought).
        slotRow = HudTheme.CreateRect("Slot Preview", rightContent);
        slotRow.anchorMin = slotRow.anchorMax = new Vector2(0.5f, 1f);
        slotRow.pivot = new Vector2(0.5f, 1f);
        slotRow.anchoredPosition = new Vector2(0f, -592f);
        slotRow.sizeDelta = new Vector2(700f, 54f);
        for (int i = 0; i < CampaignInventoryRecord.MaxBag; i++)
        {
            RectTransform tile = HudTheme.CreateRect("Slot " + i, slotRow);
            tile.anchorMin = tile.anchorMax = new Vector2(0.5f, 0.5f);
            tile.sizeDelta = new Vector2(52f, 52f);
            var preview = new SlotPreview { Rect = tile };
            preview.Face = tile.gameObject.AddComponent<Image>();
            preview.Face.preserveAspect = true;
            preview.Face.raycastTarget = false;
            RectTransform lockRect = HudTheme.CreateRect("Padlock", tile);
            lockRect.anchorMin = lockRect.anchorMax = new Vector2(0.5f, 0.55f);
            lockRect.sizeDelta = new Vector2(24f, 24f);
            preview.Padlock = HudTheme.AddImage(lockRect, HudTheme.Lock, new Color(0.85f, 0.87f, 0.9f, 0.95f));
            slotTiles.Add(preview);
        }
        // Supplies: how many you carry of the limit (same row as the slots; never both).
        countChip = Box(rightContent, "Carried", 165f, 592f, 440f, 52f, Navy, 26f);
        countText = Label(countChip, "Text", "", 16f, 0f, 408f, 52f, 24f, HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        countText.textWrappingMode = TextWrappingModes.NoWrap;
        countText.enableAutoSizing = true;
        countText.fontSizeMin = 16f;

        messageText = Label(rightContent, "Message", "", 40f, 652f, 690f, 62f, 24f, Warn, TextAlignmentOptions.Center, FontStyles.Bold);
        messageText.enableAutoSizing = true;
        messageText.fontSizeMin = 17f;

        secondaryButton = MakeButton(rightContent, "Not Now", 175f, 724f, 250f, 96f, "NOT NOW", Navy, CancelConfirm);
        secondaryRect = (RectTransform)secondaryButton.transform;
        secondaryGroup = secondaryButton.gameObject.AddComponent<CanvasGroup>();
        secondaryButton.gameObject.SetActive(false);
        primaryButton = MakeButton(rightContent, "Buy", 175f, 724f, 420f, 96f, "BUY", new Color32(120, 92, 44, 255), OnPrimary);
        primaryRect = (RectTransform)primaryButton.transform;
        primaryLabel = primaryButton.GetComponentInChildren<TextMeshProUGUI>();
        primaryLabel.fontSize = primaryLabel.fontSizeMax = 38f;
        // A slow glint sweeps across BUY while it can be pressed.
        RectTransform glintMask = HudTheme.CreateRect("Glint Mask", primaryRect);
        HudTheme.Stretch(glintMask, 4f);
        glintMask.gameObject.AddComponent<RectMask2D>();
        glintMask.SetAsFirstSibling();
        shine = HudTheme.CreateRect("Glint", glintMask);
        shine.anchorMin = shine.anchorMax = new Vector2(0f, 0.5f);
        shine.sizeDelta = new Vector2(46f, 170f);
        shine.localRotation = Quaternion.Euler(0f, 0f, -18f);
        HudTheme.AddImage(shine, null, new Color(1f, 0.96f, 0.84f, 0.2f));

        stamp = Box(rightContent, "Bought Stamp", 230f, 150f, 310f, 120f, StampRed, 16f);
        stamp.pivot = new Vector2(0.5f, 0.5f);
        stamp.anchoredPosition = new Vector2(385f, -237f);
        Box(stamp, "Face", 7f, 7f, 296f, 106f, new Color32(250, 244, 230, 235), 12f);
        Label(stamp, "Text", "BOUGHT!", 7f, 7f, 296f, 106f, 56f, StampRed, TextAlignmentOptions.Center, FontStyles.Bold).characterSpacing = 6f;
        stamp.gameObject.SetActive(false);
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    /// <summary>Opens the catalogue (firstVisit: the shop was just introduced, a welcome line shows).</summary>
    public void Open(bool firstVisit)
    {
        if (isOpen) return;
        isOpen = true;
        closingAt = -1f;
        this.firstVisit = firstVisit;
        openedAt = Time.unscaledTime;
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
        HudWindowLock.Acquire(this);
        group.blocksRaycasts = true;
        FitToScreen();
        walletShown = walletTo = Coins();
        walletAt = -10f;
        confirming = false;
        stamp.gameObject.SetActive(false);
        flyer.gameObject.SetActive(false);
        if (string.IsNullOrEmpty(selected) || Catalogue.Find(selected) == null) selected = FirstInteresting(null);
        ShopProduct chosen = Catalogue.Find(selected);
        section = chosen != null ? chosen.section : ShopCatalogue.SectionSupplies;
        selectorSnap = true;
        Refresh();
        SnapMotion();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_book_open_01");
        GameplayEvents.RaiseGuideCue(SupplyCounter.CueOpened);
    }

    /// <summary>Closes with a short fold; the player gets control back at once.</summary>
    public void Close()
    {
        if (!isOpen || closingAt > 0f) return;
        closingAt = Time.unscaledTime;
        confirming = false;
        group.blocksRaycasts = false;
        HudWindowLock.Release(this);
        if (stage != null) stage.SetRendering(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_book_close_01");
        GameplayEvents.RaiseGuideCue(SupplyCounter.CueClosed);
    }

    private void FinishClose()
    {
        isOpen = false;
        closingAt = -1f;
        root.gameObject.SetActive(false);
        Action next = afterFlight;
        afterFlight = null;
        next?.Invoke();
    }

    private void OnDisable()
    {
        if (stage != null) stage.SetRendering(false);
        if (!isOpen) return;
        isOpen = false;
        HudWindowLock.Release(this);
    }

    private void FitToScreen() => book.localScale = Vector3.one * BookScale();

    /// <summary>The book (and the ribbons standing above it) fits the screen with a margin.</summary>
    private float BookScale()
    {
        Rect area = root.rect;
        return Mathf.Max(0.45f, Mathf.Min(1f, (area.width - 40f) / BookSize.x, (area.height - 24f) / (BookSize.y + 2f * RibbonRise)));
    }

    /// <summary>The first product worth showing (buyable, else not owned, else the first), optionally on one tab.</summary>
    private string FirstInteresting(string onlySection)
    {
        ShopCatalogue catalogue = Catalogue;
        List<ShopProduct> products = onlySection != null ? catalogue.InSection(onlySection) : catalogue.products;
        foreach (ShopProduct product in products)
            if (ShopRules.StatusOf(Data, catalogue, product, CanSpend) == ShopStatus.Available) return product.id;
        foreach (ShopProduct product in products)
            if (ShopRules.StatusOf(Data, catalogue, product, CanSpend) != ShopStatus.Owned) return product.id;
        return products.Count > 0 ? products[0].id : null;
    }

    private int Coins()
    {
        PlayerProgressData data = Data;
        return data != null && data.HasCampaign ? data.campaign.coins : 0;
    }

    // =========================================================
    // RENDER
    // =========================================================

    private void SelectSection(string id, bool playSound)
    {
        if (id == section || closingAt > 0f) return;
        section = id;
        sectionAt = Time.unscaledTime;
        confirming = false;
        selected = FirstInteresting(id);
        selectorSnap = true;
        pageTurnAt = Time.unscaledTime;
        stamp.gameObject.SetActive(false);
        if (playSound && AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_page_turn_01", 0.7f);
        Refresh();
    }

    private void Select(string id, bool playSound)
    {
        if (id == selected && !confirming) return;
        selected = id;
        confirming = false;
        pageTurnAt = Time.unscaledTime;
        stamp.gameObject.SetActive(false);
        if (playSound && AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_page_turn_01", 0.7f);
        Refresh();
    }

    private void Refresh()
    {
        PlayerProgressData data = Data;
        ShopCatalogue catalogue = Catalogue;
        bool spend = CanSpend;
        foreach (SectionTab tab in tabs)
            tab.Label.color = tab.Section == section ? (Color)HudTheme.TextPrimary : InkSoft;
        foreach (ProductCard card in cards)
        {
            bool visible = card.Section == section;
            card.Rect.gameObject.SetActive(visible);
            if (!visible) continue;
            ShopProduct product = catalogue.Find(card.Id);
            ShopStatus status = ShopRules.StatusOf(data, catalogue, product, spend);
            bool owned = status == ShopStatus.Owned;
            bool locked = status == ShopStatus.Teaser || status == ShopStatus.NeedsPrevious;
            card.Owned.gameObject.SetActive(owned);
            card.Lock.gameObject.SetActive(locked);
            card.PricePill.gameObject.SetActive(!owned && !locked);
            card.Group.alpha = locked ? 0.72f : 1f;
            card.Icon.color = locked ? new Color(1f, 1f, 1f, 0.55f) : Color.white;
            card.Price.color = status == ShopStatus.TooExpensive ? ShortPrice : (Color)HudTheme.TextPrimary;
            card.Note.text = status == ShopStatus.TooExpensive ? "Need " + (product.price - Coins()) : "";
            if (card.Id == selected) selectorGoal = new Vector2(card.Origin.x - 13f, 13f - card.Origin.y);
        }

        banner.text = !spend ? "PRACTICE RUN · BROWSING ONLY" :
            firstVisit ? "Welcome! Spend the coins you earn on jobs." :
            menuMode ? "Same wallet as the Supply Counter" : "";
        banner.color = !spend ? Warn : InkSoft;
        bannerPill.gameObject.SetActive(banner.text.Length > 0);
        bannerFace.color = !spend ? new Color(Warn.r, Warn.g, Warn.b, 0.16f) : StagePaper;

        ShopProduct chosen = catalogue.Find(selected);
        if (chosen == null) return;
        ShopStatus chosenStatus = ShopRules.StatusOf(data, catalogue, chosen, spend);
        bool chosenLocked = chosenStatus == ShopStatus.Teaser || chosenStatus == ShopStatus.NeedsPrevious;
        ShowPreview(chosen, chosenLocked);
        titleText.text = chosen.title;
        taglineText.text = chosen.tagline;
        effectText.text = chosen.effect;
        string counts = SupplyCounts(chosen);
        countChip.gameObject.SetActive(counts.Length > 0);
        countText.text = counts;
        LayoutSlots(chosen, data, animateUnlock: false);

        string installedAt = chosen.IsDecor && chosenStatus == ShopStatus.Owned ? AnchorLabel(DecorRules.AnchorOf(data?.campaign, chosen.id)) : "";
        string noRoom = chosenStatus == ShopStatus.Available ? NoRoomFor(chosen) : "";
        string reason = installedAt.Length > 0 ? "In your office: " + installedAt + "." :
            noRoom.Length > 0 ? noRoom :
            chosenStatus == ShopStatus.BrowseOnly ? "" : ShopRules.ReasonFor(chosenStatus, chosen, data, catalogue);
        primaryButton.interactable = chosenStatus == ShopStatus.Available && noRoom.Length == 0;
        primaryLabel.text = confirming ? "YES, BUY IT" : chosenStatus == ShopStatus.Owned ? "OWNED" : chosenLocked ? "LOCKED" : "BUY · " + chosen.price;
        if (confirming && !secondaryButton.gameObject.activeSelf)
        {
            secondaryButton.gameObject.SetActive(true);
            secondaryRect.anchoredPosition = primaryRect.anchoredPosition;
            secondaryGroup.alpha = 0f;
        }
        else if (!confirming) secondaryButton.gameObject.SetActive(false);
        if (!confirming) SetMessage(reason, warn: chosenStatus != ShopStatus.Owned && installedAt.Length == 0 && reason.Length > 0);
        if (!spend) primaryLabel.text = "BROWSING ONLY";
    }

    /// <summary>T14: a repeatable supply says how many you carry (of its carry limit).</summary>
    private string SupplyCounts(ShopProduct product)
    {
        if (!product.consumable) return "";
        InventoryItemDefinition item = Items.Find(product.itemId);
        int carried = InventoryRules.CarriedCount(CurrentInventory, product.itemId);
        string limit = item != null && item.carryLimit > 0 ? " of " + item.carryLimit : "";
        return $"You carry {carried}{limit}";
    }

    /// <summary>
    /// Why an item cannot be bought right now, said before BUY ("" when it can):
    /// the carry limit is reached, or (the menu, with no hand) the bar and bag
    /// are full. A trial on a copy of the inventory; nothing changes.
    /// </summary>
    private string NoRoomFor(ShopProduct product)
    {
        if (!product.IsItem) return "";
        InventoryItemDefinition item = Items.Find(product.itemId);
        if (item != null && InventoryRules.OverCarryLimit(CurrentInventory, Items, product.itemId, 1) > 0)
            return $"You carry {item.carryLimit} already. Use one first.";
        if (hotbar != null) return "";
        CampaignInventoryRecord probe = JsonUtility.FromJson<CampaignInventoryRecord>(JsonUtility.ToJson(CurrentInventory));
        InventoryResult trial = InventoryRules.AcquireWithoutHand(probe, Items, product.itemId, 1, "probe", null);
        return trial.Outcome == InventoryOutcome.Rejected ? trial.Message + "." : "";
    }

    /// <summary>Office decor: its live miniature (if it has a model); everything else: its picture.</summary>
    private void ShowPreview(ShopProduct chosen, bool locked)
    {
        bool miniature = false;
        if (chosen.IsDecor && DecorModels.Has(chosen.id))
        {
            if (stage == null) stage = ShopPreviewStage.Create(StagePaper);
            miniature = stage.Show(chosen.id);
            previewMiniature.texture = stage.Texture;
        }
        else if (stage != null) stage.SetRendering(false);
        previewPlate.gameObject.SetActive(miniature);
        previewRect.gameObject.SetActive(!miniature);
        previewShadow.gameObject.SetActive(!miniature);
        previewIcon.sprite = HudIconArt.Get(chosen.icon);
        previewIcon.color = locked ? new Color(1f, 1f, 1f, 0.55f) : Color.white;
    }

    private string AnchorLabel(string anchorId)
    {
        ShopDecorAnchor anchor = Catalogue.FindAnchor(anchorId);
        return anchor != null ? anchor.label : "";
    }

    /// <summary>Upgrades show their area's slots: open ones, and the new ones (padlocked until bought).</summary>
    private void LayoutSlots(ShopProduct product, PlayerProgressData data, bool animateUnlock)
    {
        bool show = product.IsUpgrade;
        slotRow.gameObject.SetActive(show);
        if (!show) return;
        CampaignInventoryRecord inventory = CurrentInventory;
        bool bag = product.area == "Bag";
        int current = bag ? inventory.bagCapacity : inventory.hotbarCapacity;
        bool owned = ShopRules.IsOwned(data, product);
        int target = Mathf.Max(current, product.capacity);
        int baseline = owned ? Mathf.Min(current, product.capacity) : current;
        if (owned && !animateUnlock) baseline = target;
        if (animateUnlock) baseline = Mathf.Min(capacityBefore, target);
        int count = target;
        // Up to 13 Bag slots (T30): the tiles shrink a little to keep the row inside the page.
        float size = Mathf.Min(52f, (700f - (count - 1) * 6f) / Mathf.Max(1, count));
        float step = size + Mathf.Min(8f, size * 0.15f);
        float width = count * size + (count - 1) * (step - size);
        for (int i = 0; i < slotTiles.Count; i++)
        {
            SlotPreview tile = slotTiles[i];
            bool used = i < count;
            tile.Rect.gameObject.SetActive(used);
            if (!used) continue;
            tile.Rect.sizeDelta = new Vector2(size, size);
            tile.Rect.anchoredPosition = new Vector2(-width * 0.5f + size * 0.5f + i * step, 0f);
            bool locked = i >= baseline;
            tile.Face.sprite = HudIconArt.Get(locked ? "locked" : "slot");
            tile.Padlock.gameObject.SetActive(locked);
            tile.Padlock.color = new Color(0.85f, 0.87f, 0.9f, 0.95f);
            tile.Padlock.rectTransform.localScale = Vector3.one;
            tile.Rect.localScale = Vector3.one;
        }
        if (animateUnlock)
        {
            unlockFrom = baseline;
            unlockTo = target;
            padlocksAt = Time.unscaledTime;
        }
    }

    private void SetMessage(string text, bool warn)
    {
        messageText.text = text ?? "";
        messageText.color = warn ? Warn : StampGreen;
    }

    // =========================================================
    // BUY
    // =========================================================

    private void OnPrimary()
    {
        ShopProduct product = Catalogue.Find(selected);
        if (product == null || !CanSpend) return;
        if (!confirming)
        {
            confirming = true;
            Refresh();
            SetMessage(ConfirmText(product), warn: false);
            messageText.color = Ink;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_ui_select_01", 0.6f);
            return;
        }
        primaryButton.interactable = false;
        secondaryButton.interactable = false;
        CampaignInventoryRecord before = CurrentInventory;
        capacityBefore = product.area == "Bag" ? before.bagCapacity : before.hotbarCapacity;
        string request = Guid.NewGuid().ToString("N");
        PurchaseResult result = Manager.CommitPurchase(Catalogue, Items, product.id, request);
        secondaryButton.interactable = true;
        confirming = false;
        if (result.Outcome != InventoryOutcome.Applied)
        {
            Refresh();
            SetMessage(string.IsNullOrEmpty(result.Message) ? "Purchase not saved; nothing spent." : result.Message, warn: true);
            shakeAt = Time.unscaledTime;
            return;
        }
        Celebrate(product, result);
    }

    private void CancelConfirm()
    {
        confirming = false;
        Refresh();
    }

    /// <summary>"Buy for 40? It goes into your bar. You'll have 110 left." (where an item or decor piece goes, when it matters).</summary>
    private string ConfirmText(ShopProduct product)
    {
        int left = Coins() - product.price;
        string where = "";
        if (product.IsItem)
        {
            CampaignInventoryRecord probe = JsonUtility.FromJson<CampaignInventoryRecord>(JsonUtility.ToJson(CurrentInventory));
            InventoryResult landing = hotbar != null
                ? InventoryRules.Acquire(probe, Items, product.itemId, 1, "probe", null, null)
                : InventoryRules.AcquireWithoutHand(probe, Items, product.itemId, 1, "probe", null);
            where = landing.Location.Area == InventoryArea.Hotbar ? " It goes into your bar." :
                landing.Location.Area == InventoryArea.Bag ? " It goes into your bag." :
                landing.Location.Area == InventoryArea.Hand ? " Bar and bag are full: you'll make room for it." : "";
        }
        else if (product.IsDecor)
        {
            string label = AnchorLabel(DecorRules.FreeAnchor(Data?.campaign, Catalogue, product));
            where = label.Length > 0 ? $" It goes on {label}." : "";
        }
        return $"Buy for {product.price} coins?{where} You'll have {left} left.";
    }

    /// <summary>Only after the save: stamp, the balance counts down, then padlocks pop, ribbons flip and the packet flies home, or the miniature settles into your office.</summary>
    private void Celebrate(ShopProduct product, PurchaseResult result)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_stamp_press_01");
        stamp.gameObject.SetActive(true);
        stampAt = Time.unscaledTime;
        walletFrom = result.CoinsBefore;
        walletTo = result.CoinsAfter;
        walletAt = Time.unscaledTime;
        Refresh();
        string done;
        if (product.IsUpgrade)
        {
            LayoutSlots(product, Data, animateUnlock: true);
            done = product.area == "Bag" ? $"Your bag now has {product.capacity} slots." : $"Your bar now has {product.capacity} slots.";
        }
        else if (product.IsDecor)
        {
            if (stage != null) stage.Settle();
            string label = AnchorLabel(result.AnchorId);
            done = menuMode ? $"Installed on {label}. You'll see it in your office." : $"Installed on {label}.";
        }
        else
        {
            if (product.itemId == InventoryCatalogue.BookmarkSet) ribbonsAt = Time.unscaledTime;
            bool needsRoom = result.Item != null && result.Item.Outcome == InventoryOutcome.NeedsRoom;
            InventoryArea landed = result.Item != null ? result.Item.Location.Area : InventoryArea.None;
            done = needsRoom ? "Bar and bag are full: make room for it next." :
                landed == InventoryArea.Hotbar ? $"The {product.title} is in your bar." : $"The {product.title} is in your bag.";
            if (hotbar != null)
            {
                RectTransform target = needsRoom ? hotbar.BagIcon : hotbar.ItemTarget(product.itemId);
                StartFlight(product, target);
                if (needsRoom) afterFlight = () => { if (hotbar != null && hotbar.Screen != null) hotbar.Screen.OpenMakeRoom(); };
            }
        }
        SetMessage(done, warn: false);
        if (product.consumable) return;
        primaryLabel.text = "OWNED";
        primaryButton.interactable = false;
    }

    private void StartFlight(ShopProduct product, RectTransform target)
    {
        if (target == null) return;
        flyer.GetComponent<Image>().sprite = HudIconArt.Get(product.icon);
        flyFrom = RectTransformUtility.WorldToScreenPoint(null, previewRect.TransformPoint(previewRect.rect.center));
        flyTo = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center));
        flyAt = Time.unscaledTime + 0.45f;
        flyer.gameObject.SetActive(false);
    }

    // =========================================================
    // MOTION
    // =========================================================

    /// <summary>Exponential ease toward a goal, frame-rate independent.</summary>
    private static float Damp(float rate) => 1f - Mathf.Exp(-rate * Time.unscaledDeltaTime);

    /// <summary>Puts the sliding pieces straight at their goals (on opening).</summary>
    private void SnapMotion()
    {
        tabThumb.anchoredPosition = new Vector2(5f + SectionIndex() * TabWidth, -5f);
        selector.anchoredPosition = selectorGoal;
        selectorSnap = false;
        primaryRect.anchoredPosition = BuyAt(false);
    }

    /// <summary>The centre of BUY: centred alone, or on the right beside NOT NOW while confirming.</summary>
    private static Vector2 BuyAt(bool confirm) => new Vector2(confirm ? 520f : 385f, -772f);

    private int SectionIndex() => Mathf.Max(0, Array.IndexOf(ShopCatalogue.Sections, section));

    private void Update()
    {
        if (!isOpen) return;
        float now = Time.unscaledTime;
        if (closingAt > 0f)
        {
            float c = HudTheme.EaseOutCubic(Mathf.Clamp01((now - closingAt) / 0.2f));
            group.alpha = 1f - c;
            book.localScale = Vector3.one * (BookScale() * Mathf.Lerp(1f, 0.94f, c));
            book.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -30f, c));
            if (c >= 1f && now >= flyAt + 0.5f) FinishClose();
            UpdateFlight(now);
            return;
        }
        // A conversation, card or settings takes over: fold away.
        if (DialogueUI.IsAnyDialogueActive || HudRewardScreen.AnyOpen || InGameSettings.IsOpen)
        {
            Close();
            return;
        }
        // Back: a confirmation steps back first, then the book closes.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (confirming) CancelConfirm();
            else Close();
            return;
        }

        float t = now - openedAt;
        group.alpha = Mathf.Clamp01(t / 0.16f);
        float rise = HudTheme.EaseOutBack(Mathf.Clamp01(t / 0.36f));
        book.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(-60f, 0f, rise));
        book.localScale = Vector3.one * (BookScale() * Mathf.LerpUnclamped(0.96f, 1f, rise));
        // The right page unfolds from the spine like an opening book.
        float unfold = HudTheme.EaseOutCubic(Mathf.Clamp01((t - 0.06f) / 0.34f));
        rightPage.localScale = new Vector3(Mathf.Max(0.02f, unfold), 1f, 1f);
        // Cards fan in on opening and again when a tab turns the page.
        float fan = now - Mathf.Max(openedAt + 0.12f, sectionAt);
        int index = 0;
        foreach (ProductCard card in cards)
        {
            if (card.Section != section) continue;
            float c = HudTheme.EaseOutBack(Mathf.Clamp01((fan - index * 0.045f) / 0.26f));
            float lift = card.Id == selected ? 5f : 0f;
            Vector2 at = card.Lift.anchoredPosition;
            card.Lift.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-40f, 0f, c), Mathf.Lerp(at.y, lift, Damp(18f)));
            card.Lift.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, c);
            index++;
        }
        // The leather thumb slides under the open tab; the brass selector glides to the chosen card.
        Vector2 thumbGoal = new Vector2(5f + SectionIndex() * TabWidth, -5f);
        tabThumb.anchoredPosition = Vector2.Lerp(tabThumb.anchoredPosition, thumbGoal, Damp(16f));
        if (selectorSnap) { selector.anchoredPosition = selectorGoal; selectorSnap = false; }
        else selector.anchoredPosition = Vector2.Lerp(selector.anchoredPosition, selectorGoal, Damp(18f));
        float fanIn = Mathf.Clamp01((fan - 0.1f) / 0.2f);
        selectorGroup.alpha = fanIn * (0.86f + Mathf.Sin(now * 3f) * 0.14f);

        float turn = HudTheme.EaseOutCubic(Mathf.Clamp01((now - pageTurnAt) / 0.26f));
        rightGroup.alpha = Mathf.Max(0.05f, turn);
        rightContent.anchoredPosition = new Vector2(Mathf.Lerp(28f, 0f, turn), 0f);

        // The picture floats gently over its shadow (a miniature turns on its own stage instead).
        float bob = Mathf.Sin(now * 2f);
        previewRect.anchoredPosition = new Vector2(275f, -118f + bob * 6f);
        previewShadow.rectTransform.localScale = new Vector3(1f - bob * 0.08f, 1f, 1f);
        previewShadow.color = new Color(0f, 0f, 0f, 0.16f - bob * 0.03f);

        // Stamp slam.
        if (stamp.gameObject.activeSelf)
        {
            float s = Mathf.Clamp01((now - stampAt) / 0.22f);
            stamp.localScale = Vector3.one * Mathf.Lerp(1.9f, 1f, HudTheme.EaseOutCubic(s));
            stamp.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-28f, -12f, s));
            stamp.GetComponent<Image>().color = new Color(StampRed.r, StampRed.g, StampRed.b, Mathf.Clamp01(s * 3f));
        }

        // Coins count down in the wallet (and the chip hops).
        if (walletAt > 0f)
        {
            float w = Mathf.Clamp01((now - walletAt - 0.15f) / 0.6f);
            walletShown = Mathf.RoundToInt(Mathf.Lerp(walletFrom, walletTo, HudTheme.EaseOutCubic(w)));
            float hop = w < 1f ? Mathf.Sin(w * Mathf.PI) * 0.12f : 0f;
            walletChip.localScale = Vector3.one * (1f + hop);
            if (w >= 1f) walletAt = -10f;
        }
        else walletShown = Coins();
        walletText.text = walletShown.ToString();

        // BUY splits into NOT NOW / YES while confirming; a refusal shakes it.
        primaryRect.anchoredPosition = Vector2.Lerp(primaryRect.anchoredPosition, BuyAt(confirming), Damp(18f));
        if (secondaryButton.gameObject.activeSelf)
        {
            secondaryRect.anchoredPosition = Vector2.Lerp(secondaryRect.anchoredPosition, new Vector2(165f, -772f), Damp(18f));
            secondaryGroup.alpha = Mathf.Lerp(secondaryGroup.alpha, 1f, Damp(14f));
        }
        float k = now - shakeAt;
        primaryRect.localRotation = Quaternion.Euler(0f, 0f, k < 0.32f ? Mathf.Sin(k * 60f) * 3.5f * (1f - k / 0.32f) : 0f);
        // A glint crosses BUY every few seconds while it can be pressed.
        bool glint = primaryButton.interactable && !confirming;
        float g = Mathf.Repeat(t, 3.2f) / 0.7f;
        shine.gameObject.SetActive(glint && g < 1f);
        if (glint && g < 1f) shine.anchoredPosition = new Vector2(Mathf.Lerp(-60f, 480f, g * g * (3f - 2f * g)), 0f);

        // Ribbon bookmarks sway a little; the bookmark set flips them open.
        for (int i = 0; i < ribbons.Count; i++)
        {
            float r = Mathf.Clamp01((now - ribbonsAt - i * 0.08f) / 0.35f);
            bool flip = ribbonsAt > 0f && r < 1f;
            float swing = flip ? Mathf.Sin(r * Mathf.PI * 2f) * (1f - r) * 18f : Mathf.Sin(now * 1.3f + i * 1.7f) * 1.6f;
            float length = flip ? Mathf.LerpUnclamped(0.25f, 1f, HudTheme.EaseOutBack(r)) : 1f;
            ribbons[i].localScale = new Vector3(1f, length, 1f);
            ribbons[i].localRotation = Quaternion.Euler(0f, 0f, swing);
        }

        // Upgrade: the new slots' padlocks pop one after another.
        if (padlocksAt > 0f)
        {
            bool done = true;
            for (int i = unlockFrom; i < unlockTo && i < slotTiles.Count; i++)
            {
                float p = Mathf.Clamp01((now - padlocksAt - 0.25f - (i - unlockFrom) * 0.09f) / 0.22f);
                SlotPreview tile = slotTiles[i];
                tile.Padlock.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.9f, p);
                tile.Padlock.color = new Color(0.85f, 0.87f, 0.9f, 0.95f * (1f - p));
                tile.Rect.localScale = Vector3.one * (p > 0f && p < 1f ? 1f + Mathf.Sin(p * Mathf.PI) * 0.18f : 1f);
                if (p >= 1f) tile.Face.sprite = HudIconArt.Get("slot");
                else done = false;
            }
            if (done) padlocksAt = -10f;
        }

        UpdateFlight(now);
    }

    private void UpdateFlight(float now)
    {
        if (flyAt <= 0f) return;
        float f = (now - flyAt) / 0.5f;
        if (f < 0f) return;
        if (f >= 1f)
        {
            flyer.gameObject.SetActive(false);
            flyAt = -10f;
            if (hotbar != null) hotbar.CelebrateArrival();
            if (closingAt < 0f && afterFlight != null) { Close(); }
            return;
        }
        flyer.gameObject.SetActive(true);
        float e = f * f * (3f - 2f * f);
        Vector2 screen = Vector2.Lerp(flyFrom, flyTo, e) + Vector2.up * (Mathf.Sin(f * Mathf.PI) * 120f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 local);
        flyer.anchorMin = flyer.anchorMax = new Vector2(0.5f, 0.5f);
        flyer.anchoredPosition = local;
        flyer.localScale = Vector3.one * Mathf.Lerp(1f, 0.45f, e) * BookScale();
        flyer.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(f * Mathf.PI * 2f) * 10f);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static RectTransform Box(RectTransform parent, string name, float x, float y, float w, float h, Color color, float radius = 0f)
    {
        RectTransform rect = HudTheme.PlaceTopLeft(HudTheme.CreateRect(name, parent), x, y, w, h);
        HudTheme.AddImage(rect, radius > 0f ? HudTheme.Rounded : null, color, radius);
        return rect;
    }

    private static RectTransform Coin(RectTransform parent, float x, float y, float size)
    {
        RectTransform coin = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Coin", parent), x, y, size, size);
        HudTheme.AddImage(coin, HudTheme.Circle, HudTheme.Exp);
        RectTransform inner = HudTheme.CreateRect("Inner", coin);
        HudTheme.Stretch(inner, size * 0.18f);
        HudTheme.AddImage(inner, HudTheme.Circle, new Color32(214, 160, 70, 255));
        return coin;
    }

    private static TextMeshProUGUI Label(RectTransform parent, string name, string text, float x, float y, float w, float h, float size, Color color,
        TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
    {
        TextMeshProUGUI label = HudTheme.CreateLabel(name, parent, text, size, color, align, style);
        HudTheme.PlaceTopLeft(label.rectTransform, x, y, w, h);
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private static Button MakeButton(RectTransform parent, string name, float x, float y, float w, float h, string text, Color fill,
        UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = HudTheme.PlaceCentered(HudTheme.CreateRect(name, parent), x, y, w, h);
        Image face = HudTheme.AddImage(rect, HudTheme.Rounded, fill, 22f);
        face.raycastTarget = true;
        var rim = rect.gameObject.AddComponent<Outline>();
        rim.effectColor = HudTheme.Brass;
        rim.effectDistance = new Vector2(3f, -3f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.82f, 1f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.62f, 0.75f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);
        TextMeshProUGUI label = HudTheme.CreateLabel("Label", rect, text, 32f, HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(label.rectTransform, 10f);
        label.characterSpacing = 3f;
        HudPress.Add(rect);
        return button;
    }
}
