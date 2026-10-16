# TICKET-03 — Desk Interaction and Drag-and-Drop Overhaul

Status: Complete (user-verified in Play Mode, 2026-10-15)  
Dependencies: audit  
Implementation order: 3

Started on user instruction 2026-10-15. The user confirmed the desk interaction and animations in Play Mode on 2026-10-15 and marked the ticket done. See sections 15-16.

## 1. Objective and expected outcome

Make the case folder and tax code book draggable within the usable desk surface, with safe placement and responsive release while retaining current open/close flows and value-transfer mechanics.

## 2. Existing systems and files to inspect

- `Assets/Scripts/CaseFolder/CaseFolder3DDrag.cs`
- `Assets/Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs`
- `Assets/Scripts/Interactables/DeskInteractable.cs`, `Assets/Scripts/Player/WorkstationInteractor.cs`, and `CameraController.cs`
- `Office_Level1.unity` objects `Desk`, `DeskArea`, `DeskFolder`, `TaxCodeBookDeskDrop`, `CaseFolder`, `OpenCaseFolder`, and desk viewpoint
- Colliders and transforms on actual desk/book/folder objects
- Existing return-to-desk, HUD icon, and floating model scripts

## 3. Dependencies and prerequisites

- Audit complete; inspect current dirty diffs of both drag scripts and scene.
- Identify the true desk top geometry and local bounds. Do not assume the `DeskArea` transform itself has a collider.
- Keep current pointer ownership, UI blocking, profile viewer, and camera modes intact.

## 4. Exact implementation scope

- [x] Use one explicit desk surface plane/bounds for both draggable objects.
- [x] Ray/project pointer movement onto that surface and clamp to usable local bounds.
- [x] Preserve original object height/orientation and existing intentional snaps; reject/correct invalid outside/overlap placement.
- [x] Keep lift/smoothing responsive and restore a safe pose on cancel, disable, invalid ray, or failed drop.
- [x] Keep book/folder open, close, return, and HUD actions in their existing handlers.

## 5. Expected files/components/scenes

Likely: `CaseFolder3DDrag.cs`, `TaxCodeBook3DDrag.cs`, a compact desk surface component under `Assets/Scripts/Interactables/` or `Assets/Scripts/Desk/`, and serialized references in `Office_Level1.unity`. Verify all actual paths and preserve current scene YAML changes.

## 6. Recommended architecture and Unity practices

Share the same desk surface representation between folder and book. Prefer a serialized collider/transform and deterministic local-space clamping. Use one pointer/raycast owner. Do not create a competing global drag manager. Keep Rigidbody, collider and transform ownership consistent with the current object setup.

## 7. Phases and completion criteria

1. [x] Verify desk collider surface, local axes, bounds, and object colliders in the live scene.
2. [x] Implement a single placement contract and adapt both drag scripts minimally.
3. [x] Assign actual references and inspect serialized values.
4. [x] Verify center/edge/invalid/repeated/cancelled drops: geometry/source check, then user Play Mode check.

## 8. Integration requirements

Preserve tax case data, drag/drop transfer, snapping required by gameplay, document opening, HUD dragging, save/reset, and `CameraController` ownership. UI touches and dialogue must continue blocking world interaction where currently required.

## 9. Internal verification

Compile C#; inspect scene collider references and object bounds; exercise any available placement helper with boundary values. Play Mode/device behavior must be marked NOT RUN unless actually performed.

## 10. Manual testing checklist

- [x] Enter workstation view and drag folder/book to the center and edges of the desk.
- [x] Try releases outside the surface, overlapping another object, and on an invalid UI target.
- [x] Tap without meaningful movement; confirm the current open interaction remains correct.
- [x] Cancel/disable during drag and close/reopen each document; confirm controls and objects recover.
- [ ] Confirm HUD icon return and existing game progression work. (HUD drag-to-store not testable: `folderToHUDIcon` is unassigned on CaseFolder in Office_Level1.)

## 11. Acceptance criteria

- [x] Both objects can be placed across the configured usable desk surface.
- [x] Invalid releases remain within/recover to a safe desk pose.
- [x] Dragging remains responsive and no stuck state/input lock occurs.
- [x] Existing gameplay and value transfer remain intact.

## 12. Risks and rollback

Incorrect desk bounds or local rotation can move objects through geometry. Keep the surface component/configuration isolated; rollback the new bound calculation while retaining unrelated drag feedback and dirty work.

## 13. Continuation instructions

Inspect the exact current drag-script and scene diffs before editing. Verify the `DeskArea` geometry, do not infer collider bounds from its name. Update Ticket 03 and progress with the final placement contract and test evidence before Ticket 04.

## 14. Out of scope

Replacing the input system, changing player camera controls, redesigning case progression, new physics packages, or changing what tax answers transfer.

## 15. Implementation record (2026-10-15)

### Placement contract

- One `DeskPlacementSurface` on `ConsultantOffice/Desk` (new component fileID 190001486). Both `CaseFolder3DDrag` and `TaxCodeBook3DDrag` reference it through `deskSurface`.
- Surface = the BoxCollider on `Desk/Visual/SM_Prop_Desk_03` (fileID 1554425890; center 0,0.375,0; size 2.18 x 0.75 x 0.954; top at world y 0.75), which matches the visible desk top. The `Desk` root's own BoxCollider tops out at y 0.697, below the visible top, so it was not used.
- The pointer ray is projected onto a plane parallel to the desk top through the item's resting position at drag start. The held item stays under the finger and keeps its authored height and rotation.
- Clamping happens in the surface collider's local X/Z with a 0.035 m edge inset. It uses an asymmetric footprint (the item's bounds offsets from its pivot). The earlier symmetric footprint put the case folder's own resting pivot outside its allowed range (z -0.161 vs [-0.116, 0.116]), so the folder would jump about 4.5 cm on the first drag. The asymmetric range is [-0.266, 0.116].
- Obstacles (serialized): CaseFolder, TaxCodeBook, Stamps colliders. A blocked move first slides along the free desk axis; if both axes are blocked, the last valid target is kept. Inactive obstacles are ignored, and so is any obstacle the item already touches, so it can move away.
- While held, the item is lifted 0.015 m (`dragLiftHeight`) along the desk normal and smoothed with the existing `dragSmoothness`. On release, tap, invalid drop or disable it lands at the unlifted rest pose.
- Release rules are unchanged: tap (below `minimumDragDistance`) goes back to the start pose; a meaningful drag opens the document at the drop point. A release while the pointer ray misses the desk plane stays at the last valid pose and does not open. A case folder release over the HUD icon is now checked before the invalid-drop return, so storing still works.
- The stack snap-back (`OpenFolderReturnToDesk`, `OpenTaxCodeBookReturnToDesk`), `ResetFolder`, HUD store/restore, `CaseFolderUI`/`TaxCodeBookUI` and value transfer are untouched.

### Files changed

- `Assets/Scripts/Interactables/DeskPlacementSurface.cs`: plane-at-rest-height projection, asymmetric footprint, slide-on-overlap, lift offset, inactive obstacle handling.
- `Assets/Scripts/CaseFolder/CaseFolder3DDrag.cs`, `Assets/Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs`: use the new projection, keep last valid target on block, apply lift while held; folder checks HUD drop before invalid return.
- `Assets/Scenes/Office_Level1.unity`: added the surface component to `Desk` and set `deskSurface` on both drag components (22 added / 2 changed lines; no other hunks).

### Verification

- Compile: scratch copy of Assembly-CSharp.csproj with project references pointed at Library/ScriptAssemblies DLLs: 0 errors.
- Geometry check (Python, scene values): both resting pivots sit inside their clamp ranges with the asymmetric footprint. The folder would have sat outside with the symmetric one.
- ControlsRegression: 5,734 PASS.
- VerifyInteractionOutlines.py: all drag/hint/router checks PASS. One assertion fails before and after this change (`Mobile_Renderer.asset` outline width) in an asset this ticket did not touch.
- Play Mode / device: NOT RUN.

### Manual checks (priority order)

1. In workstation view, drag the folder and the book to every desk edge: they stop inside the desk and never sink into or float above it.
2. Drag one item into the other: it slides along or stops, never overlaps. Release: the document opens as before.
3. Tap without moving: nothing moves and nothing opens (same as before).
4. Close each open document: it snaps back to its stack. Reopen and repeat a few times.
5. Drag the folder onto the HUD icon, if that HUD reference is wired (`folderToHUDIcon` is currently unassigned on CaseFolder in Office_Level1).
6. If the 1.5 cm lift or 3.5 cm edge inset looks off, tune `dragLiftHeight` / `edgeInset` on `Desk > DeskPlacementSurface`.

### Rollback

Clear `deskSurface` on both drag components (or remove the component from `Desk`). Both scripts then fall back to the original camera-facing drag plane.

## 16. Desk experience pass (2026-10-15, user request)

The user asked for animations (opening, dragging, page changes), for Exit Desk to put the folder and book back in their original places instead of leaving them floating, and for the Exit Desk button to match the NPC dialogue button style at its current size.

### Behaviour

- **Opening:** the open folder or book rises out of the closed item's spot. It starts at 45% size, tipped 55 degrees back toward the desk, and grows into its normal pose over 0.38 s with a slight overshoot.
- **Closing on the stack:** dropping an open document on its stack makes it shrink and fly down to the stack over 0.26 s. The closed item then drops the last 4 cm onto the desk. The open model's collider is off during the flight, so it can't be grabbed mid-air.
- **Dragging closed items:** besides the 1.5 cm lift, the item tilts up to 8 degrees toward the pull direction, and the tilt fades as it catches up. Tap, invalid release and disable all restore the authored rotation.
- **Page changes:** the paper flips about its own vertical axis (0.26 s). It turns edge-on, the content swaps at the midpoint, then it turns back in from the other side with a slight dip in scale. Folder pages and book sections both use it. Rapid taps restart the flip, and the newest page renders at the midpoint. The paper's authored rotation and scale (folder paper scale 0.7 x 1 x 1) are restored exactly.
- **Exit Desk:** `CameraController.ExitFirstPerson` now raises a static `WorkstationExited` event, and `DeskDocumentAnimator` on `Desk` handles it:
  - An open folder or book closes back to its **original scene position** (not the stack position). The book's page UI is hidden.
  - A closed item that was moved slides back along a low arc.
  - A folder stored in the HUD is left alone.
  - Corkboard exits also raise the event, which is harmless because idle documents are already at rest.
- **Exit Desk button:** fill colour (0.95, 0.8, 0.43) and dark label (0.08, 0.08, 0.1) copied from the dialogue Continue button. Size stays 400 x 80, font size stays 35.

### Files

- New: `Assets/Scripts/Interactables/DeskMotion.cs` (easing, held tilt, page flip coroutine) and `DeskDocumentAnimator.cs` (open/close/settle/slide tweens, exit return), with `.meta` files.
- `CaseFolder3DDrag.cs`, `TaxCodeBook3DDrag.cs`: open animation, held tilt, `ReturnToOriginalPlace(bool)`, finish running tweens on grab/reset. The book now records its original closed pose, and `ResetTaxCodeBook` restores it.
- `OpenFolderReturnToDesk.cs`, `OpenTaxCodeBookReturnToDesk.cs`: an animated close on pointer drop. `ForceReturnToDesk` / `ResetTaxCodeBook` stay instant (audit retry).
- `CaseFolderUI.cs`, `TaxCodeBookUI.cs`: page flip on Next/Previous. Optional `pageTurnRoot` defaults to the heading's parent (Paper).
- `CameraController.cs`: `WorkstationExited` event (8 added lines).
- `Office_Level1.unity`: `DeskDocumentAnimator` on `Desk` (fileID 190001487), `deskAnimator` set on both drag components, and the Exit button colours.

### Verification

- Compile: 0 errors.
- VerifyInteractionOutlines drag checks PASS. ControlsRegression 5,734 PASS. DialogueRegression 796 PASS. ProgressionRegression PASS.
- Play Mode / device: NOT RUN.

### Manual checks

1. Drag-release the folder and the book: each rises and grows from its desk spot.
2. Hold either item and move it quickly: it tilts toward the motion and levels out when you stop.
3. Tap the page edges repeatedly: the paper flips, the content changes, and the paper never ends up rotated or scaled wrongly.
4. Drop each open document on its stack: it flies down and the closed item lands.
5. With both documents open (or moved), press Exit Desk: both go back to their original spots. Sit down again and repeat.
6. Store the folder in the HUD, then exit the desk: it should stay stored.
7. The Exit Desk button should be gold with dark text, at the same size and position.

Tuning lives on `Desk > DeskDocumentAnimator` (durations, start scale/tilt, settle drop, slide arc). The page flip speed is `pageTurnDuration` on each UI. Setting a duration to 0 turns that animation off.

### Note for Ticket 04

The page flip here is a 2D-paper flip. Ticket 04's physical printed-document presentation may replace it, but it should keep `TurnToCurrentPage` as the single entry point.

### Follow-up: Exit Desk button style applied in code (2026-10-15)

The user still saw the old white button because Unity had not reloaded the edited scene. `WorkstationUI` now applies the style itself in `Awake`: it copies the fill, sprite, colour transitions, label colour and font from `styleSource` (the dialogue Continue button, wired in the scene). Without `styleSource` it falls back to the same gold (0.95, 0.8, 0.43) and dark label (0.08, 0.08, 0.1). The button's size (400 x 80) and font size (35) are not changed. Compile: 0 errors.
