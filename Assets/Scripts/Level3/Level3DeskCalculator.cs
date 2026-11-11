using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TouchPhase = UnityEngine.TouchPhase;

/// <summary>
/// PURPOSE:
/// Level 3's desk calculator and its protected practice (T03), under
/// Level3Gameplay/DeskCalculator. One shared tool for the whole chapter:
/// an unrelated first-use lesson, then the existing client computations.
///
/// THE OBJECTS: a physical calculator (Level3CalculatorModel: the supplied
/// Synty prop with readable keys, display, roll and tape) and, from the
/// calculator task on, the practice clipboard (Level3PracticeCardModel).
/// Both rest on the consultant desk in reach of the seat.
///
/// AT THE DESK: tap either one and both lift toward the seated view, sized
/// to the screen (like Level 2's hand stamps): the card on the left, the
/// calculator with its tape on the right. Then:
/// - select an amount, then tap the display, or drag it there (its label goes on the tape);
/// - tap keys (one input per tap-release; a held finger never repeats);
/// - = prints a draft strip; drag to its field or select strip then tap field
///   (the existing chapter command saves first, then the strip clips on);
/// - PUT CALCULATOR AWAY, Exit Desk, settings, a conversation or losing focus
///   settles everything back onto the desk and releases the pressed key.
///
/// T07: once Bea's worksheet is collected, her computation sheet
/// (Level3ComputationLeafModel) replaces the practice card on the clipboard;
/// see Level3DeskCalculatorComputation.cs. T08: her certificate, raised from
/// the sheet's tab, owns the touches while it is up (a tap lowers it). T09:
/// the shared Level2DeskStamps/Level2StampDrag owns the existing hand stamps.
///
/// The practice is not the player's money and is never scored. Guide cues
/// (cue:CalculatorLifted, PracticeSourceSelected, PracticeTapePrinted,
/// PracticeTapeAttached) only describe what the player did.
///
/// REVISED T03 (first office use): the budget practice is retired. At Bea's
/// computation, until the chapter's calculatorTaught marker is saved, the
/// card shows one unrelated example (Level3PracticeRules.LessonCard) in place
/// of her sheet: tap an amount, −, the other, =, then select the tape and tap the slot
/// (Level3Checkpointer.CommitCalculatorLesson: saved, then the tape clips
/// on). The example has its own draft, so whatever Bea's working had on the
/// calculator is untouched. Once saved, the tools settle on the desk and lift
/// again with her sheet; an interruption before that leaves the example to
/// finish later (nothing was taught, nothing is lost). Opening the Case
/// Folder puts the tools away, and lifting them closes the floating folder.
///
/// TEACHING PASS (6 November 2026): the same card and separate example draft
/// serve three short demonstrations (Level3PracticeRules.CurrentLesson):
/// taxable income at first use, the tax table once Bea's taxable income is
/// attached, withholding once her certificate is collected. Each stands in
/// for her (read-only) sheet until its own marker is saved; one that becomes
/// due while the tools are up settles them and lifts again with its card.
///
/// ARTHUR FIRST (follow-up, the Level 2 Mark the Money! pattern): when an
/// example's card first comes up, Arthur does it himself while the player
/// watches (LessonDemo: amounts light up and enter, keys press, the tape
/// prints and clips onto the card, one line from him per step; player input
/// is off and Bea's clock is paused because the example is due). Then the
/// card and example calculator are cleared, he says "your turn", and the
/// player does the same example; his tips answer a wrong tape. Watching to
/// the end saves its "demo-" marker; putting the tools away stops it and it
/// plays again on the next lift.
/// </summary>
[DefaultExecutionOrder(-40)]
public sealed partial class Level3DeskCalculator : MonoBehaviour
{
    public const string ChildName = "DeskCalculator";
    public const string DeskPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk";
    public const string CueLifted = "CalculatorLifted";
    public const string CueSourceSelected = "PracticeSourceSelected";
    public const string CueTapePrinted = "PracticeTapePrinted";
    public const string CueTapeAttached = "PracticeTapeAttached";

    // Desk placement, relative to the seat (meters on the desk plane).
    public const float CalculatorForward = 0.40f;
    public const float CalculatorRight = 0.21f;
    public const float CardForward = 0.42f;
    public const float CardRight = -0.14f;
    // On-screen layout while lifted.
    public const float LiftDistance = 0.62f;
    public const float LiftScreenHeight = 0.62f;
    public const float PracticeScreenHeight = 0.66f;
    public const float LiftCenterY = 0.49f;
    private const float ScreenMargin = 0.03f;
    private const float LiftSeconds = 0.35f;
    private const float ReturnSeconds = 0.3f;
    private const float KeyPressSeconds = 0.08f;
    private const float TapeFeedSeconds = 0.2f;
    private const float AttachSeconds = 0.28f;

    private enum Mode { Waiting, Resting, Lifting, Lifted, Returning }

    private struct Pose
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Scale;

        public static Pose Lerp(Pose a, Pose b, float t) => new Pose
        {
            Position = Vector3.LerpUnclamped(a.Position, b.Position, t),
            Rotation = Quaternion.SlerpUnclamped(a.Rotation, b.Rotation, t),
            Scale = Mathf.LerpUnclamped(a.Scale, b.Scale, t)
        };
    }

    public static Level3DeskCalculator Instance { get; private set; }

    private readonly Level3Calculator caseCalculator = new Level3Calculator();
    private readonly Level3Calculator lessonCalculator = new Level3Calculator();

    /// <summary>The shared arithmetic and draft tape (presentation reads it; it never grades). The first-use example keeps its own draft, so the case's working is never touched.</summary>
    public Level3Calculator Calculator => LessonOpen ? lessonCalculator : caseCalculator;

    /// <summary>The example's tape was saved; it stays up until the tools settle (then Bea's sheet comes up).</summary>
    private static bool lessonHolding;
    /// <summary>The demonstration whose saved tape is on show while lessonHolding (its marker is already saved).</summary>
    private static Level3CardSpec heldLesson;
    /// <summary>A demonstration became due while the tools were up: they settle, its card is printed, they lift again.</summary>
    private bool switchingLesson;
    private float lessonSettleAt = -1f;
    private bool liftAfterSettle;
    private List<string> lessonTapeLines;
    private string lessonTapeResult;
    private const float LessonShowSeconds = 0.9f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPlaySession() { lessonHolding = false; heldLesson = null; }

    /// <summary>Revised T03: the first-use example stands in for Bea's sheet (due, or its saved tape still on show).</summary>
    private static bool LessonOpen => Level3PracticeRules.LessonDue(Chapter) || lessonHolding;

    /// <summary>The demonstration on the card now: the one just saved while it is on show, else the one due (null: none).</summary>
    private static Level3CardSpec ActiveLesson => lessonHolding && heldLesson != null ? heldLesson : Level3PracticeRules.CurrentLesson(Chapter);

    public bool Lifted => mode == Mode.Lifted;
    /// <summary>The resting calculator while it is the next thing to tap at the desk (else null: no stale hand).</summary>
    public Transform GuideBody => mode == Mode.Resting && AtDesk && !HudRewardScreen.AnyOpen && calculator != null ? calculator.Root : null;
    public bool ToolPointerActive => Lifted && AtDesk && !demoRunning;
    /// <summary>Arthur is demonstrating an example on the card and calculator (the player watches).</summary>
    public bool DemoRunning => demoRunning;
    private bool demoRunning;
    private Coroutine lessonDemo;
    private Level3CardSpec demoLesson;
    public bool Built => mode != Mode.Waiting;

    private Mode mode = Mode.Waiting;
    private Level3DeskProps props;
    private Level3CalculatorModel calculator;
    private Level3PracticeCardModel card;
    private Pose calculatorRest, cardRest, calculatorFrom, cardFrom, calculatorTo, cardTo;
    private float moveStart;
    private float moveSeconds;
    private int liftScreenW, liftScreenH;

    private Collider pressed;
    private Vector2 pressedAt;
    private Transform movingTool;
    private HeldOnScreen movingBounds;
    private Rect movingArea;
    private Vector3 movingOffset;
    private float movingDepth;
    private Doc3DLayer calculatorLayer, cardLayer, leafLayer, flyingLayer;
    private readonly List<BoxCollider> toolHitBoxes = new List<BoxCollider>(64);
    private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();
    private string selectedSource;
    private string armedSlot;
    private Transform flying;
    private Vector3 flyFrom, flyTo, flyScale;
    private Quaternion flyFromRotation, flyToRotation;
    private float flyStart = -1f;
    private string flyDestination;
    private ValueTransferManager transferManager;
    private int shownTapes = -1;
    private Level3Phase shownPhase = (Level3Phase)(-1);

    private CanvasGroup putAwayGroup;
    private GameObject putAwayRoot;
    private float putAwayAlpha;

    private void Awake()
    {
        Instance = this;
        lessonHolding = false;
        heldLesson = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (putAwayRoot != null) Destroy(putAwayRoot);
        ReleasePointer();
        FinishAttachFlight();
        ReleaseToolLayers();
        UnbindTransferSelection();
        props?.Destroy();
    }

    private void OnDisable()
    {
        StopLessonDemo();
        ReleasePointer();
        FinishAttachFlight();
        UnbindTransferSelection();
        ReleaseToolLayers();
        if (mode == Mode.Lifted || mode == Mode.Lifting || mode == Mode.Returning)
        {
            Apply(calculator.Root, calculatorRest);
            Apply(PaperRoot, cardRest);
            mode = Mode.Resting;
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) { ReleasePointer(); PutAway(); }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) { ReleasePointer(); PutAway(); }
    }

    // =========================================================
    // BUILD (once, after the chapter state is ready)
    // =========================================================

    private void TryBuild()
    {
        Level3Checkpointer chapter = Level3Checkpointer.Active;
        if (chapter == null || !chapter.IsReady || CameraController.Instance == null) return;
        Transform desk = PlayerPlacement.Find(gameObject.scene, DeskPath);
        Transform seat = PlayerPlacement.Find(gameObject.scene, Level2DeskStamps.SeatPath);
        if (desk == null || seat == null)
        {
            Debug.LogWarning($"[Level3] Desk calculator: '{DeskPath}' or its seat was not found; the calculator is not built.");
            enabled = false;
            return;
        }

        props = new Level3DeskProps(Level2DeskStamps.DragLayer);
        Level3SceneAssets assets = Level3SceneAssets.Load();
        GameObject body = assets != null ? assets.calculatorPrefab : null;
        if (body == null) Debug.LogWarning($"[Level3] Resources/{Level3SceneAssets.ResourcePath} has no calculator prefab; a plain navy body stands in for the Synty calculator.");
        calculator = Level3CalculatorModel.Build(transform, body, props);
        card = Level3PracticeCardModel.Build(transform, props, DesiredCard);
        BuildLeaf();

        Vector3 forward = Vector3.ProjectOnPlane(seat.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f) forward = -desk.forward;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);
        calculatorRest = new Pose { Position = DeskPoint(desk, seat, seat.position + forward * CalculatorForward + right * CalculatorRight), Rotation = facing, Scale = 1f };
        cardRest = new Pose { Position = DeskPoint(desk, seat, seat.position + forward * CardForward + right * CardRight), Rotation = facing, Scale = 1f };
        Apply(calculator.Root, calculatorRest);
        Apply(card.Root, cardRest);
        if (leaf != null) Apply(leaf.Root, cardRest);
        // The shared Level2DeskStamps owns the existing stand/drag/ghost/press presentation in Level 3 too.

        BindTapeTransfers();
        mode = Mode.Resting;
        RefreshDisplay();
        SyncCard(force: true);
        Debug.Log($"[Level3] Desk calculator built ({(calculator.UsesSourceBody ? "Synty body" : "stand-in body")}, " +
                  $"{calculator.Width:0.000} x {Level3CalculatorModel.BodyLength:0.000} m) at {calculatorRest.Position}.");
    }

    /// <summary>The desk top under a point (the desk's own colliders, highest upward face), clamped inside the desk.</summary>
    private static Vector3 DeskPoint(Transform desk, Transform seat, Vector3 point)
    {
        Bounds? deskBounds = null;
        foreach (Collider collider in desk.GetComponentsInChildren<Collider>(true))
        {
            if (collider.isTrigger || !collider.enabled || collider.gameObject.layer == Level2DeskStamps.DragLayer) continue;
            if (deskBounds == null) deskBounds = collider.bounds;
            else { Bounds b = deskBounds.Value; b.Encapsulate(collider.bounds); deskBounds = b; }
        }
        if (deskBounds.HasValue)
        {
            Bounds b = deskBounds.Value;
            const float inset = 0.09f;
            point.x = Mathf.Clamp(point.x, b.min.x + inset, Mathf.Max(b.min.x + inset, b.max.x - inset));
            point.z = Mathf.Clamp(point.z, b.min.z + inset, Mathf.Max(b.min.z + inset, b.max.z - inset));
        }

        float best = float.NegativeInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(point + Vector3.up * 0.6f, Vector3.down, 1.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (hit.collider.transform.IsChildOf(desk) && hit.normal.y > 0.7f && hit.point.y > best) best = hit.point.y;
        if (float.IsNegativeInfinity(best))
        {
            foreach (Renderer renderer in desk.GetComponentsInChildren<Renderer>(true))
                if (renderer.enabled) best = Mathf.Max(best, renderer.bounds.max.y);
            if (float.IsNegativeInfinity(best) || best > seat.position.y) best = seat.position.y - 0.71f;
        }
        point.y = best + 0.0005f;
        return point;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private bool AtDesk => CameraController.Instance != null &&
        CameraController.Instance.CurrentMode == CameraController.CameraMode.Workstation &&
        !CameraController.Instance.IsTransitioning && !InGameSettings.IsOpen && !DialogueUI.IsAnyDialogueActive &&
        (GameStateMachine.Instance == null || !GameStateMachine.Instance.IsPaused);

    private HighlightEffect toolHighlight;
    public void ShowToolHover(Collider target)
    {
        if (toolHighlight != null && (target == null || toolHighlight.gameObject != target.gameObject)) toolHighlight.SetFocused(false);
        if (target == null) return;
        toolHighlight = HighlightEffect.For(target);
        toolHighlight.SetTargetRoot(target.transform.parent);
        toolHighlight.SetFocused(true);
    }

    private void Update()
    {
        if (mode == Mode.Waiting)
        {
            TryBuild();
            return;
        }

        SyncCard(force: false);
        SyncLeaf(force: false);
        Animate();
        TickLessonHandOff();
        TickLessonSwitch();
        TickToolSettle();
        TickPrintedStrip();
        calculator.Tick();
        if (toolHighlight != null && toolHighlight.Strength > 0f)
        {
            calculatorLayer?.MarkDirty(); cardLayer?.MarkDirty(); leafLayer?.MarkDirty();
        }
        card?.Tick();
        leaf?.Tick();
        UpdatePutAway();

        if (!AtDesk)
        {
            // Exit Desk, settings, a conversation or a pause: settle back onto the desk.
            if (mode == Mode.Lifted || mode == Mode.Lifting) PutAway();
            ShowToolHover(null);
            ReleasePointer();
            return;
        }
        if (mode == Mode.Lifted && (Screen.width != liftScreenW || Screen.height != liftScreenH))
        {
            ReleasePointer();
            FinishAttachFlight();
            ComputeLiftPoses();
            Apply(calculator.Root, calculatorTo);
            Apply(PaperRoot, cardTo);
        }
        // Follow-up (Arthur first): the first time an example's card is up, Arthur works it before the player does.
        if (lessonDemo == null && mode == Mode.Lifted && flyStart < 0f && !lessonHolding && !switchingLesson && card != null &&
            card.Spec.IsLesson && card.Spec == ActiveLesson && !Level3PracticeRules.DemoSeen(Chapter, card.Spec) &&
            !lessonCalculator.HasDraft && lessonCalculator.Tokens.Count == 0 && lessonCalculator.Entry.Length == 0)
            lessonDemo = StartCoroutine(LessonDemo(card.Spec));
        BindTransferSelection();
        if (transferManager?.SelectedSourceComponent is Level3TapeTransfer selected && selected.Owner == this &&
            transferManager.SelectedValue?.Value is Level3CalculatorTransferValue payload && payload.Context != TransferContext)
            ReleasePointer();
        if (mode == Mode.Resting) PollPointer();
    }

    private void PollPointer()
    {
        if (ValueDragSource.IsAnyDragActive) { MobileInputGuard.SuppressWorldTapsThisFrame(); return; }
        if (!Level2DeskStamps.TryReadPointer(out Vector2 screen, out bool down, out bool up)) { ShowToolHover(null); return; }
        ShowToolHover(Pick(screen));
        if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Canceled) ||
            (UnityEngine.InputSystem.Touchscreen.current != null &&
             UnityEngine.InputSystem.Touchscreen.current.primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled))
        {
            ReleasePointer();
            return;
        }
        if (down)
        {
            ReleasePointer();
            if (PointerBlocked(screen)) return;
            pressed = Pick(screen);
            pressedAt = screen;
            if (pressed != null) MobileInputGuard.SuppressWorldTapsThisFrame();
            return;
        }
        if (!up || pressed == null) return;
        Collider released = Pick(screen);
        Collider target = pressed;
        float slop = Mathf.Max(12f, 0.03f * Mathf.Min(Screen.width, Screen.height));
        bool tap = released == target && (screen - pressedAt).sqrMagnitude <= slop * slop;
        ReleasePointer();
        if (!tap) return;
        // The tap is ours: nothing behind the tools reacts to it.
        MobileInputGuard.SuppressWorldTapsThisFrame();
        if (mode == Mode.Resting && IsCertificateTab(target)) RaiseCertificate();
        else if (mode == Mode.Resting) Lift();

    }

    private Collider Pick(Vector2 screen)
    {
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        if (view == null) return null;
        // Composited windows draw in sorting order, independently of physical depth.
        Doc3DLayer top = null;
        foreach (Doc3DLayer layer in Doc3DLayer.All)
            if (layer != null && layer != flyingLayer && layer.IsVisible && (top == null || layer.SortingOrder > top.SortingOrder) && layer.ShowsAt(screen, out _)) top = layer;
        if (top != null && top != calculatorLayer && top != cardLayer && top != leafLayer) return null;
        Ray ray = view.ScreenPointToRay(screen);
        BoxCollider nearest = null;
        float distance = float.MaxValue;
        PickTool(calculator.Root, top, ray, ref nearest, ref distance);
        PickTool(card.Root, top, ray, ref nearest, ref distance);
        if (leaf != null) PickTool(leaf.Root, top, ray, ref nearest, ref distance);
        return nearest;
    }

    private void PickTool(Transform root, Doc3DLayer top, Ray ray, ref BoxCollider nearest, ref float distance)
    {
        if (root == null || !root.gameObject.activeInHierarchy || (top != null && root != top.Source)) return;
        root.GetComponentsInChildren(false, toolHitBoxes);
        foreach (BoxCollider target in toolHitBoxes)
        {
            if (!target.enabled) continue;
            if (Level3DeskToolHitTarget.HitBox(ray, target.transform.worldToLocalMatrix,
                new Bounds(target.center, target.size), out float at) &&
                (nearest == null || PreferToolHit(IsToolMoveHandle(target), at, IsToolMoveHandle(nearest), distance)))
            { nearest = target; distance = at; }
        }
    }

    public static bool PreferToolHit(bool moveTarget, float at, bool previousMove, float previousAt) =>
        moveTarget == previousMove ? at < previousAt : !moveTarget;

    private void ReleasePointer()
    {
        EndToolMove();
        Level3TapeTransfer.Cancel(this);
        if (flyStart < 0f) RestorePrintedStrip();
        FinishToolSettle();
        calculator?.FinishInputPop();
        card?.FinishSettle();
        leaf?.FinishPops();
        if (calculatorLayer != null) calculatorLayer.GetComponentInChildren<Level3DeskToolHitTarget>()?.Clear();
        if (cardLayer != null) cardLayer.GetComponentInChildren<Level3DeskToolHitTarget>()?.Clear();
        if (leafLayer != null) leafLayer.GetComponentInChildren<Level3DeskToolHitTarget>()?.Clear();
        pressed = null;
        calculator?.ReleaseAll();
    }

    private bool PointerBlocked(Vector2 screen)
    {
        EventSystem events = EventSystem.current;
        if (events == null) return false;
        pointerHits.Clear();
        events.RaycastAll(new PointerEventData(events) { position = screen }, pointerHits);
        foreach (RaycastResult hit in pointerHits)
        {
            if (!(hit.module is GraphicRaycaster)) continue;
            return hit.gameObject.GetComponent<Level3DeskToolHitTarget>() == null;
        }
        return false;
    }

    private Doc3DLayer CreateToolLayer(Transform tool, bool pointerSurface = true)
    {
        Doc3DLayer layer = Doc3DLayer.Create(null, tool, tool.name);
        layer.AlwaysComposite = true;
        layer.CompositeOverDocuments = true;
        layer.RefreshTextBeforeCapture = true;
        layer.AssignSortingOrder(FloatingWindow.TakeTopSortingOrder());
        if (!pointerSurface) return layer;
        // The layer itself is the EventSystem hit target. No invisible Image
        // is involved in deciding whether these physical tools receive input.
        var hit = layer.gameObject.AddComponent<Level3DeskToolHitTarget>();
        hit.Owner = this;
        hit.Layer = layer;
        return layer;
    }

    private void ReleaseToolLayers()
    {
        if (calculatorLayer != null) Destroy(calculatorLayer.gameObject);
        if (cardLayer != null) Destroy(cardLayer.gameObject);
        if (leafLayer != null) Destroy(leafLayer.gameObject);
        if (flyingLayer != null) Destroy(flyingLayer.gameObject);
        calculatorLayer = cardLayer = leafLayer = flyingLayer = null;
    }

    // =========================================================
    // ACTIONS
    // =========================================================

    private void Activate(Collider target)
    {
        if (calculator.NextTapePage(target)) { AudioManager.Instance?.PlayPaperSFX(); return; }
        if (calculator.TryKey(target, out Level3CalcKey key))
        {
            calculator.Press(target, KeyPressSeconds);
            PressKey(key);
            return;
        }
        if (ActivateLeaf(target)) return;

    }

    private void PressKey(Level3CalcKey key)
    {
        Level3TapeTransfer.Cancel(this);
        if (flyStart >= 0f) FinishAttachFlight();
        RestorePrintedStrip();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayKeyboardSFX();
        Level3CalcResult result = Calculator.Press(key);
        Disarm();
        if (Calculator.EntrySource == null && key != Level3CalcKey.Percent) SetSelected(null);
        if (result.Printed)
        {
            calculator.SetTape(DraftLines(), animate: true, TapeFeedSeconds);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
            GameplayEvents.RaiseGuideCue(CueTapePrinted);
            Level3PracticeSlot next = Level3PracticeRules.NextSlot(Chapter);
            if (PracticeOpen && next != null) card.SetMessage("Drag the printed tape to slot " + SlotNumber(next.DestinationId) + ".");
            else if (LessonOpen) card.SetMessage(card.Spec == Level3PracticeRules.TaxCard && !LessonFinalStep
                ? "Keep going: next step,\nthen = again."
                : "Drag tape into the field;\nor tap tape, then field.");
            else if (ComputationOpen) LeafPrinted();
        }
        RefreshDisplay();
    }

    private void Attach(string destinationId)
    {
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null) return;
        List<Level3CalcToken> expression = Calculator.DraftExpression;
        string result = Calculator.DraftResult;
        Level3Checkpointer.TapeCommit outcome = owner.CommitPracticeTape(destinationId, expression, result, out string hint);
        Disarm();
        switch (outcome)
        {
            case Level3Checkpointer.TapeCommit.Attached:
                StartAttachFlight(destinationId, expression, result);
                Calculator.ConsumeDraft();
                card.SetMessage("Value attached.");
                break;
            case Level3Checkpointer.TapeCommit.AlreadyAttached:
                SyncCard(force: true);
                break;
            default:
                // Refused or not saved: the tape stays on the calculator, nothing is attached, no grade.
                card.SetMessage(hint);
                break;
        }
        RefreshDisplay();
    }

    private void Disarm()
    {
        DisarmLeaf();
        if (armedSlot == null) return;
        armedSlot = null;
        card.SetArmed(null);
    }

    private void SetSelected(string sourceId)
    {
        selectedSource = sourceId;
        card.SetSelected(sourceId);
        if (sourceId == null) leaf?.SetSelectedSource(null);
    }

    private void RefreshDisplay() => calculator.SetDisplay(Calculator.DisplayText, Calculator.Caption);

    private static Level3ChapterCheckpoint Chapter => Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;

    /// <summary>The card takes tapes only while the calculator task is current.</summary>
    private static bool PracticeOpen => Chapter != null && Level3ChapterRules.PhaseOf(Chapter) == Level3Phase.ToolPractice;

    private static int SlotNumber(string destinationId) => destinationId == Level3PracticeRules.SecondSlot ? 2 : 1;

    // =========================================================
    // CARD STATE (quiet: restore and repeats never animate or announce)
    // =========================================================

    /// <summary>Revised T04: Bea's table has no calculator (her records go through the paper machine), so the chapter's one calculator never leaves this desk.</summary>
    public static bool AwayAtBeaTable(Level3Phase phase) => false;

    private void SyncCard(bool force)
    {
        Level3ChapterCheckpoint chapter = Chapter;
        if (chapter == null || card == null) return;
        Level3Phase phase = Level3ChapterRules.PhaseOf(chapter);
        int tapes = chapter.tapes != null ? chapter.tapes.Count : 0;
        string lessonId = DesiredCard.Id;
        if (!force && phase == shownPhase && tapes == shownTapes && lessonId == shownLessonId) return;
        shownPhase = phase;
        shownTapes = tapes;
        shownLessonId = lessonId;

        bool away = AwayAtBeaTable(phase);
        if (calculator.Root.gameObject.activeSelf == away)
        {
            if (away && (mode == Mode.Lifted || mode == Mode.Lifting)) PutAway();
            calculator.Root.gameObject.SetActive(!away);
            // Back from the table: it shows whatever was last on it (the same shared calculator).
            if (!away)
            {
                RefreshDisplay();
                calculator.SetTape(Calculator.HasDraft ? Level3Calculator.TapeLines(Calculator.DraftExpression, Calculator.DraftResult) : null, animate: false);
            }
        }

        // T07: from Bea's computation on, her sheet takes the card's place on the clipboard.
        // Revision: the budget practice is retired; the card stays only to show tapes attached before it (references, never a task).
        bool earlierTapes = Level3PracticeRules.NextSlot(chapter) != Level3PracticeRules.Slots[0];
        bool visible = LessonOpen || ((phase == Level3Phase.ToolPractice || earlierTapes) && phase < Level3Phase.BeaComputation);
        // Revised T03: the card prints the example while the lesson is open, else the retired practice (kept tapes only).
        bool rebuilt = card.Spec != DesiredCard && mode == Mode.Resting;
        if (rebuilt) RebuildCard();
        if (card.Root.gameObject.activeSelf != visible) card.Root.gameObject.SetActive(visible);
        if (card.Spec.IsLesson)
        {
            SyncLessonCard(force || rebuilt);
            return;
        }
        foreach (Level3PracticeSlot slot in Level3PracticeRules.Slots)
        {
            Level3TapeRecord tape = chapter.Tape(slot.DestinationId);
            if (tape == null || flyDestination == slot.DestinationId) continue;
            card.ShowTape(slot.DestinationId, Level3Calculator.TapeLines(tape.expression, null), tape.result);
        }
        bool complete = Level3PracticeRules.IsComplete(chapter);
        bool bothTapes = Level3PracticeRules.NextSlot(chapter) == null;
        card.SetStatus(bothTapes ? "PRACTICE COMPLETE" : "");
        if (away) card.SetMessage("The calculator is with you at Bea's table, beside her worksheet.");
        else if (complete) card.SetMessage(bothTapes ? "Done. Bea's records are waiting in the plaza." : "Your earlier practice tape, kept for reference.");
        else if (force)
        {
            Level3PracticeSlot next = Level3PracticeRules.NextSlot(chapter);
            card.SetMessage(next != null && next.Requires == null
                ? "Tap Budget, then −, then Paper order, then =."
                : "Now find what is left after the courier.");
        }
    }

    // =========================================================
    // FIRST-USE LESSON (revised T03)
    // =========================================================

    private static Level3CardSpec DesiredCard => LessonOpen ? ActiveLesson ?? Level3PracticeRules.LessonCard : Level3PracticeRules.PracticeCard;
    private string shownLessonId;

    /// <summary>Prints the other card in the same place (only while everything rests on the desk).</summary>
    private void RebuildCard()
    {
        Level3TapeTransfer.Cancel(this);
        card.Destroy();
        Level3CardSpec next = DesiredCard;
        // A new demonstration starts on a clear example calculator (Bea's own draft is never touched).
        if (next.IsLesson && !lessonHolding) lessonCalculator.Reset();
        card = Level3PracticeCardModel.Build(transform, props, next);
        BindTapeTransfers();
        Apply(card.Root, cardRest);
        shownPhase = (Level3Phase)(-1);
    }

    /// <summary>The tax-table example's tape is complete only after its last step (+ base tax); earlier = are steps.</summary>
    private bool LessonFinalStep
    {
        get
        {
            List<Level3CalcToken> chain = lessonCalculator.DraftChain;
            return chain != null && chain.Exists(t => t != null && t.IsNumber && t.sourceId == Level3PracticeRules.TaxBase);
        }
    }

    private void SyncLessonCard(bool force)
    {
        Level3CardSpec spec = card.Spec;
        bool done = lessonHolding && heldLesson == spec || spec == Level3PracticeRules.LessonCard && Level3PracticeRules.Taught(Chapter);
        card.SetStatus(done ? "DONE" : "");
        if (!force) return;
        if (done) card.SetMessage(LessonDoneMessage(spec));
        else if (!lessonCalculator.HasDraft) card.SetMessage(LessonStartMessage(spec));
    }

    /// <summary>The card's one line of guidance as a demonstration starts (the steps; never Bea's figures).</summary>
    public static string LessonStartMessage(Level3CardSpec spec) =>
        spec == Level3PracticeRules.TaxCard ? "Taxable income - threshold =,\n× rate =, + base tax =."
        : spec == Level3PracticeRules.CreditCard ? "Tax due - tax withheld, then =.\nThe payment is already income."
        : "Tap amount, then display;\nor drag. Move by the edges.";

    /// <summary>What the finished demonstration showed, while its tape is on show.</summary>
    public static string LessonDoneMessage(Level3CardSpec spec) =>
        spec == Level3PracticeRules.TaxCard ? "Done. The rate taxed only\nthe excess. Now Bea's band."
        : spec == Level3PracticeRules.CreditCard ? "Done. A credit comes off tax,\nnot income. Now Bea's credit."
        : "Done. Bea's sheet comes up next.";

    /// <summary>The card's line after an amount went onto the calculator.</summary>
    public static string LessonSourceMessage(Level3CardSpec spec) =>
        spec == Level3PracticeRules.TaxCard ? "Follow the steps;\npress = after each one."
        : spec == Level3PracticeRules.CreditCard ? "Choose -, then tax withheld;\nthen press =."
        : "Use - and both amounts;\nthen press =.";

    /// <summary>
    /// Follow-up (Arthur first, as Level 2's IncomeToolkit.Demo): Arthur works the example on the real card and the
    /// example calculator while the player watches, one line per step; then everything is put back and the same
    /// example is the player's. Presentation only: nothing is attached, saved or graded except, at the end, the
    /// example's "seen" marker.
    /// </summary>
    private System.Collections.IEnumerator LessonDemo(Level3CardSpec lesson)
    {
        demoRunning = true;
        demoLesson = lesson;
        ReleasePointer();
        Level3TapeTransfer.Cancel(this);
        Disarm();
        lessonCalculator.Reset();
        calculator.SetTape(null, animate: false);
        RefreshDisplay();
        card.SetMessage("Watch Arthur first.");
        yield return DemoWait(0.4f);
        foreach (Level3PracticeRules.LessonDemoStep step in Level3PracticeRules.Demo(lesson))
        {
            if (!string.IsNullOrEmpty(step.SourceId))
            {
                Level3PracticeSource source = System.Array.Find(lesson.Sources, s => s.SourceId == step.SourceId);
                if (source != null)
                {
                    SetSelected(source.SourceId);
                    if (source.Percent) lessonCalculator.SelectSourcePercent(source.SourceId, source.Label, source.Amount);
                    else lessonCalculator.SelectSource(source.SourceId, source.Label, source.Amount);
                    calculator.PopInput();
                    RefreshDisplay();
                }
            }
            if (step.HasKey)
            {
                calculator.PressKey(step.Key, 0.18f);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayKeyboardSFX();
                Level3CalcResult pressed = lessonCalculator.Press(step.Key);
                if (lessonCalculator.EntrySource == null) SetSelected(null);
                if (pressed.Printed)
                {
                    calculator.SetTape(DraftLines(), animate: true, TapeFeedSeconds);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
                }
                RefreshDisplay();
            }
            if (step.Attach && lessonCalculator.HasDraft)
            {
                string slot = Level3PracticeRules.SlotOf(lesson);
                card.ShowTape(slot, Level3Calculator.ChainLines(lessonCalculator.DraftChain), lessonCalculator.DraftResult);
                card.PopSlot(slot);
                calculator.SetTape(null, animate: false);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
            }
            if (!string.IsNullOrEmpty(step.Line)) yield return DemoSay(step.Line);
            else yield return DemoWait(0.6f);
        }
        yield return DemoWait(0.6f);
        // Put the example back: the same task is the player's now.
        ClearDemo();
        Level3BeaTable.Level3Say(Level3PracticeRules.YourTurnLine(lesson), "wow", 5.5f);
        Level3Checkpointer.Active?.CommitDemoSeen(lesson);
        demoRunning = false;
        demoLesson = null;
        lessonDemo = null;
        card.SetMessage(LessonStartMessage(lesson));
    }

    /// <summary>Putting the tools away (or disable) stops Arthur's demonstration; nothing was saved, it plays again next lift.</summary>
    private void StopLessonDemo()
    {
        if (lessonDemo != null) StopCoroutine(lessonDemo);
        lessonDemo = null;
        if (!demoRunning) return;
        ClearDemo();
        demoRunning = false;
        demoLesson = null;
    }

    private void ClearDemo()
    {
        if (card != null && demoLesson != null) card.RemoveTape(Level3PracticeRules.SlotOf(demoLesson));
        lessonCalculator.Reset();
        if (calculator != null)
        {
            calculator.SetTape(null, animate: false);
            SetSelected(null);
            RefreshDisplay();
        }
    }

    /// <summary>One of Arthur's lines, held for a reading time that grows with its length.</summary>
    private static System.Collections.IEnumerator DemoSay(string line)
    {
        float seconds = Mathf.Clamp(1.4f + line.Split(' ').Length * 0.3f, 2.5f, 6.5f);
        Level3BeaTable.Level3Say(line, null, seconds);
        yield return DemoWait(seconds);
    }

    /// <summary>Real time that only runs while nothing pauses the teaching (settings, a conversation, the pause menu).</summary>
    private static System.Collections.IEnumerator DemoWait(float seconds)
    {
        for (float t = 0f; t < seconds;)
        {
            if (!InGameSettings.IsOpen && !DialogueUI.IsAnyDialogueActive && (GameStateMachine.Instance == null || !GameStateMachine.Instance.IsPaused))
                t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Teaching pass: a demonstration became due while the tools are up (Bea's taxable income tape has just
    /// clipped on): once nothing is flying they settle, its card is printed in place, and they lift again.
    /// </summary>
    private void TickLessonSwitch()
    {
        if (card == null || lessonHolding) return;
        Level3CardSpec wanted = DesiredCard;
        if (!switchingLesson)
        {
            if (card.Spec == wanted || !wanted.IsLesson || mode != Mode.Lifted || flyStart >= 0f) return;
            switchingLesson = true;
            liftAfterSettle = AtDesk;
            PutAway();
            return;
        }
        if (mode != Mode.Resting || flyStart >= 0f) return;
        switchingLesson = false;
        SyncCard(force: true);
        RefreshDisplay();
        if (liftAfterSettle && AtDesk) Lift();
        liftAfterSettle = false;
    }

    /// <summary>The lesson's one required action: saved first (CommitCalculatorLesson), only then the tape flies and clips on.</summary>
    private void AttachLesson()
    {
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null) return;
        Level3CardSpec lesson = ActiveLesson;
        // The whole chain (the tax table's steps are one tape); a one-step example is its expression.
        List<Level3CalcToken> expression = Calculator.DraftChain ?? Calculator.DraftExpression;
        string result = Calculator.DraftResult;
        // The example stays on the clipboard through the save and its tape's flight.
        lessonHolding = true;
        heldLesson = lesson;
        Level3Checkpointer.TapeCommit outcome = owner.CommitCalculatorLesson(lesson, expression, result, out string hint);
        Disarm();
        if (outcome == Level3Checkpointer.TapeCommit.Attached)
        {
            lessonTapeLines = Level3Calculator.ChainLines(expression);
            lessonTapeResult = result;
            StartAttachFlight(Level3PracticeRules.SlotOf(lesson), expression, result);
            lessonCalculator.ConsumeDraft();
            card.SetStatus("DONE");
            card.SetMessage(LessonDoneMessage(lesson));
            Level3BeaTable.Level3Say(Level3PracticeRules.DoneLine(lesson), "wow", 5f);
        }
        else
        {
            // Refused or not saved: nothing was taught, the tape stays on the calculator (AlreadyAttached: taught before).
            lessonHolding = false;
            heldLesson = null;
            if (outcome == Level3Checkpointer.TapeCommit.AlreadyAttached) SyncCard(force: true);
            else
            {
                // Follow-up: Arthur explains what to change (as in Level 2's practice); the card just invites a retry.
                card.SetMessage("Not yet. Try again;\nthe tape is still here.");
                Level3BeaTable.Level3Say(hint, null, 5.5f);
            }
        }
        RefreshDisplay();
    }

    /// <summary>
    /// After the saved tape has shown briefly the tools settle on the desk; once at rest (or after an
    /// interruption) the example is done and Bea's sheet takes the clipboard, lifting again if still seated.
    /// </summary>
    private void TickLessonHandOff()
    {
        if (!lessonHolding) return;
        if (mode == Mode.Lifted && lessonSettleAt >= 0f && Time.unscaledTime >= lessonSettleAt)
        {
            lessonSettleAt = -1f;
            liftAfterSettle = AtDesk;
            PutAway();
            return;
        }
        if (mode != Mode.Resting || flyStart >= 0f) return;
        lessonHolding = false;
        heldLesson = null;
        lessonSettleAt = -1f;
        shownPhase = (Level3Phase)(-1);
        SyncCard(force: true);
        SyncLeaf(force: true);
        RefreshDisplay();
        calculator.SetTape(Calculator.HasDraft && Calculator.ShowingResult ? DraftLines() : null, animate: false);
        if (liftAfterSettle && AtDesk) Lift();
        liftAfterSettle = false;
    }

    // =========================================================
    // MOTION
    // =========================================================

    private void Lift()
    {
        if (!AtDesk || mode != Mode.Resting) return;
        // One tool at a time (the first-use example): the floating Case Folder goes back to the bar.
        // Revised T07: Bea's computation is on the folder's Computation page, so there the folder stays out (left).
        if (!ComputationOpen && OfficeHudRuntime.Instance != null && OfficeHudRuntime.Instance.CaseFolderViewOut) OfficeHudRuntime.Instance.CloseCaseFolderView();
        // The example and the case keep separate drafts: show the one in use.
        RefreshDisplay();
        calculator.SetTape(Calculator.HasDraft && Calculator.ShowingResult ? DraftLines() : null, animate: false);
        calculatorLayer = CreateToolLayer(calculator.Root);
        cardLayer = CreateToolLayer(card.Root);
        leafLayer = CreateToolLayer(leaf.Root);
        ComputeLiftPoses();
        calculatorFrom = Capture(calculator.Root);
        cardFrom = Capture(PaperRoot);
        moveStart = Time.unscaledTime;
        moveSeconds = LiftSeconds;
        mode = Mode.Lifting;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDocumentSFX(false);
        GameplayEvents.RaiseGuideCue(CueLifted);
    }

    /// <summary>Back onto the desk (PUT CALCULATOR AWAY, leaving the desk, settings, a conversation).</summary>
    public void PutAway()
    {
        StopLessonDemo();
        if (mode != Mode.Lifted && mode != Mode.Lifting) return;
        ReleasePointer();
        FinishAttachFlight();
        Disarm();
        calculatorFrom = Capture(calculator.Root);
        cardFrom = Capture(PaperRoot);
        calculatorTo = calculatorRest;
        cardTo = cardRest;
        moveStart = Time.unscaledTime;
        moveSeconds = ReturnSeconds;
        mode = Mode.Returning;
        if (AudioManager.Instance != null && AtDesk) AudioManager.Instance.PlayDocumentSFX(true);
    }

    private void Animate()
    {
        if (flyStart >= 0f) AnimateFlight();
        if (mode != Mode.Lifting && mode != Mode.Returning) return;
        float t = Mathf.Clamp01((Time.unscaledTime - moveStart) / moveSeconds);
        float eased = t * t * (3f - 2f * t);
        Apply(calculator.Root, Pose.Lerp(calculatorFrom, calculatorTo, eased));
        Apply(PaperRoot, Pose.Lerp(cardFrom, cardTo, eased));
        if (t < 1f) return;
        mode = mode == Mode.Lifting ? Mode.Lifted : Mode.Resting;
        if (mode == Mode.Resting) ReleaseToolLayers();
    }

    /// <summary>
    /// Card left, calculator (with its tape) right, both facing the camera at
    /// LiftDistance and sized to LiftScreenHeight, shrunk together when the
    /// screen is too narrow for both (tall phones, tablets). T09: with Bea's
    /// computation the two stamps stand in a column between them, included in
    /// the same fit.
    /// </summary>
    private void ComputeLiftPoses()
    {
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        if (view == null) return;
        liftScreenW = Screen.width;
        liftScreenH = Screen.height;
        float visibleH = 2f * LiftDistance * Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float visibleW = visibleH * Mathf.Max(0.3f, view.aspect);
        Bounds cb = calculator.UnitBounds, kb = PaperBounds;
        float sc = LiftScreenHeight * visibleH / cb.size.z;
        float sk = PaperScreenHeight * visibleH / kb.size.z;
        float wc = cb.size.x * sc / visibleW, wk = kb.size.x * sk / visibleW;
        // Revised T07: with no card/leaf showing (Bea's computation is on the Case Folder), the calculator keeps its
        // full size on the right and the left is the folder's.
        bool paperShown = PaperRoot != null && PaperRoot.gameObject.activeSelf;
        if (!paperShown) wk = 0f;
        float room = 0.88f - 3f * ScreenMargin;
        if (wc + wk > room)
        {
            float f = room / (wc + wk);
            sc *= f; sk *= f; wc *= f; wk *= f;
        }
        Quaternion facing = Quaternion.LookRotation(view.transform.up, -view.transform.forward);
        calculatorTo = FacingPose(view, facing, cb, sc, 0.88f - ScreenMargin - wc * 0.5f);
        // Centre the whole clipboard / receipt / calculator group in the usable area left of the hotbar.
        float gap = paperShown ? 0.035f : 0f;
        float left = 0.5f - (wc + wk + gap) * 0.5f;
        if (paperShown) calculatorTo = FacingPose(view, facing, cb, sc, left + wk + gap + wc * 0.5f);
        cardTo = FacingPose(view, facing, kb, sk, left + wk * 0.5f);
    }

    private static Pose FacingPose(Camera view, Quaternion facing, Bounds local, float scale, float viewportX)
    {
        Vector3 center = view.ViewportToWorldPoint(new Vector3(viewportX, LiftCenterY, LiftDistance));
        return new Pose { Position = center - facing * (new Vector3(local.center.x, local.max.y, local.center.z) * scale), Rotation = facing, Scale = scale };
    }

    private static Pose Capture(Transform t) => new Pose { Position = t.position, Rotation = t.rotation, Scale = t.localScale.x };

    private static void Apply(Transform t, Pose pose)
    {
        t.SetPositionAndRotation(pose.Position, pose.Rotation);
        t.localScale = Vector3.one * pose.Scale;
    }

    /// <summary>The printed strip tears off and flies to its slot; the card's own clipped tape appears on landing.</summary>
    private void StartAttachFlight(string destinationId, IReadOnlyList<Level3CalcToken> expression, string result)
    {
        Transform strip = calculator.TapeStrip;
        if (strip == null)
        {
            folderSlot = null;
            SyncCard(force: true);
            if (lessonHolding) lessonSettleAt = Time.unscaledTime + LessonShowSeconds;
            return;
        }
        CapturePrintedStrip();
        // The accepted drop focuses its paper, while the travelling receipt
        // stays above it until the stored clip takes ownership.
        Doc3DLayer.For(folderSlot != null ? folderSlot : PaperRoot)?.BringToFront(false);
        flyingLayer?.BringToFront(false);
        stripHeld = false; stripReturnAt = -1f;
        strip.Find("Tape selected")?.gameObject.SetActive(false);
        flying = strip;
        flyScale = strip.localScale;
        flyFrom = strip.position;
        flyFromRotation = strip.rotation;
        // Revised T07: a Computation slot on the Case Folder page (else the card/leaf slot, as before).
        if (FolderFlightTarget(out Vector3 folderTo, out Quaternion folderRotation, out _, out _)) { flyTo = folderTo; flyToRotation = folderRotation; }
        else
        {
            flyTo = ComputationOpen && leaf != null ? leaf.SlotWorldPosition(LeafField(destinationId)) : card.SlotWorldPosition(destinationId);
            flyToRotation = PaperRoot.rotation;
        }
        flyDestination = destinationId;
        flyStart = Time.unscaledTime;
        SyncLeaf(force: true);
    }

    private void FinishAttachFlight()
    {
        if (flyStart < 0f) return;
        flyStart = Time.unscaledTime - AttachSeconds;
        AnimateFlight();
    }

    private void AnimateFlight()
    {
        float t = Mathf.Clamp01((Time.unscaledTime - flyStart) / AttachSeconds);
        float eased = ReceiptEase(t);
        // Destination follows its paper during return/resolution changes.
        Vector3 landingScale, arcUp;
        float arc;
        if (FolderFlightTarget(out Vector3 folderTo, out Quaternion folderRotation, out Vector3 folderUp, out Vector3 folderScale))
        {
            // Revised T07: the Case Folder's Computation slot (it may move with the folder).
            flyTo = folderTo;
            flyToRotation = folderRotation;
            landingScale = folderScale;
            arcUp = folderUp;
            arc = 0.012f;
        }
        else
        {
            flyTo = ComputationOpen && leaf != null ? leaf.SlotWorldPosition(LeafField(flyDestination)) : card.SlotWorldPosition(flyDestination);
            Vector2 slotSize = ComputationOpen ? Level3ComputationLeafModel.AttachedTapeSize : Level3PracticeCardModel.AttachedTapeSize;
            landingScale = FitReceiptScale(calculator.PrintedTapeSize, slotSize, PaperRoot.lossyScale.x);
            flyTo += PaperRoot.up * (Level3CalculatorModel.PlacementLift * PaperRoot.lossyScale.y);
            arcUp = PaperRoot.up;
            arc = 0.012f * PaperRoot.lossyScale.x;
        }
        // TapeStrip's origin is at the printer end, not at its visible centre.
        flyTo -= flyToRotation * Vector3.Scale(calculator.PrintedTapeCenter, landingScale);
        if (flying != null)
        {
            flying.SetPositionAndRotation(ReceiptFlightPosition(flyFrom, flyTo, arcUp, arc, t),
                Quaternion.Slerp(flyFromRotation, flyToRotation, eased));
            flying.localScale = Vector3.Lerp(flyScale, landingScale, eased);
        }
        if (t < 1f) return;
        // The command already saved. Return the printer's original view to its
        // owner; the stored destination view takes over only after this landing.
        flyStart = -1f;
        RestorePrintedStrip();
        calculator.SetTape(Calculator.HasDraft && Calculator.ShowingResult ? DraftLines() : null, animate: false);
        flying = null;
        string landed = flyDestination;
        flyDestination = null;
        SyncCard(force: true);
        if (lessonHolding && landed == Level3PracticeRules.SlotOf(heldLesson))
        {
            card.ShowTape(landed, lessonTapeLines, lessonTapeResult);
            card.PopSlot(landed);
            lessonSettleAt = Time.unscaledTime + LessonShowSeconds;
            return;
        }
        if (folderFlight || folderSlot != null || (ComputationOpen && landed != null && Level3CaseFolder.Active != null))
        {
            // Revised T07: the folder page shows the saved result now and settles on its slot.
            folderSlot = null;
            folderFlight = false;
            Level3CaseFolder.Active?.ComputationLanded(LeafField(landed));
            return;
        }
        if (ComputationOpen) { LeafLanded(landed); return; }
        card.PopSlot(landed);
        Level3ChapterCheckpoint chapter = Chapter;
        if (chapter != null && !Level3PracticeRules.IsComplete(chapter))
            card.SetMessage("Value attached. Tap slot 1's tape to use what was left, then take away the courier.");
    }

    // =========================================================
    // PUT CALCULATOR AWAY (HUD pill, same look as Level 2's PUT STAMPS AWAY)
    // =========================================================

    private void UpdatePutAway()
    {
        if (putAwayRoot == null && !TryCreatePutAway()) return;
        bool visible = mode == Mode.Lifted && AtDesk;
        OfficeHudRuntime.Instance?.RegisterDeskAction(putAwayGroup.transform as RectTransform, true, visible);
        putAwayAlpha = Mathf.MoveTowards(putAwayAlpha, visible ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        putAwayGroup.alpha = putAwayAlpha;
        putAwayGroup.interactable = putAwayGroup.blocksRaycasts = visible;
    }

    private bool TryCreatePutAway()
    {
        if (OfficeHudRuntime.Instance == null || OfficeHudRuntime.Instance.SafeRoot == null) return false;
        Transform canvasRoot = OfficeHudRuntime.Instance.SafeRoot.parent;
        if (canvasRoot == null) return false;

        RectTransform safe = HudTheme.CreateRect("Put Calculator Away Safe Area", canvasRoot);
        HudTheme.Stretch(safe);
        safe.gameObject.AddComponent<MobileControlsSafeArea>();
        safe.SetAsLastSibling();
        putAwayRoot = safe.gameObject;

        RectTransform frame = HudTheme.CreateRect("Put Calculator Away", safe);
        frame.anchorMin = frame.anchorMax = new Vector2(1f, 0f);
        frame.pivot = new Vector2(1f, 0f);
        frame.anchoredPosition = new Vector2(-HudTheme.Margin * 2f, HudTheme.Margin * 2f);
        frame.sizeDelta = new Vector2(380f, 84f);
        Image rim = HudTheme.AddImage(frame, HudTheme.Rounded, HudTheme.Brass, 40f);
        rim.raycastTarget = true;
        RectTransform body = HudTheme.CreateRect("Body", frame);
        HudTheme.Stretch(body, 3f);
        HudTheme.AddImage(body, HudTheme.Rounded, new Color32(18, 28, 41, 235), 38f);
        TextMeshProUGUI label = HudTheme.CreateLabel("Label", body, "PUT CALCULATOR AWAY", HudTheme.ValueSize, HudTheme.TextPrimary,
            TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(label.rectTransform, 8f);

        putAwayGroup = frame.gameObject.AddComponent<CanvasGroup>();
        putAwayGroup.alpha = 0f;
        putAwayGroup.interactable = putAwayGroup.blocksRaycasts = false;
        frame.gameObject.AddComponent<HudSortingKeeper>().Order = OfficeHudRuntime.ExitDeskSortingOrder;
        Button click = frame.gameObject.AddComponent<Button>();
        click.targetGraphic = rim;
        click.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
            MobileInputGuard.SuppressWorldTapsThisFrame();
            PutAway();
        });
        return true;
    }
}
