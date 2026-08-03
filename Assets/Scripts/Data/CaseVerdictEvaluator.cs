/// <summary>
/// PURPOSE:
/// Determines what the case's assessment SHOULD have been, based purely on
/// the case's underlying facts — completely independent of whatever the
/// player actually stamped. This is the "answer key" the Auditor checks
/// the player's stamped verdict against. Never shown to the player
/// directly; only consumed by ComplianceChecker/AuditorInteractable.
///
/// For this prototype's single hardcoded case, the correct verdict is
/// simply: Ready if every required fact was gathered and the numbers are
/// internally consistent; Not Ready otherwise. A future multi-case system
/// would compute this per-case from planted "correct" data rather than a
/// single fixed rule.
/// </summary>
public static class CaseVerdictEvaluator
{
    public static CaseAssessment DetermineCorrectVerdict(CaseData data)
    {
        bool allFactsKnown = data.residencyStatus.HasValue
            && data.taxpayerType.HasValue
            && data.incomeSource.HasValue
            && data.numberOfEmployers.HasValue
            && data.businessRegistration.HasValue
            && data.taxOption.HasValue;

        bool allDocumentsReviewed = true;
        foreach (var doc in data.supportingDocuments)
        {
            if (!doc.isReviewed) { allDocumentsReviewed = false; break; }
        }

        bool computationDone = data.computationStatus == ComputationStatus.Computed;

        return (allFactsKnown && allDocumentsReviewed && computationDone)
            ? CaseAssessment.ReadyForFiling
            : CaseAssessment.NotReadyForFiling;
    }
}