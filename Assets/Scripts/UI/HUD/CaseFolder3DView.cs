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

    private enum Phase { Closed, Opening, Open, Closing }

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

    public bool IsOpen => phase == Phase.Opening || phase == Phase.Open;
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
        if (pages != null) pages.HideFor3DView();

        folder.SetParent(savedParent, true);
        folder.position = savedPosition;
        folder.rotation = savedRotation;
        folder.localScale = savedScale;
        folder.gameObject.SetActive(savedActive);
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
        if (phase == Phase.Closed) return;

        // Sitting down at (or leaving) the desk while it floats: put it back.
        CameraController cc = CameraController.Instance;
        if (cc != null && cc.CurrentMode != openedInMode &&
            cc.CurrentMode != CameraController.CameraMode.ProfileViewer)
        {
            Restore();
            return;
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
        folder.position = main.ScreenToWorldPoint(new Vector3(data.position.x, data.position.y, depth)) + grabOffset;
    }

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
        float depth = Vector3.Dot(folder.position - main.transform.position, main.transform.forward);
        return folder.position - main.ScreenToWorldPoint(new Vector3(data.position.x, data.position.y, depth));
    }

    internal void Dropped(PointerEventData data)
    {
        if (icon != null && RectTransformUtility.RectangleContainsScreenPoint(icon, data.position, null)) Close();
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private void TakeOutOfIcon()
    {
        if (store != null) store.TryTakeOutForView();
    }

    // Fit the printed page itself, rather than the stale desk pose of its cover.
    private void FitReadingPose(Camera main, Vector2? screenPosition)
    {
        RectTransform paper = pages.PaperRect;
        Vector3 centre = folder.InverseTransformPoint(paper.TransformPoint(paper.rect.center));
        Quaternion paperInFolder = Quaternion.Inverse(folder.rotation) * paper.rotation;
        toRotation = main.transform.rotation * Quaternion.Inverse(paperInFolder);
        float width = paper.rect.width * Mathf.Abs(paper.lossyScale.x);
        float height = paper.rect.height * Mathf.Abs(paper.lossyScale.y);
        float depth = Mathf.Max(main.nearClipPlane + 0.3f, 0.75f);
        float multiplier = FitReadingScale(width, height, depth, main.fieldOfView, main.aspect);
        toScale = viewScale.x * multiplier;
        Vector2 screen = screenPosition ?? main.pixelRect.center;
        float halfWidth = width * multiplier / (2f * depth * Mathf.Tan(main.fieldOfView * Mathf.Deg2Rad * 0.5f) * main.aspect) * main.pixelRect.width * 0.5f;
        float halfHeight = height * multiplier / (2f * depth * Mathf.Tan(main.fieldOfView * Mathf.Deg2Rad * 0.5f)) * main.pixelRect.height * 0.5f;
        screen.x = Mathf.Clamp(screen.x, main.pixelRect.xMin + halfWidth + 12f, main.pixelRect.xMax - halfWidth - 12f);
        screen.y = Mathf.Clamp(screen.y, main.pixelRect.yMin + halfHeight + 12f, main.pixelRect.yMax - halfHeight - 12f);
        Vector3 paperCentre = main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        toPosition = paperCentre - toRotation * Vector3.Scale(centre, viewScale) * multiplier;
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
