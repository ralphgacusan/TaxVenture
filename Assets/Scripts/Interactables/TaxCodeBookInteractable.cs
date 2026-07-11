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
    [SerializeField] private TaxCodeBookUI taxCodeBookUI;
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
        taxCodeBookUI.Show();
    }

    public string GetPromptText() => "Click to open Tax Code Book";
}