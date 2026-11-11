# T21 — Accept the panel candidate on the current phone

## Status and readiness

**Planned. No implementation performed.** Blocked on listed ticket prerequisites. Acceptance gates remain distinct from source readiness.

## Priority and relative size/risk

P1. Size: **M**, bounded to one fresh implementation context. High: sustained frame pacing, memory/thermal and final acceptance. If an observed prerequisite makes this bound false, record the exact bounded substep/blocker; do not absorb unrelated systems.

## Blocked by

[T20](T20-three-chapter-integration-and-save-continuation.md) External decisions/assets/evidence: E-DEVICE, E-PILOT, E-CONTACT, E-VOICE-L1/L2/L3, E-CONTENT and all user gates must be resolved with actual evidence.

## Direct dependents

[T22](T22-tentative-third-person-and-jump-adoption.md) — tentative only. T21 remains the release gate for the first-person baseline and does not require T22.

## Goal and visible accomplishment

A specific tested candidate meets all documented experience gates and measured sustained30-FPS performance on the user's actual baseline phone.

## Current behavior and evidence

Current mobile governor requests60 then reduces resolution/steps to30 and ignores large hitches in sampling. No exact baseline phone or new milestone measurements exist; source geometry/desktop compilation cannot establish device acceptance. Read [CURRENT_STATE](../CURRENT_STATE.md), [AUDIT_NOTES](../AUDIT_NOTES.md) and the actual reuse paths below. Existing internal evidence is historical; no static declaration establishes Unity behavior.

## Existing systems/assets to reuse

- [MobilePerformance.cs](../../../Assets/Scripts/Core/MobilePerformance.cs)
- [AndroidPerformanceSetup.cs](../../../Assets/Editor/AndroidPerformanceSetup.cs)
- [AndroidStaticBatchingBuildStep.cs](../../../Assets/Editor/AndroidStaticBatchingBuildStep.cs)
- [ProjectSettings.asset](../../../ProjectSettings/ProjectSettings.asset)
- [QualitySettings.asset](../../../ProjectSettings/QualitySettings.asset)
- [AudioManager.cs](../../../Assets/Scripts/AudioManager/AudioManager.cs)

## Scope and explicit exclusions

12 November baseline: first-person exploration plus sprint. Keep smoothing, physical NPC contact, task/dialogue views and all case scope. Third-person/jump adoption belongs only to tentative T22 and cannot block this ticket.

Measure baseline/candidate, fix bounded observed performance/integration defects with existing owners, record all release gates and handoff. Exclude new optimization/streaming framework, feature omissions as polish, unsupported hardware guarantees, publication and L4/5. Follow [SPEC](../SPEC.md), [approved decisions](../DECISIONS.md) and [asset/contact plan](../ASSET_AND_ANIMATION_PLAN.md); no production execution, Git, Unity launch, paid generation/purchase or actual-save reset is authorized merely by this ticket.

## Player journey and entry/exit

Complete current-phone candidate through Levels1–3, interruption and offline paths. On named current phone/build, warm up and repeat representative exploration/rooms/NPCs/papers/Arthur/machine/calculator/speech/transitions, then complete all chapters and resume/mode/offline tests. Player sees evenly paced responsive30-FPS target with readable interactions.

## Mechanics and domain logic

Use measured per-stage budgets from T02, identify CPU/GPU/skin/shadow/visibility/audio/UI/alloc/path bottleneck, choose smallest evidence-backed fix and repeat affected route. TargetFrameRate flag alone cannot pass. One-week target does not overrule failed/missing gates.

| Situation | Player-visible response | State/commit | Recovery |
|---|---|---|---|
| Evidence missing | Candidate remains unaccepted | No invented pass | Name gate and required evidence |
| Measured regression | Record actual reproduction | Bounded fix through existing owner | Rebuild/recheck affected route |
| Gates all accepted | Documented panel candidate | Actual status evidence only | Preserve known limitations/next release scope |

## Visual/environment/interaction specification

Inspect accepted style at actual gameplay distances, readable silhouettes/papers/clock/results and restrained effects; lower cost through sharing/LOD/visibility/material/light before sacrificing required readability/contact.

## Animation and physical contact

Verify bounded active/skinned animators, culling/turn/stop/contact and no offscreen full simulation; no new animation feature assigned. Missing earlier quality remains its owning ticket unresolved.

## Audio/dialogue/voice

Offline full dialogue/settings/mix and measured chapter loading/audio memory, no clipped/cancelled mismatch or leak through repeated scene transitions. Missing approved takes cannot be waived silently.

## Camera/mobile/accessibility

Both current landscape orientations, safe areas/thumb regions, first-person look/sprint/doors/returns/Back/focus/app suspension/input latency. No duplicate listeners/cameras or unreachable targets.

## Data/interface/persistence

No new domain/save changes required by measurement itself. Any bounded fix uses current owners and relevant failure/mode checks; no real-save reset or test data publication. Use verified owners; introduce no speculative public class/API to match this prose.

## Failure/cancellation/interruption/reload

Long session/thermal pressure, background/resume, failed transition and repeated retry leave work/possessions/claims exact. Any crash/stall/stuck hold is recorded with reproduction/build/device, fixed and affected checks repeated. See SPEC's shared command boundary and [mode/restore verification](../VERIFICATION.md).

## Performance/resources

Record model/Android/RAM/build/settings, warm conditions, frame distribution/stalls/CPU-GPU/memory/input/transition latency, active character/mesh/material/texture/audio/UI costs.33.3ms is30-FPS frame budget, not a tolerance guarantee; explain misses and acceptance rationale.

## Internal checks — AI only

- [ ] Analyze actual supplied device measurements and owner/resource lifetime evidence; compile changed optimization paths and run only behavior/regressions affected by bounded fixes.
- [ ] All G1–G10 and coverage rows have traceable accepted evidence, no copied historical test count or source-only native pass.
- [ ] Validate final ticket/status/blocker/docs consistency and changed path handoff; no unresolved required feature or mandatory purchase/clue gate.

## USER Unity/device acceptance

- [ ] User performs named-phone sustained/warmed and complete-route frame/input/memory/transition/audio tests in both landscapes and all VERIFICATION checklist routes.
- [ ] Record explicit accept/reject for G1–G10, content approval, voices/contact/style and interruption reliability; panel-ready only when all actual evidence passes.
- [ ] Record actual build/device/steps/evidence for applicable gates **G1, G2, G3, G4, G5, G6, G7, G8, G9, G10** in [VERIFICATION](../VERIFICATION.md); source compilation cannot check this box. AI delivers assets/builders/bindings; user is not assigned manual scene assembly.

## Completion definition

The visible goal works through its real entry/exit with the stated source, presentation, ownership and recovery rules; relevant internal checks pass or precise inherited limitations are recorded. Mark **Implemented, internally verified; user manual pending** only after actual implementation/checks. Mark **User accepted** only on recorded user evidence. Required unresolved asset/content/voice/device gates remain explicit; no placeholder or documentation-only completion.

## Handoff/update requirements

Update this ticket's actual status/checks, [PROGRESS](../PROGRESS.md), [KNOWN_ISSUES](../KNOWN_ISSUES.md) and the new pack's affected contract/inventory/coverage when needed. Record owner/session/date, modified paths, exact commands/results/warnings/fixtures, failures and unperformed native checks, actual supplied acceptance and precise next dependent/blocker. Preserve old initiative histories and existing user work; follow applicable AGENTS and the user's Git/Unity ownership. Scope stays Levels1–3.

