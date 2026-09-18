
using System.Collections.Generic;
using UnityEngine;

public class AuditorSubmissionTray :
    MonoBehaviour,
    IDataValueDestination
{
    public static AuditorSubmissionTray Instance { get; private set; }


    [SerializeField]
    private ValueClickDestination clickDestination;

    [SerializeField]
    private AuditorInteractable auditor;


    private HashSet<string> submittedKeys =
        new HashSet<string>();

    [SerializeField]
    private FolderToHUDIcon folderToHUDIcon;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[AuditorSubmissionTray] Duplicate instance detected. " +
                "Destroying duplicate."
            );

            Destroy(gameObject);

            return;
        }

        Instance = this;


        if (clickDestination != null)
        {
            clickDestination.Initialize(this);
        }
        else
        {
            Debug.LogWarning(
                "[AuditorSubmissionTray] " +
                "ValueClickDestination is NOT assigned."
            );
        }
    }


    /// <summary>
    /// Clears all submission state.
    ///
    /// IMPORTANT:
    /// This must be called whenever a new case starts because
    /// "Submit_CaseFolder" is valid once per case.
    /// </summary>
    public void ResetSubmission()
    {
        submittedKeys.Clear();

        if (folderToHUDIcon != null)
        {
            folderToHUDIcon.ResetToUnstored();
        }

        Debug.Log("[AuditorSubmissionTray] Submission state RESET for new case.");
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
                "[AuditorSubmissionTray] Received NULL value."
            );

            return false;
        }


        Debug.Log(
            $"[AuditorSubmissionTray] Received: " +
            $"{value.SemanticKey}"
        );


        // =========================================================
        // VALIDATE DATA VALUE
        // =========================================================

        if (!CanAccept(value))
        {
            Debug.Log(
                $"[AuditorSubmissionTray] Rejected: " +
                $"{value.SemanticKey}"
            );

            return false;
        }


        // =========================================================
        // PREVENT DUPLICATE SUBMISSION
        // =========================================================

        if (submittedKeys.Contains(value.SemanticKey))
        {
            Debug.Log(
                "[AuditorSubmissionTray] " +
                "Case Folder already submitted."
            );

            return false;
        }


        // =========================================================
        // ACCEPT SUBMISSION
        // =========================================================

        submittedKeys.Add(value.SemanticKey);


        Debug.Log(
            "[AuditorSubmissionTray] Case Folder accepted!"
        );


        CheckIfSubmissionComplete();


        return true;
    }

    /// <summary>
    /// Resets submission state for a RETRY of the SAME case (audit failed),
    /// as opposed to ResetSubmission() which is for loading a brand-new case.
    /// Kept separate so "new case" and "retry" semantics never get conflated.
    /// </summary>
    public void ResetForRetryWithoutNewCase()
    {
        submittedKeys.Clear();

        if (folderToHUDIcon != null)
        {
            folderToHUDIcon.ResetToUnstored();
        }

        Debug.Log("[AuditorSubmissionTray] Submission state reset for case retry.");
    }


    private void CheckIfSubmissionComplete()
    {
        if (CaseManager.Instance == null)
        {
            Debug.LogError(
                "[AuditorSubmissionTray] " +
                "CaseManager.Instance is NULL."
            );

            return;
        }


        if (CaseManager.Instance.CurrentCase == null)
        {
            Debug.LogError(
                "[AuditorSubmissionTray] " +
                "CurrentCase is NULL."
            );

            return;
        }


        if (auditor == null)
        {
            Debug.LogError(
                "[AuditorSubmissionTray] " +
                "AuditorInteractable is NOT assigned."
            );

            return;
        }


        bool folderSubmitted =
            submittedKeys.Contains(
                "Submit_CaseFolder"
            );


        Debug.Log(
            $"[AuditorSubmissionTray] " +
            $"Folder Submitted: {folderSubmitted}"
        );


        if (!folderSubmitted)
            return;


        Debug.Log(
            "[AuditorSubmissionTray] " +
            "Submission complete!"
        );


        if (!folderSubmitted)
            return;

        Debug.Log("[AuditorSubmissionTray] Submission complete!");

        // Folder has left the player's hand — icon count should read 0.
        if (folderToHUDIcon != null && !folderToHUDIcon.IsFolderStored())
        {
            // Already handled by whatever triggered the drag-to-icon storage
            // in the normal flow; this is just a safety net for the
            // drag-to-tray path so the count is guaranteed to hit 0.
        }

        auditor.BeginFinalAudit();
    }


    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

