using UnityEngine;

/// <summary>
/// PURPOSE:
/// Milestone T03: which existing assets make Elijah's outdoor exploration
/// body (Resources/Player/ExplorationBody.asset). References only; the
/// sources and their imports (shared with the cutscenes) are not changed.
/// - model: a derived smoothed Humanoid Elijah, preserving its own avatar.
/// - idle: a Humanoid standing idle already shared by NPCs (NpcHumanoid.controller).
/// - walk: Elijah's own walking clip (imported non-looping; the body loops it by distance).
/// - sprint/jump: authored in-place Humanoid clips, including landing recovery.
/// - conversationModel: a matching smoothed copy of the original generic idle model.
/// </summary>
[CreateAssetMenu(menuName = "TaxVenture/Exploration Body Assets")]
public sealed class ExplorationBodyAssets : ScriptableObject
{
    [Tooltip("Tentative T22 pilot opt-in (Level 1 only). Leave disabled for first-person exploration without jumping. Does not control smoothing or sprint.")]
    public bool mixedCameraEnabled = false;

    public GameObject model;
    public AnimationClip idle;
    public AnimationClip walk;
    public AnimationClip sprint;
    public AnimationClip jump;
    public GameObject conversationModel;
}
