using UnityEngine;

/// <summary>
/// PURPOSE:
/// Simple, reusable "highlight" visual for greybox prototyping. Swaps the
/// object's material to a highlight material on focus, and restores the
/// original material on unfocus.
///
/// WHY A SEPARATE COMPONENT (not baked into each interactable script):
/// Almost every interactable object needs the same highlight behavior. Rather
/// than duplicating highlight logic inside DeskInteractable, ComputerInteractable,
/// etc., each of those scripts simply calls Highlight() / Unhighlight() on this
/// component. Later, this can be swapped for a proper outline shader without
/// touching any interactable script.
///
/// CONNECTS WITH:
/// - Any *Interactable script (e.g. DeskInteractable) that has this component
///   on the same GameObject and calls Highlight()/Unhighlight() from its own
///   OnFocus()/OnUnfocus().
/// </summary>
[RequireComponent(typeof(Renderer))]
public class HighlightEffect : MonoBehaviour
{
    [Header("Highlight Materials")]
    [Tooltip("Material applied when the object is focused/highlighted.")]
    [SerializeField] private Material highlightMaterial;

    private Renderer objectRenderer;
    private Material originalMaterial;

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        originalMaterial = objectRenderer.material;
    }

    public void Highlight()
    {
        if (highlightMaterial != null)
        {
            objectRenderer.material = highlightMaterial;
        }
    }

    public void Unhighlight()
    {
        objectRenderer.material = originalMaterial;
    }
}