# Known issues and limitations

## Not verified in Play Mode
Every visual and behavioural result is internally checked only: compile, regression suites and static scene checks. Placements were computed from the scene file's transforms and the meshes' geometry, without seeing the scene. Check the items in `PROGRESS.md` first. Marker positions are the quickest fix: move them under `Level 1 NPC Life`.

## Unity must reload the scene
`Office_Level1.unity` was edited on disk. If the editor had it open, accept the reload prompt. Saving the open scene before reloading would overwrite the changes (a backup of the pre-change scene is in the agent scratchpad, not in the project).

## No clips for sitting, talking, buying or turning
The project has three Humanoid idles and one Humanoid walk only.
- **Procedural instead:** sitting, reaching (browsing), the phone call and pair chats are procedural layers (`NpcBodyPose`, `NpcAnimationDriver`).
- **Turns** are smooth pivots in place, not stepping turns.
- **Talking** is the existing procedural head and chest motion.
- **Sitting:** IK poses are tuned by measurement, not by eye. Hips or feet may need small tweaks:
  - `legLength * 0.12` hip height above the seat;
  - `0.95 * thigh` feet forward;
  - `handsOnLap`.
- **Lorna** browses (looks and reaches) but has no purchase animation and holds no item.

## Static T-pose models (receptionist, Auditor)
These meshes have no skeleton. `StaticArmRig` builds one at runtime and skins a copy of the arm-holding parts.
- **Weights:** they come from the arm span with smooth shoulder and elbow blends. Low-poly shoulders may crease slightly. Tune `armDrop` / `elbowBend` on the component if needed.
- **Hands:** they are not rigged separately (fingers stay as modelled).
- **Read/Write:** the models' imports now allow Read/Write (needed in builds). Without it the rig logs a warning and leaves the arms as modelled.
- **Stand-in client:** the inactive conference-room stand-in (`npc_client_static`) has no rig because it is never shown in Level 1.

## Walking
- **Supersedes the old pacing walk:** `Documentation/WorldAnimationAndInteractionOverhaul` still says walking is deferred. That refers to `NpcIdlePacing`'s walk, which stays off; it now also steps aside wherever an `NpcLife` exists. Living Plaza walking is the separate `NpcLocomotion`, which fixes that issue's causes (clip-matched speed, forward-only movement, pivots before sharp turns, Humanoid walk clip through the shared controller).
- **Clip speed:** feet match the walk clip's own speed (measured on first use; a 1.1 m/s fallback before that).
- **Stopping:** the final easing into a stop is short and gentle, so a slight slide may show in the last ~0.3 m.
- **Avoidance** is deliberately simple: wait, then step aside. Someone standing in a doorway for a long time keeps the walker waiting (it never pushes through).
- **Route failure:** if a route can't be planned, the NPC is placed at its destination the next moment neither it nor the spot is on screen (logged). This has not been seen; the route points were chosen to avoid it.
- **Doors:** the front doors open with the side quest (`OfficeDoorGate`); Marie waits in the plaza until they open. Interior doors are already open in the scene.

## Marie
- **Room facing:** she now faces the way the conference room's client spot faces (into the room, toward the door). The old hidden teleport set her root to that rotation, which pointed her model to the south-west.
- **Shared clip:** her plaza idle is the same Cora clip as before, now played from the shared controller.

## Arthur
- **Grounding follow-up:** the floating report led to replacing skinned visibility bounds with actual baked vertices for placement. Unity 6000.4's [BakeMesh contract](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/SkinnedMeshRenderer.BakeMesh.html) specifies renderer-relative vertices and scale compensation with `useScale=true`. Fresh-load placement while the rig is hidden, first animated pose and resumed corkboard placement still need visual confirmation.
- **Default idle:** the end-of-level finale copy (and any copy made without scripts) plays the shared controller's default idle (`Standing Idle`, the same Mixamo idle as Marie's).
- **Static copy:** the static T-pose instance is kept in the scene but inactive, so nothing that referenced it breaks. It can be deleted later in the editor.

## Longer plaza routines
- Milo and Zorbix's longer routes reuse the existing looping stroll. Their greeting uses a procedural arm wave, so its appearance and the route clearances still need Play Mode validation.
- Dante's visits are background gestures, without speech bubbles or player dialogue. A target who is busy, has moved away or is outside the authored plaza boundary is skipped; he tries everyone again on later cycles.
- Authored markers and simple local avoidance are used, not a baked NavMesh. Visual checks should include both characters approaching the meeting area, the bench visitors and the supply counter.

## Page and category animation follow-up
- Deferred page reveal and category scale/fade motion are compile/static-tested only. User Play Mode checks must cover both directions, rapid navigation, closing mid-turn, attached records, and category close/reopen without touch-through.
- Ambient triangle/check completion is scene-local. Reloading the scene offers those optional conversations again; taxpayer quest completion continues to use the existing campaign state.

## Pre-existing, not from this work
- `Tests/VerifyInteractionOutlines.py` fails on `Assets/Settings/PC_Renderer.asset` ("missing outline feature subasset"). It was already known to fail on the renderer outline settings.
- Building the suites with `-o` refreshes `Tests/*/obj` intermediates (no `.gitignore` rule for `obj`). The tracked `Tests/*/bin` outputs were not rebuilt.

## 2026-10-28: page-turn print polish
- Page print fades between 34 and 76 degrees, before it can become an edge-on text column. Cloned UI layout is resolved and restored before animation; the physical leaf has a subtle shadow. Automated checks do not render Unity text or prove visual/touch quality.
- Attached 3D records still hide as a group at 76 degrees rather than individually fading with the cloned page ink. Verify forward/back turns, rapid taps and closing mid-turn in the folder and tax book, including both levels.
