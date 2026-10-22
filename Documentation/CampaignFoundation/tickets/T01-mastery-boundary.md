# T01 — Separate legacy score hearts behind a mastery boundary

Status: Planned -> Implemented, internally verified; user manual pending (2026-10-21). Blocked by: None.

References: [Campaign spec](../SPEC.md), [world and feedback](../WORLD_AND_FEEDBACK.md), [verification plan](../VERIFICATION.md), [progress log](../PROGRESS.md).

## Goal and visible result

Prepare the existing mastery system for a separate story campaign without changing what players see or how current scoring behaves. The player should finish a level, see the same XP, score hearts, badges, best result, and career availability as before. Internally, later tickets must have a clear seam between those accuracy records and the new public-trust, story-completion, wallet, and ownership records.

Before this ticket, LevelScoring.Passed, PlayerProgressData.RecordLevelResult, and career unlock checks treat score-heart thresholds as progression. After it, the legacy path still produces the same result and unlocks in the same situations, but callers can reach it through a narrow, named mastery boundary that later campaign code can coexist with.

## Ownership and work

The inspected ownership seam is Assets/Scripts/RewardsSystem/LevelScoring.cs for score totals and pass calculation; PlayerProgressData.cs for best LevelRecord, badge, XP, and legacy score-heart accumulation; PlayerProgressManager.cs for result persistence; CareerProgress.cs / CareerProgressService.cs for presentation data; and Assets/Scripts/UI/MenuEntry.cs for the current first-level route. Keep save ownership in PlayerProgressManager and SaveFileStore; do not introduce a singleton, PlayerPrefs counter, or alternate save document.

Make the smallest preparatory extraction that allows future commands to record mastery without using it as social trust or a story gate. Keep existing serialized field names, enum values, public UnityEvent bindings, save version, signatures, scoring formulas, result timing, and callers behaviorally compatible. Prefer a small pure result/record operation and a thin adapter around existing methods over a redesign. If a method signature must remain public for serialized callers, keep it as a forwarding compatibility entry point. Document which later campaign seam replaces active-story unlock decisions; do not switch those decisions in this ticket.

There is no new panel, label, bar, animation, or SFX in this ticket. Reuse of existing result presentation is intentional; the user should perceive no change.

## Non-goals

Do not add campaign schema, trust, New Game, menu buttons, chapter history, or new mastery scoring. Do not change thresholds, award values, level unlocks, tutorial data, save compatibility, scene wiring, or Level 1 behavior. Do not remove the old result path or broadly rename source symbols.

## Acceptance

Internal checks — implementation agent records evidence in ../PROGRESS.md:

- [ ] Existing score/pass examples produce the same totals, pass flag, best result, badge tier, and unlock answer before and after extraction. Internal evidence 2026-10-21: CampaignFoundationRegression compares MasteryBoundary against a frozen pre-T01 copy (pass-line grid, 300 random record sequences, 300 random ApplyRun sequences, level and chapter unlocks); 26/26. ProgressionRegression PASS, CareerBoardRegression 72/72 (same as pre-edit baseline).
- [ ] Repeat results still add only improvements to career XP and legacy score hearts; this refactor keeps the existing persistence timing and failure behavior unchanged. Internal evidence: tie/mixed/worse replay checks pass; ApplyLevelResult keeps the same RunId, practice, checkpoint-clear and single Save() order (source review).
- [ ] Current serialized callers and level result entry points still compile and route through the compatibility seam. Internal evidence: scratch Assembly-CSharp build 0 errors / 29 warnings (unchanged); public signatures of LevelScoring.Percent/Passed and PlayerProgressData.RecordLevelResult/IsLevelUnlocked kept as forwarders; Training 514/514, Guide 740, Level2 1326 checks PASS.
- [ ] A focused pure regression check covers boundary scores, ties, replays, and first-time records. Internal evidence: Tests/CampaignFoundationRegression (26 checks); a deliberate pass-line mutation was caught (2 FAIL), then discarded.

Implementation record: Assets/Scripts/RewardsSystem/MasteryBoundary.cs adds MasteryRun and MasteryBoundary (Percent, MeetsPassLine, RecordResult, ApplyRun, LegacyLevelUnlocked, LegacyChapterUnlocked). LevelScoring, PlayerProgressData, PlayerProgressManager and CareerProgress forward to it. The Legacy* unlock methods are the current story gate; T07 replaces active-story unlocks with corrected completion, T02 adds campaign data beside these fields, T08 adds Public Trust separately. Details in ../PROGRESS.md.

USER manual acceptance — only the user performs these checks:

- [ ] Finish an existing level once and replay it; compare its result and career card with the previous build.
- [ ] Confirm the current menu and career-board navigation still behave as they did before the refactor.

## Handoff

Report the exact extracted methods and preserved behavior in ../PROGRESS.md. T02 depends on this seam to add version-5 campaign data and define the fresh-start save boundary. Do not begin T02 inside this ticket.
