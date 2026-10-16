# Known issues: Settings board (3D)

- **In-game settings: Play Mode not run yet.** Check the in-game checklist in `PROGRESS.md`. Risk areas: the HUD settings button showing at the desk or in other camera modes (`PlayerControlHold` only holds the player in first-person mode, though world taps are still blocked through `MobileInputGuard`), and gameplay scripts that read raw input without `MobileInputGuard`.
- **`FreezeTime` uses `Time.timeScale = 0`.** Anything on unscaled time keeps running: HUD fades, the guide and the board itself. `OfficeHours` and the guide already wait while the time scale is 0. Audio keeps playing, on purpose, so the switches can be heard. If something sets `Time.timeScale` while the board is open, the saved value is still restored on close.
- **`StoryOnly` pause** calls `GameStateMachine.Pause()`, which ignores `ChangeState` calls while paused. If an NPC or timer keeps running and tries to change state then, that change is lost. Prefer `FreezeTime` (the default).
- **HUD hiding** adds a `CanvasGroup` (alpha 0) to each root screen-space canvas of the scene, and restores the values on close. A script that sets the alpha of a root canvas's own `CanvasGroup` every frame would show through. Canvases outside the scene (DontDestroyOnLoad) are not hidden.
- `Assets/Scenes/Settings.unity` was deleted (recover it with `git checkout HEAD -- Assets/Scenes/Settings.unity Assets/Scenes/Settings.unity.meta` if needed). `AudioManager.IsMusicScene` still lists "Settings", which is harmless.
- `MainMenu` reacts only to real touches (`Input.touchCount`), as before, so test the SETTINGS button with touch or the Device Simulator. The board itself also accepts the mouse.
- If the board covers something on the main menu you want visible, change `hideWhileOpen` or the layout constants.
- `Office_Level1_COPY` is not in the in-game config. Its settings button (`MainMenuUI`) still goes to the 3D Menu.
