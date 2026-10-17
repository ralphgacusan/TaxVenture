using UnityEngine;

/// <summary>
/// PURPOSE:
/// Level 2 rule for the retrieved folder (ticket T02), under
/// Level2Gameplay/FolderHandoff. After the market-client drawer, Rosa's
/// folder is in the HUD Case Folder icon (count 1). From there it cannot be
/// opened or read anywhere: the player sits at the desk and drags it from
/// the icon onto the desk. That puts the real desk folder back (count 0) and
/// opens it right there; from then on the folder works exactly as in Level 1.
///
/// HOW: wraps the HUD folder icon's tap / drop actions (set by
/// OfficeHudRuntime) while review.AwaitingDeskPlacement, and refuses an
/// Auditor hand-in until the folder has been on the desk. Uses the existing
/// FolderToHUDIcon.ResetToUnstored + CaseFolder3DDrag.RestoreAndEnableFolder.
/// </summary>
public sealed class Level2FolderHandoff : MonoBehaviour
{
    public const string ChildName = "FolderHandoff";
    public const string FolderIconPath = "UI/HUD_Canvas/PermanentHUD/IconButtons/CaseFolderIconButton";
    public const string DeskPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk";

    public const string SitAtDeskMessage = "Sit at your desk, then drag Rosa's folder onto it.";
    public const string DropOnDeskMessage = "Drop Rosa's folder on your desk.";

    private HudSubmittableIcon icon;
    private System.Func<bool> previousClick;
    private System.Func<Vector2, bool> previousDrop;
    private Transform desk;
    private float nextResolve;

    /// <summary>True once the HUD folder icon is wrapped.</summary>
    public bool Hooked => icon != null;

    private void OnEnable() => AuditorSubmissionTray.SubmissionBlocked = BlockHandIn;

    private void OnDisable()
    {
        if (AuditorSubmissionTray.SubmissionBlocked == (System.Func<bool>)BlockHandIn)
            AuditorSubmissionTray.SubmissionBlocked = null;
        Unhook();
    }

    private void Update()
    {
        // OfficeHudRuntime sets the icon's overrides in its Awake; wrap them once after.
        if (icon != null || Time.unscaledTime < nextResolve) return;
        nextResolve = Time.unscaledTime + 0.25f;
        if (OfficeHudRuntime.Instance == null) return;

        Transform found = PlayerPlacement.Find(gameObject.scene, FolderIconPath);
        HudSubmittableIcon candidate = found != null ? found.GetComponent<HudSubmittableIcon>() : null;
        if (candidate == null) return;

        icon = candidate;
        previousClick = icon.ClickOverride;
        previousDrop = icon.DropOpenOverride;
        icon.ClickOverride = HandleClick;
        icon.DropOpenOverride = HandleDrop;
        desk = PlayerPlacement.Find(gameObject.scene, DeskPath);
    }

    private void Unhook()
    {
        if (icon == null) return;
        if (icon.ClickOverride == (System.Func<bool>)HandleClick) icon.ClickOverride = previousClick;
        if (icon.DropOpenOverride == (System.Func<Vector2, bool>)HandleDrop) icon.DropOpenOverride = previousDrop;
        icon = null;
    }

    private static bool Awaiting
    {
        get
        {
            CaseReviewState review = Level2OfficeStart.Review;
            return review != null && review.AwaitingDeskPlacement && FolderToHUDIcon.GetStoredFolderCount() > 0;
        }
    }

    private bool BlockHandIn()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        return review != null && review.FolderRetrieved && !review.folderPlacedOnDesk;
    }

    // Tap on the icon: no reading from the icon while the folder waits for the desk.
    private bool HandleClick()
    {
        if (Awaiting)
        {
            Level2Say.Line(SitAtDeskMessage);
            return true;
        }
        return previousClick != null && previousClick();
    }

    // Drag out of the icon and release: only onto the desk, while sitting at it.
    private bool HandleDrop(Vector2 screenPosition)
    {
        if (!Awaiting) return previousDrop != null && previousDrop(screenPosition);

        CameraController camera = CameraController.Instance;
        if (camera == null || camera.CurrentMode != CameraController.CameraMode.Workstation)
        {
            Level2Say.Line(SitAtDeskMessage);
            return true;
        }
        if (!IsOnDesk(screenPosition))
        {
            Level2Say.Line(DropOnDeskMessage);
            return true;
        }

        PlaceOnDesk(screenPosition);
        return true;
    }

    private bool IsOnDesk(Vector2 screenPosition)
    {
        Camera view = Camera.main;
        if (view == null || desk == null) return false;
        Ray ray = view.ScreenPointToRay(screenPosition);
        foreach (RaycastHit hit in Physics.RaycastAll(ray, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            if (hit.collider.transform.IsChildOf(desk) || hit.collider.GetComponentInParent<DeskPlacementSurface>() != null)
                return true;
        return false;
    }

    private void PlaceOnDesk(Vector2 screenPosition)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        if (review == null || !review.TryPlaceFolderOnDesk()) return;

        FolderToHUDIcon store = FindStore(gameObject.scene);
        if (store != null) store.ResetToUnstored();

        // ResetToUnstored shows the closed folder; this also turns its drag/open back on.
        CaseFolder3DDrag deskFolder = null;
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            if ((deskFolder = root.GetComponentInChildren<CaseFolder3DDrag>(true)) != null) break;
        if (deskFolder != null)
        {
            deskFolder.RestoreAndEnableFolder();
            // Open it where it was dropped, as a drag-open on the desk does
            // (paper sound, case pages shown, first-open state change).
            deskFolder.BeginExternalDrag(screenPosition);
            deskFolder.EndExternalDrag(screenPosition);
            deskFolder.OpenDraggedFolder();
        }

        Debug.Log("[Level2] Rosa's folder dropped on the desk from the HUD folder icon and opened.");
    }

    /// <summary>The live folder store (the enabled FolderToHUDIcon the Auditor tray also uses).</summary>
    public static FolderToHUDIcon FindStore(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (FolderToHUDIcon candidate in root.GetComponentsInChildren<FolderToHUDIcon>(true))
                if (candidate.enabled && candidate.GetHUDIcon() != null) return candidate;
        return null;
    }
}
