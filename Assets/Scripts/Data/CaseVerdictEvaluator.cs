/// <summary>
/// Determines the correct READY / NOT READY verdict for the current case.
///
/// Level 1 checks only the three required taxpayer facts.
/// </summary>
public static class CaseVerdictEvaluator
{
    public static CaseAssessment DetermineCorrectVerdict(CaseData data)
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
            && data.incomeSource.HasValue;

        if (!allFactsKnown)
        {
            return CaseAssessment.NotReadyForFiling;
        }

        // =====================================================
        // ALL THREE REQUIRED FACTS ARE COMPLETE
        // =====================================================

        return CaseAssessment.ReadyForFiling;
    }
}