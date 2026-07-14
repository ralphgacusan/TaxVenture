using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// One physical pinned document on the corkboard. Stores only its own
/// document name; reviewed status is NOT cached here — it's read live from
/// CaseData every time the visual needs to refresh, so there is exactly one
/// source of truth (CaseData.supportingDocuments) and this script can never
/// drift out of sync with it.
///
/// RESPONSIBILITIES:
/// - Display the document's name (and a checkmark/color change if reviewed)
/// - On interact, open DocumentViewerUI for this document (reusing the
///   exact same viewer as the Folder's Page 6 and does not duplicate any
///   document-content logic)
/// - Refresh its own visual immediately after the viewer closes, since
///   opening it marks the document reviewed
///
/// CONNECTS WITH:
/// - CorkboardDocumentSpawner: calls Initialize(documentName) once, at spawn time
/// - HighlightEffect (same GameObject): focus highlight, same as every other interactable
/// - DocumentViewerUI: opened on interact; this script registers itself as
///   the refresh target so the checkmark updates on close
/// - CaseManager.Instance.CurrentCase.supportingDocuments: read-only lookup
///   for this document's current reviewed status
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class PinnedDocumentInteractable : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    [SerializeField] private TextMeshPro labelText; // world-space TMP, not TMP UGUI
    [SerializeField] private Renderer paperRenderer;
    [SerializeField] private Color unreviewedColor = Color.white;
    [SerializeField] private Color reviewedColor = new Color(0.75f, 1f, 0.75f); // pale green

    [Header("Viewer")]
    [SerializeField] private DocumentViewerUI documentViewerUI;

    private string documentName;
    private HighlightEffect highlight;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
    }

    /// <summary>
    /// Called once by CorkboardDocumentSpawner immediately after Instantiate.
    /// </summary>
    public void Initialize(string docName)
    {
        documentName = docName;
        RefreshVisual();
    }

    public void SetDocumentViewer(DocumentViewerUI viewer)
    {
        documentViewerUI = viewer;
    }

    public void OnFocus() => highlight.Highlight();
    public void OnUnfocus() => highlight.Unhighlight();

    public void OnInteract()
    {
        Debug.Log(documentViewerUI);

        documentViewerUI.Show(documentName);
        // DocumentViewerUI.Hide() (triggered by its X button) will call back
        // into RefreshVisual() via the corkboard spawner's refresh pass —
        // see the RefreshAllVisuals() note below for how this stays in sync
        // without PinnedDocumentInteractable needing a direct subscription.
    }

    public string GetPromptText() => $"Click to inspect {documentName}";

    /// <summary>
    /// Re-reads this document's reviewed status from CaseData and updates
    /// the label/color accordingly. Safe to call anytime — never caches
    /// reviewed state locally beyond this single read.
    /// </summary>
    public void RefreshVisual()
    {
        var doc = CaseManager.Instance.CurrentCase.supportingDocuments
            .Find(d => d.documentName == documentName);

        bool isReviewed = doc != null && doc.isReviewed;

        labelText.text = isReviewed ? $"\u2713 {documentName}" : documentName;
        if (paperRenderer != null)
        {
            paperRenderer.material.color = isReviewed ? reviewedColor : unreviewedColor;
        }
    }
}