# Progress: Intro Cutscene01

## 2026-10-17
Done (by a Claude Code side session):
- Analysed `Cutscene1.0.unity`. It has two PlayableDirectors (Play On Awake, Wrap Mode None, Game Time) on Timeline assets `6c754999587818b498193e98e38ca657` and `50b7538047e10b04c824eebc6b82fb2f`, a CinemachineBrain, one AudioSource with no clip, and 11 Animators with no controller (animation and audio come from the Timelines). There is no EventSystem and no UI.
- Analysed the old intro: `Level1CutsceneController` steps through 8 sprite frames on taps. It fades in from black over 0.25 s and, after the last frame, fades to black over 0.25 s and calls `SceneManager.LoadScene("Office_Level1")`. Its Skip button calls `OnSkipPressed`, which does not exist in the script, so Skip never worked.
- Routed Chapter 1 START to `Cutscene1.0` (chapters.json, the BuiltInCatalog fallback, and the ProgressUI default plus its serialized value in 3D Menu / Progress). Added the scene to the build list once.
- Added `Cutscene01Runtime` (0.25 s fades, ends when the longest director stops, a working Skip, safe-area subtitles and title card) and `CutsceneSubtitleTrack` (JSON data and clip timing).
- Added `cutscene01_subtitles.json` with all 14 lines. **Times are estimates** because the Timeline assets are missing (KNOWN_ISSUES 1).
- Added the `Tests/IntroCutsceneRegression` suite. Updated the CareerBoardRegression expectations.

Verified:
- Scratch compile: 0 errors, 29 warnings (the baseline, none from `Assets/Scripts/Cutscenes`).
- IntroCutsceneRegression 29/29, CareerBoardRegression 69/69, ControlsRegression PASS (5734), DialogueRegression PASS (799), GuideRegression 714 PASS, Level2Regression 448 PASS, ProgressionRegression PASS, SettingsRegression 93/93, TrainingRegression 513/513.
- `VerifyLevel2Scene.py` PASS (1 warning, as before). `VerifyGuideTargets.py`: 17 paths, 0 missing.

Not run: **Play Mode** (the user tests it).

## User Play Mode checklist
1. Open `3D Menu`, press Play, open the career board and tap Chapter 1 START. `Cutscene1.0` loads, fading in from black (about a quarter second).
2. Check the Console for `[Cutscene01] Following director ...` and `[Cutscene01] Subtitles timed to track ...`. If it says `No PlayableDirector with a Timeline`, the Timeline assets are still missing (KNOWN_ISSUES 1).
3. The cutscene animation and audio play as authored, with nothing changed by this work.
4. The COAST TALES title card shows over clip 1. Each subtitle then appears at the bottom with its clip, readable on a phone-sized Game view (for example 1280x720 and a notched device in the Simulator). It stays clear of the notch and home indicator.
5. Clip 10's five lines appear one after another, and clip 14's two lines in order. No subtitle shows during clips 4, 9 and 11-13.
6. When the cutscene ends, the screen fades to black and `Office_Level1` loads with its normal fade-in and level start.
7. Replay and tap **Skip** (top right) partway through. Same fade to black, then `Office_Level1`.
8. In the Simulator with a notched device, the Skip button sits inside the safe area and responds to a tap.
9. Level 1 still plays and completes normally afterwards (career progress saves as before).

## 2026-10-20: first story entry and remembered intro viewing

Main-menu Play now bypasses the career board until Level 1's first saved finish. The first intro viewing hides and rejects Skip; only completing the intro records viewing in the existing signed progress save. Subsequent Level 1 attempts/replays have Skip, including in-game Replay. Existing completed careers are treated as already having watched the intro. Reset Progress restores the initial mandatory viewing. The existing post-completion handoff shows the career board directly with a fade-in.

Internal checks: scratch compile 0 errors / 29 baseline warnings; IntroCutsceneRegression 43/43; SettingsRegression 124/124. Full flow decisions, changed paths and user Play Mode checklist: Documentation/FirstStoryFlow/README.md. No authored cutscene scene or Timeline asset was changed. User Play Mode and device checks remain pending.

## 2026-10-21: Cutscene2.0 wiring and no overlay subtitles

Implemented — internally verified; user Play Mode pending. Chapter 1 JSON and built-in catalog, ProgressUI default and the serialized values in 3D Menu/Progress now use Cutscene2.0. MenuEntry and in-game Replay already follow Cutscene01Runtime.SceneName, now Cutscene2.0. Added its existing GUID d2a88c100240fe545ba95b655d7a48f0 as an enabled build entry at the end, without changing old scene indices. Neither Cutscene1.0.unity nor Cutscene2.0.unity was edited.

Removed runtime subtitle/title UI and timing presentation, retaining the existing configuration loader for fade/nextScene/showSkip and clearing legacy lines. The scene's authored 3D text stays intact. Fresh-view Skip gating, viewed-progress persistence, replay eligibility, director completion and fade-to-office are unchanged. AudioManager suppresses global BGM/ambience in both cutscene versions.

Files: Assets/Scripts/Cutscenes/Cutscene01Runtime.cs, Assets/Resources/Career/chapters.json, Assets/Scripts/RewardsSystem/CareerProgressService.cs, Assets/Scripts/UI/ProgressUI.cs, Assets/Scenes/3D Menu.unity and Progress.unity (only the serialized route), ProjectSettings/EditorBuildSettings.asset, Assets/Scripts/AudioManager/AudioManager.cs, Tests/IntroCutsceneRegression/Program.cs, Tests/CareerBoardRegression/Program.cs, Tests/AudioRegression/Program.cs, and this pack's README/PROGRESS/KNOWN_ISSUES. Folder ownership work is recorded separately in the Level 2 pack.

Before implementation, routing/build/runtime/subtitle checks failed (7 of 44). Final: scratch compile 0 errors / 29 baseline warnings; IntroCutscene 44/44, CareerBoard 72/72, Audio 222, Level2 1,305, Guide 733, Settings 133, Controls 5,734, Dialogue 799, Training 514 and Progression PASS. VerifyLevel2Scene PASS with its two existing warnings, VerifyGuideTargets 17/17, VerifyLevel2Assets PASS (11,793 unique GUIDs). No Unity Play Mode was performed.

Asset inspection: EnvTimeline.playable now exists for GUID 6c754999587818b498193e98e38ca657. The second director in Cutscene2.0 still references GUID 50b7538047e10b04c824eebc6b82fb2f, for which no Assets metadata/file was found. Its original authored animation cannot be reconstructed from this reference; the runtime follows the available longest director. Check this missing character Timeline when testing. No guessed bindings or replacement Timeline were introduced.

Next: user runs the README fresh/replay/no-overlay/Skip checklist and verifies authored scene playback, including that unresolved Timeline reference.

## 6 November 2026 — shared chapter subtitles, aligned to existing dialogue audio

**Implemented and internally verified; user playback/listening/device acceptance pending.** User explicitly requested subtitles matching the other cutscenes, reuse only, and audio/dialogue-length timing. This supersedes the October no-overlay decision. No authored scene, Timeline, audio, route, build entry, save mechanism or intro duration was rewritten.

### Implementation and exact files

- `Assets/Scripts/Cutscenes/Chapters/ChapterCinematicPlayer.cs`: moved the existing private subtitle construction/layout into its nested reusable `SubtitleView`. Chapters and intro now call that **one** view. Same fixed font/max width/margin/padding/panel opacity, speaker styling, wrapping/outline and cleanup. Existing serialized chapter values, voice playback/clock guard, Skip, completion and pause paths retained. No new runtime file/component/framework.
- `Assets/Scripts/Cutscenes/Cutscene01Runtime.cs`: retains subtitle lines, creates the shared view inside its existing safe-area Canvas, binds the actual Timeline dialogue AudioTrack, follows that director's clock, uses existing chapter cue opacity, hides/releases owned subtitle material on exit. No additional director/audio source/playback control. A paused director no longer triggers premature completion; natural `stopped`, at-end, Skip/viewed-save and Office_Level1 handoff remain. If the master never starts, the existing fallback clock also takes over captions.
- `Assets/Scripts/Cutscenes/CutsceneSubtitleTrack.cs`: extends existing data with speaker, exact audio clip name and optional source-second phrase bounds. `ApplyAudioClipTimes` follows position, clipIn/timeScale, clip duration and decoded source duration, updates atomically, and preserves JSON fallback on missing/ambiguous/invalid references. Legacy clip/title fields and `ApplyClipTimes` remain compatible. `SubtitlePresentation` adapts existing timing to the existing ChapterCinematicCue/CueAlpha contract; no second renderer/clock engine.
- `Assets/Resources/Cutscenes/cutscene01_subtitles.json`: replaces obsolete estimates with 11 readable groups over 9 actual recordings; `timelineTrack` explicitly `Audio Track`; 47.856-second fallback matches current Timeline end. Existing spoken/scene wording used; CLIP 08 includes the authored “This might be a lot” introduction, CLIP 10 checking actions stay together to avoid rapid caption flashes. No generated narration.
- `Tests/IntroCutsceneRegression/Program.cs`: updates superseded no-subtitle and estimated-time expectations; retains route/build/Skip/viewed-save/legacy timing coverage and tests real Timeline audio bindings, cue boundaries/reading budget/fades, trims/speed/clamps, atomic fallback and single-view reuse.
- `Tests/IntroCutsceneRegression/run_offline.py`: thin entry point reusing `Tests/run_level3_repair.py` to compile current source (including not-yet-imported scripts) and run the intro suite, without launching Unity.
- `Tests/ChapterCutsceneRegression/Program.cs`: replaces the old intro-runtime hash warning with the newly authorized shared-renderer/retained-cue invariant; other authored intro/Timeline/controller preservation guards stay intact.
- `Documentation/IntroCutscene01/README.md`, `KNOWN_ISSUES.md`, `PROGRESS.md`, plus a bounded shared-change note in `Documentation/ChapterCutscenes/PROGRESS.md`.

### Audio and caption windows

| Existing audio | Caption windows (seconds in current intro) |
|---|---|
| CLIP 01 | 0.000–3.912, Coast Tales / small town opening |
| CLIP 02 | 3.936–7.200, simple town |
| CLIP 03 | 7.224–13.176, work/trade/build lives |
| CLIP 05 | 13.200–16.296, But for years… |
| CLIP 06 | 16.320–21.432, keeping track |
| CLIP 07 | 21.456–25.272, where I come in |
| CLIP 08 | 25.296–30.288, lot to fix |
| CLIP 10 | 30.312–36.067 introduction; 36.067–40.583 read/check/find; 40.583–43.896 decision |
| CLIP 14 | 43.920–47.832, Alright… Let's get to work |

The runtime recalculates these from actual clip names/windows and `AudioClip.length`, not hardcoded Timeline indices. Source metadata (`afinfo`) reports 24ms less than each Timeline clip's frame-rounded duration; the fallback excludes that padded tail. CLIP 10 internal boundaries follow existing authored scene-text activation beats, **not a listened or forced-aligned transcription**. Review those phrase changes while listening; full recording start/end alignment is internally verified. One shared subtitle style is used even for the old title flag; the authored in-scene title is left intact.

### Commands and actual results

- `python3 -B Tests/IntroCutsceneRegression/run_offline.py`: current game assembly **0 errors / 21 inherited warnings**, intro compilation **0 errors / 0 warnings**, **100/100 checks pass**. The three initial short action cues exceeded the reading budget; combining the three actions within their existing audio/visual beat corrected all failures without relaxing the test. Logs in `Temp/L3Repair/IntroCutsceneRegression/Debug/net8.0/`.
- `python3 Tests/ChapterCutsceneRegression/run_offline.py`: **434/434**, zero compiler/runtime warnings, including current chapter voice and all T01/T02/T03 scene coverage. The first `--game` attempt used a stale imported Bee source list missing another session's new SettingsSlider3D; switching to the existing full-current-source harness compiled that untouched source successfully. No unrelated source fix was made.
- `python3 -B Tests/ChapterEndingRegression/run_offline.py --reuse-game`: **58/58**, zero suite compilation warnings; current ending/save/return contract preserved.
- `python3 Tests/ChapterCutsceneRegression/VerifyVoice.py`: **14/14 existing chapter recordings** still fit their cues and shot boundaries/catalogue references.
- Shipped LiberationSans glyph-advance inspection at 38px/1180px with 4% bold allowance: **all 11 intro groups fit one or two lines**, no missing glyphs. This is geometry, not a TMP/device render.
- Two separate read-only standards/spec reviews found no actionable issue. Their remaining acceptance item is rendered/audio phrase alignment and device readability.
- Preservation hash baseline confirms authored intro scenes, EnvTimeline, all original MP3s, chapter scene/JSON/prop/material assets, build settings and starting route unchanged. Only the three intended runtime/data files and the shared subtitle renderer were changed in Assets. No Git, Unity execution/automation or actual-save access.

### User acceptance remaining

Play the current intro from fresh/replay paths: visually compare subtitles with chapter cinematics, listen through all nine recordings and the three long-recording caption groups, check mute/reading comprehension, safe-area/phone placement and coexistence with authored text, Timeline pause/resume, natural end and replay Skip cleanup/Office_Level1 return. Source timing and tests do not establish native audiovisual acceptance. The pre-existing missing secondary Timeline remains unmodified.
