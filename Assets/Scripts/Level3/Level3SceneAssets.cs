using UnityEngine;

/// <summary>
/// PURPOSE:
/// Supplied art Level 3 builds at runtime (no scene edits), loaded from
/// Resources/Levels/Level_03/scene_assets.asset (the Level2SceneAssets
/// pattern). Only references: the Synty source prefabs are never modified.
///
/// - calculatorPrefab: Synty SM_Prop_Calculator_01, the body of the desk
///   calculator (T03). Level3CalculatorModel adds the readable keys, display,
///   printer roll and tape around a scaled instance.
/// - beaPrefab: Synty SM_Chr_Developer_Female_01, the model Level 1 already
///   uses for Bea (T04), animated by npcController (the shared NpcHumanoid
///   controller, the one Level 2's town uses).
/// - worktablePrefab: Palmov Island's park "wooden table" (T04), Bea's plaza
///   worktable, fitted to working height at runtime.
/// </summary>
[CreateAssetMenu(fileName = "scene_assets", menuName = "TaxVenture/Level 3 Scene Assets")]
public sealed class Level3SceneAssets : ScriptableObject
{
    public const string ResourcePath = "Levels/Level_03/scene_assets";

    public GameObject calculatorPrefab;
    public GameObject beaPrefab;
    public RuntimeAnimatorController npcController;
    public GameObject worktablePrefab;
    /// <summary>Level 1's plaza guard (SM_Chr_Security_Male_01) and Carlo (SM_Chr_Developer_Male_02), same models, no copies.</summary>
    public GameObject guardPrefab;
    public GameObject ambientPrefab;

    public static Level3SceneAssets Load() => Resources.Load<Level3SceneAssets>(ResourcePath);
}
