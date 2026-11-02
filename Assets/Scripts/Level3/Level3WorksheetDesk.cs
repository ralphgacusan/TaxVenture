using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TouchPhase = UnityEngine.TouchPhase;

/// <summary>
/// PURPOSE:
/// Bea's worksheet at her table (T05/T06), on Level3Gameplay/BeaTable. The
/// physical board (Level3WorksheetModel) rests on the near half of her mat,
/// her original papers on the far half, and the shared desk calculator
/// (Level3CalculatorModel, the same Synty body and keys as the office desk)
/// lies on the mat to the board's right while the worksheet is being built.
///
/// SEATED: tap the board and it lifts toward you, sized to the screen (like
/// the desk calculator); her papers are gathered and stop taking touches
/// while it is up. Then:
/// - tap a record tab, then "+ ADD" on a row: the strip is lifted from that
///   record (row/source id and printed amount copied). "+ ADD" on a row that
///   is already on the sheet selects its strip instead;
/// - tap a strip, then a section tray; or drag the strip onto a tray (same
///   command). A drop outside every tray glides it back;
/// - tap a claim note's field: a chip card offers the five reasons (or the
///   other worksheet rows), all alike; the current value keeps a frame;
/// - UNDO takes back the last change; READ ORIGINAL puts the board down and
///   lifts the selected record's original paper;
/// - narrow screens show records+sheet or sheet+notes (the &lt; &gt; tabs).
/// T06 CALCULATOR: tap a money tray's SUBTOTAL slot (or the calculator) and
/// the calculator rises beside the sheet, over the notes column. Its tray
/// wears a brass frame; tap strips to put their amounts on it (each keeps
/// its row and record), use the keys, press =, then tap SUBTOTAL once to name
/// it and again to attach (saved first, then the tape tears off and clips
/// into the slot). PUT CALCULATOR DOWN returns it to the mat.
/// T06 CHECK / HINT / COLLECT: see Level3WorksheetDeskReview.cs.
/// PUT WORKSHEET DOWN, Leave Table, settings, a conversation, a pause or a
/// lost focus settles the board, the calculator and any held strip; nothing
/// is erased.
///
/// Every edit goes through Level3Checkpointer (candidate, signed write,
/// publish); the board only shows the published draft, so a failed write
/// leaves it as it was. Drafting never grades: all sections accept every
/// strip and no cue depends on correctness until the deliberate Check.
/// Guide cues: WorksheetLifted, WorksheetStripLifted, WorksheetStripPlaced,
/// WorksheetNoteAnswered, WorksheetCalcUp, WorksheetCalcSource,
/// WorksheetTapePrinted, WorksheetTapeAttached, WorksheetChecked, WorksheetHint.
/// </summary>
[DefaultExecutionOrder(-40)]
public sealed partial class Level3WorksheetDesk : MonoBehaviour
{
    public const string CueLifted = "WorksheetLifted";
    public const string CueStripLifted = "WorksheetStripLifted";
    public const string CueStripPlaced = "WorksheetStripPlaced";
    public const string CueNoteAnswered = "WorksheetNoteAnswered";
    public const string CueCalcUp = "WorksheetCalcUp";
    public const string CueCalcSource = "WorksheetCalcSource";
    public const string CueTapePrinted = "WorksheetTapePrinted";
    public const string CueTapeAttached = "WorksheetTapeAttached";

    public const float LiftDistance = 0.62f;
    public const float LiftScreenHeight = 0.8f;
    public const float LiftCenterY = 0.54f;
    /// <summary>The calculator stands closer than the board (no overlap in depth), sized like the office desk's.</summary>
    public const float CalcDistance = 0.52f;
    public const float CalcScreenHeight = 0.62f;
    public const float CalcCenterY = 0.55f;
    private const float ScreenMargin = 0.03f;
    private const float CalcGap = 0.02f;
    private const float LiftSeconds = 0.35f;
    private const float ReturnSeconds = 0.3f;
    private const float PageSeconds = 0.25f;
    private const float CalcSeconds = 0.32f;
    private const float KeyPressSeconds = 0.08f;
    private const float TapeFeedSeconds = 0.2f;
    private const float AttachSeconds = 0.3f;
    /// <summary>Narrower than this share of the whole-board scale, the board pages instead of shrinking.</summary>
    private const float PageBelow = 0.85f;

    private enum Mode { Waiting, Resting, Lifting, Lifted, Returning, Gone }
    private enum CalcMode { Resting, Rising, Up, Lowering }

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

    public static Level3WorksheetDesk Instance { get; private set; }

    public bool Lifted => mode == Mode.Lifted;
    public bool Up => mode == Mode.Lifted || mode == Mode.Lifting;
    /// <summary>T06: the calculator stands beside the sheet (rising or up).</summary>
    public bool CalculatorUp => calcMode == CalcMode.Up || calcMode == CalcMode.Rising;

    private Level3BeaTable table;
    private Transform tableRoot;
    private Vector3 restLocal;
    private float restScale;
    private Vector2 matSize = new Vector2(1f, 0.62f);
    private Level3DeskProps props;
    private Level3WorksheetModel model;
    private Mode mode = Mode.Waiting;
    private Pose from, to;
    private float moveStart, moveSeconds;
    private int liftScreenW, liftScreenH;
    private bool paged;
    private int page = 1;

    private Collider pressed;
    private Vector2 pressedAt;
    private string dragRow;
    private bool dragging;
    private string sourceTab;
    private string pickerNote;
    private bool pickerRelated;
    private int shownRevision = -1;

    // T06: the calculator at the table.
    private Level3CalculatorModel calcModel;
    private Level3Calculator ownCalculator;
    private CalcMode calcMode = CalcMode.Resting;
    private Pose calcFrom, calcTo;
    private float calcStart;
    private Vector3 calcRestLocal;
    private float calcRestScale = 1f;
    private string calcSection;
    private string armedSection;
    private Transform flyingTape;
    private Vector3 flyFrom;
    private Quaternion flyFromRotation;
    private float flyStart = -1f;
    private string flySection;

    private CanvasGroup putDownGroup;
    private GameObject putDownRoot;
    private TextMeshProUGUI putDownLabel;
    private float putDownAlpha;

    /// <summary>The shared calculator logic: the office desk's own (one calculator for the chapter), else this table's.</summary>
    private Level3Calculator Calculator
    {
        get
        {
            if (Level3DeskCalculator.Instance != null) return Level3DeskCalculator.Instance.Calculator;
            return ownCalculator ?? (ownCalculator = new Level3Calculator());
        }
    }

    /// <summary>Called by Level3BeaTable once its table is built (rest point on the mat, table-local).</summary>
    public void Init(Level3BeaTable owner, Transform root, Vector3 rest, float scale)
    {
        table = owner;
        tableRoot = root;
        restLocal = rest;
        restScale = scale;
    }

    /// <summary>As above, with the mat size (T06: the calculator rests on the mat beside the board).</summary>
    public void Init(Level3BeaTable owner, Transform root, Vector3 rest, float scale, Vector2 mat)
    {
        Init(owner, root, rest, scale);
        matSize = mat;
    }

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (putDownRoot != null) Destroy(putDownRoot);
        if (flyingTape != null) Destroy(flyingTape.gameObject);
        props?.Destroy();
    }

    private void OnDisable()
    {
        ReleasePointer();
        if (Up) Settle();
        FinishCollectNow();
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
    // BUILD (once Bea's records are on the table)
    // =========================================================

    private static Level3ChapterCheckpoint Chapter => Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
    private static CaseDefinition Definition => CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;

    private void TryBuild()
    {
        Level3ChapterCheckpoint chapter = Chapter;
        CaseDefinition definition = Definition;
        if (table == null || tableRoot == null || chapter == null || definition == null) return;
        if (Level3ChapterRules.PhaseOf(chapter) < Level3Phase.WorksheetDraft) return;
        if (Level3WorksheetCheck.Collected(chapter))
        {
            // T06: the checked worksheet is in Bea's folder now; the table keeps only her originals.
            mode = Mode.Gone;
            return;
        }
        List<Level3WorksheetRow> rows = Level3WorksheetRules.RowsOf(definition);
        if (rows.Count != Level3WorksheetRules.RowIds.Length)
        {
            Debug.LogWarning("[Level3] Bea's worksheet was not built: her case content does not resolve all seven worksheet rows.");
            enabled = false;
            return;
        }
        props = new Level3DeskProps(Level2DeskStamps.DragLayer);
        var holder = new GameObject("Worksheet");
        holder.layer = Level2DeskStamps.DragLayer;
        holder.transform.SetParent(tableRoot, false);
        model = Level3WorksheetModel.Build(holder.transform, props, rows,
            Level3WorksheetRules.ClaimOf(definition, Level3WorksheetRules.PartyNote), Level3WorksheetRules.ClaimOf(definition, Level3WorksheetRules.PrintingNote));
        Apply(model.Root, RestPose());
        BuildCalculator(holder.transform);
        mode = Mode.Resting;
        Level3WorksheetDraft draft = chapter.worksheet;
        sourceTab = !string.IsNullOrEmpty(draft?.selectedSource) ? draft.selectedSource : null;
        model.ShowSource(sourceTab, draft);
        RefreshReview(chapter, animate: false);
        model.Sync(draft, snap: true);
        shownRevision = draft != null ? draft.revision : 0;
        model.SetSelectedStrip(draft?.selectedRow);
        model.SetMessage(DefaultMessage(draft));
        Debug.Log($"[Level3] Bea's worksheet built on her table ({Level3WorksheetModel.BoardWidth:0.00} x {Level3WorksheetModel.BoardDepth:0.00} m, rest scale {restScale:0.00}).");
    }

    /// <summary>
    /// T06: the chapter's calculator, brought to Bea's table (the office desk's
    /// is hidden meanwhile): the supplied Synty body with its keys, display,
    /// roll and tape, resting on the mat to the board's right.
    /// </summary>
    private void BuildCalculator(Transform holder)
    {
        Level3SceneAssets assets = Level3SceneAssets.Load();
        GameObject body = assets != null ? assets.calculatorPrefab : null;
        if (body == null) Debug.LogWarning($"[Level3] Resources/{Level3SceneAssets.ResourcePath} has no calculator prefab; a plain navy body stands in at Bea's table.");
        calcModel = Level3CalculatorModel.Build(holder, body, props);
        Bounds unit = calcModel.UnitBounds;
        float boardHalf = Level3WorksheetModel.BoardWidth * 0.5f * restScale;
        float room = matSize.x * 0.5f - boardHalf - 0.04f;
        calcRestScale = Mathf.Clamp(room / Mathf.Max(0.01f, unit.size.x), 0.6f, 1f);
        // The calculator's left edge (its tape) a hand's width from the board, its middle level with the board's.
        float x = boardHalf + 0.03f - unit.min.x * calcRestScale;
        calcRestLocal = new Vector3(x, restLocal.y - 0.001f, restLocal.z - unit.center.z * calcRestScale);
        Apply(calcModel.Root, CalcRestPose());
        calcModel.SetTape(null, animate: false);
        RefreshCalcDisplay();
    }

    private Pose RestPose() => new Pose
    {
        Position = tableRoot.TransformPoint(restLocal),
        Rotation = tableRoot.rotation,
        Scale = restScale
    };

    private Pose CalcRestPose() => new Pose
    {
        Position = tableRoot.TransformPoint(calcRestLocal),
        Rotation = tableRoot.rotation * Quaternion.Euler(0f, -7f, 0f),
        Scale = calcRestScale
    };

    // =========================================================
    // UPDATE
    // =========================================================

    private bool AtTable => table != null && table.Seated && CameraController.Instance != null &&
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
        if (mode == Mode.Gone)
        {
            TickCollect();
            UpdatePutDown();
            return;
        }

        SyncDraft();
        Animate();
        model.Tick(Time.unscaledDeltaTime);
        if (calcModel != null) calcModel.Tick();
        TickReview();
        TickCollect();
        UpdatePutDown();

        if (!AtTable)
        {
            // Leave Table, settings, a conversation or a pause: settle back onto the mat
            // (a collection already saved just finishes its presentation at once).
            ReleasePointer();
            if (collecting) FinishCollectNow();
            else if (Up) Settle();
            return;
        }
        if (mode == Mode.Lifted && (Screen.width != liftScreenW || Screen.height != liftScreenH))
        {
            ComputeLiftPose();
            Apply(model.Root, to);
            if (calcMode == CalcMode.Up) Apply(calcModel.Root, calcTo);
        }
        if ((mode == Mode.Resting || mode == Mode.Lifted) && !collecting) PollPointer();
    }

    /// <summary>A published change (an edit here, a restore) redraws the board quietly.</summary>
    private void SyncDraft()
    {
        Level3ChapterCheckpoint chapter = Chapter;
        Level3WorksheetDraft draft = chapter?.worksheet;
        int revision = draft != null ? draft.revision : 0;
        if (ReviewChanged(chapter)) RefreshReview(chapter, animate: true);
        if (revision == shownRevision) return;
        shownRevision = revision;
        model.ShowSource(sourceTab, draft);
        model.Sync(draft, snap: false);
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
        float slop = Mathf.Max(12f, 0.03f * Mathf.Min(Screen.width, Screen.height));
        if (down)
        {
            ReleasePointer();
            if (MobileInputGuard.IsPointerOverUI(screen)) return;
            pressed = Pick(screen);
            pressedAt = screen;
            if (pressed == null || mode != Mode.Lifted) return;
            // Keys go down under the finger (and enter only on release over the same key).
            if (calcModel != null && CalculatorUp) calcModel.Press(pressed, 10f);
            model.PressKey(pressed, 10f);
            // A strip can be dragged straight onto a tray (the same command as tap, tap); not while adding up.
            if (!model.PickerOpen && !CalculatorUp && !Revealing && model.TryHit(pressed, out Level3WorksheetModel.Hit hit) &&
                hit.Kind == Level3WorksheetModel.HitKind.Strip)
                dragRow = hit.Id;
            return;
        }
        if (pressed == null) return;
        if (!up)
        {
            if (dragRow == null) return;
            if (!dragging && (screen - pressedAt).sqrMagnitude > slop * slop)
            {
                dragging = true;
                Select(dragRow, quiet: true);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_pickup_02", 0.7f);
            }
            if (dragging && TryBoardPoint(screen, out Vector3 local)) model.DragStrip(dragRow, local);
            return;
        }

        if (dragging)
        {
            string row = dragRow;
            string section = TryBoardPoint(screen, out Vector3 drop) ? model.TrayAt(drop) : null;
            ReleasePointer();
            MobileInputGuard.SuppressWorldTapsThisFrame();
            if (section != null) Place(row, section);
            else if (AudioManager.Instance != null) AudioManager.Instance.PlayExactEffect("sfx_card_return_02", 0.55f);
            return;
        }
        Collider released = Pick(screen);
        Collider target = pressed;
        bool tap = released == target && (screen - pressedAt).sqrMagnitude <= slop * slop;
        ReleasePointer();
        if (!tap) return;
        // The tap is ours: nothing behind the board reacts to it.
        MobileInputGuard.SuppressWorldTapsThisFrame();
        if (Revealing) { SkipReveal(); return; }
        if (mode == Mode.Resting) Lift(calcModel != null && target.transform.IsChildOf(calcModel.Root));
        else Activate(target);
    }

    private Collider Pick(Vector2 screen)
    {
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        if (view == null || model == null) return null;
        if (!Physics.Raycast(view.ScreenPointToRay(screen), out RaycastHit hit, 4f, 1 << Level2DeskStamps.DragLayer, QueryTriggerInteraction.Collide))
            return null;
        if (model.TryHit(hit.collider, out _)) return hit.collider;
        // T06: the calculator's keys and body (resting on the mat, or up beside the sheet).
        return calcModel != null && calcModel.Root.gameObject.activeInHierarchy && hit.collider.transform.IsChildOf(calcModel.Root) ? hit.collider : null;
    }

    /// <summary>The board-local point under the finger on the board's plane.</summary>
    private bool TryBoardPoint(Vector2 screen, out Vector3 local)
    {
        local = default;
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        if (view == null || model == null) return false;
        Ray ray = view.ScreenPointToRay(screen);
        var plane = new Plane(model.Root.up, model.Root.position);
        if (!plane.Raycast(ray, out float distance)) return false;
        local = model.Root.InverseTransformPoint(ray.GetPoint(distance));
        return true;
    }

    private void ReleasePointer()
    {
        pressed = null;
        dragRow = null;
        calcModel?.ReleaseAll();
        model?.ReleaseKeys();
        if (dragging)
        {
            dragging = false;
            model?.EndDrag();
        }
    }

    // =========================================================
    // ACTIONS (presentation; the chapter command decides)
    // =========================================================

    private void Activate(Collider target)
    {
        if (calcModel != null && target.transform.IsChildOf(calcModel.Root))
        {
            DisarmCheck();
            if (CalculatorUp && calcModel.TryKey(target, out Level3CalcKey key))
            {
                calcModel.Press(target, KeyPressSeconds);
                PressCalcKey(key);
            }
            else if (!CalculatorUp) RaiseCalculator(calcSection);
            return;
        }
        if (!model.TryHit(target, out Level3WorksheetModel.Hit hit)) return;
        if (hit.Kind != Level3WorksheetModel.HitKind.Check) DisarmCheck();
        if (hit.Kind == Level3WorksheetModel.HitKind.Undo || hit.Kind == Level3WorksheetModel.HitKind.Hint ||
            hit.Kind == Level3WorksheetModel.HitKind.Check || hit.Kind == Level3WorksheetModel.HitKind.Collect)
            model.PressKey(target, KeyPressSeconds);
        if (model.PickerOpen)
        {
            if (hit.Kind == Level3WorksheetModel.HitKind.Chip) Choose(hit.Id);
            else if (hit.Kind != Level3WorksheetModel.HitKind.None) ClosePicker(); // Cancel, or a tap outside the card.
            return;
        }
        if (CalculatorUp)
        {
            // Adding up: strips are the calculator's sources; the subtotal slots take the tape.
            switch (hit.Kind)
            {
                case Level3WorksheetModel.HitKind.Strip: UseStrip(hit.Id); return;
                case Level3WorksheetModel.HitKind.Subtotal: TapSubtotal(hit.Id); return;
                case Level3WorksheetModel.HitKind.Tray: TargetTray(hit.Id); return;
                default:
                    model.SetMessage("Tap the strips to add them up. PUT CALCULATOR DOWN to move strips or answer the notes.");
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
                    return;
            }
        }
        switch (hit.Kind)
        {
            case Level3WorksheetModel.HitKind.Tab: ShowTab(hit.Id); break;
            case Level3WorksheetModel.HitKind.Add: Extract(hit.Id); break;
            case Level3WorksheetModel.HitKind.ReadOriginal: ReadOriginal(); break;
            case Level3WorksheetModel.HitKind.Strip: Select(model.SelectedStrip == hit.Id ? null : hit.Id, quiet: false); break;
            case Level3WorksheetModel.HitKind.Tray: TapTray(hit.Id); break;
            case Level3WorksheetModel.HitKind.Reason: OpenReasons(hit.Id); break;
            case Level3WorksheetModel.HitKind.Related: OpenRelated(hit.Id); break;
            case Level3WorksheetModel.HitKind.Undo: Undo(); break;
            case Level3WorksheetModel.HitKind.Page: TurnPage(hit.Id == "left" ? 0 : 2); break;
            case Level3WorksheetModel.HitKind.Subtotal: TapSubtotal(hit.Id); break;
            case Level3WorksheetModel.HitKind.Check: TapCheck(); break;
            case Level3WorksheetModel.HitKind.Hint: TapHint(); break;
            case Level3WorksheetModel.HitKind.Collect: TapCollect(); break;
        }
    }

    private void ShowTab(string sourceId)
    {
        sourceTab = sourceId;
        model.ShowSource(sourceTab, Chapter?.worksheet);
        model.SetMessage("Tap + ADD to lift a row from " + Level3WorksheetRules.SourceTabLabel(sourceId) + " onto the worksheet.");
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
    }

    private void Extract(string rowId)
    {
        if (string.IsNullOrEmpty(rowId)) return;
        Level3Checkpointer.TapeCommit outcome = Commit((Level3ChapterCheckpoint c, out string r) =>
            Level3WorksheetRules.Extract(c, CurrentRows, rowId, out r), out string hint);
        switch (outcome)
        {
            case Level3Checkpointer.TapeCommit.Attached:
                Select(rowId, quiet: true);
                model.SetMessage("Lifted: " + model.StripLabel(rowId) + ". Tap a section on the sheet to place it, or drag it there.");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_pickup_02");
                GameplayEvents.RaiseGuideCue(CueStripLifted);
                break;
            case Level3Checkpointer.TapeCommit.AlreadyAttached:
                // One strip per row: the existing one is selected instead of a copy.
                Select(rowId, quiet: true);
                model.SetMessage("Already on the worksheet. Its strip is selected: tap a section to move it.");
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
                break;
            default:
                Refused(hint);
                break;
        }
    }

    private void Select(string rowId, bool quiet)
    {
        model.SetSelectedStrip(rowId);
        if (rowId != null) hintFocus = "dest-" + rowId;
        if (quiet) return;
        string line = rowId != null ? model.StripLabel(rowId) + ". Tap a section to place it." : DefaultMessage(Chapter?.worksheet);
        // T06: a strip the last check marked says so (and where help is), never what its answer is.
        if (rowId != null && Level3WorksheetCheck.MarkFor(Chapter, "dest-" + rowId) == Level3WorksheetCheck.Mark.LookAgain)
            line = model.StripLabel(rowId) + ": marked at your last check. Tap HINT for help, or a section to move it.";
        model.SetMessage(line);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
    }

    private void TapTray(string section)
    {
        string rowId = model.SelectedStrip;
        if (rowId == null)
        {
            model.SetMessage("Tap a strip first, then the section you want it in.");
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
            return;
        }
        Place(rowId, section);
    }

    private void Place(string rowId, string section)
    {
        bool[] staleBefore = StaleNow();
        Level3Checkpointer.TapeCommit outcome = Commit((Level3ChapterCheckpoint c, out string r) =>
            Level3WorksheetRules.Place(c, rowId, section, out r), out string hint);
        switch (outcome)
        {
            case Level3Checkpointer.TapeCommit.Attached:
                Select(null, quiet: true);
                // Neutral: the same words and sound for every section.
                model.SetMessage("Placed in " + Level3WorksheetRules.SectionLabel(section) + ". You can move it any time. " + StaleNote(staleBefore) + NextHint());
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_place_01");
                GameplayEvents.RaiseGuideCue(CueStripPlaced);
                break;
            case Level3Checkpointer.TapeCommit.AlreadyAttached:
                Select(null, quiet: true);
                model.SetMessage("It is already in " + Level3WorksheetRules.SectionLabel(section) + ".");
                break;
            default:
                Refused(hint);
                break;
        }
    }

    private void OpenReasons(string noteId)
    {
        var choices = new List<KeyValuePair<string, string>>();
        foreach (string reason in Level3WorksheetRules.Reasons) choices.Add(new KeyValuePair<string, string>(reason, Level3WorksheetRules.ReasonLabel(reason)));
        pickerNote = noteId;
        pickerRelated = false;
        hintFocus = noteId == Level3WorksheetRules.PartyNote ? "reason-R6" : "reason-R7";
        model.OpenPicker("Why is this claim questionable?", choices, Chapter?.worksheet?.Note(noteId)?.reason);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
    }

    private void OpenRelated(string noteId)
    {
        var choices = new List<KeyValuePair<string, string>>();
        foreach (string rowId in Level3WorksheetRules.RelatedCandidates(noteId)) choices.Add(new KeyValuePair<string, string>(rowId, model.StripLabel(rowId)));
        pickerNote = noteId;
        pickerRelated = true;
        hintFocus = "related-R7";
        model.OpenPicker("Which worksheet entry does it relate to?", choices, Chapter?.worksheet?.Note(noteId)?.related);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
    }

    private void Choose(string value)
    {
        string noteId = pickerNote;
        bool related = pickerRelated;
        ClosePicker();
        if (noteId == null || value == null) return;
        Level3Checkpointer.TapeCommit outcome = Commit((Level3ChapterCheckpoint c, out string r) => related
            ? Level3WorksheetRules.SetRelated(c, noteId, value, out r)
            : Level3WorksheetRules.SetReason(c, noteId, value, out r), out string hint);
        if (outcome == Level3Checkpointer.TapeCommit.Attached)
        {
            model.SetMessage("Noted. You can change it any time. " + NextHint());
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
            GameplayEvents.RaiseGuideCue(CueNoteAnswered);
        }
        else if (outcome != Level3Checkpointer.TapeCommit.AlreadyAttached) Refused(hint);
    }

    private void ClosePicker()
    {
        pickerNote = null;
        model.ClosePicker();
    }

    private void Undo()
    {
        string undone = UndoDescription(Chapter?.worksheet);
        bool[] staleBefore = StaleNow();
        Level3Checkpointer.TapeCommit outcome = Commit((Level3ChapterCheckpoint c, out string r) => Level3WorksheetRules.Undo(c, out r), out string hint);
        if (outcome == Level3Checkpointer.TapeCommit.Attached)
        {
            Select(null, quiet: true);
            model.SetMessage(undone + " " + StaleNote(staleBefore));
            if (AudioManager.Instance != null) AudioManager.Instance.PlayExactEffect("sfx_card_return_02", 0.55f);
        }
        else Refused(hint);
    }

    /// <summary>What the next Undo puts back, in plain words (T06: the undo stack is not only placements now that tapes exist).</summary>
    private string UndoDescription(Level3WorksheetDraft draft)
    {
        if (draft?.undo == null || draft.undo.Count == 0) return "Last change undone.";
        Level3WorksheetUndo last = draft.undo[draft.undo.Count - 1];
        string label = model.Row(last.target)?.Label;
        switch (last.kind)
        {
            case "extract": return "Undone: " + label + " went back to its record.";
            case "place": return "Undone: " + label + (string.IsNullOrEmpty(last.previous) ? " went back to the lifted pile." : " went back to " + Level3WorksheetRules.SectionLabel(last.previous) + ".");
            case "reason": return "Undone: the note's reason is as it was.";
            case "related": return "Undone: the printing note's related entry is as it was.";
            default: return "Last change undone.";
        }
    }

    private bool[] StaleNow()
    {
        Level3ChapterCheckpoint chapter = Chapter;
        return new[] { Level3WorksheetCheck.IsStale(chapter, Level3WorksheetRules.Income), Level3WorksheetCheck.IsStale(chapter, Level3WorksheetRules.Expenses) };
    }

    /// <summary>T06: a move that left a subtotal tape out of date says so (the tape stays, tagged, until replaced).</summary>
    private string StaleNote(bool[] before)
    {
        bool[] now = StaleNow();
        if (now[0] && !before[0] && now[1] && !before[1]) return "Both subtotal tapes are out of date now: recalculate them. ";
        if (now[0] && !before[0]) return "Income's tape is out of date now: recalculate it. ";
        if (now[1] && !before[1]) return "The Allowable expenses tape is out of date now: recalculate it. ";
        return "";
    }

    private void ReadOriginal()
    {
        if (string.IsNullOrEmpty(sourceTab))
        {
            model.SetMessage("Tap a record tab first, then READ ORIGINAL.");
            return;
        }
        string source = sourceTab;
        Settle();
        table.ReadOriginal(source);
    }

    private Level3Checkpointer.TapeCommit Commit(Level3Checkpointer.WorksheetEdit edit, out string hint)
    {
        hint = null;
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null) return Level3Checkpointer.TapeCommit.Refused;
        Level3Checkpointer.TapeCommit outcome = owner.CommitWorksheet(edit, out hint);
        SyncDraft();
        return outcome;
    }

    /// <summary>Not changed (not allowed now, or not saved): a plain line, the board as it was.</summary>
    private void Refused(string hint)
    {
        model.SetMessage(string.IsNullOrEmpty(hint) ? "That can't be changed right now." : hint);
        model.Sync(Chapter?.worksheet, snap: false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
    }

    private static List<Level3WorksheetRow> CurrentRows =>
        Level3WorksheetRules.RowsOf(CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null);

    private static string NextHint()
    {
        Level3WorksheetDraft draft = Chapter?.worksheet;
        if (draft == null) return "";
        if (Level3WorksheetRules.AllFilledIn(draft))
        {
            if (Level3WorksheetCheck.IsCurrent(Chapter, Level3WorksheetRules.Income) && Level3WorksheetCheck.IsCurrent(Chapter, Level3WorksheetRules.Expenses))
                return "Every row has a place, the notes are filled in and both subtotals have tapes.";
            return "Every row has a place and both notes are filled in. Next, the SUBTOTAL slots.";
        }
        if (draft.strips.Count < Level3WorksheetRules.RowIds.Length) return "";
        return Level3WorksheetRules.AllPlaced(draft) ? "Now the claim notes on the right." : "";
    }

    private static string DefaultMessage(Level3WorksheetDraft draft)
    {
        Level3ChapterCheckpoint chapter = Chapter;
        Level3Phase phase = Level3ChapterRules.PhaseOf(chapter);
        if (phase == Level3Phase.WorksheetReady) return "Every decision checks out. Tap COLLECT WORKSHEET to put it in Bea's folder.";
        if (!Level3WorksheetRules.Editable(chapter)) return "This worksheet is set for now.";
        if (phase == Level3Phase.WorksheetCorrections) return "Fix the places marked ?, then CHECK AGAIN. HINT helps with a marked place. Your first grade stays.";
        if (draft == null || draft.strips.Count == 0) return "Tap a record tab on the left, then + ADD to lift a row onto the worksheet.";
        if (!Level3WorksheetRules.AllFilledIn(draft)) return "Tap a strip, then a section. Claim notes are on the right.";
        return NextHint() + (Level3WorksheetCheck.IsCurrent(chapter, Level3WorksheetRules.Income) && Level3WorksheetCheck.IsCurrent(chapter, Level3WorksheetRules.Expenses)
            ? " Tap CHECK when you're ready." : "");
    }

    // =========================================================
    // T06: THE CALCULATOR AT THE TABLE
    // =========================================================

    /// <summary>Brings the calculator up beside the sheet, adding up a tray (null: none chosen yet).</summary>
    private void RaiseCalculator(string section)
    {
        if (calcModel == null) return;
        if (!Level3WorksheetRules.Editable(Chapter))
        {
            model.SetMessage(Level3ChapterRules.PhaseOf(Chapter) == Level3Phase.WorksheetReady
                ? "Every decision checks out: the tapes are set. Tap COLLECT WORKSHEET." : "The worksheet cannot be changed right now.");
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
            return;
        }
        ClosePicker();
        Select(null, quiet: true);
        calcSection = section;
        model.SetAdding(section);
        if (!CalculatorUp)
        {
            calcMode = CalcMode.Rising;
            calcFrom = Capture(calcModel.Root);
            from = Capture(model.Root);
            ComputeLiftPose();
            calcStart = moveStart = Time.unscaledTime;
            moveSeconds = CalcSeconds;
            if (mode == Mode.Lifted) mode = Mode.Lifting;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayDocumentSFX(false);
            GameplayEvents.RaiseGuideCue(CueCalcUp);
        }
        RefreshCalcDisplay();
        model.SetMessage(section != null ? AddingMessage(section) : "Tap a SUBTOTAL slot to choose which section to add up.");
    }

    /// <summary>The calculator goes back onto the mat; the board returns to its usual place. Its draft stays on the display.</summary>
    private void LowerCalculator()
    {
        if (!CalculatorUp || calcModel == null) return;
        DisarmSubtotal();
        calcModel.ReleaseAll();
        calcMode = CalcMode.Lowering;
        calcFrom = Capture(calcModel.Root);
        calcTo = CalcRestPose();
        calcStart = Time.unscaledTime;
        model.SetAdding(null);
        calcSection = null;
        if (mode == Mode.Lifted || mode == Mode.Lifting)
        {
            from = Capture(model.Root);
            ComputeLiftPose();
            moveStart = Time.unscaledTime;
            moveSeconds = CalcSeconds;
            mode = Mode.Lifting;
        }
        if (AudioManager.Instance != null && AtTable) AudioManager.Instance.PlayDocumentSFX(true);
        model.SetMessage(DefaultMessage(Chapter?.worksheet));
    }

    private void TargetTray(string section)
    {
        if (Level3WorksheetCheck.TapeFor(section) != null)
        {
            calcSection = section;
            model.SetAdding(section);
            model.SetMessage(AddingMessage(section));
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
            return;
        }
        model.SetMessage("Only Income and Allowable expenses are added up. Tap one of their SUBTOTAL slots.");
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
    }

    private void PressCalcKey(Level3CalcKey key)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayKeyboardSFX();
        Level3CalcResult result = Calculator.Press(key);
        DisarmSubtotal();
        if (result.Printed)
        {
            calcModel.SetTape(Level3Calculator.TapeLines(Calculator.DraftExpression, Calculator.DraftResult), animate: true, TapeFeedSeconds);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
            GameplayEvents.RaiseGuideCue(CueTapePrinted);
            model.SetMessage(calcSection != null
                ? "Tape printed: = " + Level3Calculator.Format(Calculator.DraftResult) + ". Tap " + Level3WorksheetRules.SectionLabel(calcSection) + "'s SUBTOTAL to attach it."
                : "Tape printed. Tap a SUBTOTAL slot to attach it.");
        }
        RefreshCalcDisplay();
    }

    /// <summary>A strip's printed amount goes onto the calculator, keeping its row and record (never typed).</summary>
    private void UseStrip(string rowId)
    {
        Level3WorksheetRow row = model.Row(rowId);
        if (row == null) return;
        Calculator.SelectSource(Level3WorksheetCheck.StripSource(rowId), row.Label, row.Amount);
        DisarmSubtotal();
        model.PulseStrip(rowId);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_pickup_02", 0.6f);
        RefreshCalcDisplay();
        GameplayEvents.RaiseGuideCue(CueCalcSource);
    }

    /// <summary>First tap names the subtotal (and shows its tape); with a printed tape, a second tap attaches it.</summary>
    private void TapSubtotal(string section)
    {
        if (Level3WorksheetCheck.TapeFor(section) == null) return;
        if (!CalculatorUp)
        {
            RaiseCalculator(section);
            return;
        }
        if (section != calcSection)
        {
            calcSection = section;
            model.SetAdding(section);
            DisarmSubtotal();
        }
        if (!Calculator.HasDraft)
        {
            model.SetMessage(AddingMessage(section));
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
            return;
        }
        if (armedSection != section)
        {
            armedSection = section;
            model.SetArmedSubtotal(section);
            model.SetMessage("Attach " + Level3Calculator.Format(Calculator.DraftResult) + " to the " + Level3WorksheetRules.SectionLabel(section) +
                             " subtotal? Tap SUBTOTAL again.");
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
            return;
        }
        AttachTape(section);
    }

    private void AttachTape(string section)
    {
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null) return;
        List<Level3CalcToken> expression = Calculator.DraftExpression;
        string result = Calculator.DraftResult;
        Level3Checkpointer.TapeCommit outcome = owner.CommitWorksheetTape(section, expression, result, out string hint);
        DisarmSubtotal();
        switch (outcome)
        {
            case Level3Checkpointer.TapeCommit.Attached:
                // Saved first; only now does the strip tear off and clip into the slot.
                StartTapeFlight(section);
                Calculator.ConsumeDraft();
                model.SetMessage("Tape attached to " + Level3WorksheetRules.SectionLabel(section) + ". " + AfterAttachHint());
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_card_place_01");
                GameplayEvents.RaiseGuideCue(CueTapeAttached);
                break;
            case Level3Checkpointer.TapeCommit.AlreadyAttached:
                model.SetMessage("That tape is already on " + Level3WorksheetRules.SectionLabel(section) + ".");
                break;
            default:
                // Refused or not saved: the tape stays on the calculator, nothing is attached, no grade.
                model.SetMessage(hint);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
                break;
        }
        RefreshCalcDisplay();
    }

    private static string AfterAttachHint()
    {
        Level3ChapterCheckpoint chapter = Chapter;
        bool income = Level3WorksheetCheck.IsCurrent(chapter, Level3WorksheetRules.Income);
        bool expenses = Level3WorksheetCheck.IsCurrent(chapter, Level3WorksheetRules.Expenses);
        if (income && expenses) return "Both subtotals have tapes. PUT CALCULATOR DOWN, then CHECK when you're ready.";
        return income ? "Now Allowable expenses' SUBTOTAL." : "Now Income's SUBTOTAL.";
    }

    private void DisarmSubtotal()
    {
        if (armedSection == null) return;
        armedSection = null;
        model?.SetArmedSubtotal(null);
    }

    private string AddingMessage(string section)
    {
        Level3ChapterCheckpoint chapter = Chapter;
        string name = Level3WorksheetRules.SectionLabel(section);
        int count = Level3WorksheetRules.InSection(chapter?.worksheet, section).Count;
        string lead = Level3WorksheetCheck.IsStale(chapter, section) ? "Its entries changed, so make a new tape. "
            : Level3WorksheetCheck.IsCurrent(chapter, section) ? "It has a tape (= " + Level3Calculator.Format(Level3WorksheetCheck.SubtotalTape(chapter, section).result) + "); a new one replaces it. "
            : "";
        if (count == 0) return "Adding up " + name + ": " + lead + "it has no strips. Type 0, press =, and attach it for an empty section.";
        return "Adding up " + name + ": " + lead + "tap each of its " + count + (count == 1 ? " strip" : " strips") + " with + between them, then =.";
    }

    private void RefreshCalcDisplay()
    {
        if (calcModel == null) return;
        calcModel.SetDisplay(Calculator.DisplayText, Calculator.Caption);
    }

    /// <summary>The printed strip tears off the calculator and flies to its tray's SUBTOTAL slot; the clipped tape appears on landing.</summary>
    private void StartTapeFlight(string section)
    {
        Transform strip = calcModel != null ? calcModel.TapeStrip : null;
        if (strip == null) { RefreshReview(Chapter, animate: false); return; }
        if (flyingTape != null) Destroy(flyingTape.gameObject);
        GameObject copy = Instantiate(strip.gameObject, strip.position, strip.rotation);
        copy.name = "Attaching Tape";
        copy.transform.localScale = strip.lossyScale;
        foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true)) Destroy(collider);
        flyingTape = copy.transform;
        flyFrom = strip.position;
        flyFromRotation = strip.rotation;
        flySection = section;
        flyStart = Time.unscaledTime;
        calcModel.SetTape(null, animate: false);
        model.SyncSubtotals(Chapter, flySection, animate: false);
    }

    private void AnimateTapeFlight()
    {
        if (flyStart < 0f) return;
        float t = Mathf.Clamp01((Time.unscaledTime - flyStart) / AttachSeconds);
        float eased = 1f - (1f - t) * (1f - t);
        if (flyingTape != null)
        {
            Vector3 target = model.SubtotalWorldPosition(flySection);
            flyingTape.SetPositionAndRotation(Vector3.Lerp(flyFrom, target, eased) + model.Root.up * (0.05f * model.Root.lossyScale.x * Mathf.Sin(t * Mathf.PI)),
                Quaternion.Slerp(flyFromRotation, model.SubtotalWorldRotation, eased));
            flyingTape.localScale = Vector3.Lerp(flyingTape.localScale, Vector3.one * model.Root.lossyScale.x * 0.3f, eased * 0.5f);
        }
        if (t < 1f) return;
        if (flyingTape != null) Destroy(flyingTape.gameObject);
        flyingTape = null;
        flyStart = -1f;
        string landed = flySection;
        flySection = null;
        model.SyncSubtotals(Chapter, null, animate: false);
        model.PopSubtotal(landed);
    }

    // =========================================================
    // MOTION
    // =========================================================

    /// <summary>Lifts the board (and, when the calculator was tapped, the calculator beside it).</summary>
    private void Lift(bool withCalculator = false)
    {
        if (!AtTable || mode != Mode.Resting) return;
        table.HoldPapers(true);
        page = 1;
        if (withCalculator && calcModel != null && Level3WorksheetRules.Editable(Chapter))
        {
            calcMode = CalcMode.Rising;
            calcFrom = Capture(calcModel.Root);
            calcStart = Time.unscaledTime;
            model.SetAdding(calcSection);
            GameplayEvents.RaiseGuideCue(CueCalcUp);
        }
        ComputeLiftPose();
        from = Capture(model.Root);
        moveStart = Time.unscaledTime;
        moveSeconds = LiftSeconds;
        mode = Mode.Lifting;
        model.SetMessage(CalculatorUp ? "Tap a SUBTOTAL slot to choose which section to add up." : DefaultMessage(Chapter?.worksheet));
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDocumentSFX(false);
        GameplayEvents.RaiseGuideCue(CueLifted);
    }

    /// <summary>Back onto the mat (PUT WORKSHEET DOWN, READ ORIGINAL, leaving the table, settings, a conversation).</summary>
    public void Settle()
    {
        if (!Up || model == null) return;
        ReleasePointer();
        ClosePicker();
        DisarmCheck();
        SkipReveal();
        if (CalculatorUp) LowerCalculator();
        model.Sync(Chapter?.worksheet, snap: true);
        from = Capture(model.Root);
        to = RestPose();
        moveStart = Time.unscaledTime;
        moveSeconds = ReturnSeconds;
        mode = Mode.Returning;
        model.SetPaged(false);
        table.HoldPapers(false);
        if (AudioManager.Instance != null && AtTable) AudioManager.Instance.PlayDocumentSFX(true);
    }

    private void TurnPage(int next)
    {
        if (!paged || next == page) return;
        page = next;
        from = Capture(model.Root);
        ComputeLiftPose();
        moveStart = Time.unscaledTime;
        moveSeconds = PageSeconds;
        mode = Mode.Lifting;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
    }

    private void Animate()
    {
        AnimateTapeFlight();
        if (calcModel != null && (calcMode == CalcMode.Rising || calcMode == CalcMode.Lowering))
        {
            float c = Mathf.Clamp01((Time.unscaledTime - calcStart) / CalcSeconds);
            float ce = c * c * (3f - 2f * c);
            if (calcMode == CalcMode.Lowering) calcTo = CalcRestPose();
            Apply(calcModel.Root, Pose.Lerp(calcFrom, calcTo, ce));
            if (c >= 1f) calcMode = calcMode == CalcMode.Rising ? CalcMode.Up : CalcMode.Resting;
        }
        if (mode != Mode.Lifting && mode != Mode.Returning) return;
        float t = Mathf.Clamp01((Time.unscaledTime - moveStart) / moveSeconds);
        float eased = t * t * (3f - 2f * t);
        // Resting follows the table (it never moves, but the pose stays exact).
        if (mode == Mode.Returning) to = RestPose();
        Apply(model.Root, Pose.Lerp(from, to, eased));
        if (t < 1f) return;
        mode = mode == Mode.Lifting ? Mode.Lifted : Mode.Resting;
    }

    /// <summary>
    /// The whole board facing the camera at LiftDistance, LiftScreenHeight
    /// tall; on a screen too narrow for that, two of its three columns at a
    /// time (records+sheet or sheet+notes) instead of shrinking it all.
    /// T06: with the calculator up, the sheet and the calculator share the
    /// screen (the calculator on the right, closer, over the notes column);
    /// both shrink together only if the screen is too narrow for both.
    /// </summary>
    private void ComputeLiftPose()
    {
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        if (view == null) return;
        liftScreenW = Screen.width;
        liftScreenH = Screen.height;
        float visibleH = 2f * LiftDistance * Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float visibleW = visibleH * Mathf.Max(0.3f, view.aspect);
        Bounds local = model.UnitBounds;
        Quaternion facing = Quaternion.LookRotation(view.transform.up, -view.transform.forward);
        float byHeight = LiftScreenHeight * visibleH / local.size.z;
        if (CalculatorUp && calcModel != null)
        {
            float calcH = 2f * CalcDistance * Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float calcW = calcH * Mathf.Max(0.3f, view.aspect);
            Bounds cb = calcModel.UnitBounds;
            float boardScale = byHeight;
            float calcScale = CalcScreenHeight * calcH / cb.size.z;
            float ws = Level3WorksheetModel.SheetWidth * boardScale / visibleW;
            float wc = cb.size.x * calcScale / calcW;
            float room = 1f - 2f * ScreenMargin - CalcGap;
            if (ws + wc > room)
            {
                float f = room / (ws + wc);
                boardScale *= f; calcScale *= f; ws *= f; wc *= f;
            }
            float calcX = 1f - ScreenMargin - wc * 0.5f;
            float sheetX = Mathf.Min(0.5f, calcX - wc * 0.5f - CalcGap - ws * 0.5f);
            model.SetPaged(false);
            Vector3 sheetCenter = view.ViewportToWorldPoint(new Vector3(sheetX, LiftCenterY, LiftDistance));
            to = new Pose { Position = sheetCenter - facing * (local.center * boardScale), Rotation = facing, Scale = boardScale };
            Vector3 calcCenter = view.ViewportToWorldPoint(new Vector3(calcX, CalcCenterY, CalcDistance));
            calcTo = new Pose { Position = calcCenter - facing * (cb.center * calcScale), Rotation = facing, Scale = calcScale };
            return;
        }
        float byWidth = (1f - 2f * ScreenMargin) * visibleW / local.size.x;
        paged = byWidth < byHeight * PageBelow;
        float scale = paged ? Mathf.Min(byHeight, (1f - 2f * ScreenMargin) * visibleW / Level3WorksheetModel.PageWidth) : Mathf.Min(byHeight, byWidth);
        if (!paged) page = 1;
        else if (page == 1) page = 0;
        model.SetPaged(paged);
        Vector3 center = view.ViewportToWorldPoint(new Vector3(0.5f, LiftCenterY, LiftDistance));
        Vector3 focus = new Vector3(local.center.x + (paged ? Level3WorksheetModel.PageCenter(page) : 0f), local.center.y, local.center.z);
        to = new Pose { Position = center - facing * (focus * scale), Rotation = facing, Scale = scale };
    }

    private static Pose Capture(Transform t) => new Pose { Position = t.position, Rotation = t.rotation, Scale = t.localScale.x };

    private static void Apply(Transform t, Pose pose)
    {
        t.SetPositionAndRotation(pose.Position, pose.Rotation);
        t.localScale = Vector3.one * pose.Scale;
    }

    // =========================================================
    // PUT WORKSHEET DOWN (HUD pill, same look as PUT CALCULATOR AWAY)
    // =========================================================

    private void UpdatePutDown()
    {
        if (putDownRoot == null && !TryCreatePutDown()) return;
        bool visible = mode == Mode.Lifted && AtTable && !collecting;
        putDownAlpha = Mathf.MoveTowards(putDownAlpha, visible ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        putDownGroup.alpha = putDownAlpha;
        putDownGroup.interactable = putDownGroup.blocksRaycasts = visible;
        // T06: while the calculator is up, the pill puts the calculator down first.
        string label = CalculatorUp ? "PUT CALCULATOR DOWN" : "PUT WORKSHEET DOWN";
        if (putDownLabel != null && putDownLabel.text != label) putDownLabel.text = label;
    }

    private bool TryCreatePutDown()
    {
        if (OfficeHudRuntime.Instance == null || OfficeHudRuntime.Instance.SafeRoot == null) return false;
        Transform canvasRoot = OfficeHudRuntime.Instance.SafeRoot.parent;
        if (canvasRoot == null) return false;

        RectTransform safe = HudTheme.CreateRect("Put Worksheet Down Safe Area", canvasRoot);
        HudTheme.Stretch(safe);
        safe.gameObject.AddComponent<MobileControlsSafeArea>();
        safe.SetAsLastSibling();
        putDownRoot = safe.gameObject;

        RectTransform frame = HudTheme.CreateRect("Put Worksheet Down", safe);
        frame.anchorMin = frame.anchorMax = new Vector2(1f, 0f);
        frame.pivot = new Vector2(1f, 0f);
        frame.anchoredPosition = new Vector2(-HudTheme.Margin * 2f, HudTheme.Margin * 2f);
        frame.sizeDelta = new Vector2(430f, 84f);
        Image rim = HudTheme.AddImage(frame, HudTheme.Rounded, HudTheme.Brass, 40f);
        rim.raycastTarget = true;
        RectTransform body = HudTheme.CreateRect("Body", frame);
        HudTheme.Stretch(body, 3f);
        HudTheme.AddImage(body, HudTheme.Rounded, new Color32(18, 28, 41, 235), 38f);
        TextMeshProUGUI label = HudTheme.CreateLabel("Label", body, "PUT WORKSHEET DOWN", HudTheme.ValueSize, HudTheme.TextPrimary,
            TextAlignmentOptions.Center, FontStyles.Bold);
        HudTheme.Stretch(label.rectTransform, 8f);
        putDownLabel = label;

        putDownGroup = frame.gameObject.AddComponent<CanvasGroup>();
        putDownGroup.alpha = 0f;
        putDownGroup.interactable = putDownGroup.blocksRaycasts = false;
        Button click = frame.gameObject.AddComponent<Button>();
        click.targetGraphic = rim;
        click.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
            MobileInputGuard.SuppressWorldTapsThisFrame();
            if (CalculatorUp) LowerCalculator();
            else Settle();
        });
        return true;
    }
}
