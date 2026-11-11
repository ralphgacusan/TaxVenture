using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// Milestone T02 visual/contact pilot: the ONE review entry point
/// (Assets/Pilot/T02/T02_PilotReview.unity, a single object holding this
/// component). Press Play and everything is assembled here; nothing needs
/// placing by hand.
///
/// Three identical bays side by side under one light, one camera:
///   A  Baseline: the original Arthur model and laundry washer, as shipped.
///   B  Shading: derived copies whose normals the importer calculates by angle
///      (intentional hard edges kept), washer optionally matte.
///   C  Shading + silhouette: B plus the bounded import-time edge refinement.
/// Each bay: Synty floor, back wall and doorway wall (deliberately hard
/// architecture, unchanged), two Pandazole washers whose tops are the work
/// surface, two small Synty folder holders, the actual handled Case Folder
/// model (SM_Prop_Folder_Manila_02) with the existing client-copy print, and
/// Arthur running the same synchronized walk/point/reach/grip/carry/place/
/// release beats (T02PilotArthur). Camera views keep the same framing in
/// every bay; speed, pause, single-frame step, contact markers and an
/// on-device measurement readout are on screen.
///
/// ISOLATION: no campaign, save, inventory, reward or case code is called
/// here; PlayerProgressManager's bootstrap skips this scene by name
/// (T02PilotIsolation), and the readout shows whether a campaign owner exists.
/// Everything built lives under one runtime root: Restart rebuilds it,
/// disable/exit destroys it, a second fixture refuses to run, focus loss
/// pauses and drops the orbit drag.
/// </summary>
public sealed class T02PilotFixture : MonoBehaviour
{
    [Serializable]
    public sealed class Variant
    {
        public string label;
        public string treatment;
        public GameObject arthur;
        public GameObject washer;
        public bool matteWasher;
    }

    [Header("Originals (referenced, never modified)")]
    public GameObject arthurOriginal;
    public GameObject folderPrefab;
    public GameObject holderPrefab;
    public GameObject washerPrefab;
    public GameObject wallDoorPrefab;
    public GameObject wallPrefab;
    public GameObject floorPrefab;

    [Header("Derived pilot assets (Assets/Pilot/T02)")]
    public GameObject arthurShading;
    public GameObject arthurSilhouette;
    public GameObject washerShading;
    public GameObject washerSilhouette;
    public Material washerMatte;
    public RuntimeAnimatorController pilotController;

    [Header("Staging")]
    public float arthurHeight = 1.7f;
    public float baySpacing = 7f;
    public float holderScale = 0.7f;
    public float holderSpacing = 0.22f;

    public enum View { Explore, Task, Hands, Doorway, Orbit, All }

    public static T02PilotFixture Active { get; private set; }

    private sealed class Bay
    {
        public Variant variant;
        public Transform root;
        public T02PilotArthur arthur;
        public Transform folder;
        public Vector3 origin, station, gripA;
        public readonly List<Renderer> washerRenderers = new List<Renderer>();
        public readonly List<Material[]> washerOriginalMaterials = new List<Material[]>();
        public string resources = "";
        public string problem = "";
    }

    private readonly List<Bay> bays = new List<Bay>();
    private GameObject runtimeRoot;
    private Camera viewCamera;
    private Light sun;
    private Transform markerWanted, markerActual;
    private int activeBay;
    private View view = View.Orbit;
    private readonly float[] speeds = { 1f, 0.5f, 0.25f, 0f };
    private int speedIndex;
    private bool stepOnce, pausedByFocus, showMarkers, showStats, matteOn = true;
    private float orbitYaw, orbitPitch = 15f, orbitRadius = 4.5f;
    private readonly T02PilotOrbitGesture orbitGesture = new T02PilotOrbitGesture();
    private float setupMs, firstFrameAt = -1f;
    private int builds;
    private readonly T02PilotFrameStats frames = new T02PilotFrameStats(600);
    private readonly FrameTiming[] timing = new FrameTiming[1];
    private readonly RaycastHit[] cameraHits = new RaycastHit[16];
    private string notice = "";

    private AmbientMode savedAmbientMode;
    private Color savedSky, savedEquator, savedGround;
    private bool savedRender;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void OnEnable()
    {
        if (Active != null && Active != this)
        {
            Debug.LogWarning("[T02 pilot] A pilot fixture is already running; this copy stays off (no duplicate actors, cameras or listeners).");
            enabled = false;
            return;
        }
        Active = this;
        Build();
    }

    private void OnDisable()
    {
        Teardown();
        if (Active == this) Active = null;
    }

    private void OnApplicationFocus(bool focused) { if (!focused) Interrupt(); else pausedByFocus = false; }
    private void OnApplicationPause(bool paused) { if (paused) Interrupt(); else pausedByFocus = false; }

    /// <summary>Focus loss: drop the owned orbit drag and hold the timeline until focus returns.</summary>
    private void Interrupt()
    {
        orbitGesture.Cancel();
        pausedByFocus = true;
    }

    public int BuildCount => builds;

    /// <summary>(Re)assembles every bay under one runtime root; the previous root is destroyed first.</summary>
    public void Build()
    {
        Teardown();
        float started = Time.realtimeSinceStartup;
        builds++;
        runtimeRoot = new GameObject("T02 Pilot (runtime)");
        SceneManager.MoveGameObjectToScene(runtimeRoot, gameObject.scene);
        SetupLighting();
        SetupCamera();
        var variants = new[]
        {
            new Variant { label = "A  Baseline", treatment = "original Arthur and washer, as shipped", arthur = arthurOriginal },
            new Variant { label = "B  Shading", treatment = "normals calculated by angle (Arthur 50°, washer 40°); hard edges kept; washer matte", arthur = arthurShading, washer = washerShading, matteWasher = true },
            new Variant { label = "C  Shading + silhouette", treatment = "B plus one bounded refinement of faceted soft edges (import time)", arthur = arthurSilhouette, washer = washerSilhouette, matteWasher = true }
        };
        for (int i = 0; i < variants.Length; i++)
        {
            Bay bay = BuildBay(i, variants[i]);
            bays.Add(bay);
        }
        markerWanted = Marker("Contact: wanted palm", new Color(0.25f, 0.8f, 0.35f));
        markerActual = Marker("Contact: actual palm", new Color(0.95f, 0.55f, 0.15f));
        SetActiveBay(Mathf.Clamp(activeBay, 0, bays.Count - 1));
        ApplyMatte();
        setupMs = (Time.realtimeSinceStartup - started) * 1000f;
        frames.Clear();
        Debug.Log($"[T02 pilot] Built {bays.Count} bays in {setupMs:0} ms (build #{builds}).");
    }

    private void Teardown()
    {
        orbitGesture.Cancel();
        bays.Clear();
        if (runtimeRoot != null)
        {
            if (Application.isPlaying) Destroy(runtimeRoot); else DestroyImmediate(runtimeRoot);
        }
        runtimeRoot = null;
        viewCamera = null;
        sun = null;
        markerWanted = markerActual = null;
        if (savedRender)
        {
            RenderSettings.ambientMode = savedAmbientMode;
            RenderSettings.ambientSkyColor = savedSky;
            RenderSettings.ambientEquatorColor = savedEquator;
            RenderSettings.ambientGroundColor = savedGround;
            savedRender = false;
        }
    }

    private void SetupLighting()
    {
        if (!savedRender)
        {
            savedAmbientMode = RenderSettings.ambientMode;
            savedSky = RenderSettings.ambientSkyColor;
            savedEquator = RenderSettings.ambientEquatorColor;
            savedGround = RenderSettings.ambientGroundColor;
            savedRender = true;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.72f);
        RenderSettings.ambientEquatorColor = new Color(0.55f, 0.52f, 0.48f);
        RenderSettings.ambientGroundColor = new Color(0.30f, 0.27f, 0.24f);
        var light = new GameObject("Pilot Sun (one light for every bay)");
        light.transform.SetParent(runtimeRoot.transform, false);
        light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        sun = light.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.96f, 0.88f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;
    }

    private void SetupCamera()
    {
        var host = new GameObject("Pilot Camera");
        host.transform.SetParent(runtimeRoot.transform, false);
        host.tag = "MainCamera";
        viewCamera = host.AddComponent<Camera>();
        viewCamera.fieldOfView = 55f;
        viewCamera.nearClipPlane = 0.03f;
        viewCamera.farClipPlane = 120f;
        // Exactly one listener: ours only when the scene has none of its own.
        bool other = false;
        foreach (AudioListener listener in FindObjectsByType<AudioListener>())
            if (listener.enabled && listener.gameObject.activeInHierarchy) other = true;
        if (!other) host.AddComponent<AudioListener>();
        else notice = "Another AudioListener is active; the pilot camera adds none.";
    }

    private Transform Marker(string name, Color color)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        sphere.transform.SetParent(runtimeRoot.transform, false);
        sphere.transform.localScale = Vector3.one * 0.018f;
        Renderer renderer = sphere.GetComponent<Renderer>();
        if (washerMatte != null)
        {
            var material = new Material(washerMatte) { name = name };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", null);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            renderer.sharedMaterial = material;
        }
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sphere.SetActive(false);
        return sphere.transform;
    }

    // =========================================================
    // ONE BAY
    // =========================================================

    private Bay BuildBay(int index, Variant variant)
    {
        var bay = new Bay { variant = variant };
        bay.origin = Vector3.right * (index * baySpacing);
        bay.root = new GameObject("Bay " + variant.label).transform;
        bay.root.SetParent(runtimeRoot.transform, false);
        bay.root.position = bay.origin;

        // Floor tiles (2.5 m Synty modules, pivot at a corner), back wall, doorway wall turned to run along z.
        foreach (float x in new[] { -3.75f, -1.25f, 1.25f })
            foreach (float z in new[] { -2.5f, 0f })
                Place(floorPrefab, bay, new Vector3(x, 0f, z), Quaternion.identity, "Floor");
        Place(wallPrefab, bay, new Vector3(-2.0f, 0f, 1.6f), Quaternion.identity, "Back wall");
        Place(wallPrefab, bay, new Vector3(0.5f, 0f, 1.6f), Quaternion.identity, "Back wall");
        Place(wallDoorPrefab, bay, new Vector3(-2.0f, 0f, -1.25f), Quaternion.Euler(0f, -90f, 0f), "Doorway wall");

        // Two washers against the back wall, fronts toward Arthur; their tops are the work surface.
        float washerZ = 1.108f;
        float top = 1.01f, front = washerZ - 0.414f;
        foreach (float x in new[] { 0.45f, -0.45f })
        {
            GameObject washer = Place(washerPrefab, bay, new Vector3(x, 0f, washerZ), Quaternion.Euler(0f, 180f, 0f), "Washer");
            if (washer == null) continue;
            foreach (Renderer r in washer.GetComponentsInChildren<Renderer>(true))
            {
                bay.washerRenderers.Add(r);
                bay.washerOriginalMaterials.Add(r.sharedMaterials);
                top = Mathf.Max(top, r.bounds.max.y);
                front = Mathf.Min(front, r.bounds.min.z - bay.origin.z);
            }
            if (variant.washer != null)
            {
                MeshFilter derived = variant.washer.GetComponentInChildren<MeshFilter>(true);
                foreach (MeshFilter filter in washer.GetComponentsInChildren<MeshFilter>(true))
                    if (derived != null && derived.sharedMesh != null) filter.sharedMesh = derived.sharedMesh;
            }
        }
        top -= bay.origin.y;

        // Small file holders on each washer top, turned so the folder's cover faces Arthur; the surface is measured, not assumed.
        Physics.SyncTransforms();
        Vector3 holderA = new Vector3(holderSpacing, SurfaceAt(bay, holderSpacing, washerZ - 0.16f, top), washerZ - 0.16f);
        Vector3 holderB = new Vector3(-holderSpacing, SurfaceAt(bay, -holderSpacing, washerZ - 0.16f, top), washerZ - 0.16f);
        top = Mathf.Max(holderA.y, holderB.y);
        float holderTop = top;
        foreach (Vector3 at in new[] { holderA, holderB })
        {
            GameObject holder = Place(holderPrefab, bay, at, Quaternion.Euler(0f, 90f, 0f), "Folder holder");
            if (holder == null) continue;
            holder.transform.localScale *= holderScale;
            foreach (Renderer r in holder.GetComponentsInChildren<Renderer>(true)) holderTop = Mathf.Max(holderTop, r.bounds.max.y - bay.origin.y);
        }

        // The actual handled Case Folder model, standing in holder A.
        GameObject folder = Place(folderPrefab, bay, Vector3.zero, Quaternion.identity, "Case Folder (SM_Prop_Folder_Manila_02)");
        if (folder == null) { bay.problem = "folder prefab missing"; return bay; }
        foreach (Rigidbody body in folder.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        bay.folder = folder.transform;
        Quaternion upright = Quaternion.LookRotation(-Vector3.right, -Vector3.forward);
        Pose restA = Rest(folder.transform, holderA, top, upright, bay), restB = Rest(folder.transform, holderB, top, upright, bay);
        folder.transform.SetPositionAndRotation(restA.position, restA.rotation);
        AddPrint(folder.transform);

        // Arthur: one root per bay (the model is its child), same scale in every bay.
        var arthurRoot = new GameObject("Arthur " + variant.label);
        arthurRoot.transform.SetParent(bay.root, false);
        GameObject model = variant.arthur != null ? Instantiate(variant.arthur, arthurRoot.transform) : null;
        if (model == null) { bay.problem = "Arthur model missing (derived asset not imported yet?)"; return bay; }
        model.name = variant.arthur.name;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        bay.arthur = arthurRoot.AddComponent<T02PilotArthur>();

        float shoulderHeight = 0.82f * arthurHeight, armGuess = 0.36f * arthurHeight;
        Vector3 gripA = restA.position + Vector3.up * 0.15f - bay.origin, gripB = restB.position + Vector3.up * 0.15f - bay.origin;
        float stationX = (gripA.x + gripB.x) * 0.5f - 0.18f;
        float stationZ = T02PilotReach.StationDepth(gripA, gripB, stationX, 0.18f, shoulderHeight, 0.02f, armGuess, 0.88f, front, 0.16f, 0.08f, out float ratio);
        bay.station = new Vector3(stationX, 0f, stationZ);
        bay.gripA = gripA;
        Vector3 start = new Vector3(-3.3f, 0f, 0f), turn = new Vector3(-3.9f, 0f, 0.6f);
        var enter = World(bay, turn, new Vector3(-2.6f, 0f, -0.02f), new Vector3(-1.4f, 0f, -0.02f), new Vector3(-0.6f, 0f, stationZ - 0.55f),
            new Vector3(stationX, 0f, stationZ - 0.75f), bay.station);
        var exit = World(bay, new Vector3(-0.7f, 0f, stationZ - 0.35f), new Vector3(-1.4f, 0f, -0.02f), new Vector3(-2.6f, 0f, -0.02f), start);
        arthurRoot.transform.position = bay.origin + start;
        arthurRoot.transform.rotation = Quaternion.LookRotation(turn - start, Vector3.up);
        bay.arthur.Init(model.transform, pilotController, bay.root, folder.transform, restA, restB, holderTop + bay.origin.y, enter, exit, Quaternion.LookRotation(Vector3.forward));
        if (!string.IsNullOrEmpty(bay.arthur.Problem)) bay.problem = bay.arthur.Problem;

        if (bay.arthur.Animator != null)
        {
            bay.arthur.Animator.Update(0f);
            bay.arthur.FitStandingHeight(arthurHeight);
        }
        if (!string.IsNullOrEmpty(bay.arthur.Problem)) bay.problem = bay.arthur.Problem;
        bay.resources = ResourceLine(bay, ratio);
        return bay;
    }

    /// <summary>The washer top under a bay-local point (its own collider), or the bounds top when nothing is hit.</summary>
    private static float SurfaceAt(Bay bay, float x, float z, float fallback)
    {
        Vector3 from = bay.origin + new Vector3(x, 2.5f, z);
        foreach (RaycastHit hit in Physics.RaycastAll(from, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
            if (hit.collider != null && hit.collider.transform.IsChildOf(bay.root) && hit.collider.name.Contains("Washer") && hit.point.y > 0.3f)
                return hit.point.y - bay.origin.y;
        return fallback;
    }

    private static List<Vector3> World(Bay bay, params Vector3[] local)
    {
        var list = new List<Vector3>(local.Length);
        foreach (Vector3 p in local) list.Add(bay.origin + p);
        return list;
    }

    /// <summary>The folder pivot that stands it upright in a holder: mesh centre over the holder, bottom on the holder floor.</summary>
    private Pose Rest(Transform folder, Vector3 holder, float surfaceTop, Quaternion rotation, Bay bay)
    {
        // Measured in world space with the folder still at its unrotated spawn pose, so mesh units and import scale cannot mislead.
        Renderer shape = folder.GetComponentInChildren<Renderer>(true);
        Bounds world = shape != null ? shape.bounds : new Bounds(folder.position, new Vector3(0.41f, 0.026f, 0.3f));
        Vector3 offset = world.center - folder.position;
        float floor = surfaceTop + 0.01f * holderScale;
        Vector3 centre = new Vector3(holder.x, floor + world.extents.x, holder.z);
        Vector3 pivot = centre - rotation * offset;
        return new Pose(bay.origin + pivot, rotation);
    }

    /// <summary>The existing client-copy print on the cover, for readability at the same distances in every bay.</summary>
    private static void AddPrint(Transform folder)
    {
        MeshFilter shape = folder.GetComponentInChildren<MeshFilter>(true);
        if (shape == null || shape.sharedMesh == null) return;
        Bounds b = shape.sharedMesh.bounds;
        var host = new GameObject("Cover print (ClientCopyStamp)", typeof(RectTransform));
        host.layer = folder.gameObject.layer;
        var rect = (RectTransform)host.transform;
        rect.SetParent(shape.transform, false);
        rect.localPosition = new Vector3(b.center.x, b.max.y + 0.002f, b.center.z);
        rect.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.right);
        const float unitsPerMetre = 1000f;
        rect.localScale = Vector3.one / unitsPerMetre;
        float width = Mathf.Max(0.05f, Mathf.Min(b.size.x, b.size.z) * 0.7f) * unitsPerMetre;
        rect.sizeDelta = new Vector2(width, width);
        host.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ClientCopyStamp.Build(rect, width, Vector2.zero);
    }

    private GameObject Place(GameObject prefab, Bay bay, Vector3 local, Quaternion rotation, string label)
    {
        if (prefab == null) { bay.problem = label + " prefab missing"; return null; }
        GameObject instance = Instantiate(prefab, bay.root);
        instance.name = label + " (" + prefab.name + ")";
        instance.transform.localPosition = local;
        instance.transform.localRotation = rotation;
        return instance;
    }

    private static string ResourceLine(Bay bay, float reachRatio)
    {
        int arthurVerts = 0, arthurTris = 0, arthurMats = 0, bones = 0;
        foreach (SkinnedMeshRenderer s in bay.arthur.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (s.sharedMesh == null) continue;
            arthurVerts += s.sharedMesh.vertexCount;
            arthurTris += Triangles(s.sharedMesh);
            arthurMats += s.sharedMaterials.Length;
            bones = Mathf.Max(bones, s.bones.Length);
        }
        int washerVerts = 0, washerTris = 0;
        foreach (Renderer r in bay.washerRenderers)
        {
            MeshFilter f = r.GetComponent<MeshFilter>();
            if (f == null || f.sharedMesh == null) continue;
            washerVerts = f.sharedMesh.vertexCount;
            washerTris = Triangles(f.sharedMesh);
        }
        MeshFilter folder = bay.folder != null ? bay.folder.GetComponentInChildren<MeshFilter>(true) : null;
        string folderStat = folder != null && folder.sharedMesh != null ? $"{folder.sharedMesh.vertexCount} v / {Triangles(folder.sharedMesh)} tris" : "n/a";
        return $"Arthur imported {arthurVerts} v / {arthurTris} tris, {arthurMats} material slots, {bones} bones; washer {washerVerts} v / {washerTris} tris (x2); " +
               $"folder {folderStat}; planned reach ratio {reachRatio:0.00}";
    }

    private static int Triangles(Mesh mesh)
    {
        long n = 0;
        for (int s = 0; s < mesh.subMeshCount; s++) n += mesh.GetIndexCount(s);
        return (int)(n / 3);
    }

    // =========================================================
    // PER FRAME
    // =========================================================

    private void Update()
    {
        if (runtimeRoot == null) return;
        if (firstFrameAt < 0f) firstFrameAt = Time.realtimeSinceStartup;
        frames.Add(Time.unscaledDeltaTime * 1000f);
        FrameTimingManager.CaptureFrameTimings();

        float speed = pausedByFocus ? 0f : speeds[speedIndex];
        float dt = Time.deltaTime * speed;
        float animatorSpeed = speed;
        if (stepOnce)
        {
            dt = 1f / 30f;
            animatorSpeed = Time.deltaTime > 0f ? dt / Time.deltaTime : 0f;
            stepOnce = false;
        }
        foreach (Bay bay in bays)
        {
            if (bay.arthur == null) continue;
            if (bay.arthur.Animator != null) bay.arthur.Animator.speed = animatorSpeed;
            bay.arthur.Tick(dt);
        }
        OrbitInput();
    }

    private void LateUpdate()
    {
        if (viewCamera == null || bays.Count == 0) return;
        Bay bay = bays[activeBay];
        PlaceCamera(bay);
        bool markers = showMarkers && bay.arthur != null && bay.arthur.Calibrated && T02PilotPlan.ArmActive(bay.arthur.Beat);
        if (markerWanted != null) { markerWanted.gameObject.SetActive(markers); if (markers) markerWanted.position = bay.arthur.DesiredPalm; }
        if (markerActual != null) { markerActual.gameObject.SetActive(markers); if (markers) markerActual.position = bay.arthur.ActualPalm; }
    }

    private void PlaceCamera(Bay bay)
    {
        Vector3 o = bay.origin, s = bay.origin + bay.station;
        Vector3 actor = bay.arthur != null ? bay.arthur.transform.position + Vector3.up * (arthurHeight * 0.55f) : s + Vector3.up;
        Vector3 pos, look;
        switch (view)
        {
            case View.Explore: look = actor; pos = look + new Vector3(1.9f, 1.3f, -3.9f); break;
            case View.Hands:
            {
                Transform hand = bay.arthur != null && bay.arthur.Animator != null && bay.arthur.Animator.isHuman
                    ? bay.arthur.Animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
                look = hand != null ? hand.position : o + bay.gripA;
                pos = look + new Vector3(0.38f, 0.12f, -0.65f);
                break;
            }
            case View.Doorway:
                look = actor; pos = look + new Vector3(2.8f, 1.0f, -2.8f); break;
            case View.Orbit:
            {
                Vector3 centre = actor;
                Quaternion q = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
                pos = centre + q * (Vector3.back * orbitRadius);
                look = centre;
                Vector3 direction = (pos - centre).normalized;
                int count = Physics.SphereCastNonAlloc(centre, 0.08f, direction, cameraHits, orbitRadius, ~0, QueryTriggerInteraction.Ignore);
                float distance = orbitRadius;
                for (int i = 0; i < count; i++)
                {
                    RaycastHit hit = cameraHits[i];
                    if (hit.collider == null || bay.arthur != null && hit.collider.transform.IsChildOf(bay.arthur.transform)) continue;
                    distance = Mathf.Min(distance, Mathf.Max(0.4f, hit.distance - 0.08f));
                }
                pos = centre + direction * distance;
                break;
            }
            case View.All:
            {
                float mid = (bays.Count - 1) * baySpacing * 0.5f;
                pos = new Vector3(mid, 5.5f, -11f); look = new Vector3(mid, 0.8f, 0.6f);
                break;
            }
            default:
                // A task close-up is useful only once the actor reaches the surface.
                // During entry/exit keep him in the frame instead of showing an empty station.
                bool atStation = Vector3.Distance(bay.arthur != null ? bay.arthur.transform.position : s, s) < 0.6f;
                look = atStation ? o + new Vector3(0f, 1.15f, bay.station.z + 0.45f) : actor;
                pos = atStation ? s + new Vector3(0.8f, 1.55f, -0.65f) : actor + new Vector3(1.2f, 0.7f, -2.6f);
                break;
        }
        viewCamera.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
    }

    private void OrbitInput()
    {
        if (pausedByFocus) { orbitGesture.Cancel(); return; }
        // Explicit finger ownership: UI starts never become drags; touch does not also drive the emulated mouse.
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began && orbitGesture.Begin(touch.fingerId, touch.position, OverPanel(touch.position))) StartOrbit();
                if (touch.phase == TouchPhase.Moved) RotateOrbit(orbitGesture.Move(touch.fingerId, touch.position));
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) orbitGesture.End(touch.fingerId);
            }
            return;
        }
        Vector2 pointer = Input.mousePosition;
        if (Input.GetMouseButtonDown(0) && orbitGesture.Begin(-2, pointer, OverPanel(pointer))) StartOrbit();
        if (Input.GetMouseButton(0)) RotateOrbit(orbitGesture.Move(-2, pointer));
        if (Input.GetMouseButtonUp(0)) orbitGesture.End(-2);
        if (!OverPanel(pointer) && Mathf.Abs(Input.mouseScrollDelta.y) > 0f)
        {
            StartOrbit();
            orbitRadius = Mathf.Clamp(orbitRadius - Input.mouseScrollDelta.y * 0.25f, 0.6f, 8f);
        }
    }

    private void RotateOrbit(Vector2 delta)
    {
        delta /= UiScale;
        orbitYaw += delta.x * 0.25f;
        orbitPitch = Mathf.Clamp(orbitPitch - delta.y * 0.2f, -10f, 70f);
    }

    private void StartOrbit()
    {
        if (view == View.Orbit) return;
        // Begin every inspection with the whole actor framed; each bay shares these orbit angles/distance.
        view = View.Orbit;
        orbitRadius = 4.5f; orbitPitch = 15f; orbitYaw = 0f;
    }

    private void SetView(View next)
    {
        if (view == next) return;
        orbitGesture.Cancel();
        view = next;
    }

    private bool OverPanel(Vector3 pointer)
    {
        float s = UiScale;
        Vector2 gui = new Vector2(pointer.x / s, (Screen.height - pointer.y) / s);
        // The measurement sheet is read-only: it must not consume camera gestures.
        return PanelRect.Contains(gui);
    }

    private void SetActiveBay(int index)
    {
        activeBay = (index % bays.Count + bays.Count) % bays.Count;
        for (int i = 0; i < bays.Count; i++)
            if (bays[i].arthur != null) bays[i].arthur.AudioActive = i == activeBay;
    }

    private void ApplyMatte()
    {
        foreach (Bay bay in bays)
            for (int i = 0; i < bay.washerRenderers.Count; i++)
            {
                Material[] original = bay.washerOriginalMaterials[i];
                if (!bay.variant.matteWasher || !matteOn || washerMatte == null) { bay.washerRenderers[i].sharedMaterials = original; continue; }
                var swapped = new Material[original.Length];
                for (int m = 0; m < original.Length; m++) swapped[m] = washerMatte;
                bay.washerRenderers[i].sharedMaterials = swapped;
            }
    }

    // =========================================================
    // ON-SCREEN CONTROLS AND MEASUREMENT READOUT
    // =========================================================

    private float UiScale => Mathf.Max(0.5f, Screen.height / 720f);
    private Rect PanelRect => new Rect(Screen.safeArea.x / UiScale + 8f, (Screen.height - Screen.safeArea.yMax) / UiScale + 8f, 430f, 290f);
    private Rect StatsRect => new Rect(Screen.width / UiScale - 470f - 8f, 8f, 470f, 420f);

    private void OnGUI()
    {
        if (runtimeRoot == null || bays.Count == 0) return;
        Matrix4x4 saved = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * UiScale);
        Bay bay = bays[activeBay];
        GUILayout.BeginArea(PanelRect, GUI.skin.box);
        GUILayout.Label("T02 pilot · isolated review (no campaign save)");
        GUILayout.Label($"{bay.variant.label}: {bay.variant.treatment}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("◀ bay")) SetActiveBay(activeBay - 1);
        for (int i = 0; i < bays.Count; i++) if (GUILayout.Toggle(i == activeBay, bays[i].variant.label.Substring(0, 1), GUI.skin.button)) SetActiveBay(i);
        if (GUILayout.Button("bay ▶")) SetActiveBay(activeBay + 1);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        foreach (View v in (View[])Enum.GetValues(typeof(View))) if (GUILayout.Toggle(view == v, v.ToString(), GUI.skin.button)) SetView(v);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        string[] labels = { "1x", "½x", "¼x", "Pause" };
        for (int i = 0; i < speeds.Length; i++) if (GUILayout.Toggle(speedIndex == i, labels[i], GUI.skin.button)) speedIndex = i;
        if (GUILayout.Button("Step")) { speedIndex = 3; stepOnce = true; }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Restart")) Build();
        showMarkers = GUILayout.Toggle(showMarkers, "Contact markers", GUI.skin.button);
        bool matte = GUILayout.Toggle(matteOn, "Matte washer (B,C)", GUI.skin.button);
        if (matte != matteOn) { matteOn = matte; ApplyMatte(); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        showStats = GUILayout.Toggle(showStats, "Measurements", GUI.skin.button);
        if (GUILayout.Button("Copy sheet")) { string sheet = Sheet(); GUIUtility.systemCopyBuffer = sheet; Debug.Log(sheet); notice = "Measurement sheet copied (also in the log)."; }
        if (GUILayout.Button("Exit")) Exit();
        GUILayout.EndHorizontal();
        if (bay.arthur != null)
            GUILayout.Label($"Beat {bay.arthur.Beat} {bay.arthur.BeatProgress:P0}{(bay.arthur.HoldsFolder ? " · folder in hand" : "")}" +
                            $"{(pausedByFocus ? " · paused (focus)" : "")}");
        GUILayout.Label("Drag outside controls: orbit · wheel or −/+: zoom");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("− Zoom out")) { StartOrbit(); orbitRadius = Mathf.Min(8f, orbitRadius + 0.4f); }
        if (GUILayout.Button("+ Zoom in")) { StartOrbit(); orbitRadius = Mathf.Max(0.6f, orbitRadius - 0.4f); }
        if (GUILayout.Button("Frame Arthur")) { SetView(View.Orbit); orbitYaw = 0f; orbitPitch = 15f; orbitRadius = 4.5f; }
        GUILayout.EndHorizontal();
        if (!string.IsNullOrEmpty(bay.problem)) GUILayout.Label("Problem: " + bay.problem);
        if (!string.IsNullOrEmpty(notice)) GUILayout.Label(notice);
        GUILayout.EndArea();
        if (showStats)
        {
            GUILayout.BeginArea(StatsRect, GUI.skin.box);
            GUILayout.Label(Sheet());
            GUILayout.EndArea();
        }
        GUI.matrix = saved;
    }

    /// <summary>The measurement sheet: device, frame pacing, memory, timings and per-bay imported resources (all measured here, none assumed).</summary>
    public string Sheet()
    {
        var text = new StringBuilder();
        text.AppendLine($"Device {SystemInfo.deviceModel} · {SystemInfo.operatingSystem} · RAM {SystemInfo.systemMemorySize} MB · GPU {SystemInfo.graphicsDeviceName}");
        text.AppendLine($"Screen {Screen.width}x{Screen.height} @{Screen.currentResolution.refreshRateRatio.value:0.#} Hz · {Screen.orientation} · target FPS {Application.targetFrameRate} · quality {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
        text.AppendLine($"Frames {frames.Count}: p50 {frames.Percentile(50):0.0} ms, p95 {frames.Percentile(95):0.0}, p99 {frames.Percentile(99):0.0}, max {frames.Max:0.0}; over 33.3 ms: {frames.Over(33.3f)}");
        uint got = FrameTimingManager.GetLatestTimings(1, timing);
        text.AppendLine(got > 0 && timing[0].cpuFrameTime > 0 ? $"CPU {timing[0].cpuFrameTime:0.0} ms · GPU {timing[0].gpuFrameTime:0.0} ms (latest)" : "CPU/GPU frame timing: not available (enable Frame Timing Stats)");
        text.AppendLine($"Memory allocated {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576} MB, reserved {UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1048576} MB, mono {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1048576} MB");
        text.AppendLine($"Pilot setup {setupMs:0} ms (build #{builds}); app start to first pilot frame {(firstFrameAt >= 0f ? firstFrameAt.ToString("0.0") : "-")} s; view/bay switch: camera cut, no load");
        text.AppendLine($"Campaign save owner in scene: {(GameObject.Find("PlayerProgressManager") == null ? "absent (isolated)" : "PRESENT (opened from a running game session; pilot does not touch it)")}");
        foreach (Bay b in bays)
        {
            text.AppendLine($"{b.variant.label}: {b.resources}");
            if (b.arthur != null && b.arthur.Calibrated)
                text.AppendLine($"   arm {b.arthur.ArmLength:0.000} m, hand {b.arthur.HandLength:0.000} m, walk {b.arthur.WalkSpeed:0.00} m/s, foot offset {b.arthur.FootGroundOffsetMm:0} mm, " +
                                $"grip error {Mm(b.arthur.GripErrorMm)}, place error {Mm(b.arthur.PlaceErrorMm)} / {b.arthur.PlaceAngleDegrees:0.0}°, reach {b.arthur.ReachRatio:0.00}");
        }
        return text.ToString();
    }

    private static string Mm(float value) => value < 0f ? "-" : value.ToString("0.0") + " mm";

    private void Exit()
    {
        Teardown();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
