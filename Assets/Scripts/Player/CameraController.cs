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
        Interview
    }

    public CameraMode CurrentMode { get; private set; }
        = CameraMode.FirstPerson;

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
        // ---------------------------------------------------------

        if (virtualJoystick != null)
        {
            virtualJoystick.Hide();

            Debug.Log(
                "[CameraController] Virtual joystick hidden."
            );
        }

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

    public void UnlockPlayerControls()
    {
        if (CurrentMode != CameraMode.FirstPerson)
            return;

        SetFirstPersonSystems(true);
    }

    // =============================================================
    // FIRST-PERSON SYSTEMS
    // =============================================================

    private void SetFirstPersonSystems(bool enabled)
    {
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
}