# Handoff: immersive interaction overhaul

## Resume point

**Documentation complete; gameplay implementation not started. Wait for approval for T01.** User's latest clarification selects **Elijah** as the main character. Do not replace him with an alternative model. Do not implement all tickets in one session.

Root: `C:\Learn\Projects\TaxVenture\TaxVenture`; planning branch `bug-fixes`; Unity `6000.4.5f1`. Start with AGENTS.md, README.md, PROGRESS.md, CURRENT_STATE.md and the approved ticket.

## What changed

Only this documentation tree was created: README, AGENTS, CURRENT_STATE, IMPLEMENTATION_PLAN, PROGRESS, HANDOFF, and tickets T01-T08. No runtime/scene/prefab/package changes were made for the overhaul.

Two runtime files were already modified on entry: `Assets/Scripts/Player/CameraController.cs` and `Assets/Scripts/SideQuest1/CollectedProfilesWorldPanel.cs`. They contain recent profile viewer open/read/drag/layer/return work and its movement/camera lock. Preserve them; neither staging nor reverting them is authorized.

## Most useful findings

- Office_Level1 uses first-person exploration; old third-person README text is not the active setup. ThirdPersonCameraFollow exists but is absent from Level 1 and lacks collision/shared mode ownership.
- Player has a Rigidbody/capsule controller and an empty visible Visual hierarchy (its Capsule is inactive), not an enabled humanoid rig.
- **Elijah assets:** `Assets/3D Models/npc_elijah/{npc_elijah_static.fbx,npc_elijah_Idle.fbx,Elijah_Walking.fbx}` and existing texture. Static/idle Generic versus walking Humanoid imports need validation; explicit walking clip loopTime is 0. Validate rig/clip/shared references in T02 before changing imports. No model configured/imported here.
- Movement joystick exists; MobileLookArea is inactive. PlayerMovement normalizes away analog strength. Mixed legacy Input/new Input System requires pointer ownership and no duplicate look handlers.
- HUD Canvas is overlay, scales with 1920x1080 and 0.5 width/height match. Input System 1.19.0/UGUI 2.0.0 installed; no manifest Cinemachine dependency.
- CameraController owns current modes/locks but assumes FirstPerson for normal exploration and several returns, including paper viewer. T02 supplies a minimal exploration compatibility bridge; T03 completes shared safe transitions.
- DialogueUI is already shared. It renders immediately, stores one queue/callback, permits replacing a busy conversation, and invokes empty-list completion synchronously. Preserve once-only callbacks and distinguish completion/cancellation/rejection.
- Office IInteractable and outdoor pointer activation differ. Receptionist/client/auditor/colleague and four taxpayer targets/tutorial each have distinct progress effects documented in CURRENT_STATE. Four town taxpayers are not interchangeable with the office client's case interview.
- Existing NPC indicator changes material color by FSM state string. T06 must derive markers from true action availability/completion, including later client content and non-FSM NPCs.
- Notes currently do not lock walking/look; T03 intentionally changes that reading policy without replacing tasks/windows.

## Proposed sequence and coordination

T01 controls -> T02 third-person/collision/Elijah -> T03 interaction modes -> T04 shared conversation lifecycle -> T05 cinematic framing/UI/typewriter -> T06 indicators -> T07 integration -> T08 regression.

T02 is large: separately approved follow/ownership, collision, and Elijah/animation checkpoints are recommended. Read-only asset/content/test preparation can be delegated in parallel; runtime shared scripts and scene/prefab edits need single-writer ownership. Keep T04/T05/T06 meanings as specified by the user.

## Checks and remaining evidence

Planning inspected source, metadata, settings and serialized scene wiring. Final checks passed: 14 Markdown files, eight ticket structures, 13 local links, whitespace/diff checks, and unchanged SHA-256 hashes for both protected runtime files. Final Git status adds only Documentation/ beyond the initial runtime edits. No Unity gameplay/Android test or new C# compilation is claimed for the documentation-only session.

Prior paper work had direct compile/numeric checks; user confirmed Phase 3. Phase 4/5 complete PlayMode acceptance is still unrecorded. Existing Editor paper/corkboard checks and PlayerProgressData regression harness are catalogued in CURRENT_STATE; they do not certify new dialogue/camera behavior.

Unresolved: actual Elijah avatar/animation/model metrics/license evidence; collision and framing tuning; device safe areas/multi-touch; historical FSM/startup risks and live duplicate audit caller. Investigate blockers narrowly rather than absorbing the entire old audit.

## Exact next action

Wait for **approval to implement T01**. Then inspect the current diff and `VirtualJoystick`, `MobileLookArea`, movement/look/interactor scripts and actual MobileControls bindings. Implement the approved T01 scope only, preserve existing paper work, run focused internal checks, give manual Unity steps, update ticket/progress/handoff, estimate T02 and stop.

Next T01 estimate: **8k-14k tokens**; split pointer/analog behavior from visuals/look integration if budget requires it. No commit, push, branch switch, merge, deletion or history action without explicit authorization.
