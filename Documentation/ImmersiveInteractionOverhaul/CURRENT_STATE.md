# Current implementation

Updated 2026-10-15. Retain existing first-person exploration and T01 controls (`74d53caf`) on `feat/immersive-interaction-overhaul`. Source/scene inspection and compilation are not Unity gameplay acceptance. T02 (both phases) and T03 (all four checkpoints) are code-complete; user scene setup and manual acceptance are pending for both.

## T03 conversation presentation — implemented, scene setup pending

Exploration stays first person. During an admitted NPC conversation only:

- **`ConversationActorVisual`** (scene object `ConversationElijahVisual`, created by the setup tool).
  - A standing Elijah built at runtime from `Assets/3D Models/npc_elijah/npc_elijah_Idle.fbx`, with colliders stripped and the Animator disabled. Its pose is sampled from the 7.57 s idle clip, held on one frame or looped at no more than 30 Hz.
  - Scale = `standingHeight` (world units, proposed as the median measured NPC height) / measured posed height, and the lowest posed point sits on the floor.
  - Measured facts: the FBX files are in centimetres, Y up, an armless/legless figure whose coat reaches the floor, about 6.7 m tall at import scale. The rig is Generic with no avatar. The walking model is not used.
  - Hidden outside conversations and while the camera is within 0.3 × height of his head.
  - Never touches the player root, collider, NPC transforms or importers. Two inactive leftover Elijah instances from the abandoned third-person setup remain untouched in `Office_Level1`.
- **`DialogueCameraDirector`** (on the CameraController object). It implements `IDialoguePresentation` and is the single camera writer while `DialogueUI` holds the controls.
  - NPC lines frame over Elijah's right shoulder; player lines over the NPC's left shoulder, on the same side of the axis.
  - Distances are fractions of character height. Pure geometry lives in `DialogueShotMath` (`System.Numerics`).
  - Obstruction tests (clearance sphere, shoulder→camera and camera→face lines, ignoring the NPC, player and visual) drive the fallback chain: pull-back 100/75/50%, a two-shot on the same side, then the other side, then a first-person look-at.
  - Elijah is placed visually at the player's floor point, clamped to 0.5–0.85 × height from the NPC.
  - NPCs are resolved by `DialogueSpeakerFrames`: an optional `DialogueSpeakerAnchor` (head and feet), otherwise visible renderer bounds (up to two parents; pickups, UI, particles and the visual excluded), otherwise colliders.
- **Return**:
  - The logical first-person pose comes from `FirstPersonCameraController.TryGetViewPose` (correct even when a conversation starts in the frame a corkboard exit handed the camera back), together with the FOV, hands and carried document.
  - End or cancel: panel hidden → blend back (0.45 s, unscaled) → hands and document restored and Elijah hidden → `DialogueUI` releases the controls → completion or cancellation callback.
  - Disable or unload restores immediately and completes any pending return. The director never restores over another camera mode.
- **Panel and typewriter**:
  - `DialogueRevealSchedule` (TMP visible characters, 40 cps, 0.22 s sentence and 0.08 s clause pauses at real breaks).
  - `DialogueAdvanceGate` (0.15 s opening grace, 0.12 s minimum gesture interval, reveal-before-advance).
  - `DialogueBoxTapRelay`, added at runtime if missing; it ignores drags and long presses.
  - The setup tool restyles the panel: an invisible full-width tap-absorbing strip with `SafeAreaBottomPanel`, a dark box 76% wide and 250 high on the 1920×1080 reference, an amber speaker name, auto-sized 22–32 white text and a Continue button. The oversized portrait object is hidden but kept.
- **Editor**: **Tools > TaxVenture > Configure Dialogue Presentation** (Undo; CPU-skins the posed model to measure it, measures NPCs and logs a report) and **Toggle Conversation Visual Preview** (an unsaved Edit-mode Elijah by the player facing the selected or nearest NPC).

Until the user runs the setup menu and saves the scene, conversations behave as T02 plus the typewriter. `Office_Level2` is unconfigured.

## Project and current camera

Unity 6000.4.5f1, URP 17.4.0, Input System 1.19.0, UGUI 2.0.0; verify live package files before API changes. Mixed legacy/Input System paths are enabled (`activeInputHandler: 2`). Main scene: `Assets/Scenes/Office_Level1.unity`. Office_Level2 configuration remains unverified.

`Assets/Scripts/Player/FirstPersonCameraController.cs` controls normal exploration. `CameraController.cs` owns existing FirstPerson, Workstation, Interview, CorkboardSideQuest and ProfileViewer modes. Preserve serialized enum values, public callers, original player Rigidbody/capsule and scene transforms. `MoveCameraToViewpoint` already interpolates scripted position/rotation with one active transition. These viewpoint APIs remain in place. T02 reuses current control APIs for conversations; T05 verifies existing interaction cancellation and return behavior without requiring a new camera system.

## Retained T01 controls

- `Assets/Scripts/Mobile/VirtualJoystick.cs`: one MOVE pointer, dead zone/sensitivity, analog magnitude, immediate logical release, eased thumb return/press feedback and capture/release/focus reset.
- `Assets/Scripts/Mobile/JoystickInputMath.cs`: production response math; `Tests/ControlsRegression` exercises it with 5,734 assertions.
- `Assets/Scripts/Player/PlayerMovement.cs`: camera-relative Rigidbody motion retains analog strength, desktop keyboard fallback and horizontal stop on disable/focus loss.
- `FirstPersonCameraController.cs`: UI-excluded single-finger swipe originating in the right 75%, resolution-normalized delta, desktop mouse fallback and existing mode guards.
- `Assets/Scripts/Mobile/MobileInputGuard.cs`: fresh GraphicRaycaster UI hits, touch/joystick capture and native tap discrimination, reused by interaction/NPC/pickup routes.
- `Assets/Scripts/Mobile/MobileControlsSafeArea.cs`: insets only its configured controls root.
- `Assets/Editor/MobileControlsSetup.cs`: **Tools > TaxVenture > Configure Mobile Controls**, outside Play; uses Undo and marks scene dirty for user save. MOVE base/handle 280/112, center (260,240), travel 80, dead zone .12, sensitivity 1. No visible LOOK joystick or full-screen blocking overlay.

Runtime and the setup tool compiled; production math regression passed. Touch routing, feel, setup/Undo, safe areas and gameplay remain user-run manual checks. Do not repeat already confirmed user work unnecessarily.

## Existing interactions and papers

- `Assets/Scripts/Player/Interactor.cs`: current first-person center-ray interaction; `WorkstationInteractor.cs`: desk pointer route. Shared seams: `Assets/Scripts/Interactables/IInteractable.cs`, `HighlightEffect.cs`, `Assets/Scripts/UI/InteractionPromptUI.cs`.
- `DeskInteractable.cs`, `CorkboardInteractable.cs`, `CorkboardSideQuestManager.cs`: separate viewpoint/callback routes; preserve both board uses and progression/tutorial events.
- `Assets/Scripts/UI/NotesPanelUI.cs`: FloatingWindow/SwipeDownToClose and task refresh; currently permits movement/look. Preserve this existing policy; T05 checks notes closing and input behavior without redesigning it.
- `Assets/Scripts/Player/FirstPersonHands.cs`: existing hands/carried-document presentation; preserve it.
- `Assets/Scripts/SideQuest1/TaxpayerProfileWorldPickup.cs`: paper drop/bob and collection-to-HUD flight. `TaxpayerProfileCollectionHUD.cs`: count/events and `TryGetCollectionScreenPoint()` icon anchor; `CollectProfile` ignores duplicates and raises `OnAllProfilesCollected` once the count reaches its configured total of 4.
- `CollectedProfilesWorldPanel.cs`: readable camera-local layout, folder opening, paper dragging/frontmost selection, viewport bounds and staggered return closing. Preserve early-close/repeated-click handling and locks through the final return.
- `CameraController.TryBeginProfileViewer/EndProfileViewer`: enabled-state snapshot, Rigidbody freeze, joystick reset, cursor/world-raycast management and restoration. Reuse rather than add a competing lock owner.
- `ProfileDragCard.cs`, `ProfileCardTextLayout.cs`, `CorkboardSlot.cs`, `CorkboardSideQuestManager.cs`: side-quest text, free placement, tray entrance and pin feedback, separate from viewer dragging.
- `Assets/Prefabs/ProfileWorldPickup_TEMP.prefab`: shared viewer visuals with pickup/legacy drag/colliders disabled for viewer instances. Preserve paper sizes/materials and quest behavior.
- `CorkboardSideQuestManager` exposes `CanInteract` (`sideQuestActive && !sideQuestCompleted`), guards its completion raise with `completionEventRaised`, and `BeginSideQuest` refuses to open without its camera, viewpoint, four valid slots, card prefab, tray and four collected profiles. Callers must treat a refused open as a failure, not a start.

## Dialogue data, paths, and outcomes

Shared `Assets/Scripts/UI/DialogueUI.cs` uses `DialogueSession.cs` for admission, line snapshots/sequencing and once-only terminal claims. `TryStartDialogue(lines, completed, cancelled, owner)` returns false for invalid, empty, busy or unavailable starts without calling success; `StartDialogue` remains a compatible bool wrapper. One active UI is admitted globally. UI is attached to its own panelRoot and supports starting while that panel is inactive. Serialized fields and Continue binding are retained. Disable/owner loss cancels, never completes. Terminal order (T03): session claim → panel hidden → optional presentation return → control release → completion or cancellation callback → `DialogueEnded`; `IsDialogueActive`/`IsAnyDialogueActive` stay true until release. CurrentLine/CurrentOwner, `IsLineTyping`, `IsReturning` and the start/line/line-revealed/end events are available to later tickets. `AdvanceDialogue()` now means one gesture (reveal while typing, otherwise advance/close); it had no external callers.

`CameraController.AcquireDialogueControls` snapshots existing controls (including actual main-camera input), joystick visibility and cursor, and (T03) freezes the player Rigidbody's constraints and velocity, restoring them on release regardless of mode; it changes no viewpoint/mode. Read-only T03 accessors: `MainCamera`, `PlayerRoot`, `Hands`, `TryGetFirstPersonView`. Release is idempotent and does not restore over a changed mode/active transition. Dialogue starts require an available normal exploration state, not a profile viewer or paused game. Native overlay/ownership behavior remains a user-run check.

**All relevant office and town callers now use explicit admission.** Every one admits before FSM/progression changes, cancels owner-scoped, restores NPC eligibility on cancellation, and keeps success once-only. Office admission occurs before FSM/success changes. Client presentation has an explicit existing-case completion guard. Audit evaluation does not commit case fields until successful conversation; a rejected/cancelled consumed submission is kept pending on the auditor for click-to-resume. The existing duplicate `BeginFinalAudit` call in `AuditorSubmissionTray.cs` is neutralized by admission/state guards, not silently removed.

`Assets/Scripts/Data/DialogueLine.cs` stores NPC/player speaker, text, portrait ID and speaker name; `DialogueBuilder.cs` builds linear sequences, attributing NPC lines to its configured name and player lines to "You". `CaseFactory.BuildDialogue` converts JSON case dialogue; only the exact Npc speaker value maps to NPC. Existing data is not a generalized branch-node graph. Preserve current conditional outcome selection and any live question UI; do not invent a new dialogue-authoring engine.

`TownNpcDialogueLibrary` holds the four hardcoded town conversations. Its placeholder fallback for an unknown id is a **valid** one-line conversation, so callers that advance quest progress must gate on the `recognized` flag from `GetDialogueFor(npcId, displayName, out bool recognized)` or on `HasDialogueFor(npcId)`. The single-argument `GetDialogueFor` signature is unchanged; `TaxpayerNpcInteractable` is its only live caller.

| Caller | Activation/content | Completion side effects to preserve |
|---|---|---|
| `Assets/Scripts/Interactables/ReceptionistInteractable.cs` | IInteractable, highlight, NPC FSM; case JSON greeting | Case-specific spoken flag, Completed NPC state, ReceiveCase game state, notes unlock, tutorial report. Admission precedes locks/progression; empty data does not complete the greeting. |
| `ClientInteractable.cs` in same folder | IInteractable/NPC FSM; case interview lines and conditionally selected findings outcomes | Interview flag/tutorial report; presentation flag, outcome routing and guarded reward/result path. Completed interview does not mean all later client interaction is unavailable. |
| `AuditorInteractable.cs` in same folder | IInteractable/NPC FSM; reminder/audit builder lines | Submission -> audit dialogue -> summary -> successful reward/case completion or retry/reset. Reminder returns Idle; success Completed; failure returns Idle. |
| `ColleagueInteractable.cs` in same folder | IInteractable/highlight; builder text; local one-shot flag | Local completion rather than a shared NPC FSM. |
| `Assets/Scripts/SideQuest1/TaxpayerNpcInteractable.cs` | Pointer clicks/NPC FSM; `TownNpcDialogueLibrary` | Conversation completion drops that NPC's profile **then** marks Completed; a failed drop restores the prior state instead of retiring the NPC. The start is refused when the pickup is unassigned or `npcId` resolves to no profile or no authored dialogue, because completion is one-shot and gates the four-profile collection. Four taxpayer targets are separate from the office client's case interview. Exposes `HasBeenTalkedTo`/`IsInConversation`. |
| `TutorialNpcController.cs` in same folder | Pointer click and TownEvents, no NPC FSM | Collection enables guide; intro completion opens the board, but the guide is retired only once `CorkboardSideQuestManager.CanInteract` confirms it opened; side-quest completion drives the final dialogue/panel once only, with the interrupted-final-guide retry retained. Exposes `HasAppeared`/`IsInConversation`/`HasStartedSideQuest`. |

`Assets/Scripts/UI/InterviewClientUI.cs` still contains selectable questions/answer application and is deliberately untouched. One serialized instance remains in the built scene with all bindings intact; its only two UnityEvent references are its own in-panel buttons calling `OnPresentationContinuePressed` and `OnFinishInterviewPressed`, which are not openers. No C# calls `Show()` or `ShowPresentation()`, and `ClientInteractable` uses the shared `DialogueUI`. Keep the component, its public method names and its question/answer code unless a later verified live caller requires adaptation.

Town flow: taxpayer conversation -> profile pickup -> HUD collection/all-profiles event -> tutorial NPC -> corkboard -> completion events/popup and office-return indicator. `TownToOfficeTransition.cs` only reveals an indicator; the office-start call is commented. This overhaul must not silently change level/quest gates.

## T05 integration fixes — implemented

- **Popup requests during a conversation.** `CameraController` tracks when a conversation holds the controls (`AcquireDialogueControls` to release). While it does, `LockPlayerControls`/`UnlockPlayerControls` from popups record a deferred request instead of re-enabling look and movement. On release, a pending Locked keeps controls locked for the still-open popup, and a pending Unlocked restores first-person systems.
- **Profile viewer refusals.** `TryBeginProfileViewer` also refuses during a conversation or while `firstPersonCamera` is disabled by a popup lock.
- **Closing-tap suppression.** `MobileInputGuard.SuppressWorldTapsThisFrame()` runs whenever first-person systems are restored, a conversation releases, or the viewer ends. `TryGetWorldTap` ignores that frame, so the closing UI tap cannot reach `Interactor`/`WorkstationInteractor`.

## T04 NPC markers — implemented, scene setup pending

`NpcStatusMarker` (one per NPC, added by **Tools > TaxVenture > Configure NPC Markers**) draws an amber pulsing triangle for available and a green check for completed. It draws nothing when the NPC is unavailable, during any conversation, in non-first-person camera modes, outside 0.35–10 × NPC height, or when occluded. State comes only from each interactable's `INpcAvailabilitySource.Availability`, computed by pure `NpcAvailabilityRules`, and is refreshed on NPC FSM changes, dialogue start and end, and a 0.2 s poll. Markers are anchored above the measured head (`DialogueSpeakerFrames`) and billboarded, use shared procedural meshes, one material (`Assets/Materials/NpcStatusMarker.mat`, created by the tool) and property-block colours, and sit at the scene root on Ignore Raycast with no colliders. The existing `NpcVisualIndicator` body tint is unchanged.

## NPC indicators and progress dependencies

`Assets/Scripts/FSM/NPC/NpcStateMachine.cs` initializes Idle in Start and emits changes. `NpcVisualIndicator.cs` subscribes and maps StateName strings to material colors. Existing color changes are not the requested overhead available/completed marker. Interactables own completion changes; DialogueUI does not mark NPCs complete itself. T04 can read the status properties the town callers now expose instead of inferring availability from colors.

Progression depends on `Assets/Scripts/FSM/GameStateMachine.cs`, `Assets/Scripts/Data/CaseProgressionManager.cs`, case data/loading, `TaskListProvider.cs`, `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs`, and town events. Read-only historical audit reports duplicate global FSM/startup races and retry issues. Track these as integration risks, not permission to repair the entire progression system.

## Evidence and test limits

Full-project compilation (runtime and Editor) against the installed Unity 6000.4.5f1 `Managed` and `NetStandard` assemblies, using the project's generated `.csproj` source lists, defines and package references: baseline 0 errors, with T03 0 errors (2 pre-existing warnings). 610 production dialogue contract assertions passed (311 T02 + 299 T03); 5,734 production controls assertions passed. Actual Unity/device acceptance remains user-run. These are not gameplay passes.

Existing targeted checks: `Tests/DialogueRegression/DialogueRegression.csproj` (DialogueSession, DialogueLine, DialogueBuilder, TownNpcDialogueLibrary, TaxpayerProfileDatabase, DialogueTerminalHandoff, DialogueAdvanceGate, DialogueRevealSchedule, DialogueShotMath); `Tests/ControlsRegression`; `Tests/VerifyProfileCards.ps1`; `Tests/ProgressionRegression/ProgressionRegression.csproj` (PlayerProgressData only); `Assets/Editor/CorkboardRegressionValidation.cs`, `PaperDropValidation.cs`, `PaperCollectionValidation.cs`, `PaperTrayValidation.cs`. Inspect scope/timestamps before using reports. Pending manual coverage includes the T03 setup and preview, shots per NPC route, tight spaces, typewriter and tap arbitration, cancel/return and input restoration, movement/swipe, all four taxpayer conversations and drops, guide/board flow, supported aspects/safe areas and Android multi-touch.

Repository file conventions: HEAD stores LF; working-tree endings vary per file (e.g. `DialogueUI.cs`, `FirstPersonHands.cs` and `Documentation/**` are CRLF on disk). Preserve each file's existing endings and check from non-Windows shells with `git diff --ignore-cr-at-eol --numstat`.
