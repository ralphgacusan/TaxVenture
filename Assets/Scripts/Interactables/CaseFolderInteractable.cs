using UnityEngine;

/// <summary>
/// PURPOSE:
/// The physical Case Folder object sitting on the Desk (visible only once
/// the player is in first-person desk view). Clicking it opens CaseFolderUI.
///
/// PER DESIGN DOC:
/// "The Case Folder is visible on the desk... highlighted... The player
/// clicks the Case Folder... front page displays [Case Number, Tax Year,
/// Date Received, Assigned Consultant]... The player clicks the Case Folder
/// again... folder opens" — for this prototype we simplify the two-click
/// cover/open sequence into a single click that opens directly to Page 1,
/// since the cover-only view adds no gameplay value in greybox form.
///
/// CONNECTS WITH:
/// - HighlightEffect (same GameObject)
/// - CaseFolderUI: calls Show() on interact
/// - Should only be interactable while the player is already in first-person
///   desk view — enforced by only being active (SetActive) once at the desk,
///   see Editor Setup section.
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class CaseFolderInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private CaseFolderUI caseFolderUI;

    private HighlightEffect highlight;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
    }

    public void OnFocus() => highlight.Highlight();
    public void OnUnfocus()
    {
        if (CameraController.Instance.CurrentMode ==
            CameraController.CameraMode.Workstation)
            return;

        highlight.Unhighlight();
    }
    public void OnInteract()
    {
        caseFolderUI.Show();
        GameStateMachine.Instance.ChangeState(new ReviewDocumentsState());

    }

    public string GetPromptText() => "Click to open Case Folder";
}