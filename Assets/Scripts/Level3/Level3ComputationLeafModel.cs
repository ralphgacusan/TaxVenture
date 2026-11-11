using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Bea's computation sheet (Level 3 T07) as a physical desk object: the same
/// navy clipboard and brass clip as the T03 practice card, holding a cream
/// working leaf headed BEA MENDOZA · 2026 COMPUTATION (individual, graduated
/// rates). It replaces the practice card on the desk once her worksheet is
/// collected. Printed lines, top to bottom:
///
/// - Gross income / Allowable expenses: a field showing the total the player
///   picked (from the collected worksheet, or Bea's draft) and a PICK key.
///   A picked total is a calculator source: tap it to put it on the calculator.
/// - Taxable income: a slot that takes a clipped calculator tape.
/// - Rate band: the row of the annual table the player picked (PICK opens the
///   table), with its floor, base tax and rate printed as three source chips.
/// - Excess over the floor / Tax on the excess (working steps) and Tax due:
///   slots for tapes. A tape whose inputs changed keeps its place under a
///   graphite NEEDS RECALCULATION tag; a tape with a typed number carries a
///   TYPED · NO SOURCE tag. Tax due shows its value rounded to cents.
/// - T08: Tax credits (a field and PICK: one of her certificate's printed
///   amounts, once it is handed over and explained) and Remaining payable (a
///   slot for a tape, shown to cents, negative kept). A blue certificate tab
///   on the clipboard's left edge holds the certificate up to read.
/// - T09: a stamp box under the header (top right): the Ready / Not Ready
///   ink of the saved stamp, a pulsing ghost while a stamp is picked, a
///   graphite STAMP OUT OF DATE tag once the sheet changed after it; and the
///   Auditor's red-pencil START HERE mark on the line a returned review names.
/// - One line of plain guidance.
///
/// Choices open on a raised navy chip card (all chips alike, the current one
/// framed). Nothing on the sheet says whether a choice or a tape is right:
/// that is the review's job (T09). Every field, chip, key and slot has its own
/// tap target. Presentation only: Level3ComputationRules and the chapter
/// command decide what changes. Local frame: lying flat, +Y up, +Z away.
/// </summary>
public sealed class Level3ComputationLeafModel
{
    public const float LeafWidth = 0.19f;
    public const float LeafLength = 0.30f;

    public enum HitKind { None, Body, Total, Pick, Band, Constant, Slot, Chip, Cancel, Certificate, StampBox }

    public struct Hit
    {
        public HitKind Kind;
        public string Id;
    }

    private const float Surface = 0.0056f;
    private const float PrintY = Surface + 0.0003f;
    private const float InnerWidth = LeafWidth * 0.92f;
    private const float LabelX = -InnerWidth * 0.5f + 0.037f;
    private const float FieldX = 0.022f;
    private const float FieldWidth = 0.068f;
    private const float PickX = 0.072f;
    private const float SlotX = 0.036f;
    private const float SlotWidth = 0.098f;
    private const float AttachedTapeDepth = 0.0155f;
    public static Vector2 AttachedTapeSize => new Vector2(SlotWidth - 0.008f, AttachedTapeDepth);
    private const float RowDepth = 0.02f;
    /// <summary>T08: the certificate tab sticks out of the clipboard's left edge (counted in the lifted bounds).</summary>
    private const float TabWidth = 0.024f;
    private const float TabX = -(LeafWidth + 0.014f) * 0.5f - TabWidth * 0.5f + 0.003f;
    /// <summary>T09: the stamp box under the header, top right (the subtitle keeps the left).</summary>
    public const float StampX = 0.051f;
    public const float StampZ = 0.104f;
    public const float StampW = 0.08f;
    public const float StampD = 0.024f;
    public static readonly Color ReadyInk = new Color32(40, 122, 78, 255);
    public static readonly Color HoldInk = new Color32(176, 52, 46, 255);
    private static readonly Color AuditorRed = new Color32(196, 58, 50, 255);
    private const float PopSeconds = 0.22f;
    private static readonly Color Graphite = new Color32(74, 79, 87, 255);
    private static readonly Color Pending = new Color32(160, 160, 152, 255);
    private static readonly Color StaleInk = new Color32(140, 146, 154, 255);

    private sealed class SlotView
    {
        public string Field;
        public Vector3 Center;
        public TextMeshPro State;
        public GameObject Armed;
        public Transform Tape;
        public TextMeshPro Print;
        public Transform Stale;
        public Transform Typed;
        public string ShownSignature;
        public bool ShownStale;
    }

    public Transform Root { get; private set; }
    public Collider BodyTarget { get; private set; }
    public Collider MoveHandle { get; private set; }
    public Collider[] MoveBorders { get; private set; }
    public bool IsMoveHandle(Collider target) => target == BodyTarget || target == MoveHandle || (MoveBorders != null && System.Array.IndexOf(MoveBorders, target) >= 0);
    public Bounds UnitBounds { get; private set; }

    private readonly Level3DeskProps props;
    private readonly Dictionary<Collider, Hit> targets = new Dictionary<Collider, Hit>();
    private readonly Dictionary<string, SlotView> slots = new Dictionary<string, SlotView>();
    private readonly Dictionary<string, TextMeshPro> totals = new Dictionary<string, TextMeshPro>();
    private readonly Dictionary<string, GameObject> sourceFrames = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, TextMeshPro> constantValues = new Dictionary<string, TextMeshPro>();
    private readonly List<GameObject> constantChips = new List<GameObject>();
    private readonly List<(Transform target, float start, Vector3 scale, Vector3 position, bool settle)> pops = new List<(Transform, float, Vector3, Vector3, bool)>();
    private TextMeshPro band;
    private TextMeshPro bandHint;
    private TextMeshPro message;
    private GameObject certificateTab;
    private Transform stampBox;
    private GameObject readyInk, holdInk, stampHint;
    private Transform staleStamp;
    private Transform ghost;
    private TextMeshPro ghostText;
    private string ghostType;
    private Transform reviewMark;
    private readonly Dictionary<string, float> rowZ = new Dictionary<string, float>();
    private Transform picker;
    private TextMeshPro pickerTitle;
    private readonly List<GameObject> pickerChips = new List<GameObject>();

    private Level3ComputationLeafModel(Level3DeskProps props)
    {
        this.props = props;
    }

    public static Level3ComputationLeafModel Build(Transform parent, Level3DeskProps props, string title, string subtitle)
    {
        var model = new Level3ComputationLeafModel(props);
        model.Create(parent, title, subtitle);
        return model;
    }

    // =========================================================
    // BUILD
    // =========================================================

    private void Create(Transform parent, string title, string subtitle)
    {
        var root = new GameObject("Computation Sheet");
        root.layer = parent.gameObject.layer;
        root.transform.SetParent(parent, false);
        Root = root.transform;

        // Clipboard, working leaf, brass clip (the practice card's construction, a sheet's size).
        props.Box("Clipboard", Root, new Vector3(0f, 0.002f, 0f), new Vector3(LeafWidth + 0.014f, 0.004f, LeafLength + 0.022f), Level3DeskProps.Navy);
        props.Box("Leaf", Root, new Vector3(0f, 0.00475f, -0.004f), new Vector3(LeafWidth, 0.0015f, LeafLength - 0.008f), Level3DeskProps.Paper);
        props.Box("Leaf Edge", Root, new Vector3(0.0012f, 0.0041f, -0.0052f), new Vector3(LeafWidth, 0.0004f, LeafLength - 0.008f), Level3DeskProps.Cream);
        props.Box("Clip Base", Root, new Vector3(0f, 0.0065f, LeafLength * 0.5f + 0.002f), new Vector3(0.056f, 0.004f, 0.016f), Level3DeskProps.Brass);
        props.Box("Clip Lever", Root, new Vector3(0f, 0.0095f, LeafLength * 0.5f - 0.004f), new Vector3(0.038f, 0.0025f, 0.012f), Level3DeskProps.BrassSoft);
        MoveHandle = props.Target("Move paper by clip", Root, new Vector3(0f, 0.012f, LeafLength * 0.5f), new Vector3(0.062f, 0.012f, 0.022f));
        MoveBorders = new Collider[]
        {
            props.Target("Move sheet left edge", Root, new Vector3(-LeafWidth * 0.5f - 0.004f, 0.005f, 0f), new Vector3(0.008f, 0.012f, LeafLength + 0.01f)),
            props.Target("Move sheet right edge", Root, new Vector3(LeafWidth * 0.5f + 0.004f, 0.005f, 0f), new Vector3(0.008f, 0.012f, LeafLength + 0.01f)),
            props.Target("Move sheet bottom edge", Root, new Vector3(0f, 0.005f, -LeafLength * 0.5f - 0.006f), new Vector3(LeafWidth + 0.014f, 0.012f, 0.01f))
        };

        props.Box("Header", Root, new Vector3(0f, Surface + 0.0002f, 0.128f), new Vector3(LeafWidth * 0.96f, 0.0006f, 0.022f), Level3DeskProps.Navy);
        props.Print("Title", Root, new Vector3(0f, Surface + 0.0007f, 0.128f), InnerWidth, 0.013f, title, Level3DeskProps.Cream, bold: true);
        // The subtitle keeps the left half; the stamp box takes the right (T09).
        props.Print("Subtitle", Root, new Vector3(LabelX + 0.016f, PrintY, 0.106f), 0.1f, 0.0085f, subtitle, Level3DeskProps.InkMuted, TextAlignmentOptions.Left);
        StampBox();
        ReviewMark();

        // Rows 22 mm apart (T08 added Tax credits and Remaining payable under Tax due).
        TotalRow(Level3ComputationRules.Gross, 0.086f);
        TotalRow(Level3ComputationRules.Expenses, 0.064f);
        SlotRow(Level3ComputationRules.Taxable, 0.042f, false);
        BandRow(0.020f);
        ConstantsRow(-0.002f);
        SlotRow(Level3ComputationRules.Excess, -0.024f, true);
        SlotRow(Level3ComputationRules.Marginal, -0.046f, true);
        SlotRow(Level3ComputationRules.TaxDue, -0.068f, false);
        TotalRow(Level3ComputationRules.Credits, -0.090f);
        SlotRow(Level3ComputationRules.Payable, -0.112f, false);
        CertificateTab(-0.090f);
        message = props.Print("Message", Root, new Vector3(0f, PrintY, -0.133f), InnerWidth, 0.022f, "", Level3DeskProps.InkMuted, TextAlignmentOptions.Center, wrap: true);
        message.fontSizeMax = 0.0085f;
        BuildPicker();

        // Below every row target, so a row is always the nearest hit; the whole sheet lifts the tools from the desk.
        BodyTarget = props.Target("Sheet Target", Root, new Vector3(0f, 0.0025f, 0f), new Vector3(LeafWidth + 0.014f, 0.005f, LeafLength + 0.022f));
        targets[BodyTarget] = new Hit { Kind = HitKind.Body };
        // The lifted sheet keeps its certificate tab on screen too.
        float left = TabX - TabWidth * 0.5f, right = (LeafWidth + 0.014f) * 0.5f;
        UnitBounds = new Bounds(new Vector3((left + right) * 0.5f, 0.006f, 0f), new Vector3(right - left, 0.012f, LeafLength + 0.03f));
    }

    /// <summary>T08: a blue certificate corner clipped behind the clipboard's left edge at the Tax credits row (shown once Bea hands it over).</summary>
    private void CertificateTab(float z)
    {
        var tab = new GameObject("Certificate Tab");
        tab.layer = Root.gameObject.layer;
        tab.transform.SetParent(Root, false);
        tab.transform.localPosition = new Vector3(TabX, 0.0016f, z);
        tab.transform.localRotation = Quaternion.Euler(0f, -6f, 0f);
        const float depth = 0.036f;
        props.Box("Sheet", tab.transform, new Vector3(0f, 0.0006f, 0f), new Vector3(TabWidth, 0.0012f, depth), Level3DeskProps.Paper);
        props.Box("Header", tab.transform, new Vector3(0f, 0.0013f, depth * 0.5f - 0.004f), new Vector3(TabWidth, 0.0004f, 0.008f), CertificateBlue);
        props.Box("Withheld Band", tab.transform, new Vector3(0f, 0.0013f, -depth * 0.5f + 0.006f), new Vector3(TabWidth - 0.004f, 0.0004f, 0.007f), Level3DeskProps.Cream);
        props.Print("Label", tab.transform, new Vector3(0f, 0.0017f, -0.002f), TabWidth - 0.003f, 0.007f, "2307", CertificateBlue, bold: true);
        targets[props.Target("Target", tab.transform, new Vector3(0f, 0.002f, 0f), new Vector3(TabWidth + 0.004f, 0.008f, depth + 0.004f))] =
            new Hit { Kind = HitKind.Certificate, Id = Level3ChapterRules.BeaCreditSource };
        tab.SetActive(false);
        certificateTab = tab;
    }

    /// <summary>T09: the stamp box: an outline, its two inks (shown from the saved stamp), the out-of-date tag, the pick ghost and its own target.</summary>
    private void StampBox()
    {
        stampBox = new GameObject("Stamp Box").transform;
        stampBox.gameObject.layer = Root.gameObject.layer;
        stampBox.SetParent(Root, false);
        stampBox.localPosition = new Vector3(StampX, Surface, StampZ);
        props.Frame("Edge", stampBox, new Vector3(0f, 0.0005f, 0f), StampW, StampD, 0.0006f, Level3DeskProps.InkMuted);
        stampHint = props.Print("Hint", stampBox, new Vector3(0f, 0.0007f, 0f), StampW - 0.008f, 0.006f, "stamp here", Level3DeskProps.InkMuted).gameObject;
        readyInk = Ink("Ready Ink", "READY FOR\nCOMPUTATION REVIEW", ReadyInk);
        holdInk = Ink("Not Ready Ink", "NOT READY", HoldInk);
        staleStamp = Tag(stampBox, StampW, "STAMP OUT OF DATE");
        staleStamp.localPosition = new Vector3(0f, 0.0024f, -0.002f);
        ghost = new GameObject("Ghost").transform;
        ghost.gameObject.layer = Root.gameObject.layer;
        ghost.SetParent(stampBox, false);
        ghost.localPosition = new Vector3(0f, 0.0012f, 0f);
        props.Frame("Outline", ghost, Vector3.zero, StampW - 0.002f, StampD - 0.002f, 0.0012f, Level3DeskProps.Highlight);
        ghostText = props.Print("Text", ghost, new Vector3(0f, 0.0003f, 0f), StampW - 0.008f, StampD * 0.7f, "", Level3DeskProps.Ink, bold: true, wrap: true);
        ghost.gameObject.SetActive(false);
        targets[props.Target("Stamp Target", stampBox, new Vector3(0f, 0.003f, 0f), new Vector3(StampW + 0.004f, 0.008f, StampD + 0.004f))] =
            new Hit { Kind = HitKind.StampBox, Id = Level3ReviewRules.StampField };
    }

    private GameObject Ink(string name, string text, Color color)
    {
        var ink = new GameObject(name);
        ink.layer = Root.gameObject.layer;
        ink.transform.SetParent(stampBox, false);
        ink.transform.localPosition = new Vector3(0f, 0.0009f, 0f);
        ink.transform.localRotation = Quaternion.Euler(0f, -5f, 0f);
        props.Frame("Ring", ink.transform, Vector3.zero, StampW - 0.004f, StampD - 0.004f, 0.0011f, color);
        props.Print("Text", ink.transform, new Vector3(0f, 0.0003f, 0f), StampW - 0.012f, StampD * 0.66f, text, color, bold: true, wrap: true);
        ink.SetActive(false);
        return ink.gameObject;
    }

    /// <summary>T09: the Auditor's red-pencil mark (an underline and START HERE) moved to the line a returned review names.</summary>
    private void ReviewMark()
    {
        reviewMark = new GameObject("Auditor Mark").transform;
        reviewMark.gameObject.layer = Root.gameObject.layer;
        reviewMark.SetParent(Root, false);
        props.Box("Underline", reviewMark, new Vector3(0.036f, 0.0003f, -0.0072f), new Vector3(0.074f, 0.0005f, 0.0011f), AuditorRed);
        props.Box("Tick", reviewMark, new Vector3(-0.003f, 0.0003f, 0f), new Vector3(0.0016f, 0.0005f, 0.016f), AuditorRed);
        props.Print("Note", reviewMark, new Vector3(0.036f, 0.0006f, -0.0098f), 0.074f, 0.0042f, "AUDITOR: START HERE", AuditorRed, TextAlignmentOptions.Left, bold: true);
        reviewMark.gameObject.SetActive(false);
    }

    /// <summary>The stamp box's centre in the world (a pressing stamp aims here).</summary>
    public Vector3 StampBoxWorld => stampBox != null ? stampBox.position : Root.position;

    /// <summary>A pulsing ghost of the picked stamp's imprint on the box (null hides it).</summary>
    public void ShowStampGhost(string type)
    {
        ghostType = type;
        if (ghost == null) return;
        ghost.gameObject.SetActive(type != null);
        if (type == null) return;
        ghostText.text = type == Level3ReviewRules.StampReady ? "READY FOR\nCOMPUTATION REVIEW" : "NOT READY";
    }

    /// <summary>The ink just landed: it pops in.</summary>
    public void PopStamp()
    {
        if (readyInk != null && readyInk.activeSelf) StartPop(readyInk.transform);
        if (holdInk != null && holdInk.activeSelf) StartPop(holdInk.transform);
    }

    private void ShowReview(Level3ChapterCheckpoint state, string caseId)
    {
        Level3StampRecord stamp = Level3ReviewRules.StampOf(state, caseId) ?? new Level3StampRecord();
        bool stale = Level3ReviewRules.StampStale(state, caseId);
        if (readyInk.activeSelf != (stamp.type == Level3ReviewRules.StampReady)) readyInk.SetActive(stamp.type == Level3ReviewRules.StampReady);
        if (holdInk.activeSelf != (stamp.type == Level3ReviewRules.StampHold)) holdInk.SetActive(stamp.type == Level3ReviewRules.StampHold);
        stampHint.SetActive(stamp.type.Length == 0);
        staleStamp.gameObject.SetActive(stale);
        Level3ReviewRecord review = state?.Case(caseId)?.review;
        bool marked = review != null && !review.approved && review.lastField.Length > 0 && review.lastOutcome.Length > 0 && review.lastOutcome != "approved";
        reviewMark.gameObject.SetActive(marked);
        if (!marked) return;
        if (review.lastField == Level3ReviewRules.StampField) reviewMark.localPosition = new Vector3(StampX - StampW * 0.5f - 0.004f, PrintY, StampZ - 0.004f);
        else if (rowZ.TryGetValue(review.lastField, out float z)) reviewMark.localPosition = new Vector3(LabelX + 0.003f, PrintY, z);
        else reviewMark.gameObject.SetActive(false);
    }

    /// <summary>The Type 8 header blue (SupportingDocumentFace.WithholdingBlue).</summary>
    private static readonly Color CertificateBlue = new Color32(52, 104, 132, 255);

    private TextMeshPro Label(string field, float z, bool working)
    {
        string text = Level3ComputationRules.SectionLabel(field) + (working ? "  (working)" : "");
        return props.Print(field + " Label", Root, new Vector3(LabelX, PrintY, z), 0.072f, 0.0095f, text, Level3DeskProps.Ink, TextAlignmentOptions.Left, bold: !working);
    }

    private void TotalRow(string field, float z)
    {
        rowZ[field] = z;
        Label(field, z, false);
        var center = new Vector3(FieldX, Surface, z);
        props.Box(field + " Field", Root, center + new Vector3(0f, 0.0003f, 0f), new Vector3(FieldWidth, 0.0006f, RowDepth), Level3DeskProps.Cream);
        props.Frame(field + " Edge", Root, center + new Vector3(0f, 0.0006f, 0f), FieldWidth, RowDepth, 0.0009f, Level3DeskProps.InkMuted);
        totals[field] = props.Print(field + " Value", Root, center + new Vector3(0f, 0.0009f, 0f), FieldWidth - 0.005f, 0.015f, "", Level3DeskProps.Ink,
            TextAlignmentOptions.Right, wrap: true);
        Source(Level3ComputationRules.FieldSource(Level3ChapterRules.BeaCaseId, field), center, FieldWidth, RowDepth);
        targets[props.Target(field + " Target", Root, center + new Vector3(0f, 0.002f, 0f), new Vector3(FieldWidth, 0.006f, RowDepth))] = new Hit { Kind = HitKind.Total, Id = field };
        PickKey(field + " Pick", new Vector3(PickX, Surface, z), HitKind.Pick, field);
    }

    private void PickKey(string name, Vector3 center, HitKind kind, string id)
    {
        var size = new Vector2(0.024f, RowDepth);
        var key = new GameObject(name);
        key.layer = Root.gameObject.layer;
        key.transform.SetParent(Root, false);
        key.transform.localPosition = center;
        props.Box("Key", key.transform, new Vector3(0f, 0.0012f, 0f), new Vector3(size.x, 0.0024f, size.y), Level3DeskProps.Brass);
        props.Print("Label", key.transform, new Vector3(0f, 0.0027f, 0f), size.x - 0.004f, size.y * 0.5f, "PICK", Level3DeskProps.Ink, bold: true);
        targets[props.Target("Target", key.transform, new Vector3(0f, 0.002f, 0f), new Vector3(size.x + 0.002f, 0.007f, size.y))] = new Hit { Kind = kind, Id = id };
    }

    private void BandRow(float z)
    {
        rowZ[Level3ComputationRules.Rule] = z;
        Label(Level3ComputationRules.Rule, z, false);
        var center = new Vector3(FieldX, Surface, z);
        props.Box("Band Field", Root, center + new Vector3(0f, 0.0003f, 0f), new Vector3(FieldWidth, 0.0006f, RowDepth), Level3DeskProps.Cream);
        props.Frame("Band Edge", Root, center + new Vector3(0f, 0.0006f, 0f), FieldWidth, RowDepth, 0.0009f, Level3DeskProps.InkMuted);
        band = props.Print("Band Value", Root, center + new Vector3(0f, 0.0009f, 0f), FieldWidth - 0.005f, 0.015f, "", Level3DeskProps.Ink, TextAlignmentOptions.Right, wrap: true);
        targets[props.Target("Band Target", Root, center + new Vector3(0f, 0.002f, 0f), new Vector3(FieldWidth, 0.006f, RowDepth))] = new Hit { Kind = HitKind.Band, Id = Level3ComputationRules.Rule };
        PickKey("Band Pick", new Vector3(PickX, Surface, z), HitKind.Band, Level3ComputationRules.Rule);
    }

    /// <summary>The chosen band's floor, base tax and rate as three printed source chips (hidden until a band is chosen).</summary>
    private void ConstantsRow(float z)
    {
        bandHint = props.Print("Band Hint", Root, new Vector3(0f, PrintY, z), InnerWidth, 0.008f, "", Level3DeskProps.InkMuted);
        string[] parts = { Level3ComputationRules.Lower, Level3ComputationRules.Base, Level3ComputationRules.Rate };
        string[] names = { "FLOOR", "BASE TAX", "RATE" };
        for (int i = 0; i < parts.Length; i++)
        {
            var chip = new GameObject("Constant " + parts[i]);
            chip.layer = Root.gameObject.layer;
            chip.transform.SetParent(Root, false);
            chip.transform.localPosition = new Vector3(-0.058f + i * 0.058f, Surface, z);
            props.Box("Chip", chip.transform, new Vector3(0f, 0.0011f, 0f), new Vector3(0.054f, 0.0022f, 0.019f), Level3DeskProps.NavyLight);
            props.Print("Name", chip.transform, new Vector3(0f, 0.0025f, 0.005f), 0.05f, 0.0055f, names[i], Level3DeskProps.BrassSoft, bold: true);
            constantValues[parts[i]] = props.Print("Value", chip.transform, new Vector3(0f, 0.0025f, -0.0025f), 0.05f, 0.0085f, "", Level3DeskProps.Cream, bold: true);
            GameObject frame = props.Frame("Selected", chip.transform, new Vector3(0f, 0.0026f, 0f), 0.056f, 0.021f, 0.0016f, Level3DeskProps.Highlight);
            frame.SetActive(false);
            sourceFrames[parts[i]] = frame;
            targets[props.Target("Target", chip.transform, new Vector3(0f, 0.003f, 0f), new Vector3(0.056f, 0.008f, 0.021f))] = new Hit { Kind = HitKind.Constant, Id = parts[i] };
            chip.SetActive(false);
            constantChips.Add(chip);
        }
    }

    private void SlotRow(string field, float z, bool working)
    {
        rowZ[field] = z;
        Label(field, z, working);
        var slot = new SlotView { Field = field, Center = new Vector3(SlotX, Surface, z) };
        props.Box(field + " Pocket", Root, slot.Center + new Vector3(0f, 0.0003f, 0f), new Vector3(SlotWidth, 0.0006f, RowDepth), Level3DeskProps.Cream);
        props.Frame(field + " Outline", Root, slot.Center + new Vector3(0f, 0.0006f, 0f), SlotWidth, RowDepth, 0.0009f, Level3DeskProps.InkMuted);
        slot.State = props.Print(field + " State", Root, slot.Center + new Vector3(0f, 0.0009f, 0f), SlotWidth - 0.008f, 0.0075f, "", Level3DeskProps.InkMuted);
        slot.Armed = props.Frame(field + " Armed", Root, slot.Center + new Vector3(0f, 0.0016f, 0f), SlotWidth + 0.003f, RowDepth + 0.003f, 0.0022f, Level3DeskProps.Brass);
        slot.Armed.SetActive(false);

        var tape = new GameObject(field + " Tape").transform;
        tape.gameObject.layer = Root.gameObject.layer;
        tape.SetParent(Root, false);
        tape.localPosition = slot.Center + new Vector3(0.002f, 0.0012f, 0f);
        float width = AttachedTapeSize.x;
        props.Box("Shadow", tape, new Vector3(0.0012f, -0.0006f, -0.0012f), new Vector3(width, 0.0004f, AttachedTapeDepth), Level3DeskProps.InkMuted);
        props.Box("Paper", tape, Vector3.zero, new Vector3(width, 0.0011f, AttachedTapeDepth), Level3DeskProps.Paper);
        props.Box("Curl", tape, new Vector3(width * 0.5f - 0.0015f, 0.0007f, 0f), new Vector3(0.003f, 0.0014f, AttachedTapeDepth), Level3DeskProps.Cream);
        props.Box("Clip", tape, new Vector3(-width * 0.5f + 0.004f, 0.0016f, 0f), new Vector3(0.007f, 0.0028f, 0.018f), Level3DeskProps.Brass);
        slot.Print = props.Print("Print", tape, new Vector3(0.003f, 0.0008f, 0f), width - 0.012f, 0.0105f, "", Level3DeskProps.Ink, TextAlignmentOptions.Right);
        GameObject frame = props.Frame("Selected", tape, new Vector3(0f, 0.0018f, 0f), width + 0.002f, 0.0175f, 0.0014f, Level3DeskProps.Highlight);
        frame.SetActive(false);
        sourceFrames[Level3ComputationRules.SheetSource(Level3ChapterRules.BeaCaseId, field)] = frame;
        slot.Stale = Tag(tape, width, "NEEDS RECALCULATION");
        slot.Typed = Tag(tape, width, "TYPED · NO SOURCE");
        tape.gameObject.SetActive(false);
        slot.Tape = tape;
        targets[props.Target(field + " Target", Root, slot.Center + new Vector3(0f, 0.004f, 0f), new Vector3(SlotWidth, 0.008f, RowDepth + 0.002f))] = new Hit { Kind = HitKind.Slot, Id = field };
        slots[field] = slot;
    }

    /// <summary>A graphite pencil tag laid across a tape (never a red failure mark).</summary>
    private Transform Tag(Transform tape, float width, string text)
    {
        var tag = new GameObject(text).transform;
        tag.gameObject.layer = Root.gameObject.layer;
        tag.SetParent(tape, false);
        tag.localPosition = new Vector3(-0.006f, 0.0022f, 0f);
        tag.localRotation = Quaternion.Euler(0f, -4f, 0f);
        props.Box("Tag", tag, Vector3.zero, new Vector3(width * 0.72f, 0.0006f, 0.0085f), Graphite);
        props.Print("Print", tag, new Vector3(0f, 0.0005f, 0f), width * 0.68f, 0.006f, text, Level3DeskProps.Cream, bold: true);
        tag.gameObject.SetActive(false);
        return tag;
    }

    private void Source(string sourceId, Vector3 center, float width, float depth)
    {
        GameObject frame = props.Frame("Selected", Root, center + new Vector3(0f, 0.0016f, 0f), width + 0.003f, depth + 0.003f, 0.0016f, Level3DeskProps.Highlight);
        frame.SetActive(false);
        sourceFrames[sourceId] = frame;
    }

    private void BuildPicker()
    {
        var go = new GameObject("Chooser");
        go.layer = Root.gameObject.layer;
        go.transform.SetParent(Root, false);
        go.transform.localPosition = new Vector3(0f, Surface + 0.014f, -0.006f);
        picker = go.transform;
        props.Box("Shadow", picker, new Vector3(0.003f, -0.0035f, -0.003f), new Vector3(LeafWidth - 0.004f, 0.0006f, LeafLength - 0.034f), new Color32(20, 26, 34, 255));
        props.Box("Card", picker, Vector3.zero, new Vector3(LeafWidth - 0.004f, 0.002f, LeafLength - 0.034f), Level3DeskProps.Navy);
        props.Frame("Edge", picker, new Vector3(0f, 0.0012f, 0f), LeafWidth - 0.004f, LeafLength - 0.034f, 0.0016f, Level3DeskProps.Brass);
        pickerTitle = props.Print("Title", picker, new Vector3(0f, 0.0013f, (LeafLength - 0.034f) * 0.5f - 0.016f), LeafWidth - 0.02f, 0.016f, "",
            Level3DeskProps.Cream, bold: true, wrap: true);
        targets[props.Target("Backdrop", picker, Vector3.zero, new Vector3(LeafWidth - 0.004f, 0.002f, LeafLength - 0.034f))] = new Hit { Kind = HitKind.None };
        picker.gameObject.SetActive(false);
    }

    // =========================================================
    // QUERIES
    // =========================================================

    public IEnumerable<KeyValuePair<Collider, Hit>> TransferTargets
    {
        get { foreach (var t in targets) if (t.Value.Kind == HitKind.Slot || t.Value.Kind == HitKind.Total || t.Value.Kind == HitKind.Constant) yield return t; }
    }

    public IEnumerable<KeyValuePair<Collider, string>> TapeTargets
    {
        get { foreach (var t in targets) if (t.Value.Kind == HitKind.Slot) yield return new KeyValuePair<Collider, string>(t.Key, t.Value.Id); }
    }

    public bool TryHit(Collider collider, out Hit hit)
    {
        hit = default;
        if (collider == null || !collider.transform.IsChildOf(Root) || !collider.gameObject.activeInHierarchy) return false;
        return targets.TryGetValue(collider, out hit);
    }

    public bool PickerOpen => picker != null && picker.gameObject.activeSelf;

    /// <summary>Where a step's tape lands (the attach flight ends here).</summary>
    public Vector3 SlotWorldPosition(string field) =>
        slots.TryGetValue(field, out SlotView slot) ? Root.TransformPoint(slot.Tape.localPosition) : Root.position;

    // =========================================================
    // STATE (quiet; pops only where asked)
    // =========================================================

    /// <summary>
    /// The sheet from the saved chapter: picked totals, the band and its
    /// constants, each step's tape (or what it waits for), stale and typed tags.
    /// hideField keeps a slot empty while its tape is still flying in.
    /// </summary>
    public void Show(Level3ChapterCheckpoint state, CaseDefinition definition, string hideField, bool animate)
    {
        string caseId = Level3ChapterRules.BeaCaseId;
        Level3ComputationRecord record = Level3ComputationRules.Record(state, caseId) ?? new Level3ComputationRecord();
        List<Level3TotalChoice> choices = Level3ComputationRules.TotalChoices(state, definition);
        ShowTotal(Level3ComputationRules.Gross, record.grossChoice, record.grossAmount, choices);
        ShowTotal(Level3ComputationRules.Expenses, record.expensesChoice, record.expensesAmount, choices);
        ShowReview(state, caseId);
        // T08: the credit comes from her certificate once she has handed it over.
        bool certificate = Level3CertificateRules.Acquired(state);
        if (certificateTab != null && certificateTab.activeSelf != certificate) certificateTab.SetActive(certificate);
        if (certificate || !string.IsNullOrEmpty(record.creditChoice))
            ShowTotal(Level3ComputationRules.Credits, record.creditChoice, record.creditAmount, Level3ComputationRules.CreditChoices(state, definition));
        else if (totals.TryGetValue(Level3ComputationRules.Credits, out TextMeshPro credits))
        {
            credits.text = "after Bea's certificate";
            credits.color = Pending;
        }

        Level3RuleDefinition rule = Level3ComputationRules.FindRule(definition, record.ruleId);
        band.text = rule != null ? Level3ComputationRules.BandLabel(rule) : "tap PICK: the annual table";
        band.color = rule != null ? Level3DeskProps.Ink : Level3DeskProps.InkMuted;
        foreach (GameObject chip in constantChips) chip.SetActive(rule != null);
        bandHint.text = rule != null ? "" : "Choose a band to see its floor, base tax and rate.";
        if (rule != null)
        {
            constantValues[Level3ComputationRules.Lower].text = Level3Calculator.Format(rule.lower);
            constantValues[Level3ComputationRules.Base].text = Level3Calculator.Format(rule.baseTax);
            constantValues[Level3ComputationRules.Rate].text = Level3ComputationRules.Percent(rule.rate);
        }

        foreach (SlotView slot in slots.Values)
        {
            Level3TapeRecord tape = Level3ComputationRules.TapeOf(state, caseId, slot.Field);
            bool shown = tape != null && slot.Field != hideField;
            if (slot.Tape.gameObject.activeSelf != shown) slot.Tape.gameObject.SetActive(shown);
            slot.State.text = shown ? "" : StepHint(slot.Field);
            if (!shown) { slot.ShownSignature = null; slot.ShownStale = false; continue; }
            bool stale = Level3ComputationRules.IsStale(state, definition, caseId, slot.Field);
            bool typed = !stale && Level3ComputationRules.HasTyped(tape);
            string value = tape.result;
            bool rounded = false;
            if ((slot.Field == Level3ComputationRules.TaxDue || slot.Field == Level3ComputationRules.Payable) && Level3Calculator.TryParse(tape.result, out decimal exact))
            {
                decimal cents = Level3ComputationRules.RoundCents(exact);
                rounded = cents != exact;
                value = Level3Calculator.Canonical(cents);
            }
            List<string> lines = Level3Calculator.ChainLines(tape.expression);
            string working = lines.Count > 2 ? (lines.Count - 1) + " LINES  " : "";
            slot.Print.text = working + "<b>= " + Level3Calculator.Format(value) + "</b>" + (rounded ? " (cents)" : "");
            slot.Print.color = stale ? StaleInk : Level3DeskProps.Ink;
            string signature = tape.actionId;
            bool newlyStale = stale && !slot.ShownStale && slot.ShownSignature == signature;
            slot.Stale.gameObject.SetActive(stale);
            slot.Typed.gameObject.SetActive(typed);
            if (newlyStale && animate) StartPop(slot.Stale);
            slot.ShownSignature = signature;
            slot.ShownStale = stale;
        }
    }

    private void ShowTotal(string field, string choiceId, string amount, List<Level3TotalChoice> choices)
    {
        if (!totals.TryGetValue(field, out TextMeshPro text)) return;
        if (string.IsNullOrEmpty(choiceId))
        {
            text.text = "tap PICK";
            text.color = Level3DeskProps.InkMuted;
            return;
        }
        Level3TotalChoice choice = choices.Find(c => c.ChoiceId == choiceId);
        string from = choice != null ? choice.Label.Replace("Worksheet · ", "worksheet ").Replace("Bea's draft · ", "Bea's draft ").Replace("Certificate ", "certificate ").Replace(" · ", " ") : "";
        text.text = "<b>" + Level3Calculator.Format(amount) + "</b>\n<size=70%>" + from + "</size>";
        text.color = Level3DeskProps.Ink;
    }

    private static string StepHint(string field)
    {
        switch (field)
        {
            case Level3ComputationRules.Taxable: return "a tape: gross − expenses";
            case Level3ComputationRules.Excess: return "optional tape";
            case Level3ComputationRules.Marginal: return "optional tape";
            case Level3ComputationRules.TaxDue: return "a tape: the band's formula";
            case Level3ComputationRules.Payable: return "a tape: tax due − credits";
            default: return "";
        }
    }

    /// <summary>Highlights the amount now on the calculator (a total, a constant or a tape); null clears.</summary>
    public void SetSelectedSource(string key)
    {
        foreach (KeyValuePair<string, GameObject> frame in sourceFrames) frame.Value.SetActive(frame.Key == key);
    }

    /// <summary>The slot waiting for a second tap to confirm (destination named first); null clears.</summary>
    public void SetArmed(string field)
    {
        foreach (SlotView slot in slots.Values) slot.Armed.SetActive(slot.Field == field);
    }

    public void SetMessage(string text)
    {
        if (message != null && message.text != (text ?? "")) message.text = text ?? "";
    }

    /// <summary>A tape just landed in its slot: it settles in with a small pop.</summary>
    public void PopSlot(string field)
    {
        if (field != null && slots.TryGetValue(field, out SlotView slot) && slot.Tape.gameObject.activeSelf) StartPop(slot.Tape, settle: true);
    }

    private void StartPop(Transform target, bool settle = false)
    {
        if (target == null) return;
        for (int i = pops.Count - 1; i >= 0; i--)
            if (pops[i].target == target)
            { target.localScale = pops[i].scale; target.localPosition = pops[i].position; pops.RemoveAt(i); }
        pops.Add((target, Time.unscaledTime, target.localScale, target.localPosition, settle));
        if (settle) target.localPosition += Level3CalculatorModel.PlacementOffset(0f);
    }

    // =========================================================
    // CHOOSER (totals, or the annual table's bands)
    // =========================================================

    /// <summary>Opens the chip card: one chip per (id, label) pair plus Cancel, all styled alike; the current value keeps a thin frame.</summary>
    public void OpenPicker(string title, IList<KeyValuePair<string, string>> choices, string current)
    {
        ClosePicker();
        pickerTitle.text = title;
        float top = (LeafLength - 0.034f) * 0.5f - 0.036f;
        float room = LeafLength - 0.034f - 0.05f;
        float spacing = Mathf.Min(0.034f, room / Mathf.Max(1, choices.Count + 1));
        float z = top;
        for (int i = 0; i < choices.Count; i++, z -= spacing)
            pickerChips.Add(Chip(choices[i].Key, choices[i].Value, z, spacing - 0.004f, choices[i].Key == current, HitKind.Chip));
        pickerChips.Add(Chip(null, "CANCEL", z - 0.004f, spacing - 0.004f, false, HitKind.Cancel));
        picker.gameObject.SetActive(true);
        StartPop(picker);
    }

    private GameObject Chip(string id, string text, float z, float depth, bool current, HitKind kind)
    {
        var chip = new GameObject("Chip");
        chip.layer = Root.gameObject.layer;
        chip.transform.SetParent(picker, false);
        chip.transform.localPosition = new Vector3(0f, 0.001f, z);
        float width = LeafWidth - 0.02f;
        Color body = kind == HitKind.Cancel ? Level3DeskProps.NavyLight : Level3DeskProps.Cream;
        Color ink = kind == HitKind.Cancel ? Level3DeskProps.Cream : Level3DeskProps.Ink;
        props.Box("Body", chip.transform, new Vector3(0f, 0.001f, 0f), new Vector3(width, 0.002f, depth), body);
        props.Print("Label", chip.transform, new Vector3(0f, 0.0023f, 0f), width - 0.01f, depth * 0.74f, text, ink, TextAlignmentOptions.Center, bold: kind == HitKind.Cancel, wrap: true);
        if (current) props.Frame("Current", chip.transform, new Vector3(0f, 0.0024f, 0f), width + 0.002f, depth + 0.002f, 0.0012f, Level3DeskProps.Highlight);
        targets[props.Target("Target", chip.transform, new Vector3(0f, 0.002f, 0f), new Vector3(width, 0.005f, depth))] = new Hit { Kind = kind, Id = id };
        return chip;
    }

    public void ClosePicker()
    {
        foreach (GameObject chip in pickerChips)
        {
            if (chip == null) continue;
            foreach (Collider collider in chip.GetComponentsInChildren<Collider>(true)) targets.Remove(collider);
            Object.Destroy(chip);
        }
        pickerChips.Clear();
        if (picker != null) picker.gameObject.SetActive(false);
    }

    // =========================================================
    // MOTION
    // =========================================================

    public void FinishPops()
    {
        foreach (var pop in pops)
            if (pop.target != null) { pop.target.localScale = pop.scale; pop.target.localPosition = pop.position; }
        pops.Clear();
    }
    public void Tick()
    {
        float now = Time.unscaledTime;
        if (ghost != null && ghost.gameObject.activeSelf)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(now * 5f);
            ghost.localScale = Vector3.one * (1f + 0.04f * pulse);
            Color ink = ghostType == Level3ReviewRules.StampReady ? ReadyInk : HoldInk;
            ghostText.color = new Color(ink.r, ink.g, ink.b, 0.28f + 0.22f * pulse);
        }
        for (int i = pops.Count - 1; i >= 0; i--)
        {
            (Transform target, float start, Vector3 scale, Vector3 position, bool settle) = pops[i];
            if (target == null) { pops.RemoveAt(i); continue; }
            float t = Mathf.Clamp01((now - start) / PopSeconds);
            target.localScale = Level3CalculatorModel.PlacementScale(scale, t);
            if (settle) target.localPosition = position + Level3CalculatorModel.PlacementOffset(t);
            if (t >= 1f) pops.RemoveAt(i);
        }
    }
}
