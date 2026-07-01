using UnityEngine;

/// <summary>
/// PURPOSE:
/// Example/placeholder implementation of IInteractable, attached to the Desk
/// object. For this milestone, it only highlights on focus and logs a message
/// on interact — this is purely to prove the interaction system end-to-end.
///
/// FUTURE (Milestone 3):
/// This script will be expanded (or replaced by a more complete version) to
/// trigger the third-person → first-person camera transition and open the
/// desk workstation view, per the "Use Computer & Tax Calculator" and
/// "Stamp Case Assessment" phases in the design document.
///
/// CONNECTS WITH:
/// - HighlightEffect (same GameObject) for visual feedback
/// - Interactor.cs (on Player) which calls these methods via the IInteractable interface
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class DeskInteractable : MonoBehaviour, IInteractable
{
    private HighlightEffect highlight;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
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
        // Placeholder behavior for this milestone only.
        Debug.Log("[DeskInteractable] Desk interacted with — camera/workstation logic arrives in Milestone 3.");
    }

    public string GetPromptText()
    {
        return "Click to interact with Desk";
    }
}