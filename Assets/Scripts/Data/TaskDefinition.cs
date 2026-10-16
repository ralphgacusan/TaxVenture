using System;

/// <summary>
/// PURPOSE:
/// Maps one IGameState type to a player-facing TASK description (not the
/// raw FSM StateName). E.g. ResearchTaxState -> "Research the applicable
/// tax laws using the Tax Code Book" instead of just "Research Tax Code".
/// Order in the list defines display/completion order in the Notes panel.
///
/// IsHeader marks a main-objective title row (e.g. "Finish Side Quest 1").
/// Header rows group the steps under them and are not counted as steps.
///
/// TriggerKey says what completes the step (see ObjectiveTriggers):
///   "state:ResearchTaxState"     the game entered that FSM state
///   "event:SideQuestCompleted"   a gameplay event happened
///   "area:Environment/Office"    the player stood inside that object's bounds
/// StateType is set for "state:" triggers (kept for NotesPanelUI's legacy rule).
///
/// ProgressSource (optional) names a live counter shown next to the step,
/// e.g. "profiles" -> "2 / 4".
/// </summary>
public class TaskDefinition
{
    public Type StateType;
    public string TaskDescription;
    public bool IsHeader;
    public string TriggerKey;
    public string ProgressSource;
    /// <summary>Optional trigger that unchecks this step again (e.g. event:CaseFolderReturned).</summary>
    public string UndoneByKey;

    public TaskDefinition(Type stateType, string taskDescription)
        : this(stateType, taskDescription, false)
    {
    }

    public TaskDefinition(Type stateType, string taskDescription, bool isHeader)
    {
        StateType = stateType;
        TaskDescription = taskDescription;
        IsHeader = isHeader;
        TriggerKey = stateType != null ? ObjectiveTriggers.StatePrefix + stateType.Name : null;
    }

    /// <summary>Trigger-based step (or header when isHeader is true; headers need no trigger).</summary>
    public TaskDefinition(string triggerKey, string taskDescription, bool isHeader, string progressSource)
    {
        TriggerKey = triggerKey;
        TaskDescription = taskDescription;
        IsHeader = isHeader;
        ProgressSource = progressSource;
        StateType = ObjectiveTriggers.TryGetState(triggerKey, out Type state) ? state : null;
    }
}
