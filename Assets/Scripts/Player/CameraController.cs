using UnityEngine;
using System.Collections;

/// <summary>
/// PURPOSE:
/// Central authority for switching between Third-Person (exploration) and
/// First-Person (workstation interaction) camera modes, as required by the
/// design document ("The camera transitions from third-person view to
/// first-person view" for every desk/computer/folder/book/corkboard/cabinet
/// interaction).
///
/// RESPONSIBILITIES:
/// - Track current camera mode (ThirdPerson / FirstPerson)
/// - On EnterFirstPerson(viewpoint): disable third-person follow, disable
///   player movement + interactor, smoothly move camera to the given
///   viewpoint transform, show first-person hands, show the Close/Workstation UI
/// - On ExitFirstPerson(): reverse all of the above
///
/// DOES NOT:
/// - Know anything about WHAT is being interacted with (desk vs computer vs
///   corkboard). It only receives a viewpoint Transform and a callback-free
///   request to switch modes. This keeps it reusable for every workstation.
///
/// CONNECTS WITH:
/// - ThirdPersonCameraFollow: disabled while in first-person
/// - PlayerMovement / Interactor: disabled while in first-person (player is
///   "seated"/focused and shouldn't be able to walk around)
/// - FirstPersonHands: toggled visible/invisible
/// - WorkstationUI: shown/hidden (provides the Close button)
/// - Any *Interactable script (DeskInteractable, ComputerInteractable, etc.)
///   calls CameraController.Instance.EnterFirstPerson(viewpoint) from its
///   OnInteract() method.
///
/// PATTERN NOTE:
/// This uses a simple static Instance reference (not a full singleton with
/// DontDestroyOnLoad) since this is scene-scoped — one CameraController per
/// gameplay scene. This is intentionally lightweight; if the project later
/// needs persistence across scenes, this can be upgraded without changing
/// how other scripts call it.
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    public enum CameraMode
    {
        ThirdPerson,
        Workstation,
        Interview
    }
    public CameraMode CurrentMode { get; private set; } = CameraMode.ThirdPerson;

    [Header("References")]
    [Tooltip("The scene's single Camera transform (Main Camera).")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("Third-person follow script — disabled while in first-person mode.")]
    [SerializeField] private ThirdPersonCameraFollow thirdPersonFollow;

    [Tooltip("Player movement script — disabled while interacting at a workstation.")]
    [SerializeField] private PlayerMovement playerMovement;

    [Tooltip("Player interactor script — disabled while interacting at a workstation (prevents re-triggering interactions mid-transition).")]
    [SerializeField] private Interactor playerInteractor;

    [Tooltip("Placeholder hands shown only in first-person view.")]
    [SerializeField] private FirstPersonHands firstPersonHands;

    [Tooltip("UI panel with the Close button, shown while in first-person mode.")]
    [SerializeField] private WorkstationUI workstationUI;

    [Header("Transition Settings")]
    [Tooltip("Time in seconds for the camera to move/rotate into position.")]
    [SerializeField] private float transitionDuration = 0.5f;

    [Tooltip("Cursor-based interactor used only while at a workstation (e.g. clicking the folder on the desk).")]
    [SerializeField] private WorkstationInteractor workstationInteractor;

    [Header("Desk Items")]
    [SerializeField] private DeskItemHighlight[] deskItemHighlights;

    private Coroutine activeTransition;
    private Transform currentViewpoint;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Called by an interactable (e.g. DeskInteractable) to switch into
    /// first-person mode, focused on the given viewpoint transform.
    /// </summary>
    public void EnterFirstPerson(Transform viewpoint, bool showHands = true)
    {
        if (CurrentMode == CameraMode.Workstation) return;
        currentViewpoint = viewpoint;
        CurrentMode = CameraMode.Workstation;

        thirdPersonFollow.enabled = false;
        playerMovement.enabled = false;

        playerInteractor.ClearFocus();
        playerInteractor.enabled = false;
        workstationInteractor.enabled = true;

        // Unlock cursor so the player can click UI (Close button, folder pages, etc.)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(TransitionCamera(viewpoint.position, viewpoint.rotation, onComplete: () =>
        {

            // if (showHands)
            //     firstPersonHands.Show();
            // else
            //     firstPersonHands.Hide();
            workstationUI.Show();
            foreach (DeskItemHighlight item in deskItemHighlights)
            {
                item.ShowHighlight();
            }
        }));
    }

    /// <summary>
    /// Called by the WorkstationUI's Close button to return to third-person
    /// exploration mode.
    /// </summary>
    public void ExitFirstPerson()
    {
        if (CurrentMode != CameraMode.Workstation)
            return;
        CurrentMode = CameraMode.ThirdPerson;

        workstationUI.Hide();

        foreach (DeskItemHighlight item in deskItemHighlights)
        {
            item.HideHighlight();
        }

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(TransitionCamera(cameraTransform.position, cameraTransform.rotation, onComplete: () =>
        {
            thirdPersonFollow.enabled = true;
            playerMovement.enabled = true;
            playerInteractor.enabled = true;
            workstationInteractor.enabled = false;   // <-- ADD THIS

            // Re-lock cursor for exploration/mouse-look.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }));
    }

    /// <summary>
    /// Smoothly moves/rotates the camera transform to a target position/rotation
    /// over transitionDuration seconds. Used for both entering and exiting
    /// first-person mode so the transition always feels consistent.
    /// </summary>
    private IEnumerator TransitionCamera(Vector3 targetPos, Quaternion targetRot, System.Action onComplete)
    {
        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            cameraTransform.position = Vector3.Lerp(startPos, targetPos, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        cameraTransform.position = targetPos;
        cameraTransform.rotation = targetRot;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Switches into Interview mode. Similar to first-person mode, but intended
    /// for NPC conversations rather than workstation interactions. The player
    /// cannot move or look around, the camera transitions to a predefined
    /// interview viewpoint, and the cursor is unlocked for dialogue UI.
    /// </summary>
    public void EnterInterview(Transform viewpoint)
    {
        if (CurrentMode != CameraMode.ThirdPerson)
            return;

        CurrentMode = CameraMode.Interview;

        thirdPersonFollow.enabled = false;
        playerMovement.enabled = false;

        playerInteractor.ClearFocus();
        playerInteractor.enabled = false;

        workstationInteractor.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (activeTransition != null)
            StopCoroutine(activeTransition);

        activeTransition = StartCoroutine(
            TransitionCamera(
                viewpoint.position,
                viewpoint.rotation,
                null
            ));
    }

    public void ExitInterview()
    {
        if (CurrentMode != CameraMode.Interview)
            return;

        CurrentMode = CameraMode.ThirdPerson;

        if (activeTransition != null)
            StopCoroutine(activeTransition);

        activeTransition = StartCoroutine(
            TransitionCamera(
                cameraTransform.position,
                cameraTransform.rotation,
                () =>
                {
                    thirdPersonFollow.enabled = true;
                    playerMovement.enabled = true;
                    playerInteractor.enabled = true;

                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }));
    }


    /// <summary>
    /// Freezes player movement and mouse-look WITHOUT any camera transition —
    /// used for NPC conversations (Client Interview, Auditor) where the camera
    /// stays exactly where it is, but the player shouldn't be able to walk
    /// away or look around mid-conversation. Distinct from EnterFirstPerson,
    /// which also moves the camera to a specific workstation viewpoint.
    /// </summary>
    public void LockPlayerControls()
    {
        if (CurrentMode != CameraMode.ThirdPerson) return; // NPC conversations only happen during exploration

        thirdPersonFollow.enabled = false;
        playerMovement.enabled = false;
        playerInteractor.ClearFocus();
        playerInteractor.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Reverses LockPlayerControls() — restores movement, mouse-look, and
    /// re-locks the cursor for exploration.
    /// </summary>
    public void UnlockPlayerControls()
    {
        if (CurrentMode != CameraMode.ThirdPerson) return;

        thirdPersonFollow.enabled = true;
        playerMovement.enabled = true;
        playerInteractor.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

}