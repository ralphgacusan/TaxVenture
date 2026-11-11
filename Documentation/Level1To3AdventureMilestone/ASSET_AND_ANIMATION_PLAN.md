# Used assets, contact animation and recording dependencies

## Current camera direction — approved 12 November 2026

First-person exploration is the active baseline outdoors and inside every establishment. Keep first-person sprint, existing close task views and directed dialogue shots, smoothed Elijah, all other smoothing/contact work, and the remaining gameplay scope. Third-person exploration, doorway perspective switching, outdoor Elijah body, jumping/button and curb onboarding are stored and disabled. They are **tentative**, owned only by [T22](tickets/T22-tentative-third-person-and-jump-adoption.md), after T21; they do not block T04–T21 or first-person release acceptance. Earlier dated implementation records remain historical evidence, not active camera requirements.


Latest 11 November user follow-up: [third-person polish](THIRD_PERSON_POLISH_2026-11-11.md) supersedes the capsule-sized visual and walk-only/held-air fallback. Level 1 now shares dialogue height, centered framing, scoped smoothed Elijah copies, authored sprint/jump/landing, extended sprint stick and icon jump control; T02 fit/finger isolation repaired. Native appearance/touch acceptance remains pending.

Planned work. Preserve original models, GUIDs, source prefabs, character identities/clothing/palette and existing authored intro/cutscene clips. AI later produces all derived assets, bindings, colliders and deterministic placement; user acceptance does not mean manual scene assembly.

## Inventory and measurement evidence

[ASSET_REFERENCE_INVENTORY.csv](ASSET_REFERENCE_INVENTORY.csv) contains **724 bounded dependency rows:305 FBX models,307 prefabs,48 materials and64 textures**, reached from three office scenes, the existing intro, five chapter scenes, L2/L3 scene-assets and current shared campaign/reference assets. It records source roots, source geometry, import/skin evidence, material/texture dependencies, all 64 texture header dimensions, source bytes, disposition/owner and acceptance needs.

This is a **source-reference census**, intentionally retaining inactive candidates for qualification, not a declaration that all724 rows render or need remodeling. No project-wide asset library is scanned for makeover work. [AUDIT_NOTES](AUDIT_NOTES.md) identifies known-used runtime families and the source owner that instantiates them. At T02, qualify each model against active saved hierarchy, runtime startup hides, actual builder/load and view distance; mark inactive/deferred candidates `retain/no migration`. Each adopting slice owns only its qualified used set; unknown activity blocks that asset's migration, not unrelated gameplay. Do not turn the census into305 automatic geometry edits.

Read-only FBX binary parsing measured 305 source-model vertex totals and fan-triangulated polygon counts. Examples: Arthur's rigged briefcase-idle source1,522 vertices / 3,020 source triangles; Elijah idle1,358 / 2,559; both contain skin deformers. These count all geometry in the file, before Unity import splits, active submesh/LOD selection, batching/skinning and runtime variants. They are **not** imported render/GPU/memory budgets. Actual Unity imported mesh/material/texture/audio residency and phone costs remain unmeasured.

For every qualified used family, T02/adopting owner adds active prefab/model/material/texture links, level/view distance, static/skinned/rig status, source/imported mesh cost, hard-edge versus silhouette diagnosis, UV/material dependency, collider/contact needs, selected treatment and regression users. A value unavailable without Unity is explicitly `pending user measurement`, not guessed. No visual defect is inferred solely from a filename or professor comment.

## Pilot and individual treatment

**11 November 2026 — T02 implemented, internally verified; user manual pending.** Review scene `Assets/Pilot/T02/T02_PilotReview.unity` compares A Baseline / B Shading / C Shading + silhouette bays (Arthur, handled `SM_Prop_Folder_Manila_02`, washer work surface, Synty doorway). Derived byte-copy models under `Assets/Pilot/T02/Derived/` change only import normals (Arthur 50°, washer 40°) and, for C, a scoped import-time refinement of faceted soft edges; a matte washer material copy; a controller copy with the IK pass. Originals are hash-checked unchanged. Diagnoses, files, offline counts and the blank measurement table: [T02_PILOT_MEASUREMENTS](T02_PILOT_MEASUREMENTS.md). Inventory rows now carry `use_status`, `use_evidence` and `t02_disposition`. Adoption beyond the pilot still waits for the user's appearance/contact verdict and measured phone cost.

T02 delivers one reviewable in-world comparison at actual gameplay distances: **Arthur**, **the repeatedly handled folder**, and **a laundry room section/doorway/working surface**. Reuse an isolated preview/fixture route and existing materials; no new gallery framework or production case completion. Capture baseline and variant under identical camera/light/action, including walking/turning, pointing/reach/grasp/release, page handling and hand contact.

| Treatment | Apply only when diagnosed | Verification |
|---|---|---|
| Normals/shading | Unwanted faceting across a genuinely curved surface | Compare normals/UV seams, preserve intentional creases; silhouette is not claimed fixed |
| Geometry/silhouette | Camera-visible angular outline remains after appropriate shading | Local bevel/retopology/limited extra loops; compare imported counts and near/far appearance |
| Materials/light | Excess shine, palette inconsistency, unreadable paper or room contrast | Keep shared palette UVs, matte paper/tool roughness and stable dark ink; bound lights/shadows |
| Rig/deformation | Shoulder/elbow/hand or clothing folds fail target action | Preserve bind/avatar; repair weights/rig or authored pose contact; inspect full action |
| Replacement | Existing source cannot meet silhouette/contact/cost through bounded treatment | Exact unavailable capability/source named; distinct identity preserved; no purchase/download presumed |

Use derived variants deliberately, original recoverable, shared mesh/material references reused across levels and cinematics where safe. Never overwrite original sources to force pilot acceptance. Expand only after the user accepts appearance/contact **and** measured current-phone cost. A lighter variant may retain visual intent through shading/LOD/visibility rather than removing readability or interaction.

## Asset ownership by playable slice

| Owner | Qualified used families / production outcome | Contact and regression obligations |
|---|---|---|
| T02 | Arthur, handled folder and representative room pilot; complete active-use qualification of census | All other treatments reference accepted pilot; originals and shared cutscene bindings preserved |
| T03 | First-person sprint, current hands/carried paper and smoothed conversation Elijah | Correct first-person/task/dialogue returns; smoothing independent of camera flag; no active jump/body |
| T22 (tentative) | Retained Elijah exploration model and idle/walk/sprint/jump clips, orbit/doorway/button/curb systems | Reaccept body/feet/scale/targeting/doorway/multitouch and phone cost before any opt-in rollout |
| T04 | Arthur reviewed reference, attached ID/background, board station | Real body beside example, grip/lift/point/return, player view unobscured, source never swapped into Marie |
| T05 | Four profile clients and existing L1 optional residents, plaza route surfaces/signs, **new** Kanor/Adrian | Per-model choices, protected cast not reused for ambient/new roles; stable footprints/markers/touch reach |
| T06 | Marie, receptionist/Auditor, office folder/book/ID/background/stamp/shared paper/copy | Rig/contact gaps explicit; shared L2/L3 and cinematic source regression; real copy handover |
| T07/T11 | Bayview continuing file/stationery/reference and approved source records | Same case identity; blue-gray Bayview, teal/cream CoastCare footer where specified; no matching-antagonist logo spoiler |
| T08 | Rosa model, working laundry room, entrance, records storage/desk, public letter chest | Same Rosa and source packet; office travel intact; records genuinely retrieve; occlusion/route/physics clearance |
| T09 | Existing toolkit/highlighter/cards/sample and same Arthur | Grip/tool/paper contacts, no sample-case or duplicate-Arthur ownership leak |
| T10 | Rosa conference-room route, folder/annotations/stamps/client copy | Current office/service vocabulary, choice staging and real transfer; no second Rosa |
| T12 | Bea, modest art room, envelope, table/stand/paper machine/summary and Arthur | Actual table/play target geometry and contact; no completed wealthy studio; seven real sources unchanged |
| T13/T14 | Existing calculator/book/source/tapes/certificate/shared stamps/copy and office actors | Reach from actual body, source-readability/clip/ink, no simultaneous object operation; correct existing return/delivery ownership |
| T15 | Existing clock/result/badge surfaces | Readable labels/legend, source-specific totals; no new art family |
| T16–T19 | Reused audio/settings and new revision-matched prerecorded takes | Voice identity/pronunciation and memory/import pilot, chapter-bounded loads, no runtime provider |

**New supporting model selection for implementation:** Kanor uses available `Assets/Synty/PolygonTown/Prefabs/Characters/SM_Chr_Father_01.prefab`; Adrian uses available `Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Boss_Male_01.prefab`. Neither was reached by the bounded current scene/resource census. Keep them distinct from protected characters and Marco's cinematic `SM_Chr_Business_Male_01` identity. Their clothing/palette adaptations and grounded scale require the same visual pilot review; they are proposed new art bindings, not pre-existing character appearances. Reuse the current Synty `SM_Gen_Prop_Chest_01` family for Kanor's chest and current Notes chest lid/pickup vocabulary, with separate source/claim and no Notes-unlock side effect.

## Motion production and exact missing capabilities

Current `NpcHumanoid.controller` references standing/relaxed/briefcase idles and walk/stroll. `NpcBodyPose`/`StaticArmRig` provide limited procedural layers; these are reusable starts, not proof of believable reach/grasp. Arthur's rigged source and Elijah's generic idle import have different compatibility needs. Existing cutscene walking import/Timeline bindings remain protected.

| Capability currently not proven by existing clips | Owning production slice | Required reviewable output / blocker if unavailable |
|---|---|---|
| Grounded outdoor Elijah sprint/jump/land and transition to conversation/FP (stored disabled) | T22 (tentative) | Compatible authored/retargeted clip or bounded accepted pose sequence, with real motion/body/feet; report exact missing rig/clip if no available tool can produce it |
| Arthur point→reach→folder/ID grip→place/release | T02 pilot → T04 adoption | **T02 (11 Nov):** IK point/reach/approach, Humanoid finger-muscle **hook grip** (mitten hands: no thumb bones or geometry), attach only between grip and release, carry/place/release/retract on the existing avatar; user contact verdict pending. Thumb pinch needs a re-rigged hand (no DCC tool installed); page action needs a hinged folder variant |
| Arthur highlighter/card operating contacts | T09 | Existing compatible clip if truly found; otherwise AI-authored/retargeted local sequence/targeted IK proven at real station; no final “choose appropriate animation” discretion |
| Arthur machine intake/dial/Why?/lever contacts | T12 | Station-specific reaches with limited reusable contact primitives, actual dial/lever timing and hand origin; no disembodied tool automation as final |
| Arthur calculator/source/key/tape example contacts | T13 | One station body plus separate current example calculator; credible key press/receipt reach, explicit authority handoff |
| Client/reception/Auditor giving/receiving, carry/place/listen/reaction | T05/T06/T08/T10/T12/T14 | Per-actor source-compatible clip/pose/rig repair with reach targets; static arm drop alone cannot meet it |

First audit actual avatars/bone maps/clips and installed authoring tools. Prefer compatible existing animation, bounded retargeting or authored Unity assets/pose keys; use targeted posing/IK only where its contact/deformation is accepted. No new animation package or downloaded clip pack is presumed. Each named owner completes assets/builders/bindings; if genuinely impossible with available source/tools, record exact asset/tool blocker rather than assigning the user manual rig/scene assembly. Walking/stop/turn, talking/listening, place/give/receive and suitable workplace activity are per-slice acceptance, not replaced with head bob/sway.

## Voice pipeline and coverage dependency list

- Existing intro: nine recordings/eleven caption groups, current Timeline and subtitle owner. Five newer chapter catalogues:14 mapped MP3 takes including four Rosa histories. Preserve current paths and actual text/speaker/variant match; do not claim provider voice IDs/model/provenance that these catalogues do not store.
- T16 pilots actual DialogueSession speech after shortened sample scripts stabilize; metadata survives start snapshots and dynamic insertion. Exact text/revision/speaker/branch, take approval/provenance and local clip must agree. Missing/stale fallback is mandatory for robustness; no missing retained dialogue passes final acceptance.
- T17, T18, T19 each produce a branch-complete manifest from real callers, an exported recording sheet, imported approved takes, coverage validation and listening list. Code-authored/dynamic lines and Elijah count. Arthur guide/coach popups and silent labels/records/system notices are explicitly excluded with reason; ordinary dialogue may not be moved into that exclusion to save takes.
- User preference is the existing ElevenLabs-style user-supplied prerecorded workflow, with no spending authorized. New approved recordings and usable rights/provenance evidence are **not yet available**; name them E-VOICE-L1/L2/L3. The AI can author/export/import/match and verify; it cannot fabricate supplied takes or purchase/generate paid recordings here.
- Pilot measures current-device audio loading/residency, import compression, package impact, first-start delay, cancel/seek/pause and volume/ducking. Choose chapter loading/import settings from that evidence, not a guessed codec/memory ceiling. Offline playback is mandatory. No lip-sync, provider credentials/runtime TTS, read-every-number or paid-service mandate.

## Production gates under the one-week target

E-PILOT requires accepted visuals/contact and recorded costs before broad asset migration. E-DEVICE requires the current phone's model/Android/RAM and actual baseline/acceptance captures; ADB lookup was declined, so user evidence is needed. E-VOICE-L1/2/3 require shortened-script-matched takes and listening. E-CONTENT requires dated applicable curriculum approval. These remain named dependencies, with owner/status in [KNOWN_ISSUES](KNOWN_ISSUES.md); the deadline alone cannot close them.
