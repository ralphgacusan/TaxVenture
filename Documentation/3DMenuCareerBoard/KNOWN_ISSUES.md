# Known issues and risks: 3D Menu career board

- **Content margins vs. frame.** Cards on the CAREER board are 340 px wide, leaving about 30 px on each side. A thick prop frame could overlap them; if so, tweak `ProgressBoardSize` or `CardWidth`.
- **Chapter 2 maximum before the first run** uses `cases: 3` from chapters.json, but `Levels/Level_02/level.json` currently lists 2 case ids. Once a result is saved, the recorded maximum is used instead.
- World TextMeshPro uses `isOrthographic` (1 font size = 1 board pixel), as the previous version did. Under a perspective camera the SDF edges can look slightly soft at steep angles.
- The "last seen" celebration state is per device (PlayerPrefs). Clearing it only replays nothing. It never changes the save.
- The old `Progress Canvas`, `ProgressController` and `LevelButtons` are inactive in the scene, a change carried over from the previous attempt, and are also hidden at runtime. The old `Corkboard Levels` board is hidden at runtime.
- **Times start empty.** Saves made before this change have no times (`--:--`) until the chapter is played again. The level time includes pauses such as Settings; the side-quest time starts at the first collected profile.
- Badge papers use small text (11-13 px) so the five spaces fit beside the chapters. Badges beyond `badgeSlots` (5) are not shown.
- The Chapter 3 → 4 string crosses between rows, so it runs over the papers. This only appears once Chapter 4 is unlocked.
