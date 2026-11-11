using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Bea's one summary: the shared physical paper, document layers and HUD drag visual.
/// The chapter/save owner collects it; this adapter only moves and reads that item.</summary>
public sealed class Level3SummaryDocument : MonoBehaviour, IClientRecordHolder
{
    public static Level3SummaryDocument Instance { get; private set; }
    private Level3BeaTable table;
    private ClientRecordPaper paper;
    private Transform slot;
    private GameObject highlight;
    private bool stored;
    private bool available;
    private bool emitted;
    private bool wasSeated;
    private bool wasAtOfficeDesk;
    private bool behind;
    private Vector2? openPoint;
    private Vector2Int screenSize;
    private Action<Level3WorksheetGrants> collected;

    public bool IsOut => available && !stored && (folderSlot == null || paper != null && !paper.IsAttached);
    private Transform folderSlot;
    public ClientRecordPaper Paper => paper;
    private bool storing;
    private Level3WorksheetGrants? pendingCollection;
    public bool IsDragging => paper != null && paper.IsDragging;
    private bool AtOfficeDesk => (table == null || !table.Seated) && CameraController.Instance != null &&
        CameraController.Instance.CurrentMode == CameraController.CameraMode.Workstation;
    public bool IsOnOfficeDesk => AtOfficeDesk && IsOut;
    public Camera ViewCamera => HeldOnScreen.ActiveCamera(Camera.main);
    private bool Owned => Level3WorksheetCheck.Collected(Level3Checkpointer.Active?.State);
    private bool CanDock => table != null && table.Seated && table.SummaryAvailable;

    public void Bind(Level3BeaTable owner, Transform standAnchor, Transform workspace,
        ClientRecord record, Action<Level3WorksheetGrants> onCollected)
    {
        Instance = this;
        table = owner;
        collected = onCollected;
        slot = new GameObject("Summary Stand Slot").transform;
        // Never copy the activity card: after the last filing it is parked inside the machine.
        slot.SetParent(standAnchor, false);
        slot.localPosition = Vector3.zero;
        slot.localRotation = Quaternion.identity;
        // Fit the real paper's portrait proportions within the existing stand plate.
        Vector2 units = SupportingDocumentFace.SizeOf(record.type);
        float height = Level3PaperMachineModel.CardHeight;
        Vector2 size = new Vector2(Mathf.Min(Level3PaperMachineModel.CardWidth, height * units.x / units.y), height);
        paper = ClientRecordPaper.Create(this, slot, workspace, record, size, standAnchor.gameObject.layer);
        paper.BeforeTakeOut = DetachFolder;
        foreach (RecordTouchForwarder touch in paper.GetComponentsInChildren<RecordTouchForwarder>(true))
        {
            GraphicRaycaster prior = touch.GetComponent<GraphicRaycaster>();
            if (prior != null) { prior.enabled = false; Destroy(prior); }
            touch.gameObject.AddComponent<Level3TablePaperRaycaster>().IndependentDocument = true;
        }
        // Revised T07: at the desk its two totals are transfer sources for the Case Folder's Computation page.
        Level3AmountSource.AttachTo(paper, CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null);
        highlight = new GameObject("Summary Stand Drop Outline");
        highlight.transform.SetParent(slot.parent, false);
        highlight.transform.localPosition = slot.localPosition;
        highlight.transform.localRotation = slot.localRotation;
        Material brass = SupportingDocumentMesh.Matte(HudTheme.BrassSoft);
        for (int i = 0; i < 4; i++)
        {
            var edge = new GameObject("Drop Edge");
            edge.layer = slot.gameObject.layer;
            edge.transform.SetParent(highlight.transform, false);
            bool side = i < 2;
            edge.transform.localPosition = side ? new Vector3((i == 0 ? -1 : 1) * (size.x * 0.5f + 0.006f), 0.002f, 0f)
                : new Vector3(0f, 0.002f, (i == 2 ? -1 : 1) * (size.y * 0.5f + 0.006f));
            edge.AddComponent<MeshFilter>().sharedMesh = SupportingDocumentMesh.RoundedSlab(side ? 0.004f : size.x + 0.016f,
                side ? size.y + 0.016f : 0.004f, 0.001f, 0.001f);
            edge.AddComponent<MeshRenderer>().sharedMaterial = brass;
        }
        highlight.SetActive(false);
        stored = Owned;
        wasSeated = owner.Seated;
        ConfigureCanvas();
    }

    private void ConfigureCanvas()
    {
        if (paper == null || paper.WindowCanvas != null || HudHotbar.Instance == null) return;
        paper.WindowCanvas = HudHotbar.Instance.DocumentCanvas;
        if (paper.WindowCanvas == null) return;
        paper.DragLiftSeconds = 0.14f;
    }

    public void Sync(bool ready)
    {
        available = ready || Owned;
        if (paper == null) return;
        if (!available) { paper.Gather(); stored = true; }
        if (available && !emitted)
        {
            emitted = true;
            GameplayEvents.RaiseGuideCue("MachineSummaryOut");
        }
        ShowPhysical();
    }

    private void ShowPhysical()
    {
        if (slot == null || paper == null) return;
        slot.gameObject.SetActive(available && !stored && folderSlot == null);
        paper.gameObject.SetActive(available && (!stored || folderSlot != null));
    }

    private void Update()
    {
        if (paper == null) return;
        ConfigureCanvas();
        bool seated = table != null && table.Seated;
        if (wasSeated && !seated) CloseForTravel();
        wasSeated = seated;
        if (wasAtOfficeDesk && !AtOfficeDesk) CloseForTravel();
        wasAtOfficeDesk = AtOfficeDesk;
        paper.SetEventCamera(ViewCamera);
        if (CameraController.Instance != null && CameraController.Instance.IsTransitioning)
        {
            paper.CancelDrag();
            iconDragging = false;
            SetDropHint(false);
            return;
        }
        if (InGameSettings.IsOpen || HudRewardScreen.AnyOpen || DialogueUI.IsAnyDialogueActive)
        {
            paper.CancelDrag();
            SetDropHint(false);
            return;
        }
        SetDropHint(CanDock && (IsDragging || iconDragging));
        if (screenSize.x != Screen.width || screenSize.y != Screen.height)
        {
            screenSize = new Vector2Int(Screen.width, Screen.height);
            paper.FitInsideView(ViewCamera);
            if (IsOnOfficeDesk)
            {
                Level3AmountSource.CancelFor(paper);
                paper.CancelDrag();
            }
        }
        ShowPhysical();
    }

    public void CloseForTravel()
    {
        if (paper == null) return;
        paper.Gather();
        stored = Owned;
        SetDropHint(false);
        ShowPhysical();
    }

    private bool iconDragging;
    public void IconDrag(Vector2 screen)
    {
        iconDragging = screen.x > -1000f;
        SetDropHint(iconDragging && CanDock);
    }

    public void SetDropHint(bool on)
    {
        if (highlight != null) highlight.SetActive(on && CanDock);
    }

    public bool OverStand(Vector2 screen)
    {
        if (!CanDock || slot == null || ViewCamera == null) return false;
        // Use the full stand plate, including its empty edges, as the drop target.
        Matrix4x4 plane = Matrix4x4.TRS(slot.position, slot.rotation * Quaternion.Euler(90f, 0f, 0f), slot.lossyScale).inverse;
        return Level3TablePaperRaycaster.HitRect(ViewCamera.ScreenPointToRay(screen), plane,
            new Rect(-Level3PaperMachineModel.CardWidth * 0.6f, -Level3PaperMachineModel.CardHeight * 0.6f,
                Level3PaperMachineModel.CardWidth * 1.2f, Level3PaperMachineModel.CardHeight * 1.2f), out _);
    }

    public bool DropFromIcon(Vector2 screen)
    {
        SetDropHint(false);
        if (!Owned || !available) return false;
        if (OverStand(screen))
        {
            paper.Gather();
            stored = false;
            ShowPhysical();
            return true;
        }
        if (HudSlotDrop.IsOver(screen)) return false;
        bool opened = Open(screen);
        if (opened) Level3CaseFolder.Active?.TryAttach(paper, screen);
        return opened;
    }

    public bool Open(Vector2? screen = null, bool details = false)
    {
        if (!available || paper == null || ViewCamera == null || InGameSettings.IsOpen || HudRewardScreen.AnyOpen) return false;
        if (!paper.IsAttached)
        {
            paper.BringWindowToFront();
            if (screen.HasValue && paper.IsSettledOut)
            {
                openPoint = screen;
                ReadingPose(paper, out Vector3 position, out Quaternion rotation, out float scale);
                openPoint = null;
                paper.MoveTo(position, rotation, scale, 0.16f);
            }
            if (details && !paper.ShowingBack) paper.Flip();
            return true;
        }
        bool fromInventory = stored;
        stored = false;
        openPoint = screen;
        ShowPhysical();
        // Stored papers keep their real stand slot; the slot stays hidden while the same paper opens elsewhere.
        paper.gameObject.SetActive(true);
        if (screen.HasValue) paper.TakeOutFromScreen(screen.Value);
        else if (fromInventory && HudHotbar.Instance != null)
        {
            paper.TakeOutFromIcon(ItemIcon());
        }
        else paper.TakeOut();
        openPoint = null;
        showDetails = details;
        return true;
    }

    public void DockTo(Transform target, float height, bool animate)
    {
        if (paper == null || folderSlot == target || !animate && !paper.IsAttached) return;
        folderSlot = target;
        target.localScale = Vector3.one * (height / paper.Size.y);
        paper.SetRestSlot(target);
        stored = false;
        ShowPhysical();
        if (animate) paper.PutBack(); else paper.Gather();
    }

    private bool DetachFolder()
    {
        if (folderSlot == null) return true;
        if (!Level3CaseFolder.SetAttached(Level3WorksheetCheck.WorksheetWorkId, false)) return false;
        folderSlot = null;
        paper.SetRestSlot(slot);
        stored = false;
        return true;
    }

    public void ReleaseFolderSlot()
    {
        if (folderSlot == null || paper == null) return;
        folderSlot = null;
        paper.SetRestSlot(slot);
        paper.Gather();
        stored = true;
        ShowPhysical();
    }

    private RectTransform ItemIcon() => HudHotbar.Instance?.ItemTarget(InventoryCatalogue.BeaSummary);

    public bool Toggle()
    {
        if (folderSlot != null) return Open();
        if (IsOut && Owned) return Store(null);
        return Open();
    }

    private bool showDetails;
    private bool Store(Vector2? screen)
    {
        HudHotbar bar = HudHotbar.Instance;
        if (bar == null) return false;
        SlotRef target = screen.HasValue ? bar.DocumentDropTarget(screen.Value) : InventoryRules.Find(bar.Inventory, InventoryCatalogue.BeaSummary);
        if (target.IsNone) { bar.Notice("Make room in your Bag first."); return false; }
        if (!Owned)
        {
            var outcome = Level3Checkpointer.Active.CollectWorksheet(out Level3WorksheetGrants grants, out string hint, target);
            if (outcome != Level3Checkpointer.CollectCommit.Collected && outcome != Level3Checkpointer.CollectCommit.AlreadyCollected)
            { bar.Notice(hint); return false; }
            storing = true;
            if (outcome == Level3Checkpointer.CollectCommit.Collected) pendingCollection = grants;
            paper.PutBackToIcon(ItemIcon);
        }
        else
        {
            SlotRef from = InventoryRules.Find(bar.Inventory, InventoryCatalogue.BeaSummary);
            if (from != target)
            {
                InventoryResult move = bar.Commit(r => from.IsNone
                    ? Level3WorksheetCheck.AcquireSummary(r, bar.Catalogue, Level3Checkpointer.Active.State, target, CaseManager.Instance?.CurrentDefinition, HudHotbar.NewAction("summary-store"))
                    : InventoryRules.Move(r, bar.Catalogue, from, target, HudHotbar.NewAction("summary-store")));
                if (!move.Ok) { bar.Notice(move.Message); return false; }
                bar.NoteResult(move);
            }
            storing = true;
            paper.PutBackToIcon(ItemIcon);
        }
        return true;
    }

    public void ReadingPose(ClientRecordPaper p, out Vector3 position, out Quaternion rotation, out float worldScale)
    {
        Camera view = ViewCamera;
        float depth = Mathf.Max(view.nearClipPlane + 0.04f, 0.55f);
        Rect pixels = view.pixelRect;
        Vector2 point = openPoint ?? new Vector2(pixels.center.x, pixels.center.y);
        position = view.ScreenToWorldPoint(new Vector3(point.x, point.y, depth));
        rotation = Quaternion.LookRotation(view.transform.up, -view.transform.forward);
        float height = 2f * depth * Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * 0.5f);
        worldScale = Mathf.Min(height * 0.74f / p.Size.y, height * view.aspect * 0.65f / p.Size.x);

    }
    public void Extracted(ClientRecordPaper p) { HudWindowLock.Acquire(this); GameplayEvents.RaiseGuideCue("MachineSummaryRead"); }
    public void PaperSettled(ClientRecordPaper p)
    {
        p.FitInsideView(ViewCamera);
        if (showDetails && !p.ShowingBack) p.Flip();
        showDetails = false;
    }
    public void PaperLanded(ClientRecordPaper p)
    {
        if (storing)
        {
            storing = false; stored = true; ShowPhysical();
            HudHotbar.Instance?.PlayItemArrival(InventoryCatalogue.BeaSummary);
            if (pendingCollection.HasValue)
            {
                Level3WorksheetGrants grants = pendingCollection.Value;
                pendingCollection = null;
                collected?.Invoke(grants);
            }
        }
        HudWindowLock.Release(this);
        SetDropHint(false);
    }
    public bool WantsReturn(ClientRecordPaper p, Vector2 screen, Action ready)
    {
        SetDropHint(false);
        if (Level3CaseFolder.Active?.TryAttach(p, screen) == true) return true;
        if (OverStand(screen)) { stored = false; ready?.Invoke(); return true; }
        return HudSlotDrop.IsOver(screen) && Store(screen);
    }
    public bool BroughtToFront(ClientRecordPaper p) { bool focused = p.RaisedThisFrame; p.BringWindowToFront(); return !behind && !focused; }
    public void PointerDown(ClientRecordPaper p, PointerEventData e)
    {
        behind = false;
        foreach (Doc3DLayer layer in Doc3DLayer.All)
            if (layer != null && layer.IsVisible && layer.SortingOrder > p.WindowOrder) behind = true;
        foreach (FloatingWindow window in FloatingWindow.Instances)
            if (window != null && window.IsOpen() && window.SortingOrder > p.WindowOrder) behind = true;
    }
    public void PointerMoved(ClientRecordPaper p) { behind = false; }
    public void PointerUp(ClientRecordPaper p, PointerEventData e) { }
    public void PaperFlipped(ClientRecordPaper p, bool back) { }
    public bool IsGrouped(ClientRecordPaper p) => false;
    public void BeginGroupDrag(ClientRecordPaper p, PointerEventData e) { }
    public void DragGroup(PointerEventData e) { }
    public void EndGroupDrag(PointerEventData e) { }
    private void OnApplicationFocus(bool focused) { if (!focused) { paper?.CancelDrag(); iconDragging = false; SetDropHint(false); } }
    private void OnApplicationPause(bool paused) { if (paused) OnApplicationFocus(false); }
    private void OnDisable() { iconDragging = false; CloseForTravel(); }
    private void OnDestroy()
    {
        HudWindowLock.Release(this);
        if (Instance == this) Instance = null;
        if (paper != null) Destroy(paper.gameObject);
        if (slot != null) Destroy(slot.gameObject);
        if (highlight != null) Destroy(highlight);
    }
}
