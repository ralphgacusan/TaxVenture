# T02 pilot — comparison and measurement sheet

Prepared 11 November 2026 with the T02 implementation. **Source/offline values and pre-repair cached imported counts are recorded separately; repaired Unity import and phone measurements remain pending.** No budget is set here: ceilings follow the user's recorded phone evidence (E-DEVICE, G2, G10).

## How to open and compare

1. Unity: **TaxVenture ▸ T02 Pilot ▸ Open Review Scene** (or open `Assets/Pilot/T02/T02_PilotReview.unity`), then **Play**. Nothing needs placing; the single `T02 Pilot Fixture` object builds everything.
2. Three identical bays under one light: **A Baseline** (shipped originals), **B Shading** (derived normals; matte washer), **C Shading + silhouette** (B plus the bounded edge refinement). Switch bays with the A/B/C buttons; the camera keeps the same framing.
3. Views: **Explore** (~4.5 m third-person distance), **Task** (~1.2 m), **Hands** (follows the actual right hand), **Doorway**, **Orbit** (initial full-actor view; world mouse/touch drag, wheel or −/+ zoom, Frame Arthur), **All** (three bays side by side).
4. Speed 1×/½×/¼×/Pause/Step; **Contact markers** shows the wanted palm point (green) and the actual palm point (orange) during arm beats; **Matte washer** toggles the derived material in B/C; **Restart** rebuilds everything; **Copy sheet** puts the readout below on the clipboard and in the log; **Exit** stops.
5. Phone: **TaxVenture ▸ T02 Pilot ▸ Build Android Pilot APK** builds `Builds/T02Pilot/TaxVenture_T02Pilot.apk` with only this scene, using a temporary `.t02pilot` application-id suffix so it installs beside the game (id and product name are restored afterwards). Warm up for 2 minutes on the same orientation, then **Copy sheet** per bay and orientation.

## Source and derived assets

| Role | Original (unchanged; SHA-256 checked by `Tests/VerifyT02Pilot.py`) | Derived pilot asset (GUID) | Change |
|---|---|---|---|
| Arthur | `Assets/3D Models/npc_tutorial_static/npc_tutorial_static1@Standing W_Briefcase Idle.fbx` (e6276f65…) | `Assets/Pilot/T02/Derived/Arthur_T02_Shading.fbx` (a1cd592b…) | Byte copy; import calculates normals from angle 50° (was: file normals) |
| Arthur | same | `Assets/Pilot/T02/Derived/Arthur_T02_Silhouette.fbx` (279336d6…) | Byte copy; same normals plus import-time silhouette refinement (`t02:silhouette`) |
| Laundry work surface | `Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Home Interior/Models/Prop_Washer_01.fbx` (80a84648…) | `Assets/Pilot/T02/Derived/Washer01_T02_Shading.fbx` (4bd91638…), `Washer01_T02_Silhouette.fbx` (c47928f0…) | Byte copies; normals from angle 40°; silhouette copy refined |
| Washer material | `…/Pandazole Home Interior/Materials/Panda Mat.mat` (293fedf4…) | `Assets/Pilot/T02/Materials/PandaMat_T02_Matte.mat` (95536dbe…) | Smoothness 0.5 → 0.18 only |
| Arthur controller | `Assets/AssetIntegration/NpcHumanoid.controller` (2e8c1209…) | `Assets/Pilot/T02/Animation/NpcHumanoid_T02Pilot.controller` (d3042edf…) | Same states and clips; renamed; IK pass on |
| Handled folder | `…/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Folder_Manila_02.prefab` (32501964…) | none | Used as shipped in every bay (diagnosis below) |
| Staging | Synty `SM_Bld_Base_Floor_01`, `SM_Bld_Base_Wall_01`, `SM_Bld_Base_Wall_Door_01`, `SM_Prop_Folder_Holder_02`; Pandazole `Prop_Washer_01.prefab` | none | Placed as shipped (holder scaled 0.7 in the fixture only) |

Review scene `Assets/Pilot/T02/T02_PilotReview.unity` (dc99c5b9…) is **not** in the game build list. No gameplay scene, prefab or resource references a derived asset.

## Diagnosis per asset (read from the source files)

| Asset | Faceting / shading | Silhouette | Material / light | Rig / contact |
|---|---|---|---|---|
| Arthur (9 meshes, 3,020 source triangles, 33 bones) | Every vertex carries split face normals (fully flat-shaded); 45–79% of corners equal their face normal; the body's median dihedral angle is 27°, i.e. curved forms rendered as facets → **normals treatment** | Low-density cylinders/boxes keep a polygonal outline after smoothing (body p50 27°, p90 90°) → **bounded silhouette refinement** compared in bay C | URP SimpleLit, no specular keyword enabled → already matte; **no material change** | Mixamo Humanoid (auto avatar). Each hand is a **mitten**: hand vertices ride `Index1–3` only, no thumb or other finger bones/geometry. Contact is limited to a hook/clamp grip; no thumb pinch is possible |
| Handled folder `SM_Prop_Folder_Manila_02` (84 triangles) | 93% face normals, median dihedral 90°: a thin slab whose hard edges are intentional | Rectangular by design | Synty atlas, smoothness 0.2: matte | Single rigid slab (no cover/hinge split): cannot be opened by a hand |
| Washer `Prop_Washer_01` (592 triangles) | 100% flat; drum/door curves at ~30° dihedral (faceted) while box edges are 90° | Coarse drum ring | URP Lit smoothness 0.5 reads glossy | Static; one collider (used to measure its top) |
| Synty floor/walls/doorway | Imported with calculated normals at 3° (flat by design) | Deliberately hard architecture | Synty atlas | Door opening 0.95 m wide (from the collision mesh) |
| Open desk folder `OpenCaseFolder.fbx` (not handled in the pilot) | Already smooth | — | — | **65,316 source triangles** for one prop: a cost item for T06 measurement, not treated here |

## Geometry counts (source and offline estimate)

Source counts are the FBX polygons fan-triangulated. "Offline import approximation" re-creates the importer's angle-based normals outside Unity to run the real silhouette code (`Tests/T02PilotRegression`); Unity's own import splits vertices differently, so **imported counts must be read from Unity**.

| Model | Source triangles | Offline approx. vertices / triangles (shading) | Offline silhouette vertices / triangles | Edges split / kept hard / flat / steep |
|---|---|---|---|---|
| Arthur | 3,020 | 3,057 / 3,020 | 4,117 / 5,088 (+68%) | 1,034 / 1,259 / 2,079 / 77 |
| Washer | 592 | 724 / 592 | 986 / 992 (+68%) | 200 / 228 / 418 / 0 |
| Folder (not treated) | 84 | 128 / 84 | 129 / 86 (+2%) | 1 / 56 / 69 / 0 |

## Pending measurements (user)

| Item | Where it comes from | A Baseline | B Shading | C Silhouette |
|---|---|---|---|---|
| Imported Arthur vertices / triangles / material slots / bones | Fixture readout "Arthur imported …", and `[T02 pilot]` lines in `~/Library/Logs/Unity/Editor.log` after import | | | |
| Imported washer vertices / triangles | Same | | | |
| Import time of derived copies, any import warning | Editor log | — | | |
| Frame p50 / p95 / p99 / max ms, frames over 33.3 ms (after 2 min warm-up) | Fixture readout per bay, Explore and Task views, both landscape orientations | | | |
| CPU / GPU frame time | Readout (needs Frame Timing Stats enabled in Player Settings) | | | |
| Allocated / reserved / mono memory | Readout | | | |
| Pilot setup ms, app start to first pilot frame | Readout | | | |
| Phone model / Android / RAM / build | Readout first line + build id | | | |
| Visual verdict at Explore/Task/Hands distances | User | | | |
| Contact verdict (reach, palm on cover, curl over edge, release) and grip/place error mm | User + readout | | | |
| Deformation verdict (shoulder, elbow, wrist, mitten curl) | User, Orbit view at ¼× | | | |

The project's `MobilePerformance` governor stays active in the pilot APK (60 → reduced resolution → 30 behaviour); record whether it changed resolution during the run. A `targetFrameRate` of 30 is not a pass.

## Rig compatibility and shared regression risk

- Derived Arthur copies are Humanoid with their own auto-generated avatar from the same bones and the same remapped Arthur materials; the shared controller's Humanoid clips retarget unchanged. If Unity reports an avatar mapping difference on import, bay B/C shows "Problem: …" instead of running.
- The pilot controller only adds the IK pass. Arm targets use Humanoid IK; the mitten curl uses the Humanoid finger muscles `Right Index 1–3 Stretched` (calibrated by curling the real rig).
- Gameplay is untouched: no scene/prefab/resource references the derived assets; the import step is scoped to `Assets/Pilot/T02/Derived/`. One shared code change: `PlayerProgressManager`'s bootstrap returns early in the pilot scene only. The test runner gained an opt-in `--t02` flag.

## User-review repair evidence (11 November)

Pre-repair cached imported counts: Arthur A **5600 v / 3020 tris**, B **3030 / 3020**, C **0 / 0**. C is a confirmed broken import, not an optimization result. Readable derived-silhouette import and guarded writes are now implemented; the old cache was not altered or reimported by AI. Keep the repaired-import/device table pending until fresh Unity evidence. Readable C meshes retain a CPU copy: include its memory cost in the phone comparison. See [repair diagnosis/checks/retest](VISIBILITY_REPAIR_2026-11-11.md). Measurements start collapsed and their passive overlay no longer blocks camera dragging.

## Follow-up after user screenshots

Current user-imported C cache is populated: **4382 v / 5474 tris**, nine skins and valid structural references. The old empty cache above is historical. User-rendered deformation/scale review **failed**, so geometry presence alone does not accept the pilot. [Follow-up repair](CHARACTER_PLAYBACK_REPAIR_2026-11-11.md) corrects the HumanPose world/local round-trip, preserves pelvis pose and initializes hand posing after final-scale binding. Fresh A/B/C animation/contact and phone cost remain pending.
