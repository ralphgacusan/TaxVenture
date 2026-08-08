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
/// ReceiveCaseState
///     ↓
/// ... existing case gameplay ...
///     ↓
/// CaseCompleteState
///     ↓
/// OnCaseFinished()
///     ↓
/// ┌───────────────────────────────┐
/// │ More cases?                   │
/// │                               │
/// │ YES → Case Completion UI      │
/// │       → next case             │
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[CaseProgressionManager] Duplicate instance detected. Destroying duplicate."
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

        var levelResult = JsonCaseLoader.LoadLevel(levelId);

        if (!levelResult.IsSuccess)
        {
            Debug.LogError(
                $"[CaseProgressionManager] Failed to load level '{levelId}': " +
                levelResult.ErrorMessage
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
                $"[CaseProgressionManager] Level '{levelId}' contains no case IDs."
            );

            return;
        }

        currentCaseIndex = 0;
        levelStarted = true;

        Debug.Log(
            $"[CaseProgressionManager] Level '{levelId}' loaded successfully."
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
                "[CaseProgressionManager] Cannot load case because " +
                "the level has not been initialized."
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

        string caseId = currentLevel.caseIds[currentCaseIndex];

        Debug.Log(
            $"[CaseProgressionManager] Loading case " +
            $"{currentCaseIndex + 1}/{currentLevel.caseIds.Count}: {caseId}"
        );

        CaseManager.Instance.LoadCase(levelId, caseId);

        Debug.Log(
            $"[CaseProgressionManager] Loaded Definition: " +
            $"{CaseManager.Instance.CurrentDefinition?.caseId}"
        );

        Debug.Log(
            $"[CaseProgressionManager] Loaded Case: " +
            $"{CaseManager.Instance.CurrentCase?.caseNumber}"
        );

        GameStateMachine.Instance.UnlockProgression();

        GameStateMachine.Instance.ChangeState(
            new ReceiveCaseState()
        );
    }

    /// <summary>
    /// Called by CaseCompleteState.
    /// Determines whether another case remains.
    /// </summary>
    public void OnCaseFinished()
    {
        Debug.Log("========== CASE PROGRESSION ==========");

        Debug.Log(
            $"[CaseProgressionManager] levelStarted: {levelStarted}"
        );

        Debug.Log(
            $"[CaseProgressionManager] currentLevel == null: " +
            $"{currentLevel == null}"
        );

        Debug.Log(
            $"[CaseProgressionManager] currentCaseIndex: " +
            $"{currentCaseIndex}"
        );

        Debug.Log(
            $"[CaseProgressionManager] levelId: {levelId}"
        );

        if (!levelStarted || currentLevel == null)
        {
            Debug.LogError(
                "[CaseProgressionManager] Cannot complete case because " +
                "the current level was never initialized."
            );

            return;
        }

        if (currentLevel.caseIds == null ||
            currentLevel.caseIds.Count == 0)
        {
            Debug.LogError(
                "[CaseProgressionManager] Current level contains no cases."
            );

            return;
        }

        bool isLastCase =
            currentCaseIndex >= currentLevel.caseIds.Count - 1;

        Debug.Log(
            $"[CaseProgressionManager] Case " +
            $"{currentCaseIndex + 1}/{currentLevel.caseIds.Count} completed."
        );

        Debug.Log(
            $"[CaseProgressionManager] Is last case: {isLastCase}"
        );

        if (isLastCase)
        {
            Debug.Log(
                "[CaseProgressionManager] LAST CASE → LevelCompleteState"
            );

            GameStateMachine.Instance.ChangeState(
                new LevelCompleteState()
            );
        }
        else
        {
            Debug.Log(
                "[CaseProgressionManager] More cases remain → " +
                "showing CaseCompletionUI."
            );

            if (CaseCompletionUI.Instance == null)
            {
                Debug.LogError(
                    "[CaseProgressionManager] CaseCompletionUI.Instance is NULL!"
                );

                return;
            }

            CaseCompletionUI.Instance.ShowContinuePrompt(
                AdvanceToNextCase
            );
        }
    }

    private void AdvanceToNextCase()
    {
        Debug.Log(
            "[CaseProgressionManager] Continue pressed → next case."
        );

        currentCaseIndex++;

        Debug.Log(
            $"[CaseProgressionManager] Advancing to case " +
            $"{currentCaseIndex + 1}/{currentLevel.caseIds.Count}"
        );

        LoadCurrentCase();
    }

    public bool IsCurrentCaseLast()
    {
        if (!levelStarted || currentLevel == null)
            return true;

        return currentCaseIndex >=
               currentLevel.caseIds.Count - 1;
    }

    public int TotalCasesInLevel =>
        currentLevel?.caseIds?.Count ?? 0;

    public int CurrentCaseNumber =>
        currentCaseIndex + 1;
}