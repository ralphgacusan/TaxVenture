using UnityEngine;

/// <summary>
/// PURPOSE:

///
/// PER DESIGN DOC:

///
/// CONNECTS WITH:

/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class ClientInteractable : MonoBehaviour, IInteractable
{

    private HighlightEffect highlight;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
    }

    public void OnFocus() => highlight.Highlight();
    public void OnUnfocus() => highlight.Unhighlight();

    public void OnInteract()
    {
        // Debug: Log a message to indicate that the Client has been clicked
        Debug.Log("Client clicked! Implement the logic to open the Client UI here.");
    }

    public string GetPromptText() => "Click to open Client";
}