/// <summary>
/// One Level 3 amount as it travels through the shared value transfer
/// (ValueTransferManager / DataValue): the exact invariant decimal and where
/// it came from. Never a float and never an answer: a plausible wrong amount
/// of the same case travels exactly like a right one.
/// </summary>
public sealed class Level3TransferValue
{
    public string CaseId;
    /// <summary>Level3ComputationRules' choice ids: "paper:B-WHT/BWithheldCredit", "worksheet:income", "tape:bea/taxable-income".</summary>
    public string SourceId;
    public string Label;
    /// <summary>Exact canonical decimal (Level3Calculator.Canonical), e.g. "12500" or "62500.5".</summary>
    public string Amount;
    public string ContentRevision;
    public int TaxYear;
}

/// <summary>A book band identity, never a percentage substituted for the complete rule.</summary>
public sealed class Level3BandTransferValue
{
    public string CaseId, RuleId, Revision;
    public int Year;
}

/// <summary>Immutable selection snapshot for the physical calculator. No view/component
/// is stored in a payload, so selecting another source cannot rewrite an old drag.</summary>
public sealed class Level3CalculatorTransferValue
{
    public string Context { get; }
    public string SourceId { get; }
    public string Label { get; }
    public string Amount { get; }
    public bool Percent { get; }
    public string DraftKey { get; }

    public Level3CalculatorTransferValue(string context, string sourceId, string label, string amount, bool percent = false, string draftKey = null)
    { Context = context; SourceId = sourceId; Label = label; Amount = amount; Percent = percent; DraftKey = draftKey; }

    public bool Matches(Level3CalculatorTransferValue current) => current != null &&
        Context == current.Context && SourceId == current.SourceId && Amount == current.Amount &&
        Label == current.Label && Percent == current.Percent && DraftKey == current.DraftKey;

    public bool Matches(DataValue value) => value?.Type == DataValueType.Number &&
        value.SemanticKey == "Level3:Calculator/" + SourceId && value.Value is Level3CalculatorTransferValue payload && Matches(payload);

    // Both click placement and drag/drop enter this exact command after the
    // owner resolves the current source catalogue (not formatted text).
    public static bool TryInsert(DataValue value, Level3CalculatorTransferValue current, Level3Calculator calculator)
    {
        if (!(value?.Value is Level3CalculatorTransferValue selected) || selected.DraftKey != null ||
            current == null || !current.Matches(value) || calculator == null) return false;
        Level3CalcResult result = selected.Percent
            ? calculator.SelectSourcePercent(selected.SourceId, selected.Label, selected.Amount)
            : calculator.SelectSource(selected.SourceId, selected.Label, selected.Amount);
        return result.Changed;
    }
}

/// <summary>
/// PURPOSE:
/// The narrow Level 3 boundary of the existing select-source -> place-value
/// transfer (revised T03; T07 adds the computation destinations). DataValue
/// already carries an object: Level 3 puts a Level3TransferValue there, so the
/// exact decimal, case, source, tax year and content revision travel together.
///
/// - Wrap: a DataValue (Number) whose SemanticKey starts "Level3:", so legacy
///   float destinations (TaxReturnFieldSlot, ComputerFieldSlot) never take it,
///   and whose AsFloat() is never a Level 3 money authority.
/// - FromPaper / FromTotal: exact amounts straight from the case content's
///   rows and totals of a paper this case acquired (never from printed text).
/// - TryRead: a destination's only way in. It refuses what is mechanically
///   invalid (no Level 3 payload, a legacy float, another case, other content,
///   a malformed amount, a source this case does not hold) and nothing else:
///   correctness is the review's (T09), never the drop's.
/// Pure C#: the regression suite runs it directly.
/// </summary>
public static class Level3ValueTransfer
{
    /// <summary>The real calculator accepts a reusable summary amount without filling a folder field.</summary>
    public static bool CanInsertSummary(DataValue value, Level3ChapterCheckpoint state, CaseDefinition definition)
    {
        if (!Level3ComputationRules.Editable(state, Level3ChapterRules.BeaCaseId) || Level3PracticeRules.LessonDue(state) ||
            !TryRead(value, state, out Level3TransferValue amount, out _)) return false;
        if (value.Type != DataValueType.Number || value.SemanticKey != KeyPrefix + amount.SourceId) return false;
        if (definition?.caseId != amount.CaseId || definition.level3 == null || amount.TaxYear != definition.level3.taxYear ||
            amount.ContentRevision != definition.level3.contentRevision) return false;
        if (amount.SourceId != Level3ComputationRules.WorksheetIncome && amount.SourceId != Level3ComputationRules.WorksheetExpenses) return false;
        return amount.Amount == Level3ComputationRules.CurrentAmount(state, definition, amount.SourceId);
    }

    public static bool InsertSummary(DataValue value, Level3ChapterCheckpoint state, CaseDefinition definition, Level3Calculator calculator)
    {
        if (calculator == null || !CanInsertSummary(value, state, definition)) return false;
        var amount = (Level3TransferValue)value.Value;
        return calculator.SelectSource(amount.SourceId, amount.Label, amount.Amount).Changed;
    }

    public const string BandKey = "Level3:AnnualBand";
    public static DataValue Band(Level3ChapterCheckpoint state, CaseDefinition definition, string ruleId)
    {
        Level3RuleDefinition rule = Level3ComputationRules.FindRule(definition, ruleId);
        if (rule == null || state?.activeCaseId != definition.caseId || !Level3WorksheetCheck.Collected(state)) return null;
        return new DataValue(new Level3BandTransferValue { CaseId = definition.caseId, RuleId = ruleId,
            Revision = definition.level3.contentRevision, Year = definition.level3.taxYear },
            Level3ComputationRules.BandLabel(rule), DataValueType.Enum, BandKey);
    }
    public static bool ReadBand(DataValue value, Level3ChapterCheckpoint state, CaseDefinition definition, out string ruleId)
    {
        ruleId = null;
        if (!(value?.Value is Level3BandTransferValue band) || value.Type != DataValueType.Enum || value.SemanticKey != BandKey ||
            !Level3ComputationRules.Editable(state, Level3ChapterRules.BeaCaseId) || Level3PracticeRules.LessonDue(state) ||
            definition?.level3 == null || band.CaseId != state.activeCaseId || band.CaseId != definition.caseId ||
            band.Year != definition.level3.taxYear || band.Revision != definition.level3.contentRevision || band.Revision != state.contentRevision ||
            Level3ComputationRules.FindRule(definition, band.RuleId) == null) return false;
        ruleId = band.RuleId;
        return true;
    }
    public static DataValue ComputationSource(Level3ChapterCheckpoint state, CaseDefinition definition, string id, string label)
    {
        if (!Level3ComputationRules.TryCurrentValue(state, definition, definition?.caseId, id, out decimal exact)) return null;
        var value = Wrap(new Level3TransferValue { CaseId = definition.caseId, SourceId = id, Label = label,
            Amount = Level3Calculator.Canonical(exact), ContentRevision = definition.level3.contentRevision, TaxYear = definition.level3.taxYear });
        if (id.StartsWith(Level3ComputationRules.RulePrefix, System.StringComparison.Ordinal) && id.EndsWith("/" + Level3ComputationRules.Rate, System.StringComparison.Ordinal))
            value.DisplayText = Level3ComputationRules.Percent(Level3Calculator.Canonical(exact));
        return CanInsertComputation(value, state, definition) ? value : null;
    }
    public static bool CanInsertComputation(DataValue value, Level3ChapterCheckpoint state, CaseDefinition definition)
    {
        if (CanInsertSummary(value, state, definition)) return true;
        if (!Level3ComputationRules.Editable(state, Level3ChapterRules.BeaCaseId) || Level3PracticeRules.LessonDue(state) ||
            !TryRead(value, state, out Level3TransferValue amount, out _) || definition?.level3 == null ||
            value.Type != DataValueType.Number || value.SemanticKey != KeyPrefix + amount.SourceId || amount.CaseId != definition.caseId ||
            amount.TaxYear != definition.level3.taxYear || amount.ContentRevision != definition.level3.contentRevision) return false;
        string id = amount.SourceId, caseId = definition.caseId;
        bool allowed = id == Level3ComputationRules.FieldSource(caseId, Level3ComputationRules.Credits);
        foreach (string field in new[] { Level3ComputationRules.Taxable, Level3ComputationRules.TaxDue })
            if (id == Level3ComputationRules.SheetSource(caseId, field))
                allowed = Level3ComputationRules.IsCurrent(state, definition, caseId, field);
        string selected = Level3ComputationRules.Record(state, caseId)?.ruleId;
        foreach (string part in new[] { Level3ComputationRules.Lower, Level3ComputationRules.Rate, Level3ComputationRules.Base })
            if (selected != null && id == Level3ComputationRules.RuleSource(selected, part)) allowed = true;
        return allowed && Level3ComputationRules.TryCurrentValue(state, definition, caseId, id, out decimal current) &&
            amount.Amount == Level3Calculator.Canonical(current);
    }
    public static bool InsertComputation(DataValue value, Level3ChapterCheckpoint state, CaseDefinition definition, Level3Calculator calculator)
    {
        if (calculator == null || !CanInsertComputation(value, state, definition)) return false;
        var amount = (Level3TransferValue)value.Value;
        return amount.SourceId.StartsWith(Level3ComputationRules.RulePrefix, System.StringComparison.Ordinal) && amount.SourceId.EndsWith("/" + Level3ComputationRules.Rate, System.StringComparison.Ordinal)
            ? calculator.SelectSourcePercent(amount.SourceId, amount.Label, amount.Amount).Changed
            : calculator.SelectSource(amount.SourceId, amount.Label, amount.Amount).Changed;
    }

    public const string KeyPrefix = "Level3:";
    public const string RefuseNotLevel3 = "That value isn't one of this case's amounts.";
    public const string RefuseFloat = "That number has no source. Pick it from one of the papers.";
    public const string RefuseForeign = "That amount belongs to another client's case.";
    public const string RefuseRevision = "That amount comes from another version of these papers.";
    public const string RefuseMalformed = "That amount can't be read exactly.";
    public const string RefuseNotHeld = "That paper isn't in this case's folder yet.";

    public static DataValue Wrap(Level3TransferValue value) =>
        value == null ? null : new DataValue(value, Level3Calculator.Format(value.Amount), DataValueType.Number, KeyPrefix + value.SourceId);

    /// <summary>"paper:{sourceId}/{fieldKey}": one printed row of an acquired paper, exactly as the content holds it (null otherwise).</summary>
    public static Level3TransferValue FromPaper(CaseDefinition definition, Level3ChapterCheckpoint chapter, string sourceId, string fieldKey)
    {
        Level3CaseContent content = definition?.level3;
        if (content == null || !content.IsSet || !Holds(chapter, definition.caseId, sourceId)) return null;
        Level3RowDefinition row = content.rows?.Find(r => r != null && r.sourceId == sourceId && r.fieldKey == fieldKey);
        string amount = row?.amount;
        if (amount == null)
            amount = content.totals?.Find(t => t != null && t.sourceId == sourceId && t.fieldKey == fieldKey)?.amount;
        if (amount == null || !Level3Calculator.TryParse(amount, out decimal exact)) return null;
        return new Level3TransferValue
        {
            CaseId = definition.caseId,
            SourceId = Level3ComputationRules.CertificateChoice(sourceId, fieldKey),
            Label = Label(definition, sourceId, fieldKey),
            Amount = Level3Calculator.Canonical(exact),
            ContentRevision = content.contentRevision,
            TaxYear = content.taxYear
        };
    }

    /// <summary>
    /// The only way a destination reads a transferred Level 3 amount. True:
    /// a same-case, current-content, exact amount from a source this case holds
    /// (right or wrong). False: refusal says what is mechanically wrong.
    /// </summary>
    public static bool TryRead(DataValue value, Level3ChapterCheckpoint chapter, out Level3TransferValue amount, out string refusal)
    {
        amount = null;
        refusal = null;
        if (value == null) { refusal = RefuseNotLevel3; return false; }
        if (value.Value is float || value.Value is double) { refusal = RefuseFloat; return false; }
        if (!(value.Value is Level3TransferValue candidate)) { refusal = RefuseNotLevel3; return false; }
        if (chapter == null || Level3ChapterRules.Problem(chapter) != null) { refusal = RefuseNotLevel3; return false; }
        if (candidate.CaseId != chapter.activeCaseId) { refusal = RefuseForeign; return false; }
        if (candidate.ContentRevision != chapter.contentRevision) { refusal = RefuseRevision; return false; }
        if (string.IsNullOrEmpty(candidate.Amount) || !Level3Calculator.TryParse(candidate.Amount, out decimal exact) ||
            Level3Calculator.Canonical(exact) != candidate.Amount)
        {
            refusal = RefuseMalformed;
            return false;
        }
        if (!SourceHeld(chapter, candidate)) { refusal = RefuseNotHeld; return false; }
        amount = candidate;
        return true;
    }

    /// <summary>The source is this case's own: an acquired paper, its collected worksheet, or one of its attached tapes.</summary>
    private static bool SourceHeld(Level3ChapterCheckpoint chapter, Level3TransferValue value)
    {
        string id = value.SourceId ?? "";
        Level3CaseProgress progress = chapter.Case(value.CaseId);
        if (progress == null) return false;
        if (id.StartsWith(Level3ComputationRules.RulePrefix, System.StringComparison.Ordinal))
            return !string.IsNullOrEmpty(progress.computation?.ruleId) && id.StartsWith(Level3ComputationRules.RulePrefix + progress.computation.ruleId + "/", System.StringComparison.Ordinal);
        if (id.StartsWith(Level3ComputationRules.SheetPrefix, System.StringComparison.Ordinal))
        {
            string field = Level3ComputationRules.FieldOf(value.CaseId, id);
            return field != null && chapter.Tape(Level3ComputationRules.Destination(value.CaseId, field)) != null;
        }
        if (id == Level3ComputationRules.FieldSource(value.CaseId, Level3ComputationRules.Credits)) return !string.IsNullOrEmpty(progress.computation?.creditChoice);
        if (id.StartsWith("paper:"))
        {
            int slash = id.IndexOf('/');
            return slash > 6 && progress.acquiredSourceIds.Contains(id.Substring(6, slash - 6));
        }
        if (id.StartsWith("worksheet:")) return progress.workIds.Contains(Level3WorksheetCheck.WorksheetWorkId);
        if (id.StartsWith("tape:"))
        {
            Level3TapeRecord tape = chapter.Tape(id.Substring(5));
            return tape != null && tape.caseId == value.CaseId;
        }
        return false;
    }

    private static bool Holds(Level3ChapterCheckpoint chapter, string caseId, string sourceId) =>
        chapter?.Case(caseId)?.acquiredSourceIds?.Contains(sourceId) == true;

    private static string Label(CaseDefinition definition, string sourceId, string fieldKey)
    {
        Level3SourceDefinition source = definition.level3.sources?.Find(s => s != null && s.sourceId == sourceId);
        DocumentDefinition document = source != null ? definition.documents?.Find(d => d != null && d.documentName == source.documentName) : null;
        return document?.fields?.Find(f => f != null && f.semanticKey == fieldKey)?.label ?? fieldKey;
    }

    /// <summary>A selection belongs to an owner when its source component sits under that owner's transform (used to clear only one's own).</summary>
    public static bool OwnsSelection(object sourceComponent, UnityEngine.Transform owner) =>
        owner != null && sourceComponent is UnityEngine.Component component && component != null && component.transform.IsChildOf(owner);
}
