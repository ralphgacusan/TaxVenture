using System;
using System.Collections.Generic;

/// <summary>One case's review clock settings (Levels/Level_03/review.json "cases").</summary>
[Serializable]
public class Level3ReviewCaseContent
{
    public string caseId;
    public ReviewClockDefinition clock = new ReviewClockDefinition();
}

/// <summary>What the Auditor says about one root cause: the line, the book section (no client value).</summary>
[Serializable]
public class Level3ReviewRootNote
{
    public string id;
    public string note;
}

/// <summary>The Auditor's review lines (CONTENT "Review returned" / "Bea approval").</summary>
[Serializable]
public class Level3AuditorLines
{
    public List<string> approved = new List<string>();
    public string returned = "Your work is kept. Start with the line I marked, then check the calculations that depend on it.";
    public string hold = "Thank you for holding it. Something on the sheet is still open, so it stays with you.";
    public string falseHold = "You held this computation, but I can't find an open problem on it.";
    public string staleStamp = "Your stamp is older than this sheet. Something changed after you stamped it.";
}

/// <summary>Levels/Level_03/review.json: the clocks, the Auditor's lines and the root-cause notes.</summary>
[Serializable]
public class Level3ReviewContent
{
    public List<Level3ReviewCaseContent> cases = new List<Level3ReviewCaseContent>();
    public Level3AuditorLines auditor = new Level3AuditorLines();
    public List<Level3ReviewRootNote> roots = new List<Level3ReviewRootNote>();

    public ReviewClockDefinition Clock(string caseId) => cases?.Find(c => c != null && c.caseId == caseId)?.clock;
}

public enum Level3ReviewOutcome { None, Approved, SupportedHold, PrematureApproval, FalseHold, StaleStamp }

/// <summary>Why a line is the review's root: empty, needing recalculation, typed by hand, wrong, or the credit used as an expense.</summary>
public enum Level3RootKind { None, Missing, Stale, Typed, Wrong, AsExpense }

/// <summary>One decided hand-in: its outcome, the earliest root (if any) and what it charges.</summary>
public sealed class Level3ReviewDecision
{
    public Level3ReviewOutcome Outcome;
    /// <summary>The case-scoped root id ("expense-sum", "stamp-revision"...), "" when nothing is open.</summary>
    public string Root = "";
    /// <summary>The sheet line it points to (a Level3ComputationRules field, or "stamp").</summary>
    public string Field = "";
    public Level3RootKind Kind;
    /// <summary>The root is charged now (first time this case sees it).</summary>
    public bool NewRoot;
    /// <summary>This hand-in is a retry (a changed hand-in after a returned review).</summary>
    public bool Retry;
    public int Xp;
    public string Note = "";

    public bool Returned => Outcome == Level3ReviewOutcome.PrematureApproval || Outcome == Level3ReviewOutcome.FalseHold || Outcome == Level3ReviewOutcome.StaleStamp;
}

/// <summary>What an approval paid (presentation reads it after the save).</summary>
public struct Level3ReviewGrants
{
    public bool Changed;
    public bool AlreadyClaimed;
    public CampaignRewards.Change Coins;
    public int Xp;
    public bool MasteryClaimed;
}

/// <summary>
/// PURPOSE:
/// Stamping and the Auditor's review of Bea's computation (Level 3 T09), pure
/// C# so the regression suite drives the same commands the desk and the
/// Auditor call. Every command runs on a COPY; the caller writes, then publishes.
///
/// - Stamp: Ready for computation review (not filing or payment) or Not Ready,
///   covering the sheet's current revision. Any later change to the sheet
///   makes the stamp out of date (it is kept, marked, until stamped again).
/// - Refusal: why the Auditor can't take the folder yet (nothing recorded):
///   another case, already approved, no stamp, today's window closed, or the
///   same computation and stamp he already went through.
/// - Submit: decides once. The review walks the sheet in dependency order
///   (gross, expenses, taxable income, band, working, tax due, credit, balance)
///   and stops at the EARLIEST line that is empty, out of date, typed by hand
///   or wrong; a line that only follows an earlier mistake is never charged.
///   A Ready stamp on a fully supported current sheet is approved; a Ready
///   stamp with an open root is a premature approval; Not Ready with an open
///   root is a justified hold (acknowledged, nothing charged, work stays
///   open); Not Ready on a supported sheet is a false hold; an out-of-date
///   stamp is returned first. Each root id is charged once per case; a changed
///   hand-in after a returned review is one retry. Case XP = 100 - min(15,
///   2 x roots + retries + late), the existing Level 2 settings.
/// - Approval: freezes the XP and the approved sheet's own values, moves the
///   chapter BeaComputation -> BeaReview -> BeaDelivery, stages Bea's copy
///   "atAuditor" and stops her clock. ApplyApprovalGrants adds the once-only
///   100 coins and the case's mastery claim in the same save. No trust,
///   ethics or chapter completion.
/// - Clock: Bea's 16 active minutes start after the protected practice; the
///   cutoff rolls to a free next morning with all work kept; the first miss is
///   the one punctuality mark (inside the cap).
/// </summary>
public static class Level3ReviewRules
{
    public const string ResourcePath = "Levels/Level_03/review";
    public const string StampReady = "ready";
    public const string StampHold = "hold";
    public const string StampField = "stamp";
    public const string StampRevisionRoot = "stamp-revision";
    public const string StampDecisionRoot = "stamp-decision";

    public const string SubmitAction = "Level_03/bea-review-submitted";
    public const string ApproveAction = "Level_03/bea-review-approved";
    public const string ApprovedMilestone = "bea-case-approved";
    public const string CopyAtAuditor = "atAuditor";
    public const string ClockStartAction = "Level_03/bea-clock-started";

    public const int MaxXp = 100;
    public const int XpPerRoot = 2;
    public const int XpPerRetry = 1;
    public const int MaxXpLoss = 15;

    public const string RefuseWrongCase = "This folder isn't the case I'm reviewing today.";
    public const string RefuseApproved = "I've already approved Bea's computation. Her copy is waiting with me.";
    public const string RefuseNotNow = "I can't take this folder right now.";
    public const string RefuseNoStamp = "Stamp Bea's computation first: Ready for computation review, or Not Ready if something is still open.";
    public const string RefuseClosed = "Today's review window has closed. Bring it to me tomorrow morning; your work is kept.";
    public const string RefuseUnchanged = "This is the same computation and stamp I already went through. Change what I marked, stamp again, then bring it back.";

    private static string Bea => Level3ChapterRules.BeaCaseId;

    /// <summary>The review lines of the dependency order (optional working lines are judged only when present).</summary>
    private static readonly (string field, bool optional)[] Order =
    {
        (Level3ComputationRules.Gross, false), (Level3ComputationRules.Expenses, false), (Level3ComputationRules.Taxable, false),
        (Level3ComputationRules.Rule, false), (Level3ComputationRules.Excess, true), (Level3ComputationRules.Marginal, true),
        (Level3ComputationRules.TaxDue, false), (Level3ComputationRules.Credits, false), (Level3ComputationRules.Payable, false)
    };

    // =========================================================
    // CONTENT
    // =========================================================

    /// <summary>The built-in review content (used when review.json is missing or broken).</summary>
    public static Level3ReviewContent BuiltIn()
    {
        var content = new Level3ReviewContent();
        content.cases.Add(new Level3ReviewCaseContent
        {
            caseId = Bea,
            clock = new ReviewClockDefinition
            {
                allowanceSeconds = 960f, warnSeconds = new List<float> { 120f, 30f }, lateExp = 2, lateHearts = 0,
                announceLine = "Bea's review is open: {minutes} minutes of review time today. The cutoff is {deadline}.",
                warnLine = "{minutes} minutes of review time left today.",
                missedLine = "Today's review window has closed. Bea's work is kept for tomorrow morning.",
                nextMorningLine = "A new morning: {minutes} minutes of review time. Everything you did is still here.",
                onTimeLine = "Approved before the cutoff."
            }
        });
        content.auditor.approved.Add("Approved for this computation review. Give Bea her copy and explain the balance.");
        return content;
    }

    /// <summary>The note for a root (content first, then the built-in one); never a client value.</summary>
    public static string Note(Level3ReviewContent content, string root, string field, Level3RootKind kind)
    {
        string label = field == StampField ? "The stamp" : Level3ComputationRules.SectionLabel(field);
        string help = content?.roots?.Find(r => r != null && r.id == (kind == Level3RootKind.AsExpense ? "credit-as-expense" : root))?.note ?? BuiltInNote(root, kind);
        switch (kind)
        {
            case Level3RootKind.Missing: return label + " is still empty. " + help;
            case Level3RootKind.Stale: return label + " needs recalculating: something it uses has changed. " + help;
            case Level3RootKind.Typed: return label + " uses a number typed by hand: take the sheet's own numbers. " + help;
            default: return help;
        }
    }

    private static string BuiltInNote(string root, Level3RootKind kind)
    {
        if (kind == Level3RootKind.AsExpense) return "The withholding credit was taken off before the tax: it comes off the tax due at the end (Tax Code Book, Section 9).";
        switch (root)
        {
            case "income-sum": return "Gross income comes from the checked worksheet's Income subtotal (Section 6).";
            case "expense-sum": return "Allowable expenses come from the checked worksheet's subtotal, not the draft claims (Sections 4 and 5).";
            case "taxable-operation": return "Taxable income is gross income minus allowable expenses, from the two lines above (Section 6).";
            case "rule-selection": return "The rate band is the row of the annual table that holds the taxable income (Section 7).";
            case "tax-formula": return "Tax due is the band's base tax plus its rate on the excess over the floor only (Section 8).";
            case "credit-source": return "Tax credits are the tax withheld on Bea's own certificate for this year (Section 9).";
            case "balance-operation": return "The remaining payable is tax due minus tax credits (Section 9).";
            case StampRevisionRoot: return "Stamp the sheet as it is now, then bring it back.";
            case StampDecisionRoot: return "Every line is supported: a hold needs an open problem. Stamp it Ready for computation review.";
            default: return "Check this line against its sources.";
        }
    }

    // =========================================================
    // STAMP
    // =========================================================

    public static Level3StampRecord StampOf(Level3ChapterCheckpoint state, string caseId) => state?.Case(caseId)?.stamp;

    /// <summary>The stamp covers the sheet as it is now.</summary>
    public static bool StampCurrent(Level3ChapterCheckpoint state, string caseId)
    {
        Level3StampRecord stamp = StampOf(state, caseId);
        Level3ComputationRecord sheet = Level3ComputationRules.Record(state, caseId);
        return stamp != null && sheet != null && stamp.type.Length > 0 && stamp.revision == sheet.revision;
    }

    /// <summary>A stamp is on the sheet but the sheet changed after it.</summary>
    public static bool StampStale(Level3ChapterCheckpoint state, string caseId) =>
        StampOf(state, caseId) != null && StampOf(state, caseId).type.Length > 0 && !StampCurrent(state, caseId);

    /// <summary>The sheet revision and the stamp of a hand-in (an unchanged one is refused after a decision).</summary>
    public static string Signature(Level3ChapterCheckpoint state, string caseId)
    {
        Level3StampRecord stamp = StampOf(state, caseId) ?? new Level3StampRecord();
        int revision = Level3ComputationRules.Record(state, caseId)?.revision ?? 0;
        return revision + "|" + stamp.type + "|" + stamp.revision;
    }

    /// <summary>Stamps the sheet Ready (for computation review) or Not Ready at its current revision. A repeat is a Duplicate.</summary>
    public static Level3CommandOutcome Stamp(Level3ChapterCheckpoint candidate, string type, out string reason)
    {
        reason = null;
        if (candidate == null || !Level3ComputationRules.Editable(candidate, Bea) || Level3ChapterRules.Problem(candidate) != null)
        {
            reason = "Bea's computation cannot be stamped right now.";
            return Level3CommandOutcome.Refused;
        }
        if (type != StampReady && type != StampHold) { reason = "That is not one of the desk's stamps."; return Level3CommandOutcome.Refused; }
        Level3CaseProgress bea = candidate.Case(Bea);
        if (bea.stamp == null) bea.stamp = new Level3StampRecord();
        int revision = bea.computation.revision;
        if (bea.stamp.type == type && bea.stamp.revision == revision) { reason = "already stamped"; return Level3CommandOutcome.Duplicate; }
        bea.stamp.type = type;
        bea.stamp.revision = revision;
        bea.stamp.actionId = "Level_03/bea-stamp@" + revision + "/" + type;
        return Level3CommandOutcome.Applied;
    }

    // =========================================================
    // HAND-IN
    // =========================================================

    /// <summary>Null when the Auditor can take the folder now; otherwise his line saying why (nothing is decided or charged).</summary>
    public static string Refusal(Level3ChapterCheckpoint state, string folderCaseId)
    {
        if (state == null || Level3ChapterRules.Problem(state) != null) return RefuseNotNow;
        if (folderCaseId != Bea || state.activeCaseId != Bea) return RefuseWrongCase;
        Level3CaseProgress bea = state.Case(Bea);
        if (bea?.review != null && bea.review.approved) return RefuseApproved;
        if (!Level3ComputationRules.Editable(state, Bea)) return RefuseNotNow;
        if (bea.stamp == null || bea.stamp.type.Length == 0) return RefuseNoStamp;
        if (bea.clock != null && bea.clock.started && bea.clock.cutoffPending) return RefuseClosed;
        Level3ReviewRecord review = bea.review ?? new Level3ReviewRecord();
        if (review.lastOutcome.Length > 0 && review.lastSignature == Signature(state, Bea)) return RefuseUnchanged;
        return null;
    }

    /// <summary>The earliest open line of the sheet in dependency order (false when every line is supported).</summary>
    public static bool EarliestRoot(Level3ComputationFacts facts, out string root, out string field, out Level3RootKind kind)
    {
        root = field = "";
        kind = Level3RootKind.None;
        if (facts == null) return false;
        foreach (var (name, optional) in Order)
        {
            Level3ComputationFact step = facts.Step(name);
            if (step == null) continue;
            if (optional && !step.Present) continue;
            Level3RootKind found = !step.Present ? Level3RootKind.Missing : !step.Current ? Level3RootKind.Stale
                : step.Typed ? Level3RootKind.Typed : !step.Follows ? Level3RootKind.Wrong : Level3RootKind.None;
            if (found == Level3RootKind.None) continue;
            field = name;
            root = step.Root;
            kind = found;
            // The credit used above its own line is the credit's root (treated as an expense), not the arithmetic's.
            bool aboveCredit = name == Level3ComputationRules.Taxable || name == Level3ComputationRules.Excess ||
                               name == Level3ComputationRules.Marginal || name == Level3ComputationRules.TaxDue;
            if (facts.CreditInIncome && aboveCredit && found == Level3RootKind.Wrong)
            {
                root = "credit-source";
                kind = Level3RootKind.AsExpense;
            }
            return true;
        }
        return false;
    }

    /// <summary>Case XP: 100 - min(15, 2 x charged roots + retries + the late mark).</summary>
    public static int CaseXp(Level3ReviewRecord review, int lateExp)
    {
        if (review == null) return MaxXp;
        int loss = XpPerRoot * (review.chargedRoots?.Count ?? 0) + XpPerRetry * review.retries + (review.late ? Math.Max(0, lateExp) : 0);
        return MaxXp - Math.Min(MaxXpLoss, loss);
    }

    /// <summary>What a hand-in of the sheet as it is now would decide (reads only).</summary>
    public static Level3ReviewDecision Evaluate(Level3ChapterCheckpoint state, CaseDefinition definition, Level3ReviewContent content)
    {
        var decision = new Level3ReviewDecision();
        Level3CaseProgress bea = state?.Case(Bea);
        if (bea == null) return decision;
        Level3ReviewRecord review = bea.review ?? new Level3ReviewRecord();
        Level3ComputationFacts facts = Level3ComputationRules.Evaluate(state, definition, Bea);
        bool open = EarliestRoot(facts, out string root, out string field, out Level3RootKind kind) || !facts.Complete;
        bool ready = bea.stamp.type == StampReady;
        if (!StampCurrent(state, Bea))
        {
            decision.Outcome = Level3ReviewOutcome.StaleStamp;
            decision.Root = StampRevisionRoot;
            decision.Field = StampField;
            decision.Kind = Level3RootKind.Stale;
        }
        else if (!open) decision.Outcome = ready ? Level3ReviewOutcome.Approved : Level3ReviewOutcome.FalseHold;
        else decision.Outcome = ready ? Level3ReviewOutcome.PrematureApproval : Level3ReviewOutcome.SupportedHold;
        if (decision.Outcome == Level3ReviewOutcome.FalseHold)
        {
            decision.Root = StampDecisionRoot;
            decision.Field = StampField;
            decision.Kind = Level3RootKind.Wrong;
        }
        else if (decision.Outcome == Level3ReviewOutcome.PrematureApproval || decision.Outcome == Level3ReviewOutcome.SupportedHold)
        {
            decision.Root = root;
            decision.Field = field;
            decision.Kind = kind;
        }
        decision.Note = decision.Root.Length > 0 ? Note(content, decision.Root, decision.Field, decision.Kind) : "";
        // A justified hold is acknowledged: it never charges the problem the player found.
        decision.NewRoot = decision.Returned && decision.Root.Length > 0 && !review.chargedRoots.Contains(decision.Root);
        bool lastReturned = review.lastOutcome == "premature" || review.lastOutcome == "false-hold" || review.lastOutcome == "stale-stamp";
        decision.Retry = lastReturned;
        int lateExp = content?.Clock(Bea)?.lateExp ?? 0;
        var after = review.Copy();
        if (decision.NewRoot) after.chargedRoots.Add(decision.Root);
        if (decision.Retry) after.retries++;
        decision.Xp = CaseXp(after, lateExp);
        return decision;
    }

    /// <summary>
    /// Decides one hand-in on the candidate (refused when Refusal says so). Approval moves the
    /// chapter to BeaDelivery with Bea's copy at the Auditor; any other outcome keeps her computation open.
    /// </summary>
    public static Level3CommandOutcome Submit(Level3ChapterCheckpoint candidate, CaseDefinition definition, Level3ReviewContent content,
        out Level3ReviewDecision decision, out string reason)
    {
        decision = null;
        reason = Refusal(candidate, definition?.caseId);
        if (reason != null) return reason == RefuseApproved ? Level3CommandOutcome.Duplicate : Level3CommandOutcome.Refused;
        decision = Evaluate(candidate, definition, content);
        Level3CaseProgress bea = candidate.Case(Bea);
        if (bea.review == null) bea.review = new Level3ReviewRecord();
        Level3ReviewRecord review = bea.review;
        review.submissions++;
        if (decision.NewRoot) review.chargedRoots.Add(decision.Root);
        if (decision.Retry) review.retries++;
        review.lastRoot = decision.Root;
        review.lastField = decision.Field;
        review.lastSignature = Signature(candidate, Bea);
        switch (decision.Outcome)
        {
            case Level3ReviewOutcome.SupportedHold: review.holds++; review.lastOutcome = "hold"; break;
            case Level3ReviewOutcome.PrematureApproval: review.returned++; review.lastOutcome = "premature"; break;
            case Level3ReviewOutcome.FalseHold: review.returned++; review.lastOutcome = "false-hold"; break;
            case Level3ReviewOutcome.StaleStamp: review.returned++; review.lastOutcome = "stale-stamp"; break;
            case Level3ReviewOutcome.Approved: review.lastOutcome = "approved"; break;
        }
        review.xp = decision.Xp;
        if (decision.Outcome != Level3ReviewOutcome.Approved) return Level3CommandOutcome.Applied;

        Level3ComputationFacts facts = Level3ComputationRules.Evaluate(candidate, definition, Bea);
        review.approved = true;
        review.approvedRevision = bea.computation.revision;
        review.taxableIncome = facts.Step(Level3ComputationRules.Taxable)?.Value ?? "";
        review.taxDue = facts.Step(Level3ComputationRules.TaxDue)?.Value ?? "";
        review.credits = facts.Step(Level3ComputationRules.Credits)?.Value ?? "";
        review.remainingPayable = facts.Step(Level3ComputationRules.Payable)?.Value ?? "";
        if (Level3ChapterRules.TryAdvance(candidate, Level3Phase.BeaComputation, Level3Phase.BeaReview, SubmitAction, out reason) != Level3CommandOutcome.Applied ||
            Level3ChapterRules.TryAdvance(candidate, Level3Phase.BeaReview, Level3Phase.BeaDelivery, ApproveAction, out reason) != Level3CommandOutcome.Applied)
            return Level3CommandOutcome.Refused;
        bea.copyPhase = CopyAtAuditor;
        if (!candidate.milestones.Contains(ApprovedMilestone)) candidate.milestones.Add(ApprovedMilestone);
        if (bea.clock != null) bea.clock.cutoffPending = false;
        reason = null;
        return Level3CommandOutcome.Applied;
    }

    /// <summary>The once-only 100 coins and the case mastery claim of Bea's approval, on a candidate save (no trust).</summary>
    public static Level3ReviewGrants ApplyApprovalGrants(PlayerProgressData candidate, Level3ChapterCheckpoint state)
    {
        var grants = new Level3ReviewGrants();
        Level3ReviewRecord review = state?.Case(Bea)?.review;
        if (candidate == null || !candidate.HasCampaign || review == null || !review.approved) return grants;
        grants.Xp = review.xp;
        if (!CampaignRewards.CompleteActivity(candidate, CampaignRewards.Level3, CampaignRewards.BeaCaseApproved))
        {
            grants.AlreadyClaimed = true;
            return grants;
        }
        grants.Changed = true;
        grants.Coins = CampaignRewards.Grant(candidate, CampaignRewards.Level3, CampaignRewards.BeaCaseApproved, CampaignRewards.CoinsKind);
        string mastery = MasteryClaimId(candidate);
        if (!candidate.campaign.rewardClaims.Contains(mastery))
        {
            candidate.campaign.rewardClaims.Add(mastery);
            grants.MasteryClaimed = true;
        }
        return grants;
    }

    /// <summary>The case's once-only mastery claim (campaign/Level_03/bea-case-approved/mastery).</summary>
    public static string MasteryClaimId(PlayerProgressData data) =>
        CampaignRewards.ClaimId(data?.campaign?.campaignId, CampaignRewards.Level3, CampaignRewards.BeaCaseApproved, CampaignRewards.MasteryKind);

    // =========================================================
    // BEA'S REVIEW CLOCK
    // =========================================================

    /// <summary>Her case is approved: her clock has stopped for good.</summary>
    public static bool CaseDone(Level3ChapterCheckpoint state) => state?.Case(Bea)?.review?.approved == true;

    /// <summary>Her 16 active minutes are due to start: the protected practice is done and her case is still open.</summary>
    public static bool ClockDue(Level3ChapterCheckpoint state)
    {
        Level3Phase phase = Level3ChapterRules.PhaseOf(state);
        return phase >= Level3Phase.BeaRecords && phase <= Level3Phase.BeaComputation && state.Case(Bea)?.clock?.started != true && !CaseDone(state);
    }

    public static Level3CommandOutcome StartClock(Level3ChapterCheckpoint candidate, Level3ReviewContent content, out string reason)
    {
        reason = null;
        ReviewClockDefinition definition = content?.Clock(Bea);
        if (candidate == null || definition == null || !definition.IsSet || Level3ChapterRules.Problem(candidate) != null) { reason = RefuseNotNow; return Level3CommandOutcome.Refused; }
        if (!ClockDue(candidate)) { reason = "already started"; return Level3CommandOutcome.Duplicate; }
        Level3CaseProgress bea = candidate.Case(Bea);
        if (bea.clock == null) bea.clock = new ReviewClockState();
        if (!ReviewClock.Start(bea.clock, definition)) { reason = "already started"; return Level3CommandOutcome.Duplicate; }
        if (!candidate.actionIds.Contains(ClockStartAction)) candidate.actionIds.Add(ClockStartAction);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>The closed window rolls to the next morning (a fresh allowance, all work kept); the first miss marks the review late once.</summary>
    public static Level3CommandOutcome Rollover(Level3ChapterCheckpoint candidate, Level3ReviewContent content, out bool firstMiss, out string reason)
    {
        firstMiss = false;
        reason = null;
        ReviewClockDefinition definition = content?.Clock(Bea);
        Level3CaseProgress bea = candidate?.Case(Bea);
        if (bea?.clock == null || definition == null || !bea.clock.cutoffPending) { reason = "no closed window"; return Level3CommandOutcome.Refused; }
        if (!ReviewClock.TryRollover(bea.clock, definition, false, CaseDone(candidate), out firstMiss)) return Level3CommandOutcome.Applied;
        if (firstMiss)
        {
            if (bea.review == null) bea.review = new Level3ReviewRecord();
            bea.review.late = true;
        }
        return Level3CommandOutcome.Applied;
    }

    /// <summary>Her clock after one review voucher (a copy; null when the window is closed, not started or already extended today).</summary>
    public static ReviewClockState ExtendedClock(Level3ChapterCheckpoint state) => ReviewClock.Extend(state?.Case(Bea)?.clock, VoucherRules.ExtensionSeconds);
}
