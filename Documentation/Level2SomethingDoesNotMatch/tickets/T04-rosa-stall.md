# T04 — Meet Rosa at a complete 3D market encounter

Status: Implemented — internally verified; user Play Mode pending (2026-10-17). Blocked by: T03.

## Visible accomplishment

Following the plaza route brings the player to Rosa's small alterations stall. Rosa responds, offers a physical envelope, and the nearby records desk becomes available.

## Work

Inspect scene space before choosing coordinates. Use `SM_Chr_Business_Female_03` under `TaxVentureWithCompliance > Characters` for Rosa, with prefab `Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Business_Female_03.prefab`. Use the red-and-white awning store selected as `Building_4 (1)` under `TownEnvironment > Env > Town`, based on `Assets/Flat_Style_Vehicles/Prefabs/Buildings/Building_4.prefab`, near the plaza. Inspect and preserve relevant instance overrides; do not confuse it with the other Building_4 instance. Preserve both reference scenes and prefab originals. Reuse an existing guide model. Build only missing signage, cloth bundles and records-desk additions using Unity geometry/existing props; do not build a substitute generic stall or canopy. Record final transforms and hierarchy in PROGRESS. Provide colliders and clear approach/camera space; never block the route. Runtime assembly or a reproducible idempotent builder must wire the scene without user assembly.

Use current dialogue, turn-to-speaker and compatible gesture clips. Two required short information beats can be asked in either order: referral source and original records. Rosa asks whether the case can be finished today. Do not state tax classifications or accuse the Desk. One identity owns stall and later completion appearances.

Build/render the six authentic-looking fictional records from CONTENT, with matching dates/references. Envelope slides and records fan into a tray; all evidence is acquired exactly once after dialogue. Grant the one Rosa conversation heart through existing ledger. Records remain collected when the player exits, revisits or fails later.

## Acceptance

Internal status (source checks only; each box also needs the user's Play Mode check):

- [x] Stall, desk, envelope and documents are physical 3D scene items, not a flat substitute. *(runtime primitives + originals)*
- [x] Rosa uses the selected SM_Chr_Business_Female_03 model with a compatible idle (humanoid Standing Idle); dialogue identity "Aling Rosa"; no portrait art yet (none shown); records supplied in one visit. *(T-pose fix needs the user's check)*
- [x] Store reuses Building_4 (mirrored X, like Building_4 (1)) near the plaza; source scenes/prefabs unchanged (hashes recorded).
- [x] Six records with distinct headings and correct, cross-checked values/references. *(printed as case-folder pages in page 1's style, by user direction)*
- [x] Required conversation and evidence acquisition occur once; no reward farming.
- [x] Market desk objective and workstation entry become available; no new walking AI.

## Verification/handoff

Source/fixture/scene-builder validation plus dialogue/progression tests and compile. User later checks path, visibility, character framing and record readability. Report exact character/store prefab paths, preserved instance overrides, animation bindings and final transforms. Next T05.
