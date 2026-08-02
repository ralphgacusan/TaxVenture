using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Small placeholder popup shown when a placement is rejected. Does NOT
/// clear the current selection — player can immediately try a different
/// destination.
/// </summary>
public class ValueTransferValidationPopup : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI messageText;

    private void OnEnable()
    {
        ValueTransferManager.Instance.OnTransferFailed += HandleTransferFailed;
    }

    private void OnDisable()
    {
        if (ValueTransferManager.Instance != null)
            ValueTransferManager.Instance.OnTransferFailed -= HandleTransferFailed;
    }

    private void HandleTransferFailed(DataValue attempted, string reason)
    {
        messageText.text = reason;
        popupRoot.SetActive(true);
        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), 2f);
    }

    private void Hide() => popupRoot.SetActive(false);
}