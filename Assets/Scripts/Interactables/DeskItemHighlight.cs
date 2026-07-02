using UnityEngine;

/// <summary>
/// PURPOSE:
/// Keeps a workstation interactable (Folder, Computer, Tax Book, Stamps,
/// etc.) highlighted whenever workstation mode is active.
///
/// RESPONSIBILITIES:
/// - Turn HighlightEffect on when enabled
/// - Turn HighlightEffect off when disabled
///
/// CONNECTS WITH:
/// - HighlightEffect
/// - CameraController
/// </summary>
[RequireComponent(typeof(HighlightEffect))]
public class DeskItemHighlight : MonoBehaviour
{
    private HighlightEffect highlight;

    private void Awake()
    {
        highlight = GetComponent<HighlightEffect>();
    }

    public void ShowHighlight()
    {
        highlight.Highlight();
    }

    public void HideHighlight()
    {
        highlight.Unhighlight();
    }
}