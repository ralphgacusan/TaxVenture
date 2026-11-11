using UnityEngine;

/// <summary>
/// PURPOSE:
/// A little of Level 1's plaza life in Level 3: Mang Dante on his Level 1 patrol in front of the office
/// and Carlo on his Level 1 bench, with their Level 1 models, the shared NPC controller and the same
/// NpcLife / AmbientNpcInteractable components Level2Town builds at runtime. Both stay far from Bea's table,
/// the supply counter and the doors. Their small talk is optional and only talk (Levels/Level_01/plaza_life):
/// no quest, reward, objective or save data.
/// </summary>
public sealed class Level3PlazaLife : MonoBehaviour
{
    public const string ChildName = "PlazaLife";
    public const string GuardId = "dante";
    public const string BenchId = "carlo";
    private const string WalkState = "Walking", StrollState = "Strolling", IdleState = "Standing Idle";

    /// <summary>Level 1's "Dante Patrol A/B" and "Carlo Bench Seat" scene markers.</summary>
    public static readonly Vector3 PatrolA = new Vector3(7f, -0.4f, 108.4f);
    public static readonly Vector3 PatrolB = new Vector3(10.9f, -0.4f, 108.9f);
    public static readonly float PatrolAYaw = -20f, PatrolBYaw = 25f;
    public static readonly Vector3 BenchSeat = new Vector3(8.87f, 0.056f, 125.95f);
    public static readonly float BenchYaw = 180f;

    private bool built;

    private void Start() => Build();

    private void Build()
    {
        if (built) return;
        built = true;
        Level3SceneAssets assets = Level3SceneAssets.Load();
        if (assets == null || assets.npcController == null) return;
        Transform receptionist = PlayerPlacement.Find(gameObject.scene, "Environment/OfficeBuilding/Rooms/Reception/Receptionist");
        int layer = receptionist != null ? receptionist.gameObject.layer : gameObject.layer;

        Vector3 standing = BenchSeat + Quaternion.Euler(0f, BenchYaw, 0f) * Vector3.forward * 0.42f;
        NpcLife guard = Resident(GuardId, "Mang Dante", assets.guardPrefab, assets.npcController, layer, PatrolA, PatrolAYaw, 1.7f);
        if (guard != null)
        {
            Transform a = Marker("Dante Patrol A", PatrolA, PatrolAYaw);
            Transform b = Marker("Dante Patrol B", PatrolB, PatrolBYaw);
            guard.ConfigureRuntime(NpcLife.Activity.Patrol, a, null, new[] { a, b }, new Vector2(3.5f, 6.5f), false, true);
            guard.gameObject.SetActive(true);
        }
        NpcLife bench = Resident(BenchId, "Carlo", assets.ambientPrefab, assets.npcController, layer, standing, BenchYaw, 1.68f);
        if (bench != null)
        {
            bench.ConfigureRuntime(NpcLife.Activity.Stand, null, Marker("Carlo Bench Seat", BenchSeat, BenchYaw), null, new Vector2(3f, 6f), false, true);
            bench.gameObject.SetActive(true);
        }
    }

    /// <summary>One person per id, built inactive so every component is configured before its first frame.</summary>
    private NpcLife Resident(string id, string fallbackName, GameObject prefab, RuntimeAnimatorController controller, int layer, Vector3 feet, float yaw, float height)
    {
        if (prefab == null || transform.Find("Ambient " + id) != null) return null;
        var holder = new GameObject("Ambient " + id);
        holder.layer = layer;
        holder.SetActive(false);
        holder.transform.SetParent(transform, false);
        holder.transform.SetPositionAndRotation(NpcLife.Ground(feet), Quaternion.Euler(0f, yaw, 0f));

        var visual = new GameObject("Visual").transform;
        visual.SetParent(holder.transform, false);
        GameObject model = Instantiate(prefab, visual);
        model.name = prefab.name;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        Fit(model, holder.transform, height);
        foreach (Animator animator in model.GetComponentsInChildren<Animator>(true))
        {
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        var capsule = holder.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, height * 0.5f, 0f);
        capsule.height = height;
        capsule.radius = 0.32f;

        AmbientNpcInteractable talk = holder.AddComponent<AmbientNpcInteractable>();
        AmbientNpcDefinition words = PlazaLifeContent.Find(id);
        talk.Configure(id, words != null && !string.IsNullOrWhiteSpace(words.name) ? words.name : fallbackName);
        NpcAnimationDriver driver = holder.GetComponent<NpcAnimationDriver>();
        if (driver == null) driver = holder.AddComponent<NpcAnimationDriver>();
        driver.ConfigureStates(IdleState, WalkState, StrollState);
        return holder.AddComponent<NpcLife>();
    }

    private Transform Marker(string name, Vector3 position, float yaw)
    {
        var marker = new GameObject(name).transform;
        marker.SetParent(transform, false);
        marker.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        return marker;
    }

    /// <summary>Scales the model to height metres and stands its soles on the holder's origin (Level2Town's fit).</summary>
    private static void Fit(GameObject model, Transform holder, float height)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        if (b.size.y < 0.01f) return;
        model.transform.localScale *= height / b.size.y;
        b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        model.transform.position += Vector3.up * (holder.position.y - b.min.y);
    }
}
