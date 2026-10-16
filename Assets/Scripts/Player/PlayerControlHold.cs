using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// One shared way for overlay UI (the level guide's tap pages, reward and
/// summary cards) to hold the player still WITHOUT changing the camera mode,
/// so the HUD stays as it is:
/// - pauses PlayerMovement, FirstPersonCameraController (look) and Interactor;
/// - freezes the player's Rigidbody (PlayerMovement normally pins the
///   sideways speed every physics step; without it the body slides on slopes);
/// - frees the cursor so buttons can be pressed on PC/Editor.
///
/// Several owners can hold at once (reference counted by owner); the player
/// is released when the last one lets go. Holding only starts in normal
/// first-person play; while a conversation runs, the conversation owns the
/// controls, so applying or releasing waits until it ends (Refresh).
/// Call Refresh() every frame from any owner (cheap when idle).
/// </summary>
public static class PlayerControlHold
{
    private static readonly HashSet<object> owners = new HashSet<object>();
    private static readonly List<Behaviour> held = new List<Behaviour>();
    private static readonly List<object> stale = new List<object>();
    private static bool applied;
    private static Rigidbody body;
    private static RigidbodyConstraints bodyConstraints;
    private static CursorLockMode cursorLock;
    private static bool cursorVisible;

    public static bool IsHeld => applied;

    public static void Acquire(object owner)
    {
        if (owner == null) return;
        owners.Add(owner);
        Refresh();
    }

    public static void Release(object owner)
    {
        if (owner == null) return;
        owners.Remove(owner);
        Refresh();
    }

    public static void Refresh()
    {
        // Owners destroyed with their scene no longer count.
        stale.Clear();
        foreach (object o in owners)
            if (o is Object unityObject && unityObject == null) stale.Add(o);
        for (int i = 0; i < stale.Count; i++) owners.Remove(stale[i]);

        bool want = owners.Count > 0;
        if (want == applied) return;
        if (DialogueUI.IsAnyDialogueActive) return; // the conversation owns the controls right now

        if (want) Apply();
        else Remove();
    }

    private static void Apply()
    {
        CameraController controller = CameraController.Instance;
        if (controller == null || controller.CurrentMode != CameraController.CameraMode.FirstPerson) return;

        applied = true;
        held.Clear();
        HoldIfEnabled(Object.FindAnyObjectByType<PlayerMovement>());
        HoldIfEnabled(Object.FindAnyObjectByType<FirstPersonCameraController>());
        HoldIfEnabled(Object.FindAnyObjectByType<Interactor>());

        cursorLock = Cursor.lockState;
        cursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Transform player = controller.PlayerRoot;
        body = player != null ? player.GetComponent<Rigidbody>() : null;
        if (body != null)
        {
            bodyConstraints = body.constraints;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private static void HoldIfEnabled(Behaviour b)
    {
        if (b == null || !b.enabled) return;
        b.enabled = false;
        held.Add(b);
    }

    private static void Remove()
    {
        applied = false;
        for (int i = 0; i < held.Count; i++)
            if (held[i] != null) held[i].enabled = true;
        held.Clear();
        if (body != null)
        {
            body.constraints = bodyConstraints;
            body = null;
        }
        CameraController controller = CameraController.Instance;
        if (controller != null && controller.CurrentMode == CameraController.CameraMode.FirstPerson)
        {
            Cursor.lockState = cursorLock;
            Cursor.visible = cursorVisible;
        }
        MobileInputGuard.SuppressWorldTapsThisFrame();
    }

    /// <summary>A new level load starts with nobody holding.</summary>
    public static void ResetForNewScene()
    {
        owners.Clear();
        held.Clear();
        applied = false;
        body = null;
    }
}
