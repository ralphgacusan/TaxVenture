using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>A total the player can put on the computation sheet (the collected summary's Income or Supported deductions, or a printed draft total).</summary>
public sealed class Level3TotalChoice
{
    public string ChoiceId;
    public string Label;
    public string Amount;
}

/// <summary>
/// What one step of the computation sheet rests on, for the review (T09) and
/// the tests. Present: chosen or attached. Current: its inputs are unchanged
/// since its tape was made. Follows: the step's own operation is right given
/// the player's own inputs. Supported: it follows AND everything it rests on
/// is supported, so it could be approved. Root: the case-scoped key a review
/// charges when the step itself does not follow (FIXTURE_CONTRACT).
/// </summary>
public sealed class Level3ComputationFact
{
    public string Field;
    public bool Present;
    public bool Current;
    public bool Typed;
    public bool Follows;
    public bool Supported;
    public string Value;
    public string Root;
}

/// <summary>Every step of one case's computation sheet, in sheet order.</summary>
public sealed class Level3ComputationFacts
{
    public readonly List<Level3ComputationFact> Steps = new List<Level3ComputationFact>();

    public Level3ComputationFact Step(string field)
    {
        foreach (Level3ComputationFact step in Steps) if (step.Field == field) return step;
        return null;
    }

    /// <summary>Taxable income and tax due are both supported (T07's part of the chain).</summary>
    public bool TaxDueSupported => Step(Level3ComputationRules.Taxable)?.Supported == true && Step(Level3ComputationRules.TaxDue)?.Supported == true;

    /// <summary>T08: the linked credit and the remaining payable are both supported (the whole chain under them is too).</summary>
    public bool PayableSupported => Step(Level3ComputationRules.Credits)?.Supported == true && Step(Level3ComputationRules.Payable)?.Supported == true;

    /// <summary>T08: every required step is present and current (gross, expenses, taxable income, band, tax due, credits, remaining payable); never a grade.</summary>
    public bool Complete;

    /// <summary>T08: a step above the credit (taxable income up to tax due) uses the linked credit, i.e. it was treated as an expense (review root credit-source).</summary>
    public bool CreditInIncome;

    /// <summary>T08: the content row the certificate's income payment is already counted in (CONTENT: R2), so it is never another revenue line.</summary>
    public string CertificateIncomeIncludedIn = "";

    /// <summary>The sheet revision these facts were read at (a stamp or review compares it; any later change makes them old).</summary>
    public int Revision;
}

/// <summary>
/// PURPOSE:
/// Bea's computation at the office desk (Level 3 T07), pure C# so the
/// regression suite drives the same commands the desk calls. Every command
/// runs on a COPY of the chapter snapshot; the caller writes, then publishes.
///
/// - SelectTotal: Gross income / Allowable expenses take a total the player
///   picks explicitly: the collected summary's (B-WORKSHEET, revised T06) exact
///   Income or Supported deductions, or Bea's draft "Proposed costs" (a plausible wrong choice). The
///   amount is copied at that moment; nothing is ever filled in for them.
/// - SelectRule: the rate band, chosen from the year's annual table (content
///   rules). Never chosen automatically.
/// - AttachTape: a calculator tape (a single calculation or a chain such as
///   taxable − floor = × rate = + base) goes onto Taxable income, Excess over
///   the floor, Tax on the excess or Tax due. Its numbers are the sheet's own
///   (chosen totals, the chosen band's floor/base/rate, earlier tapes) or
///   typed by hand; typed numbers are kept but never support an approval.
///   Anything else is refused with a reason. Any such tape is accepted,
///   whatever it computes: the review judges it (T09).
/// - Staleness follows sources: a tape whose chosen total, band constant or
///   earlier tape changed is out of date (kept, tagged "needs recalculation")
///   until replaced. Changing the band never touches Taxable income.
/// - Evaluate: the facts T09 needs, per step, with exact decimals; Tax due is
///   rounded to cents (half away from zero) only at the end
///   (TAX_AUTHORITY L3-COMPUTATION-CENTS-v1). This is the only reader of the
///   rule-selection answer; nothing here leaks it to the sheet.
/// - T08, credit and balance: once Bea's certificate is acquired and the player
///   explained it (Level3CertificateRules), SelectCredit links one of the
///   certificate's printed amounts to Tax credits (both amounts are offered
///   alike; nothing is linked for them). The Remaining payable tape is like
///   any other: tax due - credits, the sheet's own numbers or typed ones.
///   Changing only the credit leaves every line above it current and marks the
///   payable (and anything later, such as a stamp) out of date. A negative
///   balance stays negative; nothing is clamped. Using the credit above its own
///   line (as an expense) is accepted as draft work and reported (CreditInIncome).
/// There is no approval or reward here (T09).
/// </summary>
public static class Level3ComputationRules
{
    public const string Gross = "gross";
    public const string Expenses = "expenses";
    public const string Rule = "rule";
    public const string Taxable = "taxable-income";
    public const string Excess = "excess";
    public const string Marginal = "marginal-tax";
    public const string TaxDue = "tax-due";
    /// <summary>T08: the certificate amount linked as the credit (a choice, like the totals).</summary>
    public const string Credits = "credits";
    /// <summary>T08: tax due - credits, a tape.</summary>
    public const string Payable = "remaining-payable";
    /// <summary>The tape steps in dependency order: a tape may use only the tapes before it.</summary>
    public static readonly string[] TapeFields = { Taxable, Excess, Marginal, TaxDue, Payable };

    public const string WorksheetIncome = "worksheet:income";
    public const string WorksheetExpenses = "worksheet:expenses";
    public const string DraftCosts = "paper:B-DRAFT/BDraftTotal";
    /// <summary>T08: Bea's certificate's two printed amounts as credit choices (offered alike).</summary>
    public const string CertificateCredit = "paper:B-WHT/BWithheldCredit";
    public const string CertificateIncome = "paper:B-WHT/BWithheldIncome";

    public const string FieldPrefix = "field:";
    public const string RulePrefix = "rule:";
    public const string SheetPrefix = "sheet:";
    public const string Lower = "lower";
    public const string Base = "base";
    public const string Rate = "rate";

    public const string RefuseForeign = "That number isn't on Bea's computation sheet. Press C, then tap the sheet's own amounts.";
    public const string RefuseChanged = "That amount changed since you took it. Tap it on the sheet again.";
    public const string RefuseStaleSource = "That tape needs recalculating first: work it out again and attach it.";
    public const string RefuseLater = "A step can only use the steps above it on the sheet.";
    public const string RefuseNoTape = "Press = to print a tape first.";
    public const string RefuseNoCertificate = "Bea's withholding certificate isn't here yet. She is waiting in the conference room.";
    public const string RefuseExplainFirst = "Answer Bea's question about her certificate first. She is in the conference room.";
    public const string RefuseNotCertificate = "That amount is not on Bea's certificate.";

    public static string CaseKey(string caseId) =>
        caseId == Level3ChapterRules.BeaCaseId ? "bea" : caseId == Level3ChapterRules.CompanyCaseId ? "marine" : (caseId ?? "").ToLowerInvariant();

    public static string Destination(string caseId, string field) => CaseKey(caseId) + "/" + field;
    public static string FieldSource(string caseId, string field) => FieldPrefix + CaseKey(caseId) + "/" + field;
    public static string RuleSource(string ruleId, string part) => RulePrefix + ruleId + "/" + part;
    public static string SheetSource(string caseId, string field) => SheetPrefix + Destination(caseId, field);

    public static Level3ComputationRecord Record(Level3ChapterCheckpoint state, string caseId) => state?.Case(caseId)?.computation;

    public static string SectionLabel(string field)
    {
        switch (field)
        {
            case Gross: return "Gross income";
            case Expenses: return "Allowable expenses";
            case Taxable: return "Taxable income";
            case Rule: return "Rate band";
            case Excess: return "Excess over the floor";
            case Marginal: return "Tax on the excess";
            case TaxDue: return "Tax due";
            case Credits: return "Tax credits";
            case Payable: return "Remaining payable";
            default: return field ?? "";
        }
    }

    /// <summary>Bea's sheet can be worked on: her computation phase, a usable chapter, her worksheet collected.</summary>
    public static bool Editable(Level3ChapterCheckpoint state, string caseId) =>
        caseId == Level3ChapterRules.BeaCaseId && Level3ChapterRules.PhaseOf(state) == Level3Phase.BeaComputation &&
        Level3ChapterRules.Problem(state) == null && Level3WorksheetCheck.Collected(state);

    // =========================================================
    // THE ANNUAL TABLE (content rules) AND THE CENTS POLICY
    // =========================================================

    public static List<Level3RuleDefinition> Rules(CaseDefinition definition)
    {
        var rules = new List<Level3RuleDefinition>();
        if (definition?.level3?.rules == null) return rules;
        foreach (Level3RuleDefinition rule in definition.level3.rules)
            if (rule != null && !string.IsNullOrEmpty(rule.ruleId) && rule.ruleId.StartsWith("IND-", StringComparison.Ordinal)) rules.Add(rule);
        return rules;
    }

    public static Level3RuleDefinition FindRule(CaseDefinition definition, string ruleId)
    {
        foreach (Level3RuleDefinition rule in Rules(definition)) if (rule.ruleId == ruleId) return rule;
        return null;
    }

    /// <summary>The band containing an annual taxable amount: upper bound inclusive, lower exclusive except the zero band; null for a negative amount.</summary>
    public static Level3RuleDefinition ContainingRule(CaseDefinition definition, decimal taxable)
    {
        if (taxable < 0m) return null;
        foreach (Level3RuleDefinition rule in Rules(definition))
        {
            if (!Money(rule.lower, out decimal lower)) continue;
            bool unbounded = string.IsNullOrEmpty(rule.upper);
            decimal upper = 0m;
            if (!unbounded && !Money(rule.upper, out upper)) continue;
            bool aboveLower = lower == 0m ? taxable >= 0m : taxable > lower;
            if (aboveLower && (unbounded || taxable <= upper)) return rule;
        }
        return null;
    }

    /// <summary>baseTax + rate × (taxable − lower), exact (no rounding inside).</summary>
    public static decimal TaxDueExact(Level3RuleDefinition rule, decimal taxable)
    {
        Money(rule.lower, out decimal lower);
        Money(rule.baseTax, out decimal baseTax);
        Money(rule.rate, out decimal rate);
        return baseTax + rate * (taxable - lower);
    }

    /// <summary>The completed Tax due to cents, half away from zero (L3-COMPUTATION-CENTS-v1).</summary>
    public static decimal RoundCents(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static string BandLabel(Level3RuleDefinition rule)
    {
        if (rule == null) return "";
        string lower = Whole(rule.lower), upper = Whole(rule.upper);
        if (Money(rule.lower, out decimal l) && l == 0m) return "Up to " + upper;
        return string.IsNullOrEmpty(rule.upper) ? "Over " + lower : "Over " + lower + " to " + upper;
    }

    public static string BandFormula(Level3RuleDefinition rule)
    {
        if (rule == null) return "";
        Money(rule.baseTax, out decimal baseTax);
        Money(rule.rate, out decimal rate);
        if (rate == 0m && baseTax == 0m) return "no tax";
        string share = Percent(rule.rate) + " of the excess over " + Whole(rule.lower);
        return baseTax == 0m ? share : Whole(rule.baseTax) + " + " + share;
    }

    public static string Percent(string fraction) =>
        Money(fraction, out decimal f) ? Level3Calculator.Canonical(f * 100m) + "%" : "";

    private static string Whole(string amount) =>
        Money(amount, out decimal value) ? value.ToString("#,0.##", CultureInfo.InvariantCulture) : "";

    private static bool Money(string text, out decimal value) => Level3Calculator.TryParse(text, out value);

    // =========================================================
    // CHOICES
    // =========================================================

    /// <summary>What Gross income and Allowable expenses may take (the same list for both, offered alike).</summary>
    public static List<Level3TotalChoice> TotalChoices(Level3ChapterCheckpoint state, CaseDefinition definition)
    {
        var choices = new List<Level3TotalChoice>();
        if (!Level3WorksheetCheck.Collected(state)) return choices;
        // Revised T06: the collected summary's exact totals (its corrected slips), the same B-WORKSHEET for a
        // pre-revision worksheet whose tapes added those slips; no float bridge, never the answer key.
        WorksheetTotals(state, out string income, out string expenses);
        choices.Add(new Level3TotalChoice { ChoiceId = WorksheetIncome, Label = "Bea's summary · Income", Amount = income });
        choices.Add(new Level3TotalChoice { ChoiceId = WorksheetExpenses, Label = "Bea's summary · " + Level3WorksheetCheck.SupportedDeductionsLabel, Amount = expenses });
        // Bea's own draft total is on her papers too: a plausible choice, offered exactly like the others.
        if (state.Case(Level3ChapterRules.BeaCaseId)?.acquiredSourceIds?.Contains("B-DRAFT") == true && definition?.level3?.totals != null)
            foreach (Level3TotalDefinition total in definition.level3.totals)
                if (total != null && total.sourceId == "B-DRAFT" && total.fieldKey == "BDraftTotal")
                    choices.Add(new Level3TotalChoice { ChoiceId = DraftCosts, Label = "Bea's draft · " + PrintedLabel(definition, "B-DRAFT", "BDraftTotal"), Amount = total.amount });
        return choices;
    }

    /// <summary>The collected worksheet's two totals as exact canonical decimals ("900000", "300000").</summary>
    public static void WorksheetTotals(Level3ChapterCheckpoint state, out string income, out string expenses)
    {
        Level3WorksheetCheck.SummaryTotals(state, out decimal i, out decimal e);
        income = Level3Calculator.Canonical(i);
        expenses = Level3Calculator.Canonical(e);
    }

    private static string PrintedLabel(CaseDefinition definition, string sourceId, string fieldKey)
    {
        Level3SourceDefinition source = definition.level3.sources?.Find(s => s != null && s.sourceId == sourceId);
        DocumentDefinition document = source != null ? definition.documents?.Find(d => d != null && d.documentName == source.documentName) : null;
        return document?.fields?.Find(f => f != null && f.semanticKey == fieldKey)?.label ?? fieldKey;
    }

    /// <summary>"paper:B-WHT/BWithheldCredit": one printed amount of a certificate.</summary>
    public static string CertificateChoice(string sourceId, string fieldKey) => "paper:" + sourceId + "/" + fieldKey;

    /// <summary>
    /// T08: what Tax credits may take: every printed amount of this case's
    /// acquired withholding certificates (Type 8), in printed order, labelled
    /// alike. Another case's certificate is never offered.
    /// </summary>
    public static List<Level3TotalChoice> CreditChoices(Level3ChapterCheckpoint state, CaseDefinition definition)
    {
        var choices = new List<Level3TotalChoice>();
        Level3CaseContent content = definition?.level3;
        Level3CaseProgress progress = state?.Case(definition?.caseId);
        if (content == null || !content.IsSet || progress == null || content.sources == null || content.rows == null) return choices;
        foreach (Level3SourceDefinition source in content.sources)
        {
            if (source == null || source.paperType != 8 || !progress.acquiredSourceIds.Contains(source.sourceId)) continue;
            foreach (Level3RowDefinition row in content.rows)
                if (row != null && row.sourceId == source.sourceId)
                    choices.Add(new Level3TotalChoice
                    {
                        ChoiceId = CertificateChoice(source.sourceId, row.fieldKey),
                        Label = "Certificate " + source.reference + " · " + PrintedLabel(definition, source.sourceId, row.fieldKey),
                        Amount = row.amount
                    });
        }
        return choices;
    }

    // =========================================================
    // COMMANDS (on a copy)
    // =========================================================

    public static Level3CommandOutcome SelectTotal(Level3ChapterCheckpoint candidate, CaseDefinition definition, string field, string choiceId, out string reason)
    {
        if (!Ready(candidate, out reason)) return Level3CommandOutcome.Refused;
        if (field != Gross && field != Expenses) { reason = "Only Gross income and Allowable expenses take a total."; return Level3CommandOutcome.Refused; }
        Level3TotalChoice choice = TotalChoices(candidate, definition).Find(c => c.ChoiceId == choiceId);
        if (choice == null) { reason = "That total is not one of Bea's."; return Level3CommandOutcome.Refused; }
        Level3ComputationRecord record = Record(candidate, Level3ChapterRules.BeaCaseId);
        string had = field == Gross ? record.grossChoice : record.expensesChoice;
        string amount = field == Gross ? record.grossAmount : record.expensesAmount;
        if (had == choice.ChoiceId && amount == choice.Amount) { reason = "already chosen"; return Level3CommandOutcome.Duplicate; }
        if (field == Gross) { record.grossChoice = choice.ChoiceId; record.grossAmount = choice.Amount; }
        else { record.expensesChoice = choice.ChoiceId; record.expensesAmount = choice.Amount; }
        record.revision++;
        return Level3CommandOutcome.Applied;
    }

    public static Level3CommandOutcome SelectRule(Level3ChapterCheckpoint candidate, CaseDefinition definition, string ruleId, out string reason)
    {
        if (!Ready(candidate, out reason)) return Level3CommandOutcome.Refused;
        if (FindRule(definition, ruleId) == null) { reason = "That is not a row of the annual table."; return Level3CommandOutcome.Refused; }
        Level3ComputationRecord record = Record(candidate, Level3ChapterRules.BeaCaseId);
        if (record.ruleId == ruleId) { reason = "already chosen"; return Level3CommandOutcome.Duplicate; }
        record.ruleId = ruleId;
        record.revision++;
        return Level3CommandOutcome.Applied;
    }

    /// <summary>
    /// T08: links one printed amount of Bea's certificate to Tax credits. Only
    /// after the certificate is acquired and explained; never chosen for the
    /// player. Changing it marks the remaining payable out of date and nothing
    /// above it.
    /// </summary>
    public static Level3CommandOutcome SelectCredit(Level3ChapterCheckpoint candidate, CaseDefinition definition, string choiceId, out string reason)
    {
        if (!Ready(candidate, out reason)) return Level3CommandOutcome.Refused;
        if (!Level3CertificateRules.Acquired(candidate)) { reason = RefuseNoCertificate; return Level3CommandOutcome.Refused; }
        Level3ComputationRecord record = Record(candidate, Level3ChapterRules.BeaCaseId);
        if (!record.creditExplained) { reason = RefuseExplainFirst; return Level3CommandOutcome.Refused; }
        Level3TotalChoice choice = CreditChoices(candidate, definition).Find(c => c.ChoiceId == choiceId);
        if (choice == null) { reason = RefuseNotCertificate; return Level3CommandOutcome.Refused; }
        if (record.creditChoice == choice.ChoiceId && record.creditAmount == choice.Amount) { reason = "already chosen"; return Level3CommandOutcome.Duplicate; }
        record.creditChoice = choice.ChoiceId;
        record.creditAmount = choice.Amount;
        record.revision++;
        NotePayable(candidate, definition, record);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>The first time a credit is linked and the remaining payable is current (raises RemainingPayableAttached once).</summary>
    private static void NotePayable(Level3ChapterCheckpoint candidate, CaseDefinition definition, Level3ComputationRecord record)
    {
        if (!record.payableNoted && !string.IsNullOrEmpty(record.creditChoice) && IsCurrent(candidate, definition, Level3ChapterRules.BeaCaseId, Payable))
            record.payableNoted = true;
    }

    /// <summary>
    /// Attaches a printed tape (one calculation or a chain) to a sheet step.
    /// Applied: it replaces any older tape there. Duplicate: the same current
    /// tape is already there. Refused: nothing changed; reason is plain words.
    /// </summary>
    public static Level3CommandOutcome AttachTape(Level3ChapterCheckpoint candidate, CaseDefinition definition, string field,
        IReadOnlyList<Level3CalcToken> chain, string result, out string reason)
    {
        if (!Ready(candidate, out reason)) return Level3CommandOutcome.Refused;
        string caseId = Level3ChapterRules.BeaCaseId;
        int index = Array.IndexOf(TapeFields, field);
        if (index < 0) { reason = "That line takes a choice, not a tape."; return Level3CommandOutcome.Refused; }
        if (chain == null || chain.Count == 0 || result == null) { reason = RefuseNoTape; return Level3CommandOutcome.Refused; }
        if (Level3Calculator.EvaluateChain(chain, out decimal value) != null || !Money(result, out decimal printed) || value != printed)
        {
            reason = "Press = again to print the whole calculation.";
            return Level3CommandOutcome.Refused;
        }
        foreach (Level3CalcToken token in chain)
        {
            if (token == null) { reason = RefuseForeign; return Level3CommandOutcome.Refused; }
            if (!token.IsNumber) continue;
            if (token.manual && string.IsNullOrEmpty(token.sourceId)) continue; // typed: kept, never supporting
            string sid = token.sourceId ?? "";
            if (sid.StartsWith(SheetPrefix, StringComparison.Ordinal))
            {
                string used = FieldOf(caseId, sid);
                int usedAt = Array.IndexOf(TapeFields, used);
                if (usedAt < 0) { reason = RefuseForeign; return Level3CommandOutcome.Refused; }
                if (usedAt >= index) { reason = RefuseLater; return Level3CommandOutcome.Refused; }
                if (IsStale(candidate, definition, caseId, used)) { reason = RefuseStaleSource; return Level3CommandOutcome.Refused; }
            }
            if (!TryCurrentValue(candidate, definition, caseId, sid, out decimal current)) { reason = RefuseForeign; return Level3CommandOutcome.Refused; }
            if (!Money(token.value, out decimal held) || held != current) { reason = RefuseChanged; return Level3CommandOutcome.Refused; }
        }

        string destination = Destination(caseId, field);
        Level3TapeRecord existing = candidate.Tape(destination);
        if (existing != null && !IsStale(candidate, definition, caseId, field) && SameChain(existing, chain, value)) { reason = "already attached"; return Level3CommandOutcome.Duplicate; }
        Level3ComputationRecord record = Record(candidate, caseId);
        if (existing != null) candidate.tapes.Remove(existing);
        var tape = new Level3TapeRecord
        {
            tapeId = "tape-" + destination.Replace('/', '-'),
            caseId = caseId,
            destinationId = destination,
            result = Level3Calculator.Canonical(value),
            actionId = "Level_03/attach/" + destination + "@" + (record.revision + 1)
        };
        foreach (Level3CalcToken token in chain) tape.expression.Add(token.Copy());
        candidate.tapes.Add(tape);
        record.revision++;
        if (!record.taxDueNoted && IsCurrent(candidate, definition, caseId, Taxable) && IsCurrent(candidate, definition, caseId, TaxDue))
            record.taxDueNoted = true;
        NotePayable(candidate, definition, record);
        return Level3CommandOutcome.Applied;
    }

    private static bool Ready(Level3ChapterCheckpoint candidate, out string reason)
    {
        reason = null;
        if (candidate == null || !Editable(candidate, Level3ChapterRules.BeaCaseId)) { reason = "Bea's computation cannot be changed right now."; return false; }
        Level3CaseProgress bea = candidate.Case(Level3ChapterRules.BeaCaseId);
        if (bea.computation == null) bea.computation = new Level3ComputationRecord();
        return true;
    }

    private static bool SameChain(Level3TapeRecord tape, IReadOnlyList<Level3CalcToken> chain, decimal value)
    {
        if (tape.expression == null || tape.expression.Count != chain.Count || !Money(tape.result, out decimal r) || r != value) return false;
        for (int i = 0; i < chain.Count; i++)
        {
            Level3CalcToken a = tape.expression[i], b = chain[i];
            if (a == null || b == null || a.kind != b.kind || a.op != b.op || a.sourceId != b.sourceId || a.manual != b.manual || a.percent != b.percent) return false;
            if (a.IsNumber && (!Money(a.value, out decimal x) || !Money(b.value, out decimal y) || x != y)) return false;
        }
        return true;
    }

    // =========================================================
    // SOURCES AND STALENESS
    // =========================================================

    /// <summary>A sheet:… source's step ("sheet:bea/excess" -> "excess"), or null.</summary>
    public static string FieldOf(string caseId, string sourceId)
    {
        string prefix = SheetPrefix + CaseKey(caseId) + "/";
        return sourceId != null && sourceId.StartsWith(prefix, StringComparison.Ordinal) ? sourceId.Substring(prefix.Length) : null;
    }

    /// <summary>The current value of one of the sheet's sources (a chosen total, a constant of the chosen band, an attached tape).</summary>
    public static bool TryCurrentValue(Level3ChapterCheckpoint state, CaseDefinition definition, string caseId, string sourceId, out decimal value)
    {
        value = 0m;
        Level3ComputationRecord record = Record(state, caseId);
        if (record == null || string.IsNullOrEmpty(sourceId)) return false;
        // Direct summary operands retain their original source IDs even before the separate fields are filled.
        if ((sourceId == WorksheetIncome || sourceId == WorksheetExpenses) && definition?.caseId == caseId)
            return Money(CurrentAmount(state, definition, sourceId), out value);
        if (sourceId == FieldSource(caseId, Gross)) return !string.IsNullOrEmpty(record.grossChoice) && Money(record.grossAmount, out value);
        if (sourceId == FieldSource(caseId, Expenses)) return !string.IsNullOrEmpty(record.expensesChoice) && Money(record.expensesAmount, out value);
        if (sourceId == FieldSource(caseId, Credits)) return !string.IsNullOrEmpty(record.creditChoice) && Money(record.creditAmount, out value);
        if (sourceId.StartsWith(RulePrefix, StringComparison.Ordinal))
        {
            Level3RuleDefinition rule = FindRule(definition, record.ruleId);
            if (rule == null) return false;
            if (sourceId == RuleSource(rule.ruleId, Lower)) return Money(rule.lower, out value);
            if (sourceId == RuleSource(rule.ruleId, Base)) return Money(rule.baseTax, out value);
            if (sourceId == RuleSource(rule.ruleId, Rate)) return Money(rule.rate, out value);
            return false;
        }
        string field = FieldOf(caseId, sourceId);
        if (field != null && Array.IndexOf(TapeFields, field) >= 0)
        {
            Level3TapeRecord tape = state.Tape(Destination(caseId, field));
            return tape != null && Money(tape.result, out value);
        }
        return false;
    }

    public static Level3TapeRecord TapeOf(Level3ChapterCheckpoint state, string caseId, string field) =>
        state != null && Array.IndexOf(TapeFields, field) >= 0 ? state.Tape(Destination(caseId, field)) : null;

    /// <summary>A tape is out of date when any source it used changed (or went away), including an earlier tape that is out of date itself.</summary>
    public static bool IsStale(Level3ChapterCheckpoint state, CaseDefinition definition, string caseId, string field)
    {
        Level3TapeRecord tape = TapeOf(state, caseId, field);
        if (tape?.expression == null) return false;
        foreach (Level3CalcToken token in tape.expression)
        {
            if (token == null || !token.IsNumber || (token.manual && string.IsNullOrEmpty(token.sourceId))) continue;
            if (!TryCurrentValue(state, definition, caseId, token.sourceId, out decimal current)) return true;
            if (!Money(token.value, out decimal held) || held != current) return true;
            string used = FieldOf(caseId, token.sourceId);
            if (used != null && Array.IndexOf(TapeFields, used) < Array.IndexOf(TapeFields, field) && IsStale(state, definition, caseId, used)) return true;
        }
        return false;
    }

    public static bool IsCurrent(Level3ChapterCheckpoint state, CaseDefinition definition, string caseId, string field) =>
        TapeOf(state, caseId, field) != null && !IsStale(state, definition, caseId, field);

    /// <summary>The tape holds a number typed by hand (it can be kept, but it supports nothing).</summary>
    public static bool HasTyped(Level3TapeRecord tape)
    {
        if (tape?.expression == null) return false;
        foreach (Level3CalcToken token in tape.expression)
            if (token != null && token.IsNumber && token.manual && string.IsNullOrEmpty(token.sourceId)) return true;
        return false;
    }

    // =========================================================
    // FACTS FOR THE REVIEW (T09)
    // =========================================================

    public static Level3ComputationFacts Evaluate(Level3ChapterCheckpoint state, CaseDefinition definition, string caseId)
    {
        var facts = new Level3ComputationFacts();
        Level3ComputationRecord record = Record(state, caseId) ?? new Level3ComputationRecord();
        bool collected = Level3WorksheetCheck.Collected(state);
        WorksheetTotals(state, out string incomeTotal, out string expensesTotal);

        var gross = new Level3ComputationFact { Field = Gross, Present = !string.IsNullOrEmpty(record.grossChoice), Current = true, Value = record.grossAmount, Root = "income-sum" };
        gross.Follows = gross.Present && collected && record.grossChoice == WorksheetIncome && SameMoney(record.grossAmount, incomeTotal);
        gross.Supported = gross.Follows;
        var expenses = new Level3ComputationFact { Field = Expenses, Present = !string.IsNullOrEmpty(record.expensesChoice), Current = true, Value = record.expensesAmount, Root = "expense-sum" };
        expenses.Follows = expenses.Present && collected && record.expensesChoice == WorksheetExpenses && SameMoney(record.expensesAmount, expensesTotal);
        expenses.Supported = expenses.Follows;
        bool directSummary = collected && IsStep(TapeOf(state, caseId, Taxable), WorksheetIncome, "-", WorksheetExpenses);
        // T09 (fixed leaves): the page has no Income/Deductions fields. With none ever placed and no
        // field-linked taxable tape, the collected summary IS the inputs, so the review judges the
        // taxable tape itself (taxable-operation) instead of a "Gross income is empty" line nobody can fill.
        bool fieldless = collected && string.IsNullOrEmpty(record.grossChoice) && string.IsNullOrEmpty(record.expensesChoice) &&
                         !UsesInputFields(state, caseId, TapeOf(state, caseId, Taxable));
        if (directSummary || fieldless)
        {
            gross.Present = gross.Follows = gross.Supported = true; gross.Value = incomeTotal;
            expenses.Present = expenses.Follows = expenses.Supported = true; expenses.Value = expensesTotal;
        }
        facts.Steps.Add(gross);
        facts.Steps.Add(expenses);

        Level3ComputationFact taxable = TapeFact(state, definition, caseId, Taxable, "taxable-operation");
        if (taxable.Present && taxable.Current && !taxable.Typed && Money(gross.Value, out decimal g) && Money(expenses.Value, out decimal e) &&
            Money(taxable.Value, out decimal t))
            taxable.Follows = (IsStep(TapeOf(state, caseId, Taxable), FieldSource(caseId, Gross), "-", FieldSource(caseId, Expenses)) ||
                IsStep(TapeOf(state, caseId, Taxable), WorksheetIncome, "-", WorksheetExpenses)) && t == g - e;
        taxable.Supported = taxable.Follows && gross.Supported && expenses.Supported;
        facts.Steps.Add(taxable);

        Level3RuleDefinition rule = FindRule(definition, record.ruleId);
        var band = new Level3ComputationFact { Field = Rule, Present = rule != null, Current = true, Value = record.ruleId, Root = "rule-selection" };
        decimal taxableValue = 0m;
        bool haveTaxable = taxable.Present && taxable.Current && Money(taxable.Value, out taxableValue);
        band.Follows = rule != null && haveTaxable && ContainingRule(definition, taxableValue)?.ruleId == rule.ruleId;
        band.Supported = band.Follows && taxable.Supported;
        facts.Steps.Add(band);

        Level3ComputationFact excess = TapeFact(state, definition, caseId, Excess, "tax-formula");
        if (excess.Present && excess.Current && !excess.Typed && rule != null && haveTaxable && Money(excess.Value, out decimal x) && Money(rule.lower, out decimal lower))
            excess.Follows = IsStep(TapeOf(state, caseId, Excess), SheetSource(caseId, Taxable), "-", RuleSource(rule.ruleId, Lower)) && x == taxableValue - lower;
        excess.Supported = excess.Follows && taxable.Supported && band.Supported;
        facts.Steps.Add(excess);

        Level3ComputationFact marginal = TapeFact(state, definition, caseId, Marginal, "tax-formula");
        if (marginal.Present && marginal.Current && !marginal.Typed && rule != null && excess.Present && excess.Current &&
            Money(marginal.Value, out decimal m) && Money(excess.Value, out decimal ex) && Money(rule.rate, out decimal rate))
            marginal.Follows = IsStep(TapeOf(state, caseId, Marginal), SheetSource(caseId, Excess), "*", RuleSource(rule.ruleId, Rate)) && m == ex * rate;
        marginal.Supported = marginal.Follows && excess.Supported;
        facts.Steps.Add(marginal);

        Level3ComputationFact due = TapeFact(state, definition, caseId, TaxDue, "tax-formula");
        if (due.Present && Money(due.Value, out decimal exactDue)) due.Value = Level3Calculator.Canonical(RoundCents(exactDue));
        if (due.Present && due.Current && rule != null)
        {
            var sources = new HashSet<string>(StringComparer.Ordinal);
            bool typed = !Expand(state, caseId, TapeOf(state, caseId, TaxDue), sources, 0);
            decimal basis = haveTaxable ? taxableValue : 0m;
            bool basisKnown = haveTaxable;
            if (!basisKnown && Money(record.grossAmount, out decimal g2) && Money(record.expensesAmount, out decimal e2)) { basis = g2 - e2; basisKnown = true; }
            Money(rule.baseTax, out decimal baseTax);
            Money(rule.lower, out decimal floor);
            bool usesBasis = sources.Contains(SheetSource(caseId, Taxable)) || (sources.Contains(FieldSource(caseId, Gross)) && sources.Contains(FieldSource(caseId, Expenses)));
            bool usesRate = sources.Contains(RuleSource(rule.ruleId, Rate));
            bool usesFloor = floor == 0m || sources.Contains(RuleSource(rule.ruleId, Lower));
            bool usesBase = baseTax == 0m || sources.Contains(RuleSource(rule.ruleId, Base));
            bool onlySheet = true;
            foreach (string source in sources)
                if (!source.StartsWith(FieldPrefix, StringComparison.Ordinal) && !source.StartsWith(SheetPrefix, StringComparison.Ordinal) &&
                    source != WorksheetIncome && source != WorksheetExpenses &&
                    source != RuleSource(rule.ruleId, Lower) && source != RuleSource(rule.ruleId, Base) && source != RuleSource(rule.ruleId, Rate))
                    onlySheet = false;
            Money(TapeOf(state, caseId, TaxDue).result, out decimal printedDue);
            due.Follows = !typed && onlySheet && basisKnown && usesBasis && usesRate && usesFloor && usesBase &&
                          RoundCents(printedDue) == RoundCents(TaxDueExact(rule, basis));
        }
        due.Supported = due.Follows && taxable.Supported && band.Supported;
        facts.Steps.Add(due);

        // T08: the credit (a certificate amount) and the remaining payable.
        Level3ComputationFact credit = CreditFact(state, definition, caseId, record, out string includedIn);
        facts.CertificateIncomeIncludedIn = includedIn;
        facts.Steps.Add(credit);

        Level3ComputationFact payable = TapeFact(state, definition, caseId, Payable, "balance-operation");
        if (payable.Present && Money(payable.Value, out decimal exactPayable)) payable.Value = Level3Calculator.Canonical(RoundCents(exactPayable));
        bool viaDue = false;
        if (payable.Present && payable.Current && !payable.Typed && credit.Present && Money(record.creditAmount, out decimal creditValue))
        {
            var sources = new HashSet<string>(StringComparer.Ordinal);
            bool clean = Expand(state, caseId, TapeOf(state, caseId, Payable), sources, 0);
            bool onlySheet = true;
            foreach (string source in sources)
                if (!source.StartsWith(FieldPrefix, StringComparison.Ordinal) && !source.StartsWith(SheetPrefix, StringComparison.Ordinal) &&
                    source != WorksheetIncome && source != WorksheetExpenses &&
                    (rule == null || (source != RuleSource(rule.ruleId, Lower) && source != RuleSource(rule.ruleId, Base) && source != RuleSource(rule.ruleId, Rate))))
                    onlySheet = false;
            bool dueKnown = false;
            decimal dueCents = 0m;
            bool fromBasis = sources.Contains(SheetSource(caseId, Taxable)) || (sources.Contains(FieldSource(caseId, Gross)) && sources.Contains(FieldSource(caseId, Expenses)));
            if (sources.Contains(SheetSource(caseId, TaxDue)))
            {
                if (due.Present && due.Current && Money(TapeOf(state, caseId, TaxDue).result, out decimal dueTape))
                {
                    dueCents = RoundCents(dueTape);
                    dueKnown = viaDue = true;
                }
            }
            else if (rule != null && fromBasis)
            {
                // One chain from the basis to the balance: every ingredient of the band's formula must be in it.
                Money(rule.baseTax, out decimal baseTax);
                Money(rule.lower, out decimal floor);
                bool usesRate = sources.Contains(RuleSource(rule.ruleId, Rate));
                bool usesFloor = floor == 0m || sources.Contains(RuleSource(rule.ruleId, Lower));
                bool usesBase = baseTax == 0m || sources.Contains(RuleSource(rule.ruleId, Base));
                decimal basis = haveTaxable ? taxableValue : 0m;
                bool basisKnown = haveTaxable;
                if (!basisKnown && Money(record.grossAmount, out decimal g3) && Money(record.expensesAmount, out decimal e3)) { basis = g3 - e3; basisKnown = true; }
                if (usesRate && usesFloor && usesBase && basisKnown) { dueCents = RoundCents(TaxDueExact(rule, basis)); dueKnown = true; }
            }
            Money(TapeOf(state, caseId, Payable).result, out decimal printedPayable);
            payable.Follows = clean && onlySheet && dueKnown && sources.Contains(FieldSource(caseId, Credits)) &&
                              RoundCents(printedPayable) == RoundCents(dueCents - creditValue);
        }
        payable.Supported = payable.Follows && credit.Supported && (viaDue ? due.Supported : taxable.Supported && band.Supported);
        facts.Steps.Add(payable);

        foreach (string field in new[] { Taxable, Excess, Marginal, TaxDue })
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            Expand(state, caseId, TapeOf(state, caseId, field), used, 0);
            if (used.Contains(FieldSource(caseId, Credits))) facts.CreditInIncome = true;
        }
        facts.Complete = gross.Present && expenses.Present && band.Present && credit.Present &&
                         taxable.Present && taxable.Current && due.Present && due.Current && payable.Present && payable.Current;
        facts.Revision = record.revision;
        return facts;
    }

    /// <summary>
    /// T08: the credit step (root credit-source). It follows when it is the
    /// case's own credit row (content creditLinks), copied from an acquired
    /// certificate of the case's payee whose period lies inside the tax year,
    /// with the printed amount. Independent of the tax lines above it.
    /// </summary>
    private static Level3ComputationFact CreditFact(Level3ChapterCheckpoint state, CaseDefinition definition, string caseId, Level3ComputationRecord record,
        out string includedIn)
    {
        includedIn = "";
        var credit = new Level3ComputationFact { Field = Credits, Present = !string.IsNullOrEmpty(record.creditChoice), Current = true, Value = record.creditAmount, Root = "credit-source" };
        Level3CaseContent content = definition?.level3;
        Level3CaseProgress progress = state?.Case(caseId);
        if (content?.creditLinks == null || content.rows == null || content.sources == null || progress == null) return credit;
        Level3CreditLink link = content.creditLinks.Find(l => l != null && !string.IsNullOrEmpty(l.creditRowId));
        Level3RowDefinition row = link != null ? content.rows.Find(r => r != null && r.rowId == link.creditRowId) : null;
        Level3SourceDefinition source = row != null ? content.sources.Find(s => s != null && s.sourceId == row.sourceId) : null;
        if (link != null) includedIn = link.includedInRowId ?? "";
        if (row == null || source == null || !credit.Present) return credit;
        string payee = content.sources.Find(s => s != null && s.paperType == 1)?.party;
        bool samePayee = string.IsNullOrEmpty(payee) || source.party == payee;
        bool inYear = string.CompareOrdinal(source.periodStart ?? "", content.periodStart ?? "") >= 0 &&
                      string.CompareOrdinal(source.periodEnd ?? "", content.periodEnd ?? "") <= 0;
        credit.Follows = source.paperType == 8 && progress.acquiredSourceIds.Contains(source.sourceId) && samePayee && inYear &&
                         record.creditChoice == CertificateChoice(source.sourceId, row.fieldKey) && SameMoney(record.creditAmount, row.amount);
        credit.Supported = credit.Follows;
        return credit;
    }

    private static Level3ComputationFact TapeFact(Level3ChapterCheckpoint state, CaseDefinition definition, string caseId, string field, string root)
    {
        Level3TapeRecord tape = TapeOf(state, caseId, field);
        return new Level3ComputationFact
        {
            Field = field, Present = tape != null, Current = tape != null && !IsStale(state, definition, caseId, field),
            Typed = HasTyped(tape), Value = tape?.result, Root = root
        };
    }

    /// <summary>One step "a op b" made of exactly these two sources (no chain, no typed numbers).</summary>
    private static bool IsStep(Level3TapeRecord tape, string left, string op, string right)
    {
        List<Level3CalcToken> e = tape?.expression;
        return e != null && e.Count == 3 && e[0] != null && e[1] != null && e[2] != null && e[0].IsNumber && e[2].IsNumber && e[1].IsOperator &&
               e[0].sourceId == left && e[1].op == op && e[2].sourceId == right;
    }

    /// <summary>Every source a tape rests on, through the earlier tapes it uses. False when anything typed is in it.</summary>
    private static bool Expand(Level3ChapterCheckpoint state, string caseId, Level3TapeRecord tape, HashSet<string> sources, int depth)
    {
        if (tape?.expression == null || depth > TapeFields.Length) return false;
        bool clean = true;
        foreach (Level3CalcToken token in tape.expression)
        {
            if (token == null || !token.IsNumber) continue;
            if (token.manual && string.IsNullOrEmpty(token.sourceId)) { clean = false; continue; }
            sources.Add(token.sourceId);
            string used = FieldOf(caseId, token.sourceId);
            if (used != null) clean &= Expand(state, caseId, TapeOf(state, caseId, used), sources, depth + 1);
        }
        return clean;
    }

    /// <summary>The tape rests on the (legacy) Gross/Expenses folder fields.</summary>
    private static bool UsesInputFields(Level3ChapterCheckpoint state, string caseId, Level3TapeRecord tape)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        Expand(state, caseId, tape, used, 0);
        return used.Contains(FieldSource(caseId, Gross)) || used.Contains(FieldSource(caseId, Expenses));
    }

    private static bool SameMoney(string a, string b) => Money(a, out decimal x) && Money(b, out decimal y) && x == y;

    // =========================================================
    // REVISED T07/T08 (5 November 2026): THE CASE FOLDER'S COMPUTATION PAGE
    // =========================================================
    // Three stages on the existing folder page, one active at a time:
    // A taxable income (income - deductions), B tax due (the chosen band's
    // base + rate x excess), C remaining payable (tax due - tax credit).
    // Amounts arrive from the collected summary / acquired papers through the
    // shared transfer (PlaceInput); Calculate loads the stage's formula from
    // the CHOSEN inputs, operation and band onto the existing calculator
    // (LoadStage) and prints; the printed tape attaches through AttachTape.
    // Nothing here knows or reveals which band or amount is right.

    public const string StageTaxable = "taxable";
    public const string StageTaxDue = "tax-due";
    public const string StageBalance = "balance";
    public static readonly string[] Stages = { StageTaxable, StageTaxDue, StageBalance };

    public const string RefuseResultSlot = "A printed result goes in the result slot.";
    public const string RefuseAmountField = "Put an amount from Bea's papers here.";
    public const string RefuseNoCertificateYet = "Bea's certificate isn't in the folder yet.";

    /// <summary>The sheet step a stage's printed result attaches to.</summary>
    public static string StageResult(string stage) =>
        stage == StageTaxable ? Taxable : stage == StageTaxDue ? TaxDue : stage == StageBalance ? Payable : null;

    /// <summary>Short stage titles (OFFICE_CASE_PLAY: up to four words).</summary>
    public static string StageTitle(string stage) =>
        stage == StageTaxable ? "Taxable income" : stage == StageTaxDue ? "Tax due" : stage == StageBalance ? "Remaining payable" : "";

    /// <summary>The stage holding a sheet step (the result, its inputs, or the band).</summary>
    public static string StageOf(string field)
    {
        switch (field)
        {
            case Gross: case Expenses: case Taxable: return StageTaxable;
            case Rule: case Excess: case Marginal: case TaxDue: return StageTaxDue;
            case Credits: case Payable: return StageBalance;
            default: return null;
        }
    }

    /// <summary>Structural only (never a grade): the stage's result is attached and current, with its band / credit chosen.</summary>
    public static bool StageDone(Level3ChapterCheckpoint state, CaseDefinition definition, string caseId, string stage)
    {
        Level3ComputationRecord record = Record(state, caseId);
        if (record == null) return false;
        if (stage == StageTaxable) return IsCurrent(state, definition, caseId, Taxable);
        if (stage == StageTaxDue) return !string.IsNullOrEmpty(record.ruleId) && IsCurrent(state, definition, caseId, TaxDue);
        if (stage == StageBalance) return !string.IsNullOrEmpty(record.creditChoice) && IsCurrent(state, definition, caseId, Payable);
        return false;
    }

    /// <summary>The first stage that is not structurally done (C once A and B are; it may still wait for the certificate).</summary>
    public static string ActiveStage(Level3ChapterCheckpoint state, CaseDefinition definition, string caseId)
    {
        foreach (string stage in Stages)
            if (!StageDone(state, definition, caseId, stage)) return stage;
        return StageBalance;
    }

    /// <summary>Stage C opens only once the case's withholding certificate is acquired (T08); A and B are always open.</summary>
    public static bool StageOpen(Level3ChapterCheckpoint state, string stage) =>
        stage != StageBalance || Level3CertificateRules.Acquired(state);

    /// <summary>The stage's result is on the sheet but one of its sources changed (a quiet Recalculate tag).</summary>
    public static bool StageNeedsRecalculating(Level3ChapterCheckpoint state, CaseDefinition definition, string caseId, string stage)
    {
        string result = StageResult(stage);
        return TapeOf(state, caseId, result) != null && IsStale(state, definition, caseId, result);
    }

    /// <summary>The operation an attached stage tape used ("-" / "+"), so a resumed page shows the player's own choice; null if none.</summary>
    public static string StageOperation(Level3ChapterCheckpoint state, string caseId, string stage)
    {
        if (stage != StageTaxable && stage != StageBalance) return null;
        Level3TapeRecord tape = TapeOf(state, caseId, StageResult(stage));
        if (tape?.expression == null || tape.expression.Count != 3 || tape.expression[1] == null || !tape.expression[1].IsOperator) return null;
        string op = tape.expression[1].op;
        return op == "-" || op == "+" ? op : null;
    }

    /// <summary>
    /// The current exact amount of a same-case source, resolved from the
    /// chapter and content (never from a payload or printed text): the
    /// collected summary's two totals or an acquired paper's printed row /
    /// total. Null when the case does not hold it.
    /// </summary>
    public static string CurrentAmount(Level3ChapterCheckpoint state, CaseDefinition definition, string sourceId)
    {
        if (state == null || definition == null || string.IsNullOrEmpty(sourceId)) return null;
        if (sourceId == WorksheetIncome || sourceId == WorksheetExpenses)
        {
            if (!Level3WorksheetCheck.Collected(state)) return null;
            WorksheetTotals(state, out string income, out string expenses);
            return sourceId == WorksheetIncome ? income : expenses;
        }
        if (sourceId.StartsWith("paper:", StringComparison.Ordinal))
        {
            int slash = sourceId.IndexOf('/');
            if (slash <= 6 || slash == sourceId.Length - 1) return null;
            return Level3ValueTransfer.FromPaper(definition, state, sourceId.Substring(6, slash - 6), sourceId.Substring(slash + 1))?.Amount;
        }
        return null;
    }

    /// <summary>
    /// Revised T07/T08: an amount the player transferred (select source then
    /// field, or drag) onto Income, Deductions or Tax credit. Mechanically
    /// invalid values (no Level 3 payload, a float, another case, other
    /// content, an unheld paper, a printed result, a changed amount) are
    /// refused; any same-case amount the case holds is accepted as draft work,
    /// right or wrong (the review judges it). The amount is re-resolved from
    /// its source; nothing is ever filled in for the player. Tax credit needs
    /// the certificate acquired; the old explanation step is no longer required
    /// (a saved creditExplained stays as it was).
    /// </summary>
    public static Level3CommandOutcome PlaceInput(Level3ChapterCheckpoint candidate, CaseDefinition definition, string field, DataValue value, out string reason)
    {
        if (!Ready(candidate, out reason)) return Level3CommandOutcome.Refused;
        if (field != Gross && field != Expenses && field != Credits) { reason = RefuseResultSlot; return Level3CommandOutcome.Refused; }
        if (field == Credits && !Level3CertificateRules.Acquired(candidate)) { reason = RefuseNoCertificateYet; return Level3CommandOutcome.Refused; }
        if (value?.Value is Level3CalculatorTransferValue) { reason = RefuseAmountField; return Level3CommandOutcome.Refused; }
        if (!Level3ValueTransfer.TryRead(value, candidate, out Level3TransferValue amount, out string refusal)) { reason = refusal; return Level3CommandOutcome.Refused; }
        if (amount.SourceId.StartsWith("tape:", StringComparison.Ordinal)) { reason = RefuseAmountField; return Level3CommandOutcome.Refused; }
        string current = CurrentAmount(candidate, definition, amount.SourceId);
        if (current == null) { reason = Level3ValueTransfer.RefuseNotHeld; return Level3CommandOutcome.Refused; }
        if (current != amount.Amount) { reason = RefuseChanged; return Level3CommandOutcome.Refused; }
        if (definition?.level3 == null || amount.TaxYear != definition.level3.taxYear) { reason = Level3ValueTransfer.RefuseRevision; return Level3CommandOutcome.Refused; }
        Level3ComputationRecord record = Record(candidate, Level3ChapterRules.BeaCaseId);
        string had = field == Gross ? record.grossChoice : field == Expenses ? record.expensesChoice : record.creditChoice;
        string hadAmount = field == Gross ? record.grossAmount : field == Expenses ? record.expensesAmount : record.creditAmount;
        if (had == amount.SourceId && SameMoney(hadAmount, current)) { reason = "already placed"; return Level3CommandOutcome.Duplicate; }
        if (field == Gross) { record.grossChoice = amount.SourceId; record.grossAmount = current; }
        else if (field == Expenses) { record.expensesChoice = amount.SourceId; record.expensesAmount = current; }
        else { record.creditChoice = amount.SourceId; record.creditAmount = current; }
        record.revision++;
        if (field == Credits) NotePayable(candidate, definition, record);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>
    /// Calculate: loads one stage's formula onto the existing calculator from
    /// the player's CHOSEN inputs, operation and band (sourced tokens, so the
    /// tape keeps its provenance) and presses = once. A: income (op)
    /// deductions. B: taxable - floor = x rate = + base = (steps a zero floor
    /// or base does not need are left out). C: tax due (op) tax credit.
    /// Returns null when a tape printed, else a short plain reason. Never an
    /// answer: a wrong band or input calculates exactly like a right one.
    /// </summary>
    public static string LoadStage(Level3Calculator calculator, Level3ChapterCheckpoint state, CaseDefinition definition, string stage, string op)
    {
        string caseId = Level3ChapterRules.BeaCaseId;
        Level3ComputationRecord record = Record(state, caseId);
        if (calculator == null || record == null || !Editable(state, caseId)) return "Bea's computation cannot be changed right now.";
        bool add = op == "+";
        if (stage == StageTaxable)
        {
            if (string.IsNullOrEmpty(record.grossChoice) || string.IsNullOrEmpty(record.expensesChoice)) return "Place both amounts first.";
            if (op != "-" && op != "+") return "Choose subtract or add.";
            calculator.Reset();
            calculator.SelectSource(FieldSource(caseId, Gross), "Income", record.grossAmount);
            calculator.Press(add ? Level3CalcKey.Add : Level3CalcKey.Subtract);
            calculator.SelectSource(FieldSource(caseId, Expenses), "Deductions", record.expensesAmount);
        }
        else if (stage == StageTaxDue)
        {
            Level3TapeRecord taxable = TapeOf(state, caseId, Taxable);
            if (taxable == null || IsStale(state, definition, caseId, Taxable)) return "Attach taxable income first.";
            Level3RuleDefinition rule = FindRule(definition, record.ruleId);
            if (rule == null) return "Choose a band first.";
            Money(rule.lower, out decimal lower);
            Money(rule.baseTax, out decimal baseTax);
            calculator.Reset();
            calculator.SelectSource(SheetSource(caseId, Taxable), SectionLabel(Taxable), taxable.result);
            if (lower != 0m)
            {
                calculator.Press(Level3CalcKey.Subtract);
                calculator.SelectSource(RuleSource(rule.ruleId, Lower), "Floor", rule.lower);
                calculator.Press(Level3CalcKey.Equals);
            }
            calculator.Press(Level3CalcKey.Multiply);
            calculator.SelectSourcePercent(RuleSource(rule.ruleId, Rate), "Rate", rule.rate);
            if (baseTax != 0m)
            {
                calculator.Press(Level3CalcKey.Equals);
                calculator.Press(Level3CalcKey.Add);
                calculator.SelectSource(RuleSource(rule.ruleId, Base), "Base tax", rule.baseTax);
            }
        }
        else if (stage == StageBalance)
        {
            if (!Level3CertificateRules.Acquired(state)) return RefuseNoCertificateYet;
            Level3TapeRecord due = TapeOf(state, caseId, TaxDue);
            if (due == null || IsStale(state, definition, caseId, TaxDue)) return "Attach tax due first.";
            if (string.IsNullOrEmpty(record.creditChoice)) return "Place the tax credit first.";
            if (op != "-" && op != "+") return "Choose subtract or add.";
            calculator.Reset();
            calculator.SelectSource(SheetSource(caseId, TaxDue), SectionLabel(TaxDue), due.result);
            calculator.Press(add ? Level3CalcKey.Add : Level3CalcKey.Subtract);
            calculator.SelectSource(FieldSource(caseId, Credits), "Tax credit", record.creditAmount);
        }
        else return "Choose a stage.";
        Level3CalcResult printed = calculator.Press(Level3CalcKey.Equals);
        return printed.Printed ? null : (printed.Issue ?? "Press Calculate again.");
    }

    /// <summary>"₱600,000.00", a negative "−₱187,500.00" (display only; the value stays exact, its sign kept).</summary>
    public static string Peso(string canonical)
    {
        if (string.IsNullOrEmpty(canonical)) return "";
        if (Level3Calculator.TryParse(canonical, out decimal value) && value < 0m)
            return "−₱" + Level3Calculator.Format(Level3Calculator.Canonical(-value));
        return "₱" + Level3Calculator.Format(canonical);
    }

    /// <summary>
    /// The selected band's formula with current values, for stage B:
    /// "22,500 + 20% × (600,000 − 400,000)". Built from the CHOSEN band and the
    /// attached taxable income (or the words "taxable income"), never from an
    /// expected answer.
    /// </summary>
    public static string BandWorking(Level3RuleDefinition rule, string taxable)
    {
        if (rule == null) return "";
        Money(rule.baseTax, out decimal baseTax);
        Money(rule.lower, out decimal lower);
        Money(rule.rate, out decimal rate);
        if (rate == 0m && baseTax == 0m) return "No tax in this band";
        string basis = Money(taxable, out decimal t) ? Whole(Level3Calculator.Canonical(t)) : "taxable income";
        string excess = lower == 0m ? basis : "(" + basis + " − " + Whole(rule.lower) + ")";
        string share = Percent(rule.rate) + " × " + excess;
        return baseTax == 0m ? share : Whole(rule.baseTax) + " + " + share;
    }
}
