using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Represents ONE individual document viewer window.
///
/// Every opened document gets its own instance of this component.
///
/// Therefore multiple documents can stay open simultaneously.
///
/// Example:
///
/// DocumentViewerWindow #1
///     -> Invoice
///
/// DocumentViewerWindow #2
///     -> Receipt
///
/// DocumentViewerWindow #3
///     -> Tax Form
///
/// Each instance has its own:
/// - document name
/// - field rows
/// - drag position
/// - FloatingWindow sorting order
/// </summary>
public class DocumentViewerWindow : MonoBehaviour
{
    // =========================================================
    // PANEL
    // =========================================================

    [Header("Panel")]

    [SerializeField]
    private GameObject viewerPanelRoot;


    // =========================================================
    // CONTENT
    // =========================================================

    [Header("Content")]

    [SerializeField]
    private TextMeshProUGUI documentNameText;

    [SerializeField]
    private Transform fieldRowListRoot;

    [SerializeField]
    private DocumentFieldRow fieldRowPrefab;


    // =========================================================
    // REFRESH TARGETS
    // =========================================================

    [Header("Refresh Targets (optional)")]

    [SerializeField]
    private SupportingDocumentsPageLink folderPageLink;

    [SerializeField]
    private CorkboardDocumentSpawner corkboardSpawner;


    // =========================================================
    // DRAG
    // =========================================================

    [Header("Drag")]

    [SerializeField]
    private DocumentWindowDrag windowDrag;


    // =========================================================
    // FLOATING WINDOW
    // =========================================================

    [Header("Floating Window")]

    [Tooltip(
        "FloatingWindow attached to this document viewer. " +
        "This controls the layer/sorting order."
    )]
    [SerializeField]
    private FloatingWindow floatingWindow;


    // =========================================================
    // INTERNAL
    // =========================================================

    private readonly List<DocumentFieldRow> spawnedRows =
        new();


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (viewerPanelRoot != null)
        {
            viewerPanelRoot.SetActive(false);
        }
    }


    // =========================================================
    // SHOW DOCUMENT
    // =========================================================

    /// <summary>
    /// Displays a document inside THIS viewer instance.
    ///
    /// This does NOT affect any other DocumentViewerWindow.
    /// </summary>
    public void Show(string documentName)
    {
        // -----------------------------------------------------
        // Get document data.
        // -----------------------------------------------------

        DocumentFieldData data =
            DocumentDataProvider.GetFieldsFor(
                documentName
            );


        // -----------------------------------------------------
        // Update document name.
        // -----------------------------------------------------

        if (documentNameText != null)
        {
            documentNameText.text =
                data.DocumentName;
        }


        // -----------------------------------------------------
        // Clear previous rows.
        //
        // This only clears rows belonging to THIS window.
        // -----------------------------------------------------

        ClearRows();


        // -----------------------------------------------------
        // Create field rows.
        // -----------------------------------------------------

        if (fieldRowPrefab != null &&
            fieldRowListRoot != null)
        {
            foreach (var field in data.Fields)
            {
                DocumentFieldRow row =
                    Instantiate(
                        fieldRowPrefab,
                        fieldRowListRoot
                    );


                row.Initialize(field);


                spawnedRows.Add(row);
            }
        }


        // -----------------------------------------------------
        // Mark document as reviewed.
        // -----------------------------------------------------

        MarkAsReviewed(documentName);


        // -----------------------------------------------------
        // Show this viewer.
        // -----------------------------------------------------

        if (viewerPanelRoot != null)
        {
            viewerPanelRoot.SetActive(true);
        }


        gameObject.SetActive(true);


        // -----------------------------------------------------
        // Reset position.
        // -----------------------------------------------------

        windowDrag?.ResetWindowPosition();


        // -----------------------------------------------------
        // Bring THIS document to front.
        //
        // The FloatingWindow component should increase its
        // global sorting order.
        // -----------------------------------------------------

        if (floatingWindow != null)
        {
            floatingWindow.OpenWindow();
        }
        else
        {
            Debug.LogWarning(
                "[DocumentViewerWindow] " +
                $"{gameObject.name}: FloatingWindow " +
                "is not assigned."
            );
        }


        // -----------------------------------------------------
        // Tutorial interaction.
        // -----------------------------------------------------

        if (TutorialController.Instance != null)
        {
            TutorialController.Instance.ReportInteraction(
                "open_document"
            );
        }


        Debug.Log(
            $"[DocumentViewerWindow] " +
            $"Opened: {documentName}"
        );
    }


    // =========================================================
    // HIDE
    // =========================================================

    public void Hide()
    {
        if (viewerPanelRoot != null)
        {
            viewerPanelRoot.SetActive(false);
        }


        folderPageLink?.RefreshButtons();

        corkboardSpawner?.RefreshAllDocumentVisuals();
    }


    // =========================================================
    // CLOSE
    // =========================================================

    public void Close()
    {
        folderPageLink?.RefreshButtons();

        corkboardSpawner?.RefreshAllDocumentVisuals();


        Destroy(gameObject);
    }


    // =========================================================
    // CLEAR ROWS
    // =========================================================

    private void ClearRows()
    {
        foreach (var row in spawnedRows)
        {
            if (row != null)
            {
                Destroy(row.gameObject);
            }
        }


        spawnedRows.Clear();
    }


    // =========================================================
    // MARK AS REVIEWED
    // =========================================================

    private void MarkAsReviewed(
        string documentName
    )
    {
        if (CaseManager.Instance == null)
            return;


        if (CaseManager.Instance.CurrentCase == null)
            return;


        var doc =
            CaseManager.Instance
                .CurrentCase
                .supportingDocuments
                .Find(
                    d =>
                        d.documentName ==
                        documentName
                );


        if (doc != null)
        {
            doc.isReviewed = true;
        }
    }
}