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

    /// <summary>True once the book was opened in this run.</summary>
    public bool HasBeenOpenedOnce => hasBeenOpenedOnce;

    /// <summary>Resume (T05): the book was opened before; the first-open event is not raised again.</summary>
    public void RestoreOpenedOnce() => hasBeenOpenedOnce = true;

    public void OnFocus() => highlight.Highlight();
    public void OnUnfocus()
    {
        highlight.Unhighlight();
    }
    public void OnInteract()
    {
        // Same opening cue for the legacy interactable and drag-open paths.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEffect("sfx_book_open_01");
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
