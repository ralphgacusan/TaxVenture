using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// One hand stamp (Ready / Not Ready) in Level 2's desk stamping (see
/// Level2DeskStamps). The desk's original stamp models read as thin sticks
/// from the desk camera, so in Level 2 these clear stamps replace them on the
/// stand (same size and spot, label toward the desk seat): a wooden grip with
/// a coloured cap, a coloured block printed READY or NOT READY, and an inked
/// rubber pad. The original models stay hidden in Level 2 (restored if this
/// part is removed). Dragging is driven by Level2DeskStamps reading the
/// pointer directly (BeginDrag / DragTo / EndDrag), so no other UI or
/// collider can swallow the grab.
///
/// Feel:
/// - Lift: rises out of the stand in an arc into its slot beside the folder,
///   growing to a fixed on-screen size, label toward you; then bobs gently.
/// - Pick up: quick scale pop and click.
/// - Drag: smooth follow, tilted so the block shows, hovering above the paper;
///   a see-through ghost of the exact imprint shows where it will land.
/// - Release on the front page: straighten, slam, squash, paper thump and
///   sound; the decision; ink pops in (or a faint mark fades); rebound; home.
/// - Released elsewhere: glides home, no penalty.
/// All motion uses real (unscaled) time, so a pause (e.g. Arthur's lesson)
/// can never freeze a stamp mid-press, and a press always finishes.
/// </summary>
public sealed class Level2StampDrag : MonoBehaviour
{
    // Model proportions (units of stamp height = 1).
    private const float BlockWidth = 0.95f, BlockDepth = 0.55f, BlockHeight = 0.3f;
    private const float PadHeight = 0.07f;
    private const float FaceOffset = 0.5f;           // pivot (centre) to rubber face
    private const float HoverFraction = 0.35f;
    private const float AimHover = 0.45f;              // pad distance from the aim point while aiming (stamp heights)
    private const float DragTilt = 52f;                // body leans this far toward the top of the screen while aiming
    private const float AlignSeconds = 0.08f;
    private const float SlamSeconds = 0.07f;
    private const float ReboundSeconds = 0.16f;
    private const float HomeSeconds = 0.28f;
    private const float FollowSharpness = 24f;
    private const float GhostAlpha = 0.35f;

    private static readonly Color Wood = new Color32(140, 92, 54, 255);
    private static readonly Color Brass = new Color32(214, 178, 96, 255);
    private static readonly Color LabelInk = new Color32(250, 246, 236, 255);

    private StampType type;
    private Level2DeskStamps owner;
    private GameObject standModel;     // the desk's own stamp, hidden while this one is up
    private int slot;

    private float size = 0.1f;         // world height of the stamp while up
    private Vector3 restPosition;      // resting on the stand
    private Quaternion restRotation;
    private float restSize = 0.08f;
    private Vector3 home;
    private Vector3 homeUp = Vector3.up;
    private bool lifted;
    private bool dragging;
    private bool busy;
    private bool moving;
    private Coroutine motion;
    private Vector3 dragTarget;
    private Quaternion dragRotation;
    private Collider grab;
    private GameObject ghost;
    private float pressTilt;
    private float bobPhase;
    private readonly System.Collections.Generic.List<Material> materials = new System.Collections.Generic.List<Material>();

    public bool IsUp => lifted;
    public bool IsDragging => dragging;

    /// <summary>The grab target, enabled only while the stamp is up.</summary>
    public Collider Grab => grab;

    /// <summary>
    /// True when <paramref name="screen"/> is over the stamp as it is drawn
    /// right now (its renderers' bounds projected to the screen, plus a
    /// finger-friendly margin). Never lags behind the visuals like a physics
    /// collider can. <paramref name="depth"/> is the distance to its centre.
    /// </summary>
    public bool ScreenHit(Camera view, Vector2 screen, out float depth)
    {
        depth = float.MaxValue;
        if (view == null || !lifted || busy) return false;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return false;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        Vector3 c = bounds.center, e = bounds.extents;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
            Vector3 p = view.WorldToScreenPoint(corner);
            if (p.z <= 0f) return false;
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
        float margin = Mathf.Max(24f, Screen.height * 0.025f);
        bool inside = screen.x >= min.x - margin && screen.x <= max.x + margin &&
                      screen.y >= min.y - margin && screen.y <= max.y + margin;
        if (inside) depth = Vector3.Distance(view.transform.position, c);
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
        SetGrab(false);
    }

    private void BuildModel()
    {
        Color body = Level2DeskStamps.InkColorFor(type);
        body = new Color(body.r * 0.95f, body.g * 0.95f, body.b * 0.95f, 1f);
        Color pad = new Color(body.r * 0.35f, body.g * 0.35f, body.b * 0.35f, 1f);
        Material bodyMat = MaterialFor(body), padMat = MaterialFor(pad), woodMat = MaterialFor(Wood), brassMat = MaterialFor(Brass);

        // Pivot at the centre of the stamp; rubber face at -FaceOffset.
        Part(PrimitiveType.Cube, "Pad", new Vector3(0f, -FaceOffset + PadHeight * 0.5f, 0f), new Vector3(BlockWidth * 0.94f, PadHeight, BlockDepth * 0.92f), padMat);
        Part(PrimitiveType.Cube, "Block", new Vector3(0f, -FaceOffset + PadHeight + BlockHeight * 0.5f, 0f), new Vector3(BlockWidth, BlockHeight, BlockDepth), bodyMat);
        Part(PrimitiveType.Cube, "BlockTrim", new Vector3(0f, -FaceOffset + PadHeight + BlockHeight + 0.015f, 0f), new Vector3(BlockWidth * 0.8f, 0.03f, BlockDepth * 0.8f), brassMat);
        Part(PrimitiveType.Cylinder, "Neck", new Vector3(0f, 0.08f, 0f), new Vector3(0.18f, 0.16f, 0.18f), woodMat);
        Part(PrimitiveType.Sphere, "Grip", new Vector3(0f, 0.33f, 0f), new Vector3(0.36f, 0.3f, 0.36f), woodMat);
        Part(PrimitiveType.Sphere, "Cap", new Vector3(0f, 0.47f, 0f), new Vector3(0.2f, 0.08f, 0.2f), bodyMat);

        // Printed name on both long faces of the block.
        float labelY = -FaceOffset + PadHeight + BlockHeight * 0.5f;
        string text = type == StampType.ReadyForFiling ? "READY" : "NOT READY";
        Label(text, new Vector3(0f, labelY, -BlockDepth * 0.5f - 0.004f), Quaternion.identity);
        Label(text, new Vector3(0f, labelY, BlockDepth * 0.5f + 0.004f), Quaternion.Euler(0f, 180f, 0f));

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = Vector3.zero;
        box.size = new Vector3(BlockWidth * 1.3f, 1.2f, BlockDepth * 1.6f);
        grab = box;
    }

    private void Part(PrimitiveType shape, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = name;
        part.layer = gameObject.layer;
        DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void Label(string text, Vector3 position, Quaternion rotation)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
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
        busy = false;
        dragging = false;

        // Rise off the stand and grow into the slot.
        SetGrab(true);
        Play(Travel(home, Facing(homeUp), Vector3.one * size, seconds, size * 0.6f));
    }

    public void ReturnToStand(float seconds)
    {
        lifted = false;
        dragging = false;
        busy = false;
        SetGrab(false);
        HideGhost();
        Play(Travel(restPosition, restRotation, Vector3.one * restSize, seconds, size * 0.4f));
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
        dragging = true;
        dragTarget = transform.position;
        dragRotation = transform.rotation;
        pressTilt = Random.Range(-9f, 9f);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySelectSFX();
        StartCoroutine(Pulse(1.12f, 0.12f));
        return true;
    }

    public void DragTo(Vector2 screen)
    {
        if (!dragging) return;
        if (owner.TryPaperPoint(screen, out Vector2 local, out Vector3 world, out Vector3 normal, out bool onFront))
        {
            // Held at an angle like a real hand stamp: the pad points at the
            // aim point (the ghost), the body leans up the screen so its side
            // and label show instead of only the top of the grip.
            Vector3 axis = Tilted(normal);
            dragTarget = world + axis * (size * (FaceOffset + AimHover));
            dragRotation = Facing(axis);
            if (onFront) ShowGhost(local);
            else HideGhost();
            return;
        }

        Camera view = Camera.main;
        if (view == null) return;
        Ray ray = view.ScreenPointToRay(screen);
        float depth = Vector3.Dot(home - view.transform.position, view.transform.forward);
        dragTarget = ray.GetPoint(depth / Mathf.Max(0.1f, Vector3.Dot(ray.direction, view.transform.forward)));
        dragRotation = Facing(homeUp);
        HideGhost();
    }

    public void EndDrag(Vector2 screen)
    {
        if (!dragging) return;
        dragging = false;
        bool onPaper = owner.TryPaperPoint(screen, out Vector2 local, out Vector3 world, out Vector3 normal, out bool onFront);
        HideGhost();
        if (onPaper && onFront)
        {
            Play(Press(local, world, normal));
            return;
        }
        if (onPaper && !onFront) Level2Say.Line("Stamp the front page. Turn back to page 1 first.");
        Play(Travel(home, Facing(homeUp), Vector3.one * size, HomeSeconds, size * 0.2f));
    }

    private void LateUpdate()
    {
        if (dragging)
        {
            float k = 1f - Mathf.Exp(-FollowSharpness * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, dragTarget, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, dragRotation, k);
        }
        else if (lifted && !busy && !moving)
        {
            Camera view = Camera.main;
            Vector3 screenUp = view != null ? view.transform.up : Vector3.up;
            transform.position = home + screenUp * (Mathf.Sin(Time.unscaledTime * 2.2f + bobPhase) * size * 0.04f);
        }
    }

    // =========================================================
    // PRESS
    // =========================================================

    private IEnumerator Press(Vector2 local, Vector3 world, Vector3 normal)
    {
        busy = true;
        Quaternion straight = Facing(normal);
        Vector3 above = world + normal * (size * (FaceOffset + HoverFraction));
        Vector3 down = world + normal * (size * FaceOffset * 0.97f);

        yield return Tween(transform.position, above, transform.rotation, straight, AlignSeconds, 0f, easeIn: false);
        yield return Tween(above, down, straight, straight, SlamSeconds, 0f, easeIn: true);

        // Impact. The decision is wrapped so the stamp always finishes its motion.
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPaper2SFX();
        StartCoroutine(Pulse(0.86f, 0.14f));
        if (owner.Folder != null) StartCoroutine(Thump(owner.Folder.PaperRect));
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
        if (outcome != PreliminaryReview.Outcome.Recorded && owner.Folder != null)
        {
            GameObject faint = ReviewInkMark.CreateAt(owner.Folder.PaperRect, Level2DeskStamps.InkTextFor(type), local, pressTilt);
            ReviewInkMark.SetStyle(faint, Level2DeskStamps.InkColorFor(type), 0.6f);
            ReviewInkMotion.Play(faint, ReviewInkMotion.Mode.FadeOut);
        }

        yield return WaitReal(0.05f);
        yield return Tween(down, above + normal * size * 0.3f, straight, straight, ReboundSeconds, 0f, easeIn: false);
        yield return Tween(transform.position, home, transform.rotation, Facing(homeUp), HomeSeconds, size * 0.25f, easeIn: false);
        busy = false;
        motion = null;

        // A stamp that counted ends the stamping: both stamps go back on the stand.
        if (outcome == PreliminaryReview.Outcome.Recorded && owner != null) owner.PutAwaySoon(0.6f);
    }

    // =========================================================
    // GHOST
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
        }
        var rect = (RectTransform)ghost.transform;
        rect.anchoredPosition = local - paper.rect.center;
        rect.localRotation = Quaternion.Euler(0f, 0f, pressTilt);
        ghost.transform.SetAsLastSibling();
    }

    private void HideGhost()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
    }

    // =========================================================
    // MOTION HELPERS (unscaled time)
    // =========================================================

    private IEnumerator Travel(Vector3 target, Quaternion rotation, Vector3 scale, float seconds, float arc)
    {
        moving = true;
        Vector3 from = transform.position, fromScale = transform.localScale;
        Quaternion fromRot = transform.rotation;
        Camera view = Camera.main;
        Vector3 arcUp = view != null ? view.transform.up : Vector3.up;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds))
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            transform.position = Vector3.Lerp(from, target, e) + arcUp * (Mathf.Sin(e * Mathf.PI) * arc);
            transform.rotation = Quaternion.Slerp(fromRot, rotation, e);
            transform.localScale = Vector3.Lerp(fromScale, scale, e);
            yield return null;
        }
        transform.position = target;
        transform.rotation = rotation;
        transform.localScale = scale;
        moving = false;
        motion = null;
    }

    private IEnumerator Tween(Vector3 from, Vector3 to, Quaternion fromRot, Quaternion toRot, float seconds, float arc, bool easeIn)
    {
        Camera view = Camera.main;
        Vector3 arcUp = view != null ? view.transform.up : Vector3.up;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds))
        {
            float e = easeIn ? t * t : 1f - (1f - t) * (1f - t);
            transform.position = Vector3.Lerp(from, to, e) + arcUp * (Mathf.Sin(e * Mathf.PI) * arc);
            transform.rotation = Quaternion.Slerp(fromRot, toRot, e);
            yield return null;
        }
        transform.position = to;
        transform.rotation = toRot;
    }

    private static IEnumerator WaitReal(float seconds)
    {
        float until = Time.unscaledTime + seconds;
        while (Time.unscaledTime < until) yield return null;
    }

    /// <summary>Quick uniform scale pulse to <paramref name="peak"/> and back (pick-up pop, impact squash).</summary>
    private IEnumerator Pulse(float peak, float seconds)
    {
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
        {
            if (!lifted) yield break;
            float k = t < 0.5f ? Mathf.Lerp(1f, peak, t * 2f) : Mathf.Lerp(peak, 1f, (t - 0.5f) * 2f);
            transform.localScale = Vector3.one * size * k;
            yield return null;
        }
        if (lifted) transform.localScale = Vector3.one * size;
    }

    /// <summary>The paper dips a little under the impact, then settles.</summary>
    private static IEnumerator Thump(RectTransform paper)
    {
        if (paper == null) yield break;
        Vector3 rest = paper.localScale;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.14f)
        {
            paper.localScale = rest * Mathf.Lerp(0.985f, 1f, t);
            yield return null;
        }
        paper.localScale = rest;
    }

    private void Play(IEnumerator routine)
    {
        if (motion != null) StopCoroutine(motion);
        motion = StartCoroutine(routine);
    }

    /// <summary>Handle along <paramref name="up"/>, printed face turned toward the camera.</summary>
    private static Quaternion Facing(Vector3 up)
    {
        Camera view = Camera.main;
        up = up.sqrMagnitude > 1e-6f ? up.normalized : Vector3.up;
        Vector3 forward = view != null ? -Vector3.ProjectOnPlane(view.transform.forward, up) : Vector3.forward;
        if (forward.sqrMagnitude < 1e-4f) forward = Vector3.ProjectOnPlane(view != null ? view.transform.up : Vector3.forward, up);
        // Label faces local -Z, so local +Z points away from the camera.
        return Quaternion.LookRotation(-forward.normalized, up);
    }

    /// <summary>The paper normal tipped toward the top of the screen, so the stamp's block shows while dragged.</summary>
    private static Vector3 Tilted(Vector3 normal)
    {
        Camera view = Camera.main;
        if (view == null) return normal;
        Vector3 screenUpOnPaper = Vector3.ProjectOnPlane(view.transform.up, normal).normalized;
        return Quaternion.AngleAxis(DragTilt, Vector3.Cross(normal, screenUpOnPaper)) * normal;
    }

    private void SetGrab(bool on)
    {
        if (grab != null) grab.enabled = on;
    }

    private void OnDisable()
    {
        dragging = false;
        busy = false;
        moving = false;
        HideGhost();
        if (motion != null) StopCoroutine(motion);
        motion = null;
    }

    private void OnDestroy()
    {
        // Level 2 removed: the desk's own stamp model comes back.
        if (standModel != null && !standModel.activeSelf) standModel.SetActive(true);
        foreach (Material material in materials)
            if (material != null) Destroy(material);
        materials.Clear();
    }
}
