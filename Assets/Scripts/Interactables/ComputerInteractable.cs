using UnityEngine;

/// <summary>
/// PURPOSE:

///
/// PER DESIGN DOC:

///
/// CONNECTS WITH:

/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class ComputerInteractable : MonoBehaviour, IInteractable
{
    // [SerializeField] private ComputerUI computerUI;

    [SerializeField] private ComputerHomeUI computerHomeUI;

    private HighlightEffect highlight;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
    }

    public void OnFocus() => highlight.Highlight();
    public void OnUnfocus()
    {
        highlight.Unhighlight();
    }
    public void OnInteract()
    {
        if (ClientCopyHandover.CaseActionsBlocked) return;
        // Debug: Log a message to indicate that the Computer has been clicked
        Debug.Log("Computer clicked! Implement the logic to open the Computer UI here.");
        computerHomeUI.Show();
    }

    public string GetPromptText() => "Click to open Computer";
}
