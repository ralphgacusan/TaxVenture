using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Bea's worksheet (T05) as a physical object on her table: a navy board
/// holding three cream panels, built from Level3DeskProps (boxes with real
/// thickness, flat 3D print), like the T03 practice card.
///
/// - LEFT, BEA'S RECORDS: four source tabs (the papers the worksheet rows
///   come from), the rows printed on the selected one with "+ ADD" (or
///   "ON SHEET" once lifted), READ ORIGINAL, and the pile of lifted strips
///   not yet placed. After the first check its frozen result sits at the
///   bottom of this column (T06).
/// - CENTER, the sheet "WORTH THE TAX? · BEA 2026": four labelled trays
///   (Income, Allowable expenses, Not deductible, Supporting only). Income
///   and Allowable expenses end in a SUBTOTAL slot: tap it to bring up the
///   calculator; an attached tape clips into it, and a pencil tag marks it
///   "CHANGED · RECALCULATE" when its strips change (T06).
/// - RIGHT, CLAIM NOTES: the two questions about Bea's draft claims, each
///   with a Reason field (and Related entry on the printing note), the
///   UNDO / HINT / CHECK keys (COLLECT WORKSHEET once it checks out) and one
///   line of plain guidance.
///
/// Strips are small paper slabs printed with their row label, amount and
/// source; they glide (0.2 s) to wherever the saved draft puts them. Every
/// tray, tab, button, strip, slot and field has its own tap target. Until a
/// deliberate check nothing is tinted or ordered by correctness; after one,
/// each checked place carries a brass tick (confirmed) or a pencil "?" (look
/// again) only while its checked value is unchanged. Presentation only:
/// Level3WorksheetRules / Level3WorksheetCheck and the chapter commands
/// decide what changes. Local frame: lying flat, +Y up, +Z away from the player.
/// </summary>
public sealed class Level3WorksheetModel
{
    public const float BoardWidth = 0.50f;
    public const float BoardDepth = 0.26f;
    public const float StripWidth = 0.094f;
    public const float StripDepth = 0.0115f;
    /// <summary>The sheet column's width (the calculator stands beside it while subtotals are worked out).</summary>
    public const float SheetWidth = 0.21f;

    public enum HitKind { None, Board, Tab, Add, ReadOriginal, Strip, Tray, Reason, Related, Undo, Chip, Cancel, Page, Subtotal, Hint, Check, Collect, Receipt }

    public struct Hit
    {
        public HitKind Kind;
        public string Id;
    }

    /// <summary>What the CHECK key says (T06).</summary>
    public enum CheckKey { Check, Confirm, Again, Collect, Hidden }

    private const float Surface = 0.006f;
    private const float Panel = Surface + 0.0012f;
    private const float PrintY = Panel + 0.0003f;
    private const float RailX = -0.178f;
    private const float NotesX = 0.178f;
    private const float ColumnWidth = 0.13f;
    private const float InnerDepth = 0.25f;
    private const float StackTop = -0.064f;
    private const float StackBottom = -0.12f;
    private const float StackBottomWithStub = -0.09f;
    private const float GlideRate = 18f;
    private const float KeyRowZ = -0.053f;
    private const float KeyDepth = 0.021f;
    private const float MarkSize = 0.0092f;
    private const float PopSeconds = 0.22f;
    private static readonly Color NotePaper = new Color32(247, 232, 172, 255);
    private static readonly Color TrayTint = new Color32(238, 231, 212, 255);
    /// <summary>Pencil graphite: "look again", "needed" and stale tags (never a red failure colour).</summary>
    private static readonly Color Graphite = new Color32(74, 79, 87, 255);
    private static readonly Color PencilWood = new Color32(232, 184, 74, 255);
    private static readonly Color StaleInk = new Color32(140, 146, 154, 255);
    private static readonly Color KeyOff = new Color32(198, 194, 182, 255);

    private sealed class StripView
    {
        public string RowId;
        public Transform Root;
        public BoxCollider Target;
        public GameObject Frame;
        public GameObject SourceFrame;
        public Vector3 Goal;
        public bool Dragging;
        public float PulseStart = -1f;
    }

    private sealed class TrayView
    {
        public string Section;
        public Vector3 Center;
        public Vector2 Size;
        public bool HasSlot;
        public Vector3 SlotCenter;
        public Vector2 SlotSize;
        public TextMeshPro SlotState;
        public GameObject SlotArmed;
        public GameObject Adding;
        public Transform Tape;
        public TextMeshPro TapePrint;
        public Transform Stale;
        public string ShownTape;
        public bool ShownStale;
    }

    private sealed class RowButton
    {
        public TextMeshPro Text;
        public TextMeshPro Button;
        public GameObject Body;
        public BoxCollider Target;
        public string RowId;
    }

    private sealed class MarkView
    {
        public Transform Root;
        public GameObject Tick;
        public GameObject Look;
        public Level3WorksheetCheck.Mark Shown;
        public float PopStart = -1f;
    }

    private sealed class KeyView
    {
        public Transform Root;
        public Transform Cap;
        public TextMeshPro Label;
        public MeshRenderer Body;
        public BoxCollider Target;
        public Vector3 Rest;
        public float PressedUntil;
    }

    public Transform Root { get; private set; }
    public Bounds UnitBounds { get; private set; }

    private readonly Level3DeskProps props;
    private readonly Dictionary<Collider, Hit> targets = new Dictionary<Collider, Hit>();
    private readonly Dictionary<string, StripView> strips = new Dictionary<string, StripView>();
    private readonly Dictionary<string, TrayView> trays = new Dictionary<string, TrayView>();
    private readonly Dictionary<string, GameObject> tabFrames = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, Vector3> tabCenters = new Dictionary<string, Vector3>();
    private readonly List<RowButton> rowButtons = new List<RowButton>();
    private readonly Dictionary<string, TextMeshPro> noteFields = new Dictionary<string, TextMeshPro>();
    private readonly Dictionary<string, Vector3> fieldCenters = new Dictionary<string, Vector3>();
    private readonly Dictionary<string, Level3WorksheetRow> rows = new Dictionary<string, Level3WorksheetRow>();
    private readonly Dictionary<string, MarkView> marks = new Dictionary<string, MarkView>();
    private readonly Dictionary<string, GameObject> needed = new Dictionary<string, GameObject>();
    private readonly Dictionary<Collider, KeyView> keys = new Dictionary<Collider, KeyView>();
    private TextMeshPro sourceHeading;
    private TextMeshPro message;
    private Transform picker;
    private TextMeshPro pickerTitle;
    private readonly List<GameObject> pickerChips = new List<GameObject>();
    private GameObject pageLeft, pageRight;
    private string selected;
    private string adding;
    private KeyView undoKey, hintKey, checkKey, collectKey;
    private CheckKey checkShown = CheckKey.Check;
    private bool hintEnabled;
    private float checkPulse;
    private float neededUntil = -1f;
    private float neededStart;
    private Transform stub;
    private TextMeshPro stubCount, stubTier;
    private SpriteRenderer stubMedal;
    private Transform receipt;
    private TextMeshPro receiptCount, receiptLines, receiptTier, receiptFooter;
    private SpriteRenderer receiptMedal;
    private Transform stamp;
    private float stampPopStart = -1f;
    private Transform pencil;

    private Level3WorksheetModel(Level3DeskProps props)
    {
        this.props = props;
    }

    public static Level3WorksheetModel Build(Transform parent, Level3DeskProps props, IList<Level3WorksheetRow> rows,
        Level3WorksheetRow partyClaim, Level3WorksheetRow printClaim)
    {
        var model = new Level3WorksheetModel(props);
        foreach (Level3WorksheetRow row in rows) model.rows[row.RowId] = row;
        model.Create(parent, partyClaim, printClaim);
        return model;
    }

    public static string Money(string amount) => Level3Calculator.Format(amount);

    public string StripLabel(string rowId) =>
        rows.TryGetValue(rowId, out Level3WorksheetRow row) ? $"{row.Label} {Money(row.Amount)} ({row.SourceId})" : rowId;

    public Level3WorksheetRow Row(string rowId) => rowId != null && rows.TryGetValue(rowId, out Level3WorksheetRow row) ? row : null;

    // =========================================================
    // BUILD
    // =========================================================

    private void Create(Transform parent, Level3WorksheetRow partyClaim, Level3WorksheetRow printClaim)
    {
        var root = new GameObject("Worksheet Board");
        root.layer = parent.gameObject.layer;
        root.transform.SetParent(parent, false);
        Root = root.transform;
        UnitBounds = new Bounds(new Vector3(0f, Surface * 0.5f, 0f), new Vector3(BoardWidth, Surface, BoardDepth));

        // The board, its brass corner caps, and one whole-board target (picked up from the table with it).
        props.Box("Board", Root, new Vector3(0f, Surface * 0.5f, 0f), new Vector3(BoardWidth, Surface, BoardDepth), Level3DeskProps.Navy);
        foreach (float x in new[] { -BoardWidth * 0.5f + 0.012f, BoardWidth * 0.5f - 0.012f })
            foreach (float z in new[] { -BoardDepth * 0.5f + 0.012f, BoardDepth * 0.5f - 0.012f })
                props.Box("Corner", Root, new Vector3(x, Surface + 0.0004f, z), new Vector3(0.016f, 0.0008f, 0.016f), Level3DeskProps.Brass);
        Add(props.Target("Board Target", Root, new Vector3(0f, 0.001f, 0f), new Vector3(BoardWidth, 0.002f, BoardDepth)), HitKind.Board, null);

        BuildRail();
        BuildSheet();
        BuildNotes(partyClaim, printClaim);
        BuildPicker();
        BuildStub();
        BuildReceipt();
        BuildStamp();
        BuildPencil();
    }

    private void PanelCard(string name, float x, float width, Color color)
    {
        props.Box(name, Root, new Vector3(x, Surface + 0.0006f, 0f), new Vector3(width, 0.0012f, InnerDepth), color);
    }

    private void Band(string name, float x, float width, string text)
    {
        props.Box(name + " Band", Root, new Vector3(x, Panel + 0.0003f, 0.113f), new Vector3(width - 0.006f, 0.0006f, 0.019f), Level3DeskProps.Navy);
        props.Print(name + " Title", Root, new Vector3(x, Panel + 0.0008f, 0.113f), width - 0.016f, 0.011f, text, Level3DeskProps.Cream, bold: true);
    }

    private GameObject Button(string name, Vector3 center, Vector2 size, string text, Color body, Color ink, HitKind kind, string id, out TextMeshPro label)
    {
        var button = new GameObject(name);
        button.layer = Root.gameObject.layer;
        button.transform.SetParent(Root, false);
        button.transform.localPosition = center;
        props.Box("Key", button.transform, new Vector3(0f, 0.0012f, 0f), new Vector3(size.x, 0.0024f, size.y), body);
        label = props.Print("Label", button.transform, new Vector3(0f, 0.0027f, 0f), size.x - 0.006f, size.y * 0.62f, text, ink, bold: true);
        Add(props.Target("Target", button.transform, new Vector3(0f, 0.002f, 0f), new Vector3(size.x, 0.006f, size.y)), kind, id);
        return button;
    }

    /// <summary>A key with a cap that presses down under the finger (UNDO, HINT, CHECK).</summary>
    private KeyView Key(string name, Vector3 center, Vector2 size, string text, Color body, Color ink, HitKind kind)
    {
        var holder = new GameObject(name);
        holder.layer = Root.gameObject.layer;
        holder.transform.SetParent(Root, false);
        holder.transform.localPosition = center;
        props.Box("Well", holder.transform, new Vector3(0f, 0.0003f, 0f), new Vector3(size.x + 0.002f, 0.0006f, size.y + 0.002f), Level3DeskProps.Navy);
        var cap = new GameObject("Cap").transform;
        cap.gameObject.layer = Root.gameObject.layer;
        cap.SetParent(holder.transform, false);
        GameObject capBody = props.Box("Key", cap, new Vector3(0f, 0.0015f, 0f), new Vector3(size.x, 0.003f, size.y), body);
        TextMeshPro label = props.Print("Label", cap, new Vector3(0f, 0.0032f, 0f), size.x - 0.006f, size.y * 0.6f, text, ink, bold: true);
        BoxCollider target = props.Target("Target", holder.transform, new Vector3(0f, 0.002f, 0f), new Vector3(size.x + 0.002f, 0.008f, size.y + 0.002f));
        Add(target, kind, null);
        var key = new KeyView { Root = holder.transform, Cap = cap, Label = label, Body = capBody.GetComponent<MeshRenderer>(), Target = target, Rest = cap.localPosition };
        keys[target] = key;
        return key;
    }

    private void BuildRail()
    {
        PanelCard("Records Panel", RailX, ColumnWidth, Level3DeskProps.Cream);
        Band("Records", RailX, ColumnWidth, "BEA'S RECORDS");
        string[] tabs = Level3WorksheetRules.SourceTabs;
        for (int i = 0; i < tabs.Length; i++)
        {
            float x = RailX + (i % 2 == 0 ? -0.0325f : 0.0325f);
            float z = i < 2 ? 0.089f : 0.066f;
            Button("Tab " + tabs[i], new Vector3(x, Panel, z), new Vector2(0.061f, 0.02f),
                Level3WorksheetRules.SourceTabLabel(tabs[i]), Level3DeskProps.NavyLight, Level3DeskProps.Cream, HitKind.Tab, tabs[i], out _);
            tabFrames[tabs[i]] = props.Frame("Selected", Root, new Vector3(x, Panel + 0.0026f, z), 0.063f, 0.022f, 0.0018f, Level3DeskProps.Highlight);
            tabFrames[tabs[i]].SetActive(false);
            tabCenters[tabs[i]] = new Vector3(x, Panel, z);
        }
        sourceHeading = props.Print("Source Heading", Root, new Vector3(RailX, PrintY, 0.047f), ColumnWidth - 0.012f, 0.008f, "Tap a record above.", Level3DeskProps.InkMuted);
        for (int i = 0; i < 3; i++)
        {
            float z = 0.029f - i * 0.022f;
            var row = new RowButton
            {
                Text = props.Print("Row " + (i + 1), Root, new Vector3(RailX - 0.019f, PrintY, z), 0.084f, 0.017f, "", Level3DeskProps.Ink,
                    TextAlignmentOptions.Left, wrap: true)
            };
            row.Body = Button("Add " + (i + 1), new Vector3(RailX + 0.044f, Panel, z), new Vector2(0.038f, 0.017f), "+ ADD",
                Level3DeskProps.Brass, Level3DeskProps.Ink, HitKind.Add, null, out row.Button);
            foreach (KeyValuePair<Collider, Hit> pair in targets)
                if (pair.Key.transform.IsChildOf(row.Body.transform)) row.Target = (BoxCollider)pair.Key;
            row.Body.SetActive(false);
            rowButtons.Add(row);
        }
        Button("Read Original", new Vector3(RailX, Panel, -0.038f), new Vector2(ColumnWidth - 0.008f, 0.017f), "READ ORIGINAL",
            Level3DeskProps.NavyLight, Level3DeskProps.Cream, HitKind.ReadOriginal, null, out _);
        props.Print("Lifted Heading", Root, new Vector3(RailX, PrintY, -0.054f), ColumnWidth - 0.012f, 0.0075f, "LIFTED, NOT PLACED", Level3DeskProps.InkMuted, bold: true);
    }

    private void BuildSheet()
    {
        PanelCard("Sheet", 0f, SheetWidth, Level3DeskProps.Paper);
        Band("Sheet", 0f, SheetWidth, "WORTH THE TAX? · BEA 2026");
        pageLeft = Button("Page Records", new Vector3(-SheetWidth * 0.5f + 0.018f, Panel + 0.0008f, 0.113f), new Vector2(0.03f, 0.016f), "<",
            Level3DeskProps.Brass, Level3DeskProps.Ink, HitKind.Page, "left", out _);
        pageRight = Button("Page Notes", new Vector3(SheetWidth * 0.5f - 0.018f, Panel + 0.0008f, 0.113f), new Vector2(0.03f, 0.016f), ">",
            Level3DeskProps.Brass, Level3DeskProps.Ink, HitKind.Page, "right", out _);
        SetPaged(false);

        string[] sections = Level3WorksheetRules.Sections;
        for (int i = 0; i < sections.Length; i++)
        {
            var tray = new TrayView
            {
                Section = sections[i],
                Center = new Vector3(i % 2 == 0 ? -0.0525f : 0.0525f, Panel, i < 2 ? 0.044f : -0.068f),
                Size = new Vector2(0.1f, 0.108f)
            };
            props.Box("Tray " + tray.Section, Root, tray.Center + new Vector3(0f, 0.0002f, 0f), new Vector3(tray.Size.x, 0.0004f, tray.Size.y), TrayTint);
            props.Frame("Tray Edge", Root, tray.Center + new Vector3(0f, 0.0004f, 0f), tray.Size.x, tray.Size.y, 0.0012f, Level3DeskProps.BrassSoft);
            props.Print("Tray Label", Root, tray.Center + new Vector3(0f, 0.0007f, tray.Size.y * 0.5f - 0.007f), tray.Size.x - 0.008f, 0.0085f,
                Level3WorksheetRules.SectionLabel(tray.Section).ToUpperInvariant(), Level3DeskProps.Ink, bold: true);
            // T06: the tray being added up on the calculator wears a broad brass frame.
            tray.Adding = props.Frame("Adding Up", Root, tray.Center + new Vector3(0f, 0.0009f, 0f), tray.Size.x + 0.003f, tray.Size.y + 0.003f, 0.0028f, Level3DeskProps.Brass);
            tray.Adding.SetActive(false);
            Add(props.Target("Tray Target", Root, tray.Center + new Vector3(0f, 0.001f, 0f), new Vector3(tray.Size.x, 0.003f, tray.Size.y)), HitKind.Tray, tray.Section);
            if (HasSubtotal(tray.Section)) BuildSlot(tray);
            trays[tray.Section] = tray;
        }
    }

    /// <summary>
    /// The SUBTOTAL slot at the foot of a money tray (T06): an outlined pocket
    /// that takes one clipped calculator tape. Its target sits above the tray's,
    /// so a tap there always means the subtotal.
    /// </summary>
    private void BuildSlot(TrayView tray)
    {
        tray.HasSlot = true;
        tray.SlotSize = new Vector2(tray.Size.x - 0.008f, 0.019f);
        tray.SlotCenter = new Vector3(tray.Center.x, Panel, tray.Center.z - tray.Size.y * 0.5f + 0.0125f);
        props.Box("Slot Pocket", Root, tray.SlotCenter + new Vector3(0f, 0.0005f, 0f), new Vector3(tray.SlotSize.x, 0.0003f, tray.SlotSize.y), Level3DeskProps.Paper);
        props.Frame("Slot Edge", Root, tray.SlotCenter + new Vector3(0f, 0.0007f, 0f), tray.SlotSize.x, tray.SlotSize.y, 0.0009f, Level3DeskProps.InkMuted);
        props.Print("Slot Label", Root, tray.SlotCenter + new Vector3(-tray.SlotSize.x * 0.5f + 0.013f, 0.0009f, 0f), 0.022f, 0.0065f, "SUBTOTAL",
            Level3DeskProps.InkMuted, bold: true);
        tray.SlotState = props.Print("Slot State", Root, tray.SlotCenter + new Vector3(0.012f, 0.0009f, 0f), tray.SlotSize.x - 0.032f, 0.0075f, "",
            Level3DeskProps.InkMuted, TextAlignmentOptions.Right);
        tray.SlotArmed = props.Frame("Slot Armed", Root, tray.SlotCenter + new Vector3(0f, 0.0016f, 0f), tray.SlotSize.x + 0.003f, tray.SlotSize.y + 0.003f, 0.0022f, Level3DeskProps.Brass);
        tray.SlotArmed.SetActive(false);

        // The clipped tape (hidden until one is attached): paper with thickness, a backing shadow and a brass clip.
        var tape = new GameObject("Subtotal Tape").transform;
        tape.gameObject.layer = Root.gameObject.layer;
        tape.SetParent(Root, false);
        tape.localPosition = tray.SlotCenter + new Vector3(0.011f, 0.0012f, 0f);
        float width = tray.SlotSize.x - 0.026f;
        props.Box("Shadow", tape, new Vector3(0.0012f, -0.0006f, -0.0012f), new Vector3(width, 0.0004f, 0.0145f), Level3DeskProps.InkMuted);
        props.Box("Paper", tape, Vector3.zero, new Vector3(width, 0.0011f, 0.0145f), Level3DeskProps.Paper);
        props.Box("Curl", tape, new Vector3(width * 0.5f - 0.0015f, 0.0007f, 0f), new Vector3(0.003f, 0.0014f, 0.0145f), Level3DeskProps.Cream);
        props.Box("Clip", tape, new Vector3(-width * 0.5f + 0.004f, 0.0016f, 0f), new Vector3(0.007f, 0.0028f, 0.017f), Level3DeskProps.Brass);
        tray.TapePrint = props.Print("Print", tape, new Vector3(0.003f, 0.0008f, 0f), width - 0.012f, 0.0095f, "", Level3DeskProps.Ink, TextAlignmentOptions.Right);
        tray.Tape = tape;

        // "Entries changed": a graphite pencil tag laid across the tape, never a red failure mark.
        var stale = new GameObject("Stale Tag").transform;
        stale.gameObject.layer = Root.gameObject.layer;
        stale.SetParent(tape, false);
        stale.localPosition = new Vector3(-0.004f, 0.0022f, 0f);
        stale.localRotation = Quaternion.Euler(0f, -5f, 0f);
        props.Box("Tag", stale, Vector3.zero, new Vector3(width * 0.82f, 0.0006f, 0.0088f), Graphite);
        props.Print("Print", stale, new Vector3(0f, 0.0005f, 0f), width * 0.78f, 0.0062f, "CHANGED · RECALCULATE", Level3DeskProps.Cream, bold: true);
        tray.Stale = stale;
        stale.gameObject.SetActive(false);
        tape.gameObject.SetActive(false);

        AddMark("subtotal:" + tray.Section, Root, tape.localPosition + new Vector3(width * 0.5f - 0.002f, 0.0026f, 0.0068f));
        Add(props.Target("Subtotal Target", Root, tray.SlotCenter + new Vector3(0f, 0.004f, 0f), new Vector3(tray.Size.x, 0.006f, tray.SlotSize.y + 0.004f)), HitKind.Subtotal, tray.Section);
    }

    private static bool HasSubtotal(string section) => section == Level3WorksheetRules.Income || section == Level3WorksheetRules.Expenses;

    private void BuildNotes(Level3WorksheetRow partyClaim, Level3WorksheetRow printClaim)
    {
        PanelCard("Notes Panel", NotesX, ColumnWidth, Level3DeskProps.Cream);
        Band("Notes", NotesX, ColumnWidth, "CLAIM NOTES");
        Note(Level3WorksheetRules.PartyNote, partyClaim, 0.098f, 0.054f);
        Note(Level3WorksheetRules.PrintingNote, printClaim, 0.038f, 0.078f);
        // UNDO | HINT | CHECK on one row (COLLECT WORKSHEET takes the whole row once the sheet checks out).
        undoKey = Key("Undo", new Vector3(NotesX - 0.0435f, Panel, KeyRowZ), new Vector2(0.036f, KeyDepth), "UNDO", Level3DeskProps.Brass, Level3DeskProps.Ink, HitKind.Undo);
        hintKey = Key("Hint", new Vector3(NotesX - 0.0045f, Panel, KeyRowZ), new Vector2(0.036f, KeyDepth), "HINT", KeyOff, Level3DeskProps.InkMuted, HitKind.Hint);
        checkKey = Key("Check", new Vector3(NotesX + 0.0395f, Panel, KeyRowZ), new Vector2(0.044f, KeyDepth), "CHECK", Level3DeskProps.Navy, Level3DeskProps.Cream, HitKind.Check);
        props.Frame("Check Rim", checkKey.Cap, new Vector3(0f, 0.0031f, 0f), 0.044f, KeyDepth, 0.0012f, Level3DeskProps.Brass);
        collectKey = Key("Collect", new Vector3(NotesX, Panel, KeyRowZ), new Vector2(ColumnWidth - 0.006f, KeyDepth), "COLLECT WORKSHEET", Level3DeskProps.Brass, Level3DeskProps.Ink, HitKind.Collect);
        props.Frame("Collect Rim", collectKey.Cap, new Vector3(0f, 0.0031f, 0f), ColumnWidth - 0.006f, KeyDepth, 0.0012f, Level3DeskProps.Navy);
        collectKey.Root.gameObject.SetActive(false);
        message = props.Print("Message", Root, new Vector3(NotesX, PrintY, -0.092f), ColumnWidth - 0.012f, 0.052f, "", Level3DeskProps.Ink,
            TextAlignmentOptions.TopLeft, wrap: true);
        message.fontSizeMax = 0.0085f;
    }

    private void Note(string noteId, Level3WorksheetRow claim, float top, float depth)
    {
        float center = top - depth * 0.5f;
        props.Box("Note " + noteId, Root, new Vector3(NotesX, Panel + 0.0003f, center), new Vector3(ColumnWidth - 0.008f, 0.0006f, depth), NotePaper);
        string title = claim != null ? claim.Label : Level3WorksheetRules.NoteStrip(noteId);
        string amount = claim != null ? "Bea's draft claims " + Money(claim.Amount) : "";
        props.Print("Claim", Root, new Vector3(NotesX, Panel + 0.0008f, top - 0.008f), ColumnWidth - 0.018f, 0.009f, title, Level3DeskProps.Ink, bold: true);
        props.Print("Draft", Root, new Vector3(NotesX, Panel + 0.0008f, top - 0.019f), ColumnWidth - 0.018f, 0.0072f, amount, Level3DeskProps.InkMuted);
        noteFields[noteId + "/reason"] = Field(noteId, HitKind.Reason, new Vector3(NotesX, Panel + 0.0006f, top - 0.036f));
        if (Level3WorksheetRules.NoteHasRelated(noteId))
            noteFields[noteId + "/related"] = Field(noteId, HitKind.Related, new Vector3(NotesX, Panel + 0.0006f, top - 0.059f));
    }

    private TextMeshPro Field(string noteId, HitKind kind, Vector3 center)
    {
        var size = new Vector2(ColumnWidth - 0.016f, 0.019f);
        props.Box(kind + " Field", Root, center + new Vector3(0f, 0.0003f, 0f), new Vector3(size.x, 0.0006f, size.y), Level3DeskProps.Paper);
        props.Frame("Field Edge", Root, center + new Vector3(0f, 0.0006f, 0f), size.x, size.y, 0.001f, Level3DeskProps.InkMuted);
        // The value leaves room at the right end for the check mark (T06).
        TextMeshPro text = props.Print("Value", Root, center + new Vector3(-0.006f, 0.0009f, 0f), size.x - 0.018f, 0.0145f, "", Level3DeskProps.Ink,
            TextAlignmentOptions.Left, wrap: true);
        Add(props.Target(kind + " Target", Root, center + new Vector3(0f, 0.002f, 0f), new Vector3(size.x, 0.005f, size.y)), kind, noteId);
        string place = noteId + (kind == HitKind.Related ? "/related" : "/reason");
        fieldCenters[place] = center;
        AddMark(place, Root, center + new Vector3(size.x * 0.5f - 0.0068f, 0.0016f, 0f));
        return text;
    }

    /// <summary>The chip picker: a navy card raised over the sheet (reasons or related rows).</summary>
    private void BuildPicker()
    {
        var go = new GameObject("Picker");
        go.layer = Root.gameObject.layer;
        go.transform.SetParent(Root, false);
        go.transform.localPosition = new Vector3(0f, Panel + 0.012f, -0.004f);
        picker = go.transform;
        props.Box("Shadow", picker, new Vector3(0.003f, -0.0035f, -0.003f), new Vector3(0.2f, 0.0006f, 0.236f), new Color32(20, 26, 34, 255));
        props.Box("Card", picker, Vector3.zero, new Vector3(0.2f, 0.002f, 0.236f), Level3DeskProps.Navy);
        props.Frame("Edge", picker, new Vector3(0f, 0.0012f, 0f), 0.2f, 0.236f, 0.0016f, Level3DeskProps.Brass);
        pickerTitle = props.Print("Title", picker, new Vector3(0f, 0.0013f, 0.102f), 0.184f, 0.012f, "", Level3DeskProps.Cream, bold: true, wrap: true);
        Add(props.Target("Backdrop", picker, new Vector3(0f, 0f, 0f), new Vector3(0.2f, 0.002f, 0.236f)), HitKind.None, null);
        picker.gameObject.SetActive(false);
    }

    /// <summary>The frozen first-check result, kept at the foot of the records column (T06).</summary>
    private void BuildStub()
    {
        var go = new GameObject("First Check");
        go.layer = Root.gameObject.layer;
        go.transform.SetParent(Root, false);
        go.transform.localPosition = new Vector3(RailX, Panel + 0.0004f, -0.106f);
        stub = go.transform;
        props.Box("Card", stub, Vector3.zero, new Vector3(ColumnWidth - 0.008f, 0.0008f, 0.027f), Level3DeskProps.Navy);
        props.Frame("Rim", stub, new Vector3(0f, 0.0005f, 0f), ColumnWidth - 0.008f, 0.027f, 0.001f, Level3DeskProps.Brass);
        props.Print("Title", stub, new Vector3(0.008f, 0.0007f, 0.0072f), 0.09f, 0.0062f, "FIRST CHECK · GRADE KEPT", Level3DeskProps.BrassSoft, bold: true);
        stubCount = props.Print("Count", stub, new Vector3(-0.012f, 0.0007f, -0.0035f), 0.05f, 0.012f, "", Level3DeskProps.Cream, TextAlignmentOptions.Left, bold: true);
        stubTier = props.Print("Tier", stub, new Vector3(0.03f, 0.0007f, -0.0035f), 0.045f, 0.0095f, "", Level3DeskProps.Cream, TextAlignmentOptions.Right, bold: true);
        stubMedal = Medal("Medal", stub, new Vector3(-ColumnWidth * 0.5f + 0.016f, 0.0008f, 0f), 0.02f);
        stub.gameObject.SetActive(false);
    }

    /// <summary>The first-check receipt raised over the sheet while the grade is revealed (T06).</summary>
    private void BuildReceipt()
    {
        var go = new GameObject("First Check Receipt");
        go.layer = Root.gameObject.layer;
        go.transform.SetParent(Root, false);
        go.transform.localPosition = new Vector3(0f, Panel + 0.016f, -0.004f);
        receipt = go.transform;
        props.Box("Shadow", receipt, new Vector3(0.003f, -0.0035f, -0.003f), new Vector3(0.17f, 0.0006f, 0.2f), new Color32(20, 26, 34, 255));
        props.Box("Card", receipt, Vector3.zero, new Vector3(0.17f, 0.002f, 0.2f), Level3DeskProps.Paper);
        props.Box("Header", receipt, new Vector3(0f, 0.0012f, 0.085f), new Vector3(0.17f, 0.0004f, 0.03f), Level3DeskProps.Navy);
        props.Print("Title", receipt, new Vector3(0f, 0.0016f, 0.085f), 0.15f, 0.014f, "FIRST CHECK", Level3DeskProps.Cream, bold: true);
        receiptCount = props.Print("Count", receipt, new Vector3(0f, 0.0013f, 0.045f), 0.15f, 0.04f, "0/12", Level3DeskProps.Ink, bold: true);
        receiptLines = props.Print("Lines", receipt, new Vector3(0f, 0.0013f, -0.002f), 0.14f, 0.04f, "", Level3DeskProps.InkMuted, TextAlignmentOptions.Center, wrap: true);
        receiptMedal = Medal("Medal", receipt, new Vector3(-0.045f, 0.0014f, -0.05f), 0.036f);
        receiptTier = props.Print("Tier", receipt, new Vector3(0.018f, 0.0013f, -0.05f), 0.09f, 0.02f, "", Level3DeskProps.Ink, TextAlignmentOptions.Left, bold: true);
        receiptFooter = props.Print("Footer", receipt, new Vector3(0f, 0.0013f, -0.085f), 0.15f, 0.012f, "", Level3DeskProps.InkMuted, TextAlignmentOptions.Center, wrap: true);
        Add(props.Target("Receipt Target", receipt, Vector3.zero, new Vector3(0.17f, 0.006f, 0.2f)), HitKind.Receipt, null);
        receipt.gameObject.SetActive(false);
    }

    /// <summary>The CHECKED stamp impression on the sheet (terracotta ink, a rubber-stamp frame), shown once it checks out.</summary>
    private void BuildStamp()
    {
        var go = new GameObject("Checked Stamp");
        go.layer = Root.gameObject.layer;
        go.transform.SetParent(Root, false);
        go.transform.localPosition = new Vector3(0.006f, PrintY + 0.0034f, -0.012f);
        go.transform.localRotation = Quaternion.Euler(0f, -9f, 0f);
        stamp = go.transform;
        props.Frame("Rim", stamp, Vector3.zero, 0.11f, 0.03f, 0.0018f, Level3DeskProps.Terracotta);
        props.Frame("Inner Rim", stamp, Vector3.zero, 0.102f, 0.023f, 0.0007f, Level3DeskProps.Terracotta);
        props.Print("Ink", stamp, new Vector3(0f, 0.0004f, 0f), 0.096f, 0.017f, "CHECKED", Level3DeskProps.Terracotta, bold: true);
        stamp.gameObject.SetActive(false);
    }

    /// <summary>A short pencil (wood body, graphite tip, terracotta eraser) that ticks each place during a check pass.</summary>
    private void BuildPencil()
    {
        var go = new GameObject("Check Pencil");
        go.layer = Root.gameObject.layer;
        go.transform.SetParent(Root, false);
        pencil = go.transform;
        var body = new GameObject("Body").transform;
        body.gameObject.layer = Root.gameObject.layer;
        body.SetParent(pencil, false);
        // Tilted toward the player's right, tip at the pencil's origin.
        body.localRotation = Quaternion.Euler(-28f, 0f, -32f);
        props.Shape(PrimitiveType.Cylinder, "Tip", body, new Vector3(0f, 0.004f, 0f), new Vector3(0.0028f, 0.004f, 0.0028f), Graphite);
        props.Shape(PrimitiveType.Cylinder, "Cone", body, new Vector3(0f, 0.011f, 0f), new Vector3(0.0052f, 0.0035f, 0.0052f), new Color32(226, 204, 160, 255));
        props.Shape(PrimitiveType.Cylinder, "Wood", body, new Vector3(0f, 0.04f, 0f), new Vector3(0.0058f, 0.026f, 0.0058f), PencilWood);
        props.Shape(PrimitiveType.Cylinder, "Ferrule", body, new Vector3(0f, 0.068f, 0f), new Vector3(0.006f, 0.0025f, 0.006f), Level3DeskProps.Brass);
        props.Shape(PrimitiveType.Cylinder, "Eraser", body, new Vector3(0f, 0.0735f, 0f), new Vector3(0.0056f, 0.003f, 0.0056f), Level3DeskProps.Terracotta);
        pencil.gameObject.SetActive(false);
    }

    private SpriteRenderer Medal(string name, Transform parent, Vector3 center, float size)
    {
        var go = new GameObject(name);
        go.layer = Root.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        go.transform.localScale = Vector3.one * size;
        renderer.enabled = false;
        return renderer;
    }

    /// <summary>A check mark holder at a place: a brass disc with a drawn tick, or a graphite disc with "?".</summary>
    private void AddMark(string place, Transform parent, Vector3 center)
    {
        var holder = new GameObject("Mark " + place);
        holder.layer = Root.gameObject.layer;
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = center;
        var tick = new GameObject("Confirmed");
        tick.layer = Root.gameObject.layer;
        tick.transform.SetParent(holder.transform, false);
        props.Shape(PrimitiveType.Cylinder, "Disc", tick.transform, Vector3.zero, new Vector3(MarkSize, 0.0003f, MarkSize), Level3DeskProps.Brass);
        Stroke("Tick A", tick.transform, new Vector2(-0.34f, 0.02f) * MarkSize, new Vector2(-0.1f, -0.24f) * MarkSize, MarkSize * 0.16f, 0.0005f, Level3DeskProps.Navy);
        Stroke("Tick B", tick.transform, new Vector2(-0.1f, -0.24f) * MarkSize, new Vector2(0.34f, 0.28f) * MarkSize, MarkSize * 0.16f, 0.0005f, Level3DeskProps.Navy);
        var look = new GameObject("Look Again");
        look.layer = Root.gameObject.layer;
        look.transform.SetParent(holder.transform, false);
        props.Shape(PrimitiveType.Cylinder, "Disc", look.transform, Vector3.zero, new Vector3(MarkSize, 0.0003f, MarkSize), Graphite);
        props.Print("Ask", look.transform, new Vector3(0f, 0.0005f, 0f), MarkSize * 0.8f, MarkSize * 0.8f, "?", Level3DeskProps.Cream, bold: true);
        tick.SetActive(false);
        look.SetActive(false);
        marks[place] = new MarkView { Root = holder.transform, Tick = tick, Look = look, Shown = Level3WorksheetCheck.Mark.None };
    }

    /// <summary>A drawn stroke (a thin box from a to b on the local XZ plane).</summary>
    private void Stroke(string name, Transform parent, Vector2 a, Vector2 b, float width, float y, Color color)
    {
        Vector2 d = b - a;
        GameObject part = props.Box(name, parent, new Vector3((a.x + b.x) * 0.5f, y, (a.y + b.y) * 0.5f), new Vector3(width, 0.0003f, d.magnitude), color);
        part.transform.localRotation = Quaternion.Euler(0f, Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, 0f);
    }

    private void Add(Collider collider, HitKind kind, string id) => targets[collider] = new Hit { Kind = kind, Id = id };

    // =========================================================
    // QUERIES
    // =========================================================

    public bool TryHit(Collider collider, out Hit hit)
    {
        hit = default;
        if (collider == null || !collider.transform.IsChildOf(Root) || !collider.gameObject.activeInHierarchy) return false;
        return targets.TryGetValue(collider, out hit);
    }

    public bool PickerOpen => picker != null && picker.gameObject.activeSelf;
    public bool ReceiptOpen => receipt != null && receipt.gameObject.activeSelf;

    /// <summary>The section whose tray contains a board-local point (drag drops), or null.</summary>
    public string TrayAt(Vector3 local)
    {
        foreach (TrayView tray in trays.Values)
            if (Mathf.Abs(local.x - tray.Center.x) <= tray.Size.x * 0.5f + 0.004f && Mathf.Abs(local.z - tray.Center.z) <= tray.Size.y * 0.5f + 0.004f)
                return tray.Section;
        return null;
    }

    /// <summary>Board-local x of each page center (narrow screens show records+sheet or sheet+notes).</summary>
    public static float PageCenter(int page) => page == 0 ? -0.07f : page == 2 ? 0.07f : 0f;
    public const float PageWidth = 0.37f;

    /// <summary>Where a section's subtotal tape lands (the attach flight ends here).</summary>
    public Vector3 SubtotalWorldPosition(string section) =>
        trays.TryGetValue(section, out TrayView tray) && tray.HasSlot ? Root.TransformPoint(tray.Tape.localPosition) : Root.position;

    public Quaternion SubtotalWorldRotation => Root.rotation;

    /// <summary>The board-local point a check place is marked at (strips move with their strip).</summary>
    public Vector3 MarkLocal(string place) =>
        marks.TryGetValue(place, out MarkView mark) ? Root.InverseTransformPoint(mark.Root.position) : Vector3.zero;

    // =========================================================
    // STATE (quiet; positions glide in Tick)
    // =========================================================

    public void SetPaged(bool paged)
    {
        if (pageLeft != null) pageLeft.SetActive(paged);
        if (pageRight != null) pageRight.SetActive(paged);
    }

    public void ShowSource(string sourceId, Level3WorksheetDraft draft)
    {
        foreach (KeyValuePair<string, GameObject> frame in tabFrames) frame.Value.SetActive(frame.Key == sourceId);
        int index = 0;
        if (!string.IsNullOrEmpty(sourceId))
        {
            sourceHeading.text = sourceId + " · " + Level3WorksheetRules.SourceTabLabel(sourceId);
            foreach (string rowId in Level3WorksheetRules.RowIds)
            {
                if (!rows.TryGetValue(rowId, out Level3WorksheetRow row) || row.SourceId != sourceId || index >= rowButtons.Count) continue;
                RowButton button = rowButtons[index++];
                bool lifted = draft?.Strip(rowId) != null;
                button.RowId = rowId;
                button.Text.text = row.Label + "\n" + Money(row.Amount);
                button.Button.text = lifted ? "ON SHEET" : "+ ADD";
                button.Body.SetActive(true);
                targets[button.Target] = new Hit { Kind = HitKind.Add, Id = rowId };
            }
        }
        else sourceHeading.text = "Tap a record above.";
        for (; index < rowButtons.Count; index++)
        {
            rowButtons[index].Text.text = "";
            rowButtons[index].RowId = null;
            rowButtons[index].Body.SetActive(false);
        }
    }

    /// <summary>Strips go where the draft puts them; notes and subtotal lines show the saved values.</summary>
    public void Sync(Level3WorksheetDraft draft, bool snap)
    {
        var present = new HashSet<string>();
        if (draft?.strips != null)
            foreach (Level3StripRecord record in draft.strips)
                if (record != null && rows.ContainsKey(record.rowId)) present.Add(record.rowId);
        foreach (string rowId in new List<string>(strips.Keys))
            if (!present.Contains(rowId)) RemoveStrip(rowId);
        foreach (string rowId in present)
            if (!strips.ContainsKey(rowId)) CreateStrip(rowId);

        // The lifted pile (fanned tighter when it grows), then each tray in one column (tighter as it fills).
        List<Level3StripRecord> pile = Level3WorksheetRules.InSection(draft, "");
        float pileBottom = stub != null && stub.gameObject.activeSelf ? StackBottomWithStub : StackBottom;
        Place(pile, new Vector3(RailX, Panel, StackTop), StackTop - pileBottom);
        foreach (TrayView tray in trays.Values)
        {
            float top = tray.Center.z + tray.Size.y * 0.5f - 0.016f;
            // Money trays keep their foot for the SUBTOTAL slot.
            float room = tray.Size.y - (tray.HasSlot ? 0.036f : 0.02f);
            Place(Level3WorksheetRules.InSection(draft, tray.Section), new Vector3(tray.Center.x, Panel, top), room);
        }
        foreach (string noteId in Level3WorksheetRules.Notes)
        {
            Level3NoteRecord note = draft?.Note(noteId);
            if (noteFields.TryGetValue(noteId + "/reason", out TextMeshPro reason))
                reason.text = note != null && !string.IsNullOrEmpty(note.reason) ? "Reason: " + Level3WorksheetRules.ReasonLabel(note.reason) : "Reason: tap to choose";
            if (noteFields.TryGetValue(noteId + "/related", out TextMeshPro related))
                related.text = note != null && !string.IsNullOrEmpty(note.related) ? "Related: " + StripLabel(note.related) : "Related entry: tap to choose";
        }
        if (snap)
            foreach (StripView strip in strips.Values)
                if (!strip.Dragging) strip.Root.localPosition = strip.Goal;
        SetSelectedStrip(selected);
        SetAdding(adding);
    }

    /// <summary>
    /// T06: each money tray's SUBTOTAL slot from the saved tapes: empty
    /// ("tap: calculator"), a clipped tape with its result, or that tape with
    /// the "CHANGED · RECALCULATE" pencil tag. hideSection keeps a slot empty
    /// while its tape is still flying in. animate pops a newly stale tag.
    /// </summary>
    public void SyncSubtotals(Level3ChapterCheckpoint state, string hideSection, bool animate)
    {
        foreach (TrayView tray in trays.Values)
        {
            if (!tray.HasSlot) continue;
            Level3TapeRecord tape = Level3WorksheetCheck.SubtotalTape(state, tray.Section);
            bool shown = tape != null && tray.Section != hideSection;
            bool stale = shown && Level3WorksheetCheck.IsStale(state, tray.Section);
            if (tray.Tape.gameObject.activeSelf != shown) tray.Tape.gameObject.SetActive(shown);
            bool empty = Level3WorksheetRules.InSection(state?.worksheet, tray.Section).Count == 0;
            tray.SlotState.text = shown ? "" : empty ? "no strips yet" : "tap: calculator";
            if (shown)
            {
                List<string> lines = Level3Calculator.TapeLines(tape.expression, null);
                string working = lines.Count > 1 ? lines.Count + " LINES  " : "";
                tray.TapePrint.text = working + "<b>= " + Money(tape.result) + "</b>";
                tray.TapePrint.color = stale ? StaleInk : Level3DeskProps.Ink;
            }
            string signature = shown ? Level3WorksheetCheck.Signature(tape) : null;
            bool newlyStale = stale && !tray.ShownStale && tray.ShownTape == signature;
            tray.Stale.gameObject.SetActive(stale);
            if (newlyStale && animate) StartPop(tray.Stale);
            tray.ShownTape = signature;
            tray.ShownStale = stale;
        }
    }

    /// <summary>A tape just landed in its slot: it settles in with a small pop.</summary>
    public void PopSubtotal(string section)
    {
        if (section != null && trays.TryGetValue(section, out TrayView tray) && tray.HasSlot && tray.Tape.gameObject.activeSelf) StartPop(tray.Tape);
    }

    /// <summary>A mark is pointed out again (a hint about it): it pops.</summary>
    public void PopMark(string decisionId)
    {
        string place = Level3WorksheetCheck.PlaceOf(decisionId);
        if (place != null && marks.TryGetValue(place, out MarkView mark) && mark.Shown != Level3WorksheetCheck.Mark.None) mark.PopStart = Time.unscaledTime;
    }

    private readonly List<(Transform target, float start)> pops = new List<(Transform, float)>();

    private void StartPop(Transform target)
    {
        if (target == null) return;
        pops.RemoveAll(p => p.target == target);
        pops.Add((target, Time.unscaledTime));
        target.localScale = Vector3.one * 0.01f;
    }

    private void Place(List<Level3StripRecord> group, Vector3 top, float room)
    {
        float spacing = group.Count <= 1 ? StripDepth + 0.0015f : Mathf.Min(StripDepth + 0.0015f, (room - StripDepth) / (group.Count - 1));
        for (int i = 0; i < group.Count; i++)
        {
            if (!strips.TryGetValue(group[i].rowId, out StripView strip)) continue;
            // Each later strip lies a hair higher, so an overlapped pile still shows (and takes taps on) the top one.
            strip.Goal = new Vector3(top.x, top.y + 0.0012f + i * 0.0006f, top.z - StripDepth * 0.5f - i * spacing);
        }
    }

    private void CreateStrip(string rowId)
    {
        Level3WorksheetRow row = rows[rowId];
        var go = new GameObject("Strip " + rowId);
        go.layer = Root.gameObject.layer;
        go.transform.SetParent(Root, false);
        // New strips rise out of the record panel (its row), then glide to the pile.
        go.transform.localPosition = new Vector3(RailX, Panel + 0.006f, 0.01f);
        props.Box("Paper", go.transform, new Vector3(0f, 0.0006f, 0f), new Vector3(StripWidth, 0.0012f, StripDepth), Level3DeskProps.Paper);
        props.Box("Edge", go.transform, new Vector3(0f, 0.0002f, -0.0006f), new Vector3(StripWidth + 0.001f, 0.0004f, StripDepth + 0.0008f), Level3DeskProps.InkMuted);
        props.Box("Stripe", go.transform, new Vector3(-StripWidth * 0.5f + 0.0018f, 0.0007f, 0f), new Vector3(0.0028f, 0.0013f, StripDepth), Level3DeskProps.Brass);
        props.Print("Label", go.transform, new Vector3(-0.009f, 0.0014f, 0f), 0.068f, StripDepth * 0.8f, row.Label + "  " + Money(row.Amount),
            Level3DeskProps.Ink, TextAlignmentOptions.Left);
        props.Print("Source", go.transform, new Vector3(0.0345f, 0.0014f, 0f), 0.02f, StripDepth * 0.6f, row.SourceId, Level3DeskProps.InkMuted,
            TextAlignmentOptions.Right);
        GameObject frame = props.Frame("Selected", go.transform, new Vector3(0f, 0.0016f, 0f), StripWidth + 0.002f, StripDepth + 0.002f, 0.0012f, Level3DeskProps.Highlight);
        frame.SetActive(false);
        // T06: while its tray is being added up, a strip wears a brass edge: tap it to put its amount on the calculator.
        GameObject source = props.Frame("Adding", go.transform, new Vector3(0f, 0.0015f, 0f), StripWidth + 0.0015f, StripDepth + 0.0015f, 0.0009f, Level3DeskProps.Brass);
        source.SetActive(false);
        BoxCollider target = props.Target("Target", go.transform, new Vector3(0f, 0.0012f, 0f), new Vector3(StripWidth, 0.004f, StripDepth));
        Add(target, HitKind.Strip, rowId);
        AddMark("strip:" + rowId, go.transform, new Vector3(StripWidth * 0.5f - 0.0035f, 0.0024f, StripDepth * 0.5f - 0.0012f));
        strips[rowId] = new StripView { RowId = rowId, Root = go.transform, Target = target, Frame = frame, SourceFrame = source, Goal = go.transform.localPosition };
    }

    private void RemoveStrip(string rowId)
    {
        if (!strips.TryGetValue(rowId, out StripView strip)) return;
        targets.Remove(strip.Target);
        marks.Remove("strip:" + rowId);
        needed.Remove("strip:" + rowId);
        Object.Destroy(strip.Root.gameObject);
        strips.Remove(rowId);
        if (selected == rowId) selected = null;
    }

    public void SetSelectedStrip(string rowId)
    {
        selected = rowId != null && strips.ContainsKey(rowId) ? rowId : null;
        foreach (StripView strip in strips.Values) strip.Frame.SetActive(strip.RowId == selected);
    }

    public string SelectedStrip => selected;

    public void SetMessage(string text)
    {
        if (message != null) message.text = text ?? "";
    }

    /// <summary>A strip held under the finger (board-local point, lifted above the sheet).</summary>
    public void DragStrip(string rowId, Vector3 local)
    {
        if (!strips.TryGetValue(rowId, out StripView strip)) return;
        strip.Dragging = true;
        strip.Root.localPosition = new Vector3(local.x, Panel + 0.01f, local.z);
    }

    /// <summary>Lets go of a dragged strip: it glides to wherever the draft puts it (back, if nothing changed).</summary>
    public void EndDrag()
    {
        foreach (StripView strip in strips.Values) strip.Dragging = false;
    }

    public bool HasStrip(string rowId) => rowId != null && strips.ContainsKey(rowId);

    // =========================================================
    // T06: ADDING UP, MARKS, NEEDED TAGS, KEYS, RESULT
    // =========================================================

    /// <summary>The tray being added up on the calculator (null: none). Its strips wear a brass edge as calculator sources.</summary>
    public void SetAdding(string section)
    {
        adding = section;
        foreach (TrayView tray in trays.Values) if (tray.Adding != null) tray.Adding.SetActive(tray.Section == section);
        foreach (StripView strip in strips.Values) strip.SourceFrame.SetActive(false);
        if (section == null) return;
        foreach (StripView strip in strips.Values)
            if (TrayAt(strip.Goal) == section) strip.SourceFrame.SetActive(true);
    }

    /// <summary>The armed SUBTOTAL slot waiting for its second tap (null clears).</summary>
    public void SetArmedSubtotal(string section)
    {
        foreach (TrayView tray in trays.Values) if (tray.SlotArmed != null) tray.SlotArmed.SetActive(tray.Section == section);
    }

    /// <summary>A strip's amount went onto the calculator: it lifts a little and settles back.</summary>
    public void PulseStrip(string rowId)
    {
        if (strips.TryGetValue(rowId, out StripView strip)) strip.PulseStart = Time.unscaledTime;
    }

    /// <summary>Marks every check place from the last check (quiet unless animate, which pops changed marks in).</summary>
    public void SetMarks(System.Func<string, Level3WorksheetCheck.Mark> markFor, bool animate)
    {
        foreach (string id in Level3WorksheetCheck.DecisionIds)
        {
            string place = Level3WorksheetCheck.PlaceOf(id);
            if (place == null || !marks.TryGetValue(place, out MarkView mark)) continue;
            SetMark(mark, markFor != null ? markFor(id) : Level3WorksheetCheck.Mark.None, animate);
        }
    }

    /// <summary>One place's mark (the check pass reveals them one by one).</summary>
    public void SetMark(string decisionId, Level3WorksheetCheck.Mark value, bool animate)
    {
        string place = Level3WorksheetCheck.PlaceOf(decisionId);
        if (place != null && marks.TryGetValue(place, out MarkView mark)) SetMark(mark, value, animate);
    }

    private void SetMark(MarkView mark, Level3WorksheetCheck.Mark value, bool animate)
    {
        if (mark.Shown == value) return;
        mark.Shown = value;
        mark.Tick.SetActive(value == Level3WorksheetCheck.Mark.Confirmed);
        mark.Look.SetActive(value == Level3WorksheetCheck.Mark.LookAgain);
        mark.PopStart = animate && value != Level3WorksheetCheck.Mark.None ? Time.unscaledTime : -1f;
        mark.Root.localScale = mark.PopStart >= 0f ? Vector3.one * 0.01f : Vector3.one;
    }

    /// <summary>
    /// After an incomplete Check: a pencil ring around each empty place
    /// (unplaced strips, unanswered fields, missing or stale subtotals, rows
    /// still on a record's tab) for a few seconds or until the next change.
    /// </summary>
    public void ShowNeeded(IEnumerable<string> places, float seconds)
    {
        ClearNeeded();
        foreach (string place in places)
        {
            if (place == null) continue;
            string key = place.StartsWith("lift:") ? "tab:" + Row(place.Substring(5))?.SourceId
                : place.StartsWith("place:") || place.StartsWith("strip:") ? "strip:" + place.Substring(place.IndexOf(':') + 1) : place;
            GameObject ring = NeededRing(key);
            if (ring == null) continue;
            ring.SetActive(true);
            StartPop(ring.transform);
        }
        neededStart = Time.unscaledTime;
        neededUntil = Time.unscaledTime + seconds;
    }

    public void ClearNeeded()
    {
        foreach (GameObject ring in needed.Values) if (ring != null) ring.SetActive(false);
        neededUntil = -1f;
    }

    private GameObject NeededRing(string key)
    {
        if (needed.TryGetValue(key, out GameObject existing) && existing != null) return existing;
        GameObject ring = null;
        if (key.StartsWith("tab:") && tabCenters.TryGetValue(key.Substring(4), out Vector3 tab))
            ring = props.Frame("Needed " + key, Root, tab + new Vector3(0f, 0.0034f, 0f), 0.066f, 0.025f, 0.0016f, Graphite);
        else if (key.StartsWith("strip:") && strips.TryGetValue(key.Substring(6), out StripView strip))
            ring = props.Frame("Needed", strip.Root, new Vector3(0f, 0.0019f, 0f), StripWidth + 0.004f, StripDepth + 0.004f, 0.0014f, Graphite);
        else if (key.StartsWith("subtotal:") && trays.TryGetValue(key.Substring(9), out TrayView tray) && tray.HasSlot)
            ring = props.Frame("Needed " + key, Root, tray.SlotCenter + new Vector3(0f, 0.0034f, 0f), tray.SlotSize.x + 0.005f, tray.SlotSize.y + 0.005f, 0.0016f, Graphite);
        else if (fieldCenters.TryGetValue(key, out Vector3 field))
            ring = props.Frame("Needed " + key, Root, field + new Vector3(0f, 0.0028f, 0f), ColumnWidth - 0.011f, 0.024f, 0.0016f, Graphite);
        if (ring != null) needed[key] = ring;
        return ring;
    }

    /// <summary>The key row: CHECK's state, HINT's availability; in COLLECT state one wide key replaces the row.</summary>
    public void SetKeys(CheckKey check, bool hintAvailable, bool undoAvailable)
    {
        if (checkKey == null) return;
        checkShown = check;
        hintEnabled = hintAvailable;
        bool collect = check == CheckKey.Collect;
        bool row = !collect && check != CheckKey.Hidden;
        undoKey.Root.gameObject.SetActive(row);
        hintKey.Root.gameObject.SetActive(row);
        checkKey.Root.gameObject.SetActive(row);
        bool wasCollect = collectKey.Root.gameObject.activeSelf;
        collectKey.Root.gameObject.SetActive(collect);
        if (collect && !wasCollect) StartPop(collectKey.Root);
        checkKey.Label.text = check == CheckKey.Confirm ? "CHECK NOW?" : check == CheckKey.Again ? "CHECK AGAIN" : "CHECK";
        Recolor(checkKey, check == CheckKey.Confirm ? Level3DeskProps.Brass : Level3DeskProps.Navy, check == CheckKey.Confirm ? Level3DeskProps.Ink : Level3DeskProps.Cream);
        Recolor(hintKey, hintAvailable ? Level3DeskProps.Brass : KeyOff, hintAvailable ? Level3DeskProps.Ink : Level3DeskProps.InkMuted);
        Recolor(undoKey, undoAvailable ? Level3DeskProps.Brass : KeyOff, undoAvailable ? Level3DeskProps.Ink : Level3DeskProps.InkMuted);
    }

    private void Recolor(KeyView key, Color body, Color ink)
    {
        if (key.Body != null) key.Body.sharedMaterial = props.Lit(body);
        if (key.Label != null) key.Label.color = ink;
    }

    public CheckKey CheckKeyShown => checkShown;
    public bool HintAvailable => hintEnabled;

    /// <summary>A key cap held down while the finger is on it (released by ReleaseKeys or after a short press).</summary>
    public void PressKey(Collider collider, float seconds)
    {
        if (collider != null && keys.TryGetValue(collider, out KeyView key)) key.PressedUntil = Time.unscaledTime + seconds;
    }

    public void ReleaseKeys()
    {
        foreach (KeyView key in keys.Values) key.PressedUntil = 0f;
    }

    /// <summary>The frozen first-check result at the foot of the records column (hidden before the first check).</summary>
    public void ShowStub(Level3WorksheetResult result, Sprite medal)
    {
        bool show = result != null && result.firstChecked;
        if (stub.gameObject.activeSelf != show) stub.gameObject.SetActive(show);
        if (!show) return;
        stubCount.text = result.firstCorrect + "/" + result.firstTotal;
        stubTier.text = Level3WorksheetCheck.TierName(result.firstTier).ToUpperInvariant();
        stubTier.color = TierInk(result.firstTier);
        stubMedal.sprite = medal;
        stubMedal.enabled = medal != null;
        if (medal != null) stubMedal.transform.localScale = Vector3.one * (0.02f / Mathf.Max(0.001f, medal.bounds.size.x));
    }

    public Vector3 StubLocal => stub != null ? stub.localPosition : Vector3.zero;

    private static Color TierInk(int tier) =>
        tier >= 3 ? (Color)new Color32(255, 214, 102, 255) : tier == 2 ? (Color)new Color32(210, 222, 238, 255) : (Color)new Color32(236, 150, 92, 255);

    /// <summary>Raises the first-check receipt over the sheet (count starts at 0; the desk counts it up).</summary>
    public void OpenReceipt(string lines, Sprite medal)
    {
        receipt.gameObject.SetActive(true);
        receipt.localPosition = new Vector3(0f, Panel + 0.016f, -0.004f);
        receipt.localScale = Vector3.one;
        receiptCount.text = "0/" + Level3WorksheetCheck.DecisionCount;
        receiptLines.text = lines ?? "";
        receiptTier.text = "";
        receiptFooter.text = "";
        receiptMedal.sprite = medal;
        receiptMedal.enabled = false;
        if (medal != null) receiptMedal.transform.localScale = Vector3.one * (0.036f / Mathf.Max(0.001f, medal.bounds.size.x));
        StartPop(receipt);
    }

    public void SetReceiptCount(int count) => receiptCount.text = count + "/" + Level3WorksheetCheck.DecisionCount;

    public void RevealReceiptTier(int tier, string footer)
    {
        receiptTier.text = Level3WorksheetCheck.TierName(tier).ToUpperInvariant();
        receiptTier.color = tier >= 3 ? (Color)new Color32(176, 132, 40, 255) : tier == 2 ? (Color)new Color32(96, 110, 128, 255) : (Color)new Color32(160, 92, 54, 255);
        receiptFooter.text = footer ?? "";
        receiptMedal.enabled = receiptMedal.sprite != null;
        if (receiptMedal.enabled) StartPop(receiptMedal.transform);
    }

    /// <summary>Moves the receipt toward the stub while shrinking (t 0..1); hidden at the end.</summary>
    public void FoldReceipt(float t)
    {
        if (receipt == null) return;
        float e = t * t * (3f - 2f * t);
        Vector3 from = new Vector3(0f, Panel + 0.016f, -0.004f), to = stub.localPosition + new Vector3(0f, 0.006f, 0f);
        receipt.localPosition = Vector3.Lerp(from, to, e) + Vector3.up * (0.02f * Mathf.Sin(e * Mathf.PI));
        receipt.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, e);
        if (t >= 1f) receipt.gameObject.SetActive(false);
    }

    public void CloseReceipt()
    {
        if (receipt != null) receipt.gameObject.SetActive(false);
    }

    public bool StampVisible => stamp != null && stamp.gameObject.activeSelf;

    /// <summary>The CHECKED stamp on the sheet (pops in when animate).</summary>
    public void SetStamp(bool visible, bool animate)
    {
        if (stamp == null) return;
        bool was = stamp.gameObject.activeSelf;
        stamp.gameObject.SetActive(visible);
        if (visible && !was && animate) stampPopStart = Time.unscaledTime;
        if (!visible || !animate) stamp.localScale = Vector3.one;
    }

    /// <summary>The check pencil at a board-local point (dip 0..1 lowers its tip onto the paper); null hides it.</summary>
    public void SetPencil(Vector3? local, float dip)
    {
        if (pencil == null) return;
        pencil.gameObject.SetActive(local.HasValue);
        if (!local.HasValue) return;
        pencil.localPosition = local.Value + new Vector3(0f, Panel + 0.002f + 0.012f * (1f - Mathf.Clamp01(dip)), 0f);
    }

    // =========================================================
    // PICKER
    // =========================================================

    /// <summary>Opens the chip card: one chip per (id, label) pair plus Cancel, all styled alike (no suggested choice).</summary>
    public void OpenPicker(string title, IList<KeyValuePair<string, string>> choices, string current)
    {
        ClosePicker();
        pickerTitle.text = title;
        float spacing = Mathf.Min(0.027f, 0.19f / Mathf.Max(1, choices.Count + 1));
        float z = 0.08f;
        for (int i = 0; i < choices.Count; i++, z -= spacing)
            pickerChips.Add(Chip(choices[i].Key, choices[i].Value, z, spacing - 0.004f, choices[i].Key == current, HitKind.Chip));
        pickerChips.Add(Chip(null, "CANCEL", z - 0.004f, spacing - 0.004f, false, HitKind.Cancel));
        picker.gameObject.SetActive(true);
    }

    private GameObject Chip(string id, string text, float z, float depth, bool current, HitKind kind)
    {
        var chip = new GameObject("Chip");
        chip.layer = Root.gameObject.layer;
        chip.transform.SetParent(picker, false);
        chip.transform.localPosition = new Vector3(0f, 0.001f, z);
        Color body = kind == HitKind.Cancel ? Level3DeskProps.NavyLight : Level3DeskProps.Cream;
        Color ink = kind == HitKind.Cancel ? Level3DeskProps.Cream : Level3DeskProps.Ink;
        props.Box("Body", chip.transform, new Vector3(0f, 0.001f, 0f), new Vector3(0.18f, 0.002f, depth), body);
        props.Print("Label", chip.transform, new Vector3(0f, 0.0023f, 0f), 0.168f, depth * 0.66f, text, ink, bold: kind == HitKind.Cancel);
        // The value already chosen keeps a thin frame so the player sees what is set (not whether it is right).
        if (current) props.Frame("Current", chip.transform, new Vector3(0f, 0.0024f, 0f), 0.182f, depth + 0.002f, 0.0012f, Level3DeskProps.Highlight);
        Add(props.Target("Target", chip.transform, new Vector3(0f, 0.002f, 0f), new Vector3(0.18f, 0.005f, depth)), kind, id);
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

    public void Tick(float deltaTime)
    {
        float now = Time.unscaledTime;
        float k = 1f - Mathf.Exp(-GlideRate * deltaTime);
        foreach (StripView strip in strips.Values)
        {
            if (strip.Dragging) continue;
            Vector3 goal = strip.Goal;
            if (strip.PulseStart >= 0f)
            {
                // A short lift-and-settle when its amount goes onto the calculator.
                float t = (now - strip.PulseStart) / 0.26f;
                if (t >= 1f) strip.PulseStart = -1f;
                else goal += Vector3.up * (0.004f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
            }
            strip.Root.localPosition = Vector3.Lerp(strip.Root.localPosition, goal, k);
        }
        foreach (MarkView mark in marks.Values)
        {
            if (mark.PopStart < 0f) continue;
            float t = Mathf.Clamp01((now - mark.PopStart) / PopSeconds);
            mark.Root.localScale = Vector3.one * Mathf.Max(0.01f, EaseOutBack(t));
            if (t >= 1f) mark.PopStart = -1f;
        }
        for (int i = pops.Count - 1; i >= 0; i--)
        {
            (Transform target, float start) = pops[i];
            if (target == null) { pops.RemoveAt(i); continue; }
            float t = Mathf.Clamp01((now - start) / PopSeconds);
            target.localScale = Vector3.one * Mathf.Max(0.01f, EaseOutBack(t));
            if (t >= 1f) pops.RemoveAt(i);
        }
        if (stampPopStart >= 0f && stamp != null)
        {
            // The stamp lands: a little large, then pressed flat.
            float t = Mathf.Clamp01((now - stampPopStart) / 0.24f);
            stamp.localScale = Vector3.one * Mathf.Lerp(1.45f, 1f, 1f - (1f - t) * (1f - t));
            if (t >= 1f) stampPopStart = -1f;
        }
        foreach (KeyView key in keys.Values)
        {
            Vector3 target = key.Rest + new Vector3(0f, now < key.PressedUntil ? -0.0016f : 0f, 0f);
            if (key.Cap.localPosition != target) key.Cap.localPosition = Vector3.Lerp(key.Cap.localPosition, target, 0.6f);
        }
        // The one action that matters now (CHECK NOW?, COLLECT WORKSHEET) breathes gently; nothing flashes.
        KeyView breathing = checkShown == CheckKey.Collect ? collectKey : checkShown == CheckKey.Confirm ? checkKey : null;
        if (breathing != null)
        {
            checkPulse += deltaTime;
            breathing.Cap.localScale = new Vector3(1f, 1f, 1f + 0.035f * Mathf.Sin(checkPulse * 4.2f));
        }
        if (checkKey != null && checkKey != breathing && checkKey.Cap.localScale != Vector3.one) checkKey.Cap.localScale = Vector3.one;
        if (collectKey != null && collectKey != breathing && collectKey.Cap.localScale != Vector3.one) collectKey.Cap.localScale = Vector3.one;
        if (neededUntil >= 0f && now > neededUntil) ClearNeeded();
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }
}
