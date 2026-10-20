using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// The office is literally shut until Side Quest 1 is done. When rewards.json
/// says requireSideQuestBeforeOffice, the entrance door leaves (officeDoors,
/// default Office_Entrance_Door_1 and _2) swing shut toward each other and an
/// invisible wall fills the doorway. Walking up to it says "Finish Side Quest
/// 1 first". When the side quest completes (LevelGate.SideQuestDone) the
/// doors swing back to the pose they have in the scene and the wall goes.
///
/// Nothing in the scene is rewritten: the closed pose is worked out at run
/// time (each leaf turns on its hinge until its free edge points at the other
/// leaf's hinge). If the named doors are missing, the outermost door pair
/// under Reception/Doors is used and a warning names it.
/// </summary>
public sealed class OfficeDoorGate : MonoBehaviour
{
    /// <summary>Waiting = the level has not said yet whether the office is locked.</summary>
    public enum DoorPhase { Waiting, Closed, Open }

    private const string TargetSceneName = "Office_Level1";
    private const string FallbackDoorsPath = "Environment/OfficeBuilding/Rooms/Reception/Doors";
    private const string InsidePath = "Environment/OfficeBuilding/Rooms/Reception/Floor";
    private const string LockedMessage = "The office is closed. Finish Side Quest 1 first";
    private const float SwingSeconds = 1.1f;
    private const float WarnDistance = 2.2f;
    private const float WarnInterval = 6f;
    private const float WaitForLevelSeconds = 3f;

    private sealed class Leaf
    {
        public Transform transform;
        public Quaternion openLocal;
        public Quaternion closedLocal;
    }

    private readonly List<Leaf> leaves = new List<Leaf>();
    private BoxCollider barrier;
    private readonly EnumStateMachine<DoorPhase> phase = new EnumStateMachine<DoorPhase>("OfficeDoorGate", DoorPhase.Waiting)
        .Allow(DoorPhase.Waiting, DoorPhase.Closed, DoorPhase.Open)
        .Allow(DoorPhase.Closed, DoorPhase.Open)
        .Allow(DoorPhase.Open, DoorPhase.Closed);
    private bool closed => phase.Is(DoorPhase.Closed);
    private float swing = 1f;          // 0..1 progress of the current swing
    private float nextWarn;
    private float startTime;

    /// <summary>True while the entrance is shut (for tests and other systems).</summary>
    public static bool IsClosed { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeForLoadedScenes()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            EnsureForScene(SceneManager.GetSceneAt(i));
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureForScene(scene);

    private static void EnsureForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != TargetSceneName) return;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<OfficeDoorGate>(true) != null) return;
        var host = new GameObject("Office Entrance Gate");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<OfficeDoorGate>();
    }

    private void Start()
    {
        startTime = Time.unscaledTime;
        IsClosed = false;
    }

    private void OnDestroy()
    {
        IsClosed = false;
    }

    private void Update()
    {
        if (phase.Is(DoorPhase.Waiting))
        {
            // Wait for the level to read rewards.json (CaseProgressionManager.StartLevel).
            if (!LevelGate.Configured && Time.unscaledTime - startTime < WaitForLevelSeconds) return;
            Setup();
            phase.TryChange(LevelGate.CaseAllowed ? DoorPhase.Open : DoorPhase.Closed);
            swing = 1f;
            Apply(1f);
            return;
        }

        DoorPhase wanted = LevelGate.CaseAllowed ? DoorPhase.Open : DoorPhase.Closed;
        if (!phase.Is(wanted) && phase.TryChange(wanted))
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayEffect(wanted == DoorPhase.Open ? "sfx_door_open_01" : "sfx_door_close_01");
            swing = 1f - swing;   // reverse smoothly from wherever it is
        }

        if (swing < 1f)
        {
            swing = Mathf.Min(1f, swing + Time.deltaTime / SwingSeconds);
            Apply(swing);
        }

        if (closed) WarnIfNear();
    }

    // =========================================================
    // SETUP
    // =========================================================

    private void Setup()
    {
        List<Transform> found = FindNamedDoors();
        if (found.Count == 0)
        {
            found = FindFallbackPair();
            if (found.Count > 0)
                Debug.LogWarning("[OfficeDoorGate] No objects named " + string.Join(", ", LevelGate.OfficeDoors) +
                                 "; using " + string.Join(" + ", found.ConvertAll(t => t.name)) +
                                 ". Rename the entrance door leaves (or edit officeDoors in rewards.json).");
        }
        if (found.Count == 0)
        {
            Debug.LogWarning("[OfficeDoorGate] No office entrance doors found; the office is not gated by doors.");
            return;
        }

        Vector3 inside = InsidePoint(found);
        for (int i = 0; i < found.Count; i++)
        {
            Transform door = found[i];
            Transform partner = found.Count > 1 ? found[(i + 1) % found.Count] : null;
            leaves.Add(BuildLeaf(door, partner, inside));
        }
        BuildBarrier(found);
    }

    private Leaf BuildLeaf(Transform door, Transform partner, Vector3 inside)
    {
        var leaf = new Leaf { transform = door, openLocal = door.localRotation, closedLocal = door.localRotation };
        Vector3 hinge = door.position;
        Vector3 edge = Flat(LeafCenter(door) - hinge);
        if (edge.sqrMagnitude < 0.0001f) return leaf;

        Quaternion world = door.rotation;
        Quaternion closedWorld = world;
        if (partner != null)
        {
            Vector3 toPartner = Flat(partner.position - hinge);
            if (toPartner.sqrMagnitude > 0.0001f)
                closedWorld = Quaternion.AngleAxis(Vector3.SignedAngle(edge, toPartner, Vector3.up), Vector3.up) * world;
        }

        // The scene pose is the open pose, unless the door is already nearly
        // shut there: then it opens 90 degrees into the office.
        Vector3 closedEdge = Flat(closedWorld * Quaternion.Inverse(world) * edge);
        float openAngle = Vector3.Angle(edge, closedEdge);
        Quaternion openWorld = world;
        if (openAngle < 20f)
        {
            Quaternion a = Quaternion.AngleAxis(90f, Vector3.up) * closedWorld;
            Quaternion b = Quaternion.AngleAxis(-90f, Vector3.up) * closedWorld;
            Vector3 tipA = hinge + a * Quaternion.Inverse(world) * edge;
            Vector3 tipB = hinge + b * Quaternion.Inverse(world) * edge;
            openWorld = (tipA - inside).sqrMagnitude < (tipB - inside).sqrMagnitude ? a : b;
        }

        Quaternion parent = door.parent != null ? door.parent.rotation : Quaternion.identity;
        leaf.closedLocal = Quaternion.Inverse(parent) * closedWorld;
        leaf.openLocal = Quaternion.Inverse(parent) * openWorld;
        return leaf;
    }

    private void BuildBarrier(List<Transform> doors)
    {
        Vector3 a = doors[0].position;
        Vector3 b = doors.Count > 1 ? doors[1].position : a + Flat(LeafCenter(doors[0]) - a) * 2f;
        Vector3 across = Flat(b - a);
        float width = across.magnitude;
        if (width < 0.3f) width = 1.8f;

        float height = 2.6f;
        if (TryBounds(doors[0], out Bounds bounds)) height = Mathf.Max(2f, bounds.size.y + 0.2f);

        // Not named "door" on purpose: the guide's path finder walks through
        // anything called door, but it must treat a shut entrance as a wall.
        var go = new GameObject("Office Entrance Barrier");
        go.transform.SetParent(transform, false);
        go.layer = 2;   // Ignore Raycast: blocks bodies, not interaction rays
        go.transform.position = (a + b) * 0.5f + Vector3.up * (height * 0.5f);
        go.transform.rotation = Quaternion.LookRotation(Vector3.Cross(across.normalized, Vector3.up), Vector3.up);
        barrier = go.AddComponent<BoxCollider>();
        barrier.size = new Vector3(width + 0.3f, height, 0.4f);
    }

    // =========================================================
    // RUNTIME
    // =========================================================

    private void Apply(float t)
    {
        float k = Mathf.SmoothStep(0f, 1f, t);
        foreach (Leaf leaf in leaves)
        {
            if (leaf.transform == null) continue;
            leaf.transform.localRotation = closed
                ? Quaternion.Slerp(leaf.openLocal, leaf.closedLocal, k)
                : Quaternion.Slerp(leaf.closedLocal, leaf.openLocal, k);
        }
        // The wall is up the whole time the doors are shut or shutting, and
        // drops as soon as they start to open.
        if (barrier != null) barrier.enabled = closed;
        IsClosed = closed && leaves.Count > 0;
    }

    private void WarnIfNear()
    {
        if (barrier == null || Time.unscaledTime < nextWarn) return;
        Transform player = CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;
        if (player == null) return;
        Vector3 closest = barrier.ClosestPoint(player.position);
        if (Flat(closest - player.position).magnitude > WarnDistance) return;
        nextWarn = Time.unscaledTime + WarnInterval;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("sfx_door_locked_01");
        LevelGate.NotifyBlocked(LockedMessage);
    }

    // =========================================================
    // FINDING THE DOORS
    // =========================================================

    private List<Transform> FindNamedDoors()
    {
        var result = new List<Transform>();
        string[] names = LevelGate.OfficeDoors;
        if (names == null || names.Length == 0) return result;
        var all = AllTransforms();
        foreach (string name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            Transform match = all.Find(t => t.name == name);
            if (match != null && !result.Contains(match)) result.Add(match);
        }
        return result;
    }

    /// <summary>The door pair under Reception/Doors farthest from the reception floor (the way in from outside).</summary>
    private List<Transform> FindFallbackPair()
    {
        var result = new List<Transform>();
        Transform doors = FindPath(FallbackDoorsPath);
        if (doors == null) return result;

        var leafList = new List<Transform>();
        foreach (Transform child in doors)
        {
            string n = child.name;
            if (n.IndexOf("Door", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (n.IndexOf("Wall", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
            if (n.IndexOf("Frame", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
            leafList.Add(child);
        }
        if (leafList.Count == 0) return result;

        Vector3 inside = InsidePoint(leafList);
        float best = -1f;
        for (int i = 0; i < leafList.Count; i++)
        {
            // Pair each leaf with its nearest neighbour.
            Transform partner = null;
            float near = float.MaxValue;
            for (int j = 0; j < leafList.Count; j++)
            {
                if (i == j) continue;
                float d = (leafList[i].position - leafList[j].position).sqrMagnitude;
                if (d < near) { near = d; partner = leafList[j]; }
            }
            if (partner == null || near > 9f) continue;
            float far = Flat((leafList[i].position + partner.position) * 0.5f - inside).sqrMagnitude;
            if (far > best)
            {
                best = far;
                result.Clear();
                result.Add(leafList[i]);
                result.Add(partner);
            }
        }
        return result;
    }

    private Vector3 InsidePoint(List<Transform> doors)
    {
        Transform floor = FindPath(InsidePath);
        if (floor != null && TryBounds(floor, out Bounds b)) return b.center;
        Vector3 mid = Vector3.zero;
        foreach (Transform d in doors) mid += d.position;
        return mid / Mathf.Max(1, doors.Count);
    }

    private List<Transform> AllTransforms()
    {
        var list = new List<Transform>();
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            list.AddRange(root.GetComponentsInChildren<Transform>(true));
        return list;
    }

    private Transform FindPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        string[] parts = path.Split('/');
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            if (root.name != parts[0]) continue;
            Transform t = root.transform;
            for (int i = 1; i < parts.Length && t != null; i++) t = t.Find(parts[i]);
            if (t != null) return t;
        }
        return null;
    }

    private static Vector3 LeafCenter(Transform door)
    {
        return TryBounds(door, out Bounds b) ? b.center : door.position;
    }

    private static bool TryBounds(Transform t, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
        {
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any;
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
}
