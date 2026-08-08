using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// "CASE COMPLETE / Continue" popup, shown when CaseProgressionManager
/// determines another case remains in the level. Pressing Continue
/// triggers loading the next case WITHOUT a scene reload — CaseManager
/// swaps CurrentCase/CurrentDefinition in place, and the FSM restarts at
/// ReceiveCaseState, which every subscribed UI system (Folder, Documents,
/// NPCs, Notes) reacts to exactly as it already does on any normal state
/// change.
/// </summary>
public class CaseCompletionUI : MonoBehaviour
{
    public static CaseCompletionUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI headingText;
    [SerializeField] private TextMeshProUGUI progressText; // e.g. "Case 1 of 3"

    private System.Action onContinuePressed;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void ShowContinuePrompt(System.Action onContinue)
    {
        onContinuePressed = onContinue;
        headingText.text = "CASE COMPLETE";
        progressText.text = $"Case {CaseProgressionManager.Instance.CurrentCaseNumber} of {CaseProgressionManager.Instance.TotalCasesInLevel} complete.";
        panelRoot.SetActive(true);
    }

    public void OnContinuePressed()
    {
        panelRoot.SetActive(false);
        onContinuePressed?.Invoke();
    }

    private void Hide() => panelRoot.SetActive(false);
}