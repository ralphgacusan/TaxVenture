using UnityEngine;

/// <summary>
/// PURPOSE:
/// Desk interactable. On interact, switches into first-person mode focused
/// on this desk's viewpoint. FSM progression (ReceiveCase -> ReviewDocuments)
/// now happens when the Case Folder is opened (see CaseFolderUI.Show()),
/// not simply from sitting at the desk — this is more accurate to the
/// design doc, since sitting down alone isn't "reviewing documents."
///
/// CONNECTS WITH:
/// - HighlightEffect (same GameObject)
/// - CameraController for the camera transition
/// - deskViewpoint: child Transform marking where the camera should sit
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class DeskInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform deskViewpoint;

    private HighlightEffect highlight;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
    }

    public void OnFocus() => highlight.Highlight();
    public void OnUnfocus() => highlight.Unhighlight();

    public void OnInteract()
    {
        CameraController.Instance.EnterFirstPerson(deskViewpoint);
    }

    public string GetPromptText() => "Click to sit at Desk";
}