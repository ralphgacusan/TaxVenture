# Progress: Settings board (3D)

## 2026-10-16: first version
Done: the 3D hanging control board in the Settings scene with pill switches, icon badges, name plate, BACK, drop-in / settle / lift-away motion, and touch through 3D raycasts. Behaviour matches the old panel. The editor capture tool and `Tests/SettingsRegression` (16/16) were added.

Verification: scratch compile 0 errors / 29 warnings (baseline); editor scripts 0 errors; SettingsRegression 16/16; CareerBoard, Guide, Controls, Dialogue and Progression suites pass. Editor capture: NOT RUN (the request file was left for the next focused editor update). Play Mode: NOT RUN (user).

Play Mode checklist:
1. Main menu → settings: the board drops in on its cables and settles, and the background is visible behind it.
2. Tap anywhere on the MUSIC row: the switch slides, the music stops or starts, and the state text changes.
3. SOUND FX: turning it off still plays the click; turning it on plays a click.
4. Leave and reopen: the switches show the saved values.
5. BACK or Escape: the board lifts away and the 3D Menu loads.
6. Ultra-wide (2960×1140) and 4:3 windows: the whole board stays on screen.

## 2026-10-16: real 3D office behind the board
- The blurred image background was replaced by the actual menu office: the 3D Menu is loaded additively and stripped to scenery, with its lighting, the menu camera view and a slow drift. The board is on an overlay camera with a light dim, and touches are restricted to the board layer.
- Verification: scratch compile 0 errors / 29 warnings; editor 0 errors; SettingsRegression 28/28; the other suites pass. Play Mode: NOT RUN (user). The editor capture still renders with the image background, because the preview scene does not load the office.

## 2026-10-16: settings moved into the 3D Menu (no Settings scene)
- The same board (design unchanged) now lives in `3D Menu.unity`. The SETTINGS button drops it in front of the main menu camera, the menu buttons hide while it is open, and BACK lifts it away and restores them.
- `Settings.unity` was deleted and removed from Build Settings. The office-backdrop loading was removed, because the board is already in the office.
- Other scenes calling `MainMenuUI.OnSettingsPressed` now go to the 3D Menu with settings open (`MenuEntry.OpenSettings`).
- Verification: scratch compile 0 errors / 29 warnings; editor 0 errors; SettingsRegression 33/33; the other suites pass. Play Mode: NOT RUN (user).

## 2026-10-16: pop animation for the menu buttons
- Menu buttons now pop out (swell, then shrink, staggered) when settings open, and pop back in (ease-out-back, staggered) after BACK. Their own sizes are restored afterwards.
- Verification: scratch compile 0 errors / 29 warnings; SettingsRegression 39/39; the other suites pass. Play Mode: NOT RUN (user).

## 2026-10-16: in-game settings use the 3D board, and pause the game
- New `InGameSettings` (built at runtime for the scenes in `Assets/Resources/Settings/in_game_settings.json`: Office_Level1 and Office_Level2) adds the same `SettingsBoard3D` in in-game mode. The board's look, rows, switches, animations and audio logic are unchanged. In-game mode only makes it face the gameplay camera at open time, hide no menu objects, and ignore `MenuEntry.OpenSettings`. The 3D Menu behaviour is unchanged.
- The HUD settings button opens it: Office_Level1 through `SettingsUI.OpenSettings`, Office_Level2 through `SettingsButtonPlaceholder.OnSettingsPressed` (it was a log-only placeholder; Level 2 was never wired to `MainMenuUI`). `MainMenuUI.OnSettingsPressed` opens it in these scenes too. The 2D `SettingsPanel` is kept closed at runtime, and nothing was deleted.
- While open: `Time.timeScale = 0` (`FreezeTime`, configurable, or `StoryOnly` / off), the HUD canvases are hidden, the player is held (`PlayerControlHold`), and world taps are blocked (`MobileInputGuard`). After BACK or Escape, everything resumes once the lift-away ends.
- Verification: scratch compile 0 errors / 29 warnings (baseline); editor scripts 0 errors; SettingsRegression 93/93; CareerBoard 69/69, Controls, Dialogue, Guide, Progression and VerifyProfileCards pass. The Python verifiers (`Tests/Verify*.py`) were NOT RUN (Python is missing). Editor capture: NOT RUN. Play Mode: NOT RUN (user).

Play Mode checklist (in-game):
1. Office_Level1: tap the HUD settings button. The board drops in front of the view, the HUD disappears, and the old 2D panel does not appear.
2. While it is open: NPCs, the level timer and the office-hours clock stop, and the joystick, look and world taps do nothing.
3. Switch MUSIC and SOUND FX: same sounds and order as in the menu. Reopen it, and in the 3D Menu: the values are kept.
4. BACK (or Escape): the board lifts away, time and the HUD come back, and you can walk again. The closing tap does not interact with anything.
5. Open settings at the desk (if the button shows there): the board faces the desk view.
6. Office_Level2: the settings button opens the same board.
7. Set `pauseGameWhenOpen` to false in the JSON: NPCs keep moving while settings are open, but the player is still held.
8. 3D Menu settings: unchanged (menu buttons pop out and back in).
