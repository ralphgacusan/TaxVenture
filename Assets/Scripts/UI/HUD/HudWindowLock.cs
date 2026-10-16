using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Shared "hold the player still" for the Office HUD paper windows (Notes,
/// taxpayer profiles). Several windows can be open at once: the first one
/// to open freezes walking/looking and hides the joysticks (reusing
/// CameraController's profile-viewer hold), and the last one to close
/// gives control back.
///
/// At the desk/computer (not first person) the player is already still, so
/// no hold is taken. The HUD stays visible while held (OfficeHudRuntime).
/// </summary>
public static class HudWindowLock
{
    private static readonly HashSet<object> owners = new HashSet<object>();
    private static bool held;

    public static bool IsHeld => held;

    public static void Acquire(object owner)
    {
        if (owner == null) return;
        owners.Add(owner);
        if (held) return;

        CameraController cameraController = CameraController.Instance;
        if (cameraController != null &&
            cameraController.CurrentMode == CameraController.CameraMode.FirstPerson)
        {
            held = cameraController.TryBeginProfileViewer();
        }
    }

    public static void Release(object owner)
    {
        if (owner == null || !owners.Remove(owner)) return;
        if (owners.Count > 0 || !held) return;

        held = false;
        if (CameraController.Instance != null) CameraController.Instance.EndProfileViewer();
    }

    /// <summary>Forgets every owner (new level load). Does not touch camera state.</summary>
    public static void ResetForNewScene()
    {
        owners.Clear();
        held = false;
    }
}
