/// <summary>
/// PURPOSE:
/// Which physical stamp the player is holding/selecting. Kept separate from
/// CaseAssessment (which has 5 values: UnderReview, ReadyForComputation,
/// ReadyForFiling, NotReadyForFiling, Filed) since only two of those values
/// have a physical stamp object — this enum represents just the two
/// stampable outcomes.
/// </summary>
public enum StampType
{
    ReadyForFiling,
    NotReadyForFiling
}