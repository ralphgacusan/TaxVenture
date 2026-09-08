using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles dragging a DataValue from a source.
///
/// Supports both:
/// - Existing click-to-click selection
/// - Physical drag-and-drop
///
/// Important behavior:
/// - If the source is NOT selected, starting a drag selects it.
/// - If the source is ALREADY selected, starting a drag keeps the
///   existing selection instead of toggling it off.
/// - The shared DragVisualManager displays the dragged value.
/// - ValueTransferManager remains responsible for selection and transfer.
///
/// Visual-only behavior:
/// - DocumentFieldRow shows only the field value in the floating
///   drag visual.
/// - Other sources continue using DataValue.DisplayText.
/// </summary>
public class ValueDragSource : MonoBehaviour,
    IPointerDownHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private IDataValueSource source;
    private object ownerComponent;

    private bool isDragging = false;

    // ---------------------------------------------------------
    // Initialization
    // ---------------------------------------------------------

    public void Initialize(
        IDataValueSource valueSource,
        object owner
    )
    {
        source = valueSource;
        ownerComponent = owner;

        Debug.Log(
            $"ValueDragSource initialized on {gameObject.name} " +
            $"with source: {source?.GetType().Name}"
        );
    }

    // ---------------------------------------------------------
    // Pointer Down
    // ---------------------------------------------------------

    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        Debug.Log(
            $"Pointer down on {gameObject.name}"
        );
    }

    // ---------------------------------------------------------
    // Begin Drag
    // ---------------------------------------------------------

    public void OnBeginDrag(
        PointerEventData eventData
    )
    {
        // -----------------------------------------------------
        // Validate Source
        // -----------------------------------------------------

        if (source == null)
        {
            Debug.LogError(
                $"ValueDragSource on {gameObject.name} " +
                "has NO source!"
            );

            return;
        }

        // -----------------------------------------------------
        // Validate Transfer Manager
        // -----------------------------------------------------

        if (ValueTransferManager.Instance == null)
        {
            Debug.LogError(
                "ValueTransferManager.Instance is NULL!"
            );

            return;
        }

        // -----------------------------------------------------
        // Validate Drag Visual Manager
        // -----------------------------------------------------

        if (DragVisualManager.Instance == null)
        {
            Debug.LogError(
                "DragVisualManager.Instance is NULL!"
            );

            return;
        }

        ValueTransferManager manager =
            ValueTransferManager.Instance;

        // -----------------------------------------------------
        // IMPORTANT:
        // Do NOT toggle off an already-selected source.
        // -----------------------------------------------------

        if (!manager.HasSelection ||
            manager.SelectedSourceComponent != ownerComponent)
        {
            Debug.Log(
                $"[{gameObject.name}] " +
                "Source is not currently selected. " +
                "Selecting it for drag."
            );

            manager.SelectValue(
                source,
                ownerComponent
            );
        }
        else
        {
            Debug.Log(
                $"[{gameObject.name}] " +
                "Source is already selected. " +
                "Keeping existing selection for drag."
            );
        }

        // -----------------------------------------------------
        // Get Selected Value
        // -----------------------------------------------------

        DataValue selectedValue =
            manager.SelectedValue;

        if (selectedValue == null)
        {
            Debug.LogError(
                $"[{gameObject.name}] " +
                "Selected value is NULL!"
            );

            isDragging = false;

            return;
        }

        // -----------------------------------------------------
        // Start Dragging
        // -----------------------------------------------------

        isDragging = true;

        // -----------------------------------------------------
        // Determine Floating Drag Visual Text
        // -----------------------------------------------------
        //
        // DEFAULT:
        // Other sources continue using:
        //
        //     selectedValue.DisplayText
        //
        // DOCUMENT:
        // DocumentFieldRow uses only:
        //
        //     field.DisplayValue
        //
        // This changes ONLY the floating visual.
        // The actual DataValue remains unchanged.
        // -----------------------------------------------------

        string dragDisplayText =
            selectedValue.DisplayText;

        DocumentFieldRow documentRow =
            ownerComponent as DocumentFieldRow;

        if (documentRow != null)
        {
            dragDisplayText =
                documentRow.GetDragDisplayValue();
        }

        // -----------------------------------------------------
        // Show Floating Drag Visual
        // -----------------------------------------------------

        DragVisualManager.Instance.Show(
            dragDisplayText
        );

        // -----------------------------------------------------
        // Position Visual Under Cursor
        // -----------------------------------------------------

        DragVisualManager.Instance.UpdatePosition(
            eventData.position
        );

        Debug.Log(
            $"Started dragging: " +
            $"{dragDisplayText}"
        );
    }

    // ---------------------------------------------------------
    // Drag
    // ---------------------------------------------------------

    public void OnDrag(
        PointerEventData eventData
    )
    {
        if (!isDragging)
            return;

        if (DragVisualManager.Instance == null)
            return;

        DragVisualManager.Instance.UpdatePosition(
            eventData.position
        );

        Debug.Log(
            $"Dragging {gameObject.name} | " +
            $"Position: {eventData.position}"
        );
    }

    // ---------------------------------------------------------
    // End Drag
    // ---------------------------------------------------------

    public void OnEndDrag(
        PointerEventData eventData
    )
    {
        if (!isDragging)
            return;

        isDragging = false;

        // -----------------------------------------------------
        // Hide Floating Visual
        // -----------------------------------------------------

        if (DragVisualManager.Instance != null)
        {
            DragVisualManager.Instance.Hide();
        }

        /*
         * IMPORTANT:
         *
         * Do NOT clear the ValueTransferManager selection here.
         *
         * The destination's OnDrop() will call:
         *
         *     TryPlaceOnDestination()
         *
         * and that method will clear the selection ONLY when
         * the transfer succeeds.
         */

        Debug.Log(
            $"Stopped dragging {gameObject.name}"
        );
    }
}