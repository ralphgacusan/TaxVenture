using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Upper-left HUD text showing Level + current FSM state. Reuses the exact
/// same GameStateMachine.OnStateChanged event already used by
/// DebugStateLabel (Milestone 4) — this is functionally a second listener
/// of that same event, styled for the permanent HUD rather than a
/// development-only debug overlay. DebugStateLabel can remain as-is or be
/// removed later; this does not replace it, it's a second subscriber.
/// </summary>
public class HudStateLabel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private string levelLabel = "Level 1";

    private void Start()
    {
        levelText.text = levelLabel;

    }


}