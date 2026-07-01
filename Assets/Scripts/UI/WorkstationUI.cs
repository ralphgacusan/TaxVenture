using UnityEngine;

/// <summary>
/// PURPOSE:
/// Controls the generic "you are at a workstation" UI panel — currently just
/// the Close button, shown whenever the player is in first-person interaction
/// mode (desk, computer, folder, book, corkboard, cabinet).
///
/// RESPONSIBILITIES:
/// - Show/hide the workstation panel
/// - Forward the Close button's click to CameraController.ExitFirstPerson()
///
/// DOES NOT:
/// - Contain any workstation-specific UI (folder pages, computer fields, etc.)
///   Those will be their own separate UI panels layered on top in later
///   milestones (Milestone 5+). This script only owns the Close button.
///
/// CONNECTS WITH:
/// - CameraController: calls ExitFirstPerson() when Close is clicked
/// - Hooked up via the Close Button's OnClick() event in the Inspector
/// </summary>
public class WorkstationUI : MonoBehaviour
{
    [Tooltip("The panel GameObject to show/hide (contains the Close button).")]
    [SerializeField] private GameObject panelRoot;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>
    /// Wired to the Close Button's OnClick() event in the Inspector.
    /// </summary>
    public void OnCloseButtonPressed()
    {
        Debug.Log("Close pressed");
        CameraController.Instance.ExitFirstPerson();
    }
}