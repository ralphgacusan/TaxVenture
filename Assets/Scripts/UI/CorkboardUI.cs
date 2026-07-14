using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// Displays all supporting documents as "pinned" clickable entries, plus
/// the Consultant Findings (potential issues) from CaseData, and lets the
/// player make the Ready-for-Filing / Not-Ready-for-Filing assessment
/// decision. Reuses DocumentViewerUI for actually inspecting a document's
/// fields, same as the Folder's Page 6 in Milestone 6.
///
/// RESPONSIBILITIES:
/// - Build one pinned-document entry per CaseData.supportingDocuments
/// - Show potential issues identified (read-only, from CaseData)
/// - Provide "Mark Ready for Filing" / "Mark Not Ready for Filing" buttons
///   that write CaseData.caseAssessment
/// - Show/hide the panel; note this panel does NOT control the camera
///   transition itself (CorkboardInteractable does) — this script only
///   owns the evidence-board UI content
///
/// CONNECTS WITH:
/// - CorkboardInteractable: calls Show()
/// - DocumentViewerUI: opened when a pinned document is clicked
/// - WorkstationUI's Close button: exits first-person as usual (this panel
///   has its own Close to hide itself first, same two-level pattern as the
///   Folder)
/// - CaseManager.Instance.CurrentCase: source of documents/issues, and
///   destination for the assessment decision
/// </summary>
public class CorkboardUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject corkboardPanelRoot;

    [Header("Pinned Documents")]
    [SerializeField] private GameObject pinnedDocumentListRoot;
    [SerializeField] private GameObject pinnedDocumentPrefab;
    [SerializeField] private DocumentViewerUI documentViewerUI;

    [Header("Consultant Findings")]
    [SerializeField] private TextMeshProUGUI findingsText;

    [Header("Assessment Decision")]
    [SerializeField] private Button markReadyButton;
    [SerializeField] private Button markNotReadyButton;
    [SerializeField] private TextMeshProUGUI currentAssessmentText;

    private List<GameObject> spawnedPins = new List<GameObject>();

    private void Awake()
    {
        Hide();
        markReadyButton.onClick.AddListener(() => SetAssessment(CaseAssessment.ReadyForFiling));
        markNotReadyButton.onClick.AddListener(() => SetAssessment(CaseAssessment.NotReadyForFiling));
    }

    public void Show()
    {
        corkboardPanelRoot.SetActive(true);
        RebuildPinnedDocuments();
        RenderFindings();
        RenderCurrentAssessment();
    }

    public void Hide()
    {
        corkboardPanelRoot.SetActive(false);
    }

    private void RebuildPinnedDocuments()
    {
        foreach (var pin in spawnedPins) Destroy(pin);
        spawnedPins.Clear();

        foreach (var doc in CaseManager.Instance.CurrentCase.supportingDocuments)
        {
            GameObject pinObj = Instantiate(pinnedDocumentPrefab, pinnedDocumentListRoot.transform);
            TextMeshProUGUI label = pinObj.GetComponentInChildren<TextMeshProUGUI>();
            label.text = $"{(doc.isReviewed ? "[Reviewed]" : "[Not Reviewed]")} {doc.documentName}";

            Button button = pinObj.GetComponent<Button>();
            string capturedName = doc.documentName;
            button.onClick.AddListener(() =>
            {
                documentViewerUI.Show(capturedName);
                // Refresh pin labels once the viewer is closed, since opening
                // it here also marks the document reviewed (same behavior as
                // Milestone 6's folder Page 6).
            });

            spawnedPins.Add(pinObj);
        }
    }

    /// <summary>
    /// Called externally (see DocumentViewerUI note below) so pin labels
    /// update their [Reviewed] status immediately after closing a document,
    /// without needing to close/reopen the whole corkboard.
    /// </summary>
    public void RefreshPins()
    {
        if (corkboardPanelRoot.activeSelf)
        {
            RebuildPinnedDocuments();
        }
    }

    private void RenderFindings()
    {
        CaseData data = CaseManager.Instance.CurrentCase;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Potential Issues Identified:");
        foreach (var issue in data.potentialIssuesIdentified)
        {
            sb.AppendLine($"- {issue}");
        }
        findingsText.text = sb.ToString();
    }

    private void SetAssessment(CaseAssessment assessment)
    {
        CaseManager.Instance.CurrentCase.caseAssessment = assessment;
        RenderCurrentAssessment();
    }

    private void RenderCurrentAssessment()
    {
        CaseAssessment current = CaseManager.Instance.CurrentCase.caseAssessment;
        currentAssessmentText.text = $"Current Assessment: {FormatEnum(current.ToString())}";
    }

    private string FormatEnum(string enumName)
    {
        return System.Text.RegularExpressions.Regex.Replace(enumName, "(\\B[A-Z])", " $1");
    }
}