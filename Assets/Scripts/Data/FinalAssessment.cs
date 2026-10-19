using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>A final assessment stamp: verdict, reason, the packet revision it covers and where its ink landed.</summary>
[Serializable]
public class FinalAssessmentState
{
    public const string Ready = "Ready";
    public const string NotReady = "NotReady";

    public string verdict;
    public string reasonId;
    public int revision;
    public bool hasInk;
    public float inkX;
    public float inkY;
    public float inkAngle;
}

/// <summary>
/// PURPOSE:
/// The final assessment of a review case (Level 2, T08), pure C#. Cases may
/// ask for a reason or use a generated summary of the player's entered work.
/// The stamp is a recommendation for the Auditor; nothing is filed by stamping.
///
/// RULES:
/// - Only structural prerequisites are checked before committing: the office
///   review is complete (schedule line + every configured finding box); cases
///   that offer reasons also require one. No hidden correctness lock: both
///   stamps stay available.
/// - The stamp commits once, at impact, to the current packet revision.
///   Pressing the same verdict on a current stamp changes nothing.
/// - Any later change to graded work (schedule, findings, reason) raises the
///   packet revision, so the stamp is no longer current ("Review changed:
///   stamp again"). Reading pages never changes the revision.
/// - Restamping keeps a compact history (last 3) and earns nothing.
/// - The initial (preliminary) stamp is a separate record and is untouched.
/// </summary>
public static class FinalAssessment
{
    public enum Outcome { Recorded, NotAvailable, OfficeIncomplete, NoReason, AlreadyCurrent }

    public const int HistoryLimit = 3;

    /// <summary>The final assessment is open: office review structurally complete, before the audit result is final.</summary>
    public static bool Open(CaseReviewState state, CaseReviewDefinition review) =>
        state != null && review != null && state.phase >= ReviewPhase.FinalAssessment &&
        state.phase != ReviewPhase.Audit && state.phase != ReviewPhase.Completed &&
        OfficeReview.StructurallyComplete(state, review);

    /// <summary>Moves the review to FinalAssessment once the office review is structurally complete.</summary>
    public static bool TryBegin(CaseReviewState state, CaseReviewDefinition review) =>
        state != null && state.phase == ReviewPhase.OfficeReconciliation &&
        OfficeReview.StructurallyComplete(state, review) && state.TryAdvance(ReviewPhase.FinalAssessment);

    /// <summary>Chooses the assessment reason. A real change is a packet revision (it is graded).</summary>
    public static bool SetReason(CaseReviewState state, CaseReviewDefinition review, string reasonId)
    {
        if (!OfficeReview.CanEdit(state) || review == null || review.simplifiedAssessment ||
            review.FindAssessmentReason(reasonId) == null) return false;
        if (state.assessmentReasonId == reasonId) return false;
        state.assessmentReasonId = reasonId;
        state.packetRevision++;
        return true;
    }

    public static bool ClearReason(CaseReviewState state)
    {
        if (!OfficeReview.CanEdit(state) || string.IsNullOrEmpty(state.assessmentReasonId)) return false;
        state.assessmentReasonId = null;
        state.packetRevision++;
        return true;
    }

    /// <summary>True when the final stamp covers the current packet (and its reason).</summary>
    public static bool IsCurrent(CaseReviewState state) =>
        state?.finalStamp != null && !string.IsNullOrEmpty(state.finalStamp.verdict) &&
        state.finalStamp.revision == state.packetRevision && state.finalStamp.reasonId == state.assessmentReasonId;

    /// <summary>A stamp exists but the packet changed since: it needs reassessment.</summary>
    public static bool IsStale(CaseReviewState state) =>
        state?.finalStamp != null && !string.IsNullOrEmpty(state.finalStamp.verdict) && !IsCurrent(state);

    public static Outcome Check(CaseReviewState state, CaseReviewDefinition review, bool ready)
    {
        if (state == null || review == null || state.phase < ReviewPhase.OfficeReconciliation ||
            state.phase == ReviewPhase.Audit || state.phase == ReviewPhase.Completed) return Outcome.NotAvailable;
        if (!OfficeReview.StructurallyComplete(state, review)) return Outcome.OfficeIncomplete;
        if (!review.simplifiedAssessment && review.FindAssessmentReason(state.assessmentReasonId) == null)
            return Outcome.NoReason;
        if (IsCurrent(state) && state.finalStamp.verdict == (ready ? FinalAssessmentState.Ready : FinalAssessmentState.NotReady))
            return Outcome.AlreadyCurrent;
        return Outcome.Recorded;
    }

    /// <summary>Commits the stamp once, at impact. The previous stamp moves to the history.</summary>
    public static Outcome TryStamp(CaseReviewState state, CaseReviewDefinition review, bool ready,
        float? inkX = null, float? inkY = null, float inkAngle = 0f)
    {
        TryBegin(state, review);
        Outcome outcome = Check(state, review, ready);
        if (outcome != Outcome.Recorded) return outcome;
        if (state.stampHistory == null) state.stampHistory = new List<FinalAssessmentState>();
        if (state.finalStamp != null && !string.IsNullOrEmpty(state.finalStamp.verdict))
        {
            state.stampHistory.Add(state.finalStamp);
            while (state.stampHistory.Count > HistoryLimit) state.stampHistory.RemoveAt(0);
        }
        state.finalStamp = new FinalAssessmentState
        {
            verdict = ready ? FinalAssessmentState.Ready : FinalAssessmentState.NotReady,
            reasonId = state.assessmentReasonId,
            revision = state.packetRevision,
            hasInk = inkX.HasValue && inkY.HasValue,
            inkX = inkX ?? 0f,
            inkY = inkY ?? 0f,
            inkAngle = inkAngle
        };
        return outcome;
    }

    /// <summary>A handwritten-style summary made only from lines and findings the player entered.</summary>
    public static string GenerateSummary(CaseReviewState state, CaseReviewDefinition review)
    {
        if (state == null || review == null) return string.Empty;
        var summary = new StringBuilder("May: ")
            .Append(OfficeReview.ScheduledCount(state)).Append(" lines · ₱")
            .Append(OfficeReview.MayTotal(state).ToString("N0", CultureInfo.InvariantCulture)).Append(" total");
        if (review.officeFindings != null)
            foreach (OfficeFindingDefinition definition in review.officeFindings)
            {
                if (definition == null) continue;
                OfficeFindingState finding = null;
                if (state.officeFindings != null)
                    foreach (OfficeFindingState recorded in state.officeFindings)
                        if (recorded != null && recorded.id == definition.id) { finding = recorded; break; }
                string record = null;
                string conclusion = null;
                if (definition.parts != null)
                    foreach (OfficeFindingPartDefinition partDefinition in definition.parts)
                    {
                        if (partDefinition == null) continue;
                        OfficePartState part = finding?.Find(partDefinition.id);
                        if (part == null || string.IsNullOrWhiteSpace(part.label)) continue;
                        if (partDefinition.accepts == OfficeFindingPartDefinition.Record) record = part.label;
                        else if (partDefinition.accepts == OfficeFindingPartDefinition.Conclusion) conclusion = part.label;
                    }
                string title = definition.title;
                string prefix = "Finding " + definition.id + " · ";
                if (!string.IsNullOrWhiteSpace(title) && title.StartsWith(prefix)) title = title.Substring(prefix.Length);
                if (string.IsNullOrWhiteSpace(title)) title = "Unlisted payment";
                summary.Append("\n").Append(title).Append(": ");
                if (record == null && conclusion == null) summary.Append("entry incomplete");
                else
                {
                    if (record != null) summary.Append(record);
                    if (conclusion != null)
                    {
                        if (record != null) summary.Append(" · ");
                        summary.Append(conclusion);
                    }
                }
            }

        return summary.ToString();
    }

    /// <summary>Arthur's line for each outcome (no correctness: the Auditor decides).</summary>
    public static string Message(Outcome outcome, bool ready)
    {
        switch (outcome)
        {
            case Outcome.Recorded:
                return (ready ? "Ready" : "Not Ready") + " recommendation stamped. Hand Rosa's folder to the Auditor when you're done.";
            case Outcome.OfficeIncomplete: return "Finish the Reviewed Schedule and all listed finding entries first.";
            case Outcome.NoReason: return "Choose a reason on the Final Assessment page first. Reasons are in Finding Reasons.";
            case Outcome.AlreadyCurrent: return "This recommendation is already stamped for the current review.";
            default: return null;
        }
    }
}
