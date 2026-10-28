# Agent instructions: Level Guide & Tutorial

Before you change anything, read `README.md` (the feature and JSON reference), `PROGRESS.md` (status and verification) and `KNOWN_ISSUES.md`.

## Scope and rules
- The feature lives in `Assets/Scripts/UI/Guide/`, `Assets/Scripts/UI/OfficeMinimapObjective.cs`, `Assets/Resources/Tutorials/Level_01/guide.json` and `Assets/Resources/Tutorials/Portraits/`. Outside those it changes `OfficeMinimap.cs` (creates and ticks the objective layer), `HudProfileCards.cs` (read-only `IsShowingCards`) and `FirstPersonCameraController.cs` (ignores the mouse jump after a cursor relock).
- **TaxVenture is a mobile game.** Write all tutorial text for touch (joystick, swipe, tap). Never mention the mouse, WASD or clicking.
- The user wants important steps to be hands-on: explain, then a `waitFor` page where the player does it.
- It is **observation only**: never change rewards, objectives, the FSM, save data or NPC rules. If you need a new signal, add a read-only accessor or a notification event (the same rule as the HUD initiative), or poll existing public state.
- Build UI at runtime (`GuideRuntime`). Never rewrite the 4.5 MB scene YAML and never delete legacy objects.
- **Content belongs in `guide.json`**, not in code. Add new trigger kinds to `GuideTriggers` (parsing) and `GuideDirector` (evaluation), add new targets to `GuideTargets`, then document both in `README.md` and the JSON `_triggers` / `_targets` notes.
- Keep the waypoints in sync with `Levels/Level_01/objectives.json`: every guided step needs a waypoint. User revision 2026-10-27: `event:AllProfilesCollected` intentionally has none, so exploration never points to the four ID-giving NPCs. Preserve both Arthur routes. `Tests/GuideRegression` enforces this.
- Keep CRLF line endings. Every new script needs a `.meta` with a fresh unique GUID.
- **The user performs all Play Mode and device checks and all git actions.** Never claim Play Mode results you did not receive.
- Do not edit the other initiatives' documentation folders. You may read them for context.

## Checks to run after every change
From the project root:
1. Scratch compile: run `python Tests/make_scratch_build.py`, then `dotnet build Temp/ScratchBuild/Scratch.csproj`. It builds the game scripts against Unity's prebuilt package DLLs. Unity can wipe `Temp/`, which is why the generator lives in `Tests/`; the old `Temp/HudOverhaul` setup and HudCheck are gone. The baseline is **0 errors / about 29 warnings** (2026-10-16), none of them from Guide files.
2. `cd Tests/GuideRegression && dotnet run`: the vocabulary, guide.json and town.json checks (644 at the time of writing). They need step 1 first, because they reference `Temp/ScratchBuild/Temp/bin/Debug/Assembly-CSharp.dll`.
3. `python Tests/VerifyGuideTargets.py`: every scene path in guide.json must exist in Office_Level1.
4. The other suites: `Tests/ControlsRegression`, `Tests/DialogueRegression` and `Tests/ProgressionRegression` (`dotnet run --no-restore`), and `Temp/HudOverhaul/HudCheck` (`dotnet run`).
5. Update `PROGRESS.md`.

## Useful facts
- **Detect actions by state, not only events.** Some `GameplayEvents` fire only from the old click interactables (e.g. `CaseFolderOpened` is not raised by dragging the folder). Pair an event with a state condition, e.g. `event:CaseFolderOpened|active:world:.../OpenCaseFolder`.
- Aim at **renderer bounds**, not transform pivots (the Desk's pivot is in the corridor).
- Key scene paths:
  - Plaza NPCs: `Environment/Outside/Plaza/NPCS/NPC_{Marie,Zorbix,Jhun,Milo,TutorialGuide}`
  - Rooms: `Environment/OfficeBuilding/Rooms/{Reception/Receptionist, Reception/Doors, ConsultantOffice/Desk, AuditOffice/Auditor}`
  - Desk items: `ConsultantOffice/Desk/StackedCaseFolder/CaseFolder`, `.../StackedTaxCodeBook/TaxCodeBook`, `ConsultantOffice/InteractiveFolderObjects/OpenCaseFolder`
- HUD paths are under `UI/HUD_Canvas`:
  - Icons: `PermanentHUD/IconButtons/{NotesIconButton, CaseFolderIconButton, TaxpayerProfileIcon}`
  - Settings: `PermanentHUD/Settings/SettingsButton`
  - Mobile controls: `MobileControls/{Joystick, LookJoystick, MobileLookArea}`
  - The minimap panel is the runtime object `Office Minimap HUD`.
- Camera modes: `FirstPerson`, `Workstation` (desk), `CorkboardSideQuest`, `ProfileViewer` (paper windows) and `Interview`. The Office HUD is visible only in FirstPerson and ProfileViewer.
- Do not change `Time.timeScale` to pause for the tutorial: the HUD hides when timeScale is 0. `GuideDirector.HoldControls` disables `PlayerMovement`, `FirstPersonCameraController` and `Interactor` instead, and it defers the release until any conversation that started meanwhile has ended.
