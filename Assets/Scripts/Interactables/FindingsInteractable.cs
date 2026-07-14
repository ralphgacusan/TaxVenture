using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Physical "sticky notes / printed report" object on the corkboard showing
/// Consultant Findings. Clicking it opens a small read-only popup listing
/// CaseData.potentialIssuesIdentified.
///
/// WHY NOT REUSE DocumentViewerUI DIRECTLY:
/// DocumentViewerUI's Show() takes a document name and pulls from
/// DocumentDataProvider, then marks a document reviewed — none of which
/// applies to findings (there's no "document" to mark reviewed, and the
/// content source is different: a List<string> on CaseData, not
/// DocumentDataProvider). Rather than overload DocumentViewerUI with
/// special-case branching for a fundamentally different content type, this
/// gets its own minimal, single-purpose read-only popup — same "static
/// panel, no animation" pattern as everything else, just scoped correctly.
///
/// CONNECTS WITH:
/// - HighlightEffect (same GameObject)
/// - FindingsPopupUI (new, small): the actual read-only display
/// - CaseManager.Instance.CurrentCase.potentialIssuesIdentified: content source
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class FindingsInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private FindingsPopupUI findingsPopupUI;

    private HighlightEffect highlight;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
    }

    public void OnFocus() => highlight.Highlight();
    public void OnUnfocus() => highlight.Unhighlight();

    public void OnInteract()
    {
        findingsPopupUI.Show();
    }

    public string GetPromptText() => "Click to read Consultant Findings";
}