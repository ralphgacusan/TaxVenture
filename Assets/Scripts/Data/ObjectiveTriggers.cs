using System;

/// <summary>
/// PURPOSE:
/// The vocabulary of objective triggers used by the editable objectives
/// JSON. Pure data/validation — no Unity calls — so it can be checked
/// outside the editor. OfficeObjectives does the actual listening.
///
/// TRIGGER KEYS:
///   state:&lt;IGameState class&gt;  e.g. state:CaseCompleteState
///   event:&lt;name&gt;               one of the Events below
///   area:&lt;scene path&gt;          e.g. area:Environment/OfficeBuilding/Rooms/Reception/Floor
///                              several paths joined with | count as one area
/// A step may also name an "undoneBy" trigger that unchecks it again
/// (e.g. the case folder coming back out of the HUD icon after a failed audit).
/// </summary>
public static class ObjectiveTriggers
{
    public const string StatePrefix = "state:";
    public const string EventPrefix = "event:";
    public const string AreaPrefix = "area:";

    // Event names and the existing game event each one listens to.
    public const string ProfileCollected = "ProfileCollected";             // TownEvents.OnProfileCollected
    public const string AllProfilesCollected = "AllProfilesCollected";     // TownEvents.OnAllProfilesCollected
    public const string TutorialGuideAppeared = "TutorialGuideAppeared";   // TownEvents.OnTutorialNpcReady
    public const string TutorialGuideTalkedTo = "TutorialGuideTalkedTo";   // TownEvents.OnTutorialIntroCompleted
    public const string SideQuestCompleted = "SideQuestCompleted";         // TownEvents.OnSideQuestCompleted
    public const string ReceptionistTalkedTo = "ReceptionistTalkedTo";     // GameplayEvents.OnNotesUnlockRequested
    public const string CaseFolderOpened = "CaseFolderOpened";             // GameplayEvents.OnCaseFolderOpened
    public const string TaxCodeBookOpened = "TaxCodeBookOpened";           // GameplayEvents.OnTaxCodeBookFirstOpened
    public const string TaxReturnCollected = "TaxReturnCollected";         // GameplayEvents.OnTaxReturnCollected
    public const string CaseCompleted = "CaseCompleted";                   // GameplayEvents.OnCaseCompleted
    public const string CaseFolderStored = "CaseFolderStored";             // GameplayEvents.OnCaseFolderStored
    public const string CaseFolderReturned = "CaseFolderReturned";         // GameplayEvents.OnCaseFolderReturned
    public const string CaseFolderRetrieved = "CaseFolderRetrieved";       // GameplayEvents.OnCaseFolderRetrieved
    public const string FindingRecorded = "FindingRecorded";               // GameplayEvents.OnFindingRecorded
    public const string PreliminaryHoldStamped = "PreliminaryHoldStamped"; // GameplayEvents.OnPreliminaryHoldStamped
    public const string RosaRecordsCollected = "RosaRecordsCollected";     // GameplayEvents.OnRosaRecordsCollected
    public const string MarketDeskOpened = "MarketDeskOpened";             // GameplayEvents.OnMarketDeskOpened

    public static readonly string[] Events =
    {
        ProfileCollected, AllProfilesCollected, TutorialGuideAppeared, TutorialGuideTalkedTo,
        SideQuestCompleted, ReceptionistTalkedTo, CaseFolderOpened, TaxCodeBookOpened,
        TaxReturnCollected, CaseCompleted, CaseFolderStored, CaseFolderReturned, CaseFolderRetrieved,
        FindingRecorded, PreliminaryHoldStamped, RosaRecordsCollected, MarketDeskOpened
    };

    /// <summary>Live counters a step may show next to its text.</summary>
    public const string ProfilesProgress = "profiles";

    public static string EventKey(string eventName) => EventPrefix + eventName;

    public static string StateKey(Type stateType) => stateType != null ? StatePrefix + stateType.Name : null;

    /// <summary>Returns null when the key is valid, otherwise a short reason.</summary>
    public static string Validate(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "missing trigger";

        if (key.StartsWith(StatePrefix, StringComparison.Ordinal))
            return TryGetState(key, out _) ? null : $"unknown state \"{key.Substring(StatePrefix.Length)}\"";

        if (key.StartsWith(EventPrefix, StringComparison.Ordinal))
        {
            string name = key.Substring(EventPrefix.Length);
            return Array.IndexOf(Events, name) >= 0 ? null : $"unknown event \"{name}\"";
        }

        if (key.StartsWith(AreaPrefix, StringComparison.Ordinal))
            return TryGetAreaPaths(key, out _) ? null : "area needs a scene path (several joined with |)";

        return $"trigger \"{key}\" must start with state:, event: or area:";
    }

    public static bool TryGetState(string key, out Type stateType)
    {
        stateType = null;
        if (key == null || !key.StartsWith(StatePrefix, StringComparison.Ordinal)) return false;
        stateType = TaskListProvider.ResolveState(key.Substring(StatePrefix.Length));
        return stateType != null;
    }

    public static bool TryGetArea(string key, out string scenePath)
    {
        scenePath = null;
        if (key == null || !key.StartsWith(AreaPrefix, StringComparison.Ordinal)) return false;
        scenePath = key.Substring(AreaPrefix.Length).Trim().Trim('/');
        return scenePath.Length > 0;
    }

    /// <summary>Splits an area trigger into its scene paths ("A|B" = union of both).</summary>
    public static bool TryGetAreaPaths(string key, out string[] paths)
    {
        paths = null;
        if (!TryGetArea(key, out string all)) return false;

        string[] parts = all.Split('|');
        var cleaned = new System.Collections.Generic.List<string>(parts.Length);
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i].Trim().Trim('/');
            if (part.Length == 0) return false;
            cleaned.Add(part);
        }
        paths = cleaned.ToArray();
        return paths.Length > 0;
    }

    /// <summary>Normalises a key (trims spaces around the prefix and value).</summary>
    public static string Normalize(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        key = key.Trim();
        int colon = key.IndexOf(':');
        if (colon < 0) return key;
        return key.Substring(0, colon + 1).ToLowerInvariant() + key.Substring(colon + 1).Trim();
    }
}
