# Worth the Tax? — paper machine implementation contract

**5 November approved summary interaction amendment:** The actual summary occupies the left stand once all batches are correct, replacing the Collect panel and ledge proxy. It uses the shared physical paper/window/ghost system. Opening is a preview; dropping into Bag/hotbar performs the existing atomic chapter/reward collection plus its own `bea_summary` inventory item and readable pages. `B-WORKSHEET` remains its domain work/source identity. No physical duplicate remains in the Case Folder. The inventory rail stays hidden during batches and returns with the summary. Docking, repeated front/back turns, bounded movement, simultaneous folder focus and leave/return behavior follow the latest PROGRESS entry. This amendment supersedes older ledge/Collect/folder-contained presentation below; financial fixtures, grading and office provenance remain unchanged.

Approved interaction revision: **3 November 2026**. Status: **Design approved; T04 and T05 (4 November) and T06 (5 November) implemented/internal-verified, user manual pending**. This document changes the outdoor activity presentation, not the tax fixtures, nine-decision assessment, rewards or office computation. Next implementation is T07.

## Authority and navigation

This contract supersedes earlier direct-to-three-tray placement and oversized worksheet presentation for the revised outdoor activity. Read it with [REVISION_PLAN](REVISION_PLAN.md), [WORLD_AND_FEEDBACK](WORLD_AND_FEEDBACK.md), [CONTENT](CONTENT.md) and the active numbered ticket. If their older shorthand says “three destinations”, interpret that as the three selector treatments, not three physical drop bins. Stable [FIXTURE_CONTRACT](FIXTURE_CONTRACT.md) source/transaction facts and legacy grades remain authoritative. Historical progress is evidence of previous work, not instructions to restore the old board.

Implementation slices: [T04](tickets/T04-bea-encounter-and-original-records.md) owns admission, compact originals and readable table staging; [T05](tickets/T05-worksheet-drafting-and-source-links.md) owns machine/gestures/filing/draft recall; [T06](tickets/T06-worksheet-check-correct-and-collect.md) owns batch checks/corrections/summary collection. Keep all 18 IDs, existing dependencies and working T02/T03 changes. No production work is authorized by this documentation edit alone.

[Six-frame mechanical storyboard](images/side-quest-machine-storyboard.svg) is a layout/sequence reference, not a final asset or measured device view. It depicts the latest feed-first design. The earlier chat sketch turned the dial before loading; that ordering and its incidental invented amounts are superseded here.

## Player purpose and learning

Bea, a freelance logo/menu illustrator, has kept receipts and asks whether all of them can be deducted. The player prepares supported income/expense inputs for her office computation by reading, classifying and explaining exceptions. The output is **Bea’s Income & Expense Summary**, collected into the Bag and available through the existing Case Folder Supporting Documents. It is the existing worksheet item, not a second reward or inventory type.

The rhythm is **read → feed halfway → decide → pull → next paper → check batch → correct → collect summary**. The pleasure is operating a small tactile prop; reasoning remains the challenge. There is no speed/reaction/precision test, paper-jam minigame, fuel, purchase, RNG, extra scored lever task or machine that decides tax correctness.

### User stories

1. As a player, I want Bea to explain her concern briefly, so that I understand why her records need sorting.
2. As a player, I want one readable record and optional original detail, so that I can decide without a crowded screen.
3. As a player, I want to feed a slip, choose a treatment and pull a lever, so that each decision feels physical and deliberate.
4. As a touch player, I want equivalent tap controls, so that precision dragging and long holds never block progress.
5. As a learner, I want optional purpose/duplicate evidence and brief check feedback, so that I can understand exceptions.
6. As a player, I want to recall drafts and correct checked mistakes, so that experiments do not trap the activity.
7. As a returning player, I want saved choices and frozen grades preserved, so that leaving or updating cannot erase or regrade my work.
8. As a player, I want a collectible summary usable in the office, so that the side quest contributes directly to Bea’s case.
9. As a player, I want interruptions and failed saves to preserve my last committed work, so that a cosmetic animation cannot lose a record or duplicate a reward.

## Exact activity content

Nine original papers are acquired once: B-ID, B-BG, B-REV, B-EXP, B-RENT, B-SUB, B-PRINT, B-PERSONAL, B-DRAFT. They remain intact/readable. **Seven activity slips** represent economic entries; they are views of existing rows, not consumable original documents. B-WHT arrives later in the office and is absent from this machine activity.

| Batch | Row | Short face / amount | Expected treatment | Expected reason |
|---|---|---|---|---|
| Earnings (2) | R1 | Logo work / ₱500,000 | Income | None |
| Earnings (2) | R2 | Menu and illustration work / ₱400,000 | Income | None |
| Business costs (3) | R3 | Business rent / ₱180,000 | Deduct | None |
| Business costs (3) | R4 | Business subscription / ₱90,000 | Deduct | None |
| Business costs (3) | R5 | Business printing / ₱30,000 | Deduct | None |
| Questionable records (2) | R6 | Birthday purchase / ₱24,000 | Leave Out | Personal purpose |
| Questionable records (2) | R7 | Printing bill / ₱30,000 | Leave Out | Already represented |

Use existing source facts to show purpose/year/reference; do not manufacture new receipt dates, payees or transactions. B-RENT/B-SUB support existing expense rows, not extra slips. R7 describes the printing already represented by R5, with shared PRINT-26. Optional B-DRAFT shows proposed deductions ₱354,000 = ₱300,000 + ₱24,000 + ₱30,000, not additional activity entries. Leave Out means excluded from these totals, not “all excluded records are tax-exempt”.

Learning progression: earnings identify income; costs connect purpose/evidence to supported deductions; questionable records distinguish personal purpose from business use and recognize duplicate representation. Automatic totals practice no arithmetic decision; the office calculator teaches the later computation.

## Table composition and physical construction

Keep the existing plaza, Bea actor, table, seats, envelope, kraft mat, first-person route, lighting and interaction camera. Use the actual current rendered L1/L2/L3 low-poly scene language. The activity occupies a compact tabletop area rather than lifting a board to cover most of the screen. The old source currently uses a worksheet LiftScreenHeight of 0.8 and a separate outdoor calculator: these are historical implementation details to replace, not layout targets to inherit.

Suggested seated arrangement: readable angled card stand on the left, compact machine on the right, tiny batch/review rack along the machine base, envelope/remaining stack behind the stand. Preserve visible table edges and some Bea/world context. Keep the hotbar, guide and Leave Table controls clear. On narrow screens sequence temporary original/reason/review content in the same focused area rather than shrinking everything or laying out more panels. Fit actual camera projection; a screenshot must prove scale and readability.

The machine has a squat low-poly body, clearly separate intake and output, shallow angled front plate, selector dial, lever with visible pivot, two small roller cylinders at the intake, review tabs and two compact total windows. These are local parts of one prop, not independent UI windows. Its slot must visibly accept paper at the angle presented; paper should not pass through an opaque front wall. Make a local occlusion lip/recess so the feed reads as entering the body. Feeding shrinks no paper; move it behind the slot lip with correct depth/masking. The stand and machine stay anchored during content gestures.

### Reuse and asset truth

Inspect live references and render bounds before selecting meshes. Existing candidate assets observed during authoring:

- `Assets/Synty/PolygonOffice/Models/SM_Prop_CardRoller_01.fbx` and its existing Desk Props prefab: candidate body/roller visual vocabulary; not evidence that the model has usable animated subparts or a three-position mechanism.
- `Assets/3D Models/Core Items/printer1.fbx`: existing printer silhouette/material reference, not a reason to introduce office printing gameplay outside.
- Existing Synty paper and briefcase props: optional context/silhouette reference, not a required three-bin case.
- `Assets/Scripts/Level3/Level3DeskProps.cs`: cached Lit/Box/Roll/Print helpers and existing world-space text construction.
- `Assets/Scripts/Level3/Level3BeaTable.cs`, `Assets/Scripts/SideQuest1/ClientRecordPaper.cs`: table, envelope, support papers, read/return poses, current seat/input ownership.

Prefer an appropriate existing shell if it fits. Add the few missing low-poly parts locally through existing builders when necessary: slot lip, two rollers, cylinder dial/pointer, lever pivot/shaft/handle, stand and review rack. This specific small machine is authorized by the design; earlier “new props only for paper” wording does not prohibit its needed geometry. No downloaded/generated asset pack, paid asset, new renderer, new theme or shared prefab override. Do not claim a new 3D model or animation exists until inspected; implementer binds the parts and colliders, not the user.

### Palette and typography

Match the actual table/papers/HudTheme and imported props first. Existing Level3DeskProps tokens are source-backed candidates: Cream (245,240,226), Paper (250,247,238), Ink (28,38,52), InkMuted (96,106,118), NavyLight (52,66,82), Brass (202,169,101). Use warm paper and a cream or scene-compatible muted body; dark structural trim and small existing accent details only. Do not enlarge the old navy/brass board into a dashboard or impose this palette on existing Synty assets. Selection uses the existing neutral interaction highlight, not green/red correctness color.

Reuse HudTheme.Font, existing paper text/number formatting and world-space faces. Visible labels: Income, Deduct, Leave Out; title, prominent amount, brief purpose; Record n of m; Check batch; Recall; Inspect original; Compare where relevant; Collect. Technical source IDs stay in optional detail. Category text plus simple existing symbol distinguishes choices without relying on color. Labels must stay readable while half-fed; title/amount occupy the exposed header, not under the slot lip or finger. Do not use existing Print auto-size minimum as permission for tiny text: page/reflow detail instead.

## Seven-step record interaction

### 1. Present and read

The next unfiled row of the current batch slides from the compact stack onto the stand. One card is active, with short title, prominent amount and factual context. A contextual instruction says “Read the record, then feed it in.” Optional Inspect original uses the existing viewer and returns to the same card/selection; it does not acquire or duplicate a paper. No automatic nine-paper fan.

### 2. Feed halfway

Drag the active slip to the generous intake target. Near the target it aligns gently to the slot and glides partly inside, leaving title/amount/context accessible. A neutral target outline communicates mechanical validity only. Off-target release returns to the stand without changing any saved choice. Tap card → tap intake executes the same action. Clicking the card alone does not auto-file.

The half-fed position is a held draft, not a completed classification. Disable the intake for other papers while occupied. The original remains intact. The player may withdraw/Back to the stand before filing; opening detail never abandons the pending row. No grading or income/deduction tally change occurs merely on intake.

### 3. Choose treatment

The selector activates only after loading. Start every newly loaded row at a visibly neutral, unselected state. Show three labeled detents; neutral can be a fourth home position without being a valid treatment. Swipe/drag rotates between discrete detents; tapping a label chooses exactly that detent. No free-spinning precision angle or timed lock.

Each selection clicks gently and updates a small “Chosen: Income” style label. The player may change it freely. Label order and symbols remain consistent; no cycling randomization. Every treatment is mechanically available for every current row, including a plausible wrong one.

### 4. Explain exceptions

For R6/R7, show a compact “Why?” picker only after a treatment is selected, regardless of which treatment it is. Use the existing neutral reason vocabulary without marking the expected answer; always include plausible alternatives, so a lone correct button never gives the answer away. Preserve existing reason IDs and source facts; reconcile displayed options with live rules before wiring. Place the short reason card temporarily in the reading area, not as a permanent notes column. Changing treatment does not silently choose a reason.

Inspect original remains available. For R7, Compare deliberately presents B-PRINT alongside the relevant B-EXP printing entry with PRINT-26 readable; no third paper/persistent split dashboard. Close returns to the pending row, selected treatment and reason. Do not label it “duplicate” before Check unless the source itself states that fact. R1–R5 never require this extra picker.

### 5. Pull to file

Until a treatment and any required reason are selected, the lever stays raised/inactive. One concise prompt explains “Choose a category” or “Choose a reason”. Do not play a punishment buzz for an incomplete attempt.

Pull down a short generous distance to confirm, or tap the handle for the same command. Pointer movement previews the lever; crossing the deliberate release threshold commits once. Below-threshold release returns it without filing. Consume that pointer sequence so release cannot activate the next paper. No full-screen drag, repeated tapping, long hold or force simulation.

Validate and persist row+treatment+required reason as one edit before the success feed. If existing row extraction/placement/reason commands are separate, use the smallest combined worksheet command through the existing CommitWorksheet seam: do not publish half a classification or add a parallel save owner. A save failure retains the half-fed row and selected choices, releases the busy state, gives one short retry notice and allows the same action again.

After successful persistence, rollers rotate, paper travels fully behind the slot lip, lever completes its short stroke, then returns. Give every valid filing exactly the same neutral movement/sound whether right or wrong. Do not reveal a correctness badge, valid-expense sparkle or approving Bea emote. A small tab appears after the committed feed and totals update from the published choices.

### 6. Keep a recall tab

One labeled tab per filed row appears in the compact current-batch rack, showing a short record title and chosen category. Max three tabs, no all-seven fan. Tap a tab to recall its existing slip for reconsideration before Check; choose another treatment/reason and pull to replace the same row, never create a duplicate. Keep its old saved classification until the replacement saves. Cancel returns that existing filed state. The active row's reason remains available for editing. Revisiting a filed row can display its committed selection; neutral reset applies to each new/unfiled row, not erasing remembered work.

While recalling, pause the automatic next-card presentation; return to the next unfinished row on completion/cancel. First-check outcomes remain frozen if the batch has already been checked. Accepted earlier batches open for inspection only and cannot be recalled for grading edits.

### 7. Present next

After a successful feed settles, slide the next current-batch card onto the stand immediately. Do not require a separate Next confirmation for every record. No future-batch peek/check path. After the last filed row, stop spawning cards and make Check batch the one primary action. The next batch opens only after current choices are all corrected and accepted. Immediately accepting a batch is allowed only through a successful Check/recheck, never by silently comparing answers during a lever pull.

## Local presentation states and durable boundary

These are behavior contracts, not a mandate for new classes/global FSM phases. Keep local machine states within the current worksheet/table owner and reuse existing chapter phases.

| State | Available action | Successful result | Cancel/interruption |
|---|---|---|---|
| Ready/read | Inspect; feed | Half-fed row | Leave safely; no classification |
| Half-fed | Choose; detail; withdraw | Ready-to-file once complete | Discard only uncommitted input |
| Ready-to-file | Pull/tap; edit choices | Persist candidate once | No write below release threshold |
| Saving | Observe brief busy state | Publish then animate | Failure keeps choices retryable |
| Feeding | Observe/finish cosmetic movement | Review tab; next row | Settle to committed state; never recommit |
| Batch ready | Recall; Check batch | Persist first/check result | Incomplete/failed check freezes nothing |
| Correcting | Recall mistakes; inspect/help | Corrected accepted batch | Save/reopen retains first outcomes |
| Summary ready | Inspect; Collect | Atomic collection + rewards | Remains available until actual commit |
| Collected | Read through Bag/folder | Office continuation | Quiet restore; no fresh output/reward |

Do not persist transform positions, lever angles, roller rotations or animation timers. Restore actual committed drafts/checks first and derive a quiet machine view. A half-fed uncommitted row may return to the stand on re-entry; no durable work was lost. Recall keeps its original saved choice until replacement commits. During a committed feed interrupted by settings/focus loss/disable/exit, snap or settle to filed state on restore without replaying a reward/event or blocking on an animation callback.

Validate current case/content revision/source/row ownership/batch, story versus replay mode and current action identity at commit. Busy gating, pointer consumption and existing idempotent command behavior prevent double pull/check/collect. Do not invent a second reward/action ledger. Unknown/future save data stays retained/blocked per current compatibility contract.

## Checks, correction and assessment

The lever means **file my choice**, never **check my answer**. Only deliberate Check batch reveals correctness. Check considers two choices for earnings, three for costs, four for questionable records (two treatments + two reasons). Incomplete Check identifies missing choices without freezing a grade. Failed persistence produces no first-check marks or accepted-batch transition.

The first complete Check for each batch atomically freezes its own first outcomes before visible marks. Show a small review strip, with localized text/symbol marks and concise contextual help; no medal/score popup per batch. Keep review close to the machine, not a giant worksheet. Wrong choices remain recallable and correctable. Treat wrong reason with correct destination separately, so feedback targets the actual issue. Use current help/book facts; do not invent harsher tax assertions.

When correcting, recall a mistaken row into the same half-fed sequence; correct its destination/reason and pull. No immediate correctness cue from the feed. Check again deliberately. After all choices are correct, accept/gather that batch and admit the next; accepted rows remain inspectable/read-only. Corrections never increase frozen first-check accuracy. Hints are free, wrong pre-check drafts unpenalized, and honest errors never reduce Trust or coins.

Nine decisions total. Gold 9/9; Silver 7–8/9; Bronze 0–6/9. XP = 60 minus first-check wrong decisions (51–60). No score for intake aim, lever operation, time, totals or duplicate-link clicking. Preserve earned legacy v1 12-decision grade/tier/XP and claims exactly; no regrade merely to use this new machine view. Legacy frozen drafts correct through mapped rows without a fresh v2 score; already-collected work skips the activity.

## Totals, output item and office bridge

Two compact windows show current selected Income and Deductions totals from published draft rows using exact decimal values. Wrong choices deliberately yield wrong draft totals; the machine is not an answer-key calculator. Half-fed candidates do not affect totals. Leave Out has no monetary subtotal requirement. Neutral counter motion is identical whether a number rises or falls; larger deductions are not presented as better performance. If windows crowd the phone view, retain concise summary-on-demand in the same machine surface rather than a second panel.

After the final corrected check, show a short assembly/feed of one folded **Bea’s Income & Expense Summary** card at a distinct output ledge. This is not the Collect command: preview may exist while ownership is pending. Short folded front: title, Income ₱900,000, Supported deductions ₱300,000. On deliberate open: brief included entries, links to supporting originals, personal-purchase exclusion and printing counted once. Full detail may page within the existing paper viewer. Do not print taxable income/tax due/credit/balance answers on it; those belong to later office work.

Use **B-WORKSHEET**, existing worksheet inventory/acquisition/source identity and worksheet rule output, with a presentation title change only where needed. Collection must make it readable in the Bag and existing Case Folder Supporting Documents, preserving exact totals and provenance for T07. It remains a worksheet domain object even though the player played with a machine rather than a large literal form.

Collect commits item + frozen grade/badge/mastery + 60 coins +2 Public Trust once through the current transaction owner. Only after successful collection does the existing Bea office route/BeaComputation continuation occur. Failed collection leaves the card retryable on the output ledge, without granting it or moving Bea. Duplicate taps/re-entry do not generate another card/item/reward. Keep base Bag capacity usable for required quest work through current policy; no paid bag gate or silent loss of prior items.

The final medal reflects first-check accuracy, while every collected card has corrected supported figures. One existing final reward card accompanies collection, not per-record celebrations. The office source-transfer interaction reads this item’s totals, not hidden expected values or a second copied summary record.

## Motion, audio and accessibility

Starting tuning ranges, subject to actual user feel/device acceptance:

| Action | Suggested duration | Visible purpose |
|---|---|---|
| Card arrives/returns | 0.15–0.25 s | Clear active record; no camera movement |
| Intake alignment/half-feed | 0.15–0.25 s | Paper catches; header remains exposed |
| Selector detent | 0.08–0.12 s | Small weighted click; selected text updates |
| Lever down | 0.12–0.18 s | Deliberate mechanical commitment |
| Full feed/roller spin | 0.20–0.30 s | Continuous paper entry behind slot lip |
| Lever return/tab settle/next | 0.12–0.20 s, overlap safely | Keep seven records brisk; next ready within about 0.6 s after save on normal path |
| Final summary output | 0.30–0.45 s | One satisfying distinct completion moment |

Do not delay the save for animation or promise a bound during storage latency. Avoid stacked waits, camera shake, bouncing body, sparks/confetti per paper, constant rollers or looping motor sound. Use the existing AudioManager/document/UI/mechanical cue families after verifying actual clip keys; “shff-click” and “whirr-clack” describe desired feel, not claims of existing asset names. `PlayDocumentSFX(false)` and `sfx_paper_place_01` are observed table seams to inspect/reuse; select a current quiet click/feed cue or closest supported paper cue rather than downloading sound. Respect sound/motion settings and prevent overlapping repeated cues.

Tap input provides every drag/rotation/pull function. Effective primary targets aim at 48dp with safe margins, regardless of narrow visible slot geometry. Input hit areas must not overlap selector/lever/source controls; amounts stay out of finger paths. Back closes temporary detail first, then withdraws uncommitted loading, then leaves focus through existing ownership policies. No event tap-through on camera return. Existing settings/guide/focus paths pause/release owned input locks and settle visuals; no new input/camera system.

Teach the first row only with brief steps: “Feed the record”, “Choose where it belongs”, “Pull to file”. Do not teach the correct category or pull automatically. Bea’s existing 16-active-minute clock begins after the first successful saved classification, replacing “first worksheet row placement” with this equivalent boundary. Loading/turning alone does not start it. Compulsory teaching pauses under existing rules; independent reading/sorting uses time. Continued running legacy clock is preserved exactly; no fresh reset from machine initialization. Free next morning keeps work without Trust penalty.

## Verification and completion bounds

Test through current worksheet/checkpointer/save/inventory/office-source seams, using disposable fixtures. Do not use source-string assertions as the sole evidence for behavior. T04 checks acquisition/presentation ownership; T05 checks atomic filing/recall/interruptions; T06 checks grade/collection/office bridge. Record actual current build and test commands, failures and warnings. No Unity/editor launch/automation, real-save access or Git actions under the current pack instructions; user owns those and rendered/device acceptance.

Required end-to-end internal scenario: acquire once → file seven rows with nine choices across three batches → intentionally choose personal/duplicate deductions → check and freeze wrong outcomes → correct and recheck → preview without owning output → collect → verify one Bag B-WORKSHEET and folder link → transfer exact supported totals in office → reload without regrant/regrade. Repeat with failed filing/check/collection writes and duplicate callbacks. Confirm accepted-batch locks, foreign-row rejection, legacy frozen/collected resume, isolated replay and L1/L2 regressions where shared seams change.

Required user visual/touch scenario: normal phone-distance reading at stand and half-feed; drag and tap parity; wrong off-target release; selector neutral/readability; lever handle reach; original/Compare/Why/Back flow; recall during next-card readiness; wrong/correct batch feedback; summary output/Bag/folder read; camera/settings/focus interruptions; no paper clipping/duplicate visual/input owner; seven filings feel brisk and satisfying with sound on/off. Screenshots must show compact table scale and visible world, not just isolated prop geometry. Leave all manual acceptance unchecked until user evidence.

## Out of scope

No new chapter reward, medal criteria, economic rebalance, second worksheet item, office calculator change, T03 reset, third-person mode, machine shop/purchase, new NPC framework, production implementation during authoring, downloadable/generated art/audio, physical destruction of originals, Level1 corkboard replacement, company sorting replay, L4 filing/payment or L5 investigation. The approved specific machine parts do not authorize a generic machine framework or broad cleanup.
