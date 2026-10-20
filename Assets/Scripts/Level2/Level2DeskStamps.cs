using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using TouchPhase = UnityEngine.TouchPhase;

/// <summary>
/// PURPOSE:
/// Stamping on Rosa's actual desk folder (Level 2), under
/// Level2Gameplay/DeskStamps. Replaces Level 1's old stamp setup in this
/// scene only (a separate stamp folder behind the desk folder, with the
/// stamps moved off camera):
///
/// 1. Tap the stamp set (desk view, folder open): the folder leafs back to
///    page 1 with page-turn flips, and two clear hand stamps (READY / NOT READY
///    printed on their blocks) rise out of the stand into a tidy column beside
///    the folder, the same size on screen whatever the camera, gently bobbing.
///    The desk's own stamp models hide while their hand stamps are up.
/// 2. Drag a stamp (Level2StampDrag): it follows the finger, sways with the
///    motion, and over the front page it glides down to hover just above a
///    pulsing see-through ghost of the exact imprint (aimed a little above the
///    finger on touch, so the finger never hides it).
/// 3. Release on the front page: it rises, slams onto the ghost's exact spot
///    (pad = imprint size and angle), squashes, the paper thumps, an ink ring
///    bursts, the ink pops in (when recorded), then it peels off and floats
///    home. A rejected press wobbles and leaves a faint fading mark.
/// 4. Tap Put Stamps Away, close the folder or leave the desk: the stamps
///    fly back to the stand at their original size.
///
/// LAYERING: while up, the stamps live on StampLayer, which the main camera
/// skips and a stacked URP overlay camera (StampCamera) draws after the whole
/// 3D world. So no folder, book or desk geometry can ever hide them, at any
/// depth. Their Doc3DLayer only steps in above screen paper windows.
///
/// The decision is made once at impact by Level2PreliminaryStamp.Stamp
/// (initial review); animation never decides anything.
/// </summary>
public sealed class Level2DeskStamps : MonoBehaviour
{
    public const string ChildName = "DeskStamps";
    public const string ReadyPath = Level2PreliminaryStamp.StampSetPath + "/ReadyForFilingStamp";
    public const string NotReadyPath = Level2PreliminaryStamp.StampSetPath + "/NotReadyForFilingStamp";
    public const int DragLayer = 7; // "3DInteractable": in the camera's pointer mask, not the desk tap mask
    /// <summary>Unnamed layer for lifted stamps (Training binder 29, settings board 30, Doc3DLayer 31).</summary>
    public const int StampLayer = 28;

    // Layout (viewport / screen fractions), tunable.
    public const float StampScreenHeight = 0.17f;  // each floating stamp is 17% of the screen tall
    public const float ColumnGap = 0.07f;          // gap from the paper's right edge (viewport x)
    // Keep both bodies above the desk's front edge in the seated view.
    public static readonly float[] SlotHeights = { 0.69f, 0.45f };

    private const float LiftSeconds = 0.45f;
    private const float ReturnSeconds = 0.35f;

    public bool Active { get; private set; }
    public CaseFolderUI Folder { get; private set; }
    public bool CanUseDesk => CameraController.Instance != null &&
        CameraController.Instance.CurrentMode == CameraController.CameraMode.Workstation &&
        !InGameSettings.IsOpen && !DialogueUI.IsAnyDialogueActive &&
        (GameStateMachine.Instance == null || !GameStateMachine.Instance.IsPaused);
    public bool CanPoint => Active && CanUseDesk;

    /// <summary>A stamp is held or pressing: other systems (the review cutoff) wait for it to finish.</summary>
    public bool Busy => held != null || (ready != null && (ready.IsDragging || ready.IsPressing)) || (notReady != null && (notReady.IsDragging || notReady.IsPressing));

    public const string SeatPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/DeskViewpoint";

    private Level2StampDrag ready;
    private Level2StampDrag notReady;
    private Level2StampDrag held;
    private Transform stampSet;
    private bool wired;
    private int lastToggleFrame = -1;
    private GameObject standTapCanvas;
    private Doc3DLayer toolLayer;

    public Doc3DLayer AcquireToolLayer()
    {
        if (toolLayer != null) return toolLayer;
        toolLayer = Doc3DLayer.Create(null, transform, "Desk stamps");
        toolLayer.PersistentRenderLayer = StampLayer;
        // The overlay camera already puts the stamps above the 3D world; the
        // capture is only needed above screen paper windows / captured documents.
        toolLayer.AlwaysComposite = false;
        toolLayer.CompositeOverDocuments = true;
        toolLayer.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        toolLayer.AssignSortingOrder(FloatingWindow.TakeTopSortingOrder());
        return toolLayer;
    }

    private void ReleaseToolLayer()
    {
        if (toolLayer != null) Destroy(toolLayer.gameObject);
        toolLayer = null;
    }

    // =========================================================
    // STAMP CAMERA (URP overlay: lifted stamps draw over the whole 3D world)
    // =========================================================

    private Camera stampCamera;
    private Camera stackedOn;

    private void LateUpdate()
    {
        bool needed = (ready != null && ready.OnStampLayer) || (notReady != null && notReady.OnStampLayer);
        SyncStampCamera(needed);
    }

    private void SyncStampCamera(bool needed)
    {
        Camera main = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        if (stackedOn != null && stackedOn != main) Unstack();
        if (!needed || main == null)
        {
            if (stampCamera != null && stampCamera.enabled) stampCamera.enabled = false;
            return;
        }
        UniversalAdditionalCameraData baseData = main.GetUniversalAdditionalCameraData();
        if (baseData == null || baseData.renderType != CameraRenderType.Base) return;
        if (stampCamera == null)
        {
            var go = new GameObject("Desk Stamp Camera");
            stampCamera = go.AddComponent<Camera>();
            stampCamera.cullingMask = 1 << StampLayer;
            stampCamera.clearFlags = CameraClearFlags.Depth;
            UniversalAdditionalCameraData data = stampCamera.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Overlay;
            data.renderShadows = false;
            data.renderPostProcessing = false;
        }
        if (stampCamera.transform.parent != main.transform)
        {
            stampCamera.transform.SetParent(main.transform, false);
            stampCamera.transform.localPosition = Vector3.zero;
            stampCamera.transform.localRotation = Quaternion.identity;
        }
        // First in the stack, so later overlays (settings board) stay above the stamps.
        if (!baseData.cameraStack.Contains(stampCamera)) baseData.cameraStack.Insert(0, stampCamera);
        stackedOn = main;
        main.cullingMask &= ~(1 << StampLayer);
        stampCamera.orthographic = main.orthographic;
        stampCamera.orthographicSize = main.orthographicSize;
        stampCamera.fieldOfView = main.fieldOfView;
        stampCamera.nearClipPlane = Mathf.Max(0.005f, main.nearClipPlane * 0.1f);
        stampCamera.farClipPlane = main.farClipPlane;
        if (!stampCamera.enabled) stampCamera.enabled = true;
    }

    private void Unstack()
    {
        if (stackedOn != null && stampCamera != null)
        {
            UniversalAdditionalCameraData baseData = stackedOn.GetUniversalAdditionalCameraData();
            if (baseData != null) baseData.cameraStack.Remove(stampCamera);
        }
        stackedOn = null;
    }

    private void DestroyStampCamera()
    {
        Unstack();
        if (stampCamera != null) Destroy(stampCamera.gameObject);
        stampCamera = null;
    }

    private void Update()
    {
        if (!wired) TryWire();
        if (putAwayButton == null && wired && ready != null) putAwayButton = PutAwayButton.Create(this);
        if (putAwayButton != null) putAwayButton.SetVisible(Active);
        if (ready != null) ready.SetDeskAffordance(CanUseDesk);
        if (notReady != null) notReady.SetDeskAffordance(CanUseDesk);
        if (!Active)
        {
            if (ready == null || notReady == null) return;
            Level2StampDrag hover = null;
            if (CanUseDesk && TryReadPointer(out Vector2 screen, out bool down, out _))
            {
                hover = PickStamp(screen, true);
                if (down && hover != null && !PointerOverControl(screen)) Toggle();
            }
            ready.SetHovered(hover == ready);
            notReady.SetHovered(hover == notReady);
            return;
        }
        if (putAwayAt > 0f && Time.unscaledTime >= putAwayAt && held == null)
        {
            PutAway();
            return;
        }

        bool atDesk = CameraController.Instance != null &&
                      CameraController.Instance.CurrentMode == CameraController.CameraMode.Workstation;
        if (!atDesk || Folder == null || !Folder.IsShowing)
        {
            SetActive(false);
            return;
        }
        if (!CanPoint)
        {
            if (held != null) held.CancelDrag();
            held = null;
            ready.SetHovered(false);
            notReady.SetHovered(false);
            return;
        }
        PollPointer();
    }

    // =========================================================
    // POINTER (read directly: nothing else can swallow the grab)
    // =========================================================

    private void PollPointer()
    {
        if (!TryReadPointer(out Vector2 screen, out bool down, out bool up)) return;
        if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Canceled) ||
            (Touchscreen.current != null && Touchscreen.current.primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled))
        {
            if (held != null) held.CancelDrag();
            held = null;
            return;
        }
        Level2StampDrag hovered = held != null ? held : PickStamp(screen);
        if (ready != null) ready.SetHovered(hovered == ready);
        if (notReady != null) notReady.SetHovered(hovered == notReady);

        if (down && held == null)
        {
            Level2StampDrag hit = PickStamp(screen);
            if (hit != null && hit.BeginDrag(screen)) held = hit;
        }
        if (held == null) return;
        if (up)
        {
            Level2StampDrag released = held;
            held = null;
            released.EndDrag(screen);
        }
        else
        {
            held.DragTo(screen);
        }
    }

    /// <summary>First touch, else the mouse (legacy Input, like the desk's own taps).</summary>
    public static bool TryReadPointer(out Vector2 screen, out bool down, out bool up)
    {
        screen = default;
        down = up = false;
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && (touchscreen.primaryTouch.press.isPressed ||
            touchscreen.primaryTouch.press.wasPressedThisFrame || touchscreen.primaryTouch.press.wasReleasedThisFrame))
        {
            screen = touchscreen.primaryTouch.position.ReadValue();
            down = touchscreen.primaryTouch.press.wasPressedThisFrame;
            up = touchscreen.primaryTouch.press.wasReleasedThisFrame;
            return true;
        }
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            screen = mouse.position.ReadValue();
            down = mouse.leftButton.wasPressedThisFrame;
            up = mouse.leftButton.wasReleasedThisFrame;
            return true;
        }
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            screen = touch.position;
            down = touch.phase == TouchPhase.Began;
            up = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            return true;
        }
        screen = Input.mousePosition;
        down = Input.GetMouseButtonDown(0);
        up = Input.GetMouseButtonUp(0);
        return true;
    }

    /// <summary>The lifted stamp under the finger (nearest), by its drawn shape on screen.</summary>
    private Level2StampDrag PickStamp(Vector2 screen, bool includeStand = false)
    {
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        if (view == null) return null;
        Level2StampDrag best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < 2; i++)
        {
            Level2StampDrag stamp = i == 0 ? ready : notReady;
            if (stamp == null || (!stamp.IsUp && !includeStand)) continue;
            if (stamp.ScreenHit(view, screen, out float distance, includeStand) && distance < bestDistance)
            {
                bestDistance = distance;
                best = stamp;
            }
        }
        return best;
    }

    // =========================================================
    // PUT AWAY
    // =========================================================

    private float putAwayAt = -1f;
    private PutAwayButton putAwayButton;

    /// <summary>Puts both stamps back on the stand after <paramref name="delay"/> seconds (real time).</summary>
    public void PutAwaySoon(float delay) => putAwayAt = Time.unscaledTime + delay;

    /// <summary>Puts both stamps back on the stand now (the PUT STAMPS AWAY button).</summary>
    public void PutAway()
    {
        putAwayAt = -1f;
        SetActive(false);
    }

    private void TryWire()
    {
        stampSet = PlayerPlacement.Find(gameObject.scene, Level2PreliminaryStamp.StampSetPath);
        if (stampSet == null || !stampSet.gameObject.activeInHierarchy) return;
        Transform readyT = PlayerPlacement.Find(gameObject.scene, ReadyPath);
        Transform notReadyT = PlayerPlacement.Find(gameObject.scene, NotReadyPath);
        if (readyT == null || notReadyT == null)
        {
            Debug.LogWarning("[Level2] Desk stamps not found; desk stamping is off.");
            wired = true;
            return;
        }

        // Level 1's stamp set action and drags are replaced in this scene only.
        foreach (StampSetInteractable old in stampSet.GetComponents<StampSetInteractable>()) Destroy(old);
        foreach (Stamp3DDrag old in stampSet.GetComponentsInChildren<Stamp3DDrag>(true)) old.enabled = false;
        if (stampSet.GetComponent<Level2StampSet>() == null) stampSet.gameObject.AddComponent<Level2StampSet>().Owner = this;

        Transform seat = PlayerPlacement.Find(gameObject.scene, SeatPath);
        ready = Level2StampDrag.Create(transform, readyT, StampType.ReadyForFiling, this, seat);
        notReady = Level2StampDrag.Create(transform, notReadyT, StampType.NotReadyForFiling, this, seat);
        BuildStandTapTargets();
        wired = true;
    }

    private void BuildStandTapTargets()
    {
        RectTransform root = HudTheme.CreateRect("Desk stamp tap targets", null);
        standTapCanvas = root.gameObject;
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1099;
        root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        for (int i = 0; i < 2; i++)
        {
            RectTransform target = HudTheme.CreateRect("Tap stamp", root);
            HudTheme.Stretch(target);
            UnityEngine.UI.Image image = target.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            Level2StampHitTarget hit = target.gameObject.AddComponent<Level2StampHitTarget>();
            hit.Owner = i == 0 ? ready : notReady;
            hit.IncludeStand = true;
        }
    }

    /// <summary>The stamp set was tapped at the desk.</summary>
    public void OpenFromDesk()
    {
        // Touch world interactions arrive on release, while the UI press
        // arrives on down. Both must mean "open", never open then close.
        if (TryReadPointer(out Vector2 screen, out _, out _) && PointerOverControl(screen)) return;
        if (!Active) Toggle();
    }

    private static bool PointerOverControl(Vector2 screen)
    {
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events == null) return false;
        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events) { position = screen }, hits);
        foreach (var hit in hits)
            if (hit.gameObject != null && hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null) return true;
        return false;
    }

    public void Toggle()
    {
        if (lastToggleFrame == Time.frameCount) return;
        lastToggleFrame = Time.frameCount;
        if (Active)
        {
            SetActive(false);
            return;
        }

        Folder = FindDeskFolder();
        if (Folder == null || !Folder.IsShowing)
        {
            Level2Say.Line("Open Rosa's folder on the desk first, then use the stamps.");
            return;
        }
        SetActive(true);
    }

    private void SetActive(bool on)
    {
        if (Active == on || ready == null || notReady == null) return;
        Active = on;
        putAwayAt = -1f;
        if (on)
        {
            Folder.Closing -= PutAway;
            Folder.Closing += PutAway;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
            Folder.TurnToPage(StampPageIndex(), () =>
            {
                if (!Active || Folder == null || !Folder.IsShowing) return;
                ready.Lift(0, LiftSeconds);
                notReady.Lift(1, LiftSeconds + 0.1f);
                GameplayEvents.RaiseGuideCue("StampsUp");
            });
        }
        else
        {
            lastToggleFrame = Time.frameCount;
            if (Folder != null) Folder.Closing -= PutAway;
            if (held != null) held.CancelDrag();
            held = null;
            ready.ReturnToStand(ReturnSeconds);
            notReady.ReturnToStand(ReturnSeconds + 0.06f);
            ReleaseToolLayer();
        }
    }

    /// <summary>
    /// Where a lifted stamp floats: a column just right of the paper (inside
    /// the view), upright to the screen and turned slightly toward the viewer,
    /// sized so the stamp is StampScreenHeight of the screen.
    /// </summary>
    public bool TryFloatPose(int slot, out Vector3 position, out Vector3 up, out float size)
    {
        position = transform.position;
        up = Vector3.up;
        size = 0.1f;
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        RectTransform paper = Folder != null ? Folder.PaperRect : null;
        if (view == null || paper == null) return false;

        var corners = new Vector3[4];
        paper.GetWorldCorners(corners);
        float right = float.MinValue, bottom = float.MaxValue, top = float.MinValue;
        foreach (Vector3 corner in corners)
        {
            Vector3 viewport = view.WorldToViewportPoint(corner);
            right = Mathf.Max(right, viewport.x);
            bottom = Mathf.Min(bottom, viewport.y);
            top = Mathf.Max(top, viewport.y);
        }
        // Held tools belong in front of the desk's foreground geometry too,
        // not just in front of the folder. Keep room beyond the near clip.
        float depth = view.nearClipPlane * 1.6f;
        float x = Mathf.Clamp(right + ColumnGap, 0.08f, 0.9f);
        float centre = (bottom + top) * 0.5f;
        float y = Mathf.Clamp(centre + SlotHeights[Mathf.Clamp(slot, 0, SlotHeights.Length - 1)] - 0.5f, 0.14f, 0.88f);
        position = view.ViewportToWorldPoint(new Vector3(x, y, depth));

        float screenWorldHeight = 2f * depth * Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);
        size = screenWorldHeight * StampScreenHeight;
        // Upright on screen, top tipped ~15 degrees toward the viewer so its top shows.
        up = Quaternion.AngleAxis(-15f, view.transform.right) * view.transform.up;
        return true;
    }

    /// <summary>The side of the paper facing the camera.</summary>
    public Vector3 PaperNormalToCamera()
    {
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        RectTransform paper = Folder != null ? Folder.PaperRect : null;
        if (view == null || paper == null) return Vector3.up;
        Vector3 normal = -paper.forward;
        return Vector3.Dot(normal, view.transform.position - paper.position) < 0f ? -normal : normal;
    }

    /// <summary>
    /// The page a stamp goes on: the front page for the initial review, the
    /// Final Assessment page once the review is back at the office (T08).
    /// </summary>
    public int StampPageIndex()
    {
        CaseReviewState review = Level2OfficeStart.Review;
        if (Folder == null || review == null || review.phase < ReviewPhase.OfficeReconciliation) return 0;
        int index = Folder.FindPageIndex(Level2OfficeReview.FinalTitle);
        return index >= 0 ? index : 0;
    }

    /// <summary>The final assessment stamps (T08) rather than the initial review.</summary>
    public static bool FinalStamping
    {
        get
        {
            CaseReviewState review = Level2OfficeStart.Review;
            return review != null && review.phase >= ReviewPhase.OfficeReconciliation;
        }
    }

    /// <summary>What a stamp would print now (the same text as the real ink).</summary>
    public static string InkTextFor(StampType type)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        bool initial = review == null || review.phase < ReviewPhase.OfficeReconciliation;
        if (type == StampType.NotReadyForFiling) return initial ? Level2FindingsPage.InkText : "NOT READY\nFOR FILING";
        return initial ? "INITIAL REVIEW\nREADY" : "READY\nFOR FILING";
    }

    public static Color InkColorFor(StampType type) =>
        type == StampType.NotReadyForFiling ? ReviewInkMark.InkRed : ReviewInkMark.InkGreen;

    /// <summary>The desk folder's world paper that is open now (not the flat HUD copy).</summary>
    private CaseFolderUI FindDeskFolder()
    {
        foreach (CaseFolderUI folder in FindObjectsByType<CaseFolderUI>(FindObjectsInactive.Include))
            if (folder.IsWorldPaper && folder.IsShowing) return folder;
        foreach (CaseFolderUI folder in FindObjectsByType<CaseFolderUI>(FindObjectsInactive.Include))
            if (folder.IsWorldPaper) return folder;
        return null;
    }

    /// <summary>Where a screen point lands on the folder's front page: paper-local point, world point and the side facing the camera.</summary>
    public bool TryPaperPoint(Vector2 screen, out Vector2 local, out Vector3 world, out Vector3 normalToCamera, out bool onFrontPage)
    {
        local = default;
        world = default;
        normalToCamera = Vector3.up;
        onFrontPage = false;
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        RectTransform paper = Folder != null ? Folder.PaperRect : null;
        if (view == null || paper == null || !Folder.IsShowing) return false;

        normalToCamera = PaperNormalToCamera();
        var plane = new Plane(normalToCamera, paper.position);
        Ray ray = view.ScreenPointToRay(screen);
        if (!plane.Raycast(ray, out float enter)) return false;
        world = ray.GetPoint(enter);
        Vector3 p = paper.InverseTransformPoint(world);
        local = new Vector2(p.x, p.y);
        Rect rect = paper.rect;
        bool inside = local.x > rect.xMin + 20f && local.x < rect.xMax - 20f && local.y > rect.yMin + 20f && local.y < rect.yMax - 20f;
        onFrontPage = inside && Folder.CurrentPageIndex == StampPageIndex();
        return inside;
    }

    /// <summary>
    /// Moves an imprint centre (paper-local) just enough that the whole
    /// impression stays on the page, so the ghost, the landing and the ink
    /// are always the same complete mark.
    /// </summary>
    public Vector2 KeepInkOnPage(Vector2 local)
    {
        RectTransform paper = Folder != null ? Folder.PaperRect : null;
        if (paper == null) return local;
        return FitInk(local, paper.rect, ReviewInkMark.Size, 18f);
    }

    /// <summary>Clamps an imprint centre so an <paramref name="ink"/>-sized mark (plus margin) fits inside <paramref name="page"/>.</summary>
    public static Vector2 FitInk(Vector2 centre, Rect page, Vector2 ink, float margin)
    {
        float halfW = ink.x * 0.5f + margin, halfH = ink.y * 0.5f + margin;
        float x = page.width > halfW * 2f ? Mathf.Clamp(centre.x, page.xMin + halfW, page.xMax - halfW) : page.center.x;
        float y = page.height > halfH * 2f ? Mathf.Clamp(centre.y, page.yMin + halfH, page.yMax - halfH) : page.center.y;
        return new Vector2(x, y);
    }

    /// <summary>
    /// Where a stamp sits flat on the front page for an imprint at
    /// <paramref name="local"/> tilted <paramref name="tilt"/> degrees: the
    /// world point under the pad, the side facing the camera, the imprint's
    /// "up" along the paper, the imprint height in world units, and the stamp
    /// size whose rubber pad covers the imprint exactly.
    /// </summary>
    public bool TryContactPose(Vector2 local, float tilt, out Vector3 contact, out Vector3 normal, out Vector3 inkUp,
        out float inkHeight, out float stampSize)
    {
        contact = default;
        normal = Vector3.up;
        inkUp = Vector3.forward;
        inkHeight = 0.01f;
        stampSize = 0.1f;
        RectTransform paper = Folder != null ? Folder.PaperRect : null;
        if (paper == null) return false;
        contact = paper.TransformPoint(new Vector3(local.x, local.y, 0f));
        normal = PaperNormalToCamera();
        inkUp = Vector3.ProjectOnPlane(Quaternion.AngleAxis(tilt, paper.forward) * paper.up, normal);
        if (inkUp.sqrMagnitude < 1e-6f) return false;
        inkUp.Normalize();
        float inkWidth = paper.TransformVector(new Vector3(ReviewInkMark.Size.x, 0f, 0f)).magnitude;
        inkHeight = paper.TransformVector(new Vector3(0f, ReviewInkMark.Size.y, 0f)).magnitude;
        stampSize = inkWidth * 1.02f / Level2StampDrag.PadWidth;
        return stampSize > 1e-5f;
    }

    private void OnDisable()
    {
        if (Folder != null) Folder.Closing -= PutAway;
        Active = false;
        ReleaseToolLayer();
        DestroyStampCamera();
        if (standTapCanvas != null) standTapCanvas.SetActive(false);
    }

    private void OnEnable()
    {
        if (standTapCanvas != null) standTapCanvas.SetActive(true);
    }

    private void OnDestroy()
    {
        ReleaseToolLayer();
        DestroyStampCamera();
        if (standTapCanvas != null) Destroy(standTapCanvas);
    }
}
