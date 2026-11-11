# Ticket pack — 21 baseline slices plus tentative final T22

Status: T01–T03 implemented, internally verified; user manual pending. T04–T21 Planned. T22 tentative/deferred after T21, approved as a stored future option on 12 November 2026. Start with [README](README.md), [SPEC](SPEC.md), [journeys](EXPERIENCE_FLOW.md), [current evidence](CURRENT_STATE.md), [assets/contact/voice](ASSET_AND_ANIMATION_PLAN.md), [coverage](COVERAGE.md) and [verification](VERIFICATION.md).

## Why these boundaries

The smallest practical reviewed decomposition is21 slices:2 safety/pilot,4 shared/L1,4 L2,4 L3,1 clocks/results,4 speech and2 integration/device gates. Tentative T22 is excluded from the baseline release gate. The full-Bag blocker is independently executable. The visual/contact/cost pilot precedes migration. The first-person/sprint loop proves shared ownership before bounded room adoption; mixed-camera/jump adoption is stored for tentative final T22. Each Bayview stage owns its real packet/review/retention. Retrieval and teaching, and machine and office calculation, are separated for scene/input/contact/save risk and fresh-agent context fit. Client correction/explanation/delivery stay together.

Visuals, motion, audio, bindings and recovery belong within each playable slice, not separate models-only/sound-only/UI tickets. Speech has one lifecycle pilot and three branch-bounded chapter coverage tickets after stable scripts. Final integration/device gates verify and tune already complete features. No generic preparatory refactor is justified; first-person camera ownership stays stable, bounded smoothing/contact usage migrates, and superseded active writers retire only after replacement acceptance. Historical decoders/shared clips stay where needed.

## Ordered index and direct dependents

| Ticket | Outcome | Prerequisites | Direct dependents | Status |
|---|---|---|---|---|
| [T01](tickets/T01-base-paperwork-with-full-bag.md) | Finish required paperwork with a full base Bag | None | T08, T13 | Implemented, internally verified; user manual pending (11 Nov) |
| [T02](tickets/T02-visual-contact-and-device-pilot.md) | Prove the representative visual and contact pilot | None | T03 | Implemented, internally verified; user manual pending (11 Nov) |
| [T03](tickets/T03-mixed-camera-exploration-and-mobile-movement.md) | First-person exploration and sprint; store the mixed-camera pilot | T02 | T04, T05 | First-person amendment internally verified; user manual pending (12 Nov) |
| [T04](tickets/T04-arthur-reviewed-file-embodied-teaching.md) | Learn from Arthur's physical reviewed-file example | T03 | T05, T09, T13, T17 | Planned |
| [T05](tickets/T05-living-community-and-profile-round.md) | Complete a concise living community and profile round | T03, T04 | T06, T08, T17 | Planned |
| [T06](tickets/T06-marie-office-review-and-copy-service.md) | Finish Marie's supported office work and physical service | T05 | T07, T10, T14, T15, T16, T17 | Planned |
| [T07](tickets/T07-bayview-level2-classification-stage.md) | Play Bayview's first identity and classification stage | T06 | T08, T11, T18 | Planned |
| [T08](tickets/T08-rosa-laundry-retrieval-and-kanor-letter.md) | Enter Rosa's laundry and retrieve meaningful records | T01, T05, T07 | T09, T18 | Planned |
| [T09](tickets/T09-arthur-toolkit-and-income-activity.md) | Learn and use the annotation toolkit beside Arthur | T04, T08 | T10, T12, T18 | Planned |
| [T10](tickets/T10-rosa-choice-reconciliation-and-delivery.md) | Reconcile, correct and deliver Rosa's case | T06, T09 | T11, T14, T15, T18 | Planned |
| [T11](tickets/T11-bayview-level3-income-stage.md) | Play the returned Bayview income-review stage | T07, T10 | T12, T19 | Planned |
| [T12](tickets/T12-bea-workspace-and-embodied-paper-machine.md) | Work with Bea in her art room and paper machine | T09, T11 | T13, T19 | Planned |
| [T13](tickets/T13-bea-calculation-and-arthur-desk-examples.md) | Compute Bea's stages with embodied first-use examples | T01, T04, T12 | T14, T19 | Planned |
| [T14](tickets/T14-bea-review-copy-and-supported-closure.md) | Review, explain, deliver and close Bea's supported case | T06, T10, T13 | T15, T19, T20 | Planned |
| [T15](tickets/T15-dual-clocks-punctuality-and-result-meaning.md) | Understand clocks, punctuality and earned results | T06, T10, T14 | T16, T20 | Planned |
| [T16](tickets/T16-dialogue-speech-pilot-and-manifest.md) | Hear the stable dialogue-session speech pilot | T06, T15 | T17, T18, T19 | Planned |
| [T17](tickets/T17-level1-complete-dialogue-voice-coverage.md) | Hear every retained Level 1 dialogue branch | T04, T05, T06, T16 | T20 | Planned |
| [T18](tickets/T18-level2-complete-dialogue-voice-coverage.md) | Hear every retained Level 2 dialogue branch | T07, T08, T09, T10, T16 | T20 | Planned |
| [T19](tickets/T19-level3-complete-dialogue-voice-coverage.md) | Hear every retained Level 3 dialogue branch | T11, T12, T13, T14, T16 | T20 | Planned |
| [T20](tickets/T20-three-chapter-integration-and-save-continuation.md) | Continue one coherent campaign through interruptions | T14, T15, T17, T18, T19 | T21 | Planned |
| [T21](tickets/T21-current-phone-performance-and-panel-acceptance.md) | Accept the panel candidate on the current phone | T20 | T22 (tentative only) | Planned |
| [T22](tickets/T22-tentative-third-person-and-jump-adoption.md) | Tentative third-person and jump adoption after baseline acceptance | T21 | None | Tentative / deferred; stored disabled |

Size/risk, exact reuse/evidence, external gates and all 22 required ticket categories are in each linked file. Dependents name direct prerequisites; the order above is topological. No Level4/5 implementation ticket exists.

## Dependency DAG

Each row lists only direct blockers; ticket files and the index must match:

```text
T01: []
T02: []
T03: [T02]
T04: [T03]
T05: [T03, T04]
T06: [T05]
T07: [T06]
T08: [T01, T05, T07]
T09: [T04, T08]
T10: [T06, T09]
T11: [T07, T10]
T12: [T09, T11]
T13: [T01, T04, T12]
T14: [T06, T10, T13]
T15: [T06, T10, T14]
T16: [T06, T15]
T17: [T04, T05, T06, T16]
T18: [T07, T08, T09, T10, T16]
T19: [T11, T12, T13, T14, T16]
T20: [T14, T15, T17, T18, T19]
T21: [T20]
T22 (tentative): [T21]
```

E-DEVICE, E-PILOT, E-CONTACT, E-VOICE-L1/L2/L3 and E-CONTENT are named asset/evidence dependencies in [KNOWN_ISSUES](KNOWN_ISSUES.md). User native acceptance is separate from source readiness; no default or historical pass resolves a gate.

## Precise first executable ticket

[T01 — full-base-Bag paperwork](tickets/T01-base-paperwork-with-full-bag.md) has no ticket or product-decision blocker. It reproduces existing L3-R106 with disposable full-loadout fixtures, then provides a deliberate certificate→existing supporting-slot acquisition/attachment alternative through current signed owners. It must preserve possessions, source identity, reader/value-selection, failure/duplicate/reload behavior and full-Bag detach refusal, without purchase or automatic credit entry. The file supplies exact implementation boundaries and binary checks.

T02 pilot preparation is independent, but broad migration waits for accepted appearance/contact and measured current-phone cost. No parallel implementation is authorized by this dependency graph.

## One-week sequence and handoff

Target horizon11–18 November 2026, time unspecified. Prove T01 and pilot/source/device dependencies early; proceed serially through shared/L1, L2 and L3 playable slices; lock shortened scripts before the corresponding new recording batches; reserve final time for integrated/current-phone acceptance. No exact-hour estimate or deadline/performance guarantee is claimed.

Missing usable contact animation, a required dialogue take, curriculum sign-off or phone measurement remains a concrete blocked branch. Complete unaffected work and identify what resolves it; do not call placeholders accepted, silently drop approved scope or use final polish to absorb omitted requirements. Use [PROGRESS](PROGRESS.md) and [KNOWN_ISSUES](KNOWN_ISSUES.md) for truthful status.
