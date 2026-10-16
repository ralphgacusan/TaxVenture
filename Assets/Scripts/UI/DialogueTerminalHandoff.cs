using System;

/// <summary>
/// Optional conversation presentation (T03 cinematic shots and the
/// conversation-only Elijah visual). DialogueUI drives it with explicit calls
/// in a fixed order: BeginConversation, ShowLine for every line, then exactly
/// one of BeginReturn (natural end or cancellation) or AbortConversation
/// (teardown). Pure interface so contract tests can run outside Unity.
/// </summary>
public interface IDialoguePresentation
{
    /// <summary>Called after admission and control capture, before the first line.</summary>
    void BeginConversation(object owner);

    /// <summary>Called when a line becomes current, including the first one.</summary>
    void ShowLine(DialogueLine line, int index);

    /// <summary>
    /// Called after the session's terminal claim and BEFORE controls are released
    /// or any completion/cancellation callback runs. Return true to hold those
    /// until <paramref name="onReturned"/> is invoked (once); return false when
    /// nothing needs to be waited for.
    /// </summary>
    bool BeginReturn(DialogueEndReason reason, Action onReturned);

    /// <summary>Immediate cleanup for teardown. Must not invoke callbacks.</summary>
    void AbortConversation();
}

/// <summary>
/// Holds a finished conversation's control release and terminal callbacks
/// until its presentation has returned to first person, then runs them once.
/// </summary>
public sealed class DialogueTerminalHandoff
{
    private Action pending;
    private int pendingVersion;

    public bool IsPending => pending != null;
    public int PendingVersion => pendingVersion;

    /// <summary>Arms the handoff. Rejected while another one is still pending.</summary>
    public bool Begin(int version, Action complete)
    {
        if (pending != null || complete == null) return false;
        pending = complete;
        pendingVersion = version;
        return true;
    }

    /// <summary>
    /// Completes the handoff for <paramref name="version"/> only. Stale or repeated
    /// completions are ignored. State is cleared before the action runs so the
    /// action may start the next conversation.
    /// </summary>
    public bool Complete(int version)
    {
        if (pending == null || version != pendingVersion) return false;
        Action action = pending;
        pending = null;
        action();
        return true;
    }

    /// <summary>Teardown path: completes whatever is pending without a blend.</summary>
    public bool ForceComplete()
    {
        if (pending == null) return false;
        Action action = pending;
        pending = null;
        action();
        return true;
    }
}
