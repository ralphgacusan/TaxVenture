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
/// no guide intro, pins or counters. T30: Arthur's opening follows
/// ReviewedFileRules (a quiet Migrate of pre-T30 runs, then Repair). Nothing
/// here decides correctness: the
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
        // T30: T24's retired record ids go and begun rounds stay open, quietly (an upgrade, not a repair).
        ReviewedFileRules.Migrate(checkpoint);
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

        // T30: Arthur's file records only; a record put back was shown; a revisit needs both back.
        changed |= ReviewedFileRules.Repair(checkpoint);

        var collected = new HashSet<string>(checkpoint.collectedProfileIds, StringComparer.Ordinal);
        changed |= checkpoint.placements.RemoveAll(p => !collected.Contains(p.profileId) || !slotIds.Contains(p.slotId)) > 0;
        changed |= checkpoint.slotAttempts.RemoveAll(a => !slotIds.Contains(a.slotId)) > 0;

        // T06: a first check needs a complete board (4 cards) and fixes the wrong count.
        if (checkpoint.checkSubmitted)
        {
            int wrong = ClassificationCheck.Profiles - checkpoint.checkCorrectCount;
            if (checkpoint.wrongPlacements != wrong) { checkpoint.wrongPlacements = wrong; changed = true; }
            if (checkpoint.placementAttempts < ClassificationCheck.Profiles) { checkpoint.placementAttempts = ClassificationCheck.Profiles; changed = true; }
            changed |= checkpoint.checkWrongProfileIds.RemoveAll(id => !collected.Contains(id)) > 0;
            if (checkpoint.checkWrongProfileIds.Count != wrong) { checkpoint.checkWrongProfileIds.Clear(); changed = true; }
        }

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
                checkpoint.placementAttempts > 0 || checkpoint.wrongPlacements > 0 || checkpoint.checkSubmitted)
                changed = true;
            checkpoint.guideIntroDone = false;
            checkpoint.placements.Clear();
            checkpoint.slotAttempts.Clear();
            checkpoint.placementAttempts = 0;
            checkpoint.wrongPlacements = 0;
            checkpoint.checkSubmitted = false;
            checkpoint.checkCorrectCount = -1;
            checkpoint.checkWrongProfileIds.Clear();
            checkpoint.checkSettledExp = -1;
            checkpoint.checkCount = 0;
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

    // =========================================================
    // CASE (T05)
    // =========================================================

    /// <summary>
    /// Checks the saved case against the case being played. Another case or a
    /// changed revision (fingerprint) is never applied: it is moved to
    /// setAsideCase (kept for recovery) and the case starts from the
    /// receptionist (Reset, the caller explains it). Case progress before the
    /// side quest's board was solved cannot exist (the office is gated), so it
    /// is dropped. Unknown folder keys are dropped (Repaired).
    /// </summary>
    public static Level1RestoreOutcome ValidateCase(Level1FieldCheckpoint checkpoint, string caseId, string fingerprint,
        ICollection<string> fieldKeys)
    {
        if (checkpoint == null || checkpoint.caseRecord == null || !checkpoint.caseRecord.IsSet) return Level1RestoreOutcome.None;
        Level1CaseRecord saved = checkpoint.caseRecord;
        if (string.IsNullOrEmpty(caseId) || saved.caseId != caseId || saved.caseFingerprint != fingerprint)
        {
            checkpoint.setAsideCase = saved;
            checkpoint.caseRecord = new Level1CaseRecord();
            return Level1RestoreOutcome.Reset;
        }
        bool officeOpen = checkpoint.boardPhase == Level1FieldCheckpoint.PhaseSolved || checkpoint.boardPhase == Level1FieldCheckpoint.PhaseRewarded;
        if (!officeOpen)
        {
            checkpoint.caseRecord = new Level1CaseRecord();
            return Level1RestoreOutcome.Repaired;
        }
        bool changed = checkpoint.Repair();
        if (fieldKeys != null) changed |= saved.fields.RemoveAll(f => !fieldKeys.Contains(f.key)) > 0;
        return changed ? Level1RestoreOutcome.Repaired : Level1RestoreOutcome.Restored;
    }

    // =========================================================
    // CLIENT COPY HANDOVER (T08)
    // =========================================================

    /// <summary>
    /// The part of the checkpoint after the case result. A recorded result
    /// means the case is approved: it is never restored to "case received";
    /// the Client Copy is beside the Auditor, in the folder icon, or already
    /// handed over while its result flow is pending. A missing phase (the app
    /// closed between the result and the next save) is the copy beside the
    /// Auditor, quietly; an unknown one falls back to it too (Repaired).
    /// Without a recorded result no handover phase is kept.
    /// </summary>
    public static Level1RestoreOutcome ValidateHandover(Level1FieldCheckpoint checkpoint, bool caseResultRecorded, bool followThroughDone)
    {
        if (checkpoint == null) return Level1RestoreOutcome.None;
        string phase = checkpoint.handoverPhase == null ? null : checkpoint.handoverPhase.Trim();
        bool known = phase != null && Level1FieldCheckpoint.IsKnownHandover(phase);
        if (!caseResultRecorded)
        {
            if (phase == Level1FieldCheckpoint.HandoverNone) return Level1RestoreOutcome.None;
            checkpoint.handoverPhase = Level1FieldCheckpoint.HandoverNone;
            return Level1RestoreOutcome.Repaired;
        }
        if (followThroughDone)
        {
            if (phase == Level1FieldCheckpoint.HandoverDone) return Level1RestoreOutcome.Restored;
            checkpoint.handoverPhase = Level1FieldCheckpoint.HandoverDone;
            return Level1RestoreOutcome.Repaired;
        }
        if (phase == Level1FieldCheckpoint.HandoverDone)
        {
            checkpoint.handoverPhase = Level1FieldCheckpoint.HandoverAtAuditor;
            return Level1RestoreOutcome.Repaired;
        }
        if (!known)
        {
            checkpoint.handoverPhase = Level1FieldCheckpoint.HandoverAtAuditor;
            return Level1RestoreOutcome.Repaired;
        }
        if (phase == Level1FieldCheckpoint.HandoverNone)
        {
            checkpoint.handoverPhase = Level1FieldCheckpoint.HandoverAtAuditor;
            return Level1RestoreOutcome.Repaired;
        }
        checkpoint.handoverPhase = phase;
        return Level1RestoreOutcome.Restored;
    }

    /// <summary>A short stable fingerprint (FNV-1a 64-bit, hex) of a case definition's authored parts, in order.</summary>
    public static string Fingerprint(IEnumerable<string> parts)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        if (parts != null)
            foreach (string part in parts)
            {
                string text = (part ?? string.Empty) + "\u001f";
                foreach (char c in text)
                {
                    hash ^= c;
                    hash *= prime;
                }
            }
        return hash.ToString("x16");
    }
}
