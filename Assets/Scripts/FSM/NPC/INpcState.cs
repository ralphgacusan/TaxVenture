/// <summary>
/// PURPOSE:
/// Contract for one behavioral state of an NPC (Idle, Waiting, Interact,
/// Dialogue, Completed), per the design brief's NPC FSM requirement. Mirrors
/// IGameState exactly, but scoped to a single NPC instance rather than the
/// whole game.
///
/// IMPLEMENTED BY:
/// - NpcIdleState, NpcWaitingState, NpcInteractState, NpcDialogueState,
///   NpcCompletedState (this milestone)
/// - Future NPC-specific states as new NPC types are added (Auditor, etc.)
/// </summary>
public interface INpcState
{
    void Enter();
    void Exit();
    void Tick();
    string StateName { get; }
}