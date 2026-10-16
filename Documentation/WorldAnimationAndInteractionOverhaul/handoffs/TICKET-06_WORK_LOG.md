# Ticket 06 work log

Started 2026-10-15 on explicit user instruction. Reference: compact framed map from attached game screenshot; user requires TOP RIGHT. Only Office_Level1 is in scene scope. User owns git and Play Mode tests. Prefer runtime setup; preserve all Ticket05 changes and current user scene edits.

## Three resumable phases
1. Foundation: audit scene/HUD/occlusion, runtime orthographic camera + RenderTexture + compact safe-area map. Compile and record checkpoint.
2. Live indicators: player heading + NPC world-to-map projection, bounded marker placement and lifecycle. Compile and record checkpoint.
3. Game presentation and verification: cohesive frame/legend/visibility, mobile cost and cleanup, tests/review/docs. Stop for user Play Mode verification.

Each phase records findings, changed files, checks, remaining risks and exact next step. If interrupted, resume the first incomplete phase; do not redo completed phases. No phase is Play Mode verified by the agent.

## Step 1 — Reading and baseline
- Read README, AGENTS, PROGRESS, DECISIONS, KNOWN_ISSUES, Ticket06, PROJECT_AUDIT and IMPLEMENTATION_PLAN.
- Branch feat/immersive-interaction-overhaul. Ticket05 changes remain uncommitted; Office_Level1 has additional user changes. Preserve them. Ticket05 user verification not explicitly supplied, so its status remains pending.
- Usage snapshot: primary 40% consumed, secondary 6% consumed. Proceed with resumable phase checkpoints.
- Using astra-orchestrator skill: bounded Luna read-only audits of geometry/rendering and HUD/player/NPC seams underway.
- Next: integrate audit evidence and choose camera occlusion and HUD design before edits. Play Mode NOT RUN.


## Step 2 — Foundation decisions
- Actual ceiling slabs: Default-layer Cubes at y3.01, thickness .16 (underside2.93). Cannot exclude roof separately by current layer. Use fixed camera world Y25, near22.2 => horizontal cut at y2.8, far26 => y-1. Real floor groups range roughly -.022 to1.005. No geometry/layer changes.
- User scene diff moves Player spawn to (9.72,.6,127.11) in plaza; preserve it. Follow live player X/Z, never copy spawn coordinates.
- Use world mask137 (Default+Interactable+3DInteractable). IgnoreRaycast excludes floating NPC markers; UI layer excluded. Skip the existing interaction outline pass only for this camera.
- HUD_Canvas is overlay,1920x1080,.5 match. Settings100px and right-edge document icons120px already occupy corner. Place roughly248x294 map at safe-area top-right(-164,-128), beside icon column.
- Game look: dark navy frame, warm gold trim, square map, small OFFICE/1F heading, north indicator, player/people legend. No raycastable graphics.
- URP local package API inspected; Unity camera/RenderTexture official documentation checked. Separate Base camera, shadows/postprocessing/depth/color copies off, no AudioListener; 256-square RT. Main camera ownership untouched.
- Existing 11 NpcStatusMarker roots are the correct discovery seam (FSM scan would miss colleague/guide). Guide must be hidden until TutorialNpcController.HasAppeared. NPC map positions must ignore world-marker occlusion suppression.
- Next: foundation worker implements camera/HUD/runtime setup, compiles and records Phase1 checkpoint. Play Mode NOT RUN.


- Scene preservation baseline SHA256 (before Ticket06 code): 8961BCE44BC49370BDA2FE97B52304FA7D28CA88A48C4F324B1A9B6AE1BC096B. User scene diff was 15 insertions / 7 deletions; no Ticket06 scene editing authorized or needed.
- API reference: https://docs.unity3d.com/ja/current/ScriptReference/Camera-targetTexture.html (Unity camera target texture); installed URP UniversalAdditionalCameraData source used for version-exact options.


## Resume / Phase 1 checkpoint (2026-10-15)
- Prior foundation worker hit usage limit before reporting compile; it had saved both classes and metadata. Root inspected files, fixed safe-area ownership request was already saved, recreated missing scratch project and compiled: 0 errors / 25 warnings. NuGet config access required approved escalation.
- Phase1 source complete: OfficeMinimap.cs + .meta, OfficeMinimapHUD.cs + .meta, one-line minimap exclusion in InteractionOutlineFeature. Independent safe-area wrapper is owned/disposed with HUD, not attached to mobile joystick controls. No scene changes; SHA256 still matches baseline.
- User explicitly resumed interrupted work. New usage snapshot has room to continue. Phase2 worker owns live markers and OfficeMinimap integration; root owns HUD refinements and read-only modal visibility getters.
- Phase2 next: camera projection player arrow/NPC diamonds, 1Hz scene-scoped discovery, hidden guide filter, immediate stale-marker cleanup.
- Phase3 ongoing presentation: frame shadow, adaptive map size preserving right document column; read-only SettingsUI/CaseResultPopupUI.IsVisible added (no behavior changes), to suppress map under these panels. Play Mode NOT RUN.


## Verification while Phase2 is in progress
- ControlsRegression5734 PASS; DialogueRegression796 PASS; ProgressionRegression PASS (dotnet run --no-restore).
- VerifyInteractionOutlines.py fails known Mobile_Renderer width assertion. Supplemental in-memory run skipping ONLY that assertion passes all remaining renderer/hint/router/drag checks (source and assets unchanged).
- Phase1 source review: independent wrapper now owned by HUD; no dependency on joystick root. Runtime scene resource ownership and duplicate prevention inspected. Remaining Phase3 review: marker lifecycle/projection + modal suppression once worker finishes.
- Next: finish Phase2 compile/checkpoint, independent focused review, final docs. Play Mode NOT RUN.


## Phase3 review checkpoint
- Independent foundation/HUD review found no concrete material defects in scene-owned camera/RT/HUD disposal, dependency retry, safe-area wrapper, input transparency, outline skip or read-only modal getters.
- Live-marker/modal-integration review remains pending until Phase2 code is ready. Do not treat this as complete acceptance.
- Manual uncertainties remain roof slice/lighting, actual HUD overlap at aspect ratios and device GPU cost. Play Mode NOT RUN.


## Phase2 implementation checkpoint
- Marker worker was interrupted after prolonged unsaved work and reported no files/no blocker. Root took over to protect user usage (4% ->54% before save); bounded implementation is now saved and compiles.
- NEW OfficeMinimapMarkers.cs + .meta: scene-scoped dictionary of NpcStatusMarker anchors, 1Hz discovery, per-visible-frame cleanup of disabled/destroyed targets, guide hidden until HasAppeared. Positions from Camera.WorldToViewportPoint projected onto map plane; off-map markers hidden, no edge objective arrows. Player heading from existing TryGetFirstPersonView; no NPC/gameplay writes.
- NEW OfficeMinimapSymbol.cs + .meta: font-independent cyan triangle / amber diamond with dark edge; same shapes used by HUD legend, raycast-transparent.
- OfficeMinimap integrates owned marker lifetime and cached settings/result/game-state visibility; DefaultExecutionOrder100 follows ordinary player/camera updates. SettingsUI/CaseResultPopupUI read-only getters preserve behavior.
- Scratch compile: 0 errors / 27 warnings; inspecting warnings before final. Scene SHA remains unchanged. Next: independent marker review, cardinal projection checks, final docs/tests checkpoint. Play Mode NOT RUN.


## Phase3 final checkpoint / STOPPED HERE
- All three implementation phases are saved; Ticket06 awaits user verification, not marked complete.
- Final compile0errors /23baseline warnings. The 4 new scripts have no compile warnings; replaced obsolete Unity discovery overloads. Required Controls5734 / Dialogue796 / Progression regression suites PASS. Known outline-width assertion FAIL unchanged; remaining checks PASS in supplemental run.
- Analytic checks PASS: player center + four world cardinal positions (7 world units ->58 map units), four arrow headings, edge inset, roof/floor clip range. Four new script metadata GUIDs unique. Source/numeric checks are NOT Unity execution.
- Independent review completed: no material findings. Root confirmed scene SHA256 unchanged and git diff --check PASS. No commits, staging, reset, stash, push, scene edits, prefab edits or game/progression/transfer changes.
- New files: Assets/Scripts/UI/OfficeMinimap.cs, OfficeMinimapHUD.cs, OfficeMinimapMarkers.cs, OfficeMinimapSymbol.cs, each with .meta. Existing changes for Ticket06 only: InteractionOutlineFeature camera-specific skip; SettingsUI.IsVisible and CaseResultPopupUI.IsVisible read-only getters. Prior Ticket05 work preserved.
- Updated Ticket06 sections15-17 and three-phase checklist, README, AGENTS, PROGRESS, IMPLEMENTATION_PLAN, KNOWN_ISSUES, DECISIONS D012. Scratch build files ignored under Temp/Ticket06.
- Exact next step: USER runs Ticket06 section17 Play Mode checklist (office/plaza roof visibility, top-right safe-area/HUD fit, live heading/NPC markers, spawn/disable/guide filtering, modal visibility/input, scene reload/resource/device performance), reports results/screenshots. Agent waits and addresses defects. No need to reopen scene for these runtime changes.
- Play Mode / visual / device checks NOT RUN. Ticket05 remains awaiting explicit user verification. All required agents finished or explicitly failed/interrupted; foundation prior usage failure and marker stalled-worker fallback are documented above.


## Layout follow-up — user reference
- User requests minimap in top-right corner on its own row, Settings to its left, existing icons below without restyling/resizing/reordering.
- Inspected live HUD and scene: PermanentHUD/Settings and IconButtons are zero-size top-right containers. Settings child100x100; icon column children120x120, authored x10 and first y-20. Preserve child transforms and all bindings. Runtime reposition only; no scene edits.
- Next: position map at safe corner, translate existing containers beside/below map, compile and regressions. Play Mode NOT RUN.


## Layout follow-up — STOPPED HERE
- Changed OfficeMinimapHUD.cs only in runtime code: map(-20,-20), Settings16 units left/top-aligned, existing icon column20 units below/right-aligned. Container translations computed in safe-area space each LateUpdate; icon children, scale, style, ordering, hierarchy and bindings preserved. Original local positions restore on HUD destruction.
- Compile0errors/23baseline warnings; Controls5734 PASS, Dialogue796 PASS, Progression PASS; outline script known Mobile_Renderer width failure unchanged. git diff --check PASS. No new scripts, scene edits or git mutations.
- Ticket06 section18, PROGRESS and DECISIONS D013 updated. Prior layout references are historical and superseded by this user request.
- Exact next step: user checks map corner row, Settings click, icon click/drag, and phone safe-area fit. Play Mode NOT RUN. Stop for user feedback.

