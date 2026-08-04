using UnityEngine;

/// <summary>
/// PURPOSE:
/// Tracks elapsed time for the current case, from ReceiveCaseState to
/// AuditSubmittedState (the point where the FSM locks). Subscribes to
/// GameStateMachine.OnStateChanged rather than being manually
/// started/stopped by any single interactable — consistent with how
/// RewardsUI/CaseCompleteUI already react to state changes automatically.
/// </summary>
public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance { get; private set; }

    private float startTime = -1f;
    private float endTime = -1f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (GameStateMachine.Instance != null)
        {
            GameStateMachine.Instance.OnStateChanged += HandleStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (GameStateMachine.Instance != null)
        {
            GameStateMachine.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(IGameState newState)
    {
        if (newState is ReceiveCaseState && startTime < 0f)
        {
            startTime = Time.time;
        }
        else if (newState is AuditSubmittedState && endTime < 0f)
        {
            endTime = Time.time;
        }
    }

    public float GetElapsedSeconds()
    {
        if (startTime < 0f) return 0f;
        float end = endTime >= 0f ? endTime : Time.time;
        return end - startTime;
    }

    public string GetFormattedTime()
    {
        var ts = System.TimeSpan.FromSeconds(GetElapsedSeconds());
        return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}