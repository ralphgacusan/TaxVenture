# Fresh menu, tutorial settings and Level 2 lighting — 2026-10-20

User screenshot/request follow-up. Implemented; internal checks completed separately from user Unity visual/device acceptance.

## Current behavior

- Before the first saved Level 1 finish, the menu exposes only Play and Settings. Tutorial and side-quest objects are hidden at runtime, with their authored active states retained for the full menu. The tutorial binder also rejects direct/stale opening events while locked.
- Menu settings hide Reset Progress in that first-run state and centre Quit Game in its place. Reset becomes available after the first Level 1 finish. Intro-viewing and tutorial history alone do not unlock the full menu.
- ProgressChanged refreshes menu visibility, including immediately after Reset. Settings' restore animation does not resurrect tutorial/side-quest objects after a reset. Action visibility/layout is refreshed whenever settings open.
- Leave to Menu explicitly clears pending progress/settings entry requests. ConsumeOpenLevelSelect additionally rejects a stale progress-view request before Level 1 has been finished. Therefore leaving an unfinished first attempt returns to the fresh main menu, while existing completed careers keep their normal progression.
- Shared secondary action faces are brighter: muted terracotta (148,88,80) for Reset/Quit; slate blue (68,94,122) for Replay/Menu. Existing buttons, geometry, press/hover animation, audio and confirmations remain.
- A TrainingSession practice run shows REPLAY TUTORIAL, RESUME TUTORIAL and TUTORIAL PAUSED, plus tutorial-specific replay/leave confirmations. Ordinary story-level settings retain level wording, because their replay action restarts the whole level. Restart preserves the drill and guided/mastery session as before.

## Level 2 lighting

The saved Office_Level2 scene has two active all-layer directional lights: Env/Directional Light (intensity 1.15, warm) and Environment/OfficeBuilding/Inside/Directional Light (2) (intensity 1, white). The other three saved directionals are inactive. Both active lights illuminated the outdoor toolkit, contributing to washed-out paper surfaces.

Level2OfficeStart.BalanceLighting adjusts just those two scene-owned lights at startup:

- Daylight key: intensity 0.95; colour (1,0.97,0.92); shadow strength 0.7.
- Office fill: intensity 0.28; colour (0.92,0.95,1).

Sky/ambient lighting remains available for readable shadows. No global RenderSettings, shared lighting asset or Level 1 scene changes are made. ToolkitBuilder.Lit now creates matte paper/plastic (zero smoothness/glossiness and specular highlights disabled where supported) so bright direct light does not create glare over the printed surfaces. Existing collection-paper self-light is retained for grazing-angle readability.

These values are a bounded, moderate starting balance; rendered exposure and indoor/outdoor feel still need the user's Unity check. Internal tests do not establish final visual quality.

## Changed runtime and tests

- Assets/Scripts/MainMenu.cs — authored menu-object state, fresh visibility and reset refresh.
- Assets/Scripts/UI/MenuEntry.cs — shared career access and stale progress-entry guard.
- Assets/Scripts/Settings/SettingsBoard3D.cs — brighter faces, fresh action layout, tutorial labels/confirmations, safe button restoration.
- Assets/Scripts/Settings/InGameSettings.cs — explicit main-menu exit flags.
- Assets/Scripts/Training/TrainingBinder3D.cs — first-finish access gate.
- Assets/Scripts/Level2/Level2OfficeStart.cs — scene-local lighting balance.
- Assets/Scripts/Level2/AnnotationPaper.cs — matte toolkit material setup.
- Tests/SettingsRegression/Program.cs — fresh visibility/restore/exit and tutorial-copy contracts (133 checks).
- Tests/TrainingRegression/Program.cs — locked binder entry contract.
- Tests/Level2Regression/Program.cs — bounded key/fill intensity, scene-local scope and matte material contracts (1,283 checks).

No serialized scenes/prefabs or shared assets were edited; no new Unity script/metadata is needed. Text stays UTF-8/CRLF. Scratch compile: 0 errors / 29 baseline warnings. Regression results: Settings 133/133, Training 514/514, Level2 1,283, IntroCutscene 43, CareerBoard 72, Guide 733, Controls 5,734, Progression PASS and Dialogue 799. Both Level 2 validators PASS (scene validator retains its two existing warnings). Guide targets: 17 paths, zero missing.

## User Unity checklist

1. Reset an existing career. After the board lifts, only Play/Settings remain. Reopen settings: no Reset, Quit centred. Close settings repeatedly and confirm no extra buttons reappear.
2. Fresh Play, watch the intro, leave unfinished Level 1: the main menu still has Play/Settings only. No progress-board camera appears. Play still routes straight to the intro/Level 1, with the previously established Skip policy.
3. Finish Level 1: Continue reveals progress. Returning to the title menu makes the normal tutorial option available, and menu settings include Reset again.
4. Enter a tutorial practice run: settings say Replay Tutorial/Resume Tutorial; replay and leave confirmations use tutorial wording. Replay keeps the same drill/mode and career data.
5. Assess brighter settings actions in the menu and both levels, including touch feedback and cancellation.
6. Level 2: compare daylight outside, the office interior, the payslip/tutorial paper, cards and collected-document view. Printed text/highlighter colours should stay distinct, white surfaces should not glare, and shaded areas should remain readable. Level 1 should keep its previous lighting.


## Compact fresh-menu spacing — 2026-10-20

Screenshot follow-up: hiding Tutorial left an empty row between Play and Settings. MainMenu now moves the complete Settings object (including its colliders) to Tutorial's authored row while the career is fresh. It preserves Settings' authored horizontal/depth coordinates and rotation. After Level 1 is finished, its original position is restored. Reset refreshes the compact placement, and settings pop animations only change scale, so closing/reopening settings preserves the tighter spacing. No scene/prefab changes.

Verification: scratch compilation and existing SettingsRegression checks; user visual check pending. Confirm fresh Play/Settings are adjacent, closing settings retains the compact placement, and the full menu restores the original three-row layout after Level 1 completion.


## Corrected campaign/menu gate — 2026-10-22

This supersedes the campaign-creation gate introduced by the later T03 menu work. New Game, Continue, Tutorials and menu Reset require the existing story Level 1 completion marker to have been successfully saved. Campaign creation, intro entry/viewing, XP and mastery completion do not grant access. Play reuses an interrupted campaign: restart the unfinished opening unskippably, or enter unfinished Level 1 directly once the opening is complete, preserving existing restart/checkpoint rules. No progress-module detour.

The launch owner now locks before the atomic New Game publication event; both menu observers keep the displayed layout stable until scene exit. Settings duplicates discard their inherited Settings callback and use the existing physics-touch press/action path. Editable labels use the existing Minnie font/material, while the paper/pin/materials/colliders come from the actual Settings object. Results remain on screen if saving fails; the menu gate uses only the last successfully saved completion snapshot.

See [current menu behavior, changed files, internal evidence and pending manual checklist](../../docs/menu-integration.md). All Unity/visual/animation/audio/device checks remain pending with the user. Earlier verification counts above are historical and are not claims about this correction.


### Settings lettering consistency — 2026-10-22

Play, New Game and Continue now use matching beveled 3D Minnie glyph meshes, replacing the flat TMP path described above. The style builder reproduces all 35,412 original Settings text vertices within 0.00001 units before creating the additional words. Each duplicate keeps Settings' actual text renderer, transform, material and texture; only glyph geometry changes. Original Settings/button/font/texture assets remain unchanged. See [the current menu report](../../docs/menu-integration.md) for changed files, internal evidence and pending user visual acceptance.


### Authored menu objects and shared press - 2026-10-22

All five corkboard menu actions now use the saved scene objects, including the existing Play object. The runtime Play replacement has been removed. Each action completes the same press-and-return animation before opening its overlay/camera/scene, with a single input owner and immediate repeated-activation lock. The fresh Play/Settings layout stays stable through the press and launch. Only successful saved Level 1 story completion reveals New Game/Continue/Tutorials after results -> progress -> Back. See the [current menu report](../../docs/menu-integration.md) for files, internal results and pending user Unity acceptance.


### Standalone confirmation and menu motion - 2026-10-22

New Game now shows the existing animated confirmation alone with the settings slab hidden. Settings no longer creates Reset Progress; in-game Replay remains. Play and Continue use the settings component's existing staggered note-disappearance and start their route from its completion callback. Continue retains hidden notes until progress Back; failed starts restore them. All five notes have subtle 0.65-degree runtime sway, with authored rotations restored on disable. See [current menu evidence and pending manual checks](../../docs/menu-integration.md). This supersedes earlier references above to Reset being available after Level 1.
