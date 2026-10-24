# T23 — Visit Marie in the conference room during her case

Status: Planned. Blocked by: [T08 — Client Copy and handover revision](T08-public-trust-and-follow-through.md), [T22 — NPC emotion emotes](T22-npc-emotion-emotes.md).

Amendment ticket added 2026-10-24; build order T22, then the T08 Client Copy revision, then this ticket.

References: [Campaign spec](../SPEC.md), [world and feedback](../WORLD_AND_FEEDBACK.md), [verification plan](../VERIFICATION.md), [progress log](../PROGRESS.md), [known issues](../KNOWN_ISSUES.md).

## Goal and visible result

Marie becomes a client Elijah can visit during her case, with natural, non-repeating lines and no penalties. Before this ticket Marie stays in the plaza until her finale, and the only office-side lines are clientAfterFailedAudit and the finale. After it, once the side quest is completed, Marie waits in the conference room's client spot. Tapping her starts a short consultation: on the first visit she is helpful, later she gets friendly, then lightly teasing, then playful, and finally she warmly suggests checking her documents instead. When something about the case fits, she offers a clue (never the answer). After a failed audit or on a new Office Hours day she reacts to that first, and her mood ladder starts over.

## Ownership and mechanics

Relocation: after the side quest is completed, move Marie's plaza model (the clientFinale modelPath, Environment/Outside/Plaza/NPCS/NPC_Marie) to the conference room client spot (the CaseFinale spotPath, Environment/OfficeBuilding/Rooms/ConferenceRoom/Client). Move her only while she is off screen or during an existing fade, so there is no visible teleport; a walking animation comes later. Save the relocation in the Level 1 checkpoint so she restores in the conference room. Her plaza interactions, her profile handover (TaxpayerNpcInteractable / TaxpayerProfileWorldPickup) and the T08 handover must stay correct, and CaseFinale must still find her at the spot. If the T08 revision already moves her at Client Copy time, this ticket takes over that move and keeps one owner.

Visits: during the case, tapping Marie (NpcAvailabilityRules / NpcStatusMarker for availability) starts a short consultation through the shared dialogue flow (DialogueUI, DialogueSession). It is a different line set from her plaza dialogue. During the T08 handover objective, tapping her starts the handover instead.

Line selection uses priority conditions, not a branching tree, in a small pure selector (no Unity calls). Data lives in the case JSON in a new optional "consultation" block, so Level 2 clients can reuse it:

1. Case progress clue, if one fits: for example "Did you see my employment certificate?" while that document is unread, or field-specific hints for a still-empty or wrong field. Clues only, never answers; reuse the clue style of clientAfterFailedAudit. A clue condition names a document id or a field key from Level1CaseCheckpoint.FieldKeys, never an answer value.
2. Special moments: after a failed audit ("The auditor sent it back? Oh no…", worried emote); day 2 or later of Office Hours ("I was hoping to hear yesterday…").
3. Visit ladder:

| Visit | Mood | Example | Emote |
|---|---|---|---|
| 1st | Helpful | "Oh, hello again! Ask me anything." | music note |
| 2nd | Friendly | "Back already? Sure, what do you need?" | sparkles |
| 3rd | Light teasing | "You again! Is my file giving you trouble?" | sweat drop |
| 4th | Playful | "You'll know my life story better than I do!" | small anger puff |
| 5th and later | Warm redirect | "I think I've told you everything. Maybe my documents?" | "..." |

Variety: two or three variants per step, never repeating the previous line. The ladder resets after a failed audit or a new Office Hours day. The visit count, the last variant id and the reset markers (last seen failed-audit count and day) are saved in the checkpoint, so a resume continues naturally. Emotes use the T22 tags on the lines.

No consequences: visiting never changes trust, XP, score, coins, time, objectives or story. The Office Hours clock runs as normal while talking, as other conversations do, and nothing extra happens. Missing or malformed consultation data falls back to a short neutral line (or no consultation) with one warning, never an error.

## Non-goals

No branching dialogue tree, no choices, no rewards or penalties, no answers in clues, no new dialogue framework, no walking or path-finding, no new Marie model, no plaza dialogue changes. Level 2 clients only gain the data shape; authoring Rosa's consultation is out of scope.

## Acceptance

Internal checks — implementation agent records evidence in ../PROGRESS.md:

- [ ] Pure selector tests: priority order (clue, then special moment, then ladder), ladder thresholds 1-5+, no back-to-back repeat, resets after a failed audit and a new day.
- [ ] Clue tests: no clue text contains any answer-key value of the case; clue conditions reference only document ids and field keys.
- [ ] JSON loading of the consultation block and the fallback when it is missing or malformed.
- [ ] Checkpoint round trip of relocation, visit count, last variant and reset markers; restore raises no gameplay events.
- [ ] Source contracts: visiting calls no trust, coin, XP, objective or clock command; scratch build and all 12 suites pass.

USER manual acceptance — only the user performs these checks:

- [ ] Marie appears in the conference room after the side quest, with no visible pop.
- [ ] Five or more visits show the ladder, without an immediate repeat.
- [ ] Clues fit current progress and never give the answer.
- [ ] The mood resets after a failed audit and on a new day.
- [ ] Trust, coins and XP are unchanged by visiting; the handover and finale still work with her in the room.

## Handoff

Record the consultation JSON shape, selector rules, checkpoint fields and Marie's relocation owner in ../PROGRESS.md. T16 composes these visits into the finished Level 1 route; T18 may reuse the consultation block for Rosa.
