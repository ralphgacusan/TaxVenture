using UnityEngine;
using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// A Computer output field. Becomes selectable once a value has been
/// computed — pure source, dormant until SetValue() is called.
/// </summary>
public class ComputedResultSource : MonoBehaviour, IDataValueSource
{

    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private ValueClickSource clickSource;

    private float value;
    private string semanticKey;

    private void Awake()
    {
        clickSource.Initialize(this, this);
    }

    public void SetValue(float newValue, string key)
    {
        value = newValue;
        semanticKey = key;


        labelText.text = EnumDisplayFormatter.Format(key);
        valueText.text = $"₱{value:N0}";
    }

    public DataValue GetDataValue()
    {
        Debug.Log($"[GetDataValue] on instance ID = {GetInstanceID()}, value = {value}");

        return new DataValue(
            value,
            $"\u20b1{value:N0}",
            DataValueType.Number,
            semanticKey
        );
    }
}