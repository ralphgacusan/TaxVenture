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
                CameraHint = line.CameraHint
            });
        }
        lines = snapshot;
        LineIndex = 0;
        onCompleted = completed;
        onCancelled = cancelled;
        return true;
    }

    public bool TryAdvance(out bool completed)
    {
        completed = false;
        if (!IsActive) return false;
        if (LineIndex + 1 < lines.Count) LineIndex++;
        else completed = true;
        return true;
    }

    // Return the terminal callback after clearing ownership so UI can release
    // its controls before the callback starts another interaction.
    public bool TryEnd(DialogueEndReason reason, out Action callback)
    {
        callback = null;
        if (!IsActive) return false;
        if (reason == DialogueEndReason.Completed && LineIndex + 1 < lines.Count) return false;
        callback = reason == DialogueEndReason.Completed ? onCompleted : onCancelled;
        lines = null;
        LineIndex = 0;
        onCompleted = null;
        onCancelled = null;
        return true;
    }
}
