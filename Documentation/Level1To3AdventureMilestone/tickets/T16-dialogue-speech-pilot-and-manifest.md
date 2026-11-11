# T16 — Hear the stable dialogue-session speech pilot

## Status and readiness

**Planned. No implementation performed.** Blocked on listed ticket prerequisites. Acceptance gates remain distinct from source readiness.

## Priority and relative size/risk

P1. Size: **M**, bounded to one fresh implementation context. High: foreground speech lifecycle and dynamic line metadata. If an observed prerequisite makes this bound false, record the exact bounded substep/blocker; do not absorb unrelated systems.

## Blocked by

[T06](T06-marie-office-review-and-copy-service.md), [T15](T15-dual-clocks-punctuality-and-result-meaning.md) External decisions/assets/evidence: Approved exact pilot dialogue takes and usable provenance; E-VOICE-L1. Existing unchanged recordings may verify fixtures only.

## Direct dependents

[T17](T17-level1-complete-dialogue-voice-coverage.md), [T18](T18-level2-complete-dialogue-voice-coverage.md), [T19](T19-level3-complete-dialogue-voice-coverage.md)

## Goal and visible accomplishment

A real stable dialogue plays matching prerecorded speech through existing settings/audio ownership without changing manual pacing, actions or saves.

## Current behavior and evidence

VOICES toggle/volume and cinematic playback/catalogues exist; current DialogueLine/DialogueSession lack recording IDs/metadata and snapshot/insertion copies would need extension. GuideVoice is text-only and exempt. Existing cinematic catalogue does not contain the earlier proposal's general approval/provenance fields. Read [CURRENT_STATE](../CURRENT_STATE.md), [AUDIT_NOTES](../AUDIT_NOTES.md) and the actual reuse paths below. Existing internal evidence is historical; no static declaration establishes Unity behavior.

## Existing systems/assets to reuse

- [DialogueLine.cs](../../../Assets/Scripts/Data/DialogueLine.cs)
- [DialogueSession.cs](../../../Assets/Scripts/UI/DialogueSession.cs)
- [DialogueUI.cs](../../../Assets/Scripts/UI/DialogueUI.cs)
- [AudioManager.cs](../../../Assets/Scripts/AudioManager/AudioManager.cs)
- [ChapterCinematicVoiceCatalogue.cs](../../../Assets/Scripts/Cutscenes/Chapters/ChapterCinematicVoiceCatalogue.cs)
- [ChapterCinematicVoiceRules.cs](../../../Assets/Scripts/Cutscenes/Chapters/ChapterCinematicVoiceRules.cs)

## Scope and explicit exclusions

12 November baseline: first-person exploration plus sprint. All other scope remains; third-person/jump adoption is tentative T22 only and does not block this ticket.

Small real dialogue pilot, stable ID/text-speaker-revision manifest, snapshot/insert preservation, foreground ownership/cancel/pause/duck/offline/load and recording export/import seam. Exclude runtime TTS/provider credentials/paid generation, lip-sync, new dialogue engine or auto-advance. Follow [SPEC](../SPEC.md), [approved decisions](../DECISIONS.md) and [asset/contact plan](../ASSET_AND_ANIMATION_PLAN.md); no production execution, Git, Unity launch, paid generation/purchase or actual-save reset is authorized merely by this ticket.

## Player journey and entry/exit

Stable Marie/Arthur/Elijah ordinary dialogue, plus inserted Auditor response and cancellation. Speak with stable ordinary Arthur/Marie/Elijah lines, reveal text with first tap and separately advance; receive inserted Auditor response, cancel/replace, change volumes/settings/focus and resume correctly. Awaited handover still waits for actual player command.

## Mechanics and domain logic

One foreground speech owner, exact approved text/speaker/revision match, missing/stale/unapproved text fallback. Stop on advance/cancel/owner loss/scene exit before camera cleanup completes; pause/resume coherently and never restart consumed muted line midway. Music/ambience duck/restore through existing owner; voice ending cannot advance/grade/acquire.

| Situation | Player-visible response | State/commit | Recovery |
|---|---|---|---|
| Exact approved current line | One matching clip + subtitles | Presentation only | Separate reveal/advance/action rules |
| Missing/stale/unapproved take | Readable text fallback | No approximate matching or success claim | Manifest blocks release coverage |
| Advance/cancel/owner loss | Stop/replace promptly | No domain or choice mutation | Release source/ducking once |
| Settings/focus/mute | Coherent pause/stop | No playback-driven progress | Resume only valid current owner |

## Visual/environment/interaction specification

Existing compact subtitles remain available and concise; no portrait-led speech UI replacement or provider metadata in player flow. Manifest records provenance/approval outside gameplay.

## Animation and physical contact

Retain current speaking/listening gestures; phoneme lip-sync not applicable. Speech duration never drives hand contact or task authority.

## Audio/dialogue/voice

Pilot measures real imported clip quality/size/residency/first-start/cancellation. Reuse VOICES/volume and existing cinematic ownership; chapter-bounded loads, no all-game preload. Guide/coach popups/system/labels/docs remain silent.

## Camera/mobile/accessibility

Directed conversation shots/manual reveal-return semantics preserved; settings/Back/focus and rapid taps have no audio/input overlap or tap-through.

## Data/interface/persistence

Minimum optional metadata on actual line/snapshot/insert builders plus local approved recording catalogue. No campaign writes for line playback, no spoken player choices committed by clip ending. Dynamic dialogue uses bounded exact variants or fixed speech with separate silent variable UI. Use verified owners; introduce no speculative public class/API to match this prose.

## Failure/cancellation/interruption/reload

Missing take continues readable text; failed load no stuck wait. Stop/replace/pause/dispose is idempotent; release ducking and owned references on every terminal path; no voice resumes after owner has changed. See SPEC's shared command boundary and [mode/restore verification](../VERIFICATION.md).

## Performance/resources

Measure import/compression/loading on current phone, record pilot ceilings and chapter memory policy; no guessed codec/size guarantee or per-line global search.

## Internal checks — AI only

- [ ] Actual session start snapshot and inserted line preserve metadata; exact/stale/unapproved/missing/speaker/branch mismatch resolution behaves correctly.
- [ ] Reveal versus advance, awaited action, stop-before-return cancellation, replace/owner loss/scene teardown, mute midline/pause/focus and duck restore; no domain/reward writes.
- [ ] Compile current audio/dialogue/cinematic paths, run meaningful dialogue/audio/route checks where harness compiles, record inherited AudioRegression issue honestly.

## USER Unity/device acceptance

- [ ] Listen to every pilot speaker and inserted branch, pronunciation/subtitle match/mix and rapid reveal/advance/cancel/Back; actual dialogue remains comprehensible with voices off.
- [ ] Offline phone load/pause/focus/scene return and soundFX/voices independent settings; measure memory/loading, no overlapping speech or muted office after return.
- [ ] Record actual build/device/steps/evidence for applicable gates **G6, G8, G10** in [VERIFICATION](../VERIFICATION.md); source compilation cannot check this box. AI delivers assets/builders/bindings; user is not assigned manual scene assembly.

## Completion definition

The visible goal works through its real entry/exit with the stated source, presentation, ownership and recovery rules; relevant internal checks pass or precise inherited limitations are recorded. Mark **Implemented, internally verified; user manual pending** only after actual implementation/checks. Mark **User accepted** only on recorded user evidence. Required unresolved asset/content/voice/device gates remain explicit; no placeholder or documentation-only completion.

## Handoff/update requirements

Update this ticket's actual status/checks, [PROGRESS](../PROGRESS.md), [KNOWN_ISSUES](../KNOWN_ISSUES.md) and the new pack's affected contract/inventory/coverage when needed. Record owner/session/date, modified paths, exact commands/results/warnings/fixtures, failures and unperformed native checks, actual supplied acceptance and precise next dependent/blocker. Preserve old initiative histories and existing user work; follow applicable AGENTS and the user's Git/Unity ownership. Scope stays Levels1–3.

