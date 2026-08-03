/// <summary>
/// PURPOSE:
/// The four possible outcomes of the Client Result conversation, per R12.
/// Determined purely from CaseData.finalVerdictWasCorrect /
/// finalMissedIssueCount — no new evaluation logic, just a classification
/// of facts the Auditor already established.
/// </summary>
public enum ClientOutcomeBranch
{
    CorrectNoIssues,
    CorrectWithIssues,
    WrongVerdict,
    WrongVerdictWithIssues
}

public static class ClientOutcomeEvaluator
{
    public static ClientOutcomeBranch Determine(CaseData data)
    {
        bool correct = data.finalVerdictWasCorrect;
        bool hasIssues = data.finalMissedIssueCount > 0;

        if (correct && !hasIssues) return ClientOutcomeBranch.CorrectNoIssues;
        if (correct && hasIssues) return ClientOutcomeBranch.CorrectWithIssues;
        if (!correct && !hasIssues) return ClientOutcomeBranch.WrongVerdict;
        return ClientOutcomeBranch.WrongVerdictWithIssues;
    }
}