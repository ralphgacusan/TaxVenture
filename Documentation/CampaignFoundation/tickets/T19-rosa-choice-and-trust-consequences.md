# T19 — Record Rosa's explicit ethics choice and consequence

Status: Implemented, internally verified (2026-10-29); user manual acceptance pending.
Blocked by: T18.
T18 and T32 are implemented; their manual acceptance remains pending.

## Goal

Add one explicit, durable ethics choice under the [Level 2 continuation amendment](../LEVEL2_CONTINUATION.md). In the chosen execution order T32 precedes this ticket; reuse its Rosa animation owner rather than introducing a competing actor/pose controller.

## Visible accomplishment

Before: Rosa's fixed-order dialogue records no knowing pledge. After reviewing her records, the player makes one clear promise/refusal choice with Rosa and commits it in a physical case note. It is not inferred from a stamp or ordinary mistake. Responsible follow-through earns net +2 trust; unsupported reassurance later reversed and consequential nets −8; dismissive refusal nets −3. Required T18 service trust remains independent.

## Work

Insert one branch at the existing Rosa conversation seam after the player has enough context to understand the evidence concern and before the relevant promise is made. Show the source facts first, then present three plain-language choices: (1) “I cannot approve it yet. Let’s check the payment together”; (2) “I’ll call it ready now so you can move forward”; and (3) “Not ready. Bring me complete records next time.” Before confirming choice 2, state explicitly that Rosa's records leave one income amount unresolved and calling the case ready would be unsupported. Require a separate confirm action for that knowingly unsupported promise. Choice 3 is a dismissive hold, but leaves the review available: the player can gather missing records, correct required work and return for approval. No choice skips required correction, approval or follow-through. This must be a knowing choice, not an interpretation of a later stamp.

The responsible path records an authored −1 disappointment when Rosa hears that the player will not promise early, followed by +3 when the player completes supporting-record follow-through: net +2. The unsupported reassurance grants +3 on commitment, then the authored later reversal removes that +3 and applies one −8 consequence: net −8. The dismissive branch applies one −3 consequence and does not trigger the follow-through bonus. Baseline corrected field trust (+2) and corrected case/follow-through trust (+4) are independent Level 2 service claims; do not duplicate either while applying this choice. Ordinary wrong fields, draft mistakes, a late cutoff or a justified hold are not misconduct and never invoke an ethics branch.

### Source starting points

- **Existing:** the fixed-order Rosa dialogue and physical actor presentation in `Level2RosaStall`, `Level2OfficeStart`, `DialogueUI` / `DialogueSession`, and saved case/story state through the current checkpoint and signed progress owner.
- **Proposed:** a minimal three-option choice presenter/action record, stable commitment ID and once-only trust consequence command. This is a narrow insertion into the current Rosa interaction, not a new dialogue framework; do not assume such choice types already exist.

Rosa turns toward Elijah with a short authored response. Record the selected words and follow-up status on a neutral ruled note inside Elijah's existing case folder, never as a contract handed to Rosa. Later reversal updates it to “PROMISE REVISED — SUPPORT CHECKED”; responsible follow-through adds a “RECORDS CHECKED” tab. Use text and pencil marks, never moral icons. A dismissive hold closes the folder but preserves the open review and return objective. Motion: actor turn 0.3 s, note placement 0.45 s, response 0.4 s; skippable and never state-gating. Play one neutral paper cue and one restrained response cue after commit through effects-volume settings, with no frame repeats.

## Persistence and failure rules

Persist the choice, explicit commitment text/ID, trust mutations, consequence-applied IDs and the required follow-through state in the signed campaign record/checkpoint. Save each transition with its gameplay milestone before showing its consequence. If saving the choice fails, leave the player on the choice and do not present a committed card; if saving a later response fails, keep the previous committed choice and pending consequence state so retry applies it exactly once. Exiting/reloading never cancels a committed pledge or pays/penalizes it again. A player who selected unsupported reassurance cannot avoid reversal by restarting or submitting repeatedly. Level 2 replay and non-story modes cannot change campaign trust or choice history. Trust clamps to 0–100.

## Internal checks — AI source/build/regressions only

- [x] Rule tests assert the exact ethics deltas (+3 then −3 and −8; responsible −1 then +3; dismissive −3), net totals, trust caps and once-only IDs.
- [x] Resume/write-failure tests cover before/after choice, pending reassurance, reversal, required follow-through and repeated audit/submission; baseline service rewards remain exactly once.
- [x] Static content checks find explicit choice copy and physical case-note/status assets, and confirm no ordinary mistake/deadline handler routes to misconduct; relevant Level 2/trust/dialogue regressions pass.

## USER manual acceptance — Unity/device/visual/feel/listen checks

- [ ] Reach the choice after reviewing Rosa's records; verify each option is understandable before commitment and that a separate confirmation records it.
- [ ] Play all three paths, interrupt after commitment and after consequence, then resume; check the final trust difference and visible case note/revised status.
- [ ] Check actor response, card legibility, screen/touch targets, animation timing, neutral/serious tone and effect sound/volume in Unity/on device. Scene assembly remains AI-owned.

## Handoff

Next ticket: [T20](T20-comparison-stand-and-shop-unlock.md) adds the functional physical comparison stand.

## Non-goals and references

No general morality score, branching conversation framework, legal/tax rule change, inferred misconduct, morality gate, or new ending. See [SPEC](../SPEC.md), [WORLD_AND_FEEDBACK](../WORLD_AND_FEEDBACK.md), [VERIFICATION](../VERIFICATION.md), and [PROGRESS](../PROGRESS.md).

## Consistency amendment (2026-10-29)

Level 2 consistency amendment (user request, 2026-10-29; implemented, internally verified, user manual pending; see PROGRESS): Rosa's folder opens on the Level 1 structure (page 1 editable Taxpayer Information, page 2 Supporting Documents with Level 1's physical records: her Client Background notes from the start, D1/D5/D3/D4 after the handover; no Taxpayer Profile page), followed by her working pages. After the record handover the same Rosa actor is placed in the office conference room (phase-based, never in view, no walking yet) for Level 1-style consultations; her stall, records desk and required stall conversation are unchanged. The Auditor hand-in is Level 1's talk-first awaited-folder conversation (no tray audit). Approval continues into Level 1's Client Copy flow and Rosa's physical delivery in the conference room, which calls CommitLevelTwoFollowThrough once. Later tickets build on this; they must not restore the profile page, the tray hand-in, a second Rosa or a second follow-through path. For T19: the ordinary approved-case follow-through already works without the ethics choice (Rosa's Client Copy delivery). Add the choice at its valid seam and reuse that delivery; do not add another follow-through call or award path.

## Implementation evidence (2026-10-29)

The first complete CHECK asks the player to return to Rosa after all four records are marked; it does not grade until the explicit choice is recorded. Her normal interaction works at the stall, during her walk, at the front desk and in the room. A fact recap precedes the three exact options. The unsupported branch has separate warning and confirmation/back lines, distinct confirm controls and a short repeat-gesture guard. Talk later cancels without committing. Older checkpoints that already froze their first check continue without retroactive choice penalties.

The existing signed campaign contains the choice/commitment IDs and words, pending follow-through, actual promise grant, consequence IDs and separate trust ledger claims. Candidate-then-write preserves both campaign and checkpoint on failure. Approval atomically retains the approved review and Client Copy position; repeating a result after delivery cannot recreate a copy. The same Client Copy delivery resolves the pending consequence and independent service reward before feedback. Replays/practice/direct Editor choices stay local to the attempt.

The existing folder owns a neutral ruled note, pencil rules, words, next step, exact status and responsible checked tab. No internal ID is printed. Its 0.45 s placement and the 0.4 s restrained response cue are presentation only; dialogue can advance or be cancelled. Rosa retains her existing NpcLife/animation ownership. The dismissive response closes any open working folder and leaves review/records available.

Executed: scratch build 0 errors/27 baseline warnings; CampaignFoundationRegression 741/741 (31 T19 checks), Level2Regression 1485 PASS (including real snapshot/canonical-resume checks), DialogueRegression 1157 assertions, CampaignFoundationRuntimeRegression 16/16, Level1LivingPlazaRegression 77/77, AudioRegression 259, GuideRegression 905, TrainingRegression 505/505. VerifyLevel2Assets, VerifyLevel2Scene (2 inherited warnings), VerifyOfficeHoursConsequences, VerifyHudRewardPresentation, VerifyGuideTargets and documentation pack checks passed. See PROGRESS for paths, commands and limitations. No Unity/device/manual acceptance is claimed. Next: T20.
