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

    public static bool EthicsChoiceCommitted(CaseReviewState review, string caseId) =>
        review != null && review.rosaEthicsCommitted && string.Equals(review.rosaEthicsCaseId, caseId, StringComparison.Ordinal);

    public static string EthicsStatus(CaseReviewState review)
    {
        if (review == null || !review.rosaEthicsCommitted) return "CHOICE NOT RECORDED";
        if (review.rosaEthicsChoiceId.EndsWith("/responsible", StringComparison.Ordinal))
            return review.rosaEthicsFollowThroughDone ? Level2CampaignRules.ResponsibleFollowThroughStatus : Level2CampaignRules.ResponsibleStatus;
        if (review.rosaEthicsChoiceId.EndsWith("/unsupported-promise", StringComparison.Ordinal))
            return review.rosaEthicsFollowThroughDone ? Level2CampaignRules.UnsupportedStatus : "PROMISE RECORDED — SUPPORT CHECK PENDING";
        return Level2CampaignRules.DismissiveStatus;
    }

    public static void SetEthicsChoice(CaseReviewState review, Level2EthicsChoice choice, string caseId)
    {
        if (review == null) return;
        review.rosaEthicsCaseId = caseId ?? "";
        review.rosaEthicsCommitmentId = Level2CampaignRules.CommitmentId(caseId);
        review.rosaEthicsChoiceId = choice == Level2EthicsChoice.Responsible ? $"rosa-ethics/{caseId}/responsible" :
            choice == Level2EthicsChoice.UnsupportedPromise ? $"rosa-ethics/{caseId}/unsupported-promise" :
            choice == Level2EthicsChoice.DismissiveHold ? $"rosa-ethics/{caseId}/dismissive-hold" : "";
        review.rosaEthicsChoiceText = Level2CampaignRules.ChoiceText(choice);
        review.rosaEthicsCommitted = choice != Level2EthicsChoice.None;
    }

    /// <summary>The signed campaign is canonical if a checkpoint snapshot is stale.</summary>
    public static void SyncEthicsReview(PlayerProgressData data, CaseReviewState review, string caseId)
    {
        if (review == null || !Level2CampaignRules.EthicsChoiceCommitted(data, caseId)) return;
        RosaEthicsRecord ethics = data.campaign.rosaEthics;
        review.rosaEthicsCaseId = ethics.caseId;
        review.rosaEthicsCommitmentId = ethics.commitmentId;
        review.rosaEthicsChoiceId = ethics.choiceId;
        review.rosaEthicsChoiceText = ethics.choiceText;
        review.rosaEthicsCommitted = ethics.committed;
        review.rosaEthicsFollowThroughDone = ethics.followThroughDone;
    }

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
        if (state.consultation == null) state.consultation = new Level1ConsultationRecord();
        state.consultation.Repair();
        if (state.roomMomentsSaid == null) state.roomMomentsSaid = new List<string>();
        state.roomMomentsSaid.RemoveAll(string.IsNullOrWhiteSpace);
        state.rosaLeg = RosaTripRules.ToSaved(RosaTripRules.Parse(state.rosaLeg));
        state.clientCopyPhase = ClientCopyRules.ToSaved(ClientCopyRules.Parse(state.clientCopyPhase));
        // A copy exists only for an approved review.
        if (state.audit == null || !state.audit.approved) state.clientCopyPhase = ClientCopyRules.ToSaved(ClientCopyPhase.None);
        if (state.tutorialAnnotation != null && string.IsNullOrEmpty(state.tutorialAnnotation.entryId)) state.tutorialAnnotation = null;
        if (state.rosaEthicsCaseId == null) state.rosaEthicsCaseId = "";
        if (state.rosaEthicsCommitmentId == null) state.rosaEthicsCommitmentId = "";
        if (state.rosaEthicsChoiceId == null) state.rosaEthicsChoiceId = "";
        if (state.rosaEthicsChoiceText == null) state.rosaEthicsChoiceText = "";
        if (state.challenge != null)
        {
            if (state.challenge.entries == null) state.challenge.entries = new List<AnnotationEntryState>();
            state.challenge.entries.RemoveAll(e => e == null || string.IsNullOrEmpty(e.entryId));
            if (state.challenge.firstWrong == null) state.challenge.firstWrong = new List<string>();
            if (state.challenge.confirmed == null) state.challenge.confirmed = new List<string>();
            if (state.challenge.flagged == null) state.challenge.flagged = new List<string>();
        }
    }

    /// <summary>A restored state is usable: same review, and it really started (an approved one only while Rosa's copy is still to deliver).</summary>
    public static bool Usable(CaseReviewState restored, string reviewId) =>
        restored != null && !string.IsNullOrEmpty(reviewId) && restored.reviewId == reviewId &&
        restored.phase > ReviewPhase.OfficeWelcome && (restored.phase < ReviewPhase.Completed || KeepsHandover(restored));

    /// <summary>
    /// The approved review still owes its client the Client Copy (beside the Auditor or carried):
    /// the checkpoint is kept for exactly this, so a resume puts the copy back and nothing is done twice.
    /// </summary>
    public static bool KeepsHandover(CaseReviewState s)
    {
        if (s == null || s.audit == null || !s.audit.approved) return false;
        ClientCopyPhase phase = ClientCopyRules.Parse(s.clientCopyPhase);
        return phase == ClientCopyPhase.AtAuditor || phase == ClientCopyPhase.Carried;
    }

    /// <summary>The review needs a checkpoint now: work in progress, or an approved case whose copy is still owed.</summary>
    public static bool NeedsCheckpoint(CaseReviewState s) =>
        s != null && s.phase >= ReviewPhase.RetrieveFolder &&
        (KeepsHandover(s) || (s.phase < ReviewPhase.Completed && (s.audit == null || !s.audit.approved)));

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
        Add(s.finalStamp != null && FinalAssessment.IsCurrent(s), ObjectiveTriggers.FinalAssessmentStamped);
        if (s.audit != null && s.audit.approved) keys.Add(ObjectiveTriggers.StatePrefix + "CaseCompleteState");
        ClientCopyPhase copy = ClientCopyRules.Parse(s.clientCopyPhase);
        Add(copy == ClientCopyPhase.Carried || copy == ClientCopyPhase.Done, ObjectiveTriggers.ClientCopyPickedUp);
        Add(copy == ClientCopyPhase.Done, ObjectiveTriggers.ClientCopyHandedOver);
        return keys;
    }

    /// <summary>Past tool lessons inferred from durable review milestones, never a new acquisition or reward.</summary>
    public static List<string> ImpliedLessons(CaseReviewState s)
    {
        var done = new List<string>();
        if (s == null) return done;
        void Add(bool reached, params string[] ids) { if (reached) done.AddRange(ids); }
        Add(s.phase >= ReviewPhase.RetrieveFolder, "welcome");
        Add(s.FolderRetrieved, "to-cabinet", "file-room", "to-desk");
        Add(s.folderPlacedOnDesk, "desk-open");
        bool finding = s.preliminary != null || (s.findings != null && s.findings.Exists(f => f != null && f.comparisonConfirmed));
        Add(finding, "profile", "prepared-pages", "findings-paper", "file-findings");
        Add(s.preliminary != null, "initial-stamp");
        Add(s.HandoverDone, "to-rosa");
        Add(s.marketDeskOpened, "records-desk");
        Add(s.tutorialCompleted, "toolkit-colours", "toolkit-practice");
        Add(s.challenge != null && s.challenge.firstChecked, "mark-the-money");
        Add(s.challenge != null && s.challenge.awarded, "mark-the-money-fix");
        Add(s.phase >= ReviewPhase.OfficeReconciliation, "annotated-papers");
        Add(s.officeReviewAnnounced, "office-review");
        Add(s.finalStamp != null && FinalAssessment.IsCurrent(s), "final-assessment");
        Add(s.audit != null && s.audit.approved, "to-auditor", "audit-returned");
        return done;
    }

    /// <summary>
    /// T18: the review as the campaign rules see it. The income review counts
    /// as corrected only once Mark the Money! is mastered and its award given;
    /// approval is the Auditor's recorded decision. Never an answer key.
    /// </summary>
    public static Level2CaseFacts Facts(CaseReviewState s, string caseId)
    {
        if (s == null) return default;
        ChallengeState challenge = s.challenge;
        return new Level2CaseFacts
        {
            HasReview = true,
            CaseId = caseId ?? "",
            QuestCorrected = challenge != null && challenge.mastered && challenge.awarded,
            FirstChecked = challenge != null && challenge.firstChecked,
            FirstCorrect = challenge != null ? challenge.firstCorrect : 0,
            FirstTotal = challenge != null ? challenge.firstTotal : 0,
            Approved = s.audit != null && s.audit.approved
        };
    }

    public static string EnumName<T>(T? value) where T : struct => value.HasValue ? value.Value.ToString() : string.Empty;

    public static T? ParseEnum<T>(string name) where T : struct =>
        !string.IsNullOrEmpty(name) && Enum.TryParse(name, out T parsed) && Enum.IsDefined(typeof(T), parsed) ? parsed : (T?)null;
}
