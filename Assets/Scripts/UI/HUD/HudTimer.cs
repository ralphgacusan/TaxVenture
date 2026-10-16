using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Upper-center gameplay timer, HH:MM:SS, counting up from level start.
/// Placeholder implementation per spec — no save/persistence, no pause
/// integration yet (a future pass could hook this into
/// GameStateMachine.IsPaused if the timer should freeze during menus).
/// </summary>
public class HudTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    private float elapsedSeconds = 0f;
    private int shownSeconds = -1;

    /// <summary>Read-only elapsed time, used by the HUD objective tracker's clock.</summary>
    public float ElapsedSeconds => elapsedSeconds;

    private void Update()
    {
        elapsedSeconds += Time.deltaTime;

        // Only rebuild the text when the displayed second changes.
        int whole = Mathf.FloorToInt(elapsedSeconds);
        if (whole == shownSeconds) return;
        shownSeconds = whole;
        timerText.SetCharArray(clockBuffer, 0, HudTheme.FormatClock(clockBuffer, whole));
    }

    private readonly char[] clockBuffer = new char[10];
}