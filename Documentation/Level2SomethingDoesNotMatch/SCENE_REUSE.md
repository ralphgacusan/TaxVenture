# Scene creation and efficient reuse

## Decision

Create a **new `Assets/Scenes/Office_Level2.unity`**, initialized once from the current saved `Assets/Scenes/Office_Level1.unity`. Keep Level1 untouched. Reuse its authored environment, office, lighting, colliders, player rig, camera, input and wired physical tools. Then configure this new scene for the Level2 flow and add only the new local content.

This supersedes earlier instructions that assumed an existing Office_Level2 scene must be extended in place. On the latest source inspection, the old scene is `Assets/Scenes/Office_Level2_OLD.unity` and its build entry also targets that old path. Preserve it as a reference; it is not the new scene's base. Do not rename it back, overwrite it, or reuse its GUID for the new scene. Inspect live state again before implementation: a later AI/session may already have created the new destination.

The recommendation is project-specific. Unity supports prefabs, variants and multiple-scene workflows; there is no single universal industry-standard level structure. A controlled scene copy is the lowest-risk way to retain this project's large, connected authored scene today. Shared prefabs are the preferred mechanism for reusable objects and assemblies; a complete environment migration is a separate task if repeated maintenance actually warrants it.

## Why this choice

| Approach | Decision for this work | Practical effect |
|---|---|---|
| Rebuild the environment/office object by object | Reject | Wastes work and risks losing colliders, lighting, scale and interaction references |
| Duplicate Level1 once as a scene asset, specialize gameplay | **Selected** | Preserves authored layout and local references; least new architecture for a complete Level2 |
| Copy scripts, meshes, textures, materials and packages into Level2 folders | Reject | Creates unnecessary asset forks; keep references to originals |
| Extract the entire environment into a new shared prefab now | Defer | Potentially useful later, but converting mixed world/gameplay hierarchies risks references and Level1 regression |
| Load Level1 additively as Level2's environment | Reject for this level | Would also load Level1 controllers/NPC flow and introduces duplicate ownership, lighting and initialization concerns |
| Build a clean environment-only additive scene for every level | Later architectural option | Useful when independent content teams or repeated world changes justify the split; not needed to finish this level |

Copying a scene does not require copying its referenced model, material, texture or animation assets. The scene stores its own hierarchy/serialized overrides and still references those shared assets. It does increase duplicated scene configuration and can drift from Level1 later. It does **not** inherently improve frame rate or runtime memory. Document that tradeoff instead of claiming a free performance optimization.

## AI execution procedure — part of T01

1. **Inspect and record the baseline.** Read this pack, inspect current Level1 hierarchy/startup and project version (observed Unity 6000.4.5f1). Record source scene path, a file hash and relevant source asset GUIDs. Inspect existing destination, old scene, build list and level-select mapping. Do not use Office_Level1_COPY or Office_Level2_OLD as the baseline: they are not the current finished Level1.
2. **Respect the open editor and unsaved work.** Do not launch a second Unity instance, force a scene switch, overwrite an open dirty scene, or save the user's unsaved changes implicitly. A disk copy contains the saved source only. If the source has unsaved changes and a connected editor can report them, stop only the conflicting creation operation and report the precise issue; continue independent content/code work. Do not claim disk contents include unobserved editor changes.
3. **Copy once to the exact destination.** Prefer Unity's existing editor asset pipeline (`AssetDatabase.CopyAsset` or an available equivalent asset-creation tool), checking the result and destination. It must produce a distinct scene asset/GUID while retaining references to shared assets. Do not copy Level1's `.meta` verbatim. If native asset APIs are unavailable, a source-only fallback may copy the complete saved `.unity` file unchanged and create a unique valid `.meta`; report Unity import as pending. Never reconstruct the multi-megabyte YAML hierarchy by hand or regex-rewrite file IDs. This authorized initial copy is the only whole-scene copy step.
4. **Do not overwrite an existing destination.** If Office_Level2 already contains work, inspect it and continue locally. Re-running the setup must update only known owned additions/bindings and must never recopy Level1 over Level2. Write a setup-version marker under the owned Level2 root so a future AI can distinguish initialization from incremental work. Never auto-delete an existing scene, even if it looks incomplete.
5. **Preserve the borrowed core.** Keep environment hierarchy names, positions, scales, door and room bindings, player/camera/input, EventSystem, AudioManager, desk/folder/book/stamps, storage/hand-in behavior and already functioning presentation. Inspect duplication risks: scene objects plus runtime bootstraps must still yield one owner each. Keep source prefab links intact; do not unpack all instances or apply Level2 overrides back to shared prefabs.
6. **Replace level behavior through configuration.** Point case/level loading to Level_02 and Rosa. Disable copied Level1-only profile/corkboard startup, Marie encounter/case selection, office gate, tutorial content and emergency-departure policy in Level2. Do not blanket-remove every component mentioning Level1; some shared utilities have historical names. Remove/disable components only after classifying their role and dependencies. Guide remains observation-only. Explicitly opt Level2 into current HUD/map/fade/finale systems that otherwise bootstrap only in Level1.
7. **Add one owned Level2 root.** Add `Level2Gameplay` for local phase orchestration, cabinet retrieval, market encounter, toolkit, references and waypoints. Preserve existing office object paths needed by guidance. Place the selected Rosa prefab and selected Building_4 store per WORLD_AND_FEEDBACK. Reuse a suitable existing store if already present at the intended location; do not create overlapping buildings or move shared source instances. Add the records desk and new physical tools, not a second copy of the whole plaza/office.
8. **Update loading/build integration deliberately.** Register the new scene exactly once and route career/menu Level2 selection to it. Inspect name-versus-index loading before changing build order; preserve menu/cutscene mappings. Keep Office_Level2_OLD on disk, normally excluded from player build if unreferenced. Do not silently ship two competing Level2 entry scenes. Its old build GUID must not be attached to the new scene. This is configuration work owned by the AI, not a user scene-wiring task.
9. **Verify and record.** Compare source Level1 hash before/after; it must be unchanged by scene creation. Check distinct GUIDs, valid asset references, inherited core binding inventory, one Camera/input/EventSystem/manager owner, expected Level2 capability/configuration and no automatic Level1 quest start. Run internal checks and explicitly list Unity import/visual checks not performed. Record exact newly created scene, setup tool, setup version, disabled inherited behaviors, added assets and build/menu changes in PROGRESS.

The source-copy stage must not silently enable Level1 behavior in the new playable entry. T01 acceptance is reached after the Level2 start configuration is wired, not simply when the copied file exists.

## Reuse over Levels 3–5

Continue sharing source scripts and existing prefab assets; level data and local overrides carry differences. New cohesive Level2 assemblies, such as the market toolkit, can be ordinary reusable prefabs when references stay inside the assembly. Use a prefab variant only when there is an actual base prefab and useful override relationship; scenes themselves are not prefab variants.

Do not turn the entire copied Level1 scene into a prefab, add inheritance systems for scenes, or introduce Addressables/additive loading just for this plan. Keep an inventory of deliberate layout differences and source snapshot date. A later Level1 bug fix in scene-authored configuration does not automatically reach Level2: evaluate and port the specific fix. Shared script/prefab changes may reach both, so run applicable regression checks.

If repeated office/environment maintenance becomes costly, propose a separate environment-only shared prefab or additive-scene migration with explicit reference, lighting, navigation and Level1 regression acceptance. Do not perform that migration as hidden work in a Level2 ticket.

## Setup tooling and AI handoff

If a setup/builder script is needed, keep it small, narrowly scoped and idempotent, with a dry-run inventory and explicit source/destination. No new framework/package. It must not require Play Mode, shell-launch another editor, or overwrite dirty/open scenes. Prefer an available connected editor for execution; source-only script creation is not proof that a scene has been imported or wired in Unity. Report actual execution versus pending import honestly. The user tests the completed result, not assembles it. All git commands remain user-owned.

## Acceptance additions

- New Office_Level2 scene exists with unique `.meta` GUID, based on current saved Office_Level1; Level1 and _OLD untouched.
- Existing Level2 work is protected on rerun; one owned setup root, no repeated world duplication.
- Referenced meshes/materials/textures/clips remain shared; no duplicated asset tree.
- Office layout, tools, routes, lighting/input bindings preserved, with Level2-specific changes documented.
- New scene/menu/build mapping is coherent; no stale Level2 selection loading _OLD.
- Imported scene/lighting/reference validity and runtime feel are checked by the user; internal source checks reported separately.

## Official references

These document Unity mechanisms, not a mandated industry architecture. Verify exact API availability against the installed editor before execution.

- [AssetDatabase.CopyAsset](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetDatabase.CopyAsset.html): native asset-copy operation.
- [Prefab variants](https://docs.unity3d.com/6000.0/Documentation/Manual/PrefabVariants.html): inherited configurations for prefab assets.
- [Multiple scenes](https://docs.unity3d.com/6000.0/Documentation/Manual/MultiSceneEditing.html): supported alternative for scene separation, deferred here.
