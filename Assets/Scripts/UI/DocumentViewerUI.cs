using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DocumentViewerUI : MonoBehaviour
{
    // =========================================================
    // PANEL
    // =========================================================

    [Header("Panel")]

    [SerializeField] private GameObject viewerPanelRoot;


    // =========================================================
    // CONTENT
    // =========================================================

    [Header("Content")]

    [SerializeField] private TextMeshProUGUI documentNameText;

    [SerializeField] private Transform fieldRowListRoot;

    [SerializeField] private DocumentFieldRow fieldRowPrefab;


    // =========================================================
    // DRAG
    // =========================================================

    [Header("Drag")]

    [SerializeField] private FloatingWindow floatingWindow;


    // =========================================================
    // INTERNAL
    // =========================================================

    private readonly List<DocumentFieldRow> spawnedRows =
        new List<DocumentFieldRow>();


    // ---------------------------------------------------------
    // Stores the actual document represented by this viewer.
    //
    // This is important because the DocumentViewerManager
    // needs to know which document this specific window belongs
    // to.
    // ---------------------------------------------------------

    private string currentDocumentName;


    // ---------------------------------------------------------
    // Callback used by DocumentViewerManager.
    //
    // This tells the manager that this specific document
    // window has been closed.
    // ---------------------------------------------------------

    private Action onClosed;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // -----------------------------------------------------
        // Automatically find FloatingWindow if it was not
        // assigned manually.
        // -----------------------------------------------------

        if (floatingWindow == null)
        {
            floatingWindow =
                GetComponent<FloatingWindow>();
        }


        // -----------------------------------------------------
        // Hide viewer when instantiated.
        // -----------------------------------------------------

        if (viewerPanelRoot != null)
        {
            viewerPanelRoot.SetActive(false);
        }
    }


    // =========================================================
    // SHOW
    // =========================================================

    /// <summary>
    /// Displays the specified document inside this
    /// independent viewer window.
    /// </summary>
    public void Show(
        string documentName
    )
    {
        if (string.IsNullOrWhiteSpace(documentName))
        {
            Debug.LogError(
                $"[DocumentViewerUI] {name}: " +
                "Document name is empty!"
            );

            return;
        }


        if (viewerPanelRoot == null)
        {
            Debug.LogError(
                $"[DocumentViewerUI] {name}: " +
                "Viewer Panel Root is not assigned!"
            );

            return;
        }


        if (documentNameText == null)
        {
            Debug.LogError(
                $"[DocumentViewerUI] {name}: " +
                "Document Name Text is not assigned!"
            );

            return;
        }


        if (fieldRowListRoot == null)
        {
            Debug.LogError(
                $"[DocumentViewerUI] {name}: " +
                "Field Row List Root is not assigned!"
            );

            return;
        }


        if (fieldRowPrefab == null)
        {
            Debug.LogError(
                $"[DocumentViewerUI] {name}: " +
                "Field Row Prefab is not assigned!"
            );

            return;
        }


        // -----------------------------------------------------
        // STORE DOCUMENT NAME
        //
        // This allows the manager to identify exactly which
        // document this viewer represents.
        // -----------------------------------------------------

        currentDocumentName =
            documentName;


        // -----------------------------------------------------
        // GET DOCUMENT DATA
        // -----------------------------------------------------

        DocumentFieldData data =
            DocumentDataProvider.GetFieldsFor(
                documentName
            );


        // -----------------------------------------------------
        // SET DOCUMENT NAME
        // -----------------------------------------------------

        documentNameText.text =
            data.DocumentName;


        // -----------------------------------------------------
        // CLEAR OLD ROWS
        //
        // Only this viewer's rows are destroyed.
        // Other document windows remain completely independent.
        // -----------------------------------------------------

        ClearRows();


        // -----------------------------------------------------
        // CREATE FIELD ROWS
        // -----------------------------------------------------

        foreach (var field in data.Fields)
        {
            DocumentFieldRow row =
                Instantiate(
                    fieldRowPrefab,
                    fieldRowListRoot
                );


            if (row == null)
            {
                continue;
            }


            row.Initialize(field);

            spawnedRows.Add(row);
        }


        // -----------------------------------------------------
        // MARK DOCUMENT AS REVIEWED
        // -----------------------------------------------------

        MarkAsReviewed(
            documentName
        );


        // -----------------------------------------------------
        // SHOW PANEL
        // -----------------------------------------------------

        viewerPanelRoot.SetActive(true);


        // -----------------------------------------------------
        // OPEN FLOATING WINDOW
        // -----------------------------------------------------

        if (floatingWindow != null)
        {
            floatingWindow.OpenWindow();
        }
        else
        {
            Debug.LogWarning(
                $"[DocumentViewerUI] {name}: " +
                "FloatingWindow is not assigned."
            );
        }


        // -----------------------------------------------------
        // TUTORIAL
        // -----------------------------------------------------

        if (TutorialController.Instance != null)
        {
            TutorialController.Instance
                .ReportInteraction(
                    "open_document"
                );
        }


        Debug.Log(
            $"[DocumentViewerUI] " +
            $"Opened document: {documentName}"
        );
    }


    // =========================================================
    // GET DOCUMENT NAME
    // =========================================================

    /// <summary>
    /// Returns the document name currently represented
    /// by this viewer.
    ///
    /// Used by DocumentViewerManager to identify the
    /// document when the window is closed.
    /// </summary>
    public string GetDocumentName()
    {
        return currentDocumentName;
    }


    // =========================================================
    // SET CLOSE CALLBACK
    // =========================================================

    /// <summary>
    /// Registers a callback that is invoked when this
    /// document viewer is closed.
    /// </summary>
    public void SetCloseCallback(
        Action callback
    )
    {
        onClosed = callback;
    }


    // =========================================================
    // IS VIEWER OPEN
    // =========================================================

    /// <summary>
    /// Returns true if this document viewer is currently open.
    /// </summary>
    public bool IsViewerOpen()
    {
        return gameObject.activeSelf;
    }


    // =========================================================
    // CLOSE VIEWER
    // =========================================================

    /// <summary>
    /// Closes this document viewer.
    /// </summary>
    public void CloseViewer()
    {
        if (floatingWindow != null)
        {
            floatingWindow.CloseWindow();
        }
        else
        {
            gameObject.SetActive(false);
        }


        NotifyClosed();
    }


    // =========================================================
    // CLOSED BY FLOATING WINDOW
    // =========================================================

    /// <summary>
    /// Called when FloatingWindow closes itself, such as
    /// when the document is dragged back onto the Case Folder
    /// HUD icon.
    /// </summary>
    public void NotifyClosed()
    {
        if (onClosed == null)
        {
            return;
        }


        Action callback =
            onClosed;


        // -----------------------------------------------------
        // Clear callback BEFORE invoking it.
        //
        // This prevents accidental duplicate notifications.
        // -----------------------------------------------------

        onClosed = null;


        callback.Invoke();
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
                Destroy(
                    row.gameObject
                );
            }
        }


        spawnedRows.Clear();
    }


    // =========================================================
    // MARK REVIEWED
    // =========================================================

    private void MarkAsReviewed(
        string documentName
    )
    {
        if (CaseManager.Instance == null)
        {
            return;
        }


        if (CaseManager.Instance.CurrentCase == null)
        {
            return;
        }


        var doc =
            CaseManager.Instance
                .CurrentCase
                .supportingDocuments
                .Find(
                    d => d.documentName == documentName
                );


        if (doc != null)
        {
            doc.isReviewed = true;
        }
    }


    // =========================================================
    // ON DESTROY
    // =========================================================

    private void OnDestroy()
    {
        // -----------------------------------------------------
        // If the viewer is destroyed while still registered,
        // notify the manager so it does not retain a stale
        // reference.
        // -----------------------------------------------------

        NotifyClosed();
    }

    // =========================================================
    // SET RETURN ICON
    // =========================================================

    /// <summary>
    /// Assigns the Case Folder icon to this viewer's
    /// FloatingWindow at runtime.
    ///
    /// This is necessary because the DocumentViewerUI is
    /// a prefab and cannot directly reference a scene object.
    /// </summary>
    public void SetReturnIcon(RectTransform icon)
    {
        if (floatingWindow == null)
        {
            floatingWindow =
                GetComponent<FloatingWindow>();
        }

        if (floatingWindow == null)
        {
            Debug.LogError(
                $"[DocumentViewerUI] {name}: " +
                "FloatingWindow could not be found!"
            );

            return;
        }

        floatingWindow.SetReturnIcon(icon);

        Debug.Log(
            $"[DocumentViewerUI] {name}: " +
            "Case Folder return icon assigned."
        );
    }
}