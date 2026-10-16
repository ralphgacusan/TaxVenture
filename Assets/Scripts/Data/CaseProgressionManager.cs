
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

    [SerializeField] private string startingLevelId = "Level_01";

    private bool levelStarted = false;
    private bool waitingForNextCase = false;

    // Add near your other private fields:
    private readonly LevelRewardAccumulator rewardAccumulator = new LevelRewardAccumulator();

    public LevelRewardAccumulator RewardAccumulator => rewardAccumulator;
    public string CurrentLevelId => levelId;
    public string CurrentLevelName => currentLevel?.levelName;

    /// <summary>
    /// Raised after a case reward has been calculated and stored in the
    /// accumulator. Presentation-only hook (HUD); listeners must not alter rewards.
    /// </summary>
    public static event System.Action<LevelResultData> OnCaseRewardStored;

    /// <summary>
    /// The level's full score (client talks, side quest, cases) against the
    /// maximum and passing line from Resources/Levels/&lt;id&gt;/rewards.json.
    /// </summary>
    public LevelScoreLedger Score { get; } = new LevelScoreLedger();

    /// <summary>
    /// Raised when the Auditor sends a case back: mistakes found and what the
    /// failed attempt costs the case's final reward. Presentation hook only.
    /// </summary>
    public static event System.Action<int, LevelScoring.Award> OnAuditFailed;

    // Audit attempts and mistakes for the current case (reset per case).
    private int caseAuditAttempts;
    private int caseAuditMistakes;

    private float caseStartTime;


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


    private void OnEnable()
    {
        TownEvents.OnSideQuestCompleted += LevelGate.MarkSideQuestDone;
    }

    private void OnDisable()
    {
        TownEvents.OnSideQuestCompleted -= LevelGate.MarkSideQuestDone;
    }

    private void Start()
    {
        Debug.Log("[CaseProgressionManager] Start.");

        StartLevel(startingLevelId);
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
        rewardAccumulator.Reset(); // <-- add this line

        LevelRewardRules rules = LevelRewardRules.Load(levelId);
        Score.Reset(rules, currentLevel.caseIds.Count);
        LevelGate.Reset(rules.requireSideQuestBeforeOffice);
        caseAuditAttempts = 0;
        caseAuditMistakes = 0;

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

        caseStartTime = Time.time;
        caseAuditAttempts = 0;
        caseAuditMistakes = 0;


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
    /// Time elapsed since the current case was loaded, formatted the same
    /// way LevelTimer formats its total (HH:MM:SS). Used for the per-case
    /// result popup — NOT the level-wide LevelTimer value.
    /// </summary>
    public string GetCurrentCaseFormattedTime()
    {
        float elapsed = Time.time - caseStartTime;

        int hours = Mathf.FloorToInt(elapsed / 3600f);
        int minutes = Mathf.FloorToInt((elapsed % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);

        return $"{hours:00}:{minutes:00}:{seconds:00}";
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

    public int CurrentCaseAuditAttempts => caseAuditAttempts;
    public int CurrentCaseAuditMistakes => caseAuditMistakes;

    /// <summary>
    /// Called by the Auditor after each audit of the current case. A failed
    /// audit adds its mistakes and counts as a submission that must be redone.
    /// </summary>
    public void RecordAuditAttempt(int mistakes, bool passed)
    {
        caseAuditAttempts++;
        if (passed) return;

        int found = Mathf.Max(0, mistakes);
        caseAuditMistakes += found;
        OnAuditFailed?.Invoke(found, LevelScoring.FailedAuditCost(Score.Rules, found));
    }

    /// <summary>
    /// +hearts for finishing a client conversation (once per client). Returns
    /// null when this client already gave the bonus.
    /// </summary>
    public ScoreEntry AwardConversation(string npcId, string label, Vector3? origin) =>
        levelStarted ? Score.AddConversation(npcId, label, origin) : null;


    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Calculates a case's reward exactly once and stores it in the
    /// accumulator. Every place that needs a case's reward (the per-case
    /// popup, or the perfect-case silent path) must go through here —
    /// never call RewardsCalculator.BuildResult directly anywhere else.
    /// </summary>
    public LevelResultData CalculateAndStoreCaseReward(CaseData data)
    {
        LevelResultData result = RewardsCalculator.BuildResult(data);

        // Level score: a case that went through the Auditor is scored from its
        // audits (mistakes found + extra submissions, per rewards.json); other
        // cases keep the calculator's values. The result shows the same numbers.
        string label = $"Case {CurrentCaseNumber}";
        if (caseAuditAttempts > 0)
        {
            ScoreEntry entry = Score.AddCase(label, caseAuditMistakes, caseAuditAttempts - 1);
            result.ExpEarned = entry.Exp;
            result.ReputationEarned = entry.Hearts;
        }
        else
        {
            Score.AddCaseValues(label, result.ExpEarned, result.ReputationEarned);
        }

        rewardAccumulator.AddCaseResult(result);
        OnCaseRewardStored?.Invoke(result);

        Debug.Log(
            $"[CaseProgressionManager] Case reward calculated & stored. " +
            $"EXP: {result.ExpEarned}, Reputation: {result.ReputationEarned}. " +
            $"Running level total -> EXP: {rewardAccumulator.GetTotalExp()}, " +
            $"Reputation: {rewardAccumulator.GetTotalReputation()}"
        );

        return result;
    }
}
