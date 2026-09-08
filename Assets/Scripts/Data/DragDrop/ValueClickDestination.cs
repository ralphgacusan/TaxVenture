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
/// 4. Show GREEN/RED feedback
/// 5. Call existing ValueTransferManager transfer logic
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

        Debug.Log(
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

        Debug.Log(
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

            Debug.Log(
                $"[ValueClickDestination] Added " +
                $"ValueTypeValidationFeedback to " +
                $"{gameObject.name}"
            );
        }

        typeFeedback.Initialize(
            expectedType,
            expectedKey
        );

        Debug.Log(
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
        Debug.Log(
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
        Debug.Log(
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
            Debug.Log(
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


        Debug.Log(
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


        Debug.Log(
            $"[ValueClickDestination] CanAccept = {accepted}"
        );


        // =====================================================
        // VISUAL FEEDBACK
        // =====================================================

        if (typeFeedback != null)
        {
            typeFeedback.ShowFeedback(
                accepted
            );

            Debug.Log(
                $"[ValueClickDestination] " +
                $"Feedback = " +
                $"{(accepted ? "GREEN" : "RED")}"
            );
        }
        else
        {
            Debug.LogWarning(
                $"[ValueClickDestination] " +
                $"No ValueTypeValidationFeedback on " +
                $"{gameObject.name}"
            );
        }


        // =====================================================
        // EXISTING TRANSFER SYSTEM
        // =====================================================
        //
        // IMPORTANT:
        // Do not remove this.
        //
        // We intentionally call the existing manager even
        // after the visual feedback.
        //
        // The manager remains responsible for actual transfer
        // validation and placement.
        // =====================================================

        manager.TryPlaceOnDestination(
            destination
        );
    }
}