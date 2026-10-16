# Agent instructions: 3D Menu career board

Read `README.md`, `PROGRESS.md` and `KNOWN_ISSUES.md` first.

## Rules
- Scope: `Assets/Scripts/UI/Menu/`, `Assets/Scripts/RewardsSystem/CareerProgress*.cs`, `Assets/Resources/Career/`, `Assets/Editor/CareerBoardSetup.cs`, `Tests/CareerBoardRegression`, and the `Career Board` component in `3D Menu.unity`. `ToggleCamera` gained only read-only `IsTransitioning` and `IsLevelSelectorVisible`. Time tracking also touches `LevelScoreLedger` (clock, run and side-quest seconds), `CaseProgressionManager` (sets the clock and starts the side-quest clock on the first collected profile), `LevelTotalResult`, `PlayerProgressManager` and `PlayerProgressData` (additive time fields and `RecordLevelTime`).
- **Read-only on the save.** The board never writes or normalizes `PlayerProgressData`. Unlock rules, rewards and badge tiers belong to `PlayerProgressData` / `LevelScoring` / `rewards.json`. Do not duplicate them.
- Put chapter logic in `CareerProgress` (pure, tested), not in the presenter. Put content in `chapters.json`, and keep `CareerProgressService.BuiltInCatalog` in sync with it (the regression checks the scene names).
- Make text world `TextMeshPro` (`CareerBoardKit.Text`), not UGUI. Canvases are allowed only for progress bars and the sheet's dim backdrop.
- Reuse Office Level 1 art and style (Synty office props, `HudTheme`, `HudBadgeArt`) rather than inventing new styles.
- Touch-only wording (Tap, never click or mouse). TaxVenture is a mobile game.
- Hide old scene objects at runtime (`legacyObjects`, `originalBoardName`). Never delete them.
- Use CRLF line endings for .cs files. Every new script needs a `.meta` with a fresh GUID.
- **The user does Play Mode and git.** Never claim a Play Mode result you did not receive.

## Checks after every change
1. Scratch compile: `python Tests/make_scratch_build.py`, then `dotnet build Temp/ScratchBuild/Scratch.csproj`. If Python is missing (as on 2026-10-16), copy `Temp/ScratchBuild/Scratch.csproj` and swap in the new files, as `Temp/ScratchBuild/Career.csproj` does. Baseline: 0 errors, about 29 warnings, none from Career files.
2. Editor script: `Temp/ScratchBuild/Editor/Editor.csproj` builds `CareerBoardSetup.cs` against the scratch runtime DLL.
3. `cd Tests/CareerBoardRegression && dotnet run` (69 checks, including a layout check that no papers overlap), and the other suites in `Tests/`.
4. Visual: in the Unity editor, run **Tools > TaxVenture > 3D Menu > Capture Career Board**, or create `Library/CareerBoardSetup.request`; the open editor runs it on its next update, which needs the editor to be focused. Review `Library/CareerBoardPreview/*.png`, and read `interaction-checks.txt` or `error.txt`.
5. Update `PROGRESS.md`.
