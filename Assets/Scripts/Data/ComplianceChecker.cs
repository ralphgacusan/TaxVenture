using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Checks whether the Case Folder was properly completed for Level 1.
///
/// Level 1 ONLY checks:
/// - Required taxpayer facts were gathered
/// - Supporting documents were reviewed
///
/// Level 1 does NOT check:
/// - Tax computation
/// - Tax return encoding
/// - BIR form selection
/// - Printing
/// - Tax payable
///
/// The player's READY / NOT READY verdict is checked separately by
/// CaseVerdictEvaluator.
/// </summary>
public static class ComplianceChecker
{
    public static List<ComplianceIssue> RunCheck(CaseData data)
    {
        var issues = new List<ComplianceIssue>();

        if (data == null)
        {
            issues.Add(
                new ComplianceIssue("No case data was available.")
            );

            return issues;
        }

        // =====================================================
        // CASE FOLDER FACTS
        // =====================================================

        if (!data.residencyStatus.HasValue)
        {
            issues.Add(
                new ComplianceIssue(
                    "Residency status was not verified."
                )
            );
        }

        if (!data.taxpayerType.HasValue)
        {
            issues.Add(
                new ComplianceIssue(
                    "Taxpayer type was not verified."
                )
            );
        }

        if (!data.incomeSource.HasValue)
        {
            issues.Add(
                new ComplianceIssue(
                    "Income source was not verified."
                )
            );
        }

        if (!data.numberOfEmployers.HasValue)
        {
            issues.Add(
                new ComplianceIssue(
                    "Number of employers was not verified."
                )
            );
        }

        if (!data.businessRegistration.HasValue)
        {
            issues.Add(
                new ComplianceIssue(
                    "Business registration was not verified."
                )
            );
        }

        if (!data.taxOption.HasValue)
        {
            issues.Add(
                new ComplianceIssue(
                    "Tax option was not verified."
                )
            );
        }

        // =====================================================
        // SUPPORTING DOCUMENTS
        // =====================================================

        if (data.supportingDocuments == null ||
            data.supportingDocuments.Count == 0)
        {
            issues.Add(
                new ComplianceIssue(
                    "No supporting documents were reviewed."
                )
            );
        }
        else
        {
            int unreviewedCount = 0;

            foreach (var doc in data.supportingDocuments)
            {
                if (doc == null || !doc.isReviewed)
                {
                    unreviewedCount++;
                }
            }

            if (unreviewedCount > 0)
            {
                issues.Add(
                    new ComplianceIssue(
                        "Some supporting documents were not reviewed."
                    )
                );
            }
        }

        // =====================================================
        // IMPORTANT:
        //
        // No tax computation checks.
        // No BIR form checks.
        // No encoded return checks.
        // No printed return checks.
        //
        // Level 1 ends with the Case Folder + verdict.
        // =====================================================

        return issues;
    }
}
