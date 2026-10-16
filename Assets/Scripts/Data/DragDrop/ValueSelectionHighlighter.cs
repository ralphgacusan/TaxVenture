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
    private bool subscribed;
    private bool isSelected;

    public void SetOwner(object owner)
    {
        ownerComponent = owner;
        RefreshSelection();
    }

    /// <summary>
    /// Replaces the resting/selected tints (e.g. a restyled row) and
    /// re-applies the current state.
    /// </summary>
    public void SetColors(Color normal, Color selected)
    {
        normalColor = normal;
        selectedColor = selected;
        ApplyColor();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        // Handles dynamically-created UI objects where
        // ValueTransferManager may not exist yet during OnEnable().
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        isSelected = false;
        ApplyColor();
    }

    private void TrySubscribe()
    {
        if (subscribed)
            return;

        if (ValueTransferManager.Instance == null)
        {
            Debug.LogWarning(
                $"[ValueSelectionHighlighter] " +
                $"ValueTransferManager.Instance is NULL on {gameObject.name}. " +
                $"Will retry in Start()."
            );

            return;
        }

        ValueTransferManager.Instance.OnSelectionChanged += HandleSelectionChanged;
        subscribed = true;
        RefreshSelection();

        Debug.Log(
            $"[ValueSelectionHighlighter] " +
            $"Subscribed successfully: {gameObject.name}"
        );
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (ValueTransferManager.Instance != null)
        {
            ValueTransferManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
        }

        subscribed = false;
    }

    private void HandleSelectionChanged(object selectedComponent)
    {
        if (targetImage == null)
            return;

        isSelected =
            selectedComponent != null &&
            selectedComponent == ownerComponent;

        ApplyColor();
    }

    public Color RestingColor(Image image, Color fallback)
    {
        return image == targetImage ? (isSelected ? selectedColor : normalColor) : fallback;
    }

    private void RefreshSelection()
    {
        HandleSelectionChanged(ValueTransferManager.Instance != null
            ? ValueTransferManager.Instance.SelectedSourceComponent : null);
    }

    private void ApplyColor()
    {
        ValueTypeValidationFeedback feedback = GetComponent<ValueTypeValidationFeedback>();
        if (targetImage != null && (feedback == null || !feedback.IsPlaying))
            targetImage.color = isSelected ? selectedColor : normalColor;
    }

}