# T08 — Transfer the withholding credit and finish the third calculation stage

**5 November conference pickup / split-page follow-up:** Tax due stays on Computation page3; only Tax credit and Remaining payable occupy the next leaf. After the player advances past Bea’s opening thank-you line, during her handover line the actual Level1 ClientCopyPickup component and motion drops a Type8 paper beside her; the talk finishes normally. Click the waiting paper afterward: B-WHT plus a required readable `bea_certificate` Bag item commit in one signed write before flight. Failed/full-inventory collection leaves it retryable; a saved offer restores the uncollected paper. Bag/hotbar click or drag opens the existing physical certificate; its exact amount bands work at the desk. No Take menu or duplicate Supporting Documents paper. Existing certificate source/credit/remaining-payable rules stay authoritative; manual rendered acceptance is pending.

**Callback repair:** DialogueSession clones authored lines. Bea now triggers presentation using the admitted line index, with a consumed one-shot guard and immediate-current-line fallback like the Auditor; no authored-object identity comparison. The reader owner is ensured on demand by Bea or the calculator. Latest PROGRESS records the reproduced failing test, passing fix and remaining native acceptance.

Status: **Implemented, internally verified; user manual pending**. Blocked by: T07.

Priority: P1. Phase: C — Credit and balance. Estimated scope: S–M; one fresh agent context. If real call-site work exceeds this bound, record a bounded split before adding unrelated framework work.

## Goal and visible accomplishment

Collect Bea’s presented paper after her conference conversation, open its Bag item, transfer a printed amount to Tax credit, and attach manually calculated Remaining payable on the Tax and payment leaf.

Revision: **3 November 2026**. Read [REVISION_PLAN](../REVISION_PLAN.md), [SPEC](../SPEC.md), [reuse map](../REUSE_AND_ARCHITECTURE.md), [visual contract](../WORLD_AND_FEEDBACK.md), [verification](../VERIFICATION.md) and [AGENTS](../AGENTS.md). Planned refers to changed revision acceptance, not absence of reusable implementation. Historical baseline: Type8 certificate, Bea conference-room travel, credit explanation/source checks and remaining-payable rules already exist. Integrate into folder StageC without rebuilding paper/model or duplicating questions.

## Existing systems to reuse

Existing source paths relative to repository root; inspect live callbacks/content before changing them. Narrow Level_03 adapters only; no duplicate folder/stamp/calculator/input/save/shop framework.

- `Assets/Scripts/Level3/Level3CertificateRules.cs`
- `Assets/Scripts/Level3/Level3CertificatePaper.cs`
- `Assets/Scripts/Level3/Level3BeaInteractable.cs`
- `Assets/Scripts/Level3/Level3BeaOfficeVisit.cs`
- `Assets/Scripts/Level3/Level3ComputationRules.cs`
- `Assets/Scripts/SideQuest1/SupportingDocumentFace.cs`

## Work and interaction mechanics

1. Keep same Bea actor’s table->conference route after worksheet collection; at credit step present B-WHT during dialogue, then acquire it once by post-talk pickup through the existing command. Keep acquired/year/payee/source requirements and credit-versus-expense help, concise and contextual.
2. Use the existing world pickup motion with a paper visual. Save offer for restore; collect chapter evidence plus readable Bag item atomically before flight. One readable certificate opens from its inventory icon; no duplicate Supporting Documents paper. Use existing exact-value transfer adapter, not special detached credit chooser/extra clipboard tab. Both printed amounts stay legible; selecting income payment as credit may remain a wrong draft for review.
3. StageC shows current Tax due and chosen Tax credit; manual physical subtraction/equals/printed-tape attachment to Remaining payable. Retain certificate income subset of R2, do not add income or deduct expense again. Negative wrong results remain visible with need-to-check state, no clamp.
4. Credit-only changes invalidate balance/stamp, not taxable/tax-due work. Existing unsupported typed/foreign-case/stale tape rules remain. Preserve T08 legacy acquired/explained/credit/payable selections quietly and no duplicate handover.

## Assets, motion and feedback

Read [OFFICE_CASE_PLAY](../OFFICE_CASE_PLAY.md), amended 5 November. Reuse the Auditor’s side-placement/drop/hover/flight and existing reach/dialogue gates with the real paper look. A revealed handover line presents it, one closing line finishes the talk, then clicking collects it. No Take menu. Read through Bag/hotbar click/drag; reuse paper focus/turn/drag/clamp and exact amount bands. The payment leaf has two stable fields with manual calculator keys/tape attachment; no page Calculate/Help, prior-stage collapse or tool rearrangement. User verifies actual layout, motion, source readability and interruption behavior.

## Integration requirements

Preserve shared L1/L2 behavior, existing serialized identities/source scenes and all campaign possessions. Keep current exact-decimal L3 rules/provenance; no float prototype bridge. Use existing input/dialogue/FSM/guide/Bag/reward owners. Career3 stays gated until T18, L4/L5 remain unavailable. No remote ticket, package, scene recopy or global cleanup.

## Persistence and failure rules

Candidate -> signed persist -> publish before success effects. Validate mode/case/source/revision/prerequisites/action IDs; duplicates do not award. Preserve legacy grades/claims/approved snapshots/copies and quiet restores. Failure keeps prior state/source/copy retryable. Clear only owned pointer/selection/locks on Back/settings/disable/focus/camera return; independent work can be left. Practice/replay/Editor cannot mutate live story state.

## Internal checks — AI source/build/regressions only

- [x] Active runtime strings/prompts and fallback errors follow OFFICE_CASE_PLAY: short task/action copy, optional detail, no debug/provenance dump or answer-key reveal; actual bindings inspected.


- [x] B-WHT acquisition/explanation/credit source ownership persists once; cancel/failure retains predecessor.
- [x] Correct credit12,500 yields50,000; wrong printed income/credit-as-expense/cross-case/typed drafts remain unsupported.
- [x] Credit-only edit keeps StageA/B results current and invalidates only balance/stamp.
- [x] StageC uses actual folder support and shared transfers/calculator, old credit tab/view not duplicated.
- [x] Build actual changed production paths and run targeted command/save/failure/mode checks plus affected shared regressions; record commands/counts/warnings, not historical passes.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] Apply OFFICE_CASE_PLAY on a supported phone: one task/primary action, no overlapping instructions, readable values/details, short satisfying physical feedback, sound-off clarity and safe Back/focus behavior.


- [ ] Read certificate and choose amount without dialogue overlap; paper/font size matches previous levels.
- [ ] Inspect/reopen source and prior working, close/back/settings safely; three-stage result is clear without extra panels.
- [ ] Confirm real 3D targets/animations/text/safe areas/Back/focus/audio on supported devices; no source declaration counts as visual acceptance.

## Handoff and completion definition

Deliver the goal above through its real entry point, including persistence, presentation and checked limitations. Update [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md) with actual evidence and remaining user checks; do not mark implemented for documentation presence. User owns Unity and every Git operation.

Direct dependents: T09, T12. Continue the revised index, retaining prior working changes and historical results.
