# TICKET-06 â€” Dynamic Office Minimap with Live NPC Indicators

Status: In progress ï¿½ Phase 1 of 3
Dependencies: audit and tutorial review  
Implementation order: 6

Current instruction (2026-10-15): explicitly authorized. Implement in three resumable phases with work-log checkpoints. User reference: compact framed in-game map, placed TOP RIGHT for TaxVenture. Preserve existing user scene edits and all Ticket 05 work. The user owns git and Play Mode tests.

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

- [x] Create/configure an orthographic north-up minimap camera and RenderTexture displayed by a non-interactive RawImage.
- [x] Follow player X/Z at fixed height in `LateUpdate`, without parenting or rotating with the player.
- [x] Show a player marker and dynamically registered relevant NPC markers using the map camera/world transform, not manual map coordinates.
- [x] Register/unregister markers through lifecycle or scene discovery without duplicate markers; handle disable/destroy/spawn/despawn.
- [x] Exclude HUD prompts/marker UI from map rendering, disable map-camera shadows/AudioListener, and retain URP camera additional data.
- [x] Integrate at runtime only in Office_Level1.
- [ ] User verification of roof, wall, and UI overlap behavior (Play Mode NOT RUN).

## 5. Expected files/components/scenes

Likely additions: minimap camera/controller and NPC marker scripts under `Assets/Scripts/UI/` or an established navigation folder; a RenderTexture asset only if a serialized asset is necessary; actual office scene components and HUD integration; targeted layer configuration. Inspect before deciding. Avoid a second scene and avoid broad layer reassignment.

## 6. Recommended architecture and Unity best practices

Tutorial concepts adapted: separate orthographic camera; RenderTexture into RawImage; north-up fixed rotation and X/Z follow in `LateUpdate`; player marker; culling-mask exclusions. Keep the camera unparented, create no AudioListener, preserve URP camera data, and derive marker positions from camera viewport projection. Use safe-area anchors and mark the minimap graphic non-raycastable. Do not copy the tutorial's test scene coordinates, layer names, hierarchy, or code.

## 7. Three resumable phases

1. [x] **Foundation:** inspect actual scene/HUD/ceiling geometry, install a runtime orthographic camera + RenderTexture + noninteractive safe-area map. Compile, log exact settings and checkpoint.
2. [x] **Live indicators:** heading-aware player marker and live NPC markers, deduplicated discovery, disable/destroy/spawn cleanup, projection tests. Compile and checkpoint.
3. [x] **Polish and verification:** cohesive game HUD, visibility during other screens, resource/mobile-cost checks, regression suites, independent review, docs and manual checklist. Stop for user Play Mode verification.

At each checkpoint update `handoffs/TICKET-06_WORK_LOG.md` with files, checks, unresolved limitations and the exact next step. If interrupted or usage becomes tight, pause at that checkpoint and resume the next incomplete phase. Do not mark Play Mode acceptance complete from compilation alone.

## 8. Integration requirements

Keep `CameraController` as sole gameplay camera/control owner. Minimap must not accept input or block dialogue/prompts. Map camera should render only suitable world layers; don't hide or change unrelated objects. A scene transition must not leave render textures, cameras, or markers behind.

## 9. Internal verification

- Inspect camera projection, orthographic size, target texture, camera data and culling mask.
- Check marker world-to-viewport mapping with known player/NPC coordinates and bounds.
- Compile and inspect serialized scene references.
- Do not claim visual/gameplay correctness without running the actual office scene.

## 10. Manual testing checklist

- [ ] Load Office_Level1 and check top-right placement at supported aspect ratios/safe areas.
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


## 15. Phase 1 checkpoint — foundation (2026-10-15)

Implemented runtime-only `OfficeMinimap` and `OfficeMinimapHUD` (+ fresh-GUID metadata), plus a camera-specific skip in `InteractionOutlineFeature`. No scene YAML/layer/prefab edits. Bootstrap handles initial Office_Level1 load and later scene loads, deduplicates scene-owned instances, and owns the unparented map camera/texture/HUD lifetime.

Camera: north-up orthographic size14, fixed world Y25 following live player X/Z, near22.2/far26 (cuts at Y2.8 below audited ceiling slabs), world mask137. URP Base camera, no AudioListener, shadows/postprocessing/HDR/MSAA/color/depth copies disabled; 256x256 depth16 RenderTexture. Hidden map disables camera rendering. Existing gameplay camera remains untouched.

HUD: existing HUD_Canvas, owned full-size safe-area wrapper, 248x294 dark navy/gold frame, 232-square viewport, top-right offset(-164,-128) beside document buttons; all Graphics ignore raycasts. Placeholder YOU/PEOPLE legend awaits Phase2 symbols. Resources are destroyed on scene teardown.

Phase1 scratch compile: **0 errors / 25 warnings** (23 baseline and 2 obsolete discovery API warnings; the latter were removed in Phase3). Play Mode **NOT RUN**. Roof slicing, lighting and HUD placement need the user's visual check. Next: Phase2 live player heading and NPC markers.


## 16. Phase 2 checkpoint — live indicators (2026-10-15)

- Added `OfficeMinimapMarkers.cs` and `OfficeMinimapSymbol.cs`, each with fresh-GUID metadata. Player is a cyan triangle with dark edge, oriented by `CameraController.TryGetFirstPersonView`; people are amber diamonds. The footer repeats those shapes. Colors do not imply answer correctness or quest completion.
- Camera `WorldToViewportPoint` converts current transforms to viewport coordinates. Marker positions use the horizontal map plane (Y0); player is centered, +Z is map north/up and +X east/right. Off-map indicators are hidden inside a 9-unit inset, not pinned to the edge.
- Discover active scene-owned `NpcStatusMarker` components once per second, deduplicate by component, and hide/remove disabled/destroyed markers on the next visible map update. New/re-enabled NPCs appear within one second while visible. The map ignores overhead-marker distance/occlusion hiding. Hidden tutorial guide stays absent until `HasAppeared`.
- One map-owned marker collection is disposed with its UI/scene. While the entire map is hidden no marker work occurs; stale entries are removed before it becomes visible again. No NPC/FSM/progression data is modified.

## 17. Phase 3 checkpoint — polish and verification (2026-10-15)

- Dedicated safe-area wrapper; 248x294 navy/gold frame, subtle shadow, 232-square map, OFFICE / N / 1F header. Position (-164,-128) preserves Settings/document controls. Panel scales down when safe dimensions cannot fit it; hides rather than overlapping controls at extremely small sizes.
- All graphics/markers ignore raycasts. No new input handler, camera-control writer, package, static texture asset or scene edit.
- Map and its camera hide during dialogue, workstation/interview/corkboard/profile view, camera transition, timescale pause, game-state pause, Settings and case result popup. Added only read-only `IsVisible` getters in `SettingsUI` and `CaseResultPopupUI`; their behavior is unchanged.
- Camera follows after ordinary player/camera updates (`DefaultExecutionOrder(100)`). RenderTexture recreates after graphics-context loss and releases on destruction. Map camera has no AudioListener. Scene load initialization is scoped/deduplicated; no DontDestroyOnLoad objects.
- Obsolete discovery overloads replaced by installed Unity API; **final compile 0 errors / 23 baseline warnings**, none in the new minimap scripts. Scratch project `Temp/Ticket06/Ticket06.csproj` maps ProjectReferences to Library/ScriptAssemblies DLLs and includes all four new scripts; generated Assembly-CSharp.csproj untouched.
- Required regressions: Controls **5,734 PASS**, Dialogue **796 PASS**, Progression **PASS**. Outline script: known Mobile_Renderer width assertion **FAIL**; all other checks **PASS** in a supplemental in-memory run excluding only that assertion. Test/renderer assets unchanged.
- Analytic source/numeric checks: five map positions (center and four cardinal directions), four heading directions, off-edge exclusion, ceiling/floor clipping bounds, four unique script GUIDs **PASS**. These do not execute Unity rendering/lifecycle.
- Independent source review: **no material findings**. `git diff --check` passes. Office_Level1 SHA256 matches the pre-Ticket06 baseline exactly, preserving user spawn/NPC/other scene changes.
- Play Mode, visual and mobile-device performance: **NOT RUN**. User verification required before complete status.

### User Play Mode checklist

1. Enter Office_Level1: one framed map appears top-right beside document icons. Walk from plaza into rooms: floor/room layout is readable without ceiling slabs, and the map follows you without rotating. Check 16:9 and a tall/wide phone safe area; Settings and document buttons stay usable.
2. Turn in place: cyan triangle rotates correctly; +Z faces N. Nearby people are amber diamonds. Move an NPC transform: its diamond follows. Off-map people disappear rather than piling onto the border.
3. Disable/destroy/re-enable or spawn an NPC carrying NpcStatusMarker: no stale/duplicate dots; new/re-enabled dots appear within one second. The tutorial guide is absent until revealed.
4. Start/end dialogue, enter/exit desk, open/close Settings and case results: map hides and returns. Touch-look and HUD buttons work through/around map graphics.
5. Reload/leave/re-enter scene: exactly one Office Minimap Runtime and Office Minimap Safe Area, no missing-reference errors or leftover camera. Profile GPU/frame time on the target Android device.

If roof cuts, shadows, map darkness or HUD spacing look wrong, report a screenshot and where the player stood; these are the main remaining visual risks. Code-driven setup needs no scene reopen. Stop here for the user's check; do not claim visual acceptance from compilation.


## 18. User layout follow-up — corner row

Supersedes section17's initial beside-column layout. Runtime OfficeMinimapHUD now anchors the map at safe-area top-right(-20,-20). Existing PermanentHUD/Settings container moves 16 units left of the map, top-aligned. Existing IconButtons container moves below the map so its first icon starts 20 units lower and its right edge aligns with the map. Existing child sizes, positions relative to their container, order, graphics, parents and bindings are unchanged. Original container positions restore if the map HUD is destroyed. No scene edits.

Compile0errors/23existing warnings. Controls5734/Dialogue796/Progression PASS; outline check retains known Mobile_Renderer width failure. Play Mode NOT RUN. User: verify the corner layout, Settings click, document click/drag and phone safe-area placement.
