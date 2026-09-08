using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// A Tax Calculator input slot.
///
/// Pure destination:
/// - Gross Income
/// - Allowable Expenses
/// - Tax Credits
///
/// Uses the existing DataValue transfer system.
///
/// Visual feedback:
/// - GREEN = accepted value
/// - RED = rejected value
///
/// The temporary validation feedback is handled by
/// ValueTypeValidationFeedback.
/// </summary>
public class ComputerFieldSlot :
    MonoBehaviour,
    IDataValueDestination
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Image borderImage;

    [Header("Expected Data")]
    [SerializeField] private string expectedSemanticKey;

    [Header("Interaction")]
    [SerializeField] private ValueClickDestination clickDestination;

    // ---------------------------------------------------------
    // State
    // ---------------------------------------------------------

    public float CurrentValue { get; private set; }

    public bool IsFilled { get; private set; }


    // ---------------------------------------------------------
    // Awake
    // ---------------------------------------------------------

    private void Awake()
    {
        // -----------------------------------------------------
        // Make sure click destination exists
        // -----------------------------------------------------

        if (clickDestination == null)
        {
            clickDestination =
                GetComponent<ValueClickDestination>();
        }

        if (clickDestination == null)
        {
            Debug.LogError(
                $"[ComputerFieldSlot] " +
                $"No ValueClickDestination found on " +
                $"{gameObject.name}!"
            );

            return;
        }


        // -----------------------------------------------------
        // Initialize destination
        // -----------------------------------------------------

        clickDestination.Initialize(this);


        // -----------------------------------------------------
        // Initialize validation feedback
        // -----------------------------------------------------
        //
        // ComputerFieldSlot expects:
        //
        // Type = Number
        // Key  = expectedSemanticKey
        //
        // This gives ValueTypeValidationFeedback the
        // information it needs to determine whether the
        // incoming value is correct.
        // -----------------------------------------------------

        clickDestination.InitializeFeedback(
            DataValueType.Number,
            expectedSemanticKey
        );


        // -----------------------------------------------------
        // Initial state
        // -----------------------------------------------------

        Clear();


        Debug.Log(
            $"[ComputerFieldSlot] Initialized | " +
            $"Object={gameObject.name} | " +
            $"Expected Type=Number | " +
            $"Expected Key={expectedSemanticKey}"
        );
    }


    // ---------------------------------------------------------
    // Can Accept
    // ---------------------------------------------------------

    public bool CanAccept(
        DataValue value)
    {
        if (value == null)
        {
            Debug.LogWarning(
                $"[ComputerFieldSlot] " +
                $"{gameObject.name}: Incoming value is NULL."
            );

            return false;
        }


        bool typeMatches =
            value.Type == DataValueType.Number;

        bool keyMatches =
            string.Equals(
                value.SemanticKey?.Trim(),
                expectedSemanticKey?.Trim(),
                System.StringComparison.OrdinalIgnoreCase
            );


        Debug.Log(
            $"[ComputerFieldSlot] CanAccept | " +
            $"Object={gameObject.name} | " +
            $"Incoming Type={value.Type} | " +
            $"Expected Type=Number | " +
            $"Type Match={typeMatches} | " +
            $"Incoming Key='{value.SemanticKey}' | " +
            $"Expected Key='{expectedSemanticKey}' | " +
            $"Key Match={keyMatches}"
        );


        return
            typeMatches &&
            keyMatches;
    }


    // ---------------------------------------------------------
    // Receive Value
    // ---------------------------------------------------------

    public bool TryReceiveValue(
        DataValue value)
    {
        if (!CanAccept(value))
        {
            Debug.LogWarning(
                $"[ComputerFieldSlot] " +
                $"Rejected value on {gameObject.name}."
            );

            return false;
        }


        // -----------------------------------------------------
        // Store value
        // -----------------------------------------------------

        CurrentValue =
            value.AsFloat();

        IsFilled = true;


        // -----------------------------------------------------
        // Update text
        // -----------------------------------------------------

        if (valueText != null)
        {
            valueText.text =
                $"\u20b1{CurrentValue:N0}";
        }


        // -----------------------------------------------------
        // IMPORTANT:
        //
        // DO NOT set borderImage.color = Color.green here.
        //
        // ValueTypeValidationFeedback is responsible for the
        // temporary GREEN feedback.
        //
        // Otherwise both systems fight over the same Image.
        // -----------------------------------------------------

        Debug.Log(
            $"[ComputerFieldSlot] " +
            $"Value received successfully | " +
            $"Object={gameObject.name} | " +
            $"Value={CurrentValue}"
        );


        return true;
    }


    // ---------------------------------------------------------
    // Clear
    // ---------------------------------------------------------

    public void Clear()
    {
        CurrentValue = 0f;

        IsFilled = false;


        if (valueText != null)
        {
            valueText.text =
                "Click a value, then click here";
        }


        // -----------------------------------------------------
        // Restore normal appearance
        // -----------------------------------------------------

        if (borderImage != null)
        {
            borderImage.color =
                Color.white;
        }


        // -----------------------------------------------------
        // Also force feedback to stop if one is active.
        // -----------------------------------------------------

        ValueTypeValidationFeedback feedback =
            GetComponent<ValueTypeValidationFeedback>();

        if (feedback != null)
        {
            feedback.ForceRestore();
        }
    }
}