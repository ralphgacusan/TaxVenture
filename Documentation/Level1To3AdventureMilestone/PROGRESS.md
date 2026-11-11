# Milestone progress

## 12 November 2026 — first-person baseline; third person/jump stored disabled

**Implemented, internally verified; user manual pending.** User approved execution of the proposed first-person rollback/preservation plan. First-person exploration is active outdoors/indoors; sprint and chevrons remain independent in Levels 1–3. Smoothed Level 1 dialogue Elijah and latest scale/clothing/import work remain intact. Resource opt-in `mixedCameraEnabled: 0` prevents creation of exploration body, doorway switching, jump UI and curb; jump requests are ignored. Guide keeps saved `run-jump` ID with sprint-only content. No assets/animations were removed and no save migration/reset was added.

T04–T21 now target the first-person baseline with all other scope intact. T21 is its release gate. New final **T22 is tentative/deferred**, requiring later explicit selection; stored Level 1 implementation does not imply L2/L3 adoption or native acceptance. Old dated records below are historical. T01/T02 acceptance and unrelated blockers remain separate.

Fresh evidence: runtime/Editor **0 errors, unchanged 21/10 warnings**; T03 **114/114** (before edits 102/102); Level3 **2855/2855**, controls **5734**, retained polish **27/27**, retained experience **33/33**, mobile **7**, guide targets **14/0 missing**, T02 assets **118**, Level2 **1580** (3 native comparer checks explicitly skipped); documentation **22** indexed/acyclic tickets and **623** local links, none missing. Details/paths/commands and user checklist: [FIRST_PERSON_BASELINE_2026-11-12](FIRST_PERSON_BASELINE_2026-11-12.md). No Git, Unity launch/import/Play/render, touch/device/listening or real-save access/reset by AI. Next: fresh user first-person/sprint/return acceptance and separate Arthur contact pilot, then selected T04; no later gameplay implemented.


## 12 November 2026 — third-person experience repair after rejected review

**Implemented, internally verified; user manual pending.** Measured root cause of the oversized Elijah: a posed BakeMesh reading (5.946 vs 6.694 rest) over-scaled him 12% (rest-equivalent 1.77 m vs adult cohort median 1.605 m). One canonical 1.58 m rest height now drives exploration and conversation, applied once. The camera uses a lower-centre composition (3.0 m, +12° pitch, 60° FOV kept) with NPC/bench-aware collision and delayed recovery. Outdoors: analog walk to 1.45 m/s, run at 3.8 m/s, controlled acceleration, one distance-matched walk/run phase with contact footsteps. The run and jump are re-authored from Elijah's own measured walk (corrected muscle signs). The jump is an explicit windup/rise/fall/land track with coyote/buffer. The shirt no longer pokes through the jacket. Editor review aids added. [Causes, tuning, evidence and acceptance steps](THIRD_PERSON_EXPERIENCE_REPAIR_2026-11-12.md).

**Fresh checks:** runtime/Editor 0 errors (21/10 warnings); T03 102/102 (rejected-scale mutation → 6 failures); T02 99/99; VerifyThirdPersonExperience 33/33; polish 27/27; playback 7/7; VerifyT02Pilot 118/118; Level3 2855/2855; controls 5734; Level2 1580 (3 native skips); MobileInput 7; GuideTargets 14/0. No Unity launch/render/touch/device. One read-only `git diff --stat` was run by mistake (no change). Entries below are historical for this scope.

## 11 November 2026 — consistent Elijah, traversal polish and T02 fit

**Implemented, internally verified; user manual pending.** Latest user screenshots/request supersede the 1.12 m capsule-sized visual: exploration now retains the 1.5726485 m conversation height with a centered orbit. Added scoped smoother Elijah exploration/conversation copies, distinct authored Humanoid sprint and jump/landing clips, extended joystick sprint travel with chevrons/RUN feedback, and a farther-left icon-only jump button. T02 now fits each variant independently and isolates finger writes from every other body transform. Geometry tests caught and fixed seam cracks in the new smoothing. [Changes, evidence and remaining native acceptance](THIRD_PERSON_POLISH_2026-11-11.md).

**Fresh checks:** runtime/complete Editor 0 errors (21/10 inherited warnings); T03 69/69; T02 99/99 including actual Elijah source geometry; polish contracts 27/27; asset checks 118 passed; playback/mobile checks 7 each; guide targets 14/0 missing. Broader Level3 2855/2855 and controls 5734 passed. No Unity launch/render/touch/device or real-save verification. Earlier counts/scale decisions below are historical and superseded for this scope.


## 11 November 2026 — user screenshots: size, turning, cadence and split Arthur

**First repair rejected by user-rendered review. Follow-up implemented, internally verified; fresh rendered acceptance pending.** User screenshots show visible but oversized Elijah and split pilot Arthur; turning/fast leg swings also reported. No T04 work, Git, Unity launch/automation, real saves, model/importer/scene changes or packages.

**Corrected cause/implementation:** the pilot passed normalized *world* HumanPose body data into a *root-local* setter during finger curl. New frame conversion plus pelvis preservation prevents a finger edit from moving/rotating the skeleton. Fitting now scales the visual parent and rebinds before hand-pose initialization. Outdoor Elijah uses actual world capsule height (**1.12 m**, replacing the **1.573 m** conversation target), final-scale animation rebind, grounded placement and the existing camera's resized chest pivot. One explicit clip clock, actual-travel facing, idle heading retention, teleport rejection and bounded walk/sprint cadence replace the fast fitted-stride loop; physics/traversal speed and visibility/paper owners remain.

**Fresh internal evidence:** runtime/Editor compile **0 errors**, unchanged **21/10 warnings**; T02 **87/87**, T03 **66/66**; pre-fix wiring repro **0/7**, corrected **7/7**; unconverted-pose mutation **9 failures**, normal T02 rerun green. Current imported C now **4382 vertices / 5474 triangles**, nine skins with populated references (cache decoder exit 0); its former empty import is resolved, while animation deformation still needs acceptance. Asset checks **118/118**, Level3 **2855**, controls **5734**, Level2 **1580** (3 existing native skips), MobileInput **7**, GuideTargets **14/0 missing**.

See [follow-up evidence](CHARACTER_PLAYBACK_REPAIR_2026-11-11.md) for the root-frame repro, primary API contracts, exact checks and fresh-Play retest. The original numeric/source checks did not prove native animation; neither does this follow-up claim a rendered pass. Native post-animation bounds guards are now included. Final run/jump/landing clips remain separate; bounded walk cadence with unchanged traversal speed does not promise perfect sprint foot planting.

## 11 November 2026 — T02/T03 visibility repairs after user review

**Status: Implemented, internally verified; user rendered reacceptance pending.** Owner: Codex. Repair of selected implemented tickets only; T04–T21 remain Planned. No Git, Unity launch/automation/import, device/real-save access, packages, purchases or agents. Existing scene/Resources GUID bindings and original assets preserved; one new geometry helper `.meta` has a fresh GUID.

**Causes/fixes:** C's silhouette processor read non-readable meshes, cleared them and failed to write; the existing import cache reproduces 0/0 and Editor.log records the errors. Derived-only readable flags, scoped preprocess/version invalidation and pre-clear/post-write validation repair that path. Arthur's parent never received the child Animator's IK callback, leaving calibration stalled at the entrance outside the default Task shot; a runtime relay repairs routing. Review now opens following Orbit, supports owned mouse/touch drag from presets, zoom/reframe, passive stats and subject-following views. Character fitting now converts scaled baked vertices once; Elijah is initialized/measured upright at unit world scale, fitted/grounded from geometry, with fit retained after graph evaluation and existing body/hands/paper/task/conversation ownership preserved. No player physics, additional camera/input manager or Level 2/3 adoption change.

**Fresh evidence:** current runtime **0 errors/21 warnings**, Editor **0/10**; T02 **75/75**, T03 **54/54**; `VerifyT02Pilot` **118/118**; Level3 **2855/2855**, controls **5734**, Level2 **1580** (3 existing native skips), Guide **953**, Dialogue **1166**; MobileInput 7 PASS, GuideTargets 14/0 missing. Double-scale mutation gives 3 failures; normal suite rerun green. Read-only cached import check intentionally remains **red on pre-repair C**, with A/B/Elijah populated and actual root/clip/bone/avatar references checked. Not a rendered proof. No inherited failure was suppressed; the new standalone test's native Quaternion call was replaced with the actual pure fitting seam before the final passing run.

Full commands, asset counts/provenance, modified files, limitations and user retest: [visibility repair evidence](VISIBILITY_REPAIR_2026-11-11.md). **Next:** normal Unity import and user A/B/C orbit/contact plus outdoor movement/doorway/conversation/task-return acceptance. No manual model assignment/scene assembly. Final run/jump/landing clips and measured phone cost remain separate gaps.

## 11 November 2026 — T03 mixed-camera exploration loop (implementation)

**Status: Implemented, internally verified; user manual pending.** Owner: AI implementation session; user selected T03 only. T01/T02 work preserved; no T02 derived model adopted; no scene, prefab or existing asset rewritten. No Git, Unity launch/import, ADB, real-save access, package/asset download, purchase, paid generation, agents or contact. T04/T05 not started.

**What the player gets (Office_Level1 only):** outdoor third-person orbit with collision pull-in and indoor first person, switched once per deliberate crossing of the office entrance line (0.6 m hysteresis, deferred while anything else owns the controls); one Elijah body outdoors (no colliders, player capsule unchanged), hands and carried paper handled by their existing owner; joystick outer-ring sprint (1.4×) and a right-side ≥48 dp JUMP with one grounded jump per press and last-safe-point fall recovery; player-root reach/facing/line-of-sight targeting in third person; correct view after desk, interview, corkboard, viewer and conversations; a silent non-blocking run/hop hint and an optional 0.18 m practice curb with a flat route beside it. Levels 2/3 keep their first-person defaults. Full detail, audit table and steps: [T03](tickets/T03-mixed-camera-exploration-and-mobile-movement.md).

**Commands (actual):** build runtime 0/21, Editor 0/10 (unchanged); **T03ExplorationRegression 49/49** (doorway mutation probe → 2 failures, scratch deleted); Level3Regression 2855/2855; controls 5734; Level2Regression 1580 (3 native skips); GuideRegression 953 PASS; DialogueRegression 1166 PASS; VerifyMobileInput all PASS; VerifyGuideTargets 14/0 missing; VerifyShopAndGuideView PASS; T02 suites 64/64 and 114/114. **Inherited, not T03:** CampaignFoundationRegression standalone compile error (`CampaignRewards.cs` → `Level3DeliveryRules` missing from its source list; its pins on T03 files verified directly) and VerifyInteractionOutlines stopping at `Assets/Settings/PC_Renderer.asset` (untouched since 3 Nov). Disposable output only (`Temp/L3Repair`, scratchpad).

**Gaps:** no run/jump/landing (or turn-in-place) clip compatible with Elijah's Humanoid rig — sprint is a faster walk cadence and the air pose holds; not final. Everything native (Play Mode, collision feel, doorway accuracy, body fit, carried-paper pose, button placement, curb vs NPC routes, three-finger touch, both landscapes, audio, frame cost) is pending user evidence for G2/G3/G8.

## 11 November 2026 — T02 visual/contact pilot (implementation)

**Status: Implemented, internally verified; user manual pending.** Owner: AI implementation session; user selected T02 only. T01 behaviour untouched; M01-SUMMARY/M01-FOLDER-RECORD not addressed. No Git, Unity launch/import/automation, ADB, real-save access, package/asset download, purchase, paid generation, agents or contact. T03 not started; no gameplay asset migrated.

**What exists:** one review scene `Assets/Pilot/T02/T02_PilotReview.unity` (one fixture object) that builds three identical bays at Play — A Baseline, B Shading (derived normals, matte washer), C Shading + silhouette (bounded import-time refinement) — each with Arthur, the actual handled Case Folder model, two folder holders, two washers as the laundry work surface and a Synty doorway/back wall/floor, under one light with identical camera views, speed/pause/step, contact markers and an on-device measurement readout. Arthur walks in through the doorway, stops, points, reaches, hook-grips (palm on cover, mitten over the top edge), lifts, carries, lowers, releases and retracts, both directions, then walks out, looping without reset. Details, files and steps: [T02](tickets/T02-visual-contact-and-device-pilot.md); sheet: [T02_PILOT_MEASUREMENTS](T02_PILOT_MEASUREMENTS.md).

**Diagnosis (source files):** Arthur fully flat-shaded (all vertices split; body median dihedral 27°), mitten hands (Index1–4 only, no thumb), already-matte SimpleLit materials; handled folder intentionally hard (84 triangles, 90° edges, matte); washer flat-shaded with a faceted drum and glossy Panda Mat (0.5); architecture deliberately hard; open desk folder 65,316 source triangles (T06 cost item).

**Commands (actual):** `run_level3_repair.py --build-only` runtime 0/21, Editor 0/10 (baselines kept); `--suites --t02` **T02PilotRegression 64/64** (offline estimate: Arthur 3,020 → 5,088 triangles in C, washer 592 → 992, folder untreated); silhouette mutation probe 8 failures, scratch deleted; `Tests/VerifyT02Pilot.py` **114/114**; `Tests/QualifyT02Inventory.py --write` 731 rows qualified (691 active-scene, 19 runtime-resource, 15 inactive, 4 deferred, 2 unreached); `--suites` after the shared `PlayerProgressManager` guard: Level3Regression 2855/2855, controls 5734. Disposable outputs only (`Temp/L3Repair`, `Temp/T02Pilot`, scratchpad).

**Not verified:** Unity import of derived copies, Animator IK/finger muscles, Play Mode lifecycle, rendering, deformation, touch, audio, phone. Imported and device values blank in the sheet. **Next:** user G2/G10 evidence; expansion only after accepted appearance/contact and measured cost.

## 11 November 2026 — T01 full-base-Bag certificate (implementation)

**Status: Implemented, internally verified; user manual pending.** Owner: AI implementation session; user selected T01 only. No Git, Unity launch/import/automation, real-save access/reset, ADB, package, purchase, paid generation or contact. No scene/prefab/JSON/asset change; no new `.meta`. T02 and later tickets not started.

**Player-visible result:** with a full bar and Bag, tapping Bea's offered certificate writes nothing and shows a **CLIP TO BEA'S FOLDER** plate on the actual paper. Tapping it saves the handover and the existing folder attachment in one signed candidate, then the paper flies to the carried folder and docks in the existing Supporting Documents slot. With no free slot the clipped original is read in place (amount bands still usable at the desk) and settles back into the folder; unclipping the certificate or summary into a full Bag is refused. Roomy-Bag pickup is unchanged; a folder left on the desk gives one retry line. Nothing is evicted, bought, consumed or auto-entered.

**Changed:** `Assets/Scripts/Level3/Level3CertificateRules.cs`, `Level3ReviewRules.cs`, `Level3Checkpointer.cs`, `Level3CertificatePaper.cs` (incl. runtime-added `Level3CertificateClipTag`), `Level3CaseFolder.cs`; `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs`; `Tests/Level3Regression/Program.cs`. Pack: T01 ticket, TICKETS index row, README status, this file, KNOWN_ISSUES. All edited C# remains CRLF; pack Markdown LF.

**Commands (actual):**

| Command | Result |
|---|---|
| `python3 -B Tests/run_level3_repair.py` before edits | runtime 0 errors/21 warnings, Editor 0/10, Level3Regression 2826/2826, controls 5734; T13 full-Bag refusal check passed ("10 for 9 base slots") |
| `python3 -B Tests/run_level3_repair.py --build-only` after edits | runtime 0/21, Editor 0/10 (counts unchanged) |
| `python3 -B Tests/run_level3_repair.py --suites` | first run 2852/2855 (3 test-side defects, fixed in tests only); final **2855/2855** incl. Milestone T01 29/29; controls 5734 |
| Mutation probe, disposable (scratch copy of `Level3ReviewRules.cs` with `RoomFor` forced true; suite run from `Temp/M01Mutation`, deleted) | 7 failures (5 T01 + T13 full-Bag + existing full-inventory recovery); production source never mutated |
| `python3 -B Tests/VerifyHudCopyPickup.py` | PASS, 6 checks |
| `python3 -B Tests/VerifyLevel3Scene.py` | PASS, 4 inherited scene/prefab warnings; scene untouched |
| `python3 -B Tests/VerifyGuideTargets.py` | 14 paths, 0 missing |

Level2 suite not rerun (no shared L2 behaviour changed). Generated output stayed in `Temp/L3Repair` and the session scratchpad, never `Tests/*/bin`. No inherited failure was observed in the commands run; R-BASELINE-TESTS items (Training CRLF pins, CampaignFoundation, Level3Playtest hash, AudioRegression compile) were not in scope and not run.

**Not verified:** Editor import/compile, Play Mode, plate rendering/readability/reach, flight/dock/read-in-place motion, native touch/drag, audio, device. User steps are in the T01 ticket. Gates G1/G8 stay pending.

**Next:** user acceptance of T01. Dependents T08 (must respect M01-SUMMARY) and T13 remain Planned.

## 11 November 2026 — planning and ticket authoring

**Status: all production tickets Planned. No implementation performed.**

Read supplied milestone request, applicable initiative instructions, approved story pack, latest L3/cinematic/Foundation amendments, representative physical/save/integration tickets, live scenes/build/career/data/guide/source ownership and existing harness entry points. Recorded source gaps/reuse and evidence limits. User approved scoring/traversal/Bayview facts/additive compatibility, corrected voice scope, one-week/no-spend direction and 21-ticket granularity.

Documentation lives only in this new initiative. Read-only source audit also created temporary GUID-path scratch data outside production. Source asset census resolves 724 dependency rows; 305 source FBX geometry counts measured, with inactive candidates explicitly unqualified for migration. JSON census records bounded current authored dialogue/guide counts, not runtime play duration or full recording coverage.

One read-only Android device-list attempt could not start the sandbox local socket; required escalation was declined. No phone properties/gameplay/performance were read, and no retry/bypass was attempted. Exact current phone/OS/RAM remains E-DEVICE.

No Git commands, Unity/editor launch/import/Play Mode, production script/scene/prefab/asset/package/runtime JSON change, actual-save access/reset, remote issue publication, purchase, paid recording generation or contact with others. No gameplay build/suite result is claimed as fresh. Actual documentation integrity validation results are recorded below.

## Execution status

[T01](tickets/T01-base-paperwork-with-full-bag.md), [T02](tickets/T02-visual-contact-and-device-pilot.md) and [T03](tickets/T03-mixed-camera-exploration-and-mobile-movement.md): **Implemented, internally verified; user manual pending** (11 November 2026, entries below). T04–T21: **Planned**; T22: **Tentative/deferred** after T21. Broad asset migration waits for accepted pilot appearance/contact and measured phone cost. T02 pilot preparation is independent; broad asset adoption waits for accepted pilot/current-phone cost evidence. No parallel implementation is authorized. All user Unity/device/listening/educator acceptance is pending for this milestone.

## Documentation integrity validation

**PASS — 11 November 2026.** A one-off Python documentation validator checked:

- 34 Markdown files, including 21 unique sequential ticket files and each required 22-category structure/status.
- 564 local Markdown link targets and 18 additional inline source paths, all existing.
- Ticket blockers/direct dependents and index DAG match; topological order T01→T21, no missing/forward/cyclic prerequisite.
- 54 confirmed-user requirement IDs and 3 reported-feedback rows with valid owner tickets.
- 724 inventory source rows plus their source-root/material/texture dependency paths; all 305 FBX rows have measured source vertex/polygon counts, and all 64 texture files have read-only header dimensions.
- No checked production acceptance box; all ticket statuses Planned; one-week 11→18 November horizon consistent.

No validation errors. Manual consistency review retained exact Bayview fixtures, the −50 obligation/floor/history separation, approved old-save policy, guide-popup speech exception, latest L3 return/copy rules and independent external gates. This is documentation/source-reference evidence only, not fresh compilation, gameplay, rendering, listening, educator or phone acceptance.
