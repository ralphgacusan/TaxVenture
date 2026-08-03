using UnityEngine;
using TMPro;

[RequireComponent(typeof(HighlightEffect))]
public class PinnedDocumentInteractable : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    [SerializeField] private TextMeshPro labelText;
    [SerializeField] private Renderer paperRenderer;
    [SerializeField] private Color unreviewedColor = Color.white;
    [SerializeField] private Color reviewedColor = new Color(0.75f, 1f, 0.75f);

    [Header("Viewer")]
    [SerializeField] private DocumentViewerUI documentViewerUI;

    private string documentName;
    private HighlightEffect highlight;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
    }

    /// <summary>
    /// Called by CorkboardDocumentSpawner after spawning.
    /// </summary>
    public void Initialize(string docName)
    {
        documentName = docName;
        RefreshVisual();
    }

    /// <summary>
    /// Assigns the shared DocumentViewerUI.
    /// </summary>
    public void SetDocumentViewer(DocumentViewerUI viewer)
    {
        documentViewerUI = viewer;
    }

    public void OnFocus()
    {
        highlight.Highlight();
    }

    public void OnUnfocus()
    {
        highlight.Unhighlight();
    }

    public void OnInteract()
    {
        if (documentViewerUI == null)
        {
            Debug.LogError("PinnedDocumentInteractable: DocumentViewerUI is not assigned.");
            return;
        }

        documentViewerUI.Show(documentName);
    }

    public string GetPromptText()
    {
        return $"Click to inspect {documentName}";
    }

    public void RefreshVisual()
    {
        if (CaseManager.Instance == null || CaseManager.Instance.CurrentCase == null)
            return;

        var doc = CaseManager.Instance.CurrentCase.supportingDocuments
            .Find(d => d.documentName == documentName);

        bool isReviewed = doc != null && doc.isReviewed;

        if (labelText != null)
            labelText.text = isReviewed ? $"✓ {documentName}" : documentName;

        if (paperRenderer != null)
            paperRenderer.material.color = isReviewed ? reviewedColor : unreviewedColor;
    }
}