using System;
using System.IO;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Owns the player's permanent, whole-game progression (total EXP,
/// total Reputation, levels completed, best results, badges) and persists
/// it to Application.persistentDataPath/Save/progress.json (Resources is
/// read-only on device).
///
/// LIFETIME: created automatically before the first scene loads and kept
/// across scenes (DontDestroyOnLoad), so every scene, including a level
/// opened directly in the editor, can use PlayerProgressManager.Instance.
/// Copies placed in scenes remove themselves.
///
/// SAVING (SaveFileStore):
/// - Atomic write with the previous save kept as progress.json.bak.
/// - The file is a small envelope { format, payload, signature }: payload
///   is the PlayerProgressData JSON and signature its HMAC, so a hand-edited
///   save is noticed. A save that fails the check is kept aside as
///   progress.json.rejected and the backup is used instead.
/// - Older plain saves (version 1 and 2) still load and are upgraded.
/// - Whatever loads is passed through PlayerProgressData.Normalize(), which
///   repairs impossible values.
///
/// CAMPAIGN (version 5, T02): at load the save is classified once
/// (CampaignState): no save, a committed campaign, an older save that needs a
/// New Game (nothing of it is imported), a newer game's save (left untouched
/// and never overwritten by Save) or unreadable files. StartNewCampaign is the
/// only way to create a campaign: it writes the full fresh record first and
/// publishes it only after the write succeeded.
///
/// APPLYING A LEVEL: LevelCompleteUI registers the finished level with
/// SetPendingResult as soon as the Level Complete card shows, and applies it
/// on Continue. If the game is paused (sent to the background on mobile) or
/// closed before Continue, the pending result is applied and saved then, so
/// a finished level is never lost. Each result is applied at most once
/// (LevelTotalResult.RunId).
/// </summary>
public class PlayerProgressManager : MonoBehaviour
{
    [Serializable]
    private class SaveEnvelope
    {
        public int format;
        public string payload;
        public string signature;
    }

    private const int EnvelopeFormat = 1;

    public static PlayerProgressManager Instance { get; private set; }

    /// <summary>Raised after progress changed and was saved (menus refresh from it).</summary>
    public static event Action<PlayerProgressData> ProgressChanged;

    private PlayerProgressData progress;
    private SaveFileStore store;
    private LevelTotalResult pendingResult;
    private string lastAppliedRunId;
    private CampaignSaveState campaignState;

    // Snapshot of the last successful write/load, never an unsaved result mutation.
    public bool HasSavedFirstChapterCompletion { get; private set; }

    private void RememberSavedCompletion() => HasSavedFirstChapterCompletion =
        CampaignMenu.HasStory(campaignState, progress) &&
        progress.campaign.IsChapterCompleted(CampaignRecord.FirstChapterId);

    private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Save");
    private string SavePath => Path.Combine(SaveDirectory, "progress.json");

    public PlayerProgressData CurrentProgress => progress;

    /// <summary>What the save means for the updated story (T02): fresh install, campaign, old save, newer save or unreadable.</summary>
    public CampaignSaveState CampaignState => campaignState;

    /// <summary>True only for a committed version-5 campaign; old saves never supply Continue.</summary>
    public bool CanContinueCampaign => CampaignSave.CanContinue(campaignState);

    /// <summary>Why Continue is unavailable, e.g. "Start the updated story" ("" when it is available).</summary>
    public string ContinueNotice => CampaignSave.ContinueNotice(campaignState);

    /// <summary>True when a finished level is waiting to be saved.</summary>
    public bool HasPendingResult => pendingResult != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var host = new GameObject("PlayerProgressManager");
        host.AddComponent<PlayerProgressManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // A scene copy: remove just this component when the object holds
            // anything else, otherwise the whole (empty) object.
            if (GetComponents<Component>().Length <= 2) Destroy(gameObject);
            else Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        store = new SaveFileStore(SavePath);
        progress = LoadOrCreate();
        RememberSavedCompletion();

        Debug.Log(
            $"[PlayerProgressManager] Ready. " +
            $"EXP: {progress.totalExp}, Reputation: {progress.totalReputation}, " +
            $"Levels: {progress.levelsCompleted}"
        );
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) FlushPending("app paused");
    }

    private void OnApplicationQuit()
    {
        FlushPending("app closing");
    }

    // =========================================================
    // LEVEL RESULTS
    // =========================================================

    /// <summary>
    /// Remembers a finished level so it is saved even if the player never
    /// presses Continue (app paused or closed). Apply it with ApplyLevelResult.
    /// </summary>
    public void SetPendingResult(LevelTotalResult levelTotal)
    {
        if (levelTotal == null || levelTotal.RunId == lastAppliedRunId) return;
        if (IgnoredForPractice(levelTotal)) return;
        pendingResult = levelTotal;
    }

    private void FlushPending(string reason)
    {
        if (pendingResult == null) return;
        Debug.Log($"[PlayerProgressManager] Saving the finished level ({reason}).");
        ApplyLevelResult(pendingResult);
    }

    /// <summary>
    /// Folds a completed level's totals into permanent progression and saves.
    /// Safe to call more than once for the same result: it applies once.
    /// </summary>
    public void ApplyLevelResult(LevelTotalResult levelTotal)
    {
        if (levelTotal == null)
        {
            Debug.LogError("[PlayerProgressManager] ApplyLevelResult called with NULL levelTotal.");
            return;
        }

        if (string.IsNullOrWhiteSpace(levelTotal.LevelId))
        {
            Debug.LogError("[PlayerProgressManager] ApplyLevelResult requires a level ID.");
            return;
        }

        if (IgnoredForPractice(levelTotal)) return;

        if (levelTotal.RunId == lastAppliedRunId)
        {
            Debug.Log("[PlayerProgressManager] This level result was already saved.");
            if (pendingResult == levelTotal) pendingResult = null;
            return;
        }

        lastAppliedRunId = levelTotal.RunId;
        if (pendingResult == levelTotal) pendingResult = null;
        CampaignRunMode mode = CurrentRunMode;
        // The finished run replaces its resumable checkpoint in the same write (a replay never touches the story's).
        if (mode != CampaignRunMode.MasteryReplay && (levelTotal.LevelId != OfficeLevelCatalog.Level2Id || !Level2Checkpointer.IsDirectEditorAttempt))
            progress.ClearCheckpoint(levelTotal.LevelId);

        // Story runs record the campaign's first completion of the chapter (Continue then opens the career).
        if (CampaignRun.AllowsStoryWrites(mode) && progress.HasCampaign && progress.campaign.MarkChapterCompleted(levelTotal.LevelId))
            Debug.Log($"[PlayerProgressManager] Story chapter {levelTotal.LevelId} completed.");
        // ...and the finished Level 1 story run drops its field checkpoint in the same write (T04).
        if (CampaignRun.AllowsStoryWrites(mode) && progress.HasCampaign && levelTotal.LevelId == CampaignRecord.FirstChapterId)
            progress.campaign.levelOne = new Level1FieldCheckpoint();

        // Mastery records (MasteryBoundary): best result per level, badge tiers
        // and times; only a passing run unlocks the next level, and the career
        // totals grow only by improvements on the best. A run without a level
        // score (no rewards.json ledger) counts as a completion whose rewards
        // are kept as that level's best, so replaying never adds them again.
        var gain = MasteryBoundary.ApplyRun(progress, ToMasteryRun(levelTotal));
        if (levelTotal.HasScore)
        {
            Debug.Log(
                $"[PlayerProgressManager] {levelTotal.LevelId}: {levelTotal.TotalExp}/{levelTotal.MaxExp} EXP, " +
                $"{levelTotal.TotalReputation}/{levelTotal.MaxReputation} hearts, passed: {levelTotal.Passed}, " +
                $"new best: {gain.newBest} (+{gain.expGain} EXP, +{gain.heartsGain} hearts to totals)."
            );
        }

        Debug.Log(
            $"[PlayerProgressManager] Applied level result. " +
            $"New totals -> EXP: {progress.totalExp}, " +
            $"Reputation: {progress.totalReputation}, " +
            $"Levels: {progress.levelsCompleted}"
        );

        Save();
    }

    /// <summary>The run mode in force (T03): a practice drill wins; a level loaded without a menu route is a direct Editor run.</summary>
    public static CampaignRunMode CurrentRunMode => CampaignRun.Resolve(CampaignRun.Chosen, TrainingSession.IsPracticing);

    /// <summary>
    /// Before a story run leaves its scene (menu / replay): save any waiting
    /// result and the progress, and report whether that worked. Other run modes
    /// have nothing of the story to save. On false the caller stays in the run.
    /// </summary>
    public bool SaveBeforeLeaving()
    {
        if (!CampaignRun.AllowsStoryWrites(CurrentRunMode)) return true;
        if (pendingResult != null) FlushPending("leaving the level");
        return Save();
    }

    /// <summary>The mastery boundary's plain view of a finished level (TotalReputation = accuracy score hearts).</summary>
    private static MasteryRun ToMasteryRun(LevelTotalResult levelTotal) => new MasteryRun
    {
        LevelId = levelTotal.LevelId,
        HasScore = levelTotal.HasScore,
        Exp = levelTotal.TotalExp,
        Hearts = levelTotal.TotalReputation,
        MaxExp = levelTotal.MaxExp,
        MaxHearts = levelTotal.MaxReputation,
        Passed = levelTotal.Passed,
        Badges = levelTotal.Badges,
        BadgeTiers = levelTotal.BadgeTiers?.ConvertAll(tier => (int)tier),
        RunSeconds = levelTotal.RunSeconds,
        SideQuestSeconds = levelTotal.SideQuestSeconds
    };

    /// <summary>A Training Hub practice run never reaches the career (EXP, hearts, badges, levels).</summary>
    private bool IgnoredForPractice(LevelTotalResult levelTotal)
    {
        if (!TrainingSession.IsPracticing) return false;
        Debug.Log($"[PlayerProgressManager] Practice run of {levelTotal.LevelId}: not saved to the career.");
        if (pendingResult == levelTotal) pendingResult = null;
        return true;
    }

    // =========================================================
    // LEVEL CHECKPOINT AND STORY FACTS (Level 2)
    // =========================================================

    /// <summary>Saves the resumable state of an unfinished level run (signed, atomic, same save file).</summary>
    /// <summary>Clear only the unfinished attempt; retain earned career results.</summary>
    public bool ClearCheckpoint(string levelId)
    {
        LevelCheckpoint previous = progress.CheckpointFor(levelId);
        progress.ClearCheckpoint(levelId);
        if (Save()) return true;
        if (previous != null) progress.SetCheckpoint(previous);
        return false;
    }

    /// <summary>Confirmed fresh start. Replace both save generations so recovery cannot resurrect old progress.</summary>
    public bool ResetProgress()
    {
        var fresh = new PlayerProgressData();
        fresh.Normalize();
        try
        {
            string encoded = Encode(fresh);
            store.Reset(encoded);
        }
        catch (Exception error)
        {
            Debug.LogError("[PlayerProgressManager] Reset failed: " + error.Message);
            return false;
        }
        Publish(fresh, CampaignSave.Classify(true, fresh));
        return true;
    }

    /// <summary>
    /// Confirmed New Game (T02): builds the complete first campaign state, writes
    /// it as the main save and the backup, and only then makes it the current
    /// progress. Nothing from any earlier save is carried over; settings are not
    /// touched. On failure the previous files are put back, the current progress
    /// stays as it was and the error can be shown; trying again is safe.
    /// </summary>
    public bool StartNewCampaign(out string error)
    {
        error = null;
        if (store == null)
        {
            error = "The save is not ready.";
            return false;
        }

        PlayerProgressData candidate = PlayerProgressData.CreateNewCampaign(
            Guid.NewGuid().ToString("N"), DateTime.UtcNow.ToString("o"));

        SaveFileStore.Generations previous;
        try
        {
            previous = store.Capture();
        }
        catch (Exception e)
        {
            error = "Could not read the current save (" + e.Message + ").";
            Debug.LogError("[PlayerProgressManager] New Game not started: " + error);
            return false;
        }

        if (!CampaignSave.TryCommit(candidate, Encode, store.Reset, () => store.Restore(previous), out error))
        {
            Debug.LogError("[PlayerProgressManager] New Game not started: " + error);
            return false;
        }

        Publish(candidate, CampaignSaveState.Campaign);
        Debug.Log($"[PlayerProgressManager] New campaign {candidate.campaign.campaignId} started.");
        return true;
    }

    /// <summary>Makes a durably written replacement the current progress (reset or New Game).</summary>
    private void Publish(PlayerProgressData fresh, CampaignSaveState state)
    {
        progress = fresh;
        campaignState = state;
        RememberSavedCompletion();
        pendingResult = null;
        lastAppliedRunId = null;
        PlayerPrefs.DeleteKey("TaxVenture.Career.Seen.v1");
        PlayerPrefs.DeleteKey("TaxVenture.Guide.Level_01.finished");
        PlayerPrefs.DeleteKey("TaxVenture.Guide.Level_02.finished");
        PlayerPrefs.Save();
        TrainingSession.End();
        CareerProgressService.ClearCache();
        try { ProgressChanged?.Invoke(progress); }
        catch (Exception error) { Debug.LogException(error); }
    }

    public bool SaveCheckpoint(LevelCheckpoint checkpoint)
    {
        // Practice drills and mastery replays never replace the story's resumable checkpoint.
        if (progress == null || checkpoint == null || !checkpoint.IsSet || TrainingSession.IsPracticing ||
            CurrentRunMode == CampaignRunMode.MasteryReplay) return false;
        progress.SetCheckpoint(checkpoint);
        return Save();
    }

    /// <summary>The resumable run of this level, or null.</summary>
    public LevelCheckpoint GetCheckpoint(string levelId) => progress != null ? progress.CheckpointFor(levelId) : null;

    // =========================================================
    // LEVEL 1 FIELD CHECKPOINT (T04, inside the v5 campaign)
    // =========================================================

    /// <summary>Only a story run of a committed campaign keeps the Level 1 field checkpoint.</summary>
    public bool UsesLevelOneCheckpoint =>
        progress != null && progress.HasCampaign && CampaignSave.CanContinue(campaignState) &&
        CampaignRun.AllowsStoryCheckpoint(CurrentRunMode);

    /// <summary>
    /// Saves the complete candidate as the campaign's Level 1 field checkpoint.
    /// On a failed write the previous checkpoint is put back in memory (the
    /// file still holds it), nothing is reported as saved, and the caller may retry.
    /// </summary>
    public bool SaveLevelOneCheckpoint(Level1FieldCheckpoint candidate)
    {
        if (candidate == null || !UsesLevelOneCheckpoint) return false;
        Level1FieldCheckpoint previous = progress.campaign.levelOne;
        candidate.version = Level1FieldCheckpoint.CurrentVersion;
        candidate.writes = (previous != null && previous.IsSet ? previous.writes : 0) + 1;
        progress.campaign.levelOne = candidate;
        if (Save()) return true;
        progress.campaign.levelOne = previous;
        return false;
    }

    /// <summary>A copy of the saved Level 1 field checkpoint (null when there is none or this run does not use it).</summary>
    public Level1FieldCheckpoint GetLevelOneCheckpoint()
    {
        if (!UsesLevelOneCheckpoint || progress.campaign.levelOne == null || !progress.campaign.levelOne.IsSet) return null;
        return JsonUtility.FromJson<Level1FieldCheckpoint>(JsonUtility.ToJson(progress.campaign.levelOne));
    }

    /// <summary>Replay from the start: drops the Level 1 field checkpoint (restored in memory if the write fails).</summary>
    public bool ClearLevelOneCheckpoint()
    {
        if (!UsesLevelOneCheckpoint) return true;
        Level1FieldCheckpoint previous = progress.campaign.levelOne;
        if (previous == null || !previous.IsSet) return true;
        progress.campaign.levelOne = new Level1FieldCheckpoint();
        if (Save()) return true;
        progress.campaign.levelOne = previous;
        return false;
    }

    /// <summary>Remember a fully viewed first intro in the existing signed save.</summary>
    public bool MarkLevelOneIntroSeen()
    {
        if (progress.levelOneIntroSeen) return true;
        progress.levelOneIntroSeen = true;
        if (Save()) return true;
        progress.levelOneIntroSeen = false;
        return false;
    }

    /// <summary>Records a neutral story fact once and saves.</summary>
    public bool RecordStoryFact(string id)
    {
        if (progress == null || TrainingSession.IsPracticing || !progress.RecordStoryFact(id)) return false;
        Save();
        return true;
    }

    // =========================================================
    // TRAINING HUB
    // =========================================================

    /// <summary>Records an in-game tutorial lesson as seen ("Level_01/move") and saves when it is new.</summary>
    public bool RecordTutorialSeen(string key)
    {
        if (progress == null || !progress.RecordTutorialSeen(key)) return false;
        Save();
        return true;
    }

    /// <summary>Counts a finished practice run and saves. True when the card just became mastered.</summary>
    public bool RecordTrainingRun(string cardId, bool mastery, float seconds)
    {
        if (progress == null || string.IsNullOrWhiteSpace(cardId)) return false;
        bool newlyMastered = progress.RecordTrainingRun(cardId, mastery, seconds);
        Save();
        return newlyMastered;
    }

    // =========================================================
    // SAVE / LOAD
    // =========================================================

    /// <summary>Writes the current progress (signed, atomic). Returns false on failure.</summary>
    public bool Save()
    {
        try
        {
            // A newer game's save is never overwritten by ordinary saving (only a confirmed New Game/reset replaces it).
            if (!CampaignSave.AllowsOrdinarySave(campaignState))
            {
                Debug.LogWarning("[PlayerProgressManager] Not saved: the save on this device is from a newer version of TaxVenture.");
                return false;
            }
            progress.Normalize();
            store.Write(Encode(progress));
            RememberSavedCompletion();
            Debug.Log($"[PlayerProgressManager] Progress saved to {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerProgressManager] Save failed: {e.Message}");
            return false;
        }

        try
        {
            ProgressChanged?.Invoke(progress);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        return true;
    }

    private PlayerProgressData LoadOrCreate()
    {
        if (!store.Exists)
        {
            Debug.Log($"[PlayerProgressManager] No save file found at {SavePath}. Starting fresh.");
            campaignState = CampaignSave.Classify(false, null);
            var fresh = new PlayerProgressData();
            fresh.Normalize();
            return fresh;
        }

        string text = store.Read(t => TryDecode(t, out _, out _), out bool fromBackup, out string problem);
        if (text == null || !TryDecode(text, out PlayerProgressData data, out bool legacy))
        {
            Debug.LogError(
                $"[PlayerProgressManager] No usable save at {SavePath} ({problem ?? "unreadable"}). " +
                "Starting fresh; the old files were kept (.bak / .rejected).");
            campaignState = CampaignSave.Classify(true, null);
            var fresh = new PlayerProgressData();
            fresh.Normalize();
            return fresh;
        }

        if (fromBackup)
            Debug.LogWarning($"[PlayerProgressManager] The save was not usable ({problem}); loaded the backup instead.");

        // Decided before Normalize: an old save stays an old save (never a campaign).
        campaignState = CampaignSave.Classify(true, data);
        if (campaignState == CampaignSaveState.UnsupportedFuture)
        {
            // Keep the newer game's file exactly as it is; this version neither reads nor rewrites it.
            Debug.LogWarning($"[PlayerProgressManager] The save is version {data.saveVersion}, newer than this game " +
                             $"({PlayerProgressData.CurrentSaveVersion}). It was left untouched.");
            var fresh = new PlayerProgressData();
            fresh.Normalize();
            return fresh;
        }

        bool repaired = data.Normalize();
        Debug.Log($"[PlayerProgressManager] Progress loaded from {(fromBackup ? store.BackupPath : SavePath)}.");

        // Upgrade old or repaired saves on disk right away (the old file
        // becomes the .bak, so nothing is lost).
        if (legacy || repaired || fromBackup)
        {
            try
            {
                store.Write(Encode(data));
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerProgressManager] Could not write the upgraded save: {e.Message}");
            }
        }
        return data;
    }

    private static string Encode(PlayerProgressData data)
    {
        string payload = JsonUtility.ToJson(data, true);
        var envelope = new SaveEnvelope
        {
            format = EnvelopeFormat,
            payload = payload,
            signature = SaveFileStore.Sign(payload)
        };
        return JsonUtility.ToJson(envelope, true);
    }

    /// <summary>
    /// Reads a save: a signed envelope (signature must match) or, for saves
    /// written before version 3, the plain PlayerProgressData JSON.
    /// </summary>
    private static bool TryDecode(string text, out PlayerProgressData data, out bool legacy)
    {
        data = null;
        legacy = false;
        try
        {
            SaveEnvelope envelope = JsonUtility.FromJson<SaveEnvelope>(text);
            if (envelope != null && envelope.format > 0 && !string.IsNullOrEmpty(envelope.payload))
            {
                if (!SaveFileStore.SignatureMatches(envelope.payload, envelope.signature))
                {
                    Debug.LogWarning("[PlayerProgressManager] The save's signature does not match (edited or damaged).");
                    return false;
                }
                data = JsonUtility.FromJson<PlayerProgressData>(envelope.payload);
                return data != null;
            }

            // Plain save from before signing. Only genuine old versions are
            // accepted this way; a plain file claiming the current version
            // can only be a hand-made one.
            data = JsonUtility.FromJson<PlayerProgressData>(text);
            if (data == null || data.saveVersion >= PlayerProgressData.SignedSinceVersion) return false;
            legacy = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayerProgressManager] The save could not be parsed: {e.Message}");
            data = null;
            return false;
        }
    }
}
