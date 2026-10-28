# Agent instructions: Level 1 Living Plaza

Read `README.md`, `PROGRESS.md` and `KNOWN_ISSUES.md` before changing anything.

## Scope and rules
- **Scope:** Level 1 only (`Office_Level1.unity`), plus the shared page-turn fix in `CaseFolderUI` / `TaxCodeBookUI` (Level 2 uses those too).
- **Protected characters:** the four side-quest clients (Marie, Jhun, Milo, Zorbix), Arthur, the receptionist and the Auditor.
  - Never replace, redesign, repurpose or duplicate their models.
  - Never reuse their models for ambient NPCs.
  - Components, rig/Animator setup and behaviour may change.
  - Arthur's rigged twin is the same mesh; keep it his.
- **Rewards and progress:**
  - Only the four clients drop profiles/IDs.
  - Ambient NPCs are talk-only: no rewards, saves, events or quest state. User revision 2026-10-27: all plaza people now share triangle/check conversation markers; ambient completion is scene-local presentation only. `Tests/Level1LivingPlazaRegression` enforces this.
- **Quest moves have one owner:**
  - Marie: `ClientConsultation`.
  - Arthur: `TutorialNpcController`, triggered by `ArthurOpening`.
  - Arrival is presence, never progress.
  - Never let an ambient activity pull an NPC off a quest position.
  - Never teleport in view. The only fallback is `NpcLocomotion.StageWhenUnseen`.
- **Planning:** plan paths when a trip or leg starts, never per frame. Prefer authored stops, with doorway pairs on both sides of every door.
- **Animation:**
  - Keep the cutscenes' shared clips untouched (`Elijah_Walking.fbx` stays `loopTime: 0`; the controller loops it).
  - New clips go into `NpcHumanoid.controller`, and only Humanoid clips.
  - Procedural poses (sit, reach, phone) stay subtle. Prefer a calm fallback to a forced pose.
- **Text:** all player-facing text is for touch (no click, mouse, WASD). Ambient talk never names a taxpayer classification and is never a lecture.
- **Files:**
  - Scene and Unity YAML files are LF; scripts and JSON in this repo are LF too.
  - Every new asset needs a `.meta` with a fresh, project-unique GUID (`python Tests/VerifyLevel2Assets.py` checks uniqueness).
  - Keep `Arthur Start Placement` as the last entry in the scene's `SceneRoots`; the Campaign Foundation suite checks it.
- **The user runs Play Mode and all git commands.** Never claim a Play Mode result you did not receive.
- Do not edit other initiatives' documentation; read them for context.

## Checks to run after a change
From the project root:
1. `python Tests/make_scratch_build.py`, then `dotnet build Temp/ScratchBuild/Scratch.csproj`. Expect 0 errors and the 23-warning baseline (none from this initiative).
2. Never build suites in place: `Tests/*/bin` is tracked in git. Run each suite with:
   - `dotnet build Tests/<S>/<S>.csproj -o Temp/CampaignChecks/<S>/Debug/net10.0`
   - then `dotnet <S>.dll` from that folder.

   Suites: `Level1LivingPlazaRegression` (this initiative), `CampaignFoundationRegression`, `DialogueRegression`, `GuideRegression` and `CampaignFoundationRuntimeRegression` (both need step 1 first), and `Level2Regression`.
3. `python Tests/VerifyGuideTargets.py`, `python Tests/VerifyOfficeNpcMarkers.py`, `python Tests/VerifyLevel2Assets.py`.
4. Update `PROGRESS.md`, and `KNOWN_ISSUES.md` if something new is found.

## Useful facts
- NPC roots are on layer 3. Plaza NPCs: `Environment/Outside/Plaza/NPCS/NPC_*`. Ambient NPCs: `Environment/Outside/Plaza/AmbientNPCs/Ambient_*`. Markers: `Level 1 NPC Life/*`.
- Plaza models sit rotated inside their roots (Marie and Jhun about 141°). Always face the *character* (`NpcLocomotion.FaceRotation`, `NpcAnimationDriver.FacingOffset`), never the root's +Z.
- `GuideGridPath` ignores colliders named "door" (up to two parents up) and every NPC collider. That is why each doorway is crossed between an authored outside and inside point.
- The conference room's stand-in client (`ConferenceRoom/Client`) is inactive in the scene. Marie's room spot is its transform.
- The receptionist's hand-over is restored after the consultation record during a resume. `ClientConsultation` waits for `Level1FieldCheckpointer.Active.IsReady` before placing Marie.
- Heredoc Python in Git Bash can swallow backslashes. Write patch scripts with the Write tool.
