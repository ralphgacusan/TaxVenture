using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// PURPOSE:
/// The Tutorial Hub inside the 3D Menu: Arthur's tutorial binder. Tapping the
/// 3D TUTORIAL button makes the menu camera lean down into the eyes of the
/// character standing at the computer desk ("yumuko"), while the binder is
/// picked up from the desk in front of him and held just above it, facing
/// him. His model is hidden while we look through his eyes. BACK puts the
/// binder down and the camera straightens up to the main menu again; the menu buttons pop out and back like the
/// settings board's. Built at runtime and added to the 3D Menu by itself
/// (no scene edits), drawn by a URP overlay camera on its own layer so the
/// office can never cover or cut it, and lit by the menu's own lights.
///
/// LEFT PAGE: binder title, Arthur's medallion and one greeting line, the
/// MASTERED count and BACK.
/// RIGHT PAGE: the open section's drill cards (TrainingBinderCard).
/// DIVIDER TABS on the right edge: BASICS is always there; SIDE QUEST and
/// CASE appear only once one of their cards is open (met in game). Tapping a
/// tab turns the page.
///
/// A card's PRACTICE / GUIDED / MASTERY button starts the drill
/// (TrainingSession). Coming back from a drill, the menu opens straight on
/// the binder (camera already leaning) and the card celebrates.
///
/// Reads the save through TrainingCatalog (never writes it).
/// </summary>
public sealed class TrainingBinder3D : MonoBehaviour
{
    // ---------- binder layout (binder pixels) ----------
    private const float PageWidth = 790f;
    private const float PageHeight = 930f;
    private const float PageX = 405f;
    private const float CoverHalfWidth = 860f;
    private const float CoverHalfHeight = 495f;
    private const float TabWidth = 160f;
    private const float TabHeight = 140f;
    private const float TabX = PageX + PageWidth * 0.5f + 82f;
    private const float TabSelectedReach = 26f;
    private const float TabHoverReach = 10f;
    // The spread is shifted so cover + tabs sit centred in front of the camera.
    private const float SpreadX = -(TabX + TabWidth * 0.5f + TabSelectedReach - CoverHalfWidth) * 0.5f;
    private const float FrameHalfWidth = CoverHalfWidth - SpreadX + 12f;
    private const float FrameHalfHeight = CoverHalfHeight + 30f;
    private const float CardSpacing = 252f;

    // ---------- on the desk, in front of the character ----------
    private const float FrameMargin = 1.14f;      // room around the binder so the desk shows at the edges
    private const int BinderLayer = 29;           // unnamed layer of its own (settings board 30, Doc3DLayer 31)

    [Tooltip("The computer desk the binder lies on (nearest one with this name to the menu camera).")]
    [SerializeField] private string deskName = "SM_Prop_Desk_03";
    [Tooltip("Its chair: used for the viewpoint when nobody stands at the desk.")]
    [SerializeField] private string chairName = "SM_Prop_Chair_07";
    [Tooltip("The character at the desk (name starts with this, nearest to the desk): the camera becomes his eyes.")]
    [SerializeField] private string characterPrefix = "npc_";
    [Tooltip("Eye height as a fraction of the character's height.")]
    [SerializeField, Range(0.7f, 1f)] private float eyeHeight = 0.92f;
    [Tooltip("How far past the desk's front edge the binder is held, in character heights.")]
    [SerializeField, Range(0f, 0.4f)] private float binderReach = 0.1f;
    [Tooltip("How far down the eyes look at the binder (degrees below level). Lower = looking more ahead.")]
    [SerializeField, Range(10f, 60f)] private float viewPitch = 30f;
    [Tooltip("The binder's top tips away from the eyes by this much (degrees), so it reads in 3D, held at an angle.")]
    [SerializeField, Range(0f, 45f)] private float binderTilt = 24f;
    [Tooltip("Each half of the open binder bends toward the eyes at the spine by this much (degrees), like an open binder held in both hands.")]
    [SerializeField, Range(0f, 25f)] private float foldAngle = 11f;
    [Tooltip("Gap between the binder's lower edge and the desk top, in character heights.")]
    [SerializeField, Range(0f, 0.2f)] private float binderLift = 0.02f;
    [Tooltip("Main menu objects hidden while the binder is open (the 3D menu buttons), restored on close.")]
    [SerializeField] private string[] hideWhileOpen = { "play_button", "tutorial_button", "settings_button", "sidequest_button" };
    [Tooltip("How much the office darkens behind the binder (0 = not at all).")]
    [SerializeField, Range(0f, 0.6f)] private float backdropDim = 0.12f;

    private const float LeanSeconds = 1.15f;
    private const float StraightenSeconds = 1.0f;
    private const float RaiseSeconds = 0.6f;
    private const float LowerSeconds = 0.38f;
    private const float FlipSeconds = 0.52f;
    private const float CardStagger = 0.07f;

    private static readonly Color Cloth = new Color32(38, 60, 92, 255);
    private static readonly Color PageCream = new Color32(246, 241, 228, 255);
    private static readonly Color PageEdge = new Color32(222, 214, 194, 255);

    public static TrainingBinder3D Instance { get; private set; }

    private enum Phase { Closed, Leaning, Open, Lowering, Straightening, Leaving }

    private sealed class TabVisual
    {
        public string Id;
        public Transform Node;
        public Material Face;
        public Color Color;
        public CareerBoardTarget Target;
        public float Reach;
    }

    private readonly List<Material> materials = new List<Material>();
    private readonly List<TabVisual> tabs = new List<TabVisual>();
    private readonly List<TrainingBinderCard> cards = new List<TrainingBinderCard>();
    private readonly List<KeyValuePair<GameObject, bool>> hidden = new List<KeyValuePair<GameObject, bool>>();

    private Camera viewCamera;
    private Camera overlayCamera;
    private ToggleCamera cameraManager;
    private SettingsBoard3D settingsBoard;
    private Transform root;           // the binder's resting pose above the desk
    private Transform boardRoot;      // the binder (animated inside root: picked up from the desk)
    private Transform leftHalf;       // the two halves fold at the spine
    private Transform rightHalf;
    private Transform flipLeaf;
    private Transform pageContent;
    private Transform tabRoot;
    private RectTransform masteredFill;
    private Image dim;
    private float masteredTarget;

    private TrainingCatalogFile catalog;
    private TrainingHubView view;
    private string currentTab;
    private string pendingTab;

    private Phase phase = Phase.Closed;
    private float phaseTime;
    private float raiseTime;          // 0..1 binder up
    private float openTime;           // seconds since the binder finished rising (bar fill)
    private float flipTime = -1f;
    private float boardScale = 0.0016f;
    private float lastAspect = -1f;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private Vector3 leanPosition;
    private Quaternion leanRotation;
    private Vector3 fromPosition;
    private Quaternion fromRotation;
    private Vector3 binderPosition;
    private Quaternion binderRotation;
    private Vector3 flatPosition;     // lying on the desk before it is picked up
    private Quaternion flatRotation;
    private readonly List<Renderer> characterRenderers = new List<Renderer>();
    private bool characterHidden;
    private Action afterLeave;

    private CareerBoardTarget hoverTarget;
    private CareerBoardTarget pressTarget;

    /// <summary>True from the tap on TUTORIAL until the camera is back at the main menu.</summary>
    public bool IsOpen => phase != Phase.Closed;

    // =========================================================
    // BOOTSTRAP (adds itself to the 3D Menu; no scene edits)
    // =========================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeForLoadedScenes()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            EnsureForScene(SceneManager.GetSceneAt(i));
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureForScene(scene);

    private static void EnsureForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != MenuEntry.MenuSceneName) return;
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            if (sceneRoot.GetComponentInChildren<TrainingBinder3D>(true) != null) return;
        var host = new GameObject("Training Binder 3D");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<TrainingBinder3D>();
    }

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        Instance = this;
        TrainingSession.End();   // never practising while the menu is open
        // The runtime NEW GAME / CONTINUE notes hide with the modelled ones (T03).
        hideWhileOpen = MenuEntry.WithStoryButtons(hideWhileOpen);
    }

    private IEnumerator Start()
    {
        cameraManager = FindAnyObjectByType<ToggleCamera>();
        settingsBoard = FindAnyObjectByType<SettingsBoard3D>();
        yield return null;   // ToggleCamera.Start has stored the menu camera's own pose by now

        // Back from a drill: open straight on the binder, the camera already leaning.
        if (TrainingSession.ConsumeOpenBinder()) Open(immediate: true);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (overlayCamera != null) Destroy(overlayCamera.gameObject);
        foreach (Material material in materials) CareerBoardKit.Dispose(material);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        TickButtonPops(dt);
        if (phase == Phase.Closed || root == null) return;

        phaseTime += dt;
        TickPhase(dt);
        if (phase == Phase.Closed) return;
        SyncOverlayCamera();
        ApplyBinderPose();
        TickFlip(dt);
        TickTabs(dt);
        TickMasteredBar(dt);
        if (dim != null)
            dim.color = new Color(0.02f, 0.03f, 0.05f, backdropDim * Smoother(raiseTime));

        bool ready = phase == Phase.Open && raiseTime >= 0.85f && flipTime < 0f;
        if (!ready)
        {
            ClearPointer();
            return;
        }
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }
        HandlePointer();
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    /// <summary>Leans the camera to the desk and raises the binder. False when the menu is busy.</summary>
    public bool Open(bool immediate = false)
    {
        if (!MenuEntry.HasCareerAccess) return false;
        if (phase != Phase.Closed) return true;
        if (cameraManager != null && (cameraManager.IsTransitioning || cameraManager.IsLevelSelectorVisible)) return false;
        if (settingsBoard != null && settingsBoard.IsOpen) return false;
        viewCamera = cameraManager != null && cameraManager.mainCam != null ? cameraManager.mainCam : Camera.main;
        if (viewCamera == null || !viewCamera.isActiveAndEnabled) return false;
        catalog = TrainingCatalogFile.Load();
        view = TrainingCatalog.Build(catalog, PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.CurrentProgress : null);
        Rebuild();
        UseOverlayCamera();

        restPosition = viewCamera.transform.position;
        restRotation = viewCamera.transform.rotation;
        fromPosition = restPosition;
        fromRotation = restRotation;
        HideMenuButtons(immediate);
        lastAspect = -1f;
        ComputePoses();
        root.gameObject.SetActive(true);
        openTime = 0f;

        if (immediate)
        {
            viewCamera.transform.SetPositionAndRotation(leanPosition, leanRotation);
            HideCharacter(true);
            raiseTime = 1f;
            SetPhase(Phase.Open);
        }
        else
        {
            raiseTime = 0f;
            SetPhase(Phase.Leaning);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_ui_panel_open_01", 0.7f);
        SyncOverlayCamera();
        ApplyBinderPose();
        return true;
    }

    /// <summary>Lowers the binder and straightens the camera back to the main menu.</summary>
    public void Close()
    {
        if (phase != Phase.Open && phase != Phase.Leaning) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_ui_back_01", 0.65f);
        SetPhase(Phase.Lowering);
    }

    private void SetPhase(Phase next)
    {
        phase = next;
        phaseTime = 0f;
    }

    private void TickPhase(float dt)
    {
        switch (phase)
        {
            case Phase.Leaning:
            {
                float t = Mathf.Clamp01(phaseTime / LeanSeconds);
                MoveCamera(fromPosition, fromRotation, leanPosition, leanRotation, t);
                if (t >= 0.4f) HideCharacter(true);    // we are about to look through his eyes
                // The binder is picked up from the desk over the second half of the lean.
                raiseTime = Mathf.Clamp01((phaseTime - LeanSeconds * 0.45f) / RaiseSeconds);
                if (t >= 1f && raiseTime >= 1f) SetPhase(Phase.Open);
                break;
            }
            case Phase.Open:
                openTime += dt;
                break;
            case Phase.Lowering:
            case Phase.Leaving:
            {
                raiseTime = Mathf.Max(0f, raiseTime - dt / LowerSeconds);
                if (phase == Phase.Leaving)
                {
                    if (raiseTime > 0f) break;
                    Action then = afterLeave;
                    afterLeave = null;
                    then?.Invoke();
                    break;
                }
                if (raiseTime > 0.45f) break;   // the camera starts to straighten while the binder is still going down
                fromPosition = viewCamera.transform.position;
                fromRotation = viewCamera.transform.rotation;
                RestoreMenuButtons();
                SetPhase(Phase.Straightening);
                break;
            }
            case Phase.Straightening:
            {
                raiseTime = Mathf.Max(0f, raiseTime - dt / (LowerSeconds * 0.45f));
                float t = Mathf.Clamp01(phaseTime / StraightenSeconds);
                MoveCamera(fromPosition, fromRotation, restPosition, restRotation, t);
                if (t >= 0.3f) HideCharacter(false);
                if (t >= 1f) FinishClose();
                break;
            }
        }
    }

    /// <summary>
    /// The lean: smootherstep, with the height easing a little later than the
    /// forward move, so it reads as bending down over the desk ("yumuko"), not sliding.
    /// </summary>
    private void MoveCamera(Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot, float t)
    {
        if (viewCamera == null) return;
        float move = Smoother(t);
        float drop = Smoother(Mathf.Clamp01((t - 0.08f) / 0.92f));
        Vector3 flatFrom = new Vector3(fromPos.x, 0f, fromPos.z);
        Vector3 flatTo = new Vector3(toPos.x, 0f, toPos.z);
        Vector3 flat = Vector3.Lerp(flatFrom, flatTo, move);
        float y = Mathf.Lerp(fromPos.y, toPos.y, drop);
        viewCamera.transform.SetPositionAndRotation(new Vector3(flat.x, y, flat.z), Quaternion.Slerp(fromRot, toRot, move));
    }

    private void FinishClose()
    {
        if (viewCamera != null) viewCamera.transform.SetPositionAndRotation(restPosition, restRotation);
        phase = Phase.Closed;
        HideCharacter(false);
        ClearPointer();
        if (root != null) root.gameObject.SetActive(false);
        flipTime = -1f;
    }

    /// <summary>
    /// The camera becomes the eyes of the character at the desk; the binder
    /// rests just above the desk right in front of him, facing his eyes, sized
    /// to fill the view with a margin. Without a character the chair is used,
    /// and without a desk the binder floats where the camera already looks.
    /// </summary>
    private void ComputePoses()
    {
        float fov = viewCamera.fieldOfView;
        float aspect = viewCamera.aspect > 0f ? viewCamera.aspect : 16f / 9f;
        Transform desk = Nearest(deskName, viewCamera.transform.position);
        if (desk == null)
        {
            leanPosition = viewCamera.transform.position;
            leanRotation = viewCamera.transform.rotation;
            PlaceBinder(leanPosition + leanRotation * Vector3.forward * 2f, leanPosition, fov, aspect);
            flatPosition = binderPosition;
            flatRotation = binderRotation;
            return;
        }
        Bounds deskBounds = Bounds(desk);
        float deskTop = deskBounds.max.y;

        Transform character = NearestWithPrefix(characterPrefix, deskBounds.center, deskBounds.extents.magnitude * 2f);
        characterRenderers.Clear();
        Vector3 feet;
        float height;
        if (character != null)
        {
            feet = character.position;
            Bounds body = Bounds(character);
            height = Mathf.Max(0.5f, body.max.y - feet.y);
            foreach (Renderer r in character.GetComponentsInChildren<Renderer>())
                if (r.enabled) characterRenderers.Add(r);
        }
        else
        {
            Transform chair = Nearest(chairName, deskBounds.center);
            feet = chair != null ? chair.position : deskBounds.center - viewCamera.transform.forward * deskBounds.extents.z * 2f;
            feet.y = deskBounds.min.y;
            height = (deskTop - feet.y) * 2.4f;
        }

        Vector3 toDesk = deskBounds.center - feet;
        toDesk.y = 0f;
        Vector3 dir = toDesk.sqrMagnitude > 1e-4f ? toDesk.normalized : Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up).normalized;
        Vector3 eye = feet + Vector3.up * (height * eyeHeight) + dir * (height * 0.05f);

        // Held ahead of him, past the desk's front edge, along a gentle downward look.
        Vector3 edge = deskBounds.ClosestPoint(new Vector3(feet.x, deskTop, feet.z));
        float front = Vector3.Dot(edge - feet, dir);
        float ahead = Mathf.Max(height * 0.12f, front + height * binderReach);
        float pitch = viewPitch * Mathf.Deg2Rad;
        Vector3 center = eye + (dir * Mathf.Cos(pitch) + Vector3.down * Mathf.Sin(pitch)) * (ahead / Mathf.Cos(pitch));
        // Never through the desk: its lower edge stays above the desk top (raised if needed).
        for (int i = 0; i < 3; i++)
        {
            PlaceBinder(center, eye, fov, aspect);
            Vector3 up = binderRotation * Quaternion.Euler(binderTilt, 0f, 0f) * Vector3.up;
            float lowest = center.y - CoverHalfHeight * boardScale * up.y;
            float floor = deskTop + height * binderLift;
            if (lowest >= floor) break;
            center.y += floor - lowest;
        }
        PlaceBinder(center, eye, fov, aspect);

        leanPosition = eye;
        leanRotation = Quaternion.LookRotation(center - eye, Vector3.up);
        // Picked up from here: lying flat on the desk, top away from him.
        flatPosition = new Vector3(center.x, deskTop + 0.02f, center.z);
        flatRotation = Quaternion.LookRotation(Vector3.down, dir);
    }

    /// <summary>Binder centred at <paramref name="center"/>, facing the eye, scaled to fit the view with a margin.</summary>
    private void PlaceBinder(Vector3 center, Vector3 eye, float fov, float aspect)
    {
        float distance = Mathf.Max(0.05f, Vector3.Distance(center, eye));
        float visibleHeight = 2f * distance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        boardScale = Mathf.Min(visibleHeight / (2f * FrameHalfHeight * FrameMargin),
                               visibleHeight * aspect / (2f * FrameHalfWidth * FrameMargin));
        binderPosition = center;
        binderRotation = Quaternion.LookRotation(center - eye, Vector3.up);
    }

    /// <summary>We look through his eyes, so his own model is switched off (and back on when we leave).</summary>
    private void HideCharacter(bool hide)
    {
        if (hide == characterHidden) return;
        characterHidden = hide;
        foreach (Renderer r in characterRenderers)
            if (r != null) r.enabled = !hide;
    }

    private Transform Nearest(string objectName, Vector3 point)
    {
        Transform best = null;
        float bestDistance = float.MaxValue;
        foreach (GameObject sceneRoot in gameObject.scene.GetRootGameObjects())
            foreach (Transform node in sceneRoot.GetComponentsInChildren<Transform>(true))
            {
                if (node.name != objectName) continue;
                float d = (node.position - point).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = node; }
            }
        return best;
    }

    private Transform NearestWithPrefix(string prefix, Vector3 point, float within)
    {
        if (string.IsNullOrEmpty(prefix)) return null;
        Transform best = null;
        float bestDistance = within * within;
        foreach (GameObject sceneRoot in gameObject.scene.GetRootGameObjects())
            foreach (Transform node in sceneRoot.GetComponentsInChildren<Transform>())
            {
                if (!node.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                Vector3 d = node.position - point;
                d.y = 0f;
                if (d.sqrMagnitude < bestDistance) { bestDistance = d.sqrMagnitude; best = node; }
            }
        return best;
    }

    private static Bounds Bounds(Transform t)
    {
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(t.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    // =========================================================
    // HELD IN FRONT OF THE CAMERA
    // =========================================================

    private void SyncOverlayCamera()
    {
        if (overlayCamera == null || viewCamera == null) return;
        overlayCamera.fieldOfView = viewCamera.fieldOfView;
        // A short depth range of its own: the binder's thin layers never shimmer.
        overlayCamera.nearClipPlane = Mathf.Max(0.01f, viewCamera.nearClipPlane * 0.25f);
        overlayCamera.farClipPlane = 40f;
        float aspect = viewCamera.aspect > 0f ? viewCamera.aspect : 16f / 9f;
        if (Mathf.Abs(aspect - lastAspect) < 0.001f) return;
        bool first = lastAspect < 0f;
        lastAspect = aspect;
        if (!first) ComputePoses();   // screen shape changed: refit
    }

    /// <summary>Picked up from the desk (flat, then tilted up toward his eyes), with a faint hand-held sway once it is up.</summary>
    private void ApplyBinderPose()
    {
        if (root == null || boardRoot == null) return;
        root.SetPositionAndRotation(binderPosition, binderRotation);
        float k = Smoother(raiseTime);
        Quaternion inverse = Quaternion.Inverse(binderRotation);
        Vector3 flatLocal = inverse * (flatPosition - binderPosition);
        Quaternion flatLocalRotation = inverse * flatRotation;
        float t = Time.unscaledTime;
        float sway = k * k;
        float scaleNow = boardScale;
        Vector3 bob = new Vector3(Mathf.Sin(t * 0.8f), Mathf.Sin(t * 1.1f), 0f) * (2.5f * scaleNow * sway);
        // Lift arcs up a little in the middle, as a binder is picked up.
        Vector3 arc = Vector3.up * (Mathf.Sin(k * Mathf.PI) * CoverHalfHeight * 0.25f * scaleNow);
        boardRoot.position = root.TransformPoint(Vector3.Lerp(flatLocal, Vector3.zero, k) + bob) + arc;
        // Held: the top tips away from the eyes, so the binder reads in 3D.
        Quaternion held = Quaternion.Euler(binderTilt, 0f, 0f);
        boardRoot.localRotation = Quaternion.Slerp(flatLocalRotation, held, k) *
            Quaternion.Euler(Mathf.Sin(t * 0.7f) * 0.3f * sway, Mathf.Sin(t * 0.55f + 0.7f) * 0.25f * sway, Mathf.Sin(t * 0.6f) * 0.2f * sway);
        boardRoot.localScale = Vector3.one * scaleNow;
        // Picking it up bends the open halves toward you at the spine, like holding it in both hands.
        float fold = foldAngle * k + Mathf.Sin(t * 0.9f) * 0.25f * sway;
        if (leftHalf != null) leftHalf.localRotation = Quaternion.Euler(0f, -fold, 0f);
        if (rightHalf != null) rightHalf.localRotation = Quaternion.Euler(0f, fold, 0f);
    }

    /// <summary>Smootherstep: no jolt at the start or the end.</summary>
    private static float Smoother(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    // =========================================================
    // OVERLAY CAMERA (same approach as the settings board)
    // =========================================================

    private void UseOverlayCamera()
    {
        if (viewCamera == null) return;
        UniversalAdditionalCameraData baseData = viewCamera.GetUniversalAdditionalCameraData();
        if (baseData == null || baseData.renderType != CameraRenderType.Base) return;
        if (overlayCamera == null)
        {
            var go = new GameObject("Training Binder Camera");
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            overlayCamera = go.AddComponent<Camera>();
            overlayCamera.cullingMask = 1 << BinderLayer;
            overlayCamera.clearFlags = CameraClearFlags.Depth;
            UniversalAdditionalCameraData overlayData = overlayCamera.GetUniversalAdditionalCameraData();
            overlayData.renderType = CameraRenderType.Overlay;
            overlayData.renderShadows = false;
        }
        overlayCamera.transform.SetParent(viewCamera.transform, false);
        overlayCamera.transform.localPosition = Vector3.zero;
        overlayCamera.transform.localRotation = Quaternion.identity;
        baseData.cameraStack.Remove(overlayCamera);
        baseData.cameraStack.Add(overlayCamera);
        viewCamera.cullingMask &= ~(1 << BinderLayer);
    }

    private void BuildDim()
    {
        var go = new GameObject("Backdrop Dim", typeof(RectTransform), typeof(Canvas));
        go.layer = BinderLayer;
        go.transform.SetParent(root, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = -50;
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(200f, 140f);
        rect.localPosition = new Vector3(0f, 0f, 0.6f);
        RectTransform fill = HudTheme.CreateRect("Dim", rect);
        HudTheme.Stretch(fill);
        fill.gameObject.layer = BinderLayer;
        dim = fill.gameObject.AddComponent<Image>();
        dim.color = new Color(0.02f, 0.03f, 0.05f, 0f);
        dim.raycastTarget = false;
    }

    // =========================================================
    // BINDER
    // =========================================================

    private void Rebuild()
    {
        if (root != null) Destroy(root.gameObject);
        foreach (Material material in materials) CareerBoardKit.Dispose(material);
        materials.Clear();
        tabs.Clear();
        cards.Clear();
        currentTab = null;
        pendingTab = null;

        root = new GameObject("Binder Holder").transform;
        root.gameObject.layer = BinderLayer;
        root.SetParent(transform, false);
        boardRoot = CareerBoardKit.Node("Training Binder", root, Vector3.zero);
        Transform binder = CareerBoardKit.Node("Spread", boardRoot, new Vector3(SpreadX, 0f, 0f));

        // Two halves hinged at the spine (x = 0), so the open binder can fold toward the eyes.
        leftHalf = CareerBoardKit.Node("Left half", binder, Vector3.zero);
        rightHalf = CareerBoardKit.Node("Right half", binder, Vector3.zero);
        Material brass = CareerBoardKit.Lit(HudTheme.Brass, materials, 0.6f);
        CareerBoardKit.Box(binder, "Spine", new Vector3(0f, 0f, 24f), new Vector3(70f, CoverHalfHeight * 2f, 28f), Cloth, materials);
        foreach (float side in new[] { -1f, 1f })
        {
            Transform half = side < 0f ? leftHalf : rightHalf;
            CareerBoardKit.Box(half, "Cover", new Vector3(side * CoverHalfWidth * 0.5f, 0f, 22f),
                new Vector3(CoverHalfWidth, CoverHalfHeight * 2f, 24f), Cloth, materials);
            foreach (float y in new[] { 1f, -1f })
                CareerBoardKit.Box(half, "Corner", new Vector3(side * (CoverHalfWidth - 30f), y * (CoverHalfHeight - 30f), 8f),
                    new Vector3(60f, 60f, 6f), brass);
            CareerBoardKit.Box(half, "Page stack", new Vector3(side * (PageX + 4f), -5f, 12f),
                new Vector3(PageWidth + 6f, PageHeight + 6f, 10f), PageEdge, materials);
            CareerBoardKit.Box(half, "Page", new Vector3(side * PageX, 0f, 6f), new Vector3(PageWidth, PageHeight, 2f), PageCream, materials);
            CareerBoardKit.Box(half, "Crease", new Vector3(side * 13f, 0f, 4.6f), new Vector3(26f, PageHeight, 0.5f), PageEdge, materials);
        }
        foreach (float y in new[] { -300f, 0f, 300f })
        {
            MeshRenderer ring = CareerBoardKit.Box(binder, "Ring", new Vector3(0f, y, -6f), new Vector3(46f, 34f, 46f), brass,
                PrimitiveType.Cylinder);
            ring.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        BuildLeftPage(leftHalf);
        flipLeaf = CareerBoardKit.Node("Page flip", rightHalf, Vector3.zero);
        CareerBoardKit.Box(flipLeaf, "Leaf", new Vector3(PageX, 0f, 3f), new Vector3(PageWidth - 6f, PageHeight - 6f, 1f), PageCream, materials);
        pageContent = CareerBoardKit.Node("Section", flipLeaf, new Vector3(PageX, 0f, 0f));
        tabRoot = CareerBoardKit.Node("Tabs", rightHalf, Vector3.zero);
        BuildTabs();
        BuildDim();

        string wanted = TrainingSession.ReturnTab;
        TrainingTabView start = FindVisibleTab(wanted) ?? FirstVisibleTab();
        ShowTab(start != null ? start.Def.id : null, animate: false, cardDelay: LeanSeconds * 0.75f);
        SnapTabs();
        CelebrateReturn();
        CareerBoardKit.SetLayerRecursively(root, BinderLayer);
        root.gameObject.SetActive(false);
    }

    private void BuildLeftPage(Transform binder)
    {
        Transform page = CareerBoardKit.Node("Left page", binder, new Vector3(-PageX, 0f, 0f));
        Color ink = CareerBoardKit.Ink;
        CareerBoardKit.Text(page, "Title", catalog.title ?? "TUTORIAL BINDER", 56f, new Vector3(0f, 388f, -2f), 700f, 70f, ink);
        CareerBoardKit.Text(page, "Owner", $"{catalog.owner}'s drills for the new consultant", 30f, new Vector3(0f, 330f, -2f),
            700f, 40f, CareerBoardKit.InkSoft, TextAlignmentOptions.Center, FontStyles.Normal, HudTheme.HandwritingFont);
        CareerBoardKit.Box(page, "Rule", new Vector3(0f, 296f, -2f), new Vector3(620f, 4f, 1f), HudTheme.Brass, materials);

        Material brass = CareerBoardKit.Lit(HudTheme.Brass, materials, 0.15f);   // matte: no highlight sliding over it as the binder sways
        MeshRenderer medal = CareerBoardKit.Box(page, "Medallion", new Vector3(-230f, 140f, -4f), new Vector3(210f, 6f, 210f), brass,
            PrimitiveType.Cylinder);
        medal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        MeshRenderer face = CareerBoardKit.Box(page, "Medallion face", new Vector3(-230f, 140f, -13f), new Vector3(194f, 2f, 194f),
            CareerBoardKit.Lit(new Color32(250, 214, 120, 255), materials, 0f), PrimitiveType.Cylinder);
        face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Sprite portrait = LoadPortrait();
        if (portrait != null) CareerBoardKit.Sprite(page, "Arthur", portrait, new Vector3(-230f, 136f, -19f), 186f, Color.white, 4);

        CareerBoardKit.Box(page, "Bubble", new Vector3(110f, 140f, -3f), new Vector3(420f, 190f, 2f), CareerBoardKit.PaperYellow, materials)
            .transform.localRotation = Quaternion.Euler(0f, 0f, -1.2f);
        CareerBoardKit.Text(page, "Greeting", PickGreeting(), 29f, new Vector3(110f, 140f, -5f), 380f, 170f, ink,
            TextAlignmentOptions.Center, FontStyles.Normal, HudTheme.HandwritingFont);

        CareerBoardKit.Text(page, "Mastered", $"MASTERED  {view.Mastered} / {view.Total}", 40f,
            new Vector3(0f, -70f, -2f), 640f, 54f, ink);
        masteredFill = CareerBoardKit.Bar(page, "Mastered bar", new Vector3(0f, -130f, -2f), 520f, 24f, TrainingBinderCard.StampGreen, out _);
        masteredTarget = view.Total > 0 ? (float)view.Mastered / view.Total : 0f;
        CareerBoardKit.SetFill(masteredFill, 0f);

        if (view.Tabs.Exists(t => !t.Visible))
            CareerBoardKit.Text(page, "Tip", "More drills appear here as you play the chapters.", 26f, new Vector3(0f, -205f, -2f),
                640f, 40f, CareerBoardKit.InkSoft, TextAlignmentOptions.Center, FontStyles.Normal, HudTheme.HandwritingFont);

        CareerBoardButton back = CareerBoardButton.Create(page, "Back", "BACK", new Vector3(0f, -365f, -12f), new Vector2(260f, 92f),
            HudTheme.Brass, HudTheme.PanelInner, 34f);
        back.Tapped += _ => Close();
    }

    /// <summary>The MASTERED bar fills from empty once the binder is up.</summary>
    private void TickMasteredBar(float dt)
    {
        if (masteredFill == null) return;
        CareerBoardKit.SetFill(masteredFill, masteredTarget * Smoother(Mathf.Clamp01((openTime - 0.1f) / 0.9f)));
    }

    private string PickGreeting()
    {
        if (catalog.greetings == null || catalog.greetings.Length == 0) return "Back for a refresher, rookie? Pick a drill.";
        return catalog.greetings[Random.Range(0, catalog.greetings.Length)];
    }

    private static Sprite LoadPortrait()
    {
        const string path = "Tutorials/Portraits/Arthur-Default";
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;
        Texture2D tex = Resources.Load<Texture2D>(path);
        return tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : null;
    }

    // =========================================================
    // TABS
    // =========================================================

    private TrainingTabView FindVisibleTab(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (TrainingTabView tab in view.Tabs)
            if (tab.Visible && tab.Def.id == id) return tab;
        return null;
    }

    private TrainingTabView FirstVisibleTab()
    {
        foreach (TrainingTabView tab in view.Tabs)
            if (tab.Visible) return tab;
        return null;
    }

    /// <summary>Built once per opening; selection and hover only glide and recolour them.</summary>
    private void BuildTabs()
    {
        float y = 330f;
        foreach (TrainingTabView tab in view.Tabs)
        {
            if (!tab.Visible) continue;
            var visual = new TabVisual { Id = tab.Def.id, Color = tab.Def.ParsedColor };
            visual.Node = CareerBoardKit.Node("Tab " + tab.Def.id, tabRoot, new Vector3(TabX, y, 2f));
            visual.Face = CareerBoardKit.Lit(visual.Color, materials, 0.3f);
            CareerBoardKit.Box(visual.Node, "Tab", Vector3.zero, new Vector3(TabWidth, TabHeight, 6f), visual.Face);
            CareerBoardKit.Box(visual.Node, "Tab shine", new Vector3(0f, TabHeight * 0.36f, -3.2f), new Vector3(TabWidth - 24f, 10f, 0.5f),
                Color.Lerp(Color.white, visual.Color, 0.15f), materials);
            CareerBoardKit.Text(visual.Node, "Label", (tab.Def.title ?? tab.Def.id).ToUpperInvariant(), 27f, new Vector3(0f, -4f, -4f),
                140f, 110f, Color.white);
            visual.Target = visual.Node.gameObject.AddComponent<CareerBoardTarget>();
            visual.Target.AddHitBox(visual.Node, Vector3.zero, new Vector3(TabWidth, TabHeight, 30f));
            string id = tab.Def.id;
            visual.Target.Tapped += _ => ShowTab(id, animate: true);
            tabs.Add(visual);
            y -= 160f;
        }
    }

    private float TabReachGoal(TabVisual tab)
    {
        if (tab.Id == (pendingTab ?? currentTab)) return TabSelectedReach;
        return tab.Target == hoverTarget ? TabHoverReach : 0f;
    }

    private void SnapTabs()
    {
        foreach (TabVisual tab in tabs) tab.Reach = TabReachGoal(tab);
        TickTabs(0f);
    }

    private void TickTabs(float dt)
    {
        float k = dt <= 0f ? 1f : CareerBoardKit.Approach(14f, dt);
        foreach (TabVisual tab in tabs)
        {
            tab.Reach = Mathf.Lerp(tab.Reach, TabReachGoal(tab), k);
            Vector3 p = tab.Node.localPosition;
            tab.Node.localPosition = new Vector3(TabX + tab.Reach, p.y, p.z);
            float lit = Mathf.Clamp01(tab.Reach / TabSelectedReach);
            CareerBoardKit.SetColor(tab.Face, Color.Lerp(Color.Lerp(tab.Color, Color.black, 0.22f), tab.Color, lit));
        }
    }

    // =========================================================
    // SECTION AND PAGE TURN
    // =========================================================

    private void ShowTab(string id, bool animate, float cardDelay = 0f)
    {
        if (id == null) return;
        if (animate)
        {
            if (id == currentTab || flipTime >= 0f) return;
            pendingTab = id;
            flipTime = 0f;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_page_turn_01", 0.55f);
            return;
        }
        currentTab = id;
        TrainingSession.ReturnTab = id;
        BuildSection(cardDelay);
    }

    /// <summary>The page leans in toward the spine (easing in), the new section appears edge-on, and it eases out flat.</summary>
    private void TickFlip(float dt)
    {
        if (flipTime < 0f || flipLeaf == null) return;
        flipTime += dt;
        float t = Mathf.Clamp01(flipTime / FlipSeconds);
        float lift = Mathf.Sin(t * Mathf.PI) * 34f;
        float angle;
        if (t < 0.5f)
        {
            float a = t / 0.5f;
            angle = -88f * a * a;
        }
        else
        {
            if (pendingTab != null)
            {
                string next = pendingTab;
                pendingTab = null;
                ShowTab(next, animate: false, cardDelay: 0.04f);
                CareerBoardKit.SetLayerRecursively(pageContent, BinderLayer);
            }
            float b = 1f - (t - 0.5f) / 0.5f;
            angle = 88f * b * b;
        }
        flipLeaf.localRotation = Quaternion.Euler(0f, angle, 0f);
        flipLeaf.localPosition = new Vector3(0f, 0f, -lift);
        if (t >= 1f)
        {
            flipLeaf.localRotation = Quaternion.identity;
            flipLeaf.localPosition = Vector3.zero;
            flipTime = -1f;
        }
    }

    private void BuildSection(float cardDelay)
    {
        for (int i = pageContent.childCount - 1; i >= 0; i--) Destroy(pageContent.GetChild(i).gameObject);
        cards.Clear();
        TrainingTabView tab = FindVisibleTab(currentTab);
        if (tab == null) return;

        Color accent = tab.Def.ParsedColor;
        CareerBoardKit.Text(pageContent, "Section", (tab.Def.title ?? tab.Def.id).ToUpperInvariant(), 46f,
            new Vector3(0f, 400f, -2f), 700f, 60f, CareerBoardKit.Ink);
        CareerBoardKit.Box(pageContent, "Section rule", new Vector3(0f, 362f, -2f), new Vector3(220f, 8f, 1f), accent, materials);

        float y = 222f;
        int index = 0;
        foreach (TrainingCardView card in tab.Cards)
        {
            TrainingBinderCard built = TrainingBinderCard.Create(pageContent, new Vector3(0f, y, -4f), card, accent);
            built.PracticeRequested += StartDrill;
            built.SlideIn(cardDelay + index * CardStagger);
            cards.Add(built);
            y -= CardSpacing;
            index++;
        }
        if (tab.Cards.Count == 0)
            CareerBoardKit.Text(pageContent, "Empty", "No drills here yet.", 32f, Vector3.zero, 600f, 50f, CareerBoardKit.InkSoft,
                TextAlignmentOptions.Center, FontStyles.Normal, HudTheme.HandwritingFont);
    }

    /// <summary>Back from a drill: that card hops (and its stamp slams once) after the binder is up.</summary>
    private void CelebrateReturn()
    {
        string returned = TrainingSession.ReturnCard;
        bool mastered = TrainingSession.ReturnNewlyMastered;
        TrainingSession.ConsumeReturn();
        if (returned == null) return;
        foreach (TrainingBinderCard card in cards)
            if (card.View.Def.id == returned) card.Celebrate(mastered, 0.45f);
    }

    // =========================================================
    // STARTING A DRILL
    // =========================================================

    private void StartDrill(TrainingBinderCard card, bool mastery)
    {
        if (phase != Phase.Open || card == null || !card.View.Unlocked) return;
        TrainingDrill drill = TrainingDrill.Load(card.View.Def.drill);
        if (drill == null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_ui_unavailable_01", 0.65f);
            card.Celebrate(false, 0f);   // a hop: nothing to start (missing drill file, see the Console)
            return;
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_ui_panel_close_01", 0.7f);
        afterLeave = () => TrainingSession.Begin(card.View.Def, drill, mastery);
        SetPhase(Phase.Leaving);
    }

    // =========================================================
    // MENU BUTTONS (pop out and back, like the settings board)
    // =========================================================

    private const float PopOutDuration = 0.24f;
    private const float PopInDuration = 0.42f;
    private const float PopStagger = 0.06f;

    private sealed class ButtonPop
    {
        public GameObject Button;
        public bool Showing;
        public float Delay;
        public float Elapsed;
    }

    private readonly List<ButtonPop> pops = new List<ButtonPop>();
    private readonly Dictionary<GameObject, Vector3> buttonScales = new Dictionary<GameObject, Vector3>();

    private void HideMenuButtons(bool immediate)
    {
        hidden.Clear();
        int order = 0;
        foreach (GameObject sceneRoot in gameObject.scene.GetRootGameObjects())
            foreach (Transform node in sceneRoot.GetComponentsInChildren<Transform>(true))
            {
                if (Array.IndexOf(hideWhileOpen, node.name) < 0 || node.IsChildOf(transform)) continue;
                GameObject button = node.gameObject;
                hidden.Add(new KeyValuePair<GameObject, bool>(button, button.activeSelf));
                if (!button.activeSelf) continue;
                if (immediate) button.SetActive(false);
                else StartPop(button, showing: false, delay: order++ * PopStagger);
            }
    }

    private void RestoreMenuButtons()
    {
        int order = 0;
        foreach (KeyValuePair<GameObject, bool> entry in hidden)
            if (entry.Key != null && entry.Value)
                StartPop(entry.Key, showing: true, delay: 0.35f + order++ * PopStagger);
        hidden.Clear();
    }

    private void StartPop(GameObject button, bool showing, float delay)
    {
        if (!buttonScales.ContainsKey(button)) buttonScales[button] = button.transform.localScale;
        pops.RemoveAll(p => p.Button == button);
        if (showing)
        {
            button.transform.localScale = Vector3.zero;
            button.SetActive(true);
        }
        pops.Add(new ButtonPop { Button = button, Showing = showing, Delay = delay });
    }

    private void TickButtonPops(float dt)
    {
        for (int i = pops.Count - 1; i >= 0; i--)
        {
            ButtonPop pop = pops[i];
            if (pop.Button == null) { pops.RemoveAt(i); continue; }
            Vector3 size = buttonScales[pop.Button];
            pop.Elapsed += dt;
            float time = pop.Elapsed - pop.Delay;
            if (time < 0f) continue;
            if (pop.Showing)
            {
                float t = Mathf.Clamp01(time / PopInDuration);
                pop.Button.transform.localScale = size * HudTheme.EaseOutBack(t);
                if (t >= 1f) { pop.Button.transform.localScale = size; pops.RemoveAt(i); }
            }
            else
            {
                float t = Mathf.Clamp01(time / PopOutDuration);
                float factor = t < 0.33f ? Mathf.Lerp(1f, 1.12f, HudTheme.EaseOutCubic(t / 0.33f))
                    : Mathf.Lerp(1.12f, 0f, Mathf.Pow((t - 0.33f) / 0.67f, 2f));
                pop.Button.transform.localScale = size * factor;
                if (t >= 1f)
                {
                    pop.Button.SetActive(false);
                    pop.Button.transform.localScale = size;
                    pops.RemoveAt(i);
                }
            }
        }
    }

    // =========================================================
    // INPUT (same handling as the career board)
    // =========================================================

    private void HandlePointer()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null || viewCamera == null) return;
        Vector2 position = pointer.position.ReadValue();
        bool pressed = pointer.press.wasPressedThisFrame;
        bool released = pointer.press.wasReleasedThisFrame;
        bool canHover = pointer is Mouse || pointer is Pen;

        CareerBoardTarget target = RaycastTarget(position);
        SetHoverTarget(canHover || pointer.press.isPressed ? target : null);

        if (pressed)
        {
            pressTarget = target;
            if (pressTarget != null) pressTarget.SetPressed(true);
        }
        if (released)
        {
            CareerBoardTarget pressedTarget = pressTarget;
            if (pressedTarget != null) pressedTarget.SetPressed(false);
            if (pressedTarget != null && pressedTarget == target && target.Interactable)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
                target.Tap();
            }
            pressTarget = null;
            if (!canHover) SetHoverTarget(null);
        }
    }

    private CareerBoardTarget RaycastTarget(Vector2 screen)
    {
        Ray ray = viewCamera.ScreenPointToRay(screen);
        // Only the binder's layer: the office behind it never takes the touch.
        if (!Physics.Raycast(ray, out RaycastHit hit, 50f, 1 << BinderLayer, QueryTriggerInteraction.Collide))
            return null;
        CareerBoardTarget target = hit.collider.GetComponentInParent<CareerBoardTarget>();
        return target != null && target.transform.IsChildOf(root) ? target : null;
    }

    private void SetHoverTarget(CareerBoardTarget target)
    {
        if (hoverTarget == target) return;
        if (hoverTarget != null) hoverTarget.SetHover(false);
        hoverTarget = target;
        if (hoverTarget != null) hoverTarget.SetHover(true);
    }

    private void ClearPointer()
    {
        SetHoverTarget(null);
        if (pressTarget != null) pressTarget.SetPressed(false);
        pressTarget = null;
    }
}
