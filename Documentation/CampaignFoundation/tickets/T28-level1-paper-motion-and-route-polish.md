# T28 — Polish the first-morning paper interactions and transitions

Status: Planned -> Implemented, internally verified; user manual pending (2026-10-28; evidence in PROGRESS.md "T28"). Blocked by: T26.

## Scheduling amendment — 2026-10-28

Polish only existing Level 1 papers and interactions; no additional document types or isolated all-eight preview belongs to this pass. T26 permits this work. Next T29, then T17; after that T18 owns Rosa's required Types 4–7. The latest user revision keeps T27 as a fixed-design roadmap without an advance library build. User manual acceptance remains separate.

## Goal and visible accomplishment

The document moments and existing Level 1 route feel like one physical adventure: clear handovers, satisfying paper motion, quiet readable pauses, deliberate board placement, and a smooth return to the client. This is a bounded integration pass across the touched Level 1 interactions, not authorization to redesign the entire game or replace its established rewards and finale.

## Work and player-visible behavior

Follow the complete current route: title/intro, movement/Notes/shop, click Arthur at his configurable start marker, floating teal Reviewed File, collect/open its two pages, inspect/return its 3D papers, all four client profiles freely ordered, Arthur follow-up, board, reception, Marie's editable two-page case, Auditor, client copy, current Chapter 1 card and Career return. Verify one logical Arthur and safe start-to-board positioning; no fifth client or Jhun-specific bench sleeve remains. Inspect actual latest source and PROGRESS: later user refinements supersede old descriptions of an extra wrap-up sheet. Do not resurrect that removed sheet, the check lever, Quax, or a practice board.

Unify handover, lift/turn, compare, put-down, attachment return, folder storage and page-change motion against the standard. Teal cover, REVIEWED tab, ghost, Bag icon and previews share one identity; Marie stays manila. Verify visible card bevel/thickness, paper curvature/clip, contact shadows and printed face depth. Two-second hold grouping remains optional; taps/drags respond immediately and groups return to their owning page. Use subtle edge emphasis only while available/focused. Keep the readable face stable; let the selected paper lift above neighbors with a restrained contact shadow. Direct dragging follows the pointer without spring lag; gentle settling happens after release. ID cards feel slightly rigid, full sheets have only minimal bend. Do not create an idle animation, required camera shake, confetti burst, or looping paper sound.

Use configurable starting targets: handover 0.4–0.6 s, inspect 0.25–0.4 s, return/page change 0.2–0.3 s, grouped storage 0.35–0.5 s. Reuse existing easing/audio settings. Map one supplied paper/card cue per deliberate action, never every frame or every pointer move. Preserve current neutral pre-check board choreography and actual post-commit reward cues. No new celebration interrupts the two short document moments.

Check arrival and departure framing, safe-area close targets, near-surface placement, handover reachability and the visibility of book/folder fields beside papers. Resolve actual surface bounds rather than guessing coordinates or shifting existing NPCs/rooms. Record all anchors. A missing final art/audio asset must be named explicitly; functional placeholder art cannot be called final polish.

## Input, persistence and timing

Reuse the shared camera/input locks, dialogue admission, paper focus and existing pause rules. First-presented records wait for the camera to settle; closing cannot tap through into the world. Settings, Android back, leaving a workstation, scene fades, interrupted drags, repeated taps, next morning, and reentry must leave one valid input owner. Restore shows papers already settled, not an unsolicited repeat handover.

Do not persist transient transforms or finish a gameplay commit only from an animation callback. A failed save cannot show a successful new acquisition. Record ownership, profile completion and case preparation stay with their existing domain/checkpoint boundaries. No added per-paper coins, trust, XP, score penalties or mandatory purchase. Preserve the current deadline trigger and existing compulsory/independent reading distinction.

Keep the combined two new sequences within the approximate 2–3 minute target through replaced dialogue and fewer interruptions, not faster text or forced auto-close. No extra tasks for Marie/Milo/Zorbix in the plaza. Any wider visual defects discovered outside touched interactions go into KNOWN_ISSUES with a proposed future scope rather than becoming an unbounded rewrite.

## Internal checks — AI source/build/regressions only

- [ ] Cancellation, repeated input, focus/lock restoration and duplicate instance cleanup have targeted coverage.
- [ ] Static asset anchors, type bindings and audio-setting routes are validated; source checks show no frame-based sound or animation-owned rewards.
- [ ] Affected build/guide/control/campaign checks pass; timing declarations are reported as targets, never rendered-motion evidence.

## USER manual acceptance

- [ ] Play the full route on the target phone: readable faces, fitting comparisons, reachable props, no clipping/tap-through or abrupt camera/paper jumps.
- [ ] Inspect SFX on/off, repeated taps, interruptions, and a resumed run; motion and sound remain consistent.
- [ ] Report actual pacing of both document moments; the board and case still feel like the main jobs.

## Non-goals and handoff

No whole-game art replacement, new rendering pipeline, NPC locomotion, legal content revision, economy rebalance, or Level 2 rollout. Record internal results and exact user-only polish checks in PROGRESS. Next: [T29](T29-record-route-verification.md), then [T17](T17-delivery-a-verification.md). T18 follows T17 and builds only its needed types; T27 is a roadmap, not a gate.
