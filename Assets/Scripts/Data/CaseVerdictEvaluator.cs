
/// <summary>
/// PURPOSE:
/// Determines the correct READY / NOT READY verdict for the current case.
///
/// Level 1 is focused only on completing the Case Folder.
//
/// READY means:
/// - All required taxpayer facts were gathered
/// - All supporting documents were reviewed
///
/// NOT READY means:
/// - At least one required fact is missing
/// - OR at least one supporting document has not been reviewed
///
/// Tax computation and tax return requirements are intentionally NOT
/// included in Level 1.
/// </summary>
public static class CaseVerdictEvaluator
{
    public static CaseAssessment DetermineCorrectVerdict(
        CaseData data)
    {
        if (data == null)
        {
            return CaseAssessment.NotReadyForFiling;
        }

        // =====================================================
        // REQUIRED CASE FOLDER FACTS
        // =====================================================

        bool allFactsKnown =
            data.residencyStatus.HasValue
            && data.taxpayerType.HasValue
            && data.incomeSource.HasValue
            && data.numberOfEmployers.HasValue
            && data.businessRegistration.HasValue
            && data.taxOption.HasValue;

        if (!allFactsKnown)
        {
            return CaseAssessment.NotReadyForFiling;
        }

        // =====================================================
        // SUPPORTING DOCUMENTS
        // =====================================================

        if (data.supportingDocuments == null ||
            data.supportingDocuments.Count == 0)
        {
            return CaseAssessment.NotReadyForFiling;
        }

        foreach (var doc in data.supportingDocuments)
        {
            if (doc == null || !doc.isReviewed)
            {
                return CaseAssessment.NotReadyForFiling;
            }
        }

        // =====================================================
        // EVERYTHING REQUIRED FOR LEVEL 1 IS COMPLETE
        // =====================================================

        return CaseAssessment.ReadyForFiling;
    }
}
