using UnityEngine;

/// <summary>
/// PURPOSE:

///
/// PER DESIGN DOC:

///
/// CONNECTS WITH:

/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class TaxCodeBookInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private TaxCodeBookUI taxCodeBookUI;
    private HighlightEffect highlight;

    private bool hasBeenOpenedOnce = false;

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
        // Play Paper 2 SFX every time the 3D Tax Code Book is interacted with.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPaperSFX();
        }

        if (!hasBeenOpenedOnce)
        {
            hasBeenOpenedOnce = true;
            GameplayEvents.RaiseTaxCodeBookFirstOpened();

            if (TutorialController.Instance != null)
                TutorialController.Instance.ReportInteraction("tax_code_book");
        }

        if (GameStateMachine.Instance.CurrentState is InterviewClientState
            || GameStateMachine.Instance.CurrentState is ReviewDocumentsState)
        {
            GameStateMachine.Instance.ChangeState(new ResearchTaxState());
        }

        taxCodeBookUI.Show();
    }
    public string GetPromptText() => "Click to open Tax Code Book";
}