using UnityEngine;

/// <summary>
/// PURPOSE:
/// The original assets Level 2 builds its market scene from, referenced (not
/// copied) so they load at runtime and ship in builds:
/// - Rosa: Assets/3D Models/npc_cora_static/npc_cora_static@Standing Idle.fbx (Marie's former
///   model; swapped 2026-10-20, Level 1's Marie now wears SM_Chr_Business_Female_03).
///   Level2RosaStall fits it to a person's height.
/// - Her store: Assets/Flat_Style_Vehicles/Prefabs/Buildings/Building_4.prefab
///   (used only when the scene has no RosaStallAnchor; with the anchor, no shop is built)
/// - Rosa's idle: the project's humanoid "Standing Idle" controller
///   (npc_cora_static), because the Synty prefab's own controller asset is
///   not in the project (that is why she stood in a T-pose).
/// - T32: the shared NPC controller and the Level 2 plaza cast's models.
/// Lives at Resources/Levels/Level_02/scene_assets.asset. The source scenes
/// and prefabs are never modified.
/// </summary>
[CreateAssetMenu(fileName = "scene_assets", menuName = "TaxVenture/Level 2 Scene Assets")]
public sealed class Level2SceneAssets : ScriptableObject
{
    public const string ResourcePath = "Levels/Level_02/scene_assets";

    public GameObject rosaPrefab;
    public GameObject storePrefab;
    public RuntimeAnimatorController rosaIdleController;
    /// <summary>
    /// T32: Level 1's shared Humanoid controller (Assets/AssetIntegration/NpcHumanoid.controller: Standing Idle,
    /// Idle Relaxed, Briefcase Idle, Walking, Strolling). Rosa walks with it; the townspeople use it too.
    /// </summary>
    public RuntimeAnimatorController npcController;
    /// <summary>
    /// Level 2's small plaza cast: existing townspeople, then reused Arthur (the same rigged
    /// FBX as Level 1), guard, Marie and customer models, matched by order to town_life.json.
    /// </summary>
    public GameObject[] residentModels = new GameObject[0];

    public static Level2SceneAssets Load() => Resources.Load<Level2SceneAssets>(ResourcePath);
}
