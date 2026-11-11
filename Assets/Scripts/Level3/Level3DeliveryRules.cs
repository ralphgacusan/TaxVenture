/// <summary>What Bea's follow-through granted (T10), for presentation after the save.</summary>
public struct Level3DeliveryGrants
{
    public bool Changed;
    public bool AlreadyClaimed;
    public CampaignRewards.Change Trust;
}

/// <summary>
/// PURPOSE:
/// Bea's explanation and copy delivery (Level 3 T10). Pure: no Unity calls.
/// After the Auditor's approval (T09) the chapter waits in BeaDelivery with
/// her copy beside him ("atAuditor"); collecting it (Level3ReviewRules.
/// CollectCopy) makes it "carried". In the conference room she asks one
/// question about her balance; the player points to a figure on the
/// approved copy:
/// - Explain(): only Remaining payable answers "how much is left after the
///   tax already withheld?". It saves balanceExplained under its own action
///   id; it never changes a figure. Tax due / Tax credit are not commands
///   (Explain refuses them; RecordWrongAnswer saves each actual choice before its contextual hint).
/// - Deliver(): the actual copy handover, only after the saved explanation:
///   copy "done", BeaDelivery -> ClosingFinding (T14 owns that step), Bea
///   stays the active case and the company case is never admitted.
/// - ApplyDeliveryGrants(): the once-only +1..4 Public Trust follow-through
///   claim on the same candidate save (story runs only; the caller writes it
///   before anything shows). No coins, XP, approval or chapter completion.
/// Every command validates phase, case, approval, copy phase and action id;
/// a repeat is a Duplicate that changes nothing.
/// </summary>
public static class Level3DeliveryRules
{
    private const string Bea = Level3ChapterRules.BeaCaseId;

    public const string WrongAnswerPrefix = "Level_03/bea-balance-wrong/";

    /// <summary>Forgive two slips, then reduce the earned +4 by one per three wrong choices; delivery still earns +1.</summary>
    public static int FollowThroughTrust(int wrongAnswers) => System.Math.Max(1, 4 - System.Math.Max(0, wrongAnswers) / 3);

    /// <summary>A real wrong figure choice, saved once for this displayed question. Invalid/stale callbacks change nothing.</summary>
    public static Level3CommandOutcome RecordWrongAnswer(Level3ChapterCheckpoint candidate, string evidenceId, int attempt, out string reason)
    {
        reason = Problem(candidate);
        if (reason != null || Answers(evidenceId) || System.Array.IndexOf(Evidence, evidenceId) < 0 || attempt <= 0)
            return Level3CommandOutcome.Refused;
        string action = WrongAnswerPrefix + attempt;
        if (candidate.actionIds.Contains(action)) return Level3CommandOutcome.Duplicate;
        Level3CaseProgress bea = candidate.Case(Bea);
        if (bea.balanceExplained || attempt != bea.balanceWrongAnswers + 1) return Level3CommandOutcome.Refused;
        bea.balanceWrongAnswers = attempt;
        candidate.actionIds.Add(action);
        return Level3CommandOutcome.Applied;
    }

    public const string ExplainAction = "Level_03/bea-balance-explained";
    public const string DeliverAction = "Level_03/bea-copy-delivered";
    public const string DeliveredMilestone = "bea-copy-delivered";
    public const string CopyCarried = "carried";
    public const string CopyDone = "done";

    /// <summary>The figures on the copy's tax and payment portion the player can point to.</summary>
    public const string TaxDue = "tax-due";
    public const string Credit = "tax-credit";
    public const string Payable = "remaining-payable";
    public static readonly string[] Evidence = { TaxDue, Credit, Payable };

    public const string RefuseNotNow = "Bea's copy isn't ready to explain yet.";
    public const string RefuseExplainFirst = "Answer Bea's question first.";

    /// <summary>Where Bea's resolution stands.</summary>
    public enum Stage { None, CopyAtAuditor, Explain, HandOver, Delivered }

    public static Stage StageOf(Level3ChapterCheckpoint state)
    {
        Level3CaseProgress bea = state?.Case(Bea);
        if (bea == null || bea.review == null || !bea.review.approved) return Stage.None;
        // Only the saved delivery itself counts: an unsupported "done" without it is never treated as delivered.
        if (bea.copyPhase == CopyDone)
            return Level3ChapterRules.PhaseOf(state) >= Level3Phase.ClosingFinding && state.actionIds != null && state.actionIds.Contains(DeliverAction)
                ? Stage.Delivered : Stage.None;
        if (Level3ChapterRules.PhaseOf(state) != Level3Phase.BeaDelivery) return Stage.None;
        if (bea.copyPhase == Level3ReviewRules.CopyAtAuditor) return Stage.CopyAtAuditor;
        if (bea.copyPhase != CopyCarried) return Stage.None;
        return bea.balanceExplained ? Stage.HandOver : Stage.Explain;
    }

    public static bool Delivered(Level3ChapterCheckpoint state) => StageOf(state) == Stage.Delivered;

    public static InventoryResult ReleaseCopyInventory(CampaignInventoryRecord inventory, InventoryCatalogue catalogue, Level3ChapterCheckpoint state)
    {
        if (!Delivered(state)) return new InventoryResult { Outcome = InventoryOutcome.NoChange, ItemId = InventoryCatalogue.CaseFolder };
        InventoryResult result = InventoryRules.Remove(inventory, catalogue, InventoryCatalogue.CaseFolder, null);
        InventoryRules.Unpin(inventory, InventoryCatalogue.CaseFolder);
        return result;
    }

    /// <summary>Bea can still show her acquired originals after receiving the copy.
    /// This read-only view is not an owned inventory item and never recreates the delivered folder.</summary>
    public static InventoryItemRecord ReadDeliveredRecords(Level3ChapterCheckpoint state, CaseDefinition definition)
    {
        if (!Delivered(state) || definition?.caseId != Bea) return null;
        var record = new InventoryItemRecord { itemId = InventoryCatalogue.CaseFolder };
        foreach (ClientRecord paper in Level3RecordRules.Build(definition, state.Case(Bea).acquiredSourceIds))
        {
            var lines = new System.Collections.Generic.List<string> { paper.issuer, paper.reference };
            if (paper.rows != null)
                foreach (ClientRecordRow row in paper.rows) lines.Add(row.label + ": " + row.value);
            for (int first = 0; first < lines.Count; first += 6)
                record.referencePages.Add(new InventoryReferencePage { id = paper.id + "/" + first, title = paper.title,
                    lines = lines.GetRange(first, System.Math.Min(6, lines.Count - first)) });
        }
        return record;
    }

    /// <summary>The approved copy's frozen figure for a piece of evidence ("" when unknown or missing).</summary>
    public static string Figure(Level3ChapterCheckpoint state, string evidenceId)
    {
        Level3ReviewRecord review = state?.Case(Bea)?.review;
        if (review == null || !review.approved) return "";
        switch (evidenceId)
        {
            case TaxDue: return review.taxDue ?? "";
            case Credit: return review.credits ?? "";
            case Payable: return review.remainingPayable ?? "";
            default: return "";
        }
    }

    /// <summary>The copy's own label for a figure (the Tax and payment leaf's wording).</summary>
    public static string Label(string evidenceId) =>
        evidenceId == TaxDue ? "Tax due" : evidenceId == Credit ? "Tax credit (withheld)" : evidenceId == Payable ? "Remaining payable" : "";

    /// <summary>The figure answers her question (only the balance left after the withheld credit).</summary>
    public static bool Answers(string evidenceId) => evidenceId == Payable;

    /// <summary>Phase, case, approval with frozen figures, a carried copy and a usable record.</summary>
    private static string Problem(Level3ChapterCheckpoint candidate)
    {
        if (candidate == null || Level3ChapterRules.Problem(candidate) != null) return RefuseNotNow;
        Level3CaseProgress bea = candidate.Case(Bea);
        if (Level3ChapterRules.PhaseOf(candidate) != Level3Phase.BeaDelivery || candidate.activeCaseId != Bea || bea?.review == null ||
            !bea.review.approved || string.IsNullOrEmpty(bea.review.remainingPayable)) return RefuseNotNow;
        if (bea.copyPhase != CopyCarried) return RefuseNotNow;
        return null;
    }

    /// <summary>
    /// The player pointed to Remaining payable: saved as explained (her copy
    /// and its figures unchanged). Any other figure is Refused here (the
    /// conversation gives Hint and the same question waits).
    /// </summary>
    public static Level3CommandOutcome Explain(Level3ChapterCheckpoint candidate, string evidenceId, out string reason)
    {
        reason = null;
        Level3CaseProgress bea = candidate?.Case(Bea);
        if (bea != null && (bea.balanceExplained || bea.copyPhase == CopyDone) && candidate.actionIds.Contains(ExplainAction))
        { reason = "already explained"; return Level3CommandOutcome.Duplicate; }
        reason = Problem(candidate);
        if (reason != null) return Level3CommandOutcome.Refused;
        if (System.Array.IndexOf(Evidence, evidenceId) < 0 || !Answers(evidenceId)) { reason = "not the balance"; return Level3CommandOutcome.Refused; }
        bea.balanceExplained = true;
        candidate.actionIds.Add(ExplainAction);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>
    /// The copy reached Bea (a drop on her or the icon tap while she waits for it), after the saved
    /// explanation: copy done and the chapter moves to her closing step (T14). Nothing else changes.
    /// </summary>
    public static Level3CommandOutcome Deliver(Level3ChapterCheckpoint candidate, out string reason)
    {
        reason = null;
        if (candidate?.actionIds != null && candidate.actionIds.Contains(DeliverAction) && Delivered(candidate))
        { reason = "already delivered"; return Level3CommandOutcome.Duplicate; }
        reason = Problem(candidate);
        if (reason != null) return Level3CommandOutcome.Refused;
        Level3CaseProgress bea = candidate.Case(Bea);
        if (!bea.balanceExplained || !candidate.actionIds.Contains(ExplainAction)) { reason = RefuseExplainFirst; return Level3CommandOutcome.Refused; }
        Level3CommandOutcome step = Level3ChapterRules.TryAdvance(candidate, Level3Phase.BeaDelivery, Level3Phase.ClosingFinding, DeliverAction, out reason);
        if (step != Level3CommandOutcome.Applied) return step;
        bea.copyPhase = CopyDone;
        if (!candidate.milestones.Contains(DeliveredMilestone)) candidate.milestones.Add(DeliveredMilestone);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>The once-only earned Public Trust of Bea's follow-through on a candidate save.</summary>
    public static Level3DeliveryGrants ApplyDeliveryGrants(PlayerProgressData candidate, Level3ChapterCheckpoint state)
    {
        var grants = new Level3DeliveryGrants();
        if (candidate == null || !candidate.HasCampaign || !Delivered(state) || state.Case(Bea)?.balanceExplained != true) return grants;
        if (!CampaignRewards.CompleteActivity(candidate, CampaignRewards.Level3, CampaignRewards.BeaFollowThrough))
        {
            grants.AlreadyClaimed = true;
            return grants;
        }
        grants.Changed = true;
        grants.Trust = CampaignRewards.GrantBeaFollowThrough(candidate, state.Case(Bea).balanceWrongAnswers);
        return grants;
    }
}
