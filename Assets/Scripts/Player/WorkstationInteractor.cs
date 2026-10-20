using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PURPOSE:
/// Cursor-based equivalent of Interactor.cs, active ONLY while the player is
/// in first-person workstation mode. Regular Interactor raycasts from screen
/// center (correct for locked-cursor exploration); this script raycasts from
/// the actual mouse/touch position (correct for free-cursor workstation
/// interaction, e.g. clicking the Case Folder sitting on the Desk).
///
/// CameraController is responsible for enabling this exactly when
/// playerInteractor is disabled, and vice versa — the two should never both
/// be active or both be inactive at the same time.
/// </summary>
public class WorkstationInteractor : MonoBehaviour
{
    [SerializeField] private float interactRange = 5f;
    [SerializeField] private LayerMask interactableLayer;

    private IInteractable currentFocus;
    private HighlightEffect focusOutline;

    private void OnDisable()
    {
        // Safety net: if this gets disabled while mid-focus (e.g. CameraController
        // toggles it off), make sure the highlighted object gets cleaned up too.
        ClearFocus();
    }

    private void Update()
    {
        HandleHoverDetection();
        HandleClick();
    }

    private void HandleHoverDetection()
    {
        if (Camera.main == null || DialogueUI.IsAnyDialogueActive)
        {
            ClearFocus();
            return;
        }

        IInteractable hitInteractable = null;

        bool cursorOverUI = MobileInputGuard.BlocksWorldInput();

        if (!cursorOverUI)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
            {
                hitInteractable = hit.collider.GetComponentInParent<IInteractable>();

            }
        }

        if (hitInteractable != currentFocus)
        {
            if (focusOutline != null) focusOutline.SetFocused(false);
            if (currentFocus is Component previous && previous != null) currentFocus.OnUnfocus();
            currentFocus = hitInteractable;
            currentFocus?.OnFocus();
            focusOutline = HighlightEffect.For(currentFocus);
            if (focusOutline != null) focusOutline.SetFocused(true);
        }
    }

    private void HandleClick()
    {
        bool cursorOverUI = MobileInputGuard.BlocksWorldInput();
        if (cursorOverUI || DialogueUI.IsAnyDialogueActive) return; // don't double-trigger world objects through UI clicks

        if (MobileInputGuard.TryGetWorldTap(out Vector2 position) && Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(position);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    AudioManager.Instance?.PlayInteractionSFX(interactable as Component);
                    interactable.OnInteract();
                }
            }
        }
    }

    public void ClearFocus()
    {
        if (focusOutline != null) focusOutline.SetFocused(false);
        focusOutline = null;
        if (currentFocus is Component previous && previous != null) currentFocus.OnUnfocus();
        currentFocus = null;
    }
}
