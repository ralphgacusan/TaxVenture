# T11 — Resume safely and finish Rosa's story

Status: Planned. Blocked by: T10.

## Visible accomplishment

The player can resume an interrupted investigation, complete a satisfying Rosa ending, see the neutral Assistance Desk clue, and return to the career board with accurate rewards and badge.

## Work

Extend existing PlayerProgressData/Manager and SaveFileStore ownership with an optional active Level2 checkpoint as specified in REWARDS_AND_VALIDATION. Do not change signing key, create another save owner or serialize scene references. Save frozen quest snapshot/tier, corrections, acquired evidence, phase, summary/findings/revision/stamp, audit/late penalties, remaining active time/day and reward flags. Restore to stable presentation without replaying domain actions. Old saves default safely.

Finalize accepted-case staging through existing CaseFinale/SceneFade/reward-card release. Reuse conference-room setup and Rosa's selected SM_Chr_Business_Female_03 identity, disabling stall duplicate while completion actor is active. Rosa responds to the actual path, then confirms Desk provenance. Save one neutral story fact; no accusation tree. Reuse any genuinely active Level1 wrap-up/archive flow; do not activate dormant legacy states or require an unexplained cabinet trip.

Calculate/store final result once: maximum 160 EXP / 9 hearts, existing 80% pass, best-tier badge, no repeat-farming. Clear checkpoint only after durable completed result. Career board/continue loads existing 3D menu and Level3 gate recognizes passed Level_02. Below-score math should not force a restart after prescribed mastery/caps.

## Acceptance

- [ ] Relaunch restores every meaningful checkpoint without resetting first grade/penalties.
- [ ] Completed save/badge/result cannot be awarded twice across resume or replay.
- [ ] Old saves work and signature contract preserved.
- [ ] Only approved case triggers Rosa finale/story note and Level Complete.
- [ ] No empty held reward card or duplicate active Rosa; Continue transitions once.
- [ ] Career best scores, badge and next-level gate reflect actual completion.

## Verification/handoff

Roundtrip/migration/interrupted-write/idempotence tests using existing save seams; career/progression suites, compile. User later checks resume at quest/audit/day rollover, ending, card timing and menu return. Next T12.
