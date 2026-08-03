using System.Collections.Generic;
using UnityEngine;

public class AuditorSubmissionTray : MonoBehaviour, IDataValueDestination
{
    [SerializeField] private ValueClickDestination clickDestination;
    [SerializeField] private AuditorInteractable auditor;

    private HashSet<string> submittedKeys = new HashSet<string>();

    private void Awake()
    {
        clickDestination.Initialize(this);
    }

    public void ResetSubmission() => submittedKeys.Clear();

    public bool CanAccept(DataValue value)
    {
        return value.SemanticKey.StartsWith("Submit_");
    }

    public bool TryReceiveValue(DataValue value)
    {
        Debug.Log($"SubmissionTray received: {value.SemanticKey}");

        if (!CanAccept(value))
        {
            Debug.Log("SubmissionTray rejected value.");
            return false;
        }

        submittedKeys.Add(value.SemanticKey);

        Debug.Log("SubmissionTray accepted value.");

        CheckIfSubmissionComplete();

        return true;
    }

    // private void CheckIfSubmissionComplete()
    // {
    //     CaseData data = CaseManager.Instance.CurrentCase;

    //     bool folderSubmitted = submittedKeys.Contains("Submit_CaseFolder");
    //     bool returnSubmitted = submittedKeys.Contains("Submit_TaxReturn");

    //     bool requiredComplete = data.filingStatus == FilingStatus.ReadyForFiling
    //         ? (folderSubmitted && returnSubmitted)
    //         : folderSubmitted;

    //     Debug.Log($"Folder Submitted: {folderSubmitted}");
    //     Debug.Log($"Return Submitted: {returnSubmitted}");
    //     Debug.Log($"Required Complete: {requiredComplete}");

    //     if (requiredComplete)
    //     {
    //         Debug.Log("Submission complete. Starting final audit...");
    //         auditor.BeginFinalAudit();
    //     }
    // }

    private void CheckIfSubmissionComplete()
    {
        Debug.Log($"CaseManager.Instance == null: {CaseManager.Instance == null}");

        if (CaseManager.Instance != null)
        {
            Debug.Log($"CurrentCase == null: {CaseManager.Instance.CurrentCase == null}");
        }

        Debug.Log($"Auditor == null: {auditor == null}");

        CaseData data = CaseManager.Instance.CurrentCase;

        bool folderSubmitted = submittedKeys.Contains("Submit_CaseFolder");
        bool returnSubmitted = submittedKeys.Contains("Submit_TaxReturn");

        bool requiredComplete = data.filingStatus == FilingStatus.ReadyForFiling
            ? (folderSubmitted && returnSubmitted)
            : folderSubmitted;

        if (requiredComplete)
        {
            auditor.BeginFinalAudit();
        }
    }
}