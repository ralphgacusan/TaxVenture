using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Checks a case's optional review data (CaseReviewDefinition) before the
/// case is allowed to load: stable IDs, no dangling or duplicate IDs,
/// starting pages that exist, and summary pages whose income rows add up
/// to their subtotal. Pure C# (JsonCaseLoader and the regression suite
/// both call it). An ordinary case without review data has no problems.
/// </summary>
public static class CaseReviewValidator
{
    public static readonly string[] Kinds = { "cover", "summary", "note", "record" };
    public static readonly string[] Roles = { "info", "income", "subtotal", "memo" };
    public static readonly string[] ContentStatuses = { "verified", "educational-draft" };

    /// <summary>
    /// Every review page must open an existing case document (its documentName);
    /// no two pages may open the same one.
    /// </summary>
    public static List<string> ValidateDocumentLinks(CaseReviewDefinition review, IEnumerable<string> caseDocumentNames)
    {
        var problems = new List<string>();
        if (review == null || !review.IsReview || review.documents == null) return problems;
        var names = new HashSet<string>(caseDocumentNames ?? new string[0], StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (ReviewDocumentDefinition document in review.documents)
        {
            if (document == null) continue;
            string where = $"review '{review.reviewId}', document '{document.id}'";
            if (string.IsNullOrWhiteSpace(document.documentName))
                problems.Add($"{where}: documentName is empty");
            else if (!names.Contains(document.documentName))
                problems.Add($"{where}: documentName '{document.documentName}' is not one of the case documents");
            else if (!used.Add(document.documentName))
                problems.Add($"{where}: documentName '{document.documentName}' is used by two pages");
        }
        return problems;
    }

    /// <summary>Every problem found (empty list = valid).</summary>
    public static List<string> Validate(CaseReviewDefinition review)
    {
        var problems = new List<string>();
        if (review == null || !review.IsReview) return problems;

        string tag = $"review '{review.reviewId}'";
        if (review.priorVerifiedAggregate < 0f) problems.Add($"{tag}: priorVerifiedAggregate is negative");
        if (string.IsNullOrWhiteSpace(review.focusPeriod)) problems.Add($"{tag}: focusPeriod is empty");
        if (string.IsNullOrWhiteSpace(review.contentStatus) || Array.IndexOf(ContentStatuses, review.contentStatus) < 0)
            problems.Add($"{tag}: contentStatus must be one of {string.Join(", ", ContentStatuses)}");
        if (!string.IsNullOrWhiteSpace(review.startPhase) &&
            (!Enum.TryParse(review.startPhase.Trim(), out ReviewPhase start) || !Enum.IsDefined(typeof(ReviewPhase), start)))
            problems.Add($"{tag}: unknown startPhase '{review.startPhase}'");

        var documentIds = new HashSet<string>(StringComparer.Ordinal);
        if (review.documents == null || review.documents.Count == 0)
            problems.Add($"{tag}: has no documents");
        else
            foreach (ReviewDocumentDefinition document in review.documents)
                CheckDocument(tag, document, documentIds, problems);

        var reasonIds = new HashSet<string>(StringComparer.Ordinal);
        if (review.reasons != null)
            foreach (ReviewReasonDefinition reason in review.reasons)
            {
                if (reason == null || string.IsNullOrWhiteSpace(reason.id) || string.IsNullOrWhiteSpace(reason.label))
                    problems.Add($"{tag}: every reason needs an id and a label");
                else if (!reasonIds.Add(reason.id))
                    problems.Add($"{tag}: reason id '{reason.id}' used twice");
            }

        if (review.handoverDocumentIds != null)
            foreach (string id in review.handoverDocumentIds)
            {
                if (!documentIds.Contains(id ?? string.Empty))
                    problems.Add($"{tag}: handover document '{id}' does not exist");
                else if (review.initialDocumentIds != null && review.initialDocumentIds.Contains(id))
                    problems.Add($"{tag}: handover document '{id}' is already held at the start");
            }
        if (review.handoverDocumentIds != null && review.handoverDocumentIds.Count > 0 &&
            (review.stall == null || review.stall.conversationLines == null || review.stall.conversationLines.Count == 0))
            problems.Add($"{tag}: a handover needs stall.conversationLines");

        PreliminaryRuleDefinition rule = review.preliminaryRule;
        if (rule != null && rule.IsSet)
        {
            if (!documentIds.Contains(rule.concernDocument ?? string.Empty))
                problems.Add($"{tag}: preliminaryRule.concernDocument '{rule.concernDocument}' does not exist");
            if (!documentIds.Contains(rule.evidenceDocument ?? string.Empty))
                problems.Add($"{tag}: preliminaryRule.evidenceDocument '{rule.evidenceDocument}' does not exist");
            if (!reasonIds.Contains(rule.reasonId ?? string.Empty))
                problems.Add($"{tag}: preliminaryRule.reasonId '{rule.reasonId}' is not one of the reasons");
        }

        CheckAnnotation(tag, review.annotation, problems);
        CheckAnnotationKey(tag, review, problems);
        CheckOfficeReview(tag, review, reasonIds, problems);

        if (review.initialDocumentIds == null || review.initialDocumentIds.Count == 0)
            problems.Add($"{tag}: initialDocumentIds is empty");
        else
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in review.initialDocumentIds)
            {
                if (!documentIds.Contains(id ?? string.Empty)) problems.Add($"{tag}: initial document '{id}' does not exist");
                if (!seen.Add(id ?? string.Empty)) problems.Add($"{tag}: initial document '{id}' listed twice");
            }
        }
        return problems;
    }

    /// <summary>Toolkit options and entries: ids, labels, and a demonstration that uses only its own entry's options.</summary>
    private static void CheckAnnotation(string tag, AnnotationDefinition annotation, List<string> problems)
    {
        if (annotation == null) return;
        CheckOptions($"{tag}, annotation typeOptions", annotation.typeOptions, problems);
        CheckOptions($"{tag}, annotation treatmentOptions", annotation.treatmentOptions, problems);
        var entryIds = new HashSet<string>(StringComparer.Ordinal);
        var entries = new List<AnnotationEntryDefinition>();
        if (annotation.HasTutorial) entries.Add(annotation.tutorial);
        if (annotation.entries != null) entries.AddRange(annotation.entries);
        if (entries.Count > 0 && (annotation.typeOptions == null || annotation.typeOptions.Count == 0 ||
                                  annotation.treatmentOptions == null || annotation.treatmentOptions.Count == 0))
            problems.Add($"{tag}: annotation entries need typeOptions and treatmentOptions");
        foreach (AnnotationEntryDefinition entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
            {
                problems.Add($"{tag}: an annotation entry has no id");
                continue;
            }
            string where = $"{tag}, annotation entry '{entry.id}'";
            if (!entryIds.Add(entry.id)) problems.Add($"{where}: id used twice");
            if (string.IsNullOrWhiteSpace(entry.title)) problems.Add($"{where}: title is empty");
            if (entry.lines == null || entry.lines.Count == 0) problems.Add($"{where}: has no lines");
            var lineIds = new HashSet<string>(StringComparer.Ordinal);
            if (entry.lines != null)
                foreach (ReviewLineDefinition line in entry.lines)
                    if (line == null || string.IsNullOrWhiteSpace(line.id) || !lineIds.Add(line.id))
                        problems.Add($"{where}: every line needs a unique id");
            CheckOptions($"{where}, evidenceOptions", entry.evidenceOptions, problems);
            if (entry.evidenceOptions == null || entry.evidenceOptions.Count == 0) problems.Add($"{where}: has no evidenceOptions");
            if (entry.maxEvidence < 1 || entry.maxEvidence > 2) problems.Add($"{where}: maxEvidence must be 1 or 2");
        }
        if (!annotation.HasTutorial) return;
        AnnotationEntryDefinition tutorial = annotation.tutorial;
        AnnotationDemoDefinition demo = tutorial.demo;
        string t = $"{tag}, annotation tutorial '{tutorial.id}'";
        if (demo == null || !demo.IsSet)
        {
            problems.Add($"{t}: demo needs highlightLineId, typeId, treatmentId and evidenceIds");
            return;
        }
        if (tutorial.lines == null || !tutorial.lines.Exists(l => l != null && l.id == demo.highlightLineId))
            problems.Add($"{t}: demo.highlightLineId '{demo.highlightLineId}' is not one of its lines");
        if (annotation.FindOption(AnnotationSlot.Type, demo.typeId, tutorial) == null)
            problems.Add($"{t}: demo.typeId '{demo.typeId}' is not a type option");
        if (annotation.FindOption(AnnotationSlot.Treatment, demo.treatmentId, tutorial) == null)
            problems.Add($"{t}: demo.treatmentId '{demo.treatmentId}' is not a treatment option");
        foreach (string id in demo.evidenceIds)
            if (annotation.FindOption(AnnotationSlot.Evidence, id, tutorial) == null)
                problems.Add($"{t}: demo evidence '{id}' is not one of its evidenceOptions");
        if (demo.evidenceIds.Count > Math.Max(1, tutorial.maxEvidence))
            problems.Add($"{t}: demo clips more evidence than maxEvidence");
    }

    /// <summary>One key per scored entry, naming that entry's own options, with hints for every note place.</summary>
    private static void CheckAnnotationKey(string tag, CaseReviewDefinition review, List<string> problems)
    {
        AnnotationDefinition annotation = review.annotation;
        var entryIds = new HashSet<string>(StringComparer.Ordinal);
        if (annotation != null && annotation.entries != null)
            foreach (AnnotationEntryDefinition entry in annotation.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id)) continue;
                entryIds.Add(entry.id);
                AnnotationKeyDefinition key = review.FindKey(entry.id);
                string where = $"{tag}, annotationKey '{entry.id}'";
                if (key == null)
                {
                    problems.Add($"{tag}: annotation entry '{entry.id}' has no annotationKey");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(key.highlightLineId) || entry.lines == null ||
                    !entry.lines.Exists(line => line != null && line.id == key.highlightLineId))
                    problems.Add($"{where}: highlightLineId '{key.highlightLineId}' is not one of the entry's printed lines");
                if (annotation.FindOption(AnnotationSlot.Type, key.typeId, entry) == null)
                    problems.Add($"{where}: typeId '{key.typeId}' is not a type option");
                if (annotation.FindOption(AnnotationSlot.Treatment, key.treatmentId, entry) == null)
                    problems.Add($"{where}: treatmentId '{key.treatmentId}' is not a treatment option");
                if (key.evidenceIds == null || key.evidenceIds.Count == 0)
                    problems.Add($"{where}: needs evidenceIds");
                else
                {
                    foreach (string id in key.evidenceIds)
                        if (annotation.FindOption(AnnotationSlot.Evidence, id, entry) == null)
                            problems.Add($"{where}: evidence '{id}' is not one of the entry's evidenceOptions");
                    if (key.evidenceIds.Count > Math.Max(1, entry.maxEvidence))
                        problems.Add($"{where}: more evidence than the entry's clip holds");
                }
                foreach (AnnotationSlot slot in IncomeChallenge.ScoredSlots)
                {
                    List<string> hints = key.HintsFor(slot);
                    if (hints == null || hints.Count == 0 || hints.Exists(string.IsNullOrWhiteSpace))
                        problems.Add($"{where}: needs {slot} hints");
                }
            }
        if (review.annotationKey != null)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (AnnotationKeyDefinition key in review.annotationKey)
            {
                if (key == null || string.IsNullOrWhiteSpace(key.entryId) || !entryIds.Contains(key.entryId))
                    problems.Add($"{tag}: annotationKey '{key?.entryId}' has no matching annotation entry");
                else if (!seen.Add(key.entryId))
                    problems.Add($"{tag}: annotationKey '{key.entryId}' listed twice");
            }
        }
    }

    /// <summary>Schedule sources, office finding pages and the office rule refer only to things that exist.</summary>
    private static void CheckOfficeReview(string tag, CaseReviewDefinition review, HashSet<string> reasonIds, List<string> problems)
    {
        var conclusionIds = new HashSet<string>(StringComparer.Ordinal);
        if (review.conclusions != null)
            foreach (ReviewConclusionDefinition c in review.conclusions)
            {
                if (c == null || string.IsNullOrWhiteSpace(c.id) || string.IsNullOrWhiteSpace(c.label)) problems.Add($"{tag}: every conclusion needs an id and a label");
                else if (!conclusionIds.Add(c.id)) problems.Add($"{tag}: conclusion '{c.id}' used twice");
            }
        var sourceKeys = new HashSet<string>(StringComparer.Ordinal);
        if (review.scheduleSources != null)
            foreach (ScheduleSourceDefinition source in review.scheduleSources)
            {
                if (source == null || string.IsNullOrWhiteSpace(source.semanticKey)) { problems.Add($"{tag}: a schedule source has no semanticKey"); continue; }
                if (!sourceKeys.Add(source.semanticKey)) problems.Add($"{tag}: schedule source '{source.semanticKey}' listed twice");
                if (source.amount < 0f) problems.Add($"{tag}: schedule source '{source.semanticKey}' has a negative amount");
                if (source.moneyIds == null || source.moneyIds.Count == 0) problems.Add($"{tag}: schedule source '{source.semanticKey}' names no money");
            }
        if (review.scheduleSources != null && review.scheduleSources.Count > 0 && (review.scheduleSlots < 1 || review.scheduleSlots > 5))
            problems.Add($"{tag}: scheduleSlots must be 1 to 5");
        if (review.scheduleSlotLabels != null && review.scheduleSlotLabels.Count > 0 && review.scheduleSlotLabels.Count != review.scheduleSlots)
            problems.Add($"{tag}: scheduleSlotLabels must name every schedule line ({review.scheduleSlots})");
        if (review.scheduleSources != null)
            foreach (ScheduleSourceDefinition source in review.scheduleSources)
            {
                if (source == null || string.IsNullOrWhiteSpace(source.entryId)) continue;
                if (review.annotation?.entries == null || !review.annotation.entries.Exists(e => e != null && e.id == source.entryId))
                    problems.Add($"{tag}: schedule source '{source.semanticKey}' names unknown annotation entry '{source.entryId}'");
                else if (review.scheduleSources.FindAll(x => x != null && x.entryId == source.entryId).Count > 1)
                    problems.Add($"{tag}: annotation entry '{source.entryId}' supplies more than one schedule source");
            }

        var findingParts = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        if (review.officeFindings != null)
            foreach (OfficeFindingDefinition finding in review.officeFindings)
            {
                if (finding == null || string.IsNullOrWhiteSpace(finding.id)) { problems.Add($"{tag}: an office finding has no id"); continue; }
                if (findingParts.ContainsKey(finding.id)) { problems.Add($"{tag}: office finding '{finding.id}' listed twice"); continue; }
                var parts = new Dictionary<string, string>(StringComparer.Ordinal);
                findingParts[finding.id] = parts;
                if (finding.parts == null || finding.parts.Count == 0 || finding.parts.Count > 5)
                    problems.Add($"{tag}, office finding '{finding.id}': needs 1 to 5 parts");
                else
                    foreach (OfficeFindingPartDefinition part in finding.parts)
                    {
                        if (part == null || string.IsNullOrWhiteSpace(part.id) || parts.ContainsKey(part.id))
                        { problems.Add($"{tag}, office finding '{finding.id}': every part needs a unique id"); continue; }
                        if (part.accepts != OfficeFindingPartDefinition.Record && part.accepts != OfficeFindingPartDefinition.Reason &&
                            part.accepts != OfficeFindingPartDefinition.Conclusion)
                            problems.Add($"{tag}, office finding '{finding.id}', part '{part.id}': accepts must be record, reason or conclusion");
                        parts[part.id] = part.accepts;
                    }
            }

        OfficeReviewRuleDefinition rule = review.officeRule;
        if (rule == null || !rule.IsSet) return;
        var money = new HashSet<string>(StringComparer.Ordinal);
        if (review.scheduleSources != null)
            foreach (ScheduleSourceDefinition source in review.scheduleSources)
                if (source?.moneyIds != null) foreach (string id in source.moneyIds) money.Add(id);
        foreach (string id in rule.requiredMoney) if (!money.Contains(id)) problems.Add($"{tag}: officeRule required money '{id}' is in no schedule source");
        if (rule.forbiddenMoney != null) foreach (string id in rule.forbiddenMoney) if (!money.Contains(id)) problems.Add($"{tag}: officeRule forbidden money '{id}' is in no schedule source");
        if (!string.IsNullOrEmpty(rule.omittedMoney) && !rule.requiredMoney.Contains(rule.omittedMoney))
            problems.Add($"{tag}: officeRule omittedMoney must be one of requiredMoney");
        if (rule.findings != null)
            foreach (OfficeFindingRuleDefinition f in rule.findings)
            {
                if (f == null || !findingParts.TryGetValue(f.findingId ?? string.Empty, out var parts))
                { problems.Add($"{tag}: officeRule finding '{f?.findingId}' has no office finding page"); continue; }
                if (string.IsNullOrWhiteSpace(f.rootCause)) problems.Add($"{tag}: officeRule finding '{f.findingId}' needs a rootCause");
                if (f.parts != null)
                    foreach (OfficePartRuleDefinition p in f.parts)
                    {
                        if (p == null || !parts.TryGetValue(p.partId ?? string.Empty, out string accepts))
                        { problems.Add($"{tag}: officeRule '{f.findingId}' part '{p?.partId}' is not on its page"); continue; }
                        foreach (string a in p.accepted ?? new List<string>())
                            if ((accepts == OfficeFindingPartDefinition.Reason && !reasonIds.Contains(a)) ||
                                (accepts == OfficeFindingPartDefinition.Conclusion && !conclusionIds.Contains(a)))
                                problems.Add($"{tag}: officeRule '{f.findingId}' part '{p.partId}' accepts unknown '{a}'");
                    }
                foreach (string c in f.falseFlagConclusions ?? new List<string>())
                    if (!conclusionIds.Contains(c)) problems.Add($"{tag}: officeRule '{f.findingId}' false flag '{c}' is not a conclusion");
                if (!string.IsNullOrEmpty(f.needsScheduleMoney) && !money.Contains(f.needsScheduleMoney))
                    problems.Add($"{tag}: officeRule '{f.findingId}' needs unknown money '{f.needsScheduleMoney}'");
            }
    }

    private static void CheckOptions(string where, List<AnnotationOptionDefinition> options, List<string> problems)
    {
        if (options == null) return;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (AnnotationOptionDefinition option in options)
        {
            if (option == null || string.IsNullOrWhiteSpace(option.id) || string.IsNullOrWhiteSpace(option.label))
                problems.Add($"{where}: every option needs an id and a label");
            else if (!ids.Add(option.id))
                problems.Add($"{where}: option id '{option.id}' used twice");
        }
    }

    private static void CheckDocument(string tag, ReviewDocumentDefinition document,
        HashSet<string> documentIds, List<string> problems)
    {
        if (document == null)
        {
            problems.Add($"{tag}: contains a null document");
            return;
        }
        if (string.IsNullOrWhiteSpace(document.id))
        {
            problems.Add($"{tag}: a document has no id");
            return;
        }
        string where = $"{tag}, document '{document.id}'";
        if (!documentIds.Add(document.id)) problems.Add($"{where}: id used twice");
        if (string.IsNullOrWhiteSpace(document.title)) problems.Add($"{where}: title is empty");
        if (Array.IndexOf(Kinds, document.kind) < 0)
            problems.Add($"{where}: kind '{document.kind}' must be one of {string.Join(", ", Kinds)}");

        if (document.lines == null || document.lines.Count == 0)
        {
            problems.Add($"{where}: has no lines");
            return;
        }

        var lineIds = new HashSet<string>(StringComparer.Ordinal);
        float incomeSum = 0f;
        int subtotals = 0;
        float subtotal = 0f;
        foreach (ReviewLineDefinition line in document.lines)
        {
            if (line == null)
            {
                problems.Add($"{where}: contains a null line");
                continue;
            }
            if (string.IsNullOrWhiteSpace(line.id)) problems.Add($"{where}: a line has no id");
            else if (!lineIds.Add(line.id)) problems.Add($"{where}: line id '{line.id}' used twice");
            if (string.IsNullOrWhiteSpace(line.label) || string.IsNullOrWhiteSpace(line.value))
                problems.Add($"{where}, line '{line.id}': label and value must both print something");
            if (line.amount < 0f) problems.Add($"{where}, line '{line.id}': amount is negative");

            string role = string.IsNullOrWhiteSpace(line.role) ? "info" : line.role;
            if (Array.IndexOf(Roles, role) < 0)
                problems.Add($"{where}, line '{line.id}': role '{line.role}' must be one of {string.Join(", ", Roles)}");
            if (role == "income") incomeSum += line.amount;
            if (role == "subtotal")
            {
                subtotals++;
                subtotal = line.amount;
            }
        }

        if (document.kind == "summary")
        {
            if (subtotals != 1) problems.Add($"{where}: a summary needs exactly one subtotal line (found {subtotals})");
            else if (Math.Abs(incomeSum - subtotal) > 0.005f)
                problems.Add($"{where}: income lines add up to {incomeSum:0.##}, not the printed subtotal {subtotal:0.##}");
        }
        else if (subtotals > 0)
            problems.Add($"{where}: only summary pages may have a subtotal line");
    }
}
