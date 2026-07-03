using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Page 6 (Supporting Documents) needs to be clickable per document, unlike
/// every other folder page which is just static read-only text on the
/// shared Paper. Rather than complicating CaseFolderUI's generic text
/// renderer, this script manages a small vertical list of document buttons
/// that overlays on top of the Paper — visible ONLY while Page 6 is the
/// active page, positioned to match where Page 6's text would be.
///
/// RESPONSIBILITIES:
/// - Build one button per supporting document, labeled with its name and
///   reviewed status
/// - Show this button list only when Page 6 is active; hide otherwise
/// - On document button click, open DocumentViewerUI for that document
///
/// CONNECTS WITH:
/// - CaseFolderUI: tells this script when the current page changes (see
///   CaseFolderUI edit below) and whether it's Page 6
/// - DocumentViewerUI: opens the clicked document
/// - CaseManager.Instance.CurrentCase.supportingDocuments: source list
/// </summary>
public class SupportingDocumentsPageLink : MonoBehaviour
{
    [SerializeField] private GameObject documentButtonListRoot;
    [SerializeField] private GameObject documentButtonPrefab; // simple Button + TMP child
    [SerializeField] private DocumentViewerUI documentViewerUI;

    private const int SUPPORTING_DOCUMENTS_PAGE_INDEX = 5; // Page 6, zero-indexed

    private List<GameObject> spawnedButtons = new List<GameObject>();

    /// <summary>
    /// Called by CaseFolderUI whenever the current page changes. Shows/hides
    /// and rebuilds the document button list based on whether the current
    /// page is Page 6.
    /// </summary>
    public void OnFolderPageChanged(int pageIndex)
    {
        bool isDocumentsPage = pageIndex == SUPPORTING_DOCUMENTS_PAGE_INDEX;
        documentButtonListRoot.SetActive(isDocumentsPage);

        if (isDocumentsPage)
        {
            RebuildButtons();
        }
    }

    /// <summary>
    /// Called after a document is closed, to refresh reviewed [x]/[ ] labels
    /// without needing to flip pages away and back.
    /// </summary>
    public void RefreshButtons()
    {
        if (documentButtonListRoot.activeSelf)
        {
            RebuildButtons();
        }
    }

    private void RebuildButtons()
    {
        foreach (var btn in spawnedButtons) Destroy(btn);
        spawnedButtons.Clear();

        foreach (var doc in CaseManager.Instance.CurrentCase.supportingDocuments)
        {
            GameObject buttonObj = Instantiate(documentButtonPrefab, documentButtonListRoot.transform);
            TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            label.text = $"{(doc.isReviewed ? "[x]" : "[ ]")} {doc.documentName}";

            Button button = buttonObj.GetComponent<Button>();
            string capturedName = doc.documentName; // avoid closure-over-loop-variable bug
            button.onClick.AddListener(() =>
            {
                documentViewerUI.Show(capturedName);
            });

            spawnedButtons.Add(buttonObj);
        }
    }
}