using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// The ONLY place CaseData.caseAssessment can ever be set. Player selects a
/// stamp (Ready/Not Ready) based on their own review of the evidence, then
/// applies it directly to the folder paper — no validation against a prior
/// Corkboard decision (that system has been removed; the stamp IS the
/// decision now). Instantly refreshes the open Case Folder UI so Page 1's
/// assessment text updates immediately without needing to close/reopen it.
///
/// CONNECTS WITH:
/// - CaseFolderUI: calls SelectStamp() from button OnClick, calls
///   TryApplyStampToPaper() when the Paper itself is clicked while stamping;
///   also has Refresh() called on it directly after a successful stamp
/// - CaseManager.Instance.CurrentCase: read/write target
/// - GameStateMachine: transition on success
/// </summary>
public class StampUI : MonoBehaviour
{
    [Header("Stamp Buttons (inside folder panel)")]
    [SerializeField] private Button readyStampButton;
    [SerializeField] private Button notReadyStampButton;
    [SerializeField] private Image readyStampButtonImage;
    [SerializeField] private Image notReadyStampButtonImage;
    [SerializeField] private Color unselectedColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.3f);

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private float feedbackDisplayDuration = 2.5f;

    private StampType? selectedStampType = null;

    public bool HasSelectedStamp => selectedStampType.HasValue;

    private void Awake()
    {
        readyStampButton.onClick.AddListener(() => SelectStamp(StampType.ReadyForFiling));
        notReadyStampButton.onClick.AddListener(() => SelectStamp(StampType.NotReadyForFiling));
        ClearFeedback();
    }

    public void ResetSelection()
    {
        selectedStampType = null;
        readyStampButtonImage.color = unselectedColor;
        notReadyStampButtonImage.color = unselectedColor;
    }

    private void SelectStamp(StampType type)
    {
        selectedStampType = type;
        readyStampButtonImage.color = type == StampType.ReadyForFiling ? selectedColor : unselectedColor;
        notReadyStampButtonImage.color = type == StampType.NotReadyForFiling ? selectedColor : unselectedColor;

        ShowFeedback($"{type} stamp selected. Click the paper to apply it.", false);
    }

    /// <summary>
    /// Called by CaseFolderUI when the Paper itself is clicked while a stamp
    /// is currently selected. No validation step anymore — whichever stamp
    /// the player picked IS the final assessment, applied immediately.
    /// </summary>
    public void TryApplyStampToPaper()
    {
        if (!selectedStampType.HasValue) return;

        CaseData data = CaseManager.Instance.CurrentCase;
        ApplyStamp(selectedStampType.Value, data);

        ResetSelection();
    }

    private void ApplyStamp(StampType stamp, CaseData data)
    {
        data.assessmentStamped = true;

        if (stamp == StampType.ReadyForFiling)
        {
            data.caseAssessment = CaseAssessment.ReadyForFiling;
            data.filingStatus = FilingStatus.ReadyForFiling;

            ShowFeedback("Stamped: READY FOR FILING.", false);

            if (GameStateMachine.Instance.CurrentState is StampAssessmentState)
                GameStateMachine.Instance.ChangeState(new PrepareReturnState());
        }
        else
        {
            data.caseAssessment = CaseAssessment.NotReadyForFiling;

            ShowFeedback("Stamped: NOT READY FOR FILING.", false);

            if (GameStateMachine.Instance.CurrentState is StampAssessmentState)
                GameStateMachine.Instance.ChangeState(new ComplianceAuditState());
        }

        // Instantly refresh the Case Folder's Page 1 so the new assessment
        // text shows immediately, matching the old behavior.
        FindFirstObjectByType<CaseFolderUI>()?.Refresh();
    }

    private void ShowFeedback(string message, bool isWarning)
    {
        feedbackText.text = message;
        feedbackText.color = isWarning ? Color.red : Color.black;
        CancelInvoke(nameof(ClearFeedback));
        Invoke(nameof(ClearFeedback), feedbackDisplayDuration);
    }

    private void ClearFeedback()
    {
        if (feedbackText != null) feedbackText.text = "";
    }
}