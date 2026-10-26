using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Provides the player-facing task lists.
///
/// 1) GetTasks(): the original case task list keyed by FSM state, used by
///    NotesPanelUI in scenes without the Office HUD (unchanged behaviour).
///
/// 2) LoadObjectives(levelId): the editable, per-level objectives file
///    Assets/Resources/Levels/&lt;levelId&gt;/objectives.json — same
///    "data lives in Resources JSON" pattern as level.json and the tutorial
///    JSON. Main objectives each contain ordered steps; every step names
///    the trigger that completes it (see ObjectiveTriggers).
///
/// OBJECTIVES JSON:
/// { "objectives": [
///     { "title": "Finish Side Quest 1",
///       "steps": [ { "text": "Talk to Arthur in the plaza.",
///                    "trigger": "event:TutorialGuideTalkedTo",
///                    "progress": "" } ] } ] }
///
/// FALLBACK:
/// A missing/invalid file logs a warning and uses the built-in copy of the
/// same Level 1 objectives. Invalid steps are skipped with a warning.
///
/// CONNECTS WITH:
/// - NotesPanelUI (legacy list, or the paper view in Office_Level1)
/// - OfficeObjectives / HudObjectiveTracker (objectives)
/// </summary>
public static class TaskListProvider
{
    public const string Level1Id = "Level_01";

    /// <summary>Reception floor and doors: "Go to the office" counts as soon as the player is inside.</summary>
    public const string ReceptionArea =
        "Environment/OfficeBuilding/Rooms/Reception/Floor|Environment/OfficeBuilding/Rooms/Reception/Doors";

    // Filled by JsonUtility at runtime, so the compiler sees no assignments.
#pragma warning disable 0649
    [Serializable]
    private class ObjectivesFile
    {
        public ObjectiveEntry[] objectives;
    }

    [Serializable]
    private class ObjectiveEntry
    {
        public string title;
        public StepEntry[] steps;
    }

    [Serializable]
    private class StepEntry
    {
        public string text;
        public string trigger;
        public string progress;
        public string undoneBy;
    }
#pragma warning restore 0649

    // =========================================================
    // LEGACY CASE TASK LIST (unchanged)
    // =========================================================

    public static List<TaskDefinition> GetTasks()
    {
        return new List<TaskDefinition>
        {
            new TaskDefinition(typeof(CaseOutcomeState), "Main Objective: Finish Tutorial and Unguided Case", true),

            new TaskDefinition(typeof(ReceiveCaseState), "Meet with the receptionist and receive today's case."),
            new TaskDefinition(typeof(ReviewDocumentsState), "Review the case folder and supporting documents."),
            new TaskDefinition(typeof(InterviewClientState), "Interview the client to verify missing information."),
            new TaskDefinition(typeof(ResearchTaxState), "Research the applicable tax laws using the Tax Code Book."),
            new TaskDefinition(typeof(ComputeTaxesState), "Compute the client's tax liability using the Computer."),
            new TaskDefinition(typeof(AnalyzeEvidenceState), "Analyze the evidence at the corkboard and determine the case assessment."),
            new TaskDefinition(typeof(StampAssessmentState), "Stamp the case with its final assessment."),
            new TaskDefinition(typeof(PrepareReturnState), "Prepare and print the client's tax return."),
            new TaskDefinition(typeof(ComplianceAuditState), "Submit the case for compliance audit."),
            new TaskDefinition(typeof(CaseOutcomeState), "Present the completed findings to the client."),
            new TaskDefinition(typeof(ArchiveCaseState), "Archive the completed case in the filing cabinet."),
        };
    }

    // =========================================================
    // EDITABLE OBJECTIVES
    // =========================================================

    public static string ObjectivesResourcePath(string levelId) => $"Levels/{levelId}/objectives";

    public static List<TaskDefinition> LoadObjectives(string levelId)
    {
        string path = ObjectivesResourcePath(levelId);
        TextAsset file = Resources.Load<TextAsset>(path);
        if (file == null)
        {
            Debug.LogWarning($"[TaskListProvider] Resources/{path}.json not found. Using built-in objectives.");
            return GetBuiltInObjectives(levelId);
        }

        ObjectivesFile parsed = null;
        try
        {
            parsed = JsonUtility.FromJson<ObjectivesFile>(file.text);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TaskListProvider] {path}.json is not valid JSON ({e.Message}). Using built-in objectives.");
            return GetBuiltInObjectives(levelId);
        }

        List<TaskDefinition> tasks = Build(parsed, out string problems);
        if (!string.IsNullOrEmpty(problems))
            Debug.LogWarning($"[TaskListProvider] {path}.json: {problems}");

        if (tasks == null)
        {
            Debug.LogWarning($"[TaskListProvider] {path}.json has no valid steps. Using built-in objectives.");
            return GetBuiltInObjectives(levelId);
        }

        return tasks;
    }

    private static List<TaskDefinition> Build(ObjectivesFile parsed, out string problems)
    {
        var report = new StringBuilder();
        if (parsed == null || parsed.objectives == null || parsed.objectives.Length == 0)
        {
            problems = "No \"objectives\" array found.";
            return null;
        }

        var tasks = new List<TaskDefinition>();
        int steps = 0;
        for (int o = 0; o < parsed.objectives.Length; o++)
        {
            ObjectiveEntry objective = parsed.objectives[o];
            if (objective == null || objective.steps == null || objective.steps.Length == 0)
            {
                report.Append($"Objective {o + 1} has no steps and was skipped. ");
                continue;
            }

            int headerIndex = tasks.Count;
            string title = string.IsNullOrWhiteSpace(objective.title) ? $"Objective {o + 1}" : objective.title.Trim();
            tasks.Add(new TaskDefinition(null, title, true, null));

            int added = 0;
            for (int s = 0; s < objective.steps.Length; s++)
            {
                StepEntry step = objective.steps[s];
                string label = $"Objective {o + 1} step {s + 1}";
                if (step == null || string.IsNullOrWhiteSpace(step.text))
                {
                    report.Append($"{label} has no text and was skipped. ");
                    continue;
                }

                string trigger = ObjectiveTriggers.Normalize(step.trigger);
                string error = ObjectiveTriggers.Validate(trigger);
                if (error != null)
                {
                    report.Append($"{label}: {error}; skipped. ");
                    continue;
                }

                string progress = string.IsNullOrWhiteSpace(step.progress) ? null : step.progress.Trim().ToLowerInvariant();
                if (progress != null && progress != ObjectiveTriggers.ProfilesProgress)
                {
                    report.Append($"{label}: unknown progress \"{progress}\" ignored. ");
                    progress = null;
                }

                string undoneBy = ObjectiveTriggers.Normalize(step.undoneBy);
                if (undoneBy != null)
                {
                    string undoError = ObjectiveTriggers.Validate(undoneBy);
                    if (undoError != null)
                    {
                        report.Append($"{label}: undoneBy {undoError}; ignored. ");
                        undoneBy = null;
                    }
                }

                tasks.Add(new TaskDefinition(trigger, step.text.Trim(), false, progress) { UndoneByKey = undoneBy });
                added++;
                steps++;
            }

            if (added == 0) tasks.RemoveAt(headerIndex);
        }

        problems = report.ToString().Trim();
        return steps > 0 ? tasks : null;
    }

    /// <summary>
    /// Built-in copy of the Level 1 objectives (mirrors objectives.json).
    /// Other levels fall back to the legacy case task list.
    /// </summary>
    public static List<TaskDefinition> GetBuiltInObjectives(string levelId)
    {
        if (levelId != Level1Id) return GetTasks();

        return new List<TaskDefinition>
        {
            new TaskDefinition(null, "Finish Side Quest 1", true, null),
            // T30: the rounds start with Arthur's reviewed file.
            new TaskDefinition(ObjectiveTriggers.EventKey(ObjectiveTriggers.ArthurMet), "Meet Arthur.", false, null),
            new TaskDefinition(ObjectiveTriggers.EventKey(ObjectiveTriggers.ReviewedFileLookedOver), "Look over Arthur's file.", false, null),
            new TaskDefinition(ObjectiveTriggers.EventKey(ObjectiveTriggers.AllProfilesCollected),
                "Meet the four clients and collect all 4 IDs.", false,
                ObjectiveTriggers.ProfilesProgress),
            new TaskDefinition(ObjectiveTriggers.EventKey(ObjectiveTriggers.TutorialGuideTalkedTo),
                "Talk to Arthur in the plaza.", false, null),
            new TaskDefinition(ObjectiveTriggers.EventKey(ObjectiveTriggers.SideQuestCompleted),
                "Organize the four IDs on Arthur's corkboard.", false, null),

            new TaskDefinition(null, "Finish a Tax Case", true, null),
            new TaskDefinition(ObjectiveTriggers.AreaPrefix + ReceptionArea,
                "Go to the office.", false, null),
            new TaskDefinition(ObjectiveTriggers.EventKey(ObjectiveTriggers.ReceptionistTalkedTo),
                "Talk to the receptionist.", false, null),
            new TaskDefinition(ObjectiveTriggers.EventKey(ObjectiveTriggers.CaseFolderStored),
                "Go to the main office room, finish the tax case and drag the case folder to your folder icon.", false, null)
            {
                UndoneByKey = ObjectiveTriggers.EventKey(ObjectiveTriggers.CaseFolderReturned)
            },
            new TaskDefinition(ObjectiveTriggers.StatePrefix + nameof(CaseCompleteState),
                "Talk to the Auditor in his office and hand him the case folder.", false, null),

            // T08: the Client Copy handover (story runs; replays end at the result card).
            new TaskDefinition(null, "Follow Through with Marie", true, null),
            new TaskDefinition(ObjectiveTriggers.EventKey(ObjectiveTriggers.ClientCopyPickedUp),
                "Pick up Marie's client copy from the Auditor.", false, null),
            new TaskDefinition(ObjectiveTriggers.EventKey(ObjectiveTriggers.ClientCopyHandedOver),
                "Return Marie's client copy (Conference Room).", false, null),
        };
    }

    /// <summary>Resolves a state class name (e.g. "ResearchTaxState") to its IGameState type.</summary>
    public static Type ResolveState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName)) return null;

        Type type = typeof(IGameState).Assembly.GetType(stateName.Trim(), false);
        if (type == null || type.IsAbstract || !typeof(IGameState).IsAssignableFrom(type)) return null;
        return type;
    }
}
