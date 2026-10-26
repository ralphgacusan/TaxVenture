using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Second way to open a HUD document: drag it out of its icon. While
/// dragging, an item (the Notes paper, the profile stack) follows the
/// finger; dropping it on the screen opens the real window there, dropping
/// it back on the icon cancels. A plain tap still uses the icon's Button
/// (Unity does not send the click once a drag has started).
///
/// Closing by drag is handled by the windows themselves (drag a window, or
/// the gathered profile stack, onto the icon).
/// </summary>
public sealed class HudIconDragOpen : MonoBehaviour,
    IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public IHudDragVisual Visual;
    /// <summary>Optional: whether a drag may start right now (e.g. not already open).</summary>
    public Func<bool> CanDrag;
    /// <summary>Opens the window at the drop point; returns true when it opened.</summary>
    public Func<Vector2, bool> OpenAt;

    private bool dragging;

    public bool IsDragging => dragging;

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Unity can still send a click after a drag when both handlers live
        // on the same object. A drag must never toggle the paper a second time.
        eventData.eligibleForClick = false;
        Button button = GetComponent<Button>();
        dragging = eventData.button == PointerEventData.InputButton.Left &&
                   (button == null || (button.IsActive() && button.IsInteractable())) &&
                   Visual != null && OpenAt != null && (CanDrag == null || CanDrag());
        if (dragging) Visual.Begin(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragging) Visual.Move(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;

        bool backOnIcon = RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, eventData.position, null);
        if (!backOnIcon && OpenAt(eventData.position))
            Visual.Drop(eventData.position, HudDragOutcome.Opened);
        else
            Visual.Drop(eventData.position, HudDragOutcome.Returned);
    }

    private void OnDisable()
    {
        if (!dragging) return;
        dragging = false;
        Visual.Drop(Vector2.zero, HudDragOutcome.Returned);
    }
}
