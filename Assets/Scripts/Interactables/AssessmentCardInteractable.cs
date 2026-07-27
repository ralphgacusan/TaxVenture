using UnityEngine;

/// <summary>
/// PURPOSE:
/// READ-ONLY indicator card at the Corkboard showing whether this
/// assessment matches the case's CURRENT stamped status. No longer
/// writable — assessment decisions now happen exclusively at the Stamp
/// (see StampUI). This exists purely so the board visually reflects
/// whatever was stamped, without letting the player change it here.
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class AssessmentCardInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private CaseAssessment representedAssessment;
    [SerializeField] private Renderer cardRenderer;
    [SerializeField] private Color unselectedColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    private HighlightEffect highlight;

    public static System.Action OnAnyCardSelected;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
    }

    private void OnEnable()
    {
        OnAnyCardSelected += RefreshVisual;
    }

    private void Start()
    {
        RefreshVisual();
    }

    private void OnDisable()
    {
        OnAnyCardSelected -= RefreshVisual;
    }

    public void OnFocus() => highlight.Highlight();
    public void OnUnfocus() => highlight.Unhighlight();

    // No longer writes anything — purely informational now.
    public void OnInteract() { }

    public string GetPromptText() => $"{representedAssessment} (set at the Stamp)";

    public void RefreshVisual()
    {
        if (CaseManager.Instance == null || CaseManager.Instance.CurrentCase == null) return;

        bool isSelected = CaseManager.Instance.CurrentCase.caseAssessment == representedAssessment;
        if (cardRenderer != null)
        {
            cardRenderer.material.color = isSelected ? selectedColor : unselectedColor;
        }
    }
}