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
/// CASE AND CLOCK (T05): once the office is open, Marie's case work is part
/// of the same record (Level1CaseCheckpoint: folder fields by key, documents
/// read, flags; receptionist hand-over; folder stored or on the desk; audit
/// counters and the Auditor's retry guard), plus the Office Hours clock from
/// the level start. Saved on every case milestone (hand-over, folder
/// opened/stored/returned, value transferred, book or return, state change,
/// clock announcement / new day) and every 30 s while the clock runs, so a
/// lost process loses at most that much time. Resume puts the clock back
/// exactly (deadlines are never recomputed), refills the case, retires the
/// receptionist, puts the folder back in the icon if it was stored and returns
/// to the "case received" phase. A snapshot of another case or revision is
/// kept aside, not applied, and the case starts again from the receptionist.
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
    /// <summary>A case snapshot that did not match this case, carried in every later save (T05).</summary>
    private Level1CaseRecord setAsideCase;
    private OfficeHours watchedClock;
    private GameStateMachine watchedMachine;
    private ValueTransferManager watchedTransfers;
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
        CorkboardSideQuestManager.DraftChanged += SaveNow;
        CorkboardSideQuestManager.BoardChecked += HandleBoardChecked;
        LevelScoreLedger.EntryAdded += HandleScoreEntry;
        GuideDirector.LessonFinished += HandleLessonFinished;
        GameplayEvents.OnNotesUnlockRequested += SaveNow;
        GameplayEvents.OnCaseFolderOpened += SaveNow;
        GameplayEvents.OnCaseFolderStored += SaveNow;
        GameplayEvents.OnCaseFolderReturned += SaveNow;
        GameplayEvents.OnTaxCodeBookFirstOpened += SaveNow;
        GameplayEvents.OnTaxReturnCollected += SaveNow;
    }

    private void OnDisable()
    {
        if (Active == this) Active = null;
        TownEvents.OnProfileCollected -= HandleProfileCollected;
        TownEvents.OnTutorialIntroCompleted -= SaveNow;
        TownEvents.OnSideQuestCompleted -= SaveNow;
        CorkboardSideQuestManager.DraftChanged -= SaveNow;
        CorkboardSideQuestManager.BoardChecked -= HandleBoardChecked;
        LevelScoreLedger.EntryAdded -= HandleScoreEntry;
        GuideDirector.LessonFinished -= HandleLessonFinished;
        GameplayEvents.OnNotesUnlockRequested -= SaveNow;
        GameplayEvents.OnCaseFolderOpened -= SaveNow;
        GameplayEvents.OnCaseFolderStored -= SaveNow;
        GameplayEvents.OnCaseFolderReturned -= SaveNow;
        GameplayEvents.OnTaxCodeBookFirstOpened -= SaveNow;
        GameplayEvents.OnTaxReturnCollected -= SaveNow;
        UnwatchCaseOwners();
    }

    /// <summary>Instance events of the case owners (clock, state machine, value transfers), hooked once they exist.</summary>
    private void WatchCaseOwners()
    {
        if (watchedClock == null && OfficeHours.Instance != null) { watchedClock = OfficeHours.Instance; watchedClock.Changed += SaveNow; }
        if (watchedMachine == null && GameStateMachine.Instance != null) { watchedMachine = GameStateMachine.Instance; watchedMachine.OnStateChanged += HandleStateChanged; }
        if (watchedTransfers == null && ValueTransferManager.Instance != null) { watchedTransfers = ValueTransferManager.Instance; watchedTransfers.OnTransferSucceeded += HandleTransfer; }
    }

    private void UnwatchCaseOwners()
    {
        if (watchedClock != null) watchedClock.Changed -= SaveNow;
        if (watchedMachine != null) watchedMachine.OnStateChanged -= HandleStateChanged;
        if (watchedTransfers != null) watchedTransfers.OnTransferSucceeded -= HandleTransfer;
        watchedClock = null;
        watchedMachine = null;
        watchedTransfers = null;
    }

    private void HandleStateChanged(IGameState state) => SaveNow();
    private void HandleTransfer(DataValue value) => SaveNow();

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
        WatchCaseOwners();
    }

    private void Update()
    {
        if (ready) WatchCaseOwners();
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
    private void HandleBoardChecked(int correct) => SaveNow();

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

    /// <summary>Explicitly saves a Client Copy phase transition (T08).</summary>
    public bool RequestSave() => SaveCurrent(force: true);

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

        if (OfficeHours.Instance != null) OfficeHours.Instance.CaptureClock(checkpoint.clock);
        CaptureCase(checkpoint.caseRecord);
        if (ClientCopyHandover.Instance != null) checkpoint.handoverPhase = ClientCopyHandover.Instance.SavedPhase;
        else if (Progress != null) checkpoint.handoverPhase = ClientCopyRules.ToSaved(Progress.SavedClientCopyPhase);
        if (ClientConsultation.Instance != null) checkpoint.consultation = ClientConsultation.Instance.Capture();
        if (setAsideCase != null) checkpoint.setAsideCase = setAsideCase;
        checkpoint.Repair();
        return checkpoint;
    }

    /// <summary>Marie's case work, once the office is open (before that the case cannot have started).</summary>
    private static void CaptureCase(Level1CaseRecord into)
    {
        CaseManager cases = CaseManager.Instance;
        if (!LevelGate.SideQuestDone || cases == null || cases.CurrentCase == null || cases.CurrentDefinition == null ||
            cases.CurrentDefinition.HasReview) return;
        CaseData data = cases.CurrentCase;
        Level1CaseCheckpoint.Capture(data, cases.CurrentDefinition, into);

        ReceptionistInteractable receptionist = FindAnyObjectByType<ReceptionistInteractable>(FindObjectsInactive.Include);
        into.receptionistDone = receptionist != null && receptionist.HasSpokenFor(into.caseId);

        AuditorSubmissionTray tray = AuditorSubmissionTray.Instance;
        IGameState state = GameStateMachine.Instance != null ? GameStateMachine.Instance.CurrentState : null;
        bool stored = (tray != null && tray.FolderSubmitted) || state is AuditSubmittedState;
        // Either folder component (closed desk folder or open folder) may hold the folder in the icon.
        foreach (FolderToHUDIcon folder in FindObjectsByType<FolderToHUDIcon>(FindObjectsInactive.Include))
            if (folder.IsStoredOrViewing) stored = true;
        into.folder = stored ? Level1CaseRecord.FolderStored : Level1CaseRecord.FolderOnDesk;

        into.encodedFormJson = data.encodedForm != null ? JsonUtility.ToJson(data.encodedForm) : null;
        foreach (TaxCodeBookInteractable book in FindObjectsByType<TaxCodeBookInteractable>(FindObjectsInactive.Include))
            if (book.HasBeenOpenedOnce) into.taxCodeBookOpened = true;

        CaseProgressionManager progression = CaseProgressionManager.Instance;
        if (progression != null)
        {
            into.auditAttempts = progression.CurrentCaseAuditAttempts;
            into.auditMistakes = progression.CurrentCaseAuditMistakes;
        }
        AuditorInteractable auditor = FindAnyObjectByType<AuditorInteractable>(FindObjectsInactive.Include);
        if (auditor != null)
        {
            into.failedAudits = auditor.FailedAudits;
            into.lastFailedAnswers = auditor.LastFailedAnswers;
        }
    }

    // =========================================================
    // RESUME
    // =========================================================

    private void TryRestore()
    {
        Level1FieldCheckpoint saved = Progress.GetLevelOneCheckpoint();
        PlayerProgressData current = Progress != null ? Progress.CurrentProgress : null;
        bool caseResultRecorded = current != null && current.HasCampaign &&
            current.campaign.levelOneCaseResult != null && current.campaign.levelOneCaseResult.recorded;
        bool followThroughDone = current != null && CampaignRewards.IsActivityCompleted(current,
            CampaignRewards.Level1, CampaignRewards.FollowThrough);
        var board = FindAnyObjectByType<CorkboardSideQuestManager>(FindObjectsInactive.Include);
        var slotIds = new List<string>();
        foreach (TaxpayerClassification classification in System.Enum.GetValues(typeof(TaxpayerClassification)))
            slotIds.Add(Level1FieldCheckpointRules.SlotId(classification));
        TaxpayerProfileCollectionHUD hud = TaxpayerProfileCollectionHUD.Instance;
        int required = hud != null ? hud.RequiredProfiles : 4;

        Level1RestoreOutcome outcome = Level1FieldCheckpointRules.Validate(saved,
            Level1FieldCheckpointRules.ProfileMap(TaxpayerProfileDatabase.All), slotIds, required);
        if (outcome == Level1RestoreOutcome.None)
        {
            if (!caseResultRecorded) return;
            saved = new Level1FieldCheckpoint();
            outcome = Level1RestoreOutcome.Repaired;
        }
        if (outcome == Level1RestoreOutcome.Reset)
        {
            Debug.LogWarning("[Level1] The saved field progress is from a newer version; the level starts from its beginning.");
            Tell("Your saved progress for this level could not be used, so the level starts from the beginning.");
            return;
        }

        Level1RestoreOutcome handover = Level1FieldCheckpointRules.ValidateHandover(saved, caseResultRecorded, followThroughDone);
        if (handover == Level1RestoreOutcome.Repaired) outcome = Level1RestoreOutcome.Repaired;

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

        if ((solved || rewarded) && board != null)
            board.RestoreCompleted(saved.placementAttempts, saved.wrongPlacements, saved.checkSubmitted ? saved.checkCorrectCount : -1);
        if (active && (board == null || !board.ResumeSideQuest(saved)))
        {
            // The board could not reopen: the guide stays available to open it again.
            active = false;
            saved.guideIntroDone = false;
            outcome = Level1RestoreOutcome.Repaired;
        }
        if (guide != null) guide.RestoreProgress(allProfiles, saved.guideIntroDone, solved, rewarded);
        if (solved || rewarded) LevelGate.MarkSideQuestDone();
        // T23: Marie back in the conference room with her saved visits (quietly, no line or event).
        if (ClientConsultation.Instance != null) ClientConsultation.Instance.Restore(saved.consultation, solved || rewarded);
        if (rewarded && CollectedProfilesWorldPanel.Instance != null) CollectedProfilesWorldPanel.Instance.LockToSingleSurvivingProfile();

        RestoreScore(saved, rewarded);
        if (OfficeObjectives.Instance != null) OfficeObjectives.Instance.RestoreTriggers(saved.objectiveTriggers);
        RestoreGuideCounts(saved, allProfiles, solved || rewarded);
        if (OfficeHours.Instance != null)
        {
            OfficeHours.Instance.RestoreClock(saved.clock);
            if (caseResultRecorded) OfficeHours.Instance.RestoreApprovedResult();
        }
        RestoreCase(saved, caseResultRecorded);
        if (caseResultRecorded)
        {
            GameStateMachine machine = GameStateMachine.Instance;
            if (machine != null)
            {
                machine.ChangeState(new LevelCompleteState(restoredApprovedResult: true));
                machine.LockProgression();
            }
            if (ClientCopyHandover.Instance != null)
                ClientCopyHandover.Instance.Restore(ClientCopyRules.Parse(saved.handoverPhase));
        }

        Debug.Log($"[Level1] Resumed field progress: {saved.talkedNpcIds.Count} talks, {saved.collectedProfileIds.Count} profiles, " +
                  $"board '{saved.boardPhase}' ({saved.placements.Count} pins). Outcome: {outcome}.");
        if (outcome == Level1RestoreOutcome.Repaired)
            Tell("Welcome back. Part of your saved progress could not be used, so you continue from the nearest safe point.");
    }

    /// <summary>
    /// Marie's case (T05), after the field state, objectives and clock: the
    /// work goes back into CaseData, the receptionist is retired, the folder is
    /// back in its icon if it was stored, the audit counters and retry guard
    /// return, and the run continues from "case received". Nothing is
    /// transferred, stamped, audited or charged again.
    /// </summary>
    private void RestoreCase(Level1FieldCheckpoint saved, bool approvedResultRecorded)
    {
        CaseManager cases = CaseManager.Instance;
        CaseDefinition definition = cases != null ? cases.CurrentDefinition : null;
        CaseData data = cases != null ? cases.CurrentCase : null;
        if (definition == null || data == null || definition.HasReview) return;

        Level1RestoreOutcome outcome = Level1FieldCheckpointRules.ValidateCase(saved, definition.caseId,
            Level1CaseCheckpoint.Fingerprint(definition), Level1CaseCheckpoint.FieldKeys);
        if (saved.setAsideCase != null && saved.setAsideCase.IsSet) setAsideCase = saved.setAsideCase;
        if (outcome == Level1RestoreOutcome.None) return;
        if (outcome == Level1RestoreOutcome.Reset)
        {
            Debug.LogWarning($"[Level1] The saved case ({setAsideCase?.caseId}) does not match {definition.caseId} as it is now; it was kept aside and the case starts from the receptionist.");
            Tell("This case changed since you last played, so it starts again from the receptionist. Your other progress is kept.");
            return;
        }

        Level1CaseRecord record = saved.caseRecord;
        if (!record.IsSet) return;
        int rejected = Level1CaseCheckpoint.Apply(record, data);
        if (!string.IsNullOrEmpty(record.encodedFormJson))
        {
            try { data.encodedForm = JsonUtility.FromJson<EncodedFormData>(record.encodedFormJson); }
            catch (System.Exception) { rejected++; }
        }

        if (record.receptionistDone || approvedResultRecorded)
        {
            ReceptionistInteractable receptionist = FindAnyObjectByType<ReceptionistInteractable>(FindObjectsInactive.Include);
            if (receptionist != null) receptionist.RestoreSpoken(definition.caseId);
            GameStateMachine machine = GameStateMachine.Instance;
            if (!approvedResultRecorded && machine != null && !(machine.CurrentState is ReceiveCaseState))
                machine.ChangeState(new ReceiveCaseState());
        }
        AuditorSubmissionTray tray = AuditorSubmissionTray.Instance;
        if (record.folder == Level1CaseRecord.FolderStored && tray != null && tray.Folder != null)
        {
            tray.Folder.RestoreStored();
            foreach (HudIconButton icon in FindObjectsByType<HudIconButton>(FindObjectsInactive.Include)) icon.RefreshFolderState();
        }
        if (record.taxCodeBookOpened)
            foreach (TaxCodeBookInteractable book in FindObjectsByType<TaxCodeBookInteractable>(FindObjectsInactive.Include)) book.RestoreOpenedOnce();

        if (CaseProgressionManager.Instance != null) CaseProgressionManager.Instance.RestoreAuditCounters(record.auditAttempts, record.auditMistakes);
        AuditorInteractable auditor = FindAnyObjectByType<AuditorInteractable>(FindObjectsInactive.Include);
        if (auditor != null) auditor.RestoreRetryGuard(record.failedAudits, record.lastFailedAnswers);
        foreach (CaseFolderUI folder in FindObjectsByType<CaseFolderUI>(FindObjectsInactive.Include)) folder.RefreshCurrentPage();

        Debug.Log($"[Level1] Resumed case {record.caseId}: {record.fields.Count} fields, folder {record.folder}, " +
                  $"{record.failedAudits} failed audits, day {saved.clock.day}.");
        if (rejected > 0 || outcome == Level1RestoreOutcome.Repaired)
            Tell("Welcome back. A few saved case entries could not be used, so check the case folder before handing it in.");
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
            // T06: the frozen first-check tier; a board finished before T06 keeps the EXP-derived badge.
            if (rewarded) score.AddSideQuest(saved.wrongPlacements, null,
                saved.checkSubmitted ? (BadgeTier)ClassificationCheck.TierFor(saved.checkCorrectCount) : (BadgeTier?)null);
        }
        finally { score.Restoring = false; }
        // T08/T09: a rewarded board whose field-assignment grants were never saved gets them now, quietly
        // (claimed once; nothing happens when they were already saved).
        if (rewarded && Progress != null) Progress.CommitFieldAssignment(questCorrected: true);
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
