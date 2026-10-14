# Implementation plan

Status: proposed implementation, awaiting user approval. No proposed new API, state, component, or configuration below exists merely because it is named here. See CURRENT_STATE.md for implemented facts.

## Player stories

1. As a mobile player, I want a responsive movement joystick and right-side screen-swipe look so I can explore while looking around without accidental taps (user revision after initial T01).
2. As a player, I want a visible, style-matched character with idle/walk transitions so movement feels grounded and readable.
3. As a player, I want a third-person camera that handles confined rooms and outdoor space so walls and furniture do not obscure navigation.
4. As a player, I want close viewpoints for conversations, desk work, board tasks, notes and profiles so interactions feel intentional and readable.
5. As a player, I want safe cancel/close and interruption recovery so I return to exploration with control and the same progress.
6. As a player, I want a consistent conversation flow for all relevant NPCs so skip, advance, end and conditional content behave predictably.
7. As a player, I want readable dialogue and speaker framing so I can follow who is talking without camera or UI obstruction.
8. As a player, I want distinct available/completed NPC markers so I can identify useful conversations without losing later interactions.
9. As a player, I want profile collection, paper viewing/dragging, matching, interviews and objectives preserved so presentation changes do not change quest rules.
10. As a player, I want UI and input to recover after pauses, multi-touch, rapid taps, scene changes and unavailable interactions so no action leaks or gets stuck.

## Architecture decisions

### A01: Evolve the existing camera/control owner

Use `CameraController` as the shared interaction entry/exit owner; do not introduce a parallel manager that independently enables controls or writes the same camera pose. Evolve `ThirdPersonCameraFollow` for exploration and retain existing first-person/anchored views as appropriate. Define explicit exploration, entering, active interaction and returning behavior; these can be a small extension of the existing controller rather than a general framework.

Preserve existing public call sites and serialized UnityEvents through adapters until callers migrate. Append new serialized enum values or migrate deliberately; do not silently renumber them. Audit every `FirstPerson` guard, reset, unlock and startup assumption, especially `TryBeginProfileViewer/EndProfileViewer`, dialogue and board entry/exit.

The active camera has exactly one transform writer at any time. A successful interaction owns its lock until its return transition finishes; rejection never acquires a lock or fires completion. Snapshot or explicitly manage movement/look/interactors, touch IDs, joystick visibility/input, Rigidbody motion/constraints, cursor, player visual/hands, camera pose/FOV and any raycast ownership changed by that interaction. Restore only what this owner changed; closing one overlay must not unlock another active interaction.

T02 establishes the minimum exploration compatibility bridge: default third-person owner, legacy interaction entry still accepted, return routed back to exploration, existing paper-viewer API still functional. T03 completes shared close-viewpoint blending and interruption handling for every designated interaction. T02 is not accepted if basic existing interactions break while awaiting T03.

### A02: Reuse the present camera before adding packages

No Cinemachine setup is wired in the inspected Level 1 scene and it is not a manifest dependency. Prefer extending the current follow script with a configurable orbit pivot, pitch/distance bounds, damping and collision resolution. A new package requires an evidence-based reason and an explicit plan change, not automatic installation.

Separate preferred orbit distance from obstruction-limited distance. Query a camera volume from a safe player/shoulder pivot toward the desired pose; use appropriate radius/skin/near-plane clearance and layer/trigger filtering. Check initial overlap and ceiling/corner cases, not just a center ray. Retract promptly to avoid clipping; restore outward smoothly with stable damping/hysteresis. Prevent the smoothing path from interpolating through a wall. Exclude player/self, irrelevant triggers, markers and held/viewer papers. Reuse this collision safety for interaction blends and dialogue shots; use fallback close framing where a desired shoulder position is obstructed.

### A03: One owner per pointer and input context

Reuse VirtualJoystick for movement and the existing single-finger camera swipe path. Latest user instruction removes the visible LOOK stick; right 75% is the configurable origin zone, with UI touches excluded. Disable old LOOK UI and clear references through the setup menu; leave unused MobileLookArea inactive rather than adding a transparent raycast surface over NPCs. Reuse the installed Input System native tap qualification for touch-world actions on release, so starting a swipe cannot immediately activate a focused object. Keep desktop input working.

Capture pointer identity on press and release only that pointer; reset on cancel, focus loss, disable and mode changes. UI-started touches remain UI-owned even when dragged away; joystick touches cannot interact with NPCs/world objects. Move/look can use two separate fingers; a third HUD tap cannot steal either. Normalize only above unit magnitude and remap an adjustable dead zone while retaining analog movement strength. Input should reset immediately on release while the handle visual may ease home; visual smoothing must not produce residual movement.

### A04: Reuse DialogueUI and its data; normalize lifecycle

The project already shares a renderer/queue. Improve its session arbitration, one-action-per-gesture routing, cancellation and outcomes rather than inventing a second dialogue controller. Office IInteractable and town pointer handlers become small adapters to the same lifecycle. Preserve current JSON/builder content and conditional outcome branches; verify legacy question UI before modifying it. Add branch/choice support only where an actual current content path requires it, not a new graph editor.

Distinguish completed, cancelled, rejected and invalid sessions. Emit completion side effects once only after successful conclusion. An empty list must follow a documented, safe outcome without leaving pre-acquired locks or silently duplicating a reward/drop. A busy start is rejected or explicitly queued according to the final contract; never overwrite an active queue/callback. Presentation completion is not the same as interview completion or an exhausted NPC.

### A05: Dialogue presentation consumes session events

Expose current line/speaker and lifecycle signals for camera, UI and indicators without letting them mutate quest state. Build bottom safe-area dialogue UI using existing Canvas/TMP/portraits; Continue at bottom-right and tapping the dialogue box share one advance path. Typewriter uses visible-character count/rich-text-aware character handling, configurable pacing and punctuation pauses. First tap reveals the line; only a subsequent gesture advances. Cancel/line replacement clears stale routines, and closing/paused UI cannot leak the same touch into gameplay.

Use supplied shoulder/reverse-shot references to guide composition, not to copy assets. Frame the NPC when NPC speaks and player when player speaks, using validated player model/head targets. Provide obstruction-safe alternatives when no safe shoulder shot fits; support different heights and tight office spaces. Hide/reposition the player mesh only where first-person clipping requires it; preserve shoulders for dialogue when feasible. No new NPC animations.

### A06: Indicators read real availability and completion

Evolve NpcVisualIndicator and NPC adapters. Use availability/exhaustion/interview/profile/quest data as the authority, with initial state synchronization and change notifications. Do not infer completion merely from closing DialogueUI or from a color/string alone. For the client, completed interview can coexist with a later available presentation conversation. Ordinary colleague/tutorial NPCs need adapters because they lack the same FSM.

Use distinct shape plus color for available/completed; hide or change markers for active conversation/unavailable/out-of-range NPCs. Anchor above character bounds or assigned head point, face the camera, use restrained unscaled/appropriate-time bob/pulse, and configurable distance/occlusion rules. Avoid adding colliders that steal clicks or per-NPC material clones every frame.

## Dependencies and scope boundaries

Default serial order is T01 -> T02 -> T03 -> T04 -> T05 -> T06 -> T07 -> T08. T01 defines pointer/input contracts needed by T02. T02's minimal compatibility changes are supporting scope, not full T03 delivery. T03 defines common ownership for T04/T05. T06 uses T04's actual conversation status; coordinate marker suppression with T05. T07 integrates all; T08 records final evidence.

Safe parallel preparation: model/clip inventory, NPC/content mapping, UI safe-area mock layout, and test case preparation can be read-only and independent. Do not concurrently edit CameraController, DialogueUI, NPC completion callbacks, or Office_Level1. Sequential approval still applies even when read-only work is delegated.

Out of scope: unrelated case grading, tax calculations, level unlocking/rewards, save format redesign, repaired level routing, new NPC animation, imported environment art, new quests, and a full narrative authoring package. A discovered issue that blocks an assigned ticket must be documented with a minimal supporting fix proposal; do not absorb the whole historical audit.

## Risks and decisions still needing evidence

| Risk | Mitigation / decision checkpoint |
|---|---|
| No visible humanoid player in Level 1 | User selected Elijah. T02 validates `Assets/3D Models/npc_elijah/` and integrates a player-specific visual/prefab/controller using the existing model/texture. No substitute model is authorized. |
| Elijah static/idle Generic imports versus Humanoid walking import | Verify skeleton/avatar compatibility and non-looping walking clip configuration in Unity. Inspect shared references before importer changes; avoid altering NPCs while configuring the player. |
| Existing rig/controller/license uncertainty | Inspect Animator references in Unity, humanoid avatar validity, root motion, triangles/skinning/materials, license entitlement and clip permissions. Record actual findings, not guessed metrics. External candidate only if needed, with exact source URL before import. |
| Root player rotation competes with camera/model | Keep movement facing, orbit yaw, and visual animation separate; camera should not force a second player rotation. Retain Rigidbody/capsule behavior. |
| Mixed input, default pointer-over checks, inactive look area | T01 owns movement pointer routing/dead zones and single-finger screen swipe; verify movement + look + HUD on actual touch input. Asset presence is not working wiring. |
| Old FirstPerson guards and multiple camera scripts | T02 audits startup/exit compatibility; T03 completes ownership migration and guarantees one writer. Preserve serialized enum/API callers. |
| Notes currently permit walking/look | T03 explicitly migrates notes into close reading mode while preserving task refresh and swipe close. |
| UI overlay/GraphicRaycaster and raw paper hit tests | Preserve icon priority, camera pixelRect projection, viewer layering and disabled world raycasts. Lock through both paper close and camera return. |
| Multiple dialogue start paths/empty receptionist content | T04 adds arbitration and once-only outcome handling; test busy/empty/cancelled flows and caller locks. |
| Historical duplicate FSM/startup and live duplicate audit calls | Record baseline; reproduce separately. Do not label new behavior broken solely from old audit text. Minimal blocking fixes require explicit scope/evidence. |
| Inconsistent completion semantics and later client content | Record interview/profile/exhaustion separately; T06 markers must use current available actions. |
| Device/collision tuning cannot be proven by compilation | Require indoor/outdoor and Android/manual evidence at ticket acceptance; static inspection and numeric checks stay separately labeled. |

## Validation and release gate

For every implemented ticket: C# compile and diff checks; focused state/input/data checks; relevant Unity EditMode/PlayMode checks where runnable; user manual/device checklist with actual results. Do not repeatedly run unrelated checks or count old Library reports as fresh passes. Existing PlayerProgressData tests do not validate conversations.

T08 must cover indoor/outdoor movement, analog strength and movement-plus-screen-swipe controls, character idle/walk/lock, wall/ceiling/corner/furniture collision and obstruction recovery, every interaction entry/exit/cancel, indoor/outdoor/four target conversations, typewriter/skip/advance, repeated/busy/empty conversations, correct progress/indicators, corkboard/tray/pinning, complete profile open/drag/close, objective notes/desk, pause/focus loss, scene transitions, and supported resolutions/safe areas. Record real device and performance measurements; do not invent an FPS/triangle budget without a target-device baseline.

Manual acceptance includes readable framing, unobstructed faces/papers, stable movement/camera, no stuck locks, no lost/duplicated callbacks or changed quest outcomes, and no new Console errors. Initial documentation delivery requires complete files/links/ticket dependencies and unchanged gameplay files; it does not require a gameplay build.
