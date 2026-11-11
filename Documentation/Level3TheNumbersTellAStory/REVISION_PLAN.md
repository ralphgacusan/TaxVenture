# Level 3 revision — approved direction, 3 November 2026

Office presentation update — **4 November 2026**: [OFFICE_CASE_PLAY](OFFICE_CASE_PLAY.md) governs the approved Bea office interaction, short screen copy, progressive detail and restrained print/clip/stamp/review/delivery feedback in T07–T10; T17 verifies the complete experience. Sources/financial fixtures remain intact, richer details stay accessible and no production work is claimed.

This is the authoritative design amendment for the existing Level 3 pack. It records the user's accepted interview decisions and their explicit request to revise the tickets. It supersedes conflicting gameplay/presentation requirements in the earlier proposal, T01–T09 implementation notes and retained historical fixtures. Financial/source identities and tax-authority gates remain unchanged. This document specifies future work; authoring it does not implement or accept gameplay.

## Problem and desired experience

The current seven-strip/four-zone board, separate claim forms, subtotal tapes, source selectors and computation leaf expose too many decisions and too much small print together. Preserve the office/plaza adventure and the physical vocabulary of Levels 1–2. Increase responsibility, not simultaneous controls. The target is a relaxed 15–20-minute first play, to be measured with beginners; compulsory instruction/dialogue is paused time and no speed/precision task gates learning.

Level 1 identifies taxpayer circumstances. Level 2 verifies income and reconciles records. The current Level 3 release determines Bea's supported individual inputs, applies her three-stage computation, reviews and delivers her copy, and closes the chapter. The second company case remains documented for future expansion but is deferred from the current active release. Level 4 owns return selection/encoding/support assembly and simulated filing. Level 5 owns the authorized integrated investigation and substantiated findings. Arthur remains the existing guide; Quax supplies the story message. No guide replacement.

## Current release scope decision — Bea only

The active Level 3 story and completion path contain one taxpayer case: Bea. T11/T12 and the existing Nina/Marine fixtures and design documents are retained as a deferred future case. They are excluded from the current implementation order, chapter completion, replay eligibility, equipment checks, computer register, content-authority gate, and Career release gate. Reopen and integrate that case only when the user requests it. Do not delete its source facts, ticket IDs, reward definitions, or legacy/future compatibility notes.

## Player journey and separation of activities

| Part | Player action | Durable output | Excluded responsibility |
|---|---|---|---|
| Reception | Receive Bea's appointment and one short next task | Assignment, Rosa history preserved | No standalone two-tape budget lesson |
| Outside: Worth the Tax? | Inspect one record, place its slip, check a small batch, correct | Supported worksheet, 60 coins/+2 Trust once | No calculator, bracket, tax, credit, stamp or audit |
| Office: Bea case | Read folder support, transfer selected totals, build three calculations | Source-linked computation | No repeated record-sorting puzzle |
| Review and delivery | Stamp, talk-first hand-in, correct, pick up copy, explain and hand over | Approval 100 coins; delivery +4 Trust | No separate service-plan errand |
| Company case — deferred | Retained T11/T12: compare one unsupported cost, then reuse calculation/review/delivery | Future separate case result and rewards | Not required for current chapter completion or release |
| Closing | Record the limited Bea-supported finding, read mayor message, acknowledge chapter result | Bea-only Career completion | No cross-client/systemic claim, accusation of intent, filing or final ending |
| Optional upgrade | Buy existing 250-coin computer in menu/counter and inspect verified cases | Owned installed computer/register | No purchase required to finish; L4 transfer/printing remains future |

## Outside activity: compact paper machine and collectible summary

The user approved [SIDE_QUEST_MACHINE](SIDE_QUEST_MACHINE.md) on 3 November after rejecting the oversized worksheet and repetition of Level1 direct drag-to-board sorting. That detailed contract and its updated [storyboard](images/side-quest-machine-storyboard.svg) govern T04–T06. This is design documentation only; T03 remains last implemented and T04 next.

Keep seven monetary entries and original sources. Three treatments remain Income, Deduct, Leave Out, expressed through a neutral-start selector on a compact physical machine rather than three drop targets. Player reads one card, drags it halfway into the intake (tap equivalent), chooses treatment and required R6/R7 reason, then pulls/taps the lever. After a successful atomic save, rollers draw it fully inside, a recall tab appears and the next card arrives. Loading/turning does not classify or grade. All valid filings have identical neutral feedback.

Batches remain earnings R1/R2, costs R3/R4/R5, questions R6/R7. Original/comparison and Why? appear temporarily. Recall lets the player revise before Check and correct mistakes after it; accepted earlier batches remain read-only. Only Check batch reveals correctness and freezes that batch's first outcomes; incomplete/failed checks freeze nothing. Correct before next batch. Automatic totals reflect actual selected entries, including wrong ones, without arithmetic scoring.

After all batches are corrected, the machine emits a small folded Bea’s Income & Expense Summary. Actual Collect grants existing B-WORKSHEET into Bag/Case Folder support with corrected exact income900,000/deductions300,000, source links and short exclusions, plus existing once-only rewards. Preview/output motion does not acquire/award/move Bea. Office T07 uses that same item’s totals; no second summary type or giant permanent paper. No outdoor calculator, subtotal tapes, claim columns, link-choice puzzle or new framework.

### Revised decision registry (assessment version L3-worksheet-v2)

| decisionId | batch | expected treatment | expected reason |
|---|---|---|---|
| dest-R1 | earnings | Income | - |
| dest-R2 | earnings | Income | - |
| dest-R3 | costs | Deduct | - |
| dest-R4 | costs | Deduct | - |
| dest-R5 | costs | Deduct | - |
| dest-R6 | questions | Leave Out | - |
| dest-R7 | questions | Leave Out | - |
| reason-R6 | questions | - | Personal purpose |
| reason-R7 | questions | - | Already represented |

Nine scored decisions: seven treatments plus two reasons. Totals and automatic source/reference links are not scored. Implementation defaults: Gold 9/9, Silver 7–8/9, Bronze 0–6/9; XP = 60 minus first-check wrong decisions (51–60). These thresholds are authored settings, not measured learning evidence. Save first outcomes per batch atomically; only actual corrected worksheet collection grants the badge, mastery claim, 60 coins and +2 Trust. A failed batch write freezes nothing. One final reward card. Preserve legacy 12-decision first grades under their original assessment version; never convert an earned Bronze to Gold or award again.

### Revised first-check score fixtures

| wrong | correct | tier | xp |
|---|---|---|---|
| 0 | 9 | Gold | 60 |
| 1 | 8 | Silver | 59 |
| 2 | 7 | Silver | 58 |
| 3 | 6 | Bronze | 57 |
| 4 | 5 | Bronze | 56 |
| 5 | 4 | Bronze | 55 |
| 6 | 3 | Bronze | 54 |
| 7 | 2 | Bronze | 53 |
| 8 | 1 | Bronze | 52 |
| 9 | 0 | Bronze | 51 |

## Office case: familiar folder, deliberate transfers, three stages

Reuse the actual existing Case Folder object/open/close/page/Bag path; do not create a second folder or a parallel clipboard case UI. Configure Level_03 pages/roles as Taxpayer Information, Supporting Documents, Computation using existing page navigation. Re-enable/adapt the L3-hidden folder safely without reviving the L1 filing chain. Keep client identity/context available without redoing classification. Folder support lists acquired worksheet/originals/certificate with stable identities; open one relevant paper, optional second only for deliberate comparison. Outside desk modes these records remain readable, not editable/transferable.

Value transfer is the existing select-source -> select-field interaction, with drag optional. Exact decimal values, case/source/row/year/revision travel together. Extend existing interfaces with a narrow L3 adapter: DataValue supports object payloads, but AsFloat/legacy CaseData callbacks are not valid L3 money authorities. Do not coerce through float or write into the old flat-rate calculation. Mechanical invalidity (foreign case, wrong data kind, unowned source) is refused; plausible wrong same-case money/choices remain drafts for deliberate review. Neutral transfer acknowledgment is not a correctness award. Other levels retain their behavior.

| Stage | Player responsibility | Visible result |
|---|---|---|
| A: Taxable income | Open worksheet, transfer income and deductions, select subtraction, Calculate, attach | 900,000 - 300,000 = 600,000 |
| B: Tax due | Choose annual band; inspect base + rate x excess formula populated from selected values; Calculate, attach | 22,500 + 20% x (600,000 - 400,000) = 62,500 |
| C: Remaining payable | Acquire/read certificate, select withheld tax and transfer credit, subtract, Calculate, attach | 62,500 - 12,500 = 50,000 |

These table values are author fixtures, not permanently shown answers. One active stage; earlier stages collapse to tappable summaries. Advancing depends on structural completion/current work, not silently checking a secret answer to reveal correctness. Later stages can hold unsupported calculations for Auditor review. The formula uses the chosen rule/inputs, not expected answers; wrong bands/inputs can yield wrong results. Explain that the marginal rate applies to excess only. The six-band reference is a temporary readable source, not a permanent extra panel. Full working reopens on demand. No required separate excess/marginal tape attachments; reuse existing chain evaluation and accept equivalent supported working. A printed result attaches directly; no duplicate manual re-entry.

Introduce the calculator at its first office use through one short unscored unrelated demonstration of source selection/subtract/calculate/attach, then return to Bea's editable work. No separate budget card, two compulsory practice tapes, field calculator or long narrated tutorial. Save the first-use teaching marker; skip on resume once taught. Actual client work remains independent. Reuse the existing decimal calculator/keypad for optional draft edits; typed numbers never establish sourced support.

Source/treatment changes invalidate only dependent results and stamp. Old results remain available with a quiet Recalculate tag. No forced restart of the whole computation. Freeze supported figures on approval. The same existing stamp bodies/stand/ghost/press/ink/return and Auditor talk-first Case Folder hand-in serve Level 3 through narrow case/revision rules. Remove/hide duplicate procedural stamp bodies only after shared presentation works. Readiness means computation review, not filed/paid.

## Deferred future case — company transfer challenge and client resolution

This design remains available for a later user-approved expansion through T11/T12. It is not in the current Level 3 release path and does not block Bea-only implementation, completion, or Career enablement.

Only Bea delivery admits Nina and company papers. Reuse the same folder/desk/fields with case-scoped state; keep Bea's approved result and copy status. Compare C-DRAFT's 2,120,000 proposed costs with C-COST/C-TRIP. The player identifies the personal trip and deliberately confirms the supported 2,000,000 expense total; no automatic correction on reading. Existing rows retain their evidence relationships. No new worksheet item or medal.

Reuse the three stages with supplied corporate context: 8,000,000 - 2,000,000 = 6,000,000; ordinary 25% -> 1,500,000; less 300,000 -> 1,200,000. Show the relevant rule conditions in the book; player selects the applicable rule rather than receiving an unexplained preset. No depreciation/MCIT/OSD/withholding-rate lesson or asset hunt. Fewer guide interruptions, same mechanics.

Client resolution uses one short connected sequence: pick up the read-only computation copy -> point to one relevant figure/source to explain -> hand it to the correct client. Bea's active T10 question is about credit/balance. Nina's higher-supported-balance question remains for deferred T12. Incorrect explanations receive brief help and retry, not Trust loss. Explanation and completed handover gate +4 once; cancel/resume retains the copy. Remove separate receipts-versus-trace service-plan tasks, not their saved history. No automatic delivery just because approval occurred.

## Review, campaign and equipment

Reuse current T09 review/root-cause XP and clocks, reconciling actual code before editing. Bea starts the 16-active-minute clock when her record activity becomes independent (after the first compulsory interaction lesson); pause compulsory lessons/dialogue/settings/results under existing rules. Reading/independent work consume time. Free next morning keeps work, no Trust penalty. Retain an already-started legacy clock exactly; neither migration nor re-entry resets it. Existing voucher: 180 active seconds, one per case, two carried, atomic consumption/extension; no pressure to buy. The separate 10-minute company clock is deferred with T11/T12.

Retain activity IDs/rewards. Current Bea-only release: worksheet collection60 coins, Bea approval100 coins, and Bea delivery+4 Trust; maximum160 story coins,160 mastery XP, and+6 Public Trust including worksheet+2. No company reward is granted or required. The retained future company case keeps its planned additional100 coins,100 XP,+4 Trust, which would extend the totals to260/260/+10 only after that case is explicitly added. No new ethics branch; ordinary mistakes/hints/holds are learning, not misconduct. Corrected Bea work, actual delivery and explicit wrap-up gate current chapter completion. No score/Trust/wallet gate. Computer remains product office_computer, price250, same menu/counter/wallet/ownership; its current register covers Bea only. Sale requires completed Bea-only Level_03 plus implemented installation and usable verified register. If unaffordable, return later; paper/calculator remains complete. L4 gains confirmed transfer/draft printing later.

## Presentation and game feel contract

Follow existing L1/L2 paper materials, typography, page turns, outlines, buttons, audio and prop scale. Preserve office and plaza identity; no competing navy/brass dashboard or enlarged multi-panel worksheet. Level 3 identity comes from slip sorting and physical calculator printouts. One active task and primary action per view; context controls remain recognizable.

Record face: short title, prominent amount, purpose/period/reference, optional Inspect original. No unexplained IDs in primary labels. Technical trace stays in optional detail. Use short contextual sentences, not repeated dialogue/tutorial paragraphs. Starting copy budgets: title <= 6 words, primary instruction <= 14 words, routine help <= 2 short sentences. Rewrite/page before shrinking type. Budget exceptions for original source records/rate tables use readable pages, never tiny text. No simultaneous source list/claim notes/worksheet/keypad/tax table/stamp overlay.

Large paper/slip targets, effective 48dp primary hit regions, adequate text contrast, safe areas, readable figures under real phone rendering and no finger-obscured amount. These are implementation targets, not certified standards compliance. Fit/paginate the chosen document rather than shrinking the whole workspace. Source selection uses a neutral outline; correctness waits for Check/review. Symbols/text accompany color. No flashing correctness on every tap.

Reuse bounded motion: paper lift/settle (roughly 0.15–0.3 seconds), short printer feed and clip-in, shared page turn, physical stamp. Stable camera while reading. Small sound on meaningful actions; no overlapping celebrations, per-character sounds, bouncing text or constant sparkle. Final side-quest and chapter rewards use existing result surfaces. Skip cosmetic motion without skipping/simulating commands. Honor existing motion settings where available; do not invent settings/features. Reuse AudioManager/settings and supplied cue families. Cache layout/references; no frame-by-frame scene search/mesh rebuild.

## Continue from existing implementation: expand, migrate, contract

T01–T08 historical implementations and T09 source-present work are the starting point. Revised active T02–T10 and T13–T18 acceptance does not erase their existing code or claim it satisfies the new design. Start at the active sequence in TICKETS, not by rebuilding T01 or starting fresh at T10. T02 verifies actual T09 state and adds only required compatibility/entry behavior. T03 restores the folder/teaching path; T04–T06 replace the outside activity; T07–T09 switch computation/stamps onto reused presentation; T10 delivers Bea's copy. T13–T18 complete the current Bea-only chapter. T11/T12 and the company section below remain deferred. Only retire old L3 UI owners after replacements are wired and restore-tested. No generic refactor prerequisite or parallel old/new active input owners.

### Resume contract

| Existing saved state | Revised continuation | Protected data |
|---|---|---|
| Reception/ToolPractice | Assignment or BeaRecords as appropriate; remove mandatory budget exercise without awarding it | Existing practice tapes kept as legacy references; teaching marker separate |
| BeaRecords | Same acquired source state; normal handover only if not collected | Source IDs, Bag/hotbar, quantities, prior readable papers |
| Worksheet draft | Map section choices by row ID into revised batches; show before first v2 check | Existing edits/reasons retained; no inferred grade/acquisition |
| Legacy worksheet first check frozen | Correct in revised compact view under frozen legacy assessment; no second v2 grade | Original correct/total/tier/XP and claims; old tapes become reference-only |
| Worksheet collected / BeaComputation | Open same case/stage from existing inputs and tapes; no field side quest rerun | Decimal selections, tape provenance/revision, first score/rewards |
| Bea approved / BeaDelivery, copy atAuditor | Pick up/explain/deliver via T10, no recomputation or second approval | Approved snapshot/XP, copy phase, 100-coin claim, clock stopped |
| Bea delivery/closing/completed | Resume copy handover or one-case wrap-up | Bea result/rewards, message/finding claims, inventory |
| Company phases (deferred compatible fixtures) | Preserve exact phase/case/copy state without entering it in the current release | Separate future case results, once-only claims, ownership |

Add a narrowly scoped presentation/assessment version where needed, not a fabricated tax-content change. Detect legacy before normalizing; operate on candidate, persist once, then publish. Migration is idempotent, failure-safe, quiet, no reward events and no progress reset. Do not mutate actual player saves during authoring/testing. Unknown/future snapshots are retained and safely blocked. Do not fabricate old annotation text from answer keys. No signed-save framework replacement or automatic live-save repair.

## Acceptance and exclusions

Completion requires source/build/command/save checks plus separate user phone/Play Mode acceptance. Check actual folder transfer paths bypassing float, current stamps, current paper view, Back/focus/settings interruptions, low-storage write failures, duplicate events, old checkpoint recovery, no cross-case leakage, no side-quest regrading, and unchanged L1/L2 routes. Measure readability/comfort/target reach and per-segment times on actual devices. 15–20 minutes is a tuning target, not license to add pressure or a claimed result.

No implementation, production asset edit, remote ticket, new folder/stamp framework, source-scene recopy, package install, guide replacement, playable archive/drawer/stand, L4 filing or L5 investigation is authorized by this documentation task. Future implementation is assigned by the revised local tickets.


## Screenshot-driven correction — 3 November 2026

User explicitly requires the exact familiar Level2 taxpayer-information layout and its existing transferable field widgets in Level3, not the custom Client/Work/Request/Tax year/Case summary. Use Name, TIN ID, Taxpayer Classification, Taxpayer Type, Income Sources. Established professional context may prefill type/income after background acquisition; do not invent citizenship or relabel the masked identity-card number as TIN. Unsupplied facts remain blank; this adds no mandatory classification quiz or computation gate.

Reuse actual Level2DeskStamps/Level2StampDrag motion/input/ghost/paper contact with a narrow Level3 case/revision/commit adapter. Stamp placement is free on the current folder page, persisted as optional geometry alongside the existing readiness record. No duplicate L3 stamp set or tap-stamp-box workflow. Default adapter-null Level2 routes retain Rosa behavior. Do not mark all T09 revised acceptance complete merely because this presentation follow-up shipped.

Replace double-tap tape attachment with shared ValueDragSource/ValueTransferManager drag/drop plus existing physical paper flight, clipped result and brief settle. Tap-select then tap-destination is an equivalent alternative; no double-tap confirmation. **Latest user correction (3 November): paper LEFT, calculator RIGHT, short tape in the gap beside the calculator throughout lift/attach/return/reopen and lesson handoff.** This supersedes the earlier paper-right follow-up. Latest follow-up: move tools only through explicit paper/calculator borders, clip or roll, bounded to separate reading columns. Content gestures never reposition either tool. Source amounts select through ValueTransferManager and place through its destination on the existing calculator display, by tap or drag. Print in a measured point-to-meter coordinate system; both lesson amounts share a fixed size, with a large receiving field. Preserve exact calculations, teaching markers and source provenance; no changed reward or tax-rule contract. T07 still owns the full progressive Computation pages.
