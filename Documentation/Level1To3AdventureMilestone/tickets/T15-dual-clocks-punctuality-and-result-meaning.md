# T15 — Understand clocks, punctuality and earned results

## Status and readiness

**Planned. No implementation performed.** Blocked on listed ticket prerequisites. Acceptance gates remain distinct from source readiness.

## Priority and relative size/risk

P1. Size: **M**, bounded to one fresh implementation context. High: scoring versions, clock pause and frozen history. If an observed prerequisite makes this bound false, record the exact bounded substep/blocker; do not absorb unrelated systems.

## Blocked by

[T06](T06-marie-office-review-and-copy-service.md), [T10](T10-rosa-choice-reconciliation-and-delivery.md), [T14](T14-bea-review-copy-and-supported-closure.md) External decisions/assets/evidence: User clock/result comprehension and cutoff-device acceptance; approved D2 policy is settled.

## Direct dependents

[T16](T16-dialogue-speech-pilot-and-manifest.md), [T20](T20-three-chapter-integration-and-save-continuation.md)

## Goal and visible accomplishment

Players distinguish office time, active review left, learning mastery, punctuality, Public Trust and coins, with one approved50-XP late obligation.

## Current behavior and evidence

L1 uses dynamic270–480s allowance and legacy late fields; L2 uses720s/late3XP, L3 uses960s/late2XP and different pause paths. Current AddPenalty clamps to currently earned XP, so naive early subtraction could lose part of the new obligation. Read [CURRENT_STATE](../CURRENT_STATE.md), [AUDIT_NOTES](../AUDIT_NOTES.md) and the actual reuse paths below. Existing internal evidence is historical; no static declaration establishes Unity behavior.

## Existing systems/assets to reuse

- [OfficeHours.cs](../../../Assets/Scripts/UI/HUD/OfficeHours.cs)
- [ReviewClock.cs](../../../Assets/Scripts/Data/ReviewClock.cs)
- [Level2ReviewClock.cs](../../../Assets/Scripts/Level2/Level2ReviewClock.cs)
- [Level3ReviewClock.cs](../../../Assets/Scripts/Level3/Level3ReviewClock.cs)
- [LevelScoreLedger.cs](../../../Assets/Scripts/RewardsSystem/LevelScoreLedger.cs)
- [MasteryBoundary.cs](../../../Assets/Scripts/RewardsSystem/MasteryBoundary.cs)

## Scope and explicit exclusions

12 November baseline: first-person exploration plus sprint. All other scope remains; third-person/jump adoption is tentative T22 only and does not block this ticket.

Synchronized labeled office/active clocks and paused/warning/cutoff state, approved once-only punctuality replacement, clear gross/net/mastery/Trust/wallet/badge explanation. Exclude new allowance/balance/ending policy, Trust/coin penalty or retrospective regrading. Follow [SPEC](../SPEC.md), [approved decisions](../DECISIONS.md) and [asset/contact plan](../ASSET_AND_ANIMATION_PLAN.md); no production execution, Git, Unity launch, paid generation/purchase or actual-save reset is authorized merely by this ticket.

## Player journey and entry/exit

All main-client timed work and result moments; Bayview stages untimed. See Office time and Active review left during real main-client work; instruction/dialogue pauses are labeled; independent reading runs. First cutoff preserves work/next morning and states one−50run-XP consequence; result explains actual badge and separate totals.

## Mechanics and domain logic

Persist one versioned lateness obligation at first missed main-client cutoff. Net=max(0,gross learning run XP−50 if late), including subsequently earned XP. Replace all existing late-XP deductions for new-policy attempts, no case-cap double charge or later-day repeat. Bayview opening never starts main-client clock. Preserve starts/allowances/voucher180s once/case and current next-morning recovery.

| Situation | Player-visible response | State/commit | Recovery |
|---|---|---|---|
| Valid deliberate action | Neutral handling, then actual result | Existing owner validates candidate and persists before publishing | Settle to committed state |
| Save fails or command refused | Concise actionable retry; no success effect | Previous durable work/ownership unchanged | Source/task stays reachable |
| Duplicate/stale callback | Existing settled result, no second grant | Current action/case/source/revision guards | Ignore stale cosmetic completion |
| Cancel/interrupt/reload | Release only owned input; retain saved work | Never complete from voice/gesture/arrival | Restore canonical state, not animation frame |

## Visual/environment/interaction specification

Reuse existing HUD/result surfaces with explicit labels, readable numbers, paused indication and text/form warnings. Activity/version-specific badge legend: L2 Gold16/Silver14–15; L3 v2 Gold9/Silver7–8; L1 actual current rules; legacy versions retained.

## Animation and physical contact

Only current restrained clock warning/result count/medal cues; no mandatory new character animation. Never use pulse/celebration as a state event.

## Audio/dialogue/voice

Deadline warning/cutoff and badge cues from current families, distinguishable without harsh repetition. Short ordinary cutoff dialogue finalized before recording; labels/system notices remain silent.

## Camera/mobile/accessibility

HUD visibility and clocks work in both exploration modes and appropriate task views without covering targets; settings/cinematic/focus holds own pause, safe-area layout preserved.

## Data/interface/persistence

Small versioned obligation/result separation in current clock/ledger/checkpoint/save owners. Keep frozen first-check/tier, best mastery, recorded old results/late history, corrected story completion, Trust and wallet unchanged. No second score ledger. Use verified owners; introduce no speculative public class/API to match this prose.

## Failure/cancellation/interruption/reload

Persist first miss before rollover; failed write stays closed/retryable with prior work. Re-entry/reload/duplicate cutoff/old missed flag cannot charge again or retroactively impose50. App closed time never advances. See SPEC's shared command boundary and [mode/restore verification](../VERIFICATION.md).

## Performance/resources

Update formatted labels on meaningful time/second/state changes, not per-frame layout/allocation. Existing clocks remain source of truth; no parallel wall clock.

## Internal checks — AI only

- [ ] First cutoff when grossXP=0/10/60, then later100 caseXP; floor, whole50 obligation, repeated days/voucher/warnings/failed rollover and duplicate/reload fixtures.
- [ ] Pause matrix for mandatory instruction/demo/compulsory practice/dialogue/settings/cutscene/suspension versus independent reading/exploration/work; Bayview untimed.
- [ ] Frozen grades/best mastery/legacy versions/Trust/wallet/completion unchanged; compile and affected clock/score/campaign/L2/L3/HUD/career checks.

## USER Unity/device acceptance

- [ ] Observe both clocks/labels/pause while learning, reading independently and crossing cutoff; next morning keeps work and consequence is understandable once.
- [ ] Read each activity/legacy result legend, gross/net versus best mastery, Trust and coins; no cramped HUD, misleading grade or unexpected gate.
- [ ] Record actual build/device/steps/evidence for applicable gates **G3, G7, G8** in [VERIFICATION](../VERIFICATION.md); source compilation cannot check this box. AI delivers assets/builders/bindings; user is not assigned manual scene assembly.

## Completion definition

The visible goal works through its real entry/exit with the stated source, presentation, ownership and recovery rules; relevant internal checks pass or precise inherited limitations are recorded. Mark **Implemented, internally verified; user manual pending** only after actual implementation/checks. Mark **User accepted** only on recorded user evidence. Required unresolved asset/content/voice/device gates remain explicit; no placeholder or documentation-only completion.

## Handoff/update requirements

Update this ticket's actual status/checks, [PROGRESS](../PROGRESS.md), [KNOWN_ISSUES](../KNOWN_ISSUES.md) and the new pack's affected contract/inventory/coverage when needed. Record owner/session/date, modified paths, exact commands/results/warnings/fixtures, failures and unperformed native checks, actual supplied acceptance and precise next dependent/blocker. Preserve old initiative histories and existing user work; follow applicable AGENTS and the user's Git/Unity ownership. Scope stays Levels1–3.

