using System;
using System.Collections.Generic;

/// <summary>
/// The player's Mark the Money! challenge (Level 2, T06): their work on each
/// scored entry, the frozen first full check, and whether the corrections
/// are done and the award was given. Saved with the review; never the key.
/// </summary>
[Serializable]
public class ChallengeState
{
    /// <summary>Scoring rules version; used to safely migrate older saved checks.</summary>
    public int scoringVersion;
    public List<AnnotationEntryState> entries = new List<AnnotationEntryState>();
    public string currentEntryId;

    /// <summary>The first complete check is frozen here and never changes again.</summary>
    public bool firstChecked;
    public int firstCorrect;
    public int firstTotal;
    public List<string> firstWrong = new List<string>();

    /// <summary>Complete checks so far (hints grow more specific with them; never a deduction).</summary>
    public int checks;

    /// <summary>Decisions the last check found right (locked) or wrong (red), until that note is changed.</summary>
    public List<string> confirmed = new List<string>();
    public List<string> flagged = new List<string>();

    /// <summary>The challenge was introduced (Rosa's records dealt out) once.</summary>
    public bool introSeen;

    /// <summary>Every decision correct (after corrections).</summary>
    public bool mastered;
    /// <summary>The quest award was given (once).</summary>
    public bool awarded;

    public AnnotationEntryState Find(string entryId)
    {
        if (entries == null || string.IsNullOrEmpty(entryId)) return null;
        foreach (AnnotationEntryState entry in entries)
            if (entry != null && entry.entryId == entryId) return entry;
        return null;
    }
}

/// <summary>
/// PURPOSE:
/// The scored side of the income toolkit, pure C# so the regression suite
/// can test every rule outside Unity.
///
/// RULES (Documentation/Level2SomethingDoesNotMatch REWARDS_AND_VALIDATION):
/// - Four entries x (highlight, TYPE, TREATMENT, EVIDENCE) = 16 decisions.
/// - An incomplete check scores nothing and changes nothing; it says what is
///   missing.
/// - The first complete check freezes correct count and wrong decisions once.
///   Later checks (corrections) never change it, so a tier cannot be raised.
/// - Evidence is right only as the exact record set (a bundle needs both).
/// - Mastery (all 16 right) is required before the award, which is given once.
/// - Nothing here touches the summary, findings, stamps or packet revision.
/// </summary>
public static class IncomeChallenge
{
    public const int ScoringVersion = 2;
    public static readonly AnnotationSlot[] ScoredSlots = { AnnotationSlot.Type, AnnotationSlot.Treatment, AnnotationSlot.Evidence, AnnotationSlot.Highlight };

    public enum CheckResult { NotAvailable, Incomplete, Checked }

    public struct Decision
    {
        public string EntryId;
        public AnnotationSlot Slot;
        public bool Correct;
        public string Id => DecisionId(EntryId, Slot);
    }

    public static string DecisionId(string entryId, AnnotationSlot slot) => entryId + "." + slot;

    /// <summary>The challenge can be edited: after the tutorial, before mastery.</summary>
    public static bool CanEdit(CaseReviewState review) =>
        review != null && review.challenge != null && !review.challenge.mastered &&
        (review.phase == ReviewPhase.Challenge || review.phase == ReviewPhase.Feedback);

    /// <summary>The challenge state (created on first use, once the review reached the challenge).</summary>
    public static ChallengeState Ensure(CaseReviewState review, CaseReviewDefinition definition = null)
    {
        if (review == null || review.phase < ReviewPhase.Challenge) return null;
        if (review.challenge == null) review.challenge = new ChallengeState();
        ChallengeState challenge = review.challenge;
        if (challenge.entries == null) challenge.entries = new List<AnnotationEntryState>();
        if (challenge.firstWrong == null) challenge.firstWrong = new List<string>();
        if (challenge.confirmed == null) challenge.confirmed = new List<string>();
        if (challenge.flagged == null) challenge.flagged = new List<string>();
        MigrateScoring(challenge, definition);
        return challenge;
    }

    private static void MigrateScoring(ChallengeState challenge, CaseReviewDefinition definition)
    {
        if (challenge.scoringVersion >= ScoringVersion) return;

        if (challenge.awarded)
        {
            // A completed old save cannot be sent through the quest twice. Add
            // its previously unscored highlight decisions to the displayed grade.
            if (definition?.annotation != null && challenge.firstChecked)
            {
                List<AnnotationEntryDefinition> entries = definition.annotation.entries ?? new List<AnnotationEntryDefinition>();
                foreach (AnnotationEntryDefinition entry in entries)
                {
                    if (entry == null) continue;
                    AnnotationKeyDefinition key = definition.FindKey(entry.id);
                    AnnotationEntryState work = challenge.Find(entry.id);
                    string id = DecisionId(entry.id, AnnotationSlot.Highlight);
                    challenge.firstWrong.Remove(id);
                    challenge.confirmed.Remove(id);
                    challenge.flagged.Remove(id);
                    if (key == null || work == null || work.highlightLineId != key.highlightLineId)
                    {
                        challenge.firstWrong.Add(id);
                        challenge.flagged.Add(id);
                    }
                    else challenge.confirmed.Add(id);
                }
                challenge.firstTotal = entries.Count * ScoredSlots.Length;
                challenge.firstCorrect = Math.Max(0, challenge.firstTotal - challenge.firstWrong.Count);
            }
            else return; // Wait until the case definition is available.
        }
        else if (challenge.firstChecked || challenge.mastered || challenge.firstTotal != 0)
        {
            // Old unawarded checks omitted the highlight from the score. Keep
            // the player's work but require a fresh first check under 16 rules.
            challenge.firstChecked = false;
            challenge.firstCorrect = 0;
            challenge.firstTotal = 0;
            challenge.firstWrong.Clear();
            challenge.checks = 0;
            challenge.confirmed.Clear();
            challenge.flagged.Clear();
            challenge.mastered = false;
        }

        challenge.scoringVersion = ScoringVersion;
    }

    /// <summary>The player's work on one entry (created on first use).</summary>
    public static AnnotationEntryState Work(ChallengeState challenge, AnnotationEntryDefinition entry)
    {
        if (challenge == null || entry == null) return null;
        AnnotationEntryState work = challenge.Find(entry.id);
        if (work != null) return work;
        AnnotationActions.Ensure(ref work, entry);
        challenge.entries.Add(work);
        return work;
    }

    public static bool IsCorrect(AnnotationEntryState work, AnnotationKeyDefinition key, AnnotationSlot slot)
    {
        if (work == null || key == null) return false;
        switch (slot)
        {
            case AnnotationSlot.Type: return !string.IsNullOrEmpty(work.typeId) && work.typeId == key.typeId;
            case AnnotationSlot.Treatment: return !string.IsNullOrEmpty(work.treatmentId) && work.treatmentId == key.treatmentId;
            case AnnotationSlot.Highlight: return !string.IsNullOrEmpty(work.highlightLineId) && work.highlightLineId == key.highlightLineId;
            default:
                if (work.evidenceIds == null || key.evidenceIds == null || work.evidenceIds.Count != key.evidenceIds.Count) return false;
                foreach (string id in key.evidenceIds)
                    if (!work.evidenceIds.Contains(id)) return false;
                return work.evidenceIds.Count > 0;
        }
    }

    /// <summary>What still has to be done before a check, e.g. "E2: TREATMENT note" (empty = ready).</summary>
    public static List<string> Missing(ChallengeState challenge, AnnotationDefinition annotation)
    {
        var missing = new List<string>();
        if (annotation == null || annotation.entries == null) return missing;
        foreach (AnnotationEntryDefinition entry in annotation.entries)
        {
            if (entry == null) continue;
            AnnotationEntryState work = challenge != null ? challenge.Find(entry.id) : null;
            if (work == null || string.IsNullOrEmpty(work.highlightLineId)) missing.Add(entry.id + ": highlight line");
            if (work == null || string.IsNullOrEmpty(work.typeId)) missing.Add(entry.id + ": TYPE note");
            if (work == null || string.IsNullOrEmpty(work.treatmentId)) missing.Add(entry.id + ": TREATMENT note");
            if (work == null || work.evidenceIds == null || work.evidenceIds.Count == 0) missing.Add(entry.id + ": EVIDENCE record");
        }
        return missing;
    }

    /// <summary>True when one entry has its line marked and every card place filled.</summary>
    public static bool EntryComplete(ChallengeState challenge, AnnotationEntryDefinition entry)
    {
        AnnotationEntryState work = challenge != null && entry != null ? challenge.Find(entry.id) : null;
        return work != null && work.IsComplete;
    }

    public static List<Decision> Grade(ChallengeState challenge, CaseReviewDefinition review)
    {
        var decisions = new List<Decision>();
        if (review == null || review.annotation == null || review.annotation.entries == null) return decisions;
        foreach (AnnotationEntryDefinition entry in review.annotation.entries)
        {
            if (entry == null) continue;
            AnnotationEntryState work = challenge != null ? challenge.Find(entry.id) : null;
            AnnotationKeyDefinition key = review.FindKey(entry.id);
            foreach (AnnotationSlot slot in ScoredSlots)
                decisions.Add(new Decision { EntryId = entry.id, Slot = slot, Correct = IsCorrect(work, key, slot) });
        }
        return decisions;
    }

    /// <summary>
    /// "Check annotations". Incomplete: nothing is scored or counted.
    /// Complete: grades all decisions; the first complete check is frozen
    /// (Challenge -> Feedback); all correct marks mastery.
    /// </summary>
    public static CheckResult TryCheck(CaseReviewState state, CaseReviewDefinition review,
        out List<Decision> decisions, out List<string> missing)
    {
        decisions = new List<Decision>();
        missing = new List<string>();
        if (state?.challenge != null) Ensure(state, review);
        if (!CanEdit(state) || review == null || review.annotation == null || review.annotation.entries == null ||
            review.annotation.entries.Count == 0)
            return CheckResult.NotAvailable;
        ChallengeState challenge = state.challenge;
        challenge.scoringVersion = ScoringVersion;
        missing = Missing(challenge, review.annotation);
        if (missing.Count > 0) return CheckResult.Incomplete;

        decisions = Grade(challenge, review);
        challenge.checks++;
        int correct = 0;
        foreach (Decision d in decisions) if (d.Correct) correct++;
        if (!challenge.firstChecked)
        {
            challenge.firstChecked = true;
            challenge.firstCorrect = correct;
            challenge.firstTotal = decisions.Count;
            challenge.firstWrong = new List<string>();
            foreach (Decision d in decisions) if (!d.Correct) challenge.firstWrong.Add(d.Id);
            state.TryAdvance(ReviewPhase.Feedback);
        }
        challenge.confirmed = new List<string>();
        challenge.flagged = new List<string>();
        foreach (Decision d in decisions) (d.Correct ? challenge.confirmed : challenge.flagged).Add(d.Id);
        if (correct == decisions.Count) challenge.mastered = true;
        return CheckResult.Checked;
    }

    /// <summary>A note confirmed right by the last check stays put (only red notes are reworked).</summary>
    public static bool IsLocked(ChallengeState challenge, string entryId, AnnotationSlot slot) =>
        challenge != null && challenge.confirmed != null && challenge.confirmed.Contains(DecisionId(entryId, slot));

    public static bool IsFlagged(ChallengeState challenge, string entryId, AnnotationSlot slot) =>
        challenge != null && challenge.flagged != null && challenge.flagged.Contains(DecisionId(entryId, slot));

    /// <summary>The player changed a note: its last result no longer applies (it is unknown until the next check).</summary>
    public static void NoteEdited(ChallengeState challenge, string entryId, AnnotationSlot slot)
    {
        if (challenge == null) return;
        string id = DecisionId(entryId, slot);
        challenge.confirmed?.Remove(id);
        challenge.flagged?.Remove(id);
    }

    /// <summary>Wrong answers on the frozen first check (the EXP deduction count).</summary>
    public static int FirstWrongCount(ChallengeState challenge) =>
        challenge != null && challenge.firstChecked ? Math.Max(0, challenge.firstTotal - challenge.firstCorrect) : 0;

    /// <summary>
    /// The award, once, after mastery: the review moves on to office
    /// reconciliation. False when not mastered or already awarded.
    /// </summary>
    public static bool TryAward(CaseReviewState state)
    {
        ChallengeState challenge = state?.challenge;
        if (challenge == null || !challenge.mastered || !challenge.firstChecked || challenge.awarded) return false;
        challenge.awarded = true;
        state.TryAdvance(ReviewPhase.OfficeReconciliation);
        return true;
    }

    /// <summary>A correction hint for one wrong decision: general first, more specific on later checks.</summary>
    public static string Hint(AnnotationKeyDefinition key, AnnotationSlot slot, int checks)
    {
        List<string> hints = key?.HintsFor(slot);
        if (hints == null || hints.Count == 0) return null;
        int index = Math.Min(hints.Count - 1, Math.Max(0, checks - 1));
        return hints[index];
    }

    /// <summary>The corrected per-entry facts for the Annotated Income List (no sum, no reconciliation).</summary>
    public static List<string> AnnotatedList(ChallengeState challenge, AnnotationDefinition annotation)
    {
        var rows = new List<string>();
        if (challenge == null || annotation == null || annotation.entries == null) return rows;
        foreach (AnnotationEntryDefinition entry in annotation.entries)
        {
            AnnotationEntryState work = challenge.Find(entry.id);
            if (work == null) continue;
            string type = annotation.FindOption(AnnotationSlot.Type, work.typeId, entry)?.label ?? "-";
            string treatment = annotation.FindOption(AnnotationSlot.Treatment, work.treatmentId, entry)?.label ?? "-";
            string highlight = entry.lines?.Find(line => line != null && line.id == work.highlightLineId)?.label ?? "-";
            var records = new List<string>();
            if (work.evidenceIds != null)
                foreach (string id in work.evidenceIds) records.Add(id);
            rows.Add(entry.title + ": " + highlight + " · " + type + " · " + treatment + " · " + string.Join(" + ", records));
        }
        return rows;
    }
}
