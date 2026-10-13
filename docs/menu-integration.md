# Menu integration checkpoint

## Resume status

1. **3D menu and input — complete.** `3D Menu` is the first enabled build scene. The old `MainMenu` scene remains unchanged as a disabled backup. Physics raycasters are on `Main Camera` and `LevelSelectorCam`; the duplicate `MainMenu` component was removed from `Buttons`.
2. **Managers and scene routes — complete.** `PlayerProgressManager` and the full `AudioManager` tree, including both child audio sources and serialized clip references, are in `3D Menu`. Settings and Progress return to `3D Menu`, and menu music recognizes it.
3. **Unity verification — pending.** Run the checks below in the Editor; Unity batch mode was not run during implementation.

The existing Play action is unchanged: its single `MainMenu` component still calls `ToggleCamera.SwitchToLevelSelector()` on touch.

## Manual checks

- Launch Play mode from the build scene list and confirm `3D Menu` opens with music. Click and tap **Settings** on the 3D menu; verify both audio toggles work and the Back button returns to `3D Menu` with the saved toggle state.
- Use touch input (or Unity touch simulation) on **Play**; confirm the existing camera transition reaches the level selector exactly once and its Back control returns to the menu view.
- Open **Progress** and **Settings** from their normal flows; verify each return path loads `3D Menu`.
- Confirm there is only one active `AudioManager` after scene changes and that menu music resumes in `3D Menu`, Progress, and Settings.
- Open the disabled `MainMenu` scene manually to confirm the backup remains available.
