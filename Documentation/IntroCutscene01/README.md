# Intro cutscene: Cutscene2.0

Current wiring (2026-10-21): fresh Play, Chapter 1 START/Replay, in-game Level 1 Replay and legacy ProgressUI all load Assets/Scenes/Cutscene2.0.unity. Cutscene01Runtime keeps its existing class name but bootstraps only Cutscene2.0. The new scene is enabled once at the end of the build list, preserving existing scene indices.

There is no generated subtitle or title overlay. The scene's own authored text, animation, camera and audio are preserved. The legacy subtitle JSON remains on disk; the runtime reads its fade, destination, fallback duration and showSkip options, then clears its subtitle lines. No authored cutscene scene or Timeline asset is edited.

The first viewing hides Skip and rejects direct Skip calls. Natural completion marks the existing signed progress save as viewed. Later attempts and replays allow Skip; old completed careers remain eligible. Skip and completion share the fade to Office_Level1. Reset Progress restores mandatory viewing. Global music and ambience stay suppressed during Cutscene2.0 so its own audio can play.

Internal verification and changed paths: PROGRESS.md. Existing authored-asset limitations: KNOWN_ISSUES.md. The user performs Unity Play Mode and all git operations.

## Unity acceptance

1. Fresh/reset progress: Play loads Cutscene2.0 with no added subtitles and no Skip.
2. Let it end: Office_Level1 loads and viewing is saved.
3. Replay from settings and Chapter 1: Cutscene2.0 has Skip; tapping it fades into Office_Level1.
4. Check its authored animation, audio, camera and text, safe-area Skip and fade timing on the target device.
