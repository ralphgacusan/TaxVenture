# Chapter cutscenes — voiceover plan and line list

Status: **Implemented, internally verified; user audio/device acceptance pending, 6 November 2026.** The sections below preserve the original proposal and recording script; the actual delivery supersedes proposed paths, timing and ownership details. See [PROGRESS](PROGRESS.md#cutscene-voiceover--6-november-2026): original MP3s stay in `Assets/AudioManager/Cutscene_Dialogues`, five scene catalogues reference them, cue windows are fixed, stale text falls back at runtime, and existing T04 music suppression is reused. The three ending routes are integrated; the two openings remain standalone. No Unity launch or Play Mode verification was performed.

The user asked for every chapter cutscene line to be spoken, one clip per line, with the speaker named. This plan reuses the existing chapter cinematic player, `AudioManager`, the settings board and the subtitle timing. It does not add a new audio or dialogue framework.

## 1. What exists today

### The text-to-speech plan

[VoiceoverPlan.md](../VoiceoverPlan.md) is a **deferred proposal**, not a feature. It says so itself: "DEFERRED — future reference only, not implemented" (user decision, 28 October 2026). Its main points still apply:

- Generate the lines **during development** with ElevenLabs Text to Speech, one clip per line. Ship the approved clips inside Unity for offline playback. There is no live text-to-speech in the game and no provider call or API key in the build.
- Give each voiced line a stable ID and a small ID-to-clip catalogue that records the source text, so a changed line is detected as stale.
- Subtitles stay on at all times. Voice has its own setting. A missing clip falls back to text. Voice never drives progression.
- The user picks the voices, approves any spending, and checks the result by ear. Release audio needs an eligible paid plan (free-plan output is not commercially licensed). Recheck provider terms, models and prices before generating, because that research is from 28 October.

### What the code actually has

| Area | Finding |
|---|---|
| Voice or TTS code | **None.** No script plays spoken dialogue. `GuideVoice` (`Assets/Scripts/UI/Guide/GuideVoice.cs`) is despite its name a **text** bubble for Arthur's lines, not audio. |
| `AudioManager` | Music and effects only. `PlayDialogueSFX`, `PlayNPCDialogueSFX` and `StopDialogueSFX` are **empty stubs** ("dialogue voices are disabled"), kept only for old UnityEvents. The gibberish clips in `SFX/Dialogue` (`femgib_*`, `malegib_*`) are unused. |
| Settings | Two on/off toggles, **MUSIC** (`MusicEnabled`) and **SOUND FX** (`SFXEnabled`), in `SettingsManager`, `SettingsBoard3D` and `AudioManager`. There are no volume sliders and no voice setting. |
| Audio clips | `Assets/AudioManager/TaxVenture_Audio_Library/` holds Ambience, Music and SFX, referenced through `Resources/TaxVentureAudioLibrary.asset`. There are no voice clips anywhere in `Assets`. |
| Intro cutscene (`Cutscene2.0`) | No voice. It plays its own Timeline audio. The old subtitle file `Resources/Cutscenes/cutscene01_subtitles.json` is still on disk, but `Cutscene01Runtime` clears its lines at runtime. |
| Chapter cutscenes | `ChapterCinematicSound` plays room tone and one paper cue per scene. Volume follows the SOUND FX toggle and fades with the Skip curtain. Sounds pause on focus loss and stop on finish. The production standard currently says "No new voice-generation dependency" and "No new generated/downloaded narration required". **This request changes that rule.** Amend [PRODUCTION_STANDARD](PRODUCTION_STANDARD.md) §5/§8 when voice is implemented. |

**Short version:** a plan exists, but no part of it is built. Cutscene voice would be the first voice work in the project. It is a good pilot because it is small (14 lines), each line has a fixed time slot, and there is no branching except Rosa's four variants.

### The chapter cutscenes

T01, T02 and T03 are all **implemented as standalone scenes** (internally verified; your Play Mode check is still pending). T04 routing is not built. Every subtitle line is already **in data**, in the per-scene timing JSON. The ticket text matches the JSON word for word. No line exists only in the docs.

| Stable ID | Scene | Timing data |
|---|---|---|
| `chapter01.end` | `Assets/Scenes/Cutscenes/CS_Chapter01_End.unity` | `Assets/Cutscenes/Chapter01/chapter01_end_script.json` |
| `chapter02.start` | `Assets/Scenes/Cutscenes/CS_Chapter02_Start.unity` | `Assets/Cutscenes/Chapter02/chapter02_start_script.json` |
| `chapter02.end` | `Assets/Scenes/Cutscenes/CS_Chapter02_End.unity` | `Assets/Cutscenes/Chapter02/chapter02_end_script.json` |
| `chapter03.start` | `Assets/Scenes/Cutscenes/CS_Chapter03_Start.unity` | `Assets/Cutscenes/Chapter03/chapter03_start_script.json` |
| `chapter03.end` | `Assets/Scenes/Cutscenes/CS_Chapter03_End.unity` | `Assets/Cutscenes/Chapter03/chapter03_end_script.json` |

Each scene runs for exactly 15.000 s, with shot cuts at 0.00, 4.80 and 10.10. `ChapterCinematicPlayer` reads the JSON `cues` (speaker, text, start, end, variant). `CueAt(time, variant)` chooses the subtitle on screen, and only cues whose `variant` is empty or matches `PendingContext` are shown. **Elijah has no spoken lines.** The receptionist and Marco speak while off camera or over inserts. No face has lip-sync, so a clip only has to match its subtitle window, not mouth movement.

## 2. Line list (play order)

Level 1 has no chapter opening, because the existing intro serves as its opening. So the list starts with the Level 1 ending. Levels 2 and 3 each have an opening and an ending, and both are listed in play order. Times are the authored subtitle window. **Budget** is the longest the clip should run so that it ends before the subtitle window closes (see §3.3). **Est.** is a natural read at about 2.6 words per second.

All 14 lines are **in data**. None is docs-only.

### Level 1 end — "A First Case Closed" (`chapter01.end`)

| # | Voice ID | Speaker | Line | Window | Budget / est. | Source |
|---|---|---|---|---|---|---|
| 1 | `vo_ch01_end_01_marie` | Marie | Thank you. I understand it now. | 0.60–4.60 | 3.8 s / 2.3 s | `chapter01_end_script.json` cue 1 |
| 2 | `vo_ch01_end_02_arthur` | Arthur | Good work. Another file is waiting. | 5.20–9.80 | 4.4 s / 2.3 s | `chapter01_end_script.json` cue 2 |

Delivery: Marie sounds relieved and grateful, quietly, because the explanation landed. Arthur sounds warm and brief, and turns the line toward the next task. He is a mentor, not a cheerleader.

### Level 2 start — "An Ordinary File" (`chapter02.start`)

| # | Voice ID | Speaker | Line | Window | Budget / est. | Source |
|---|---|---|---|---|---|---|
| 3 | `vo_ch02_start_01_marco` | Marco | Bayview's first records. More will follow. | 5.20–9.80 | 4.4 s / 2.3 s | `chapter02_start_script.json` cue 1 |
| 4 | `vo_ch02_start_02_receptionist` | Receptionist (off camera) | Start with the company details. | 10.50–14.50 | 3.8 s / 1.9 s | `chapter02_start_script.json` cue 2 |

Delivery: Marco is a careful, reserved, organised bookkeeper. Keep him matter-of-fact, **not** nervous or sinister. The receptionist is friendly and efficient, giving a routine handoff.

### Level 2 end — "A Familiar Name" (`chapter02.end`)

Only **one** Rosa line plays in a given viewing (0.60–4.60). Which one depends on the player's committed Level 2 history, through `ChapterCinematicContext.Variant`. Record all four.

| # | Voice ID | Speaker | Variant (when it plays) | Line | Window | Budget / est. | Source |
|---|---|---|---|---|---|---|---|
| 5a | `vo_ch02_end_01_rosa_responsible` | Aling Rosa | `rosa.responsible_completed`: responsible help with completed follow-through | You checked it with me. Thank you. | 0.60–4.60 | 3.8 s / 2.7 s | `chapter02_end_script.json` cue 1 |
| 5b | `vo_ch02_end_01_rosa_promise` | Aling Rosa | `rosa.promise_resolved`: an unsupported promise that was later resolved | Next time, please check before you promise. | 0.60–4.60 | 3.8 s / 2.7 s | `chapter02_end_script.json` cue 2 |
| 5c | `vo_ch02_end_01_rosa_dismissive` | Aling Rosa | `rosa.dismissive_completed`: dismissive at first, then corrected service | Thank you for finishing the review. | 0.60–4.60 | 3.8 s / 2.3 s | `chapter02_end_script.json` cue 3 |
| 5d | `vo_ch02_end_01_rosa_neutral` | Aling Rosa | `rosa.neutral`: default, legacy saves, isolated preview | Thank you for explaining the corrected copy. | 0.60–4.60 | 3.8 s / 2.7 s | `chapter02_end_script.json` cue 4 |
| 6 | `vo_ch02_end_02_receptionist` | Receptionist (off camera) | always | Bea's next. She's bringing her receipts. | 10.50–14.50 | 3.8 s / 2.3 s | `chapter02_end_script.json` cue 5 |

Delivery: Rosa is an older, warm laundry owner. 5a is genuinely warm. 5b is gently firm, polite rather than angry. 5c is courteous and a little reserved. 5d is plain and grateful. The 4.80–10.10 CoastCare insert is deliberately silent: no voice, no sting. The receptionist is light and routine.

### Level 3 start — "The File Returns" (`chapter03.start`)

| # | Voice ID | Speaker | Line | Window | Budget / est. | Source |
|---|---|---|---|---|---|---|
| 7 | `vo_ch03_start_01_receptionist` | Receptionist (off camera) | Bayview's income records arrived, with the original entries. | 5.20–9.80 | 4.4 s / 3.1 s | `chapter03_start_script.json` cue 1 |
| 8 | `vo_ch03_start_02_arthur` | Arthur | Check whether the summary agrees. | 10.50–14.50 | 3.8 s / 1.9 s | `chapter03_start_script.json` cue 2 |

Delivery: the receptionist is calm and matter-of-fact. Arthur is focused and quietly instructive. Do not hint at an answer or stress any word in a suspicious way.

### Level 3 end — "Plans Beyond the Paper" (`chapter03.end`)

| # | Voice ID | Speaker | Line | Window | Budget / est. | Source |
|---|---|---|---|---|---|---|
| 9 | `vo_ch03_end_01_bea` | Bea | Now I can plan for that studio. | 0.60–4.60 | 3.8 s / 2.7 s | `chapter03_end_script.json` cue 1 |
| 10 | `vo_ch03_end_02_receptionist` | Receptionist (off camera) | The mayor wants supported findings, not guesses. | 5.20–9.80 | 4.4 s / 2.7 s | `chapter03_end_script.json` cue 2 |
| 11 | `vo_ch03_end_03_receptionist` | Receptionist (off camera) | Nina sent two versions. She'll explain next time. | 10.50–14.50 | 3.8 s / 3.1 s | `chapter03_end_script.json` cue 3 |

Delivery: Bea is a young freelance artist, hopeful and relieved, with a small smile in the voice. In line 10 the receptionist is passing on a message, so keep it neutral and avoid accusation. In line 11 she is light and forward-looking.

### Totals and cast

14 clips: 5 receptionist, 4 Rosa, 2 Arthur, 1 each for Marie, Marco and Bea. All speakers voice the current English text. Check these names and terms by ear:

| Speaker | Voice brief (from [WORLD_AND_CHARACTERS](../CampaignStory/WORLD_AND_CHARACTERS.md)) | Pronunciation to confirm |
|---|---|---|
| Marie Bautista | Young adult local BPO employee; warm | — |
| Arthur | Senior tax consultant and guide; mature, calm male | — |
| Marco Ramos | Bayview bookkeeper; careful, reserved male | "Bayview" |
| Aling Rosa | Older laundry owner; warm, direct female | — |
| Receptionist | Office coordinator, no personal name; friendly, efficient female (she is "her" in the T01–T03 staging) | "Bayview", "Bea", "Nina" |
| Bea (Beatriz Mendoza) | Young freelance graphic artist; hopeful female | — |

Keep **one fixed voice per character across all chapters**. Arthur in Level 1 and Level 3, and the receptionist across four scenes, must sound like the same person. If Arthur, Marie or Rosa later get voiced gameplay dialogue (VoiceoverPlan step 7), reuse these voices.

## 3. Plan: how voice plugs into the existing cinematic player

### 3.1 IDs and data (one optional field per cue)

Add one optional field, `voice`, to `ChapterCinematicCue`, and add the ID to each cue in the five JSON files:

```json
{ "speaker": "Marie", "text": "Thank you. I understand it now.", "start": 0.6, "end": 4.6, "variant": "", "voice": "vo_ch01_end_01_marie" }
```

- ID pattern: `vo_ch<NN>_<start|end>_<line#>_<speaker>[_<variant>]`, lower case, underscores only. The ID **is** the clip file name, without extension. Rosa's variants share line number `01` because they fill the same slot.
- An empty or absent `voice` means a subtitle-only cue, so old JSON keeps working unchanged.
- The existing `Validate()` gains three checks: the ID matches the pattern, IDs are unique within a script, and the speaker segment of the ID matches `speaker`.

### 3.2 Where clips live

| What | Where |
|---|---|
| Shipped clips | `Assets/Resources/Cutscenes/Voice/<voice id>.wav` (one flat folder, 14 files). Loaded with `Resources.Load<AudioClip>("Cutscenes/Voice/" + id)` **only for the cues of the scene being played**, when the player starts. Nothing is preloaded for other chapters. Dropping in a correctly named file is enough: no scene edit and no Inspector drag. |
| Catalogue | `Assets/Resources/Cutscenes/Voice/voice_catalogue.json`: one row per ID with speaker, exact text, and take metadata (source: ElevenLabs or recorded; voice name or ID; model; settings; date; plan or licence; approved yes/no). Used by tests and as your production record, not read by the game. |
| Masters and outtakes | Outside `Assets` (for example `VoiceMasters/` at the repo root, or your own drive). Keep the uncompressed 48 kHz approved takes there. Whether they go into Git is your call. The repo has no Git LFS. |

Import settings (confirm on a pilot clip): **Force To Mono**, Load Type **Decompress On Load** (clips are under 4 s), Compression **Vorbis**, quality about 70, Preload Audio Data on. Sample rate: keep the existing 44,100 Hz override used by the library. About 14 clips × 3 s should come to well under 1 MB in the build. Measure on the pilot.

### 3.3 Timing: subtitle and clip

The 15.000 s clock, the three shot cuts and the fades are fixed by the production standard. Voice fits inside them. It never lengthens a scene.

- The voice starts at `cue.start`, on the same frame the subtitle starts to fade in. Trim lead-in silence to about 50 ms in the master.
- The subtitle hides at **max(`cue.end`, voice end + 0.25 s)**, capped at the next cue's start (minus 0.1 s) and at the fade-out (14.75 s). With the budgets in §2, every clip ends inside its authored window, so in practice the authored windows stay as they are.
- **Budget rule:** a clip must be no longer than the window minus 0.2 s (3.8 s for 4.0 s windows, 4.4 s for 4.6 s windows). Every line is estimated at 1.9–3.1 s, so there is plenty of room. If a take runs long, ask for a tighter read or move `start`/`end` in the JSON within the shot. Do not speed audio up at runtime.
- A new offline check (`Tests/ChapterCutsceneRegression/VerifyVoice.py`, reading WAV/OGG headers without Unity) reports the length of each clip against its budget and fails on an overflow.

### 3.4 Playback owner

Keep it in the existing cinematic player, because it already resolves the cue, the variant, pause, Skip and the audio scale:

- `ChapterCinematicPlayer` creates **one 2D voice `AudioSource`** at start, in the same way it already builds its overlay at runtime, so the five scenes need no edits. When `CueAt` returns a new cue that has a loaded clip, it plays that clip once. The `ChapterCinematicSound` late-start rule is reused: a cue whose start passed more than 0.25 s ago (after a hitch or resume) is not started late.
- Volume = fixed voice mix level (start at 0.9, tuned against the 0.22 room tone) × the VOICES setting (§3.7) × the Skip fade. The Skip fade is the same `1 − CurtainAlpha` term that `AudioScale()` already applies while a Skip finishes. Split that term out so voice uses it **without** the SOUND FX on/off factor.
- Variants come for free: Rosa's clip is chosen by the same `CueAt(time, variant)` that chooses her subtitle.
- Room tone and paper cues stay on `ChapterCinematicSound`, unchanged.

### 3.5 Skip, pause, finish, abort

| Event | Voice behaviour |
|---|---|
| Skip (button or Android Back) | Fades with the 0.25 s Skip curtain, then stops. Taps on an already-fading Skip do nothing more. |
| Focus loss or suspend | `Pause()` in the existing `SyncPause`. On resume, `UnPause()` continues mid-clip. The presentation clock is paused too, so subtitle and voice stay in sync. |
| Natural end, finish, abort, scene unload | `Stop()` in the same path that releases elements. Voice never outlives the scene. |
| Voice ends early or late | It has no effect on `Finished`, results, routing or saves. Voice is presentation only. |

### 3.6 Missing or stale clip

- Missing file, or empty `voice` field: the subtitle plays exactly as it does today. In the Editor only, log one warning per missing ID per playback. On device, log nothing and show no error.
- A changed line (cue text no longer matches the catalogue text): the regression check fails and lists the IDs to re-record. At runtime the old clip still plays, because the check happens at test time and the game does no text comparison.
- Subtitles are never hidden, whether voice is on or off.

### 3.7 Voice setting

- Add a third toggle row, **VOICES** ("Spoken cutscene lines"), to the settings board, with key `VoiceEnabled` (default on). It goes through `SettingsManager` → `AudioManager.SetVoiceEnabled/IsVoiceEnabled`, the same pattern as MUSIC and SOUND FX.
- The settings board only exists in gameplay scenes. The cinematic reads `AudioManager.IsVoiceEnabled()`, or `PlayerPrefs.GetInt("VoiceEnabled", 1)` when no manager exists. This mirrors how it already reads `SFXEnabled`.
- Voice does **not** follow SOUND FX, so a player can mute taps and keep voices.
- A volume slider is deferred. The project has none yet. Add one only if your listening check shows players need it.

### 3.8 Prerequisite: office music in cutscene scenes

`AudioManager` currently starts its office playlist in any scene except `Cutscene1.0` and `Cutscene2.0`, which means it also plays in the `CS_Chapter*` scenes. That is already recorded as a T04 issue. Voice makes it more noticeable. Before you judge voice levels, add `CS_` scene suppression (or `ChapterCinematicPlayer.IsPlaying`) to the exclusions, as T04 or as a one-line part of the voice change.

## 4. Making the clips

Recommended order:

1. **Scratch pass (free, about 10 minutes).** Use the built-in macOS `say` command to produce placeholder WAVs with the IDs above. This tests naming, loading, timing, Skip and pause before any money or acting time is spent. Replace these before release.
2. **Audition.** In the ElevenLabs web app, try two or three voices per character using the actual lines from §2. Pick one voice per character and note the voice, model and settings in the catalogue. Before spending, confirm an eligible paid plan for release rights and set a credit cap (14 short lines are a very small job).
3. **Generate.** Make one TTS clip per line, never one multi-speaker file. Generate 2–3 takes and keep the best. Put any acting tags or phonetic spellings in the request only, never in the displayed `text`. Example request: "Bay-view's first records." while the subtitle still reads "Bayview's".
4. **Or record yourself or friends.** Use a quiet room and a phone or USB mic at 48 kHz/24-bit WAV, about 20 cm from the mouth. Record three takes per line, then name the files by ID.
5. **Master.** Trim to about 50 ms of head and tail silence. Normalise all 14 clips to the same loudness (about −18 LUFS integrated, peaks ≤ −1 dBTP) so the receptionist does not jump between scenes. Export WAV named `<voice id>.wav`.
6. **Drop in.** Copy the files to `Assets/Resources/Cutscenes/Voice/`, fill in the catalogue, and run the regression and `VerifyVoice.py`.

Never put an API key in chat, the project or the build.

## 5. Implementation steps (only when you approve)

1. Data: add the `voice` field and validator rules, and add IDs to the five JSON files (all 14 cues).
2. Player: add the voice source and playback, with the subtitle-hold rule (§3.3–3.6), in `ChapterCinematicPlayer`. No scene edits.
3. Settings: add the VOICES toggle through `SettingsManager`, `SettingsBoard3D` and `AudioManager`. Add the `CS_` music suppression (§3.8) if T04 has not done it yet.
4. Tests: extend `Tests/ChapterCutsceneRegression` with ID format and uniqueness, catalogue text matching cue text, clip-length budget, missing-clip fallback, voice stopped on Skip, abort and finish, voice paused and resumed with the clock, voice off respected, and the Rosa variant choosing the right clip. Add `VerifyVoice.py`.
5. Clips: scratch pass first (§4.1). After you are happy with the pipeline, do the real voices.
6. Your manual checks: voice identity and pronunciation, emotional fit of each line (especially Rosa's four), loudness against room tone, Skip early, middle and late (no voice tail), focus loss and resume, VOICES off (subtitles only), and Android offline playback.

Estimated size: about 60–90 lines in the player, about 30 across the settings and `AudioManager`, plus tests. Gameplay scripts, the intro, the cinematic scenes and saves are untouched.

## 6. Not covered by this list

- **Level 1 intro (`Cutscene2.0`)**: it uses its own Timeline audio and in-scene 3D text, and its old subtitle lines are cleared at runtime. If you want Elijah's intro narration voiced too, that needs a separate decision about its Timeline audio.
- **Gameplay dialogue** (conversations, Arthur's guide bubbles through `GuideVoice`, the `CaseFinale` bubble, notices): VoiceoverPlan steps 3–7 cover these. Cutscene voice is a good pilot for that larger job, but it does not voice those lines.
- **Lip-sync and facial animation**: none. Faces are static, and the speaker name on each subtitle shows who is talking.
