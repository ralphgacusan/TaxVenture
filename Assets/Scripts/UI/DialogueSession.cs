using System;
using System.Collections.Generic;

public enum DialogueEndReason { Completed, Cancelled }

/// <summary>Conversation admission and sequencing, independent of UI and Unity callbacks.</summary>
public sealed class DialogueSession
{
    private List<DialogueLine> lines;
    private Action onCompleted;
    private Action onCancelled;

    public bool IsActive => lines != null;
    public int LineIndex { get; private set; }
    public DialogueLine CurrentLine => IsActive ? lines[LineIndex] : null;

    // T08: the current line's awaited action has been completed.
    private bool actionDone;

    /// <summary>True while the current line waits for its action (advancing and completing are refused).</summary>
    public bool IsAwaitingAction => IsActive && lines[LineIndex].AwaitAction != null && !actionDone;

    /// <summary>The action the current line waits for, or null.</summary>
    public string AwaitingActionId => IsAwaitingAction ? lines[LineIndex].AwaitAction : null;

    /// <summary>Completes the current line's awaited action once (wrong id, no wait or done already: false).</summary>
    public bool TryCompleteAction(string actionId)
    {
        if (!IsAwaitingAction || actionId == null || actionId.Trim() != lines[LineIndex].AwaitAction) return false;
        actionDone = true;
        return true;
    }

    public bool TryStart(IReadOnlyList<DialogueLine> source, Action completed, Action cancelled = null)
    {
        if (IsActive || source == null || source.Count == 0) return false;
        for (int i = 0; i < source.Count; i++)
            if (source[i] == null || string.IsNullOrWhiteSpace(source[i].Text)) return false;

        var snapshot = new List<DialogueLine>(source.Count);
        for (int i = 0; i < source.Count; i++)
        {
            DialogueLine line = source[i];
            snapshot.Add(new DialogueLine(line.Speaker, line.Text, line.PortraitId, line.SpeakerName)
            {
                CameraHint = line.CameraHint,
                Emote = line.Emote,
                EmoteBurst = line.EmoteBurst,
                AwaitAction = string.IsNullOrWhiteSpace(line.AwaitAction) ? null : line.AwaitAction.Trim(),
                ActionPrompt = line.ActionPrompt
            });
        }
        lines = snapshot;
        LineIndex = 0;
        actionDone = false;
        onCompleted = completed;
        onCancelled = cancelled;
        return true;
    }

    /// <summary>
    /// Inserts lines right after the current one (a conversation that goes on
    /// with what just happened, e.g. the Auditor's findings once he has the
    /// folder). Same validation as TryStart. Returns the index of the first
    /// inserted line, or -1 when nothing was inserted.
    /// </summary>
    public int TryInsertAfterCurrent(IReadOnlyList<DialogueLine> source)
    {
        if (!IsActive || source == null || source.Count == 0) return -1;
        for (int i = 0; i < source.Count; i++)
            if (source[i] == null || string.IsNullOrWhiteSpace(source[i].Text)) return -1;
        int at = LineIndex + 1;
        for (int i = 0; i < source.Count; i++)
        {
            DialogueLine line = source[i];
            lines.Insert(at + i, new DialogueLine(line.Speaker, line.Text, line.PortraitId, line.SpeakerName)
            {
                CameraHint = line.CameraHint,
                Emote = line.Emote,
                EmoteBurst = line.EmoteBurst,
                AwaitAction = string.IsNullOrWhiteSpace(line.AwaitAction) ? null : line.AwaitAction.Trim(),
                ActionPrompt = line.ActionPrompt
            });
        }
        return at;
    }

    public bool TryAdvance(out bool completed)
    {
        completed = false;
        if (!IsActive || IsAwaitingAction) return false;
        if (LineIndex + 1 < lines.Count)
        {
            LineIndex++;
            actionDone = false;
        }
        else completed = true;
        return true;
    }

    // Return the terminal callback after clearing ownership so UI can release
    // its controls before the callback starts another interaction.
    public bool TryEnd(DialogueEndReason reason, out Action callback)
    {
        callback = null;
        if (!IsActive) return false;
        if (reason == DialogueEndReason.Completed && (LineIndex + 1 < lines.Count || IsAwaitingAction)) return false;
        callback = reason == DialogueEndReason.Completed ? onCompleted : onCancelled;
        lines = null;
        LineIndex = 0;
        actionDone = false;
        onCompleted = null;
        onCancelled = null;
        return true;
    }
}
