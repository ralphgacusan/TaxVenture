# T10 — Add deadline pressure while the Auditor stays

Status: Implemented — internally verified; user Play Mode pending (2026-10-19). Blocked by: T09.

## Visible accomplishment

The opening conversation reveals the review cutoff; the clock warns the player, and missing the window rolls safely to the next morning without losing their case.

## Work

Extend existing OfficeHours/OfficeHoursMath/chip with Level2 policy selected by configuration, preserving Level1 departure behavior. Default 12 active real minutes from opening announcement; pause compulsory guide lessons, conversations, blocking results/menu. Independent reading and solving consume time. Warnings at two minutes and 30 seconds use existing chip/toast patterns.

Keep Auditor present and interactive for schedule reminders. At cutoff close review submissions, let in-flight gesture resolve exactly once, checkpoint stable work and fade to next morning at consultant desk. Avoid teleport while stamp/drag/hand-in owns an action. Resume phase/objective/inventory/current graded state; fresh 12 active minutes. Unsupported submission at the exact cutoff must produce one ordered result, not both day rollover and approval.

First miss adds 3 EXP loss and at most the single case-heart deduction within existing Level2 caps; later misses add none. Disable duplicate generic Level1 late charges for Level2. On-time recognition only after accepted final packet before first cutoff, not preliminary stamping or supported hold. No time reward farming.

## Acceptance

Internal status (source/data checks only; each box also needs the user's Play Mode check):

- [x] Correct cutoff announced at opening, warnings and chip reflect active allowance. *(starts at the receptionist; warnings at 2:00 and 0:30; top chip "Review cutoff m:ss")*
- [x] Mandatory teaching/modal pause and independent-solving clock behavior differ as specified.
- [x] Auditor never disappears in Level2; Level1 behavior preserved. *(Level 1 OfficeHours untouched and Level-1-only)*
- [x] Safe rollover keeps all work and returns to stable desk state. *(waits for stamps/conversations, fades, stands the player at the consultant desk)*
- [x] Only one capped late consequence; exact-cutoff races cannot duplicate outcomes.

## Implementation (2026-10-19)

Deviation, recorded: instead of extending Level 1's `OfficeHours` (its Auditor-leaves flow and game-clock face), Level 2 uses its own small `Level2ReviewClock` part plus the pure `ReviewClock` model and its own chip; `OfficeHours` stays unchanged and Level-1-only. Settings live in the case data (`review.clock`: 720 active seconds, warnings 120/30, lateExp 3, lateHearts 1, lines). The clock pauses for blocking guide pages, the toolkit demonstration, conversations, reward cards, settings and pause. At zero hand-ins close; the rollover waits for stamps/conversations, fades to "Next morning", leaves any desk view, stands the player at the consultant desk and gives a fresh allowance. The first miss adds the late loss to the case award inside the caps; later days add nothing. On-time recognition only for an approval before the first cutoff. Next: T11.

## Verification/handoff

Deterministic clock/pause/rollover tests, capped reward tests and action-boundary tests. Compile and progression/guide regression. User later tests cutoff during dialogue, book reading, drag, stamp and audit. Record actual timings and pending feel checks. Next T11.


### Consistency follow-up (2026-10-19)

Implemented — internally verified; user Play Mode pending. See SPEC and PROGRESS, “Level 1 consistency follow-up”, for the current six-page folder, shared clock/splash, receipt papers, annotated HUD collection and direct-Editor checkpoint policy. Earlier implementation descriptions above remain historical.
