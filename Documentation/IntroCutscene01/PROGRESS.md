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
