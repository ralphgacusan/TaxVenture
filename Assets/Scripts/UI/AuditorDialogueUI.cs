using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Static dialogue-style panel showing the Auditor's review result — either
/// a congratulations message (pass) or a list of specific mistakes to fix
/// (fail), per design doc: "If correct: congratulates... If incorrect:
/// explains the mistakes."
///
/// RESPONSIBILITIES:
/// - Run ComplianceChecker against the current case
/// - Display pass/fail outcome
/// - Store the mistake count into CaseData for Milestone 14 to consume
/// - Provide a Continue/Return button appropriate to the outcome
///
/// CONNECTS WITH:
/// - AuditorInteractable: calls Show() when the player hands over the case
/// - ComplianceChecker: runs the actual rule checks
/// - CaseManager.Instance.CurrentCase: read + write (auditMistakeCount, auditPassed)
/// - GameStateMachine: advances to RewardsState on pass
/// - AuditorInteractable's NpcStateMachine: notified when audit dialogue closes
/// </summary>
public class AuditorDialogueUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject auditorPanelRoot;

    [Header("Content")]
    [SerializeField] private TextMeshProUGUI resultHeadingText;
    [SerializeField] private TextMeshProUGUI issuesListText;

    [Header("Continue")]
    [SerializeField] private GameObject continueButton;

    private NpcStateMachine auditorNpcState;

    [Header("Missing Form Warning")]
    [SerializeField] private TextMeshProUGUI quickWarningText; // small floating text, no full panel
    [SerializeField] private float warningDuration = 2.5f;

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

    private void Awake()
    {
        Hide();
    }

    public void Show(NpcStateMachine npcState)
    {
        auditorNpcState = npcState;

        CaseData data = CaseManager.Instance.CurrentCase;
        List<ComplianceIssue> issues = ComplianceChecker.RunCheck(data);

        data.auditMistakeCount = issues.Count;
        data.auditPassed = issues.Count == 0;

        if (data.auditPassed)
        {
            resultHeadingText.text = "Audit Passed — Congratulations!";
            issuesListText.text = "No issues found. Your case handling was thorough and accurate.";
        }
        else
        {
            resultHeadingText.text = $"Audit Found {issues.Count} Issue(s)";
            var sb = new System.Text.StringBuilder();
            foreach (var issue in issues)
            {
                sb.AppendLine($"- {issue.Description}");
            }
            sb.AppendLine();
            sb.AppendLine("Please return to your desk and address these before re-submitting for audit.");
            issuesListText.text = sb.ToString();
        }

        auditorPanelRoot.SetActive(true);
    }

    public void Hide()
    {
        auditorPanelRoot.SetActive(false);
    }

    /// <summary>
    /// Wired to the Continue button. Closes the panel, marks the Auditor NPC
    /// Completed, and — only on a pass — advances the game FSM toward Rewards.
    /// On a fail, the game FSM deliberately stays wherever it was, so the
    /// player can go fix issues and return to the Auditor again later.
    /// </summary>
    public void OnContinuePressed()
    {
        CameraController.Instance.ExitInterview();

        Hide();

        CaseData data = CaseManager.Instance.CurrentCase;

        if (data.auditPassed)
        {
            auditorNpcState?.ChangeState(new NpcCompletedState());

            if (GameStateMachine.Instance.CurrentState is ComplianceAuditState)
            {
                GameStateMachine.Instance.ChangeState(new RewardsState());
            }
        }
        else
        {
            // Fail: return the Auditor to Idle so the player can re-trigger
            // the audit later without it being stuck in Dialogue/Completed.
            auditorNpcState?.ChangeState(new NpcIdleState());
        }
    }
}