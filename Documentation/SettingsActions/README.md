# Settings actions — 2026-10-20

Implemented; internally verified. User Unity Play Mode, visual and device acceptance pending.

## Behavior

The existing SettingsBoard3D is used in the 3D Menu and both office levels. Navy/brass materials, world TextMeshPro, CareerBoardButton press/hover feedback, audio toggle behavior, cable drop/sway/lift and unscaled animation are reused. The board is taller to accommodate large touch buttons; camera fitting accounts for its height. No scene, prefab, package or serialized wiring changes are required.

- Menu: RESET PROGRESS and QUIT GAME.
- Office_Level1 / Office_Level2: REPLAY LEVEL, BACK TO MENU and RESUME. An II LEVEL PAUSED status reflects InGameSettings.IsPaused (the optional no-pause configuration shows SETTINGS).
- Every new action opens an animated confirmation with action-specific consequences, Cancel and a named destructive action. Underlying controls cannot be tapped. Android back/Escape cancels confirmation first; a second back resumes. An opening-tap guard and busy flag prevent accidental repeated confirmation.
- Reset removes career results, unlocks, rewards/badges, training history, story facts, checkpoints, career-board seen state and both levels' remembered guide lessons. Audio preferences are retained. In-memory pending results and run IDs are cleared, and existing progress listeners refresh. SaveFileStore.Reset replaces the main and backup generations and removes rejected/interrupted remnants; a successful reset cannot recover the former career from its backup.
- Replay restarts Level 1 through its intro (Skip is available once watched); Level 2 reloads its active office scene. Earned career rewards remain. Level 2 clears its saved checkpoint first, including when replay is requested from a direct editor attempt, and blocks outgoing autosaves. Training drills retain their practice session and do not clear the real career checkpoint.
- Back to Menu loads MenuEntry.MenuSceneName (3D Menu). Level 1 abandons the unfinished attempt; Level 2 saves the review so career-board entry can resume. Existing direct-editor Level 2 attempts remain temporary and preserve the prior career checkpoint; their confirmation explicitly says this.
- Scene availability is checked before discarding an attempt. Save failure prevents a Level 2 menu exit; a visible error leaves the user in settings to retry/cancel. Gameplay stays paused during confirmation and resumes through existing scene teardown or normal closing.
- Quit calls Application.Quit in a player build; in Unity Editor it stops Play Mode.
- Physics.SyncTransforms before board raycasts keeps animated colliders aligned even while timeScale is zero.

## Changed files

- Assets/Scripts/Settings/SettingsBoard3D.cs — actions, modal confirmation, paused status, layout and touch synchronization.
- Assets/Scripts/Settings/InGameSettings.cs — confirmed replay/menu scene routing.
- Assets/Scripts/Level2/Level2Checkpointer.cs — settings exit save/discard, save failure propagation and outgoing save suppression.
- Assets/Scripts/RewardsSystem/PlayerProgressManager.cs — reset progression and clear current checkpoint.
- Assets/Scripts/Save/SaveFileStore.cs — replace both recoverable save generations.
- Tests/SettingsRegression/Program.cs and SettingsRegression.csproj — confirmation/routing contracts and executable file-store reset/recovery checks.

All edited project text retains CRLF. No new Unity assets/scripts require metadata.

## Internal verification

Commands:

- python Tests/make_scratch_build.py
- dotnet restore Temp/ScratchBuild/Scratch.csproj
- dotnet build Temp/ScratchBuild/Scratch.csproj --no-restore -v:q
- dotnet run --project Tests/SettingsRegression
- dotnet run --project Tests/<Name>Regression for Level2, Guide, Controls, Progression, Dialogue, Training, CareerBoard and IntroCutscene
- python Tests/VerifyLevel2Assets.py
- python Tests/VerifyLevel2Scene.py

Guide uses TEMP/TMP = <repo>/Temp/ConsistencyTests.

Results: scratch compile 0 errors / 29 baseline warnings; Settings 115/115; Level2 1,280; Guide 733; Controls 5,734; Progression PASS; Dialogue 799; Training 513; CareerBoard 69; IntroCutscene 29. Both Level 2 validators PASS. Scene validator reports its existing two warnings about Level 1 divergence from the original copy and a prefab-internal entrance door.

## User Play Mode checklist

1. Open menu settings. Check the enlarged board fits the display and existing audio switches still behave correctly.
2. Quit: Cancel and Android back keep the app open; Confirm closes an Android/player build (stops Editor Play Mode).
3. With earned progress, Reset: Cancel preserves data. Confirm removes rewards/unlocks/history and restores tutorials, while preserving audio choices. Relaunch and verify the fresh career remains.
4. In both levels open settings: visible paused indicator, frozen level/world, hidden HUD, no walking/look/world taps. Confirmations stay paused and animate.
5. Replay: Cancel preserves the attempt; Confirm starts the scene fresh with earned career rewards retained. In Level 2, test after retrieving the folder and making progress, then reopen from the career board to ensure the old checkpoint never reappears.
6. Back to Menu: test cancellation in both levels. Confirm returns to 3D Menu. Level 1 restarts on next entry; Level 2 resumes the saved review from the career board.
7. Resume restores HUD/controls/time. Check portrait, wide and tablet layouts, touch target alignment, confirmation text wrapping and button feedback.
8. Optional: replay and leave a training drill; career progress/checkpoints must remain intact. Direct Editor Play of Level 2 preserves its documented temporary-attempt behavior on menu exit.

No Unity editor, Play Mode, screenshots or device tests were run. Internal contract checks do not establish rendered appearance or engine lifecycle behavior; the checklist is the remaining acceptance work.

## Follow-up polish — 2026-10-20

User screenshot feedback addressed:

- Secondary settings actions now sit side by side as 340 × 88 buttons, below a subtle brass divider. Reset/Quit use muted burgundy faces; Replay/Menu use the existing navy. Back/Resume is a separate, larger brass primary action. The existing press, hover, sound and confirmation feedback remain.
- Side-quest papers and medals appear only for badges with an earned tier. A fresh/reset career shows all five question-mark slots; neither catalog availability nor a tier-zero badge reveals its paper/name/medal. Earned badges still open their chapter and celebrate upgrades.
- Leave and Replay use SceneFade's existing 0.6-second unscaled fade-out. Their destination receives a 0.9-second fade-in, including the 3D Menu, which normally has no fade component. Existing callers retain their prior destination behavior through the optional fadeInDestination argument.
- Successful exits keep the settings board open/locked and gameplay paused through the outgoing fade until scene teardown. The fade canvas is excluded from settings HUD hiding/restoration. The menu's direct world-touch raycasts wait while the curtain blocks input.

Additional files touched: Assets/Scripts/UI/HUD/SceneFade.cs, Assets/Scripts/MainMenu.cs, Assets/Scripts/UI/Menu/CareerBoardPresenter.cs, Tests/CareerBoardRegression/Program.cs and Documentation/3DMenuCareerBoard/{README,PROGRESS}.md. No scenes/prefabs/new assets were required.

Internal checks: scratch build 0 errors / 29 baseline warnings; Settings 123/123; CareerBoard 72/72; Level2 1,280 PASS. Other regression suites and Level 2 validators were rerun; see final session report.

Remaining user checks: assess the revised button spacing/colors at device resolution; reset progress and confirm five question marks with no medal papers; earn one badge and confirm exactly one paper appears; cancel/confirm Leave and Replay in both levels and verify black-to-scene fades, pause continuity and no touch-through.


## First-story replay follow-up

Level 1's in-game Replay now goes through Cutscene1.0 and uses its own fade-in. Once watched, the intro has Skip; fresh/reset careers must watch it once. Practice replay still reloads its drill directly. SettingsRegression is now 124/124. The new initial Play/progress-board flow and persistent viewing state are documented in Documentation/FirstStoryFlow/README.md.


## Fresh-menu and lighting follow-up

The first-run menu now shows only Play and Settings; Reset is hidden until Level 1's first finish, and quitting an unfinished first run preserves that menu state. Settings action faces are brighter and TrainingSession practice uses tutorial wording. Level 2's duplicated full-strength directional lighting and toolkit glare were reduced. Implementation, values, internal checks and user acceptance checklist: FreshMenuAndLighting.md in this folder.
