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