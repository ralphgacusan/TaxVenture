using System;

/// <summary>
/// PURPOSE:
/// Level 1 order: the case (receptionist, desk, Case Folder) stays locked
/// until the side quest is finished, when rewards.json asks for it
/// (requireSideQuestBeforeOffice). Blocked attempts raise OnBlocked so the
/// HUD can say what to do first. Reset when a level starts.
/// </summary>
public static class LevelGate
{
    public const string SideQuestFirstMessage = "Finish Side Quest 1 first";

    public static bool RequireSideQuestFirst { get; private set; }
    public static bool SideQuestDone { get; private set; }

    /// <summary>True once a level has set the gate up (rewards.json read).</summary>
    public static bool Configured { get; private set; }

    /// <summary>Scene names of the office entrance doors (rewards.json officeDoors).</summary>
    public static string[] OfficeDoors { get; private set; } = new string[0];

    /// <summary>Raised when something is used too early (message for the player).</summary>
    public static event Action<string> OnBlocked;

    public static void Reset(bool requireSideQuestFirst, string[] officeDoors = null)
    {
        RequireSideQuestFirst = requireSideQuestFirst;
        SideQuestDone = false;
        OfficeDoors = officeDoors ?? new string[0];
        Configured = true;
    }

    public static void MarkSideQuestDone() => SideQuestDone = true;

    /// <summary>True when the case may be worked on now.</summary>
    public static bool CaseAllowed => !RequireSideQuestFirst || SideQuestDone;

    /// <summary>Returns true when allowed; otherwise tells the player why not.</summary>
    public static bool TryCase()
    {
        if (CaseAllowed) return true;
        OnBlocked?.Invoke(SideQuestFirstMessage);
        return false;
    }

    public static void NotifyBlocked(string message) => OnBlocked?.Invoke(message);
}
