# T04 — Unified conversation lifecycle

Status: **PLANNED — awaiting approval**
Dependencies: T03 shared ownership/camera session contract. Next: T05 (T06 status work may prepare read-only). Estimate: **10k-18k tokens**; callback migration is the major uncertainty.

## Objective and inspected entry points

All relevant NPCs use one safe start/advance/end/cancel lifecycle while retaining individual content, conditions and successful outcomes. DialogueUI is already shared: improve it rather than add a competing dialogue manager.

Primary files: `Assets/Scripts/UI/DialogueUI.cs`, `InterviewClientUI.cs`; `Assets/Scripts/Data/DialogueLine.cs`, `DialogueBuilder.cs`, `CaseFactory.cs`, case definitions/content; `Assets/Scripts/Interactables/ReceptionistInteractable.cs`, `ColleagueInteractable.cs`, `ClientInteractable.cs`, `AuditorInteractable.cs`; `Assets/Scripts/SideQuest1/TaxpayerNpcInteractable.cs`, `TutorialNpcController.cs`, `TownNpcDialogueLibrary.cs`, `TownEvents.cs`; NPC FSM scripts.

## Scope

- Map every caller's data source, availability, replay/branch conditions, lock acquisition, successful completion and cancellation effects before migration.
- Use adapters for office IInteractable and town pointer activation; same lifecycle/input rules and underlying UI for both.
- Reject a simultaneous start safely rather than overwrite the stored queue/callback. One admitted session owns its state.
- Define once-only completion/cancel/reject outcomes; resolve empty-list handling without pre-lock leaks.
- Preserve case JSON/builder dialogue, conditional audited-result selection and existing live branch/question behavior. Inspect InterviewClientUI Inspector bindings before classifying it unused.
- Preserve receptionist notes/case/tutorial signals; client interview and presentation distinction; auditor summary/reward/retry sequencing; four taxpayer profile drops; tutorial/board events.
- Emit line/speaker and availability/completion notifications for T05/T06. Leave typewriter/presentation polish to T05.
- Do not reclassify NPC completion or rewards simply because UI closes; ordinary dialogue exhaustion differs from later available case-specific dialogue.

## Acceptance criteria

- [ ] Relevant indoor/outdoor NPCs and all four taxpayer targets use the shared lifecycle.
- [ ] Start, advance, skip hook, end and cancel input routes are consistent; no duplicate simultaneous conversation.
- [ ] Empty/invalid/busy/cancelled starts leave no locks or lost prior callback and produce no unintended progress.
- [ ] Successful outcomes fire once; existing interview/profile/objective/tutorial/reward/result semantics remain unchanged.
- [ ] Repeated/already-interviewed NPC behavior and later client presentation remain available as intended.
- [ ] JSON conditions/response paths remain functional; no unnecessary new branching authoring system.
- [ ] A new NPC can supply content/targets/outcomes through the shared path instead of copying a controller.

## Verification and handoff

Compile; test data conversion and session lifecycle/once-only outcomes at the highest existing seam feasible. Unity/manual: representative office roles, each taxpayer, tutorial NPC, already-completed/repeated conversations, empty content, double-start, cancel, retry and outcome branches. Record exact callbacks tested. Existing PlayerProgressData tests do not cover these behaviors.

Record historical audit defects separately; address a duplicate audit caller only if necessary to the approved lifecycle migration and with evidence. Update docs and preserve scene bindings. Stop for approval.

