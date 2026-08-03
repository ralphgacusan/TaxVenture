using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Level Result popup shown after the Client outcome conversation
/// concludes. Stub version for R12 — shows which of the four outcome
/// branches occurred. R13 will expand this with full EXP/Reputation
/// scoring per its redesigned reward rules.
/// </summary>
public class LevelResultPopupUI : MonoBehaviour
{
    public static LevelResultPopupUI Instance { get; private set; }

    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI resultText;

    private System.Action onClosed;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show(ClientOutcomeBranch branch, System.Action onClosedCallback)
    {
        CameraController.Instance?.LockPlayerControls();

        onClosed = onClosedCallback;

        resultText.text = branch switch
        {
            ClientOutcomeBranch.CorrectNoIssues => "Case Closed: Correct Verdict, No Issues",
            ClientOutcomeBranch.CorrectWithIssues => "Case Closed: Correct Verdict, Minor Issues Found",
            ClientOutcomeBranch.WrongVerdict => "Case Closed: Incorrect Verdict",
            ClientOutcomeBranch.WrongVerdictWithIssues => "Case Closed: Incorrect Verdict, Multiple Issues Found",
            _ => "Case Closed"
        };

        popupRoot.SetActive(true);
    }

    public void OnClosePressed()
    {
        popupRoot.SetActive(false);

        CameraController.Instance?.UnlockPlayerControls();

        onClosed?.Invoke();
    }

    private void Hide()
    {
        popupRoot.SetActive(false);
        CameraController.Instance?.UnlockPlayerControls();
    }
}