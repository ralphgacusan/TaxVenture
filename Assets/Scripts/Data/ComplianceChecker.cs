using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Checks whether the case's required taxpayer facts were gathered AND are
/// correct, per the case's JSON answerKey. WHICH fields are required, in
/// what order, and the Auditor's hint for each come from the case JSON
/// (CaseDefinition.requiredFields; Level 1 defaults: residency status,
/// taxpayer type, income source).
///
/// STRICT: a field that is missing OR filled in with the wrong value both
/// count as an issue. There is no partial credit for "at least they typed
/// something" — the player's value must match the answer key exactly on
/// every required field to pass. If the answer key itself can't be read
/// (missing/unparseable), the check fails closed rather than silently
/// passing.
/// </summary>
public static class ComplianceChecker
{
    private const string UnavailableMessage = "The case information is unavailable. Review the case again.";

    public static List<ComplianceIssue> RunCheck(CaseData data) =>
        RunCheck(data, CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null);

    public static List<ComplianceIssue> RunCheck(CaseData data, CaseDefinition definition)
    {
        var issues = new List<ComplianceIssue>();

        if (data == null)
        {
            issues.Add(new ComplianceIssue(UnavailableMessage));
            return issues;
        }

        AnswerKeyDefinition answerKey = definition != null ? definition.answerKey : null;
        if (answerKey == null)
        {
            Debug.LogError(
                "[ComplianceChecker] CurrentDefinition.answerKey is NULL. " +
                "Cannot strictly verify this case — failing closed so a " +
                "broken case never silently passes.");

            issues.Add(new ComplianceIssue(UnavailableMessage));
            return issues;
        }

        // =====================================================
        // REQUIRED TAXPAYER FACTS — missing OR wrong both flag
        // =====================================================

        foreach (RequiredFieldDefinition field in definition.GradedFields())
        {
            string problem = CaseFieldRules.AnswerProblem(answerKey, field.key);
            if (problem != null)
                Debug.LogError($"[ComplianceChecker] {problem}. Check the case JSON. Treating as incorrect.");

            if (problem != null || !CaseFieldRules.IsCorrect(data, answerKey, field.key))
            {
                string hint = string.IsNullOrWhiteSpace(field.hint)
                    ? "Review the case again: one of the required facts is missing or wrong."
                    : field.hint;
                issues.Add(new ComplianceIssue(hint, field.key));
            }
        }

        return issues;
    }
}
