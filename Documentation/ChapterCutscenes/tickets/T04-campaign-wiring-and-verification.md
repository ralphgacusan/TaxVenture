# T04 — Wire chapter cinematics into real Levels 1–3

**Status:** Three endings and requested normal-entry opening playback implemented, internally verified; user manual pending. **Deferred:** real receiving Bayview gameplay, independently of opening playback. **Final ticket in this pack.**

## 6 November 2026 — later opening-entry amendment

The user reported that the Level 2/3 start cutscenes do not play when starting the levels and explicitly requested a fix. Normal fresh Story entry and career-board Replay now play the existing opening scenes, then load the existing Office_Level2/Office_Level3 gameplay. Mid-chapter Story checkpoints go directly to the office; pending endings keep priority. No opening flags, save writes, rewards or fake Bayview stages are added. Direct cinematic preview remains isolated. The original intro and Practice routes are unchanged; settings restart retains its existing direct office route.

This later instruction supersedes the original Bayview prerequisite/inactive-route requirements below **for opening playback only**. Bayview receiving gameplay remains deferred and is not claimed complete. The prior ending-only amendment is retained as history.

## 6 November 2026 — authorized scope amendment

The user's execution prompt explicitly defers the continuing case and authorizes completing all three eligible endings now. `chapter01.end`, `chapter02.end` and `chapter03.end` are wired before existing results. The existing Level 1 intro is unchanged. `chapter02.start` and `chapter03.start` stay standalone, unregistered and unrouted until their promised Bayview stages exist. No opening dialogue was rewritten to imply implemented gameplay.

Implementation, exact file inventory, save contract, current verification and manual route are recorded in [PROGRESS](../PROGRESS.md#t04--three-ending-integration-6-november-2026). Original acceptance items below describe the full scope and remain unchecked pending the two openings and actual Unity/device acceptance; they are not a claim that the bounded ending work is unfinished.

## Goal and observable result

Play the five authored scenes at their correct campaign moments, using the existing intro for Chapter 1's start. Natural completion and Skip return to the same intended continuation, once. Reload, replay, missing prerequisites and failed saves must not duplicate rewards, client delivery, chapter completion or cinematic playback loops.

This ticket owns wiring, not new Bayview mechanics, a new save/dialogue framework, Level 4–5, financial content, purchases or cosmetic redesign of the existing intro.

Read [production standard](../PRODUCTION_STANDARD.md), all three scene tickets, [story continuity](../../CampaignStory/CONTINUITY_AND_OPEN_DESIGN.md), and applicable CampaignFoundation/Level3/Intro AGENTS before editing their files. Reinspect live changes made by bug-fix work; dated user overrides and source win over stale examples here.

## 1. Prerequisite audit — do this before routes change

Record the actual entry/completion state and source call site for each row:

| ID | Required real context | Expected receiver |
|---|---|---|
| Existing L1 opening | Current New Game/Chapter 1 rules | Existing `Cutscene2.0` → Office_Level1 route, unchanged |
| chapter01.end | Marie's actual copy delivery/follow-through saved; post-service result continuation pending | Return to existing Chapter 1 result/acknowledgment then Career |
| chapter02.start | Legitimate first chapter entry; actual Bayview identity stage ready to receive player | Office_Level2 at its Bayview stage, then ordinary Rosa flow |
| chapter02.end | Rosa corrected, approved and actually delivered; CoastCare preparer identity established; existing chapter state respected | Existing Chapter 2 result/Career continuation |
| chapter03.start | Legitimate first chapter entry; actual Bayview income stage and retained L2 stage ready | Office_Level3 at its Bayview income stage, then ordinary Bea flow |
| chapter03.end | Bea approved, explanation and physical copy delivery saved; limited finding and mayor message acknowledged | Existing current Chapter 3 result with its shared Continue behavior |

### Missing prerequisites

Bayview stages are not established implemented at ticket creation. If absent, name the source/state missing, leave the affected start route inactive and finish unaffected routing/verification. Do not fake a checkpoint or write completion to satisfy the cinematic. Do not silently substitute Rosa/Bea as receiver while captions promise Bayview work. Standalone scenes remain available for user review.

The full ticket cannot be marked complete until all required live routes are truthful. This is a concrete external gameplay dependency, not permission to build the entire continuing case here. Similarly, a CoastCare label/message must be established naturally before an ending reinforces it; avoid silently turning a cutscene into the first unsupported accusation.

## 2. Inspect real source seams

The following are starting references, not guaranteed final patch locations:

- `Assets/Scripts/UI/MenuEntry.cs`: New Game/resume/Continue, current intro force-opening policy and `OpenLevelSelect` handoff.
- `Assets/Scripts/UI/Menu/CareerBoardPresenter.cs`, `CareerChapterSheet.cs`, `Assets/Scripts/RewardsSystem/CareerProgressService.cs`, `CampaignRun.cs`, `Assets/Resources/Career/chapters.json`: actual Story/MasteryReplay selection and chapter scene routing. Inspect all real launch paths, not only one button.
- `Assets/Scripts/UI/HUD/ClientCopyHandover.cs`, `CaseFinale` source, `Assets/Scripts/UI/LevelCompleteUI.cs`: delivered copy → finale/result sequencing and Continue action.
- `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs`: `CommitLevelOneWrapUp`, `CommitLevelTwoFollowThrough`, current L3 acknowledgment and signed candidate-write-publish.
- `Assets/Scripts/Level2/Level2OfficeStart.cs`, `Level2Checkpointer.cs`, `Level2CampaignRules` source and checkpoint data: L2 entry/restore/completion.
- `Assets/Scripts/Level3/Level3OfficeStart.cs`, `Level3Checkpointer.cs`, `Level3ClosingRules.cs`, `Level3ChapterRules.cs`: normal restore, message/finding prerequisites and pending result display. At inspection, `TickWrapUp` presents L3 result when ready and modal-free; it must not race the ending.
- `Assets/Scripts/UI/HUD/SceneFade.cs`, `HudRewardScreen`, existing camera/input/modal owners and `AudioManager`: clean ownership/pause/return.

Existing L1 chapter completion can occur at result acknowledgment; L2 completion is currently tied to follow-through; L3 has its own acknowledged result. **Do not force these into a new common domain lifecycle just to add cinematics.** New presentation state must work alongside their current transactions.

## 3. Route architecture and scene registration

Use scene names/IDs from [README](../README.md) as stable identities. Add only these new scenes to the build configuration once, preserving existing entries, GUIDs and indices. Do not rename/reorder original scenes or set the career chapter to an ending scene.

For openings, route first legitimate entry through the cinematic and then the actual receiving gameplay stage. Mid-level Continue/resume goes directly to the saved gameplay checkpoint unless an interrupted opening is explicitly pending. Avoid chapter launch recursion when the cinematic returns.

For endings, prefer **loading the dedicated cinematic additively while retaining the current gameplay scene**, then unloading only its owned set and resuming the existing pending result flow. This avoids destroying live LevelCompleteUI/checkpoint owners. Use another existing safe transition mechanism only if source inspection demonstrates equivalent result/resume behavior and documents why; do not reload a fresh office merely to show results.

During an additive ending:

1. Capture immutable read-only chapter/variant context after successful service commit.
2. Take existing compatible gameplay input/camera/UI/review-clock holds without triggering an independent pause menu.
3. Suppress conflicting result/finale presentation while the scene owns the screen; never suppress the underlying domain/save command.
4. Load the small dedicated set, select its camera, and ensure only one rendered cinematic camera/audio listener at the intended time.
5. Use T01 presentation finish contract; play on an owned clock that does not require globally freezing timeScale.
6. On natural/Skip completion, persist presentation consumption through existing owner before resuming the pending result; release all cinematic holds/objects/audio correctly.
7. Return to the previous gameplay active scene/camera/result context, never to an arbitrary object found globally.

Cinematic copies do not acquire gameplay components that bootstrap quests or resolve active actors. Existing persistent audio/UI/input managers must not duplicate when the additive scene loads. Cache scene references; no per-frame global scans.

## 4. Ending placement and existing finale reconciliation

Desired player order: **real service → required finding/message if any → new ending cinematic → existing result/Continue → Career**. If the current result lifecycle requires a narrow different insertion seam, document the precise sequence while preserving transactions and the same visible meaning.

- Do not start an ending at audit approval while a Client Copy is still pending.
- Do not call `CommitLevelOneWrapUp`, L2 follow-through or L3 acknowledgment from Timeline signals or Skip.
- Do not replay delivery, repeated emotional dialogue, the old animated finale and then a second nearly identical new finale. Preserve meaningful required gameplay/feedback; coordinate/suppress only overlapping presentation while the new cinematic owns it.
- L3's latest inspected requests use the shared conference-room finale/template and **Continue only**. Integrate one cinematic into that lifecycle, not a restored STAY IN OFFICE choice or a new result UI. Reinspect in case a later user decision changes the flow.
- Preserve actual L3 earned delivery trust tiers, persisted wrong-figure counts, delivered-copy inventory removal and other current bug fixes. Scenes read results; they do not recompute them.
- Endings do not auto-load the next chapter or unlock L4. The Nina teaser can exist while L4 remains Coming Soon.

## 5. Skip and viewing state

All new scenes have first-view Skip, matching the intro button's visual style. The original intro's existing first-view mandatory policy remains unchanged. New finish handlers must not write `levelOneIntroSeen` or use it as a gate for other scenes.

Use stable chapter/slot presentation markers in the **existing signed campaign/save owner** only as needed for pending/consumed state. Do not add another JSON file, PlayerPrefs ledger, cloud service, arbitrary wall-clock retry state or global singleton save system. A small additive schema extension is permitted for wiring if existing state cannot express the required pending handoff.

Important distinction: natural completion may record watched; Skip records handled/consumed for routing without claiming the player watched every line. An abort/interruption does not consume pending presentation. Do not persist every animation frame; restarting an interrupted short scene from zero is acceptable.

### Required behavior table

| Situation | Required behavior |
|---|---|
| Fresh legitimate chapter entry | Eligible opening once, then real receiving stage |
| Mid-chapter Continue | Restore actual saved work directly, no routine opening replay |
| App closes during an opening | Pending opening may restart at zero; never rerun completed gameplay/duplicate assignments |
| Actual service committed, ending not finished | Resume pending ending safely, then existing pending result |
| Natural end and Skip arrive together | One consumed transition and one continuation |
| Presentation-marker save fails | Keep retryable pending continuation and existing domain state; show a concise retry action, not a silent loop or duplicate award |
| Already consumed ending, result still pending | Resume result, not ending repeatedly |
| Existing completed campaign with no new markers | Do not replay every past ending on menu launch or reset progress. Infer only legitimate currently pending work from actual state; absence alone is not permission to replay history |
| Gameplay/cinematic scene unavailable | Preserve state, name/log exact missing dependency, recover to a usable UI; no endless black screen, fabricated success or lost pending result |
| App loses focus | Presentation/subtitles pause coherently; gameplay clock does not run; cleanup/restore remains safe |

If existing transaction boundaries cannot atomically distinguish pending ending from consumed-with-result-pending, design the smallest explicit state that can. Document the candidate/write/publish order and failure cases before patching; do not mark chapter complete early to avoid designing presentation state.

## 6. Run modes

- **Story:** real prerequisites, actual saved Rosa variant, presentation markers through existing signed owner. No new cinematic rewards.
- **MasteryReplay:** follow current chapter entry policy; any optional cinematic replay is local to the run. Cannot change campaign trust/choice/viewing/progression/possessions. Do not create a separate persistent replay system.
- **Practice/Training:** bypass these story cinematics; existing drill flow stays direct.
- **Standalone/direct Editor scene:** safe preview, explicit neutral or chosen sample variant, no real save reads/writes, gameplay acquisition or forced production load. End in a preview-finished state if there is no legitimate caller.

Reset/New Game follows existing owner rules for any newly added presentation flags, without changing audio preferences or resetting the user's actual save during testing.

## 7. Transition, audio and UI ownership

One fade owner per transition; do not stack a shared black curtain over an already fading local curtain so captions disappear or black holds double. Keep the new scene's authored interval 15 seconds and measure load latency separately. On Skip, fade from current alpha, cancel cosmetic cues, finish once.

No gameplay objectives/dialogue/reward panels under captions. Releasing a cinematic must restore exactly the prior valid camera/input/UI ownership, not blindly enable every disabled component. Avoid tap-through into Continue/interact after Skip. Restore ambience/music settings on every exit; no duplicate listener, muted office after return or overlapping intro/new-scene music.

## 8. Verification plan

Use existing test harnesses discovered in the repository. Run current affected route/save/dialogue/campaign/intro/L2/L3 checks appropriate to actual code changes; build current source before source-dependent DLL checks. Do not quote old counts as a new pass or write a huge suite for cosmetic curve edits.

Meaningful internal cases:

1. Route selection across new entry, saved mid-stage, pending ending, consumed ending/result pending, completed older campaign and isolated modes.
2. Natural finish/early Skip/last-frame Skip plus repeated callbacks produce one continuation and no duplicate grants.
3. Failure before/after the new presentation write leaves a reachable retry path and unchanged prior domain progress.
4. Correct Rosa context variant; neutral fallback for missing committed history; no guessing from heart total.
5. Endings cannot run before delivery/finding/message prerequisites; scenes cannot grant them.
6. Practice/Replay/direct Editor paths leave live campaign unchanged.
7. New scene registration/bindings/duration/subtitle bounds/metadata valid; all existing scenes/intro references preserved.
8. Bayview-absent builds do not expose a misleading start route and report the explicit external blocker.

### Ordered user route

1. Use a user-approved test campaign, not an AI-reset real save. Fresh L1: existing intro policy unchanged; Marie handover remains playable; new ending plays once; result/Continue/Career still work.
2. With real Bayview L2 stage available: opening then actual stage, accepted Auditor retention, Rosa flow. Test each committed Rosa history through the same ending shots. Skip cannot cancel the trust consequence or delivery.
3. With real Bayview L3 stage available: opening then actual income work; Bea activity, computation, review, explanation and actual copy delivery; finding/message; one ending; shared Continue-only result; L4 remains locked as intended.
4. Relaunch during each opening, after actual delivery, mid-ending and at pending results. Confirm work/possessions/claims and next tasks remain correct.
5. Test sound off, changed volumes, phone safe areas/aspect ratios, Android Back, early/mid/late Skip and rapid taps. Observe caption readability, stable camera handoff, no duplicate listeners/curtains and no loaded set left behind.
6. Verify Practice/Replay and direct cinematic preview cannot mutate the story.

User performs Unity import/Play Mode/device/touch/audio/performance checks. AI records internal evidence without launching Unity under current project rules.

## Acceptance checklist

- [ ] T01–T03 assets present, durations valid, referenced by stable IDs; existing L1 intro preserved.
- [ ] Each enabled route has the real narrative/gameplay prerequisites and correct continuation; absent external Bayview stages recorded as blockers, not fake completion.
- [ ] Starting/resuming/ending/Career flows respect existing chapter commit boundaries and unlock rules.
- [ ] Natural completion and Skip use one safe continuation; new first-view Skip works without changing original intro policy.
- [ ] Pending/consumed/failed-write behavior is persisted with existing owners and tested; no lost result or repeated service/rewards.
- [ ] Existing finales/dialogue/results do not race or duplicate the new cinematic; current L3 overrides retained.
- [ ] No changes to tax answers, monetary/trust formulas, client evidence, inventory ownership, optional purchases or L4/L5 availability.
- [ ] Build entries appended safely, no duplicate cameras/listeners/managers, no stuck fade/input/clock/audio state.
- [ ] Affected internal checks and exact file changes recorded; user visual/device acceptance separately reported.
- [ ] Whole ticket marked complete only when all five intended scenes are truthfully wired, including the opening receiving-stage dependencies.
