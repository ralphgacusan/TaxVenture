using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Placeholder Notes panel — shows a fixed list of "Today's Tasks" text,
/// per spec. Not yet wired to real objective/progress tracking; this is
/// intentionally the simplest possible static panel, matching the
/// "no polish yet" scope of this pass.
///
/// RESPONSIBILITIES:
/// - Show/hide the panel
/// - Display a fixed placeholder objectives list
///
/// CONNECTS WITH:
/// - HudIconButton (Notes icon): Button.OnClick wired directly to Show()
///   once unlocked via GameplayEvents.NotesUnlockRequested (Phase 1)
///
/// FUTURE:
/// A later pass can replace the hardcoded list with entries that update
/// based on GameStateMachine.OnStateChanged (e.g. checking off completed
/// phases), without needing to change how this panel is opened/closed.
/// </summary>
public class NotesPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI objectivesText;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        objectivesText.text =
            "Today's Tasks\n\n" +
            "• Receive Case\n" +
            "• Review Documents\n" +
            "• Interview Client\n" +
            "• Research Tax Code\n" +
            "• Prepare Return";

        panelRoot.SetActive(true);
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }
}