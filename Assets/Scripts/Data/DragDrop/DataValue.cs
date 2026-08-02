/// <summary>
/// PURPOSE:
/// THE universal transferable value object. Every piece of data that can
/// move between systems (income figures, TIN, taxpayer type, computed
/// results) is wrapped in one of these. Holds everything a destination
/// needs to validate and display it, without either side needing to know
/// the other's concrete type.
///
/// CONNECTS WITH:
/// - IDataValueSource: produces these
/// - IDataValueDestination: receives + validates these
/// - ValueTransferManager: holds the currently selected one
/// </summary>
public class DataValue
{
    public object Value;
    public string DisplayText;
    public DataValueType Type;

    /// <summary>
    /// Which CaseData field this value semantically represents (e.g.
    /// "GrossIncome", "TIN"). Lets a destination validate not just TYPE
    /// but WHICH specific field this is meant for.
    /// </summary>
    public string SemanticKey;

    public DataValue(object value, string displayText, DataValueType type, string semanticKey)
    {
        Value = value;
        DisplayText = displayText;
        Type = type;
        SemanticKey = semanticKey;
    }

    public float AsFloat() => Value is float f ? f : 0f;
    public string AsString() => Value?.ToString() ?? "";
}