# T17 — Hear every retained Level 1 dialogue branch

## Status and readiness

**Planned. No implementation performed.** Blocked on listed ticket prerequisites. Acceptance gates remain distinct from source readiness.

## Priority and relative size/risk

P1. Size: **M**, bounded to one fresh implementation context. Medium: route/optional/dynamic coverage and asset matching. If an observed prerequisite makes this bound false, record the exact bounded substep/blocker; do not absorb unrelated systems.

## Blocked by

[T04](T04-arthur-reviewed-file-embodied-teaching.md), [T05](T05-living-community-and-profile-round.md), [T06](T06-marie-office-review-and-copy-service.md), [T16](T16-dialogue-speech-pilot-and-manifest.md) External decisions/assets/evidence: E-VOICE-L1: approved revision-matched takes and usable provenance for every retained L1 dialogue, plus listening acceptance.

## Direct dependents

[T20](T20-three-chapter-integration-and-save-continuation.md)

## Goal and visible accomplishment

Every retained L1 dialogue turn, including optional conversations and Elijah, has a matching approved offline take after its script is stable.

## Current behavior and evidence

Existing original intro has nine recordings/eleven caption groups; chapter1 ending has two catalogued takes. Required/optional gameplay dialogues and new Kanor/Adrian turns are not voiced merely because these recordings exist. Read [CURRENT_STATE](../CURRENT_STATE.md), [AUDIT_NOTES](../AUDIT_NOTES.md) and the actual reuse paths below. Existing internal evidence is historical; no static declaration establishes Unity behavior.

## Existing systems/assets to reuse

- [town.json](../../../Assets/Resources/Levels/Level_01/town.json)
- [plaza_life.json](../../../Assets/Resources/Levels/Level_01/plaza_life.json)
- [case_001.json](../../../Assets/Resources/Levels/Level_01/cases/case_001.json)
- [DialogueSession.cs](../../../Assets/Scripts/UI/DialogueSession.cs)
- [cutscene01_subtitles.json](../../../Assets/Resources/Cutscenes/cutscene01_subtitles.json)

## Scope and explicit exclusions

12 November baseline: first-person exploration plus sprint. All other scope remains; third-person/jump adoption is tentative T22 only and does not block this ticket.

Route-aware L1 line manifest/recording sheet, stable IDs at real JSON/builders, approved takes/import/matching, all mandatory/optional/conditional branches and listening list. Exclude rerecording unchanged compatible intro without cause, voice-all-documents, guide-popup speech or paid generation. Follow [SPEC](../SPEC.md), [approved decisions](../DECISIONS.md) and [asset/contact plan](../ASSET_AND_ANIMATION_PLAN.md); no production execution, Git, Unity launch, paid generation/purchase or actual-save reset is authorized merely by this ticket.

## Player journey and entry/exit

All ordinary L1 dialogue and original/new cinematic speech. Fresh control/Arthur/profile/Marie route and optional community/consultation/review returns speak naturally; original intro and ending keep their actual owning timelines/routes. Unselected choice/menu labels are silent; any selected utterance authored as dialogue is voiced.

## Mechanics and domain logic

Enumerate actual live callers, not grep counts; each retained line maps to exact text/speaker/revision/take and branch. Finite inserted/conditional returns are included. Required clues/emotion remain after shortening; source-only/retired lines excluded with evidence.

| Situation | Player-visible response | State/commit | Recovery |
|---|---|---|---|
| Exact approved current line | One matching clip + subtitles | Presentation only | Separate reveal/advance/action rules |
| Missing/stale/unapproved take | Readable text fallback | No approximate matching or success claim | Manifest blocks release coverage |
| Advance/cancel/owner loss | Stop/replace promptly | No domain or choice mutation | Release source/ducking once |
| Settings/focus/mute | Coherent pause/stop | No playback-driven progress | Resume only valid current owner |

## Visual/environment/interaction specification

Current concise subtitle layout and character/station presentation unchanged; no new UI. Coverage manifest lists exclusions by route with guide/system/document reason, never ordinary dialogue relabeled to evade coverage.

## Animation and physical contact

Existing accepted talk/listen/physical gestures retained. New lip-sync/gesture system not applicable to this recording-coverage slice.

## Audio/dialogue/voice

User-supplied no-spend prerecorded workflow; match existing chosen voices where known without inventing provider IDs. Missing/stale fallback remains robust but release coverage fails until all retained ordinary lines have approved takes. Existing intro/cinematic clips only reused when exact.

## Camera/mobile/accessibility

Reuse T16 lifecycle and current directed shots, tap reveal/advance/awaited action semantics; no clip-duration camera or progression trigger.

## Data/interface/persistence

Stable line IDs/revision and local catalogue through T16 seam and existing builders/snapshots/inserts; no new save fields or rewards required. Use verified owners; introduce no speculative public class/API to match this prose.

## Failure/cancellation/interruption/reload

Text revision invalidates stale take visibly in development manifest, not a silent approximate match. Interruptions/owner loss clean through T16; rerun branch never repeats task/reward. See SPEC's shared command boundary and [mode/restore verification](../VERIFICATION.md).

## Performance/resources

Chapter-bounded clip imports/loading and pilot compression/memory ceilings; measure all L1 dialogue set and original intro transitions, avoid eager all-level preload.

## Internal checks — AI only

- [ ] Manifest covers every reachable required/optional Elijah/NPC/ordinary-Arthur route and inserted result; no duplicate IDs, missing/foreign/stale takes or unsupported exclusions.
- [ ] Validate exact text/speaker/revision/file/approval/provenance and updated source references; test fallback and isolated replay/intro interaction.
- [ ] Compile changed line/data/import paths and selected dialogue/guide/intro/chapter/campaign/reference checks; no broad gameplay suite for pure clip batches.

## USER Unity/device acceptance

- [ ] Listen to every retained L1 line/branch, including optional community and post-return consultations; names/terms/tone/subtitles/mix match.
- [ ] Play offline fresh/replay, quick reveal/advance/cancel/Back/settings/focus and intro/end handoffs; no missing speech, duplicate line or memory spike beyond pilot.
- [ ] Record actual build/device/steps/evidence for applicable gates **G6, G8, G10** in [VERIFICATION](../VERIFICATION.md); source compilation cannot check this box. AI delivers assets/builders/bindings; user is not assigned manual scene assembly.

## Completion definition

The visible goal works through its real entry/exit with the stated source, presentation, ownership and recovery rules; relevant internal checks pass or precise inherited limitations are recorded. Mark **Implemented, internally verified; user manual pending** only after actual implementation/checks. Mark **User accepted** only on recorded user evidence. Required unresolved asset/content/voice/device gates remain explicit; no placeholder or documentation-only completion.

## Handoff/update requirements

Update this ticket's actual status/checks, [PROGRESS](../PROGRESS.md), [KNOWN_ISSUES](../KNOWN_ISSUES.md) and the new pack's affected contract/inventory/coverage when needed. Record owner/session/date, modified paths, exact commands/results/warnings/fixtures, failures and unperformed native checks, actual supplied acceptance and precise next dependent/blocker. Preserve old initiative histories and existing user work; follow applicable AGENTS and the user's Git/Unity ownership. Scope stays Levels1–3.

