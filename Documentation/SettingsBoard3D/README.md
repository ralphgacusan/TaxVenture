# Settings board (3D: the 3D Menu and in-game)

There is **no separate Settings scene**. The settings live in `Assets/Scenes/3D Menu.unity` on the `Settings Board 3D` root object, built at runtime by `SettingsBoard3D`. Gameplay scenes show the same board as their in-game settings (see In-game settings).

## Flow
- On the main menu, tap the 3D **SETTINGS** button (`settings_button`). Both `MainMenu`'s touch raycast and `MainMenuUI.OnSettingsPressed` (the Button wired on that model) call `SettingsBoard3D.Open()`.
- The board drops in on its cables **in front of the main menu camera** (`ToggleCamera.mainCam`), so it sits at the main menu's angle with the office behind it, under a light dim.
- While it is open, the menu's 3D buttons (`play_button`, `tutorial_button`, `settings_button`, `sidequest_button`) are hidden, and `MainMenu` ignores touches. They **pop out** one after another (each swells to 112%, then shrinks away in 0.24 s, 0.06 s apart) as the board drops in.
- **BACK** (or Escape / Android back) lifts the board away. When the lift ends, the board hides and the buttons **pop back in** with a springy overshoot (0.42 s each, staggered). A button that was hidden before settings opened stays hidden.
- Settings only open on the main menu view, never mid camera transition or on the level select.
- From any other scene without in-game settings, `MainMenuUI.OnSettingsPressed` sets `MenuEntry.OpenSettings` and loads the 3D Menu, which opens the board as soon as the main menu view is ready.

## In-game settings (Office_Level1, Office_Level2)
The same board (same design, rows, switches, animations and audio logic) is the in-game settings. `InGameSettings` adds itself and a `SettingsBoard3D` (in in-game mode) to every scene listed in the config at load time; no scene is edited.
- **Opening:** the HUD settings button. Office_Level1's button calls `SettingsUI.OpenSettings`, and Office_Level2's calls `SettingsButtonPlaceholder.OnSettingsPressed`; both now open the 3D board. `MainMenuUI.OnSettingsPressed` also opens it in these scenes, with no trip to the 3D Menu.
- **Where it appears:** in front of the gameplay camera live at open time (`CameraController.MainCamera`, else `Camera.main`), drawn by the same overlay camera on layer 30, which moves to the end of that camera's stack.
- **While open:** the game pauses (see below), the scene's screen-space HUD canvases are hidden (alpha 0, no touches; restored exactly on close), the player cannot walk, look around or interact (`PlayerControlHold`), and world taps are blocked (`MobileInputGuard`). `SettingsUI.IsVisible` is true while it is open, so the Office HUD and minimap readers still work.
- **Closing:** BACK, Escape or Android back lifts the board away. When the lift ends, the game resumes, the HUD returns and the controls are released.
- **Legacy 2D panel:** `SettingsUI` and the `SettingsPanel` prefab stay in Office_Level1 but are kept closed at runtime; they are not deleted.

## Pause configuration (where to change it)
Edit **`Assets/Resources/Settings/in_game_settings.json`**:

| Field | Default | Meaning |
|---|---|---|
| `scenes` | `["Office_Level1", "Office_Level2"]` | Gameplay scenes that get the in-game board. |
| `pauseGameWhenOpen` | `true` | `false` = the game keeps running behind the board (controls are still held). |
| `pauseMode` | `"FreezeTime"` | `FreezeTime`: `Time.timeScale = 0` (the old value is restored on close), so NPCs, physics, animations, the level timer and office hours stop. `StoryOnly`: only `GameStateMachine.Pause()`, so office hours stop but the world keeps moving. |
| `hideHudWhileOpen` | `true` | Hide the HUD canvases while open. |

To use inspector values instead, add the `InGameSettings` component to an object in the scene yourself, untick **Use Resources Config**, and set **Pause Game When Open**, **Pause Mode** and **Hide Hud While Open**. The scene then keeps that component and does not get a second one.

## What the player sees (design unchanged)
- A navy control board with a brass rim, a recessed face and corner bolts, hanging on two cables, with a **SETTINGS** name plate and an **AUDIO** label.
- Two rows. Each has a round icon badge (gold with a music note; cyan with a speaker), the titles **MUSIC** / **SOUND FX**, and a subtitle with a coloured **ON/OFF**.
- Chunky 3D pill switches: brass ring, dark outline, a track that is mint when on and slate when off, gloss slashes, and a cream knob that slides with overshoot and squash. The whole row is the touch target.
- "Changes are saved automatically" and the brass **BACK** button.
- Motion: drop-in, a damped swing and a faint sway, rows popping in once, switch slides, and the lift-away.

## Behaviour (same as the old SettingsPanel)
- **Music**: the select sound plays, then `AudioManager.SetMusicEnabled`.
- **SFX on**: SFX is enabled, then the click plays. **SFX off**: the click plays, then SFX is disabled.
- `AudioManager` saves the values (PlayerPrefs `MusicEnabled` / `SFXEnabled`), and opening the board reads them back.

## Rendering
The board is on unnamed layer 30 and is drawn by a URP overlay camera stacked on the main menu camera, so office props never cut through it. Touch raycasts only test layer 30, and the dim (`backdropDim`) is a world canvas behind the board.

## Files
| File | Role |
|---|---|
| `Assets/Scripts/Settings/SettingsBoard3D.cs` | Board build, open/close, hiding menu buttons, audio behaviour, motion, input, overlay camera. |
| `Assets/Scripts/Settings/SettingsToggle3D.cs` | The 3D pill switch. |
| `Assets/Scripts/Settings/InGameSettings.cs` | In-game settings: adds the board to gameplay scenes, pause / resume, HUD hiding, control hold, legacy panel kept closed. |
| `Assets/Resources/Settings/in_game_settings.json` | In-game pause configuration. |
| `SettingsUI.cs`, `SettingsButtonPlaceholder.cs`, `MobileInputGuard.cs`, `OfficeHudRuntime.cs`, `OfficeMinimap.cs` | Open the in-game board from the HUD button; block world taps and hide the HUD while it is open. |
| `Assets/Scripts/MainMenu.cs`, `Assets/Scripts/UI/MainMenuUI.cs` | Open the board, and ignore menu taps while it is open. |
| `Assets/Scripts/UI/MenuEntry.cs` | `OpenSettings` / `ConsumeOpenSettings`: open settings when arriving at the 3D Menu. |
| `Assets/Editor/SettingsBoardSetup.cs` | **Tools > TaxVenture > Settings > Capture Settings Board**: renders the board from the 3D Menu main camera into `Library/SettingsBoardPreview`. |
| `Tests/SettingsRegression` | 93 checks: audio parity, no Settings scene, open paths, hide/restore, camera wiring, 3D/touch, and in-game settings (pause config, pause / resume, input and HUD while open, HUD button wiring). |

## How to edit
- Layout and colours: constants at the top of `SettingsBoard3D`.
- Which menu objects hide while open: `hideWhileOpen` on the component.
- Dim strength: `backdropDim`.
