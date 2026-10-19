using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Pure helpers for resuming a Level 2 review from its checkpoint (T11).
///
/// - Normalize: Unity's JsonUtility restores null class fields as blank
///   objects; a blank initial stamp would look "stamped". Blank records are
///   turned back into null here.
/// - ImpliedTriggers: the objective / guide events the restored review has
///   already passed, so the objective list catches up without replaying any
///   gameplay action (no event is raised, nothing is awarded).
/// - Field strings: the taxpayer facts the player entered (nullable enums
///   are stored as names).
/// </summary>
public static class Level2Checkpoint
{
    public const string StoryFactDeskProvenance = "rosa-desk-provenance";

    /// <summary>Editor direct Play is isolated from the durable in-progress run.</summary>
    public static bool ShouldUseCheckpoint(bool isEditor, bool isInitialOfficeScene) =>
        !isEditor || !isInitialOfficeScene;

    public static void Normalize(CaseReviewState state)
    {
        if (state == null) return;
        if (state.preliminary != null && string.IsNullOrEmpty(state.preliminary.verdict)) state.preliminary = null;
        if (state.finalStamp != null && string.IsNullOrEmpty(state.finalStamp.verdict)) state.finalStamp = null;
        if (state.acquiredDocumentIds == null) state.acquiredDocumentIds = new List<string>();
        if (state.findings == null) state.findings = new List<ReviewFindingState>();
        if (state.schedule == null) state.schedule = new List<ScheduleRowState>();
        if (state.officeFindings == null) state.officeFindings = new List<OfficeFindingState>();
        if (state.stampHistory == null) state.stampHistory = new List<FinalAssessmentState>();
        state.stampHistory.RemoveAll(s => s == null || string.IsNullOrEmpty(s.verdict));
        if (state.audit == null) state.audit = new AuditTrackState();
        if (state.audit.charged == null) state.audit.charged = new List<string>();
        if (state.clock == null) state.clock = new ReviewClockState();
        if (state.clock.warned == null) state.clock.warned = new List<float>();
        if (state.tutorialAnnotation != null && string.IsNullOrEmpty(state.tutorialAnnotation.entryId)) state.tutorialAnnotation = null;
        if (state.challenge != null)
        {
            if (state.challenge.entries == null) state.challenge.entries = new List<AnnotationEntryState>();
            state.challenge.entries.RemoveAll(e => e == null || string.IsNullOrEmpty(e.entryId));
            if (state.challenge.firstWrong == null) state.challenge.firstWrong = new List<string>();
            if (state.challenge.confirmed == null) state.challenge.confirmed = new List<string>();
            if (state.challenge.flagged == null) state.challenge.flagged = new List<string>();
        }
    }

    /// <summary>A restored state is usable: same review, and it really started.</summary>
    public static bool Usable(CaseReviewState restored, string reviewId) =>
        restored != null && !string.IsNullOrEmpty(reviewId) && restored.reviewId == reviewId &&
        restored.phase > ReviewPhase.OfficeWelcome && restored.phase < ReviewPhase.Completed;

    /// <summary>Objective triggers the restored review has already passed (in play order).</summary>
    public static List<string> ImpliedTriggers(CaseReviewState s)
    {
        var keys = new List<string>();
        if (s == null) return keys;
        void Add(bool done, string name)
        {
            if (done) keys.Add(ObjectiveTriggers.EventKey(name));
        }
        Add(s.phase >= ReviewPhase.RetrieveFolder, ObjectiveTriggers.ReceptionistTalkedTo);
        Add(s.FolderRetrieved, ObjectiveTriggers.CaseFolderRetrieved);
        if (s.folderPlacedOnDesk) keys.Add(ObjectiveTriggers.StatePrefix + "ReviewDocumentsState");
        Add(s.preliminary != null || (s.findings != null && s.findings.Exists(f => f != null && f.comparisonConfirmed)), ObjectiveTriggers.FindingRecorded);
        Add(s.preliminary != null, ObjectiveTriggers.PreliminaryHoldStamped);
        Add(s.HandoverDone, ObjectiveTriggers.RosaRecordsCollected);
        Add(s.marketDeskOpened, ObjectiveTriggers.MarketDeskOpened);
        Add(s.toolkitDemoSeen, ObjectiveTriggers.ToolkitDemoFinished);
        Add(s.tutorialCompleted, ObjectiveTriggers.AnnotationTutorialCompleted);
        Add(s.challenge != null && s.challenge.firstChecked, ObjectiveTriggers.AnnotationsChecked);
        Add(s.challenge != null && s.challenge.awarded, ObjectiveTriggers.IncomeQuestCompleted);
        Add(s.officeReviewAnnounced, ObjectiveTriggers.OfficeReviewCompleted);
        Add(s.finalStamp != null || (s.stampHistory != null && s.stampHistory.Count > 0), ObjectiveTriggers.FinalAssessmentStamped);
        return keys;
    }

    public static string EnumName<T>(T? value) where T : struct => value.HasValue ? value.Value.ToString() : string.Empty;

    public static T? ParseEnum<T>(string name) where T : struct =>
        !string.IsNullOrEmpty(name) && Enum.TryParse(name, out T parsed) && Enum.IsDefined(typeof(T), parsed) ? parsed : (T?)null;
}
