using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Read-only popup shown after the Auditor's dialogue concludes.
/// Displays either a success message or hints about incomplete facts.
/// </summary>
public class AuditSummaryPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject popupPanelRoot;
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private GameObject closeButton;

    private System.Action onClosed;

    private void Awake()
    {
        Hide();
    }

    public void Show(
        List<ComplianceIssue> issues,
        System.Action onClosedCallback)
    {
        CameraController.Instance.LockPlayerControls();
        onClosed = onClosedCallback;

        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Audit Summary");
        sb.AppendLine();

        if (issues == null || issues.Count == 0)
        {
            sb.AppendLine("Congratulations!");
            sb.AppendLine();
            sb.AppendLine(
                "You now know your taxpayer. All three required facts have been gathered."
            );
        }
        else
        {
            sb.AppendLine($"Hints: {issues.Count}");
            sb.AppendLine();

            foreach (var issue in issues)
            {
                if (issue == null)
                {
                    continue;
                }

                sb.AppendLine($"- {issue.ShortLabel}");
            }

            sb.AppendLine();
            sb.AppendLine(
                "Review the case again and look for details that may still be missing."
            );
        }

        summaryText.text = sb.ToString();
        popupPanelRoot.SetActive(true);
        HudRewardScreen.Present(popupPanelRoot, BuildScreen(issues));
    }

    /// <summary>The findings card (Office HUD): what the Auditor found and what it costs, one button to go fix it.</summary>
    private static RewardScreenData BuildScreen(List<ComplianceIssue> issues)
    {
        bool clean = issues == null || issues.Count == 0;
        var data = new RewardScreenData
        {
            Title = "Audit Summary",
            ButtonLabel = clean ? "Continue" : "Review the case",
            Accent = clean ? new Color32(110, 201, 140, 255) : new Color32(226, 112, 96, 255)
        };

        if (clean)
        {
            data.Subtitle = "The Auditor reviewed your case";
            data.HasStatus = true;
            data.StatusGood = true;
            data.StatusText = "APPROVED";
            data.StatusDetail = "Every required fact is correct";
            data.Fact("Mistakes", "0");
            data.Summary = "You now know your taxpayer. All three required facts have been gathered.";
            // A success needs no decision: it moves on by itself (tap to go now).
            data.AutoContinueSeconds = 3.5f;
            return data;
        }

        data.Subtitle = issues.Count == 1 ? "The Auditor found 1 thing to fix" : $"The Auditor found {issues.Count} things to fix";
        foreach (var issue in issues)
            if (issue != null) data.Issues.Add(issue.ShortLabel);

        CaseProgressionManager progression = CaseProgressionManager.Instance;
        if (progression != null)
        {
            LevelScoring.Award cost = LevelScoring.FailedAuditCost(progression.Score.Rules, issues.Count);
            data.Fact("Case reward", $"-{cost.Exp} EXP  -{cost.Hearts} hearts");
        }
        data.Summary = "Your answers were cleared. Look at the case again and resubmit it to the Auditor.";
        return data;
    }

    public void OnCloseButtonPressed()
    {
        popupPanelRoot.SetActive(false);

        CameraController.Instance.UnlockPlayerControls();

        onClosed?.Invoke();

        FirstPersonHands.Instance.ShowCarriedDocument();
    }

    private void Hide()
    {
        popupPanelRoot.SetActive(false);
    }
}