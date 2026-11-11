/// <summary>
/// PURPOSE:
/// Disposable Level 3 chapters for a direct Editor attempt (revised T03),
/// chosen from Tools > TaxVenture > Level 3 (Assets/Editor/Level3EditorFixtureMenu.cs).
/// A direct Editor attempt never saves, so a fixture never reaches a
/// campaign, a reward or the real save.
///
/// T16 (6 November 2026): also compiled into player builds, because two of
/// these chapters are the Tutorial Hub's Level 3 practice starts (TrainingDrill
/// .fixture: bea-table-restored for the paper machine, office-practice for the
/// three office stages). A practice run never saves either (Level3Checkpointer:
/// TrainingSession.IsPracticing is never a story run); the chapter lives in
/// memory only. Level3Checkpointer.EditorFixture stays Editor-attempt only.
///
/// FirstOfficeUse: Bea's chapter at her first office calculation. It is built
/// with the real rule commands (assignment, handover, the paper machine's
/// lever filings, the three batch checks, collection: revised T06), using the
/// authored correct plan below; the calculator is not taught yet. Null when the
/// case content does not produce that state.
///
/// BeaEncounter (T04 buildup): the reception assignment done, Bea's records
/// not handed over yet, so her whole first encounter plays at her table.
/// BeaTableRestored: her records already handed over (as after a reload), so
/// the table restores quietly to its concise continuation.
///
/// BeaCostsBatch / BeaQuestionsBatch (T05; revised T06): her earlier batches
/// filed through the real lever command (Level3WorksheetRules.File) and
/// accepted through the real Check batch (Level3WorksheetCheck.CheckBatch), so
/// the machine opens on the costs (R3-R5, COMPARE on R5) or the questions
/// (R6/R7, Why? and COMPARE) for an Editor look.
/// BeaEarningsMistake (T06): the earnings filed with Menu illustration fees
/// deducted (a deliberate wrong choice), not checked yet: CHECK BATCH marks it.
/// BeaSummaryReady (T06): all three batches filed right and accepted; the
/// summary is ready on the output ledge, not collected.
/// Nothing is saved: a direct Editor attempt never writes.
/// </summary>
public static class Level3EditorFixtures
{
    public const string FirstOfficeUseId = "first-office-use";
    public const string BeaEncounterId = "bea-encounter";
    public const string BeaTableRestoredId = "bea-table-restored";
    public const string BeaCostsBatchId = "bea-costs-batch";
    public const string BeaQuestionsBatchId = "bea-questions-batch";
    public const string BeaEarningsMistakeId = "bea-earnings-mistake";
    public const string BeaSummaryReadyId = "bea-summary-ready";
    /// <summary>T16: the office practice start: FirstOfficeUse with the calculator already taught (the drill shows no first-use example).</summary>
    public const string OfficePracticeId = "office-practice";

    /// <summary>The authored correct plan (FIXTURE_CONTRACT / REVISION_PLAN registry), R1-R7 in order; reasons for R6/R7.</summary>
    private static readonly string[] Plan =
    {
        Level3WorksheetRules.Income, Level3WorksheetRules.Income, Level3WorksheetRules.Deduct, Level3WorksheetRules.Deduct,
        Level3WorksheetRules.Deduct, Level3WorksheetRules.LeaveOut, Level3WorksheetRules.LeaveOut
    };

    private static string PlannedReason(string rowId) => rowId == "R6" ? "personal-purpose" : rowId == "R7" ? "already-represented" : "";

    public static Level3ChapterCheckpoint Build(string fixtureId, CaseDefinition definition) =>
        fixtureId == FirstOfficeUseId ? FirstOfficeUse(definition)
        : fixtureId == BeaEncounterId ? BeaEncounter(definition, handedOver: false)
        : fixtureId == BeaTableRestoredId ? BeaEncounter(definition, handedOver: true)
        : fixtureId == BeaCostsBatchId ? MachineBatch(definition, Level3WorksheetRules.CostsBatch)
        : fixtureId == BeaQuestionsBatchId ? MachineBatch(definition, Level3WorksheetRules.QuestionsBatch)
        : fixtureId == BeaEarningsMistakeId ? EarningsMistake(definition)
        : fixtureId == BeaSummaryReadyId ? SummaryReady(definition)
        : fixtureId == OfficePracticeId ? OfficePractice(definition)
        : null;

    /// <summary>
    /// T16: Bea's collected summary at the office desk, the calculator taught: stages A, B and C to practise.
    /// Teaching pass: the drill is itself practice, so its tax-table and withholding examples count as given too.
    /// </summary>
    public static Level3ChapterCheckpoint OfficePractice(CaseDefinition bea)
    {
        Level3ChapterCheckpoint s = FirstOfficeUse(bea);
        if (s == null) return null;
        s.calculatorTaught = true;
        s.milestones.Add(Level3PracticeRules.TaxTaughtMilestone);
        s.milestones.Add(Level3PracticeRules.CreditTaughtMilestone);
        return s;
    }

    /// <summary>T05/T06: Bea's machine open on the given batch, the batches before it filed with the lever command and accepted by a real Check batch.</summary>
    public static Level3ChapterCheckpoint MachineBatch(CaseDefinition bea, string openBatch)
    {
        Level3ChapterCheckpoint s = BeaEncounter(bea, handedOver: true);
        if (s == null) return null;
        var rows = Level3WorksheetRules.RowsOf(bea);
        foreach (string batch in Level3WorksheetRules.Batches)
        {
            if (batch == openBatch) break;
            if (!FileBatch(s, rows, batch) || !Accept(s, bea, batch)) return null;
        }
        return Level3WorksheetRules.OpenBatch(s) == openBatch ? s : null;
    }

    /// <summary>T06: the earnings filed with Menu illustration fees deducted (a plausible wrong choice), not checked.</summary>
    public static Level3ChapterCheckpoint EarningsMistake(CaseDefinition bea)
    {
        Level3ChapterCheckpoint s = BeaEncounter(bea, handedOver: true);
        if (s == null) return null;
        var rows = Level3WorksheetRules.RowsOf(bea);
        if (!File(s, rows, "R1", Level3WorksheetRules.Income) || !File(s, rows, "R2", Level3WorksheetRules.Deduct)) return null;
        return s;
    }

    /// <summary>T06: every batch filed right and accepted: the summary waits on the output ledge (WorksheetReady, not collected).</summary>
    public static Level3ChapterCheckpoint SummaryReady(CaseDefinition bea)
    {
        Level3ChapterCheckpoint s = BeaEncounter(bea, handedOver: true);
        if (s == null) return null;
        var rows = Level3WorksheetRules.RowsOf(bea);
        foreach (string batch in Level3WorksheetRules.Batches)
            if (!FileBatch(s, rows, batch) || !Accept(s, bea, batch)) return null;
        return Level3ChapterRules.PhaseOf(s) == Level3Phase.WorksheetReady ? s : null;
    }

    private static bool FileBatch(Level3ChapterCheckpoint s, System.Collections.Generic.List<Level3WorksheetRow> rows, string batch)
    {
        foreach (string row in Level3WorksheetRules.RowsInBatch(batch))
            if (!File(s, rows, row, Plan[System.Array.IndexOf(Level3WorksheetRules.RowIds, row)])) return false;
        return true;
    }

    private static bool File(Level3ChapterCheckpoint s, System.Collections.Generic.List<Level3WorksheetRow> rows, string row, string treatment) =>
        Level3WorksheetRules.File(s, rows, new Level3Filing { RowId = row, Treatment = treatment, Reason = PlannedReason(row), BasedOn = s.worksheet.revision }, out _)
        == Level3CommandOutcome.Applied;

    private static bool Accept(Level3ChapterCheckpoint s, CaseDefinition bea, string batch) =>
        Level3WorksheetCheck.CheckBatch(s, bea, batch, s.worksheet.revision, out Level3BatchEvaluation eval, out _) == Level3WorksheetCheck.CheckOutcome.Checked &&
        eval.Accepted;

    /// <summary>Bea's chapter right before her first encounter (assigned), or right after her saved handover.</summary>
    public static Level3ChapterCheckpoint BeaEncounter(CaseDefinition bea, bool handedOver)
    {
        if (bea == null || bea.caseId != Level3ChapterRules.BeaCaseId) return null;
        Level3ChapterCheckpoint s = Level3ChapterRules.CreateEntry();
        if (Level3ChapterRules.AssignBea(s, out _) != Level3CommandOutcome.Applied) return null;
        if (handedOver && Level3ChapterRules.CollectBeaRecords(s, out _) != Level3CommandOutcome.Applied) return null;
        return s;
    }

    public static Level3ChapterCheckpoint FirstOfficeUse(CaseDefinition bea)
    {
        Level3ChapterCheckpoint s = SummaryReady(bea);
        if (s == null || Level3WorksheetCheck.Acquire(s, bea, out _) != Level3CommandOutcome.Applied) return null;
        return Level3ChapterRules.PhaseOf(s) == Level3Phase.BeaComputation && !s.calculatorTaught ? s : null;
    }
}
