using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>One fixed Computation leaf. Values come from real papers/book and manual calculator prints;
/// no active-stage replacement, chooser menu, operation or Calculate button. Existing save/flight owners remain.
/// T09: after a returned review or a hold, the line the Auditor marked carries a quiet red outline and caption
/// tag, and the leaf's note shows his short line (Level3ReviewRules.MarkedField/MarkedLine) until the next hand-in.</summary>
public sealed class Level3ComputationPage
{
    public const string Role = "level3-computation", PaymentRole = "level3-payment", Heading = "Computation";
    public static void Workspace(float aspect, out Rect source, out Rect folder, out Rect calculator)
    {
        if (aspect >= 1.3f)
        {
            source = new Rect(0.02f, 0.2f, 0.23f, 0.66f);
            folder = new Rect(0.27f, 0.1f, 0.36f, 0.78f);
            calculator = new Rect(0.65f, 0.18f, 0.25f, 0.68f);
        }
        else
        {
            source = new Rect(0.04f, 0.6f, 0.86f, 0.35f);
            folder = new Rect(0.02f, 0.06f, 0.56f, 0.50f);
            calculator = new Rect(0.61f, 0.06f, 0.29f, 0.50f);
        }
    }
    public static Rect ScreenArea(Rect viewport, Rect area) => new Rect(viewport.x + area.x * viewport.width,
        viewport.y + area.y * viewport.height, area.width * viewport.width, area.height * viewport.height);
    public const string HelpCredit = "Tax already withheld counts toward payment, not as an expense.";
    public const string HelpBand = "The rate applies only to the amount above the band's floor.";

    private const float W = 760f;
    public const float FormHeight = 128f, FormGap = 20f;
    private static readonly Color Ink = PrintedPaperStyle.InkColor, Muted = new Color32(91,101,120,255);
    private static readonly Color FieldFill = PrintedPaperStyle.PaperColor;
    private int drawn = int.MinValue;
    private string status, lastRefusal;
    private static string Bea => Level3ChapterRules.BeaCaseId;
    private static Level3ChapterCheckpoint Chapter => Level3Checkpointer.Active?.State;
    private static CaseDefinition Definition => CaseManager.Instance?.CurrentDefinition;
    private static bool AtDesk => CameraController.Instance?.CurrentMode == CameraController.CameraMode.Workstation;
    public string LastRefusal => lastRefusal;
    public static bool Shown(Level3ChapterCheckpoint state) => state != null && state.activeCaseId == Bea &&
        Level3ChapterRules.PhaseOf(state) >= Level3Phase.BeaComputation && Level3WorksheetCheck.Collected(state);
    public CaseFolderPageContent Content() => new CaseFolderPageContent(Heading, "") { Render = page => Draw(page, false), Role = Role };
    public CaseFolderPageContent PaymentContent() => new CaseFolderPageContent("Tax and payment", "") { Render = page => Draw(page, true), Role = PaymentRole };
    public void Note(string text) => status = text;
    public string ViewStage(Level3ChapterCheckpoint state, CaseDefinition definition) => Level3ComputationRules.ActiveStage(state, definition, Bea);
    public int Signature()
    {
        unchecked
        {
            return (Level3ComputationRules.Record(Chapter, Bea)?.revision ?? -1) * 397 ^
                (Chapter?.phase ?? "").GetHashCode() ^ (status ?? "").GetHashCode() ^
                (Level3CertificateRules.Acquired(Chapter) ? 1 : 0) ^ (AtDesk ? 2 : 0) ^
                ((Level3DeskCalculator.Instance?.FlyingDestination ?? "").GetHashCode()) ^
                (Level3DeskCalculator.Instance?.HasPrintedTape == true ? 4 : 0) ^ (Chapter?.calculatorTaught == true ? 8 : 0) ^
                ((Chapter?.Case(Bea)?.review?.submissions ?? 0) * 7919) ^ (Level3ReviewRules.CaseDone(Chapter) ? 16 : 0) ^
                // Teaching pass: a desk example starting or finishing turns the page read-only / editable again.
                (Level3PracticeRules.LessonDue(Chapter) ? 32 : 0);
        }
    }
    public bool NeedsRedraw => Signature() != drawn;
    public void Reset() { status = null; }
    public void Landed(string field) { status = "Result attached"; }
    public void Tick() { }

    private void Draw(CaseFolderUI.PageBuilder page, bool payment)
    {
        drawn = Signature();
        if (page?.Paper == null || Chapter == null || Definition?.level3 == null) return;
        RectTransform root = HudTheme.CreateRect("Level3 Computation Page", page.Paper);
        page.AddOverlay(root.gameObject);
        RectTransform column = page.ContentColumn;
        if (column != null) root.SetParent(column, false);
        root.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, 1f);
        root.sizeDelta = new Vector2(W, 960f);
        root.anchoredPosition = column != null ? Vector2.zero : new Vector2(0f, -250f);
        root.localScale = Vector3.one * (column != null ? column.rect.width / W : 1f);
        bool editable = AtDesk && Level3ComputationRules.Editable(Chapter, Bea) && !Level3PracticeRules.LessonDue(Chapter);
        marked = Level3ReviewRules.MarkedField(Chapter);
        if (!payment)
        {
            ResultRow(root, 24f, Level3ComputationRules.Taxable, "Taxable income", editable);
            BandRow(root, 172f, editable);
            BandParts(root, 320f);
            ResultRow(root, 512f, Level3ComputationRules.TaxDue, "Tax due", editable);
            StatusLine(root, 660f, false);
            return;
        }
        Level3ComputationRecord record = Level3ComputationRules.Record(Chapter, Bea);
        RectTransform credit = FormField(root, "Tax Credit Field", "Tax credit" + (marked == Level3ComputationRules.Credits ? MarkSuffix : ""), string.IsNullOrEmpty(record?.creditChoice) ? "Place amount" : Level3ComputationRules.Peso(record.creditAmount), string.IsNullOrEmpty(record?.creditChoice), 24f, editable);
        var target = credit.gameObject.AddComponent<Level3ComputationField>();
        target.Accepts = v => AcceptsInput(Level3ComputationRules.Credits, v);
        target.Receive = v => ReceiveInput(Level3ComputationRules.Credits, v);
        target.Rejected = why => status = why;
        Source(credit, () => Level3ValueTransfer.ComputationSource(Chapter, Definition, Level3ComputationRules.FieldSource(Bea, Level3ComputationRules.Credits), "Tax credit"));
        Mark(credit, Level3ComputationRules.Credits);
        ResultRow(root, 172f, Level3ComputationRules.Payable, "Remaining payable", editable);
        StatusLine(root, 488f, true);
    }

    // =========================================================
    // T09: THE AUDITOR'S MARK (after a returned review or a hold)
    // =========================================================

    /// <summary>The field his last return or hold marked ("" when none); set at each draw.</summary>
    private string marked = "";

    /// <summary>One quiet red-pen outline on the line he marked (no flash, no lock: the field works as before).</summary>
    private void Mark(RectTransform box, string field)
    {
        if (string.IsNullOrEmpty(marked) || marked != field) return;
        Outline(box, MarkInk, 12f);
    }

    private static readonly Color MarkInk = new Color32(178, 54, 46, 255);
    public const string MarkSuffix = " · Auditor's mark";

    /// <summary>
    /// The leaf's one note: a routine acknowledgment or refusal while working; otherwise (T09) the Auditor's
    /// short line on the leaf that holds his mark (a stamp-only return shows on the first leaf). One voice.
    /// </summary>
    private void StatusLine(RectTransform root, float y, bool payment)
    {
        string text = status;
        if (string.IsNullOrEmpty(text))
        {
            bool onPayment = marked == Level3ComputationRules.Credits || marked == Level3ComputationRules.Payable;
            string line = Level3ReviewRules.MarkedLine(Chapter, Level3Checkpointer.ReviewContent);
            if (!string.IsNullOrEmpty(line) && onPayment == payment) text = "Auditor: " + line;
        }
        if (!string.IsNullOrEmpty(text)) Text(root, "Status", text, 0f, y, W, 120f, 28f, string.IsNullOrEmpty(status) ? MarkInk : Muted, TextAlignmentOptions.TopLeft, wrap: true);
    }
    private void BandRow(RectTransform root, float y, bool editable)
    {
        string id = Level3ComputationRules.Record(Chapter, Bea)?.ruleId;
        var rule = Level3ComputationRules.FindRule(Definition, id);
        RectTransform box = FormField(root, "Annual Band Field", "Annual band" + (marked == Level3ComputationRules.Rule ? MarkSuffix : ""), rule == null ? "Place band" : Level3ComputationRules.BandLabel(rule), rule == null, y, editable);
        Mark(box, Level3ComputationRules.Rule);
        var drop = box.gameObject.AddComponent<Level3ComputationField>();
        drop.Accepts = v => AtDesk && Level3ValueTransfer.ReadBand(v, Chapter, Definition, out _);
        drop.Receive = ReceiveBand;
        Source(box, () => Level3ValueTransfer.Band(Chapter, Definition, Level3ComputationRules.Record(Chapter, Bea)?.ruleId));
    }
    private bool ReceiveBand(DataValue value)
    {
        if (!AtDesk || !Level3ValueTransfer.ReadBand(value, Chapter, Definition, out string ruleId)) return false;
        var outcome = Level3Checkpointer.Active.CommitComputation((Level3ChapterCheckpoint c, out string why) => Level3ComputationRules.SelectRule(c, Definition, ruleId, out why), out string hint);
        bool ok = outcome == Level3Checkpointer.TapeCommit.Attached || outcome == Level3Checkpointer.TapeCommit.AlreadyAttached;
        status = ok ? "Band placed" : hint;
        if (outcome == Level3Checkpointer.TapeCommit.Attached) GameplayEvents.RaiseGuideCue(Level3DeskCalculator.CueRuleChosen);
        return ok;
    }
    private void BandParts(RectTransform root, float y)
    {
        var rule = Level3ComputationRules.FindRule(Definition, Level3ComputationRules.Record(Chapter, Bea)?.ruleId);
        if (rule == null) return;
        string[] parts = { Level3ComputationRules.Lower, Level3ComputationRules.Rate, Level3ComputationRules.Base };
        string[] captions = { "Threshold", "Rate", "Base tax" };
        string[] values = { RuleMoney(rule.lower), Level3ComputationRules.Percent(rule.rate), RuleMoney(rule.baseTax) };
        for (int i = 0; i < parts.Length; i++)
        {
            string id = Level3ComputationRules.RuleSource(rule.ruleId, parts[i]), label = captions[i];
            RectTransform box = FormField(root, label + " Source", label, values[i], false, y, true, 240f, 96f, 34f);
            box.anchoredPosition = new Vector2(i * 260f, -y);
            Source(box, () => Level3ValueTransfer.ComputationSource(Chapter, Definition, id, label));
        }
        Text(root, "Band Formula", "(Taxable income − Threshold) × Rate + Base tax", 0f, y + 104f, W, 76f, 36f, Ink, TextAlignmentOptions.Left, wrap: true);
    }
    private static string RuleMoney(string exact) => Level3Calculator.TryParse(exact, out decimal amount)
        ? "₱" + amount.ToString("#,0.######", System.Globalization.CultureInfo.InvariantCulture) : "";
    private static void Source(RectTransform box, Func<DataValue> value)
    {
        Level3ComputationField field = box.GetComponent<Level3ComputationField>() ?? box.gameObject.AddComponent<Level3ComputationField>();
        field.Source = value;
        ValueDragSource drag = box.gameObject.AddComponent<ValueDragSource>();
        drag.Initialize(field, field);
    }
    private void ResultRow(RectTransform root, float y, string field, string caption, bool editable)
    {
        Level3DeskCalculator desk = Level3DeskCalculator.Instance;
        var tape = Level3ComputationRules.TapeOf(Chapter, Bea, field);
        bool shown = tape != null && desk?.FlyingDestination != Level3ComputationRules.Destination(Bea, field);
        bool stale = shown && Level3ComputationRules.IsStale(Chapter, Definition, Bea, field);
        RectTransform box = FormField(root, caption + " Slot", caption + (stale ? " · Recalculate" : marked == field ? MarkSuffix : ""), shown ? Level3ComputationRules.Peso(tape.result) : "Attach result here", !shown, y, editable);
        Mark(box, field);
        var drop = box.gameObject.AddComponent<Level3ComputationField>();
        drop.Accepts = v => AcceptsResult(v);
        drop.Receive = v => desk != null && desk.AttachPrintedToFolder(field, box, v);
        drop.Rejected = why => status = why;
        Source(box, () => Level3ValueTransfer.ComputationSource(Chapter, Definition, Level3ComputationRules.SheetSource(Bea, field), caption));
        Box(box, "Clip", W * .5f - 40f, -6f, 80f, 12f, PrintedPaperStyle.BorderColor, 4f);
    }
    private bool AcceptsInput(string field, DataValue value)
    {
        if (!AtDesk || Chapter == null) return false;
        var outcome = Level3ComputationRules.PlaceInput(Chapter.Copy(), Definition, field, value, out lastRefusal);
        return outcome == Level3CommandOutcome.Applied || outcome == Level3CommandOutcome.Duplicate;
    }
    private bool ReceiveInput(string field, DataValue value)
    {
        if (Level3Checkpointer.Active == null || !AtDesk) return false;
        var outcome = Level3Checkpointer.Active.CommitComputation((Level3ChapterCheckpoint c, out string why) => Level3ComputationRules.PlaceInput(c, Definition, field, value, out why), out string hint);
        bool ok = outcome == Level3Checkpointer.TapeCommit.Attached || outcome == Level3Checkpointer.TapeCommit.AlreadyAttached;
        status = ok ? "Amount placed" : hint;
        if (outcome == Level3Checkpointer.TapeCommit.Attached) GameplayEvents.RaiseGuideCue(Level3DeskCalculator.CueCreditChosen);
        return ok;
    }
    private bool AcceptsResult(DataValue value)
    {
        bool ok = AtDesk && value?.Value is Level3CalculatorTransferValue print && print.DraftKey != null && Level3DeskCalculator.Instance?.CanPlaceTransfer(value, true) == true;
        lastRefusal = ok ? null : "Select the calculator's printed result.";
        return ok;
    }
    public static string SourceName(string sourceId) => string.IsNullOrEmpty(sourceId) ? "" : sourceId.StartsWith("worksheet:", StringComparison.Ordinal) ? "Bea's summary" : sourceId.StartsWith("paper:B-WHT/", StringComparison.Ordinal) ? "Certificate" : "Bea's paper";
    private static RectTransform FormField(RectTransform root, string name, string caption, string value, bool blank, float y, bool editable, float width = W, float height = FormHeight, float font = PaperInkStyle.FieldFontSize)
    {
        RectTransform box = Box(root, name, 0f, y, width, height, FieldFill, 12f, raycast: editable);
        var text = Text(box, "Value", "", 0f, 12f, width, height - 24f, font, PaperInkStyle.PrintInk, TextAlignmentOptions.MidlineLeft, wrap: true);
        PaperInkStyle.StyleFieldRow(box.GetComponent<Image>(), text, true, null, null);
        text.margin = new Vector4(18f, 0f, 40f, 0f);
        text.text = PaperInkStyle.FieldTextStacked(caption, value, blank);
        return box;
    }
    private static TextMeshProUGUI Text(RectTransform parent, string name, string text, float x, float y, float w, float h, float size, Color color,
        TextAlignmentOptions align, FontStyles style = FontStyles.Normal, bool wrap = false)
    {
        TextMeshProUGUI label = HudTheme.CreateLabel(name, parent, text, size, color, align, style);
        label.enableAutoSizing = false;
        label.fontSize = size;
        label.overflowMode = TextOverflowModes.Overflow;
        label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        label.extraPadding = true;
        label.raycastTarget = false;
        HudTheme.PlaceTopLeft(label.rectTransform, x, y, w, h);
        return label;
    }

    private static RectTransform Box(RectTransform parent, string name, float x, float y, float w, float h, Color color, float radius, bool raycast = false)
    {
        RectTransform rect = HudTheme.PlaceTopLeft(HudTheme.CreateRect(name, parent), x, y, w, h);
        Image image = HudTheme.AddImage(rect, radius > 0f ? HudTheme.Rounded : null, color, Mathf.Max(1f, radius));
        image.raycastTarget = raycast;
        return rect;
    }

    private static void Outline(RectTransform rect, Color color, float radius)
    {
        RectTransform outline = HudTheme.CreateRect("Outline", rect);
        HudTheme.Stretch(outline, -2f);
        Image image = HudTheme.AddImage(outline, HudTheme.Rounded, color, radius + 2f);
        image.fillCenter = false;
        image.raycastTarget = false;
    }

}

public sealed class Level3ComputationField : MonoBehaviour, IDataValueDestination, IDataValueSource, IPointerUpHandler, IPointerDownHandler, IPointerClickHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    private ValueTypeValidationFeedback feedback;
    private void Awake()
    {
        feedback = GetComponent<ValueTypeValidationFeedback>() ?? gameObject.AddComponent<ValueTypeValidationFeedback>();
    }
    public Func<DataValue> Source;
    public DataValue GetDataValue() => Source?.Invoke();
    public Func<DataValue, bool> Accepts;
    public Func<DataValue, bool> Receive;
    public Action<string> Rejected;
    public Action Tapped;
    private Image hoverOutline;
    private Color restingOutline;
    private UnityEngine.UI.Outline fieldOutline;
    public void OnPointerEnter(PointerEventData e)
    {
        if (ValueTransferManager.Instance?.HasSelection == true)
            feedback?.ShowFeedback(CanAccept(ValueTransferManager.Instance.SelectedValue));
        fieldOutline = GetComponent<UnityEngine.UI.Outline>();
        if (fieldOutline != null)
        {
            restingOutline = fieldOutline.effectColor;
            if (ValueTransferManager.Instance?.HasSelection == true && CanAccept(ValueTransferManager.Instance.SelectedValue))
                fieldOutline.effectColor = HudTheme.Brass;
            return;
        }
        if (hoverOutline == null)
        {
            Transform outline = transform.Find("Outline");
            if (outline == null) return;
            hoverOutline = outline.GetComponent<Image>();
            if (hoverOutline == null) return;
            restingOutline = hoverOutline.color;
        }
        if (ValueTransferManager.Instance?.HasSelection == true && CanAccept(ValueTransferManager.Instance.SelectedValue))
            hoverOutline.color = HudTheme.Brass;
    }
    public void OnPointerExit(PointerEventData e)
    {
        feedback?.ForceRestore();
        if (fieldOutline != null) fieldOutline.effectColor = restingOutline;
        if (hoverOutline != null) hoverOutline.color = restingOutline;
    }
    private void ClearOwned()
    {
        if (ReferenceEquals(ValueTransferManager.Instance?.SelectedSourceComponent, this))
        {
            GetComponent<ValueDragSource>()?.OnEndDrag(null);
            ValueTransferManager.Instance.ClearSelection();
        }
    }
    private void OnDisable() { OnPointerExit(null); ClearOwned(); }
    private void OnApplicationFocus(bool focused) { if (!focused) ClearOwned(); }
    private void OnApplicationPause(bool paused) { if (paused) ClearOwned(); }
    private void Update() { if (InGameSettings.IsOpen || DialogueUI.IsAnyDialogueActive || CameraController.Instance?.CurrentMode != CameraController.CameraMode.Workstation) ClearOwned(); }
    public void OnPointerUp(PointerEventData e) { if (Level3DeskToolHitTarget.Canceled(e)) ClearOwned(); }

    public bool CanAccept(DataValue value) => value != null && Accepts != null && Accepts(value);
    public bool TryReceiveValue(DataValue value)
    {
        bool received = CanAccept(value) && Receive != null && Receive(value);
        feedback?.ShowFeedback(received);
        return received;
    }

    // Keep the press on the field, not on the folder's cover drag.
    public void OnPointerDown(PointerEventData eventData) { }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || MobileInputGuard.IsDragOrCancelled(eventData)) return;
        if (ValueTransferManager.Instance != null && (!ValueTransferManager.Instance.HasSelection ||
            ReferenceEquals(ValueTransferManager.Instance.SelectedSourceComponent, this)) && GetDataValue() != null)
            ValueTransferManager.Instance.SelectValue(this, this);
        else Place();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (Level3DeskToolHitTarget.Canceled(eventData)) return;
        GameObject dragged = eventData?.pointerDrag;
        Level3AmountSource source = dragged != null ? dragged.GetComponent<Level3AmountSource>() : null;
        if (source != null && (!source.IsValueDragging || !ReferenceEquals(ValueTransferManager.Instance?.SelectedSourceComponent, source))) return;
        if (dragged != null && (dragged.GetComponent<ValueDragSource>() != null || dragged.GetComponent<Level3DeskToolHitTarget>() != null)) Place();
    }

    private void Place()
    {
        ValueTransferManager manager = ValueTransferManager.Instance;
        if (manager == null || !manager.HasSelection || manager.SelectedValue == null) { Tapped?.Invoke(); return; }
        if (!CanAccept(manager.SelectedValue))
        {
            GetComponent<ValueTypeValidationFeedback>()?.ShowFeedback(false);
            Level3ComputationPage page = Level3CaseFolder.Active != null ? Level3CaseFolder.Active.Computation : null;
            Rejected?.Invoke(page != null && !string.IsNullOrEmpty(page.LastRefusal) ? page.LastRefusal : "That can't go here.");
            AudioManager.Instance?.PlayEffect("sfx_ui_unavailable_01", 0.5f);
            return;
        }
        manager.TryPlaceOnDestination(this);
    }
}
