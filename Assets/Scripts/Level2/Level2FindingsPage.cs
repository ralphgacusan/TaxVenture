using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// The case folder's "Findings" page in Level 2 (ticket T03), under
/// Level2Gameplay/FindingsPage. It comes after Rosa's pages once her folder is
/// retrieved, in page 1's rows and style:
///
///   Concern        [?]   <- tap any line of the May summary, then this box
///   Related record [?]   <- any line of a record / note (e.g. the referral note)
///   Reason         [?]   <- one of the Choice rows below
///   Choice: Not on summary / Amounts differ / Maybe not income
///
/// Values move with the existing tap-to-pick / tap-to-place (or drag on the
/// page). The boxes accept whole groups, so they never reveal the answer;
/// the stamp checks it (PreliminaryReview). When all three are set the
/// finding is recorded (FindingRecorded, once). After the initial stamp the
/// page shows the finding as a read-only note with the stamp's ink.
/// </summary>
public sealed class Level2FindingsPage : MonoBehaviour
{
    public const string ChildName = "FindingsPage";
    public const string PageTitle = "Findings";
    public const string ReasonKey = "FindingReason";
    public const string InkText = "INITIAL REVIEW\nNOT READY";

    private bool announced;
    private CaseFolderUI.PageBuilder pendingRefresh;

    private void OnEnable()
    {
        CaseFolderUI.ExtraPages = BuildPages;
        CaseFolderUI.PageDecorator = DecoratePage;
    }

    private void OnDisable()
    {
        if (CaseFolderUI.ExtraPages == (System.Func<CaseData, List<CaseFolderPageContent>>)BuildPages)
            CaseFolderUI.ExtraPages = null;
        if (CaseFolderUI.PageDecorator == (System.Action<int, CaseFolderUI.PageBuilder>)DecoratePage)
            CaseFolderUI.PageDecorator = null;
    }

    /// <summary>The initial review ink on the front page, exactly where it was stamped.</summary>
    private static void DecoratePage(int index, CaseFolderUI.PageBuilder page)
    {
        if (index != 0) return;
        PreliminaryAssessmentState stamp = Level2OfficeStart.Review?.preliminary;
        if (stamp == null || !stamp.hasInk) return;
        page.AddOverlay(ReviewInkMark.CreateAt(page.Paper, InkText, new Vector2(stamp.inkX, stamp.inkY), stamp.inkAngle));
    }

    // A box's value arrives inside the transfer; redraw on the next frame.
    private void LateUpdate()
    {
        if (pendingRefresh == null) return;
        CaseFolderUI.PageBuilder builder = pendingRefresh;
        pendingRefresh = null;
        builder.Refresh();
    }

    private static CaseDefinition Definition =>
        CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;

    private List<CaseFolderPageContent> BuildPages(CaseData data)
    {
        var pages = new List<CaseFolderPageContent>();
        CaseDefinition definition = Definition;
        if (data == null || data.review == null || !data.review.FolderRetrieved ||
            definition == null || !definition.HasReview || !definition.review.preliminaryRule.IsSet)
            return pages;

        pages.Add(new CaseFolderPageContent(PageTitle, "") { Render = Draw });
        return pages;
    }

    private void Draw(CaseFolderUI.PageBuilder page)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = Definition;
        if (review == null || definition == null) return;
        PreliminaryRuleDefinition rule = definition.review.preliminaryRule;
        ReviewFindingState finding = review.FindFinding(rule.findingId);

        if (review.CanEditPreliminaryFinding)
        {
            Slot(page, "Concern", finding?.concernLabel, ReviewFindingState.Concern, rule.findingId);
            Slot(page, "Related record", finding?.evidenceLabel, ReviewFindingState.Evidence, rule.findingId);
            Slot(page, "Reason", finding?.reasonLabel, ReviewFindingState.Reason, rule.findingId);
            foreach (ReviewReasonDefinition reason in definition.review.reasons)
                if (reason != null) page.AddSourceRow("Choice", reason.label, DataValueType.Text, ReasonKey);
        }
        else if (finding != null)
        {
            page.AddSourceRow("Concern", finding.concernLabel, DataValueType.Text, "FindingConcernNote");
            page.AddSourceRow("Related record", finding.evidenceLabel, DataValueType.Text, "FindingEvidenceNote");
            page.AddSourceRow("Reason", finding.reasonLabel, DataValueType.Text, "FindingReasonNote");
            page.AddSourceRow("Status", finding.status, DataValueType.Text, "FindingStatus");
        }

        if (review.preliminary != null)
            page.AddSourceRow("Initial review", "Not Ready (front page)", DataValueType.Text, "PreliminaryAssessment");
    }

    private void Slot(CaseFolderUI.PageBuilder page, string label, string value, string part, string findingId)
    {
        FindingSlot slot = FindingSlot.Make(page.AddRowObject(), label, value);
        slot.Accepts = v => Resolve(v, part, out _, out _);
        slot.Received = v =>
        {
            if (!Resolve(v, part, out string documentId, out string shownLabel)) return;
            CaseReviewState review = Level2OfficeStart.Review;
            if (review == null) return;
            string key = part == ReviewFindingState.Reason ? ReasonIdFor(v) : v.SemanticKey;
            if (review.SetFindingPart(findingId, part, documentId, key, shownLabel))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
                ReviewFindingState finding = review.FindFinding(findingId);
                if (!announced && finding != null && finding.IsComplete)
                {
                    announced = true;
                    GameplayEvents.RaiseFindingRecorded();
                }
            }
            pendingRefresh = page;
        };
    }

    /// <summary>Which page a value comes from and what the box prints, if this box takes it.</summary>
    private static bool Resolve(DataValue value, string part, out string documentId, out string shownLabel)
    {
        documentId = null;
        shownLabel = null;
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = Definition;
        if (value == null || review == null || definition == null || !definition.HasReview) return false;

        if (part == ReviewFindingState.Reason)
        {
            string id = ReasonIdFor(value);
            if (id == null) return false;
            shownLabel = definition.review.FindReason(id).label;
            return true;
        }

        ReviewDocumentDefinition source = definition.review.PageForKey(definition.documents, value.SemanticKey, out string fieldLabel);
        if (source == null || !review.HasDocument(source.id)) return false;
        bool fits = part == ReviewFindingState.Concern
            ? source.kind == "summary"
            : source.kind == "note" || source.kind == "record";
        if (!fits) return false;
        documentId = source.id;
        shownLabel = fieldLabel;
        return true;
    }

    private static string ReasonIdFor(DataValue value)
    {
        CaseDefinition definition = Definition;
        if (value == null || value.SemanticKey != ReasonKey || definition == null) return null;
        string label = value.Value != null ? value.Value.ToString() : value.DisplayText;
        foreach (ReviewReasonDefinition reason in definition.review.reasons)
            if (reason != null && reason.label == label) return reason.id;
        return null;
    }
}
