using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Rosa's Case Folder on the Level 1 foundation (Level 2 consistency pass),
/// under Level2Gameplay/CaseRecords. Page 1 stays the editable Taxpayer
/// Information page; page 2 is "Supporting Documents" on THIS desk folder
/// (CaseFolderUI.InstancePages, found by its role), holding real physical
/// records through Level 1's own AttachedRecordsPage / ClientRecordPaper: the
/// same 3D bodies, clips, contact and drop shadows, tap/drag out, front focus,
/// safe screen bounds, optional two-second grouping, return, foreign-folder
/// refusal and gather-on-close/interruption. Rosa's working pages (Summary,
/// Payment Note, Findings, Reviewed Schedule, Final Assessment) follow it.
///
/// RECORDS (CaseRecordRules.BuildReview, values straight from the case JSON):
///   the office's Client Background notes (Type 2, review.background: the
///     facts the retired Taxpayer Profile page printed) from the start;
///   once Rosa has handed her records over (review.HandoverDone), each of her
///     handover records by its canonical id: D1 Collection Sheet (Type 4),
///     D5 Loan Agreement (Type 7), D3 and D4 receipts (Type 5 slips).
///   Legacy-only records (D2, D6) are never added. Record faces are never
///   value sources: the schedule still takes its amounts from the annotated
///   papers, the Findings slip from the Summary and Payment Note pages.
///
/// The record set is rebuilt only while the folder is closed and nothing is
/// out, so no record is orphaned, duplicated or left on another folder.
/// Reading a record marks its case document read (consultation clues use it);
/// nothing here changes an answer, reward, clock or review state.
/// </summary>
public sealed class Level2CaseRecords : MonoBehaviour
{
    public const string ChildName = "CaseRecords";
    public const string BackgroundDocumentName = "Client Background (office notes)";
    private const string DeskFolderPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/InteractiveFolderObjects/OpenCaseFolder";

    public static Level2CaseRecords Active { get; private set; }

    private CaseFolderUI pages;
    private Transform folder;
    private AttachedRecordsPage records;
    private bool builtWithHandover;
    private List<ClientRecord> built = new List<ClientRecord>();

    /// <summary>The records on the page now (page order).</summary>
    public IReadOnlyList<ClientRecord> Built => built;
    public AttachedRecordsPage Records => records;

    private void OnEnable() => Active = this;

    private void OnDisable()
    {
        if (Active == this) Active = null;
        if (pages != null)
        {
            pages.PageRendered -= HandlePageRendered;
            if (pages.InstancePages == (Func<CaseData, List<CaseFolderPageContent>>)SupportingPages) pages.InstancePages = null;
        }
    }

    private IEnumerator Start()
    {
        float waited = 0f;
        while (!TryBind())
        {
            if (waited > 12f) { Debug.LogWarning("[Level2] Rosa's desk folder was not found; Supporting Documents is not attached."); yield break; }
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private bool TryBind()
    {
        CaseManager cases = CaseManager.Instance;
        if (cases == null || cases.CurrentCase?.review == null || cases.CurrentDefinition == null || !cases.CurrentDefinition.HasReview) return false;
        // The checkpoint first, so a resumed review builds its records once.
        if (Level2Checkpointer.Active != null && !Level2Checkpointer.Active.IsReady) return false;
        folder = PlayerPlacement.Find(gameObject.scene, DeskFolderPath);
        CaseFolderUI ui = folder != null ? folder.GetComponentInChildren<CaseFolderUI>(true) : null;
        if (ui == null || ui.IsReference || ui.PaperRect == null) return false;
        pages = ui;
        pages.InstancePages = SupportingPages;
        pages.PageRendered += HandlePageRendered;
        Rebuild();
        return true;
    }

    private void Update()
    {
        if (pages == null) return;
        CaseReviewState review = Level2OfficeStart.Review;
        bool handedOver = review != null && review.HandoverDone;
        if (handedOver == builtWithHandover) return;
        // Only on a closed, idle folder: nothing is out, nothing is mid-flight.
        if (pages.IsShowing || (records != null && records.AnyOut)) return;
        Rebuild();
    }

    private void Rebuild()
    {
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        CaseReviewState review = Level2OfficeStart.Review;
        if (definition == null || review == null || pages == null) return;
        if (records != null)
        {
            records.Shown -= HandleShown;
            records.Dispose();
            records = null;
        }
        builtWithHandover = review.HandoverDone;
        built = CaseRecordRules.BuildReview(definition.caseId, definition.fullName, Background(definition),
            builtWithHandover ? HandoverRecords(definition, review) : null);
        if (built.Count == 0) return;
        records = AttachedRecordsPage.Create(gameObject, folder, pages, -1, definition.caseId, built, RecordPageLayout.ReviewRecords,
            () => pages != null && pages.IsShowing, CaseFolderPageContent.SupportingRecordsRole);
        if (records == null)
        {
            Debug.LogWarning("[Level2] Rosa's supporting records could not be attached to the desk folder.");
            return;
        }
        records.Shown += HandleShown;
        pages.RefreshCurrentPage();
        Debug.Log($"[Level2] Supporting Documents: {built.Count} record(s) ({string.Join(", ", built.ConvertAll(r => r.tab))}).");
    }

    /// <summary>The client background notes (review.background), the old profile page's facts in Level 1's background layout.</summary>
    public static ReviewBackgroundInput Background(CaseDefinition definition)
    {
        ReviewBackgroundDefinition source = definition?.review?.background;
        if (source == null) return null;
        return new ReviewBackgroundInput
        {
            documentName = BackgroundDocumentName,
            period = source.period,
            home = source.home,
            work = source.work,
            income = source.income
        };
    }

    /// <summary>The records Rosa handed over, by canonical id in handover order (never a legacy-only record).</summary>
    public static List<ReviewRecordInput> HandoverRecords(CaseDefinition definition, CaseReviewState review)
    {
        var inputs = new List<ReviewRecordInput>();
        CaseReviewDefinition rd = definition?.review;
        if (rd?.handoverDocumentIds == null || rd.documents == null || review == null) return inputs;
        foreach (string id in rd.handoverDocumentIds)
        {
            ReviewDocumentDefinition record = rd.FindDocument(id);
            if (record == null || record.kind != "record" || !review.HasDocument(id)) continue;
            DocumentDefinition document = definition.documents?.Find(d => d != null && d.documentName == record.documentName);
            if (document == null)
            {
                // Never another record in its place: it is left out and named.
                Debug.LogWarning($"[Level2] Record {id} has no case document \"{record.documentName}\"; it is not attached.");
                continue;
            }
            var input = new ReviewRecordInput
            {
                id = record.id,
                documentName = record.documentName,
                title = record.title,
                paperType = record.paperType,
                paperTitle = record.paperTitle,
                issuer = record.issuer,
                reference = record.reference,
                date = record.date,
                status = record.paperStatus,
                totalKey = record.paperTotalKey
            };
            if (document.fields != null)
                foreach (DocumentFieldDefinition field in document.fields)
                    if (field != null) input.fields.Add(new CaseRecordField { label = field.label, value = field.displayValue, key = field.semanticKey });
            inputs.Add(input);
        }
        return inputs;
    }

    /// <summary>Page two of this folder: the attached records draw themselves (no rows, never a value source).</summary>
    private List<CaseFolderPageContent> SupportingPages(CaseData data) => new List<CaseFolderPageContent>
    {
        new CaseFolderPageContent(CaseFolderPageContent.SupportingRecordsHeading, "")
        {
            Render = DrawNote,
            Role = CaseFolderPageContent.SupportingRecordsRole
        }
    };

    /// <summary>Before the handover one small printed line says where Rosa's originals are.</summary>
    private void DrawNote(CaseFolderUI.PageBuilder page)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        if (review == null || review.HandoverDone || page?.Paper == null) return;
        TextMeshProUGUI note = HudTheme.CreateLabel("Originals Note", page.Paper, "Rosa's original May records: at her stall, Plaza Market",
            26f, new Color32(94, 84, 70, 255), TextAlignmentOptions.Center, FontStyles.Italic);
        RectTransform rect = note.rectTransform;
        rect.anchorMin = new Vector2(0.08f, 0.08f);
        rect.anchorMax = new Vector2(0.92f, 0.16f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        note.raycastTarget = false;
        page.AddOverlay(note.gameObject);
        rect.anchorMin = new Vector2(0.08f, 0.08f);
        rect.anchorMax = new Vector2(0.92f, 0.16f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void HandlePageRendered(int index)
    {
        if (records != null && index == records.PageIndex) records.SetEventCamera(HeldOnScreen.ActiveCamera(Camera.main));
    }

    private static void HandleShown(ClientRecord record)
    {
        // The existing "document read" mark (Rosa's consultation clues use it). Changes no answer.
        if (record != null && !string.IsNullOrEmpty(record.source)) CaseFolderUI.MarkReviewDocumentRead(record.source);
    }
}
