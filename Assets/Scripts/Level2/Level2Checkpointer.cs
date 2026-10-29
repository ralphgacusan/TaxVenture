using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Saves and resumes Rosa's review (Level 2, T11), under
/// Level2Gameplay/Checkpoint. One optional checkpoint inside the existing
/// signed save (PlayerProgressManager / SaveFileStore), never a second file.
///
/// SAVE: at every milestone (retrieval, finding, initial stamp, records,
/// tutorial, first check, quest award, office review, final stamp, audit
/// result, day rollover, Rosa's conference-room visits), every 30 s while the
/// review changes, and when the app is paused or closed. Once the case is
/// approved the level result clears the checkpoint; the Client Copy step
/// saves it again (approved, copy beside the Auditor or carried) until Rosa
/// has her copy, when it ends (the story follow-through write, or here).
///
/// RESUME (relaunch into Level 2 with a checkpoint for this case): the review
/// (phase, records, quest answers and frozen grade, schedule, findings, stamps,
/// audit history, clock day/time) and the taxpayer facts are put back. Then
/// presentation is rebuilt without replaying actions: the folder goes back on
/// the desk or into the folder icon, the records sit in the stall tray, the
/// objective list catches up, and the level score gets back what was already
/// earned (Rosa's talk, the quest award) plus the audit counters and late
/// flag, so nothing is awarded twice and no penalty is forgotten.
///
/// CAMPAIGN (T18): in a story run every save is one milestone commit
/// (PlayerProgressManager.CommitLevelTwoCheckpoint): the checkpoint and the
/// claims it has earned (the corrected income review) are written together;
/// a failed write changes neither and the next save retries. The live review
/// is offered to the save owner as plain facts (Level2Checkpoint.Facts).
/// </summary>
public sealed class Level2Checkpointer : MonoBehaviour
{
    public const string ChildName = "Checkpoint";
    private const float SaveEvery = 30f;

    private static Scene directEditorScene;

    // Direct Play is a fresh, temporary attempt. Entering through the menu or
    // launching a player build retains the normal checkpoint/resume contract.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPlaySession() => directEditorScene = default;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CaptureInitialScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isEditor && OfficeLevelProfile.LevelIdFor(scene) == OfficeLevelCatalog.Level2Id)
            directEditorScene = scene;
    }

    public static bool IsDirectEditorAttempt => Application.isEditor && directEditorScene.IsValid() && directEditorScene.isLoaded;

    /// <summary>The checkpointer of the loaded Level 2 (null elsewhere).</summary>
    public static Level2Checkpointer Active { get; private set; }

    /// <summary>The resume (or the fresh start) has finished: the HUD may reconcile its items now.</summary>
    public bool IsReady => restoredOrFresh;

    /// <summary>This run resumes a saved review (its items stay as saved).</summary>
    public bool Resumed { get; private set; }

    private bool UsesCheckpoint => Level2Checkpoint.ShouldUseCheckpoint(Application.isEditor,
        directEditorScene.IsValid() && gameObject.scene == directEditorScene);

    private bool settingsExit;
    private bool restoredOrFresh;
    private float nextSave;
    private int lastRevision = -1;
    private ReviewPhase lastPhase;
    private int lastDay;

    private static CaseReviewState Review => Level2OfficeStart.Review;

    private static string LevelId => OfficeLevelCatalog.Level2Id;

    private static string CurrentCaseId => CaseManager.Instance != null && CaseManager.Instance.CurrentDefinition != null ? CaseManager.Instance.CurrentDefinition.caseId : null;

    private void OnEnable()
    {
        Active = this;
        PlayerProgressManager.LevelTwoFacts = CurrentFacts;
        GameplayEvents.OnCaseFolderRetrieved += SaveNow;
        GameplayEvents.OnFindingRecorded += SaveNow;
        GameplayEvents.OnPreliminaryHoldStamped += SaveNow;
        GameplayEvents.OnRosaRecordsCollected += SaveNow;
        GameplayEvents.OnMarketDeskOpened += SaveNow;
        GameplayEvents.OnAnnotationTutorialCompleted += SaveNow;
        GameplayEvents.OnAnnotationsChecked += SaveNow;
        GameplayEvents.OnIncomeQuestCompleted += SaveNow;
        GameplayEvents.OnOfficeReviewCompleted += SaveNow;
        GameplayEvents.OnFinalAssessmentStamped += SaveNow;
        GameplayEvents.OnAuditReturned += SaveNow;
        GameplayEvents.OnReviewDayRolled += SaveNow;
    }

    private void OnDisable()
    {
        if (Active == this) Active = null;
        if (PlayerProgressManager.LevelTwoFacts == (System.Func<Level2CaseFacts>)CurrentFacts) PlayerProgressManager.LevelTwoFacts = null;
        GameplayEvents.OnCaseFolderRetrieved -= SaveNow;
        GameplayEvents.OnFindingRecorded -= SaveNow;
        GameplayEvents.OnPreliminaryHoldStamped -= SaveNow;
        GameplayEvents.OnRosaRecordsCollected -= SaveNow;
        GameplayEvents.OnMarketDeskOpened -= SaveNow;
        GameplayEvents.OnAnnotationTutorialCompleted -= SaveNow;
        GameplayEvents.OnAnnotationsChecked -= SaveNow;
        GameplayEvents.OnIncomeQuestCompleted -= SaveNow;
        GameplayEvents.OnOfficeReviewCompleted -= SaveNow;
        GameplayEvents.OnFinalAssessmentStamped -= SaveNow;
        GameplayEvents.OnAuditReturned -= SaveNow;
        GameplayEvents.OnReviewDayRolled -= SaveNow;
    }

    private IEnumerator Start()
    {
        float until = Time.unscaledTime + 8f;
        while (Time.unscaledTime < until && (CaseProgressionManager.Instance == null || !CaseProgressionManager.Instance.LevelStarted))
            yield return null;
        // Let the other Level 2 parts build first (stall, toolkit, HUD).
        yield return null;
        yield return null;
        try
        {
            if (UsesCheckpoint) TryRestore();
            else Debug.Log("[Level2] Fresh direct Play attempt. The saved checkpoint is preserved; enter through the career board to resume it.");
            if (UsesCheckpoint && !IsDirectEditorAttempt && PlayerProgressManager.Instance != null && PlayerProgressManager.Instance.UsesLevelTwoStory)
            {
                PlayerProgressManager progress = PlayerProgressManager.Instance;
                Level2Checkpoint.SyncEthicsReview(progress != null ? progress.CurrentProgress : null, Review, CurrentCaseId);
            }
        }
        catch (System.Exception error)
        {
            Debug.LogError("[Level2] Could not resume the checkpoint; starting fresh. " + error.Message);
        }
        restoredOrFresh = true;
        nextSave = Time.unscaledTime + SaveEvery;
    }

    private void Update()
    {
        if (!restoredOrFresh || Time.unscaledTime < nextSave) return;
        nextSave = Time.unscaledTime + SaveEvery;
        CaseReviewState review = Level2OfficeStart.Review;
        if (review == null) return;
        bool changed = review.packetRevision != lastRevision || review.phase != lastPhase ||
                       (review.clock != null && review.clock.day != lastDay) || (review.clock != null && review.clock.started);
        if (changed) SaveNow();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveNow();
    }

    private void OnApplicationQuit() => SaveNow();

    // =========================================================
    // SAVE
    // =========================================================

    public bool PrepareSettingsExit(bool replay)
    {
        if (replay)
        {
            if (PlayerProgressManager.Instance == null ||
                !PlayerProgressManager.Instance.ClearCheckpoint(LevelId)) return false;
        }
        else
        {
            if (!SaveCurrentCheckpoint()) return false;
        }
        settingsExit = true;
        return true;
    }

    private void SaveNow() => SaveCurrentCheckpoint();

    /// <summary>A Level 2 milestone outside the review events (Rosa's visits, the Client Copy): saved now. False when the write failed.</summary>
    public bool RequestSave() => SaveCurrentCheckpoint();

    private static Level2CaseFacts CurrentFacts() => Level2Checkpoint.Facts(Level2OfficeStart.Review, CurrentCaseId);

    private bool SaveCurrentCheckpoint()
    {
        if (settingsExit || !UsesCheckpoint) return true;
        if (!restoredOrFresh) return false;
        CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        CaseReviewState review = data?.review;
        PlayerProgressManager progress = PlayerProgressManager.Instance;
        if (!Level2Checkpoint.NeedsCheckpoint(review))
        {
            // Rosa has her copy: the checkpoint kept for it ends (a story run's follow-through write already ended it).
            if (review != null && ClientCopyRules.Parse(review.clientCopyPhase) == ClientCopyPhase.Done &&
                progress != null && progress.GetCheckpoint(LevelId) != null)
                return progress.ClearCheckpoint(LevelId);
            return true;
        }
        if (progress == null) return false;
        PlayerProgressManager.StoryGrants grants = progress.CommitLevelTwoCheckpoint(Capture(data, review.clock), CurrentFacts());
        if (grants.Saved)
        {
            lastRevision = review.packetRevision;
            lastPhase = review.phase;
            lastDay = review.clock != null ? review.clock.day : 1;
            // A newly earned claim shows once, after it is saved (never on restore or a repeat).
            HudFeedback.AnnounceCampaign(grants.Trust, grants.Coins, null);
        }
        return grants.Saved;
    }

    /// <summary>
    /// T18: the complete checkpoint of this moment with another clock in it
    /// (a voucher's extended clock), for a command that saves it together with
    /// its own change. Null when this run keeps no checkpoint.
    /// </summary>
    public LevelCheckpoint CaptureForCommit(ReviewClockState clock)
    {
        if (settingsExit || !UsesCheckpoint || !restoredOrFresh) return null;
        CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        if (data?.review == null || data.review.phase < ReviewPhase.RetrieveFolder || data.review.phase >= ReviewPhase.Completed) return null;
        return Capture(data, clock);
    }

    /// <summary>Builds the exact choice checkpoint without mutating the live review.</summary>
    public LevelCheckpoint CaptureEthicsChoice(Level2EthicsChoice choice, string caseId)
    {
        if (settingsExit || !UsesCheckpoint || !restoredOrFresh) return null;
        CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        if (data?.review == null || data.review.phase < ReviewPhase.Challenge || data.review.phase >= ReviewPhase.Completed ||
            CurrentCaseId != caseId || data.review.rosaEthicsCommitted) return null;
        CaseReviewState candidateReview = JsonUtility.FromJson<CaseReviewState>(JsonUtility.ToJson(data.review));
        Level2Checkpoint.SetEthicsChoice(candidateReview, choice, caseId);
        return Capture(data, candidateReview, data.review.clock);
    }

    /// <summary>Preserves the approved case and copy position in the result's signed write.</summary>
    public LevelCheckpoint CaptureApprovedClientCopy()
    {
        if (settingsExit || !UsesCheckpoint || !restoredOrFresh) return null;
        CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        if (data?.review?.audit == null || !data.review.audit.approved || CurrentCaseId == null) return null;
        CaseReviewState candidateReview = JsonUtility.FromJson<CaseReviewState>(JsonUtility.ToJson(data.review));
        ClientCopyPhase savedPhase = ClientCopyRules.Parse(data.review.clientCopyPhase);
        candidateReview.clientCopyPhase = ClientCopyRules.ToSaved(savedPhase == ClientCopyPhase.Carried
            ? ClientCopyPhase.Carried : ClientCopyPhase.AtAuditor);
        return Capture(data, candidateReview, data.review.clock);
    }

    /// <summary>Choice for a development/replay attempt: session state only, no campaign write.</summary>
    public bool CommitLocalEthicsChoice(Level2EthicsChoice choice, string caseId)
    {
        CaseReviewState review = Review;
        if (review == null || review.phase < ReviewPhase.Challenge || review.phase >= ReviewPhase.Completed ||
            CurrentCaseId != caseId || review.rosaEthicsCommitted || choice == Level2EthicsChoice.None) return false;
        Level2Checkpoint.SetEthicsChoice(review, choice, caseId);
        Level2FindingsPage.RefreshEthicsNote();
        return true;
    }

    /// <summary>Publishes the just-saved choice into the existing live review and note.</summary>
    public void PublishEthicsChoice(Level2EthicsChoice choice, string caseId)
    {
        Level2Checkpoint.SetEthicsChoice(Review, choice, caseId);
        Level2FindingsPage.RefreshEthicsNote();
    }

    /// <summary>Updates the note after the existing Client Copy handover commits.</summary>
    public void PublishEthicsFollowThrough()
    {
        CaseReviewState review = Review;
        if (review == null || !review.rosaEthicsCommitted) return;
        PlayerProgressManager manager = PlayerProgressManager.Instance;
        if (manager != null && manager.UsesLevelTwoStory && !IsDirectEditorAttempt)
            Level2Checkpoint.SyncEthicsReview(manager.CurrentProgress, review, CurrentCaseId);
        else if (review.rosaEthicsChoiceId.EndsWith("/responsible", System.StringComparison.Ordinal) ||
                 review.rosaEthicsChoiceId.EndsWith("/unsupported-promise", System.StringComparison.Ordinal))
            review.rosaEthicsFollowThroughDone = true;
        Level2FindingsPage.RefreshEthicsNote();
    }

    private static LevelCheckpoint Capture(CaseData data, ReviewClockState clock) => Capture(data, data?.review, clock);

    private static LevelCheckpoint Capture(CaseData data, CaseReviewState review, ReviewClockState clock)
    {
        if (data == null || review == null) return null;
        ReviewClockState live = review.clock;
        review.clock = clock ?? live;
        string json;
        try { json = JsonUtility.ToJson(review); }
        finally { review.clock = live; }
        return new LevelCheckpoint
        {
            levelId = LevelId,
            caseId = CurrentCaseId,
            reviewJson = json,
            residencyStatus = Level2Checkpoint.EnumName(data.residencyStatus),
            taxpayerType = Level2Checkpoint.EnumName(data.taxpayerType),
            incomeSource = Level2Checkpoint.EnumName(data.incomeSource),
            transferredFields = data.folderTransferredFields != null ? new List<string>(data.folderTransferredFields) : new List<string>()
        };
    }

    // =========================================================
    // RESUME
    // =========================================================

    private void TryRestore()
    {
        PlayerProgressManager progress = PlayerProgressManager.Instance;
        CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        LevelCheckpoint checkpoint = progress != null ? progress.GetCheckpoint(LevelId) : null;
        if (checkpoint == null || data?.review == null || definition == null || checkpoint.caseId != definition.caseId) return;

        CaseReviewState restored = JsonUtility.FromJson<CaseReviewState>(checkpoint.reviewJson);
        Level2Checkpoint.Normalize(restored);
        if (!Level2Checkpoint.Usable(restored, data.review.reviewId)) return;

        // The signed campaign is canonical if a legacy/inconsistent checkpoint snapshot differs.
        if (progress.UsesLevelTwoStory)
            Level2Checkpoint.SyncEthicsReview(progress.CurrentProgress, restored, definition.caseId);

        data.review = restored;
        Resumed = true;
        data.residencyStatus = Level2Checkpoint.ParseEnum<ResidencyStatus>(checkpoint.residencyStatus);
        data.taxpayerType = Level2Checkpoint.ParseEnum<TaxpayerType>(checkpoint.taxpayerType);
        data.incomeSource = Level2Checkpoint.ParseEnum<IncomeSource>(checkpoint.incomeSource);
        data.folderTransferredFields = new HashSet<string>(checkpoint.transferredFields ?? new List<string>());
        Debug.Log($"[Level2] Resumed Rosa's review at {restored.phase} (revision {restored.packetRevision}, day {restored.clock.day}).");

        RestoreWorld(restored);
        RestoreScore(restored, definition);
        if (OfficeObjectives.Instance != null)
            foreach (string trigger in Level2Checkpoint.ImpliedTriggers(restored)) OfficeObjectives.Instance.MarkDone(trigger);
        bool approved = Level2Checkpoint.KeepsHandover(restored);
        if (approved) RestoreApproved(restored, data);
        foreach (CaseFolderUI folder in FindObjectsByType<CaseFolderUI>(FindObjectsInactive.Include)) folder.RefreshCurrentPage();
        lastRevision = restored.packetRevision;
        lastPhase = restored.phase;
        lastDay = restored.clock.day;
        Level2Say.Line(approved ? "Welcome back. Rosa's approved copy is waiting to reach her." : "Welcome back. Rosa's review is right where you left it.", null, 5f);
    }

    /// <summary>
    /// An approved review that still owes Rosa her Client Copy: the case stays approved (no audit,
    /// summary, reward or card replays), the level score gets its approved case line back for the
    /// card that follows the handover, and the copy returns beside the Auditor or into the folder icon.
    /// </summary>
    private static void RestoreApproved(CaseReviewState review, CaseData data)
    {
        CaseProgressionManager manager = CaseProgressionManager.Instance;
        if (manager != null)
        {
            // The approving submission counts too (RestoreScore restored the returned ones).
            int charged = review.audit.charged != null ? review.audit.charged.Count : 0;
            manager.RestoreAuditCounters(review.audit.failedSubmissions + 1, charged);
            manager.CalculateAndStoreCaseReward(data);
        }
        GameStateMachine machine = GameStateMachine.Instance;
        if (machine != null)
        {
            machine.ChangeState(new LevelCompleteState(restoredApprovedResult: true));
            machine.LockProgression();
        }
        if (ClientCopyHandover.Instance != null) ClientCopyHandover.Instance.Restore(ClientCopyRules.Parse(review.clientCopyPhase));
    }

    private void RestoreWorld(CaseReviewState review)
    {
        if (review.FolderRetrieved)
        {
            if (review.folderPlacedOnDesk) Level2FolderHandoff.RestorePlacedFolder(gameObject.scene);
            else
            {
                Level2CaseRetrieval retrieval = Level2OfficeStart.Instance != null ? Level2OfficeStart.Instance.GetComponentInChildren<Level2CaseRetrieval>() : null;
                if (retrieval != null) retrieval.StoreRetrievedFolder();
            }
        }
        Level2RosaStall stall = Level2OfficeStart.Instance != null ? Level2OfficeStart.Instance.GetComponentInChildren<Level2RosaStall>() : null;
        if (stall != null && stall.Built && review.HandoverDone) stall.ShowRecordsOnDesk(instant: true);
    }

    /// <summary>The level score gets back what this run already earned and charged (each once).</summary>
    private static void RestoreScore(CaseReviewState review, CaseDefinition definition)
    {
        CaseProgressionManager manager = CaseProgressionManager.Instance;
        if (manager == null) return;
        if (review.HandoverDone) manager.AwardConversation(RosaInteractable.NpcId, "Talked with Aling Rosa", null);
        if (review.challenge != null && review.challenge.awarded)
        {
            LevelRewardRules rules = manager.Score.Rules;
            BadgeTier tier = LevelScoring.UsesAccuracyBadge(rules)
                ? LevelScoring.AccuracyBadge(rules, review.challenge.firstCorrect)
                : BadgeTier.Bronze;
            manager.Score.AddSideQuest(IncomeChallenge.FirstWrongCount(review.challenge), null, tier);
        }
        if (review.audit != null)
        {
            int charged = review.audit.charged != null ? review.audit.charged.Count : 0;
            manager.RestoreAuditCounters(review.audit.failedSubmissions, charged);
        }
        if (review.clock != null && review.clock.missed && definition.review.clock != null)
        {
            manager.CaseExtraExpLoss = Mathf.Max(0, definition.review.clock.lateExp);
            manager.CaseExtraHeartLoss = Mathf.Max(0, definition.review.clock.lateHearts);
        }
    }
}
