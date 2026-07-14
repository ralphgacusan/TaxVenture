using UnityEngine;

/// <summary>
/// PURPOSE:
/// One of the two physical assessment cards pinned to the corkboard (Ready
/// For Filing / Not Ready For Filing). Clicking writes
/// CaseData.caseAssessment directly — same effect as the old UI buttons,
/// just triggered by a world object instead.
///
/// RESPONSIBILITIES:
/// - Know which CaseAssessment value THIS card represents (set in Inspector)
/// - On interact, write that value into CaseData.caseAssessment
/// - Visually reflect whether it is the CURRENTLY selected assessment,
///   by reading CaseData.caseAssessment live (never caches "am I selected")
///
/// CONNECTS WITH:
/// - HighlightEffect (same GameObject): focus highlight
/// - CaseManager.Instance.CurrentCase.caseAssessment: read AND write target
/// - CorkboardInteractable / spawner: calls RefreshVisual() on both cards
///   after either one is clicked, so the "deselected" card updates too
///   (see note in CorkboardInteractable below)
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class AssessmentCardInteractable : MonoBehaviour, IInteractable
{
    [Header("This Card's Value")]
    [SerializeField] private CaseAssessment representedAssessment;

    [Header("Selected Visual")]
    [SerializeField] private Renderer cardRenderer;
    [SerializeField] private Color unselectedColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    private HighlightEffect highlight;

    /// <summary>Fired whenever THIS card writes an assessment, so sibling cards can refresh too.</summary>
    public static System.Action OnAnyCardSelected;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
    }

    private void OnEnable()
    {
        OnAnyCardSelected += RefreshVisual;
        RefreshVisual();
    }

    private void OnDisable()
    {
        OnAnyCardSelected -= RefreshVisual;
    }

    public void OnFocus() => highlight.Highlight();
    public void OnUnfocus()
    {
        highlight.Unhighlight();
        RefreshVisual();
    }

    public void OnInteract()
    {
        CaseManager.Instance.CurrentCase.caseAssessment = representedAssessment;
        OnAnyCardSelected?.Invoke(); // notifies this card AND its sibling to refresh
    }

    public string GetPromptText() => $"Click to mark case as {representedAssessment}";

    public void RefreshVisual()
    {
        bool isSelected = CaseManager.Instance.CurrentCase.caseAssessment == representedAssessment;
        if (cardRenderer != null)
        {
            cardRenderer.material.color = isSelected ? selectedColor : unselectedColor;
        }
        Debug.Log(cardRenderer.material.color);
    }
}