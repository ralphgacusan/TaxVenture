using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class CaseFolderFieldRow : MonoBehaviour, IDataValueSource, IDataValueDestination, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private ValueSelectionHighlighter highlighter;

    private string label;
    private object currentValue;
    private bool hasValue;
    private DataValueType type;
    private string semanticKey;
    private System.Action<object> onValueReceived;
    private bool isDestination; // true only if this field can ever RECEIVE

    /// <summary>
    /// Full source+destination row (editable field, e.g. Residency Status).
    /// </summary>
    public void Initialize(string fieldLabel, object value, bool valueKnown, DataValueType valueType, string key, System.Action<object> receiveCallback)
    {
        label = fieldLabel;
        currentValue = value;
        hasValue = valueKnown;
        type = valueType;
        semanticKey = key;
        onValueReceived = receiveCallback;
        isDestination = receiveCallback != null;

        Refresh();
        highlighter?.SetOwner(this);
    }

    /// <summary>
    /// Source-only row (fixed fact, e.g. Full Name, TIN, Case Number).
    /// Always "known" (never shows "?"), never accepts a placement.
    /// </summary>
    public void InitializeSourceOnly(string fieldLabel, object value, DataValueType valueType, string key)
    {
        label = fieldLabel;
        currentValue = value;
        hasValue = true;
        type = valueType;
        semanticKey = key;
        onValueReceived = null;
        isDestination = false;

        Refresh();
        highlighter?.SetOwner(this);
    }

    private void Refresh()
    {
        string displayValue;
        if (!hasValue)
        {
            displayValue = "?";
        }
        else if (type == DataValueType.Number)
        {
            displayValue = $"\u20b1{System.Convert.ToSingle(currentValue):N0}";
        }
        else
        {
            displayValue = currentValue == null
                ? "?"
                : EnumDisplayFormatter.Format(currentValue.ToString());
        }

        labelText.text = $"{label}: {displayValue}";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var manager = ValueTransferManager.Instance;

        if (isDestination && manager.HasSelection && CanAccept(manager.SelectedValue))
        {
            manager.TryPlaceOnDestination(this);
            return;
        }

        if (!hasValue) return; // nothing to offer as a source

        Debug.Log($"[CaseFolder Source] Field: {label} | Value: {labelText.text}");
        manager.SelectValue(this, this);
    }

    public DataValue GetDataValue() => new DataValue(currentValue, labelText.text, type, semanticKey);

    public bool CanAccept(DataValue value)
    {
        return isDestination && value.Type == type && value.SemanticKey == semanticKey;
    }

    public bool TryReceiveValue(DataValue value)
    {
        if (!CanAccept(value)) return false;

        Debug.Log($"[CaseFolder Receive] Field: {label} | Incoming Value: {value.DisplayText}");

        currentValue = value.Value;
        hasValue = true;
        Refresh();
        onValueReceived?.Invoke(value.Value);
        return true;
    }
}