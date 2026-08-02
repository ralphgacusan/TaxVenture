using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PURPOSE:
/// Reusable "click this element to place the currently selected value
/// here" wrapper. Attach to any UI element implementing IDataValueDestination.
/// </summary>
public class ValueClickDestination : MonoBehaviour, IPointerClickHandler
{
    private IDataValueDestination destination;

    public void Initialize(IDataValueDestination valueDestination)
    {
        destination = valueDestination;

        Debug.Log(
            $"ValueClickDestination initialized on {gameObject.name} " +
            $"with destination: {destination?.GetType().Name}"
        );
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log(
            $"Clicked destination: {gameObject.name}"
        );

        if (destination == null)
        {
            Debug.LogError(
                $"ValueClickDestination on {gameObject.name} has NO destination assigned!"
            );
            return;
        }

        if (ValueTransferManager.Instance == null)
        {
            Debug.LogError(
                "ValueTransferManager.Instance is NULL!"
            );
            return;
        }

        if (!ValueTransferManager.Instance.HasSelection)
        {
            Debug.Log(
                "No selected value. Nothing to place."
            );
            return;
        }

        Debug.Log(
            $"Attempting to place: " +
            $"{ValueTransferManager.Instance.SelectedValue.DisplayText} " +
            $"into {destination.GetType().Name}"
        );

        ValueTransferManager.Instance.TryPlaceOnDestination(destination);
    }
}