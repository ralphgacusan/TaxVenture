# Handoff: immersive interaction overhaul

## Resume point

**T01 code implemented; awaiting user Unity setup and gameplay acceptance. T02 not started.** Main character remains **Elijah**; no model or NPC animation change is part of T01. User performs Git write actions and manual Unity testing. Do not proceed automatically.

Root: `C:\Learn\Projects\TaxVenture\TaxVenture`; current branch `feat/immersive-interaction-overhaul` (clean at T01 entry); Unity `6000.4.5f1`. Start with AGENTS.md, README.md, PROGRESS.md, CURRENT_STATE.md and the approved ticket.

## What changed

T01 changed VirtualJoystick; movement, FP/TP camera input, CameraController paired-stick lock visibility; legacy Interactor/WorkstationInteractor and three town NPC/pickup input guards. Added JoystickInputMath, MobileInputGuard, MobileControlsSafeArea, Editor/MobileControlsSetup, their .meta files and Tests/ControlsRegression. Documentation/ticket status updated. Full paths and behavior are in CURRENT_STATE's T01 section.

Previous paper-viewer work was committed/pushed by the user before T01. CollectedProfilesWorldPanel remains untouched. Latest user decision replaces LOOK stick with screen swipe over the right 75%, excluding UI. MOVE is 280/112 base/handle and positioned (260,240) from bottom-left. Setup disables legacy LOOK and clears CameraController's reference. The user's saved scene changes were preserved byte-for-byte; rerun the menu, then save with Ctrl+S. No scene YAML/model/package edits were made by this follow-up.

## Most useful findings

- Office_Level1 uses first-person exploration; old third-person README text is not the active setup. ThirdPersonCameraFollow exists but is absent from Level 1 and lacks collision/shared mode ownership.
- Player has a Rigidbody/capsule controller and an empty visible Visual hierarchy (its Capsule is inactive), not an enabled humanoid rig.
- **Elijah assets:** `Assets/3D Models/npc_elijah/{npc_elijah_static.fbx,npc_elijah_Idle.fbx,Elijah_Walking.fbx}` and existing texture. Static/idle Generic versus walking Humanoid imports need validation; explicit walking clip loopTime is 0. Validate rig/clip/shared references in T02 before changing imports. No model configured/imported here.
- Movement retains analog strength and pointer ownership/dead zones/visual easing. Both cameras use one finger starting in the right 75% outside UI. Delta is resolution-normalized; release/disable/focus reset look. MobileLookArea stays unused/inactive; there is no full-screen UI blocker. World input uses native tap release, and pointer NPC/profile paths reject non-tap/cancelled drags. Test rapid out-and-back swipe behavior on device; native tap criteria alone do not track every motion sample.
- HUD Canvas is overlay, scales with 1920x1080 and 0.5 width/height match. Input System 1.19.0/UGUI 2.0.0 installed; no manifest Cinemachine dependency.
- CameraController now handles both joystick visibilities but still assumes FirstPerson for exploration/returns. T02 supplies a third-person compatibility bridge; T03 completes shared safe transitions. Do not accidentally unlock during profile paper return.
- DialogueUI is already shared. It renders immediately, stores one queue/callback, permits replacing a busy conversation, and invokes empty-list completion synchronously. Preserve once-only callbacks and distinguish completion/cancellation/rejection.
- Office IInteractable and outdoor pointer activation differ. Receptionist/client/auditor/colleague and four taxpayer targets/tutorial each have distinct progress effects documented in CURRENT_STATE. Four town taxpayers are not interchangeable with the office client's case interview.
- Existing NPC indicator changes material color by FSM state string. T06 must derive markers from true action availability/completion, including later client content and non-FSM NPCs.
- Notes currently do not lock walking/look; T03 intentionally changes that reading policy without replacing tasks/windows.

## Proposed sequence and coordination

T01 controls -> T02 third-person/collision/Elijah -> T03 interaction modes -> T04 shared conversation lifecycle -> T05 cinematic framing/UI/typewriter -> T06 indicators -> T07 integration -> T08 regression.

T02 is large: separately approved follow/ownership, collision, and Elijah/animation checkpoints are recommended. Read-only asset/content/test preparation can be delegated in parallel; runtime shared scripts and scene/prefab edits need single-writer ownership. Keep T04/T05/T06 meanings as specified by the user.

## Checks and remaining evidence

T01 checks passed: direct runtime C# compile against installed Unity assemblies (Editor/Standalone branches included), separate Editor setup compile, 5,734 production JoystickInputMath assertions and diff checks. Source review found and corrected held-center smoothing drift and release-frame mouse leakage. No Unity menu execution, PlayMode, Android multi-touch, safe-area visual or Undo test was performed.

To rerun pure checks: `dotnet run --project Tests/ControlsRegression/ControlsRegression.csproj`. This does not certify Unity input routing. Temporary direct-compile response files are in ignored Temp/CodexViewerChecks; regenerate from current source/Unity references if needed rather than relying on stale outputs.

Prior paper work had direct compile/numeric checks; user confirmed Phase 3. Phase 4/5 complete PlayMode acceptance is still unrecorded. Existing Editor paper/corkboard checks and PlayerProgressData regression harness are catalogued in CURRENT_STATE; they do not certify new dialogue/camera behavior.

Unresolved: actual Elijah avatar/animation/model metrics/license evidence; collision and framing tuning; device safe areas/multi-touch; historical FSM/startup risks and live duplicate audit caller. Investigate blockers narrowly rather than absorbing the entire old audit.

## Exact next action

**User next:** open Office_Level1 outside Play mode; let Unity compile; run **Tools > TaxVenture > Configure Mobile Controls**; confirm success, save Ctrl+S; follow the T01 manual checklist for move/look/release, multi-touch, UI/world isolation and every interaction lock. Mouse UI testing may need Esc to free the cursor. Report failures with Console text/step/device/aspect. Setup validates refs and reuses LookJoystick; never guess missing legacy Level2 bindings.

After T01 acceptance and explicit approval, T02a follow/ownership estimate **8k-12k tokens**; full T02 **16k-28k** with collision/Elijah checkpoints split. No commit, push, branch switch, merge, deletion or history action on the user's behalf.
