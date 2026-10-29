# Inspected baseline and reuse map

Inspection date: 29 October 2026. Read-only source, YAML, catalogue, controller, document and test inspection; no Unity import/render/Play Mode or performance test. `ProjectSettings/ProjectVersion.txt`: Unity **6000.4.5f1**. Installed manifest includes URP 17.4.0, Input System 1.19.0, UGUI 2.0.0 and Test Framework 1.6.0; no version change is needed. Package availability is not evidence that the scene uses a particular feature.

## Level 1 and Level 2 status

Level 1 is the current office/plaza/classification/Marie route with campaign foundation, physical reference/records, current NPC life and client delivery. Level 2 is Rosa's Laundry review with first-check income annotations, summary reconciliation, physical stamp, explicit three-way choice, review/next-day handling, and Client Copy follow-through. Latest Campaign Foundation T33/T21 and the CHECK-to-Rosa follow-up report internal verification and normal Level 2 access enabled; user manual acceptance remains pending. Older ticket headings and JSON comments still describe superseded states. Preserve these manual gaps; Level 3 does not retroactively certify them.

Only `Documentation/Level3DesignProposal.md` existed as Level 3 design before this pack; no `Assets/Scenes/Office_Level3.unity` or mapped Level_03 implementation was found. Career has coming-soon Chapters 3–5. `ComputeTaxesState` is a state shell. `TaxComputationCalculator` is an explicit flat-rate prototype; `ComputerUI.Show()` clears inputs and its existing calculate route is not a persistent manual tape calculator.

Saved scene SHA256 at inspection (recompute at future implementation; do not enforce stale hashes on later user edits):

- Office_Level1: `F4564ECD2513CB2B5E8CF81E2FFE92A075649F591AD52024674A44D24C675275`.
- Office_Level2: `F5B8538E20841926228B4F84BB5447DB9C1C4AA488B65E73F435F06343DB19E7`.

## Concrete source references

Paths below are verified existing files relative to repository root. Any new Level3 path in tickets is explicitly proposed.

| Area | Verified source | Reuse and necessary extension |
|---|---|---|
| Scene/environment | `Assets/Scenes/Office_Level1.unity`; `Assets/Scenes/Office_Level2.unity`; `Assets/Editor/Level2SceneSetup.cs` | One-time saved Level2 copy, preserve source, specialize local startup. Existing setup registers only L2 and is not an L3 builder |
| Scene capabilities | `Assets/Scripts/Data/OfficeLevelCatalog.cs`; `Assets/Scripts/Data/OfficeLevelProfile.cs` | Currently maps L1/L2 only. Add opt-in L3 map/HUD/guide/fade/appropriate finale capabilities |
| World controls | `Assets/Scripts/Player/PlayerMovement.cs`; `Interactor.cs`; `FirstPersonCameraController.cs`; `CameraController.cs`; `WorkstationInteractor.cs`; `Assets/Scripts/Mobile/VirtualJoystick.cs` | Preserve current first-person movement/look/tap and workstation/interview transitions; no new third-person system |
| FSM/objectives | `Assets/Scripts/FSM/GameStateMachine.cs`; `Assets/Scripts/Data/CaseProgressionManager.cs`; `Assets/Scripts/UI/HUD/OfficeObjectives.cs` | Existing ordered state transitions and observer tasks. L3 needs bounded chapter/case phases; prevent old filing/completion routes firing early |
| Input/UI ownership | `Assets/Scripts/UI/WorkstationUI.cs`; `Assets/Scripts/UI/HUD/HudWindowLock.cs`; `Assets/Scripts/UI/Guide/GuideTargets.cs` | Acquire/release existing holds, avoid tap-through; add L3 target bindings and cleanup |
| Dialogue | `Assets/Scripts/UI/DialogueUI.cs`; `DialogueSession.cs`; `Assets/Scripts/Level2/Level2RosaConsultation.cs` | AwaitAction/CompleteAction, terminal callbacks and existing choice presentation. Rosa-specific effects are not general APIs for Bea |
| NPC life | `Assets/Scripts/FSM/NPC/NpcLife.cs`; `NpcLocomotion.cs`; `Assets/Scripts/UI/HUD/WorldEmote.cs`; `Assets/AssetIntegration/NpcHumanoid.controller` | One actor owner, quest travel/conversation priority, idle/walk procedural gestures/emotes; no new NPC framework |
| Papers | `Assets/Scripts/SideQuest1/ClientRecordPaper.cs`; `SupportingDocumentFace.cs`; `Assets/Scripts/UI/CaseFolderUI.cs`; `Assets/Scripts/UI/Books/PageTurnStage.cs`; `Assets/Scripts/UI/DocumentViewerManager.cs` | Same bodies/faces/extract/compare/return; add Type 8 presentation only when required. Type 8 support is not claimed merely because its standard exists |
| Calculation | `Assets/Scripts/UI/Computer/ComputerUI.cs`; `ComputerFieldSlot.cs`; `Assets/Scripts/Data/TaxComputationCalculator.cs`; `Assets/Scripts/FSM/States/ComputeTaxesState.cs` | Inspect value-transfer seams; add persistent source/tape rules without silently replacing L1's guided calculator behavior |
| Stamp/audit | `Assets/Scripts/Level2/Level2DeskStamps.cs`; `Level2StampDrag.cs`; `Level2Audit.cs`; `Assets/Scripts/Data/FinalAssessment.cs` | Reuse actual physical interaction; narrow case/revision adapter needed because L2 code is Rosa-bound. New arithmetic/support validator is necessary |
| Campaign/save | `Assets/Scripts/RewardsSystem/PlayerProgressData.cs`; `PlayerProgressManager.cs`; `CampaignSave.cs`; `CampaignRun.cs`; `Assets/Scripts/Save/SaveFileStore.cs`; `Assets/Scripts/Level2/Level2Checkpointer.cs` | Existing signed owner, modes, candidate/save/publish. Extend checkpoint with two case subrecords and pending deliveries, not two competing save owners |
| Rewards/results | `Assets/Scripts/RewardsSystem/CampaignRewards.cs`; `Level2CampaignRules.cs`; `CaseResultRules.cs`; `ClientCopyRules.cs`; `LevelScoreLedger.cs`; `LevelScoring.cs`; `LevelRewardRules.cs` | Fixed catalogue/claims and first grade; add distinct case IDs, combine worksheet XP once. Current handover/result rules contain L1/Rosa assumptions |
| Bag/shop | `Assets/Scripts/RewardsSystem/CampaignInventory.cs`; `CampaignShop.cs`; `CampaignVouchers.cs`; `Assets/Scripts/Interactables/OfficeDecor.cs`; `Assets/Resources/Campaign/inventory.json`; `shop.json` | Same items/capacities/pins/quantity/wallet; extend scopes and source resolvers. Drawer is removed from play; stand/computer currently pending |
| Career/replay | `Assets/Scripts/RewardsSystem/CampaignMenu.cs`; `CareerProgressService.cs`; `Assets/Resources/Career/chapters.json`; `Assets/Scripts/UI/Menu/CareerChapterSheet.cs` | Continue into Career; add two-case sheet and one badge; only T18 enables story route. L4/L5 remain coming soon |
| Practice | `Assets/Scripts/Training/TrainingSession.cs`; `PracticeModeRuntime.cs` | Reuse binder/drills and local attempt isolation; no duplicate tutorial menu |
| Audio | `Assets/Scripts/AudioManager/AudioManager.cs`; `AudioLibrary.cs`; `AudioEnvironment.cs`; `Assets/Resources/TaxVentureAudioLibrary.asset` | Existing named clip lookup, settings, environment/music transitions; no new direct-play manager |

## Actual asset candidates

- Bea: `Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Developer_Female_01.prefab`, the model referenced for her existing plaza role. Level1 `plaza_life.json` identifies Beatriz Mendoza as freelance graphic artist.
- Company bookkeeper, proposed Nina Cruz: reuse verified `Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Developer_Female_02.prefab` as an intentional model reuse; it is not a newly discovered Nina prefab. No separate portrait is verified. Text/name dialogue and existing silhouette presentation are acceptable; do not claim unique art.
- Calculator: `Assets/Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Calculator_01.prefab` or `_02.prefab`. These are prop assets, not verified functional buttons. Add local hit targets/readable focus controls and tape; retain source mesh/materials.
- Computer: `Assets/Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Computer_Monitor_01.prefab` and `SM_Prop_Computer_Keyboard_01.prefab`; use existing assembled terminal references if better. No existing functional purchased-upgrade anchor is claimed.
- Shared controller states inspected: Standing Idle, Idle Relaxed, Briefcase Idle, Walking, Strolling. A controller state is not proof that a specific reused model animates correctly. Test binding/Avatar internally, user verifies rendered motion. No special talking/handover clip is required; reuse existing procedural gestures.
- Supplied audio exists at `Assets/AudioManager/TaxVenture_Audio_Library/SFX/Corkboard/sfx_card_pickup_02.mp3`, `sfx_card_place_01.mp3`, `sfx_card_return_02.mp3`; office `sfx_stamp_pickup_01.mp3`, `sfx_stamp_press_01.mp3`, `sfx_stamp_return_01.mp3`. File names and runtime AudioLibrary names may differ; inspect resolved clip identity before binding. No dedicated calculator-key/tape-feed clip is asserted.

## Keep/change/disable

Keep environment, authored lighting, doors, player, joysticks, cameras, EventSystem, audio, office desk/book/stamp and physical source references. Verify actual owner counts; the YAML has multiple named directional lights, so never assume a single light or blindly delete apparent duplicates. Copying a scene is not a performance optimization.

In the new scene only, disable copied Rosa start/retrieval/stall/toolkit/audit/checkpointer/choice/finale owners and copied Level1 profile/corkboard/Marie startup. Retain functional shared service actors/components even if historical names mention a level. No automatic L2 grants, source pickups, deadlines or guide pages may run in L3. Existing Arthur reference remains read-only; Quax is a message, not a guide replacement.

## Integration risks and tests

Single-case result/checkpoint assumptions; stage-to-FSM mismatch; premature legacy filing/finale transitions; shared calculator correctness changes; duplicate scene bootstrap; silent pin rebinding; one claim hiding the second case; template grading before all fields are ready; nested modal/camera cancellation; copy ownership after case switch; review-clock persistence/consumption; static asset assumptions; conflicting saved-source scopes. Each has a ticket owner in the index and an executable/manual verification row. The current CF-37 outline-renderer concern and source-only NPC/animation acceptance gaps are inherited limitations, not new failures or permission to edit render settings.
