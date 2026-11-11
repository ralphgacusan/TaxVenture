using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>What a touch on the paper machine asked for (the table decides through Level3TableStaging.Machine).</summary>
public enum Level3MachineInput
{
    /// <summary>The stand's card (arm the slot).</summary>
    Card,
    /// <summary>INSPECT ORIGINAL (stand card tab or note card tab).</summary>
    Inspect,
    /// <summary>COMPARE (note card tab, rows that share a transaction).</summary>
    Compare,
    /// <summary>The slot on top of the machine (tap card, then slot).</summary>
    Intake,
    /// <summary>A drag of the stand's card was released over the slot.</summary>
    CardDropped,
    /// <summary>A drag of the stand's card was released anywhere else.</summary>
    CardDropMissed,
    /// <summary>The half-fed card's exposed header was tapped (back out / cancel a recall).</summary>
    Header,
    /// <summary>The half-fed card's header started a drag (pull it back out).</summary>
    HeaderDragged,
    /// <summary>A selector label (index into Level3WorksheetRules.Treatments).</summary>
    Detent,
    /// <summary>Legacy relative detent intent; the physical dial now reports an absolute Detent.</summary>
    DialStep,
    /// <summary>The note card's Why? row.</summary>
    Why,
    /// <summary>A reason in the lifted Why? card (index into Level3WorksheetRules.Reasons).</summary>
    WhyOption,
    /// <summary>The lifted Why? card's Close.</summary>
    WhyClose,
    /// <summary>The lever was released past its threshold.</summary>
    LeverPulled,
    /// <summary>The lever handle was tapped, or tried while a choice is missing.</summary>
    LeverTapped,
    /// <summary>A review tab (index = rack slot).</summary>
    Tab,
    /// <summary>T06: the stand's batch card key (CHECK BATCH / CHECK AGAIN / COLLECT / CLOSE; the table decides which).</summary>
    BatchKey,
    /// <summary>T06: a total window (index 0 = INCOME, 1 = DEDUCTIONS): what is in it, read only.</summary>
    Window,
    /// <summary>T06: the folded summary on the output ledge (open it to read; never a collection).</summary>
    Summary
}

/// <summary>
/// PURPOSE:
/// Bea's table staging (revised T04) and the working paper machine (T05, 4
/// November 2026), built once from Level3DeskProps' cached boxes, rolls and
/// fixed print (the same construction as the office calculator and
/// clipboard). Layout comes from Level3TableStaging; every part sits on the
/// mat in table space.
///
/// ASSET CHOICE (inspected 4 November 2026): Synty SM_Prop_CardRoller_01 is a
/// single mesh with no sub-parts or animation, and "3D Models/Core
/// Items/printer1.fbx" is a 102 MB import with its own camera and light.
/// Neither has an intake, selector or lever, so the machine is primitives in
/// the office palette (cream body, navy trim, brass accents, paper cards,
/// calculator-display windows). No new asset, prefab or scene edit.
///
/// PARTS (T05): the stand's record card (the activity slip) is fed, never
/// shrunk: a 0.34 m body takes the 0.24 m card through a rear slot with a
/// front lip and two rollers. Half-fed, the card leans back at the stand's
/// angle with its position/title/amount exposed above the lip; the rest is
/// inside the opaque body. The full feed pivots it upright as it slides fully
/// behind the lip, then it is hidden (Tests/Level3Regression checks the paper
/// path stays inside the body/plinth). Front plate: three raised category keys,
/// an angular draggable dial with a neutral home, "Chosen:" and two total windows. Right side:
/// the lever on its pivot. In front: a rack of up to three review tabs. On the
/// stand while a card is loaded: a note card with the record's context,
/// Why? (R6/R7), INSPECT ORIGINAL and COMPARE; Why? lifts its own card into
/// the reading position.
///
/// INPUT: every touch surface is a world rectangle on the table's direct
/// EventSystem raycaster (Level3TablePaperRaycaster), which already refuses
/// touches unless the player sits free at the table. The table switches on
/// only the surfaces of the current machine step (no overlapping live
/// targets). Drags are owned by the surface that was pressed; a click after a
/// drag or a cancelled touch never counts. The model only reports intents:
/// the table's Level3TableStaging.Machine decides, and only the table saves.
///
/// T06 (5 November 2026): once the open batch is filed the stand shows the
/// batch card (one key: CHECK BATCH, then CHECK AGAIN; COLLECT when the summary
/// is out; CLOSE for the read-only totals list); the review tabs carry a small
/// mark after a check (brass tick = right, graphite "?" = look again); the
/// total windows open that read-only list; and the folded Income & Expense
/// Summary comes out of the output mouth onto its ledge (a tent card resting
/// on the empty rack), later flying to the Case Folder once collected. The
/// table decides and saves; these are views and touch surfaces only.
///
/// MOTION: one motion per part (card, lever, dial, note, Why?, tabs, rollers,
/// windows); a new motion replaces the old one, Settle snaps every motion to
/// its end (an interruption never waits or replays). Durations follow the
/// SIDE_QUEST_MACHINE table; reduced motion (WorldEmote.ReducedMotion) snaps.
/// </summary>
public sealed class Level3PaperMachineModel
{
    // ---------- stand card (metres, card plane; print sizes measured with the shipped font in Tests/VerifyLevel3Print.py) ----------
    public const float CardWidth = 0.24f;
    public const float CardHeight = 0.18f;
    public const float CardPad = 0.012f;
    public const float CardTextWidth = CardWidth - CardPad * 2f;
    public const float CardTop = 0.008f;
    public const float CardRowGap = 0.003f;
    public const float PositionPoints = 105f;
    public const float PositionHeight = 0.013f;
    public const float TitlePoints = 170f;
    public const float TitleHeight = 0.021f;
    public const float AmountPoints = 320f;
    public const float AmountHeight = 0.039f;
    public const float PurposePoints = 125f;
    public const float PurposeHeight = 0.016f;
    public const float DetailsPoints = 125f;
    public const float DetailsHeight = 0.03f;
    public const float TabHeight = 0.026f;
    public const float TabPoints = 130f;
    public const string InspectLabel = "INSPECT ORIGINAL";
    /// <summary>The stand's plate leans back this far from horizontal (its face toward the seated reader).</summary>
    public const float StandTilt = 35f;
    private const float CardArriveSeconds = 0.22f;

    // ---------- machine body (metres, machine space: -Z toward the player, +Y up) ----------
    public const float BodyWidth = 0.34f;
    public const float BodyHeight = 0.17f;
    public const float BodyOffsetX = -0.015f;
    public const float PlinthHeight = 0.01f;
    public const float BodyFrontZ = -0.02f;
    public const float BodyBackZ = Level3TableStaging.MachineDepth * 0.5f;
    public const float TopY = PlinthHeight + BodyHeight;
    public const float PlateBottomZ = -0.065f;
    public const float PlateBottomY = 0.016f;

    // ---------- paper path ----------
    /// <summary>The slot line on top of the body (z), between its two rollers.</summary>
    public const float SlotZ = 0.075f;
    public const float SlotWidth = CardWidth + 0.012f;
    public const float RollerGap = 0.012f;
    public const float RollerDiameter = 0.011f;
    /// <summary>A half-fed card leans back at the stand's angle (degrees from horizontal) and pivots upright as it is drawn in.</summary>
    public const float FeedTilt = StandTilt;
    public const float FedTilt = 75f;
    /// <summary>Card length above the slot line: half-fed (position, title and amount out), hovering, fully fed (top edge behind the lip).</summary>
    public const float HalfFedExposed = 0.092f;
    public const float HoverExposed = CardHeight + 0.02f;
    public const float FedExposed = -0.006f;

    // ---------- selector, windows, lever (plate space: +Z up the plate, +Y its face) ----------
    public const float DialLabelPoints = 135f;
    public const float DialLabelWidth = 0.1f;
    public const float DialLabelHeight = 0.021f;
    public const float CategoryButtonHeight = 0.052f;
    public const float DialTouchWidth = 0.104f;
    public const float DialTouchHeight = 0.084f;
    public const float PaperTouchWidth = CardWidth + 0.016f;
    public const float PaperTouchHeight = CardHeight + 0.012f;
    public const float ReviewTouchWidth = RackTabWidth + 0.006f;
    public const float ReviewTouchHeight = RackTabDepth + 0.014f;
    public const float DialDeadRadius = 0.01f;
    public const float DialHysteresis = 4f;
    public const float DragFollowSeconds = 0.035f;
    public const float ControlFollowSeconds = 0.025f;
    public const float DetentSpacing = 0.108f;
    public const float LabelRowZ = 0.052f;
    public const float DialZ = -0.019f;
    public const float DialDiameter = 0.072f;
    public const float DialTouchZ = DialZ - 0.006f;
    public const float ChosenPoints = 105f;
    public const float ChosenWidth = 0.11f;
    public const float ChosenHeight = 0.016f;
    public const float ChosenZ = -0.075f;
    public const float WindowLabelPoints = 110f;
    public const float WindowLabelHeight = 0.016f;
    public const float WindowWidth = 0.1f;
    public const float WindowValuePoints = 120f;
    public const float WindowValueHeight = 0.021f;
    public const float WindowSurfaceY = 0.0055f;
    public const float WindowTextY = 0.008f;
    public const float LeverLength = 0.108f;
    /// <summary>Lever angles about its pivot (degrees): raised leans back, pulled comes forward and down toward the player.</summary>
    public const float LeverRaised = 18f;
    public const float LeverPulled = -55f;
    public const float LeverPivotY = 0.11f;
    public const float LeverPivotZ = 0.045f;
    public const float LeverTouchX = BodyOffsetX + BodyWidth * 0.5f + 0.026f;
    public const float LeverTouchY = 0.205f;
    public const float LeverTouchZ = 0.078f;
    public const float LeverTouchWidth = 0.05f;
    public const float LeverTouchHeight = 0.084f;
    /// <summary>The slot's tap area on top of the body (x from the body's left edge; it stops short of the lever's).</summary>
    public const float IntakeTouchWidth = BodyWidth - 0.02f;
    /// <summary>Share of the lever's stroke a pull must pass before release files (below it springs back).</summary>
    public const float LeverThreshold = 0.55f;
    public static readonly string[] DialLabels = { "Income", "Deduct", "Leave Out" };
    public static readonly string[] WindowLabels = { "INCOME", "DEDUCTIONS" };
    /// <summary>Pointer angles (degrees, plate space) of the three detents; the neutral home points down the plate.</summary>
    public static readonly float[] DetentAngles = { -57f, 0f, 57f };
    public const float NeutralAngle = 180f;

    // ---------- note card, Why? card, review tabs ----------
    public const float WhyRowHeight = 0.046f;
    public const float WhyLabelPoints = 170f;
    public const float WhyValuePoints = 125f;
    public const float WhyValueWidth = 0.15f;
    public const float NoteTabHeight = 0.032f;
    /// <summary>INSPECT ORIGINAL sharing its row with COMPARE: two lines at this size (the full-width tab keeps TabPoints).</summary>
    public const float NoteSplitPoints = 105f;
    public const float NoteHalfTabWidth = (CardWidth - 0.016f - 0.008f) * 0.5f;
    public const string CompareLabel = "COMPARE";
    public const string WhyPrompt = "Choose a reason";
    public const float WhyCardHeight = 0.23f;
    public const float WhyTitlePoints = 200f;
    public const float WhyTitleHeight = 0.026f;
    public const float WhyOptionPoints = 150f;
    public const float WhyOptionHeight = 0.027f;
    public const float WhyCloseHeight = 0.022f;
    public const string WhyCloseLabel = "CLOSE";
    public const float RackTabWidth = 0.09f;
    public const float RackTabDepth = 0.052f;
    public const float RackTabTilt = 25f;
    public const float RackTitlePoints = 90f;
    public const float RackTitleHeight = 0.023f;
    public const float RackTitleWidth = RackTabWidth - 0.006f;
    public const float RackCategoryPoints = 100f;
    public const float RackCategoryHeight = 0.014f;

    // ---------- T06: batch card, review marks, total windows, summary output ----------
    public const float BatchStatusPoints = 125f;
    public const float BatchStatusHeight = 0.016f;
    public const float BatchBodyPoints = 100f;
    public const float BatchBodyHeight = 0.06f;
    public const float BatchKeyPoints = 150f;
    public const float BatchKeyHeight = 0.04f;
    /// <summary>The key's touch area reaches past its face (into plain card), so the one primary action meets the 48 dp aim.</summary>
    public const float BatchKeyTouchHeight = BatchKeyHeight + 0.022f;
    public const float NoteHintPoints = 100f;
    public const float NoteHintHeight = 0.023f;
    public const float MarkDiameter = 0.014f;
    public const float MarkPoints = 110f;
    public const float MarkX = RackTabWidth * 0.5f - 0.011f;
    public const float MarkZ = 0.013f;
    public const float WindowTouchWidth = WindowWidth;
    public const float WindowTouchHeight = 0.05f;
    public const float WindowTouchZ = -0.017f;
    public const string WindowPrompt = "Tap a total to see what's in it.";
    public const float SummaryWidth = 0.15f;
    public const float SummaryHeight = 0.105f;
    public const float SummaryLean = 72f;
    public const float SummaryTextWidth = SummaryWidth - 0.014f;
    public const float SummaryTitlePoints = 100f;
    public const float SummaryTitleHeight = 0.025f;
    public const float SummaryLabelPoints = 90f;
    public const float SummaryLabelHeight = 0.011f;
    public const float SummaryAmountPoints = 170f;
    public const float SummaryAmountHeight = 0.02f;
    /// <summary>The summary's centre across the column (under the widened output mouth).</summary>
    public const float SummaryX = 0.07f;
    /// <summary>Its front edge, rack space (just behind the rack's near edge).</summary>
    public const float SummaryFrontZ = -0.026f;
    public const float OutputMouthWidth = SummaryWidth;
    public const float SummaryTouchWidth = SummaryWidth + 0.014f;
    public const float SummaryTouchHeight = SummaryHeight + 0.012f;

    // ---------- motion (seconds; SIDE_QUEST_MACHINE starting ranges) ----------
    public const float CardPickupSeconds = 0.18f;
    public const float DragAlignSeconds = 0.24f;
    public const float DragReturnSeconds = 0.32f;
    public const float ArmSeconds = 0.1f;
    public const float HalfFeedSeconds = 0.22f;
    public const float SlideInSeconds = 0.16f;
    public const float ReturnSeconds = 0.22f;
    public const float DetentSeconds = 0.1f;
    public const float LeverDownSeconds = 0.14f;
    public const float LeverReturnSeconds = 0.16f;
    public const float FullFeedSeconds = 0.26f;
    public const float TabSettleSeconds = 0.16f;
    public const float NoteSeconds = 0.16f;
    public const float WhyLiftSeconds = 0.22f;
    public const float CardAwaySeconds = 0.14f;
    public const float EmergeSeconds = 0.22f;
    public const float ExtractSeconds = 0.1f;
    public const float BatchCardSeconds = 0.2f;
    public const float KeyPressSeconds = 0.08f;
    public const float MarkSeconds = 0.14f;
    public const float MarkStagger = 0.05f;
    public const float GatherSeconds = 0.18f;
    /// <summary>The one distinct completion moment: out of the mouth, onto the ledge, unfolding.</summary>
    public const float SummaryOutSeconds = 0.42f;
    public const float CollectFlySeconds = 0.5f;

    public Transform Stand { get; private set; }
    /// <summary>Fixed dock on the stand; the activity Card itself moves into FeedFrame during filing.</summary>
    public Transform StandDocumentAnchor { get; private set; }
    public Transform Card { get; private set; }
    public Transform Machine { get; private set; }
    public Transform Rack { get; private set; }
    public Transform IntakeSlot { get; private set; }
    /// <summary>Guide targets: the physical CHECK BATCH key and the Why? row.</summary>
    public Transform BatchKey => batchKey;
    public Transform WhyRow => noteWhyRow != null ? noteWhyRow.transform : null;
    public Transform[] Rollers { get; private set; }
    public Transform DialPointer { get; private set; }
    public Transform LeverHinge { get; private set; }
    public Transform OutputLedge { get; private set; }
    public Transform[] RackSlots { get; private set; }
    public TextMeshPro[] WindowValues { get; private set; }
    /// <summary>T05: the slot line the card feeds along (+Z up the half-fed card, +Y its face).</summary>
    public Transform FeedFrame { get; private set; }
    public Transform NoteCard { get; private set; }
    public Transform WhyCard { get; private set; }
    public bool CardShown => Card != null && Card.gameObject.activeSelf;
    /// <summary>The Why? card is up in the reading position.</summary>
    public bool WhyUp { get; private set; }
    /// <summary>A card or lever drag is in progress (owned by its pressed surface).</summary>
    public bool Dragging => drag != DragKind.None;
    /// <summary>T06: the batch card on the stand (check / summary / totals list).</summary>
    public Transform BatchCard { get; private set; }
    public bool BatchCardShown => BatchCard != null && BatchCard.gameObject.activeSelf;
    /// <summary>T06: the folded summary card on the output ledge.</summary>
    public Transform SummaryCard { get; private set; }
    public bool SummaryShown => SummaryCard != null && SummaryCard.gameObject.activeSelf;
    /// <summary>T06: the summary is coming out, unfolding or flying (a touch finishes it).</summary>
    public bool SummaryMoving => summaryMotion.Running;
    /// <summary>T06: marks are appearing or a batch's tabs are gathering.</summary>
    public bool ReviewMoving => markMotion.Running || gatherMotion.Running || batchMotion.Running;

    private enum Part { Card, Inspect, Header, Intake, Detent, Dial, Lever, Why, WhyOption, WhyClose, NoteInspect, NoteCompare, Tab, BatchKey, Window, Summary }
    private enum DragKind { None, Card, Dial, Lever }
    private enum CardPlace { Stand, Away, Flying, Dragging, Loaded, Inside }

    private Level3DeskProps props;
    private Action<Level3MachineInput, int> input;
    private Transform table;
    private float scale = 1f;
    private TextMeshPro position, title, amount, purpose, details;
    private Vector3 cardRest;
    private float arriveStart = -1f;
    private CardPlace cardPlace = CardPlace.Stand;
    private bool armed;
    private Camera viewCamera;
    private bool touchOn;

    // Touch surfaces (one direct raycaster each).
    private readonly List<Surface> surfaces = new List<Surface>();
    private Surface cardTouch, headerTouch, intakeTouch, dialTouch, leverTouch, whyTouch, noteInspectTouch, noteCompareTouch, whyCloseTouch;
    private readonly Surface[] detentTouch = new Surface[3];
    private readonly Surface[] optionTouch = new Surface[5];
    private readonly Surface[] tabTouch = new Surface[3];
    private Surface batchKeyTouch, summaryTouch;
    private readonly Surface[] windowTouch = new Surface[2];

    // Machine parts that move or change.
    private Transform plate;
    private GameObject intakeOutline;
    private GameObject chosenFrame;
    private readonly Transform[] categoryButtons = new Transform[3];
    private readonly MeshRenderer[] categoryFaces = new MeshRenderer[3];
    private readonly TextMeshPro[] categoryLabels = new TextMeshPro[3];
    private TextMeshPro chosenLabel;
    private float leverAngle = LeverRaised;
    private float dialAngle = NeutralAngle;
    private Quaternion[] rollerRest;
    private float rollerSpin;
    private float rollerSpeed;
    private readonly Transform[] tabs = new Transform[3];
    private readonly TextMeshPro[] tabTitles = new TextMeshPro[3];
    private readonly TextMeshPro[] tabCategories = new TextMeshPro[3];
    private readonly string[] tabRows = new string[3];
    private Vector3[] tabRest;
    private TextMeshPro noteWhyValue, notePurpose, noteDetails, noteInspectLabel;
    private GameObject noteWhyRow, noteCompareTab;
    private Transform noteInspectTab;
    private Vector3 noteRest;
    private TextMeshPro whySubtitle;
    private readonly GameObject[] optionFrames = new GameObject[5];
    private Vector3 whyRest;
    private bool noteShown;
    private bool hasPartner;
    private bool needsWhy;
    private TextMeshPro noteHint;
    // T06 parts.
    private Vector3 batchRest;
    private TextMeshPro batchPosition, batchTitle, batchStatus, batchBody, batchKeyLabel;
    private Transform batchKey;
    private MeshRenderer batchKeyFace;
    private bool batchKeyLive;
    private readonly Transform[] markTick = new Transform[3], markAsk = new Transform[3];
    private readonly Level3WorksheetCheck.Mark[] marks = new Level3WorksheetCheck.Mark[3];
    private Transform summaryFront, summaryBack;
    private Vector3 summaryRest;
    private float summaryBackAngle, summaryBackLength;
    private TextMeshPro summaryTitle, summaryIncome, summaryDeductions, summaryIncomeLabel, summaryDeductionsLabel;

    // One motion per part.
    private readonly Motion cardMotion = new Motion(), leverMotion = new Motion(), dialMotion = new Motion(), noteMotion = new Motion(),
        whyMotion = new Motion(), tabMotion = new Motion(), windowMotion = new Motion(),
        batchMotion = new Motion(), keyMotion = new Motion(), markMotion = new Motion(), gatherMotion = new Motion(), summaryMotion = new Motion();
    private bool settling;

    // Drag state (the pressed surface owns it).
    private DragKind drag;
    private Level3MachineTouch dragOwner;
    private Vector2 dragStart;
    private int selectedDetent = -1;
    private float dialDragAngle, leverStartProgress;
    private Vector2 dragGrabOffset, dialLastOffset;
    private float dragDepth, dragScale, pickupStart = -1f, pickupScale;
    private Pose pickupPose;
    private Vector2 leverRaisedScreen, leverPulledScreen;
    private float leverProgress;
    private bool leverResists;
    private Vector3 dragTarget;
    private Quaternion dragRotation;
    private bool overIntake;

    /// <summary>Builds stand, machine and rack under table (table space, surfaceY = the mat's top), turned as the layout says.</summary>
    public static Level3PaperMachineModel Build(Level3DeskProps props, Transform table, Level3TableStaging.Layout layout, float surfaceY,
        int layer, Action<Level3MachineInput, int> input)
    {
        var model = new Level3PaperMachineModel { props = props, input = input, table = table, scale = layout.Scale };
        model.Stand = Root("Reading Stand", table, layout.Stand, layout.StandYaw, surfaceY, layout.Scale, layer);
        model.BuildStand(layer);
        // The machine and its rack are one piece: both hang under the column root and turn together.
        Transform column = Root("Paper Machine", table, layout.Column, layout.MachineYaw, surfaceY, layout.Scale, layer);
        model.Machine = Child("Machine", column, layout.Machine, layout.Column, layout.Scale, layer);
        model.BuildMachine(layer);
        model.Rack = Child("Review Rack", column, layout.Rack, layout.Column, layout.Scale, layer);
        model.BuildRack(layer);
        model.ShowCard(null, animate: false);
        model.SetTouch(false, null);
        return model;
    }

    private static Transform Child(string name, Transform column, Rect area, Rect columnArea, float scale, int layer)
    {
        var child = new GameObject(name).transform;
        child.gameObject.layer = layer;
        child.SetParent(column, false);
        child.localPosition = new Vector3(0f, 0f, (area.center.y - columnArea.center.y) / Mathf.Max(0.0001f, scale));
        return child;
    }

    private static Transform Root(string name, Transform table, Rect area, float yaw, float surfaceY, float scale, int layer)
    {
        var root = new GameObject(name).transform;
        root.gameObject.layer = layer;
        root.SetParent(table, false);
        root.localPosition = new Vector3(area.center.x, surfaceY, area.center.y);
        root.localRotation = Quaternion.Euler(0f, yaw, 0f);
        root.localScale = Vector3.one * scale;
        return root;
    }

    private Transform Node(string name, Transform parent, Vector3 localPosition, Quaternion localRotation)
    {
        var node = new GameObject(name).transform;
        node.gameObject.layer = parent.gameObject.layer;
        node.SetParent(parent, false);
        node.localPosition = localPosition;
        node.localRotation = localRotation;
        return node;
    }

    // =========================================================
    // STAND, RECORD CARD AND NOTE CARD
    // =========================================================

    private Transform hinge;

    private void BuildStand(int layer)
    {
        float depth = Level3TableStaging.StandDepth, plateLength = CardHeight + 0.03f;
        float hingeZ = -depth * 0.5f + 0.03f;
        props.Box("Foot", Stand, new Vector3(0f, 0.006f, hingeZ), new Vector3(Level3TableStaging.StandWidth, 0.012f, 0.06f), Level3DeskProps.NavyLight);
        // Local +Z runs up the plate, +Y is its face (toward the seat).
        hinge = Node("Easel", Stand, new Vector3(0f, 0.012f, hingeZ), Quaternion.Euler(-StandTilt, 0f, 0f));
        props.Box("Plate", hinge, new Vector3(0f, -0.003f, plateLength * 0.5f), new Vector3(CardWidth + 0.02f, 0.006f, plateLength), Level3DeskProps.NavyLight);
        props.Box("Lip", hinge, new Vector3(0f, 0.007f, 0.004f), new Vector3(CardWidth + 0.02f, 0.014f, 0.008f), Level3DeskProps.Brass);
        Vector3 top = hinge.localPosition + hinge.localRotation * new Vector3(0f, 0f, plateLength);
        props.Box("Strut", Stand, new Vector3(0f, top.y * 0.5f, top.z - 0.012f), new Vector3(0.08f, top.y, 0.008f), Level3DeskProps.NavyLight);
        props.Box("Back Foot", Stand, new Vector3(0f, 0.005f, top.z - 0.012f), new Vector3(0.1f, 0.01f, 0.03f), Level3DeskProps.NavyLight);

        cardRest = new Vector3(0f, 0.0015f, 0.012f + CardHeight * 0.5f);
        StandDocumentAnchor = Node("Stand Document Anchor", hinge, cardRest + Vector3.up * 0.003f, Quaternion.identity);
        Card = Node("Record Card", hinge, cardRest, Quaternion.identity);
        props.Box("Card", Card, Vector3.zero, new Vector3(CardWidth, 0.002f, CardHeight), Level3DeskProps.Paper);

        float x = 0f, y = 0.0012f, z = CardHeight * 0.5f - CardTop;
        position = Row("Position", ref z, PositionHeight, PositionPoints, Level3DeskProps.Ink, TextAlignmentOptions.Right, false);
        title = Row("Title", ref z, TitleHeight, TitlePoints, Level3DeskProps.Ink, TextAlignmentOptions.Left, true);
        amount = Row("Amount", ref z, AmountHeight, AmountPoints, Level3DeskProps.Ink, TextAlignmentOptions.Left, true);
        purpose = Row("Purpose", ref z, PurposeHeight, PurposePoints, Level3DeskProps.Ink, TextAlignmentOptions.Left, false);
        details = Row("Details", ref z, DetailsHeight, DetailsPoints, Level3DeskProps.Ink, TextAlignmentOptions.Left, false);
        float tabZ = -CardHeight * 0.5f + TabHeight * 0.5f + 0.004f;
        GameObject inspect = props.Box("Inspect Tab", Card, new Vector3(x, 0.0012f, tabZ), new Vector3(CardWidth - 0.016f, 0.0008f, TabHeight), Level3DeskProps.Brass);
        inspect.SetActive(false);
        TextMeshPro inspectLabel = props.FixedPrint("Inspect Label", Card, new Vector3(x, 0.0018f, tabZ), CardTextWidth, TabHeight, InspectLabel, Level3DeskProps.Ink, TabPoints,
            TextAlignmentOptions.Center, bold: true);
        inspectLabel.gameObject.SetActive(false);

        TextMeshPro Row(string name, ref float rowTop, float height, float points, Color color, TextAlignmentOptions alignment, bool bold)
        {
            TextMeshPro label = props.FixedPrint(name, Card, new Vector3(x, y, rowTop - height * 0.5f), CardTextWidth, height, "", color, points, alignment, bold);
            rowTop -= height + CardRowGap;
            return label;
        }

        // One full-paper surface owns both actions: a tap in the printed strip inspects;
        // a drag from anywhere (including that strip) carries the paper. No competing targets.
        cardTouch = AddSurface("Card Touch", Card, new Vector3(0f, 0.003f, 0f), PaperTouchWidth, PaperTouchHeight, Part.Card, 0);
        float header = HalfFedExposed - 0.008f;
        headerTouch = AddSurface("Header Touch", Card, new Vector3(0f, 0.003f, CardHeight * 0.5f - header * 0.5f), CardWidth, header, Part.Header, 0);

        BuildNoteCard();
        BuildWhyCard();
        BuildBatchCard();
    }

    /// <summary>The note card on the stand while a record is loaded: its context, Why? (R6/R7), INSPECT ORIGINAL and COMPARE.</summary>
    private void BuildNoteCard()
    {
        noteRest = cardRest;
        NoteCard = Node("Note Card", hinge, noteRest, Quaternion.identity);
        props.Box("Note", NoteCard, Vector3.zero, new Vector3(CardWidth, 0.002f, CardHeight), Level3DeskProps.Cream);
        float topZ = CardHeight * 0.5f - CardTop;
        // Why? row (R6/R7): a framed strip across the top.
        noteWhyRow = new GameObject("Why Row");
        noteWhyRow.layer = NoteCard.gameObject.layer;
        noteWhyRow.transform.SetParent(NoteCard, false);
        float whyZ = topZ - WhyRowHeight * 0.5f;
        props.Box("Why Strip", noteWhyRow.transform, new Vector3(0f, 0.0011f, whyZ), new Vector3(CardWidth - 0.016f, 0.0006f, WhyRowHeight), Level3DeskProps.Paper);
        props.Frame("Why Edge", noteWhyRow.transform, new Vector3(0f, 0.0014f, whyZ), CardWidth - 0.016f, WhyRowHeight, 0.003f, Level3DeskProps.Brass);
        props.FixedPrint("Why Label", noteWhyRow.transform, new Vector3(-CardTextWidth * 0.5f + 0.032f, 0.0018f, whyZ), 0.06f, 0.024f, "WHY?",
            Level3DeskProps.Ink, WhyLabelPoints, TextAlignmentOptions.Left, bold: true);
        noteWhyValue = props.FixedPrint("Why Value", noteWhyRow.transform, new Vector3(CardTextWidth * 0.5f - WhyValueWidth * 0.5f, 0.0018f, whyZ),
            WhyValueWidth, 0.018f, WhyPrompt, Level3DeskProps.Ink, WhyValuePoints, TextAlignmentOptions.Right);
        whyTouch = AddSurface("Why Touch", noteWhyRow.transform, new Vector3(0f, 0.003f, whyZ), CardWidth - 0.012f, WhyRowHeight + 0.004f, Part.Why, 0);

        notePurpose = props.FixedPrint("Note Purpose", NoteCard, Vector3.zero, CardTextWidth, PurposeHeight, "", Level3DeskProps.Ink, PurposePoints, TextAlignmentOptions.Left);
        noteDetails = props.FixedPrint("Note Details", NoteCard, Vector3.zero, CardTextWidth, DetailsHeight, "", Level3DeskProps.Ink, DetailsPoints, TextAlignmentOptions.Left);
        // T06: after a check marked this record, one short "look again" line (where to look, never the answer).
        noteHint = props.FixedPrint("Note Look Again", NoteCard, Vector3.zero, CardTextWidth, NoteHintHeight, "", Level3DeskProps.Ink, NoteHintPoints,
            TextAlignmentOptions.TopLeft, bold: false, wrap: true);

        float tabZ = -CardHeight * 0.5f + NoteTabHeight * 0.5f + 0.006f;
        noteInspectTab = Node("Note Inspect", NoteCard, new Vector3(0f, 0f, tabZ), Quaternion.identity);
        props.Box("Inspect Tab", noteInspectTab, new Vector3(0f, 0.0012f, 0f), new Vector3(1f, 0.0008f, NoteTabHeight), Level3DeskProps.Brass);
        noteInspectLabel = props.FixedPrint("Inspect Label", noteInspectTab, new Vector3(0f, 0.0018f, 0f), CardTextWidth, NoteTabHeight, InspectLabel,
            Level3DeskProps.Ink, TabPoints, TextAlignmentOptions.Center, bold: true);
        noteInspectTouch = AddSurface("Note Inspect Touch", noteInspectTab, new Vector3(0f, 0.003f, 0f), 1f, NoteTabHeight + 0.004f, Part.NoteInspect, 0);
        float half = (CardWidth - 0.016f - 0.008f) * 0.5f;
        noteCompareTab = new GameObject("Note Compare");
        noteCompareTab.layer = NoteCard.gameObject.layer;
        noteCompareTab.transform.SetParent(NoteCard, false);
        noteCompareTab.transform.localPosition = new Vector3(half * 0.5f + 0.004f, 0f, tabZ);
        props.Box("Compare Tab", noteCompareTab.transform, new Vector3(0f, 0.0012f, 0f), new Vector3(half, 0.0008f, NoteTabHeight), Level3DeskProps.BrassSoft);
        props.FixedPrint("Compare Label", noteCompareTab.transform, new Vector3(0f, 0.0018f, 0f), half - 0.008f, NoteTabHeight, CompareLabel,
            Level3DeskProps.Ink, TabPoints, TextAlignmentOptions.Center, bold: true);
        noteCompareTouch = AddSurface("Note Compare Touch", noteCompareTab.transform, new Vector3(0f, 0.003f, 0f), half, NoteTabHeight + 0.004f, Part.NoteCompare, 0);
        NoteCard.gameObject.SetActive(false);
    }

    /// <summary>The Why? card: five neutral reasons (the existing vocabulary, fixed order) and Close; lifted into the reading position on demand.</summary>
    private void BuildWhyCard()
    {
        whyRest = cardRest + new Vector3(0f, 0.004f, 0f);
        WhyCard = Node("Why Card", hinge, whyRest, Quaternion.identity);
        props.Box("Why Paper", WhyCard, Vector3.zero, new Vector3(CardWidth, 0.002f, WhyCardHeight), Level3DeskProps.Paper);
        float z = WhyCardHeight * 0.5f - CardTop - WhyTitleHeight * 0.5f;
        props.FixedPrint("Why Title", WhyCard, new Vector3(0f, 0.0012f, z), CardTextWidth, WhyTitleHeight, "WHY?", Level3DeskProps.Ink, WhyTitlePoints,
            TextAlignmentOptions.Left, bold: true);
        z -= WhyTitleHeight * 0.5f + CardRowGap + PurposeHeight * 0.5f;
        whySubtitle = props.FixedPrint("Why Record", WhyCard, new Vector3(0f, 0.0012f, z), CardTextWidth, PurposeHeight, "", Level3DeskProps.InkMuted,
            PurposePoints, TextAlignmentOptions.Left);
        z -= PurposeHeight * 0.5f + 0.006f;
        string[] reasons = Level3WorksheetRules.Reasons;
        for (int i = 0; i < optionTouch.Length && i < reasons.Length; i++)
        {
            float rowZ = z - WhyOptionHeight * 0.5f;
            props.Box("Option " + (i + 1), WhyCard, new Vector3(0f, 0.0011f, rowZ), new Vector3(CardWidth - 0.016f, 0.0006f, WhyOptionHeight - 0.003f), Level3DeskProps.Cream);
            props.FixedPrint("Option Label " + (i + 1), WhyCard, new Vector3(0f, 0.0017f, rowZ), CardTextWidth - 0.008f, WhyOptionHeight - 0.004f,
                Level3WorksheetRules.ReasonLabel(reasons[i]), Level3DeskProps.Ink, WhyOptionPoints, TextAlignmentOptions.Left);
            optionFrames[i] = props.Frame("Option Chosen " + (i + 1), WhyCard, new Vector3(0f, 0.0016f, rowZ), CardWidth - 0.016f, WhyOptionHeight - 0.003f, 0.0025f,
                Level3DeskProps.Highlight);
            optionFrames[i].SetActive(false);
            optionTouch[i] = AddSurface("Option Touch " + (i + 1), WhyCard, new Vector3(0f, 0.003f, rowZ), CardWidth - 0.012f, WhyOptionHeight - 0.002f, Part.WhyOption, i);
            z -= WhyOptionHeight;
        }
        float closeZ = -WhyCardHeight * 0.5f + WhyCloseHeight * 0.5f + 0.006f;
        props.Box("Close Tab", WhyCard, new Vector3(0f, 0.0012f, closeZ), new Vector3(0.1f, 0.0008f, WhyCloseHeight), Level3DeskProps.BrassSoft);
        props.FixedPrint("Close Label", WhyCard, new Vector3(0f, 0.0018f, closeZ), 0.09f, WhyCloseHeight, WhyCloseLabel, Level3DeskProps.Ink, TabPoints,
            TextAlignmentOptions.Center, bold: true);
        whyCloseTouch = AddSurface("Close Touch", WhyCard, new Vector3(0f, 0.003f, closeZ), 0.12f, WhyCloseHeight + 0.004f, Part.WhyClose, 0);
        WhyCard.gameObject.SetActive(false);
    }

    /// <summary>
    /// T06: the batch card, in the stand's one reading place (never beside the record card): position,
    /// title, one status line, a short body (a look-again line, or the read-only totals list) and ONE
    /// large key. The table writes its text; only the key is a touch surface.
    /// </summary>
    private void BuildBatchCard()
    {
        batchRest = cardRest;
        BatchCard = Node("Batch Card", hinge, batchRest, Quaternion.identity);
        props.Box("Batch Paper", BatchCard, Vector3.zero, new Vector3(CardWidth, 0.002f, CardHeight), Level3DeskProps.Cream);
        float z = CardHeight * 0.5f - CardTop;
        TextMeshPro Row(string name, float height, float points, Color color, TextAlignmentOptions alignment, bool bold, bool wrap = false)
        {
            TextMeshPro label = props.FixedPrint(name, BatchCard, new Vector3(0f, 0.0012f, z - height * 0.5f), CardTextWidth, height, "", color, points, alignment, bold, wrap);
            z -= height + CardRowGap;
            return label;
        }
        batchPosition = Row("Batch Position", PositionHeight, PositionPoints, Level3DeskProps.Ink, TextAlignmentOptions.Right, false);
        batchTitle = Row("Batch Title", TitleHeight, TitlePoints, Level3DeskProps.Ink, TextAlignmentOptions.Left, true);
        batchStatus = Row("Batch Status", BatchStatusHeight, BatchStatusPoints, Level3DeskProps.Ink, TextAlignmentOptions.Left, true);
        z -= 0.001f;
        batchBody = Row("Batch Body", BatchBodyHeight, BatchBodyPoints, Level3DeskProps.Ink, TextAlignmentOptions.TopLeft, false, wrap: true);
        batchKey = Node("Batch Key", Stand, new Vector3(0f, 0.022f, -Level3TableStaging.StandDepth * 0.5f), Quaternion.identity);
        batchKeyFace = props.Box("Key Face", batchKey, new Vector3(0f, 0.004f, 0f), new Vector3(CardWidth - 0.016f, 0.006f, BatchKeyHeight), Level3DeskProps.Brass).GetComponent<MeshRenderer>();
        props.Frame("Key Edge", batchKey, new Vector3(0f, 0.0072f, 0f), CardWidth - 0.016f, BatchKeyHeight, 0.002f, Level3DeskProps.NavyLight);
        batchKeyLabel = props.FixedPrint("Key Label", batchKey, new Vector3(0f, 0.0075f, 0f), CardTextWidth, BatchKeyHeight, "", Level3DeskProps.Ink, BatchKeyPoints,
            TextAlignmentOptions.Center, bold: true);
        batchKeyTouch = AddSurface("Batch Key Touch", batchKey, new Vector3(0f, 0.009f, 0f), CardWidth - 0.008f, BatchKeyTouchHeight, Part.BatchKey, 0);
        BatchCard.gameObject.SetActive(false);
        batchKey.gameObject.SetActive(false);
    }

    /// <summary>T06: what the batch card says (the table writes every line; empty Key hides the key).</summary>
    public struct BatchFace
    {
        public string Position;
        public string Title;
        public string Status;
        public string Body;
        public string Key;
        /// <summary>The key takes touches (false while a check or collection is being saved, or a motion plays).</summary>
        public bool KeyLive;
    }

    /// <summary>T06: the batch card onto the stand (the record card is off it), or its text refreshed in place.</summary>
    public void ShowBatchCard(BatchFace face, bool animate)
    {
        if (BatchCard == null) return;
        batchPosition.text = face.Position ?? "";
        batchTitle.text = face.Title ?? "";
        batchStatus.text = face.Status ?? "";
        batchBody.text = face.Body ?? "";
        batchKeyLabel.text = face.Key ?? "";
        batchKey.gameObject.SetActive(!string.IsNullOrEmpty(face.Key));
        batchKeyLive = face.KeyLive && !string.IsNullOrEmpty(face.Key);
        batchKeyFace.sharedMaterial = props.Lit(batchKeyLive ? Level3DeskProps.Brass : Level3DeskProps.BrassSoft);
        if (!BatchCard.gameObject.activeSelf)
        {
            BatchCard.gameObject.SetActive(true);
            Vector3 from = ArriveFrom;
            BatchCard.localPosition = animate ? from : batchRest;
            if (animate) Run(batchMotion, BatchCardSeconds, t => BatchCard.localPosition = Vector3.LerpUnclamped(from, batchRest, EaseOut(t)),
                () => { BatchCard.localPosition = batchRest; RefreshSurfaces(); });
            else batchMotion.Stop();
        }
        RefreshSurfaces();
    }

    /// <summary>T06: the batch card leaves the stand (a short slide toward the stack when animated).</summary>
    public void HideBatchCard(bool animate)
    {
        if (batchKey != null) batchKey.gameObject.SetActive(false);
        if (!BatchCardShown) return;
        if (!animate)
        {
            batchMotion.Stop();
            BatchCard.gameObject.SetActive(false);
            BatchCard.localPosition = batchRest;
            RefreshSurfaces();
            return;
        }
        Vector3 from = BatchCard.localPosition, to = ArriveFrom;
        Run(batchMotion, CardAwaySeconds, t => BatchCard.localPosition = Vector3.LerpUnclamped(from, to, EaseIn(t)), () =>
        {
            BatchCard.gameObject.SetActive(false);
            BatchCard.localPosition = batchRest;
            RefreshSurfaces();
        });
        RefreshSurfaces();
    }

    /// <summary>T06: the key goes down and springs back (a pressed physical key; the save has its own answer).</summary>
    public void PressBatchKey()
    {
        if (batchKey == null) return;
        Vector3 rest = batchKey.localPosition;
        Run(keyMotion, KeyPressSeconds * 2f, t => batchKey.localPosition = rest - Vector3.up * (0.0012f * Mathf.Sin(t * Mathf.PI)),
            () => batchKey.localPosition = rest);
    }

    /// <summary>
    /// T06: the review marks on the rack's tabs, by slot (None hides one). Marks that newly appear pop in one
    /// after another (a quiet reveal; reduced motion and settles show them at once).
    /// </summary>
    public void SetTabMarks(IList<Level3WorksheetCheck.Mark> bySlot, bool animate)
    {
        var appear = new List<Transform>();
        for (int i = 0; i < marks.Length; i++)
        {
            Level3WorksheetCheck.Mark next = bySlot != null && i < bySlot.Count ? bySlot[i] : Level3WorksheetCheck.Mark.None;
            bool changed = marks[i] != next;
            marks[i] = next;
            markTick[i].gameObject.SetActive(next == Level3WorksheetCheck.Mark.Confirmed);
            markAsk[i].gameObject.SetActive(next == Level3WorksheetCheck.Mark.LookAgain);
            Transform shown = next == Level3WorksheetCheck.Mark.Confirmed ? markTick[i] : next == Level3WorksheetCheck.Mark.LookAgain ? markAsk[i] : null;
            if (shown == null) continue;
            if (changed && animate) appear.Add(shown);
            else shown.localScale = Vector3.one;
        }
        if (appear.Count == 0) return;
        foreach (Transform mark in appear) mark.localScale = Vector3.zero;
        float total = MarkSeconds + MarkStagger * (appear.Count - 1);
        Run(markMotion, total, t =>
        {
            for (int i = 0; i < appear.Count; i++)
            {
                float u = Mathf.Clamp01((t * total - i * MarkStagger) / MarkSeconds);
                appear[i].localScale = Vector3.one * EaseOutBack(u);
            }
        }, () => { foreach (Transform mark in appear) mark.localScale = Vector3.one; });
    }

    /// <summary>T06: an accepted batch's tabs sink into the rack together (then the rack is empty for the next batch).</summary>
    public void GatherTabs(Action done)
    {
        var shown = new List<Transform>();
        foreach (Transform tab in tabs) if (tab != null && tab.gameObject.activeSelf) shown.Add(tab);
        var from = new List<Vector3>();
        foreach (Transform tab in shown) from.Add(tab.localPosition);
        RefreshSurfaces();
        Run(gatherMotion, GatherSeconds, t =>
        {
            float e = EaseIn(t);
            for (int i = 0; i < shown.Count; i++) shown[i].localPosition = from[i] + new Vector3(0f, -0.014f * e, 0.012f * e);
        }, () =>
        {
            for (int i = 0; i < shown.Count; i++)
            {
                shown[i].localPosition = from[i];
                shown[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < tabRows.Length; i++) tabRows[i] = null;
            SetTabMarks(null, animate: false);
            RefreshSurfaces();
            done?.Invoke();
        });
    }

    /// <summary>T06: the summary's printed front (exact text from the table).</summary>
    public struct SummaryFace
    {
        public string Title;
        public string Income;
        public string Deductions;
    }

    /// <summary>
    /// T06: the folded summary. animate: it comes out of the output mouth closed, lands on the ledge and
    /// opens into its tent (rollers turning; one quiet cue at the start and at the landing). Otherwise it is
    /// simply there (a restore). landed runs once it rests.
    /// </summary>
    public void ShowSummary(SummaryFace face, bool animate, Action landed)
    {
        if (SummaryCard == null) return;
        summaryTitle.text = face.Title ?? "";
        summaryIncomeLabel.text = "Income";
        summaryIncome.text = face.Income ?? "";
        summaryDeductionsLabel.text = Level3WorksheetCheck.SupportedDeductionsLabel;
        summaryDeductions.text = face.Deductions ?? "";
        if (SummaryCard.parent != Rack) Park(SummaryCard, Rack, summaryRest, Quaternion.identity);
        bool appearing = !SummaryCard.gameObject.activeSelf;
        SummaryCard.gameObject.SetActive(true);
        if (!appearing || !animate)
        {
            summaryMotion.Stop();
            SetSummaryPose(1f);
            RefreshSurfaces();
            if (appearing) landed?.Invoke();
            return;
        }
        SetSummaryPose(0f);
        SpinFor(SummaryOutSeconds * 0.6f, 1f);
        Level3BeaTable.Cue("sfx_paper_rustle_01", 0.45f);
        Run(summaryMotion, SummaryOutSeconds, SetSummaryPose, () =>
        {
            SetSummaryPose(1f);
            Level3BeaTable.Cue("sfx_card_place_02", 0.45f);
            RefreshSurfaces();
            landed?.Invoke();
        });
        RefreshSurfaces();
    }

    /// <summary>
    /// The summary's way out (0 = folded flat inside the body behind the plate's lower edge, 1 = open on
    /// the ledge): it slides flat and closed out of the mouth over the ledge, then stands up into its tent
    /// with a small settle. Never scaled: the mouth is as wide as the card.
    /// </summary>
    private void SetSummaryPose(float t)
    {
        Vector3 plateEdge = Rack.InverseTransformPoint(Machine.TransformPoint(new Vector3(BodyOffsetX, PlateBottomY, PlateBottomZ)));
        float ledgeTop = Rack.InverseTransformPoint(OutputLedge.TransformPoint(new Vector3(0f, 0.5f, 0f))).y + 0.002f;
        var inside = new Vector3(summaryRest.x, ledgeTop, plateEdge.z + 0.003f);
        float slide = EaseOut(Mathf.Clamp01(t / 0.55f));
        float stand = Mathf.Clamp01((t - 0.45f) / 0.55f);
        float open = EaseOutBack(stand);
        Vector3 over = new Vector3(summaryRest.x, ledgeTop, Mathf.LerpUnclamped(inside.z, summaryRest.z, slide));
        SummaryCard.localPosition = Vector3.LerpUnclamped(over, summaryRest, EaseInOut(stand));
        summaryFront.localRotation = Quaternion.Euler(-Mathf.LerpUnclamped(0f, SummaryLean, open), 0f, 0f);
        summaryBack.localRotation = Quaternion.Euler(Mathf.LerpUnclamped(180f, summaryBackAngle, open), 0f, 0f);
    }

    /// <summary>T06: the summary leaves the ledge at once (a reader holds the full paper, or it was collected).</summary>
    public void HideSummary()
    {
        if (SummaryCard == null) return;
        summaryMotion.Stop();
        SummaryCard.gameObject.SetActive(false);
        Park(SummaryCard, Rack, summaryRest, Quaternion.identity);
        SetSummaryPose(1f);
        RefreshSurfaces();
    }

    /// <summary>
    /// T06, after a SAVED collection: the folded summary lifts off the ledge and flies to the Case Folder
    /// (target: a world point in front of the HUD icon), shrinking into it; then it is hidden.
    /// </summary>
    public void FlySummary(Vector3 target, Quaternion rotation, float endScale, Action done)
    {
        if (!SummaryShown) { done?.Invoke(); return; }
        summaryMotion.Finish();
        Vector3 start = SummaryCard.position;
        Quaternion from = SummaryCard.rotation;
        float startScale = SummaryCard.lossyScale.x;
        SummaryCard.SetParent(table, true);
        Vector3 rise = table.up * (0.08f * scale);
        RefreshSurfaces();
        Level3BeaTable.Cue("sfx_folder_close_01", 0.7f);
        Run(summaryMotion, CollectFlySeconds, t =>
        {
            float e = EaseInOut(t), u = 1f - e;
            SummaryCard.position = u * u * start + 2f * u * e * (start + rise) + e * e * target;
            SummaryCard.rotation = Quaternion.Slerp(from, rotation, e);
            SetWorldScale(SummaryCard, Mathf.Lerp(startScale, endScale, e * e));
        }, () =>
        {
            HideSummary();
            done?.Invoke();
        });
    }

    /// <summary>The card of the current record (null hides it). animate slides it onto the stand from the stack's side.</summary>
    public void ShowCard(Level3TableStaging.CardFace? face, bool animate)
    {
        if (Card == null) return;
        bool show = face.HasValue && !string.IsNullOrEmpty(face.Value.Title);
        if (!show)
        {
            cardMotion.Stop();
            Card.gameObject.SetActive(false);
            cardPlace = CardPlace.Away;
            armed = false;
            arriveStart = -1f;
            RefreshSurfaces();
            return;
        }
        SetFace(face.Value);
        // Already on the stand: only its face may change (an arrival, the armed lift or a return keep going).
        bool onStand = cardPlace == CardPlace.Stand && Card.gameObject.activeSelf;
        if (!onStand)
        {
            bool arriving = animate && !settling && !ReducedMotion;
            cardMotion.Stop();
            Park(Card, hinge, cardRest, Quaternion.identity);
            cardPlace = CardPlace.Stand;
            armed = false;
            Card.gameObject.SetActive(true);
            arriveStart = arriving ? Time.unscaledTime : -1f;
            Card.localPosition = arriving ? ArriveFrom : cardRest;
        }
        RefreshSurfaces();
    }

    /// <summary>The printed face of the record card (the same view of the row on the stand, half-fed or recalled).</summary>
    public void SetFace(Level3TableStaging.CardFace f)
    {
        position.text = f.Position ?? "";
        title.text = f.Title ?? "";
        amount.text = f.Amount ?? "";
        purpose.text = f.Purpose ?? "";
        // Put the issuer above the date/reference; preserve every source fact
        // at readable size instead of shrinking a long issuer onto one line.
        details.text = TwoLines(f.Details);
    }

    private static string TwoLines(string detail)
    {
        detail = detail ?? "";
        int separator = detail.IndexOf(" · ", StringComparison.Ordinal);
        return separator < 0 ? detail : detail.Substring(0, separator) + "\n" + detail.Substring(separator + 3);
    }

    private Vector3 ArriveFrom => cardRest + new Vector3(-0.06f, 0.04f, 0.05f);

    /// <summary>The table's seated/free state and the seat's event camera: surfaces of the current step take touches only while on.</summary>
    public void SetTouch(bool on, Camera camera)
    {
        touchOn = on;
        if (camera != null) viewCamera = camera;
        foreach (Surface surface in surfaces)
            if (surface.Canvas != null && camera != null) surface.Canvas.worldCamera = camera;
        if (!on) CancelDrag();
        RefreshSurfaces();
    }

    // =========================================================
    // MACHINE
    // =========================================================

    private void BuildMachine(int layer)
    {
        float depth = Level3TableStaging.MachineDepth, w = BodyWidth, h = BodyHeight, bx = BodyOffsetX;
        float bodyDepth = BodyBackZ - BodyFrontZ, bodyZ = (BodyBackZ + BodyFrontZ) * 0.5f;
        props.Box("Plinth", Machine, new Vector3(0f, PlinthHeight * 0.5f, 0f), new Vector3(Level3TableStaging.MachineWidth - 0.01f, PlinthHeight, depth),
            Level3DeskProps.NavyLight);
        props.Box("Body", Machine, new Vector3(bx, PlinthHeight + h * 0.5f, bodyZ), new Vector3(w, h, bodyDepth), Level3DeskProps.Cream);

        // The shallow front plate: from the plinth's front strip (left free for the output ledge) up to the body's top front edge.
        var bottom = new Vector3(bx, PlateBottomY, PlateBottomZ);
        var upper = new Vector3(bx, TopY, BodyFrontZ);
        Vector3 slope = upper - bottom;
        float length = slope.magnitude;
        plate = Node("Front Plate", Machine, (bottom + upper) * 0.5f, Quaternion.LookRotation(slope / length, Vector3.Cross(slope, Vector3.right)));
        props.Box("Plate", plate, Vector3.zero, new Vector3(w * 0.96f, 0.008f, length), Level3DeskProps.NavyLight);
        // A thick slab under the plate closes the wedge between it, the plinth and the body (its far corners sit inside them).
        props.Box("Plate Fill", plate, new Vector3(0f, -0.024f, 0f), new Vector3(w * 0.94f, 0.04f, length * 0.96f), Level3DeskProps.Cream);

        // Three raised keys stay left/centre/right; the larger rotary selector points to the chosen key.
        var dial = new Vector3(0f, 0.006f, DialZ);
        props.Shape(PrimitiveType.Cylinder, "Dial", plate, dial, new Vector3(DialDiameter, 0.004f, DialDiameter), Level3DeskProps.Brass);
        props.Shape(PrimitiveType.Cylinder, "Dial Cap", plate, dial + new Vector3(0f, 0.003f, 0f), new Vector3(0.025f, 0.003f, 0.025f), Level3DeskProps.BrassSoft);
        DialPointer = Node("Dial Pointer", plate, dial + new Vector3(0f, 0.0048f, 0f), Quaternion.Euler(0f, NeutralAngle, 0f));
        props.Box("Pointer", DialPointer, new Vector3(0f, 0f, 0.025f), new Vector3(0.009f, 0.003f, 0.04f), Level3DeskProps.Ink);
        props.Box("Home Mark", plate, dial + new Vector3(0f, 0.0042f, -DialDiameter * 0.5f - 0.004f), new Vector3(0.006f, 0.002f, 0.003f), Level3DeskProps.InkMuted);
        for (int i = 0; i < DialLabels.Length; i++)
        {
            float a = DetentAngles[i] * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            props.Box("Detent " + DialLabels[i], plate, dial + dir * (DialDiameter * 0.5f + 0.004f) + new Vector3(0f, 0.002f, 0f), new Vector3(0.003f, 0.002f, 0.006f),
                Level3DeskProps.BrassSoft);
            var at = new Vector3((i - 1) * DetentSpacing, 0.0045f, LabelRowZ);
            props.Box("Button Socket " + DialLabels[i], plate, at, new Vector3(DialLabelWidth + 0.004f, 0.004f, CategoryButtonHeight + 0.004f), Level3DeskProps.Display);
            Transform button = Node("Category Button " + DialLabels[i], plate, at + new Vector3(0f, 0.004f, 0f), Quaternion.identity);
            categoryButtons[i] = button;
            categoryFaces[i] = props.Box("Button Face", button, Vector3.zero, new Vector3(DialLabelWidth, 0.006f, CategoryButtonHeight), Level3DeskProps.Cream).GetComponent<MeshRenderer>();
            categoryLabels[i] = props.FixedPrint("Dial " + DialLabels[i], button, new Vector3(0f, 0.0035f, 0f), DialLabelWidth, DialLabelHeight, DialLabels[i], Level3DeskProps.Ink, DialLabelPoints,
                TextAlignmentOptions.Center, bold: true);
            detentTouch[i] = AddSurface("Detent Touch " + DialLabels[i], plate, at + new Vector3(0f, 0.009f, 0f), DialLabelWidth + 0.002f, CategoryButtonHeight + 0.002f, Part.Detent, i);
        }
        chosenFrame = props.Frame("Chosen Frame", plate, new Vector3(0f, 0.012f, LabelRowZ), DialLabelWidth + 0.002f, CategoryButtonHeight + 0.002f, 0.002f, Level3DeskProps.Highlight);
        chosenFrame.SetActive(false);
        chosenLabel = props.FixedPrint("Chosen", plate, new Vector3(0f, 0.0045f, ChosenZ), ChosenWidth, ChosenHeight, "", Level3DeskProps.Cream, ChosenPoints,
            TextAlignmentOptions.Center);
        dialTouch = AddSurface("Dial Touch", plate, dial + new Vector3(0f, 0.004f, DialTouchZ - DialZ), DialTouchWidth, DialTouchHeight, Part.Dial, 0);

        // Two compact total windows either side of the dial (display only, no input).
        WindowValues = new TextMeshPro[WindowLabels.Length];
        for (int i = 0; i < WindowLabels.Length; i++)
        {
            var at = new Vector3((i == 0 ? -1f : 1f) * DetentSpacing, 0.0045f, -0.002f);
            props.FixedPrint("Window Label " + WindowLabels[i], plate, at, WindowWidth, WindowLabelHeight, WindowLabels[i], Level3DeskProps.Cream, WindowLabelPoints,
                TextAlignmentOptions.Center, bold: true);
            props.Box("Window " + WindowLabels[i], plate, at + new Vector3(0f, -0.0005f, -0.024f), new Vector3(WindowWidth, 0.003f, 0.027f), Level3DeskProps.Display);
            WindowValues[i] = props.FixedPrint("Window Value " + WindowLabels[i], plate, new Vector3(at.x, WindowTextY, at.z - 0.024f), WindowWidth - 0.006f, WindowValueHeight,
                "—", Level3DeskProps.DisplayInk, WindowValuePoints, TextAlignmentOptions.Right);
        }
        // T06: the output mouth is as wide as the folded summary it issues (the card never shrinks to pass).
        props.Box("Output Mouth", plate, new Vector3(SummaryX - bx, 0.0045f, -length * 0.5f + 0.006f), new Vector3(OutputMouthWidth + 0.006f, 0.002f, 0.004f), Level3DeskProps.Display);
        OutputLedge = props.Box("Output Ledge", Machine, new Vector3(SummaryX, PlinthHeight + 0.004f, -depth * 0.5f + 0.017f), new Vector3(SummaryWidth + 0.01f, 0.008f, 0.03f),
            Level3DeskProps.Cream).transform;
        // T06: each total window opens the read-only list of what it adds (when nothing is loaded).
        for (int i = 0; i < windowTouch.Length; i++)
            windowTouch[i] = AddSurface("Window Touch " + WindowLabels[i], plate, new Vector3((i == 0 ? -1f : 1f) * DetentSpacing, 0.009f, WindowTouchZ),
                WindowTouchWidth, WindowTouchHeight, Part.Window, i);

        // The intake: a dark slot across the top near the back, a lip in front (paper goes behind it), a rail behind, two rollers either side of the paper.
        var slot = new Vector3(bx, TopY, SlotZ);
        IntakeSlot = props.Box("Intake Slot", Machine, slot, new Vector3(SlotWidth, 0.004f, RollerGap * 2f + 0.006f), Level3DeskProps.Display).transform;
        props.Box("Intake Lip", Machine, slot + new Vector3(0f, 0.004f, -RollerGap - 0.009f), new Vector3(SlotWidth + 0.016f, 0.008f, 0.008f), Level3DeskProps.NavyLight);
        props.Box("Intake Rail", Machine, slot + new Vector3(0f, 0.003f, RollerGap + 0.008f), new Vector3(SlotWidth + 0.016f, 0.006f, 0.007f), Level3DeskProps.NavyLight);
        Rollers = new Transform[2];
        rollerRest = new Quaternion[2];
        for (int i = 0; i < 2; i++)
        {
            Rollers[i] = props.Roll("Roller " + (i + 1), Machine, slot + new Vector3(0f, -0.003f, i == 0 ? -RollerGap : RollerGap), SlotWidth - 0.01f, RollerDiameter,
                Level3DeskProps.InkMuted).transform;
            rollerRest[i] = Rollers[i].localRotation;
            // Grip ridges so a turning roller reads as turning.
            for (int r = 0; r < 4; r++)
            {
                float a = r * Mathf.PI * 0.5f;
                props.Box("Ridge " + (r + 1), Rollers[i], new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f), new Vector3(0.12f, 0.9f, 0.12f), Level3DeskProps.Ink);
            }
        }
        FeedFrame = Node("Feed Frame", Machine, slot, Quaternion.Euler(-FeedTilt, 0f, 0f));
        intakeOutline = props.Frame("Intake Outline", Machine, slot + new Vector3(0f, 0.0085f, 0f), SlotWidth + 0.02f, RollerGap * 2f + 0.03f, 0.003f, Level3DeskProps.Highlight);
        intakeOutline.SetActive(false);
        // Generous tap area: the whole top of the body (the paper slot is narrow; the target is not).
        intakeTouch = AddSurface("Intake Touch", Machine, new Vector3(bx - 0.005f, TopY + 0.009f, (BodyFrontZ + BodyBackZ) * 0.5f + 0.002f), IntakeTouchWidth,
            bodyDepth - 0.004f, Part.Intake, 0);

        // The lever on the right side: pivot, shaft and brass handle, raised until a choice is complete.
        float side = bx + w * 0.5f;
        props.Roll("Lever Pivot", Machine, new Vector3(side + 0.007f, LeverPivotY, LeverPivotZ), 0.016f, 0.03f, Level3DeskProps.NavyLight);
        LeverHinge = Node("Lever Hinge", Machine, new Vector3(side + 0.016f, LeverPivotY, LeverPivotZ), Quaternion.Euler(LeverRaised, 0f, 0f));
        props.Box("Lever Shaft", LeverHinge, new Vector3(0f, LeverLength * 0.5f, 0f), new Vector3(0.01f, LeverLength, 0.012f), Level3DeskProps.NavyLight);
        props.Roll("Lever Handle", LeverHinge, new Vector3(0.004f, LeverLength, 0f), 0.034f, 0.03f, Level3DeskProps.Brass);
        // The handle's touch area stands upright around the raised handle, facing the player, clear of the slot and the plate.
        leverTouch = AddSurface("Lever Touch", Machine, new Vector3(LeverTouchX, LeverTouchY, LeverTouchZ), LeverTouchWidth, LeverTouchHeight, Part.Lever, 0, upright: true);
    }

    private void BuildRack(int layer)
    {
        float w = Level3TableStaging.RackWidth, d = Level3TableStaging.RackDepth;
        props.Box("Rack Base", Rack, new Vector3(0f, 0.005f, 0f), new Vector3(w, 0.01f, d), Level3DeskProps.NavyLight);
        props.Box("Rack Rail", Rack, new Vector3(0f, 0.012f, d * 0.5f - 0.004f), new Vector3(w, 0.014f, 0.006f), Level3DeskProps.NavyLight);
        RackSlots = new Transform[3];
        tabRest = new Vector3[3];
        for (int i = 0; i < RackSlots.Length; i++)
        {
            RackSlots[i] = props.Box("Tab Slot " + (i + 1), Rack, new Vector3((i - 1) * w * 0.33f, 0.0105f, -0.003f), new Vector3(w * 0.29f, 0.002f, d * 0.6f),
                Level3DeskProps.Cream).transform;
            tabRest[i] = new Vector3((i - 1) * w * 0.33f, 0.013f, -d * 0.5f + 0.004f);
            Transform tab = Node("Review Tab " + (i + 1), Rack, tabRest[i], Quaternion.Euler(-RackTabTilt, 0f, 0f));
            props.Box("Tab Paper", tab, new Vector3(0f, 0f, RackTabDepth * 0.5f), new Vector3(RackTabWidth, 0.0015f, RackTabDepth), Level3DeskProps.Paper);
            tabTitles[i] = props.FixedPrint("Tab Title", tab, new Vector3(0f, 0.001f, RackTabDepth - 0.004f - RackTitleHeight * 0.5f), RackTitleWidth, RackTitleHeight, "",
                Level3DeskProps.Ink, RackTitlePoints, TextAlignmentOptions.Center, wrap: true);
            tabCategories[i] = props.FixedPrint("Tab Category", tab, new Vector3(0f, 0.001f, 0.006f + RackCategoryHeight * 0.5f), RackTitleWidth, RackCategoryHeight, "",
                Level3DeskProps.Ink, RackCategoryPoints, TextAlignmentOptions.Center, bold: true);
            tabTouch[i] = AddSurface("Tab Touch " + (i + 1), tab, new Vector3(0f, 0.002f, RackTabDepth * 0.5f), ReviewTouchWidth, ReviewTouchHeight, Part.Tab, i);
            BuildMarks(tab, i);
            tabs[i] = tab;
            tab.gameObject.SetActive(false);
        }
        BuildSummary();
    }

    /// <summary>
    /// T06: a tab's review mark beside its category (never only colour): a brass disc with a ticked stroke
    /// (right) or a graphite disc with "?" (look again). Hidden until a saved check marks it.
    /// </summary>
    private void BuildMarks(Transform tab, int slot)
    {
        var at = new Vector3(MarkX, 0.0012f, MarkZ);
        Transform tick = Node("Mark Right", tab, at, Quaternion.identity);
        props.Shape(PrimitiveType.Cylinder, "Disc", tick, Vector3.zero, new Vector3(MarkDiameter, 0.0008f, MarkDiameter), new Color32(79, 163, 112, 255));
        var icon = new ToolkitBuilder(tick.gameObject.layer);
        icon.CheckIcon(tick, new Vector3(0f, 0.001f, 0f), MarkDiameter * 0.66f, props.Lit(Color.white));
        Transform ask = Node("Mark Look Again", tab, at, Quaternion.identity);
        props.Shape(PrimitiveType.Cylinder, "Disc", ask, Vector3.zero, new Vector3(MarkDiameter, 0.0008f, MarkDiameter), Level3DeskProps.NavyLight);
        props.FixedPrint("Ask", ask, new Vector3(0f, 0.001f, 0f), MarkDiameter, MarkDiameter, "?", Level3DeskProps.Cream, MarkPoints, TextAlignmentOptions.Center, bold: true);
        tick.gameObject.SetActive(false);
        ask.gameObject.SetActive(false);
        markTick[slot] = tick;
        markAsk[slot] = ask;
    }

    /// <summary>
    /// T06: the folded summary card (a paper tent): the front leans back toward the seat on the empty rack,
    /// its back leg rests on the rack rail. Front: title, Income, Supported deductions (the table's exact
    /// text). Hidden until the worksheet is ready.
    /// </summary>
    private void BuildSummary()
    {
        summaryRest = new Vector3(SummaryX, 0.01f, SummaryFrontZ);
        SummaryCard = Node("Summary Card", Rack, summaryRest, Quaternion.identity);
        summaryFront = Node("Summary Front", SummaryCard, Vector3.zero, Quaternion.Euler(-SummaryLean, 0f, 0f));
        props.Box("Front Paper", summaryFront, new Vector3(0f, 0.00075f, SummaryHeight * 0.5f), new Vector3(SummaryWidth, 0.0015f, SummaryHeight), Level3DeskProps.Paper);
        props.Box("Fold", summaryFront, new Vector3(0f, 0.0016f, SummaryHeight - 0.0012f), new Vector3(SummaryWidth, 0.0004f, 0.0024f), Level3DeskProps.Cream);
        float z = SummaryHeight - 0.007f;
        summaryTitle = props.FixedPrint("Summary Title", summaryFront, new Vector3(0f, 0.0017f, z - SummaryTitleHeight * 0.5f), SummaryTextWidth, SummaryTitleHeight,
            "", Level3DeskProps.Ink, SummaryTitlePoints, TextAlignmentOptions.Left, bold: true, wrap: true);
        z -= SummaryTitleHeight + 0.005f;
        TextMeshPro Line(string name, float height, float points, Color color, bool bold)
        {
            TextMeshPro label = props.FixedPrint(name, summaryFront, new Vector3(0f, 0.0017f, z - height * 0.5f), SummaryTextWidth, height, "", color, points,
                TextAlignmentOptions.Left, bold);
            z -= height;
            return label;
        }
        summaryIncomeLabel = Line("Income Label", SummaryLabelHeight, SummaryLabelPoints, Level3DeskProps.InkMuted, false);
        summaryIncome = Line("Income Amount", SummaryAmountHeight, SummaryAmountPoints, Level3DeskProps.Ink, true);
        z -= 0.003f;
        summaryDeductionsLabel = Line("Deductions Label", SummaryLabelHeight, SummaryLabelPoints, Level3DeskProps.InkMuted, false);
        summaryDeductions = Line("Deductions Amount", SummaryAmountHeight, SummaryAmountPoints, Level3DeskProps.Ink, true);
        // The back leg from the fold down onto the rack rail (geometry from the lean, so it always meets).
        float lean = SummaryLean * Mathf.Deg2Rad;
        var top = new Vector2(SummaryFrontZ + SummaryHeight * Mathf.Cos(lean), 0.01f + SummaryHeight * Mathf.Sin(lean));
        var rail = new Vector2(Level3TableStaging.RackDepth * 0.5f - 0.004f, 0.0195f);
        float drop = Mathf.Atan2(top.y - rail.y, rail.x - top.x);
        summaryBackLength = Vector2.Distance(top, rail);
        summaryBackAngle = SummaryLean + drop * Mathf.Rad2Deg;
        summaryBack = Node("Summary Back", summaryFront, new Vector3(0f, 0f, SummaryHeight), Quaternion.Euler(summaryBackAngle, 0f, 0f));
        props.Box("Back Paper", summaryBack, new Vector3(0f, -0.00075f, summaryBackLength * 0.5f), new Vector3(SummaryWidth, 0.0015f, summaryBackLength), Level3DeskProps.Cream);
        summaryTouch = AddSurface("Summary Touch", summaryFront, new Vector3(0f, 0.003f, SummaryHeight * 0.5f), SummaryTouchWidth, SummaryTouchHeight, Part.Summary, 0);
        SummaryCard.gameObject.SetActive(false);
    }

    // =========================================================
    // WHAT THE TABLE SHOWS (presentation only; the table decides)
    // =========================================================

    /// <summary>Which surfaces the current machine step offers (set by the table every change; nothing else is live).</summary>
    public struct View
    {
        public bool CardOnStand;
        public bool Loaded;
        public bool Busy;
        public bool Recalled;
        public bool NeedsWhy;
        public bool WhyOpen;
        public bool HasPartner;
        public bool LeverReady;
        public bool TabsLive;
        /// <summary>T06: the total windows open their read-only list (nothing loaded, idle).</summary>
        public bool WindowsLive;
        /// <summary>T06: the folded summary may be opened (it rests on the ledge).</summary>
        public bool SummaryLive;
    }

    private View view;

    public void SetView(View next)
    {
        view = next;
        if (!view.Loaded && WhyUp) CloseWhy(animate: false);
        RefreshSurfaces();
    }

    /// <summary>The note card's content for the loaded record (shown while loaded, hidden otherwise).</summary>
    public void ShowNote(Level3TableStaging.CardFace face, bool needsReason, bool partner, string reasonLabel, bool animate, float delay = 0f, string lookAgain = null)
    {
        needsWhy = needsReason;
        hasPartner = partner;
        noteWhyRow.SetActive(false);
        noteWhyValue.text = string.IsNullOrEmpty(reasonLabel) ? WhyPrompt : reasonLabel;
        // Context rows follow the Why? row when there is one.
        float z = CardHeight * 0.5f - CardTop - (needsReason ? WhyRowHeight + 0.006f : 0.004f);
        notePurpose.text = face.Purpose ?? "";
        notePurpose.transform.localPosition = new Vector3(0f, 0.0012f, z - PurposeHeight * 0.5f);
        z -= PurposeHeight + CardRowGap;
        noteDetails.text = TwoLines(face.Details);
        noteDetails.transform.localPosition = new Vector3(0f, 0.0012f, z - DetailsHeight * 0.5f);
        z -= DetailsHeight + CardRowGap;
        noteHint.text = string.IsNullOrEmpty(lookAgain) ? "" : "Look again: " + lookAgain;
        noteHint.transform.localPosition = new Vector3(0f, 0.0012f, z - NoteHintHeight * 0.5f);
        // INSPECT ORIGINAL fills the row, or shares it with COMPARE.
        float full = CardWidth - 0.016f, half = (full - 0.008f) * 0.5f;
        float inspectWidth = partner ? half : full;
        noteInspectTab.localPosition = new Vector3(partner ? -half * 0.5f - 0.004f : 0f, 0f, noteInspectTab.localPosition.z);
        noteInspectTab.GetChild(0).localScale = new Vector3(inspectWidth, 0.0008f, NoteTabHeight);
        noteInspectLabel.rectTransform.sizeDelta = new Vector2(inspectWidth - 0.008f, NoteTabHeight) / Level3DeskProps.PrintScale;
        // Never shrunk to fit: sharing the row, it reads on two lines at a measured size.
        noteInspectLabel.text = partner ? "INSPECT\nORIGINAL" : InspectLabel;
        noteInspectLabel.fontSize = partner ? NoteSplitPoints : TabPoints;
        noteInspectTouch.Resize(inspectWidth, NoteTabHeight + 0.004f);
        noteCompareTab.SetActive(false);
        noteInspectTab.gameObject.SetActive(false);
        whySubtitle.text = face.Title ?? "";
        bool wasShown = noteShown;
        noteShown = true;
        if (!wasShown)
        {
            // It settles onto the stand once the record card has left it (never both at once).
            Vector3 from = noteRest + new Vector3(0f, 0.012f, -0.03f);
            float total = animate ? NoteSeconds + Mathf.Max(0f, delay) : 0f;
            NoteCard.localPosition = from;
            NoteCard.gameObject.SetActive(!animate);
            Run(noteMotion, total, t =>
            {
                float u = total <= 0f ? 1f : Mathf.Clamp01((t * total - Mathf.Max(0f, delay)) / NoteSeconds);
                if (!NoteCard.gameObject.activeSelf && u > 0f) NoteCard.gameObject.SetActive(true);
                NoteCard.localPosition = Vector3.LerpUnclamped(from, noteRest, EaseOut(u));
            }, () =>
            {
                NoteCard.gameObject.SetActive(true);
                NoteCard.localPosition = noteRest;
                RefreshSurfaces();
            });
        }
        else NoteCard.gameObject.SetActive(true);
        RefreshSurfaces();
    }

    /// <summary>The Why? row appears on the note card only once a treatment is chosen (its space is kept, so nothing jumps).</summary>
    public void SetWhyAvailable(bool on)
    {
        if (noteWhyRow != null && noteWhyRow.activeSelf != (needsWhy && on)) noteWhyRow.SetActive(needsWhy && on);
        RefreshSurfaces();
    }

    public void SetReasonLabel(string reasonLabel)
    {
        if (noteWhyValue != null) noteWhyValue.text = string.IsNullOrEmpty(reasonLabel) ? WhyPrompt : reasonLabel;
    }

    /// <summary>The note card leaves the stand (a short slide down and away when animated).</summary>
    public void HideNote(bool animate = false)
    {
        if (NoteCard == null) return;
        if (WhyUp) CloseWhy(animate: false);
        bool was = noteShown && NoteCard.gameObject.activeSelf;
        noteShown = false;
        RefreshSurfaces();
        if (!was || !animate)
        {
            noteMotion.Stop();
            NoteCard.gameObject.SetActive(false);
            NoteCard.localPosition = noteRest;
            return;
        }
        Vector3 from = NoteCard.localPosition, to = noteRest + new Vector3(0f, 0.012f, -0.03f);
        Run(noteMotion, CardAwaySeconds, t => NoteCard.localPosition = Vector3.LerpUnclamped(from, to, EaseIn(t)), () =>
        {
            NoteCard.gameObject.SetActive(false);
            NoteCard.localPosition = noteRest;
        });
    }

    /// <summary>Lifts the Why? card into the reading position (centred, readable), marking the pending reason.</summary>
    public void OpenWhy(Camera camera, string pendingReason)
    {
        if (WhyCard == null || camera == null) return;
        MarkReason(pendingReason);
        WhyUp = true;
        WhyCard.gameObject.SetActive(true);
        Vector3 fromPosition = WhyCard.position;
        Quaternion fromRotation = WhyCard.rotation;
        float fromScale = WhyCard.lossyScale.x;
        if (WhyCard.parent != table)
        {
            Park(WhyCard, hinge, whyRest, Quaternion.identity);
            fromPosition = WhyCard.position;
            fromRotation = WhyCard.rotation;
            fromScale = WhyCard.lossyScale.x;
            WhyCard.SetParent(table, true);
        }
        ReaderPose(camera, CardWidth, WhyCardHeight, out Vector3 toPosition, out Quaternion toRotation, out float toScale);
        Run(whyMotion, WhyLiftSeconds, t =>
        {
            float e = EaseInOut(t);
            WhyCard.SetPositionAndRotation(Vector3.LerpUnclamped(fromPosition, toPosition, e), Quaternion.Slerp(fromRotation, toRotation, e));
            SetWorldScale(WhyCard, Mathf.Lerp(fromScale, toScale, e));
        }, null);
        RefreshSurfaces();
    }

    public void MarkReason(string reason)
    {
        for (int i = 0; i < optionFrames.Length; i++)
            if (optionFrames[i] != null) optionFrames[i].SetActive(i < Level3WorksheetRules.Reasons.Length && Level3WorksheetRules.Reasons[i] == reason);
    }

    /// <summary>The Why? card goes back down onto the stand and out of sight.</summary>
    public void CloseWhy(bool animate)
    {
        if (WhyCard == null || !WhyUp) return;
        WhyUp = false;
        Vector3 fromPosition = WhyCard.position;
        Quaternion fromRotation = WhyCard.rotation;
        float fromScale = WhyCard.lossyScale.x;
        Vector3 toPosition = hinge.TransformPoint(whyRest);
        Quaternion toRotation = hinge.rotation;
        float toScale = hinge.lossyScale.x;
        Run(whyMotion, animate ? ReturnSeconds : 0f, t =>
        {
            float e = EaseInOut(t);
            WhyCard.SetPositionAndRotation(Vector3.LerpUnclamped(fromPosition, toPosition, e), Quaternion.Slerp(fromRotation, toRotation, e));
            SetWorldScale(WhyCard, Mathf.Lerp(fromScale, toScale, e));
        }, () =>
        {
            Park(WhyCard, hinge, whyRest, Quaternion.identity);
            WhyCard.gameObject.SetActive(false);
        });
        RefreshSurfaces();
    }

    /// <summary>The selector: -1 = neutral home. Animated detents settle with weight; a quiet restore or a new record just sets it. loaded shows "Chosen: —" at home.</summary>
    public void SetSelector(int index, bool animate, bool loaded)
    {
        selectedDetent = index >= 0 && index < DetentAngles.Length ? index : -1;
        float target = selectedDetent >= 0 ? DetentAngles[selectedDetent] : NeutralAngle;
        chosenFrame.SetActive(selectedDetent >= 0);
        if (selectedDetent >= 0) chosenFrame.transform.localPosition = new Vector3((selectedDetent - 1) * DetentSpacing, 0.012f, LabelRowZ);
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            bool selected = i == selectedDetent;
            categoryFaces[i].sharedMaterial = props.Lit(selected ? Level3DeskProps.Highlight : Level3DeskProps.Cream);
            categoryLabels[i].color = Level3DeskProps.Ink;
        }
        chosenLabel.text = selectedDetent >= 0 ? "Chosen: " + DialLabels[selectedDetent] : loaded ? "Chosen: —" : "";
        // A held dial follows the finger continuously; category commits only update its indication.
        if (drag == DragKind.Dial) return;
        float from = dialAngle;
        // The shorter way round (the home is opposite the middle detent).
        float delta = Mathf.DeltaAngle(from, target);
        Run(dialMotion, animate ? DetentSeconds : 0f, t =>
        {
            dialAngle = from + delta * EaseOut(t);
            DialPointer.localRotation = Quaternion.Euler(0f, dialAngle, 0f);
        }, () =>
        {
            dialAngle = target;
            DialPointer.localRotation = Quaternion.Euler(0f, target, 0f);
        });
    }

    /// <summary>The two windows (exact text from the saved draft). A changed value gives the same short neutral tick, up or down.</summary>
    public void SetTotals(string income, string deductions, bool animate)
    {
        if (WindowValues == null) return;
        bool changed = WindowValues[0].text != income || WindowValues[1].text != deductions;
        var flags = new[] { WindowValues[0].text != income, WindowValues[1].text != deductions };
        WindowValues[0].text = income;
        WindowValues[1].text = deductions;
        if (!changed || !animate) return;
        Run(windowMotion, TabSettleSeconds, t =>
        {
            float lift = Mathf.Sin(t * Mathf.PI) * 0.0015f;
            for (int i = 0; i < 2; i++)
                if (flags[i]) WindowValues[i].transform.localPosition = new Vector3(WindowValues[i].transform.localPosition.x, WindowTextY + lift,
                    WindowValues[i].transform.localPosition.z);
        }, () =>
        {
            for (int i = 0; i < 2; i++)
                WindowValues[i].transform.localPosition = new Vector3(WindowValues[i].transform.localPosition.x, WindowTextY, WindowValues[i].transform.localPosition.z);
        });
    }

    /// <summary>
    /// The review rack: one tab per filed row of the open batch (title and chosen category, same
    /// look for every choice). hiddenRow (a recalled record in the machine) has no tab meanwhile;
    /// settleRow is the tab that just arrived after its feed (a short settle; others stay still).
    /// </summary>
    public void SetTabs(IList<(string row, string title, string category)> filed, string hiddenRow, string settleRow, bool animate)
    {
        Transform settling = null;
        var show = new bool[tabs.Length];
        for (int i = 0; i < tabs.Length; i++) tabRows[i] = null;
        if (filed != null)
            foreach ((string row, string tabTitle, string category) in filed)
            {
                // Each record keeps its own slot (its place in the batch), so tabs never shuffle; a recalled
                // record's slot stays empty while it is in the machine.
                int slot = -1;
                IReadOnlyList<string> batch = Level3WorksheetRules.RowsInBatch(Level3WorksheetRules.BatchOf(row));
                for (int i = 0; i < batch.Count; i++) if (batch[i] == row) slot = i;
                if (slot < 0 || slot >= tabs.Length || row == hiddenRow) continue;
                tabRows[slot] = row;
                if (tabTitles[slot].text != (tabTitle ?? "")) tabTitles[slot].text = tabTitle ?? "";
                if (tabCategories[slot].text != (category ?? "")) tabCategories[slot].text = category ?? "";
                show[slot] = true;
                if (row == settleRow) settling = tabs[slot];
            }
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i].gameObject.activeSelf != show[i]) tabs[i].gameObject.SetActive(show[i]);
            if (!show[i] || tabs[i] != settling) continue;
            tabs[i].localPosition = tabRest[i];
        }
        if (settling != null && animate)
        {
            Transform tab = settling;
            Vector3 rest = tab.localPosition;
            Run(tabMotion, TabSettleSeconds, t => tab.localPosition = rest + new Vector3(0f, -0.012f * (1f - EaseOut(t)), 0.006f * (1f - EaseOut(t))),
                () => tab.localPosition = rest);
        }
        RefreshSurfaces();
    }

    /// <summary>The row in a rack slot (null when that slot is empty).</summary>
    public string TabRow(int slot) => slot >= 0 && slot < tabRows.Length ? tabRows[slot] : null;

    // =========================================================
    // CARD MOTIONS (cosmetic; the table has already decided and saved)
    // =========================================================

    /// <summary>The stand's card lifts a little (armed: tap the slot to feed) or settles back.</summary>
    public void Arm(bool on)
    {
        if (cardPlace != CardPlace.Stand || armed == on) { armed = on && cardPlace == CardPlace.Stand; RefreshSurfaces(); return; }
        armed = on;
        Vector3 from = Card.localPosition, to = on ? cardRest + new Vector3(0f, 0.008f, 0.004f) : cardRest;
        arriveStart = -1f;
        Run(cardMotion, ArmSeconds, t => Card.localPosition = Vector3.LerpUnclamped(from, to, EaseOut(t)), () => Card.localPosition = to);
        intakeOutline.SetActive(on);
        RefreshSurfaces();
    }

    /// <summary>Half-feed: from the stand (lifted over the body, then down the slot) or from a drop over the slot (slides straight in).</summary>
    public void HalfFeed(Action done)
    {
        Card.gameObject.SetActive(true);
        bool fromStand = cardPlace == CardPlace.Stand || cardPlace == CardPlace.Away;
        Pose start = WorldPose(Card);
        float startScale = Card.lossyScale.x;
        Pose end = FeedPose(0f, HalfFedExposed);
        Pose hover = FeedPose(0f, HoverExposed);
        Vector3 axis = FeedFrame.rotation * Vector3.forward;
        Vector3 up = table.up * (0.14f * scale);
        Vector3 c1 = fromStand ? start.position + up : start.position + (hover.position - start.position) * 0.5f;
        Vector3 c2 = fromStand ? hover.position + axis * (0.06f * scale) : hover.position;
        void Loaded()
        {
            Park(Card, FeedFrame, end.localPosition, end.localRotation);
            Spin(0f);
            done?.Invoke();
        }
        if (fromStand)
        {
            Fly(start, startScale, c1, c2, end, HalfFeedSeconds, CardPlace.Loaded, Loaded);
            SpinFor(HalfFeedSeconds * 0.45f, 1f);
        }
        else
        {
            // A real alignment waypoint keeps the card clear of the casing before it enters the slot.
            Fly(start, startScale, start.position + table.up * (0.025f * scale), hover.position + axis * (0.025f * scale),
                hover, DragAlignSeconds, CardPlace.Flying, () =>
                {
                    Park(Card, FeedFrame, hover.localPosition, hover.localRotation);
                    SpinFor(SlideInSeconds, 1f);
                    Run(cardMotion, SlideInSeconds, t =>
                    {
                        Card.localPosition = Vector3.Lerp(hover.localPosition, end.localPosition, EaseInOut(t));
                    }, () =>
                    {
                        cardPlace = CardPlace.Loaded;
                        Loaded();
                        RefreshSurfaces();
                    });
                });
        }
        intakeOutline.SetActive(false);
        armed = false;
        RefreshSurfaces();
    }

    /// <summary>Back to the stand from wherever the card is (a missed drop, backing out): up over the body, then down onto the stand.</summary>
    public void ReturnToStand(Action done)
    {
        Card.gameObject.SetActive(true);
        Pose start = WorldPose(Card);
        float startScale = Card.lossyScale.x;
        Pose end = new Pose { localPosition = cardRest, localRotation = Quaternion.identity, position = hinge.TransformPoint(cardRest), rotation = hinge.rotation };
        Vector3 up = table.up * (0.14f * scale);
        bool fromSlot = cardPlace == CardPlace.Loaded;
        Vector3 c1 = fromSlot ? FeedPose(0f, HoverExposed).position + (FeedFrame.rotation * Vector3.forward) * (0.04f * scale) : start.position + up * 0.4f;
        Vector3 c2 = end.position + up;
        Fly(start, startScale, c1, c2, end, cardPlace == CardPlace.Dragging ? DragReturnSeconds : ReturnSeconds, CardPlace.Stand, () =>
        {
            Park(Card, hinge, cardRest, Quaternion.identity);
            done?.Invoke();
        });
        if (fromSlot) SpinFor(ReturnSeconds * 0.35f, -1f);
        intakeOutline.SetActive(false);
        armed = false;
        RefreshSurfaces();
    }

    /// <summary>
    /// After a SAVED filing (or a cancelled recall): the rollers turn and the card travels fully behind the
    /// lip, pivoting upright, then it is out of sight. Identical for every choice.
    /// </summary>
    public void FeedIn(Action done)
    {
        Card.gameObject.SetActive(true);
        if (cardPlace != CardPlace.Loaded) Park(Card, FeedFrame, FeedPose(0f, HalfFedExposed).localPosition, Quaternion.identity);
        cardPlace = CardPlace.Inside;
        RefreshSurfaces();
        SpinFor(FullFeedSeconds, 1f);
        Run(cardMotion, FullFeedSeconds, t =>
        {
            float e = EaseInOut(t);
            Pose p = FeedPose((FedTilt - FeedTilt) * e, Mathf.Lerp(HalfFedExposed, FedExposed, e));
            Card.localPosition = p.localPosition;
            Card.localRotation = p.localRotation;
        }, () =>
        {
            Pose p = FeedPose(FedTilt - FeedTilt, FedExposed);
            Park(Card, FeedFrame, p.localPosition, p.localRotation);
            Card.gameObject.SetActive(false);
            Spin(0f);
            done?.Invoke();
        });
    }

    /// <summary>Recall: the stand's card (if any) slides off toward the stack; the recalled record's card comes back up out of the slot, half-fed.</summary>
    public void Emerge(Level3TableStaging.CardFace face, Action done)
    {
        void Up()
        {
            SetFace(face);
            Pose fed = FeedPose(FedTilt - FeedTilt, FedExposed);
            Park(Card, FeedFrame, fed.localPosition, fed.localRotation);
            Card.gameObject.SetActive(true);
            cardPlace = CardPlace.Loaded;
            RefreshSurfaces();
            SpinFor(EmergeSeconds, -1f);
            Run(cardMotion, EmergeSeconds, t =>
            {
                float e = EaseOut(t);
                Pose p = FeedPose((FedTilt - FeedTilt) * (1f - e), Mathf.Lerp(FedExposed, HalfFedExposed, e));
                Card.localPosition = p.localPosition;
                Card.localRotation = p.localRotation;
            }, () =>
            {
                Pose p = FeedPose(0f, HalfFedExposed);
                Park(Card, FeedFrame, p.localPosition, p.localRotation);
                done?.Invoke();
            });
        }
        if (Card.gameObject.activeSelf && cardPlace == CardPlace.Stand)
        {
            Vector3 from = Card.localPosition, away = ArriveFrom;
            cardPlace = CardPlace.Away;
            armed = false;
            intakeOutline.SetActive(false);
            RefreshSurfaces();
            Run(cardMotion, CardAwaySeconds, t => Card.localPosition = Vector3.LerpUnclamped(from, away, EaseIn(t)), () =>
            {
                Card.gameObject.SetActive(false);
                Up();
            });
        }
        else Up();
    }

    // =========================================================
    // LEVER
    // =========================================================

    /// <summary>A tapped (or released) lever completes its stroke after the save; then it returns while the paper feeds.</summary>
    public void LeverStroke()
    {
        float from = leverAngle;
        float down = Mathf.Max(0.04f, LeverDownSeconds * (1f - Progress(from)));
        Run(leverMotion, down, t => SetLever(Mathf.Lerp(from, LeverPulled, EaseIn(t))), () =>
        {
            SetLever(LeverPulled);
            Level3BeaTable.Cue("sfx_stamp_return_01", 0.45f);
            Run(leverMotion, LeverReturnSeconds, t => SetLever(Mathf.Lerp(LeverPulled, LeverRaised, EaseOut(t))), () => SetLever(LeverRaised));
        });
    }

    /// <summary>A pull that did not file (below the threshold, a missing choice, a failed save): the lever springs back up.</summary>
    public void LeverBack()
    {
        float from = leverAngle;
        Run(leverMotion, LeverReturnSeconds, t => SetLever(Mathf.Lerp(from, LeverRaised, EaseOut(t))), () => SetLever(LeverRaised));
    }

    /// <summary>A tap while a choice is missing: a small give and return, no sound (the prompt says what is missing).</summary>
    public void LeverResist()
    {
        float from = leverAngle;
        Run(leverMotion, LeverReturnSeconds, t => SetLever(Mathf.Lerp(from, LeverRaised, EaseOut(t)) - Mathf.Sin(t * Mathf.PI) * 6f), () => SetLever(LeverRaised));
    }

    private void SetLever(float angle)
    {
        leverAngle = angle;
        if (LeverHinge != null) LeverHinge.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    private static float Progress(float angle) => Mathf.Clamp01((angle - LeverRaised) / (LeverPulled - LeverRaised));

    // =========================================================
    // TOUCH
    // =========================================================

    private sealed class Surface
    {
        public RectTransform Rect;
        public Canvas Canvas;
        public Level3TablePaperRaycaster Raycaster;
        public Level3MachineTouch Touch;
        public Part Part;
        public int Index;
        public Vector3 Position;
        public Quaternion Rotation;

        public void Resize(float width, float height)
        {
            var units = new Vector2(1000f, 1000f * height / width);
            Rect.localScale = Vector3.one * (width / units.x);
            Rect.sizeDelta = units;
            Raycaster?.BindPlane(Rect.parent, Position, Rotation, width, height);
        }
    }

    private Surface AddSurface(string name, Transform parent, Vector3 localPosition, float width, float height, Part part, int index, bool upright = false)
    {
        RectTransform hit = HudTheme.CreateRect(name, parent);
        hit.gameObject.layer = parent.gameObject.layer;
        var surface = new Surface { Rect = hit, Part = part, Index = index };
        surface.Canvas = hit.gameObject.AddComponent<Canvas>();
        surface.Canvas.renderMode = RenderMode.WorldSpace;
        hit.anchorMin = hit.anchorMax = hit.pivot = new Vector2(0.5f, 0.5f);
        hit.localPosition = localPosition;
        // Apply the final physical pose AFTER Canvas initialization.
        hit.localRotation = upright ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.Euler(90f, 0f, 0f);
        surface.Position = localPosition;
        surface.Rotation = hit.localRotation;
        surface.Raycaster = hit.gameObject.AddComponent<Level3TablePaperRaycaster>();
        surface.Resize(width, height);
        var touch = hit.gameObject.AddComponent<Level3MachineTouch>();
        surface.Touch = touch;
        touch.Model = this;
        touch.Part = (int)part;
        touch.Index = index;
        surface.Raycaster.enabled = false;
        surfaces.Add(surface);
        return surface;
    }

    /// <summary>
    /// Exactly the surfaces of the current step take touches (while seated and free): the full stand card
    /// (including its Inspect tap region); the slot to feed it; the loaded card's header,
    /// selector, lever and note card while loaded; the Why? options only while that card is up (then
    /// nothing else); tabs when nothing is loaded. Nothing during a save/feed or a drag of another part.
    /// </summary>
    private void RefreshSurfaces()
    {
        if (surfaces.Count == 0) return;
        bool on = touchOn;
        bool stand = on && !view.Busy && !WhyUp && cardPlace == CardPlace.Stand && Card.gameObject.activeSelf && view.CardOnStand;
        bool loaded = on && !view.Busy && !WhyUp && view.Loaded && cardPlace == CardPlace.Loaded && !cardMotion.Running;
        bool dragCard = drag == DragKind.Card;
        Live(cardTouch, stand || dragCard);
        Live(intakeTouch, stand && !dragCard);
        Live(headerTouch, loaded);
        Live(dialTouch, loaded || drag == DragKind.Dial);
        for (int i = 0; i < detentTouch.Length; i++) Live(detentTouch[i], loaded && drag == DragKind.None);
        Live(leverTouch, loaded || drag == DragKind.Lever);
        bool note = loaded && noteShown && drag == DragKind.None;
        Live(whyTouch, note && needsWhy && view.NeedsWhy);
        Live(noteInspectTouch, false);
        Live(noteCompareTouch, false);
        bool why = on && WhyUp && !whyMotion.Running;
        for (int i = 0; i < optionTouch.Length; i++) Live(optionTouch[i], why);
        Live(whyCloseTouch, why);
        bool review = markMotion.Running || gatherMotion.Running;
        bool tabsLive = on && view.TabsLive && !view.Busy && !view.Loaded && !WhyUp && drag == DragKind.None && !cardMotion.Running && !review;
        for (int i = 0; i < tabTouch.Length; i++) Live(tabTouch[i], tabsLive && tabRows[i] != null);
        // T06: the batch card's one key, the two windows (read-only list) and the folded summary.
        bool idle = on && !view.Busy && !view.Loaded && !WhyUp && drag == DragKind.None;
        Live(batchKeyTouch, idle && BatchCardShown && batchKeyLive && !batchMotion.Running && !review && !summaryMotion.Running);
        for (int i = 0; i < windowTouch.Length; i++) Live(windowTouch[i], idle && view.WindowsLive && !review && !summaryMotion.Running);
        Live(summaryTouch, idle && view.SummaryLive && SummaryShown && !summaryMotion.Running && SummaryCard.parent == Rack);
    }

    private static void Live(Surface surface, bool on)
    {
        if (surface?.Raycaster != null && surface.Raycaster.enabled != on) surface.Raycaster.enabled = on;
        if (!on) surface?.Touch?.SetHighlighted(false);
    }

    internal bool InputAllowed => touchOn && !SideQuestSplash.IsShowing && !DialogueUI.IsAnyDialogueActive && !InGameSettings.IsOpen && !HudRewardScreen.AnyOpen;

    internal void Clicked(int partIndex, int index, Vector2 screen)
    {
        if (!InputAllowed) return;
        var part = (Part)partIndex;
        MobileInputGuard.SuppressWorldTapsThisFrame();
        switch (part)
        {
            case Part.Card: input?.Invoke(CardTapAt(screen), 0); break;
            case Part.Inspect: case Part.NoteInspect: input?.Invoke(Level3MachineInput.Inspect, 0); break;
            case Part.NoteCompare: input?.Invoke(Level3MachineInput.Compare, 0); break;
            case Part.Intake: input?.Invoke(Level3MachineInput.Intake, 0); break;
            case Part.Header: input?.Invoke(Level3MachineInput.Header, 0); break;
            case Part.Detent: input?.Invoke(Level3MachineInput.Detent, index); break;
            case Part.Dial: break;
            case Part.Lever: input?.Invoke(Level3MachineInput.LeverTapped, 0); break;
            case Part.Why: input?.Invoke(Level3MachineInput.Why, 0); break;
            case Part.WhyOption: input?.Invoke(Level3MachineInput.WhyOption, index); break;
            case Part.WhyClose: input?.Invoke(Level3MachineInput.WhyClose, 0); break;
            case Part.Tab: input?.Invoke(Level3MachineInput.Tab, index); break;
            case Part.BatchKey: input?.Invoke(Level3MachineInput.BatchKey, 0); break;
            case Part.Window: input?.Invoke(Level3MachineInput.Window, index); break;
            case Part.Summary: input?.Invoke(Level3MachineInput.Summary, 0); break;
        }
    }

    public static Level3MachineInput StandCardAction(float paperZ) =>
        Level3MachineInput.Card;

    private Level3MachineInput CardTapAt(Vector2 screen)
    {
        if (viewCamera == null) return Level3MachineInput.Card;
        Ray ray = viewCamera.ScreenPointToRay(screen);
        var plane = new Plane(Card.up, Card.position);
        return plane.Raycast(ray, out float distance)
            ? StandCardAction(Card.InverseTransformPoint(ray.GetPoint(distance)).z) : Level3MachineInput.Card;
    }

    internal void BeginDrag(Level3MachineTouch owner, int partIndex, PointerEventData e)
    {
        if (!InputAllowed || drag != DragKind.None || e == null) return;
        var part = (Part)partIndex;
        Camera camera = viewCamera;
        if (camera == null) return;
        dragStart = e.position;
        dragOwner = owner;
        switch (part)
        {
            case Part.Card:
                if (cardPlace != CardPlace.Stand || view.Busy) return;
                StartCardDrag(e.pressPosition, extract: false);
                DragTarget(e.position);
                Level3BeaTable.Cue("sfx_card_pickup_01", 0.6f);
                break;
            case Part.Header:
                // The table decides (a new record backs out; a recalled one does not drag); it calls PullOut.
                input?.Invoke(Level3MachineInput.HeaderDragged, 0);
                break;
            case Part.Dial:
                drag = DragKind.Dial;
                dialMotion.Stop();
                dialDragAngle = dialAngle;
                DialOffset(e.pressPosition, out dialLastOffset);
                break;
            case Part.Lever:
                drag = DragKind.Lever;
                leverResists = !view.LeverReady;
                leverMotion.Stop();
                leverRaisedScreen = camera.WorldToScreenPoint(HandleAt(LeverRaised));
                leverPulledScreen = camera.WorldToScreenPoint(HandleAt(LeverPulled));
                leverProgress = Progress(leverAngle);
                leverStartProgress = leverProgress;
                break;
        }
        RefreshSurfaces();
    }

    /// <summary>The table let the loaded card be pulled back out by its header: the same drag continues from the slot (up along it first).</summary>
    public void PullOut()
    {
        if (cardPlace != CardPlace.Loaded || drag != DragKind.None) return;
        StartCardDrag(dragStart, extract: true);
    }

    private void StartCardDrag(Vector2 screen, bool extract)
    {
        cardMotion.Stop();
        arriveStart = -1f;
        pickupPose = WorldPose(Card);
        pickupScale = Card.lossyScale.x;
        pickupStart = Time.unscaledTime;
        drag = DragKind.Card;
        intakeOutline.SetActive(true);
        Card.SetParent(table, true);
        cardPlace = CardPlace.Dragging;
        dragRotation = Card.rotation;
        Camera camera = viewCamera;
        if (camera == null) return;
        Vector3 pressedCentre = camera.WorldToScreenPoint(Card.position);
        dragGrabOffset = new Vector2(pressedCentre.x, pressedCentre.y) - screen;
        dragDepth = camera.nearClipPlane + 0.08f;
        // Keep its projected size while lifting onto a camera-facing plane in front of the props.
        dragScale = Card.lossyScale.x * dragDepth / Mathf.Max(camera.nearClipPlane, pressedCentre.z);
        if (extract) SpinFor(ExtractSeconds, -1f);
        DragTarget(screen);
        RefreshSurfaces();
    }

    internal void Dragged(Level3MachineTouch owner, PointerEventData e)
    {
        if (e == null || owner != dragOwner) return;
        switch (drag)
        {
            case DragKind.Card:
                DragTarget(e.position);
                break;
            case DragKind.Dial:
            {
                if (DialOffset(e.position, out Vector2 offset))
                {
                    float nextAngle = RotatePointer(dialDragAngle, dialLastOffset, offset);
                    if (nextAngle != dialDragAngle)
                    {
                        dialDragAngle = nextAngle;
                        int next = DetentForAngle(dialDragAngle, selectedDetent);
                        if (next != selectedDetent) input?.Invoke(Level3MachineInput.Detent, next);
                    }
                    if (offset.sqrMagnitude >= DialDeadRadius * DialDeadRadius) dialLastOffset = offset;
                }
                break;
            }
            case DragKind.Lever:
            {
                Vector2 travel = leverPulledScreen - leverRaisedScreen;
                float p = travel.sqrMagnitude < 1f ? 0f : Mathf.Clamp01(Vector2.Dot(e.position - dragStart, travel) / travel.sqrMagnitude + leverStartProgress);
                // A missing choice: the lever gives a little and no further.
                leverProgress = leverResists ? Mathf.Min(p, 0.12f) : p;
                leverMotion.Stop();
                break;
            }
        }
    }

    internal void EndDrag(Level3MachineTouch owner, PointerEventData e)
    {
        if (owner != dragOwner) return;
        if (e != null && (drag == DragKind.Dial || drag == DragKind.Lever)) Dragged(owner, e);
        dragOwner = null;
        DragKind ended = drag;
        drag = DragKind.None;
        pickupStart = -1f;
        switch (ended)
        {
            case DragKind.Card:
                if (e != null) DragTarget(e.position);
                intakeOutline.SetActive(false);
                bool over = overIntake;
                overIntake = false;
                RefreshSurfaces();
                input?.Invoke(over ? Level3MachineInput.CardDropped : Level3MachineInput.CardDropMissed, 0);
                break;
            case DragKind.Dial:
                SetSelector(selectedDetent, animate: true, loaded: view.Loaded);
                RefreshSurfaces();
                break;
            case DragKind.Lever:
                RefreshSurfaces();
                if (leverResists) { LeverBack(); input?.Invoke(Level3MachineInput.LeverTapped, 0); }
                else if (leverProgress >= LeverThreshold) input?.Invoke(Level3MachineInput.LeverPulled, 0);
                else LeverBack();
                break;
            default:
                RefreshSurfaces();
                break;
        }
        MobileInputGuard.SuppressWorldTapsThisFrame();
    }

    /// <summary>Settings, a conversation, focus loss, the camera moving: a drag in progress ends with nothing decided (card back where it was, lever up).</summary>
    public void CancelDrag()
    {
        dragOwner = null;
        DragKind ended = drag;
        drag = DragKind.None;
        pickupStart = -1f;
        overIntake = false;
        if (ended == DragKind.Card)
        {
            intakeOutline.SetActive(false);
            input?.Invoke(Level3MachineInput.CardDropMissed, 0);
        }
        else if (ended == DragKind.Lever) SetLever(LeverRaised);
        else if (ended == DragKind.Dial) SetSelector(selectedDetent, animate: false, loaded: view.Loaded);
        RefreshSurfaces();
    }

    /// <summary>A surface was switched off: only the drag it owns (if any) ends undecided.</summary>
    internal void OwnerDisabled(Level3MachineTouch owner)
    {
        if (owner != null && owner == dragOwner) CancelDrag();
    }

    /// <summary>Carry anywhere on a camera-facing plane; the intake previews acceptance without stealing the finger. Release owns insertion.</summary>
    private void DragTarget(Vector2 screen)
    {
        Camera camera = viewCamera;
        if (camera == null) return;
        Ray ray = camera.ScreenPointToRay(screen);
        overIntake = OverIntake(ray);
        Rect pixels = camera.pixelRect;
        float visibleHeight = 2f * dragDepth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float pxPerMetre = pixels.height / Mathf.Max(0.001f, visibleHeight);
        float padX = Mathf.Min(pixels.width * 0.45f, CardWidth * dragScale * pxPerMetre * 0.5f + 8f);
        float padY = Mathf.Min(pixels.height * 0.45f, CardHeight * dragScale * pxPerMetre * 0.5f + 8f);
        float x = Mathf.Clamp(screen.x + dragGrabOffset.x, pixels.xMin + padX, pixels.xMax - padX);
        float y = Mathf.Clamp(screen.y + dragGrabOffset.y, pixels.yMin + padY, pixels.yMax - padY);
        dragTarget = camera.ScreenToWorldPoint(new Vector3(x, y, dragDepth));
        dragRotation = Quaternion.LookRotation(camera.transform.up, -camera.transform.forward);
        if (intakeOutline != null) intakeOutline.transform.localScale = overIntake ? new Vector3(1.04f, 1f, 1.15f) : Vector3.one;
    }

    /// <summary>A generous drop area around the slot: the body's top, extended forward, sideways and back over where a half-fed card stands.</summary>
    private bool OverIntake(Ray ray)
    {
        Matrix4x4 toLocal = Machine.worldToLocalMatrix;
        Vector3 origin = toLocal.MultiplyPoint3x4(ray.origin), direction = toLocal.MultiplyVector(ray.direction);
        if (Mathf.Abs(direction.y) < 1e-6f) return false;
        float at = (TopY - origin.y) / direction.y;
        if (at < 0f) return false;
        Vector3 p = origin + direction * at;
        // Over any part of the machine (lever side and plate included) the card aligns above the slot, so a carried
        // card never passes through the lever or the body on its way in.
        return p.x >= BodyOffsetX - BodyWidth * 0.5f - 0.04f && p.x <= Level3TableStaging.MachineWidth * 0.5f + 0.04f &&
               p.z >= PlateBottomZ - 0.04f && p.z <= BodyBackZ + 0.12f;
    }

    /// <summary>Plate-plane pointer angle; the centre hub and rear mechanical stop retain the last angle.</summary>
    public static float PointerAngle(Vector2 offset, float previous)
    {
        if (offset.y < 0f || offset.sqrMagnitude < DialDeadRadius * DialDeadRadius) return previous;
        float angle = Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg;
        return Mathf.Clamp(angle, DetentAngles[0], DetentAngles[2]);
    }

    /// <summary>A grabbed rim turns relatively, so crossing atan2's rear seam cannot jump across the dial.</summary>
    public static float RotatePointer(float previous, Vector2 from, Vector2 to)
    {
        if (to.sqrMagnitude < DialDeadRadius * DialDeadRadius) return previous;
        if (from.sqrMagnitude < DialDeadRadius * DialDeadRadius)
            return PointerAngle(new Vector2(to.x, Mathf.Max(0f, to.y)), previous);
        float before = Mathf.Atan2(from.x, from.y) * Mathf.Rad2Deg;
        float after = Mathf.Atan2(to.x, to.y) * Mathf.Rad2Deg;
        float current = previous == NeutralAngle ? 0f : previous;
        return Mathf.Clamp(current + Mathf.DeltaAngle(before, after), DetentAngles[0], DetentAngles[2]);
    }

    /// <summary>Nearest detent with a small boundary dead band. Every category uses the same rule.</summary>
    public static int DetentForAngle(float angle, int previous)
    {
        if (previous >= 0 && previous < DetentAngles.Length)
        {
            float low = previous == 0 ? float.NegativeInfinity : (DetentAngles[previous - 1] + DetentAngles[previous]) * 0.5f - DialHysteresis;
            float high = previous == 2 ? float.PositiveInfinity : (DetentAngles[previous] + DetentAngles[previous + 1]) * 0.5f + DialHysteresis;
            if (angle >= low && angle <= high) return previous;
        }
        return angle < DetentAngles[0] * 0.5f ? 0 : angle > DetentAngles[2] * 0.5f ? 2 : 1;
    }

    private bool DialOffset(Vector2 screen, out Vector2 offset)
    {
        offset = Vector2.zero;
        if (viewCamera == null) return false;
        var plane = new Plane(plate.up, DialPointer.position);
        Ray ray = viewCamera.ScreenPointToRay(screen);
        if (!plane.Raycast(ray, out float distance)) return false;
        Vector3 local = plate.InverseTransformPoint(ray.GetPoint(distance));
        offset = new Vector2(local.x, local.z - DialZ);
        return true;
    }

    private Vector3 HandleAt(float angle) => LeverHinge.parent.TransformPoint(LeverHinge.localPosition + Quaternion.Euler(angle, 0f, 0f) * new Vector3(0f, LeverLength, 0f));

    // =========================================================
    // TICK AND SETTLE
    // =========================================================

    /// <summary>Advances every running motion (one per part) and the carried card. Cheap; no lookups.</summary>
    public void Tick()
    {
        if (arriveStart >= 0f && Card != null)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - arriveStart) / CardArriveSeconds);
            Card.localPosition = Vector3.LerpUnclamped(ArriveFrom, cardRest, EaseOut(t));
            if (t >= 1f) arriveStart = -1f;
        }
        if (drag == DragKind.Card && Card != null && !cardMotion.Running)
        {
            float pickup = ReducedMotion ? 1f : Mathf.Clamp01((Time.unscaledTime - pickupStart) / CardPickupSeconds);
            if (pickupStart >= 0f && pickup < 1f)
            {
                float e = EaseInOut(pickup);
                Card.position = Vector3.Lerp(pickupPose.position, dragTarget, e) + table.up * (Mathf.Sin(pickup * Mathf.PI) * 0.012f * scale);
                Card.rotation = Quaternion.Slerp(pickupPose.rotation, dragRotation, e);
                SetWorldScale(Card, Mathf.Lerp(pickupScale, dragScale, e));
            }
            else
            {
                pickupStart = -1f;
                float k = ReducedMotion ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime / DragFollowSeconds);
                Card.position = Vector3.Lerp(Card.position, dragTarget, k);
                Card.rotation = Quaternion.Slerp(Card.rotation, dragRotation, k);
                SetWorldScale(Card, Mathf.Lerp(Card.lossyScale.x, dragScale, k));
            }
        }
        float follow = ReducedMotion ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime / ControlFollowSeconds);
        if (drag == DragKind.Dial)
        {
            dialAngle += Mathf.DeltaAngle(dialAngle, dialDragAngle) * follow;
            DialPointer.localRotation = Quaternion.Euler(0f, dialAngle, 0f);
        }
        if (drag == DragKind.Lever) SetLever(Mathf.Lerp(leverAngle, Mathf.Lerp(LeverRaised, LeverPulled, leverProgress), follow));
        // Selected keys sit slightly depressed; unselected keys rise back to their socket lip.
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            Vector3 at = categoryButtons[i].localPosition;
            at.y = Mathf.Lerp(at.y, i == selectedDetent ? 0.006f : 0.0085f, follow);
            categoryButtons[i].localPosition = at;
        }
        bool wasRunning = cardMotion.Running || whyMotion.Running;
        cardMotion.Tick();
        leverMotion.Tick();
        dialMotion.Tick();
        noteMotion.Tick();
        whyMotion.Tick();
        tabMotion.Tick();
        windowMotion.Tick();
        bool reviewWas = batchMotion.Running || markMotion.Running || gatherMotion.Running || summaryMotion.Running;
        batchMotion.Tick();
        keyMotion.Tick();
        markMotion.Tick();
        gatherMotion.Tick();
        summaryMotion.Tick();
        if (reviewWas && !batchMotion.Running && !markMotion.Running && !gatherMotion.Running && !summaryMotion.Running) RefreshSurfaces();
        if (rollerSpeed != 0f && Rollers != null)
        {
            rollerSpin += rollerSpeed * Time.unscaledDeltaTime;
            if (rollerUntil >= 0f && Time.unscaledTime >= rollerUntil) Spin(0f);
            ApplyRollers();
        }
        if (wasRunning && !cardMotion.Running && !whyMotion.Running) RefreshSurfaces();
    }

    /// <summary>
    /// Interruption (settings, focus, leave, disable): every motion snaps to its end and finishes (a feed
    /// completes, so the table settles to the committed state); a drag ends undecided. Nothing replays.
    /// </summary>
    public void Settle()
    {
        CancelDrag();
        settling = true;
        try
        {
            arriveStart = -1f;
            if (Card != null && cardPlace == CardPlace.Stand && Card.gameObject.activeSelf) Card.localPosition = armed ? Card.localPosition : cardRest;
            // A finishing motion may start the next one (a feed presents the next card): settle those too.
            for (int pass = 0; pass < 4; pass++)
            {
                bool any = cardMotion.Finish() | leverMotion.Finish() | dialMotion.Finish() | noteMotion.Finish() | whyMotion.Finish() |
                           tabMotion.Finish() | windowMotion.Finish() | batchMotion.Finish() | keyMotion.Finish() | markMotion.Finish() |
                           gatherMotion.Finish() | summaryMotion.Finish();
                if (!any) break;
            }
            Spin(0f);
        }
        finally
        {
            settling = false;
        }
        RefreshSurfaces();
    }

    /// <summary>Leaving the table: the record card back on the stand (or hidden), no note, no Why?, lever up, dial home.</summary>
    public void Rest()
    {
        Settle();
        if (WhyUp) CloseWhy(animate: false);
        HideNote(animate: false);
        SetLever(LeverRaised);
        armed = false;
        if (intakeOutline != null) intakeOutline.SetActive(false);
        if (Card != null && cardPlace != CardPlace.Stand && cardPlace != CardPlace.Away)
        {
            Park(Card, hinge, cardRest, Quaternion.identity);
            cardPlace = CardPlace.Stand;
        }
        RefreshSurfaces();
    }

    // ---------- helpers ----------

    private float rollerUntil = -1f;

    private void SpinFor(float seconds, float direction)
    {
        Spin(direction * 1440f);
        rollerUntil = Time.unscaledTime + (settling || ReducedMotion ? 0f : seconds);
    }

    private void Spin(float degreesPerSecond)
    {
        rollerSpeed = settling || ReducedMotion ? 0f : degreesPerSecond;
        if (rollerSpeed == 0f) rollerUntil = -1f;
    }

    private void ApplyRollers()
    {
        for (int i = 0; i < Rollers.Length; i++)
            if (Rollers[i] != null) Rollers[i].localRotation = rollerRest[i] * Quaternion.Euler(0f, (i == 0 ? 1f : -1f) * rollerSpin, 0f);
    }

    private struct Pose
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    private static Pose WorldPose(Transform t) => new Pose { position = t.position, rotation = t.rotation, localPosition = t.localPosition, localRotation = t.localRotation };

    /// <summary>The card in the feed frame: pivoted pivotDegrees past the half-fed tilt about the slot line, exposed metres of it above that line.</summary>
    private Pose FeedPose(float pivotDegrees, float exposed)
    {
        Quaternion local = Quaternion.Euler(-pivotDegrees, 0f, 0f);
        Vector3 localPosition = local * new Vector3(0f, 0.0015f, exposed - CardHeight * 0.5f);
        return new Pose
        {
            localPosition = localPosition, localRotation = local,
            position = FeedFrame.TransformPoint(localPosition), rotation = FeedFrame.rotation * local
        };
    }

    /// <summary>One continuous eased flight along a cubic curve (lift, carry, settle) between two parents; the card's size never changes.</summary>
    private void Fly(Pose start, float startScale, Vector3 c1, Vector3 c2, Pose end, float seconds, CardPlace place, Action done)
    {
        arriveStart = -1f;
        Card.SetParent(table, true);
        cardPlace = CardPlace.Flying;
        SetWorldScale(Card, startScale);
        Run(cardMotion, seconds, t =>
        {
            float e = EaseInOut(t), u = 1f - e;
            Card.position = u * u * u * start.position + 3f * u * u * e * c1 + 3f * u * e * e * c2 + e * e * e * end.position;
            Card.rotation = Quaternion.Slerp(start.rotation, end.rotation, e);
            SetWorldScale(Card, Mathf.Lerp(startScale, hinge.lossyScale.x, e));
        }, () =>
        {
            cardPlace = place;
            done?.Invoke();
            RefreshSurfaces();
        });
    }

    private static void Park(Transform t, Transform parent, Vector3 localPosition, Quaternion localRotation)
    {
        t.SetParent(parent, false);
        t.localPosition = localPosition;
        t.localRotation = localRotation;
        t.localScale = Vector3.one;
    }

    private static void SetWorldScale(Transform t, float world)
    {
        float parent = t.parent != null ? t.parent.lossyScale.x : 1f;
        t.localScale = Vector3.one * (world / Mathf.Max(0.0001f, parent));
    }

    /// <summary>The reading position for a card w x h (the originals' reader: centred, in front of the stand and machine, about three quarters of the view high).</summary>
    public static void ReaderPose(Camera camera, float width, float height, out Vector3 position, out Quaternion rotation, out float worldScale)
    {
        Rect pixels = camera.pixelRect;
        float depth = Mathf.Max(camera.nearClipPlane + 0.04f, 0.32f);
        position = camera.ScreenToWorldPoint(new Vector3(pixels.x + pixels.width * 0.5f, pixels.y + pixels.height * 0.47f, depth));
        rotation = Quaternion.LookRotation(camera.transform.up, -camera.transform.forward);
        float visibleHeight = 2f * depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float visibleWidth = visibleHeight * Mathf.Max(0.1f, camera.aspect);
        worldScale = Mathf.Min(visibleHeight * 0.78f / height, visibleWidth * 0.7f / width);
    }

    private void Run(Motion motion, float seconds, Action<float> apply, Action done)
    {
        motion.Start(settling || ReducedMotion ? 0f : seconds, apply, done);
    }

    private static bool ReducedMotion => WorldEmote.ReducedMotion;

    public static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);
    public static float EaseIn(float t) => t * t * t;
    public static float EaseInOut(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    /// <summary>A weighted detent: decelerates into place with a slight settle past it.</summary>
    public static float EaseOutBack(float t)
    {
        const float c1 = 1.2f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    /// <summary>One part's motion: replaced by the next, finished at once by Settle (no stacked waits).</summary>
    private sealed class Motion
    {
        private float start, seconds;
        private Action<float> apply;
        private Action done;

        public bool Running => apply != null;

        public void Start(float duration, Action<float> applyStep, Action finished)
        {
            apply = applyStep;
            done = finished;
            start = Time.unscaledTime;
            seconds = duration;
            if (duration <= 0f) Finish();
        }

        public void Tick()
        {
            if (apply == null) return;
            float t = seconds <= 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - start) / seconds);
            apply(t);
            if (t >= 1f) Finish();
        }

        /// <summary>Snaps to the end and runs its completion once. True when one was running.</summary>
        public bool Finish()
        {
            if (apply == null) return false;
            Action<float> step = apply;
            Action finished = done;
            apply = null;
            done = null;
            step(1f);
            finished?.Invoke();
            return true;
        }

        public void Stop()
        {
            apply = null;
            done = null;
        }
    }
}

/// <summary>One machine touch surface (forwarded to its model). Drags, cancelled touches, dialogue, settings and reward screens never count as taps.</summary>
public sealed class Level3MachineTouch : MonoBehaviour, IPointerClickHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    internal Level3PaperMachineModel Model;
    internal int Part;
    internal int Index;
    private HighlightEffect highlight;

    internal void SetHighlighted(bool value)
    {
        if (value && highlight == null)
        {
            highlight = HighlightEffect.For(this);
            highlight.SetTargetRoot(transform.parent);
        }
        highlight?.SetFocused(value && Model != null && Model.InputAllowed);
    }
    public void OnPointerEnter(PointerEventData e) => SetHighlighted(true);
    public void OnPointerExit(PointerEventData e) => SetHighlighted(false);

    public void OnInitializePotentialDrag(PointerEventData eventData) { if (eventData != null) eventData.useDragThreshold = true; }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
        eventData.eligibleForClick = false;
        Model?.BeginDrag(this, Part, eventData);
    }

    public void OnDrag(PointerEventData eventData) => Model?.Dragged(this, eventData);
    public void OnEndDrag(PointerEventData eventData) => Model?.EndDrag(this, eventData);

    public void OnPointerClick(PointerEventData eventData)
    {
        // The UI module owns click eligibility and cancellation. Re-reading the Remote device's
        // touch phase here can reject an already accepted UI release after its state has advanced.
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left || eventData.dragging || !eventData.eligibleForClick) return;
        if (TouchWasCancelled(eventData) || DialogueUI.IsAnyDialogueActive || InGameSettings.IsOpen || HudRewardScreen.AnyOpen || !isActiveAndEnabled) return;
        Model?.Clicked(Part, Index, eventData.position);
    }

    private static bool TouchWasCancelled(PointerEventData eventData)
    {
        if (eventData is ExtendedPointerEventData extended && extended.device is Touchscreen screen)
            foreach (var touch in screen.touches)
                if (touch.touchId.ReadValue() == extended.touchId)
                    return touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled;
        // A Remote touch may already be cleared by callback time; the UI module's eligibility still owns that release.
        return false;
    }

    private void OnDisable() { SetHighlighted(false); Model?.OwnerDisabled(this); }
}

/// <summary>The stand card's tap (forwarded to the table). Drags, cancelled touches, dialogue, settings and reward screens never count.</summary>
public sealed class Level3StandCardTouch : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Action Clicked;
    public void OnBeginDrag(PointerEventData e) { if (e != null) e.eligibleForClick = false; }
    public void OnDrag(PointerEventData e) { }
    public void OnEndDrag(PointerEventData e) { }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left || MobileInputGuard.IsDragOrCancelled(eventData)) return;
        if (DialogueUI.IsAnyDialogueActive || InGameSettings.IsOpen || HudRewardScreen.AnyOpen || !isActiveAndEnabled) return;
        Clicked?.Invoke();
    }
}

/// <summary>Level3-only input on the existing physical card/original touch rect.
/// The EventSystem still owns pointer capture, thresholds and callbacks; no
/// transparent Graphic/canvas mesh depth/physics broadphase is needed to hit it.</summary>
public sealed class Level3TablePaperRaycaster : GraphicRaycaster
{
    public bool IndependentDocument;
    private ClientRecordPaper independentPaper;
    // EventSystem compares module priority before geometry/distance. Overlay HUD graphics otherwise
    // always beat a world-space canvas, including transparent legacy panels over the lower screen.
    // Floating documents start at1100, Leave Table at2400, transition shield at32000.
    public const int PointerPriority = 1000;
    public override int sortOrderPriority => (IndependentDocument || independentPaper != null && !independentPaper.IsAttached) ? Doc3DLayer.For(transform)?.SortingOrder ?? PointerPriority : PointerPriority;
    public override int renderOrderPriority => sortOrderPriority;

    private RectTransform surface;
    private Canvas surfaceCanvas;
    private Transform planeParent;
    private Matrix4x4 parentToPlane;
    private Vector2 planeSize;

    public void BindPlane(Transform parent, Vector3 position, Quaternion rotation, float width, float height)
    {
        planeParent = parent;
        parentToPlane = Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
        planeSize = new Vector2(width, height);
    }

    public static bool HitPlane(Ray ray, Matrix4x4 worldToPlane, Vector2 size, out float at) =>
        HitRect(ray, worldToPlane, new Rect(-size.x * 0.5f, -size.y * 0.5f, size.x, size.y), out at);

    protected override void Awake()
    {
        base.Awake();
        surface = (RectTransform)transform;
        independentPaper = GetComponentInParent<ClientRecordPaper>();
        surfaceCanvas = GetComponent<Canvas>();
    }
    public override Camera eventCamera => surfaceCanvas != null && surfaceCanvas.worldCamera != null
        ? surfaceCanvas.worldCamera : HeldOnScreen.ActiveCamera();
    public override void Raycast(PointerEventData e, List<RaycastResult> hits)
    {
        Level3BeaTable owner = Level3BeaTable.Instance;
        Camera view = eventCamera;
        if ((IndependentDocument ? (InGameSettings.IsOpen || DialogueUI.IsAnyDialogueActive || HudRewardScreen.AnyOpen || InventoryScreen.AnyOpen ||
            (independentPaper != null && independentPaper.IsAttached && (owner == null || !owner.CanTouchPapers)) ||
            (CameraController.Instance != null && CameraController.Instance.IsTransitioning)) : (owner == null || !owner.CanTouchPapers)) || surface == null || view == null ||
            !view.pixelRect.Contains(e.position) || !surface.gameObject.activeInHierarchy) return;
        Doc3DLayer layer = Doc3DLayer.For(surface);
        if (IndependentDocument || independentPaper != null && !independentPaper.IsAttached)
        {
            int order = layer != null ? layer.SortingOrder : 0;
            // Module priority precedes result sortingOrder in Unity. Never hit
            // through a visually higher document, including the real Case Folder.
            foreach (Doc3DLayer other in Doc3DLayer.All)
                if (other != null && other != layer && other.IsVisible && other.SortingOrder > order && other.ShowsAt(e.position, out _)) return;
            foreach (FloatingWindow window in FloatingWindow.Instances)
                if (window != null && window.IsOpen() && window.SortingOrder > order && window.ContainsScreenPoint(e.position)) return;
        }
        // A floating document in front keeps its touches; attached stack
        // rectangles otherwise sort by real distance (top paper first).
        Ray ray = view.ScreenPointToRay(e.position);
        float at;
        bool hit = planeParent != null
            ? HitPlane(ray, parentToPlane * planeParent.worldToLocalMatrix, planeSize, out at)
            : HitRect(ray, surface.worldToLocalMatrix, surface.rect, out at);
        if (!hit || at > view.farClipPlane) return;
        // Revised T07: an offered amount band on this paper's surface takes the touch (else the paper does).
        Level3AmountSource band = IndependentDocument && planeParent == null
            ? Level3AmountSource.At(surface, surface.worldToLocalMatrix.MultiplyPoint3x4(ray.GetPoint(at))) : null;
        hits.Add(new RaycastResult
        {
            gameObject = band != null ? band.gameObject : gameObject, module = this, distance = at,
            screenPosition = e.position, worldPosition = view.ScreenPointToRay(e.position).GetPoint(at),
            sortingOrder = layer != null ? layer.SortingOrder : surfaceCanvas.sortingOrder,
            sortingLayer = surfaceCanvas.sortingLayerID, depth = 0, index = hits.Count
        });
    }
    public static bool HitRect(Ray ray, Matrix4x4 worldToLocal, Rect rect, out float at)
    {
        Vector3 origin = worldToLocal.MultiplyPoint3x4(ray.origin);
        Vector3 direction = worldToLocal.MultiplyVector(ray.direction);
        at = 0f;
        if (Mathf.Abs(direction.z) < 0.0000001f) return false;
        at = -origin.z / direction.z;
        if (at < 0f) return false;
        Vector3 local = origin + direction * at;
        return local.x >= rect.xMin && local.x <= rect.xMax && local.y >= rect.yMin && local.y <= rect.yMax;
    }
}
