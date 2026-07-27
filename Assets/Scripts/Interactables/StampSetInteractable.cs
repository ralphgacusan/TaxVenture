using UnityEngine;

/// <summary>
/// PURPOSE:
/// Replaces the old physical Stamp cylinders. Sits on the Desk (DeskItem
/// layer). Clicking it opens the Case Folder UI forced to Page 1, with the
/// stamp panel visible alongside it, per updated design: stamps live inside
/// the folder view, not as separate desk objects.
///
/// CONNECTS WITH:
/// - HighlightEffect (same GameObject)
/// - CaseFolderUI: opened via ShowForStamping() (new method, see below)
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class StampSetInteractable : MonoBehaviour, IInteractable
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
        caseFolderUI.ShowForStamping();

        // NEW: entering the stamping UI is now what advances into StampAssessmentState.
        if (GameStateMachine.Instance.CurrentState is AnalyzeEvidenceState
            || GameStateMachine.Instance.CurrentState is ComputeTaxesState
            || GameStateMachine.Instance.CurrentState is ResearchTaxState
            || GameStateMachine.Instance.CurrentState is InterviewClientState)
        {
            GameStateMachine.Instance.ChangeState(new StampAssessmentState());
        }
    }

    public string GetPromptText() => "Click to stamp the Case Folder";
}