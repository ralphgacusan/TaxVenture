using System;
using System.Collections.Generic;

/// <summary>
/// PURPOSE:
/// Worth the Tax? practice packet (teaching pass, 6 November 2026), pure C#
/// so the regression suite drives the same rules the table calls. Before
/// Bea's first scored filing, four short fictional slips from a practice
/// painter go through the SAME stand, half-feed, selector, Why? and lever
/// (Level3BeaTable): earnings are income; a supported business cost may be
/// deducted; a receipt alone does not make a personal purchase deductible;
/// two records of one purchase count once.
///
/// ARTHUR FIRST (follow-up, 6 November 2026, after Level 2's Mark the Money!
/// demonstration): Arthur sorts the same four slips himself while the player
/// watches (the machine moves on its own; Demo lists his lines and moves),
/// then says "your turn" and the player sorts the same four. One saved
/// milestone (DemoMilestone) means the demonstration is not shown again; an
/// interrupted one replays on the next visit, as in Level 2.
///
/// Unlike Bea's records, a practice pull is answered at once (it is not
/// filed, saved, checked or scored): a wrong choice springs the lever back
/// and Arthur says one sentence pointing at the question to ask, and the same
/// slip waits for another try; a right one feeds in with his short why. Nothing here
/// touches her worksheet, its first-check grade, coins, XP, Public Trust,
/// inventory or her review clock (her clock starts only with her first saved
/// filing). Which slip is next is presentation state only: it lasts while the
/// scene is loaded (leaving the table and coming back resumes); a reload
/// restarts the packet from the first slip.
///
/// Complete (after the fourth slip is right) saves one chapter milestone and
/// action id through the existing signed chapter write, so the packet never
/// replays. A chapter whose draft already holds a filing (or that is past the
/// draft) never shows it: older saves continue unchanged.
/// </summary>
public static class Level3SortingPractice
{
    public const string Milestone = "teach-sorting-practice";
    public const string Action = "Level_03/teach/sorting-practice";
    /// <summary>Arthur's demonstration of the four slips was watched to the end (saved; never shown again).</summary>
    public const string DemoMilestone = "demo-sorting-practice";
    public const string DemoAction = "Level_03/demo/sorting-practice";
    /// <summary>Arthur, after his demonstration: the same task is the player's now.</summary>
    public const string YourTurnLine = "Your turn. Sort the same four slips. I'll tell you how each one goes.";
    public const string DoneLine = " Now Bea's own records. For hers, you check each batch yourself.";
    public const string RefuseNotNow = "The practice slips are not in use right now.";
    /// <summary>Shown on the stand card's detail line: what this packet is (and is not).</summary>
    public const string Details = "Practice painter · No score";

    private sealed class Slip
    {
        public string RowId, Title, Amount, Purpose, Treatment, Reason, Right;
        /// <summary>Arthur's demonstration: what he reads, why he turns the dial, and (P3/P4) why he picks the reason.</summary>
        public string DemoRead, DemoChoose, DemoWhy;
        /// <summary>Wrong treatment -> its one hint (the question to ask; no category is named).</summary>
        public Dictionary<string, string> Wrong;
        /// <summary>Right treatment, other reason: the hint for the reason.</summary>
        public string WrongReason;
    }

    private static readonly Slip[] Slips =
    {
        new Slip
        {
            RowId = "P1", Title = "Mural fee", Amount = "5000", Purpose = "A café paid for a wall mural.",
            Treatment = Level3WorksheetRules.Income, Reason = "", Right = "Earned from work: that is income.",
            DemoRead = "Watch me first. This slip says a café paid for a mural.",
            DemoChoose = "Money earned from work is income, so I turn the dial to Income. Then I pull the lever.",
            Wrong = new Dictionary<string, string>
            {
                [Level3WorksheetRules.Deduct] = "This money came in. Ask yourself: was it earned?",
                [Level3WorksheetRules.LeaveOut] = "This money came in. Ask yourself: was it earned?"
            }
        },
        new Slip
        {
            RowId = "P2", Title = "Paint for the mural", Amount = "800", Purpose = "For the café mural. Receipt PR-02.",
            Treatment = Level3WorksheetRules.Deduct, Reason = "", Right = "For the business, with a receipt: a deduction.",
            DemoRead = "Paint bought for that mural job, with receipt PR-02.",
            DemoChoose = "It was for the business and a receipt supports it, so it's a deduction.",
            Wrong = new Dictionary<string, string>
            {
                [Level3WorksheetRules.Income] = "This money went out. Ask: was it for the business?",
                [Level3WorksheetRules.LeaveOut] = "It was for the mural job. Is it supported?"
            }
        },
        new Slip
        {
            RowId = "P3", Title = "Concert tickets", Amount = "1200", Purpose = "A night out with friends. Receipt kept.",
            Treatment = Level3WorksheetRules.LeaveOut, Reason = "personal-purpose", Right = "A receipt alone doesn't make it business: left out.",
            DemoRead = "Concert tickets with friends. There's a receipt, but read what it was for.",
            DemoChoose = "A receipt alone isn't enough. It was personal, so I leave it out.",
            DemoWhy = "Leave Out asks why. I pick Personal purpose.",
            Wrong = new Dictionary<string, string>
            {
                [Level3WorksheetRules.Income] = "This money went out. Ask: was it for the business?",
                [Level3WorksheetRules.Deduct] = "A receipt isn't enough. Was it for the business?"
            },
            WrongReason = "Right to leave it out. Now, what was it for?"
        },
        new Slip
        {
            RowId = "P4", Title = "Card line: paint shop", Amount = "800", Purpose = "Same paint as receipt PR-02.",
            Treatment = Level3WorksheetRules.LeaveOut, Reason = "already-represented", Right = "One purchase, two papers: count it once.",
            DemoRead = "A card statement line for the same paint, receipt PR-02.",
            DemoChoose = "We already counted that paint. One purchase, two papers: count it once.",
            DemoWhy = "So the reason is Already represented.",
            Wrong = new Dictionary<string, string>
            {
                [Level3WorksheetRules.Income] = "This money went out. Is it already counted?",
                [Level3WorksheetRules.Deduct] = "Compare it with receipt PR-02. Already counted?"
            },
            WrongReason = "Right to leave it out. Is it already counted?"
        }
    };

    /// <summary>The practice rows in presentation order.</summary>
    public static readonly string[] RowIds = { "P1", "P2", "P3", "P4" };

    public static bool IsPracticeRow(string rowId) => rowId != null && Array.IndexOf(RowIds, rowId) >= 0;

    /// <summary>P3/P4 ask Why? after any treatment, exactly like Bea's two questionable records.</summary>
    public static bool NeedsReason(string rowId) => rowId == "P3" || rowId == "P4";

    private static Slip Find(string rowId) => Array.Find(Slips, s => s.RowId == rowId);

    /// <summary>A practice slip as a machine row (never one of Bea's rows: no source, field or transaction).</summary>
    public static Level3WorksheetRow Row(string rowId)
    {
        Slip slip = Find(rowId);
        return slip == null ? null : new Level3WorksheetRow { RowId = slip.RowId, Label = slip.Title, Amount = slip.Amount };
    }

    /// <summary>The stand card of a practice slip (its own printed facts; no treatment named).</summary>
    public static Level3TableStaging.CardFace Face(string rowId)
    {
        Slip slip = Find(rowId);
        if (slip == null) return new Level3TableStaging.CardFace();
        return new Level3TableStaging.CardFace
        {
            Position = "Practice " + (Array.IndexOf(RowIds, rowId) + 1) + " of " + RowIds.Length,
            Title = slip.Title,
            Amount = "₱" + Level3Calculator.Format(slip.Amount),
            Purpose = slip.Purpose,
            Details = Details
        };
    }

    public static bool Taught(Level3ChapterCheckpoint state) => state?.milestones != null && state.milestones.Contains(Milestone);

    /// <summary>Arthur's demonstration was watched to the end (or the packet is already done).</summary>
    public static bool DemoSeen(Level3ChapterCheckpoint state) =>
        Taught(state) || state?.milestones != null && state.milestones.Contains(DemoMilestone);

    /// <summary>One step of Arthur's demonstration of a slip (what he says, what the machine does).</summary>
    public struct DemoStep
    {
        public string RowId, Read, Choose, Why, Treatment, Reason;
    }

    /// <summary>Arthur's demonstration, in slip order: the same four slips, their right choices and his reasons.</summary>
    public static List<DemoStep> Demo()
    {
        var steps = new List<DemoStep>();
        foreach (Slip slip in Slips)
            steps.Add(new DemoStep { RowId = slip.RowId, Read = slip.DemoRead, Choose = slip.DemoChoose, Why = slip.DemoWhy ?? "", Treatment = slip.Treatment, Reason = slip.Reason });
        return steps;
    }

    /// <summary>
    /// Arthur's demonstration was watched to the end, on a copy: its milestone and action id only (presentation
    /// fact, never a reward). Duplicate: already seen or the packet is done. Refused: the packet is not due.
    /// </summary>
    public static Level3CommandOutcome MarkDemoSeen(Level3ChapterCheckpoint candidate, out string reason)
    {
        reason = null;
        if (DemoSeen(candidate)) { reason = "already seen"; return Level3CommandOutcome.Duplicate; }
        if (!Due(candidate)) { reason = RefuseNotNow; return Level3CommandOutcome.Refused; }
        candidate.milestones.Add(DemoMilestone);
        if (!candidate.actionIds.Contains(DemoAction)) candidate.actionIds.Add(DemoAction);
        return Level3CommandOutcome.Applied;
    }

    /// <summary>
    /// The packet stands before Bea's first record: her worksheet is open, nothing of hers is filed or
    /// checked yet, and it was never completed. A chapter already past that point never shows it.
    /// </summary>
    public static bool Due(Level3ChapterCheckpoint state) =>
        state != null && !Taught(state) && Level3ChapterRules.Problem(state) == null &&
        Level3ChapterRules.PhaseOf(state) == Level3Phase.WorksheetDraft && !Level3ChapterRules.BeaWorkIndependent(state) &&
        (state.worksheetCheck == null || state.worksheetCheck.checks == 0) &&
        (state.worksheet?.strips == null || state.worksheet.strips.Count == 0);

    /// <summary>The next practice slip after 'done' right answers (null once all four are right).</summary>
    public static string Next(int done) => done >= 0 && done < RowIds.Length ? RowIds[done] : null;

    /// <summary>
    /// A practice pull, judged at once (never saved or scored). True when the slip's treatment and (P3/P4)
    /// reason are right; feedback is one short sentence either way: the why when right, otherwise the
    /// question to ask (Arthur says it). Unknown rows/choices are false.
    /// </summary>
    public static bool Judge(string rowId, string treatment, string reason, out string feedback)
    {
        feedback = null;
        Slip slip = Find(rowId);
        if (slip == null || Array.IndexOf(Level3WorksheetRules.Treatments, treatment) < 0) return false;
        if (treatment != slip.Treatment)
        {
            slip.Wrong.TryGetValue(treatment, out feedback);
            return false;
        }
        if (NeedsReason(rowId) && reason != slip.Reason)
        {
            feedback = slip.WrongReason;
            return false;
        }
        feedback = slip.Right;
        return true;
    }

    /// <summary>
    /// The packet is done (all four slips right this visit), on a copy: the milestone and its action id only.
    /// Duplicate: already done (a repeat, a restore). Refused: nothing changed (not due, unusable chapter).
    /// </summary>
    public static Level3CommandOutcome Complete(Level3ChapterCheckpoint candidate, out string reason)
    {
        reason = null;
        if (Taught(candidate)) { reason = "already practised"; return Level3CommandOutcome.Duplicate; }
        string problem = Level3ChapterRules.Problem(candidate);
        if (problem != null) { reason = problem; return Level3CommandOutcome.Refused; }
        if (!Due(candidate)) { reason = RefuseNotNow; return Level3CommandOutcome.Refused; }
        candidate.milestones.Add(Milestone);
        if (!candidate.actionIds.Contains(Action)) candidate.actionIds.Add(Action);
        return Level3CommandOutcome.Applied;
    }
}
