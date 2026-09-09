using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages independent document viewer windows.
///
/// BEHAVIOR:
/// - Clicking a document button opens its document.
/// - Clicking the same document button again closes it.
/// - Multiple different documents can remain open at the same time.
/// - The same document can NEVER have duplicate windows.
/// - If a document is closed by dragging it to the Case Folder icon,
///   clicking its button again creates a new window.
///
/// IMPORTANT:
/// - This manager owns the document instances.
/// - FloatingWindow owns window movement/layering.
/// - Closing a FloatingWindow directly should notify this manager
///   so the dictionary stays synchronized.
/// </summary>
public class DocumentViewerManager : MonoBehaviour
{
    // =========================================================
    // PREFAB
    // =========================================================

    [Header("Document Viewer Prefab")]

    [Tooltip(
        "Prefab containing DocumentViewerUI and FloatingWindow."
    )]
    [SerializeField]
    private DocumentViewerUI documentViewerPrefab;


    // =========================================================
    // PARENT
    // =========================================================

    [Header("Parent")]

    [Tooltip(
        "Parent Canvas/Transform where document windows will be created."
    )]
    [SerializeField]
    private Transform documentWindowParent;

    // =========================================================
    // CASE FOLDER ICON
    // =========================================================

    [Header("Case Folder")]

    [Tooltip(
        "Drag the Case Folder Icon from the Hierarchy here. " +
        "This icon will automatically be assigned to every " +
        "DocumentViewer FloatingWindow."
    )]
    [SerializeField]
    private RectTransform caseFolderIcon;


    // =========================================================
    // OPEN DOCUMENTS
    // =========================================================

    /*
     * Stores the currently existing viewer for every
     * open document.
     *
     * Example:
     *
     * "Valid ID" -> Viewer A
     * "Payslip"  -> Viewer B
     * "TIN"      -> Viewer C
     *
     * This is the main duplicate-prevention system.
     */

    private readonly Dictionary<string, DocumentViewerUI>
        openDocuments =
            new Dictionary<string, DocumentViewerUI>();


    // =========================================================
    // TOGGLE DOCUMENT
    // =========================================================

    /// <summary>
    /// Opens the document if it is closed.
    ///
    /// Closes the document if it is already open.
    ///
    /// Different documents are not affected.
    /// </summary>
    public void ToggleDocument(string documentName)
    {
        if (string.IsNullOrWhiteSpace(documentName))
        {
            Debug.LogError(
                "[DocumentViewerManager] " +
                "Cannot toggle document because the name is empty!"
            );

            return;
        }


        // -----------------------------------------------------
        // Check whether this document is already registered.
        // -----------------------------------------------------

        if (openDocuments.TryGetValue(
                documentName,
                out DocumentViewerUI existingViewer))
        {
            // -------------------------------------------------
            // Unity destroyed-object check.
            // -------------------------------------------------

            if (existingViewer != null)
            {
                Debug.Log(
                    $"[DocumentViewerManager] " +
                    $"'{documentName}' is already open. " +
                    "Closing it."
                );


                CloseDocument(
                    documentName,
                    existingViewer
                );


                return;
            }


            // -------------------------------------------------
            // Stale dictionary entry.
            // -------------------------------------------------

            openDocuments.Remove(
                documentName
            );
        }


        // -----------------------------------------------------
        // Document is not open.
        //
        // Create it.
        // -----------------------------------------------------

        OpenDocument(
            documentName
        );
    }


    // =========================================================
    // OPEN DOCUMENT
    // =========================================================

    /// <summary>
    /// Creates and opens a document viewer.
    ///
    /// Includes a second duplicate check so that even if
    /// another script directly calls OpenDocument(), duplicate
    /// document windows cannot be created.
    /// </summary>
    public void OpenDocument(string documentName)
    {
        if (string.IsNullOrWhiteSpace(documentName))
        {
            Debug.LogError(
                "[DocumentViewerManager] " +
                "Cannot open document because the name is empty!"
            );

            return;
        }


        // -----------------------------------------------------
        // Check prefab.
        // -----------------------------------------------------

        if (documentViewerPrefab == null)
        {
            Debug.LogError(
                "[DocumentViewerManager] " +
                "Document Viewer Prefab is not assigned!"
            );

            return;
        }


        // -----------------------------------------------------
        // Check parent.
        // -----------------------------------------------------

        if (documentWindowParent == null)
        {
            Debug.LogError(
                "[DocumentViewerManager] " +
                "Document Window Parent is not assigned!"
            );

            return;
        }


        // -----------------------------------------------------
        // STRICT DUPLICATE PROTECTION
        // -----------------------------------------------------

        if (openDocuments.TryGetValue(
                documentName,
                out DocumentViewerUI existingViewer))
        {
            if (existingViewer != null)
            {
                Debug.LogWarning(
                    $"[DocumentViewerManager] " +
                    $"Duplicate prevented! " +
                    $"'{documentName}' is already open."
                );

                return;
            }


            // -------------------------------------------------
            // Remove stale reference.
            // -------------------------------------------------

            openDocuments.Remove(
                documentName
            );
        }


        // -----------------------------------------------------
        // CREATE VIEWER
        // -----------------------------------------------------

        DocumentViewerUI viewer =
            Instantiate(
                documentViewerPrefab,
                documentWindowParent
            );

        // -----------------------------------------------------
        // ASSIGN CASE FOLDER ICON
        //
        // The prefab cannot reference a scene object directly.
        // Therefore the Case Folder icon is injected at runtime.
        // -----------------------------------------------------

        if (caseFolderIcon != null)
        {
            viewer.SetReturnIcon(caseFolderIcon);
        }
        else
        {
            Debug.LogWarning(
                "[DocumentViewerManager] " +
                "Case Folder Icon is not assigned!"
            );
        }

        if (viewer == null)
        {
            Debug.LogError(
                "[DocumentViewerManager] " +
                "Failed to instantiate Document Viewer."
            );

            return;
        }


        // -----------------------------------------------------
        // REGISTER IMMEDIATELY
        //
        // Register BEFORE Show().
        //
        // This prevents duplicate creation if Show() or
        // another callback causes another open request.
        // -----------------------------------------------------

        openDocuments.Add(
            documentName,
            viewer
        );


        // -----------------------------------------------------
        // SHOW
        // -----------------------------------------------------

        viewer.Show(
            documentName
        );


        Debug.Log(
            $"[DocumentViewerManager] " +
            $"Opened document: {documentName}"
        );
    }


    // =========================================================
    // CLOSE DOCUMENT
    // =========================================================

    /// <summary>
    /// Completely closes and removes a document viewer.
    ///
    /// This destroys the viewer GameObject and removes it
    /// from the open-document dictionary.
    /// </summary>
    public void CloseDocument(
        string documentName,
        DocumentViewerUI viewer
    )
    {
        // -----------------------------------------------------
        // Remove ONLY if the dictionary currently points
        // to this exact viewer.
        //
        // This prevents an old viewer from accidentally
        // removing a newer viewer with the same document name.
        // -----------------------------------------------------

        if (openDocuments.TryGetValue(
                documentName,
                out DocumentViewerUI registeredViewer))
        {
            if (registeredViewer == viewer)
            {
                openDocuments.Remove(
                    documentName
                );
            }
        }


        // -----------------------------------------------------
        // Destroy viewer.
        // -----------------------------------------------------

        if (viewer != null)
        {
            Destroy(
                viewer.gameObject
            );
        }


        Debug.Log(
            $"[DocumentViewerManager] " +
            $"Closed document: {documentName}"
        );
    }


    // =========================================================
    // CLOSE DOCUMENT BY NAME
    // =========================================================

    /// <summary>
    /// Closes a specific document by name.
    /// </summary>
    public void CloseDocument(
        string documentName
    )
    {
        if (string.IsNullOrWhiteSpace(documentName))
        {
            return;
        }


        if (openDocuments.TryGetValue(
                documentName,
                out DocumentViewerUI viewer))
        {
            CloseDocument(
                documentName,
                viewer
            );

            return;
        }


        Debug.LogWarning(
            $"[DocumentViewerManager] " +
            $"Cannot close '{documentName}' because it is not open."
        );
    }


    // =========================================================
    // NOTIFY DOCUMENT CLOSED
    // =========================================================

    /// <summary>
    /// Called by a FloatingWindow when a document is closed
    /// directly, for example by dragging it onto the Case
    /// Folder icon.
    ///
    /// The viewer is used to identify which registered
    /// document was closed.
    /// </summary>
    public void NotifyDocumentClosed(
        DocumentViewerUI viewer
    )
    {
        if (viewer == null)
        {
            return;
        }


        string documentName =
            viewer.GetDocumentName();


        if (string.IsNullOrWhiteSpace(documentName))
        {
            return;
        }


        // -----------------------------------------------------
        // Only remove it if this exact viewer is registered.
        // -----------------------------------------------------

        if (openDocuments.TryGetValue(
                documentName,
                out DocumentViewerUI registeredViewer))
        {
            if (registeredViewer == viewer)
            {
                openDocuments.Remove(
                    documentName
                );


                Debug.Log(
                    $"[DocumentViewerManager] " +
                    $"Removed closed document: " +
                    $"{documentName}"
                );
            }
        }
    }


    // =========================================================
    // IS DOCUMENT OPEN
    // =========================================================

    /// <summary>
    /// Returns true when a document currently has an
    /// active viewer.
    /// </summary>
    public bool IsDocumentOpen(
        string documentName
    )
    {
        if (string.IsNullOrWhiteSpace(documentName))
        {
            return false;
        }


        if (!openDocuments.TryGetValue(
                documentName,
                out DocumentViewerUI viewer))
        {
            return false;
        }


        // -----------------------------------------------------
        // Remove stale reference.
        // -----------------------------------------------------

        if (viewer == null)
        {
            openDocuments.Remove(
                documentName
            );

            return false;
        }


        // -----------------------------------------------------
        // Also make sure the GameObject itself is active.
        // -----------------------------------------------------

        if (!viewer.gameObject.activeInHierarchy)
        {
            openDocuments.Remove(
                documentName
            );

            return false;
        }


        return true;
    }


    // =========================================================
    // GET DOCUMENT VIEWER
    // =========================================================

    /// <summary>
    /// Returns the currently registered viewer for a document.
    ///
    /// Returns null if the document is not open.
    /// </summary>
    public DocumentViewerUI GetDocumentViewer(
        string documentName
    )
    {
        if (string.IsNullOrWhiteSpace(documentName))
        {
            return null;
        }


        if (openDocuments.TryGetValue(
                documentName,
                out DocumentViewerUI viewer))
        {
            if (viewer != null &&
                viewer.gameObject.activeInHierarchy)
            {
                return viewer;
            }


            // -------------------------------------------------
            // Remove stale reference.
            // -------------------------------------------------

            openDocuments.Remove(
                documentName
            );
        }


        return null;
    }


    // =========================================================
    // CLOSE ALL
    // =========================================================

    /// <summary>
    /// Closes every currently open document.
    /// </summary>
    public void CloseAllDocuments()
    {
        // -----------------------------------------------------
        // Make a temporary list.
        //
        // This prevents dictionary modification problems
        // while destroying viewers.
        // -----------------------------------------------------

        List<DocumentViewerUI> viewersToClose =
            new List<DocumentViewerUI>(
                openDocuments.Values
            );


        // -----------------------------------------------------
        // Clear dictionary first.
        // -----------------------------------------------------

        openDocuments.Clear();


        // -----------------------------------------------------
        // Destroy all viewers.
        // -----------------------------------------------------

        foreach (DocumentViewerUI viewer
                 in viewersToClose)
        {
            if (viewer != null)
            {
                Destroy(
                    viewer.gameObject
                );
            }
        }


        Debug.Log(
            "[DocumentViewerManager] " +
            "All document windows closed."
        );
    }


    // =========================================================
    // DEBUG
    // =========================================================

    /// <summary>
    /// Returns the number of documents currently registered
    /// as open.
    /// </summary>
    public int GetOpenDocumentCount()
    {
        return openDocuments.Count;
    }
}