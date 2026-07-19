using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Interview-style linear dialogue for the Auditor: one line at a time, a
/// single Continue button, no branching questions (unlike the Client
/// Interview's question-choice system — the Auditor's dialogue is scripted
/// and sequential, not player-directed). Each detected issue gets exactly
/// one vague line; after all issues, a closing statement plays; after that,
/// the Audit Summary popup appears. Player controls stay locked
/// (CameraController.LockPlayerControls) for the ENTIRE sequence, only
/// unlocking after the summary popup is closed.
///
/// FLOW:
/// AuditorInteractable.OnInteract() -> Show()
///   -> builds vague issue-line queue via ComplianceChecker
///   -> displays line 1, Continue advances through queue
///   -> after last issue line, shows closing statement
///   -> Continue on closing statement -> Hide() + AuditSummaryPopupUI.Show()
///   -> popup Close -> CameraController.UnlockPlayerControls() + NPC state
///      + GameStateMachine transition (pass/fail branching, same as before)
/// </summary>
public class AuditorDialogueUI : MonoBehaviour
{
    [Header("Panel (same structural style as InterviewPanel)")]
    [SerializeField] private GameObject auditorPanelRoot;
    [SerializeField] private TextMeshProUGUI auditorLineText;
    [SerializeField] private GameObject continueButton;

    [Header("Summary Popup")]
    [SerializeField] private AuditSummaryPopupUI summaryPopupUI;

    [Header("Missing Form Warning (unchanged from before)")]
    [SerializeField] private TextMeshProUGUI quickWarningText;
    [SerializeField] private float warningDuration = 2.5f;

    private List<ComplianceIssue> currentIssues;
    private int lineIndex;
    private bool isShowingClosingStatement;
    private NpcStateMachine auditorNpcState;

    private void Awake()
    {
        Hide();
    }

    public void Show(NpcStateMachine npcState)
    {
        auditorNpcState = npcState;

        CaseData data = CaseManager.Instance.CurrentCase;
        currentIssues = ComplianceChecker.RunCheck(data);
        data.auditMistakeCount = currentIssues.Count;
        data.auditPassed = currentIssues.Count == 0;

        lineIndex = 0;
        isShowingClosingStatement = false;

        auditorPanelRoot.SetActive(true);
        ShowCurrentLine();
    }

    public void Hide()
    {
        auditorPanelRoot.SetActive(false);
    }

    private void ShowCurrentLine()
    {
        if (currentIssues.Count == 0)
        {
            auditorLineText.text = "I've reviewed everything, and I found no issues. Well done.";
            isShowingClosingStatement = true;
            return;
        }

        if (lineIndex < currentIssues.Count)
        {
            auditorLineText.text = currentIssues[lineIndex].ShortLabel;
        }
        else if (!isShowingClosingStatement)
        {
            auditorLineText.text = "That concludes my review. Please check the audit summary before making your corrections.";
            isShowingClosingStatement = true;
        }
    }

    /// <summary>
    /// Wired to the single Continue button. Advances through issue lines,
    /// then the closing statement, then transitions to the summary popup.
    /// </summary>
    public void OnContinuePressed()
    {
        if (isShowingClosingStatement)
        {
            Hide();
            summaryPopupUI.Show(currentIssues, OnSummaryClosed);
            return;
        }

        lineIndex++;
        ShowCurrentLine();
    }

    /// <summary>
    /// Called after the player closes the Audit Summary popup. This is the
    /// ONLY point where player controls are restored and the FSM/NPC state
    /// actually advances — matching "Only after the Audit Summary popup is
    /// closed should player movement and camera controls be restored."
    /// </summary>
    private void OnSummaryClosed()
    {
        CameraController.Instance.UnlockPlayerControls();

        CaseData data = CaseManager.Instance.CurrentCase;

        if (data.auditPassed)
        {
            auditorNpcState?.ChangeState(new NpcCompletedState());

            if (GameStateMachine.Instance.CurrentState is ComplianceAuditState)
            {
                GameStateMachine.Instance.ChangeState(new CaseOutcomeState());
            }
        }
        else
        {
            auditorNpcState?.ChangeState(new NpcIdleState());
        }
    }

    public void ShowMissingFormWarning()
    {
        if (quickWarningText == null) return;
        quickWarningText.text = "You need to bring the printed Tax Return before the audit can begin.";
        CancelInvoke(nameof(ClearWarning));
        Invoke(nameof(ClearWarning), warningDuration);
    }

    private void ClearWarning()
    {
        if (quickWarningText != null) quickWarningText.text = "";
    }
}