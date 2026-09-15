using UnityEngine;

/// <summary>
/// PURPOSE:
/// Holds the currently active CaseData instance — UNCHANGED public shape
/// from before (Instance.CurrentCase is still exactly how every other
/// script accesses case data). The only change: CurrentCase is now
/// populated from a JSON-loaded CaseDefinition via CaseFactory instead of
/// a hardcoded placeholder.
///
/// PHASE 2 SCOPE: still loads a single fixed level/case
/// ("Level_01"/"case_001") at startup — multi-case progression (loading
/// case 2, 3, etc. and the Continue/Level Complete flow) arrives in
/// Phase 3. This phase's job is ONLY to prove JSON correctly replaces the
/// hardcoded case, nothing about progression yet.
/// </summary>
public class CaseManager : MonoBehaviour
{
    public static CaseManager Instance { get; private set; }

    [Header("Phase 2: Fixed Level/Case (multi-case progression in Phase 3)")]
    [SerializeField] private string levelId = "Level_01";
    [SerializeField] private string caseId = "case_001";

    public CaseData CurrentCase { get; private set; }
    public CaseDefinition CurrentDefinition { get; private set; }

    private void Awake()
    {
        Instance = this;
        // NOTE: LoadCase() is no longer called here — CaseProgressionManager.StartLevel()
        // now owns the first case-load, so it can correctly initialize
        // currentLevel/currentCaseIndex before anything requests CurrentCase.
    }

    public void LoadCase(string levelToLoad, string caseToLoad)
    {
        var caseResult = JsonCaseLoader.LoadCase(levelToLoad, caseToLoad);

        if (!caseResult.IsSuccess)
        {
            Debug.LogError($"[CaseManager] Failed to load case '{caseToLoad}' from level '{levelToLoad}': {caseResult.ErrorMessage}");
            Debug.LogError("[CaseManager] Falling back to an empty placeholder case so the game does not crash.");
            CurrentDefinition = CreateFallbackDefinition();
        }
        else
        {
            CurrentDefinition = caseResult.Data;
        }

        CurrentCase = CaseFactory.CreateCaseData(CurrentDefinition);
        Debug.Log($"[CaseManager] Loaded case '{CurrentDefinition.caseId}' — client: {CurrentCase.fullName}");
    }

    /// <summary>
    /// Safety net so a missing/broken JSON file never crashes the game —
    /// it degrades to a clearly-labeled placeholder instead, matching the
    /// "no broken intermediate states" requirement.
    /// </summary>
    private CaseDefinition CreateFallbackDefinition()
    {
        return new CaseDefinition
        {
            caseId = "fallback_case",
            caseNumber = "ERROR-0000",
            taxYear = "----",
            fullName = "MISSING CASE DATA",
            caseTitle = "Case Failed To Load",
            caseSummary = "This case could not be loaded. Check the Console for the JSON error.",
            answerKey = new AnswerKeyDefinition(),
            documents = new System.Collections.Generic.List<DocumentDefinition>(),
            potentialIssues = new System.Collections.Generic.List<string>()
        };
    }

    /// <summary>
    /// Resets the CURRENT case's player-entered data in place (no JSON reload),
    /// for the "audit failed, try again" loop. CurrentDefinition/CurrentCase
    /// stay the same object — only CaseData.ResetForRetry() is invoked.
    /// </summary>
    public void ResetCurrentCaseForRetry()
    {
        if (CurrentCase == null)
        {
            Debug.LogError("[CaseManager] Cannot reset — CurrentCase is NULL.");
            return;
        }

        CurrentCase.ResetForRetry();
        Debug.Log($"[CaseManager] Case '{CurrentCase.caseNumber}' reset for retry.");
    }
}