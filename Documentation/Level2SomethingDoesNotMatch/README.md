# Level 2 — Something Does Not Match

Status: all 12 tickets implemented and internally verified (2026-10-19). The polish and completion pass (2026-10-20, see SPEC "Polish and completion pass") simplified the case, made the annotated papers the schedule's value source and added the Tax Code Book icon with cheat sheets; user Play Mode acceptance is pending. Details in [PROGRESS.md](PROGRESS.md) ("Final handoff"). Prepared 2026-10-17; business wording updated 2026-10-18. Rosa's Laundry and the simpler blanket-washing payment are now implemented in runtime content; user Play Mode acceptance is pending.

## The experience

Retrieve Aling Rosa's folder at the office, notice an unaccounted payment, record a preliminary hold, visit her market stall near the plaza, complete the scored **Mark the Money!** document challenge, return with evidence, reconcile her prepared summary, apply a physical readiness stamp, and submit to the Auditor. Finish by explaining the outcome to Rosa and recording the first Assistance Desk connection.

One client, one required excursion, one scored side quest, one omission, one explainable mismatch. Learning happens through looking, selecting, annotating, comparing, correcting, and defending a decision. No second case, trust meter, free typing, new currency, elaborate deduction board, or arithmetic minigame.

## Read order

1. [AGENTS.md](AGENTS.md): implementation boundaries, user testing and git ownership.
2. [SCENE_REUSE.md](SCENE_REUSE.md): create the new scene once from current Level1, preserve shared assets, specialize gameplay safely.
3. [SPEC.md](SPEC.md): complete flow, state transitions, mechanics, outcomes, user stories.
4. [CONTENT.md](CONTENT.md): exact fictional document set, answer keys, dialogue and teaching boundaries.
5. [REUSE_AND_ARCHITECTURE.md](REUSE_AND_ARCHITECTURE.md): inspected source map, keep/change/disable decisions and contracts.
6. [WORLD_AND_FEEDBACK.md](WORLD_AND_FEEDBACK.md): 3D assets, layout, animation, sound and mobile interactions.
7. [REWARDS_AND_VALIDATION.md](REWARDS_AND_VALIDATION.md): scoring, recovery, internal checks and user Play Mode checklist.
8. [TICKETS.md](TICKETS.md): dependency order and concrete deliverables.
9. [PROGRESS.md](PROGRESS.md), [KNOWN_ISSUES.md](KNOWN_ISSUES.md), then the active ticket.

## Accepted decisions

- Rosa runs **Rosa's Laundry**. Main earnings are laundry payments; the other receipts are payment for a separate blanket-washing job and payment for renting out her spare washing machine. Preserve all IDs, amounts, evidence relationships and level mechanics; see CONTENT for the exact mapping.

- Preserve Level 1's core tools and touch vocabulary. Use `SM_Chr_Business_Female_03` for Aling Rosa, as shown under `TaxVentureWithCompliance > Characters`. Reuse `Building_4 (1)` from `TownEnvironment > Env > Town` as her store near the plaza. See WORLD_AND_FEEDBACK for exact prefab paths; other characters may reuse existing placeholders.
- Create a fresh Office_Level2 scene through a one-time copy of current saved Office_Level1; reuse asset references, then replace level-specific behavior. Preserve Office_Level2_OLD. Follow SCENE_REUSE rather than rebuilding the office or migrating the whole project.
- Start inside the office. Office access is available before the side quest.
- Partly completed work must be checked; a filled field is not proof of correctness. Findings uses three detached-paper fields (Entry to check, Clue, Reason), reasons sourced from two concise book leaves, and automatic link checking with quiet paper feedback and a handwritten supported note.
- At the desk, tapping the folder icon centres and opens its paper with the existing entrance animation; dragging still places it where dropped.
- Initial guided Not Ready assessment; later independent reassessment of the same case.
- Side quest has objective right/wrong answers and Bronze/Silver/Gold. Its topic is income recognition and treatment, not summary reconciliation.
- Side quest gives classification feedback and authentic-looking records; it does not identify the office omission, resolve the mismatch, choose the final stamp, or fill the case.
- New physical tools and props are built in Unity as 3D objects. Paper text and notes may use the existing world-space document presentation. Existing HUD/result screens remain reusable.
- A justified Not Ready recommendation is a valid interim result. It never completes an unresolved case.
- Mistakes retain documents and work. Review feedback creates targeted correction tasks.
- The Auditor remains present; the review window closes. A next-morning transition preserves work.
- AI implementers do code, scene wiring, procedural models, animation, content and internal tests. The user does Unity Play Mode/device testing and ALL git commands.

- Polish pass (2026-10-20, user direction): Supporting Documents = Findings + four records (D1 and D4 back the two listed summary lines, D3 is the missing paid job, D5 explains the borrowed deposit). The Reviewed Schedule has the two listed lines plus one Missing entry and a conclusion. Its values come only from the highlighted lines of the four Side Quest 2 annotated papers (the same AnnotationPaper model). A Tax Code Book icon (below Case Folder, above Annotated Papers) opens the real book, whose first three Level 2 leaves are cheat sheets.

## Completion definition

Level 2 is fully wired and internally verified through the same presentation quality as current Level 1, including guidance, rewards, feedback, retry, saving, finale and level-select integration. Internal verification and user visual verification are separate statuses. No ticket may claim Unity appearance, timing, navigation or device performance was tested without the user's result.
