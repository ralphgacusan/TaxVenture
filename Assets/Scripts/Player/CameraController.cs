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
/// There is NO third-person camera.
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

        // Enable normal first-person systems.
        SetFirstPersonSystems(true);

        if (workstationInteractor != null)
            workstationInteractor.enabled = false;

        if (workstationUI != null)
            workstationUI.Hide();

        HideDeskHighlights();

        if (firstPersonHands != null)
            firstPersonHands.Show();

        Debug.Log(
            "[CameraController] Initialized in FirstPerson mode."
        );
    }

    // =============================================================
    // ENTER WORKSTATION / DESK VIEW
    // =============================================================

    public void EnterFirstPerson(
        Transform viewpoint,
        string exitLabel = "Exit Desk",
        bool showHands = true)
    {
        Debug.Log(
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

        Debug.Log(
            "[CameraController] DeskViewpoint WORLD POSITION: "
            + viewpoint.position
        );

        Debug.Log(
            "[CameraController] DeskViewpoint WORLD ROTATION: "
            + viewpoint.rotation.eulerAngles
        );

        Debug.Log(
            "[CameraController] DeskViewpoint LOCAL POSITION: "
            + viewpoint.localPosition
        );

        Debug.Log(
            "[CameraController] DeskViewpoint LOCAL ROTATION: "
            + viewpoint.localRotation.eulerAngles
        );

        // ---------------------------------------------------------
        // CHANGE MODE
        // ---------------------------------------------------------

        CurrentMode = CameraMode.Workstation;

        // ---------------------------------------------------------
        // STOP FIRST-PERSON CAMERA
        // ---------------------------------------------------------

        if (firstPersonCamera != null)
        {
            firstPersonCamera.enabled = false;

            Debug.Log(
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

            Debug.Log(
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

        // ---------------------------------------------------------
        // ENABLE WORKSTATION INTERACTION
        // ---------------------------------------------------------

        if (workstationInteractor != null)
            workstationInteractor.enabled = true;

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

        Debug.Log(
            "[CameraController] Moving camera to: "
            + viewpoint.name
        );

        activeTransition = StartCoroutine(
            MoveCameraToViewpoint(
                viewpoint,
                () =>
                {
                    Debug.Log(
                        "[CameraController] Workstation view reached."
                    );

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
        Debug.Log(
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

        Debug.Log(
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

            Debug.Log(
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

        Debug.Log(
            "[CameraController] Camera transition COMPLETE\n" +
            "Final Position: "
            + mainCamera.transform.position
            + "\nFinal Rotation: "
            + mainCamera.transform.rotation.eulerAngles
        );

        activeTransition = null;

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

        Debug.Log(
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
            Debug.Log(
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

        Debug.Log(
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

        Debug.Log(
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

        Debug.Log(
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

        Debug.Log(
            "[CameraController] Exiting interview."
        );

        StopActiveTransition();

        CurrentMode = CameraMode.FirstPerson;

        SetFirstPersonSystems(true);

        if (firstPersonHands != null)
            firstPersonHands.Show();

        Debug.Log(
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

            Debug.Log(
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

            Debug.Log(
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
    // TEMPORARY DEBUG
    // =============================================================

    private void OnGUI()
    {
        GUI.Label(
            new Rect(10, 10, 300, 30),
            $"CurrentMode: {CurrentMode}"
        );
    }

    // =============================================================
    // ENTER CORKBOARD SIDE QUEST
    // =============================================================

    public void EnterCorkboardSideQuest(Transform viewpoint, System.Action onComplete = null)
    {
        Debug.Log("[CameraController] EnterCorkboardSideQuest called.");

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

        Debug.Log("[CameraController] Moving camera to corkboard viewpoint: " + viewpoint.name);

        activeTransition = StartCoroutine(
            MoveCameraToViewpoint(
                viewpoint,
                () =>
                {
                    Debug.Log("[CameraController] Corkboard viewpoint reached.");
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

        Debug.Log("[CameraController] Exiting corkboard side quest.");

        StopActiveTransition();

        CurrentMode = CameraMode.FirstPerson;

        SetFirstPersonSystems(true);

        if (firstPersonHands != null)
            firstPersonHands.Show();

        Debug.Log("[CameraController] Returned to normal first-person.");
    }
}
