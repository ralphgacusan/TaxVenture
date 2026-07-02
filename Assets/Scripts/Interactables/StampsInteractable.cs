using UnityEngine;

/// <summary>
/// PURPOSE:

///
/// PER DESIGN DOC:

///
/// CONNECTS WITH:

/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class StampsInteractable : MonoBehaviour, IInteractable
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
            CameraController.CameraMode.FirstPerson)
            return;

        highlight.Unhighlight();
    }
    public void OnInteract()
    {
        // Debug: Log a message to indicate that the Stamps have been clicked
        Debug.Log("Stamps clicked! Implement the logic to open the Stamps UI here.");
    }

    public string GetPromptText() => "Click to open Stamps";
}