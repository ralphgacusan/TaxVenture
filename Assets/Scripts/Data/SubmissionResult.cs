using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Full result of a final audit submission — combines verdict correctness
/// with the existing ComplianceChecker field-level issue list, per R11's
/// four evaluation questions: Correct verdict? Wrong verdict? Missing
/// issues? False issues?
/// </summary>
public class SubmissionResult
{
    public bool VerdictWasCorrect;
    public CaseAssessment PlayerVerdict;
    public CaseAssessment CorrectVerdict;
    public List<ComplianceIssue> MissedIssues;   // real problems the player didn't catch/fix
    public List<ComplianceIssue> FalseIssues;    // TODO placeholder — see note below
}