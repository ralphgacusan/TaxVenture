using UnityEngine;

/// <summary>
/// PURPOSE:
/// Minimal read-only confirmation popup shown after successfully archiving
/// a case. One message, one OK button — same "smallest possible UI"
/// philosophy as FindingsPopupUI (Milestone 11) and AuditSummaryPopupUI.
/// </summary>
public class ArchiveConfirmationPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject popupPanelRoot;
    [SerializeField] private CaseCompleteRewardsUI rewardsUI;
    private System.Action onClosed;

    private void Awake()
    {
        Hide();
    }

    public void Show(System.Action onClosedCallback)
    {
        onClosed = onClosedCallback;
        popupPanelRoot.SetActive(true);
        CameraController.Instance.LockPlayerControls(); // NEW

    }

    public void OnOkPressed()
    {
        popupPanelRoot.SetActive(false);
        onClosed?.Invoke();
        rewardsUI.Show();
    }

    private void Hide()
    {
        popupPanelRoot.SetActive(false);
    }
}