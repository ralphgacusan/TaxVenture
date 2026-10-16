# Agent instructions: Tutorial Hub

Read `README.md` (the feature, the JSON reference and the code map), `PROGRESS.md` (status and what was verified) and `KNOWN_ISSUES.md` before changing anything. `PLAN.md` holds the original plan and the user's decisions.

## Scope and rules
- The feature lives in `Assets/Scripts/Training/`, `Assets/Resources/Tutorials/Training/`, `Assets/Editor/TutorialHubSetup.cs` and `Tests/TrainingRegression/`. The hub lives in the 3D Menu (`TrainingBinder3D` adds itself at runtime); there is no hub scene. The small hooks it adds elsewhere are listed at the end of `README.md`; keep them additive.
- **TaxVenture is a mobile game.** All text is for touch (tap, drag, press and hold, swipe, joystick). Never mouse, click, WASD or keys. The regression checks drill and catalog text for this.
- **The user's decisions** (see `PLAN.md`): Basics always open; side quest and case cards open only when seen in the level (SKIP does not count); only open tabs are shown; **no rewards, no stars**: the MASTERED stamp is the only recognition; case drills use `case_002`; GUIDED is skippable after the first practice.
- **Practice never reaches the career.** Keep the `PlayerProgressManager` guard and never call EXP, hearts, badge or level APIs from training code.
- **Content belongs in JSON.** Reuse in-game lessons with `"use"` instead of copying their text. New guide trigger kinds go into `GuideTriggers` (parsing) and `GuideDirector` (evaluation), documented in `README.md` (here) since the Level Guide docs belong to another initiative.
- Build everything at runtime. Never hand-edit the 3D Menu's or Office_Level1's YAML; the binder finds the desk and chair by name.
- Save changes stay additive and versioned, with `Normalize()` repairs and regression fixtures for older saves.
- Keep CRLF line endings. Every new asset needs a `.meta` with a fresh unique GUID.
- **The user does all Play Mode and device checks and all git actions.** Never claim a Play Mode result you did not receive. Commit nothing; switch no branch. The checkout is shared with other sessions: touch only this feature's files.
- Do not edit other initiatives' documentation folders.

## Checks after every change (from the project root)
1. `python Tests/make_scratch_build.py`, then `dotnet build Temp/ScratchBuild/Scratch.csproj`. Baseline **0 errors / 29 warnings** (2026-10-16).
2. `cd Tests/TrainingRegression && dotnet run --no-restore`: **513/513** at the time of writing.
3. The suites this feature touches: `Tests/ProgressionRegression` (save), `Tests/GuideRegression` (guide vocabulary), `Tests/CareerBoardRegression`, `Tests/SettingsRegression`, plus `Tests/ControlsRegression`, `Tests/DialogueRegression` and `python Tests/VerifyGuideTargets.py`.
4. In Unity: **Tools > TaxVenture > Tutorial Hub > Check Content** (composes every drill against guide.json).
5. Update `PROGRESS.md`.

## Useful facts
- A drill run is composed into a plain `GuideScript`, so everything the Level Guide supports (targets, triggers, hints, `duringResults`) works in drills. The run ends when its last step's lesson ends (`GuideDirector.LessonFinished`).
- `GuideDirector.Calm()` blocks lessons while a reward card is up; a last step that should show over a card needs `"duringResults": true`.
- After the result card shows, `PracticeModeRuntime` disables the director and holds every reward card's auto-continue, so nothing underneath can load another scene.
- `hide` runs after the level has started (every `Start` has run); hiding earlier breaks systems that look objects up in `Start`.
- The binder uses layer 29 and its own URP overlay camera; the settings board uses 30 with its own. Keep them apart, or each overlay would draw the other's objects too.
- Card state is derived on the fly (`TrainingCatalog.Build`); the save only stores seen lessons and run counts.
