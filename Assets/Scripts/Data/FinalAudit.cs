using System;
using System.Collections.Generic;

/// <summary>The audit history of a review case: what was charged and what was submitted (deduplicates penalties).</summary>
[Serializable]
public class AuditTrackState
{
    /// <summary>Root causes already charged once (a defect is never charged twice).</summary>
    public List<string> charged = new List<string>();
    /// <summary>Returned final submissions (premature approval, false hold, reason error).</summary>
    public int failedSubmissions;
    /// <summary>Revision and verdict of the last reviewed submission (unchanged resubmissions are blocked).</summary>
    public int lastRevision = -1;
    public string lastVerdict;
    public string lastOutcome;
    /// <summary>Supported interim holds acknowledged (no reward, no cost).</summary>
    public int holds;
    public bool approved;
    /// <summary>The case award was given (once).</summary>
    public bool rewarded;
    /// <summary>Approved before the first cutoff (T10 recognition).</summary>
    public bool onTime;
}

/// <summary>
/// PURPOSE:
/// The Auditor's review of a stamped Level 2 packet (T09), pure C#.
///
/// OUTCOMES:
/// - Approved: Ready on a supported packet, and (when configured) with a Ready reason. Completes once.
/// - SupportedHold: Not Ready on a packet with a real defect. Acknowledged;
///   no reward, no cost, no completion; all work kept.
/// - PrematureApproval: Ready while real defects remain. Returned.
/// - FalseHold: Not Ready on a packet with no real defect (only an explained
///   difference, or nothing). Returned.
/// - ReasonError: for cases with reason selection, the verdict fits but the
///   selected reason does not. Returned.
/// - Unchanged / NotStamped: refused before review (nothing changes).
///
/// COSTS (REWARDS_AND_VALIDATION): each returned submission counts once; each
/// root cause is charged once across all submissions (a verdict caused by a
/// defect is not charged again). Holds and free edits cost nothing. The case
/// award applies the capped deductions once, on approval.
/// </summary>
public static class FinalAudit
{
    public enum Outcome { NotStamped, Unchanged, Approved, SupportedHold, PrematureApproval, FalseHold, ReasonError }

    public const string RootFalseHold = "false-hold";
    public const string RootReason = "reason-unsupported";

    public sealed class Result
    {
        public Outcome Outcome;
        /// <summary>Every defect in this packet (for feedback).</summary>
        public readonly List<string> Defects = new List<string>();
        /// <summary>Root causes newly charged by this submission.</summary>
        public readonly List<string> NewlyCharged = new List<string>();
        public float MayTotal;
        public float AnnualTotal;
        public bool Returned => Outcome == Outcome.PrematureApproval || Outcome == Outcome.FalseHold || Outcome == Outcome.ReasonError;
    }

    /// <summary>
    /// Real defects of the current packet: office review root causes plus
    /// inherited required-field problems (passed in as their field keys).
    /// A falsely flagged explained difference is not a packet defect; it is
    /// the false-hold mistake.
    /// </summary>
    public static List<string> Defects(CaseReviewState state, CaseReviewDefinition review, IEnumerable<string> fieldIssueKeys, out OfficeReview.Result office)
    {
        office = OfficeReview.Evaluate(state, review);
        var defects = new List<string>();
        foreach (string root in office.RootCauses)
            if (root != OfficeReview.RootF2FalseFlag) defects.Add(root);
        if (fieldIssueKeys != null)
            foreach (string key in fieldIssueKeys)
                if (!string.IsNullOrEmpty(key) && !defects.Contains("field:" + key)) defects.Add("field:" + key);
        return defects;
    }

    /// <summary>Can this packet be handed in now? (current stamp, and changed since a returned or held submission).</summary>
    public static Outcome Admission(CaseReviewState state)
    {
        if (!FinalAssessment.IsCurrent(state)) return Outcome.NotStamped;
        AuditTrackState audit = state.audit;
        if (audit != null && !audit.approved && audit.lastRevision == state.packetRevision &&
            audit.lastVerdict == state.finalStamp.verdict) return Outcome.Unchanged;
        return Outcome.Approved; // admitted
    }

    /// <summary>Evaluates without changing anything.</summary>
    public static Result Evaluate(CaseReviewState state, CaseReviewDefinition review, IEnumerable<string> fieldIssueKeys)
    {
        var result = new Result();
        Outcome admission = Admission(state);
        if (admission != Outcome.Approved)
        {
            result.Outcome = admission;
            return result;
        }
        List<string> defects = Defects(state, review, fieldIssueKeys, out OfficeReview.Result office);
        result.Defects.AddRange(defects);
        result.MayTotal = office.MayTotal;
        result.AnnualTotal = office.AnnualTotal;
        bool falseFlag = office.RootCauses.Contains(OfficeReview.RootF2FalseFlag);
        bool simplifiedAssessment = review.simplifiedAssessment;
        AssessmentReasonDefinition reason = simplifiedAssessment ? null : review.FindAssessmentReason(state.finalStamp.reasonId);
        bool ready = state.finalStamp.verdict == FinalAssessmentState.Ready;

        if (ready)
        {
            if (defects.Count > 0 || falseFlag) result.Outcome = Outcome.PrematureApproval;
            else if (!simplifiedAssessment && (reason == null || reason.verdict != FinalAssessmentState.Ready)) result.Outcome = Outcome.ReasonError;
            else result.Outcome = Outcome.Approved;
            if (falseFlag && result.Outcome == Outcome.PrematureApproval) result.Defects.Add(RootFalseHold);
            return result;
        }

        if (defects.Count == 0)
        {
            result.Outcome = Outcome.FalseHold;
            result.Defects.Add(RootFalseHold);
            return result;
        }
        if (simplifiedAssessment)
        {
            result.Outcome = Outcome.SupportedHold;
            return result;
        }
        bool names = reason != null && reason.verdict == FinalAssessmentState.NotReady && reason.names != null &&
                     defects.Exists(d => reason.names.Exists(n => !string.IsNullOrEmpty(n) && d.StartsWith(n, StringComparison.Ordinal)));
        result.Outcome = names ? Outcome.SupportedHold : Outcome.ReasonError;
        return result;
    }

    /// <summary>
    /// Reviews the submission and records it once: charges new root causes,
    /// counts a returned submission, acknowledges a hold, or approves (phase
    /// Completed). Refused submissions (NotStamped / Unchanged) change nothing.
    /// </summary>
    public static Result Submit(CaseReviewState state, CaseReviewDefinition review, IEnumerable<string> fieldIssueKeys)
    {
        Result result = Evaluate(state, review, fieldIssueKeys);
        if (result.Outcome == Outcome.NotStamped || result.Outcome == Outcome.Unchanged) return result;
        if (state.audit == null) state.audit = new AuditTrackState();
        AuditTrackState audit = state.audit;
        if (audit.charged == null) audit.charged = new List<string>();
        audit.lastRevision = state.packetRevision;
        audit.lastVerdict = state.finalStamp.verdict;
        audit.lastOutcome = result.Outcome.ToString();

        switch (result.Outcome)
        {
            case Outcome.Approved:
                audit.approved = true;
                state.TryAdvance(ReviewPhase.Completed);
                break;
            case Outcome.SupportedHold:
                audit.holds++;
                state.TryAdvance(ReviewPhase.Correction);
                break;
            default:
                audit.failedSubmissions++;
                var charge = new List<string>(result.Defects);
                if (result.Outcome == Outcome.ReasonError) charge.Add(RootReason);
                foreach (string root in charge)
                    if (!audit.charged.Contains(root))
                    {
                        audit.charged.Add(root);
                        result.NewlyCharged.Add(root);
                    }
                state.TryAdvance(ReviewPhase.Correction);
                break;
        }
        return result;
    }

    /// <summary>The case award, once, after approval. False when not approved or already rewarded.</summary>
    public static bool TryReward(CaseReviewState state)
    {
        AuditTrackState audit = state?.audit;
        if (audit == null || !audit.approved || audit.rewarded) return false;
        audit.rewarded = true;
        return true;
    }

    /// <summary>The Auditor's feedback lines for the defects (first matching line per root, no fixes, deduplicated).</summary>
    public static List<string> Feedback(Result result, AuditLinesDefinition lines)
    {
        var said = new List<string>();
        if (result == null || lines?.feedback == null) return said;
        foreach (string defect in result.Defects)
            foreach (AuditFeedbackDefinition f in lines.feedback)
                if (f != null && !string.IsNullOrEmpty(f.root) && defect.StartsWith(f.root, StringComparison.Ordinal))
                {
                    if (!said.Contains(f.line)) said.Add(f.line);
                    break;
                }
        return said;
    }
}
