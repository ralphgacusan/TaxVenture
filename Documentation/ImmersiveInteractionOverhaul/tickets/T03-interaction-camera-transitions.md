# T03 — Shared interaction cameras and transitions

Status: **PLANNED — awaiting approval**
Dependencies: T02 third-person ownership/compatibility and T01 input. Next: T04. Estimate: **12k-20k tokens**; split lifecycle foundation and adapter integration if needed.

## Objective and inspected entry points

Smoothly enter close first-person-style views from third-person exploration and restore exploration safely after every designated interaction.

Primary files: `Assets/Scripts/Player/CameraController.cs`, both exploration/first-person camera scripts, `FirstPersonHands.cs`, `Interactor.cs`, `WorkstationInteractor.cs`; `Assets/Scripts/Interactables/DeskInteractable.cs`, `CorkboardInteractable.cs` and NPC adapters; `Assets/Scripts/SideQuest1/CorkboardSideQuestManager.cs`, `CollectedProfilesWorldPanel.cs`; `Assets/Scripts/UI/NotesPanelUI.cs`, `DialogueUI.cs`; scene viewpoint/HUD references.

## Scope and ownership contract

- Evolve existing central camera owner, with explicit entering/active/returning behavior, rejected-entry handling and one pose writer. Preserve existing public methods/UnityEvents or migrate all consumers deliberately.
- Supply appropriate references/configuration for ordinary NPCs, four interview-target taxpayers, office interview, desk, main board/side quest, profile viewer and objective notes.
- Reuse T02 collision safety for the entire transition path; validate target clearance and use a safe fallback rather than an invalid final pose.
- Snapshot/restore owned input, camera/FOV/pivot, joystick state, Rigidbody constraints, cursor, world raycasts, visual/body/hands. Restore third-person yaw/pitch without snapping.
- Reject competing interactions or handle a documented handoff. End/cancel/missing target/disabled owner/scene unload must release only the appropriate owner and never complete a quest by cancellation.
- Coordinate existing paper animation stages: close viewpoint before/with readable viewer presentation, retain drag/layering, and wait for paper return plus camera return before releasing gameplay controls.
- Migrate notes' current non-locking policy explicitly; preserve task content, floating window and swipe-close behavior.
- Define session events usable by T04/T05 without implementing their typewriter/framing work here.

## Acceptance criteria

- [ ] All listed entry paths blend from exploration into a suitable close view.
- [ ] Exiting/cancelling every path blends back to third person with correct controls.
- [ ] No simultaneous follow/first-person/transition writes and no camera/model/geometry clipping.
- [ ] Camera and movement remain locked when required, including closing/returning transitions.
- [ ] Rapid toggles, rejected starts, missing/disabled targets, repeated close, focus changes and scene unload do not strand state or restore controls early.
- [ ] Notes, desk, boards, profiles and interviews preserve original data/outcomes.
- [ ] Profile viewer remains readable and its opening, drag/topmost selection and return animations remain intact.

## Verification and handoff

Compile and inspect every changed call site/scene event. Add state-transition tests for admitted/rejected/cancelled/returning requests where feasible. Manual matrix: exploration -> each listed interaction -> exit; interrupt at entry/exit; close profiles during arrival; notes swipe close; collisions during blends; unrelated UI overlay ownership.

Document state/ownership and compatibility decisions, source paths, tested/unrun cases and next action. No new dialogue content, quest policy or camera package. Stop for approval.

