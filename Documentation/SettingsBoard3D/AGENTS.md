# Agent instructions: Settings board (3D)

Read `README.md`, `PROGRESS.md` and `KNOWN_ISSUES.md` first.

- Scope: `Assets/Scripts/Settings/SettingsBoard3D.cs`, `SettingsToggle3D.cs`, `InGameSettings.cs` (in-game settings) and `Assets/Resources/Settings/in_game_settings.json` (its pause config), `Assets/Editor/SettingsBoardSetup.cs`, the `Settings Board 3D` object in `3D Menu.unity`, `MainMenu.cs` / `MainMenuUI.cs` / `MenuEntry.cs` (opening paths) and `Tests/SettingsRegression`. There is no Settings scene; do not add one back. It reuses `CareerBoardKit` / `CareerBoardButton` / `CareerBoardTarget` from `Assets/Scripts/UI/Menu`; change those only in ways that keep the career board working (see `Documentation/3DMenuCareerBoard`).
- Keep the audio behaviour identical to `SettingsUI` (call order and saved keys). `Tests/SettingsRegression` enforces it.
- In-game settings are the same board (`InGameSettings`, built at runtime for the scenes in the JSON). Do not give the in-game board its own look or audio logic: change `SettingsBoard3D` for both, keep in-game differences behind `inGame` / `ConfigureInGame()`, and keep the 3D Menu behaviour identical.
- `SettingsUI` / the `SettingsPanel` prefab stay in Office Level 1 but are kept closed; `SettingsUI.OpenSettings` routes to the board. Do not delete or rewrite them, and do not edit the big scene YAML to rewire the button.
- Pause: default `FreezeTime` (`Time.timeScale = 0`, restored to the saved value). The board, switches and buttons must stay on unscaled time. `FreezeTime` deliberately does not call `GameStateMachine.Pause()` (a paused machine drops `ChangeState` calls). The Level Guide rule "no timeScale 0" is about tutorial pages; settings hide the HUD on purpose, so it does not apply here.
- World TextMeshPro only. Touch-only wording. CRLF line endings. Every new script needs a `.meta` with a fresh GUID.
- The user does Play Mode and git. Never claim Play Mode results you did not receive.

## Checks
1. Scratch compile: `Temp/ScratchBuild/Career.csproj` (includes the Settings scripts). Baseline: 0 errors / 29 warnings. `InGameSettings.cs` was added to `Career.csproj` by hand (Python is missing; see `Documentation/3DMenuCareerBoard/AGENTS.md`). Editor: `Temp/ScratchBuild/Editor/Editor.csproj`.
2. `cd Tests/SettingsRegression && dotnet run` (93), plus the other `Tests/*` suites.
3. Visual: **Tools > TaxVenture > Settings > Capture Settings Board** (or create `Library/SettingsBoardSetup.request`). Review `Library/SettingsBoardPreview`.
