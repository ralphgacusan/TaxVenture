using UnityEditor;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Milestone T03 third-person repair: Editor-only review aids for the user's native acceptance in
/// Office_Level1 Play Mode (never in a build; no scene, save or asset changes):
/// - Stand Beside Nearest Adult: puts Elijah ~1.2 m beside the nearest ordinary adult NPC on the
///   same floor and turns the view so both stand at EQUAL camera depth, then logs both heights.
/// - Framing Guides: overlays the composition targets inside the camera's real gameplay rectangle
///   (centre line, feet band 88-93%, body 38-46%) with a live readout of Elijah's projection.
/// - Slow Motion: 0.25x time for reviewing starts, stops, turns, walk/run blends and jumps.
/// </summary>
public static class T03ExperienceReview
{
    private const string Menu = "TaxVenture/T03 Third-Person Review/";
    private static readonly string[] Adults = { "NPC_Marie", "NPC_Jhun", "NPC_Milo", "Ambient_Bea", "Ambient_Carlo", "Ambient_Aling_Lorna", "NPC_TutorialGuide" };

    [MenuItem(Menu + "Stand Beside Nearest Adult (equal depth)")]
    private static void StandBeside()
    {
        CameraController owner = CameraController.Instance;
        PlayerMovement mover = owner != null ? owner.Movement : null;
        FirstPersonCameraController writer = Camera.main != null ? Camera.main.GetComponent<FirstPersonCameraController>() : null;
        if (!EditorApplication.isPlaying || mover == null || writer == null || !owner.IsThirdPersonView)
        {
            Debug.LogWarning("[T03 Review] Enter Play Mode in Office_Level1 and walk outdoors first.");
            return;
        }
        Transform npc = null; float best = float.MaxValue;
        foreach (string name in Adults)
        {
            GameObject go = GameObject.Find(name);
            if (go == null || !go.activeInHierarchy) continue;
            float d = (go.transform.position - mover.transform.position).sqrMagnitude;
            if (d < best) { best = d; npc = go.transform; }
        }
        if (npc == null) { Debug.LogWarning("[T03 Review] No ordinary adult NPC is active nearby."); return; }
        // Side by side, perpendicular to the view: equal depth from the camera.
        Vector3 side = Vector3.Cross(Vector3.up, npc.forward).normalized;
        Vector3 spot = npc.position + side * 1.2f + Vector3.up * 1.5f;
        if (Physics.Raycast(spot, Vector3.down, out RaycastHit floor, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            spot = floor.point + Vector3.up * (mover.transform.position.y - (owner.Body != null ? owner.Body.Feet.y : mover.transform.position.y));
        Rigidbody rb = mover.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;
        if (rb != null) rb.position = spot;
        mover.transform.position = spot;
        Vector3 view = -npc.forward; // look at both from the NPC's front side
        writer.SetLook(Mathf.Atan2(-view.x, -view.z) * Mathf.Rad2Deg, 0f);
        float npcHeight = 0f;
        foreach (Renderer r in npc.GetComponentsInChildren<Renderer>())
            if (r.enabled && r.gameObject.activeInHierarchy) npcHeight = Mathf.Max(npcHeight, r.bounds.max.y - floor.point.y);
        Debug.Log($"[T03 Review] Elijah beside {npc.name} at equal depth. Elijah canonical {ExplorationRules.ElijahRestHeight:0.000} m " +
                  $"(world scale {(owner.Body != null ? owner.Body.WorldScale : 0f):0.00000}); {npc.name} renderer top ~{npcHeight:0.000} m above the floor (posed, incl. hair). " +
                  "Compare silhouettes in the Game view; a ±5% difference is expected across the cast.");
    }

    [MenuItem(Menu + "Toggle Framing Guides")]
    private static void ToggleGuides()
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("[T03 Review] Framing guides are a Play Mode overlay."); return; }
        FramingGuides existing = Object.FindAnyObjectByType<FramingGuides>();
        if (existing != null) { Object.Destroy(existing.gameObject); return; }
        var go = new GameObject("T03 Framing Guides (review only)") { hideFlags = HideFlags.DontSave };
        go.AddComponent<FramingGuides>();
    }

    [MenuItem(Menu + "Toggle Slow Motion (0.25x)")]
    private static void ToggleSlow()
    {
        if (!EditorApplication.isPlaying) return;
        Time.timeScale = Mathf.Approximately(Time.timeScale, 0.25f) ? 1f : 0.25f;
        Debug.Log($"[T03 Review] Time scale {Time.timeScale:0.00}");
    }

    /// <summary>Composition guides drawn inside the camera's gameplay rectangle (no editor chrome, no letterbox).</summary>
    private sealed class FramingGuides : MonoBehaviour
    {
        private static Texture2D pixel;

        private void OnGUI()
        {
            Camera cam = Camera.main;
            CameraController owner = CameraController.Instance;
            if (cam == null || owner == null || owner.Body == null) return;
            if (pixel == null) { pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply(); }
            Rect r = cam.pixelRect;
            // GUI space is top-down; the camera rect is bottom-up.
            float top = Screen.height - r.yMax;
            void Line(float x, float y, float w, float h, Color c) { GUI.color = c; GUI.DrawTexture(new Rect(r.x + x, top + y, w, h), pixel); }
            Color guide = new Color(0.2f, 1f, 0.6f, 0.6f), band = new Color(1f, 0.85f, 0.2f, 0.25f);
            Line(r.width * 0.5f - 1f, 0f, 2f, r.height, guide);
            Line(0f, r.height * 0.88f, r.width, r.height * 0.05f, band);
            Vector3 feet = cam.WorldToViewportPoint(owner.Body.Feet);
            Vector3 head = cam.WorldToViewportPoint(owner.Body.Feet + Vector3.up * owner.Body.StandingHeight);
            float feetFromTop = 1f - feet.y, headFromTop = 1f - head.y;
            Line(0f, r.height * headFromTop, r.width, 2f, Color.cyan);
            Line(0f, r.height * feetFromTop, r.width, 2f, Color.cyan);
            GUI.color = Color.white;
            GUI.Label(new Rect(r.x + 12f, top + 12f, 520f, 60f),
                $"Gameplay rect {r.width:0}x{r.height:0}  centre x {feet.x:0.000} (target 0.50±0.03)\n" +
                $"feet {feetFromTop:0.000} from top (0.88-0.93)  body {feetFromTop - headFromTop:0.000} of height (0.38-0.46)  depth {feet.z:0.00} m");
        }
    }
}
