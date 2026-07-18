using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Pure logic that checks a CaseData instance against the design doc's
/// Section 12 Compliance Checklist:
///   Residency Status Determined
///   Taxpayer Type Determined
///   Income Source Determined
///   Business Registration Verified
///   Tax Option Verified
///   Supporting Documents Verified
///   Tax Computation Completed
///   Correct BIR Form Selected
///   No Unresolved Issues Remain
///
/// Zero Unity/UI/NPC dependency — same reasoning as
/// TaxComputationCalculator: takes data in, returns a list of problems out.
///
/// CONNECTS WITH:
/// - AuditorInteractable / AuditorDialogueUI: calls RunCheck() when the
///   player hands over the case
/// </summary>
public static class ComplianceChecker
{
    public static List<ComplianceIssue> RunCheck(CaseData data)
    {
        var issues = new List<ComplianceIssue>();



        if (!data.residencyStatus.HasValue)
            issues.Add(new ComplianceIssue("Residency Status was never determined during the interview."));

        if (!data.taxpayerType.HasValue)
            issues.Add(new ComplianceIssue("Taxpayer Type was never determined during the interview."));

        if (!data.incomeSource.HasValue)
            issues.Add(new ComplianceIssue("Income Source was never determined during the interview."));

        if (!data.businessRegistration.HasValue)
            issues.Add(new ComplianceIssue("Business Registration was never verified during the interview."));

        if (!data.taxOption.HasValue)
            issues.Add(new ComplianceIssue("Tax Option was never verified during the interview."));

        int unreviewedCount = 0;
        foreach (var doc in data.supportingDocuments)
        {
            if (!doc.isReviewed) unreviewedCount++;
        }
        if (unreviewedCount > 0)
            issues.Add(new ComplianceIssue($"{unreviewedCount} supporting document(s) were never reviewed."));

        if (data.computationStatus != ComputationStatus.Computed)
            issues.Add(new ComplianceIssue("Tax computation was never completed at the Computer."));

        if (!data.requiredForm.HasValue)
            issues.Add(new ComplianceIssue("No BIR Form was determined/filed."));
        else
        {
            RequiredForm expectedForm = DetermineExpectedForm(data.taxpayerType, data.taxOption);
            if (data.requiredForm.Value != expectedForm)
                issues.Add(new ComplianceIssue($"Incorrect BIR Form selected: filed {data.requiredForm.Value}, expected {expectedForm}."));
        }

        // ---------- Encoded Form Validation (Milestone 12.5) ----------
        if (data.filingStatus == FilingStatus.ReadyForFiling)
        {
            if (data.encodedForm == null || !data.encodedForm.isConfirmed)
            {
                issues.Add(new ComplianceIssue("No tax return form was encoded and confirmed."));
            }
            else
            {
                CheckEncodedForm(data, data.encodedForm, issues);
            }

            if (!data.hasPrintedReturn)
            {
                issues.Add(new ComplianceIssue("The tax return was never printed."));
            }
        }


        if (data.filingStatus == FilingStatus.NotReady)
            issues.Add(new ComplianceIssue("The case was never stamped with a final assessment."));

        return issues;
    }

    /// <summary>
    /// Compares every manually-typed field in EncodedFormData against the
    /// authoritative CaseData values. This is the ONLY place typed values are
    /// validated, per design goal ("The Auditor is the ONLY validation system").
    /// </summary>
    private static void CheckEncodedForm(CaseData data, EncodedFormData encoded, List<ComplianceIssue> issues)
    {
        RequiredForm expectedForm = DetermineExpectedForm(data.taxpayerType, data.taxOption);
        if (encoded.selectedForm != expectedForm)
        {
            issues.Add(new ComplianceIssue($"Wrong BIR Form encoded: filled out {encoded.selectedForm}, expected {expectedForm}."));
        }

        CheckTextField("Full Name", encoded.fullName, data.fullName, issues);
        CheckTextField("TIN", encoded.tin, data.tin, issues);

        if (!string.IsNullOrEmpty(encoded.address))
            CheckTextField("Address", encoded.address, data.address, issues);

        if (!string.IsNullOrEmpty(encoded.residencyStatus) && data.residencyStatus.HasValue)
            CheckTextField("Residency Status", encoded.residencyStatus, data.residencyStatus.ToString(), issues);

        if (!string.IsNullOrEmpty(encoded.taxpayerType) && data.taxpayerType.HasValue)
            CheckTextField("Taxpayer Type", encoded.taxpayerType, data.taxpayerType.ToString(), issues);

        if (!string.IsNullOrEmpty(encoded.incomeSource) && data.incomeSource.HasValue)
            CheckTextField("Income Source", encoded.incomeSource, data.incomeSource.ToString(), issues);

        CheckNumericField("Gross Income", encoded.grossIncome, data.grossIncome, issues);
        if (!string.IsNullOrEmpty(encoded.allowableExpenses))
            CheckNumericField("Allowable Expenses", encoded.allowableExpenses, data.allowableExpenses, issues);
        if (!string.IsNullOrEmpty(encoded.taxableIncome))
            CheckNumericField("Taxable Income", encoded.taxableIncome, data.taxableIncome, issues);
        CheckNumericField("Tax Due", encoded.taxDue, data.taxDue, issues);
        CheckNumericField("Tax Credits", encoded.taxCredits, data.taxWithheldOrCredits, issues);
        CheckNumericField("Final Tax Payable", encoded.finalTaxPayable, data.finalTaxPayable, issues);
    }

    private static void CheckTextField(string fieldName, string typed, string actual, List<ComplianceIssue> issues)
    {
        if (typed == null) typed = "";
        if (!typed.Trim().Equals(actual?.Trim(), System.StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new ComplianceIssue($"{fieldName} mismatch: entered \"{typed}\", expected \"{actual}\"."));
        }
    }

    private static void CheckNumericField(string fieldName, string typed, float actual, List<ComplianceIssue> issues)
    {
        if (!float.TryParse(typed, out float typedValue))
        {
            issues.Add(new ComplianceIssue($"{fieldName} could not be read as a number: \"{typed}\"."));
            return;
        }

        // Small tolerance for float rounding, not for actual mistakes.
        if (Mathf.Abs(typedValue - actual) > 1f)
        {
            issues.Add(new ComplianceIssue($"{fieldName} mismatch: entered {typedValue:N0}, expected {actual:N0}."));
        }
    }

    /// <summary>
    /// Same logic as ComputerUI.DetermineRequiredForm from Milestone 10
    /// Part 2, duplicated intentionally here rather than shared, since this
    /// represents the AUDITOR'S independent verification, not a call back
    /// into the player's own tool. If the two definitions ever diverge,
    /// that's actually meaningful (the auditor's rulebook vs. the
    /// consultant's calculator could theoretically disagree in a future
    /// "gotcha" scenario) — but for now they should stay in sync manually.
    /// </summary>
    private static RequiredForm DetermineExpectedForm(TaxpayerType? taxpayerType, TaxOption? taxOption)
    {
        if (taxpayerType == TaxpayerType.CompensationEarner) return RequiredForm.BIR1700;
        if (taxOption == TaxOption.EightPercentTaxRate) return RequiredForm.BIR1701A;
        return RequiredForm.BIR1701;
    }
}