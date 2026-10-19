using UnityEngine;

/// <summary>
/// PURPOSE:
/// Level 2 rule for the retrieved folder (ticket T02), under
/// Level2Gameplay/FolderHandoff. After the market-client drawer, Rosa's
/// folder is in the HUD Case Folder icon (count 1). From there it cannot be
/// opened or read anywhere: the player sits at the desk and taps the icon or drags it
/// onto the desk. That puts the real desk folder back (count 0) and
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

    public const string SitAtDeskMessage = "Sit at your desk, then open Rosa's folder.";
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
        if (review == null) return false;
        if (review.FolderRetrieved && !review.folderPlacedOnDesk) return true;
        return !CanHandInReview(review, true);
    }

    /// <summary>
    /// Level 2 hand-in gate (T08-T10): only a final assessment stamped for the
    /// current review, changed since a returned or held submission, while the
    /// review window is open. Says why when <paramref name="explain"/>.
    /// </summary>
    public static bool CanHandInReview(CaseReviewState review, bool explain)
    {
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        if (review == null || definition == null || !definition.HasReview) return true;
        string why = null;
        if (review.phase < ReviewPhase.OfficeReconciliation)
            why = "The Auditor reviews Rosa's folder after your final assessment.";
        else if (!ReviewClock.WindowOpen(review.clock))
            why = definition.review.clock?.missedLine;
        else
            switch (FinalAudit.Admission(review))
            {
                case FinalAudit.Outcome.NotStamped:
                    why = FinalAssessment.IsStale(review)
                        ? "Your review changed after the stamp. Stamp the Final Assessment page again."
                        : "Stamp your recommendation on the Final Assessment page first.";
                    break;
                case FinalAudit.Outcome.Unchanged:
                    why = definition.review.audit?.unchanged;
                    break;
            }
        if (why == null) return true;
        if (explain) Level2Say.Line(why, null, 5f);
        return false;
    }

    /// <summary>The folder went back into its icon after it was first placed (Level 1's store): seated at the desk it returns to the desk.</summary>
    private static bool StoredAfterPlacement
    {
        get
        {
            CaseReviewState review = Level2OfficeStart.Review;
            CameraController controller = CameraController.Instance;
            return review != null && review.folderPlacedOnDesk && FolderToHUDIcon.GetStoredFolderCount() > 0 &&
                   controller != null && controller.CurrentMode == CameraController.CameraMode.Workstation && IsOfficeDeskView();
        }
    }

    /// <summary>Seated at the office desk (not Rosa's records desk).</summary>
    private static bool IsOfficeDeskView()
    {
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        Transform officeDesk = view != null ? PlayerPlacement.Find(view.gameObject.scene, DeskPath) : null;
        if (view == null) return false;
        if (officeDesk == null) return true;
        return (view.transform.position - officeDesk.position).sqrMagnitude < 9f;
    }

    // Tap at the desk places/opens centrally; reading elsewhere remains gated.
    private bool HandleClick()
    {
        if (StoredAfterPlacement)
        {
            CameraController controller = CameraController.Instance;
            PlaceOnDesk(controller.MainCamera.pixelRect.center, true);
            return true;
        }
        if (Awaiting)
        {
            CameraController controller = CameraController.Instance;
            if (controller != null && controller.CurrentMode == CameraController.CameraMode.Workstation && controller.MainCamera != null)
            {
                PlaceOnDesk(controller.MainCamera.pixelRect.center, true);
                return true;
            }
            Level2Say.Line(SitAtDeskMessage);
            return true;
        }
        return previousClick != null && previousClick();
    }

    // Drag out of the icon and release: only onto the desk, while sitting at it.
    private bool HandleDrop(Vector2 screenPosition)
    {
        // Seated at the desk, a stored folder dropped on the desk goes onto it (as in Level 1).
        if (!Awaiting && StoredAfterPlacement && IsOnDesk(screenPosition))
        {
            PlaceOnDesk(screenPosition);
            return true;
        }
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

    private void PlaceOnDesk(Vector2 screenPosition, bool centrePaper = false)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        if (review == null) return;
        // The first placement records the review step; later ones only put the folder back.
        if (!review.folderPlacedOnDesk && !review.TryPlaceFolderOnDesk()) return;

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
            deskFolder.OpenDraggedFolder(centrePaper);
        }

        Debug.Log("[Level2] Rosa's folder dropped on the desk from the HUD folder icon and opened.");
    }

    /// <summary>Resume (T11): the folder is back on the desk, closed and usable, without replaying the first open.</summary>
    public static void RestorePlacedFolder(UnityEngine.SceneManagement.Scene scene)
    {
        FolderToHUDIcon store = FindStore(scene);
        if (store != null) store.ResetToUnstored();
        CaseFolder3DDrag deskFolder = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            if ((deskFolder = root.GetComponentInChildren<CaseFolder3DDrag>(true)) != null) break;
        if (deskFolder != null) deskFolder.RestoreAndEnableFolder();
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
