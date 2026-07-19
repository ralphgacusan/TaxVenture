using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// The filing drawer/cabinet in the File Storage Room. Clicking it while
/// carrying the Case Folder archives the case (simulated, no animation).
/// If the folder isn't being carried, shows a warning instead.
///
/// NOTE ON "CARRYING THE CASE FOLDER":
/// The Case Folder itself was never modeled as a "carried item" in prior
/// milestones (unlike the printed Tax Return in Milestone 12.5, which uses
/// CaseData.isCarryingPrintedReturn + FirstPersonHands). For this milestone,
/// "carrying the Case Folder" is represented the same way: a boolean flag
/// on CaseData, set true once the case reaches ArchiveCaseState (i.e., the
/// folder is conceptually always "in hand" once you've reached this final
/// phase, since there's no earlier point where the player would have left
/// it behind). This keeps the check meaningful without requiring a
/// retroactive redesign of every earlier folder interaction.
///
/// RESPONSIBILITIES:
/// - Verify CaseData.isCarryingCaseFolder before allowing archiving
/// - On successful archive: mark CaseData.isArchived, show confirmation
///   popup, disable further interaction with this cabinet for this case
///
/// CONNECTS WITH:
/// - HighlightEffect (same GameObject)
/// - ArchiveConfirmationPopupUI: shown on success
/// - CaseManager.Instance.CurrentCase: read + write
/// - GameStateMachine: advances to RewardsState after popup closes
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class FilingCabinetInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ArchiveConfirmationPopupUI archiveConfirmationPopupUI;
    [SerializeField] private TextMeshProUGUI quickWarningText;
    [SerializeField] private float warningDuration = 2.5f;

    private HighlightEffect highlight;
    private bool alreadyArchivedThisSession = false;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
    }

    public void OnFocus() => highlight.Highlight();
    public void OnUnfocus() => highlight.Unhighlight();

    public void OnInteract()
    {
        if (alreadyArchivedThisSession)
        {
            return; // disabled — case already filed
        }

        if (!(GameStateMachine.Instance.CurrentState is ArchiveCaseState))
        {
            ShowWarning("There's nothing to archive right now.");
            return;
        }

        CaseData data = CaseManager.Instance.CurrentCase;

        if (!data.isCarryingCaseFolder)
        {
            ShowWarning("You don't have the Case Folder with you.");
            return;
        }

        ArchiveCase(data);

    }

    private void ArchiveCase(CaseData data)
    {
        data.isArchived = true;
        data.isCarryingCaseFolder = false; // "placed" into storage — simulated, no animation
        alreadyArchivedThisSession = true;

        archiveConfirmationPopupUI.Show(OnArchiveConfirmed);

    }

    private void OnArchiveConfirmed()
    {
        if (GameStateMachine.Instance.CurrentState is ArchiveCaseState)
        {
            GameStateMachine.Instance.ChangeState(new RewardsState());
        }
    }

    private void ShowWarning(string message)
    {
        if (quickWarningText == null) return;
        quickWarningText.text = message;
        CancelInvoke(nameof(ClearWarning));
        Invoke(nameof(ClearWarning), warningDuration);
    }

    private void ClearWarning()
    {
        if (quickWarningText != null) quickWarningText.text = "";
    }

    public string GetPromptText()
    {
        if (alreadyArchivedThisSession) return "Case already archived.";
        return "Click to file the Case Folder into storage";
    }
}