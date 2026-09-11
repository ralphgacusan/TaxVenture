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

        // Lock player while confirmation popup is open.
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
        // Close popup first.
        // -----------------------------------------------------

        if (popupPanelRoot != null)
        {
            popupPanelRoot.SetActive(false);
        }

        // -----------------------------------------------------
        // IMPORTANT:
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

        System.Action callback = onClosed;

        onClosed = null;

        callback?.Invoke();
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
        // Safety net:
        // If this popup is disabled while open, make sure the
        // player does not remain permanently locked.
        // -----------------------------------------------------

        if (CameraController.Instance != null)
        {
            CameraController.Instance.UnlockPlayerControls();

            Debug.Log(
                "[ArchiveConfirmationPopupUI] " +
                "Popup disabled → Player controls UNLOCKED."
            );
        }

        onClosed = null;
    }
}
