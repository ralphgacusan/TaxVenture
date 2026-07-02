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
public class HighlightEffect : MonoBehaviour
{
    [Header("Highlight Materials")]
    [Tooltip("Material applied when the object is focused/highlighted.")]
    [SerializeField] private Material highlightMaterial;

    private Renderer[] renderers;
    private Material[][] originalMaterials;

    private void Awake()
    {
        // Finds every renderer on this object and all of its children.
        renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning($"[{nameof(HighlightEffect)}] No Renderer found on '{name}' or its children.");
            return;
        }

        originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;
        }
    }

    public void Highlight()
    {
        if (highlightMaterial == null) return;

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = new Material[renderer.materials.Length];

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = highlightMaterial;
            }

            renderer.materials = materials;
        }
    }

    public void Unhighlight()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].materials = originalMaterials[i];
        }
    }
}