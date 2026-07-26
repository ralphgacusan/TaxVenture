using System;

/// <summary>
/// PURPOSE:
/// Maps one IGameState type to a player-facing TASK description (not the
/// raw FSM StateName). E.g. ResearchTaxState -> "Research the applicable
/// tax laws using the Tax Code Book" instead of just "Research Tax Code".
/// Order in the list defines display/completion order in the Notes panel.
/// </summary>
public class TaskDefinition
{
    public Type StateType;
    public string TaskDescription;

    public TaskDefinition(Type stateType, string taskDescription)
    {
        StateType = stateType;
        TaskDescription = taskDescription;
    }
}