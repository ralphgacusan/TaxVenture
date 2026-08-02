using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TaxCodeClickableValue
{
    public string displayText;     // UI text
    public string enumValue;       // Internal enum name (only used for enums)

    public DataValueType type;
    public string semanticKey;
}

[Serializable]
public class TaxCodeSection
{
    public string heading;
    [TextArea(4, 20)]
    public string body;
    public List<TaxCodeClickableValue> clickableValues = new List<TaxCodeClickableValue>();
}