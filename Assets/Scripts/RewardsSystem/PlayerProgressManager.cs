using UnityEngine;
using System.IO;

/// <summary>
/// PURPOSE:
/// Owns the player's permanent, whole-game progression (total EXP,
/// total Reputation, levels completed) and persists it to disk as JSON.
///
/// This is the project's first WRITE path. JsonCaseLoader reads level/case
/// data from Resources (read-only at runtime, bundled into the build).
/// Progress cannot live there — Resources cannot be written to on device.
/// Instead this reads/writes Application.persistentDataPath, which is
/// writable on every platform (PC, Mac, Android, iOS).
///
/// Follows the same "never guess information" principle as
/// CaseValidationResult<T>: Load() always produces a valid, non-null
/// PlayerProgressData, and failures are logged rather than silently
/// producing bad state.
/// </summary>
public class PlayerProgressManager : MonoBehaviour
{
    public static PlayerProgressManager Instance { get; private set; }

    private PlayerProgressData progress;

    private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Save");
    private string SavePath => Path.Combine(SaveDirectory, "progress.json");

    public PlayerProgressData CurrentProgress => progress;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[PlayerProgressManager] Duplicate instance detected. Destroying duplicate."
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        var loadResult = Load();

        progress = loadResult.IsSuccess
            ? loadResult.Data
            : new PlayerProgressData();

        Debug.Log(
            $"[PlayerProgressManager] Ready. " +
            $"EXP: {progress.totalExp}, Reputation: {progress.totalReputation}, " +
            $"Levels: {progress.levelsCompleted}"
        );
    }

    /// <summary>
    /// Folds a completed level's totals into permanent progression and saves.
    /// Call this once, when the player closes the Level Result popup.
    /// </summary>
    public void ApplyLevelResult(LevelTotalResult levelTotal)
    {
        if (levelTotal == null)
        {
            Debug.LogError("[PlayerProgressManager] ApplyLevelResult called with NULL levelTotal.");
            return;
        }

        progress.totalExp += levelTotal.TotalExp;
        progress.totalReputation += levelTotal.TotalReputation;
        progress.levelsCompleted += 1;

        Debug.Log(
            $"[PlayerProgressManager] Applied level result. " +
            $"New totals -> EXP: {progress.totalExp}, " +
            $"Reputation: {progress.totalReputation}, " +
            $"Levels: {progress.levelsCompleted}"
        );

        var saveResult = Save(progress);

        if (!saveResult.IsSuccess)
        {
            Debug.LogError($"[PlayerProgressManager] Save failed: {saveResult.ErrorMessage}");
        }
    }

    private CaseValidationResult<bool> Save(PlayerProgressData data)
    {
        try
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);

            Debug.Log($"[PlayerProgressManager] Progress saved to {SavePath}");

            return CaseValidationResult<bool>.Success(true);
        }
        catch (System.Exception e)
        {
            return CaseValidationResult<bool>.Fail($"Exception while saving progress: {e.Message}");
        }
    }

    private CaseValidationResult<PlayerProgressData> Load()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log(
                    $"[PlayerProgressManager] No save file found at {SavePath}. Starting fresh."
                );

                return CaseValidationResult<PlayerProgressData>.Fail("No save file found.");
            }

            string json = File.ReadAllText(SavePath);
            PlayerProgressData data = JsonUtility.FromJson<PlayerProgressData>(json);

            if (data == null)
            {
                return CaseValidationResult<PlayerProgressData>.Fail(
                    $"progress.json at {SavePath} could not be parsed."
                );
            }

            Debug.Log($"[PlayerProgressManager] Progress loaded from {SavePath}.");

            return CaseValidationResult<PlayerProgressData>.Success(data);
        }
        catch (System.Exception e)
        {
            return CaseValidationResult<PlayerProgressData>.Fail(
                $"Exception while loading progress: {e.Message}"
            );
        }
    }
}