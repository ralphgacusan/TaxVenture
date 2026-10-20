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

    private static ValueDragSource activeDrag;

    /// <summary>True while any value is being dragged.</summary>
    public static bool IsAnyDragActive => activeDrag != null && activeDrag.isDragging;

    // ---------------------------------------------------------
    // Initialization
    // ---------------------------------------------------------

    public void Initialize(
        IDataValueSource valueSource,
        object owner
    )
    {
        ValueHoverFeedback.Ensure(this);
        source = valueSource;
        ownerComponent = owner;

        DebugTrace.Log(
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
        DebugTrace.Log(
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
            DebugTrace.Log(
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
            DebugTrace.Log(
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

        // One shared floating label: a newer drag takes ownership.
        if (activeDrag != null && activeDrag != this)
            activeDrag.isDragging = false;
        isDragging = true;
        activeDrag = this;

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

        DebugTrace.Log(
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

        DebugTrace.Log(
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
        if (activeDrag == this)
            activeDrag = null;

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

        DebugTrace.Log(
            $"Stopped dragging {gameObject.name}"
        );
    }

    private void OnDisable()
    {
        // A disabled or destroyed source can no longer finish its drag.
        isDragging = false;
        if (activeDrag == this)
        {
            activeDrag = null;
            DragVisualManager.Instance?.Hide();
        }
    }
}