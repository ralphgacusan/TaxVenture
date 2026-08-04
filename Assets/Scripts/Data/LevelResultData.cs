/// <summary>
/// PURPOSE:
/// Complete, self-contained record of one level's outcome — time, verdict,
/// issues, and final rewards. Built once by RewardsCalculator and consumed
/// by LevelResultPopupUI for display. Also the intended foundation for a
/// future achievement system: any achievement check just inspects this one
/// object rather than needing to re-derive facts from CaseData/scattered
/// state.
/// </summary>
public class LevelResultData
{
    public ClientOutcomeBranch Branch;
    public string FormattedCompletionTime;
    public CaseAssessment PlayerVerdict;
    public CaseAssessment CorrectVerdict;
    public bool VerdictWasCorrect;
    public int IssuesFound;
    public int ExpEarned;
    public int ReputationEarned;
    public string SummaryText;
}