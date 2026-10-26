using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Reading Arthur's Reviewed File (Campaign Foundation T30, revised
/// 2026-10-26; physical records per T31). The file is a separate copy of the
/// in-game Case Folder, not a new folder UI: ReviewedFileLook copies the
/// desk's open folder (model, paper, typography, page arrows) with a teal
/// cover, CaseFolderUI prints it from Arthur's finished data
/// (UseReference), and CaseFolder3DView drives it exactly like Marie's folder
/// opened from the bar: the same open pose and flight, cover drag, document
/// layering, page turns, and close/store by dropping it on the bar or Bag
/// (or tapping its slot again).
///   Page 1 "Taxpayer Information": the former client's completed entries,
///     in the folder's own rows, read-only.
///   Page 2 "Supporting Documents": his ID card in photo corners above his
///     background sheet under a clip: attached physical records handled by
///     the shared AttachedRecordsPage (T31: take out by tap or drag, move with
///     the finger, front on touch, back on the page, optional hold-to-group).
///
/// ISOLATION: the copy prints only ReviewedFileRules.Content(); its document
/// layer is AlwaysReadOnly, so no value can be selected, given, taken or
/// placed in it (at the desk too). It never touches CaseManager, Marie's
/// folder or its store, value transfers or the Auditor; nothing is submitted.
///
/// PROGRESS: records shown (settled out of the folder) and put back, and
/// page one shown again, are reported to ArthurOpening, which saves them.
/// The viewer saves nothing; closing gathers any record that is out.
/// </summary>
public sealed class ReviewedFileViewer : MonoBehaviour
{
    public const string SupportingPageTitle = CaseFolderPageContent.SupportingRecordsHeading;
    public const int SupportingPageIndex = 1;

    public static ReviewedFileViewer Instance { get; private set; }

    private ArthurOpening owner;
    private Transform copy;
    private CaseFolderUI pages;
    private CaseFolder3DView view;
    private Doc3DLayer layer;
    private AttachedRecordsPage records;
    private bool returningToFront;

    public bool IsOpen => view != null && view.IsOpen;
    public bool IsOut => view != null && view.IsOut;
    public int PageIndex => pages != null ? pages.CurrentPageIndex : 0;
    public AttachedRecordsPage Records => records;

    public static ReviewedFileViewer Create(Scene scene, ArthurOpening owner)
    {
        var host = new GameObject("Arthur's Reviewed File (viewer)");
        SceneManager.MoveGameObjectToScene(host, scene);
        ReviewedFileViewer viewer = host.AddComponent<ReviewedFileViewer>();
        viewer.owner = owner;
        Instance = viewer;
        return viewer;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (pages != null) pages.PageRendered -= HandlePageRendered;
        if (copy != null) Destroy(copy.gameObject);
        if (layer != null) Destroy(layer.gameObject);
    }

    // =========================================================
    // BUILD (on first use: the HUD and its document layers exist by then)
    // =========================================================

    private bool Ensure()
    {
        if (view != null) return true;
        Scene scene = gameObject.scene;
        Transform hudCanvas = HudCanvasRoot();
        int interactable = LayerMask.NameToLayer("Interactable");
        copy = ReviewedFileLook.BuildOpenCopy(scene, interactable < 0 ? 3 : interactable);
        if (copy == null || hudCanvas == null)
            return FailBuild("No desk Case Folder or HUD document layer here; Arthur's file cannot open.");
        pages = copy.GetComponentInChildren<CaseFolderUI>(true);
        if (pages == null || pages.PaperRect == null)
            return FailBuild("The folder copy has no pages.");
        pages.UseReference(BuildData(), new List<CaseFolderPageContent>
        {
            new CaseFolderPageContent(SupportingPageTitle, "") { Render = page => { }, Role = CaseFolderPageContent.SupportingRecordsRole }
        });
        pages.PageRendered += HandlePageRendered;

        layer = Doc3DLayer.Create(hudCanvas, copy, "Reviewed File");
        layer.AlwaysComposite = true;   // one look, like the Case Folder's own layer
        layer.AlwaysReadOnly = true;
        view = CaseFolder3DView.CreateReference(gameObject, scene, copy, ItemPlace, layer);
        if (view == null) return FailBuild("The folder copy could not be connected to its viewer.");
        view.Opened += HandleOpened;
        // Page two's attached records (the copy is still at rest at the desk folder's pose: measured at normal size).
        records = AttachedRecordsPage.Create(gameObject, copy, pages, SupportingPageIndex, ReviewedFileRules.ClientId,
            ReviewedFileRules.Content().records, RecordPageLayout.Default, () => view != null && view.IsOpen);
        if (records == null || records.Papers.Count != ReviewedFileRules.RecordIds.Length)
            return FailBuild("The supporting records could not be attached to the folder.");
        records.Shown += record => { if (owner != null) owner.RecordShown(record.id); };
        records.PutBack += HandleRecordReturned;
        records.Flipped += (record, backUp) => { if (owner != null) owner.RecordFlipped(backUp); };
        records.Grouped += () => { if (owner != null) owner.RecordsGrouped(); };
        return true;
    }

    private bool FailBuild(string reason)
    {
        Debug.LogWarning("[ReviewedFile] " + reason);
        if (pages != null) pages.PageRendered -= HandlePageRendered;
        if (records != null) Destroy(records);
        if (view != null) Destroy(view);
        if (layer != null) Destroy(layer.gameObject);
        if (copy != null) Destroy(copy.gameObject);
        records = null;
        view = null;
        pages = null;
        layer = null;
        copy = null;
        return false;
    }

    private void HandleRecordReturned(ClientRecord record)
    {
        if (owner == null) return;
        owner.RecordPutBack(record.id);
        if (!returningToFront && owner.HasInspectedRecords && owner.HasReturnedRecords &&
            records != null && !records.IsGathering && IsOpen)
        {
            returningToFront = true;
            StartCoroutine(ReturnToCompletedPage());
        }
    }

    private IEnumerator ReturnToCompletedPage()
    {
        // Let the last record's attachment finish before turning the actual page.
        yield return null;
        bool allAttached = records != null;
        if (records != null)
            foreach (ClientRecordPaper paper in records.Papers)
                if (!paper.IsAttached) allAttached = false;
        if (allAttached && IsOpen && !records.IsGathering && pages.CurrentPageIndex == SupportingPageIndex)
            pages.TurnToPage(0);
        returningToFront = false;
    }

    /// <summary>The HUD canvas the Case Folder's document layer lives on.</summary>
    private static Transform HudCanvasRoot()
    {
        foreach (Doc3DLayer existing in Doc3DLayer.All)
            if (existing != null && existing.Source != null && existing.Source.name == "OpenCaseFolder") return existing.transform.parent;
        return null;
    }

    /// <summary>Where the file sits now: its bar slot, or the Bag.</summary>
    private static RectTransform ItemPlace()
    {
        HudHotbar hotbar = HudHotbar.Instance;
        return hotbar == null ? null : hotbar.ItemTarget(ReviewedFileRules.ItemId) ?? hotbar.BagIcon;
    }

    /// <summary>Arthur's former client as the folder prints it: name, TIN and the three completed entries.</summary>
    private static CaseData BuildData()
    {
        ReviewedFileContent content = ReviewedFileRules.Content();
        var data = new CaseData { fullName = content.clientName, tin = content.tin };
        foreach (ReviewedEntry entry in content.entries)
        {
            if (entry == null) continue;
            switch (entry.key)
            {
                case "ResidencyStatus":
                    if (Enum.TryParse(entry.value, out ResidencyStatus residency)) data.residencyStatus = residency;
                    break;
                case "TaxpayerType":
                    if (Enum.TryParse(entry.value, out TaxpayerType type)) data.taxpayerType = type;
                    break;
                case "IncomeSource":
                    if (Enum.TryParse(entry.value, out IncomeSource source)) data.incomeSource = source;
                    break;
            }
            // Completed: the rows print their value as filled in.
            data.folderTransferredFields.Add(entry.key);
        }
        return data;
    }

    private void HandlePageRendered(int index)
    {
        if (owner != null) owner.PageShown(index);
    }

    // =========================================================
    // OPEN / CLOSE (CaseFolder3DView)
    // =========================================================

    /// <summary>Opens the file (at screenPosition when dragged out of its slot). False when it cannot open now.</summary>
    public bool Open(Vector2? screenPosition)
    {
        if (!Ensure()) return false;
        if (!view.Open(screenPosition)) return false;
        return true;
    }

    /// <summary>Tap on its slot while it is out: it closes back in, like the Case Folder icon.</summary>
    public void Toggle()
    {
        if (view != null && view.IsOpen) view.Close();
        else Open(null);
    }

    private void HandleOpened()
    {
        if (records != null) records.SetEventCamera(HeldOnScreen.ActiveCamera(Camera.main));
        if (owner != null) owner.ViewerOpened();
    }

    // =========================================================
    // ARTHUR'S EMPHASIS (this reference file only)
    // =========================================================

    /// <summary>A brief, gentle pulse on front-page rows from..to (by order: classification, type, income), while page one shows.</summary>
    public void Emphasize(int from, int to, float seconds)
    {
        if (pages == null || !isActiveAndEnabled) return;
        StartCoroutine(Pulse(from, to, seconds));
    }

    private IEnumerator Pulse(int from, int to, float seconds)
    {
        // The front page prints Name and TIN first, then the three entries.
        const int firstEntryRow = 2;
        var rows = new List<RectTransform>();
        IReadOnlyList<CaseFolderFieldRow> all = pages.FrontPageRows;
        for (int i = from; i <= to; i++)
        {
            int index = firstEntryRow + i;
            if (index >= 0 && index < all.Count && all[index] != null) rows.Add((RectTransform)all[index].transform);
        }
        float elapsed = 0f;
        while (elapsed < seconds && pages.CurrentPageIndex == 0)
        {
            elapsed += Time.unscaledDeltaTime;
            float scale = 1f + 0.035f * Mathf.Sin(elapsed / seconds * Mathf.PI * 3f) * (1f - elapsed / seconds);
            foreach (RectTransform row in rows) if (row != null) row.localScale = Vector3.one * Mathf.Max(1f, scale);
            yield return null;
        }
        foreach (RectTransform row in rows) if (row != null) row.localScale = Vector3.one;
    }
}
