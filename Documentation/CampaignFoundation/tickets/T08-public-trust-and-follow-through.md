# T08 — Earn Public Trust through completed service and show its reasons

Status: Planned. Blocked by: [T07 — corrected progression and results](T07-corrected-progression-and-results.md).

References: [Campaign spec](../SPEC.md), [world and feedback](../WORLD_AND_FEEDBACK.md), [verification plan](../VERIFICATION.md), [progress log](../PROGRESS.md).

## Goal and visible result

Give the player a separate, understandable public-trust measure for community confidence, earned through authored service and follow-through. Before this ticket, legacy score hearts represent accuracy and there is no social-trust domain. After it, a new campaign begins at trust 40/100; the HUD's hearts exclusively show that public trust. Completing corrected Level 1 field assignment adds +2, and completing Marie's required follow-through adds +4, once each, for 46 total. The future support target of 65 is shown only as a future goal, never as an ending already earned.

After the corrected case is approved, Marie sees the work and Elijah completes the promised next step before Quax's shop introduction. If the player leaves during follow-through, Continue restores its pending state. The trust increase and its completion fact are saved together; returning, repeating the dialogue, replaying the chapter, or reopening the result cannot award them again. T16 owns final chapter completion and the optional counter visit-or-skip decision.

## Ownership and mechanics

Use the v5 campaign record and command boundary introduced in T02/T07. The inspected current path includes TaxpayerNpcInteractable / TutorialNpcController for interaction endings, the existing Level 1 finale/result path, and PlayerProgressManager.ProgressChanged for observers. Gameplay milestones own the trust command; the HUD, guide, career paper, dialogue presentation and animation only read committed snapshots. Do not infer trust from wrong-drop counts, field type errors, late clock, audit retries, score hearts, or tutorial page advancement.

Implement bounded trust (0–100) with deterministic normalization and authored stable event IDs. A trust action validates story mode, event ID, precondition, and whether the event was already claimed. Apply the +2 corrected-assignment and +4 follow-through facts to a candidate state, persist related campaign/checkpoint changes, then publish the snapshot and presentation facts. Clamp safely, reject invalid IDs/arithmetic, and return no-change on duplicate action. On write failure, restore the old in-memory state, emit no success line/hearts/effect, preserve the checkpoint, and offer retry. Practice, replay, and temporary direct-Editor runs never affect live trust. New Game starts at 40; existing progress never supplies a trust value or history.

Show five familiar heart icons or the existing trust row filled proportionally, with a numeric PUBLIC TRUST 46/100 label and a separate muted Community support target: 65. Pair fill with a number/label for color independence. On a committed change, ease the bar to the saved value, settle the heart marker, and show a concise reason tag naming the service or person; do not animate an estimated value before persistence. Keep the update subtle, with no flashing warning or character shame response. Reuse the existing UI select/confirmation SFX, or use no new SFX if the asset is not appropriate. Trust remains presentation-only until T19 authors a knowing choice in Level 2.

## Non-goals

Do not add intentional misconduct, trust loss, ending eligibility, a new heart bar for mastery, auto-penalties for ordinary mistakes or missed deadlines, trust from optional conversation, or trust inferred from old progress. Do not unlock new story content by reaching 65.

## Acceptance

Internal checks — implementation agent records evidence in ../PROGRESS.md:

- [ ] New campaign initializes trust to 40; old progress creates no inferred value or history.
- [ ] The two Level 1 events commit +2 and +4 once each and produce 46/100 after Marie follow-through.
- [ ] Duplicate callbacks, result reload, and replay yield no second trust change; write failure publishes no trust success.
- [ ] Wrong drops, field errors, late clock, and tutorial/guide display do not change trust.
- [ ] Practice, mastery replay, and direct-Editor modes are isolated.
- [ ] Every UI heart in the new campaign displays public trust only; score hearts remain accuracy/mastery data.

USER manual acceptance — only the user performs these checks:

- [ ] Observe trust before and after corrected field completion and Marie's follow-through.
- [ ] Repeat the dialogue/result and confirm trust does not rise again.
- [ ] Trigger an ordinary mistake and a late review; confirm trust is unchanged.
- [ ] Confirm the numeric value and future target remain readable without relying on color or animation.

## Handoff

Record exact action IDs, event owners, and trust display in ../PROGRESS.md. T09 adds the separate wallet and fixed completion claims. T19 later owns the first authored Level 2 ethics consequence; this ticket adds no such choice.
