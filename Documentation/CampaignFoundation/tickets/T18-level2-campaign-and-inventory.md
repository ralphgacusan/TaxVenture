# T18 — Adapt Rosa's review to campaign state and inventory

Status: Planned. Blocked by: T17.

Scheduling revision 2026-10-28: T17 internal verification is complete; this is the next implementation ticket. T27 retains the fixed eight-type roadmap but is not a dependency. Implement only Rosa's required types here; T21 still verifies the integrated result and controls campaign enablement.

## Goal

Adapt Rosa's review to shared campaign ownership, carried Level 1 equipment and consistent physical case folders without enabling it. Follow the [Level 2 continuation amendment](../LEVEL2_CONTINUATION.md).

## Visible accomplishment

Before: the Rosa review runs through its existing case checkpoint but is outside the new campaign bag/reward domain. After: the existing Rosa review can run against the new campaign foundation with saved source records, bag contents, corrected-case progression, public trust and the two once-only Level 2 story rewards. The player can read and compare existing records through the unified bag while completing the real review. This prepares Level 2 for Delivery B integration but leaves New Game/Continue and Career launch disabled until T21.

## Work

Supporting-paper amendment — incremental adoption (user revision 2026-10-28): follow the [fixed document standard](../../SupportingDocuments.md) and [T27 roadmap](T27-eight-document-template-library.md), building only Types 4–7 required by Rosa. This ticket owns their reusable physical presentations, source mapping and bounded fixtures; no advance all-eight library or preview is required. Inspect and bind Rosa's six D1–D6 records: collection sheet, bank extract, three transaction records and loan agreement. Record and verify the actual ID-to-type mapping here, preserving all six papers, source IDs, periods, amounts, evidence links, annotations and current review flow. Reuse existing card/sheet/slip builders, safe extraction/return and viewer behavior. Keep the fixed layouts across clients; long values wrap, missing optional values use a dash or “Not applicable”, and table overflow uses another face/page of the same type. Types 3 and 8 and unused variants wait for later relevant cases. Keep paid comparison-stand benefits and free inspection intact. T21 verifies this subset as part of Delivery B.

Preserve Level 2 case facts, annotated-paper identities, review rules, clock and records while adapting ownership. Tutorial, dialogue, folder presentation and finale delivery may improve under the continuation amendment; T33 owns integrated polish. Do not preserve obsolete Arthur-led opening requirements merely because old copy exists. Continue using the saved Level 2 checkpoint data and stable review/case IDs. Preserve original and working document identity/revision separately and keep answer keys out of bag item snapshots. Add existing acquired records, profile references and annotated/working papers to the same bag as T10/T11; the bag is a catalogue of real models, not a replacement for the desk or a second source of case values. A player can inspect owned records while exploring, with movement locked during the viewer; formal comparison, transfer, filing, stamping, findings submission and approval remain at their existing workstations. Bag viewing follows the active review clock rule, while only the same authored blocking pauses used by the existing review may pause it.

### Level 1 items and folder continuity

Carry the same campaign inventory, wallet, trust, mastery, settings and upgrades into Level 2 without clearing/reseeding or replaying acquisition effects. Record an actual item-category eligibility table: references remain optionally readable; chapter-bound Level 1 quest objects cannot become Rosa evidence or reactivate old tasks; bookmarks retain applicable behavior; slot upgrades retain capacity; used supplies stay used. Eligible vouchers extend Rosa's review by the existing +180 active seconds and consume once. Shop ownership prevents repurchases. Purchased decor restores at compatible authored Level 2 office anchors; missing anchors preserve ownership with clear unplaced feedback. Full Bag or failed acquisition leaves required records retrievable without an upgrade purchase. Use existing eligibility/save commands, not a second wallet or inventory.

Bring Rosa's real folder, annotations and source/working papers to Level 1's physical depth, page turns, typography, safe front focus/drag, labels, Bag representation and interrupted return/cleanup. Preserve all six D1–D6 papers and existing packet pages; no forced two-page conversion. Opening Arthur's carried reference never changes Rosa's active case or transfers answers. Improve narrow existing start/guide/shop/item/viewer seams only where continuation needs them, rather than building speculative future-level frameworks.

### Source starting points

- **Existing:** `Assets/Scripts/Level2/Level2Checkpointer.cs`, `Level2OfficeStart.cs`, `Level2ReviewClock.cs`, `Level2RosaStall.cs`, `Level2AnnotatedPapers.cs`, `Assets/Scripts/Data/Level2Checkpoint.cs`, and the current signed progress owner.
- **Proposed:** campaign-mode guard, bag snapshot and shared reward/trust commands from SPEC, called when the existing level domain reports a committed result. Preserve the Level2 checkpointer as case-resume data; do not add another progress singleton or replace the review framework.

Adapt only campaign scoring/story semantics: the first complete 12-decision check freezes its tier at the approved existing boundaries; corrections are required for actual approval but cannot improve that first tier. Corrected required field completion awards +2 public trust once; corrected approved case plus required follow-through awards +4 once. Level 2's 60-coin quest claim and 100-coin corrected-case claim each commit once, for 160 total. Do not award per hand-in, stamp, annotation, conversation or replay. The activity's story completion is independent of XP/hearts threshold. The final Chapter 2 celebration and new-campaign launch are held for T21; as an isolated scene, the existing Level 2 route remains usable for development without mutating campaign state.

Use the existing physical desk, papers and bag viewers. For bag opening, a hinged leather folio rotates toward the camera about 70 degrees in 0.4 s; selected paper rises face-forward, reads, then slides back into the same slot on close. A paper must not teleport from desk to bag: represent the owned item as a stable view over the same document identity. A successful new acquisition uses one paper pickup/slot-settle cue after durable commit; restore uses no gameplay cue. Honor effects volume and prevent repeat sound from each frame or restore.

## Persistence and failure rules

The Level 2 checkpoint and campaign inventory/reward claims must describe the same committed milestone. A checkpoint restore raises no award, stamp, profile acquisition or trust event. Candidate checkpoint plus associated wallet/claim/trust update validates and saves together; failed persistence retains the preceding in-memory and durable values, shows no success effect and offers safe retry. Corrected work is required before approval and chapter completion; no premature final stamp or false “complete” state from a stale presentation. Practice, mastery replay and temporary Editor play are isolated from live bag, coins, trust, stamps and choice state. Replays may improve eligible mastery record only. Until T21 explicitly integrates all of Delivery B, do not expose a new-campaign Level 2 launch.

## Internal checks — AI source/build/regressions only

- [ ] Existing review/checkpoint round-trip includes bag links, original/working revisions, 12-decision freeze, clock and corrections without restoring gameplay effects.
- [ ] Tests prove 60/100 coin and +2/+4 trust claims exactly once, approved-only completion, retry on failed commit and story/practice/replay isolation.
- [ ] Static inspection validates source/model links and no new campaign path to Level 2; affected build, campaign-save, Level 2 and bag regressions pass.

- [ ] Types 4–7 preserve all six D1–D6 source identities/values/evidence links through production-viewer inspection and return. Bounded isolated fixtures cover relevant long/missing values, overflow, missing mappings/sources, repeated open/close and interruption; no fixture mutates live campaign state. Existing Level 1 Types 1–2 still pass affected checks.

- [ ] Level 1 completion -> Level 2 entry/resume preserves actual item ownership and eligibility, capacity, settings, wallet/trust/mastery and compatible decor placement. Cover voucher +180 seconds/one consumption, bookmarks, used supplies, references, full Bag, failed save and repeat entry without loss or duplication.

## USER manual acceptance — Unity/device/visual/feel/listen checks

- [ ] Inspect Types 4–7 for fixed layout, recognizable shape, physical depth/shadows, phone readability and safe return; unused types are not required.
- [ ] In isolated Level 2 testing, collect/read actual source papers, leave the desk, use the bag, then perform comparison/submission only at the workstation.
- [ ] Interrupt at review, correction and approved milestones, resume, and verify no duplicate paper, stamp, reward or trust change.
- [ ] Check bag/book camera framing, document readability, touch/movement lock, clock pause behavior, actor/stamp responses, motion and audio in Unity/on device. No manual setup is requested.

## Handoff

Next ticket: [T32](T32-level2-town-and-rosa-animation.md), then T19, T20, T33 and T21. Normal campaign entry remains disabled until T21.

## Non-goals and references

No final Level 2 enablement, ethical-choice implementation, comparison-stand purchase, new tax rules, L3 computer, or replacement case framework. See [SPEC](../SPEC.md), [WORLD_AND_FEEDBACK](../WORLD_AND_FEEDBACK.md), [VERIFICATION](../VERIFICATION.md), and [PROGRESS](../PROGRESS.md).
