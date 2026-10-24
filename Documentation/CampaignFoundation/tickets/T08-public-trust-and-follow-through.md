# T08 — Earn Public Trust through completed service and show its reasons

Status: Planned. Blocked by: [T07 — corrected progression and results](T07-corrected-progression-and-results.md), [T22 — NPC emotion emotes](T22-npc-emotion-emotes.md).

Revision 2026-10-24: the first implementation (trust, claims and a paper follow-through card) is recorded in ../PROGRESS.md. This revision replaces the paper card with the Auditor's Client Copy and an in-world handover to Marie. Trust rules, claims and acceptance intent are unchanged. Build order: T22, then this revision, then [T23](T23-marie-consultation-visits.md).

References: [Campaign spec](../SPEC.md), [world and feedback](../WORLD_AND_FEEDBACK.md), [verification plan](../VERIFICATION.md), [progress log](../PROGRESS.md).

## Goal and visible result

Give the player a separate, understandable public-trust measure for community confidence, earned through authored service and follow-through. Before this ticket, legacy score hearts represent accuracy and there is no social-trust domain. After it, a new campaign begins at trust 40/100; the HUD's hearts exclusively show that public trust. Completing corrected Level 1 field assignment adds +2, and completing Marie's required follow-through adds +4, once each, for 46 total. The future support target of 65 is shown only as a future goal, never as an ending already earned.

Following through means walking back to Marie and handing her the approved copy yourself. After the corrected case is approved, the Auditor hands Elijah a read-only Client Copy; Elijah carries it in the case-folder icon to the conference room, and drags it onto Marie during a short conversation. The trust increase and its completion fact are saved together; returning, repeating the conversation, replaying the chapter or reopening the result cannot award them again. If the player leaves at any point, PLAY resumes at the right step. T16 owns final chapter completion and the optional counter visit-or-skip decision.

## Ownership and mechanics

Use the v5 campaign record and command boundary introduced in T02/T07. Gameplay milestones own the trust command; the HUD, guide, career paper, dialogue presentation, emotes and animation only read committed snapshots. Do not infer trust from wrong-drop counts, field type errors, late clock, audit retries, score hearts, consultation visits (T23) or tutorial page advancement.

Implement bounded trust (0–100) with deterministic normalization and authored stable event IDs. A trust action validates story mode, event ID, precondition, and whether the event was already claimed. Apply the +2 corrected-assignment and +4 follow-through facts to a candidate state, persist related campaign/checkpoint changes, then publish the snapshot and presentation facts. Clamp safely, reject invalid IDs/arithmetic, and return no-change on duplicate action. On write failure, restore the old in-memory state, emit no success line/hearts/effect, preserve the checkpoint, and offer retry. Practice, replay, and temporary direct-Editor runs never affect live trust. New Game starts at 40; existing progress never supplies a trust value or history.

Show five familiar heart icons or the existing trust row filled proportionally, with a numeric PUBLIC TRUST 46/100 label and a separate muted Community support target: 65. Pair fill with a number/label for color independence. On a committed change, ease the bar to the saved value, settle the heart marker, and show a concise reason tag naming the service or person; do not animate an estimated value before persistence. Keep the update subtle, with no flashing warning or character shame response. Trust remains presentation-only until T19 authors a knowing choice in Level 2.

## Client Copy and handover (replaces the follow-through card)

Flow:

1. Marie's case is approved and the Audit Summary appears. The case result is still committed quietly at approval (keep PlayerProgressManager.CommitLevelOneCaseResult, its claims and the case's 100 coins). The Case Approved / Level Complete card does NOT appear yet.
2. After the Audit Summary's Continue, a floating Client Copy folder appears beside the Auditor, reusing the pop-up and hover animation of a plaza NPC's dropped profile (TaxpayerProfileWorldPickup). The Auditor shows the T22 alert emote and says, for example, "Approved. This is Marie's client copy. Bring it to her and walk her through it."
3. Pickup: tapping the floating folder flies it into the case-folder HUD icon with the existing folder-to-icon flight (FolderToHUDIcon / HudIconButton); the icon switches from disabled to enabled with count 1.
4. The Client Copy uses the same case-folder mechanics and contents (open, read, drag: CaseFolder3DView, HudDragItem ghost) but is READ-ONLY: every field locked, no edit, transfer, assessment, stamp or print action, and the Auditor tray does not accept it. A "CLIENT COPY" stamp shows on the folder cover, the front page and the drag ghost, plus a small tag on the HUD icon. Reuse the existing folder assets with an overlay stamp; do not duplicate the models. DeskFolderStackSync and AuditorSubmissionTray must not treat the copy as the working case folder.
5. Objective "Return Marie's client copy (Conference Room)" through OfficeObjectives, with a guide target if one fits. Marie (in the conference room after T23, or moved to the CaseFinale client spot at this point if T23 is not built yet) shows her available NpcStatusMarker and the alert emote.
6. Handover conversation (tap Marie): Elijah says he has her approved return. At the handover line the dialogue PAUSES in a new generic "awaiting action" step with the prompt "Drag the client copy to Marie". Tapping the dialogue box does not advance; only the case-folder icon is active. Dragging the folder onto Marie completes the step; TAPPING the folder icon also completes it (accessibility, small screens). A drop anywhere else returns the folder to the icon. Implement the awaiting step generically in DialogueSession / DialogueUI (a line flag plus a completion call) so other conversations can reuse it; DialogueAdvanceGate behaviour stays intact for ordinary lines.
7. On the drop the icon empties and disables, and CommitFollowThrough runs (trust +4 claimed once, the activity, and the existing temporary chapter-completion bridge until T16, CF-41). Only after a successful write: a T22 heart burst over Marie, the trust toast with its reason, and the HUD trust bar easing 42 -> 46. On a failed write: no burst, sound or gain, a retry prompt, and the copy stays in the icon. The conversation then continues by itself as Elijah explains the result and next steps. Marie's closing lines and emote follow CaseFinaleRules tiers with clientFinale great/good/okay (sparkles for first try and on time; a music note for relieved but thankful).
8. End: after her last line the existing finale and case-result card flow runs, then the career. Remove FollowThroughView, FollowThroughScript and the MenuEntry PLAY resume of that card once the handover replaces them. Keep CampaignRewards, its claims and its ledger.

Saving and resume: extend the Level 1 checkpoint (story runs only, quiet restore, no replayed events). Today a recorded case result makes UsesLevelOneCheckpoint false and CommitFollowThrough ends the field checkpoint; keep the checkpoint writable until the follow-through commits. Add an explicit post-approval phase (copy at the Auditor / copy carried / follow-through done) and validate it in Level1FieldCheckpointRules: after a recorded case result the case itself never restores to "case received".

| You quit… | You come back to… |
|---|---|
| Before picking up the copy | The copy floating beside the Auditor (case result already saved) |
| With the copy in your icon | Copy in the icon, objective active, Marie waiting |
| During the conversation, before the drop | Marie waiting; the conversation restarts from its beginning |
| After the drop | Trust saved once (never doubled); the end-of-level flow |

PLAY must resume into Office_Level1 at the matching state; this replaces the T08 menu resume of the card.

## Non-goals

Do not add intentional misconduct, trust loss, ending eligibility, a new heart bar for mastery, auto-penalties for ordinary mistakes or missed deadlines, trust from optional conversation, or trust inferred from old progress. Do not unlock new story content by reaching 65. The Client Copy is never editable, never re-submittable and never a second case. No walking animation for Marie, no new folder models, no change to the case's coins or mastery result, and no chapter wrap-up (T16).

## Acceptance

Internal checks — implementation agent records evidence in ../PROGRESS.md:

- [ ] New campaign initializes trust to 40; old progress creates no inferred value or history.
- [ ] The two Level 1 events commit +2 and +4 once each and produce 46/100 after Marie's handover.
- [ ] Duplicate callbacks, repeated drops, result reload, resume and replay yield no second trust change; write failure publishes no trust success, burst or sound.
- [ ] Wrong drops, field errors, late clock, and tutorial/guide display do not change trust.
- [ ] Practice, mastery replay, and direct-Editor modes are isolated.
- [ ] Every UI heart in the new campaign displays public trust only; score hearts remain accuracy/mastery data.
- [ ] Pure tests: post-approval phase transitions and their validation; a recorded case result never restores to "case received".
- [ ] Source contracts: the awaiting step blocks dialogue advance, the folder-icon tap fallback exists, the stamp appears on the drag ghost, and the Client Copy is read-only.
- [ ] FollowThroughView/Script and the MenuEntry card resume are removed; scratch build and all 12 suites pass.

USER manual acceptance — only the user performs these checks:

- [ ] Observe trust before and after corrected field completion and the handover (42 -> 46).
- [ ] The floating copy beside the Auditor, its pickup flight and the icon going from disabled to 1.
- [ ] The CLIENT COPY stamp on the cover, front page and ghost; no field can be edited.
- [ ] The objective and Marie's marker; the pause-and-drag step, the tap fallback, and a wrong drop returning the folder.
- [ ] The heart burst, closing lines by tier, the end card and the career.
- [ ] Quit at each of the four resume points and confirm the matching state; trust never rises twice.
- [ ] Trigger an ordinary mistake and a late review; confirm trust is unchanged and readable without colour or animation.

## Handoff

Record exact action IDs, event owners, checkpoint phase names, the awaiting-step API and trust display in ../PROGRESS.md. T09 adds the separate wallet and fixed completion claims. T23 adds Marie's consultation visits and owns her relocation. Chapter 1's ending is now staged by this handover; T16 still owns final chapter completion and the wrap-up. T19 later owns the first authored Level 2 ethics consequence; this ticket adds no such choice.
