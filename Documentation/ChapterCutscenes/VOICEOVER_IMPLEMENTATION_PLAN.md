# Cutscene voiceover implementation plan

Status: executed with user-supplied recordings, 6 November 2026; internal verification complete, user Unity/device acceptance pending. This document preserves the planning decisions; actual paths, timing-guard behavior, source ownership and verification are recorded in [PROGRESS](PROGRESS.md#cutscene-voiceover--6-november-2026). Wider gameplay voiceover and provider spending remain outside scope.

## Recommendation

Use the 14 line IDs and six character briefs in [VOICEOVER_LINES](VOICEOVER_LINES.md). Generate individual clips during development, bundle approved clips, and play them from the existing cinematic owner. Start with the three integrated endings (10 clips including all four Rosa variants). Prepare the remaining four opening clips after the pilot, without activating the two blocked campaign openings.

## Verified project facts

- All five timing JSON files contain 14 cues in total: 2 / 2 / 5 / 2 / 3 in story order. Text, speaker, variant and timing already exist; the optional voice ID does not. See [cue model](../../Assets/Scripts/Cutscenes/Chapters/ChapterCinematicScript.cs:65), [selection](../../Assets/Scripts/Cutscenes/Chapters/ChapterCinematicScript.cs:105) and the source files listed in [VOICEOVER_LINES](VOICEOVER_LINES.md).
- The three endings are integrated; two openings remain inactive. The earlier line-list statement that T04 is unbuilt is stale. See [current route status](README.md:3).
- Cinematic music/ambience suppression already exists, including additive scene handling. Do not add another exclusion. See [scene notification](../../Assets/Scripts/AudioManager/AudioManager.cs:196), [music](../../Assets/Scripts/AudioManager/AudioManager.cs:342) and [ambience](../../Assets/Scripts/AudioManager/AudioManager.cs:471).
- The subtitle player already resolves the visible cue and Rosa variant together. Its lifecycle has pause and release seams. See [cue transition](../../Assets/Scripts/Cutscenes/Chapters/ChapterCinematicPlayer.cs:339), [pause](../../Assets/Scripts/Cutscenes/Chapters/ChapterCinematicPlayer.cs:261) and [release](../../Assets/Scripts/Cutscenes/Chapters/ChapterCinematicPlayer.cs:304).
- Effects audio has a 0.25-second late-start tolerance and pause/release behavior to reuse conceptually, but does not resolve subtitle variants. See [ChapterCinematicSound](../../Assets/Scripts/Cutscenes/Chapters/ChapterCinematicSound.cs:41).
- The presentation clock caps frame advancement at 1/15 second, while AudioSource playback has its own clock. A simple Play-at-cue-start implementation does not guarantee sync during hitches. See [session clock](../../Assets/Scripts/Cutscenes/Chapters/ChapterCinematicScript.cs:279).

## Decisions proposed for implementation

1. Keep the exact 15-second scenes, authored shot cuts and subtitle windows. Require takes to finish at least 0.25 seconds before cue.end: maximum 3.75 seconds for the 4-second windows and 4.35 seconds for the 4.6-second windows. This replaces the earlier 0.2-second margin and removes the proposed dynamic subtitle extension. Do not change story timing to accommodate an oversized take; regenerate or edit the take.
2. Use one scene-owned 2D AudioSource in ChapterCinematicPlayer. Play once when the selected cue begins; stop on cue exit, finish, abort, owner disable or unload. Never start another line during the Skip curtain. Fade the current clip with Skip, pause/unpause with the presentation, and keep speech independent of SOUND FX.
3. Add a VOICES toggle, default on, through existing SettingsManager, SettingsBoard3D and AudioManager. Keep a fixed mix level for the pilot. A muted cue is considered consumed: switching voices back on must not restart it halfway through. A standalone preview can read the same preference when no manager exists.
4. Add optional voice IDs to all 14 cues. Store clips at Assets/Resources/Cutscenes/Voice/<id>.wav. Load only cues relevant to the current scene and resolved variant; clear owned references on teardown. Measure memory/import behavior rather than assuming resources unload immediately.
5. Use one small runtime-readable catalogue with ID, exact subtitle text, speaker, variant, approved flag and take provenance. Resolve approval and exact text/speaker/variant match once during scene preparation. Missing, unapproved or stale entries stay text-only. This deliberately tightens the earlier test-only catalogue proposal, which would still play stale recordings. Keep pronunciation text and acting tags in production metadata, separate from displayed text.
6. Keep placeholders outside the release catalogue or explicitly unapproved. A development-only switch can admit them during the pilot; the release validator rejects placeholder/unapproved or missing required clips. Do not silently ship scratch speech.
7. Keep voice completion separate from Finished, result consumption, saved markers, rewards and routes. Voice is presentation only. Original intro, gameplay conversations, guide bubbles and lip sync remain separate future scope.

## Execution sequence

| Step | Deliverable | Acceptance |
|---|---|---|
| 1. Data and production manifest | Optional cue voice field, all 14 IDs, catalogue schema and exported recording sheet derived from actual JSON | Unique IDs; exact text/speaker/variant matching; 10 ending + 4 opening entries; current wording preserved |
| 2. Small playback pilot | Marie and Arthur ending clips; runtime source, lifecycle and VOICES toggle | Two lines play once; text-only fallback works; Effects off permits voice; Voices off preserves subtitles |
| 3. Clock and interruption check | Low-FPS/hitch probe plus focus/suspend/Skip handling | Verify actual source offset against cue-local presentation time. If material drift occurs, use bounded seek correction on the owned source; test audible repetition/stutter before accepting. Do not change the campaign clock to follow audio |
| 4. Endings coverage | 10 approved ending clips, four Rosa histories, catalogue and duration validator | Correct Rosa take only; silent CoastCare insert; no office playlist overlap; same result/Continue behavior |
| 5. Opening coverage | Four approved opening clips for standalone previews | Existing route gates remain inactive; no receiving gameplay is invented |
| 6. Delivery review | Import preset based on measured pilot; updated production standard, line-list corrections and PROGRESS | Internal checks recorded separately from user Play Mode/Android/listening acceptance |

Start with a small placeholder pilot or actual audition exports; no bulk paid generation is necessary to build the pipeline. Use WAV for the first duration validator (Python standard wave reader); do not claim OGG support from a WAV-header parser. Provider output may be a compressed source export converted once to the project master format; converting it does not restore lossless quality.

## Provider checks and production choices

Verified against first-party documentation on 6 November 2026:

- [Create speech](https://elevenlabs.io/docs/api-reference/text-to-speech/convert) accepts selected voice, text, model and per-request settings. One generated file per game line is our workflow choice.
- That endpoint documents plan-dependent export formats: 44.1 kHz PCM/WAV requires Pro or above. Do not purchase a higher tier solely because the Unity import asset is WAV; audition/export options and a local format conversion can be assessed first.
- [Models](https://elevenlabs.io/docs/overview/models) currently lists v4, v3 and other models. Audition actual character lines and confirm voice/model availability; do not assume an older plan's preferred model is the current best option.
- [Publishing guidance](https://elevenlabs.io/docs/help-center/legal/can-i-publish-the-content-i-generate-on-the-platform) excludes commercial use for free-plan output and Beta Services; eligible paid output remains subject to the stated terms. Record the generation subscription/model context for approved takes.

Open production choices: six voice identities, account access, approved credit cap, model/settings and pronunciation. Resolve these before paid generation. Keep credentials outside chat, source control and the game. Fixed mix/import quality, clip storage and memory budgets remain measured pilot choices rather than guarantees.

## Verification and completion

Internal checks should cover cue/catalogue consistency, duration budgets, stale/unapproved/missing fallback, active variant, once-only playback, no new starts after Skip, release on all exits, pause/resume, mute changes, late starts and hitch drift. Extend the existing chapter regression harness; rerun chapter-ending regression for continuation risks. Compile touched runtime/editor code. No broad unrelated code cleanup is part of this work.

User acceptance: listen to every character and Rosa variant; confirm Bayview/Bea/Nina pronunciation and emotional delivery; inspect subtitle/speech alignment on phone, offline playback, low FPS, early/middle/late Skip, Back, interruption/resume, independent audio toggles and transitions to the unchanged result flow. Measure package size and audio memory using imported clips. Source inspection cannot certify these checks.

No implementation tests were run for this planning-only document. The repository/source checks above establish the starting point, not audio or visual acceptance.
