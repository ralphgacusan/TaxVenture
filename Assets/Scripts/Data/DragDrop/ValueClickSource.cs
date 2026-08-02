using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PURPOSE:
/// Reusable "click this element to select its value" wrapper. Attach to
/// any UI element displaying a DataValue; call Initialize() once with the
/// owning IDataValueSource. Handles the click and delegates to
/// ValueTransferManager — the element itself never touches the manager
/// directly beyond this.
/// </summary>
public class ValueClickSource : MonoBehaviour, IPointerClickHandler
{
    private IDataValueSource source;
    private object ownerComponent;
    private ValueSelectionHighlighter highlighter;

    public void Initialize(IDataValueSource valueSource, object owner)
    {
        source = valueSource;
        ownerComponent = owner;

        highlighter = GetComponent<ValueSelectionHighlighter>();
        highlighter?.SetOwner(owner);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Clicked {gameObject.name}");
        ValueTransferManager.Instance.SelectValue(source, ownerComponent);
    }
}