# Agent instructions: Intro Cutscene01

Read `README.md`, `PROGRESS.md` and `KNOWN_ISSUES.md` first.

## Rules
- Scope: `Assets/Scripts/Cutscenes/`, `Assets/Resources/Cutscenes/`, `Tests/IntroCutsceneRegression/`, and the Chapter 1 `scene` in `chapters.json` / `CareerProgressService.BuiltInCatalog` (keep both in sync; both regressions check them).
- **Never edit `Cutscene1.0.unity`.** Its directors, Timeline bindings, animation and audio are the user's authored content. Add behaviour through `Cutscene01Runtime` (runtime-built UI, scene-name gate). Never hand-rewrite scene YAML.
- Text and timings belong in `cutscene01_subtitles.json`, not in code. Keep timing logic in `CutsceneSubtitleTrack` (plain C#, no Unity calls) so the regression can compile it.
- Touch-only wording (Tap, never click / mouse / WASD).
- New Assets files need `.meta` files with fresh GUIDs (keep Unity's if it already made them). CRLF for project text.
- Do not use `GetComponent<T>() ?? AddComponent<T>()` (Unity fake null).
- **The user does Play Mode and all git.** Never run git, never launch Unity, and never claim a Play Mode result you did not receive.

## Checks after every change
1. `python Tests/make_scratch_build.py`, then `dotnet build Temp/ScratchBuild/Scratch.csproj`. Baseline: 0 errors, 29 warnings, none from `Assets/Scripts/Cutscenes`.
2. `cd Tests/IntroCutsceneRegression && dotnet run` (29 checks).
3. The other suites: every `Tests/*Regression` (`dotnet run`), `python Tests/VerifyLevel2Scene.py`, `python Tests/VerifyGuideTargets.py`.
4. Update `PROGRESS.md`.

## Retiming subtitles once the Timeline is in the project
Press Play from the 3D Menu and START Chapter 1, then copy the clip times from the `[Cutscene01] Subtitles timed to track ...` Console line. Runtime timing already follows the clips automatically. Copying the times into the JSON (and setting `"estimate": false`) only matters for the no-Timeline fallback. If the numbered track is not the one with the most clips, set `timelineTrack` to its name.
