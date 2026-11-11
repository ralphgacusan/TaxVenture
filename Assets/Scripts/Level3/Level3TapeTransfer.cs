using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Narrow surface adapter. Shared selection and ValueDragSource own transfer;
/// the calculator's existing commands own provenance, persistence and attachment.</summary>
public sealed class Level3TapeTransfer : MonoBehaviour, IDataValueSource, IDataValueDestination
{
    public Level3DeskCalculator Owner { get; private set; }
    private bool source;
    public bool IsSource => source || (IsTape && Owner != null && (!Owner.Calculator.HasDraft || !Owner.Calculator.ShowingResult) && Owner.AttachedTapeSource(id) != null);
    public bool IsTape { get; private set; }
    public bool IsPrintedStrip => source && IsTape;
    public bool ShowValueDragVisual => !IsPrintedStrip;
    private string id;
    private ValueDragSource drag;
    private static Level3TapeTransfer active;
    public bool IsDragging => active == this;

    private static Level3TapeTransfer Bind(GameObject target, Level3DeskCalculator owner, string id, bool source, bool tape)
    {
        var t = target.GetComponent<Level3TapeTransfer>() ?? target.AddComponent<Level3TapeTransfer>();
        t.Owner = owner; t.id = id; t.source = source; t.IsTape = tape;
        if (source || tape)
        {
            t.drag = target.GetComponent<ValueDragSource>() ?? target.AddComponent<ValueDragSource>();
            // The composited surface forwards EventSystem gestures. The hidden
            // physics target must not receive a second copy from PhysicsRaycaster.
            t.drag.enabled = false;
            t.drag.Initialize(t, t);
        }
        return t;
    }
    public static void Source(GameObject target, Level3DeskCalculator owner) => Bind(target, owner, null, true, true);
    public static void Amount(GameObject target, Level3DeskCalculator owner, string id) => Bind(target, owner, id, true, false);
    public static void Destination(GameObject target, Level3DeskCalculator owner, string id) => Bind(target, owner, id, false, true);
    public static void Input(GameObject target, Level3DeskCalculator owner) => Bind(target, owner, null, false, false);

    public DataValue GetDataValue()
    {
        Level3CalculatorTransferValue payload = Owner != null ? Owner.TransferSource(source ? id : Owner.AttachedTapeSource(id), source && IsTape) : null;
        if (payload == null) return null;
        string display = payload.Percent && Level3Calculator.TryParse(payload.Amount, out decimal fraction)
            ? Level3Calculator.Format(Level3Calculator.Canonical(fraction * 100m)) + "%" : Level3Calculator.Format(payload.Amount);
        return new DataValue(payload, display, DataValueType.Number, "Level3:Calculator/" + payload.SourceId);
    }
    public bool CanAccept(DataValue value) => !IsSource && Owner != null && Owner.CanPlaceTransfer(value, IsTape);
    public bool TryReceiveValue(DataValue value) => CanAccept(value) && Owner.PlaceTransfer(value, IsTape, id);
    public void Click()
    {
        var manager = ValueTransferManager.Instance;
        if (IsSource)
        {
            if (GetDataValue() != null) manager?.SelectValue(this, this);
            else Owner.ClickTool(GetComponent<Collider>());
        }
        else if (manager?.HasSelection == true) manager.TryPlaceOnDestination(this);
        else Owner.ClickTool(GetComponent<Collider>());
    }
    public void Begin(PointerEventData e)
    {
        if (!IsSource || GetDataValue() == null) return;
        drag.ShowVisual = ShowValueDragVisual;
        drag.OnBeginDrag(e);
        if (ValueDragSource.IsAnyDragActive)
        {
            active = this;
            if (IsPrintedStrip) Owner.BeginPrintedStrip(e.pressPosition);
        }
    }
    public void Move(PointerEventData e)
    {
        if (active != this) return;
        drag.OnDrag(e);
        if (IsPrintedStrip) Owner.MovePrintedStrip(e.position);
        Owner.ShowTransferHover(Owner.TapeAt(e.position));
        MobileInputGuard.SuppressWorldTapsThisFrame();
    }
    public void End(PointerEventData e)
    {
        if (active != this) return;
        drag.OnEndDrag(e);
        Owner?.ShowTransferHover(null);
        active = null;
        if (IsPrintedStrip) Owner?.EndPrintedStrip(false);
    }
    public void CancelGesture()
    {
        if (active != this) return;
        drag.OnEndDrag(new PointerEventData(EventSystem.current));
        active = null;
        Owner?.ShowTransferHover(null);
        if (IsPrintedStrip) Owner?.EndPrintedStrip(true);
    }
    public static void Cancel(Level3DeskCalculator owner)
    {
        if (active != null && active.Owner == owner) active.CancelGesture();
        var manager = ValueTransferManager.Instance;
        if (manager?.SelectedSourceComponent is Level3TapeTransfer selected && selected.Owner == owner) manager.ClearSelection();
    }
    private void OnDisable()
    {
        if (active == this) CancelGesture();
        if (object.ReferenceEquals(ValueTransferManager.Instance?.SelectedSourceComponent, this)) ValueTransferManager.Instance.ClearSelection();
    }
}
