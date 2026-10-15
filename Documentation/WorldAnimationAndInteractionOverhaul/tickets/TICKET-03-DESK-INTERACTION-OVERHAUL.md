# TICKET-03 — Desk Interaction and Drag-and-Drop Overhaul

Status: Planned  
Dependencies: audit  
Implementation order: 3

Current instruction (2026-10-15): setup only. Do not begin or resume this ticket without a new user instruction. Existing partial implementation is historical, unverified work.

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

- [ ] Use one explicit desk surface plane/bounds for both draggable objects.
- [ ] Ray/project pointer movement onto that surface and clamp to usable local bounds.
- [ ] Preserve original object height/orientation and existing intentional snaps; reject/correct invalid outside/overlap placement.
- [ ] Keep lift/smoothing responsive and restore a safe pose on cancel, disable, invalid ray, or failed drop.
- [ ] Keep book/folder open, close, return, and HUD actions in their existing handlers.

## 5. Expected files/components/scenes

Likely: `CaseFolder3DDrag.cs`, `TaxCodeBook3DDrag.cs`, a compact desk surface component under `Assets/Scripts/Interactables/` or `Assets/Scripts/Desk/`, and serialized references in `Office_Level1.unity`. Verify all actual paths and preserve current scene YAML changes.

## 6. Recommended architecture and Unity practices

Share the same desk surface representation between folder and book. Prefer a serialized collider/transform and deterministic local-space clamping. Use one pointer/raycast owner. Do not create a competing global drag manager. Keep Rigidbody, collider and transform ownership consistent with the current object setup.

## 7. Phases and completion criteria

1. [ ] Verify desk collider surface, local axes, bounds, and object colliders in the live scene.
2. [ ] Implement a single placement contract and adapt both drag scripts minimally.
3. [ ] Assign actual references and inspect serialized values.
4. [ ] Verify center/edge/invalid/repeated/cancelled drops with a focused geometry/source check and Editor test if available.

## 8. Integration requirements

Preserve tax case data, drag/drop transfer, snapping required by gameplay, document opening, HUD dragging, save/reset, and `CameraController` ownership. UI touches and dialogue must continue blocking world interaction where currently required.

## 9. Internal verification

Compile C#; inspect scene collider references and object bounds; exercise any available placement helper with boundary values. Play Mode/device behavior must be marked NOT RUN unless actually performed.

## 10. Manual testing checklist

- [ ] Enter workstation view and drag folder/book to the center and edges of the desk.
- [ ] Try releases outside the surface, overlapping another object, and on an invalid UI target.
- [ ] Tap without meaningful movement; confirm the current open interaction remains correct.
- [ ] Cancel/disable during drag and close/reopen each document; confirm controls and objects recover.
- [ ] Confirm HUD icon return and existing game progression work.

## 11. Acceptance criteria

- [ ] Both objects can be placed across the configured usable desk surface.
- [ ] Invalid releases remain within/recover to a safe desk pose.
- [ ] Dragging remains responsive and no stuck state/input lock occurs.
- [ ] Existing gameplay and value transfer remain intact.

## 12. Risks and rollback

Incorrect desk bounds or local rotation can move objects through geometry. Keep the surface component/configuration isolated; rollback the new bound calculation while retaining unrelated drag feedback and dirty work.

## 13. Continuation instructions

Inspect the exact current drag-script and scene diffs before editing. Verify the `DeskArea` geometry, do not infer collider bounds from its name. Update Ticket 03 and progress with the final placement contract and test evidence before Ticket 04.

## 14. Out of scope

Replacing the input system, changing player camera controls, redesigning case progression, new physics packages, or changing what tax answers transfer.
