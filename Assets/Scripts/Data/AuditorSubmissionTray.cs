
using System.Collections.Generic;
using UnityEngine;

public class AuditorSubmissionTray : MonoBehaviour, IDataValueDestination
{
    [SerializeField] private ValueClickDestination clickDestination;
    [SerializeField] private AuditorInteractable auditor;

    private HashSet<string> submittedKeys = new HashSet<string>();

    private void Awake()
    {
        if (clickDestination != null)
            clickDestination.Initialize(this);
    }

    public void ResetSubmission()
    {
        submittedKeys.Clear();
    }

    public bool CanAccept(DataValue value)
    {
        return value != null &&
               value.SemanticKey == "Submit_CaseFolder";
    }

    public bool TryReceiveValue(DataValue value)
    {
        if (value == null)
        {
            Debug.LogWarning(
                "[AuditorSubmissionTray] Received NULL value.");
            return false;
        }

        Debug.Log(
            $"[AuditorSubmissionTray] Received: {value.SemanticKey}");

        if (!CanAccept(value))
        {
            Debug.Log(
                $"[AuditorSubmissionTray] Rejected: {value.SemanticKey}");
            return false;
        }

        if (submittedKeys.Contains(value.SemanticKey))
        {
            Debug.Log(
                "[AuditorSubmissionTray] Case Folder already submitted.");
            return false;
        }

        submittedKeys.Add(value.SemanticKey);

        Debug.Log(
            "[AuditorSubmissionTray] Case Folder accepted!");

        CheckIfSubmissionComplete();

        return true;
    }

    private void CheckIfSubmissionComplete()
    {
        if (CaseManager.Instance == null)
        {
            Debug.LogError(
                "[AuditorSubmissionTray] CaseManager.Instance is NULL.");
            return;
        }

        if (CaseManager.Instance.CurrentCase == null)
        {
            Debug.LogError(
                "[AuditorSubmissionTray] CurrentCase is NULL.");
            return;
        }

        if (auditor == null)
        {
            Debug.LogError(
                "[AuditorSubmissionTray] AuditorInteractable is NOT assigned.");
            return;
        }

        bool folderSubmitted =
            submittedKeys.Contains("Submit_CaseFolder");

        Debug.Log(
            $"[AuditorSubmissionTray] Folder Submitted: {folderSubmitted}");

        if (!folderSubmitted)
            return;

        Debug.Log(
            "[AuditorSubmissionTray] Submission complete!");

        auditor.BeginFinalAudit();
    }
}

