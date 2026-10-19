# T11 — Resume safely and finish Rosa's story

Status: Implemented — internally verified; user Play Mode pending (2026-10-19). Blocked by: T10.

## Visible accomplishment

The player can resume an interrupted investigation, complete a satisfying Rosa ending, see the neutral Assistance Desk clue, and return to the career board with accurate rewards and badge.

## Work

Extend existing PlayerProgressData/Manager and SaveFileStore ownership with an optional active Level2 checkpoint as specified in REWARDS_AND_VALIDATION. Do not change signing key, create another save owner or serialize scene references. Save frozen quest snapshot/tier, corrections, acquired evidence, phase, summary/findings/revision/stamp, audit/late penalties, remaining active time/day and reward flags. Restore to stable presentation without replaying domain actions. Old saves default safely.

Finalize accepted-case staging through existing CaseFinale/SceneFade/reward-card release. Reuse conference-room setup and Rosa's selected SM_Chr_Business_Female_03 identity, disabling stall duplicate while completion actor is active. Rosa responds to the actual path, then confirms Desk provenance. Save one neutral story fact; no accusation tree. Reuse any genuinely active Level1 wrap-up/archive flow; do not activate dormant legacy states or require an unexplained cabinet trip.

Calculate/store final result once: maximum 160 EXP / 9 hearts, existing 80% pass, best-tier badge, no repeat-farming. Clear checkpoint only after durable completed result. Career board/continue loads existing 3D menu and Level3 gate recognizes passed Level_02. Below-score math should not force a restart after prescribed mastery/caps.

## Acceptance

Internal status (source/data checks only; each box also needs the user's Play Mode check):

- [x] Relaunch restores every meaningful checkpoint without resetting first grade/penalties.
- [x] Completed save/badge/result cannot be awarded twice across resume or replay.
- [x] Old saves work and signature contract preserved. *(save version 4 kept; same signed envelope)*
- [x] Only approved case triggers Rosa finale/story note and Level Complete.
- [x] No empty held reward card or duplicate active Rosa; Continue transitions once. *(existing CaseFinale card release; Rosa in the finale is a visual-only copy, see KNOWN_ISSUES)*
- [x] Career best scores, badge and next-level gate reflect actual completion.

## Implementation (2026-10-19)

`PlayerProgressData.activeCheckpoint` (`LevelCheckpoint`: level/case id, review JSON, taxpayer facts as names, transferred fields) and `storyFacts`, inside the existing signed save. `PlayerProgressManager.SaveCheckpoint` / `GetCheckpoint` / `RecordStoryFact`; `ApplyLevelResult` clears the level's checkpoint in the same write as the result. `Level2Checkpointer` (part) saves at every milestone event, every 30 s while the review runs and on pause/quit, never after approval; on entering Level 2 with a checkpoint for Rosa's case it restores the review (`Level2Checkpoint.Normalize` turns Unity's blank records back to null), the taxpayer facts, the folder (desk or icon), the stall records, the objective list (`ImpliedTriggers` via `OfficeObjectives.MarkDone`) and the score already earned (Rosa talk, quest award with its frozen tier, audit counters, late flag), raising no gameplay events. `CaseFinale` is now opted in by the level profile (`OfficeFeature.Finale`; Level 2 level.json adds "finale"); its tier for a review case comes from returned submissions and the cutoff; Rosa's lines (great/good/okay) end with the neutral Desk confirmation. Approval saves the story fact `rosa-desk-provenance` once.

## Verification/handoff

Roundtrip/migration/interrupted-write/idempotence tests using existing save seams; career/progression suites, compile. User later checks resume at quest/audit/day rollover, ending, card timing and menu return. Next T12.


### Consistency follow-up (2026-10-19)

Implemented — internally verified; user Play Mode pending. See SPEC and PROGRESS, “Level 1 consistency follow-up”, for the current six-page folder, shared clock/splash, receipt papers, annotated HUD collection and direct-Editor checkpoint policy. Earlier implementation descriptions above remain historical.
