using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Exercises bones omitted by a sampled/built idle clip, using real Unity transforms.</summary>
public static class ConversationAnimationValidation
{
    [MenuItem("Tools/TaxVenture/Validate Conversation Animation")]
    public static void Validate()
    {
        ElijahDoesNotAccumulate();
        NpcDoesNotAccumulate();
        ImportedElijahDoesNotAccumulate();
        Debug.Log("[ConversationAnimationValidation] PASS: Elijah (partial clip and imported idle) and NPC offsets stay bounded across 600 frames.");
    }

    private static void ElijahDoesNotAccumulate()
    {
        var root = new GameObject("AnimationRegression_Elijah");
        var model = new GameObject("Model");
        var clip = new AnimationClip();
        try
        {
            model.transform.SetParent(root.transform, false);
            Transform spine = Bone("Spine", model.transform);
            Transform head = Bone("Head", spine);
            Quaternion rest = Quaternion.Euler(-87f, 12f, 4f);
            spine.localRotation = rest;
            // The idle intentionally has no spine curve. A stripped/constant
            // curve must never let procedural motion become the next base pose.
            clip.SetCurve("Spine/Head", typeof(Transform), "localRotation.w", AnimationCurve.Constant(0f, 1f, 1f));
            var actor = root.AddComponent<ConversationActorVisual>();
            Set(actor, "modelRoot", model.transform);
            Set(actor, "poseClip", clip);
            Invoke(actor, "CacheMotionBones");
            for (int frame = 0; frame < 600; frame++)
            {
                if (frame % 2 == 0) Invoke(actor, "SamplePose", frame / 60f);
                Set(actor, "smoothedBody", new Vector3(2f, 0f, 0f));
                Invoke(actor, "ApplyConversationMotion");
                Require(Quaternion.Angle(rest, spine.localRotation) < 3f,
                    $"Elijah spine accumulated dialogue rotation at frame {frame}.");
            }
            Invoke(actor, "SamplePose", 0f);
            Require(Quaternion.Angle(rest, spine.localRotation) < 0.01f, "Elijah idle retained a dialogue offset.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(clip);
        }
    }

    private static void NpcDoesNotAccumulate()
    {
        var root = new GameObject("AnimationRegression_Npc");
        var controller = new AnimatorController();
        try
        {
            controller.AddLayer("Base Layer");
            controller.layers[0].stateMachine.AddState("Standing Idle");
            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            Transform spine = Bone("Spine", root.transform);
            Transform head = Bone("Head", spine);
            Quaternion rest = Quaternion.Euler(-87f, 12f, 4f);
            spine.localRotation = rest;
            var driver = root.AddComponent<NpcAnimationDriver>();
            Set(driver, "animator", animator);
            Set(driver, "spineBone", spine);
            Set(driver, "headBone", head);
            Set(driver, "detectMovement", false);
            for (int frame = 0; frame < 600; frame++)
            {
                Invoke(driver, "Update");
                // Simulates a culled or partial pose write: this bone is untouched.
                Set(driver, "smoothedBody", new Vector3(2f, 0f, 0f));
                Invoke(driver, "LateUpdate");
                Require(Quaternion.Angle(rest, spine.localRotation) < 3f,
                    $"NPC spine accumulated dialogue rotation at frame {frame}.");
            }
            Invoke(driver, "OnDisable");
            Require(Quaternion.Angle(rest, spine.localRotation) < 0.01f, "Disabled NPC retained a dialogue offset.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(controller);
        }
    }

    private static void ImportedElijahDoesNotAccumulate()
    {
        const string path = "Assets/3D Models/npc_elijah/npc_elijah_Idle.fbx";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        AnimationClip clip = null;
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is AnimationClip candidate && !candidate.name.StartsWith("__preview__"))
            { clip = candidate; break; }
        Require(prefab != null && clip != null, "Elijah's imported idle model/clip is missing.");
        var root = new GameObject("AnimationRegression_ImportedElijah");
        GameObject model = UnityEngine.Object.Instantiate(prefab, root.transform);
        GameObject reference = UnityEngine.Object.Instantiate(prefab);
        try
        {
            foreach (Animator animator in root.GetComponentsInChildren<Animator>()) animator.enabled = false;
            foreach (Animator animator in reference.GetComponentsInChildren<Animator>()) animator.enabled = false;
            var actor = root.AddComponent<ConversationActorVisual>();
            Set(actor, "modelRoot", model.transform);
            Set(actor, "poseClip", clip);
            Invoke(actor, "CacheMotionBones");
            var bones = (Transform[])typeof(ConversationActorVisual)
                .GetField("motionBones", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(actor);
            Require(bones != null, "Elijah's conversation bones were not found.");
            for (int frame = 0; frame < 600; frame++)
            {
                float time = (frame / 30f) % clip.length;
                clip.SampleAnimation(reference, time);
                Invoke(actor, "SamplePose", time);
                Set(actor, "smoothedBody", new Vector3(2f, 0f, 0f));
                Set(actor, "smoothedHead", Vector3.zero);
                Invoke(actor, "ApplyConversationMotion");
                foreach (Transform bone in bones)
                {
                    if (bone == null) continue;
                    string bonePath = AnimationUtility.CalculateTransformPath(bone, model.transform);
                    Transform expected = reference.transform.Find(bonePath);
                    Require(expected != null && Quaternion.Angle(expected.localRotation, bone.localRotation) < 6f,
                        $"Imported Elijah bone {bone.name} drifted from the idle at frame {frame}.");
                }
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(reference);
        }
    }

    private static Transform Bone(string name, Transform parent)
    {
        var bone = new GameObject(name).transform;
        bone.SetParent(parent, false);
        return bone;
    }

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static void Invoke(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
