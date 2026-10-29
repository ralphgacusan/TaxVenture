using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public const string EthicsNotePageTitle = "Rosa's Review Note";
    public const string ReasonKey = "FindingReason";
    public const string InkText = "INITIAL REVIEW\nNOT READY";

    private bool announced;
    private string lastNote;
    private CaseFolderUI.PageBuilder pendingRefresh;
    private Level2FindingsSheet sheet;
    private CaseData observedCase;
    private bool observedHandover;
    private string lastEthicsNote;
    private bool lastEthicsComplete;

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
        CaseReviewState ethicsReview = Level2OfficeStart.Review;
        string ethicsNote = ethicsReview?.rosaEthicsChoiceId;
        bool ethicsComplete = ethicsReview != null && ethicsReview.rosaEthicsFollowThroughDone;
        if (ethicsNote != lastEthicsNote || ethicsComplete != lastEthicsComplete)
        {
            lastEthicsNote = ethicsNote;
            lastEthicsComplete = ethicsComplete;
            RefreshEthicsNote();
        }
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
        pages.Add(new CaseFolderPageContent(EthicsNotePageTitle, "") { Render = DrawEthicsNote });
        return pages;
    }

    /// <summary>Refreshes the note when a committed choice or delivery changes its status.</summary>
    public static void RefreshEthicsNote()
    {
        foreach (CaseFolderUI folder in FindObjectsByType<CaseFolderUI>(FindObjectsInactive.Include))
        {
            if (folder == null || folder.IsReference || !folder.IsShowing) continue;
            int index = folder.FindPageIndex(EthicsNotePageTitle);
            if (index >= 0 && folder.CurrentPageIndex == index) folder.RefreshCurrentPage();
        }
    }

    private void DrawEthicsNote(CaseFolderUI.PageBuilder page)
    {
        RectTransform note = HudTheme.CreateRect("Neutral ruled Rosa review note", page.Paper);
        note.anchorMin = new Vector2(0.08f, 0.12f);
        note.anchorMax = new Vector2(0.92f, 0.9f);
        note.offsetMin = Vector2.zero;
        note.offsetMax = Vector2.zero;
        HudTheme.AddImage(note, HudTheme.Rounded, new Color32(250, 247, 235, 255), 12f);
        Shadow shadow = note.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.15f, 0.15f, 0.12f, 0.18f);
        shadow.effectDistance = new Vector2(2f, -2f);
        RectTransform margin = HudTheme.CreateRect("Notebook margin", note);
        margin.anchorMin = new Vector2(0.085f, 0.1f);
        margin.anchorMax = new Vector2(0.085f, 0.78f);
        margin.sizeDelta = new Vector2(1f, 0f);
        HudTheme.AddImage(margin, null, new Color32(187, 144, 136, 130));

        AddNoteText(note, "ROSA'S REVIEW NOTE", 0.12f, 0.82f, 0.82f, 0.94f, 22f, FontStyles.Bold);
        AddRule(note, 0.78f);
        CaseReviewState review = Level2OfficeStart.Review;
        string choice = review != null && review.rosaEthicsCommitted ? review.rosaEthicsChoiceText : "No choice has been recorded.";
        string followThrough = review == null || !review.rosaEthicsCommitted ? "No follow-through is recorded." :
            ClientCopyRules.Parse(review.clientCopyPhase) == ClientCopyPhase.Done ? "Required follow-through is complete." :
            review.rosaEthicsChoiceId.EndsWith("/dismissive-hold", System.StringComparison.Ordinal)
                ? "The review stays open. Complete the records and return for approval."
                : "Complete the supported correction, approval, and Client Copy.";
        string status = Level2Checkpoint.EthicsStatus(review);
        AddNoteText(note, "PLAYER'S WORDS", 0.12f, 0.66f, 0.82f, 0.75f, 16f, FontStyles.Bold);
        AddNoteText(note, choice, 0.12f, 0.49f, 0.82f, 0.66f, 20f, FontStyles.Normal);
        AddRule(note, 0.46f);
        AddNoteText(note, "NEXT STEP: " + followThrough, 0.12f, 0.34f, 0.82f, 0.43f, 17f, FontStyles.Normal);
        AddRule(note, 0.30f);
        AddNoteText(note, "STATUS: " + status, 0.12f, 0.14f, 0.82f, 0.27f, 18f, FontStyles.Bold);

        if (review != null && review.rosaEthicsFollowThroughDone &&
            review.rosaEthicsChoiceId.EndsWith("/responsible", System.StringComparison.Ordinal))
        {
            RectTransform tab = HudTheme.CreateRect("RECORDS CHECKED tab", note);
            tab.anchorMin = new Vector2(0.52f, 0.95f);
            tab.anchorMax = new Vector2(1f, 1.04f);
            tab.offsetMin = tab.offsetMax = Vector2.zero;
            HudTheme.AddImage(tab, HudTheme.Rounded, new Color32(235, 230, 211, 255), 5f);
            TextMeshProUGUI tabText = HudTheme.CreateLabel("Checked tab text", tab,
                Level2CampaignRules.ResponsibleFollowThroughStatus, 15f, new Color32(50, 54, 56, 255),
                TextAlignmentOptions.Center, FontStyles.Bold);
            HudTheme.Stretch(tabText.rectTransform, 3f);
        }
        page.AddOverlay(note.gameObject);
        StartCoroutine(PlaceNote(note));
    }

    private static TextMeshProUGUI AddNoteText(RectTransform parent, string text, float xMin, float yMin, float xMax, float yMax,
        float fontSize, FontStyles style)
    {
        TextMeshProUGUI label = HudTheme.CreateLabel("Ruled note text", parent, text, fontSize, new Color32(50, 54, 56, 255),
            TextAlignmentOptions.Left, style);
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.margin = new Vector4(0f, 0f, 0f, 0f);
        return label;
    }

    private static void AddRule(RectTransform parent, float y)
    {
        RectTransform rule = HudTheme.CreateRect("Pencil ruled line", parent);
        rule.anchorMin = new Vector2(0.1f, y);
        rule.anchorMax = new Vector2(0.9f, y);
        rule.sizeDelta = new Vector2(0f, 1f);
        HudTheme.AddImage(rule, null, new Color32(150, 170, 176, 160));
    }

    private static IEnumerator PlaceNote(RectTransform note)
    {
        if (note == null) yield break;
        Vector3 target = note.localScale;
        note.localScale = target * 0.96f;
        float elapsed = 0f;
        while (elapsed < 0.45f)
        {
            if (note == null || !note.gameObject.activeInHierarchy) yield break;
            elapsed += Time.unscaledDeltaTime;
            note.localScale = Vector3.Lerp(target * 0.96f, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.45f)));
            yield return null;
        }
        if (note != null) note.localScale = target;
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
        // A wrong link keeps all three answers; the slip writes a clue for the box to look at again.
        string note = result == FindingComparison.Result.NoDiscrepancy
            ? FindingComparison.Clue(FindingComparison.FirstMismatch(review, rule))
            : rule.supportedObservation;
        bool changed = note != lastNote;
        lastNote = note;
        paper.ShowComparison(result, note, (changed || !confirmed) && result != FindingComparison.Result.Incomplete);
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
            review.SetFindingPart(findingId, part, documentId, key, shownLabel);
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
