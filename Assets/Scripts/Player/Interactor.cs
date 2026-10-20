using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PURPOSE:
/// Attached to the Player. Casts a ray forward from the camera each frame to
/// detect nearby IInteractable objects, manages focus/unfocus transitions,
/// and triggers OnInteract() when the player presses the interact input.
///
/// RESPONSIBILITIES:
/// - Raycast from camera center outward, up to interactRange
/// - Track the "currently focused" interactable and call OnFocus/OnUnfocus
///   correctly as focus changes
/// - Listen for interact input (Left Click for now, swappable later) and
///   call OnInteract() on the focused object
/// - Notify the UI (via InteractionPromptUI) what prompt text to show
///
/// DOES NOT:
/// - Know anything about what a "Desk" or "Computer" specifically does —
///   that logic lives inside each object's own script (see IInteractable.cs)
///
/// CONNECTS WITH:
/// - IInteractable: anything implementing this can be detected
/// - InteractionPromptUI: told when to show/hide/update prompt text
/// - Camera.main: raycast origin/direction comes from the active camera,
///   so this will keep working correctly once we add first-person camera
///   switching in Milestone 3.
///
/// INPUT NOTE:
/// Interact input is currently "Left Click" (Input.GetMouseButtonDown(0)).
/// This is read from a single method (TryGetInteractInput) so that later,
/// swapping to a mobile touch button only requires changing that one method.
/// </summary>
public class Interactor : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Tooltip("Maximum distance the player can interact with an object from.")]
    [SerializeField] private float interactRange = 3f;

    [Tooltip("Layer mask for interactable objects. Set this in the Inspector to only hit the 'Interactable' layer for performance and to avoid hitting walls/floor.")]
    [SerializeField] private LayerMask interactableLayer;

    [Header("UI Reference")]
    [Tooltip("Reference to the on-screen prompt UI. Assigned in Inspector.")]
    [SerializeField] private InteractionPromptUI promptUI;
    [SerializeField] private bool enableDebugLogs = false;

    private IInteractable currentFocus;
    private HighlightEffect focusOutline;

    private void OnDisable() => ClearFocus();

    private void Update()
    {
        if (CameraController.Instance != null &&
            CameraController.Instance.CurrentMode != CameraController.CameraMode.FirstPerson)
        {
            ClearFocus();
            return;
        }

        HandleRaycastDetection();
        HandleInteractInput();
    }

    /// <summary>
    /// Casts a ray from the center of the screen (camera forward) and checks
    /// whether it hits an IInteractable within range. Manages focus/unfocus
    /// transitions when the target changes.
    /// </summary>
    private void HandleRaycastDetection()
    {
        // Looking at something keeps its prompt and outline while the joystick
        // is held or the pointer rests on the HUD; only the interact tap is
        // blocked then (HandleInteractInput checks BlocksWorldInput).
        if (Camera.main == null || DialogueUI.IsAnyDialogueActive)
        {
            ClearFocus();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width / 2f, Screen.height / 2f)
        );

        IInteractable hitInteractable = null;

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            // Debugging: log the name of the object hit by the raycast
            // DebugTrace.Log("Hit: " + hit.collider.name);

            hitInteractable = hit.collider.GetComponentInParent<IInteractable>();

            if (hitInteractable != null)
            {
                // Debugging: Log that we found an interactable object
                // DebugTrace.Log("Interactable Found!");
            }
        }

        // If the object we're looking at has changed, handle focus transitions.
        if (hitInteractable != currentFocus)
        {
            if (focusOutline != null) focusOutline.SetFocused(false);
            if (currentFocus is Component previous && previous != null) currentFocus.OnUnfocus();
            currentFocus = hitInteractable;
            currentFocus?.OnFocus();
            focusOutline = HighlightEffect.For(currentFocus);
            if (focusOutline != null) focusOutline.SetFocused(true);

            if (currentFocus != null && promptUI != null)
            {
                string prompt = currentFocus.GetPromptText();
                if (string.IsNullOrEmpty(prompt))
                {
                    promptUI.HidePrompt(); // NEW: empty prompt = no UI shown at all, still highlights via HighlightEffect
                }
                else
                {
                    promptUI.ShowPrompt(prompt);
                }
            }
            else
            {
                promptUI?.HidePrompt();
            }
        }
    }

    /// <summary>
    /// Checks for interact input and forwards it to the currently focused object.
    /// Isolated into its own method so swapping input methods later (mobile touch
    /// button, controller button, etc.) only requires changing this one place.
    /// </summary>

    private void HandleInteractInput()
    {
        if (currentFocus == null) return;
        if (MobileInputGuard.BlocksWorldInput()) return;
        if (TryGetInteractInput())
        {
            AudioManager.Instance?.PlayInteractionSFX(currentFocus as Component);
            currentFocus.OnInteract();
        }
    }

    /// <summary>
    /// Temporary input check (mouse left click / editor testing).
    /// Replace this method's contents when switching to mobile touch input —
    /// no other part of Interactor.cs needs to change.
    /// </summary>

    private bool TryGetInteractInput()
    {
        return MobileInputGuard.TryGetWorldTap(out _);
    }

    // Optional: visualize the interact ray in the Scene view for debugging.
    private void OnDrawGizmos()
    {
        if (Camera.main == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * interactRange);
    }

    public void ClearFocus()
    {
        if (focusOutline != null) focusOutline.SetFocused(false);
        focusOutline = null;
        if (currentFocus is Component previous && previous != null) currentFocus.OnUnfocus();
        currentFocus = null;

        promptUI?.HidePrompt();
    }
}
