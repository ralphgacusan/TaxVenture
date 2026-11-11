# T22 — Tentative third-person and jump adoption

## Status and readiness

**Tentative / deferred; stored disabled.** User approved preservation and this final optional ticket on 12 November 2026, not reactivation. T01–T21 deliver the first-person baseline; this ticket is not a release requirement. Later explicit user selection is required.

## Priority and relative size/risk

Optional, last. Size L; camera/input/animation/device risk. Reassess against the completed first-person game before adoption.

## Blocked by

[T21](T21-current-phone-performance-and-panel-acceptance.md), later explicit user instruction, accepted character/contact quality and measured phone cost. Missing evidence cannot enable the feature by default.

## Direct dependents

None. Final tentative ticket, after the required first-person release gate.

## Goal and visible accomplishment

If selected, compare a polished third-person outdoor / first-person indoor experience with the accepted first-person baseline, then adopt only on actual user acceptance. Assess optional jumping separately for gameplay value; no required platforming.

## Current behavior and evidence

The Level 1 implementation is preserved behind `mixedCameraEnabled: 0` in `Assets/Resources/Player/ExplorationBody.asset`. The absent-resource/default field path is off too. No body, jump UI or practice curb is created; Space and public jump requests are ignored. First-person sprint and Level 1 smoothed dialogue Elijah do not depend on the flag. Historical internal checks establish retained logic/assets, not native acceptance.

## Existing systems/assets to reuse

- [ExplorationRules.cs](../../../Assets/Scripts/Player/ExplorationRules.cs), [CameraController.cs](../../../Assets/Scripts/Player/CameraController.cs), [FirstPersonCameraController.cs](../../../Assets/Scripts/Player/FirstPersonCameraController.cs).
- [PlayerMovement.cs](../../../Assets/Scripts/Player/PlayerMovement.cs), [ExplorationBody.cs](../../../Assets/Scripts/Player/ExplorationBody.cs), [JumpButton.cs](../../../Assets/Scripts/Mobile/JumpButton.cs), [ExplorationPracticeCurb.cs](../../../Assets/Scripts/Player/ExplorationPracticeCurb.cs).
- [ExplorationBody.asset](../../../Assets/Resources/Player/ExplorationBody.asset), retained smoothed Elijah models, idle/walk sources, authored sprint/jump clips and original GUIDs.
- [Interactor.cs](../../../Assets/Scripts/Player/Interactor.cs), [FirstPersonHands.cs](../../../Assets/Scripts/Player/FirstPersonHands.cs) and existing conversation/hold/guide owners.

## Scope and explicit exclusions

Reinspect the finished baseline and stored Level 1 pilot. Deliberately opt into the resource setting for a review build, then extend bounded room/chapter adoption only if selected and accepted. Simply changing the flag only enables the stored Level 1 pilot; it does not implement Levels 2/3 adoption or restore jump onboarding. No alternate camera/input/save framework, new packages, broad smoothing, stamina/combat or required jump route. No automatic reactivation by T04–T21.

## Player journey and entry/exit

Plaza movement/look/run → optional safe hop or flat alternative → office doorway → task → conversation → correct prior exploration return. Repeat for laundry/art room only after their first-person layouts exist and deliberate adoption is implemented.

## Mechanics and domain logic

One camera writer/player/listener. Stable doorway hysteresis, collision-shortened orbit, player-root reach/occlusion and correct body/hands/carry ownership. Jump is grounded and one press, with safe fall recovery and no item/progress loss. Every required route remains walkable; no animation/arrival grants progress or rewards.

## Visual/environment/interaction specification

Keep latest calibrated scale, derived smoothing/material/UV/rig improvements and original assets. Verify body visibility, framing, target selection and motion at gameplay distances without hiding defects through camera placement.

## Animation and physical contact

Reaccept walk/run cadence, turning, feet, authored jump/landing, clothing and conversation scale. Keep Arthur/client contact requirements intact. Retained muscle curves and offline geometry evidence are not native motion acceptance.

## Audio/dialogue/voice

Reuse footsteps, landing and current audio owners. No new dialogue required merely to enable a camera. If control onboarding returns, use short silent guide hints; retained ordinary dialogue keeps baseline voice requirements.

## Camera/mobile/accessibility

Both landscape orientations; simultaneous move/look/jump pointers, safe areas and ≥48dp effective jump target. No HUD tap-through, motion under holds, inaccessible paper, oscillating door transition or camera clipping. Keep the first-person fallback available until acceptance.

## Data/interface/persistence

Use the existing resource field, no save migration or player-facing toggle required. Keep `run-jump` lesson ID compatibility. Active JSON is sprint-only; any restored jump lesson must preserve completed teaching and independent sprint behavior.

## Failure/cancellation/interruption/reload

Back/settings/focus/pause/task/dialogue/disable/reload cancel owned input safely and restore the valid prior perspective. Carried records stay owned and visible. No stuck button, black frame, duplicated body/camera/listener or saved midair pose.

## Performance/resources

Compare warm phone frame pacing, memory, transitions and skinned-body cost against accepted T21 first-person measurements. Source triangles/targetFrameRate do not establish sustained30 acceptance. Do not waive baseline readability or contact quality to fit the camera.

## Internal checks — AI only

- [ ] Rebuild runtime/Editor and run T03 rules/ownership plus retained asset/animation checks against current sources.
- [ ] Prove flag-off baseline, explicit opt-in boundaries, no save/item/reward changes and clean cancellation/returns.
- [ ] Verify each newly adopted chapter/room deliberately; preserve first-person baseline and defaults.

## USER Unity/device acceptance

- [ ] Explicitly select this tentative ticket before enabling gameplay adoption.
- [ ] Compare FP and hybrid loops, target selection, body/animation/contact and optional jumping; record concrete verdict.
- [ ] Record phone/build/warm performance, touch and interruption evidence; accept or leave stored disabled.

## Completion definition

Selection alone is not acceptance. Mark implemented only after current internal verification, and user accepted only after real appearance/control/device evidence. If rejected or deferred, retain the working first-person release with the feature off.

## Handoff/update requirements

Update T22, index, decisions, coverage, verification, progress and known issues with actual chosen scope, enabled scenes, evidence and remaining blockers. Do not revise earlier baseline acceptance into a fictional third-person pass. No Git or Unity operation by AI under current ownership rules.
