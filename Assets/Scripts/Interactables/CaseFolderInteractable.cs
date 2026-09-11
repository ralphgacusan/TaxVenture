
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The physical Case Folder object sitting on the Desk.
///
/// Clicking it:
/// - Raises the CaseFolderOpened gameplay event
/// - Opens the CaseFolderUI
/// - Moves the game into ReviewDocumentsState
///
/// IMPORTANT:
/// The CaseFolderOpened event is raised EVERY TIME the folder
/// for the current case is opened.
///
/// This is intentional because the HUD Case Folder icon is
/// locked again whenever a case is completed and must unlock
/// again for the next case.
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class CaseFolderInteractable :
    MonoBehaviour,
    IInteractable
{
    [SerializeField]
    private CaseFolderUI caseFolderUI;


    [SerializeField]
    private StampUI stampUI;


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
        if (CameraController.Instance.CurrentMode ==
            CameraController.CameraMode.Workstation)
        {
            return;
        }

        highlight.Unhighlight();
    }


    public void OnInteract()
    {
        Debug.Log(
            "[CaseFolderInteractable] " +
            "Case Folder opened."
        );


        // =========================================================
        // UNLOCK HUD CASE FOLDER ICON
        // =========================================================

        // IMPORTANT:
        // Do NOT use a "first time only" bool here.
        //
        // Case 1 → event fires
        // Case 2 → event fires again
        // Case 3 → event fires again
        // etc.
        GameplayEvents.RaiseCaseFolderOpened();


        // =========================================================
        // OPEN CASE FOLDER
        // =========================================================

        if (caseFolderUI == null)
        {
            Debug.LogError(
                "[CaseFolderInteractable] " +
                "CaseFolderUI is NOT assigned."
            );

            return;
        }


        caseFolderUI.Show();


        // =========================================================
        // CHANGE GAMEPLAY STATE
        // =========================================================

        if (GameStateMachine.Instance == null)
        {
            Debug.LogError(
                "[CaseFolderInteractable] " +
                "GameStateMachine.Instance is NULL."
            );

            return;
        }


        GameStateMachine.Instance.ChangeState(
            new ReviewDocumentsState()
        );


        // =========================================================
        // TUTORIAL
        // =========================================================

        if (TutorialController.Instance != null)
        {
            TutorialController.Instance.ReportInteraction(
                "case_folder"
            );
        }
    }


    public string GetPromptText()
    {
        return "Click to open Case Folder";
    }
}

