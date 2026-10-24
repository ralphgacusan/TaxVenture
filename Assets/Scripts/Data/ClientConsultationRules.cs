using System;
using System.Collections.Generic;

/// <summary>One way of saying a consultation line (a ladder step, a special moment or the fallback).</summary>
[Serializable]
public class ConsultationVariant
{
    /// <summary>Stable id, unique within the case's consultation (saved as the last line said).</summary>
    public string id;
    public List<DialogueLineDefinition> lines = new List<DialogueLineDefinition>();
}

/// <summary>One rung of the visit ladder (1st helpful ... 5th and later warm redirect).</summary>
[Serializable]
public class ConsultationStep
{
    /// <summary>For authors only: helpful, friendly, teasing, playful, redirect.</summary>
    public string mood;
    public List<ConsultationVariant> variants = new List<ConsultationVariant>();
}

/// <summary>
/// A clue that fits the case progress: names a document (its documentName,
/// while it is unread) or a folder field key (Level1CaseCheckpoint.FieldKeys,
/// while it is "empty" or "wrong"). Never an answer value.
/// </summary>
[Serializable]
public class ConsultationClue
{
    public string id;
    public string document;
    public string field;
    /// <summary>"unread" (documents), "empty" or "wrong" (fields). Empty = unread for a document, empty for a field.</summary>
    public string when;
    public List<DialogueLineDefinition> lines = new List<DialogueLineDefinition>();
}

/// <summary>
/// The case client's consultation visits (Campaign Foundation T23), case JSON
/// "consultation". Optional: a case without it has no consultation.
/// </summary>
[Serializable]
public class ClientConsultationDefinition
{
    public bool enabled = true;
    public List<ConsultationClue> clues = new List<ConsultationClue>();
    /// <summary>After a failed audit (empty: the case's clientAfterFailedAudit).</summary>
    public List<ConsultationVariant> failedAudit = new List<ConsultationVariant>();
    /// <summary>On Office Hours day 2 or later.</summary>
    public List<ConsultationVariant> laterDay = new List<ConsultationVariant>();
    /// <summary>The visit ladder; the last step repeats (without an immediate repeat).</summary>
    public List<ConsultationStep> ladder = new List<ConsultationStep>();
    /// <summary>A short neutral line when nothing else is usable.</summary>
    public List<DialogueLineDefinition> fallback = new List<DialogueLineDefinition>();
}

public enum ConsultationFieldState { Filled, Empty, Wrong }

public enum ConsultationKind { Fallback, Clue, FailedAudit, LaterDay, Ladder }

/// <summary>What the consultation knows about the case right now (ids and states only, never answers).</summary>
public struct ConsultationContext
{
    public int FailedAudits;
    public int Day;
    /// <summary>The case folder is in the player's hands (clues only make sense from then on).</summary>
    public bool CaseOpen;
    public Func<string, bool> DocumentUnread;
    public Func<string, ConsultationFieldState> Field;
}

/// <summary>The line chosen for a visit. Lines is null for the built-in neutral fallback.</summary>
public struct ConsultationPick
{
    public ConsultationKind Kind;
    public string Id;
    public int Step;
    public List<DialogueLineDefinition> Lines;
}

/// <summary>
/// PURPOSE:
/// Chooses what the case client says on a consultation visit (T23). Pure: no
/// Unity calls. Priority conditions, not a dialogue tree:
///   1. a case-progress clue that fits (each once until the next reset),
///   2. a special moment (a failed audit, or a new Office Hours day 2+),
///   3. the visit ladder (helpful, friendly, teasing, playful, redirect; the
///      last rung repeats).
/// A failed audit or a new day resets the ladder and the clues. The line just
/// said is never said again on the next visit. Visiting changes nothing but
/// this record: no trust, coins, XP, score, clock, objective or story.
/// </summary>
public static class ClientConsultationRules
{
    public const string WhenUnread = "unread";
    public const string WhenEmpty = "empty";
    public const string WhenWrong = "wrong";
    public const string SpecialFailedAudit = "failedAudit";
    public const string SpecialLaterDay = "laterDay";
    public const string FallbackId = "fallback";

    /// <summary>The ladder's moods by step (documentation and validation).</summary>
    public static readonly string[] Moods = { "helpful", "friendly", "teasing", "playful", "redirect" };

    /// <summary>
    /// A failed audit or a new Office Hours day since the last visit: the
    /// matching special moment waits, the ladder starts over and the clues
    /// may be given again. A failed audit wins over a new day.
    /// </summary>
    public static bool NoteResets(Level1ConsultationRecord state, ConsultationContext context)
    {
        if (state == null) return false;
        bool reset = false;
        if (context.FailedAudits > state.seenFailedAudits)
        {
            state.seenFailedAudits = context.FailedAudits;
            state.pendingSpecial = SpecialFailedAudit;
            reset = true;
        }
        if (context.Day > state.seenDay)
        {
            state.seenDay = context.Day;
            if (state.pendingSpecial != SpecialFailedAudit) state.pendingSpecial = SpecialLaterDay;
            reset = true;
        }
        if (reset)
        {
            state.ladderVisits = 0;
            state.givenClueIds.Clear();
        }
        return reset;
    }

    /// <summary>Chooses this visit's line and records it in the state.</summary>
    public static ConsultationPick Next(ClientConsultationDefinition definition, Level1ConsultationRecord state, ConsultationContext context)
    {
        if (state == null) state = new Level1ConsultationRecord();
        NoteResets(state, context);
        state.visits++;
        definition ??= new ClientConsultationDefinition();

        // 1. A clue that fits the case right now.
        if (context.CaseOpen && definition.clues != null)
            foreach (ConsultationClue clue in definition.clues)
            {
                if (!Usable(clue) || state.givenClueIds.Contains(clue.id.Trim()) || clue.id.Trim() == state.lastLineId) continue;
                if (!ClueFits(clue, context)) continue;
                state.givenClueIds.Add(clue.id.Trim());
                return Said(state, ConsultationKind.Clue, clue.id.Trim(), -1, clue.lines);
            }

        // 2. A special moment.
        if (!string.IsNullOrEmpty(state.pendingSpecial))
        {
            bool failed = state.pendingSpecial == SpecialFailedAudit;
            state.pendingSpecial = "";
            ConsultationVariant special = PickVariant(failed ? definition.failedAudit : definition.laterDay, state.lastLineId,
                failed ? state.seenFailedAudits : state.seenDay);
            if (special != null)
                return Said(state, failed ? ConsultationKind.FailedAudit : ConsultationKind.LaterDay, special.id.Trim(), -1, special.lines);
        }

        // 3. The visit ladder (the last step repeats).
        List<ConsultationStep> steps = UsableSteps(definition);
        if (steps.Count > 0)
        {
            int step = Math.Min(state.ladderVisits, steps.Count - 1);
            ConsultationVariant variant = PickVariant(steps[step].variants, state.lastLineId, state.ladderVisits);
            state.ladderVisits++;
            if (variant != null) return Said(state, ConsultationKind.Ladder, variant.id.Trim(), step, variant.lines);
        }

        // Nothing usable (or only a line just said): a short neutral line.
        bool authored = HasText(definition.fallback) && state.lastLineId != FallbackId;
        return Said(state, ConsultationKind.Fallback, FallbackId, -1, authored ? definition.fallback : null);
    }

    /// <summary>The clue's condition holds now (an unread document, an empty or wrong field).</summary>
    public static bool ClueFits(ConsultationClue clue, ConsultationContext context)
    {
        if (!Usable(clue)) return false;
        string when = (clue.when ?? "").Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(clue.document))
            return (when.Length == 0 || when == WhenUnread) && context.DocumentUnread != null && context.DocumentUnread(clue.document.Trim());
        if (context.Field == null) return false;
        ConsultationFieldState state = context.Field(clue.field.Trim());
        if (when.Length == 0 || when == WhenEmpty) return state == ConsultationFieldState.Empty;
        return when == WhenWrong && state == ConsultationFieldState.Wrong;
    }

    /// <summary>
    /// Authoring problems (empty list = fine): duplicate or missing ids, clues
    /// naming neither or both of a document and a field, unknown documents or
    /// field keys, unknown conditions, and any line containing an answer value.
    /// Unusable entries are skipped at play time; nothing here throws.
    /// </summary>
    public static List<string> Problems(ClientConsultationDefinition definition, ICollection<string> documentIds,
        ICollection<string> fieldKeys, IEnumerable<string> answerValues)
    {
        var problems = new List<string>();
        if (definition == null) return problems;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        void Id(string id, string where)
        {
            if (string.IsNullOrWhiteSpace(id)) problems.Add($"{where}: missing id");
            else if (!ids.Add(id.Trim())) problems.Add($"{where}: duplicate id '{id.Trim()}'");
        }

        if (definition.clues != null)
            for (int i = 0; i < definition.clues.Count; i++)
            {
                ConsultationClue clue = definition.clues[i];
                string where = $"clues[{i}]";
                if (clue == null) { problems.Add($"{where}: empty"); continue; }
                Id(clue.id, where);
                bool hasDocument = !string.IsNullOrWhiteSpace(clue.document), hasField = !string.IsNullOrWhiteSpace(clue.field);
                string when = (clue.when ?? "").Trim().ToLowerInvariant();
                if (hasDocument == hasField) problems.Add($"{where}: name exactly one of document or field");
                else if (hasDocument && documentIds != null && !documentIds.Contains(clue.document.Trim()))
                    problems.Add($"{where}: unknown document '{clue.document.Trim()}'");
                else if (hasField && fieldKeys != null && !fieldKeys.Contains(clue.field.Trim()))
                    problems.Add($"{where}: unknown field key '{clue.field.Trim()}'");
                if (hasDocument && when.Length > 0 && when != WhenUnread) problems.Add($"{where}: a document clue is 'unread'");
                if (hasField && when.Length > 0 && when != WhenEmpty && when != WhenWrong) problems.Add($"{where}: a field clue is 'empty' or 'wrong'");
                if (!HasText(clue.lines)) problems.Add($"{where}: no lines");
            }
        CheckVariants(definition.failedAudit, "failedAudit", Id, problems);
        CheckVariants(definition.laterDay, "laterDay", Id, problems);
        if (definition.ladder != null)
            for (int i = 0; i < definition.ladder.Count; i++)
                CheckVariants(definition.ladder[i]?.variants, $"ladder[{i}]", Id, problems);

        var answers = new List<string>();
        if (answerValues != null)
            foreach (string value in answerValues)
                if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > 2) answers.Add(value.Trim());
        foreach (string text in AllTexts(definition))
            foreach (string answer in answers)
                if (text.IndexOf(answer, StringComparison.OrdinalIgnoreCase) >= 0)
                    problems.Add($"a line gives the answer '{answer}': \"{text}\"");
        return problems;
    }

    /// <summary>Every authored line text (clues, specials, ladder, fallback).</summary>
    public static List<string> AllTexts(ClientConsultationDefinition definition)
    {
        var texts = new List<string>();
        if (definition == null) return texts;
        void Add(List<DialogueLineDefinition> lines)
        {
            if (lines == null) return;
            foreach (DialogueLineDefinition line in lines)
                if (line != null && !string.IsNullOrWhiteSpace(line.text)) texts.Add(line.text);
        }
        if (definition.clues != null) foreach (ConsultationClue clue in definition.clues) if (clue != null) Add(clue.lines);
        if (definition.failedAudit != null) foreach (ConsultationVariant v in definition.failedAudit) if (v != null) Add(v.lines);
        if (definition.laterDay != null) foreach (ConsultationVariant v in definition.laterDay) if (v != null) Add(v.lines);
        if (definition.ladder != null)
            foreach (ConsultationStep step in definition.ladder)
                if (step?.variants != null) foreach (ConsultationVariant v in step.variants) if (v != null) Add(v.lines);
        Add(definition.fallback);
        return texts;
    }

    /// <summary>True when the definition has anything to say (a usable clue, special, ladder step or fallback).</summary>
    public static bool HasContent(ClientConsultationDefinition definition)
    {
        if (definition == null || !definition.enabled) return false;
        if (UsableSteps(definition).Count > 0 || HasText(definition.fallback)) return true;
        if (definition.clues != null) foreach (ConsultationClue clue in definition.clues) if (Usable(clue)) return true;
        return Any(definition.failedAudit) || Any(definition.laterDay);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static ConsultationPick Said(Level1ConsultationRecord state, ConsultationKind kind, string id, int step, List<DialogueLineDefinition> lines)
    {
        state.lastLineId = id;
        return new ConsultationPick { Kind = kind, Id = id, Step = step, Lines = lines };
    }

    /// <summary>A usable variant other than the line just said, rotating by seed; null when none.</summary>
    private static ConsultationVariant PickVariant(List<ConsultationVariant> variants, string lastLineId, int seed)
    {
        if (variants == null) return null;
        var candidates = new List<ConsultationVariant>();
        foreach (ConsultationVariant v in variants)
            if (Usable(v) && v.id.Trim() != lastLineId) candidates.Add(v);
        if (candidates.Count == 0) return null;
        return candidates[Math.Abs(seed) % candidates.Count];
    }

    private static List<ConsultationStep> UsableSteps(ClientConsultationDefinition definition)
    {
        var steps = new List<ConsultationStep>();
        if (definition?.ladder != null)
            foreach (ConsultationStep step in definition.ladder)
                if (step != null && Any(step.variants)) steps.Add(step);
        return steps;
    }

    private static void CheckVariants(List<ConsultationVariant> variants, string where, Action<string, string> id, List<string> problems)
    {
        if (variants == null) return;
        for (int i = 0; i < variants.Count; i++)
        {
            ConsultationVariant v = variants[i];
            if (v == null) { problems.Add($"{where}[{i}]: empty"); continue; }
            id(v.id, $"{where}[{i}]");
            if (!HasText(v.lines)) problems.Add($"{where}[{i}]: no lines");
        }
    }

    private static bool Any(List<ConsultationVariant> variants)
    {
        if (variants != null) foreach (ConsultationVariant v in variants) if (Usable(v)) return true;
        return false;
    }

    private static bool Usable(ConsultationVariant v) => v != null && !string.IsNullOrWhiteSpace(v.id) && HasText(v.lines);

    private static bool Usable(ConsultationClue clue) =>
        clue != null && !string.IsNullOrWhiteSpace(clue.id) && HasText(clue.lines) &&
        string.IsNullOrWhiteSpace(clue.document) != string.IsNullOrWhiteSpace(clue.field);

    private static bool HasText(List<DialogueLineDefinition> lines)
    {
        if (lines != null) foreach (DialogueLineDefinition line in lines) if (line != null && !string.IsNullOrWhiteSpace(line.text)) return true;
        return false;
    }
}
