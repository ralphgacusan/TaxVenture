# T05 — Reconstruct a reversible source-linked worksheet at Bea’s table

Status: **Planned**. Blocked by: T04.

Priority: P1. Phase: B — Side-quest drafting. Estimated scope: M; target one fresh agent context. If unexpected shared refactoring is required, document and split it before broadening this ticket.

## Goal and visible accomplishment

Players lift seven working strips from originals, place them into four sections and explain two questionable claims without answer-revealing snapping.

This is local Level3 numbering. Read [AGENTS](../AGENTS.md), [SPEC](../SPEC.md), [CONTENT](../CONTENT.md), [source map](../REUSE_AND_ARCHITECTURE.md), [presentation](../WORLD_AND_FEEDBACK.md), [rewards](../REWARDS_AND_VALIDATION.md), [verification](../VERIFICATION.md) and this ticket before execution. Current request is documentation only; future implementation requires an implementation instruction.

## Existing systems to reuse

Verified existing paths relative to repository root; recheck live behavior rather than rely on stale comments:

- `Assets/Scripts/Level2/IncomeToolkit.cs`
- `Assets/Scripts/Level2/AnnotationPaper.cs`
- `Assets/Scripts/UI/Books/PageTurnStage.cs`
- `Assets/Scripts/SideQuest1/ClientRecordPaper.cs`
- `Assets/Scripts/UI/WorkstationUI.cs`

New L3 components/resources mentioned below are proposed outputs, not verified existing APIs. Prefer existing owners/adapters and level configuration; do not copy global frameworks.

## Work and interaction mechanics

1. Reuse existing workstation/physical record handling and build L3-specific worksheet behavior, not a cloned Rosa annotation game. Left source/current center worksheet/right tool dock with narrow-screen paging; physical tray/strip/claim/tape bodies and labels follow WORLD_AND_FEEDBACK.
2. Source row→Add to worksheet lifts a strip retaining row/source ID and printed amount. Re-extraction focuses existing strip. R5 annual printing and R7 individual invoice have different source IDs/shared PRINT-26, so mistaken double counting stays possible while infinite technical copies do not.
3. Implement Income, Allowable expenses, Not deductible and Supporting only. Accept all plausible wrong destinations neutrally. Tap-place and optional broad-target drag have identical semantics; off-target returns safely, Undo last move works, no draft action changes mastery/trust.
4. Two claim notes require personal-purpose and already-represented reasons plus R7 related-entry selection. Keep fields accessible regardless of wrong destination; offer neutral reason candidates and any eligible related row until Check. A wrong placement must neither hide required fields nor expose the correct answer.
5. Each edit updates saved draft/source links, current selected row/section and appropriate subtotal-dirty flags through the existing checkpoint. Exit/reopen/back/disable settle held strips without erasing placements. Maintain single input/camera owner, still reading and bounded layout updates. Grading and rewards remain disabled until T06’s complete Check path.

## Assets, motion and feedback

New worksheet/strip/claim/relationship-clip assemblies using existing physical paper geometry/material/printing. No asset-only delivery: real extraction/place/undo/source-open entry points must work.

Follow WORLD_AND_FEEDBACK and fixed document types. Bind every new world object to actual 3D body, collider/target, correct source/record identity and existing camera/input/audio ownership. Record resolved anchors/clip names and any unsupported asset assumption. No user scene-assembly chores or claims of rendered acceptance from declarations.

## Integration requirements

T03 calculator will fill subtotal fields in T06. This ticket supplies the same persisted draft the grader consumes; no private presentation-only choices or hidden answer keys in Bag. Keep L2 toolkit behavior unchanged.

Preserve current L1/L2 scenes, source prefabs/GUIDs, signed save owner, mode guards, input/FSM/dialogue/audio frameworks and Android controls. Normal L3 Career remains unavailable until T18. No L4 filing, ending, stand, drawer or global archive feature is added.

## Persistence and failure rules

Use the existing candidate→save→publish command boundary and stable chapter/case/action IDs. Validate actual prerequisites/revision/source before committing. Failed write leaves prior state, source/copy and retry available, with no success effect. Quiet restore does not raise acquisition/reward/choice/consumption events. Back, repeated input, disable and app focus interruption settle pointer state and release only owned holds. Story/replay/practice/Editor distinctions protect live campaign data.

## Internal checks — AI source/build/regressions only

- [ ] All7 rows extract once, wrong plausible trays accept, original sources stay unchanged, R7/R5 remain distinct.
- [ ] Tap-place/drag/Undo/source tabs change the same saved model; foreign-case sources and mechanical invalidity give explicit safe refusal.
- [ ] Both reasons and related link remain reachable on every wrong destination; no per-drop correctness leak.
- [ ] Exit/reentry/reload/failed draft commit restore stable placements and release holds, no mastery/reward events.
- [ ] Run targeted behavioral/serialization/failure checks and affected existing regressions named in VERIFICATION for the actual changes. Build changed production paths before tests that load their DLL. Record exact commands/results/warnings; no Unity/editor launch or assumed pass count.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] User checks section paging/readability, finger offset/occlusion, broad targets, neutral cues, paper depth/motion, no pixel-precision gate or camera clipping.
- [ ] Inspect applicable actual imported references, 3D bodies, paper text, targets, motion, camera/control recovery and Android layout. Manual acceptance remains pending until the user reports it; static tests are not a substitute.

## Handoff and completion definition

Complete editable physical worksheet with source/claim relationships. Next T06.

Direct dependents: T06. Record actual changed source/content/assets, internal evidence, preserved baseline limitations and remaining user checks in [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md). Mark implemented/internal-verified only after real entry points and checks work; Git operations and user acceptance remain user-owned. No commits, scene edits or gameplay implementation are performed by authoring this ticket.
