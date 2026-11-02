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
/// the budget practice now, worksheet and tax computations in later tickets.
///
/// THE OBJECTS: a physical calculator (Level3CalculatorModel: the supplied
/// Synty prop with readable keys, display, roll and tape) and, from the
/// calculator task on, the practice clipboard (Level3PracticeCardModel).
/// Both rest on the consultant desk in reach of the seat.
///
/// AT THE DESK: tap either one and both lift toward the seated view, sized
/// to the screen (like Level 2's hand stamps): the card on the left, the
/// calculator with its tape on the right. Then:
/// - tap a card amount: the calculator takes it (its label goes on the tape);
/// - tap keys (one input per tap-release; a held finger never repeats);
/// - = prints the draft tape; tap a card slot to name where it goes, tap the
///   same slot again to attach it (Level3Checkpointer.CommitPracticeTape:
///   saved first, then clipped to the card);
/// - PUT CALCULATOR AWAY, Exit Desk, settings, a conversation or losing focus
///   settles everything back onto the desk and releases the pressed key.
///
/// T07: once Bea's worksheet is collected, her computation sheet
/// (Level3ComputationLeafModel) replaces the practice card on the clipboard;
/// see Level3DeskCalculatorComputation.cs. T08: her certificate, raised from
/// the sheet's tab, owns the touches while it is up (a tap lowers it). T09:
/// the READY / NOT READY hand stamps lift with them (Level3DeskCalculatorReview.cs).
///
/// The practice is not the player's money and is never scored. Guide cues
/// (cue:CalculatorLifted, PracticeSourceSelected, PracticeTapePrinted,
/// PracticeTapeAttached) only describe what the player did.
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
    public const float CardRight = -0.02f;
    // On-screen layout while lifted.
    public const float LiftDistance = 0.62f;
    public const float LiftScreenHeight = 0.62f;
    public const float PracticeScreenHeight = 0.78f;
    public const float LiftCenterY = 0.55f;
    private const float ScreenMargin = 0.03f;
    private const float LiftSeconds = 0.35f;
    private const float ReturnSeconds = 0.3f;
    private const float KeyPressSeconds = 0.08f;
    private const float TapeFeedSeconds = 0.2f;
    private const float AttachSeconds = 0.3f;

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

    /// <summary>The shared arithmetic and draft tape (presentation reads it; it never grades).</summary>
    public Level3Calculator Calculator { get; } = new Level3Calculator();

    public bool Lifted => mode == Mode.Lifted;
    public bool ToolPointerActive => Lifted && AtDesk && !CertificateUp;
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
    private Transform heldTool;
    private HeldOnScreen heldToolBounds;
    private Vector3 heldToolOffset;
    private float heldToolDepth;
    private bool toolDragging;
    private Doc3DLayer calculatorLayer, cardLayer, leafLayer, flyingLayer, pressingLayer;
    private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();
    private Vector2? certificateTapAt;
    private string selectedSource;
    private string armedSlot;
    private Transform flying;
    private Vector3 flyFrom, flyTo;
    private Quaternion flyFromRotation, flyToRotation;
    private float flyStart = -1f;
    private string flyDestination;
    private int shownTapes = -1;
    private Level3Phase shownPhase = (Level3Phase)(-1);

    private CanvasGroup putAwayGroup;
    private GameObject putAwayRoot;
    private float putAwayAlpha;

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (putAwayRoot != null) Destroy(putAwayRoot);
        ReleaseToolLayers();
        props?.Destroy();
    }

    private void OnDisable()
    {
        ReleasePointer();
        SettleStamps();
        ReleaseToolLayers();
        if (mode == Mode.Lifted || mode == Mode.Lifting || mode == Mode.Returning)
        {
            Apply(calculator.Root, calculatorRest);
            Apply(PaperRoot, cardRest);
            if (readyStamp != null) Apply(readyStamp.Root, readyStamp.Rest);
            if (holdStamp != null) Apply(holdStamp.Root, holdStamp.Rest);
            mode = Mode.Resting;
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) ReleasePointer();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) ReleasePointer();
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
        card = Level3PracticeCardModel.Build(transform, props);
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
        BuildStamps(desk, seat, forward, right, facing);

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
        calculator.Tick();
        leaf?.Tick();
        TickStamps();
        UpdatePutAway();

        if (!AtDesk)
        {
            // Exit Desk, settings, a conversation or a pause: settle back onto the desk.
            if (mode == Mode.Lifted || mode == Mode.Lifting) PutAway();
            ReleasePointer();
            return;
        }
        if (mode == Mode.Lifted && (Screen.width != liftScreenW || Screen.height != liftScreenH))
        {
            ReleasePointer();
            ComputeLiftPoses();
            Apply(calculator.Root, calculatorTo);
            Apply(PaperRoot, cardTo);
        }
        if (mode == Mode.Resting || mode == Mode.Lifted) PollPointer();
    }

    private void PollPointer()
    {
        if (!Level2DeskStamps.TryReadPointer(out Vector2 screen, out bool down, out bool up)) return;
        if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Canceled) ||
            (UnityEngine.InputSystem.Touchscreen.current != null &&
             UnityEngine.InputSystem.Touchscreen.current.primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled))
        {
            ReleasePointer();
            return;
        }
        // T08: while Bea's certificate is up it owns the touches; a tap off it lowers it (its own face takes a tap on it).
        if (CertificateUp)
        {
            if (down)
            {
                ReleasePointer();
                certificateTapAt = MobileInputGuard.IsPointerOverUI(screen) ? (Vector2?)null : screen;
            }
            else if (up && certificateTapAt.HasValue)
            {
                float lowerSlop = Mathf.Max(12f, 0.03f * Mathf.Min(Screen.width, Screen.height));
                if ((screen - certificateTapAt.Value).sqrMagnitude <= lowerSlop * lowerSlop)
                {
                    MobileInputGuard.SuppressWorldTapsThisFrame();
                    certificate.Lower();
                }
                certificateTapAt = null;
            }
            return;
        }
        certificateTapAt = null;
        if (down)
        {
            ReleasePointer();
            if (PointerBlocked(screen)) return;
            pressed = Pick(screen);
            pressedAt = screen;
            if (pressed != null && mode == Mode.Lifted)
            {
                calculator.Press(pressed, 10f);
                heldTool = pressed.transform.IsChildOf(calculator.Root) ? calculator.Root :
                    pressed.transform.IsChildOf(PaperRoot) ? PaperRoot : null;
                if (heldTool != null)
                {
                    Doc3DLayer.For(heldTool)?.BringToFront(false);
                    Camera view = HeldOnScreen.ActiveCamera();
                    heldToolDepth = Vector3.Dot(heldTool.position - view.transform.position, view.transform.forward);
                    heldToolOffset = heldTool.position - view.ScreenToWorldPoint(new Vector3(screen.x, screen.y, heldToolDepth));
                    heldToolBounds = HeldOnScreen.ForModel(heldTool, view, screen);
                }
                MobileInputGuard.SuppressWorldTapsThisFrame();
            }
            return;
        }
        if (heldTool != null)
        {
            float dragSlop = Mathf.Max(12f, 0.03f * Mathf.Min(Screen.width, Screen.height));
            if (!toolDragging && (screen - pressedAt).sqrMagnitude > dragSlop * dragSlop)
            {
                toolDragging = true;
                calculator.ReleaseAll();
            }
            if (toolDragging)
            {
                Camera view = HeldOnScreen.ActiveCamera();
                Vector2 clamped = heldToolBounds.Clamp(screen, view.pixelRect);
                heldTool.position = view.ScreenToWorldPoint(new Vector3(clamped.x, clamped.y, heldToolDepth)) + heldToolOffset;
                MobileInputGuard.SuppressWorldTapsThisFrame();
                if (up) ReleasePointer();
                return;
            }
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
        else Activate(target);
    }

    private Collider Pick(Vector2 screen)
    {
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        if (view == null) return null;
        // Composited windows draw in sorting order, independently of physical depth.
        Doc3DLayer top = null;
        foreach (Doc3DLayer layer in Doc3DLayer.All)
            if (layer != null && layer.IsVisible && (top == null || layer.SortingOrder > top.SortingOrder) && layer.ShowsAt(screen, out _)) top = layer;
        if (top != null)
        {
            if (top != calculatorLayer && top != cardLayer && top != leafLayer) return null;
            Collider nearest = null;
            float distance = float.MaxValue;
            foreach (RaycastHit candidate in Physics.RaycastAll(view.ScreenPointToRay(screen), 4f, 1 << Level2DeskStamps.DragLayer, QueryTriggerInteraction.Collide))
                if (candidate.collider.transform.IsChildOf(top.Source) && candidate.distance < distance)
                { nearest = candidate.collider; distance = candidate.distance; }
            return nearest;
        }
        if (!Physics.Raycast(view.ScreenPointToRay(screen), out RaycastHit hit, 4f, 1 << Level2DeskStamps.DragLayer, QueryTriggerInteraction.Collide))
            return null;
        Collider collider = hit.collider;
        bool ours = collider.transform.IsChildOf(calculator.Root) || (card.Root.gameObject.activeInHierarchy && collider.transform.IsChildOf(card.Root)) ||
                    LeafOwns(collider) || StampOwns(collider);
        return ours ? collider : null;
    }

    private void ReleasePointer()
    {
        heldTool = null;
        toolDragging = false;
        pressed = null;
        certificateTapAt = null;
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
        layer.gameObject.AddComponent<GraphicRaycaster>();
        RectTransform target = HudTheme.CreateRect("Tool pointer surface", layer.transform);
        HudTheme.Stretch(target);
        Image image = target.gameObject.AddComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;
        var hit = target.gameObject.AddComponent<Level3DeskToolHitTarget>();
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
        if (pressingLayer != null) Destroy(pressingLayer.gameObject);
        calculatorLayer = cardLayer = leafLayer = flyingLayer = pressingLayer = null;
    }

    // =========================================================
    // ACTIONS
    // =========================================================

    private void Activate(Collider target)
    {
        if (ActivateStamp(target)) return;
        if (calculator.TryKey(target, out Level3CalcKey key))
        {
            calculator.Press(target, KeyPressSeconds);
            PressKey(key);
            return;
        }
        if (ActivateLeaf(target)) return;
        if (card.TrySource(target, out string sourceId))
        {
            UseSource(sourceId);
            return;
        }
        if (card.TrySlot(target, out string destinationId))
        {
            UseSlot(destinationId);
            return;
        }
    }

    private void PressKey(Level3CalcKey key)
    {
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
            if (PracticeOpen && next != null) card.SetMessage("Tap slot " + SlotNumber(next.DestinationId) + " to attach this tape.");
            else if (ComputationOpen) LeafPrinted();
        }
        RefreshDisplay();
    }

    private void UseSource(string sourceId)
    {
        Level3PracticeSource source = null;
        foreach (Level3PracticeSource candidate in Level3PracticeRules.Sources(Chapter))
            if (candidate.SourceId == sourceId) source = candidate;
        if (source == null) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_pickup_02");
        Calculator.SelectSource(source.SourceId, source.Label, source.Amount);
        Disarm();
        SetSelected(sourceId);
        RefreshDisplay();
        GameplayEvents.RaiseGuideCue(CueSourceSelected);
    }

    /// <summary>First tap names the slot (and offers an attached tape's result); a second tap on the same slot attaches.</summary>
    private void UseSlot(string destinationId)
    {
        Level3ChapterCheckpoint chapter = Chapter;
        if (chapter != null && chapter.Tape(destinationId) != null)
        {
            // An attached tape: reopen it as a source (only the first one is ever needed again).
            if (destinationId == Level3PracticeRules.FirstSlot) UseSource(Level3PracticeRules.FirstTapeSource);
            card.SetMessage("Tape " + SlotNumber(destinationId) + ": " + string.Join("  ",
                Level3Calculator.TapeLines(chapter.Tape(destinationId).expression, chapter.Tape(destinationId).result)));
            return;
        }
        if (!PracticeOpen) return;
        if (!Calculator.HasDraft)
        {
            card.SetMessage(Level3PracticeRules.RefuseNoTape);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
            return;
        }
        if (armedSlot != destinationId)
        {
            armedSlot = destinationId;
            card.SetArmed(destinationId);
            card.SetMessage("Attach " + Level3Calculator.Format(Calculator.DraftResult) + " to “" +
                            Level3PracticeRules.Slot(destinationId).Label + "”? Tap the slot again.");
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
            return;
        }
        Attach(destinationId);
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
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_place_01");
                break;
            case Level3Checkpointer.TapeCommit.AlreadyAttached:
                SyncCard(force: true);
                break;
            default:
                // Refused or not saved: the tape stays on the calculator, nothing is attached, no grade.
                card.SetMessage(hint);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
                break;
        }
        RefreshDisplay();
    }

    private void Disarm()
    {
        DisarmLeaf();
        DisarmStamp();
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

    /// <summary>T06: while Bea's worksheet is being built the calculator is on her plaza table (one calculator for the chapter).</summary>
    public static bool AwayAtBeaTable(Level3Phase phase) =>
        phase == Level3Phase.WorksheetDraft || phase == Level3Phase.WorksheetCorrections || phase == Level3Phase.WorksheetReady;

    private void SyncCard(bool force)
    {
        Level3ChapterCheckpoint chapter = Chapter;
        if (chapter == null || card == null) return;
        Level3Phase phase = Level3ChapterRules.PhaseOf(chapter);
        int tapes = chapter.tapes != null ? chapter.tapes.Count : 0;
        if (!force && phase == shownPhase && tapes == shownTapes) return;
        shownPhase = phase;
        shownTapes = tapes;

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
        bool visible = phase >= Level3Phase.ToolPractice && phase < Level3Phase.BeaComputation;
        if (card.Root.gameObject.activeSelf != visible) card.Root.gameObject.SetActive(visible);
        foreach (Level3PracticeSlot slot in Level3PracticeRules.Slots)
        {
            Level3TapeRecord tape = chapter.Tape(slot.DestinationId);
            if (tape == null || flyDestination == slot.DestinationId) continue;
            card.ShowTape(slot.DestinationId, Level3Calculator.TapeLines(tape.expression, null), tape.result);
        }
        bool complete = Level3PracticeRules.IsComplete(chapter);
        card.SetStatus(complete ? "PRACTICE COMPLETE" : "");
        if (away) card.SetMessage("The calculator is with you at Bea's table, beside her worksheet.");
        else if (complete) card.SetMessage("Done. Bea's records are waiting in the plaza.");
        else if (force)
        {
            Level3PracticeSlot next = Level3PracticeRules.NextSlot(chapter);
            card.SetMessage(next != null && next.Requires == null
                ? "Tap Budget, then −, then Paper order, then =."
                : "Now find what is left after the courier.");
        }
    }

    // =========================================================
    // MOTION
    // =========================================================

    private void Lift()
    {
        if (!AtDesk || mode != Mode.Resting) return;
        calculatorLayer = CreateToolLayer(calculator.Root);
        cardLayer = CreateToolLayer(card.Root);
        leafLayer = CreateToolLayer(leaf.Root);
        ComputeLiftPoses();
        calculatorFrom = Capture(calculator.Root);
        cardFrom = Capture(PaperRoot);
        CaptureStamps(lifting: true);
        moveStart = Time.unscaledTime;
        moveSeconds = LiftSeconds;
        mode = Mode.Lifting;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDocumentSFX(false);
        GameplayEvents.RaiseGuideCue(CueLifted);
    }

    /// <summary>Back onto the desk (PUT CALCULATOR AWAY, leaving the desk, settings, a conversation).</summary>
    public void PutAway()
    {
        if (mode != Mode.Lifted && mode != Mode.Lifting) return;
        ReleasePointer();
        Disarm();
        calculatorFrom = Capture(calculator.Root);
        cardFrom = Capture(PaperRoot);
        calculatorTo = calculatorRest;
        cardTo = cardRest;
        CaptureStamps(lifting: false);
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
        AnimateStamps(eased);
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
        bool stamps = StampsShown;
        float ss = StampScreenHeight * visibleH / StampHeight;
        float ws = stamps ? (StampBlockWidth + 0.01f) * ss / visibleW : 0f;
        float room = 1f - (stamps ? 4f : 3f) * ScreenMargin;
        if (wc + wk + ws > room)
        {
            float f = room / (wc + wk + ws);
            sc *= f; sk *= f; ss *= f; wc *= f; wk *= f; ws *= f;
        }
        Quaternion facing = Quaternion.LookRotation(view.transform.up, -view.transform.forward);
        calculatorTo = FacingPose(view, facing, cb, sc, 1f - ScreenMargin - wc * 0.5f);
        cardTo = FacingPose(view, facing, kb, sk, ScreenMargin + wk * 0.5f);
        // The stamps take the middle of the space left between the sheet and the calculator.
        if (stamps) ComputeStampPoses(view, ss, (ScreenMargin + wk + (1f - ScreenMargin - wc)) * 0.5f);
    }

    private static Pose FacingPose(Camera view, Quaternion facing, Bounds local, float scale, float viewportX)
    {
        Vector3 center = view.ViewportToWorldPoint(new Vector3(viewportX, LiftCenterY, LiftDistance));
        return new Pose { Position = center - facing * (local.center * scale), Rotation = facing, Scale = scale };
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
        if (strip == null) { SyncCard(force: true); return; }
        if (flying != null) Destroy(flying.gameObject);
        if (flyingLayer != null) Destroy(flyingLayer.gameObject);
        GameObject copy = Instantiate(strip.gameObject, strip.position, strip.rotation);
        copy.name = "Attaching Tape";
        copy.transform.localScale = strip.lossyScale;
        foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true)) Destroy(collider);
        flying = copy.transform;
        if (mode == Mode.Lifted) flyingLayer = CreateToolLayer(flying, pointerSurface: false);
        flyFrom = strip.position;
        flyFromRotation = strip.rotation;
        flyTo = ComputationOpen && leaf != null ? leaf.SlotWorldPosition(LeafField(destinationId)) : card.SlotWorldPosition(destinationId);
        flyToRotation = PaperRoot.rotation;
        flyDestination = destinationId;
        flyStart = Time.unscaledTime;
        calculator.SetTape(null, animate: false);
    }

    private void AnimateFlight()
    {
        float t = Mathf.Clamp01((Time.unscaledTime - flyStart) / AttachSeconds);
        float eased = 1f - (1f - t) * (1f - t);
        if (flying != null)
        {
            flying.SetPositionAndRotation(Vector3.Lerp(flyFrom, flyTo, eased) + PaperRoot.up * (0.04f * PaperRoot.lossyScale.x * Mathf.Sin(t * Mathf.PI)),
                Quaternion.Slerp(flyFromRotation, flyToRotation, eased));
            flying.localScale = Vector3.Lerp(flying.localScale, Vector3.one * PaperRoot.lossyScale.x * 0.35f, eased * 0.5f);
        }
        if (t < 1f) return;
        if (flying != null) Destroy(flying.gameObject);
        if (flyingLayer != null) Destroy(flyingLayer.gameObject);
        flyingLayer = null;
        flying = null;
        flyStart = -1f;
        string landed = flyDestination;
        flyDestination = null;
        SyncCard(force: true);
        if (ComputationOpen) { LeafLanded(landed); return; }
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
