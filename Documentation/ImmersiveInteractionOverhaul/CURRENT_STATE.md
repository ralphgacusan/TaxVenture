# Current implementation

Inspected 2026-10-14 from scripts, package/project settings, scene serialization, and imported asset metadata. This is **static inspection**, not a Unity Editor or Android playthrough. Future agents must update this as tickets land.

## Baseline and existing work

- Root: `C:\Learn\Projects\TaxVenture\TaxVenture`; branch `bug-fixes`, tracking `origin/bug-fixes` at inspection.
- Unity `6000.4.5f1`; URP `17.4.0`; Input System `1.19.0`; UGUI `2.0.0`; Test Framework `1.6.0`; Timeline `1.8.12`. Source: `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`.
- Cinemachine is not a direct dependency in the manifest. No assumption that it must be installed: inspect existing camera reuse first.
- `ProjectSettings/ProjectSettings.asset` has `activeInputHandler: 2`; current code mixes legacy Input and the newer Input System.
- Prior uncommitted changes: `Assets/Scripts/Player/CameraController.cs` and `Assets/Scripts/SideQuest1/CollectedProfilesWorldPanel.cs`. They implement the recent paper viewer and must survive this overhaul.
- `docs/GAME_FLOW_CURRENT.md` correctly describes first-person exploration; the root README's third-person description is historical intent. `docs/LOGIC_AUDIT.md` contains older risks requiring live verification before fixes.

## Controls, camera, and scene wiring

| System | Inspected behavior and dependency |
|---|---|
| `Assets/Scripts/Mobile/VirtualJoystick.cs` | UGUI pointer down/drag/up; clamps vector to unit circle, moves handle immediately, resets immediately on release/disable/hide. No explicit dead zone, easing, or active-pointer ownership. One movement reference is wired in Office_Level1. |
| `Assets/Scripts/Mobile/MobileLookArea.cs` | Drag area accumulating `LookDelta` with `ConsumeDelta()`. No joystick visual or pointer-ID ownership. Scene object exists but is inactive; its existence does not mean look input uses it. |
| `Assets/Scripts/Player/PlayerMovement.cs` | Rigidbody velocity in FixedUpdate, camera-relative movement, rotates player toward travel, footstep timing. Uses VirtualJoystick then legacy axis fallback. Normalizing the camera-relative direction discards analog stick magnitude, an important T01 defect. No Animator driving here. |
| `Assets/Scripts/Player/FirstPersonCameraController.cs` | Enabled on active main camera in Office_Level1. Mouse delta + touch swipe, guards against camera mode other than FirstPerson; owns pose in LateUpdate. Touches starting over UI are rejected. Camera position follows player at fixed height. |
| `Assets/Scripts/Player/ThirdPersonCameraFollow.cs` | Existing orbit/follow code with distance/height/smoothing, mouse/swipe input, pitch limits and debug first-person toggle. No environmental collision queries or shared mode guard in inspected LateUpdate. Do not enable alongside another pose writer. |
| `Assets/Scripts/Player/CameraController.cs` | Central existing owner for FirstPerson, Workstation, Interview, CorkboardSideQuest and ProfileViewer modes. Start explicitly enables first-person exploration. Several mode exit/reset paths assume FirstPerson. Movement/look/interactor/joystick visibility is toggled here. |
| `CameraController.MoveCameraToViewpoint` | SmoothStep position/rotation interpolation with final-pose assignment and one active transition. No obstacle sweep in inspected transition. Several exit paths immediately hand pose control back; T03 must reconcile them with third-person restoration. |
| `Assets/Scripts/Player/FirstPersonHands.cs` | Shows/hides placeholder hands and carried document; does not animate the humanoid player. Preserve carried-document presentation. |
| `Assets/Scripts/Player/Interactor.cs` | Center-screen ray + legacy mouse click, `IInteractable` focus/prompt/highlight; UI exclusion uses default pointer-over query. Range/focus assumptions must be reassessed when camera is behind player. |
| `Assets/Scripts/Player/WorkstationInteractor.cs` | Cursor-position ray and legacy click in desk mode; independently checks UI and clears focus on disable. CameraController owns enable/disable. |
| `Assets/Scripts/ToggleCamera.cs` | Menu/level-selector camera transition code; not the exploration controller. Keep menu navigation outside runtime overhaul unless an actual dependency is found. |

`Assets/Scenes/Office_Level1.unity` contains active Player with Rigidbody, CapsuleCollider, PlayerMovement, Interactor, and an active Visual child. PlayerMovement references the active movement joystick. One tagged Main Camera is active with FirstPersonCameraController; another tagged Main Camera is inactive. Do not infer two active cameras from duplicated names. Scene serialized references, not asset presence alone, decide the active setup.

The active Visual child contains an inactive Capsule: no enabled visible humanoid player mesh was found in this scene. `ThirdPersonCameraFollow` is absent from Office_Level1; it appears in Office_Level2 with `firstPersonMode: 1`. Merely enabling that script does not deliver the requested third-person experience.

`HUD_Canvas` uses Screen Space Overlay (`m_RenderMode: 0`), a GraphicRaycaster, and CanvasScaler Scale With Screen Size (`m_UiScaleMode: 1`), reference 1920x1080, Match Width Or Height 0.5. Safe-area behavior is not certified by this serialized configuration. `Assets/InputSystem_Actions.inputactions` defines Player Move/Look/Interact and other actions, but PlayerMovement currently reads its custom joystick/legacy axes directly; no shared action-map ownership should be presumed.

## Player model and animation assets

- **User decision during planning: use Elijah as the main character.** Existing files: `Assets/3D Models/npc_elijah/npc_elijah_static.fbx`, `npc_elijah_Idle.fbx`, `Elijah_Walking.fbx`, and `npc_elijah_static.fbm/elijah_texture.png`. `TownNpcDialogueLibrary.cs` already identifies the player as Elijah.
- Static/idle importer metadata currently uses `animationType: 2` (Generic), `avatarSetup: 0`. Walking metadata uses `animationType: 3` (Humanoid), `avatarSetup: 1`; its explicit clip is named `walking_idle` with `loopTime: 0`. This mismatch needs T02 rig/avatar/clip inspection; filenames alone do not establish compatible looping idle/walk.
- Do not mutate shared NPC importers blindly. Determine whether the chosen FBXs are used by any NPC/scene and whether animation-bearing Elijah mesh can provide a valid player avatar. Prefer a player-specific prefab/controller and reference existing textures/clips; coordinate any necessary importer changes. No rig conversion was performed in planning.
- Alternative discovered before the Elijah decision: `Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Business_Male_01.prefab`, using `Assets/Synty/PolygonOffice/Models/Character.fbx`. This is not the selected player and must not replace Elijah without the user's approval.
- `Character.fbx.meta`: Humanoid (`animationType: 3`), animations not imported (`importAnimation: 0`). Prefab has Animator/Avatar references; its controller GUID did not resolve during inspection. Validate these in Unity before considering it ready.
- Existing animation candidates are in `Assets/3D Models/npc_tutorial_static/`, `npc_jhun_static/`, and other npc folders, with idle/walking FBXs and some controllers/clips. They are not currently player locomotion wiring. Retarget only after checking avatar validity, root motion, clip permissions, and actual animation quality.
- Synty importer `licenseType: Store` does not establish purchase/redistribution entitlement. Custom NPC licenses, triangle counts, skinning/bone cost, avatar validity, foot contact and Android cost remain unverified.
- Use the selected Elijah assets first. If a verified rig defect blocks them, report it with a repair proposal before considering substitution; any external asset requires exact source URL, license and rig/performance assessment before importing. No external download or model change occurred in this session.

## Interaction adapters and protected paper behavior

- `Assets/Scripts/Interactables/IInteractable.cs`, `HighlightEffect.cs`, and `Assets/Scripts/UI/InteractionPromptUI.cs` supply existing focus/feedback seams.
- `DeskInteractable.cs` uses a serialized desk viewpoint and `CameraController.EnterFirstPerson`. `CorkboardInteractable.cs` uses its viewpoint and game/tutorial callbacks. Side-quest board camera entry is separately managed by `CorkboardSideQuestManager.cs` and the camera's CorkboardSideQuest API; preserve both board uses.
- `Assets/Scripts/UI/NotesPanelUI.cs` reuses `FloatingWindow`/`SwipeDownToClose`, refreshes tasks, and intentionally **does not** lock movement/camera today. The requested close reading mode changes that policy and belongs to T03, not a replacement notes/task implementation.
- `Assets/Scripts/SideQuest1/TaxpayerProfileWorldPickup.cs`: existing paper drop/flutter/bob and collection-to-HUD flight.
- `TaxpayerProfileCollectionHUD.cs`: collection/count/events and `TryGetCollectionScreenPoint()` resolving icon center for overlay/camera Canvas; reuse this anchor.
- `CollectedProfilesWorldPanel.cs`: camera-local readable layout, icon-to-paper opening, mouse/touch dragging, frontmost selection with perspective compensation, viewport bounds, and staggered shrink/return closing. It cancels opening when closing early, rejects repeated closing clicks, and retains its lock through closing.
- `CameraController.TryBeginProfileViewer/EndProfileViewer`: snapshot enabled states, hide/reset joystick, freeze player Rigidbody, unlock cursor, guard world raycasting, restore on exit/cleanup. These currently assume FirstPerson as exploration; T02/T03 must migrate them deliberately.
- `ProfileDragCard.cs`, `ProfileCardTextLayout.cs`, `CorkboardSlot.cs`, `CorkboardSideQuestManager.cs`: separate side-quest cards, text fitting, free placement, tray entrance and pin feedback. Do not confuse viewer dragging with board placement.
- `Assets/Prefabs/ProfileWorldPickup_TEMP.prefab` is reused for viewer visuals. Existing viewer papers disable pickup/legacy drag/colliders. No paper prefab/size/material redesign is authorized here.

## Dialogue data, paths, and outcomes

Shared UI already exists: `Assets/Scripts/UI/DialogueUI.cs` accepts `List<DialogueLine>` and one completion callback, displays the full line immediately, advances through Continue, hides/unlocks at the end, clears state, then invokes the callback. No typewriter was found. Empty lists call completion synchronously. A second start can replace the first queue/callback: there is no active-session rejection guard.

`Assets/Scripts/Data/DialogueLine.cs` stores NPC/player speaker, text, portrait ID and speaker name; `DialogueBuilder.cs` builds linear sequences. `CaseFactory.BuildDialogue` converts JSON case dialogue; only the exact Npc speaker value maps to NPC. Existing data is not a generalized branch-node graph. Preserve current conditional outcome selection and any live question UI; do not invent a new dialogue-authoring engine.

| Caller | Activation/content | Completion side effects to preserve |
|---|---|---|
| `Assets/Scripts/Interactables/ReceptionistInteractable.cs` | IInteractable, highlight, NPC FSM; case JSON greeting | Case-specific spoken flag, Completed NPC state, ReceiveCase game state, notes unlock, tutorial report. It pre-locks controls; its direct empty-content branch can bypass the UI's unlock. |
| `ClientInteractable.cs` in same folder | IInteractable/NPC FSM; case interview lines and conditionally selected findings outcomes | Interview flag/tutorial report; presentation flag, outcome routing and guarded reward/result path. Completed interview does not mean all later client interaction is unavailable. |
| `AuditorInteractable.cs` in same folder | IInteractable/NPC FSM; reminder/audit builder lines | Submission -> audit dialogue -> summary -> successful reward/case completion or retry/reset. Reminder returns Idle; success Completed; failure returns Idle. |
| `ColleagueInteractable.cs` in same folder | IInteractable/highlight; builder text; local one-shot flag | Local completion rather than a shared NPC FSM. |
| `Assets/Scripts/SideQuest1/TaxpayerNpcInteractable.cs` | Pointer clicks/NPC FSM; `TownNpcDialogueLibrary.cs` | Conversation completion marks Completed and drops that NPC's profile. Four taxpayer targets are separate from the office client's case interview. |
| `TutorialNpcController.cs` in same folder | Pointer click and TownEvents, no NPC FSM | Collection enables guide; dialogue completion starts board; side-quest completion drives final dialogue/panel. |

`Assets/Scripts/UI/InterviewClientUI.cs` still contains selectable questions/answer application. Current ClientInteractable uses DialogueUI and fixed interviewLines; Inspector wiring of legacy question UI remains to be confirmed before removing/adapting anything.

Town flow: taxpayer conversation -> profile pickup -> HUD collection/all-profiles event -> tutorial NPC -> corkboard -> completion events/popup and office-return indicator. `TownToOfficeTransition.cs` only reveals an indicator; the office-start call is commented. This overhaul must not silently change level/quest gates.

## NPC indicators and progress dependencies

`Assets/Scripts/FSM/NPC/NpcStateMachine.cs` initializes Idle in Start and emits changes. `NpcVisualIndicator.cs` subscribes and maps StateName strings to material colors. Existing color changes are not the requested overhead available/completed marker. Interactables own completion changes; DialogueUI does not mark NPCs complete itself.

Progression depends on `Assets/Scripts/FSM/GameStateMachine.cs`, `Assets/Scripts/Data/CaseProgressionManager.cs`, case data/loading, `TaskListProvider.cs`, `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs`, and town events. Read-only historical audit reports duplicate global FSM/startup races and retry issues. A current source inspection confirmed two BeginFinalAudit calls in `AuditorSubmissionTray.cs`; runtime impact was not tested. Track these as integration risks, not permission to repair the entire progression system.

## Evidence and testing limits

- This planning session performed source/scene/package/metadata inspection and documentation integrity checks only; no new gameplay, PlayMode, device, camera-collision, or model animation pass is claimed.
- In the preceding paper work, direct C# compilation and numeric geometry/timing checks passed. User reported Phase 3 working. Phase 4/5 full manual acceptance has not been recorded; do not label them fully playtested.
- Existing `Tests/VerifyProfileCards.ps1` checks paper assets/layout/material references. `Tests/ProgressionRegression/ProgressionRegression.csproj` checks PlayerProgressData, not dialogue/interview behavior.
- Existing `Assets/Editor/CorkboardRegressionValidation.cs`, `PaperDropValidation.cs`, `PaperCollectionValidation.cs`, `PaperTrayValidation.cs` provide Unity preview/Editor checks; inspect their scope/version before using reports under Library as evidence.
- Character asset/Canvas inspection details and outstanding measurements are recorded above and in IMPLEMENTATION_PLAN.md. Elijah is selected; exact rig validity, appearance fidelity, licenses, triangle counts, on-device performance and live safe-area/framing checks require T02/T05 verification.
