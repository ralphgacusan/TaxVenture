/// <summary>
/// PURPOSE:
/// Static provider of each supporting document's field content — SAME
/// public method signature as before (GetFieldsFor(string documentName)
/// returning DocumentFieldData), so DocumentViewerUI and every other
/// caller needs ZERO changes. Internally, the hardcoded switch is
/// replaced by a lookup into the currently active CaseManager
/// CaseDefinition's documents list.
/// </summary>
public static class DocumentDataProvider
{
    public static DocumentFieldData GetFieldsFor(string documentName)
    {
        CaseDefinition def = CaseManager.Instance.CurrentDefinition;

        DocumentDefinition docDef = def.documents.Find(d => d.documentName == documentName);

        if (docDef == null)
        {
            return new DocumentFieldData(documentName)
                .AddField("(No data available)", "", DataValueType.Text, "");
        }

        return CaseFactory.BuildDocumentFieldData(docDef);
    }
}