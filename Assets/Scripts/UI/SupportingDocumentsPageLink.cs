using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SupportingDocumentsPageLink : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [SerializeField] private GameObject documentButtonListRoot;

    [SerializeField] private GameObject documentButtonPrefab;


    // =========================================================
    // DOCUMENT VIEWER
    // =========================================================

    [Header("Document Viewer")]

    [SerializeField] private DocumentViewerManager documentViewerManager;


    // =========================================================
    // PAGE
    // =========================================================

    private const int SUPPORTING_DOCUMENTS_PAGE_INDEX = 5;


    // =========================================================
    // BUTTONS
    // =========================================================

    private readonly List<GameObject> spawnedButtons = new();


    // =========================================================
    // FOLDER PAGE CHANGED
    // =========================================================

    public void OnFolderPageChanged(
        int pageIndex
    )
    {
        bool isDocumentsPage =
            pageIndex ==
            SUPPORTING_DOCUMENTS_PAGE_INDEX;


        if (documentButtonListRoot != null)
        {
            documentButtonListRoot.SetActive(
                isDocumentsPage
            );
        }


        if (isDocumentsPage)
        {
            RebuildButtons();
        }
    }


    // =========================================================
    // REFRESH BUTTONS
    // =========================================================

    public void RefreshButtons()
    {
        if (documentButtonListRoot != null &&
            documentButtonListRoot.activeSelf)
        {
            RebuildButtons();
        }
    }


    // =========================================================
    // REBUILD BUTTONS
    // =========================================================

    private void RebuildButtons()
    {
        // -----------------------------------------------------
        // Remove old buttons
        // -----------------------------------------------------

        foreach (GameObject button in spawnedButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }


        spawnedButtons.Clear();


        // -----------------------------------------------------
        // Validate CaseManager
        // -----------------------------------------------------

        if (CaseManager.Instance == null ||
            CaseManager.Instance.CurrentCase == null)
        {
            Debug.LogError(
                "SupportingDocumentsPageLink: " +
                "No current case found."
            );

            return;
        }


        // -----------------------------------------------------
        // Validate DocumentViewerManager
        // -----------------------------------------------------

        if (documentViewerManager == null)
        {
            Debug.LogError(
                "SupportingDocumentsPageLink: " +
                "DocumentViewerManager reference is missing."
            );

            return;
        }


        // -----------------------------------------------------
        // Create document buttons
        // -----------------------------------------------------

        foreach (
            var doc in
            CaseManager.Instance.CurrentCase.supportingDocuments)
        {
            GameObject buttonObj =
                Instantiate(
                    documentButtonPrefab,
                    documentButtonListRoot.transform
                );


            // -------------------------------------------------
            // DOCUMENT NAME
            // -------------------------------------------------

            string capturedName =
                doc.documentName;


            // -------------------------------------------------
            // BUTTON LABEL
            // -------------------------------------------------

            TextMeshProUGUI label =
                buttonObj.GetComponentInChildren<
                    TextMeshProUGUI
                >();


            if (label != null)
            {
                label.text =
                    $"{(doc.isReviewed ? "[x]" : "[ ]")} " +
                    capturedName;
            }


            // -------------------------------------------------
            // GET BUTTON
            // -------------------------------------------------

            Button button =
                buttonObj.GetComponent<Button>();


            if (button == null)
            {
                Debug.LogError(
                    $"SupportingDocumentsPageLink: " +
                    $"'{buttonObj.name}' has no Button component."
                );

                continue;
            }


            // -------------------------------------------------
            // BUTTON CLICK
            // -------------------------------------------------

            button.onClick.AddListener(
                () =>
                {
                    if (documentViewerManager == null)
                    {
                        Debug.LogError(
                            "SupportingDocumentsPageLink: " +
                            "DocumentViewerManager reference " +
                            "is missing."
                        );

                        return;
                    }


                    // -------------------------------------------------
                    // TOGGLE
                    //
                    // Closed document:
                    //     OPEN
                    //
                    // Already open:
                    //     CLOSE
                    //
                    // Other documents remain untouched.
                    // -------------------------------------------------

                    documentViewerManager.ToggleDocument(
                        capturedName
                    );


                    // -------------------------------------------------
                    // Update button state.
                    // -------------------------------------------------

                    RefreshButtons();
                }
            );


            spawnedButtons.Add(
                buttonObj
            );
        }
    }
}