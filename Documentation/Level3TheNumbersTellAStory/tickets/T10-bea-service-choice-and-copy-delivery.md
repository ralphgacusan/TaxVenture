# T10 — Explain Bea’s balance and deliver her existing computation copy

Status: **Implemented, internally verified; user manual pending**. Blocked by: T09. (5 November 2026; Unity/device acceptance is the user's.)

**5 November 2026 implementation:** Continues T09's approved, frozen result and its existing copy lifecycle (Auditor drop → saved pickup → read-only carried copy). At Bea's existing conference-room spot (no extra trip) her BeaDelivery talk asks one 15-word question, “Before I keep it: how much is left to pay after the tax already withheld?”, on an awaited line (dialogue taps never answer it). The approved copy's Tax due / Tax credit (withheld) / Remaining payable lines with their frozen figures are docked beside the dialogue (existing DockChoices card, paper rows, fixed 30/34 pt, Talk later). Tax due or Tax credit: the player's line, one contextual sentence, the same question waits; nothing saved, no Trust change. Remaining payable: `Level3Checkpointer.CommitBalanceExplanation` (candidate → signed save → publish) before her short reply, then her handover line waits on the existing `ClientCopyRules.HandoverAction`. The existing folder icon tap or a drag onto Bea (`ClientCopyHandover.ILevelDelivery`, an opt-in seam; Levels 1/2 unchanged) runs `CommitDelivery`: copy done, BeaDelivery → ClosingFinding with Bea still active, company not admitted, and the once-only +4 `bea-follow-through` Public Trust claim in one signed write; only then the icon empties, paper sound, heart emote, Trust toast/HUD peek and her one thank-you line. Any other drop returns the copy; failed writes keep the prior actionable step. T14 owns the closing finding/wrap-up; no chapter completion is shown or saved. Details, commands and manual script: latest PROGRESS; limitations KNOWN_ISSUES L3-R100–L3-R105.

Priority: P1. Phase: C — Client resolution. Estimated scope: M; one fresh agent context. If real call-site work exceeds this bound, record a bounded split before adding unrelated framework work.

## Goal and visible accomplishment

Pick up Bea’s read-only computation copy, answer one meaningful question about credit/balance by pointing to its figure/source, then hand it to her in one short resolution scene.

Revision: **3 November 2026**. Read [REVISION_PLAN](../REVISION_PLAN.md), [SPEC](../SPEC.md), [reuse map](../REUSE_AND_ARCHITECTURE.md), [visual contract](../WORLD_AND_FEEDBACK.md), [verification](../VERIFICATION.md) and [AGENTS](../AGENTS.md). Planned refers to changed revision acceptance, not absence of reusable implementation. Historical baseline: Current code ends at BeaDelivery with copyPhase atAuditor. Reuse this already-approved predecessor; pickup/delivery must not approve or pay again. Separate service-plan routes were proposed only and are now removed.

## Existing systems to reuse

Existing source paths relative to repository root; inspect live callbacks/content before changing them. Narrow Level_03 adapters only; no duplicate folder/stamp/calculator/input/save/shop framework.

- `Assets/Scripts/RewardsSystem/ClientCopyRules.cs`
- `Assets/Scripts/CaseFolder/ClientCopyPickup.cs`
- `Assets/Scripts/Level2/Level2FolderHandoff.cs`
- `Assets/Scripts/Level3/Level3Audit.cs`
- `Assets/Scripts/Level3/Level3BeaInteractable.cs`
- `Assets/Scripts/UI/DialogueUI.cs`
- `Assets/Scripts/UI/DialogueSession.cs`

## Work and interaction mechanics

1. Adapt existing ClientCopy pickup/reader/hotbar handover lifecycle to Bea’s case/source/action identities. Preserve atAuditor/carried/delivered phases and existing real folder/copy asset, not a new duplicate document type.
2. Bea receives the copy at the conference/client spot used by her current route; no extra outside trip/receipt-sorting/trace-totals errand. One question uses the copy’s credit/balance or certificate. Point/select relevant evidence, concise response, retry with help on misunderstanding.
3. Existing dialogue AwaitAction and icon-tap/drag-to-correct-NPC must finish actual handover. Ordinary dialogue taps cannot bypass it. Keep copy usable on talk-later/cancel/settings/reload, wrong-client drop and failed write.
4. Persist explanation outcome and completed handover with once-only+4 Trust; save before emote/toast. Delivery then advances to Bea's closing/wrap-up flow (T14) and does not reset her result. It does not admit Nina or complete the chapter until the explicit T14 acknowledgment. Legacy service-plan fields, if any, remain historical facts without a required extra task.

## Assets, motion and feedback

Read [OFFICE_CASE_PLAY](../OFFICE_CASE_PLAY.md), approved 4 November, for the concise physical office flow. One active stage/task, details on demand, existing folder left/calculator right/short tape between. One short Bea question (normally≤30 words), readable credit/balance portion of approved copy, point/select relevant figure/source, concise response, then actual handover. Example: “How much is left after the tax already withheld?” Never overlap guide narration/certificate/copy figures with dialogue or turn this into a full quiz/three-stage recap. Wrong explanation gives one contextual sentence and retry with no Trust loss; cancel preserves copy. Use current handover motion/emote, then one once-only service acknowledgment. No extra service-plan errand or repeated approval reward.

Implement this slice with actual existing 3D bodies, readable paper faces, reachable targets and current page/camera/audio ownership. One active task/primary action; temporary source/reference only on demand. Reuse L1/L2 styling and quiet lift/settle/feed/stamp cues. No tiny auto-shrunk text, full-tool overlay or correctness flash on draft placement. Bind models/colliders/callbacks here; T17 cannot substitute for this ticket’s visual implementation. User verifies rendered/touch/feel results.

## Integration requirements

Preserve shared L1/L2 behavior, existing serialized identities/source scenes and all campaign possessions. Keep current exact-decimal L3 rules/provenance; no float prototype bridge. Use existing input/dialogue/FSM/guide/Bag/reward owners. Career3 stays gated until T18, L4/L5 remain unavailable. No remote ticket, package, scene recopy or global cleanup.

## Persistence and failure rules

Candidate -> signed persist -> publish before success effects. Validate mode/case/source/revision/prerequisites/action IDs; duplicates do not award. Preserve legacy grades/claims/approved snapshots/copies and quiet restores. Failure keeps prior state/source/copy retryable. Clear only owned pointer/selection/locks on Back/settings/disable/focus/camera return; independent work can be left. Practice/replay/Editor cannot mutate live story state.

## Internal checks — AI source/build/regressions only

- [x] Active runtime strings/prompts and fallback errors follow OFFICE_CASE_PLAY: short task/action copy, optional detail, no debug/provenance dump or answer-key reveal; actual bindings inspected.


- [x] Approved atAuditor resumes into pickup, never recomputation/second100 coins or copy recreation.
- [x] Carried copy is read-only/case-scoped; correct explanation+actual correct-client delivery gates+4 once.
- [x] Wrong explanation/deferral/wrong client/cancel/write failure retains work/copy and has no Trust penalty.
- [x] Only completed delivery advances the Bea-only story to closing; it does not admit the deferred company case. Frozen Bea results and all possessions remain preserved.
- [x] Build actual changed production paths and run targeted command/save/failure/mode checks plus affected shared regressions; record commands/counts/warnings, not historical passes.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] Apply OFFICE_CASE_PLAY on a supported phone: one task/primary action, no overlapping instructions, readable values/details, short satisfying physical feedback, sound-off clarity and safe Back/focus behavior.


- [ ] One short understandable resolution, relevant figure/source readable above dialogue, familiar copy handover gesture.
- [ ] Interrupt before pickup/while carried/during explanation/handover and resume without stuck camera or extra errands.
- [ ] Confirm real 3D targets/animations/text/safe areas/Back/focus/audio on supported devices; no source declaration counts as visual acceptance.

## Handoff and completion definition

Deliver the goal above through its real entry point, including persistence, presentation and checked limitations. Update [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md) with actual evidence and remaining user checks; do not mark implemented for documentation presence. User owns Unity and every Git operation.

Direct dependents: T14. Deferred future dependent: T11. Continue the revised index, retaining prior working changes and historical results.
