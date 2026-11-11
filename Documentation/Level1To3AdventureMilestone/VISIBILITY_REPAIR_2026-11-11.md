# T02/T03 visibility repair — 11 November 2026

**Subsequent user review rejected the first repair's scale/animation.** The empty C import is now populated, but the screenshots show split Arthur and oversized/turning Elijah. See the [follow-up diagnosis and repair](CHARACTER_PLAYBACK_REPAIR_2026-11-11.md), especially the HumanPose getter/setter frame mismatch missed here. The results below are historical and do not certify the follow-up's rendering.

Owner: Codex, user-requested repair of implemented T02/T03 only. **Implemented, internally verified; user rendered reacceptance pending.** T04–T21 remain Planned. No Git, Unity launch/automation/import, device access, real-save access, packages, purchases or agents. No scene, original model, original importer, controller, Resources binding, camera writer or player physics asset was rewritten.

## Reproductions before production edits

Read the pack guidance/tickets/current progress/issues and inspected the actual scene, Resources asset, source FBX, import metadata, runtime construction and visibility owners. Read the existing `~/Library/Logs/Unity/Editor.log` without starting Unity. Added a read-only v22 type-tree decoder, now `Tests/T02PilotRegression/read_imported_model.py`, to inspect existing cached imported assets rather than assuming that a GUID or FBX source count proves mesh validity.

| Cached imported asset | Vertices / triangles | Additional evidence |
|---|---|---|
| Arthur A | 5,600 / 3,020 | Nine enabled, active skinned renderers; real root file ID, bone, avatar and clip references resolve |
| Arthur B | 3,030 / 3,020 | Same reference checks pass |
| Arthur C | **0 / 0** | All ten mesh subassets empty; all nine renderer mesh references point at empty meshes |
| Elijah Walking | 4,585 / 2,537 | Nine enabled, active skins; required Humanoid mapping populated (22 mapped body/toe entries; optional eyes/jaw absent), avatar scale 3.520674; clip and root IDs resolve |

Reproduce the cached-import failure (read-only; exit 1 on the pre-repair C cache):

```sh
python3 -B Tests/T02PilotRegression/read_imported_model.py \
  Library/Artifacts/1a/1abe5abaf532f7d4196efb8a07b4be71 \
  Library/Artifacts/70/706747acc6bad0ba8f2e6357a751484b \
  Library/Artifacts/c4/c4813c3ea7c9bcb183c1524a96fa337d \
  Library/Artifacts/bf/bf44068112d939bdb01585a875f105cf
```

These are historical cache paths, not stable asset identifiers. Source labels and artifact SHA-256 values are retained in `Tests/T02PilotRegression/imported_visibility_fixture.json`. The diagnostic also checks actual object classes/IDs, active renderer objects, bone pointers, avatar mapping and clip presence. It does not claim that Unity rendered anything during this session.

## Causes and repairs

**T02 C, zero geometry.** The silhouette importer inherited `isReadable: 0`. The existing Editor log records `Not allowed to access vertices/normals/uv`, bone-weight length errors and failed index writes from `T02PilotTreatment.Treat`; it nevertheless called `mesh.Clear()`. The cached asset independently reproduces the empty result. Both *derived silhouette* metadata files now enable read/write; `OnPreprocessModel` enforces it only in the flagged derived scope. Importer version 2 invalidates the old treatment result through normal Unity import. Guards reject unreadable/empty or inconsistent refinement **before Clear**, and compare actual output counts after writing. Original FBX bytes, original `.meta` files and all existing GUIDs are preserved. CPU-readable silhouette meshes retain a CPU copy: record that memory cost in the pilot sheet.

**T02 A/B apparent absence and stalled contact.** The review initially selected Task, which aimed at the station while Arthur started at the entrance. `T02PilotArthur` waited for an IK sample before calibrating/moving, but its `OnAnimatorIK` was on the *parent*, while the Animator was on the model child. A runtime-only relay is now installed on the Animator object and forwards the callback to the existing actor; the controller's existing IK pass, timeline, finger curl and folder attachment owner remain unchanged. Calibration and synchronized movement can now advance. The initial view is a full-body following Orbit. Explore/Doorway follow Arthur, Hands follows his actual right hand, and Task keeps him framed on entry/exit before closing on the station. All three bays still use the same time and corresponding view offsets.

**T02/T03 fitting and Elijah's outdoor body.** Imported renderer transforms include approximately 60–110× scales, including nonuniform torso scaling. The old fit mixed baked geometry with the full scaled renderer matrix; T03 also measured under the player's scaled/tilted parent. A numerical probe using the *imported* vertex/transform data demonstrates the double-scale hazard: Arthur's ~7.00 m import envelope becomes ~391.93 m; Elijah's ~6.69 m envelope becomes ~859.39 m. Fitting those erroneous envelopes yields centimetre-sized bodies. These are unanimated numerical envelopes, not a native measurement of `BakeMesh(false)` or a render capture.

`CharacterBodyGeometry` now explicitly uses `BakeMesh(true)` and a rigid position/rotation conversion, applying renderer scale once. This follows [Unity's BakeMesh scale/coordinate contract](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/SkinnedMeshRenderer.BakeMesh.html). Arthur is grounded from the fitted geometry. Elijah's visual host cancels inherited parent scale per axis and becomes upright **before** binding/evaluating/measuring. His real geometry is fitted to the conversation Elijah's 1.5726485 m standing height, centred over the capsule, and its lowest point aligned to the capsule bottom. Animation is rebound/enabled, the existing manual idle/walk graph is initialized, and the fitted model-root pose/scale is restored after evaluation so clips cannot overwrite it. Renderer offscreen updates and AlwaysAnimate initialization avoid stale pose/culling dependencies. Runtime validation reports missing/empty geometry, inactive/unbound renderers, missing materials or invalid Humanoid avatars rather than silently treating a bound GameObject as a visible body.

**T03 rejected alternative causes / preserved owners.** Resources model/own-walk IDs resolve to the actual `Elijah_Walking.fbx`; the shared idle still resolves to the NPC idle. No replacement art/binding was needed. The saved player is scaled 0.7, with an active unit-scale Visual child; the helper now compensates visual inheritance without changing the root, capsule or Rigidbody. Main Camera includes layer 0/all layers; the existing bounded collision cast excludes descendants of the player (including this body). Third-person activation already worked, as reported. No legacy `ThirdPersonCameraFollow` is enabled. `SetPerspectiveShown`, task hiding and conversation `HideFor`/`ReleaseHide` remain the authorities; re-enable and destroyed-hider cleanup now reapply that policy. Indoor first person, outdoor hidden camera hands and the existing single-paper hand/home reparenting are unchanged. Level 2/3 adoption defaults are unchanged.

**T02 camera input.** The former input worked only after selecting Orbit, used an emulated mouse for touch, and refused starts anywhere in the 430×330 controls rectangle or 470×420 passive statistics rectangle. At small landscape resolutions that could exclude almost the entire useful drag surface. It also adopted held pointers after they left UI. Now a deliberate mouse press or explicit world-touch begin captures one pointer until release/cancel; UI starts and other fingers cannot steal it. World dragging enters Orbit from any preset. Passive measurements never block it; measurements start collapsed, controls scale with landscape height/safe-area top and use a smaller panel. Mouse wheel and on-screen −/+ zoom, Frame Arthur and a persistent concise instruction expose the controls. Bay switches retain matching angles/distance; focus/view changes cancel the owned gesture. Orbit uses one bounded collision cast to avoid placing the camera behind fixture walls/props. Native touch delivery, small-screen readability and camera feel still need user acceptance.

## Checks actually run

| Command / check | Fresh result |
|---|---|
| `python3 -B Tests/run_level3_repair.py --build-only` (before and after repair; final current runtime built before suites) | Runtime **0 errors / 21 warnings**, complete Editor **0 / 10**; unchanged inherited warning counts |
| `python3 -B Tests/run_level3_repair.py --suites --t02 --t03` | **T02 75/75; T03 54/54**. T02 harness compilation has 5 existing unassigned JSON-field warnings |
| Disposable double-scale mutation of the baked-point conversion in the T02 numerical regression | **3 failures** (A/B/Elijah world errors ~278–288 m); normal suite then rerun **75/75** |
| `python3 -B Tests/VerifyT02Pilot.py` | **118 passed / 0 failed**, including original SHA-256, GUID uniqueness, source-copy identity, scene/controller/material references, readable-silhouette policy, no gameplay migration and campaign isolation |
| Cached-import command above | **Expected red on the old C cache**, A/B/Elijah populated with valid structural references. Cache was neither edited nor reimported |
| `python3 -B Tests/run_level3_repair.py --suites --level2` | **Level3 2855/2855**, **controls 5734**, **Level2 1580 PASS**; three existing native EventSystem comparer checks skipped |
| GuideRegression / DialogueRegression using current runtime, installed compiler and disposable response files in `Temp/L3Repair` | **953 PASS / 1166 PASS** |
| `python3 -B Tests/VerifyMobileInput.py`; `python3 -B Tests/VerifyGuideTargets.py` | **7 contracts PASS**; **14 paths, 0 missing** |

One new T03 test initially called `ConversationActorVisual.ComputeModelLocal`, whose Quaternion.Euler needs Unity native execution; standalone .NET stopped with `SecurityException`. Replaced that test path with the production pure geometry fit used by the repaired body, then rebuilt/reran successfully. No assertion or production acceptance gate was suppressed. Native import/animation/rendering/physics/touch/phone checks were **not run**. Other known unrelated failing harnesses were not rerun or relabelled.

Changes: `CharacterBodyGeometry.cs` (+ fresh unique `.meta`); `ExplorationBody.cs`; `T02PilotArthur.cs`, `T02PilotFixture.cs`, `T02PilotRules.cs`; `T02PilotTreatment.cs`, `T02PilotMenu.cs`; the two derived silhouette `.fbx.meta` readable flags; T02/T03 regression programs, imported fixture/decoder and `VerifyT02Pilot.py`; this pack's tickets/progress/issues/current-state/measurement notes. No tracked test binaries generated.

## User Unity/device retest — acceptance remains open

1. Let the existing Unity editor finish normal import/compilation. No rig/scene assembly is required. Optionally run **TaxVenture → T02 Pilot → Validate Imported Character Geometry (T02 + T03)**: it validates actual imported root/mesh/material/bone/Humanoid references without saving a scene. Any failure is a defect to report, not a request to assign assets manually.
2. Open `T02_PilotReview`, Play. A/B/C each show a full-sized, grounded Arthur; C reports **nonzero** geometry and no importer errors. Drag the world with mouse, scroll, use −/+ and Frame Arthur; repeat one-finger drag/zoom buttons on phone in both landscapes. Measurements must not swallow dragging. Test every view and A/B/C switch while walking and at the station.
3. At ¼×/Pause/Step watch all bays reach the station, point/reach/grip/carry/place/release and walk out; inspect Hands/contact markers. Restart, switch bays, lose/regain focus. Confirm synchronization and the isolation readout; no campaign save action is part of this check.
4. In Office_Level1 outdoors confirm **one** grounded Elijah at normal human size while standing, turning, walking, sprinting and jumping; right-look and collision pull-in still frame him. Camera hands remain hidden outdoors. Carry a paper if available: one paper rides his hand.
5. Cross the office doorway both ways, including threshold shuffling: first person/hands indoors, third person/body outdoors. Talk outside and finish/cancel: only the conversation Elijah during shots, then the exploration body returns. Enter/leave desk/interview/corkboard/viewer tasks and repeat doorway crossings, Settings and focus loss: the correct region/body/hands/paper returns. Smoke-check Level 2/3 first-person defaults.

These checks establish user-rendered acceptance only when the user records their results. Missing final run/jump/landing/turn clips, mitten thumb pinch, rigid-folder page action and phone performance measurements remain separate named limitations; none defers basic body visibility to a later ticket.
