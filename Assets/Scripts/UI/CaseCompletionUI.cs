
using UnityEngine;
using TMPro;

/// <summary>
/// Displays the Case Complete popup when another case remains.
///
/// The Continue button calls OnContinuePressed(), which invokes
/// the callback supplied by CaseProgressionManager.
/// </summary>
public class CaseCompletionUI : MonoBehaviour
{
    public static CaseCompletionUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI headingText;
    [SerializeField] private TextMeshProUGUI progressText;

    private System.Action onContinuePressed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[CaseCompletionUI] Duplicate instance detected. Destroying duplicate."
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        Debug.Log("[CaseCompletionUI] Awake.");

        Hide();
    }

    public void ShowContinuePrompt(System.Action onContinue)
    {
        Debug.Log(
            "[CaseCompletionUI] ShowContinuePrompt() CALLED."
        );

        if (panelRoot == null)
        {
            Debug.LogError(
                "[CaseCompletionUI] panelRoot is NOT assigned!"
            );

            return;
        }

        if (onContinue == null)
        {
            Debug.LogError(
                "[CaseCompletionUI] Continue callback is NULL!"
            );

            return;
        }

        onContinuePressed = onContinue;

        if (headingText != null)
        {
            headingText.text = "CASE COMPLETE";
        }

        if (progressText != null)
        {
            if (CaseProgressionManager.Instance != null)
            {
                progressText.text =
                    $"Case {CaseProgressionManager.Instance.CurrentCaseNumber} " +
                    $"of {CaseProgressionManager.Instance.TotalCasesInLevel} complete.";
            }
        }

        panelRoot.SetActive(true);

        Debug.Log(
            "[CaseCompletionUI] Case completion panel shown."
        );
    }

    public void OnContinuePressed()
    {
        Debug.Log(
            "[CaseCompletionUI] >>> CONTINUE BUTTON PRESSED <<<"
        );

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        System.Action callback = onContinuePressed;

        // Clear first to prevent accidental double invocation.
        onContinuePressed = null;

        if (callback == null)
        {
            Debug.LogError(
                "[CaseCompletionUI] Continue callback is NULL!"
            );

            return;
        }

        Debug.Log(
            "[CaseCompletionUI] Invoking next-case callback."
        );

        callback();
    }

    private void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        onContinuePressed = null;
    }
}
