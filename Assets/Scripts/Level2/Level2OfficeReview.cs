using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// The office reconciliation pages of Rosa's folder (Level 2, T07). After
/// Mark the Money!, the folder gains a Reviewed Schedule and Final Assessment.
/// A schedule may include compact findings below its totals. Each schedule or
/// finding box accepts the player's chosen source line; totals are summed automatically and the
/// prepared summary S1 stays untouched as provenance. Reasons and conclusions
/// come from the book's Finding Reasons page. The annotated list is shown by its HUD icon.
///
/// RULES: all edits go through OfficeReview (pure). Any source line may be
/// chosen, so a complete wrong review is possible; the same payment can never
/// be counted twice (the box says which line already holds it). There is no
/// correctness sparkle: feedback says "fits" only. Tap a filled box with
/// nothing picked up to empty it; tap an empty one to bring out the annotated
/// papers. OfficeReviewCompleted fires once when the review is structurally
/// complete.
///
/// When the case's schedule sources name annotation entries (Rosa's case),
/// the values come from the highlighted lines of the four annotated papers
/// from Mark the Money! (AnnotatedValueRow), not from the record papers,
/// which stay readable as evidence.
/// </summary>
public sealed class Level2OfficeReview : MonoBehaviour
{
    public const string ChildName = "OfficeReview";
    public const string ListTitle = "Annotated Income List";
    public const string ScheduleTitle = "Reviewed Schedule";
    public const string ReasonKey = "FindingReason";
    public const string ConclusionKey = "FindingConclusion";
    public const string FinalTitle = "Final Assessment";
    public const string AssessmentReasonKey = "AssessmentReason";
    public const string StaleNote = "Review changed: stamp again";

    private static Level2OfficeReview instance;
    private CaseFolderUI.PageBuilder pendingRefresh;
    private static string selectedOfficeFindingId;
    private static CaseData selectedOfficeFindingCase;

    private static CaseDefinition Definition =>
        CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;

    private void OnEnable() => instance = this;

    private void OnDisable()
    {
        if (instance == this) instance = null;
    }

    private void LateUpdate()
    {
        if (pendingRefresh == null) return;
        CaseFolderUI.PageBuilder page = pendingRefresh;
        pendingRefresh = null;
        page.Refresh();
        AnnounceIfComplete();
    }

    // =========================================================
    // PAGES
    // =========================================================

    /// <summary>Adds the office pages once the review reached office reconciliation (called by the Findings page).</summary>
    public static void AppendPages(List<CaseFolderPageContent> pages, CaseData data)
    {
        CaseReviewState review = data?.review;
        CaseDefinition definition = Definition;
        if (review == null || definition == null || !definition.HasReview || review.phase < ReviewPhase.OfficeReconciliation) return;
        CaseReviewDefinition rd = definition.review;
        if (rd.scheduleSources == null || rd.scheduleSources.Count == 0) return;
        // An older checkpoint's record-line rows cannot stay hidden in the total.
        if (OfficeReview.CanEdit(review) && OfficeReview.DropRetiredRows(review, rd))
            Level2Say.Line("The Reviewed Schedule now takes its amounts from your annotated papers. Fill its lines again.", null, 6f);

        pages.Add(new CaseFolderPageContent(ScheduleTitle, "") { Render = DrawSchedule });
        if (!rd.simplifiedAssessment && rd.officeFindings != null && rd.officeFindings.Exists(f => f != null))
            pages.Add(new CaseFolderPageContent("Office Findings", "") { Render = DrawOfficeFindings });
        if (rd.simplifiedAssessment || (rd.assessmentReasons != null && rd.assessmentReasons.Count > 0))
            pages.Add(new CaseFolderPageContent(FinalTitle, "") { Render = DrawFinal });
    }

    /// <summary>
    /// The final assessment slip (T08): a generated work summary or chosen
    /// reason, the recommendation for the Auditor, revision and stamp history.
    /// </summary>
    private static void DrawFinal(CaseFolderUI.PageBuilder page)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = Definition;
        if (review == null || definition == null) return;
        CaseReviewDefinition rd = definition.review;
        string current = review.finalStamp == null || string.IsNullOrEmpty(review.finalStamp.verdict) ? "Not stamped yet"
            : (review.finalStamp.verdict == FinalAssessmentState.Ready ? "READY" : "NOT READY") +
              (FinalAssessment.IsCurrent(review) ? " (current review)" : " · " + StaleNote);
        if (rd.simplifiedAssessment)
            page.AddSourceRow("Recommendation for the Auditor", current, DataValueType.Text, "FinalRecommendation");
        else
        {
            page.AddSourceRow("Your recommendation (the Auditor decides)", current, DataValueType.Text, "FinalRecommendation");
            FindingSlot slot = FindingSlot.Make(page.AddRowObject(), "Reason",
                rd.FindAssessmentReason(review.assessmentReasonId)?.label);
            slot.Accepts = v => OfficeReview.CanEdit(Level2OfficeStart.Review) && AssessmentReasonId(v) != null;
            slot.Rejected = () => Level2Say.Line("This box takes an assessment reason from Reading Records in your book.", null, 4f);
            slot.Received = v =>
            {
                if (FinalAssessment.SetReason(Level2OfficeStart.Review, Definition.review, AssessmentReasonId(v))) Changed(page);
            };
            slot.Cleared = () =>
            {
                if (FinalAssessment.ClearReason(Level2OfficeStart.Review)) Changed(page);
            };
        }
        page.AddSourceRow("Reviewed work", OfficeReview.StructurallyComplete(review, rd)
            ? "Revision " + review.packetRevision + ": ready to stamp"
            : rd.simplifiedAssessment ? "Finish the schedule and its conclusion first" : "Finish the schedule and both findings first",
            DataValueType.Text, "FinalRevision");
        if (review.stampHistory != null && review.stampHistory.Count > 0)
        {
            var earlier = new List<string>();
            foreach (FinalAssessmentState old in review.stampHistory)
                if (old != null && !string.IsNullOrEmpty(old.verdict))
                    earlier.Add((old.verdict == FinalAssessmentState.Ready ? "Ready" : "Not Ready") + " (rev " + old.revision + ")");
            page.AddSourceRow("Earlier stamps", string.Join(", ", earlier), DataValueType.Text, "FinalHistory");
        }
        // The handwritten case summary is the page's last row.
        if (rd.simplifiedAssessment) DrawHandwrittenSummary(page, FinalAssessment.GenerateSummary(review, rd));
    }

    private static void DrawHandwrittenSummary(CaseFolderUI.PageBuilder page, string summary)
    {
        GameObject row = page.AddRowObject();
        foreach (MonoBehaviour behaviour in row.GetComponents<MonoBehaviour>())
            if (behaviour is CaseFolderFieldRow || behaviour is ValueDragSource ||
                behaviour is ValueSelectionHighlighter || behaviour is ValueTypeValidationFeedback ||
                behaviour is ValueHoverFeedback)
                Destroy(behaviour);

        TMP_Text text = row.GetComponentInChildren<TMP_Text>(true);
        if (text == null) return;
        Image background = row.GetComponent<Image>();
        if (background == null) background = row.GetComponentInChildren<Image>(true);
        PaperInkStyle.StyleFieldRow(background, text, false, null, null);
        if (background != null) background.raycastTarget = false;
        text.text = PaperInkStyle.FieldTextStacked("Handwritten case summary", summary, false);
        text.font = HudTheme.HandwritingFont;
        text.color = PaperInkStyle.PrintInk;
        text.fontStyle = FontStyles.Normal;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.enableAutoSizing = true;
        text.fontSize = 36f;
        text.fontSizeMin = 32f;
        text.fontSizeMax = 36f;
        text.overflowMode = TextOverflowModes.Overflow;
        LayoutElement layout = row.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.minHeight = 180f;
            layout.preferredHeight = 180f;
        }
    }

    private static string AssessmentReasonId(DataValue value)
    {
        CaseDefinition definition = Definition;
        if (value == null || value.SemanticKey != AssessmentReasonKey || definition == null) return null;
        return IdFor(value, id => definition.review.FindAssessmentReason(id)?.label,
            definition.review.assessmentReasons?.ConvertAll(r => (r?.id, r?.label)));
    }

    /// <summary>The final stamp ink on the Final Assessment page; faded when the review changed since.</summary>
    public static void DecorateFinal(int index, CaseFolderUI.PageBuilder page)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        if (review?.finalStamp == null || !review.finalStamp.hasInk || string.IsNullOrEmpty(review.finalStamp.verdict)) return;
        if (page.Owner == null || index != page.Owner.FindPageIndex(FinalTitle)) return;
        bool ready = review.finalStamp.verdict == FinalAssessmentState.Ready;
        GameObject ink = ReviewInkMark.CreateAt(page.Paper, ready ? "READY\nFOR FILING" : "NOT READY\nFOR FILING",
            new Vector2(review.finalStamp.inkX, review.finalStamp.inkY), review.finalStamp.inkAngle);
        bool current = FinalAssessment.IsCurrent(review);
        ReviewInkMark.SetStyle(ink, ready ? ReviewInkMark.InkGreen : ReviewInkMark.InkRed, current ? 1f : 0.3f);
        page.AddOverlay(ink);
    }

    private static void DrawList(CaseFolderUI.PageBuilder page)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = Definition;
        if (review?.challenge == null || definition == null) return;
        AnnotationDefinition annotation = definition.review.annotation;
        foreach (AnnotationEntryDefinition entry in annotation.entries)
        {
            AnnotationEntryState work = review.challenge.Find(entry.id);
            if (entry == null || work == null) continue;
            string type = annotation.FindOption(AnnotationSlot.Type, work.typeId, entry)?.label ?? "-";
            string treatment = annotation.FindOption(AnnotationSlot.Treatment, work.treatmentId, entry)?.label ?? "-";
            string records = work.evidenceIds != null ? string.Join(" + ", work.evidenceIds) : "-";
            page.AddSourceRow(entry.title, type + " · " + treatment + " · " + records, DataValueType.Text, "AnnotatedList" + entry.id);
        }
    }

    private static void DrawSchedule(CaseFolderUI.PageBuilder page)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = Definition;
        if (review == null || definition == null) return;
        CaseReviewDefinition rd = definition.review;
        int slots = Mathf.Clamp(rd.scheduleSlots, 1, 5);
        for (int i = 0; i < slots; i++)
        {
            int index = i;
            ScheduleRowState row = review.schedule != null && i < review.schedule.Count ? review.schedule[i] : null;
            string sourceId = !OfficeReview.IsEmpty(row)
                ? (string.IsNullOrEmpty(row.documentId) ? row.semanticKey : row.documentId)
                : null;
            string shown = OfficeReview.IsEmpty(row) ? null : sourceId + " · " + Money(row.amount);
            FindingSlot slot = FindingSlot.Make(page.AddRowObject(), rd.ScheduleSlotLabel(i), shown);
            slot.Accepts = v => CanSchedule(v, index, out _);
            slot.Rejected = () => ExplainRefusal(index);
            slot.Received = v =>
            {
                if (!ResolveRecord(v, out string documentId, out string label)) return;
                if (OfficeReview.SetScheduleRow(Level2OfficeStart.Review, Definition.review, index, v.SemanticKey, documentId, label, out _))
                {
                    Changed(page);
                    GameplayEvents.RaiseGuideCue("ScheduleLineFilled");
                }
            };
            slot.Cleared = () =>
            {
                if (OfficeReview.ClearScheduleRow(Level2OfficeStart.Review, index)) Changed(page);
                else if (OfficeReview.IsEmpty(RowAt(index))) OfferAnnotatedPapers();
            };
        }
        page.AddSourceRow("Reviewed totals",
            "May " + Money(OfficeReview.MayTotal(review)) + " · Year " + Money(OfficeReview.AnnualTotal(review, rd)),
            DataValueType.Text, "ReviewedTotals");
        if (rd.simplifiedAssessment && rd.officeFindings != null)
            foreach (OfficeFindingDefinition finding in rd.officeFindings)
                if (finding != null) DrawFinding(page, finding);
    }

    private static void DrawOfficeFindings(CaseFolderUI.PageBuilder page)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        CaseReviewDefinition definition = Definition?.review;
        if (review == null || definition?.officeFindings == null) return;
        CaseData data = CaseManager.Instance != null ? CaseManager.Instance.CurrentCase : null;
        if (selectedOfficeFindingCase != data)
        {
            selectedOfficeFindingCase = data;
            selectedOfficeFindingId = null;
        }

        var findings = new List<OfficeFindingDefinition>();
        foreach (OfficeFindingDefinition finding in definition.officeFindings)
            if (finding != null) findings.Add(finding);
        if (findings.Count == 0) return;
        if (!findings.Exists(f => f.id == selectedOfficeFindingId)) selectedOfficeFindingId = findings[0].id;

        DrawOfficeFindingTabs(page, findings);
        OfficeFindingDefinition selected = findings.Find(f => f.id == selectedOfficeFindingId);
        if (selected == null) return;
        if (!string.IsNullOrEmpty(selected.continuesFinding))
        {
            ReviewFindingState earlier = review.FindFinding(selected.continuesFinding);
            if (earlier != null)
            {
                // Keep the original source keys available from the compact page.
                page.AddSourceRow("Entry to check", earlier.concernLabel, DataValueType.Text, "FindingConcernNote");
                page.AddSourceRow("Clue", earlier.evidenceLabel, DataValueType.Text, "FindingEvidenceNote");
            }
        }
        DrawFinding(page, selected);
    }

    private static void DrawOfficeFindingTabs(CaseFolderUI.PageBuilder page, List<OfficeFindingDefinition> findings)
    {
        RectTransform paper = page.Paper;
        float tabWidth = Mathf.Min(320f, paper.rect.width * 0.38f);
        float tabHeight = 76f;
        float gap = 14f;
        float totalWidth = findings.Count * tabWidth + (findings.Count - 1) * gap;
        float x = (paper.rect.width - totalWidth) * 0.5f;
        for (int i = 0; i < findings.Count; i++)
        {
            OfficeFindingDefinition finding = findings[i];
            string id = finding.id;
            bool selected = id == selectedOfficeFindingId;
            RectTransform tab = HudTheme.PlaceTopLeft(HudTheme.CreateRect("Office finding tab " + id, paper),
                x + i * (tabWidth + gap), 148f, tabWidth, tabHeight);
            Image background = HudTheme.AddImage(tab, HudTheme.Rounded,
                selected ? new Color32(235, 222, 186, 255) : new Color32(209, 199, 173, 255), 12f);
            background.raycastTarget = true;
            Button button = tab.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() =>
            {
                if (selectedOfficeFindingId == id) return;
                selectedOfficeFindingId = id;
                page.Refresh();
            });
            string label = finding.title;
            string repeatedPrefix = "Finding " + id + " · ";
            if (!string.IsNullOrEmpty(label) && label.StartsWith(repeatedPrefix))
                label = id + " · " + label.Substring(repeatedPrefix.Length);
            TextMeshProUGUI text = HudTheme.CreateLabel("Tab label", tab, label, 24f,
                new Color32(48, 49, 43, 255), TextAlignmentOptions.Center,
                selected ? FontStyles.Bold : FontStyles.Normal);
            text.enableAutoSizing = true;
            text.fontSizeMin = 19f;
            text.fontSizeMax = 24f;
            text.textWrappingMode = TextWrappingModes.Normal;
            HudTheme.Stretch(text.rectTransform, 8f);
            page.AddOverlay(tab.gameObject);
        }
    }

    private static void DrawFinding(CaseFolderUI.PageBuilder page, OfficeFindingDefinition definition)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        if (review == null || definition?.parts == null) return;
        OfficeFindingState state = OfficeReview.Finding(review, definition.id, false);
        foreach (OfficeFindingPartDefinition part in definition.parts)
        {
            if (part == null) continue;
            OfficeFindingPartDefinition captured = part;
            FindingSlot slot = FindingSlot.Make(page.AddRowObject(), FindingSlotLabel(definition, part),
                state?.Find(part.id)?.label);
            slot.Accepts = v => Resolve(v, captured.accepts, out _, out _, out _);
            slot.Rejected = () => Level2Say.Line(captured.accepts == OfficeFindingPartDefinition.Record
                ? "This box takes a line from one of Rosa's records."
                : captured.accepts == OfficeFindingPartDefinition.Reason
                    ? "This box takes a reason from Section 7, Finding Reasons, in your book."
                    : "This box takes a conclusion from Section 8, Review Conclusions, in your book.", null, 4f);
            slot.Received = v =>
            {
                if (!Resolve(v, captured.accepts, out string documentId, out string key, out string label)) return;
                if (OfficeReview.SetPart(Level2OfficeStart.Review, Definition.review, definition.id, captured.id, documentId, key, label))
                {
                    Changed(page);
                    GameplayEvents.RaiseGuideCue("OfficeConclusionSet");
                }
            };
            slot.Cleared = () =>
            {
                if (OfficeReview.ClearPart(Level2OfficeStart.Review, definition.id, captured.id)) Changed(page);
            };
        }
    }

    private static string FindingSlotLabel(OfficeFindingDefinition definition, OfficeFindingPartDefinition part)
    {
        CaseReviewDefinition review = Definition?.review;
        if (review == null || !review.simplifiedAssessment) return definition.id + " · " + part.label;
        if (part.accepts != OfficeFindingPartDefinition.Record) return part.label;

        string title = definition.title;
        string prefix = "Finding " + definition.id + " · ";
        if (!string.IsNullOrWhiteSpace(title) && title.StartsWith(prefix)) title = title.Substring(prefix.Length);
        return string.IsNullOrWhiteSpace(title) ? part.label : title;
    }

    // =========================================================
    // VALUES
    // =========================================================

    /// <summary>
    /// The schedule source an annotated paper's highlighted line offers right
    /// now: only while the office review can be edited and the papers were
    /// collected. False for cases whose schedule uses record lines.
    /// </summary>
    public static bool AnnotatedSource(string entryId, out ScheduleSourceDefinition source)
    {
        source = null;
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = Definition;
        if (!OfficeReview.CanEdit(review) || definition == null || !definition.HasReview ||
            review.challenge == null || !review.challenge.awarded)
            return false;
        source = definition.review.FindScheduleSourceForEntry(entryId);
        return source != null;
    }

    /// <summary>
    /// A highlighted amount was picked up: an open case folder turns to the
    /// Reviewed Schedule and comes to the front, so its lines can take the drop.
    /// </summary>
    public static void PresentSchedule()
    {
        if (!OfficeReview.CanEdit(Level2OfficeStart.Review)) return;
        foreach (CaseFolderUI folder in Object.FindObjectsByType<CaseFolderUI>(FindObjectsInactive.Exclude))
        {
            if (folder == null || !folder.IsShowing) continue;
            int index = folder.FindPageIndex(ScheduleTitle);
            if (index < 0) continue;
            if (folder.CurrentPageIndex != index && !ValueDragSource.IsAnyDragActive) folder.TurnToPage(index);
            foreach (Doc3DLayer layer in Doc3DLayer.All)
                if (layer != null && layer.IsVisible && layer.Source != null && folder.PaperRect != null && folder.PaperRect.IsChildOf(layer.Source))
                    layer.BringToFront();
        }
    }

    /// <summary>A schedule line already holds this source.</summary>
    public static bool IsScheduled(string semanticKey)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        if (review?.schedule == null || string.IsNullOrEmpty(semanticKey)) return false;
        foreach (ScheduleRowState row in review.schedule)
            if (!OfficeReview.IsEmpty(row) && row.semanticKey == semanticKey) return true;
        return false;
    }

    private static bool UsesAnnotatedSources(CaseReviewDefinition review) =>
        review?.scheduleSources != null && review.scheduleSources.Exists(s => s != null && !string.IsNullOrEmpty(s.entryId));

    private static ScheduleRowState RowAt(int index)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        return review?.schedule != null && index >= 0 && index < review.schedule.Count ? review.schedule[index] : null;
    }

    /// <summary>An empty schedule box tapped with nothing picked up: bring out the papers that hold the values.</summary>
    private static void OfferAnnotatedPapers()
    {
        CaseReviewDefinition review = Definition?.review;
        if (!OfficeReview.CanEdit(Level2OfficeStart.Review) || !UsesAnnotatedSources(review)) return;
        Level2AnnotatedPapers papers = Level2AnnotatedPapers.Instance;
        if (papers != null && papers.OpenForReference())
            Level2Say.Line("Tap a highlighted amount on an annotated paper, then tap this line.", null, 5f);
        else
            Level2Say.Line("Your annotated papers hold the amounts. Open them from the side icon.", null, 5f);
    }

    private static void Changed(CaseFolderUI.PageBuilder page)
    {
        // A box's value arrives inside the transfer; redraw on the next frame.
        if (instance != null) instance.pendingRefresh = page;
        else page.Refresh();
    }

    private static bool CanSchedule(DataValue value, int index, out ScheduleRowState blockedBy)
    {
        blockedBy = null;
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = Definition;
        if (!OfficeReview.CanEdit(review) || definition == null || value == null) return false;
        ScheduleSourceDefinition source = definition.review.FindScheduleSource(value.SemanticKey);
        if (source == null || !ResolveRecord(value, out _, out _)) return false;
        // The line being replaced in this box does not count as an overlap.
        ScheduleRowState current = review.schedule != null && index < review.schedule.Count ? review.schedule[index] : null;
        if (!OfficeReview.IsEmpty(current) && current.semanticKey == value.SemanticKey) return true;
        if (review.schedule != null && index < review.schedule.Count) review.schedule[index] = null;
        blockedBy = OfficeReview.Overlap(review, source);
        if (review.schedule != null && index < review.schedule.Count) review.schedule[index] = current;
        return blockedBy == null;
    }

    /// <summary>Why a line did not fit: the same payment is already counted, or it is not a money line of a record.</summary>
    private static void ExplainRefusal(int index)
    {
        DataValue value = ValueTransferManager.Instance != null ? ValueTransferManager.Instance.SelectedValue : null;
        if (value != null && Definition?.review.FindScheduleSource(value.SemanticKey) != null)
        {
            CanSchedule(value, index, out ScheduleRowState blockedBy);
            if (blockedBy != null)
            {
                Level2Say.Line("That money is already counted in \"" + blockedBy.label + "\". Each payment goes in once.", null, 5f);
                return;
            }
        }
        Level2Say.Line(UsesAnnotatedSources(Definition?.review)
            ? "Schedule lines take a highlighted amount from your annotated papers. The records are there to read."
            : "Schedule lines take a money line from one of Rosa's records (not the prepared summary).", null, 5f);
    }

    /// <summary>A line from a record page the player holds.</summary>
    private static bool ResolveRecord(DataValue value, out string documentId, out string label)
    {
        documentId = null;
        label = null;
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = Definition;
        if (value == null || review == null || definition == null || !definition.HasReview) return false;
        ScheduleSourceDefinition annotated = definition.review.FindScheduleSource(value.SemanticKey);
        if (annotated != null && !string.IsNullOrEmpty(annotated.entryId))
        {
            AnnotationEntryDefinition entry = definition.review.annotation?.entries?.Find(e => e != null && e.id == annotated.entryId);
            if (entry == null || review.challenge == null || !review.challenge.awarded) return false;
            documentId = entry.id;
            label = entry.title + ": " + value.DisplayText;
            return true;
        }
        ReviewDocumentDefinition source = definition.review.PageForKey(definition.documents, value.SemanticKey, out string fieldLabel);
        if (source == null || source.kind != "record" || !review.HasDocument(source.id)) return false;
        documentId = source.id;
        label = source.id + " " + fieldLabel + ": " + value.DisplayText;
        return true;
    }

    private static bool Resolve(DataValue value, string accepts, out string documentId, out string key, out string label)
    {
        documentId = key = label = null;
        if (!OfficeReview.CanEdit(Level2OfficeStart.Review) || value == null) return false;
        CaseDefinition definition = Definition;
        if (definition == null) return false;
        switch (accepts)
        {
            case OfficeFindingPartDefinition.Record:
                if (!ResolveRecord(value, out documentId, out label)) return false;
                key = value.SemanticKey;
                return true;
            case OfficeFindingPartDefinition.Reason:
                if (value.SemanticKey != ReasonKey) return false;
                key = IdFor(value, id => definition.review.FindReason(id)?.label, definition.review.reasons?.ConvertAll(r => (r?.id, r?.label)));
                label = key != null ? definition.review.FindReason(key).label : null;
                return key != null;
            case OfficeFindingPartDefinition.Conclusion:
                if (value.SemanticKey != ConclusionKey) return false;
                key = IdFor(value, id => definition.review.FindConclusion(id)?.label, definition.review.conclusions?.ConvertAll(c => (c?.id, c?.label)));
                label = key != null ? definition.review.FindConclusion(key).label : null;
                return key != null;
        }
        return false;
    }

    private static string IdFor(DataValue value, System.Func<string, string> labelOf, List<(string id, string label)> all)
    {
        string text = value.Value != null ? value.Value.ToString() : value.DisplayText;
        if (labelOf(text) != null) return text;
        if (all != null)
            foreach ((string id, string label) item in all)
                if (item.label == text || item.label == value.DisplayText) return item.id;
        return null;
    }

    private static string Money(float amount) => "₱" + amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

    private void AnnounceIfComplete()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        CaseDefinition definition = Definition;
        if (review == null || definition == null) return;
        FinalAssessment.TryBegin(review, definition.review);
        if (review.officeReviewAnnounced || !OfficeReview.StructurallyComplete(review, definition.review)) return;
        review.officeReviewAnnounced = true;
        string line = definition.review.simplifiedAssessment
            ? "Your schedule and conclusion are recorded. They stand as written until the Auditor checks them. Read your summary on Final Assessment, then stamp it."
            : "Your schedule and both findings are recorded. They stand as you wrote them until the review. Choose a reason on the Final Assessment page, then stamp it.";
        Level2Say.Line(line, null, 6f);
        GameplayEvents.RaiseOfficeReviewCompleted();
    }
}
