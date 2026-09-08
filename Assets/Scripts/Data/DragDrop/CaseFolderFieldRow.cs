using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Represents one field inside the Tax Code / Case Folder.
///
/// Supports:
/// - Click-to-select as a DataValue source
/// - Click-to-place when this field is a destination
/// - Drag-and-drop placement when this field is a destination
///
/// The same GameObject can act as BOTH a source and destination.
///
/// Visual feedback:
/// - GREEN = correct DataValueType AND SemanticKey
/// - RED   = wrong DataValueType OR SemanticKey
///
/// Actual validation still uses:
/// - DataValueType
/// - SemanticKey
///
/// through the existing ValueTransferManager system.
/// </summary>
public class CaseFolderFieldRow :
    MonoBehaviour,
    IDataValueSource,
    IDataValueDestination,
    IPointerClickHandler,
    IDropHandler
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private ValueSelectionHighlighter highlighter;

    // ---------------------------------------------------------
    // Components
    // ---------------------------------------------------------

    private ValueTypeValidationFeedback typeFeedback;
    private ValueDragSource dragSource;

    // ---------------------------------------------------------
    // Field Data
    // ---------------------------------------------------------

    private string label;
    private object currentValue;
    private bool hasValue;
    private DataValueType type;
    private string semanticKey;

    private System.Action<object> onValueReceived;

    // True when this field can receive a value.
    private bool isDestination;

    // ---------------------------------------------------------
    // Awake
    // ---------------------------------------------------------

    private void Awake()
    {
        // -----------------------------------------------------
        // ValueDragSource
        // -----------------------------------------------------

        dragSource =
            GetComponent<ValueDragSource>();

        if (dragSource == null)
        {
            dragSource =
                gameObject.AddComponent<ValueDragSource>();

            Debug.Log(
                $"[CaseFolder] Added ValueDragSource automatically " +
                $"to {gameObject.name}"
            );
        }

        dragSource.Initialize(
            this,
            this
        );

        // -----------------------------------------------------
        // ValueTypeValidationFeedback
        // -----------------------------------------------------

        typeFeedback =
            GetComponent<ValueTypeValidationFeedback>();

        if (typeFeedback == null)
        {
            typeFeedback =
                gameObject.AddComponent<ValueTypeValidationFeedback>();

            Debug.Log(
                $"[CaseFolder] Added ValueTypeValidationFeedback " +
                $"automatically to {gameObject.name}"
            );
        }
    }

    // ---------------------------------------------------------
    // Initialization
    // ---------------------------------------------------------

    public void Initialize(
        string fieldLabel,
        object value,
        bool valueKnown,
        DataValueType valueType,
        string key,
        System.Action<object> receiveCallback)
    {
        label = fieldLabel;
        currentValue = value;
        hasValue = valueKnown;
        type = valueType;
        semanticKey = key;
        onValueReceived = receiveCallback;

        isDestination =
            receiveCallback != null;

        // -----------------------------------------------------
        // Type Feedback
        // -----------------------------------------------------

        EnsureTypeFeedback();

        typeFeedback.Initialize(
            type,
            semanticKey
        );

        // -----------------------------------------------------
        // Refresh Display
        // -----------------------------------------------------

        Refresh();

        highlighter?.SetOwner(this);

        // -----------------------------------------------------
        // Drag Source
        // -----------------------------------------------------

        EnsureDragSource();

        dragSource.Initialize(
            this,
            this
        );

        Debug.Log(
            $"[CaseFolder] Initialized: {label} | " +
            $"Value: {currentValue} | " +
            $"Known: {hasValue} | " +
            $"Destination: {isDestination} | " +
            $"Expected Type: {type} | " +
            $"Expected Key: {semanticKey} | " +
            $"Drag Source: READY"
        );
    }

    // ---------------------------------------------------------
    // Source Only Initialization
    // ---------------------------------------------------------

    public void InitializeSourceOnly(
        string fieldLabel,
        object value,
        DataValueType valueType,
        string key)
    {
        label = fieldLabel;
        currentValue = value;
        hasValue = true;
        type = valueType;
        semanticKey = key;

        onValueReceived = null;
        isDestination = false;

        // -----------------------------------------------------
        // Type Feedback
        // -----------------------------------------------------

        EnsureTypeFeedback();

        typeFeedback.Initialize(
            type,
            semanticKey
        );

        // -----------------------------------------------------
        // Refresh
        // -----------------------------------------------------

        Refresh();

        highlighter?.SetOwner(this);

        // -----------------------------------------------------
        // Drag Source
        // -----------------------------------------------------

        EnsureDragSource();

        dragSource.Initialize(
            this,
            this
        );

        Debug.Log(
            $"[CaseFolder] Initialized SOURCE ONLY: " +
            $"{label} | " +
            $"Type: {type} | " +
            $"Key: {semanticKey} | " +
            $"Drag Source: READY"
        );
    }

    // ---------------------------------------------------------
    // Ensure Type Feedback
    // ---------------------------------------------------------

    private void EnsureTypeFeedback()
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
                $"[CaseFolder] Added ValueTypeValidationFeedback " +
                $"automatically to {gameObject.name}"
            );
        }
    }

    // ---------------------------------------------------------
    // Ensure Drag Source
    // ---------------------------------------------------------

    private void EnsureDragSource()
    {
        if (dragSource == null)
        {
            dragSource =
                GetComponent<ValueDragSource>();
        }

        if (dragSource == null)
        {
            dragSource =
                gameObject.AddComponent<ValueDragSource>();
        }
    }

    // ---------------------------------------------------------
    // Refresh Display
    // ---------------------------------------------------------

    private void Refresh()
    {
        if (labelText == null)
        {
            Debug.LogError(
                $"[CaseFolder] Label Text is NULL on " +
                $"{gameObject.name}!"
            );

            return;
        }

        string displayValue;

        // -----------------------------------------------------
        // Unknown / Empty
        // -----------------------------------------------------

        if (!hasValue ||
            currentValue == null)
        {
            displayValue = "?";
        }

        // -----------------------------------------------------
        // Number
        // -----------------------------------------------------

        else if (type == DataValueType.Number)
        {
            displayValue =
                $"\u20b1{System.Convert.ToSingle(currentValue):N0}";
        }

        // -----------------------------------------------------
        // Text
        // -----------------------------------------------------

        else if (type == DataValueType.Text)
        {
            displayValue =
                currentValue.ToString();
        }

        // -----------------------------------------------------
        // Enum
        // -----------------------------------------------------

        else if (type == DataValueType.Enum)
        {
            string rawValue =
                currentValue.ToString();

            displayValue =
                EnumDisplayFormatter.Format(rawValue);

            // Visual-only Case Folder formatting
            if (semanticKey == "ResidencyStatus" ||
                semanticKey == "Residency Status")
            {
                if (rawValue.Equals(
                    "Alien",
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    displayValue =
                        "Resident Alien";
                }
            }
        }

        // -----------------------------------------------------
        // Fallback
        // -----------------------------------------------------

        else
        {
            displayValue =
                currentValue.ToString();
        }

        // -----------------------------------------------------
        // Update Label
        // -----------------------------------------------------

        labelText.text =
            $"{label}: {displayValue}";
    }

    // ---------------------------------------------------------
    // Click
    // ---------------------------------------------------------

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (ValueTransferManager.Instance == null)
        {
            Debug.LogError(
                "[CaseFolder] ValueTransferManager.Instance " +
                "is NULL!"
            );

            return;
        }

        ValueTransferManager manager =
            ValueTransferManager.Instance;

        // -----------------------------------------------------
        // Destination Click
        // -----------------------------------------------------

        if (isDestination &&
            manager.HasSelection)
        {
            DataValue selectedValue =
                manager.SelectedValue;

            if (selectedValue == null)
                return;

            Debug.Log(
                $"[CaseFolder Click] " +
                $"Attempting to place {selectedValue.DisplayText} " +
                $"into {label}"
            );

            // -------------------------------------------------
            // Check actual validation first
            // -------------------------------------------------

            bool accepted =
                CanAccept(selectedValue);

            if (!accepted)
            {
                Debug.LogWarning(
                    $"[CaseFolder Click] INVALID | " +
                    $"Destination={label} | " +
                    $"Expected Type={type} | " +
                    $"Expected Key={semanticKey} | " +
                    $"Incoming Type={selectedValue.Type} | " +
                    $"Incoming Key={selectedValue.SemanticKey}"
                );

                // Show RED feedback.
                EnsureTypeFeedback();

                typeFeedback.ShowFeedback(
                    selectedValue
                );

                return;
            }

            // -------------------------------------------------
            // Perform transfer
            // -------------------------------------------------

            manager.TryPlaceOnDestination(
                this
            );

            // -------------------------------------------------
            // Show GREEN AFTER transfer
            // -------------------------------------------------
            //
            // This is important.
            //
            // TryReceiveValue() calls Refresh(), which changes
            // the field from "?" to the actual value.
            //
            // We apply the feedback AFTER that operation so
            // the final visual state is GREEN.
            // -------------------------------------------------

            EnsureTypeFeedback();

            typeFeedback.ShowFeedback(
                selectedValue
            );

            return;
        }

        // -----------------------------------------------------
        // Source Click
        // -----------------------------------------------------

        if (!hasValue)
        {
            Debug.Log(
                $"[CaseFolder] {label} has no value to select."
            );

            return;
        }

        Debug.Log(
            $"[CaseFolder Source] " +
            $"Field: {label} | " +
            $"Value: {labelText.text}"
        );

        manager.SelectValue(
            this,
            this
        );
    }

    // ---------------------------------------------------------
    // Drag and Drop
    // ---------------------------------------------------------

    public void OnDrop(
        PointerEventData eventData)
    {
        Debug.Log(
            $"[CaseFolder Drop] " +
            $"Dropped value on: {gameObject.name} " +
            $"({label})"
        );

        if (ValueTransferManager.Instance == null)
        {
            Debug.LogError(
                "[CaseFolder Drop] " +
                "ValueTransferManager.Instance is NULL!"
            );

            return;
        }

        ValueTransferManager manager =
            ValueTransferManager.Instance;

        // -----------------------------------------------------
        // Check Destination
        // -----------------------------------------------------

        if (!isDestination)
        {
            Debug.Log(
                $"[CaseFolder Drop] " +
                $"{label} is NOT a destination."
            );

            return;
        }

        // -----------------------------------------------------
        // Check Selection
        // -----------------------------------------------------

        if (!manager.HasSelection)
        {
            Debug.Log(
                "[CaseFolder Drop] " +
                "No selected value."
            );

            return;
        }

        DataValue selectedValue =
            manager.SelectedValue;

        if (selectedValue == null)
        {
            Debug.LogError(
                "[CaseFolder Drop] " +
                "SelectedValue is NULL!"
            );

            return;
        }

        // -----------------------------------------------------
        // Prevent dropping onto itself
        // -----------------------------------------------------

        if (ReferenceEquals(
            manager.SelectedSourceComponent,
            this))
        {
            Debug.Log(
                $"[CaseFolder Drop] " +
                $"Cannot drop {label} onto itself."
            );

            return;
        }

        // -----------------------------------------------------
        // Actual validation
        // -----------------------------------------------------

        bool accepted =
            CanAccept(selectedValue);

        // -----------------------------------------------------
        // INVALID
        // -----------------------------------------------------

        if (!accepted)
        {
            Debug.LogWarning(
                $"[CaseFolder Drop] INVALID | " +
                $"Cannot accept {selectedValue.DisplayText} " +
                $"into {label}. " +
                $"Expected Type: {type}, " +
                $"Expected Key: {semanticKey} | " +
                $"Incoming Type: {selectedValue.Type}, " +
                $"Incoming Key: {selectedValue.SemanticKey}"
            );

            EnsureTypeFeedback();

            // RED
            typeFeedback.ShowFeedback(
                selectedValue
            );

            return;
        }

        // -----------------------------------------------------
        // VALID
        // -----------------------------------------------------

        Debug.Log(
            $"[CaseFolder Drop] VALID | " +
            $"Attempting to place: " +
            $"{selectedValue.DisplayText} " +
            $"into {label}"
        );

        // -----------------------------------------------------
        // Perform transfer FIRST
        // -----------------------------------------------------

        manager.TryPlaceOnDestination(
            this
        );

        // -----------------------------------------------------
        // Show GREEN AFTER transfer
        // -----------------------------------------------------
        //
        // This is the important change.
        //
        // TryReceiveValue() calls Refresh().
        //
        // Refresh() changes:
        //
        //     Residency Status: ?
        //
        // into:
        //
        //     Residency Status: Resident Citizen
        //
        // After that has happened, we apply GREEN.
        // -----------------------------------------------------

        EnsureTypeFeedback();

        typeFeedback.ShowFeedback(
            selectedValue
        );

        Debug.Log(
            $"[CaseFolder Drop] " +
            $"GREEN feedback requested AFTER placement."
        );
    }

    // ---------------------------------------------------------
    // DataValue Source
    // ---------------------------------------------------------

    public DataValue GetDataValue()
    {
        string displayValue;

        if (!hasValue ||
            currentValue == null)
        {
            displayValue = "?";
        }
        else if (type == DataValueType.Number)
        {
            displayValue =
                $"\u20b1{System.Convert.ToSingle(currentValue):N0}";
        }
        else if (type == DataValueType.Text)
        {
            displayValue =
                currentValue.ToString();
        }
        else if (type == DataValueType.Enum)
        {
            string rawValue =
                currentValue.ToString();

            displayValue =
                EnumDisplayFormatter.Format(rawValue);

            // Visual-only formatting
            if (semanticKey == "ResidencyStatus" ||
                semanticKey == "Residency Status")
            {
                if (rawValue.Equals(
                    "Alien",
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    displayValue =
                        "Resident Alien";
                }
            }
        }
        else
        {
            displayValue =
                currentValue.ToString();
        }

        return new DataValue(
            currentValue,
            displayValue,
            type,
            semanticKey
        );
    }

    // ---------------------------------------------------------
    // Destination Validation
    // ---------------------------------------------------------

    public bool CanAccept(
        DataValue value)
    {
        if (!isDestination)
            return false;

        if (value == null)
            return false;

        bool typeMatches =
            value.Type == type;

        bool keyMatches =
            string.Equals(
                value.SemanticKey?.Trim(),
                semanticKey?.Trim(),
                System.StringComparison.OrdinalIgnoreCase
            );

        Debug.Log(
            $"[CaseFolder CanAccept] " +
            $"{label} | " +
            $"Type: {value.Type} == {type} -> {typeMatches} | " +
            $"Key: '{value.SemanticKey}' == '{semanticKey}' -> {keyMatches}"
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
            return false;

        Debug.Log(
            $"[CaseFolder Receive] " +
            $"Field: {label} | " +
            $"Incoming Value: {value.DisplayText}"
        );

        currentValue =
            value.Value;

        hasValue = true;

        // -----------------------------------------------------
        // Update the text
        // -----------------------------------------------------

        Refresh();

        // -----------------------------------------------------
        // Notify CaseFolderUI / CaseData
        // -----------------------------------------------------

        onValueReceived?.Invoke(
            value.Value
        );

        Debug.Log(
            $"[CaseFolder Receive] " +
            $"Successfully received {value.DisplayText} " +
            $"into {label}"
        );

        return true;
    }
}