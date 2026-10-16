using System;
using System.Collections.Generic;

/// <summary>Display status of one task row.</summary>
public enum TaskRowStatus
{
    Pending,
    Active,
    Done
}

/// <summary>Read-only result of TaskProgress.Evaluate.</summary>
public sealed class TaskProgressSnapshot
{
    public TaskRowStatus[] Status;
    /// <summary>Index into the task list of the step to do next, or -1.</summary>
    public int ActiveIndex = -1;
    /// <summary>Index of the main-objective header that contains the active step, or -1.</summary>
    public int ActiveHeaderIndex = -1;
    public int DoneCount;
    public int StepCount;
    public bool AllDone => StepCount > 0 && DoneCount == StepCount;
}

/// <summary>
/// PURPOSE:
/// Pure presentation rule that turns the task list plus "is this step
/// done?" into per-row statuses. Shared by NotesPanelUI and the HUD
/// objective tracker so both always agree. Records nothing itself.
///
/// RULES:
/// - Done:    the step's trigger has happened.
/// - Active:  the first step, in list order, that is not done.
/// - Pending: every other step that is not done (a later step that
///            happened early still shows Done).
/// - Header (main objective): Done when every step under it is done,
///   Active when it contains the active step, otherwise Pending.
///   Headers are not counted as steps.
/// </summary>
public static class TaskProgress
{
    public static TaskProgressSnapshot Evaluate(IReadOnlyList<TaskDefinition> tasks, Func<TaskDefinition, bool> isDone)
    {
        int count = tasks != null ? tasks.Count : 0;
        var snapshot = new TaskProgressSnapshot { Status = new TaskRowStatus[count] };

        int header = -1;
        int headerStepCount = 0;
        int headerDoneCount = 0;

        for (int i = 0; i < count; i++)
        {
            TaskDefinition task = tasks[i];
            if (task == null) continue;

            if (task.IsHeader)
            {
                CloseHeader(snapshot, header, headerStepCount, headerDoneCount);
                header = i;
                headerStepCount = 0;
                headerDoneCount = 0;
                continue;
            }

            snapshot.StepCount++;
            headerStepCount++;
            if (isDone != null && isDone(task))
            {
                snapshot.Status[i] = TaskRowStatus.Done;
                snapshot.DoneCount++;
                headerDoneCount++;
            }
            else if (snapshot.ActiveIndex < 0)
            {
                snapshot.Status[i] = TaskRowStatus.Active;
                snapshot.ActiveIndex = i;
                snapshot.ActiveHeaderIndex = header;
            }
            else
            {
                snapshot.Status[i] = TaskRowStatus.Pending;
            }
        }

        CloseHeader(snapshot, header, headerStepCount, headerDoneCount);
        return snapshot;
    }

    private static void CloseHeader(TaskProgressSnapshot snapshot, int header, int steps, int done)
    {
        if (header < 0) return;
        if (steps > 0 && done == steps) snapshot.Status[header] = TaskRowStatus.Done;
        else if (snapshot.ActiveHeaderIndex == header) snapshot.Status[header] = TaskRowStatus.Active;
        else snapshot.Status[header] = TaskRowStatus.Pending;
    }
}
