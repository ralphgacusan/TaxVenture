# T04 — Learn from Arthur's physical reviewed-file example

## Status and readiness

**Planned. No implementation performed.** Blocked on listed ticket prerequisites. Acceptance gates remain distinct from source readiness.

## Priority and relative size/risk

P1. Size: **M**, bounded to one fresh implementation context. High: actor/navigation/contact versus saved teaching authority. If an observed prerequisite makes this bound false, record the exact bounded substep/blocker; do not absorb unrelated systems.

## Blocked by

[T03](T03-mixed-camera-exploration-and-mobile-movement.md) External decisions/assets/evidence: Accepted Arthur contact pilot; any exact unproducible contact asset is E-CONTACT.

## Direct dependents

[T05](T05-living-community-and-profile-round.md), [T09](T09-arthur-toolkit-and-income-activity.md), [T13](T13-bea-calculation-and-arthur-desk-examples.md), [T17](T17-level1-complete-dialogue-voice-coverage.md)

## Goal and visible accomplishment

Arthur joins the introductory route, physically demonstrates his separate reviewed file, then the player performs the real read/return actions before the independent rounds.

## Current behavior and evidence

ArthurOpening already saves actual file pickup/inspection/return/page-revisit gates; it does not grant tax rewards. Existing guide and locomotion are reusable, but final embodied contact/lead-follow presentation is not established. Read [CURRENT_STATE](../CURRENT_STATE.md), [AUDIT_NOTES](../AUDIT_NOTES.md) and the actual reuse paths below. Existing internal evidence is historical; no static declaration establishes Unity behavior.

## Existing systems/assets to reuse

- [ArthurOpening.cs](../../../Assets/Scripts/SideQuest1/ArthurOpening.cs)
- [ReviewedFileViewer.cs](../../../Assets/Scripts/SideQuest1/ReviewedFileViewer.cs)
- [Level1FieldCheckpointer.cs](../../../Assets/Scripts/SideQuest1/Level1FieldCheckpointer.cs)
- [NpcLocomotion.cs](../../../Assets/Scripts/FSM/NPC/NpcLocomotion.cs)
- [TutorialNpcController.cs](../../../Assets/Scripts/SideQuest1/TutorialNpcController.cs)

## Scope and explicit exclusions

12 November baseline: first-person exploration plus sprint. Keep smoothing, physical NPC contact, task/dialogue views and all case scope. Third-person/jump adoption belongs only to tentative T22 and cannot block this ticket.

One Arthur introductory/board-help sequence with actual body/props/player turn, shortened content and saved existing milestones. Exclude second Arthur/guide/save file, Quax guide, extra graded exercise and Marie answer transfer. Follow [SPEC](../SPEC.md), [approved decisions](../DECISIONS.md) and [asset/contact plan](../ASSET_AND_ANIMATION_PLAN.md); no production execution, Git, Unity launch, paid generation/purchase or actual-save reset is authorized merely by this ticket.

## Player journey and entry/exit

L1-B/C/E. After Bag/Notes/counter, Arthur leads briefly and waits clear; demonstrate one evidence connection on former-client file; release objects; player extracts/reads/returns records and revisits first page; rounds open from actual state.

## Mechanics and domain logic

Reuse existing observation and milestone commands. Navigation arrival/animation/popup Continue cannot complete read or acquisition. Board help points/explains briefly without moving graded cards or rewriting first outcomes. Completed lessons skip; optional help remains unscored/reward-free.

| Situation | Player-visible response | State/commit | Recovery |
|---|---|---|---|
| Valid deliberate action | Neutral handling, then actual result | Existing owner validates candidate and persists before publishing | Settle to committed state |
| Save fails or command refused | Concise actionable retry; no success effect | Previous durable work/ownership unchanged | Source/task stays reachable |
| Duplicate/stale callback | Existing settled result, no second grant | Current action/case/source/revision guards | Ignore stale cosmetic completion |
| Cancel/interrupt/reload | Release only owned input; retain saved work | Never complete from voice/gesture/arrival | Restore canonical state, not animation frame |

## Visual/environment/interaction specification

Actual Arthur position beside teal reviewed file and visible holders; frame body/hand/target together without blocking next action. Same separate sources in world, Bag and reader.

## Animation and physical contact

Accepted reach/grasp/lift/point/place/release and grounded stop/turn/lead/wait; doorway/stuck recovery follows SPEC, never visible teleport. Player and Arthur cannot hold one object simultaneously.

## Audio/dialogue/voice

Ordinary Arthur dialogue voiced later by T17; finalize concise scripts here. Guide/coach popups remain silent. Neutral paper/footstep cues only after real action success; no voiced answer-key tutorial.

## Camera/mobile/accessibility

FP approach→appropriate close view→actual prior FP context; no arm originates from player camera. Accessible tap/drag record handling and prompts, current safe-area ownership.

## Data/interface/persistence

Extend existing Arthur/field checkpoint only if a minimal demo marker is needed; keep example acquisition/source separate from active Marie and independent grade/reward owners. Use verified owners; introduce no speculative public class/API to match this prose.

## Failure/cancellation/interruption/reload

Interrupted demo restores example props only; saved player milestones remain. Failed pickup/inspection save leaves retryable state; help/reload cannot grant or complete case work. See SPEC's shared command boundary and [mode/restore verification](../VERIFICATION.md).

## Performance/resources

Reuse one actor/controller and bounded per-leg route planning; no always-following simulation or duplicate reference mesh sets.

## Internal checks — AI only

- [ ] Existing route tests cover actual pickup/read/return/revisit→rounds; prove wrong/foreign sources and mere animation/arrival cannot satisfy gates.
- [ ] Inject save failure/duplicate callback/cancel/resume at each handoff, verify example never replaces CaseManager active client or grants score/reward.
- [ ] Compile and run affected guide/NPC/field/dialogue and current paper regressions; preserve flexible four-profile order.

## USER Unity/device acceptance

- [ ] Watch full-body/hand contact and exact authority handoff, do the record actions, leave/rejoin midlesson and return after completion.
- [ ] Explore/sprint away, block doorway briefly, use Back/settings/focus and help; no visible teleport, clipped hand, repeated compulsory lesson or automatic client answer.
- [ ] Record actual build/device/steps/evidence for applicable gates **G2, G3, G5, G8** in [VERIFICATION](../VERIFICATION.md); source compilation cannot check this box. AI delivers assets/builders/bindings; user is not assigned manual scene assembly.

## Completion definition

The visible goal works through its real entry/exit with the stated source, presentation, ownership and recovery rules; relevant internal checks pass or precise inherited limitations are recorded. Mark **Implemented, internally verified; user manual pending** only after actual implementation/checks. Mark **User accepted** only on recorded user evidence. Required unresolved asset/content/voice/device gates remain explicit; no placeholder or documentation-only completion.

## Handoff/update requirements

Update this ticket's actual status/checks, [PROGRESS](../PROGRESS.md), [KNOWN_ISSUES](../KNOWN_ISSUES.md) and the new pack's affected contract/inventory/coverage when needed. Record owner/session/date, modified paths, exact commands/results/warnings/fixtures, failures and unperformed native checks, actual supplied acceptance and precise next dependent/blocker. Preserve old initiative histories and existing user work; follow applicable AGENTS and the user's Git/Unity ownership. Scope stays Levels1–3.

