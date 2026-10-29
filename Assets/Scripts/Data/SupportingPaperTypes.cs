using System;
using System.Collections.Generic;

/// <summary>The eight fixed supporting-document types (Documentation/SupportingDocuments.md). Values are the type numbers.</summary>
public enum SupportingPaperType
{
    Unmapped = 0,
    Identity = 1,
    Background = 2,
    Compensation2316 = 3,
    IncomeRecord = 4,
    PaymentRecord = 5,
    BankStatement = 6,
    Agreement = 7,
    Withholding2307 = 8
}

/// <summary>The three basic paper bodies of the standard.</summary>
public enum SupportingPaperShape { Sheet, Slip, Card }

/// <summary>One type's fixed look: shape, title, restrained accent and how many table rows one face shows.</summary>
public struct SupportingPaperStyle
{
    public SupportingPaperType Type;
    public SupportingPaperShape Shape;
    public string Title;
    /// <summary>0xRRGGBB accent of the header band (identifies the type only, never correctness).</summary>
    public uint Header;
    /// <summary>0xRRGGBB paper colour (warm ivory; the bank statement is pale blue).</summary>
    public uint Paper;
    /// <summary>Owner/context labels: who it is from or for, and the second party (Type 7) when there is one.</summary>
    public string IssuerLabel;
    public string PartyLabel;
    public int RowsPerFace;
    public bool HasTotalBand;
    public bool HasColumns;
    public bool HasSignatures;
}

/// <summary>What one record paper prints (data only, built from the case JSON; never an answer).</summary>
public sealed class SupportingPaperFace
{
    public string RecordId = "";
    public SupportingPaperStyle Style;
    public string Title = "";
    public string Issuer = "";
    public string Party = "";
    public string Reference = "";
    public string Date = "";
    public string Status = "";
    public string TotalKey = "";
    /// <summary>The semantic keys of the record's rows in print order (faces split them; the total band finds its row).</summary>
    public List<string> RowKeys = new List<string>();
    /// <summary>A plain readable sheet because the record has no implemented type (the diagnostic says why).</summary>
    public bool Fallback;
    public string Problem = "";
}

/// <summary>
/// PURPOSE:
/// The supporting-document types implemented so far and the fixed reading
/// order every record face follows (T18, incremental adoption): header
/// (icon + short title) -> owner/context (issuer, reference, date) ->
/// evidence rows -> footer (total band / payment status / signatures).
/// Level 1 owns Types 1-2 (ClientRecordPaper); T18 adds Rosa's Types 4-7
/// on Level 2's record papers. Types 3 and 8 stay planned: a record mapped
/// to them, or not mapped at all, prints as a plain readable sheet with a
/// diagnostic, never as another client's record.
///
/// RULES: missing context values print a dash; long values wrap (the paper
/// grows, text never shrinks into microprint); a table longer than one face
/// continues on another face of the same type (FaceCount / FaceRange).
/// </summary>
public static class SupportingPaperTypes
{
    public const string Dash = "—";
    public const int SheetRows = 5;
    public const int SlipRows = 4;

    private const uint Ivory = 0xFDF7E2;

    /// <summary>Implemented physical presentations (Level 1: 1-2; T18: 4-7). 3 and 8 wait for a case that needs them.</summary>
    public static bool Implemented(SupportingPaperType type) =>
        type == SupportingPaperType.Identity || type == SupportingPaperType.Background ||
        (type >= SupportingPaperType.IncomeRecord && type <= SupportingPaperType.Agreement);

    public static SupportingPaperStyle Style(SupportingPaperType type)
    {
        switch (type)
        {
            case SupportingPaperType.IncomeRecord:
                return new SupportingPaperStyle
                {
                    Type = type, Shape = SupportingPaperShape.Sheet, Title = "INCOME RECORD", Header = 0x7E9C7A, Paper = Ivory,
                    IssuerLabel = "Kept by", RowsPerFace = SheetRows, HasTotalBand = true
                };
            case SupportingPaperType.PaymentRecord:
                return new SupportingPaperStyle
                {
                    Type = type, Shape = SupportingPaperShape.Slip, Title = "PAYMENT RECORD", Header = 0xC8973E, Paper = Ivory,
                    IssuerLabel = "Issued by", RowsPerFace = SlipRows
                };
            case SupportingPaperType.BankStatement:
                return new SupportingPaperStyle
                {
                    Type = type, Shape = SupportingPaperShape.Sheet, Title = "BANK STATEMENT", Header = 0x5C7FA8, Paper = 0xEAF1F8,
                    IssuerLabel = "Bank", RowsPerFace = SheetRows, HasColumns = true
                };
            case SupportingPaperType.Agreement:
                return new SupportingPaperStyle
                {
                    Type = type, Shape = SupportingPaperShape.Sheet, Title = "AGREEMENT", Header = 0x86607E, Paper = Ivory,
                    IssuerLabel = "Party", PartyLabel = "With", RowsPerFace = SheetRows, HasSignatures = true
                };
            default:
                // Unmapped or not implemented here: a plain readable sheet in the record's own words.
                return new SupportingPaperStyle
                {
                    Type = SupportingPaperType.Unmapped, Shape = SupportingPaperShape.Sheet, Title = "RECORD", Header = 0x6E706A, Paper = Ivory,
                    IssuerLabel = "From", RowsPerFace = SheetRows
                };
        }
    }

    /// <summary>The record's type from its authored mapping; Unmapped (with the reason) when it has none this build can print.</summary>
    public static SupportingPaperType Resolve(ReviewDocumentDefinition record, out string problem)
    {
        problem = "";
        if (record == null) { problem = "no record"; return SupportingPaperType.Unmapped; }
        if (record.paperType == 0) { problem = $"record {record.id} has no paperType"; return SupportingPaperType.Unmapped; }
        if (!Enum.IsDefined(typeof(SupportingPaperType), record.paperType))
        {
            problem = $"record {record.id} names unknown paperType {record.paperType}";
            return SupportingPaperType.Unmapped;
        }
        var type = (SupportingPaperType)record.paperType;
        if (!Implemented(type))
        {
            problem = $"record {record.id} uses Type {record.paperType}, which no level has built yet";
            return SupportingPaperType.Unmapped;
        }
        return type;
    }

    /// <summary>
    /// What a record prints. party is the case's client (Type 7's second
    /// party). Only the record's own authored values are used; blanks print a dash.
    /// </summary>
    public static SupportingPaperFace Face(ReviewDocumentDefinition record, string party)
    {
        SupportingPaperType type = Resolve(record, out string problem);
        SupportingPaperStyle style = Style(type);
        var face = new SupportingPaperFace
        {
            RecordId = record?.id ?? "",
            Style = style,
            Fallback = type == SupportingPaperType.Unmapped,
            Problem = problem,
            Issuer = OrDash(record?.issuer),
            Reference = OrDash(record?.reference),
            Date = OrDash(record?.date),
            Party = style.PartyLabel != null ? OrDash(party) : "",
            Status = record != null && !string.IsNullOrWhiteSpace(record.paperStatus) ? record.paperStatus.Trim().ToUpperInvariant() : "",
            TotalKey = record?.paperTotalKey?.Trim() ?? ""
        };
        string title = record != null && !string.IsNullOrWhiteSpace(record.paperTitle) ? record.paperTitle.Trim() : "";
        // An unmapped record keeps its own title so it is never mistaken for another record.
        face.Title = (title.Length > 0 ? title : face.Fallback && record != null && !string.IsNullOrWhiteSpace(record.title) ? record.title : style.Title)
            .ToUpperInvariant();
        return face;
    }

    public static string OrDash(string value) => string.IsNullOrWhiteSpace(value) ? Dash : value.Trim();

    /// <summary>Faces one table needs (at least one, even for no rows).</summary>
    public static int FaceCount(int rows, int perFace) => rows <= 0 ? 1 : (rows + Math.Max(1, perFace) - 1) / Math.Max(1, perFace);

    /// <summary>The rows face `face` shows (clamped to the existing faces).</summary>
    public static void FaceRange(int rows, int perFace, int face, out int start, out int count)
    {
        perFace = Math.Max(1, perFace);
        int faces = FaceCount(rows, perFace);
        face = Math.Max(0, Math.Min(face, faces - 1));
        start = face * perFace;
        count = Math.Max(0, Math.Min(perFace, rows - start));
    }

    /// <summary>Every problem in a packet's record mapping (the regression suite and the load-time warning use it).</summary>
    public static List<string> MappingProblems(IEnumerable<ReviewDocumentDefinition> documents)
    {
        var problems = new List<string>();
        if (documents == null) return problems;
        foreach (ReviewDocumentDefinition document in documents)
        {
            if (document == null || document.kind != "record") continue;
            Resolve(document, out string problem);
            if (problem.Length > 0) problems.Add(problem);
        }
        return problems;
    }
}
