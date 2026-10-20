using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PURPOSE:
/// One-click scene update (user direction 2026-10-20): gives Office_Level2 the
/// same OUTSIDE environment as Office_Level1, without touching Level 2's
/// office, gameplay, data or scripts.
///
/// TaxVenture > Level 2 > Copy Outside Environment From Level 1:
/// 1. Replaces Office_Level2's "Env" root with a copy of Office_Level1's
///    (Unity's own copy/paste, so prefab links stay intact), minus Level 1's
///    environment NPCs (any object named npc_* or Elijah*).
/// 2. Matches the transforms of Environment/Outside, Outside/Plaza,
///    Outside/Floor and Outside/TownProgression to Level 1 (the ground the new
///    town sits on). Their children are not changed.
/// 3. Places Environment/Outside/Plaza/RosaStallAnchor in Level 2 where
///    NPC_Zorbix stands in Level 1. Level2RosaStall puts Rosa there, with her
///    records desk and sign beside her (no shop building).
/// 4. Model swap: Level 1's Marie gets Rosa's former model
///    (SM_Chr_Business_Female_03), fitted to Marie's height, keeping Marie's
///    object, position, scripts and idle animation. Level 2's Rosa uses
///    Marie's former model through Resources/Levels/Level_02/scene_assets.
///
/// Safe to run again: the Env copy is redone, the anchor is moved, and a Marie
/// already wearing the new model is left alone. Both scenes are saved; git
/// keeps the previous versions. The Office building, Level 2 gameplay
/// (built at runtime) and every data file stay as they are.
/// </summary>
public static class Level2EnvironmentSync
{
    private const string Level1Path = "Assets/Scenes/Office_Level1.unity";
    private const string Level2Path = "Assets/Scenes/Office_Level2.unity";
    private const string EnvRootName = "Env";
    private const string ZorbixPath = "Environment/Outside/Plaza/NPCS/NPC_Zorbix";
    private const string MarieVisualPath = "Environment/Outside/Plaza/NPCS/NPC_Marie/Visual";
    private const string PlazaPath = "Environment/Outside/Plaza";
    private const string AnchorName = "RosaStallAnchor";
    private const string MarieNewModelPath = "Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Business_Female_03.prefab";
    private const string RosaNewModelPath = "Assets/3D Models/npc_cora_static/npc_cora_static@Standing Idle.fbx";
    private const string SceneAssetsPath = "Assets/Resources/Levels/Level_02/scene_assets.asset";

    private static readonly string[] AlignedPaths =
    {
        "Environment/Outside", "Environment/Outside/Plaza", "Environment/Outside/Floor", "Environment/Outside/TownProgression"
    };

    [MenuItem("TaxVenture/Level 2/Copy Outside Environment From Level 1")]
    private static void Run()
    {
        if (!EditorUtility.DisplayDialog("Copy Outside Environment",
                "Office_Level2 gets Office_Level1's outside environment (Env), without Level 1's NPCs.\n\n" +
                "Rosa's stall moves to Zorbix's spot, and Rosa and Marie swap 3D models.\n\n" +
                "The office, gameplay and data are not changed. Both scenes are saved.",
                "Update", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var report = new StringBuilder();
        Scene level2 = EditorSceneManager.OpenScene(Level2Path, OpenSceneMode.Single);
        Scene level1 = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Additive);
        try
        {
            AlignOutside(level1, level2, report);
            PlaceAnchor(level1, level2, report);
            CopyEnv(level1, level2, report);
            SwapMarie(level1, report);
            SwapRosaAsset(report);

            EditorSceneManager.MarkSceneDirty(level1);
            EditorSceneManager.MarkSceneDirty(level2);
            EditorSceneManager.SaveScene(level1);
            EditorSceneManager.SaveScene(level2);
            report.AppendLine("Saved Office_Level1 and Office_Level2.");
        }
        finally
        {
            SceneManager.SetActiveScene(level2);
            EditorSceneManager.CloseScene(level1, true);
        }

        Debug.Log("[Level2EnvironmentSync]\n" + report);
        EditorUtility.DisplayDialog("Copy Outside Environment", report.ToString(), "OK");
    }

    // =========================================================
    // STEPS
    // =========================================================

    private static void AlignOutside(Scene level1, Scene level2, StringBuilder report)
    {
        foreach (string path in AlignedPaths)
        {
            Transform from = Find(level1, path), to = Find(level2, path);
            if (from == null || to == null)
            {
                report.AppendLine("Skipped (not found): " + path);
                continue;
            }
            to.localPosition = from.localPosition;
            to.localRotation = from.localRotation;
            to.localScale = from.localScale;
        }
        report.AppendLine("Matched the Outside, Plaza, Floor and TownProgression transforms to Level 1.");
    }

    private static void PlaceAnchor(Scene level1, Scene level2, StringBuilder report)
    {
        Transform zorbix = Find(level1, ZorbixPath);
        Transform plaza = Find(level2, PlazaPath);
        if (zorbix == null || plaza == null)
        {
            report.AppendLine("Rosa's anchor NOT placed: Zorbix or the Level 2 plaza is missing.");
            return;
        }
        Transform anchor = plaza.Find(AnchorName);
        if (anchor == null)
        {
            var go = new GameObject(AnchorName);
            SceneManager.MoveGameObjectToScene(go, level2);
            anchor = go.transform;
            anchor.SetParent(plaza, false);
        }
        anchor.SetPositionAndRotation(zorbix.position, Quaternion.Euler(0f, zorbix.eulerAngles.y, 0f));
        report.AppendLine($"Rosa's stall anchor placed at Zorbix's Level 1 spot {zorbix.position:F2}.");
    }

    private static void CopyEnv(Scene level1, Scene level2, StringBuilder report)
    {
        GameObject source = Root(level1, EnvRootName);
        GameObject old = Root(level2, EnvRootName);
        if (source == null)
        {
            report.AppendLine("Env NOT copied: Office_Level1 has no Env root.");
            return;
        }
        int index = old != null ? old.transform.GetSiblingIndex() : -1;

        Selection.objects = new Object[] { source };
        Unsupported.CopyGameObjectsToPasteboard();
        SceneManager.SetActiveScene(level2);
        Selection.objects = new Object[0];
        Unsupported.PasteGameObjectsFromPasteboard();
        GameObject copy = Selection.activeGameObject;
        if (copy == null || copy == source)
        {
            report.AppendLine("Env NOT copied: Unity's paste returned nothing. Copy Env by hand (Ctrl+C / Ctrl+V).");
            return;
        }
        if (copy.scene != level2)
        {
            copy.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(copy, level2);
        }
        if (old != null) Object.DestroyImmediate(old);
        copy.name = EnvRootName;
        if (index >= 0) copy.transform.SetSiblingIndex(index);
        Selection.objects = new Object[0];

        // Level 1's environment NPCs stay in Level 1.
        var npcs = new List<GameObject>();
        foreach (Transform t in copy.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            if (!n.StartsWith("npc_", System.StringComparison.OrdinalIgnoreCase) &&
                !n.StartsWith("Elijah", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (npcs.Exists(p => t.IsChildOf(p.transform))) continue;
            npcs.Add(t.gameObject);
        }
        int removed = 0;
        foreach (GameObject npc in npcs)
        {
            if (npc == null) continue;
            if (PrefabUtility.IsPartOfPrefabInstance(npc) && !PrefabUtility.IsOutermostPrefabInstanceRoot(npc))
            {
                npc.SetActive(false);   // inside another prefab: cannot delete, switch off
                continue;
            }
            Object.DestroyImmediate(npc);
            removed++;
        }
        report.AppendLine($"Env replaced with Level 1's; removed {removed} environment NPC(s).");
    }

    private static void SwapMarie(Scene level1, StringBuilder report)
    {
        Transform visual = Find(level1, MarieVisualPath);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MarieNewModelPath);
        if (visual == null || prefab == null)
        {
            report.AppendLine("Marie NOT swapped: her Visual or the new model is missing.");
            return;
        }
        foreach (Transform child in visual)
            if (child.name.StartsWith("SM_Chr_Business_Female_03"))
            {
                report.AppendLine("Marie already wears the new model; left as is.");
                return;
            }

        GameObject old = null;
        foreach (Transform child in visual)
            if (child.GetComponentInChildren<SkinnedMeshRenderer>(true) != null) { old = child.gameObject; break; }
        if (old == null)
        {
            report.AppendLine("Marie NOT swapped: no character model under her Visual.");
            return;
        }

        var fresh = (GameObject)PrefabUtility.InstantiatePrefab(prefab, level1);
        fresh.transform.SetParent(visual, false);
        fresh.transform.localPosition = old.transform.localPosition;
        fresh.transform.localRotation = old.transform.localRotation;
        fresh.transform.localScale = Vector3.one;
        fresh.transform.SetSiblingIndex(old.transform.GetSiblingIndex());

        // Same height and feet as Marie's old model.
        Bounds was = Bounds(old), now = Bounds(fresh);
        if (now.size.y > 0.01f) fresh.transform.localScale *= was.size.y / now.size.y;
        now = Bounds(fresh);
        fresh.transform.position += Vector3.up * (was.min.y - now.min.y);

        // Keep Marie's idle animation.
        Animator oldAnimator = old.GetComponentInChildren<Animator>(true);
        Animator newAnimator = fresh.GetComponentInChildren<Animator>(true);
        if (newAnimator != null && oldAnimator != null && oldAnimator.runtimeAnimatorController != null)
        {
            newAnimator.runtimeAnimatorController = oldAnimator.runtimeAnimatorController;
            newAnimator.applyRootMotion = oldAnimator.applyRootMotion;
        }

        int remapped = RemapReferences(level1, old, fresh, oldAnimator, newAnimator);
        Object.DestroyImmediate(old);
        report.AppendLine($"Marie now wears SM_Chr_Business_Female_03 (scripts kept; {remapped} reference(s) moved to the new model).");
    }

    private static void SwapRosaAsset(StringBuilder report)
    {
        var assets = AssetDatabase.LoadAssetAtPath<Level2SceneAssets>(SceneAssetsPath);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(RosaNewModelPath);
        if (assets == null || model == null)
        {
            report.AppendLine("Rosa's model NOT set: scene_assets or Marie's former model is missing.");
            return;
        }
        assets.rosaPrefab = model;
        EditorUtility.SetDirty(assets);
        AssetDatabase.SaveAssets();
        report.AppendLine("Level 2's Rosa uses Marie's former model (npc_cora_static).");
    }

    // =========================================================
    // HELPERS
    // =========================================================

    /// <summary>Points scene references at the old model (its root, Transform or Animator) to the new one.</summary>
    private static int RemapReferences(Scene scene, GameObject old, GameObject fresh, Animator oldAnimator, Animator newAnimator)
    {
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component.transform.IsChildOf(old.transform)) continue;
                var so = new SerializedObject(component);
                SerializedProperty p = so.GetIterator();
                bool changed = false;
                while (p.NextVisible(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue == null) continue;
                    Object value = p.objectReferenceValue;
                    Object replacement = null;
                    if (value == old) replacement = fresh;
                    else if (value == old.transform) replacement = fresh.transform;
                    else if (oldAnimator != null && value == oldAnimator) replacement = newAnimator;
                    if (replacement == null) continue;
                    p.objectReferenceValue = replacement;
                    changed = true;
                    count++;
                }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
        return count;
    }

    private static Bounds Bounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    private static GameObject Root(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }

    private static Transform Find(Scene scene, string path)
    {
        int slash = path.IndexOf('/');
        GameObject root = Root(scene, slash < 0 ? path : path.Substring(0, slash));
        if (root == null) return null;
        return slash < 0 ? root.transform : root.transform.Find(path.Substring(slash + 1));
    }
}
