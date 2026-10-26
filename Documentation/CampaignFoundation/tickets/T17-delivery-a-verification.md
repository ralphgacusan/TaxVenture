# T17 — Verify and hand off Delivery A

Status: Planned. Blocked by: T16, T29.

## Goal

Prove Delivery A forms one honest, resumable, release-bounded story.

## Visible accomplishment

Before: the new shared systems and Level 1 story are separate planned slices. After: Delivery A is a coherent playable New Game through Level 1 wrap-up. Continue restores the unfinished first campaign or opens Career after Level 1 completion; the Level 1 story, shop introduction, bag and physical pickups agree with the saved result. The campaign menu may show later chapters as coming soon, but it cannot launch the current Level 2 legacy scene as the new campaign continuation. That scene remains available only for isolated development and regression work until T21 integrates Delivery B.

## Work

Latest user revision: wait for T30/T31 and T25-T29 implementation/internal work; original T24's bench path is superseded. Include Arthur start-marker/click/one collectible handover, teal model/ghost/icons, read-only two-page sample, +1 Bag capacities once, real extracted 3D papers and optional two-second grouping, all four freely ordered profile-only clients, short board reminder/category references, Marie's separate editable two-page case with attached records, source isolation and progressed-save compatibility. No global case swap or extra full case. T29's focused findings feed this full matrix; rendered polish and 2–3 minute pacing need user evidence. Use the latest Chapter 1 card/finale; do not rebuild a check lever, opaque flap or obsolete wrap-up sheet.

Verify the ordered player route from first main-menu input through opening, plaza, four profile conversations, draft/check/correction, Marie case, follow-through, optional supply exploration and career return. Include the difficult paths: low frozen grade followed by required corrections; incorrect first submission followed by a corrected approval; missed office cutoff and next-day recovery; optional purchases/no purchase; exit and relaunch before and after each committed milestone. A paused, quit or crashed run must resume at the last durable checkpoint without re-awarding or replaying gameplay events. Confirm New Game requires explicit confirmation, creates the specified fresh campaign, retains settings, and cannot be disguised as Continue. A supported pre-v5 save only receives the updated-story message and cannot resume or grant mastery, story unlocks, trust, coins, or inventory.

### Source starting points

- **Existing:** signed save/reset seams in `PlayerProgressData`, `PlayerProgressManager` and the existing store; `MenuEntry`, `CareerProgress`, `CareerBoardPresenter`; Level 1 town, objective, case and resume systems.
- **Proposed:** feature-readiness/catalogue state that explicitly keeps new-campaign Level 2 pending until T21. Do not add a player save flag as a substitute for code/content integration readiness. Do not alter old Level 2 scene/build data while verifying Delivery A.

Review all owned source/content and deterministic placement data for unsaved transient references, duplicate reward sources, scene names, dialogue pause leaks, input ownership, colliders, physical collection visuals and audio setting use. Confirm no guide or HUD callback mutates campaign state, no badge/tier or XP threshold blocks next story activity, and no shop/keepsake purchase is required. Confirm the first independent check hides correctness before checking and frozen grading cannot be improved by later correction. Check delivery boundary: Career shows Level 1; Level 2 visibly says integration pending/coming soon and cannot load into the legacy route from New Game or Continue. Do not remove the old scene from build for sandbox use. Source review also confirms profile/document lift/turn/return, stable reading poses, neutral draft motion and the automatic complete-board check, with sounds through the configured effects volume. Do not rebuild the removed check lever/flap.

The manual handoff must be ordered and reproducible, with a fresh save and interrupted-run steps. It must distinguish AI source/build/regression findings from user-only checks for Unity import, Play Mode, screenshot/rendered motion, scene navigation, device/touch, performance, feel, and listening. No compile or static validator may be described as a rendered visual/audio pass. Tickets remain Planned at authoring; after actual implementation/internal verification use 'Implemented — internally verified; user manual pending', and only mark User accepted after the user's supplied result. Update this initiative's implementation facts and outstanding issues without changing historical feature documentation.

## Internal checks — AI source/build/regressions only

- [ ] Full Delivery A build, targeted progression/save/Level 1 suites, content validators, scene/asset static checks and source audits pass or list precise blockers.
- [ ] Fresh, supported pre-v5, interrupted-write and resumed flows prove one signed owner, preserved settings, no imported old progress, no repeated payout, and no new-campaign Level 2 path before T21.
- [ ] Documentation lists exact commands/results, touched-path inventory supplied by implementers, remaining art limitation, reproduction route and next work (T18); no unsupported Unity/manual claim is recorded.

## USER manual acceptance — Unity/device/visual/feel/listen checks

- [ ] User opens the assembled project in Unity, plays the ordered fresh Level 1 path and interrupted Continue path, and confirms all required actions remain reachable.
- [ ] User checks touch targets, text, physical prop placement, transitions, clock/reward timing, guide motion, device performance and sound/effects settings.
- [ ] User verifies Level 2 is visibly pending and cannot launch as a new campaign chapter. All objects and scene wiring must already be authored; acceptance does not include manual assembly.

## Handoff

Next ticket: [T18](T18-level2-campaign-and-inventory.md) begins Delivery B after Delivery A passes its internal gate.

## Non-goals and references

No Level 2 enablement, Level 2 content correction, Level 3–5 implementation, remote publication, or user Git operations. See [SPEC](../SPEC.md), [WORLD_AND_FEEDBACK](../WORLD_AND_FEEDBACK.md), [VERIFICATION](../VERIFICATION.md), and [PROGRESS](../PROGRESS.md).
