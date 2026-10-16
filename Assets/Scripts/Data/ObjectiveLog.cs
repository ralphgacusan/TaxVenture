using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Pure record of which objective triggers have happened (no Unity calls),
/// including "undoneBy" handling. OfficeObjectives feeds it from game events;
/// tests drive it directly.
///
/// UNDO RULE:
/// When a trigger arrives, every step whose UndoneByKey equals it loses its
/// own trigger again (it becomes unchecked). The undo trigger itself is not
/// kept, so the same undo can happen again later (fail -> resubmit -> fail).
/// </summary>
public sealed class ObjectiveLog
{
    private readonly HashSet<string> reached = new HashSet<string>(StringComparer.Ordinal);

    public bool Contains(string key) => key != null && reached.Contains(key);

    public bool IsDone(TaskDefinition task) =>
        task != null && task.TriggerKey != null && reached.Contains(task.TriggerKey);

    /// <summary>Records a trigger; returns true when any step's state may have changed.</summary>
    public bool Record(string key, IReadOnlyList<TaskDefinition> tasks)
    {
        if (string.IsNullOrEmpty(key)) return false;

        bool isUndo = false;
        bool changed = false;
        if (tasks != null)
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                TaskDefinition task = tasks[i];
                if (task == null || task.UndoneByKey != key) continue;
                isUndo = true;
                if (task.TriggerKey != null && reached.Remove(task.TriggerKey)) changed = true;
            }
        }

        // An undo trigger is an occurrence, not a lasting fact: it is never
        // stored, so the same undo can happen again later.
        if (!isUndo && reached.Add(key)) changed = true;

        return changed;
    }
}
