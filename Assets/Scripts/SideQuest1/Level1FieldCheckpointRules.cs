using System;
using System.Collections.Generic;

/// <summary>What a saved Level 1 field checkpoint can give the restore.</summary>
public enum Level1RestoreOutcome
{
    /// <summary>Nothing saved: the level starts as usual.</summary>
    None = 0,
    /// <summary>Restored exactly as saved.</summary>
    Restored = 1,
    /// <summary>Unknown or impossible parts were dropped; the rest is restored (nearest safe phase).</summary>
    Repaired = 2,
    /// <summary>Unusable (newer format): the level starts from its beginning and the player is told.</summary>
    Reset = 3
}

/// <summary>
/// PURPOSE:
/// Checks a saved Level 1 field checkpoint against the current content before
/// it is restored (Campaign Foundation T04). Pure C#, no Unity.
///
/// Deterministic rules: ids must exist in the content (talked NPCs, profiles,
/// board areas); a collected profile means its NPC's handover happened; pins
/// only for collected profiles; the board (any phase) needs every required
/// profile, otherwise everything from the guide onward falls back to "talk to
/// the guide"; an open board means the guide's intro happened; no board means
/// no guide intro, pins or counters. Nothing here decides correctness: the
/// board recomputes it from the profiles when the pins are put back.
/// </summary>
public static class Level1FieldCheckpointRules
{
    /// <param name="profileNpc">Every content profile id and the NPC who hands it over.</param>
    /// <param name="slotIds">The board's area ids (classification names).</param>
    /// <param name="requiredProfiles">Profiles needed before the guide / board (4).</param>
    public static Level1RestoreOutcome Validate(Level1FieldCheckpoint checkpoint,
        IReadOnlyDictionary<string, string> profileNpc, ICollection<string> slotIds, int requiredProfiles)
    {
        if (checkpoint == null || !checkpoint.IsSet) return Level1RestoreOutcome.None;
        if (checkpoint.version > Level1FieldCheckpoint.CurrentVersion || profileNpc == null || slotIds == null)
            return Level1RestoreOutcome.Reset;

        bool changed = checkpoint.Repair();
        var npcs = new HashSet<string>(profileNpc.Values, StringComparer.Ordinal);

        changed |= checkpoint.talkedNpcIds.RemoveAll(id => !npcs.Contains(id)) > 0;
        changed |= checkpoint.collectedProfileIds.RemoveAll(id => !profileNpc.ContainsKey(id)) > 0;
        foreach (string profile in checkpoint.collectedProfileIds)
        {
            string npc = profileNpc[profile];
            if (!string.IsNullOrEmpty(npc) && !checkpoint.talkedNpcIds.Contains(npc))
            {
                checkpoint.talkedNpcIds.Add(npc);
                changed = true;
            }
        }

        var collected = new HashSet<string>(checkpoint.collectedProfileIds, StringComparer.Ordinal);
        changed |= checkpoint.placements.RemoveAll(p => !collected.Contains(p.profileId) || !slotIds.Contains(p.slotId)) > 0;
        changed |= checkpoint.slotAttempts.RemoveAll(a => !slotIds.Contains(a.slotId)) > 0;

        bool boardOpened = checkpoint.boardPhase != Level1FieldCheckpoint.PhaseNone;
        if (boardOpened && checkpoint.collectedProfileIds.Count < Math.Max(1, requiredProfiles))
        {
            checkpoint.boardPhase = Level1FieldCheckpoint.PhaseNone;
            boardOpened = false;
            changed = true;
        }
        if (!boardOpened)
        {
            if (checkpoint.guideIntroDone || checkpoint.placements.Count > 0 || checkpoint.slotAttempts.Count > 0 ||
                checkpoint.placementAttempts > 0 || checkpoint.wrongPlacements > 0)
                changed = true;
            checkpoint.guideIntroDone = false;
            checkpoint.placements.Clear();
            checkpoint.slotAttempts.Clear();
            checkpoint.placementAttempts = 0;
            checkpoint.wrongPlacements = 0;
        }
        else if (!checkpoint.guideIntroDone)
        {
            checkpoint.guideIntroDone = true;
            changed = true;
        }
        return changed ? Level1RestoreOutcome.Repaired : Level1RestoreOutcome.Restored;
    }

    /// <summary>The content map for Validate: profile id -> handing-over NPC id.</summary>
    public static Dictionary<string, string> ProfileMap(IEnumerable<TaxpayerProfile> profiles)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (profiles == null) return map;
        foreach (TaxpayerProfile profile in profiles)
            if (profile != null && !string.IsNullOrWhiteSpace(profile.ProfileId) && !map.ContainsKey(profile.ProfileId))
                map[profile.ProfileId] = profile.NpcId;
        return map;
    }

    /// <summary>Stable id of a board area: its classification's name.</summary>
    public static string SlotId(TaxpayerClassification classification) => classification.ToString();
}
