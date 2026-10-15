using System;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Per-NPC state machine, mirroring GameStateMachine's pattern exactly but
/// WITHOUT a static Instance — each NPC GameObject has its own instance of
/// this component, since multiple NPCs (Client, Auditor, future NPCs) exist
/// simultaneously and each needs independent state.
///
/// RESPONSIBILITIES:
/// - Track and expose this NPC's current state
/// - Provide ChangeState() for transitions
/// - Call Tick() on the current state every frame
/// - Broadcast OnStateChanged for anything that wants to react (e.g. this
///   NPC's own visual indicator, or a future UI showing NPC status)
///
/// CONNECTS WITH:
/// - Attached to the same GameObject as the NPC's *Interactable script
///   (e.g. ClientInteractable references GetComponent<NpcStateMachine>())
/// - NpcStatusMarker: subscribes to OnStateChanged to refresh the overhead
///   conversation availability indicator
/// </summary>
[RequireComponent(typeof(NpcAnimationDriver))]
public class NpcStateMachine : MonoBehaviour
{
    public event Action<INpcState> OnStateChanged;

    public INpcState CurrentState { get; private set; }

    private void Awake()
    {
        // Existing scene instances predate the animation adapter. Add it at
        // runtime as well as requiring it for newly configured NPCs.
        if (GetComponent<NpcAnimationDriver>() == null)
            gameObject.AddComponent<NpcAnimationDriver>();
    }

    private void Start()
    {
        // Every NPC begins Idle, matching the design brief's NPC FSM list order.
        ChangeState(new NpcIdleState());
    }

    private void Update()
    {
        CurrentState?.Tick();
    }

    public void ChangeState(INpcState newState)
    {
        if (newState == null)
        {
            Debug.LogWarning($"[NpcStateMachine:{gameObject.name}] Attempted to change to a null state.");
            return;
        }

        Debug.Log($"[NpcStateMachine:{gameObject.name}] {CurrentState?.StateName ?? "None"} -> {newState.StateName}");

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();

        OnStateChanged?.Invoke(CurrentState);
    }
}
