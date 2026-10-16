using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// A small, allocation-free finite state machine over an enum, for the
/// backend flows that are simple phases rather than whole gameplay states
/// (the level's case flow, a door gate, a save in progress...). The big
/// gameplay loop keeps using GameStateMachine + IGameState.
///
/// - Allow(from, to) lists the legal transitions. A machine with no rules
///   allows every change; once any rule is added, anything not listed is
///   refused (logged, state unchanged) so a bug can't push a flow into an
///   impossible phase.
/// - TryChange returns false instead of throwing.
/// - A change requested from inside a Changed listener is queued and runs
///   right after the current one finishes, so listeners always see the
///   transitions in order and CurrentState always matches the last event.
/// - No Unity calls (unit-testable); a Log hook can be set for warnings.
/// </summary>
public sealed class EnumStateMachine<TState> where TState : struct, Enum
{
    private readonly Dictionary<TState, HashSet<TState>> allowed = new Dictionary<TState, HashSet<TState>>();
    private readonly Queue<TState> pending = new Queue<TState>();
    private readonly string name;
    private bool changing;

    /// <summary>(from, to) after every completed transition.</summary>
    public event Action<TState, TState> Changed;

    /// <summary>Where refused transitions are reported (UnityEngine.Debug.LogWarning in the game).</summary>
    public Action<string> Log;

    public TState State { get; private set; }

    /// <summary>The state before the last transition.</summary>
    public TState Previous { get; private set; }

    public EnumStateMachine(string name, TState initial)
    {
        this.name = string.IsNullOrEmpty(name) ? typeof(TState).Name : name;
        State = initial;
        Previous = initial;
    }

    public bool Is(TState state) => EqualityComparer<TState>.Default.Equals(State, state);

    /// <summary>Declares legal transitions from one state.</summary>
    public EnumStateMachine<TState> Allow(TState from, params TState[] to)
    {
        if (!allowed.TryGetValue(from, out HashSet<TState> set))
        {
            set = new HashSet<TState>();
            allowed[from] = set;
        }
        if (to != null)
            foreach (TState target in to) set.Add(target);
        return this;
    }

    /// <summary>True when the rules allow going from the current state to this one.</summary>
    public bool CanChange(TState to) => CanChange(State, to);

    private bool CanChange(TState from, TState to)
    {
        if (allowed.Count == 0) return true;
        return allowed.TryGetValue(from, out HashSet<TState> set) && set.Contains(to);
    }

    /// <summary>
    /// Moves to a new state when allowed. Changing to the current state is a
    /// no-op that returns true. Requests made during a transition are queued.
    /// </summary>
    public bool TryChange(TState to)
    {
        if (changing)
        {
            pending.Enqueue(to);
            return true;
        }

        if (!Apply(to)) return false;
        while (pending.Count > 0) Apply(pending.Dequeue());
        return true;
    }

    /// <summary>Puts the machine in a state without rules or events (level restart).</summary>
    public void Reset(TState state)
    {
        pending.Clear();
        Previous = state;
        State = state;
    }

    private bool Apply(TState to)
    {
        if (Is(to)) return true;
        if (!CanChange(State, to))
        {
            Log?.Invoke($"[{name}] Refused transition {State} -> {to}.");
            return false;
        }

        changing = true;
        try
        {
            Previous = State;
            State = to;
            Changed?.Invoke(Previous, State);
        }
        finally
        {
            changing = false;
        }
        return true;
    }
}
