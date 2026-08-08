using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// The filing drawer/cabinet in the File Storage Room. Clicking it while
/// carrying the Case Folder archives the current case.
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
/// - Continue to RewardsState after confirmation/result popup
///
/// CONNECTS WITH:
/// - HighlightEffect
/// - ArchiveConfirmationPopupUI
/// - CaseManager.Instance.CurrentCase
/// - GameStateMachine
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
        // Make sure there is a current case.
        if (CaseManager.Instance == null ||
            CaseManager.Instance.CurrentCase == null)
        {
            ShowWarning("No active case.");
            return;
        }

        CaseData data = CaseManager.Instance.CurrentCase;

        // ---------------------------------------------------------
        // 1. Prevent interacting with the cabinet outside the
        //    Archive Case phase.
        // ---------------------------------------------------------
        if (!(GameStateMachine.Instance.CurrentState is ArchiveCaseState))
        {
            ShowWarning("There's nothing to archive right now.");
            return;
        }

        // ---------------------------------------------------------
        // 2. Use CaseData.isArchived instead of a local boolean.
        //
        //    This is important because CaseData changes when
        //    CaseProgressionManager loads the next case.
        // ---------------------------------------------------------
        if (data.isArchived)
        {
            ShowWarning("This case has already been archived.");
            return;
        }

        // ---------------------------------------------------------
        // 3. Verify the player has the Case Folder.
        // ---------------------------------------------------------
        if (!data.isCarryingCaseFolder)
        {
            ShowWarning("You don't have the Case Folder with you.");
            return;
        }

        // Remove the visual carried document, if present.
        if (FirstPersonHands.Instance != null)
        {
            FirstPersonHands.Instance.HideCarriedDocument();
        }

        ArchiveCase(data);
    }

    private void ArchiveCase(CaseData data)
    {
        // Store archive status directly on the current case.
        data.isArchived = true;
        data.isCarryingCaseFolder = false;

        Debug.Log(
            $"[FilingCabinetInteractable] Case archived: {data.caseNumber}"
        );

        archiveConfirmationPopupUI.Show(OnArchiveConfirmed);
    }

    private void OnArchiveConfirmed()
    {
        CaseData data = CaseManager.Instance.CurrentCase;

        ClientOutcomeBranch branch =
            ClientOutcomeEvaluator.Determine(data);

        if (branch == ClientOutcomeBranch.CorrectNoIssues)
        {
            // Perfect case:
            // Level Result was intentionally delayed until after
            // the archive confirmation.
            LevelResultPopupUI.Instance.Show(
                data,
                OnLevelResultClosedAfterArchive
            );
        }
        else
        {
            // Other branches already showed the Level Result
            // before the archive phase.
            if (GameStateMachine.Instance.CurrentState is ArchiveCaseState)
            {
                GameStateMachine.Instance.ChangeState(
                    new RewardsState()
                );
            }
        }
    }

    private void OnLevelResultClosedAfterArchive()
    {
        if (GameStateMachine.Instance.CurrentState is ArchiveCaseState)
        {
            GameStateMachine.Instance.ChangeState(
                new RewardsState()
            );
        }
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
