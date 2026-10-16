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

    private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Save");
    private string SavePath => Path.Combine(SaveDirectory, "progress.json");

    public PlayerProgressData CurrentProgress => progress;

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

        if (levelTotal.RunId == lastAppliedRunId)
        {
            Debug.Log("[PlayerProgressManager] This level result was already saved.");
            if (pendingResult == levelTotal) pendingResult = null;
            return;
        }

        lastAppliedRunId = levelTotal.RunId;
        if (pendingResult == levelTotal) pendingResult = null;

        if (levelTotal.HasScore)
        {
            // Best result per level; only a passing run unlocks the next level,
            // and the career totals grow only by improvements on the best.
            var gain = progress.RecordLevelResult(
                levelTotal.LevelId,
                levelTotal.TotalExp,
                levelTotal.TotalReputation,
                levelTotal.MaxExp,
                levelTotal.MaxReputation,
                levelTotal.Passed,
                levelTotal.Badges);

            // Collected badges keep their best tier (bronze / silver / gold).
            for (int i = 0; i < levelTotal.Badges.Count && i < levelTotal.BadgeTiers.Count; i++)
                progress.RecordBadge(levelTotal.Badges[i], (int)levelTotal.BadgeTiers[i]);

            Debug.Log(
                $"[PlayerProgressManager] {levelTotal.LevelId}: {levelTotal.TotalExp}/{levelTotal.MaxExp} EXP, " +
                $"{levelTotal.TotalReputation}/{levelTotal.MaxReputation} hearts, passed: {levelTotal.Passed}, " +
                $"new best: {gain.newBest} (+{gain.expGain} EXP, +{gain.heartsGain} hearts to totals)."
            );
        }
        else
        {
            // No level score (no rewards.json ledger): counts as a completion,
            // and its rewards are kept as that level's best like any other run,
            // so replaying never adds them again.
            progress.RecordLevelCompletion(levelTotal.LevelId);
            progress.RecordLevelResult(
                levelTotal.LevelId,
                levelTotal.TotalExp,
                levelTotal.TotalReputation,
                0,
                0,
                true,
                levelTotal.Badges);
        }

        Debug.Log(
            $"[PlayerProgressManager] Applied level result. " +
            $"New totals -> EXP: {progress.totalExp}, " +
            $"Reputation: {progress.totalReputation}, " +
            $"Levels: {progress.levelsCompleted}"
        );

        Save();
    }

    // =========================================================
    // SAVE / LOAD
    // =========================================================

    /// <summary>Writes the current progress (signed, atomic). Returns false on failure.</summary>
    public bool Save()
    {
        try
        {
            progress.Normalize();
            store.Write(Encode(progress));
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
            var fresh = new PlayerProgressData();
            fresh.Normalize();
            return fresh;
        }

        if (fromBackup)
            Debug.LogWarning($"[PlayerProgressManager] The save was not usable ({problem}); loaded the backup instead.");

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
            if (data == null || data.saveVersion >= PlayerProgressData.CurrentSaveVersion) return false;
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
