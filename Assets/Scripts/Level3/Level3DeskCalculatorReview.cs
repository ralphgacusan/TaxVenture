using UnityEngine;

/// <summary>
/// PURPOSE:
/// Stamping Bea's computation at the office desk (Level 3 T09), the third part
/// of Level3DeskCalculator (one component, one input owner). Two hand stamps,
/// READY and NOT READY, rest on the desk's own stamp stand (Desk/Stamps: the
/// inherited models hide while these stand in for them, placed from the
/// scene's own transforms) and lift with the sheet and the calculator into a
/// column between them, sized to the screen.
///
/// - Tap a stamp: it rises a little and a pulsing ghost of its imprint shows
///   in the sheet's stamp box. Tap the stamp box: the stamp swings over it,
///   slams, squashes and lifts home (the supplied stamp cues). At the moment
///   of contact the chapter command (Level3ReviewRules.Stamp) saves first;
///   only a saved stamp leaves ink. A refused or unsaved press leaves none
///   and says why. Tap the picked stamp again to put it down.
/// - READY means "Ready for computation review" (supported and current), not
///   filing or payment; NOT READY is a justified hold. The ink covers the
///   sheet's current revision; any later change marks it STAMP OUT OF DATE.
/// Nothing here decides the review (Level3Audit / Level3ReviewRules do).
/// Guide cues: StampPicked, SheetStamped.
/// </summary>
public sealed partial class Level3DeskCalculator
{
    public const string CueStampPicked = "StampPicked";
    public const string CueSheetStamped = "SheetStamped";
    /// <summary>Each lifted stamp is about this share of the screen tall (Level 2's desk stamps use 0.17).</summary>
    public const float StampScreenHeight = 0.17f;
    private const float StampHeight = 0.066f;
    private const float StampBlockWidth = 0.074f;
    private const float PressTravel = 0.22f, PressSlam = 0.07f, PressSquash = 0.18f, PressHome = 0.28f;
    private static readonly Color StampWood = new Color32(140, 92, 54, 255);
    private static readonly Color StampRubber = new Color32(58, 52, 50, 255);

    private sealed class DeskStamp
    {
        public string Type;
        public Transform Root;
        public Transform Body;
        public Collider Grab;
        public Pose Rest, From, To;
    }

    private DeskStamp readyStamp, holdStamp;
    private GameObject[] standModels = new GameObject[0];
    private string pickedStamp;
    private DeskStamp pressing;
    private float pressStart = -1f;
    private bool pressContact;
    private Pose pressFrom;

    /// <summary>A tool is moving, a tape is flying or a stamp is pressing (the review cutoff waits for it).</summary>
    public bool Busy => toolDragging || mode == Mode.Lifting || mode == Mode.Returning || flyStart >= 0f || pressStart >= 0f;

    /// <summary>The stamps belong to Bea's computation (her sheet is on the clipboard).</summary>
    private bool StampsShown => readyStamp != null && ComputationOpen;

    // =========================================================
    // BUILD
    // =========================================================

    private void BuildStamps(Transform desk, Transform seat, Vector3 forward, Vector3 right, Quaternion facing)
    {
        readyStamp = BuildStamp(Level3ReviewRules.StampReady, "READY", Level3ComputationLeafModel.ReadyInk);
        holdStamp = BuildStamp(Level3ReviewRules.StampHold, "NOT READY", Level3ComputationLeafModel.HoldInk);
        // The desk's own stamp stand: these stand where its models are (resolved from the scene), else beside the clipboard.
        Transform readyModel = PlayerPlacement.Find(gameObject.scene, Level2DeskStamps.ReadyPath);
        Transform holdModel = PlayerPlacement.Find(gameObject.scene, Level2DeskStamps.NotReadyPath);
        standModels = new[] { readyModel != null ? readyModel.gameObject : null, holdModel != null ? holdModel.gameObject : null };
        readyStamp.Rest = StandPose(readyModel, DeskPoint(desk, seat, seat.position + forward * 0.36f - right * 0.2f), facing);
        holdStamp.Rest = StandPose(holdModel, DeskPoint(desk, seat, seat.position + forward * 0.36f - right * 0.28f), facing);
        Apply(readyStamp.Root, readyStamp.Rest);
        Apply(holdStamp.Root, holdStamp.Rest);
        readyStamp.Root.gameObject.SetActive(false);
        holdStamp.Root.gameObject.SetActive(false);
        Debug.Log($"[Level3] Desk stamps at {readyStamp.Rest.Position} / {holdStamp.Rest.Position} ({(readyModel != null ? "the desk's stamp stand" : "beside the clipboard")}).");
    }

    /// <summary>On the stand model's own base (its renderers' lowest point), facing the seat; the fallback point otherwise.</summary>
    private static Pose StandPose(Transform model, Vector3 fallback, Quaternion facing)
    {
        Vector3 at = fallback;
        if (model != null)
        {
            Bounds? bounds = null;
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (bounds == null) bounds = r.bounds;
                else { Bounds b = bounds.Value; b.Encapsulate(r.bounds); bounds = b; }
            }
            at = bounds.HasValue ? new Vector3(bounds.Value.center.x, bounds.Value.min.y, bounds.Value.center.z) : model.position;
        }
        return new Pose { Position = at, Rotation = facing, Scale = 1f };
    }

    private DeskStamp BuildStamp(string type, string label, Color cap)
    {
        var root = new GameObject("Stamp " + label).transform;
        root.gameObject.layer = Level2DeskStamps.DragLayer;
        root.SetParent(transform, false);
        var body = new GameObject("Body").transform;
        body.gameObject.layer = root.gameObject.layer;
        body.SetParent(root, false);
        // Pad at the bottom (its face at y = 0), coloured block, wooden neck and knob on top.
        props.Box("Pad", body, new Vector3(0f, 0.003f, 0f), new Vector3(StampBlockWidth * 0.94f, 0.006f, 0.029f), StampRubber);
        props.Box("Block", body, new Vector3(0f, 0.017f, 0f), new Vector3(StampBlockWidth, 0.022f, 0.032f), cap);
        props.Box("Neck", body, new Vector3(0f, 0.035f, 0f), new Vector3(0.018f, 0.016f, 0.018f), StampWood);
        props.Shape(PrimitiveType.Sphere, "Knob", body, new Vector3(0f, 0.052f, 0f), new Vector3(0.032f, 0.026f, 0.032f), StampWood);
        // The label on the block's front (toward the seat, and toward the camera when lifted).
        var face = new GameObject("Label").transform;
        face.gameObject.layer = root.gameObject.layer;
        face.SetParent(body, false);
        face.localPosition = new Vector3(0f, 0.017f, -0.0165f);
        face.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        props.Print("Print", face, Vector3.zero, StampBlockWidth - 0.008f, 0.014f, label, Level3DeskProps.Cream, bold: true);
        BoxCollider grab = props.Target("Grab", root, new Vector3(0f, 0.032f, 0f), new Vector3(StampBlockWidth + 0.01f, StampHeight + 0.006f, 0.042f));
        return new DeskStamp { Type = type, Root = root, Body = body, Grab = grab };
    }

    private bool StampOwns(Collider collider) =>
        StampsShown && collider != null && (collider.transform.IsChildOf(readyStamp.Root) || collider.transform.IsChildOf(holdStamp.Root));

    private DeskStamp StampOf(Collider collider) =>
        collider == null || readyStamp == null ? null : collider.transform.IsChildOf(readyStamp.Root) ? readyStamp : collider.transform.IsChildOf(holdStamp.Root) ? holdStamp : null;

    // =========================================================
    // STATE AND MOTION
    // =========================================================

    /// <summary>Shown with Bea's computation; the inherited stand models hide while these stand in for them.</summary>
    private void SyncStamps()
    {
        if (readyStamp == null) return;
        bool shown = StampsShown;
        if (readyStamp.Root.gameObject.activeSelf == shown) return;
        readyStamp.Root.gameObject.SetActive(shown);
        holdStamp.Root.gameObject.SetActive(shown);
        foreach (GameObject model in standModels)
            if (model != null)
                foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true)) r.enabled = !shown;
        if (shown && mode == Mode.Resting) { Apply(readyStamp.Root, readyStamp.Rest); Apply(holdStamp.Root, holdStamp.Rest); }
    }

    /// <summary>Lift (true) or put away (false): the stamps travel with the tools.</summary>
    private void CaptureStamps(bool lifting)
    {
        if (!StampsShown) return;
        DisarmStamp();
        foreach (DeskStamp stamp in new[] { readyStamp, holdStamp })
        {
            stamp.From = Capture(stamp.Root);
            if (!lifting) stamp.To = stamp.Rest;
        }
    }

    private void AnimateStamps(float eased)
    {
        if (!StampsShown || pressing != null) return;
        Apply(readyStamp.Root, Pose.Lerp(readyStamp.From, readyStamp.To, eased));
        Apply(holdStamp.Root, Pose.Lerp(holdStamp.From, holdStamp.To, eased));
    }

    /// <summary>The column between the sheet and the calculator: READY above, NOT READY below, upright, label to the camera.</summary>
    private void ComputeStampPoses(Camera view, float scale, float viewportX)
    {
        Quaternion upright = Quaternion.LookRotation(view.transform.forward, view.transform.up);
        float[] heights = { 0.64f, 0.38f };
        DeskStamp[] stamps = { readyStamp, holdStamp };
        for (int i = 0; i < 2; i++)
        {
            Vector3 center = view.ViewportToWorldPoint(new Vector3(viewportX, heights[i], LiftDistance));
            stamps[i].To = new Pose { Position = center - upright * new Vector3(0f, StampHeight * 0.5f * scale, 0f), Rotation = upright, Scale = scale };
        }
    }

    private void ApplyStampsLifted()
    {
        if (!StampsShown || pressing != null) return;
        Apply(readyStamp.Root, LiftedPose(readyStamp));
        Apply(holdStamp.Root, LiftedPose(holdStamp));
    }

    /// <summary>A picked stamp rises a little toward the camera and grows (the other stays in the column).</summary>
    private Pose LiftedPose(DeskStamp stamp)
    {
        Pose pose = stamp.To;
        if (stamp.Type != pickedStamp) return pose;
        Camera view = CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
        float bob = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
        if (view != null) pose.Position += view.transform.up * (StampHeight * pose.Scale * (0.12f + 0.04f * bob)) - view.transform.forward * 0.02f;
        pose.Scale *= 1.1f;
        return pose;
    }

    private void TickStamps()
    {
        SyncStamps();
        if (!StampsShown) return;
        if (pressing != null) { AnimatePress(); return; }
        if (mode == Mode.Lifted) ApplyStampsLifted();
    }

    // =========================================================
    // ACTIONS
    // =========================================================

    /// <summary>A tap on a lifted stamp: pick it (ghost on the box), or put it down again.</summary>
    private bool ActivateStamp(Collider target)
    {
        DeskStamp stamp = StampOwns(target) ? StampOf(target) : null;
        if (stamp == null) return false;
        if (pressing != null) return true;
        if (pickedStamp == stamp.Type)
        {
            DisarmStamp();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_stamp_return_01");
            return true;
        }
        DisarmLeaf();
        CloseLeafPicker();
        pickedStamp = stamp.Type;
        leaf.ShowStampGhost(stamp.Type);
        leaf.SetMessage(stamp.Type == Level3ReviewRules.StampReady
            ? "READY: ready for computation review (not filing or paying). Tap the stamp box on the sheet to stamp it."
            : "NOT READY: something is still open. Tap the stamp box on the sheet to stamp it.");
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_stamp_pickup_01");
        GameplayEvents.RaiseGuideCue(CueStampPicked);
        return true;
    }

    /// <summary>A tap on the sheet's stamp box: press the picked stamp, or say how.</summary>
    private void TapStampBox()
    {
        if (pressing != null) return;
        DeskStamp stamp = pickedStamp == Level3ReviewRules.StampReady ? readyStamp : pickedStamp == Level3ReviewRules.StampHold ? holdStamp : null;
        if (stamp == null)
        {
            leaf.SetMessage("Tap READY or NOT READY beside the sheet first, then tap this box.");
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
            return;
        }
        leaf.ShowStampGhost(null);
        pressing = stamp;
        if (mode == Mode.Lifted) pressingLayer = CreateToolLayer(stamp.Root, pointerSurface: false);
        pressFrom = Capture(stamp.Root);
        pressStart = Time.unscaledTime;
        pressContact = false;
    }

    private void DisarmStamp()
    {
        if (pickedStamp == null) return;
        pickedStamp = null;
        leaf?.ShowStampGhost(null);
    }

    /// <summary>Over the box, slam, squash (the save happens at contact), lift off and home. Real time: a pause never freezes a press.</summary>
    private void AnimatePress()
    {
        float t = Time.unscaledTime - pressStart;
        Vector3 up = leaf.Root.up;
        float leafScale = leaf.Root.lossyScale.x;
        Quaternion down = Quaternion.LookRotation(leaf.Root.forward, up);
        float pressScale = leafScale * (Level3ComputationLeafModel.StampW * 0.95f / StampBlockWidth);
        Vector3 contact = leaf.StampBoxWorld + up * (0.0004f * leafScale);
        var hover = new Pose { Position = contact + up * (StampHeight * 0.8f * pressScale), Rotation = down, Scale = pressScale };
        var hit = new Pose { Position = contact, Rotation = down, Scale = pressScale };
        if (t < PressTravel)
        {
            Apply(pressing.Root, Pose.Lerp(pressFrom, hover, Smooth(t / PressTravel)));
            return;
        }
        if (t < PressTravel + PressSlam)
        {
            float u = (t - PressTravel) / PressSlam;
            Apply(pressing.Root, Pose.Lerp(hover, hit, u * u));
            return;
        }
        if (!pressContact)
        {
            pressContact = true;
            Apply(pressing.Root, hit);
            StampContact(pressing.Type);
        }
        if (t < PressTravel + PressSlam + PressSquash)
        {
            float u = (t - PressTravel - PressSlam) / PressSquash;
            float squash = 1f - 0.18f * Mathf.Sin(u * Mathf.PI) * (1f - u);
            pressing.Body.localScale = new Vector3(1f + (1f - squash) * 0.5f, squash, 1f + (1f - squash) * 0.5f);
            return;
        }
        pressing.Body.localScale = Vector3.one;
        float home = (t - PressTravel - PressSlam - PressSquash) / PressHome;
        Pose back = mode == Mode.Lifted ? pressing.To : pressing.Rest;
        Apply(pressing.Root, Pose.Lerp(hit, back, Smooth(Mathf.Clamp01(home))));
        if (home < 1f) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_stamp_return_01", 0.7f);
        if (pressingLayer != null) Destroy(pressingLayer.gameObject);
        pressingLayer = null;
        pressing = null;
        pressStart = -1f;
        pickedStamp = null;
    }

    private static float Smooth(float t) => t * t * (3f - 2f * t);

    /// <summary>The decision of a press: saved first (Level3ReviewRules.Stamp); ink only from the saved stamp.</summary>
    private void StampContact(string type)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_stamp_press_01");
        Level3Checkpointer owner = Level3Checkpointer.Active;
        if (owner == null) return;
        Level3Checkpointer.TapeCommit outcome = owner.CommitComputation((Level3ChapterCheckpoint c, out string r) => Level3ReviewRules.Stamp(c, type, out r), out string hint);
        SyncLeaf(force: true);
        switch (outcome)
        {
            case Level3Checkpointer.TapeCommit.Attached:
                leaf.PopStamp();
                leaf.SetMessage(type == Level3ReviewRules.StampReady
                    ? "Stamped READY for computation review. Take your Case Folder to the Auditor."
                    : "Stamped NOT READY. Hand it to the Auditor as a hold, or keep working and stamp again.");
                GameplayEvents.RaiseGuideCue(CueSheetStamped);
                break;
            case Level3Checkpointer.TapeCommit.AlreadyAttached:
                leaf.SetMessage("The sheet already carries this stamp for its current lines.");
                break;
            default:
                leaf.SetMessage(string.IsNullOrEmpty(hint) ? "That stamp didn't take." : hint);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
                break;
        }
    }

    /// <summary>Settles a press at once (the desk is left mid-press): the stamp goes home; a saved stamp stays saved.</summary>
    private void SettleStamps()
    {
        if (pressing != null)
        {
            // Before contact nothing was decided; after it the stamp is already saved.
            pressing.Body.localScale = Vector3.one;
            pressing = null;
            pressStart = -1f;
        }
        DisarmStamp();
    }
}
