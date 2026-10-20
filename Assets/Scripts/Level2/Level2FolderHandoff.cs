using UnityEngine;

/// <summary>
/// PURPOSE:
/// Level 2 rule for the retrieved folder (ticket T02), under
/// Level2Gameplay/FolderHandoff. After the market-client drawer, Rosa's
/// folder is in the HUD Case Folder icon (count 1). Its first opening must
/// happen at the office desk. Presentation then uses the same toggle, drag,
/// and animated return as Level 1; previewing does not consume stored ownership.
/// Auditor submission remains gated by the review state.
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

    /// <summary>Seated at the office desk (not Rosa's records desk).</summary>
    private static bool IsOfficeDeskView()
    {
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        Transform officeDesk = view != null ? PlayerPlacement.Find(view.gameObject.scene, DeskPath) : null;
        if (view == null) return false;
        if (officeDesk == null) return true;
        return (view.transform.position - officeDesk.position).sqrMagnitude < 9f;
    }

    // Only the first opening has a Level 2 desk gate. All presentation is shared.
    private bool HandleClick()
    {
        if (FolderToHUDIcon.GetStoredFolderCount() <= 0) return true;
        if (Awaiting && !AtOfficeDesk())
        {
            Level2Say.Line(SitAtDeskMessage);
            return true;
        }
        bool handled = previousClick != null && previousClick();
        RecordDeskPlacement();
        return handled;
    }

    private bool HandleDrop(Vector2 screenPosition)
    {
        if (FolderToHUDIcon.GetStoredFolderCount() <= 0) return true;
        if (Awaiting)
        {
            if (!AtOfficeDesk())
            {
                Level2Say.Line(SitAtDeskMessage);
                return true;
            }
            if (!IsOnDesk(screenPosition))
            {
                Level2Say.Line(DropOnDeskMessage);
                return true;
            }
        }
        bool handled = previousDrop != null && previousDrop(screenPosition);
        RecordDeskPlacement();
        return handled;
    }

    private static bool AtOfficeDesk()
    {
        CameraController controller = CameraController.Instance;
        return controller != null && controller.CurrentMode == CameraController.CameraMode.Workstation && IsOfficeDeskView();
    }

    private static void RecordDeskPlacement()
    {
        if (Level2OfficeStart.Review == null || !Level2OfficeStart.Review.AwaitingDeskPlacement || OfficeHudRuntime.Instance == null) return;
        CaseFolder3DView view = OfficeHudRuntime.Instance.GetComponent<CaseFolder3DView>();
        if (view != null && view.IsOpen) Level2OfficeStart.Review.TryPlaceFolderOnDesk();
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

    /// <summary>Resume (T11): the folder is back on the desk, closed and usable, without replaying the first open.</summary>
    public static void RestorePlacedFolder(UnityEngine.SceneManagement.Scene scene)
    {
        FolderToHUDIcon store = FindStore(scene);
        if (store != null) store.ResetToUnstored();
        CaseFolder3DDrag deskFolder = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            if ((deskFolder = root.GetComponentInChildren<CaseFolder3DDrag>(true)) != null) break;
        if (deskFolder != null) deskFolder.RestoreAndEnableFolder();
        Transform folderIcon = PlayerPlacement.Find(scene, FolderIconPath);
        if (folderIcon != null) folderIcon.GetComponent<HudIconButton>()?.SetAvailable(FolderToHUDIcon.GetStoredFolderCount() > 0);
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
