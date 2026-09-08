using UnityEngine;

/// <summary>
/// Creates document viewer windows.
///
/// Every time a document is opened, a NEW
/// DocumentViewerWindow is instantiated.
///
/// Existing windows remain open.
/// </summary>
public class DocumentViewerManager : MonoBehaviour
{
    // =========================================================
    // PREFAB
    // =========================================================

    [Header("Document Viewer Prefab")]

    [Tooltip(
        "The DocumentViewerWindow prefab that will be " +
        "created whenever a document is opened."
    )]
    [SerializeField]
    private DocumentViewerWindow documentViewerPrefab;


    // =========================================================
    // PARENT
    // =========================================================

    [Header("Viewer Parent")]

    [Tooltip(
        "The scene Canvas Transform where document " +
        "viewer windows will be created."
    )]
    [SerializeField]
    private Transform viewerParent;


    // =========================================================
    // OPEN DOCUMENT
    // =========================================================

    public void OpenDocument(
        string documentName
    )
    {
        if (documentViewerPrefab == null)
        {
            Debug.LogError(
                "[DocumentViewerManager] " +
                "Document Viewer Prefab is not assigned."
            );

            return;
        }


        if (viewerParent == null)
        {
            Debug.LogError(
                "[DocumentViewerManager] " +
                "Viewer Parent is not assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // Create a completely new window.
        // -----------------------------------------------------

        DocumentViewerWindow viewer =
            Instantiate(
                documentViewerPrefab,
                viewerParent
            );


        if (viewer == null)
        {
            Debug.LogError(
                "[DocumentViewerManager] " +
                "Failed to instantiate DocumentViewerWindow."
            );

            return;
        }


        // -----------------------------------------------------
        // Populate the new window.
        // -----------------------------------------------------

        viewer.Show(
            documentName
        );


        Debug.Log(
            $"[DocumentViewerManager] " +
            $"Created document window: {documentName}"
        );
    }
}