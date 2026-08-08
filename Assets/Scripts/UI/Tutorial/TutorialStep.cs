using System;

/// <summary>
/// PURPOSE:
/// One tutorial popup's data. Matches the JSON shape in tutorial_steps.json
/// exactly (field names are case-sensitive for JsonUtility).
///
/// "triggerId" is the key piece: any interactable can complete this step by
/// calling TutorialController.Instance.ReportInteraction("someId") where
/// someId matches this step's triggerId. No enum, no per-NPC event needed —
/// just a matching string.
///
/// Steps with an empty/null triggerId are "no gameplay action required" —
/// they auto-advance as soon as Continue is pressed (e.g. intro text,
/// "nice job" confirmations).
/// </summary>
[Serializable]
public class TutorialStep
{
    public string id;              // Unique id for this step (for logging/debugging)
    public string speakerName;     // e.g. "Colleague"
    public string text;            // The tutorial line shown
    public string triggerId;       // Interactable ID this step waits for. Empty = no wait.
    public string targetName;      // Optional. Name of a scene GameObject the arrow should point at. Empty = no arrow.
    public string portraitId;      // Optional. Looked up via DialoguePortraitDatabase, same as DialogueLine. Empty = use default placeholder.
}

/// <summary>
/// Wrapper class because JsonUtility cannot deserialize a top-level JSON array
/// directly — the JSON file wraps the array in a "steps" field.
/// </summary>
[Serializable]
public class TutorialStepList
{
    public TutorialStep[] steps;
}