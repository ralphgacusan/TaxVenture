using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SupportingDocumentsPageLink : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject documentButtonListRoot;
    [SerializeField] private GameObject documentButtonPrefab;
    [SerializeField] private DocumentViewerUI documentViewerUI;

    private const int SUPPORTING_DOCUMENTS_PAGE_INDEX = 5;

    private readonly List<GameObject> spawnedButtons = new();

    public void OnFolderPageChanged(int pageIndex)
    {
        bool isDocumentsPage = pageIndex == SUPPORTING_DOCUMENTS_PAGE_INDEX;

        if (documentButtonListRoot != null)
            documentButtonListRoot.SetActive(isDocumentsPage);

        if (isDocumentsPage)
            RebuildButtons();
    }

    public void RefreshButtons()
    {
        if (documentButtonListRoot != null && documentButtonListRoot.activeSelf)
        {
            RebuildButtons();
        }
    }

    private void RebuildButtons()
    {
        foreach (var button in spawnedButtons)
        {
            if (button != null)
                Destroy(button);
        }

        spawnedButtons.Clear();

        if (CaseManager.Instance == null || CaseManager.Instance.CurrentCase == null)
        {
            Debug.LogError("SupportingDocumentsPageLink: No current case found.");
            return;
        }

        foreach (var doc in CaseManager.Instance.CurrentCase.supportingDocuments)
        {
            GameObject buttonObj = Instantiate(documentButtonPrefab, documentButtonListRoot.transform);

            TextMeshProUGUI label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = $"{(doc.isReviewed ? "[x]" : "[ ]")} {doc.documentName}";
            }

            Button button = buttonObj.GetComponent<Button>();
            string capturedName = doc.documentName;

            button.onClick.AddListener(() =>
            {
                if (documentViewerUI == null)
                {
                    Debug.LogError("SupportingDocumentsPageLink: DocumentViewerUI reference is missing.");
                    return;
                }

                documentViewerUI.Show(capturedName);
            });

            spawnedButtons.Add(buttonObj);
        }
    }
}