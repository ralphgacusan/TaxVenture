# T05 — Feed, select and lever-file records through the compact machine

Status: **Implemented, internally verified; user manual pending**. Blocked by: T04. Built 4 November 2026: see PROGRESS (T05 entry) and KNOWN_ISSUES L3-R58–L3-R66.

Priority: P1. Phase: B — Side-quest drafting. Estimated scope: M; one fresh agent context. If real call-site work exceeds this bound, record a bounded split before adding unrelated framework work.

## Goal and visible accomplishment

Implement the physical read → half-feed → select → pull → next-paper loop, neutral draft totals and recall tabs. The machine replaces direct three-tray sorting, not the worksheet rules/save owner. Read [SIDE_QUEST_MACHINE](../SIDE_QUEST_MACHINE.md), including its state table, exact record registry, motion table, visual composition and failure boundaries.

Revision: **3 November 2026 — approved paper-machine interaction**. Status remains Planned; source presence is reuse evidence, not acceptance. Retain source/save identities and work serially from the current continuation.

## Existing systems to reuse

Existing source paths relative to repository root; inspect live callbacks/content before changing them. Narrow Level_03 adapters only; no duplicate folder/stamp/calculator/input/save/shop framework.

- `Assets/Scripts/Level3/Level3WorksheetRules.cs`
- `Assets/Scripts/Level3/Level3WorksheetDesk.cs`
- `Assets/Scripts/Level3/Level3WorksheetModel.cs`
- `Assets/Scripts/Level3/Level3WorksheetHints.cs`
- `Assets/Scripts/Level3/Level3Checkpointer.cs`
- `Assets/Scripts/Level3/Level3DeskProps.cs`

## Work and interaction mechanics

1. Reuse R1/R2 earnings, R3/R4/R5 costs, R6/R7 questions; only the current batch is available. One card on a stand, one compact machine, max three current-batch review tabs; no old board, permanent notes, source fan or field calculator.
2. Drag current slip to intake or tap slip then intake. Snap gently and half-feed while leaving title/amount visible. Off-target returns safely; mechanical target accepts any owned current row without a correctness cue. Loading does not classify, alter totals or consume an original.
3. Enable selector after loading, neutral for each new/unfiled row. Tap a category or rotate between Income/Deduct/Leave Out detents. All choices accept every row. The lever stays raised until category and required reason exist; explain missing input in one short prompt.
4. For R6/R7 only, show the compact Why? picker after selection for every destination, with plausible neutral existing reason options. Preserve pending choices during original viewing and deliberate B-PRINT/B-EXP comparison. No auto-reason, answer-revealing label or extra scored link form.
5. Pull down and release past a deliberate threshold, or tap handle, to execute one atomic worksheet edit through existing CommitWorksheet. Candidate validates row/case/revision/batch/mode and writes category+reason together. Do not mutate the saved draft during selector preview. Save failure keeps the half-fed choices retryable; no feed-success effects.
6. After publish, animate lever/rollers/full suction behind a slot lip, then tab settle and next paper. Every valid classification receives identical neutral effects, including wrong choices. Busy gating and consumed pointer release prevent double filing/tap-through; skip/interrupt settles committed state without another command.
7. Review tabs show title/current category; tap to recall before Check or during T06 correction. Keep the old durable row until replacement saves. Cancel preserves it; refile replaces, never duplicates. Accepted earlier batches are read-only. Recalled rows retain saved choices; new rows start neutral.
8. Totals use exact selected draft amounts, including wrong choices; no hidden expected-answer correction, arithmetic grade or half-fed contribution. Compact Income/Deductions windows only. Original source/transaction identity follows every choice automatically.
9. Teach feed/select/pull once without revealing treatment. Update BeaWorkIndependent/guide semantics to first successful saved filing, not intake or dial rotation. Preserve running legacy clock, frozen grade, drafts and possessions. Back/settings/focus/leave release only owned locks; uncommitted paper returns safely, committed work restores quietly.
10. Implement actual body/slot/stand/dial/lever pivots/rollers/colliders and readable faces using current asset candidates plus minimal Level3DeskProps parts. Follow contract timing, existing palette/font/audio and tap alternatives. Expose current-batch completeness to T06; no pretend success/check button until T06 owns actual checks.

## Assets, motion and feedback

Follow [machine visual/motion contract](../SIDE_QUEST_MACHINE.md) and [world contract](../WORLD_AND_FEEDBACK.md). Same existing low-poly scene, paper/font/material vocabulary and restrained sound/settings; no oversized navy/brass board, new art theme, full-screen flat substitute or auto-shrunk labels. Real anchored 3D bodies, reachable targets and correct slot occlusion are required in this slice. Implementer binds everything; user checks actual Unity/phone rendering. The linked storyboard is a mechanical reference, not final visual acceptance.

## Integration requirements

Preserve shared L1/L2 behavior, existing serialized identities/source scenes and all campaign possessions. Keep current exact-decimal L3 rules/provenance; no float prototype bridge. Use existing input/dialogue/FSM/guide/Bag/reward owners. Career3 stays gated until T18, L4/L5 remain unavailable. No remote ticket, package, scene recopy or global cleanup.

### Entry from the T04 buildup (4 November)

Start in the seated workspace the buildup opens: `Level3BeaTable.TrySit` after Help Bea, or the seat after Later or a restore (`Step == Ready`). Never replay her introduction, envelope, reveal or handover; acquisition stays `CommitBeaRecords`. Replace `worksheet-intro`'s pages (same id, `flag:atBeaTable`, Workstation, no `after`) with "Feed the record" / "Choose where it belongs" / "Pull to file", each waiting on the real action. Bea's T04 lines name no treatment and the buildup leaves the draft empty and her clock unstarted; T05 moves `BeaWorkIndependent` to the first saved filing. Keep one live table target: machine colliders are seated-only, and the seat box's Interactable layer stays the table's switch.

## Persistence and failure rules

Candidate -> signed persist -> publish before success effects. Validate mode/case/source/revision/prerequisites/action IDs; duplicates do not award. Preserve legacy grades/claims/approved snapshots/copies and quiet restores. Failure keeps prior state/source/copy retryable. Clear only owned pointer/selection/locks on Back/settings/disable/focus/camera return; independent work can be left. Practice/replay/Editor cannot mutate live story state.

## Internal checks — AI source/build/regressions only

- [x] Half-feed is uncommitted; lever saves destination+reason atomically; failed/double pull and interrupted feed preserve exactly one row.
- [x] Tap/drag and tap/pull parity; new neutral selector; loaded-header readability; neutral wrong filing; recall/cancel replaces no duplicate. (Parity/readability by command, geometry and font checks only.)
- [x] Totals follow published wrong/right choices; first saved filing clock boundary and legacy quiet restore remain correct.
- [x] Seven entries preserve source identity; three batches/selector treatments work without correctness snapping. (Later batches via accepted-batch fixtures; T06 writes acceptance.)
- [x] Automatic totals reflect selected entries; no field calculator/subtotal gate; source totals never read expected answers.
- [x] R6/R7 reasons accessible on any placement; repeated extraction/undo/cancel/save failure have defined outcomes.
- [x] Legacy choices map correctly through T02; existing batch state/draft restores without duplicate events.
- [x] Build actual changed production paths and run targeted command/save/failure/mode checks plus affected shared regressions; record commands/counts/warnings, not historical passes.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] Drag or tap slip into slot; exposed header stays readable; selector starts neutral; Why?/Compare return to same pending row.
- [ ] Pull or tap lever; paper enters behind lip, rollers move, tab settles and next arrives briskly; wrong choices feel identical.
- [ ] Recall/cancel/retry, off-target drop, Back/settings/focus and camera return preserve safe poses, reachable targets and clean audio.

## Handoff and completion definition

Deliver the goal above through its real entry point, including persistence, presentation and checked limitations. Update [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md) with actual evidence and remaining user checks; do not mark implemented for documentation presence. User owns Unity and every Git operation.

Direct dependents: T06. Continue the revised index, retaining prior working changes and historical results.
