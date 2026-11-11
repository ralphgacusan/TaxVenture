# Level 3 implementation specification — revision 2

Office presentation update — **4 November 2026**: [OFFICE_CASE_PLAY](OFFICE_CASE_PLAY.md) governs the approved Bea office interaction, short screen copy, progressive detail and restrained print/clip/stamp/review/delivery feedback in T07–T10; T17 verifies the complete experience. Sources/financial fixtures remain intact, richer details stay accessible and no production work is claimed.

Current outdoor interaction: [SIDE_QUEST_MACHINE](SIDE_QUEST_MACHINE.md) governs T04–T06. Feed halfway → select treatment/reason → lever-file → next record → Check batch/correct → collect the small summary (existing B-WORKSHEET) into Bag/Case Folder support for office use. It supersedes direct three-bin placement and giant worksheet presentation; source facts, nine-decision grading, legacy results and rewards are unchanged.

Authority: [REVISION_PLAN](REVISION_PLAN.md), approved 3 November 2026, is the detailed current contract. Existing financial/source tables in [FIXTURE_CONTRACT](FIXTURE_CONTRACT.md) remain valid; its explicitly labeled v1 interaction/scoring sections are historical compatibility only. The [earlier proposal](../Level3DesignProposal.md) is background, not an instruction to rebuild removed mechanics.

## Scope and owners

Keep Chapter Level_03, Office_Level3, existing Level3Gameplay root, L3-BEA-001, the retained L3-MARINE-001 future fixture, worth_the_tax_3 badge, existing sources/claims/currency/equipment. The current release and active story path contain Bea's case only. The company case is deferred with its tickets and documentation preserved; it is not a second current case or a completion/release prerequisite. Keep all Bag/hotbar ownership, quantities, slots, capacities, pins and readable prior records across L1 -> L2 -> L3. Used supplies stay used. No re-created answer-key annotations, global archive or new save manager.

Use GameStateMachine as coarse runtime owner, Level3ChapterRules/Checkpointer as bounded saved phase/command owners. Objectives/guide/UI observe committed progress; they cannot grant it. Retain enum identities and legacy ToolPractice compatibility; fresh progression skips mandatory budget practice. First-use calculator instruction is a local saved teaching fact, not another chapter phase framework.

## Current phase mapping

| Phase | Player-visible responsibility | Exit gate |
|---|---|---|
| Reception | Bea appointment | Committed assignment -> BeaRecords |
| ToolPractice (legacy only) | Quiet compatibility route | T02 mapping, no invented teaching reward |
| BeaRecords | Talk, acquire originals | Actual saved handover -> WorksheetDraft |
| WorksheetDraft / WorksheetCorrections | Three batches, one record at a time, automatic totals | Complete batch checks freeze once; all corrected -> WorksheetReady |
| WorksheetReady | Collect folded summary from machine output into Bag/folder | Atomic item/reward/grade claims -> BeaComputation |
| BeaComputation | Existing folder; totals/rule/credit transfers; three calculated results | Current stamp and actual hand-in |
| BeaReview | Existing Auditor review | Return -> computation; approval/result/copy saved -> BeaDelivery |
| BeaDelivery | Copy pickup, one relevant explanation, actual handover | +4 Trust once -> ClosingFinding |
| ClosingFinding / WrapUpPending | Limited finding, mayor message, result acknowledgment | Completion saved only on acknowledgment |
| CompletedOffice | Optional shop/computer/exit | No reopened review or repeated rewards |

The company phases are retained for future compatibility and the deferred T11/T12 case design; they are not part of the current release path: CompanyAppointment, CompanyComputation, CompanyReview, and CompanyDelivery.

Exact 9-decision batches, scoring, transfer provenance, result dependencies, authored values, presentation limits, clocks, user journey and legacy resume table are defined in REVISION_PLAN. Do not retain old 12-decision grading as the new activity merely to keep old tests green. Preserve it only for legacy saves/results. No mandatory separate subtotal/excess/marginal attachments.

## User stories

1. As a returning player, I want my possessions and history intact, so that this chapter continues my career.
2. As a player, I want one readable record and a compact feed/selector/lever machine, so that I can reason without managing a crowded board.
3. As a learner, I want a deliberate check of a small batch, so that I can correct misunderstandings before proceeding.
4. As a player, I want my first checked choices retained, so that corrections and mastery have distinct meanings.
5. As a consultant, I want to collect my corrected worksheet, so that the outside task supplies my office case.
6. As a returning player, I want the familiar Case Folder and transfer gesture, so that new calculations build on tools I know.
7. As a learner, I want to choose supported inputs and the rule, so that the calculator does arithmetic without deciding the case for me.
8. As a learner, I want to see base tax, excess and credit clearly, so that I understand the three results.
9. As a player, I want unchanged working preserved when I edit one input, so that correction feels manageable.
10. As a consultant, I want the existing stamp and Auditor review, so that I recommend readiness and receive focused correction.
11. As a player, I want free next-day recovery, so that a missed review window never erases work.
12. As a client, I want an explained copy delivered, so that approval resolves my actual concern.
13. **Deferred future case:** As a learner, I want a shorter company challenge with less guidance, so that I can apply the same process independently.
14. As a player, I want a qualified story discovery, so that the investigation builds without claiming unsupported intent.
15. As a player, I want an optional usable computer upgrade, so that my office develops without a purchase gate.
16. As a resuming player, I want old drafts/grades/approvals/copies restored safely, so that the revision does not erase or repay my work.
17. As a mobile player, I want large readable targets and reliable Back/settings/focus recovery, so that physical interaction remains comfortable.
18. As a replaying player, I want mastery practice isolated from my campaign, so that learning cannot spend or duplicate story possessions.

## Data and transaction boundaries

Exact money remains invariant decimal data owned by existing L3 rules. Adapt existing value-transfer interfaces; no AsFloat/prototype calculation bridge. Source ownership, case, row, reporting year, selected rule, tape provenance and revisions survive transfers/restores. Supported chains, not matching final numbers, allow approval. Wrong plausible same-case choices remain editable drafts; foreign sources and structurally invalid actions are refused neutrally.

Bounded additive snapshot fields cover assessment/presentation version, three batch first outcomes/accepted state, first-use instruction marker and any necessary per-case stage/copy facts. Preserve existing source/tape/computation/review state rather than duplicating it for each UI. Legacy migrations are candidate -> signed persist -> quiet publish, idempotent and failure-safe; unknown future data is retained/blocked. No answer keys in saves.

Each durable command owns all associated effects: batch freeze, worksheet collection, approval, voucher use, explanation/handover, closing and purchase. A company case switch remains a future command only if the deferred second case is activated. Duplicate action IDs cannot grant twice. Failed writes keep prior work and actionable source/copy. Full result aggregation never double-awards worksheet XP or writes a completed level record early. Source/credit changes invalidate only dependents. Copy reading is not a mutation.

## Implementation and release

Local specification readiness: **ready-for-agent**; no remote issue publication. Follow the active Bea-only sequence in [TICKETS](TICKETS.md): T07–T10, T13–T18. T11/T12 and company content remain preserved as deferred future work, outside the current release dependency graph. Expand only required seams, move existing functionality to the reused surfaces in bounded slices, retire obsolete L3 interaction owners after their replacements work. Each feature ticket includes readable 3D presentation, input/animation/save integration and targeted checks; T17 polishes, it does not rescue missing visual implementation.

T18 enables the Bea-only Career3 release only after actual internal integration and the applicable Bea content-authority gates are satisfied. Company-only authority gates do not block this release; the future company release must resolve them before activating T11/T12. Keep user visual/device acceptance distinct; unresolved user-reported blockers prevent user-accepted claims. L4/L5, simulated filing, endings, comparison stand, drawer and broad archive stay outside this revision.
