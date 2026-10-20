using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PURPOSE:
/// Reusable destination wrapper for DataValue transfers.
///
/// Supports:
/// - Click-to-place
/// - Drag-and-drop placement
///
/// This component does NOT perform the actual transfer.
///
/// Sequence:
/// 1. Detect interaction
/// 2. Get selected DataValue
/// 3. Ask destination.CanAccept()
/// 4. Call existing ValueTransferManager transfer logic
/// 5. Show type/key feedback after the field has refreshed
///
/// The actual transfer system is NOT replaced or modified.
/// </summary>
public class ValueClickDestination :
    MonoBehaviour,
    IPointerClickHandler,
    IDropHandler
{
    // ---------------------------------------------------------
    // Destination
    // ---------------------------------------------------------

    private IDataValueDestination destination;

    // ---------------------------------------------------------
    // Visual Feedback
    // ---------------------------------------------------------

    private ValueTypeValidationFeedback typeFeedback;


    // ---------------------------------------------------------
    // Awake
    // ---------------------------------------------------------

    private void Awake()
    {
        typeFeedback =
            GetComponent<ValueTypeValidationFeedback>();

        DebugTrace.Log(
            $"[ValueClickDestination] Awake on " +
            $"{gameObject.name} | " +
            $"Feedback={(typeFeedback != null ? "FOUND" : "NOT FOUND")}"
        );
    }


    // ---------------------------------------------------------
    // Initialize Destination
    // ---------------------------------------------------------

    public void Initialize(
        IDataValueDestination valueDestination)
    {
        destination = valueDestination;

        if (typeFeedback == null)
        {
            typeFeedback =
                GetComponent<ValueTypeValidationFeedback>();
        }

        DebugTrace.Log(
            $"[ValueClickDestination] Initialized | " +
            $"Object={gameObject.name} | " +
            $"Destination={destination?.GetType().Name} | " +
            $"Feedback={(typeFeedback != null ? "READY" : "MISSING")}"
        );
    }


    // ---------------------------------------------------------
    // Initialize Validation Feedback
    // ---------------------------------------------------------

    /// <summary>
    /// Initializes the reusable visual feedback component.
    ///
    /// This is called by the actual destination because the
    /// destination knows what DataValueType and SemanticKey
    /// it expects.
    /// </summary>
    public void InitializeFeedback(
        DataValueType expectedType,
        string expectedKey)
    {
        if (typeFeedback == null)
        {
            typeFeedback =
                GetComponent<ValueTypeValidationFeedback>();
        }

        if (typeFeedback == null)
        {
            typeFeedback =
                gameObject.AddComponent<ValueTypeValidationFeedback>();

            DebugTrace.Log(
                $"[ValueClickDestination] Added " +
                $"ValueTypeValidationFeedback to " +
                $"{gameObject.name}"
            );
        }

        typeFeedback.Initialize(
            expectedType,
            expectedKey
        );

        DebugTrace.Log(
            $"[ValueClickDestination] Feedback initialized | " +
            $"Object={gameObject.name} | " +
            $"Expected Type={expectedType} | " +
            $"Expected Key={expectedKey}"
        );
    }


    // ---------------------------------------------------------
    // Click
    // ---------------------------------------------------------

    public void OnPointerClick(
        PointerEventData eventData)
    {
        DebugTrace.Log(
            $"[ValueClickDestination] Clicked: " +
            $"{gameObject.name}"
        );

        TryPlaceValue();
    }


    // ---------------------------------------------------------
    // Drag and Drop
    // ---------------------------------------------------------

    public void OnDrop(
        PointerEventData eventData)
    {
        DebugTrace.Log(
            $"[ValueClickDestination] Dropped on: " +
            $"{gameObject.name}"
        );

        TryPlaceValue();
    }


    // ---------------------------------------------------------
    // Shared Transfer Logic
    // ---------------------------------------------------------

    private void TryPlaceValue()
    {
        // -----------------------------------------------------
        // Destination check
        // -----------------------------------------------------

        if (destination == null)
        {
            Debug.LogError(
                $"[ValueClickDestination] " +
                $"{gameObject.name} has NO destination!"
            );

            return;
        }


        // -----------------------------------------------------
        // Manager check
        // -----------------------------------------------------

        if (ValueTransferManager.Instance == null)
        {
            Debug.LogError(
                "[ValueClickDestination] " +
                "ValueTransferManager.Instance is NULL!"
            );

            return;
        }


        ValueTransferManager manager =
            ValueTransferManager.Instance;


        // -----------------------------------------------------
        // Selection check
        // -----------------------------------------------------

        if (!manager.HasSelection)
        {
            DebugTrace.Log(
                "[ValueClickDestination] " +
                "No selected value."
            );

            return;
        }


        // -----------------------------------------------------
        // Get selected value
        // -----------------------------------------------------

        DataValue selectedValue =
            manager.SelectedValue;

        if (selectedValue == null)
        {
            Debug.LogError(
                "[ValueClickDestination] " +
                "SelectedValue is NULL!"
            );

            return;
        }


        DebugTrace.Log(
            $"[ValueClickDestination] Attempting placement | " +
            $"Value={selectedValue.DisplayText} | " +
            $"Type={selectedValue.Type} | " +
            $"Key={selectedValue.SemanticKey} | " +
            $"Destination={destination.GetType().Name}"
        );


        // =====================================================
        // VALIDATE FIRST
        // =====================================================

        bool accepted =
            destination.CanAccept(
                selectedValue
            );


        DebugTrace.Log(
            $"[ValueClickDestination] CanAccept = {accepted}"
        );


        // Transfer first: receive/refresh may clear the previous presentation.
        // This remains the only transfer call for this interaction.
        manager.TryPlaceOnDestination(destination);

        // Feedback describes category compatibility, never answer correctness.
        if (typeFeedback != null)
            typeFeedback.ShowFeedback(selectedValue);
    }
}
