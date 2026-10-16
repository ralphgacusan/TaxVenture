/// <summary>
/// PURPOSE:
/// One detected problem with the case. ShortLabel is intentionally VAGUE —
/// it names WHAT is wrong, never WHY, never the expected value, per design
/// requirement that the Auditor identifies issues without teaching or
/// revealing corrections.
/// </summary>
public class ComplianceIssue
{
    public string ShortLabel;

    /// <summary>The CaseData field this issue is about ("residencyStatus", "taxpayerType", "incomeSource"), or null.</summary>
    public string FieldKey;

    public ComplianceIssue(string shortLabel, string fieldKey = null)
    {
        ShortLabel = shortLabel;
        FieldKey = fieldKey;
    }
}