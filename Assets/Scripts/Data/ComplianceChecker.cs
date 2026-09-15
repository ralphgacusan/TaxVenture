using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Checks whether the three required taxpayer facts were gathered AND are
/// correct, per the case's JSON answerKey. Level 1 checks:
/// - Residency status
/// - Taxpayer type
/// - Income source
///
/// STRICT: a field that is missing OR filled in with the wrong value both
/// count as an issue. There is no partial credit for "at least they typed
/// something" — the player's value must match the answer key exactly on
/// all three fields to pass. If the answer key itself can't be read
/// (missing/unparseable), the check fails closed rather than silently
/// passing.
/// </summary>
public static class ComplianceChecker
{
    public static List<ComplianceIssue> RunCheck(CaseData data)
    {
        var issues = new List<ComplianceIssue>();

        if (data == null)
        {
            issues.Add(new ComplianceIssue(
                "The case information is unavailable. Review the case again."));
            return issues;
        }

        AnswerKeyDefinition answerKey = CaseManager.Instance?.CurrentDefinition?.answerKey;

        if (answerKey == null)
        {
            Debug.LogError(
                "[ComplianceChecker] CurrentDefinition.answerKey is NULL. " +
                "Cannot strictly verify this case — failing closed so a " +
                "broken case never silently passes.");

            issues.Add(new ComplianceIssue(
                "The case information is unavailable. Review the case again."));
            return issues;
        }

        // =====================================================
        // REQUIRED TAXPAYER FACTS — missing OR wrong both flag
        // =====================================================

        if (!TryMatchEnum(data.residencyStatus, answerKey.residencyStatus))
        {
            issues.Add(new ComplianceIssue(
                "Review the taxpayer's background and living situation."));
        }

        if (!TryMatchEnum(data.taxpayerType, answerKey.taxpayerType))
        {
            issues.Add(new ComplianceIssue(
                "Look for details that help identify the taxpayer's classification."));
        }

        if (!TryMatchEnum(data.incomeSource, answerKey.incomeSource))
        {
            issues.Add(new ComplianceIssue(
                "Review how the taxpayer earns income."));
        }

        return issues;
    }

    /// <summary>
    /// Compares a nullable player-entered enum against the answer key's
    /// string value by NAME. Returns false (fails the check) if the
    /// player hasn't entered a value, if the answer key string is missing,
    /// or if the answer key string doesn't parse to this enum type at all
    /// — a typo in the JSON should never accidentally make a field
    /// unpassable-but-silently-skipped or auto-pass.
    /// </summary>
    private static bool TryMatchEnum<TEnum>(TEnum? playerValue, string answerKeyValue)
        where TEnum : struct, System.Enum
    {
        if (!playerValue.HasValue)
        {
            return false;
        }

        if (string.IsNullOrEmpty(answerKeyValue))
        {
            Debug.LogError(
                $"[ComplianceChecker] answerKey is missing a value for " +
                $"{typeof(TEnum).Name}. Treating as incorrect.");
            return false;
        }

        if (!System.Enum.TryParse(answerKeyValue, out TEnum parsedAnswer))
        {
            Debug.LogError(
                $"[ComplianceChecker] answerKey value '{answerKeyValue}' " +
                $"does not match any {typeof(TEnum).Name} enum member. " +
                $"Check the case JSON for a typo. Treating as incorrect.");
            return false;
        }

        return playerValue.Value.Equals(parsedAnswer);
    }
}