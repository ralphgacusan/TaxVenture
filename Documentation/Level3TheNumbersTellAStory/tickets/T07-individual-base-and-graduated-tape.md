# T07 — Compute Bea’s taxable income and tax due inside the existing Case Folder

**5 November fixed-page/manual-band revision:** The user's latest clarification supersedes changing/collapsing stages and separate Income/Deductions fields. Page3 is Computation: Taxable income, Annual band, revealed band parts/formula and Tax due. The next leaf contains only Tax credit and Remaining payable. No Calculate/Help/retrieval buttons or band menu. Drop a band from the appended Tax Code Book practice page to reveal reusable Threshold/Rate/Base tax sources and the formula. Perform subtraction, multiplication and addition manually on the existing calculator; attach its final printed tape. Previous book pages and all six domain bands remain intact. Three low-range teaching bands fit the added practice leaf. Internal checks pass; Unity/device acceptance remains pending.

**Follow-up:** Tax due remains on page3; only Tax credit and Remaining payable have their own next page. Bea's certificate is collected from a world paper after her dialogue and opened from Bag; T08/PROGRESS define that handover. Existing source rules and calculator/tape/save mechanics remain.

Status: **Implemented, internally verified; user manual pending**. Blocked by: T01, T03, T06.

Priority: P1. Phase: C — Office computation. Estimated scope: M+; one fresh agent context. If real call-site work exceeds this bound, record a bounded split before adding unrelated framework work.

## Goal and visible accomplishment

At the office, use summary/book/folder values and manual physical calculator keys to attach taxable income, tax due and remaining payable on one fixed Computation leaf.

Revision: **3 November 2026**. Read [REVISION_PLAN](../REVISION_PLAN.md), [SPEC](../SPEC.md), [reuse map](../REUSE_AND_ARCHITECTURE.md), [visual contract](../WORLD_AND_FEEDBACK.md), [verification](../VERIFICATION.md) and [AGENTS](../AGENTS.md). Planned refers to changed revision acceptance, not absence of reusable implementation. Historical baseline: Reuse current exact income/tax/band/tape/dependency evaluation and physical calculator. Replace separate clipboard computation leaf with existing folder page presentation, not a second computation model.

## Existing systems to reuse

Existing source paths relative to repository root; inspect live callbacks/content before changing them. Narrow Level_03 adapters only; no duplicate folder/stamp/calculator/input/save/shop framework.

- `Assets/Scripts/Level3/Level3ComputationRules.cs`
- `Assets/Scripts/Level3/Level3DeskCalculatorComputation.cs`
- `Assets/Scripts/Level3/Level3Calculator.cs`
- `Assets/Scripts/UI/CaseFolderUI.cs`
- `Assets/Scripts/UI/CaseFolderPageContent.cs`
- `Assets/Scripts/Data/DragDrop/ValueTransferManager.cs`
- `Assets/Scripts/Data/DragDrop/CaseFolderFieldRow.cs`
- `Assets/Scripts/Data/DragDrop/DataValue.cs`

## Work and interaction mechanics

1. Keep the real folder's Client/Supporting pages reachable; page3 has taxable income, annual band and tax due; page4 has tax credit and remaining payable. No active-stage replacement, collapsed summaries or extra Income/Deductions fields. A selected band reveals three source fields in reserved space; each leaf’s fields stay put.
2. Reuse the shared select/place and drag interfaces. Exact decimals/source/case/year/revision travel together; no float or legacy CaseData money bridge. Wrong plausible bands/operations stay unsupported drafts, not blocked answers.
3. From the one Bag summary, drag Income → calculator, physical minus, Supported deductions, equals; attach the printed Taxable income. No separate folder total placements. Direct summary-token provenance supports the result; do not autofill or persist invisible legacy fields.
4. Append one three-band practice leaf to the book using its existing styled chips/descriptions. Do not modify existing pages. A band chip transfers a rule identity to Annual band, not a single numeric operand. That field is a source/destination, with no menu.
5. The selected band's Threshold, Rate and Base tax are exact reusable numeric sources. Drag Taxable income, subtract Threshold, equals; multiply Rate, equals; add Base tax, equals. The existing calculator carries the chain, prints and attaches Tax due. No automatic formula load or new band operation. Then transfer the certificate credit, manually subtract it from Tax due, and attach Remaining payable on the Tax and payment page.
6. Keep original rule validation, signed save-before-flight, dependent staleness, legacy field-linked tapes, neutral feedback and first-use lesson. A changed band invalidates old rule-source drags/tapes; source transfer never consumes a document. Typed answers remain unsupported.

## Assets, motion and feedback

Read [OFFICE_CASE_PLAY](../OFFICE_CASE_PLAY.md), amended by the user's 5 November clarification. Reuse printed blue destination/source fields with stacked labels/values, existing page turns, grips, pop/settle/receipt flight and quiet cues. The player opens/positions each tool manually. 128-unit fields keep fixed positions on their respective leaves; band-part fields/formula occupy reserved space only after a band is chosen. The added book leaf uses native chip/description spacing; existing reference pages stay unchanged. Actual phone readability and three-tool manipulation need user acceptance. No tiny auto-shrink or correctness flash.

## Integration requirements

T06 now emits Bea’s Income & Expense Summary via the compact machine described in [SIDE_QUEST_MACHINE](../SIDE_QUEST_MACHINE.md). It remains one `bea_summary` campaign possession with `B-WORKSHEET` domain work provenance, readable from Bag and as the same desk paper; no physical summary duplicate is created in Supporting Documents. Transfer its exact corrected income/deduction totals and provenance; do not reconstruct another summary, reopen the outdoor machine/large board, expose answer-key totals or regrant worksheet rewards.

Preserve shared L1/L2 behavior, existing serialized identities/source scenes and all campaign possessions. Keep current exact-decimal L3 rules/provenance; no float prototype bridge. Use existing input/dialogue/FSM/guide/Bag/reward owners. Career3 stays gated until T18, L4/L5 remain unavailable. No remote ticket, package, scene recopy or global cleanup.

## Persistence and failure rules

Candidate -> signed persist -> publish before success effects. Validate mode/case/source/revision/prerequisites/action IDs; duplicates do not award. Preserve legacy grades/claims/approved snapshots/copies and quiet restores. Failure keeps prior state/source/copy retryable. Clear only owned pointer/selection/locks on Back/settings/disable/focus/camera return; independent work can be left. Practice/replay/Editor cannot mutate live story state.

## Internal checks — AI source/build/regressions only

- [x] Active runtime strings/prompts and fallback errors follow OFFICE_CASE_PLAY: short task/action copy, optional detail, no debug/provenance dump or answer-key reveal; actual bindings inspected.


- [x] Live folder transfers/round trips keep exact cents and provenance, no float authority or duplicate state.
- [x] Correct fixture600,000/62,500 and wrong input/band/formula drafts behave; matching typed number cannot support approval.
- [x] Two result attachments suffice; equivalent supported calculation accepted; edits stale only dependents/current stamp.
- [x] One actual folder/computation owner, old leaf not simultaneously active; L1/L2 transfers/folder pages unchanged.
- [x] Build actual changed production paths and run targeted command/save/failure/mode checks plus affected shared regressions; record commands/counts/warnings, not historical passes.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] Apply OFFICE_CASE_PLAY on a supported phone: one task/primary action, no overlapping instructions, readable values/details, short satisfying physical feedback, sound-off clarity and safe Back/focus behavior.


- [ ] Open the actual Bag summary, calculate and attach taxable income; transfer a book band, manually calculate tax due from its parts, then turn to Tax and payment, subtract the certificate credit and attach balance.
- [ ] Both fixed leaves and revealed band parts fit phone; added book leaf readable; page positions stay fixed and printer/clip motion is restrained.
- [ ] Confirm real 3D targets/animations/text/safe areas/Back/focus/audio on supported devices; no source declaration counts as visual acceptance.

## Handoff and completion definition

Deliver the goal above through its real entry point, including persistence, presentation and checked limitations. Update [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md) with actual evidence and remaining user checks; do not mark implemented for documentation presence. User owns Unity and every Git operation.

Direct dependents: T08. Continue the revised index, retaining prior working changes and historical results.
