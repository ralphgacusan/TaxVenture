# First story entry and intro replay — 2026-10-20

Implemented and internally checked; user Unity visual, touch and Play Mode acceptance remains pending.

## Player flow

- Fresh install or Reset Progress: main-menu Play fades directly to Cutscene1.0, then Office_Level1. It does not show the career/progress board first.
- The first intro viewing has no Skip button. Calling Skip directly also has no effect. Watching to the ending records the viewing in the existing signed progress save before the office loads.
- Before the first Level 1 finish, later Play presses still launch the intro/Level 1 directly. Once the intro has been watched, these attempts have Skip. An interrupted first viewing is not marked watched.
- First Level 1 finish and Continue: the result is applied, the game fades to the 3D Menu and opens directly on its career board, using the existing MenuEntry.OpenLevelSelect handoff.
- After a saved Level 1 finish, Play opens the normal career board. A finish at any score reveals the board; passing still controls Level 2's existing unlock rule.
- Career-board Level 1 Replay and in-game Level 1 Replay both visit the intro, with Skip available after watching. Training drills continue to reload their drill scene directly.
- Skip and natural ending share the same fade-to-office route. The JSON showSkip option can still disable Skip globally.
- Reset Progress resets intro viewing as well as career completion. Audio preferences remain preserved by the existing reset behavior.

## Fresh-menu visibility refinement

Before the first Level 1 finish, the title menu now exposes only Play and Settings. Settings hide Reset and centre Quit. Tutorial/side-quest objects and the binder's opening path stay gated until the first finish. Leaving an unfinished first attempt clears pending career/settings requests and returns to that fresh title state. Intro viewing alone does not unlock the full menu. See Documentation/SettingsActions/FreshMenuAndLighting.md.

## Implementation

- PlayerProgressData adds the additive levelOneIntroSeen bool (old saves default false), plus read-only HasFinishedFirstLevel and HasSeenLevelOneIntro properties. Existing completed careers are considered to have seen the intro even without the new bool. No save-version bump or extra PlayerPrefs key/file is needed.
- PlayerProgressManager.MarkLevelOneIntroSeen writes the existing signed save; on write failure it restores the in-memory flag so a failed save does not claim persistence. Existing Save logs the failure.
- MenuEntry.Play owns routing for both MainMenu's touch raycast and MainMenuUI's Start button. It checks loaded progress rather than EXP/reputation totals, so a completed zero-score run and migrated completions behave correctly. Tutorials/practice do not count as Level 1 completion.
- Cutscene01Runtime captures Skip eligibility before building UI. It records viewing only from Finish, not Awake, and guards the public Skip handler. Authored cutscene scenes, Timeline bindings, animation, subtitles and audio are unchanged.
- SceneFade creates its curtain on demand when the initial menu lacks one. This gives first Play a real outgoing fade and blocks repeat menu taps. The intro keeps its own authored/runtime fade-in; Level 1 replay avoids layering another shared fade over it.
- LevelCompleteUI keeps the existing direct-to-career handoff and requests a destination fade-in.

Changed runtime files: Assets/Scripts/{MainMenu.cs,UI/MainMenuUI.cs,UI/MenuEntry.cs,UI/LevelCompleteUI.cs,UI/HUD/SceneFade.cs,Settings/InGameSettings.cs,Cutscenes/Cutscene01Runtime.cs,RewardsSystem/PlayerProgressData.cs,RewardsSystem/PlayerProgressManager.cs}. Tests: IntroCutsceneRegression/{Program.cs,IntroCutsceneRegression.csproj} and SettingsRegression/Program.cs. All text is UTF-8 with CRLF. No scenes, prefabs, new assets or metadata changes are required.

## Internal verification

- python Tests/make_scratch_build.py
- dotnet restore Temp/ScratchBuild/Scratch.csproj
- dotnet build Temp/ScratchBuild/Scratch.csproj --no-restore -v:q — 0 errors / 29 baseline warnings.
- dotnet run --project Tests/IntroCutsceneRegression — 43/43. Includes real production save-data checks for fresh/tutorial/practice states, viewed-but-unfinished attempts, JSON persistence, zero-score finishes, legacy completion migration and fresh reset; source checks cover both Play entry points, Skip gating and fade routing.
- dotnet run --project Tests/SettingsRegression — 124/124, including Level 1 replay via the intro with direct practice replay retained.
- Other suites PASS: CareerBoard 72, Guide 733, Controls 5,734, Progression, Dialogue 799, Training 513 and Level2 1,280. VerifyLevel2Assets and VerifyLevel2Scene PASS (the scene check retains its two existing warnings). VerifyGuideTargets: 17 paths, zero missing.

## User Unity acceptance

1. Reset Progress, then tap Play: the menu fades straight into the intro with no career-board visit and no Skip.
2. Watch to the end: the office loads. Replay Level in settings: the intro now has Skip; using it fades into a fresh Level 1.
3. Leave an unfinished Level 1 to the menu: Play still launches Level 1's intro directly, with Skip now available. Relaunch the app and confirm that viewing is remembered.
4. Finish Level 1 and tap Continue: the career/progress board appears for the first time with the real saved result. Later Play opens the board. Level 2 unlock behavior still follows passing requirements.
5. Replay Chapter 1 from the board: intro Skip works. An older completed save also has Skip without requiring a new first viewing.
6. Reset again: initial Play bypasses the board and Skip is absent again. Interrupting an unwatched intro must not unlock Skip.
7. Check the intro's first title/subtitles remain visible during entry fades, no double curtain appears, and rapid taps cannot activate hidden menu buttons.
