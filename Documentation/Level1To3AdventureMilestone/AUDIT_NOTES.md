# Audit notes, asset seams and verification entry points

Status: source audit; production and device measurements are not performed. See [CURRENT_STATE](CURRENT_STATE.md) for the requirement matrix and [DECISIONS](DECISIONS.md) for user-approved changes.

## Live ownership map

| Concern | Existing owner / entry | Boundary to preserve |
|---|---|---|
| Entry/Continue | `Assets/Scripts/UI/MenuEntry.cs`; `UI/Menu/CareerBoardPresenter.cs`; `RewardsSystem/CareerProgressService.cs` | Inspect actual Story/Replay selection, pending ending and checkpoint routes; do not infer from catalogue prose |
| Camera/input | `Player/CameraController.cs`, `PlayerControlHold.cs`, `Interactor.cs`, `DialogueCameraDirector.cs`; `Mobile/VirtualJoystick.cs`, `MobileLookArea.cs`, `MobileInputGuard.cs` | FirstPerson guards occur beyond the old orbit script; one camera transform writer and return owner |
| L1 opening/field | `SideQuest1/ArthurOpening.cs`, `ReviewedFilePickup.cs`, `ReviewedFileViewer.cs`, `Level1FieldCheckpointer.cs` | Example never rebinds active Marie; rounds open from saved real inspection milestones |
| Client movement | `SideQuest1/ClientConsultation.cs`; `Level2/Level2RosaConsultation.cs`; `Level3/Level3BeaTable.cs` | One live character per stable identity; arrival only presence |
| NPC motion | `FSM/NPC/NpcLife.cs`, `NpcLocomotion.cs`, `NpcAnimationDriver.cs`, `NpcBodyPose.cs`, `StaticArmRig.cs` | Plan a path per leg; authored doorway pairs; unseen staging only as bounded recovery |
| Papers/Bag | `RewardsSystem/CampaignInventory.cs`; `UI/HUD/HudHotbar.cs`, `InventoryScreen.cs`; `SideQuest1/ClientRecordPaper.cs`; `CaseFolder/CaseFolder3DDrag.cs` | Current-case validity limits use, not ownership; retained text cannot transfer foreign-client values |
| Folder/book | `UI/HUD/CaseFolder3DView.cs`; `UI/CaseFolderUI.cs`; `UI/TaxCodeBookUI.cs`; `UI/Books/PageTurnStage.cs` | Existing compositing, front focus, page turn, physical attachment and close cleanup |
| L2 task | `Level2/IncomeToolkit.cs`, `MarketRecordsDesk.cs`, `Level2AnnotatedPapers.cs`, `Level2OfficeReview.cs`, `Level2Audit.cs` | Actual four-paper/16-decision and annotation source bridge; draft placement is not a check |
| L3 task | `Level3/Level3BeaTable.cs`, `Level3TableStaging.cs`, `Level3PaperMachineModel.cs`, `Level3WorksheetCheck.cs`, `Level3SummaryDocument.cs` | Current machine, nine-decision first outcomes and source-supported collected summary |
| L3 office | `Level3/Level3DeskCalculator.cs`, `Level3CaseFolder.cs`, `Level3ReviewRules.cs`, `Level3DeliveryRules.cs`, `Level3ClosingRules.cs` | Actual shared tools, exact decimals, latest return/reset and delivery/closing ownership |
| Dialogue | `Data/DialogueLine.cs`; `UI/DialogueSession.cs`, `DialogueUI.cs`; source-specific builders | Snapshot and inserted lines need recording metadata; reveal tap and advance remain different gestures |
| Guide | `UI/Guide/GuideDirector.cs`, `GuideTargets.cs`, `GuideVoice.cs`; level guide JSON | Observer only. Arthur guide popups are silent under the user's correction |
| Audio | `AudioManager/AudioManager.cs`, `AudioEnvironment.cs`; `Settings/SettingsManager.cs` | Existing VOICES and volume ownership; old dialogue-SFX methods are empty, not working speech |
| Save/reward | `RewardsSystem/PlayerProgressManager.cs`, `CampaignSave.cs`, `CampaignRewards.cs`, `CampaignRun.cs`; `Save/SaveFileStore.cs` | Candidate→persist→publish, signed current save, once-only claims; do not touch real player saves |
| Chapter continuation | `Cutscenes/Chapters/ChapterOpeningSequence.cs`, `ChapterEndingSequence.cs`, `ChapterEndingRules.cs` | Openings/ending presentation never supplies real case completion |
| Performance | `Core/MobilePerformance.cs`; existing URP assets, `Editor/AndroidPerformanceSetup.cs`, `AndroidStaticBatchingBuildStep.cs` | Measure actual phone before budgets; no editor invocation by AI |

Paths in this table are under `Assets/Scripts/` unless a different prefix is shown. They are inspected seams, not proposed new public APIs.

## Dialogue census — exact bounded counts, not a fictional global total

Counts below were read from current JSON on 11 November. They count authored data entries, **not an executed playthrough, elapsed speech, or all code-generated lines**. Removed/conditional content must be traced at its caller before the recording manifest includes it.

| Source / route | Required or optional | Observed authored entries | Implication |
|---|---|---|---|
| L1 `town.json` four taxpayer `talk` arrays | Required, flexible order | 6 turns each, 24 total; 279 whitespace-separated words total | Already shortened historically; preserve citizenship/residence/work clues while applying current budgets |
| Same file: reviewedFile.arthurIntro | Required opening | 2 strings, 30 words | Keep example separate from four clients and Marie |
| Same file: arthurBoardIntro / arthurBoardDone | Required guide stages | 4 turns/92 words and 3 turns/41 words | Check duplication with guide lesson and real board action before cutting |
| L1 `plaza_life.json` four optional residents | Optional and conditional follow-ups | 27 `talks[].lines` turns, 253 words across 12 exchanges | Full retained dialogue coverage still applies; no compulsory clue or tax-answer label |
| L1 Marie travel interjections | Conditional | 3 single-turn arrays, 32 words | Classify actual conversation versus unvoiced guide notice at source |
| L2 `plaza_life.json` six residents | Optional/conditional | 33 turns, 394 words across 20 exchanges | Ambient optionality does not exempt actual dialogue from voice |
| L3 `town_life.json` Bea introduction | Required | 4 turns/52 words; organizer intro1/13; certificate handover1/16 | Preserve envelope/record and later certificate acquisition boundaries |
| Same file: receptionist Quax message | Required closing | 4 turns/38 words | Message stays limited; it cannot prove the scheme |
| L1 guide JSON | Required/conditional/optional help | 43 lessons,97 pages,1,412 page-text words; longest26 words | Guide pages are not a count of ordinary conversations; popup speech exempt |
| L2 guide JSON | Required/conditional/optional help | 22 lessons,61 pages,1,228 page-text words; longest32 words | Avoid repeated toolkit instruction and objective text |
| L3 guide JSON | Required/conditional/optional help | 28 lessons,37 pages,444 page-text words; longest19 words | Existing coach steps and self-moving demonstrations are already concise in places |

Word counts use whitespace splitting, including any formatting tokens. The table deliberately excludes inactive L3 creditAnswers, finding-choice labels, record text and menu strings from spoken-turn totals. Full route-aware manifest must also cover case JSON, `TownNpcDialogueLibrary`, `DialogueBuilder`/case factory paths, conditional consultation/ethics/Auditor returns, dynamically inserted outcomes and code-authored L3 lines. Do not call a grep count of `new DialogueLine` a complete coverage inventory.

## Known-used asset families and actual references

The resource GUIDs below were resolved against existing `.meta` files; source construction was inspected. Serialized candidates hidden by runtime startup are **not** automatically counted as used. This is the first-pass production inventory, to be expanded into individual active model/material/mesh/texture rows before the final pack is accepted. No project-wide library migration is intended.

| Family / exposure | Verified reference / live use | Status, dependencies and treatment gate |
|---|---|---|
| Arthur, close instruction + optional L2 plaza | `Assets/3D Models/npc_tutorial_static/npc_tutorial_static1@Standing W_Briefcase Idle.fbx`; L2 residentModels[2]; shared `NpcHumanoid.controller` | Humanoid import (`animationType:3`), mesh read/write off, no mesh compression in inspected meta. Pilot smoothing/silhouette + deformation/contact; do not replace identity or trust briefcase pose as grasping capability |
| Elijah, conversation close view → new outdoor body | `Assets/3D Models/npc_elijah/npc_elijah_Idle.fbx`; `ConversationActorVisual`; `Elijah_Walking.fbx` | Idle import `animationType:2`, not Arthur's import type; shared walking source is also used by cutscenes. Prove body/clip compatibility without changing protected shared clip import loops |
| Rosa, task/client close view | L2 scene_assets.rosaPrefab resolves to `Assets/3D Models/npc_cora_static/npc_cora_static@Standing Idle.fbx` | Current identity supersedes old Synty Rosa choice. Preserve clothing/palette/model identity; test current rig plus handover before variant migration |
| Marie, four-profile round and office | `Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Business_Female_03.prefab`; L2 residentModels[4] | Preserve latest Marie/Rosa swap and cross-chapter shared usage; inspect skinned source/material references before changing normals/geometry |
| Bea, familiar optional L1 + required L3 | `Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Developer_Female_01.prefab`; L3 beaPrefab | One same artist identity, not an upgraded wealthy studio-owner variant; grip/envelope/table deformation and clothing silhouette |
| Dante and Carlo, ambient world | `SM_Chr_Security_Male_01.prefab` and `SM_Chr_Developer_Male_02.prefab` under `Assets/Synty/PolygonOffice/Prefabs/Characters/`; L3 guardPrefab/ambientPrefab | Shared L1/L2/L3 usage; bounded animation/visibility, optional talks, no rewards |
| Ernesto, Tess, Lina, L2 ambient | `Assets/Synty/PolygonTown/Prefabs/Characters/SM_Chr_Father_02.prefab`, `SM_Chr_Mother_01.prefab`, `SM_Chr_ShopKeeper_01.prefab`; residentModels[0,1,5] | Use per-person treatment, current grounding/idle/route. Do not duplicate models reserved for protected characters to make new residents |
| Receptionist/Auditor, office close views | Existing authored actors; `Assets/AssetIntegration/npc_auditor_static.prefab`; `StaticArmRig` added by office runtimes | Static arm lowering is a presentation fallback, not articulated finger reach. Record exact mesh/UV/rig source before contact work; missing usable rig is a named pilot dependency |
| Remaining L1 profile/ambient actors | Actual `Office_Level1.unity` roots and living-plaza setup; Jhun rigged source `Assets/3D Models/npc_jhun_static/npc_jhun_static@Standing Idle.fbx` is referenced | Complete per-actor prefab-source tracing for Milo/Zorbix/Lorna and all current actors; never infer identity from asset filename alone |
| Active folder / client copies | Existing shared CaseFolder3DView and `SM_Prop_Folder_Manila_02` source, copy pickup/handover and `ReviewedFileClosedModel.prefab` | Handled-prop pilot candidate; separate manila active copy from reviewed reference. Preserve card holders/page faces, UV/text, collider and contact anchors |
| Readable papers/book/IDs/summary | Current SupportingDocumentFace/ClientRecordPaper, TaxCodeBookUI/PageTurnStage and Level3SummaryDocument | Existing beveled body/darker core/soft shadow language; improve readability/page motion; no library-wide subdivision or flattened replacement |
| Calculator/machine/stamps/toolkit | `Assets/Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Calculator_01.prefab`; current generated L3 machine + shared stamp bodies and L2 toolkit | Preserve actual button/collider/tool authority and source/tape bridge. Per-tool reach/contact; no duplicate demonstration tool unless existing example isolation requires one |
| Rosa exterior / new room | `Assets/Flat_Style_Vehicles/Prefabs/Buildings/Building_4.prefab` available; live frontage/anchor branch decides current use | Representative room section pilot; intentionally hard architectural edges stay. Interior must not depend on exterior mesh having a usable inside |
| Bea table / new workspace | `Assets/Palmov Island/Low Poly Environment Park/Prefabs/Environment/wooden table.prefab`; L3 worktablePrefab | Retain working-surface proportions/targets while staging modest art room; validate camera clearance, occlusion and one-actor route |
| Office/plaza/world set | Existing saved Env/OfficeBuilding geometry in Office_Level1/2/3 and shared source-prefab references | Full active-model census must account for startup hides and runtime builders. Static vs skinned, source materials/texture sizes, collider and shared regression risk per individual family |

Available read-only evidence: file/GUID/prefab/controller links, import configuration, source owner/placement logic, current resource references. **Unavailable measured values:** actual imported vertex/triangle/material-pass counts for the complete active set, in-memory texture/audio residency, rendered hard-edge/silhouette quality, camera-distance readability and phone CPU/GPU/frame pacing. Leave fields “not measured” until the allowed tool/user evidence exists; source file bytes are not GPU memory. A numeric performance budget must follow these measurements.

Current shared controller: Standing Idle, Idle Relaxed, Briefcase Idle, Walk and Stroll, with sources in Jhun/Cora/Arthur idle FBXs and Elijah_Walking. Name-based searches found no clearly named point/reach/grasp/give/receive/carry/sprint/jump clips in the inspected character collection. This does not prove an unnamed clip cannot exist. The pilot must resolve actual compatible clips, a bounded authored/retargeted alternative, and contact quality before those assets become unblocked. Subtle procedural sway or a lowered static arm is not final action animation.

## Current verification commands — discovered, not run here

| Scope | Current entry point and important limit |
|---|---|
| Current runtime + full Editor compilation | `python3 -B Tests/run_level3_repair.py --build-only`; uses existing Bee response files and installed Unity bundled compiler without launching Unity; output `Temp/L3Repair` |
| L3 + controls; optionally L2 | `python3 -B Tests/run_level3_repair.py --level2`; generates disposable output, explicitly skips three native EventSystem comparer checks in its L2 scratch copy. Not native input acceptance |
| Chapter cinematics | `python3 -B Tests/ChapterCutsceneRegression/run_offline.py`; optional `--game` compile; `Tests/ChapterEndingRegression/run_offline.py`; outputs Temp folders |
| Existing intro | `python3 -B Tests/IntroCutsceneRegression/run_offline.py`; no listening/render evidence |
| General scratch-project fallback | `python3 Tests/make_scratch_build.py`, then SDK build of generated project if that SDK is available. Do not blindly use old Windows/Temp paths or old warning counts |
| Content/scene/reference checks | `Tests/VerifyLevel3Scene.py`, `VerifyLevel3Print.py`, `VerifyGuideTargets.py`, `VerifyOfficeNpcMarkers.py`, `VerifyLevel2Assets.py`, `VerifyMobileInput.py`, `VerifyHudCopyPickup.py`, `VerifyShopAndGuideView.py`; select only affected checks |
| Existing pack integrity | `Documentation/CampaignFoundation/verify_pack.py`; `Documentation/Level3TheNumbersTellAStory/verify_pack.py` and `verify_content.py`; these validate their own pack/fixture contracts, not this new initiative or live gameplay |
| Audio | `Tests/AudioRegression/README.md` and `.csproj`; latest L3 log reports a standalone harness compilation issue. Inspect before promising a run |

The installed Android SDK ADB path is `/Applications/Unity/Hub/Editor/6000.4.5f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`, outside `Unity.app`. The sandbox attempt to list devices could not open its local socket. The required escalation was declined; **no phone inventory, device performance, gameplay or save operation was performed**. Do not retry through another tool.

Existing tests frequently have tracked `Tests/*/bin` output. Future tickets must use disposable output and actual source builds for DLL-dependent checks. No broad gameplay suite is appropriate merely to author this Markdown pack.

## Authoring completion and implementation evidence gaps

The full 21-ticket pack, journeys/shared contracts, requirement coverage, dependencies and release checklist now exist after user approval. See TICKETS/COVERAGE/VERIFICATION and the actual PROGRESS validation record. The source-reference CSV and first-pass live-family audit are complete planning evidence; T02 must qualify actual active uses/imported costs before any migration. Per-chapter route-aware recording manifests are implementation deliverables, not existing complete coverage. Phone identity/profiling, approved new takes, applicable educator sign-off and visual/contact acceptance remain named gates, never satisfied by documentation.
