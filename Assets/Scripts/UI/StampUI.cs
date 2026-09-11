
using UnityEngine;
using TMPro;

/// <summary>
/// Handles applying a Ready / Not Ready assessment to the current case.
///
/// The 3D stamps are responsible for selecting the stamp type.
/// This class handles:
/// - Game-data update
/// - FSM transition
/// - World Space Case Folder refresh
/// - Stamp feedback
/// </summary>
public class StampUI : MonoBehaviour
{
    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private float feedbackDisplayDuration = 2.5f;

    public static StampUI Instance { get; private set; }


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        Instance = this;

        ClearFeedback();
    }


    // =========================================================
    // APPLY STAMP TYPE
    // =========================================================

    /// <summary>
    /// Called by Stamp3DDrag when a 3D stamp is successfully
    /// dropped onto the Case Folder.
    /// </summary>
    public void ApplyStampType(StampType type)
    {
        // -----------------------------------------------------
        // Validate CaseManager
        // -----------------------------------------------------

        if (CaseManager.Instance == null)
        {
            Debug.LogError(
                "[StampUI] CaseManager.Instance is NULL."
            );

            return;
        }


        // -----------------------------------------------------
        // Validate Current Case
        // -----------------------------------------------------

        if (CaseManager.Instance.CurrentCase == null)
        {
            Debug.LogError(
                "[StampUI] CurrentCase is NULL."
            );

            return;
        }


        // -----------------------------------------------------
        // Get Current CaseData
        // -----------------------------------------------------

        CaseData data =
            CaseManager.Instance.CurrentCase;


        Debug.Log(
            $"[StampUI] Applying 3D stamp: {type}"
        );


        // -----------------------------------------------------
        // Apply stamp
        // -----------------------------------------------------

        ApplyStamp(
            type,
            data
        );
    }


    // =========================================================
    // APPLY STAMP
    // =========================================================

    private void ApplyStamp(
        StampType type,
        CaseData data
    )
    {
        // -----------------------------------------------------
        // Mark assessment as stamped
        // -----------------------------------------------------

        data.assessmentStamped = true;


        // -----------------------------------------------------
        // READY FOR FILING
        // -----------------------------------------------------

        if (type == StampType.ReadyForFiling)
        {
            data.caseAssessment =
                CaseAssessment.ReadyForFiling;

            data.filingStatus =
                FilingStatus.ReadyForFiling;


            ShowFeedback(
                "Stamped: READY FOR FILING.",
                false
            );


            // -------------------------------------------------
            // FSM Transition
            // -------------------------------------------------

            if (GameStateMachine.Instance != null &&
                GameStateMachine.Instance.CurrentState
                    is StampAssessmentState)
            {
                GameStateMachine.Instance.ChangeState(
                    new PrepareReturnState()
                );
            }
        }


        // -----------------------------------------------------
        // NOT READY FOR FILING
        // -----------------------------------------------------

        else
        {
            data.caseAssessment =
                CaseAssessment.NotReadyForFiling;


            ShowFeedback(
                "Stamped: NOT READY FOR FILING.",
                false
            );


            // -------------------------------------------------
            // FSM Transition
            // -------------------------------------------------

            if (GameStateMachine.Instance != null &&
                GameStateMachine.Instance.CurrentState
                    is StampAssessmentState)
            {
                GameStateMachine.Instance.ChangeState(
                    new ComplianceAuditState()
                );
            }
        }


        // =====================================================
        // REFRESH WORLD SPACE CASE FOLDER
        // =====================================================

        StampCaseFolderUI stampFolder =
            FindFirstObjectByType<StampCaseFolderUI>();


        if (stampFolder == null)
        {
            Debug.LogError(
                "[StampUI] StampCaseFolderUI NOT FOUND!"
            );
        }
        else
        {
            Debug.Log(
                $"[StampUI] " +
                $"Refreshing StampCaseFolderUI | " +
                $"Assessment={data.caseAssessment} | " +
                $"Stamped={data.assessmentStamped}"
            );


            // -------------------------------------------------
            // Refresh the World Space Case Folder
            // using the UPDATED CaseData.
            // -------------------------------------------------

            stampFolder.Refresh();
        }


        // =====================================================
        // TUTORIAL
        // =====================================================

        if (TutorialController.Instance != null)
        {
            TutorialController.Instance.ReportInteraction(
                "stamp_applied"
            );
        }


        // =====================================================
        // FINAL DEBUG
        // =====================================================

        Debug.Log(
            $"[StampUI] Case assessment updated: " +
            $"{data.caseAssessment}"
        );
    }


    // =========================================================
    // FEEDBACK
    // =========================================================

    private void ShowFeedback(
        string message,
        bool isWarning
    )
    {
        // Feedback Text is optional.
        // If it is not assigned, simply skip the visual feedback.

        if (feedbackText == null)
            return;


        feedbackText.text =
            message;


        feedbackText.color =
            isWarning
                ? Color.red
                : Color.black;


        CancelInvoke(
            nameof(ClearFeedback)
        );


        Invoke(
            nameof(ClearFeedback),
            feedbackDisplayDuration
        );
    }


    // =========================================================
    // CLEAR FEEDBACK
    // =========================================================

    private void ClearFeedback()
    {
        if (feedbackText != null)
        {
            feedbackText.text = "";
        }
    }
}
