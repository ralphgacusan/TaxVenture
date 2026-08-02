using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Generic "am I currently selected" visual for any source element —
/// listens to ValueTransferManager.OnSelectionChanged and tints itself.
/// </summary>
public class ValueSelectionHighlighter : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.3f);

    private object ownerComponent;

    public void SetOwner(object owner)
    {
        ownerComponent = owner;
    }

    private void OnEnable()
    {
        ValueTransferManager.Instance.OnSelectionChanged += HandleSelectionChanged;
    }

    private void OnDisable()
    {
        if (ValueTransferManager.Instance != null)
            ValueTransferManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
    }

    private void HandleSelectionChanged(object selectedComponent)
    {
        if (targetImage == null) return;
        targetImage.color = (selectedComponent != null && selectedComponent == ownerComponent) ? selectedColor : normalColor;
    }
}