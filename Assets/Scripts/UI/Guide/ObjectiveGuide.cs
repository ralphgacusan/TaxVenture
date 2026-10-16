using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Guides the player to the CURRENT objective step for the whole level:
/// - picks the goal from guide.json "waypoints" (keyed by the step's
///   objectives.json trigger), e.g. the nearest plaza client still waiting
///   to talk, the receptionist, the desk, the Auditor;
/// - finds a walkable route (GuideGridPath) and draws flowing arrows on the
///   ground (ObjectiveTrail);
/// - shows a floating goal marker (ObjectiveBeacon);
/// - publishes the goal and route so the minimap can pin and pulse it
///   (OfficeMinimapObjective).
/// Hidden during conversations, the desk/corkboard views and result cards.
/// Read-only: it never changes objectives or NPC state.
/// </summary>
public sealed class ObjectiveGuide : MonoBehaviour
{
    private const float PickInterval = 0.5f;
    private const float PathInterval = 0.6f;

    public static ObjectiveGuide Instance { get; private set; }

    private GuideScript script;
    private GuideTargets targets;
    private GuideGridPath grid;
    private ObjectiveTrail trail;
    private ObjectiveBeacon beacon;

    private string stepKey;
    private GuideWaypoint waypoint;
    private Transform target;
    private Vector3 areaCenter;
    private bool areaTarget;
    private float nextPick;
    private float nextPath;
    private Vector3 pathFromPlayer;
    private Vector3 pathToTarget;
    private readonly List<Vector3> path = new List<Vector3>();
    private readonly List<Vector3> pathBuffer = new List<Vector3>();
    private readonly List<Renderer> renderers = new List<Renderer>();

    /// <summary>True when there is a goal for the current step.</summary>
    public bool HasTarget { get; private set; }
    /// <summary>Ground position of the goal.</summary>
    public Vector3 TargetPosition { get; private set; }
    /// <summary>Top of the goal (for the floating marker).</summary>
    public Vector3 TargetTop { get; private set; }
    public string Label { get; private set; }
    /// <summary>The goal object (null for an area goal).</summary>
    public Transform TargetTransform => target;
    /// <summary>Changes every time the objective step (and so the goal) changes.</summary>
    public int Version { get; private set; }
    /// <summary>True while arrows are shown (goal exists, player free and not there yet).</summary>
    public bool IsGuiding { get; private set; }
    /// <summary>Horizontal distance from the player to the goal.</summary>
    public float Distance { get; private set; }
    /// <summary>Route from the player to the goal (may be partial).</summary>
    public IReadOnlyList<Vector3> Route => path;
    public bool MinimapEnabled => script != null && script.trail.minimap && Revealed;
    /// <summary>False until the guide introduces the arrows (trail.waitForGuide).</summary>
    public bool Revealed { get; private set; }

    /// <summary>Shows the arrows, marker and minimap pin from now on (with a minimap pop).</summary>
    public void Reveal()
    {
        if (Revealed) return;
        Revealed = true;
        Version++;
        if (beacon != null) beacon.Flash();
    }
    public float ReminderSeconds => script != null ? script.trail.minimapReminderSeconds : 0f;
    public Color Accent => script != null ? script.trail.ParsedColor : (Color)new Color32(100, 215, 255, 255);

    public static ObjectiveGuide Create(GameObject host, GuideScript script, RectTransform uiParent)
    {
        var guide = host.AddComponent<ObjectiveGuide>();
        guide.script = script;
        guide.Revealed = !script.trail.waitForGuide;
        guide.targets = new GuideTargets(host.scene);
        guide.grid = new GuideGridPath();
        guide.trail = ObjectiveTrail.Create(host.transform);
        guide.trail.Spacing = Mathf.Max(0.4f, script.trail.spacing);
        guide.trail.Length = Mathf.Max(4f, script.trail.length);
        guide.trail.Speed = script.trail.speed;
        guide.trail.Color = script.trail.ParsedColor;
        if (script.trail.beacon)
        {
            guide.beacon = ObjectiveBeacon.Create(uiParent);
            guide.beacon.SetAccent(script.trail.ParsedColor);
        }
        return guide;
    }

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (trail != null) Destroy(trail.gameObject);
        if (beacon != null) Destroy(beacon.gameObject);
    }

    private void LateUpdate()
    {
        float now = Time.unscaledTime;
        UpdateStep();
        if (now >= nextPick)
        {
            nextPick = now + PickInterval;
            PickTarget();
        }

        Transform player = GuideTargets.Player;
        if (!HasTarget || player == null || !Revealed)
        {
            IsGuiding = false;
            trail.SetVisible(false);
            if (beacon != null) beacon.Hide();
            return;
        }

        Vector3 flat = TargetPosition - player.position;
        flat.y = 0f;
        Distance = flat.magnitude;
        float arrive = waypoint != null && waypoint.arriveDistance > 0f ? waypoint.arriveDistance : script.trail.arriveDistance;
        bool free = PlayerIsFree();
        IsGuiding = free && Distance > arrive;

        if (script.trail.enabled && IsGuiding && now >= nextPath &&
            ((player.position - pathFromPlayer).sqrMagnitude > 1f || (TargetPosition - pathToTarget).sqrMagnitude > 1f || path.Count == 0))
        {
            nextPath = now + PathInterval;
            RebuildPath(player);
        }
        else if (path.Count > 0)
        {
            // Keep the first point under the player between rebuilds.
            path[0] = new Vector3(player.position.x, path[0].y, player.position.z);
        }

        trail.SetRoute(path);
        trail.SetVisible(script.trail.enabled && IsGuiding);

        Camera cam = GuideTargets.ViewCamera;
        if (beacon != null)
        {
            if (free && cam != null) beacon.Tick(cam, TargetTop, Distance, Label);
            else beacon.Hide();
        }
    }

    private static bool PlayerIsFree()
    {
        CameraController controller = CameraController.Instance;
        if (controller == null || controller.CurrentMode != CameraController.CameraMode.FirstPerson) return false;
        if (DialogueUI.IsAnyDialogueActive || HudRewardScreen.AnyOpen || Time.timeScale <= 0f) return false;
        if (CaseResultPopupUI.Instance != null && CaseResultPopupUI.Instance.IsVisible) return false;
        OfficeHudRuntime hud = OfficeHudRuntime.Instance;
        return hud == null || hud.IsShown;
    }

    private void UpdateStep()
    {
        OfficeObjectives objectives = OfficeObjectives.Instance;
        string key = null;
        if (objectives != null && objectives.Snapshot != null && objectives.Snapshot.ActiveIndex >= 0)
            key = ObjectiveTriggers.Normalize(objectives.Tasks[objectives.Snapshot.ActiveIndex].TriggerKey);
        if (key == stepKey) return;

        stepKey = key;
        waypoint = script.FindWaypoint(key);
        areaTarget = false;
        if (waypoint == null && key != null && ObjectiveTriggers.TryGetArea(key, out _) &&
            OfficeObjectives.ResolveArea(gameObject.scene, key, out Bounds area))
        {
            areaTarget = true;
            areaCenter = new Vector3(area.center.x, area.min.y, area.center.z);
        }
        target = null;
        path.Clear();
        nextPick = 0f;
        nextPath = 0f;
        Version++;
        if (beacon != null) beacon.Flash();
    }

    private void PickTarget()
    {
        Label = waypoint != null ? waypoint.label : null;
        if (areaTarget)
        {
            HasTarget = true;
            TargetPosition = areaCenter;
            TargetTop = areaCenter + Vector3.up * 2.2f;
            return;
        }
        if (waypoint == null || string.IsNullOrWhiteSpace(waypoint.target))
        {
            HasTarget = false;
            return;
        }

        Transform player = GuideTargets.Player;
        Vector3 from = player != null ? player.position : Vector3.zero;
        string pick = string.IsNullOrEmpty(waypoint.pick) ? "nearestAvailable" : waypoint.pick;
        bool first = string.Equals(pick, "first", System.StringComparison.OrdinalIgnoreCase);
        bool needUseful = string.Equals(pick, "nearestAvailable", System.StringComparison.OrdinalIgnoreCase);

        Transform best = null, bestAny = null;
        float bestD = float.MaxValue, bestAnyD = float.MaxValue;
        string[] tokens = waypoint.target.Split('|');
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i].Trim();
            if (token.Length == 0) continue;
            if (token.StartsWith("type:", System.StringComparison.Ordinal))
            {
                System.Type type = GuideTargets.FindType(token.Substring(5));
                if (type == null) continue;
                Object[] found = FindObjectsByType(type, FindObjectsInactive.Exclude);
                for (int j = 0; j < found.Length; j++)
                    if (found[j] is Component c && c.gameObject.scene == gameObject.scene)
                        Consider(c.transform, GuideTargets.IsUseful(c), from, ref best, ref bestD, ref bestAny, ref bestAnyD);
            }
            else
            {
                Transform t = targets.FindInScene(token);
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                INpcAvailabilitySource npc = t.GetComponent<INpcAvailabilitySource>();
                bool useful = npc == null || npc.Availability == NpcAvailability.Available;
                Consider(t, useful, from, ref best, ref bestD, ref bestAny, ref bestAnyD);
            }
            if (first && (best != null || bestAny != null)) break;
        }

        target = needUseful ? (best != null ? best : bestAny) : (bestAny != null ? bestAny : best);
        if (first) target = best != null ? best : bestAny;
        HasTarget = target != null;
        if (!HasTarget) return;

        // An object's pivot can be far from what you see (the Desk's pivot is
        // out in the corridor), so aim at the middle of its visible shape.
        if (TryBounds(target, out Bounds b))
        {
            TargetPosition = new Vector3(b.center.x, b.min.y, b.center.z);
            TargetTop = new Vector3(b.center.x, b.max.y + 0.75f, b.center.z);
        }
        else
        {
            TargetPosition = target.position;
            TargetTop = target.position + Vector3.up * 2.4f;
        }
    }

    private static void Consider(Transform t, bool useful, Vector3 from, ref Transform best, ref float bestD,
        ref Transform bestAny, ref float bestAnyD)
    {
        Vector3 d = t.position - from;
        d.y = 0f;
        float dist = d.sqrMagnitude;
        if (dist < bestAnyD)
        {
            bestAnyD = dist;
            bestAny = t;
        }
        if (useful && dist < bestD)
        {
            bestD = dist;
            best = t;
        }
    }

    private bool TryBounds(Transform t, out Bounds b)
    {
        renderers.Clear();
        t.GetComponentsInChildren(false, renderers);
        bool any = false;
        b = default;
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer r = renderers[i];
            if (!r.enabled || r is ParticleSystemRenderer || r.gameObject.layer == 5) continue;
            if (!any) { b = r.bounds; any = true; }
            else b.Encapsulate(r.bounds);
        }
        return any;
    }

    private readonly RaycastHit[] groundHits = new RaycastHit[8];

    /// <summary>Floor height under the player (their pivot may be at the body's centre).</summary>
    private float GroundBelow(Transform player)
    {
        int n = Physics.RaycastNonAlloc(player.position + Vector3.up * 0.5f, Vector3.down, groundHits, 4f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float best = float.MinValue;
        for (int i = 0; i < n; i++)
            if (!groundHits[i].transform.IsChildOf(player) && groundHits[i].point.y > best) best = groundHits[i].point.y;
        return best > float.MinValue ? best : player.position.y;
    }

    private void RebuildPath(Transform player)
    {
        pathFromPlayer = player.position;
        pathToTarget = TargetPosition;
        Vector3 feet = new Vector3(player.position.x, GroundBelow(player), player.position.z);
        bool reached = grid.FindPath(feet, TargetPosition, player, pathBuffer);
        path.Clear();
        if (pathBuffer.Count >= 2)
        {
            path.AddRange(pathBuffer);
            path[0] = new Vector3(player.position.x, path[0].y, player.position.z);
            if (!reached)
            {
                // Partial route: finish with a straight hint toward the goal.
                Vector3 last = path[path.Count - 1];
                Vector3 rest = TargetPosition - last;
                rest.y = 0f;
                if (rest.magnitude > 1f) path.Add(last + Vector3.ClampMagnitude(rest, 4f));
            }
        }
        else
        {
            // No route found (yet): a short straight hint toward the goal.
            float groundY = pathBuffer.Count > 0 ? pathBuffer[0].y : GroundBelow(player);
            Vector3 start = new Vector3(player.position.x, groundY, player.position.z);
            Vector3 dir = TargetPosition - start;
            dir.y = 0f;
            path.Add(start);
            path.Add(start + Vector3.ClampMagnitude(dir, 6f));
        }
    }
}
