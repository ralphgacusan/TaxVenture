/// <summary>
/// Determines the correct READY / NOT READY verdict for the current case:
/// READY once every required field (the case JSON's requiredFields, or the
/// Level 1 defaults) has a value.
/// </summary>
public static class CaseVerdictEvaluator
{
    public static CaseAssessment DetermineCorrectVerdict(CaseData data) =>
        DetermineCorrectVerdict(data, CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null);

    public static CaseAssessment DetermineCorrectVerdict(CaseData data, CaseDefinition definition)
    {
        if (data == null)
        {
            return CaseAssessment.NotReadyForFiling;
        }

        var fields = definition != null ? definition.GradedFields() : CaseFieldRules.Defaults();
        foreach (RequiredFieldDefinition field in fields)
        {
            if (CaseFieldRules.PlayerValue(data, field.key) == null)
            {
                return CaseAssessment.NotReadyForFiling;
            }
        }

        // =====================================================
        // ALL REQUIRED FACTS ARE COMPLETE
        // =====================================================

        return CaseAssessment.ReadyForFiling;
    }
}
