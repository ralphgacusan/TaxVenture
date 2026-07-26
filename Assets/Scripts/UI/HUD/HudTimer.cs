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

    private void Update()
    {
        elapsedSeconds += Time.deltaTime;
        var ts = System.TimeSpan.FromSeconds(elapsedSeconds);
        timerText.text = $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}