using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

public static class ComplianceChecker
{
    public static List<ComplianceIssue> RunCheck(CaseData data)
    {
        var issues = new List<ComplianceIssue>();

        if (!data.residencyStatus.HasValue)
            issues.Add(new ComplianceIssue("Residency status was not verified."));

        if (!data.taxpayerType.HasValue)
            issues.Add(new ComplianceIssue("Taxpayer type was not verified."));

        if (!data.incomeSource.HasValue)
            issues.Add(new ComplianceIssue("Income source was not verified."));

        if (!data.businessRegistration.HasValue)
            issues.Add(new ComplianceIssue("Business registration was not verified."));

        if (!data.taxOption.HasValue)
            issues.Add(new ComplianceIssue("Tax option was not verified."));

        int unreviewedCount = 0;
        foreach (var doc in data.supportingDocuments)
        {
            if (!doc.isReviewed) unreviewedCount++;
        }
        if (unreviewedCount > 0)
            issues.Add(new ComplianceIssue("Some supporting documents were not reviewed."));

        if (data.computationStatus != ComputationStatus.Computed)
            issues.Add(new ComplianceIssue("Tax computation was not completed."));

        RequiredForm expectedForm = DetermineExpectedForm(data.taxpayerType, data.taxOption);

        if (!data.requiredForm.HasValue)
        {
            issues.Add(new ComplianceIssue("No BIR form was determined."));
        }
        else if (data.requiredForm.Value != expectedForm)
        {
            issues.Add(new ComplianceIssue("The wrong BIR form appears to have been filed."));
        }

        if (data.filingStatus == FilingStatus.NotReady)
            issues.Add(new ComplianceIssue("The case was never given a final assessment."));

        if (data.filingStatus == FilingStatus.ReadyForFiling)
        {
            if (data.encodedForm == null || !data.encodedForm.isConfirmed)
            {
                issues.Add(new ComplianceIssue("The tax return form was never completed."));
            }
            else
            {
                CheckEncodedForm(data, data.encodedForm, expectedForm, issues);
            }

            if (!data.hasPrintedReturn)
            {
                issues.Add(new ComplianceIssue("The tax return was never printed."));
            }
        }

        return issues;
    }

    private static void CheckEncodedForm(CaseData data, EncodedFormData encoded, RequiredForm expectedForm, List<ComplianceIssue> issues)
    {
        if (encoded.selectedForm != expectedForm)
            issues.Add(new ComplianceIssue("The wrong BIR form appears to have been encoded."));

        CheckTextField("An issue was found with the taxpayer's name.", encoded.fullName, data.fullName, issues);
        CheckTextField("An issue was found with the TIN entered.", encoded.tin, data.tin, issues);

        if (!string.IsNullOrEmpty(encoded.address))
            CheckTextField("An issue was found with the address entered.", encoded.address, data.address, issues);

        if (!string.IsNullOrEmpty(encoded.residencyStatus) && data.residencyStatus.HasValue)
            CheckEnumField(
                "An issue was found with the residency status entered.",
                encoded.residencyStatus,
                data.residencyStatus,
                issues);
        if (!string.IsNullOrEmpty(encoded.taxpayerType) && data.taxpayerType.HasValue)
            CheckEnumField(
                "An issue was found with the taxpayer type entered.",
                encoded.taxpayerType,
                data.taxpayerType,
                issues);

        if (!string.IsNullOrEmpty(encoded.incomeSource) && data.incomeSource.HasValue)
            CheckEnumField(
                "An issue was found with the income source entered.",
                encoded.incomeSource,
                data.incomeSource,
                issues);
        CheckNumericField("The declared income does not match the supporting records.", encoded.grossIncome, data.grossIncome, issues);

        if (!string.IsNullOrEmpty(encoded.allowableExpenses))
            CheckNumericField("An issue was found with the declared expenses.", encoded.allowableExpenses, data.allowableExpenses, issues);

        if (!string.IsNullOrEmpty(encoded.taxableIncome))
            CheckNumericField("An issue was found with the taxable income entered.", encoded.taxableIncome, data.taxableIncome, issues);

        CheckNumericField("An issue was found with the declared tax due.", encoded.taxDue, data.taxDue, issues);
        CheckNumericField("An issue was found with the tax credits entered.", encoded.taxCredits, data.taxWithheldOrCredits, issues);
        CheckNumericField("An issue was found with the final tax payable.", encoded.finalTaxPayable, data.finalTaxPayable, issues);
    }
    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        text = text.Trim();

        // Replace multiple spaces with a single space
        text = Regex.Replace(text, @"\s+", " ");

        return text.ToLowerInvariant();
    }

    // Not so strict: if the player typed a name or TIN that is slightly off (typo, extra space, etc.), we don't want to mark it as a compliance issue. Instead, we normalize both the typed and actual values and compare them.
    // private static void CheckTextField(string vagueLabel, string typed, string actual, List<ComplianceIssue> issues)
    // {
    //     if (NormalizeText(typed) != NormalizeText(actual))
    //     {
    //         issues.Add(new ComplianceIssue(vagueLabel));
    //     }
    // }

    private static void CheckTextField(string vagueLabel, string typed, string actual, List<ComplianceIssue> issues)
    {
        if ((typed ?? "").Trim() != (actual ?? "").Trim())
        {
            issues.Add(new ComplianceIssue(vagueLabel));
        }
    }

    private static void CheckNumericField(string vagueLabel, string typed, float actual, List<ComplianceIssue> issues)
    {
        if (!float.TryParse(typed, out float typedValue))
        {
            issues.Add(new ComplianceIssue(vagueLabel));
            return;
        }

        if (Mathf.Abs(typedValue - actual) > 1f)
        {
            issues.Add(new ComplianceIssue(vagueLabel));
        }
    }

    private static RequiredForm DetermineExpectedForm(TaxpayerType? taxpayerType, TaxOption? taxOption)
    {
        if (taxpayerType == TaxpayerType.CompensationEarner) return RequiredForm.BIR1700;
        if (taxOption == TaxOption.EightPercentTaxRate) return RequiredForm.BIR1701A;
        return RequiredForm.BIR1701;
    }

    private static void CheckEnumField<T>(
        string vagueLabel,
        string typed,
        T? actual,
        List<ComplianceIssue> issues)
        where T : struct, System.Enum
    {
        if (!actual.HasValue)
        {
            issues.Add(new ComplianceIssue(vagueLabel));
            return;
        }

        if (!System.Enum.TryParse<T>(typed, out var typedEnum))
        {
            issues.Add(new ComplianceIssue(vagueLabel));
            return;
        }

        if (!typedEnum.Equals(actual.Value))
        {
            issues.Add(new ComplianceIssue(vagueLabel));
        }
    }
}