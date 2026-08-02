using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// One field within a supporting document, now carrying DataValueType and
/// SemanticKey so it can produce a real DataValue for the transfer system,
/// in addition to its display label/value.
/// </summary>
public class DocumentField
{
    public string Label;
    public string DisplayValue;
    public DataValueType Type;
    public string SemanticKey;

    public DocumentField(string label, string displayValue, DataValueType type, string semanticKey)
    {
        Label = label;
        DisplayValue = displayValue;
        Type = type;
        SemanticKey = semanticKey;
    }

    public DataValue ToDataValue()
    {
        object rawValue = Type == DataValueType.Number ? (object)ParseNumeric(DisplayValue) : DisplayValue;
        return new DataValue(rawValue, $"{Label}: {DisplayValue}", Type, SemanticKey);
    }

    private float ParseNumeric(string display)
    {
        string cleaned = display.Replace("\u20b1", "").Replace(",", "").Trim();
        float.TryParse(cleaned, out float result);
        return result;
    }
}

public class DocumentFieldData
{
    public string DocumentName;
    public List<DocumentField> Fields = new List<DocumentField>();

    public DocumentFieldData(string documentName)
    {
        DocumentName = documentName;
    }

    public DocumentFieldData AddField(string label, string displayValue, DataValueType type, string semanticKey)
    {
        Fields.Add(new DocumentField(label, displayValue, type, semanticKey));
        return this;
    }
}