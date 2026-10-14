# T05 - Integration and restrained polish

Status: **PLANNED - awaiting approval**
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
