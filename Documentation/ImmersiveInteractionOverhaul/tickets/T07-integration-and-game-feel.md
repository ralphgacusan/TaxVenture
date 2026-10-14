# T07 — Integration and restrained polish

Status: **PLANNED — awaiting approval**
Dependencies: T01-T06 accepted; unresolved blockers explicitly reviewed. Next: T08. Estimate: **8k-14k tokens**, depending on integration failures.

## Objective and inspected entry points

Combine controls, Elijah locomotion, camera transitions, conversations, papers and indicators into one coherent experience without input leaks or changed progression.

Primary surface: existing mobile/player scripts, DialogueUI and NPC adapters/FSM indicators, NotesPanelUI, desk/workstation exit UI, CorkboardSideQuestManager and CollectedProfilesWorldPanel, and their actual Office_Level1 bindings. Read the completed tickets' live file lists instead of assuming the original inventory is exhaustive.

## Scope

- Audit exploration -> dialogue/board/desk/notes/profile -> return transitions and consistency of ownership, touch reset, cursor, joystick visibility, body visibility and Animator state.
- Harmonize existing timing/press feedback without adding decorative visual effects.
- Prevent repeated entry, tap-through after close, mid-transition cancellation races, double callbacks, lost locks and competing camera writes.
- Check overlays and pause/focus/scene transitions, missing or disabled targets, rejected interactions, and rapid multi-touch.
- Preserve paper drop/bounce, collection flight, tray entrance, exact board pin position, readable viewer open/drag/topmost/return, and lock through return.
- Preserve case outcomes, quests/objectives, taxpayer conversations, office interviews and completion indicators. Compare data/events before and after, not just visuals.
- Fix only integration issues from approved tickets; report unrelated historical defects separately with evidence.

## Acceptance criteria

- [ ] All interaction routes restore third-person exploration and correct controls without jitter or stuck states.
- [ ] Joystick, dialogue, paper and overlay input do not leak into each other.
- [ ] Elijah animation/visibility agrees with movement locks and framing.
- [ ] Camera collision, close viewpoints and speaker framing cooperate in tight and open areas.
- [ ] Invalid/completed/unavailable actions give clear restrained feedback without starting a conflicting session.
- [ ] Callback counts, interview flags, objective/task state and paper counts match intended existing behavior.
- [ ] No new Console errors, orphan routines/listeners or scene serialization regressions.

## Verification and handoff

Compile and run relevant existing regression checks; run focused checks for reproduced integration failures. User/Unity manual: realistic end-to-end route plus fast repeated entry/exit, multi-touch, focus/pause, cancel during blends, and missing-target recovery. Record results per system and list unresolved defects with reproduction steps.

Update actual architecture/configuration, integration changes, test evidence, and remaining T08 matrix gaps. Do not use this ticket as an unlimited refactor. Stop for approval.

