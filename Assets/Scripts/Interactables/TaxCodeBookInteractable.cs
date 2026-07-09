using UnityEngine;

/// <summary>
/// PURPOSE:

///
/// PER DESIGN DOC:

///
/// CONNECTS WITH:

/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class TaxCodeBookInteractable : MonoBehaviour, IInteractable
{

    private HighlightEffect highlight;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
    }

    public void OnFocus() => highlight.Highlight();
    public void OnUnfocus()
    {
        if (CameraController.Instance.CurrentMode ==
            CameraController.CameraMode.Workstation)
            return;

        highlight.Unhighlight();
    }
    public void OnInteract()
    {
        // Debug: Log a message to indicate that the Tax Code Book has been clicked
        Debug.Log("Tax Code Book clicked! Implement the logic to open the Tax Code Book UI here.");
    }

    public string GetPromptText() => "Click to open Tax Code Book";
}