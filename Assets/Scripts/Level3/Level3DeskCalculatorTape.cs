using System;
using UnityEngine;

public sealed partial class Level3DeskCalculator
{

    private Transform stripParent;
    private int stripSibling;
    private Vector3 stripPosition, stripScale, stripOffset, returnFrom;
    private Quaternion stripRotation, stripHeldRotation, returnRotation;
    private Vector3 stripHeldScale, returnScale, stripVelocity;
    private HeldOnScreen stripBounds;
    private Vector2 stripScreen;
    private float stripPickupAt;
    private const float StripPickupSeconds = 0.14f;
    private const float StripReturnSeconds = 0.24f;
    private float stripDepth, stripReturnAt = -1f;
    private string stripKey;
    private bool stripCaptured, stripHeld;
    public bool PhysicalReceiptVisible => stripCaptured;
    private Collider[] stripColliders;
    private bool[] stripColliderStates;
    private Renderer[] stripRenderers;
    private bool[] stripRendererStates;

    private void CapturePrintedStrip()
    {
        if (stripCaptured) return;
        Transform strip = calculator.TapeStrip;
        stripParent = strip.parent; stripSibling = strip.GetSiblingIndex();
        stripPosition = strip.localPosition; stripRotation = strip.localRotation; stripScale = strip.localScale;
        stripKey = PrintedKey;
        stripColliders = strip.GetComponentsInChildren<Collider>(true);
        stripColliderStates = new bool[stripColliders.Length];
        for (int i = 0; i < stripColliders.Length; i++)
        { stripColliderStates[i] = stripColliders[i].enabled; stripColliders[i].enabled = false; }
        stripRenderers = strip.GetComponentsInChildren<Renderer>(true);
        stripRendererStates = new bool[stripRenderers.Length];
        for (int i = 0; i < stripRenderers.Length; i++) stripRendererStates[i] = stripRenderers[i].enabled;
        stripCaptured = true;
        strip.SetParent(transform, true);
        flyingLayer = CreateToolLayer(strip, pointerSurface: false);
        flyingLayer.KeepInFront = true;
    }
    public void BeginPrintedStrip(Vector2 screen)
    {
        if (!CanDragPrintedTape) return;
        RestorePrintedStrip();
        CapturePrintedStrip();
        stripHeld = true;
        Camera view = HeldOnScreen.ActiveCamera();
        Transform strip = calculator.TapeStrip;
        stripDepth = Vector3.Dot(strip.position - view.transform.position, view.transform.forward);
        stripOffset = strip.position - view.ScreenToWorldPoint(new Vector3(screen.x, screen.y, stripDepth));
        stripHeldRotation = strip.rotation; stripHeldScale = strip.localScale;
        stripScreen = screen; stripVelocity = Vector3.zero; stripPickupAt = Time.unscaledTime;
        // Measure the fully lifted view once. Its eventual bounds, including
        // finger clearance and tilt, constrain the pointer via the shared clamp.
        Vector3 position = strip.position;
        strip.position += StripLift(view);
        strip.rotation = stripHeldRotation * Quaternion.Euler(0f, 0f, -3f);
        strip.localScale = stripHeldScale * 1.025f;
        stripBounds = HeldOnScreen.ForModel(strip, view, screen);
        strip.SetPositionAndRotation(position, stripHeldRotation);
        strip.localScale = stripHeldScale;
    }
    private static Vector3 StripLift(Camera view) => view.transform.up * 0.012f - view.transform.forward * 0.006f;
    public void MovePrintedStrip(Vector2 screen)
    {
        if (!stripHeld || !ToolPointerActive) return;
        if (stripKey != PrintedKey) { Level3TapeTransfer.Cancel(this); RestorePrintedStrip(); return; }
        stripScreen = screen; // discovery still uses the real, unclamped pointer
    }
    public static float ReceiptEase(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }
    public static Vector3 ReceiptFlightPosition(Vector3 from, Vector3 to, Vector3 up, float lift, float t)
    {
        t = Mathf.Clamp01(t);
        float arch = 16f * t * t * (1f - t) * (1f - t);
        return Vector3.Lerp(from, to, ReceiptEase(t)) + up * (lift * arch);
    }
    public static Vector3 FitReceiptScale(Vector2 printed, Vector2 slot, float paperScale) =>
        new Vector3(slot.x / printed.x, 1f, slot.y / printed.y) * paperScale;
    public enum StripRelease { None, Flight, Restore, Return }
    public static StripRelease ResolveStripRelease(bool captured, bool committed, bool canceled, bool current) =>
        !captured ? StripRelease.None : committed ? StripRelease.Flight : canceled || !current ? StripRelease.Restore : StripRelease.Return;
    public void EndPrintedStrip(bool cancel)
    {
        StripRelease release = ResolveStripRelease(stripCaptured, flyStart >= 0f, cancel, stripKey == PrintedKey);
        if (release == StripRelease.None || release == StripRelease.Flight) return;
        stripHeld = false;
        if (release == StripRelease.Restore) { RestorePrintedStrip(); return; }
        returnFrom = calculator.TapeStrip.position;
        returnRotation = calculator.TapeStrip.rotation;
        returnScale = calculator.TapeStrip.localScale;
        stripReturnAt = Time.unscaledTime;
    }
    private void TickPrintedStrip()
    {
        if (!stripCaptured || flyStart >= 0f) return;
        if (stripKey != PrintedKey) { Level3TapeTransfer.Cancel(this); RestorePrintedStrip(); return; }
        Transform strip = calculator.TapeStrip;
        if (stripHeld)
        {
            Camera view = HeldOnScreen.ActiveCamera();
            Vector2 screen = stripBounds.Clamp(stripScreen, view.pixelRect);
            float lift = ReceiptEase((Time.unscaledTime - stripPickupAt) / StripPickupSeconds);
            Vector3 target = view.ScreenToWorldPoint(new Vector3(screen.x, screen.y, stripDepth)) + stripOffset + StripLift(view) * lift;
            strip.position = Vector3.SmoothDamp(strip.position, target, ref stripVelocity, 0.035f, Mathf.Infinity, Time.unscaledDeltaTime);
            strip.rotation = Quaternion.Slerp(stripHeldRotation, stripHeldRotation * Quaternion.Euler(0f, 0f, -3f), lift);
            strip.localScale = stripHeldScale * Mathf.Lerp(1f, 1.025f, lift);
            return;
        }
        if (stripReturnAt < 0f) return;
        float t = Mathf.Clamp01((Time.unscaledTime - stripReturnAt) / StripReturnSeconds);
        float eased = ReceiptEase(t);
        strip.position = ReceiptFlightPosition(returnFrom, stripParent.TransformPoint(stripPosition), stripParent.up, 0.004f * stripParent.lossyScale.x, t);
        strip.rotation = Quaternion.Slerp(returnRotation, stripParent.rotation * stripRotation, eased);
        strip.localScale = Vector3.Lerp(returnScale, Vector3.Scale(stripParent.lossyScale, stripScale), eased);
        if (t >= 1f) RestorePrintedStrip();
    }
    private void RestorePrintedStrip()
    {
        if (!stripCaptured) return;
        bool current = stripKey == PrintedKey;
        Transform strip = calculator.TapeStrip;
        strip.SetParent(stripParent, false);
        strip.SetSiblingIndex(stripSibling);
        strip.SetLocalPositionAndRotation(stripPosition, stripRotation);
        strip.localScale = stripScale;
        for (int i = 0; i < stripColliders.Length; i++)
            if (stripColliders[i] != null) stripColliders[i].enabled = stripColliderStates[i];
        for (int i = 0; i < stripRenderers.Length; i++)
            if (stripRenderers[i] != null) stripRenderers[i].enabled = stripRendererStates[i];
        stripCaptured = stripHeld = false; stripReturnAt = -1f;
        if (flyingLayer != null)
        {
            flyingLayer.KeepInFront = false;
            flyingLayer.gameObject.SetActive(false);
            Destroy(flyingLayer.gameObject);
        }
        flyingLayer = null;
        // A changed draft owns its current text/feed/collider state; never replay old print.
        if (!current) calculator.SetTape(Calculator.HasDraft && Calculator.ShowingResult ? DraftLines() : null, animate: false);
    }

    public Collider ToolAt(Vector2 screen) => ToolPointerActive ? Pick(screen) : null;
    public Level3TapeTransfer TapeAt(Vector2 screen) => ToolAt(screen)?.GetComponent<Level3TapeTransfer>();
    public void PressToolKey(Collider target) => calculator?.Press(target, 10f);
    public void ReleaseToolKeys() => calculator?.ReleaseAll();
    public void CancelToolPointer() => ReleasePointer();
    public void ClickTool(Collider target) { if (ToolPointerActive) Activate(target); }

    public bool IsToolMoveHandle(Collider target) => target != null &&
        (calculator.IsMoveHandle(target) || (ComputationOpen ? leaf.IsMoveHandle(target) : card.IsMoveHandle(target)));

    private Transform settlingTool;
    private Vector3 toolLift, toolSettlePosition;
    private float toolSettleAt = -1f;
    private void FinishToolSettle()
    {
        if (settlingTool != null) settlingTool.position = toolSettlePosition;
        settlingTool = null; toolSettleAt = -1f;
    }
    private void TickToolSettle()
    {
        if (settlingTool == null) return;
        float t = Mathf.Clamp01((Time.unscaledTime - toolSettleAt) / 0.18f);
        settlingTool.position = toolSettlePosition + toolLift * ((1f - t) * (1f - t));
        if (t >= 1f) FinishToolSettle();
    }

    // Same HeldOnScreen clamp used by the book/folder. The picker excludes
    // content before ordinary body/border/clip/roll targets enter this path.
    public void BeginToolMove(Collider target, Vector2 screen)
    {
        if (!ToolPointerActive || flyStart >= 0f || !IsToolMoveHandle(target)) return;
        Level3TapeTransfer.Cancel(this);
        RestorePrintedStrip();
        FinishToolSettle();
        movingTool = calculator.IsMoveHandle(target) ? calculator.Root : PaperRoot;
        Camera view = HeldOnScreen.ActiveCamera();
        toolLift = -view.transform.forward * 0.003f;
        movingTool.position += toolLift;
        movingDepth = Vector3.Dot(movingTool.position - view.transform.position, view.transform.forward);
        movingOffset = movingTool.position - view.ScreenToWorldPoint(new Vector3(screen.x, screen.y, movingDepth));
        // Measure at the actual held depth, so even the pickup stays in bounds.
        movingBounds = HeldOnScreen.ForModel(movingTool, view, screen);
        movingArea = ToolMoveArea(view.pixelRect);
        Doc3DLayer.For(movingTool)?.BringToFront(false);
    }
    public void MoveTool(Vector2 screen)
    {
        if (movingTool == null || !ToolPointerActive) return;
        Vector2 clamped = ClampToolMove(movingBounds, screen, movingArea);
        Camera view = HeldOnScreen.ActiveCamera();
        movingTool.position = view.ScreenToWorldPoint(new Vector3(clamped.x, clamped.y, movingDepth)) + movingOffset;
        MobileInputGuard.SuppressWorldTapsThisFrame();
    }
    public void EndToolMove()
    {
        if (movingTool == null) return;
        settlingTool = movingTool;
        toolSettlePosition = movingTool.position - toolLift;
        toolSettleAt = Time.unscaledTime;
        movingTool = null;
    }

    public static Vector2 ClampToolMove(HeldOnScreen held, Vector2 pointer, Rect area) => held.Clamp(pointer, area);

    /// <summary>Both tools share the whole viewport and the standard document edge margin.</summary>
    public static Rect ToolMoveArea(Rect view) => view;

    public void ShowTapeDropTarget(string destination)
    {
        if (LessonOpen || PracticeOpen) card?.SetArmed(destination);
        else leaf?.SetArmed(destination);
    }
    public void ShowTransferHover(Level3TapeTransfer target)
    {
        ShowTapeDropTarget(null);
        if (calculator?.InputFrame != null) calculator.InputFrame.SetActive(false);
        if (target == null || target.IsSource || ValueTransferManager.Instance?.HasSelection != true) return;
        // Neutral possible-drop cue, independent of lesson correctness.
        if (target == calculator.InputTarget.GetComponent<Level3TapeTransfer>()) calculator.InputFrame.SetActive(true);
        else if (card != null && card.TrySlot(target.GetComponent<Collider>(), out string slot)) card.SetArmed(slot);
        else if (leaf != null && leaf.TryHit(target.GetComponent<Collider>(), out Level3ComputationLeafModel.Hit hit)) leaf.SetArmed(hit.Id);
    }

    public string AttachedTapeSource(string destination)
    {
        if (!ComputationOpen) return null;
        string field = LeafField(destination) ?? destination;
        return Level3ComputationRules.TapeOf(Chapter, BeaCase, field) != null ? Level3ComputationRules.SheetSource(BeaCase, field) : null;
    }

    public bool CanDragPrintedTape => ToolPointerActive && Calculator.HasDraft && Calculator.ShowingResult && !calculator.IsFeeding && flyStart < 0f;
    private string TransferContext => Chapter == null ? null : string.Join("/", Chapter.activeCaseId, Chapter.contentRevision,
        Chapter.phase, Definition?.level3?.taxYear, Chapter.Case(Chapter.activeCaseId)?.computation?.revision, LessonOpen ? "lesson" : "case");
    public string PrintedKey => !CanDragPrintedTape ? null : TransferContext + ":" + Calculator.InputRevision + ":" +
        string.Join(";", Calculator.DraftChain.ConvertAll(t => string.Join("|", t.kind, t.value, t.op, t.sourceId, t.manual, t.percent)));
    public string PrintedResult => CanDragPrintedTape ? Calculator.DraftResult : null;

    public Level3CalculatorTransferValue TransferSource(string id, bool tape)
    {
        if (!ToolPointerActive || flyStart >= 0f) return null;
        if (tape) return CanDragPrintedTape ? new Level3CalculatorTransferValue(TransferContext, "printed-tape", "Result", PrintedResult, draftKey: PrintedKey) : null;
        if (LessonOpen || PracticeOpen)
        {
            foreach (var source in LessonOpen ? (ActiveLesson ?? Level3PracticeRules.LessonCard).Sources : Level3PracticeRules.Sources(Chapter).ToArray())
                if (source.SourceId == id) return new Level3CalculatorTransferValue(TransferContext, source.SourceId, source.Label, source.Amount, source.Percent);
            return null;
        }
        return ComputationOpen && Level3ComputationRules.Editable(Chapter, BeaCase) && !leaf.PickerOpen ? SheetTransferSource(id) : null;
    }

    public bool CanPlaceTransfer(DataValue value, bool tape)
    {
        if (!ToolPointerActive || flyStart >= 0f) return false;
        if (!tape && value?.Value is Level3TransferValue) return ComputationOpen && Level3ValueTransfer.CanInsertComputation(value, Chapter, Definition);
        if (!(value?.Value is Level3CalculatorTransferValue selected)) return false;
        Level3CalculatorTransferValue current = TransferSource(tape ? null : selected.SourceId, tape);
        return current != null && current.Matches(value) && (tape ? selected.DraftKey != null : selected.DraftKey == null);
    }
    public bool PlaceTransfer(DataValue value, bool tape, string destination)
    {
        if (!CanPlaceTransfer(value, tape)) return false;
        if (tape)
        {
            bool placed = DropPrintedTape(destination);
            if (placed) DragVisualManager.Instance?.Hide();
            return placed;
        }
        RestorePrintedStrip();
        if (value.Value is Level3TransferValue)
        {
            if (!Level3ValueTransfer.InsertComputation(value, Chapter, Definition, Calculator)) return false;
        }
        else
        {
            var selected = (Level3CalculatorTransferValue)value.Value;
            if (!Level3CalculatorTransferValue.TryInsert(value, TransferSource(selected.SourceId, false), Calculator)) return false;
        }
        // Replacing the current operand invalidates the old printed presentation.
        calculator.SetTape(null, animate: false);
        Disarm();
        RefreshDisplay();
        calculator.PopInput();
        GameplayEvents.RaiseGuideCue(LessonOpen ? CueSourceSelected : CueSheetSource);
        if (LessonOpen) card.SetMessage(LessonSourceMessage(card.Spec));
        return true;
    }
    private void BindTapeTransfers()
    {
        if (calculator?.TapeTarget != null) Level3TapeTransfer.Source(calculator.TapeTarget.gameObject, this);
        if (calculator?.InputTarget != null) Level3TapeTransfer.Input(calculator.InputTarget.gameObject, this);
        if (leaf != null)
            foreach (var target in leaf.TransferTargets)
            {
                if (target.Value.Kind == Level3ComputationLeafModel.HitKind.Slot)
                    Level3TapeTransfer.Destination(target.Key.gameObject, this, target.Value.Id);
                else Level3TapeTransfer.Amount(target.Key.gameObject, this, target.Value.Id);
            }
        if (card != null)
        {
            foreach (var target in card.TapeTargets) Level3TapeTransfer.Destination(target.Key.gameObject, this, target.Value);
            foreach (var target in card.SourceTargets) Level3TapeTransfer.Amount(target.Key.gameObject, this, target.Value);
        }
    }
    public bool DropPrintedTape(string destination)
    {
        if (!CanDragPrintedTape) return false;
        Level3Calculator draftOwner = Calculator;
        if (LessonOpen && destination == Level3PracticeRules.SlotOf(ActiveLesson)) AttachLesson();
        else if (ComputationOpen && !leaf.PickerOpen) AttachLeaf(destination);
        else if (PracticeOpen) Attach(destination);
        else return false;
        return !draftOwner.HasDraft;
    }
    private void BindTransferSelection()
    {
        var manager = ValueTransferManager.Instance;
        if (transferManager == manager) return;
        UnbindTransferSelection();
        transferManager = manager;
        if (manager != null)
        {
            manager.OnSelectionChanged += TransferSelectionChanged;
            manager.OnTransferFailed += TransferFailed;
        }
    }
    private void UnbindTransferSelection()
    {
        if (transferManager != null)
        {
            transferManager.OnSelectionChanged -= TransferSelectionChanged;
            transferManager.OnTransferFailed -= TransferFailed;
        }
        transferManager = null;
    }
    private void TransferSelectionChanged(object source)
    {
        string id = source is Level3TapeTransfer selected && selected.Owner == this &&
            transferManager.SelectedValue?.Value is Level3CalculatorTransferValue amount && amount.DraftKey == null ? amount.SourceId : null;
        SetSelected(id);
        if (ComputationOpen) leaf?.SetSelectedSource(id);
        calculator?.InputFrame.SetActive(id != null || (ComputationOpen && Level3ValueTransfer.CanInsertComputation(transferManager.SelectedValue, Chapter, Definition)));
        calculator?.TapeSelectionFrame.SetActive(source is Level3TapeTransfer tape && tape.Owner == this &&
            transferManager.SelectedValue?.Value is Level3CalculatorTransferValue print && print.DraftKey != null);
    }
    private void TransferFailed(DataValue value, string reason)
    {
        if (!(transferManager?.SelectedSourceComponent is Level3TapeTransfer source) || source.Owner != this ||
            !(value?.Value is Level3CalculatorTransferValue payload)) return;
        // Keep command-specific save/support refusals. Explain only an expired
        // capture here; generic UI cannot describe Level3 provenance.
        if (payload.Matches(TransferSource(payload.SourceId, payload.DraftKey != null))) return;
        const string message = "Selection changed.\nSelect the value again.";
        if (LessonOpen || PracticeOpen) card?.SetMessage(message);
        else Say("Selection changed. Select it again.");
    }
}
