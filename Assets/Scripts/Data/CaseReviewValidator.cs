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
