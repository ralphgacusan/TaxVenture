# T16 — Reuse practice and mastery replay with version-aware grade isolation

Status: **Implemented, internally verified; user manual pending**. Blocked by: T06, T14. (6 November 2026; Unity/device acceptance is the user's.)

**6 November 2026 implementation:** The existing Tutorial Hub (training.json → TrainingBinder3D → TrainingSession → PracticeModeRuntime, the drill script on GuideDirector) gains one "Bea's Case" tab, hidden until a Chapter 3 lesson is seen or the chapter is completed, with two cards: **Worth the Tax?** (drill `l3_machine`, starts at Bea's handed-over records: feed, choose, lever, check all three batches) and **Three Office Stages** (drill `l3_office`, starts at her collected summary at the desk with the calculator taught: taxable income, tax due, certificate credit and remaining payable). Each drill names a `TrainingDrill.fixture` (new field); Level3Checkpointer builds that chapter with the real commands (Level3EditorFixtures, now also compiled for builds) and restores it quietly; practice never saves (existing PRACTICE ribbon and LEAVE, lesson-only training record). A Career mastery replay of the completed chapter plays it locally; its acknowledged result writes only the best-only mastery record (`Level3ClosingRules.ApplyReplayBest` via `PlayerProgressManager.CommitLevelThreeReplay`, one signed write; v1/v2 compared by their own tiers and the shared 60-point worksheet XP scale, never raw counts or sums; the story's frozen grades, snapshot, wallet, Trust, claims, possessions and history are byte-equal). Replay/practice result cards show the run mode and their own XP, never the story's coins/Trust. Details: latest PROGRESS; limitations KNOWN_ISSUES L3-R124–L3-R129.

Priority: P1. Phase: F — Learning replay. Estimated scope: M; one fresh agent context. If real call-site work exceeds this bound, record a bounded split before adding unrelated framework work.

## Goal and visible accomplishment

Replay unlocked batch/computation lessons or eligible chapter mastery through existing binder/Career without changing live campaign work, possessions or service outcomes.

Revision: **3 November 2026**. Read [REVISION_PLAN](../REVISION_PLAN.md), [SPEC](../SPEC.md), [reuse map](../REUSE_AND_ARCHITECTURE.md), [visual contract](../WORLD_AND_FEEDBACK.md), [verification](../VERIFICATION.md) and [AGENTS](../AGENTS.md). Planned refers to changed revision acceptance, not absence of reusable implementation. Historical baseline: Shared Practice/Career/mode isolation exists; revised batch assessment and computation must adopt it, not create another tutorial menu or score ledger.

## Existing systems to reuse

Existing source paths relative to repository root; inspect live callbacks/content before changing them. Narrow Level_03 adapters only; no duplicate folder/stamp/calculator/input/save/shop framework.

- `Assets/Scripts/Training/TrainingSession.cs`
- `Assets/Scripts/Training/PracticeModeRuntime.cs`
- `Assets/Scripts/RewardsSystem/CampaignRun.cs`
- `Assets/Scripts/RewardsSystem/CareerProgressService.cs`
- `Assets/Scripts/RewardsSystem/PlayerProgressData.cs`
- `Assets/Scripts/Level3/Level3Checkpointer.cs`

## Work and interaction mechanics

1. Expose revised one-record Bea batches and her three office stages through existing unlocked practice/drill/replay entries. Reuse exact tools/feedback with local attempts and clear mode/exit; no in-person practice board or duplicate binder. Company drills remain deferred with T11/T12.
2. Practice is lesson-only; mastery replay updates eligible best-only record through existing boundary, not cumulative XP/coins/Trust. No live snapshot/checkpoint/source acquisition, real voucher consumption, purchases or history mutation in local modes.
3. Keep assessment version and normalized correctness/tier when comparing v1 twelve-decision versus v2 nine-decision mastery; do not compare raw correct counts or sum both XP. Preserve first story grade/claims separately from best replay.
4. DirectEditor preview, if compatible prior campaign inventory exists, uses independent copy only. Unsupported preview starts local empty rather than fabricating possessions. Back/settings/cancel restore mode/UI/input cleanly.

## Assets, motion and feedback

Implement this slice with actual existing 3D bodies, readable paper faces, reachable targets and current page/camera/audio ownership. One active task/primary action; temporary source/reference only on demand. Reuse L1/L2 styling and quiet lift/settle/feed/stamp cues. No tiny auto-shrunk text, full-tool overlay or correctness flash on draft placement. Bind models/colliders/callbacks here; T17 cannot substitute for this ticket’s visual implementation. User verifies rendered/touch/feel results.

## Integration requirements

Preserve shared L1/L2 behavior, existing serialized identities/source scenes and all campaign possessions. Keep current exact-decimal L3 rules/provenance; no float prototype bridge. Use existing input/dialogue/FSM/guide/Bag/reward owners. Career3 stays gated until T18, L4/L5 remain unavailable. No remote ticket, package, scene recopy or global cleanup.

## Persistence and failure rules

Candidate -> signed persist -> publish before success effects. Validate mode/case/source/revision/prerequisites/action IDs; duplicates do not award. Preserve legacy grades/claims/approved snapshots/copies and quiet restores. Failure keeps prior state/source/copy retryable. Clear only owned pointer/selection/locks on Back/settings/disable/focus/camera return; independent work can be left. Practice/replay/Editor cannot mutate live story state.

## Internal checks — AI source/build/regressions only

- [x] Practice/replay/direct-editor route to intended revised activities and local attempt states.
- [x] Live checkpoint/wallet/Trust/inventory/quantities/history byte-equivalent after isolated practice/replay actions.
- [x] Best-only normalized v1/v2 comparison does not farmXP/rewards or overwrite frozen story results.
- [x] Exit/reload/cancel and local voucher/purchase restrictions preserve actual campaign and shared L1/L2 modes.
- [x] Build actual changed production paths and run targeted command/save/failure/mode checks plus affected shared regressions; record commands/counts/warnings, not historical passes.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] Existing binder/Career entry is understandable; drills use identical readable tools, not a separate crowded interface.
- [ ] Clear mode indication, no story reward confusion, safe return to menu/unfinished campaign.
- [ ] Confirm real 3D targets/animations/text/safe areas/Back/focus/audio on supported devices; no source declaration counts as visual acceptance.

## Handoff and completion definition

Deliver the goal above through its real entry point, including persistence, presentation and checked limitations. Update [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md) with actual evidence and remaining user checks; do not mark implemented for documentation presence. User owns Unity and every Git operation.

Direct dependents: T17. Continue the revised index, retaining prior working changes and historical results.
