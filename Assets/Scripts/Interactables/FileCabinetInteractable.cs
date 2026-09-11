
using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// The filing drawer/cabinet in the File Storage Room. Clicking it while
/// carrying the Case Folder archives the current case.
///
/// FLOW:
/// Auditor
///     ↓
/// Present Findings to Client
///     ↓
/// Archive Case
///     ↓
/// Filing Cabinet
///     ↓
/// Archive Confirmation
///     ↓
/// CaseCompleteState
///     ↓
/// CaseProgressionManager
///     ↓
/// Next Case / Level Complete
///
/// IMPORTANT:
/// The archive status is stored in CaseData.isArchived, which is
/// per-case data. This means the same cabinet can be reused when
/// CaseProgressionManager loads Case 2, Case 3, etc.
///
/// RESPONSIBILITIES:
/// - Verify the player is currently in ArchiveCaseState
/// - Verify the current CaseData has the Case Folder
/// - Prevent archiving a case that is already archived
/// - Mark the current case as archived
/// - Show archive confirmation
/// - Continue to CaseCompleteState after confirmation
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class FilingCabinetInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ArchiveConfirmationPopupUI archiveConfirmationPopupUI;
    [SerializeField] private TextMeshProUGUI quickWarningText;
    [SerializeField] private float warningDuration = 2.5f;

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
        // ---------------------------------------------------------
        // 1. Make sure there is a current case.
        // ---------------------------------------------------------

        if (CaseManager.Instance == null ||
            CaseManager.Instance.CurrentCase == null)
        {
            ShowWarning("No active case.");
            return;
        }

        CaseData data = CaseManager.Instance.CurrentCase;

        // ---------------------------------------------------------
        // 2. Prevent interacting with the cabinet outside the
        //    Archive Case phase.
        // ---------------------------------------------------------

        if (!(GameStateMachine.Instance.CurrentState is ArchiveCaseState))
        {
            ShowWarning("There's nothing to archive right now.");
            return;
        }

        // ---------------------------------------------------------
        // 3. Prevent archiving an already archived case.
        // ---------------------------------------------------------

        if (data.isArchived)
        {
            ShowWarning("This case has already been archived.");
            return;
        }

        // ---------------------------------------------------------
        // 4. Verify the player has the Case Folder.
        // ---------------------------------------------------------

        if (!data.isCarryingCaseFolder)
        {
            ShowWarning("You don't have the Case Folder with you.");
            return;
        }

        // ---------------------------------------------------------
        // 5. Remove the visual carried document.
        // ---------------------------------------------------------

        if (FirstPersonHands.Instance != null)
        {
            FirstPersonHands.Instance.HideCarriedDocument();
        }

        ArchiveCase(data);
    }

    private void ArchiveCase(CaseData data)
    {
        // ---------------------------------------------------------
        // Mark the current case as archived.
        // ---------------------------------------------------------

        data.isArchived = true;
        data.isCarryingCaseFolder = false;

        Debug.Log(
            $"[FilingCabinetInteractable] Case archived: {data.caseNumber}"
        );

        // ---------------------------------------------------------
        // Show confirmation popup.
        // ---------------------------------------------------------

        archiveConfirmationPopupUI.Show(
            OnArchiveConfirmed
        );

        if (TutorialController.Instance != null)
        {
            TutorialController.Instance.ReportInteraction(
                "case_archived"
            );
        }
    }

    private void OnArchiveConfirmed()
    {
        Debug.Log(
            "[FilingCabinetInteractable] Archive confirmation completed."
        );

        // ---------------------------------------------------------
        // Archive is the final action for the current case.
        //
        // CaseCompleteState will now hand control to
        // CaseProgressionManager.
        // ---------------------------------------------------------

        if (GameStateMachine.Instance == null)
        {
            Debug.LogError(
                "[FilingCabinetInteractable] GameStateMachine.Instance is NULL."
            );

            return;
        }

        if (!(GameStateMachine.Instance.CurrentState is ArchiveCaseState))
        {
            Debug.LogWarning(
                "[FilingCabinetInteractable] Current state is no longer " +
                "ArchiveCaseState. Case completion transition skipped."
            );

            return;
        }

        Debug.Log(
            "[FilingCabinetInteractable] Archive complete → CaseCompleteState."
        );

        GameStateMachine.Instance.ChangeState(
            new CaseCompleteState()
        );
    }

    private void ShowWarning(string message)
    {
        if (quickWarningText == null)
            return;

        quickWarningText.text = message;

        CancelInvoke(nameof(ClearWarning));
        Invoke(nameof(ClearWarning), warningDuration);
    }

    private void ClearWarning()
    {
        if (quickWarningText != null)
        {
            quickWarningText.text = "";
        }
    }

    public string GetPromptText()
    {
        if (CaseManager.Instance == null ||
            CaseManager.Instance.CurrentCase == null)
        {
            return "Filing Cabinet";
        }

        CaseData data = CaseManager.Instance.CurrentCase;

        if (data.isArchived)
        {
            return "Case already archived.";
        }

        if (!(GameStateMachine.Instance.CurrentState is ArchiveCaseState))
        {
            return "Filing Cabinet";
        }

        return "Click to file the Case Folder into storage";
    }
}
