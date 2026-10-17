using UnityEngine;

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
/// 2. Drag a stamp (Level2StampDrag): it tilts so you see it, hovers above the
///    paper (never behind it), and a see-through ghost imprint shows exactly
///    where it will land.
/// 3. Release on the front page: it straightens, slams down with a squash, the
///    paper thumps, the ink pops in where it landed (when recorded), then it
///    rebounds and floats home. A rejected press leaves a faint fading mark.
/// 4. Tap the stamp set again, close the folder or leave the desk: the stamps
///    fly back to the stand at their original size.
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

    // Layout (viewport / screen fractions), tunable.
    public const float StampScreenHeight = 0.17f;  // each floating stamp is 17% of the screen tall
    public const float ColumnGap = 0.07f;          // gap from the paper's right edge (viewport x)
    public static readonly float[] SlotHeights = { 0.62f, 0.3f };

    private const float LiftSeconds = 0.45f;
    private const float ReturnSeconds = 0.35f;

    public bool Active { get; private set; }
    public CaseFolderUI Folder { get; private set; }

    public const string SeatPath = "Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/DeskViewpoint";

    private Level2StampDrag ready;
    private Level2StampDrag notReady;
    private Level2StampDrag held;
    private Transform stampSet;
    private bool wired;

    private void Update()
    {
        if (!wired) TryWire();
        if (putAwayButton == null && wired && ready != null) putAwayButton = PutAwayButton.Create(this);
        if (putAwayButton != null) putAwayButton.SetVisible(Active);
        if (!Active) return;
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
        PollPointer();
    }

    // =========================================================
    // POINTER (read directly: nothing else can swallow the grab)
    // =========================================================

    private void PollPointer()
    {
        if (!TryReadPointer(out Vector2 screen, out bool down, out bool up)) return;

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
    private static bool TryReadPointer(out Vector2 screen, out bool down, out bool up)
    {
        screen = default;
        down = up = false;
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            screen = touch.position;
            down = touch.phase == TouchPhase.Began;
            up = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            return true;
        }
        if (!Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonUp(0)) return false;
        screen = Input.mousePosition;
        down = Input.GetMouseButtonDown(0);
        up = Input.GetMouseButtonUp(0);
        return true;
    }

    /// <summary>The lifted stamp under the finger (nearest), by its drawn shape on screen.</summary>
    private Level2StampDrag PickStamp(Vector2 screen)
    {
        Camera view = Camera.main;
        if (view == null) return null;
        Level2StampDrag best = null;
        float bestDistance = float.MaxValue;
        foreach (Level2StampDrag stamp in new[] { ready, notReady })
        {
            if (stamp == null || !stamp.IsUp) continue;
            if (stamp.ScreenHit(view, screen, out float distance) && distance < bestDistance)
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
        wired = true;
    }

    /// <summary>The stamp set was tapped at the desk.</summary>
    public void Toggle()
    {
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
            Folder.TurnToPage(0);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
            ready.Lift(0, LiftSeconds);
            notReady.Lift(1, LiftSeconds + 0.1f);
        }
        else
        {
            if (held != null) held.EndDrag(Vector2.zero);
            held = null;
            ready.ReturnToStand(ReturnSeconds);
            notReady.ReturnToStand(ReturnSeconds + 0.06f);
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
        Camera view = Camera.main;
        RectTransform paper = Folder != null ? Folder.PaperRect : null;
        if (view == null || paper == null) return false;

        float depth = Mathf.Max(0.2f, Vector3.Dot(paper.position - view.transform.position, view.transform.forward) * 0.95f);
        var corners = new Vector3[4];
        paper.GetWorldCorners(corners);
        float right = 0f;
        foreach (Vector3 corner in corners) right = Mathf.Max(right, view.WorldToViewportPoint(corner).x);
        float x = Mathf.Clamp(right + ColumnGap, 0.7f, 0.9f);
        float y = SlotHeights[Mathf.Clamp(slot, 0, SlotHeights.Length - 1)];
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
        Camera view = Camera.main;
        RectTransform paper = Folder != null ? Folder.PaperRect : null;
        if (view == null || paper == null) return Vector3.up;
        Vector3 normal = -paper.forward;
        return Vector3.Dot(normal, view.transform.position - paper.position) < 0f ? -normal : normal;
    }

    /// <summary>What a stamp would print now (the same text as the real ink).</summary>
    public static string InkTextFor(StampType type)
    {
        CaseReviewState review = Level2OfficeStart.Review;
        bool initial = review == null || review.phase < ReviewPhase.FinalAssessment;
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
        Camera view = Camera.main;
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
        onFrontPage = inside && Folder.CurrentPageIndex == 0;
        return inside;
    }

    private void OnDisable()
    {
        Active = false;
    }
}
