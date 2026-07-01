using UnityEngine;

/// <summary>
/// PURPOSE:
/// Desk interactable. On interact, triggers the CameraController to switch
/// into first-person mode focused on this desk's viewpoint. Additionally,
/// if the game is currently in ReceiveCaseState, interacting with the Desk
/// advances the FSM to ReviewDocumentsState — matching the design doc's
/// "Receive Case" -> "Review Documents" flow.
///
/// FUTURE (Milestone 5+):
/// This will also trigger opening the Case Folder UI. The state-transition
/// logic here will likely move to be triggered by "Case Folder closed after
/// first read" rather than "Desk clicked," once the folder UI exists. For
/// now, Desk-click is the testable trigger for FSM progression.
///
/// CONNECTS WITH:
/// - HighlightEffect (same GameObject) for visual feedback
/// - CameraController for the camera transition
/// - GameStateMachine for FSM progression
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

        // Only advance the FSM if we're still in the Receive Case phase.
        // This guard prevents the Desk from incorrectly forcing a state
        // change during later phases that also involve sitting at the desk.
        if (GameStateMachine.Instance.CurrentState is ReceiveCaseState)
        {
            GameStateMachine.Instance.ChangeState(new ReviewDocumentsState());
        }
    }

    public string GetPromptText()
    {
        return "Click to sit at Desk";
    }
}