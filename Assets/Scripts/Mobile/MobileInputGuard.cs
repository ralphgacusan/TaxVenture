using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Fresh UI hit tests for legacy world input alongside the Input System.</summary>
public static class MobileInputGuard
{
    private static readonly List<RaycastResult> hits = new List<RaycastResult>();
    private static PointerEventData pointer;
    private static EventSystem owner;
    private static int suppressedFrame = -1;

    /// <summary>
    /// The UI tap that closes a view (desk exit, last dialogue line, viewer) is
    /// released in the same frame world interaction is re-enabled; the closing UI
    /// is already hidden, so it would otherwise read as a world tap.
    /// </summary>
    public static void SuppressWorldTapsThisFrame() => suppressedFrame = Time.frameCount;

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
        if (suppressedFrame == Time.frameCount) return false;
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (!touch.press.wasReleasedThisFrame || !IsTapRelease(touch)) continue;
                position = touch.position.ReadValue();
                if (!IsPointerOverUI(touch.startPosition.ReadValue()) && !IsPointerOverUI(position)) return true;
            }
        }
        if (HasTouchInput || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return false;
        position = Mouse.current.position.ReadValue();
        return !IsPointerOverUI(position);
    }

    // The Input System's own tap needs the finger lifted within 0.2 s, which
    // drops ordinary deliberate taps (and Unity Remote-delayed ones), so the
    // first tap on an NPC often did nothing. A tap here is a short, nearly
    // still touch that ended normally.
    private const double MaxTapSeconds = 0.5;

    private static bool IsTapRelease(TouchControl touch)
    {
        if (touch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Ended) return false;
        if (InputState.currentTime - touch.startTime.ReadValue() > MaxTapSeconds) return false;
        float slop = TapSlop();
        return (touch.position.ReadValue() - touch.startPosition.ReadValue()).sqrMagnitude <= slop * slop;
    }

    // EventSystem's default 10 px is a tiny wobble on a high-resolution phone.
    private static float TapSlop() =>
        Mathf.Max(EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10f,
            0.02f * Mathf.Min(Screen.width, Screen.height));

    public static bool IsDragOrCancelled(PointerEventData eventData)
    {
        float threshold = TapSlop();
        if (eventData.dragging || (eventData.position - eventData.pressPosition).sqrMagnitude > threshold * threshold) return true;
        if (eventData is ExtendedPointerEventData extended && extended.device is Touchscreen screen)
        {
            foreach (var touch in screen.touches)
                if (touch.touchId.ReadValue() == extended.touchId)
                    return !IsTapRelease(touch);
            return true;
        }
        return false;
    }
}
