using UnityEngine;
using TMPro;

/// <summary>
/// Optional rejection text. A single unscaled deadline replaces earlier messages.
/// Does not change selection or lock input; local badges own the drop animation.
/// </summary>
public class ValueTransferValidationPopup : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI messageText;

    private ValueTransferManager subscribedManager;
    private CanvasGroup visibility;
    private bool showing;
    private float hideAt;

    private void Awake()
    {
        if (popupRoot == null)
            return;
        visibility = popupRoot.GetComponent<CanvasGroup>();
        if (visibility == null)
            visibility = popupRoot.AddComponent<CanvasGroup>();
        visibility.blocksRaycasts = false;
        visibility.interactable = false;
        Hide();
    }

    private void OnEnable() => TrySubscribe();
    private void Start() => TrySubscribe();

    private void TrySubscribe()
    {
        if (subscribedManager != null || ValueTransferManager.Instance == null)
            return;
        subscribedManager = ValueTransferManager.Instance;
        subscribedManager.OnTransferFailed += HandleTransferFailed;
        subscribedManager.OnTransferSucceeded += HandleTransferSucceeded;
        subscribedManager.OnSelectionChanged += HandleSelectionChanged;
    }

    private void OnDisable()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OnTransferFailed -= HandleTransferFailed;
            subscribedManager.OnTransferSucceeded -= HandleTransferSucceeded;
            subscribedManager.OnSelectionChanged -= HandleSelectionChanged;
        }
        subscribedManager = null;
        Hide();
    }

    private void HandleTransferFailed(DataValue attempted, string reason)
    {
        if (visibility == null || messageText == null)
            return;
        // Manager reasons describe the incoming type, not the destination's rule.
        // Keep this text accurate without duplicating validation here.
        messageText.text = "This value could not be placed here. Try another value or field.";
        popupRoot.SetActive(true);
        visibility.alpha = 1f;
        showing = true;
        hideAt = Time.unscaledTime + 2f;
    }

    private void HandleTransferSucceeded(DataValue value) => Hide();
    private void HandleSelectionChanged(object source) => Hide();

    private void Update()
    {
        if (showing && Time.unscaledTime >= hideAt)
            Hide();
    }

    private void Hide()
    {
        showing = false;
        // The scene puts this listener on popupRoot itself. Hiding by alpha
        // keeps its subscriptions alive for the next failure.
        if (visibility != null)
            visibility.alpha = 0f;
    }
}
