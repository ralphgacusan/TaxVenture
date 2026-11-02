# T06 — Check, correct and collect the worksheet with frozen mastery and service rewards

Status: **Implemented, internally verified; user manual pending**. Blocked by: T03, T05.

Priority: P1. Phase: B — Side-quest completion. Estimated scope: M; target one fresh agent context. If unexpected shared refactoring is required, document and split it before broadening this ticket.

## Goal and visible accomplishment

The player calculates subtotals, deliberately freezes one 12-decision grade, corrects the worksheet and acquires the case input with once-only 60 coins/+2 trust.

This is local Level3 numbering. Read [AGENTS](../AGENTS.md), [SPEC](../SPEC.md), [CONTENT](../CONTENT.md), [source map](../REUSE_AND_ARCHITECTURE.md), [presentation](../WORLD_AND_FEEDBACK.md), [rewards](../REWARDS_AND_VALIDATION.md), [verification](../VERIFICATION.md) and this ticket before execution. Implemented 30 October 2026 on an implementation instruction; evidence in [PROGRESS](../PROGRESS.md), limitations in [KNOWN_ISSUES](../KNOWN_ISSUES.md).

## Existing systems to reuse

Verified existing paths relative to repository root; recheck live behavior rather than rely on stale comments:

- `Assets/Scripts/Data/IncomeChallenge.cs`
- `Assets/Scripts/RewardsSystem/LevelScoring.cs`
- `Assets/Scripts/RewardsSystem/LevelRewardRules.cs`
- `Assets/Scripts/RewardsSystem/CampaignRewards.cs`
- `Assets/Scripts/RewardsSystem/LevelScoreLedger.cs`
- `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs`

New L3 components/resources mentioned below are proposed outputs, not verified existing APIs. Prefer existing owners/adapters and level configuration; do not copy global frameworks.

## Work and interaction mechanics

1. Connect T03 calculator to selected Income/Expense rows and attach actual subtotals/tapes. A treatment change makes affected tape stale rather than auto-answering. Accept addition matching the selected set even if treatment is wrong; count treatment and arithmetic as distinct skills.
2. Implement deliberate Check worksheet. Completeness checks seven destinations, two reasons, one relationship and two current subtotal tapes. Incomplete shows missing work and freezes nothing. Unlike L1 automatic fourth-card check, filling the final field does not trigger a scored attempt.
3. First complete Check freezes correct/total/wrong IDs/tier once. Gold 12, Silver 10–11, Bronze 0–9; XP 60−first wrong, floor 48. Preserve feedback/correct fields; requested hints progress from source/concept to specific help without moral penalty. Corrections never change first tier/XP.
4. Require all twelve semantic decisions and correct current totals before Ready. Player explicitly acquires supported worksheet. Save mastery award, corrected/acquired phase, folder reference, 60-coin and +2-trust distinct claims together. Only after save show paper flight/receipt/hearts; failure leaves the ready source retrievable.
5. Office case must use this exact acquired worksheet, not a newly generated answer-key summary. Branch service-plan discussion can occur after acquisition but does not gate/check/grade the side quest or force a new CHECK-to-NPC detour. Exit/reload and duplicate callbacks preserve frozen first grade and once-only grants.

## Assets, motion and feedback

Existing result card/badge/coin/trust presentation; one amber receipt-emblem Worth the Tax? badge configuration. Ready worksheet is a container document, no extra Bag slot.

Follow WORLD_AND_FEEDBACK and fixed document types. Bind every new world object to actual 3D body, collider/target, correct source/record identity and existing camera/input/audio ownership. Record resolved anchors/clip names and any unsupported asset assumption. No user scene-assembly chores or claims of rendered acceptance from declarations.

## Integration requirements

Extend fixed CampaignRewards catalogue and save owner for Level_03; never call Rosa-specific ethics deltas. Existing L2 remains 16 first-check decisions. T07 consumes supported worksheet; story completion still independent of mastery thresholds.

Preserve current L1/L2 scenes, source prefabs/GUIDs, signed save owner, mode guards, input/FSM/dialogue/audio frameworks and Android controls. Normal L3 Career remains unavailable until T18. No L4 filing, ending, stand, drawer or global archive feature is added.

## Persistence and failure rules

Use the existing candidate→save→publish command boundary and stable chapter/case/action IDs. Validate actual prerequisites/revision/source before committing. Failed write leaves prior state, source/copy and retry available, with no success effect. Quiet restore does not raise acquisition/reward/choice/consumption events. Back, repeated input, disable and app focus interruption settle pointer state and release only owned holds. Story/replay/practice/Editor distinctions protect live campaign data.

## Internal checks — AI source/build/regressions only

- [x] Incomplete/ungraded vs first complete/graded vs corrected/mastered/acquired transitions and all 12 decisions have behavioral tests. (An empty, a tape-less and a stale-tape worksheet are Incomplete and freeze nothing; the first complete Check freezes once (WorksheetDraft -> WorksheetCorrections, or -> WorksheetReady when perfect); a corrected re-check reaches WorksheetReady; collection moves to BeaComputation; every decision is exercised right and wrong through the real calculator and commands.)
- [x] Wrong destination plus internally correct chosen-set addition does not double-penalize; final correct totals 900,000/300,000 required. (FIXTURE_CONTRACT's examples: 324,000 and 330,000 sets -> 11/12, 354,000 -> 10/12, a 300,000 tape on that set -> 9/12; R5+R7 each once earns the arithmetic; a strip added twice, a subtraction, a typed or chained number never does; Ready needs every decision and current 900,000/300,000.)
- [x] Gold/Silver/Bronze and XP 48–60 freeze; repeated corrections/acquisition/resume do not farm XP/60 coins/+2 trust. (Tier/XP equal the ledger's side-quest maths for every wrong count; a 0/12 first check corrected to Ready stays Bronze/48; Acquire twice is a Duplicate; applying the collection again, after a reload or a successful retry is AlreadyClaimed; trust clamps at 100.)
- [x] Failure/reload round trip retains source, first result, actual acquisition and dependent next objective in one transaction. (Disposable signed store: a failed check write grades nothing; a failed collection write leaves WorksheetReady, no coins/trust/claim/badge and both save files byte-identical; the retry writes BeaComputation, the folder work id, 60 coins, +2 trust, the mastery claim and badge in one save; resume implies WorksheetReady/WorksheetCollected quietly; a T05 save without the new fields resumes without a rewrite.)
- [x] Run targeted behavioral/serialization/failure checks and affected existing regressions named in VERIFICATION for the actual changes. Build changed production paths before tests that load their DLL. Record exact commands/results/warnings; no Unity/editor launch or assumed pass count. (Commands and counts in PROGRESS; suites built out of tree; a two-mutation run proved the freeze and once-only checks bite.)

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] User checks deliberate scoring warning, clear feedback without answering, correction/Undo feel, actual collection motion and exact post-save reward reasons.
- [ ] Inspect applicable actual imported references, 3D bodies, paper text, targets, motion, camera/control recovery and Android layout. Manual acceptance remains pending until the user reports it; static tests are not a substitute.

## Handoff and completion definition

Required side quest complete with usable supported worksheet and durable reward boundaries. Next T07.

Direct dependents: T07, T13, T16. Record actual changed source/content/assets, internal evidence, preserved baseline limitations and remaining user checks in [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md). Mark implemented/internal-verified only after real entry points and checks work; Git operations and user acceptance remain user-owned. No commits, scene edits or gameplay implementation are performed by authoring this ticket.
