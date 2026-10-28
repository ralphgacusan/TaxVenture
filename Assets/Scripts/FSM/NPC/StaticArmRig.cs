using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PURPOSE:
/// Gives an unrigged character mesh that was modelled in a T-pose (Level 1's
/// receptionist and Auditor) natural, lowered arms without new art. At start
/// it measures the model, builds a tiny skeleton (body, and per arm an upper
/// arm and a forearm), skins only the mesh parts that hold the arms (torso,
/// blazer, sleeves) to it, and lowers the arms with a slight elbow bend. The
/// head parts stay ordinary meshes, so NpcAnimationDriver's head and body
/// motion keeps working on top. While its NPC speaks, the forearms make a
/// small, slow gesture; otherwise they only breathe with the body.
///
/// Weights come from each vertex's place along the arm (the span axis of the
/// T-pose), blended at the shoulder and elbow so the sleeve bends smoothly.
/// The original mesh asset is never modified (a skinned copy is made at
/// runtime), and the model's import must allow reading its mesh (Read/Write
/// enabled), which is set for the two models that use this.
///
/// Presentation only: never moves the NPC root, colliders or any state. If
/// the model is not a T-pose (no clear arm span) it does nothing.
/// </summary>
[DisallowMultipleComponent]
public sealed class StaticArmRig : MonoBehaviour
{
    [Tooltip("The character model (the imported FBX instance). Empty: the first child of Visual/Visuals with meshes.")]
    [SerializeField] private Transform model;
    [Tooltip("Arm angle below horizontal (90 = straight down).")]
    [SerializeField, Range(45f, 88f)] private float armDrop = 79f;
    [Tooltip("Forearm bend forward from the upper arm (degrees).")]
    [SerializeField, Range(0f, 80f)] private float elbowBend = 22f;
    [Tooltip("Forearm lift while this NPC speaks (degrees).")]
    [SerializeField, Range(0f, 30f)] private float talkGesture = 10f;
    [Tooltip("Mesh parts never skinned (heads move on their own).")]
    [SerializeField] private string[] keepStatic = { "head", "face", "hair", "eyes", "ears", "mouth", "glasses", "beard", "stache", "tie" };

    private struct Arm
    {
        public Transform upper, lower;
        public Quaternion upperRest, lowerRest;
        public float side;
    }

    private Arm left, right;
    private Transform body;
    private bool built;
    private Vector3 spanAxis, upAxis, forwardAxis;   // model-local
    private NpcAnimationDriver driver;
    private float seed;

    /// <summary>The arms were rigged and lowered.</summary>
    public bool IsRigged => built;

    private void Start()
    {
        driver = GetComponent<NpcAnimationDriver>();
        seed = Random.Range(0f, 10f);
        if (model == null) model = FindModel();
        if (model == null) return;
        built = Build();
        if (!built) Debug.LogWarning($"[StaticArmRig:{name}] The model has no clear T-pose arm span; its arms are left as modelled.", this);
    }

    private Transform FindModel()
    {
        Transform visual = transform.Find("Visual");
        if (visual == null) visual = transform.Find("Visuals");
        if (visual == null) return null;
        foreach (Transform child in visual)
            if (child.gameObject.activeInHierarchy && child.GetComponentInChildren<MeshFilter>() != null)
                return child;
        return null;
    }

    // =========================================================
    // BUILD
    // =========================================================

    private struct Part
    {
        public MeshFilter filter;
        public Vector3[] modelVertices;
    }

    private bool Build()
    {
        var parts = new List<Part>();
        var all = new List<Vector3>(2048);
        foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null || !filter.sharedMesh.isReadable) continue;
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || !renderer.enabled) continue;
            Vector3[] vertices = filter.sharedMesh.vertices;
            var local = new Vector3[vertices.Length];
            Matrix4x4 toModel = model.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int i = 0; i < vertices.Length; i++) local[i] = toModel.MultiplyPoint3x4(vertices[i]);
            all.AddRange(local);
            if (!IsKeptStatic(filter.name)) parts.Add(new Part { filter = filter, modelVertices = local });
        }
        if (all.Count < 50 || parts.Count == 0) return false;

        // Model axes: up from the world, the arm span is the wider horizontal axis.
        upAxis = model.InverseTransformDirection(Vector3.up).normalized;
        Vector3 x = Vector3.ProjectOnPlane(Vector3.right, upAxis).normalized;
        Vector3 z = Vector3.Cross(x, upAxis).normalized;
        Extent(all, x, out float xMin, out float xMax);
        Extent(all, z, out float zMin, out float zMax);
        spanAxis = (xMax - xMin) >= (zMax - zMin) ? x : z;
        // The modelled front (+Z for these imports), across the arm span.
        forwardAxis = spanAxis == x ? z : -x;
        Extent(all, upAxis, out float yMin, out float yMax);
        Extent(all, spanAxis, out float sMin, out float sMax);
        float height = yMax - yMin, centre = (sMin + sMax) * 0.5f, halfSpan = (sMax - sMin) * 0.5f;
        if (height <= 0f || halfSpan < height * 0.3f) return false;

        // Waist half width (below any arm), then the arm's height and thickness.
        float waist = Percentile(all, v => Mathf.Abs(Vector3.Dot(v, spanAxis) - centre), v => Frac(v, yMin, height) > 0.42f && Frac(v, yMin, height) < 0.56f, 0.95f);
        float armHeight = Percentile(all, v => Vector3.Dot(v, upAxis), v => Mathf.Abs(Vector3.Dot(v, spanAxis) - centre) > waist + (halfSpan - waist) * 0.55f, 0.5f);
        float armFrac = (armHeight - yMin) / height;
        if (armFrac < 0.5f || armFrac > 0.92f || halfSpan < waist * 2.2f) return false;
        float chest = Percentile(all, v => Mathf.Abs(Vector3.Dot(v, spanAxis) - centre), v => Frac(v, yMin, height) > armFrac - 0.11f && Frac(v, yMin, height) < armFrac - 0.05f, 0.95f);
        float shoulder = Mathf.Max(waist, chest) * 0.86f;
        float armLength = halfSpan - shoulder;
        float thickness = Percentile(all, v => Mathf.Abs(Vector3.Dot(v, upAxis) - armHeight), v =>
        {
            float s = Mathf.Abs(Vector3.Dot(v, spanAxis) - centre);
            return s > shoulder + armLength * 0.25f && s < shoulder + armLength * 0.75f;
        }, 0.9f);
        thickness = Mathf.Clamp(thickness, height * 0.015f, height * 0.08f);
        float armDepth = Percentile(all, v => Vector3.Dot(v, forwardAxis), v => Mathf.Abs(Vector3.Dot(v, spanAxis) - centre) > shoulder + armLength * 0.3f, 0.5f);

        // Bones under the model (rest = the modelled T-pose).
        body = new GameObject("Arm Rig").transform;
        body.SetParent(model, false);
        Vector3 Joint(float side, float along) =>
            spanAxis * (centre + side * (shoulder + along * armLength)) + upAxis * armHeight + forwardAxis * armDepth;
        left = MakeArm(-1f, Joint(-1f, 0f), Joint(-1f, 0.48f));
        right = MakeArm(1f, Joint(1f, 0f), Joint(1f, 0.48f));

        Transform[] bones = { body, left.upper, left.lower, right.upper, right.lower };
        foreach (Part part in parts)
            Skin(part, bones, centre, shoulder, armLength, armHeight, thickness);

        Pose(0f);
        return true;
    }

    private Arm MakeArm(float side, Vector3 shoulderLocal, Vector3 elbowLocal)
    {
        var arm = new Arm { side = side };
        arm.upper = new GameObject(side < 0f ? "UpperArm.L" : "UpperArm.R").transform;
        arm.upper.SetParent(body, false);
        arm.upper.localPosition = shoulderLocal;
        arm.lower = new GameObject(side < 0f ? "LowerArm.L" : "LowerArm.R").transform;
        arm.lower.SetParent(arm.upper, false);
        arm.lower.localPosition = elbowLocal - shoulderLocal;
        arm.upperRest = arm.upper.localRotation;
        arm.lowerRest = arm.lower.localRotation;
        return arm;
    }

    private void Skin(Part part, Transform[] bones, float centre, float shoulder, float armLength, float armHeight, float thickness)
    {
        Vector3[] v = part.modelVertices;
        var weights = new BoneWeight[v.Length];
        bool anyArm = false;
        for (int i = 0; i < v.Length; i++)
        {
            float lateral = Vector3.Dot(v[i], spanAxis) - centre;
            float along = (Mathf.Abs(lateral) - shoulder) / armLength;
            float vertical = Mathf.Abs(Vector3.Dot(v[i], upAxis) - armHeight) / thickness;
            float arm = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.12f, 0.14f, along)) *
                        (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.6f, 2.6f, vertical)));
            float fore = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.40f, 0.56f, along));
            int upperIndex = lateral < 0f ? 1 : 3;
            weights[i] = Weights(1f - arm, arm * (1f - fore), arm * fore, upperIndex);
            if (arm > 0.001f) anyArm = true;
        }
        if (!anyArm) return;

        Mesh source = part.filter.sharedMesh;
        Mesh skinned = Instantiate(source);
        skinned.name = source.name + " (arm rig)";
        skinned.boneWeights = weights;
        var bindposes = new Matrix4x4[bones.Length];
        for (int b = 0; b < bones.Length; b++)
            bindposes[b] = bones[b].worldToLocalMatrix * part.filter.transform.localToWorldMatrix;
        skinned.bindposes = bindposes;

        MeshRenderer original = part.filter.GetComponent<MeshRenderer>();
        var host = new GameObject(part.filter.name + " (skinned)");
        host.layer = part.filter.gameObject.layer;
        host.transform.SetParent(part.filter.transform, false);
        var renderer = host.AddComponent<SkinnedMeshRenderer>();
        renderer.sharedMesh = skinned;
        renderer.sharedMaterials = original.sharedMaterials;
        renderer.bones = bones;
        renderer.rootBone = body;
        renderer.updateWhenOffscreen = true;
        renderer.shadowCastingMode = original.shadowCastingMode;
        renderer.receiveShadows = original.receiveShadows;
        original.enabled = false;
    }

    /// <summary>
    /// Normalized weights, largest first: a device skinning only one or two
    /// influences (Quality "Skin Weights") keeps the ones that matter.
    /// </summary>
    private static BoneWeight Weights(float body, float upper, float fore, int upperIndex)
    {
        float sum = body + upper + fore;
        if (sum <= 0f) return new BoneWeight { boneIndex0 = 0, weight0 = 1f };
        int[] index = { 0, upperIndex, upperIndex + 1 };
        float[] weight = { body / sum, upper / sum, fore / sum };
        for (int i = 0; i < 2; i++)
            for (int j = i + 1; j < 3; j++)
                if (weight[j] > weight[i])
                {
                    (weight[i], weight[j]) = (weight[j], weight[i]);
                    (index[i], index[j]) = (index[j], index[i]);
                }
        return new BoneWeight
        {
            boneIndex0 = index[0], weight0 = weight[0],
            boneIndex1 = index[1], weight1 = weight[1],
            boneIndex2 = index[2], weight2 = weight[2]
        };
    }

    // =========================================================
    // POSE
    // =========================================================

    private void LateUpdate()
    {
        if (!built) return;
        bool speaking = driver != null && driver.IsSpeaking;
        float t = Time.time + seed;
        float gesture = speaking ? talkGesture * (0.5f + 0.5f * Mathf.Sin(t * 2.1f)) : 0f;
        Pose(gesture + Mathf.Sin(t * 0.9f) * 1.2f);
    }

    /// <summary>Lowers both arms from the modelled T-pose, plus <paramref name="lift"/> degrees of forearm lift.</summary>
    private void Pose(float lift)
    {
        PoseArm(left, lift);
        PoseArm(right, lift * 0.7f);
    }

    private void PoseArm(Arm arm, float lift)
    {
        if (arm.upper == null) return;
        // Model-local: the arm points along side * span; turn it down (toward -up) about the forward axis.
        Vector3 outward = spanAxis * arm.side;
        Vector3 down = (Vector3.Slerp(outward, -upAxis, armDrop / 90f) + forwardAxis * 0.08f).normalized;
        arm.upper.localRotation = Quaternion.FromToRotation(outward, down) * arm.upperRest;
        // The forearm swings forward around the elbow's hinge.
        Vector3 hinge = Vector3.Cross(down, forwardAxis).normalized;
        Quaternion bendLocal = Quaternion.AngleAxis(elbowBend + lift, Quaternion.Inverse(arm.upper.localRotation) * hinge);
        arm.lower.localRotation = bendLocal * arm.lowerRest;
    }

    // =========================================================
    // MEASUREMENT HELPERS
    // =========================================================

    private bool IsKeptStatic(string partName)
    {
        string lower = partName.ToLowerInvariant();
        int dot = lower.IndexOf('.');
        if (dot >= 0) lower = lower.Substring(0, dot);
        foreach (string kept in keepStatic)
            if (lower == kept) return true;
        return false;
    }

    private float Frac(Vector3 v, float yMin, float height) => (Vector3.Dot(v, upAxis) - yMin) / height;

    private static void Extent(List<Vector3> points, Vector3 axis, out float min, out float max)
    {
        min = float.MaxValue;
        max = float.MinValue;
        foreach (Vector3 p in points)
        {
            float d = Vector3.Dot(p, axis);
            if (d < min) min = d;
            if (d > max) max = d;
        }
    }

    private static readonly List<float> Values = new List<float>(1024);

    private static float Percentile(List<Vector3> points, System.Func<Vector3, float> value, System.Func<Vector3, bool> include, float percentile)
    {
        Values.Clear();
        foreach (Vector3 p in points)
            if (include(p)) Values.Add(value(p));
        if (Values.Count == 0) return 0f;
        Values.Sort();
        float result = Values[Mathf.Clamp(Mathf.RoundToInt((Values.Count - 1) * percentile), 0, Values.Count - 1)];
        Values.Clear();
        return result;
    }
}
