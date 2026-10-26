using System;
using System.Collections.Generic;

/// <summary>One printed fact of a case document (its label, shown value and semantic key, as authored in the case JSON).</summary>
[Serializable]
public class CaseRecordField
{
    public string label;
    public string value;
    public string key;
}

/// <summary>One authored case document as the records page reads it (documentName and its fields).</summary>
[Serializable]
public class CaseRecordDocument
{
    public string documentName;
    public List<CaseRecordField> fields = new List<CaseRecordField>();

    /// <summary>The shown value of the field with this semantic key, or null.</summary>
    public string Value(string key)
    {
        CaseRecordField field = Field(key);
        return field == null || string.IsNullOrWhiteSpace(field.value) ? null : field.value.Trim();
    }

    public CaseRecordField Field(string key) => fields == null ? null : fields.Find(f => f != null && f.key == key);
}

/// <summary>What the records page needs from the active case: identity, year and its authored documents.</summary>
[Serializable]
public class CaseRecordSource
{
    public string caseId;
    public string caseNumber;
    public string taxYear;
    public string birthdate;
    /// <summary>The client's taxpayer profile, printed on the back of her ID card (2026-10-27).</summary>
    public string profileText;
    public string profileNumber;
    public List<CaseRecordDocument> documents = new List<CaseRecordDocument>();
}

/// <summary>
/// PURPOSE:
/// Marie's two-page Level 1 case folder (Campaign Foundation T25): page two
/// "Supporting Documents" holds her case documents as attached physical
/// records, printed from the case JSON's own values (no second source, no
/// answer). User revision 2026-10-27: Level 1 has no BIR form, so her page
/// holds two records like Arthur's file:
///   Valid Government ID -> Type 1 ID card (name, citizenship, address; born
///     from the case's birthdate; masked case reference), her taxpayer
///     profile on its back;
///   Employment Records -> Type 2 client background sheet (her residency,
///     employment type and number of employers, one block each).
/// A compensation certificate (Type 3) is still recognised for later levels
/// (T27). The first-desk look-over: both records taken out and put back.
/// Pure C#, no Unity.
/// </summary>
public static class CaseRecordRules
{
    /// <summary>The supporting page's role (CaseFolderPageContent.Role).</summary>
    public const string PageRole = "supporting-records";

    public const string IdentityDocument = "Valid Government ID";
    public const string BackgroundDocument = "Employment Records";
    /// <summary>The 2316's documentName starts with this.</summary>
    public const string CompensationDocumentPrefix = "Certificate of Compensation";

    /// <summary>The page's type order: ID card on top, then the two work papers side by side.</summary>
    public static readonly string[] TypeOrder = { ClientRecord.Identity, ClientRecord.Background, ClientRecord.Compensation };

    /// <summary>The fixed type of a case document (by its documentName), or null when it is not one of these three.</summary>
    public static string TypeOf(string documentName)
    {
        if (string.IsNullOrWhiteSpace(documentName)) return null;
        string name = documentName.Trim();
        if (name == IdentityDocument) return ClientRecord.Identity;
        if (name == BackgroundDocument) return ClientRecord.Background;
        if (name.StartsWith(CompensationDocumentPrefix, StringComparison.Ordinal)) return ClientRecord.Compensation;
        return null;
    }

    /// <summary>A record's stable id: the case id and its type ("case_marie_001-compensation").</summary>
    public static string RecordId(string caseId, string type) => (caseId ?? "case") + "-" + type;

    /// <summary>The records in page order (one per known document type present; unknown documents are not shown here).</summary>
    public static List<ClientRecord> Build(CaseRecordSource source)
    {
        var records = new List<ClientRecord>();
        if (source?.documents == null) return records;
        foreach (string type in TypeOrder)
        {
            CaseRecordDocument document = source.documents.Find(d => d != null && TypeOf(d.documentName) == type);
            if (document == null) continue;
            ClientRecord record = type == ClientRecord.Identity ? Identity(source, document)
                : type == ClientRecord.Background ? Background(source, document)
                : Compensation(source, document);
            record.id = RecordId(source.caseId, type);
            record.npcId = source.caseId;
            record.type = type;
            record.source = document.documentName.Trim();
            records.Add(record);
        }
        return records;
    }

    /// <summary>
    /// The records the first desk look-over needs, in page order: every record on her page
    /// (user revision 2026-10-27: her ID and her background, like Arthur's file; the 2316 left Level 1).
    /// </summary>
    public static List<string> WorkRecordIds(IEnumerable<ClientRecord> records)
    {
        var ids = new List<string>();
        if (records == null) return ids;
        foreach (ClientRecord record in records)
            if (record != null && !string.IsNullOrEmpty(record.id)) ids.Add(record.id);
        return ids;
    }

    /// <summary>Every work record was taken out and looked at (settled out of the folder).</summary>
    public static bool AllShown(ICollection<string> workIds, ICollection<string> shownIds) => All(workIds, shownIds);

    /// <summary>The first-desk preparation: every work record shown and put back (the ID is never needed).</summary>
    public static bool Prepared(ICollection<string> workIds, ICollection<string> shownIds, ICollection<string> returnedIds) =>
        All(workIds, shownIds) && All(workIds, returnedIds);

    /// <summary>
    /// A case already under way (saved preparation, a filled entry or an audit) never asks for the
    /// preparation again: the guided pages pass at once and nothing is locked or reset.
    /// </summary>
    public static bool AlreadyUnderWay(bool savedPrepared, int filledEntries, int auditAttempts, bool approved) =>
        savedPrepared || filledEntries > 0 || auditAttempts > 0 || approved;

    private static bool All(ICollection<string> wanted, ICollection<string> have)
    {
        if (wanted == null || wanted.Count == 0 || have == null) return false;
        foreach (string id in wanted)
            if (!have.Contains(id)) return false;
        return true;
    }

    // =========================================================
    // THE THREE FACES (fixed layouts; values straight from the case)
    // =========================================================

    private static ClientRecord Identity(CaseRecordSource source, CaseRecordDocument document) => new ClientRecord
    {
        tab = "Identity",
        title = "IDENTITY CARD",
        name = document.Value("FullName"),
        citizenship = document.Value("Citizenship"),
        born = ShortDate(source.birthdate),
        address = document.Value("Address"),
        reference = Masked(source.caseNumber),
        profileText = string.IsNullOrWhiteSpace(source.profileText) ? null : source.profileText.Trim(),
        profileNumber = source.profileNumber
    };

    private static ClientRecord Background(CaseRecordSource source, CaseRecordDocument document)
    {
        CaseRecordField residency = document.Field("ResidencyStatus");
        CaseRecordField employers = document.Field("NumberOfEmployers");
        return new ClientRecord
        {
            tab = "Background",
            title = "CLIENT BACKGROUND",
            name = NameOf(source, document),
            period = string.IsNullOrWhiteSpace(source.taxYear) ? null : "Year " + source.taxYear.Trim(),
            living = residency == null ? null : WithPlace(residency.value, residency.label),
            work = document.Value("EmploymentType"),
            income = employers == null || string.IsNullOrWhiteSpace(employers.value) ? null : "Employers this year: " + employers.value.Trim(),
            footer = "Client interview notes"
        };
    }

    private static ClientRecord Compensation(CaseRecordSource source, CaseRecordDocument document) => new ClientRecord
    {
        tab = "2316",
        title = "COMPENSATION 2316",
        name = document.Value("FullName"),
        period = string.IsNullOrWhiteSpace(source.taxYear) ? null : "Tax year " + source.taxYear.Trim(),
        employer = document.Value("EmployerName"),
        employerTin = MaskedTin(document.Value("EmployerTin")),
        compensationAmount = document.Value("GrossIncome"),
        withheldAmount = document.Value("TaxWithheldOrCredits"),
        reference = string.IsNullOrWhiteSpace(source.taxYear) ? "2316" : "2316-" + source.taxYear.Trim(),
        footer = "Certificate of compensation and tax withheld"
    };

    /// <summary>The name printed on the background sheet: the document's own, else the ID's.</summary>
    private static string NameOf(CaseRecordSource source, CaseRecordDocument document)
    {
        string own = document.Value("FullName");
        if (own != null) return own;
        CaseRecordDocument id = source.documents.Find(d => d != null && TypeOf(d.documentName) == ClientRecord.Identity);
        return id?.Value("FullName");
    }

    /// <summary>"Lifetime resident" + "Length of Residency in the Philippines" -> "Lifetime resident of the Philippines".</summary>
    public static string WithPlace(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        if (string.IsNullOrWhiteSpace(label)) return value;
        int at = label.LastIndexOf(" in ", StringComparison.Ordinal);
        if (at < 0) return value;
        string place = label.Substring(at + 4).Trim();
        return place.Length == 0 || value.IndexOf(place, StringComparison.OrdinalIgnoreCase) >= 0 ? value : value + " of " + place;
    }

    /// <summary>"March 14, 1997" -> "14 Mar 1997" (unchanged when it does not read as month day, year).</summary>
    public static string ShortDate(string date)
    {
        if (string.IsNullOrWhiteSpace(date)) return null;
        string[] parts = date.Replace(",", " ").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || parts[0].Length < 3 || !int.TryParse(parts[1], out int day) || !int.TryParse(parts[2], out _)) return date.Trim();
        return $"{day} {char.ToUpperInvariant(parts[0][0])}{parts[0].Substring(1, 2).ToLowerInvariant()} {parts[2]}";
    }

    /// <summary>A masked reference: the last four digits only ("ITR-2026-0101" -> "•••• 0101").</summary>
    public static string Masked(string reference)
    {
        string digits = Digits(reference);
        return digits.Length < 4 ? null : "•••• " + digits.Substring(digits.Length - 4);
    }

    /// <summary>A masked TIN: the last two groups kept ("789-012-345-000" -> "•••-•••-345-000").</summary>
    public static string MaskedTin(string tin)
    {
        if (string.IsNullOrWhiteSpace(tin)) return null;
        string[] groups = tin.Trim().Split('-');
        if (groups.Length < 3) return tin.Trim();
        for (int i = 0; i < groups.Length - 2; i++) groups[i] = new string('•', groups[i].Length);
        return string.Join("-", groups);
    }

    /// <summary>
    /// A plaza client's ID card (user revision 2026-10-27): the Type 1 front from their ID facts,
    /// their taxpayer profile on the back. Same layout as Marie's and Arthur's IDs.
    /// </summary>
    public static ClientRecord FromProfile(TaxpayerProfile profile)
    {
        if (profile == null) return null;
        int number = 0;
        IReadOnlyList<TaxpayerProfile> all = TaxpayerProfileDatabase.All;
        for (int i = 0; i < all.Count; i++) if (all[i].ProfileId == profile.ProfileId) number = i + 1;
        return new ClientRecord
        {
            id = "id-" + profile.ProfileId,
            npcId = profile.NpcId,
            type = ClientRecord.Identity,
            tab = "Identity",
            title = "IDENTITY CARD",
            name = profile.TaxpayerName,
            citizenship = profile.Citizenship,
            born = profile.Born,
            address = profile.Address,
            reference = profile.IdReference,
            profileText = profile.Description,
            profileNumber = number > 0 ? "No. 0" + number : null
        };
    }

    private static string Digits(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var digits = new System.Text.StringBuilder();
        foreach (char c in text) if (char.IsDigit(c)) digits.Append(c);
        return digits.ToString();
    }
}
