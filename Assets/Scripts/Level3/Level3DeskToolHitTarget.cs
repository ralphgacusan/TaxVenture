using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>The lifted tools have one EventSystem input owner. The physical
/// targets identify content and blank-body/border/clip/roll movement areas.</summary>
public sealed class Level3DeskToolHitTarget : GraphicRaycaster,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerMoveHandler, IPointerExitHandler
{
    public Level3DeskCalculator Owner;
    public Doc3DLayer Layer;
    private Collider pressed;
    private Level3TapeTransfer source;
    private int? pointer;
    private static Level3DeskToolHitTarget capture;
    private bool dragged;
    public enum GestureRole { Content, Tool, Value, PrintedStrip }
    private GestureRole role;
    public static GestureRole Classify(bool valueSource, bool printedStrip, bool moveBody) =>
        valueSource ? (printedStrip ? GestureRole.PrintedStrip : GestureRole.Value) : moveBody ? GestureRole.Tool : GestureRole.Content;
    public static bool AllowsDrop(GestureRole captured, bool isDragging) => isDragging &&
        (captured == GestureRole.Value || captured == GestureRole.PrintedStrip);
    public static bool ReserveAssemblyForeground(bool pointerHeld, bool physicalReceiptVisible) => pointerHeld && !physicalReceiptVisible;
    private int releasedFrame = -1;

    // This is the input raycaster for the existing composited tool layer.
    // It needs no transparent Graphic, canvas geometry, camera event mask or
    // delayed physics broadphase to dispatch the ordinary EventSystem gestures.
    public override Camera eventCamera => null;
    public override int sortOrderPriority => Layer != null ? Layer.SortingOrder : 0;
    public override int renderOrderPriority => sortOrderPriority;
    public override void Raycast(PointerEventData e, List<RaycastResult> results)
    {
        if (Owner == null || !Owner.ToolPointerActive || Layer == null || !Layer.IsVisible) return;
        Collider target = Owner.ToolAt(e.position);
        if (target == null || !target.transform.IsChildOf(Layer.Source)) return;
        results.Add(new RaycastResult
        {
            gameObject = gameObject, module = this, distance = 0f,
            screenPosition = e.position, sortingOrder = Layer.SortingOrder,
            index = results.Count, depth = 0
        });
    }

    /// <summary>World-distance intersection with a current local box. The
    /// transformed direction stays unnormalized so differently scaled targets
    /// compare in the same units. Pure math; no physics synchronization.</summary>
    public static bool HitBox(Ray ray, Matrix4x4 worldToLocal, Bounds box, out float distance)
    {
        Vector3 origin = worldToLocal.MultiplyPoint3x4(ray.origin);
        Vector3 direction = worldToLocal.MultiplyVector(ray.direction);
        Vector3 min = box.min, max = box.max;
        float near = 0f, far = 4f;
        distance = 0f;
        for (int axis = 0; axis < 3; axis++)
        {
            if (Mathf.Abs(direction[axis]) < 0.0000001f)
            {
                if (origin[axis] < min[axis] || origin[axis] > max[axis]) return false;
                continue;
            }
            float a = (min[axis] - origin[axis]) / direction[axis];
            float b = (max[axis] - origin[axis]) / direction[axis];
            near = Mathf.Max(near, Mathf.Min(a, b));
            far = Mathf.Min(far, Mathf.Max(a, b));
            if (near > far) return false;
        }
        distance = near;
        return true;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (pointer.HasValue || capture != null || !Owner.ToolPointerActive || e.button != PointerEventData.InputButton.Left || ValueDragSource.IsAnyDragActive) return;
        capture = this;
        pointer = e.pointerId;
        dragged = false;
        releasedFrame = -1;
        pressed = Owner.ToolAt(e.position);
        source = pressed != null ? pressed.GetComponent<Level3TapeTransfer>() : null;
        role = Classify(source != null && source.IsSource, source != null && source.IsPrintedStrip,
            Owner.IsToolMoveHandle(pressed));
        // A tap on content focuses its assembly just like a blank-body grab.
        // Role/target were already captured, so changing order cannot retarget it.
        if (Layer != null)
        {
            Layer.KeepInFront = ReserveAssemblyForeground(pointer.HasValue, Owner.PhysicalReceiptVisible);
            Layer.BringToFront(false);
        }
        Owner.PressToolKey(pressed);
        MobileInputGuard.SuppressWorldTapsThisFrame();
    }
    public void OnPointerUp(PointerEventData e)
    {
        if (pointer != e.pointerId) return;
        if (Canceled(e)) { e.eligibleForClick = false; Owner.CancelToolPointer(); return; }
        Owner.ReleaseToolKeys();
        releasedFrame = Time.frameCount;
        // Click and drop follow Up; retain capture until those callbacks.
        MobileInputGuard.SuppressWorldTapsThisFrame();
    }
    public void OnPointerClick(PointerEventData e)
    {
        if (pointer != e.pointerId || dragged || e.dragging || Canceled(e) || !Owner.ToolPointerActive) return;
        if (pressed != null && Owner.ToolAt(e.position) == pressed)
        {
            if (source != null) source.Click();
            else Owner.ClickTool(pressed);
        }
        Clear();
    }
    public void OnBeginDrag(PointerEventData e)
    {
        if (pointer != e.pointerId) return;
        dragged = true;
        e.eligibleForClick = false;
        Owner.ReleaseToolKeys();
        if (AllowsDrop(role, true)) source.Begin(e);
        else if (role == GestureRole.Tool) Owner.BeginToolMove(pressed, e.pressPosition);
    }
    public void OnDrag(PointerEventData e)
    {
        if (pointer != e.pointerId) return;
        if (AllowsDrop(role, true)) source?.Move(e);
        else if (role == GestureRole.Tool) Owner.MoveTool(e.position);
    }
    public void OnEndDrag(PointerEventData e)
    {
        if (pointer != e.pointerId) return;
        if (Canceled(e)) { Owner.CancelToolPointer(); return; }
        if (AllowsDrop(role, dragged)) source?.End(e);
        Clear();
    }
    public void OnDrop(PointerEventData e)
    {
        // A summary band uses the ordinary shared ValueDragSource, rather than a tool-local gesture.
        var amount = e.pointerDrag != null ? e.pointerDrag.GetComponent<Level3AmountSource>() : null;
        if (Owner.ToolPointerActive && !Canceled(e) && amount != null && amount.IsValueDragging &&
            ReferenceEquals(ValueTransferManager.Instance?.SelectedSourceComponent, amount))
        {
            Level3TapeTransfer input = Owner.TapeAt(e.position);
            if (input != null && !input.IsSource && !input.IsTape) ValueTransferManager.Instance.TryPlaceOnDestination(input);
            return;
        }
        var field = e.pointerDrag != null ? e.pointerDrag.GetComponent<Level3ComputationField>() : null;
        if (Owner.ToolPointerActive && !Canceled(e) && field != null && ValueDragSource.IsAnyDragActive &&
            ReferenceEquals(ValueTransferManager.Instance?.SelectedSourceComponent, field))
        {
            Level3TapeTransfer input = Owner.TapeAt(e.position);
            if (input != null && !input.IsSource && !input.IsTape) ValueTransferManager.Instance.TryPlaceOnDestination(input);
            return;
        }
        var gesture = e.pointerDrag != null ? e.pointerDrag.GetComponent<Level3DeskToolHitTarget>() : null;
        if (!Owner.ToolPointerActive || Canceled(e) || gesture == null || gesture.pointer != e.pointerId ||
            gesture.Owner != Owner || !AllowsDrop(gesture.role, gesture.dragged) ||
            gesture.source == null || !gesture.source.IsDragging ||
            !ReferenceEquals(ValueTransferManager.Instance?.SelectedSourceComponent, gesture.source)) return;
        Level3TapeTransfer target = Owner.TapeAt(e.position);
        if (target != null && !target.IsSource) ValueTransferManager.Instance?.TryPlaceOnDestination(target);
    }
    public void OnPointerMove(PointerEventData e)
    {
        if (!pointer.HasValue) { Owner.ShowToolHover(Owner.ToolAt(e.position)); Owner.ShowTransferHover(Owner.TapeAt(e.position)); }
    }
    public void OnPointerExit(PointerEventData e) { Owner?.ShowToolHover(null); Owner?.ShowTransferHover(null); }
    public void Clear()
    {
        // Keep the touched assembly's final order after a normal release.
        if (Layer != null)
        {
            if (pointer.HasValue && Layer.KeepInFront) Layer.BringToFront(false);
            Layer.KeepInFront = false;
        }
        Owner?.ReleaseToolKeys();
        Owner?.ShowToolHover(null);
        Owner?.ShowTransferHover(null);
        if (pointer.HasValue && role == GestureRole.Tool) Owner?.EndToolMove();
        if (AllowsDrop(role, dragged)) source?.CancelGesture();
        if (capture == this) capture = null;
        pressed = null; source = null; pointer = null; dragged = false;
        releasedFrame = -1;
    }
    private void LateUpdate()
    {
        // An off-target Up has no Click callback. Canceled touches and focus
        // changes also release capture without synthesizing a click.
        if (Owner == null || !Owner.ToolPointerActive || releasedFrame >= 0) Clear();
        else if (pointer.HasValue && Layer != null)
            // The detached receipt gets the sole reservation while held,
            // returning or attaching; its former parent cannot overtake it.
            Layer.KeepInFront = ReserveAssemblyForeground(pointer.HasValue, Owner.PhysicalReceiptVisible);
    }
    public static bool Canceled(PointerEventData e)
    {
        if (e == null) return true;
        if (e is ExtendedPointerEventData extended && extended.device is Touchscreen screen)
            foreach (var touch in screen.touches)
                if (touch.touchId.ReadValue() == extended.touchId && touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled) return true;
        for (int i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            if (touch.fingerId == e.pointerId && touch.phase == UnityEngine.TouchPhase.Canceled) return true;
        }
        return false;
    }
    protected override void OnDisable()
    {
        Clear();
        base.OnDisable(); // unregister from EventSystem's RaycasterManager
    }
}
