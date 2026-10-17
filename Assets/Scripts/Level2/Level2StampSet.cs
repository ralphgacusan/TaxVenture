using UnityEngine;

/// <summary>Tapping the desk stamp set in Level 2 (replaces StampSetInteractable there).</summary>
public sealed class Level2StampSet : MonoBehaviour, IInteractable
{
    public Level2DeskStamps Owner;
    private HighlightEffect highlight;

    private void Awake() => highlight = GetComponent<HighlightEffect>();

    public void OnFocus() { if (highlight != null) highlight.Highlight(); }

    public void OnUnfocus() { if (highlight != null) highlight.Unhighlight(); }

    public string GetPromptText() => Owner != null && Owner.Active ? "Put the stamps away" : "Use the stamps";

    public void OnInteract()
    {
        if (Owner != null) Owner.Toggle();
    }
}
