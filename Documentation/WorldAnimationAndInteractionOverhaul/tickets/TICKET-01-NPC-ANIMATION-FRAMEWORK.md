# TICKET-01 — Reusable NPC Animation Framework

Status: Complete (2026-10-15) - user confirmed in Play Mode; walking deferred as a known issue  
Dependencies: audit  
Implementation order: 1

Started on explicit user instruction (2026-10-15). See `handoffs/TICKET-01_CODE_COMPLETE.md`.

## 1. Objective and expected outcome

Provide one reusable, Inspector-configurable animation driver that can be used by distinct NPCs. It must fail safely when an Animator or configured state is unavailable and must not own dialogue or case rules. The first integrated profiles should use only assets confirmed compatible with actual Office_Level1 NPC rigs.

## 2. Existing systems and files to inspect

- `Assets/Scripts/FSM/NPC/NpcStateMachine.cs` and `Assets/Scripts/FSM/NPC/States/`
- `Assets/Scripts/Interactables/ClientInteractable.cs`, `ColleagueInteractable.cs`, `AuditorInteractable.cs`, and `ReceptionistInteractable.cs`
- Actual NPC GameObjects and Animator components in `Assets/Scenes/Office_Level1.unity`
- `Assets/3D Models/npc_jhun_static/`, `npc_cora_static/`, `npc_tutorial_static/`, `npc_receptionist_static/`, and `npc_elijah/`
- Existing Animator Controllers and FBX rig import settings; do not infer compatibility from filenames.

## 3. Dependencies and prerequisites

- Audit complete.
- Confirm scene NPC Animator references, avatar type, controller, and usable clips.
- Preserve existing dirty hunks in NPC FSM/interaction scripts and scene files.
- Ticket 02 depends on the state API established here.

## 4. Exact implementation scope

- [x] Add a small driver/profile using existing Animator state/parameter APIs; cache hashes/lookups and guard missing references.
- [x] Support an idle/default state and optional per-NPC configured states for movement, talking, listening, thinking, gesture, pointing, and reactions, only where actual Animator states exist.
- [x] Use smooth cross-fades and keep gameplay rules outside the driver.
- [x] Add an honest low-cost fallback when no animation state exists; do not present root movement or a repeated idle as a custom gesture.
- [x] Wire FSM state changes to a driver on existing NPC FSM objects; scene integration is runtime-added so legacy scene instances receive the safe adapter without YAML changes.

## 5. Expected files/components/scenes

Potential additions: `Assets/Scripts/FSM/NPC/NpcAnimationDriver.cs` and, only if useful, a compact profile asset/type under the existing Scripts/Data or ScriptableObjects convention. Likely integrations are `NpcStateMachine.cs`, NPC scene objects in `Office_Level1.unity`, and relevant Animator Controller assets. Verify actual paths and GUIDs before editing. No controller or prefab name is assumed to be assigned until inspected.

## 6. Recommended architecture and Unity practices

Keep one driver per animated NPC. Cache the `Animator` and parameter/state identifiers. Listen to NPC state changes instead of polling unrelated game systems. Keep per-character state names or supported actions in serialized configuration; unavailable states should be ignored with at most a useful one-time warning. Do not add Animator layers or a ScriptableObject if a serialized state map on the component is sufficient.

## 7. Phases and completion criteria

1. [x] Verify rig/controller compatibility for each target NPC and record supported states. (Section 15.)
2. [x] Implement the smallest safe driver/profile and compile. (Scratch-csproj compile: 0 errors.)
3. [x] Configure actual NPCs in the scene/prefab and inspect serialized references. (11 serialized drivers; Animator auto-discovered from the FBX prefab instance under `Visual`. No scene edit was needed.)
4. [x] Verify the available idle/state transitions in Play Mode; mark unsupported motion as incomplete. (User confirmed idle and conversation facing; walking deferred.)

## 8. Integration requirements

Keep the current NPC state machine and interaction interfaces stable. An NPC without an Animator must continue to interact normally. Scene/prefab references must survive reload. Do not modify shared imported rig scale or replace player colliders.

## 9. Internal verification

- Compile in the already-running Unity Editor if available; inspect fresh Console/Editor.log evidence.
- Inspect each modified NPC's Animator, avatar, controller, and serialized driver values.
- Run existing relevant checks only if this ticket changes behavior covered by them.
- Do not claim Play Mode behavior without actually running the scene.

## 10. Manual testing checklist

- [ ] Load Office_Level1 and confirm each configured NPC renders without Animator warnings/errors.
- [ ] Trigger idle and supported state changes; check smooth transition and no root drift.
- [ ] Disable/remove a test NPC Animator and confirm dialogue/interactions still work.
- [ ] Verify only Office_Level1; other scenes are outside scope.

## 11. Acceptance criteria

- [x] Different NPCs can use different Inspector profiles without duplicated behavior scripts. (Marie uses the Cora idle controller, Jhun/Zorbix the Jhun idle controller, static NPCs the head fallback, all through one driver.)
- [x] Existing interactions and FSM transitions still function. (Presentation-only listener; dialogue/controls/progression regressions pass; Play Mode check pending.)
- [x] Missing Animator/state references do not break the scene. (Driver no-ops when Animator/controller/state is unavailable.)
- [x] Every claimed animation is backed by a compatible assigned clip/state and verification. (Only "Standing Idle" on 3 Humanoid NPCs is claimed; visual check pending.)

## 12. Risks and rollback

Wrong avatar/clip mapping can deform a character; root-motion defaults may move or jitter scene NPCs. Preserve controller assets and keep changes isolated. Rollback by removing the new driver component and its narrow integration, without reverting other scene changes.

## 13. Continuation instructions

Code is complete; only the user's Play Mode check remains. Earlier audit wording that the office scenes have "no NPC Animator components" was wrong: NPC_Marie, NPC_Jhun and NPC_Zorbix use Humanoid FBX prefab instances whose Animator and idle controller live in the instance overrides (section 15). If the manual check passes, mark this ticket Complete and start Ticket 02. Adding talk/listen/gesture clips later only needs states in the NPC's controller plus matching `stateBindings` entries.

## 14. Out of scope

New animation packs, custom motion capture, facial/lip-sync authoring, NPC locomotion redesign, dialogue content changes, and game progression changes.

## 15. Verified NPC animation support (Office_Level1)

| NPC | Visual model (rig) | Controller | Supported states |
|---|---|---|---|
| NPC_Marie | npc_cora_static@Standing Idle (Humanoid) | npc_cora_static@Standing Idle | Standing Idle (loops, root motion off) |
| NPC_Jhun, NPC_Zorbix | npc_jhun_static@Standing Idle (Humanoid) | npc_jhun_static@Standing Idle | Standing Idle (loops, root motion off) |
| Auditor, Client, Client1, Client2, Colleague, NPC_Milo, NPC_TutorialGuide, Receptionist | Static/Generic meshes, no controller | none | Static head fallback only (subtle sway; talk nod and listen tilt during their own dialogue) |

Talking, listening, thinking, gestures, pointing, reactions and walking have no compatible clips; requests fall back to idle. The Generic `*_walking.fbx` clips cannot drive the Humanoid models and are not assigned. The inactive Elijah models under `Env` are not NPCs.

## 16. Follow-up: more visible idle and pacing (user request, 2026-10-15)

- Static NPCs: the driver now also breathes and sways the `Visual`/`Visuals` model container (never the collider root), glances around while idle (about 14 degrees), and plays a step bob/waddle while moving. Head nod defaults to 3.5 degrees.
- New `Assets/Scripts/FSM/NPC/NpcIdlePacing.cs`: walks back and forth (3 m, raycast-clamped) only in Idle/Completed with no dialogue open; stops and faces the player within 3 m; stays put when the model cannot show walking (`NpcAnimationDriver.SupportsWalking`).
- New `Assets/Editor/NpcAnimationSetup.cs` (Tools > TaxVenture > Configure NPC Animation): imports the unused `npc_jhun_walking.fbx`/`npc_cora_walking.fbx` as Humanoid looping in-place clips, adds a `Walking` state to the Jhun/Cora idle controllers, binds Walking on matching drivers, raises scene head motion to 3.5, and adds pacing to NPC_Jhun, NPC_Zorbix, NPC_Marie and NPC_Milo.
- Verification: runtime and editor scratch builds 0 errors; dialogue/controls/progression regressions pass. Humanoid avatar validity of the walking FBXs is checked by the tool at run time (not verifiable outside Unity). No Play Mode test.

## 17. Play-test fixes (user feedback, 2026-10-15)

Reported: NPCs kept turning to follow the player, some did not face the player when clicked, every walker froze when any NPC was clicked, and walking slid.
- Pacing no longer turns toward a nearby player; it just stops when the NPC becomes Waiting.
- Only the clicked NPC's own dialogue stops it, and that NPC turns its body to face the player (walkers only; indoor NPCs keep their fixed viewpoints).
- Walk speed now comes from the playing Walking clip (`AnimatedWalkSpeed`, scaled by avatar size); the driver enters Walking on request (`SetLocomotion`), movement eases in, the NPC moves only forward and turns in place for large angles at 120 deg/s.
- Static meshes no longer pace (`allowStaticModels` off; NPC_Milo removed from the tool). Builds 0 errors; regressions pass; no Play Mode test.

## 18. Idle-only scope and improved idle (user decision, 2026-10-15)

User decision: everyone idles in place for now; no walking.
- `NpcIdlePacing.enableWalking` defaults to false, so pacing NPCs no longer walk. The component still turns the clicked NPC to face the player.
- Idle choreography in `NpcAnimationDriver`, shared by all NPCs: continuous breathing, sway and head drift, plus a random idle action every 2.5-6 s (look around, weight shift, look down, look up/stretch, glance at a player within 9 m). Each NPC gets random amplitude (0.85-1.2) and tempo (0.85-1.15) so neighbours never move in sync. Tunable through `idleActionStrength` and `idleActionInterval`.
- Static NPCs: applied to the `Visual` container (body) and head parts.
- Humanoid NPCs (Marie, Jhun, Zorbix): the Standing Idle clip keeps playing with a random start time and speed (0.9-1.1); head/neck/spine/chest offsets are layered on top in LateUpdate (`animateRigBones`). Animator culling is set to Always Animate so offsets cannot accumulate offscreen.
- During the NPC's own dialogue the actions stop; the head faces the player, nods while talking and tilts while listening.
- Verification: runtime/editor builds 0 errors; regressions pass; no Play Mode test.

**Known problem - walking (deferred):** walking looked wrong in play-testing (sliding feet, odd turning). Unresolved: the in-place Humanoid walk clip speed vs movement speed, turning while idle, and static meshes having no legs. The code (`NpcIdlePacing` walking path, the Walking states added by the setup tool, `SetLocomotion`/`AnimatedWalkSpeed`) is kept but disabled. Fix and verify before setting `enableWalking` on any NPC.

## 19. Fix: NPC turned its back in conversation (2026-10-15)

Cause: the Plaza model instances sit rotated about 141 degrees inside their NPC roots (Milo about 163), so aiming the root's +Z at the player turned the model's back to the player. `NpcAnimationDriver.FacingOffset` now stores the model root's yaw relative to the NPC root at Awake. Facing the player, head yaw/glances, body lean and bone offsets all use the model's facing; `NpcIdlePacing` turns and moves by it too. Builds 0 errors; regressions pass; no Play Mode test.
