# T18 — Hear every retained Level 2 dialogue branch

## Status and readiness

**Planned. No implementation performed.** Blocked on listed ticket prerequisites. Acceptance gates remain distinct from source readiness.

## Priority and relative size/risk

P1. Size: **M**, bounded to one fresh implementation context. High: knowing-choice/confirmation and conditional service branches. If an observed prerequisite makes this bound false, record the exact bounded substep/blocker; do not absorb unrelated systems.

## Blocked by

[T07](T07-bayview-level2-classification-stage.md), [T08](T08-rosa-laundry-retrieval-and-kanor-letter.md), [T09](T09-arthur-toolkit-and-income-activity.md), [T10](T10-rosa-choice-reconciliation-and-delivery.md), [T16](T16-dialogue-speech-pilot-and-manifest.md) External decisions/assets/evidence: E-VOICE-L2: revision-matched approved Bayview/Rosa/ambient/choice takes and usable provenance; all Rosa histories listened to.

## Direct dependents

[T20](T20-three-chapter-integration-and-save-continuation.md)

## Goal and visible accomplishment

Bayview and Rosa's complete retained conversations sound coherent, including all knowing-choice outcomes and optional dialogue, with matching subtitles offline.

## Current behavior and evidence

L2 cinematic opening/ending takes and four Rosa histories exist; Bayview gameplay and shortened room/service/choice/ambient turns need real new coverage. Arthur toolkit guide popups are explicitly exempt. Read [CURRENT_STATE](../CURRENT_STATE.md), [AUDIT_NOTES](../AUDIT_NOTES.md) and the actual reuse paths below. Existing internal evidence is historical; no static declaration establishes Unity behavior.

## Existing systems/assets to reuse

- [case_rosa_001.json](../../../Assets/Resources/Levels/Level_02/cases/case_rosa_001.json)
- [plaza_life.json](../../../Assets/Resources/Levels/Level_02/plaza_life.json)
- [town_life.json](../../../Assets/Resources/Levels/Level_02/town_life.json)
- [Level2RosaConsultation.cs](../../../Assets/Scripts/Level2/Level2RosaConsultation.cs)
- [chapter02_end.asset](../../../Assets/Resources/Cutscenes/Voice/chapter02_end.asset)

## Scope and explicit exclusions

12 November baseline: first-person exploration plus sprint. All other scope remains; third-person/jump adoption is tentative T22 only and does not block this ticket.

Complete L2 line/branch inventory, stable IDs, recording export/import/approval and ordinary dialogue voice through T16 seam. Exclude silent toolkit popups/docs/labels, altered ethics consequences and paid recording generation. Follow [SPEC](../SPEC.md), [approved decisions](../DECISIONS.md) and [asset/contact plan](../ASSET_AND_ANIMATION_PLAN.md); no production execution, Git, Unity launch, paid generation/purchase or actual-save reset is authorized merely by this ticket.

## Player journey and entry/exit

All ordinary L2 dialogue, optional talks, Bayview stage and cinematic variants. Bayview reception/correction/retention, laundry encounter and optional residents, actual Rosa commitment/confirmation/cancel/return/approval/explanation/delivery and appropriate cinematic history all speak once in correct context.

## Mechanics and domain logic

Every conditional/insertion/Elijah utterance gets exact approved text/speaker/revision/take; choice UI labels silent but selected spoken dialogue covered. Correctness/choice commands remain independent of playback. Do not infer variant from Trust total.

| Situation | Player-visible response | State/commit | Recovery |
|---|---|---|---|
| Exact approved current line | One matching clip + subtitles | Presentation only | Separate reveal/advance/action rules |
| Missing/stale/unapproved take | Readable text fallback | No approximate matching or success claim | Manifest blocks release coverage |
| Advance/cancel/owner loss | Stop/replace promptly | No domain or choice mutation | Release source/ducking once |
| Settings/focus/mute | Coherent pause/stop | No playback-driven progress | Resume only valid current owner |

## Visual/environment/interaction specification

Existing safe-area dialogue/choice panels/subtitles; no extra modal or legal/provenance metadata in play. Silent guide popup exclusions documented explicitly.

## Animation and physical contact

Keep accepted body/tool/choice gestures; lip-sync not applicable. No voice-end callback moves Rosa, accepts work or hands over copy.

## Audio/dialogue/voice

Use user-supplied no-spend workflow and pilot chapter loading/duck/mix. All retained ordinary optional dialogue voiced; exact unchanged cinematic variants reused. Stale/missing fallback cannot satisfy final coverage.

## Camera/mobile/accessibility

T16 lifecycle across directed choices, office/task/room transitions and actual return owner; no clip overlap or premature control release during confirmation.

## Data/interface/persistence

T16 metadata at live JSON/builders and inserted lines only, existing choice/history/variant authority untouched; no new save or reward field. Use verified owners; introduce no speculative public class/API to match this prose.

## Failure/cancellation/interruption/reload

Cancel/replace/late callback, repeated choice UI, settings/focus/reload stop/restore audio without changing commitment history or replaying grants. Wrong/missing variant stays text fallback and a release blocker. See SPEC's shared command boundary and [mode/restore verification](../VERIFICATION.md).

## Performance/resources

Measured L2 clip memory/import/transition ceilings from pilot; only current chapter/variant references, no preload of all Rosa branches into active playback.

## Internal checks — AI only

- [ ] Coverage includes Bayview and all ordinary Rosa responsible/unsupported-confirmation/dismissive/return/service/ambient branches, exact variant and revision.
- [ ] Choice/cancel/insert/awaited handover audio lifecycle and no history/grant mutation; manifest missing/stale/foreign exclusions fail release.
- [ ] Compile changed paths and selected dialogue/L2/ethics/chapter voice/route/reference checks; preserve current tax figures and timed policy.

## USER Unity/device acceptance

- [ ] Listen to each Rosa history and confirmation/cancel path plus Bayview/optional residents, with exact subtitle and coherent emotional delivery.
- [ ] Offline rapid taps/Back/settings/focus/room→office→cinematic transitions; voices/FX controls independent and no duplicate speech or incorrect history.
- [ ] Record actual build/device/steps/evidence for applicable gates **G6, G8, G10** in [VERIFICATION](../VERIFICATION.md); source compilation cannot check this box. AI delivers assets/builders/bindings; user is not assigned manual scene assembly.

## Completion definition

The visible goal works through its real entry/exit with the stated source, presentation, ownership and recovery rules; relevant internal checks pass or precise inherited limitations are recorded. Mark **Implemented, internally verified; user manual pending** only after actual implementation/checks. Mark **User accepted** only on recorded user evidence. Required unresolved asset/content/voice/device gates remain explicit; no placeholder or documentation-only completion.

## Handoff/update requirements

Update this ticket's actual status/checks, [PROGRESS](../PROGRESS.md), [KNOWN_ISSUES](../KNOWN_ISSUES.md) and the new pack's affected contract/inventory/coverage when needed. Record owner/session/date, modified paths, exact commands/results/warnings/fixtures, failures and unperformed native checks, actual supplied acceptance and precise next dependent/blocker. Preserve old initiative histories and existing user work; follow applicable AGENTS and the user's Git/Unity ownership. Scope stays Levels1–3.

