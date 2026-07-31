using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Controls the workstation exit control — now a single bottom-center
/// placeholder button reading "Exit Desk" / "Exit Corkboard" (dynamic per
/// workstation), replacing the old top-right "Close" button entirely, per
/// R2 redesign. Still purely a "show/hide + forward click to
/// CameraController.ExitFirstPerson()" responsibility — no gameplay logic.
///
/// CONNECTS WITH:
/// - CameraController: calls Show(label) on EnterFirstPerson, Hide() on exit;
///   ExitFirstPerson() is called when the exit button is clicked
/// </summary>
public class WorkstationUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI exitButtonLabel;

    private void Awake()
    {
        Hide();
    }

    /// <summary>
    /// Shows the bottom-center exit control with workstation-specific text,
    /// e.g. "Exit Desk" or "Exit Corkboard".
    /// </summary>
    public void Show(string exitLabel)
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        if (exitButtonLabel != null) exitButtonLabel.text = exitLabel;
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>Wired to the bottom-center Exit button's OnClick().</summary>
    public void OnExitButtonPressed()
    {
        CameraController.Instance.ExitFirstPerson();
    }
}