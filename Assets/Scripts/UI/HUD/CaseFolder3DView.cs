using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Opening the Case Folder from its HUD icon (tap, or drag out and drop)
/// shows the REAL open folder from the desk â€” the 3D model with its pages â€”
/// instead of the old flat HUD panel. It flies out of the icon and settles in
/// front of the camera, facing it and fitted to a readable paper size,
/// at the drop point when it was dragged out. Pages render and turn as on
/// the desk. Grab it by its cover to move it; drop it on the folder icon (or
/// tap the icon) and it flies back in.
///
/// The folder is put back exactly as it was afterwards (parent, pose, scale,
/// visibility, drag setting). The world paper never starts a flat-window animation.
/// Sitting down at the desk closes the floating view first.
/// It joins the shared front-to-back order through its Doc3DLayer.
/// </summary>
public sealed class CaseFolder3DView : MonoBehaviour
{
    private const string OpenFolderPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/InteractiveFolderObjects/OpenCaseFolder";
    private const string ViewpointPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/DeskViewpoint";
    private const float OpenDuration = 0.42f;
    private const float CloseDuration = 0.3f;
    private const float IconScale = 0.12f;

    private enum Phase { Closed, Opening, Open, Closing, ToDesk }

    private Transform folder;
    private Transform viewpoint;
    private CaseFolderUI pages;
    private FloatingModel3D model;
    private RectTransform icon;
    private Doc3DLayer layer;

    private Transform savedParent;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private Vector3 savedScale;
    private Vector3 viewScale;
    private OpenFolderReturnToDesk deskReturn;
    private bool savedDeskReturnEnabled;
    private DocumentPhysicsRaycaster documentRaycaster;
    private bool savedActive;
    private bool savedDragging;
    private CameraController.CameraMode openedInMode;

    private Phase phase = Phase.Closed;
    private float elapsed;
    private Vector3 fromPosition, toPosition;
    private Quaternion fromRotation, toRotation;
    private float fromScale, toScale;
    private FloatingFolderGrab grab;
    private FolderToHUDIcon store;
    private bool layerWasAlwaysComposite;

    // The folder's authored open pose on the desk, as seen from the desk viewpoint: the floating
    // view uses the same angle and size, so it looks exactly like the folder opened from the desk.
    private bool hasDeskPose;
    private Vector3 deskPaperCentre;
    private Vector3 deskPosition;
    private Quaternion deskRotation;
    private Vector3 deskLossyScale;
    private CaseFolder3DDrag deskDrag;
    // The seated (desk) camera, remembered while at the desk: away from it the floating folder keeps
    // exactly the pose it has on the desk as seen from the seat.
    private bool seatKnown;
    private Vector3 seatPosition;
    private Quaternion seatRotation;

    public bool IsOpen => phase == Phase.Opening || phase == Phase.Open;

    /// <summary>The folder is out of its icon (opening, open, closing or going back to the desk).</summary>
    public bool IsOut => phase != Phase.Closed;
    public Doc3DLayer Layer => layer;

    public static CaseFolder3DView Create(GameObject host, Scene scene, RectTransform caseFolderIcon, Doc3DLayer folderLayer)
    {
        Transform folder = Find(scene, OpenFolderPath);
        if (folder == null)
        {
            Debug.LogWarning("[OfficeHud] OpenCaseFolder not found; the Case Folder icon keeps its old panel.");
            return null;
        }

        var view = host.AddComponent<CaseFolder3DView>();
        view.folder = folder;
        view.viewpoint = Find(scene, ViewpointPath);
        view.pages = folder.GetComponentInChildren<CaseFolderUI>(true);
        view.model = folder.GetComponent<FloatingModel3D>();
        view.icon = caseFolderIcon;
        view.layer = folderLayer;
        foreach (CaseFolder3DDrag drag in Object.FindObjectsByType<CaseFolder3DDrag>(FindObjectsInactive.Include))
            if (drag != null && drag.gameObject.scene == scene) { view.deskDrag = drag; break; }
        if (view.pages != null && view.pages != null && view.pages.PaperRect != null)
        {
            RectTransform paper = view.pages.PaperRect;
            view.deskPaperCentre = paper.TransformPoint(paper.rect.center);
            view.deskPosition = folder.position;
            view.deskRotation = folder.rotation;
            view.deskLossyScale = folder.lossyScale;
            view.hasDeskPose = true;
        }
        return view;
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open(null);
    }

    /// <summary>Shows the real folder (at screenPosition when dropped there).</summary>
    public bool Open(Vector2? screenPosition)
    {
        if (FolderToHUDIcon.GetStoredFolderCount() <= 0) return false;

        // Level order: the case starts only after the side quest (rewards.json).
        if (phase == Phase.Closed && !LevelGate.TryCase()) return false;

        if (phase != Phase.Closed)
        {
            if (IsOpen && screenPosition.HasValue) TakeOutOfIcon();
            layer?.BringToFront();
            return true;
        }

        Camera main = MainCamera();
        if (main == null || pages == null || pages.PaperRect == null) return false;
        store = null;
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            foreach (FolderToHUDIcon candidate in root.GetComponentsInChildren<FolderToHUDIcon>(true))
                if (candidate.IsFolderStored()) { store = candidate; break; }
        if (store == null) return false;

        savedParent = folder.parent;
        savedPosition = folder.position;
        savedRotation = folder.rotation;
        savedScale = folder.localScale;
        viewScale = folder.lossyScale;
        deskReturn = folder.GetComponent<OpenFolderReturnToDesk>();
        savedDeskReturnEnabled = deskReturn != null && deskReturn.enabled;
        if (deskReturn != null) deskReturn.enabled = false;
        savedActive = folder.gameObject.activeSelf;
        savedDragging = model != null && model.IsDraggingEnabled;
        if (model != null) model.SetDraggingEnabled(false);
        openedInMode = CameraController.Instance != null ? CameraController.Instance.CurrentMode : CameraController.CameraMode.FirstPerson;

        // Initialize the real model at rest before any temporary entrance scale.
        folder.gameObject.SetActive(true);
        folder.SetParent(null, true);
        phase = Phase.Opening;
        if (!pages.ShowFor3DView(Close))
        {
            Restore();
            return false;
        }
        FitReadingPose(main, screenPosition);
        fromScale = viewScale.x * (screenPosition.HasValue ? 0.4f : IconScale);
        fromRotation = toRotation * Quaternion.Euler(0f, 0f, 25f);
        float depth = Vector3.Dot(toPosition - main.transform.position, main.transform.forward);
        fromPosition = screenPosition.HasValue ? toPosition : IconWorldPoint(main, depth, toPosition);
        ApplyPose(fromPosition, fromRotation, fromScale);

        // The profile-viewer hold disables the world raycaster. This replacement
        // accepts this folder only, so its cover can still be returned safely.
        documentRaycaster = main.gameObject.AddComponent<DocumentPhysicsRaycaster>();
        documentRaycaster.Document = folder;
        documentRaycaster.enabled = HudWindowLock.IsHeld;

        grab = folder.gameObject.AddComponent<FloatingFolderGrab>();
        grab.Owner = this;

        // Floating in front of the camera it must never be cut by the world
        // (at the desk the tabletop is nearer than the reading distance), the
        // same rule as Level 2's floating Tax Code Book.
        if (layer != null)
        {
            layerWasAlwaysComposite = layer.AlwaysComposite;
            layer.AlwaysComposite = true;
        }

        phase = Phase.Opening;
        elapsed = 0f;
        layer?.BringToFront();
        if (screenPosition.HasValue) TakeOutOfIcon();
        // HUD opens bypass CaseFolderInteractable. Notify the guide and other
        // observers only after the real folder has successfully opened.
        GameplayEvents.RaiseCaseFolderOpened();
        return true;
    }

    public void Close()
    {
        if (phase != Phase.Opening && phase != Phase.Open) return;
        pages?.NotifyClosing();
        Camera main = MainCamera();
        fromPosition = folder.position;
        fromRotation = folder.rotation;
        fromScale = folder.localScale.x;
        float depth = main != null ? Vector3.Dot(fromPosition - main.transform.position, main.transform.forward) : 0.5f;
        toPosition = main != null ? IconWorldPoint(main, depth, fromPosition) : fromPosition;
        toRotation = fromRotation * Quaternion.Euler(0f, 0f, -20f);
        toScale = viewScale.x * IconScale;
        phase = Phase.Closing;
        elapsed = 0f;
    }

    private void Restore()
    {
        phase = Phase.Closed;
        if (documentRaycaster != null)
        {
            documentRaycaster.enabled = false;
            Destroy(documentRaycaster);
            documentRaycaster = null;
        }
        if (grab != null) Destroy(grab);
        grab = null;
        if (layer != null) layer.AlwaysComposite = layerWasAlwaysComposite;
        if (pages != null) pages.HideFor3DView();

        folder.SetParent(savedParent, true);
        folder.position = savedPosition;
        folder.rotation = savedRotation;
        folder.localScale = savedScale;
        // Back in the icon: the open folder is hidden (never left showing without its pages).
        folder.gameObject.SetActive(store == null && savedActive);
        if (model != null) model.SetDraggingEnabled(savedDragging);
        if (deskReturn != null) deskReturn.enabled = savedDeskReturnEnabled;
        phase = Phase.Closed;
        store?.ReturnFromView();
        store = null;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        RememberSeat();
        if (phase == Phase.Closed) return;

        // Sitting down at (or leaving) the desk while it is out: it goes back where it came from,
        // its icon (2026-10-26: with the usual close motion instead of a jump).
        CameraController cc = CameraController.Instance;
        if (cc != null && cc.CurrentMode != openedInMode &&
            cc.CurrentMode != CameraController.CameraMode.ProfileViewer)
        {
            if (phase == Phase.ToDesk) { FinishToDesk(); return; }
            if (phase == Phase.Opening || phase == Phase.Open) Close();
        }

        if (phase == Phase.Opening)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / OpenDuration);
            float travel = 1f - (1f - t) * (1f - t) * (1f - t);
            float pop = HudTheme.EaseOutBack(t);
            ApplyPose(Vector3.Lerp(fromPosition, toPosition, travel), Quaternion.SlerpUnclamped(fromRotation, toRotation, pop),
                Mathf.LerpUnclamped(fromScale, toScale, pop));
            if (t >= 1f)
            {
                ApplyPose(toPosition, toRotation, toScale);
                phase = Phase.Open;
            }
        }
        else if (phase == Phase.Closing)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / CloseDuration);
            float ease = t * t;
            ApplyPose(Vector3.Lerp(fromPosition, toPosition, ease), Quaternion.Slerp(fromRotation, toRotation, ease),
                Mathf.Lerp(fromScale, toScale, ease));
            if (t >= 1f) Restore();
        }
        else if (phase == Phase.ToDesk)
        {
            // Flying down onto the desk stack, closing as it lands; then it is the desk's folder again.
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / CloseDuration);
            float ease = t * t * (3f - 2f * t);
            ApplyPose(Vector3.Lerp(fromPosition, toPosition, ease), Quaternion.Slerp(fromRotation, toRotation, ease),
                Mathf.Lerp(fromScale, toScale, ease));
            if (t >= 1f) FinishToDesk();
        }
    }

    /// <summary>The open floating folder goes back on the desk stack: the icon slot empties (the hotbar follows the world).</summary>
    private void PutBackOnDesk()
    {
        if (phase != Phase.Open || deskReturn == null) return;
        pages?.NotifyClosing();
        fromPosition = folder.position;
        fromRotation = folder.rotation;
        fromScale = folder.localScale.x;
        // It ends just above the stack, where the closed folder's drop starts (one continuous motion).
        toPosition = deskReturn.DeskStackPosition + Vector3.up * 0.12f;
        toRotation = hasDeskPose ? deskRotation : fromRotation;
        toScale = fromScale * 0.6f;
        phase = Phase.ToDesk;
        elapsed = 0f;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
    }

    private void FinishToDesk()
    {
        FolderToHUDIcon owner = store;
        Restore();
        // Restore handed it back to the icon; now it leaves the icon for the desk (closed, on the stack).
        if (owner != null) owner.ResetToUnstored();
    }

    // =========================================================
    // GRAB (move by the cover, drop on the icon to close)
    // =========================================================

    internal void Dragged(PointerEventData data, Vector3 grabOffset)
    {
        if (phase != Phase.Open) return;
        Camera main = MainCamera();
        if (main == null) return;
        TakeOutOfIcon();
        float depth = Vector3.Dot(folder.position - main.transform.position, main.transform.forward);
        // The folder stops at the screen edge; the finger can carry on to the icon.
        Vector2 screen = held.Clamp(data.position);
        folder.position = main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth)) + grabOffset;
    }

    private HeldOnScreen held;

    internal Vector3 GrabOffset(PointerEventData data)
    {
        // Grabbing mid-entrance lands the model before calculating the offset.
        if (phase == Phase.Opening)
        {
            ApplyPose(toPosition, toRotation, toScale);
            phase = Phase.Open;
        }
        Camera main = MainCamera();
        if (main == null) return Vector3.zero;
        held = HeldOnScreen.ForModel(folder, main, data.position);
        float depth = Vector3.Dot(folder.position - main.transform.position, main.transform.forward);
        return folder.position - main.ScreenToWorldPoint(new Vector3(data.position.x, data.position.y, depth));
    }

    /// <summary>
    /// Released: over the bar (any slot or the Bag with the Level 1 hotbar; its icon otherwise) it closes back
    /// into it; seated at the desk over the folder stack, it goes back on the desk; anywhere else it stays open.
    /// </summary>
    internal void Dropped(PointerEventData data)
    {
        if (phase != Phase.Open) return;
        bool overBar = HudSlotDrop.Active ? HudSlotDrop.IsOver(data.position)
            : icon != null && RectTransformUtility.RectangleContainsScreenPoint(icon, data.position, null);
        if (overBar) { Close(); return; }
        CameraController cc = CameraController.Instance;
        if (cc != null && cc.CurrentMode == CameraController.CameraMode.Workstation && deskReturn != null &&
            deskReturn.IsOverDeskStack(data.position, MainCamera()))
            PutBackOnDesk();
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private void TakeOutOfIcon()
    {
        if (store != null) store.TryTakeOutForView();
    }

    /// <summary>
    /// The floating pose is the folder opened from the desk (one look for both). Seated at the desk:
    /// exactly the desk's open pose (a drop lands like a desk drop, a tap centres the page like the
    /// desk does). Away from the desk: that same pose relative to the camera, as seen from the seat
    /// (or the desk viewpoint before the player has sat down). Then all of it is kept on screen.
    /// Without any desk reference: the printed page faces the camera, fitted to the screen.
    /// </summary>
    private void FitReadingPose(Camera main, Vector2? screenPosition)
    {
        RectTransform paper = pages.PaperRect;
        Vector3 centre = folder.InverseTransformPoint(paper.TransformPoint(paper.rect.center));
        Vector2 screen = screenPosition ?? main.pixelRect.center;
        CameraController cc = CameraController.Instance;
        bool seated = cc != null && cc.CurrentMode == CameraController.CameraMode.Workstation;
        if (seated) RememberSeat();
        if (hasDeskPose && (seated || seatKnown || viewpoint != null))
        {
            Vector3 restPosition = deskDrag != null ? deskDrag.OpenRestPosition : deskPosition;
            Quaternion restRotation = deskDrag != null ? deskDrag.OpenRestRotation : deskRotation;
            toScale = deskLossyScale.x;
            if (seated)
            {
                toRotation = restRotation;
                toPosition = screenPosition.HasValue && deskDrag != null ? deskDrag.OpenPositionFor(screen) : restPosition;
            }
            else
            {
                Vector3 refPosition = seatKnown ? seatPosition : viewpoint.position;
                Quaternion inverse = Quaternion.Inverse(seatKnown ? seatRotation : viewpoint.rotation);
                Vector3 relative = inverse * (restPosition - refPosition);
                toRotation = main.transform.rotation * (inverse * restRotation);
                toPosition = screenPosition.HasValue
                    ? main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Max(main.nearClipPlane + 0.1f, relative.z)))
                    : main.transform.position + main.transform.rotation * relative;
            }
            ApplyPose(toPosition, toRotation, toScale);
            if (!screenPosition.HasValue)
            {
                // A tap centres the printed page, as the desk does.
                toPosition = DeskMotion.CentrePaper(main, toPosition, paper.TransformPoint(paper.rect.center));
                ApplyPose(toPosition, toRotation, toScale);
            }
            else
            {
                // A drop: the printed page sits exactly where the finger let go.
                toPosition = HeldOnScreen.CentreUnder(folder, paper.TransformPoint(paper.rect.center), main, screen);
            }
            toPosition = HeldOnScreen.FitOnScreen(folder, main);
            return;
        }
        Quaternion paperInFolder = Quaternion.Inverse(folder.rotation) * paper.rotation;
        toRotation = main.transform.rotation * Quaternion.Inverse(paperInFolder);
        float width = paper.rect.width * Mathf.Abs(paper.lossyScale.x);
        float height = paper.rect.height * Mathf.Abs(paper.lossyScale.y);
        float fitDepth = Mathf.Max(main.nearClipPlane + 0.3f, 0.75f);
        float multiplier = FitReadingScale(width, height, fitDepth, main.fieldOfView, main.aspect);
        toScale = viewScale.x * multiplier;
        float halfWidth = width * multiplier / (2f * fitDepth * Mathf.Tan(main.fieldOfView * Mathf.Deg2Rad * 0.5f) * main.aspect) * main.pixelRect.width * 0.5f;
        float halfHeight = height * multiplier / (2f * fitDepth * Mathf.Tan(main.fieldOfView * Mathf.Deg2Rad * 0.5f)) * main.pixelRect.height * 0.5f;
        screen.x = Mathf.Clamp(screen.x, main.pixelRect.xMin + halfWidth + 12f, main.pixelRect.xMax - halfWidth - 12f);
        screen.y = Mathf.Clamp(screen.y, main.pixelRect.yMin + halfHeight + 12f, main.pixelRect.yMax - halfHeight - 12f);
        Vector3 paperCentre = main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, fitDepth));
        toPosition = paperCentre - toRotation * Vector3.Scale(centre, viewScale) * multiplier;
    }

    /// <summary>While seated at the desk, the camera's pose is the reference for the folder's desk look.</summary>
    private void RememberSeat()
    {
        CameraController cc = CameraController.Instance;
        if (cc == null || cc.CurrentMode != CameraController.CameraMode.Workstation) return;   // the latest seated frame wins
        Camera main = MainCamera();
        if (main == null) return;
        seatPosition = main.transform.position;
        seatRotation = main.transform.rotation;
        seatKnown = true;
    }

    private static readonly Vector3[] PageCorners = new Vector3[4];

    /// <summary>Half the page's on-screen width and height at its current pose.</summary>
    private static Vector2 PageHalfExtents(Camera main, RectTransform paper)
    {
        paper.GetWorldCorners(PageCorners);
        Vector2 centre = main.WorldToScreenPoint(paper.TransformPoint(paper.rect.center));
        Vector2 half = Vector2.zero;
        for (int i = 0; i < 4; i++)
        {
            Vector2 corner = main.WorldToScreenPoint(PageCorners[i]);
            half = Vector2.Max(half, new Vector2(Mathf.Abs(corner.x - centre.x), Mathf.Abs(corner.y - centre.y)));
        }
        return half;
    }

    /// <summary>Pure page-fit calculation, also used by the internal regression runner.</summary>
    public static float FitReadingScale(float width, float height, float depth, float fieldOfView, float aspect)
    {
        if (width <= 0f || height <= 0f || depth <= 0f || aspect <= 0f || fieldOfView <= 0f || fieldOfView >= 179f) return 1f;
        double visibleHeight = 2d * depth * System.Math.Tan(fieldOfView * System.Math.PI / 360d);
        return (float)System.Math.Min(visibleHeight * aspect * 0.68d / width, visibleHeight * 0.68d / height);
    }

    private void ApplyPose(Vector3 position, Quaternion rotation, float scale)
    {
        folder.SetPositionAndRotation(position, rotation);
        folder.localScale = viewScale * (scale / Mathf.Max(1e-5f, viewScale.x));
    }

    private Vector3 IconWorldPoint(Camera main, float depth, Vector3 fallback)
    {
        if (icon == null || depth <= main.nearClipPlane) return fallback;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, icon.TransformPoint(icon.rect.center));
        return main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
    }

    private static Camera MainCamera() =>
        CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;

    private static Transform Find(Scene scene, string path)
    {
        int slash = path.IndexOf('/');
        string rootName = path.Substring(0, slash);
        string rest = path.Substring(slash + 1);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != rootName) continue;
            Transform t = root.transform.Find(rest);
            if (t != null) return t;
        }
        return null;
    }

    private void OnDisable()
    {
        if (phase != Phase.Closed && folder != null) Restore();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && phase != Phase.Closed && folder != null) Restore();
    }

    private void OnDestroy()
    {
        if (phase != Phase.Closed && folder != null) Restore();
    }
}
