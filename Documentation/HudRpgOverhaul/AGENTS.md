# Agent instructions — HUD RPG overhaul

Before any work, read `README.md`, `PROGRESS.md`, `KNOWN_ISSUES.md` and the active phase file in `phases/`.

- Scope: `Assets/Scenes/Office_Level1.unity` HUD only. Do not edit `Documentation/WorldAnimationAndInteractionOverhaul` or `Documentation/ImmersiveInteractionOverhaul`.
- UI/UX only: do not change reward maths, objective logic, FSM transitions, save data or NPC/3D content. Gameplay classes may receive only read-only accessors or notification events.
- Build HUD UI at runtime under `OfficeHudRuntime`. Hide legacy objects at runtime; never delete them or rewrite scene YAML.
- Show only real data (see the README audit). Never hardcode or fake progress.
- One phase at a time. After each phase: scratch compile (`python Temp/HudOverhaul/make_csproj.py`, then `dotnet build Temp/HudOverhaul/HudOverhaul.csproj --no-restore`; the baseline is 0 errors / 23 warnings), the three `Tests/*Regression` suites (`dotnet run --no-restore`), the HUD logic check (`Temp/HudOverhaul/HudCheck`, `dotnet run`), and update `PROGRESS.md` and the phase status.
- The user performs all Play Mode / device checks and all git actions. Never claim Play Mode results you did not receive.
- Keep line endings CRLF; give new scripts a `.meta` with a fresh unique GUID.
- Level objectives are user-editable data at `Assets/Resources/Levels/Level_01/objectives.json` (main objectives, then steps with `event:`/`state:`/`area:` triggers; see `ObjectiveTriggers`). Do not rewrite the content without the user's request. Keep `TaskListProvider.GetBuiltInObjectives` in sync as the fallback (the HUD check verifies they match). `TaskListProvider.GetTasks()` is the unchanged legacy list for other scenes.
