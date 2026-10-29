# T17 — Polish the complete physical chapter flow and mobile presentation

Status: **Planned**. Blocked by: T13, T14, T15, T16.

Priority: P1. Phase: F — Integrated polish. Estimated scope: M; scoped fixes only; target one fresh agent context. If unexpected shared refactoring is required, document and split it before broadening this ticket.

## Goal and visible accomplishment

The implemented chapter feels like a continuous client-service adventure with readable paper interactions, phase-correct dialogue and consistent audio/input ownership.

This is local Level3 numbering. Read [AGENTS](../AGENTS.md), [SPEC](../SPEC.md), [CONTENT](../CONTENT.md), [source map](../REUSE_AND_ARCHITECTURE.md), [presentation](../WORLD_AND_FEEDBACK.md), [rewards](../REWARDS_AND_VALIDATION.md), [verification](../VERIFICATION.md) and this ticket before execution. Current request is documentation only; future implementation requires an implementation instruction.

## Existing systems to reuse

Verified existing paths relative to repository root; recheck live behavior rather than rely on stale comments:

- `Assets/Scripts/UI/WorkstationUI.cs`
- `Assets/Scripts/UI/HUD/HudWindowLock.cs`
- `Assets/Scripts/Player/CameraController.cs`
- `Assets/Scripts/UI/DialogueUI.cs`
- `Assets/Scripts/FSM/NPC/NpcLife.cs`
- `Assets/Scripts/AudioManager/AudioEnvironment.cs`
- `Assets/Scripts/UI/Books/PageTurnStage.cs`

New L3 components/resources mentioned below are proposed outputs, not verified existing APIs. Prefer existing owners/adapters and level configuration; do not copy global frameworks.

## Work and interaction mechanics

1. Trace actual start→practice→Bea records→worksheet→computation→review→delivery→company→closing→Career/optional office route. Remove repeated first-day movement/shop/reference instructions, false old objectives, stale client names and any unimplemented completion promise. Do not defer missing gameplay here as vague polish.
2. Check every first/repeat/resume dialogue/guide marker against committed phase and history. Prioritize actionable clue, review reaction, then small talk. Keep flexible inquiry, two equal service plans, limited journal finding and one actor owner; no extra ethical branch.
3. Tune existing physical motions/focus/typography/paper depth/source tabs/calculator keys/tape/claim links/stamp/copy to WORLD_AND_FEEDBACK. All effects remain presentation after their domain commit; instant settle/Back cannot skip actions or award. Support narrow/tall/wide screens without tiny all-at-once layout.
4. Audit input/joystick/camera/Back/app-focus/disable/scene-transition ownership through nested book/dialogue/shop/result states. Prevent tap-through, early look release, stuck held strip and repeated sound. Reuse AudioLibrary/AudioEnvironment/settings; rate-limit cues, no direct SFX bypass.
5. Optional Bea keepsake may use the existing collect/read pattern with neutral artist note and no reward/essential clue; omit if it would expand scope or unresolved art. All core assets/assembly are AI work. Record actual user-measured timing/frame-time/touch/render/audio acceptance separately; do not invent performance success.

## Assets, motion and feedback

No new asset family. Tune existing authored3D assemblies; optional small card uses supplied/procedural print and existing keepsake behavior. Missing rigs/audio documented with practical existing fallback.

Follow WORLD_AND_FEEDBACK and fixed document types. Bind every new world object to actual 3D body, collider/target, correct source/record identity and existing camera/input/audio ownership. Record resolved anchors/clip names and any unsupported asset assumption. No user scene-assembly chores or claims of rendered acceptance from declarations.

## Integration requirements

No reward/ethics/economy/tax changes or global renderer fix hidden in polish. Shared UI fixes are narrowly adopted and regression-tested in L1/L2. Initial24–30minute chapter/16+10active budgets are tuning targets, not measured evidence.

Preserve current L1/L2 scenes, source prefabs/GUIDs, signed save owner, mode guards, input/FSM/dialogue/audio frameworks and Android controls. Normal L3 Career remains unavailable until T18. No L4 filing, ending, stand, drawer or global archive feature is added.

## Persistence and failure rules

Use the existing candidate→save→publish command boundary and stable chapter/case/action IDs. Validate actual prerequisites/revision/source before committing. Failed write leaves prior state, source/copy and retry available, with no success effect. Quiet restore does not raise acquisition/reward/choice/consumption events. Back, repeated input, disable and app focus interruption settle pointer state and release only owned holds. Story/replay/practice/Editor distinctions protect live campaign data.

## Internal checks — AI source/build/regressions only

- [ ] All required phases/targets/content paths bind; stale inherited quests/tutorials/reward/case names absent from active route.
- [ ] Nested input/camera/coroutine cancellation and event lifetime assertions cover actual ownership seams.
- [ ] Only actual saved operations emit bounded audio/emote/coin/heart effects, honoring settings.
- [ ] Known issues distinguish fixed source problems from user render/device/manual gaps; no placeholder art called final.
- [ ] Run targeted behavioral/serialization/failure checks and affected existing regressions named in VERIFICATION for the actual changes. Build changed production paths before tests that load their DLL. Record exact commands/results/warnings; no Unity/editor launch or assumed pass count.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] User checks complete free and upgraded routes, all choices/corrections, motion/grounding/readability/sound/touch/camera, measured Android performance and actual fun/pacing.
- [ ] Inspect applicable actual imported references, 3D bodies, paper text, targets, motion, camera/control recovery and Android layout. Manual acceptance remains pending until the user reports it; static tests are not a substitute.

## Handoff and completion definition

Integrated polish with truthful internal/user evidence and no unresolved hidden assembly chores. Next T18.

Direct dependents: T18. Record actual changed source/content/assets, internal evidence, preserved baseline limitations and remaining user checks in [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md). Mark implemented/internal-verified only after real entry points and checks work; Git operations and user acceptance remain user-owned. No commits, scene edits or gameplay implementation are performed by authoring this ticket.
