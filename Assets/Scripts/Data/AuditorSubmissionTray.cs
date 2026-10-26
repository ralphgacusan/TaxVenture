
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

    /// <summary>The case folder this tray receives (its HUD icon state), for the Level 1 checkpoint (T05).</summary>
    public FolderToHUDIcon Folder => folderToHUDIcon;

    /// <summary>True once the case folder was handed in and is being audited.</summary>
    public bool FolderSubmitted => submittedKeys.Contains("Submit_CaseFolder");


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

        ReturnStoredFolders();

        Debug.Log("[AuditorSubmissionTray] Submission state RESET for new case.");
    }


    /// <summary>
    /// Optional level rule (Level 2): return true to refuse a case-folder
    /// hand-in for now. Null = always allowed (Level 1 behaviour).
    /// </summary>
    public static System.Func<bool> SubmissionBlocked;

    public bool CanAccept(DataValue value)
    {
        return !ClientCopyHandover.CaseActionsBlocked && value != null &&
               value.SemanticKey == "Submit_CaseFolder";
    }


    public bool TryReceiveValue(DataValue value)
    {
        if (ClientCopyHandover.CaseActionsBlocked)
        {
            Debug.Log("[AuditorSubmissionTray] Refused: the approved Client Copy cannot be submitted.");
            return false;
        }

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

        if (SubmissionBlocked != null && SubmissionBlocked())
        {
            Debug.Log("[AuditorSubmissionTray] Refused: the level does not allow a hand-in yet.");
            return false;
        }

        // Level 1 (2026-10-26): the folder is handed to the Auditor in his conversation, never dropped on the tray.
        if (AuditorInteractable.TalkFirst)
        {
            Debug.Log("[AuditorSubmissionTray] Refused: talk to the Auditor and hand him the folder when he asks.");
            return false;
        }

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
    /// Level 1 (2026-10-26): the Auditor took the folder in his conversation.
    /// Marks the Case Folder submitted (one paper sound) without starting a
    /// second audit; the conversation runs the audit itself. False when the
    /// case or the hand-in is not possible now.
    /// </summary>
    public bool AcceptFromConversation()
    {
        if (ClientCopyHandover.CaseActionsBlocked || CaseManager.Instance == null || CaseManager.Instance.CurrentCase == null) return false;
        if (submittedKeys.Contains("Submit_CaseFolder")) return false;
        submittedKeys.Add("Submit_CaseFolder");
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_folder_submit_01");
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

        ReturnStoredFolders();

        Debug.Log("[AuditorSubmissionTray] Submission state reset for case retry.");
    }


    /// <summary>Forgets a hand-in without moving any folder (the Auditor's conversation stopped before his check).</summary>
    public void ResetSubmissionKeysOnly() => submittedKeys.Clear();

    /// <summary>
    /// Brings the case folder back to the desk from the icon. Level 1 has two
    /// folder components (the closed desk folder and the open folder) and
    /// either can be the one dropped into the icon; resetting only the
    /// assigned one left a folder stored by the other stuck in the icon.
    /// </summary>
    private void ReturnStoredFolders()
    {
        bool returned = false;
        foreach (FolderToHUDIcon folder in FindObjectsByType<FolderToHUDIcon>(FindObjectsInactive.Include))
        {
            if (folder == null || folder.gameObject.scene != gameObject.scene || !folder.IsStoredOrViewing) continue;
            folder.ResetToUnstored();
            returned = true;
        }
        if (!returned && folderToHUDIcon != null) folderToHUDIcon.ResetToUnstored();
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


        Debug.Log("[AuditorSubmissionTray] Submission complete!");

        // Folder has left the player's hand — icon count should read 0.
        if (folderToHUDIcon != null && !folderToHUDIcon.IsFolderStored())
        {
            // Already handled by whatever triggered the drag-to-icon storage
            // in the normal flow; this is just a safety net for the
            // drag-to-tray path so the count is guaranteed to hit 0.
        }

        // Play paper sound when the case folder is successfully submitted.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEffect("sfx_folder_submit_01");
        }

        // Once: a second call made Level 2's Auditor repeat his lines.
        auditor.BeginFinalAudit();
    }


    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

