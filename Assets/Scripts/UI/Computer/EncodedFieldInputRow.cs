using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// One labeled input row inside the BIR form encoding screen. Uses
/// TMP_InputField (not a custom keyboard), so this is already
/// mobile-compatible — Unity's TMP_InputField automatically opens the
/// device's native soft keyboard on Android/iOS with zero extra code, so no
/// desktop-specific input logic is hardcoded here.
/// </summary>
public class EncodedFieldInputRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TMP_InputField inputField;

    public EncodedFieldId FieldId { get; private set; }

    public void Initialize(EncodedFieldDefinition definition, string startingValue, System.Action<EncodedFieldId, string> onValueChanged)
    {
        FieldId = definition.Id;
        labelText.text = definition.Label;
        inputField.contentType = definition.IsNumeric ? TMP_InputField.ContentType.DecimalNumber : TMP_InputField.ContentType.Standard;
        inputField.text = startingValue;
        inputField.onValueChanged.AddListener(value => onValueChanged(FieldId, value));
    }

    public void SetReadOnly(bool readOnly)
    {
        inputField.readOnly = readOnly;
        inputField.interactable = !readOnly;
    }
}