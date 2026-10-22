# T04 — Resume plaza conversations, profiles and fieldwork

Status: Planned -> Implemented, internally verified; user manual pending (2026-10-22). Blocked by: [T03 — New Game and Continue](T03-new-game-and-continue.md).

References: [Campaign spec](../SPEC.md), [world and feedback](../WORLD_AND_FEEDBACK.md), [verification plan](../VERIFICATION.md), [progress log](../PROGRESS.md).

## Goal and before/after flow

A player who leaves Level 1 during town exploration can Continue without repeating conversations, losing collected profiles, or seeing different board placements. Before this ticket, taxpayer profiles and board phase/placements are runtime-only, so relaunch recreates the opening run. After it, Continue restores the campaign to a safe town or board point and reconstructs those facts from saved IDs while preserving the board's current scoring and completion loop.

Persist the current board phase and placement snapshot in a forward-compatible shape that T06 can extend with first-check and frozen-grade fields. Do not convert placements to drafts, add a Check Board action, or change today's feedback/scoring/completion behavior here; T06 performs that behavior change. Storing stable profile/placement IDs now allows its later check state to extend the same Level 1 checkpoint without breaking T04 saves.

## Ownership and mechanics

The inspected flow spans Assets/Scripts/SideQuest1/TaxpayerNpcInteractable.cs and TutorialNpcController.cs for conversations and handoff, TaxpayerProfileWorldPickup.cs / TaxpayerProfileCollectionHUD.cs for profile collection, CorkboardSideQuestManager.cs for placements, OfficeObjectives.cs for objective presentation, and PlayerProgressData / PlayerProgressManager for the existing checkpoint owner. Add plain checkpoint fields to the existing v5 campaign record and a small Level 1 adapter near the Level 1 gameplay owners; do not create a second save owner. Proposed future file names may follow the existing Level2Checkpointer pattern, but exact new paths are for the implementation agent to choose.

Persist stable NPC/profile IDs, completed conversation IDs, profile acquisition IDs, current side-quest board phase/attempt state, objective trigger IDs, and stable placement IDs in the shape the current board needs. Include optional forward-compatible fields for T06's later full-check/frozen-grade snapshot, left unset until T06 owns them. Do not serialize scene objects, camera transforms, held drag state, animation frame, answer keys, or computed correct/incorrect flags. Normalize unknown and duplicate IDs deterministically. A missing or incompatible checkpoint starts from the nearest safe phase and reports a recoverable reset instead of restoring a half-valid world.

Save after each stable conversation/profile handover/placement milestone, on pause/quit, and before menu exit. For dirty running state, use bounded periodic writes consistent with the shared save contract. Build a complete candidate first; on failure retain the prior durable checkpoint and current run, display retry feedback, and do not emit collection/award events. Restore world and objective visuals as an idempotent projection of data: no repeated conversations, profile pickup, score event, or tutorial award. Spawn at a safe marker with no held input.

Use the existing profile pickup objects, board, and objective presenter. Claimed profile objects should appear already collected only after the profile's required handover, so required source objects are not hidden prematurely. No new animation or sound is required; reuse current profile/board feedback and do not add a correctness cue in this preparatory ticket.

## Non-goals

Do not implement first-check rules, mastery tier, trust, coins, case review/clock, the bag, world pickups, or menu layout. Do not rewrite profile content, taxpayer facts, movement, or interaction framework.

## Acceptance

Internal checks — implementation agent records evidence in ../PROGRESS.md:

- [ ] Round-trip each town phase, profile IDs, current board placements/attempt phase, and objective state through the existing signed save without changing existing scoring or completion behavior. Internal evidence 2026-10-22: CampaignFoundationRegression writes six phases (first talk, partial collection, all collected, open board with pins/attempts, solved, rewarded) through SaveFileStore + signed envelope, reloads, normalizes and validates: identical, Restored. Board feedback/scoring/completion code paths unchanged (only additive restore methods).
- [ ] Restore raises no gameplay events and does not duplicate score, conversation, collection, or objective rewards. Internal evidence: source contracts - restore raises no TownEvents/GameplayEvents, never calls CollectProfile/BeginSideQuest/TryStartDialogue/HandleCardDropped/PlayEffect; score re-earned once with LevelScoreLedger.Restoring (no EntryAdded); objectives via RestoreTriggers with HudFeedback silent; guide counters via Fire only.
- [ ] Duplicate/unknown IDs and absent checkpoint normalize deterministically; old v5 data without these fields loads safely. Internal evidence: messy checkpoint normalizes deterministically and idempotently (unknown/duplicate ids, collected-implies-talked, one pin per card/area, counters, unknown phase, board without four profiles -> town, intro without board -> undone); newer format -> Reset; absent -> None; a v5 save without levelOne loads with no checkpoint.
- [ ] Failed writes preserve the last durable checkpoint and permit retry; pause/quit and menu exit save stable state. Internal evidence: SaveLevelOneCheckpoint puts the previous record back when Save() fails; checkpointer retries after 5 s and tells the player once; OnApplicationPause/Quit save; InGameSettings.LeaveLevel waits for PrepareSettingsExit (menu saves, replay clears).
- [ ] Restored player spawn and controls do not retain a held drag, modal, or stale focus. Internal evidence: scene loads at its normal spawn (no saved transforms); restored cards are Placed with no pending slot; no dialogue or modal is opened by restore except an owed guide wrap-up through its normal one-shot path.

Implementation record: Level1FieldCheckpoint / Level1Placement / Level1SlotAttempts in PlayerProgressData.cs (CampaignRecord.levelOne); Assets/Scripts/SideQuest1/Level1FieldCheckpointRules.cs (validation) and Level1FieldCheckpointer.cs (runtime save/restore under the Campaign Foundation root); restore hooks in TaxpayerNpcInteractable, TaxpayerProfileCollectionHUD, TutorialNpcController, CorkboardSideQuestManager, CorkboardSlot, ProfileDragCard; quiet restore in LevelScoreLedger, ObjectiveLog, OfficeObjectives, HudFeedback; save-owner API in PlayerProgressManager; menu-exit hook in InGameSettings. Details in ../PROGRESS.md.

USER manual acceptance — only the user performs these checks:

- [ ] Leave during town conversations, after partial profile collection, and during current board placement/attempt feedback; Continue each time.
- [ ] Confirm the player returns to a safe location and sees the same profiles, objective state, and placements without restore inventing additional feedback or score.
- [ ] Confirm a required profile remains available until its handover is complete.

## Handoff

Document the persisted IDs and safe restore locations in ../PROGRESS.md. T05 adds case, form, transfer, audit, and clock state. T06 then changes the saved board from the preserved legacy attempt loop to the four-slot draft/check/correction loop.
