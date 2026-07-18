/// <summary>
/// PURPOSE:
/// One detected problem with the case, found by ComplianceChecker. Plain
/// data — just a human-readable description of what's wrong, matching the
/// design doc's Section 12 Compliance Checklist items (e.g. "Residency
/// Status Determined").
/// </summary>
public class ComplianceIssue
{
    public string Description;

    public ComplianceIssue(string description)
    {
        Description = description;
    }
}