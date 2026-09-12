using System;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// THE central mediator for the click-to-select -> click-to-place system.
/// Sources call SelectValue(); destinations call TryPlaceOnDestination().
/// Neither ever references the other directly.
///
/// RESPONSIBILITIES:
/// - Hold the currently selected DataValue + which source component produced it
/// - Broadcast selection-changed so sources can show highlight state
/// - Validate and perform transfers when a destination is clicked
/// - On success: clear selection
/// - On failure: KEEP selection active (player can try a different destination)
///
/// CONNECTS WITH:
/// - ValueClickSource: calls SelectValue()
/// - ValueClickDestination: calls TryPlaceOnDestination()
/// - ValueSelectionHighlighter: subscribes to OnSelectionChanged
/// - ValueTransferValidationPopup: subscribes to OnTransferFailed
/// </summary>
public class ValueTransferManager : MonoBehaviour
{
    public static ValueTransferManager Instance { get; private set; }

    public event Action<object> OnSelectionChanged;
    public event Action<DataValue> OnTransferSucceeded;
    public event Action<DataValue, string> OnTransferFailed;

    public DataValue SelectedValue { get; private set; }
    public object SelectedSourceComponent { get; private set; }
    public bool HasSelection => SelectedValue != null;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        Instance = this;
    }


    // =========================================================
    // SELECT VALUE
    // =========================================================

    /// <summary>
    /// Called by a source's UI wrapper when clicked.
    /// Clicking the SAME already-selected source again deselects it.
    /// </summary>
    public void SelectValue(
        IDataValueSource source,
        object sourceComponent
    )
    {
        if (SelectedSourceComponent == sourceComponent)
        {
            ClearSelection();
            return;
        }

        SelectedValue = source.GetDataValue();
        SelectedSourceComponent = sourceComponent;

        Debug.Log(
            $"Selected: {SelectedValue.DisplayText} | " +
            $"Key: {SelectedValue.SemanticKey} | " +
            $"Type: {SelectedValue.Type}"
        );


        // -----------------------------------------------------
        // Play selection SFX.
        // -----------------------------------------------------

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMouseClickSelectSFX();
        }


        OnSelectionChanged?.Invoke(
            SelectedSourceComponent
        );
    }


    // =========================================================
    // CLEAR SELECTION
    // =========================================================

    public void ClearSelection()
    {
        SelectedValue = null;
        SelectedSourceComponent = null;

        OnSelectionChanged?.Invoke(null);
    }


    // =========================================================
    // PLACE VALUE
    // =========================================================

    /// <summary>
    /// Called by a destination's UI wrapper when clicked.
    /// </summary>
    public void TryPlaceOnDestination(
        IDataValueDestination destination
    )
    {
        if (!HasSelection)
        {
            Debug.Log(
                "TryPlaceOnDestination called but no value is selected."
            );

            return;
        }

        if (destination == null)
        {
            Debug.LogError(
                "Destination is NULL!"
            );

            return;
        }


        Debug.Log(
            $"Trying to place {SelectedValue.DisplayText} " +
            $"into {destination.GetType().Name}"
        );


        // =====================================================
        // VALIDATE DESTINATION
        // =====================================================

        bool canAccept =
            destination.CanAccept(
                SelectedValue
            );

        Debug.Log(
            $"CanAccept: {canAccept}"
        );


        if (!canAccept)
        {
            Debug.Log(
                $"Rejected: {SelectedValue.SemanticKey} " +
                $"({SelectedValue.Type})"
            );

            OnTransferFailed?.Invoke(
                SelectedValue,
                BuildRejectionReason(
                    SelectedValue
                )
            );

            return;
        }


        // =====================================================
        // PERFORM TRANSFER
        // =====================================================

        bool accepted =
            destination.TryReceiveValue(
                SelectedValue
            );

        Debug.Log(
            $"TryReceiveValue returned: {accepted}"
        );


        // =====================================================
        // SUCCESS
        // =====================================================

        if (accepted)
        {
            Debug.Log(
                "Transfer successful!"
            );


            // -------------------------------------------------
            // Play successful placement SFX.
            // -------------------------------------------------

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySelectSFX();
            }


            OnTransferSucceeded?.Invoke(
                SelectedValue
            );

            ClearSelection();

            return;
        }


        // =====================================================
        // DESTINATION REFUSED
        // =====================================================

        Debug.Log(
            "Destination refused value after CanAccept."
        );

        OnTransferFailed?.Invoke(
            SelectedValue,
            "This value could not be placed here."
        );
    }


    // =========================================================
    // REJECTION REASON
    // =========================================================

    private string BuildRejectionReason(
        DataValue value
    )
    {
        return value.Type switch
        {
            DataValueType.Number =>
                "This field only accepts numeric values.",

            DataValueType.Text =>
                "This field only accepts text values.",

            DataValueType.Enum =>
                "This field only accepts a matching category.",

            _ =>
                "Value cannot be placed here."
        };
    }
}