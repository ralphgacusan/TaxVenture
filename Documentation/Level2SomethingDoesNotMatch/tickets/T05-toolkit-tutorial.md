# T05 — Open the toolkit and teach one hands-on example

Status: Implemented — internally verified; user Play Mode pending (2026-10-19). Blocked by: T04.

## Visible accomplishment

At the market desk, a 3D toolkit opens, the guide demonstrates one salary record, and the player highlights a line and places classification/treatment/evidence notes themselves.

## Work

Reuse workstation camera/controls, document lift, book reference and tap/drag semantics. Create a real box/lid, marker, note cards and evidence clip with Unity meshes/primitives. Implement motions in WORLD_AND_FEEDBACK: lid opening, marker lift/trace, note snap and clip settle. No flat full-screen minigame replacement or new input framework.

T0 is an unrelated salary example. Demonstrate source observation, compensation type, treatment and supporting payslip, then wait for player actions. Type and treatment are separate slots; source highlight is unscored. Labels/icons accompany color. Compatible drop feedback never asserts a tax answer is correct. No fines, timer pressure or badge score during tutorial.

Implement a single-entry annotation view/controller reused by T06, not separate tutorial-only mechanics. Book opens through current system. Exit/back and reentry restore state and release movement/look/modal ownership cleanly. Pause compulsory teaching through a clear pause predicate; do not set Time.timeScale to zero to pause the guide.

## Acceptance

Internal status (source/data checks only; each box also needs the user's Play Mode check):

- [x] Toolkit/tools/paper are 3D, animate and stay on desk correctly. *(primitives: case + hinged lid, marker, cards, record tags, two-piece clip, clipboard paper; lid ~100 degrees / 0.45 s, trace ~0.2 s, settle 0.12 s; placement on the desk is a user check)*
- [x] Player performs highlight, type, treatment and evidence actions with large touch targets. *(tap/tap default, drag optional; card targets ~0.15 x 0.09 m)*
- [x] Tutorial has immediate rule explanations and cannot affect scored accuracy. *(every card has a rule line; practice never touches packetRevision, scores or rewards)*
- [x] Same entry interaction can render a data-defined challenge entry. *(AnnotationPaper renders any AnnotationEntryDefinition; AnnotationActions are shared; `annotation.entries` is ready for T06)*
- [x] Exit/book/dialogue/pause do not leave stuck controls or duplicate objects. *(leaving the desk closes the case and cancels a held card; teaching waits on TeachingPaused; no Time.timeScale; built once per desk)*

## Implementation (2026-10-19)

Sitting at Rosa's records desk after the handover opens the **Income Toolkit**. The first time, Arthur demonstrates once on an unrelated payslip (T0, the ₱8,000 April salary of a fictional employee): the marker traces the salary line, Compensation income goes on TYPE, Regular income schedule on TREATMENT, and the clip bites Payslip PS-0430. Everything then goes back and the player repeats it. Each action gets an immediate rule explanation; a fitting place is tinted green only as "fits here". Matching the demonstration moves the review to Challenge and raises `AnnotationTutorialCompleted`. See PROGRESS for files and checks. T06 adds Rosa's E1–E4 to `review.annotation.entries` with their separate answer keys.

## Verification/handoff

Compile; test annotation data/actions and tutorial transition; controls/dialogue/guide regression as applicable. Validate model roots/colliders/materials and cleanup statically. User later tests phone readability, input feel, animation and desk framing. Next T06.


## Feel and visual pass (2026-10-19, with T06)

User feedback: highlighting didn't feel like highlighting (a yellow band showed before inking), cards looked off, few animations. Rebuilt: swipe a line (or pick up the highlighter) and the ink appears only where the nib passes, as uneven hand-drawn segments with a wet-ink dot; a forgiving sweep finishes with a sparkle burst and a SOURCE flag; a tap still draws it for you. Cards have a coloured border and header band with their word, a soft shadow that grows as they lift, a glow when picked up, a deal-out flip from the case, squash-and-spring landing with sparkles and a "Placed!" pop, a shake-back on a wrong-kind place, idle breathing and an idle nudge toward the next step. Note places have neutral dashed outlines that march and light up only while a fitting card is held. Fixed: the lid used to rotate down into the case; it now opens up and back with its scoreboard facing the seat.
