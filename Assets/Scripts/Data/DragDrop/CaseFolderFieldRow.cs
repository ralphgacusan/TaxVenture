using UnityEngine;
using UnityEngine.UI;
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

    /// <summary>
    /// T08: the case folder is Marie's approved Client Copy, read-only. While
    /// set, no field accepts, gives or changes a value (reading is unchanged).
    /// </summary>
    public static bool ReadOnly;

    /// <summary>Read-only now: the Client Copy (ReadOnly), or this folder opened away from the desk (2026-10-26).</summary>
    private bool Locked => ReadOnly || Doc3DLayer.BlocksValueTransfer(transform);

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

            DebugTrace.Log(
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

            DebugTrace.Log(
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

        DebugTrace.Log(
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

        DebugTrace.Log(
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

            DebugTrace.Log(
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
        // The previous attempt no longer describes this refreshed value.
        typeFeedback?.ForceRestore();

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

        bool isBlank =
            !hasValue ||
            currentValue == null;

        // One line when it fits; otherwise the caption on top and the value
        // on its own line below (never wrapped mid-value).
        PaperInkStyle.SetFieldText(labelText, label, displayValue, isBlank);

        // Printed-form presentation; data and drop behaviour are unchanged.
        Image rowImage = GetComponent<Image>();
        if (rowImage == null)
            rowImage = GetComponentInChildren<Image>(true);
        PaperInkStyle.StyleFieldRow(rowImage, labelText, isDestination, highlighter, typeFeedback);
    }

    // ---------------------------------------------------------
    // Click
    // ---------------------------------------------------------

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (Locked) return;
        AudioManager.Instance?.QueueInteractionSFX("paper_place");

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

            DebugTrace.Log(
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
                AudioManager.Instance?.PlayEffect("sfx_ui_unavailable_01", 0.5f);

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
            DebugTrace.Log(
                $"[CaseFolder] {label} has no value to select."
            );

            return;
        }

        DebugTrace.Log(
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
        if (Locked) return;
        DebugTrace.Log(
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
            DebugTrace.Log(
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
            DebugTrace.Log(
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
            DebugTrace.Log(
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

        DebugTrace.Log(
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

        DebugTrace.Log(
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
    // Read-only accessors (level guide, drop indicator)
    // ---------------------------------------------------------

    /// <summary>True when this field can receive values.</summary>
    public bool IsDestination => isDestination;

    /// <summary>True when the field already holds a value.</summary>
    public bool HasValue => hasValue && currentValue != null;

    /// <summary>Same rule as CanAccept, without logging (safe to call every frame).</summary>
    public bool WouldAccept(DataValue value)
    {
        return !Locked && isDestination && value != null && value.Type == type &&
               string.Equals(value.SemanticKey?.Trim(), semanticKey?.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------
    // Destination Validation
    // ---------------------------------------------------------

    public bool CanAccept(
        DataValue value)
    {
        if (Locked || !isDestination)
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

        DebugTrace.Log(
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

        DebugTrace.Log(
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

        DebugTrace.Log(
            $"[CaseFolder Receive] " +
            $"Successfully received {value.DisplayText} " +
            $"into {label}"
        );

        return true;
    }
}
