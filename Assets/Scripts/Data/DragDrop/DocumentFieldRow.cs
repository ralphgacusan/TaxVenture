using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// One displayed, selectable field row inside DocumentViewerUI.
/// Source-only — documents never receive values.
/// </summary>
public class DocumentFieldRow : MonoBehaviour, IDataValueSource
{
    [SerializeField] private TextMeshProUGUI fieldText;
    [SerializeField] private ValueClickSource clickSource;

    private DocumentField field;

    public void Initialize(DocumentField documentField)
    {
        field = documentField;
        fieldText.text = $"{field.Label}: {field.DisplayValue}";
        clickSource.Initialize(this, this);
    }

    public DataValue GetDataValue() => field.ToDataValue();
}