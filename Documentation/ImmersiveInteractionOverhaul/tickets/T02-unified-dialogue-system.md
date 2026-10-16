# T02 - Unified conversation lifecycle

Status: **PHASE 1 AND PHASE 2 IMPLEMENTED — internal checks passed; user Unity acceptance pending.**
Dependencies: T01 controls and the existing DialogueUI and CameraController APIs. Next: T03 after user acceptance of this ticket. Original estimate: **10k-18k tokens**; both phases are now code-complete.

## Approved phased delivery

User authorized this ticket and permits phases when needed; user performs manual Unity tests and Git writes. Keep current first-person/T01 behavior.

1. **Phase 1 — shared lifecycle and office callers (implemented):** pure production DialogueSession admission/sequence/terminal claims; DialogueUI global busy rejection, validated start, owner-scoped cancellation/disable cleanup, line/start/end hooks and same-frame advance guard; conversation-only control snapshot in existing CameraController. Office receptionist/colleague/client/auditor starts admit before progress changes. Empty content cannot succeed, repeated presentation cannot reward again, duplicate audit start is blocked. Pending audit can resume from the auditor after rejected admission/cancel because the tray already consumed its submission.
2. **Phase 2 — town/tutorial completion and compatibility (implemented):** the taxpayer and guide callers no longer use the legacy wrapper bridge. Both now admit through TryStartDialogue before any state change, cancel owner-scoped, and protect their one-shot outcomes. Legacy InterviewClientUI is untouched and verified to have no live opener.

## Phase 2 changes

**`Assets/Scripts/SideQuest1/TaxpayerNpcInteractable.cs`**
- Admission happens before the Interact/Dialogue state changes, so a rejected start no longer emits a spurious NPC state-change pair. The old code mutated the FSM first and rolled it back on failure.
- Added `dialogueSessionActive`; `OnDisable` cancels only a session this NPC actually owns, matching the office callers.
- Prior NPC state is captured per start and restored on cancellation, replacing the single `wasWaitingBeforeDialogue` flag.
- **Soft-lock fix.** This conversation's only successful outcome is its profile drop, and completion is one-shot and gates the four-profile collection. The start is now refused, with an explicit error, when the pickup is unassigned or `npcId` resolves to no `TaxpayerProfile` or to no authored dialogue. Previously an unknown `npcId` still produced a valid one-line placeholder conversation ("..."), which completed, set Completed, logged a failed drop, and left `OnAllProfilesCollected` unreachable — no tutorial guide, no side quest. An unassigned pickup did the same from a warning only.
- On completion the profile drops **before** Completed is set; if the drop fails the NPC returns to its prior state and stays available rather than retiring with nothing to collect.
- Added read-only `HasBeenTalkedTo` and `IsInConversation` for T04 markers.

**`Assets/Scripts/SideQuest1/TutorialNpcController.cs`**
- Both conversations use `TryStartDialogue` and track `dialogueSessionActive`; the intro's return value is no longer discarded.
- **Soft-lock fix.** `BeginSideQuest` no longer retires the guide unconditionally. `CorkboardSideQuestManager.BeginSideQuest` refuses to open without its camera, viewpoint, four slots, card prefab, tray and four collected profiles; the old code set `hasStartedSideQuest = true` before calling it, so a refused board left the guide permanently unclickable and the side quest unreachable. The guide is now retired only when `CanInteract` confirms the board is running, or when it has already been completed, and warns otherwise.
- The intro is refused outright when `corkboardSideQuest` is unassigned, so the board cannot be skipped by a completed intro.
- Final completion is once-only via `hasCompletedSideQuest`/`hasShownCompletionPanel`, so a repeated `OnSideQuestCompleted` cannot replay the result panel. `CorkboardSideQuestManager` already guards the raise with `completionEventRaised`; this is defence in depth, not a reported defect.
- The interrupted-final-guide retry from Phase 1 is retained: cancellation re-arms `pendingCompletionDialogue` unless the panel already showed.
- A missing `SideQuestCompleteUI`/manager now warns instead of silently skipping the result popup.
- Added read-only `HasAppeared`, `IsInConversation` and `HasStartedSideQuest` for T04.

**`Assets/Scripts/SideQuest1/TownNpcDialogueLibrary.cs`** (+21 lines, additive)
- New `GetDialogueFor(npcId, displayName, out bool recognized)` overload and `HasDialogueFor(npcId)`. The existing single-argument signature and the placeholder fallback are unchanged, so no other caller is affected; a repository grep confirms `TaxpayerNpcInteractable` is the only live caller.

## Verified scene and content wiring

All four taxpayer NPCs in `Office_Level1.unity` are correctly bound and their ids match both the dialogue library and `TaxpayerProfileDatabase`: `npc_marie`, `npc_jhunjhun`, `npc_milo`, `npc_zorbix`. Each has a distinct `profilePickup`; all four and the guide share the one `DialogueUI` (fileID 1416291532), which sits on its own `panelRoot`. The guide's `visualRoot`, `dialogueUI` and `corkboardSideQuest` are all assigned.

Legacy `InterviewClientUI` is preserved untouched. One serialized instance remains with all its bindings, and its only two UnityEvent references are its own in-panel buttons calling `OnPresentationContinuePressed` and `OnFinishInterviewPressed` — not openers. No C# calls `Show()` or `ShowPresentation()`; `ClientInteractable` uses the shared `DialogueUI`. No adapter is justified.

## Internal evidence

- **PASS: 311 production contract assertions** (`Tests/DialogueRegression`, was 52). The project now also links production `DialogueBuilder`, `TownNpcDialogueLibrary` and `TaxpayerProfileData`. New cases: every taxpayer id is recognized, two-sided, correctly attributed, admitted by a session, and advances to exactly one completion claim that cannot be claimed twice; each id resolves to a profile that round-trips by profile id, with four distinct ids and four distinct classifications; unknown, empty, whitespace and wrong-case ids report unrecognized while their placeholder still admits, proving the `recognized` flag is the only guard; both guide conversations keep their authored line counts and run their success outcome exactly once, with cancellation never running it.
- **PASS: runtime C# compilation** of the three changed scripts against the Unity 6000.4.5f1 module assemblies and the project's current `Library/ScriptAssemblies/Assembly-CSharp.dll`. Only CS0649 `[SerializeField]` warnings, which Unity suppresses. That assembly was built after the Phase 1 sources, so Unity has already compiled Phase 1 successfully.
- **PASS: scoped diff check.** Only the three scripts and the two test files changed; the library diff is +21/-0 with no line-ending noise.

**NOT RUN by agent:** actual Unity Play-mode behavior, UI activation and native control restoration, real NPC outcomes, profile pickup/collection, board opening, Android and device testing. These are user-run; compilation and assertions are not gameplay acceptance. Phase 2's refuse-on-misconfiguration paths cannot trigger with the current correct scene bindings, so their error branches are reasoned and unit-covered, not observed in Unity.

## Manual acceptance matrix (user)

Phase 1 (if not already done): Play `Office_Level1`; talk to receptionist, colleague and client and finish normally; verify one active conversation and MOVE/swipe restoration; check a normal audit and no repeated rewards. Optional cancel test: during Play deactivate the dialogue panel GameObject (not its parent Canvas), then retry that NPC; the audit prompt should allow resuming without resubmitting the consumed folder.

Phase 2:
- Talk to **all four** taxpayers. Each completes, drops exactly one paper, and the NPC becomes non-clickable afterwards.
- Collect all four; the HUD reaches 4/4 and the tutorial guide appears.
- Talk to the guide; the corkboard opens after the **last** line, not before.
- Cancel the guide intro (deactivate the dialogue panel mid-conversation): the board must not open and the guide must stay clickable.
- Solve the board; the final guide conversation plays once and the result panel shows once.
- Cancel the final conversation; it must become available again and still reach the result panel.
- Click a taxpayer during another conversation: nothing should happen and the first conversation must be unaffected.
- After each conversation, confirm MOVE and swipe-look work again.

Report exact Console errors and the failing route. No setup menu or scene changes are required.

## Objective and inspected entry points

Give relevant NPCs one safe start, advance, end and cancel lifecycle while retaining their individual content, conditions and successful outcomes. Reuse the existing DialogueUI and CameraController APIs; improve the shared presentation surface rather than add a competing dialogue manager or a broader camera/focus system. This ticket may own and release the locks needed for its conversation lifecycle.

Primary files: Assets/Scripts/UI/DialogueUI.cs, DialogueSession.cs, InterviewClientUI.cs; Assets/Scripts/Data/DialogueLine.cs, DialogueBuilder.cs, CaseFactory.cs, case definitions and content; Assets/Scripts/Interactables/ReceptionistInteractable.cs, ColleagueInteractable.cs, ClientInteractable.cs, AuditorInteractable.cs; Assets/Scripts/SideQuest1/TaxpayerNpcInteractable.cs, TutorialNpcController.cs, TownNpcDialogueLibrary.cs, TownEvents.cs; NPC FSM scripts.

## Acceptance criteria

- [x] Relevant indoor and outdoor NPCs and all four taxpayer targets use the shared lifecycle. *(code-complete; user gameplay acceptance pending)*
- [x] Start, advance, skip hook, end and cancel input routes are consistent, with no duplicate simultaneous conversation. *(internal; native overlay behavior user-tested)*
- [x] Empty, invalid, busy and cancelled starts leave no conversation-owned locks or lost prior callback and cause no unintended progress.
- [x] Successful outcomes fire once; existing interview, profile, objective, tutorial, reward and result semantics remain unchanged.
- [x] Repeated and already-interviewed NPC behavior and later client presentation remain available as intended.
- [x] JSON conditions and response paths remain functional; no unnecessary branching authoring system is added.
- [x] A new NPC can supply content, targets and outcomes through the shared path instead of copying a controller.
- [ ] **User Unity/device acceptance of the matrix above.**

## Verification and handoff

Compile; test data conversion and session lifecycle and once-only outcomes at the highest existing seam feasible. Existing PlayerProgressData tests do not cover these behaviors.

Record historical audit defects separately. The duplicate `BeginFinalAudit` caller in `AuditorSubmissionTray.cs` remains neutralized by admission/state guards rather than removed. User performs Unity manual testing and Git write actions. Stop for approval.

Next unit after acceptance: **T03 checkpoint 1** (conversation-only standing Elijah visual and grounding preview). Do not start it from this ticket's completion alone.
