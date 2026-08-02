using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// One field slot on the printable Tax Return form. Pure destination —
/// receives values via click-select/click-place, writes into
/// CaseData.encodedForm (kept separate from CaseData's authoritative
/// fields, so a player can still place the WRONG value here — the
/// Compliance Auditor is what catches that later, not this slot).
/// </summary>
public class TaxReturnFieldSlot : MonoBehaviour, IDataValueDestination
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private ValueClickDestination clickDestination;

    private EncodedFieldId fieldId;
    private bool isNumeric;
    private System.Action<EncodedFieldId, string> onValueSet;
    private bool isFilled = false;

    public bool IsFilled => isFilled;

    public void Initialize(EncodedFieldDefinition definition, System.Action<EncodedFieldId, string> onValueSetCallback)
    {
        fieldId = definition.Id;
        isNumeric = definition.IsNumeric;
        labelText.text = definition.Label;
        onValueSet = onValueSetCallback;
        Clear();
        clickDestination.Initialize(this);
    }

    public bool CanAccept(DataValue value)
    {
        DataValueType expected = isNumeric ? DataValueType.Number : DataValueType.Text;
        return value.Type == expected || (value.Type == DataValueType.Enum && !isNumeric);
    }

    public bool TryReceiveValue(DataValue value)
    {
        if (!CanAccept(value)) return false;
        string displayValue = value.Type == DataValueType.Number ? $"\u20b1{value.AsFloat():N0}" : value.AsString();
        valueText.text = displayValue;
        isFilled = true;
        onValueSet?.Invoke(fieldId, value.Type == DataValueType.Number ? value.AsFloat().ToString() : value.AsString());
        return true;
    }

    public void Clear()
    {
        valueText.text = "Click a value, then click here";
        isFilled = false;
    }
}