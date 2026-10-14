using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Fresh UI hit tests for legacy world input alongside the Input System.</summary>
public static class MobileInputGuard
{
    private static readonly List<RaycastResult> hits = new List<RaycastResult>();
    private static PointerEventData pointer;
    private static EventSystem owner;

    public static bool IsPointerOverUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;
        if (owner != EventSystem.current || pointer == null)
        {
            owner = EventSystem.current;
            pointer = new PointerEventData(owner);
        }
        pointer.Reset();
        pointer.position = position;
        hits.Clear();
        owner.RaycastAll(pointer, hits);
        for (int i = 0; i < hits.Count; i++)
            if (hits[i].module is GraphicRaycaster) return true;
        return false;
    }

    public static bool HasTouchInput
    {
        get
        {
            if (Touchscreen.current == null) return false;
            foreach (var touch in Touchscreen.current.touches)
                if (touch.press.isPressed || touch.press.wasReleasedThisFrame) return true;
            return false;
        }
    }

    public static bool BlocksWorldInput()
    {
        if (VirtualJoystick.BlocksWorldInput) return true;
        if (Touchscreen.current != null)
            foreach (var touch in Touchscreen.current.touches)
                if ((touch.press.isPressed || touch.press.wasReleasedThisFrame) &&
                    IsPointerOverUI(touch.position.ReadValue())) return true;
        return Mouse.current != null && IsPointerOverUI(Mouse.current.position.ReadValue());
    }

    // Let touch gestures finish before deciding whether they meant "interact".
    // The installed Input System already distinguishes a tap from a swipe/cancel.
    public static bool TryGetWorldTap(out Vector2 position)
    {
        position = default;
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (!touch.press.wasReleasedThisFrame || !touch.tap.wasPressedThisFrame ||
                    touch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Ended) continue;
                position = touch.position.ReadValue();
                if (!IsPointerOverUI(touch.startPosition.ReadValue()) && !IsPointerOverUI(position)) return true;
            }
        }
        if (HasTouchInput || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return false;
        position = Mouse.current.position.ReadValue();
        return !IsPointerOverUI(position);
    }

    public static bool IsDragOrCancelled(PointerEventData eventData)
    {
        float threshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10f;
        if (eventData.dragging || (eventData.position - eventData.pressPosition).sqrMagnitude > threshold * threshold) return true;
        if (eventData is ExtendedPointerEventData extended && extended.device is Touchscreen screen)
        {
            foreach (var touch in screen.touches)
                if (touch.touchId.ReadValue() == extended.touchId)
                    return !touch.tap.wasPressedThisFrame || touch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Ended;
            return true;
        }
        return false;
    }
}
