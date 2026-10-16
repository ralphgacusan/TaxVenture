using UnityEngine;

/// <summary>How a HUD drag ended, so the dragged item can animate accordingly.</summary>
public enum HudDragOutcome
{
    /// <summary>Dropped on the screen: the window opens where it landed.</summary>
    Opened,
    /// <summary>Dropped on a target that took it (e.g. the Auditor's tray).</summary>
    Consumed,
    /// <summary>Cancelled or dropped back on its icon: it flies home.</summary>
    Returned
}

/// <summary>
/// The item shown under the finger while dragging out of a HUD icon
/// (closed Case Folder, Notes paper, taxpayer profile stack).
/// </summary>
public interface IHudDragVisual
{
    void Begin(Vector2 screenPosition);
    void Move(Vector2 screenPosition);
    void Drop(Vector2 screenPosition, HudDragOutcome outcome);
}
