# Intro Cutscene01: the new Level 1 intro

Career Chapter 1 **START** now plays the new intro cutscene scene, `Assets/Scenes/Cutscene1.0.unity` (the user calls it "Cutscene01" / "cutscene1,0"; the file name is `Cutscene1.0`). It replaces `Level1_Character_Intro_Cutscene`, which stays on disk and in the build list, but nothing routes to it now.

## What happens
1. 3D Menu > Play > Chapter 1 sheet > START loads `Cutscene1.0` (`chapters.json`, or the `CareerProgressService.BuiltInCatalog` fallback). The legacy `ProgressUI` Level 1 button also loads it.
2. `Cutscene01Runtime` adds itself to the scene at load (same `RuntimeInitializeOnLoadMethod` + `sceneLoaded` pattern as `SceneFade`), gated by the scene name `Cutscene1.0`. It does not edit the scene.
3. The screen fades in from black over 0.25 s, like the old intro.
4. The scene's PlayableDirectors play as authored (Play On Awake, Wrap Mode None). The runtime never plays, pauses or rebinds them and never touches audio or animation.
5. Subtitles from `Assets/Resources/Cutscenes/cutscene01_subtitles.json` appear at the bottom of the safe area in the HUD style: a brass-edged navy panel with the UI font. Clip 1 shows as a large centred title card ("COAST TALES." over "A small town by the sea.").
6. When the longest director stops, or the player taps **Skip** (top right, inside the safe area), the screen fades to black over 0.25 s and `Office_Level1` loads. The old intro did the same. Office_Level1's `SceneFade` then fades the office in.

## Subtitle timing
- Each JSON line names its clip (1-based, as counted on the Timeline). When the scene's Timeline can be read at runtime, the lines are timed to the real clip start and end times. The lines of one clip share that clip evenly, in order. The Console logs every clip's times (`[Cutscene01] Subtitles timed to track ...`).
- **Current JSON times are estimates** (`"estimate": true`). The Timeline assets the scene's directors use were not in the project when this was written (see KNOWN_ISSUES), so no clip times could be read from disk. The estimates assume about 4 s per clip, with clip 10 at 14 s.
- If the Timeline cannot be read, or the numbered track has fewer clips than the subtitles name, the JSON times are used. With no playable Timeline at all, the cutscene ends after `fallbackDurationSeconds` (66 s).

## Files
New:
- `Assets/Scripts/Cutscenes/Cutscene01Runtime.cs` (+ `.meta`): bootstrap, fades, end detection, Skip, subtitle UI, Timeline clip timing.
- `Assets/Scripts/Cutscenes/CutsceneSubtitleTrack.cs` (+ `.meta`): subtitle data and timing (plain C#, tested).
- `Assets/Scripts/Cutscenes.meta`, `Assets/Resources/Cutscenes.meta`: folder metas.
- `Assets/Resources/Cutscenes/cutscene01_subtitles.json` (+ `.meta`): editable subtitle text, timings and options.
- `Tests/IntroCutsceneRegression/` (`IntroCutsceneRegression.csproj`, `Program.cs`): routing, build entry and subtitle JSON check (29 checks).
- `Documentation/IntroCutscene01/` (this folder).

Changed:
- `Assets/Resources/Career/chapters.json`: Level_01 `scene` is now `Cutscene1.0`.
- `Assets/Scripts/RewardsSystem/CareerProgressService.cs`: the `BuiltInCatalog` Level_01 scene is now `Cutscene1.0`.
- `Assets/Scripts/UI/ProgressUI.cs`: the `levelOneCutscene` default is now `Cutscene1.0`.
- `Assets/Scenes/3D Menu.unity` and `Assets/Scenes/Progress.unity`: one line each, the serialized `levelOneCutscene: Cutscene1.0` on `ProgressUI`.
- `ProjectSettings/EditorBuildSettings.asset`: added `Assets/Scenes/Cutscene1.0.unity` (enabled, GUID `811be01558705b644a273b4054817722`) once, after the old intro.
- `Tests/CareerBoardRegression/Program.cs`: the expected Chapter 1 scene is now `Cutscene1.0`.

Not changed: `Assets/Scenes/Cutscene1.0.unity`, `Assets/Scenes/Level1_Character_Intro_Cutscene.unity`, `Assets/Cutscenes/Script/Level1CutsceneController.cs`, and every Level 2 file.
