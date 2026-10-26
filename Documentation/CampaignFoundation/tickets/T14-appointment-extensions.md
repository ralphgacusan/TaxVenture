# T14 — Buy and use appointment extensions safely

Status: Planned. Blocked by: T05, T11, T12.

## Goal

Make review extensions a bounded, save-safe use of the existing case clock.

## Visible accomplishment

Before: there is no saved campaign voucher with a case-specific clock effect. After: the player can buy an optional appointment voucher for 30 coins, carry no more than two, and choose to apply one to an eligible active review before its cutoff. A confirmation shows the new review cutoff and the voucher's +180 active review seconds. The paper visibly leaves the bag only after the saved extension is committed. The voucher never changes statutory tax dates, a missed cutoff, or next-day recovery.

## Work

Use the T11 voucher and sell additional ones at T12's shared counter. The folded green ticket has a copper clock seal, “+180 REVIEW SECONDS”, “ONE PER CASE” and a 0/2 carry tab. Add a real two-drawer oak supply unit beside the office counter; label each drawer, use brass pulls, and show stored quantity. Tapping it opens the physical drawer and lists stored vouchers. If carry is below two, Retrieve moves one into carry; full carry gets a clear refusal with no quantity change. Thus overflow from T11 or purchases remains usable. Apply a carried voucher from the bag; retrieve drawer stock first. A desk action may call the same command. Read the existing review clock and change only the appointment window, never statutory date or assessment. Do not add another clock.

Revision 2026-10-25 (implementation notes, see [PROGRESS](../PROGRESS.md) and KNOWN_ISSUES CF-84 to CF-86): the oak supply unit stands beside the player's desk, where the existing Desk Drawer storage lives; vouchers apply to Level 1's Office Hours clock (Level 2 follows with T18); the voucher card, opened from the bar or the Bag, is the one way to apply (no separate desk action). "+180 active review seconds" moves the cutoff at the clock's own rate (30 game minutes at 10x).

### Source starting points

- **Existing:** the active signed save through `PlayerProgressData` / `PlayerProgressManager`; Level 1's office-hours rules and Level 2's `Level2ReviewClock`, `ReviewClockState` and existing checkpoint path; T12's catalogue interaction.
- **Proposed:** campaign supply quantities, active case ID and one consume/extend command that persists quantity, scoped case-use claim and new checkpoint clock together. Do not create a second clock singleton or treat a UI countdown as authoritative.

The drawer face slides out about 10 cm in 0.25 s; a voucher lifts 5 cm, rotates face-up and settles into the carried sleeve in another 0.25 s after commit. To apply one, the ticket lifts from its sleeve, a copper clock hand turns exactly half a turn, and the ticket slides into a datebook pocket while the clock card receives the new time; total application choreography about 0.7 s. Confirmation states price, carry/drawer destination, “+180 active review seconds” and the resulting in-game cutoff in large, plain text before any purchase commit. Play one quiet paper sound after retrieval, one drawer slide, and one subtle clock tick only on successful apply; never tick each frame or play success sounds before persistence. Route through the saved SFX preference/effects volume; effects OFF remains silent.

## Persistence and failure rules

Purchase shows its destination before confirmation: carry while fewer than two vouchers are carried, or the office drawer when carry is full. Confirm atomically debits 30 coins and increments the chosen quantity; cancel changes nothing. The drawer has no carry-cap loss path, so overflow is stored rather than discarded. Retrieval requires story mode and carry below two; it atomically decrements drawer and increments carry. Full carry, invalid context or failed persistence leaves both counts unchanged. Applying requires a carried voucher, active eligible case, not approved/completed, cutoff not passed/pending, and no prior voucher claim for that case. One transaction decrements carry, records the stable per-case claim, updates the existing clock/checkpoint and commits once. On any write failure, wallet, both quantities and clock remain at their previous values, no success cue plays and retry is safe. Restore, duplicate taps, new day or replay never reapplies it. Max one voucher per case, +180 active review seconds, carry cap two. Practice/mastery/Editor runs use temporary supplies. Missed cases are not rescued retroactively; no trust, extra coins or statutory tax benefit is attached.

## Internal checks — AI source/build/regressions only

- [ ] Rule cases cover carry cap, carry-to-drawer overflow, drawer retrieval, full-carry refusal, cutoff boundaries, approved/completed case, duplicate case-use, replay and non-story isolation.
- [ ] Persistence tests prove purchase destination, drawer retrieval and clock use are atomic, including simulated failure and resume at the changed cutoff.
- [ ] Static inspection verifies voucher model, physical drawer, sleeve, labels, destination prompt, reachable apply target and clock adapter; compile plus affected save/clock suites pass.

## USER manual acceptance — Unity/device/visual/feel/listen checks

- [ ] Fill carry, acquire or buy an overflow voucher into the drawer, free one carry slot, retrieve it, reload and confirm both counts; with full carry, confirm retrieval changes nothing.
- [ ] Use one before cutoff, reload and verify the displayed cutoff; try after cutoff and on a completed case and confirm no item/time change.
- [ ] On device, inspect legibility, confirmation clarity, ticket motion, clock hand, active countdown, tap forgiveness and sound/effects setting. The user only verifies the generated scene in Unity.

## Handoff

Next ticket: [T15](T15-quax-identity-and-demonstrations.md) establishes the guide identity and sample demonstrations.

## Non-goals and references

No extension after cutoff, statutory deadline change, arbitrary timer pause, additional time items, paid correct answers, grading bypass or required purchase. See [SPEC](../SPEC.md), [WORLD_AND_FEEDBACK](../WORLD_AND_FEEDBACK.md), [VERIFICATION](../VERIFICATION.md), and [PROGRESS](../PROGRESS.md).

Revision 2026-10-25 (user request): the Desk Drawer unit was removed. A third voucher is refused at the pickup and the counter ("You can't carry more of these", no coins spent) instead of going to the drawer; nothing is retrieved from a drawer any more.
