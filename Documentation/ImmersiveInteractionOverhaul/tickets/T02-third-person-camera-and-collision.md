# T02 — Third-person camera, collision and Elijah locomotion

Status: **PLANNED — awaiting approval**
Dependencies: T01 input contract. Next: T03. Estimate: **16k-28k tokens**; use separately approved checkpoints below.

## Objective and inspected entry points

Default exploration follows a visible Elijah player naturally, with safe indoor/outdoor camera collision and basic idle/walk animation.

Primary files: `Assets/Scripts/Player/ThirdPersonCameraFollow.cs`, `PlayerMovement.cs`, `FirstPersonCameraController.cs`, `CameraController.cs`, `FirstPersonHands.cs`; `Assets/Scenes/Office_Level1.unity`. Check `Office_Level2.unity` legacy camera settings before shared script changes. Cinemachine is not in the manifest and is not wired in Level 1: extend existing suitable code unless evidence justifies otherwise.

**Selected main character: Elijah, as explicitly directed by the user.** Assets:
- `Assets/3D Models/npc_elijah/npc_elijah_static.fbx`
- `Assets/3D Models/npc_elijah/npc_elijah_Idle.fbx`
- `Assets/3D Models/npc_elijah/Elijah_Walking.fbx`
- `Assets/3D Models/npc_elijah/npc_elijah_static.fbm/elijah_texture.png`

Static/idle currently import as Generic; walking imports as Humanoid with a non-looping explicit clip named walking_idle. Determine actual animation/skeleton contents in Unity. Do not select the previously inventoried Synty candidate instead.

## Approved-unit proposal

1. **T02a — Ownership and follow:** make third person the default, use T01's right-side screen-swipe look (user rejected LOOK stick), configure orbit distance/height/pitch/damping. Establish the minimum CameraController compatibility bridge so existing interactions and profile APIs can enter/exit safely and return to exploration. No simultaneous camera writers.
2. **T02b — Collision:** camera-volume sweep/overlap clearance, wall/ceiling/corner/furniture handling, smooth outward recovery and stable narrow-space behavior. Configure masks, radius, skin, distance limits; exclude player/triggers/papers/markers.
3. **T02c — Elijah model and animation:** validate rig/avatar and clip imports, shared NPC references, texture/materials, model scale, ground alignment, collider fit, triangle/bone/material cost, licensing evidence. Build a player-specific visual/prefab/controller, reuse Elijah idle/walk where valid, configure loop/root-motion policies and speed-based blending. Do not modify NPC animation behavior.

Stop after each approved checkpoint if splitting; never mark the whole ticket complete until all criteria pass. User character choice is settled; technical suitability is still to be verified.

## Acceptance criteria

- [ ] Exploration uses a stable third-person camera with useful player/environment framing indoors and outdoors.
- [ ] Configurable orbit/look, pitch bounds and damping feel responsive and movement direction remains predictable.
- [ ] Camera stays outside walls/ceilings/furniture, handles initial overlap/narrow corners, and returns smoothly after obstruction clears.
- [ ] Smoothing does not carry the camera through geometry or produce repeated zoom jitter.
- [ ] Elijah has a validated compatible rig/avatar, visible textured mesh and functional idle/walk transitions.
- [ ] Animation follows actual movement speed; locked movement yields idle/no locomotion; no root-motion double movement, foot sliding or competing facing rotations.
- [ ] Run/strafe/turn clips are included only if existing assets and movement design support them; no invented required animation set.
- [ ] First-person views hide/reposition Elijah where needed; valid shoulder shots can retain appropriate model parts.
- [ ] Existing desk, dialogue, corkboard and full profile open/drag/return still function and restore exploration.
- [ ] No new package or external asset is imported without a documented reason; any blocked Elijah repair/substitution is reported before replacement.

## Verification and handoff

Compile and inspect scene/Animator/avatar wiring. Use meaningful camera-volume/state tests; manually navigate offices, doorways, corners, ceilings, furniture and outdoor paths at different frame rates/look angles. Test idle -> walk -> stop/lock and camera/body overlap. Record measured triangles/materials/device cost and unresolved license evidence instead of guessed values.

No NPC animation, unrelated model swaps or customizer. Record exact clip/avatar/controller paths and camera settings in CURRENT_STATE/HANDOFF; preserve serialized APIs and recent paper changes. Stop for approval.

