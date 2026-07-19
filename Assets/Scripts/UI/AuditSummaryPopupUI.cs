using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Read-only popup shown after the Auditor's dialogue concludes. Lists
/// issue count and short labels only — no explanations, no correct values.
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

    public void Show(List<ComplianceIssue> issues, System.Action onClosedCallback)
    {
        onClosed = onClosedCallback;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Audit Summary");
        sb.AppendLine();

        if (issues.Count == 0)
        {
            sb.AppendLine("Issues Found: 0");
            sb.AppendLine("No issues detected. This case is fully compliant.");
        }
        else
        {
            sb.AppendLine($"Issues Found: {issues.Count}");
            sb.AppendLine();
            foreach (var issue in issues)
            {
                sb.AppendLine($"- {issue.ShortLabel}");
            }
        }

        summaryText.text = sb.ToString();
        popupPanelRoot.SetActive(true);
    }

    public void OnCloseButtonPressed()
    {
        popupPanelRoot.SetActive(false);
        onClosed?.Invoke();
    }

    private void Hide()
    {
        popupPanelRoot.SetActive(false);
    }
}