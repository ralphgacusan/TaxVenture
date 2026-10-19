using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// PURPOSE:
/// One hand stamp (Ready / Not Ready) in Level 2's desk stamping (see
/// Level2DeskStamps). The desk's original stamp models read as thin sticks
/// from the desk camera, so in Level 2 these clear stamps replace them on the
/// stand (same size and spot, label toward the desk seat): a wooden grip with
/// a coloured cap, a coloured block printed READY or NOT READY, and an inked
/// rubber pad the same shape as the printed imprint. The original models stay
/// hidden in Level 2 (restored if this part is removed). Dragging is driven by
/// Level2DeskStamps reading the pointer directly (BeginDrag / DragTo /
/// EndDrag), so no other UI or collider can swallow the grab.
///
/// LAYERING: while up (and while flying back), the model is on
/// Level2DeskStamps.StampLayer, drawn by the stacked overlay stamp camera
/// after the whole 3D world: it is never hidden by the folder or the desk.
///
/// Feel:
/// - Lift: rises out of the stand in an arc into its slot beside the folder,
///   a fixed on-screen size, label toward you; then bobs gently. Hover grows it.
/// - Pick up: a quick pop and a click; it stays visible under the finger.
/// - Drag: smooth follow with a sway that leans into the motion. Over the
///   front page it glides down to hover just above a pulsing ghost of the
///   exact imprint (aimed a little above the finger on touch).
/// - Release on the front page: anticipation lift, straighten, slam onto the
///   ghost's exact spot (pad = imprint size and angle), squash and spring,
///   paper thump, ink ring burst, sound; the decision; ink pops in (or a
///   rejected press wobbles and leaves a faint mark that fades); peel off; home.
/// - Released elsewhere: glides home, no penalty.
/// All motion uses real (unscaled) time, so a pause (e.g. Arthur's lesson)
/// can never freeze a stamp mid-press, and a press always finishes.
/// </summary>
public sealed class Level2StampDrag : MonoBehaviour
{
    // Model proportions (units of stamp height = 1). The pad matches the
    // imprint's aspect (ReviewInkMark.Size), so a landed pad covers the ink.
    private const float BlockWidth = 1f, BlockDepth = 0.4f, BlockHeight = 0.28f;
    private const float PadHeight = 0.07f;
    public const float PadWidth = BlockWidth * 0.94f;
    private const float PadDepth = BlockDepth * 0.92f;
    private const float FaceOffset = 0.5f;           // pivot (centre) to rubber face

    // Press timing (seconds, real time).
    private const float AnticipateSeconds = 0.09f;
    private const float SlamSeconds = 0.07f;
    private const float SquashSeconds = 0.24f;
    private const float LiftOffSeconds = 0.16f;
    private const float HomeSeconds = 0.32f;

    // Drag feel.
    private const float FollowSharpness = 22f;
    private const float AimSharpness = 12f;
    private const float HoverTilt = 18f;              // degrees the handle leans back while aiming
    private const float HoverLift = 0.32f;            // pad height above the page while aiming (stamp sizes)
    private const float HoverBack = 1.05f;            // pad shifted past the ghost's top edge (imprint heights)
    private const float TouchAimLift = 0.075f;        // touch: imprint aimed this much of the screen above the finger
    private const float MaxSway = 14f;
    private const float SwayPerSpeed = 9f;            // degrees per (screen heights / second)
    private const float GhostAlpha = 0.38f;

    private static readonly Color Wood = new Color32(140, 92, 54, 255);
    private static readonly Color Brass = new Color32(214, 178, 96, 255);
    private static readonly Color LabelInk = new Color32(250, 246, 236, 255);

    private StampType type;
    private Level2DeskStamps owner;
    private GameObject standModel;     // the desk's own stamp, hidden while this one is up
    private Transform body;            // every part; pivot on the rubber face, for squash and stretch
    private int slot;

    private float size = 0.1f;         // world height of the stamp while floating
    private Vector3 restPosition;      // resting on the stand
    private Quaternion restRotation;
    private float restSize = 0.08f;
    private Vector3 home;
    private Vector3 homeUp = Vector3.up;
    private bool lifted;
    private bool dragging;
    private bool busy;
    private bool moving;
    private bool onStampLayer;
    private Coroutine motion;
    private Collider grab;
    private GameObject ghost;
    private float pressTilt;
    private float bobPhase;
    private HighlightEffect highlight;
    private Doc3DLayer documentLayer;
    private Renderer[] modelRenderers;
    private int hitFrame = -1;
    private Rect hitRect;
    private bool focusPending;
    private GameObject pointerSurface;
    private static Camera View => CameraController.Instance != null ? CameraController.Instance.MainCamera : Camera.main;
    private readonly System.Collections.Generic.List<Material> materials = new System.Collections.Generic.List<Material>();

    // Drag state.
    private Vector2 grabOffset, dragStart, dragScreen, lastScreen;
    private Vector2 velocity;          // screen heights per second, smoothed
    private bool dragMoved;
    private bool touchDrag;
    private bool aimValid;
    private Vector2 aimLocal;
    private float aimT;                // 0 = free in hand, 1 = hovering over the imprint
    private float currentScale;
    private float pickupTime = -1f;
    private bool hovered;
    private float hoverT;

    public bool IsUp => lifted;
    public bool IsDragging => dragging;
    /// <summary>A press is animating (its decision may still be pending).</summary>
    public bool IsPressing => busy;
    /// <summary>True while the model is drawn by the overlay stamp camera.</summary>
    public bool OnStampLayer => onStampLayer;

    /// <summary>The grab target, enabled only while the stamp is up.</summary>
    public Collider Grab => grab;

    /// <summary>
    /// True when <paramref name="screen"/> is over the stamp as it is drawn
    /// right now (its renderers' bounds projected to the screen, plus a
    /// finger-friendly margin). Never lags behind the visuals like a physics
    /// collider can. The score prefers the closest visible centre on overlap.
    /// </summary>
    public bool ScreenHit(Camera view, Vector2 screen, out float depth, bool includeStand = false)
    {
        depth = float.MaxValue;
        if (view == null || (!lifted && !includeStand) || busy || moving || owner == null || !owner.CanUseDesk) return false;
        if (hitFrame != Time.frameCount)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            // Project each actual model box, not a combined world AABB or TMP bounds.
            // Rotated labels and generous text rectangles must not move the grab area.
            foreach (Renderer renderer in modelRenderers)
            {
                if (renderer == null || !renderer.enabled) continue;
                Bounds bounds = renderer.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = bounds.center, e = bounds.extents;
                    Vector3 corner = c + new Vector3((i & 1) == 0 ? -e.x : e.x,
                        (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                    Vector3 p = view.WorldToScreenPoint(renderer.transform.TransformPoint(corner));
                    if (p.z <= 0f) continue;
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                }
            }
            float margin = includeStand ? Mathf.Max(24f, Screen.height * 0.025f) : Mathf.Max(14f, Screen.height * 0.022f);
            hitRect = Rect.MinMaxRect(min.x - margin, min.y - margin, max.x + margin, max.y + margin);
            hitFrame = Time.frameCount;
        }
        bool inside = hitRect.Contains(screen);
        if (inside) depth = (hitRect.center - screen).sqrMagnitude;
        return inside;
    }

    /// <summary>
    /// Builds the hand stamp for one of the desk's stamps and puts it on the
    /// stand in that stamp's place (same height), its label toward
    /// <paramref name="seat"/> (the desk viewpoint). The original model hides.
    /// </summary>
    public static Level2StampDrag Create(Transform parent, Transform deskStamp, StampType stampType, Level2DeskStamps desk, Transform seat)
    {
        var root = new GameObject(stampType == StampType.ReadyForFiling ? "HandStamp Ready" : "HandStamp NotReady");
        root.layer = Level2DeskStamps.DragLayer;
        root.transform.SetParent(parent, false);
        Level2StampDrag stamp = root.AddComponent<Level2StampDrag>();
        stamp.type = stampType;
        stamp.owner = desk;
        stamp.standModel = deskStamp != null ? deskStamp.gameObject : null;
        stamp.bobPhase = stampType == StampType.ReadyForFiling ? 0f : 1.7f;
        stamp.BuildModel();
        stamp.modelRenderers = System.Array.FindAll(root.GetComponentsInChildren<Renderer>(),
            renderer => renderer.GetComponentInParent<TMP_Text>() == null);
        stamp.highlight = HighlightEffect.For(stamp);
        stamp.PlaceOnStand(seat);
        return stamp;
    }

    private void PlaceOnStand(Transform seat)
    {
        Bounds bounds = new Bounds(transform.position, Vector3.one * 0.08f);
        if (standModel != null)
        {
            Renderer[] renderers = standModel.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            }
        }
        restSize = Mathf.Clamp(bounds.size.y, 0.05f, 0.25f);
        restPosition = new Vector3(bounds.center.x, bounds.min.y + restSize * FaceOffset, bounds.center.z);

        Vector3 toSeat = seat != null ? seat.position - restPosition : -Vector3.forward;
        toSeat.y = 0f;
        if (toSeat.sqrMagnitude < 1e-4f) toSeat = -Vector3.forward;
        // Label is on local -Z: +Z points away from the seat.
        restRotation = Quaternion.LookRotation(-toSeat.normalized, Vector3.up);

        if (standModel != null) standModel.SetActive(false);
        transform.SetPositionAndRotation(restPosition, restRotation);
        transform.localScale = Vector3.one * restSize;
        currentScale = restSize;
        SetGrab(false);
    }

    private void BuildModel()
    {
        Color bodyColor = Level2DeskStamps.InkColorFor(type);
        bodyColor = new Color(bodyColor.r * 0.95f, bodyColor.g * 0.95f, bodyColor.b * 0.95f, 1f);
        Color pad = new Color(bodyColor.r * 0.35f, bodyColor.g * 0.35f, bodyColor.b * 0.35f, 1f);
        Material bodyMat = MaterialFor(bodyColor), padMat = MaterialFor(pad), woodMat = MaterialFor(Wood), brassMat = MaterialFor(Brass);

        // Every part hangs off "Body", whose pivot is the rubber face, so a
        // squash keeps the pad pressed exactly where it landed.
        var bodyGo = new GameObject("Body");
        bodyGo.layer = gameObject.layer;
        body = bodyGo.transform;
        body.SetParent(transform, false);
        body.localPosition = new Vector3(0f, -FaceOffset, 0f);

        Part(PrimitiveType.Cube, "Pad", new Vector3(0f, PadHeight * 0.5f, 0f), new Vector3(PadWidth, PadHeight, PadDepth), padMat);
        Part(PrimitiveType.Cube, "Block", new Vector3(0f, PadHeight + BlockHeight * 0.5f, 0f), new Vector3(BlockWidth, BlockHeight, BlockDepth), bodyMat);
        Part(PrimitiveType.Cube, "BlockTrim", new Vector3(0f, PadHeight + BlockHeight + 0.015f, 0f), new Vector3(BlockWidth * 0.8f, 0.03f, BlockDepth * 0.75f), brassMat);
        Part(PrimitiveType.Cylinder, "Neck", new Vector3(0f, FaceOffset + 0.08f, 0f), new Vector3(0.16f, 0.16f, 0.16f), woodMat);
        Part(PrimitiveType.Sphere, "Grip", new Vector3(0f, FaceOffset + 0.33f, 0f), new Vector3(0.34f, 0.3f, 0.34f), woodMat);
        Part(PrimitiveType.Sphere, "Cap", new Vector3(0f, FaceOffset + 0.47f, 0f), new Vector3(0.2f, 0.08f, 0.2f), bodyMat);

        // Printed name on both long faces of the block.
        float labelY = PadHeight + BlockHeight * 0.5f;
        string text = type == StampType.ReadyForFiling ? "READY" : "NOT READY";
        Label(text, new Vector3(0f, labelY, -BlockDepth * 0.5f - 0.004f), Quaternion.identity);
        Label(text, new Vector3(0f, labelY, BlockDepth * 0.5f + 0.004f), Quaternion.Euler(0f, 180f, 0f));

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = Vector3.zero;
        box.size = new Vector3(BlockWidth * 1.3f, 1.2f, BlockDepth * 1.8f);
        grab = box;
    }

    private void Part(PrimitiveType shape, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = name;
        part.layer = gameObject.layer;
        DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(body, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void Label(string text, Vector3 position, Quaternion rotation)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.layer = gameObject.layer;
        go.transform.SetParent(body, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation;
        var label = go.AddComponent<TextMeshPro>();
        label.rectTransform.sizeDelta = new Vector2(BlockWidth * 0.9f, BlockHeight * 0.8f);
        label.font = HudTheme.Font;
        label.text = "<b>" + text + "</b>";
        label.color = LabelInk;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 0.05f;
        label.fontSizeMax = 2f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private Material MaterialFor(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = "Level2 HandStamp", color = color };
        materials.Add(material);
        return material;
    }

    // =========================================================
    // LIFT / RETURN
    // =========================================================

    public void Lift(int floatSlot, float seconds)
    {
        slot = floatSlot;
        if (!owner.TryFloatPose(slot, out home, out homeUp, out size)) return;
        lifted = true;
        hitFrame = -1;
        SetModelLayer(Level2DeskStamps.StampLayer);
        if (documentLayer == null)
        {
            documentLayer = owner.AcquireToolLayer();
            if (highlight != null) { highlight.RefreshTargets(); highlight.SetAffordance(true); }
            RectTransform target = HudTheme.CreateRect("Stamp pointer surface", documentLayer.transform);
            pointerSurface = target.gameObject;
            HudTheme.Stretch(target);
            Image hit = target.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            target.gameObject.AddComponent<Level2StampHitTarget>().Owner = this;
        }
        Focus();
        busy = false;
        dragging = false;
        ResetBody();

        // Rise off the stand and grow into the slot.
        SetGrab(true);
        Play(Travel(home, Facing(homeUp), size, seconds, size * 0.6f, followFolder: true));
    }

    public void ReturnToStand(float seconds)
    {
        CancelPulse();
        SetHovered(false);
        ReleaseLayer();
        lifted = false;
        dragging = false;
        busy = false;
        SetGrab(false);
        HideGhost();
        ResetBody();
        Play(FlyToStand(seconds));
    }

    private IEnumerator FlyToStand(float seconds)
    {
        // Stays on the stamp layer for the flight, so the folder never cuts
        // it off on the way; it rejoins the desk only once it has landed.
        yield return Travel(restPosition, restRotation, restSize, seconds, size * 0.4f);
        SetModelLayer(Level2DeskStamps.DragLayer);
        motion = null;
    }

    // =========================================================
    // POINTER (driven by Level2DeskStamps)
    // =========================================================

    /// <summary>The finger went down on this stamp. False when it cannot be picked up now.</summary>
    public bool BeginDrag(Vector2 screen)
    {
        if (!lifted || busy || owner == null || !owner.Active) return false;
        if (motion != null) StopCoroutine(motion);
        motion = null;
        moving = false;
        CancelPulse();
        Focus();
        dragging = true;
        dragMoved = false;
        dragStart = dragScreen = lastScreen = screen;
        velocity = Vector2.zero;
        aimValid = false;
        aimT = 0f;
        touchDrag = Input.touchCount > 0 ||
                    (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed);
        Camera view = View;
        grabOffset = view != null ? (Vector2)view.WorldToScreenPoint(transform.position) - screen : Vector2.zero;
        pressTilt = Random.Range(-7f, 7f);
        pickupTime = Time.unscaledTime;
        currentScale = transform.localScale.x;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
        SetHovered(true);
        return true;
    }

    public void DragTo(Vector2 screen)
    {
        if (!dragging) return;
        dragScreen = screen;
        if (!dragMoved && (screen - dragStart).sqrMagnitude < 64f) return;
        dragMoved = true;
        aimValid = TryAim(screen, out aimLocal);
        if (aimValid) ShowGhost(aimLocal);
        else HideGhost();
    }

    public void EndDrag(Vector2 screen)
    {
        if (!dragging) return;
        dragging = false;
        SetHovered(false);
        if (!dragMoved)
        {
            // A tap focuses the stamp; it does not press or toss it.
            Play(Travel(home, Facing(homeUp), size, HomeSeconds * 0.6f, 0f, followFolder: true));
            return;
        }
        // Land exactly where the ghost was last shown (what the player saw).
        bool onFront = aimValid || TryAim(screen, out aimLocal);
        bool onPaper = owner.TryPaperPoint(AimScreen(screen), out _, out _, out _, out _);
        HideGhost();
        if (onFront)
        {
            Play(Press(aimLocal));
            return;
        }
        if (onPaper) Level2Say.Line(Level2DeskStamps.FinalStamping
            ? "Stamp the Final Assessment page. Turn to it first."
            : "Stamp the front page. Turn back to page 1 first.");
        Play(Travel(home, Facing(homeUp), size, HomeSeconds, size * 0.2f, followFolder: true));
    }

    public void CancelDrag()
    {
        if (!dragging) return;
        dragging = false;
        SetHovered(false);
        HideGhost();
        Play(Travel(home, Facing(homeUp), size, HomeSeconds, size * 0.2f, followFolder: true));
    }

    /// <summary>Touch aims a little above the finger so the finger never covers the imprint.</summary>
    private Vector2 AimScreen(Vector2 screen) =>
        touchDrag ? screen + Vector2.up * (Screen.height * TouchAimLift) : screen;

    private bool TryAim(Vector2 screen, out Vector2 local)
    {
        local = default;
        if (!owner.TryPaperPoint(AimScreen(screen), out Vector2 point, out _, out _, out bool onFront) || !onFront) return false;
        local = owner.KeepInkOnPage(point);
        return true;
    }

    // =========================================================
    // PER-FRAME POSE (the single owner of the transform outside coroutines)
    // =========================================================

    private void LateUpdate()
    {
        if (lifted && owner != null) owner.TryFloatPose(slot, out home, out homeUp, out size);
        if (documentLayer != null && lifted) documentLayer.BringToFront(false);
        if (focusPending && documentLayer != null)
        {
            // Resolve after the shared document-focus Update, so the paper
            // underneath a stamp cannot steal the same press.
            documentLayer.AssignSortingOrder(FloatingWindow.TakeTopSortingOrder());
            focusPending = false;
        }
        float dt = Time.unscaledDeltaTime;
        hoverT = Mathf.MoveTowards(hoverT, hovered && !busy ? 1f : 0f, dt * 8f);

        if (dragging) UpdateDragPose(dt);
        else if (lifted && !busy && !moving)
        {
            Camera view = View;
            Vector3 screenUp = view != null ? view.transform.up : Vector3.up;
            float lift = Mathf.Sin(Time.unscaledTime * 2.2f + bobPhase) * size * 0.04f + hoverT * size * 0.06f;
            transform.position = home + screenUp * lift;
            transform.rotation = Facing(homeUp);
            currentScale = size * (1f + 0.06f * hoverT);
            transform.localScale = Vector3.one * currentScale;
            KeepModelInView();
        }
        if (ghost != null) PulseGhost();
    }

    private void UpdateDragPose(float dt)
    {
        Camera view = View;
        if (view == null) return;

        // Sway: lean into the motion, settle when still.
        Vector2 frameVelocity = dt > 1e-5f ? (dragScreen - lastScreen) / (dt * Mathf.Max(1f, Screen.height)) : Vector2.zero;
        lastScreen = dragScreen;
        velocity = Vector2.Lerp(velocity, frameVelocity, 1f - Mathf.Exp(-14f * dt));
        aimT = Mathf.Lerp(aimT, aimValid ? 1f : 0f, 1f - Mathf.Exp(-AimSharpness * dt));

        // Free in hand: the grabbed point stays under the finger at the float depth.
        float homeDepth = Vector3.Dot(home - view.transform.position, view.transform.forward);
        Ray ray = view.ScreenPointToRay(dragScreen + grabOffset);
        Vector3 position = ray.GetPoint(homeDepth / Mathf.Max(0.1f, Vector3.Dot(ray.direction, view.transform.forward)));
        Quaternion rotation = Facing(homeUp);
        float scale = size;

        // Aiming: hover just past the ghost, pad parallel to the page, ready to slam.
        if (aimT > 0.001f && owner.TryContactPose(aimLocal, pressTilt, out Vector3 contact, out Vector3 normal,
                out Vector3 inkUp, out float inkHeight, out float stampSize))
        {
            Quaternion hover = Tilted(normal, inkUp, HoverTilt);
            Vector3 pad = contact + normal * (stampSize * HoverLift) + inkUp * (inkHeight * HoverBack);
            position = Vector3.Lerp(position, RootForPad(pad, hover, stampSize), aimT);
            rotation = Quaternion.Slerp(rotation, hover, aimT);
            scale = Mathf.Lerp(scale, stampSize, aimT);
        }

        float swayScale = 1f - 0.5f * aimT;
        float roll = Mathf.Clamp(velocity.x * SwayPerSpeed, -MaxSway, MaxSway) * swayScale;
        float pitch = Mathf.Clamp(-velocity.y * SwayPerSpeed * 0.5f, -MaxSway * 0.5f, MaxSway * 0.5f) * swayScale;
        rotation = Quaternion.AngleAxis(roll, view.transform.forward) * Quaternion.AngleAxis(pitch, view.transform.right) * rotation;

        // Pick-up pop, then held a touch larger than resting.
        float sincePick = pickupTime >= 0f ? Time.unscaledTime - pickupTime : 1f;
        float pop = 1f + 0.12f * Mathf.Sin(Mathf.Clamp01(sincePick / 0.2f) * Mathf.PI) + 0.04f * (1f - aimT);

        float k = 1f - Mathf.Exp(-FollowSharpness * dt);
        transform.position = Vector3.Lerp(transform.position, position, k);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, k);
        currentScale = Mathf.Lerp(currentScale, scale, k);
        transform.localScale = Vector3.one * currentScale * pop;
        hitFrame = -1;
    }

    /// <summary>Root position that puts the rubber face's centre on <paramref name="pad"/>.</summary>
    public static Vector3 RootForPad(Vector3 pad, Quaternion rotation, float modelSize)
        => pad + rotation * (Vector3.up * (FaceOffset * modelSize));

    /// <summary>
    /// Pad parallel to the page (handle along <paramref name="normal"/>,
    /// label toward the paper's bottom edge), the handle leaned back by
    /// <paramref name="degrees"/> toward the top of the page.
    /// </summary>
    public static Quaternion Tilted(Vector3 normal, Vector3 inkUp, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        Vector3 up = normal * Mathf.Cos(r) + inkUp * Mathf.Sin(r);
        Vector3 forward = inkUp * Mathf.Cos(r) - normal * Mathf.Sin(r);
        return Quaternion.LookRotation(forward, up);
    }

    private void KeepModelInView()
    {
        Camera view = View;
        if (view == null) return;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        float nearest = float.MaxValue;
        foreach (Renderer renderer in modelRenderers)
        {
            if (renderer == null || !renderer.enabled) continue;
            Bounds bounds = renderer.localBounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = bounds.center, e = bounds.extents;
                Vector3 p = view.WorldToScreenPoint(renderer.transform.TransformPoint(c + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z)));
                nearest = Mathf.Min(nearest, p.z);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
        }
        if (nearest < view.nearClipPlane * 1.1f)
        {
            transform.position += view.transform.forward * (view.nearClipPlane * 1.1f - nearest);
            hitFrame = -1;
            return;
        }
        Rect viewport = view.pixelRect;
        float margin = Mathf.Max(8f, viewport.height * 0.025f);
        // Keep the grip below the coach's top hint band as well as on camera.
        float topMargin = Mathf.Max(margin, viewport.height * 0.18f);
        Vector2 offset = new Vector2(
            FitScreenAxis(min.x, max.x, viewport.xMin + margin, viewport.xMax - margin),
            FitScreenAxis(min.y, max.y, viewport.yMin + margin, viewport.yMax - topMargin));
        if (offset.sqrMagnitude <= 0.01f) return;
        Vector3 centre = view.WorldToScreenPoint(transform.position);
        transform.position = view.ScreenToWorldPoint(centre + new Vector3(offset.x, offset.y, 0f));
        hitFrame = -1;
    }

    /// <summary>Screen-space correction that keeps both ends inside the actual camera viewport.</summary>
    public static float FitScreenAxis(float min, float max, float lower, float upper)
    {
        if (max - min > upper - lower) return (lower + upper - min - max) * 0.5f;
        if (min < lower) return lower - min;
        if (max > upper) return upper - max;
        return 0f;
    }

    public void TapStand()
    {
        if (!lifted && owner != null && owner.CanUseDesk) owner.OpenFromDesk();
    }

    private void SetModelLayer(int layer)
    {
        foreach (Transform node in GetComponentsInChildren<Transform>(true))
            if (node != transform) node.gameObject.layer = layer;
        onStampLayer = layer == Level2DeskStamps.StampLayer;
    }

    // =========================================================
    // PRESS
    // =========================================================

    private IEnumerator Press(Vector2 local)
    {
        busy = true;
        if (!owner.TryContactPose(local, pressTilt, out _, out _, out _, out _, out float stampSize))
        {
            busy = false;
            yield return Travel(home, Facing(homeUp), size, HomeSeconds, size * 0.2f, followFolder: true);
            yield break;
        }

        // 1. Anticipation: straighten over the imprint and draw back a little.
        Vector3 fromPos = transform.position;
        Quaternion fromRot = transform.rotation;
        float fromScale = transform.localScale.x;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / AnticipateSeconds)
        {
            float e = EaseOutCubic(t);
            if (!ContactRoot(local, stampSize * 0.55f, 0f, out Vector3 target, out Quaternion straight)) break;
            transform.position = Vector3.Lerp(fromPos, target, e);
            transform.rotation = Quaternion.Slerp(fromRot, straight, e);
            transform.localScale = Vector3.one * Mathf.Lerp(fromScale, stampSize, e);
            body.localScale = new Vector3(1f - 0.05f * e, 1f + 0.1f * e, 1f - 0.05f * e);
            yield return null;
        }

        // 2. Slam: accelerate straight down onto the exact imprint.
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / SlamSeconds)
        {
            float e = t * t * t;
            if (!ContactRoot(local, stampSize * 0.55f * (1f - e), 0f, out Vector3 target, out Quaternion straight)) break;
            transform.SetPositionAndRotation(target, straight);
            transform.localScale = Vector3.one * stampSize;
            body.localScale = new Vector3(0.95f, 1.1f + 0.06f * e, 0.95f);
            yield return null;
        }
        PinToContact(local, stampSize, 0f);
        currentScale = stampSize;

        // 3. Impact. The decision is wrapped so the stamp always finishes its motion.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPaper2SFX();
            AudioManager.Instance.PlayMouseClickSelectSFX();
        }
        RectTransform paper = owner.Folder != null ? owner.Folder.PaperRect : null;
        if (paper != null)
        {
            StartCoroutine(Thump(paper));
            StartCoroutine(InkBurst(paper, local, pressTilt, Level2DeskStamps.InkColorFor(type)));
        }
        PreliminaryReview.Outcome outcome = PreliminaryReview.Outcome.NoRule;
        try
        {
            ReviewInkMark.PopNext = true;
            outcome = Level2PreliminaryStamp.Stamp(type, local, pressTilt);
        }
        catch (System.Exception error)
        {
            Debug.LogException(error);
        }
        finally
        {
            ReviewInkMark.PopNext = false;
        }
        bool recorded = outcome == PreliminaryReview.Outcome.Recorded;
        if (!recorded && paper != null)
        {
            GameObject faint = ReviewInkMark.CreateAt(paper, Level2DeskStamps.InkTextFor(type), local, pressTilt);
            ReviewInkMark.SetStyle(faint, Level2DeskStamps.InkColorFor(type), 0.6f);
            ReviewInkMotion.Play(faint, ReviewInkMotion.Mode.FadeOut);
        }

        // Squash and spring back with the pad pinned (the paper may still be thumping).
        for (float t = 0f; t < SquashSeconds; t += Time.unscaledDeltaTime)
        {
            float wave = Mathf.Exp(-13f * t) * Mathf.Cos(t * 34f);
            body.localScale = new Vector3(1f + 0.12f * wave, 1f - 0.24f * wave, 1f + 0.12f * wave);
            // A rejected press gives a small "no" wobble on the paper.
            float wobble = recorded ? 0f : Mathf.Sin(t * 42f) * 5f * Mathf.Exp(-6f * t);
            PinToContact(local, stampSize, wobble);
            yield return null;
        }
        body.localScale = Vector3.one;
        PinToContact(local, stampSize, 0f);

        // 4. Peel off the page, leaning back as it rises.
        Vector3 downPos = transform.position;
        Quaternion downRot = transform.rotation;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / LiftOffSeconds)
        {
            float e = EaseOutCubic(t);
            if (!ContactRoot(local, stampSize * 0.7f, 0f, out Vector3 up, out Quaternion straight)) break;
            Quaternion lean = owner.TryContactPose(local, pressTilt, out _, out Vector3 n, out Vector3 inkUp, out _, out _)
                ? Tilted(n, inkUp, HoverTilt) : straight;
            transform.position = Vector3.Lerp(downPos, up, e);
            transform.rotation = Quaternion.Slerp(downRot, lean, e);
            yield return null;
        }

        // 5. Home.
        yield return Travel(home, Facing(homeUp), size, HomeSeconds, size * 0.3f, followFolder: true);
        busy = false;
        motion = null;

        // A stamp that counted ends the stamping: both stamps go back on the stand.
        if (recorded && owner != null) owner.PutAwaySoon(0.6f);
    }

    /// <summary>Root and straight rotation with the pad <paramref name="height"/> above the imprint (live, follows the paper).</summary>
    private bool ContactRoot(Vector2 local, float height, float twist, out Vector3 root, out Quaternion straight)
    {
        root = transform.position;
        straight = transform.rotation;
        if (!owner.TryContactPose(local, pressTilt, out Vector3 contact, out Vector3 normal, out Vector3 inkUp, out _, out float stampSize))
            return false;
        straight = Quaternion.AngleAxis(twist, normal) * Quaternion.LookRotation(inkUp, normal);
        root = RootForPad(contact + normal * height, straight, stampSize);
        return true;
    }

    private void PinToContact(Vector2 local, float stampSize, float twist)
    {
        if (!ContactRoot(local, 0f, twist, out Vector3 root, out Quaternion straight)) return;
        transform.SetPositionAndRotation(root, straight);
        transform.localScale = Vector3.one * stampSize;
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t) * (1f - t);
    }

    // =========================================================
    // GHOST AND INK FX
    // =========================================================

    private void ShowGhost(Vector2 local)
    {
        RectTransform paper = owner.Folder != null ? owner.Folder.PaperRect : null;
        if (paper == null) return;
        if (ghost == null || ghost.transform.parent != paper)
        {
            HideGhost();
            ghost = ReviewInkMark.CreateAt(paper, Level2DeskStamps.InkTextFor(type), local, pressTilt);
            ghost.name = "StampGhost";
            ReviewInkMark.SetStyle(ghost, Level2DeskStamps.InkColorFor(type), GhostAlpha);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMouseClickSelectSFX();
        }
        var rect = (RectTransform)ghost.transform;
        rect.anchoredPosition = local - paper.rect.center;
        rect.localRotation = Quaternion.Euler(0f, 0f, pressTilt);
        ghost.transform.SetAsLastSibling();
    }

    /// <summary>The ghost breathes so the landing spot reads as "live".</summary>
    private void PulseGhost()
    {
        float wave = Mathf.Sin(Time.unscaledTime * 6.5f);
        ReviewInkMark.SetStyle(ghost, Level2DeskStamps.InkColorFor(type), GhostAlpha + 0.14f * wave);
        ghost.transform.localScale = Vector3.one * (1f + 0.015f * wave);
    }

    private void HideGhost()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
    }

    /// <summary>A soft ring of ink that bursts out from the imprint at impact.</summary>
    private static IEnumerator InkBurst(RectTransform paper, Vector2 local, float angle, Color ink)
    {
        if (paper == null) yield break;
        RectTransform ring = HudTheme.CreateRect("StampBurst", paper);
        ring.gameObject.layer = paper.gameObject.layer;
        ring.anchorMin = ring.anchorMax = ring.pivot = new Vector2(0.5f, 0.5f);
        ring.anchoredPosition = local - paper.rect.center;
        ring.sizeDelta = ReviewInkMark.Size;
        ring.localRotation = Quaternion.Euler(0f, 0f, angle);
        Image image = ring.gameObject.AddComponent<Image>();
        image.sprite = HudTheme.Rounded;
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.32f)
        {
            if (ring == null) yield break;
            float e = 1f - (1f - t) * (1f - t);
            ring.localScale = new Vector3(1f + 0.28f * e, 1f + 0.55f * e, 1f);
            image.color = new Color(ink.r, ink.g, ink.b, 0.28f * (1f - e));
            yield return null;
        }
        if (ring != null) Destroy(ring.gameObject);
    }

    // =========================================================
    // MOTION HELPERS (unscaled time)
    // =========================================================

    private IEnumerator Travel(Vector3 target, Quaternion rotation, float scale, float seconds, float arc, bool followFolder = false)
    {
        moving = true;
        Vector3 from = transform.position;
        float fromScale = transform.localScale.x;
        Quaternion fromRot = transform.rotation;
        Camera view = View;
        Vector3 arcUp = view != null ? view.transform.up : Vector3.up;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds))
        {
            if (followFolder && owner != null && owner.TryFloatPose(slot, out home, out homeUp, out size))
            {
                target = home;
                rotation = Facing(homeUp);
                scale = size;
            }
            float e = EaseOutCubic(t);
            transform.position = Vector3.Lerp(from, target, e) + arcUp * (Mathf.Sin(e * Mathf.PI) * arc);
            transform.rotation = Quaternion.Slerp(fromRot, rotation, e);
            transform.localScale = Vector3.one * Mathf.Lerp(fromScale, scale, e);
            yield return null;
        }
        transform.position = target;
        transform.rotation = rotation;
        transform.localScale = Vector3.one * scale;
        currentScale = scale;
        moving = false;
        hitFrame = -1;
        motion = null;
    }

    /// <summary>The paper dips a little under the impact, then settles.</summary>
    private static IEnumerator Thump(RectTransform paper)
    {
        if (paper == null) yield break;
        Vector3 rest = paper.localScale;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.16f)
        {
            if (paper == null) yield break;
            paper.localScale = rest * Mathf.Lerp(0.978f, 1f, EaseOutCubic(t));
            yield return null;
        }
        if (paper != null) paper.localScale = rest;
    }

    private void Play(IEnumerator routine)
    {
        if (motion != null) StopCoroutine(motion);
        moving = false;
        motion = StartCoroutine(routine);
    }

    /// <summary>Handle along <paramref name="up"/>, printed face turned toward the camera.</summary>
    private static Quaternion Facing(Vector3 up)
    {
        Camera view = View;
        up = up.sqrMagnitude > 1e-6f ? up.normalized : Vector3.up;
        Vector3 forward = view != null ? -Vector3.ProjectOnPlane(view.transform.forward, up) : Vector3.forward;
        if (forward.sqrMagnitude < 1e-4f) forward = Vector3.ProjectOnPlane(view != null ? view.transform.up : Vector3.forward, up);
        // Label faces local -Z, so local +Z points away from the camera.
        return Quaternion.LookRotation(-forward.normalized, up);
    }

    /// <summary>Compensates perspective depth without nonuniform scaling or changing the model proportions.</summary>
    public static float ScaleAtDepth(float baseSize, float homeDepth, float depth)
        => baseSize * (homeDepth > 0.001f && depth > 0.001f ? depth / homeDepth : 1f);

    public void SetHovered(bool value)
    {
        hovered = value;
        if (highlight != null) highlight.SetFocused(value && !busy);
    }

    public void SetDeskAffordance(bool value)
    {
        if (highlight != null) highlight.SetAffordance(value && !busy);
    }

    private void Focus()
    {
        if (documentLayer != null) documentLayer.AssignSortingOrder(FloatingWindow.TakeTopSortingOrder());
        focusPending = true;
    }

    private void ReleaseLayer()
    {
        if (pointerSurface != null) Destroy(pointerSurface);
        pointerSurface = null;
        documentLayer = null;
    }

    private void ResetBody()
    {
        if (body != null) body.localScale = Vector3.one;
    }

    private void CancelPulse()
    {
        pickupTime = -1f;
        ResetBody();
    }

    private void SetGrab(bool on)
    {
        if (grab != null) grab.enabled = on;
    }

    private void OnDisable()
    {
        CancelPulse();
        SetHovered(false);
        ReleaseLayer();
        dragging = false;
        busy = false;
        moving = false;
        HideGhost();
        if (motion != null) StopCoroutine(motion);
        motion = null;
        // Switched off while up or mid-flight: back on the stand, on the desk layer,
        // never left on the stamp layer without its camera.
        if (lifted || onStampLayer)
        {
            lifted = false;
            SetGrab(false);
            transform.SetPositionAndRotation(restPosition, restRotation);
            transform.localScale = Vector3.one * restSize;
            SetModelLayer(Level2DeskStamps.DragLayer);
        }
    }

    private void OnDestroy()
    {
        ReleaseLayer();
        // Level 2 removed: the desk's own stamp model comes back.
        if (standModel != null && !standModel.activeSelf) standModel.SetActive(true);
        foreach (Material material in materials)
            if (material != null) Destroy(material);
        materials.Clear();
    }
}
