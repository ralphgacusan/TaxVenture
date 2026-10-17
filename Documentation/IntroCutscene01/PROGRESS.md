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
