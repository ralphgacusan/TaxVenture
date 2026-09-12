using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// The ordered list of tasks for the whole case, each tied to the IGameState
/// that represents it. This is the ONE place to edit task wording — same
/// "data lives outside the UI script" pattern as DocumentDataProvider,
/// InterviewDataProvider, etc.
///
/// CONNECTS WITH:
/// - NotesPanelUI: reads this list and cross-references GameStateMachine's
///   current/passed states to render completed/active/pending tasks.
/// </summary>
public static class TaskListProvider
{
    public static List<TaskDefinition> GetTasks()
    {
        return new List<TaskDefinition>
        {
            new TaskDefinition(typeof(CaseOutcomeState), "Main Objective: Finish Tutorial and Unguided Case"),

            new TaskDefinition(typeof(ReceiveCaseState), "Meet with the receptionist and receive today's case."),
            new TaskDefinition(typeof(ReviewDocumentsState), "Review the case folder and supporting documents."),
            new TaskDefinition(typeof(InterviewClientState), "Interview the client to verify missing information."),
            new TaskDefinition(typeof(ResearchTaxState), "Research the applicable tax laws using the Tax Code Book."),
            new TaskDefinition(typeof(ComputeTaxesState), "Compute the client's tax liability using the Computer."),
            new TaskDefinition(typeof(AnalyzeEvidenceState), "Analyze the evidence at the corkboard and determine the case assessment."),
            new TaskDefinition(typeof(StampAssessmentState), "Stamp the case with its final assessment."),
            new TaskDefinition(typeof(PrepareReturnState), "Prepare and print the client's tax return."),
            new TaskDefinition(typeof(ComplianceAuditState), "Submit the case for compliance audit."),
            new TaskDefinition(typeof(CaseOutcomeState), "Present the completed findings to the client."),
            new TaskDefinition(typeof(ArchiveCaseState), "Archive the completed case in the filing cabinet."),
        };
    }
}