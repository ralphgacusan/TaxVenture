using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Saves and resumes Level 1's town and corkboard progress for the story
/// (Campaign Foundation T04): which taxpayers were talked to, which profiles
/// were collected, the guide's intro, the corkboard (phase, pins, attempt
/// counters) and the objective steps reached. One record inside the v5
/// campaign (PlayerProgressManager.SaveLevelOneCheckpoint), never a second file.
///
/// LIFETIME: added at runtime to the Level 1 office scene, under the scene's
/// "Campaign Foundation" root (one per scene). Only story runs of a committed
/// campaign use it; practice, replays and direct Editor runs never save or
/// resume it.
///
/// SAVE: after each conversation reward, profile collection, guide intro,
/// card pin and board completion / reward; every 30 s when the field state
/// changed; on app pause / quit; and before Settings leaves the level (a
/// replay clears it instead). A write that fails keeps the previous record,
/// tells the player once and retries in 5 s.
///
/// RESUME: puts the state back through each owner's quiet restore (no
/// conversation, pickup, score, objective or guide event is raised and no
/// toast plays): talked NPCs are retired, uncollected profiles lie beside
/// their NPC, collected ones are in the collection, the guide is where it was,
/// an open board reopens straight away with its pins, a solved board's
/// wrap-up plays if its reward was still due, and the level score gets back
/// what was earned (talks, side-quest reward) exactly once. The player starts
/// at the scene's normal spawn with no held input. Unknown ids are dropped
/// (nearest safe phase); an unreadable newer record starts the level fresh.
/// </summary>
public sealed class Level1FieldCheckpointer : MonoBehaviour
{
    public const string ObjectName = "Level 1 Field Checkpoint";
    private const float SaveEvery = 30f;
    private const float RetryAfterFailure = 5f;

    public static Level1FieldCheckpointer Active { get; private set; }

    private bool ready;
    private bool settingsExit;
    private bool failureShown;
    private float nextSave;
    private string lastSavedJson;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        GuideDirector.ResumedLessons = ResumedLessons;
    }

    /// <summary>The guide asks while it is created (before its first lesson): the saved run's finished lessons.</summary>
    private static IEnumerable<string> ResumedLessons(string levelId)
    {
        if (levelId != OfficeLevelCatalog.Level1Id || !InUse) return null;
        Level1FieldCheckpoint saved = Progress.GetLevelOneCheckpoint();
        return saved != null && saved.version <= Level1FieldCheckpoint.CurrentVersion ? saved.guideLessonsDone : null;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || OfficeLevelProfile.LevelIdFor(scene) != OfficeLevelCatalog.Level1Id) return;
        GameObject root = null;
        foreach (GameObject candidate in scene.GetRootGameObjects())
            if (candidate.name == MainMenuCampaignPanel.RootName) { root = candidate; break; }
        if (root == null)
        {
            root = new GameObject(MainMenuCampaignPanel.RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }
        if (root.GetComponentInChildren<Level1FieldCheckpointer>(true) != null) return;
        var host = new GameObject(ObjectName);
        host.transform.SetParent(root.transform, false);
        host.AddComponent<Level1FieldCheckpointer>();
    }

    private static PlayerProgressManager Progress => PlayerProgressManager.Instance;
    private static bool InUse => Progress != null && Progress.UsesLevelOneCheckpoint;

    private void OnEnable()
    {
        Active = this;
        TownEvents.OnProfileCollected += HandleProfileCollected;
        TownEvents.OnTutorialIntroCompleted += SaveNow;
        TownEvents.OnSideQuestCompleted += SaveNow;
        CorkboardSideQuestManager.CardPlaced += HandleCardPlaced;
        LevelScoreLedger.EntryAdded += HandleScoreEntry;
        GuideDirector.LessonFinished += HandleLessonFinished;
    }

    private void OnDisable()
    {
        if (Active == this) Active = null;
        TownEvents.OnProfileCollected -= HandleProfileCollected;
        TownEvents.OnTutorialIntroCompleted -= SaveNow;
        TownEvents.OnSideQuestCompleted -= SaveNow;
        CorkboardSideQuestManager.CardPlaced -= HandleCardPlaced;
        LevelScoreLedger.EntryAdded -= HandleScoreEntry;
        GuideDirector.LessonFinished -= HandleLessonFinished;
    }

    private IEnumerator Start()
    {
        float until = Time.unscaledTime + 8f;
        while (Time.unscaledTime < until && (CaseProgressionManager.Instance == null || !CaseProgressionManager.Instance.LevelStarted))
            yield return null;
        // Let the level's own parts build first (guide, HUD, objectives, board).
        yield return null;
        yield return null;
        if (InUse)
        {
            try { TryRestore(); }
            catch (System.Exception error)
            {
                Debug.LogError("[Level1] Could not resume the saved field progress; continuing from the level's start. " + error.Message);
            }
        }
        lastSavedJson = InUse ? JsonUtility.ToJson(Capture()) : null;
        ready = true;
        nextSave = Time.unscaledTime + SaveEvery;
    }

    private void Update()
    {
        if (!ready || Time.unscaledTime < nextSave) return;
        nextSave = Time.unscaledTime + SaveEvery;
        SaveNow();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveNow();
    }

    private void OnApplicationQuit() => SaveNow();

    private void HandleProfileCollected(string profileId) => SaveNow();
    private void HandleLessonFinished(string levelId, string lessonId) => SaveNow();
    private void HandleCardPlaced(ProfileDragCard card, bool correct, bool locked) => SaveNow();

    private void HandleScoreEntry(ScoreEntry entry)
    {
        if (entry != null && (entry.Source == ScoreSource.Conversation || entry.Source == ScoreSource.SideQuest)) SaveNow();
    }

    // =========================================================
    // SAVE
    // =========================================================

    /// <summary>
    /// Settings is leaving the level: back to the menu saves the field state
    /// first; a replay starts over, so the record is cleared. False = stay.
    /// </summary>
    public bool PrepareSettingsExit(bool replay)
    {
        if (!InUse) return true;
        bool ok = replay ? Progress.ClearLevelOneCheckpoint() : SaveCurrent(force: true);
        if (ok) settingsExit = true;
        return ok;
    }

    private void SaveNow() => SaveCurrent(force: false);

    /// <summary>Writes the current field state when it changed. True when it is safely saved (or nothing to do).</summary>
    private bool SaveCurrent(bool force)
    {
        if (settingsExit || !InUse) return true;
        if (!ready) return false;
        Level1FieldCheckpoint candidate = Capture();
        string json = JsonUtility.ToJson(candidate);
        if (!force && json == lastSavedJson) return true;
        if (Progress.SaveLevelOneCheckpoint(candidate))
        {
            lastSavedJson = json;
            failureShown = false;
            return true;
        }
        nextSave = Time.unscaledTime + RetryAfterFailure;
        if (!failureShown)
        {
            failureShown = true;
            Tell("Your progress could not be saved just now. It will try again in a moment.");
        }
        return false;
    }

    /// <summary>The complete field state as stable ids and counters (never scene objects or answers).</summary>
    private Level1FieldCheckpoint Capture()
    {
        var checkpoint = new Level1FieldCheckpoint();
        foreach (TaxpayerNpcInteractable npc in FindObjectsByType<TaxpayerNpcInteractable>(FindObjectsInactive.Include))
            if (npc.HasBeenTalkedTo && !string.IsNullOrWhiteSpace(npc.NpcId)) checkpoint.talkedNpcIds.Add(npc.NpcId);

        TaxpayerProfileCollectionHUD hud = TaxpayerProfileCollectionHUD.Instance;
        if (hud != null)
            foreach (TaxpayerProfile profile in hud.CollectedProfiles)
                if (profile != null) checkpoint.collectedProfileIds.Add(profile.ProfileId);

        CorkboardSideQuestManager board = FindAnyObjectByType<CorkboardSideQuestManager>(FindObjectsInactive.Include);
        if (board != null) board.CaptureState(checkpoint);
        TutorialNpcController guide = FindAnyObjectByType<TutorialNpcController>(FindObjectsInactive.Include);
        if (guide != null)
        {
            checkpoint.guideIntroDone = guide.HasStartedSideQuest;
            if (guide.HasShownCompletionPanel) checkpoint.boardPhase = Level1FieldCheckpoint.PhaseRewarded;
            else if (guide.HasCompletedSideQuest) checkpoint.boardPhase = Level1FieldCheckpoint.PhaseSolved;
        }

        if (OfficeObjectives.Instance != null) checkpoint.objectiveTriggers.AddRange(OfficeObjectives.Instance.ReachedKeys());
        GuideDirector director = GuideRuntime.Instance != null ? GuideRuntime.Instance.Director : null;
        if (director != null) checkpoint.guideLessonsDone.AddRange(director.FinishedLessons());
        checkpoint.Repair();
        return checkpoint;
    }

    // =========================================================
    // RESUME
    // =========================================================

    private void TryRestore()
    {
        Level1FieldCheckpoint saved = Progress.GetLevelOneCheckpoint();
        var board = FindAnyObjectByType<CorkboardSideQuestManager>(FindObjectsInactive.Include);
        var slotIds = new List<string>();
        foreach (TaxpayerClassification classification in System.Enum.GetValues(typeof(TaxpayerClassification)))
            slotIds.Add(Level1FieldCheckpointRules.SlotId(classification));
        TaxpayerProfileCollectionHUD hud = TaxpayerProfileCollectionHUD.Instance;
        int required = hud != null ? hud.RequiredProfiles : 4;

        Level1RestoreOutcome outcome = Level1FieldCheckpointRules.Validate(saved,
            Level1FieldCheckpointRules.ProfileMap(TaxpayerProfileDatabase.All), slotIds, required);
        if (outcome == Level1RestoreOutcome.None) return;
        if (outcome == Level1RestoreOutcome.Reset)
        {
            Debug.LogWarning("[Level1] The saved field progress is from a newer version; the level starts from its beginning.");
            Tell("Your saved progress for this level could not be used, so the level starts from the beginning.");
            return;
        }

        var collected = new HashSet<string>(saved.collectedProfileIds);
        foreach (TaxpayerNpcInteractable npc in FindObjectsByType<TaxpayerNpcInteractable>(FindObjectsInactive.Include))
        {
            if (!saved.talkedNpcIds.Contains(npc.NpcId)) continue;
            TaxpayerProfile profile = TaxpayerProfileDatabase.GetByNpcId(npc.NpcId);
            npc.RestoreHandover(profile != null && collected.Contains(profile.ProfileId));
        }
        if (hud != null)
            foreach (string id in saved.collectedProfileIds) hud.RestoreCollected(TaxpayerProfileDatabase.GetById(id));

        bool allProfiles = hud != null ? hud.AllProfilesCollected : saved.collectedProfileIds.Count >= required;
        bool solved = saved.boardPhase == Level1FieldCheckpoint.PhaseSolved;
        bool rewarded = saved.boardPhase == Level1FieldCheckpoint.PhaseRewarded;
        bool active = saved.boardPhase == Level1FieldCheckpoint.PhaseActive;
        TutorialNpcController guide = FindAnyObjectByType<TutorialNpcController>(FindObjectsInactive.Include);

        if ((solved || rewarded) && board != null) board.RestoreCompleted(saved.placementAttempts, saved.wrongPlacements);
        if (active && (board == null || !board.ResumeSideQuest(saved)))
        {
            // The board could not reopen: the guide stays available to open it again.
            active = false;
            saved.guideIntroDone = false;
            outcome = Level1RestoreOutcome.Repaired;
        }
        if (guide != null) guide.RestoreProgress(allProfiles, saved.guideIntroDone, solved, rewarded);
        if (solved || rewarded) LevelGate.MarkSideQuestDone();
        if (rewarded && CollectedProfilesWorldPanel.Instance != null) CollectedProfilesWorldPanel.Instance.LockToSingleSurvivingProfile();

        RestoreScore(saved, rewarded);
        if (OfficeObjectives.Instance != null) OfficeObjectives.Instance.RestoreTriggers(saved.objectiveTriggers);
        RestoreGuideCounts(saved, allProfiles, solved || rewarded);

        Debug.Log($"[Level1] Resumed field progress: {saved.talkedNpcIds.Count} talks, {saved.collectedProfileIds.Count} profiles, " +
                  $"board '{saved.boardPhase}' ({saved.placements.Count} pins). Outcome: {outcome}.");
        if (outcome == Level1RestoreOutcome.Repaired)
            Tell("Welcome back. Part of your saved progress could not be used, so you continue from the nearest safe point.");
    }

    /// <summary>The level score gets back what this run already earned (each once, without toasts).</summary>
    private static void RestoreScore(Level1FieldCheckpoint saved, bool rewarded)
    {
        CaseProgressionManager manager = CaseProgressionManager.Instance;
        if (manager == null) return;
        LevelScoreLedger score = manager.Score;
        score.Restoring = true;
        try
        {
            foreach (string npcId in saved.talkedNpcIds)
            {
                TaxpayerProfile talked = TaxpayerProfileDatabase.GetByNpcId(npcId);
                manager.AwardConversation(npcId, talked != null ? talked.TaxpayerName : npcId, null);
            }
            if (saved.collectedProfileIds.Count > 0) score.MarkSideQuestStarted();
            if (rewarded) score.AddSideQuest(saved.wrongPlacements, null);
        }
        finally { score.Restoring = false; }
        foreach (HudProgressionPanel panel in FindObjectsByType<HudProgressionPanel>(FindObjectsInactive.Exclude))
            panel.RefreshFromGame(animate: false);
    }

    /// <summary>The guide's lesson prerequisites see the restored milestones (counters only; nothing is raised).</summary>
    private static void RestoreGuideCounts(Level1FieldCheckpoint saved, bool allProfiles, bool sideQuestDone)
    {
        GuideDirector director = GuideRuntime.Instance != null ? GuideRuntime.Instance.Director : null;
        if (director == null) return;
        for (int i = 0; i < saved.collectedProfileIds.Count; i++)
            director.Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.ProfileCollected));
        if (allProfiles)
        {
            director.Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.AllProfilesCollected));
            director.Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.TutorialGuideAppeared));
        }
        if (saved.guideIntroDone) director.Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.TutorialGuideTalkedTo));
        if (sideQuestDone) director.Fire(ObjectiveTriggers.EventKey(ObjectiveTriggers.SideQuestCompleted));
    }

    private static void Tell(string message)
    {
        if (!GuideVoice.TrySay(message, null, 6f)) LevelGate.NotifyBlocked(message);
    }
}
