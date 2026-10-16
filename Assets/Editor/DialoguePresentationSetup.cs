using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// T03 setup, applied to the user's open scene with Undo (no external scene edits):
///   Tools > TaxVenture > Configure Dialogue Presentation
///     - measures the posed Elijah model and the scene's NPCs,
///     - creates/updates the conversation-only Elijah visual,
///     - adds the camera director beside CameraController,
///     - restyles the dialogue panel into a compact bottom safe-area box.
///   Tools > TaxVenture > Toggle Conversation Visual Preview
///     - stands an unsaved Elijah preview by the player, facing the selected or
///       nearest NPC, to inspect grounding and scale in Edit mode.
/// Never changes the player root/collider, NPC transforms or FBX importers.
/// </summary>
public static class DialoguePresentationSetup
{
    private const string IdleModelGuid = "4d8c6cb1568c845489cd64e97a4ff7e6";   // npc_elijah_Idle.fbx
    private const string StaticModelGuid = "561657a2153bc56488613400339d7c82"; // npc_elijah_static.fbx
    private const string VisualName = "ConversationElijahVisual";
    private const string PreviewName = "ConversationElijahPreview (unsaved)";
    private const float FallbackHeight = 1.75f;

    private struct PosedMeasurement
    {
        public float MinY;
        public float MaxY;
        public float HeadFraction;
        public float YawOffset;
        public string HeadSource;
        public string FacingSource;
    }

    // ------------------------------------------------------------------
    // Configure
    // ------------------------------------------------------------------

    [MenuItem("Tools/TaxVenture/Configure Dialogue Presentation")]
    private static void Configure()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Stop Play mode before configuring the dialogue presentation.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        CameraController owner = Single<CameraController>(scene, "CameraController");
        DialogueUI ui = Single<DialogueUI>(scene, "DialogueUI");
        if (owner == null || ui == null) return;

        var uiData = new SerializedObject(ui);
        GameObject panel = uiData.FindProperty("panelRoot").objectReferenceValue as GameObject;
        var dialogueText = uiData.FindProperty("dialogueText").objectReferenceValue as TextMeshProUGUI;
        var speakerText = uiData.FindProperty("speakerNameText").objectReferenceValue as TextMeshProUGUI;
        var continueButton = uiData.FindProperty("continueButton").objectReferenceValue as Button;
        var portrait = uiData.FindProperty("portraitImage").objectReferenceValue as Image;
        if (panel == null || dialogueText == null || continueButton == null)
        {
            Debug.LogError("DialogueUI is missing its panel, dialogue text or Continue button reference. No changes applied.");
            return;
        }

        if (!LoadElijah(out GameObject model, out AnimationClip clip, out string modelSource)) return;
        if (!MeasurePosedModel(model, clip, 0f, out PosedMeasurement posed))
        {
            Debug.LogError("Could not measure the posed Elijah geometry (no renderers/vertices). No changes applied.");
            return;
        }

        var report = new StringBuilder();
        List<string> npcLines = MeasureNpcs(scene, out float medianNpcHeight, out int measuredCount);

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure TaxVenture dialogue presentation");

        // 1. Conversation-only Elijah visual (scene root, unscaled, no colliders).
        ConversationActorVisual visual = FindInScene<ConversationActorVisual>(scene);
        if (visual == null)
        {
            var go = new GameObject(VisualName);
            Undo.RegisterCreatedObjectUndo(go, "Create conversation Elijah visual");
            SceneManager.MoveGameObjectToScene(go, scene);
            visual = Undo.AddComponent<ConversationActorVisual>(go);
        }
        Undo.RecordObject(visual.transform, "Reset conversation visual transform");
        visual.transform.localScale = Vector3.one;

        var visualData = new SerializedObject(visual);
        visualData.FindProperty("modelPrefab").objectReferenceValue = model;
        visualData.FindProperty("poseClip").objectReferenceValue = clip;
        visualData.FindProperty("poseTime").floatValue = 0f;
        visualData.FindProperty("nativeMinY").floatValue = posed.MinY;
        visualData.FindProperty("nativeMaxY").floatValue = posed.MaxY;
        visualData.FindProperty("headHeightFraction").floatValue = Mathf.Clamp(posed.HeadFraction, 0.5f, 1f);
        visualData.FindProperty("forwardYawOffset").floatValue = posed.YawOffset;
        SerializedProperty height = visualData.FindProperty("standingHeight");
        bool heightProposed = height.floatValue <= 0f;
        if (heightProposed) height.floatValue = measuredCount > 0 ? medianNpcHeight : FallbackHeight;
        visualData.ApplyModifiedProperties();

        // 2. Camera director under the existing camera owner.
        DialogueCameraDirector director = owner.GetComponent<DialogueCameraDirector>();
        if (director == null) director = Undo.AddComponent<DialogueCameraDirector>(owner.gameObject);
        var directorData = new SerializedObject(director);
        directorData.FindProperty("dialogueUI").objectReferenceValue = ui;
        directorData.FindProperty("playerVisual").objectReferenceValue = visual;
        directorData.ApplyModifiedProperties();

        // 3. Compact bottom dialogue panel.
        string panelResult = StylePanel(ui, panel, portrait, speakerText, dialogueText, continueButton);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = visual.gameObject;

        float nativeHeight = posed.MaxY - posed.MinY;
        float standing = visualData.FindProperty("standingHeight").floatValue;
        report.AppendLine("Dialogue presentation configured. Save the scene (Ctrl+S), then test in Play mode.");
        report.AppendLine($"Elijah model: {modelSource}{(clip != null ? $", pose clip '{clip.name}' ({clip.length:0.00}s) sampled at 0s" : ", static (no clip)")}.");
        report.AppendLine($"Posed native geometry: minY {posed.MinY:0.###}, maxY {posed.MaxY:0.###}, height {nativeHeight:0.###} (model units at scale 1).");
        report.AppendLine($"Head/eye height fraction {posed.HeadFraction:0.###} ({posed.HeadSource}); facing yaw offset {posed.YawOffset:0.#}° ({posed.FacingSource}).");
        report.AppendLine($"Standing height {standing:0.###} world units ({(heightProposed ? (measuredCount > 0 ? $"proposed: median of {measuredCount} NPCs" : "fallback, no NPC measured") : "kept existing value")}); resulting model scale {standing / Mathf.Max(1e-4f, nativeHeight):0.#####}.");
        report.AppendLine("NPC measurements (renderer bounds unless an anchor is present):");
        foreach (string line in npcLines) report.AppendLine("  " + line);
        report.AppendLine(panelResult);
        report.AppendLine("Player root, collider, NPC transforms and FBX importers were not changed. Use Tools > TaxVenture > Toggle Conversation Visual Preview to inspect grounding.");
        Debug.Log(report.ToString(), visual);
    }

    // ------------------------------------------------------------------
    // Preview
    // ------------------------------------------------------------------

    [MenuItem("Tools/TaxVenture/Toggle Conversation Visual Preview")]
    private static void TogglePreview()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("The preview is for Edit mode; in Play mode talk to an NPC instead.");
            return;
        }

        GameObject existing = GameObject.Find(PreviewName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
            Debug.Log("Conversation visual preview removed.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        ConversationActorVisual visual = FindInScene<ConversationActorVisual>(scene);
        CameraController owner = Single<CameraController>(scene, "CameraController");
        if (visual == null || owner == null)
        {
            Debug.LogError("Run Tools > TaxVenture > Configure Dialogue Presentation first.");
            return;
        }

        var data = new SerializedObject(visual);
        var model = data.FindProperty("modelPrefab").objectReferenceValue as GameObject;
        var clip = data.FindProperty("poseClip").objectReferenceValue as AnimationClip;
        float poseTime = data.FindProperty("poseTime").floatValue;
        float standing = data.FindProperty("standingHeight").floatValue;
        float minY = data.FindProperty("nativeMinY").floatValue;
        float maxY = data.FindProperty("nativeMaxY").floatValue;
        float yawOffset = data.FindProperty("forwardYawOffset").floatValue;
        if (model == null || maxY <= minY || standing <= 0f)
        {
            Debug.LogError("The conversation visual is not measured yet. Run Configure Dialogue Presentation first.");
            return;
        }

        var player = new SerializedObject(owner).FindProperty("playerMovement").objectReferenceValue as Component;
        if (player == null)
        {
            Debug.LogError("CameraController has no PlayerMovement reference to stand the preview by.");
            return;
        }

        Component npcOwner = PickNpc(scene, player.transform.position);
        Vector3 floor = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
        string npcInfo = "no NPC found; facing the player's forward";
        float yaw = player.transform.eulerAngles.y;
        float npcHeight = standing;
        if (npcOwner != null && DialogueSpeakerFrames.TryResolve(npcOwner, out DialogueSpeakerFrame npc))
        {
            npcHeight = npc.Height;
            float scaleRef = (npc.Height + standing) * 0.5f;
            floor = Floor(player.transform, npc.Floor.y, scaleRef);
            System.Numerics.Vector3 placed = DialogueShotMath.PlaceListener(
                new System.Numerics.Vector3(floor.x, floor.y, floor.z),
                new System.Numerics.Vector3(npc.Floor.x, npc.Floor.y, npc.Floor.z),
                0.5f * scaleRef, 0.85f * scaleRef, out bool moved);
            floor = new Vector3(placed.X, placed.Y, placed.Z);
            yaw = DialogueShotMath.YawDegrees(new System.Numerics.Vector3(npc.Floor.x - floor.x, 0f, npc.Floor.z - floor.z));
            npcInfo = $"facing '{npcOwner.name}' (height {npc.Height:0.###}{(moved ? ", placement clamped toward a readable separation" : "")})";
        }

        var root = new GameObject(PreviewName) { hideFlags = HideFlags.DontSave };
        root.transform.SetPositionAndRotation(floor, Quaternion.Euler(0f, yaw, 0f));
        var instance = (GameObject)Object.Instantiate(model, root.transform, false);
        instance.hideFlags = HideFlags.DontSave;
        foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        if (clip != null) clip.SampleAnimation(instance, poseTime);
        ConversationActorVisual.ComputeModelLocal(standing, minY, maxY, yawOffset,
            out Vector3 localPosition, out Quaternion localRotation, out float scale);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        instance.transform.localScale = Vector3.one * scale;

        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        Debug.Log($"Conversation visual preview: standing height {standing:0.###} (NPC {npcHeight:0.###}), scale {scale:0.#####}, feet on y {floor.y:0.###}, {npcInfo}. Run the menu again to remove it; it is never saved.", root);
    }

    // ------------------------------------------------------------------
    // Measurement
    // ------------------------------------------------------------------

    private static bool LoadElijah(out GameObject model, out AnimationClip clip, out string source)
    {
        model = null;
        clip = null;
        source = null;
        string idlePath = AssetDatabase.GUIDToAssetPath(IdleModelGuid);
        string staticPath = AssetDatabase.GUIDToAssetPath(StaticModelGuid);

        if (!string.IsNullOrEmpty(idlePath))
        {
            model = AssetDatabase.LoadAssetAtPath<GameObject>(idlePath);
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(idlePath))
                if (asset is AnimationClip candidate && !candidate.name.StartsWith("__preview__"))
                {
                    clip = candidate;
                    break;
                }
            source = idlePath;
        }

        if (model == null && !string.IsNullOrEmpty(staticPath))
        {
            model = AssetDatabase.LoadAssetAtPath<GameObject>(staticPath);
            clip = null;
            source = staticPath;
        }

        if (model == null)
        {
            Debug.LogError("Existing Elijah model not found (npc_elijah_Idle.fbx / npc_elijah_static.fbx). The walking model is deliberately not used as a standing pose. No changes applied.");
            return false;
        }
        return true;
    }

    private static bool MeasurePosedModel(GameObject model, AnimationClip clip, float time, out PosedMeasurement result)
    {
        result = default;
        var temp = (GameObject)Object.Instantiate(model);
        temp.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            temp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            temp.transform.localScale = Vector3.one;
            foreach (Animator animator in temp.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            if (clip != null)
            {
                clip.SampleAnimation(temp, time);
                temp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                temp.transform.localScale = Vector3.one;
            }

            float minY = float.MaxValue, maxY = float.MinValue;
            Bounds head = default, face = default;
            bool hasHead = false, hasFace = false;
            string faceName = null;
            var vertices = new List<Vector3>(2048);

            foreach (Renderer renderer in temp.GetComponentsInChildren<Renderer>(true))
            {
                vertices.Clear();
                if (!CollectWorldVertices(renderer, vertices) || vertices.Count == 0) continue;
                Bounds b = new Bounds(vertices[0], Vector3.zero);
                foreach (Vector3 v in vertices)
                {
                    b.Encapsulate(v);
                    minY = Mathf.Min(minY, v.y);
                    maxY = Mathf.Max(maxY, v.y);
                }

                string n = renderer.gameObject.name.ToLowerInvariant();
                if (n.Contains("head")) { head = hasHead ? Encapsulated(head, b) : b; hasHead = true; }
                if (n.Contains("eye") || n.Contains("mouth"))
                {
                    face = hasFace ? Encapsulated(face, b) : b;
                    hasFace = true;
                    faceName = faceName == null ? renderer.gameObject.name : faceName + "+" + renderer.gameObject.name;
                }
            }

            if (maxY <= minY) return false;
            float height = maxY - minY;
            result.MinY = minY;
            result.MaxY = maxY;

            if (hasFace)
            {
                result.HeadFraction = (face.center.y - minY) / height;
                result.HeadSource = "measured at " + faceName;
            }
            else if (hasHead)
            {
                result.HeadFraction = (head.center.y - minY) / height;
                result.HeadSource = "measured at head mesh centre";
            }
            else
            {
                result.HeadFraction = 0.9f;
                result.HeadSource = "default, no head mesh found";
            }

            if (hasFace && hasHead)
            {
                Vector3 forward = face.center - head.center;
                forward.y = 0f;
                if (forward.sqrMagnitude > 1e-8f)
                {
                    // Rotate the model so its face points along +Z of the visual root.
                    result.YawOffset = -Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                    result.FacingSource = $"face ({faceName}) relative to head";
                }
                else result.FacingSource = "face centred on head; assumed +Z";
            }
            else result.FacingSource = "no face/head meshes; assumed +Z";
            return true;
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }
    }

    private static Bounds Encapsulated(Bounds a, Bounds b)
    {
        a.Encapsulate(b);
        return a;
    }

    // Exact posed vertices: skinned meshes are skinned on the CPU from their
    // bones and bind poses (no reliance on cached renderer bounds).
    private static bool CollectWorldVertices(Renderer renderer, List<Vector3> output)
    {
        if (renderer is SkinnedMeshRenderer skinned)
        {
            Mesh mesh = skinned.sharedMesh;
            if (mesh == null) return false;
            Vector3[] vertices = mesh.vertices;
            Transform[] bones = skinned.bones;
            Matrix4x4[] bindposes = mesh.bindposes;
            BoneWeight[] weights = mesh.boneWeights;
            if (bones == null || bones.Length == 0 || weights == null || weights.Length != vertices.Length ||
                bindposes == null || bindposes.Length != bones.Length)
            {
                Matrix4x4 rigid = skinned.transform.localToWorldMatrix;
                foreach (Vector3 v in vertices) output.Add(rigid.MultiplyPoint3x4(v));
                return true;
            }

            var skin = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++)
                skin[i] = bones[i] != null ? bones[i].localToWorldMatrix * bindposes[i] : skinned.transform.localToWorldMatrix;

            for (int i = 0; i < vertices.Length; i++)
            {
                BoneWeight w = weights[i];
                Vector3 v = vertices[i];
                Vector3 p = Vector3.zero;
                if (w.weight0 > 0f) p += skin[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0;
                if (w.weight1 > 0f) p += skin[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1;
                if (w.weight2 > 0f) p += skin[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2;
                if (w.weight3 > 0f) p += skin[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
                float total = w.weight0 + w.weight1 + w.weight2 + w.weight3;
                output.Add(total > 1e-5f ? p / total : skinned.transform.localToWorldMatrix.MultiplyPoint3x4(v));
            }
            return true;
        }

        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return false;
        Matrix4x4 matrix = renderer.transform.localToWorldMatrix;
        foreach (Vector3 v in filter.sharedMesh.vertices) output.Add(matrix.MultiplyPoint3x4(v));
        return true;
    }

    private static List<string> MeasureNpcs(Scene scene, out float median, out int count)
    {
        var lines = new List<string>();
        var heights = new List<float>();
        foreach (Component npc in AllNpcs(scene))
        {
            if (DialogueSpeakerFrames.TryResolve(npc, out DialogueSpeakerFrame frame))
            {
                bool anchored = npc.GetComponentInParent<DialogueSpeakerAnchor>() != null;
                lines.Add($"{npc.GetType().Name} '{npc.name}': height {frame.Height:0.###}, floor y {frame.Floor.y:0.###}{(anchored ? " (anchor)" : "")}");
                // The guide is usually hidden in Edit mode; keep it out of the median.
                if (!(npc is TutorialNpcController)) heights.Add(frame.Height);
            }
            else
            {
                lines.Add($"{npc.GetType().Name} '{npc.name}': not measurable in Edit mode (no active renderers/colliders); runtime will measure it when visible.");
            }
        }
        heights.Sort();
        count = heights.Count;
        median = count == 0 ? 0f : count % 2 == 1 ? heights[count / 2] : 0.5f * (heights[count / 2 - 1] + heights[count / 2]);
        if (lines.Count == 0) lines.Add("No NPC dialogue owners found in the active scene.");
        return lines;
    }

    private static IEnumerable<Component> AllNpcs(Scene scene)
    {
        foreach (Component c in Find<TaxpayerNpcInteractable>(scene)) yield return c;
        foreach (Component c in Find<ReceptionistInteractable>(scene)) yield return c;
        foreach (Component c in Find<ClientInteractable>(scene)) yield return c;
        foreach (Component c in Find<AuditorInteractable>(scene)) yield return c;
        foreach (Component c in Find<ColleagueInteractable>(scene)) yield return c;
        foreach (Component c in Find<TutorialNpcController>(scene)) yield return c;
    }

    private static Component PickNpc(Scene scene, Vector3 from)
    {
        if (Selection.activeGameObject != null)
            foreach (Component c in AllNpcs(scene))
                if (c.gameObject == Selection.activeGameObject || c.transform.IsChildOf(Selection.activeGameObject.transform) ||
                    Selection.activeGameObject.transform.IsChildOf(c.transform))
                    return c;

        Component best = null;
        float bestDistance = float.MaxValue;
        foreach (Component c in AllNpcs(scene))
        {
            float d = (c.transform.position - from).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = c; }
        }
        return best;
    }

    private static Vector3 Floor(Transform player, float fallbackY, float scaleRef)
    {
        Vector3 start = player.position + Vector3.up * (0.1f * scaleRef);
        float reach = 3f * scaleRef;
        Vector3 floor = new Vector3(player.position.x, fallbackY, player.position.z);
        float best = float.MaxValue;
        foreach (RaycastHit hit in Physics.RaycastAll(start, Vector3.down, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(player)) continue;
            if (hit.distance < best) { best = hit.distance; floor = hit.point; }
        }
        return floor;
    }

    // ------------------------------------------------------------------
    // Panel
    // ------------------------------------------------------------------

    private static string StylePanel(DialogueUI ui, GameObject panel, Image portrait,
        TextMeshProUGUI speaker, TextMeshProUGUI body, Button continueButton)
    {
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        RectTransform box = body.transform.parent as RectTransform;
        if (panelRect == null || box == null || box == panelRect ||
            (speaker != null && speaker.transform.parent != box) || continueButton.transform.parent != box)
        {
            return "Panel layout NOT changed: the dialogue text, speaker name and Continue button are expected to share one box under the panel. Typewriter and gestures still work.";
        }

        // Full-width, invisible strip that keeps taps off the world; the visible
        // box sits inside it. Safe area keeps both inside notches/home bars.
        Undo.RecordObject(panelRect, "Dialogue panel layout");
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(0f, 300f);
        panelRect.anchoredPosition = Vector2.zero;
        Image strip = panel.GetComponent<Image>();
        if (strip != null)
        {
            Undo.RecordObject(strip, "Dialogue panel strip");
            Color c = strip.color;
            c.a = 0f;
            strip.color = c;
            strip.raycastTarget = true;
        }
        if (panel.GetComponent<SafeAreaBottomPanel>() == null) Undo.AddComponent<SafeAreaBottomPanel>(panel);

        if (portrait != null)
        {
            Undo.RecordObject(portrait.gameObject, "Hide oversized portrait");
            portrait.gameObject.SetActive(false);
        }

        Undo.RecordObject(box, "Dialogue box layout");
        box.anchorMin = new Vector2(0.12f, 0f);
        box.anchorMax = new Vector2(0.88f, 0f);
        box.pivot = new Vector2(0.5f, 0f);
        box.sizeDelta = new Vector2(0f, 250f);
        box.anchoredPosition = new Vector2(0f, 24f);
        Image boxImage = box.GetComponent<Image>();
        if (boxImage != null)
        {
            Undo.RecordObject(boxImage, "Dialogue box colour");
            boxImage.color = new Color(0.07f, 0.08f, 0.11f, 0.92f);
            boxImage.raycastTarget = true;
        }
        DialogueBoxTapRelay relay = box.GetComponent<DialogueBoxTapRelay>();
        if (relay == null) relay = Undo.AddComponent<DialogueBoxTapRelay>(box.gameObject);
        var relayData = new SerializedObject(relay);
        relayData.FindProperty("dialogueUI").objectReferenceValue = ui;
        relayData.ApplyModifiedProperties();

        if (speaker != null)
        {
            Undo.RecordObject(speaker, "Speaker name style");
            Undo.RecordObject(speaker.rectTransform, "Speaker name layout");
            RectTransform r = speaker.rectTransform;
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.sizeDelta = new Vector2(-64f, 44f);
            r.anchoredPosition = new Vector2(32f, -16f);
            speaker.fontSize = 30f;
            speaker.enableAutoSizing = false;
            speaker.fontStyle = FontStyles.Bold;
            speaker.alignment = TextAlignmentOptions.TopLeft;
            speaker.color = new Color(1f, 0.82f, 0.45f, 1f);
            speaker.raycastTarget = false;
        }

        Undo.RecordObject(body, "Dialogue text style");
        Undo.RecordObject(body.rectTransform, "Dialogue text layout");
        RectTransform t = body.rectTransform;
        t.anchorMin = Vector2.zero;
        t.anchorMax = Vector2.one;
        t.pivot = new Vector2(0.5f, 0.5f);
        t.offsetMin = new Vector2(32f, 76f);
        t.offsetMax = new Vector2(-32f, -64f);
        body.enableAutoSizing = true;
        body.fontSizeMin = 22f;
        body.fontSizeMax = 32f;
        body.fontSize = 32f;
        body.alignment = TextAlignmentOptions.TopLeft;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.color = new Color(0.95f, 0.96f, 0.98f, 1f);
        body.raycastTarget = false;

        RectTransform buttonRect = continueButton.GetComponent<RectTransform>();
        Undo.RecordObject(buttonRect, "Continue layout");
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.sizeDelta = new Vector2(200f, 52f);
        buttonRect.anchoredPosition = new Vector2(-20f, 14f);
        Image buttonImage = continueButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            Undo.RecordObject(buttonImage, "Continue colour");
            buttonImage.color = new Color(0.95f, 0.8f, 0.43f, 1f);
        }
        TextMeshProUGUI label = continueButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            Undo.RecordObject(label, "Continue label");
            label.fontSize = 24f;
            label.color = new Color(0.08f, 0.08f, 0.1f, 1f);
            label.raycastTarget = false;
        }

        return "Panel restyled: invisible full-width safe-area strip (300 high at 1920x1080 reference), dark box 76% wide, speaker name top-left, auto-sized text 22–32, Continue bottom-right; oversized portrait hidden (its object and DialogueUI reference are kept).";
    }

    // ------------------------------------------------------------------
    // Scene helpers
    // ------------------------------------------------------------------

    private static T Single<T>(Scene scene, string label) where T : Component
    {
        T found = null;
        foreach (T candidate in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate.gameObject.scene != scene) continue;
            if (found != null)
            {
                Debug.LogError($"Multiple {label} components in the active scene; configure one first. No changes applied.");
                return null;
            }
            found = candidate;
        }
        if (found == null) Debug.LogError($"Open Office_Level1 first ({label} missing). No changes applied.");
        return found;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (T candidate in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (candidate.gameObject.scene == scene) return candidate;
        return null;
    }

    private static IEnumerable<T> Find<T>(Scene scene) where T : Component
    {
        foreach (T candidate in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (candidate.gameObject.scene == scene) yield return candidate;
    }
}
