# T14 — Close the supported finding and aggregate the completed chapter

Status: **Implemented, internally verified; user manual pending**. Blocked by: T10, T13. (6 November 2026; Unity/device acceptance is the user's. T13's open base-capacity decision L3-R106 is not a T14 dependency.)

**6 November 2026 implementation:** After Bea's explained, actual delivery (T10, ClosingFinding) her next talk asks one question about her own draft (DRAFT-B26, issued by her): it listed ₱354,000, her originals support ₱300,000. Three statements dock beside the dialogue in her existing choice card with Review her records and Talk later. Only "Party ₱24,000 was private; printing ₱30,000 listed twice." is saved (`Level3ClosingRules.RecordFinding` via `Level3Checkpointer.CommitFinding`: candidate → signed save → publish; the existing FindingRecorded event ticks Notes); intent ("on purpose") or a wider pattern gets one reply ("shows what was wrong, not why" / "only covers my own draft") and the same question waits, nothing saved, no penalty. Review her records ends the talk and opens her papers in the existing Case Folder reader; the question waits. The existing receptionist then passes on Mayor Quax's short message through one opt-in `ReceptionistInteractable.ILevelMessage` seam (Arthur stays the guide; one player acknowledgment, no choice, no Trust). Hearing it to the end saves ClosingFinding → WrapUpPending; cancelling saves nothing. The existing LevelCompleteUI/HudRewardScreen card then shows one Chapter 3 result once no dialogue runs (again after a reload): frozen worksheet first check (v1 or v2 as graded) + Bea's case XP of 160, the worth_the_tax_3 tier, the chapter's coins/Trust from the Level_03 ledger, the finding fact. TO CAREER or STAY IN OFFICE (an opt-in second card button) is the explicit acknowledgment: `CommitAcknowledgment` → `PlayerProgressManager.CommitLevelThreeWrapUp` writes CompletedOffice, the `Level_03/chapter-wrap-up` activity, Level_03 completion and one aggregated mastery record (best kept) in one signed write, no coins or Trust; a failed write keeps the card with TRY AGAIN. The completed office restarts no clock, copy, hand-in, case edit or reward. Details, commands and the manual script: latest PROGRESS; limitations KNOWN_ISSUES L3-R109–L3-R117.

Priority: P1. Phase: E — Narrative closure. Estimated scope: M; one fresh agent context. If real call-site work exceeds this bound, record a bounded split before adding unrelated framework work.

## Goal and visible accomplishment

After Bea’s actual delivery, record the limited finding supported by her file, receive Quax’s message via the receptionist, acknowledge the Bea-only chapter result, and return to Career or the optional completed office. Make no cross-client or systemic claim that would require the deferred company case.

Revision: **3 November 2026**. Read [REVISION_PLAN](../REVISION_PLAN.md), [SPEC](../SPEC.md), [reuse map](../REUSE_AND_ARCHITECTURE.md), [visual contract](../WORLD_AND_FEEDBACK.md), [verification](../VERIFICATION.md) and [AGENTS](../AGENTS.md). Planned refers to changed revision acceptance, not absence of reusable implementation. Historical baseline: Closing phases/aggregate contracts are planned; reuse existing campaign result/mastery/Notes/message surfaces instead of creating an evidence board or alternate completion manager.

## Existing systems to reuse

Existing source paths relative to repository root; inspect live callbacks/content before changing them. Narrow Level_03 adapters only; no duplicate folder/stamp/calculator/input/save/shop framework.

- `Assets/Scripts/Level3/Level3ChapterRules.cs`
- `Assets/Scripts/Level3/Level3Checkpointer.cs`
- `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs`
- `Assets/Scripts/RewardsSystem/LevelScoreLedger.cs`
- `Assets/Scripts/UI/LevelCompleteUI.cs`
- `Assets/Scripts/UI/HUD/OfficeObjectives.cs`

## Work and interaction mechanics

1. Use Bea’s retained B-DRAFT/preparer reference and original support to record the specific unsupported costs shown in her file. Distinguish discrepancy/correction from proof of intent/author authorization or a wider pattern. Review records action returns safely; no new L5 evidence board.
2. Receptionist delivers short mayor message; Arthur remains guide, no replacement actor. Save message/finding/wrap-up facts quietly and preserve Rosa history. No extra ethics branch/trust reward for choosing a polite line.
3. Aggregate the frozen side-quest assessment and Bea’s case result through existing mastery/result owners once. Support v1/v2 worksheet grades; current maximum is160 story coins,160 mastery XP, and+6 Public Trust, with one badge. Never reissue already-claimed coins/XP/Trust or overwrite wallet with fixture totals. The deferred company case may extend these totals later, but is not included now.
4. Completion requires Bea’s explained delivery, the supported finding/message and explicit Continue acknowledgment. Failed save keeps the result pending. Choose Career or completed-office supply counter; the latter has no active review/copy/reward loop. L4/L5 remain unavailable.

## Assets, motion and feedback

Implement this slice with actual existing 3D bodies, readable paper faces, reachable targets and current page/camera/audio ownership. One active task/primary action; temporary source/reference only on demand. Reuse L1/L2 styling and quiet lift/settle/feed/stamp cues. No tiny auto-shrunk text, full-tool overlay or correctness flash on draft placement. Bind models/colliders/callbacks here; T17 cannot substitute for this ticket’s visual implementation. User verifies rendered/touch/feel results.

## Integration requirements

Preserve shared L1/L2 behavior, existing serialized identities/source scenes and all campaign possessions. Keep current exact-decimal L3 rules/provenance; no float prototype bridge. Use existing input/dialogue/FSM/guide/Bag/reward owners. Career3 stays gated until T18 (superseded 5 November: user-authorized Career3 entry after completed Level 2 for the APK playtest, preserved), L4/L5 remain unavailable. No remote ticket, package, scene recopy or global cleanup.

## Persistence and failure rules

Candidate -> signed persist -> publish before success effects. Validate mode/case/source/revision/prerequisites/action IDs; duplicates do not award. Preserve legacy grades/claims/approved snapshots/copies and quiet restores. Failure keeps prior state/source/copy retryable. Clear only owned pointer/selection/locks on Back/settings/disable/focus/camera return; independent work can be left. Practice/replay/Editor cannot mutate live story state.

## Internal checks — AI source/build/regressions only

- [x] Missing Bea delivery/message/finding blocks completion; review-records and cancel return to pending wrap-up.
- [x] Legacy/v2 worksheet plus Bea case XP aggregate once without early Level3 completion/duplicate rewards.
- [x] Failed acknowledgment/reload preserves pending card and current state; repeat Continue cannot pay.
- [x] Bea-supported qualified fact/history and result retained; deferred company state is not required; optional office has no clock or reopened case.
- [x] Build actual changed production paths and run targeted command/save/failure/mode checks plus affected shared regressions; record commands/counts/warnings, not historical passes.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] Ending clearly connects records to emerging story without overstating intent or repeating lectures.
- [ ] One existing chapter result feels conclusive, offers clear Career/optional-office exit and readable named rewards.
- [ ] Confirm real 3D targets/animations/text/safe areas/Back/focus/audio on supported devices; no source declaration counts as visual acceptance.

## Handoff and completion definition

Deliver the goal above through its real entry point, including persistence, presentation and checked limitations. Update [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md) with actual evidence and remaining user checks; do not mark implemented for documentation presence. User owns Unity and every Git operation.

Direct dependents: T15, T16, T17. Continue the revised index, retaining prior working changes and historical results.
