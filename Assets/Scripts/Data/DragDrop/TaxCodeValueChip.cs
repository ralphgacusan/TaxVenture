using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// One small selectable "chip" for a clickable value embedded in a Tax
/// Code Book section. Source-only — the Tax Code never receives values.
/// </summary>
public class TaxCodeValueChip : MonoBehaviour, IDataValueSource
{
    [SerializeField] private TextMeshProUGUI chipText;
    [SerializeField] private ValueClickSource clickSource;

    private TaxCodeClickableValue clickableValue;

    public void Initialize(TaxCodeClickableValue value)
    {
        clickableValue = value;
        chipText.text = value.displayText;
        clickSource.Initialize(this, this);
    }

    public DataValue GetDataValue()
    {
        object raw;

        if (clickableValue.type == DataValueType.Number)
        {
            raw = ParseNumeric(clickableValue.displayText);
        }
        else if (clickableValue.type == DataValueType.Enum)
        {
            raw = string.IsNullOrEmpty(clickableValue.enumValue)
                ? clickableValue.displayText
                : clickableValue.enumValue;
        }
        else
        {
            raw = clickableValue.displayText;
        }

        return new DataValue(
            raw,
            clickableValue.displayText,
            clickableValue.type,
            clickableValue.semanticKey);
    }

    private float ParseNumeric(string text)
    {
        float.TryParse(text.Replace(",", ""), out float result);
        return result;
    }
}