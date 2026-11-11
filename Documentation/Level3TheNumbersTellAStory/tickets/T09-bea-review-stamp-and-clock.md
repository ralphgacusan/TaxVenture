# T09 — Reuse the existing stamps and reconcile Bea’s wired review and clock

**5 November user follow-up:** whole-case READY/NOT READY stamp on the front, shared foreground reservation and saved ink on reopen away from the desk; original Levels1/2 copy-drop presentation reused during the Auditor handover line. Collection is saved before flight, idempotent and quietly restored. Shared read-only copy mark/icon tag reused. See latest PROGRESS for checks and manual acceptance; T10's remaining Bea explanation/delivery is still Planned.

**5 November 2026 implementation:** Stamping already ran through the shared Level2DeskStamps/Level2StampDrag set bound by Level3CaseFolder (ICaseStamp: Bea's case, computation revision, free-position StampAt, save before ink); the L3 procedural READY/NOT READY bodies in Level3DeskCalculatorReview were no longer built and are now removed with their call sites (one stamp set, one input owner). The review now judges the fixed leaves correctly: with no Income/Deductions fields the collected summary is the input, so an early or wrong taxable tape is returned at Taxable income rather than an unfillable Gross income line. The Auditor says one line (approval, or one problem plus the next action, at most 24 words, no value; a justified hold starts "Good hold."); the longer reason moves to the existing Audit Summary under WHY with "Marked: <field>". The Computation leaf holding his mark gets a quiet red outline, a " · Auditor's mark" caption tag and his short line in its note until the next hand-in. Approval/100 coins/XP/frozen figures/copyAtAuditor/clock stop, refusals, retries/cap and the clock/voucher adapter are reused unchanged. See PROGRESS and KNOWN_ISSUES L3-R94–L3-R99.

Status: **Implemented, internally verified; user manual pending**. Blocked by: T08.

Priority: P1. Phase: C — Review. Estimated scope: M; one fresh agent context. If real call-site work exceeds this bound, record a bounded split before adding unrelated framework work.

## Goal and visible accomplishment

Use the familiar physical stamps on the actual Case Folder and hand it to the Auditor; approved current work saves a result and copyAtAuditor, returned work remains correctable.

Revision: **3 November 2026**. Read [REVISION_PLAN](../REVISION_PLAN.md), [SPEC](../SPEC.md), [reuse map](../REUSE_AND_ARCHITECTURE.md), [visual contract](../WORLD_AND_FEEDBACK.md), [verification](../VERIFICATION.md) and [AGENTS](../AGENTS.md). Planned refers to changed revision acceptance, not absence of reusable implementation. Historical baseline: This work is source-present/wired despite old Planned label. Preserve and verify actual root-cause/approval/coins/copy/clock implementation; revise physical integration rather than rebuilding T09.

## Existing systems to reuse

Existing source paths relative to repository root; inspect live callbacks/content before changing them. Narrow Level_03 adapters only; no duplicate folder/stamp/calculator/input/save/shop framework.

- `Assets/Scripts/Level3/Level3ReviewRules.cs`
- `Assets/Scripts/Level3/Level3Audit.cs`
- `Assets/Scripts/Level3/Level3ReviewClock.cs`
- `Assets/Scripts/Level3/Level3DeskCalculatorReview.cs`
- `Assets/Scripts/UI/Stamp3DDrag.cs`
- `Assets/Scripts/UI/StampCaseFolderUI.cs`
- `Assets/Scripts/Level2/Level2DeskStamps.cs`
- `Assets/Scripts/Interactables/AuditorInteractable.cs`

## Work and interaction mechanics

1. Use T02 verification baseline. Inspect actual shared stamp stand/models/lift/ghost/ink/return path and adapt L3 case/revision callbacks. Current L3 BuildStamp creates duplicate procedural bodies and hides originals; replace that admission only after reused shared presentation works. No new stamp mesh/set/framework.
2. Stamp covers current folder computation revision. Same Ready/Not Ready meaning for computation review only. Current typed/source/rule/credit/balance/root facts govern approval; supported hold stays open, stale/premature/false hold returns focused note. Prevent legacy prepare-return/archive/completion transitions.
3. Retain existing Auditor talk-first requested hotbar Case Folder handover and T09 candidate-save-publish review. One earliest root, existing retries/cap, no Trust/ethics loss. Unchanged hand-in does not charge again; failed save keeps folder/source and retries.
4. Retain once-only100-coin approval, frozen figures/XP, stopped clock and atAuditor copy; no delivery reward/early chapter completion. Shared AuditSummary/result surface only, no new dashboard.
5. Keep clock/voucher adapter: Bea16 active minutes starts with independent outdoor activity after mandatory lesson; preserve legacy started clock, existing pause policy, warnings/free next morning/no Trust penalty/+180 one voucher for Bea. The separate10-minute company allowance remains deferred with T11/T12.

## Assets, motion and feedback

Read [OFFICE_CASE_PLAY](../OFFICE_CASE_PLAY.md), approved 4 November, for the concise physical office flow. One active stage/task, details on demand, existing folder left/calculator right/short tape between. Put unused tools down before shared-stamp focus; use actual free-position ghost/press/ink/return. Ready is player judgment, not automatic correctness. Three short result summaries, working on demand. Auditor return shows one earliest issue plus next action (target≤24 words), with longer explanation behind Help; do not dump validation/provenance codes. Reopen the affected stage, preserve earlier work, mark only true dependents Recalculate. One existing concise approval surface, not stacked reward/modals; keep current clock/retry/XP rules and honest-error Trust protection. Verify current shared-stamp integration before relying on the older baseline’s duplicate-model description.

Implement this slice with actual existing 3D bodies, readable paper faces, reachable targets and current page/camera/audio ownership. One active task/primary action; temporary source/reference only on demand. Reuse L1/L2 styling and quiet lift/settle/feed/stamp cues. No tiny auto-shrunk text, full-tool overlay or correctness flash on draft placement. Bind models/colliders/callbacks here; T17 cannot substitute for this ticket’s visual implementation. User verifies rendered/touch/feel results.

## Integration requirements

Preserve shared L1/L2 behavior, existing serialized identities/source scenes and all campaign possessions. Keep current exact-decimal L3 rules/provenance; no float prototype bridge. Use existing input/dialogue/FSM/guide/Bag/reward owners. Career3 stays gated until T18, L4/L5 remain unavailable. No remote ticket, package, scene recopy or global cleanup.

## Persistence and failure rules

Candidate -> signed persist -> publish before success effects. Validate mode/case/source/revision/prerequisites/action IDs; duplicates do not award. Preserve legacy grades/claims/approved snapshots/copies and quiet restores. Failure keeps prior state/source/copy retryable. Clear only owned pointer/selection/locks on Back/settings/disable/focus/camera return; independent work can be left. Practice/replay/Editor cannot mutate live story state.

## Internal checks — AI source/build/regressions only

- [x] Active runtime strings/prompts and fallback errors follow OFFICE_CASE_PLAY: short task/action copy, optional detail, no debug/provenance dump or answer-key reveal; actual bindings inspected.


- [x] Shared original stamp bodies and folder target perform one persisted revision stamp; duplicate L3 models/input owners retired safely.
- [x] Approval, justified hold, earliest-root returns, stale/false/premature stamp and unchanged hand-in behave without legacy filing.
- [x] Approval100 coins/XP/frozen snapshot/copyAtAuditor occurs once, survives reload and failed-write retry, no delivery Trust.
- [x] Clock start/pause/cutoff/rollover/voucher preserve work/claims and legacy timing; actual current T09 regression evidence recorded.
- [x] Build actual changed production paths and run targeted command/save/failure/mode checks plus affected shared regressions; record commands/counts/warnings, not historical passes.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] Apply OFFICE_CASE_PLAY on a supported phone: one task/primary action, no overlapping instructions, readable values/details, short satisfying physical feedback, sound-off clarity and safe Back/focus behavior.


- [ ] Same stamp feel/body/ghost/ink/return as earlier levels, no collision/duplicate model/new controls.
- [ ] Hand in/receive returned folder, correct only marked step, recover next morning; notes/clock do not crowd computation.
- [ ] Confirm real 3D targets/animations/text/safe areas/Back/focus/audio on supported devices; no source declaration counts as visual acceptance.

## Handoff and completion definition

Deliver the goal above through its real entry point, including persistence, presentation and checked limitations. Update [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md) with actual evidence and remaining user checks; do not mark implemented for documentation presence. User owns Unity and every Git operation.

Direct dependents: T10. Deferred future dependent: T12. Continue the revised index, retaining prior working changes and historical results.
