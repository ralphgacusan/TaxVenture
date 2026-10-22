# T21 — Verify Delivery B and enable Level 2 for the campaign

Status: Planned. Blocked by: T19, T20.

## Goal

Verify the full Level 2 continuation before enabling its campaign route.

## Visible accomplishment

Before: the new campaign stops at Level 1 while Rosa's scene remains isolated. After: the integrated new campaign continues from completed Level 1 into Rosa's Level 2 review only after T18's save/inventory adaptation, T19's explicit choice and T20's usable stand all work together. Level 2 completion is saved once and returns to Career with accurate story status, trust, coins and best mastery record. The old Level 2 scene and sandbox path remain available for isolated development. Chapter 3 and the computer benefit stay locked/coming soon until corrected Level 3 exists.

## Work

Run the full campaign route from New Game through Level 1, including a corrected quest and accepted case, then intentionally open Career and start Level 2. Verify the menu integration is a single explicit content/readiness change made only after this ticket's integrated checks; before that change, T17's Level 2 pending behavior remains the release contract. Do not grant active story launch from old `passedLevelIds`, score hearts, or mastery replay alone. Use the campaign's supported chapter availability; L2 ready status is not a player choice/reward and is never stored as a mutable player save flag.

Trace Rosa's required review from records through first frozen 12-decision grade, corrections, actual approval, follow-through and the selected ethical path. Verify Level 2 checkpoints at each stable milestone and recover from quit/relaunch, failed save, cutoff/day rollover, interrupted dialogue, wrong submission, choice commit, pending reassurance consequence, and stand placement. Ensure restore never restamps, re-awards, re-applies trust or replays the choice. Check both routine and ethical consequences together: required field service +2 and approved follow-through +4 apply once independently; then apply exactly one selected ethics outcome from T19. Total wallet receives only the 60-coin quest and 100-coin corrected case claims once. Frozen grade is not improved by corrections; low tier after corrections still completes story. Purchases and optional records are not gates.

### Source starting points

- **Existing:** the main menu/career routing (`MenuEntry`, `CareerProgress`, `CareerBoardPresenter`), signed save/reset behavior, Level 2 review/checkpoint and physical desk/case systems.
- **Proposed:** integrated story availability/content readiness and shared campaign result orchestration from T18–T20. No new generic launcher or second save owner should be introduced for this release gate.

Perform source, data, build and regression verification only on the AI side. Confirm T01–T16 remain transitively complete for Delivery A and T18–T20 are wired into actual entry points rather than dormant helpers. Validate physical stand rails and pins, office anchors, bag-to-paper identity, Quax/shop scenes, menu/career cards, all touched text files and asset metadata. Preserve GUIDs, serialized enums and existing scene data. Check T19 actor-turn/personal-note placement timings (0.3/0.45 s) and T20 stand-rail opening (0.35 s), clip settle (0.12 s), movement (0.55 s), plus one-shot post-commit sounds through settings. Review exact configured build/scene lists and verify no new campaign path opens unsupported Level 3–5, evidence-board, meeting or ending content. Record command results and precise limitations; no static result may be reported as an import, Play Mode, audio, feel or device pass.

## Internal checks — AI source/build/regressions only

- [ ] Full Delivery B build, targeted save/clock/inventory/ethics/career suites and content/scene/asset validators pass, or blockers are named exactly.
- [ ] End-to-end tests cover all three Rosa choices and correct/incorrect/resumed paths, one-time rewards, no early approval, source isolation and signed-save compatibility.
- [ ] Level 2 is launchable only in the integrated new campaign after Level 1 story completion; legacy scene remains sandbox-safe, Level 3–5 remain coming soon and computer purchase stays locked.
- [ ] Pack records as-built files and commands, remaining art or source limitations, and an ordered Unity/device checklist. Do not mark user acceptance complete in source-only documentation.

## USER manual acceptance — Unity/device/visual/feel/listen checks

- [ ] User imports/opens the completed project and plays Level 1 into Career and then Level 2 using the new-campaign route; verify the legacy scene is not substituted.
- [ ] Play all Rosa choices, a correction path and a resumed path; inspect stand, physical source papers, bag, stamps, career result and once-only coin/trust changes.
- [ ] Check scene readability, visual/animation rendering, navigation, touch, device performance, clock feel and listening/effects settings. All scenes, bindings, colliders and placements must already be authored by the implementation work.

## Handoff

Next ticket: none; T21 closes this implementation DAG. The USER performs Delivery B manual acceptance and records the results in PROGRESS.md.

## Non-goals and references

No computer benefit, corrected Level 3, evidence board, final meeting, ending, tax-rule changes, remote publication or user Git operations. See [SPEC](../SPEC.md), [WORLD_AND_FEEDBACK](../WORLD_AND_FEEDBACK.md), [VERIFICATION](../VERIFICATION.md), and [PROGRESS](../PROGRESS.md).
