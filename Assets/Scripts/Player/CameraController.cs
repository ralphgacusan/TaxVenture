using UnityEngine;
using System.Collections;

/// <summary>
/// CAMERA CONTROLLER
///
/// Normal mode:
/// - FirstPersonCameraController controls the Main Camera.
/// - Player can move and interact normally.
/// - Virtual joystick is visible.
///
/// Workstation mode:
/// - Player movement is disabled.
/// - FirstPersonCameraController is disabled.
/// - Virtual joystick is hidden.
/// - Main Camera moves to the assigned workstation viewpoint.
/// - Workstation interaction is enabled.
///
/// Interview mode:
/// - Player movement and normal interaction are disabled.
/// - FirstPersonCameraController is disabled.
/// - Virtual joystick is hidden.
/// - Main Camera moves to the assigned interview viewpoint.
///
/// The same camera writer also supports outdoor third-person exploration.
///
/// Milestone T03 (Office_Level1 only, ExplorationRules.AdoptsMixedCamera):
/// exploration (CameraMode.FirstPerson, serialized value unchanged) has two
/// views. Outdoors the same FirstPersonCameraController writes a collision-
/// shortened third-person orbit and Elijah's ExplorationBody is shown;
/// indoors it is the original first-person view with hands. The region
/// changes on the office entrance's threshold line with a hysteresis band
/// (floor detection only away from doorways: spawns, placements, reloads),
/// and never while a task, conversation, viewer, hold or transition owns the
/// controls, or while the previous view blend runs. Every return path keeps
/// the current view, because the view lives here, not in the task.
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    public enum CameraMode
    {
        FirstPerson,
        Workstation,
        Interview,
        CorkboardSideQuest,
        ProfileViewer
    }

    public CameraMode CurrentMode { get; private set; }
        = CameraMode.FirstPerson;

    /// <summary>
    /// Raised after the player leaves a workstation view (e.g. Exit Desk),
    /// once first-person control has been restored.
    /// </summary>
    public static event System.Action WorkstationExited;

    // =============================================================
    // REFERENCES
    // =============================================================

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private FirstPersonCameraController firstPersonCamera;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Interactor playerInteractor;
    [SerializeField] private VirtualJoystick virtualJoystick;
    [SerializeField] private VirtualJoystick lookJoystick;

    [Header("First Person Hands")]
    [SerializeField] private FirstPersonHands firstPersonHands;

    [Header("Workstation")]
    [SerializeField] private WorkstationUI workstationUI;
    [SerializeField] private WorkstationInteractor workstationInteractor;

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 0.5f;

    [Header("Desk Highlights")]
    [SerializeField] private DeskItemHighlight[] deskItemHighlights;

    private Coroutine activeTransition;

    private Behaviour[] profileViewerControls;
    private bool[] profileViewerControlStates;
    private bool profileViewerJoystickVisible;
    private bool profileViewerLookJoystickVisible;
    private Rigidbody profileViewerBody;
    private RigidbodyConstraints profileViewerBodyConstraints;
    private CursorLockMode profileViewerCursorLock;
    private bool profileViewerCursorVisible;

    private enum DeferredControls { None, Locked, Unlocked }
    private bool dialogueHoldsControls;
    private int dialogueHoldVersion;
    private DeferredControls controlsRequestedDuringDialogue;

    public bool TryBeginProfileViewer()
    {
        if (!isActiveAndEnabled || CurrentMode != CameraMode.FirstPerson || activeTransition != null ||
            profileViewerControls != null)
            return false;
        // T05: a conversation or popup already holds the controls. The viewer's
        // snapshot would capture them locked and restore them locked on close.
        if (dialogueHoldsControls || (firstPersonCamera != null && !firstPersonCamera.enabled))
            return false;

        profileViewerControls = new Behaviour[]
        {
            firstPersonCamera, playerMovement, playerInteractor, workstationInteractor,
            mainCamera != null ? mainCamera.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>() : null
        };
        profileViewerControlStates = new bool[profileViewerControls.Length];
        for (int i = 0; i < profileViewerControls.Length; i++)
        {
            if (profileViewerControls[i] == null) continue;
            profileViewerControlStates[i] = profileViewerControls[i].enabled;
            profileViewerControls[i].enabled = false;
        }
        if (playerInteractor != null) playerInteractor.ClearFocus();
        if (lookJoystick != null)
        {
            profileViewerLookJoystickVisible = lookJoystick.gameObject.activeSelf;
            lookJoystick.Hide();
        }
        if (virtualJoystick != null)
        {
            profileViewerJoystickVisible = virtualJoystick.gameObject.activeSelf;
            virtualJoystick.SetVisible(false);
        }
        profileViewerBody = playerMovement != null ? playerMovement.GetComponent<Rigidbody>() : null;
        if (profileViewerBody != null)
        {
            profileViewerBodyConstraints = profileViewerBody.constraints;
            if (!profileViewerBody.isKinematic)
            {
                profileViewerBody.linearVelocity = Vector3.zero;
                profileViewerBody.angularVelocity = Vector3.zero;
            }
            profileViewerBody.constraints = RigidbodyConstraints.FreezeAll;
        }
        profileViewerCursorLock = Cursor.lockState;
        profileViewerCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        CurrentMode = CameraMode.ProfileViewer;
        return true;
    }

    public void EndProfileViewer()
    {
        if (profileViewerControls == null) return;
        if (profileViewerBody != null) profileViewerBody.constraints = profileViewerBodyConstraints;
        // Other camera modes manage movement themselves, but not the viewer's click-through guard.
        int raycasterIndex = profileViewerControls.Length - 1;
        if (profileViewerControls[raycasterIndex] != null)
            profileViewerControls[raycasterIndex].enabled = profileViewerControlStates[raycasterIndex];
        if (CurrentMode == CameraMode.ProfileViewer)
        {
            CurrentMode = CameraMode.FirstPerson;
            for (int i = 0; i < profileViewerControls.Length; i++)
                if (profileViewerControls[i] != null)
                    profileViewerControls[i].enabled = profileViewerControlStates[i];
            if (virtualJoystick != null) virtualJoystick.SetVisible(profileViewerJoystickVisible);
            if (lookJoystick != null) lookJoystick.SetVisible(profileViewerLookJoystickVisible);
            Cursor.lockState = profileViewerCursorLock;
            Cursor.visible = profileViewerCursorVisible;
            MobileInputGuard.SuppressWorldTapsThisFrame();
        }
        profileViewerControls = null;
        profileViewerControlStates = null;
        profileViewerBody = null;
    }

    private void OnDisable()
    {
        EndProfileViewer();
    }

    // =============================================================
    // INITIALIZATION
    // =============================================================

    // =============================================================
    // NO TAPS WHILE THE CAMERA MOVES (2026-10-26)
    // =============================================================

    /// <summary>A short guard after the camera arrives, so a tap made during the move does not land.</summary>
    private const float ArrivalTapGuard = 0.15f;
    private float inputHeldUntil = -1f;
    private GameObject transitionBlocker;

    /// <summary>The camera is moving between views (desk, corkboard, interview).</summary>
    public bool IsTransitioning => activeTransition != null;

    /// <summary>
    /// Nothing can be tapped: the camera is moving into a view (or just
    /// arrived). A full-screen invisible blocker above every UI swallows
    /// touches, so neither the HUD nor the world reacts until it is done.
    /// </summary>
    public bool InputHeld => activeTransition != null || Time.unscaledTime < inputHeldUntil;

    private void LateUpdate()
    {
        bool held = InputHeld;
        if (held && transitionBlocker == null) transitionBlocker = BuildTransitionBlocker();
        if (transitionBlocker != null && transitionBlocker.activeSelf != held) transitionBlocker.SetActive(held);
    }

    private GameObject BuildTransitionBlocker()
    {
        var host = new GameObject("Camera Move Input Blocker", typeof(RectTransform));
        host.transform.SetParent(transform, false);
        var canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        host.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        var shield = new GameObject("Shield", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        var rect = (RectTransform)shield.transform;
        rect.SetParent(host.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = shield.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;
        return host;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "[CameraController] Main Camera could not be found!"
            );
        }
    }

    private void Start()
    {
        CurrentMode = CameraMode.FirstPerson;
        SetupMixedExploration();

        // Enable normal first-person systems.
        SetFirstPersonSystems(true);

        if (workstationInteractor != null)
            workstationInteractor.enabled = false;

        if (workstationUI != null)
            workstationUI.Hide();

        HideDeskHighlights();

        if (firstPersonHands != null)
            firstPersonHands.Show();

        DebugTrace.Log(
            "[CameraController] Initialized in FirstPerson mode."
        );
    }

    // =============================================================
    // T03: MIXED EXPLORATION (outdoor third person / indoor first person)
    // =============================================================

    private bool mixedExploration;
    private ExplorationRules.Region region = ExplorationRules.Region.Indoor;
    private ExplorationBody body;
    private JumpButton jumpButton;
    private float floorDisagreeSince = -1f;
    private float nextFloorCheck;
    private ExplorationRules.Region? floorRegion;
    private readonly RaycastHit[] floorHits = new RaycastHit[16];
    private bool bodyHiddenForTask;

    /// <summary>T03: this scene plays the mixed outdoor/indoor exploration loop.</summary>
    public bool MixedExplorationAdopted => mixedExploration;
    /// <summary>T03: the region the exploration view follows.</summary>
    public ExplorationRules.Region Region => region;
    /// <summary>T03: exploration is (heading to) the outdoor third-person view.</summary>
    public bool IsThirdPersonView => mixedExploration && CameraWriter != null && CameraWriter.ThirdPerson;

    /// <summary>
    /// T03: the look controller that actually writes the Main Camera. The office scenes carry two
    /// (one on Main Camera, one on a helper object this controller references); both get the same
    /// view so their poses agree, and only the Main Camera one moves the camera.
    /// </summary>
    private FirstPersonCameraController CameraWriter
    {
        get
        {
            Camera cam = MainCamera;
            FirstPersonCameraController onCamera = cam != null ? cam.GetComponent<FirstPersonCameraController>() : null;
            return onCamera != null ? onCamera : firstPersonCamera;
        }
    }
    /// <summary>T03: the player's own eye point (reach and line of sight are measured from it, never the follow camera).</summary>
    public Vector3 PlayerEye => CameraWriter != null ? CameraWriter.EyePosition : (PlayerRoot != null ? PlayerRoot.position : Vector3.zero);
    public ExplorationBody Body => body;
    /// <summary>T03: the gameplay movement the exploration camera follows (grounding, take-off floor).</summary>
    public PlayerMovement Movement => playerMovement;
    public JumpButton Jump => jumpButton;

    private void SetupMixedExploration()
    {
        mixedExploration = ExplorationRules.AdoptsMixedCamera(gameObject.scene.name) && firstPersonCamera != null && CameraWriter != null && playerMovement != null;
        // Sprint remains available when the stored third-person/jump pilot is disabled.
        if (virtualJoystick != null) virtualJoystick.ShowSprintRing(ExplorationRules.AdoptsSprint(gameObject.scene.name));
        if (!mixedExploration) return;
        // One canonical Elijah height (ExplorationRules.ElijahRestHeight) for exploration and conversation.
        body = ExplorationBody.Ensure(playerMovement);
        if (body != null && !string.IsNullOrEmpty(body.Problem)) Debug.LogWarning("[CameraController] T03 exploration body: " + body.Problem);
        jumpButton = JumpButton.Create(virtualJoystick, playerMovement);
        ExplorationPracticeCurb.Ensure(gameObject.scene, PlayerRoot);
        floorRegion = FloorRegion();
        region = floorRegion ?? ExplorationRules.Region.Outdoor;
        ApplyExplorationView(true);
    }

    private void Update()
    {
        if (!mixedExploration) return;
        HideBodyDuringTasks();
        ExplorationRules.Region next = NextRegion();
        if (next == region) return;
        FirstPersonCameraController writer = CameraWriter;
        bool exploring = isActiveAndEnabled && CurrentMode == CameraMode.FirstPerson && writer != null && writer.enabled && firstPersonCamera.enabled;
        // Deferred, not dropped: the next free frame re-evaluates where the player actually is.
        if (!ExplorationRules.CanSwitchView(exploring, activeTransition != null, DialogueUI.IsAnyDialogueActive || dialogueHoldsControls,
                PlayerControlHold.IsHeld, profileViewerControls != null, writer.IsBlending)) return;
        region = next;
        ApplyExplorationView(false);
    }

    /// <summary>The doorway line decides near the entrance; elsewhere the floor (after it has disagreed for a moment).</summary>
    private ExplorationRules.Region NextRegion()
    {
        Transform player = PlayerRoot;
        if (player == null) return region;
        if (OfficeDoorGate.TryGetEntrance(out Vector3 centre, out Vector3 outward, out float halfWidth))
        {
            Vector3 offset = player.position - centre;
            offset.y = 0f;
            float past = Vector3.Dot(offset, outward);
            float along = Vector3.Dot(offset, Vector3.Cross(Vector3.up, outward));
            if (Mathf.Abs(along) <= halfWidth + 0.5f && Mathf.Abs(past) <= 3f)
            {
                floorDisagreeSince = -1f;
                return ExplorationRules.RegionAtDoor(region, past, along, halfWidth);
            }
        }
        if (Time.unscaledTime >= nextFloorCheck)
        {
            nextFloorCheck = Time.unscaledTime + 0.2f;
            floorRegion = FloorRegion();
        }
        if (!floorRegion.HasValue || floorRegion.Value == region) { floorDisagreeSince = -1f; return region; }
        if (floorDisagreeSince < 0f) floorDisagreeSince = Time.unscaledTime;
        return ExplorationRules.RegionFromFloor(region, floorRegion, Time.unscaledTime - floorDisagreeSince);
    }

    /// <summary>Which root the floor under the player belongs to (the same grouping AudioEnvironment uses); bounded cast.</summary>
    private ExplorationRules.Region? FloorRegion()
    {
        Transform player = PlayerRoot;
        if (player == null) return null;
        int count = Physics.RaycastNonAlloc(player.position + Vector3.up * 0.5f, Vector3.down, floorHits, 5f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        ExplorationRules.Region? found = null;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = floorHits[i];
            if (hit.collider.transform.IsChildOf(player) || hit.distance >= nearest) continue;
            for (Transform part = hit.collider.transform; part != null; part = part.parent)
            {
                bool office = string.Equals(part.name, "OfficeBuilding", System.StringComparison.OrdinalIgnoreCase);
                bool outside = string.Equals(part.name, "Outside", System.StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(part.name, "Env", System.StringComparison.OrdinalIgnoreCase);
                if (!office && !outside) continue;
                found = outside ? ExplorationRules.Region.Outdoor : ExplorationRules.Region.Indoor;
                nearest = hit.distance;
                break;
            }
        }
        return found;
    }

    /// <summary>One view for writer, body and hands together.</summary>
    private void ApplyExplorationView(bool instant)
    {
        bool tp = ExplorationRules.ThirdPersonFor(region);
        FirstPersonCameraController writer = CameraWriter;
        if (writer != null) writer.SetThirdPerson(tp, instant);
        if (firstPersonCamera != null && firstPersonCamera != writer) firstPersonCamera.SetThirdPerson(tp, true);
        if (body != null) body.SetPerspectiveShown(tp);
        FirstPersonHands hands = Hands;
        if (hands != null) hands.SetThirdPersonView(tp, body != null ? body.LeftHand : null);
        DebugTrace.Log("[CameraController] Exploration view: " + (tp ? "third person (outdoors)" : "first person (indoors)"));
    }

    /// <summary>Fixed task views (desk, interview, corkboard) never show the outdoor body in their frame.</summary>
    private void HideBodyDuringTasks()
    {
        if (body == null) return;
        bool task = CurrentMode == CameraMode.Workstation || CurrentMode == CameraMode.Interview || CurrentMode == CameraMode.CorkboardSideQuest;
        if (task == bodyHiddenForTask) return;
        bodyHiddenForTask = task;
        if (task) body.HideFor(this); else body.ReleaseHide(this);
    }

    // =============================================================
    // ENTER WORKSTATION / DESK VIEW
    // =============================================================

    public void EnterFirstPerson(
        Transform viewpoint,
        string exitLabel = "Exit Desk",
        bool showHands = true)
    {
        DebugTrace.Log(
            "[CameraController] EnterFirstPerson called."
        );

        // ---------------------------------------------------------
        // VALIDATE VIEWPOINT
        // ---------------------------------------------------------

        if (viewpoint == null)
        {
            Debug.LogError(
                "[CameraController] DeskViewpoint is NULL!"
            );

            return;
        }

        // ---------------------------------------------------------
        // VALIDATE CAMERA
        // ---------------------------------------------------------

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "[CameraController] Main Camera could not be found!"
            );

            return;
        }

        // ---------------------------------------------------------
        // IF SOMEHOW STUCK IN ANOTHER MODE
        // ---------------------------------------------------------

        if (CurrentMode != CameraMode.FirstPerson)
        {
            Debug.LogWarning(
                "[CameraController] Current mode is "
                + CurrentMode
                + ". Resetting to FirstPerson."
            );

            ForceResetToFirstPerson();
        }

        // ---------------------------------------------------------
        // DEBUG VIEWPOINT TRANSFORM
        // ---------------------------------------------------------

        DebugTrace.Log(
            "[CameraController] DeskViewpoint WORLD POSITION: "
            + viewpoint.position
        );

        DebugTrace.Log(
            "[CameraController] DeskViewpoint WORLD ROTATION: "
            + viewpoint.rotation.eulerAngles
        );

        DebugTrace.Log(
            "[CameraController] DeskViewpoint LOCAL POSITION: "
            + viewpoint.localPosition
        );

        DebugTrace.Log(
            "[CameraController] DeskViewpoint LOCAL ROTATION: "
            + viewpoint.localRotation.eulerAngles
        );

        // ---------------------------------------------------------
        // CHANGE MODE
        // ---------------------------------------------------------

        CurrentMode = CameraMode.Workstation;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_chair_sit_01");

        // ---------------------------------------------------------
        // STOP FIRST-PERSON CAMERA
        // ---------------------------------------------------------

        if (firstPersonCamera != null)
        {
            firstPersonCamera.enabled = false;

            DebugTrace.Log(
                "[CameraController] FirstPersonCamera disabled."
            );
        }
        else
        {
            Debug.LogWarning(
                "[CameraController] FirstPersonCamera reference is NULL!"
            );
        }

        // ---------------------------------------------------------
        // STOP PLAYER MOVEMENT
        // ---------------------------------------------------------

        if (playerMovement != null)
            playerMovement.enabled = false;

        // ---------------------------------------------------------
        // HIDE VIRTUAL JOYSTICK
        // -------------------------------------------------------------

        if (virtualJoystick != null)
        {
            virtualJoystick.Hide();

            DebugTrace.Log(
                "[CameraController] Virtual joystick hidden."
            );
        }
        if (lookJoystick != null) lookJoystick.Hide();

        // ---------------------------------------------------------
        // STOP NORMAL PLAYER INTERACTION
        // ---------------------------------------------------------

        if (playerInteractor != null)
        {
            playerInteractor.ClearFocus();
            playerInteractor.enabled = false;
        }

        // Desk taps start only once the camera has arrived (2026-10-26): see the transition below.

        // ---------------------------------------------------------
        // HANDS
        // ---------------------------------------------------------

        if (firstPersonHands != null)
        {
            if (showHands)
                firstPersonHands.Show();
            else
                firstPersonHands.Hide();
        }

        // ---------------------------------------------------------
        // STOP PREVIOUS CAMERA TRANSITION
        // ---------------------------------------------------------

        StopActiveTransition();

        // ---------------------------------------------------------
        // MOVE CAMERA TO DESK VIEWPOINT
        // ---------------------------------------------------------

        DebugTrace.Log(
            "[CameraController] Moving camera to: "
            + viewpoint.name
        );

        activeTransition = StartCoroutine(
            MoveCameraToViewpoint(
                viewpoint,
                () =>
                {
                    DebugTrace.Log(
                        "[CameraController] Workstation view reached."
                    );

                    if (CurrentMode == CameraMode.Workstation && workstationInteractor != null)
                        workstationInteractor.enabled = true;

                    if (workstationUI != null)
                        workstationUI.Show(exitLabel);

                    ShowDeskHighlights();
                }
            )
        );
    }

    // =============================================================
    // FORCE RESET TO FIRST PERSON
    // =============================================================

    private void ForceResetToFirstPerson()
    {
        DebugTrace.Log(
            "[CameraController] ForceResetToFirstPerson called."
        );

        StopActiveTransition();

        CurrentMode = CameraMode.FirstPerson;

        if (workstationUI != null)
            workstationUI.Hide();

        HideDeskHighlights();

        if (workstationInteractor != null)
            workstationInteractor.enabled = false;

        SetFirstPersonSystems(true);

        if (firstPersonHands != null)
            firstPersonHands.Show();
    }

    // =============================================================
    // CAMERA TRANSITION
    // =============================================================

    private IEnumerator MoveCameraToViewpoint(
        Transform viewpoint,
        System.Action onComplete)
    {
        if (mainCamera == null)
        {
            Debug.LogError(
                "[CameraController] Cannot move camera because Main Camera is NULL!"
            );

            yield break;
        }

        if (viewpoint == null)
        {
            Debug.LogError(
                "[CameraController] Cannot move camera because viewpoint is NULL!"
            );

            yield break;
        }

        // ---------------------------------------------------------
        // GET START TRANSFORM
        // ---------------------------------------------------------

        Vector3 startPosition =
            mainCamera.transform.position;

        Quaternion startRotation =
            mainCamera.transform.rotation;

        // ---------------------------------------------------------
        // GET TARGET TRANSFORM
        // ---------------------------------------------------------

        Vector3 targetPosition =
            viewpoint.position;

        Quaternion targetRotation =
            viewpoint.rotation;

        // ---------------------------------------------------------
        // DEBUG START
        // ---------------------------------------------------------

        DebugTrace.Log(
            "[CameraController] Camera transition START\n" +
            "Camera Position: " + startPosition + "\n" +
            "Target Position: " + targetPosition + "\n" +
            "Camera Rotation: " + startRotation.eulerAngles + "\n" +
            "Target Rotation: " + targetRotation.eulerAngles
        );

        // ---------------------------------------------------------
        // INSTANT TRANSITION
        // ---------------------------------------------------------

        if (transitionDuration <= 0f)
        {
            mainCamera.transform.SetPositionAndRotation(
                targetPosition,
                targetRotation
            );

            DebugTrace.Log(
                "[CameraController] Camera transition COMPLETE."
            );

            activeTransition = null;

            onComplete?.Invoke();

            StartCoroutine(
                DebugCameraAfterTransition(
                    targetPosition,
                    targetRotation
                )
            );

            yield break;
        }

        // ---------------------------------------------------------
        // SMOOTH TRANSITION
        // ---------------------------------------------------------

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / transitionDuration
            );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            Vector3 currentPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );

            Quaternion currentRotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    t
                );

            mainCamera.transform.SetPositionAndRotation(
                currentPosition,
                currentRotation
            );

            yield return null;
        }

        // ---------------------------------------------------------
        // GUARANTEE EXACT FINAL TRANSFORM
        // ---------------------------------------------------------

        mainCamera.transform.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );

        // ---------------------------------------------------------
        // FINAL TRANSITION DEBUG
        // ---------------------------------------------------------

        DebugTrace.Log(
            "[CameraController] Camera transition COMPLETE\n" +
            "Final Position: "
            + mainCamera.transform.position
            + "\nFinal Rotation: "
            + mainCamera.transform.rotation.eulerAngles
        );

        activeTransition = null;
        // The tap that was still down when the camera arrived is not a new tap.
        inputHeldUntil = Time.unscaledTime + ArrivalTapGuard;
        MobileInputGuard.SuppressWorldTapsThisFrame();

        onComplete?.Invoke();

        // ---------------------------------------------------------
        // NEXT-FRAME CAMERA CHECK
        // ---------------------------------------------------------

        StartCoroutine(
            DebugCameraAfterTransition(
                targetPosition,
                targetRotation
            )
        );
    }

    // =============================================================
    // NEXT-FRAME CAMERA DIAGNOSTIC
    // =============================================================

    private IEnumerator DebugCameraAfterTransition(
        Vector3 expectedPosition,
        Quaternion expectedRotation)
    {
        // Wait exactly one frame.
        yield return null;

        if (mainCamera == null)
        {
            Debug.LogError(
                "[CameraController] NEXT-FRAME CHECK: Main Camera is NULL!"
            );

            yield break;
        }

        Vector3 actualPosition =
            mainCamera.transform.position;

        Quaternion actualRotation =
            mainCamera.transform.rotation;

        float positionDifference =
            Vector3.Distance(
                expectedPosition,
                actualPosition
            );

        float rotationDifference =
            Quaternion.Angle(
                expectedRotation,
                actualRotation
            );

        DebugTrace.Log(
            "[CameraController] CAMERA CHECK - NEXT FRAME\n" +
            "Expected Position: " + expectedPosition + "\n" +
            "Actual Position: " + actualPosition + "\n" +
            "Position Difference: " + positionDifference + "\n" +
            "Expected Rotation: " + expectedRotation.eulerAngles + "\n" +
            "Actual Rotation: " + actualRotation.eulerAngles + "\n" +
            "Rotation Difference: " + rotationDifference
        );

        if (positionDifference > 0.01f ||
            rotationDifference > 0.5f)
        {
            Debug.LogError(
                "[CameraController] WARNING: " +
                "Something changed the Main Camera after the transition!"
            );
        }
        else
        {
            DebugTrace.Log(
                "[CameraController] Camera remained at the correct " +
                "workstation viewpoint on the next frame."
            );
        }
    }

    // =============================================================
    // EXIT WORKSTATION / DESK VIEW
    // =============================================================

    public void ExitFirstPerson()
    {
        if (CurrentMode != CameraMode.Workstation)
            return;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_chair_stand_01");

        DebugTrace.Log(
            "[CameraController] Exiting desk view."
        );

        StopActiveTransition();

        CurrentMode = CameraMode.FirstPerson;

        // ---------------------------------------------------------
        // HIDE WORKSTATION UI
        // ---------------------------------------------------------

        if (workstationUI != null)
            workstationUI.Hide();

        // ---------------------------------------------------------
        // HIDE DESK HIGHLIGHTS
        // ---------------------------------------------------------

        HideDeskHighlights();

        // ---------------------------------------------------------
        // DISABLE WORKSTATION INTERACTION
        // ---------------------------------------------------------

        if (workstationInteractor != null)
            workstationInteractor.enabled = false;

        // ---------------------------------------------------------
        // RESTORE FIRST-PERSON SYSTEMS
        // ---------------------------------------------------------

        SetFirstPersonSystems(true);

        // ---------------------------------------------------------
        // SHOW HANDS
        // ---------------------------------------------------------

        if (firstPersonHands != null)
            firstPersonHands.Show();

        DebugTrace.Log(
            "[CameraController] Returned to normal first-person."
        );

        WorkstationExited?.Invoke();
    }

    // =============================================================
    // ENTER INTERVIEW
    // =============================================================

    public void EnterInterview(Transform viewpoint)
    {
        if (CurrentMode != CameraMode.FirstPerson)
        {
            Debug.LogWarning(
                "[CameraController] Cannot enter interview. Current mode: "
                + CurrentMode
            );

            return;
        }

        if (viewpoint == null)
        {
            Debug.LogError(
                "[CameraController] Interview viewpoint is NULL!"
            );

            return;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "[CameraController] Main Camera is NULL!"
            );

            return;
        }

        DebugTrace.Log(
            "[CameraController] Entering interview."
        );

        CurrentMode = CameraMode.Interview;

        // ---------------------------------------------------------
        // STOP FIRST-PERSON SYSTEMS
        // ---------------------------------------------------------

        SetFirstPersonSystems(false);

        // ---------------------------------------------------------
        // DISABLE WORKSTATION INTERACTION
        // ---------------------------------------------------------

        if (workstationInteractor != null)
            workstationInteractor.enabled = false;

        // ---------------------------------------------------------
        // HIDE HANDS
        // ---------------------------------------------------------

        if (firstPersonHands != null)
            firstPersonHands.Hide();

        // ---------------------------------------------------------
        // STOP PREVIOUS TRANSITION
        // ---------------------------------------------------------

        StopActiveTransition();

        // ---------------------------------------------------------
        // MOVE CAMERA TO INTERVIEW VIEWPOINT
        // ---------------------------------------------------------

        activeTransition = StartCoroutine(
            MoveCameraToViewpoint(
                viewpoint,
                null
            )
        );
    }

    // =============================================================
    // EXIT INTERVIEW
    // =============================================================

    public void ExitInterview()
    {
        if (CurrentMode != CameraMode.Interview)
            return;

        DebugTrace.Log(
            "[CameraController] Exiting interview."
        );

        StopActiveTransition();

        CurrentMode = CameraMode.FirstPerson;

        SetFirstPersonSystems(true);

        if (firstPersonHands != null)
            firstPersonHands.Show();

        DebugTrace.Log(
            "[CameraController] Returned to normal first-person."
        );
    }

    // =============================================================
    // LOCK PLAYER CONTROLS
    // =============================================================

    public void LockPlayerControls()
    {
        // T05: a popup opened during a conversation keeps its lock after the
        // conversation releases (see AcquireDialogueControls).
        if (dialogueHoldsControls) controlsRequestedDuringDialogue = DeferredControls.Locked;
        if (lookJoystick != null) lookJoystick.Hide();
        if (firstPersonCamera != null)
            firstPersonCamera.enabled = false;

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerInteractor != null)
        {
            playerInteractor.ClearFocus();
            playerInteractor.enabled = false;
        }

        // ---------------------------------------------------------
        // HIDE VIRTUAL JOYSTICK
        // ---------------------------------------------------------

        if (virtualJoystick != null)
        {
            virtualJoystick.Hide();

            DebugTrace.Log(
                "[CameraController] Player controls locked. " +
                "Virtual joystick hidden."
            );
        }
    }

    // =============================================================
    // UNLOCK PLAYER CONTROLS
    // =============================================================

    public bool CanStartDialogue => isActiveAndEnabled && CurrentMode == CameraMode.FirstPerson &&
        activeTransition == null && profileViewerControls == null;

    // T03 read-only accessors for the conversation presentation. Ownership of
    // modes, controls and the camera stays here; nothing below changes state.
    public Camera MainCamera
    {
        get
        {
            if (mainCamera == null) mainCamera = Camera.main;
            return mainCamera;
        }
    }

    public Transform PlayerRoot => playerMovement != null ? playerMovement.transform : null;

    public FirstPersonHands Hands => firstPersonHands != null ? firstPersonHands : FirstPersonHands.Instance;

    /// <summary>The view the first-person controller will resume when it is re-enabled.</summary>
    public bool TryGetFirstPersonView(out Vector3 position, out Quaternion rotation)
    {
        FirstPersonCameraController controller = firstPersonCamera != null
            ? firstPersonCamera
            : MainCamera != null ? MainCamera.GetComponent<FirstPersonCameraController>() : null;
        if (controller != null && controller.TryGetViewPose(out position, out rotation)) return true;
        position = default;
        rotation = Quaternion.identity;
        return false;
    }

    /// <summary>Conversation-owned snapshot; does not alter the camera mode or viewpoint.</summary>
    public System.Action AcquireDialogueControls()
    {
        Behaviour[] controls = {
            firstPersonCamera,
            mainCamera != null ? mainCamera.GetComponent<FirstPersonCameraController>() : null,
            playerMovement, playerInteractor, workstationInteractor
        };
        bool[] enabledStates = new bool[controls.Length];
        for (int i = 0; i < controls.Length; i++)
            enabledStates[i] = controls[i] != null && controls[i].enabled;
        bool movementVisible = virtualJoystick != null && virtualJoystick.gameObject.activeSelf;
        bool lookVisible = lookJoystick != null && lookJoystick.gameObject.activeSelf;
        CameraMode mode = CurrentMode;
        CursorLockMode cursorLock = Cursor.lockState;
        bool cursorVisible = Cursor.visible;
        LockPlayerControls();
        foreach (Behaviour control in controls)
            if (control != null) control.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // T03: freeze the gameplay body for the whole conversation so the
        // restored first-person view matches where the player still stands.
        // Only constraints/velocity change; the root, collider and scale do not.
        Rigidbody body = playerMovement != null ? playerMovement.GetComponent<Rigidbody>() : null;
        RigidbodyConstraints bodyConstraints = body != null ? body.constraints : RigidbodyConstraints.None;
        if (body != null)
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.constraints = RigidbodyConstraints.FreezeAll;
        }

        // T05: popups (tutorial, results) may lock/unlock while the conversation
        // holds the controls; their last request is applied on release instead.
        int holdVersion = ++dialogueHoldVersion;
        dialogueHoldsControls = true;
        controlsRequestedDuringDialogue = DeferredControls.None;

        bool released = false;
        return () =>
        {
            if (released) return;
            released = true;
            DeferredControls requested = DeferredControls.None;
            if (this != null && holdVersion == dialogueHoldVersion)
            {
                requested = controlsRequestedDuringDialogue;
                dialogueHoldsControls = false;
                controlsRequestedDuringDialogue = DeferredControls.None;
            }
            // Body constraints belong to this conversation alone; restore them
            // even when another mode has since taken the camera.
            if (body != null) body.constraints = bodyConstraints;
            // A different mode/transition has taken ownership; its own exit
            // path decides when controls return, not this finished dialogue.
            if (this == null || !isActiveAndEnabled || CurrentMode != mode || activeTransition != null) return;
            Cursor.lockState = cursorLock;
            Cursor.visible = cursorVisible;
            // A popup opened mid-conversation is still up; it unlocks on close.
            if (requested == DeferredControls.Locked) return;
            for (int i = 0; i < controls.Length; i++)
                if (controls[i] != null) controls[i].enabled = enabledStates[i];
            if (virtualJoystick != null) virtualJoystick.SetVisible(movementVisible);
            if (lookJoystick != null) lookJoystick.SetVisible(lookVisible);
            // A popup that was open at the start closed mid-conversation.
            if (requested == DeferredControls.Unlocked && mode == CameraMode.FirstPerson)
                SetFirstPersonSystems(true);
            MobileInputGuard.SuppressWorldTapsThisFrame();
        };
    }

    public void UnlockPlayerControls()
    {
        if (CurrentMode != CameraMode.FirstPerson)
            return;

        // T05: re-enabling look/movement now would fight the conversation camera.
        if (dialogueHoldsControls)
        {
            controlsRequestedDuringDialogue = DeferredControls.Unlocked;
            return;
        }

        SetFirstPersonSystems(true);
    }

    // =============================================================
    // FIRST-PERSON SYSTEMS
    // =============================================================

    private void SetFirstPersonSystems(bool enabled)
    {
        if (lookJoystick != null) lookJoystick.SetVisible(enabled);
        if (firstPersonCamera != null)
            firstPersonCamera.enabled = enabled;

        if (playerMovement != null)
            playerMovement.enabled = enabled;

        if (playerInteractor != null)
        {
            if (!enabled)
                playerInteractor.ClearFocus();

            playerInteractor.enabled = enabled;
        }

        // ---------------------------------------------------------
        // VIRTUAL JOYSTICK
        // ---------------------------------------------------------

        if (virtualJoystick != null)
        {
            virtualJoystick.SetVisible(enabled);

            DebugTrace.Log(
                "[CameraController] Virtual joystick "
                + (enabled ? "shown." : "hidden.")
            );
        }

        // The exit/close tap is released this frame over now-hidden UI.
        if (enabled) MobileInputGuard.SuppressWorldTapsThisFrame();
    }

    // =============================================================
    // STOP ACTIVE TRANSITION
    // =============================================================

    private void StopActiveTransition()
    {
        if (activeTransition == null)
            return;

        StopCoroutine(activeTransition);
        activeTransition = null;
    }

    // =============================================================
    // DESK HIGHLIGHTS
    // =============================================================

    private void ShowDeskHighlights()
    {
        if (deskItemHighlights == null)
            return;

        foreach (DeskItemHighlight item in deskItemHighlights)
        {
            if (item != null)
                item.ShowHighlight();
        }
    }

    private void HideDeskHighlights()
    {
        if (deskItemHighlights == null)
            return;

        foreach (DeskItemHighlight item in deskItemHighlights)
        {
            if (item != null)
                item.HideHighlight();
        }
    }

    // =============================================================
    // ENTER CORKBOARD SIDE QUEST
    // =============================================================

    public void EnterCorkboardSideQuest(Transform viewpoint, System.Action onComplete = null)
    {
        DebugTrace.Log("[CameraController] EnterCorkboardSideQuest called.");

        if (viewpoint == null)
        {
            Debug.LogError("[CameraController] Corkboard viewpoint is NULL!");
            return;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("[CameraController] Main Camera could not be found!");
            return;
        }

        if (CurrentMode != CameraMode.FirstPerson)
        {
            Debug.LogWarning(
                "[CameraController] Current mode is " + CurrentMode +
                ". Resetting to FirstPerson before entering corkboard."
            );

            ForceResetToFirstPerson();
        }

        CurrentMode = CameraMode.CorkboardSideQuest;

        // Same as interview mode: stop normal player systems entirely.
        SetFirstPersonSystems(false);

        if (workstationInteractor != null)
            workstationInteractor.enabled = false;

        if (firstPersonHands != null)
            firstPersonHands.Hide();

        StopActiveTransition();

        DebugTrace.Log("[CameraController] Moving camera to corkboard viewpoint: " + viewpoint.name);

        activeTransition = StartCoroutine(
            MoveCameraToViewpoint(
                viewpoint,
                () =>
                {
                    DebugTrace.Log("[CameraController] Corkboard viewpoint reached.");
                    onComplete?.Invoke();
                }
            )
        );
    }

    // =============================================================
    // EXIT CORKBOARD SIDE QUEST
    // =============================================================

    public void ExitCorkboardSideQuest()
    {
        if (CurrentMode != CameraMode.CorkboardSideQuest)
            return;

        DebugTrace.Log("[CameraController] Exiting corkboard side quest.");

        StopActiveTransition();

        CurrentMode = CameraMode.FirstPerson;

        SetFirstPersonSystems(true);

        if (firstPersonHands != null)
            firstPersonHands.Show();

        DebugTrace.Log("[CameraController] Returned to normal first-person.");
    }
}
