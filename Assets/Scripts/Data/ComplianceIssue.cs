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

    public ComplianceIssue(string shortLabel)
    {
        ShortLabel = shortLabel;
    }
}