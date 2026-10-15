# TICKET-04 — Immersive 3D Case Folder and Tax Code Book

Status: Planned  
Dependencies: Ticket 03  
Implementation order: 4

Current instruction (2026-10-15): setup only. Do not begin or resume this ticket without a new user instruction. Existing partial implementation is historical, unverified work.

## 1. Objective and expected outcome

Make the actual folder and tax book pages read as printed documents integrated with their existing 3D meshes, while preserving current content, interactive fields, case data, and navigation state.

## 2. Existing systems and files to inspect

- `Assets/Scripts/UI/CaseFolderUI.cs`, `TaxCodeBookUI.cs`, `CaseFolderPageContent.cs`
- `Assets/Scripts/Data/TaxCodeBookData.cs` and the assigned data assets
- `CaseFolderFieldRow`, `TaxCodeValueChip`, `ValueDragSource`, `ValueClickDestination`, and `ValueTransferManager`
- Actual open/closed model hierarchy, colliders, canvas render mode, TMP sizing, page roots, and serialized references in office scenes
- Existing BIR form and paper/material assets under `Assets/3D Models/Core Items/` and folder/book asset folders

## 3. Dependencies and prerequisites

- Ticket 03 defines where the objects settle and how document surfaces are presented.
- Inspect actual page/model/Canvas alignment before changing visual hierarchy.
- Reuse existing project facts and tax content; no new statutory language or case facts may be invented.

## 4. Exact implementation scope

- [ ] Integrate clear paper backgrounds, typography, labels, and hierarchy with the existing open 3D folder/book pages.
- [ ] Add a convincing, short page transition where it can be done without breaking drag state; explicitly identify a transform/crossfade fallback if no page rig exists.
- [ ] Keep clickable/dragged values legible and their raycast/collider targets unobstructed.
- [ ] Preserve page/section data and active values when navigating; block/defer navigation during active value dragging.
- [ ] Update actual scene/prefab presentation, not a disconnected mockup.

## 5. Expected files/components/scenes

Likely existing UI scripts (`CaseFolderUI.cs`, `TaxCodeBookUI.cs`), page content data, targeted page prefabs/materials, and serialized open document objects in `Office_Level1.unity`. Add a focused page transition helper only if existing UI animation cannot provide the behavior. Verify all locations before editing.

## 6. Recommended architecture and Unity practices

Separate presentation from case data and transfer logic. Favor existing world-space/page UI surfaces and existing meshes. Reuse TMP and UGUI already installed. Do not generate elaborate custom meshes or add packages. Keep text readable at the interaction camera distance and avoid extra raycast blockers.

## 7. Phases and completion criteria

1. [ ] Record the live canvas/page hierarchy, its model surface, and interactive hit targets.
2. [ ] Apply visual treatment and an appropriate bounded page transition.
3. [ ] Configure and inspect the actual Office scene references and value interaction components.
4. [ ] Verify page state, drag, transfer, and validation with focused checks; record whether visual Play Mode was available.

## 8. Integration requirements

Do not change case facts, legal/tax claims, field keys, `TaxCodeBookData` structure, transfer validation, answer correction, or progression. Preserve scene GUIDs and TMP layout references.

## 9. Internal verification

Compile; inspect page references and raycast targets; run existing progression/transfer checks if their behavior is affected. Do not equate compilation with visual readability or a physical page-turn pass.

## 10. Manual testing checklist

- [ ] Open folder and book in workstation view; inspect paper, typography, page edges, and reading distance.
- [ ] Navigate first/middle/last page and confirm correct content/indicator.
- [ ] Drag values before and after navigation and confirm destination behavior.
- [ ] Correct/replace transferred values and confirm data remains intact.
- [ ] Close/reopen and confirm page and gameplay state do not become stuck.

## 11. Acceptance criteria

- [ ] Existing folder/book meshes visibly support a printed-page presentation.
- [ ] Interactive values remain selectable and transferable.
- [ ] Navigation preserves the correct content and active interaction state.
- [ ] Any page motion is accurately described as animated or as a limited transition.

## 12. Risks and rollback

Changing canvas scale/order can impair value hit areas or readability; dynamic field regeneration can lose transient values if data is not authoritative. Keep visuals separable and restore the prior presentation without reverting the underlying data logic.

## 13. Continuation instructions

Inspect the actual open-book/folder scene hierarchy and current dirty UI script diffs. Confirm whether page geometry is animated before promising a physical turn. Record what is actually implemented, then proceed to Ticket 05.

## 14. Out of scope

New tax laws, statutory quotations, new case facts, replacing the case data model, redesigning answer rules, or importing an unverified document asset pack.
