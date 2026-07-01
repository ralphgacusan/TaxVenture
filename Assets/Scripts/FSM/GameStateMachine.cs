using System;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Central authority driving the game's phase progression, per the design
/// document's gameplay loop. Holds the current IGameState and manages
/// transitions between states (Exit old -> Enter new).
///
/// RESPONSIBILITIES:
/// - Track and expose the current state
/// - Provide ChangeState() for states (or other scripts) to request a transition
/// - Call Tick() on the current state every frame
/// - Broadcast an event (OnStateChanged) whenever the state changes, so UI
///   (To-Do List, objective text, etc.) can react without being tightly
///   coupled to specific state classes
///
/// DOES NOT:
/// - Contain any gameplay logic itself — that lives inside each IGameState
///   implementation. This script is purely the "traffic controller."
///
/// CONNECTS WITH:
/// - Any IGameState implementation (ReceiveCaseState, ReviewDocumentsState, ...)
/// - Interactable scripts (e.g. DeskInteractable) call
///   GameStateMachine.Instance.ChangeState(new SomeState()) to progress
///   the game when the player completes a phase's required action.
/// - Future UI scripts subscribe to OnStateChanged to update on-screen text.
///
/// PATTERN NOTE:
/// Same lightweight static Instance approach as CameraController — scene-scoped,
/// not persisted across scenes (Phase 1 has only one scene/case).
/// </summary>
public class GameStateMachine : MonoBehaviour
{
    public static GameStateMachine Instance { get; private set; }

    /// <summary>
    /// Fired whenever the active state changes. Passes the new state so
    /// listeners (UI) can read its StateName or cast to a specific type if needed.
    /// </summary>
    public event Action<IGameState> OnStateChanged;

    public IGameState CurrentState { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // The game always begins at ReceiveCaseState, per the design document's
        // gameplay loop starting point. This is the ONLY place a state is
        // hardcoded — every subsequent transition is requested by the states
        // themselves or by interactables, never hardcoded here.
        ChangeState(new ReceiveCaseState());
    }

    private void Update()
    {
        CurrentState?.Tick();
    }

    /// <summary>
    /// Transitions from the current state to a new one, calling Exit() on the
    /// old state and Enter() on the new one. This is the ONLY way the active
    /// state should ever change.
    /// </summary>
    public void ChangeState(IGameState newState)
    {
        if (IsPaused)
        {
            Debug.LogWarning($"[GameStateMachine] Ignored ChangeState({newState?.StateName}) — machine is paused.");
            return;
        }

        if (newState == null)
        {
            Debug.LogWarning("[GameStateMachine] Attempted to change to a null state.");
            return;
        }

        Debug.Log($"[GameStateMachine] {CurrentState?.StateName ?? "None"} -> {newState.StateName}");

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();

        OnStateChanged?.Invoke(CurrentState);
    }

    /// <summary>
    /// True while the machine is paused (CurrentState is retained but not ticked,
    /// and ChangeState calls are ignored). Used when a modal UI, cutscene, or
    /// menu needs to temporarily halt phase logic without losing progress.
    /// </summary>
    public bool IsPaused { get; private set; }

    private void Update()
    {
        if (IsPaused) return;
        CurrentState?.Tick();
    }

    /// <summary>
    /// Pauses the FSM. CurrentState is untouched — Exit() is NOT called — so
    /// resuming picks up exactly where it left off. Use this for things like
    /// "player opened a pause menu mid-interview" rather than switching states.
    /// </summary>
    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Debug.Log($"[GameStateMachine] Paused during '{CurrentState?.StateName}'.");
    }

    /// <summary>
    /// Resumes ticking the current state from where it was paused.
    /// </summary>
    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Debug.Log($"[GameStateMachine] Resumed '{CurrentState?.StateName}'.");
    }
}