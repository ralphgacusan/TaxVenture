# Current continuation — approved revision, 3 November 2026

## 6 November 2026 — polish pass against Levels 1 and 2

**Implemented and internally verified; Unity Play Mode, rendered, audio, touch and device acceptance pending.** No Git, Unity launch, scene edit, or real-save access.

- **Hotbar:** the inventory already frees the folder at approval. `HudHotbar` drew a slot's + tile only for an empty record, and Level 3 hides the folder icon while it is not carried, so a slot could show nothing. A slot now shows its tile unless an icon really stands on it. The handover gate's saved states are re-read as soon as it ends. Popped-in slots always finish at normal size.
- **Splash and camera:** `SitAtTable` starts the camera and calls `BeginIntroSplash` in the same call (`SideQuestSplash.Show(..., withCamera: true)`, an opt-in that Levels 1-2 do not use). Help Bea starts the move from the conversation's shot. `OpenAfterIntro` waits for the settled camera after the saved acknowledgment.
- **Guide:** new machine and calculator steps use the coach box and hand through `level3:` targets, resolved only while that part is usable. Removed `worksheet-subtotals`, `worksheet-drafted`, `computation-totals`, `computation-done` and `credit-done`; added `calculator-lift`.
- **Objectives:** Finish Side Quest 3 / Finish Bea's Tax Case / Follow Through with Bea; triggers and their order unchanged. `PresentedCasesInLevel` hides CASE 1 / 2 and rescales the HUD EXP bar to one case.
- **Result card:** adds Time to Level 1's template. Editor test runs now also show what the attempt earned (coins, Public Trust), which is not saved. Replay and Practice keep their own card.
- **Plaza:** `Level3PlazaLife` builds Mang Dante on Level 1's patrol and Carlo on Level 1's bench, 40+ m from Bea's table, with Level 1's models and the shared NPC components.
- **Sound:** the audit found existing cues on almost every action. Added only the acknowledgment's paper cue and Talk later's choice cue.

### Verification actually run

`python3 -B Tests/run_level3_repair.py --level2`: runtime 0 errors / 21 inherited warnings, Editor 0 / 10, **Level3Regression 2824/2824** (23 new polish checks), controls 5734, Level2 1580 PASS. GuideRegression 937, Level1LivingPlaza 77/77, CareerBoard 72/72, ChapterEnding 58/58, TrainingRegression 586/600 (the same 14 CRLF pins), CampaignFoundation 739/748 (the same 9 failures before and after this work). `VerifyLevel3Print` 352, `VerifyLevel3Scene` PASS (4 warnings), `VerifyGuideTargets`, `VerifyShopAndGuideView`, `VerifyHudRewardPresentation`, `VerifyHudIcons`, `VerifyHudCopyPickup`, `verify_content` 344, `verify_pack` PASS. `VerifyLevel3Playtest.py` stops at its scene-hash baseline assert (noted before this pass; the scene was not edited). `AudioRegression` does not compile in the standalone harness (its own test code), so no automated audio run.

### Remaining USER checks

1. Hand the folder to the Auditor, including a return and a resubmission: after approval the slot shows +, opens the Bag, and takes the approved copy on pickup.
2. Help Bea, and Later then Sit: the card and camera start together; a quick tap, a double tap, and leaving and returning leave no stuck controls; a resume shows no second card.
3. Machine, summary and calculator: the hand sits on the real part, the box does not cover it, and it stays hidden during demonstrations and modals.
4. HUD and Notes sections, no case chip; the Chapter 3 card in a Story run and an Editor run.
5. Mang Dante's patrol and Carlo's bench pose in Office_Level3 (bench presence, grounding, sit animation), and sound without double playback.

## 6 November 2026 — follow-up: Arthur demonstrates first, then the player does the same task

**Implemented and internally verified; Unity Play Mode, motion/bubble timing, rendered/touch and learner acceptance pending.** User request: make the new teaching work like Level 2's Mark the Money! — Arthur does it first while the player watches (the tools move on their own, he explains in his dialogue bubble), then gives the player the same task to try, then the real Bea work.

### Level 2 pattern reused (traced in `Assets/Scripts/Level2/IncomeToolkit.cs`)

`OpenFlow` → `Demo()` (Arthur's lines through `Level2Say.Line` → `GuideVoice.TrySay`, cards/highlighter move on their own, `demoRunning` blocks input and pauses the clock) → reset everything → "your turn" line → saved `toolkitDemoSeen` (an interrupted demo replays next visit) → the player repeats the same payslip with Arthur's rule line per action → done line → Rosa's real records. Level 3 now follows the same shape with its existing owners; no new UI, dialogue or guide system.

### What the player now experiences

- **Worth the Tax? practice:** after the side-quest splash (instruction now "Watch Arthur, then try it · Then sort Bea's records") and seating, Arthur sorts the same four practice slips himself (`Level3BeaTable.PracticeDemo`): each slip slides onto the stand, half-feeds, the dial turns, P3/P4 lift the Why? card and mark the reason, the lever pulls and the slip feeds in; one Arthur line per step explains the concept (earned → income; for the business + receipt → deduction; receipt alone, personal → leave out; same purchase twice → count once). Player input is off; the review clock was already paused while the packet is due. Then the slips are back, Arthur says "Your turn…", and the player sorts the same four. Wrong-pull tips are now **Arthur's bubble lines**, not printed on the stand's note card; right pulls get his short why; after the fourth he hands over to Bea's records.
- **Desk examples (taxable income, Design Studio tax table, withholding):** when an example card first comes up, Arthur works it himself on the real card and the separate example calculator (`Level3DeskCalculator.LessonDemo`): the amount row lights up and enters the display, keys press, the tape prints and clips onto the card, with one line per step (e.g. "First the excess: taxable income… minus the band's threshold", "The rate applies only to that excess", "It's a credit: it comes off the tax, not off income"). Then the card and example calculator are cleared, Arthur says "Your turn…", and the player does the same example. A wrong tape gets Arthur's tip (the card only says "Not yet. Try again"); a saved tape gets his closing line, then Bea's own step. Putting the tools away (or settings/conversation, which already put them away) stops the demonstration; it plays again on the next lift.
- Guide hint pages are now the player's turn: `atBeaTable`, `sortingPractice`, `calculatorLesson`, `taxLesson`, `creditLesson` are false while Arthur demonstrates; the tax/withholding first pages became "Your turn: select …".

### State

Watching a demonstration to the end saves one presentation marker through the existing signed chapter write (`Level3Checkpointer.CommitDemoSeen`): `demo-sorting-practice`, `demo-lesson`, `demo-lesson-tax`, `demo-lesson-credit`. A failed write just lets it play again. Already-taught examples and older saves past a step never replay a demonstration. Demonstrations file, check, attach, grade or reward nothing and never touch Bea's draft (`lessonCalculator` only).

### Verification actually run

- `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors / 21 inherited warnings**, Editor **0 / 10**; **Level3Regression 2800/2800** ("Arthur first" **25** new checks, inside the teaching section now **130**), controls **5734**, Level2 **1580 PASS** (3 native comparer skips).
- New checks: the slip demonstration is exactly the player's task (same slips, every step's choice judged right, Why? only on P3/P4) and runs through the real `Level3TableStaging.Machine` to an empty machine; each desk demonstration's moves, replayed on a real `Level3Calculator`, produce a chain the real `CompleteLesson` accepts for that same example; seen markers change nothing else, are quiet duplicates, refuse when not due, and count as seen once the example is done; signed-write order without rewards; source pins that the demonstrations move the real stand/machine/Why?/lever and card/keys/tape, never call CommitWorksheet/CommitSortingPractice/CommitCalculatorLesson/CommitComputation or the case calculator, block player input, stop on leave/put-away, speak tips through Arthur, and gate the guide flags.
- `VerifyLevel3Print.py` **352 PASS** (practice tips are no longer note-card print; two new card messages measured). `VerifyLevel3Scene.py` PASS (4 warnings), `VerifyGuideTargets.py` 14/0, `VerifyShopAndGuideView.py`, `VerifyHudRewardPresentation.py`, `verify_content.py` (344), `verify_pack.py` PASS; GuideRegression **937**, Level1LivingPlaza **77/77**, CareerBoard **72/72**, TrainingRegression **586/600** (same 14 pre-existing CRLF pins). The `VerifyLevel3Playtest.py` scene-hash note from the previous entry still applies.

### Modified paths (this follow-up)

`Assets/Scripts/Level3/{Level3SortingPractice.cs,Level3PracticeRules.cs,Level3BeaTable.cs,Level3DeskCalculator.cs,Level3CalculatorModel.cs,Level3PracticeCardModel.cs,Level3Checkpointer.cs}`; `Assets/Resources/Tutorials/Level_03/guide.json`; `Tests/Level3Regression/Program.cs`; `Tests/VerifyLevel3Print.py`; this PROGRESS, KNOWN_ISSUES and AGENTS. No Git, Unity launch, scene/prefab/package edit or real-save access.

### Remaining USER checks

1. New Chapter 3: Help Bea → splash → seat. Watch Arthur sort all four slips (timing of each bubble vs. the motion, Why? card lift for P3/P4, nothing tappable meanwhile), then "Your turn". Try a wrong pull per slip (Arthur's tip, slip stays), finish, then Bea's R1. Leave the seat mid-demonstration and come back (replays from the start); reload after watching (does not replay; your turn starts at slip 1).
2. Office: lift the calculator — Arthur works 10,000 − 2,000 (rows light, keys press, tape prints and clips), clears it, "Your turn". Same for the Design Studio card after Bea's taxable income, and the withholding card after her certificate. Put the tools away mid-demonstration and lift again. Try a wrong tape (Arthur's tip).
3. Judge whether Arthur's line lengths/reading times feel right on a phone, whether any bubble line is cut off or covers the card, and whether learners can then do each task alone.

## 6 November 2026 — teaching pass: practice packet, four questions and three desk examples

**Implemented and internally verified; Unity Play Mode, rendered/touch, timing and educator/learner acceptance pending.** User-requested focused teaching improvement to Bea's chapter only. Every existing mechanic, fixture, answer, scored decision, Trust rule, inventory behavior, review rule, clock rule and progression is retained; no Bayview case, cutscene, other chapter or campaign change. No Git, Unity launch/automation, scene/prefab/package edit or real-save access by this pass.

### Revised player journey (where each concept is taught, practised, then applied)

1. **Meet Bea** (unchanged envelope / record / sketch / Help Bea / Later). Her concern now reads "I kept every receipt. Can I count them all? I'm unsure what belongs as business costs." beside the existing logo/menu and studio-saving lines.
2. **Practice packet, before her first scored filing** (`Level3SortingPractice`, driven by `Level3BeaTable`). Four fictional practice-painter slips take the existing stand and go through the same feed → selector → Why? → lever: P1 mural fee (earnings are income), P2 paint with receipt PR-02 (a supported business cost may be deducted), P3 concert tickets with a receipt (a receipt alone does not make a personal purchase deductible; Why? = personal purpose), P4 card line for the same paint (two records of one purchase count once; Why? = already represented). Each pull is answered at once: wrong springs the lever back with one "Look again:" line naming the question to ask (no category named), and the same slip waits; right feeds in with a one-line why. Nothing is filed, checked, scored, rewarded, timed or added to Trust/inventory; Bea's clock still starts only at her first saved filing. After the fourth, one saved milestone (`teach-sorting-practice`) ends it; Bea's R1 follows. Not shown when any of her records is already filed (older saves continue unchanged).
3. **Worth the Tax? (unchanged scoring)** plus a rereadable reference: Tax Code Book Section 4 now ends "Ask of every record: Was it earned? Was it for the business? Is it supported? Is it already counted?" (new read-only term Four Questions; no section added or renumbered). Checked-error briefs (worksheet.json and the built-in mirror) now open with the relevant question and point to where to look, never the category or reason.
4. **Office taxable-income example** (the existing first-use calculator lesson, same card/marker/flow): Income 10,000 − Costs (supported) 2,000 = 8,000 Taxable income on its own example calculator; Bea's sheet stays read-only until it is saved.
5. **Bea's taxable income**: independent, unchanged (no prefill, no auto operation).
6. **Tax-table example** (new, `TaxCard`): due once her Taxable income tape is attached and before she picks a band. The Practice Design Studio from Section 8: the card note and Arthur's hint show that 350,000 falls in Section 7's band over 250,000 to 400,000; the player then does (350,000 − 250,000) =, × 15% =, + base tax 0 = and attaches Tax due 15,000. Wrong tapes get one hint (rate missing, rate on the whole income, base missing, order) and retry.
7. **Bea's band and tax due**: independent, unchanged (neutral drafts, review/correction, dependent invalidation).
8. **Certificate**: conversation, paper drop, deliberate acquisition and Bag behavior unchanged. Before her credit, a **withholding example** (`CreditCard`): the same studio's Tax due 15,000, Income payment 20,000 (printed, never to be used) and Tax withheld 2,000 → 15,000 − 2,000 = 13,000 Still to pay. Hints: using/adding the income payment, adding the credit, reversed order. Card note "A credit, not a cost". The retired credit question remains unused (no active credit question; no Trust effect).
9. **Finish** (balance question, papers, stamps, Auditor review, client copy, handover, closing) unchanged. Practice/examples never touch `balanceWrongAnswers`, the worksheet first check, review records or any reward.

### State, resume and the clock

- Narrow state only: chapter milestones `teach-sorting-practice`, `teach-tax-table`, `teach-withholding` (+ action ids), alongside the existing `calculatorTaught`; saved through the existing candidate → signed `CommitLevelThreeCheckpoint` → publish path (`Level3Checkpointer.CommitSortingPractice`, `CommitCalculatorLesson(lesson, …)`). Practice/Replay/Editor runs keep them in memory only (existing `SavesStory`).
- Due rules (`Level3PracticeRules.CurrentLesson`, `Level3SortingPractice.Due`) are false for any chapter already past the step (a filed record; a band/tax due; a credit/balance; any Auditor submission), so older saves are never reset or forced back. Their optional review is the existing Tax Code Book (Sections 4, 7, 8, 9) and the Tutorial Hub drills. `ImpliedLessons` marks the three guide lessons (and the packet's feed/choose/pull hint) as shown, so nothing replays after reload.
- Interruptions: a desk example's draft lives on the separate example calculator (unsaved; finishing later is safe); the packet's slip progress lasts while the scene is loaded and restarts after a reload.
- `Level3ReviewClock.ClockPaused` now also pauses while a demonstration is due (`TeachingPaused`), on top of the existing pauses; a running saved clock keeps its exact time/day.
- An example becoming due while the tools are up (Bea's taxable tape just clipped on) settles the tools, prints its card, and lifts again (`TickLessonSwitch`); `AttachPrintedToFolder`/`AttachLeaf` now consume the case calculator's own draft. The folder Computation page redraws when an example starts/finishes.
- The Tutorial Hub office drill fixture (`OfficePractice`) counts all three desk examples as given (the drill is itself practice); the machine drill shows the practice packet.

### Systems reused

Existing clipboard card model and first-use lesson flow (now three specs; three/four-amount cards lengthen instead of shrinking type), example calculator and tape transfer, `Level3TableStaging.Machine` and the paper machine/stand/lever/Why? presentation, note-card look-again line, `Level3Checkpointer` signed commits, `Level3ChapterRules.ImpliedLessons`, guide.json hint lessons/flags/cues, Tax Code Book leaves, worksheet hint briefs, `Level3ReviewClock`/`ReviewClock.Advance` pause. No new framework, scene object, asset (except the one new script and its .meta), package or UI surface.

### Verification actually run

- `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors / 21 inherited warnings**, Editor **0 / 10**; **Level3Regression 2775/2775** (new "Teaching pass" section: **105 checks**), controls **5734 PASS**, **Level2Regression 1580 PASS** (3 existing native EventSystem comparer checks skipped).
- New checks cover: packet due/not-due (fresh, filed, office, v1, accepted batch), slip isolation (no Bea rows/sources/amounts), every right and wrong treatment/reason with question-style hints, the real machine flow on P3/P1, completion only adds its marker, duplicate/refusals, clock not started and paused only while due, implied lessons, signed-store failed write/retry/reload; four-question book paragraph/term and all briefs (question + where to look, no category/reason, built-in mirror); all three example cards (sources, lengths, 15% print), the studio chain on the real calculator, read-only gating of Bea's band chips and sheet amounts while due, marker-only completion leaving her sheet/tapes untouched, seven tax and five credit refusals, early/duplicate/foreign refusals, old-save skipping, one example at a time, running-clock preservation, round trip, drill fixture, and source pins for command order/no rewards/guide wiring.
- Two mutation probes (packet due rule without the filed-record guard; lessons without the current-lesson guard): 5 targeted failures; sources restored and verified by shasum.
- Updated existing pins for the deliberate changes: Bea's intro line (still ≤ 30-word merged turn), the taxable-income example values/tape lines, the calculator flag, the pull page's practice cue, credit-link/creditStep, the case-draft owner, and the stage tests' fixture (examples given).
- `Tests/VerifyLevel3Print.py`: **365 PASS** (was 273): every example amount/result/label/note/title/message and hint, compact-row height, practice slip titles/amounts/purposes/position/details and every "Look again" line at the shipped font metrics (not a render; the U+2212 minus is not in the atlas, so card messages use "-").
- `VerifyLevel3Scene.py` PASS (4 warnings); `VerifyGuideTargets.py` 14/0; `VerifyShopAndGuideView.py`, `VerifyHudRewardPresentation.py`, `verify_content.py` (344), `verify_pack.py`: PASS. GuideRegression **937 PASS**, Level1LivingPlazaRegression **77/77**, CareerBoardRegression **72/72**, TrainingRegression **586/600** (the same 14 pre-existing CRLF pins).
- `Tests/VerifyLevel3Playtest.py`: its binding checks pass, but its fixed scene-hash baseline assertion **fails** because `Assets/Scenes/Office_Level3.unity` was modified at 07:58 today outside this pass (this pass did not touch any scene). Needs the user's confirmation of that scene change and a new baseline by whoever owns it.
- The open Editor's log showed recompilation requests and only inherited warnings at the time of checking; Unity's own import of the final edits was not confirmed.

### Modified paths

`Assets/Scripts/Level3/{Level3SortingPractice.cs (new),Level3SortingPractice.cs.meta (new),Level3PracticeRules.cs,Level3PracticeCardModel.cs,Level3DeskCalculator.cs,Level3DeskCalculatorTape.cs,Level3DeskCalculatorComputation.cs,Level3ComputationPage.cs,Level3BeaTable.cs,Level3TableStaging.cs,Level3Checkpointer.cs,Level3ChapterRules.cs,Level3ReviewClock.cs,Level3EditorFixtures.cs,Level3WorksheetHints.cs}`; `Assets/Resources/Tutorials/Level_03/guide.json`; `Assets/Resources/Levels/Level_03/{book.json,worksheet.json,town_life.json}`; `Tests/Level3Regression/Program.cs`; `Tests/VerifyLevel3Print.py`; this PROGRESS, KNOWN_ISSUES and AGENTS. Existing uncommitted work in these files was preserved; line endings unchanged.

### Remaining USER checks (Unity/device; not run here)

1. New Chapter 3 run: Help Bea → seated: the first stand card reads Practice 1 of 4; feed/choose/pull hints; deliberately choose wrong on each slip (lever springs back, one readable "Look again" line, slip stays), then right (short why, next slip); P3/P4 Why?; after P4, Bea's R1 arrives. Confirm no clock chip starts until her first real filing; leave/return mid-packet (resumes), reload (restarts), reload after finishing (never again).
2. Wrong Bea batch: the look-again brief reads as a question and points to the right record/book section. Open Tax Code Book Section 4 and read the four questions (paragraph/term fit).
3. Office: taxable-income example (10,000 − 2,000) then Bea's stage A; on attaching her taxable income the tools settle and lift with the Design Studio card; check its four rows, 15% chip, step messages, a wrong tape (hint, retry), the saved tape, then Bea's band/tax due. Collect her certificate; the withholding card; try the income payment (hint), then 15,000 − 2,000.
4. Check the clock chip does not move while any example is up or due, and that a running clock resumes from the same time afterwards; interrupt each example with Settings/Back/app switch.
5. Phone readability of the longer example cards, the compact rows and the note-card hint lines; touch/tap parity; sound on/off. Ask learners to explain income vs deduction vs leave out, why a receipt is not enough, excess × rate + base, and credit vs deduction; record confusion. No learning effectiveness, visual quality or touch readability is claimed.

## 6 November 2026 — second copy-icon report: refresh the actual button

**Implemented and internally verified; native drag/render acceptance pending.** This supersedes the previous copy-icon repair: the user reported that the COPY icon still accepted neither a press nor a drag during Bea's awaited handover.

- Found the missed serialized identity: in Office_Level1, Office_Level2 and Office_Level3, `FolderToHUDIcon.folderHUDIcon` points to `FolderHUDDropArea`, a separate invisible Image with no HudIconButton. The prior direct GetComponent refresh was therefore a no-op. The latest Editor log confirms quiet copy restoration followed by the balance explanation, without a button unlock or pointer-down.
- Removed that ineffective call from `Assets/Scripts/CaseFolder/FolderToHUDIcon.cs`. The actual `Assets/Scripts/UI/HUD/HudIconButton.cs` now subscribes/unsubscribes `RefreshFolderAvailability` to the existing `GameplayEvents.OnClientCopyPickedUp` in its folder branch. The pickup owners already raise this notification after quiet storage. This unlocks both the Button and its HudSubmittableIcon through the existing availability owner; unrelated icons stay unchanged. No added progress event, save/reward mutation, scene or prefab edit. The existing HudDragItem visual and Bea drop resolver remain wired.
- Added `Tests/VerifyHudCopyPickup.py`: compiles the actual production HudIconButton with minimal managed engine-boundary substitutes and invokes its lifecycle/event path. Before the fix it failed at **Copy pickup must unlock the actual Button and its drag/pointer controller**. After the fix **6/6 PASS**: locked baseline, quiet copy pickup, unsubscribe, single resubscribe, no invented copy on empty count and unrelated Notes isolation. Four expected CS0649 warnings concern serialized private fields populated by reflection in this harness. This exercises production subscription/availability behavior, not Unity native pointer dispatch/rendering.
- Updated `Tests/VerifyHudIcons.py` to guard the actual pickup subscription and reject the false drop-area/button assumption: **15/15 PASS**.
- `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors / 21 inherited warnings**, full Editor **0 errors / 10 inherited warnings**, Level3 **2669/2669**, controls **5734 PASS** (8 harness compiler warnings), Level2 **1580 PASS** (3 existing native EventSystem comparer skips).
- Modified paths: the two production files and two tests above, this PROGRESS and KNOWN_ISSUES. Existing line endings retained. No Git, Unity automation, actual-save access or native playtest.
- Manual follow-up remains T17: after recompilation, collect the approved copy and answer Bea's question, press-drag its COPY icon, see the existing ghost folder follow the pointer, and release onto Bea. Also verify wrong-target return, tap fallback, cancellation and duplicate/failed-save behavior. Keep visual/device acceptance pending.

## 6 November 2026 — client copy icon cannot start Bea handover drag

**Implemented and internally verified; Unity/device acceptance pending.** User screenshot shows Bea waiting on the copy action and the folder count at 1.

- The existing Editor log records the icon being locked after the Auditor hand-in, then Bea's balance explanation being accepted with no subsequent folder pointer-down. `Level3Audit.PickedUpCopy` uses `FolderToHUDIcon.RestoreStored`, which restores the stored count quietly without the normal stored event; the icon's Button and HudSubmittableIcon therefore remain locked. It only unlocked later during HUD teardown, as recorded in the log.
- Fixed the shared `FolderToHUDIcon.RestoreStored` seam: after storing, refresh the assigned icon through its existing `HudIconButton.RefreshFolderState`. All restore callers receive the same input refresh; no progress event, save/reward command, scene or prefab change.
- Modified paths: `Assets/Scripts/CaseFolder/FolderToHUDIcon.cs`, `Tests/VerifyHudIcons.py`, this PROGRESS and KNOWN_ISSUES. Existing file line endings retained.
- `python3 -B Tests/VerifyHudIcons.py`: the new quiet-restore availability contract failed before the fix, then **15/15 PASS**. These are source contract checks, not a native pointer reproduction.
- `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors / 21 inherited warnings**, full Editor **0 errors / 10 inherited warnings**; Level3 **2669/2669**, controls **5734 PASS**, Level2 **1580 PASS** (3 existing native EventSystem comparer checks skipped; controls harness has 8 compiler warnings).
- No Unity/editor automation, Play Mode or real-save access. Manual follow-up: after recompilation, collect the approved copy again and answer Bea's balance question, then drag the icon onto her; also try icon tap, wrong-target drop and cancel/retry. Confirm the copy stays actionable on a failed save and the earned Trust occurs once. T17 remains active; visual/touch acceptance is pending.

## 6 November 2026 — fifth playtest: real audit inventory removal and repeat corrections

**Implemented and internally verified; Unity/device acceptance pending.** User reported that the audit hides the folder icon while leaving its item in the Bag, and a later failed audit cannot be redone.

- Reproduced both problems: the returned folder still occupied its real inventory slot, and OpenFolderReturnToDesk.ForceReturnToDesk only showed the closed mesh after storage had disabled CaseFolder3DDrag. The existing return now calls that exact owner's RestoreAndEnableFolder before stacking it. This restores dragging/opening on every return, through the same shared desk-drop presentation.
- Extended the existing Level3ReviewRules.SyncFolderInventory adapter to follow actual folder location: onDesk, atAuditor and delivered mean no carried folder slot/collectible/pin. Both signed review paths (returned checkpoint and approved/grant candidate) save this removal before publishing; temporary review runs use the existing isolated inventory commit. HudHotbar repairs older ghost-slot saves through existing removal/retry and refuses world-located folders as carried. Taking it from the stack uses the existing store/acquisition path; approved client-copy pickup acquires a normal inventory item in the same candidate and refuses a truly full inventory without displacing prior items.
- A third repeat blocker was reproduced separately: after a stamp-only return, applying the same wrong stamp regenerated the previous submission signature, permanently refusing it as unchanged. A stamp-only return now increments the attempt revision while preserving every calculation value, tape and attachment. Fresh stamps remain resubmittable, including a repeat wrong choice; correcting only the stamp approves the retained work. Full field/value/support returns retain the previously requested complete-redo behavior.

### Verification actually run

- Initial `python3 -B Tests/run_level3_repair.py --suites`: two new focused reproductions failed (actual returned inventory occupancy; closed drag owner never restored). A later stamp-only repeat reproduction also failed before its fix. All now pass.
- `python3 -B Tests/run_level3_repair.py --level2`: current runtime **0 errors / 21 inherited warnings**, full Editor **0 / 10**, Level3 **2668/2668** before the final retained-calculation assertion, controls **5734**, Level2 **1580 PASS** (3 existing native EventSystem comparer checks skipped).
- Final `python3 -B Tests/run_level3_repair.py --suites --level2` against that current compiled runtime: **Level3 2669/2669**, including **35 fifth-playtest checks**, controls **5734 PASS**, Level2 **1580 PASS**, same 3 native skips. The final test-only assertion confirms correct restamping approves retained tapes/attachments. One older source expectation now recognizes the unified world-location guard used by desk, Auditor and recipient.
- Actual-command repeat loop: three rejected reviews total, reload at each desk return, reacquire folder, rebuild through actual summary → subtraction tape → annual band → threshold/rate/base chain → tax-due tape → certificate credit → remaining-payable tape → supporting attachments → stamp; fourth complete review approves. Each hand-in empties the actual slot and preserves the frozen worksheet. Separate tests cover unchanged/repeat stamp refusal recovery, approved-copy reacquisition, full-capacity refusal, signed failed-write isolation, retry/reload and old ghost-slot cleanup.
- `Tests/run_level3_repair.py`'s `suite('Level1LivingPlazaRegression')` against the current runtime: **77/77 PASS**. `Tests/VerifyLevel3Playtest.py`, `VerifyHudRewardPresentation.py`, `VerifyShopAndGuideView.py`, `verify_content.py` (344), `verify_pack.py`: PASS. Modified project text retains UTF-8 CRLF.
- No Git, Unity/editor launch, Play Mode, actual-save access, scene/prefab/package changes or rendered/device verification. Internal checks cannot certify the native drag/stack animation or ordinary plus/Bag appearance.

### Modified paths

`Assets/Scripts/Level3/{Level3ReviewRules.cs,Level3Checkpointer.cs}`; `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs`; `Assets/Scripts/UI/HUD/HudHotbar.cs`; `Assets/Scripts/CaseFolder/OpenFolderReturnToDesk.cs`; `Tests/Level3Regression/Program.cs`; this PROGRESS, AGENTS and KNOWN_ISSUES.

### Remaining manual checks / continuation

T17 remains active. Submit a wrong folder from bar, then from Bag after Take it out: its slot should use the normal plus and no folder remains in Bag/collectibles/pins. Close the return summary/dialogue, return to the office desk, drag the top folder out, correct/redo, store it and submit again. Fail at least twice and verify every desk return can be dragged/opened. Try a stamp-only false hold twice, then Ready: calculations/tapes/attachments should remain and the corrected stamp should approve. Reload after a return, then after approval; deliberately pick up the client copy and verify it receives one actual slot. Check that other items can occupy vacated slots and a full inventory leaves the copy with the Auditor. Prior T13/L3-R106, native T17 acceptance and T18 release checks remain open.

## 6 November 2026 — fourth playtest: desk-arrival splash and delivered-copy inventory

**Implemented and internally verified; Unity/device acceptance pending.** Current user correction supersedes the third pass's splash-before-seat order.

- Help Bea and the table-after-Later path now call the existing SitAtTable camera transition first. An owned coroutine waits for the actual Workstation camera transition and input settle, then calls the same SideQuestSplash.Show. Pending intro blocks paper/machine input, the stand-card arrival and the atBeaTable guide flag. Continue persists the same acknowledgment and releases the pending state, without a second camera move or HudWindowLock/profile-viewer hold. Save failure keeps the intro due for retry; exit/disable cancels the owned wait and splash.
- Root cause of the delivered-copy gap: the world folder binding was released, but its inventory slot/record remained. Added a small Level3DeliveryRules adapter over existing InventoryRules.Remove/Unpin. The delivery candidate now saves copy-done, earned Trust and actual inventory removal together, then notifies existing inventory listeners. Temporary attempts use their existing isolated inventory commit. Old delivered saves clean up through HudHotbar's existing world-removal/retry path; FolderCarried refuses delivered copies so they cannot be reacquired invisibly. The vacated slot uses the ordinary empty/plus UI and accepts another item.
- Preserved the closing question's Review her records action. After delivery it can show only acquired originals through the existing read-only reference-card viewer, without persisting a new inventory item, restoring a copy icon or changing chapter/rewards.

### Verification actually run

- Before edits: two focused reproduction checks failed for immediate splash timing and missing signed inventory removal.
- `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors / 21 inherited warnings**, full Editor **0 / 10**; Level3 **2634/2634** including **18 fourth-playtest checks**; controls **5734 PASS**, Level2 **1580 PASS** (3 existing native EventSystem comparer checks skipped).
- New actual-command tests cover a copy in either bar or Bag, unchanged undelivered copy, actual slot/collectible/pin removal, another item using the freed slot, duplicate cleanup, signed-store failure/retry/reload with once-only Trust, and acquired-only read access after delivery. Source binding checks verify camera-before-splash scheduling, real settle guards, no second seat transition, pending input/guide gates, exit cancellation, signed candidate order and old-save cleanup. Updated the earlier source expectations for the corrected entrance order/cleanup.
- `Tests/run_level3_repair.py`'s `suite('Level1LivingPlazaRegression')` against the current runtime: **77/77 PASS**. `Tests/VerifyLevel3Playtest.py`, `VerifyHudRewardPresentation.py`, `VerifyShopAndGuideView.py`, `verify_content.py` (344), `verify_pack.py`: PASS. Project text retains UTF-8 CRLF. No scene/prefab/package edits in this pass; previous placement markers remain intact.
- No Git, Unity/editor launch, Play Mode, actual-save access or rendered/device verification. Tests do not certify the observed camera move, splash timing or plus/Bag appearance.

### Modified paths

`Assets/Scripts/Level3/{Level3BeaTable.cs,Level3Checkpointer.cs,Level3DeliveryRules.cs}`; `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs`; `Assets/Scripts/UI/HUD/HudHotbar.cs`; `Tests/Level3Regression/Program.cs`; this PROGRESS, AGENTS and KNOWN_ISSUES.

### Remaining manual checks

T17 remains active. Use Help Bea and, separately, Later → table: visibly arrive at the desk before the splash, then Continue with no second camera move. Exit during the camera move; re-enter once, and check settings/focus interruptions and acknowledged reload. Deliver Bea's copy from bar and Bag: the physical handoff should leave an ordinary plus/free slot, no separate copy in Bag/collectibles/pins, and no reappearance on reload. Put another item in that slot. Use Review her records after delivery and close its existing viewer normally. Prior T13/L3-R106, T17 device acceptance and T18 release work remain pending.

## 6 November 2026 — third playtest: highlights, receipt, inventory, placement and entrance

**Implemented and internally verified; Unity/device visual acceptance pending.** This is the user's current authorized scope; earlier changes remain intact.

- Reused HighlightEffect and the existing grey outline renderer. Separate table/featured-record hit areas now bind their actual rendered models instead of empty colliders. Level 3's ClientRecordPaper, machine/stand surfaces and calculator targets receive the same hover highlight. Cached document captures redraw during highlight fades; inactive surfaces/teardown release focus. Other levels keep their normal record behavior.
- Root cause of stale-looking receipt: SetTape retained the full calculation but always selected page zero. It now opens the latest page; a short final page overlaps earlier lines so the operation and final result stay visible together. All earlier pages remain accessible on the same physical receipt; decimal/provenance chains are unchanged. Tests exercise lengths 0–40 plus Bea's real seven-line subtraction, 20% multiplication and base-tax chain.
- Root cause of ghost inventory: physical attachment only hid the hotbar wrapper, leaving its slot and collectible record occupied. Checkpoint candidates now use existing InventoryRules.Remove/Unpin, copy readable pages into the folder with KeepReference, save the packet and inventory together, then notify existing inventory listeners. Temporary modes use the existing isolated inventory commit. Legacy reconciliation removes stale carried copies without rebuilding them every frame. Recovery respects attachment flags; detaching/returning uses normal Acquire/Move with fresh action IDs, retaining readable source data. A full inventory refuses recovery without displacing earlier possessions; the actual paper stays available. No new inventory area or framework.
- Added the existing Supply Counter Placement to Office_Level3 with the exact saved Level 1 position, rotation and scale. Added **Bea Outside Placement**, a small scene component whose position/yaw sets the table, stand, machine, papers, viewpoint and Bea's outdoor starting place together. It previews the actual table/Bea assets using gizmos. Move/rotate this root in edit mode before Play; no assembly is required. The runtime controller remains under Level3Gameplay; the placement does not move Bea's later conference-room spot or create another NPC.
- Both Help Bea and the later table tap use existing SideQuestSplash.Show. It holds first-person controls, shows SIDE QUEST 3 / The Numbers Tell a Story with the shared art/animation, saves sideQuestIntroSeen on Continue, then enters the existing seat. Failed saves leave the intro due. Repeat entry/reload does not replay an acknowledged splash; disabling the table cancels only its owned splash/hold. Existing clock pause follows the shared splash state.

### Verification actually run

- Before edits: `/tmp/l3_third_red.py` failed seven focused reproduction contracts (receipt page, inventory reconciliation, attached-paper recovery, splash, supply marker, Bea marker and record hover). All seven passed after implementation; permanent behavior and authoring checks are below.
- `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors / 21 inherited warnings**, complete Editor **0 / 10**. Level3 **2616/2616**, including **104 new third-playtest checks**; controls **5734 PASS**; Level2 **1580 PASS**, with 3 existing native EventSystem comparer checks skipped. Updated four obsolete onboarding/input source expectations to allow the requested saved splash and highlight cleanup.
- New actual-command tests cover real emptied slots accepting another item, no separate Bag/bar/collectible/pin records while attached, readable pages retained in the folder, repeated detach/reacquire without duplicates, full-capacity refusal, signed-store failed-write isolation and successful retry/reload, complete/latest receipt pagination, and splash copy/acknowledgment/reload gates.
- `Tests/run_level3_repair.py`'s `suite('Level1LivingPlazaRegression')` against the current runtime: **77/77 PASS**. `python3 -B Tests/VerifyLevel3Print.py`: **273 PASS**. `Tests/VerifyLevel3Playtest.py`: PASS; the supply pose exactly matches Level 1 and removing just the two marker records/root entries reproduces the entire prior scene hash `3e8d29dfd93c2efc…`.
- `Tests/VerifyLevel3Scene.py`: PASS, **4 warnings** (authorized Level3 scene change, inherited missing CaseDropDestination, two paths inside prefab instances). Same scene GUID; new hash `f676f4f60c5c9dcf…`. Updated its exact-script-set contract to permit only the two authorized marker components. `VerifySupplyCounterInteraction.py`, `VerifyHudRewardPresentation.py`, `VerifyShopAndGuideView.py`, `VerifyGuideTargets.py` (14 paths / 0 missing), `verify_content.py` (344), `verify_pack.py`: PASS. Modified project text retains UTF-8 CRLF.
- No Git, Unity/editor launch, Play Mode, real-save access, package/shared-prefab change or rendered/device test. Font/scene/source checks do not certify outline appearance, default outdoor clearance, splash timing or touch feel.

### Modified paths in this pass

`Assets/Scripts/Level3/{Level3CalculatorModel.cs,Level3ReviewRules.cs,Level3Checkpointer.cs,Level3WorksheetCheck.cs,Level3CertificateRules.cs,Level3CertificatePaper.cs,Level3SummaryDocument.cs,Level3BeaPlacement.cs,Level3BeaPlacement.cs.meta,Level3BeaTable.cs,Level3TableStaging.cs,Level3BeaTableSeat.cs,Level3PaperMachineModel.cs,Level3DeskCalculator.cs,Level3DeskToolHitTarget.cs}`; `Assets/Scripts/Interactables/HighlightEffect.cs`; `Assets/Scripts/SideQuest1/ClientRecordPaper.cs`; `Assets/Scripts/UI/HUD/HudHotbar.cs`; `Assets/Scripts/RewardsSystem/{PlayerProgressManager.cs,PlayerProgressData.cs}`; `Assets/Scenes/Office_Level3.unity`; `Tests/{Level3Regression/Program.cs,VerifyLevel3Playtest.py,VerifyLevel3Scene.py}`; this PROGRESS, AGENTS and KNOWN_ISSUES.

### Remaining manual checks / continuation

T17 remains in progress. Import the changes, stop/restart Play, inspect both named placement objects, and check the default outdoor clearance and group rotation. Verify grey highlights while standing/seated and while moving among paper fields, machine controls, calculator keys and open documents; verify they clear under settings/disable. Enter through Help Bea, then leave/re-enter, and separately use Later → table; check the existing splash and one camera/seat transition. Compute the full three-step tax chain: newest receipt page first, all earlier steps still accessible. Attach each actual paper from both Bag and bar; its old slot must show the ordinary plus and accept another item, with no separate collectible/pin. Detach, store and repeat; check reload, a full Bag and the existing auditor return path. Scene appearance/animation/device acceptance, T13's unrelated all-items capacity scenario and T18 release checks remain pending.

## 6 November 2026 — second screenshot playtest fixes

**Implemented and internally verified; user Unity/rendered/device acceptance pending.** This entry supersedes older Inspect controls, bottom-of-stack ordering, blank classification, retained editable packet on a field return, and the bespoke Chapter 3 result. Existing unrelated work and scene identity are preserved.

- Moved only the originals/envelope group to the left of the stand. Current stand/recall source and compare partner now occupy the actual front two stack slots, with a short reorder settle. The exposed originals tap or drag into the existing movable document reader; deliberate stack drops return each independently to the top. Removed stand Inspect controls and outside-tap dismissal. A reader cue explains that originals stay on the table and cannot go to Bag/icons.
- Moved the physical rectangular Check batch key to the stand base, retained its existing action/press, and used the existing Level 2 green-circle/white-check geometry. Summary collection completes on the actual icon landing, uses the existing icon pop, then waits 0.5 seconds before the completion modal.
- Plaza seating no longer overwrites the office folder reference pose. Summary/certificate taps open in the centre at their fixed drag-open reading scale. Both use the existing actual paper motion and can attach/detach in the supporting page; their icons hide while attached. Only ID/background and these two reserved attachment slots appear there, with no cloned summary/certificate models.
- Improved example clipboard title/label/amount hierarchy and spacing. Receipt ink is 136.5 points (30% above 105), three lines per page, with the existing 70-point unusually-long-number fallback. Calculator transfers pulse in size and settle from brass to mint. The band formula is larger, and the computation/payment fields use shared green/red category/type feedback. Existing Exit/put-away buttons share a responsive, smoothly centred row for one, two or three actions; stamp and calculator remain independently usable.
- Supplied classification/type, left Income Sources blank and retained Section 3. Actual front-page strings/enums and supporting-paper attachment state persist in a copied folder packet. Transfers save before changing the actual row; failed saves leave it untouched. Auditor validates all front-page fields and requires both papers before approval, reports real field/support/math issues before stale-stamp feedback, and checks the current revision.
- Actual hand-in removes the carried folder icon. Returned folder uses the existing desk-stack/store path: stamp-only retains fields, calculations, tapes and attachments; other packet issues reset editable fields/calculations/tapes and attachments for a complete redo while preserving acquired sources and the frozen worksheet. Saved desk/carried location survives reload.
- Chapter 1 and Chapter 3 now call one shared reward-template function, with the same visual/animation/button owners and run EXP, career EXP and best-on-level footer. Continue is the sole chapter action.

### Verification actually run

- Initial red checks reproduced automatic Income Sources filling and the plaza seat leaking into the office folder pose. New actual domain and signed-store tests cover front-page values, each missing supporting paper, real issues with stale stamps, stamp-only preservation, packet copy/serialization, failed-save isolation and retry/reload.
- `python3 -B Tests/run_level3_repair.py --level2`: current runtime **0 errors / 21 inherited warnings**, complete Editor **0 / 10**; **Level3 2511/2511**, including **25 second-playtest folder checks**; controls **5734 PASS**; Level2 **1580 PASS**, with 3 existing native EventSystem comparer checks skipped. `Tests/run_level3_repair.py`'s `suite('Level1LivingPlazaRegression')` against that current runtime: **77/77 PASS**.
- `python3 -B Tests/VerifyLevel3Print.py`: **273 PASS**, now checking all eight actual book leaves, all six selectable annual bands and the shared Continue text. Section 7 uses the existing 58-unit chips / 26-point caption fallback and measures 857/860 units; this is geometry evidence, not a render. The minus glyph's hyphen width is an explicit estimate. Normal receipt text at 136.5 points and long-value fallback fit.
- `Tests/VerifyLevel3Scene.py`: PASS, 3 inherited warnings, unchanged scene SHA `3e8d29dfd93c2efc…` and GUID. `Tests/VerifyHudRewardPresentation.py`, `Tests/VerifyShopAndGuideView.py`, `Tests/VerifyGuideTargets.py` (14 paths / 0 missing), `Documentation/Level3TheNumbersTellAStory/verify_content.py` (344) and `verify_pack.py`: PASS. All modified project text checked as UTF-8 CRLF without doubled CR.
- No Git, Unity/editor launch, Play Mode, real-save access, scene/prefab/package mutation or device test. Source and font geometry cannot certify placement, rendered text or animation feel.

### Modified paths in this pass

`Assets/Scripts/Level3/{Level3TableStaging.cs,Level3BeaTable.cs,Level3PaperMachineModel.cs,Level3PracticeCardModel.cs,Level3CalculatorModel.cs,Level3SummaryDocument.cs,Level3CertificatePaper.cs,Level3CaseFolder.cs,Level3ReviewRules.cs,Level3Checkpointer.cs,Level3Audit.cs,Level3DeskCalculator.cs,Level3ComputationPage.cs}`; `Assets/Scripts/SideQuest1/{ClientRecordPaper.cs,AttachedRecordsPage.cs}`; `Assets/Scripts/UI/HUD/{CaseFolder3DView.cs,HudHotbar.cs,OfficeHudRuntime.cs,HudRewardScreen.cs}`; `Assets/Scripts/UI/LevelCompleteUI.cs`; `Assets/Scripts/RewardsSystem/PlayerProgressData.cs`; `Assets/Scripts/Data/DragDrop/CaseFolderFieldRow.cs`; `Assets/Scripts/Interactables/AuditorInteractable.cs`; `Assets/Scripts/Level2/PutAwayButton.cs`; `Assets/Resources/Levels/Level_03/cases/case_bea_001.json`; `Assets/Resources/Tutorials/Level_03/guide.json`; `Tests/{Level3Regression/Program.cs,VerifyLevel3Print.py}`; this PROGRESS, AGENTS and KNOWN_ISSUES.

### Remaining manual verification / continuation

T17 remains in progress. In Unity/device play, check left-side originals while standing/seated, each active-source stack reorder, both compare papers out together, independent stack return, blocked Bag return, stand key press/check, summary icon landing/pop/pause, office folder pose after leaving the plaza, centred click/drag scale, both real attachments/unclipping/reload, all three desk actions together, receipt/formula/type feedback and the shared conference-room Chapter 3 EXP result. Submit missing/incorrect personal data, missing each attachment, wrong calculation with an old stamp, and stamp-only; confirm icon disappearance and the appropriate physical desk-stack return/reset. T13's unrelated L3-R106 capacity decision, late-state fixtures and T18 release acceptance remain open. No acceptance is inferred from passing internal checks.

## 6 November 2026 — requested Chapter 3 playtest fixes

**Implemented and internally verified; Unity rendered/device checks pending.** The user's screenshots and current request authorize these changes. Existing in-progress work is retained.

- Calculator/clipboard lift poses now centre the pair, moving the clipboard right. Receipt ink grows from 70 to 105 points, uses four lines per page, and fits unusually long amounts down to 70 points without dropping digits.
- Summary/certificate reuse ClientRecordPaper's motion and document layers for actual icon-origin/icon-return flights (the Case Folder's 0.12 icon scale). Their inventory taps toggle. Certificate input uses the summary's independent-paper raycaster; outside taps no longer close it, and it never disables calculator input. Inventory return resolves the correct item's slot, including a paper already in the Bag.
- Certificate world drop waits until the player advances past the “here's the certificate” line. Bea's acquired ID supplies a fictional TIN to the existing folder and prints as TIN ID.
- Book: shorter Sections 4–10, original section numbers retained; shared classification Sections 1–2 omitted only in Chapter 3; one numbered Section 7 annual-bands table replaces the duplicate practice page, supplies all six bands, and loses the practice heading. New read-only Section 11 explains the client copy. Section 3 remains available.
- Stamp capture retains its foreground document layer and now uses the same short near plane as the stamp camera, so an aimed grip is not clipped by the main camera's near plane.
- Audit Summary shows tax due, credit and remaining payable, plus a compact XP/coin footer. It waits for Continue. Shared fact chips wrap instead of shrinking the whole row.
- Follow-through choices persist before feedback. Earned Trust: 0–2 wrong = +4; 3–5 = +3; 6–8 = +2; 9+ = +1. Existing Trust is not removed. Duplicate/invalid/stale callbacks and failed writes do not inflate attempts or grants; reload retains the count. Other chapter rewards retain their outcomes.
- Chapter 3 calls the actual shared CaseFinale (Bea clone on the conference-room left, existing camera/bubble/emotes/fade) and HudRewardScreen (same chapter status, buttons, medal/XP/reward art and animation). Only the shared Continue remains; it saves acknowledgment before Career. No separate finale implementation was created.

### Verification actually run

- Red reproduction before fixes: six failures in the existing Level3Regression for calculator/certificate input, paper-drop timing, audit density/timing and the extra result exit.
- Current runtime + complete Editor compiled with `python3 -B Tests/run_level3_repair.py --level2`: 0 errors, 21 existing runtime warnings / 10 existing Editor warnings. After updating obsolete expectation pins, final `python3 -B Tests/run_level3_repair.py --suites --level2`: Level3 **2493/2493**, controls **5734**, Level2 **1580 PASS** (3 pre-existing native comparer checks skipped).
- Added real-command range/invalid/duplicate/serialization coverage and signed-store failure/retry coverage for wrong choices; existing signed delivery tests retain once-only grants.
- `Tests/VerifyLevel3Print.py`: **269 PASS** including 105-point normal tapes and the 70-point largest-number fallback. `Tests/VerifyLevel3Scene.py`: PASS, 3 inherited warnings; scene hash remains `3e8d29dfd93c2efc…`. `Tests/VerifyHudRewardPresentation.py`, `Tests/VerifyShopAndGuideView.py`, `Tests/VerifyGuideTargets.py` (14 paths / 0 missing), `verify_content.py` (344), `verify_pack.py`: PASS.
- No Unity launch, Play Mode, real-save access or device/render verification. Source/geometry checks do not certify animation quality.

### Modified paths in this pass

`Assets/Scripts/Level3/{Level3DeskCalculator.cs,Level3DeskCalculatorComputation.cs,Level3CalculatorModel.cs,Level3SummaryDocument.cs,Level3CertificatePaper.cs,Level3BeaInteractable.cs,Level3RecordRules.cs,Level3CaseFolder.cs,Level3Audit.cs,Level3DeliveryRules.cs,Level3Checkpointer.cs,Level3OfficeStart.cs,Level3PaperMachineModel.cs}`; `Assets/Scripts/SideQuest1/{ClientRecordPaper.cs,SupportingDocumentFace.cs}`; `Assets/Scripts/UI/{LevelCompleteUI.cs,TaxCodeBookUI.cs}`; `Assets/Scripts/UI/HUD/{HudHotbar.cs,HudRewardScreen.cs,CaseFinale.cs,Doc3DLayer.cs}`; `Assets/Scripts/Data/LevelBookLeaves.cs`; `Assets/Scripts/Level2/Level2DeskStamps.cs`; `Assets/Scripts/RewardsSystem/{CampaignRewards.cs,PlayerProgressData.cs}`; `Assets/Resources/Levels/Level_03/{book.json,cases/case_bea_001.json}`; `Assets/Resources/Tutorials/Level_03/guide.json`; `Tests/{Level3Regression/Program.cs,Level2Regression/Program.cs,VerifyLevel3Print.py}`; this PROGRESS, AGENTS and KNOWN_ISSUES.

### Remaining user visual checks / continuation

T17 remains the active integration ticket. Check calculator first lift, normal/large tape print, both paper icon toggles and return flights (hotbar + Bag), outside taps, dragging the calculator with both papers open, Bea's completed spoken handover before the drop, stamp grip while aiming/pressing, audit reading size and final shared conference-room scene. Try three wrong follow-through choices, reload, then deliver and verify +3 Trust once. Existing unrelated T13 base-capacity decision and T18 release checks remain open; this pass does not mark them accepted.


## 6 November 2026 — T17: integration pass (in progress)

Status: **In progress.** T14–T16 are internal-verified; T13's **L3-R106** is still an open user decision; late-state Editor fixtures were not added; all Unity/device/readability/audio/timing acceptance is outstanding. No production change was needed by the checks below. No Git, Unity, real-save access, scene/prefab/package edits.

### Decision still needed — L3-R106 (T13)

With base capacity (bar 3 + Bag 6) and every Chapter 1/2 possession kept (notes, Tax Code Book returned, folder, profile binder, Arthur's file, Rosa's marked records, postcard, bookmark set, two vouchers), Bea's certificate has no free slot after her summary: 10 items with no Level 3 exit for 9 slots. Today the pickup refuses plainly and the paper waits beside Bea. Choose one: (a) Level 3's Make room may hold an earlier chapter's reference record or keepsake in the existing owned hold; (b) the certificate gets a slot-free reader (e.g. from the Tax and payment leaf); (c) accept that a Satchel/voucher use resolves it (conflicts with "Base capacity must support all required work"). Nothing was changed to hide it.

### Integration evidence (Tests/Level3Regression T17, 33 checks)

- Twelve real saved states (fresh, assigned, records, summary ready, office, payable, approved, carried, delivered, finding, pending result, completed office): a resume ticks exactly the first n of the 14 Notes steps, never a later one first; the completed office has all 14 and replays no guide lesson.
- Every `flag:` in Level 3's guide lessons is a real Level3Checkpointer fact; guide pages, Notes steps and the closing lines contain no case/source/row identifiers or unfilled tokens.
- One owner per closing seam (`ClientCopyHandover.LevelDelivery`, `ReceptionistInteractable.LevelMessage`, `ShowLevelThreeChapter`, `CommitAcknowledgment`); the retired worksheet board is never built; the legacy single-case completion states are refused in all 16 phases.
- Retired-view trace: `Level3WorksheetDesk`/`Level3WorksheetDeskReview` (the old board, tapes, notes) have no constructor call and no scene component; `worksheet-subtotals` waits on `flag:worksheetUp`, which can no longer be true. `Level3PracticeCardModel` is live (the calculator's first-use example). The old board source is kept as legacy evidence for its regression pins (L3-R131).

### Commands actually run (this pass and the final state)

- Probe (scratch, removed): lessons/steps not implied at CompletedOffice → none. Python scan of guide flags/identifiers → none.
- Mutation probe: the finding's implied trigger removed → 4 failures (T14 reload + three T17 prefix/restore checks); restored by shasum (675e461f…).
- Final `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors/21 inherited warnings**, Editor **0/10**, Level3 **2280/2280** (T13 37, T14 74, T15 28, T16 24, T17 33), controls **5734**, Level2 **1580** (3 native comparer skips). `VerifyLevel3Scene.py` PASS (3 inherited warnings, scene hash unchanged), `VerifyLevel3Print.py` 269 PASS, `VerifyHudRewardPresentation.py`, `VerifyShopAndGuideView.py` PASS, `VerifyGuideTargets.py` 14 paths / 0 missing. GuideRegression 937, CareerBoardRegression 72/72, TrainingRegression 586/600 (14 pre-existing CRLF pins).

### USER playtest script (supported phones; not run here; nothing measured)

Use a completed Chapter 2 campaign (one with base capacity and many kept items, and one upgraded), sound on and off, portrait and landscape where supported. Note the clock time at each numbered step to compare with the relaxed 15–20 minute target; record help used, repeated reading, missed taps, covered values and any stutter.

1. Career → Chapter 3. Reception: Rosa-aware line, assignment; Notes shows one active step. Interrupt once (Settings → back).
2. Plaza: meet Bea, envelope, one record, Help Bea, seat. Machine: feed, choose, Why? on R6/R7, lever; check each batch, look again on a mistake; collect the summary (Bag). Reload mid-batch once.
3. Office desk: first-use calculator example once; stage A taxable income, stage B band + tax due. Base-capacity save: try Bea's certificate with a full Bag (L3-R106) and note what you would expect.
4. Certificate in the conference room, stage C credit and remaining payable. Stamp, Auditor: one deliberate wrong band first, then correct; check the clock chip and a voucher if owned.
5. Collect the copy, Bea's balance question, hand over (drag and icon tap). Then her draft question (try a wrong statement and Review her records), the receptionist's message, the Chapter 3 card. Reload with the card up once.
6. STAY IN OFFICE: supply counter; with ≥ 250 coins buy the computer, watch it arrive on the printer desk, open the register and HER PAPERS. Leave via Settings → Career.
7. Tutorial Hub: Bea's Case tab, both drills (guided and mastery), LEAVE mid-drill once. Career: replay Chapter 3 to its card (REPLAY · BEST ONLY).
8. Throughout: no text clipped or tiny, no panel over values, one cue per action, camera stable while reading, Back/Settings/app-switch always return control.

Next: decide **L3-R106**, then finish T13 and T17 (late-state fixtures if wanted, device findings). **T18 is not ready.**

## 6 November 2026 — T16: Level 3 practice drills and best-only mastery replay

Status: **Implemented, internally verified; USER Unity/device acceptance pending.** No Git, Unity/editor launch, real-save access, scene/prefab/package edits.

### Traced before editing

- Level 3 already isolated non-story runs: `Level3ChapterRules.UsesStoryCheckpoint` (story + not practising + completed Chapter 2), every `CommitLevelThree*` returns early without `UsesLevelThreeStory`, `Inventory` gives practice/replay a temporary per-scene record (DirectEditor previews a deep copy only for a Chapter 2 campaign), purchases browse only, vouchers are temporary claims for the visit. A Career replay of a completed chapter (`CareerProgress.LaunchMode` → MasteryReplay) started a fresh local chapter, but its acknowledged result wrote nothing, so replay mastery could never improve.
- The Tutorial Hub had Level 1 cards only (training.json tabs basics/sidequest/case); drills compose guide lessons/flags; `TrainingCatalog.LessonSeen` also opens cards of a completed chapter. Level 3's practice starts existed only as Editor fixtures (`#if UNITY_EDITOR`).

### Changes

- `Assets/Resources/Tutorials/Training/training.json`: tab `bea` ("Bea's Case") and cards `bea.machine` / `bea.office` (their own Level_03 lessons). New drills `Drills/l3_machine.json`, `Drills/l3_office.json` (+ `.meta` 0a9b182c…, bcea458c…): Level_03 / Office_Level3, `fixture` start, guided steps reuse `machine-batch-filed`, `computation-rule`, `computation-tax` and wait on real Level 3 flags; mastery runs three short steps.
- `Assets/Scripts/Training/TrainingData.cs`: additive `TrainingDrill.fixture`.
- `Assets/Scripts/Level3/Level3EditorFixtures.cs`: no longer Editor-only (practice starts); new `office-practice` (FirstOfficeUse with the calculator taught). `Level3Checkpointer.cs`: `TryPracticeStart` (non-story only; quiet restore without the welcome-back line), the acknowledgment's non-story branch calls `CommitLevelThreeReplay` (a failed write keeps the card pending).
- `Level3ClosingRules.ApplyReplayBest` / `NormalizedAccuracy`; `PlayerProgressManager.CommitLevelThreeReplay` (MasteryReplay of a story-completed Chapter 3 only, one candidate, MasteryBoundary best-only). `LevelCompleteUI.cs`: non-story Chapter 3 cards show the run label and `CampaignRun.WhatChanges`, no coin/Trust tiles.
- Tests: `Tests/Level3Regression/Program.cs` (T16 section, 24 checks; two Editor-only fixture pins and the `RestoreQuietly` anchor updated); `Tests/TrainingRegression/Program.cs` (Level 3 cards checked against Level 3's guide/scene; tab/total pins updated).

### Commands actually run

- `--build-only` after edits: one compile error (an unassigned out variable when no manager exists) fixed; then 0 errors, 21/10 warnings.
- First T16 run threw (an existing check anchored on `RestoreQuietly()`'s old signature); anchors updated; then Level3 **2247/2247** (T16 **24**). Mutation probe (replay eligibility gate removed): exactly the eligibility check failed (2246/2247); restored by shasum (91ab2736…).
- Final `python3 -B Tests/run_level3_repair.py --level2`: runtime **0/21**, Editor **0/10**, Level3 **2247/2247**, controls **5734**, Level2 **1580** (3 native skips). TrainingRegression **586/600**: all 14 failures are its existing CRLF pins on files that were LF before this work (TrainingData.cs, TrainingSession.cs, PracticeModeRuntime.cs, training.json, the seven Level 1 drills, TutorialHubSetup.cs); the two new drills are CRLF and pass. GuideRegression **937 PASS**, CareerBoardRegression **72/72**.
- T16 checks: both practice starts (Bea only); catalogue and drills (Level 3 scene, fixture, real flags/lessons, short steps, no company drill); story writes refused in Practice/Replay/Editor; temporary inventory/vouchers/purchases; replay weaker/better/repeated (byte-equal outside the mastery record; frozen 8/9 Silver kept; only the improvement joins the career total; no farming); v1 11/12 story vs v2 8/9 replay (same tier and XP, no change, versions kept); eligibility; signed-write order; card wiring. Binder rendering, drill play, ribbon/LEAVE and the replay flow in Unity were not run.

### USER manual script (Unity/device; not run here)

1. On a Chapter 3 save open the Tutorial Hub: the Bea's Case tab shows two cards (tab spacing with four tabs); on a Chapter 1/2 save it is hidden.
2. Practise Worth the Tax?: starts at reception with Bea's records handed over, PRACTICE ribbon, no welcome-back line; sit, file, check three batches; the drill completes; LEAVE returns to the binder. Back on the story: wallet, Trust, Bag, Bea's snapshot unchanged.
3. Practise Three Office Stages: the desk calculator needs no example; stages A, B, the certificate from Bea, C; completes.
4. From Career replay Chapter 3 to the end: result card says REPLAY · BEST ONLY with no coin/Trust tiles; Continue: the career board's best XP rises only if better; repeat: no change.
5. Interrupt each (Back, Settings, app switch, reload): returns to the binder/menu with the story intact.

Next: **T17**.

## 6 November 2026 — T15: the optional office computer and Bea's verified register

Status: **Implemented, internally verified; USER Unity/device acceptance pending.** No Git, Unity/editor launch, real-save access, scene/prefab/package edits. L3-R106 (T13) re-checked: still open; T15 adds no inventory item.

### Traced before editing

- `office_computer` (250, office tab) existed as a `pending:office_computer` teaser in shop.json and `ShopCatalogue.BuiltIn()`. Purchases already run through `ShopRules.Purchase` (status, request-id receipt, anchor, debit, ledger, ownership in one candidate) and `PlayerProgressManager.CommitPurchase` (writes only an Applied candidate, then `PurchaseCommitted`); decor is installed by `OfficeDecor` from `DecorRules.Resolve` with a 0.3 s arrival in all three offices.
- No computer or `ComputerInteractable` exists at the Level 3 desk; `ComputerHomeUI` (in the scenes' workspace UI) only routes to the legacy prototype calculator/return forms, which must stay off. The consultant desk already holds the folder, stamps, book, the Level 3 calculator and the pencil cup corner; a separate `PrinterDesk` (scale 3 × 1 × 1) stands beside it in Office_Level1/2/3.

### What the player now does

1. Before Level 3 is completed the computer card is the same teaser ("Requires corrected Level 3"). Afterwards, at the Supply Counter or the menu shop, it is available for 250 coins; too few coins says how many more; nothing is required by the chapter, practice or replay (they only browse).
2. Buying it: one debit with a "Bought Office Computer" ledger line, ownership and the printer-desk placement in one signed write; the existing receipt; the supplied Synty monitor and keyboard arrive on the printer desk with the existing lift-and-settle when first seen.
3. Use computer → the Client Register on the existing Audit Summary card: Beatriz Mendoza · 2026, VERIFIED, "Approved by the Auditor · Copy delivered", Taxable income ₱600,000.00, Tax due ₱62,500.00, Tax credit (withheld) ₱12,500.00, Remaining payable ₱50,000.00 and (in Level 3) the finding; CLOSE, or HER PAPERS to read her saved papers in the Case Folder reader. Nothing is saved or changed.

### Changed paths

- `Assets/Resources/Campaign/shop.json`, `Assets/Scripts/RewardsSystem/CampaignShop.cs`: `office_computer` requires `chapter:Level_03`, anchors `printer_desk`, plain effect text; new `printer_desk` surface anchor (`ShopCatalogue.PrinterDeskAnchor`).
- `Assets/Scripts/Interactables/CampaignShopAssets.cs` + `Assets/Resources/Campaign/shop_assets.asset`: `computerMonitorPrefab` / `computerKeyboardPrefab` reference the Synty SM_Prop_Computer_Monitor_01 / Keyboard_01 prefabs (not copied).
- `Assets/Scripts/Interactables/DecorModels.cs`: `ComputerFootprint`, `Footprint(itemId)`, `BuildComputer` (fitted copies, their colliders removed, plain stand-ins if a reference is missing). `Has()` unchanged: the catalogue shows the computer icon, not a miniature.
- `Assets/Scripts/Interactables/OfficeDecor.cs`: per-item footprint (z uses the surface's z scale), computer build + `MakeTerminal` (one box, Interactable layer, terminal), colliders follow visibility while a piece waits to arrive.
- New `Assets/Scripts/Interactables/OfficeComputerTerminal.cs` (+`.meta` 1910f23b…) and `Assets/Scripts/Level3/Level3RegisterRules.cs` (+`.meta` f4212314…).
- Tests: `Tests/Level3Regression/Program.cs` (T15 section, 28 checks); `Tests/CampaignFoundationRegression/Program.cs` (one superseded computer pin updated; that suite does not compile in the standalone helper, L3-R116).

### Commands actually run

- `python3 -B Tests/run_level3_repair.py` after the production edits: runtime 0 errors/21 inherited warnings, Editor 0/10, Level3 2195/2195 (existing checks unaffected), controls 5734.
- T15 section first run 2221/2222: my test helper wrote an AlreadyApplied repeat, unlike `CommitPurchase` (writes only Applied); helper corrected and the owner's order pinned. Then **2223/2223** (T15 **28**).
- Mutation probe: register no longer requiring the completed chapter → exactly the "not verified" check failed (2222/2223); source restored by shasum (df5cb85b…).
- Final `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors/21 inherited warnings**, Editor **0/10**, Level3 **2223/2223**, controls **5734**, Level2 **1580** (3 native comparer skips). `VerifyShopAndGuideView.py`, `VerifySupplyCounterInteraction.py`, `VerifyHudRewardPresentation.py`, `VerifyHudIcons.py`: PASS.
- T15 checks: catalogue/fallback parity and validation; PrinterDesk in all three scene files; teaser before completion, too expensive, shop not introduced, browse-only replay; refused without a valid anchor; one atomic purchase (coins 300 → 50, one ledger line, ownership, placement; Trust/XP/chapter/possessions/Rosa unchanged); repeat request AlreadyApplied, second request Owned; one computer resolved, separate from the pencil cup; signed write failure (disk bytes unchanged), retry, reload restores without repurchase, arrival once; register figures equal the frozen review and reading changes nothing; unfinished/tampered/missing data shows no figures; no answer key/save/purchase/prototype route in the register/terminal; chapter completes without it; wiring. Placement, monitor facing, the card and touch are not rendered.

### USER manual script (Unity/device; not run here)

1. With a completed Chapter 3 and ≥ 250 coins: open the Supply Counter → Office tab: Office Computer is available; with fewer coins it says how many more; on a Chapter 2 save it is the teaser.
2. Buy it once (double-tap the buy button): one debit, one receipt. Walk to the consultant office: monitor and keyboard settle on the printer desk, upright, screen facing the room, clear of other props; the consultant desk is unchanged.
3. Look at it: "Use computer". The register card: readable figures, CLOSE returns control; HER PAPERS opens her papers; Back/Settings while it is open.
4. Reload/re-enter Level 3 (and a Level 1/2 replay): the computer is still there, no repurchase, no second arrival.
5. Confirm nothing offers calculation, return preparation, filing or printing.

Next: **T16**.

## 6 November 2026 — T14: Bea's supported finding, Quax's message and the acknowledged chapter result

Status: **Implemented, internally verified; USER Unity/device acceptance pending.** No Git, Unity/editor launch or automation, real-save access, scene/prefab/package edits. One new script (`Level3ClosingRules.cs` + unique `.meta` GUID 9c3e206d…); no new manager, dialogue/UI/save framework or asset family.

### Prerequisites inspected

- T10: implemented/internal-verified (5 November); its delivery saves copy done, ClosingFinding (Bea active, company never admitted) and +4 once; ClosingFinding had no further owner. Re-verified in this build (T10 41/41).
- T13: still **in progress** on one design decision (L3-R106: base capacity when every earlier possession is kept, at Bea's certificate pickup). Its possession/pin/voucher/decor continuity is implemented and was re-run here (37/37). T14 adds no item and changes no inventory, so L3-R106 does not block T14; it remains open.
- Content fact: Bea's B-DRAFT (DRAFT-B26) is issued by Bea herself, a rough claim schedule (354,000 proposed; 24,000 private party purchases; 30,000 printing listed again; the B-EXP schedule supports 300,000). The "Assistance Desk-prepared summary" in CONTENT/REVISION_PLAN belongs to the deferred company C-DRAFT (AD-26). The Bea-only finding therefore states only what her own file shows (KNOWN_ISSUES L3-R109).

### What the player now does

1. After delivering Bea's copy, talk to her again (same conference-room spot). One question (19 words): her draft listed ₱354,000, you used ₱300,000; what did her papers show? Her existing docked choice card (heading WHAT BEA'S FILE SHOWS) offers three one-line statements, Review her records and Talk later.
2. "Party ₱24,000 was private; printing ₱30,000 listed twice." is saved first (`CommitFinding`), then her reply ("corrected the draft by ₱54,000 … shows the mistake, not why") and one pointer to the receptionist. "Someone added those costs on purpose." / "Drafts like this must all be wrong." get one reply each (not why / only her own draft) and the same question waits: nothing saved, no Trust change.
3. Review her records ends the talk and opens her papers in the existing Case Folder reader (`HudHotbar.OpenViewer(CaseFolder)`, read-only saved pages) once the dialogue has returned; Talk later/Back also save nothing. The next talk asks the same question.
4. The receptionist (marker available again) passes on Mayor Quax's message: three short lines and the player's one acknowledgment, no choice or reward. Completing it saves ClosingFinding → WrapUpPending; cancelling saves nothing.
5. Once no dialogue holds the screen, the existing result card (LevelCompleteUI → HudRewardScreen, large side layout) shows CHAPTER 3 RESULT: XP bar (frozen worksheet first-check XP + Bea's case XP, of 160), the Worth the Tax? medal at its saved tier, Coins and Public Trust tiles from the Level_03 ledger with wallet/Trust, worksheet first check (e.g. 8/9 · Silver or a legacy 11/12), computation review XP, finding fact. Two buttons: TO CAREER (the scene's button) and STAY IN OFFICE (opt-in second choice). Either is the explicit acknowledgment.
6. Acknowledgment saves CompletedOffice, `Level_03/chapter-wrap-up`, Level_03 completion and one aggregated mastery record in one signed write, then the Notes step ticks. TO CAREER leaves for the career board; STAY IN OFFICE closes the card and Arthur says one line (look around, supply counter, Settings returns to Career). A failed write keeps the card as NOT SAVED YET with TRY AGAIN.

### Changed paths

- New `Assets/Scripts/Level3/Level3ClosingRules.cs`(+`.meta`): `StageOf` (Finding/Message/Result/Completed, Bea-only: delivered, Bea active, company not admitted), `RecordFinding`, `ReadMessage`, `Acknowledge`, `Result` (frozen v1/v2 worksheet + approved case XP, clamped, unknown never invented), `ApplyWrapUp` (activity, chapter completion, `MasteryBoundary.ApplyRun`; AlreadyDone when completed; no coins/Trust), `DraftOverstatement`/`UnsupportedDraftLines` from the case file (exact decimal).
- `Level3ChapterRules.cs`: closing triggers (FindingRecorded reused, ChapterMessageRead, ChapterWrapUpConfirmed) in `ImpliedTriggers`, closing lessons in `ImpliedLessons`. `Level3Checkpointer.cs`: `CommitFinding`, `CommitMessage`, `CommitAcknowledgment`, `EnterCompletedOffice`; implements `ReceptionistInteractable.ILevelMessage` (registered while enabled); `TickWrapUp` shows the pending card once; publish events; closing flags; phase-aware resume line. `PlayerProgressManager.cs`: `CommitLevelThreeWrapUp` (story only, one candidate).
- Shared, opt-in: `ReceptionistInteractable.cs` (`ILevelMessage`/`LevelMessage`; null in Levels 1/2), `HudRewardScreen.cs` (`RewardScreenData.SecondaryLabel/Secondary`; unset cards keep one centred button), `LevelCompleteUI.cs` (`ShowLevelThreeChapter`, `ContinueLevelThree`, Chapter 3 card; L1/L2 branches unchanged), `GameplayEvents.cs`/`ObjectiveTriggers.cs`/`OfficeObjectives.cs` (two additive events).
- `Level3BeaInteractable.cs` (closing talk, finding card, review records), `Level3TownContent.cs` + `town_life.json` (finding lines, `reception.quaxMessage`, validation, built-in mirrors), `objectives.json` (+3 steps, 14 total), `guide.json` (closing-finding, closing-message hints).
- Tests: `Tests/Level3Regression/Program.cs` (T14 section, 74 checks; two objective-count pins 11 → 14), `Tests/VerifyLevel3Print.py` (finding card/second button fit).

### Commands actually run

- Baseline before edits, `python3 -B Tests/run_level3_repair.py`: runtime 0 errors/21 inherited warnings, Editor 0/10, Level3 **2119/2119**, controls 5734.
- First production build (`--build-only`): 0 errors, 21/10 warnings (none in changed files). Existing suite: 2118/2120, the two superseded objective-count pins (11 steps); updated to 14.
- T14 section first run: Level3 **2194/2194** (T14 **74**). Mutation probes with full rebuilds: (1) unsupported statements accepted + completion guard removed → 6 failures; (2) finding no longer required for the message + Career exit before the acknowledgment save → 28 failures. Sources restored and confirmed by shasum (Level3ClosingRules 970cb85d…, LevelCompleteUI 235db062…).
- Final `python3 -B Tests/run_level3_repair.py --level2` (after CRLF normalisation of the new script): runtime **0 errors/21 inherited warnings**, Editor **0/10**, Level3 **2194/2194** (T14 74, T10 41, T13 37), controls **5734**, Level2 **1580** (3 native EventSystem comparer checks skipped as before).
- Other suites through the same helper (shared files changed): GuideRegression **937 PASS**, Level1LivingPlazaRegression **77/77**, IntroCutsceneRegression **44/44**, SettingsRegression **136/138** (the two failures are CRLF pins on untouched `InGameSettings.cs`/`SettingsBoard3D.cs`, pre-existing); AudioRegression and CampaignFoundationRegression do not compile in this standalone helper (missing `ClientRecordRules.PreserveApparentScale`, `T19Regression`, AudioClip type identity; unrelated, pre-existing) — not counted.
- `python3 -B Tests/VerifyHudRewardPresentation.py`: PASS. `python3 -B Tests/VerifyLevel3Scene.py`: PASS, same 3 inherited warnings, scene hash unchanged. `python3 -B Tests/VerifyLevel3Print.py`: first measurement showed the supported statement at 30 pt was 1062/884 units (would auto-shrink); shortened, then **269 PASS** (font geometry only). `verify_content.py`: 344 PASS. After these docs, `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py`: **PASS** (18 tickets; 11 implemented/manual-pending, 1 in progress, 4 planned, 2 deferred; 239 links; 122 source references).
- T14 checks drive the real rules and the signed `SaveFileStore`: prerequisites at approved/at-Auditor/carried/explained/fake-delivered; six unsupported statements; missing B-DRAFT; other revision/active case; duplicates of every command; v2 8/9 59 XP, legacy v1 11/12 59 XP, unknown grade (0, no badge), clamped values; wrap-up once (completion, activity, mastery best/badge tier kept, no coins/Trust/ledger/claims/possessions/Rosa/company change), repeat AlreadyDone, better earlier best/tier preserved; run modes; failure/retry/reload at finding, message and acknowledgment with disk bytes unchanged on failure; completed office refuses hand-in, copy, edits and clock; T10 save resumes at the question; triggers/lessons; content figures from case_bea_001; wording limits; call-site order. NPC dialogue, the docked card, the result card layout, camera/touch/audio and the reader opening are source wiring only.

### USER manual script (Unity/device; not run here)

1. From a T10 save with Bea's copy delivered: talk to Bea. One question, the three statements and Review her records beside the dialogue; nothing overlaps; dialogue taps do not answer it.
2. Pick "on purpose", then "all wrong": one reply each, same question, Trust unchanged. Tap Review her records: the talk ends, her papers open read-only (find DRAFT-B26, PARTY-26, PRINT-26); close; talk again: same question. Try Talk later and Back.
3. Pick the supported statement: her reply and the receptionist pointer; Notes ticks "Tell Bea what her draft shows." Reload: her next talk is her one short line; the receptionist shows as available.
4. Talk to the receptionist: Quax's message (three lines, your one reply). Cancel midway once (Back): talk again, it replays. Let it finish: after the camera returns the CHAPTER 3 RESULT card appears once, no second card or guide line on top.
5. With the card up: kill/reload the app: the card returns, Notes shows the message step done; wallet/Trust unchanged since delivery.
6. Simulate a save failure if possible (or check the TRY AGAIN state wording): the card stays; retry works. Press TO CAREER: the career board shows Chapter 3 completed; XP grew once; coins/Trust unchanged. Relaunching Chapter 3 from Career is a replay (T16).
7. On another save, press STAY IN OFFICE: the card closes, Arthur's one line, controls return; Bea and the Auditor offer only their after-lines, no clock chip, no copy in the bar; the supply counter works; Settings → Career leaves normally. Reload: welcome-back line, no card, no reward.
8. Check mouse vs touch, both card buttons' readability and spacing on phone, sound off, Settings/focus loss during each conversation, and that wording never alleges intent or other files.

Next numbered work: **T15** (functional computer and optional office), with T13's L3-R106 decision still open. T15–T18 are not implemented.

## 5 November 2026 — T13: carried equipment, inventory continuity and source pins (in progress)

Status: **In progress — four of five internal checks verified; one base-capacity design decision open (L3-R106).** No Git, Unity/editor launch, real-save access, scene/prefab/package edits. T10 was re-verified in the same build (its 41 checks pass).

### Traced (existing behavior, kept)

- Chapter changes never delete belongings (`Level2CampaignRules.RetireOtherChapters` is a no-op; scope only sets eligibility; `ResetsOnFreshAttempt` false for Level 3). Bea's acquired papers are kept as read-only Case Folder pages by `HudHotbar.KeepVisibleReferences`; the summary/certificate keep their own pages.
- Pins are item ids on the existing three ribbons; `OnPinTab` → `HudHotbar.OpenViewer` → Bea's certificate/summary papers, earlier chapters' records as read-only saved pages; a record with no saved copy shows the existing "did not include a readable paper copy" text. `Normalize` drops pins to records no longer owned.
- `Level3ValueTransfer.TryRead` refuses float/text, foreign-case, other-revision, malformed and not-held values. `VoucherUseCard` checks status and builds Bea's extended chapter before `CommitLevelThreeVoucherUse` (shared `CommitVoucher`: candidate → one signed write → publish). Decor: `DecorRules.Resolve`; the stand and computer are `pending:` teasers.

### Fixed

- `Level3CertificateRules.HasRoom`/`RefuseFull` and `Level3Checkpointer.CommitCertificate`: with no free bar/Bag slot the certificate pickup used to reach the signed write and report "Couldn't save. Bea still has her certificate". It now refuses before writing with a plain full-Bag line; the paper stays beside Bea and the Bag is unchanged.
- Reverted in the same session (not shipped): holding the certificate in the inventory's never-stuck hold when full. The T08 checks show the credit is dragged from the certificate's own Bag paper, so a slotless certificate would block stage C.

### Open (needs a user decision) — L3-R106

At base capacity (3 + 6) a player who kept every Chapter 1/2 record (notes, folder, profile binder, Arthur's file, Rosa's marked records), the postcard, the bookmark set and a stack of two vouchers, after returning the desk book for the summary, has 10 items with no Level 3 exit for 9 slots when Bea's certificate arrives. Level 3 has no keepsake stand, required records cannot be released, and one voucher stays after Bea's single use. A Satchel, a lone voucher used on Bea, or not owning a keepsake avoids it. Options: (a) let Level 3's Make room keep an earlier chapter's reference record or a keepsake in the existing owned hold, (b) give Bea's certificate a slot-free reader (e.g. from the Tax and payment leaf), (c) accept a purchase/voucher dependency for this edge (conflicts with REWARDS_AND_VALIDATION "Base capacity must support all required work").

### Commands actually run

- `python3 -B Tests/run_level3_repair.py`: runtime **0 errors/21 inherited warnings**, Editor **0/10**; first T13 run 2118/2119 (test fixture: the shop needs `shopIntroduced`; fixed in the test), then **Level3 2119/2119** (T13 **37**, T10 41), controls **5734**. Mutation probe (`HasRoom` always true) failed exactly the full-Bag check (2118/2119); source restored by shasum and rebuilt: final **2119/2119**, controls 5734. Level2 suite not rerun: T13 changed no shared code (only `Level3CertificateRules.cs`, `Level3Checkpointer.cs`, tests).
- After these docs: `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py` **PASS** (18 tickets; 10 implemented, 1 in progress; 239 links; 122 source references); `verify_content.py` **344 PASS**.

### USER manual script (Unity/device; not run here)

1. Continue a Chapter 2 save with several kept items and pins: open the Bag at reception, Bea's table, the desk and the conference room; slots, counts, ribbons and capacity are unchanged; ribbons open the right paper; earlier records read only.
2. With the bookmark set, pin Bea's summary/certificate (replace asks which ribbon), reload, unpin.
3. Fill the bar/Bag, then take Bea's certificate: one "bar and Bag are full" line, the paper stays; free a slot and take it.
4. Drag a value from Rosa's marked records or Arthur's file toward Bea's sheet: refused with a plain line.
5. With Bea's clock running, use a voucher (+180 s once); reload; a second use is refused; after approval it is refused.
6. With a pencil cup/print owned: they appear once in place, clear of the folder, stamps, calculator and seat (the Bea calculator is built after decor resolves, L3-R107).

Next: decide L3-R106, then finish T13; T14 stays after it.

## 5 November 2026 — T10: explain Bea's balance and deliver her approved copy

Status: **Implemented, internally verified; USER Unity/device acceptance pending.** No Git, Unity/editor launch or automation, real-save access, scene/prefab/package edits. One new script (`Level3DeliveryRules.cs` + unique `.meta` GUID 2b20c0ab…); no new asset family, manager or framework.

### Traced before editing

- T09 baseline kept: Auditor `TryContinueApproval` + `ClientCopyHandover.SpawnAuditorCopy` drop, `Level3ReviewRules.CollectCopy` saved before flight (atAuditor → carried), quiet restore, `Level3CaseFolder` read-only/CLIENT COPY mark and icon tag. Chapter transitions allowed only BeaDelivery → CompanyAppointment; `ClientCopyHandover` exists only in Levels 1/2, so the folder icon tap/drag had no Level 3 client owner. Bea's BeaDelivery talk fell through to her source-question menu.
- Source pins forbid trust/coins in `Level3ReviewRules`/`Level3Audit`/`Level3BeaInteractable`, so the claim lives in a new pure rules file.

### What the player now does

1. Collect the copy from the Auditor (unchanged T09 pickup). Objectives add “Collect Bea's copy from the Auditor.” and “Answer Bea's question and give her the copy.”
2. Talk to Bea at her existing conference-room spot. Copy still with the Auditor: one waiting line. Carried: one question (15 words) on an awaited line; the approved copy's Tax due / Tax credit (withheld) / Remaining payable rows with their frozen figures dock beside the dialogue (existing `DockChoices` card; cream paper rows, fixed 30/34 pt, Talk later; no auto-size, no IDs, no right/wrong flash).
3. Tax due or Tax credit: the player says the figure, Bea gives one contextual sentence that waits again on the same question. Nothing is saved; no Trust change, recap or reveal.
4. Remaining payable: saved first (`CommitBalanceExplanation`), then her short reply and the handover line waiting on the existing `client-copy` action. The HUD peek shows the folder icon; tap it or drag the folder onto Bea. A drop on the icon, elsewhere or on anyone else returns the copy. Dialogue taps cannot advance either awaited line.
5. `CommitDelivery`: copy done + BeaDelivery → ClosingFinding (Bea stays active, company never admitted) + once-only +4 Public Trust (`Level_03/bea-follow-through`, “Beatriz Mendoza: follow-through”) in one signed write → publish (objective tick) → icon empties, paper sound, heart emote, Trust toast/HUD peek, her one thank-you. Later talks: one after-delivery line. Chapter completion/finding/wrap-up stay T14.

### Changed paths

- New `Assets/Scripts/Level3/Level3DeliveryRules.cs`(+`.meta`): `StageOf`, `Figure`, `Explain` (only Payable; validates phase/case/revision/approval/frozen figure/carried copy/action id), `Deliver` (requires the saved explanation and action id), `ApplyDeliveryGrants` (+4 once, no coins). An unsupported `done` without the delivery action is never treated as delivered.
- `Level3ChapterRules.cs`: BeaDelivery → ClosingFinding allowed; `ExpectedActiveCase(phase, companyAdmitted)` keeps Bea active in closing phases when the company was never admitted (the deferred company route is unchanged); implied pickup/delivery triggers. `Level3Checkpointer.cs`: `CommitBalanceExplanation`, `CommitDelivery`, publish raises `ClientCopyHandedOver`, guide flags `beaCopyCarried`/`beaBalanceExplained`/`beaCopyDelivered`. `PlayerProgressManager.cs`: `CommitLevelThreeDelivery` (story runs only). `CampaignRewards.cs`: `BeaFollowThrough` +4 trust. `PlayerProgressData.cs`: additive `Level3CaseProgress.balanceExplained` (older saves restore false).
- `ClientCopyHandover.cs`: opt-in `ILevelDelivery`/`LevelDelivery` included in `AwaitingHandoverAction`/`AwaitingClientCopy`/`ClientDisplayName`, reset on subsystem registration. `OfficeHudRuntime.cs`: folder icon tap and drop resolver also ask it. Levels 1/2 never set it.
- `Level3BeaInteractable.cs`: delivery talk, docked copy figures, `PointTo`, `ILevelDelivery`, `DeliverCopy`/`PlayDelivered` (toast only when a real grant changed). `Level3CaseFolder.cs`: a delivered copy is not put back in the bar on resume (as Levels 1/2). `Level3TownContent.cs` + `town_life.json`: authored lines with built-in fallbacks and validation. `objectives.json`: two steps.
- Tests: `Tests/Level3Regression/Program.cs` (T10 section, 41 checks; objective-count pin 9 → 11), `Tests/VerifyLevel3Print.py` (copy card fit).

### Commands actually run

- Baseline after first edits, `python3 -B Tests/run_level3_repair.py`: runtime 0 errors/21 inherited warnings, Editor 0/10, Level3 2040/2041 — the one failure was the superseded objective-count pin, updated.
- First T10 run: 2081/2082 — caught a real bug: a default `CampaignRewards.Change` reads as Applied, so a non-story delivery would have shown an empty Trust toast. Fixed in `PlayDelivered`; test tightened.
- Final `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors/21 inherited warnings**, Editor **0/10**, Level3 **2082/2082** (T10 **41**), controls **5734**, Level2 **1580** (3 native EventSystem comparer checks skipped as before). Mutation probe (explanation gate removed) failed exactly the gate check (2081/2082); source restored by shasum and rebuilt: final `python3 -B Tests/run_level3_repair.py` Level3 **2082/2082**, controls 5734.
- `python3 -B Tests/VerifyLevel3Scene.py`: PASS, same 3 inherited warnings, scene hash unchanged. `python3 -B Tests/VerifyLevel3Print.py`: **263 PASS** (font geometry only). `verify_content.py`: 344 PASS. `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py` after these docs: first FAIL (non-canonical T10 status wording), corrected, then **PASS** (18 tickets, 10 implemented/manual-pending, 239 links, 122 source references, 17 fixtures, 344 content assertions).
- T10 checks drive the real rules and signed `SaveFileStore`: prerequisites; six wrong/unknown figures unchanged; frozen 62,500/12,500/50,000; explain-before-deliver; wrong case/revision refused; duplicates; ClosingFinding with Bea active and no company admission; fake `done` refused; +4 once with no coins/re-approval/chapter completion; run-mode isolation; failure/retry/reload at atAuditor, carried, explained and delivered with inventory/Rosa/stamp/figures preserved; older save without the field; objectives/implied triggers; content budgets; call-site ordering. Icon tap/drag/raycast, docking, camera/HUD peek and rendering are source wiring only.

### USER manual script (Unity/device; not run here)

1. From an approved T09 save: collect the copy at the Auditor once; reload; it stays carried with the COPY tag and read-only pages, no second 100 coins.
2. Walk to Bea in the conference room. Her question shows with the three copy rows beside the dialogue; rows, amounts and the dialogue do not overlap; tapping the dialogue does nothing.
3. Tap Tax due, then Tax credit: each gives one sentence and the same question; Trust unchanged. Tap Talk later, talk again: question again, copy still carried.
4. Tap Remaining payable: her reply, then “May I keep this copy…” waiting. Press Back / open Settings / switch apps / reload here: the next talk goes straight to the handover line.
5. Drag the folder to empty floor, to the icon, then onto Bea; separately try the icon tap. Only Bea/icon-tap deliver; others return the folder smoothly.
6. After delivery: icon empties, heart, +4 Trust toast once, her thanks; camera/input released. Reload: no copy, no extra Trust, Bea says her after-delivery line, the Auditor offers nothing new; no chapter-complete screen (T14). Check sound off, mouse vs touch, phone readability.

Next numbered work: **T13** (T11/T12 deferred).

## 5 November 2026 — T09 user follow-up: foreground case stamps, persistent ink and shared Auditor copy drop

Status: **Implemented/internal-verified; USER rendered/device acceptance pending.** The user requests stamps above the folder, a whole-case READY/NOT READY impression retained outside stamping/desk mode, and the existing Levels1/2 Auditor client-copy drop during dialogue. This follow-up implements that pickup seam, not the remainder of T10's Bea explanation/delivery.

- Reproduction: four new source contracts failed against the prior implementation (2028/2032), covering pre-activation ink rendering, block-matching case verdict text, foreground reservation and shared approval handover. This is deterministic source evidence; no Unity screenshot reproduction was run.
- `Assets/Scripts/Level2/Level2DeskStamps.cs`: shared tools reserve foreground for their entire lifted/press/return lifetime through existing Doc3DLayer.KeepInFront, and reassert order after the document compositor (execution600 vs500). The existing release path destroys the reservation when tools finish returning. A bound case now turns to the packet's front before stamping; Level2's initial/final page rules remain unchanged.
- `Assets/Scripts/Level3/Level3CaseFolder.cs`: READY/NOT READY matches the stamp blocks; same ReviewInkMark, colours, dimensions, free-position placement and saved revision. Saved ink no longer depends on IsShowing during PageRendered: CaseFolderUI deliberately renders before activating its panel, which previously erased the impression on reopening (including away from the desk). Dirty/text invalidation refreshes the existing compositor. Stale ink fades as in Level2, retaining the original verdict rather than replacing it with instructions. Existing stamps retain saved page/position; new case stamps land on the front. Read-only collected copies reuse ClientCopyHandover.DecorateCopyPage and ClientCopyStamp.BuildTag; original field read-only state is restored on disable.
- `Assets/Scripts/UI/HUD/ClientCopyHandover.cs`: extracted its existing SpawnPickup geometry/model/placement into shared SpawnAuditorCopy, called by both the unchanged Levels1/2 flow and Level3. No replacement folder, camera, dialogue or flight system. The original cover stamp, speaker-relative placement, 15-degree facing, ClientCopyPickup/WorldPickupMotion drop/hover/flight remain the owners; an optional existing save-before-flight callback is forwarded for Level3.
- `Assets/Scripts/Level3/Level3Audit.cs`: Approve continues AuditorInteractable.TryContinueApproval with two short lines; its existing admitted-line-index callback drops the actual client folder on the handover line while conversation is still active. Cancellation uses the existing DropNow cleanup. The working folder leaves the store; collection returns it as the read-only copy. Saved atAuditor restores quietly, without replayed dialogue/drop/rewards. Missing presentation dependencies retry without silently awarding/collecting.
- `Assets/Scripts/Level3/Level3ReviewRules.cs`: CollectCopy validates BeaDelivery, active case, approved review and atAuditor; changes only copyPhase to carried, duplicates harmless. Existing Level3Checkpointer.CommitComputation performs candidate -> signed save -> publish before ClientCopyPickup starts flight. Failed write retains world pickup; no coins/Trust/XP/delivery/chapter reward.
- `Assets/Resources/Tutorials/Level_03/guide.json`: concise case-front stamp instruction, no computation-only label or filing claim. `Tests/Level3Regression/Program.cs`: wiring/reopen plus actual candidate/refusal/duplicate/signed-store failure/retry/reload/possession-preservation tests.

Verification: `python3 -B Tests/run_level3_repair.py --level2` compiled current runtime **0 errors/21 inherited warnings** and complete Editor **0/10**; **2039/2039 Level3**, **5734 controls**, **1580 Level2** passed (3 native EventSystem comparer checks explicitly skipped). Final `python3 -B Tests/run_level3_repair.py --suites` after the copy-presentation assertion: **2040/2040 Level3** and **5734 controls PASS** against that compiled runtime. `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py`: PASS,18 tickets/239 links/122 references/17 arithmetic fixtures/344 content assertions. No Git, Unity/editor launch/automation, scene/prefab/new asset edits or real-save access.

USER: restart Play after import. From any computation page open stamps (front page appears), drag READY and NOT READY across the folder edges, press and return: tool stays above the folder and ghost matches ink. Put stamps away, close/reopen from the icon, leave desk and reopen; verdict/position persist. Change work: stale impression fades, restamp restores current ink. Approve a supported case: after Summary, the existing conversation continues, copy drops on the handover line before the conversation ends; collect once, flight to folder icon, read-only copy and COPY tag. Cancel/reload before drop/after pickup: one copy in the saved place, no repeated100 coins. Check mouse/touch/Back/Settings/focus, sound off and small-screen readability. T09 manual boxes remain pending; T10 explanation/delivery remains next.

## 5 November 2026 — T09: shared stamps on the Case Folder, one-line Auditor returns, the marked line

Status: **Implemented, internally verified; USER Unity/device acceptance pending.** No Unity/editor launch or automation, real-save access, scene/prefab/package/new-asset edits. One read-only `git diff --stat` on review.json was run by mistake during this work; nothing else touched Git, and no Git state was changed.

### What was already wired (reconciled, kept)

- Stamping already ran through the shared Level2DeskStamps/Level2StampDrag (stand models, lift, free-position ghost, press, ink, return) bound by `Level3CaseFolder` as `Level2DeskStamps.ICaseStamp`: Available only in Bea's open computation after the taught marker, Revision = her computation revision, `Commit` saves `Level3ReviewRules.StampAt` through `CommitComputation` before ink, out-of-date ink reads OUT OF DATE / STAMP AGAIN, `Opening` puts the calculator away. Legacy filing/archive/completion transitions stay refused by `Level3ChapterRules.TransitionAllowed`.
- Talk-first hotbar-folder hand-in (`Level3Audit` as ILevelHandIn/ILevelFolder), `CommitReview` candidate -> signed write (approval with its 100 coins and mastery claim) -> publish, earliest root, charge once, retries, 15 XP cap, refusals (no stamp, closed window, unchanged hand-in, approved), frozen figures, copyAtAuditor, BeaDelivery, clock stop. Clock/voucher adapter (`Level3ReviewClock`) unchanged.

### Changed

- `Assets/Scripts/Level3/Level3DeskCalculatorReview.cs`, `Level3DeskCalculator.cs`, `Level3DeskCalculatorComputation.cs`: removed the L3 procedural READY/NOT READY bodies, their stand-model hiding, tap-stamp-box press and lift/pose code (never built in the live game any more; `BuildStamps` had no caller). Cue names and the cutoff's `Busy` remain. One stamp set, one input owner.
- `Assets/Scripts/Level3/Level3ComputationRules.cs` (`Evaluate`): on the fixed leaves (no Gross/Expenses field ever placed, taxable tape not field-linked) the collected summary is the input, so the review judges the taxable tape itself. Before: an early Ready or Income + Deductions was returned as "Gross income is still empty", a field the page no longer has.
- `Assets/Scripts/Level3/Level3ReviewRules.cs`: `Line` (one problem + next action, at most 24 words, no value; stamp roots always use their own line), `VisibleField`/`VisibleLabel` (legacy gross/expenses/working roots point at the visible Taxable income / Tax due), `MarkedField`/`MarkedLine`; decisions carry `Line`; Submit records `lastKind`. Built-in notes reworded for the summary/fixed page.
- `Assets/Scripts/RewardsSystem/PlayerProgressData.cs`: additive `Level3ReviewRecord.lastKind` (copy/repair; old saves restore with "").
- `Assets/Resources/Levels/Level_03/review.json`: every root gains a spoken `line`; four notes reworded (taxable inputs from the summary, threshold wording). `Assets/Resources/Tutorials/Level_03/guide.json`: returned hint shortened to point to the mark.
- `Assets/Scripts/Level3/Level3Audit.cs`: one Auditor line per hand-in (approval line, or the short line; a justified hold starts "Good hold."). The approval no longer reads all four figures aloud (they remain on the card). Audit Summary for a return/hold: "Marked: <field>", the same short line, WHY + the longer note, footer from the authored returned/hold/falseHold/staleStamp line.
- `Assets/Scripts/Level3/Level3ComputationPage.cs`: the leaf holding the Auditor's mark outlines that field in red pen, tags its caption " · Auditor's mark" (Recalculate wins when stale) and shows "Auditor: <line>" in the leaf's one note when no routine status is showing; a stamp-only return shows its note on the Computation leaf. Redraws on each decided hand-in.
- Tests: `Tests/Level3Regression/Program.cs` (new "Revised T09" section, 68 checks; two old pins of the retired procedural stamps replaced by shared-path pins), `Tests/VerifyLevel3Print.py` (tagged captions and the leaf note fit). Docs: T09 ticket, TICKETS, README, AGENTS, this entry, KNOWN_ISSUES L3-R94–L3-R99.

### Observed verification

- Baseline before edits, `python3 -B Tests/run_level3_repair.py`: runtime 0 errors/21 inherited warnings, Editor 0/10, Level3 **1960/1960**, controls 5734.
- Final `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors/21 inherited warnings**, complete Editor **0 errors/10 inherited warnings**, Level3 **2028/2028** (Revised T09 **68**), controls **5734** (suite compile 8 warnings), Level2 **1580** with the same 3 native EventSystem comparer skips.
- The new checks drive the real commands on the actual fixed-leaf manual work: approval of 600,000/62,500/12,500/50,000 with full XP, copy atAuditor, BeaDelivery, 100 coins once and no Trust; a wrong band returned at Annual band, charged once, unchanged hand-in refused; a justified hold thanked and uncharged; a false hold; a stale stamp after a credit change; early Ready and Income + Deductions on the fieldless page; every spoken line <= 24 words with no value or section code; mark/line survive copy, serialization and a save without lastKind; the clock starts after the first lever filing, once. One new check caught a real bug before the final run (a stale stamp was given the generic "needs recalculating" wording); fixed in `Line`.
- Mutation probe: restoring the old `if (directSummary)` in Evaluate made exactly the two fieldless-review checks fail (2026/2028); the production file was restored and confirmed by shasum. (A first probe with `--suites` reused the earlier runtime build and proved nothing; it was repeated with a full rebuild.)
- `python3 -B Tests/VerifyLevel3Print.py`: **253 PASS** (font geometry only). `python3 -B Tests/VerifyLevel3Scene.py`: PASS, same 3 inherited warnings, scene hash unchanged. `verify_content.py`: 344 PASS. `verify_pack.py`: run separately.

### USER manual acceptance

Import/restart Play from the T08 state (tax due, credit and remaining payable attached). At the desk with the calculator up, open the Case Folder and lift the stamps: the calculator goes down; drag READY, see the ghost, release on a page; ink lands there after the save. Hand the carried folder to the Auditor. Try: wrong band then Ready (one short line, summary card with Marked: Annual band and WHY, red outline and note on the Computation leaf; fix the band, recalculate, stamp, hand in again); Not Ready with an open line (Good hold); Not Ready on a correct sheet; stamp then change the credit; the same sheet twice; a correct sheet (one approval line, card figures, coins once, copy waits). Check sound on/off, Back/Settings/focus during stamp drag and the hand-in, phone readability of the outline/tag/note, and the clock chip/warnings/next-morning. Next numbered work: **T10**.

## 5 November 2026 — User-authorized Level3 Career entry for the test APK

Status: **Entry implemented/internal-verified; APK/manual acceptance pending.** This is the user-requested entry portion of T18 before the chapter is finished, not full T18 release completion. The explicit user instruction overrides the pack’s earlier Coming Soon gate for this playtest. No Git, Unity/editor launch or APK build/device execution, actual-save access, scene/prefab/package edits, checkpoint reset or production readiness claim.

### Live path and changes

- `Assets/Resources/Career/chapters.json`: Chapter3 now has title The Numbers Tell a Story, note Bea’s records, Office_Level3 scene, cases1, comingSoon=false and campaignPending=false. Normal CareerProgress rules still lock it until Chapter2’s saved story completion (including Rosa follow-through/delivery), independently of mastery score, Trust or coins. Chapters4–5 stay Coming Soon. No extra badge/release-completion work was silently marked done.
- `Assets/Scripts/RewardsSystem/CareerProgressService.cs`: built-in fallback likewise routes Chapter3 to Office_Level3, so resource fallback cannot silently turn it back into Coming Soon. Existing CareerBoardPresenter launches its scene through CampaignRunMode.Story; Level3ChapterRules/PlayerProgressManager already save/resume this chapter for completedL2 campaigns. No new launch/save framework or skip introduced.
- `Assets/Scripts/UI/LevelCompleteUI.cs`: Chapter2 footer now says “Return to Career to continue your story.” instead of claiming future chapters are all Coming Soon. Existing completion/delivery gates unchanged.
- `Assets/Resources/Levels/Level_03/level.json`: updated only the historical _readme gate note; active caseIds/features unchanged. Office_Level3 was already enabled in ProjectSettings/EditorBuildSettings.asset and already self-bootstraps Level3OfficeStart; no scene list or scene edit needed.
- Tests: `Tests/Level3Regression/Program.cs`, `Tests/Level2Regression/Program.cs`, `Tests/CareerBoardRegression/Program.cs`, `Tests/VerifyLevel3Scene.py`. Updated superseded Coming Soon/message pins and added real Career projection checks with authored and fallback catalogues.
- Docs: README/AGENTS/TICKETS/T18 ticket explicitly record the playtest exception while keeping full release status Planned; PROGRESS/KNOWN_ISSUES. Existing unfinished gameplay, content authority and device acceptance remain outstanding.

### Observed verification

- `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors/21 inherited warnings**, complete Editor **0 errors/10 inherited warnings**, **1960/1960 Level3**, **5734 controls**, **1580 Level2** with **3 native EventSystem comparer skips**. New entry checks cover locked-before-completion, available-after-completion even without mastery records/with0 Trust/0 coins, actual Story save eligibility, no save/inventory mutation by Career projection, fallback parity, unfinished checkpoint resume, Chapters4–5 still unavailable and enabled scene route. The first new no-mutation check incorrectly assumed Normalize left a null checkpoint instead of its empty non-IsSet object; corrected that test expectation, preserving byte-equivalent full save comparison.
- Existing out-of-tree suite helper, `suite('CareerBoardRegression')` loaded via runpy from `Tests/run_level3_repair.py`: **72/72 PASS**. Existing synthetic Coming Soon scenarios remain covered; shipped catalogue pins now expect Chapter3’s route and4–5 Coming Soon.
- `python3 -B Tests/VerifyLevel3Scene.py`: **PASS**, with **3 inherited warnings** (missing CaseDropDestination script also present in Level2; two prefab-instance placements not provable from scene text). Initial failure was solely the old Coming Soon pin, updated to the authorized route. Scene hash **3e8d29dfd93c2efc…** remains unchanged.
- `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py`: documentation validation run separately. No normal-menu click, scene rendering, APK compilation/installation or native/device performance tested here.

### User test path

Export the updated APK using the existing build scene list (Office_Level3 is already enabled). Complete Level2 including Rosa’s copy delivery, return to Career, select Chapter3/The Numbers Tell a Story, and start it. Existing completedL2 campaigns can select it without restarting the story. Continue launches the normal Career board; unfinished Level3 can be reopened with its saved checkpoint and all campaign possessions retained. This is ordinary Story play in the APK, not an Editor fixture or mastery replay. Android installation/save-upgrade/menu/scene transitions remain USER acceptance. Full T18 remains Planned for release work; continue unfinished gameplay work at the existing ticket sequence.

## 5 November 2026 — Certificate focuses on initial touch

Implemented/internal-verified; USER native layering acceptance pending. Certificate holder's PointerDown was empty: body clicks could focus only on release, and amount-band clicks select a value without invoking BroughtToFront at all. A pre-edit assertion reproduced the missing foreground call. `Assets/Scripts/Level3/Level3CertificatePaper.cs` now raises its existing ClientRecordPaper/Doc3DLayer immediately on left-pointer-down. Both the clear paper face and Level3AmountSource bands already forward there. A press that changed its order is focus-only, so it does not also flip the newly exposed page; later taps retain turning. No permanent KeepInFront pin, so touching the calculator/folder can still focus those tools.

`Tests/Level3Regression/Program.cs` adds two routing/gesture contracts. Verification command: `python3 -B Tests/run_level3_repair.py` (runtime + complete Editor + Level3 + controls). Final: runtime0 errors/21 inherited warnings, complete Editor0/10, **1953/1953 Level3**, **5734 controls** passed. No shared source, scene/prefab, save or Git changes. USER: place certificate behind calculator/folder, touch its visible body and each amount band; it should come forward on touch-down, allow value drag, avoid an accidental first-focus flip, and yield focus when another tool is touched. Native mouse/touch/composited rendering not run here.

## 5 November 2026 — Bea speaks before dropping her certificate

Implemented/internal-verified; USER timing/animation acceptance pending. User requested at least one dialogue before the drop. `Assets/Scripts/Level3/Level3BeaInteractable.StartHandover` now inserts “Thanks for helping me work through the figures.” before the authored certificate line and sets the handover index afterward. Clicking Bea starts only this opening line; completing/revealing it does not drop a paper. Advancing to the next handover line triggers the same existing ClientCopyPickup animation once, followed by the closing line and post-talk collection.

`Tests/Level3Regression/Program.cs` now drives an actual three-line DialogueSession and verifies zero drops on admission/first-line callbacks, one on advancement to the handover line, and no repeat on reveal/closing. `python3 -B Tests/run_level3_repair.py`: runtime0 errors/21 inherited warnings, complete Editor0/10; **1951/1951 Level3**, **5734 controls** pass. Shared pickup, inventory and page layout were not changed, so Level2 was not rerun for this timing-only follow-up. Updated active T08/OFFICE_CASE_PLAY. No Unity/editor automation, real saves or Git commands. User should check first-line advance → handover animation → closing line → collectible paper in Play Mode.

## 5 November 2026 — Bea drop callback root-cause repair; actual Level1 Auditor pickup

Status: **Implemented, internally verified; USER rendered/Play Mode/device acceptance pending.** User reports no Bea drop after the prior implementation and requests Tax due restored to page3. No Git, Unity/editor launch/automation, real-save access, scene/prefab/package/new asset edits.

### Reproduced cause and fix

`DialogueSession.TryStart` copies every authored DialogueLine. The earlier Bea callback compared the admitted copied line with the authored `certificateDropLine` through ReferenceEquals; it could never match. The normal typed conversation therefore never saved the offer or created a pickup. Reproduced before editing via the actual DialogueSession plus a regression contract: Level3 **1944/1945**, with exactly the handover-identity check failing. This is an internal deterministic reproduction, not native visual reproduction.

`Assets/Scripts/Level3/Level3BeaInteractable.cs` now follows the Auditor's admitted `CurrentLineIndex` approach: arm the handover index, check it on line-change/reveal and immediately after admission (including non-typewriter starts), consume it before calling presentation so repeated callbacks cannot drop twice. `Level3CertificatePaper.Ensure` lazily creates/reuses the same owner when Bea needs it; `Level3DeskCalculatorComputation.BuildLeaf` reuses that instance instead of making another. Handover is no longer dependent on successful desk-tool initialization.

`Assets/Scripts/CaseFolder/ClientCopyPickup.cs` now directly supplies Bea’s pickup—the exact component used by the Level1 Auditor in the user screenshots. Optional paper-visual factory builds the existing IdCardModel Type8 face/body; no CLIENT COPY stamp on the paper. Optional collect-before-flight callback saves chapter acquisition plus Bag item first; null/failure leaves the paper at rest. Original folder callers use default arguments and retain original cloning/stamp/input/drop/hover/WorldPickupMotion.Fly behavior. No alternate flight implementation. Bea placement uses the same15° facing, side offsets and drop origin as ClientCopyHandover, with the existing paper audio cue.

`Assets/Scripts/Level3/Level3CertificatePaper.cs` uses ClientCopyPickup.Create, retaining saved-offer restore, chapter+inventory collection, Bag/hotbar icon opening/drag ghost, real readable paper, exact reusable desk amount bands and put-away. `Assets/Scripts/SideQuest1/ReviewedFilePickup.cs` is restored to its original Arthur-only API; the prior optional certificate-visual adapter was removed rather than leaving two pickup integrations.

### Page adjustment

`Assets/Scripts/Level3/Level3ComputationPage.cs`: Computation page3 has Taxable income, Annual band, selected threshold/rate/base/formula and **Tax due** (y488). Next Tax and payment leaf has only **Tax credit** (y24) and **Remaining payable** (y172). Existing next/previous animation, native fields, exact sources/destinations, calculator keys and save-first tape flight unchanged. Both leaves have room for neutral status/refusal text below their fields.

Tests: `Tests/Level3Regression/Program.cs` (real dialogue copy/index/one-shot/immediate-line cases, exact pickup wiring/commit ordering and current page-layout pins); `Tests/VerifyLevel3Print.py` (both leaves’ status clearance). Docs: T07/T08, OFFICE_CASE_PLAY, PROGRESS, KNOWN_ISSUES. Prior historical entries describe superseded implementations.

### Actual checks

- Failing reproduction: `python3 -B Tests/run_level3_repair.py --suites` before fix: **1944/1945**, exactly the authored-line identity trigger failure; actual DialogueSession copy behavior confirmed.
- Final `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors/21 inherited warnings**; complete Editor **0 errors/10 inherited warnings**; **1949/1949 Level3**, **5734 controls**, **1580 Level2**, **3 native EventSystem comparer skips**. The six added checks cover admitted cloned-line identity/index, no pre-line/unarmed trigger, exactly once across repeated callbacks and closing line, actual ClientCopyPickup/WorldPickupMotion.Fly with failed-commit flight refusal, and shared lazy reader ownership. Existing full manual arithmetic and certificate offer/collection failure-retry-duplicate-reload checks remain green.
- `python3 -B Tests/VerifyLevel3Print.py`: **237 PASS**, font/field bounds only.
- `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py`: PASS,18 tickets/239 links/122 source references/17 arithmetic fixtures/344 content assertions.
- The previously recorded broader CampaignFoundation suite compilation limitation was not repaired or rerun; no new pass claimed. Native rendering, input dispatch, audio, real pickup placement and animation still require user acceptance.

### USER verification

Import/restart Play. With tax due attached and certificate not yet acquired, talk to Bea: on the handover line the paper should appear beside her before the closing line, using the same motion as the Level1 Auditor. It stays after talk; click to collect once, flight to Bag/hotbar, icon click/drag ghost opens the readable paper, and its amounts transfer to the calculator/folder at the desk. Try typewriter-on and fast reveal/advance, cancel before/after handover, repeated talk/click, focus/settings, full inventory/retry and pending-offer reload. An already-acquired historical certificate stays owned/readable in its icon; it is not duplicated as a new drop. Compare original Level1 Auditor folder pickup and Arthur Reviewed File (default paths). Check page3 Tax due and next-page two fields/arrows on mouse and phone; text/layer rendering and feel remain pending. Next numbered work remains T09 after acceptance.

## 5 November 2026 — Split Tax/payment leaf and Bea's conference-room certificate pickup

Status: **Implemented, internally verified; USER Unity/device acceptance pending.** Latest user request supersedes the prior five-field single leaf and modal Take certificate interaction. No Git commands, Unity/editor launch/automation, scene/prefab/package/new asset edits or actual-save access.

### Changed runtime/data paths and reused flow

- `Assets/Scripts/Level3/Level3ComputationPage.cs`, `Level3CaseFolder.cs`: page3 retains Taxable income, Annual band and revealed rule parts/formula. Added next page Tax and payment (role level3-payment) with Tax due/Tax credit/Remaining payable at24/172/320 units. Same PageBuilder, native printed blue fields, exact source/destination callbacks, page turns and shared computation state. Both leaves refresh after saved transfers/tape flight; no Calculate/Help controls or automatic tool setup.
- `Level3BeaInteractable.cs`, `Level3TownContent.cs`, `Assets/Resources/Levels/Level_03/town_life.json`: when the handover line is revealed, call existing certificate owner’s PresentFrom. A closing line ends the conversation normally; deleted Take menu/TakeCertificate callbacks. No certificate acquisition merely from dialogue. Cancellation before reveal saves no offer; a presented uncollected paper remains after the talk.
- `Assets/Scripts/SideQuest1/ReviewedFilePickup.cs`: optional paper-visual/prompt hook on the existing commit-first pickup; default Arthur behavior unchanged. Certificate uses the Auditor placement geometry and shared WorldPickupMotion drop/hover/FlyFolder path, same durations, reach guards and post-dialogue input gate. It builds the existing IdCardModel Type8 paper instead of a folder. No second motion/input framework.
- `Level3CertificatePaper.cs`: owns saved-offer restore and world pickup, then the existing ClientRecordPaper reader. Bag/hotbar click or drag opens from icon/drop point, front/back/focus/drag/clamp retained. Level3AmountSource attaches both existing Type8 numeric bands; values transfer only at workstation. Drop on Bag/hotbar puts away. Old acquired B-WHT saves quietly/retryably acquire the missing Bag item; no source or ownership reset. Pending offer restore waits for Bea's conference-room placement.
- `Level3CertificateRules.cs`, `Level3Checkpointer.cs`, `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs`: Offer action records presentation separately from Acquired. On pickup, existing HandOver command plus AcquireItem produce chapter + required readable inventory possession in one candidate/write before publish/flight. Full bag or failed write leaves neither newly acquired; duplicate collection does not duplicate items. Scratch/Editor collection uses isolated HUD inventory, not live story.
- `Assets/Scripts/RewardsSystem/CampaignInventory.cs`, `Assets/Resources/Campaign/inventory.json`: bea_certificate, one required item, source B-WHT, display-only readable copies. `HudHotbar.cs`, `InventoryScreen.cs`, `HudIconArt.cs`: Bag/hotbar click/drag-out via existing HudIconDragOpen/HudDragItem and paper-look icon; open-item fade uses actual reader state. Reader/error fallback uses saved references. `Level3CaseFolder.SupportingRecords` no longer builds a duplicate physical B-WHT; ShowSource routes to the actual certificate reader.
- `Assets/Resources/Tutorials/Level_03/guide.json`: certificate hint points to Bag; IDs, source identity and later credit/remaining-payable gates stay. T07 manual threshold/rate/base calculator chains unchanged. Tests: `Tests/Level3Regression/Program.cs`, `Tests/VerifyLevel3Print.py`. Docs: AGENTS/README/OFFICE_CASE_PLAY/T07/T08/PROGRESS/KNOWN_ISSUES.

### Observed verification

- Final `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors/21 inherited warnings**, complete Editor **0 errors/10 inherited warnings**, **1943/1943 Level3**, including **12 certificate-pickup/split-leaf checks**, **5734 controls**, **1580 Level2** with **3 native EventSystem comparer skips**. Full manual taxable/tax due/payable arithmetic remains green. New checks cover offered-not-owned state, duplicate/too-early offer, readable item identity, full inventory refusal, signed-write failure publishing neither chapter acquisition nor item, retry/reload retaining both, duplicate collection and real callback/page/icon routing.
- A new check initially found that InventoryRules.Acquire deliberately does not copy referencePages from metadata. Fixed AcquireItem to use the existing KeepReference API after successful acquisition; final readable-item test passes.
- `python3 -B Tests/VerifyLevel3Print.py`: **237 PASS**, updated next-page final-field clearance. Font geometry only, not rendered acceptance.
- Broader `CampaignFoundationRegression` attempted through existing out-of-tree suite helper: **did not compile/run**. Its Program references missing existing `ClientRecordRules.PreserveApparentScale` (four errors), and this generic helper omits that project's additional `T19Regression.cs` (one additional error). This is not a pass or a failure attributed to the pickup change; no unrelated API/test repair was made. Targeted inventory/pickup persistence checks above passed. Generated logs stay under Temp/L3Repair.
- `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py`: **PASS** (18 tickets,239 links,122 source references,17 arithmetic fixtures,344 content assertions). No Play Mode, native UI/touch, actual placement, animation, audio, save write performance or device acceptance claimed.

### USER manual acceptance

Import/restart Play. Check page3/page4 arrows and page counts; tax/payment fields stay above navigation, redraw after tape attachment and provide exact reusable values. Finish taxable/band arithmetic, attach Tax due on next leaf, then visit Bea. During her first handover line the paper drops/flutters/settles beside her, then talk closes; try tapping during talk (no collection), then click afterward. Confirm flight to item/Bag, one possession and readable certificate. Open by click and drag from hotbar and Bag, move/turn/focus/clamp/put away; transfer both printed values at desk. Try repeat conversations/clicks, Settings/focus interruption, full inventory/retry, and offer-before-pickup reload. Historical acquired saves should gain only the missing readable item. Verify original Auditor/Arthur folder pickups and existing summary still look/behave the same. Portrait/landscape, layered-text rendering, phone reach, audio and motion remain pending. Next numbered work remains T09 after user acceptance.

## 5 November 2026 — Fixed Computation page, appended book-band sources and manual arithmetic

Status: **Implemented, internally verified; USER Unity/device acceptance pending.** Latest user clarification supersedes the earlier A/B/C page replacement, Income/Deductions folder rows and page Calculate flow. Five main fields stay on page3: Taxable income, Annual band, Tax due, Tax credit, Remaining payable. The player opens and positions all tools independently. No Git, Unity/editor launch/automation, actual-save access, scene/prefab/package/new-asset edits.

### Reused owners and concrete changes

- `Assets/Scripts/Level3/Level3ComputationPage.cs`: replaced active-stage rendering/chooser/buttons with the fixed leaf. Main fields stay at y24/172/488/636/784; selected band parts and a formula occupy reserved y320–480. Existing printed blue fields, stacked captions/values, grip, PaperPopIn and real folder page-turn persist. No Calculate, Help, Open summary, collapsed summary or band-menu action. The same controls implement exact sources as well as destinations using existing ValueDragSource/ValueTransferManager. Taxable income/Tax due are reusable current tape sources; Tax credit is a reusable linked source. Rate drag displays a percentage. Stale outputs show Recalculate and cannot supply current operands. Sources clear only owned gestures on canceled pointer/settings/disable/focus/pause/desk exit.
- `Assets/Resources/Levels/Level_03/book.json`: appended one Annual bands – practice leaf with three useful teaching bands (IND-0, IND-15, IND-20); seven original section objects, contents, order and read-only values remain unchanged. The existing full six-band reference/domain rules remain. New chips use existing TaxCodeValueChip/Book renderer/native PaperInkStyle and description layout, not a second menu or book. Short descriptions distinguish no-tax, marginal-rate and base-plus-excess cases.
- `Assets/Scripts/Level3/Level3ValueTransfer.cs`: a band payload carries rule identity/case/year/content revision; ReadBand validates before existing SelectRule commit. The band identity cannot be inserted as a numeric calculator operand. Selected rule parts and current attached tapes use exact Level3TransferValue with narrow source allowlist/current-value/year/revision/staleness checks; rate enters real SelectSourcePercent, money enters SelectSource. Original CanInsertSummary remains for existing callers.
- `Assets/Scripts/Data/DragDrop/TaxCodeValueChip.cs`: opt-in only for the new Level3:AnnualBand key: returns the provenance payload rather than float/enum raw text; owns cleanup on settings/canceled touch/focus/disable/pause. Other book chips retain their original flow.
- `Assets/Scripts/Level3/Level3DeskCalculatorTape.cs`, `Level3DeskToolHitTarget.cs`: same real input path now accepts matching active folder source drags, retaining original summary/tool/tape handlers, display feedback and existing printed strip/flight.
- `Assets/Scripts/Level3/Level3ComputationRules.cs`: a direct worksheet-source taxable tape supplies income/expense facts from its original provenance, rather than requiring removed UI fields or autofilling hidden persisted inputs. Legacy field-proxy tapes remain checked against their original selected fields. Current taxable tape structurally completes A. Existing six-band tax rules, exact-chain evaluation, unsupported drafts, dependent staleness, saved-record identities and save-before-flight remain.
- `Assets/Scripts/Level3/Level3Checkpointer.cs`, `Assets/Resources/Tutorials/Level_03/guide.json`: old totals wait now follows acquired summary; hints describe manual keys and appended book leaf, preserving lesson IDs and downstream cues. No invisible total placements are required.
- Tests: `Tests/Level3Regression/Program.cs`, `Tests/VerifyLevel3Print.py`. Docs: AGENTS, README, OFFICE_CASE_PLAY, T07/T08 tickets, PROGRESS and KNOWN_ISSUES. Historical entries remain evidence of previous revisions, not current acceptance instructions.

### Player flow

1. Drag summary Income into the real calculator, minus, Supported deductions, equals; deliberately attach printed Taxable income. Summary stays reusable; no Income/Deductions folder fields or invisible persisted autofill.
2. Open the appended practice leaf, drag a band chip into Annual band. Its Threshold, Rate, Base tax and formula appear in reserved space; main fields do not change position or disappear. Selection is neutral and may be wrong.
3. Drag Taxable income to calculator; minus Threshold; equals. Multiply Rate; equals. Add Base tax; equals. The existing chain keeps every exact source and intermediate equals. Deliberately attach its final print to Tax due.
4. After the existing certificate acquisition/explanation, drag a printed certificate amount into Tax credit. On the same calculator, Tax due minus Tax credit equals; attach Remaining payable. Wrong/negative work stays visible, unsupported where required; existing review decides correctness.

### Commands and observed internal results

- `python3 -B Tests/run_level3_repair.py --level2`: final runtime **0 errors/21 inherited warnings**, complete Editor **0 errors/10 inherited warnings**; **1929/1929 Level3**, including **24 fixed-manual-computation checks**; **5734 controls**; **1580 Level2**, with the same **3 native EventSystem comparer skips**. Covers actual manual minus/equals, multiply/equals, add/equals chain to62500, direct summary taxable support without fields, manual credit balance50000, exact identities, stale drag/tape on band change, foreign/year/revision/unknown band refusal, year-invalid operand, and wrong band52500 attached unsupported. Existing signed-save failure/retry/duplicate/reload, legacy T07/T08 and shared transfer checks remain passing. Updated superseded UI pins; domain helpers LoadStage remain legacy regression coverage, not live page controls.
- `python3 -B Tests/VerifyLevel3Print.py`: **237 PASS**. Fixed main fields use native 50-point values; compact band sources fit their 240×96 areas at34 points with padding/grip clearance. Appended book leaf measures655 of860 native inline units using three native chips and36-point descriptions. Exact font measurements are not rendered/touch acceptance.
- `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py`: **PASS**, 18 tickets /239 local links /122 source references /17 arithmetic fixtures /344 stable-legacy-revision assertions. Documentation only.

### USER acceptance still required

Import/restart Play; page3 should show all five main fields before and after each attachment. Verify the new practice book leaf, original pages/order/page turns and native chip appearance. Try every band and inspect revealed threshold/rate/base/formula, fixed row positions and native mouse/touch drags to calculator. Perform the complete sequence above including intermediate equals; confirm one final tape chain flies only after save. Test changed band after print, wrong operation/band, off-target drop, settings/back/focus/rotation, source reuse and other folder/book pages. Inspect field/readability/grip hit areas, remaining-payable placement above page arrows/footer, composited text/layer visibility, animations and audio on landscape/portrait phones. Prior screenshots' intermittent missing folder text is still a rendered acceptance concern; no new visual proof is claimed. No real saves were touched. Next remains T09 after user acceptance, not release completion.

## 5 November 2026 — T07 manual workspace and stacked-field spacing follow-up

Status: **Implemented, internally verified; user Unity/device acceptance pending.** User revisions supersede earlier automatic workspace setup and adaptive inline field layouts in the historical entries below.

- Removed Stage A's Open summary button and in-page instruction. Summary retrieval stays on its actual Bag/hotbar icon. Existing Arthur guide progression is unchanged; contextual empty-field messages and B/C actions remain.
- Opening/dropping/resizing the summary no longer lifts or repositions the calculator/folder. Deleted the unused `PrepareSummaryDesk` / `ShowComputationBeside` helpers and the calculator's summary-dependent lane override; lifting the calculator no longer automatically opens the folder. The player operates each existing tool manually. Summary icon-to-screen opening honors the actual drop point; resize only clamps that paper and cancels its owned gestures. Ownership, common drag behavior and exact transfers remain.
- Computation fields always stack caption above value through `PaperInkStyle.FieldTextStacked`, retain 50-point value size/blue printed form styling, and use 128-unit height, 12-unit vertical text padding and 20-unit gaps. Stage headings are centered 44-point print with top inset and room below. Result/other stages use the same field sizing; no financial domain or persistence change.
- Summary front fields now have 36-point labels, 50-point values, left/top/bottom padding, grip clearance and 20-unit space between amber backgrounds. Amount hit bands include the whole colored source field, not just glyph bounds. Summary back remains unchanged.

Changed paths: `Assets/Scripts/Level3/Level3SummaryDocument.cs`, `Level3DeskCalculator.cs`, `Level3CaseFolder.cs`, `Level3ComputationPage.cs`, `Level3AmountSource.cs`; `Assets/Scripts/SideQuest1/SupportingDocumentFace.cs`; `Tests/Level3Regression/Program.cs`, `Tests/VerifyLevel3Print.py`; this PROGRESS, KNOWN_ISSUES and T07 ticket.

Observed checks: `python3 -B Tests/run_level3_repair.py --level2` passed: runtime 0 errors/21 inherited warnings, complete Editor 0/10; **1905/1905 Level3**, **5734 controls**, **1580 Level2**, three native EventSystem comparer checks skipped. Existing exact-value, signed persistence, T07/T08 and shared transfer regressions remain green. Updated superseded automatic-opening/button/layout expectations; two new checks guard manual independence and stacked/padded field sizing. `python3 -B Tests/VerifyLevel3Print.py`: **220 PASS**; stacked 50-point field text measures 102 of 104 usable vertical units; larger summary labels/amounts fit with grip clearance. Geometry checks do not render Unity. Documentation validator run separately.

USER remaining: import/restart Play, open and place each tool independently; drop/move the summary and confirm other tools stay put. Compare stage-heading inset, stacked fields, equal gaps, summary text and padding on mouse/phone in both orientations. Repeat transfers from the full amber field to calculator/folder, turn the summary over, attach tape, continue B/C, and test Settings/Back/focus. The screenshots still show missing folder glyphs in some composited views; native rendered text/layer acceptance remains unresolved until user testing (the prior capture invalidation fix is retained). No visual, native touch, audio or animation acceptance claim. No Git commands, Unity/editor launch/automation, real saves or scene/prefab/package edits.

## 5 November 2026 — T07 presentation repair: first-page form treatment and summary source fields

Status: **Implemented, internally verified; user rendered/device acceptance pending.** Follow-up to the user's screenshots of blank Computation text and inconsistent small outlined boxes. No Git, Unity/editor launch or automation, scene/prefab/package edits, or real-save access.

The Computation renderer used a separate 600 × 700 centered/scaled design, smaller labels and split label/value boxes rather than the first page's real content column and `PaperInkStyle`. The folder compositor also lacked page-redraw glyph-cache invalidation: its text refresh could skip a changed page at unchanged world scale. These source findings are verified internally; the exact native rendering symptom still requires user reproduction after import.

Changes:

- `Assets/Scripts/Level3/Level3ComputationPage.cs`: the existing PageBuilder now hosts the page in the same first-page content column, aligned to its top, without vertical squeezing. Full-width Income/Deductions/result fields reuse `PaperInkStyle.StyleFieldRow` and `SetFieldText`: blue fill, pen ink, caption/blank styling, rounded border, grip and `PaperPopIn`. Values use the same 50-point size as the first-page prefab; 110-unit rows have 10-unit gaps. Native blue outlines retain neutral transfer-hover feedback. Stage B/C values and band chooser use the same field treatment; larger instructions/buttons/reference/working text retain their existing actions and page turns. Band selection opens by tapping its whole printed field. Exact transfer, manual Stage A keys, tape/save flight and computation domain code unchanged.
- `Assets/Scripts/UI/CaseFolderUI.cs`: narrow `PageBuilder.ContentColumn` accessor, and invalidation of the existing document layer's text capture after page redraw/layout.
- `Assets/Scripts/UI/HUD/Doc3DLayer.cs`: `InvalidateText` clears the scale cache and enables glyph refresh before the next capture. `Assets/Scripts/UI/HUD/CaseFolder3DView.cs` enables it when configuring the existing folder view. Regular stationary captures still skip unchanged text meshes; no alternate overlay or camera system.
- `Assets/Scripts/UI/PaperInkStyle.cs`: shared first-page font-size constant; `StyleSourceField` reuses the printed form's border, grip and entrance with the existing warm marker palette.
- `Assets/Scripts/SideQuest1/SupportingDocumentFace.cs`: only front summary total rows receive that amber field treatment. Back-face detailed rows stay plain. `Assets/Scripts/Level3/Level3AmountSource.cs`: summary selection overlay uses amber rather than the certificate's blue. Paper/amount transfer ownership is unchanged.
- `Tests/Level3Regression/Program.cs`, `Tests/VerifyLevel3Print.py`: current-column/native-font and captured-text lifecycle contracts, front-only summary styling, and shipped-font width/line-height measurements for actual folder labels/values. Two obsolete old-layout/copy test pins were updated to the revised presentation; no domain assertions removed.
- Documentation: this entry, KNOWN_ISSUES and active T07 ticket presentation note.

Observed commands/results:

- `/tmp/check_l3_presentation.py` reproduced five failing source contracts before edits and passed all five after (column, native field style/size, capture invalidation, front source styling). This is a structural reproduction, not a Unity rendering test; permanent regression contracts are in the suite.
- Final `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors / 21 inherited warnings**; complete Editor **0 errors / 10 inherited warnings**; Level3 **1903/1903**, controls **5734**, Level2 **1580**, with **3 native EventSystem comparer checks skipped**. Save/transfer/exact-value/T07/T08 checks remain passing.
- `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py`: **PASS** (18 tickets, 239 links, 122 source references, 17 arithmetic fixtures, 344 content assertions).
- `python3 -B Tests/VerifyLevel3Print.py`: **220 PASS**, including native 50-point field values/captions fitting 708 usable units and at most 102 of the 110-unit row height for stacked prompts. Font geometry only.

USER manual remaining: import/restart Play, reopen Computation and page-turn repeatedly with summary/calculator present; confirm labels/header/arrows/indicator render and layer correctly. Compare field fonts, 10-unit gaps, blue form treatment and entrance to Taxpayer Information. Confirm amber fields appear on summary front only, are readable/draggable repeatedly into calculator and folder, and back remains readable. Repeat portrait/landscape, tape flight/settle, B band selection/C certificate continuation, Settings/Back/focus, mouse and phone. No rendered visual, native touch, audio or animation acceptance is claimed. Next ticket remains T09 after user acceptance; no scope expansion.

## 5 November 2026 — T07 follow-up: Bag summary through the physical calculator

Status: **Implemented, internally verified; user Unity/device acceptance pending.** This user-requested Stage A amendment supersedes the page Operation/Calculate instructions in the historical T07 entry below. Stage B and T08 C retain their existing interaction and saved records. No Git commands, Unity/editor launch or automation, scene/prefab/package changes, or real-save access.

### Live path and implementation

- Reused the single `bea_summary` possession and `Level3SummaryDocument` / `ClientRecordPaper`; no Supporting Documents summary copy. Bag opening at the office workstation lifts that same paper into its reading lane, brings up the existing desk calculator, and opens/reframes the existing Case Folder Computation view. Its original front totals remain reusable.
- `Level3AmountSource` drives the existing `ValueDragSource` / `ValueTransferManager`. Summary amount bands opt into the existing editable desk layer; other paper defaults remain read-only. `Level3DeskToolHitTarget.OnDrop` accepts an active matching summary-band gesture at the actual calculator display, and `Level3TapeTransfer.PlaceTransfer` calls the narrow `Level3ValueTransfer.InsertSummary` adapter into the real `Level3Calculator.SelectSource` path. No preloaded operand, field autofill, automatic operation or answer.
- Player path: Bag summary onto desk → drag Income into display → press physical minus → drag Supported deductions into display → press physical equals → drag existing printed tape into Taxable income. Separately drag the same summary bands into folder Income/Deductions. Those fields remain blank until placed. Existing `CommitComputation` saves before the tape flight and consumption; a failed save retains actionable tape for retry.
- `Level3ComputationRules` accepts direct worksheet operands alongside the preserved field-source chain, and carries that original lineage through tax due and remaining payable. Exact decimals, worksheet source IDs, case, tax year and content revision are validated; no money goes through float or legacy CaseData. A plausible wrong operation can still print and attach as an unsupported draft. Matching separately placed fields and sourced subtraction are required for supported Stage A; attachment alone does not complete missing fields.
- Stage A no longer builds page Operation/Calculate controls. Stage B chosen-band Calculate and T08 stage C remain. The computation page reuses `PrintedPaperStyle` colors and existing field/button treatment. Summary-only front totals are larger (30-point labels, 42-point values) with taller drag bands. Existing two-line front/back summary headings remain.
- Workspace lanes use the existing physical paper, folder and calculator: three columns in landscape, summary above folder/calculator in portrait. Reflow cancels only this summary's active amount/paper gestures and moves all three existing views. `CaseFolder3DView.PlaceInScreenArea` and `OfficeHudRuntime.PlaceCaseFolderInArea` fit the existing folder; `Level3DeskCalculator.PrepareSummaryDesk` uses its own lift/move animation. Neutral field/display outlines indicate mechanical acceptance, not correctness.
- Cancellation review caught pointer-up preceding canceled drop and incomplete rotation reflow. Source cancellation now occurs on pointer-up; destination guards require a noncanceled active matching source. Settings/dialogue/focus/pause/disable clear only the owned gesture/selection. Leaving the desk puts the same paper away without removing ownership.

### Exact changed paths

- `Assets/Scripts/Level3/`: `Level3ValueTransfer.cs`, `Level3DeskCalculatorTape.cs`, `Level3DeskToolHitTarget.cs`, `Level3AmountSource.cs`, `Level3ComputationRules.cs`, `Level3ComputationPage.cs`, `Level3CaseFolder.cs`, `Level3SummaryDocument.cs`, `Level3DeskCalculator.cs`.
- `Assets/Scripts/SideQuest1/ClientRecordPaper.cs`, `Assets/Scripts/SideQuest1/SupportingDocumentFace.cs`, `Assets/Scripts/UI/HUD/CaseFolder3DView.cs`, `Assets/Scripts/UI/HUD/OfficeHudRuntime.cs`, `Assets/Scripts/UI/PrintedPaperStyle.cs`.
- `Assets/Resources/Tutorials/Level_03/guide.json`, `Tests/Level3Regression/Program.cs`, `Tests/VerifyLevel3Print.py`.
- This pack's `AGENTS.md`, `README.md`, `OFFICE_CASE_PLAY.md`, `tickets/T07-individual-base-and-graduated-tape.md`, `PROGRESS.md`, `KNOWN_ISSUES.md`.

### Commands and observed results

- Baseline `python3 -B Tests/run_level3_repair.py --level2`: runtime 0 errors/21 inherited warnings; complete Editor 0 errors/10 inherited warnings; Level3 1867/1867; controls 5734; Level2 1580, with three native EventSystem comparer checks skipped.
- Final same command: runtime **0 errors/21 warnings**, Editor **0 errors/10 warnings**; Level3 **1900/1900**, including **31 physical-summary Stage A checks**; controls **5734**; Level2 **1580**, same **3 native skips**. Actual shared-manager insertion, independent reusable placements, real minus/equals token provenance, signed-store failure/retry/duplicate/reload, wrong draft support, case/year/revision/amount refusals, downstream B/C lineage and workspace geometry are covered. The old T07 domain helper checks remain regression coverage, not a Stage A page Calculate UI.
- `python3 -B Tests/VerifyLevel3Print.py`: **214 shipped-font geometry checks PASS**. This measures print bounds, not actual Unity rendering.
- `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py`: PASS, 18 tickets / 239 local links / 122 existing source references / 17 arithmetic fixtures / 344 stable-legacy-revision content assertions. Documentation validation only.
- Two independent read-only specification/safety reviews completed; canceled-drop and reflow findings above fixed and included in the final run. No native pointer dispatch, Play Mode, device visuals, audio, timing or touch feel was tested.

### USER manual acceptance still required

1. After Unity imports, obtain the actual T06 summary, return to the office desk, and take it out of Bag/hotbar. Confirm one owned item and one readable front/back paper; no duplicate summary in folder Supporting Documents.
2. On mouse and phone, test the complete physical sequence (900,000 minus 300,000 equals 600,000), printed-tape drag/flight into Taxable income, then separate source drags into Income/Deductions. Repeat in the opposite field/attachment order. Confirm the source is reusable and fields are never autofilled.
3. Confirm summary totals/header, folder fields, calculator keys/display/tape, page tabs and essential HUD controls remain readable and reachable together in landscape and portrait. Try resize/rotation with paper out and while dragging; check layer focus and physical settle/flight.
4. Try plus instead of minus and a mismatched source in a field: neutral visible unsupported draft, no success flash. Test drop off-target, canceled touch, Settings, Back, focus loss, put-away and reopen; no stuck selection or duplicated transfer.
5. Continue Stage B with the annual band and T08 withholding/remaining payable; other folder pages stay reachable. Confirm existing quiet audio/settings behavior. Signed persistence failure/retry/reload was tested only in scratch; no real campaign save was accessed here.

Next remains user acceptance of T04–T08, then T09; this change does not implement review/stamps or advance release acceptance.

## 5 November 2026 — Revised T08: Bea's withholding certificate and stage C (remaining payable)

Status: **Implemented, internally verified; user Play Mode/manual acceptance pending.** T08 only, after T07 was completed and verified (entry below). No Git command, Unity/editor launch or automation, scene/prefab/package/asset change, real-save access or scene recopy.

### Found before changing

- The existing Type 8 certificate (`Level3RecordRules` print, `SupportingDocumentFace.BuildWithholding`), `Level3CertificateRules.HandOver` (one action id, saved first through `Level3Checkpointer.CommitCertificate`), Bea's office walk and conference-room talk, and Supporting Documents listing of acquired papers were all reusable.
- Her talk raised the held certificate over the dialogue and then asked a three-answer credit-versus-expense question; `SelectCredit` required that saved explanation, and the credit was picked from a leaf chooser beside a blue tab — the detached chooser/duplicate question the revised ticket removes.

### What the player now experiences

1. With tax due attached (T07), stage C on the folder says "Bea is waiting with one more paper." In the conference room Bea says one line ("One more paper. Harbor Menu already withheld some tax from my menu fees. Here's the certificate.") ending on **Take the certificate** (saved first), then "It's in your Case Folder now, with my other papers." and the talk ends. Nothing is held up over the dialogue; no question.
2. At the desk, stage C shows Tax due (stage B's attached result) and an empty Tax credit field: "Transfer the tax credit from the certificate." **Read certificate** turns to Supporting Documents and takes the certificate out (payer, payee, period, income payment with "Already part of …", tax withheld, reference). Both printed amounts are alike transfer bands; select one and tap Tax credit (or drag). Optional **Help**: "Tax already withheld counts toward payment, not as an expense."
3. Choose − Subtract, Calculate (Tax due − Tax credit), attach Remaining payable with the same print/clip gesture. Stages A and B stay collapsed; all three summaries are tappable.
4. The payment amount, another amount or + calculate and attach as drafts; a negative balance shows with its sign (−₱187,500.00). Changing only the credit marks Remaining payable "Recalculate" and puts a stamp out of date; A and B stay current.

### Changes (existing owners)

- `Assets/Scripts/Level3/Level3BeaInteractable.cs`: the office talk no longer starts the question; after the saved handover one closing line (`certificateReadLine`) ends the talk (no `AskLines`/paper raise). The question/answer code is kept unreachable for reference; a saved `creditExplained` is preserved and no longer required.
- `Assets/Resources/Levels/Level_03/town_life.json` (+ default in `Level3TownContent.cs`): one 16-word handover turn and the closing line; `creditQuestion`/`creditAnswers` kept (validator) but not asked; readme note.
- `Level3ComputationRules.cs`: `PlaceInput` for Tax credit needs the acquired certificate only; signed `Peso` display for negatives. Stage C (`LoadStage` balance, `StageOpen`) was added with T07's stage model.
- `Level3CertificateRules.cs`: documentation of the revised flow (commands unchanged).
- `Level3ComputationPage.cs` stage C (Tax due, Tax credit, operation, Calculate, Read certificate, Help, result) and `Level3AmountSource` certificate bands were written with T07's page and are first reachable here; `Level3CaseFolder` lets the records rebuild while the Computation page shows so the handed-over certificate reaches Supporting Documents.
- `Assets/Resources/Tutorials/Level_03/guide.json`: credit hints point to Supporting Documents, name no figure; `credit-link` now follows `flag:certificateReceived` (was the retired explanation).
- `Tests/Level3Regression/Program.cs`: new Revised T08 section (28 checks); two historical T08 pins updated to this contract (one short handover turn; credit hint after the handover).

### Commands and results actually run (5 November)

- `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors / 21 warnings**, full Editor **0 / 10** (baseline); Level3Regression **1867/1867** (Revised T07 **65**, Revised T08 **28**, historical T08 92 with the two updated pins); ControlsRegression **5734 PASS**; Level2 scratch **1580 PASS**, the same 3 native EventSystem checks explicitly skipped.
- First T08 run 1866/1867: my own fixture assumption (payee printed "Bea"), corrected to the real fixture.
- Mutation probe (scratchpad; `shasum -c` OK after restore): a credit edit that drops stage A's tape and a Tax credit without the certificate → **1856/1867**, 11 checks failed as intended.
- `python3 -B Documentation/Level3TheNumbersTellAStory/verify_content.py` **PASS 344**; `python3 -B Tests/VerifyGuideTargets.py` 14 paths, 0 missing.
- Revised T08 covers: no handover before tax due; handover acquires B-WHT once (action id, milestone), duplicate quiet; certificate listed in Supporting Documents with payee/period/included-in and its two bands; exact 12,500 / 250,000 only after the handover; stage C waits for the credit and the player's operation; 62,500 − 12,500 = 50,000 attached and fully supported, income unchanged at 900,000 (`CertificateIncomeIncludedIn` R2, no credit in income); payment-as-credit −187,500 kept negative and unsupported; other amount, + and credit-as-deduction drafts unsupported; another case/float/altered amount refused unchanged; credit-only change keeps A/B current and supported, stales C and the stamp; legacy explained/linked credit is a quiet duplicate and a pending retired question does not block; signed-store failures for handover and credit publish nothing, retry once, duplicates write nothing, reload resumes complete with one certificate and possessions intact; Bea/page wiring and copy; guide.
- Not run: Unity, Play Mode, rendering, Bea's conversation/camera, native touch/drag onto the folder, audio, phone readability.

### USER manual script (Unity/device; not run here)

Continue from the T07 script (fixture or story) with tax due attached.

1. Stage C reads "Bea is waiting with one more paper." Walk to the conference room and talk to Bea: one line, Take the certificate, one closing line, the talk ends; no paper over the dialogue box, no question. Try Talk later before taking, then return.
2. Back at the desk: Read certificate → it comes out of Supporting Documents; check both amounts legible and the payee/period/"Already part of" line. Select Tax withheld, turn to Computation, tap Tax credit; repeat by dragging. Try the payment amount.
3. Choose − Subtract, Calculate, attach Remaining payable (tap and drag). Check the negative case shows its sign.
4. Change only the credit: Remaining payable "Recalculate", A and B unchanged; stamp (T09) shows out of date.
5. Interrupt the handover save (story route: quit right after Take) and reload: certificate once, nothing replays. Settings/focus/Back with the certificate out. Sound on/off.

### Next step

User acceptance of T04–T08; then **T09** (shared physical stamps on the folder's Computation page and the Auditor review).

## 5 November 2026 — Revised T07: taxable income and tax due on the Case Folder's Computation page

Status: **Implemented, internally verified; user Play Mode/manual acceptance pending.** T07 only (T08 follows as its own entry). T06 kept as the accepted prerequisite and unchanged. No Git command, Unity/editor launch or automation, scene/prefab/package/asset change, real-save access or scene recopy.

### Found before changing

- Bea's computation still lived on the retired clipboard leaf (`Level3ComputationLeafModel`) with PICK choosers; the real folder had only Client and Supporting Documents, and the calculator and the floating folder were kept mutually exclusive (`Lift` closed the folder; `Level3CaseFolder.KeepOneToolUp` put the calculator away).
- `Level3ValueTransfer` (exact payload, `TryRead`, `FromPaper`, `worksheet:`/`paper:`/`tape:` sources) existed but no production code used it; the collected summary paper offered no transfer sources. `CaseFolderFieldRow.GetDataValue` converts numbers through `Convert.ToSingle`, so it cannot carry Level 3 money.
- Record papers take touches on a transparent "Touch" canvas (`ClientRecordPaper.Create`); the stand/Bag summary uses `Level3TablePaperRaycaster`, which reports only the surface.
- `Level3ComputationRules` (exact bands, chains, staleness, `Evaluate`) and `Level3Checkpointer.CommitComputation` (candidate → signed write → publish) were reusable as-is.

### What the player now experiences

1. Sit at the desk and tap the calculator. If the first-use example is still due, it runs as before (T03, once, unscored); the folder's Computation page meanwhile says "Tap the calculator for a short example."
2. Afterwards the calculator lifts on the right and the real Case Folder opens on the left at its new third page, **Computation** (Taxpayer Information and Supporting Documents stay one page-turn away).
3. **Stage A · Taxable income** — "Transfer income and deductions." Open summary turns to Supporting Documents and takes Bea's summary out; tap its Income line (neutral outline), turn back, tap the Income field (or drag the line onto it). Same for Deductions. Each field shows the amount and a short source name ("Bea's summary"). Choose − Subtract (or + Add), Calculate: the existing calculator prints Income − Deductions. Tap the printed strip then the result slot, or drag it there: the save comes first, then the strip flies and clips in; the slot shows the result with a brief settle.
4. Stage A collapses to "Taxable income · ₱amount"; **Stage B · Tax due** — "Choose an annual tax band." Choose band opens the six-row annual table (range + formula per row, none highlighted); tapping a row saves the band and closes it. The stage shows the selected band and its formula with current values, e.g. base + rate × (taxable − floor); Calculate prints the one chain; attach as before. Help: "The rate applies only to the amount above the band's floor."
5. Then stage C waits: "Bea is waiting with one more paper." (T08). A and B are tappable summaries that reopen their stage; View working shows the attached tape's full chain; a changed input marks only dependents "Recalculate".
6. Wrong amounts, the + operation or a wrong band calculate and attach exactly like right ones (no colour, flash or lock); the Auditor judges support later (T09). Away from the desk the page reads but does not act.

### Changes (existing owners; narrow Level 3 adapters)

- New `Assets/Scripts/Level3/Level3ComputationPage.cs` (+ `.meta` b280587abd7d4395b660667c0817ea5b): the Computation page drawn through `CaseFolderUI.PageBuilder` (Role `level3-computation`), one active stage, collapsed summaries, operation chips, band reference, working view, result slot; `Level3ComputationField` (shared `IDataValueDestination`, tap or drop, dry-run acceptance).
- New `Assets/Scripts/Level3/Level3AmountSource.cs` (+ `.meta` ad4041f7e26744cbb7b949eb0c2efec6): transfer bands on the summary's Income / Supported deductions lines (and the certificate's two amounts, used in T08) on each paper's own touch canvas; value resolved from chapter/content at selection; drives the shared `ValueDragSource`; otherwise forwards every touch to the paper.
- `Level3ComputationRules.cs`: stages (`StageDone`/`ActiveStage`/`StageOpen`/`StageNeedsRecalculating`/`StageOperation`), `CurrentAmount`, `PlaceInput` (exact, re-resolved, mechanical refusals only), `LoadStage` (Calculate from chosen inputs/operation/band), `BandWorking`, `Peso`.
- `Level3CaseFolder.cs`: adds the Computation page after Supporting Documents, redraws only on change (never during a strip flight), `ShowComputationBeside`, `ShowSource`, `ComputationLanded`; bands attached to rebuilt records; records may rebuild while the Computation page shows (never on Supporting Documents or with a paper out); calculator no longer put away while the folder opens during the computation.
- `Level3DeskCalculator.cs` / `Level3DeskCalculatorComputation.cs` / `Level3DeskCalculatorTape.cs`: leaf never shown (`SyncLeaf` open=false; code kept for T17 removal); the lift reserves no paper width and opens the folder at the left; `Calculate`, `AttachPrintedToFolder` (save → flight → consume), folder-slot flight target with uniform fit and a cached pose; page messages instead of leaf messages.
- `Level3SummaryDocument.cs`, `Level3PaperMachineModel.cs` (`Level3TablePaperRaycaster` reports an offered band under the pointer): the Bag/stand summary carries the same bands.
- Shared, opt-in: `Assets/Scripts/UI/HUD/OfficeHudRuntime.cs` `OpenCaseFolderViewAt(Vector2)` (the existing drop-open path). `CaseFolderUI`, `ValueTransferManager`, `ValueDragSource`, `SupportingDocumentFace`, Level 1/2 code unchanged.
- `Assets/Resources/Tutorials/Level_03/guide.json`: the six computation hints rewritten (folder/summary, no PICK/clipboard, no figures); waits unchanged.
- `Tests/Level3Regression/Program.cs`: new Revised T07 section (65 checks); two T03 pins updated to this contract (lifting for the example still closes the folder, Bea's computation keeps it out; records rebuild while idle and not on show).

### Commands and results actually run (5 November)

- `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors / 21 warnings**, full Editor **0 / 10** (same baseline); Level3Regression **1838/1838**; ControlsRegression **5734 PASS**; Level2 scratch **1580 PASS** with the same 3 native EventSystem comparer checks explicitly skipped.
- After the final layout/alignment fix: `python3 -B Tests/run_level3_repair.py` → 0/21, 0/10; Level3Regression **1839/1839** (Revised T07 **65**); Controls **5734**.
- First T07 run was 1834/1838: my own copy check (a figure in a doc comment, fixed), two T03 pins of the retired one-tool rule (updated as listed), one T08 guide pin from a T08-scoped guide edit made early (reverted here; redone under T08).
- Mutation probe (scratchpad; production file restored, `shasum -c` OK): `PlaceInput` trusting the payload amount and `LoadStage` using the containing band instead of the chosen one → **1837/1839**, the altered-amount and wrong-band checks failed as intended.
- Revised T07 covers: summary sources (exact 900,000 / 300,000, case/source/year/revision; nothing before collection, while the example is due, or from unheld papers); `PlaceInput` applied/duplicate/plausible-wrong and ten mechanical refusals that change nothing; Calculate waits for both amounts and the player's operation; 600,000 attached; band never preselected; `BandWorking` with current values; 62,500 chain attached with two attachments only and fully supported facts; wrong band (52,500), + (1,200,000), swapped (−600,000) and typed drafts stay editable and unsupported; input change stales A and B only, band change stales B only; signed-store failure → nothing published or written, retry once, duplicate no write, reload resumes at stage B with possessions intact and no answers saved; command order and mode isolation pins; copy budgets at the bindings; guide.
- Not run: Unity, Play Mode, rendering, native EventSystem dispatch onto the folder page/touch canvas, drag feel, audio, phone readability.

### USER manual script (Unity/device; not run here)

Choose Tools > TaxVenture > Level 3 > Fixture: First Office Calculation, Play Office_Level3 (nothing saves), sit at the desk.

1. Tap the calculator: it lifts right; the folder comes out left on **Computation**. Check placement, overlap on your phone, that either can still be moved, and readable page text (no tiny or clipped figures).
2. Open summary → the summary comes out on Supporting Documents. Tap its Income line (faint outline turns brass), turn to Computation, tap Income. Repeat Deductions by dragging the line onto the field. Try a wrong line on the wrong field (accepted quietly).
3. Choose − Subtract, Calculate: the strip prints. Tap it, then the result slot; then repeat once by dragging a new print. Check the save-then-flight/clip motion and the settle.
4. Stage B: Choose band, read the six rows, pick one; check the formula line; Calculate, attach. Try a wrong band, then the right one: no correctness cue anywhere.
5. Tap the collapsed stages to reopen; View working; change Deductions to see "Recalculate" on A and B only.
6. Leave the desk, Settings and app switch mid-flight and with an amount selected; Back with the band table open. Sound on/off.
7. Story route (fixture None): reload after stage A — the page resumes at stage B, nothing replays.

### Next step

**T08** (certificate handover, Supporting Documents certificate amounts, stage C), then user acceptance of T04–T08.

## 5 November 2026 — release scope set to one Bea case

User decision: the current Level 3 chapter/release contains only Bea’s case. Nina/Marine and the company case are deferred until the user asks to add them. Kept T11/T12 and all company source, fixture, and tax-authority documentation for that future work; reclassified those tickets as deferred. Updated the active sequence/dependencies so T13–T18 complete and release Bea’s case without company phases or company-only authority gates. Bea-only current maximum: 160 coins, 160 mastery XP, +6 Public Trust. Updated the current plan, specification, ticket pack, verification/reward guidance, T13–T18, and documentation validators. Documentation-only change; no runtime, fixture amount, actual save, Unity or Git changes. Current next implementation remains T07 after user-authorized T06 acceptance.

## 5 November 2026 — readable matching summary headers

User screenshots showed front title ellipsis and back title `(continued)` spilling past the paper. Cause: the generic front transaction header forces one line/ellipsis, while the back appends continuation inline with overflow enabled. Added a summary-only shared `BuildSummaryHeader` for both faces in `Assets/Scripts/SideQuest1/SupportingDocumentFace.cs`. The full title breaks into INCOME & EXPENSE / SUMMARY at fixed38pt on both sides; icon/insets/band are identical. TOTALS · FRONT or ENTRIES & SOURCES · BACK has a separate20pt line. A154-unit header and166-unit body start preserve spacing and existing body font sizes. Other supporting-document headers retain their existing layouts. No auto-shrink or inline continued suffix on the summary.

Added6 actual shipped-font geometry assertions to `Tests/VerifyLevel3Print.py`: two title-line widths, two-line height, marker separation, body clearance and shared front/back header use. The new check failed before the change; **208** font-geometry checks now pass. `python3 -B Tests/run_level3_repair.py --level2`: runtime/Editor compile0 errors,21/10 baseline warnings; Level3 **1772/1772**, Controls **5734**, Level2 **1580** pass;3 native EventSystem checks remain explicitly skipped. Changed files: the face builder, font validator, this progress record and KNOWN_ISSUES. No Unity/editor launch/automation, Git action, scene/prefab change or actual-save access. User restarts Play after import to rebuild printed faces, then checks front/back readability on screen and device. Actual TMP rendering/manual visual acceptance remains pending.

## 5 November 2026 — physical stand drag and inventory-origin summary entrance

User revised the gesture: stand extraction must carry the actual printed summary, while Bag/hotbar retain their existing ghost; dropping an inventory ghost must not fly the real paper from the remote world stand.

Removed the stand-only extraction ghost and its now-unused shared `ClientRecordPaper` ghost callback/state path. The summary opts into a0.14second real-sheet pickup: the same paper detaches once, interpolates its pose/size from the stand to the bounded reading drag, then follows the pointer directly. Release/cancel finishes that pickup before existing stand/store handling; reduced-motion skips the pickup easing. Other record holders retain the default immediate drag behavior. There is no paper left on the stand while carrying the real sheet.

Added `ClientRecordPaper.TakeOutFromScreen`: uses the shared compositor and smooth motion but establishes the initial readable pose at the inventory release/icon point (72% scale, slight tilt,0.16second settle;0.05 in reduced-motion). No world-stand lift arc or clip delay. Summary icon drag hands off at its release; a stored icon tap starts from its actual hotbar/Bag target. An entrance caught mid-motion can be dragged normally; its settled callback is retained for details/page behavior. `HudDragItem`, `HudIconDragOpen`, hotbar ghost construction and Bag tile ghost were not modified in this follow-up, preserving the requested existing inventory ghost.

Changed `Assets/Scripts/SideQuest1/ClientRecordPaper.cs`, `Assets/Scripts/Level3/Level3SummaryDocument.cs`, `Tests/Level3Regression/Program.cs`, `PROGRESS.md`, `KNOWN_ISSUES.md`. Added two gesture/origin regression contracts; both failed before the change (1770/1772), then passed. Final `python3 -B Tests/run_level3_repair.py --level2`: runtime/Editor compile0 errors,21/10 baseline warnings; Level3 **1772/1772**, Controls **5734**, Level2 **1580** pass, with the existing3 native EventSystem checks explicitly skipped. No Unity launch/automation, actual-save access, Git, scene/prefab/package changes. User restarts Play after import and checks physical pickup, quick release, stand/Bag docking, unchanged inventory ghost, screen-edge drops, inventory opening away from Bea's desk, reduced motion, focus/settings interruptions and actual device feel. Render/touch/audio/smoothness acceptance remains manual pending. This supersedes earlier references to a stand extraction ghost.

## 5 November 2026 — fix opened summary drawing behind the table props

User screenshot showed the readable summary behind the machine and stand. Root cause: `Level3SummaryDocument.ConfigureCanvas` called `HudHotbar.Instance.GetComponentInParent<Canvas>()`, but `HudHotbar.Create(host, canvasRoot)` attaches its controller to the separate `Office HUD Runtime` host, outside the Canvas hierarchy. That lookup returned null, leaving `ClientRecordPaper.WindowCanvas` unassigned. The paper opened as a normal world object instead of joining the existing always-composited `Doc3DLayer`; the same failed setup also prevented its extraction ghost from being configured.

Added `HudHotbar.DocumentCanvas`, exposing its already-bound `canvasRoot`; summary setup now uses that reference directly. The existing shared paper compositor, independent capture layer, front ordering and ghost are therefore connected before opening. No forced permanent top layer, second paper renderer, new canvas manager or world-depth workaround; folder/summary focus remains the shared system.

Changed: `Assets/Scripts/UI/HUD/HudHotbar.cs`, `Assets/Scripts/Level3/Level3SummaryDocument.cs`, `Tests/Level3Regression/Program.cs`, `PROGRESS.md`, `KNOWN_ISSUES.md`. Regression contract failed before the change (1769/1770), then passed. `python3 -B Tests/run_level3_repair.py --level2`: current runtime/Editor compile0 errors with21/10 baseline warnings; Level3 **1770/1770**, Controls **5734**, Level2 **1580** pass. Existing3 native EventSystem comparer checks explicitly skipped. No Unity/editor launch/automation, Git, scene/prefab change or actual-save access. User stops Play after import and restarts so the runtime-built paper acquires its compositor; verify unobscured paper over machine/stand and folder/summary focus in both orders. Rendering/native touch remain manual pending.

## 5 November 2026 — fix summary missing from the stand after final feed

User screenshot showed the summary-ready guide and returned inventory rail, but an empty stand. Root cause: `Level3SummaryDocument.Bind` copied `Machine.Card.parent/localPosition/localRotation`. The activity card is reparented to `FeedFrame` and left hidden at its fully-fed pose after filing. Creating the summary following real final-batch play therefore put the document inside the machine. A ready-only fixture could miss this lifecycle dependency.

Added `Level3PaperMachineModel.StandDocumentAnchor`, created once under the stand's fixed easel at its normal card rest pose plus the existing3mm face clearance. `Level3BeaTable` binds the summary to that anchor; `Level3SummaryDocument` creates its slot there with identity local pose. Paper, outline and docking hit plane now share the stable stand anchor and cannot inherit activity-card feed/recall movement. Existing collection, exact totals, inventory and rewards remain untouched.

Regression: added the final-feed/fixed-stand wiring contract to `Tests/Level3Regression/Program.cs`; it failed before the fix (1768/1769), then passed with the fixed anchor. Verification command `python3 -B Tests/run_level3_repair.py --level2`: runtime/Editor compilation0 errors (21/10 baseline warnings), Level3 **1769/1769**, Controls **5734**, Level2 **1580** pass; the existing three native EventSystem checks remain explicitly skipped. Source/managed checks do not prove rendering. No Unity/editor launch/automation, Git action, scene/prefab change or actual-save access. User stops Play after import and starts again so runtime-created anchors are rebuilt; verify both final-batch play and the summary-ready fixture, then stand/open/Bag/stand round trips. Visual/touch acceptance remains pending.

Changed: `Assets/Scripts/Level3/Level3PaperMachineModel.cs`, `Level3BeaTable.cs`, `Level3SummaryDocument.cs`; `Tests/Level3Regression/Program.cs`; `PROGRESS.md`, `KNOWN_ISSUES.md`.

## 5 November 2026 — recurring Unity Remote connection failure

User reported Unity Remote again remaining on its phone connection screen. Evidence: current Editor.log ends with `[remote] error: Init socket failed`; Unity's installed SDK `adb devices -l` shows one USB-connected authorized Redmi, while `adb forward --list` was empty and no process listened on port7201. The prior native `AndroidADBKillServerOnExit` preference still read0, so preserving the server alone did not preserve this session-scoped forward. Sandbox-local ADB access was denied; approved local-service escalation was used for actual checks/repair.

Restored only the missing mapping with Unity6000.4.5f1's SDK `adb -d forward --no-rebind tcp:7201 tcp:7201`; verified `forward --list` and the127.0.0.1:7201 listener. No ADB kill-server, device restart, USB authorization change, game-save access, Editor launch/Play automation, scene/prefab/input-settings/package edit or Git operation.

Added `Assets/Editor/UnityRemoteRecovery.cs` (+ unique `.meta`): Editor-only, enabled by default, nonblocking checks every8seconds plus Play-mode transition scheduling. Resolves the configured/bundled SDK rather than a hardcoded Unity version. Requires exactly one authorized USB Android; missing forward is added with `--no-rebind` then verified. Correct mappings are left alone; another mapping on7201 is never overwritten. Preserves native keep-ADB preference, selects Any Android Device only when Remote was None/Android, respects other selected platforms. Per-command5second timeout, own-client process cleanup on assembly reload/quit, changed-status logging. Tools > TaxVenture > Unity Remote exposes Automatic Connection Recovery and Repair Connection Now. No OS background daemon or player-build code. A timed-out native Remote attempt may still need one manual Stop/Play after repair; the helper does not restart the user's Play session. Physical USB/phone authorization/app readiness and Unity's native video connection are outside the mapping repair's guarantees.

Verification: `python3 -B Tests/run_level3_repair.py --build-only --remote` compiled current runtime/complete Editor with0 errors and the same21/10 baseline warnings; **416** actual managed ADB-output recovery assertions passed (100 disconnect/restore cycles, USB authorization, single-device selection, protected port conflicts). `Tests/UnityRemoteRecoveryRegression/Program.cs` is new; runner includes new/unimported Editor scripts and optional `--remote` suite. `python3 -B Tests/VerifyMobileInput.py`: all **7** existing startup/mouse-touch/focus contracts pass. Current7201 listener remains verified. No end-to-end phone video/touch or Editor-restart claim: user keeps Remote open and starts a fresh Play session after import, then confirms reconnect on device.

Primary references: [Unity Remote setup](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/cross-platform-features/unity-remote5), [Unity's Editor Remote device selection source](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/EditorSettingsInspector.cs).

## 5 November 2026 — T06 follow-up: the summary is the actual stand document

Status: **Implemented, internally verified; user Play Mode/manual acceptance pending.** This entry supersedes the earlier T06 ledge preview / Collect key / folder-contained summary presentation. User accepted the single interview round, with the rail hidden through batch work and restored only when the real summary appears. Totals continue borrowing the stand before readiness; afterward they open the summary details.

The final accepted batch puts the existing `ClientRecordPaper` summary on the left stand. There is no separate Collect panel or ledge proxy. Tap opens a preview; stand/icon drags use the shared ghost and open at the release point, clamped inside the view. The same paper turns front/back repeatedly and uses `Doc3DLayer` beside the real Case Folder; a first touch of a background paper focuses it. Higher visible documents reject covered summary hits before Unity's raycaster module priority can steal them. The stand outlines while dragging. Drag back docks; drop onto Bag/unlocked bar slot stores. The first storage drop commits chapter `B-WORKSHEET`, the separate `bea_summary` inventory item, readable reference pages and existing once-only rewards together, before feedback. Full/locked targets and failed writes keep the paper retryable. Owned papers keep their inventory location while out; docking does not revoke completion. Leaving returns an uncollected preview visibly to the stand; an owned paper stores. Focus/settings/dialogue/rewards/camera transitions cancel active physical drags. Old collected worksheets quietly gain the separate item without re-awards, preserving saved readable text and removing its folder duplicate. Office decimal totals and grading remain the existing domain output.

Changed paths:

- `Assets/Scripts/Level3/Level3SummaryDocument.cs` and `.cs.meta` (new narrow physical-paper adapter).
- `Assets/Scripts/Level3/Level3BeaTable.cs`, `Level3CaseFolder.cs`, `Level3Checkpointer.cs`, `Level3WorksheetCheck.cs`, `Level3PaperMachineModel.cs`.
- `Assets/Scripts/RewardsSystem/CampaignInventory.cs`, `PlayerProgressManager.cs`; `Assets/Resources/Campaign/inventory.json`.
- `Assets/Scripts/SideQuest1/ClientRecordPaper.cs` (opt-in compositor parent/extraction ghost; cancellation).
- `Assets/Scripts/UI/HUD/HudHotbar.cs`, `InventoryScreen.cs`, `OfficeHudRuntime.cs`, `HudIconArt.cs`, `HudIconDragOpen.cs`, `HudDragItem.cs`, `Doc3DLayer.cs`, `CaseFolder3DView.cs`.
- `Assets/Resources/Tutorials/Level_03/guide.json`; `Tests/Level3Regression/Program.cs`; this progress record, `KNOWN_ISSUES.md`, `SIDE_QUEST_MACHINE.md`, T06 ticket.

Internal evidence:

- `python3 -B Tests/run_level3_repair.py --build-only`: current runtime/Editor compilation passes, 21/10 existing warnings (same as baseline).
- `python3 -B Tests/run_level3_repair.py --suites --level2`: **Level3 1768/1768**, **Controls 5734**, **Level2 1580** pass. Three native Unity EventSystem comparer checks are explicitly skipped by the existing standalone harness; no claim they passed. New managed checks cover candidate/save-failure/retry/reload with the item and complete reference pages, idempotence, bag destination, normal move/swap, full/locked refusal and legacy readable-page transfer. The real stand-paper target projects to 121 dp minimum side in the existing 2400×1080 geometry check; this is not a render/device test.
- `python3 -B Documentation/Level3TheNumbersTellAStory/verify_content.py`: **343** pass.
- `python3 -B Tests/VerifyLevel3Print.py`: **202** font-geometry checks pass; this is not a render.
- `python3 -B Tests/VerifyLevel3Scene.py`: pass with **3 inherited warnings** (missing CaseDropDestination, two prefab-contained paths not provable from serialized scene). Scene unchanged by this follow-up.
- Independent standards/spec review found and fixed foreground-folder input interception and off-seat preview visibility; transition drag cancellation also applied.

No Git operations, Unity/editor launch/automation, real-save access or scene/prefab/package changes. Next: user checks the ready-summary Editor fixture, readable front/back, stand/icon/Bag round trips, simultaneous folder focus in both orders, edge clamping, leave/re-enter, device drag/touch/audio and final-batch rail timing. Then continue T07; visual acceptance is not inferred from managed checks.

## 5 November 2026 — T06: check machine batches, correct, collect Bea's Income & Expense Summary

Status: **Implemented, internally verified; user manual pending.** T06 only; its T07 handoff (office totals) is integrated, office gameplay is not. All T02–T05 working changes kept, including the latest user-confirmed T05 repairs (table raycast priority, whole-paper drag, Inspect Original, review tabs, enlarged dial/pointer, readable totals, smooth pickup/alignment/insertion, hidden inventory rail while seated). No Git command, Unity/editor launch or automation, scene/prefab/package/asset change, real-save access or scene recopy.

### Found before changing

- Nothing wrote `acceptedBatches`: a fresh run filed the earnings and stopped (L3-R43/L3-R61). The only checker was the retired twelve-decision board (tapes, link choice); `Acquire` required its current tapes.
- **The office read Bea's totals only from v1 subtotal tapes** (`Level3ComputationRules.TotalChoices`/`Evaluate`): a revised collection would have reached the office with no worksheet total at all.
- Supporting Documents listed only acquired originals; the Bag page printed the board's section/tape lines. The Editor's first-office fixture was still built through the board (L3-R29).
- Reusable seams: `CollectWorksheet` → `CommitLevelThreeWorksheet` (chapter + claims + mastery, one write), the board's reward card/flight/announce pattern, `Level3WorksheetHints`, `Level3RecordRules`/`ClientRecordPaper` (one reader), `Level3PaperMachineModel` motion/surface owners, `HudHotbar.KeepVisibleReferences`.

### What the player now experiences (seated at Bea's machine)

1. **Batch filed.** The stand shows the batch card: "Batch 1 of 3 · Earnings · 2 of 2 filed", one brass key **CHECK BATCH**. The lever still only files. Tabs can still recall a record first.
2. **Check.** One saved command. Only then: each tab gets a mark (brass disc with a ticked stroke = right; graphite disc with "?" = look again), the card says e.g. "1 of 2 right · 1 marked ?" and one short where-to-look line ("Menu illustration fees: Who paid Bea, and for what work? Read her Studio fees sheet."). A wrong reason with the right treatment is marked on the reason alone. No buzz, no medal popup. Incomplete / unchanged / failed checks show one plain sentence; a failed save says nothing was graded.
3. **Correct.** Tap a ? tab: the record comes back half-fed with its saved choices and the note card's "Look again:" line; refile with the lever (neutral; its mark simply clears); the key reads **CHECK AGAIN**. A repeat mistake shows the idea (hint 2), never the answer line.
4. **Accept.** An all-right check is saved as accepted: ticks pop in, a short hold, the tabs sink into the rack, the next batch's first record slides onto the stand (≈0.7 s; any touch or interruption finishes it). Accepted records are read only; the INCOME / DEDUCTIONS windows open a read-only "what's in this total" list (CLOSE), and the originals stay readable in the stack.
5. **Summary.** After the last batch: the folded **Bea's Income & Expense Summary** comes flat out of the widened output mouth and stands up as a tent card on the (now empty) rack, front: Income ₱900,000.00 / Supported deductions ₱300,000.00 (0.42 s, rollers turning). The stand card says "Bea's summary · Ready on the machine's ledge" with **COLLECT SUMMARY**. Tapping the tent lifts the full summary into the one reader from the tent's own pose (front: the two totals; tap: entries with their originals, e.g. "Deduction · EXP-B26 · RENT-26 — Studio rent ₱180,000.00", and "Left out · Personal purpose / Already represented"); tap again to put it back. Preview owns nothing.
6. **Collect.** COLLECT SUMMARY saves the existing transaction first (chapter → BeaComputation, B-WORKSHEET, frozen result, 60 coins, +2 Trust). Only then does the card fly to the Case Folder, the saved Trust/coins announce once, one reward card shows ("First checks 8/9", Silver, 59 XP; "Gold needs 9/9 on the first checks."), and after Continue the player stands up; Bea walks to the office (existing phase-driven route). A failed save keeps the card on the ledge, retryable.

Restores derive everything from the save: a reload mid-batch shows the marks still valid, at Ready the tent is already on the ledge (no replayed arrival), after collection the table is empty and Bea is in the office; nothing re-awards.

### Changes (existing owners; no new framework, file or .meta)

- `Assets/Scripts/Level3/Level3WorksheetCheck.cs`: revised section (L3-worksheet-v2): registry/batch decisions, `RevisedTierFor`/`RevisedXpFor`, `LegacyGraded`, `MachineChecked`, `EvaluateBatch`, `CheckBatch` (open batch, draft-revision action identity, freeze once per batch, marks, accept, last → Ready), `BatchMark`/`RowMark`/`LookAgainIn`, `AllBatchesCorrect` (the collection gate), `SummaryTotals`, `SummaryEntries`, `SummaryRecord`, `ReferenceLines(state, definition)`, `BoardPage`; `Level3BatchEvaluation`. The board's `Check` refuses once machine checks began; `Acquire` gates on `AllBatchesCorrect`.
- `Assets/Scripts/RewardsSystem/PlayerProgressData.cs`: additive `Level3WorksheetResult.checkedBatches`, `lastBatch` (+ Copy/Repair; absent in older saves).
- `Assets/Scripts/Level3/Level3Checkpointer.cs`: `CheckBatch` (candidate → signed write → publish; a failed write returns no evaluation); guide flags `machineChecked`, `machineAccepted`, `machineLookAgain`, `machineSummary`; collect retry copy.
- `Assets/Scripts/Level3/Level3ChapterRules.cs`: `BatchFiledLesson`; quiet-restore lesson inference for the batch-check lessons.
- `Assets/Scripts/Level3/Level3ComputationRules.cs`: `WorksheetTotals` — the office's worksheet choices and support facts read the collected summary's exact totals ("Bea's summary · Income / Supported deductions"; legacy tapes reference-only).
- `Assets/Scripts/Level3/Level3CaseFolder.cs`: `SupportingRecords` (the summary, first of Bea's sheets, once collected); `RecordKey` includes it.
- `Assets/Scripts/UI/HUD/HudHotbar.cs` (shared, Level 3 branch only): the Bag page reads `ReferenceLines(chapter, definition)` and titles it Bea's Income & Expense Summary (a board-collected worksheet keeps "Checked worksheet" and its page).
- `Assets/Scripts/Level3/Level3WorksheetHints.cs`, `Assets/Resources/Levels/Level_03/worksheet.json`: optional `brief` line per scored decision and `LookAgain(id, repeated)`.
- `Assets/Scripts/Level3/Level3PaperMachineModel.cs`: inputs `BatchKey`/`Window`/`Summary`; batch card (position/title/status/body/one key, 0.04 m face, 0.062 m touch), tab marks, window touch surfaces, widened output mouth/ledge, the tent summary (front/back legs computed to rest on the rack rail), `ShowBatchCard`/`HideBatchCard`/`PressBatchKey`/`SetTabMarks`/`GatherTabs`/`ShowSummary`/`HideSummary`/`FlySummary`, note-card look-again line; five new motions inside the existing one-motion-per-part/Settle owner.
- `Assets/Scripts/Level3/Level3BeaTable.cs`: batch card states, `CheckOpenBatch`, `AcceptSequence`/`FinishReview`, totals list, `OpenSummary`/`EnsureSummaryPaper` (preview in the one reader), `Collect`/`StartCollect`/`LandCollect`/`ShowRewardCard`/`FinishCollectNow` (once), guide cues `MachineBatchChecked`, `MachineBatchAccepted`, `MachineSummaryOut`, `MachineSummaryRead`; interruptions settle through `CancelDrags`/`GatherAll`.
- `Assets/Resources/Tutorials/Level_03/guide.json`: machine-batch-filed → CHECK BATCH; worksheet-check → look again; worksheet-corrections → batch accepted; worksheet-ready → open, then COLLECT SUMMARY; worksheet-collected; worksheet-subtotals now waits for the retired board only. `objectives.json`: steps 4–5 text (triggers unchanged).
- `Assets/Scripts/Level3/Level3EditorFixtures.cs`, `Assets/Editor/Level3EditorFixtureMenu.cs`: earlier batches accepted through the real Check batch; First Office Calculation through the revised path (9/9); new **Bea Earnings Mistake** and **Bea Summary Ready**.
- `Tests/Level3Regression/Program.cs`: new Revised T06 section (87 checks); 4 earlier pins updated to the revised contract (T06 lesson triggers, Bag page call, office choice labels, first-office fixture 9/9). `Tests/VerifyLevel3Print.py`: T06 prints (117 → 202).

### Commands and results actually run (5 November)

- Baseline (last recorded, 5 November T05 follow-up): runtime 0 errors/21, Editor 0/10, Level3Regression 1670/1670, Controls 5734.
- `python3 -B Tests/run_level3_repair.py --build-only` during Pass 1: only the two expected missing table members failed, then 0 errors.
- First full run after the edits: 1664/1670 — exactly 6 earlier pins of retired contracts. 4 were updated to the revised contract (above); 2 were met by moving source instead: the collection settle moved into `GatherAll` (so `OnDisable`'s pinned shape stays), and the acceptance cue uses `sfx_stamp_press_01` so the T05 "no correctness cue in the table" guard stays file-wide.
- Final `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors/21 inherited warnings**, full Editor **0 errors/10 inherited**; Level3Regression **1757/1757** (Revised T06 **87**); Controls **5734**; Level2 scratch **1580** with the same **3 native skips**.
- Scratch driver on the same DLL (`more_suites.py`, session scratchpad): GuideRegression **937 PASS**; CampaignFoundationRuntimeRegression **16/16**; CampaignFoundationRegression **743/748**, the same 5 inherited source-text pins (L3-R15).
- `python3 -B Tests/VerifyLevel3Print.py` **202 PASS**; `VerifyLevel3Scene.py` **PASS** (3 inherited warnings; scene sha256 `3e8d29dfd93c…` unchanged); `VerifyGuideTargets.py` 14 paths, 0 missing; `verify_pack.py` **PASS** (6 implemented/internal-verified, 12 Planned); `verify_content.py` **PASS 343**.
- Pass 1 (domain/persistence): 2/3/4 decisions = 9; tier/XP table 0–9 wrong; incomplete checks name the missing record and freeze nothing; later/unknown/accepted/ready/pre-handover/stale-revision checks refused unchanged; first outcomes freeze once per batch and never improve (1/2 → corrected → accepted, still 1/2); wrong-reason-only marking; one result after the third batch (7/9 Silver 58); four full runs (9, 8, 5, 0 right → Gold 60, Silver 59, Bronze 56, Bronze 51) all reach Ready after correction; the board can no longer grade; legacy v1 frozen chapter corrected through the machine keeps 11/12 and its label; an unchecked legacy draft gets its first v2 grade only from a deliberate check; a board-collected worksheet keeps its page and skips the machine. Signed-store mirror: failed check write (no marks, disk bytes unchanged) → retry → duplicate (no write) → accepted once → reload at Ready (Resume, possessions untouched, no labels/answers in the save) → failed collection (nothing granted, still Ready) → retry (60 coins, +2 Trust, Silver 59 once) → duplicate refused → reload (Resume at BeaComputation, AlreadyClaimed, possessions kept).
- Pass 2 (integration): summary front/back rows exact, no office answer printed; Bag page; office choices 900000/300000 from the summary, supported when chosen, legacy equal; Supporting Documents gains one summary; guide/objectives/hints content; four fixtures.
- Pass 3 (presentation/lifecycle): batch card rows, tent and mouth fit; projected targets at the modeled 2400×1080 seat: CHECK BATCH/COLLECT **55 dp**, summary **84 dp**, tabs 61–64, INCOME/DEDUCTIONS windows **35/38 dp**; no overlaps; windows clear of dial/keys. Wiring order guards (save before marks/flight/rewards; one AnnounceCampaign/AddSideQuest/ShowScreen; preview has no command); interruption settles; brief motions.
- Mutation probes (scratchpad `mutate.py`; each production file restored and byte-compared identical): corrections improve the first result → 8 failures; complete-but-wrong batch accepted → 26; legacy grade overwritten → 5; failed save returns marks → 1; Leave Out counted as a deduction → 4; stale check accepted → 1; reward announced twice → 1; office reads tapes only → 2. A redundant folder guard (`Collected` beside the `workIds` check) changed no result; it is kept as defence.
- Review fix found in Pass 3: during the acceptance the rack was being rebuilt from the newly opened batch (tabs would vanish before their ticks and gather); `RefreshMachine` now keeps the shown tabs while `reviewing` (new guard check). Leaving or opening the summary with the totals list up now refreshes the stand.
- Not run: Unity, Play Mode, rendering, native EventSystem dispatch, touch, audio, timing/feel, phone readability.

### USER manual script (Unity/device; not run here)

Choose Tools > TaxVenture > Level 3 > fixture, Play Office_Level3 directly (nothing saves), sit at Bea's table.

1. **Wrong answers — Bea Earnings Mistake.** The stand shows the batch card (no record). Tap CHECK BATCH once: Menu fees' tab shows "?", Logo fees' a tick; the card reads "1 of 2 right · 1 marked ?" with one short line. Nothing names Income. Tap CHECK BATCH again unchanged: "Nothing has changed…".
2. **Correction.** Tap the ? tab: the record rises half-fed with its saved choice and "Look again:" on the note card; Inspect Original works; choose Income, pull: identical neutral feed, the mark clears, key = CHECK AGAIN. Check: ticks pop, the tabs sink, the costs' first record arrives (one smooth beat). Try Settings / Leave Table during that beat: it settles, nothing replays.
3. **All batches — Bea Costs Batch / Bea Questions Batch.** File; on the questions choose Leave Out with a wrong Why? once: only the reason is marked. Tap INCOME and DEDUCTIONS windows: read-only list, CLOSE; accepted records cannot be recalled.
4. **Summary.** After the last acceptance: the folded card leaves the mouth, stands on the ledge (no paper through the plate, rack or table), front amounts readable at phone distance. Tap it: full summary in the reader; tap: entries and exclusions; tap: back on the ledge. Then **Bea Summary Ready** to see the restored ledge without the arrival.
5. **Collect.** COLLECT SUMMARY: the card flies toward the Case Folder, Trust/coins announce once, one reward card, Continue → stand up; Bea heads to the office. Repeat-tap Collect quickly (one collection). Interrupt the flight with Leave Table and with Settings (rewards once, card after Settings closes).
6. **Bag/folder readability.** Open the Bag: Case Folder page "… · Bea's Income & Expense Summary" lines; open the Case Folder → Supporting Documents: the summary sheet first among Bea's sheets, tap to read front/back.
7. **Office continuation — First Office Calculation.** At the desk the computation's totals offer "Bea's summary · Income ₱900,000 / Supported deductions ₱300,000" and Bea's draft; Bea waits in the conference room.
8. **Story route (fixture None).** Fresh story: three batches with one deliberate mistake, collect, quit, reload: no regrant, office continues. Sound on/off, reduced motion, mouse/Remote/phone.

### Next step

User acceptance of T04–T06, then **T07** (office stages A/B inside the existing Case Folder, consuming `Level3ComputationRules`' summary totals).


## 5 November — repeated tab/whole-paper misses: EventSystem priority and padded controls

User confirmed the two small filed-record cards are the intended review tabs and still cannot be clicked; the lower paper/Inspect and dial also need easier access. Earlier hit-plane geometry checks had not tested EventSystem arbitration. Inspected the installed uGUI EventSystem.RaycastComparer and GraphicRaycaster: module sortOrderPriority is compared before sorting/distance, and ordinary world-space raycasters use the base minimum priority while overlay canvases return their sortingOrder. Thus an overlay graphic can beat a geometrically correct machine hit. Exact live blocking-object identity still cannot be established without user Unity evidence; prior geometry-only results did not certify native clicks. The Interactable physics layer is not the authority for this direct UI raycast path, so no arbitrary scene-layer reassignment was made.

Modified `Assets/Scripts/Level3/Level3PaperMachineModel.cs`: Level3TablePaperRaycaster explicitly uses PointerPriority1000. Its existing CanTouchPapers guard scopes it to this seated activity; normal overlay HUD backgrounds no longer automatically outrank its hits. Floating document windows start1100, Leave Table2400 and camera transition shield32000 retain higher priority. Table originals use the same custom raycaster and keep their distance/document ordering over machine controls. The stand paper's full-face target is padded to0.256×0.192m, preserving its lower Inspect tap region and whole-paper drag. Review targets padded to0.096×0.066m with0.099m rack spacing, keeping neighboring tabs separate. Dial target grows0.082×0.078→0.104×0.084m; dark needle grows0.006×0.032→0.009×0.040m. Dial hit centre shifts0.006m down the plate so the larger region does not steal Deduct taps. No style/theme, domain, save, reward, source/scene/layer edits.

Modified `Assets/Scripts/Level3/Level3BeaTable.cs`: FreeToTouch uses !CameraController.InputHeld (transition AND arrival guard) rather than transition alone. Settings/dialogue/reward/pause gates remain. Modified `Tests/Level3Regression/Program.cs`: module-priority declaration guard first failed on the previous implementation; after the fix, priority boundaries, padded paper corners, neighboring target separation and actual projected review tabs pass. First expanded dial projection caught Deduct/dial overlap; lowering its hit centre resolved it. Mathematical projection at the default modeled phone: paper168dp shorter side, dial59dp, review tabs61/62/64dp, with no live-target overlap. These are modeled values, not phone measurements.

Final `python3 -B Tests/run_level3_repair.py`: runtime0 errors/21 inherited warnings; full Editor0 errors/10 inherited warnings; Level3Regression1670/1670 and Controls5734 (8 inherited compile warnings). Scoped independent standards/spec reviews found no introduced actionable blocker; native Unity touch remains unverified. No Git, Unity launch/UI automation or real saves. Documentation-only follow-up updates this file and KNOWN_ISSUES.

User acceptance remains pending: fresh Play with Bea Table Restored, drag/tap all paper regions including lower Inspect, rotate from the enlarged dial rim, file both records and click both review tabs to open/return the original and refile; ensure neighboring controls, Leave Table, Settings/arrival/focus guards and floating-original priority remain correct. T06 remains next; these tabs review/recall, not batch checking.


## 5 November — repeated Inspect/tab failure: physical hit planes and whole-paper pickup

User's latest rendered test still found stand Inspect and review tabs unresponsive and only the paper's top reliably draggable; pickup and machine-drop animations looked snappy. The previous click-guard repair did not establish user acceptance. This follow-up changes the actual machine surface construction/hit path rather than adding another click-phase workaround.

Modified `Assets/Scripts/Level3/Level3PaperMachineModel.cs`: AddSurface now creates/configures the WorldSpace Canvas BEFORE assigning final anchor/pivot/position/rotation/size. Each machine touch surface also binds its physical parent, plane transform and dimensions to the existing Level3TablePaperRaycaster. Raycast uses cached parentToPlane × current parent.worldToLocalMatrix and metre-sized bounds; native Canvas/RectTransform initialization or layout no longer controls the physical hit region. Resizing the note button updates the binding. Original-record surfaces keep the existing unbound RectTransform path, with no shared L1/L2 changes. Source construction order was unsafe and its new regression assertion failed first; runtime Canvas mutation and actual click routing still require user evidence, not a claimed automated Unity reproduction.

The stand card now has ONE full-face touch surface. A lower-strip tap maps to Inspect Original; a body tap keeps the tap-then-intake alternative. A drag starting anywhere on the paper, including the Inspect strip, is the same card gesture. There are no overlapping body/button raycasters or an untouchable seam. Review tabs and note Inspect use the same physically bound factory, so their planes follow actual visible parents and scale rather than Canvas bootstrap coordinates.

Pickup now has an explicit0.18s eased lift/turn/scale transition before the existing short-lag follow, replacing the immediate near-camera chase. A dragged machine drop first aligns above the slot over0.24s, then slides along the feed frame over0.16s, with a continuous world-scale transition. Off-target drops return over0.32s; tap-fed path, lever/save/neutral effects, original return targets and current palette/font/style remain unchanged. Chained motions use the existing single motion owner and settle synchronously on interruptions. No batch-check/collect/reward work: T06 remains next; T05 tabs review and recall existing classifications.

Modified `Tests/Level3Regression/Program.cs`: Canvas construction-order guard (red before fix), full-face card grid/Inspect-region mapping, full tab grid and outside bounds checks exercise the same physical HitPlane used by Raycast. Projection now treats Inspect as a tap region inside the one full-card target rather than a second rectangle. Current runtime and full Editor compile passed0 errors (21/10 inherited warnings); final current suite run `python3 -B Tests/run_level3_repair.py --suites` PASS1661/1661 Level3 checks and5734 Controls (8 inherited compile warnings). Two independent scoped standards/spec reviews found no introduced actionable blocker; neither ran Unity. No Git, Unity launch/automation, real-save or scene edit.

User acceptance still pending: stop/restart Play with Bea Table Restored; tap the lower Inspect strip, then close/reopen the original; drag from the amount, purpose, lower text, corners and Inspect strip; release off-machine to return and over-machine to align/insert; file both records and tap each review tab; verify recalled source visibility and return/refile; interrupt pickup/alignment/slide/recall with Settings/focus/Leave Table. Confirm restrained smooth motion and unchanged readable amounts/HUD clearance on the phone. No rendered/native-touch/feel pass is asserted from managed/static results.


## 5 November — T05 user screenshots: interactions, display depth and clear lever access

User reported Inspect Original not opening, awkward stand-card dragging, dial rotation failing, chopped amount glyphs, unclear review-tab behavior and the right inventory rail covering the lever. The screenshot does establish amount rendering defects/HUD obstruction; native interaction reproduction remains user-owned under AGENTS (no Unity launch/automation). Continued the current T05 implementation, without changing the palette/font/visual style or domain filing/save/reward rules.

Changed `Assets/Scripts/Level3/Level3PaperMachineModel.cs`: amount print was at0.0055m, exactly coplanar with the enlarged display slab's top; separated it to WindowTextY0.008m (2.5mm gap), including the totals tick's rest pose. Card drag now uses a camera-facing constant-depth plane, keeps a screen grab offset and projected size, follows with existing damping and stays within screen bounds. Hovering the machine previews acceptance but no longer steals the drag into a magnetically locked pose; release still chooses insert versus animated return. Fly now interpolates world scale back to the normal table scale for a continuous landing/insertion. Dial drag uses relative angular motion from its actual grabbed rim with DeltaAngle (including the lower half and atan2 seam), or direct upper-arc aim when dragged from the hub; existing neutral buttons, hysteresis, tap alternative and save boundary remain. Machine click handler relies on the UI module's eligibleForClick/dragging state instead of requiring a fresh Ended phase from a potentially advanced Unity Remote device. An explicitly matching Canceled touch is still rejected. Native pointer/cancellation behavior needs device evidence; this is not an assertion that its original failure was observed in an automated Unity run.

Changed `Assets/Scripts/Level3/Level3BeaTable.cs`: Inspect resolves the current flow row, including final-row recalls after the stand empties, reapplies its event camera/touch and retries an interrupted return rather than silently ignoring it. A settled open original still toggles closed. Review tabs retain their existing saved-choice recall and open the full original after the emerge finishes so the player can actually read it, then close the original to continue editing/refiling. Completion requires CanTouchPapers, so settling during Settings/focus/camera interruptions cannot open a new reader. Changed `Assets/Scripts/Level3/Level3TableStaging.cs`: machine shift0.055→0.075×scale, stand moves0.025×scale left to preserve the clear lane. The six-size containment/non-overlap checks still pass; the footprint may now extend into the middle lane, so the obsolete strict machine.xMin>0 assertion now checks its centre remains right of centre instead.

Changed shared `Assets/Scripts/UI/HUD/OfficeHudRuntime.cs` and `HudHotbar.cs`: existing HUD visibility owner fades the inventory IconButtons group while Bea's table is seated, disables its input immediately, and restores it on leaving/disable through the same predicate. Settings/Leave Table remain accessible. CanOpenPanels also blocks the Bag during this focused activity. No possessions, slot contents or persistent visibility are changed; L1/L2 don't meet the Bea-seated predicate.

Changed `Tests/Level3Regression/Program.cs`: added depth-gap, real relative rim/hub/end-stop/seam and final-record recall checks; retained atomic filing/failure/duplicate/mode/legacy suites and projected non-overlap. Final `python3 -B Tests/run_level3_repair.py --level2`: current runtime0 errors/21 inherited warnings; complete Editor0 errors/10 inherited warnings; Level3Regression1626/1626, Controls5734 (8 inherited compile warnings), Level2Regression1580 PASS (3 native EventSystem comparer checks explicitly skipped in disposable scratch source). `python3 -B Tests/VerifyLevel3Print.py`: PASS117. No Unity launch/UI automation/Git/real-save operations.

User acceptance pending: restart Play with Bea Table Restored; tap Inspect on both stand and note card; close/reopen during return; drag card to all four screen edges and drop away from machine to animate home, then drag/release over intake for insertion; rotate dial from centre and different rim positions in both directions; check all amount glyphs before/after filing; file both records then tap each review tab to read its original and return/refile; verify hidden inventory rail, reachable lever, settings/focus/leave/re-entry and no duplicate saves. T06 remains next; batch check/collection/rewards were not implemented here.


## 4 November — T05 screenshot follow-up: larger controls and rotary feel

User requested the machine slightly left, a larger interface, three visibly raised category keys in the same left/centre/right order, a genuinely rotating pointer with selected-key feedback, and smoother card/lever handling. Modified `Assets/Scripts/Level3/Level3PaperMachineModel.cs`, `Assets/Scripts/Level3/Level3TableStaging.cs`, `Tests/Level3Regression/Program.cs`, this file and `KNOWN_ISSUES.md` only. Machine body is now0.34×0.17m (was0.28×0.12), footprint0.38m, column shifted0.055×scale toward the centre relative to its new width; the stand stays in its lane. Controls have raised cream key faces/dark sockets; the chosen key turns cyan, gains a border and depresses slightly. Dial diameter0.05→0.072m; category text105→135points, total values100→120, total labels90→110, chosen status85→105. Rack width0.24→0.30m with larger review cards and title/category print. Exact source values, filing/clock/save/reward owners and T06 scope unchanged.

Dial gestures now project onto the physical face plane and follow angular position continuously, with a centre dead zone, symmetric4-degree selection hysteresis, rear mechanical stop and eased release to the chosen detent. Category taps still choose directly. The card preserves a grab offset, follows with a0.035s time constant and aligns over the existing forgiving intake. Lever follow uses0.025s smoothing, keeps its initial stroke on re-grab, samples the release position and retains the0.55 commit threshold; tap parity and post-save stroke remain. Reduced motion bypasses follow lag; failed filing/duplicate protections remain through existing T05 seams.

Two-axis scoped source review identified a neutral centre-drag pointer mismatch and a lower atan2 seam jump. Fixed by starting held motion at the actual dial angle and retaining the previous angle through the hub/rear sector, without emitting a category there. Added runnable mapping/hysteresis/rear-seam checks; the four rear-seam checks first failed, then passed after the fix. Updated plate/hit projection checks to the actual new dimensions and added a45dp minimum for the three category keys/dial/lever. Existing containment/non-overlap checks across six mat sizes and card-path occlusion checks still pass.

Final internal evidence: `python3 -B Tests/run_level3_repair.py` PASS: current runtime0 errors/21 inherited warnings, complete Editor0 errors/10 inherited warnings, Level3Regression1620/1620, Controls5734 (8 inherited compile warnings). `python3 -B Tests/VerifyLevel3Print.py` PASS117 shipped-font geometry checks. At the modeled default landscape2400×1080 seat/2.47px per dp, category short sides≈46/48/52dp, dial56dp, lever49dp, with no overlapping live targets; these are mathematical projections, not measured phone results. No Unity/editor launch, native UI automation, Git or real-save operation.

User acceptance remains pending: restart Play with Bea Table Restored, verify the left shift/larger body does not cover originals or right HUD, readable raised keys/windows/review tabs, angular drag in both directions and key-tap parity, neutral hub/rear stop, selection indication, paper pickup/drop/return, lever short pull/full pull/tap, rapid input, and Settings/focus/leave/restore. Compare feel with reduced motion and sound on/off. T06 remains next; no batch check/collect/reward work in this follow-up.


## 4 November 2026 — T05: feed, select, Why? and lever-file on Bea's paper machine

Status: **Implemented, internally verified; user manual pending.** T05 only: batch checks, acceptance, the summary card and collection stay T06's. All T02/T03/T04 working changes (including T04's paper-depth, exposed-stack and touch-border repairs) are kept; T04 visual acceptance is still pending. No Unity/editor launch or automation, scene/prefab/package/asset change, generated/downloaded art or sound, or real-save access. One accidental no-op Git invocation is recorded under the commands below (L3-R66).

### Inspected before changing

- `Level3Checkpointer.CommitWorksheet(edit)` already runs candidate → signed write → publish; the draft keeps T02's four section keys, read through `RevisedDestination`/`BatchOf`. `BeaWorkIndependent` meant "a row has a section". Nothing stored batch acceptance. T04's machine was passive with its parts exposed.
- The stand card is 0.24 m wide but T04's body was 0.20 m with a 0.17 m slot, so the card could not be fed without shrinking (forbidden). The machine had to widen.
- Existing seams reused: T04's direct rectangle raycaster (`Level3TablePaperRaycaster`, seated/free gate), the one reader (`ClientRecordPaper` + `Level3TableStaging.Stack`), `GuideVoice`, `GameplayEvents.RaiseGuideCue`, `AudioManager.PlayEffect` with shipped clips, `WorldEmote.ReducedMotion`.

### What the player now experiences (seated at Bea's table)

1. **Read.** The first unfiled record of the open batch is on the stand. Tapping its body lifts it slightly and outlines the slot (neutral, no correctness); INSPECT ORIGINAL opens the original.
2. **Feed halfway.** Drag the card onto the machine (it aligns above the slot whenever the finger is over the machine), or tap card then slot. It lifts over the body and slides down the rear slot behind the lip (0.22 s), leaning back at the stand's angle with its position, title and amount above the lip. The stand then shows a note card with the record's purpose and details, INSPECT ORIGINAL and, on R5/R7, COMPARE. Nothing is saved, totalled or graded.
3. **Choose.** Tap Income / Deduct / Leave Out, or drag across the dial (one detent per step, stops at the ends). The pointer settles with a short weighted ease (0.10 s) and one soft click; "Chosen: …" updates. A new record starts at the neutral home; a recalled (or partly filed legacy) record starts at its saved choice.
4. **Why? (R6/R7 only).** After any treatment, a Why? row appears on the note card; tapping it lifts a card into the reading position with the five existing neutral reasons and CLOSE. No reason is preselected or marked.
5. **Pull.** Drag the lever down past about half its stroke and release, or tap it. With a choice missing it only gives a little and one line says "Choose a category" or "Choose a reason" (no buzz). Otherwise ONE `CommitWorksheet` edit (`Level3WorksheetRules.File`) saves treatment and reason together. Only after the save: the lever completes its stroke (one soft clunk), the rollers turn and the card pivots upright behind the lip (0.26 s), its review tab settles in the rack, the windows tick, and the next card arrives (ready about 0.48 s after the save). Identical for every choice, right or wrong.
6. **Recall.** Tap a tab: the card comes back up out of the slot with its saved choices; its saved filing stands until a replacement saves. Tap the loaded card to cancel (it feeds back in, unchanged); pull to refile (same slip replaced).
7. **Batch filed.** With every record of the open batch filed no card is spawned; Arthur says "This batch is filed. Tap a tab to look at a record again." Checking is T06's, so a fresh run cannot pass the earnings batch yet (L3-R43).

Back-out: tap the loaded card's header (or drag it out) returns a new record to the stand with its pending choices dropped. Leave Table/disable returns an unfiled card to the stand; settings, dialogue, focus loss, pause and camera moves end drags undecided and settle every motion (a feed completes to its saved state; nothing is filed again). Back keeps the existing global policy (L3-R62).

### Changes (existing owners; no new framework, file or .meta)

- `Assets/Scripts/Level3/Level3WorksheetRules.cs`: `Level3Filing`; `Treatments`/`TreatmentLabel`, `NoteForRow`/`NeedsReason`, lossless `SectionFor`; batch seam `RowsInBatch`, `BatchAccepted`, `OpenBatch`, `RowOpen`; `SavedTreatment`/`SavedReason`, `RowFiled`, `BatchFiled` (T06 seam), `AllFiled`; `ComparePartner` (shared transaction from content); exact-decimal `DraftTotals` (open/accepted batches only); the lever command `File` (case/acquired source/batch open and not accepted/treatment/reason vocabulary/R1–R5 no reason/provenance match/Duplicate/revision guard; writes slip section and note reason together; keeps a note's link; sets `drafted` once all seven are filed with both reasons).
- `Assets/Scripts/RewardsSystem/PlayerProgressData.cs`: `Level3WorksheetResult.acceptedBatches` (+ Copy/Repair). T05 reads it; T06 is its writer. Absent in older saves → empty.
- `Assets/Scripts/Level3/Level3TableStaging.cs`: `ActiveRow` limited to the open batch; `RackRows`, `RowOf`; `Stack` Compare pair (`OpenPair`, `Partner`); pure `Machine` flow (Empty/Ready/Loaded/Saving/Feeding, arm, feed, recall, withdraw, choose/step, reason, one `BeginFile`, `Saved`, `Fed`, `Reset`); `MachineWidth` 0.26 → 0.32.
- `Assets/Scripts/Level3/Level3PaperMachineModel.cs`: rebuilt machine (0.28 m body, rear slot with lip/rail, two ridged rollers, feed frame, dial with neutral home and three labelled detents, Chosen label, two windows, lever with pivot, three-slot rack with tabs), note card, Why? card, per-part touch surfaces on the existing raycaster with step-based gating and drag ownership, one motion per part (`Settle` snaps all, reduced motion snaps), `Level3MachineInput`, `Level3MachineTouch`. `Level3StandCardTouch`/`Level3TablePaperRaycaster` kept.
- `Assets/Scripts/Level3/Level3BeaTable.cs`: machine intents → flow → `PullLever` (the only save), `FeedCard`, `BackOut`, `Choose`, Why?, `RecallTab`, `CompareOriginals`, `FeedDone`; `RefreshStand`/`RefreshMachine` from the published draft; Compare reading poses; interruption/leave handling; `Cue` helper.
- `Assets/Scripts/Level3/Level3ChapterRules.cs`: `BeaWorkIndependent` = first saved filing (legacy placed rows unchanged); Why? lesson implied only once a reason was saved.
- `Assets/Scripts/Level3/Level3Checkpointer.cs`: read-only flags `machineLoaded`, `machineWhy`, `machineBatchFiled`; worksheet doc.
- `Assets/Resources/Tutorials/Level_03/guide.json`: `worksheet-intro` = Feed the record / Choose where it belongs / Pull to file (cues MachineFed, MachineChosen, WorksheetFiled); `worksheet-notes` = Why? hint; new `machine-batch-filed`; `worksheet-drafted` text; readme. `Assets/Resources/Levels/Level_03/objectives.json`: readme line only (WorksheetDrafted now means all seven filed with both reasons; steps/triggers unchanged).
- `Assets/Scripts/Level3/Level3EditorFixtures.cs`, `Assets/Editor/Level3EditorFixtureMenu.cs`: Editor-only **Bea Costs Batch** / **Bea Questions Batch** fixtures (earlier batches filed with the real command and marked accepted as a T06 stand-in; never saved).
- `Tests/Level3Regression/Program.cs`: new revised-T05 section (133 checks); 7 earlier pins updated to the T05 contract (T05 lesson cues, stand row limited to the open batch, table lesson wording, stand-card wiring, only-save-is-the-lever, buildup no-save outside the machine section, passive-machine message). `Tests/VerifyLevel3Print.py`: T05 labels.
- Documentation: T05 ticket, this entry, KNOWN_ISSUES L3-R21/R43/R46/R48 updated and L3-R58–L3-R66, README/AGENTS/TICKETS/SIDE_QUEST_MACHINE pointers.

### Commands and results actually run (4 November)

- Baseline: the last recorded run (T04 reader-depth entry above) was runtime 0 errors/21, Editor 0/10, Level3Regression 1475/1475, Controls 5734. The first run after the T05 edits failed exactly 6 earlier pins that described T04-era behavior (1469/1475); they were updated to the T05 contract, not weakened.
- `python3 -B Tests/run_level3_repair.py --level2` (final, clean rebuild): runtime **0 errors/21 inherited warnings** (none in edited files), entire Editor **0 errors/10 inherited**; Level3Regression **1609/1609** (revised T05 **133**); Controls **5734**; Level2 scratch **1580** with the same **3 native skips**.
- Scratch driver reusing the same runner on the current DLL: GuideRegression **937 PASS**; CampaignFoundationRuntimeRegression **16/16**; CampaignFoundationRegression (linked sources incl. `PlayerProgressData.cs`) **743/748**, the same 5 inherited source-text pins (L3-R15).
- `python3 -B Tests/VerifyLevel3Print.py` **117 PASS** (72 before; all T05 labels, reasons, tab titles in at most two lines, windows up to ₱1,254,000.00 measured with the wider 'P'); `VerifyLevel3Scene.py` **PASS**, 3 inherited warnings, scene sha256 `3e8d29dfd93c…` unchanged; `VerifyGuideTargets.py` 14 paths, 0 missing.
- Pass 1 (domain/persistence/duplicates): every treatment × row accepted neutrally; R6/R7 need a reason and save it with the treatment; R1–R5 refuse a reason; foreign/unacquired/unknown rows, later and accepted batches, ready worksheets refused unchanged; Duplicate and stale pulls write nothing; refile replaces one slip; legacy Leave Out kept losslessly; tampered provenance refused; a note's link kept; exact totals including wrong choices (354,000.00 with Bea's own wrong deductions); signed-store mirror with injected write failure (nothing published, disk bytes unchanged) → retry Attached → repeat AlreadyAttached → reload round trip without repair, possessions/coins/trust unchanged, no answer keys; legacy frozen v1 check refile leaves its grade untouched.
- Pass 2/3 (flow, geometry, projection, lifecycle): pure flow (neutral start, one filing while saving, failed save keeps choices, feeding blocks, recall/cancel/refile, interruptions); the full loop against the real command (no clock before the first saved filing); paper path inside the opaque body/plinth at 21 points of the feed; header above the lip; rollers straddle the visible paper; target rectangles apart; projected targets at the default seat (2400 × 1080): no overlaps in either state, sizes in L3-R60; contract timings; save-before-effects ordering; no branch on the choice; one motion per part; step-gated surfaces; drag ownership; interruption paths; shipped clips exist.
- Mutation probes (scratch backups, production restored byte-identical by `filecmp` each time): reason not written → 33 failures; batch gate removed → 1; revision guard removed → 3; new record not neutral → 8; failed save drops choices → 12; feed before save → 1; clock on any slip → 5; cue differs by choice → 2.
- Fixed during verification: a test-local name clash (CS0136), the stand card's body/INSPECT touch areas sharing an edge (now a 3 mm seam), a 15-word hint (trimmed to 14), "INSPECT ORIGINAL" overflowing a half-width tab (two lines at 105 pt), a two-declarator constant the print parser could not read.
- Each edited file keeps its own line endings (byte-checked): C# rules/checkpointer/chapter/fixtures/save data LF; table, staging, machine model, guide.json, regression and print verifier CRLF.
- Rule deviation: a no-op `git diff --no-index /dev/null /dev/null` was appended to one shell line by mistake; it read and changed nothing. No other Git command was run (L3-R66).
- Not run: Unity, Play Mode, rendering, native EventSystem dispatch, touch, audio, timing/feel, phone readability.

### USER manual script (Unity/device; not run here)

Fixture **Bea Table Restored** (earnings), then **Bea Costs Batch** (R3–R5, COMPARE on R5) and **Bea Questions Batch** (R6/R7 Why?, COMPARE on R7); Play Office_Level3 directly (nothing saves) and sit at Bea's table.

1. Drag the card onto the machine; drop it off target (it returns); tap card then slot. Header readable above the lip, no paper through the body/table, stack still reachable.
2. The selector starts neutral; tap each label and drag across the dial; Chosen updates; click feels weighted, not loud.
3. Pull without a choice (one prompt, no buzz); pull below the threshold (springs back); pull fully and tap-pull. Lever, rollers, feed, tab and next card read as one brisk motion; wrong and right choices feel identical.
4. Why? on R6/R7 for each treatment; INSPECT ORIGINAL and COMPARE return to the same pending card and choices.
5. Recall a tab, cancel it, recall again and refile; tabs never shuffle or duplicate.
6. Settings, focus loss (Remote), Leave Table mid-drag and mid-feed, re-enter: safe poses, no stuck lever/card, no tap-through; the fresh route (fixture None) starts Bea's clock only after the first saved filing.
7. Sound on and off; reduced motion; phone readability and reach of labels/lever (L3-R60).

### Next step

User acceptance of T04 and T05, then **T06** (Check batch, freeze first outcomes, write `acceptedBatches`, corrections, summary card and collection).

## 4 November — reader depth and separate rear record lane

User screenshot confirmed the world-rendered reader intersected foreground props at0.6m and the stack remained visually obstructed. ReadingPose now uses0.32m (at least nearClip+0.04m), recomputing scale to preserve its screen size and previous lowered screen centre. Repositioned envelope/stack in rear centre gap and machine/rack forward; scale fitting reserves rear envelope plus forward column depth. Sketch moved to rear outer-right margin. Existing table-local hit surfaces follow moved records, with the previous fitted border retained. No shared paper rendering/input changes.

First verification caught footprint overlaps; fitting corrected. Final `python3 -B Tests/run_level3_repair.py`: runtime0 errors/21 inherited warnings; whole Editor0 errors/10 inherited warnings; Level3Regression1475/1475, Controls5734 pass. Updated full-size scale expectation to actual1.2×0.72m mat while preserving containment/nonoverlap checks across six mat sizes. User acceptance remains pending: restart Bea Table Restored; originals should be directly exposed in rear centre gap, reader should appear in front of stand/machine at unchanged screen position/size; check taps/return/drag and initial buildup. No Unity launch/Git/real-save operation. T05 remains next.

## 4 November — exposed originals, lower reader and fitted touch border

Screenshot follow-up: shifted the rear envelope/original stack0.10×layout scale metres toward the open centre lane, away from the stand’s projected face. Opened reader moves down7% of camera pixel height for both ID and sheet, with horizontal centre, size and depth unchanged. Only Level3BeaTable’s original-paper touch surfaces enlarge to about108% of paper width (previous94–96%); their existing local raycaster follows actual transformed position/rotation/scale, preserving shared L1/L2 behavior. No scene/save/gameplay progression changes.

`python3 -B Tests/run_level3_repair.py`: runtime0 errors/21 inherited warnings, entire Editor0 errors/10 inherited warnings; Level3Regression1475/1475, Controls5734 pass. Footprint containment/nonoverlap checks pass, including the initial envelope pose. User rendered/touch acceptance pending: restart Play with Bea Table Restored, inspect stack exposure, tap paper edges, open/return ID and sheets and confirm lower reader with unchanged horizontal position. No Unity or Git operation performed; T05 remains next.

## 4 November — T04 screenshot layout follow-up

User reported the original stack behind the raised stand and tabletop items too small. Adjusted existing Level3TableStaging/Level3BeaTable only: original-paper width0.13→0.18m, envelope anchored against far-left mat margin, mat uses94% table width/90% depth (caps1.2×0.72m), uniform prop scale may reach1.2 when space allows, seated camera interpolation0.88→0.76 for larger projected props. Existing mat containment/nonoverlap/encounter checks now include1.2×0.72m. No new gameplay, save or scene edits. T05 still owns intake/selector/lever; T04 inspection remains active.

`python3 -B Tests/run_level3_repair.py`: runtime0 errors/21 warnings; whole Editor0 errors/10 warnings; Level3Regression1475/1475; Controls5734 pass. User acceptance pending: stop/restart Play with Bea Table Restored fixture, confirm originals visibly left of stand and clickable, larger stand/machine/rack, no mat/table clipping or HUD overlap; inspect/return originals and test standing buildup. Static footprint checks do not establish projected occlusion/touch acceptance. No Unity launch, Git or real-save operation.

## 4 November — concise office-case ticket refinement

User approved the Bea office storyboard/flow and requested short, non-overwhelming screen content with fun physical feedback. Added [OFFICE_CASE_PLAY](OFFICE_CASE_PLAY.md) and updated existing T07–T10 in place, plus T17’s whole-flow checks; T07 owns integration with T03’s existing lesson. Contract specifies active-stage composition, concrete copy limits/examples, on-demand source/rule/working, corrected canonical band reference, neutral sourced transfers, short print/clip motions, targeted returns and brief real-copy delivery. Rich source facts remain available; no answer autofill or legal-content deletion to hit word limits.

Updated pack pointers in README/AGENTS/SPEC/REVISION_PLAN/CONTENT/WORLD_AND_FEEDBACK/REUSE_AND_ARCHITECTURE/VERIFICATION/TICKETS, and required-file list in verify_pack.py. T03 status/evidence unchanged, T04 still next, T07–T10/T17 remain Planned; no gameplay/asset/save/Unity/Git operations. This task refines Bea case presentation; the earlier Bea-only release request still needs a coordinated company deferral/dependency/reward/closing revision, and no complete one-case release plan is claimed here. Documentation validation: initial verify_pack run identified T03’s word limit after an added pointer; that pointer was removed, leaving implemented T03 unchanged. Final `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py` PASS:18 tickets, unchanged lifecycle/dependency DAG,234 local links,122 existing source references,13 arithmetic fixtures and343 content assertions. `python3 -B Documentation/Level3TheNumbersTellAStory/verify_content.py` PASS343. Documentation only; no runtime/Unity/device acceptance performed.


[REVISION_PLAN](REVISION_PLAN.md), current SPEC/TICKETS/WORLD/REWARDS are authoritative for new implementation. Everything under **Historical progress log** below retains its original dates, warnings, counts and acceptance limits. It may describe mechanics now retired; those passages are not current implementation instructions.

## 3 November — approved side-quest machine documentation (current design)

User approved a compact paper-machine loop after reviewing the earlier storyboard: read → load halfway with exposed header → neutral selector → Why? on R6/R7 → pull/tap lever → save then full feed → recall tab/next → deliberate batch check/correct → folded summary Collect. [SIDE_QUEST_MACHINE](SIDE_QUEST_MACHINE.md) and its [updated storyboard](images/side-quest-machine-storyboard.svg) now specify detailed behavior, motion, source-backed reuse/material candidates, failure/interruptions, grading and Bag/folder/office handoff. Old direct-to-three-target/oversized-board presentation is superseded.

Existing T04/T05/T06 IDs/dependencies retained and tickets revised in place; all remain **Planned**. Last implemented remains **T03**, with its rendered/touch acceptance reopened and pending. Next implementation remains **T04**. No production source/assets/scenes/saves, Unity or Git operations in this authoring task. Summary remains existing B-WORKSHEET; source fixtures/rewards/legacy results unchanged. Modified local pack paths: SIDE_QUEST_MACHINE.md; images/side-quest-machine-storyboard.svg; existing tickets T04/T05/T06 and T07’s item-handoff integration note; AGENTS, README, REVISION_PLAN, SPEC, CONTENT, SCENE_REUSE, WORLD_AND_FEEDBACK, REUSE_AND_ARCHITECTURE, REWARDS_AND_VALIDATION, VERIFICATION, TICKETS, PROGRESS, and verify_pack.py. T07 retains its existing scope/status/dependencies. No earlier implementation or test evidence was replaced.

Documentation evidence: `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py` PASS (18 tickets; unchanged lifecycle counts: 3 implemented/internal-verified/manual-pending including retained T01, 15 Planned; dependency DAG/order, local links, existing source references, 13 arithmetic fixtures and 343 content assertions). `python3 -B Documentation/Level3TheNumbersTellAStory/verify_content.py` PASS343. New SVG parsed as XML with six frames; this checks file structure, not final rendered/device acceptance. Gameplay/build/Unity tests were not run for this documentation-only change. Remaining: implement T04 then T05/T06; user rendered/feel acceptance remains pending.

## Current actual implementation baseline

- T01–T08 were historically implemented/internal-verified/manual-pending. Existing scene/bootstrap/checkpointer/Bag, calculator, Bea/table/papers, source-linked worksheet, first-check/reward rules, exact computations and Type8 credit are reusable.
- T09 source is present/wired: Level3OfficeStart creates Audit/ReviewClock; Level3ReviewRules implements stamping/review/approval,100 coins, frozen values and copyAtAuditor. Level3Regression/Program.cs contains T09 assertions. This observation is not a new passing suite or manual acceptance. The source endpoint is BeaDelivery; no subsequent playable delivery/company/closing command was established by this audit.
- The outside activity/computation baseline still includes the old four-zone worksheet/note columns/subtotal calculator and leaf pending their numbered tickets. Revised T03 restored the real Case Folder; the screenshot follow-up reused Taxpayer Information and shared Level2 stamps. The calculator/paper regression repair below replaces its broken print/gesture routing; rendered acceptance is reopened.
- At the earlier audit in this chat, documentation validation failed CRLF rules and VerifyLevel3Scene failed exact equality with copied Level2 (plus5 warnings). This revision fixes documentation line endings; it does not modify the scene validator or silently certify current scene edits. T02 owns scoped static-contract reconciliation. **Resolved by revised T02 (3 November, below):** the obsolete equality became recorded-baseline WARNs; structural checks and genuine inherited findings stay.
- Level3Regression/Program.cs exists but its project file was not found during source inspection. T02 must locate/restore minimal runnable harness and build current source before reporting results. All previous counts below remain historical. **Resolved by revised T02:** csproj restored; current results below.
- Career3 is still Coming Soon. Source scene/production code/data/saves remain unchanged by this authoring task. No Unity, renderer/device/animation/audio/performance acceptance was performed.

## Design decisions now recorded

1. Outdoor side quest: one record at a time, half-feed/selector/lever machine, batches2/3/2 records, nine scored treatment/reason decisions, automatic chosen-entry totals, collectible summary for Bag/folder/office, one final medal. No calculator/subtotal tapes/claim columns/separate link form there.
2. Office case: same Case Folder with Client/Supporting Documents/Computation; exact source transfers; contextual first-use calculator teaching; three result stages with selected rule/provenance. No new folder/clipboard/stamp framework or float bridge.
3. Same existing stamps/Auditor, targeted correction, short copy explanation+actual delivery. No separate service-plan errands.
4. Shorter company comparison and same computation/review/delivery, less guidance. Qualified finding/message/wrap-up; optional existing250-coin computer with usable verified register. L4 transfer/printing and L5 integrated investigation remain future.
5. Visual simplicity/readability/physical3D/motion/input/audio checks in each feature slice. Target15–20 minutes, measured later, not a timing promise.
6. Old state migration keeps every possession, frozen v1 grade/XP, tapes, approvals/copies/clocks and reward claim. No reset, regrade, duplicate reward or live-save operation during authoring/testing.

## Ticket lifecycle after the revision

T01 remains retained historical content preparation with authority/manual gate open. Revised T02–T18 are Planned for changed acceptance; no boxes prechecked on the basis of existing files. This does not undo historical delivered code. Same18 IDs and paths remain.

Next implementation action (updated 5 November after T06): **T07**. Then serial T04–T09 migrate existing systems/presentation; T10–T18 finish the remaining chapter. Do not start T01 again or old T10 while leaving crowded prerequisite screens unchanged.

## Authoring changes and evidence

Updated existing AGENTS/README/SPEC/CONTENT/FIXTURE_CONTRACT/TAX_AUTHORITY/REUSE_AND_ARCHITECTURE/SCENE_REUSE/WORLD_AND_FEEDBACK/REWARDS_AND_VALIDATION/VERIFICATION/TICKETS and18 existing ticket files. Added REVISION_PLAN in this same pack; annotated the earlier Level3DesignProposal as historical. Preserved original tax/legacy tables and complete progress/issue logs. Updated documentation validators for current dependencies, nine-decision registry and versioned score fixtures, with separate legacy checks. No remote publication or production implementation.

Current authoring-check results are recorded after execution in the final entry of this file. Those checks cover documents only, not runtime adoption or legal/educator approval.

## 3 November 2026 — Revised T02: fresh entry without the budget practice, legacy continuation

Status: **Implemented, internally verified; user manual pending**. T03–T18 remain Planned; T03 was not started. No Unity launch, Play Mode, device, real-save or Git operation was performed.

### Actual state before changes (reconciliation)

- No .NET SDK is installed on this Mac. Compilation used Unity 6000.4.5f1's bundled Roslyn (`Unity.app/Contents/Resources/Scripting/DotNetSdkRoslyn/csc.dll`) on its bundled .NET 8.0.16 runtime (`.../NetCoreRuntime/dotnet`); the Unity Editor itself was not launched. Scratch build/run scripts live in the session scratchpad, not the repository.
- Unchanged source: `python3 Tests/make_scratch_build.py` → 535 files; csc of `Temp/ScratchBuild/Scratch.csproj` **0 errors, 385 warnings** (361 CS0649 serialized fields, 12 CS0618, 7 CS0414, 3 CS0169, 1 CS0184, 1 other; csc warning level 4, so not comparable with the historical `dotnet build` count of 27).
- `Tests/Level3Regression/Level3Regression.csproj` was absent and was restored (below). The unchanged source then ran **926/926**, including **67 T09 checks**: T09 Audit/ReviewClock/stamp/approval code compiles, is created by `Level3OfficeStart` (`EnsurePart<Level3Audit>`, `EnsurePart<Level3ReviewClock>`) and its rule/save/wiring assertions pass. This is source/rule evidence only; T09 presentation and Play Mode remain unverified.
- `python3 -B Tests/VerifyLevel3Scene.py` on the unchanged validator: **1 FAIL** (Office_Level3 bytes no longer equal the copied Office_Level2) and 5 WARN. Office_Level3 is now registered in EditorBuildSettings.
- Recorded saved-scene baseline (sha256, 3 November): Office_Level1 `f4564ecd…5275`, Office_Level2 `0a46b8d6…d433`, Office_Level3 `3e8d29df…b9c5`, Office_Level3.meta `86a1b249…979d`, EditorBuildSettings `3bfdcea7…751f`. None of these files was modified by T02.

### What changed

Production (Level_03 opt-in; Levels 1–2 paths unchanged):

- `Assets/Scripts/RewardsSystem/PlayerProgressData.cs`: `Level3ChapterCheckpoint.CurrentVersion` 2 (`LegacyVersion` 1) and `calculatorTaught`; `Level3WorksheetResult.assessmentVersion`; both in Copy/Repair. Signed save v5 unchanged.
- `Assets/Scripts/Level3/Level3ChapterRules.cs`: `AssignBea` is Reception → BeaRecords (same action id; ToolPractice keeps its enum value and its legacy exit). `Problem` refuses version 1 until continued and a version 2 ToolPractice. `Decide` adds `Continue`. `TryContinueLegacy` (copy-only; ToolPractice → BeaRecords under `Level_03/practice-retired`, never the practice completion; frozen first check labelled `L3-worksheet-v1`; `calculatorTaught` only from both practice tapes or any case tape). `BeaWorkIndependent`. Arthur's `bea-assignment` lesson implied from the handover on.
- `Assets/Scripts/Level3/Level3Checkpointer.cs`: Continue = candidate → `CommitLevelThreeCheckpoint` → live → `RestoreQuietly` (no event, reward or publish). A failed write keeps the saved snapshot, holds the chapter not ready with one notice and retries every 3 s; settings exit stops the retry without writing. ChapterAssignmentReceived fires on Reception → any later phase.
- `Level3ReviewRules.cs` (`ClockDue` reads `BeaWorkIndependent`), `Level3ReviewClock.cs` (comments), `Level3WorksheetCheck.cs` (`AssessmentVersion` written at the freeze), `Level3WorksheetRules.cs` (read-only `RevisedDestination`, `BatchOf`), `Level3DeskCalculator.cs` (the practice card shows only tapes attached before the revision; COMPLETE only with both).
- Content: `Levels/Level_03/cases/case_bea_001.json` (two reception lines now send the player to Bea), `Levels/Level_03/objectives.json` (practice step removed), `Tutorials/Level_03/guide.json` (four practice lessons replaced by one `bea-assignment` hint; notes updated).

Tests/tools: new `Tests/Level3Regression/Level3Regression.csproj` (existing scratch-reference convention plus a macOS Unity path); `Tests/Level3Regression/Program.cs` (practice states rebuilt as explicit version 1 fixtures; T04–T09 now run on the continued legacy chapter; new revision section); `Tests/VerifyLevel3Scene.py` (obsolete byte equality → recorded baselines as WARN; everything else unchanged); `Tests/CampaignFoundationRegression/CampaignFoundationRegression.csproj` + `ConsultationStubs.cs` (T09's `ReviewClockState` dependency: links `ReviewClock.cs`, `OfficeHoursMath.cs` and a matching `ReviewClockDefinition` stub). `CareerBoardRegression`/`ProgressionRegression` csproj were briefly extended and then restored byte-identical (`cmp`).

Not changed: scenes, prefabs, metas, packages, `Career/chapters.json` (Level_03 still `comingSoon: true`), Level 1/2 code, any real save. Each edited file kept its existing line endings (code/JSON LF in this checkout, documentation CRLF).

### Decisions within T02

- The saved worksheet keeps its four-section keys (lossless; Leave Out keeps which reason section). A revised view reads them through `RevisedDestination`/`BatchOf` (T05). An unchecked legacy draft gets no inferred grade; its first revised check is T06's.
- Bea's 16 active minutes start after the first worksheet row is placed (the first compulsory lesson's last step). T05 redefines `BeaWorkIndependent` for the slip batches.
- The retired practice code (`Level3PracticeRules`, the card, Bea's `duringPractice` line) remains as legacy-only/reference until T17 retires demonstrated-unused paths.

### Commands and results actually run (3 November)

- Production after changes: 535 files, **0 errors, 385 warnings — the same warning set as the pre-change baseline** (compared with line numbers removed).
- `Level3Regression` (out of tree, `Temp/L3T02/Level3Regression/Debug/net10.0`): **1064/1064** = T02 base 248, T03 76, T04 49, T05 148, T06 142, T07 102, T08 92, T09 67, revision 140. Intermediate runs: 14 failures pinned the retired practice route, then 8 objective-index/clock pins; all updated to the revision, none weakened beyond those pins. One new-fixture error (a reused state had moved on) was fixed by a dedicated fixture.
- Revision checks cover: fresh route and clock; legacy fixtures at reception, practice (none/one/both tapes), records, handover, draft, frozen check correcting, ready, collected, computation, credit with running clock, stamped, approved with copy atAuditor, future company computation and completed office: exact carry, assessment label, teaching marker, determinism, idempotence, signed-save round trip with possessions/claims unchanged, no answer keys; no repayment of 60/100 coins, trust or mastery; started clocks kept; eight unusable/newer snapshots Blocked and untouched; failure-injected continuation on a disposable signed store (both generations byte-identical, retry writes version 2, backup keeps version 1); wiring order.
- Mutation probes on scratchpad copies (production untouched): clock reset on continue → 7 failures; assignment back to ToolPractice → 12; wrong assessment label → 9; clock due at BeaRecords → 4; invented practice completion → 2.
- Affected suites: CampaignFoundationRegression **743/748** (5 pre-existing source-text pins on files T02 did not touch; L3-R15); CampaignFoundationRuntimeRegression **16/16**; GuideRegression **937 PASS**; DialogueRegression **1163 PASS**; AudioRegression **281**; ControlsRegression **5734 PASS**; Level1LivingPlazaRegression **77/77**; IntroCutsceneRegression **44/44**; SettingsRegression **136/138** and TrainingRegression **499/513** (all failures are CRLF pins on untouched files; L3-R18); Level2Regression **1580 PASS** on a scratchpad copy that guards only 3 checks calling Unity's native EventSystem comparer (the repo file aborts there; L3-R17); CareerBoardRegression and ProgressionRegression **do not compile since T09** (L3-R16).
- `VerifyLevel3Scene.py` **PASS, 3 WARN** (inherited missing `CaseDropDestination` script; two prefab-instance paths; reports the 2 inherited GameStateMachine components). `VerifyLevel2Scene.py` PASS, 2 WARN. `VerifyGuideTargets.py` 14 paths, 0 missing. `VerifyLevel2Assets.py` FAIL on LF line endings of untouched Level 2 files only; 11,940 unique .meta GUIDs.
- Documentation validators after this update: `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py` **PASS** (18 tickets: 2 implemented/internal-verified/user-manual-pending, 16 Planned; DAG/order; 197 local links; 122 existing source references; 13 arithmetic fixtures; 343 content assertions); `verify_content.py` **PASS 343**. Documentation only.

### Pending user acceptance (Unity/device)

1. With a disposable campaign that completed Chapter 2, enter Level 3: check in at reception; the receptionist sends you to Bea; Arthur shows one short Bea hint; no calculator task, budget card or Practise objective; Bea hands over her records.
2. Place the first worksheet row and confirm Bea's clock announcement only then.
3. With disposable pre-revision saves (practice, worksheet, collected, computation, approved): resume, confirm the same papers, grade, tapes, stamp/approval, copy at the Auditor and clock, with no new reward card, coin or trust change. Needs an isolated save route inside Unity (L3-19).
4. Desk card: absent for a fresh chapter; shows only earlier practice tapes for a continued one.
5. Reception/desk access, Back/settings/focus and readability on supported devices.

### Next step

User acceptance of T02 in Unity, then **T03** (existing Case Folder in Level 3 and first-use calculator teaching, which reads `calculatorTaught`).

## 3 November 2026 — Revised T03: the real Case Folder and the calculator's first office use

Status: **Implemented, internally verified; user manual pending**. T04–T18 remain Planned; T04 was not started. T02 changes were inspected and kept. No Unity launch, Play Mode, device, real-save or Git operation was performed.

### What was found

- `Level3OfficeStart` hid the desk folder (T02) and Level 3's bar Case Folder opened only the Bag's saved text pages: `HudHotbar.OpenViewer` falls back to them unless the real folder is in its HUD store (`FolderToHUDIcon`). T09's Auditor hand-in (`Level3Audit`, `ILevelFolder`) already read the carried inventory folder and never touches the store.
- Level 2 already had the whole pattern: `Level2CaseRecords` (Supporting Documents through `CaseFolderUI.InstancePages` + `AttachedRecordsPage`) and `Level2FolderHandoff.RestorePlacedFolder` (store resume). The folder's page 1 is always Level 1's editable "Taxpayer Information" (classification dropdowns), and its first open moves ReceiveCase → ReviewDocuments.
- The practice card model was hard-wired to the retired 100/20/8 budget.

### What changed

Production (Level_03 opt-in; one shared hook, null elsewhere):

- New `Assets/Scripts/Level3/Level3CaseFolder.cs` (+.meta), a Level3Gameplay part: from Bea's assignment (story: only if the campaign owns its folder) it puts the real desk folder into its existing store with `RestoreStored` (quiet resume path). Bar/Bag then open the existing `CaseFolder3DView`, which is read-only away from the desk (`Doc3DLayer`); the desk-stack return is unchanged. It sets page 1 "Client" (`ClientLines`: name, work once her ID/background is acquired, request, tax year/period, case number; text rows, read only) and page 2 "Supporting Documents" (`Level3RecordRules.Build` of the active case's acquired papers in `AttachedRecordsPage`, Level 2's review layout with a smaller card). It rebuilds them only while the folder is closed and idle, clears only selections taken from this folder (on close, leaving the desk, case change), and puts the calculator away when the folder opens.
- `Assets/Scripts/UI/CaseFolderUI.cs`: opt-in static `FrontPage` (used only when set and not a reference copy); page 1 renders a level page's `Render` when it has one. Only `Level3CaseFolder` sets it.
- New `Assets/Scripts/Level3/Level3ValueTransfer.cs` (+.meta): `Level3TransferValue` inside `DataValue.Value` (exact canonical decimal, case, source, label, year, content revision); `Wrap`, `FromPaper` (content rows/totals of acquired papers), `TryRead` (refuses float/text payloads, another case, other content, malformed or unheld sources; never judges correctness), `OwnsSelection`.
- `Level3PracticeRules.cs`: `Level3CardSpec`; `PracticeCard` (retired, legacy tapes) and `LessonCard` (Supply fund 1,250.00, Spent on ink 480.00, one slot "Left in the fund"); `LessonDue`, `Taught`, `CompleteLesson` (card sources only, a subtraction, exact, 770; saves `calculatorTaught` + `Level_03/calculator-taught` only).
- `Level3PracticeCardModel.cs`: prints a spec (same clipboard body, targets, tapes); `Destroy`.
- `Level3DeskCalculator.cs` / `Level3DeskCalculatorComputation.cs`: while the lesson is due, the example card stands in for Bea's sheet; the example has its own draft (`Calculator` switches instances), so her working is untouched. The slot's second tap calls `CommitCalculatorLesson`; only after the save does the tape fly and clip on. Then the tools settle and lift again with her sheet. Lifting closes the floating folder.
- `Level3Checkpointer.cs`: `CommitCalculatorLesson` (candidate → write → publish → cue); flags `calculatorLesson`, `calculatorTaught`; `EditorFixture` hook read only by direct Editor attempts.
- `Level3ChapterRules.cs`: `ReviewDocumentsState` refused in Level 3 (`FolderForeignStates`; shared legacy guard unchanged); `calculator-lesson` implied once taught.
- `Level3Audit.cs`: the Auditor takes only the carried folder (not one left on the desk stack). `Level3OfficeStart.cs`: `EnsurePart<Level3CaseFolder>` (setup version unchanged).
- Editor-only fixture: new `Assets/Scripts/Level3/Level3EditorFixtures.cs` (`#if UNITY_EDITOR`; Bea's first office use built by the real commands with the authored v1 worksheet plan) and `Assets/Editor/Level3EditorFixtureMenu.cs` (Tools > TaxVenture > Level 3 > Fixture: First Office Calculation / None; EditorPrefs), each +.meta with checked-unique GUIDs.
- Content: `Tutorials/Level_03/guide.json` adds `calculator-lesson` (three hint pages waiting for the player's source tap, = and attach); `computation-totals` now follows it.

Tests/tools: `Tests/Level3Regression/Program.cs` (three pins updated; new T03 section); `Tests/VerifyLevel3Scene.py` (desk folder and stack paths). Not changed: scenes, prefabs, existing metas, packages, Career (Level_03 `comingSoon`), Level 1/2 code paths, real saves.

### Commands and results actually run (3 November)

- Same tooling as T02 (Unity 6000.4.5f1's bundled Roslyn and .NET 8.0.16 runtime; out-of-tree outputs under `Temp/`, removed afterwards).
- Production: `python3 Tests/make_scratch_build.py` → 538 files (three new scripts not yet imported by Unity); csc **0 errors, 385 warnings — the same warning set as the pre-T02 baseline**. Editor scripts (`Level3EditorFixtureMenu.cs`, `Level3SceneSetup.cs`) compiled against UnityEditor + the scratch Assembly-CSharp: **0 errors, 0 warnings** (not executed).
- `Level3Regression`: **1145/1145**, of which **78 T03 revision checks**: Editor fixture (real commands, deterministic, Bea only, no answer data in the snapshot, Editor-only and direct-attempt-only); the lesson (exact 1,250.00 − 480.00 = 770.00 on the existing calculator; marker-only save; duplicate; nine refusals without change; version 1 refused; example numbers refused on Bea's sheet; unrelated amounts/ids); card specs; save round trip, quiet resume, failure-injected write and retry on a disposable signed store; carry rule; Client lines; record sets by stable id (0 / 9 / 10 with the certificate); the transfer boundary (accept, plausible wrong accepted, ten mechanical refusals, worksheet/tape ownership, exact decimals, no float path); the guard in all 16 phases with the shared guard unchanged; the guide; wiring order and ownership; unique new GUIDs. First run 1062/1067: five pins expected the setup version 3 (my bump was reverted), the retired-lesson id prefix and the old desk card line; they were updated, none weakened.
- Mutation probes (scratchpad copies): typed numbers accepted → 3 failures; folder carried without ownership → 1; float payload accepted → 1; lesson not implied when taught → 1; ReviewDocuments allowed → 16.
- Shared suites: Level2Regression **1580 PASS** (scratchpad copy guarding its 3 native EventSystem checks, as at T02); CampaignFoundationRegression **743/748** (the same 5 pre-existing failures as T02; the two folder-related ones pin literal CRLF in `CaseFolder3DView`/`SupportingDocumentsPageLink`, untouched); CampaignFoundationRuntime **16/16**; Guide **937 PASS**; Dialogue **1163 PASS**; Level1LivingPlaza **77/77**; IntroCutscene **44/44**; Audio **282**; Controls **5734 PASS**; Settings **136/138** and Training **499/513** (CRLF pins only). CareerBoard/Progression still do not compile (L3-R16).
- `VerifyLevel3Scene.py` **PASS, 3 WARN** (same inherited findings; Office_Level3 hash unchanged `3e8d29df…`); `VerifyLevel2Scene.py` PASS, 2 WARN; `VerifyGuideTargets.py` 14 paths, 0 missing.
- Documentation validators after this update: `verify_pack.py` **PASS** (18 tickets: 3 implemented/internal-verified/user-manual-pending, 15 Planned; 199 local links; 122 existing source references; 343 content assertions); `verify_content.py` **PASS 343**. Documentation only.

### Pending user acceptance (Unity/device), with the fixture

Import (three new scripts, one Editor menu). For steps 3–7 choose **Tools > TaxVenture > Level 3 > Fixture: First Office Calculation**, then press Play in `Office_Level3` (direct attempt, nothing saved). Set **Fixture: None** afterwards.

1. Fresh direct Play (no fixture): before reception the bar has no case folder to open (or, with a Chapter 2 preview, only its saved pages); after the receptionist's assignment the real folder joins the bar and opens as the 3D folder: page 1 "Client" (no dropdowns), page 2 "Supporting Documents" with "not handed over yet".
2. After Bea's handover: her nine papers on page 2; tap one out, read it large, put it back; page turns and typography match Levels 1–2; no paper fan or duplicate folder.
3. Away from the desk the folder reads and turns pages but nothing can be picked up; at the desk values can be selected; closing clears that selection.
4. Sit at the desk and tap the calculator: the example card (EXAMPLE · SUPPLY FUND) comes up instead of Bea's sheet; Arthur's three hints follow your taps; typed numbers or the wrong order give a plain hint.
5. Attach to the slot (two taps): the tape clips on, then the tools settle and lift again with Bea's sheet. Put away/Exit Desk/settings before attaching, then return: the example is still there, nothing taught. After attaching, leave and return: no lesson again.
6. Open the folder from the bar while the calculator is up (the calculator settles) and lift the calculator while the folder is out (the folder flies back). Drop the folder on the desk stack, try the Auditor (reminder), take it back.
7. Readability, targets, safe areas, Back/focus, sounds on a supported phone; desk overlap between the folder stack and the tools (L3-R24).

### Next step

User acceptance of T02 and T03 in Unity, then **T04** (Bea and her originals with one calm readable record).

## 3 November 2026 — T03 follow-up: fix the Unity Editor compile blocker

User could not enter Play Mode. Editor.log identified CS7036 at Assets/Editor/DeskToolLayerValidation.cs:41: the diagnostic still called the former two-argument Level3PracticeCardModel.Build after T03 introduced the required Level3CardSpec.

Reproduced by compiling the complete Editor assembly with Unity's actual Library/Bee/artifacts/200b0aE.dag/Assembly-CSharp-Editor.rsp, redirecting only output/ref-output to Temp/T03EditorCompileCheck, using Unity6000.4.5f1 bundled NetCoreRuntime/dotnet + DotNetSdkRoslyn/csc.dll. Before: exit1, the same CS7036. Minimal fix: pass Level3PracticeRules.LessonCard explicitly in the existing validation caller. All Build callers inspected; runtime caller already passes DesiredCard.

After: same full Editor assembly compile exit0, **0 errors,10 existing deprecation warnings**. Output/logs: Temp/T03EditorCompileCheck/Editor.rsp, before.log, after.log. This compiles all Editor sources in Unity's generated response file instead of only the two T03 Editor scripts. No Unity launch, scene/asset regeneration, real-save operation or Git command. No runtime logic changed; user Play Mode/visual checks remain pending. VERIFICATION now requires whole-Editor-assembly compilation when shared runtime APIs change, so Editor diagnostic callers are included in the regression gate.

## 3 November 2026 — User screenshot follow-up: shared folder, free-position stamps, paper drag/drop

Status: implemented/internal checks passed; Unity/device/visual acceptance remains pending. User specifically authorized these T03 presentation fixes and early shared-stamp integration; later T04–T18 acceptance is not claimed. No Unity launch/automation, Git, real-save access, package, scene/prefab or source asset edit.

### Changes

- Level3CaseFolder returns the existing Taxpayer Information page with native Name/TIN ID/Classification/Type/Income Sources widgets. Acquired background supplies Professional/ProfessionalIncome through normal typed fields, without a new quiz. Unknown citizenship/TIN stay blank; the masked identity card is never fabricated into a TIN. Supporting Documents remain attached originals.
- Shared Level2DeskStamps has an optional per-instance ICaseStamp adapter; its default path retains Level2 rules/text/revisions. Level2StampDrag reuses the exact lift/grab/aiming ghost/drag/press/squash/return and calls that adapter once at contact. Level3OfficeStart now reuses this component; its calculator stops building the alternate stamp bodies. Level3CaseFolder binds the shared stand, current folder, readiness/revision and saved ink.
- Level3ReviewRules.StampAt records optional page/x/y/tilt through existing CommitComputation (save before ink). Level3StampRecord copies/repairs/serializes placement fields; invalid geometry clears only geometry, not readiness. The shared review clock waits for shared stamp motion. No new score/reward logic or filing transitions.
- Lesson amounts print at identical fixed1.35 size in full-width rows; destination is taller/wider and short result prints at fixed1.2. The printer strip is0.085m rather than0.175m long and uses larger fixed0.68 print; small tape face shows numeric working while token provenance/full working is retained. Lesson paper is right, calculator left.
- New Level3TapeTransfer and partial Level3DeskCalculatorTape adapt existing ValueDragSource/ValueTransferManager to printed paper (same command validators, no new manager). Existing tool pointer surface forwards shared begin/drag/drop/end through document layers; physical clone follows above finger, neutral destination outline, save-before flight/clip/pop. Invalid/cancel/focus/put-away ends held view without consuming draft. No floating-label duplicate; ValueDragSource.ShowVisual defaults true for all existing callers. Draft identity includes expression/source/manual/percent plus case/revision, not only matching result.
- Double-tap confirmation removed from lesson/practice/current computation slot handlers; printed tape drag/drop and single placement feed existing attach commands. Guide text updated. Full revised computation folder stages remain T07/T08 work; old computation leaf is still present until that ticket.

Modified runtime paths: Level3CaseFolder.cs, Level3OfficeStart.cs, Level3ReviewRules.cs, Level3ReviewClock.cs, Level3DeskCalculator.cs, Level3DeskCalculatorComputation.cs, Level3DeskCalculatorReview.cs, Level3PracticeCardModel.cs, Level3CalculatorModel.cs, Level3DeskToolHitTarget.cs, RewardsSystem/PlayerProgressData.cs, Data/DragDrop/ValueDragSource.cs, Level2/Level2DeskStamps.cs and Level2StampDrag.cs. New Level3TapeTransfer.cs / Level3DeskCalculatorTape.cs and unique metas. Content: Tutorials/Level_03/guide.json. Tests: Level3Regression/Program.cs (13 behavior assertions); Level2Regression/Program.cs (four source contracts updated for adapter indirection while asserting default fallback).

### Internal evidence

- Current runtime assembly compiled with Unity6000.4.5f1 bundled Roslyn using current Bee Assembly-CSharp.rsp plus the two new scripts, outputs redirected to Temp/Level3Polish:0 errors/21 existing warnings. Full Editor rsp against rebuilt runtime:0 errors/10 existing warnings. Initial Scratch.csproj helper invocation lacked current package references and failed; actual Bee response files supplied the complete current references. No Editor process launched.
- Level3Regression1160/1160 on current runtime, including13 new assertions for invalid geometry/no mutation, free placement/idempotence/move, copy/serialization/repair, no calculation/approval changes, known versus unavailable front-page facts and no invented TIN/citizenship. Existing save-failure/lesson/source/grade/reward/mode checks remain green. One obsolete bootstrap source assertion was changed to permit only shared DeskStamps, still forbidding Rosa Audit/Checkpointer.
- ControlsRegression5734 passed. Level2Regression1580 passed through the already-documented disposable scratch copy that skips exactly3 native EventSystem comparer checks; four direct-call source pins updated to verify shared adapter/default behavior. The initial four failures were those source pins, not runtime assertions. No claim of those three skipped native checks passing.
- VerifyLevel3Scene PASS with3 inherited warnings; scene hash remains3e8d29df…; no serialized scene edits. Documentation validators PASS:18 tickets,199 links,122 source references,343 content assertions. The two new script metas were checked unique across asset GUIDs. Logs/harness commands retained under Temp/Level3Polish. User must confirm 3D rendering/touch/feel in Play Mode.

### Manual checks

1. Use Fixture: First Office Calculation. Open folder: Taxpayer Information has the same five rows/style as Level2, known professional fields filled, no fake TIN/citizenship. Support originals still open/read/return.
2. Calculator lesson: compare1,250.00 and480.00 fixed size; paper right, shorter readable tape. Calculate770.00. Drag the actual strip onto enlarged Left in the fund field; neutral hover, flight/clip/pop, no double tap. Drop outside/cancel/settings/focus loss: no taught marker or lost draft. Successful attach teaches once in this attempt; no coins/XP/Trust.
3. After lesson, open Case Folder and use stand stamps. Drag READY/NOT READY, aim ghost and drop anywhere on its current paper. Same Level2 press animation, persisted position and current revision; moving/closing/cancel never creates duplicate ink/reward. Reopen page, inspect stamp. Edit computation and inspect stale readiness on next open. Auditor still reviews actual supported calculation.
4. Phone: amounts/field/tape readable, no tool/hotbar overlap; drag does not move the whole calculator, source remains after failed drop, no touch-through/stuck controls. Compare Level2 folder/stamps for shared regression. Live-save persistence still requires isolated story fixture; direct Editor is attempt-local.

## 3 November 2026 — T03 calculator/paper regression repair (current)

Status: **Code repaired, internally verified; rendered/touch acceptance reopened and pending.** User rejected the preceding screenshot-follow-up visuals. This bounded repair does not implement T04–T18 wholesale or accept T07/T08/T09. Existing Taxpayer Information and shared Level2DeskStamps/Level2StampDrag improvements are preserved. No Git commands, Unity launch/automation, source-scene/prefab/package changes or real-save operations.

### Red evidence and runtime trace

Inspected all four supplied clipboard PNGs (a0b8b03f,46021131,ada42e92,fceb8206): calculator left/paper right, invisible source amounts with working hit regions, overflowing giant tape print and movable overlapping tools. Traced Level3OfficeStart.EnsurePart -> Level3DeskCalculator.TryBuild/Update -> card/calculator print, focus poses, PollPointer/Pick, tape adapter, shared book/folder selection/drag/drop and attach/hand-off/cleanup. No hypothetical/device diagnosis is claimed.

Baseline current runtime/full Editor compile:0 errors,21/10 existing warnings. Baseline current Level3Regression1160/1160 and Controls5734 pass; these preceding screenshot failures prove they were not visual acceptance. The prior Temp/Level3Polish/compile.py was absent. The new reproducible helper discovers the current Bee response files rather than retaining a DAG id.

A deterministic red metric probe used the shipped HudFonts -> LiberationSans SDF face/glyph metrics and the installed TextMeshPro perspective multiplier0.1. At fixed1.35,1,250.00 advances0.52744m inside a0.132m rectangle, with a0.15524m line inside0.022m; attached1.2 likewise does not fit. The tape's0.68 gives0.26567m inside0.0616m. Amount ellipsis hid the digits, while tape Overflow spilled them. Parent tool scaling multiplies both rectangles and glyphs, so it cannot repair that mismatch. Text pivots/scales, physical surface depth, local bounds and composited renderer/hit bounds were inspected; native meshes/rendering were not run. ComputeLiftPoses explicitly swapped LessonOpen tools. PollPointer assigned most child hits to whole-tool movement, competing with EventSystem tape callbacks. The tape adapter additionally carried mutable component-backed payload state and a redundant cloned drag view.

### Repair and exact modified paths

- `Assets/Scripts/Level3/Level3DeskProps.cs`, `Level3PracticeCardModel.cs`, `Level3CalculatorModel.cs`: FixedPrint uses point-space rectangles, one explicit0.001 meter conversion and explicit perspective metrics. Both source amounts share160-point type on identical rows; attached770 uses150. Row/note/selection/target spacing fits the same existing clipboard. Tape uses70-point type, a bounded0.042–0.085m length, sufficient width, matching target/print bounds and no discarded working: optional long client chains page at the strip edge. ASCII minus comes from the baked atlas rather than dynamic fallback; exact payloads/working tokens stay unchanged. Native renderer states are preserved; no generic renderer re-enable loop remains.
- `Level3DeskCalculator.cs`: removes the lesson-only flip and focused whole-tool drag fields/fallback. Paper left, calculator right, short slip between, common front-surface depth, hotbar column reserved, guide/button clearance; same rest/focus/handoff direction. Polling only lifts resting tools or lowers the certificate. Committed flight finishes on interruption and follows its current paper destination; no animation callback owns persistence. Selection frame is omitted from the flight clone.
- `Level3DeskToolHitTarget.cs`: one focused EventSystem owner, pointer identity, shared drag threshold, drag suppresses click, off-target Up clears capture, explicit canceled-touch/focus checks, no parent repositioning. `Level3DeskCalculatorReview.cs` only replaces removed toolDragging in Busy with the shared drag lifetime; shared free-position stamps remain unchanged.
- `Level3TapeTransfer.cs`, `Level3DeskCalculatorTape.cs`: narrow surface adapters for amount -> existing display and tape -> existing field using IDataValueSource/IDataValueDestination, ValueTransferManager and ValueDragSource. Tap selects without inserting; tap display/place and drop invoke the same command. Both tape modes enter existing AttachLesson/AttachLeaf command boundaries; no double-tap confirmation. One shared floating visual, neutral hover/selection frames, no physical drag proxy/renderer hiding, no proxy hit-test layer. Outside/refused drop keeps the source/draft; cancel clears only owned selection. Subscription/drag cleanup is paired with disable/destroy/rebuild/leave/focus.
- `Level3ValueTransfer.cs`: immutable Level3CalculatorTransferValue snapshot in DataValue, exact decimal/source identity and current context; validates Number/key and the current authored/selected source. Existing acquired-paper Level3ValueTransfer remains intact. `Level3Calculator.cs`: monotonic InputRevision for stale captured prints, including changed working with the same final result; arithmetic/support rules unchanged. `Level3DeskCalculatorComputation.cs`, `Level3ComputationLeafModel.cs`: bind the existing totals/constants/attached results and destinations to that same adapter, retaining PICK/save/review behavior. No progressive folder computation rebuild.
- `Assets/Resources/Tutorials/Level_03/guide.json`: concise source-to-display and tape-to-field guidance describes both modes. `Tests/Level3Regression/Program.cs`: new command/actual-manager/failure-retry checks; two obsolete polling source pins now check the EventSystem owner. New `Tests/run_level3_repair.py` and `Tests/VerifyLevel3Print.py` provide reproducible disposable builds and shipped-font geometry checks. Documentation: README,REVISION_PLAN,REUSE_AND_ARCHITECTURE,WORLD_AND_FEEDBACK,VERIFICATION,T03,PROGRESS,KNOWN_ISSUES. Edited project text uses CRLF UTF-8.

### Executed checks

- `python3 -B Tests/run_level3_repair.py --level2`: current runtime **0 errors/21 inherited warnings**, whole Editor **0 errors/10 inherited warnings**. Uses Unity6000.4.5f1 bundled Roslyn and .NET8.0.16, no .NET SDK or Editor process. Generated files/logs under Temp/L3Repair, no Tests/*/bin output. The first suite-helper attempts exposed missing .NET references and package implementation DLLs; helper corrected to use framework references and current ScriptAssemblies implementation DLLs. Those harness failures are not production defects.
- Level3Regression **1183/1183**, including22 new repair command assertions plus one new signed-store/shared-manager failure assertion. Actual ValueTransferManager is driven with pure interface endpoints: selection has no insertion side effect, tap/drop command equivalence, exact sourced operand, no duplicate placement after selection consumption, rejected/foreign/changed-context drop retains selection, cancel/reselect, invalid data kind/key, six fractional digits, manual input distinction and same-result changed-draft rejection. Existing lesson marker-only/once-only, injected save failure/retry, compatibility, calculations, grades, rewards, approvals, clocks, inventory and mode checks remain green. Signed lesson failure now also runs through the actual shared manager; only successful persistence clears its selection.
- ControlsRegression **5734 PASS** (8 pre-existing compile warnings). Level2Regression disposable copy **1580 PASS with exactly3 native EventSystem comparer checks skipped**, same limitation as before; repo Level2 test source is untouched. No skipped assertion is claimed passing.
- `python3 -B Tests/VerifyLevel3Print.py`: **7 PASS**, real baked font advances/full line height plus4% bold allowance.1,250.00 uses0.06501/0.132m width and0.01840/0.022m height;480.00 shares the same size; eight tape lines fit0.06439/0.067m. The largest permitted displayed tape number fits0.08575/0.09240m. Offline evidence only, not native TMP mesh/culling/render/device acceptance.
- VerifyLevel3Scene **PASS,3 inherited warnings**; VerifyLevel2Scene **PASS,2 inherited warnings**; Level3 scene sha256 still3e8d29dfd93c2efc… . Content validator **343 PASS**. Pack validator **PASS**:18 tickets (3 implemented/internal-only,15 Planned,0 User accepted),200 links,122 source references,13 arithmetic fixtures,343 content assertions. Initial documentation check caught T03 above its1250-word budget; shortened ticket evidence to1221 words, keeping detailed history here. Historical1160/1160,5734,1580-with3-skips and all earlier warnings/counts above remain unchanged as history.

### Short user manual route (all pending)

1. Select **Tools > TaxVenture > Level 3 > Fixture: First Office Calculation**, open Office_Level3 and Play directly. Sit at the consultant desk and lift either tool. Confirm paper LEFT/calculator RIGHT/tape in gap, both1,250.00 and480.00 visible at identical size, no guide/hotbar/Exit/Put-Away overlap throughout movement.
2. Tap Supply fund: only its neutral selection appears. Tap the calculator display to insert; choose−. Drag Spent on ink to that same display, then=. Check compact working and770.00 within tape edges. Repeat with the two transfer modes reversed. A held/wobbled/dragged amount/key/display/field must never move either tool or produce a second action.
3. Drop tape outside, then try an unsupported/changed draft: no completion/reward/source loss. Print valid working again; drag tape into Left in the fund (also test select tape then tap field). Confirm neutral hover, save-before-clip/settle, one readable drag visual, once-only teaching and unchanged client working when Bea's sheet replaces the example on the LEFT. Longer optional client working pages remain within the strip. Check familiar folder rows and shared stamp aiming/ink restore.
4. On mouse and Unity Remote/phone test real drag, slight wobble, canceled/off-target touch, focus loss, Settings, Back, Exit Desk, put-away/reopen and orientation/resolution change during selection/drag/feed/attachment/return. No stuck hover/hold, stale selection, phantom key, tap-through, duplicate/lost attachment or overlapping tools. Stop Play and return the fixture to **None (fresh start)**. This Editor fixture is local/attempt-only and cannot prove live-save persistence. All native/device/audio/visual acceptance remains open.

Next numbered ticket remains T04 after user acceptance of these repaired tools; later tickets are not marked implemented by this repair.

## 3 November 2026 — T03 label readability and inactive touch surface follow-up

User screenshot `codex-clipboard-f828e31d-8112-497b-85b7-30d08e37e559.png` confirms the **amount size is good**, but rejects tiny labels and inability to click/drag/interact. This is partial user evidence, not acceptance of all rendering/input. No Unity launch/automation, Git, real-save, package, scene or prefab operation.

Cause: the new fully transparent Image on the focused tool surface left CanvasRenderer.cullTransparentMesh enabled. Installed UGUI GraphicRaycaster skips any graphic whose canvasRenderer.cull is true **before** its raycast filter/handlers. The existing working ClientRecordPaper touch face explicitly disables transparent-mesh culling. The earlier standalone shared-manager tests could not exercise that native rendering/input path. A new construction-contract assertion went red on the omission (1183/1184); this source guard is not a native raycast test.

Changed code:

- `Assets/Scripts/Level3/Level3DeskCalculator.cs`: sets `image.canvasRenderer.cullTransparentMesh=false` on the actual focused hit surface, retaining the single EventSystem owner; initializes movement/cancellation bounds. No legacy focused polling is re-enabled.
- `Level3PracticeCardModel.cs`, `Level3DeskProps.cs`, `Level3PracticeRules.cs`: source and destination labels use measured fixed140-point text; note/status95; wrapped guidance90 in a taller footer. Amount160-point text and its positions stay unchanged. Status replaces the note rather than competing for footer room. Note keeps "Not Bea's · No score". The point-to-meter helper now supports wrapping without shrinking. Existing title/body/materials retained.
- `Level3CalculatorModel.cs`, `Level3ComputationLeafModel.cs`: explicit roll/clip movement colliders on existing geometry. `Level3DeskToolHitTarget.cs`, `Level3DeskCalculatorTape.cs`, `Level3DeskCalculatorReview.cs`: the same pointer owner distinguishes shared value/tape drag from handle-only tool movement; uses HeldOnScreen with separate reading columns and HUD clearance. The area already includes gaps, so its wrapper avoids applying the shared12px margin twice on a nearly full phone column. Release/focus/Settings/disable/orientation still clear captures. No value/key/display/destination has a parent-drag fallback; arithmetic/provenance/save commands remain unchanged.
- `Tests/Level3Regression/Program.cs`: transparent-hit construction guard plus12 actual managed area/clamp tests over landscape, portrait and offset viewports, with nearly full tool bounds/extreme drags. An initial geometry assertion used Rect.Contains, which excludes a rectangle's maximum edge; corrected to inclusive bounds with0.001pixel tolerance, without altering runtime geometry to satisfy the test. The existing Note assertion caught a shortened label dropping "Not Bea's"; the note now retains that distinction.
- `Tests/VerifyLevel3Print.py`: extends actual shipped-font checks to labels/note and full wrapped lesson/refusal/save-retry messages. Source label line0.01610m fits0.018m; three footer lines0.03105m fit0.033m. Larger labels/guidance fit without changing accepted numeric size. Documentation updated: current T03, REVISION_PLAN, WORLD_AND_FEEDBACK, KNOWN_ISSUES and this entry.

Verification: current runtime and entire Editor compile with0 errors and21/10 inherited warnings using Tests/run_level3_repair.py. Final current-DLL results: Level3Regression **1196/1196**, Controls **5734**, Level2 scratch **1580** with exactly **3 native EventSystem comparer checks skipped** (same limitation, repo Level2 source untouched). Shipped-font geometry **29 PASS**, including larger labels/note and complete wrapped instructions/refusals. Pack validator **PASS**:18 tickets,215 links,122 source references,13 arithmetic fixtures,343 content assertions. Prior counts above remain history; these checks do not certify native input or rendering. All outputs remain in Temp/L3Repair. No native mouse/Unity Remote/touch/culling/mesh/appearance claim follows from these standalone checks.

Manual recheck: use **Tools > TaxVenture > Level 3 > Fixture: First Office Calculation**. Verify larger labels/note/footer at the unchanged number size; tap an amount then display, press keys, and drag amounts/tape to their targets. Drag the **paper's brass clip** and **calculator's roll** to reposition them; content must retain its own gestures. Check column/HUD limits, off-target/cancel, Settings/focus/Exit/reopen/rotation and once-only attachment. Return fixture to **None (fresh start)** after testing. Native/device acceptance remains pending; fixture cannot prove live-save persistence.

## 4 November 2026 — T03 direct tool raycasting and usable drag borders

User reports paper/calculator still cannot be clicked/moved/dropped. **The preceding culling change was insufficient; no native interaction acceptance is claimed.** Existing larger print and amount size/positions are retained. No Git, Unity launch/automation, real-save, scene/prefab/package operations.

Inspected actual target creation, TagManager, DynamicsManager, installed GraphicRaycaster/BaseRaycaster/handlers, camera and shared input guards. Targets already use Level2DeskStamps.DragLayer7 (`3DInteractable`); the collision matrix does not disable it. Root/prefab mesh layer0 is not an unbound monetary/input source. Simply changing another layer would not repair this routing.

The focused ingress depended on a transparent Image/filter, while target selection queried physics after moving the tools; this project has autoSyncTransforms disabled. A separate `applicationFocused` latch was also required by the focused hit filter even though fresh resting input could lift the tools without it. Delivered Remote input after editor focus loss could therefore reach the lifted view but remain rejected. OnApplicationFocus/OnApplicationPause still release captures and settle tools; no persistent focus latch now rejects subsequent fresh events.

Repair:

- `Assets/Scripts/Level3/Level3DeskToolHitTarget.cs`: subclasses the existing GraphicRaycaster and overrides its raycast on the actual Doc3DLayer root. It returns the tool layer as the EventSystem target at its document sorting order, using current tool-target geometry. No transparent Graphic/canvas-depth/alpha/culling gate remains. Ordinary pointer/click/drag/drop callbacks, identity/thresholds/cancel and ValueDragSource/ValueTransferManager remain owners; no second polling path or transfer manager. Base OnDisable unregisters the raycaster. Shared MobileInputGuard still recognizes this subclass as UI, preventing world tap-through.
- `Level3DeskCalculator.cs`: CreateToolLayer binds that one component directly instead of a GraphicRaycaster plus invisible full-screen Image. PickTool measures enabled current local BoxCollider sizes/centres and transforms beneath the existing tool roots; no stale physics broadphase, layer-mask or cached world collider bound governs focused hits. Existing Doc3DLayer order still blocks tools behind another document. Physics is retained only for the original desk-surface placement. Focus latch removed, cancellation kept.
- `Level3DeskToolHitTarget.HitBox`: bounded pure ray/box math preserves world distance across scaled/rotated tools by leaving the transformed direction unnormalized. `Level3PracticeCardModel.cs`, `Level3CalculatorModel.cs`, `Level3ComputationLeafModel.cs`, `Level3DeskCalculatorTape.cs`: add explicit navy-border/rim targets beside clip/roll handles. Dragging these repositions the tool within its reading column; releasing leaves it there. Printed values, tape, display, keypad and receiving fields keep their distinct gestures. Geometry families/materials/print/domain/save/stamps stay unchanged.
- `Tests/Level3Regression/Program.cs`: replaces the obsolete transparent-image source guard with actual raycaster-type registration coverage (red1195/1196 before repair). Adds9 executed production hit-math assertions: front face/world distance, immediate old-position miss/new-position hit after translation without a physics step, scale-independent nearest order, rotation, parallel miss, behind-ray rejection, included edge and bounded distance. Two historical wiring pins now follow current local-box leaf picking and the already-shipped shared Level2 stamp owner. One missing System qualifier during incremental compile was corrected before the final full build; no claim that an intermediate broken build was green.

Verification: `python3 -B Tests/run_level3_repair.py --level2` compiles current runtime and the **entire Editor** with0 errors/21 and10 inherited warnings, respectively; outputs in Temp/L3Repair. Level3Regression **1205/1205**, Controls **5734**, Level2 disposable copy **1580** with exactly **3 native EventSystem comparer checks skipped**. The new hit maths and existing manager/command/save/failure/compatibility checks execute against the rebuilt DLL. These are not a native EventSystem/game rendering run. No live save is accessed. The updated edge-drag hint also fits: shipped-font geometry **28 PASS** (the shorter opening instruction uses two rather than three lines); pack validator **PASS**,215 links,122 source references,343 content assertions.

User recheck: **stop and restart Play Mode after Unity imports the code**, so runtime-created tool layers/targets are rebuilt; hot reload is not proof that their construction ran again. Then choose **Tools > TaxVenture > Level 3 > Fixture: First Office Calculation**, Play Office_Level3 and lift the tools. Grab either navy border/rim (or paper clip/calculator roll), move and release; tools should remain in their columns. Tap/drag Supply fund to the display, choose−, tap/drag Spent on ink, press=, select/drop the printed tape in the field; off-target release must retain the draft. Repeat after editor focus loss on Unity Remote, Settings, Exit/reopen and orientation. Return fixture to **None (fresh start)**. Native click/drag/drop, visibility, touch and audio acceptance remain pending; Editor fixture cannot prove live persistence.

## 4 November 2026 — Revised T04: Bea's compact records and the paper-machine table

Status: **Implemented, internally verified; user manual pending.** T04 only: T05 filing/lever and T06 checks/output/rewards are not started. All T02/T03 working changes (calculator dragging, receipt motion, foreground layering, folder, stamps) are kept untouched apart from the one `AwayAtBeaTable` line below. No Git commands, Unity/editor launch or automation, scene/prefab/package/asset import, or real-save access.

### What was found

Acquisition already satisfied the contract: `Level3BeaInteractable.HandOver` runs only when the introduction completes, calls `Level3Checkpointer.CommitBeaRecords` (candidate → signed write → publish), and B-WHT is not in `BeaHandoverSources`. The table then fanned nine papers across the far mat, built the old worksheet board on the near half (`Level3WorksheetDesk`, `LiftScreenHeight` 0.8) and moved the chapter's calculator to the table. Candidate assets: `SM_Prop_CardRoller_01.fbx` is one mesh with no sub-parts or animation; `printer1.fbx` (102 MB) embeds a camera and light. Neither has an intake, selector or lever.

### What changed (exact paths)

- `Assets/Scripts/Level3/Level3BeaTable.cs`: the nine existing `ClientRecordPaper`s are stacked on the opened envelope (back left) instead of fanned; one reader (stack tap = top paper, read paper goes to the bottom; stand-card tap = that record's original; opening one returns the other; tap again or drop on the table returns it; ID card turns first). Centred reading pose. Stand card refreshes from the published phase/draft revision. Settings/dialogue/pause/camera transition disable touches and cancel drags; focus/pause cancel drags; Leave Table/disable gather everything. The worksheet board is no longer built; `HoldPapers` stays only so the unbuilt class compiles. Seat viewpoint now comes from the layout. Built-in Bea mirrors the new introduction.
- New `Assets/Scripts/Level3/Level3TableStaging.cs` (+ `.meta`, unique GUID): pure layout (stack, stand, machine+rack column inside the mat, apart, each placed by its footprint turned the most it may turn, ≤10° toward the seat), `TableSources` (no B-WHT/repeats), `ActiveRow`/`Face` (first unfiled row; printed label/amount/purpose/issuer·period·reference; no source id or treatment), `Stack` (one original out).
- New `Assets/Scripts/Level3/Level3PaperMachineModel.cs` (+ `.meta`): `Level3DeskProps` primitives only. Stand with a 0.24×0.16 m card at 35° and one world-canvas tap surface (cull-transparent off, drag/cancel/settings/dialogue filtered); passive machine (cream body, navy plinth/front plate, intake slot with lip and two rollers, brass dial with Income/Deduct/Leave Out detents and a neutral pointer, lever hinge raised, two blank total windows, output ledge) and a three-slot rack on the same root. No colliders or input on the machine; parts exposed for T05.
- `Assets/Scripts/Level3/Level3BeaInteractable.cs`: handover notice wording only. `Assets/Scripts/Level3/Level3DeskCalculator.cs`: `AwayAtBeaTable` returns false (the table has no calculator in any phase).
- `Assets/Resources/Levels/Level_03/town_life.json`: introduction's last two lines shortened to the invitation to prepare the records at the machine. `Assets/Resources/Tutorials/Level_03/guide.json`: `bea-records` wording; `worksheet-intro` (same id) now two short hints (read the stand card's original → put back/stack/machine). `Assets/Resources/Levels/Level_03/objectives.json`: step 3 text only (trigger unchanged).
- `Tests/Level3Regression/Program.cs`: four obsolete pins (fan/board/board lesson/table calculator) replaced by revised contracts; new 60-check revised-T04 block. `Tests/VerifyLevel3Print.py`: stand-card and machine label checks from the case's printed values.
- Documentation: T04 ticket, this entry, KNOWN_ISSUES (L3-R28 updated, L3-R43–L3-R48), TICKETS/README/AGENTS next-ticket pointers. Edited files keep their own line endings; new files CRLF UTF-8.

### Commands and results actually run (4 November)

- Baseline before edits, `python3 -B Tests/run_level3_repair.py --level2`: runtime 0 errors/21 warnings, Editor 0/10; Level3Regression 1345/1345, Controls 5734, Level2 scratch 1580 (3 native comparer skips).
- First rebuild: runtime failed CS1061 ×3 (`HoldPapers` still called by the unbuilt `Level3WorksheetDesk*`); restored as a seam. First suite run: exactly the four expected source pins failed. First revised-T04 run: 10 layout failures — turned footprints overlapped/left the margin at an 18° clamp; fixed by reserving the turned footprint in placement, a 10° clamp and one machine+rack root (not by loosening the test).
- Final `python3 -B Tests/run_level3_repair.py` then `--suites --level2`: runtime **0 errors/21 inherited warnings**, entire Editor **0 errors/10 inherited warnings**; Level3Regression **1407/1407** (revised T04 60; historical T04 49, T05 148, T06 149 including the replaced pins); Controls **5734**; Level2 scratch **1580** with the same **3 native skips**. Outputs in `Temp/L3Repair`; the failed-save sandbox is `Temp/L3Repair/T04-<guid>` and is deleted after the run.
- Revised-T04 checks execute production code: signed-store handover with injected write failure (nothing published, disk bytes unchanged, no table sources), retry (exactly nine, no B-WHT, possessions intact), two duplicates (no write), reload → same nine record ids for table and folder `RecordKey`; stack cycle/one-reader/unknown/B-WHT/close/CloseAll; stand row/face for all seven rows, restored partial draft, no leaks; layout on five mat sizes; copy/guide/objective; wiring pins; unique GUIDs.
- `python3 -B Tests/VerifyLevel3Print.py` **65 PASS** (27 new; ₱ measured as the wider 'P'). VerifyLevel3Scene **PASS, 3 inherited warnings**, scene sha256 still 3e8d29dfd93c…; VerifyLevel2Scene **PASS, 2**; VerifyGuideTargets 14/0 missing; verify_content **343 PASS**; verify_pack **PASS** (18 tickets: 4 implemented/internal-verified/manual-pending, 14 Planned; 236 local links, 122 source references, 13 arithmetic fixtures, T04 1008/1250 words).

### Pending user acceptance (Unity/device)

1. Play Office_Level3 from a fresh start (fixture **None**). Check in, talk to Bea, cancel once mid-introduction (nothing handed over), talk again to the end: envelope opens, papers settle into one stack, the card arrives on the stand.
2. Sit at her table: Bea's table edges and surroundings visible; stack back left, stand front left, machine right; guide, hotbar and Leave Table clear; card title, large ₱ amount, purpose and details readable.
3. Tap the card (B-REV original, centred), tap it back. Tap the stack repeatedly: one paper at a time, ID card turns then returns; tapping the stack with one out swaps it; drag one onto the table to return it.
4. With a paper out: open/close Settings, lose focus mid-drag (Unity Remote), Leave Table, sit again — no stuck paper, duplicate, clipping or tap-through; Bag/Case Folder Supporting Documents still list the nine papers.
5. Repeat on phone/Unity Remote for legibility and touch. Filing is not available yet (T05); the office remains testable with **Fixture: First Office Calculation**.

### Next step

T05 (feed/select/lever filing on this machine), after or alongside user checks of this slice.

# Historical progress log


# Level 3 progress

## 29 October 2026 — Ticket authoring

Status: **Planned — documentation only**. All 18 tickets remain unstarted. Existing proposal retained as design authority; this pack supplies execution scope, source inspection, state/content/reward contracts and separate acceptance.

Read the two user attachments, Level3DesignProposal, Campaign Foundation rules/current Level2 continuation/status/handoff/tickets, Level2 scene-reuse and toolkit patterns, source/controller/catalogue/scene/test entries named in REUSE_AND_ARCHITECTURE. Observed Unity6000.4.5f1, first-person exploration, existing calculator/computer props, placeholder calculation state/prototype rates, and bounded single-case assumptions requiring extension. Level2 is reported internally verified/access enabled; user acceptance still pending. No runtime readiness change follows from this inspection.

Authoring verification: see final entry below after documentation checks execute. No gameplay suites, scratch compilation, Unity/import/Play Mode/device/listening/performance checks were run for Markdown authoring. No production script, scene, prefab, model, animation, runtime JSON, package, .meta, actual save or Git operation is part of this task.

### Authoring verification completed

- Created the 18 individual tickets and 12 supporting Markdown files, including master index, inspected-source map, canonical content, scene/physical direction, reward rules and separate verification/limitations. Added a documentation-only validator; linked this pack from the existing proposal. All implementation statuses remain Planned and acceptance boxes unchecked.
- Executed `python Documentation/Level3TheNumbersTellAStory/verify_pack.py`: **PASS — 18 planned tickets; dependency DAG/topological order; 200 local links; 107 claimed existing source references; 12 arithmetic fixtures**. Later prose edits were revalidated. This does not execute or validate game behavior.
- Compared both source scene SHA256 values again: unchanged from the inspection values in REUSE_AND_ARCHITECTURE. No production file or actual save was intentionally written. Temporary documentation authoring helper was removed after generating the files; it is not a runtime feature or delivered dependency.
- Reviewed actual worksheet-scoring/approval/release tickets, master index, both-case reward/copy separation, explicit user-only scene acceptance, current-source versus proposed-path labels, required side-quest exception, and optional computer gating. Refined prose without changing code paths/IDs or financial fixtures.
- Required future content-authority approval and prior/new Unity/device manual gaps remain recorded; they do not prevent this documentation pack from being complete. Next remains T01 upon implementation instruction.

Next implementation ticket, only upon a new implementation instruction: **T01 — Validate and bind the bounded tax content fixtures**. No assumption that curriculum authority is already confirmed.

## Implementation evidence template

For each ticket record: status; exact source/content/assets changed; actual scene/route/source bindings; internal commands/results and baseline warnings; save/rollback evidence; unresolved rule/art/runtime limitations; USER-only checks pending; next ticket. Keep approval status separate from internal verification. Do not replace historical results with new counts or copy expected pass text as evidence.

## 30 October 2026 — T01 content preparation completed

Status: **Implemented, internally verified; user manual pending**. This status covers T01's bounded documentation/content exception only. T02–T18 remain Planned. No educator sign-off was provided and no graded-content release is approved. Next ticket is **T02**, only on instruction; it was not started.

### Delivered content and schema

- Added [TAX_AUTHORITY.md](TAX_AUTHORITY.md): twelve source-register entries with primary URLs and section/page locators, applicability, CREATE MORE/EDR scope, MCIT counting, expense substantiation, credits and later CMEPA check. Research date30 October2026. The original BIR CREATE MORE scan exceeded the browsing tool's fetch limit; the Supreme Court text and BIR RR7-2025 implementing digest supplied the inspected authority. Current BIR indexes are partly dynamic; research does not certify exhaustive future changes through the end of2026. Recheck before release.
- Added [FIXTURE_CONTRACT.md](FIXTURE_CONTRACT.md): two2026 annual contexts,17 source documents,29 monetary rows, transaction/support/subset identity, six explicit total/component bindings, two credit links, full six-band annual table, corporate constants/facts,14 expected calculation fields,12 worksheet decisions, ten wrong chains and midpoint/boundary fixtures. Defined optional L3 content block in existing case envelopes, decimal strings/decimal evaluation, explicit working-paper handling and separate immutable answers versus player state. No production DTO or resource was created.
- Preserved all adopted financial figures: Bea900,000 revenue,300,000 corrected costs,600,000 taxable,62,500 due,12,500 credit,50,000 payable; rough costs354,000. Marine8,000,000 revenue,2,000,000 corrected costs,6,000,000 taxable,1,500,000 due,300,000 assumed supported credit,1,200,000 payable; draft costs2,120,000/payable1,170,000; correction raises balance30,000.
- Corrected document semantics only: B-EXP, C-COST and C-ASSET use plain working schedules, rather than Type4 income records. This does not add a ninth source type or change amounts/decisions. Type8 is required but presently unimplemented. Specified corporate operations/BIR registration2024-01-01, first MCIT2028, to make the proposal's MCIT exclusion explicit. These supplied curriculum assumptions require acceptance.
- Finalized independent subtotal scoring against the player's chosen sets; frozen first-complete check, medals/XP, correction before acquisition, case-scoped root IDs, suppression of dependent cascades and repeated/failed-save debits. These are contracts for later runtime implementation, not tested gameplay behavior.

### Actual inspection and checks

Read proposal Sections4–7 and15–17 (including expanded scoring/calculator contracts), the required pack documents and source map. Reinspected `TaxComputationCalculator`, `CaseData`, `CaseDefinition`/`LevelDefinition`/`CaseReviewDefinition`, `JsonCaseLoader`, `CaseFactory`, `CaseManager`, `SupportingPaperTypes`, `ClientRecord`/`ClientRecordRow`, `SupportingDocumentFace`, `CaseRecordRules`, L2 paper bindings and Rosa JSON, LevelBookLeaves, SupportingDocuments and Bea's existing `bea` identity in plaza_life. Findings: float/flat-rate/clamping prototype; individual-only default enums/grade; exact documentName links; absent generic row-to-field transaction validation; Type8 fallback. T01 records these existing facts separately from proposed additions.

Commands actually executed:

- Baseline `python Documentation/Level3TheNumbersTellAStory/verify_pack.py`: PASS18 Planned tickets,200 local links,107 existing-source references,12 arithmetic fixtures (historical baseline retained above).
- `python -B Documentation/Level3TheNumbersTellAStory/verify_content.py`: **PASS313 assertions** reading the actual Markdown tables with Decimal. Includes identities, period containment, source/field uniqueness, transaction edges/cycles, withholding subsets, component totals, both full chains, all five bracket ceilings at−0.01/exact/+0.01 with band identity/continuity, cents/whole-peso midpoints, corporate AND thresholds at all nine combinations, MCIT dates, non-client example, score bounds and independently calculated wrong examples.
- `python -B Documentation/Level3TheNumbersTellAStory/verify_pack.py`: **PASS18 tickets:1 implemented/internal-verified/user-manual-pending,17 Planned; dependency DAG/order;214 local links;107 existing-source references;12 original arithmetic fixtures;313 T01 content assertions**. Status handling now permits only the four documented lifecycle statuses, rejects Planned prechecks, requires completed internal checks for implemented status and manual checks for User accepted. Dependency/link/source checks remain enforced.
- Inline Python `runpy`/`unittest.mock`/temporary-fixture probes: **8 status cases passed** (all four allowed statuses and rejection of unknown status, prechecked Planned, incomplete implemented and premature User accepted). **14 altered-content fixtures rejected**: period, shared transaction, cross-case support, certificate parent, total components, corporate ceiling/operator/start year, bracket rate, expected answer, wrong answer, boundary, rounding and decision ID. Production fixtures were not mutated.
- Full pack in-memory `Path.read_bytes` probes: **dependency, missing-local-link and missing-existing-source mutations each failed with the expected diagnostic**, and the unmodified pack passed before/after. Both validator files compiled in memory using Python `compile`; all14 delivered files normalized to UTF-8/CRLF; no runtime build needed. Disposable temporary documentation copies were removed by TemporaryDirectory; an incidental Python bytecode file was removed, and final commands use `-B`.
- `Get-FileHash Assets/Scenes/Office_Level1.unity,Assets/Scenes/Office_Level2.unity -Algorithm SHA256` (read only): current L1 `148D871CD00586BFF6A495C85C63BB37C46E22670B526A97052ECDD2E03462FE`; current L2 `F5B8538E20841926228B4F84BB5447DB9C1C4AA488B65E73F435F06343DB19E7`. L2 matches the29 October inspection; L1 differs from that historical entry. No T01 scene write occurred; origin of the existing L1 change is unknown. Do not claim unchanged-from-authoring L1 or revert it. `Test-Path` confirmed no Office_Level3 scene or Level_03 resource directory.

Independent read-only explorer/researcher supplied loader/source and primary-tax findings. Independent reviewer found missing explicit total components and corporate tests that only compared hardcoded constants; both were fixed by adding parsed contract tables and validating them against independent anchors. Follow-up reviewer confirmation failed due to the agent's usage limit; root directly inspected the fixes and ran their positive/mutation checks. No unreported reviewer approval is claimed.

No Unity/editor, scratch/.NET gameplay suite, production serialization/failure injection, actual-save operation or Git command ran. Those runtime checks are not applicable to Markdown/documentation validators. No model, prop, animation, runtime script, production JSON, package or scene was created/modified.

### Files changed and remaining acceptance

All paths below are under `Documentation/Level3TheNumbersTellAStory/`:

- New: `TAX_AUTHORITY.md`, `FIXTURE_CONTRACT.md`, `verify_content.py`.
- Updated: `AGENTS.md`, `README.md`, `CONTENT.md`, `SPEC.md`, `REWARDS_AND_VALIDATION.md`, `VERIFICATION.md`, `KNOWN_ISSUES.md`, `PROGRESS.md`, `TICKETS.md`, `tickets/T01-tax-content-and-fixtures.md`, `verify_pack.py`.

**L3-CONTENT-AUTHORITY remains pending** with the curriculum owner/Philippine tax educator (identity/date not supplied). Required review: bounded expenses/accounting period and private-withdrawal interpretation; ordinary/small/EDR/MCIT assumptions; Bea's conditional professional5% certificate context; Marine's300,000 credit payer/ATC/income provenance or explicit supplied-credit simplification; cents calculation versus BIR whole-peso form entry; worked example/assessed copy. The Marine credit is preserved for isolated development, not claimed validated as a3.75% legal rate. RR7-2025 supplies100-million assets despite the RA12066 words/digits discrepancy; Marine's6-million income makes its25% result unaffected.

T02 can implement the optional L3 DTO/loader validation and isolated two-case resumable state with exact case/source/row/field/component bindings and no answer-key snapshots. It must preserve Career's coming-soon gate and two-client separation; runtime and physical work follow subsequent tickets. T18 cannot treat arithmetic research as educator approval. No new user Unity/device acceptance is claimed; inherited manual gaps remain open.

## 30 October 2026 — T02 safe scene entry and resumable chapter state

Status: **Implemented, internally verified; user manual pending**. T03–T18 remain Planned; T03 was not started.

### Prerequisite check

T01 was inspected before work: its ticket status is "Implemented, internally verified; user manual pending", and TAX_AUTHORITY.md, FIXTURE_CONTRACT.md and verify_content.py exist. The only open T01 item is the **L3-CONTENT-AUTHORITY** educator approval. KNOWN_ISSUES L3-02 records that this gate permits isolated T02 development, and FIXTURE_CONTRACT assigns the concrete DTO/loading/checkpoint code to T02. That gate stays open: T02 copies the contract figures exactly, adds no ATC, credit or rule changes, and does not treat its own tests as sign-off.

### Scene copy

- Immediate pre-change baseline (30 October, before any write): Office_Level1 `148D871CD00586BFF6A495C85C63BB37C46E22670B526A97052ECDD2E03462FE`, Office_Level2 `F5B8538E20841926228B4F84BB5447DB9C1C4AA488B65E73F435F06343DB19E7`, their .meta files `681BC120…6EAF` / `C4F6D4B8…0C69`, EditorBuildSettings `CDBBD200…2F2F`. `Assets/Scenes/Office_Level3.unity` did not exist.
- Created **once** with exclusive-create writes: `Assets/Scenes/Office_Level3.unity` (6,622,879 bytes, a byte copy of the saved Office_Level2 whose hash matched the baseline at copy time) and `Office_Level3.unity.meta` with new GUID **`844c604a279e4b66ae226bf66f1dd99a`** (checked unique among all project metas). No fileID rewrite, hand reconstruction or editor save. The scene file is not edited afterwards. Level3Gameplay is built at runtime.
- After all work, every baseline hash above is unchanged, including EditorBuildSettings. The Unity editor was open during the session; no scene was switched, forced to save or saved, and Unity import of every new file is **pending** (the user owns it). The copy includes only saved disk content.

### Inherited behavior: disabled versus retained (Office_Level3 only)

Disabled at runtime by `Level3OfficeStart` from `Levels/Level_03/office_start.json`. Objects are deactivated, never deleted.

- Hidden: `Environment/Outside/Plaza/NPCS` (Level 1 quest crowd: Marie, Jhun, Milo, Zorbix and the plaza TutorialNpcController guide), `Environment/Outside/Plaza/CorkboardSideQuestRoot` (CorkboardSideQuestManager), `UI/HUD_Canvas/PermanentHUD/IconButtons/TaxpayerProfileIcon` (Client IDs), and `Environment/OfficeBuilding/Rooms/Reception/WaitingArea/Client2` (already inactive in the saved scene, so this is a no-op).
- Disabled component: `TutorialController` (the old Level 1 popup tutorial).
- Desk Case Folder: hidden through the existing `CaseFolder3DDrag.HideAndDisableFolder()`, the same API Level 2 uses before retrieval, so Level 1's folder → ReviewDocuments flow cannot start.
- FSM guard: `GameStateMachine.TransitionGuard` refuses `CaseCompleteState`, `LevelCompleteState`, `ArchiveCaseState` and `PrepareReturnState` in every Level 3 phase. This blocks the Auditor → CaseComplete → OnCaseFinished → LevelComplete/Continue route after a single case, the client archive route and the filing-cabinet archive.
- Never built here (they are level-id gated): all Level2OfficeStart parts (Rosa's retrieval drawer, folder hand-off, findings, stamps, Town, stall, office review, audit, review clock, checkpointer, annotated papers, book icon, case records, consultation); Level 1 bootstraps (ArthurOpening, CaseRecordsDesk, ClientConsultation, Level1FieldCheckpointer, OfficeDoorGate, SideQuestSplash, OfficeHours, ConsequenceReactions, DeskFolderStackSync, TownContentLoader); ClientCopyHandover; CaseFinale (no `finale` feature); HudHotbar (Levels 1 and 2 only; see L3-18).
- Office gate: `Levels/Level_03/rewards.json` sets `requireSideQuestBeforeOffice: false` and no held doors. Without it, the built-in default would block the receptionist behind Level 1's side quest.

Retained shared parts (unchanged scene objects): environment/OfficeBuilding, doors and colliders; directional lights, with Level 2's runtime balance mirrored in Level3OfficeStart (key 0.95, fill 0.28; Level 2's code is untouched); Player (PlayerMovement, Interactor, WorkstationInteractor); Main Camera (FirstPersonCameraController, FirstPersonHands); Controller/CameraController + DialogueCameraDirector; VirtualJoystick/MobileLookArea/MobileControlsSafeArea; EventSystem; AudioManager sources; Receptionist (dialogue; Level 2's runtime NpcAnimationDriver/StaticArmRig added the same way); Auditor + AuditorSubmissionTray; Colleague; desk/workstation; desk Tax Code Book; filing cabinet (inert outside ArchiveCase); CaseProgressionManager; CaseManager; LevelTimer; DialogueUI; Notes panel/icon; and passive inherited UI. Classified as shared despite the name: Level1SceneController (generic fade-in), CollectedProfilesWorldPanel (passive), and PrintJobController (idle). Office features: hud, minimap, guide and fade; the office ambience context (AudioManager); and the settings board.

### Chapter state, save and resume

- `campaign.levelThree` (`Level3ChapterCheckpoint` v1 in PlayerProgressData.cs, inside the existing signed v5 save; no version bump) holds the content revision, phase name, active case, applied action ids, milestones, and **both** case subrecords (`L3-BEA-001`, `L3-MARINE-001`: admitted, acquired source ids, packet revision, copy phase). It holds no answers, content or Unity objects. Structural repair only; a pre-T02 save loads without repair or rewrite.
- `Level3ChapterRules` is pure C#. It covers the 16 SPEC phases saved by name, the transition table, the active case per phase, entry decisions (Fresh, Resume, or Blocked = kept untouched, attempt saves nothing), the `AssignBea` command (Reception → ToolPractice, action `Level_03/reception-assigned`, duplicates quiet), implied objective triggers/lessons, coarse FSM mapping, the legacy guard, the story gate (Story mode, not practice, campaign completed Level_02, readable/non-future save), `WithCheckpoint` candidate composition, and the Rosa history variant.
- `PlayerProgressManager`: `UsesLevelThreeStory`, `GetLevelThreeCheckpoint`, `CommitLevelThreeCheckpoint` (CloneProgress → WithCheckpoint → existing TryWriteCandidate) and `ClearLevelThreeCheckpoint` (settings replay).
- `Level3Checkpointer` (Level3Gameplay/Checkpoint) waits for the level to start, then decides entry. Resume is quiet: receptionist `RestoreSpoken`, Notes `HudIconButton.SetAvailable(true)`, `OfficeObjectives.MarkDone`, coarse ReceiveCaseState, and one "welcome back" line, with no gameplay event raised. When the receptionist's conversation ends, the candidate is written first and only then published, which raises `ChapterAssignmentReceived`. A failed write keeps Reception and retries every 3 s with a short notice. It also saves on pause/quit and settings exit, sets the FSM guard and guide flags (`assignmentReceived`, `toolPractice`), and releases only its own hooks on disable. Direct Editor, replay and practice attempts keep the chapter in memory only.
- Possessions: the wallet, Public Trust, Rosa history, claims, ledger, mastery/badges, tutorials, settings, inventory slots/pins, upgrades, decor and supplies are carried untouched. No reset, reseed or retirement runs on Level 3 entry. Notes, Case Folder and Tax Code Book scopes are deliberately extended to `Level_01,Level_02,Level_03` (inventory.json and the built-in copy), and `ResetsOnFreshAttempt` keeps Notes/Book slots on a fresh Level 3 attempt. Rosa's marked records and Level 1's Client IDs classify as other chapters' objects (L3-18).

### Opening task wiring

The receptionist uses the existing dialogue. `case_bea_001`'s greeting opens with `{history}`, which is filled through the new `ReceptionistInteractable.GreetingFilter` with CONTENT's exact "First reception" variant for the saved Rosa choice (read only). It then assigns the desk calculator first. `Levels/Level_03/objectives.json`: "Check in at reception." (`event:ChapterAssignmentReceived`), then "Practise with the desk calculator." (`event:CalculatorPracticeAttached`, raised by T03). `Tutorials/Level_03/guide.json`: Arthur's welcome card, then the calculator task on `flag:assignmentReceived`. No arrows. GuideDirector waits for the chapter restore and skips implied lessons. The calculator activity itself is not implemented.

### Content DTO (FIXTURE_CONTRACT, owned by T02)

`CaseDefinition.level3` (`Level3CaseContent` and its row/source/total/credit/rule/decision/expected classes) and `Level3ContentValidator` (exact decimals) check identity, revision, period, document/field bindings and printed amounts, money syntax/bounds, support/claim/subset edges and cycles, totals, credit links, band continuity, explicit corporate facts, the twelve decisions and both expected chains. `JsonCaseLoader` validates every Level_03 case and skips the individual-only default graded fields only there. `CaseManager` creates **no placeholder** for a failed Level_03 load (fail closed). The case files were generated from the contract tables. Legacy float answer keys stay blank. Top-level `clientId` is blank, so no conference-room client appears on load.

### Files changed

New: `Assets/Scenes/Office_Level3.unity`(+.meta); `Assets/Scripts/Level3/` (folder .meta, `Level3ChapterRules.cs`, `Level3ContentValidator.cs`, `Level3OfficeStart.cs`, `Level3Checkpointer.cs` + metas); `Assets/Scripts/Data/Definitions/Level3CaseContent.cs`(+.meta); `Assets/Editor/Level3SceneSetup.cs`(+.meta); `Assets/Resources/Levels/Level_03/` (folder, `level.json`, `office_start.json`, `objectives.json`, `rewards.json`, `cases/case_bea_001.json`, `cases/case_marine_001.json` + metas); `Assets/Resources/Tutorials/Level_03/guide.json`(+folder/meta); `Tests/Level3Regression/` (`Level3Regression.csproj`, `Program.cs`); `Tests/VerifyLevel3Scene.py`.

Changed (opt-in for Level_03; Levels 1/2 behavior unchanged): `OfficeLevelCatalog.cs` (Office_Level3 → Level_03), `GameStateMachine.cs` (static TransitionGuard, null by default), `GameplayEvents.cs` + `ObjectiveTriggers.cs` + `OfficeObjectives.cs` (two Level 3 events), `GuideDirector.cs` (Level 3 restore branch), `ReceptionistInteractable.cs` (static GreetingFilter, null by default), `PlayerProgressData.cs`, `PlayerProgressManager.cs`, `CaseDefinition.cs`, `JsonCaseLoader.cs`, `CaseManager.cs`, `InGameSettings.cs` + `Resources/Settings/in_game_settings.json` (settings board in Office_Level3; Level 3 save on exit), `SettingsBoard3D.cs` (leave text), `AudioManager.cs` (indoor ambience for Office_Level3), `CampaignInventory.cs` + `Resources/Campaign/inventory.json` (three scopes), `Level2CampaignRules.cs` (Level_03 keeps Notes/Book slots), `CampaignRewards.cs` (Level3 id). `Level2OfficeStart.cs` was briefly refactored, then restored byte-for-byte from the committed content (sha256 `1D1FF3A7…0CA4`); it is unchanged.

Line endings: an early edit helper flattened CRLF in the shared files above. Pre-session PDB checksums proved every determinable original was CRLF, so all touched and new text files are CRLF again. Git `core.autocrlf=true`, so EOL is not a content change.

### Internal checks actually run (30 October)

- `python Tests/make_scratch_build.py` reported 5 new runtime scripts added and 506 files. `dotnet build Temp/ScratchBuild/Scratch.csproj --nologo` **succeeded, 0 errors, 27 warnings** (all pre-existing: obsolete Find/TMP APIs, unused fields, unresolved optional package DLLs; none in Level 3 files). Rebuilt after every production change.
- The editor script was compiled in a scratch netstandard2.1 project with `Level3SceneSetup.cs` + `Level2SceneSetup.cs` against the installed UnityEditor/UnityEngine CoreModule: **succeeded, 0 warnings**. It was not executed (Unity not launched).
- `Tests/Level3Regression` (`dotnet run`): **199/199**. Covers: mapping/features; fresh entry with both subrecords; 22 phase-name cases; the transition table; AssignBea apply/duplicate/refusals; 8 Blocked snapshots; implied triggers/lessons; the legacy guard on real state classes; the mode matrix; candidate isolation; round trip equal to the fixture except `levelThree` (wallet 245, trust 63, Rosa responsible history, claims, ledger, mastery, inventory, pins, upgrades, decor); no answer data in the snapshot; pre-T02 save loads without repair; structural repair; a newer version kept and refused; **failed write** (directory planted at `progress.json.tmp` in a disposable temp store: both generations byte-identical, nothing published, retry writes the signed ToolPractice save, backup keeps the previous generation); Rosa variants; both real case files validate; 34 content mutations refused; cents midpoints; Level 1/2 cases keep their path; config/guide/objective/inventory/career checks; source wiring for write-before-publish, quiet restore, scoped hooks and settings exit. First run 196/197 because a substring check was too loose (`50,000.00` inside `250,000.00`); it was fixed to exact values.
- Existing suites after the final build: Level2Regression **1584 PASS**; CampaignFoundationRegression **748/748**; CampaignFoundationRuntimeRegression **16/16**; GuideRegression **937 PASS**; SettingsRegression **138/138**; AudioRegression **259**; DialogueRegression **1159 assertions PASS**; ProgressionRegression **PASS**; CareerBoardRegression **72/72**; TrainingRegression **513/513**; ControlsRegression **5734 assertions PASS**. Two intermediate failures were fixed: Level2Regression 2/1584 (source checks pinned to the original Level2OfficeStart lighting code; resolved by restoring that file) and SettingsRegression 2/138 (CRLF; resolved).
- Level1LivingPlazaRegression: **73/77, 4 pre-existing failures** (page-turn begin pattern, walk self-transition, strolling speed, ambient-talk pattern). All four match literal `\n` against untouched CRLF files (`PageTurnStage.cs`, `NpcHumanoid.controller`, `AmbientNpcInteractable.cs`, last modified 29 October 23:58, before T02). Not caused or fixed here.
- `python Tests/VerifyLevel3Scene.py`: **PASS, 3 warnings**: an inherited missing script `CaseDropDestination` (GUID `98b9d8ae…2c13`, also in Office_Level1/2); a spawn door leaf inside a prefab instance (Level 2's verifier warns identically); the build entry is not present until Unity import. It checks owner counts identical to Office_Level2, including **2 GameStateMachine components** (inherited: Controller and Receptionist), and that no runtime part is serialized.
- `python Tests/VerifyLevel2Scene.py` **PASS (2 warnings, pre-existing)**; `VerifyLevel2Assets.py` **PASS, 11,907 unique .meta GUIDs**; `VerifyGuideTargets.py` **14 paths, 0 missing**.
- After the documentation updates: `python -B Documentation/Level3TheNumbersTellAStory/verify_pack.py` **PASS** (18 tickets: 2 implemented/internal-verified/user-manual-pending, 16 Planned; dependency DAG/order; 216 local links; 107 existing-source references; 12 arithmetic fixtures; 313 T01 content assertions). `verify_content.py` **PASS 313**. These validate documentation only.
- Read-only repository-object reads (Python over `.git/index` and packs, no Git command) were used only to restore Level2OfficeStart and determine line endings.
- Not run: Unity import, Play Mode, rendering, device/touch, listening and performance; in-Unity save/resume (no story route reaches Level 3 before T18). Suites were run in place with `dotnet run`, not the established out-of-tree build. This rebuilt the 28 tracked files under `Tests/Level2Regression/bin` (already modified before this session; restoring them is the user's Git decision) and untracked `Tests/*/obj` intermediates. `Tests/Level3Regression/bin` and `obj` were deleted after the final run.

### Pending user acceptance (Unity/device/manual)

Import the project (Level3SceneSetup then appends Office_Level3 to the end of the build list). Open `Assets/Scenes/Office_Level3.unity` and press Play: this is a direct Editor attempt that saves nothing. Confirm: spawn about 1 m inside the entrance facing the receptionist; movement/look/joysticks; lighting matching Level 2; doors/walls; no duplicate actors; empty plaza; no desk folder; no Level 1 popup tutorial or corkboard; and a safe reception camera. Talk to the receptionist: one history line, calculator assignment, Notes unlocked, objective "Check in at reception." ticked, Arthur's calculator hint, no Level Complete/case card. Leave and replay through settings. On Android, check layout and controls. Resume inside Unity needs an isolated story fixture route, which is not provided before T18 (L3-19).

### Next step

Before T03: user imports and accepts T02 in Unity. **T03** then implements the physical calculator practice inside this seam. It adds its part under `Level3Gameplay` via `Level3OfficeStart.EnsurePart` and extends `Level3ChapterCheckpoint` with the practice tape. The protected 100 − 20 = 80 − 8 = 72 budget uses exact decimals with no wallet/score effect. Attaching both tapes is one `Level3ChapterRules.TryAdvance(ToolPractice → BeaRecords, "Level_03/practice-attached")` written through `CommitLevelThreeCheckpoint`, and only after the save does `GameplayEvents.RaiseCalculatorPracticeAttached()` tick the existing objective. It extends ImpliedTriggers/Lessons for any new lesson, and keeps the FSM guard, the Career gate and L3-18 in place.

## 30 October 2026 — Shared Bag/hotbar and keep-all chapter continuation

Status: Implemented, internally verified; USER Unity/device acceptance pending. Direct user instruction now requires every collected inventory item to remain across Levels1→2→3 regardless of current usefulness. It supersedes the prior retirement design and L3-18 split. T03 and later gameplay were not implemented.

Root cause/repro: HudHotbar.UsesHotbar excluded Level3; its non-Level2 path used Level1 checkpoint/reset/profile logic, while RetireOtherChapters removed prior quest objects. A new regression initially failed7 checks (198/205): L3 HUD admission, L2/L3 retention and later-case-folder reset. Failures were reproduced against pre-fix runtime DLL with disposable saves under Temp/BagContinuation/SaveTemp. An initial runner output-depth error and system-temp access denial were corrected by matching the suite's five-parent root convention and setting TEMP/TMP to the workspace sandbox; neither counted as a gameplay test result.

Changed runtime paths:
- Assets/Scripts/UI/HUD/HudHotbar.cs: shared L3 admission; explicit ready/resume/blocked branch; own book carrier; retained folder/IDs/marked records; accumulated folder contents; acquired-only display snapshots with batched candidate-save/retry; source-case validation; saved-paper reading and correct current slot/Bag return target.
- Assets/Scripts/RewardsSystem/Level2CampaignRules.cs: no chapter retirement, no L2/L3 entry reset; earlier-item scope describes read-only eligibility rather than deletion.
- Assets/Scripts/RewardsSystem/PlayerProgressData.cs and CampaignInventory.cs: additive display-only InventoryReferencePage/record storage and pure KeepReference upsert/no-op/deep-copy rules. Original print/player-selected annotations only, never correctValue/answer keys/semantic transfer.
- Assets/Scripts/UI/HUD/HudProfileCards.cs: retained actual profile IDs feed the existing3D ID-card viewer, with its live slot/Bag return anchor; no collection or quest event replay.
- Assets/Scripts/UI/HUD/InventoryScreen.cs and Assets/Scripts/SideQuest1/WorldCollectibles.cs: existing Bag/inspect presentation gains SAVED PAPERS pages and previous-chapter reference labeling; no new scene/model/archive framework. Old save references without paper text stay honest, never show fabricated annotations or raw internal-ID lists.
- Assets/Scripts/UI/HUD/HudIconButton.cs, HudSubmittableIcon.cs and OfficeHudRuntime.cs: retained-folder click reads saved pages; actual current stored-folder count remains required for drag/submission. Existing talk-first audit/client-copy precedence preserved.
- Assets/Scripts/Level3/Level3ChapterRules.cs and Assets/Scripts/RewardsSystem/PlayerProgressManager.cs: bounded direct-Level3 preview of a deep local inventory copy only for a compatible completed-Chapter2 campaign. Local preview commands never write live story inventory; other practice/replay/chapters retain existing isolation. No Career/readiness unlock.

Tests changed: Tests/Level3Regression/Program.cs adds real rule/serialization/reference/deep-copy/preview-mode checks plus actual startup/reader-binding contracts; Level2Regression and CampaignFoundationRegression update superseded deliberate retirement/empty-folder source contracts while retaining behavioral no-folder drag/click rejection tests. The unowned-empty folder still cannot click/submit; an owned retained copy can read but cannot submit without the active physical folder.

Actual internal evidence:
- python Tests/make_scratch_build.py, then dotnet build Temp/ScratchBuild/Scratch.csproj --no-restore --nologo -v quiet: final0 errors/27 inherited warnings. Log Temp/BagContinuation-scratch.log.
- Built suites out-of-tree using dotnet build Tests/<suite>/<suite>.csproj -o Temp/BagContinuation/<suite>/Debug/net10.0 --nologo -v quiet, then ran each emitted DLL with workspace TEMP/TMP. Final Level3Regression245/245; Level2Regression1584 PASS; CampaignFoundationRegression748/748; CampaignFoundationRuntimeRegression16/16. Build/run logs Temp/BagContinuation-<suite>-build.log and Temp/BagContinuation-<suite>.log. No tracked Tests/*/bin executable output was intentionally targeted; dotnet may refresh normal obj intermediates.
- Affected additional checks executed: Controls5734 assertions; Dialogue1159 assertions; Audio259 checks; Guide937 checks. No new framework or external packages.
- VerifyLevel3Scene PASS with3 inherited/pending warnings (missing CaseDropDestination script also in L2, prefab-internal spawn not provable statically, build registration pending Unity import); VerifyLevel2Assets PASS with11907 unique metaGUIDs; VerifyGuideTargets14 paths/0missing; Level3 documentation validator PASS. No warning was relabeled as rendered acceptance.
- Source scene hashes observed: L1 148D871CD00586BFF6A495C85C63BB37C46E22670B526A97052ECDD2E03462FE; L2 and L3 F5B8538E20841926228B4F84BB5447DB9C1C4AA488B65E73F435F06343DB19E7. No scene/prefab/package/meta/model/save file was edited. No Unity/Git operations. Temporary authoring helper removed after use.

Manual route: import scripts, enter Office_Level3 and confirm Bag/hotbar visible, talk to receptionist and read Notes. With a compatible completedL2 campaign, DirectEditor preview must show copied actual possessions/slots/capacities/pins; inspect old IDs/marked records/SAVED PAPERS and book, close/read/move items, exit/reenter, verify the live campaign quantities/ownership unchanged. Without completedL2 data, the isolated Bag is visible but initially empty. Confirm retained-folder taps read while empty-current-folder drags cannot submit; actual L1/L2 current folder/audit/copy delivery still work. Check mobile sizing/Back/camera/motion/audio/animation in Unity/device; no AI manual/render acceptance claimed.

Limits: already deleted historical possessions are not reconstructed; old saves that retained only identities cannot recover missing paper/player-annotation text from answer keys. Newly acquired display snapshots are retained. DirectEditor preview tests possessions, not story chapter resume; L3-19 remains. Normal Career3 stays comingSoon until T18. Next numbered ticket remains T03.

## 30 October 2026 — Unwanted Display Stats overlay and Remote input interference

User supplied a screenshot of Unity's Rendering Debugger Display Stats overlay and reported joystick interruption while testing through Unity Remote. This is a scoped shared-input follow-up, not new chapter gameplay.

Verified installed-source findings: Core RP DebugManager.Actions.GetActionToggleDebugMenuWithTouch opens the runtime debug UI on a three-finger double-tap; DebugUpdater enables it in Editor/development runs. InputSystemUIInputModule's default SingleMouseOrPenButMultiTouchAndTrack removes other pointer state on mouse/pen activity. All three office scenes serialize pointerBehavior0 (that default). The shown20FPS is screenshot evidence only, not a measured Android benchmark. Hardware ghost touches and the exact Remote interruption were not reproduced by AI.

Modified Assets/Scripts/Mobile/MobileInputGuard.cs only: BeforeSceneLoad disables/closes rendering debug runtime/persistent UI (the editor debugging window is unchanged), resets its own static raycast state, idempotently registers sceneLoaded, and configures each loaded scene's existing InputSystemUIInputModule to AllPointersAsIs. This keeps mouse/touch pointers independent; no new EventSystem, simulated touch, per-frame scene search, UI framework, renderer/package/scene setting or joystick-release delay. Existing pause/focus/disable/own-pointer release remains intact. Focus loss intentionally stops movement for safety and was not bypassed.

Added Tests/VerifyMobileInput.py, a static startup/configuration regression contract (explicitly not device/runtime gesture validation). Red run:5 failed/2 passed before fix. Green:7 contracts passed. Scratch build0errors/27 inherited warnings; ControlsRegression5734 assertions, Level2Regression1584 PASS, GuideRegression937 PASS. Suites built out of tree under Temp/MobileInputChecks with workspace TEMP/TMP; logs retained there. No Unity/Play Mode/device operation, real save write, Git command or scene/prefab/package/meta change.

User acceptance pending: stop Play, allow compile/import, restart. Keep Game view focused; hold movement, add look touch, move desktop mouse, and try a three-finger double tap. Overlay must stay closed; finger ownership should not be dropped by PC mouse activity. Check actual release/pause/disable still stops movement; dialogue/Bag/station locks still restore correctly. If interruptions persist, compare a native Android build with Remote before attributing them to hardware or declaring the transport fixed. This follow-up addresses verified configuration hazards, not an asserted complete fix for unobserved Remote/device failures.

## 30 October 2026 — T03 physical calculator and unscored practice tape

Status: **Implemented, internally verified; user manual pending**. T02's Unity acceptance was still pending when T03 started (the user asked to continue). T04–T18 remain Planned; T04 was not started.

### What the player gets (design as built)

- **On the desk:** from the seat, a real calculator sits on the consultant desk to the right of centre, and from the calculator task on, a practice clipboard sits beside it. The calculator is the supplied **Synty SM_Prop_Calculator_01** body, fitted to 0.19 m with its long side away from the player; the source prefab is untouched (hash `C119D059…` unchanged). On top of the body:
  - a navy faceplate;
  - a recessed dark display with a brass rim, mint digits and a caption line;
  - **20 raised keycaps**: cream digits, navy operators, a brass `=`, terracotta C/CE/DEL;
  - a cream paper roll with brass caps in a navy housing behind the display;
  - a **tape strip** with real thickness and a curled lip, fed out onto the desk on the player's left of the calculator.

  The clipboard is a navy board with a brass clip holding a cream card. The card reads "PRACTICE · OFFICE BUDGET" and "Not your money. Practice only, no score." It lists the amounts Budget 100.00, Paper order 20.00 and Courier 8.00, has two tape slots, a status line and a hint line.
- **Lift:** tapping either object lifts both toward the seated view (0.35 s). Like Level 2's hand stamps, they float 0.62 m from the camera, sized to 62% of the screen height. The card goes on the left; the calculator and its tape go on the right. They shrink together on narrow screens, refit when the screen size changes, and stay clear of the 0.3 m near clip. A bottom-right **PUT CALCULATOR AWAY** pill (Level 2's PUT STAMPS AWAY look) sets them down (0.3 s).
- **Use:**
  - Tapping a card amount puts it on the calculator with its label.
  - Keys enter **once on tap-release over the same key**: pressing lowers the cap, sliding off cancels, and nothing auto-repeats.
  - `=` prints the working on the tape (0.2 s feed).
  - The first tap on a slot names it ("Attach 80.00 to 'Left after paper order'? Tap the slot again."); the second tap attaches.
  - Once the save succeeds, the strip tears off and flies to the slot (0.3 s), where a clipped paper tape with a brass clip shows the working and result.
  - Tapping an attached tape offers its result for the next step.
  - Wrong attempts get one plain hint line and nothing changes; there is no grade and no penalty.
- **Practice:** 100 − 20 = 80 goes in slot 1, then tape 1 − 8 = 72 goes in slot 2. That save moves the chapter **ToolPractice → BeaRecords**, and only then does the existing "Practise with the desk calculator." objective tick. The card then shows PRACTICE COMPLETE. The calculator stays a free shared tool for later tickets.
- **Guide (Arthur, hint style, never blocking the desk):** `calculator-intro` (sit down and tap the calculator), then `calculator-practice`, whose three steps each wait on a player cue (`PracticeSourceSelected`, `PracticeTapePrinted`, `PracticeTapeAttached`). Then `calculator-own-turn` and `calculator-done` ("Bea's records are waiting at her plaza table").
- **Resolved anchors (runtime, from the scene):** seat = DeskViewpoint ≈ (11.10, 1.08, 95.72), looking −x and 58° down; the desk's seat-relative forward is −x and right is +z. Calculator rest = seat + 0.40 forward + 0.21 right ≈ (10.70, desk top, 95.93); card rest = seat + 0.42 forward − 0.02 right ≈ (10.68, desk top, 95.70). The desk top comes from a downward raycast against the desk's own colliders, clamped 0.09 m inside them, with a renderer-bounds fallback. Pointer layer 7 (3DInteractable) is in the camera's event mask, so the tools block taps to the book behind them, but it is outside the WorkstationInteractor desk mask (layer 6 only).
- **Audio (existing library names, debounced by AudioManager families):** keys use `sfx_keyboard_type` (PlayKeyboardSFX); tape feed uses `sfx_paper_rustle_01`; a source tap uses `sfx_card_pickup_02`; attach uses `sfx_card_place_01`; lift and put away use the `document_open`/`document_close` families; arming a slot uses `sfx_ui_select_01`; a refusal uses `sfx_ui_click_01`. No new clip and no dedicated calculator recording is claimed.

### Rules and state

- `Level3Calculator` (pure C#): exact `decimal` arithmetic; × ÷ before + −, read left to right; `%` turns the entry into its fraction (A × 20% = A × 0.20; A + 20% = A + 0.20, with no consumer shortcut). It allows 12 integer and 6 decimal digits, enforces a ±999,999,999,999.99 bound, and reports divide-by-zero, overflow, unfinished and leading-operator input as **issues without other change**. Repeated operators replace each other; backspace steps back over an operator; CE clears the entry; C clears the expression while the printed draft stays; an operator after `=` continues from the result. Each number keeps its provenance (source id + label, or typed). Display uses grouped money with a "…" marker where it rounds; tape lines read `100.00 BUDGET`, `− 20.00 PAPER`, `= 80.00`.
- `Level3PracticeRules` (pure): the card's three sources, two ordered slots, and an `AttachTape` command run on a copy of the snapshot. A tape is accepted only if the phase is ToolPractice, the slot is open and in order, the expression has at least one operation, and its exact re-evaluation equals both the printed result and the slot's amount. A filled slot is a quiet duplicate. The second tape advances the phase in the same candidate. Typed (manual) numbers are allowed.
- `Level3ChapterCheckpoint.tapes` (`Level3TapeRecord` + `Level3CalcToken`) stores the player's own expression and exact result, one per destination; repair de-duplicates. `ImpliedLessons(state)` skips the guided lesson after tape 1. `Level3Checkpointer.CommitPracticeTape` runs candidate → `CommitLevelThreeCheckpoint` (story runs only) → publish → guide cue. Refusal or a failed save changes nothing; the player taps again to retry.
- The draft expression is kept in memory across put-away and Exit Desk within a visit. Only attached tapes and the phase persist (L3-25).

### Files

New (all with unique .meta GUIDs, CRLF): `Assets/Scripts/Level3/Level3Calculator.cs`, `Level3PracticeRules.cs`, `Level3SceneAssets.cs`, `Level3DeskProps.cs`, `Level3CalculatorModel.cs`, `Level3PracticeCardModel.cs`, `Level3DeskCalculator.cs`; `Assets/Resources/Levels/Level_03/scene_assets.asset` (calculatorPrefab → Synty prefab guid `350c2278…4913`, root `1708030912986460`).

Changed: `PlayerProgressData.cs` (tapes, token/tape records), `Level3ChapterRules.cs` (practice lessons, state-based implied lessons), `Level3Checkpointer.cs` (CommitPracticeTape, the practice-complete event, flags `practiceFirstTape`/`practiceDone`/`calculatorUp`), `Level3OfficeStart.cs` (setup **v2**: adds `DeskCalculator`), `GuideDirector.cs` (implied lessons from chapter state), `Tutorials/Level_03/guide.json` (practice lessons), `Tests/Level3Regression/Program.cs` (T03 section; two T02 assertions updated for the longer guide and the state-based call). No scene, prefab, Level 1/2 code, Career, shop or settings change.

### Internal checks actually run

- `python Tests/make_scratch_build.py` (7 new scripts added, 513 files); `dotnet build Temp/ScratchBuild/Scratch.csproj --nologo`: **succeeded, 0 errors, 27 warnings** (unchanged pre-existing set; none in Level 3 files).
- `Tests/Level3Regression` (`dotnet run`, own untracked bin/obj deleted afterwards): **325/325**, of which **80 are T03 checks**. The first T03 run was 322/325: two T02 assertions pinned the old 2-lesson guide and the old `ImpliedLessons(chapter.Phase)` call, and the display showed a source amount as "100" instead of "100.00". All three were fixed; no assertion was weakened beyond those updates.
- Existing suites, **built out of tree** (`dotnet build Tests/<s>/<s>.csproj -o Temp/CampaignChecks/<s>/Debug/net10.0`, run from there): Level2Regression **1584 PASS**, GuideRegression **937 PASS**, CampaignFoundationRegression **748/748**, CampaignFoundationRuntimeRegression **16/16**, TrainingRegression **513/513**, SettingsRegression **138/138**. A check for new files under any `Tests/*/bin` found none; tracked bin outputs were not rewritten this time.
- `VerifyLevel3Scene.py` **PASS** (the same 3 warnings as T02); `VerifyLevel2Assets.py` **PASS, 11,915 unique .meta GUIDs**. Office_Level1/2/3 hashes unchanged (`148D871C…`, `F5B8538E…`, and the L3 copy still equals its source).
- Not run: Unity import, Play Mode, rendering, scale/readability on device, touch feel, audio listening, performance. All geometry and screen fitting are computed at runtime and are not visually verified (L3-26).

### Pending user acceptance

1. Open Office_Level3 (a direct Editor attempt saves nothing). Check in at reception, sit at the desk, and confirm the calculator and clipboard rest on the desk, in reach, not clipping the lamp, book or desk edges, and at a believable scale.
2. Tap either object. Check the lift, that both fit on screen with no camera clipping, keycap and display readability, tape legibility, and the card text.
3. Do 100 − 20 = 80 and attach it (two taps). Watch the strip fly to slot 1. Then do tape 1 − 8 = 72 and attach it.
4. The objective ticks, Arthur's hints follow your actions, and PRACTICE COMPLETE shows.
5. Try a wrong value, typing 80 directly, ÷0, Exit Desk mid-sum, settings, Android Back, app switch, and fast double taps.
6. Check sound with SFX on and off, and Android layout in portrait and landscape.

Story-run save/resume needs the fixture route (L3-19).

### Next step

User Unity acceptance of T02 and T03. Before **T04**, the Level 3 Bag/hotbar world branch (L3-18) must exist so Bea's records can be acquired. T04 then adds Bea and her records, reusing this calculator through `Level3DeskCalculator.Calculator` and its source/destination taps.

## 30 October 2026 — T04 Bea encounter and original records

Status: **Implemented, internally verified; user manual pending**. T02/T03 Unity acceptance was still pending. Unity had imported the project by this point: Office_Level3 is registered as the fifth build entry, after the original four. T05–T18 remain Planned; T05 was not started.

### Found before starting

- **New user rule (SPEC "Current continuation rule", CampaignFoundation AGENTS, Level3DesignProposal, T13; written 01:46 outside this session).** Every collected Bag/hotbar item stays across Levels 1 → 2 → 3, and Level 3 adopts the shared Bag/hotbar with its own checkpoint branch.
- **A parallel session (01:46–02:03) changed shared code.** It edited `HudHotbar`, `InventoryScreen`, `OfficeHudRuntime`, `HudIconButton`, `HudProfileCards`, `HudSubmittableIcon`, `WorldCollectibles`, `MobileInputGuard`, `Level2CampaignRules` (RetireOtherChapters is now a no-op) and tests. Level 3 now has a hotbar branch keyed to `Level3Checkpointer`: its Case Folder lists `Level3CaseProgress.acquiredSourceIds` and keeps read-only reference pages. That work is outside this pack's PROGRESS; I inspected it and built on it, without re-verifying all of it.
- **L3-18 is therefore superseded:** the Level 3 Bag exists. T04 only had to commit source ids into the chapter snapshot, plus one Level 3-only fix (below).

### Design as built

- **Placement (measured):** beside Bea's familiar Level 1 bench (`Env/Town/bench (1)` ≈ (9.29, −0.47, 125.95); her Level 1 spot ≈ (9.75, −0.42, 124.75)). Candidate spots lie on rings of 1.8–4.2 m every 30°, nearest her old spot first. A spot is accepted only if a `CheckBox` (table + 0.25 m sides, + 0.85 m standing room front and back, from 0.15 m above the ground) hits no solid collider, and the ground is found by a downward ray within 0.6 m of plaza height. The table's near side faces the office doors (the player's way in); Bea stands 0.38 m behind it facing the player. The chosen spot is logged (`[Level3] Bea's worktable at …`); it is not visually verified.
- **Table:** Palmov Island's park **wooden table** (unused elsewhere), fitted from mesh bounds to 0.76 m working height. A primitive table stands in if the asset is missing. On top sit a kraft work mat (≤ 1.0 × 0.62 m, with a dark edge) and a manila envelope labelled "BEA MENDOZA / 2026 RECORDS" with a flap. A tap target on layer 3 carries the "Sit at Bea's table" prompt.
- **Bea:** Synty SM_Chr_Developer_Female_01 (her Level 1 model), fitted to 1.6 m, on the shared NpcHumanoid controller. One `NpcLife` (Stand) and `NpcAnimationDriver` (`Idle Relaxed`, Walking/Strolling) own her body and turn her to the player; there is no duplicate actor and no unverified clip.
- **Conversation (existing DialogueUI, CONTENT lines in `town_life.json`):**
  - Before the reception assignment, and during practice, she says one short line with no progress.
  - In BeaRecords, she gives a complete introduction (it works whether or not you met her in Level 1), then CONTENT's "I kept the receipts…" and "Let's look at what each record supports." **Only when the introduction completes** does `Level3Checkpointer.CommitBeaRecords` run: candidate → signed write (story runs) → publish. Only then do the nine papers fan out of the envelope onto the mat (0.45 s, staggered), `ClientRecordsCollected` ticks "Meet Bea at her plaza table and collect her records.", and Arthur's `bea-records` hint shows.
  - Cancelling changes nothing. A failed save keeps her records and says so, and the next talk offers them again.
  - Afterwards, a docked question card (Rosa's pattern) offers CONTENT's three questions plus "I'll review the papers first." and Talk later. They are repeatable in any order, the question about the record read last comes first, and they carry no reward, trust or grade.
- **Papers:** `Level3RecordRules` prints the acquired sources through the existing fixed types. B-ID is an ID card (name, masked "•••• B26"); B-BG is client background notes ("Calendar year 2026", freelance illustrator, graduated rates). B-REV prints as an income record, B-RENT as an agreement, and B-SUB/B-PRINT/B-PERSONAL as payment slips, all through CaseRecordRules' own builder. B-EXP and B-DRAFT print as plain working papers. Each paper shows its issuer, period, a "Note" row with its stated purpose/status, and rows in order, with totals in their band (900,000 / 300,000 / 354,000). A row whose transaction appears on another paper shows its reference, so PRINT-26 is visible on B-EXP, B-PRINT and B-DRAFT. Ids are `L3-BEA-001/<source>`, the same identity the Bag's Case Folder lists. **B-WHT is never built**, and a snapshot that holds it before BeaComputation is refused.
- **Reading:** after the handover, tap the table to sit (`EnterFirstPerson`, "Leave Table"). Papers are the existing `ClientRecordPaper` bodies (thickness, clip/corners, contact shadow); tap one to lift it into a reading pose, with two side by side or one large on a small phone. Tap it again or drop it on the table to put it back; ID cards flip. Papers take touches only while you are seated; leaving gathers them. `Level3BeaTable` is their `IClientRecordHolder`.
- **Bag (Level 3-only fix in the parallel branch):** `HudHotbar` filled the Case Folder only when the *world* desk folder was carried, and Level 3 hides that folder, so Bea's papers would never appear. A new `HoldsCasePapers()` returns "owns the Case Folder" in Level 3 and the old check in Levels 1/2. The Case Folder is required (it cannot be discarded), so the handover never needs a free Bag slot.
- **Content defect fixed:** T02's case generator gave B-BG and C-BG two text fields with the same key (`…/period`). The facts are now labelled "Covers" (`…/covers`). Both case files were regenerated with only those two fields changed (diffed), and `Level3ContentValidator` now rejects a repeated field key within a paper.
- **Not done (deliberate):** Bea's walk to the office "may" follow the first independent worksheet admission. It is left for T05 together with the worksheet (L3-28).

### Files

New: `Assets/Scripts/Level3/Level3BeaTable.cs`, `Level3BeaInteractable.cs`, `Level3BeaTableSeat.cs`, `Level3RecordRules.cs`, `Level3TownContent.cs` (+ .meta); `Assets/Resources/Levels/Level_03/town_life.json` (+ .meta).

Changed:
- **Level 3 code:** `Level3ChapterRules.cs` (handover command, records trigger and lesson, B-WHT guard, coarse state through WorksheetDraft), `Level3Checkpointer.cs` (CommitBeaRecords, the records event, flags `beaRecordsCollected`/`atBeaTable`), `Level3OfficeStart.cs` (setup **v3**: BeaTable part), `Level3SceneAssets.cs`, `Level3ContentValidator.cs` (duplicate field keys).
- **Level 3 resources:** `scene_assets.asset` (beaPrefab, npcController, worktablePrefab), `objectives.json` (step 3), `Tutorials/Level_03/guide.json` (bea-records lesson; one waypoint to `Level3Gameplay/BeaTable/Bea`; trail on, no office arrows), both case files (the "Covers" fix).
- **Shared code:** `GameplayEvents.cs`, `ObjectiveTriggers.cs`, `OfficeObjectives.cs` (ClientRecordsCollected), and `HudHotbar.cs` (Level 3 `HoldsCasePapers`).
- **Tests:** `Tests/Level3Regression/Program.cs` (T04 section; two earlier assertions loosened for the longer objectives/guide and the setup version), `Tests/VerifyLevel3Scene.py` (arrows only to runtime places; Bea's anchor exists).

### Internal checks actually run

- `python Tests/make_scratch_build.py` (5 new scripts, 518 files) and `dotnet build Temp/ScratchBuild/Scratch.csproj --nologo`: **0 errors, 27 warnings** (the unchanged baseline).
- `Tests/Level3Regression`: **375/375**, including **49 T04 checks**.
  - The first run was 371/374. One T03 assertion pinned setup v2 (now v3, expected). One record check failed on the genuine B-BG duplicate-key defect (fixed in content and validator). One of my own checks was case-sensitive ("business-only" vs the printed "Business-only"); it was made case-insensitive.
  - The suite builds its own untracked bin/obj in place; I deleted them afterwards.
- Built out of tree (`Temp/CampaignChecks`): Level2Regression **1584 PASS**, CampaignFoundationRegression **748/748**, CampaignFoundationRuntimeRegression **16/16**, GuideRegression **937 PASS**, TrainingRegression **513/513**. No tracked `Tests/*/bin` file was rewritten.
- `VerifyLevel3Scene.py` **PASS, 3 warnings**:
  - the inherited missing `CaseDropDestination` script;
  - the entrance door leaf, which sits inside a prefab instance;
  - `Env/Town/bench (1)`, also inside a prefab instance; it is resolved at runtime, with a fallback to Bea's Level 1 spot.

  The build entry is present, after the original four. `VerifyLevel2Assets.py` **PASS, 11,921 unique .meta GUIDs**. `VerifyGuideTargets.py` 14 paths, 0 missing. Office_Level1/2/3 hashes are unchanged.
- Not run: Unity, Play Mode, rendering, NPC motion/grounding, placement clearance, paper readability on a phone, touch, audio, performance.

### Pending user acceptance

1. Open Office_Level3 (a direct Editor attempt saves nothing), check in, and finish the calculator practice. Bea's arrow leads to her table by the bench.
2. Check the table spot (not blocking paths, not clipping benches or trees, on the ground), the table's scale, the mat and envelope, and Bea's height, grounding, idle and facing. Confirm there is only one Bea.
3. Talk to her: introduction, records fanning out, the objective ticking, Arthur's hint. Cancel her introduction once first; it should restart next time with nothing handed over.
4. Sit at the table: paper readability, lifting and putting back, ID flip, Leave Table.
5. Ask all three questions after reading the party receipt, then the printing bill (order changes), then Talk later.
6. Check that the Bag's Case Folder lists her papers.
7. Check Android layout and Back.

### Next step

User Unity acceptance of T02–T04, then **T05**. The worksheet is admitted: the chapter is in WorksheetDraft and Bea's nine sources are acquired. T05 builds the source-linked worksheet on Bea's mat, using `Level3BeaTable` (seat view, `Viewpoint`, `LastReadSource`) and lifting strips from the `ClientRecordPaper` originals. It then decides whether Bea walks to the office after the first independent worksheet admission (L3-28).

## 30 October 2026 — T05 Bea's worksheet draft and source links

Status: **Implemented, internally verified; user manual pending**. T02–T04 Unity acceptance was still pending. T06–T18 remain Planned; T06 was not started.

### Found before starting

- T04's table already gives what the worksheet needs: a seated view (`EnterFirstPerson(TableViewpoint, "Leave Table")`, which puts the camera in Workstation mode), the papers as `ClientRecordPaper` world canvases, and `LastReadSource`.
- `MobileInputGuard.IsPointerOverUI` counts world-space GraphicRaycasters. That makes Bea's papers part of the UI hit test, so the board and the papers must not overlap, and the papers must stop taking touches while the board is up.
- Layer 7 ("3DInteractable") is what T03's calculator uses for physical taps: it is in the camera's PhysicsRaycaster mask but not the first-person Interactor mask. The board uses the same layer.
- The case content defines exactly R1–R7 as worksheet rows. R5 (B-EXP "Printing and supplies") and R7 (B-PRINT "Printing bill, paid") are both 30,000.00 and share PRINT-26. Note-R6 and note-R7 quote B-DRAFT's "Party purchases" (24,000.00) and "Printing bill" (30,000.00).
- CONTENT's book leaves "Costs and Purpose" and "Count Once" exist in no ticket's scope, and no Level 3 book leaves exist yet (L3-33).

### Design as built

- **Saved draft (signed save, inside the chapter snapshot):**
  - `Level3ChapterCheckpoint.worksheet` is a `Level3WorksheetDraft` holding: revision; strips {rowId, sourceId, amount, section}; notes {noteId, reason, related}; a bounded undo list (24); the selected source/row; the Income and Allowable expenses dirty flags; and `drafted`.
  - It is deep-copied with the chapter and repaired on load (nulls, duplicates, oversize undo).
  - A T04 save without it resumes with an empty draft.
- **Rules (`Level3WorksheetRules`, pure C#, always on a copy):**
  - **Extract:** only R1–R7, and only from a record actually handed over. A second Extract of the same row is a Duplicate that only selects the existing strip.
  - **Place:** every strip is accepted in every one of the four sections. Unknown section keys are refused.
  - **SetReason:** offers the five neutral reasons on both notes.
  - **SetRelated:** the printing note only, linking any other worksheet row (six equal choices).
  - **Undo:** restores the last change.
  - Edits are allowed only in WorksheetDraft and WorksheetCorrections.
  - A membership change in Income or Allowable expenses marks that subtotal dirty.
  - `drafted` is set once, the first time every strip has a section and both notes are filled in. This is structural only: a wrong draft counts the same as a right one.
  - Nothing reads `worksheetDecisions` or `expectedFields`.
- **Command boundary:** `Level3Checkpointer.CommitWorksheet(edit)` runs candidate → `CommitLevelThreeCheckpoint` (story runs) → publish, the same pattern as T03 tapes and T04's handover.
  - A failed write publishes nothing and says so; the board stays as it was, and the player can repeat the edit.
  - `WorksheetDrafted` is raised only from a published change (once). A resume marks it quietly (`ImpliedTriggers(state)`; GuideDirector uses the state too).
- **The board (`Level3WorksheetModel`, primitives + 3D TextMeshPro through `Level3DeskProps`, navy/cream/brass):**
  - A 0.50 × 0.26 m navy board with brass corners and three cream panels.
  - **Left, BEA'S RECORDS:** four tabs (Studio fees, Cost schedule, Party shop, Print bill); the selected record's rows with + ADD / ON SHEET; READ ORIGINAL; and the "lifted, not placed" pile.
  - **Center:** the paper sheet "WORTH THE TAX? · BEA 2026" with four bordered trays in a 2 × 2 grid. Income and Allowable expenses show a subtotal line that only says whether a calculator tape is needed.
  - **Right, CLAIM NOTES:** two yellow notes with Reason (and Related entry) fields, a brass UNDO key, and one line of plain guidance.
  - **Strips** are paper slabs with a brass edge, printed with label, amount and source id. They glide to wherever the saved draft puts them; piles fan tighter as they grow.
  - **Choices** come up on a raised navy chip card. All chips look alike; the current value keeps a thin frame.
  - No success tint, sound or ordering depends on correctness.
- **The desk (`Level3WorksheetDesk`, a component on BeaTable, built once the chapter reaches WorksheetDraft and all seven rows resolve):**
  - **At rest:** the board lies on the near half of the mat. `Level3BeaTable.LayOut` now lays the nine originals in one row across the far 42 % of the mat; it was two rows in T04.
  - **Lifting:** seated, tap the board and it lifts to 0.62 m, 80 % of the screen height. On screens narrower than 85 % of the whole-board fit it pages (records + sheet, or sheet + notes, via the < > tabs on the sheet's title band) instead of shrinking.
  - **Input:** one owner. Taps enter on release on layer 7 against the board's own targets. Strips can also be dragged onto a tray; a drop off every tray glides back.
  - **Papers:** while the board is up, Bea's papers are gathered and their raycasters are off (`HoldPapers`). READ ORIGINAL puts the board down and lifts the selected record's real paper (`ReadOriginal` → `TakeOut`).
  - **Settling:** PUT WORKSHEET DOWN (HUD pill, same style as PUT CALCULATOR AWAY), Leave Table, settings, a conversation, a pause, or focus loss all settle the board and any held strip without erasing anything.
  - **Audio:** existing cues only: card pick/place/return, paper, document open/close, UI select/click.
- **Guidance:**
  - **Objective 4:** "Build Bea's worksheet: place every row and fill in both claim notes." (event `WorksheetDrafted`).
  - **Arthur's hint lessons,** each waiting for the player's own action:
    - `worksheet-intro`: tap the board, then + ADD, then place (waits on cues WorksheetLifted, WorksheetStripLifted, WorksheetStripPlaced).
    - `worksheet-notes`: answer the notes (waits on WorksheetNoteAnswered).
    - `worksheet-drafted`: a reminder that everything can still change.
  - No lesson names an answer. Touch wording only. Guide flags: worksheetUp, worksheetAllPlaced, worksheetDrafted.
- **Not done (deliberate):**
  - Bea's optional walk to the office (L3-28, now proposed for T07).
  - Subtotal tapes, Check, grading, mastery and rewards (T06).
  - Book leaves (L3-33).
- **Setup:** no new Level3Gameplay part. Setup version stays 3 and no scene file changed.

### Files

New (each with a fresh unique .meta):
- `Assets/Scripts/Level3/Level3WorksheetRules.cs`
- `Assets/Scripts/Level3/Level3WorksheetModel.cs`
- `Assets/Scripts/Level3/Level3WorksheetDesk.cs`

Changed:
- **Save:** `RewardsSystem/PlayerProgressData.cs` (worksheet draft records).
- **Level 3 code:**
  - `Level3ChapterRules.cs`: worksheet trigger/lessons; state-based ImpliedTriggers; lessons implied by a started or drafted draft.
  - `Level3Checkpointer.cs`: CommitWorksheet, the drafted event, three flags, quiet resume by state.
  - `Level3BeaTable.cs`: one far row of originals, BuildWorksheet, HoldPapers, ReadOriginal.
- **Shared code:** `Core/GameplayEvents.cs`, `Data/ObjectiveTriggers.cs`, `UI/HUD/OfficeObjectives.cs` (WorksheetDrafted); `UI/Guide/GuideDirector.cs` (implied triggers from the chapter state).
- **Resources:** `Levels/Level_03/objectives.json` (step 4), `Tutorials/Level_03/guide.json` (three lessons, readme).
- **Tests:** `Tests/Level3Regression/Program.cs`.
  - New: a T05 section (148 checks).
  - Two T04 pins updated: objectives count ≥ 3; papers interactive only while seated and the board is down.
- **Docs:** T05 ticket, README, TICKETS, AGENTS, SCENE_REUSE, KNOWN_ISSUES (L3-28 moved, L3-32…L3-35), this entry.

### Internal checks actually run

- `python Tests/make_scratch_build.py` (521 files; 8 scripts not yet imported by Unity) and `dotnet build Temp/ScratchBuild/Scratch.csproj --nologo`: **0 errors, 27 warnings** (the unchanged baseline).
- `Tests/Level3Regression`:
  - **How it was built and run:** out of tree to `Temp/CampaignChecks/Level3Regression/Debug/net10.0`, run there with TEMP/TMP in `Temp/CampaignChecks/tmp`.
  - **Result:** **523/523**: T03 80, T04 49, **T05 148**.
  - **Earlier attempts:** the first two builds failed to compile because test locals clashed with earlier top-level names; they were renamed. One run from too shallow an output folder could not find the Resources; it was rerun at the right depth. The suite's untracked `obj` was deleted afterwards.
- Built out of tree (`Temp/CampaignChecks/<suite>/Debug/net10.0`):
  - Level2Regression **1584 PASS**
  - CampaignFoundationRegression **748/748**
  - CampaignFoundationRuntimeRegression **16/16**
  - GuideRegression **937 PASS**
  - TrainingRegression **513/513**

  No tracked `Tests/*/bin` file is newer than this ticket's sources.
- `VerifyLevel3Scene.py` **PASS, 3 warnings** (the same inherited three). `VerifyLevel2Assets.py` **PASS, 11,924 unique .meta GUIDs**. `VerifyGuideTargets.py` 14 paths, 0 missing.
- New and edited text files are CRLF UTF-8 (byte-checked).
- **Rule deviation, reported:** one read-only Git command (`git show HEAD:Tests/Level3Regression/Program.cs`, output discarded) was run by mistake inside a shell line. It changed nothing; no other Git command was run.
- **Not run:** Unity, Play Mode, rendering, touch, drag feel, readability on a phone, page tabs, audio, performance of one signed write per edit.

### Pending user acceptance

1. Open Office_Level3 (a direct Editor attempt saves nothing). Reach Bea's table after the handover and sit.
   - Check the nine originals in one far row: readable, not overlapping the board, not off the mat.
   - Check the board resting on the near half.
2. Tap the board. It lifts, Bea's papers stop reacting, and Arthur's first hint appears.
   - Check the size, readability on a phone (L3-32), and that the board is not clipped by the HUD or the camera near plane.
3. Lift rows from all four tabs.
   - + ADD on a row already lifted only selects its strip.
   - R5 and R7 are separate strips.
4. Place strips by tap-tap and by dragging. A drop off a tray glides back. Move strips between sections; check the subtotal lines.
5. Fill both notes from the chip card, including with R6/R7 in wrong sections.
   - Check Undo several times.
   - Check that no colour or sound differs between right and wrong choices.
6. READ ORIGINAL, then lift the board again.
   - PUT WORKSHEET DOWN, Leave Table mid-drag, settings, and Android Back all settle the board without losing placements.
7. Finish the draft: objective 4 ticks once, and Arthur's filled-in hint appears. Leave and re-enter (and, on a story save, reload): placements, notes and the objective restore quietly.
8. Check the Android layout and the narrow-screen paging (< >).

### Next step

User Unity acceptance of T02–T05, then **T06**:
- calculator subtotal tapes for Income and Allowable expenses (clearing the dirty flags);
- the one frozen 12-decision Check and corrections in WorksheetCorrections (same draft);
- the supported acquisition with its once-only 60 coins / +2 trust.

The book leaves "Costs and Purpose" and "Count Once" (L3-33) should land before that Check grades anything.

## 30 October 2026 — T06 subtotal tapes, the frozen check, corrections and collection

Status: **Implemented, internally verified; user manual pending**. T02–T05 Unity acceptance was still pending. T07–T18 remain Planned; T07 was not started.

### Found before starting

- T05's subtotal dirty flags were set but nothing cleared them, the calculator existed only on the office desk, and no Level 3 Tax Code Book leaves existed (L3-33).
- Level_03's `rewards.json` had no side-quest block, so the session score fell back to Level 1's "Know Your Taxpayer" badge defaults.
- `PlayerProgressData.Normalize` treats any `LevelRecord` as a completed level. My first design banked the worksheet XP as a Level_03 record; the new round-trip test caught Level 3 turning "completed" on the next save, so the design was changed (see Collect).

### Design as built

- **One calculator.** From WorksheetDraft to WorksheetReady the office desk's calculator is hidden (its practice card says "The calculator is with you at Bea's table") and the same Synty-bodied `Level3CalculatorModel` rests on Bea's mat to the board's right. Both use the one `Level3DeskCalculator.Calculator` logic. From BeaComputation it is back on the desk, showing whatever was last on it.
- **Subtotal tapes.**
  - Each money tray ends in a SUBTOTAL slot (an outlined pocket). Tapping it raises the calculator beside the sheet over the notes column (0.32 s; 0.52 m from the camera, 62 % of the screen height; sheet and calculator shrink together on narrow screens).
  - The tray being added up wears a brass frame and its strips a brass edge. Tapping a strip puts its printed amount on the calculator with its row and record (`strip:Rn`), with a small lift-and-settle. Keys enter on release over the same key, as on the desk. `=` prints the tape (0.2 s feed).
  - Tap SUBTOTAL once to name it ("Attach 900,000.00 to the Income subtotal? Tap SUBTOTAL again."), and again to attach. `Level3Checkpointer.CommitWorksheetTape` saves first; then the strip tears off and flies (0.3 s, arced) into the slot as a clipped tape (paper with shadow, curl and brass clip, "= 900,000.00"), which pops in. PUT CALCULATOR DOWN (the same HUD pill) returns it.
  - Accepted: any tape made of strip amounts with any operators, whatever set it adds, plus an explicit typed 0 for an empty section. Refused, with a plain line and nothing attached: typed numbers, practice-card or chained-result numbers, a strip not on the sheet, a non-money section, or a printed result that does not re-evaluate.
  - Moving a strip into or out of a money section, or undoing such a move, makes that section's tape stale. It stays, greyed, under a graphite "CHANGED · RECALCULATE" tag that pops in, and the message names the tape that is out of date. Undo now says what it put back.
- **Check.**
  - The notes column's key row is UNDO | HINT | CHECK, with pressed caps and a muted tint when unavailable.
  - CHECK tests completeness first. An incomplete worksheet circles every missing place in pencil for 6 s (the record tab of an unlifted row, unplaced strips, unanswered fields, missing or stale SUBTOTAL slots) and says "Not checked yet, so nothing was graded. Still needed: …". Nothing is saved.
  - The first complete check asks once: "CHECK NOW?" (a gently breathing brass key) with the scoring warning. The second tap saves it (`CheckWorksheet`). Only then does a pencil travel to each of the 12 places (0.11 s each), leaving a brass tick or a graphite "?".
  - A FIRST CHECK receipt then rises over the sheet. It counts up with keyboard clicks, shows "Sections 6/7  Reasons 2/2 / Link 1/1  Subtotals 2/2", pops the tier and medal (achievement cue), and folds into a navy stub at the foot of the records column ("FIRST CHECK · GRADE KEPT 11/12 SILVER", with the medal). Any tap skips the reveal.
  - Arthur says the tier line. The board adds a note that keeps arithmetic and treatment apart ("Your tapes add up exactly what is in their sections; the strips marked ? need another look.").
  - **Grading** (`Level3WorksheetCheck`, the only code that reads the answer key):
    - destinations and reasons are compared by label, and the link as R5/B-EXP/PRINT-26;
    - a subtotal is right when it adds exactly the strips the player put in that section, each once, with + only (an explicit 0 for an empty section);
    - the first complete check freezes the correct count, wrong decision ids, tier (Gold 12, Silver 10–11, Bronze 0–9), XP (60 − wrong, floor 48) and the draft revision. Later checks refresh only the marks and the values they read. A mark shows only while its checked value is unchanged.
  - All 12 right with current 900,000 / 300,000 → WorksheetReady. The CHECKED stamp lands (stamp press cue) and the key row becomes one breathing COLLECT WORKSHEET key. Edits, tapes and checks are refused from then on.
- **Hints.** After the first check, HINT gives three steps for the selected (or first) marked place, then moves on to the next marked place: where to look, with the book section; then the idea; then specific help. Hints are free and never saved; the text is in `worksheet.json`, with a built-in copy. The Tax Code Book gains Section 4 – Costs and Purpose and Section 5 – Count Once (`book.json`), the leaves the hints name (L3-33 resolved).
- **Collect.**
  - COLLECT WORKSHEET calls `CollectWorksheet`: Acquire moves Ready → BeaComputation and puts "B-WORKSHEET" in Bea's `workIds`.
  - A story run then writes everything in one candidate (`PlayerProgressManager.CommitLevelThreeWorksheet`):
    - the chapter;
    - the `Level_03/bea-worksheet` activity;
    - 60 coins ("Bea's corrected worksheet") and +2 Public Trust ("Bea's records organized and checked");
    - a once-only `…/bea-worksheet/mastery` claim;
    - the Worth the Tax? badge at its frozen tier.
  - The frozen XP stays in the chapter snapshot. As Level 2's quest XP does with its level result, it joins the career total with the chapter result (T14). No Level_03 level record is written early.
  - Only after the write returns does the presentation start:
    1. The board lifts as the stamp presses (0.5 s), then flies into the Case Folder icon (0.62 s, folder cue).
    2. The session score adds the frozen XP and badge (the HUD's own toasts and flight).
    3. `HudFeedback` announces the saved trust and coins with their reasons.
    4. The Side Quest Complete card shows the first check, the badge, and trust and coin tiles. Continue stands the player up from the table.
  - Leaving mid-flight finishes the presentation at once, exactly once. A failed write keeps the worksheet ready ("Couldn't save. Bea's worksheet is still here: tap COLLECT WORKSHEET to try again."). Editor, replay and practice attempts publish locally with no campaign grant.
- **Bag.** The Case Folder lists `L3-BEA-001/B-WORKSHEET` (a container document, no extra slot), with read-only pages of the player's own checked worksheet: sections, tapes, notes and the first grade.
- **Objectives and guide.**
  - Objective steps 5 ("Add up both subtotals, check the worksheet and fix anything marked.", WorksheetReady) and 6 ("Collect Bea's checked worksheet.", WorksheetCollected) are raised only from saved transitions.
  - New Arthur hint lessons: `worksheet-subtotals` (waits on the player's own calculator, tape and attach), `worksheet-check`, `worksheet-corrections`, `worksheet-ready` and `worksheet-collected`. None names an answer. The practice-done line now says "Take the calculator along".
- **Resume.**
  - The board is rebuilt quietly from the snapshot: tapes, stale tags, marks, stub, stamp and keys. A collected worksheet is not rebuilt (it is in the folder), and the session score gets its XP and badge back without toasts.
  - Objectives and lessons are implied by state, and the coarse game state stays ReceiveCaseState through WorksheetReady.
- **Audio** (existing families only):
  - keys: PlayKeyboardSFX; strip to calculator: `sfx_card_pickup_02`; tape feed: paper rustle; attach: `sfx_card_place_01`;
  - arm and select: ui_select; refusal: ui_click; check pass: ui_click on every other mark; count-up: keyboard;
  - tier: PlayAchievementSFX; stamp: `sfx_stamp_press_01`; flight into the folder: `sfx_folder_close_01`; calculator up and down: document open/close.
- **Not done (deliberate):**
  - Bea's walk to the office (L3-28, T07).
  - New Bea lines after the check or the collection (her source questions remain).
  - The Career board's badge paper for Chapter 3 (T18, `chapters.json`).

### Files

New (each with a fresh unique .meta):
- `Assets/Scripts/Level3/Level3WorksheetCheck.cs`
- `Assets/Scripts/Level3/Level3WorksheetHints.cs`
- `Assets/Scripts/Level3/Level3WorksheetDeskReview.cs` (the desk's second partial file)
- `Assets/Resources/Levels/Level_03/worksheet.json`
- `Assets/Resources/Levels/Level_03/book.json`

Changed:
- **Save:** `RewardsSystem/PlayerProgressData.cs` (`Level3WorksheetResult` on the chapter, `Level3CaseProgress.workIds`, copy/repair).
- **Rewards:** `RewardsSystem/CampaignRewards.cs` (Level_03 `bea-worksheet`: trust 2, coins 60; `MasteryKind`), `RewardsSystem/PlayerProgressManager.cs` (`CommitLevelThreeWorksheet`).
- **Level 3 code:**
  - `Level3WorksheetDesk.cs`: the calculator at the table, its layout, tape attach and flight, stale notes, undo wording.
  - `Level3WorksheetModel.cs`: slots, tapes, stale tags, marks, pencil rings, key row, stub, receipt, stamp, pencil, pops.
  - `Level3Checkpointer.cs`: three commands, two events, six flags, quiet score restore.
  - `Level3ChapterRules.cs`: triggers, lessons, coarse state.
  - `Level3DeskCalculator.cs`: `AwayAtBeaTable`.
  - `Level3BeaTable.cs`: passes the mat size to the desk.
- **Shared code:** `Core/GameplayEvents.cs`, `Data/ObjectiveTriggers.cs`, `UI/HUD/OfficeObjectives.cs` (WorksheetReady, WorksheetCollected); `UI/HUD/HudHotbar.cs` (folder work ids and the worksheet's reference page).
- **Resources:** `Levels/Level_03/rewards.json` (sideQuest block), `Levels/Level_03/objectives.json` (steps 5–6), `Tutorials/Level_03/guide.json` (five lessons, readme, practice-done line).
- **Tests:** `Tests/Level3Regression/Program.cs` (T06 section, 131 checks; no earlier assertion changed).
- **Docs:** the T06 ticket, README, TICKETS, AGENTS, SCENE_REUSE, KNOWN_ISSUES and this entry.

No scene, prefab, Level 1/2 content, Career, shop or settings change. The setup version stays 3.

### Internal checks actually run

- `python Tests/make_scratch_build.py` (524 files) and `dotnet build Temp/ScratchBuild/Scratch.csproj --nologo`: **0 errors, 27 warnings** (the unchanged baseline; none in Level 3 files). Rebuilt after every source change and after the mutation run.
- `Tests/Level3Regression`, built and run out of tree: **654/654**: T02 246, T03 80, T04 49, T05 148, **T06 131**.
  - The first runs were 650/653, then 651/653. Two failures were my own test mistakes: a rename that also changed words inside string literals, and expecting the claim label where R6's row label is "Birthday dinner, decor". The third was the genuine level-record defect above, fixed in the design.
  - A mutation run (re-freezing the grade on every check, paying on every collect) gave **644/654**: 10 failures across the freeze, collection, retry and round-trip checks. The source was restored byte-identical and rebuilt.
- Built out of tree (`Temp/CampaignChecks/<suite>/Debug/net10.0`):
  - Level2Regression **1584 PASS**
  - CampaignFoundationRegression **748/748** (its own pre-existing CS8321 warning)
  - CampaignFoundationRuntimeRegression **16/16**
  - GuideRegression **937 PASS**
  - TrainingRegression **513/513**
  - CareerBoardRegression **72/72**
  - ProgressionRegression **PASS**
  - IntroCutsceneRegression **44/44**
  - SettingsRegression **138/138**
- `VerifyLevel3Scene.py` **PASS, 3 warnings** (the same inherited three). `VerifyLevel2Assets.py` **PASS, 11,929 unique .meta GUIDs**. `VerifyGuideTargets.py` 14 paths, 0 missing. `verify_pack.py` **PASS**: 18 tickets (6 implemented, 12 Planned), 224 local links, 107 source references, 12 arithmetic fixtures, 313 T01 content assertions.
- No tracked `Tests/*/bin` file is newer than this session's start. The out-of-tree builds refreshed the existing `Tests/<suite>/obj` intermediates of 8 suites (L3-43); the session-created `Tests/Level3Regression/obj` was deleted.
- Every touched text file is CRLF UTF-8 (byte-checked). The Edit tool once left `Tests/Level3Regression/Program.cs` as LF; it was restored.
- No Git command was run.
- **Not run:** Unity, Play Mode, rendering, touch, drag/calculator feel, readability on a phone, audio, performance.

### Pending user acceptance

Story save/resume still needs the fixture route (L3-19). A direct Editor attempt saves nothing and grants no coins or trust, but shows everything else.

1. Reach WorksheetDraft and sit at Bea's table. Check the calculator on the mat: scale, clearance from the board and the mat/table edge. The office desk shows only the card, which says where the calculator is.
2. Tap Income's SUBTOTAL.
   - Check the rise and the layout: sheet on the left, calculator on the right, no overlap or near-plane clipping, the notes column covered.
   - Check the tray frame, strip edges, key and strip taps on a phone, the tape feed, arm/attach, the flight, and the clipped tape's readability.
   - Try typed numbers and an empty section with 0.
3. Move a strip in or out of a money section: the stale tag pops in and reads clearly. Attach a new tape.
4. CHECK with something missing: pencil rings and the message. Then complete it: CHECK NOW? warning, second tap, pencil pass, marks, receipt count/tier/medal/fold, stub, Arthur's line. Tap during the reveal to skip it.
5. Correct with HINT (three levels, then the next marked place) and CHECK AGAIN. The first grade must not change. At Ready, check the stamp and the COLLECT key.
6. COLLECT: stamp press, flight into the Case Folder icon, then the toasts (XP, badge, +2 Public Trust, +60 coins), the Side Quest Complete card tiles and standing up. Read the Bag's worksheet page. Repeat once leaving the table mid-flight.
7. Back at the office desk: the calculator has returned and shows the last tape.
8. Android: landscape and portrait, a narrow screen with the calculator up, Back, settings, an app switch mid-reveal and mid-collect, SFX off and on.

### Next step

User Unity acceptance of T02–T06, then **T07**:
- the calculator is back on the office desk;
- T07 consumes the collected worksheet (`workIds` "B-WORKSHEET" and its 900,000 / 300,000 subtotal tapes) for taxable income and the graduated tax.

T14 must add the worksheet's frozen first-check XP (`worksheetCheck.firstXp`, marked by the `…/bea-worksheet/mastery` claim) to the chapter result exactly once (L3-37).

## 30 October 2026 — T07 Bea's taxable income and graduated tax at the office desk

Status: **Implemented, internally verified; user manual pending**. T02–T06 Unity acceptance was still pending. T08–T18 remain Planned; T08 was not started.

### Found before starting

- `ComputeTaxesState` is a passive label (it only logs), but from it the inherited Level 1 stamp set (`StampSetInteractable`, present in Office_Level3) and the office corkboard move the game state to StampAssessmentState / AnalyzeEvidenceState.
- The existing CaseFolderUI / TaxComputationCalculator path is Level 1's float, flat-rate prototype, and Level 3 hides the desk Case Folder (T02). So the "familiar folder" is not reused as a 2D form: Bea's computation is a physical leaf on the T03 clipboard, and her identity/background papers stay readable in the Bag's Case Folder (L3-44).
- The calculator's "continue after =" replaced the previous working with a bare "result" number, so a chained tape (600,000 − 400,000 = × 20% = + 22,500) lost its provenance.

### Design as built

- **The sheet.** From BeaComputation the practice card is hidden and Bea's computation sheet lies on the same navy clipboard (0.19 × 0.30 m leaf, brass clip). It lifts with the calculator (sheet left at 84 % of the screen height, calculator right; both shrink together on narrow screens) and is headed "BEA MENDOZA · 2026 COMPUTATION / Individual · graduated rates · from her checked worksheet". Its lines:
  - **Gross income / Allowable expenses:** a field and a PICK key. PICK opens a raised chooser with the collected worksheet's Income subtotal (900,000), its Allowable expenses subtotal (300,000) and Bea's draft "Proposed costs" (354,000), all alike, the current one framed. Nothing is filled in until the player picks. A picked total shows its amount and where it came from; tapping it puts it on the calculator and shows its worksheet tape.
  - **Taxable income:** a slot for a tape.
  - **Rate band:** PICK opens the year's annual table, every row printed with its range and base-plus-excess formula (never pre-selected). The chosen row's FLOOR, BASE TAX and RATE appear as three source chips (the rate goes on as a percentage).
  - **Excess over the floor / Tax on the excess** (working, optional) and **Tax due:** slots. Tax due shows its value rounded to cents.
  - **Tax credits · Still to pay:** greyed, "next: Bea's withholding certificate" (T08).
  - One line of plain guidance that only says what the sheet still waits for.
- **Tapes.** The calculator now keeps a chain: an operator straight after = continues the same tape with a step separator, so `600,000 − 400,000 = × 20% = + 22,500 =` is one traceable tape printed with each step's result. The practice and the worksheet still use the last step only (T03/T06 unchanged). Tap a line once to name it ("Attach 62,500.00 to Tax due? Tap it again."), again to attach: `CommitComputation` saves first, then the strip tears off and flies into the slot (clip, shadow, curl, pop). A line with a tape and nothing printed offers its result and shows its working.
  - Accepted: numbers taken from the sheet (picked totals, the chosen band's constants, the tapes above the line) and numbers typed by hand (kept with a graphite TYPED · NO SOURCE tag; they never support an approval). Any such tape is accepted, whatever it computes; the review judges it (T09).
  - Refused with a plain line, nothing changed: practice-card or worksheet-strip numbers, another band's constants, a line below the target, an amount that changed since it was taken, a tape that itself needs recalculating, a result that does not re-evaluate, or no tape.
- **Invalidation follows sources.** A tape is out of date when any number it used no longer matches its source (a re-picked total, another band, an earlier tape that changed or is itself out of date). It stays on the sheet, greyed, under a NEEDS RECALCULATION tag, and the message names the lines affected. Changing the band leaves Taxable income current; changing a total invalidates Taxable income and everything built on it. Reading the sheet changes nothing.
- **Rules (`Level3ComputationRules`, exact decimals):** the content's six IND bands; the containing band is the first whose upper bound (inclusive) holds the amount, lower bound exclusive except the zero band; a negative amount has no band and stays visible. Tax due = base + rate × (taxable − floor), exact, rounded to cents half away from zero only at the end (L3-COMPUTATION-CENTS-v1).
- **Facts for T09 (`Evaluate`).** Per step: present, current, typed, follows (its own operation is right given the player's own inputs), supported (it follows and everything it rests on is supported), value and the case-scoped root (`income-sum`, `expense-sum`, `taxable-operation`, `rule-selection`, `tax-formula`). Taxable income must be exactly gross − expenses from the picked totals; Tax due must use the taxable basis, the chosen band's floor, rate and base (when non-zero), no typed number, and match the formula to the cent. Supported chains: step tapes, one chain, a chain straight from the totals, or an equivalent combined expression. Nothing on the sheet shows any of this.
- **Game state.** Collecting the worksheet (and resuming at BeaComputation) enters ComputeTaxesState. While Bea's computation is open, the chapter guard also refuses StampAssessmentState and AnalyzeEvidenceState (the inherited stamp set and corkboard); the legacy completion route stays refused.
- **Book, objective, guide.**
  - Tax Code Book Section 6 – Taxable Income, Section 7 – Annual Individual Table (2023 onwards, the six rows plus how to read them) and Section 8 – Base Plus Excess with the separate Practice Design Studio example (350,000 → excess 100,000 × 15 % = 15,000). No Bea figure.
  - Objective 7 "Work out Bea's taxable income and tax due at your desk." (`TaxDueAttached`, raised once when both lines first have current tapes; structural, never a grade).
  - Arthur hint lessons `computation-intro`, `-totals`, `-taxable`, `-rule`, `-tax`, `-done`, each waiting for the player's own pick or tape; they name the method and the book section, never a band or value. Resume skips lessons the saved sheet already shows taught.
- **Audio** (existing families): pick/choose ui_select, chosen paper rustle, source to calculator `sfx_card_pickup_02`, keys keyboard, tape feed paper, attach `sfx_card_place_01`, refusal ui_click.
- **Not done (deliberate):** credits and the remaining balance (T08); review, stamp and approval (T09); Bea's walk to the office (L3-28, now proposed for T10's delivery); no bridge to the old float case fields (nothing writes them).

### Files

New (each with a fresh unique .meta):
- `Assets/Scripts/Level3/Level3ComputationRules.cs`
- `Assets/Scripts/Level3/Level3ComputationLeafModel.cs`
- `Assets/Scripts/Level3/Level3DeskCalculatorComputation.cs` (the desk calculator's second partial file)

Changed:
- **Save:** `RewardsSystem/PlayerProgressData.cs` (`Level3ComputationRecord` on each case: picked totals and amounts, band, revision, `taxDueNoted`).
- **Level 3 code:** `Level3Calculator.cs` (chains: `DraftChain`, `EvaluateChain`, `ChainLines`; `SelectSourcePercent`), `Level3DeskCalculator.cs` (partial; the paper that lifts is the sheet from BeaComputation; the tape prints the chain), `Level3Checkpointer.cs` (`CommitComputation`, `TaxDueAttached`, ComputeTaxesState, phase-aware guard, five flags), `Level3ChapterRules.cs` (trigger, lessons, coarse state, `TransitionAllowed`).
- **Shared code:** `Core/GameplayEvents.cs`, `Data/ObjectiveTriggers.cs`, `UI/HUD/OfficeObjectives.cs` (TaxDueAttached).
- **Resources:** `Levels/Level_03/book.json` (Sections 6–8), `objectives.json` (step 7), `Tutorials/Level_03/guide.json` (six lessons, readme).
- **Tests:** `Tests/Level3Regression/Program.cs` (T07 section, 102 checks). Four earlier pins changed with this ticket's contract: T02's coarse-state check now pins BeaReview as unowned (BeaComputation shows Compute Taxes); T06's coarse-state, objective-count and book checks follow the added state, step and sections (its book leak check now reads Sections 4–5 only), and one T06 source check follows the collection event's new block form.
- **Docs:** the T07 ticket, README, TICKETS, AGENTS, SCENE_REUSE, KNOWN_ISSUES and this entry.

No scene, prefab, Level 1/2 content, Career, shop or settings change; setup version stays 3.

### Internal checks actually run

- `python Tests/make_scratch_build.py` (527 files) and `dotnet build Temp/ScratchBuild/Scratch.csproj --nologo`: **0 errors, 27 warnings** (the baseline; none in Level 3 files), first try and after every change.
- `Tests/Level3Regression` (out of tree): **759/759**: T02 246, T03 80, T04 49, T05 148, T06 134 (+3: the answer-key scan now covers the three new files), **T07 102**. The first run was 751/755: three of my own expectations (the existing percent display prints "20.00%", so a rate tape line reads "× 20.00% RATE"; one more check short-circuited on that) and the T06 source pin above.
- Mutation runs on `Level3ComputationRules`: making every tape current failed 4 invalidation checks; skipping the cents formula comparison was **not** caught at first (every negative also lacked a constant), so two negatives using every constant with wrong arithmetic were added and then caught it (753/759). The source was restored byte-identical and rebuilt.
- Out of tree: Level2Regression **1584 PASS**, CampaignFoundationRegression **748/748** (its own pre-existing CS8321 warning), CampaignFoundationRuntimeRegression **16/16**, GuideRegression **937 PASS**, TrainingRegression **513/513**, CareerBoardRegression **72/72**, ProgressionRegression **PASS**, IntroCutsceneRegression **44/44**, SettingsRegression **138/138**.
- `VerifyLevel3Scene.py` **PASS, 3 warnings** (the inherited three); `VerifyLevel2Assets.py` **PASS, 11,932 unique .meta GUIDs**; `VerifyGuideTargets.py` 14 paths, 0 missing; `verify_pack.py` PASS.
- No tracked `Tests/*/bin` file changed; the same 8 suites' `obj` intermediates were refreshed (L3-43); the session-created `Tests/Level3Regression/obj` was deleted. Every touched text file is CRLF (byte-checked). No Git command was run.
- **Not run:** Unity, Play Mode, rendering, touch, readability, audio, performance.

### Pending user acceptance

Story save/resume still needs the fixture route (L3-19); a direct Editor attempt saves nothing.

1. Collect Bea's worksheet (T06), walk to the office desk and sit. The calculator is back; the clipboard holds her computation sheet. Check scale, clearance and the lift (sheet left, calculator right, no clipping).
2. PICK Gross income and Allowable expenses: the chooser card, its three alike choices, the picked field with its source line; tap a picked total and read the worksheet tape it names.
3. Gross − Expenses =, then tap Taxable income twice: tape print, arm frame, flight, clipped tape.
4. PICK the rate band: readability of the six rows; the three constant chips; the rate entering as a percentage.
5. Build tax due both ways: step tapes (excess, tax on the excess, tax due) and one chain with = after each step. Check the chain's printed lines on the calculator tape and the clipped tape.
6. Re-pick the band, then a total: NEEDS RECALCULATION tags and messages; type a number into a tape: TYPED · NO SOURCE.
7. Try the practice numbers, a worksheet strip and a later line as sources: plain refusals.
8. The game state reads Compute Taxes; the old stamp set and corkboard do nothing to it. Android layout, Back, settings and app switch mid-attach; SFX on/off.

### Next step

User acceptance of T02–T07, then **T08**: Bea's withholding certificate (B-WHT, Type 8 face) handed over at the credit step, the credit-versus-expense explanation, and the Tax credits / Still to pay lines (62,500 − 12,500 = 50,000) on the same sheet, with credit-only changes leaving the tax lines current. T09 reads `Level3ComputationRules.Evaluate` for its review.

## 31 October 2026 — T08 Bea's withholding certificate, the credit and the remaining payable

Status: **Implemented, internally verified; user manual pending**. T02–T07 Unity acceptance was still pending. T09–T18 remain Planned; T09 was not started.

### Found before starting

- `SupportingPaperType.Withholding2307 = 8` existed, but `SupportingPaperTypes.Implemented` refused it, `Style` had no case (the grey "RECORD" fallback), `CaseRecordRules.TypeForPaper` mapped only 4/5/7, `SupportingDocumentFace` had no Type 8 face, and `Level3RecordRules` skipped paper type 8 on purpose. B-WHT's content (payer, payee, period, reference, income payment 250,000, tax withheld 12,500, credit link to R2) was already in `case_bea_001.json` from T01.
- Bea had no way to reach the office (L3-28); T07's sheet ended in a greyed "Tax credits · Still to pay" row.
- A record paper takes no touches while a dialogue is open (`ClientRecordPaper.InputAllowed`), and the carried Tax Code Book cannot open from the conversation camera (`Level2TaxCodeBookIcon.CanOpen`). Both shaped the conversation below.

### Design as built

- **Bea's office visit (L3-28 for the computation).** A short beat after her worksheet is collected she walks from her plaza table to the conference room's client spot (`Environment/OfficeBuilding/Rooms/ConferenceRoom/Client`, the spot Marie and Rosa use) with NpcLife's quest travel, over Rosa's Level 2 doorway points (town_life.json `bea.toOffice`); the room's stand-in client steps aside. A talk pauses the walk. A route that fails ends with NpcLife's out-of-view placement; without NpcLife or route points she is placed only while neither she nor the spot is on screen. A chapter resumed at her computation finds her in the room at once. Her place follows the phase: nothing is saved for the walk and arriving awards nothing (`Level3BeaOfficeVisit`, on her actor).
- **Talking to her during the computation.** On her way, or waiting before the tax due is on the sheet, she answers the same source questions ("I'll wait here while you work out the tax. Anything about my records?"). Once taxable income and tax due have been on the sheet (`taxDueNoted`):
  1. Her handover lines (the payment "is already in my menu fees") end on a docked TAKE THE CERTIFICATE choice. Taking it is one chapter command (`Level3Checkpointer.CommitCertificate` → `Level3CertificateRules.HandOver`: B-WHT acquired, action `Level_03/bea-certificate`, milestone), saved first.
  2. Only then does the certificate rise out of her hands into a readable pose above the dialogue box while she says "Here it is. Read it, then lower it when you're ready."; the next line lowers it.
  3. Her question (CONTENT): "A client withheld some tax from a payment. Is that another business expense?", with the three CONTENT answers on the docked card (plus Talk later):
     - *It reduces the remaining tax when supported by the certificate.* Saved (`CommitCreditExplanation`, action `Level_03/bea-credit-explained`); she replies and the talk ends. Nothing is filled in on the sheet.
     - *It reduces the income amount before the tax calculation.* Nothing is saved, no trust/ethics/grade: she suggests the book, and the docked card shows the Tax Code Book's Section 9 – Credit Versus Expense (the same leaf as in the book) with "Back to Bea's question", which returns to the same question.
     - *Let's inspect the certificate together.* The certificate rises again from the player's hands, then the same question returns.
  A failed save keeps the same choice on screen with a plain line; Talk later (or any interruption) leaves the step for the next talk (certificate still hers, or the question still due).
- **The certificate (Type 8).** The existing record paper (`ClientRecordPaper`: real thickness, edge, clip, contact shadow) with a new Type 8 face (`SupportingDocumentFace.BuildWithholding`): a blue header of its own (0x346884, distinct from the ID card's and the 2316's), its purpose ("Creditable tax withheld at source: tax the payer already paid for the payee."), a PAYER block, PAYEE and PERIOD, the INCOME PAYMENT band with "Already part of Menu illustration fees ₱400,000.00 on income record REV-B26 (2026).", the TAX WITHHELD band and the reference. `Level3RecordRules` builds it from the case document and the content's credit link; more payments than fit continue on the back (two per face), so the company's certificate packet uses the same paper (built and checked, the case not implemented). The standard (`SupportingPaperTypes`, `CaseRecordRules.TypeForPaper`) now implements Type 8; Type 3 stays planned. It is held by the player (`Level3CertificatePaper`): resting just below the view and hidden, raised by the conversation or at the desk, lowered again; never left floating. The Bag's Case Folder lists it like her other acquired papers (by source id, read-only page). It never appears on her plaza table.
- **At the desk.** Bea's sheet now has, under Tax due:
  - **Tax credits:** a field and PICK. Before the handover the field reads "after Bea's certificate"; before the explanation PICK says to answer her question first. Then PICK offers her certificate's two printed amounts alike (Income payment 250,000; Tax withheld 12,500); nothing is linked for the player. The linked amount is a calculator source (tap it).
  - **Remaining payable:** a tape slot ("a tape: tax due − credits"), shown to cents, a negative balance kept.
  - **A blue "2307" certificate tab** tucked under the clipboard's left edge: tap it (desk or lifted) to hold the certificate up in front of the tools; while it is up the tools take no touches and a tap anywhere lowers it. Leaving the desk, settings, a conversation or focus loss lowers it.
  Rows are now 22 mm apart (were 24) to fit; the lifted bounds include the tab.
- **Rules and facts (`Level3ComputationRules`).** `Credits` is a choice and `Payable` the fifth tape step; `SelectCredit` requires the acquired, explained certificate and one of its printed amounts (another case's certificate, a worksheet total or a missing link is refused). A payable tape may use only the sheet's own numbers (the linked credit, tax due, earlier lines, the band's constants) or typed numbers; the certificate's number straight off the paper, another case's credit, a changed credit, a later line or a stale tax due are refused. Facts for T09: `credits` (root `credit-source`: the content's credit row, from an acquired certificate of the case's payee inside the tax year, with the printed amount) and `remaining-payable` (root `balance-operation`: uses the credit and either the tax-due tape or the whole band formula from the basis, `RoundCents(printed) == RoundCents(RoundCents(tax due) − credit)`, no clamp), plus `PayableSupported`, `Complete`, `CreditInIncome` (the credit used above its line, i.e. as an expense), `CertificateIncomeIncludedIn` ("R2") and `Revision`. A credit-only change marks only the payable stale and moves the revision on (a later stamp of the old revision is out of date); every tax line stays current.
- **Objectives, guide, book.** Objective 8 "Meet Bea in the conference room and take her withholding certificate." (`WithholdingCertificateReceived`) and 9 "Link Bea's tax credit and attach what she still has to pay." (`RemainingPayableAttached`), both raised only from saved transitions. Arthur's hint lessons `credit-certificate`, `credit-link`, `credit-payable`, `credit-done` wait for the player's own handover, link and tape; no amount, no answer to her question, no arrow (the conference room is a known place). Tax Code Book Section 9 – Credit Versus Expense: an expense comes off income before the tax; tax withheld is a credit that comes off the tax due at the end; the certificate's income payment is already counted (count it once); same payee and tax year; a negative balance keeps its sign; its example is Practice Design Studio (15,000 − 2,000 = 13,000), no Bea figure.
- **Audio** (existing families): the certificate's paper pickup/place (`ClientRecordPaper`), document SFX as it leaves her hands, `sfx_book_open_01`/`sfx_book_close_01` around the help card, ui_select/ui_click for picks and refusals, `sfx_card_place_01` for the attached tape.
- **Not done (deliberate):** review, stamp, approval, rewards (T09); Bea's delivery and where she receives her copy (T10); the company case (T11/T12).

### Files

New (each with a fresh unique .meta):
- `Assets/Scripts/Level3/Level3CertificateRules.cs` (handover and explanation commands)
- `Assets/Scripts/Level3/Level3CertificatePaper.cs` (the held certificate, `IClientRecordHolder`)
- `Assets/Scripts/Level3/Level3BeaOfficeVisit.cs` (her walk to the conference room)

Changed:
- **Shared record presentation:** `Data/SupportingPaperTypes.cs` (Type 8 implemented and styled), `SideQuest1/ReviewedFileRules.cs` (`ClientRecord.Withholding`, `includedIn`), `SideQuest1/CaseRecordRules.cs` (8 → Withholding, two payments per face), `SideQuest1/SupportingDocumentFace.cs` (Type 8 face, colour, icon). Level 1/2 types and layouts unchanged.
- **Save:** `RewardsSystem/PlayerProgressData.cs` (`Level3ComputationRecord`: `creditExplained`, `creditChoice`, `creditAmount`, `payableNoted`).
- **Level 3 code:** `Level3ComputationRules.cs`, `Level3ComputationLeafModel.cs`, `Level3DeskCalculatorComputation.cs`, `Level3DeskCalculator.cs`, `Level3RecordRules.cs`, `Level3BeaInteractable.cs`, `Level3BeaTable.cs`, `Level3TownContent.cs`, `Level3Checkpointer.cs`, `Level3ChapterRules.cs`.
- **Shared code:** `Core/GameplayEvents.cs`, `Data/ObjectiveTriggers.cs`, `UI/HUD/OfficeObjectives.cs` (two events).
- **Resources:** `Levels/Level_03/town_life.json` (route, office lines, handover, question and answers), `book.json` (Section 9), `objectives.json` (steps 8–9), `Tutorials/Level_03/guide.json` (four lessons, readme).
- **Tests/verifiers:** `Tests/Level3Regression/Program.cs` (T08 section, 92 checks), `Tests/VerifyLevel3Scene.py` (office spot, desk, route = Rosa's). Pins changed with this ticket's contract: T04's "B-WHT is never built" now pins its Type 8 print and the table's exclusion; T07's facts list ends with the two credit steps and its book/objective counts are minimums; Level2Regression's T18 pins now expect Type 8 implemented (3 and unknown numbers still fall back).
- **Docs:** the T08 ticket, README, TICKETS, AGENTS, SCENE_REUSE, KNOWN_ISSUES and this entry.

No scene, prefab, Level 1/2 content, Career, shop or settings change; setup version stays 3.

### Internal checks actually run

- `python Tests/make_scratch_build.py` (530 files) and `dotnet build Temp/ScratchBuild/Scratch.csproj --nologo`: **0 errors, 27 warnings** (the baseline; none in a changed file), first try and after every change.
- `Tests/Level3Regression` (out of tree): **854/854**: T02 246, T03 80, T04 49, T05 148, T06 137, T07 102, **T08 92**. Before the T08 section, the existing suite gave 757/762: five expected pin failures from this ticket's contract (T04's "B-WHT is never built" and table source pin, T07's facts list, book count and objective count), updated as listed above. The first T08 build failed on a clashing test-local name (`start`, CS0136); after renaming it the suite ran 853/853.
- Mutation runs (each on a backup, restored byte-identical by sha256, then rebuilt): linking the credit without the explanation 852/853, no tax-year check 852/853, a balance clamped at zero 852/853, a handover before the tax due 852/853, no "already part of" line 852/853, a credit change that is not a revision 850/853 and an unreported credit-as-expense 851/853 were all caught. A balance that follows without using the credit was **not** caught at first (853/853: every negative also had wrong arithmetic), so FIXTURE's "unsupported cancellation to a matching balance" was added (tax due − tax due × 20% = 50,000 with no credit) and then caught it (853/854).
- Out of tree: Level2Regression **1583 PASS** (1584 before: its T18 "planned types" loop no longer lists 8; first run 2 expected pin failures), CampaignFoundationRegression **748/748** (its own pre-existing CS8321 warning), CampaignFoundationRuntimeRegression **16/16**, GuideRegression **937 PASS**, TrainingRegression **513/513**, CareerBoardRegression **72/72**, ProgressionRegression **PASS**, IntroCutsceneRegression **44/44**, SettingsRegression **138/138**.
- `VerifyLevel3Scene.py` **PASS, 3 warnings** (the inherited three) with the new office-spot, desk and route-equals-Rosa's checks (its first run stopped on a quoting mistake in one of my new lines, fixed); `VerifyLevel2Assets.py` **PASS, 11,935 unique .meta GUIDs** (+3); `VerifyGuideTargets.py` 14 paths, 0 missing; `verify_pack.py` PASS (8 tickets implemented); `verify_content.py` PASS (313 assertions).
- No tracked `Tests/*/bin` file changed; the session-created `Tests/Level3Regression/obj` was deleted; the other suites' `obj` intermediates were refreshed as before (L3-43). Every touched text file is CRLF (byte-checked). No Git command was run.
- **Not run:** Unity, Play Mode, rendering, Bea's walk, touch, readability, audio, performance.

### Pending user acceptance

Story save/resume still needs the fixture route (L3-19); a direct Editor attempt saves nothing.

1. Collect Bea's worksheet (T06). Watch her leave her table a moment later and walk into the office; check the walk (doors, corridor, no clipping, no visible jump) and that she ends at the conference room's client spot facing in, with the usual stand-in client gone.
2. At your desk, finish Tax due (T07). Tax credits reads "after Bea's certificate"; PICK says she is waiting. Read objective 8 and Arthur's hint.
3. In the conference room talk to Bea: her lines, the TAKE THE CERTIFICATE card, then the certificate rising out of her hands above the dialogue box. Check its Type 8 face on a phone in both orientations: blue header, purpose line, payer, payee, period, income payment with "Already part of Menu illustration fees ₱400,000.00 on income record REV-B26 (2026).", tax withheld, reference, paper depth and clip.
4. Answer the question each way: the wrong answer (help card with Section 9, readable; "Back to Bea's question"); inspect (certificate up again, then the same question); the right answer (her reply; nothing filled in on the sheet). Talk later mid-question and talk again: the question returns. Open Settings or switch apps mid-conversation: the certificate never stays up.
5. Open the Bag's Case Folder: B-WHT is listed with its fields.
6. At the desk: tap the blue 2307 tab with the tools down and lifted (certificate in front, tools frozen, a tap lowers it). PICK Tax credits: both amounts alike. Link the tax withheld, then Tax due − Tax credits = and attach to Remaining payable (50,000). Try the income payment as the credit (−187,500 kept), change the credit (only Remaining payable NEEDS RECALCULATION) and change it back.
7. Leave and reload: Bea is in the conference room, the certificate is in the folder, the sheet and its tapes are as left; nothing replays.
8. Android: Back, repeated taps on TAKE/answers, rotation with the certificate up, SFX on/off.

### Next step

User acceptance of T02–T08, then **T09**: stamp and review Bea's computation, reading `Level3ComputationRules.Evaluate` (now with `credits`, `remaining-payable`, `Complete`, `CreditInIncome` and `Revision`); Bea's computation is complete on the sheet but unreviewed.


## 3 November 2026 — Revision authoring checks actually executed

- `python3 -B Documentation/Level3TheNumbersTellAStory/verify_content.py`: PASS343 stable/legacy/revision assertions with Decimal. Separate old twelve-decision and current nine-decision registries, source bindings, financial chains and v2 scoring fixtures checked.
- `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py`: PASS18 tickets (1 retained implemented baseline,17 Planned revisions), dependency DAG/order,196 local links,122 existing source references,13 arithmetic fixtures,343 content assertions. Both Python validators compile in memory.
- Seven in-memory altered-contract probes rejected: duplicate decision, wrong treatment, wrong batch, changed tier, changedXP, lost legacy treatment identity and changed financial amount. Clean content returned343 before/after; no production/doc fixture mutated by probes.
- Ticket sizing inspection:468–761 words each at this check, below the1250-word review bound. Scope remains bounded to reuse slices, not generic systems.
- No production build/regression/Unity/device check run for this documentation-only revision; no gameplay pass claimed. Historical runtime tests/counters/manual warnings above remain unchanged.
- Current next action is revisedT02, with existing T09 code and old save compatibility verified first. All revised runtime/manual acceptance remains pending.

## 4 November 2026 — T03 original-body and original-strip dragging; neutral arrival

Status: **Implemented, internally verified; native visual/input/phone acceptance pending**. This bounded repair supersedes earlier clip-only/floating-tape presentation guidance. User confirms printed numeric amounts now transfer successfully; that working path is retained. T04 remains next; no T04–T18 implementation, Git, Unity/editor launch/automation, real-save access, scene/prefab/package or shared Level1/2 code change.

### Inspection and actual routing

Read the current T03/design/architecture/feedback/verification guidance, bootstrap and calculator/input/model/command paths, plus existing FloatingModel3D/CaseFolder3DDrag, ClientRecordPaper, Level2StampDrag, HeldOnScreen, Doc3DLayer and shared transfer/visual feedback seams. Inspected actual supplied image `codex-clipboard-d0a89b18-13e0-4988-b25a-f09c4b9abbb7.png`: large example clipboard LEFT, calculator RIGHT displaying770.00, short printed strip in the gap with a blue outline. An outline alone does not establish a drag ghost. The user's behavioral report and source establish the defect.

Level3OfficeStart creates the same DeskCalculator. Lift binds Level3DeskToolHitTarget directly to each Doc3DLayer root. Raycast uses ToolAt/Pick/PickTool and current enabled local boxes. Pointer-down captures that collider/source; BeginDrag formerly sent any source to ValueDragSource, and only explicit clip/roll/borders to movement. The existing broad BodyTarget was neither, so ordinary blank grabs did nothing. The lesson source targets cover their two printed rows (0.038m each), the field is separate, and broad backing lies underneath. Calculator keys/display/printed strip/page target are independent content. This is a role/bounds defect, not another layer7/mask/ingress defect.

The strip source formerly used the default shared label; StartAttachFlight independently instantiated an Attaching Tape copy. The old movement rectangle was only0.67 viewport heights for a paper fitted around0.66; it also permanently split unused middle space. These constraints could leave very little useful movement. Drop formerly attempted any retained selection without checking the captured drag role.

### Changed paths and behavior

All runtime paths below are under `Assets/Scripts/Level3/`:

- `Level3CalculatorModel.cs`, `Level3PracticeCardModel.cs`, `Level3ComputationLeafModel.cs`: existing BodyTarget joins existing clip/roll/borders as a physical movement target. Other colliders do not become parent-movement fallbacks. Accepted amount/label positions/font sizes and models remain.
- `Level3DeskToolHitTarget.cs`: fixed Content/Tool/Value/PrintedStrip role at pointer-down, one capture across tool surfaces, original press-position grab offset, threshold-driven click suppression. Drop requires the same pointer, an actually active value drag and that source's shared selection; keys/display/field/body drags cannot place an earlier selected number. Clear/disable release only owned capture/drag. Existing GraphicRaycaster ingress and MobileInputGuard remain.
- `Level3DeskCalculatorTape.cs`: move original Root with stable orientation/depth/grab offset, modest pickup and0.18s settle. HeldOnScreen bounds use actual neighbouring assembly edges to share free middle space; reserved review-stamp columns remain when present. Vertical workspace is0.13–0.82 of the viewport, with hotbar/guide/buttons reserved. No scale reduction. Printed-strip view snapshots original parent/sibling/local pose/scale and individual renderer/collider states once, detaches the same TapeStrip preserving world pose/scale, disables its colliders and renders through one temporary existing Doc3DLayer. Drop coordinates stay at the real pointer. Invalid drop returns in0.2s; cancel/stale/close restores ownership, while changed drafts rebuild from current data instead of old text. Expression/source/case/revision identity remains authoritative.
- `Level3TapeTransfer.cs`: only the printer strip sets ValueDragSource.ShowVisual=false. Ordinary amount and attached-result transfers retain the shared numeric visual/selection. End/cancel/disable call the shared drag end and release its global marker. No new transfer manager or polling loop.
- `Level3DeskCalculator.cs`: Pick ignores its own held/flying visual layer when discovering destinations. StartAttachFlight reuses the captured original strip for BOTH tap and drag attachment, after the existing commit; no Instantiate/drag clone. A0.22s restrained flight aligns the visible strip centre with the field, then hands off to the stored slot and returns the printer view to its owner. Interruptions finish presentation without another command. Keys/new operands settle an earlier view before changing its draft. Existing lesson handoff/client working/once-only marker remain.
- `Level3DeskCalculatorComputation.cs`: shared manager owns transfer audio, removing the second card-place cue and duplicate refusal-select cue from attachment paths (also removed in DeskCalculator).
- Existing model Tick/ShowTape/PopSlot/StartPop seams provide destination feedback: input digits/receiving tape get a local0.2–0.22s pulse, at most4.5%, relative to their actual nonuniform base scale (including0.001 text). ShowTape refresh no longer starts feedback. Only accepted insertion/flight landing triggers it; restore/selection/CanAccept do not. Interrupted feedback restores its captured scale. No correctness badge/celebration or whole-calculator scaling.

Tests: `Tests/Level3Regression/Program.cs` adds89 managed checks (1205 ->1294). Real model body predicates use inert non-null identity tokens, without native collider creation; role/content priority/drop eligibility, per-source visual choice, shared end/disable marker cleanup, strip release decisions, nonuniform text scale and practical clamp travel run against production methods. Existing immutable-payload/same-result-stale/shared-manager/duplicate/save-failure/lesson/legacy math checks remain. Actual reparenting/renderer restoration/EventSystem dispatch/compositing/touch are native and **not exercised** by standalone tests.

### Commands and evidence actually run

- Before changes: `python3 -B Tests/run_level3_repair.py --level2`: runtime0 errors/21 inherited warnings; whole Editor0 errors/10; Level3 **1205/1205**, Controls **5734**, Level2 scratch **1580**, exactly **3 native EventSystem comparer checks skipped**. These supersede no historical evidence.
- Final current-source command: `python3 -B Tests/run_level3_repair.py --level2`: runtime0 errors/21 inherited warnings; whole Editor0 errors/10; Level3 **1294/1294**, Controls **5734**, Level2 scratch **1580**, same3 skipped native checks. Output only under Temp/L3Repair, using discovered current Bee responses and Unity6000.4.5f1 bundled Roslyn/runtime. No .NET SDK or tracked Tests/*/bin writes.
- Red replay: `python3 -B Temp/L3PhysicalBaseline/replay_body_baseline.py` compiles disposable copies with the three original body-excluding predicates restored: **1291/1294**, exactly the calculator/card/leaf blank-body checks fail. Production and current test DLLs are untouched. Initial test scaffolding hit native Unity identity/equality limitations; inert cached-pointer/entity tokens made the model predicates runnable. One added test-local variable shadowing error was corrected before the final suite. No native UI repro is claimed.
- `python3 -B Tests/VerifyLevel3Print.py`: **28 PASS**, actual shipped-font geometry, not rendered meshes.
- `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py`: PASS; `python3 -B Documentation/Level3TheNumbersTellAStory/verify_content.py`: PASS343. Documentation/content only.
- Pack validation initially rejected the expanded T03 ticket (>1250 words). The new follow-up was reduced to a compact PROGRESS pointer, retaining its existing history; final ticket1248 words, pack PASS216 links/122 source references/13 fixtures/343 assertions.
- Changed-file comparison uses a pre-edit Temp snapshot and Python difflib, no Git. T03, PROGRESS, KNOWN_ISSUES, WORLD_AND_FEEDBACK and REUSE_AND_ARCHITECTURE updated; history retained. All touched project text stays UTF-8 CRLF.

### USER manual route (still required)

1. Stop Play, choose **Tools > TaxVenture > Level 3 > Fixture: First Office Calculation**, restart Play in Office_Level3 to rebuild runtime targets/layers.
2. Grab normal non-content paper/calculator areas, move/release. Actual assemblies must follow and settle at valid released positions, with useful travel, no numeric ghost, content hijacking, HUD overlap or interpenetration.
3. Transfer Supply fund, choose−, transfer Spent on ink, press=. Numeric transfer must still work, with brief local display feedback.
4. Drag the short printed strip: same physical strip follows, no value tag/copy. Outside drop returns; valid drop commits then clips/pops into Left in the fund. Repeat tap-select/place and interrupt during drag/return/settle.
5. Check mouse and Unity Remote/phone; focus/Settings/Back/Exit/reopen/rotation; no duplicate/lost attachment and preserved client working after lesson handoff. Stop Play and set fixture to **None (fresh start)**. Fixture is attempt-local and cannot prove live-save persistence.

## 4 November 2026 — Whole-screen calculator/paper movement, touch ordering and receipt motion

User requested free placement anywhere on screen (bounded by screen edges), whichever assembly is touched comes forward, and smoother receipt pickup/drag/drop/float/pop. Supplied screenshot `codex-clipboard-23845b29-a54b-41e7-8d5f-df94e100a50b.png` shows the computation leaf left, calculator right displaying36.00 and printed strip between them. It does not establish native drag acceptance. This request **supersedes the earlier disjoint-column/neighbour/non-overlap/HUD-clearance movement restrictions** for these tools. Initial readable layout and ordinary HUD input ownership stay; tools may intentionally overlap and cross sides. No full progressive-computation/T04–T18 implementation.

### Changes (existing owners only)

- `Assets/Scripts/Level3/Level3DeskCalculatorTape.cs`: both tool roots now clamp directly through existing HeldOnScreen.Clamp against the full camera pixelRect and its standard12px edge margin. Removed neighbour/stamp/guide/hotbar column restrictions and double-margin workaround. Measure the actual lifted assembly before clamping so the pickup's depth shift is included. Tools retain their released positions until the existing put-away/reopen/reset boundary; normal content keeps its captured role.
- `Assets/Scripts/Level3/Level3DeskToolHitTarget.cs`: every captured pointer-down brings its existing Doc3DLayer forward with BringToFront(false), including key/display/source/field taps. Target/role are captured before order changes, so the touch cannot retarget through an overlapping tool. Exposed portions of a covered tool remain available to bring it forward; hidden content does not click through opaque paper. Existing EventSystem/raycaster/pointer ownership remains.
- `Level3DeskCalculatorTape.cs`: original receipt lifts over0.14s with modest finger clearance,3-degree tilt and2.5% scale emphasis; follows using Unity's Vector3.SmoothDamp with explicit unscaled delta/0.035s response. The fully lifted mesh is measured once with HeldOnScreen for drag bounds. Actual real pointer still discovers drops; visual smoothing/offset never substitutes for it. Invalid drop returns from its displayed position/rotation/scale over0.24s, easing all three back to the printer. Existing payload/staleness/renderer/collider/parent/cancel restoration remains.
- `Assets/Scripts/Level3/Level3DeskCalculator.cs`: removes unused divider state. Saved attachment focuses destination paper, then raises the same travelling strip above it. Tap/drag use one0.28s flight from actual release pose, a restrained arch with zero endpoint slope, smooth acceleration/deceleration and actual receiving-strip dimensions. Its visible paper centre lands on the receiving plane instead of using the elevated collider centre or arbitrary uniform0.85 scale. Original strip/stored view handoff remains once-only after commit; no new prefab/ghost/clone or monetary authority.
- `Assets/Scripts/Level3/Level3CalculatorModel.cs`: exposes the actual printer paper centre/dimensions for fitting, plus shared1.5mm local landing offset. `Level3PracticeCardModel.cs` and `Level3ComputationLeafModel.cs` expose/reuse their unchanged stored-paper dimensions; existing PopSlot/Tick/StartPop now settle that paper down from the matching landing height while giving the restrained4.5% base-scale pulse. Cancellation restores position as well as scale. Non-slot stamp/picker/stale feedback does not acquire drop translation. Existing0.001 print scales/font sizes/source positions unchanged.
- `Tests/Level3Regression/Program.cs`: replaces obsolete disjoint-column tests with whole-viewport/four-corner/portrait/letterbox/cross-divider comparisons against actual shared HeldOnScreen math; adds actual receipt easing/flight endpoint/slot-fit/settle geometry coverage. Earlier exact decimal/shared manager/role/cleanup/stale/same-result/once-only/save/legacy checks remain.

### Executed verification

- New whole-screen assertions against the prior runtime were red: **1283/1306**,23 failures for column limits/margin mismatch/cross-divider movement. After current-source compile, the movement slice passed1306/1306. With receipt math coverage, final Level3 **1337/1337**.
- `python3 -B Tests/run_level3_repair.py --level2`: current runtime0 errors/21 inherited warnings; complete Editor0 errors/10 inherited warnings; Level3 **1337/1337**, Controls **5734**, Level2 scratch **1580** with exactly **3 native EventSystem comparer checks skipped**. Current discovered Bee response files and Unity6000.4.5f1 bundled compiler/runtime, outputs in Temp/L3Repair; no Tests/*/bin writes.
- `python3 -B Tests/VerifyLevel3Print.py`: **28 PASS** actual shipped-font geometry. `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py`: PASS; `python3 -B Documentation/Level3TheNumbersTellAStory/verify_content.py`: PASS343. No visual/phone/input/compositing/audio acceptance follows from these commands.
- Review uses Python difflib against a pre-edit Temp snapshot; changed project text is UTF-8 CRLF. No Git, Unity/editor launch/automation, real-save, scene/prefab, package, new art/manager or reward change. Current guidance updated in WORLD_AND_FEEDBACK, REUSE_AND_ARCHITECTURE, OFFICE_CASE_PLAY and T03, with historical evidence retained here and in KNOWN_ISSUES.

### Native acceptance pending

Stop Play, choose Tools > TaxVenture > Level 3 > Fixture: First Office Calculation, restart Office_Level3. Drag both tools across former sides and into all corners; whole assemblies must stay within screen and retain released positions. Overlap them, tap an exposed blank area/key/source/field, and verify the touched assembly comes to front with no content hijack or click-through. Complete1,250−480=770; try original-receipt slow/quick drags, release far/near the field, invalid outside drop, tap-select/place, and pickup/return/flight/settle interruptions. Repeat on computation leaf, mouse/Remote/phone, Settings/focus/Back/Exit/reopen/rotation. Confirm smooth transitions, correct clip alignment, no clone/lost/duplicate attachment and preserved client draft. Stop Play; reset fixture to None (fresh start). Fixture remains attempt-local and cannot prove live-save persistence. User has not yet accepted the new native feel.

## 4 November 2026 — Held calculator and physical receipt foreground repair

User screenshots `codex-clipboard-0f2c2c24-8877-4f21-8f42-38650ad75a28.png` and `codex-clipboard-e7125595-da4e-47e1-a95d-9a1dd286192b.png` show the calculator obscured by the example clipboard and the dragged receipt partly behind it. User explicitly reports holding calculator/receipt in those states. Prior internal ordering declarations did not establish native acceptance; this reopens that acceptance. Same working value transfer, free-screen movement, existing physical models/receipt, immutable payload/save authority retained.

### Trace and scoped repair

Inspected current Doc3DLayer creation/render/sorting/canvas lifetime, FloatingWindow shared ordering/renumbering, HudWindowFocus, shared stamp foreground behavior, calculator pointer capture and original receipt capture/return/flight cleanup. Existing press/start called BringToFront only once. Shared UI focus can subsequently change document order; original receipt return/flight outlives its source pointer. Doc3DLayer cached sorting order also lacked the per-frame repair of actual Canvas.overrideSorting/sortingOrder already used by HudSortingKeeper. These are source-established gaps; a live native capture proving the exact cause of both screenshots is unavailable without Unity, which was not launched.

Changed paths:

- `Assets/Scripts/UI/HUD/Doc3DLayer.cs`: opt-in KeepInFront reservation, default false for existing documents. Its existing LateUpdate500 reasserts reserved foreground after ordinary focus/input updates, before capture display. It also restores actual Canvas.overrideSorting/sortingOrder from its cached order even when BringToFront considers the layer already top. Creation explicitly specifies ScreenSpaceOverlay. Existing shared document order allocator/capture path remains; no camera/layer-mask/depth/polling-manager replacement.
- `Assets/Scripts/Level3/Level3DeskToolHitTarget.cs`: captured assembly owns the reservation through press/hold/drag; Clear releases it without demoting the last touched tool. Original physical receipt takes precedence over former parent while detached. No content/target recapture or accidental placement from physical tool drag.
- `Assets/Scripts/Level3/Level3DeskCalculatorTape.cs`: original receipt's existing temporary layer reserves foreground from capture through hold/return/saved attachment flight. Restore releases it before disabling/destroying that layer. Existing Pick still excludes the receipt visual layer, and its disabled colliders still permit real-pointer destination discovery. Existing command-first attachment/handoff/cleanup remains unchanged.
- `Tests/Level3Regression/Program.cs`: eight new checks for the opt-in property/default, foreground ownership precedence and reservation release. The initial missing-seam assertion ran red1337/1338. Final current-runtime suite1345/1345. These are managed lifetime-policy checks, **not native Canvas rendering or gesture acceptance**.
- `Assets/Editor/DeskToolLayerValidation.cs`: extends the existing diagnostic with native calculator/receipt versus clipboard foreground checks after competing focus and simulated actual Canvas sorting resets, plus release checks and an original receipt preview/capture. Complete Editor compilation verifies these callers. **Not executed**: local instructions prohibit Unity/editor launch/automation. The diagnostic remains user-invoked via Tools > TaxVenture > Run Desk Tool Layer Check; Close Desk Tool Layer Preview cleans the temporary preview. It does not save a scene.

### Commands/results and limits

`python3 -B Tests/run_level3_repair.py --level2`: current runtime0 errors/21 inherited warnings, complete Editor0 errors/10 inherited warnings; Level3 **1345/1345**, Controls **5734**, Level2 scratch **1580** with exactly **3 native EventSystem comparer checks skipped**. Dynamic current Bee responses and Unity6000.4.5f1 bundled Roslyn/runtime; outputs under Temp/L3Repair, no Tests/*/bin writes. Existing1345 checks preserve math/provenance/save/lesson/legacy/input checks; eight additional assertions do not prove GPU compositing.

`python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py`: PASS. `python3 -B Documentation/Level3TheNumbersTellAStory/verify_content.py`: PASS343. Diff review via Python against pre-edit Temp snapshots, no Git. Changed project text UTF-8 CRLF. No Unity launch, native diagnostic execution, phone check, real-save, scene/prefab/package or later-ticket change. Historical screenshots/results remain preserved; native layering acceptance is pending.

Native route: Stop Play and restart with Fixture: First Office Calculation. Overlap tools, grab exposed calculator body/roll and drag across clipboard: calculator must cover it throughout hold and remain front after release. Tap/grab clipboard's exposed area: it must come forward in turn. Calculate1,250−480=770, drag original receipt over the whole clipboard and field: full receipt must remain visible above paper through hold/drop/clip animation while field detection still works. Test invalid return, tap-place, Settings/focus/Back/Exit/rotation/reopen and computation leaf; no ghost/duplicate/lost attachment. Mouse/Remote/phone and native Canvas/render acceptance remain user pending. Reset fixture to None (fresh start); fixture does not prove live-save persistence.

## 4 November 2026 — T04 tabletop card input and readable staging follow-up

User confirms Settings and Leave Table work, but stand/original cards do not. Screenshot `codex-clipboard-9128ec8f-7356-46f7-b643-c1f7447d2cdc.png` also shows small supporting card text and machine labels. T04 visual/input acceptance is reopened; T05 remains next and machine actions stay passive.

### Inspected flow and changes

Read current AGENTS/T04 implementation, Level3BeaTable/Level3PaperMachineModel/Level3TableStaging, ClientRecordPaper/RecordTouchForwarder, CameraController/WorkstationInteractor/MobileInputGuard and installed UGUI GraphicRaycaster. Table seating already sets Workstation mode, camera and interactive flags. Both card and originals depend on invisible world-space Graphic hit meshes; the installed raycaster rejects a hit before callbacks for native canvas cull or depth=-1. cullTransparentMesh=false alone does not bypass those conditions. Stand's InspectActive and original's existing ClientRecordPaper handlers already own read/return. No native event trace was executed, so the precise native filter responsible is not claimed proven by the screenshot alone.

- `Assets/Scripts/Level3/Level3PaperMachineModel.cs`: added Level3TablePaperRaycaster in this existing file. It is an EventSystem GraphicRaycaster subclass on each existing touch rect, intersects current local rectangle math directly, and returns the same GameObject/handlers with real world distance and document/canvas ordering. No invisible Graphic depth/cull, extra collider, camera mask, alternate polling loop or global manager. Stand no longer creates the transparent hit Image. Its whole card including Inspect Original uses this one raycaster; dragging past the EventSystem threshold cancels a card click.
- `Assets/Scripts/Level3/Level3BeaTable.cs`: opts only this table's original RecordTouchForwarder surfaces into that same direct raycaster, disabling/removing their previous raycaster. ClientRecordPaper's existing open/read/flip/drag/return/one-reader stack behavior remains. Shared L1/L2 code unchanged. CanTouchPapers gates both stand and originals on the actual seated/free camera state and reward screen; Settings/dialogue/pause/camera transitions/leave still block touches. Existing raycaster lifecycle registers/unregisters via the inherited base callbacks.
- Readability: table viewpoint12% closer along its existing sight line (roughly14% larger projection near the focus), anchored objects/table/world retained. Card height0.16→0.18m; purpose105→125pt, issuer/details95→125pt, record position85→105pt, Inspect105→130pt. Secondary card text now uses full Ink contrast. Issuer occupies one line and date/reference another, preserving all facts without shrink-to-fit. No title/amount reduction. Larger stand footprint0.20→0.22m deep, stand/stack gap0.03→0.015m; all turned footprints remain apart/inside the mat and nominal full-size scale remains1.
- `Assets/Scripts/Level3/Level3TableStaging.cs`: footprint/gap tuning above; source identities/stack/domain rules unchanged.
- Machine label polish in existing model: selector labels70→105pt, total headings55→90pt, larger measured rectangles. Selector labels form three clear lines above a slightly lower dial, separate from the right-hand windows and within the existing plate. No working filing/lever controls or correctness feedback added.
- `Tests/Level3Regression/Program.cs`: direct-raycaster seam and actual HitRect behavior (centre/edge/outside/behind/parallel/current scaled tilted translated pose/world distance), existing EventSystem/UI subclass recognition. One obsolete invisible-image wiring pin now checks direct ingress. Existing acquisition/failed-write/idempotence/source/one-reader/layout/legacy/calculator tests retained.
- `Tests/VerifyLevel3Print.py`: measures actual enlarged labels and two-line issuer/date/reference rectangles from all seven authored rows with the shipped font. Complete card rows+tab fit0.173/0.180m; no text auto-shrunk. Stand and machine geometry/labels remain native rendering acceptance pending.

### Verification actually run

- Before runtime edits, new ingress seam failed as expected:1407/1408. Runtime/full Editor compile baseline0 errors/21 and10 inherited warnings. Initial enlarged details overflowed for Island Party Shop; split into two lines. The longest purpose exceeded width at135pt;125pt fits. The larger stand initially reduced nominal scale; narrower clear stack/stand gap restores scale1 while all layout tests stay green. A test using Quaternion.Euler hit standalone Unity native-call limits; replaced with explicit managed sine/cosine matrices (runtime unchanged for that correction).
- `python3 -B Tests/run_level3_repair.py --level2`: current runtime0 errors/21 inherited warnings; whole Editor0 errors/10; Level3 **1421/1421** (revised T04 74), Controls **5734**, Level2 scratch **1580** with exactly **3 native EventSystem comparer checks skipped**. Outputs under Temp/L3Repair using discovered current Bee responses and Unity6000.4.5f1 bundled Roslyn/runtime. No tracked Tests/*/bin writes or SDK install.
- `python3 -B Tests/VerifyLevel3Print.py`: **72 PASS** shipped-font geometry, including every source's complete two-line details. Documentation `verify_pack.py`: PASS; `verify_content.py`: PASS343.
- Python diff review against pre-edit Temp snapshot. Changed project text UTF-8 CRLF. No Git, Unity/editor launch/automation, native rendered/input/phone test, actual-save, scene/prefab/package/shared L1/L2 change. T03 calculator/receipt/layer repairs untouched. The internal tests establish managed hit geometry/data/layout, not native dispatch or readability acceptance.

### USER recheck

Stop/restart Play so runtime objects/cameras rebuild; enter Bea's table from the normal T04 route (Fixture None). Tap stand card and Inspect Original: original opens, tap returns (ID flips first); tap stack repeatedly and drag originals back, preserving one reader. Settings and Leave Table must still work. Inspect larger title/amount/purpose/two-line details and machine labels at normal distance; no overlap/cropping/guide/hotbar obstruction. Repeat mouse/Remote/phone, Settings/focus/pause/leave/reopen/rotation and canceled/wobbled touch. Confirm no duplicate acquisition or input, no stuck reader/locks. Machine remains passive until T05. Native acceptance remains pending.

## 4 November 2026 — T04 buildup follow-up: meeting Bea before her table

Status: **Implemented, internally verified; user manual pending.** Bounded follow-up under the existing T04 ID. T05 machine filing and T06 checks/rewards are not started. All earlier T02/T03/T04 changes are kept. No Git commands, Unity/editor launch or automation, scene/prefab/package/asset import, or real-save access.

### Inspected before changing

- T04 handover: the introduction's completion ran `Level3Checkpointer.CommitBeaRecords` (candidate → signed write → publish), then the papers slid into their stack and Arthur said "Sit at her table". The seat box covered the whole table top up to 0.1 m above it.
- Bea: one NpcLife + NpcAnimationDriver on the shared NpcHumanoid controller, whose only states are Standing Idle, Idle Relaxed, Briefcase Idle, Walking and Strolling (no point, handover, thoughtful or glance clip; no look-at target). Dialogue emotes (thinking, idea …) and procedural conversation motion exist.
- DialogueUI runs completion callbacks only after the conversation camera's return and the control release. The Office_Level3 Interactor ray mask is layer 3 only. `HudWindowLock` → `CameraController.TryBeginProfileViewer` freezes walking/looking and the Interactor. ClientRecordPaper supplies TakeOut/PutBack/Gather and asks its holder for ReadingPose/WantsReturn.
- Review clock: `ClockDue` needs `BeaWorkIndependent` (a placed row). The buildup adds no command, so it cannot start or reset the clock.

### What the player now experiences

1. **Notice.** Bea stands behind her table. Her closed envelope lies on her side, turned toward her, and a small sketch (sheet, logo mark, pencil) lies behind the machine. The first time the player comes within 6 m before the handover she shows one thinking bubble. No marker, no forced walk.
2. **Talk.** "Hi again, or properly hello! I'm Beatriz, Bea. I design logos and menus for shops around town." / "I'm saving for a studio. Could you help me check my records?" / player: "Let's look at what each record supports." / "I kept every receipt this time. Can I count them all? I'm not sure what belongs." (thinking). Cancelling saves nothing and the next talk plays it again.
3. **Present.** On completion `CommitBeaRecords` saves first. Only then does the envelope slide and turn into the player's reach (0.35 s, one quiet `sfx_paper_place_01`), with the existing resting outline; aiming shows "Open Bea's records". A failed save shows the existing retry line, the envelope stays with her and the next talk plays only her envelope line.
4. **Open.** The player taps it: the flap lifts (0.25 s) while the nine papers slide out of the envelope (0.25 s each, 0.012 s stagger, about 0.43 s in all, one `PlayDocumentSFX`). Her logo-work original (B-REV, row R1) comes out last and lies forward of the neat pile, turned slightly toward the player. Aiming shows "Inspect a record".
5. **Inspect.** The player taps it: the existing reader lifts the real original in front of the standing player, with walking/looking held by `HudWindowLock`. Its own canonical title, amounts and reference are shown. Tapping it, tapping off it or dropping it on the table settles it back into the pile. No category, grade, highlight of a treatment or inventory item.
6. **Organizer.** About 0.35 s after it lands (if nothing else holds the player): "See my little organizer? We can use it to prepare a clear summary." (idea emote), then one card: **Help Bea** / **Later**.
7. **Enter.** Help Bea ends the conversation first, then takes the existing seat path ("Leave Table"). Once the camera has settled, R1's card slides onto the stand and Arthur's existing seated lesson starts. Later: "Take your time. I'll be right here." (no penalty). After that the table seat sits directly, and talking to her opens "Ready to sort them? Ask me anything first if you like." with Help Bea first, then her questions (plus the new optional "What are you working on?").

Within a session, leaving and returning keeps the current step. A reload, legacy draft, collected worksheet or any restore with her records handed over skips straight to that concise continuation (envelope open, stack out, card on the stand when seated).

### Changes (existing owners; no new framework)

- `Assets/Scripts/Level3/Level3TableStaging.cs`: pure `EncounterStep`/`Encounter` (forward-only order, this session only; `Sync` from the saved acquisition), `EnvelopeWithBea`, `SketchArea`, `FeaturedSource`.
- `Assets/Scripts/Level3/Level3BeaTable.cs`: the envelope's two poses; `Present`, `OpenEnvelope`, `InspectFeatured`/`EndStandingRead`, `ApplyEncounterPose`, `PresentMotion`, `Reveal`, `SettleMotion`; her sketch; the envelope/record step targets. `UpdateEncounter` keeps exactly one live table target (the still-solid seat box moves between the Interactable and Default layers), shows the one emote, handles tap-off return and times her offer. `TrySit` waits for her offer and presents the card after the camera settles. The reader callbacks feed the encounter; `SyncPapers()` is the quiet restore. The built-in Bea mirrors the JSON.
- `Assets/Scripts/Level3/Level3BeaInteractable.cs`: the envelope-line retry; HandOver presents the envelope only after `Attached` (Arthur's line removed); `OfferHelp`/`ShowOffer`/`ChooseHelp`/`Later`/`Concluded`; the continuation menu; a `ShowPanel` option without the extra Talk later.
- `Assets/Scripts/Level3/Level3BeaTableSeat.cs`: `Level3BeaTableTarget` added to this existing file (no new script/.meta).
- `Assets/Scripts/Level3/Level3TownContent.cs` and `Assets/Resources/Levels/Level_03/town_life.json`: `organizerIntro`, `helpChoice`, `laterChoice`, `continuePrompt` (validated), the revised introduction and one optional work question.
- `Assets/Scripts/Level3/Level3Checkpointer.cs`: read-only guide flag `beaIntroDone`.
- `Assets/Resources/Tutorials/Level_03/guide.json`: `bea-records` now waits for `flag:beaIntroDone` while standing (1 s delay; "Bea's records are in your Case Folder too. Ask her about anything odd."); `worksheet-intro` no longer waits for it. Arthur says nothing during the buildup. Objectives unchanged.
- `Assets/Scripts/Level3/Level3EditorFixtures.cs`, `Assets/Editor/Level3EditorFixtureMenu.cs`: Editor-only fixtures **Bea Encounter** (assigned, envelope not handed over) and **Bea Table Restored** (handed over).
- `Tests/Level3Regression/Program.cs`: 10 T04 pins updated for the new copy, guide trigger and method names; a new 49-check buildup block.
- Documentation: T04/T05 tickets, this entry, KNOWN_ISSUES L3-R51–L3-R57, README/AGENTS/TICKETS pointers.

Reused unchanged: DialogueUI/DialogueCameraDirector, CameraController/HudWindowLock, Interactor/HighlightEffect, ClientRecordPaper, NpcLife/NpcAnimationDriver, WorldEmote, AudioManager cues, Level3DeskProps, the checkpointer's commands and Levels 1–2 code. New local parts are only the envelope flap's animated opening (existing hinge), the sketch primitives and two collider targets. No new asset, GUID, scene or prefab edit.

### Commands and results actually run

- Baseline before edits, `python3 -B Tests/run_level3_repair.py --level2`: runtime 0 errors/21 warnings, Editor 0/10, Level3Regression 1421/1421, Controls 5734, Level2 scratch 1580 (3 native skips).
- First run after the edits: exactly the 10 expected T04 pins failed (copy, guide trigger, renamed calls) and were updated to the new contract; one CS0136 name clash in the new test code was fixed. Mutation check: weakening `Encounter.Returned` (no shown record needed) and `Accept` (before her offer) produced 6 buildup failures; the source was restored byte-identical (`cmp`).
- Design correction found during review: switching the seat box off would leave the stand-in table (used when the prefab is missing) without a solid collider, so the box now changes layer instead.
- Final `python3 -B Tests/run_level3_repair.py --level2`: runtime **0 errors/21 inherited warnings**, entire Editor **0 errors/10 inherited** (none in edited files); Level3Regression **1470/1470** (buildup 49, revised T04 74); Controls **5734**; Level2 scratch **1580** with the same **3 native skips**. The signed-store sandbox under `Temp/L3Repair` is deleted after the run.
- `python3 -B Tests/VerifyLevel3Print.py` **72 PASS**; `python3 -B Tests/VerifyLevel3Scene.py` **PASS** (3 inherited warnings; scene sha256 3e8d29dfd93c… unchanged); `python3 -B Tests/VerifyGuideTargets.py` 14 paths, 0 missing.
- `python3 -B Documentation/Level3TheNumbersTellAStory/verify_pack.py` **PASS** (18 tickets: 4 implemented/internal-verified/manual-pending, 14 Planned; 239 local links; 122 source references; 13 arithmetic fixtures); `verify_content.py` **PASS 343**.
- Edited files keep their own line endings (checked by script). The buildup checks cover order, restores/legacy, the signed-store failure/retry/duplicate path, no-save/no-grade/no-clock guarantees, the record/layout maths, copy budgets, guide/T05 seam and wiring. They do not run Unity rendering, NPC motion, touch, audio or timing.

### USER manual script (Unity/device; not run here)

Choose Tools > TaxVenture > Level 3 > **Fixture: Bea Encounter**, then Play Office_Level3 directly (a direct attempt never saves).

1. Walk to Bea's table from the office doors: sketch and closed envelope on her side, one thinking bubble, route clear, nothing flashy.
2. Talk; cancel once mid-way (the intro replays next time); then let it finish. Each turn reads in one glance.
3. Watch the envelope slide and turn into reach with its outline and quiet cue; aim at it: "Open Bea's records".
4. Tap: flap, stack, the logo-work record forward; nothing clips through the envelope, stand or machine. Aim: "Inspect a record".
5. Tap: the record rises and walking/looking stop. Read its title and amounts. Return it by tapping it, by tapping off it and by dragging it onto the table (repeat via Later/re-entry if needed). Open Settings while it is up, close it, then return the paper. Try focus loss.
6. Bea's organizer line and the Help Bea / Later card. Choose Later, walk away and back, talk: continuation line with Help Bea first; or tap the table to sit.
7. Help Bea: the conversation closes, one seat move, the card slides onto the stand after the camera settles, then the table hint. Leave Table, then sit again (direct, nothing replays).
8. **Fixture: Bea Table Restored**: the envelope is already open with the stack out, Bea offers Help Bea at once, no slide or reveal replays.
9. **Fixture: None** from reception: objective 2 ticks only after the saved handover; coins, Trust and Bea's clock do not change during the buildup.

Time the first encounter (target about 60–90 s) and check sound on/off, mouse, Unity Remote and phone, prompt aiming, readability and clipping. Leave the T04 manual boxes unchecked until this evidence exists.

### Next step

User manual acceptance of T04 including this buildup, then **T05** from the seated workspace (T05 "Entry from the T04 buildup").
