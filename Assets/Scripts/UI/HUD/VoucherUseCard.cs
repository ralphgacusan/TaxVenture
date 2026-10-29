using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The one voucher-use command (T14), kept apart from the inventory so that
/// opening or arranging the Bag never touches the clock. It checks the rules
/// over the existing Office Hours clock, builds the extended clock and the
/// complete Level 1 checkpoint around it, commits once (voucher, per-case
/// claim, clock) and only then gives the live clock its later cutoff.
/// </summary>
public static class ReviewVoucherCommand
{
    /// <summary>The active case's id ("" before a case is loaded).</summary>
    public static string ActiveCaseId =>
        CaseManager.Instance != null && CaseManager.Instance.CurrentDefinition != null ? CaseManager.Instance.CurrentDefinition.caseId ?? "" : "";

    /// <summary>What the card shows of the active clock: Office Hours in Level 1, Rosa's review clock in Level 2 (T18).</summary>
    public struct ClockInfo
    {
        public bool Shown;
        public int Cutoff;
        public int ExtendedCutoff;
        public bool Extended;
    }

    /// <summary>Level 2's Auditor stays all day: its card speaks of today's review cutoff instead of his leaving time.</summary>
    public static bool ReviewCutoff => Level2ReviewClock.Active != null;
    public static string CaptionNow => ReviewCutoff ? "TODAY'S REVIEW CUTOFF" : "THE AUDITOR LEAVES AT";
    public static string CaptionWill => ReviewCutoff ? "THE CUTOFF WILL MOVE TO" : "THE AUDITOR WILL LEAVE AT";
    public static string CaptionAfter => ReviewCutoff ? "TODAY'S CUTOFF IS NOW" : "THE AUDITOR NOW LEAVES AT";

    public static ClockInfo Clock()
    {
        Level2ReviewClock rosa = Level2ReviewClock.Active;
        if (rosa != null)
            return new ClockInfo
            {
                Shown = rosa.HasDeadline, Cutoff = rosa.DeadlineSeconds, ExtendedCutoff = rosa.ExtendedDeadlineSeconds, Extended = rosa.ExtensionSeconds > 0
            };
        OfficeHours hours = OfficeHours.Instance;
        bool shown = hours != null && hours.Running && hours.HasDeadline;
        return new ClockInfo
        {
            Shown = shown, Cutoff = shown ? hours.DeadlineSeconds : 0, ExtendedCutoff = shown ? hours.ExtendedDeadlineSeconds : 0,
            Extended = hours != null && hours.ExtensionSeconds > 0
        };
    }

    public static VoucherUseStatus Status(HudHotbar hotbar)
    {
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        string caseId = ActiveCaseId;
        Level2ReviewClock rosa = Level2ReviewClock.Active;
        if (rosa != null)
        {
            if (hotbar == null || manager == null) return VoucherUseStatus.NoCase;
            return VoucherRules.Check(InventoryRules.CarriedCount(hotbar.Inventory, InventoryCatalogue.ReviewVoucher), caseId, rosa.VoucherView,
                manager.IsVoucherUsed(Level2CampaignRules.LevelId, caseId));
        }
        OfficeHours hours = OfficeHours.Instance;
        if (hotbar == null || hours == null || !hours.Running || manager == null) return VoucherUseStatus.NoCase;
        return VoucherRules.Check(InventoryRules.CarriedCount(hotbar.Inventory, InventoryCatalogue.ReviewVoucher), caseId, hours.VoucherView,
            manager.IsVoucherUsed(TaskListProvider.Level1Id, caseId));
    }

    public static VoucherUseResult Use(HudHotbar hotbar)
    {
        VoucherUseStatus status = Status(hotbar);
        if (status != VoucherUseStatus.Ready) return new VoucherUseResult { Message = VoucherRules.ReasonFor(status) };
        Level2ReviewClock rosa = Level2ReviewClock.Active;
        if (rosa != null)
        {
            // Rosa's review: the extended clock goes into the Level 2 checkpoint saved with the voucher and its claim.
            ReviewClockState extendedReview = rosa.ExtendedClock();
            if (extendedReview == null) return new VoucherUseResult { Message = VoucherRules.ReasonFor(VoucherUseStatus.AlreadyUsed) };
            LevelCheckpoint reviewCheckpoint = Level2Checkpointer.Active != null ? Level2Checkpointer.Active.CaptureForCommit(extendedReview) : null;
            VoucherUseResult used = PlayerProgressManager.Instance.CommitLevelTwoVoucherUse(hotbar.Catalogue, ActiveCaseId, reviewCheckpoint,
                "voucher/" + Guid.NewGuid().ToString("N"));
            if (used.Outcome == InventoryOutcome.Applied) rosa.ApplyExtension(extendedReview);
            return used;
        }
        OfficeHours hours = OfficeHours.Instance;
        Level1ClockRecord extended = hours.ExtendedClock();
        Level1FieldCheckpointer checkpointer = UnityEngine.Object.FindAnyObjectByType<Level1FieldCheckpointer>();
        Level1FieldCheckpoint checkpoint = checkpointer != null ? checkpointer.CaptureForCommit() : null;
        if (checkpoint != null) checkpoint.clock = extended;
        VoucherUseResult result = PlayerProgressManager.Instance.CommitVoucherUse(hotbar.Catalogue, TaskListProvider.Level1Id, ActiveCaseId, checkpoint,
            "voucher/" + Guid.NewGuid().ToString("N"));
        if (result.Outcome == InventoryOutcome.Applied) hours.ApplyExtension(extended);
        return result;
    }
}

/// <summary>
/// PURPOSE:
/// The review voucher card (Campaign Foundation T14), opened by tapping a
/// carried voucher in the bar or reading it in the Bag. Left: the folded
/// green ticket standing in its leather sleeve, with its carry tab (how many
/// you carry of two). Right: the clock card - a
/// copper clock and "THE AUDITOR LEAVES AT 9:12 AM" - and one big button.
///
/// USE ON THIS CASE asks once, in large plain words: "9:12 AM -> 9:42 AM",
/// "+180 active review seconds", one per case, the voucher is used up. YES,
/// USE IT commits through ReviewVoucherCommand.Use (one save: the voucher, the
/// per-case claim and the extended clock). Only after that succeeds does
/// the choreography play (about 0.7 s): the ticket lifts out of its sleeve,
/// the copper hand turns exactly half a turn, one quiet tick, the clock
/// card shows the new time and the ticket slides into the datebook pocket.
/// A refusal says why (no voucher, no case, cutoff passed, case approved,
/// already used) and changes nothing; no success sound before the save.
/// </summary>
public sealed class VoucherUseCard : MonoBehaviour
{
    private const int SortingOrder = 2575;
    private static readonly Color Cream = new Color32(246, 238, 216, 255);
    private static readonly Color Ink = new Color32(52, 40, 26, 255);
    private static readonly Color InkSoft = new Color32(118, 94, 64, 255);
    private static readonly Color Copper = new Color32(190, 110, 58, 255);
    private static readonly Color Warn = new Color32(255, 196, 120, 255);
    private static readonly Color Good = new Color32(140, 220, 160, 255);

    private enum Stage { Status, Confirm, Applying, Done }

    private HudHotbar hotbar;
    private RectTransform card, ticket, ticketFace, sleeve, pocket, hand, clockCard;
    private TextMeshProUGUI title, status, carryLine, clockCaption, clockTime, extraLine, message;
    private Button primary, secondary;
    private TextMeshProUGUI primaryLabel, secondaryLabel;
    private RectTransform primaryRect;
    private CanvasGroup ticketGroup;
    private Stage stage;
    private bool showing;
    private float shownAt, appliedAt = -10f, shakeAt = -10f;
    private bool ticked;
    private int oldCutoff, newCutoff;
    private Vector2 ticketRest, pocketAt;

    public bool IsOpen => showing;

    public static VoucherUseCard Create(Transform canvasRoot, HudHotbar hotbar)
    {
        RectTransform layer = HudTheme.CreateRect("Voucher Card (T14)", canvasRoot);
        HudTheme.Stretch(layer);
        var canvas = layer.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;
        layer.gameObject.AddComponent<GraphicRaycaster>();
        VoucherUseCard view = layer.gameObject.AddComponent<VoucherUseCard>();
        view.hotbar = hotbar;
        view.Build(layer);
        layer.gameObject.SetActive(false);
        return view;
    }

    // =========================================================
    // BUILD
    // =========================================================

    private void Build(RectTransform layer)
    {
        RectTransform shade = HudTheme.CreateRect("Backdrop", layer);
        HudTheme.Stretch(shade);
        HudTheme.AddImage(shade, null, new Color(0.02f, 0.03f, 0.05f, 0.58f)).raycastTarget = true;

        card = HudTheme.CreateRect("Card", layer);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(1280f, 680f);
        RectTransform body = HudTheme.CreatePanel("Panel", card, 20f);
        HudTheme.Stretch((RectTransform)body.parent);

        // The leather sleeve the ticket stands in (its front edge is drawn over the ticket).
        sleeve = Box(body, "Sleeve", 50f, 330f, 500f, 300f, new Color32(96, 60, 34, 255), 22f);
        Box(sleeve, "Stitching", 12f, 12f, 476f, 4f, new Color32(214, 176, 118, 160));
        ticket = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Ticket", body), 70f, 120f, 460f, 307f);
        ticketGroup = ticket.gameObject.AddComponent<CanvasGroup>();
        ticketFace = HudTheme.CreateRect("Face", ticket);
        ticketFace.anchorMin = ticketFace.anchorMax = new Vector2(0f, 1f);
        ticketFace.pivot = new Vector2(0f, 1f);
        ticketFace.sizeDelta = WorldCardFace.Size;
        ticketFace.localScale = Vector3.one * (460f / WorldCardFace.Size.x);
        RectTransform lip = Box(body, "Sleeve Front", 50f, 400f, 500f, 230f, new Color32(118, 76, 44, 255), 22f);
        Box(lip, "Brass Rivet L", 24f, 20f, 18f, 18f, HudTheme.Brass, 9f);
        Box(lip, "Brass Rivet R", 458f, 20f, 18f, 18f, HudTheme.Brass, 9f);
        TextMeshProUGUI sleeveLabel = Label(lip, "Label", "REVIEW VOUCHERS", 0f, 64f, 500f, 50f, 30f, new Color32(236, 206, 140, 255), TextAlignmentOptions.Center, FontStyles.Bold);
        sleeveLabel.characterSpacing = 4f;
        carryLine = Label(lip, "Carry", "", 20f, 124f, 460f, 80f, 24f, HudTheme.TextPrimary, TextAlignmentOptions.Center);
        ticketRest = ticket.anchoredPosition;

        title = Label(body, "Title", "Review Voucher", 600f, 30f, 640f, 56f, 40f, HudTheme.BrassSoft, TextAlignmentOptions.Left, FontStyles.Bold);
        status = Label(body, "Status", "", 600f, 88f, 640f, 64f, 24f, HudTheme.TextPrimary, TextAlignmentOptions.TopLeft);
        status.enableAutoSizing = true;
        status.fontSizeMin = 16f;

        // The clock card: a copper clock and the Auditor's leaving time (the new one after a use).
        clockCard = Box(body, "Clock Card", 600f, 160f, 640f, 250f, Cream, 18f);
        RectTransform dial = Box(clockCard, "Clock", 24f, 30f, 170f, 170f, Copper);
        dial.GetComponent<Image>().sprite = HudTheme.Circle;
        RectTransform face = Box(dial, "Face", 14f, 14f, 142f, 142f, new Color32(242, 222, 182, 255));
        face.GetComponent<Image>().sprite = HudTheme.Circle;
        for (int i = 0; i < 12; i++)
        {
            RectTransform tick = HudTheme.CreateRect("Tick " + i, face);
            tick.anchorMin = tick.anchorMax = new Vector2(0.5f, 0.5f);
            tick.sizeDelta = new Vector2(i % 3 == 0 ? 7f : 4f, i % 3 == 0 ? 16f : 10f);
            tick.localRotation = Quaternion.Euler(0f, 0f, -i * 30f);
            tick.anchoredPosition = (Vector2)(tick.localRotation * new Vector3(0f, 58f, 0f));
            HudTheme.AddImage(tick, null, new Color32(120, 66, 34, 255));
        }
        hand = HudTheme.CreateRect("Hand", face);
        hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0.5f);
        hand.pivot = new Vector2(0.5f, 0.1f);
        hand.sizeDelta = new Vector2(8f, 60f);
        HudTheme.AddImage(hand, HudTheme.Rounded, new Color32(110, 56, 26, 255), 4f);
        RectTransform hub = HudTheme.CreateRect("Hub", face);
        hub.anchorMin = hub.anchorMax = new Vector2(0.5f, 0.5f);
        hub.sizeDelta = new Vector2(16f, 16f);
        HudTheme.AddImage(hub, HudTheme.Circle, Copper);

        clockCaption = Label(clockCard, "Caption", "THE AUDITOR LEAVES AT", 214f, 26f, 410f, 34f, 22f, InkSoft, TextAlignmentOptions.Left, FontStyles.Bold);
        clockCaption.characterSpacing = 2f;
        clockTime = Label(clockCard, "Time", "", 214f, 62f, 410f, 80f, 64f, Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        clockTime.textWrappingMode = TextWrappingModes.NoWrap;
        clockTime.enableAutoSizing = true;
        clockTime.fontSizeMin = 30f;
        clockTime.fontSizeMax = 64f;
        extraLine = Label(clockCard, "Extra", "", 214f, 146f, 410f, 40f, 24f, new Color32(46, 140, 84, 255), TextAlignmentOptions.Left, FontStyles.Bold);
        // The datebook pocket the used ticket slides into.
        pocket = Box(clockCard, "Datebook Pocket", 214f, 196f, 410f, 40f, new Color32(150, 104, 62, 255), 10f);
        Label(pocket, "Label", "DATEBOOK", 0f, 4f, 410f, 32f, 20f, new Color32(246, 226, 186, 255), TextAlignmentOptions.Center, FontStyles.Bold).characterSpacing = 6f;

        message = Label(body, "Message", "", 600f, 420f, 640f, 70f, 24f, Warn, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        message.enableAutoSizing = true;
        message.fontSizeMin = 16f;
        primary = Button(body, "Primary", 600f, 520f, 400f, 120f, new Color32(120, 92, 44, 255), out primaryLabel, OnPrimary);
        primaryRect = (RectTransform)primary.transform;
        secondary = Button(body, "Secondary", 1020f, 520f, 220f, 120f, new Color32(16, 25, 38, 245), out secondaryLabel, OnSecondary);
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    public void Open()
    {
        if (!showing)
        {
            showing = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            HudWindowLock.Acquire(this);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_pickup_01", 0.6f);
        }
        shownAt = Time.unscaledTime;
        appliedAt = -10f;
        ticked = false;
        stage = Stage.Status;
        ticket.gameObject.SetActive(true);
        ticket.SetSiblingIndex(sleeve.GetSiblingIndex() + 1);
        ticket.anchoredPosition = ticketRest;
        ticket.localScale = Vector3.one;
        ticket.localRotation = Quaternion.identity;
        ticketGroup.alpha = 1f;
        hand.localRotation = Quaternion.identity;
        Render();
    }

    public void Close()
    {
        if (!showing || stage == Stage.Applying) return;
        showing = false;
        gameObject.SetActive(false);
        HudWindowLock.Release(this);
    }

    private void OnDisable()
    {
        if (!showing) return;
        showing = false;
        HudWindowLock.Release(this);
    }

    // =========================================================
    // RENDER
    // =========================================================

    private void Render()
    {
        CampaignInventoryRecord inventory = hotbar != null ? hotbar.Inventory : new CampaignInventoryRecord();
        InventoryItemDefinition item = hotbar != null ? hotbar.Catalogue.Find(InventoryCatalogue.ReviewVoucher) : null;
        int limit = item != null && item.carryLimit > 0 ? item.carryLimit : 2;
        int carried = InventoryRules.CarriedCount(inventory, InventoryCatalogue.ReviewVoucher);
        for (int i = ticketFace.childCount - 1; i >= 0; i--) Destroy(ticketFace.GetChild(i).gameObject);
        WorldCardFace.BuildVoucherFace(ticketFace, "REVIEW VOUCHER", carried, limit);
        carryLine.text = $"You carry {carried} of {limit}";

        ReviewVoucherCommand.ClockInfo info = ReviewVoucherCommand.Clock();
        bool clock = info.Shown;
        clockCard.gameObject.SetActive(clock || stage == Stage.Done);
        if (stage == Stage.Done) return;

        VoucherUseStatus check = ReviewVoucherCommand.Status(hotbar);
        oldCutoff = clock ? info.Cutoff : 0;
        newCutoff = clock ? info.ExtendedCutoff : 0;
        clockCaption.text = ReviewVoucherCommand.CaptionNow;
        clockTime.text = clock ? OfficeHoursMath.Format(oldCutoff) : "";
        extraLine.text = info.Extended ? "Extended with a voucher" : "";

        if (stage == Stage.Confirm && check == VoucherUseStatus.Ready)
        {
            status.text = "Use one voucher on this case? It is used up, and only one can be used per case.";
            clockCaption.text = ReviewVoucherCommand.CaptionWill;
            clockTime.text = $"<color=#76603F>{OfficeHoursMath.Format(oldCutoff)}</color>  <color=#BE6E3A>→</color>  <color=#2E8C54>{OfficeHoursMath.Format(newCutoff)}</color>";
            extraLine.text = "+180 active review seconds";
            message.text = "";
            primaryLabel.text = "YES, USE IT";
            secondaryLabel.text = "NOT NOW";
            primary.interactable = true;
            return;
        }
        stage = Stage.Status;
        status.text = ReviewVoucherCommand.ReviewCutoff
            ? (check == VoucherUseStatus.Ready
                ? "Use it now and today's cutoff moves 180 active review seconds later. One per case."
                : "+180 active review seconds on one case, before today's cutoff. One per case.")
            : check == VoucherUseStatus.Ready
                ? "Use it now and the Auditor leaves 180 active review seconds later. One per case."
                : "+180 active review seconds on one case, before the Auditor leaves. One per case.";
        message.text = VoucherRules.ReasonFor(check);
        message.color = Warn;
        primaryLabel.text = "USE ON THIS CASE";
        secondaryLabel.text = "CLOSE";
        primary.interactable = check == VoucherUseStatus.Ready;
    }

    // =========================================================
    // ACTIONS
    // =========================================================

    private void OnPrimary()
    {
        if (stage == Stage.Applying) return;
        if (stage == Stage.Done) { Close(); return; }
        if (stage == Stage.Status)
        {
            stage = Stage.Confirm;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_ui_select_01", 0.6f);
            Render();
            return;
        }
        VoucherUseResult result = ReviewVoucherCommand.Use(hotbar);
        if (result.Outcome != InventoryOutcome.Applied)
        {
            stage = Stage.Status;
            Render();
            message.text = string.IsNullOrEmpty(result.Message) ? "That could not be saved just now. Try again." : result.Message;
            message.color = Warn;
            shakeAt = Time.unscaledTime;
            return;
        }
        // Saved: now (and only now) the ticket goes into the datebook and the clock turns.
        stage = Stage.Applying;
        appliedAt = Time.unscaledTime;
        ticked = false;
        pocketAt = new Vector2(clockCard.anchoredPosition.x + pocket.anchoredPosition.x + 80f, clockCard.anchoredPosition.y + pocket.anchoredPosition.y + 10f);
        ticket.SetAsLastSibling();
        primary.interactable = false;
        secondary.interactable = false;
        message.text = "";
    }

    private void OnSecondary()
    {
        if (stage == Stage.Applying) return;
        if (stage == Stage.Confirm) { stage = Stage.Status; Render(); return; }
        Close();
    }

    private void FinishApply()
    {
        stage = Stage.Done;
        primary.interactable = true;
        secondary.interactable = true;
        primaryLabel.text = "DONE";
        secondaryLabel.text = "CLOSE";
        status.text = "Voucher used on this case.";
        clockCaption.text = ReviewVoucherCommand.CaptionAfter;
        clockTime.text = OfficeHoursMath.Format(newCutoff);
        extraLine.text = "+180 active review seconds";
        message.text = ReviewVoucherCommand.ReviewCutoff
            ? "Your review clock keeps running; today's window just stays open a little longer."
            : "Your review clock keeps running; the Auditor just stays a little longer.";
        message.color = Good;
        ticket.gameObject.SetActive(false);
        Render();
    }

    // =========================================================
    // MOTION
    // =========================================================

    private void Update()
    {
        if (!showing) return;
        float now = Time.unscaledTime;
        card.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, HudTheme.EaseOutBack(Mathf.Clamp01((now - shownAt) / 0.25f)));
        if (stage != Stage.Applying)
        {
            if (DialogueUI.IsAnyDialogueActive || HudRewardScreen.AnyOpen || InGameSettings.IsOpen) { Close(); return; }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { OnSecondary(); return; }
            // Gentle idle: the ticket breathes in its sleeve.
            if (ticket.gameObject.activeSelf) ticket.anchoredPosition = ticketRest + Vector2.up * (Mathf.Sin(now * 2f) * 3f);
        }
        float k = now - shakeAt;
        primaryRect.localRotation = Quaternion.Euler(0f, 0f, k < 0.32f ? Mathf.Sin(k * 60f) * 3.5f * (1f - k / 0.32f) : 0f);
        if (stage == Stage.Applying) Animate(now - appliedAt);
    }

    /// <summary>About 0.7 s: lift out of the sleeve (0-0.2), the hand turns half a turn (0.2-0.5, one tick as it lands), slide into the pocket (0.3-0.7).</summary>
    private void Animate(float t)
    {
        float lift = HudTheme.EaseOutCubic(Mathf.Clamp01(t / 0.2f));
        Vector2 lifted = ticketRest + new Vector2(0f, 70f) * lift;
        float slide = Mathf.Clamp01((t - 0.3f) / 0.4f);
        float s = slide * slide * (3f - 2f * slide);
        ticket.anchoredPosition = Vector2.Lerp(lifted, pocketAt, s);
        ticket.localScale = Vector3.one * Mathf.Lerp(1f + 0.05f * lift, 0.32f, s);
        ticket.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-4f * lift, 0f, s));
        ticketGroup.alpha = 1f - Mathf.Clamp01((slide - 0.7f) / 0.3f);

        float turn = Mathf.Clamp01((t - 0.2f) / 0.3f);
        float eased = turn * turn * (3f - 2f * turn);
        hand.localRotation = Quaternion.Euler(0f, 0f, -180f * eased);
        if (turn >= 1f && !ticked)
        {
            // One quiet tick, only on a saved use; the clock card shows the new time with a pop.
            ticked = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_ui_click_02", 0.45f);
            clockTime.text = OfficeHoursMath.Format(newCutoff);
            clockCaption.text = ReviewVoucherCommand.CaptionAfter;
        }
        float pop = Mathf.Clamp01((t - 0.5f) / 0.2f);
        clockCard.localScale = Vector3.one * (pop > 0f && pop < 1f ? 1f + Mathf.Sin(pop * Mathf.PI) * 0.04f : 1f);
        if (t >= 0.72f) FinishApply();
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static RectTransform Box(RectTransform parent, string name, float x, float y, float w, float h, Color color, float radius = 0f)
    {
        RectTransform rect = HudTheme.PlaceTopLeft(HudTheme.CreateRect(name, parent), x, y, w, h);
        HudTheme.AddImage(rect, radius > 0f ? HudTheme.Rounded : null, color, radius).raycastTarget = false;
        return rect;
    }

    private static TextMeshProUGUI Label(RectTransform parent, string name, string text, float x, float y, float w, float h, float size, Color color,
        TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
    {
        TextMeshProUGUI label = HudTheme.CreateLabel(name, parent, text, size, color, align, style);
        HudTheme.PlaceTopLeft(label.rectTransform, x, y, w, h);
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private static Button Button(RectTransform parent, string name, float x, float y, float w, float h, Color fill, out TextMeshProUGUI label,
        UnityEngine.Events.UnityAction click)
    {
        RectTransform rect = HudTheme.PlaceTopLeft(HudTheme.CreateRect(name, parent), x, y, w, h);
        Image face = HudTheme.AddImage(rect, HudTheme.Rounded, fill, 18f);
        face.raycastTarget = true;
        var rim = rect.gameObject.AddComponent<Outline>();
        rim.effectColor = HudTheme.Brass;
        rim.effectDistance = new Vector2(3f, -3f);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = face;
        var colors = button.colors;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.62f, 0.75f);
        button.colors = colors;
        button.onClick.AddListener(click);
        label = HudTheme.CreateLabel("Label", rect, "", 32f, HudTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(label.rectTransform, 10f);
        label.characterSpacing = 3f;
        return button;
    }
}
