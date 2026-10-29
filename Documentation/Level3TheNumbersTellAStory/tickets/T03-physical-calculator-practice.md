# T03 — Operate the physical calculator and attach an unscored practice tape

Status: **Planned**. Blocked by: T02.

Priority: P1. Phase: A — New tool introduction. Estimated scope: M; target one fresh agent context. If unexpected shared refactoring is required, document and split it before broadening this ticket.

## Goal and visible accomplishment

At the actual desk the player selects values, performs subtraction and attaches persistent calculation tape using the same tool later used for tax work.

This is local Level3 numbering. Read [AGENTS](../AGENTS.md), [SPEC](../SPEC.md), [CONTENT](../CONTENT.md), [source map](../REUSE_AND_ARCHITECTURE.md), [presentation](../WORLD_AND_FEEDBACK.md), [rewards](../REWARDS_AND_VALIDATION.md), [verification](../VERIFICATION.md) and this ticket before execution. Current request is documentation only; future implementation requires an implementation instruction.

## Existing systems to reuse

Verified existing paths relative to repository root; recheck live behavior rather than rely on stale comments:

- `Assets/Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Calculator_01.prefab`
- `Assets/Scripts/UI/Computer/ComputerUI.cs`
- `Assets/Scripts/Player/CameraController.cs`
- `Assets/Scripts/UI/WorkstationUI.cs`
- `Assets/Scripts/UI/HUD/HudWindowLock.cs`
- `Assets/Scripts/UI/Guide/GuideTargets.cs`
- `Assets/Scripts/AudioManager/AudioManager.cs`

New L3 components/resources mentioned below are proposed outputs, not verified existing APIs. Prefer existing owners/adapters and level configuration; do not copy global frameworks.

## Work and interaction mechanics

1. Reuse supplied calculator prop; add local readable focus/display/key hit targets and a real 3D paper tape dock. Fit source mesh bounds to consultant desk; do not edit source prefab or use a flat minigame as its world replacement.
2. Add bounded pure arithmetic/input/tape state and presentation under the L3 owner. Support digits/decimal, clear-entry, backspace, +/−/×/÷, deterministic percent, Equals, source selection and Attach result. Tap-release enters once; malformed/overflow/divide-by-zero reports input issue without mastery/trust effect.
3. Existing guide teaches unrelated budget100−20=80, then80−8=72. Clearly separate sample from campaign coins/tax rules. Use same live calculator/tape controller for practice and later work; no fake animation-only tutorial. Stop at actual player actions, not page advancement.
4. Equals produces a draft source-linked expression/result; Attach names the destination before confirmation. Saved practice progress/tapes resume without animation/reward replay. Manual numeric entry remains possible; provenance for later case use is implemented through the source-link seam, not forced guessing.
5. Use existing workstation/guide/window lock and pause predicates. Back/Exit Desk/settings/focus loss/disable settle held input and release only owned holds. Tutorial may pause review; independent later calculator work does not receive an unlimited hidden pause. Complete practice advances Bea objective, no score or wallet/trust change.

## Assets, motion and feedback

Existing calculator; new bounded tape mesh/body/display and hit targets. Proposed press0.06–0.1s, tape feed0.15–0.25s, attach0.2–0.35s. Reuse suitable resolved neutral library cue, not an invented dedicated calculator sound.

Follow WORLD_AND_FEEDBACK and fixed document types. Bind every new world object to actual 3D body, collider/target, correct source/record identity and existing camera/input/audio ownership. Record resolved anchors/clip names and any unsupported asset assumption. No user scene-assembly chores or claims of rendered acceptance from declarations.

## Integration requirements

Do not globally route existing L1 ComputerUI.Show clearing behavior into this persistent state. Keep new capability opted into L3; old computer/guide routes keep their behavior. Source keys/answer values are not embedded in tool UI.

Preserve current L1/L2 scenes, source prefabs/GUIDs, signed save owner, mode guards, input/FSM/dialogue/audio frameworks and Android controls. Normal L3 Career remains unavailable until T18. No L4 filing, ending, stand, drawer or global archive feature is added.

## Persistence and failure rules

Use the existing candidate→save→publish command boundary and stable chapter/case/action IDs. Validate actual prerequisites/revision/source before committing. Failed write leaves prior state, source/copy and retry available, with no success effect. Quiet restore does not raise acquisition/reward/choice/consumption events. Back, repeated input, disable and app focus interruption settle pointer state and release only owned holds. Story/replay/practice/Editor distinctions protect live campaign data.

## Internal checks — AI source/build/regressions only

- [ ] Real key/source→expression→Equals→attached tape path gives 80 and72; no wallet/XP/trust/first-check mutation.
- [ ] Percent/arithmetic bounds, repeated tap/held key, manual input and malformed operation have deterministic tests.
- [ ] Exit/reopen/save/reload preserve intended practice state without duplicated tape or held pointer.
- [ ] Modal/camera/audio ownership cleans up on cancel/disable/settings and advances only after actual player work.
- [ ] Run targeted behavioral/serialization/failure checks and affected existing regressions named in VERIFICATION for the actual changes. Build changed production paths before tests that load their DLL. Record exact commands/results/warnings; no Unity/editor launch or assumed pass count.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] User checks real prop scale/depth, screen/key/tape readability, tap targets, responsiveness, SFX settings, camera clipping and Back/Exit feel.
- [ ] Inspect applicable actual imported references, 3D bodies, paper text, targets, motion, camera/control recovery and Android layout. Manual acceptance remains pending until the user reports it; static tests are not a substitute.

## Handoff and completion definition

Free shared L3 calculator/tape interaction and completed protected introduction. Next T04.

Direct dependents: T04, T06. Record actual changed source/content/assets, internal evidence, preserved baseline limitations and remaining user checks in [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md). Mark implemented/internal-verified only after real entry points and checks work; Git operations and user acceptance remain user-owned. No commits, scene edits or gameplay implementation are performed by authoring this ticket.
