using UnityEngine;
using TMPro;

/// <summary>
/// PURPOSE:
/// Simple debug overlay that displays the current GameState's name on
/// screen. Not part of the final game UI — purely a development aid so we
/// can visually verify FSM transitions during testing without relying on
/// the Console.
///
/// RESPONSIBILITIES:
/// - Subscribe to GameStateMachine.OnStateChanged
/// - Update its text to match the current state's StateName
///
/// CONNECTS WITH:
/// - GameStateMachine: subscribes to OnStateChanged event
///
/// REMOVAL NOTE:
/// Safe to delete this script's GameObject once a real To-Do List / objective
/// UI exists and displays the same information more appropriately.
/// </summary>
public class DebugStateLabel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    private void OnEnable()
    {
        // GameStateMachine.Instance may not exist yet if this runs before
        // GameStateMachine.Awake(). Script execution order isn't guaranteed,
        // so we defer subscription to Start() instead, by which point all
        // Awake() calls in the scene have run.
    }

    private void Start()
    {
        if (GameStateMachine.Instance != null)
        {
            GameStateMachine.Instance.OnStateChanged += HandleStateChanged;

            // Show the current state immediately, in case it changed before
            // this listener subscribed.
            if (GameStateMachine.Instance.CurrentState != null)
            {
                HandleStateChanged(GameStateMachine.Instance.CurrentState);
            }
        }
        else
        {
            Debug.LogWarning("[DebugStateLabel] No GameStateMachine found in scene.");
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
        UpdateLabel();
    }

    private void Update()
    {
        // Cheap polling is fine here — this is a debug-only script.
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (label == null || GameStateMachine.Instance == null) return;

        string suffix = GameStateMachine.Instance.IsPaused ? " (Idle/Paused)" : "";
        label.text = $"State: {GameStateMachine.Instance.CurrentState?.StateName}{suffix}";
    }
}