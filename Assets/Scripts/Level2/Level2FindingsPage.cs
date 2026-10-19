using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Three-part observation kept on a removable sheet: reported entry to check,
/// supporting statement, and a principle transferred from the book. Filling
/// boxes creates a draft; a complete supported link raises
/// FindingRecorded. The same state is used by the initial stamp.
///
/// The Findings page holds the actual Findings paper, attached at a modest
/// size; lift it to work on it and drop it anywhere on the folder to file it
/// back (the folder turns to this page first). Rosa's records are read on
/// the annotated papers from Mark the Money!, so they are not folder pages.
/// </summary>
public sealed class Level2FindingsPage : MonoBehaviour
{
    public const string ChildName = "FindingsPage";
    public const string PageTitle = "Findings";
    public const string ReasonKey = "FindingReason";
    public const string InkText = "INITIAL REVIEW\nNOT READY";

    private bool announced;
    private CaseFolderUI.PageBuilder pendingRefresh;
    private Level2FindingsSheet sheet;
    private CaseData observedCase;
    private bool observedHandover;

    private void OnEnable()
    {
        CaseFolderUI.ExtraPages = BuildPages;
        CaseFolderUI.PageDecorator = DecoratePage;
    }

    private void OnDisable()
    {
        if (sheet != null) sheet.Dispose();
        if (CaseFolderUI.ExtraPages == (System.Func<CaseData, List<CaseFolderPageContent>>)BuildPages)
            CaseFolderUI.ExtraPages = null;
        if (CaseFolderUI.PageDecorator == (System.Action<int, CaseFolderUI.PageBuilder>)DecoratePage)
            CaseFolderUI.PageDecorator = null;
    }

    /// <summary>The initial review ink on the front page, exactly where it was stamped.</summary>
    private static void DecoratePage(int index, CaseFolderUI.PageBuilder page)
    {
        Level2OfficeReview.DecorateFinal(index, page);
        if (index != 0) return;
        PreliminaryAssessmentState stamp = Level2OfficeStart.Review?.preliminary;
        if (stamp == null || !stamp.hasInk) return;
        page.AddOverlay(ReviewInkMark.CreateAt(page.Paper, InkText, new Vector2(stamp.inkX, stamp.inkY), stamp.inkAngle));
    }

    // A box's value arrives inside the transfer; redraw on the next frame.
    private void LateUpdate()
    {
        CaseData current = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        bool handedOver = current?.review?.HandoverDone == true;
        if (current != observedCase || handedOver != observedHandover)
        {
            observedCase = current;
            observedHandover = handedOver;
            if (current != null)
            {
                foreach (CaseFolderUI folder in FindObjectsByType<CaseFolderUI>(FindObjectsInactive.Include))
                    if (folder != null && folder.IsShowing && folder.FindPageIndex(PageTitle) >= 0)
                        folder.RefreshCurrentPage();
            }
        }
        if (sheet != null) sheet.SyncVisibility();
        if (pendingRefresh == null) return;
        CaseFolderUI.PageBuilder builder = pendingRefresh;
        pendingRefresh = null;
        if (sheet != null && sheet.IsDetached)
        {
            sheet.RefreshContent();
            CheckFinding(sheet);
        }
        else builder.Refresh();
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
        Level2OfficeReview.AppendPages(pages, data);
        return pages;
    }

    private void Draw(CaseFolderUI.PageBuilder page)
    {
        if (sheet != null && sheet.IsDetached)
            Level2FindingsSheet.DrawEmptyMount(page, "Your Findings paper is out.\n\nDrop it on the folder to file it here.");
        else
            sheet = Level2FindingsSheet.Create(page, null, DrawContents, active => sheet = active, CheckFinding);
    }

    /// <summary>Each record Rosa handed over is its own read-only page (lines can still be picked up as clues).</summary>
    private static void AddRecordPages(List<CaseFolderPageContent> pages, CaseData data, CaseDefinition definition)
    {
        CaseReviewState review = data.review;
        if (!review.HandoverDone || definition.review.documents == null || definition.documents == null) return;
        foreach (string id in definition.review.handoverDocumentIds)
        {
            ReviewDocumentDefinition record = definition.review.FindDocument(id);
            if (record == null || record.kind != "record" || !review.HasDocument(id)) continue;
            DocumentDefinition document = definition.documents.Find(d => d != null && d.documentName == record.documentName);
            if (document == null) continue;
            pages.Add(new CaseFolderPageContent(record.title, "") { Render = page => DrawRecord(page, document) });
        }
    }

    private static void DrawRecord(CaseFolderUI.PageBuilder page, DocumentDefinition document)
    {
        CaseFolderUI.MarkReviewDocumentRead(document.documentName);
        if (document.fields == null) return;
        foreach (DocumentFieldDefinition field in document.fields)
        {
            if (field == null) continue;
            DataValueType type = System.Enum.TryParse(field.type, out DataValueType parsed) ? parsed : DataValueType.Text;
            object value = field.displayValue;
            if (type == DataValueType.Number)
            {
                if (CaseFolderUI.TryParseAmount(field.displayValue, out float amount)) value = amount;
                else type = DataValueType.Text;
            }
            page.AddSourceRow(field.label, value, type, field.semanticKey);
        }
    }

    private void DrawContents(CaseFolderUI.PageBuilder page, RectTransform rows)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = Definition;
        if (review == null || definition == null) return;
        PreliminaryRuleDefinition rule = definition.review.preliminaryRule;
        ReviewFindingState finding = review.FindFinding(rule.findingId);

        if (review.CanEditPreliminaryFinding)
        {
            Slot(page, rows, 0, "Entry to check", finding?.concernLabel, ReviewFindingState.Concern, rule.findingId);
            Slot(page, rows, 1, "Clue", finding?.evidenceLabel, ReviewFindingState.Evidence, rule.findingId);
            Slot(page, rows, 2, "Reason", finding?.reasonLabel, ReviewFindingState.Reason, rule.findingId);
        }
        else if (finding != null)
        {
            Source(page, rows, 0, "Entry to check", finding.concernLabel, "FindingConcernNote");
            Source(page, rows, 1, "Clue", finding.evidenceLabel, "FindingEvidenceNote");
            Source(page, rows, 2, "Reason", finding.reasonLabel, "FindingReasonNote");
        }

    }

    private void CheckFinding(Level2FindingsSheet paper)
    {
        if (paper == null || !paper.CanTransfer || Definition == null) return;
        CaseReviewState review = Level2OfficeStart.Review;
        if (review == null || !review.CanEditPreliminaryFinding) return;
        PreliminaryRuleDefinition rule = Definition.review.preliminaryRule;
        bool confirmed = review.FindFinding(rule.findingId)?.comparisonConfirmed == true;
        FindingComparison.Result result = FindingComparison.Check(review, rule);
        if (result == FindingComparison.Result.NoDiscrepancy)
        {
            review.ClearFindingDraft(rule.findingId);
            paper.RefreshContent();
        }
        paper.ShowComparison(result, rule.supportedObservation, !confirmed && result != FindingComparison.Result.Incomplete);
        if (!announced && result == FindingComparison.Result.Supported)
        {
            announced = true;
            GameplayEvents.RaiseFindingRecorded();
        }
    }

    private static GameObject Row(CaseFolderUI.PageBuilder page, RectTransform rows, int index)
    {
        GameObject row = rows.childCount > index ? rows.GetChild(index).gameObject : page.CreateSheetRow(rows).gameObject;
        Level2FindingsSheet.StyleRow(row, index);
        return row;
    }

    private static void Source(CaseFolderUI.PageBuilder page, RectTransform rows, int index, string label, string value, string key)
    {
        CaseFolderFieldRow row = Row(page, rows, index).GetComponent<CaseFolderFieldRow>();
        row.InitializeSourceOnly(label, value, DataValueType.Text, key);
        Level2FindingsSheet.StyleRow(row.gameObject, index);
        row.GetComponentInChildren<TMPro.TMP_Text>(true).text = PaperInkStyle.FieldTextStacked(label, value, false);
    }

    private void Slot(CaseFolderUI.PageBuilder page, RectTransform rows, int index, string label, string value, string part, string findingId)
    {
        FindingSlot slot = FindingSlot.Make(Row(page, rows, index), label, value);
        Level2FindingsSheet.StyleRow(slot.gameObject, index);
        Level2FindingsSheet paper = rows.GetComponentInParent<Level2FindingsSheet>();
        slot.Accepts = v => paper != null && paper.CanTransfer && Resolve(v, part, out _, out _);
        slot.Rejected = () => paper.ShowTransferHint(part);
        slot.Received = v =>
        {
            if (paper == null || !paper.CanTransfer) return;
            if (!Resolve(v, part, out string documentId, out string shownLabel)) return;
            CaseReviewState review = Level2OfficeStart.Review;
            if (review == null) return;
            string key = part == ReviewFindingState.Reason ? ReasonIdFor(v) : v.SemanticKey;
            if (review.SetFindingPart(findingId, part, documentId, key, shownLabel))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPaperSFX();
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
        shownLabel = part == ReviewFindingState.Concern ? fieldLabel + " · " + value.DisplayText : fieldLabel + ": " + value.DisplayText;
        return true;
    }

    private static string ReasonIdFor(DataValue value)
    {
        CaseDefinition definition = Definition;
        if (value == null || value.SemanticKey != ReasonKey || definition == null) return null;
        string label = value.Value != null ? value.Value.ToString() : value.DisplayText;
        if (definition.review.FindReason(label) != null) return label;
        foreach (ReviewReasonDefinition reason in definition.review.reasons)
            if (reason != null && reason.label == label) return reason.id;
        return null;
    }
}
