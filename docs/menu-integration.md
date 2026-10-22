# Main-menu integration -- corrected completion gate (2026-10-22)

Implemented; internally verified. All Unity, visual, animation, audio, touch and device acceptance remains with the user.

## Root cause

The previous menu model used `CampaignSaveState.Campaign` plus `HasCampaign` as the returning-menu gate. `StartNewCampaign` publishes `ProgressChanged` synchronously, before the old launch code set its lock, so Play immediately refreshed into New Game/Continue/Tutorials during the outgoing transition. Play also treated an interrupted campaign as permission to open progress. The copied Settings objects retained a serialized Button callback to `MainMenuUI.OnSettingsPressed`, and their replacement labels selected the generic career/HUD font and manually reconstructed outline styling.

## Current behavior

- Campaign existence and menu access are separate. Only story completion of `Level_01` in `campaign.completedChapterIds`, successfully saved by `PlayerProgressManager`, unlocks the returning menu. Mastery records, XP, intro-viewing history and campaign creation do not unlock it.
- `PlayerProgressManager.HasSavedFirstChapterCompletion` snapshots the completion gate on load and after successful writes/replacements, before notifying observers. An unsaved completion mutation cannot unlock the menu. This adds no save field and uses the existing signed save and atomic New Game operation.
- Before that gate: Play and Settings only, with Settings raised to the Tutorials row. After it: balanced New Game/Continue columns in the top row, Tutorials and Settings below.
- New Game and Continue are saved Settings prefab instances under `Buttons`, visible/selectable in Edit Mode; all five menu actions use the saved scene objects directly. All reuse the complete authored Settings object: the existing FBX blue paper, pin, shared art materials, transforms and collider. Settings itself remains intact. The authored Play object is activated on the fresh route and hidden on the returning route; no replacement is created. Labels now use imported 3D Minnie glyph meshes matching the original Settings font, depth and bevel. Edit Mode binding assigns the existing text MeshFilter matching glyph geometry and records the prefab override. Runtime binds the saved New Game/Continue objects and preserves their authored positions, scales and text edits. No flat TMP label remains. Longer words shrink uniformly only as needed to fit the original bounds. The original Settings object, font and texture assets remain unchanged. Exact rendered correspondence remains a user visual check.
- Copies discard the inherited serialized Settings onClick event and disable the duplicate UGUI Button receiver. MainMenu's existing physics-touch dispatcher owns the actions, shared press animation and action SFX. Inactive controls disable their colliders too.
- Play reuses an existing unfinished campaign and its ID. An interrupted opening restarts directly, unskippable. After the opening has finished, Play enters Office_Level1 directly; it does not clear progress/checkpoints or detour through progress. Level 1 currently has no separate checkpoint writer; its existing restart rules remain in force.
- Launch locks are acquired before campaign publication. MainMenu and the note panel ignore refresh requests while launching. Each note completes its shared press and returns to its authored resting position before its action runs. Play/Continue then reuse the settings note-disappearance animation; its completion callback starts the existing scene/camera transition. There is no separate timing delay. Repeated activation is blocked.
- New Game displays the existing animated confirmation card alone, without the settings board behind it. Settings has no Reset Progress action; in-game Replay remains. Cancel changes no campaign data. Confirm atomically saves first, preserves settings, resets completion, then requests the mandatory opening. Save failure retains the previous campaign and displays the existing retry error.
- Results save before opening progress; failed saving retains the results screen for retry. Successful completion follows results â†’ progress â†’ Back â†’ returning menu. The gate reloads from the same signed story marker after restart.
- Continue opens the existing progress camera only. Shop stays in that module with its existing appearance and unlock policy.
- Menu launch/entry flags reset at Unity subsystem registration, including Play Mode without domain reload.

## Changed files

- `Assets/Scripts/RewardsSystem/CampaignMenu.cs` -- separate campaign existence and completion visibility.
- `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs` -- last successfully saved completion snapshot.
- `Assets/Scripts/UI/MenuEntry.cs` -- launch ownership, campaign reuse, direct resume and mandatory opening request.
- `Assets/Scripts/MainMenu.cs` -- launch refresh/input guards and obsolete TMP font binding removed.
- `Assets/Scripts/UI/Menu/MainMenuCampaignPanel.cs` -- Settings copies, matching 3D glyph meshes and inherited callback removal.
- `Assets/Scenes/3D Menu.unity` -- obsolete TMP font reference removed from MainMenu.
- `Assets/Scripts/Cutscenes/Cutscene01Runtime.cs` -- consume mandatory-opening request.
- `Assets/Scripts/UI/LevelCompleteUI.cs` -- refuse results exit on save failure.
- `Tests/CampaignFoundationRegression/Program.cs` -- corrected gate, reload/reset/failure and wiring contracts.
- `Tests/SettingsRegression/Program.cs`, `Tests/IntroCutsceneRegression/Program.cs` -- update source contracts for the new guards.
- This document and `Documentation/SettingsActions/FreshMenuAndLighting.md` -- corrected current routing and gate.

## Internal verification

The updated unfinished-campaign test first failed against the previous implementation (93/94), then passed after the fix. Campaign Foundation: 105/105; Settings: 133/133; Training: 514/514; Career Board: 72/72; Intro Cutscene: 44/44; Progression: PASS. Scratch runtime compilation: 0 errors, 29 existing warnings. These include pure C# model/serialization and isolated atomic-save fixtures plus source/scene wiring contracts; they do not establish rendered appearance or live Unity behavior. Fixtures use isolated test directories, not real player saves. No Git commands or Unity launches were used.

## User manual checklist -- all pending

1. Fresh menu shows only Play and Settings.
2. Play animates and enters the opening without a button-layout flash.
3. Stop during the cutscene and restart: still Play and Settings; Play reuses the campaign ID and restarts the opening.
4. Unfinished Level 1 does not unlock returning-player buttons; Play follows existing restart/checkpoint rules directly.
5. Complete corrected Level 1 â†’ results â†’ progress â†’ Back: New Game/Continue and Tutorials appear. Inject a completion-save failure and confirm no false unlock or results exit.
6. Restart after completion: returning-player layout remains.
7. New Game Cancel preserves progress; Confirm resets correctly, preserves settings and enters the unskippable opening. Inject a New Game save failure and confirm the previous campaign remains recoverable.
8. All buttons reuse the original Settings artwork and interaction styling; verify label readability, balanced columns, press feedback, SFX, touch reachability and absence of inherited Settings actions.


## Blank/tiny note label follow-up — 2026-10-22

The user screenshot after the first correction showed a blue Play note with no readable label. The new direct TextMeshPro construction had omitted `isOrthographic = true`, which the existing `CareerBoardKit.Text` helper sets for design-unit sizing. The installed TMP implementation (`Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/TMP/TextMeshPro.cs`, `orthographicMultiplier`) uses 0.1 without that flag: the generated lettering was one-tenth its intended size. The previous static checks verified the font/material references but missed that sizing contract.

`MainMenuCampaignPanel.Letter` now sets the same design-unit sizing flag, creates its RectTransform explicitly, initializes TMP before assigning the final world pose/scale, disables label shadow casting/receiving as the existing helper does, and builds the text mesh explicitly even while the copied note is inactive. Paper, pins, font/material assets, menu gate and save routes are unchanged by this follow-up.

Changed files: `Assets/Scripts/UI/Menu/MainMenuCampaignPanel.cs`, `Tests/CampaignFoundationRegression/Program.cs`, and this report. The added source-contract regression failed first (105/106), then passed after the correction (106/106). Scratch runtime compilation: 0 errors, 29 existing warnings. These are internal checks; Unity rendering, label placement/readability and interaction acceptance remain pending with the user. No Git command, Unity launch or real player-save modification was performed.


## Settings typography consistency — 2026-10-22

The next user screenshot confirmed that flat TMP lettering still differed from the existing Settings button. The direct TMP path has now been removed entirely for Play, New Game and Continue. The original Settings lettering is beveled/extruded geometry from `settings_button.fbx`.

New `Tests/BuildMenuActionLettering.py` derives labels using the existing `Assets/fonts/Minnie.ttf`: font size 0.904, curve resolution 12, bevel depth 0.018 and bevel resolution 4. Before exporting anything it rebuilds Settings and compares all 35,412 vertices against the original mesh. The maximum nearest-vertex difference was 0.000007012 (threshold 0.00001), verifying the original font and 3D style directly.

New `Assets/Resources/MenuActionLettering.fbx` and its unique `.meta` contain the additional glyph meshes. The runtime loads these and exchanges each duplicate's existing text MeshFilter geometry. It keeps Settings' actual text renderer, shared material, texture, transform and lighting response. Labels sample an existing opaque-white region of the original texture. Longer words shrink uniformly to fit the original text bounds. No original button/font/texture is modified. MainMenu's obsolete serialized TMP font binding was removed from source and scene.

Internal verification: source geometry/style builder PASS; Play/New Game/Continue geometry fit checks PASS; original/new FBX coordinate axes and unit scale match; Campaign Foundation 106/106; scratch runtime build 0 errors / 29 existing warnings. No Unity launch, capture, Git command or real player-save change. Unity import and rendered appearance checks remain pending with the user.

Changed in this follow-up: `Assets/Scripts/UI/Menu/MainMenuCampaignPanel.cs`, `Assets/Scripts/MainMenu.cs`, `Assets/Scenes/3D Menu.unity`, new `Assets/Resources/MenuActionLettering.fbx` and `.meta`, new `Tests/BuildMenuActionLettering.py`, `Tests/CampaignFoundationRegression/Program.cs`, this report and the SettingsActions note. Earlier TMP implementation notes above are historical and superseded by this section.


## Scene-editable New Game and Continue — 2026-10-22

The user requested normal scene objects like the hand-made duplicate in their screenshots. `3D Menu.unity` now contains saved Settings-model prefab instances named `new_game_button` and `continue_button`, parented under `Buttons`. Each has the original pin/paper/Text hierarchy and a copied root BoxCollider. They are active in Edit Mode so they can be selected, moved, scaled and inspected. Initial positions form a horizontal pair; user edits are authoritative.

A saved `MainMenuCampaignPanel` component on `Buttons` references both objects. It no longer constructs, reparents, sizes or positions either note at runtime. It still owns the shared press and campaign visibility gate: both notes remain hidden during gameplay until saved Level 1 completion. The original fresh-menu Play adaptation remains as before.

`[ExecuteAlways]` label binding applies the existing imported 3D NewGame/Continue meshes in Edit Mode, preserving the copied Settings text transform/material; the prefab overrides are recorded and the scene is marked dirty for the user's normal Save. The binder only converts untouched Settings/Play glyphs; subsequent custom mesh/transform edits are retained. It never runs menu visibility or press animations in Edit Mode.

New `Assets/Editor/MainMenuButtonAuthoring.cs` handles an already-open scene after script import: it reuses existing `new_game_button` / legacy `newgame_button` duplicates and existing Continue, creates only missing notes, binds the panel references, and applies the labels. It preserves the user's existing placement, including unsaved duplicates from the screenshot. It removes an inherited OnSettingsPressed Button receiver from action duplicates so physics-touch input remains the single action owner. It does not save/reload the scene or touch player progress. The same idempotent binding command is available under Tools > TaxVenture > 3D Menu > Bind Editable Story Buttons.

Select `Buttons/new_game_button` or `Buttons/continue_button` in Edit Mode to adjust the complete note/collider. Expand their Text children to adjust lettering placement/scale. Save the scene normally. The geometry remains baked 3D lettering; the matching word meshes are assigned by the binder, rather than a TMP text field. Runtime recognizes the screenshot's `new_game_button` name and the older `newgame_button` spelling. New Game still opens Settings' reset confirmation, and Continue still opens saved progress.

Changed files for this follow-up: `Assets/Scenes/3D Menu.unity`, `Assets/Scripts/UI/Menu/MainMenuCampaignPanel.cs`, `Assets/Scripts/UI/MenuEntry.cs`, `Assets/Scripts/MainMenu.cs`, new `Assets/Editor/MainMenuButtonAuthoring.cs` and `.meta`, `Tests/CampaignFoundationRegression/Program.cs`, new `Tests/VerifyEditableMenuButtons.py`, and this report.

Internal evidence: saved-scene validator PASS (unique IDs, Settings source-prefab reuse, colliders, hierarchy, panel references, no runtime pair generation/layout); Campaign Foundation 108/108; Settings 133/133; Intro Cutscene 44/44. Runtime scratch build: 0 errors / 29 existing warnings. Editor authoring scratch build: 0 errors / 6 missing optional-reference warnings. Unity import, rendered appearance, Edit Mode gizmos and live touch behavior remain pending with the user. No Unity launch, Git command or real player-save modification.


## Dark lettering and overlapping editor preview — 2026-10-22

The latest user screenshot exposed two issues in the preceding change. Converted Blender text already contained UV0. The generator appended the white texture sample as UV1; Settings' material reads UV0, so the new geometry sampled black/unrelated texture regions. Reimporting the previous exported FBX confirmed two UV layers per label, with varying default coordinates in UV0 and the intended constant white sample in UV1. Geometry-only validation had missed this material/mesh contract.

The generator now removes the converted text's UV layers before creating the single intended UV0. It reimports the exported FBX and verifies all three meshes have exactly one UV layer, with every UV0 coordinate sampling the existing texture's opaque-white region. `MenuActionLettering.fbx` was rebuilt, retaining its .meta GUID and the Settings font/material/style.

The original Play note was also active behind the returning-player pair in Edit Mode. The saved scene and already-open-scene authoring helper now show the returning-row preview with Play inactive. This does not reposition the user's notes. Runtime still finds the authored Play object including inactive objects, builds its fresh-menu adapter and uses the saved-completion model to select Play or the returning row as before.

Changed: `Tests/BuildMenuActionLettering.py`, `Assets/Resources/MenuActionLettering.fbx`, `Assets/Scenes/3D Menu.unity`, `Assets/Editor/MainMenuButtonAuthoring.cs`, `Tests/VerifyEditableMenuButtons.py`, and this report. Internal checks: geometry/style and exported-FBX UV0 round-trip checks PASS for Play/New Game/Continue; scene validator PASS including no overlapping Play preview; Campaign Foundation 108/108; editor scratch compile 0 errors / 6 existing optional-reference warnings. All Unity import/render/interaction checks remain pending; no Unity launch, Git command or real-save change.


## Final authored-object menu flow and shared press - 2026-10-22

The user's approved scene objects are now the runtime controls for all five actions. `MainMenuCampaignPanel` serializes Play alongside New Game/Continue, and its entire runtime Play-copy/rename/reposition path has been removed. The scene and editor binding store the existing `play_button` reference. Startup binds the saved objects, derives the press direction from the menu camera even when arriving at the progress camera, and applies the saved-completion model. The original Play geometry, label, materials and authored placement remain intact.

Every corkboard action - Play, New Game, Continue, Tutorials and Settings - now uses the same 0.16-second press-and-return animation. Input is locked immediately, the action executes exactly once after the note returns to its resting position, and repeated activations are rejected. Legacy UGUI Button callbacks on these world notes are cleared/disabled at runtime so they cannot bypass the shared physics-pointer owner. Both touch and mouse work through that owner; touches cannot produce a second mouse action. Child collider hits resolve to the authored note root. Existing action SFX remain owned by their existing commands.

Fresh flow: Play + Settings -> press Play (same visible menu and original Play object) -> opening cutscene directly, unskippable on the fresh route -> Level 1. Save-change menu refresh remains blocked during the press and launch, preventing a layout flash. An interrupted campaign retains the established direct restart/resume rules and campaign ID.

Completion flow: finish corrected Level 1 -> existing results -> successful result save -> existing progress module -> Back -> New Game/Continue + Tutorials/Settings. The menu still reads `HasSavedFirstChapterCompletion`, updated only after successful save/load; creating a campaign, entering/viewing the cutscene, XP or unfinished work cannot unlock it. Save failure leaves access locked and retains results for retry. The completed marker persists across restart. Continue and the Settings confirmation-based New Game route are unchanged.

Changed this turn: `Assets/Scripts/MainMenu.cs`, `Assets/Scripts/UI/Menu/MainMenuCampaignPanel.cs`, `Assets/Editor/MainMenuButtonAuthoring.cs`, `Assets/Scenes/3D Menu.unity`, `Tests/CampaignFoundationRegression/Program.cs`, `Tests/SettingsRegression/Program.cs`, `Tests/TrainingRegression/Program.cs`, `Tests/VerifyEditableMenuButtons.py`, and the menu/SettingsActions documentation. No artwork, lettering mesh or player save was changed.

Internal checks: scene/object/press wiring validator PASS; Campaign Foundation 110/110; Settings 133/133; Training 514/514; Intro Cutscene 44/44; Career Board 72/72. Runtime scratch compile 0 errors / 29 existing warnings; editor compile 0 errors / 6 optional-reference warnings. The checks include pure model/save-state cases and source/scene contracts; they do not establish rendered animation/audio/device behavior. No Unity launch, Git command or real player-save modification.

Manual acceptance remains pending: fresh Play/Settings only; all five notes share tactile feedback; Play stays the same object/layout throughout its press before the opening; interrupted intro/unfinished Level 1 stays fresh; corrected Level 1 -> results -> progress -> Back reveals the returning row; restart preserves it; Continue opens progress; New Game Cancel/Confirm/failure retain the established behavior. These latest notes supersede the historical runtime-Play-adapter notes above.


## Standalone New Game card, shared disappearance and subtle sway - 2026-10-22

New Game no longer calls `SettingsBoard3D.Open`. It activates the existing confirmation card under the overlay root, hides the settings slab/rows/title/back button as a group, and retains the same 0.22-second EaseOutBack pop-up and original confirmation text/buttons. Cancel closes the standalone card and restores the notes; confirming uses the same atomic fresh-campaign save and unskippable opening. Save failure keeps the standalone card open for retry. Closing restores the card's normal parent while the overlay root is inactive, so later Settings opens normally without a settings-board flash.

Settings no longer constructs Reset Progress, and the internal Reset settings action has been removed. Menu settings retain Music, SFX, Quit and Back; Quit is centred. In-game Replay/Back to Menu/Resume remain available. The save owner's reset/fresh-campaign APIs remain unchanged.

Play and Continue still complete their shared press first, then call `SettingsBoard3D.HideMenuForTransition`. This reuses the exact existing menu-note pop-out implementation (small swell, shrink and stagger), rather than a new animation approximation. Its single callback runs only after all scheduled pops finish. Play then saves/starts the opening; Continue opens the saved-progress camera. Notes stay hidden under progress ownership and pop back on Back. A failed Play save restores notes for retry. Input and visibility observers respect that ownership so notes cannot reappear mid-transition. Refresh listeners are restored when the main camera/component is enabled by Back.

All five authored notes have a 0.65-degree idle sway, following the career papers' slow sine pattern with staggered phases and a 3.8-second period. It rotates around each paper's face normal, does not scale/translate the authored layout, runs only in Play Mode, and restores rest rotations/press positions on disable. The existing `MainMenuCampaignPanel` inspector exposes Idle Sway Degrees (0 disables it). Pop animations retain their own scale ownership.

Changed: `Assets/Scripts/Settings/SettingsBoard3D.cs`, `Assets/Scripts/MainMenu.cs`, `Assets/Scripts/UI/Menu/MainMenuCampaignPanel.cs`, `Tests/SettingsRegression/Program.cs`, `Tests/CampaignFoundationRegression/Program.cs`, this report and the SettingsActions note. No source art, scene layout, lettering or save files were modified.

Internal verification: runtime scratch build 0 errors / 29 existing warnings; editor authoring compile 0 errors / 6 optional-reference warnings; Campaign Foundation 112/112; Settings 138/138; Training 514/514; Intro 44/44; Career Board 72/72; editable scene/wiring validator PASS. These verify compilation, pure campaign cases and source/scene contracts, not rendered motion or audio. No Unity launch or Git command; no real player-save changes.

User manual checks remain pending: New Game shows only the original popping card; Cancel restores notes and Confirm/failure retains the established save behavior; Settings has no reset and its normal opening still works after cancelling New Game; fresh Play and returning Continue pop all visible notes away before transitioning; progress Back pops them back; rapid taps do not duplicate commands; all five notes sway gently without altering saved placement or colliders; stopping Play Mode restores authored transforms.
