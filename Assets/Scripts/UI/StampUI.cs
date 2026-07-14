using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// Owns stamp selection + validation logic. Now driven by two UI buttons
/// inside the folder panel (Ready / Not Ready) rather than physical world
/// stamps. Validates the selected stamp against CaseData.caseAssessment
/// (set at the Corkboard), same rule as before.
///
/// CONNECTS WITH:
/// - CaseFolderUI: calls SelectStamp() from button OnClick, calls
///   TryApplyStampToPaper() when the Paper itself is clicked while stamping
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
    /// is currently selected. Validates against CaseData.caseAssessment.
    /// </summary>
    public void TryApplyStampToPaper()
    {
        if (!selectedStampType.HasValue) return;

        CaseData data = CaseManager.Instance.CurrentCase;
        bool matchesReady = selectedStampType.Value == StampType.ReadyForFiling
            && data.caseAssessment == CaseAssessment.ReadyForFiling;
        bool matchesNotReady = selectedStampType.Value == StampType.NotReadyForFiling
            && data.caseAssessment == CaseAssessment.NotReadyForFiling;

        if (matchesReady || matchesNotReady)
        {
            ApplyStamp(selectedStampType.Value, data);
        }
        else
        {
            ShowFeedback(
                $"Mismatch: your evidence review concluded '{data.caseAssessment}', " +
                $"but you tried to stamp '{selectedStampType.Value}'.", true);
        }

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