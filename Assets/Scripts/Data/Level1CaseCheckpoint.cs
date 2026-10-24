using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// PURPOSE:
/// Reads and writes the player's Level 1 case work in CaseData for the field
/// checkpoint (Campaign Foundation T05). No Unity calls.
///
/// Only what the player produced is copied: folder fields they filled (by the
/// folder's stable keys, as enum names or invariant numbers), the documents
/// they read, the assessment / stamp / computation / print flags. Authored case
/// data and the answer key are never copied; the definition only contributes a
/// fingerprint so a snapshot of another case or revision is recognised.
/// </summary>
public static class Level1CaseCheckpoint
{
    /// <summary>The case folder's player-filled field keys (CaseFolderUI rows).</summary>
    public static readonly string[] FieldKeys =
    {
        "ResidencyStatus", "TaxpayerType", "IncomeSource", "EmployerCount", "BusinessRegistration", "TaxOption",
        "RequiredForm", "GrossIncome", "AllowableExpenses", "TaxableIncome", "TaxDue", "TaxCredits", "FinalTaxPayable"
    };

    /// <summary>Fingerprint of the authored case: id, number, documents (names, labels, shown values, keys) and graded field keys.</summary>
    public static string Fingerprint(CaseDefinition definition)
    {
        if (definition == null) return null;
        var parts = new List<string> { definition.caseId, definition.caseNumber };
        if (definition.documents != null)
            foreach (DocumentDefinition document in definition.documents)
            {
                if (document == null) continue;
                parts.Add(document.documentName);
                if (document.fields == null) continue;
                foreach (DocumentFieldDefinition field in document.fields)
                    if (field != null) { parts.Add(field.label); parts.Add(field.displayValue); parts.Add(field.semanticKey); }
            }
        foreach (RequiredFieldDefinition graded in definition.GradedFields())
            if (graded != null) parts.Add(graded.key);
        return Level1FieldCheckpointRules.Fingerprint(parts);
    }

    /// <summary>Copies the player's case work into the record (identity, fields, documents, flags).</summary>
    public static void Capture(CaseData data, CaseDefinition definition, Level1CaseRecord into)
    {
        if (data == null || definition == null || into == null) return;
        into.caseId = definition.caseId;
        into.caseFingerprint = Fingerprint(definition);
        into.fields.Clear();
        foreach (string key in FieldKeys)
        {
            if (data.folderTransferredFields == null || !data.folderTransferredFields.Contains(key)) continue;
            string value = Read(data, key);
            if (value != null) into.fields.Add(new Level1CaseField { key = key, value = value });
        }
        into.reviewedDocuments.Clear();
        if (data.supportingDocuments != null)
            foreach (SupportingDocument document in data.supportingDocuments)
                if (document != null && document.isReviewed && !string.IsNullOrWhiteSpace(document.documentName))
                    into.reviewedDocuments.Add(document.documentName);
        into.caseAssessment = data.caseAssessment.ToString();
        into.assessmentStamped = data.assessmentStamped;
        into.computationStatus = data.computationStatus.ToString();
        into.hasPrintedReturn = data.hasPrintedReturn;
        into.isCarryingPrintedReturn = data.isCarryingPrintedReturn;
    }

    /// <summary>
    /// Puts the saved work back into a freshly loaded CaseData (no events, no
    /// transfers replayed). Returns how many saved values could not be used.
    /// </summary>
    public static int Apply(Level1CaseRecord record, CaseData data)
    {
        if (record == null || data == null) return 0;
        int rejected = 0;
        if (data.folderTransferredFields == null) data.folderTransferredFields = new HashSet<string>();
        foreach (Level1CaseField field in record.fields)
        {
            if (field != null && Write(data, field.key, field.value)) data.folderTransferredFields.Add(field.key);
            else rejected++;
        }
        var reviewed = new HashSet<string>(record.reviewedDocuments ?? new List<string>(), StringComparer.Ordinal);
        if (data.supportingDocuments != null)
            foreach (SupportingDocument document in data.supportingDocuments)
                if (document != null && reviewed.Contains(document.documentName)) document.isReviewed = true;
        if (TryEnum(record.caseAssessment, out CaseAssessment assessment)) data.caseAssessment = assessment;
        data.assessmentStamped = record.assessmentStamped;
        if (TryEnum(record.computationStatus, out ComputationStatus computation)) data.computationStatus = computation;
        data.hasPrintedReturn = record.hasPrintedReturn;
        data.isCarryingPrintedReturn = record.isCarryingPrintedReturn;
        return rejected;
    }

    /// <summary>The field's current value as text, or null when it is empty.</summary>
    public static string Read(CaseData d, string key)
    {
        switch (key)
        {
            case "ResidencyStatus": return d.residencyStatus?.ToString();
            case "TaxpayerType": return d.taxpayerType?.ToString();
            case "IncomeSource": return d.incomeSource?.ToString();
            case "EmployerCount": return d.numberOfEmployers?.ToString();
            case "BusinessRegistration": return d.businessRegistration?.ToString();
            case "TaxOption": return d.taxOption?.ToString();
            case "RequiredForm": return d.requiredForm?.ToString();
            case "GrossIncome": return Number(d.grossIncome);
            case "AllowableExpenses": return Number(d.allowableExpenses);
            case "TaxableIncome": return Number(d.taxableIncome);
            case "TaxDue": return Number(d.taxDue);
            case "TaxCredits": return Number(d.taxWithheldOrCredits);
            case "FinalTaxPayable": return Number(d.finalTaxPayable);
            default: return null;
        }
    }

    /// <summary>Sets one field from its saved text. False for an unknown key or a value that does not parse.</summary>
    public static bool Write(CaseData d, string key, string value)
    {
        switch (key)
        {
            case "ResidencyStatus": return Set<ResidencyStatus>(value, v => d.residencyStatus = v);
            case "TaxpayerType": return Set<TaxpayerType>(value, v => d.taxpayerType = v);
            case "IncomeSource": return Set<IncomeSource>(value, v => d.incomeSource = v);
            case "EmployerCount": return Set<EmployerCount>(value, v => d.numberOfEmployers = v);
            case "BusinessRegistration": return Set<BusinessRegistration>(value, v => d.businessRegistration = v);
            case "TaxOption": return Set<TaxOption>(value, v => d.taxOption = v);
            case "RequiredForm": return Set<RequiredForm>(value, v => d.requiredForm = v);
            case "GrossIncome": return SetNumber(value, v => d.grossIncome = v);
            case "AllowableExpenses": return SetNumber(value, v => d.allowableExpenses = v);
            case "TaxableIncome": return SetNumber(value, v => d.taxableIncome = v);
            case "TaxDue": return SetNumber(value, v => d.taxDue = v);
            case "TaxCredits": return SetNumber(value, v => d.taxWithheldOrCredits = v);
            case "FinalTaxPayable": return SetNumber(value, v => d.finalTaxPayable = v);
            default: return false;
        }
    }

    private static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static bool TryEnum<T>(string text, out T value) where T : struct
    {
        value = default;
        return !string.IsNullOrWhiteSpace(text) && Enum.TryParse(text.Trim(), false, out value) && Enum.IsDefined(typeof(T), value);
    }

    private static bool Set<T>(string text, Action<T> assign) where T : struct
    {
        if (!TryEnum(text, out T value)) return false;
        assign(value);
        return true;
    }

    private static bool SetNumber(string text, Action<float> assign)
    {
        if (string.IsNullOrWhiteSpace(text) ||
            !float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
            float.IsNaN(value) || float.IsInfinity(value)) return false;
        assign(value);
        return true;
    }
}
