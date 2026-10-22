# T18 — Adapt Rosa's review to campaign state and inventory

Status: Planned. Blocked by: T17.

## Goal

Adapt Rosa's existing review to shared campaign ownership without enabling it.

## Visible accomplishment

Before: the Rosa review runs through its existing case checkpoint but is outside the new campaign bag/reward domain. After: the existing Rosa review can run against the new campaign foundation with saved source records, bag contents, corrected-case progression, public trust and the two once-only Level 2 story rewards. The player can read and compare existing records through the unified bag while completing the real review. This prepares Level 2 for Delivery B integration but leaves New Game/Continue and Career launch disabled until T21.

## Work

Keep the existing Level 2 case, annotated papers, clock, records, tutorial and finale content intact while adapting campaign ownership at its boundaries. Continue using the saved Level 2 checkpoint data and stable review/case IDs. Preserve original and working document identity/revision separately and keep answer keys out of bag item snapshots. Add existing acquired records, profile references and annotated/working papers to the same bag as T10/T11; the bag is a catalogue of real models, not a replacement for the desk or a second source of case values. A player can inspect owned records while exploring, with movement locked during the viewer; formal comparison, transfer, filing, stamping, findings submission and approval remain at their existing workstations. Bag viewing follows the active review clock rule, while only the same authored blocking pauses used by the existing review may pause it.

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

## USER manual acceptance — Unity/device/visual/feel/listen checks

- [ ] In isolated Level 2 testing, collect/read actual source papers, leave the desk, use the bag, then perform comparison/submission only at the workstation.
- [ ] Interrupt at review, correction and approved milestones, resume, and verify no duplicate paper, stamp, reward or trust change.
- [ ] Check bag/book camera framing, document readability, touch/movement lock, clock pause behavior, actor/stamp responses, motion and audio in Unity/on device. No manual setup is requested.

## Handoff

Next ticket: [T19](T19-rosa-choice-and-trust-consequences.md) adds the explicit, once-only ethics choice.

## Non-goals and references

No final Level 2 enablement, ethical-choice implementation, comparison-stand purchase, new tax rules, L3 computer, or replacement case framework. See [SPEC](../SPEC.md), [WORLD_AND_FEEDBACK](../WORLD_AND_FEEDBACK.md), [VERIFICATION](../VERIFICATION.md), and [PROGRESS](../PROGRESS.md).
