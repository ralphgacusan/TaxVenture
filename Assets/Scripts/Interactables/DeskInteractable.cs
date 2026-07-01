using UnityEngine;

/// <summary>
/// PURPOSE:
/// Desk interactable. On interact, triggers the CameraController to switch
/// into first-person mode focused on this desk's viewpoint, matching the
/// design doc's "Receive Case" flow: "The camera transitions from third-person
/// view to first-person view. The player is now seated at the desk."
///
/// FUTURE (Milestone 5+):
/// This will also trigger opening the Case Folder UI once at the desk. For
/// now, entering first-person mode is the complete, testable behavior.
///
/// CONNECTS WITH:
/// - HighlightEffect (same GameObject) for visual feedback
/// - CameraController for the camera transition
/// - deskViewpoint: child Transform marking where the camera should sit
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class DeskInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("Child transform marking the first-person camera position/rotation for this desk.")]
    [SerializeField] private Transform deskViewpoint;

    private HighlightEffect highlight;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
    }

    public void OnFocus()
    {
        highlight.Highlight();
    }

    public void OnUnfocus()
    {
        highlight.Unhighlight();
    }

    public void OnInteract()
    {
        CameraController.Instance.EnterFirstPerson(deskViewpoint);
    }

    public string GetPromptText()
    {
        return "Click to sit at Desk";
    }
}