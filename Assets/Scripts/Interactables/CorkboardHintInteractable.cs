using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// The physical "hint notes" object pinned to the Corkboard. Clicking it
/// shows a small selection of vague investigator-style hints — same
/// "physical object -> small read-only popup" pattern as FindingsPaper
/// (Milestone 11) and Auditor's summary popup.
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class CorkboardHintInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject hintPopupPanel;
    [SerializeField] private TextMeshProUGUI hintText;

    private HighlightEffect highlight;
    private bool isShowing = false;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
        hintPopupPanel.SetActive(false);
    }

    public void OnFocus() => highlight.Highlight();
    public void OnUnfocus() => highlight.Unhighlight();

    public void OnInteract()
    {
        isShowing = !isShowing;

        if (isShowing)
        {
            var hints = CorkboardHintProvider.GetHints();
            hintText.text = string.Join("\n\n", hints.ConvertAll(h => $"\u2022 {h}"));
        }

        hintPopupPanel.SetActive(isShowing);
    }

    public string GetPromptText() => "Click to view investigation notes";
}