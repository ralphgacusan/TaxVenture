using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Turns a guide.json target ("point", "to", "near:", "visible:") into
/// something on screen: a screen-pixel rectangle for the spotlight and the
/// hand, plus a world position for distance checks. Read-only: it only
/// looks at objects, it never changes them.
///
/// TARGETS:
///   hud:objective  hud:progress  hud:timer  hud:minimap  hud:notes
///   hud:folder  hud:profiles  hud:settings  hud:icons  hud:joystick
///   hud:look  hud:prompt  hud:exit
///   world:&lt;scene path&gt;      a 3D object (its renderers' outline)
///   type:&lt;ComponentName&gt;    the best active one (on screen, still useful)
///   ui:&lt;object name&gt;       any UI object with that name in the scene
///   screen:x,y[,w,h]        a point/area in screen fractions (0..1)
///   waypoint                the current objective goal (see ObjectiveGuide)
/// </summary>
public sealed class GuideTargets
{
    public struct Hit
    {
        public bool Found;
        /// <summary>True when ScreenRect is usable (on screen, in front of the camera).</summary>
        public bool OnScreen;
        /// <summary>Screen pixels, origin bottom-left.</summary>
        public Rect ScreenRect;
        public bool HasWorld;
        public Vector3 World;
    }

    public static readonly string[] HudNames =
    {
        "objective", "progress", "timer", "minimap", "notes", "folder", "profiles", "settings", "icons",
        "joystick", "look", "prompt", "exit", "card", "continue"
    };

    private const float CacheSeconds = 0.4f;

    private sealed class CacheEntry
    {
        public float Time;
        public readonly List<Component> Components = new List<Component>();
        public Transform Transform;
    }

    private readonly Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>();
    private static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();
    private readonly Scene scene;
    private readonly Vector3[] corners = new Vector3[4];
    private readonly List<Renderer> rendererBuffer = new List<Renderer>();

    public GuideTargets(Scene ownerScene)
    {
        scene = ownerScene;
    }

    // =========================================================
    // VALIDATION (pure)
    // =========================================================

    /// <summary>Null when the key looks valid, otherwise a short reason.</summary>
    public static string Validate(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "missing target";
        key = key.Trim();
        if (key == "waypoint" || key == "chip" || key == "field") return null;
        if (key.StartsWith("hud:", StringComparison.Ordinal))
        {
            string name = key.Substring(4);
            return Array.IndexOf(HudNames, name) >= 0 ? null : $"unknown hud target \"{name}\" (use {string.Join(", ", HudNames)})";
        }
        if (key.StartsWith("world:", StringComparison.Ordinal)) return key.Length > 6 ? null : "world: needs a scene path";
        if (key.StartsWith("type:", StringComparison.Ordinal)) return key.Length > 5 ? null : "type: needs a component name";
        if (key.StartsWith("ui:", StringComparison.Ordinal)) return key.Length > 3 ? null : "ui: needs an object name";
        if (key.StartsWith("screen:", StringComparison.Ordinal))
            return TryParseScreen(key, out _, out _) ? null : "screen: needs x,y (0..1) or x,y,w,h";
        return $"target \"{key}\" must start with hud:, world:, type:, ui:, screen: or be waypoint";
    }

    /// <summary>"screen:0.7,0.5" or "screen:0.7,0.5,0.3,0.4" (centre and size as screen fractions).</summary>
    public static bool TryParseScreen(string key, out Vector2 center, out Vector2 size)
    {
        center = size = Vector2.zero;
        string[] parts = key.Substring("screen:".Length).Split(',');
        if (parts.Length != 2 && parts.Length != 4) return false;
        var values = new float[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            if (!float.TryParse(parts[i].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out values[i]))
                return false;
        center = new Vector2(values[0], values[1]);
        size = parts.Length == 4 ? new Vector2(values[2], values[3]) : new Vector2(0.06f, 0.06f);
        return true;
    }

    // =========================================================
    // RESOLUTION
    // =========================================================

    public static Camera ViewCamera
    {
        get
        {
            CameraController controller = CameraController.Instance;
            Camera cam = controller != null ? controller.MainCamera : null;
            return cam != null ? cam : Camera.main;
        }
    }

    public static Transform Player => CameraController.Instance != null ? CameraController.Instance.PlayerRoot : null;

    public Hit Resolve(string key)
    {
        var hit = new Hit();
        if (string.IsNullOrWhiteSpace(key)) return hit;
        key = key.Trim();

        if (key == "waypoint")
        {
            ObjectiveGuide guide = ObjectiveGuide.Instance;
            if (guide == null || !guide.HasTarget) return hit;
            if (guide.TargetTransform != null) return FromTransform(guide.TargetTransform);
            return FromWorldPoint(guide.TargetPosition + Vector3.up * 1.2f, 90f);
        }

        if (key == "chip") return ResolveChip();
        if (key == "field") return ResolveField();

        if (key.StartsWith("screen:", StringComparison.Ordinal))
        {
            if (!TryParseScreen(key, out Vector2 c, out Vector2 s)) return hit;
            hit.Found = hit.OnScreen = true;
            Vector2 size = new Vector2(s.x * Screen.width, s.y * Screen.height);
            hit.ScreenRect = new Rect(c.x * Screen.width - size.x * 0.5f, c.y * Screen.height - size.y * 0.5f, size.x, size.y);
            return hit;
        }

        if (key.StartsWith("hud:", StringComparison.Ordinal)) return FromRect(ResolveHud(key.Substring(4)));

        if (key.StartsWith("ui:", StringComparison.Ordinal))
        {
            CacheEntry entry = Lookup(key, () => FindUi(key.Substring(3)));
            return FromRect(entry.Transform as RectTransform);
        }

        if (key.StartsWith("world:", StringComparison.Ordinal))
        {
            CacheEntry entry = Lookup(key, () => FindInScene(key.Substring(6)));
            return FromTransform(entry.Transform);
        }

        if (key.StartsWith("type:", StringComparison.Ordinal))
        {
            CacheEntry entry = LookupComponents(key, key.Substring(5));
            return Best(entry.Components, preferPlayer: false);
        }

        return hit;
    }

    /// <summary>Closest match to the player (for "near:").</summary>
    public Hit ResolveNearPlayer(string key)
    {
        if (key != null && key.Trim().StartsWith("type:", StringComparison.Ordinal))
        {
            CacheEntry entry = LookupComponents(key.Trim(), key.Trim().Substring(5));
            return Best(entry.Components, preferPlayer: true);
        }
        return Resolve(key);
    }

    private CacheEntry Lookup(string key, Func<Transform> find)
    {
        if (!cache.TryGetValue(key, out CacheEntry entry))
        {
            entry = new CacheEntry { Time = -100f };
            cache[key] = entry;
        }
        if (Time.unscaledTime - entry.Time > CacheSeconds || (entry.Transform == null && Time.unscaledTime - entry.Time > 0.1f))
        {
            entry.Time = Time.unscaledTime;
            entry.Transform = find();
        }
        return entry;
    }

    private CacheEntry LookupComponents(string key, string typeName)
    {
        if (!cache.TryGetValue(key, out CacheEntry entry))
        {
            entry = new CacheEntry { Time = -100f };
            cache[key] = entry;
        }
        if (Time.unscaledTime - entry.Time > CacheSeconds)
        {
            entry.Time = Time.unscaledTime;
            entry.Components.Clear();
            Type type = FindType(typeName);
            if (type != null)
            {
                UnityEngine.Object[] found = UnityEngine.Object.FindObjectsByType(type, FindObjectsInactive.Exclude);
                for (int i = 0; i < found.Length; i++)
                    if (found[i] is Component c && c.gameObject.scene == scene) entry.Components.Add(c);
            }
        }
        return entry;
    }

    public static Type FindType(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (typeCache.TryGetValue(name, out Type cached)) return cached;
        Type found = null;
        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = assembly.GetType(name, false);
            if (t != null && typeof(Component).IsAssignableFrom(t))
            {
                found = t;
                break;
            }
        }
        if (found == null) Debug.LogWarning($"[Guide] Component type \"{name}\" not found.");
        typeCache[name] = found;
        return found;
    }

    /// <summary>Still worth pointing at: skips filled areas, settled cards and NPCs with nothing to say (T06: never reveals a draft's correctness).</summary>
    public static bool IsUseful(Component c)
    {
        if (c == null) return false;
        if (c is Behaviour b && !b.isActiveAndEnabled) return false;
        if (c is CorkboardSlot slot) return slot.OccupyingCard == null;
        if (c is ProfileDragCard card) return card.NeedsAttention;
        if (c is INpcAvailabilitySource npc) return npc.Availability == NpcAvailability.Available;
        return true;
    }

    private Hit Best(List<Component> components, bool preferPlayer)
    {
        Hit best = new Hit();
        float bestScore = float.MaxValue;
        bool bestUseful = false;
        Transform player = Player;
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        for (int i = 0; i < components.Count; i++)
        {
            Component c = components[i];
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            Hit hit = c.transform is RectTransform rt && rt.GetComponentInParent<Canvas>() != null
                ? FromRect(rt) : FromTransform(c.transform);
            if (!hit.Found) continue;
            bool useful = IsUseful(c);

            float score;
            if (preferPlayer && player != null && hit.HasWorld)
                score = (hit.World - player.position).sqrMagnitude;
            else
                score = hit.OnScreen ? (hit.ScreenRect.center - screenCenter).sqrMagnitude : 1e12f;

            // Useful candidates always beat finished ones.
            if ((useful && !bestUseful) || (useful == bestUseful && score < bestScore))
            {
                best = hit;
                bestScore = score;
                bestUseful = useful;
            }
        }
        return best;
    }

    // =========================================================
    // VALUE TRANSFER: the right chip and the right field
    // =========================================================

    private DataValue chipValue;

    /// <summary>
    /// A Tax Code value on screen that some still-empty case-folder field
    /// accepts (so a drag demo goes from a value to where it really fits).
    /// </summary>
    private Hit ResolveChip()
    {
        List<Component> chips = LookupComponents("type:TaxCodeValueChip", "TaxCodeValueChip").Components;
        List<Component> fields = LookupComponents("type:CaseFolderFieldRow", "CaseFolderFieldRow").Components;
        Hit best = new Hit(), fallback = new Hit();
        DataValue bestValue = null, fallbackValue = null;
        float bestScore = float.MaxValue, fallbackScore = float.MaxValue;
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        for (int i = 0; i < chips.Count; i++)
        {
            if (!(chips[i] is TaxCodeValueChip chip) || !chip.isActiveAndEnabled) continue;
            Hit hit = FromRect(chip.transform as RectTransform);
            if (!hit.OnScreen) continue;
            DataValue value = chip.GetDataValue();
            float score = (hit.ScreenRect.center - center).sqrMagnitude;
            if (score < fallbackScore) { fallback = hit; fallbackValue = value; fallbackScore = score; }
            if (FindField(fields, value, emptyOnly: true) != null && score < bestScore)
            {
                best = hit;
                bestValue = value;
                bestScore = score;
            }
        }
        chipValue = bestValue ?? fallbackValue;
        return best.Found ? best : fallback;
    }

    /// <summary>
    /// The case-folder field for the value in hand: the selected value if one
    /// is picked up, else the chip the guide is pointing at, else the first
    /// empty field. Name/TIN rows (not destinations) are never chosen.
    /// </summary>
    private Hit ResolveField()
    {
        List<Component> fields = LookupComponents("type:CaseFolderFieldRow", "CaseFolderFieldRow").Components;
        DataValue value = ValueTransferManager.Instance != null && ValueTransferManager.Instance.HasSelection
            ? ValueTransferManager.Instance.SelectedValue : chipValue;
        CaseFolderFieldRow field = value != null
            ? FindField(fields, value, emptyOnly: true) ?? FindField(fields, value, emptyOnly: false)
            : null;
        if (field == null)
        {
            for (int i = 0; i < fields.Count && field == null; i++)
                if (fields[i] is CaseFolderFieldRow f && f.isActiveAndEnabled && f.IsDestination && !f.HasValue &&
                    FromRect(f.transform as RectTransform).OnScreen)
                    field = f;
        }
        return field != null ? FromRect(field.transform as RectTransform) : new Hit();
    }

    private CaseFolderFieldRow FindField(List<Component> fields, DataValue value, bool emptyOnly)
    {
        if (value == null) return null;
        for (int i = 0; i < fields.Count; i++)
        {
            if (!(fields[i] is CaseFolderFieldRow f) || !f.isActiveAndEnabled) continue;
            if (!f.WouldAccept(value) || (emptyOnly && f.HasValue)) continue;
            if (FromRect(f.transform as RectTransform).OnScreen) return f;
        }
        return null;
    }

    // =========================================================
    // HUD NAMES
    // =========================================================

    private RectTransform ResolveHud(string name)
    {
        OfficeHudRuntime hud = OfficeHudRuntime.Instance;
        switch (name)
        {
            case "objective":
            case "timer":
                return hud != null && hud.Objectives != null ? hud.Objectives.transform as RectTransform : null;
            case "progress":
                return hud != null && hud.Progression != null ? hud.Progression.transform as RectTransform : null;
            case "minimap":
                return Lookup("hud:minimap", () => FindUi("Office Minimap HUD")).Transform as RectTransform;
            case "card":
                return HudRewardScreen.Top != null ? HudRewardScreen.Top.Card : null;
            case "continue":
                return HudRewardScreen.Top != null ? HudRewardScreen.Top.ContinueButton : null;
            case "notes": return HudPath("PermanentHUD/IconButtons/NotesIconButton");
            case "folder": return HudPath("PermanentHUD/IconButtons/CaseFolderIconButton");
            case "profiles": return HudPath("PermanentHUD/IconButtons/TaxpayerProfileIcon");
            case "settings": return HudPath("PermanentHUD/Settings/SettingsButton");
            case "icons": return HudPath("PermanentHUD/IconButtons");
            case "joystick": return HudPath("MobileControls/Joystick");
            case "look":
            {
                RectTransform look = HudPath("MobileControls/LookJoystick");
                if (look != null && look.gameObject.activeInHierarchy) return look;
                return HudPath("MobileControls/MobileLookArea");
            }
            case "prompt":
                return Lookup("hud:prompt", () =>
                {
                    HudPrompt prompt = UnityEngine.Object.FindAnyObjectByType<HudPrompt>(FindObjectsInactive.Exclude);
                    return prompt != null ? prompt.transform : null;
                }).Transform as RectTransform;
            case "exit":
                return Lookup("hud:exit", () =>
                {
                    WorkstationUI ui = UnityEngine.Object.FindAnyObjectByType<WorkstationUI>(FindObjectsInactive.Include);
                    UnityEngine.UI.Button button = ui != null ? ui.GetComponentInChildren<UnityEngine.UI.Button>(false) : null;
                    return button != null ? button.transform : null;
                }).Transform as RectTransform;
        }
        return null;
    }

    private RectTransform HudPath(string path)
    {
        return Lookup("hudpath:" + path, () =>
        {
            Canvas canvas = FindHudCanvas();
            return canvas != null ? canvas.transform.Find(path) : null;
        }).Transform as RectTransform;
    }

    private Canvas FindHudCanvas()
    {
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int i = 0; i < canvases.Length; i++)
            if (canvases[i].gameObject.scene == scene && canvases[i].name == "HUD_Canvas") return canvases[i];
        return null;
    }

    private Transform FindUi(string name)
    {
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].gameObject.scene != scene || !canvases[i].isRootCanvas) continue;
            Transform found = FindDeep(canvases[i].transform, name);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name && root.gameObject.activeInHierarchy) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    public Transform FindInScene(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        path = path.Trim().Trim('/');
        int slash = path.IndexOf('/');
        string rootName = slash < 0 ? path : path.Substring(0, slash);
        string rest = slash < 0 ? null : path.Substring(slash + 1);
        if (!scene.IsValid()) return null;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name != rootName) continue;
            if (rest == null) return roots[i].transform;
            Transform child = roots[i].transform.Find(rest);
            if (child != null) return child;
        }
        return null;
    }

    // =========================================================
    // SCREEN RECTANGLES
    // =========================================================

    private Hit FromRect(RectTransform rect)
    {
        var hit = new Hit();
        if (rect == null || !rect.gameObject.activeInHierarchy) return hit;
        if (EffectiveAlpha(rect) < 0.2f) return hit;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas == null) return hit;
        Canvas root = canvas.rootCanvas;
        Camera cam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null
            : root.worldCamera != null ? root.worldCamera : ViewCamera;

        rect.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            if (cam != null && root.renderMode == RenderMode.WorldSpace)
            {
                Vector3 vp = cam.WorldToScreenPoint(corners[i]);
                if (vp.z <= 0f) return hit;
            }
            Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        hit.Found = true;
        hit.HasWorld = true;
        hit.World = rect.TransformPoint(rect.rect.center);
        hit.ScreenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        hit.OnScreen = hit.ScreenRect.width > 2f && hit.ScreenRect.height > 2f && Overlaps(hit.ScreenRect);
        return hit;
    }

    private static float EffectiveAlpha(Transform t)
    {
        float alpha = 1f;
        for (Transform cur = t; cur != null; cur = cur.parent)
        {
            CanvasGroup group = cur.GetComponent<CanvasGroup>();
            if (group != null)
            {
                alpha *= group.alpha;
                if (group.ignoreParentGroups) break;
            }
        }
        return alpha;
    }

    private Hit FromTransform(Transform target)
    {
        var hit = new Hit();
        if (target == null || !target.gameObject.activeInHierarchy) return hit;
        Camera cam = ViewCamera;
        hit.Found = true;
        hit.HasWorld = true;
        hit.World = target.position;
        if (cam == null) return hit;

        rendererBuffer.Clear();
        target.GetComponentsInChildren(false, rendererBuffer);
        bool any = false;
        Bounds bounds = default;
        for (int i = 0; i < rendererBuffer.Count; i++)
        {
            Renderer r = rendererBuffer[i];
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        if (!any) return FromWorldPoint(target.position, 70f);

        hit.World = bounds.center;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        int inFront = 0;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new Vector3(
                (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                (i & 4) == 0 ? bounds.min.z : bounds.max.z);
            Vector3 sp = cam.WorldToScreenPoint(corner);
            if (sp.z <= 0.05f) continue;
            inFront++;
            min = Vector2.Min(min, sp);
            max = Vector2.Max(max, sp);
        }
        if (inFront < 8) return hit; // partly behind the camera: no reliable outline
        // Big objects (a whole board) are clamped to the screen.
        min = Vector2.Max(min, Vector2.zero);
        max = Vector2.Min(max, new Vector2(Screen.width, Screen.height));
        hit.ScreenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        hit.OnScreen = hit.ScreenRect.width > 2f && hit.ScreenRect.height > 2f;
        return hit;
    }

    private Hit FromWorldPoint(Vector3 world, float sizePixels)
    {
        var hit = new Hit { Found = true, HasWorld = true, World = world };
        Camera cam = ViewCamera;
        if (cam == null) return hit;
        Vector3 sp = cam.WorldToScreenPoint(world);
        if (sp.z <= 0.05f) return hit;
        float size = sizePixels * Screen.height / 1080f;
        hit.ScreenRect = new Rect(sp.x - size * 0.5f, sp.y - size * 0.5f, size, size);
        hit.OnScreen = Overlaps(hit.ScreenRect);
        return hit;
    }

    private static bool Overlaps(Rect r) => r.xMax > 0f && r.yMax > 0f && r.xMin < Screen.width && r.yMin < Screen.height;
}
