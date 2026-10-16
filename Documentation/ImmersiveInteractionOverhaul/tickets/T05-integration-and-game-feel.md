# T05 - Integration and restrained polish

Status: **IMPLEMENTED (2026-10-15) - internal checks passed; manual Unity acceptance pending.** See "Delivery" at the end.
Dependencies: T01-T04 accepted; unresolved blockers explicitly reviewed. Next: T06. Rough estimate: **10k-16k tokens**, depending on integration failures and conversation return-state issues.

## Objective and inspected entry points

Combine the retained first-person controls, conversation-only OTS presentation, dialogue UI, papers and indicators into one coherent experience without input leaks or changed progression.

Primary surface: existing mobile/player scripts, DialogueUI and NPC adapters/FSM indicators, NotesPanelUI, desk/workstation exit UI, CorkboardSideQuestManager, CollectedProfilesWorldPanel and their actual Office_Level1 bindings. Read the completed tickets' live file lists instead of assuming the original inventory is exhaustive.

## Scope

- Verify T01 controls, T02 conversation events, T03 presentation and T04 markers cooperate, including speaker changes, availability/completion updates and active-conversation marker suppression. Resolve reproduced integration failures without rewriting working systems.
- Confirm every office and town NPC route uses the shared T02 conversation flow, including all four taxpayer targets and the tutorial/guide. NPC lines frame the NPC over Elijah's shoulder, player lines frame Elijah over the NPC's shoulder, and the two-shot fallback appears only when a clear shot/reverse-shot cannot be framed. Confirm OTS presentation starts and ends with dialogue and does not alter first-person exploration.
- Check that the standing Elijah visual uses existing assets and remains grounded while dialogue is active. Verify conversation presentation does not move/teleport the gameplay player root, resize its collider, change shared NPC importers or add NPC animation.
- During dialogue, movement and look remain frozen. On natural end and cancellation, verify the camera returns to the captured first-person yaw, pitch and FOV, hands/visual visibility return to their captured state, and input remains locked until the return blend completes. Cover cancellation during entry, an active shot and return where feasible; preserve the existing camera/control owner.
- Check the compact bottom safe-area panel with short and long lines and speaker changes. Verify a tap on the box or Continue while typing reveals only the current line; a distinct later gesture advances, or ends the final line; opening input and duplicate/held tap, button or submit events cannot skip, advance or close twice.
- Keep existing touch feedback and dialogue/UI timing consistent; use restrained polish rather than adding unrelated effects.
- Check safe cancellation and control restoration on the affected conversation and existing camera-return paths; use this ticket only to fix demonstrated integration bugs within those paths. Confirm dialogue-panel input cannot move/look/interact through the UI.
- Check that closing the profile/paper viewer after open, drag or layering returns input and controls correctly, preserving the existing lock through its return animation. Fix only demonstrated paper-closing regressions.
- Exercise repeated entry, tap-through after close, cancellation races, duplicate callbacks or competing camera writes only as needed to reproduce a bug in those return paths.
- Preserve paper drop/bounce, collection flight, tray entrance, exact board pin position, readable viewer open/drag/topmost/return, and delayed control restoration.
- Keep the current camera architecture and existing ownership rules. Do not create a standalone camera/focus architecture or redesign the notes-lock policy.
- Confirm the scoped fixes preserve case outcomes, quests/objectives, taxpayer conversations, office interviews and completion indicators; compare data/events before and after, not just visuals.
- Report unrelated historical defects separately with evidence; do not expand this ticket to resolve them.

## Acceptance criteria

- [ ] Checked conversation routes use shot/reverse-shot for the active speaker, keep it scoped to dialogue, and return to the retained first-person exploration view with no jitter or stuck states.
- [ ] The player remains grounded in the dialogue visual without gameplay-root movement or collider resize; first-person movement/look are frozen only for the conversation.
- [ ] Natural end and cancellation restore captured yaw, pitch, FOV, hands/visual visibility and input state; controls resume only after the return blend finishes.
- [ ] The compact dialogue panel respects safe areas; reveal-current-line and distinct-advance/close gestures remain separate, and opening/duplicate/held input cannot skip, advance or close twice.
- [ ] T02 session completion, terminal callbacks/success outcomes and control release wait until the conversation return blend completes.
- [ ] Joystick, dialogue and paper input do not leak during the checked cancellation and close/return paths.
- [ ] Existing camera/control owners agree during conversation entry and return and the other checked paths; no competing camera writes or restoration over an active owner occur.
- [ ] Invalid, completed and unavailable actions give clear restrained feedback without starting a conflicting session.
- [ ] Conversation events, dialogue presentation and NPC markers stay synchronized, including cancellation and later available NPC actions.
- [ ] Callback counts, interview flags, objective/task state and paper counts match intended existing behavior.
- [ ] No new Console errors, orphan routines/listeners or scene serialization regressions.

## Verification and handoff

Compile and run relevant existing regression checks; run focused checks for reproduced integration failures. User's Unity manual checks: representative office/town conversation with dialogue UI and markers, then cancel during conversation camera focus/return; close the paper/profile viewer after opening, dragging and changing layering; verify the existing delayed control restoration. Use repeated entry or other interruption cases only when reproducing a scoped failure. Record results and unresolved defects with reproduction steps.

Update only the affected integration behavior, test evidence and remaining regression-matrix gaps. Do not use this ticket as an unlimited refactor. User performs Unity manual testing and Git write actions. Stop for approval.

## Delivery (2026-10-15)

The user approved T05 and asked for internal checks only; the user runs the Unity tests and handles Git. T01-T04 manual acceptance is still outstanding, so the defects below come from a static audit of the live code. Each one has a concrete reproduction to confirm in Unity, and none of them comes from a playtest.

### Paths audited

`CameraController` (all modes, the profile-viewer snapshot, the dialogue hold and `Lock/UnlockPlayerControls`), `DialogueUI` terminal ordering, `DialogueCameraDirector.BeginReturn`, `CollectedProfilesWorldPanel` open, close and cleanup, `NotesPanelUI` (it touches no controls, which is the preserved policy), the desk and corkboard entry points with `WorkstationUI` exit, all `CorkboardSideQuestManager` exits, `Interactor`, `WorkstationInteractor`, `MobileInputGuard`, `VirtualJoystick` hide and reset, the town pointer-click NPC and pickup routes, and every popup caller of `Lock/UnlockPlayerControls` (Tutorial, AuditSummary, CaseResult, ArchiveConfirmation, CaseFolder, StampCaseFolder, TaxCodeBook, TaxReturnViewer, InterviewClient).

### Defects found and fixed

1. **A popup unlocks controls mid-conversation.** `TutorialController` shows its first step on a 5 s timer, and `TutorialUI` calls `LockPlayerControls`/`UnlockPlayerControls` around it.
   - Reproduction: talk to the receptionist within 5 s of loading, then press Continue on the tutorial popup.
   - Result before the fix: `SetFirstPersonSystems(true)` re-enabled look and movement and showed the joystick during the conversation, so `FirstPersonCameraController` fought `DialogueCameraDirector` for the camera.
   - The reverse case also failed: if a popup was open when a conversation started and closed during it, the conversation's release restored the _locked_ snapshot and the player stayed stuck.
   - Fix: while a conversation holds the controls, `CameraController` records the popup's last request instead of applying it. On release, a pending **Locked** keeps the controls locked, so the popup unlocks them when it closes. A pending **Unlocked** restores first-person systems. Body constraints and the cursor are always restored.
2. **The profile viewer opens during a conversation or under a popup.** The HUD folder icon stays clickable, and `TryBeginProfileViewer` checked only the camera mode, which is FirstPerson during dialogue.
   - Result before the fix: the viewer snapshotted the dialogue-locked controls, and the conversation's release then unfroze the body under the open viewer. Closing the viewer restored the locked snapshot, leaving movement and look stuck off.
   - Fix: `TryBeginProfileViewer` refuses while a conversation holds the controls or while `firstPersonCamera` is disabled (a popup lock). The HUD click is silently ignored, as it already was for other refused states.
3. **The closing tap passes through to the world (touch).** The tap that closes a view is released in the same frame that `Interactor` is re-enabled: the last dialogue line (returns are instant cuts since T03 revision 1), the desk or corkboard exit, or the viewer's final return.
   - The closing UI is already hidden in that frame, so `MobileInputGuard.TryGetWorldTap` sees no UI under the touch and reports a world tap. The centre ray can then immediately re-open the desk or restart a repeatable NPC conversation.
   - Fix: `MobileInputGuard.SuppressWorldTapsThisFrame()` is called when `CameraController` restores first-person systems, releases a conversation, or ends the viewer. `TryGetWorldTap` ignores taps for that one frame only. Pointer-click NPC and pickup routes are unaffected, because their press and release must land on the same object.

### Files changed

- `Assets/Scripts/Player/CameraController.cs`: deferred lock/unlock while a conversation holds the controls, viewer refusal, and world-tap suppression on restore. There are no serialized, enum or public API changes.
- `Assets/Scripts/Mobile/MobileInputGuard.cs`: `SuppressWorldTapsThisFrame()` and a one-frame check in `TryGetWorldTap`.

No scene, prefab, asset or data changes were made.

### Checked and left unchanged

- Notes: they never touch controls, so movement and look stay allowed as the preserved policy requires.
- Corkboard: exits are idempotent and mode-guarded.
- Dialogue terminal order: hide, return, release, callback.
- Joystick: hidden joysticks reset their input.
- Desk and corkboard entry during a conversation: impossible, because both interactors are disabled.
- Dialogue start during a viewer, desk, board or transition: refused by `CanStartDialogue`.

`CanStartDialogue` was deliberately **not** extended to refuse when a popup is open, because the guide's final post-side-quest conversation can legitimately follow a popup. The deferred-request fix covers that case safely.

### Evidence

- PASS: `Assembly-CSharp` compiles against the installed Unity 6000.4.5f1 `Managed` assemblies and `Library/ScriptAssemblies` package DLLs, using the generated project's source list and defines. The result is 0 errors and only the 2 pre-existing warnings (`ComputedResultSource.cs` CS0105 and `ClientInteractable.cs` CS0184).
- Not re-run: the dialogue and controls regression projects, because they do not include the changed files.
- NOT RUN: Unity Play mode, devices, and every manual case below.

### Manual checks for the user

1. Load `Office_Level1` and start a receptionist conversation before the tutorial popup appears (within 5 s). Press Continue on the popup. Expect the conversation camera to stay steady and no movement or look to be possible. After the last line, controls return.
2. Repeat, but let the popup appear first and then talk (on a desktop, use a pointer-click NPC). Close the popup mid-conversation. After the conversation, controls return.
3. In town during any conversation, tap the HUD folder icon. Expect nothing to open. After the conversation, open and close the viewer (after dragging and re-layering papers). Expect controls only after the papers finish returning.
4. On a touch device, sit at the desk, face it, and tap Exit Desk. Expect no immediate re-entry. End a repeatable NPC conversation (auditor or receptionist reminder) with a box tap while facing the NPC. Expect no restart.
5. Re-run a representative T02, T03 and T04 conversation with markers to confirm no regression.
