using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Checks whether the three required taxpayer facts were gathered.
///
/// Level 1 ONLY checks:
/// - Residency status
/// - Taxpayer type
/// - Income source
///
/// Only actual missing information is added to the issues list.
/// </summary>
public static class ComplianceChecker
{
    public static List<ComplianceIssue> RunCheck(CaseData data)
    {
        var issues = new List<ComplianceIssue>();

        if (data == null)
        {
            issues.Add(
                new ComplianceIssue(
                    "The case information is unavailable. Review the case again."
                )
            );

            return issues;
        }

        // =====================================================
        // REQUIRED TAXPAYER FACTS
        // =====================================================

        if (!data.residencyStatus.HasValue)
        {
            issues.Add(
                new ComplianceIssue(
                    "Review the taxpayer's background and living situation."
                )
            );
        }

        if (!data.taxpayerType.HasValue)
        {
            issues.Add(
                new ComplianceIssue(
                    "Look for details that help identify the taxpayer's classification."
                )
            );
        }

        if (!data.incomeSource.HasValue)
        {
            issues.Add(
                new ComplianceIssue(
                    "Review how the taxpayer earns income."
                )
            );
        }

        return issues;
    }
}