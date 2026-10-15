# Handoff

## Latest completed unit — T03 (all four checkpoints), 2026-10-15

Updated 2026-10-15. Root: `C:\Learn\Projects\TaxVenture\TaxVenture`; branch `feat/immersive-interaction-overhaul` at `74d53caf`; Unity 6000.4.5f1. Current work: **T03 implemented; internal checks passed; user scene setup and Unity acceptance pending.** T02 (both phases) is also code-complete with manual acceptance pending. Keep T01 and the original first-person exploration. No Git, scene, prefab, importer or data edits were made by the agent.

The user authorized all of T03 at once and asked for internal tests only. Full detail, file list, evidence, manual matrix and risks are in `tickets/T03-dialogue-camera-and-ui.md`.

## Latest unit — T06 regression evidence, 2026-10-15

All automated checks pass:

- compile: 0 errors
- dialogue: 796 assertions
- controls: 5,734 assertions
- progression
- profile cards
- the T03/T04 setup saved in `Office_Level1`
- a clean diff scope

The four Editor validations and every manual or device row are NOT RUN. The matrix and known issues are in `tickets/T06-regression-testing-and-handoff.md`. The overhaul is **not** declared complete until the user's manual results arrive.

Agent: **STOP** for final user review. Any failure becomes a separately estimated fix.

## Previous unit — T05 integration and safe input returns, 2026-10-15

Implemented; see the "Delivery" section of `tickets/T05-integration-and-game-feel.md`.

- **Changed:** `Assets/Scripts/Player/CameraController.cs` and `Assets/Scripts/Mobile/MobileInputGuard.cs` only. No scene, prefab or API changes.
- **Fixes:**
  - Popup lock/unlock requests are deferred while a conversation holds the controls.
  - The profile viewer refuses to open during a conversation or under a popup lock.
  - One-frame world-tap suppression whenever controls are restored (closing tap fall-through on touch).
- **Evidence:** `Assembly-CSharp` compiles with 0 errors and the 2 baseline warnings. NOT RUN in Unity.

User next: run the five T05 manual checks in the ticket (plus the outstanding T02/T03/T04 matrices), then commit with git.

Agent: **STOP.** Next is **T06 regression evidence and handoff**, on explicit approval only. Estimate **8k–14k tokens**.

## Previous unit — T04 NPC markers, 2026-10-15

Implemented. See `tickets/T04-npc-interaction-indicators.md`.

- **New:** `NpcAvailability.cs` (`INpcAvailabilitySource` and the pure `NpcAvailabilityRules`) and `NpcStatusMarker.cs`, both in `Assets/Scripts/FSM/NPC`, plus `Assets/Editor/NpcMarkerSetup.cs`.
- **Changed:** additive `Availability` adapters on the six NPC interactables.
- **Evidence:** Unity compile 0 errors; 796 assertions pass; files hash-verified.

User next:

1. Run **Tools > TaxVenture > Configure NPC Markers** in `Office_Level1` and save the scene.
2. Run the T04 matrix, plus the outstanding T02 and T03 matrices.
3. Commit with git; the new `.meta` files and `Assets/Materials/NpcStatusMarker.mat` must be included.

T05 has since been implemented (see above).

## Revision 1 (user feedback)

The changes are instant cuts (blends commented out in `DialogueCameraDirector.ShowLine`/`BeginReturn`), a wider side angle (migrated via `settingsVersion`), a runtime dialogue backdrop in `DialogueUI`, and solo close-ups driven by line content, a 15% chance on borderline lines, or `DialogueBuilder.CloseUp()`. No setup re-run is needed. Unity compile 0 errors; 757 assertions pass. See the T03 ticket.

## T03 delivery in one screen

- **Conversation-only Elijah** (`ConversationActorVisual`):
  - Built at runtime from `npc_elijah_Idle.fbx`, with no colliders, posed by sampling the idle clip.
  - Fitted to a measured posed height and a world-unit `standingHeight` (proposed as the median NPC height), with its feet on the floor.
  - Hidden outside conversations and while the camera is near his head.
  - The player root, collider, NPC transforms and importers are untouched.
- **Shots** (`DialogueCameraDirector`, on the CameraController object):
  - NPC lines frame over Elijah's right shoulder; player lines over the NPC's left shoulder. Both stay on the same side of the axis.
  - Obstruction tests drive a shortened pull-back, then a two-shot, then a first-person fallback.
  - NPC framing comes from an optional `DialogueSpeakerAnchor`, otherwise renderer bounds, otherwise colliders.
- **Return and terminal order**:
  - The logical first-person pose is captured from `FirstPersonCameraController.TryGetViewPose`.
  - On end or cancel: hide the panel → blend back → restore hands and document → hide Elijah → release controls → completion or cancellation callback, via `DialogueTerminalHandoff`. This replaces T02's release-before-callback order.
  - The conversation counts as active until release.
  - The conversation also freezes the player Rigidbody (constraints and velocity only).
- **UI**:
  - TMP typewriter over visible characters, with pauses at real sentence and clause breaks.
  - Box taps, Continue, submit and `AdvanceDialogue()` go through one `DialogueAdvanceGate`: a gesture reveals the line while typing, a distinct later gesture advances or closes, and opening, duplicate and held input is rejected.
  - The setup tool restyles the existing panel into a compact bottom safe-area box (`SafeAreaBottomPanel`) and hides the oversized portrait object, keeping it and its reference.
- **Integration**: every office and town caller already goes through `DialogueUI`, so all routes share the presentation without per-NPC code.
- **Editor**:
  - **Tools > TaxVenture > Configure Dialogue Presentation** (Undo) measures, creates, wires, restyles and reports.
  - **Toggle Conversation Visual Preview** stands an unsaved Elijah by the player for grounding checks.

Without the setup tool, behaviour is T02 plus the typewriter.

## Evidence and limits

- PASS: full-project compilation (runtime and Editor) against the user's installed Unity 6000.4.5f1 `Managed` and `NetStandard` assemblies, the project's generated `.csproj` lists and defines, and the `Library/ScriptAssemblies` packages. Baseline 0 errors; with T03, 0 errors. The Editor build used .NET Standard facades instead of the .NET Framework profile.
- PASS: 610 production contract assertions (311 T02 + 299 T03).
- PASS: hash match between on-disk files and the tested copies; diffs scoped.
- NOT RUN: the setup menu, preview, Play mode, device or any visual or input behaviour.

Risks to watch:

- Elijah's facing sign: fix with `forwardYawOffset` ± 180.
- Sampling a Generic clip without an avatar: fall back to `npc_elijah_static` or untick `animateIdle`.
- NPC bounds that include props: add a `DialogueSpeakerAnchor`.
- The intended 0.45 s wait before outcomes.
- `Office_Level2` is not configured.

Leftovers: two compile-check archives in the git-ignored `Library` folder (about 22 MB, deletable), and two pre-existing inactive Elijah instances from the abandoned third-person setup, left untouched.

Line endings: HEAD stores LF, but working-tree endings vary per file (for example `DialogueUI.cs` and `FirstPersonHands.cs` are CRLF on disk, most others LF); the user's Windows git normalizes them. When checking diffs from a non-Windows shell, use `git diff --ignore-cr-at-eol --numstat`, and preserve each file's existing endings when writing.

## Exact next action

User:

1. Let Unity compile.
2. In `Office_Level1` (Edit mode) run **Configure Dialogue Presentation** and read the Console report.
3. Run **Toggle Conversation Visual Preview** to check feet on the floor, NPC-like height and facing the NPC, then run it again to remove the preview.
4. Save the scene.
5. Run the T03 manual matrix (shots per NPC route, tight spaces, typewriter and taps, cancel and return, safe areas) and the T02 matrix.
6. Report Console errors, the NPC or route, and a screenshot of any bad shot.

Do not save transient Play-mode inspection changes.

Agent: **STOP.** On explicit approval, start **T04 NPC indicators**: overhead available/completed markers driven by real state (NPC FSMs plus the T02 read-only status properties), suppressed while `DialogueUI.IsAnyDialogueActive`, with no click-stealing colliders and no per-frame allocation. Estimate **8k–14k tokens**.
