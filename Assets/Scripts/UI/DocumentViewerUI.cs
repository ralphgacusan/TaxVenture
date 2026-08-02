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

    private List<DocumentFieldRow> spawnedRows = new List<DocumentFieldRow>();

    private void Awake()
    {
        Hide();
    }

    public void Show(string documentName)
    {
        DocumentFieldData data = DocumentDataProvider.GetFieldsFor(documentName);
        documentNameText.text = data.DocumentName;

        foreach (var row in spawnedRows) Destroy(row.gameObject);
        spawnedRows.Clear();

        foreach (var field in data.Fields)
        {
            var rowObj = Instantiate(fieldRowPrefab, fieldRowListRoot);
            rowObj.Initialize(field);
            spawnedRows.Add(rowObj);
        }

        MarkAsReviewed(documentName);
        viewerPanelRoot.SetActive(true);
    }

    public void Hide()
    {
        viewerPanelRoot.SetActive(false);
        folderPageLink?.RefreshButtons();
        corkboardSpawner?.RefreshAllDocumentVisuals();
    }

    private void MarkAsReviewed(string documentName)
    {
        var doc = CaseManager.Instance.CurrentCase.supportingDocuments.Find(d => d.documentName == documentName);
        if (doc != null) doc.isReviewed = true;
    }
}