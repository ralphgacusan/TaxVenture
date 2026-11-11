# T02 — Prove the representative visual and contact pilot

12 November camera amendment: first-person exploration is the release baseline; third-person/jump adoption is tentative T22 only. This ticket's inventory or appearance/contact/device scope stays intact. Pilot Explore/Orbit inspection cameras remain available for asset review.

Latest requested follow-up: [consistent third-person scale, animation/control polish and T02 fitting](../THIRD_PERSON_POLISH_2026-11-11.md), implemented and internally verified (runtime/Editor 0 errors; T03 69/69, T02 99/99, polish 27/27). This supersedes the smaller capsule visual and missing-sprint/jump statements in historical records below. Fresh user-rendered/native acceptance remains pending.

## Status and readiness

**Implemented, internally verified; user manual pending** (11 November 2026, AI implementation session). The fixture and repaired bindings pass internal checks; the user reported missing Arthur/blocked camera inspection, addressed in the repair record below. Fresh Unity reimport, rendered contact and phone measurements remain pending. Rollout stays blocked on E-PILOT and E-DEVICE.

## Follow-up after user screenshots — 11 November 2026

The first repair did **not** pass rendered acceptance: Elijah was visible but oversized, turning and swinging his legs too fast; pilot Arthur's parts were displaced. The [follow-up repair](../CHARACTER_PLAYBACK_REPAIR_2026-11-11.md) fixes HumanPose world/local conversion and pelvis preservation, final-scale binding order, capsule-derived outdoor height, camera framing, explicit clip timing and bounded cadence/travel heading. **T02 87/87; T03 66/66; runtime/Editor 0 errors, unchanged 21/10 warnings.** Current C import is populated (4382 v / 5474 tris); native animation/deformation/feel reacceptance remains pending. No scene/rig assembly is assigned to the user, and T04 remains Planned.

## Repair record — 11 November 2026

User-rendered review found defects in the original implementation. The requested pilot model visibility and camera inspection repair is **implemented, internally verified; user rendered reacceptance pending**. See [fresh diagnosis, changes, exact checks and retest](../VISIBILITY_REPAIR_2026-11-11.md). T02 now passes 75/75, T03 54/54; current runtime/Editor compile with 0 errors and unchanged 21/10 warnings. This was the earlier internal repair result; the subsequent user import populated C, but screenshots rejected deformation and body scale. See the follow-up above; no new rendered/phone pass is claimed. T04–T21 remain Planned.

## Priority and relative size/risk

P1. Size: **M**, bounded to one fresh implementation context. High: visual treatment, rig/contact and real-phone uncertainty. If an observed prerequisite makes this bound false, record the exact bounded substep/blocker; do not absorb unrelated systems.

## Blocked by

None (ticket prerequisites). External decisions/assets/evidence: E-DEVICE for measured cost; E-PILOT for rollout approval; exact E-CONTACT capability if available tooling cannot produce it.

## Direct dependents

[T03](T03-mixed-camera-exploration-and-mobile-movement.md)

## Goal and visible accomplishment

A reviewable gameplay-distance pilot demonstrates smoother style, credible action contact and measured cost before the rest of the game adopts it.

## Current behavior and evidence

Existing rigged/static actors and procedural props are reusable. The724-row source census includes inactive candidates;305 FBX source counts are not imported runtime budgets. Existing idle/walk/stroll does not prove grasping. Read [CURRENT_STATE](../CURRENT_STATE.md), [AUDIT_NOTES](../AUDIT_NOTES.md) and the actual reuse paths below. Existing internal evidence is historical; no static declaration establishes Unity behavior.

## Existing systems/assets to reuse

- [NpcHumanoid.controller](../../../Assets/AssetIntegration/NpcHumanoid.controller)
- [NpcAnimationDriver.cs](../../../Assets/Scripts/FSM/NPC/NpcAnimationDriver.cs)
- [NpcBodyPose.cs](../../../Assets/Scripts/FSM/NPC/NpcBodyPose.cs)
- [ReviewedFileClosedModel.prefab](../../../Assets/Resources/Campaign/ReviewedFile/ReviewedFileClosedModel.prefab)

## Scope and explicit exclusions

Qualify active used families, then compare Arthur, handled folder and one laundry doorway/work surface under identical framing/light/action. Produce scoped recoverable variants only. Exclude whole-library smoothing, runtime subdivision, unrelated remastering and new packages/purchases. Follow [SPEC](../SPEC.md), [approved decisions](../DECISIONS.md) and [asset/contact plan](../ASSET_AND_ANIMATION_PLAN.md); no production execution, Git, Unity launch, paid generation/purchase or actual-save reset is authorized merely by this ticket.

## Player journey and entry/exit

Representative Arthur/folder/laundry-section comparison before migration. From an isolated review fixture, see baseline and variant at normal exploration and task distances, watch walking/turn/reach/folder contact/page action, then return without affecting story.

## Mechanics and domain logic

Read current active hierarchy/runtime construction and mark census candidates used/inactive/deferred with provenance; unknown activity prevents that asset's migration. Separate normals, silhouette, material/light and rig treatments. Obtain user appearance/contact/cost approval before expanding.

| Situation | Player-visible response | State/commit | Recovery |
|---|---|---|---|
| Preview begins | Actual baseline/variant comparison | Isolated fixture only | Exit releases owned objects/holds |
| Source/contact/cost unavailable | Named evidence gap | No rollout approval | Complete unaffected qualification; retain blocker |
| User rejects pilot | Concrete difference recorded | No broad migration | Revise bounded treatment and recheck |

## Visual/environment/interaction specification

Use existing identity/clothing/palette. Show intentional hard edges versus curved surfaces, unchanged UV/text and readable paper/room contrast. Record exact source/derived model/material and shared usage.

## Animation and physical contact

Prove Arthur point→reach→grip→place/release with real body/hand origin and folder anchor, plus foot/turn/deformation. Name missing clip/rig/tool capability; procedural sway/static arm drop is not final grasp.

## Audio/dialogue/voice

Reuse current quiet paper/footstep cues, no new dialogue/voice production. Silence is valid in comparison; do not add effects to conceal motion flaws.

## Camera/mobile/accessibility

Normal player and task-distance framing, both landscape orientations; no camera-framing trick that hides hand errors or inflates readability.

## Data/interface/persistence

Only isolated fixture/derived asset references and inventory measurement documentation; no campaign state, rewards, case values or save migration. Use verified owners; introduce no speculative public class/API to match this prose.

## Failure/cancellation/interruption/reload

Fixture exit/disable/focus releases owned objects/holds; no live save reads/writes, source-prefab overwrite or reset. Originals remain recoverable. See SPEC's shared command boundary and [mode/restore verification](../VERIFICATION.md).

## Performance/resources

User records current-phone model/OS/RAM/build and baseline/pilot frame distribution, CPU/GPU cause, memory, import mesh/material/texture/audio costs and transitions. Record measured ceilings, not guessed budgets.

## Internal checks — AI only

- [x] Validate source/variant/GUID/material/UV/controller references and active-use qualification; compare source counts while explicitly distinguishing imported cost. (`Tests/VerifyT02Pilot.py` 114/114; `Tests/QualifyT02Inventory.py` over 731 rows; imported counts pending Unity.)
- [x] Compile any actual authoring/runtime fixture paths; prove fixture never calls acquisition/reward/story-save commands and cleanup is idempotent. (Runtime 0 errors/21 inherited warnings, Editor 0/10; `T02PilotRegression` 64/64; cleanup proven by source checks, native lifecycle is the user's Play Mode check.)
- [x] Record unavailable tooling/rig/contact assets precisely and update inventory disposition; no broad gameplay suites for asset-only evidence.

## USER Unity/device acceptance

- [ ] Compare baseline/variant at gameplay distances and actual reach/contact/deformation, paper typography and room thresholds; accept or record concrete defects.
- [ ] Capture current phone costs after warm-up and approve measured rollout ceilings. Device lookup remains pending user evidence; do not bypass declined access.
- [ ] Record actual build/device/steps/evidence for applicable gates **G2, G10** in [VERIFICATION](../VERIFICATION.md); source compilation cannot check this box. AI delivers assets/builders/bindings; user is not assigned manual scene assembly.

## Completion definition

The visible goal works through its real entry/exit with the stated source, presentation, ownership and recovery rules; relevant internal checks pass or precise inherited limitations are recorded. Mark **Implemented, internally verified; user manual pending** only after actual implementation/checks. Mark **User accepted** only on recorded user evidence. Required unresolved asset/content/voice/device gates remain explicit; no placeholder or documentation-only completion.

## Handoff/update requirements

Update this ticket's actual status/checks, [PROGRESS](../PROGRESS.md), [KNOWN_ISSUES](../KNOWN_ISSUES.md) and the new pack's affected contract/inventory/coverage when needed. Record owner/session/date, modified paths, exact commands/results/warnings/fixtures, failures and unperformed native checks, actual supplied acceptance and precise next dependent/blocker. Preserve old initiative histories and existing user work; follow applicable AGENTS and the user's Git/Unity ownership. Scope stays Levels1–3.


## Implementation record — 11 November 2026

Owner: AI implementation session (Claude), user-selected T02 only. T01's certificate clip, full-Bag reading and detach safeguards untouched; M01-SUMMARY and M01-FOLDER-RECORD not addressed. No Git, Unity launch/import/automation, ADB, real-save access, package/asset download, purchase, paid generation, agents or contact. Comparison/measurement sheet: [T02_PILOT_MEASUREMENTS](../T02_PILOT_MEASUREMENTS.md).

### What the pilot is

One scene, `Assets/Pilot/T02/T02_PilotReview.unity`, holding one object (`T02PilotFixture`). On Play it builds three identical bays under one light and one camera: **A Baseline** (shipped originals), **B Shading** (derived Arthur/washer with importer-calculated normals; matte washer), **C Shading + silhouette** (B plus a bounded import-time refinement of faceted soft edges). Each bay: Synty floor, back wall and doorway wall (hard architecture, unchanged), two Pandazole washers as the laundry work surface, two Synty folder holders, the actual handled Case Folder model `SM_Prop_Folder_Manila_02` with the existing `ClientCopyStamp` print, and Arthur. Camera views (Explore ~4.5 m, Task ~1.2 m, Hands, Doorway, Orbit, All) keep identical framing per bay. Controls: bay A/B/C, speed 1×/½×/¼×/pause/step, restart, contact markers, matte toggle, measurement readout, copy sheet, exit.

### Treatments (smallest effective, per diagnosis)

- **Arthur:** fully flat-shaded source (every vertex split, curved body at ~27° median dihedral) → derived copies import with normals calculated from 50° (hard edges above it kept). Bay C adds one refinement level only on smooth-shaded edges between 12° and 50° (PN-curved split points, creases/open edges untouched, UVs interpolated, skin weights blended, bind poses kept). Materials are already matte (SimpleLit, no specular keyword): unchanged. Identity, clothing, palette, UVs, rig and the original file are unchanged (derived files are byte copies).
- **Handled folder:** intentional 90° hard edges, matte Synty material → no geometry/material change; it is the contact subject.
- **Laundry section:** washer is flat-shaded with a faceted drum → normals at 40° (B) and refinement (C); its glossy Panda Mat (smoothness 0.5) gets a matte derived copy (0.18). Architecture deliberately hard: unchanged.

### Contact and motion

Arthur walks in through the doorway at the walk clip's own ground speed (no foot sliding while walking), turns along the path, stops into the standing idle at one station chosen from his measured arm length, then: point → reach → approach → **hook grip** (palm on the folder's cover just below its top edge, mitten curled over the edge via Humanoid finger muscles) → lift clear of holder A → carry → lower into holder B → release (uncurl; folder detaches at its world pose and settles onto its rest) → back off → retract; then the same from B back to A, and walks out; loops without reset. The folder is parented to the hand bone only between grip and release. Hand frame (IK goal offset, finger direction, palm side) is calibrated from the actual bones by curling the real finger muscles. Grip error, place error/angle, reach ratio, arm/hand length, walk speed and foot offset are shown live.

### Files

New: `Assets/Pilot/` (+`.meta`), `Assets/Pilot/T02/{Derived,Materials,Animation}/` with `Arthur_T02_Shading.fbx`, `Arthur_T02_Silhouette.fbx`, `Washer01_T02_Shading.fbx`, `Washer01_T02_Silhouette.fbx`, `PandaMat_T02_Matte.mat`, `NpcHumanoid_T02Pilot.controller`, `T02_PilotReview.unity` (all with fresh unique GUIDs); `Assets/Scripts/Pilot/T02PilotRules.cs`, `T02PilotArthur.cs`, `T02PilotFixture.cs`; `Assets/Editor/Pilot/T02PilotSilhouette.cs`, `T02PilotTreatment.cs`, `T02PilotMenu.cs`; `Tests/T02PilotRegression/Program.cs` (+ read-only `fbxinfo.py`, `export_source_meshes.py`, run automatically by `--t02`), `Tests/VerifyT02Pilot.py`, `Tests/QualifyT02Inventory.py`; `Documentation/Level1To3AdventureMilestone/T02_PILOT_MEASUREMENTS.md`.
Changed: `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs` (bootstrap returns early in the pilot scene only; CRLF kept), `Tests/run_level3_repair.py` (opt-in `--t02`), `ASSET_REFERENCE_INVENTORY.csv` (+`use_status`, `use_evidence`, `t02_disposition`; 7 staging rows added), pack docs.

### Commands and results

| Command | Result |
|---|---|
| `python3 -B Tests/run_level3_repair.py --build-only` | runtime 0 errors / 21 warnings (baseline 21), Editor 0 / 10 (baseline 10); first build had 2 new obsolete-API warnings in the fixture, fixed |
| `python3 -B Tests/run_level3_repair.py --suites --t02` | **T02PilotRegression 64/64** (first run 61/64: three over-strict test expectations corrected, no production change) |
| Mutation probe (scratch copy of `T02PilotSilhouette.cs` without the crease check; output in `Temp/T02Mutation`, deleted) | 8 failures, incl. crack detection |
| `python3 -B Tests/VerifyT02Pilot.py` | **PASS 114/114** (originals' SHA-256 unchanged, unique GUIDs, byte-identical derived models, meta/controller/material diffs limited, scene references resolve to real root file IDs, not in build list, no gameplay user) |
| `python3 -B Tests/QualifyT02Inventory.py --write` | 731 rows: 691 used:active-scene, 19 used:runtime-resource, 15 inactive:saved-hierarchy, 4 deferred:non-build-scene, 2 unreached (pilot washer candidates) |
| `python3 -B Tests/run_level3_repair.py --suites` (shared `PlayerProgressManager` changed) | Level3Regression 2855/2855, controls 5734 (unchanged) |

Read-only FBX parsing and geometry export used scratch scripts and `Temp/T02Pilot/source_meshes.json` (disposable). No tracked `Tests/*/bin` output.

### Remaining blockers and limits (exact)

- **E-PILOT / G2 (user):** appearance, contact and deformation verdicts not yet given; no rollout before that.
- **E-DEVICE / G10 (user):** phone model/OS/RAM, imported counts and frame/memory/transition measurements are blank in the sheet; nothing is claimed as 30 FPS.
- **Thumb pinch:** unavailable — Arthur's rig and mesh have mitten hands (`Index1–4` only, no thumb bones or geometry). A flat folder cannot be picked from a table's near edge credibly; the pilot uses an upright hook grip. A pinch needs a re-rigged/re-modelled hand (Blender or another DCC tool is not installed).
- **Page action:** not demonstrated — `SM_Prop_Folder_Manila_02` is one rigid slab with no cover/hinge split, and the open desk folder is a separate 65,316-triangle model. Needs a hinged handled-folder variant (split cover with pivot) from a DCC tool or a bounded builder in the adopting ticket.
- **Turn in place:** the shared controller has no turn clip; turns happen while walking, and the final small settle turn uses the idle (minor visible foot slide).
- **Not verified here:** Unity import of the derived copies (avatar mapping, `[T02 pilot]` log lines), Animator IK/finger-muscle behaviour, fixture lifecycle in Play Mode, rendering, touch, listening (paper/footstep cues only on the active bay).

### USER Unity/device acceptance steps

1. Let Unity import; check the Console/Editor log for `[T02 pilot]` lines (imported counts) and no import errors. Open via **TaxVenture ▸ T02 Pilot ▸ Open Review Scene**, press Play; the readout should say "Campaign save owner in scene: absent (isolated)".
2. In **Task** and **Explore** views compare A/B/C at 1× and ¼×: smooth curved shading, kept hard edges, silhouette, paper print and matte washer. Note any broken UV, seam, crack or identity change.
3. **Hands** + **Contact markers** at ¼×/Step: palm reaches the cover, mitten curls over the top edge, folder follows without jumping, lowers into holder B and releases. Note grip/place error mm.
4. **Orbit** at ¼×: shoulder/elbow/wrist and mitten deformation during reach and carry; walking feet, stop and turns.
5. Press **Restart** several times, switch away from the app and back (focus loss pauses), then **Exit**: no duplicate Arthurs, cameras or listeners; the folder never stays stuck to a hand.
6. Phone: **TaxVenture ▸ T02 Pilot ▸ Build Android Pilot APK**, install beside the game, warm up 2 minutes, then **Copy sheet** for each bay in Explore and Task, both landscape orientations. Fill the pending table and G2/G10 in VERIFICATION.

Next: user pilot acceptance and measured ceilings. T03 waits; no broad asset migration.
