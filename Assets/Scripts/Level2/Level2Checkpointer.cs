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
/// result, day rollover), every 30 s while the review changes, and when the
/// app is paused or closed. Nothing is saved once the case is approved: the
/// level result takes over and clears the checkpoint in the same write.
///
/// RESUME (relaunch into Level 2 with a checkpoint for this case): the review
/// (phase, records, quest answers and frozen grade, schedule, findings, stamps,
/// audit history, clock day/time) and the taxpayer facts are put back. Then
/// presentation is rebuilt without replaying actions: the folder goes back on
/// the desk or into the folder icon, the records sit in the stall tray, the
/// objective list catches up, and the level score gets back what was already
/// earned (Rosa's talk, the quest award) plus the audit counters and late
/// flag, so nothing is awarded twice and no penalty is forgotten.
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

    private bool UsesCheckpoint => Level2Checkpoint.ShouldUseCheckpoint(Application.isEditor,
        directEditorScene.IsValid() && gameObject.scene == directEditorScene);

    private bool restoredOrFresh;
    private float nextSave;
    private int lastRevision = -1;
    private ReviewPhase lastPhase;
    private int lastDay;

    private static string LevelId => OfficeLevelCatalog.Level2Id;

    private static string CurrentCaseId => CaseManager.Instance != null && CaseManager.Instance.CurrentDefinition != null ? CaseManager.Instance.CurrentDefinition.caseId : null;

    private void OnEnable()
    {
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

    private void SaveNow()
    {
        if (!restoredOrFresh || !UsesCheckpoint) return;
        CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        CaseReviewState review = data?.review;
        if (review == null || review.phase < ReviewPhase.RetrieveFolder || review.phase >= ReviewPhase.Completed) return;
        if (review.audit != null && review.audit.approved) return;
        PlayerProgressManager progress = PlayerProgressManager.Instance;
        if (progress == null) return;
        var checkpoint = new LevelCheckpoint
        {
            levelId = LevelId,
            caseId = CurrentCaseId,
            reviewJson = JsonUtility.ToJson(review),
            residencyStatus = Level2Checkpoint.EnumName(data.residencyStatus),
            taxpayerType = Level2Checkpoint.EnumName(data.taxpayerType),
            incomeSource = Level2Checkpoint.EnumName(data.incomeSource),
            transferredFields = data.folderTransferredFields != null ? new List<string>(data.folderTransferredFields) : new List<string>()
        };
        if (progress.SaveCheckpoint(checkpoint))
        {
            lastRevision = review.packetRevision;
            lastPhase = review.phase;
            lastDay = review.clock != null ? review.clock.day : 1;
        }
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

        data.review = restored;
        data.residencyStatus = Level2Checkpoint.ParseEnum<ResidencyStatus>(checkpoint.residencyStatus);
        data.taxpayerType = Level2Checkpoint.ParseEnum<TaxpayerType>(checkpoint.taxpayerType);
        data.incomeSource = Level2Checkpoint.ParseEnum<IncomeSource>(checkpoint.incomeSource);
        data.folderTransferredFields = new HashSet<string>(checkpoint.transferredFields ?? new List<string>());
        Debug.Log($"[Level2] Resumed Rosa's review at {restored.phase} (revision {restored.packetRevision}, day {restored.clock.day}).");

        RestoreWorld(restored);
        RestoreScore(restored, definition);
        if (OfficeObjectives.Instance != null)
            foreach (string trigger in Level2Checkpoint.ImpliedTriggers(restored)) OfficeObjectives.Instance.MarkDone(trigger);
        foreach (CaseFolderUI folder in FindObjectsByType<CaseFolderUI>(FindObjectsInactive.Include)) folder.RefreshCurrentPage();
        lastRevision = restored.packetRevision;
        lastPhase = restored.phase;
        lastDay = restored.clock.day;
        Level2Say.Line("Welcome back. Rosa's review is right where you left it.", null, 5f);
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
