# T02/T03 follow-up — size, turning, walk timing and split pilot body

11 November 2026, Codex. User supplied two screenshots after the first repair: oversized outdoor Elijah and a split/displaced Arthur in the pilot, plus reported turning and excessively fast leg swings. **The first repair did not pass user-rendered acceptance.** This follow-up repairs T02/T03 only; no T04 work, Git, Unity launch/automation, packages, original-model/importer edits or real-save access. No new asset or GUID is required.

## Evidence and corrected diagnosis

- Read the latest existing Editor.log without launching Unity. It reports Elijah's pre-follow-up fit as **5.946 m → 1.573 m**. The scene's actual player capsule is **1.6 × 0.7 = 1.12 m** tall in world space. Using the conversation actor's 1.573 m target made the exploration body approximately 40% taller than that capsule.
- C's empty import is now resolved structurally: read-only `read_imported_model.py Library/Artifacts/1a/1a66339397717cd13042800917e51df9` reports **4382 vertices / 5474 triangles, nine skins, populated root/bone/avatar/clip references**, exit 0. The latest Editor log records successful silhouette writes. This is native imported-data evidence, not acceptance of deformation or rendering.
- **A more specific pilot cause was missed previously:** `GetHumanPose` reports world-space body position/rotation (position normalized by avatar human scale), while `SetHumanPose` expects normalized root-local body data. Calibration and finger curl passed those values back unchanged. A translated/scaled/rotated bay therefore moved/rotated the *whole skeleton* during finger-only edits. Primary contracts: [getter](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/HumanPoseHandler.GetHumanPose.html), [setter](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/HumanPoseHandler.SetHumanPose.html). A pre-fix numerical repro at root X=7, scale=.25, humanScale=3.5 moved the body centre from **(7, .875, 0)** to **(8.75, .21875, 0)**. This can explain separated parts relative to their renderer objects; the screenshots are the rendered failure evidence.
- Both builders also rescaled the imported Animator root after initialization; Arthur's `HumanPoseHandler` was constructed before the final scale. Fitting must precede final animation/pose binding. The previous arithmetic checks did not exercise that native lifecycle.
- Imported source clips have 130 Humanoid bindings and no generic rotation/position/scale curves. Elijah's own walk is **1.133333 s** and advances normalized RootT.z by **1.631240** per cycle. The shared idle's root yaw varies only a few degrees; it is not an authored full-spin clip. The old stride shrank with visual fit while traversal remained 3.5 m/s, demanding rapid repetitions of a walk-only clip. Explicit SetTime sampling also left native clip advancement enabled and evaluated another dt, making sample times dependent on frame dt.

## Repair

- T02 converts normalized world body data into the setter's root-local frame before every HumanPose round-trip. All finger edits use one writer that preserves pelvis world position, rotation and local scale exactly; finger curl cannot relocate the actor. Scale is applied to the visual parent, the Animator is rebound, and only then is the hand-pose handler created. Recalibration after a facing adjustment rebuilds that handler too. Original synchronized timeline, hand IK and folder attachment/release remain.
- T03 sizes the outdoor visual from the unchanged capsule's world height (**1.12 m** here). Parent scale/tilt is cancelled before initial binding; fitting is applied to the visual parent, not the imported Animator root, and the Animator is rebound at the final scale. Grounding is measured after that rebind. The existing camera's outdoor pivot uses the body's chest point; indoor first-person height/defaults are unchanged.
- The visual's facing turns toward actual planar travel at a bounded rate and holds its last heading while stationary. It does not inherit incidental player yaw each idle frame. Task/conversation returns reset the travel origin; teleports/recovery cannot create a giant walk step. Existing body/hands/paper visibility ownership is retained.
- Idle/walk playable speeds are zero because explicit timestamps own the clock. Poses evaluate at zero delta. Travel distance advances the walk phase against the existing traversal pace; ordinary full walking is approximately the source clip's rate, sprint approximately 1.4×, with a 1.5× ceiling against spikes. Player movement speed, capsule, Rigidbody and jump physics are unchanged; `PlayerMovement.WalkSpeed` is read-only.
- Both actors check posed bounds **after animation/calibration**, reporting a lost fit or widely separated parts. This guard is deliberately beyond checking whether a GameObject or mesh exists. These native guards have not been executed by AI.

**Remaining animation limit:** with the existing walk-only asset and unchanged fast traversal speed, bounded cadence does not promise exact foot planting at sprint speed. A final run clip/stride treatment is still needed for final sprint motion; this is separate from delivering a visible, proportionate, grounded body now. No clip, source rig or purchase was substituted.

## Internal checks (fresh)

- Before edits, `python3 -B Tests/VerifyCharacterPlayback.py`: **0/7**, reproducing the unsafe clock/fit-order contracts. After edits: **7/7**. These are wiring checks, not rendered proof.
- `python3 -B Tests/run_level3_repair.py --build-only`: runtime **0 errors / 21 inherited warnings**, complete Editor **0 / 10**, unchanged counts. Current runtime built before dependent suites.
- `python3 -B Tests/run_level3_repair.py --suites --t02 --t03`: **T02 87/87**, **T03 66/66**. Includes nine translated/scaled/rotated HumanPose frame round-trips, capsule size, ordinary/sprint cadence, stationary heading, bounded reverse turn and post-animation fit rejection. T02 harness still has five unassigned JSON-field warnings.
- Disposable mutation restoring the unconverted world-position round-trip: **9 failures**. Normal T02 suite subsequently rerun green; no production file mutated.
- `--suites --level2`: **Level3 2855/2855, controls 5734, Level2 1580 PASS**, with three existing native EventSystem comparer skips.
- `VerifyT02Pilot.py`: **118/118** (including unchanged originals/GUIDs/isolation); `VerifyMobileInput.py`: **7 PASS**; `VerifyGuideTargets.py`: **14 paths / 0 missing**.
- No native rendering/animation, touch, Play Mode, phone or save tests were executed by AI. The screenshots are a user failure report, not approval of this revision. Historical first-repair results remain historical.

Changed production files: `CharacterBodyGeometry.cs`, `ExplorationBody.cs`, `ExplorationRules.cs`, `PlayerMovement.cs` (read-only speed property), `FirstPersonCameraController.cs` (existing outdoor pivot), `T02PilotArthur.cs`, `T02PilotFixture.cs`, `T02PilotRules.cs`. Checks: T02/T03 regression programs and `VerifyCharacterPlayback.py`. All remain LF. Documentation: T02/T03 tickets, PROGRESS, KNOWN_ISSUES, CURRENT_STATE, measurement/repair notes and this record. No scene, prefab, controller, model or `.meta` change in this follow-up.

## Fresh user retest

1. Stop Play, let compilation finish, then start a fresh Play session so final-scale binding initializes again. No rig/scene assembly is needed.
2. In Office_Level1 compare Elijah with the environment/NPCs: smaller grounded body, stable while idle and orbiting. Walk slowly, at full stick and sprint; legs should have a controlled cadence and heading should settle after one turn. Check jump and collision framing.
3. Cross indoors/outdoors, finish/cancel an outdoor conversation, and return from a task/viewer: body/hands/paper should restore without a phase burst or spin.
4. In T02 check A/B/C from the first idle through calibration, walk, reach, grip and release. Head, torso, arms and feet must remain together and correctly grounded in each bay. Try Pause/Step and restart. Any `post-animation body fit invalid` Console entry is a failure to report, not a manual assignment step.

**Status: implemented and internally verified; fresh user-rendered acceptance pending.**
