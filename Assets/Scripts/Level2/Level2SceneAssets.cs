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

    public static Level2SceneAssets Load() => Resources.Load<Level2SceneAssets>(ResourcePath);
}
