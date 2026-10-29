using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Who owns a set of attached records (T31; first used by Arthur's Reviewed
/// File, later by Marie's folder in T25): the camera, where a taken-out record
/// settles, whether a release over the page puts it back, and the progress
/// it counts. The holder owns input holds and any saving.
/// </summary>
public interface IClientRecordHolder
{
    Camera ViewCamera { get; }
    /// <summary>Where this record settles when it is taken out (readable, facing the camera), and its world scale there.</summary>
    void ReadingPose(ClientRecordPaper paper, out Vector3 position, out Quaternion rotation, out float worldScale);
    /// <summary>It left its holder (tap or drag).</summary>
    void Extracted(ClientRecordPaper paper);
    /// <summary>It came to rest out of the folder (it has been shown).</summary>
    void PaperSettled(ClientRecordPaper paper);
    /// <summary>It is back in its holder.</summary>
    void PaperLanded(ClientRecordPaper paper);
    /// <summary>Released at screen: true when it goes back to its holder; ready runs once its page shows.</summary>
    bool WantsReturn(ClientRecordPaper paper, Vector2 screen, Action ready);
    /// <summary>It was touched while out: it comes in front of the others. True for an ordinary tap (not the lift ending a hold).</summary>
    bool BroughtToFront(ClientRecordPaper paper);
    /// <summary>An ID card was turned over (true = its back, the profile, is up).</summary>
    void PaperFlipped(ClientRecordPaper paper, bool backUp);
    /// <summary>A finger went down / up on it (the optional two-second hold that groups records).</summary>
    void PointerDown(ClientRecordPaper paper, PointerEventData eventData);
    void PointerUp(ClientRecordPaper paper, PointerEventData eventData);
    /// <summary>The finger moved past the drag threshold on it (a drag began): no grouping.</summary>
    void PointerMoved(ClientRecordPaper paper);
    /// <summary>It is part of a grouped stack: drags move the whole stack (the holder moves it).</summary>
    bool IsGrouped(ClientRecordPaper paper);
    void BeginGroupDrag(ClientRecordPaper paper, PointerEventData eventData);
    void DragGroup(PointerEventData eventData);
    void EndGroupDrag(PointerEventData eventData);
}

/// <summary>
/// PURPOSE:
/// One supporting record attached to a folder page as a physical 3D object
/// (Campaign Foundation T31, Documentation/SupportingDocuments.md): an ID card
/// (bevelled, rounded, matte slab in four photo-corner holders) or a sheet
/// (background or 2316: thin sheet with a visible edge and lightly lifted free
/// corners, held by a small modelled clip), each with a soft contact shadow on the page and
/// its printed face (SupportingDocumentFace) on top.
///
/// HANDLING: a tap takes it out (a short lift and turn into a readable pose
/// beside the folder, then it holds still); dragging takes it out directly and
/// it follows the finger exactly, kept on screen (HeldOnScreen). Touching one
/// that is out brings it in front. Released over its folder's page it goes
/// back into its holder with a short settle and the clip closing; anywhere
/// else it stays out; over another client's folder it gives a small shake
/// (Refuse). Gather() puts it straight back (closing, interruptions). A finger
/// held still on it for two seconds lets its holder group the records that
/// are out; a grouped record's drags move the holder's whole stack.
/// The holder decides where it settles and counts progress; nothing is saved here.
///
/// INPUT: its face canvas takes the touches (world canvas + GraphicRaycaster),
/// the same route as the folder's own page controls.
/// </summary>
public sealed class ClientRecordPaper : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
    IInitializePotentialDragHandler, IPointerDownHandler, IPointerUpHandler
{
    private enum State { Attached, Lifting, Out, Dragging, Returning, Moving }
    private const float RefuseSeconds = 0.3f;
    private const float RefuseDegrees = 5f;

    public const float LiftSeconds = 0.38f;
    public const float ReturnSeconds = 0.3f;
    private const float ClipOpenDegrees = 16f;
    private const float ClipSeconds = 0.14f;

    private IClientRecordHolder holder;
    private Transform slot;
    private Transform workspace;
    private Transform clip;
    private Quaternion clipRest;
    private float clipOpen, clipTarget;
    private Canvas faceCanvas;
    private CardFlip flip;
    private GameObject dropShadow;
    private State state = State.Attached;
    private float elapsed;
    private Vector3 fromPosition, toPosition;
    private Quaternion fromRotation, toRotation;
    private float fromScale, toScale;
    private Vector3 grabOffset;
    private float dragDepth;
    private HeldOnScreen held;
    private bool extractedByDrag;
    private bool groupDrag;
    private float refuseElapsed = -1f;
    private Quaternion refuseBase;
    private Doc3DLayer windowLayer;
    private Transform contactShadow;
    private Canvas touchCanvas;
    private int attachedLayer;
    private int captureLayer = -1;

    public ClientRecord Record { get; private set; }
    /// <summary>Its size in the page's metres (width x height of the face).</summary>
    public Vector2 Size { get; private set; }
    public bool IsAttached => state == State.Attached;
    public bool IsOut => state == State.Out || state == State.Dragging || state == State.Lifting;
    /// <summary>It is out of the folder and at rest (shown).</summary>
    public bool IsSettledOut => state == State.Out;
    /// <summary>An ID card showing its back (the taxpayer profile).</summary>
    public bool ShowingBack => flip != null && flip.ShowingBack;
    public bool CanFlip => flip != null && flip.HasBack;

    /// <summary>A taken-out ID card turns over (front: the ID; back: the profile). Never while attached.</summary>
    public bool Flip()
    {
        if (flip == null || state != State.Out || !flip.Toggle()) return false;
        return true;
    }

    private void HandleFlipChanged()
    {
        // The cached capture of this record's window redraws while the card turns.
        windowLayer?.MarkDirty();
    }

    // =========================================================
    // BUILD
    // =========================================================

    /// <summary>
    /// Builds the record lying in slot (slot space: +Y out of the page, +Z the page's top, metres),
    /// with its holders and contact shadow left on the page. workspace holds it while it is out.
    /// </summary>
    public static ClientRecordPaper Create(IClientRecordHolder holder, Transform slot, Transform workspace, ClientRecord record, Vector2 size, int layer)
    {
        bool card = record.IsCard;
        float thickness = IdCardModel.ThicknessFor(record, size);

        // On the page: the soft contact shadow (stays when the record is out) and the holders.
        var shadow = Part("Contact Shadow", slot, layer, SupportingDocumentMesh.RoundedSlab(size.x * 1.01f, size.y * 1.01f, 0.0002f, card ? size.x * 0.06f : size.x * 0.006f),
            SupportingDocumentMesh.Matte(PageShadow));
        shadow.localPosition = new Vector3(size.x * 0.012f, 0f, -size.y * 0.016f);
        Transform clip = card ? BuildCorners(slot, size, thickness, layer) : BuildClip(slot, size, thickness, layer);

        var root = new GameObject("Record " + record.id);
        root.layer = layer;
        root.transform.SetParent(slot, false);
        root.transform.localPosition = new Vector3(0f, 0.0003f, 0f);

        Vector2 units = SupportingDocumentFace.SizeOf(record.type);
        // One paper model for both (2026-10-27): the ID card (profile on its back) and the sheet with the
        // same depth (bevelled both sides around a darker core), each printed as before.
        CardFlip flip = IdCardModel.Build(root.transform, record, size, layer, out Canvas canvas, out _);
        GameObject dropShadow = IdCardModel.AddDropShadow(root.transform, record, size, layer);
        dropShadow.SetActive(false);   // on the page the contact shadow does this; out of the folder it follows the paper
        // Input stays on an ordinary layer while the visible paper is captured
        // separately. It never needs a second visible copy behind its window.
        RectTransform hit = HudTheme.CreateRect("Touch", root.transform);
        hit.localPosition = new Vector3(0f, thickness + IdCardModel.FaceLift(size) * 1.5f, 0f);
        hit.localRotation = Quaternion.Euler(90f, 0f, 0f);
        hit.localScale = Vector3.one * (size.x * (card ? 0.94f : 0.96f) / units.x);
        hit.sizeDelta = units;
        hit.gameObject.layer = layer;
        Canvas touchCanvas = hit.gameObject.AddComponent<Canvas>();
        touchCanvas.renderMode = RenderMode.WorldSpace;
        hit.gameObject.AddComponent<GraphicRaycaster>();
        Image hitImage = HudTheme.AddImage(hit, null, new Color(1f, 1f, 1f, 0f));
        hitImage.raycastTarget = true;
        hitImage.canvasRenderer.cullTransparentMesh = false;
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            if (graphic != hitImage) graphic.raycastTarget = false;
        hit.gameObject.AddComponent<RecordTouchForwarder>();

        ClientRecordPaper paper = root.AddComponent<ClientRecordPaper>();
        paper.flip = flip;
        paper.dropShadow = dropShadow;
        if (flip != null)
        {
            flip.Changed += paper.HandleFlipChanged;
            flip.Flipped += backUp => paper.holder?.PaperFlipped(paper, backUp);
        }
        paper.holder = holder;
        paper.slot = slot;
        paper.workspace = workspace;
        paper.Record = record;
        paper.Size = size;
        paper.faceCanvas = canvas;
        paper.touchCanvas = touchCanvas;
        paper.attachedLayer = layer;
        paper.contactShadow = shadow;
        paper.clip = clip;
        if (clip != null) paper.clipRest = clip.localRotation;
        return paper;
    }

    private static readonly Color PageShadow = new Color32(206, 200, 188, 255);
    private static readonly Color HolderColor = new Color32(86, 68, 50, 255);
    private static readonly Color Steel = new Color32(152, 158, 166, 255);
    private static readonly Color SteelDark = new Color32(104, 110, 118, 255);

    /// <summary>Four photo corners over the card's corners (they stay on the page). Returns null (no clip).</summary>
    private static Transform BuildCorners(Transform slot, Vector2 size, float thickness, int layer)
    {
        float leg = Mathf.Min(size.x, size.y) * 0.2f;
        float hx = size.x * 0.5f + leg * 0.06f, hz = size.y * 0.5f + leg * 0.06f;
        Mesh wedge = SupportingDocumentMesh.CornerWedge(leg, thickness * 1.5f);
        Material material = SupportingDocumentMesh.Matte(HolderColor);
        // Each corner's legs run along the card's edges, pointing inward.
        (Vector3 at, float yaw)[] corners =
        {
            (new Vector3(-hx, 0f, hz), 90f), (new Vector3(hx, 0f, hz), 180f),
            (new Vector3(hx, 0f, -hz), 270f), (new Vector3(-hx, 0f, -hz), 0f)
        };
        foreach (var (at, yaw) in corners)
        {
            Transform part = Part("Photo Corner", slot, layer, wedge, material);
            part.localPosition = at;
            part.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
        return null;
    }

    /// <summary>A small modelled clip over the sheet's top edge (it stays on the page); returns its hinged plate.</summary>
    private static Transform BuildClip(Transform slot, Vector2 size, float thickness, int layer)
    {
        float w = size.x * 0.24f, d = size.y * 0.06f;
        var hinge = new GameObject("Clip");
        hinge.layer = layer;
        hinge.transform.SetParent(slot, false);
        // Hinged on its back edge, just above the sheet's top edge.
        hinge.transform.localPosition = new Vector3(0f, 0f, size.y * 0.5f + d * 0.35f);
        Transform back = Part("Clip Back", hinge.transform, layer, SupportingDocumentMesh.RoundedSlab(w, d * 0.7f, 0.0012f, d * 0.1f),
            SupportingDocumentMesh.Matte(SteelDark));
        back.localPosition = Vector3.zero;
        Transform plate = Part("Clip Plate", hinge.transform, layer, SupportingDocumentMesh.BeveledSlab(w, d, 0.0016f, d * 0.12f, 0.0005f),
            SupportingDocumentMesh.Matte(Steel));
        plate.localPosition = new Vector3(0f, thickness + 0.0006f, -d * 0.5f);
        Transform wire = Part("Clip Wire", hinge.transform, layer, SupportingDocumentMesh.RoundedSlab(w * 0.7f, d * 0.9f, 0.0008f, d * 0.2f),
            SupportingDocumentMesh.Matte(SteelDark));
        wire.localPosition = new Vector3(0f, thickness + 0.003f, d * 0.2f);
        wire.localRotation = Quaternion.Euler(-24f, 0f, 0f);
        return hinge.transform;
    }

    /// <summary>A part with one material per submesh (the printed sheet: its print, then its back and edge).</summary>
    private static Transform Part(string name, Transform parent, int layer, Mesh mesh, Material first, Material second)
    {
        Transform part = Part(name, parent, layer, mesh, first);
        part.GetComponent<MeshRenderer>().sharedMaterials = new[] { first, second };
        return part;
    }

    private static Transform Part(string name, Transform parent, int layer, Mesh mesh, Material material)
    {
        var part = new GameObject(name);
        part.layer = layer;
        part.transform.SetParent(parent, false);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = part.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return part.transform;
    }

    // =========================================================
    // MOTION
    // =========================================================

    /// <summary>The camera the record's face takes touches through.</summary>
    public void SetEventCamera(Camera camera)
    {
        if (faceCanvas != null) faceCanvas.worldCamera = camera;
        if (touchCanvas != null) touchCanvas.worldCamera = camera;
    }

    private Vector3 RestPosition => slot.TransformPoint(new Vector3(0f, 0.0003f, 0f));
    private Quaternion RestRotation => slot.rotation;
    private float RestScale => slot.lossyScale.x;

    private float WorldScale => transform.lossyScale.x;

    private void SetWorldScale(float scale)
    {
        Transform parent = transform.parent;
        float parentScale = parent != null ? Mathf.Max(1e-6f, parent.lossyScale.x) : 1f;
        transform.localScale = Vector3.one * (scale / parentScale);
    }

    /// <summary>Out of the holder into the workspace (world pose kept).</summary>
    private void Detach()
    {
        Doc3DLayer folderLayer = Doc3DLayer.For(slot);
        Transform hudCanvas = folderLayer != null ? folderLayer.transform.parent : null;
        float scale = WorldScale;
        transform.SetParent(workspace, true);
        SetWorldScale(scale);
        if (contactShadow != null) contactShadow.gameObject.SetActive(false);
        if (dropShadow != null) dropShadow.SetActive(true);
        if (hudCanvas != null)
        {
            windowLayer = Doc3DLayer.Create(hudCanvas, transform, Record.tab);
            windowLayer.AlwaysComposite = true;
            windowLayer.AlwaysReadOnly = true;
            windowLayer.ResolutionScale = 2f;
            windowLayer.RefreshTextBeforeCapture = true;
            windowLayer.RenderOnlyWhenChanged = true;
            // 16-22 are spare render layers; 23-26 belong to annotated papers.
            // Reserve one per open record, including records from other files.
            for (int candidate = 16; candidate <= 22; candidate++)
            {
                bool used = false;
                foreach (Doc3DLayer existing in Doc3DLayer.All)
                    if (existing != null && existing.PersistentRenderLayer == candidate) { used = true; break; }
                if (used) continue;
                captureLayer = candidate;
                break;
            }
            if (captureLayer >= 0)
            {
                foreach (Transform node in GetComponentsInChildren<Transform>(true)) node.gameObject.layer = captureLayer;
                touchCanvas.gameObject.layer = attachedLayer;
                windowLayer.PersistentRenderLayer = captureLayer;
                ExcludeMainCopy(holder?.ViewCamera);
            }
            windowLayer.AssignSortingOrder(FloatingWindow.TakeTopSortingOrder());
        }
        clipTarget = 1f;
        holder?.Extracted(this);
    }

    public void BringWindowToFront() => windowLayer?.BringToFront(false);

    /// <summary>Resize/orientation changes retain a readable full object inside the active view.</summary>
    public void FitInsideView(Camera camera)
    {
        if (camera == null || state != State.Out) return;
        if (HeldOnScreen.TryScreenBox(transform, camera, out Rect box))
        {
            float fit = Mathf.Min((camera.pixelWidth - HeldOnScreen.Margin * 2f) / Mathf.Max(1f, box.width),
                (camera.pixelHeight - HeldOnScreen.Margin * 2f) / Mathf.Max(1f, box.height));
            if (fit < 1f) SetWorldScale(WorldScale * Mathf.Max(0.01f, fit));
        }
        transform.position = HeldOnScreen.FitOnScreen(transform, camera);
    }

    private void CloseWindowLayer()
    {
        if (windowLayer == null) return;
        windowLayer.gameObject.SetActive(false);
        Destroy(windowLayer.gameObject);
        windowLayer = null;
        foreach (Transform node in GetComponentsInChildren<Transform>(true)) node.gameObject.layer = attachedLayer;
        captureLayer = -1;
    }

    private void ExcludeMainCopy(Camera camera)
    {
        if (camera != null && captureLayer >= 0) camera.cullingMask &= ~(1 << captureLayer);
    }

    private void OnDestroy() => CloseWindowLayer();

    /// <summary>A tap on the attached record: a short lift and turn into its readable pose.</summary>
    public void TakeOut()
    {
        if (state != State.Attached || holder == null) return;
        Detach();
        holder.ReadingPose(this, out toPosition, out toRotation, out toScale);
        FitTargetPose();
        StartMotion(State.Lifting, !Record.IsCard ? 0.44f : LiftSeconds);
        PlayCue(true);
    }

    /// <summary>Back into its holder with a short settle (onto the page shown now).</summary>
    public void PutBack()
    {
        if (state == State.Attached || state == State.Returning) return;
        float seconds = !Record.IsCard ? 0.34f : ReturnSeconds;
        // An ID card showing its profile turns back to its front on the way in, finishing before it lands.
        flip?.TurnToFront(seconds * 0.85f);
        clipTarget = 1f;
        StartMotion(State.Returning, seconds);
    }

    /// <summary>Straight back into its holder (closing the folder, an interruption).</summary>
    public void Gather()
    {
        flip?.ShowFront();
        CloseWindowLayer();
        if (contactShadow != null) contactShadow.gameObject.SetActive(true);
        if (dropShadow != null) dropShadow.SetActive(false);
        bool wasOut = state != State.Attached;
        transform.SetParent(slot, false);
        transform.localPosition = new Vector3(0f, 0.0003f, 0f);
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        state = State.Attached;
        clipTarget = 0f;
        clipOpen = 0f;
        if (clip != null) clip.localRotation = clipRest;
        if (wasOut) holder?.PaperLanded(this);
    }

    /// <summary>Puts it at this camera depth (front order among records that are out), keeping its place and size on screen.</summary>
    public void SetFrontDepth(Camera camera, float depth)
    {
        if (camera == null || state != State.Out) return;
        float current = Vector3.Dot(transform.position - camera.transform.position, camera.transform.forward);
        if (current <= camera.nearClipPlane || depth <= camera.nearClipPlane) return;
        Vector3 screen = camera.WorldToScreenPoint(transform.position);
        float scale = WorldScale * depth / current;
        transform.position = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        SetWorldScale(scale);
    }

    private void StartMotion(State next, float seconds)
    {
        fromPosition = transform.position;
        fromRotation = transform.rotation;
        fromScale = WorldScale;
        elapsed = 0f;
        motionSeconds = Mathf.Max(0.05f, seconds);
        state = next;
    }

    private float motionSeconds;

    // Fit the destination before the motion starts, so a paper does not snap
    // sideways on its last frame when it reaches the viewport margin.
    private void FitTargetPose()
    {
        Camera camera = holder?.ViewCamera;
        if (camera == null) return;
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        float scale = WorldScale;
        transform.SetPositionAndRotation(toPosition, toRotation);
        SetWorldScale(toScale);
        toPosition = HeldOnScreen.FitOnScreen(transform, camera);
        transform.SetPositionAndRotation(position, rotation);
        SetWorldScale(scale);
    }

    private static float SmoothMotion(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    private void Update()
    {
        ExcludeMainCopy(holder?.ViewCamera);
        // The clip opens as the sheet leaves and closes as it lands (restrained, one short swing).
        if (clip != null && !Mathf.Approximately(clipOpen, clipTarget))
        {
            clipOpen = Mathf.MoveTowards(clipOpen, clipTarget, Time.unscaledDeltaTime / ClipSeconds);
            // A positive turn about the hinge lifts the plate's free (front) edge off the sheet.
            clip.localRotation = clipRest * Quaternion.Euler(ClipOpenDegrees * WorldPickupMotion.Smooth(clipOpen), 0f, 0f);
            if (clipTarget > 0f && clipOpen >= 1f && state != State.Returning) clipTarget = 0f;
        }
        UpdateRefuse();
        if (state != State.Lifting && state != State.Returning && state != State.Moving) return;

        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / motionSeconds);
        // A sheet's clip releases first; direct dragging still has no delay.
        float moveT = state == State.Lifting && clip != null ? Mathf.Clamp01((t - 0.1f) / 0.9f) : t;
        float eased = SmoothMotion(moveT);
        if (state == State.Returning)
        {
            // Aim at the holder as it is now (the folder may move); a small arc toward the viewer, then it settles flat.
            toPosition = RestPosition;
            toRotation = RestRotation;
            toScale = RestScale;
            if (t > 0.8f) clipTarget = 0f;
        }
        Vector3 position = Vector3.Lerp(fromPosition, toPosition, eased);
        Camera camera = holder?.ViewCamera;
        if (state != State.Moving)
        {
            // Leave the holder along its surface normal before sweeping into
            // the reading pose; reverse the path into a soft, flush landing.
            Vector3 lift = fromPosition + fromRotation * Vector3.up * (Size.y * fromScale * 0.22f);
            Vector3 settle = toPosition + toRotation * Vector3.up * (Size.y * toScale * 0.12f);
            float u = 1f - eased;
            position = u * u * u * fromPosition + 3f * u * u * eased * lift +
                3f * u * eased * eased * settle + eased * eased * eased * toPosition;
        }
        transform.SetPositionAndRotation(position, Quaternion.Slerp(fromRotation, toRotation, eased));
        SetWorldScale(Mathf.Lerp(fromScale, toScale, eased));
        if (t < 1f) return;

        if (state == State.Moving)
        {
            state = State.Out;
            FitInsideView(camera);
        }
        else if (state == State.Lifting)
        {
            state = State.Out;
            FitInsideView(camera);
            holder?.PaperSettled(this);
        }
        else
        {
            float landingClip = clipOpen;
            Gather();
            // Landed: the clip (opened on the way in) swings shut.
            clipOpen = landingClip;
            clipTarget = 0f;
            if (clip != null) clip.localRotation = clipRest * Quaternion.Euler(ClipOpenDegrees * WorldPickupMotion.Smooth(clipOpen), 0f, 0f);
            PlayCue(false);
        }
    }

    /// <summary>Out of the folder: eases to a new pose (the holder arranging or stacking), then holds still. No progress counted.</summary>
    public void MoveTo(Vector3 position, Quaternion rotation, float worldScale, float seconds)
    {
        if (state != State.Out && state != State.Moving) return;
        toPosition = position;
        toRotation = rotation;
        toScale = worldScale;
        FitTargetPose();
        StartMotion(State.Moving, seconds);
    }

    /// <summary>A gentle "not here": a short, small shake where it was let go.</summary>
    public void Refuse()
    {
        if (state != State.Out) return;
        refuseElapsed = 0f;
        refuseBase = transform.rotation;
    }

    private void UpdateRefuse()
    {
        if (refuseElapsed < 0f) return;
        if (state != State.Out) { refuseElapsed = -1f; return; }
        refuseElapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(refuseElapsed / RefuseSeconds);
        Camera camera = holder?.ViewCamera;
        Vector3 axis = camera != null ? camera.transform.forward : transform.up;
        transform.rotation = Quaternion.AngleAxis(Mathf.Sin(t * Mathf.PI * 4f) * RefuseDegrees * (1f - t), axis) * refuseBase;
        if (t >= 1f) { transform.rotation = refuseBase; refuseElapsed = -1f; }
    }

    /// <summary>The holder moves it as part of a stack (it follows the holder, not its own drag).</summary>
    public void BeginGroupMove()
    {
        if (state == State.Out || state == State.Moving) state = State.Dragging;
    }

    public void EndGroupMove()
    {
        if (state == State.Dragging) state = State.Out;
    }

    /// <summary>Focus lost mid-drag: it simply stays where it is, out and still.</summary>
    public void CancelDrag()
    {
        groupDrag = false;
        if (state == State.Dragging) state = State.Out;
    }

    private void PlayCue(bool lift)
    {
        if (AudioManager.Instance == null) return;
        bool card = Record != null && Record.type == ClientRecord.Identity;
        AudioManager.Instance.PlayEffect(lift ? (card ? "sfx_card_pickup_01" : "sfx_paper_pickup_01") : (card ? "sfx_card_place_01" : "sfx_paper_place_01"),
            lift ? 0.7f : 0.6f);
    }

    // =========================================================
    // INPUT (forwarded from the face's touch area)
    // =========================================================

    private bool InputAllowed => holder != null && holder.ViewCamera != null && !DialogueUI.IsAnyDialogueActive &&
        !InGameSettings.IsOpen && !HudRewardScreen.AnyOpen && isActiveAndEnabled;

    public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = true;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (InputAllowed) holder.PointerDown(this, eventData);
    }

    public void OnPointerUp(PointerEventData eventData) => holder?.PointerUp(this, eventData);

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left || MobileInputGuard.IsDragOrCancelled(eventData) || !InputAllowed) return;
        if (state == State.Attached) TakeOut();
        else if (state == State.Out && holder.BroughtToFront(this)) Flip();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left || !InputAllowed) return;
        // A record still lifting from a tap can be caught straight away (T28: no input wait).
        if (state != State.Attached && state != State.Out && state != State.Moving && state != State.Lifting) return;
        eventData.eligibleForClick = false;
        holder.PointerMoved(this);
        if (state != State.Attached && holder.IsGrouped(this))
        {
            groupDrag = true;
            holder.BeginGroupDrag(this, eventData);
            return;
        }
        if (state == State.Moving) state = State.Out;
        Camera camera = holder.ViewCamera;
        bool attached = state == State.Attached;
        extractedByDrag = attached || state == State.Lifting;
        if (extractedByDrag)
        {
            // Dragged straight out of its holder (or caught mid-lift): it turns to read and sits under the finger.
            if (attached) Detach();
            holder.ReadingPose(this, out Vector3 position, out Quaternion rotation, out float scale);
            transform.SetPositionAndRotation(position, rotation);
            SetWorldScale(scale);
            dragDepth = Vector3.Dot(position - camera.transform.position, camera.transform.forward);
            transform.position = camera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, dragDepth));
            // One cue per action: the tap that started the lift already played it.
            if (attached) PlayCue(true);
        }
        else holder.BroughtToFront(this);
        dragDepth = Vector3.Dot(transform.position - camera.transform.position, camera.transform.forward);
        grabOffset = transform.position - camera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, dragDepth));
        held = HeldOnScreen.ForModel(transform, camera, eventData.position);
        state = State.Dragging;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (groupDrag) { holder?.DragGroup(eventData); return; }
        if (state != State.Dragging || holder?.ViewCamera == null) return;
        Camera camera = holder.ViewCamera;
        // Exactly under the finger (no spring), stopping where more would leave the screen.
        Vector2 screen = held.Clamp(eventData.position, camera.pixelRect);
        transform.position = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, dragDepth)) + grabOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (groupDrag)
        {
            groupDrag = false;
            holder?.EndGroupDrag(eventData);
            return;
        }
        if (state != State.Dragging) return;
        state = State.Out;
        if (holder != null && holder.WantsReturn(this, eventData.position, PutBack)) return;
        if (extractedByDrag) holder?.PaperSettled(this);
        extractedByDrag = false;
    }

    private void OnDisable()
    {
        groupDrag = false;
        refuseElapsed = -1f;
        if (state == State.Dragging || state == State.Lifting || state == State.Returning || state == State.Moving) Gather();
    }
}

/// <summary>Passes the record face's touches to its ClientRecordPaper (the face canvas sits under the record root).</summary>
public sealed class RecordTouchForwarder : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
    IInitializePotentialDragHandler, IPointerDownHandler, IPointerUpHandler
{
    private ClientRecordPaper paper;
    private ClientRecordPaper Paper => paper != null ? paper : (paper = GetComponentInParent<ClientRecordPaper>(true));

    public void OnInitializePotentialDrag(PointerEventData eventData) => Paper?.OnInitializePotentialDrag(eventData);
    public void OnPointerClick(PointerEventData eventData) => Paper?.OnPointerClick(eventData);
    public void OnPointerDown(PointerEventData eventData) => Paper?.OnPointerDown(eventData);
    public void OnPointerUp(PointerEventData eventData) => Paper?.OnPointerUp(eventData);
    public void OnBeginDrag(PointerEventData eventData) => Paper?.OnBeginDrag(eventData);
    public void OnDrag(PointerEventData eventData) => Paper?.OnDrag(eventData);
    public void OnEndDrag(PointerEventData eventData) => Paper?.OnEndDrag(eventData);
}
