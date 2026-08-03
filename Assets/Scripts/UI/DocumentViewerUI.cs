using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DocumentViewerUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject viewerPanelRoot;

    [Header("Content")]
    [SerializeField] private TextMeshProUGUI documentNameText;
    [SerializeField] private Transform fieldRowListRoot;
    [SerializeField] private DocumentFieldRow fieldRowPrefab;

    [Header("Refresh Targets (optional)")]
    [SerializeField] private SupportingDocumentsPageLink folderPageLink;
    [SerializeField] private CorkboardDocumentSpawner corkboardSpawner;

    [Header("Drag")]
    [SerializeField] private DocumentWindowDrag windowDrag;

    private readonly List<DocumentFieldRow> spawnedRows = new();

    private void Awake()
    {
        Hide();
    }

    public void Show(string documentName)
    {
        // Reset the window to the center every time it opens.
        windowDrag?.ResetWindowPosition();

        DocumentFieldData data = DocumentDataProvider.GetFieldsFor(documentName);
        documentNameText.text = data.DocumentName;

        ClearRows();

        foreach (var field in data.Fields)
        {
            var row = Instantiate(fieldRowPrefab, fieldRowListRoot);
            row.Initialize(field);
            spawnedRows.Add(row);
        }

        MarkAsReviewed(documentName);

        viewerPanelRoot.SetActive(true);

        // Bring the document window to the front.
        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        viewerPanelRoot.SetActive(false);

        folderPageLink?.RefreshButtons();
        corkboardSpawner?.RefreshAllDocumentVisuals();
    }

    private void ClearRows()
    {
        foreach (var row in spawnedRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }

        spawnedRows.Clear();
    }

    private void MarkAsReviewed(string documentName)
    {
        var doc = CaseManager.Instance.CurrentCase.supportingDocuments
            .Find(d => d.documentName == documentName);

        if (doc != null)
            doc.isReviewed = true;
    }
}