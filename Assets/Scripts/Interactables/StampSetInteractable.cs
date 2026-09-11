using UnityEngine;

/// <summary>
/// Clicking the physical Stamp Set on the desk opens the Case Folder
/// for stamping and triggers the existing 3D stamp transition.
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class StampSetInteractable : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private StampCaseFolderUI caseFolderUI;
    [SerializeField] private StampTransitionController stampTransitionController;

    private HighlightEffect highlight;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
    }

    public void OnFocus()
    {
        if (highlight != null)
        {
            highlight.Highlight();
        }
    }

    public void OnUnfocus()
    {
        if (CameraController.Instance != null &&
            CameraController.Instance.CurrentMode ==
            CameraController.CameraMode.Workstation)
        {
            return;
        }

        if (highlight != null)
        {
            highlight.Unhighlight();
        }
    }

    public void OnInteract()
    {
        Debug.Log("======================================");
        Debug.Log("[StampSetInteractable] STAMP SET CLICKED");

        // =====================================================
        // 1. OPEN STAMP CASE FOLDER
        // =====================================================

        if (caseFolderUI != null)
        {
            Debug.Log(
                "[StampSetInteractable] Calling " +
                "caseFolderUI.ShowForStamping()..."
            );

            caseFolderUI.ShowForStamping();

            Debug.Log(
                "[StampSetInteractable] ShowForStamping() returned."
            );
        }
        else
        {
            Debug.LogError(
                "[StampSetInteractable] " +
                "caseFolderUI IS NULL!"
            );
        }

        // =====================================================
        // 2. CHANGE FSM STATE
        // =====================================================

        if (GameStateMachine.Instance != null)
        {
            if (GameStateMachine.Instance.CurrentState is AnalyzeEvidenceState
                || GameStateMachine.Instance.CurrentState is ComputeTaxesState
                || GameStateMachine.Instance.CurrentState is ResearchTaxState
                || GameStateMachine.Instance.CurrentState is InterviewClientState)
            {
                Debug.Log(
                    "[StampSetInteractable] Changing FSM to " +
                    "StampAssessmentState."
                );

                GameStateMachine.Instance.ChangeState(
                    new StampAssessmentState()
                );
            }
        }

        // =====================================================
        // 3. MOVE 3D STAMPS INTO WORKSPACE
        // =====================================================

        if (stampTransitionController != null)
        {
            Debug.Log(
                "[StampSetInteractable] Starting stamp transition..."
            );

            stampTransitionController.MoveStampsToWorkspace();
        }
        else
        {
            Debug.LogError(
                "[StampSetInteractable] " +
                "stampTransitionController IS NULL!"
            );
        }

        Debug.Log("======================================");
    }

    public string GetPromptText()
    {
        return "Click to stamp the Case Folder";
    }
}