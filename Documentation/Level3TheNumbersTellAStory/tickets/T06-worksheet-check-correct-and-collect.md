# T06 — Check machine batches and collect Bea’s usable summary card

**5 November user-approved follow-up:** Implemented/internal checks pass; user manual pending. The actual summary takes the stand after final acceptance, rail returns then, shared ghost drags open it or dock/store it, and Bag/hotbar storage acquires its separate `bea_summary` item together with the existing `B-WORKSHEET` chapter output and rewards. Remove Collect panel/ledge proxy/physical folder duplicate. Preserve exact office totals. See latest PROGRESS for changed paths, evidence and manual checks; this supersedes the older presentation in steps 5–7 below.

Status: **Implemented, internally verified; user manual pending**. Blocked by: T05. Built 5 November 2026: see PROGRESS (T06 entry) and KNOWN_ISSUES L3-R71–L3-R80.

Priority: P1. Phase: B — Side-quest completion. Estimated scope: M; one fresh agent context. If real call-site work exceeds this bound, record a bounded split before adding unrelated framework work.

## Goal and visible accomplishment

Complete deliberate batch review/correction and one collectible Bea’s Income & Expense Summary. The output is existing B-WORKSHEET, available in Bag and Case Folder support for office transfers. Read [SIDE_QUEST_MACHINE](../SIDE_QUEST_MACHINE.md), especially grading, atomic output and office bridge; do not restore a large literal worksheet.

Revision: **3 November 2026 — approved paper-machine interaction**. Status remains Planned; source presence is reuse evidence, not acceptance. Retain source/save identities and work serially from the current continuation.

## Existing systems to reuse

Existing source paths relative to repository root; inspect live callbacks/content before changing them. Narrow Level_03 adapters only; no duplicate folder/stamp/calculator/input/save/shop framework.

- `Assets/Scripts/Level3/Level3WorksheetCheck.cs`
- `Assets/Scripts/Level3/Level3WorksheetDeskReview.cs`
- `Assets/Scripts/Level3/Level3Checkpointer.cs`
- `Assets/Scripts/RewardsSystem/CampaignRewards.cs`
- `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs`
- `Assets/Resources/Levels/Level_03/rewards.json`

## Work and interaction mechanics

1. After the current batch is filed, Check batch becomes primary. The lever files, never grades. Incomplete checks identify missing choices; failed saves freeze nothing and reveal no committed marks.
2. Use L3-worksheet-v2 registry: batches have 2/3/4 scored decisions, seven treatments plus R6/R7 reasons. First complete check freezes that batch once, before visible marks. Corrections never improve first outcomes; future batches cannot be checked early.
3. Show a compact localized review strip with text/symbol feedback and brief contextual help. Recall mistakes into the same half-feed/select/reason/pull loop; keep prior choices until replacement saves. Refile is neutral; deliberate recheck establishes correction. All current-batch choices must be correct before accepting it and admitting next. Earlier accepted batches remain inspectable/read-only.
4. Keep hints free, pre-check edits unpenalized and honest mistakes free of Trust/coin penalties. One final Gold9/Silver7–8/Bronze0–6 result; XP60-wrong (51–60). No per-row/batch medal popup or new speed/lever/amount score.
5. When all batches are corrected, feed one small folded preview card onto the output ledge. Front: Bea’s Income & Expense Summary, Income ₱900,000, Supported deductions ₱300,000. Deliberate open shows short entries, original links and personal/duplicate exclusions. It contains no office tax/base/credit/balance solution. Preview is not inventory ownership or completion.
6. Collect via existing CollectWorksheet/CommitLevelThreeWorksheet candidate transaction: B-WORKSHEET + frozen grade/badge/mastery +60 coins/+2 Trust once. Persist before acquisition effects/reward card/Bea route transition. Failed write leaves retryable output, no grant; duplicate callback/reload never prints or awards again. Base capacity must allow required work through current policy.
7. Reuse the existing item/source identity, adding the presentation title rather than a second summary item/ledger. Verify Bag readability, Case Folder Supporting Documents link, exact source-backed income/deduction selections in office T07, no float bridge or answer-key copy. Collected state opens the office continuation, not chapter completion.
8. Preserve frozen v1 tier/XP/claims while correcting mapped rows; never start fresh v2 grading for earned legacy results. Already collected snapshots skip machine replay; drafts resume quietly and future saves remain blocked/retained. Test practice/replay isolation and all interrupted output/save paths.

## Assets, motion and feedback

Follow [machine visual/motion contract](../SIDE_QUEST_MACHINE.md) and [world contract](../WORLD_AND_FEEDBACK.md). Same existing low-poly scene, paper/font/material vocabulary and restrained sound/settings; no oversized navy/brass board, new art theme, full-screen flat substitute or auto-shrunk labels. Real anchored 3D bodies, reachable targets and correct slot occlusion are required in this slice. Implementer binds everything; user checks actual Unity/phone rendering. The linked storyboard is a mechanical reference, not final visual acceptance.

## Integration requirements

Preserve shared L1/L2 behavior, existing serialized identities/source scenes and all campaign possessions. Keep current exact-decimal L3 rules/provenance; no float prototype bridge. Use existing input/dialogue/FSM/guide/Bag/reward owners. Career3 stays gated until T18, L4/L5 remain unavailable. No remote ticket, package, scene recopy or global cleanup.

## Persistence and failure rules

Candidate -> signed persist -> publish before success effects. Validate mode/case/source/revision/prerequisites/action IDs; duplicates do not award. Preserve legacy grades/claims/approved snapshots/copies and quiet restores. Failure keeps prior state/source/copy retryable. Clear only owned pointer/selection/locks on Back/settings/disable/focus/camera return; independent work can be left. Practice/replay/Editor cannot mutate live story state.

## Internal checks — AI source/build/regressions only

- [x] Incomplete/failed batch checks freeze no score; first outcomes remain across corrections and accepted-batch locks.
- [x] Preview grants nothing; failed/double collection grants exactly once; Bag/folder expose the same B-WORKSHEET and office exact totals.
- [x] Nine decisions cover exactly three batches; incomplete/failed check freezes none; first results freeze once per batch.
- [x] Corrections/accepted batch revisit cannot upgrade first outcomes; all-correct gate works independently of medal.
- [x] Compact worksheet totals derive from corrected selected rows; collected source identity B-WORKSHEET and rewards persist once.
- [x] Legacy v1 frozen/collected states keep tier/XP/claims, no new award/regrade or mandatory subtotal tape.
- [x] Build actual changed production paths and run targeted command/save/failure/mode checks plus affected shared regressions; record commands/counts/warnings, not historical passes. (Rules, signed-store mirror, wiring, geometry and font checks only; no Unity run.)

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] Check wrong/incomplete/correct batches; concise feedback and recall correction remain compact, with one final medal only.
- [ ] Collect small folded summary; reopen through Bag/folder and use its totals at office without giant worksheet or duplicate item.
- [ ] Interrupt summary output/collection, leave/return and verify clean restoration, clear next task and Bea office transition only after collection.

## Handoff and completion definition

Deliver the goal above through its real entry point, including persistence, presentation and checked limitations. Update [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md) with actual evidence and remaining user checks; do not mark implemented for documentation presence. User owns Unity and every Git operation.

Direct dependents: T07, T13, T16. Continue the revised index, retaining prior working changes and historical results.
