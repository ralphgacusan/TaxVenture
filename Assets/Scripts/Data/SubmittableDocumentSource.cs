using UnityEngine;

/// <summary>
/// PURPOSE:
/// Represents one of the two submittable items (Case Folder, Tax Return)
/// as an IDataValueSource for the R8 transfer system — the player selects
/// it, then places it onto the Auditor's submission destination. Reuses
/// the exact same select/place mechanism as every other value transfer in
/// the game, just carrying a symbolic "this document" value instead of a
/// number/text field.
/// </summary>
public class SubmittableDocumentSource : MonoBehaviour, IDataValueSource
{
    public enum DocumentKind { CaseFolder, TaxReturn }

    [SerializeField] private DocumentKind kind;
    [SerializeField] private ValueClickSource clickSource;

    private void Awake()
    {
        clickSource.Initialize(this, this);
    }

    public DataValue GetDataValue()
    {
        return new DataValue(kind, kind.ToString(), DataValueType.Text, $"Submit_{kind}");
    }
}