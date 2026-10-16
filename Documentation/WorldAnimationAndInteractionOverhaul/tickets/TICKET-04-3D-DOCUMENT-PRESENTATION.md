# TICKET-04 — Immersive 3D Case Folder and Tax Code Book

Status: Complete (user-verified in Play Mode, 2026-10-15)  
Dependencies: Ticket 03  
Implementation order: 4

Started on user instruction 2026-10-15. Code complete and compiling; Play Mode NOT RUN. See section 15 and `handoffs/TICKET-04_WORK_LOG.md`.

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

- [x] Integrate clear paper backgrounds, typography, labels, and hierarchy with the existing open 3D folder/book pages.
- [x] Add a convincing, short page transition where it can be done without breaking drag state; explicitly identify a transform/crossfade fallback if no page rig exists.
- [x] Keep clickable/dragged values legible and their raycast/collider targets unobstructed.
- [x] Preserve page/section data and active values when navigating; block/defer navigation during active value dragging.
- [x] Update actual scene/prefab presentation, not a disconnected mockup.

## 5. Expected files/components/scenes

Likely existing UI scripts (`CaseFolderUI.cs`, `TaxCodeBookUI.cs`), page content data, targeted page prefabs/materials, and serialized open document objects in `Office_Level1.unity`. Add a focused page transition helper only if existing UI animation cannot provide the behavior. Verify all locations before editing.

## 6. Recommended architecture and Unity practices

Separate presentation from case data and transfer logic. Favor existing world-space/page UI surfaces and existing meshes. Reuse TMP and UGUI already installed. Do not generate elaborate custom meshes or add packages. Keep text readable at the interaction camera distance and avoid extra raycast blockers.

## 7. Phases and completion criteria

1. [x] Record the live canvas/page hierarchy, its model surface, and interactive hit targets.
2. [x] Apply visual treatment and an appropriate bounded page transition.
3. [x] Configure and inspect the actual Office scene references and value interaction components.
4. [x] Verify page state (focused checks, then user Play Mode check), drag, transfer, and validation with focused checks; record whether visual Play Mode was available.

## 8. Integration requirements

Do not change case facts, legal/tax claims, field keys, `TaxCodeBookData` structure, transfer validation, answer correction, or progression. Preserve scene GUIDs and TMP layout references.

## 9. Internal verification

Compile; inspect page references and raycast targets; run existing progression/transfer checks if their behavior is affected. Do not equate compilation with visual readability or a physical page-turn pass.

## 10. Manual testing checklist

- [x] Open folder and book in workstation view; inspect paper, typography, page edges, and reading distance.
- [x] Navigate first/middle/last page and confirm correct content/indicator.
- [x] Drag values before and after navigation and confirm destination behavior.
- [x] Correct/replace transferred values and confirm data remains intact.
- [x] Close/reopen and confirm page and gameplay state do not become stuck.

## 11. Acceptance criteria

- [x] Existing folder/book meshes visibly support a printed-page presentation.
- [x] Interactive values remain selectable and transferable.
- [x] Navigation preserves the correct content and active interaction state.
- [x] Any page motion is accurately described as animated or as a limited transition.

## 12. Risks and rollback

Changing canvas scale/order can impair value hit areas or readability; dynamic field regeneration can lose transient values if data is not authoritative. Keep visuals separable and restore the prior presentation without reverting the underlying data logic.

## 13. Continuation instructions

Inspect the actual open-book/folder scene hierarchy and current dirty UI script diffs. Confirm whether page geometry is animated before promising a physical turn. Record what is actually implemented, then proceed to Ticket 05.

## 14. Out of scope

New tax laws, statutory quotations, new case facts, replacing the case data model, redesigning answer rules, or importing an unverified document asset pack.

## 15. Implementation record (2026-10-15)

### What was built

- **Printed-paper look** (`Assets/Scripts/UI/PrintedPaperStyle.cs`). It is applied once at runtime from `CaseFolderUI.Show()` / `TaxCodeBookUI.Show()` on the existing `Paper` rect of each open document's world-space canvas. Because it is code-driven, it needs no scene YAML and shows up even when Unity hasn't reloaded the scene. It adds:
  - a soft drop shadow on the paper;
  - two slightly offset, slightly tilted, darker sheets behind it (siblings of `Paper`), so the page reads as the top of a printed stack and the flip turns the top sheet over a still stack;
  - a thin printed border inset 28 units from the edge;
  - heading, body and page-indicator text recoloured from pure black to ink (0.12, 0.13, 0.17). Only pure-black text is recoloured.
- Every decoration has `raycastTarget = false`, so value rows, chips and page-edge zones are unobstructed. Paper size, position, rotation and scale are not changed, so the Ticket 03 page flip still restores the exact rest pose.
- **Page transition:** the Ticket 03 flip (`DeskMotion.TurnPage`) is the transition. It is an animated 2D-paper flip about the page's vertical axis, not a physical mesh page-curl; there is no page rig.
- **Navigation is blocked during value drags:** `ValueDragSource.IsAnyDragActive` (static; cleared on drag end and in `OnDisable`). Folder and book Next/Previous return early while a value is dragged, because changing page rebuilds rows and chips.
- Not changed: case data, field keys, `TaxCodeBookData`, transfer validation, progression, and the row/chip prefabs.

### Verification

- Compile 0 errors. VerifyInteractionOutlines drag checks, ControlsRegression 5,734, DialogueRegression 796 and ProgressionRegression all PASS.
- Play Mode / device: NOT RUN.

### Manual checks

1. Open the folder and the book: the paper should have a shadow, two sheets peeking out underneath, a thin border, and dark ink text that is still readable at desk distance.
2. Flip pages: the top sheet turns while the sheets under it stay still, and the content changes correctly on first, middle and last pages.
3. Start dragging a value chip/row and, while holding, tap a page edge: the page must NOT change. Drop the value and confirm the transfer still works.
4. Correct/replace a transferred value, then close and reopen the documents.

### Tuning / rollback

Offsets, colours and the border inset are constants at the top of `PrintedPaperStyle`. To roll back, remove the two `PrintedPaperStyle.Apply(...)` lines in the UIs; the navigation gate is independent.

## 16. Printed-ink restyle (2026-10-15, round 2 after user screenshots)

The user said the first pass still looked like UI: white field boxes that looked like input fields, white chip buttons, and the section 2 descriptions overlapping the chips. Changes:

- **Folder field rows** (`CaseFolderFieldRow` -> `PaperInkStyle.StyleFieldRow`):
  - Text reads as a printed form line: a small grey-ink caption (`Name:`) followed by the value in dark-blue pen ink. A missing value `?` is shown in faint grey.
  - Drop-target rows show a pale-blue fill-in box with a thin ink edge, so it's clear they accept drops.
  - Value-only rows have no box, just a printed underline.
  - All rows show small grip dots, since they can be dragged.
- **Tax-code values** (`TaxCodeValueChip` -> `StyleValueChip`): a soft highlighter-marker strip with a thin edge, bold ink text, and grip dots on the left.
- **Selection tint and green/red feedback** still work. They now return to the new resting colours, via `ValueSelectionHighlighter.SetColors` and `ValueTypeValidationFeedback.SetRestingColor`.
- **Book descriptions:** each description is now printed directly under its value, so they can no longer drift or overlap. This applies when the section body has one paragraph per value (true for all three sections); otherwise the authored body layout is kept.
- Not changed: data, display values, field keys, transfer and validation rules, prefabs and scene. Everything is applied at runtime.

Verification: compile 0 errors. Outline drag checks, ControlsRegression 5,734, DialogueRegression 796 and ProgressionRegression all PASS. Play Mode NOT RUN.

Manual checks:

1. Folder page 1 should read like a printed form, with the `?` fields in blue fill-in boxes.
2. Book sections 1-3: each value sits on a yellow marker strip with its description directly underneath, and nothing overlaps.
3. Tap or drag a value onto a `?` field: the selection turns a stronger yellow, the green/red flash still happens, and afterwards the field returns to its blue box (not white).
4. Values filled in by a drop show in blue pen ink.

## 17. Book layout fix (2026-10-15, round 3)

- Long value labels now stay on one line and shrink to fit (auto-size 24-40) inside a taller 58-unit strip.
- Descriptions are bigger (start at 36) and flow as one paragraph. They shrink automatically only when a section would not fit.
- The value list area grows from 600x750 to 700x860 (same top edge) while descriptions are printed under the values. That space was previously used by the separate body text.
- Compile 0 errors; regressions pass. Play Mode NOT RUN. Details are in `handoffs/TICKET-04_WORK_LOG.md` (round 3).

## 18. Drop feedback and centred folder (2026-10-15, round 4)

- **Validation rule unchanged:** `IsCorrectValue` (type + semantic key = the field's category). The right kind of value turns green even if the answer itself is wrong; the wrong kind turns red.
- **New feedback visuals** (`DropFeedbackFX` + `ValueTypeValidationFeedback`):
  - Soft mint/coral tint pulse instead of a hard pure-green/red flash.
  - A check or cross badge pops in at the field's right edge.
  - An accepted drop gives a small scale "pop"; a rejected drop gives a head-shake wobble.
  - The field edge glows briefly.
  - Everything resets to the resting look afterwards. It also applies to the computer-form slots that use the same feedback component.
- **Case folder (3D paper only):** heading, rows and document links sit in one centred 760-wide column (was 650, offset right), and the heading is centred. The left page-turn strip mirrors the right one so it doesn't overlap the wider column.
- Compile 0 errors; regressions pass. Play Mode NOT RUN.

## 19. Completion

The user confirmed all rounds in Play Mode on 2026-10-15 ("it looks good") and marked the ticket complete.
