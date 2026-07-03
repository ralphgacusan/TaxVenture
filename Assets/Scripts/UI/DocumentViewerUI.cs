using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Displays a single enlarged document's fields as a static overlay panel,
/// per the design doc: "The selected document becomes focused. The document
/// is enlarged for easier reading." Sits above the folder panel; closing it
/// (X button) returns to the folder without affecting folder state.
///
/// RESPONSIBILITIES:
/// - Show/hide the viewer panel
/// - Render a document's name + all its (label, value) fields as text
/// - Mark the document as reviewed in CaseData when opened
///
/// CONNECTS WITH:
/// - SupportingDocumentsPageLink (below): calls Show(documentName) when a
///   document entry on Page 6 is clicked
/// - DocumentDataProvider: source of field data to display
/// - CaseManager.Instance.CurrentCase.supportingDocuments: updated with
///   isReviewed = true when a document is opened
/// - CaseFolderUI: not directly coupled — this panel simply layers on top;
///   closing it doesn't call anything on CaseFolderUI, so the folder's
///   current page/state is untouched underneath.
/// </summary>
public class DocumentViewerUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject viewerPanelRoot;

    [Header("Content")]
    [SerializeField] private TextMeshProUGUI documentNameText;
    [SerializeField] private TextMeshProUGUI fieldsText;

    [Header("Optional: refresh Page 6 list on close")]
    [SerializeField] private SupportingDocumentsPageLink supportingDocumentsPageLink;

    private void Awake()
    {
        Hide();
    }

    /// <summary>
    /// Opens the viewer for the given document name, pulling its fields from
    /// DocumentDataProvider and marking it reviewed in CaseData.
    /// </summary>
    public void Show(string documentName)
    {
        DocumentFieldData data = DocumentDataProvider.GetFieldsFor(documentName);

        documentNameText.text = data.DocumentName;

        var sb = new System.Text.StringBuilder();
        foreach (var field in data.Fields)
        {
            sb.AppendLine($"{field.label}: {field.value}");
        }
        fieldsText.text = sb.ToString();

        MarkAsReviewed(documentName);

        viewerPanelRoot.SetActive(true);
    }

    public void Hide()
    {
        viewerPanelRoot.SetActive(false);
        supportingDocumentsPageLink?.RefreshButtons();
    }

    /// <summary>
    /// Flags the given document as reviewed in the current case's data.
    /// This is what lets Page 6 show "[x]" instead of "[ ]" next time the
    /// folder is opened, and is what Milestone 11's Evidence Board will
    /// later check.
    /// </summary>
    private void MarkAsReviewed(string documentName)
    {
        var doc = CaseManager.Instance.CurrentCase.supportingDocuments
            .Find(d => d.documentName == documentName);

        if (doc != null)
        {
            doc.isReviewed = true;
        }
    }
}