using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Minimal read-only popup listing CaseData.potentialIssuesIdentified.
/// Intentionally the smallest possible UI — one panel, one text field, one
/// close button — since findings are static reference text, not an
/// interactive system.
/// </summary>
public class FindingsPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject popupPanelRoot;
    [SerializeField] private TextMeshProUGUI issuesText;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Potential Issues Identified:");
        foreach (var issue in CaseManager.Instance.CurrentCase.potentialIssuesIdentified)
        {
            sb.AppendLine($"- {issue}");
        }
        issuesText.text = sb.ToString();
        popupPanelRoot.SetActive(true);
    }

    public void Hide()
    {
        popupPanelRoot.SetActive(false);
    }
}