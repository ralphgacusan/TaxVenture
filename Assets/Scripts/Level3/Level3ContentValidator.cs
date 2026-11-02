using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// PURPOSE:
/// Load-time check of a Level 3 case's content block (Level3 T02,
/// FIXTURE_CONTRACT "Load validation"), pure C# with exact decimals.
/// JsonCaseLoader refuses a Level_03 case with any problem, so a broken or
/// missing block fails closed instead of becoming a placeholder case.
///
/// Checks: requested/internal case identity, schema/content revision, annual
/// period containment, unique ids, source -> document and row -> semanticKey
/// bindings with printed amounts agreeing exactly, money syntax and bounds,
/// support/claim/subset edges (owner, shared transaction, amount, no cycles),
/// totals over their own rows, credit links, contiguous rule bands,
/// explicit corporate facts, the twelve Bea decisions and the expected
/// chains. It never grades player work (T06/T07/T12 own that).
/// </summary>
public static class Level3ContentValidator
{
    public const int SchemaVersion = 1;
    public const string RoundingPolicyId = "L3-COMPUTATION-CENTS-v1";
    public const string Individual = "individual-professional";
    public const string Corporation = "domestic-corporation";
    /// <summary>Largest source/output magnitude this bounded content uses.</summary>
    public const decimal MaxMagnitude = 100000000.00m;

    public static readonly string[] BeaDecisionIds =
    {
        "dest-R1", "dest-R2", "dest-R3", "dest-R4", "dest-R5", "dest-R6", "dest-R7",
        "reason-R6", "reason-R7", "related-R7", "subtotal-income", "subtotal-expenses"
    };

    private static readonly Regex Money = new Regex(@"^-?\d+\.\d{2}$", RegexOptions.CultureInvariant);
    private static readonly Regex Rate = new Regex(@"^\d+(\.\d+)?$", RegexOptions.CultureInvariant);
    private static readonly int[] PaperTypes = { 0, 1, 2, 4, 5, 7, 8 };

    /// <summary>Problems with the definition's Level 3 block (empty = valid). expectedCaseId: the id the level's resource stem maps to.</summary>
    public static List<string> Validate(CaseDefinition definition, string expectedCaseId)
    {
        var problems = new List<string>();
        if (definition == null) { problems.Add("no case definition"); return problems; }
        Level3CaseContent c = definition.level3;
        if (c == null || !c.IsSet) { problems.Add("missing the level3 content block"); return problems; }
        if (c.schemaVersion != SchemaVersion) problems.Add($"level3.schemaVersion {c.schemaVersion} is not {SchemaVersion}");
        if (c.contentRevision != Level3ChapterRules.ContentRevision) problems.Add($"level3.contentRevision '{c.contentRevision}' is not {Level3ChapterRules.ContentRevision}");
        if (string.IsNullOrEmpty(expectedCaseId) || definition.caseId != expectedCaseId || c.caseId != expectedCaseId)
            problems.Add($"case identity '{definition.caseId}' / level3 '{c.caseId}' does not match the requested case '{expectedCaseId}'");
        if (string.IsNullOrWhiteSpace(c.clientId) || string.IsNullOrWhiteSpace(c.taxpayer) || string.IsNullOrWhiteSpace(c.regimeId))
            problems.Add("level3 needs clientId, taxpayer and regimeId");
        if (c.roundingPolicyId != RoundingPolicyId) problems.Add($"level3.roundingPolicyId must be {RoundingPolicyId}");
        bool individual = c.taxpayerKind == Individual;
        bool corporation = c.taxpayerKind == Corporation;
        if (!individual && !corporation) problems.Add($"unknown taxpayerKind '{c.taxpayerKind}'");

        bool period = TryDate(c.periodStart, out DateTime start) & TryDate(c.periodEnd, out DateTime end);
        if (!period || start > end || start.Year != c.taxYear || end.Year != c.taxYear)
            problems.Add("level3 needs an ISO annual period inside taxYear");

        var documents = new Dictionary<string, DocumentDefinition>(StringComparer.Ordinal);
        if (definition.documents != null)
            foreach (DocumentDefinition d in definition.documents)
            {
                if (d == null || string.IsNullOrEmpty(d.documentName)) continue;
                documents[d.documentName] = d;
                // A field is found by its semantic key within its paper: a repeated key would be ambiguous.
                var keys = new HashSet<string>(StringComparer.Ordinal);
                foreach (DocumentFieldDefinition field in d.fields ?? new List<DocumentFieldDefinition>())
                    if (field != null && !string.IsNullOrEmpty(field.semanticKey) && !keys.Add(field.semanticKey))
                        problems.Add($"document {d.documentName} repeats the field key {field.semanticKey}");
            }

        // ---------- sources ----------
        var sources = new Dictionary<string, Level3SourceDefinition>(StringComparer.Ordinal);
        foreach (Level3SourceDefinition s in c.sources ?? new List<Level3SourceDefinition>())
        {
            if (s == null || string.IsNullOrWhiteSpace(s.sourceId)) { problems.Add("a source has no sourceId"); continue; }
            if (!sources.ContainsKey(s.sourceId)) sources[s.sourceId] = s;
            else { problems.Add($"source {s.sourceId} is listed twice"); continue; }
            if (string.IsNullOrEmpty(s.documentName) || !documents.ContainsKey(s.documentName))
                problems.Add($"source {s.sourceId} names no document '{s.documentName}'");
            if (Array.IndexOf(PaperTypes, s.paperType) < 0) problems.Add($"source {s.sourceId} has unsupported paperType {s.paperType}");
            if (!TryDate(s.periodStart, out DateTime ss) || !TryDate(s.periodEnd, out DateTime se) || ss > se || (period && (ss < start || se > end)))
                problems.Add($"source {s.sourceId} period is not inside the case period");
            if (string.IsNullOrWhiteSpace(s.reference) || string.IsNullOrWhiteSpace(s.issuer) || string.IsNullOrWhiteSpace(s.party))
                problems.Add($"source {s.sourceId} needs reference, issuer and party");
        }
        if (sources.Count == 0) problems.Add("level3 lists no sources");

        // ---------- rows ----------
        var rows = new Dictionary<string, Level3RowDefinition>(StringComparer.Ordinal);
        var amounts = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var fieldKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (Level3RowDefinition r in c.rows ?? new List<Level3RowDefinition>())
        {
            if (r == null || string.IsNullOrWhiteSpace(r.rowId)) { problems.Add("a row has no rowId"); continue; }
            if (!rows.ContainsKey(r.rowId)) rows[r.rowId] = r;
            else { problems.Add($"row {r.rowId} is listed twice"); continue; }
            if (string.IsNullOrWhiteSpace(r.fieldKey) || !fieldKeys.Add(r.fieldKey)) problems.Add($"row {r.rowId} needs a unique fieldKey");
            if (string.IsNullOrWhiteSpace(r.transactionId)) problems.Add($"row {r.rowId} needs a transactionId");
            if (!TryMoney(r.amount, out decimal amount)) { problems.Add($"row {r.rowId} amount '{r.amount}' is not bounded money (0.00)"); continue; }
            amounts[r.rowId] = amount;
            if (r.sourceId == null || !sources.TryGetValue(r.sourceId, out Level3SourceDefinition source))
            {
                problems.Add($"row {r.rowId} names unknown source '{r.sourceId}'");
                continue;
            }
            string printed = PrintedProblem(documents, source.documentName, r.fieldKey, amount);
            if (printed != null) problems.Add($"row {r.rowId}: {printed}");
        }
        if (rows.Count == 0) problems.Add("level3 lists no rows");

        // ---------- relations (support / claim / subset), acyclic ----------
        var parent = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Level3RowDefinition r in rows.Values)
        {
            string relation = r.relation ?? "";
            if (relation == "-") continue;
            int colon = relation.IndexOf(':');
            string kind = colon > 0 ? relation.Substring(0, colon) : "";
            string target = colon > 0 ? relation.Substring(colon + 1) : "";
            if ((kind != "supports" && kind != "claim" && kind != "subset") || !rows.TryGetValue(target, out Level3RowDefinition to) || target == r.rowId)
            {
                problems.Add($"row {r.rowId} relation '{relation}' is not '-', supports:/claim:/subset:<row>");
                continue;
            }
            parent[r.rowId] = target;
            if (!amounts.TryGetValue(r.rowId, out decimal a) || !amounts.TryGetValue(target, out decimal b)) continue;
            if (kind == "supports" && (r.transactionId != to.transactionId || a != b))
                problems.Add($"support {r.rowId} -> {target} must share the transaction and amount");
            if (kind == "claim" && r.transactionId != to.transactionId)
                problems.Add($"claim {r.rowId} -> {target} must name the claimed transaction");
            if (kind == "subset" && (a > b || r.sourceId == to.sourceId))
                problems.Add($"subset {r.rowId} -> {target} must be another paper's part of its parent");
        }
        foreach (string id in parent.Keys)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal) { id };
            string at = id;
            while (parent.TryGetValue(at, out string next))
            {
                if (!seen.Add(next)) { problems.Add($"row relations loop through {id}"); break; }
                at = next;
            }
        }

        // ---------- totals ----------
        foreach (Level3TotalDefinition t in c.totals ?? new List<Level3TotalDefinition>())
        {
            if (t == null || t.sourceId == null || !sources.TryGetValue(t.sourceId, out Level3SourceDefinition source)) { problems.Add("a total names an unknown source"); continue; }
            if (!TryMoney(t.amount, out decimal amount)) { problems.Add($"total {t.fieldKey} amount '{t.amount}' is not bounded money"); continue; }
            if (string.IsNullOrWhiteSpace(t.fieldKey) || !fieldKeys.Add(t.fieldKey)) problems.Add($"total {t.fieldKey} needs a unique fieldKey");
            decimal sum = 0m;
            var parts = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in t.componentRowIds ?? new List<string>())
            {
                if (!parts.Add(id ?? "") || !rows.TryGetValue(id ?? "", out Level3RowDefinition row) || row.sourceId != t.sourceId || !amounts.ContainsKey(id))
                {
                    problems.Add($"total {t.fieldKey} component '{id}' is not a distinct row of {t.sourceId}");
                    continue;
                }
                sum += amounts[id];
            }
            if (parts.Count == 0 || sum != amount) problems.Add($"total {t.fieldKey} components sum to {Format(sum)}, printed {t.amount}");
            string printed = PrintedProblem(documents, source.documentName, t.fieldKey, amount);
            if (printed != null) problems.Add($"total {t.fieldKey}: {printed}");
        }

        // ---------- credit links ----------
        var credits = new HashSet<string>(StringComparer.Ordinal);
        foreach (Level3CreditLink link in c.creditLinks ?? new List<Level3CreditLink>())
        {
            if (link == null || !credits.Add(link.creditRowId ?? "")) { problems.Add($"credit row '{link?.creditRowId}' is linked twice"); continue; }
            bool known = rows.TryGetValue(link.creditRowId ?? "", out Level3RowDefinition credit) &
                         rows.TryGetValue(link.incomeRowId ?? "", out Level3RowDefinition income) &
                         rows.ContainsKey(link.includedInRowId ?? "");
            if (!known) { problems.Add($"credit link {link.creditRowId} names unknown rows"); continue; }
            if (credit.sourceId != income.sourceId || income.relation != "subset:" + link.includedInRowId || credit.relation != "-")
                problems.Add($"credit {link.creditRowId} must come from its certificate, whose income is a subset of {link.includedInRowId}");
            if (sources.TryGetValue(credit.sourceId, out Level3SourceDefinition paper) && paper.paperType != 8)
                problems.Add($"credit {link.creditRowId} must come from a Type 8 certificate");
        }
        if (credits.Count != 1) problems.Add("each case needs exactly one credit link");

        // ---------- rules ----------
        var ruleIds = new HashSet<string>(StringComparer.Ordinal);
        var bands = new List<Level3RuleDefinition>();
        foreach (Level3RuleDefinition rule in c.rules ?? new List<Level3RuleDefinition>())
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.ruleId) || !ruleIds.Add(rule.ruleId)) { problems.Add("a rule needs a unique ruleId"); continue; }
            bool ok = TryMoney(rule.lower, out _) & TryMoney(rule.baseTax, out _) & TryRate(rule.rate, out _) &
                      (string.IsNullOrEmpty(rule.upper) || TryMoney(rule.upper, out _));
            if (!ok) problems.Add($"rule {rule.ruleId} has malformed numbers");
            else bands.Add(rule);
        }
        if (individual) ValidateBands(bands, problems);
        if (corporation) ValidateCorporate(c.corporateFacts, rows, amounts, problems);
        else if (c.corporateFacts != null && c.corporateFacts.IsSet) problems.Add("an individual case carries corporate facts");

        // ---------- decisions ----------
        var decisions = new List<string>();
        foreach (Level3DecisionDefinition d in c.worksheetDecisions ?? new List<Level3DecisionDefinition>())
        {
            if (d == null || string.IsNullOrWhiteSpace(d.decisionId) || decisions.Contains(d.decisionId) || string.IsNullOrWhiteSpace(d.expected))
                problems.Add($"worksheet decision '{d?.decisionId}' is blank or repeated");
            else decisions.Add(d.decisionId);
        }
        if (individual && (decisions.Count != BeaDecisionIds.Length || !decisions.TrueForAll(id => Array.IndexOf(BeaDecisionIds, id) >= 0)))
            problems.Add("the individual worksheet needs exactly the twelve decision ids");
        if (corporation && decisions.Count > 0) problems.Add("the company case has no second scored worksheet");

        ValidateChain(c, individual, bands, problems);
        return problems;
    }

    // =========================================================
    // RULES AND CHAINS
    // =========================================================

    private static void ValidateBands(List<Level3RuleDefinition> bands, List<string> problems)
    {
        if (bands.Count == 0) { problems.Add("the individual case needs its annual rule bands"); return; }
        decimal expectedLower = 0m, expectedBase = 0m;
        for (int i = 0; i < bands.Count; i++)
        {
            Level3RuleDefinition band = bands[i];
            TryMoney(band.lower, out decimal lower);
            TryMoney(band.baseTax, out decimal baseTax);
            TryRate(band.rate, out decimal rate);
            bool last = i == bands.Count - 1;
            if (lower != expectedLower || baseTax != expectedBase) problems.Add($"rule {band.ruleId} does not continue the previous band");
            if (last != string.IsNullOrEmpty(band.upper)) problems.Add($"rule {band.ruleId}: only the last band is unbounded");
            if (last) break;
            TryMoney(band.upper, out decimal upper);
            if (upper <= lower) problems.Add($"rule {band.ruleId} upper is not above lower");
            expectedLower = upper;
            expectedBase = baseTax + rate * (upper - lower);
        }
    }

    private static void ValidateCorporate(Level3CorporateFacts f, Dictionary<string, Level3RowDefinition> rows, Dictionary<string, decimal> amounts, List<string> problems)
    {
        if (f == null || !f.IsSet) { problems.Add("the company case needs explicit corporateFacts"); return; }
        bool ok = TryRate(f.ordinaryRate, out _) & TryRate(f.smallRate, out _) & TryRate(f.mcitRate, out _) &
                  TryMoney(f.smallIncomeCeiling, out _) & TryMoney(f.smallAssetCeiling, out _) &
                  TryDate(f.operationsStart, out DateTime operations) & TryDate(f.birRegistrationDate, out _);
        if (!ok) problems.Add("corporateFacts has malformed values");
        if (f.eligibilityOperator != "AND") problems.Add("corporateFacts.eligibilityOperator must be AND");
        if (string.IsNullOrWhiteSpace(f.incentiveRegime)) problems.Add("corporateFacts.incentiveRegime must be explicit");
        if (string.IsNullOrEmpty(f.assetRowId) || !rows.ContainsKey(f.assetRowId) || !amounts.ContainsKey(f.assetRowId))
            problems.Add("corporateFacts.assetRowId names no row");
        if (ok && f.firstMCITYear != operations.Year + f.mcitFollowingYearOffset)
            problems.Add("corporateFacts.firstMCITYear must be the operations year plus its offset");
    }

    /// <summary>The expected fields agree with each other and with the rules (exact decimals, cents policy for tax due).</summary>
    private static void ValidateChain(Level3CaseContent c, bool individual, List<Level3RuleDefinition> bands, List<string> problems)
    {
        var fields = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (Level3ExpectedField f in c.expectedFields ?? new List<Level3ExpectedField>())
        {
            if (f == null || string.IsNullOrWhiteSpace(f.field) || fields.ContainsKey(f.field) || !TryMoney(f.amount, out decimal value))
            {
                problems.Add($"expected field '{f?.field}' is blank, repeated or not money");
                continue;
            }
            fields[f.field] = value;
        }
        string[] required = { "grossIncome", "allowableExpenses", "taxableIncome", "taxDue", "taxWithheldOrCredits", "finalTaxPayable" };
        foreach (string key in required)
            if (!fields.ContainsKey(key)) problems.Add($"expected field {key} is missing");
        if (problems.Exists(p => p.StartsWith("expected field", StringComparison.Ordinal))) return;

        if (fields["taxableIncome"] != fields["grossIncome"] - fields["allowableExpenses"]) problems.Add("expected taxableIncome is not gross minus expenses");
        if (fields["finalTaxPayable"] != fields["taxDue"] - fields["taxWithheldOrCredits"]) problems.Add("expected finalTaxPayable is not tax due minus credit");
        decimal taxable = fields["taxableIncome"];
        decimal? tax = null;
        if (individual)
        {
            foreach (Level3RuleDefinition band in bands)
            {
                TryMoney(band.lower, out decimal lower);
                bool fits = string.IsNullOrEmpty(band.upper) || (TryMoney(band.upper, out decimal upper) && taxable <= upper);
                if (taxable < lower || !fits) continue;
                TryMoney(band.baseTax, out decimal baseTax);
                TryRate(band.rate, out decimal rate);
                tax = Cents(baseTax + rate * (taxable - lower));
                break;
            }
        }
        else if (c.corporateFacts != null && TryRate(c.corporateFacts.ordinaryRate, out decimal ordinary))
        {
            tax = Cents(taxable * ordinary);
        }
        if (tax == null || tax.Value != fields["taxDue"]) problems.Add($"expected taxDue {Format(fields["taxDue"])} does not follow the case rule");
    }

    // =========================================================
    // PARSING
    // =========================================================

    /// <summary>Invariant money "-?digits.cc" within the bounded magnitude.</summary>
    public static bool TryMoney(string text, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrEmpty(text) || !Money.IsMatch(text)) return false;
        if (!decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value)) return false;
        return Math.Abs(value) <= MaxMagnitude;
    }

    /// <summary>A rate fraction 0..1 ("0.20").</summary>
    public static bool TryRate(string text, out decimal value)
    {
        value = 0m;
        return !string.IsNullOrEmpty(text) && Rate.IsMatch(text) &&
               decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value) && value <= 1m;
    }

    /// <summary>"₱1,234.50" / "1234.50" -> 1234.50 (printed document values).</summary>
    public static bool TryPrinted(string text, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(text)) return false;
        string plain = text.Replace("₱", "").Replace(",", "").Trim();
        return decimal.TryParse(plain, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Half-up to cents (TAX_AUTHORITY policy L3-COMPUTATION-CENTS-v1; only the completed tax due is rounded).</summary>
    public static decimal Cents(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string Format(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static bool TryDate(string text, out DateTime date) =>
        DateTime.TryParseExact(text ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static string PrintedProblem(Dictionary<string, DocumentDefinition> documents, string documentName, string fieldKey, decimal amount)
    {
        if (string.IsNullOrEmpty(documentName) || !documents.TryGetValue(documentName, out DocumentDefinition document) || document.fields == null)
            return $"document '{documentName}' is missing";
        foreach (DocumentFieldDefinition field in document.fields)
        {
            if (field == null || field.semanticKey != fieldKey) continue;
            if (field.type != "Number") return $"field {fieldKey} must be a Number";
            if (!TryPrinted(field.displayValue, out decimal shown) || shown != amount) return $"field {fieldKey} prints '{field.displayValue}', content says {Format(amount)}";
            return null;
        }
        return $"document '{documentName}' has no field {fieldKey}";
    }
}
