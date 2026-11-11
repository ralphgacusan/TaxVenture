# T17 — Polish the integrated physical chapter and retire superseded L3 views

Status: **In progress**. Blocked by: T13, T14, T15, T16. (6 November 2026: T14–T16 internal-verified; T13's L3-R106 base-capacity decision still open; late-state fixtures and all device/timing acceptance outstanding.)

**6 November 2026 integration pass (partial):** Walked the Bea-only graph in source. Added 33 behavioral/wiring checks: every saved phase from a fresh chapter to the completed office ticks exactly a prefix of the 14 Notes steps (one active step; none ahead of an earlier one), a completed office restores with every step ticked and no guide lesson replayed, every guide `flag:` is a real Level3Checkpointer fact, no case/source/row identifiers or unfilled tokens appear in guide pages, Notes steps or closing lines, each closing seam has one owner (Bea's copy delivery, the receptionist message, the chapter card and its acknowledgment), and the legacy single-case completion route is refused in every phase. Traced the retired views: the old worksheet board (Level3WorksheetDesk/…DeskReview) is never constructed (no AddComponent, not in the scene); its two guide flags are null-safe and its tape lesson can no longer show; the source stays as legacy evidence for the regression pins (removal is a bounded follow-up, KNOWN_ISSUES L3-R131). The practice card model is live (calculator first-use example). No production change was needed by these checks. Not done: Editor fixtures for approved/delivered/closing/computer states (playtest uses normal play or an existing save), L3-R106, and every Unity/device/readability/timing check (playtest script in PROGRESS).

Priority: P1. Phase: F — Integration polish. Estimated scope: M; one fresh agent context. If real call-site work exceeds this bound, record a bounded split before adding unrelated framework work.

## Goal and visible accomplishment

The Bea outside/office/review/delivery/closing loop feels continuous, readable and restrained, with no surviving mandatory old board/clipboard/budget/service-task path. The deferred company case is outside this integration pass.

Revision: **3 November 2026**. Read [REVISION_PLAN](../REVISION_PLAN.md), [SPEC](../SPEC.md), [reuse map](../REUSE_AND_ARCHITECTURE.md), [visual contract](../WORLD_AND_FEEDBACK.md), [verification](../VERIFICATION.md) and [AGENTS](../AGENTS.md). Planned refers to changed revision acceptance, not absence of reusable implementation. Historical baseline: Per-ticket presentation is required earlier. T17 tunes and integrates a working chapter; it does not defer all visual work or rebuild existing props.

## Existing systems to reuse

Existing source paths relative to repository root; inspect live callbacks/content before changing them. Narrow Level_03 adapters only; no duplicate folder/stamp/calculator/input/save/shop framework.

- `Assets/Scripts/Level3/Level3WorksheetModel.cs`
- `Assets/Scripts/Level3/Level3ComputationLeafModel.cs`
- `Assets/Scripts/Level3/Level3DeskCalculatorReview.cs`
- `Assets/Scripts/UI/HUD/CaseFolder3DView.cs`
- `Assets/Scripts/UI/Books/PageTurnStage.cs`
- `Assets/Scripts/UI/HUD/HudWindowLock.cs`
- `Assets/Resources/Levels/Level_03/objectives.json`
- `Assets/Resources/Tutorials/Level_03/guide.json`

## Work and interaction mechanics

Follow [OFFICE_CASE_PLAY](../OFFICE_CASE_PLAY.md) for whole-flow copy/pacing acceptance. T07–T10 must already implement it: T17 observes/refines Bea’s active A/B/C stages, source/rule/certificate, return-note and delivery views on phone. Record missed taps, repeated reading, obscured values, duplicated guide/objective/dialogue and stalled animation; verify the user can identify the current action and explain tax due versus remaining payable. Keep complete details on demand and shorten/page before shrinking. Do not add Nina/Marine play or make it a release prerequisite.


1. Walk the current source/objective/guide/binding graph for all old and revised views. Remove or disable proven-unused L3 four-zone/notes/subtotal dock/computation leaf/duplicate stamp builder paths only after replacements and legacy restores pass. Keep historical decoders/shared helpers/source records; no speculative shared cleanup.
2. Tune one-record/one-stage composition, actual type/number size/safe areas/target reach, on-demand originals/rate/working, contextual copy budgets and stable camera. Rewrite/page text before shrinking; no simultaneous tool/panel pile-up.
3. Reuse paper lift/settle, result feed/clip, page turns, stamp and NPC/emote cues. One quiet cue per meaningful action and one final side-quest card; avoid per-tap correctness/popups/confetti/shakes. Cosmetic skip cannot skip saved commands.
4. Provide disposable seed fixtures/user scripts for fresh/base/upgraded, legacy draft/frozen/collected/approved, wrong work, cutoff, copy delivery and completed office. User performs Unity/device/readability/audio/path/actual performance checks; AI owns ready scene targets, not user assembly.
5. Measure beginner segment/full play against relaxed15–20-minute target. Record help/repeated reading/missed taps/occlusion/actual performance; tune friction without removing learning or adding timer pressure. Fix observed issues within scope; preserve inherited warnings honestly.

## Assets, motion and feedback

Implement this slice with actual existing 3D bodies, readable paper faces, reachable targets and current page/camera/audio ownership. One active task/primary action; temporary source/reference only on demand. Reuse L1/L2 styling and quiet lift/settle/feed/stamp cues. No tiny auto-shrunk text, full-tool overlay or correctness flash on draft placement. Bind models/colliders/callbacks here; T17 cannot substitute for this ticket’s visual implementation. User verifies rendered/touch/feel results.

## Integration requirements

Preserve shared L1/L2 behavior, existing serialized identities/source scenes and all campaign possessions. Keep current exact-decimal L3 rules/provenance; no float prototype bridge. Use existing input/dialogue/FSM/guide/Bag/reward owners. Career3 stays gated until T18, L4/L5 remain unavailable. No remote ticket, package, scene recopy or global cleanup.

## Persistence and failure rules

Candidate -> signed persist -> publish before success effects. Validate mode/case/source/revision/prerequisites/action IDs; duplicates do not award. Preserve legacy grades/claims/approved snapshots/copies and quiet restores. Failure keeps prior state/source/copy retryable. Clear only owned pointer/selection/locks on Back/settings/disable/focus/camera return; independent work can be left. Practice/replay/Editor cannot mutate live story state.

## Internal checks — AI source/build/regressions only

- [ ] Bea-only production graph has one active owner/surface per tool; old required gates/copy/sidequest paths removed, legacy data readable. Deferred company paths remain inactive.
- [ ] Objectives/guide/dialogue/camera/audio correspond to committed revised phases, no source-answer leaks/repeated tutorials. (6 Nov: objectives/guide/text checked internally; dialogue/camera/audio need Unity.)
- [ ] All interrupts/save/reload/modes/fixtures pass current targeted integration, shared L1/L2 behavior preserved.
- [ ] Manual scripts/artifact evidence and actual measured vs pending results are recorded without static-render claims.
- [ ] Build actual changed production paths and run targeted command/save/failure/mode checks plus affected shared regressions; record commands/counts/warnings, not historical passes.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] Play complete chapter on supported phone sizes/orientations; text/targets/collisions/NPC route/input recovery comfortable.
- [ ] Assess outside/office rhythm, restrained animation/sound/reward density and timing; no oversized worksheet or tiny multi-panel layout remains.
- [ ] Confirm real 3D targets/animations/text/safe areas/Back/focus/audio on supported devices; no source declaration counts as visual acceptance.

## Handoff and completion definition

Deliver the goal above through its real entry point, including persistence, presentation and checked limitations. Update [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md) with actual evidence and remaining user checks; do not mark implemented for documentation presence. User owns Unity and every Git operation.

Direct dependents: T18. Continue the revised index, retaining prior working changes and historical results.
