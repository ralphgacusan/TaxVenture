/// <summary>
/// PURPOSE:
/// Contract for every phase of the gameplay loop described in the design
/// document (ReceiveCase, ReviewDocuments, InterviewClient, ResearchTax,
/// ComputeTaxes, AnalyzeEvidence, StampAssessment, PrepareReturn,
/// ComplianceAudit, Rewards, Outcome, Archive, CompleteCase).
///
/// WHY AN INTERFACE (same reasoning as IInteractable):
/// GameStateMachine should never need to know what a specific state DOES —
/// it only needs to call Enter/Exit/Tick on whatever the "current" state is.
/// This means adding a brand new state later (e.g. splitting ComputeTaxes
/// into two states) never requires modifying GameStateMachine.cs.
///
/// IMPLEMENTED BY:
/// - ReceiveCaseState (this milestone)
/// - ReviewDocumentsState (this milestone, stub)
/// - Future: InterviewClientState, ResearchTaxState, ComputeTaxesState,
///   AnalyzeEvidenceState, StampAssessmentState, PrepareReturnState,
///   ComplianceAuditState, RewardsState, OutcomeState, ArchiveState,
///   CompleteCaseState
/// </summary>
public interface IGameState
{
    /// <summary>Called once when the state machine switches TO this state.</summary>
    void Enter();

    /// <summary>Called once when the state machine switches AWAY from this state.</summary>
    void Exit();

    /// <summary>Called every frame while this state is active (optional per-state logic).</summary>
    void Tick();

    /// <summary>
    /// Human-readable name for this state, used for debug display and
    /// potentially save data / UI (e.g. To-Do List headers).
    /// </summary>
    string StateName { get; }
}