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
        if (Camera.main == null) return;

        IInteractable hitInteractable = null;

        bool cursorOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (!cursorOverUI)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
            {
                hitInteractable = hit.collider.GetComponent<IInteractable>();

            }
        }

        if (hitInteractable != currentFocus)
        {
            currentFocus?.OnUnfocus();
            currentFocus = hitInteractable;
            currentFocus?.OnFocus();
        }
    }

    private void HandleClick()
    {
        if (currentFocus == null) return;

        bool cursorOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (cursorOverUI) return; // don't double-trigger world objects through UI clicks

        if (Input.GetMouseButtonDown(0))
        {
            currentFocus.OnInteract();
        }
    }

    public void ClearFocus()
    {
        currentFocus?.OnUnfocus();
        currentFocus = null;
    }
}