# Known issues and limitations

- The repository was already dirty at the audit baseline. Before edits to any shared scene or script, inspect and preserve its live diff.
- Only NPC_Marie, NPC_Jhun and NPC_Zorbix have animated (Humanoid "Standing Idle") models; the other 8 NPCs use static meshes with the procedural head fallback. The supplied character animation controllers contain a single idle state with no parameters or transitions. Existing walking FBXs do not by themselves prove the scene NPCs have compatible, assigned locomotion clips. Talk/listen/gesture animation availability remains unverified.
- Desk drags are bounded by `Desk > DeskPlacementSurface` (Ticket 03) and were user-verified in Play Mode. Overlap tests use world AABBs, so rotated items keep a slightly larger gap than their real outline. `CaseFolder3DDrag.folderToHUDIcon` is unassigned in Office_Level1, so drag-to-HUD storing cannot be exercised there.
- Current folder and book page changes update TMP content in place; there is no verified physical page-turn animation.
- The office minimap must be checked for roof and wall occlusion; the tutorial's scene/layers cannot be copied directly.
- Unity is already running for this project. Do not launch another instance. No Play Mode, device, or visual verification has been claimed.
- The tutorial transcript has been reviewed. Its key architecture is summarized in the audit and README; implementation must still be checked against actual office scene behavior.

## Walking animations (deferred)

NPC walking is disabled (`NpcIdlePacing.enableWalking` = false). In play-testing, walkers slid their feet and turned oddly. Clip speed matching, turning and static meshes with no legs still need work; see Ticket 01 section 18.

## Current checkpoint

Tickets 01-03 are user-verified. `Tests/VerifyInteractionOutlines.py` fails one pre-existing assertion (`Mobile_Renderer.asset` outline width) that Ticket 03 did not touch. Office_Level1_COPY and Office_Level2 are excluded from future work.

## Desk animation pass (Ticket 03 section 16)

- User-verified in Play Mode. The open/close tilt direction is derived from the camera position; if a document tips the wrong way, flip the sign of `openStartTilt` on `Desk > DeskDocumentAnimator` or report it.
- `CameraController.WorkstationExited` fires for every workstation exit (desk and corkboard). The desk handler is idempotent.
- Closing on the stack still requires releasing over the stack collider (`DeskFolder` / `TaxCodeBookDeskDrop`), as before.
- If Unity is not told to reload Office_Level1 after an on-disk scene edit, Play Mode uses the stale in-memory scene. That happened on 2026-10-15: no animator, white button. The drag scripts now add `DeskDocumentAnimator` to the desk-surface object at runtime if it is missing, but the gold Exit button colour exists only in the scene file. The 3 "referenced script (Unknown) is missing" warnings in the Editor log predate this initiative.

## Ticket 04 presentation

- User-verified in Play Mode. The page transition is a 2D paper flip, not a physical page curl (no page rig exists). The under-sheets may need offset tuning in `PrintedPaperStyle` if they show too much or too little at the desk camera angle.
