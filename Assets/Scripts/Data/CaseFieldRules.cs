using System;
using System.Collections.Generic;

/// <summary>
/// One field the player must get right for the case to pass, from the case
/// JSON "requiredFields" list. key = a CaseData field (see
/// CaseFieldRules.SupportedKeys), hint = what the Auditor says when it is
/// missing or wrong (never the answer), mistakeCost = its real-world meaning
/// on the Audit Summary (optional; falls back to mistakeCosts).
/// </summary>
[Serializable]
public class RequiredFieldDefinition
{
    public string key;
    public string hint;
    public string mistakeCost;
}

/// <summary>
/// PURPOSE:
/// The single place that knows how to read a gradeable field from the
/// player's CaseData and from the case's answerKey. Which fields a case
/// grades, in what order, with which hints, comes from the case JSON
/// (CaseDefinition.requiredFields); this class only maps a key to its value.
///
/// Adding a gradeable field for a future level = one line in each switch
/// below plus the field itself in CaseData / AnswerKeyDefinition. Nothing in
/// the checkers changes.
/// </summary>
public static class CaseFieldRules
{
    public const string ResidencyStatus = "residencyStatus";
    public const string TaxpayerType = "taxpayerType";
    public const string IncomeSource = "incomeSource";
    public const string NumberOfEmployers = "numberOfEmployers";
    public const string BusinessRegistration = "businessRegistration";
    public const string TaxOption = "taxOption";
    public const string RequiredForm = "requiredForm";

    public static readonly IReadOnlyList<string> SupportedKeys = new[]
    {
        ResidencyStatus, TaxpayerType, IncomeSource, NumberOfEmployers,
        BusinessRegistration, TaxOption, RequiredForm
    };

    /// <summary>
    /// The Level 1 rules, used when a case JSON has no requiredFields list
    /// (keeps every existing case behaving exactly as before).
    /// </summary>
    public static List<RequiredFieldDefinition> Defaults() => new List<RequiredFieldDefinition>
    {
        new RequiredFieldDefinition { key = ResidencyStatus, hint = "Review the taxpayer's background and living situation." },
        new RequiredFieldDefinition { key = TaxpayerType, hint = "Look for details that help identify the taxpayer's classification." },
        new RequiredFieldDefinition { key = IncomeSource, hint = "Review how the taxpayer earns income." }
    };

    public static bool IsSupported(string key)
    {
        foreach (string k in SupportedKeys)
            if (k == key) return true;
        return false;
    }

    /// <summary>The enum type behind a key (null when unknown).</summary>
    public static Type EnumType(string key)
    {
        switch (key)
        {
            case ResidencyStatus: return typeof(global::ResidencyStatus);
            case TaxpayerType: return typeof(global::TaxpayerType);
            case IncomeSource: return typeof(global::IncomeSource);
            case NumberOfEmployers: return typeof(EmployerCount);
            case BusinessRegistration: return typeof(global::BusinessRegistration);
            case TaxOption: return typeof(global::TaxOption);
            case RequiredForm: return typeof(global::RequiredForm);
        }
        return null;
    }

    /// <summary>The player's current value by name, or null when not filled in.</summary>
    public static string PlayerValue(CaseData data, string key)
    {
        if (data == null) return null;
        switch (key)
        {
            case ResidencyStatus: return data.residencyStatus?.ToString();
            case TaxpayerType: return data.taxpayerType?.ToString();
            case IncomeSource: return data.incomeSource?.ToString();
            case NumberOfEmployers: return data.numberOfEmployers?.ToString();
            case BusinessRegistration: return data.businessRegistration?.ToString();
            case TaxOption: return data.taxOption?.ToString();
            case RequiredForm: return data.requiredForm?.ToString();
        }
        return null;
    }

    /// <summary>The answer key's value for a field (as written in the JSON).</summary>
    public static string AnswerValue(AnswerKeyDefinition answers, string key)
    {
        if (answers == null) return null;
        switch (key)
        {
            case ResidencyStatus: return answers.residencyStatus;
            case TaxpayerType: return answers.taxpayerType;
            case IncomeSource: return answers.incomeSource;
            case NumberOfEmployers: return answers.numberOfEmployers;
            case BusinessRegistration: return answers.businessRegistration;
            case TaxOption: return answers.taxOption;
            case RequiredForm: return answers.requiredForm;
        }
        return null;
    }

    /// <summary>
    /// Null when the answer key value is usable for this field, otherwise
    /// why not (missing, or not one of the field's allowed values).
    /// </summary>
    public static string AnswerProblem(AnswerKeyDefinition answers, string key)
    {
        Type type = EnumType(key);
        if (type == null) return $"'{key}' is not a gradeable field (use one of: {string.Join(", ", SupportedKeys)})";
        string value = AnswerValue(answers, key);
        if (string.IsNullOrWhiteSpace(value)) return $"answerKey.{key} is empty";
        if (!Enum.IsDefined(type, value.Trim()))
            return $"answerKey.{key} '{value}' is not one of: {string.Join(", ", Enum.GetNames(type))}";
        return null;
    }

    /// <summary>True when the player filled the field in with the answer key's value.</summary>
    public static bool IsCorrect(CaseData data, AnswerKeyDefinition answers, string key)
    {
        string player = PlayerValue(data, key);
        if (player == null || AnswerProblem(answers, key) != null) return false;
        return string.Equals(player, AnswerValue(answers, key).Trim(), StringComparison.Ordinal);
    }

    /// <summary>A fingerprint of the player's answers to these fields (same answers = same text).</summary>
    public static string Signature(CaseData data, IEnumerable<RequiredFieldDefinition> fields)
    {
        if (data == null || fields == null) return string.Empty;
        var parts = new List<string>();
        foreach (RequiredFieldDefinition field in fields)
            if (field != null) parts.Add(PlayerValue(data, field.key) ?? string.Empty);
        return string.Join("|", parts);
    }
}
