/// <summary>What kind of run is playing (Campaign Foundation T03). Values are never saved.</summary>
public enum CampaignRunMode
{
    /// <summary>No run chosen by a menu route (in the menu, or a scene opened directly).</summary>
    None = 0,
    /// <summary>The campaign story (New Game / Continue / starting the current chapter).</summary>
    Story = 1,
    /// <summary>Replaying a finished chapter from the career board: best XP / badges only.</summary>
    MasteryReplay = 2,
    /// <summary>A Training Hub drill (TrainingSession): lesson progress only.</summary>
    Practice = 3,
    /// <summary>A level opened directly in the Unity Editor: temporary, never story progress.</summary>
    DirectEditor = 4
}

/// <summary>
/// PURPOSE:
/// The explicit run mode and what each mode may change (pure C#, no Unity).
///
/// - Story: may write story progression, Public Trust, claims, coins,
///   ownership and the campaign checkpoint (later tickets add those commands
///   and must check AllowsStoryWrites first).
/// - Mastery replay: may improve the chapter's best XP / badges only; never
///   the story, its checkpoint, trust, coins or supplies.
/// - Practice: lesson progress only (TrainingSession already keeps results
///   out of the career).
/// - Direct Editor: a temporary test run; nothing is written to the story.
///
/// Menu routes call Begin; the 3D Menu calls EndRun when it opens. A level
/// loaded with no route resolves to DirectEditor.
/// </summary>
public static class CampaignRun
{
    /// <summary>The mode the last menu route chose (None in the menu).</summary>
    public static CampaignRunMode Chosen { get; private set; }

    public static void Begin(CampaignRunMode mode) => Chosen = mode == CampaignRunMode.DirectEditor ? CampaignRunMode.None : mode;

    public static void EndRun() => Chosen = CampaignRunMode.None;

    /// <summary>The mode in force: a practice drill always wins; no chosen route means a direct Editor run.</summary>
    public static CampaignRunMode Resolve(CampaignRunMode chosen, bool practicing) =>
        practicing ? CampaignRunMode.Practice : chosen == CampaignRunMode.None ? CampaignRunMode.DirectEditor : chosen;

    /// <summary>Story progression, trust, claims, coins, ownership and supplies.</summary>
    public static bool AllowsStoryWrites(CampaignRunMode mode) => mode == CampaignRunMode.Story;

    /// <summary>The resumable checkpoint of the story (replays and drills never replace it).</summary>
    public static bool AllowsStoryCheckpoint(CampaignRunMode mode) => mode == CampaignRunMode.Story;

    /// <summary>Short label for menus and confirmations.</summary>
    public static string Label(CampaignRunMode mode)
    {
        switch (mode)
        {
            case CampaignRunMode.Story: return "STORY";
            case CampaignRunMode.MasteryReplay: return "REPLAY";
            case CampaignRunMode.Practice: return "PRACTICE";
            case CampaignRunMode.DirectEditor: return "EDITOR TEST";
            default: return "";
        }
    }

    /// <summary>One plain sentence: what this kind of run can change.</summary>
    public static string WhatChanges(CampaignRunMode mode)
    {
        switch (mode)
        {
            case CampaignRunMode.Story: return "Your story, Public Trust, coins and items are saved as you play.";
            case CampaignRunMode.MasteryReplay: return "Replay: only your best XP and badges can improve. Story, Public Trust, coins and items stay the same.";
            case CampaignRunMode.Practice: return "Practice: only lesson progress is saved.";
            case CampaignRunMode.DirectEditor: return "Editor test run: nothing is saved to the story.";
            default: return "";
        }
    }
}
