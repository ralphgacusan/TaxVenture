# T02 — Enter a safely specialized Level 3 scene with resumable chapter state

Status: **Implemented, internally verified; user manual pending**. Blocked by: T01.

Priority: P1. Phase: A — Chapter entry. Estimated scope: M+; one safe entry/resume slice; target one fresh agent context. If unexpected shared refactoring is required, document and split it before broadening this ticket.

## Goal and visible accomplishment

An isolated Level3 attempt enters the familiar office at reception, reads the appropriate next task and restores the same campaign state without starting old quests.

This is local Level3 numbering. Read [AGENTS](../AGENTS.md), [SPEC](../SPEC.md), [CONTENT](../CONTENT.md), [source map](../REUSE_AND_ARCHITECTURE.md), [presentation](../WORLD_AND_FEEDBACK.md), [rewards](../REWARDS_AND_VALIDATION.md), [verification](../VERIFICATION.md) and this ticket before execution. Implemented 30 October 2026 on an implementation instruction; evidence is in [PROGRESS](../PROGRESS.md), limitations in [KNOWN_ISSUES](../KNOWN_ISSUES.md).

## Existing systems to reuse

Verified existing paths relative to repository root; recheck live behavior rather than rely on stale comments:

- `Assets/Scenes/Office_Level2.unity`
- `Assets/Editor/Level2SceneSetup.cs`
- `Assets/Scripts/Data/OfficeLevelCatalog.cs`
- `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs`
- `Assets/Scripts/RewardsSystem/PlayerProgressData.cs`
- `Assets/Scripts/Level2/Level2Checkpointer.cs`
- `Assets/Scripts/FSM/GameStateMachine.cs`

New L3 components/resources below were outputs of this ticket; later tickets extend them rather than adding frameworks.

## Work and interaction mechanics

1. Follow SCENE_REUSE exactly: inspect current source/destination/hashes, copy saved Office_Level2 once into proposed Office_Level3 with distinct GUID, preserve source scenes/prefabs and never overwrite existing work. Record all inherited owners and serialized/runtime target bindings.
2. Add a versioned Level3Gameplay root and opted-in OfficeLevelCatalog mapping/capabilities. Disable only old quest/start/reward/checkpoint controllers identified by inspection. Preserve current first-person input, camera, lighting, services, audio and physical desk tools. Keep normal Career3 entry comingSoon; use disposable development fixtures.
3. Extend the existing signed checkpoint/command owner with SPEC's bounded two-case skeleton, active case identity and phases. Save/load the reception/task/practice admission state and preserve actual wallet/trust/Rosa history/settings/inventory/ownership. Extend Notes/folder/book scope deliberately; do not retire carried chapter tools accidentally.
4. Wire existing receptionist dialogue, OfficeObjectives, guide/Notes observers and coarse GameStateMachine mapping. First task is the calculator introduction. A resumed phase must not replay reception, old acquisition/rewards or prototype filing flow. Prevent legacy LevelComplete from firing after a single case.
5. Exercise save/reload/failed-save/mode isolation through real commands. Normalize only new L3 defaults when no L3 checkpoint exists; no reset/import of the user's actual save. Keep existing signed-save/future-version policy. Scene setup is idempotent; subscriptions/holds are scoped and released on disable.

## Assets, motion and feedback

Reuse saved scene/environment/player/camera/EventSystem/light/material references. New scene .meta and small Level3 configuration/builder only; no new world reconstruction.

Follow WORLD_AND_FEEDBACK and fixed document types. Bind every new world object to actual 3D body, collider/target, correct source/record identity and existing camera/input/audio ownership. Record resolved anchors/clip names and any unsupported asset assumption. No user scene-assembly chores or claims of rendered acceptance from declarations.

## Integration requirements

Runtime lives in `Assets/Scripts/Level3` and `Resources/Levels/Level_03`. Keep L1/L2 mapping and startup unchanged. Preserve both eventual case subrecords from the beginning, but implement only entry transitions in this slice. No second save/FSM manager.

Preserve current L1/L2 scenes, source prefabs/GUIDs, signed save owner, mode guards, input/FSM/dialogue/audio frameworks and Android controls. Normal L3 Career remains unavailable until T18. No L4 filing, ending, stand, drawer or global archive feature is added.

## Persistence and failure rules

Use the existing candidate→save→publish command boundary and stable chapter/case/action IDs. Validate actual prerequisites/revision/source before committing. Failed write leaves prior state, source/copy and retry available, with no success effect. Quiet restore does not raise acquisition/reward/choice/consumption events. Back, repeated input, disable and app focus interruption settle pointer state and release only owned holds. Story/replay/practice/Editor distinctions protect live campaign data.

## Internal checks — AI source/build/regressions only

- [x] Source L1/L2 immediate-baseline hashes remain unchanged; destination GUID is distinct; setup reruns without duplicates. (L1 `148D871C…62FE`, L2 `F5B8538E…19E7` before and after; Office_Level3 GUID `844c604a279e4b66ae226bf66f1dd99a`; one runtime root, none serialized: VerifyLevel3Scene PASS.)
- [x] Isolated L3 has one active owner per input/manager role and no inherited L1/L2 quest/reward starts. (Owner counts equal Office_Level2's, including its inherited second GameStateMachine component; Level 1 quest objects hidden, popup tutorial and desk folder flow off, Rosa parts never built, legacy completion states refused.)
- [x] Reception→practice task and save/resume round-trip use existing observers and exact carried campaign state. (Level3Regression: candidate/round trip equal except `campaign.levelThree`; quiet-restore triggers/lessons; history line read only.)
- [x] Failed writes and non-story attempts do not replace live campaign data; Career3 remains unavailable. (Injected temp-file failure leaves both save generations and the published state unchanged, retry succeeds; Editor/replay/practice/no-Chapter-2 never write; chapters.json unchanged.)
- [x] Run targeted behavioral/serialization/failure checks and affected existing regressions named in VERIFICATION for the actual changes. Build changed production paths before tests that load their DLL. Record exact commands/results/warnings; no Unity/editor launch or assumed pass count. (Commands and counts in PROGRESS; Level1LivingPlazaRegression keeps 4 pre-existing failures in untouched files.)

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] User confirms import, spawn/facing, actual movement/look/joysticks, lighting/doors/walls, no duplicate actors and safe reception camera.
- [ ] Inspect applicable actual imported references, 3D bodies, paper text, targets, motion, camera/control recovery and Android layout. Manual acceptance remains pending until the user reports it; static tests are not a substitute.

## Handoff and completion definition

Delivered: Office_Level3 (byte copy of the saved Office_Level2, new GUID); `Level3OfficeStart` (Level3Gameplay root v1, config-driven hide/disable, reception placement, history greeting filter); `Level3Checkpointer` (chapter entry/resume, AssignBea command, legacy guard, guide flags, settings exit); `Level3ChapterRules`; `Level3ContentValidator` with the `level3` case DTO; Level_03 level/start/objectives/rewards/guide and both case files; `campaign.levelThree` in the signed save; `Level3SceneSetup` (append-only build entry on import); Level3Regression and VerifyLevel3Scene. Bag/hotbar presentation in Level 3 is split out (KNOWN_ISSUES L3-18) and must land before T04 acquisitions.

Next: user imports and checks T02 in Unity, then T03 consumes the ToolPractice phase.

Direct dependents: T03, T04. Record actual changed source/content/assets, internal evidence, preserved baseline limitations and remaining user checks in [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md). Git operations and user acceptance remain user-owned.

## 30 October 2026 — Bag/hotbar continuation follow-up

Implemented/internal-verified; user manual pending. User override: every collected item stays across Levels1–3, regardless of usefulness. L3 now uses shared HudHotbar with its own ready/resume/blocked branch. Chapter entry no longer retires IDs, marked records or carried tools. Folder contents accumulate; older papers/player annotations have saved read-only pages, never current-case transfer or answer keys. Current-folder submission still requires the actual stored folder.

Direct L3 Editor testing previews a deep local copy only from a compatible completedL2 campaign; changes never write live possessions. Otherwise isolated inventory starts empty. Old already-deleted items/missing paper text are not fabricated. Career3 remains comingSoon. See PROGRESS for exact paths,245/245 L3 checks, previous-level regressions and user import/read/move/exit tests. No scene, actual save, Unity or Git operation occurred. Next remains T03.
