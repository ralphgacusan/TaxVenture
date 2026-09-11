
using UnityEngine;

/// <summary>
/// Drives case-to-case progression within a JSON-defined level.
///
/// Flow:
///
/// StartLevel()
///     ↓
/// LoadCurrentCase()
///     ↓
/// Reset case-specific systems
///     ↓
/// Load correct Conference Room Client
///     ↓
/// ReceiveCaseState
///     ↓
/// ... case gameplay ...
///     ↓
/// CaseCompleteState
///     ↓
/// OnCaseFinished()
///     ↓
/// ┌───────────────────────────────┐
/// │ More cases?                   │
/// │                               │
/// │ YES → Case Completion UI      │
/// │       → Continue              │
/// │       → Load next case        │
/// │                               │
/// │ NO  → LevelCompleteState      │
/// └───────────────────────────────┘
/// </summary>
public class CaseProgressionManager : MonoBehaviour
{
    public static CaseProgressionManager Instance { get; private set; }

    private LevelDefinition currentLevel;
    private int currentCaseIndex;
    private string levelId;

    private bool levelStarted = false;
    private bool waitingForNextCase = false;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[CaseProgressionManager] Duplicate instance detected. " +
                "Destroying duplicate."
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        Debug.Log("[CaseProgressionManager] Awake.");
    }


    private void Start()
    {
        Debug.Log("[CaseProgressionManager] Start.");

        StartLevel("Level_01");
    }


    public void StartLevel(string levelIdToLoad)
    {
        Debug.Log(
            $"[CaseProgressionManager] StartLevel called: {levelIdToLoad}"
        );

        levelId = levelIdToLoad;

        levelStarted = false;
        waitingForNextCase = false;

        var levelResult = JsonCaseLoader.LoadLevel(levelId);

        if (!levelResult.IsSuccess)
        {
            Debug.LogError(
                $"[CaseProgressionManager] Failed to load level " +
                $"'{levelId}': {levelResult.ErrorMessage}"
            );

            return;
        }

        currentLevel = levelResult.Data;

        if (currentLevel == null)
        {
            Debug.LogError(
                "[CaseProgressionManager] JsonCaseLoader returned SUCCESS " +
                "but LevelDefinition is NULL."
            );

            return;
        }

        if (currentLevel.caseIds == null ||
            currentLevel.caseIds.Count == 0)
        {
            Debug.LogError(
                $"[CaseProgressionManager] Level '{levelId}' " +
                "contains no case IDs."
            );

            return;
        }

        currentCaseIndex = 0;
        levelStarted = true;

        Debug.Log(
            $"[CaseProgressionManager] Level '{levelId}' " +
            "loaded successfully."
        );

        Debug.Log(
            $"[CaseProgressionManager] Total cases: " +
            $"{currentLevel.caseIds.Count}"
        );

        LoadCurrentCase();
    }


    private void LoadCurrentCase()
    {
        if (!levelStarted || currentLevel == null)
        {
            Debug.LogError(
                "[CaseProgressionManager] Cannot load case: " +
                "level not initialized."
            );

            return;
        }

        if (currentLevel.caseIds == null ||
            currentLevel.caseIds.Count == 0)
        {
            Debug.LogError(
                "[CaseProgressionManager] Cannot load case: " +
                "no case IDs."
            );

            return;
        }

        if (currentCaseIndex < 0 ||
            currentCaseIndex >= currentLevel.caseIds.Count)
        {
            Debug.LogError(
                $"[CaseProgressionManager] Invalid case index: " +
                $"{currentCaseIndex}"
            );

            return;
        }

        if (CaseManager.Instance == null)
        {
            Debug.LogError(
                "[CaseProgressionManager] CaseManager.Instance is NULL."
            );

            return;
        }

        if (GameStateMachine.Instance == null)
        {
            Debug.LogError(
                "[CaseProgressionManager] GameStateMachine.Instance is NULL."
            );

            return;
        }

        string caseId =
            currentLevel.caseIds[currentCaseIndex];

        Debug.Log(
            $"[CaseProgressionManager] Loading case " +
            $"{currentCaseIndex + 1}/" +
            $"{currentLevel.caseIds.Count}: " +
            $"{caseId}"
        );


        // =========================================================
        // LOAD NEW CASE DATA
        // =========================================================

        CaseManager.Instance.LoadCase(
            levelId,
            caseId
        );


        if (CaseManager.Instance.CurrentCase == null)
        {
            Debug.LogError(
                "[CaseProgressionManager] CaseManager loaded " +
                "NULL CurrentCase."
            );

            return;
        }


        // =========================================================
        // RESET CASE-SPECIFIC SYSTEMS
        // =========================================================

        if (AuditorSubmissionTray.Instance != null)
        {
            AuditorSubmissionTray.Instance.ResetSubmission();
        }
        else
        {
            Debug.LogWarning(
                "[CaseProgressionManager] " +
                "AuditorSubmissionTray.Instance is NULL. " +
                "Submission state could not be reset."
            );
        }


        // =========================================================
        // LOAD CONFERENCE ROOM CLIENT
        // =========================================================

        string clientId =
            CaseManager.Instance.CurrentDefinition?.clientId;

        string clientModel =
            CaseManager.Instance.CurrentDefinition?.clientModel;

        Debug.Log(
            $"[CaseProgressionManager] Client ID: " +
            $"{clientId}"
        );

        Debug.Log(
            $"[CaseProgressionManager] Client Model: " +
            $"{clientModel}"
        );


        if (ConferenceRoomClient.Instance != null)
        {
            if (!string.IsNullOrEmpty(clientId))
            {
                ConferenceRoomClient.Instance.ShowClient(clientId);
            }
            else
            {
                Debug.LogWarning(
                    "[CaseProgressionManager] Current case has no clientId."
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[CaseProgressionManager] " +
                "ConferenceRoomClient.Instance is NULL. " +
                "Conference Room client was not changed."
            );
        }


        // =========================================================
        // LOG LOADED CASE
        // =========================================================

        Debug.Log(
            $"[CaseProgressionManager] Loaded Definition: " +
            $"{CaseManager.Instance.CurrentDefinition?.caseId}"
        );

        Debug.Log(
            $"[CaseProgressionManager] Loaded Case: " +
            $"{CaseManager.Instance.CurrentCase.caseNumber}"
        );


        waitingForNextCase = false;


        // =========================================================
        // START NEW CASE
        // =========================================================

        GameStateMachine.Instance.UnlockProgression();

        GameStateMachine.Instance.ChangeState(
            new ReceiveCaseState()
        );

        Debug.Log(
            $"[CaseProgressionManager] Case " +
            $"{currentCaseIndex + 1} is now active."
        );
    }


    /// <summary>
    /// Called by CaseCompleteState.
    /// Determines whether another case remains.
    /// </summary>
    public void OnCaseFinished()
    {
        Debug.Log("========================================");
        Debug.Log(
            "[CaseProgressionManager] OnCaseFinished() CALLED"
        );
        Debug.Log("========================================");


        if (!levelStarted)
        {
            Debug.LogError(
                "[CaseProgressionManager] Level has not started."
            );

            return;
        }


        if (currentLevel == null)
        {
            Debug.LogError(
                "[CaseProgressionManager] currentLevel is NULL."
            );

            return;
        }


        if (currentLevel.caseIds == null ||
            currentLevel.caseIds.Count == 0)
        {
            Debug.LogError(
                "[CaseProgressionManager] No cases exist " +
                "in current level."
            );

            return;
        }


        if (waitingForNextCase)
        {
            Debug.LogWarning(
                "[CaseProgressionManager] Already waiting for " +
                "next case. Ignoring duplicate completion call."
            );

            return;
        }


        bool isLastCase =
            currentCaseIndex >=
            currentLevel.caseIds.Count - 1;


        Debug.Log(
            $"[CaseProgressionManager] Finished case " +
            $"{currentCaseIndex + 1}/" +
            $"{currentLevel.caseIds.Count}"
        );

        Debug.Log(
            $"[CaseProgressionManager] Is last case: " +
            $"{isLastCase}"
        );


        // =========================================================
        // LAST CASE
        // =========================================================

        if (isLastCase)
        {
            Debug.Log(
                "[CaseProgressionManager] LAST CASE → " +
                "LevelCompleteState"
            );

            GameStateMachine.Instance.ChangeState(
                new LevelCompleteState()
            );

            return;
        }


        // =========================================================
        // MORE CASES REMAIN
        // =========================================================

        waitingForNextCase = true;

        Debug.Log(
            "[CaseProgressionManager] More cases remain → " +
            "showing CaseCompletionUI."
        );


        if (CaseCompletionUI.Instance == null)
        {
            Debug.LogError(
                "[CaseProgressionManager] " +
                "CaseCompletionUI.Instance is NULL."
            );

            waitingForNextCase = false;

            return;
        }


        CaseCompletionUI.Instance.ShowContinuePrompt(
            AdvanceToNextCase
        );
    }


    private void AdvanceToNextCase()
    {
        Debug.Log(
            "[CaseProgressionManager] AdvanceToNextCase() CALLED."
        );


        if (!waitingForNextCase)
        {
            Debug.LogWarning(
                "[CaseProgressionManager] AdvanceToNextCase called " +
                "when not waiting for a next case."
            );

            return;
        }


        if (currentLevel == null ||
            currentLevel.caseIds == null)
        {
            Debug.LogError(
                "[CaseProgressionManager] Cannot advance: " +
                "level data is NULL."
            );

            waitingForNextCase = false;

            return;
        }


        if (currentCaseIndex >=
            currentLevel.caseIds.Count - 1)
        {
            Debug.LogWarning(
                "[CaseProgressionManager] Cannot advance past last case."
            );

            waitingForNextCase = false;

            return;
        }


        currentCaseIndex++;


        Debug.Log(
            $"[CaseProgressionManager] Advancing to case " +
            $"{currentCaseIndex + 1}/" +
            $"{currentLevel.caseIds.Count}"
        );


        LoadCurrentCase();
    }


    public bool IsCurrentCaseLast()
    {
        if (!levelStarted ||
            currentLevel == null ||
            currentLevel.caseIds == null)
        {
            return true;
        }

        return currentCaseIndex >=
               currentLevel.caseIds.Count - 1;
    }


    public int TotalCasesInLevel =>
        currentLevel?.caseIds?.Count ?? 0;


    public int CurrentCaseNumber =>
        currentCaseIndex + 1;


    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
