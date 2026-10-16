using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// World Animation T01 setup with Undo: Tools > TaxVenture > Configure NPC Animation.
/// 1. Imports the unused Jhun/Cora walking FBXs as Humanoid looping in-place clips
///    and adds a "Walking" state to the matching idle controllers.
/// 2. Binds Walking on every NpcAnimationDriver whose controller has that state.
/// 3. Adds NpcIdlePacing to the plaza NPCs that have a walk clip.
/// Changes no collider, dialogue or quest data. Save the scene afterwards.
/// </summary>
public static class NpcAnimationSetup
{
    private const string WalkingState = "Walking";
    private const int WalkingBehavior = 10; // NpcAnimationDriver.Behavior.Walking

    private static readonly (string walkFbx, string controller)[] WalkClips =
    {
        ("Assets/3D Models/npc_jhun_static/npc_jhun_walking.fbx", "Assets/3D Models/npc_jhun_static/npc_jhun_static@Standing Idle.controller"),
        ("Assets/3D Models/npc_cora_static/npc_cora_walking.fbx", "Assets/3D Models/npc_cora_static/npc_cora_static@Standing Idle.controller"),
    };

    // Only NPCs with a real walk clip; static meshes would slide.
    private static readonly string[] PacingNpcs = { "NPC_Jhun", "NPC_Zorbix", "NPC_Marie" };

    [MenuItem("Tools/TaxVenture/Configure NPC Animation")]
    private static void Configure()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Stop Play mode before configuring NPC animation.");
            return;
        }

        var report = new StringBuilder("NPC animation setup:\n");
        var walkSpeeds = new Dictionary<RuntimeAnimatorController, float>();
        foreach (var (walkFbx, controllerPath) in WalkClips)
        {
            AnimationClip clip = ImportHumanoidWalk(walkFbx, report);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (clip == null || controller == null)
            {
                report.AppendLine($"  SKIPPED {controllerPath}: missing clip or controller.");
                continue;
            }
            AddWalkingState(controller, clip, report);
            Vector3 speed = clip.averageSpeed;
            speed.y = 0f;
            walkSpeeds[controller] = Mathf.Clamp(speed.magnitude, 0.6f, 1.6f);
        }
        AssetDatabase.SaveAssets();

        Scene scene = SceneManager.GetActiveScene();
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure TaxVenture NPC animation");

        int drivers = 0;
        foreach (NpcAnimationDriver driver in Object.FindObjectsByType<NpcAnimationDriver>(FindObjectsInactive.Include))
        {
            if (driver.gameObject.scene != scene) continue;
            drivers++;
            var data = new SerializedObject(driver);
            Animator animator = FindAnimator(driver, data);
            if (animator != null && animator.runtimeAnimatorController is AnimatorController controller &&
                HasState(controller, WalkingState))
            {
                if (BindWalking(data))
                    report.AppendLine($"  {driver.name}: Walking bound.");
            }
            // Earlier scene values (1.2 degrees) were too subtle to notice.
            SerializedProperty headMotion = data.FindProperty("headMotionDegrees");
            if (headMotion.floatValue < 3.5f) headMotion.floatValue = 3.5f;
            data.ApplyModifiedProperties();

            if (System.Array.IndexOf(PacingNpcs, driver.name) < 0) continue;
            NpcIdlePacing pacing = driver.GetComponent<NpcIdlePacing>();
            if (pacing == null)
            {
                pacing = Undo.AddComponent<NpcIdlePacing>(driver.gameObject);
                if (animator != null && animator.runtimeAnimatorController != null &&
                    walkSpeeds.TryGetValue(animator.runtimeAnimatorController, out float walkSpeed))
                {
                    var pacingData = new SerializedObject(pacing);
                    pacingData.FindProperty("walkSpeed").floatValue = walkSpeed;
                    pacingData.ApplyModifiedPropertiesWithoutUndo();
                }
                report.AppendLine($"  {driver.name}: NpcIdlePacing added.");
            }
        }

        if (drivers == 0)
        {
            Debug.LogError("No NpcAnimationDriver found in the active scene. Open Office_Level1 first. Asset import changes were still applied.");
            return;
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        report.AppendLine("Save the scene (Ctrl+S) to keep the scene changes.");
        Debug.Log(report.ToString());
    }

    private static AnimationClip ImportHumanoidWalk(string path, StringBuilder report)
    {
        if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
        {
            report.AppendLine($"  SKIPPED {path}: not found.");
            return null;
        }
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
        }

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.loopTime = true;
            // Root rotation and height stay baked; forward root motion is
            // discarded (Apply Root Motion is off), so the clip walks in place
            // while NpcIdlePacing moves the NPC.
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = true;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = true;
            clip.lockRootPositionXZ = false;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        Avatar avatar = assets.OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
        {
            report.AppendLine($"  FAILED {path}: Humanoid avatar is invalid; check the FBX Rig tab.");
            return null;
        }
        AnimationClip result = assets.OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        report.AppendLine(result != null ? $"  {path}: Humanoid walk clip '{result.name}'." : $"  FAILED {path}: no clip.");
        return result;
    }

    private static void AddWalkingState(AnimatorController controller, AnimationClip clip, StringBuilder report)
    {
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == WalkingState);
        if (state == null)
            state = machine.AddState(WalkingState);
        Undo.RecordObject(state, "Assign walking clip");
        state.motion = clip;
        EditorUtility.SetDirty(controller);
        report.AppendLine($"  {controller.name}: '{WalkingState}' state uses '{clip.name}'.");
    }

    private static bool HasState(AnimatorController controller, string name) =>
        controller.layers.Length > 0 && controller.layers[0].stateMachine.states.Any(s => s.state.name == name);

    private static Animator FindAnimator(NpcAnimationDriver driver, SerializedObject data)
    {
        if (data.FindProperty("animator").objectReferenceValue is Animator assigned) return assigned;
        Animator first = null;
        foreach (Animator candidate in driver.GetComponentsInChildren<Animator>(true))
        {
            if (candidate.runtimeAnimatorController != null) return candidate;
            if (first == null) first = candidate;
        }
        return first;
    }

    private static bool BindWalking(SerializedObject data)
    {
        SerializedProperty bindings = data.FindProperty("stateBindings");
        for (int i = 0; i < bindings.arraySize; i++)
            if (bindings.GetArrayElementAtIndex(i).FindPropertyRelative("behavior").enumValueIndex == WalkingBehavior)
                return false;
        bindings.InsertArrayElementAtIndex(bindings.arraySize);
        SerializedProperty binding = bindings.GetArrayElementAtIndex(bindings.arraySize - 1);
        binding.FindPropertyRelative("behavior").enumValueIndex = WalkingBehavior;
        binding.FindPropertyRelative("stateName").stringValue = WalkingState;
        return true;
    }
}
