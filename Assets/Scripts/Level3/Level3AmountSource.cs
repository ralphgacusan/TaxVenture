using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// Revised T07/T08 (5 November 2026): a printed amount on one of Bea's real
/// record papers (ClientRecordPaper) as a shared transfer source, so the
/// folder's Computation page takes it by select-source -> select-field
/// (ValueTransferManager) or drag (ValueDragSource). Only the papers the
/// office needs carry one: the collected Income &amp; Expense Summary
/// (B-WORKSHEET: Income, Supported deductions) and her withholding
/// certificate (B-WHT: the income payment and the tax withheld, both alike).
///
/// The band lies on the paper's existing touch canvas over its printed line.
/// The value is resolved from the chapter/content at selection time
/// (Level3ComputationRules.CurrentAmount: exact decimal, case, source, year,
/// content revision), never from printed text. It is offered only while the
/// paper is out, front up, at the desk, and Bea's computation is editable;
/// otherwise every touch goes to the paper as before (take out, turn, drag).
/// A neutral outline marks it; selecting it never says whether it is right.
/// </summary>
public sealed class Level3AmountSource : MonoBehaviour, IDataValueSource,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IInitializePotentialDragHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private static readonly Color Ready = new Color32(64, 98, 140, 255);
    private static readonly Color Picked = new Color32(202, 169, 101, 255);

    private ClientRecordPaper paper;
    private string sourceId;
    private string label;
    private ValueDragSource drag;
    private Image band, frame;
    private bool valueDrag;
    private bool selected;
    private bool subscribed;

    public string SourceId => sourceId;
    public bool IsValueDragging => valueDrag;

    /// <summary>Adds the bands a record needs (none for other papers). Safe to call again: existing bands are kept.</summary>
    public static int AttachTo(ClientRecordPaper paper, CaseDefinition definition)
    {
        if (paper == null || paper.Record == null || definition?.level3 == null) return 0;
        RectTransform touch = paper.transform.Find("Touch") as RectTransform;
        RectTransform face = paper.transform.Find("Card Flip/Face") as RectTransform;
        if (touch == null || face == null || touch.GetComponentInChildren<Level3AmountSource>(true) != null) return 0;
        int added = 0;
        foreach (KeyValuePair<string[], string> target in Targets(paper.Record, definition))
        {
            if (!TryArea(face, target.Key, out Rect area)) continue;
            Create(paper, touch, target.Value, area);
            added++;
        }
        if (added > 0) paper.OffersDeskValues = true;
        return added;
    }

    /// <summary>The printed parts (face child names) of each source on this record's front, in print order.</summary>
    public static List<KeyValuePair<string[], string>> Targets(ClientRecord record, CaseDefinition definition)
    {
        var targets = new List<KeyValuePair<string[], string>>();
        if (record == null || definition?.level3 == null) return targets;
        if (record.type == ClientRecord.Plain && record.source == Level3WorksheetCheck.WorksheetWorkId)
        {
            targets.Add(new KeyValuePair<string[], string>(new[] { "Summary Source Field 0", "Row Label 0", "Row Value 0" }, Level3ComputationRules.WorksheetIncome));
            targets.Add(new KeyValuePair<string[], string>(new[] { "Summary Source Field 1", "Row Label 1", "Row Value 1" }, Level3ComputationRules.WorksheetExpenses));
            return targets;
        }
        if (record.type != ClientRecord.Withholding) return targets;
        // The same split as Level3RecordRules' Type 8 print: its credit row is the TAX WITHHELD band, the others income payments.
        Level3CaseContent content = definition.level3;
        Level3SourceDefinition source = content.sources?.Find(s => s != null && s.sourceId == record.source);
        DocumentDefinition document = source != null ? definition.documents?.Find(d => d != null && d.documentName == source.documentName) : null;
        if (document?.fields == null || content.rows == null) return targets;
        string credit = null;
        var payments = new List<string>();
        foreach (DocumentFieldDefinition field in document.fields)
        {
            if (field == null || field.type != "Number") continue;
            Level3RowDefinition row = content.rows.Find(r => r != null && r.sourceId == source.sourceId && r.fieldKey == field.semanticKey);
            bool isCredit = row != null && content.creditLinks?.Find(l => l != null && l.creditRowId == row.rowId) != null;
            if (isCredit && credit == null) credit = field.semanticKey;
            else payments.Add(field.semanticKey);
        }
        int onFront = Mathf.Min(payments.Count, CaseRecordRules.WithholdingRowsPerFace);
        if (onFront == 1)
            targets.Add(new KeyValuePair<string[], string>(new[] { "Income Payment Row" }, Level3ComputationRules.CertificateChoice(source.sourceId, payments[0])));
        else
            for (int i = 0; i < onFront; i++)
                targets.Add(new KeyValuePair<string[], string>(new[] { "Row Label " + i, "Row Value " + i }, Level3ComputationRules.CertificateChoice(source.sourceId, payments[i])));
        if (credit != null)
            targets.Add(new KeyValuePair<string[], string>(new[] { "Tax Withheld Band" }, Level3ComputationRules.CertificateChoice(source.sourceId, credit)));
        return targets;
    }

    /// <summary>The union of the named printed parts, in the face's own local units (the touch canvas shares them).</summary>
    private static bool TryArea(RectTransform face, string[] names, out Rect area)
    {
        area = default;
        bool any = false;
        float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
        var corners = new Vector3[4];
        foreach (string name in names)
        {
            RectTransform part = face.Find(name) as RectTransform;
            if (part == null) continue;
            part.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 local = face.InverseTransformPoint(corner);
                xMin = Mathf.Min(xMin, local.x); xMax = Mathf.Max(xMax, local.x);
                yMin = Mathf.Min(yMin, local.y); yMax = Mathf.Max(yMax, local.y);
            }
            any = true;
        }
        if (!any) return false;
        area = Rect.MinMaxRect(xMin - 10f, yMin - 6f, xMax + 10f, yMax + 6f);
        return true;
    }

    private static void Create(ClientRecordPaper paper, RectTransform touch, string sourceId, Rect area)
    {
        RectTransform rect = HudTheme.CreateRect("Amount " + sourceId, touch);
        rect.gameObject.layer = touch.gameObject.layer;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = area.center;
        rect.sizeDelta = area.size;
        Level3AmountSource source = rect.gameObject.AddComponent<Level3AmountSource>();
        source.paper = paper;
        source.sourceId = sourceId;
        source.band = HudTheme.AddImage(rect, HudTheme.Rounded, new Color(Ready.r, Ready.g, Ready.b, 0f), 10f);
        source.band.raycastTarget = true;
        source.band.canvasRenderer.cullTransparentMesh = false;
        RectTransform outline = HudTheme.CreateRect("Frame", rect);
        outline.gameObject.layer = touch.gameObject.layer;
        HudTheme.Stretch(outline, -3f);
        source.frame = HudTheme.AddImage(outline, HudTheme.Rounded, new Color(Ready.r, Ready.g, Ready.b, 0f), 12f);
        source.frame.fillCenter = false;
        source.frame.raycastTarget = false;
        // The shared drag is driven by this band's own handlers (kept disabled so it never receives a second copy).
        source.drag = rect.gameObject.AddComponent<ValueDragSource>();
        source.drag.Initialize(source, source);
        source.drag.enabled = false;
    }

    /// <summary>The active band containing a point in a touch surface's local plane (for surfaces with a custom raycaster), or null.</summary>
    public static Level3AmountSource At(RectTransform surface, Vector3 local)
    {
        if (surface == null) return null;
        for (int i = 0; i < surface.childCount; i++)
        {
            Transform child = surface.GetChild(i);
            if (!child.gameObject.activeInHierarchy) continue;
            Level3AmountSource band = child.GetComponent<Level3AmountSource>();
            if (band == null) continue;
            RectTransform rect = (RectTransform)child;
            Vector2 centre = rect.anchoredPosition;
            Vector2 half = rect.sizeDelta * 0.5f;
            if (local.x >= centre.x - half.x && local.x <= centre.x + half.x && local.y >= centre.y - half.y && local.y <= centre.y + half.y) return band;
        }
        return null;
    }

    // =========================================================
    // VALUE
    // =========================================================

    private static Level3ChapterCheckpoint Chapter => Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;
    private static CaseDefinition Definition => CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
    private static bool AtDesk => CameraController.Instance != null && CameraController.Instance.CurrentMode == CameraController.CameraMode.Workstation;

    /// <summary>The exact amount this band offers right now, or null (not editable, lesson still due, not held).</summary>
    public static Level3TransferValue Resolve(Level3ChapterCheckpoint state, CaseDefinition definition, string sourceId)
    {
        if (!Level3ComputationRules.Editable(state, Level3ChapterRules.BeaCaseId) || Level3PracticeRules.LessonDue(state) || definition?.level3 == null) return null;
        string amount = Level3ComputationRules.CurrentAmount(state, definition, sourceId);
        if (amount == null) return null;
        string label = sourceId == Level3ComputationRules.WorksheetIncome ? "Bea's summary · Income"
            : sourceId == Level3ComputationRules.WorksheetExpenses ? "Bea's summary · " + Level3WorksheetCheck.SupportedDeductionsLabel
            : null;
        if (label == null)
        {
            int slash = sourceId.IndexOf('/');
            label = Level3ValueTransfer.FromPaper(definition, state, sourceId.Substring(6, slash - 6), sourceId.Substring(slash + 1))?.Label ?? sourceId;
        }
        return new Level3TransferValue
        {
            CaseId = definition.caseId,
            SourceId = sourceId,
            Label = label,
            Amount = amount,
            ContentRevision = definition.level3.contentRevision,
            TaxYear = definition.level3.taxYear
        };
    }

    public DataValue GetDataValue() => Level3ValueTransfer.Wrap(Resolve(Chapter, Definition, sourceId));

    /// <summary>Offered only on a paper that is out, front up, at the desk, while the computation is editable.</summary>
    private bool Available => paper != null && paper.IsOut && !paper.ShowingBack && AtDesk && GetDataValue() != null;

    // =========================================================
    // INPUT (an unavailable band hands every touch to its paper)
    // =========================================================

    public void OnPointerDown(PointerEventData eventData) => paper?.OnPointerDown(eventData);
    public void OnPointerUp(PointerEventData eventData)
    {
        if (Level3DeskToolHitTarget.Canceled(eventData)) CancelOwned();
        paper?.OnPointerUp(eventData);
    }
    public void OnInitializePotentialDrag(PointerEventData eventData) => paper?.OnInitializePotentialDrag(eventData);

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left || MobileInputGuard.IsDragOrCancelled(eventData)) return;
        if (!paper.IsSettledOut || !Available || ValueTransferManager.Instance == null) { paper?.OnPointerClick(eventData); return; }
        ValueTransferManager.Instance.SelectValue(this, this);
        MobileInputGuard.SuppressWorldTapsThisFrame();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        valueDrag = Available && eventData != null && eventData.button == PointerEventData.InputButton.Left;
        if (valueDrag)
        {
            drag.OnBeginDrag(eventData);
            valueDrag = ValueDragSource.IsAnyDragActive;
            // A value drag is not a long press on the paper: its holder's grouping hold ends here.
            if (valueDrag) { paper?.OnPointerUp(eventData); return; }
        }
        paper?.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (valueDrag) { drag.OnDrag(eventData); MobileInputGuard.SuppressWorldTapsThisFrame(); }
        else paper?.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (valueDrag) drag.OnEndDrag(eventData);
        else paper?.OnEndDrag(eventData);
        valueDrag = false;
    }

    // =========================================================
    // NEUTRAL OUTLINE AND OWNED SELECTION
    // =========================================================

    private void OnEnable() => Subscribe();
    private void Start() => Subscribe();

    private void Subscribe()
    {
        if (subscribed || ValueTransferManager.Instance == null) return;
        ValueTransferManager.Instance.OnSelectionChanged += HandleSelection;
        subscribed = true;
        HandleSelection(ValueTransferManager.Instance.SelectedSourceComponent);
    }

    private void HandleSelection(object source) => selected = ReferenceEquals(source, this);

    private void Update()
    {
        if (!subscribed) Subscribe();
        if (!AtDesk || InGameSettings.IsOpen || DialogueUI.IsAnyDialogueActive ||
            (CameraController.Instance != null && CameraController.Instance.IsTransitioning)) CancelOwned();
        bool offered = Available;
        float alpha = selected ? 0.32f : offered ? 0.08f : 0f;
        Color tint = selected ? Picked : sourceId != null && sourceId.StartsWith("worksheet:", System.StringComparison.Ordinal) ? new Color32(179, 124, 48, 255) : Ready;
        if (band != null) band.color = new Color(tint.r, tint.g, tint.b, alpha);
        if (frame != null) frame.color = new Color(tint.r, tint.g, tint.b, selected ? 0.9f : offered ? 0.45f : 0f);
        // A selection from this paper lasts while Bea's computation is open at the desk (page turns keep it).
        if (selected && (!AtDesk || !Level3ComputationRules.Editable(Chapter, Level3ChapterRules.BeaCaseId)))
            ValueTransferManager.Instance?.ClearSelection();
    }

    public static void CancelFor(ClientRecordPaper owner)
    {
        if (owner == null) return;
        foreach (Level3AmountSource source in owner.GetComponentsInChildren<Level3AmountSource>(true)) source.CancelOwned();
    }

    private void CancelOwned()
    {
        if (valueDrag) { drag.OnEndDrag(null); valueDrag = false; }
        if (ReferenceEquals(ValueTransferManager.Instance?.SelectedSourceComponent, this)) ValueTransferManager.Instance.ClearSelection();
    }

    private void OnDisable() => CancelOwned();
    private void OnApplicationFocus(bool focused) { if (!focused) CancelOwned(); }
    private void OnApplicationPause(bool paused) { if (paused) CancelOwned(); }

    private void OnDestroy()
    {
        ValueTransferManager manager = ValueTransferManager.Instance;
        if (manager == null) return;
        if (subscribed) manager.OnSelectionChanged -= HandleSelection;
        if (ReferenceEquals(manager.SelectedSourceComponent, this)) manager.ClearSelection();
    }
}
