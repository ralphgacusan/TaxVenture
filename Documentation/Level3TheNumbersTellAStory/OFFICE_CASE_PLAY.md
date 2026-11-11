# Bea office case — concise, tactile play contract

Approved **4 November 2026** after the user reviewed the office storyboard. Design documentation only; active revised T07–T10 remain planned. The current next implementation is T07 after the accepted T06 side quest. No restart of completed work.

## Authority, scope and reading order

This contract refines [REVISION_PLAN](REVISION_PLAN.md), [WORLD_AND_FEEDBACK](WORLD_AND_FEEDBACK.md) and existing T07–T10 for Bea’s case. It makes concise presentation and satisfying physical interactions acceptance requirements in each ticket, not work deferred to T17. [SIDE_QUEST_MACHINE](SIDE_QUEST_MACHINE.md) continues to govern outdoor play and B-WORKSHEET acquisition.

The approved player flow is return with summary → first-use lesson if needed → taxable income → tax due → receive certificate → remaining payable → stamp → Auditor review/correction → explain and deliver. The current Level 3 release contains this Bea case only, followed by its Bea-supported chapter wrap-up. The Nina/Marine company case remains documented for future use but is deferred from the active release sequence; it is not a chapter or Career gate. Do not auto-complete company work, fabricate company evidence, erase legacy progress or imply the deferred case has shipped.

The earlier generated office storyboard is conceptual only. Its tax-table labels were inaccurate and its transfer arrows ambiguous; do not copy those into runtime UI. Use existing canonical six-band reference/fixtures: Bea’s example band is over400,000 through800,000;22,500 +20% ×(600,000−400,000)=62,500. Values below describe solved authored fixtures, never permanently visible answers.

## User stories

1. As a player, I want my collected summary to supply the office inputs, so that the side quest has a tangible purpose.
2. As a learner, I want one short first-use example, so that I learn the gestures without a long tutorial.
3. As a player, I want one calculation active at a time, so that I can focus without a crowded desk.
4. As a learner, I want to choose evidence and a rule, so that the calculator does arithmetic without deciding the case for me.
5. As a player, I want paper feed, clipping and stamping to respond clearly, so that finishing work feels physical and satisfying.
6. As a learner, I want focused review feedback and optional explanation, so that I can correct the actual problem without rereading a lecture.
7. As a player, I want interrupted work and earlier results retained, so that correcting one step does not restart the case.
8. As a player, I want to explain and physically deliver the approved copy, so that Bea’s case ends with a meaningful client interaction.

## Screen contract: glance, act, inspect if needed

Keep real existing Case Folder/paper on the left, actual calculator on the right, short tape in their gap, with stable reading camera and visible desk context. One active stage occupies the existing Computation page, not a new clipboard/full-screen dashboard. Completed stages collapse to a short title and player-produced amount; tap to reopen working. Idle keypad, tax reference, source details, guide dialogue and stamp controls must not all compete at once.

Use current Taxpayer Information/Supporting Documents/Computation page navigation; do not rename/rebuild T03’s familiar taxpayer layout. Existing blank/unknown taxpayer facts remain blank. No new classification gate. Supporting Documents opens one source temporarily; select a value, close/turn page as needed, then place into the intended field with selection retained safely. Only show source beside calculator when actual phone layout fits; otherwise sequence these views rather than shrink text.

### Copy budgets and examples

These are implementation starting limits with actual rendered review, not claims that word counts alone certify clarity. The invariant is one short task, readable figures and accessible explanation. Essential original facts/statutory conditions may require paged detail; never delete or simplify them into false rules.

| Surface | Default budget | Example |
|---|---|---|
| Stage title | Up to 4 words | Taxable income; Tax due; Remaining payable |
| Primary instruction | One line, up to 10 words | Transfer income and deductions. |
| Button | 1–3 words | Calculate; Attach; View working; Read certificate |
| Field | Short label + amount | Income; Deductions; Tax credit |
| Routine neutral acknowledgment | No popup; optional ≤4 words near target | Amount selected; Result attached |
| Contextual hint | One sentence, up to 18 words | Use the withheld-tax amount, not the payment amount. |
| Auditor return | One problem + next action, up to 24 words | Check your tax band. Open the annual table and compare your taxable income. |
| Conversation turn | 1–2 short sentences, normally ≤30 words | Some tax was already withheld. I brought the certificate. |
| Completed-stage summary | Stage name + one amount | Tax due · ₱62,500 |

Only one instruction/help voice is visible at once. Guide must not restate an objective directly above it or narrate every transfer. Routine source selection/printing/attachment needs no dialogue, modal, Continue button or celebratory paragraph. Offer a clearly labeled Help/View working action for expanded meaning; do not hide necessary instructions behind unlabeled icons. Do not expose source IDs, expression token/provenance dumps, revision numbers, validation codes or XP calculations in primary gameplay copy.

Example first-use guidance appears step by step: “Select an amount.” → “Place it here.” → “Choose subtract.” → “Calculate.” → “Attach the result.” Show only the current step, teach once, preserve the taught marker. Use the existing unrelated1,250−480=770 example, not Bea’s answer. No repeated forced lesson after resume or during each stage.

## Stage-by-stage experience

### Return and stage A — T07

Take the one existing `bea_summary` Bag item out manually. Its front Income and Supported deductions are reusable sources. Independently open/position the real calculator and folder; no summary-triggered opening or rearrangement.

Two fixed leaves: page3 Computation has Taxable income, Annual band, its revealed threshold/rate/base/formula and Tax due. The next leaf has only Tax credit and Remaining payable. Income/Deductions fields and active-stage/collapse replacements are removed. Drag summary Income to the calculator, press minus, drag Supported deductions, press equals, then attach its existing print to Taxable income. Original worksheet-token lineage is authoritative without saving hidden input fields. Legacy field-linked tapes still retain their dependency checks.

### Stage B — T07 (manual; 5 November user clarification)

All existing book pages stay unchanged. Append Annual bands – practice with three native styled source chips (up to250,000; over250,000–400,000; over400,000–800,000) and short formula descriptions. The existing full annual reference/domain rules remain available; this is one compact teaching leaf, not a replacement table.

Drag a chip into Annual band. It transfers the complete rule identity, not a percentage or an answer. The field has no chooser/menu; it can also give its selected identity as a value. Reveal three reusable sources—Threshold, Rate, Base tax—and the formula `(Taxable income − Threshold) × Rate + Base tax` in reserved space, keeping each leaf’s rows fixed.

Drag attached Taxable income to the calculator; minus Threshold; equals. Multiply Rate; equals. Add Base tax; equals. The physical calculator preserves the whole sourced chain and prints the final tape for deliberate Tax due attachment. No page Calculate/Help controls or formula-preload shortcut. A wrong band/operation can print/attach neutrally but remains unsupported; changing the band stales old dependent work. Save before flight/clip; failed saves leave the print retryable.

### Certificate and stage C — T08

After tax due has been attached, talk to Bea in the conference room. First Bea says “Thanks for helping me work through the figures.” The player advances past that complete line. When the admitted certificate handover line is then displayed, a saved offer presents the Type8 paper beside her. Use the conversation line index (the session clones authored lines), a one-shot trigger and the immediate-current-line fallback. Reuse the actual Level1 ClientCopyPickup component and its WorldPickupMotion drop/hover/flight and reach/input guards, with a paper model. A closing line finishes the conversation; there is no Take action/menu and the paper cannot be picked during dialogue.

Click it after the talk. Chapter acquisition and readable required Bag item save together before flight. Failed write/full inventory keeps the paper available. A reload after offer but before pickup restores it beside Bea without replaying the drop. Already acquired historical sources quietly receive their own inventory item; inventory failure is retryable and existing possessions remain intact.

Bag/hotbar click or drag opens the existing physical certificate at the icon/drop point. It keeps its Type8 face, exact payment and withheld-amount source bands (both offered alike at the desk), shared focus/drag/bounds and slot put-away. Do not duplicate it as another Supporting Documents paper. The handover itself does not choose a credit or fill a field.

On the Tax and payment leaf, deliberately transfer a certificate amount to Tax credit. Drag Tax due to the calculator, minus Tax credit, equals; attach its printed Remaining payable. Correct fixture62,500−12,500=50,000. Wrong/negative drafts remain visible, unsupported where appropriate; credit edits stale only dependent balance/stamp. No Calculate button or repeated question/tutorial.

### Ready stamp and review — T09

After structural completeness, show three short result summaries with optional working. Put unused calculation tools down before stamp focus. Use the existing shared physical Ready/Not Ready bodies, free placement ghost/press/ink/return and current folder revision; not a duplicate stamp box or a new “submit dashboard”. Marking Ready is the player’s judgment, not auto-approval or a green correctness reveal.

Close/carry the actual folder and use current Auditor talk-first hand-in. A return names one earliest unsupported step and an action to fix it. Longer reasoning opens on demand. Highlight/reopen that stage without discarding other work. Changed upstream inputs invalidate only actual dependent results; use a quiet Recalculate tag. Do not erase old evidence or charge an already-assessed side-quest error again merely because downstream arithmetic followed it.

One brief existing approval surface grants100 coins and freezes case figures/XP once, stops the case clock and leaves copy atAuditor. No automatic client delivery or second chapter result here. Retain current review-root/retry/cap/clock/voucher rules and no Trust penalty for honest mistakes. Warnings are occasional existing time notices, not a new countdown over every field.

### Explain and deliver — T10

Pick up the actual approved read-only copy. At Bea’s existing office client spot, she asks one question, for example “How much is left after the tax already withheld?” Show the relevant portion of the copy above/alongside dialogue without covering its figures. Player points/selects credit/balance or its supporting source, receives one concise response and performs the real copy handover through existing controls.

A wrong explanation gives one contextual hint and retry, without Trust loss, recomputation or a full quiz page. Keep the held copy on cancel/talk later/wrong drop/write failure. Explanation plus actual delivery grants+4 Trust once. No extra service-plan errand, client satisfaction meter or lengthy recap of every calculation. Closing is a separate chapter handoff, not another approval reward.

## Motion, sound and existing style

Reuse actual low-poly folder, imported calculator, paper bodies, clip/page/stamp motions, HudTheme.Font/materials, current AudioManager/settings and existing selection/layer owners. T03 initially opens paper left/calculator right/tape between. The latest user-authorized improvement allows either tool anywhere inside the screen, including overlap; touched assemblies come forward through existing document ordering. Inspect rendered current assets before matching scale/materials; do not recolor the game into a new navy/brass dashboard or add new generated models/audio.

Starting timings: source lift/place0.15–0.25s, short result feed0.20–0.30s, attach flight/clip settle0.20–0.30s, stage collapse0.15–0.20s. Overlap safe cosmetic parts and avoid stacked waits. One quiet meaningful paper/key/clip/stamp cue; no repeated jingles, particle bursts, shaking camera, glowing correctness or per-character dialogue sounds. No haptic feature claim unless the game already supports it. Sound-off play must communicate the same state with motion/text/shape.

Short tape length follows visible essential result; full expression/provenance reopens through working/detail instead of a long strip covering the desk. Never lose mathematical working to shorten a preview. Fit/paginate long formulas without tiny type. Effective primary hit areas aim at48dp with safe spacing; text and numbers remain readable at normal phone distance. Taps are equivalent to every required drag. Cosmetic skip/interrupt never substitutes for a saved command or blocks progress on a missing animation callback.

## Save, input and acceptance

Keep current Level3 computation/checkpointer/signed-save/ClientCopy transaction owners and exact decimal/provenance boundaries. Wrong-case/type/ownership is mechanically refused; plausible wrong same-case work is neutrally accepted. Candidate → persist → publish precedes source/tape/stamp/approval/delivery success effects. Failed saves keep action/source/copy retryable; duplicate presses never attach or pay twice. Restore silently with existing lesson marker, results, frozen grades, stamp revision and copy phase retained. Never modify actual player saves during authoring/testing.

Back closes top detail/reference first and returns focus safely; settings/focus loss/desk exit consume/release only owned pointer/layer/window holds, with no tap-through or lost selection beyond the intended boundary. Reference reopening, stage reopening and correction must preserve unfinished work. No simultaneous old computation leaf and new folder owner. No new global UI/input/camera framework.

Internal checks use current real computation/transfer/checkpointer/save seams, not word-count/source pins alone: correct and plausible-wrong inputs/bands/credit, exact supported transfer, two A/B attachments plus C, stale dependencies, save failures/double presses/resume, earliest-root review, one approval and real delivery. Verify display strings at their actual bindings; hidden debug paragraphs must not leak through fallback messages. Build current affected runtime and whole Editor assembly when shared APIs change, then appropriate affected regression suites.

User acceptance requires screenshots/play on supported phone sizes for the fixed five-field page, source, appended band leaf, certificate, returned note and client-copy dialogue. Check one task/action, readable unclipped values/formula, no overlapping voices, stable world context, tap/drag parity, clips/ink alignment, sound on/off and interruptions. Ask the user to paraphrase the current task and distinguish tax due from remaining payable; record confusion/missed taps/repeated reading and refine copy. No fabricated playtest outcome or manual checkbox from static evidence. T17 repeats whole-flow checks, not first implementation of this contract.

## Out of scope

No production implementation in this authoring task, changed tax rules/rewards/fixture facts, full-screen dashboard, alternative calculator/folder/stamp family, source-answer autofill, extra scored operation mini-game, new narration/theme/assets/packages, forced company case implementation, live-save edits or Git/Unity operations. Keep richer source/legal detail accessible rather than stripping learning content merely to meet a word budget.
