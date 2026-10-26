using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// A folder's supporting page with attached physical records (Campaign
/// Foundation T31): Arthur's Reviewed File now (T30), Marie's case folder in
/// T25 (her ID, background and 2316, through RecordPageLayout). Folder-
/// independent: give it the folder's root, its CaseFolderUI, the page index,
/// an owner id and the records.
///
///   On the page: records lie in their holders (ClientRecordPaper) on an
///     anchor that follows the page's paper every frame (the folder model may
///     be scaled unevenly, so they live in an inverse-scaled uniform holder
///     inside the folder: drawn by the folder's own document layer).
///   Out of the folder: a tap or drag takes one out; several can be out,
///     each reachable; a touch brings one in front. On a small phone one face
///     is read at a time, the others wait small at the lower left (still a tap
///     away). A drop on this folder's page puts a record back (turning to the
///     page first); a drop on another client's folder is refused with a small
///     shake, nothing moves between folders.
///   Optional grouping: resting a finger still on a taken-out record for two
///     seconds (a ring fills) gathers this folder's taken-out records into one
///     staggered stack in their original order; dragging moves the stack as
///     one (kept on screen) and a drop on the page returns each record once.
///     Moving past the drag threshold, lifting the finger, a cancelled pointer
///     or losing focus cancels the hold; ordinary taps never wait.
///   Interruptions: closing/storing the folder, a conversation, Settings, a
///     result card or a camera change gather every record back.
/// Nothing is saved here: Shown / PutBack report reading progress to the owner.
/// </summary>
public sealed class AttachedRecordsPage : MonoBehaviour, IClientRecordHolder
{
    private const float FrontStep = 0.012f;
    private const float GroupStagger = 0.035f;
    private const float ArrangeSeconds = 0.28f;
    private static readonly Quaternion ReadingTilt = Quaternion.identity;

    private Transform folderRoot;
    private CaseFolderUI pages;
    private int fixedPageIndex;
    private string pageRole;
    private string ownerId;
    private Func<bool> folderOpen;
    private Transform recordsRoot;
    private Transform detachedWorkspace;
    private Transform pageAnchor;
    private float referencePageWidth;
    private readonly List<ClientRecordPaper> papers = new List<ClientRecordPaper>();
    private readonly List<ClientRecordPaper> outOrder = new List<ClientRecordPaper>();
    private readonly HashSet<string> shownOut = new HashSet<string>();
    private readonly List<ClientRecordPaper> group = new List<ClientRecordPaper>();
    private static readonly Vector3[] Corners = new Vector3[4];

    // The two-second hold.
    private ClientRecordPaper holdPaper;
    private int holdPointer;
    private Vector2 holdStart;
    private float holdElapsed;
    private bool holdDown;
    private bool focused = true;
    private RectTransform holdRing;
    private Image holdFill;

    // A grouped drag.
    private HeldOnScreen groupHeld;
    private readonly List<Vector3> groupOffsets = new List<Vector3>();
    private readonly List<float> groupDepths = new List<float>();
    private bool groupDragging;
    private ClientRecordPaper groupedByHold;
    private Camera lastCamera;
    private Rect lastViewport;
    private float lastFov;

    /// <summary>A record was shown (settled out of the folder).</summary>
    public event Action<ClientRecord> Shown;
    /// <summary>A shown record is back in its holder.</summary>
    public event Action<ClientRecord> PutBack;
    /// <summary>An ID card was turned over (true = its back, the profile, is up).</summary>
    public event Action<ClientRecord, bool> Flipped;
    /// <summary>The optional two-second hold stacked this folder's taken-out records.</summary>
    public event Action Grouped;

    public IReadOnlyList<ClientRecordPaper> Papers => papers;
    public string OwnerId => ownerId;
    public bool IsGathering { get; private set; }
    public Camera ViewCamera => HeldOnScreen.ActiveCamera(Camera.main);
    public bool IsGrouped(ClientRecordPaper paper) => group.Contains(paper);

    /// <summary>
    /// Builds the records on folderRoot's page pageIndex (the folder at rest, so the page is measured
    /// at its normal size). folderOpen says whether the folder is open (returns need an open page).
    /// With pageRole the page is found by its role in the folder's current pages instead (T25: the
    /// active case's page list is rebuilt on every opening; never a fixed page number).
    /// </summary>
    public static AttachedRecordsPage Create(GameObject host, Transform folderRoot, CaseFolderUI pages, int pageIndex, string ownerId,
        IList<ClientRecord> records, RecordPageLayout layout, Func<bool> folderOpen, string pageRole = null)
    {
        if (host == null || folderRoot == null || pages == null || pages.PaperRect == null || records == null) return null;
        var page = host.AddComponent<AttachedRecordsPage>();
        page.folderRoot = folderRoot;
        page.pages = pages;
        page.fixedPageIndex = pageIndex;
        page.pageRole = pageRole;
        page.ownerId = ownerId;
        page.folderOpen = folderOpen;
        page.Build(records, layout ?? RecordPageLayout.Default, folderRoot.gameObject.layer);
        pages.PageRendered += page.HandlePageRendered;
        pages.Closing += page.GatherAll;
        return page;
    }

    private void OnDestroy()
    {
        if (pages != null)
        {
            pages.PageRendered -= HandlePageRendered;
            pages.Closing -= GatherAll;
        }
        if (holdRing != null) Destroy(holdRing.root.gameObject);
        if (detachedWorkspace != null) Destroy(detachedWorkspace.gameObject);
    }

    private void Build(IList<ClientRecord> records, RecordPageLayout layout, int layer)
    {
        detachedWorkspace = new GameObject("Detached Records " + ownerId).transform;
        SceneManager.MoveGameObjectToScene(detachedWorkspace.gameObject, gameObject.scene);
        Vector3 s = folderRoot.lossyScale;
        recordsRoot = new GameObject("Attached Records").transform;
        recordsRoot.gameObject.AddComponent<AttachedRecordsRoot>();
        recordsRoot.gameObject.layer = layer;
        recordsRoot.SetParent(folderRoot, false);
        // Undo the folder's uneven scale (identity rotation, so the result is uniform).
        recordsRoot.localScale = new Vector3(1f / Mathf.Max(1e-6f, s.x), 1f / Mathf.Max(1e-6f, s.y), 1f / Mathf.Max(1e-6f, s.z));
        pageAnchor = new GameObject("Supporting Page").transform;
        pageAnchor.gameObject.layer = layer;
        pageAnchor.SetParent(recordsRoot, false);

        PagePose(out _, out _, out float width, out float height);
        referencePageWidth = Mathf.Max(1e-5f, width);
        FollowPage();

        var types = new List<string>();
        foreach (ClientRecord record in records) types.Add(record != null ? record.type : "");
        List<RecordSlot> slots = ClientRecordRules.LayoutPage(width, height, types, layout);
        for (int i = 0; i < records.Count && i < slots.Count; i++)
        {
            if (records[i] == null) continue;
            var slot = new GameObject("Holder " + records[i].tab).transform;
            slot.gameObject.layer = layer;
            slot.SetParent(pageAnchor, false);
            slot.localPosition = new Vector3(slots[i].x, 0.0004f, slots[i].z);
            papers.Add(ClientRecordPaper.Create(this, slot, detachedWorkspace, records[i], new Vector2(slots[i].width, slots[i].height), layer));
        }
        pageAnchor.gameObject.SetActive(false);
    }

    // =========================================================
    // THE PAGE
    // =========================================================

    /// <summary>The page paper's frame from its world corners: centre, +Z its top, +Y out toward the reader.</summary>
    private void PagePose(out Vector3 centre, out Quaternion rotation, out float width, out float height)
    {
        RectTransform paper = pages.PaperRect;
        paper.GetWorldCorners(Corners);   // bottom-left, top-left, top-right, bottom-right
        Vector3 right = Corners[3] - Corners[0], up = Corners[1] - Corners[0];
        width = right.magnitude;
        height = up.magnitude;
        Vector3 normal = Vector3.Cross(up, right).normalized;   // the side the print faces
        Vector3 pageUp = Vector3.Cross(right.normalized, normal);
        centre = (Corners[0] + Corners[2]) * 0.5f;
        rotation = Quaternion.LookRotation(pageUp, normal);
    }

    private void FollowPage()
    {
        PagePose(out Vector3 centre, out Quaternion rotation, out float width, out _);
        pageAnchor.SetPositionAndRotation(centre, rotation);
        float parentScale = Mathf.Max(1e-6f, recordsRoot.lossyScale.x);
        pageAnchor.localScale = Vector3.one * (width / referencePageWidth / parentScale);
    }

    /// <summary>The folder is open and showing (guide targets read this; never changes anything).</summary>
    public bool IsOpenNow => FolderOpen && pages != null && pages.IsShowing;
    /// <summary>The page paper the records lie on and go back to.</summary>
    public RectTransform PageRect => pages != null ? pages.PaperRect : null;
    /// <summary>The folder's page showing now.</summary>
    public int CurrentPage => pages != null ? pages.CurrentPageIndex : -1;

    /// <summary>The supporting page's index in the folder's current pages (-1 while it has none).</summary>
    public int PageIndex => pageRole != null ? pages.FindPageIndexByRole(pageRole) : fixedPageIndex;

    private void HandlePageRendered(int index)
    {
        if (pageAnchor != null) pageAnchor.gameObject.SetActive(index == PageIndex);
    }

    public void SetEventCamera(Camera camera)
    {
        foreach (ClientRecordPaper paper in papers) paper.SetEventCamera(camera);
    }

    private bool FolderOpen => folderOpen == null || folderOpen();

    private void LateUpdate()
    {
        if (pageAnchor == null || folderRoot == null || !folderRoot.gameObject.activeInHierarchy) return;
        // Attached records show only on their page of an open folder (a closed desk folder shows none).
        bool onPage = pages.IsShowing && pages.CurrentPageIndex == PageIndex;
        if (pageAnchor.gameObject.activeSelf != onPage) pageAnchor.gameObject.SetActive(onPage);
        FollowPage();
        // Interruptions put everything back (a safe, idle folder).
        if (outOrder.Count > 0 && (DialogueUI.IsAnyDialogueActive || InGameSettings.IsOpen || HudRewardScreen.AnyOpen)) GatherAll();
        Camera camera = ViewCamera;
        if (lastCamera != null && camera != lastCamera) GatherAll();
        if (camera != null && (camera != lastCamera || camera.pixelRect != lastViewport || !Mathf.Approximately(camera.fieldOfView, lastFov)))
        {
            CancelHold();
            groupDragging = false;
            Ungroup();
            foreach (ClientRecordPaper paper in outOrder)
            {
                paper.CancelDrag();
                paper.FitInsideView(camera);
            }
            lastCamera = camera;
            lastViewport = camera.pixelRect;
            lastFov = camera.fieldOfView;
        }
        UpdateHold();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        focused = hasFocus;
        if (hasFocus) return;
        CancelHold();
        foreach (ClientRecordPaper paper in papers) paper.CancelDrag();
        groupDragging = false;
    }

    // =========================================================
    // OUT OF THE FOLDER (IClientRecordHolder)
    // =========================================================

    private float FolderDepth(Camera camera)
    {
        RectTransform paper = pages.PaperRect;
        Vector3 centre = paper.TransformPoint(paper.rect.center);
        return Vector3.Dot(centre - camera.transform.position, camera.transform.forward);
    }

    private float FrontDepth(Camera camera, int rank) =>
        Mathf.Max(camera.nearClipPlane + 0.12f, FolderDepth(camera) * 0.82f - FrontStep * rank);

    private static bool FocusOne(Camera camera) =>
        ClientRecordRules.FocusOne(camera.pixelWidth, camera.pixelHeight, Screen.dpi);

    public void ReadingPose(ClientRecordPaper paper, out Vector3 position, out Quaternion rotation, out float worldScale)
    {
        Camera camera = ViewCamera;
        bool card = paper.Record.IsCard;
        int rank = outOrder.Contains(paper) ? outOrder.IndexOf(paper) : outOrder.Count;
        bool focus = FocusOne(camera);
        // Beside the folder, on the left (the side records slide out to); a second sheet (T25: Marie's
        // 2316 beside her background) reads on the right, so the two can be compared side by side.
        // On a small phone, one larger face.
        bool right = !card && SheetOrdinal(paper) % 2 == 1;
        Rect pixels = camera.pixelRect;
        Vector2 screen = focus
            ? new Vector2(pixels.x + pixels.width * 0.36f, pixels.y + pixels.height * 0.52f)
            : new Vector2(pixels.x + pixels.width * (card ? 0.3f : right ? 0.82f : 0.18f), pixels.y + pixels.height * (card ? 0.7f : 0.5f));
        PoseAt(camera, paper, screen, FrontDepth(camera, rank), focus ? (card ? 0.46f : 0.76f) : (card ? 0.34f : 0.68f), focus ? 0.62f : 0.45f,
            out position, out rotation, out worldScale);
    }

    /// <summary>The paper's place among this page's sheets (0 for the first sheet), in page order.</summary>
    private int SheetOrdinal(ClientRecordPaper paper)
    {
        int ordinal = 0;
        foreach (ClientRecordPaper other in papers)
        {
            if (other == paper) return ordinal;
            if (other != null && !other.Record.IsCard) ordinal++;
        }
        return ordinal;
    }

    /// <summary>Facing the camera at screen, depth deep, heightShare of the screen tall (at most widthShare wide).</summary>
    private static void PoseAt(Camera camera, ClientRecordPaper paper, Vector2 screen, float depth, float heightShare, float widthShare,
        out Vector3 position, out Quaternion rotation, out float worldScale)
    {
        position = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        rotation = Quaternion.LookRotation(camera.transform.up, -camera.transform.forward) * ReadingTilt;
        float visibleHeight = 2f * depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float visibleWidth = visibleHeight * Mathf.Max(0.1f, camera.aspect);
        worldScale = Mathf.Min(visibleHeight * heightShare / paper.Size.y, visibleWidth * widthShare / paper.Size.x);
    }

    public void Extracted(ClientRecordPaper paper)
    {
        Ungroup();
        outOrder.Remove(paper);
        outOrder.Add(paper);
        paper.SetEventCamera(ViewCamera);
        paper.BringWindowToFront();
    }

    public void PaperSettled(ClientRecordPaper paper)
    {
        if (paper == null || paper.Record == null) return;
        shownOut.Add(paper.Record.id);
        Shown?.Invoke(paper.Record);
        ArrangeSmallScreen(paper);
    }

    public void PaperLanded(ClientRecordPaper paper)
    {
        outOrder.Remove(paper);
        group.Remove(paper);
        if (paper == null || paper.Record == null || !shownOut.Contains(paper.Record.id)) return;
        PutBack?.Invoke(paper.Record);
    }

    public bool BroughtToFront(ClientRecordPaper paper)
    {
        paper.BringWindowToFront();
        // The lift that ends a completed hold is not a tap.
        if (paper == groupedByHold) { groupedByHold = null; return false; }
        // Any other tap reads that record on its own: the stack comes apart.
        Ungroup();
        List<string> order = ClientRecordRules.BringToFront(Ids(outOrder), paper.Record.id);
        outOrder.Sort((a, b) => order.IndexOf(a.Record.id).CompareTo(order.IndexOf(b.Record.id)));
        Camera camera = ViewCamera;
        // The newest touched is nearest the camera; the others keep their places and sizes on screen.
        for (int i = 0; i < outOrder.Count; i++) outOrder[i].SetFrontDepth(camera, FrontDepth(camera, i));
        ArrangeSmallScreen(paper);
        return true;
    }

    public void PaperFlipped(ClientRecordPaper paper, bool backUp)
    {
        if (paper != null && paper.Record != null) Flipped?.Invoke(paper.Record, backUp);
    }

    /// <summary>On a small phone the front record is read large; the others wait small at the lower left, a tap away.</summary>
    private void ArrangeSmallScreen(ClientRecordPaper front)
    {
        Camera camera = ViewCamera;
        if (camera == null || !FocusOne(camera) || group.Count > 0 || front == null || !front.IsSettledOut) return;
        ReadingPose(front, out Vector3 position, out Quaternion rotation, out float scale);
        front.MoveTo(position, rotation, scale, ArrangeSeconds);
        int slot = 0;
        Rect pixels = camera.pixelRect;
        for (int i = 0; i < outOrder.Count; i++)
        {
            ClientRecordPaper other = outOrder[i];
            if (other == front || !other.IsSettledOut) continue;
            var screen = new Vector2(pixels.x + pixels.width * (0.08f + 0.1f * slot), pixels.y + pixels.height * 0.16f);
            PoseAt(camera, other, screen, FrontDepth(camera, i), 0.2f, 0.12f, out Vector3 p, out Quaternion r, out float s);
            other.MoveTo(p, r, s, ArrangeSeconds);
            slot++;
        }
    }

    /// <summary>Released at screen: on this folder's open page it goes back; on another client's folder it is gently refused.</summary>
    public bool WantsReturn(ClientRecordPaper paper, Vector2 screen, Action ready)
    {
        if (OverOwnPage(screen))
        {
            int pageIndex = PageIndex;
            if (pageIndex < 0) return false;
            if (pages.CurrentPageIndex == pageIndex) ready?.Invoke();
            else pages.TurnToPage(pageIndex, () => ready?.Invoke());
            return true;
        }
        if (OverForeignFolder(screen))
        {
            paper.Refuse();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_paper_pickup_01", 0.35f);
        }
        return false;
    }

    private bool OverOwnPage(Vector2 screen)
    {
        Camera camera = ViewCamera;
        if (camera == null || !FolderOpen || !folderRoot.gameObject.activeInHierarchy) return false;
        pages.PaperRect.GetWorldCorners(Corners);
        var p = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            Vector3 s = camera.WorldToScreenPoint(Corners[i]);
            if (s.z <= 0f) return false;
            p[i] = s;
        }
        return InTriangle(screen, p[0], p[1], p[2]) || InTriangle(screen, p[0], p[2], p[3]);
    }

    /// <summary>Another visible folder (a different CaseFolderUI's document) under the point: never a place for these records.</summary>
    private bool OverForeignFolder(Vector2 screen)
    {
        foreach (Doc3DLayer layer in Doc3DLayer.All)
        {
            if (layer == null || layer.Source == null || layer.Source == folderRoot || !layer.IsVisible) continue;
            if (layer.Source.GetComponentInChildren<CaseFolderUI>(true) == null) continue;
            if (layer.TryGetFootprint(out Rect footprint) && footprint.Contains(screen) &&
                !ClientRecordRules.MayAttach(ownerId, layer.Source.name)) return true;
        }
        return false;
    }

    private static bool InTriangle(Vector2 q, Vector2 a, Vector2 b, Vector2 c)
    {
        float Side(Vector2 p1, Vector2 p2, Vector2 p3) => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        float d1 = Side(q, a, b), d2 = Side(q, b, c), d3 = Side(q, c, a);
        bool negative = d1 < 0f || d2 < 0f || d3 < 0f, positive = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(negative && positive);
    }

    /// <summary>Closing, storing or an interruption: every record goes straight back into its holder.</summary>
    public void GatherAll()
    {
        IsGathering = true;
        try
        {
            CancelHold();
            groupDragging = false;
            group.Clear();
            foreach (ClientRecordPaper paper in papers)
                if (paper != null && !paper.IsAttached) paper.Gather();
            outOrder.Clear();
        }
        finally { IsGathering = false; }
    }

    private static List<string> Ids(List<ClientRecordPaper> list)
    {
        var ids = new List<string>();
        foreach (ClientRecordPaper paper in list) if (paper != null && paper.Record != null) ids.Add(paper.Record.id);
        return ids;
    }

    // =========================================================
    // TWO-SECOND HOLD AND GROUPING
    // =========================================================

    public void PointerDown(ClientRecordPaper paper, PointerEventData eventData)
    {
        if (paper != null) paper.BringWindowToFront();
        groupedByHold = null;
        if (paper == null || eventData == null || !paper.IsSettledOut || group.Contains(paper)) return;
        holdPaper = paper;
        holdPointer = eventData.pointerId;
        holdStart = eventData.position;
        holdElapsed = 0f;
        holdDown = true;
    }

    public void PointerUp(ClientRecordPaper paper, PointerEventData eventData)
    {
        if (paper == holdPaper && (eventData == null || eventData.pointerId == holdPointer)) holdDown = false;
    }

    /// <summary>The finger moved past the drag threshold (a drag began): no grouping.</summary>
    public void PointerMoved(ClientRecordPaper paper)
    {
        if (paper == holdPaper) CancelHold();
    }

    private void UpdateHold()
    {
        if (holdPaper == null) return;
        holdElapsed += Time.unscaledDeltaTime;
        float threshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10f;
        ClientRecordRules.HoldPhase phase = ClientRecordRules.Hold(holdElapsed, 0f, threshold, holdDown, focused && holdPaper.IsSettledOut);
        if (phase == ClientRecordRules.HoldPhase.Cancelled) { CancelHold(); return; }
        ShowRing(holdStart, ClientRecordRules.HoldProgress(holdElapsed));
        if (phase != ClientRecordRules.HoldPhase.Grouped) return;
        ClientRecordPaper lead = holdPaper;
        CancelHold();
        Group(lead);
    }

    private void CancelHold()
    {
        holdPaper = null;
        holdDown = false;
        holdElapsed = 0f;
        if (holdRing != null) holdRing.gameObject.SetActive(false);
    }

    /// <summary>This folder's taken-out records into one staggered stack at the held one, in their original order.</summary>
    private void Group(ClientRecordPaper lead)
    {
        var canonical = new List<string>();
        var owners = new Dictionary<string, string>();
        foreach (ClientRecordPaper paper in papers)
        {
            canonical.Add(paper.Record.id);
            owners[paper.Record.id] = ownerId;
        }
        var outIds = new HashSet<string>();
        foreach (ClientRecordPaper paper in outOrder) if (paper.IsSettledOut) outIds.Add(paper.Record.id);
        List<string> members = ClientRecordRules.GroupMembers(canonical, owners, outIds, lead.Record.id);
        if (members.Count == 0) return;
        groupedByHold = lead;

        Camera camera = ViewCamera;
        Vector3 anchorScreen = camera.WorldToScreenPoint(lead.transform.position);
        float[] stagger = ClientRecordRules.Stagger(members.Count, GroupStagger);
        group.Clear();
        outOrder.Clear();
        for (int i = 0; i < members.Count; i++)
        {
            ClientRecordPaper paper = papers.Find(p => p.Record.id == members[i]);
            group.Add(paper);
            outOrder.Add(paper);
            paper.BringWindowToFront();
            float depth = FrontDepth(camera, i);
            // Keep each record's apparent size as its depth changes in perspective.
            float currentDepth = Vector3.Dot(paper.transform.position - camera.transform.position, camera.transform.forward);
            float scale = ClientRecordRules.PreserveApparentScale(paper.transform.lossyScale.x, currentDepth, depth);
            float shift = stagger[i] * camera.pixelHeight;
            var screen = new Vector2(anchorScreen.x - shift, anchorScreen.y + shift);
            Vector3 position = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            paper.MoveTo(position, Quaternion.LookRotation(camera.transform.up, -camera.transform.forward) * ReadingTilt, scale, ArrangeSeconds);
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_paper_place_01", 0.5f);
        Grouped?.Invoke();
    }

    private void Ungroup() => group.Clear();

    public void BeginGroupDrag(ClientRecordPaper paper, PointerEventData eventData)
    {
        Camera camera = ViewCamera;
        if (camera == null || group.Count == 0) return;
        Rect box = default;
        bool any = false;
        groupOffsets.Clear();
        groupDepths.Clear();
        foreach (ClientRecordPaper member in group)
        {
            if (HeldOnScreen.TryScreenBox(member.transform, camera, out Rect b))
            {
                box = any ? Rect.MinMaxRect(Mathf.Min(box.xMin, b.xMin), Mathf.Min(box.yMin, b.yMin), Mathf.Max(box.xMax, b.xMax), Mathf.Max(box.yMax, b.yMax)) : b;
                any = true;
            }
            float depth = Vector3.Dot(member.transform.position - camera.transform.position, camera.transform.forward);
            groupDepths.Add(depth);
            groupOffsets.Add(member.transform.position - camera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, depth)));
            member.BeginGroupMove();
        }
        groupHeld = any ? HeldOnScreen.FromScreenBox(box, eventData.position) : default;
        groupDragging = true;
    }

    public void DragGroup(PointerEventData eventData)
    {
        Camera camera = ViewCamera;
        if (!groupDragging || camera == null) return;
        // The whole stack under the finger, exactly, stopping where any of it would leave the screen.
        Vector2 screen = groupHeld.Clamp(eventData.position, camera.pixelRect);
        for (int i = 0; i < group.Count && i < groupDepths.Count; i++)
            group[i].transform.position = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, groupDepths[i])) + groupOffsets[i];
    }

    public void EndGroupDrag(PointerEventData eventData)
    {
        if (!groupDragging) return;
        groupDragging = false;
        var members = new List<ClientRecordPaper>(group);
        foreach (ClientRecordPaper member in members) member.EndGroupMove();
        if (OverOwnPage(eventData.position))
        {
            var attached = new HashSet<string>();
            foreach (ClientRecordPaper paper in papers) if (paper.IsAttached) attached.Add(paper.Record.id);
            List<string> back = ClientRecordRules.ReturnOnce(Ids(members), attached);
            Action returnAll = () =>
            {
                foreach (string id in back) papers.Find(p => p.Record.id == id)?.PutBack();
            };
            group.Clear();
            int pageIndex = PageIndex;
            if (pageIndex < 0) return;
            if (pages.CurrentPageIndex == pageIndex) returnAll();
            else pages.TurnToPage(pageIndex, returnAll);
            return;
        }
        if (OverForeignFolder(eventData.position))
        {
            foreach (ClientRecordPaper member in members) member.Refuse();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_paper_pickup_01", 0.35f);
        }
    }

    /// <summary>A small ring above the finger that fills while the hold counts (hidden before HoldCueDelay).</summary>
    private void ShowRing(Vector2 screen, float progress)
    {
        if (progress <= 0f)
        {
            if (holdRing != null) holdRing.gameObject.SetActive(false);
            return;
        }
        if (holdRing == null) BuildRing();
        holdRing.gameObject.SetActive(true);
        holdFill.fillAmount = progress;
        var canvas = (RectTransform)holdRing.parent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen + new Vector2(0f, 110f), null, out Vector2 local);
        holdRing.anchoredPosition = local;
    }

    private void BuildRing()
    {
        var host = new GameObject("Record Hold Cue", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(host, gameObject.scene);
        host.layer = LayerMask.NameToLayer("UI");
        var canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = host.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        holdRing = HudTheme.CreateRect("Ring", host.transform);
        holdRing.anchorMin = holdRing.anchorMax = new Vector2(0.5f, 0.5f);
        holdRing.sizeDelta = new Vector2(84f, 84f);
        Image back = HudTheme.AddImage(holdRing, HudTheme.Circle, new Color(0.06f, 0.09f, 0.14f, 0.55f));
        back.raycastTarget = false;
        RectTransform fill = HudTheme.CreateRect("Fill", holdRing);
        HudTheme.Stretch(fill, 8f);
        holdFill = HudTheme.AddImage(fill, HudTheme.Circle, HudTheme.Brass);
        holdFill.type = Image.Type.Filled;
        holdFill.fillMethod = Image.FillMethod.Radial360;
        holdFill.fillOrigin = (int)Image.Origin360.Top;
        holdFill.raycastTarget = false;
    }
}

/// <summary>
/// Marks a folder's attached-records root (runtime only), so a copy of that
/// folder (Arthur's Reviewed File is built from the desk's Case Folder) can
/// leave another client's records behind.
/// </summary>
public sealed class AttachedRecordsRoot : MonoBehaviour
{
}
