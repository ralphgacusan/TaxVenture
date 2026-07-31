using System.Collections.Generic;
using System.Text;

/// <summary>
/// PURPOSE:
/// Holds the specific field data for one supporting document (per the design
/// doc's Page 6 tables — e.g. BIR Form 2316 has Employer Name, Total
/// Compensation, Tax Withheld). Separate from CaseFolderPageContent because
/// each document type has different, named fields rather than one big body
/// of text — this keeps data structured for future use (e.g. Evidence Board
/// clue matching in Milestone 11).
///
/// CONNECTS WITH:
/// - DocumentViewerUI: displays a document's Fields as a simple label/value list
/// - CaseData / CaseManager: the source document list this data is generated from
/// </summary>
public class DocumentFieldData
{
    public string DocumentName;

    public List<(string label, string value)> Fields = new List<(string, string)>();

    public DocumentFieldData(string documentName)
    {
        DocumentName = documentName;
    }

    public DocumentFieldData AddField(string label, string value)
    {
        Fields.Add((label, value));
        return this;
    }

}