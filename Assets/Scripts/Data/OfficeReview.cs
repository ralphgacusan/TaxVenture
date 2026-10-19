using System;
using System.Collections.Generic;

/// <summary>One line of the player-reviewed schedule: the record line they chose and its amount.</summary>
[Serializable]
public class ScheduleRowState
{
    public string semanticKey;
    public string documentId;
    public string label;
    public float amount;
    public List<string> moneyIds = new List<string>();
}

/// <summary>One office finding as the player recorded it: part id -> chosen line/reason/conclusion.</summary>
[Serializable]
public class OfficeFindingState
{
    public string id;
    public List<OfficePartState> parts = new List<OfficePartState>();

    public OfficePartState Find(string partId)
    {
        if (parts == null || string.IsNullOrEmpty(partId)) return null;
        foreach (OfficePartState part in parts)
            if (part != null && part.partId == partId) return part;
        return null;
    }
}

[Serializable]
public class OfficePartState
{
    public string partId;
    public string documentId;
    /// <summary>Semantic key (record line) or reason / conclusion id.</summary>
    public string key;
    public string label;
}

/// <summary>
/// PURPOSE:
/// The office reconciliation (Level 2, T07), pure C#. The player builds a
/// reviewed May schedule from record lines and fills the configured findings;
/// the original prepared summary (S1) is never edited.
///
/// RULES:
/// - Totals are the sum of the PLAYER's chosen lines (plus the supplied prior
///   verified aggregate for the year); never from the answer key.
/// - The same money can never be scheduled twice: a line whose payments are
///   already covered (a cash receipt inside a monthly total, a bank credit of
///   a collected payment) is refused with the line that already holds it.
/// - Any record line may be chosen, including wrong ones (e.g. a loan): a
///   complete wrong review stands until the Auditor reviews it.
/// - Every real change counts as one packet revision; reading pages does not.
/// - Evaluate() names configured root causes once each (omission, missing
///   income, loan counted, unsupported finding) for the Auditor.
/// </summary>
public static class OfficeReview
{
    public const string RootOmission = "omission";
    public const string RootMissingIncome = "missing-income";
    public const string RootLoanCounted = "not-income-counted";
    public const string RootDoubleCounted = "double-counted";
    public const string RootF1Unsupported = "f1-unsupported";
    public const string RootF2Unexplained = "f2-unexplained";
    public const string RootF2FalseFlag = "f2-false-flag";

    /// <summary>Unity's JsonUtility restores null list items as blank objects: treat both as empty.</summary>
    public static bool IsEmpty(ScheduleRowState row) => row == null || string.IsNullOrEmpty(row.semanticKey);

    public static bool CanEdit(CaseReviewState state) =>
        state != null && state.phase >= ReviewPhase.OfficeReconciliation &&
        state.phase != ReviewPhase.Audit && state.phase != ReviewPhase.Completed;

    // =========================================================
    // SCHEDULE
    // =========================================================

    /// <summary>The scheduled row that already covers some of this source's money (null when none).</summary>
    public static ScheduleRowState Overlap(CaseReviewState state, ScheduleSourceDefinition source)
    {
        if (state?.schedule == null || source?.moneyIds == null) return null;
        foreach (ScheduleRowState row in state.schedule)
        {
            if (IsEmpty(row) || row.moneyIds == null) continue;
            foreach (string money in source.moneyIds)
                if (row.moneyIds.Contains(money)) return row;
        }
        return null;
    }

    /// <summary>
    /// Puts a record line into the schedule at <paramref name="index"/>
    /// (replacing what was there). False when editing is closed, the line is
    /// not schedulable, or its money is already counted by another row.
    /// </summary>
    public static bool SetScheduleRow(CaseReviewState state, CaseReviewDefinition review, int index,
        string semanticKey, string documentId, string label, out ScheduleRowState blockedBy)
    {
        blockedBy = null;
        if (!CanEdit(state) || review == null) return false;
        ScheduleSourceDefinition source = review.FindScheduleSource(semanticKey);
        int slots = Math.Max(1, review.scheduleSlots);
        if (source == null || index < 0 || index >= slots) return false;
        if (state.schedule == null) state.schedule = new List<ScheduleRowState>();
        while (state.schedule.Count < slots) state.schedule.Add(null);

        ScheduleRowState current = state.schedule[index];
        if (!IsEmpty(current) && current.semanticKey == semanticKey) return false;
        // Check overlap against every other row (the replaced one does not count).
        state.schedule[index] = null;
        blockedBy = Overlap(state, source);
        if (blockedBy != null)
        {
            state.schedule[index] = current;
            return false;
        }
        state.schedule[index] = new ScheduleRowState
        {
            semanticKey = semanticKey,
            documentId = documentId,
            label = label,
            amount = source.amount,
            moneyIds = new List<string>(source.moneyIds ?? new List<string>())
        };
        state.packetRevision++;
        return true;
    }

    /// <summary>
    /// Brings a saved schedule in line with the current case: rows whose
    /// source this case no longer offers (an older checkpoint's record lines)
    /// and rows past the last schedule line are emptied, so nothing hidden
    /// still counts. One packet revision when anything changed.
    /// </summary>
    public static bool DropRetiredRows(CaseReviewState state, CaseReviewDefinition review)
    {
        if (state?.schedule == null || review == null || review.scheduleSources == null || review.scheduleSources.Count == 0) return false;
        int slots = Math.Max(1, review.scheduleSlots);
        bool changed = false;
        for (int i = state.schedule.Count - 1; i >= 0; i--)
        {
            ScheduleRowState row = state.schedule[i];
            bool retired = !IsEmpty(row) && review.FindScheduleSource(row.semanticKey) == null;
            if (i >= slots)
            {
                changed |= !IsEmpty(row);
                state.schedule.RemoveAt(i);
            }
            else if (retired)
            {
                state.schedule[i] = null;
                changed = true;
            }
        }
        if (changed) state.packetRevision++;
        return changed;
    }

    public static bool ClearScheduleRow(CaseReviewState state, int index)
    {
        if (!CanEdit(state) || state.schedule == null || index < 0 || index >= state.schedule.Count || IsEmpty(state.schedule[index]))
            return false;
        state.schedule[index] = null;
        state.packetRevision++;
        return true;
    }

    public static float MayTotal(CaseReviewState state)
    {
        float sum = 0f;
        if (state?.schedule != null)
            foreach (ScheduleRowState row in state.schedule)
                if (!IsEmpty(row)) sum += row.amount;
        return sum;
    }

    public static float AnnualTotal(CaseReviewState state, CaseReviewDefinition review) =>
        (review != null ? review.priorVerifiedAggregate : 0f) + MayTotal(state);

    public static int ScheduledCount(CaseReviewState state)
    {
        int count = 0;
        if (state?.schedule != null) foreach (ScheduleRowState row in state.schedule) if (!IsEmpty(row)) count++;
        return count;
    }

    // =========================================================
    // FINDINGS
    // =========================================================

    public static OfficeFindingState Finding(CaseReviewState state, string findingId, bool create)
    {
        if (state == null || string.IsNullOrEmpty(findingId)) return null;
        if (state.officeFindings == null) state.officeFindings = new List<OfficeFindingState>();
        foreach (OfficeFindingState f in state.officeFindings)
            if (f != null && f.id == findingId) return f;
        if (!create) return null;
        var created = new OfficeFindingState { id = findingId };
        state.officeFindings.Add(created);
        return created;
    }

    /// <summary>Sets one part of an office finding. A real change is one packet revision.</summary>
    public static bool SetPart(CaseReviewState state, CaseReviewDefinition review, string findingId, string partId,
        string documentId, string key, string label)
    {
        if (!CanEdit(state) || review == null || string.IsNullOrEmpty(key)) return false;
        OfficeFindingDefinition definition = review.FindOfficeFinding(findingId);
        if (definition?.parts == null || !definition.parts.Exists(p => p != null && p.id == partId)) return false;
        OfficeFindingState finding = Finding(state, findingId, true);
        OfficePartState part = finding.Find(partId);
        if (part != null && part.key == key && part.documentId == documentId) return false;
        if (part == null)
        {
            part = new OfficePartState { partId = partId };
            finding.parts.Add(part);
        }
        part.documentId = documentId;
        part.key = key;
        part.label = label;
        state.packetRevision++;
        return true;
    }

    public static bool ClearPart(CaseReviewState state, string findingId, string partId)
    {
        if (!CanEdit(state)) return false;
        OfficeFindingState finding = Finding(state, findingId, false);
        OfficePartState part = finding?.Find(partId);
        if (part == null) return false;
        finding.parts.Remove(part);
        state.packetRevision++;
        return true;
    }

    public static bool FindingComplete(CaseReviewState state, OfficeFindingDefinition definition)
    {
        OfficeFindingState finding = Finding(state, definition?.id, false);
        if (finding == null || definition.parts == null) return false;
        foreach (OfficeFindingPartDefinition part in definition.parts)
            if (part != null && string.IsNullOrEmpty(finding.Find(part.id)?.key)) return false;
        return true;
    }

    /// <summary>Structurally complete: at least one schedule line and every finding part filled (no correctness).</summary>
    public static bool StructurallyComplete(CaseReviewState state, CaseReviewDefinition review)
    {
        if (state == null || review == null || ScheduledCount(state) == 0) return false;
        if (review.officeFindings != null)
            foreach (OfficeFindingDefinition finding in review.officeFindings)
                if (finding != null && !FindingComplete(state, finding)) return false;
        return true;
    }

    // =========================================================
    // EVALUATION (for the Auditor, T09; never shown as a sparkle)
    // =========================================================

    public sealed class Result
    {
        public float MayTotal;
        public float AnnualTotal;
        public readonly List<string> RootCauses = new List<string>();
        public bool Supported => RootCauses.Count == 0;
    }

    public static Result Evaluate(CaseReviewState state, CaseReviewDefinition review)
    {
        var result = new Result { MayTotal = MayTotal(state), AnnualTotal = AnnualTotal(state, review) };
        OfficeReviewRuleDefinition rule = review?.officeRule;
        if (state == null || rule == null || !rule.IsSet) return result;

        var covered = new List<string>();
        bool doubled = false;
        if (state.schedule != null)
            foreach (ScheduleRowState row in state.schedule)
                if (!IsEmpty(row) && row.moneyIds != null)
                    foreach (string money in row.moneyIds)
                    {
                        if (covered.Contains(money)) doubled = true;
                        else covered.Add(money);
                    }

        bool omitted = !string.IsNullOrEmpty(rule.omittedMoney) && !covered.Contains(rule.omittedMoney);
        if (omitted) Add(result, RootOmission);
        foreach (string money in rule.requiredMoney)
            if (money != rule.omittedMoney && !covered.Contains(money)) Add(result, RootMissingIncome + ":" + money);
        if (rule.forbiddenMoney != null)
            foreach (string money in rule.forbiddenMoney)
                if (covered.Contains(money)) Add(result, RootLoanCounted + ":" + money);
        if (doubled) Add(result, RootDoubleCounted);

        if (rule.findings != null)
            foreach (OfficeFindingRuleDefinition findingRule in rule.findings)
                EvaluateFinding(state, findingRule, covered, omitted, result);
        return result;
    }

    private static void EvaluateFinding(CaseReviewState state, OfficeFindingRuleDefinition rule, List<string> covered,
        bool omitted, Result result)
    {
        if (rule == null || string.IsNullOrEmpty(rule.findingId)) return;
        OfficeFindingState finding = Finding(state, rule.findingId, false);
        string root = !string.IsNullOrEmpty(rule.rootCause) ? rule.rootCause : "finding-unsupported:" + rule.findingId;

        // A conclusion that holds or adjusts an explained difference is a false flag (its own root).
        OfficePartState conclusion = finding?.Find("conclusion");
        if (conclusion != null && rule.falseFlagConclusions != null && rule.falseFlagConclusions.Contains(conclusion.key))
        {
            Add(result, RootF2FalseFlag);
            return;
        }

        bool supported = finding != null && PartsMatch(finding, rule);
        if (!string.IsNullOrEmpty(rule.needsScheduleMoney) && !covered.Contains(rule.needsScheduleMoney))
        {
            // The same defect as the omission: counted once.
            if (omitted) return;
            supported = false;
        }
        if (!supported) Add(result, root);
    }

    private static bool PartsMatch(OfficeFindingState finding, OfficeFindingRuleDefinition rule)
    {
        if (rule.parts == null) return true;
        var unordered = rule.unorderedParts ?? new List<string>();
        foreach (OfficePartRuleDefinition part in rule.parts)
        {
            if (part == null || unordered.Contains(part.partId)) continue;
            string key = finding.Find(part.partId)?.key;
            if (string.IsNullOrEmpty(key) || part.accepted == null || !part.accepted.Contains(key)) return false;
        }
        if (unordered.Count > 0)
        {
            // Each unordered answer must fit a different unordered rule.
            var rules = rule.parts.FindAll(p => p != null && unordered.Contains(p.partId));
            var keys = new List<string>();
            foreach (string id in unordered) keys.Add(finding.Find(id)?.key);
            if (keys.Exists(string.IsNullOrEmpty)) return false;
            if (!Assign(keys, rules, 0, new bool[rules.Count])) return false;
        }
        return true;
    }

    private static bool Assign(List<string> keys, List<OfficePartRuleDefinition> rules, int index, bool[] used)
    {
        if (index == keys.Count) return true;
        for (int r = 0; r < rules.Count; r++)
        {
            if (used[r] || rules[r].accepted == null || !rules[r].accepted.Contains(keys[index])) continue;
            used[r] = true;
            if (Assign(keys, rules, index + 1, used)) return true;
            used[r] = false;
        }
        return false;
    }

    private static void Add(Result result, string root)
    {
        if (!result.RootCauses.Contains(root)) result.RootCauses.Add(root);
    }
}
