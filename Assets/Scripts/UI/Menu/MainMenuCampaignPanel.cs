using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Uses the authored Play / New Game / Continue scene objects directly.
/// Labels use imported 3D Minnie lettering that reproduces
/// Settings' font, bevel and depth, on its existing renderer/material. Uniform
/// scale preserves proportions. MainMenu owns input/save refresh; this observer
/// owns visibility and the shared press.
/// </summary>
[ExecuteAlways]
public sealed class MainMenuCampaignPanel : MonoBehaviour
{
    public const string RootName = "Campaign Foundation";
    private const string PlayButtonName = "play_button";
    private const string SettingsButtonName = "settings_button";
    private const string NoteMeshPrefix = "SM_Prop_Note";
    private const string WordMeshPrefix = "Text";

    private const float PressDuration = 0.16f;
    [SerializeField, Range(0f, 2f)] private float idleSwayDegrees = 0.65f;
    private sealed class Sway
    {
        public Transform Note;
        public Quaternion Rest;
        public Vector3 Axis;
        public float Phase;
    }
    private readonly List<Sway> sways = new List<Sway>();

    private readonly Dictionary<string, Mesh> letteringMeshes = new Dictionary<string, Mesh>();

    private SettingsBoard3D settingsBoard;
    [SerializeField] private GameObject playNote;
    [Tooltip("Saved scene object. Its position and scale are never changed by this panel.")]
    [SerializeField] private GameObject newGameNote;
    [SerializeField] private GameObject continueNote;
    private bool built;
    private Vector3 pressDirection;   // in the Buttons parent's space, into the board
    private CampaignMenuModel model;

    private sealed class Press
    {
        public Transform Note;
        public Vector3 Rest;
        public float Elapsed;
        public System.Action Completed;
    }
    private readonly List<Press> presses = new List<Press>();

    public GameObject NewGameNote => newGameNote;
    public GameObject ContinueNote => continueNote;

    /// <summary>Binds the saved panel and authored notes without replacing their geometry or layout.</summary>
    public static MainMenuCampaignPanel Ensure(Scene scene, SettingsBoard3D settingsBoard)
    {
        MainMenuCampaignPanel panel = null;
        foreach (GameObject candidate in scene.GetRootGameObjects())
        {
            panel = candidate.GetComponentInChildren<MainMenuCampaignPanel>(true);
            if (panel != null) break;
        }
        if (panel == null)
        {
            Debug.LogError("[MainMenuCampaignPanel] The scene needs its authored menu panel and button references.");
            return null;
        }
        panel.settingsBoard = settingsBoard;
        if (!panel.built || panel.playNote == null) panel.built = panel.Build(scene);
        return panel.built ? panel : null;
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorApplication.delayCall += ApplyLabelsInEditor;
#endif
    }

#if UNITY_EDITOR
    private void ApplyLabelsInEditor()
    {
        if (this != null && !Application.isPlaying) ApplyAuthoredLabels();
    }

#endif

    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= ApplyLabelsInEditor;
#endif
        foreach (Sway sway in sways)
            if (sway.Note != null) sway.Note.localRotation = sway.Rest;
        sways.Clear();
        foreach (Press press in presses)
            if (press.Note != null) press.Note.localPosition = press.Rest;
        presses.Clear();
        built = false;
    }

    // Runs in Edit Mode too. Replace only the duplicate's original Settings
    // glyphs; retain later user mesh/transform edits and all original materials.
    [ContextMenu("Apply Existing 3D Button Labels")]
    public void ApplyAuthoredLabels()
    {
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;
        GameObject settings = Find(gameObject.scene, SettingsButtonName);
        MeshFilter original = WordFilter(settings);
        if (original == null) return;
        foreach (Mesh mesh in Resources.LoadAll<Mesh>("MenuActionLettering"))
            letteringMeshes[mesh.name] = mesh;
        MeshFilter authoredPlay = WordFilter(Find(gameObject.scene, PlayButtonName));
        Mesh playMesh = authoredPlay != null ? authoredPlay.sharedMesh : null;
        ApplyAuthoredLabel(newGameNote, "NewGame", original, playMesh);
        ApplyAuthoredLabel(continueNote, "Continue", original, playMesh);
    }

    private void ApplyAuthoredLabel(GameObject note, string name, MeshFilter original, Mesh playMesh)
    {
        MeshFilter label = WordFilter(note);
        if (label == null || (label.sharedMesh != original.sharedMesh && label.sharedMesh != playMesh) ||
            !letteringMeshes.TryGetValue(name, out Mesh mesh)) return;
        label.sharedMesh = mesh;
        // A hand-made Play duplicate may use a different texture atlas. Keep
        // the original Settings lettering material for its matching white UVs.
        MeshRenderer renderer = label.GetComponent<MeshRenderer>();
        renderer.sharedMaterials = original.GetComponent<MeshRenderer>().sharedMaterials;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            UnityEditor.EditorUtility.SetDirty(label);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private static MeshFilter WordFilter(GameObject note)
    {
        if (note != null)
            foreach (MeshFilter filter in note.GetComponentsInChildren<MeshFilter>(true))
                if (filter.name.StartsWith(WordMeshPrefix)) return filter;
        return null;
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                if (node.name == name) return node.gameObject;
        return null;
    }

    // =========================================================
    // BUILD
    // =========================================================

    private bool Build(Scene scene)
    {
        if (playNote == null) playNote = Find(scene, PlayButtonName);
        GameObject settings = Find(scene, SettingsButtonName);
        if (newGameNote == null) newGameNote = Find(scene, MenuEntry.NewGameButtonName);
        if (newGameNote == null) newGameNote = Find(scene, MenuEntry.LegacyNewGameButtonName);
        if (continueNote == null) continueNote = Find(scene, MenuEntry.ContinueButtonName);
        if (playNote == null || settings == null || newGameNote == null || continueNote == null)
        {
            Debug.LogError($"[MainMenuCampaignPanel] '{PlayButtonName}' or '{SettingsButtonName}' is missing in {scene.name}; the saved Play / Settings / New Game / Continue objects are required.");
            return false;
        }
        ApplyAuthoredLabels();
        Transform buttons = playNote.transform.parent;
        if (settings.transform.parent != buttons)
        {
            Debug.LogError("[MainMenuCampaignPanel] play_button and settings_button no longer share a parent; the story row cannot be laid out.");
            return false;
        }

        // Sizes in the Buttons parent's space (the column of notes on the corkboard).
        Bounds paper = LocalBounds(buttons, settings, NoteMeshPrefix);
        if (paper.size.x <= 0f)
        {
            Debug.LogError("[MainMenuCampaignPanel] settings_button has no note mesh to copy.");
            return false;
        }

        // All controls are the existing scene objects. Their authored geometry,
        // names, parents, positions and scales stay intact.
        ToggleCamera cameras = FindAnyObjectByType<ToggleCamera>();
        Camera menuView = cameras != null && cameras.mainCam != null ? cameras.mainCam : Camera.main;
        pressDirection = PaperNormal(buttons, settings, toward: menuView != null ? menuView.transform.position : buttons.position) * -1f;
        pressDirection *= paper.size.y * 0.06f;
        sways.Clear();
        GameObject[] notes = { playNote, newGameNote, continueNote, settings, Find(scene, "tutorial_button") };
        foreach (GameObject note in notes)
        {
            if (note == null) continue;
            sways.Add(new Sway { Note = note.transform, Rest = note.transform.localRotation,
                Axis = PaperNormal(note.transform.parent, note, menuView != null ? menuView.transform.position : buttons.position),
                Phase = sways.Count * 1.3f });
        }

        Apply(MenuEntry.CurrentModel());
        return true;
    }

    /// <summary>The paper's face normal (toward the camera), its up direction and world size.</summary>
    private static void PaperAxes(Renderer paper, Vector3 toCamera, out Vector3 normal, out Vector3 up, out float width, out float height, out float thickness)
    {
        Transform t = paper.transform;
        Vector3 size = paper.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null ? filter.sharedMesh.bounds.size : Vector3.one;
        Vector3[] axes = { t.TransformVector(new Vector3(size.x, 0f, 0f)), t.TransformVector(new Vector3(0f, size.y, 0f)), t.TransformVector(new Vector3(0f, 0f, size.z)) };
        int thin = 0;
        for (int i = 1; i < 3; i++) if (axes[i].sqrMagnitude < axes[thin].sqrMagnitude) thin = i;
        int a = (thin + 1) % 3, b = (thin + 2) % 3;
        int vertical = Mathf.Abs(Vector3.Dot(axes[a].normalized, Vector3.up)) >= Mathf.Abs(Vector3.Dot(axes[b].normalized, Vector3.up)) ? a : b;
        int horizontal = vertical == a ? b : a;
        normal = axes[thin].normalized;
        if (Vector3.Dot(normal, toCamera) < 0f) normal = -normal;
        up = Vector3.ProjectOnPlane(axes[vertical], normal).normalized;
        if (Vector3.Dot(up, Vector3.up) < 0f) up = -up;
        width = axes[horizontal].magnitude;
        height = axes[vertical].magnitude;
        thickness = axes[thin].magnitude;
    }

    private static Vector3 PaperNormal(Transform space, GameObject note, Vector3 toward)
    {
        foreach (Renderer renderer in note.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.name.StartsWith(NoteMeshPrefix)) continue;
            PaperAxes(renderer, toward - renderer.bounds.center, out Vector3 normal, out _, out _, out _, out _);
            return space.InverseTransformDirection(normal);
        }
        return Vector3.back;
    }

    /// <summary>Bounds of the meshes under root (optionally only names starting with prefix) in space's local coordinates.</summary>
    private static Bounds LocalBounds(Transform space, GameObject root, string prefix, Transform exclude = null)
    {
        bool any = false;
        var bounds = new Bounds();
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            if (prefix != null && !filter.name.StartsWith(prefix)) continue;
            if (exclude != null && filter.transform.IsChildOf(exclude)) continue;
            Bounds mesh = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mesh.center + Vector3.Scale(mesh.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 local = space.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (!any) { bounds = new Bounds(local, Vector3.zero); any = true; }
                else bounds.Encapsulate(local);
            }
        }
        return bounds;
    }

    // =========================================================
    // STATE
    // =========================================================

    /// <summary>Which notes the corkboard shows (applied now and kept while no overlay owns the menu).</summary>
    public void Apply(CampaignMenuModel menu)
    {
        if (MenuEntry.IsStoryLaunching) return;
        model = menu;
        EnforceVisibility();
    }

    private bool OverlayOpen =>
        (settingsBoard != null && (settingsBoard.IsOpen || settingsBoard.MenuButtonsHidden)) || (TrainingBinder3D.Instance != null && TrainingBinder3D.Instance.IsOpen);

    private void EnforceVisibility()
    {
        if (model == null || OverlayOpen) return;
        SetActive(playNote, model.ShowPlay);
        SetActive(newGameNote, model.ShowNewGameAndContinue);
        SetActive(continueNote, model.ShowNewGameAndContinue);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active) target.SetActive(active);
    }

    /// <summary>The note dips into the board and springs back (any menu note).</summary>
    public bool PressNote(GameObject note, System.Action completed)
    {
        if (note == null || !note.activeInHierarchy || presses.Count != 0) return false;
        presses.Add(new Press { Note = note.transform, Rest = note.transform.localPosition, Completed = completed });
        return true;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying) return;
        // Settings / the training binder restore the notes they hid; keep the right set once they are gone.
        EnforceVisibility();
        // Same slow sine motion as career papers, bounded to a subtle angle.
        // Rotate around the paper normal; leave authored positions/scales alone.
        foreach (Sway sway in sways)
            if (sway.Note != null && sway.Note.gameObject.activeInHierarchy)
                sway.Note.localRotation = Quaternion.AngleAxis(
                    Mathf.Sin(Time.unscaledTime * (2f * Mathf.PI / 3.8f) + sway.Phase) * idleSwayDegrees,
                    sway.Axis) * sway.Rest;

        for (int i = presses.Count - 1; i >= 0; i--)
        {
            Press press = presses[i];
            if (press.Note == null) { presses.RemoveAt(i); continue; }
            press.Elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(press.Elapsed / PressDuration);
            Vector3 offset = press.Note.parent != null ? pressDirection : Vector3.zero;
            press.Note.localPosition = press.Rest + offset * Mathf.Sin(t * Mathf.PI);
            if (t >= 1f)
            {
                press.Note.localPosition = press.Rest;
                presses.RemoveAt(i);
                // Remove before invoking: actions may change camera/scene or
                // open an overlay. Only the animation's completion dispatches.
                press.Completed?.Invoke();
            }
        }
    }
}
