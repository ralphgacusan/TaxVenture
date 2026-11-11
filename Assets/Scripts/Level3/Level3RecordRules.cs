using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// A Level 3 case's papers as the existing fixed physical record types print
/// them (T04), pure C# so the regression suite checks the real bindings:
///
/// - Type 1 (B-ID) -> the ID card; Type 2 (B-BG) -> client background notes;
///   Types 4 / 5 / 7 and plain working papers (type 0) -> CaseRecordRules'
///   own transaction-paper builder, the same one Rosa's papers use.
/// - Every value is the case document's printed text; rows keep their order,
///   a source's total prints in its total band, and a row whose transaction
///   also appears on another paper shows that reference (PRINT-26, RENT-26),
///   so the duplicate printing bill is visible on both papers.
/// - Type 8 (B-WHT, T08) -> the creditable withholding certificate: payer,
///   payee, period, reference, its purpose, the income payment with where it
///   is already counted (the content's credit link: "part of ... on REV-B26",
///   so it is never another revenue line) and the tax withheld in its band.
/// - Only acquired sources are built (B-WHT only after the credit step).
/// Ids are "caseId/sourceId"; record.source is the sourceId (= documentName),
/// the same identity the Bag's Case Folder lists. Nothing is an answer.
/// </summary>
public static class Level3RecordRules
{
    /// <summary>The papers of these acquired sources in a stable reading order (ID, background, then content order).</summary>
    public static List<ClientRecord> Build(CaseDefinition definition, ICollection<string> acquiredSourceIds)
    {
        var records = new List<ClientRecord>();
        Level3CaseContent content = definition?.level3;
        if (content == null || !content.IsSet || acquiredSourceIds == null) return records;
        string caseId = definition.caseId;
        string client = content.taxpayer;
        foreach (Level3SourceDefinition source in content.sources)
        {
            if (source == null || !acquiredSourceIds.Contains(source.sourceId)) continue;
            DocumentDefinition document = definition.documents?.Find(d => d != null && d.documentName == source.documentName);
            if (document == null) continue;
            ClientRecord record = source.paperType == 1 ? Identity(caseId, client, source, document)
                : source.paperType == 2 ? Background(caseId, client, source, document)
                : source.paperType == 8 ? Withholding(caseId, client, source, document, definition)
                : Transaction(caseId, client, source, document, content);
            if (record != null) records.Add(record);
        }
        records.Sort((a, b) => Rank(a).CompareTo(Rank(b)));
        return records;
    }

    private static int Rank(ClientRecord record) =>
        record.type == ClientRecord.Identity ? 0 : record.type == ClientRecord.Background ? 1 : record.IsSlip ? 3 : 2;

    public static string RecordId(string caseId, string sourceId) => (caseId ?? "case") + "/" + (sourceId ?? "");

    private static string Text(DocumentDefinition document, string label) =>
        document.fields?.Find(f => f != null && f.type == "Text" && f.label == label)?.displayValue;

    private static ClientRecord Identity(string caseId, string client, Level3SourceDefinition source, DocumentDefinition document) => new ClientRecord
    {
        id = RecordId(caseId, source.sourceId),
        npcId = caseId,
        type = ClientRecord.Identity,
        source = source.sourceId,
        tab = source.sourceId,
        title = Text(document, "TIN") != null ? "TIN ID" : "IDENTITY CARD",
        name = Text(document, "Name") ?? client,
        reference = Text(document, "TIN") ?? Text(document, "ID number") ?? source.reference
    };

    private static ClientRecord Background(string caseId, string client, Level3SourceDefinition source, DocumentDefinition document) => new ClientRecord
    {
        id = RecordId(caseId, source.sourceId),
        npcId = caseId,
        type = ClientRecord.Background,
        source = source.sourceId,
        tab = source.sourceId,
        title = "CLIENT BACKGROUND",
        name = client,
        period = Text(document, "Covers") ?? Text(document, "Period"),
        work = Text(document, "Work"),
        income = Text(document, "Tax rates"),
        living = Text(document, "Goal") ?? Text(document, "Bookkeeper"),
        footer = "Client interview notes · " + source.reference
    };

    /// <summary>Types 4/5/7 and working papers through CaseRecordRules.FromReviewRecord (Rosa's builder).</summary>
    private static ClientRecord Transaction(string caseId, string client, Level3SourceDefinition source, DocumentDefinition document,
        Level3CaseContent content)
    {
        var shared = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Level3RowDefinition row in content.rows)
            if (row != null && !string.IsNullOrEmpty(row.transactionId))
                shared[row.transactionId] = shared.TryGetValue(row.transactionId, out int n) ? n + 1 : 1;

        string totalKey = content.totals?.Find(t => t != null && t.sourceId == source.sourceId)?.fieldKey;
        var input = new ReviewRecordInput
        {
            id = source.sourceId,
            documentName = source.documentName,
            paperType = source.paperType,
            title = source.paperType == 0 ? "WORKING PAPER" : null,
            issuer = source.issuer,
            reference = source.reference,
            date = source.periodStart == source.periodEnd ? source.periodStart : source.periodStart + " to " + source.periodEnd,
            status = null,
            totalKey = totalKey
        };
        // The purpose/status the source states is printed first (CONTENT: issuer, purpose/status, amount/reference).
        if (!string.IsNullOrWhiteSpace(source.status))
            input.fields.Add(new CaseRecordField { key = source.sourceId + "/status", label = "Note", value = Sentence(source.status) });
        foreach (DocumentFieldDefinition field in document.fields)
        {
            if (field == null || field.type != "Number") continue;
            Level3RowDefinition row = content.rows.Find(r => r != null && r.sourceId == source.sourceId && r.fieldKey == field.semanticKey);
            string label = field.label;
            if (row != null && shared.TryGetValue(row.transactionId, out int count) && count > 1 && row.transactionId.Contains("-"))
                label += " · " + row.transactionId;
            input.fields.Add(new CaseRecordField { key = field.semanticKey, label = label, value = field.displayValue });
        }
        ClientRecord record = CaseRecordRules.FromReviewRecord(caseId, client, input);
        if (record != null) record.source = source.sourceId;
        return record;
    }

    /// <summary>The purpose line every withholding certificate prints (what it is, not what to do with it).</summary>
    public const string WithholdingPurpose = "Creditable tax withheld at source: tax the payer already paid for the payee.";

    /// <summary>
    /// Type 8: the certificate's own printed values. Its credit row (content
    /// creditLinks) is the TAX WITHHELD band; every other amount is an income
    /// payment, printed with the row it is already counted in.
    /// </summary>
    private static ClientRecord Withholding(string caseId, string client, Level3SourceDefinition source, DocumentDefinition document, CaseDefinition definition)
    {
        Level3CaseContent content = definition.level3;
        var record = new ClientRecord
        {
            id = RecordId(caseId, source.sourceId),
            npcId = caseId,
            type = ClientRecord.Withholding,
            source = source.sourceId,
            tab = source.sourceId,
            title = "WITHHOLDING CERTIFICATE",
            name = client,
            issuer = Text(document, "Issued by") ?? source.issuer,
            party = Text(document, "For") ?? source.party,
            period = Text(document, "Period") ?? (source.periodStart + " to " + source.periodEnd),
            reference = Text(document, "Reference") ?? source.reference,
            footer = WithholdingPurpose
        };
        var rows = new List<ClientRecordRow>();
        foreach (DocumentFieldDefinition field in document.fields)
        {
            if (field == null || field.type != "Number") continue;
            Level3RowDefinition row = content.rows.Find(r => r != null && r.sourceId == source.sourceId && r.fieldKey == field.semanticKey);
            Level3CreditLink credit = row != null ? content.creditLinks?.Find(l => l != null && l.creditRowId == row.rowId) : null;
            if (credit != null && record.totalValue == null)
            {
                record.totalLabel = field.label;
                record.totalValue = field.displayValue;
                continue;
            }
            rows.Add(new ClientRecordRow { label = field.label, value = field.displayValue });
            Level3CreditLink basis = row != null ? content.creditLinks?.Find(l => l != null && l.incomeRowId == row.rowId) : null;
            if (basis != null && record.includedIn == null) record.includedIn = IncludedIn(definition, basis.includedInRowId);
        }
        int perFace = CaseRecordRules.WithholdingRowsPerFace;
        record.rows = rows.Count > perFace ? rows.GetRange(0, perFace) : rows;
        record.backRows = rows.Count > perFace ? rows.GetRange(perFace, rows.Count - perFace) : new List<ClientRecordRow>();
        return record;
    }

    /// <summary>"Already part of Menu illustration fees ₱400,000.00 on income record REV-B26 (2026)." (null without a printed parent row)</summary>
    public static string IncludedIn(CaseDefinition definition, string rowId)
    {
        Level3CaseContent content = definition?.level3;
        Level3RowDefinition parent = content?.rows?.Find(r => r != null && r.rowId == rowId);
        Level3SourceDefinition source = parent != null ? content.sources?.Find(s => s != null && s.sourceId == parent.sourceId) : null;
        DocumentDefinition document = source != null ? definition.documents?.Find(d => d != null && d.documentName == source.documentName) : null;
        DocumentFieldDefinition field = document?.fields?.Find(x => x != null && x.semanticKey == parent.fieldKey);
        if (field == null) return null;
        string year = content.taxYear > 0 ? " (" + content.taxYear + ")" : "";
        return "Already part of " + field.label + " " + field.displayValue + " on income record " + source.reference + year + ".";
    }

    private static string Sentence(string text)
    {
        string t = text.Trim();
        return t.Length == 0 ? t : char.ToUpperInvariant(t[0]) + t.Substring(1);
    }
}
