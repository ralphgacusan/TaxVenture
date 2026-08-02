using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PURPOSE:
/// A Tax Calculator input slot. Pure destination — receives Gross Income /
/// Allowable Expenses / Tax Credits via the value transfer system.
/// </summary>
public class ComputerFieldSlot : MonoBehaviour, IDataValueDestination
{
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Image borderImage;
    [SerializeField] private string expectedSemanticKey;
    [SerializeField] private ValueClickDestination clickDestination;

    public float CurrentValue { get; private set; }
    public bool IsFilled { get; private set; }

    private void Awake()
    {
        clickDestination.Initialize(this);
        Clear();
    }

    public bool CanAccept(DataValue value)
    {
        Debug.Log(
            $"ComputerFieldSlot Check:\n" +
            $"Incoming Type = {value.Type}\n" +
            $"Incoming Key = '{value.SemanticKey}'\n" +
            $"Expected Key = '{expectedSemanticKey}'"
        );

        return value.Type == DataValueType.Number
            && value.SemanticKey == expectedSemanticKey;
    }

    public bool TryReceiveValue(DataValue value)
    {
        if (!CanAccept(value)) return false;
        CurrentValue = value.AsFloat();
        IsFilled = true;
        valueText.text = $"\u20b1{CurrentValue:N0}";
        borderImage.color = Color.green;
        return true;
    }

    public void Clear()
    {
        CurrentValue = 0f;
        IsFilled = false;
        valueText.text = "Click a value, then click here";
        borderImage.color = Color.white;
    }
}