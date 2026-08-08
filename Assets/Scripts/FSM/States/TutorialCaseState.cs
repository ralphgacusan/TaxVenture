using UnityEngine;

/// <summary>
/// PURPOSE:
/// Thin GameStateMachine state for Level 1's tutorial case. Its only job is
/// to hand off to TutorialController when entered — it does NOT contain any
/// step logic, popup logic, or gameplay-waiting logic itself. That keeps the
/// FSM from growing a state per tutorial popup.
///
/// Implements IGameState directly, matching the project's existing state
/// contract (StateName, Enter, Exit, Tick).
/// </summary>
public class TutorialCaseState : IGameState
{
    public string StateName => "Tutorial Case";

    public void Enter()
    {
        Debug.Log("[TutorialCaseState] Entered — starting tutorial case.");
        TutorialController.Instance.BeginTutorial();
    }

    public void Exit()
    {
        Debug.Log("[TutorialCaseState] Exited.");
    }

    public void Tick()
    {
        // Intentionally empty — TutorialController drives everything
        // event-/callback-driven, no per-frame logic needed here.
    }
}