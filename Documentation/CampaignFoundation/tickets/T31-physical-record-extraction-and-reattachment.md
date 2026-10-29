# T31 — Handle Arthur's attached records as physical 3D objects

Status: Planned -> Implemented, internally verified; user manual pending (2026-10-26; evidence in PROGRESS.md "T31"). Blocked by: T30.

## Goal and visible accomplishment

Arthur's second page contains actual attached 3D documents. Elijah can lift them into the existing floating-document workspace, compare and reorder them, then return them to the folder. This completes T30's visible opening and supplies the same mechanics to Marie in T25. A floating UI rectangle or thicker flat panel alone is not finished art.

## Physical page and objects

Use the [supporting document standard](../../SupportingDocuments.md). The identity card sits near the top of **Supporting Records** in shallow corner holders; the background sheet is beneath it under a small modeled clip. Give both generous separation and clear grab targets. They are children/attachments of the owning file in the resting view, not unrelated overlay windows. Keep source title/IDs and original attachment anchors when extracted.

The ID has visible thickness, softly beveled edges, matte material and a subtle raised portrait frame with the shared generic bust silhouette. No real client portrait, shiny glass badge or dense microprint. The background sheet has a visible thin edge, slight curvature and subtly lifted corners, warm cream paper, simple **Home**/**Work** blocks and short useful facts. A restrained clip-release motion and a contact shadow show the paper leaving the page. Keep crisp editable printing on the object's face; text need not be extruded. Match existing low-poly materials/light and document compositing without rewriting the render pipeline. Verify real depth and lighting from the reading angle, rather than assuming a world canvas makes it look physical.

## Interaction contract

1. Tap to extract and enlarge; drag can extract directly as the existing Findings paper does. Settle in a readable pose with a short lift/turn, then hold still. Tapping the second record opens it alongside the first; both remain individually accessible.
2. Drag with direct finger tracking. Clicking a record brings its full object, text and interaction surface to the front. Use the existing floating-paper focus, raycaster, `Doc3DLayer`/equivalent and `HeldOnScreen` fitting. Its visible bounds stay inside the safe screen area on opening, dragging, front-focus and viewport changes. On a narrow phone, focus one larger face at a time without hiding access to the other or shrinking text into microprint.
3. Drop over the owning folder's supporting-page return region to reattach to the correct holder, with a short settle/clip cue. The ghost/target describes a valid return only, not factual correctness. If another folder page is visible, a drop on the owning folder may turn to Supporting Records and return, matching Findings behavior. Dropping onto another client's folder is refused gently; no cross-file ownership transfer.
4. Preserve the existing optional hold-and-stack gesture, with **two seconds of stationary hold** to begin grouping. Show a small progress cue and cancel on movement beyond the existing drag threshold, pointer cancel or focus loss. Do not make ordinary taps wait two seconds or introduce a compulsory gesture explanation. Group only detached records from the same folder; keep a slight stagger and stable original order. A grouped drag remains fully on screen, moves as one selection, and dropping onto the owning page restores each original attachment exactly once.
5. Folder close/store, Back, settings, dialogue, camera change and scene exit gather detached records to their owner or restore a safe closed/idle state. If source ownership was not committed, show no successful storage animation. Returning a paper never grants, erases or duplicates a document or read milestone. Rereading stays available.

## Reuse and source boundaries

Inspect Level 2 Findings lift/return, `AnnotationPaper` hold/collection, floating focus/input locks, existing mesh builders, `SupportingDocumentFace`, T30's per-folder sources and page decorator. Reuse narrowly; do not fork a separate input/window framework. Any shared fixes retain existing Findings/annotation behavior and receive targeted regression coverage. Preserve GUIDs, record identity, semantic values and active-case binding. No live Level 2 scene/content migration here; T18 later adopts presentation.

Expose enough layout/attachment placement configuration for T25 to put Marie's ID, background and 2316 on her own second page. Two pages is the Level 1 folder shell, not a campaign-wide two-page restriction on Rosa's existing packet. Later adopting level tickets extend only their needed types through this same physical path, following the fixed T27 roadmap.

## Animation, sound and lifecycle

Use gentle lift/turn 0.25–0.4 s, return/page motion 0.2–0.3 s and gather/store 0.35–0.5 s as editable starting targets. Card motion is slightly rigid; sheet flex is restrained. Direct dragging has no spring lag; no idle bob in inspection, camera shake, confetti or repeated rustle. Existing effects-volume settings own one cue per deliberate action. Do not demand a new NPC rig or image-generation assets.

Save only canonical ownership and durable inspection milestones through T30's owner; detached positions, stack offsets and animation frames are transient. Resume reattaches records with no replay of dialogue/rewards. Implementers finish models, anchors and bindings; user acceptance is a visual/touch check, not manual assembly. Record provisional/missing art honestly.

## Internal checks — AI source/build/regressions only

- [ ] Per-folder extraction/return and close cleanup preserve source IDs; foreign-folder drops and mixed-owner stacks are refused.
- [ ] Hold threshold, movement cancellation, front order, repeated taps, grouped returns, viewport fitting declarations and interrupted animation are covered at actual input seams.
- [ ] Reopening/restoring never loses papers or changes active case; source/build and affected Level 2 paper controls pass.

## USER manual acceptance

- [ ] See card thickness, bevels, sheet curvature, physical holders and shadows from tilted and readable views; flat UI styling is not accepted as final.
- [ ] Extract both papers, overlap/focus them, drag to every edge, hold two seconds, stack and return; no clipping, off-screen text or delayed finger tracking.
- [ ] Close/store with papers out; reopen from the bag at the board and desk. Repeat with SFX off/on and narrow/wide phone framing.
- [ ] Paper handling is satisfying but optional stacking does not interrupt Arthur's short route.

## Non-goals and handoff

No evidence-attachment answer puzzle, cross-client transfer, freeform physics pile, new reward, mandatory advanced-control lesson or extra folder page in Arthur's file. Record production assets/meshes, control reuse, resolved attachment regions and actual internal results separately from manual acceptance. Next: [T25](T25-marie-desk-record-connection.md), using these mechanics for Marie's editable two-page case.
