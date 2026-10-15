# TICKET-06 — Dynamic Office Minimap with Live NPC Indicators

Status: Planned  
Dependencies: audit and tutorial review  
Implementation order: 6

Current instruction (2026-10-15): setup only. Do not begin or resume this ticket without a new user instruction. Existing partial implementation is historical, unverified work.

## 1. Objective and expected outcome

Add a compact top-right minimap to the actual office gameplay scene, with a north-up player-following map and player/NPC markers derived from live world transforms. It must use the existing URP/UI setup and avoid interfering with gameplay input.

## 2. Existing systems and files to inspect

- `Assets/Scenes/Office_Level1.unity`: player start, playable floor, roof/walls, HUD canvas/safe area, relevant NPCs
- `CameraController.cs`, `FirstPersonCameraController.cs`, player movement and `MobileInputGuard.cs`
- NPC interaction/FSM components used to identify relevant scene NPCs
- Current URP renderer/camera data and layers
- Supplied tutorial: https://youtu.be/YNbuhDiaXg4

## 3. Dependencies and prerequisites

- Audit complete and tutorial transcript inspected.
- Additive camera must not become another writer of the player camera.
- Identify roof/occlusion and screen safe-area constraints in each supported office scene.

## 4. Exact implementation scope

- [ ] Create/configure an orthographic north-up minimap camera and RenderTexture displayed by a non-interactive RawImage.
- [ ] Follow player X/Z at fixed height in `LateUpdate`, without parenting or rotating with the player.
- [ ] Show a player marker and dynamically registered relevant NPC markers using the map camera/world transform, not manual map coordinates.
- [ ] Register/unregister markers through lifecycle or scene discovery without duplicate markers; handle disable/destroy/spawn/despawn.
- [ ] Exclude HUD prompts/marker UI from map rendering, disable map-camera shadows/AudioListener, and retain URP camera additional data.
- [ ] Integrate into the actual Office_Level1 scene; verify roof, wall, and UI overlap behavior.

## 5. Expected files/components/scenes

Likely additions: minimap camera/controller and NPC marker scripts under `Assets/Scripts/UI/` or an established navigation folder; a RenderTexture asset only if a serialized asset is necessary; actual office scene components and HUD integration; targeted layer configuration. Inspect before deciding. Avoid a second scene and avoid broad layer reassignment.

## 6. Recommended architecture and Unity best practices

Tutorial concepts adapted: separate orthographic camera; RenderTexture into RawImage; north-up fixed rotation and X/Z follow in `LateUpdate`; player marker; culling-mask exclusions. Keep the camera unparented, create no AudioListener, preserve URP camera data, and derive marker positions from camera viewport projection. Use safe-area anchors and mark the minimap graphic non-raycastable. Do not copy the tutorial's test scene coordinates, layer names, hierarchy, or code.

## 7. Phases and completion criteria

1. [ ] Inspect actual office map bounds, roof/wall occlusion, HUD safe-area scaling, player and NPC transforms.
2. [ ] Implement camera, RT/RawImage presentation, and north-up follow behavior.
3. [ ] Add lifecycle-safe player/NPC indicators and wire real scene entities.
4. [ ] Check coordinate alignment, culling, references, and compile; record Editor/Play Mode limits.

## 8. Integration requirements

Keep `CameraController` as sole gameplay camera/control owner. Minimap must not accept input or block dialogue/prompts. Map camera should render only suitable world layers; don't hide or change unrelated objects. A scene transition must not leave render textures, cameras, or markers behind.

## 9. Internal verification

- Inspect camera projection, orthographic size, target texture, camera data and culling mask.
- Check marker world-to-viewport mapping with known player/NPC coordinates and bounds.
- Compile and inspect serialized scene references.
- Do not claim visual/gameplay correctness without running the actual office scene.

## 10. Manual testing checklist

- [ ] Load each office scene and check top-right placement at supported aspect ratios/safe areas.
- [ ] Walk/turn: player marker remains centered/readable and north-up map orientation stays fixed.
- [ ] Move an NPC in scene/Play Mode: its dot tracks the transform without coordinate edits.
- [ ] Disable, destroy, spawn, and despawn an NPC; confirm markers disappear/appear once.
- [ ] Verify roof/wall visibility, no HUD prompts in the map, and minimap does not steal UI/world input.

## 11. Acceptance criteria

- [ ] Minimap appears in the actual office scene at the top-right and remains compact.
- [ ] Player and relevant NPC markers align with actual world positions.
- [ ] Marker lifecycle and scene transitions do not create duplicates or stale references.
- [ ] Camera/RenderTexture and HUD work with the project URP/UI configuration.
- [ ] No gameplay input or dialogue behavior is disrupted.

## 12. Risks and rollback

Roofs/walls can occlude the map; map layers can accidentally suppress real objects; secondary URP cameras can cost mobile frame time. Keep the map camera/layer changes narrowly scoped and disable/remove the minimap component to roll back without altering gameplay camera control.

## 13. Continuation instructions

Read the short adaptation note in `PROJECT_AUDIT.md`; the full transcript is available from the user-provided reference, and the architecture was inspected. Inspect current scene diffs and live geometry. Update this ticket, progress, and handoff with exact culling/occlusion verification; don't copy transcript wording into repository docs.

## 14. Out of scope

Route planning, objective arrows, user-controlled zoom/pan, a full second gameplay scene, a third-party minimap package, and map-based NPC interaction.
