using UnityEngine;

/// <summary>
/// PURPOSE:
/// Minimal read-only confirmation popup shown after successfully archiving
/// a case.
///
/// One message, one OK button.
///
/// While the popup is open, player controls are locked.
/// Controls are restored when the popup closes.
///
/// After confirmation, the callback supplied by FilingCabinetInteractable
/// continues the case flow.
/// </summary>
public class ArchiveConfirmationPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject popupPanelRoot;

    private System.Action onClosed;

    private void Awake()
    {
        Hide();
    }

    // =========================================================
    // SHOW
    // =========================================================

    public void Show(System.Action onClosedCallback)
    {
        onClosed = onClosedCallback;

        if (popupPanelRoot != null)
        {
            popupPanelRoot.SetActive(true);
        }

        if (CameraController.Instance != null)
        {
            CameraController.Instance.LockPlayerControls();

            Debug.Log(
                "[ArchiveConfirmationPopupUI] " +
                "Player controls LOCKED."
            );
        }
    }

    // =========================================================
    // OK BUTTON
    // =========================================================

    public void OnOkPressed()
    {
        Debug.Log(
            "[ArchiveConfirmationPopupUI] " +
            "OK pressed."
        );

        // -----------------------------------------------------
        // Save callback BEFORE disabling the popup.
        //
        // IMPORTANT:
        // SetActive(false) triggers OnDisable(), so the callback
        // must be copied first.
        // -----------------------------------------------------

        System.Action callback = onClosed;

        onClosed = null;

        // -----------------------------------------------------
        // Close popup.
        // -----------------------------------------------------

        if (popupPanelRoot != null)
        {
            popupPanelRoot.SetActive(false);
        }

        // -----------------------------------------------------
        // Restore player controls.
        // -----------------------------------------------------

        if (CameraController.Instance != null)
        {
            CameraController.Instance.UnlockPlayerControls();

            Debug.Log(
                "[ArchiveConfirmationPopupUI] " +
                "Player controls UNLOCKED."
            );
        }

        // -----------------------------------------------------
        // Continue archive flow.
        // -----------------------------------------------------

        if (callback == null)
        {
            Debug.LogError(
                "[ArchiveConfirmationPopupUI] " +
                "Archive completion callback is NULL!"
            );

            return;
        }

        Debug.Log(
            "[ArchiveConfirmationPopupUI] " +
            "Invoking archive completion callback."
        );

        callback();
    }

    // =========================================================
    // HIDE
    // =========================================================

    private void Hide()
    {
        if (popupPanelRoot != null)
        {
            popupPanelRoot.SetActive(false);
        }
    }

    // =========================================================
    // SAFETY
    // =========================================================

    private void OnDisable()
    {
        // -----------------------------------------------------
        // Do NOT clear onClosed here.
        //
        // OnOkPressed() already saves and clears the callback
        // before disabling the popup.
        //
        // Clearing it here would destroy the callback before
        // OnOkPressed() can invoke it.
        // -----------------------------------------------------

        if (CameraController.Instance != null)
        {
            CameraController.Instance.UnlockPlayerControls();

            Debug.Log(
                "[ArchiveConfirmationPopupUI] " +
                "Popup disabled → Player controls UNLOCKED."
            );
        }
    }
}