# Intro cutscene: Cutscene2.0

Current wiring (2026-10-21): fresh Play, Chapter 1 START/Replay, in-game Level 1 Replay and legacy ProgressUI all load Assets/Scenes/Cutscene2.0.unity. Cutscene01Runtime keeps its existing class name but bootstraps only Cutscene2.0. The new scene is enabled once at the end of the build list, preserving existing scene indices.

The intro now displays the **same subtitle view used by the chapter cinematics**: LiberationSans 38, off-white text, brass speaker name, translucent dark panel, 28/18 padding, 56 reference-pixel bottom margin, safe-area placement and 0.1-second cue fades. Both runtimes call `ChapterCinematicPlayer.SubtitleView`; its former private chapter layout was exposed for reuse, not duplicated. The intro keeps its own authored Timeline and existing transition/Skip owner.

`Resources/Cutscenes/cutscene01_subtitles.json` contains 11 readable caption groups across the 9 existing dialogue recordings. Runtime binds exact `audioClip` names on `Audio Track`, respecting each Timeline clip's position, trim/speed and actual decoded audio length. CLIP 10 is split into its existing three visual/dialogue beats (introduction, checking actions, decision); the final “Alright… Let's get to work” recording stays one cue. The fallback data follows the current 47.856-second Timeline rather than the retired 66-second estimate. No audio, animation, scene text or Timeline asset is edited.

The first viewing hides Skip and rejects direct Skip calls. Natural completion marks the existing signed progress save as viewed. Later attempts and replays allow Skip; old completed careers remain eligible. Skip and completion share the fade to Office_Level1. Reset Progress restores mandatory viewing. Global music and ambience stay suppressed during Cutscene2.0 so its own audio can play.

Internal verification and changed paths: PROGRESS.md. Existing authored-asset limitations: KNOWN_ISSUES.md. The user performs Unity Play Mode and all git operations.

## Unity acceptance

1. Fresh/reset progress: Play loads Cutscene2.0 with shared-style subtitles and no Skip (the existing first-view rule).
2. Check caption/audio alignment through all nine recordings, especially the three CLIP 10 groups; internal phrase boundaries were based on authored text beats, not a listening/forced-alignment pass.
3. Verify text contrast, two-line holds, safe areas, and no interference with the existing authored scene lettering on a phone.
4. Pause the Timeline and confirm captions stay with it; resume without jumping ahead. Natural completion still marks viewing and fades to Office_Level1.
5. Replay: Skip remains upper right and follows its existing fade/transition. No hanging subtitle panel afterward.

Internal checks: `python3 -B Tests/IntroCutsceneRegression/run_offline.py` and `python3 Tests/ChapterCutsceneRegression/run_offline.py`. The user owns Unity playback, audio listening/device acceptance and all Git operations.
