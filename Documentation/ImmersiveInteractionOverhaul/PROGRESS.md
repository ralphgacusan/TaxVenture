# Progress and continuation

Last updated: 2026-10-14. Current work unit: **T01 implemented; awaiting user Unity setup/manual acceptance**. Branch: `feat/immersive-interaction-overhaul`, clean at T01 entry. No Git write actions performed.

## T01 delivered

- Single-pointer, dead-zone/sensitivity-aware movement joystick, immediate logical release and eased thumb feedback.
- User follow-up replaces LOOK stick with right-75%-screen swipe in FP/TP cameras, excluding UI. MOVE grows from 240 to 280 and moves from (180,180) to (260,240). Setup disables the old LOOK object and clears its owner reference.
- World touch actions wait for native tap release; swipes do not activate on touch-down. Desktop mouse interaction remains press-based. User's saved scene edits are preserved; menu rerun/save applies revised settings.
- Fresh UI hit guards and capture/release-frame isolation for world interaction, paired movement/look visibility in existing camera lock paths.
- Safe-area control layout plus **Tools > TaxVenture > Configure Mobile Controls** Editor command; user must run it in Office_Level1 and save the open scene. No scene/prefab/model/material asset edits were made externally.
- New production response regression in `Tests/ControlsRegression`.

T01 checks: runtime C# compiled against installed Unity references with Editor/Standalone defines; Editor setup tool compiled separately; **5,734 production analog-response assertions passed**; source review and diff checks passed. Review findings (center-stick drift and mouse release-frame leakage) were corrected. Pointer routing, menu execution/idempotency/Undo, visual quality, safe areas, full interaction locks and Android multi-touch remain **NOT RUN**, for the user to test.

Actual runtime files: `VirtualJoystick.cs`, `JoystickInputMath.cs`, `MobileInputGuard.cs`, `MobileControlsSafeArea.cs`; `PlayerMovement.cs`, `FirstPersonCameraController.cs`, `ThirdPersonCameraFollow.cs`, `CameraController.cs`, `Interactor.cs`, `WorkstationInteractor.cs`; input guards only in `TaxpayerNpcInteractable.cs`, `TutorialNpcController.cs`, `TaxpayerProfileWorldPickup.cs`. New Editor tool: `Assets/Editor/MobileControlsSetup.cs`. New scripts have .meta files; no packages changed. Existing profile-viewer code, card sizing, quest/dialogue callbacks and model files were preserved.

## Earlier planning session (historical baseline)

- Inspected the repository root/branch/status, live mobile/movement/camera scripts, interaction adapters, NPC/dialogue paths, paper viewer, scene wiring, package/input settings and character import metadata.
- Created this documentation folder, six shared documents and all eight named tickets; no duplicate Unity project.
- Defined existing-system reuse, camera/control ownership, pointer routing, dialogue outcomes, test boundaries, dependency order, agent ownership and stop gates.
- Recorded the user's main-character decision: **Elijah**. Located static/idle/walking FBXs and texture under `Assets/3D Models/npc_elijah/`; documented Generic/Humanoid mismatch and non-looping walking metadata for T02.
- Preserved existing paper-viewer modifications. No gameplay, scene, prefab, model, material, animation controller or package changes are authorized for this first session.

## Ticket status

| Ticket | State | Next condition |
|---|---|---|
| T01 | Code implemented; manual acceptance pending | User runs Unity setup/save and T01 test checklist |
| T02 | Planned | T01 accepted and explicit approval; separately approve follow/collision/Elijah checkpoints |
| T03 | Planned | T02 compatibility foundation accepted |
| T04 | Planned | Shared camera/control lifecycle accepted |
| T05 | Planned | T04 events and Elijah visual/targets ready |
| T06 | Planned | Truthful availability/completion contract from T04 |
| T07 | Planned | T01-T06 integrated |
| T08 | Planned | T07 accepted; run/document final matrix |

## Evidence log

| Check | Result / scope |
|---|---|
| Git branch/status inspection | `bug-fixes...origin/bug-fixes`; two pre-existing modified runtime files listed below |
| Source/package/scene/metadata inspection | Performed; findings in CURRENT_STATE; no claim of live gameplay behavior |
| Documentation file completeness, local links and diff hygiene | PASS: 14 expected Markdown files, eight ticket structures, 13 local links; whitespace cleanup and tracked diff check passed |
| Preservation of existing runtime edits | PASS: SHA-256 hashes of both pre-existing modified runtime files matched planning-entry hashes byte for byte; final Git status adds only Documentation/ |
| C# compilation during this documentation session | NOT RUN: no gameplay code changed |
| Unity EditMode/PlayMode/device regression | NOT RUN: documentation-only session; leave manual/device acceptance for ticket work |
| Earlier paper viewer work | Direct C# compile + geometry/timing calculations passed in prior work; user confirmed Phase 3 working. Phase 4/5 full manual acceptance not recorded |

Existing modified files at original planning entry (subsequently committed by user before T01):
- `Assets/Scripts/Player/CameraController.cs`
- `Assets/Scripts/SideQuest1/CollectedProfilesWorldPanel.cs`

## Known issues and unresolved verification

- Default exploration is first person; third-person script is not wired in Office_Level1. No enabled humanoid player visual was found there.
- Elijah static/idle import as Generic, walking as Humanoid; walking clip has loopTime 0. No rig/loop/controller fix has been attempted.
- T01 addresses pointer ownership/dead zones/analog strength/swipe look, but the revised Editor setup and actual feel still need user acceptance. Level2 has older camera serialization and is not migrated here.
- Shared DialogueUI has no typewriter and can overwrite an active queue/callback. Receptionist empty-content/pre-lock behavior is a lock risk.
- NPC state-color feedback is not the requested overhead available/completed presentation.
- Old audit reports global FSM/startup/retry issues; duplicate audit start calls remain in current source. Runtime impact/reproduction is unverified; these are not blanket scope for unrelated fixes.
- Device performance, asset/license evidence, avatar/clip compatibility, screen safe areas, and camera collision remain unverified.

## Exact next action

**User:** stop Play mode, open Office_Level1, wait for compilation, run **Tools > TaxVenture > Configure Mobile Controls**, press **Ctrl+S**, then follow T01's manual checklist and report results. Agents must wait; do not start T02 automatically. User handles commits/pushes after testing.

After T01 acceptance/explicit approval, next-unit estimate: **T02a follow/ownership 8k-12k tokens**. Full T02 remains **16k-28k**; split follow, collision and Elijah/animation into approved checkpoints. Rig and legacy scene compatibility are the main uncertainties; no reliable account-percentage conversion is available.

Planning validation corrected a missing Verification heading in T08, then reran successfully. No gameplay error was encountered or repaired during this session.
