using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Bea's withholding certificate (B-WHT, Level 3 T08) as ONE physical paper
/// the player holds: the standard record paper (ClientRecordPaper, Type 8
/// face from Level3RecordRules: real thickness, edge, clip and print), owned
/// by this shared holder, ensured by Bea or the desk when first needed.
///
/// - Mid-conversation offer: the actual ClientCopyPickup / WorldPickupMotion
///   presents a Type 8 paper beside Bea, using the Auditor's drop/hover/flight.
///   Clicking after dialogue commits chapter + Bag possession before flight.
/// - The one readable paper opens from Bag/hotbar at the icon/drop point;
///   shared amount bands supply exact values at the desk, and slot drops put
///   it away. Ordinary reads turn/focus/drag/clamp through ClientRecordPaper.
/// Acquisition delegates to the existing checkpointer; reading never grades or links a credit.
///
/// Milestone T01 (full base bar and Bag): tapping the offered paper with no
/// free slot leaves it beside Bea and shows one "Clip to Bea's folder" tag on
/// it (Level3CertificateClipTag). Tapping the tag saves the handover and the
/// folder attachment together (Level3Checkpointer.CommitCertificateToFolder);
/// only then does the paper fly to the folder and dock in its existing
/// Supporting Documents slot. While no slot is free, taking it out of the
/// folder reads it where it is clipped (its amounts stay usable at the desk)
/// and it settles back into the folder; it is never unclipped into nowhere.
/// </summary>
public sealed class Level3CertificatePaper : MonoBehaviour, IClientRecordHolder
{
    public const string CueRead = "CertificateRead";
    /// <summary>The paper's printed width in metres (a Type 8 sheet, 700 x 980 face units).</summary>
    public const float PaperWidth = 0.18f;
    private const float ReadDepth = 0.5f;
    private const float HandDrop = 0.75f;

    /// <summary>Where it is being read: in a conversation (clear of the dialogue box) or at the desk.</summary>
    public enum Context { None, Conversation, Desk }

    public static Level3CertificatePaper Instance { get; private set; }

    /// <summary>It came back down (after a lift): the conversation or the desk goes on.</summary>
    public event Action Lowered;

    public Context Showing { get; private set; }

    /// <summary>Up, lifting, held or on its way down (not resting in hand).</summary>
    public bool IsUp => paper != null && !paper.IsAttached;

    private Transform folderSlot;
    public ClientRecordPaper Paper => EnsureBuilt() ? paper : null;
    private Transform hand;
    private Transform workspace;
    private ClientRecordPaper paper;

    private ClientCopyPickup pickup;
    private Vector2? openPoint;
    private Transform presentedBy;
    private bool focusOnlyPress;
    public static Level3CertificatePaper Ensure(Transform owner)
    {
        if (Instance != null) return Instance;
        return owner.GetComponent<Level3CertificatePaper>() ?? owner.gameObject.AddComponent<Level3CertificatePaper>();
    }
    public bool Open(Vector2? screen = null)
    {
        if (!Level3CertificateRules.Acquired(Chapter) || InGameSettings.IsOpen || DialogueUI.IsAnyDialogueActive || !EnsureBuilt()) return false;
        if (!paper.IsAttached) { paper.BringWindowToFront(); return true; }
        openPoint = screen;
        Showing = Context.Desk;
        hand.gameObject.SetActive(true);
        PlaceHand(ViewCamera);
        // Milestone T01: clipped with no slot to unclip into, it lifts out of the folder to be read there.
        if (ReadsClipped) paper.TakeOut();
        else if (screen.HasValue) paper.TakeOutFromScreen(screen.Value);
        else
        {
            paper.TakeOutFromIcon(ItemIcon());
        }
        openPoint = null;
        if (screen.HasValue) Level3CaseFolder.Active?.TryAttach(paper, screen.Value);
        return true;
    }
    public void DockTo(Transform target, float height, bool animate)
    {
        if (!EnsureBuilt() || folderSlot == target || !animate && !paper.IsAttached) return;
        folderSlot = target;
        target.localScale = Vector3.one * (height / paper.Size.y);
        paper.SetRestSlot(target);
        paper.gameObject.SetActive(true);
        if (animate) paper.PutBack(); else paper.Gather();
    }

    /// <summary>Milestone T01: clipped in Bea's folder with no ordinary slot it could be unclipped into.</summary>
    private bool ReadsClipped => folderSlot != null && !Level3CertificateRules.HasRoom(HudHotbar.Instance != null ? HudHotbar.Instance.Inventory : null);

    private bool DetachFolder()
    {
        if (folderSlot == null) return true;
        // Milestone T01: no slot to unclip into: read it where it is clipped (it stays attached and returns to its folder slot).
        if (ReadsClipped)
        {
            Showing = Context.Desk;
            return true;
        }
        if (!Level3CaseFolder.SetAttached(Level3ChapterRules.BeaCreditSource, false)) return false;
        folderSlot = null;
        hand.gameObject.SetActive(true);
        paper.SetRestSlot(hand);
        Showing = Context.Desk;
        return true;
    }

    public void ReleaseFolderSlot()
    {
        if (folderSlot == null || paper == null) return;
        folderSlot = null;
        paper.SetRestSlot(hand);
        paper.Gather();
        hand.gameObject.SetActive(false);
    }

    private RectTransform ItemIcon() => HudHotbar.Instance?.ItemTarget(InventoryCatalogue.BeaCertificate);

    public bool Toggle()
    {
        if (IsUp) { Lower(); return true; }
        return Open();
    }

    public void PresentFrom(Transform speaker)
    {
        if (speaker == null || pickup != null || Level3CertificateRules.Acquired(Chapter)) return;
        var owner = Level3Checkpointer.Active;
        if (owner == null) return;
        var outcome = owner.CommitComputation((Level3ChapterCheckpoint c, out string why) => Level3CertificateRules.Offer(c, out why), out string hint);
        if (outcome != Level3Checkpointer.TapeCommit.Attached && outcome != Level3Checkpointer.TapeCommit.AlreadyAttached) { Level3BeaTable.Level3Say(hint); return; }
        presentedBy = speaker;
        BuildPickup(true);
    }
    private void BuildPickup(bool animated)
    {
        if (presentedBy == null && Level3BeaOfficeVisit.Instance?.InRoom != true) return;
        Transform speaker = presentedBy != null ? presentedBy : Level3BeaOfficeVisit.Instance?.transform;
        if (speaker == null || pickup != null || Level3CertificateRules.Acquired(Chapter)) return;
        var definition = CaseManager.Instance?.CurrentDefinition;
        var records = Level3RecordRules.Build(definition, new[] { Level3ChapterRules.BeaCreditSource });
        if (records.Count == 0) return;
        Vector3 floor = speaker.position;
        float height = 1.7f;
        if (DialogueSpeakerFrames.TryResolve(speaker.GetComponent<Level3BeaInteractable>(), out DialogueSpeakerFrame frame)) { floor = frame.Floor; height = frame.Height; }
        Vector3 toward = speaker.forward;
        Transform player = CameraController.Instance?.PlayerRoot;
        if (animated && player != null) toward = player.position - floor;
        toward.y = 0f;
        if (toward.sqrMagnitude < .001f) toward = Vector3.forward;
        toward.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, toward).normalized;
        Vector3 rest = floor + Vector3.up * (height * .62f) + toward * .55f + side * .5f;
        float tip = 15f * Mathf.Deg2Rad;
        Quaternion facing = Quaternion.LookRotation(Vector3.up * Mathf.Cos(tip) - toward * Mathf.Sin(tip), toward * Mathf.Cos(tip) + Vector3.up * Mathf.Sin(tip));
        Vector3? from = animated ? floor + Vector3.up * (height * .55f) + toward * .2f : (Vector3?)null;
        pickup = ClientCopyPickup.Create(gameObject.scene, null, rest, facing, from, HudHotbar.Instance?.ItemTarget(InventoryCatalogue.BeaCertificate), () => { pickup = null; },
            root => { IdCardModel.Build(root, records[0], new Vector2(.2f, .28f), root.gameObject.layer, out _, out _); }, CollectPickup);
        AudioManager.Instance?.PlayPaperSFX();
    }
    private RectTransform CollectPickup()
    {
        HudHotbar bar = HudHotbar.Instance;
        if (bar == null || Level3Checkpointer.Active == null) return null;
        // Milestone T01: the clip tag chose the folder (one deliberate tap); the paper flies only after that save.
        if (clipChosen)
        {
            clipChosen = false;
            bool carried = Level3CaseFolder.Active != null && Level3CaseFolder.Active.Carried;
            var clipped = Level3Checkpointer.Active.CommitCertificateToFolder(carried, out string why);
            if (clipped != Level3Checkpointer.TapeCommit.Attached && clipped != Level3Checkpointer.TapeCommit.AlreadyAttached) { Level3BeaTable.Level3Say(why); return null; }
            RemoveClipTag();
            if (clipped == Level3Checkpointer.TapeCommit.Attached) Level3BeaTable.Level3Say(Level3CertificateRules.ClippedLine);
            return bar.ItemTarget(InventoryCatalogue.CaseFolder);
        }
        // A full bar and Bag: nothing is written; the paper stays beside Bea and offers the folder instead.
        if (!Level3CertificateRules.Acquired(Chapter) && !Level3CertificateRules.HasRoom(bar.Inventory))
        {
            ShowClipTag();
            Level3BeaTable.Level3Say(Level3CertificateRules.OfferClip);
            return null;
        }
        var outcome = Level3Checkpointer.Active.CommitCertificate(out string hint);
        if (outcome != Level3Checkpointer.TapeCommit.Attached && outcome != Level3Checkpointer.TapeCommit.AlreadyAttached) { Level3BeaTable.Level3Say(hint); return null; }
        RemoveClipTag();
        return Level3CertificateRules.Clipped(Chapter) ? bar.ItemTarget(InventoryCatalogue.CaseFolder) : bar.ItemTarget(InventoryCatalogue.BeaCertificate);
    }

    // =========================================================
    // MILESTONE T01: CLIP TO BEA'S FOLDER (on the offered paper)
    // =========================================================

    private Level3CertificateClipTag clipTag;
    private bool clipChosen;

    private void ShowClipTag()
    {
        if (pickup == null || clipTag != null) return;
        clipTag = Level3CertificateClipTag.Create(pickup.transform, gameObject.scene, ClipChosen);
    }

    private void RemoveClipTag()
    {
        if (clipTag != null) Destroy(clipTag.gameObject);
        clipTag = null;
    }

    /// <summary>The tag was tapped within reach: the same pickup collects into the folder (stale taps do nothing).</summary>
    private void ClipChosen()
    {
        if (pickup == null || Level3CertificateRules.Acquired(Chapter)) { RemoveClipTag(); return; }
        clipChosen = true;
        // The pickup's own collect-then-fly path (its tap entry without dialogue): CollectPickup saves first.
        pickup.TryPickUpFromAuditor();
        // Consumed by CollectPickup; a refused or not-yet-settled tap leaves the paper and its tag to try again.
        clipChosen = false;
    }
    private void Update()
    {
        if (Level3CertificateRules.Offered(Chapter) && !Level3CertificateRules.Acquired(Chapter) && pickup == null && !DialogueUI.IsAnyDialogueActive) BuildPickup(false);
        // Milestone T01: the tag lives only on the offered paper while the Bag is full (room made: ordinary pickup again).
        if (clipTag != null && (pickup == null || Level3CertificateRules.Acquired(Chapter) || Level3CertificateRules.HasRoom(HudHotbar.Instance?.Inventory))) RemoveClipTag();
        // Quiet inventory repair for historical saves that already acquired B-WHT before it was a Bag item.
        HudHotbar bar = HudHotbar.Instance;
        if (Level3CertificateRules.Acquired(Chapter) && !Level3CaseFolder.Attached(Level3ChapterRules.BeaCreditSource) && bar != null && !InventoryRules.Owns(bar.Inventory, InventoryCatalogue.BeaCertificate) && Time.unscaledTime >= retryItemAt)
        {
            retryItemAt = Time.unscaledTime + 2f;
            bar.Commit(inv => Level3CertificateRules.AcquireItem(inv, bar.Catalogue, Chapter, CaseManager.Instance?.CurrentDefinition, HudHotbar.NewAction("certificate-recover")));
        }
    }
    private float retryItemAt;

    private void OnEnable() => Instance = this;

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
        if (paper != null && !paper.IsAttached) paper.Gather();
        if (hand != null) hand.gameObject.SetActive(false);
        Showing = Context.None;
        HudWindowLock.Release(this);
    }

    private void OnDestroy()
    {
        RemoveClipTag();
        if (pickup != null) Destroy(pickup.gameObject);
        if (workspace != null) Destroy(workspace.gameObject);
        if (hand != null) Destroy(hand.gameObject);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && paper != null) paper.CancelDrag();
    }

    private static Level3ChapterCheckpoint Chapter => Level3Checkpointer.Active != null ? Level3Checkpointer.Active.State : null;

    /// <summary>The certificate is Bea's acquired source and its paper can be shown.</summary>
    public bool Available => Level3CertificateRules.Acquired(Chapter) && EnsureBuilt();

    private bool EnsureBuilt()
    {
        if (paper != null)
        {
            if (paper.WindowCanvas == null) paper.WindowCanvas = HudHotbar.Instance?.DocumentCanvas;
            return true;
        }
        Level3ChapterCheckpoint chapter = Chapter;
        CaseDefinition definition = CaseManager.Instance != null ? CaseManager.Instance.CurrentDefinition : null;
        if (!Level3CertificateRules.Acquired(chapter) || definition == null || definition.caseId != Level3ChapterRules.BeaCaseId) return false;
        List<ClientRecord> records = Level3RecordRules.Build(definition, new[] { Level3ChapterRules.BeaCreditSource });
        if (records.Count == 0)
        {
            Debug.LogWarning("[Level3] Bea's withholding certificate (B-WHT) could not be printed from her case content.");
            return false;
        }
        ClientRecord record = records[0];
        hand = new GameObject("Certificate In Hand").transform;
        hand.gameObject.layer = gameObject.layer;
        SceneManager.MoveGameObjectToScene(hand.gameObject, gameObject.scene);
        workspace = new GameObject("Certificate Out").transform;
        SceneManager.MoveGameObjectToScene(workspace.gameObject, gameObject.scene);
        Vector2 units = SupportingDocumentFace.SizeOf(record.type);
        var size = new Vector2(PaperWidth, PaperWidth * units.y / units.x);
        paper = ClientRecordPaper.Create(this, hand, workspace, record, size, gameObject.layer);
        paper.BeforeTakeOut = DetachFolder;
        paper.WindowCanvas = HudHotbar.Instance?.DocumentCanvas;
        foreach (RecordTouchForwarder touch in paper.GetComponentsInChildren<RecordTouchForwarder>(true))
        {
            UnityEngine.UI.GraphicRaycaster prior = touch.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (prior != null) { prior.enabled = false; Destroy(prior); }
            touch.gameObject.AddComponent<Level3TablePaperRaycaster>().IndependentDocument = true;
        }
        Level3AmountSource.AttachTo(paper, definition);
        hand.gameObject.SetActive(false);
        Debug.Log($"[Level3] Bea's certificate paper is ready ({record.id}, Type 8).");
        return true;
    }

    // =========================================================
    // RAISE / LOWER
    // =========================================================

    /// <summary>Lifts it into its readable pose; from = where it starts (Bea's hands at the handover), else from the hand below the view.</summary>
    public bool Raise(Context context, Vector3? from = null)
    {
        if (!Available) return false;
        Showing = context;
        if (!paper.IsAttached) return true;
        Camera camera = ViewCamera;
        if (camera == null) return false;
        hand.gameObject.SetActive(true);
        PlaceHand(camera);
        if (from.HasValue)
        {
            // Held out toward the player at its real size, then lifted to read.
            hand.position = from.Value;
            hand.localScale = Vector3.one;
        }
        paper.TakeOut();
        // The holder's own clip and shadow go back below the view at once; the paper flies on its own.
        PlaceHand(camera);
        return true;
    }

    /// <summary>Back down into the hand (animated); Lowered follows when it has landed.</summary>
    public void Lower()
    {
        if (paper == null || paper.IsAttached) return;
        // Milestone T01: read while still clipped: it settles back into its folder slot, not toward a Bag icon.
        if (folderSlot != null) { paper.CancelDrag(); paper.PutBack(); }
        else if (Showing == Context.Desk) paper.PutBackToIcon(ItemIcon);
        else { paper.CancelDrag(); paper.PutBack(); }
    }

    /// <summary>Straight down without motion (an interruption).</summary>
    public void Gather()
    {
        if (paper != null && !paper.IsAttached) paper.Gather();
        if (hand != null) hand.gameObject.SetActive(false);
        Showing = Context.None;
        HudWindowLock.Release(this);
    }

    private void LateUpdate()
    {
        if (paper == null || hand == null || !hand.gameObject.activeSelf) return;
        Camera camera = ViewCamera;
        if (camera != null) PlaceHand(camera);
        if (Showing == Context.Desk && IsUp && (InGameSettings.IsOpen || DialogueUI.IsAnyDialogueActive)) { paper.CancelDrag(); }
        // A conversation that ended without lowering it (cancelled, interrupted) never leaves it up.
        if (Showing == Context.Conversation && IsUp && !DialogueUI.IsAnyDialogueActive) Lower();
    }

    private static bool AtDesk => CameraController.Instance != null &&
        CameraController.Instance.CurrentMode == CameraController.CameraMode.Workstation;

    /// <summary>Just below the bottom of the view, facing the camera like the reading pose, at the reading pose's scale (it slides down out of view).</summary>
    private void PlaceHand(Camera camera)
    {
        Quaternion facing = Quaternion.LookRotation(camera.transform.up, -camera.transform.forward);
        float visibleH = 2f * ReadDepth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        hand.SetPositionAndRotation(camera.transform.position + camera.transform.forward * ReadDepth - camera.transform.up * (visibleH * HandDrop + paper.Size.y),
            facing);
        hand.localScale = Vector3.one * ReadingScale(camera);
    }

    private float ReadingScale(Camera camera)
    {
        bool talk = Showing == Context.Conversation;
        float visibleH = 2f * ReadDepth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float visibleW = visibleH * Mathf.Max(0.1f, camera.aspect);
        return Mathf.Min(visibleH * (talk ? 0.56f : 0.8f) / paper.Size.y, visibleW * (talk ? 0.62f : 0.72f) / paper.Size.x);
    }

    // =========================================================
    // IClientRecordHolder
    // =========================================================

    public Camera ViewCamera => HeldOnScreen.ActiveCamera(Camera.main);

    public void ReadingPose(ClientRecordPaper record, out Vector3 position, out Quaternion rotation, out float worldScale)
    {
        Camera camera = ViewCamera;
        Rect pixels = camera.pixelRect;
        // In a conversation it reads above the dialogue box; at the desk, centred in front of the tools.
        float y = Showing == Context.Conversation ? 0.62f : 0.5f;
        float depth = Mathf.Max(camera.nearClipPlane + 0.12f, ReadDepth);
        Vector2 point = openPoint ?? new Vector2(pixels.x + pixels.width * .5f, pixels.y + pixels.height * y);
        position = camera.ScreenToWorldPoint(new Vector3(point.x, point.y, depth));
        rotation = Quaternion.LookRotation(camera.transform.up, -camera.transform.forward);
        worldScale = ReadingScale(camera);
    }

    public void Extracted(ClientRecordPaper record)
    {
        HudWindowLock.Acquire(this);
        record.SetEventCamera(ViewCamera);
        record.BringWindowToFront();
    }

    public void PaperSettled(ClientRecordPaper record) { record.FitInsideView(ViewCamera); GameplayEvents.RaiseGuideCue(CueRead); }

    public void PaperLanded(ClientRecordPaper record)
    {
        if (hand != null) hand.gameObject.SetActive(false);
        Showing = Context.None;
        HudWindowLock.Release(this);
        Lowered?.Invoke();
    }

    /// <summary>A deliberate inventory drop flies back to the item's icon; every other drop stays open.</summary>
    public bool WantsReturn(ClientRecordPaper record, Vector2 screen, Action ready)
    {
        // Milestone T01: read while still clipped. The folder takes it back; a bar/Bag drop unclips it only when a slot is free.
        if (folderSlot != null)
        {
            if (Level3CaseFolder.Active?.TryAttach(record, screen) == true) { record.PutBack(); return true; }
            if (!HudSlotDrop.IsOver(screen)) return false;
            if (ReadsClipped)
            {
                HudHotbar.Instance?.Notice(Level3ReviewRules.RefuseUnclipFull);
                record.PutBack();
                return true;
            }
            if (!DetachFolder()) { record.PutBack(); return true; }
        }
        if (Level3CaseFolder.Active?.TryAttach(record, screen) == true) return true;
        if (!HudSlotDrop.IsOver(screen)) return false;
        HudHotbar bar = HudHotbar.Instance;
        SlotRef target = bar != null ? bar.DocumentDropTarget(screen, InventoryCatalogue.BeaCertificate) : SlotRef.None;
        if (target.IsNone) return false;
        SlotRef from = InventoryRules.Find(bar.Inventory, InventoryCatalogue.BeaCertificate);
        if (from != target)
        {
            InventoryResult result = bar.Commit(inv => from.IsNone
                ? Level3CertificateRules.AcquireItem(inv, bar.Catalogue, Chapter, CaseManager.Instance?.CurrentDefinition, HudHotbar.NewAction("certificate-store"), target)
                : InventoryRules.Move(inv, bar.Catalogue, from, target, HudHotbar.NewAction("certificate-store")));
            if (!result.Ok) { bar.Notice(result.Message); return false; }
            bar.NoteResult(result);
        }
        Lower();
        return true;
    }

    /// <summary>A covered paper's first tap focuses it; a later tap can turn its face.</summary>
    public bool BroughtToFront(ClientRecordPaper record)
    {
        bool focusedByPress = focusOnlyPress;
        focusOnlyPress = false;
        int previousOrder = record.WindowOrder;
        record.BringWindowToFront();
        return !focusedByPress && previousOrder == record.WindowOrder;
    }

    public void PaperFlipped(ClientRecordPaper record, bool backUp) { }
    public void PointerDown(ClientRecordPaper record, PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
        int previousOrder = record.WindowOrder;
        // The amount bands forward pointer-down here too: focus before selection or dragging.
        record.BringWindowToFront();
        focusOnlyPress = previousOrder != record.WindowOrder;
    }
    public void PointerUp(ClientRecordPaper record, PointerEventData eventData) { }
    public void PointerMoved(ClientRecordPaper record) { }
    public bool IsGrouped(ClientRecordPaper record) => false;
    public void BeginGroupDrag(ClientRecordPaper record, PointerEventData eventData) { }
    public void DragGroup(PointerEventData eventData) { }
    public void EndGroupDrag(PointerEventData eventData) { }
}

/// <summary>
/// PURPOSE:
/// Milestone T01: the one "Clip to Bea's folder" tag on Bea's offered
/// certificate when the bar and Bag are full. A small plate in the HUD's
/// palette and font just below the floating paper, facing the view and
/// following its hover. A deliberate tap within reach (the paper's own
/// guards: no drag, joystick, UI or dialogue) asks its owner to clip.
/// Presentation and input only: Level3Checkpointer decides and saves.
/// </summary>
public sealed class Level3CertificateClipTag : MonoBehaviour, IPointerClickHandler
{
    public const string Label = "CLIP TO BEA'S FOLDER";
    private const float Width = 0.42f;
    private const float Height = 0.1f;
    private const float Below = 0.21f;
    private const float UnitsPerMetre = 1000f;

    private Transform paper;
    private Action chosen;

    public static Level3CertificateClipTag Create(Transform paper, Scene scene, Action chosen)
    {
        if (paper == null) return null;
        var root = new GameObject("Clip To Bea's Folder");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.layer = paper.gameObject.layer;
        var tag = root.AddComponent<Level3CertificateClipTag>();
        tag.paper = paper;
        tag.chosen = chosen;
        var box = root.AddComponent<BoxCollider>();
        box.size = new Vector3(Width, Height, 0.02f);

        var host = new GameObject("Plate", typeof(RectTransform));
        host.layer = root.layer;
        var rect = (RectTransform)host.transform;
        rect.SetParent(root.transform, false);
        rect.localScale = Vector3.one / UnitsPerMetre;
        rect.sizeDelta = new Vector2(Width, Height) * UnitsPerMetre;
        host.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        UnityEngine.UI.Image face = HudTheme.AddImage(rect, HudTheme.Rounded, HudTheme.PanelFill, 18f);
        face.raycastTarget = false;
        var rim = host.AddComponent<UnityEngine.UI.Outline>();
        rim.effectColor = HudTheme.BrassSoft;
        rim.effectDistance = new Vector2(3f, -3f);
        TMPro.TextMeshProUGUI text = HudTheme.CreateLabel("Label", rect, Label, 32f, HudTheme.TextPrimary, TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
        text.raycastTarget = false;
        HudTheme.Stretch(text.rectTransform, 10f);
        tag.Follow();
        return tag;
    }

    private void LateUpdate()
    {
        if (paper == null) { Destroy(gameObject); return; }
        Follow();
    }

    /// <summary>Just below the floating paper, its face toward the view.</summary>
    private void Follow()
    {
        transform.position = paper.position + Vector3.down * Below;
        Camera view = HeldOnScreen.ActiveCamera(Camera.main);
        if (view == null) return;
        Vector3 away = transform.position - view.transform.position;
        if (away.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(away, Vector3.up);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && (eventData.button != PointerEventData.InputButton.Left || MobileInputGuard.IsDragOrCancelled(eventData) ||
            VirtualJoystick.BlocksWorldInput || MobileInputGuard.IsPointerOverUI(eventData.position))) return;
        if (DialogueUI.IsAnyDialogueActive || InGameSettings.IsOpen) return;
        if (eventData != null && !InteractionRange.PlayerCanReach(this)) return;
        chosen?.Invoke();
    }
}
