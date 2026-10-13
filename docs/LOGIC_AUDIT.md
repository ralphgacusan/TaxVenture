# TaxVenture: logic and configuration audit

Review date: 12 October 2026. Read alongside [the actual game flow](C:/Learn/Projects/TaxVenture/TaxVenture/docs/GAME_FLOW_CURRENT.md).

## How to read this document

- **Confirmed defect:** a contradiction, broken connection, or erroneous operation visible in source/configuration.
- **Confirmed behavior / design decision:** the code is clear, but whether it is wrong depends on the intended game rules.
- **Risk requiring runtime verification:** a plausible failure whose occurrence depends on execution order, UI interaction, or scene conditions.

Priority indicates player impact, not permission to implement a fix. **No fixes were made.** This review is against the current working tree, including existing package changes, rather than only committed history. There is no separate authoritative design specification in the repository; README and code comments disagree with the implementation in several places.

## A. Confirmed defects and configuration problems

### A01 — Two active game-state machines in the Level 1 scene

**Priority: High.** Office_Level1 contains an enabled GameStateMachine on the Controller/GameStateMachine object and another on the Receptionist. Both objects and their inspected parent chains are active. Each Awake unconditionally assigns the same static Instance.

**Consequence:** there is no single reliable state owner. Components that retain/subcribe to a particular machine can disagree with callers using the final static Instance. The machine on the receptionist is not an NPC FSM; it is a second global game FSM.

Evidence: [first component](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scenes/Office_Level1.unity:4736), [second component](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scenes/Office_Level1.unity:80386), [unconditional Instance assignment](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/FSM/GameStateMachine.cs:48).

**Related startup risk:** GameStateMachine.Start sets Idle while CaseProgressionManager.Start loads a case and requests ReceiveCase. No explicit ordering was found for these scripts. A later Idle initialization can overwrite the already-started case phase. Exact observed order requires a Unity run.

### A02 — Tutorial data is empty

**Priority: High.** The configured `level1_tutorial.json` is zero bytes. The controller rejects whitespace/empty content and stops before showing steps. Office_Level1 enables automatic tutorial startup.

**Consequence:** the JSON-driven tutorial does not teach or guide the player. This is separate from Arthur's side-quest dialogue, which has its own implementation.

Evidence: [empty tutorial file](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Resources/Tutorials/level1_tutorial.json), [empty-content rejection](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/UI/Tutorial/TutorialController.cs:203), [scene configuration](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scenes/Office_Level1.unity:93470).

### A03 — Supporting-document page is unreachable from the current folder

**Priority: High for document-review gameplay.** CaseFolderUI.BuildPages creates one page. RenderPage handles index 0. SupportingDocumentsPageLink expects supporting documents on index 5.

**Consequence:** navigating the current folder cannot expose its intended supporting-document page or its document-opening buttons. Existing later-page builder functions do not make those pages available.

This does **not** prove the three-fact case is impossible to finish: the tax book has valid classification chips and can supply the required answers. It does mean the advertised document-review route is disconnected.

Evidence: [one-page builder](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/UI/CaseFolderUI.cs:350), [document-page condition](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/UI/SupportingDocumentsPageLink.cs:32), [valid tax-book values](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Data/TaxCodeBookData_Main.asset).

### A04 — The same submission starts the audit twice

**Priority: Medium.** AuditorSubmissionTray calls `auditor.BeginFinalAudit()` twice consecutively.

**Consequence:** the audit state and dialogue startup run twice. DialogueUI replaces the same queue/callback, so this is not necessarily two visible full conversations. It does duplicate entry work and first-line effects. Do not infer double rewards from this alone: the successful summary callback checks the current state before awarding.

Evidence: [duplicate calls](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/AuditorSubmissionTray.cs:244), [dialogue startup](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/UI/DialogueUI.cs:164).

### A05 — Displayed verdict correctness does not grade the player's verdict

**Priority: High if assessment/stamping is meant to be graded.** ComplianceChecker checks three fact values. The auditor sets `VerdictWasCorrect` and `finalVerdictWasCorrect` from whether that issue list is empty. It never compares the player's caseAssessment with answerKey.correctAssessment. CaseVerdictEvaluator separately calls a case Ready whenever the three values are present, even if they are incorrect.

**Example:** enter the three correct answers, leave the assessment at its initial NotReady value, then submit. The audit treats the work as successful and the reward branch is perfect. Conversely, filled but incorrect facts produce a reported correct assessment of Ready while the audit rejects them.

**Consequence:** the fields labeled verdict correctness, actual classification correctness, and case readiness represent different things but are presented as one result.

Evidence: [audit evaluation](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Interactables/AuditorInteractable.cs:89), [presence-only verdict](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/CaseVerdictEvaluator.cs:19), [three-field grader](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/ComplianceChecker.cs:50).

### A06 — Auditor asks for a return that the tray does not accept or require

**Priority: High for return-filing gameplay.** The Ready-case reminder requests both the folder and tax return. The tray accepts only Submit_CaseFolder and begins auditing as soon as it gets that folder. The active Level 1 return HUD icon has no submission target.

**Consequence:** a folder alone satisfies the actual submission rule. Returning a printed form is not a graded requirement, and the advertised return-submission route is not wired.

Evidence: [reminder](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Interactables/AuditorInteractable.cs:58), [accepted key](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/AuditorSubmissionTray.cs:77), [return icon](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scenes/Office_Level1.unity:84116).

### A07 — Retry preserves incompatible old state

**Priority: Medium now; High as the full workflow is enabled.** ResetForRetry clears the checked classifications, stamp/assessment, encoded form, and audit bookkeeping. It does not clear filingStatus, requiredForm, numerical calculations/computationStatus, or printed-return flags. Transferred-field metadata and document-review flags also survive.

**Example:** stamp Ready, prepare/print a return, fail, then retry. The answers/stamp are reset but the case can still say Ready and carry old return/computation state.

**Consequence:** a retry is partly fresh and partly the old attempt. Some omitted fields are outside today's three-fact grading, so their consequences are mainly in the broader workflow.

Evidence: [reset implementation](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/CaseData.cs:130), [retry caller](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Interactables/AuditorInteractable.cs:187).

### A08 — Restamping NotReady leaves a prior Ready filing status

**Priority: Medium.** The Ready stamp sets both caseAssessment and filingStatus. The NotReady branch changes caseAssessment only.

**Consequence:** the same case can say NotReadyForFiling in its assessment and ReadyForFiling in its filing status. The auditor reminder and other filing-status consumers continue treating it as Ready.

Evidence: [Ready and NotReady branches](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/UI/StampUI.cs:128).

### A09 — Client interview cannot be replayed after a retry

**Priority: Medium.** ClientInteractable sets a component-level hasInterviewed flag and returns immediately on later interactions. Neither the retry reset nor case-loading manager resets it.

**Consequence:** after an audit clears the answers, the player cannot repeat that NPC's explanation. Reusing that client component for another case also retains this flag. This removes a hint route; it does not prove the tax-book route is blocked.

Evidence: [interview guard and completion](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Interactables/ClientInteractable.cs:40), [case reset](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/CaseData.cs:130).

### A10 — Result timer stops at the first submission, including a failed one

**Priority: Medium.** LevelTimer sets startTime and endTime only while each is negative. It has no retry or next-case reset. The level result reads that timer.

**Example:** submit incorrectly after two minutes; succeed ten minutes later. Once the initial start was captured, the level result still uses time through the first submission, not the eventual finish.

**Consequence:** displayed completion time can omit retries and later cases and differ from the continuously running HUD. The timer can also miss the initial ReceiveCase event because it subscribes in Start; that is an additional startup-order risk.

Evidence: [latched timestamps](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/LevelTimer.cs:39), [level result time](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/RewardsSystem/LevelTotalResult.cs:23), [separate HUD timer](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/UI/HUD/HudTimer.cs:18).

### A11 — Task list reports entering a phase as completing its work

**Priority: Medium.** NotesPanelUI marks the current state's type complete as soon as it sees it. It does not wait for an action-completed event. Completion is held in a static set, and no caller of ResetCompletedTasks was found in scripts or serialized scene/prefab callbacks.

**Examples:** entering ReviewDocuments can mark document review DONE before the documents were read. Previously completed tasks can remain checked on retry or scene replay in the same process.

The task list also still requires computation, return preparation, client outcome, and archive steps which the current audit route does not require. Its main objective mentions a tutorial and an unguided case even though Level 1 lists only one case.

Evidence: [marking logic](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/UI/NotesPanelUI.cs:303), [unused reset method](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/UI/NotesPanelUI.cs:568), [task definitions](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/TaskListProvider.cs).

### A12 — Level 2 has multiple independent routing defects

**Priority: High for progression beyond Level 1.**

1. ProgressUI's configured Level 2 button has an empty persistent click list, and the controller does not add its loading listener in code.
2. The scene's levelTwoCutscene value is `Level2_Character_Intro_Cutscene`, which was not found among the project's scenes.
3. Office_Level2 is not enabled in build settings.
4. CaseProgressionManager.Start always loads Level_01, even if the scene's CaseManager Inspector value says Level_02; that Inspector field does not control the manager's startup call.
5. Level_02/level.json declares levelId Level_01 and calls itself a three-case level while listing two IDs.

**Consequence:** unlocking the button does not make Level 2 playable. Correcting only one of these connections would leave the others.

Evidence: [button and scene configuration](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scenes/Progress.unity:3805), [hardcoded startup](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/CaseProgressionManager.cs:73), [Level 2 manifest](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Resources/Levels/Level_02/level.json), [build list](C:/Learn/Projects/TaxVenture/TaxVenture/ProjectSettings/EditorBuildSettings.asset).

### A13 — Case JSON has values outside the runtime enum vocabulary

**Priority: High before using Level 2; Medium for currently unused Level 1 fields.**

| Content | Incompatible values | Effect |
|---|---|---|
| Level_01 case_001 | One, NotApplicable, GraduatedIncomeTax | Do not match EmployerCount, BusinessRegistration, TaxOption; not currently among the three graded facts |
| Level_02 case_001 | Individual, CompensationIncome | Do not match TaxpayerType or IncomeSource; the strict grader rejects these answer keys if this case is loaded |
| Level_02 case_002 | NoEmployer, InformalEarner, RegularIncomeTax | Do not match the current employer/business/tax-option enums |

The current Level 1 three graded answer-key values do match their enums. Do not mistake the unused-field inconsistencies for an automatic Level 1 audit failure.

JsonCaseLoader validates basic structure and document field types, but not these answer-key enums, matching level IDs, numeric answers, or correct-assessment consistency. Invalid domain content can therefore pass loading and fail later.

Evidence: [runtime enums](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/CaseData.cs:162), [Level 1 case](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Resources/Levels/Level_01/cases/case_001.json:36), [Level 2 first case](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Resources/Levels/Level_02/cases/case_001.json:36), [loader validation](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/Loading/JsonCaseLoader.cs:199).

### A14 — A document's residency source has the wrong transfer type

**Priority: Medium for document-transfer gameplay.** Marie's Employment Records defines the ResidencyStatus field as Text. The folder destination requires Enum, and acceptance checks both type and semantic key.

**Consequence:** even after restoring the document page, that document value cannot be dropped directly into the residency destination. The book's Enum chips are a valid alternative; its ResidencyStatus values were checked against the enum.

Evidence: [document field](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Resources/Levels/Level_01/cases/case_001.json:235), [destination acceptance](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/DragDrop/CaseFolderFieldRow.cs:711), [book data](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Data/TaxCodeBookData_Main.asset).

### A15 — Authored numerical answers and calculator behavior disagree

**Priority: High before computation is graded.** The calculator uses max(0, gross − expenses), then 8% or a flat 20%, then max(0, tax due − credits).

For Marie's authored 480,000 income, zero expenses, and 32,500 credits, the flat 20% calculation gives 96,000 due and 63,500 payable. The case answer key says 32,500 due and zero payable. Additionally, her authored tax-option string is not a valid runtime enum.

**Consequence:** the calculator and case answer sheet cannot both define the expected numerical result. Today's three-fact audit hides this disagreement because it never checks the amounts.

This is an internal consistency finding, not a determination of legally correct tax liability. Current tax-law accuracy was not evaluated in this code review.

Evidence: [calculator](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/TaxComputationCalculator.cs:26), [authored amounts](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Resources/Levels/Level_01/cases/case_001.json:54).

### A16 — The current folder/book layout cannot supply the calculator workflow

**Priority: High if computation is part of Level 1.** ComputerUI requires transferred Number values for GrossIncome, AllowableExpenses, and TaxWithheldOrCredits. The reachable folder page supplies name/TIN and three enum fields; the current book supplies classification enums. The supporting-document/numerical folder pages that would supply the normal numerical path are unreachable.

Calculation also leaves its computed results in output chips for separate transfer, while the folder page intended to receive them is absent from BuildPages.

**Consequence:** the normal folder/book investigation route cannot complete the calculator-and-record-results workflow. It can still complete the three-fact audit, which does not require computation. If the layout is intentionally restricted for the tutorial, the calculator should be treated as outside that lesson rather than advertised as a required task.

Evidence: [input readiness and output handling](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/UI/Computer/ComputerUI.cs:60), [one-page folder](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/UI/CaseFolderUI.cs:350), [current book](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Data/TaxCodeBookData_Main.asset).

## B. Confirmed behavior that needs a design decision

### B01 — Successful audit skips client outcome and archive

The auditor goes directly to CaseCompleteState after storing the reward. No normal `new CaseOutcomeState()` transition was found in the scripts. The client outcome and file-cabinet paths exist but are disconnected from successful audit completion.

**Decision:** is this an intentional Level 1 shortcut, or must every case end with findings presentation and archiving? The README, task list, and several comments still describe the longer route.

Evidence: [success routing](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Interactables/AuditorInteractable.cs:155), [client outcome handler](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Interactables/ClientInteractable.cs:74), [archive requirement](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Interactables/FileCabinetInteractable.cs:84).

### B02 — Unlimited retries still earn perfect rewards

Failures return to investigation without a persistent attempt penalty. Only successful zero-issue submissions reach reward calculation, so normal completed cases get 100 EXP and 5 reputation regardless of previous failures. The other three reward tiers are not reached through the normal completion path.

**Decision:** mastery practice with unlimited full-credit retries, or scored performance with lasting mistakes/non-perfect outcomes?

Evidence: [retry/success branches](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Interactables/AuditorInteractable.cs:153), [reward table](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/RewardsCalculator.cs:18).

### B03 — Side-quest reward is displayed but never awarded permanently

GrantRewards logs the amounts; the completion UI displays them. Neither updates PlayerProgressManager. Default/configured reward amounts are 50 EXP and 10 reputation.

**Decision:** should this activity award persistent currency/progression, and is that once per profile activity, once per level, or every replay?

Evidence: [reward stub](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/SideQuest1/CorkboardSideQuestManager.cs:173), [result display](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/SideQuest1/SideQuestCompleteUI.cs:42).

### B04 — Town activity does not gate office case startup

The case manager starts Level 1 automatically. TownToOfficeTransition only activates an optional indicator on completion. There is no implemented prerequisite connecting its completion to StartLevel.

**Decision:** an optional activity in the same world, or a mandatory introduction before the office case?

Evidence: [automatic case startup](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/CaseProgressionManager.cs:73), [transition placeholder](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/SideQuest1/TownToOfficeTransition.cs:25).

### B05 — There is one Level 1 case, not a guided-plus-unguided sequence

The manifest lists only case_001. A case_002 file is present, but files are not played unless the manifest lists them. The final-case route therefore runs after the first accepted case.

**Decision:** one tutorial case or two sequential cases? Reconcile the notes objective with the manifest.

Evidence: [Level 1 manifest](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Resources/Levels/Level_01/level.json), [objective text](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/TaskListProvider.cs:18).

### B06 — Progress is saved only when leaving the level result screen

The result-screen return button calls ApplyLevelResult, which updates totals and writes the save. Case success and showing the result panel do not themselves commit progression. In-progress answers and side-quest state are never saved.

**Decision:** acceptable restart-on-exit behavior, or automatic checkpoint/save-on-completion?

Evidence: [return action](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/UI/LevelCompleteUI.cs:140), [save schema](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/RewardsSystem/PlayerProgressData.cs).

### B07 — “Levels completed” counts runs, not unique levels

Each ApplyLevelResult increments a number; no completed level ID is stored. Replaying Level 1 adds rewards and increments levelsCompleted again. The Level 2 unlock only needs that count to reach one.

**Decision:** distinguish replay rewards from unique progression? With more levels, a single count is insufficient to establish which specific prerequisites were completed.

Evidence: [progress application](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/RewardsSystem/PlayerProgressManager.cs:69), [unlock check](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/UI/ProgressUI.cs:65).

### B08 — Reopening a folder rewinds the phase label

Physical CaseFolderInteractable requests ReviewDocumentsState every time it opens, regardless of whether the player was researching, stamping, or preparing a return. The FSM lock blocks that transition after final submission, but before submission the earlier phase is requested again.

**Decision:** does the FSM represent the currently viewed activity or monotonic case progress? Reopening documents is normal player behavior; it should not accidentally erase a required phase or make task completion misleading.

Evidence: [unconditional state request](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Interactables/CaseFolderInteractable.cs:98).

### B09 — Forms accept broadly typed values without grading their meaning

TaxReturnFieldSlot accepts Number for numeric slots and Text/Enum for nonnumeric slots, rather than matching semantic keys. The confirmation UI checks that slots are filled, not that they are correct. The current audit never checks the encoded form.

**Decision:** should the UI reject a mismatched value immediately, or allow mistakes for the auditor to grade? Today neither semantic validation nor final form grading catches them. A fixed submission date is also written by PrintJobController rather than read from the case or a game calendar.

Evidence: [slot acceptance](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Data/DragDrop/TaxReturnFieldSlot.cs:35), [confirmation](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/UI/Computer/BirFormEncodingUI.cs:96), [fixed print metadata](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Interactables/PrintJobController.cs:40).

## C. Risks and gaps requiring targeted verification

1. **Exactly-once rewards are not enforced by the reward APIs.** AddCaseResult always appends; ApplyLevelResult always adds totals. Current audit state guards reduce repeated callbacks, so duplicated rewards were not demonstrated. Reconnecting the client-outcome path without reconciling its second reward call could duplicate results. Sources: [accumulator](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/RewardsSystem/LevelRewardAccumulator.cs:15), [outcome reward call](C:/Learn/Projects/TaxVenture/TaxVenture/Assets/Scripts/Interactables/ClientInteractable.cs:113).
2. **FSM event subscriptions depend on initialization timing.** Some listeners subscribe in OnEnable or Start and have no retry/current-state synchronization. Combined with duplicate FSMs, HUD/task/timer events may be missed or attached to a different instance. Inspect a live run before claiming a particular observed failure.
3. **A locked FSM is not an immutable submission.** ChangeState rejects certain transitions, but writable CaseData and value destinations do not share a universal submission lock. Determine whether open UI/raycast access permits editing while audit dialogue or results are shown.
4. **Modal controls need playtesting.** Dialogue/tutorial/book locking is not reference-counted. Opening/closing overlapping panels could unlock controls while another panel still expects them locked. Disabling PlayerMovement also does not explicitly zero Rigidbody velocity; actual drift depends on physics settings.
5. **Mobile inputs mix pointer, mouse, touch, and center-ray paths.** Both input backends are enabled. This is not inherently wrong, but UI touch blocking, dragging between world/canvas/HUD, and multiple simultaneous fingers need Android testing.
6. **Fallback cases are not proof of recoverable gameplay.** A malformed case can become a placeholder; empty answer data cannot pass the strict audit. Loader error handling prevents some exceptions but does not implement a player-facing recovery/menu path.
7. **Save failure and corruption recovery are limited.** Save writes directly to the destination file; there is no backup/atomic replacement or user-visible failed-save recovery. Loading errors start a fresh in-memory record. No device storage-failure tests were run.
8. **No authored automated test suite was found.** The Unity test package exists, but searches found no NUnit/UnityTest gameplay tests under Assets. No build or device validation was performed for this documentation task.

## D. Coverage and maintenance context

The investigation covered menu/build routing, scene initialization, both FSMs, the current JSON cases and loader, client dialogue, tutorial, side quest, case folder/document paths, book/value transfer, calculator/forms/printing, stamp/submission/audit, retry, outcome/archive, reward accumulation, progression saves, timers/tasks, camera/input and settings/audio connections. It included targeted GUID-to-script and Inspector-reference checks in the active scenes and relevant prefabs.

It is not an exhaustive visual inspection of every imported asset, shader, vendor script, or every serialized reference in every prototype scene. Binary artwork, device performance, and legal correctness of educational tax content were outside the static logic review. A full playthrough is still needed to prove the assembled game works on a target device.

Repository issues that affect maintenance but are not direct gameplay defects:

- README milestones, controls, save-system claims, and several comments are outdated.
- Legacy and current UI paths coexist; edits must identify which scene/prefab references the component before assuming it is used.
- Generated Android/build/debug output is tracked, producing a very large unrelated Git change surface.
- Existing Packages/manifest.json and packages-lock.json modifications were present before this documentation work and were left untouched.

## E. Verification scenarios for a later implementation/playtest pass

| Scenario | What to establish |
|---|---|
| Fresh launch through MainMenu | Exactly one FSM owns state; case starts in the intended phase |
| Tutorial startup | Popups load and each required trigger advances once |
| Town activity then office | Intended gate, profile count, matching, and persistent reward agree |
| Read documents and transfer facts | Document page is reachable; correct typed values can be placed |
| Correct facts with wrong/no stamp | Outcome follows the intended assessment rule |
| Ready case with folder only | Return requirement is enforced if intended |
| Wrong answer, retry, then succeed | Interview, folder, flags, notes, timing, and penalties reset as designed |
| Success after multiple failures | Reward and displayed timing reflect the chosen retry policy |
| Client presentation and archive | Required steps are reachable and award only once |
| Two cases in one level | Correct client/content, fresh state, per-case and total timing, correct summed rewards |
| Return from level result | Save happens once; restarting restores the same totals |
| Replay a completed level | Replay rewards and unique unlock progression follow separate intended rules |
| Unlock and press Level 2 | Button callback, scene, level ID, and case content all agree |
| Android touch with overlapping UI | No accidental world interaction, stuck controls, or incorrect drag acceptance |
| Quit before/after completion | Retained and lost progress match the agreed save policy |

Do not implement repairs until the intended flow and reward policy are agreed. Several differences may be deliberate Level 1 simplifications; preserving those decisions matters more than restoring every older subsystem automatically.
