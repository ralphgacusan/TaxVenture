# T02 — Continue the existing chapter with safe revised entry and legacy resume

Status: **Implemented, internally verified; user manual pending**. Blocked by: T01.

Execution readiness: implemented 3 November 2026 against retained T01 (evidence below and in [PROGRESS](../PROGRESS.md)). User Unity/device acceptance is open; next ticket T03.

Priority: P1. Phase: A — Continuation. Estimated scope: M; one fresh agent context. If real call-site work exceeds this bound, record a bounded split before adding unrelated framework work.

## Goal and visible accomplishment

A fresh revised attempt receives Bea’s assignment and goes to her records; a legacy attempt resumes its real work, including approved Bea/copyAtAuditor, without resetting or paying again.

Revision: **3 November 2026**. Read [REVISION_PLAN](../REVISION_PLAN.md), [SPEC](../SPEC.md), [reuse map](../REUSE_AND_ARCHITECTURE.md), [visual contract](../WORLD_AND_FEEDBACK.md), [verification](../VERIFICATION.md) and [AGENTS](../AGENTS.md). Planned refers to changed revision acceptance, not absence of reusable implementation. Historical baseline: T02 entry/save and later inventory continuation were historically internal-verified; T09 Audit/ReviewClock/reward code now exists beyond the old tracker cutoff. Preserve it and establish its actual current verification state.

## Existing systems to reuse

Existing source paths relative to repository root; inspect live callbacks/content before changing them. Narrow Level_03 adapters only; no duplicate folder/stamp/calculator/input/save/shop framework.

- `Assets/Scripts/Level3/Level3OfficeStart.cs`
- `Assets/Scripts/Level3/Level3ChapterRules.cs`
- `Assets/Scripts/Level3/Level3Checkpointer.cs`
- `Assets/Scripts/RewardsSystem/PlayerProgressData.cs`
- `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs`
- `Assets/Scripts/Level3/Level3ReviewRules.cs`
- `Tests/Level3Regression/Program.cs`
- `Tests/VerifyLevel3Scene.py`

## Work and interaction mechanics

1. Inspect actual T01–T09 source/resources, local scene edits, mode guards and tests. Record what runs versus only exists. Locate/restore the minimal Level3Regression project using existing suite conventions if absent; compile production before tests. No speculative refactoring or scene recopy.
2. Change only L3 fresh admission to skip mandatory ToolPractice/budget tapes; preserve serialized enum identities. Start Bea independent review hours after the first required activity lesson, retaining any existing started clock. Keep Career3 Coming Soon.
3. Implement the narrow versioned/idempotent snapshot adapter in REVISION_PLAN: map old destinations by row/source; retain frozen v1 grades/XP, acquisitions, tapes, claims, stage inputs, approvals/copy/clock and all inventory. Add only required presentation/assessment fields; unknown future version retains data and blocks safely.
4. Build disposable old-state fixtures at every documented boundary. Migrate candidate -> signed persist -> quiet publish; failure changes nothing. Legacy ToolPractice advances only its retired gate, never grants practice teaching/mastery/rewards. Restore approved Bea directly into delivery.
5. Reconcile the static scene check with the authorized current scene: replace only obsolete source-copy equality after recording present references/baseline. Preserve missing-script/duplicate-owner/prefab limitations as explicit findings; do not mask them or repair unrelated scenes.

## Assets, motion and feedback

Implement this slice with actual existing 3D bodies, readable paper faces, reachable targets and current page/camera/audio ownership. One active task/primary action; temporary source/reference only on demand. Reuse L1/L2 styling and quiet lift/settle/feed/stamp cues. No tiny auto-shrunk text, full-tool overlay or correctness flash on draft placement. Bind models/colliders/callbacks here; T17 cannot substitute for this ticket’s visual implementation. User verifies rendered/touch/feel results.

## Integration requirements

Preserve shared L1/L2 behavior, existing serialized identities/source scenes and all campaign possessions. Keep current exact-decimal L3 rules/provenance; no float prototype bridge. Use existing input/dialogue/FSM/guide/Bag/reward owners. Career3 stays gated until T18, L4/L5 remain unavailable. No remote ticket, package, scene recopy or global cleanup.

## Persistence and failure rules

Candidate -> signed persist -> publish before success effects. Validate mode/case/source/revision/prerequisites/action IDs; duplicates do not award. Preserve legacy grades/claims/approved snapshots/copies and quiet restores. Failure keeps prior state/source/copy retryable. Clear only owned pointer/selection/locks on Back/settings/disable/focus/camera return; independent work can be left. Practice/replay/Editor cannot mutate live story state.

## Internal checks — AI source/build/regressions only

- [x] Fresh assignment reaches BeaRecords without two budget tapes; actual T09 code/harness/build results are recorded.
- [x] Legacy draft, frozen grade, collected worksheet, partial computation and approved copy round-trip with possessions/claims unchanged.
- [x] Migration repeated/failed/unknown-version cases are quiet, failure-safe and never award or erase state.
- [x] Current scoped scene/build/Career checks distinguish obsolete creation assertions from real unresolved references.
- [x] Build actual changed production paths and run targeted command/save/failure/mode checks plus affected shared regressions; record commands/counts/warnings, not historical passes.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] Use disposable fresh and legacy fixtures; confirm next task and retained papers/results, no inherited quests or extra reward.
- [ ] Confirm first-person reception/desk access and no requirement to perform removed budget practice.
- [ ] Confirm real 3D targets/animations/text/safe areas/Back/focus/audio on supported devices; no source declaration counts as visual acceptance.

## Handoff and completion definition

Deliver the goal above through its real entry point, including persistence, presentation and checked limitations. Update [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md) with actual evidence and remaining user checks; do not mark implemented for documentation presence. User owns Unity and every Git operation.

### Evidence — 3 November 2026 (internal only)

- Fresh: `AssignBea` is Reception -> BeaRecords (same action id); no tapes, practice completion or teaching marker. Receptionist line, objectives and Arthur's `bea-assignment` hint send the player to Bea; the four practice lessons are retired. Bea's clock starts once her first worksheet row is placed (`Level3ChapterRules.BeaWorkIndependent`); a started clock is never restarted.
- Continue: snapshot version 2. `Level3ChapterRules.TryContinueLegacy` copies a usable version 1 snapshot exactly (ToolPractice -> BeaRecords under `Level_03/practice-retired`; frozen grade labelled `L3-worksheet-v1`; `calculatorTaught` from tapes actually attached). `Level3Checkpointer` writes it, then restores quietly; a failed write keeps the old snapshot, stays not ready and retries. Unusable/newer snapshots stay Blocked and untouched.
- Commands and counts are in PROGRESS (Level3Regression 1064/1064 including 140 revision checks; five mutation probes caught). Pre-existing suite gaps and T02 limits are in KNOWN_ISSUES L3-R14–L3-R22.
- Still open: user Play Mode/device checks above; a disposable legacy-save route inside Unity (L3-19).

Direct dependents: T03, T04. Continue the revised index, retaining prior working changes and historical results.
