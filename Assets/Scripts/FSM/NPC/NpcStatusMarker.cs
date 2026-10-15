using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// T04: floating marker above an NPC that tells the truth about its conversation.
///
///   Available → amber downward triangle (gently pulsing)
///   Completed → green check mark (still)
///   Unavailable, during any conversation, in another camera mode, out of range
///   or occluded → nothing
///
/// Shape differs as well as colour. State comes only from the NPC's own
/// interactable (INpcAvailabilitySource); this component never decides quest
/// rules. It refreshes on NPC FSM changes, dialogue start/end and a light poll,
/// so it also syncs on enable and scene reload.
///
/// Rendering: shared procedural meshes and one shared material with per-marker
/// MaterialPropertyBlock colours (no per-frame allocation or material
/// instancing). Children are on Ignore Raycast with no colliders, no shadows,
/// so markers can never take clicks, taps or joystick input.
/// </summary>
[DisallowMultipleComponent]
public class NpcStatusMarker : MonoBehaviour
{
    [Header("Source (defaults to the interactable on this object)")]
    [SerializeField] private MonoBehaviour availabilitySource;

    [Header("Look")]
    [SerializeField] private Material markerMaterial;
    [SerializeField] private Color availableColor = new Color(1f, 0.74f, 0.18f, 1f);
    [SerializeField] private Color completedColor = new Color(0.35f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color outlineColor = new Color(0.08f, 0.09f, 0.12f, 1f);
    [Tooltip("Marker size as a fraction of the NPC's height.")]
    [SerializeField, Range(0.04f, 0.3f)] private float sizeFraction = 0.11f;
    [Tooltip("Gap above the top of the NPC's head, as a fraction of height.")]
    [SerializeField, Range(0f, 0.5f)] private float heightGapFraction = 0.12f;

    [Header("Motion (restrained)")]
    [SerializeField, Range(0f, 0.5f)] private float bobAmplitude = 0.12f;
    [SerializeField, Range(0f, 3f)] private float bobFrequency = 0.6f;
    [SerializeField, Range(0f, 0.2f)] private float pulseAmount = 0.06f;
    [SerializeField, Range(0f, 3f)] private float pulseFrequency = 1.1f;
    [SerializeField, Range(1f, 30f)] private float appearSpeed = 8f;

    [Header("Visibility")]
    [Tooltip("Hide beyond this distance (fraction of NPC height).")]
    [SerializeField, Range(2f, 40f)] private float maxDistanceFraction = 10f;
    [Tooltip("Hide when the camera is this close (fraction of NPC height): the marker would sit overhead.")]
    [SerializeField, Range(0f, 2f)] private float minDistanceFraction = 0.35f;
    [SerializeField] private bool hideWhenOccluded = true;
    [SerializeField] private LayerMask occlusionMask = Physics.DefaultRaycastLayers;
    [SerializeField, Range(0.05f, 1f)] private float pollInterval = 0.2f;

    private const int IgnoreRaycastLayer = 2;

    private static Mesh triangleFill, triangleOutline, checkFill, checkOutline;
    private static Material fallbackMaterial;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly RaycastHit[] hits = new RaycastHit[8];

    private INpcAvailabilitySource source;
    private NpcStateMachine stateMachine;
    private HighlightEffect bodyOutline;
    private DialogueUI dialogueUI;
    private Transform markerRoot;
    private MeshFilter fillFilter, outlineFilter;
    private MeshRenderer fillRenderer, outlineRenderer;
    private MaterialPropertyBlock fillBlock, outlineBlock;

    private bool anchored;
    private Vector3 anchorOffset;
    private Transform ignoreRoot;
    private float npcHeight = 1.7f;

    private NpcAvailability availability;
    private NpcMarkerShape targetShape;
    private NpcMarkerShape drawnShape;
    private bool occluded;
    private float scale;
    private float clock;
    private float nextPoll;

    public NpcAvailability CurrentAvailability => availability;
    public NpcMarkerShape CurrentShape => drawnShape;

    private void Awake()
    {
        source = availabilitySource as INpcAvailabilitySource;
        if (source == null)
            foreach (MonoBehaviour candidate in GetComponents<MonoBehaviour>())
                if (candidate is INpcAvailabilitySource found) { source = found; break; }
        stateMachine = GetComponent<NpcStateMachine>();
        bodyOutline = HighlightEffect.For(this);
        BuildMarker();
    }

    private void OnEnable()
    {
        if (stateMachine != null) stateMachine.OnStateChanged += HandleNpcStateChanged;
        dialogueUI = dialogueUI != null ? dialogueUI : FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);
        if (dialogueUI != null)
        {
            dialogueUI.DialogueStarted += HandleDialogueChanged;
            dialogueUI.DialogueEnded += HandleDialogueEnded;
        }
        anchored = false;
        scale = 0f;
        drawnShape = NpcMarkerShape.None;
        Refresh(true);
    }

    private void OnDisable()
    {
        if (bodyOutline != null) bodyOutline.SetAffordance(false);
        if (stateMachine != null) stateMachine.OnStateChanged -= HandleNpcStateChanged;
        if (dialogueUI != null)
        {
            dialogueUI.DialogueStarted -= HandleDialogueChanged;
            dialogueUI.DialogueEnded -= HandleDialogueEnded;
        }
        if (markerRoot != null) markerRoot.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (markerRoot != null) Destroy(markerRoot.gameObject);
    }

    private void HandleNpcStateChanged(INpcState state) => Refresh(false);
    private void HandleDialogueChanged() => Refresh(false);
    private void HandleDialogueEnded(DialogueEndReason reason) => Refresh(false);

    /// <summary>Keep the body outline and head marker attached to a newly spawned client model.</summary>
    public void RefreshVisualTargets()
    {
        if (bodyOutline != null) bodyOutline.RefreshTargets();
        anchored = false;
        Refresh(true);
    }

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        clock += dt;
        if (clock >= nextPoll) Refresh(true);

        Camera cam = MainCamera();
        if (cam == null || markerRoot == null)
        {
            if (bodyOutline != null) bodyOutline.SetAffordance(false);
            return;
        }

        // Evaluate the shape every frame from cached availability/occlusion so
        // conversations and camera modes hide markers immediately.
        targetShape = ResolveShape(cam);
        // A visible status marker identifies an interactive NPC even without pointer hover.
        if (bodyOutline != null) bodyOutline.SetAffordance(targetShape != NpcMarkerShape.None);

        float target = targetShape == NpcMarkerShape.None ? 0f : 1f;
        if (targetShape != NpcMarkerShape.None && targetShape != drawnShape)
        {
            // Swap shape only once fully hidden, so a change reads as a new marker.
            if (scale <= 0.001f) ApplyShape(targetShape);
            else target = 0f;
        }
        scale = Mathf.MoveTowards(scale, target, appearSpeed * dt);

        bool visible = scale > 0.001f && drawnShape != NpcMarkerShape.None;
        if (markerRoot.gameObject.activeSelf != visible) markerRoot.gameObject.SetActive(visible);
        if (!visible) return;

        float size = sizeFraction * npcHeight;
        float bob = NpcAvailabilityRules.Bob(clock, bobAmplitude, bobFrequency) * size;
        float pulse = NpcAvailabilityRules.Pulse(drawnShape, clock, pulseAmount, pulseFrequency);
        Vector3 position = transform.position + anchorOffset + Vector3.up * bob;
        Vector3 away = position - cam.transform.position;
        markerRoot.SetPositionAndRotation(position,
            away.sqrMagnitude > 1e-8f ? Quaternion.LookRotation(away, cam.transform.up) : Quaternion.identity);
        markerRoot.localScale = Vector3.one * (size * pulse * scale);
    }

    private NpcMarkerShape ResolveShape(Camera cam)
    {
        bool suppressed = DialogueUI.IsAnyDialogueActive ||
                          (CameraController.Instance != null &&
                           CameraController.Instance.CurrentMode != CameraController.CameraMode.FirstPerson);
        if (!anchored) return NpcMarkerShape.None;
        Vector3 anchor = transform.position + anchorOffset;
        float distance = Vector3.Distance(cam.transform.position, anchor);
        return NpcAvailabilityRules.Shape(availability, suppressed, distance,
            minDistanceFraction * npcHeight, maxDistanceFraction * npcHeight, occluded);
    }

    /// <summary>Re-reads availability, re-anchors if needed and refreshes occlusion.</summary>
    private void Refresh(bool poll)
    {
        if (poll) nextPoll = clock + pollInterval;
        availability = source != null ? source.Availability : NpcAvailability.Unavailable;
        if (!anchored && availability != NpcAvailability.Unavailable) TryAnchor();
        if (poll) occluded = hideWhenOccluded && anchored && IsOccluded();
    }

    private void TryAnchor()
    {
        if (!DialogueSpeakerFrames.TryResolve(this, out DialogueSpeakerFrame frame)) return;
        npcHeight = Mathf.Max(0.05f, frame.Height);
        ignoreRoot = frame.Root;
        Vector3 top = new Vector3(frame.Head.x, frame.Floor.y + npcHeight, frame.Head.z);
        anchorOffset = top + Vector3.up * (heightGapFraction * npcHeight + 0.5f * sizeFraction * npcHeight) - transform.position;
        anchored = true;
    }

    private bool IsOccluded()
    {
        Camera cam = MainCamera();
        if (cam == null) return false;
        Vector3 from = cam.transform.position;
        Vector3 to = transform.position + anchorOffset;
        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 1e-3f) return false;
        int count = Physics.RaycastNonAlloc(from, delta / distance, hits, distance, occlusionMask, QueryTriggerInteraction.Ignore);
        Transform player = CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;
        for (int i = 0; i < count; i++)
        {
            Transform t = hits[i].collider.transform;
            if (t.IsChildOf(transform) || (ignoreRoot != null && t.IsChildOf(ignoreRoot)) ||
                (player != null && t.IsChildOf(player)))
                continue;
            return true;
        }
        return false;
    }

    private static Camera MainCamera()
    {
        CameraController controller = CameraController.Instance;
        return controller != null && controller.MainCamera != null ? controller.MainCamera : Camera.main;
    }

    // ------------------------------------------------------------------
    // Visuals
    // ------------------------------------------------------------------

    private void BuildMarker()
    {
        EnsureMeshes();
        // Unparented so it inherits no NPC scale/rotation and never counts toward
        // the NPC's own bounds; it follows the NPC by position every frame.
        var root = new GameObject($"NpcStatusMarker ({name})") { layer = IgnoreRaycastLayer };
        markerRoot = root.transform;
        outlineFilter = CreatePart("Outline", 0f, out outlineRenderer);
        fillFilter = CreatePart("Fill", -0.04f, out fillRenderer); // toward the camera
        fillBlock = new MaterialPropertyBlock();
        outlineBlock = new MaterialPropertyBlock();
        SetColor(outlineRenderer, outlineBlock, outlineColor);
        root.SetActive(false);
    }

    private MeshFilter CreatePart(string partName, float depth, out MeshRenderer renderer)
    {
        var go = new GameObject(partName) { layer = IgnoreRaycastLayer };
        go.transform.SetParent(markerRoot, false);
        go.transform.localPosition = new Vector3(0f, 0f, depth);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = markerMaterial != null ? markerMaterial : FallbackMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        return filter;
    }

    private void ApplyShape(NpcMarkerShape shape)
    {
        drawnShape = shape;
        bool available = shape == NpcMarkerShape.Available;
        fillFilter.sharedMesh = available ? triangleFill : checkFill;
        outlineFilter.sharedMesh = available ? triangleOutline : checkOutline;
        SetColor(fillRenderer, fillBlock, available ? availableColor : completedColor);
    }

    private static void SetColor(Renderer renderer, MaterialPropertyBlock block, Color color)
    {
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        renderer.SetPropertyBlock(block);
    }

    private static Material FallbackMaterial()
    {
        if (fallbackMaterial != null) return fallbackMaterial;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        fallbackMaterial = new Material(shader) { name = "NpcStatusMarker (fallback)" };
        Debug.LogWarning("[NpcStatusMarker] No marker material assigned; using a runtime fallback. Run Tools > TaxVenture > Configure NPC Markers.");
        return fallbackMaterial;
    }

    // Shapes in the XY plane, unit size, double-sided.
    private static void EnsureMeshes()
    {
        if (triangleFill != null) return;
        Vector2[] tri = { new Vector2(-0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(0f, -0.48f) };
        triangleFill = Polygon("MarkerTriangleFill", tri, 1f);
        triangleOutline = Polygon("MarkerTriangleOutline", tri, 1.32f);
        Vector2[] check = { new Vector2(-0.44f, 0.02f), new Vector2(-0.12f, -0.32f), new Vector2(0.46f, 0.4f) };
        checkFill = Polyline("MarkerCheckFill", check, 0.2f);
        checkOutline = Polyline("MarkerCheckOutline", check, 0.36f);
    }

    private static Mesh Polygon(string meshName, Vector2[] points, float scaleAboutCentroid)
    {
        Vector2 c = Vector2.zero;
        foreach (Vector2 p in points) c += p;
        c /= points.Length;
        var vertices = new List<Vector3>();
        foreach (Vector2 p in points)
        {
            Vector2 s = c + (p - c) * scaleAboutCentroid;
            vertices.Add(new Vector3(s.x, s.y, 0f));
        }
        var triangles = new List<int>();
        for (int i = 1; i + 1 < points.Length; i++) AddDoubleSided(triangles, 0, i, i + 1);
        return Finish(meshName, vertices, triangles);
    }

    private static Mesh Polyline(string meshName, Vector2[] points, float thickness)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        float half = thickness * 0.5f;
        for (int i = 0; i + 1 < points.Length; i++)
        {
            Vector2 a = points[i], b = points[i + 1];
            Vector2 dir = (b - a).normalized;
            Vector2 n = new Vector2(-dir.y, dir.x) * half;
            // Extend each stroke by half its width so joints and ends are covered.
            a -= dir * half;
            b += dir * half;
            int v = vertices.Count;
            vertices.Add(a + n); vertices.Add(a - n); vertices.Add(b - n); vertices.Add(b + n);
            AddDoubleSided(triangles, v, v + 1, v + 2);
            AddDoubleSided(triangles, v, v + 2, v + 3);
        }
        return Finish(meshName, vertices, triangles);
    }

    private static void AddDoubleSided(List<int> triangles, int a, int b, int c)
    {
        triangles.Add(a); triangles.Add(b); triangles.Add(c);
        triangles.Add(a); triangles.Add(c); triangles.Add(b);
    }

    private static Mesh Finish(string meshName, List<Vector3> vertices, List<int> triangles)
    {
        var mesh = new Mesh { name = meshName, hideFlags = HideFlags.DontSave };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.UploadMeshData(true);
        return mesh;
    }
}
