using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The Bag (Campaign Foundation T10): a navy-and-brass satchel panel with the
/// bag grid on top (13 tiles since T30; locked ones show a padlock), a copy of the
/// hotbar below (5 tiles) with the "Make room" hand in the last column, an
/// action bar, and a details side panel on the right that always shows the
/// chosen item (picture, name, category, count, what it is and where it
/// sits). The evidence journal is a page reached from the JOURNAL button.
/// The Desk Drawer left play on 2026-10-25: everything owned is carried.
///
/// TOUCH: tap an item to choose it (the side panel shows it), then tap a
/// slot (move, or swap when occupied); drag and drop does the same; press
/// and hold chooses it too and nudges the side panel; TO BAR / TO BAG sends
/// the chosen item between bag and bar; tap the chosen item again (or READ)
/// to read it. Back drops the choice and closes. Every action has a visible
/// button.
///
/// FEEL: the panel rises in and the tiles pop in a wave; tiles squash under
/// a finger, the chosen one lifts on a breathing brass ring, a move snaps
/// the tiles it touched, the side panel's paper slides in fresh for each new
/// item (its picture pops), and the panel fades out when it closes.
///
/// OWNERSHIP: every change is an inventory command through HudHotbar.Commit
/// (PlayerProgressManager.CommitInventory); this panel only renders the
/// snapshot. While it is open the player is held still (HudWindowLock) and
/// world taps are blocked by its backdrop; the review clock keeps running.
/// </summary>
public sealed class InventoryScreen : MonoBehaviour
{
    private const int SortingOrder = 2550;
    // T30: the Bag holds up to 13 (6 + upgrades), seven to a row; the bar copy uses the same tiles.
    private const float TileSize = 120f;
    private const float TileStep = 134f;
    private const int BagColumns = 7;
    private const float GridLeft = 52f;
    private const float ActionTop = 772f, ActionHeight = 84f, ActionLeft = 24f, ActionWidth = 976f, ActionGap = 12f;
    private const float SideLeft = 1024f, SideTop = 100f, SideWidth = 472f, SideHeight = 756f;
    private const string DefaultHelp = "Tap an item to see it, then tap a slot to move it";
    private const string MakeRoomHelp = "No room! Tap a slot to swap it in.";
    private const string SelectedHelp = "Tap a slot to move it · tap it again to read";
    private static readonly Color Warn = new Color32(255, 196, 120, 255);
    private static readonly Color CanvasBand = new Color32(43, 58, 76, 255);
    private static readonly Color Paper = new Color32(236, 226, 200, 255);
    private static readonly Color Ink = new Color32(52, 40, 26, 255);
    private static readonly Color InkSoft = new Color32(110, 90, 60, 255);
    private static readonly Color Category = new Color32(150, 116, 58, 255);
    private static readonly Color Needed = new Color32(168, 44, 40, 255);
    private static readonly Color ButtonFill = new Color32(16, 25, 38, 245);

    private HudHotbar hotbar;
    private RectTransform root;
    private CanvasGroup group;
    private RectTransform panel;
    private float panelScale = 1f;
    private TextMeshProUGUI title;
    private TextMeshProUGUI help;
    private TextMeshProUGUI bagHeading;
    private float helpWarnUntil, helpAt = -10f;
    private readonly List<InventoryTile> bagTiles = new List<InventoryTile>();
    private readonly List<InventoryTile> barTiles = new List<InventoryTile>();
    private InventoryTile handTile;
    private TextMeshProUGUI handLabel;
    private Button readButton, quickButton, backButton, pinButton;
    private readonly List<Button> actionButtons = new List<Button>();
    private TextMeshProUGUI pinLabel, quickLabel;

    // T12 bookmark pins: three ribbon tabs at the top of the bag (display preference only).
    private RectTransform pinStrip;
    private readonly List<RectTransform> pinTabs = new List<RectTransform>();
    private readonly List<Image> pinTabIcons = new List<Image>();
    private readonly List<TextMeshProUGUI> pinTabLabels = new List<TextMeshProUGUI>();
    private static readonly Color[] RibbonColors = { new Color32(150, 40, 46, 255), new Color32(46, 112, 74, 255), new Color32(44, 70, 128, 255) };
    private string pinPending;
    private float pinPulseAt = -10f;
    private TextMeshProUGUI backLabel;

    // Details side panel.
    private RectTransform sheet, sheetWell;
    private Vector2 sheetHome;
    private CanvasGroup sheetGroup;
    private RectTransform sheetFilled;
    private Image sheetIcon;
    private TextMeshProUGUI sheetTitle, sheetMeta, sheetBody, sheetPlace, sheetEmpty;
    private string shownItem;
    private int shownQuantity;
    private float sheetAt = -10f, sheetNudgeAt = -10f;

    // Overlays.
    private RectTransform journal;
    private CanvasGroup journalGroup;
    private TextMeshProUGUI journalBody;
    private float journalAt = -10f;
    private RectTransform ghost;
    private Image ghostImage;

    // State.
    private bool isOpen;
    private bool makeRoom;
    private SlotRef selected = SlotRef.None;
    private SlotRef target = SlotRef.None;
    private SlotRef cursor = SlotRef.None;
    private InventoryTile dragFrom;
    private float openedAt, closingAt = -1f;
    private readonly Dictionary<InventoryTile, float> punches = new Dictionary<InventoryTile, float>();

    public bool IsOpen => isOpen;

    /// <summary>The bag or a "Make room" panel is open (other panels wait).</summary>
    public static bool AnyOpen => HudHotbar.Instance != null && HudHotbar.Instance.Screen != null && HudHotbar.Instance.Screen.isOpen;

    private CampaignInventoryRecord Inv => hotbar.Inventory;
    private InventoryCatalogue Catalogue => hotbar.Catalogue;

    // =========================================================
    // BUILD
    // =========================================================

    public static InventoryScreen Create(Transform canvasRoot, HudHotbar owner)
    {
        RectTransform layer = HudTheme.CreateRect("Inventory Screen (T10)", canvasRoot);
        HudTheme.Stretch(layer);
        var canvas = layer.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;
        layer.gameObject.AddComponent<GraphicRaycaster>();
        InventoryScreen screen = layer.gameObject.AddComponent<InventoryScreen>();
        screen.hotbar = owner;
        screen.root = layer;
        screen.Build();
        layer.gameObject.SetActive(false);
        return screen;
    }

    private void Build()
    {
        group = root.gameObject.AddComponent<CanvasGroup>();

        RectTransform backdrop = HudTheme.CreateRect("Backdrop", root);
        HudTheme.Stretch(backdrop);
        Image shade = HudTheme.AddImage(backdrop, null, new Color(0.02f, 0.03f, 0.05f, 0.66f));
        shade.raycastTarget = true;

        RectTransform frame = HudTheme.CreateRect("Satchel", root);
        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.sizeDelta = new Vector2(1520f, 880f);
        panel = frame;
        RectTransform body = HudTheme.CreatePanel("Panel", frame, 24f);
        HudTheme.Stretch((RectTransform)body.parent);
        // A canvas band (the satchel's inside) behind the slots.
        RectTransform band = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Canvas Band", body), 24f, 96f, 976f, 664f);
        HudTheme.AddImage(band, HudTheme.Rounded, CanvasBand, 18f);

        // Header: BACK, the title, JOURNAL, and a brass rule under them.
        backButton = MakeButton(body, "Inventory Back", 24f, 16f, 200f, 68f, "BACK", OnBack, out backLabel);
        title = HudTheme.CreateLabel("Title", body, "BAG", 46f, HudTheme.BrassSoft, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.PlaceTopLeft(title.rectTransform, 380f, 16f, 760f, 68f);
        title.characterSpacing = 8f;
        MakeButton(body, "Journal", 1292f, 16f, 200f, 68f, "JOURNAL", OpenJournal, out _);
        RectTransform rule = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Rule", body), 24f, 88f, 1468f, 2f);
        HudTheme.AddImage(rule, null, new Color(HudTheme.Brass.r, HudTheme.Brass.g, HudTheme.Brass.b, 0.35f));

        // Bag grid: 13 tiles, 7 per row (names under the items).
        bagHeading = SectionLabel(body, "Bag Label", "BAG", GridLeft, 106f, 380f);
        for (int i = 0; i < CampaignInventoryRecord.MaxBag; i++)
        {
            int col = i % BagColumns, row = i / BagColumns;
            bagTiles.Add(InventoryTile.Create(body, this, SlotRef.Bag(i), GridLeft + col * TileStep, 160f + row * 176f, TileSize));
        }

        // The hotbar copy, styled like the HUD icons; the hand takes the last column.
        SectionLabel(body, "Bar Label", "BAR  <size=80%><color=#A8B4C0>ready to use</color></size>", GridLeft, 504f, 600f);
        for (int i = 0; i < CampaignInventoryRecord.MaxHotbar; i++)
            barTiles.Add(InventoryTile.Create(body, this, SlotRef.Hotbar(i), GridLeft + i * TileStep, 552f, TileSize));
        handTile = InventoryTile.Create(body, this, SlotRef.Hand, GridLeft + 5 * TileStep, 552f, TileSize);
        handLabel = HudTheme.CreateLabel("Hand Label", body, "IN HAND", 26f, Warn, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.PlaceTopLeft(handLabel.rectTransform, GridLeft + 5 * TileStep - 10f, 504f, TileSize + 20f, 40f);
        handLabel.characterSpacing = 3f;

        help = HudTheme.CreateLabel("Help", body, DefaultHelp, 26f, HudTheme.TextPrimary, TextAlignmentOptions.Center);
        help.fontSizeMin = 18f;
        HudTheme.PlaceTopLeft(help.rectTransform, GridLeft, 718f, BagColumns * TileStep - 20f, 38f);

        // Action bar: every action has a visible button (laid out to fill the row).
        readButton = MakeButton(body, "Read", 0f, ActionTop, 180f, ActionHeight, "READ", OnRead, out _);
        quickButton = MakeButton(body, "Quick Move", 0f, ActionTop, 180f, ActionHeight, "TO BAR", OnQuickMove, out quickLabel);
        pinButton = MakeButton(body, "Pin", 0f, ActionTop, 180f, ActionHeight, "PIN", OnPin, out pinLabel);
        actionButtons.AddRange(new[] { readButton, quickButton, pinButton });
        BuildPinStrip(body);

        BuildSidePanel(body);
        BuildJournal(body);

        ghost = HudTheme.CreateRect("Drag Ghost", root);
        ghost.sizeDelta = new Vector2(TileSize, TileSize);
        ghostImage = ghost.gameObject.AddComponent<Image>();
        ghostImage.raycastTarget = false;
        ghostImage.preserveAspect = true;
        ghostImage.color = new Color(1f, 1f, 1f, 0.92f);
        var ghostShadow = ghost.gameObject.AddComponent<Shadow>();
        ghostShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        ghostShadow.effectDistance = new Vector2(8f, -12f);
        ghost.gameObject.SetActive(false);
    }

    /// <summary>The PINNED ribbons at the right of the BAG heading: icon and name on each (never colour alone).</summary>
    private void BuildPinStrip(RectTransform body)
    {
        const float width = 160f, gap = 12f;
        float left = GridLeft + BagColumns * TileStep - 20f - (3 * width + 2 * gap);
        pinStrip = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Pinned Ribbons", body), left, 102f, 3 * width + 2 * gap, 48f);
        for (int i = 0; i < CampaignInventoryRecord.MaxPins; i++)
        {
            RectTransform tab = HudTheme.PlaceCentered(HudTheme.CreateRect("Ribbon " + (i + 1), pinStrip), i * (width + gap), 0f, width, 48f);
            Image face = HudTheme.AddImage(tab, HudTheme.Rounded, RibbonColors[i], 10f);
            face.raycastTarget = true;
            var shadow = tab.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
            shadow.effectDistance = new Vector2(0f, -3f);
            RectTransform icon = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Icon", tab), 6f, 5f, 38f, 38f);
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            TextMeshProUGUI label = HudTheme.CreateLabel("Name", tab, "", 20f, HudTheme.TextPrimary, TextAlignmentOptions.Left, FontStyles.Bold);
            label.fontSizeMin = 13f;
            HudTheme.PlaceTopLeft(label.rectTransform, 50f, 6f, 104f, 36f);
            var button = tab.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            int index = i;
            button.onClick.AddListener(() => OnPinTab(index));
            pinTabs.Add(tab);
            pinTabIcons.Add(iconImage);
            pinTabLabels.Add(label);
        }
    }

    /// <summary>The DETAILS column: a paper sheet that describes the chosen item, or says how to choose one.</summary>
    private void BuildSidePanel(RectTransform body)
    {
        RectTransform side = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Details Panel", body), SideLeft, SideTop, SideWidth, SideHeight);
        HudTheme.AddImage(side, HudTheme.Rounded, CanvasBand, 18f);
        TextMeshProUGUI heading = HudTheme.CreateLabel("Heading", side, "DETAILS", 30f, HudTheme.BrassSoft, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.PlaceTopLeft(heading.rectTransform, 0f, 12f, SideWidth, 48f);
        heading.characterSpacing = 4f;

        sheet = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Sheet", side), 16f, 70f, SideWidth - 32f, SideHeight - 86f);
        sheetHome = sheet.anchoredPosition;
        HudTheme.AddImage(sheet, HudTheme.Rounded, Paper, 18f);
        var shadow = sheet.gameObject.AddComponent<Shadow>();
        shadow.effectColor = HudTheme.ShadowColor;
        shadow.effectDistance = new Vector2(4f, -8f);
        sheetGroup = sheet.gameObject.AddComponent<CanvasGroup>();
        float w = SideWidth - 32f;

        sheetFilled = HudTheme.CreateRect("Item", sheet);
        HudTheme.Stretch(sheetFilled);
        sheetWell = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Icon Well", sheetFilled), (w - 184f) * 0.5f, 26f, 184f, 184f);
        HudTheme.AddImage(sheetWell, HudTheme.Rounded, new Color32(214, 200, 170, 255), 24f);
        RectTransform icon = HudTheme.CreateRect("Icon", sheetWell);
        HudTheme.Stretch(icon, 10f);
        sheetIcon = icon.gameObject.AddComponent<Image>();
        sheetIcon.preserveAspect = true;
        sheetIcon.raycastTarget = false;
        sheetTitle = HudTheme.CreateLabel("Title", sheetFilled, "", 36f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        sheetTitle.fontSizeMin = 22f;
        HudTheme.PlaceTopLeft(sheetTitle.rectTransform, 20f, 224f, w - 40f, 52f);
        sheetMeta = HudTheme.CreateLabel("Category", sheetFilled, "", 22f, Category, TextAlignmentOptions.Center, FontStyles.Bold);
        sheetMeta.fontSizeMin = 16f;
        HudTheme.PlaceTopLeft(sheetMeta.rectTransform, 20f, 278f, w - 40f, 32f);
        sheetMeta.characterSpacing = 2f;
        RectTransform line = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Rule", sheetFilled), 40f, 322f, w - 80f, 2f);
        HudTheme.AddImage(line, null, new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.4f));
        sheetBody = HudTheme.CreateLabel("Body", sheetFilled, "", 25f, Ink, TextAlignmentOptions.TopLeft);
        sheetBody.textWrappingMode = TextWrappingModes.Normal;
        sheetBody.enableAutoSizing = true;
        sheetBody.fontSizeMin = 17f;
        sheetBody.fontSizeMax = 25f;
        HudTheme.PlaceTopLeft(sheetBody.rectTransform, 28f, 338f, w - 56f, 222f);
        sheetPlace = HudTheme.CreateLabel("Where", sheetFilled, "", 23f, InkSoft, TextAlignmentOptions.Center, FontStyles.Bold);
        sheetPlace.textWrappingMode = TextWrappingModes.Normal;
        sheetPlace.enableAutoSizing = true;
        sheetPlace.fontSizeMin = 16f;
        sheetPlace.fontSizeMax = 23f;
        HudTheme.PlaceTopLeft(sheetPlace.rectTransform, 24f, 574f, w - 48f, 76f);

        sheetEmpty = HudTheme.CreateLabel("Empty", sheet, "Tap an item to see what it is.", 28f, InkSoft, TextAlignmentOptions.Center, FontStyles.Italic);
        sheetEmpty.textWrappingMode = TextWrappingModes.Normal;
        HudTheme.PlaceTopLeft(sheetEmpty.rectTransform, 30f, 260f, w - 60f, 140f);
    }

    private void BuildJournal(RectTransform body)
    {
        journal = HudTheme.CreateRect("Evidence Journal", body);
        HudTheme.Stretch(journal, 16f);
        journalGroup = journal.gameObject.AddComponent<CanvasGroup>();
        Image page = HudTheme.AddImage(journal, HudTheme.Rounded, Paper, 20f);
        page.raycastTarget = true;
        TextMeshProUGUI heading = HudTheme.CreateLabel("Heading", journal, "EVIDENCE JOURNAL", 40f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.PlaceTopLeft(heading.rectTransform, 300f, 18f, 888f, 60f);
        heading.characterSpacing = 4f;
        TextMeshProUGUI rule = HudTheme.CreateLabel("Rule", journal,
            "Only what you have learned, and where it came from. It never answers anything.", 22f,
            InkSoft, TextAlignmentOptions.Center, FontStyles.Italic);
        HudTheme.PlaceTopLeft(rule.rectTransform, 120f, 82f, 1248f, 36f);
        journalBody = HudTheme.CreateLabel("Entries", journal, "", 28f, Ink, TextAlignmentOptions.TopLeft);
        journalBody.textWrappingMode = TextWrappingModes.Normal;
        journalBody.enableAutoSizing = true;
        journalBody.fontSizeMin = 18f;
        journalBody.lineSpacing = 12f;
        HudTheme.PlaceTopLeft(journalBody.rectTransform, 80f, 140f, 1328f, 680f);
        MakeButton(journal, "Close Journal", 24f, 16f, 260f, 64f, "BACK TO BAG", () => journal.gameObject.SetActive(false), out _);
        journal.gameObject.SetActive(false);
    }

    private static TextMeshProUGUI SectionLabel(RectTransform parent, string name, string text, float x, float y, float width)
    {
        TextMeshProUGUI label = HudTheme.CreateLabel(name, parent, text, 30f, HudTheme.BrassSoft, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        HudTheme.PlaceTopLeft(label.rectTransform, x, y, width, 40f);
        label.characterSpacing = 4f;
        return label;
    }

    private static Button MakeButton(RectTransform parent, string name, float x, float y, float width, float height, string text,
        UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI label)
    {
        RectTransform rect = HudTheme.PlaceCentered(HudTheme.CreateRect(name, parent), x, y, width, height);
        Image face = HudTheme.AddImage(rect, HudTheme.Rounded, ButtonFill, 18f);
        face.raycastTarget = true;
        var rim = rect.gameObject.AddComponent<Outline>();
        rim.effectColor = HudTheme.Brass;
        rim.effectDistance = new Vector2(3f, -3f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.8f, 1f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.5f);
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        button.onClick.AddListener(onClick);
        label = HudTheme.CreateLabel("Label", rect, text, 30f, HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(label.rectTransform, 8f);
        label.characterSpacing = 3f;
        HudPress.Add(rect);
        return button;
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    /// <summary>Opens the bag (target: the empty hotbar slot that was tapped; selectItem: preselected item).</summary>
    public void Open(SlotRef targetSlot, string selectItem)
    {
        if (!isOpen)
        {
            isOpen = true;
            openedAt = Time.unscaledTime;
            closingAt = -1f;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            group.blocksRaycasts = true;
            HudWindowLock.Acquire(this);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_ui_panel_open_01");
            SlotRef.TryParse(Inv.selectedSlot, out cursor);
            journal.gameObject.SetActive(false);
            shownItem = null;
            FitToScreen();
        }
        target = targetSlot.Area == InventoryArea.Hotbar ? targetSlot : SlotRef.None;
        selected = SlotRef.None;
        if (!string.IsNullOrEmpty(selectItem))
        {
            SlotRef at = InventoryRules.Find(Inv, selectItem);
            if (!at.IsNone) selected = at;
        }
        makeRoom = !Inv.hand.IsEmpty;
        if (makeRoom) selected = SlotRef.Hand;
        SetHelp(target.IsNone ? (makeRoom ? MakeRoomHelp : DefaultHelp) : "Tap an item to put it in this slot.", false);
        Rebuild();
    }

    /// <summary>A pickup did not fit: the item waits in the hand under the heading "Make room".</summary>
    public void OpenMakeRoom()
    {
        if (!isOpen) Open(SlotRef.None, null);
        makeRoom = true;
        selected = SlotRef.Hand;
        SetHelp(MakeRoomHelp, false);
        Rebuild();
    }

    private void OnBack()
    {
        if (journal.gameObject.activeSelf) { journal.gameObject.SetActive(false); return; }
        // Back drops a selection first (not an item in hand).
        if ((!selected.IsNone && selected.Area != InventoryArea.Hand) || !target.IsNone)
        {
            selected = SlotRef.None;
            target = SlotRef.None;
            Rebuild();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_ui_back_01");
        }
        Close(force: false);
    }

    /// <summary>
    /// Closes the panel. An item still in hand goes back to its world source
    /// (or a free slot); a required one cannot be left behind, so the panel
    /// stays until it is placed. force (a dialogue or card took over): the
    /// hand is resolved by the inventory's own never-stuck rule. The panel is
    /// closed at once (input returns); only its fade-out plays on.
    /// </summary>
    public bool Close(bool force)
    {
        if (!isOpen) return true;
        if (!Inv.hand.IsEmpty)
        {
            string held = Inv.hand.itemId;
            InventoryResult released = hotbar.Commit(r => InventoryRules.ReleaseHand(r, Catalogue, true, HudHotbar.NewAction("release/" + held)));
            if (released.Outcome == InventoryOutcome.Rejected || released.Outcome == InventoryOutcome.SaveFailed)
            {
                if (!force)
                {
                    SetHelp(released.Outcome == InventoryOutcome.Rejected
                        ? $"{Catalogue.Label(held)}: {(Catalogue.IsRequired(held) ? InventoryRules.NeededForCase : released.Message)}. Place it in a slot first."
                        : "That could not be saved just now. Try again.", true);
                    return false;
                }
                if (PlayerProgressManager.Instance != null) PlayerProgressManager.Instance.NormalizeInventory(Catalogue);
            }
            else if (released.Changed && released.Location.IsNone) hotbar.ReturnedToWorld(held);
        }
        hotbar.Commit(r => InventoryRules.SetSelected(r, cursor));
        isOpen = false;
        makeRoom = false;
        selected = SlotRef.None;
        target = SlotRef.None;
        EndDragVisual();
        closingAt = Time.unscaledTime;
        group.blocksRaycasts = false;
        HudWindowLock.Release(this);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_ui_panel_close_01");
        return true;
    }

    private void OnDisable()
    {
        closingAt = -1f;
        if (isOpen)
        {
            isOpen = false;
            HudWindowLock.Release(this);
        }
    }

    private void FitToScreen()
    {
        Rect area = root.rect;
        float scale = Mathf.Min(1f, (area.width - 32f) / panel.sizeDelta.x, (area.height - 32f) / panel.sizeDelta.y);
        panelScale = Mathf.Max(0.5f, scale);
        panel.localScale = Vector3.one * panelScale;
    }

    // =========================================================
    // UPDATE (motion, hold, guards)
    // =========================================================

    private void Update()
    {
        float now = Time.unscaledTime;
        if (closingAt > 0f)
        {
            // Fade and sink away; the panel already let go of the player.
            float c = HudTheme.EaseOutCubic(Mathf.Clamp01((now - closingAt) / 0.18f));
            group.alpha = 1f - c;
            panel.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -40f, c));
            panel.localScale = Vector3.one * (panelScale * Mathf.Lerp(1f, 0.96f, c));
            if (c >= 1f)
            {
                closingAt = -1f;
                root.gameObject.SetActive(false);
            }
            return;
        }
        if (!isOpen) return;
        // A conversation, card or settings takes over: close without stranding anything.
        if (DialogueUI.IsAnyDialogueActive || HudRewardScreen.AnyOpen || InGameSettings.IsOpen)
        {
            Close(force: true);
            return;
        }
        float t = now - openedAt;
        group.alpha = Mathf.Clamp01(t / 0.18f);
        float slide = HudTheme.EaseOutBack(Mathf.Clamp01(t / 0.34f));
        panel.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(-80f, 0f, slide));
        panel.localScale = Vector3.one * (panelScale * Mathf.LerpUnclamped(0.95f, 1f, slide));
        for (int i = 0; i < bagTiles.Count; i++) bagTiles[i].Fan(t - 0.08f - (i % BagColumns) * 0.03f - (i / BagColumns) * 0.05f);
        for (int i = 0; i < barTiles.Count; i++) barTiles[i].Fan(t - 0.16f - i * 0.03f);
        handTile.Fan(t - 0.2f);
        foreach (KeyValuePair<InventoryTile, float> punch in punches) punch.Key.Punch(now - punch.Value);
        // Choosing a ribbon to replace: the three tabs breathe until one is tapped.
        for (int i = 0; i < pinTabs.Count; i++)
        {
            float wave = pinPending != null ? 1f + Mathf.Sin((now - pinPulseAt) * 6f + i) * 0.06f : 1f;
            float snap = now - pinPulseAt < 0.25f && pinPending == null ? 1f + Mathf.Sin((now - pinPulseAt) / 0.25f * Mathf.PI) * 0.12f : 1f;
            pinTabs[i].localScale = Vector3.one * wave * snap;
        }
        // The hand's label pulses while something waits to be placed.
        if (handLabel.gameObject.activeSelf) handLabel.alpha = 0.7f + Mathf.Sin(now * 5f) * 0.3f;
        // The details sheet slides in fresh for each new item, its picture pops, and a hold nudges it.
        float s = Mathf.Clamp01((now - sheetAt) / 0.24f);
        sheetGroup.alpha = Mathf.Lerp(0.35f, 1f, HudTheme.EaseOutCubic(s));
        sheet.anchoredPosition = sheetHome + new Vector2((1f - HudTheme.EaseOutCubic(s)) * 28f, 0f);
        float n = now - sheetNudgeAt;
        sheet.localScale = Vector3.one * (n < 0.26f ? 1f + Mathf.Sin(n / 0.26f * Mathf.PI) * 0.035f : 1f);
        sheetWell.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, HudTheme.EaseOutBack(Mathf.Clamp01((now - sheetAt - 0.04f) / 0.28f)));
        // A new help line slides up into place.
        float h = HudTheme.EaseOutCubic(Mathf.Clamp01((now - helpAt) / 0.22f));
        help.rectTransform.anchoredPosition = new Vector2(GridLeft, -718f - (1f - h) * 12f);
        help.alpha = h;
        // The journal pops in.
        if (journal.gameObject.activeSelf)
        {
            float j = Mathf.Clamp01((now - journalAt) / 0.24f);
            journalGroup.alpha = Mathf.Clamp01(j * 2f);
            journal.localScale = Vector3.one * Mathf.LerpUnclamped(0.96f, 1f, HudTheme.EaseOutCubic(j));
        }
        if (ghost.gameObject.activeSelf) ghost.localRotation = Quaternion.Euler(0f, 0f, -6f + Mathf.Sin(now * 8f) * 2f);
        if (helpWarnUntil > 0f && now > helpWarnUntil)
        {
            helpWarnUntil = 0f;
            SetHelp(makeRoom ? MakeRoomHelp : DefaultHelp, false);
        }
    }

    // =========================================================
    // RENDER
    // =========================================================

    private void Rebuild()
    {
        CampaignInventoryRecord r = Inv;
        title.text = makeRoom ? "MAKE ROOM" : "BAG";
        title.color = makeRoom ? Warn : HudTheme.BrassSoft;
        int used = 0;
        for (int i = 0; i < bagTiles.Count; i++)
        {
            InventorySlot slot = i < r.bagCapacity ? Slot(r.bag, i) : null;
            if (slot != null && !slot.IsEmpty) used++;
            bagTiles[i].Show(slot, i >= r.bagCapacity, false, Catalogue);
        }
        bagHeading.text = $"BAG  <size=80%><color=#A8B4C0>{used}/{r.bagCapacity}</color></size>";
        for (int i = 0; i < barTiles.Count; i++)
            barTiles[i].Show(i < r.hotbarCapacity ? Slot(r.hotbar, i) : null, i >= r.hotbarCapacity, true, Catalogue);
        bool handShown = makeRoom || !r.hand.IsEmpty;
        handTile.gameObject.SetActive(handShown);
        handLabel.gameObject.SetActive(handShown);
        if (handShown) handTile.Show(r.hand, false, true, Catalogue);

        foreach (InventoryTile tile in AllTiles())
        {
            tile.SetSelected(!selected.IsNone && tile.Slot == selected);
            tile.SetTargeted(!target.IsNone && tile.Slot == target);
            tile.SetCursor(!cursor.IsNone && tile.Slot == cursor && selected.IsNone);
        }

        bool hasSlotItem = !selected.IsNone && InventoryRules.IsUsable(r, selected) && !InventoryRules.SlotAt(r, selected).IsEmpty;
        InventorySlot chosenSlot = hasSlotItem ? InventoryRules.SlotAt(r, selected) : null;
        string chosen = chosenSlot?.itemId;
        readButton.interactable = !string.IsNullOrEmpty(chosen);
        quickButton.interactable = hasSlotItem;
        quickLabel.text = hasSlotItem && selected.Area == InventoryArea.Hotbar ? "TO BAG" :
            hasSlotItem && selected.Area == InventoryArea.Hand ? "PLACE" : "TO BAR";
        backLabel.text = r.hand.IsEmpty || !Catalogue.IsRequired(r.hand.itemId) ? "BACK" : "PLACE IT";
        RebuildPins(r, chosen);
        LayoutActions();
        ShowSheet(chosen, chosenSlot != null ? chosenSlot.quantity : 0, selected.Area);
    }

    /// <summary>The shown action buttons share the row evenly (PIN appears with the bookmark set).</summary>
    private void LayoutActions()
    {
        var shown = actionButtons.FindAll(b => b.gameObject.activeSelf);
        if (shown.Count == 0) return;
        float width = (ActionWidth - (shown.Count - 1) * ActionGap) / shown.Count;
        for (int i = 0; i < shown.Count; i++)
            HudTheme.PlaceCentered((RectTransform)shown[i].transform, ActionLeft + i * (width + ActionGap), ActionTop, width, ActionHeight);
    }

    private static InventorySlot Slot(List<InventorySlot> slots, int index) => index < slots.Count ? slots[index] : null;

    private IEnumerable<InventoryTile> AllTiles()
    {
        foreach (InventoryTile tile in bagTiles) yield return tile;
        foreach (InventoryTile tile in barTiles) yield return tile;
        yield return handTile;
    }

    /// <summary>Fills the details sheet; a different item (or none) slides a fresh sheet in.</summary>
    private void ShowSheet(string itemId, int quantity, InventoryArea area)
    {
        InventoryItemDefinition def = string.IsNullOrEmpty(itemId) ? null : Catalogue.Find(itemId);
        string shown = def != null ? itemId : null;
        if (shown != shownItem) sheetAt = Time.unscaledTime;
        else if (quantity != shownQuantity) sheetNudgeAt = Time.unscaledTime;
        shownItem = shown;
        shownQuantity = quantity;
        sheetFilled.gameObject.SetActive(def != null);
        sheetEmpty.gameObject.SetActive(def == null);
        if (def == null)
        {
            sheetEmpty.text = makeRoom ? "Tap the item in your hand to see what it is." : "Tap an item to see what it is.";
            return;
        }
        sheetIcon.sprite = HudIconArt.Get(!string.IsNullOrEmpty(def.icon) ? def.icon : "papers");
        sheetTitle.text = def.label;
        string count = quantity > 1 ? $"  ·  ×{quantity}" + (def.stackLimit > 1 ? $" of {def.stackLimit}" : "") : "";
        sheetMeta.text = def.category.ToUpperInvariant() + count;
        string holds = "";
        InventoryItemRecord record = InventoryRules.Record(Inv, itemId);
        if (def.container && record != null && record.contents.Count > 0)
            holds = itemId == InventoryCatalogue.ProfileBinder ? $"\n\nHolds {record.contents.Count} profile card{(record.contents.Count == 1 ? "" : "s")}."
                : itemId == InventoryCatalogue.ReviewedFile ? "\n\nHolds his completed entries, his ID card and his background sheet."
                : "\n\nHolds the case papers" + (record.contents.Exists(c => c.EndsWith("/client-copy")) ? " and the client copy." : ".");
        sheetBody.text = def.description + holds;
        string where = area == InventoryArea.Hotbar ? "In your bar: ready to use." :
            area == InventoryArea.Hand ? "In your hand: tap a slot to place it." : "In your bag.";
        sheetPlace.text = def.reference ? $"<color=#{ColorUtility.ToHtmlStringRGB(Needed)}>✔ Reference: read only, always kept.</color>\n{where}"
            : def.required ? $"<color=#{ColorUtility.ToHtmlStringRGB(Needed)}>✔ Needed for your case: always carried.</color>\n{where}" : where;
    }

    private void SetHelp(string text, bool warn)
    {
        if (help == null) return;
        if (help.text != text) helpAt = Time.unscaledTime;
        help.text = text;
        help.color = warn ? Warn : HudTheme.TextPrimary;
        helpWarnUntil = warn ? Time.unscaledTime + 2.8f : 0f;
    }

    // =========================================================
    // INPUT
    // =========================================================

    internal void TileTapped(InventoryTile tile)
    {
        CampaignInventoryRecord r = Inv;
        if (tile.Locked)
        {
            SetHelp(InventoryRules.ShopUpgrade + ": buy more slots at the Supply Counter.", true);
            tile.Shake();
            return;
        }
        SlotRef slot = tile.Slot;
        InventorySlot content = InventoryRules.SlotAt(r, slot);
        bool hasItem = content != null && !content.IsEmpty;
        if (slot.Area == InventoryArea.Hand)
        {
            // The held item is placed by tapping a slot; tapping it again only keeps it selected.
            if (hasItem) { selected = SlotRef.Hand; Rebuild(); }
            return;
        }
        if (!target.IsNone && hasItem && slot != target)
        {
            SlotRef into = target;
            target = SlotRef.None;
            Run(rec => InventoryRules.Move(rec, Catalogue, slot, into, HudHotbar.NewAction("move")));
            return;
        }
        if (selected.IsNone)
        {
            if (!hasItem) { SetHelp("That slot is empty. Tap an item first.", false); return; }
            selected = slot;
            cursor = slot.IsSlot ? slot : cursor;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_ui_select_01", 0.5f);
            SetHelp(SelectedHelp, false);
            Rebuild();
            return;
        }
        if (selected == slot)
        {
            OnRead();
            return;
        }
        SlotRef from = selected;
        Run(rec => InventoryRules.Move(rec, Catalogue, from, slot, HudHotbar.NewAction("move")));
    }

    /// <summary>Press and hold: chooses the item (no move) and nudges the details sheet toward the finger.</summary>
    internal void TileHeld(InventoryTile tile)
    {
        InventorySlot content = InventoryRules.SlotAt(Inv, tile.Slot);
        if (tile.Locked || content == null || content.IsEmpty) return;
        if (selected != tile.Slot)
        {
            selected = tile.Slot;
            if (tile.Slot.IsSlot) cursor = tile.Slot;
            SetHelp(SelectedHelp, false);
        }
        sheetNudgeAt = Time.unscaledTime;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_paper_place_01", 0.5f);
        Rebuild();
    }

    internal bool CanDrag(InventoryTile tile)
    {
        if (tile.Locked) return false;
        InventorySlot content = InventoryRules.SlotAt(Inv, tile.Slot);
        return content != null && !content.IsEmpty;
    }

    internal void BeginDrag(InventoryTile tile, Vector2 screen)
    {
        dragFrom = tile;
        ghostImage.sprite = tile.IconSprite;
        ghost.gameObject.SetActive(ghostImage.sprite != null);
        ghost.SetAsLastSibling();
        MoveGhost(screen);
        selected = tile.Slot;
        Rebuild();
        tile.SetDragging(true);
    }

    internal void Drag(Vector2 screen) => MoveGhost(screen);

    private void MoveGhost(Vector2 screen)
    {
        if (!ghost.gameObject.activeSelf) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 local);
        ghost.anchorMin = ghost.anchorMax = new Vector2(0.5f, 0.5f);
        ghost.anchoredPosition = local;
        ghost.localScale = Vector3.one * panelScale * 1.12f;
    }

    internal void EndDrag(PointerEventData eventData)
    {
        InventoryTile from = dragFrom;
        EndDragVisual();
        if (from == null) return;
        from.SetDragging(false);
        GameObject hit = eventData.pointerCurrentRaycast.gameObject;
        InventoryTile onto = hit != null ? hit.GetComponentInParent<InventoryTile>() : null;
        if (onto != null && onto != from && onto.Slot.IsSlot && !onto.Locked)
        {
            SlotRef source = from.Slot, to = onto.Slot;
            Run(rec => InventoryRules.Move(rec, Catalogue, source, to, HudHotbar.NewAction("move")));
            return;
        }
        if (onto != null && onto.Locked) { SetHelp(InventoryRules.ShopUpgrade + ": buy more slots at the Supply Counter.", true); onto.Shake(); return; }
        Rebuild();
    }

    private void EndDragVisual()
    {
        if (dragFrom != null) dragFrom.SetDragging(false);
        dragFrom = null;
        if (ghost != null) ghost.gameObject.SetActive(false);
    }

    private void OnRead()
    {
        CampaignInventoryRecord r = Inv;
        string item = !selected.IsNone && InventoryRules.IsUsable(r, selected) ? InventoryRules.SlotAt(r, selected).itemId : null;
        if (string.IsNullOrEmpty(item)) return;
        if (!Close(force: false)) return;
        if (!hotbar.OpenViewer(item))
            hotbar.OpenInventory(SlotRef.None, item);
    }

    private void OnQuickMove()
    {
        if (selected.IsNone) return;
        SlotRef from = selected;
        Run(rec => InventoryRules.QuickMove(rec, Catalogue, from, HudHotbar.NewAction("quick")));
    }

    // =========================================================
    // BOOKMARK PINS (T12)
    // =========================================================

    private void RebuildPins(CampaignInventoryRecord r, string selectedItem)
    {
        bool owned = InventoryRules.HasBookmarks(r);
        pinStrip.gameObject.SetActive(owned);
        pinButton.gameObject.SetActive(owned);
        if (!owned) { pinPending = null; return; }
        for (int i = 0; i < pinTabs.Count; i++)
        {
            string id = i < r.pins.Count ? r.pins[i] : null;
            InventoryItemDefinition def = id != null ? Catalogue.Find(id) : null;
            pinTabIcons[i].enabled = def != null;
            if (def != null) pinTabIcons[i].sprite = HudIconArt.Get(def.icon);
            pinTabLabels[i].text = def != null ? def.label : "(empty)";
            pinTabLabels[i].alpha = def != null ? 1f : 0.6f;
        }
        bool pinned = selectedItem != null && r.pins.Contains(selectedItem);
        pinLabel.text = pinned ? "UNPIN" : "PIN";
        pinButton.interactable = selectedItem != null && (pinned || InventoryRules.IsPinnable(r, Catalogue, selectedItem));
    }

    private void OnPin()
    {
        CampaignInventoryRecord r = Inv;
        string item = !selected.IsNone && InventoryRules.IsUsable(r, selected) ? InventoryRules.SlotAt(r, selected).itemId : null;
        if (string.IsNullOrEmpty(item)) return;
        InventoryResult result = r.pins.Contains(item)
            ? hotbar.Commit(rec => InventoryRules.Unpin(rec, item))
            : hotbar.Commit(rec => InventoryRules.Pin(rec, Catalogue, item));
        if (result.Outcome == InventoryOutcome.Rejected && result.Message == InventoryRules.PinReplace)
        {
            pinPending = item;
            pinPulseAt = Time.unscaledTime;
            SetHelp("All three ribbons are in use. Tap one to replace it.", false);
            return;
        }
        FinishPin(result);
    }

    /// <summary>A ribbon tab: replaces it while choosing, otherwise opens that record (a shortcut to read it).</summary>
    private void OnPinTab(int index)
    {
        CampaignInventoryRecord r = Inv;
        if (pinPending != null)
        {
            string item = pinPending;
            pinPending = null;
            FinishPin(hotbar.Commit(rec => InventoryRules.Pin(rec, Catalogue, item, index)));
            return;
        }
        if (index >= r.pins.Count) { SetHelp("Select a record, then tap PIN to keep it here.", false); return; }
        string pinned = r.pins[index];
        if (!Close(force: false)) return;
        if (!hotbar.OpenViewer(pinned)) hotbar.OpenInventory(SlotRef.None, pinned);
    }

    private void FinishPin(InventoryResult result)
    {
        if (result.Changed)
        {
            // One soft page/ribbon snap per pin change.
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_page_turn_01", 0.8f);
            pinPulseAt = Time.unscaledTime;
            SetHelp(Inv.pins.Contains(result.ItemId) ? "Pinned. Tap its ribbon to open it quickly." : "Unpinned. The record is still in your bag.", false);
        }
        else if (result.Outcome == InventoryOutcome.Rejected) SetHelp(result.Message, true);
        else if (result.Outcome == InventoryOutcome.SaveFailed) SetHelp("That could not be saved just now. Try again.", true);
        Rebuild();
    }


    private void OpenJournal()
    {
        CampaignInventoryRecord r = Inv;
        List<InventoryJournalFact> facts = InventoryRules.Journal(Catalogue, hotbar.LearnedFacts());
        if (facts.Count == 0) journalBody.text = "Nothing learned yet. Facts you learn while you work appear here, with the record they came from.";
        else
        {
            var lines = new System.Text.StringBuilder();
            foreach (InventoryJournalFact fact in facts)
            {
                bool isNew = !r.journalSeenFactIds.Contains(fact.id);
                lines.Append(isNew ? "<color=#B0602A><b>NEW</b></color>  " : "•  ");
                lines.Append(fact.text);
                if (!string.IsNullOrEmpty(fact.sourceItemId)) lines.Append($"  <color=#7A6440><i>Source: {Catalogue.Label(fact.sourceItemId)}</i></color>");
                lines.Append('\n');
            }
            journalBody.text = lines.ToString();
            var ids = facts.ConvertAll(f => f.id);
            hotbar.Commit(rec => InventoryRules.MarkJournalSeen(rec, ids));
        }
        journal.gameObject.SetActive(true);
        journal.SetAsLastSibling();
        journalAt = Time.unscaledTime;
        journalGroup.alpha = 0f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_page_turn_01", 0.7f);
    }

    /// <summary>Runs a command; success snaps the tiles and plays one paper cue, a refusal says why (no sound).</summary>
    private void Run(Func<CampaignInventoryRecord, InventoryResult> command)
    {
        InventoryResult result = hotbar.Commit(command);
        if (result.Changed)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_paper_place_01");
            hotbar.NoteResult(result);
            Snap(result.Location);
            Snap(result.DisplacedTo);
            if (result.Location.IsSlot) cursor = result.Location;
            selected = SlotRef.None;
            if (makeRoom && Inv.hand.IsEmpty)
            {
                makeRoom = false;
                SetHelp("Done. Your bag is sorted.", false);
            }
            else if (makeRoom) selected = SlotRef.Hand;
            else SetHelp(DefaultHelp, false);
        }
        else if (result.Outcome == InventoryOutcome.Rejected) SetHelp(result.Message, true);
        else if (result.Outcome == InventoryOutcome.SaveFailed) SetHelp("That could not be saved just now. Try again.", true);
        Rebuild();
    }

    private void Snap(SlotRef slot)
    {
        if (slot.IsNone) return;
        foreach (InventoryTile tile in AllTiles())
            if (tile.Slot == slot) punches[tile] = Time.unscaledTime;
    }
}

/// <summary>One tile of the Bag panel (bag, bar or hand): picture, count, seal, lock, selection.</summary>
public sealed class InventoryTile : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
    IPointerEnterHandler, IPointerExitHandler,
    IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public SlotRef Slot;
    public bool Locked { get; private set; }
    public Sprite IconSprite => icon != null && icon.enabled ? icon.sprite : null;

    private InventoryScreen screen;
    private RectTransform content;
    private Image frame, icon, seal, padlock, selection, targetRing;
    private TextMeshProUGUI count, label;
    private bool pressing, held, dragging, over, selectedOn;
    private float pressedAt, shakeAt = -10f;
    private float fanScale = 1f, punchScale = 1f, pressScale = 1f, lift, liftGoal;

    private const float RingInset = 8f, LiftHeight = 6f;

    public static InventoryTile Create(RectTransform parent, InventoryScreen screen, SlotRef slot, float x, float y, float size)
    {
        RectTransform rect = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Tile " + slot, parent), x, y, size, size);
        InventoryTile tile = rect.gameObject.AddComponent<InventoryTile>();
        tile.screen = screen;
        tile.Slot = slot;

        tile.selection = Fill(rect, "Selected", -RingInset, HudTheme.BrassSoft, 28f);
        tile.targetRing = Fill(rect, "Target", -RingInset, HudTheme.Active, 28f);
        tile.content = HudTheme.CreateRect("Content", rect);
        HudTheme.Stretch(tile.content);
        tile.frame = tile.content.gameObject.AddComponent<Image>();
        tile.frame.preserveAspect = true;
        tile.frame.raycastTarget = true;
        RectTransform iconRect = HudTheme.CreateRect("Icon", tile.content);
        HudTheme.Stretch(iconRect);
        tile.icon = iconRect.gameObject.AddComponent<Image>();
        tile.icon.preserveAspect = true;
        tile.icon.raycastTarget = false;

        RectTransform sealRect = HudTheme.CreateRect("Seal", tile.content);
        sealRect.anchorMin = sealRect.anchorMax = new Vector2(0f, 1f);
        sealRect.pivot = new Vector2(0f, 1f);
        sealRect.sizeDelta = new Vector2(size * 0.3f, size * 0.3f);
        sealRect.anchoredPosition = new Vector2(2f, -2f);
        tile.seal = HudTheme.AddImage(sealRect, HudTheme.Circle, new Color32(176, 40, 42, 255));
        RectTransform tick = HudTheme.CreateRect("Tick", sealRect);
        HudTheme.Stretch(tick, size * 0.06f);
        HudTheme.AddImage(tick, HudTheme.Check, HudTheme.TextPrimary);

        RectTransform lockRect = HudTheme.CreateRect("Padlock", tile.content);
        lockRect.anchorMin = lockRect.anchorMax = new Vector2(0.5f, 0.55f);
        lockRect.sizeDelta = new Vector2(size * 0.34f, size * 0.34f);
        tile.padlock = HudTheme.AddImage(lockRect, HudTheme.Lock, new Color(0.8f, 0.82f, 0.86f, 0.75f));

        tile.count = HudTheme.CreateLabel("Count", tile.content, "", size * 0.22f, HudTheme.TextPrimary, TextAlignmentOptions.BottomRight, FontStyles.Bold);
        HudTheme.Stretch(tile.count.rectTransform, size * 0.09f);
        tile.count.outlineWidth = 0.25f;
        tile.count.outlineColor = new Color32(10, 16, 26, 255);

        tile.label = HudTheme.CreateLabel("Name", rect, "", 19f, HudTheme.TextPrimary, TextAlignmentOptions.Center);
        tile.label.fontSizeMin = 13f;
        tile.label.rectTransform.anchorMin = new Vector2(0f, 0f);
        tile.label.rectTransform.anchorMax = new Vector2(1f, 0f);
        tile.label.rectTransform.pivot = new Vector2(0.5f, 1f);
        tile.label.rectTransform.sizeDelta = new Vector2(12f, 26f);
        tile.label.rectTransform.anchoredPosition = new Vector2(0f, -4f);
        return tile;
    }

    private static Image Fill(RectTransform parent, string name, float inset, Color color, float radius)
    {
        RectTransform rect = HudTheme.CreateRect(name, parent);
        HudTheme.Stretch(rect, inset);
        Image image = HudTheme.AddImage(rect, HudTheme.Rounded, color, radius);
        rect.gameObject.SetActive(false);
        return image;
    }

    /// <summary>Shows a slot's content (null = a locked tile when locked is true).</summary>
    public void Show(InventorySlot slot, bool locked, bool barStyle, InventoryCatalogue catalogue)
    {
        Locked = locked;
        bool hasItem = !locked && slot != null && !slot.IsEmpty;
        InventoryItemDefinition def = hasItem ? catalogue.Find(slot.itemId) : null;
        frame.sprite = HudIconArt.Get(locked ? "locked" : "slot");
        // The frame stays the tile's touch target; with an item it is invisible under the picture.
        frame.enabled = true;
        icon.enabled = hasItem;
        icon.color = Color.white;
        if (hasItem) icon.sprite = HudIconArt.Get(def != null && !string.IsNullOrEmpty(def.icon) ? def.icon : "papers");
        seal.gameObject.SetActive(hasItem && def != null && def.required);
        padlock.gameObject.SetActive(locked);
        count.text = hasItem && slot.quantity > 1 ? "×" + slot.quantity : "";
        // Only items are named: an empty or locked tile shows its picture alone.
        label.text = hasItem ? (def != null ? def.label : slot.itemId) : "";
        label.color = HudTheme.TextPrimary;
        frame.color = hasItem ? Color.clear : locked ? new Color(1f, 1f, 1f, 0.55f) : barStyle ? Color.white : new Color(1f, 1f, 1f, 0.85f);
    }

    public void SetSelected(bool on)
    {
        selectedOn = on;
        selection.gameObject.SetActive(on);
        if (on) selection.color = HudTheme.BrassSoft;
        liftGoal = on ? LiftHeight : 0f;
    }

    public void SetTargeted(bool on) => targetRing.gameObject.SetActive(on);

    public void SetCursor(bool on)
    {
        if (!on || selectedOn) return;
        // The remembered cursor: a faint ring, no lift.
        selection.gameObject.SetActive(true);
        selection.color = new Color(HudTheme.BrassSoft.r, HudTheme.BrassSoft.g, HudTheme.BrassSoft.b, 0.3f);
    }

    /// <summary>The picture stays behind faintly while its ghost is dragged.</summary>
    public void SetDragging(bool on)
    {
        if (icon != null && icon.enabled) icon.color = new Color(1f, 1f, 1f, on ? 0.3f : 1f);
    }

    /// <summary>A short "no" wobble (a locked tile was tapped).</summary>
    public void Shake() => shakeAt = Time.unscaledTime;

    public void Fan(float t)
    {
        fanScale = t <= 0f ? 0.6f : Mathf.LerpUnclamped(0.6f, 1f, HudTheme.EaseOutBack(Mathf.Clamp01(t / 0.24f)));
        Apply();
    }

    public void Punch(float t)
    {
        punchScale = t < 0.22f ? 1f + Mathf.Sin(t / 0.22f * Mathf.PI) * 0.16f : 1f;
        Apply();
    }

    private void Apply()
    {
        if (content == null) return;
        float grow = selectedOn ? 1.04f : 1f;
        content.localScale = Vector3.one * (fanScale * punchScale * pressScale * grow);
        float k = Time.unscaledTime - shakeAt;
        float wobble = k < 0.3f ? Mathf.Sin(k * 55f) * 6f * (1f - k / 0.3f) : 0f;
        content.anchoredPosition = new Vector2(wobble, lift);
        // The rings rise with the lifted picture.
        Follow(selection.rectTransform, wobble);
        Follow(targetRing.rectTransform, wobble);
    }

    private void Follow(RectTransform ring, float wobble)
    {
        ring.offsetMin = new Vector2(-RingInset + wobble, -RingInset + lift);
        ring.offsetMax = new Vector2(RingInset + wobble, RingInset + lift);
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        float ease = 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime);
        lift = Mathf.Lerp(lift, liftGoal, ease);
        float pressGoal = dragging ? 1f : pressing ? 0.92f : over && !Locked ? 1.05f : 1f;
        pressScale = Mathf.Lerp(pressScale, pressGoal, ease);
        // The chosen tile's ring breathes; a target slot's ring pulses.
        if (selectedOn) selection.color = new Color(HudTheme.BrassSoft.r, HudTheme.BrassSoft.g, HudTheme.BrassSoft.b, 0.75f + Mathf.Sin(now * 5f) * 0.25f);
        if (targetRing.gameObject.activeSelf) targetRing.color = new Color(HudTheme.Active.r, HudTheme.Active.g, HudTheme.Active.b, 0.6f + Mathf.Sin(now * 6f) * 0.4f);
        Apply();

        if (!pressing || held || dragging) return;
        if (now - pressedAt >= 0.5f)
        {
            held = true;
            pressing = false;
            screen.TileHeld(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => over = true;

    public void OnPointerExit(PointerEventData eventData) => over = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        pressing = true;
        held = false;
        pressedAt = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData) => pressing = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (held || dragging || eventData.dragging) return;
        screen.TileTapped(this);
    }

    public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = true;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (held || !screen.CanDrag(this))
        {
            eventData.pointerDrag = null;
            return;
        }
        dragging = true;
        pressing = false;
        screen.BeginDrag(this, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragging) screen.Drag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;
        screen.EndDrag(eventData);
    }

    private void OnDisable()
    {
        pressing = held = dragging = over = false;
        pressScale = 1f;
    }
}

/// <summary>
/// PURPOSE:
/// "Take it out" (T10): when a world action needs an item that is only in
/// the bag (the Auditor asking for the Case Folder, Marie waiting for her
/// copy), a small card beside the bar says so and
/// offers ONE large button that swaps the item into the bar. The player is
/// never sent hunting through menus to finish a required step.
/// </summary>
public sealed class HudTakeOutPrompt : MonoBehaviour
{
    private const int SortingOrder = 2560;
    private HudHotbar hotbar;
    private RectTransform card;
    private TextMeshProUGUI message;
    private Button takeButton;
    private string itemId;
    private float shownAt;
    private bool duringDialogue;

    public bool Showing => card != null && card.gameObject.activeSelf;

    /// <summary>When the card was last closed (the handover prompt comes back while Marie still waits).</summary>
    public float HiddenAt { get; private set; } = -10f;

    public static HudTakeOutPrompt Create(Transform canvasRoot, HudHotbar owner)
    {
        RectTransform layer = HudTheme.CreateRect("Take It Out Prompt (T10)", canvasRoot);
        HudTheme.Stretch(layer);
        var canvas = layer.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;
        layer.gameObject.AddComponent<GraphicRaycaster>();
        HudTakeOutPrompt prompt = layer.gameObject.AddComponent<HudTakeOutPrompt>();
        prompt.hotbar = owner;
        prompt.Build(layer);
        return prompt;
    }

    private void Build(RectTransform layer)
    {
        card = HudTheme.CreateRect("Card", layer);
        card.anchorMin = card.anchorMax = new Vector2(1f, 1f);
        card.pivot = new Vector2(1f, 1f);
        card.sizeDelta = new Vector2(560f, 250f);
        card.anchoredPosition = new Vector2(-190f, -150f);
        RectTransform body = HudTheme.CreatePanel("Panel", card, 18f);
        HudTheme.Stretch((RectTransform)body.parent);
        ((Image)body.parent.GetComponent<Image>()).raycastTarget = true;

        message = HudTheme.CreateLabel("Message", body, "", 26f, HudTheme.TextPrimary, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        message.textWrappingMode = TextWrappingModes.Normal;
        HudTheme.PlaceTopLeft(message.rectTransform, 24f, 18f, 470f, 90f);

        RectTransform close = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Close", body), 494f, 10f, 52f, 52f);
        Image closeFace = HudTheme.AddImage(close, HudTheme.Circle, new Color32(52, 66, 82, 255));
        closeFace.raycastTarget = true;
        Button closeButton = close.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeFace;
        closeButton.onClick.AddListener(Hide);
        TextMeshProUGUI x = HudTheme.CreateLabel("X", close, "×", 34f, HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(x.rectTransform);

        RectTransform take = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Take It Out", body), 24f, 120f, 512f, 104f);
        Image face = HudTheme.AddImage(take, HudTheme.Rounded, new Color32(120, 92, 44, 255), 18f);
        face.raycastTarget = true;
        var rim = take.gameObject.AddComponent<Outline>();
        rim.effectColor = HudTheme.BrassSoft;
        rim.effectDistance = new Vector2(3f, -3f);
        takeButton = take.gameObject.AddComponent<Button>();
        takeButton.targetGraphic = face;
        takeButton.onClick.AddListener(OnTakeOut);
        TextMeshProUGUI label = HudTheme.CreateLabel("Label", take, "TAKE IT OUT", 38f, HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(label.rectTransform, 10f);
        label.characterSpacing = 4f;
        card.gameObject.SetActive(false);
    }

    /// <summary>Shows the card for one item. False when nothing was shown.</summary>
    public bool Show(string item, string text, bool duringDialogue = false, bool showButton = true)
    {
        if (card == null || (!duringDialogue && !HudHotbar.CanOpenPanels())) return false;
        itemId = item;
        this.duringDialogue = duringDialogue;
        message.text = text;
        takeButton.gameObject.SetActive(showButton);
        card.sizeDelta = new Vector2(560f, showButton ? 250f : 130f);
        card.gameObject.SetActive(true);
        shownAt = Time.unscaledTime;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_ui_panel_open_01");
        return true;
    }

    public void Hide()
    {
        if (card != null && card.gameObject.activeSelf) HiddenAt = Time.unscaledTime;
        if (card != null) card.gameObject.SetActive(false);
        itemId = null;
    }

    public void HideFor(string item)
    {
        if (itemId == item) Hide();
    }

    private void OnTakeOut()
    {
        if (string.IsNullOrEmpty(itemId)) return;
        InventoryResult result = hotbar.TakeOut(itemId);
        if (result.Changed || result.Outcome == InventoryOutcome.NoChange) Hide();
        else message.text = result.Outcome == InventoryOutcome.SaveFailed ? "That could not be saved just now. Try again." : result.Message;
    }

    private void Update()
    {
        if (!Showing) return;
        float t = Time.unscaledTime - shownAt;
        card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, HudTheme.EaseOutBack(Mathf.Clamp01(t / 0.25f)));
        // A plain notice (no button) fades on its own; a conversation that was not this prompt's closes it.
        if (!takeButton.gameObject.activeSelf && t > 4f) Hide();
        else if (!duringDialogue && DialogueUI.IsAnyDialogueActive) Hide();
        else if (InventoryScreen.AnyOpen) Hide();
    }
}
