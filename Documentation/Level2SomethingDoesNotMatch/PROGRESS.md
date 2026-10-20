# Progress and handoff

Updated 2026-10-19.

## Current state

T01 and T02: T02 is user accepted. T03 to T12 are implemented and internally verified; the user tests them in Play Mode. All tickets are built; see "Final handoff" at the end.

| Ticket | Owner | Source/internal status | User Play Mode |
|---|---|---|---|
| 01 | Claude (session 2026-10-17) | Implemented — internally verified | Partly: user reached the empty cabinet (expected before T02) |
| 02 | Claude (session 2026-10-17) | User accepted | PASS (2026-10-17): drawer, flight to the HUD icon, drop-to-open on the desk, bottom page buttons and page text all work |
| 03 | Claude (session 2026-10-17) | Implemented — internally verified | Pending (tested together with T04) |
| 04 | Claude (session 2026-10-17) | Implemented — internally verified | Pending (tested together with T03) |
| 05 | Claude (session 2026-10-19) | Implemented — internally verified | Pending |
| 06 | Claude (session 2026-10-19) | Implemented — internally verified | Pending |
| 07 | Claude (session 2026-10-19) | Implemented — internally verified | Pending |
| 08 | Claude (session 2026-10-19) | Implemented — internally verified | Pending |
| 09 | Claude (session 2026-10-19) | Implemented — internally verified | Pending |
| 10 | Claude (session 2026-10-19) | Implemented — internally verified | Pending |
| 11 | Claude (session 2026-10-19) | Implemented — internally verified | Pending |
| 12 | Claude (session 2026-10-19) | Implemented — internally verified | Pending |

## Planning evidence

Read current Level1 gameplay/tutorial/reward docs and relevant source for case progression, cabinet, stamps, verdict, score, HUD/clock/finale bootstraps and save storage. Office_Level2 already has a build entry; Level_02 currently references two placeholder cases. Current verdict evaluates filled fields, cabinet archives, several runtimes target only Office_Level1, and false-issue audit results are a placeholder. These gaps are explicitly assigned to tickets rather than assumed finished.

Primary-source income-category research linked in CONTENT. Loan-specific Philippine primary authority is not established by that pass and remains a bounded content verification item. Exact tax rates and real filing dates are deliberately outside the level.

Documentation verification: PASS. Checked 22 Markdown files, 12 ticket templates, all local Markdown links, the dependency chain, income fixture totals, and worst-run passing bounds. Pack contains approximately 12,918 words before this verification note. No gameplay compile/regression tests were needed because this task changes documentation only.

## Next action

**Start T05 (hands-on toolkit) after the user's T03+T04 Play Mode result.** The review is in `Tutorial` once Rosa hands over the records (`CaseReviewState.HandoverDone`). The market desk is `Level2Gameplay/RosaStall/MarketRecordsDesk` (desk view via `MarketRecordsDesk.Viewpoint`), and the six record sheets lie in its tray. Add the toolkit as a new part (`Level2OfficeStart.EnsurePart`). Do not recopy the scene.

(Original T01 instruction, kept for history:) Start T01 by reading SCENE_REUSE.md. Inspect current saved Office_Level1 and destination state, create the new Office_Level2 once with a new GUID, preserve Office_Level2_OLD, then inventory inherited startup conflicts and verify course rules. Implement a one-case office start with explicitly opted-in shared presentation. Keep this pack as the entry point for future sessions.

## Per-ticket handoff template

- Ticket / owner / date:
- Behavior now playable:
- Files changed (exact paths):
- Assets built/reused and final transforms/anchors:
- Source/internal commands actually run and outputs:
- Acceptance criteria met / remaining:
- User Play Mode checklist and results (pending unless provided):
- Known limitations and content authority status:
- Next exact action / ticket:

Completion labels: `Planned`, `In progress`, `Implemented — internal checks pending`, `Implemented — internally verified; user Play Mode pending`, `User accepted`. Source-only completion is never visual acceptance.

## User asset selection — 2026-10-17

Documentation updated to require Rosa model SM_Chr_Business_Female_03 from TaxVentureWithCompliance/Characters and the red-and-white awning Building_4 (1) store from TownEnvironment/Env/Town. Verified both prefab files and named scene references exist. Updated source paths/GUIDs, placement/reuse rules, T04 acceptance, finale identity and superseded generic-stall/character-placeholder wording. No scene, prefab, animation or gameplay source edited. No Play Mode or git commands run.

## New scene/reuse decision — 2026-10-17

Latest inspection: Office_Level1.unity exists (4,642,931 bytes), previous Level2 is now Office_Level2_OLD.unity, and its current build entry points to _OLD. The new Office_Level2 path is absent at this inspection. Added SCENE_REUSE.md and synchronized T01/pack instructions: create fresh Level2 from one controlled copy of current saved Level1, share asset references, then specialize gameplay locally. No broad prefab/additive-scene migration; no overwrite on rerun. Earlier notes about an existing Office_Level2 GUID/build entry describe the earlier snapshot and are superseded by this decision. Documentation only; no scenes/build settings changed, no git or Play Mode commands run.

Scene-guidance documentation verification: PASS. All local Markdown links resolve across 23 documents; 12-ticket plan retained; CRLF normalized. New scene creation is specified but not executed.

## T01 handoff — 2026-10-17

- **Ticket / owner / date:** T01 Office start / Claude / 2026-10-17.
- **Behavior now playable (source):** Office_Level2 plays Level_02, with one case: Rosa, `L2-ROSA-001`.
  - The player starts in the reception facing the receptionist (Tutorial Hub drill placement, blend 0.45).
  - The HUD, minimap, guide and fade build there and load Level_02 content.
  - Arthur's Level 2 welcome card and two lessons play. The objectives are receptionist, then the market-client drawer (`event:CaseFolderRetrieved`), then the Auditor.
  - Level 1 parts are switched off before they start: plaza clients and Arthur (`Plaza/NPCS`), the corkboard side quest, the profiles HUD icon and the old popup `TutorialController`.
  - There is no side-quest gate and no shut doors.
  - Rosa's review state starts at `OfficeWelcome` with COVER/S1/N1 held, and moves to `RetrieveFolder` when the receptionist talk ends.
  - The book shows three Level 2 leaves after the shared sections.
- **Scene creation:**
  - Source: `Assets/Scenes/Office_Level1.unity`, sha256 `914f31ffd8f0f24fd22f075cdfaadc751a6f890eb43ac8c42b80d118c7fd63cf`, 4,771,471 bytes, Unity 6000.4.5f1.
  - Destination: `Assets/Scenes/Office_Level2.unity`, byte-identical copy, new GUID `00c1e5f98fce4a819da741bdd9d51819`.
  - Method: source-only fallback (editor was open on 3D Menu; Level 1 was not open or dirty). Unity import pending.
  - Level1 and `_OLD` hashes unchanged (`_OLD` sha256 `ace9d360...`, GUID `355dc2e2...`).
  - Lighting uses Unity's built-in default; there is no navmesh asset. No assets were duplicated.
  - Setup version 1 (`Level2OfficeStart.SetupVersion`).
- **Build/menu routing:**
  - `EditorBuildSettings.asset` adds Office_Level2 (enabled, new GUID) in the old slot. `Office_Level2_OLD` is kept but disabled.
  - Career catalog (`Resources/Career/chapters.json` and the `CareerProgressService` fallback): Level_02 is now "Something Does Not Match", 1 case, scene `Office_Level2`.
  - `Assets/Editor/Level2SceneSetup.cs` re-adds the build entry only if it is missing, and offers *Tools > TaxVenture > Level 2 > Check Scene Setup* (inventory only).
- **Files added:**
  - `Assets/Scripts/Data/{OfficeLevelCatalog,OfficeLevelProfile,CaseReviewState,CaseReviewValidator,LevelBookLeaves}.cs`
  - `Assets/Scripts/Data/Definitions/CaseReviewDefinition.cs`
  - `Assets/Scripts/Player/PlayerPlacement.cs`
  - `Assets/Scripts/Level2/Level2OfficeStart.cs`
  - `Assets/Editor/Level2SceneSetup.cs`
  - `Assets/Resources/Levels/Level_02/{rewards,objectives,office_start,book}.json` and `cases/case_rosa_001.json`
  - `Assets/Resources/Tutorials/Level_02/guide.json`
  - `Tests/Level2Regression/*`, `Tests/VerifyLevel2Scene.py`
  - all `.meta` files with fresh GUIDs
- **Files changed:**
  - `Assets/Scenes/Office_Level2.unity(.meta)` (new)
  - `ProjectSettings/EditorBuildSettings.asset`
  - `Assets/Resources/Levels/Level_02/level.json`
  - `Assets/Resources/Career/chapters.json`
  - `Assets/Scripts/RewardsSystem/CareerProgressService.cs`
  - `Assets/Scripts/Data/{CaseProgressionManager,CaseData,CaseFactory,ObjectiveTriggers,Definitions/LevelDefinition,Definitions/CaseDefinition,Loading/JsonCaseLoader}.cs`
  - `Assets/Scripts/Core/GameplayEvents.cs`
  - `Assets/Scripts/UI/{OfficeMinimap,TaxCodeBookUI}.cs`
  - `Assets/Scripts/UI/HUD/{OfficeHudRuntime,SceneFade,OfficeObjectives}.cs`
  - `Assets/Scripts/UI/Guide/{GuideRuntime,GuideDirector}.cs`
  - `Assets/Scripts/Training/PracticeModeRuntime.cs` (placement moved to `PlayerPlacement`; behavior unchanged)
  - `Tests/SettingsRegression/Program.cs`
- **Commands run and results:**
  - `python Tests/make_scratch_build.py` + `dotnet build Temp/ScratchBuild/Scratch.csproj`: 0 errors, 29 warnings (baseline).
  - Editor scratch build of `Level2SceneSetup.cs`: 0 errors, no C# warnings.
  - `Level2Regression`: 145 PASS.
  - Controls: 5734 PASS. Dialogue: 799 PASS. Progression: PASS. Guide: 714 PASS. CareerBoard: 69/69. Settings: 93/93 (after updating the stale Office_Level2 check). Training: 513/513.
  - `VerifyLevel2Scene.py`: PASS. `VerifyGuideTargets.py`: PASS. `VerifyOfficeNpcMarkers.py`: PASS.
  - `VerifyInteractionOutlines.py`: FAIL on `Mobile_Renderer.asset` (pre-existing, unrelated; L2-18).
- **Not run (user-owned):** Unity import, Play Mode, visuals, device, git.
- **User Play Mode checklist:**
  1. Open Unity and let it import. Run *Tools > TaxVenture > Level 2 > Check Scene Setup*.
  2. Play Office_Level2 directly, and from the career board Chapter 2. Check:
     - a fade-in, a single HUD, the Level 2 welcome card and Arthur's lines;
     - the player stands in the reception facing the receptionist;
     - no plaza clients, Arthur or corkboard; no profiles icon; office doors open;
     - the receptionist gives Rosa's greeting, and the tracker then shows the drawer step with arrows to the file storage room;
     - the book has three Level 2 leaves at the end.
  3. Play Office_Level1 once to confirm it is unchanged.
- **Known limitations:** KNOWN_ISSUES L2-13 to L2-19.
- **Next exact action:** T02.

## T02 handoff — 2026-10-17

- **Ticket / owner / date:** T02 Retrieve and inspect / Claude / 2026-10-17.
- **User result carried over:** In T01 Play Mode the user reached the cabinet and found nothing to do. That was expected, and T02 builds the drawer.
- **Behavior now playable (source):**
  - Until the folder is retrieved, the desk Case Folder is hidden with the existing `CaseFolder3DDrag.HideAndDisableFolder()`.
  - A labeled "MARKET CLIENTS / R. Santos · Stall 04" drawer is built on `Filing_Cabinet_1`.
  - Before the receptionist talk, tapping the drawer shows "Talk to the receptionist first." through `LevelGate.NotifyBlocked`.
  - After the talk, the prompt reads "Take Rosa's folder". Tapping it:
    1. commits the retrieval once: `CaseReviewState.TryRetrieveFolder()` moves RetrieveFolder to PreliminaryReview and raises `CaseFolderRetrieved`;
    2. puts the desk folder back with `RestoreAndEnableFolder()` (same desk pose, no clone);
    3. animates: the drawer slides out 0.20 m in 0.35 s, the folder lifts 0.22 m in 0.30 s with the paper sound, flies toward the view and shrinks over 0.35 s, and the drawer closes in 0.35 s. Player controls are held during the motion.
  - Later taps say "Rosa's folder is already on your desk."
  - The next objective, "Sit at your desk, open Rosa's folder and read...", completes on `state:ReviewDocumentsState`. That state is entered on the first folder open, by tap or by drag.
  - Arthur gives short hints after retrieval and when the folder opens.
  - D1–D6 stay out of reach because they are not in the case documents.
- **Drawer anchor (runtime):**
  - Path: `Level2Gameplay/CaseRetrieval/MarketClientDrawer`.
  - It goes on the cabinet side with the most free space in front of it (raycast up to 3 m). Height: 62% of the cabinet height, clamped to 0.55–1.1 m above its base.
  - Front: 0.80 × cabinet width, clamped 0.30–0.60 m, by 0.20 m high, sitting 0.012 m proud of the face. One BoxCollider on the cabinet's layer (3, the interactor mask).
  - The Console logs the exact position and facing. If they look wrong, record them here and tune `Level2CaseRetrieval` constants.
- **Materials:** four runtime URP Lit colours, created once and destroyed with the part. No assets were added.
- **Files added:**
  - `Assets/Scripts/Level2/Level2CaseRetrieval.cs` and `MarketClientDrawer.cs`
  - their `.meta` files, which Unity generated with unique GUIDs while it was open
- **Files changed:**
  - `Assets/Scripts/Level2/Level2OfficeStart.cs` (builds CaseRetrieval; SetupVersion 2)
  - `Assets/Scripts/Data/CaseReviewState.cs` (`FolderRetrieved`, `TryRetrieveFolder`)
  - `Assets/Resources/Levels/Level_02/objectives.json`
  - `Assets/Resources/Tutorials/Level_02/guide.json`
  - `Tests/Level2Regression/Program.cs`
- **Not changed:** `FileCabinetInteractable` (archive only in ArchiveCaseState), the Level 1 scene and Level 1 data.
- **Commands run and results:**
  - Scratch build: 0 errors, 29 warnings (baseline).
  - Level2Regression: 177 PASS.
  - Controls 5734, Dialogue 799, Progression, Guide 714, CareerBoard 69/69, Settings 93/93, Training 513/513: all PASS.
  - VerifyLevel2Scene: PASS. VerifyGuideTargets: PASS.
- **Not run (user-owned):** Play Mode, visuals, device, git.
- **User Play Mode checklist:**
  1. Tap the drawer before talking to the receptionist: you should see the reminder.
  2. Talk to the receptionist, follow the arrows, and tap the drawer. Check the drawer slide, the folder lift and the drawer close; the label should be readable.
  3. Check that the drawer front sits on the right side of the cabinet and doesn't clip.
  4. The tracker should move to the desk step, and the folder should now be on the desk.
  5. Open the folder by tap and, on another run, by drag. Both should complete the step. Read the cover, S1 and N1.
  6. Leave the desk and sit again: still one folder. The book still works.
- **Known limitations:** KNOWN_ISSUES L2-20 to L2-22.
- **Next exact action:** T03.

## T02 fix — Rosa's pages (2026-10-17)

- **User Play Mode result:**
  - Working: taking the folder from the drawer, the folder appearing on the desk, opening it, and Arthur's hints.
  - Not working: the pages could not be turned or read.
- **Cause:** `CaseFolderUI` builds only one page, Taxpayer Information. `SupportingDocumentsPageLink` lists documents only on page index 5, which no longer exists. So case documents had no way to open, in Level 1 as well (Level 1 never needed them).
- **Fix:**
  - New part `Level2Gameplay/PacketPages` (`Assets/Scripts/Level2/Level2PacketPages.cs`). While Rosa's folder is open at the desk (the `OpenCaseFolder` object is active) and the folder has been retrieved, a HUD-styled **ROSA'S PAGES** stack appears at the left: Case Cover, Prepared May Income Summary, Referral Note.
  - Each 64 px tab opens or closes that page in the existing `DocumentViewerManager` paper window, and a green check shows once the page has been read.
  - Only pages the review state holds are listed, so D1–D6 appear only after T04 hands them over. Leaving the folder closes the open page windows.
  - It is presentation only and changes no review state.
- **Data:**
  - `ReviewDocumentDefinition.documentName` links each review page to its case `documents` entry.
  - `CaseReviewValidator.ValidateDocumentLinks` runs on load and rejects missing or duplicated links.
  - Rosa's case JSON fills in all three links. Arthur's hint now says the pages are on the left.
  - SetupVersion is now 3.
- **Not changed:** `CaseFolderUI`, `SupportingDocumentsPageLink`, `DocumentViewerManager` and Level 1.
- **Commands run and results:**
  - Scratch build: 0 errors, 29 warnings.
  - Level2Regression: 189 PASS.
  - All seven existing suites: PASS. VerifyLevel2Scene: PASS.
- **User re-test:**
  1. Open Rosa's folder at the desk. Check that the ROSA'S PAGES stack shows on the left and doesn't cover the open folder.
  2. Tap each tab: the page window opens, and tapping again closes it. Check the text is readable and the check mark appears.
  3. Close the folder: the tabs and page windows go away.
  4. Open the folder again from the HUD folder icon: the tabs appear there too.

## T02 rework — user direction (2026-10-17)

- **User feedback:**
  - The ROSA'S PAGES side tabs (opening a separate window) were not wanted.
  - Rosa's pages must be real pages of the case folder, continuing page 1's style and turned like the Tax Code Book.
  - The retrieved folder must animate into the HUD folder icon (0 → 1).
  - It must not be readable or openable from the icon. The player enters desk view and drops it on the desk, and dropping it anywhere else does nothing.
- **Folder pages:**
  - `CaseFolderUI.BuildPages` adds one page per held review page after Taxpayer Information: Case Cover, Prepared May Income Summary, Referral Note.
  - Each page prints its case-document fields as page-1 rows (same row prefab, same container, same style), and reading a page marks it reviewed.
  - The existing page-edge tap zones and page-turn animation turn pages, and the edge arrows now show like the Tax Code Book's.
  - A case without review data still has one page, so Level 1 is unchanged.
  - New optional field: `CaseFolderPageContent.Document`.
- **Handoff:**
  - The drawer animation flies the folder to the HUD Case Folder icon. On arrival, or at once if cut short, `Level2CaseRetrieval.StoreRetrievedFolder()` calls the existing `FolderToHUDIcon.StoreFolderFromExternalSource()` (count 0 → 1).
  - New part `Level2Gameplay/FolderHandoff` (`Level2FolderHandoff.cs`) wraps the icon's tap and drop actions while `CaseReviewState.AwaitingDeskPlacement`:
    - tapping the icon shows "Sit at your desk, then drag Rosa's folder onto it." and opens nothing;
    - a drop while not in desk view shows the same reminder;
    - a drop in desk view but off the desk shows "Drop Rosa's folder on your desk.";
    - a drop on the desk calls `ResetToUnstored()` (count 1 → 0) and `CaseFolder3DDrag.RestoreAndEnableFolder()`, and records `folderPlacedOnDesk`.
  - After that the folder icon behaves exactly as in Level 1.
  - `AuditorSubmissionTray.SubmissionBlocked`, a new optional hook that Level 1 leaves unset, refuses a hand-in until the folder has been on the desk.
- **Removed:** `Level2PacketPages.cs` (and its meta).
- **SetupVersion:** 4.
- **Design note:** the ticket said retrieval should stay separate from the final hand-in. It still is: retrieval only stores the folder in the icon, and the Auditor refuses it until it has been put on the desk.
- **Commands run and results:**
  - Scratch build: 0 errors, 29 warnings.
  - Level2Regression: 200 PASS.
  - All seven existing suites: PASS. VerifyLevel2Scene: PASS. VerifyGuideTargets: PASS.
- **User re-test:**
  1. Take the folder from the drawer: it flies to the folder icon, and the count shows 1.
  2. Tap the icon while standing: you get the reminder, and nothing opens.
  3. Drag it out while standing: you get the reminder.
  4. Sit at the desk and drag it off the desk: "Drop Rosa's folder on your desk."
  5. Drag it onto the desk: the closed folder lies on the desk and the count shows 0.
  6. Open it. Turn pages with the right and left edges: Taxpayer Information, then Case Cover, Prepared May Income Summary, Referral Note. They should be in the same style as page 1, with arrows only where there's another page.
  7. Try the Auditor before putting it on the desk: refused.

## T02 polish — user direction (2026-10-17)

- **Drop opens the folder:** a valid drop on the desk now also calls `CaseFolder3DDrag.BeginExternalDrag/EndExternalDrag(drop point)` and then `OpenDraggedFolder()`, so the folder opens right where you released it (the same path as a desk drag-open). This also enters ReviewDocumentsState.
- **Page buttons at the bottom:**
  - `CaseFolderUI.CentreContentColumn` (the world-space folder paper only) puts the previous and next page tap zones in the bottom corners: 130 × 130, 70 in from the sides and 10 up, level with "Page n / m". Each arrow is centered in its zone.
  - Level 1's single page shows no arrows, so it looks the same.
- **Page text fits like page 1:**
  - The row prefab is a fixed 50 pt with wrapping, so the long values were wrapping into tall rows.
  - Rosa's case documents are now page-1-sized: at most 5 rows, labels ≤ 18 characters and values ≤ 22, all checked by Level2Regression.
    - Cover: Client, Works as, Residence, Earns from, Other months.
    - May Income Summary: Alterations, Equip. rental, May subtotal, Memo not income (Loan LN-0510 ₱20,000), Prepared with (Assistance Desk).
    - Referral Note: Please confirm, Reference, Ask, Original record.
  - Headings are no longer than "Taxpayer Information".
  - Number rows now pass real amounts (`CaseFolderUI.TryParseAmount`), because the row formats numbers with `Convert.ToSingle` and would fail on "₱12,000".
- **Commands run and results:** scratch build 0 errors, 29 warnings. Level2Regression: 224 PASS. All seven existing suites: PASS. VerifyLevel2Scene: PASS.
- **User re-test:**
  1. In desk view, drag the folder from the icon onto the desk: it opens immediately.
  2. The arrows are at the bottom corners, and tapping them turns pages.
  3. Each of Rosa's pages fits on the paper at page 1's text size.

## T03 handoff — 2026-10-17

- **Ticket / owner / date:** T03 Preliminary finding / Claude / 2026-10-17 (built back to back with T04 at the user's request; the T03 internal gate passed before T04 started).
- **Behavior now playable (source):**
  - After retrieval, the case folder gets a last page, **Findings**, in page 1's rows and style:
    - **Concern** box: takes any line of a held summary page (S1).
    - **Related record** box: takes any line of a held note or record (N1; later the D records).
    - **Reason** box: takes one of three **Choice** rows (Not on summary, Amounts differ, Maybe not income).
  - Lines move with the existing tap-to-pick / tap-to-place, or drag on the same page (`FindingSlot`). The boxes accept whole groups, so a fitting box never hints at the answer.
  - When all three boxes are filled, the finding is recorded (`FindingRecorded`, once), its status is "Needs checking", and the review moves to PreliminaryStamp.
  - Stamping with the desk stamps: `StampUI.StampOverride` (a new optional hook, null in Level 1) sends the stamp to `Level2PreliminaryStamp`.
    - A supported **Not Ready** (concern on S1, record N1, reason Not on summary) is recorded once as `review.preliminary` (verdict, finding, reason, packet revision). The review moves to MarketVisit and `PreliminaryHoldStamped` fires.
    - The stamp folder shows an "Initial review: Not Ready" row and red ink "INITIAL REVIEW / NOT READY" (`ReviewInkMark`). The Findings page becomes a read-only note with the same ink.
    - Any other try gets Arthur's guided line (`PreliminaryReview.Message`) and is not recorded, scored or rewarded.
    - The case assessment, filing status, game state, audit and completion are never touched.
  - Arthur's lessons: "A filled page can still need checking...", a first-step demonstration, then "your turn" hints for the finding and the stamp.
  - Level 2 messages now go through Arthur's bubble (`Level2Say`), because the HUD toast is hidden at the desk.
- **Data:**
  - `review.reasons` and `review.preliminaryRule` (answer data, never printed), both validated on load.
  - `CaseReviewState.findings`, `packetRevision` and `preliminary`.
  - New events `FindingRecorded` and `PreliminaryHoldStamped` (objectives and guide).
- **Files added:**
  - `Assets/Scripts/Data/PreliminaryReview.cs`
  - `Assets/Scripts/UI/ReviewInkMark.cs`
  - `Assets/Scripts/Level2/{FindingSlot,Level2FindingsPage,Level2PreliminaryStamp,Level2Say}.cs`
  - their `.meta` files
- **Files changed:**
  - `CaseReviewDefinition.cs`, `CaseReviewState.cs`, `CaseReviewValidator.cs`
  - `GameplayEvents.cs`, `ObjectiveTriggers.cs`, `OfficeObjectives.cs`, `GuideDirector.cs`
  - `CaseFolderUI.cs` (ExtraPages / PageBuilder level-page hook), `CaseFolderPageContent.cs`
  - `StampUI.cs` (StampOverride), `StampCaseFolderUI.cs` (initial review row and ink)
  - `Level2OfficeStart.cs`, `Level2FolderHandoff.cs`, `MarketClientDrawer.cs` (messages via Level2Say)
  - Rosa case JSON, objectives, guide, Level2Regression
- **Internal gate before T04:** scratch build 0 errors, 29 warnings. Level2Regression: 288 PASS. All seven existing suites: PASS. VerifyLevel2Scene and VerifyGuideTargets: PASS.

## T04 handoff — 2026-10-17

- **Ticket / owner / date:** T04 Rosa's stall / Claude / 2026-10-17.
- **Built at runtime under `Level2Gameplay/RosaStall`:**
  - **Originals** (referenced, never modified) via `Resources/Levels/Level_02/scene_assets.asset` (`Level2SceneAssets`):
    - Rosa: `Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Business_Female_03.prefab` (GUID f78a881d…)
    - Store: `Assets/Flat_Style_Vehicles/Prefabs/Buildings/Building_4.prefab` (GUID 4c701139…)
    - Rosa's idle: `Assets/3D Models/npc_cora_static/npc_cora_static@Standing Idle.controller` (GUID 0b1fbdea…; default state Standing Idle; humanoid)
  - **Why the T-pose:** Rosa's prefab Animator points at a controller GUID (3197541a…) that is not in the project. Her avatar (Synty Character.fbx) and the Cora clips are both Humanoid, so the controller retargets onto her.
  - **Store** (`RosaStore`):
    - Instance overrides of Building_4 (1) in TownEnvironment: scale (−7.19, 7.19, 7.19) under Env scale 0.04, rotation y 180.54, no material or component overrides.
    - The office town is scaled 0.7, so the TownEnvironment world scale doesn't carry over. Instead the store keeps the X mirror and its proportions, and is fitted to a 3.8 m shop height (half-width capped at 5 m).
    - Its front is the side that overhangs more at mid height than at the base (the awning), found by ray-casting its own mesh collider. An offline read of 4.FBX also found a single overhanging side.
  - **Spot search:**
    - Rings 12–24 m around `Environment/Outside/Plaza`, every 15°, front facing the plaza.
    - Each spot needs ground within 0.6 m of plaza height, no solid collider in the store volume or the 3.2 m apron in front, and at least 2.5 m clearance from the reception-doors-to-plaza route.
    - The spot nearest that route wins, with a logged fallback if none passes.
    - The Console logs the final position, yaw, scale, footprint and front; copy them here after the first run.
  - **In front of the store:**
    - Rosa (capsule collider, faces the plaza, turns to the player while talking)
    - `MarketRecordsDesk` (1.4 × 0.7 m, 0.78 m top, BoxCollider, tray, cloth bundles, envelope with six D1–D6 sheets; `DeskViewpoint` 1.3 m up on the plaza side)
    - `StallSign` reading "ROSA'S ALTERATIONS / Stall 04 · Plaza Market"
- **Behavior:**
  - **Before the initial stamp:** Rosa greets you only, and the desk says "Check Rosa's folder at the office first."
  - **During MarketVisit:** the required conversation from CONTENT plays in a fixed order, because the project has no dialogue choices (rule R4). When it completes, a single commit:
    - `TryCollectHandover(D1–D6)` adds the six records to the review, which moves to Tutorial;
    - `AwardConversation("rosa_santos")` gives one heart through the ledger;
    - `RosaRecordsCollected` fires;
    - the envelope slides, its flap opens and the six sheets fan into the tray.
    - A cancelled conversation gives nothing.
  - **After:** the records are case-folder pages D1–D6 (page 1's style, at most 5 rows each). The desk enters the normal desk view (`EnterFirstPerson`), and `MarketDeskOpened` fires once.
  - **Objectives and arrows:** initial stamp, then Rosa, then the records desk.
- **Records** (values cross-checked by Level2Regression):
  - D1 collection sheet: 9,000 bank + 3,000 cash = 12,000
  - D2 bank statement: BT-0905 9,000 + LN-0510 20,000 = credits 29,000
  - D3 commission invoice CM-0526: 5,000
  - D4 rental receipt RA-0520: 3,000
  - D5 loan agreement LN-0510: 20,000, repaid monthly
  - D6 cash receipt RC-0531: 3,000, included in the May collection sheet
- **Files added:**
  - `Assets/Scripts/Level2/{Level2SceneAssets,Level2RosaStall,RosaInteractable,MarketRecordsDesk}.cs`
  - `Assets/Resources/Levels/Level_02/scene_assets.asset`
  - the `.meta` files
- **Files changed:**
  - `CaseReviewDefinition.cs` (handoverDocumentIds, StallDefinition), `CaseReviewState.cs` (TryCollectHandover, TryOpenMarketDesk), `CaseReviewValidator.cs`
  - events/triggers/objectives/guide wiring
  - `Level2OfficeStart.cs` (SetupVersion 6)
  - Rosa case JSON (D1–D6, stall lines), objectives, guide
  - `Tests/VerifyLevel2Scene.py` (runtime targets listed), Level2Regression
- **Source scenes and prefabs unchanged (sha256 prefixes):** TownEnvironment cab54383…, TaxVentureWithCompliance 04b00a43…, Office_Level1 914f31ff…, SM_Chr_Business_Female_03.prefab 752d635d…, Building_4.prefab 23e83fd5…
- **Commands run and results:**
  - Scratch build: 0 errors, 29 warnings.
  - Level2Regression: 414 PASS.
  - Controls 5734, Dialogue 799, Progression, Guide 714, CareerBoard 69/69, Settings 93/93, Training 513/513: all PASS.
  - VerifyLevel2Scene: PASS. VerifyGuideTargets: PASS.
- **Not run (user-owned):** Play Mode, visuals, device, git.
- **User Play Mode checklist (T03 + T04 together):**
  1. Open Rosa's folder and turn to the last page, **Findings**. Tap a May summary line, turn to Findings and tap **Concern**. Do the same with a referral note line into **Related record**, then a **Choice** into **Reason**. Arthur's hints should guide each step, and the tracker should move on.
  2. Try a wrong stamp first (Ready, or a wrong reason): Arthur explains, and nothing is recorded. Then stamp **Not Ready**: the red "INITIAL REVIEW / NOT READY" ink and the "Initial review" row appear, and Findings becomes a read-only note.
  3. Follow the arrows out of the office to Rosa's stall. Check that the store sits beside the plaza (not on the path), the awning faces the plaza, and the scale looks right next to Rosa.
  4. Rosa should be idling, not in a T-pose. She turns to you while talking.
  5. Talk to her: the conversation plays, then the heart reward appears, the envelope slides and the six sheets fan into the tray. Talk again: only a short reminder.
  6. Open your folder (HUD icon): pages D1–D6 follow the Findings page. Check they're readable.
  7. Tap the records desk: you sit in desk view, and the tracker step completes. Exit the desk.
  8. Copy the Console line "[Level2] Rosa's stall: store at …" into this file.
- **Next exact action:** T05 after the user's result.

## T03 fix — stamp set missing (2026-10-17)

- **User Play Mode result:**
  - Steps 1–13 work: Findings boxes filled (Concern: May subtotal, Related record: Reference, Reason: Not on summary), and Arthur moved to the stamp hint.
  - Problem: there was no stamp set on the desk.
- **Cause:** `Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/Stamps` (StampSetInteractable, layer 6) is inactive in Office_Level1, and therefore in its copy. Level 1's current flow no longer stamps at the desk, and nothing in code switches it on.
- **Fix:** `Level2PreliminaryStamp.Start()` switches that object on in Level 2 only. Its scene links (StampCaseFolderUI, StampTransitionController) were already wired, and desk view taps layer 6 (WorkstationInteractor mask 64).
- **Results:** scratch build 0 errors. Level2Regression: 415 PASS.
- **User re-test:** continue from checklist step 15 (tap the stamp set on the desk).

## T03 rework — desk stamping on the real folder (user direction, 2026-10-17)

- **User Play Mode result:**
  - The stamp set was now visible.
  - Tapping it played the stamp animation out of camera view.
  - Dragging a stamp put it behind the case folder (Level 1's old setup: a separate stamp folder panel plus a stamp workspace off camera).
- **User direction:**
  - The stamp can be dragged and dropped anywhere on the front page of the case folder, and stamps exactly there (Ready or Not Ready).
  - Tapping the stamp set turns the folder to page 1 with a page-turn animation.
  - Improve the animations and mechanics.
- **Done (Level 2 only):** new part `Level2Gameplay/DeskStamps` (`Level2DeskStamps`, `Level2StampDrag`, `Level2StampSet`).
  - It replaces the stamp set's old StampSetInteractable (destroyed on the Level 2 instance) and turns off the old Stamp3DDrag components.
  - Tapping the stamp set at the desk with the folder open:
    - `CaseFolderUI.TurnToPage(0)` leafs back page by page (0.14 s flips with paper sound);
    - both physical stamps lift (0.45 s arc) to float spots beside the folder (viewport x 0.86, y 0.64 / 0.36, 85% of the paper distance), handles toward the camera.
  - **Dragging:** the stamp follows the finger, hovering 5 cm above the paper plane, rubber face down. It can never go behind the folder.
  - **Release on the front page (page 1):** align 0.12 s, then press 0.10 s, then impact (paper sound). `Level2PreliminaryStamp.Stamp(type, paper point, random tilt ±12°)` makes the single decision. Then rebound 0.15 s and back to the float spot 0.25 s.
  - **Release elsewhere:** the stamp glides back. On another page, Arthur says to turn back to page 1.
  - **Putting the stamps away:** tap the stamp set again, close the folder or leave the desk.
  - **Ink:** a recorded initial hold stores where it landed (`preliminary.hasInk/inkX/inkY/inkAngle`). The front page draws "INITIAL REVIEW / NOT READY" right there through the new `CaseFolderUI.PageDecorator` hook (null in Level 1). The Findings note shows "Initial review: Not Ready (front page)".
  - **Wrong tries:** no ink. Arthur's guided line explains.
  - Grab colliders use layer 7 "3DInteractable" (in the camera pointer mask, not the desk tap mask) and exist only while the stamps are lifted.
- **Files:**
  - Added: `Assets/Scripts/Level2/Level2DeskStamps.cs`, `Level2StampDrag.cs`, `Level2StampSet.cs` (+ metas).
  - Changed: `CaseFolderUI.cs` (TurnToPage, PageDecorator, PaperRect, CurrentPageIndex, IsWorldPaper, IsShowing), `ReviewInkMark.cs` (CreateAt), `CaseReviewState.cs` (ink fields), `Level2PreliminaryStamp.cs` (static Stamp API), `Level2FindingsPage.cs` (front-page ink decorator), `Level2OfficeStart.cs` (SetupVersion 7), guide hint, Level2Regression.
- **Results:** scratch build 0 errors, 29 warnings. Level2Regression: 429 PASS. All seven existing suites: PASS. VerifyLevel2Scene: PASS.
- **User re-test (checklist steps 15–19):**
  1. At the desk with Rosa's folder open, tap the stamp set: the folder flips back to page 1, and both stamps float up beside it, in view.
  2. Drag **Ready** onto the front page and release: it presses down, Arthur says not yet, and no ink appears.
  3. Drag **Not Ready** anywhere on the front page: it presses down, and the red ink appears exactly there.
  4. Drop a stamp off the paper: it glides back.
  5. Tap the stamp set again: the stamps return to the stand.

## T03 polish — game-feel desk stamping (user direction, 2026-10-17)

- **User Play Mode result:**
  - The folder flipped to page 1.
  - One stamp appeared oversized and twisted in the bottom-right corner, because the float spot depended on the model's own scale and orientation.
  - Asked for a better, more game-like and polished stamp system.
- **Now:**
  - **Float column:** just right of the paper (viewport x = paper's right edge + 0.07, clamped 0.70–0.90; Ready at y 0.62, Not Ready at y 0.30) at 95% of the paper depth.
    - Each stamp is resized so it's 20% of the screen tall, whatever the model's scale, and gets its original scale back on the stand.
    - It stands upright on screen, tipped 15° toward the viewer, with its front turned to the camera.
    - A READY (green) / NOT READY (red) name tag floats above it, and it bobs gently.
  - **Pick up:** scale pop 1.1 and a click.
  - **Drag:** smooth follow, tilted 30° toward the screen top so its body shows, hovering 25% of its height above the paper. A 32%-opacity ghost imprint (the exact text and ink colour it would print) follows on the front page.
  - **Release on the front page:** straighten 0.08 s, slam 0.07 s (ease-in), impact (squash 0.88, paper thump 0.985, Paper2 sound), the decision, then rebound 0.15 s and home 0.25 s.
    - A recorded stamp's ink pops in (1.35 to 1 over 0.18 s).
    - A rejected press leaves a faint mark that fades (hold 0.35 s, fade 0.6 s).
  - **Release elsewhere:** glides home.
- **Files:** `Level2DeskStamps.cs` and `Level2StampDrag.cs` rewritten; `ReviewInkMark.cs` (SetStyle, InkRed/InkGreen, PopNext); new `Assets/Scripts/UI/ReviewInkMotion.cs` (+ meta).
- **Results:** scratch build 0 errors, 29 warnings. Level2Regression: 434 PASS. All seven existing suites: PASS. VerifyLevel2Scene: PASS.
- **User re-test:** checklist steps 15–19. Check the stamp size, position and tags, the ghost while dragging, and how the slam and ink feel.

## T03/T04 fixes — stamp visuals and freeze (user Play Mode, 2026-10-17)

- **User Play Mode result:**
  - The stamps looked bad (the desk's own models read as thin sticks; the READY tag floated apart from them).
  - After a press, the stamp froze on the paper.
  - The ink was not visible on the desk folder (it did show later in the HUD folder, small).
  - Market desk: sat down and nothing happened. That's expected: T05 adds the toolkit there.
  - The rest of the T03/T04 flow worked ("I was able to do what you said").
- **Freeze cause (found in source):** `ReviewInkMotion.Play` used `GetComponent<T>() ?? AddComponent<T>()`. In the Unity Editor a missing component comes back as a fake null object, so `??` skips the AddComponent and the next line throws.
  - It ran at the moment of a successful stamp (ink pop). The exception killed the press coroutine (stamp stuck on the paper) after the hold was recorded but before the folder redraw (no ink on the desk folder).
  - Fixed with explicit null checks there and in `FindingSlot`. A Level2Regression check now forbids `??` after GetComponent in the Level 2 stamping files.
- **Hardening:** all stamp motion runs on unscaled time. The commit is wrapped so the press always finishes. `Level2PreliminaryStamp.Stamp` guards the Arthur line, the folder redraw and the event listeners separately, and redraws before the listeners run.
- **New visuals:** clear procedural hand stamps (`Level2StampDrag.Create`): wooden neck and grip, coloured cap, a coloured block printed READY or NOT READY on both faces with a brass trim, and an inked rubber pad.
  - The desk's original stamp models hide while their hand stamp is up and come back on return.
  - The stamps grow out of the stand into the float column (17% of screen height), with their label facing you.
  - Separate floating tags are removed.
- **Ink:** 440 × 160 paper units (was 330 × 120), text up to 64, thicker outline.
- **Market desk:** the record-sheet labels lying flat were upside down from the desk view; fixed. Arthur now says: "Here are Rosa's six records. You can read each one in your folder, after the Findings page."
- **Results:** scratch build 0 errors, 29 warnings. Level2Regression: 442 PASS. All seven existing suites: PASS. VerifyLevel2Scene: PASS.

## T03 fix — stand model and stamp grab (user Play Mode, 2026-10-17)

- **User Play Mode result:**
  - The hand stamps look good and should also be the stamps on the desk stand.
  - The stamps could not be dragged (they stayed put).
- **Stand:** in Level 2, the hand stamps now rest on the stand in place of the desk's original models.
  - Same spot, height from the original's bounds (0.05–0.25 m), standing on its base, label toward `Desk/DeskViewpoint`.
  - The originals stay hidden and come back if the part is removed.
  - Lifting rises from the stand; returning lands back on it at stand size.
- **Grab cause:** the grab relied on the EventSystem physics raycast, and at the desk other raycast targets (for example the open folder's world-space canvas around the paper) can answer first.
- **Grab fix:** `Level2DeskStamps.PollPointer` reads the pointer directly with legacy Input (first touch, else the mouse, as the desk's own taps do) and hit-tests only the lifted stamps' grab colliders (`Collider.Raycast`). It then drives `BeginDrag` / `DragTo` / `EndDrag`. The stamps no longer implement EventSystem pointer interfaces.
- **Results:** scratch build 0 errors, 29 warnings. Level2Regression: 445 PASS. Controls, Dialogue, Guide and Training suites: PASS.

## T03 fix — grab area, aim pose, put-away (user Play Mode, 2026-10-17)

- **User Play Mode result:**
  - Dragging works, but the press had to land beside the stamp, not on it.
  - While aiming, the stamp couldn't be seen; it should show its 3D side. The aiming and the ink are good: keep them.
  - Asked for a way to send the stamps back to the desk.
- **Grab:** `Level2StampDrag.ScreenHit` projects the stamp's current renderer bounds to the screen, with a margin of max(24 px, 2.5% of the screen height), and picks the nearest. It no longer uses a physics collider, which can lag behind moved transforms until the next physics sync.
- **Aim pose:** over the paper, the stamp's axis leans 52° toward the top of the screen. Its pad points at the aim point (0.45 stamp-heights away), so the body, block and label show. On release it straightens over the aim point and slams as before. The aim point, ghost and ink placement are unchanged.
- **Put away:**
  - A PUT STAMPS AWAY pill (EXIT DESK style, bottom right, 330 × 84) shows while the stamps are out.
  - After a stamp that counts, both stamps go back on the stand after 0.6 s.
  - Tapping the stand, closing the folder and leaving the desk still put them away too.
- **Files:** `Level2StampDrag.cs`, `Level2DeskStamps.cs`, new `PutAwayButton.cs` (+ meta), Level2Regression.
- **Results:** scratch build 0 errors, 29 warnings. Level2Regression: 448 PASS. Controls and Guide suites: PASS.


## T03 follow-up — removable Findings sheet (2026-10-18)

- **Owner/status:** Codex; implemented — internally verified; user Play Mode pending. This is the requested T03 presentation improvement; T05 remains next.
- **Behavior:** Findings is now a cream working paper with a printed heading, margin, tape, subtle paper stack/shadow and the existing source/destination rows. Tap its heading/blank paper or drag it to lift it from the page. The same sheet stays outside the folder while other pages are read; its fields still accept the existing tap transfer and value drags. Clicking it participates in FloatingWindow/HudWindowFocus/Doc3DLayer's shared order with Notes, the folder and the book.
- **Return:** Drag the sheet's heading/blank area and release the pointer inside any visible folder paper. The footer highlights compatible return placement, the folder flips to Findings, and the sheet arcs/scales home before an attached copy redraws from the same CaseReviewState. A book/Notes covering the folder blocks a drop through it. Dropping elsewhere keeps the paper loose and within screen reach.
- **Lifecycle:** Release the overlay from page ownership before lifting, so turning pages cannot delete it. Detached rows are refreshed independently from folder pages. Closing the folder hides the loose paper and reopening restores it; settings also hides it. A cancelled return recovers the loose sheet. Case changes and level teardown dispose it. No domain answer, score, revision or save schema is added by moving paper.
- **Files changed:** `Assets/Scripts/Level2/Level2FindingsSheet.cs` (+ fresh `.meta`), `Assets/Scripts/Level2/Level2FindingsPage.cs`, `Assets/Scripts/Level2/FindingSlot.cs`, `Assets/Scripts/UI/CaseFolderUI.cs`, `Assets/Resources/Tutorials/Level_02/guide.json`; this initiative's PROGRESS, SPEC, KNOWN_ISSUES and T03 ticket.
- **Checks:** scratch C# build: 0 errors, 29 pre-existing warnings; Level2Regression: 448 PASS. Controls: 5,734 PASS; Dialogue: 799 PASS; Guide: 714 PASS; Training: 513 PASS; Progression: PASS; Settings: 93 PASS; Career: 69 PASS; Intro: 29 PASS. VerifyLevel2Scene: PASS (existing warning that Level1 has changed since its original copy); VerifyGuideTargets: 17 paths, none missing. The initial no-restore build needed generated restore assets; normal scratch build succeeded. Guide's temporary save replacement was denied inside the sandbox and passed when rerun with authorized access.
- **Manual checks pending:** lift by heading/blank paper; compare summary/referral pages beside the loose sheet; tap and drag values into all three boxes; switch front layer between sheet, folder, book and Notes; drop onto Findings and another page; close/reopen folder and settings; repeat detach/reattach; confirm read-only content after preliminary stamp. Check text size, framing, tape/peel/return motion and touch feel on device. No Unity editor or Play Mode was launched.
- **Next:** user verifies this follow-up and the T03/T04 flow; then T05 hands-on toolkit. No Git commands run for this change.


## T03 follow-up — larger raised paper and detach-first editing (2026-10-18)

- **User direction:** loose Findings paper should feel more 3D, have bigger paper/text, and accept no value transfers while attached.
- **Implementation:** detached footprint is 672 x 896 reference units (previously 480 x 640), with 34-unit field type (minimum 29, previously 25/minimum 20), larger headings/footer, a resting X/Y/Z tilt, movement lean, deeper cast shadow and lit/shaded paper edges. The folder retains its fitted attached preview. Peel, cancelled-return recovery and reopening all restore the enlarged loose pose; filing still scales to the original attached fit.
- **Editing:** attached field CanvasGroup blocks source selection, value dragging and destination taps/drops. Tap/drag its heading or paper to lift it first. Destinations additionally gate CanAccept and mutation on the actual sheet's CanTransfer state (detached, active, not returning). Returning disables fields and clears only selections originating from this sheet, preventing an attached sheet's reason from remaining selected for transfer.
- **Files:** `Assets/Scripts/Level2/Level2FindingsSheet.cs`, `Assets/Scripts/Level2/Level2FindingsPage.cs`, and this initiative's PROGRESS/SPEC/KNOWN_ISSUES/T03 ticket.
- **Internal verification:** scratch build 0 errors, same 29 existing warnings. Level2Regression: 448 PASS; ControlsRegression: 5,734 PASS. Source review checked attached pointer blocking plus destination guards, larger bounds/clamping, and enlarged pose restoration paths. Unity Play Mode remains user-owned.
- **User checks:** attached fields cannot receive a selected value or drag; tap paper to detach, then all three fields accept normal taps/drops; check larger text, depth/tilt, side-by-side placement, and repeated return/reopen sizing on your screen. Next remains T05 after user acceptance of T03/T04.


## Business wording decision — Rosa's Laundry (2026-10-18)

- **Owner/status:** Codex; documentation update complete. User requested documentation only. Historical implementation logs above retain their original wording.
- **Approved direction:** Rosa's Laundry; laundry payments 12,000; payment for sending customers to another shop 5,000; spare washing machine rental 3,000; same repayable loan 20,000. Keep all IDs, evidence links, totals, rewards, scoring, character/store assets and ticket order.
- **Files changed:** README.md, CONTENT.md, SPEC.md, WORLD_AND_FEEDBACK.md, tickets/T02-retrieve-and-inspect.md, tickets/T04-rosa-stall.md, tickets/T07-office-reconciliation.md, KNOWN_ISSUES.md and PROGRESS.md, all within this initiative.
- **Scope:** no Assets, scenes, runtime JSON or scripts edited; no Git commands or Unity testing. The Findings statement/field redesign was discussed but is not included in this wording approval.
- **Verification:** checked current wording consistency, unchanged fixed totals/evidence mapping, local Markdown links and CRLF. No gameplay build required for documentation only.
- **Next:** runtime laundry text/signage migration awaits a separate user request. Existing T03/T04 manual acceptance and T05 remain tracked above; this documentation change does not complete them.


## T03 findings comparison redesign — implementation (2026-10-18)

- **Owner:** Codex, current session. User authorized a three-drop findings sheet, book-based reason sources and general tips, explicit game-like discrepancy validation, and animated handwritten conclusions.
- **Touched areas planned:** preliminary finding data/rules, Findings page/sheet, optional Level 2 book leaves, guide content, regression tests and this initiative's handoff documents. Keep existing paper focus/drag/return, detach-first editing, IDs, scene assets, rewards and later ticket boundaries.
- **Next:** implement/test a pure comparison rule; wire comparison feedback and book sources. Unity Play Mode remains user-owned.


### Findings comparison handoff — 2026-10-18

- **Status:** Implemented — internally verified; user Play Mode pending. This supersedes the earlier three-box auto-completion and on-sheet reason-choice instructions.
- **Playable source:** detach the paper; identify a summary page, transfer a supporting statement, and take a Review Principle from the book. Compare records gives visible incomplete/unsupported/supported status. Only a supported explicit comparison raises FindingRecorded. A correct observation writes itself in the Notes handwriting font; edits reset confirmation and the old note. Return/reopen restores confirmed text without restarting the reveal. Stamps enforce the same meaningful-statement rule and require confirmation. Comparing never changes amounts, awards rewards or stamps a case.
- **Exact runtime files:** new `Assets/Scripts/Data/FindingComparison.cs` (+ unique `.meta`); changed `Assets/Scripts/Data/CaseReviewState.cs`, `Assets/Scripts/Data/PreliminaryReview.cs`, `Assets/Scripts/Data/Definitions/CaseReviewDefinition.cs`, `Assets/Scripts/Level2/Level2FindingsPage.cs`, `Assets/Scripts/Level2/Level2FindingsSheet.cs`, `Assets/Scripts/Level2/FindingSlot.cs`, `Assets/Resources/Levels/Level_02/book.json`, `Assets/Resources/Levels/Level_02/cases/case_rosa_001.json`, `Assets/Resources/Tutorials/Level_02/guide.json`.
- **Tests/docs:** `Tests/Level2Regression/Program.cs`; this pack's README, SPEC, CONTENT, WORLD_AND_FEEDBACK, REWARDS_AND_VALIDATION, TICKETS, tickets/T03-preliminary-finding, KNOWN_ISSUES and PROGRESS.
- **Internal checks actually run:** `python Tests/make_scratch_build.py`; `dotnet build Temp/ScratchBuild/Scratch.csproj` (and final no-restore build): 0 errors, same 29 pre-existing warnings. `dotnet run --project Tests/Level2Regression --no-restore`: **470 PASS**, including 22 additional checks for meaningful evidence, incorrect/incomplete combinations, explicit confirmation, idempotence, invalidation, round-trip persistence and book principle contracts. Controls 5,734 PASS; Dialogue 799 PASS; Training 513 PASS; Progression PASS; Guide 714 PASS; Settings 93 PASS; Career 69 PASS; Intro 29 PASS. Guide's temporary save replacement was blocked inside the sandbox and passed with authorized access. VerifyLevel2Scene PASS (one existing warning about Level1 edits since the original copy); VerifyGuideTargets: 17 targets, none missing. One-off validation: 24 local documentation links, CRLF/JSON, fresh unique script GUID and evidence keys matching the actual source document all PASS.
- **Manual checks:** attached fields remain noninteractive; detached sheet has exactly three fields; both new book pages fit and each principle supports tap/drag; incomplete and random combinations do not advance the guide; a valid combination shows the handwritten paragraph and advances once; edit then attempt stamping (must require a new supported comparison); repeat detach/return/reopen/settings and check focus, readability and reveal timing. No Unity editor or Play Mode was launched by Codex.
- **Scope/next:** existing paper system, stable IDs, amounts, rewards, selected models/store and scenes are preserved. Laundry wording remains a separately pending runtime content migration. User acceptance of these T03/T04 changes is next; T05 remains the next implementation ticket. No Git commands run.


## T03 compact automatic findings polish — implementation (2026-10-18)

- **Owner:** Codex. User requests two concise book leaves, no level-number headings, a smaller desk-aligned slip, automatic three-field validation, minimal feedback copy, and working tap/drag transfers with shared aiming/drop feedback. This supersedes the explicit Compare button design.
- **Scope:** Findings source/UI/rules, Level 2 book/guide copy, bounded opt-in per-point paper raycast handling in shared FloatingWindow/HudWindowFocus, targeted regression and this initiative's docs. Preserve detach-first editing, amounts, IDs and paper return/focus. User retains Unity/Git checks.
- **Diagnosis:** FindingSlot removes the normal transfer feedback; HudWindowFocus blocks an entire paper for any overlapping higher 3D document. These source paths plausibly explain missing feedback and unresponsive uncovered fields; actual Unity pointer checks remain user-owned.


### Compact automatic Findings polish handoff — 2026-10-18

- **Status:** Implemented — internally verified; user Play Mode pending. Latest user direction supersedes the explicit Compare button, five added book leaves, long field labels and oversized slip.
- **Behavior:** the slip uses Page / Clue / Reason, a one-sentence introduction and automatic checks after field edits. Partial drafts are neutral; unsupported links show No link yet; supported links show Needs checking plus the short handwritten note. Attached fields remain blocked. Existing meaningful-evidence and stamp gates, amounts, IDs and rewards remain intact.
- **Presentation:** detached footprint is approximately 564 x 639 reference units (was 672 x 896); paper orientation derives from the source folder relative to the camera, with only slight drag lean. The book adds exactly Reading Records and Review Notes, totaling 69 body words, no level-number headings, and the same three stable reason IDs. Guide bubbles and supplementary stamp messages are shorter.
- **Transfer fixes:** FindingSlot retains ValueTypeValidationFeedback and uses its shared accepted/rejected pulses/DropFeedbackFX. It also reuses hover cues. Slot instances are reused while values update so the badge/pulse can finish. FloatingWindow/HudWindowFocus has a default-off per-point filtering opt-in used by this slip, both for focus and raycasts. A higher overlapping 3D book only blocks the covered pointer location, preserving exposed destinations for the existing DropAimIndicator and tap/drag placement. Actual Unity pointer routing remains unverified by Codex.
- **Exact runtime paths:** `Assets/Scripts/Level2/{Level2FindingsPage,Level2FindingsSheet,FindingSlot}.cs`; `Assets/Scripts/Data/{FindingComparison,CaseReviewState,PreliminaryReview}.cs`; `Assets/Scripts/UI/HUD/{FloatingWindow,HudWindowFocus}.cs`; `Assets/Resources/Levels/Level_02/book.json`, `Assets/Resources/Levels/Level_02/cases/case_rosa_001.json`, `Assets/Resources/Tutorials/Level_02/guide.json`. Tests: `Tests/Level2Regression/Program.cs`. Current design/content/validation/T03 docs and this log updated within this initiative only.
- **Verification actually run:** scratch generator and build: 0 errors, same 29 baseline warnings. Level2Regression **474 PASS** (including source wiring guards for feedback/per-point coverage and content limits). Controls 5,734 PASS; Dialogue 799 PASS; Training 513 PASS; Progression PASS; Guide 714 PASS; Settings 93 PASS; Career 69 PASS; Intro 29 PASS. Guide temp save replacement was blocked in the sandbox and passed on authorized rerun. VerifyGuideTargets: 17 targets, none missing. One-off checks: all 24 local doc links resolve; edited sources use CRLF; JSON parses; exactly two book leaves/69 body words/no level-number headings. No scenes, source assets, packages or new script GUIDs were needed. No Git commands or Unity Play Mode run.
- **User checklist:** check matching paper angle and compact readability; click a source then each slot; drag source values from folder/book while watching the aim frame; test accepted and wrong-kind badges; touch/overlap the book and verify exposed fields still work; fill a bad complete link then a supported one; edit to invalidate; repeat file/reopen/settings and initial stamp handoff. Source checks do not replace these UI checks.
- **Next:** user acceptance of T03/T04, then T05. Runtime laundry wording migration remains separately pending; this change polishes the Findings interaction rather than claiming all Level 2 is complete.


## T03 concrete total/payment finding and desk slant — internally verified (2026-10-18)

- **Owner:** Codex. User requests a stronger backward-leaning slip with matching desk-paper feel, always-stacked labels/values, a simpler paid-laundry-job omission, a specific May total as the entry, a payment statement as the clue, the existing book reason, and brief Arthur/book hints.
- **Decision:** Entry to check = May total; clue = a paid blanket-washing job; reason = Unlisted payment. Preserve case/evidence IDs, semantic keys, amounts and game flow. Runtime laundry content is now in scope, replacing the prior referral scenario; the second paid job remains separate from the ordinary clothes collection sheet.
- **Next:** tighten the rule to the total semantic key, simplify case/records/dialogue coherently, then polish the live slip geometry and stacked typography. No Git or Unity Play Mode commands.

- **Completed:** runtime Rosa's Laundry, separate paid blanket job and clothes-only daily list; specific May-total concern guard; stacked captions in all states; backward pitch and tapered sheet/control geometry; short book principle and indirect Arthur introduction. Preserve IDs, amounts, stamps, rewards and transfer/focus systems.
- **Exact source files:** `Assets/Scripts/Data/Definitions/CaseReviewDefinition.cs`, `Assets/Scripts/Data/FindingComparison.cs`, `Assets/Scripts/Data/PreliminaryReview.cs`, `Assets/Scripts/Level2/{FindingSlot,Level2FindingsPage,Level2FindingsSheet,Level2RosaStall,Level2PreliminaryStamp,RosaInteractable}.cs`; new `Assets/Scripts/Level2/FindingsPaperPerspective.cs` and unique `.meta`. Content: `Assets/Resources/Levels/Level_02/{book.json,cases/case_rosa_001.json}`, `Assets/Resources/Tutorials/Level_02/guide.json`. Tests: `Tests/Level2Regression/Program.cs`. Updated current design/content/validation/ticket/handoff docs within this initiative.
- **Verification:** `python Tests/make_scratch_build.py` (397 files); `dotnet build Temp/ScratchBuild/Scratch.csproj` (0 errors, 29 baseline warnings); Level2Regression 485 PASS; Controls 5,734 PASS; Dialogue 799 PASS; Training 513 PASS; Progression PASS; Guide 714 PASS; Settings 93 PASS; Career 69 PASS; Intro 29 PASS. Guide temp-file replacement required authorized sandbox escalation, then passed. VerifyLevel2Scene PASS with the existing Level1-copy drift warning; VerifyGuideTargets 17 paths, none missing. No Git commands or Unity Play Mode.
- **Manual pending / next:** user verifies stronger backward lean against both folder/book, text fit with captions always above values, wrong-row rejection then total/payment/reason success, handwriting timing, drag and tap transfer/aim feedback, overlap focus, repeat detach/return and Arthur's book hint. T03/T04 user acceptance remains pending, then T05; no whole-level completion claim.

- **Final static checks:** 24 local documentation links resolve; Level 2 JSON and guide JSON parse; exactly two added book leaves with 74 body words and no level-number headings; edited C# files use CRLF. New perspective-script GUID is unique under Assets. Final scratch rebuild repeats 0 errors/29 baseline warnings and Level2Regression repeats 485 PASS.


## T03/T02 paper, stamps and cabinet polish — implemented; user Play Mode pending (2026-10-18)

- **Owner:** Codex. User reports clipped handwriting, mismatched attached/loose paper transforms, offset stamp grabs, unstable stamp size/rotation and an overlay drawer front that does not fit the authored cabinet. Scope: Findings layout/transition, Level 2 stamp picking/drag/feedback, cabinet presentation; preserve rule/state/rewards and Level 1.
- **Next:** trace projection and transform ownership before editing; internally build/regress; user performs Unity visual/feel checks.

- **Fixes:** the handwriting block doubles from 98 to 198 units with font locked at 28, extending the paper and footer. Lift size follows the attached projection (+6 percent), preserving its plane with a slight peel; return blends from the current pose. Stamps keep camera-plane depth/pose while held, preserve the grabbed point, use a drag threshold (tap only focuses), and apply uniform depth compensation during a press. One scale owner composes the smaller impact pulse; no pickup/press scale contention. Model bounds and projected hit rectangles are cached, excluding text; the shared outline and matching pointer surface provide visible, accurate interaction without dragging papers underneath. Pause/modal and canceled-touch guards avoid accidental submission. The drag plane is ahead of the nearest paper corner. A default-off Doc3DLayer option lets held stamps cover overlapping composited documents. The runtime cabinet front is thin, dark and sized to one authored drawer row.
- **Exact files:** `Assets/Scripts/Level2/Level2FindingsSheet.cs`, `Level2StampDrag.cs`, `Level2DeskStamps.cs`, `Level2CaseRetrieval.cs`; new `Assets/Scripts/Level2/Level2StampHitTarget.cs` plus unique `.meta`; narrowly extended `Assets/Scripts/UI/HUD/Doc3DLayer.cs`; `Tests/Level2Regression/Program.cs`. Updated this initiative's SPEC, WORLD_AND_FEEDBACK, REWARDS_AND_VALIDATION, KNOWN_ISSUES, T02 and T03 ticket checklists and PROGRESS only. No scene/prefab/content/reward/state files changed.
- **Evidence:** the three new source guards first failed on the old stamp/paper implementation. The revised source and pure depth-size invariants pass. Scratch generator includes 398 files; final build is recorded below. Existing domain tests remain intact; UI/3D contact and performance cannot be verified by these tests. New script GUID is unique; edited text is CRLF; all 24 local documentation links resolve.
- **User checks / next:** full handwriting at target resolution, matching paper size/angle at detach/return (including early return), stamp center/edge/touch grabs, outline and focus over book/notes/folder, steady shape through press/rebound, ink landing, supported/rejected/tap-only/canceled/paused/close behavior, repeated reopen and drawer alignment/open/close. User Play Mode acceptance of T02/T03/T04 remains pending; then T05. No Unity editor or Git command run.

- **Final verification:** `python Tests/make_scratch_build.py` → 398 files; `dotnet build Temp/ScratchBuild/Scratch.csproj --verbosity quiet` → 0 errors, 29 baseline warnings. `dotnet run --project Tests/Level2Regression/Level2Regression.csproj --verbosity quiet` → 497 PASS; ControlsRegression → 5,734 PASS; DialogueRegression → 799 PASS; ProgressionRegression → PASS. Visual/feel/performance checks remain user-owned.


## Desk follow-up — implemented; user Play Mode pending (2026-10-18)

Owner: Codex. User reports remaining handwriting clipping, stand-stamp highlight/hit and lifted occlusion problems, book grab/open-centre drift, conflicting automatic page turns, hint overlap and unsupported-link retry friction. Scope: these desk interactions; preserve rules/rewards/Level 1 defaults where optional. Next: repair measured layout, capture/pose and animation lifecycle, then internal checks. User retains Unity and Git checks.

- **Implemented:** real-font preferred-height layout at fixed size 28, shared paper/footer growth; desktop settle; clear unsupported editable drafts and preserve stamped records. Replacement stamp stand affordance, expanded projected handle/body target, same-frame toggle guard, and always-visible isolated rendering without received shadows. Auto page-turn cancellation/index/midpoint/current-page lifecycle and delayed stamp lift. Book lifted-plane finger tracking separated from safe rest placement; open/closed visible centres aligned. Arthur hint slides upward in desk/profile-reading mode.
- **Exact files:** `Assets/Scripts/Data/CaseReviewState.cs`; `Assets/Scripts/Level2/{Level2FindingsPage,Level2FindingsSheet,Level2StampDrag,Level2DeskStamps,Level2StampSet}.cs`; `Assets/Scripts/UI/CaseFolderUI.cs`, `Assets/Scripts/UI/HUD/Doc3DLayer.cs`, `Assets/Scripts/UI/Guide/GuideCoachView.cs`, `Assets/Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs`, `Assets/Scripts/Interactables/DeskMotion.cs`; `Tests/Level2Regression/Program.cs`. Current initiative docs/tickets updated. No scene, prefab, font, JSON content, new GUID, reward or final-assessment changes.
- **Verification:** scratch generator 398 files; build 0 errors/29 baseline warnings. Level2Regression 507 PASS; GuideRegression 714 PASS (sandbox temp-save replacement failed, authorized rerun passed); Controls 5,734 PASS; Dialogue 799 PASS; Progression PASS; VerifyGuideTargets 17 paths/none missing. Direct domain tests cover retry clearing/idempotence and stamped immutability; source guards cover presentation wiring. No Unity/Git commands.
- **Next/user acceptance:** check all reported screenshots in Unity: entire handwritten conclusion at target resolution; actual stamp outlines and handle/body touches on stand, visible aim/press, book finger and opening centre, auto/manual flips and canceled/repeated requests, hint travel, unsupported retry and free paper settle/filing. Device frame pacing with both stamps out remains unmeasured. Then resume existing ticket order (T03/T04 acceptance, T05).

The loose paper retains a minimum 0.94 visual scale when the measured note makes the fitted folder preview taller. This prevents extra writing room from indirectly shrinking the detached handwriting; the attached preview still fits its folder.


## Stamp tool-plane visibility and aiming repair — implemented; user Play Mode pending (2026-10-18)

Owner: Codex. User still sees the ink preview without the stamp while aiming, and mispositioned lifted stamps. Initial hypothesis that Camera.CopyFrom omitted the transform was disproved by Unity documentation; no such defect is claimed. The previous plane was positioned relative to the paper, leaving the tool behind foreground desk geometry, and pressing moved the model back into paper/world depth. Scope: move floating/dragging/pressing to a near-camera tool plane, align the render viewport and capture after movement, and add a readable pad-over-preview pose. Preserve stamp rules and input ownership. User owns Unity/Git checks.

- **Final change:** stamp home/drag/press/rebound stay in the near-camera tool plane; actual ink/decision remain at the real paper-local point. Slots raised to 0.69/0.45 viewport height; aiming and pressing partially align toward the paper normal with a visible pad above the preview. Rendering executes after tool motion and maps the render texture to the main camera viewport.
- **Investigation correction:** Unity documents that Camera.CopyFrom already copies its transform. The initial transform hypothesis and its redundant assignment/assertion were removed after checking https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera.CopyFrom.html. The failed source guard was not evidence of a runtime defect.
- **Files:** `Assets/Scripts/Level2/Level2DeskStamps.cs`, `Assets/Scripts/Level2/Level2StampDrag.cs`, `Assets/Scripts/UI/HUD/Doc3DLayer.cs`, `Tests/Level2Regression/Program.cs`; current initiative SPEC, WORLD_AND_FEEDBACK, KNOWN_ISSUES, T03 and PROGRESS. No assets/scenes/GUIDs/rules/rewards changed.
- **Verification:** final scratch build 0 errors/29 baseline warnings; Level2Regression 509 PASS (the earlier 510 included the subsequently removed transform guard); ControlsRegression 5,734 PASS. Source guards cover capture ordering/viewport and angled pad-over-preview aiming; pure/domain rules still pass. No Unity/Git commands.
- **Next:** user checks both complete stamp models, both handles, all folder aiming regions, visible angle/contact/press/rebound, correct ink and wrong/correct stamp outcomes, layered book/notes interaction, repeated put-away/reopen and device frame pacing. These remain rendered/feel checks, not established by internal tests. Then existing T03/T04 acceptance and T05 order.


## Stamp viewport capture and tap pickup follow-up — implemented; user Play Mode pending (2026-10-18)

Owner: Codex. User still loses the held model while aiming and needs dragging rather than a tap to open the desk stamps. Scope: actual viewport bounds, held front order, stable isolated render layers and EventSystem tap pickup. Preserve stamp validation/ink and user-owned Unity/Git checks.

- **Changes:** open-only pickup reconciles custom press-down with legacy touch-release world taps; EventSystem stand targets and current Input System edges support a stationary tap. Full-model pixelRect/near-clip fitting with coach clearance, held/press front-order retention without scale pop, stable renderer isolation on unused layers 29/30 and restoration on cleanup. Existing ink coordinates, validation, scoring, rewards and ordinary document defaults remain.
- **Files:** `Assets/Scripts/Level2/Level2DeskStamps.cs`, `Level2StampDrag.cs`, `Level2StampHitTarget.cs`, `Level2StampSet.cs`; `Assets/Scripts/UI/HUD/Doc3DLayer.cs`; `Tests/Level2Regression/Program.cs`. Current initiative SPEC, WORLD_AND_FEEDBACK, KNOWN_ISSUES, T03 and PROGRESS updated. No new assets/GUIDs, scene, prefab, TagManager or content edits.
- **Verification:** `dotnet build Temp/ScratchBuild/Scratch.csproj --verbosity quiet` → 0 errors/29 baseline warnings; `dotnet run --project Tests/Level2Regression/Level2Regression.csproj --verbosity quiet` → 524 PASS; ControlsRegression → 5,734 PASS. New pure tests cover both viewport edges, oversized centering and three offset/letterboxed viewport ranges. Source guards cover tap/open-only routing, stable render isolation and held front order. These tests do not verify rendered visibility. No Unity/Git commands run.
- **Next/user checks:** stationary tap on both handles; no open/close double trigger; complete model during aim at all page edges/top and during press/rebound; indicator/ink consistency where visual bounds are constrained; focus against book/notes, touch/mouse, repeat put-away/reopen, pause/cancel, desk/scene exit and frame pacing. T03/T04 acceptance remains pending, followed by T05.


## Folder tap centering and shared stamp capture/contact — implemented; user Play Mode pending (2026-10-19)

Owner: Codex. User asks for centred folder-button placement with unchanged entrance animation, matching Ready highlight, Not Ready visibility equal to Ready, and exact pad-to-indicator/ink contact. Scope: folder endpoint, one shared tool compositor, shared highlight refresh, projective pad anchor during press/pulse. Preserve state/rewards; user owns Unity and Git checks.

- **Changes:** folder button centres the visible paper on initial placement and reopening; explicit drag placement and entrance animation remain. Both stamps share one capture on stable layer 29 and refresh outline targets. Pad projection is solved at the held depth, then locked to the actual ink anchor throughout pulse/paper thump. No new assets, scene/prefab/GUID, scoring or reward changes.
- **Exact files:** `Assets/Scripts/Level2/Level2DeskStamps.cs`, `Level2StampDrag.cs`, `Level2FolderHandoff.cs`; `Assets/Scripts/Interactables/DeskMotion.cs`; `Assets/Scripts/UI/HUD/CaseFolder3DView.cs`; `Assets/Scripts/CaseFolder/CaseFolder3DDrag.cs`; `Assets/Resources/Tutorials/Level_02/guide.json`; `Tests/Level2Regression/Program.cs`. This initiative's README, SPEC, WORLD_AND_FEEDBACK, KNOWN_ISSUES, T02, T03 and PROGRESS updated.
- **Verification:** `dotnet build Temp/ScratchBuild/Scratch.csproj --verbosity quiet` → 0 errors/29 baseline warnings. Level2Regression → 546 PASS (18 new mathematical pad-anchor cases plus wiring guards); ControlsRegression → 5,734 PASS; DialogueRegression → 799 PASS; ProgressionRegression → PASS; GuideRegression → 714 PASS after authorized rerun for sandbox-blocked replacement of its temporary save fixture. Source guards verify wiring rather than rendered behavior.
- **Next/user checks:** first/repeated folder-button centering and unchanged entrance, chosen drag position, Ready outline, full Not Ready model while floating/aiming over folder/book/notes, both pad/indicator/ink landings through impact/rebound, rejected/accepted presses, cancellation/put-away/reopen and device frame pacing. T03/T04 user acceptance remains pending, then T05. No Unity or Git commands run.

- **Content/document checks:** `python Tests/VerifyGuideTargets.py` -> 17 paths, none missing; 24 local documentation links resolve; guide JSON parses and edited text passes CRLF validation.

## Stamp overlay camera, exact landing and feel polish — implemented; user Play Mode pending (2026-10-19)

Owner: Claude. User screenshots: floating stamps drawn behind the open case folder, and the held stamp invisible while aiming (only the ghost imprint shows). Several earlier capture/sort-order repairs (Doc3DLayer `AlwaysComposite`, tool plane, layer 29) did not fix it in Play Mode, so the visibility no longer depends on the capture's sort order at all.

- **Root-cause reasoning (not confirmed in Unity):** the lifted stamps were drawn by the main camera at `1.6 × near clip` depth (0.16–0.48 m) while the desk folder can sit closer, and the only "on top" path was the Doc3DLayer capture, which loses to the folder whenever the folder takes the front (any press on the paper, page turns). Over the folder the held stamp was therefore fully covered while its UI ghost stayed visible.
- **Fix — layering:** lifted stamps (and stamps flying back) move to `Level2DeskStamps.StampLayer = 28` (unused, checked in TagManager and Office_Level2). The main camera skips that layer and a URP Overlay camera ("Desk Stamp Camera", child of the main camera, first in its camera stack so the settings board stays above, near clip 0.1 × main) draws only it after the whole 3D world. The Doc3DLayer capture is kept with `AlwaysComposite = false`, only for screen paper windows / captured documents, and is raised to the front every frame while the stamps are up. `HudWindowFocus` treats a press on a layer's own UI (the stamp pointer surface) as touching that layer, so grabbing a stamp no longer raises the folder.
- **Fix — exact landing:** the stamp now physically lands on the paper. Its pad is built with the imprint's proportions and the stamp is sized at contact so the pad covers `ReviewInkMark.Size` (440 × 160 paper units) at the imprint's angle (`TryContactPose`). Press = anticipation lift → straighten → slam (ease-in) → pad pinned to the live paper point during squash/spring and paper thump → peel off → home. The decision/ink still happens once at impact with the same paper-local point the ghost showed. An aimed imprint near the edge is nudged fully onto the page (`KeepInkOnPage` / `FitInk`).
- **Feel:** model parts hang off a `Body` child pivoted on the rubber face (squash keeps the pad on the ink); pick-up pop + click; drag sway that leans into the motion; over the front page the stamp blends (smoothed) from the hand pose to a hover just past the ghost, leaning back so the ghost and label stay readable; on touch the imprint is aimed 7.5% of the screen above the finger; ghost pulses; click when the ghost first appears; impact = paper + click sounds, ink ring burst, squash-and-spring; rejected press = small "no" wobble plus the existing fading faint mark; mouse hover grows/lifts a floating stamp.
- **Exact files:** `Assets/Scripts/Level2/Level2StampDrag.cs` (rewritten), `Assets/Scripts/Level2/Level2DeskStamps.cs`, `Assets/Scripts/UI/ReviewInkMark.cs` (public `Size`), `Assets/Scripts/UI/HUD/HudWindowFocus.cs`, `Tests/Level2Regression/Program.cs`. No scene, prefab, TagManager, GUID, content, rule or reward changes.
- **Verification:** `python Tests/make_scratch_build.py` + scratch build → 0 errors / 29 baseline warnings. Level2Regression → 542 PASS (old source-string guards for the replaced tool-plane/PadRootAxis design were rewritten; new numeric checks: pad face lands on the imprint point at several sizes/rotations, edge imprints fit the page, in-page imprints are untouched). `python Tests/VerifyLevel2Scene.py` → PASS (existing Office_Level1 copy warning). Play Mode NOT RUN; no rendered result is claimed.
- **Next/user checks:** stamps above the folder while floating, held, aiming and pressing; pad lands exactly on the ghost (center and edges of the page, both stamps); feel of lift/sway/hover/slam/squash/burst; rejected vs recorded press; put away/reopen; Notes or Findings sheet overlapping the stamps (capture path); settings opened while stamps are out (board must stay above); device frame pacing. Tune constants at the top of `Level2StampDrag` (timings, `HoverLift`, `HoverBack`, `HoverTilt`, `TouchAimLift`, sway).


## T05 hands-on income toolkit — implemented; user Play Mode pending (2026-10-19)

Owner: Claude. Scope: the physical toolkit on Rosa's records desk, one guided example and the player's own repeat, built as the single annotation view and action set that T06 reuses. No scores, rewards, scene/prefab edits; four new script `.meta` files with unique GUIDs.

- **Behavior:** after Rosa's handover, sitting at the records desk opens a navy case on the desk's left (hinged lid swings ~100 degrees in 0.45 s with a slight overshoot). TYPE cards (blue left tab) and TREATMENT cards (amber top bar) rise out into two columns, record tags (grey edge, punched hole) sit right of the paper, and a clipboard with the T0 payslip lies on Rosa's record tray. First visit only: Arthur demonstrates (the marker lifts and traces the salary line, leaving a translucent highlighter stroke; each card flies to its place and settles; the clip flies over and bites the payslip tag), with one short rule line per step, then everything goes back. Player: tap a line to highlight; tap a card then its place, or drag it there; tap a placed card to take it off (or select another card of its kind and tap the placed one to swap). A wrong-kind place returns the card softly with a hint; while a card is held its place tints green ("fits", never "correct"); "Placed" shows briefly with a paper tick. Each placement gives the card's rule; a note that differs from the demonstration says so (practice only). Matching all four calls `TryCompleteTutorial` (Tutorial to Challenge), shows Arthur's done line, plays the achievement sound and raises `AnnotationTutorialCompleted`.
- **Ownership/cleanup:** input is read directly only while seated at this desk, with no demonstration running, nothing animating and not `TeachingPaused` (settings, dialogue, game pause). Presses on HUD buttons, open paper windows or 3D documents are ignored. Leaving the desk closes the case, cancels a held card and returns loose cards; placed notes stay on the paper; an interrupted demonstration replays next visit. Built once per desk (`IncomeToolkit.Create` reuses an existing child); runtime materials are released on destroy. No `Time.timeScale` use.
- **Data:** `review.annotation` in `Levels/Level_02/cases/case_rosa_001.json`: 5 TYPE options and 4 TREATMENT options with general rule lines (passive income is not called tax-free or final-taxed; "Not income" is separate from "Statutory exclusion"); tutorial T0 (fictional Northwind Printing payslip PS-0430, 5 lines, 3 record tags, the demonstration and its guide lines); `entries: []` for T06. `CaseReviewValidator` checks option ids/labels, entry lines/tags, maxEvidence 1–2, and that the demonstration uses its own entry's line and options.
- **State:** `CaseReviewState.toolkitDemoSeen`, `tutorialAnnotation` (`AnnotationEntryState`), `tutorialCompleted`; `TutorialActive`, `TutorialWork`, `TryMarkToolkitDemoSeen`, `TryCompleteTutorial`. Practice never changes `packetRevision`. New events `ToolkitDemoFinished` and `AnnotationTutorialCompleted` (objective/guide triggers). New objective step with a records-desk waypoint, and guide lesson `toolkit-practice`.
- **Exact files:** new `Assets/Scripts/Data/IncomeAnnotation.cs`, `Assets/Scripts/Level2/IncomeToolkit.cs`, `Assets/Scripts/Level2/AnnotationPaper.cs`, `Assets/Scripts/Level2/ToolkitTarget.cs` (each with a `.meta`); edited `Assets/Scripts/Data/Definitions/CaseReviewDefinition.cs`, `Assets/Scripts/Data/CaseReviewState.cs`, `Assets/Scripts/Data/CaseReviewValidator.cs`, `Assets/Scripts/Data/ObjectiveTriggers.cs`, `Assets/Scripts/Core/GameplayEvents.cs`, `Assets/Scripts/UI/Guide/GuideDirector.cs`, `Assets/Scripts/UI/HUD/OfficeObjectives.cs`, `Assets/Scripts/Level2/Level2RosaStall.cs`, `Assets/Scripts/Level2/MarketRecordsDesk.cs`, `Assets/Scripts/Level2/Level2DeskStamps.cs` (`TryReadPointer` made public for reuse); content `Assets/Resources/Levels/Level_02/cases/case_rosa_001.json`, `Assets/Resources/Levels/Level_02/objectives.json`, `Assets/Resources/Tutorials/Level_02/guide.json`; `Tests/Level2Regression/Program.cs`.
- **Verification:** `python Tests/make_scratch_build.py` + scratch build: 0 errors / 29 baseline warnings. Level2Regression: 616 PASS (new: content/validator cases; every annotation action including kind fit, replace, swap, bundle and removal; a data-defined second entry; tutorial transition, once-only, no packet change; JSON round trip; triggers; presentation-only demonstration; pause predicate; timings; no score/reward calls; cleanup). GuideRegression 714 PASS; DialogueRegression 799 PASS; ControlsRegression 5,734 PASS; ProgressionRegression PASS; `python Tests/VerifyGuideTargets.py` 17 paths, 0 missing; `python Tests/VerifyLevel2Scene.py` PASS (existing Office_Level1 copy warning). Play Mode NOT RUN.
- **Next/user checks:** sit at the records desk after Rosa: lid/card/marker/clip motion and layout on the desk (nothing floating or clipping the tray, bundles or desk edge), phone text readability, demonstration pacing and Arthur's lines; then tap/tap and drag for each action, wrong-kind return, replace/remove, completion line and objective tick; leave mid-demonstration and mid-drag, then reopen; open settings during the demonstration. Then **T06** (Rosa's E1–E4 entries, 12 scored decisions, frozen tier), reusing `IncomeToolkit` and `AnnotationActions`.


## T06 Mark the Money! challenge + toolkit feel pass — implemented; user Play Mode pending (2026-10-19)

Owner: Claude. User asked to make the toolkit feel satisfying and game-like (highlighting looked yellow before inking, cards looked off, dull animation, add validations), then implement T06. Note: between this session's T05 and this work, another session had edited the toolkit (marker pickup, `HighlighterSweep`, card captions, layout tests, desk depth 0.82 and folded laundry). This pass rebuilt `IncomeToolkit`/`AnnotationPaper` on top of that state, keeping `HighlighterSweep`, `CardCaption`, `CardHome`, `PracticeHint` and their tests. Also: this session ran one read-only `git diff --stat` by mistake (AGENTS forbids any git command); nothing was changed by it.

- **Feel/visuals:** swipe-to-ink highlighter (ink only where the nib passed, uneven segments, wet-ink dot, completion burst + SOURCE flag, tap draws it for you); pale blue row hover instead of yellow; cards with border, header band, drawn icons/ticks (no font glyphs), shadows that follow height, pickup glow, deal-out flips, squash-and-spring landing, sparkles (`ToolkitFx`: runtime ParticleSystems), shake-back for wrong-kind places, idle breathing and idle nudges; dashed note places that light up only for a fitting held card; lid now opens up/back (was rotating down into the case) with a scoreboard underneath; lid closes with a bounce.
- **Validations:** wrong-kind places refused with a hint and red flash; locked (confirmed right) notes cannot be removed; CHECK with missing items names the first record's missing parts, flashes its tabs/places and scores nothing; the two-record clip says when it swaps out the oldest record; tutorial gives a rule line per action; hints only for red notes.
- **T06 rules:** `IncomeChallenge` (pure): 4 entries x TYPE/TREATMENT/EVIDENCE = 12 decisions; highlight required but unscored; incomplete check changes nothing; first complete check frozen (`firstCorrect`, `firstWrong`); evidence must be the exact record set (E4 = D2 + D5; E1 = D1 only); confirmed/flagged notes until edited; hints general then specific; mastery required; `TryAward` once (phase to OfficeReconciliation). Rewards: `rewards.json` `minExp 48`, `goldAtCorrect 12`, `silverAtCorrect 10`; `LevelScoring.AccuracyBadge`; `LevelScoreLedger.AddSideQuest(..., accuracyTier)` (Level 1 call and badge rule unchanged). Reward card through new `SideQuestCompleteUI.ShowScreen(RewardScreenData)`.
- **Content:** E1–E4 records (player-facing, no answers printed) and `annotationKey` with hints that never mention summary totals, findings, the omission or the stamp. Validator checks keys, options, bundle size and hints. Objectives: two new steps (`AnnotationsChecked`, `IncomeQuestCompleted`) with records-desk waypoints; guide lessons `mark-the-money` and `mark-the-money-fix`.
- **Exact files:** new `Assets/Scripts/Data/IncomeChallenge.cs`, `Assets/Scripts/Level2/ToolkitFx.cs` (+ `.meta`); rewritten `Assets/Scripts/Level2/IncomeToolkit.cs`, `Assets/Scripts/Level2/AnnotationPaper.cs`, `Assets/Scripts/Level2/ToolkitTarget.cs`; edited `Assets/Scripts/Data/CaseReviewState.cs`, `Assets/Scripts/Data/CaseReviewValidator.cs`, `Assets/Scripts/Data/Definitions/CaseReviewDefinition.cs`, `Assets/Scripts/Data/ObjectiveTriggers.cs`, `Assets/Scripts/Core/GameplayEvents.cs`, `Assets/Scripts/UI/Guide/GuideDirector.cs`, `Assets/Scripts/UI/HUD/OfficeObjectives.cs`, `Assets/Scripts/RewardsSystem/LevelRewardRules.cs`, `LevelScoring.cs`, `LevelScoreLedger.cs`, `Assets/Scripts/SideQuest1/SideQuestCompleteUI.cs`, `Assets/Scripts/Level2/MarketRecordsDesk.cs`; content `Assets/Resources/Levels/Level_02/cases/case_rosa_001.json`, `rewards.json`, `objectives.json`, `Assets/Resources/Tutorials/Level_02/guide.json`; `Tests/Level2Regression/Program.cs`.
- **Verification:** scratch build 0 errors / 29 baseline warnings. Level2Regression 806 PASS (new: tier boundaries 0/9/10/11/12, EXP floor, Level 1 badge unchanged, key vs CONTENT, no answers on records, hint leakage words, incomplete/partial checks, complete runs with 0/1/2/3/12 wrong, frozen score through corrections, locks/flags/hints, award once, save round trip, bundle rules, Annotated List without totals, source guards). GuideRegression 714, DialogueRegression 799, ControlsRegression 5,734, ProgressionRegression, CareerBoardRegression 69, SettingsRegression 93, TrainingRegression 513 all PASS; VerifyGuideTargets 17/0 missing; VerifyLevel2Scene PASS (existing warning). Play Mode NOT RUN.
- **Next/user checks:** toolkit feel (swipe ink, card look/readability on phone, deal/flip/squash/sparkles, lid scoreboard readable), tutorial then challenge transition, three runs for Gold (12/12), Silver (one or two wrong) and Bronze (three or more wrong), CHECK with missing notes, red-note hints and locks, mastery spread/pack-up, EXP/hearts and medal card once, leaving mid-sequence and returning. Then **T07** (office reconciliation).


## T07 office reconciliation — implemented; user Play Mode pending (2026-10-19)

Owner: Claude. Scope: the player-reviewed schedule and findings F1/F2 in Rosa's folder, a pure evaluator with root causes for T09, and meaningful revision tracking. No scene, prefab, GUID (beyond two new script `.meta` files), reward or stamp changes.

- **Behavior:** after the quest award (phase OfficeReconciliation) the folder shows, after Findings: Annotated Income List, Reviewed Schedule, Finding F1 · Unlisted payment, Finding F2 · Do two records disagree?. Boxes use the existing FindingSlot tap/drag transfer; a tap with nothing picked up empties a box. Schedule boxes take money lines from Rosa's records only (not S1); totals are summed from the player's lines. The same payment cannot be counted twice (moneyIds per record line; the refusal names the line already holding it). Any other line, including the loan, can be added (complete wrong reviews stand until review). Reasons/conclusions come from the book. `OfficeReviewCompleted` fires once when structurally complete.
- **Rules (pure `OfficeReview`):** CanEdit from OfficeReconciliation until Audit/Completed; `SetScheduleRow`/`ClearScheduleRow`/`SetPart`/`ClearPart` each count one packet revision only on a real change; `StructurallyComplete`; `Evaluate` returns May/annual totals and root causes once each: `omission`, `missing-income:<money>`, `not-income-counted:<money>`, `double-counted`, `f1-unsupported`, `f2-unexplained`, `f2-false-flag` (F1 "corrected" without the scheduled job is the omission, counted once). Blank rows restored by Unity JSON count as empty.
- **Data:** `review.conclusions`, `scheduleSources` (10 record lines with amounts and moneyIds), `scheduleSlots` 4, `officeFindings` (F1, F2 player-facing) and `officeRule` (answer data); new reason `paid-another-way`; book Review Notes gains that reason, one 7-word paragraph and three conclusion values (82 words total across the two leaves). Validator checks sources, pages, parts and that rule answers exist.
- **Exact files:** new `Assets/Scripts/Data/OfficeReview.cs`, `Assets/Scripts/Level2/Level2OfficeReview.cs` (+ `.meta`); edited `Assets/Scripts/Data/Definitions/CaseReviewDefinition.cs`, `Assets/Scripts/Data/CaseReviewState.cs`, `Assets/Scripts/Data/CaseReviewValidator.cs`, `Assets/Scripts/Data/ObjectiveTriggers.cs`, `Assets/Scripts/Core/GameplayEvents.cs`, `Assets/Scripts/UI/Guide/GuideDirector.cs`, `Assets/Scripts/UI/HUD/OfficeObjectives.cs`, `Assets/Scripts/Level2/FindingSlot.cs` (optional `Cleared`), `Assets/Scripts/Level2/Level2FindingsPage.cs` (appends the office pages), `Assets/Scripts/Level2/Level2OfficeStart.cs` (part + SetupVersion 8); content `Assets/Resources/Levels/Level_02/cases/case_rosa_001.json`, `book.json`, `objectives.json`, `Assets/Resources/Tutorials/Level_02/guide.json`; `Tests/Level2Regression/Program.cs`.
- **Verification:** scratch build 0 errors / 29 baseline warnings. Level2Regression 907 PASS (new: data/record-line amount match, no answers in player text, supported review = 20,000 / 140,000, split bank+cash lines, either-order F2, omitted/loan/double-count/false-flag/unexplained/F1-unsupported/missing-income packets, idempotent transfer, revision counting, edit window, save round trip, blank rows, S1 unchanged, Gold quest alone incomplete, no-rule cases, validator negatives, source guards). GuideRegression 714, DialogueRegression 799, ControlsRegression 5,734, ProgressionRegression PASS; VerifyGuideTargets 17/0 missing; VerifyLevel2Scene PASS (existing warning). Play Mode NOT RUN.
- **Next/user checks:** after the quest, open Rosa's folder at the office desk: the four new pages read clearly; picking a record line and tapping a schedule box (and dragging) works; totals update; adding D6 after D1's total is refused with a clear message; F1/F2 boxes take record lines, Review Notes reasons and conclusions; tapping a filled box empties it; the objective ticks once when everything is filled. Then **T08** (final stamp with reason, bound to the packet revision).


## T08 final stamp, T09 Auditor outcomes, T10 review cutoff — implemented; user Play Mode pending (2026-10-19)

Owner: Claude. The user asked for three tickets in one go with internal tests before each; they run the Unity checks. Each ticket was compiled and tested before the next (T08: 963 PASS, T09: 993 PASS, T10: 1,018 PASS).

- **T08:** pure `FinalAssessment` (reason required, structural prerequisites only, commit once at impact to the packet revision, same verdict on a current stamp ignored, stale after any graded change, history of 3, initial stamp untouched). Final Assessment folder page (recommendation, Reason box, revision, history) and its ink (faded when stale). Desk stamps turn to and stamp that page once the review is back at the office; `Level2PreliminaryStamp` routes final stamps. Hand-in gate (`Level2FolderHandoff.CanHandInReview`): current stamp, changed since a returned/held review, window open. Book Reading Records offers 5 generic assessment reasons.
- **T09:** pure `FinalAudit` (Approved / SupportedHold / PrematureApproval / FalseHold / ReasonError; NotStamped / Unchanged refused; defects = office root causes + inherited field issues; false flag folded into one `false-hold`; root causes charged once; holds free; award once). `AuditorInteractable.LevelAudit` hook + `RunLevelDialogue` / `ReturnFolderForReview` / `CompleteLevelReview`; `Level2Audit` records once before the dialogue, then Auditor lines (data, per root cause) and Rosa's reaction. Rewards: `caseFile.maxExpLoss/maxHeartLoss` caps (0 = off for Level 1), `LevelScoring.Case(..., extraExpLoss, extraHeartLoss)`, `CaseProgressionManager.CaseExtraExpLoss/HeartLoss`; Level 2 `heartsPerRetry` 1. Worst completed run: 133 EXP / 8 hearts (passes).
- **T10:** pure `ReviewClock` + `Level2ReviewClock` part (chip, warnings, deferred rollover with fade, desk placement, single late consequence, on-time recognition flag). Level 1 `OfficeHours` untouched.
- **Events/objectives/guide:** `FinalAssessmentStamped`, `AuditReturned`, `ReviewDayRolled`; objective step for the final stamp (waypoint: desk stamps); guide lessons `final-assessment`, `to-auditor`, `audit-returned`. The last objective (state:CaseCompleteState, Auditor) is reached through approval.
- **Exact files:** new `Assets/Scripts/Data/FinalAssessment.cs`, `FinalAudit.cs`, `ReviewClock.cs`, `Assets/Scripts/Level2/Level2Audit.cs`, `Level2ReviewClock.cs` (+ `.meta` each); edited `Assets/Scripts/Data/Definitions/CaseReviewDefinition.cs`, `Data/CaseReviewState.cs`, `Data/ObjectiveTriggers.cs`, `Data/CaseProgressionManager.cs`, `Core/GameplayEvents.cs`, `Interactables/AuditorInteractable.cs`, `RewardsSystem/LevelRewardRules.cs`, `LevelScoring.cs`, `LevelScoreLedger.cs`, `UI/Guide/GuideDirector.cs`, `UI/HUD/OfficeObjectives.cs`, `Level2/Level2OfficeReview.cs`, `Level2FindingsPage.cs`, `Level2PreliminaryStamp.cs`, `Level2DeskStamps.cs`, `Level2StampDrag.cs`, `Level2FolderHandoff.cs`, `Level2OfficeStart.cs` (SetupVersion 10); content `cases/case_rosa_001.json`, `book.json`, `rewards.json`, `objectives.json`, `Tutorials/Level_02/guide.json`; `Tests/Level2Regression/Program.cs`.
- **Verification:** scratch build 0 errors / 29 baseline warnings. Level2Regression 1,018 PASS. GuideRegression 714, ProgressionRegression PASS, CareerBoardRegression 69, ControlsRegression 5,734, DialogueRegression 799, SettingsRegression 93, TrainingRegression 513; VerifyGuideTargets 17/0 missing; VerifyLevel2Scene PASS (existing warning). Play Mode NOT RUN.
- **Next/user checks:** Final Assessment page readable; reason box takes Reading Records reasons; stamps turn to that page, ink lands there, a change fades it and the hand-in asks for a restamp. Auditor: approval (completion + case card once), justified hold (Payment not counted while the blanket job is missing), premature Ready, false hold (Difference unexplained on a supported packet), wrong reason; unchanged resubmission refused; Rosa's reactions; folder comes back each time with all work. Clock: chip at the top after the receptionist, pauses during dialogue/guide/demonstration, warnings, cutoff during a stamp or drag waits, next-morning fade at the desk, the 3 EXP / 1 heart late loss once. Then **T11** (save/checkpoint and finale).


## T11 resume, Rosa's ending and story fact — implemented; user Play Mode pending (2026-10-19)

Owner: Claude. Note: between T10 and T11 another session changed Mark the Money! to score the highlighted source line too (16 decisions; rewards.json Gold 16 / Silver 14), and edited the guide, book and toolkit. That design was kept; T11/T12 tests use it.

- **Checkpoint:** `LevelCheckpoint` in `PlayerProgressData.activeCheckpoint` (same signed save, version 4 unchanged); `Level2Checkpointer` saves at milestones / 30 s / pause / quit (not after approval) and restores review, taxpayer facts, folder, stall records, objectives and earned score without raising events. `ApplyLevelResult` clears the checkpoint in the result's write.
- **Finale:** `OfficeFeature.Finale` (Level 2 level.json "finale"); `CaseFinale` gated by the level profile, tier from the review's returned submissions and cutoff; Rosa's lines from case JSON `clientFinale` with the Desk confirmation; story fact `rosa-desk-provenance` saved once on approval.
- **Files:** new `Assets/Scripts/Data/Level2Checkpoint.cs`, `Assets/Scripts/Level2/Level2Checkpointer.cs` (+ `.meta`); edited `RewardsSystem/PlayerProgressData.cs`, `PlayerProgressManager.cs`, `Data/CaseProgressionManager.cs` (`RestoreAuditCounters`), `Data/OfficeLevelCatalog.cs`, `UI/HUD/CaseFinale.cs`, `Level2/Level2Audit.cs`, `Level2FolderHandoff.cs` (`RestorePlacedFolder`), `Level2OfficeStart.cs` (SetupVersion 11); content `cases/case_rosa_001.json` (clientFinale), `level.json`.
- **Verification:** build 0 errors / 29 warnings; Level2Regression 1,047 PASS at T11 (round trip of a mid-run checkpoint, blank-record normalization, implied triggers, old-save defaults, story fact once, career result once + Level 3 gate, 160/9 maximum, source guards); Progression, CareerBoard 69, Guide 714, Training 513, Settings 93, Dialogue 799, Controls 5,734 PASS.

## T12 integration — implemented; user Play Mode pending (2026-10-19)

- Whole-level pure flows (correct 160/160 EXP + 9/9 hearts + Gold; wrong income Bronze; premature Ready then fixed; justified hold; false hold; late with one loss; resumed at the office) and content/leakage checks in Level2Regression (1,065 PASS). New `Tests/VerifyLevel2Assets.py`: 11,659 unique `.meta` GUIDs; 36 Level 2 scripts and 7 content files have `.meta` and CRLF (fixed `Level2DeskStamps.cs`, which was LF-only).
- Commands (project root): `python Tests/make_scratch_build.py`; `dotnet build Temp/ScratchBuild/Scratch.csproj` (0 errors / 29 baseline warnings); `dotnet run` in Tests/Level2Regression, GuideRegression, ProgressionRegression, CareerBoardRegression, ControlsRegression, DialogueRegression, SettingsRegression, TrainingRegression (all PASS); `python Tests/VerifyLevel2Scene.py` (PASS, existing Office_Level1-copy warning); `python Tests/VerifyGuideTargets.py` (17 paths, 0 missing); `python Tests/VerifyLevel2Assets.py` (PASS). Unity import, Play Mode and device checks: NOT RUN (user-owned).

## Final handoff (2026-10-19)

**Playable flow (source-complete, user Play Mode pending):** reception (cutoff announced) -> market-client drawer -> desk (folder from HUD icon) -> Findings slip -> initial Not Ready stamp -> Rosa's stall (records) -> income toolkit (demo, practice, Mark the Money! E1-E4, frozen first check, corrections, medal card) -> office: Annotated Income List, Reviewed Schedule, F1, F2 -> Final Assessment (reason + stamp) -> Auditor (approve / hold / return) -> Rosa's ending -> Level Complete. Review cutoff, next-morning rollover and resume work throughout.

**Runtime parts (all built by `Level2OfficeStart` under `Level2Gameplay`, no manual scene assembly):** CaseRetrieval, FolderHandoff, FindingsPage, PreliminaryStamp, DeskStamps (overlay stamp camera, layer 28), RosaStall (Building_4 store, SM_Chr_Business_Female_03 Rosa, records desk, IncomeToolkit), OfficeReview, Audit, ReviewClock, Checkpoint. Shared hooks added: `AuditorInteractable.LevelAudit`, `StampUI.StampOverride` (T03), `AuditorSubmissionTray.SubmissionBlocked`, `CaseFolderUI.ExtraPages/PageDecorator`, `OfficeFeature.Finale`, reward caps/accuracy badge fields (0 = off for Level 1).

**Pure rule models (tested outside Unity):** `CaseReviewState`, `PreliminaryReview`, `FindingComparison`, `AnnotationActions`, `IncomeChallenge`, `OfficeReview`, `FinalAssessment`, `FinalAudit`, `ReviewClock`, `Level2Checkpoint`, `CaseReviewValidator`, `LevelScoring`.

**Content (Resources/Levels/Level_02):** `level.json`, `rewards.json`, `objectives.json`, `office_start.json`, `book.json` (2 leaves), `cases/case_rosa_001.json` (documents, review, annotation + key, office review + rule, assessment reasons, audit lines, clock, clientFinale), `scene_assets.asset`; `Resources/Tutorials/Level_02/guide.json`.

**Ordered user checks (Play Mode / device):**
1. Start Level 2 from the career board: one HUD, receptionist, cutoff chip appears after the talk.
2. Drawer -> folder -> desk -> Findings slip -> initial stamp (stamps visible above the folder, exact landing).
3. Stall: Rosa idle (no T-pose), conversation, records in the tray.
4. Toolkit: lid/cards/marker feel, demo, practice, Mark the Money! three runs (Gold / Silver / Bronze), medal card once.
5. Office: four new folder pages readable on phone; D6-after-D1 refusal; F1/F2 boxes; Final Assessment reason + stamp; edit -> stamp fades -> hand-in asks to restamp.
6. Auditor: approve, justified hold, premature Ready, false hold, wrong reason, unchanged resubmission refused; Rosa's reactions; folder returns with work.
7. Clock: pauses in dialogue/guide/demo; warnings; cutoff during a stamp/drag waits; next-morning fade at the desk; late loss once.
8. Quit mid-quest and mid-office, relaunch Level 2: work, grade, penalties and objectives restored; no reward repeats.
9. Approval: Rosa's ending in the conference room, Level Complete card, Continue to the menu, Level 3 unlocked when passed; replay farms nothing.
10. Revisit Level 1: original quest, gate, Marie case, office hours, rewards unchanged.

**Not claimed:** Unity import success, visuals, timing, readability, device performance, audio balance. Git is user-owned.


## Level 1 consistency follow-up — in progress (2026-10-19)

Owner: Codex. User requested shared office-hours presentation, physical detachable records, a compact folder, fresh direct Editor Play starts, annotated-paper collection and the shared side-quest splash. Preserve existing work and Level 1 defaults. Scope: clock presentation, Level2 papers/rewards/startup and focused shared UI seams. Internal build/regression checks only; user owns Play Mode. Read-only exploration delegated under the astra-orchestrator skill; implementation ownership will remain separated by file. One read-only git status was run before discovering the local prohibition; no further git commands.


## Level 1 consistency follow-up — 2026-10-19

Implemented — internally verified; user Play Mode pending. This supersedes the older separate top-centre review-chip and 16-page folder descriptions.

- Shared OfficeHoursChip and OfficeMorningOverlay now present Level 2 time in the minimap, an analog cutoff chip below progression, and the same next-morning fade/title/day layout and timing. Day one begins 7:30 AM, subsequent days 8:00 AM; 12 active minutes map to two game hours. Auditor remains present. Existing one-time late penalty and case state are retained. Modal/teaching pauses and gesture boundaries prevent rollover during interaction.
- Folder now has three initial pages and six after the quest: Taxpayer Information, Prepared Packet (S1/N1 tabs), Findings (Finding and D1–D6 tabs after collection), Reviewed Schedule, Office Findings (F1/F2 tabs), Final Assessment. Stable records, evidence keys and grading remain intact. The obsolete page-6 document links are suppressed for review cases.
- Records reuse the existing removable Findings paper: attached world-space preview with layered paper edges/shadow, lift, independent floating comparison, selectable source rows, and animated filing. Detached papers use the existing perspective/composited UI system, not new free-standing mesh simulations.
- Annotated papers have a third HUD icon below Notes/Folder, saved-state contents, tap/drag opening, and a reward-card-following preview, flatten, return-to-icon and pulse. The shared SideQuestSplash shows Mark the Money content before the scored challenge.
- Direct Editor Play in Office_Level2 starts fresh and bypasses checkpoint reads/writes. Completing that attempt also preserves the existing checkpoint. Career-board entry and player builds retain normal resume. Existing career best-result saving remains enabled; this is checkpoint isolation, not a separate career-save sandbox.

Pending user checks: direct Play fresh cabinet start; career-board resume; readable S1/N1 and F1/F2 tabs; detach multiple receipts, transfer values and file them; splash and reward-to-icon animation; cutoff while reading/dragging/stamping and next-morning preservation. No Unity Play/device/visual verification was performed.

Validation: scratch build 0 errors / 29 existing warnings; Level2Regression 1,075 PASS; GuideRegression 714 PASS; ControlsRegression 5,734 PASS; ProgressionRegression PASS; VerifyLevel2Assets PASS (11,662 unique GUIDs); VerifyLevel2Scene PASS with existing baseline-drift warning; VerifyGuideTargets 17/0 missing. GuideRegression initially could not move its temp save under the host temp directory; rerun with TEMP/TMP set to workspace Temp/ConsistencyTests passed. Reviewer found completion clearing the preserved checkpoint; fixed in PlayerProgressManager. Review initially interrupted by usage limit, then resumed and completed. Final post-animation scratch compile passed: 0 errors / 29 existing warnings.

Touched files this follow-up: Level2ReviewClock, ReviewClock, OfficeHoursChip, OfficeHours, new OfficeMorningOverlay (+meta); Level2Checkpointer, Level2Checkpoint, PlayerProgressManager, Level2OfficeStart; CaseFolderUI, Level2FindingsPage, Level2FindingsSheet, new Level2RecordPaper (+meta), Level2OfficeReview; SideQuestSplash, IncomeToolkit, HudIconArt, new Level2AnnotatedPapers (+meta), GuideDirector; Level_02 case/objectives, guide.json; Tests/Level2Regression/Program.cs and this documentation pack. Scene files were not edited. Next: user Play Mode acceptance of this consistency pass.


## Paper desk interaction redesign — in progress (2026-10-19)

Owner: Codex. User requests chaptered readable objectives, separate prepared pages, desk-entry splash before teaching, four individually floating annotated quest papers, Supporting Documents stack (Finding + five records) with 3-second hold gather/drop return, simplified schedule/unlisted-payment review and handwritten final summary without final-reason selection. Ownership split across paper stacks, annotation cards and domain review; root handles objectives/splash/integration/tests. No scene regeneration, git or Unity Play.


## Paper desk interaction redesign — implementation and verification (2026-10-20)

Implemented — internally verified; user Play Mode pending. The newer SPEC section supersedes the prior tabbed folder, single reward list and F2/final-reason flow. All requested source/content work is runtime-built; no scene regeneration or manual assembly is required.

Changed paths in this pass:
- Objectives/splash: Assets/Scripts/UI/HUD/NotesPaperView.cs; Assets/Resources/Levels/Level_02/objectives.json; Assets/Scripts/Level2/IncomeToolkit.cs; Assets/Scripts/SideQuest1/SideQuestSplash.cs; Assets/Scripts/Data/CaseReviewState.cs.
- Paper stack: Assets/Scripts/UI/CaseFolderUI.cs; Assets/Scripts/Level2/Level2FindingsPage.cs, Level2FindingsSheet.cs, Level2RecordPaper.cs, Level2SupportingStack.cs (+meta), Level2SupportingStackCover.cs (+meta); Assets/Scripts/UI/HUD/FloatingWindow.cs; Assets/Scripts/Level2/Level2ReviewClock.cs.
- Annotated collection: Assets/Scripts/Level2/Level2AnnotatedPapers.cs, AnnotationPaper.cs, AnnotationPaperCollectionFace.cs (+meta).
- Compact review: Assets/Scripts/Data/Definitions/CaseReviewDefinition.cs; Assets/Scripts/Data/OfficeReview.cs, FinalAssessment.cs, FinalAudit.cs; Assets/Scripts/Level2/Level2OfficeReview.cs; Assets/Resources/Levels/Level_02/cases/case_rosa_001.json and book.json.
- Guidance/handover: Assets/Resources/Tutorials/Level_02/guide.json; Assets/Scripts/Level2/Level2FolderHandoff.cs, MarketRecordsDesk.cs, Level2RosaStall.cs.
- Verification/docs: Tests/Level2Regression/Program.cs and LegacyReview.json; this documentation pack. The legacy fixture preserves regression coverage for non-simplified cases while current-content tests exercise the compact flow.

Internal verification: python Tests/make_scratch_build.py; dotnet build Temp/ScratchBuild/Scratch.csproj --no-restore -v:q (0 errors, 29 baseline warnings); Level2Regression 1,086 PASS; GuideRegression 714 PASS; ControlsRegression 5,734 PASS; ProgressionRegression PASS; VerifyLevel2Assets PASS (11,665 unique meta GUIDs); VerifyLevel2Scene PASS (existing Level1-baseline drift warning); VerifyGuideTargets 17 paths, zero missing. Guide tests use workspace Temp/ConsistencyTests for TEMP/TMP. Scene hashes are unchanged from the previous pass, including Office_Level2_OLD.

Review fixes included objective re-completion after audit return, readable final summary, shorter schedule labels, paper resting scale preserved across focus/reopen, cancelled hidden-stack holds/filing, actual AnnotationPaper models for saved rewards and collection input protection. The summary uses player entries only; malformed or wrong work still reaches the auditor rather than being silently corrected.

User acceptance sequence:
1. Direct Editor Play begins fresh; career-board entry resumes a saved attempt.
2. Notes has three readable chapters; Summary and Payment Note are separate pages.
3. First records-desk visit shows splash before tutorial; completing the quest collects four actual papers to the new icon.
4. Open all four, reveal annotations, drag/focus each above/below folder/book, then toggle the icon to return them.
5. Drag Supporting Documents out; hold a paper three seconds, miss a folder drop, then gather/file successfully. Repeat with settings and folder close during hold/filing.
6. Fill the schedule and unlisted-payment task; read summary, stamp final, edit/restamp, submit. Test an incorrect submission and corrections.
7. Cutoff waits for paper/stamp actions, shows next morning, preserves work; revisit Level1 profile papers/office hours.

No Unity editor launch, Play Mode, touch/device, visual, timing or performance checks were performed. User-owned git operations were not run in this redesign. Next: user Play Mode acceptance; tune only reproduced visual/interaction issues.


## Polish and completion pass — implemented; user Play Mode pending (2026-10-20)

Owner: Claude (session 2026-10-20). User request: polish and complete Level 2 (visuals, layout, simplified case logic, annotated paper as knowledge/value source, Tax Code Book icon with cheat sheets, learning support, internal tests). Three interpretations were confirmed in chat (see SPEC "Polish and completion pass").

Changed paths:
- Content: Assets/Resources/Levels/Level_02/cases/case_rosa_001.json, book.json, objectives.json; Assets/Resources/Tutorials/Level_02/guide.json.
- Data/rules: Assets/Scripts/Data/Definitions/CaseReviewDefinition.cs (scheduleSlotLabels, ScheduleSourceDefinition.entryId, lookups), Data/CaseReviewValidator.cs, Data/OfficeReview.cs (DropRetiredRows).
- Office review and papers: Assets/Scripts/Level2/Level2OfficeReview.cs, AnnotationPaperCollectionFace.cs (AnnotatedValueRow), AnnotationPaper.cs (ToolkitBuilder.Unlit, TryLineBand), Level2AnnotatedPapers.cs, Level2SupportingStack.cs, Level2FindingsSheet.cs, Level2RecordPaper.cs, Level2RosaStall.cs, Level2OfficeStart.cs (SetupVersion 13).
- Book: new Assets/Scripts/Level2/Level2TaxCodeBookIcon.cs (+ .meta, unique GUID); Assets/Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs (OpenFromHud and accessors); Assets/Scripts/UI/TaxCodeBookUI.cs (IsShowing, first-leaf opening, FitBody); Assets/Scripts/UI/HUD/HudIconArt.cs ("book" glyph).
- Tests/docs: Tests/Level2Regression/Program.cs; this pack (README, SPEC, CONTENT, PROGRESS, KNOWN_ISSUES).

Internal verification (all run 2026-10-20): python Tests/make_scratch_build.py; dotnet build Temp/ScratchBuild/Scratch.csproj --no-restore -v:q (0 errors, 29 baseline warnings); Level2Regression 1,107 PASS (was 1,086; content checks rewritten for the simplified case, plus new checks for the annotated schedule, retired-row migration, slot captions/validator, book icon order, cheat sheets and source wiring); GuideRegression 714 PASS; ControlsRegression 5,734 PASS; ProgressionRegression PASS; DialogueRegression 799 PASS; TrainingRegression 513/513; SettingsRegression 93/93; CareerBoardRegression 69/69; IntroCutsceneRegression 29/29; VerifyLevel2Assets PASS (11,666 unique GUIDs); VerifyLevel2Scene PASS (existing Level1-drift warning); VerifyGuideTargets 17 paths, 0 missing; VerifyOfficeNpcMarkers PASS. No scene, prefab or TagManager file was edited.

Process note: at the start of this session one read-only `git status` ran before AGENTS.md was read, which breaks the git-ownership rule. No other git command was run.

Pending user Play Mode checks:
1. Tax Code Book icon sits between Case Folder and Annotated Papers; at the desk it opens and returns the desk book; while walking and at Rosa's records desk it floats, turns pages, swipes closed, and flies back on a second tap; leaving or entering a desk while it floats puts it back.
2. The book opens on "Cheat Sheet: The Cards"; every Level 2 leaf fits its page.
3. Rosa's envelope shows four sheets (D1, D3, D4, D5); Mark the Money! offers four record tags and a one-record clip; the practice ends with the new line.
4. The annotated papers look cream and bright (unlit), not grey; they are larger and do not overlap.
5. At the office the Reviewed Schedule shows three captioned lines, totals and one conclusion, all inside the page. Tapping an empty line opens the annotated papers to the left of the folder with their highlights showing. A highlighted amount pulses; tap it, then tap a line (and also try dragging). It turns green once scheduled, and a record row is refused with the pointer message.
6. Supporting Documents spreads Findings large on the left and four records in a 2 x 2 grid with readable text; gather, file and re-scatter still work.
7. Correct path: 12,000 + 3,000 + 5,000 and `Corrected: included`, approved. Loan in the missing line returns targeted feedback. Resume a checkpoint saved mid-review under the old layout and confirm the schedule note.

Next: user Play Mode acceptance; tune only reproduced visual or interaction issues.


## Additional Level 2 fixes — implemented; user Play Mode pending (2026-10-20)

Owner: Claude (same session). Covers the user's follow-up: book page styling, the Findings paper and folder, Tax Code Book storing and toggling, the exact annotated papers, and internal tests.

Changed paths: Assets/Resources/Levels/Level_02/book.json; Assets/Resources/Tutorials/Level_02/guide.json; Assets/Scripts/Data/TaxCodeSection.cs (readOnly); Data/DragDrop/TaxCodeValueChip.cs; Data/CaseReviewState.cs (taxBookStored); TaxCodeBook/TaxCodeBook3DDrag.cs (store hooks, ParkClosedBook, reset guards); Level2/Level2TaxCodeBookIcon.cs; Level2/Level2FindingsPage.cs; Level2/Level2FindingsSheet.cs; Level2/Level2OfficeReview.cs (summary last); Level2/IncomeToolkit.cs (BuildCardVisual, public PlacedScale/PaperScale); Level2/AnnotationPaper.cs (SelfLight, cardsAttached); Level2/AnnotationPaperCollectionFace.cs; Level2/Level2AnnotatedPapers.cs; Level2/Level2ReviewClock.cs; Tests/Level2Regression/Program.cs; this pack. Level2SupportingStack and Level2RecordPaper remain in the project but are no longer used by the Findings page.

Internal verification: scratch build 0 errors (29 baseline warnings); Level2Regression 1,118 PASS; GuideRegression 714; ControlsRegression 5,734; ProgressionRegression PASS; DialogueRegression 799; TrainingRegression 513/513; SettingsRegression 93/93; CareerBoardRegression 69/69; IntroCutsceneRegression 29/29; VerifyLevel2Assets PASS; VerifyLevel2Scene PASS (existing Level1-drift warning); VerifyGuideTargets 0 missing; VerifyOfficeNpcMarkers PASS. No git commands were run in this pass. No scene or prefab edits were made.

Pending user Play Mode checks:
1. The book opens on Section 4. Sections 4-6 look like Sections 1-3 (highlighted terms with explanations, readable size), and their terms cannot be dragged.
2. At the desk the empty book icon pulses with "Drag the book here". Drag the desk book onto it: it flies in, the icon fills, and the desk copy is gone. Leave and return to the desk: the book stays in the icon.
3. Tap the icon to open the book in the centre, tap again to close it, and drag it out to a spot. Do this idle, walking, at the office desk and during Mark the Money!. Before storing, the icon cannot be dragged.
4. The Findings page shows the attached paper smaller, with the page visible around it. Lift it (peel animation), work on it, then drop it on the folder while another page is showing: it hovers, the folder turns to Findings, and it attaches smoothly. Close the folder mid-drop and the paper stays loose.
5. The record pages (Clothes Collections, Blanket Wash Receipt, Rental Receipt, Loan Agreement) are readable, and their rows can still be used as the Clue.
6. Final Assessment: the handwritten summary is the last row.
7. Annotated papers: four in one row, with highlights and the attached cards (ticks when mastered), no flip, lit with shadows. Tap to bring one forward, drag them anywhere, and check the highlighted-amount bands still line up.


## Book, annotated paper and folder follow-up — implemented; user Play Mode pending (2026-10-20)

Changed: Assets/Scripts/Level2/Level2TaxCodeBookIcon.cs (prompt removed, FloatingBookGrab, composited while out); TaxCodeBook/TaxCodeBook3DDrag.cs (SetHeldTint, drag tint); UI/TaxCodeBookUI.cs (page 1, PaperRoot); Level2/AnnotationPaper.cs (ink under text, larger rows); Level2/AnnotationPaperCollectionFace.cs (AlwaysComposite, lighter value band, PresentSchedule on pick-up); Level2/Level2AnnotatedPapers.cs (size); Level2/Level2OfficeReview.cs (PresentSchedule); Level2/Level2FindingsPage.cs (no record pages); Resources case_rosa_001.json (row labels, schedule captions), guide.json (Arthur's clue, side quest line); Tests/Level2Regression/Program.cs.

Internal verification: scratch build 0 errors; Level2Regression 1,133 PASS; GuideRegression 714; ControlsRegression 5,734; ProgressionRegression PASS; DialogueRegression 799; TrainingRegression 513/513; SettingsRegression 93/93; CareerBoardRegression 69/69; IntroCutsceneRegression 29/29; VerifyLevel2Assets, VerifyLevel2Scene (existing warning), VerifyGuideTargets and VerifyOfficeNpcMarkers PASS. No git or Unity was run.

Pending user Play Mode checks:
1. No drag tag on screen; Arthur's line hints at the icon.
2. Drag the desk book: it is red-tinted while held, then flies into the icon.
3. Open the book from the icon: it is on page 1 and in front of everything. Drag it by the page (red tint) and drop it on the icon to put it away. Taps on page edges and Review Notes values still work.
4. After the side quest, all four papers, E1 included, show in front of the HUD and desk at the larger size. Rows are readable, the highlights sit under the text, and the cards are attached.
5. At the office, tap or drag a highlighted amount: the folder turns to the Reviewed Schedule and comes forward; drop or tap a line. The captions read Summary line 1/2 and Missing entry.
6. The folder has six pages, with no record pages.


## Book pose, red book and paper-drag performance — implemented; user Play Mode pending (2026-10-20)

Changed: Assets/Scripts/Level2/Level2TaxCodeBookIcon.cs (page-based reading pose, grip on the book root, red ghost); UI/HUD/HudIconArt.cs (red book glyph); UI/HUD/Doc3DLayer.cs (opt-in RenderOnlyWhenChanged, FootprintRect, MarkDirty); Level2/AnnotationPaperCollectionFace.cs (per-paper layers 23-26, change-only captures, window footprint, main camera skips paper layers); Level2/AnnotationPaper.cs and Level2/Level2AnnotatedPapers.cs (pass the paper index); Tests/Level2Regression/Program.cs.

Internal verification: scratch build 0 errors; Level2Regression 1,138 PASS; GuideRegression 714; ControlsRegression 5,734; ProgressionRegression PASS; DialogueRegression 799; TrainingRegression 513/513; SettingsRegression 93/93; CareerBoardRegression 69/69; IntroCutsceneRegression 29/29; VerifyLevel2Assets, VerifyLevel2Scene (existing warning), VerifyGuideTargets and VerifyOfficeNpcMarkers PASS. No git or Unity was run.

Pending user Play Mode checks: open the stored book while walking, at the office desk and at Rosa's desk. It should read upright and centred with the whole book visible. Drag it by its page (red tint), drop it on the icon, take it out again, and repeat. The icon and drag ghost are red. With all four annotated papers open, drag each one: it should follow the finger without stutter, come to the front on tap, and show no paper twice or behind the desk. Watch the editor frame time and the device.


## Tax Code Book on the Case Folder system — implemented; user Play Mode pending (2026-10-20)

Changed: Assets/Scripts/Level2/Level2TaxCodeBookIcon.cs (Case Folder framing, camera-local hold, open desk book storing); TaxCodeBook/TaxCodeBookFloatingModel3D.cs (TryStoreAt hook, red tint while held); TaxCodeBook/TaxCodeBook3DDrag.cs (ParkOpenBook); Tests/Level2Regression/Program.cs. Scratch build 0 errors; Level2Regression 1,139 PASS; Guide 714, Controls 5,734, Progression, Dialogue 799, Training 513, Settings 93, CareerBoard 69, Intro 29, VerifyLevel2Assets and VerifyLevel2Scene PASS.

Pending user checks: at the desk, drag the open book onto the icon (red while held), and also the closed book. Tap the icon while walking, then look around and walk: the book stays upright and readable in front of you. Drag it by its page and drop it on the icon. Take it out by dragging from the icon. Repeat at the office desk and at Rosa's records desk.


## Records desk, book colour, folder return and audit summary — implemented; user Play Mode pending (2026-10-20)

Changed: Assets/Scripts/UI/WorkstationUI.cs (ExitBlocked); UI/Guide/GuideCoachView.cs (HintAtBottom); Level2/IncomeToolkit.cs (exit hold, bottom hint, higher paper/pen); TaxCodeBook/TaxCodeBook3DDrag.cs (ApplyRedCover, no colour-changing hold tint); Level2/Level2FolderHandoff.cs (stored folder back to the desk); UI/AuditSummaryPopupUI.cs (ShowScreen, null-safe hands); Interactables/AuditorInteractable.cs (ShowLevelSummary); Level2/Level2Audit.cs (Summary card before completion and returns); Tests/Level2Regression/Program.cs.

Internal verification: scratch build 0 errors; Level2Regression 1,147 PASS; Guide 714; Controls 5,734; Progression PASS; Dialogue 799; Training 513; Settings 93; CareerBoard 69; Intro 29; VerifyLevel2Assets, VerifyLevel2Scene (existing warning), VerifyGuideTargets, VerifyOfficeNpcMarkers PASS.

Pending user checks: at Rosa's desk there is no Exit Desk during practice or the challenge, and it comes back afterwards. Arthur's lines are at the bottom and the paper and highlighter are fully visible. Touching or dragging the Tax Code Book keeps it red. Store the folder in its icon, then drag it onto the desk (and tap the icon at the desk). An approved hand-in shows the Audit Summary, then the case reward and Level Complete. A returned one shows the summary with what to fix.


## Side quest finish and Auditor order — implemented; user Play Mode pending (2026-10-20)

Changed: Assets/Scripts/Level2/IncomeToolkit.cs (BoxScale, stowed bodies hidden, exit hold over the reward card and collection); Level2/Level2Audit.cs (TalkWhenFree, totals line, no on-time line); Tests/Level2Regression/Program.cs. Scratch build 0 errors; Level2Regression 1,149 PASS; the other suites and the Level 2 asset and scene validators PASS.

Pending user checks: no small cards in the case when the toolkit opens or packs up. No Exit Desk over Side Quest Complete; Continue does not leave the desk, and Exit Desk appears after the papers are collected. Hand in the folder: the Auditor talks first, then the Audit Summary, then the reward and Level Complete, with no "On time!" bubble.


## Leaving the records desk and the reasons page — implemented; user Play Mode pending (2026-10-20)

Changed: Assets/Scripts/Level2/IncomeToolkit.cs, Level2/Level2OfficeReview.cs, Data/FinalAssessment.cs, Resources/Levels/Level_02/book.json, Tests/Level2Regression/Program.cs. Scratch build 0 errors; Level2Regression 1,151 PASS; Guide, Controls, Progression and Training PASS; VerifyLevel2Assets PASS. Pending: press Continue on Side Quest Complete. The papers should fly to their icon and the player should stand up from the desk. Then check that the book page reads "Section 7 – Finding Reasons".


## Book back onto the desk; summary after the dialogue closes — implemented; user Play Mode pending (2026-10-20)

Changed: Assets/Scripts/Level2/Level2TaxCodeBookIcon.cs (OverOfficeDesk, ReturnToDesk), TaxCodeBook/TaxCodeBook3DDrag.cs (UnparkClosedBook, RestingClosedPosition), UI/DialogueUI.cs (read-only IsPanelShowing), Level2/Level2Audit.cs (AfterDialogueCloses), Tests/Level2Regression/Program.cs. Scratch build 0 errors; Level2Regression 1,153 PASS; Guide, Controls, Dialogue, Progression and Training PASS; VerifyLevel2Assets PASS. Pending: at the desk, drag the book from its icon onto the desk and it restacks; open it from the icon, drag it onto the desk and it restacks. Hand in the folder: the Audit Summary appears only after the last Auditor line is continued and the box is gone.


## Guided polish pass — implemented; user Play Mode pending (2026-10-20)

Owner: Claude (session 2026-10-20). The user asked for:
- a start at the office door, and the receptionist (not Arthur) telling the review cutoff;
- a better-looking cabinet drawer and retrieval animation, with Arthur introducing the file storage room;
- a Taxpayer Profile page after Taxpayer Information;
- Arthur guiding the case step by step: profile, Findings slip lift/fill/file, a stamp tutorial, the toolkit colours and the book sections;
- the side-quest case staying shut after it closes, and no Arthur line plus a visible Exit Desk when the finished records desk is revisited;
- annotated papers with no × button: a 2-second hold stacks them, a drop on the icon puts them away, and a drop elsewhere scatters them;
- detailed second-part tutorials and no office or receptionist arrows;
- a Tax Code Book that guides like a cheat sheet without giving answers;
- a fix for the Side Quest 2 medal missing from the 3D menu after Level Complete.

Changed paths:
- Start/receptionist: `Assets/Resources/Levels/Level_02/office_start.json` (blend 0.12); `Assets/Scripts/Level2/Level2OfficeStart.cs` (built-in blend; `GuideFlags` provider `Flag()`: folderRetrieved, bookStored, initialStamped, recordsCollected, questDone, officeReview, inFileRoom); `Assets/Scripts/Interactables/ReceptionistInteractable.cs` (`{deadline}` from the review clock); `case_rosa_001.json` (receptionist lines with the cutoff time; `clock.announceLine` empty).
- Drawer: `Assets/Scripts/Level2/Level2CaseRetrieval.cs` (cabinet-grey front with seam and lip, chrome pull, brass label holder with an orange MARKET tab, drawer walls and rails, five hanging client folders, Rosa's folder with a red tab and name card); `Assets/Scripts/Level2/MarketClientDrawer.cs` (the label glows while the folder waits; unlatch, roll out with overshoot, riffle, lift and turn to the camera, a short beat, then an arc and spin into the HUD icon while the drawer closes with a bump).
- Folder: `Assets/Scripts/Data/Definitions/CaseReviewDefinition.cs` (`profileText`); `Assets/Scripts/UI/CaseFolderUI.cs` (Taxpayer Profile page right after Taxpayer Information: a Name row and a printed "About the taxpayer" paragraph); `case_rosa_001.json` (`profileText`: one sentence with clues for Sections 1-3).
- Guide engine (observation only): `Assets/Scripts/Core/GameplayEvents.cs` (`OnGuideCue` / `RaiseGuideCue`); `Assets/Scripts/UI/Guide/GuideTriggers.cs` (`cue:<name>` events, `flag:<name>` conditions, `GuideFlags`); `Assets/Scripts/UI/Guide/GuideDirector.cs`. Cues are raised in `Level2FindingsSheet` (FindingsPaperLifted, FindingsPaperFiled), `Level2DeskStamps` (StampsUp), `Level2TaxCodeBookIcon` (TaxBookStored), `Level2AnnotatedPapers` (AnnotatedPapersOpened, AnnotatedPapersStacked, AnnotatedPapersPutAway) and `Level2OfficeReview` (ScheduleLineFilled, OfficeConclusionSet).
- Guide content: `Assets/Resources/Tutorials/Level_02/guide.json` rewritten as 21 lessons: welcome, file room introduction, profile, prepared pages, Findings slip (lift, fill, file), stamp tutorial, to Rosa, toolkit colours with book sections, practice, challenge, red notes, annotated papers with the stack gesture, Reviewed Schedule, final assessment, Auditor and returned folder. Waypoints remain only for Rosa's stall and records desk; `trail.waitForGuide` is off.
- Book: `Assets/Resources/Levels/Level_02/book.json`. Sections 4-8 are rewritten to answer the questions the player meets, headings name what they help with, and a new Section 9 covers the Final Assessment. Findings hints, schedule and conclusion refusals, and Auditor feedback name the section to read (`Level2FindingsSheet`, `Level2OfficeReview`, `case_rosa_001.json` audit feedback).
- Side quest: `Assets/Scripts/Level2/IncomeToolkit.cs`. The lid no longer reopens after pack-up. A finished desk stays shut with no Arthur line, and the bottom hint is off once the quest is done, so Exit Desk is clear.
- Annotated papers: `Assets/Scripts/Level2/Level2AnnotatedPapers.cs` (hold 2 s to stack under the finger, with a fill ring and a label; the icon pulses; drop on the icon to put them away, anywhere else to scatter them back); `Assets/Scripts/Level2/AnnotationPaperCollectionFace.cs` (no × button; a long press does not count as a tap on an amount).
- Career: `Assets/Resources/Career/chapters.json` lists Side Quest 2, Mark the Money!, for Level_02, so its medal, tier and time show on the 3D menu. `Assets/Scripts/Data/CaseProgressionManager.cs` starts the side-quest clock at Rosa's records desk.
- Tests: `Tests/Level2Regression/Program.cs` (old arrow, hint and book-count checks updated; 66 new checks); `Tests/VerifyLevel2Scene.py` (runtime-built `Level2Gameplay/` guide points are listed, not failed).

Internal verification (2026-10-20):

| Check | Result |
|---|---|
| `python Tests/make_scratch_build.py` + scratch build | 0 errors (29 baseline warnings) |
| Level2Regression | 1,226 PASS |
| GuideRegression | 714 PASS |
| ControlsRegression | 5,734 PASS |
| ProgressionRegression | PASS |
| DialogueRegression | 799 PASS |
| TrainingRegression | 513/513 |
| SettingsRegression | 93/93 |
| CareerBoardRegression | 69/69 |
| VerifyLevel2Assets | PASS (11,796 GUIDs) |
| VerifyLevel2Scene | PASS (existing Level1-drift warning) |
| VerifyGuideTargets | 17 paths, 0 missing |
| VerifyOfficeNpcMarkers | PASS |

No scene, prefab or .meta file was edited. No git command was run, and Unity Play Mode was not run.

Pending user Play Mode checks:
1. Level 2 starts just inside the office door, facing the receptionist. Her talk names the cutoff time; Arthur does not. There are no floor arrows, beacon or minimap pin to the receptionist, cabinet, desk or Auditor.
2. Walking into the file storage room starts Arthur's introduction. The MARKET CLIENTS drawer matches the cabinet, and its label glows softly. Taking the folder plays the full sequence: roll out, riffle, Rosa's folder rising and turning to show her name, then flying into the folder icon while the drawer closes.
3. The folder's second page is Taxpayer Profile, with one readable sentence. Arthur sends you to Sections 1-3 and waits for the three fields.
4. Arthur walks through Summary, Payment Note and Findings. Lifting, filling and filing the slip each move him on. The stamp lesson waits for the stamps to rise.
5. Toolkit: after the demo, Arthur explains yellow, blue, orange and purple and points at the book icon. After pack-up the case stays shut. Revisiting the finished desk shows no Arthur line, and Exit Desk is visible.
6. Annotated papers: there is no ×. Hold a paper still for 2 s: the ring fills, the papers stack under the finger and the icon pulses. Drop on the icon: they all fly in. Drop elsewhere: they scatter back. A quick tap on an amount still selects it.
7. Office: Arthur teaches the Reviewed Schedule, the conclusion and the final stamp, naming Sections 6, 8 and 9. Book Sections 4-9 fit their pages.
8. Finish the level and press Continue: the 3D menu shows the Mark the Money! badge (side quest 2) with its tier and time.

Next: user Play Mode acceptance; tune only reproduced visual or timing issues.


## Follow-up fixes after first Play Mode feedback — implemented; user Play Mode pending (2026-10-20)

Owner: Claude (same session). User feedback: profile text too small; the Auditor repeated his lines before the Audit Summary; Next Goal stuck on a passed chapter; laggy drags; Arthur stopped guiding after the side quest; start closer to the door.

- **Arthur went silent after the side quest.** A "your turn" page only accepts events that happen after it appears. When the quest finished during Arthur's tap pages (for example all notes right on the first CHECK), `mark-the-money-fix` waited forever and blocked every later lesson. Every such wait now also accepts the done state (`flag:` firstChecked, questDone, officeDone, finalStamped, initialStamped, findingRecorded, tutorialDone, stampsUp). The red-notes lesson only runs when there are red notes (`flag:redNotes`), and the annotated-papers lesson follows `mark-the-money` directly and ends with a "head back to your desk" hint. Files: `Level2OfficeStart.cs` (`Flag`), `Tutorials/Level_02/guide.json`.
- **Auditor repeated himself.** `AuditorSubmissionTray` called `auditor.BeginFinalAudit()` twice. Level 1 ignored the second call; Level 2's audit queued a second conversation, then the summary popped in. The call now runs once, and `Level2Audit` handles one hand-in at a time (`inProgress`).
- **Next Goal.** When every open chapter is passed, the goal moves on to the next chapter (coming soon) instead of the last passed one; the card shows career totals and a "coming soon, replay for a better best or gold badge" line (`CareerProgress.FindGoal` / `NextStep`, `CareerBoardPresenter.ApplyGoalCard`).
- **Profile text** auto-sizes from 26 to 44 pt into the space left on the page (truncates rather than overflowing) (`CaseFolderUI.BuildProfileRows`).
- **Start position.** `Doors` holds both the street entrance and an inner door pair, so its centre was mid-room. The spawn is now the entrance leaf `Doors/Office_Entrance_Door_2`, blend 0.16 (about 1 m inside), with the old point as fallback (`office_start.json`, `Level2OfficeStart`).
- **Smoother touches.** About 110 per-touch `Debug.Log` calls in the window, drag/drop, value transfer, interactor and camera scripts now go through `DebugTrace.Log` (new `Assets/Scripts/Core/DebugTrace.cs`, compiled out unless the `TAXVENTURE_TRACE` define is set). Warnings and errors are unchanged. No per-frame scene searches remain in Level 2, HUD, book or guide update loops.

Internal verification: scratch build 0 errors (29 baseline warnings); Level2Regression 1,240 PASS; GuideRegression 714; ControlsRegression 5,734; ProgressionRegression PASS; DialogueRegression 799; TrainingRegression 513/513; SettingsRegression 93/93; CareerBoardRegression 69/69 (goal check updated); IntroCutsceneRegression 29/29; VerifyLevel2Assets PASS; VerifyLevel2Scene PASS (new warning: the entrance leaf is inside a prefab instance, so the file check cannot prove it; the fallback covers it); VerifyGuideTargets 0 missing. No git, no Unity.

Pending user checks: the profile sentence is large and stays on the page; the start is about 1 m inside the entrance; one Auditor conversation, then the summary; Arthur keeps guiding after Mark the Money! (also with all notes right first time); the Next Goal reads Chapter 3 coming soon once Chapter 2 is passed; drags feel smoother in the Editor.


## Third feedback round — implemented; user Play Mode pending (2026-10-20)

User feedback: a floor path to the file storage room; the profile text overlapped; a laggy desk (book, folder, stamps, page turns); page arrows too small; Notes need drag/drop animation; the drop-field "fits" mark was not a check; skip the side-quest practice; Arthur's lines overlapped the lesson box; the Findings slip cleared answers; and the Side Quest Complete Continue could not be pressed.

- **Continue could not be pressed.** No blocker was proven from source. As a safety net, `HudRewardScreen.LateUpdate` presses Continue itself when a tap starts and ends on the button but the EventSystem click did not arrive. Normal clicks are never doubled (same-frame guard).
- **Overlapping dialogue.** `GuideDirector.Say` holds Arthur's short lines while a lesson box is up and shows the latest one once it is dismissed. At the records desk the lesson box sits at the top (`box: top`), so the TYPE/TREATMENT/RECORD slots stay visible.
- **Skip practice.** At the records desk, a `SKIP PRACTICE` button shows during Arthur's payslip demo and the practice. It calls `CaseReviewState.TrySkipTutorial()` (Challenge phase, no score), raises the same guide events and deals Rosa's records. Arthur's colour lesson still plays.
- **Findings slip.** A complete but wrong link no longer clears the three boxes. The slip shows "No link yet" with a clue for the first wrong box (`FindingComparison.FirstMismatch` / `Clue`; the reason clue names Section 7, never the answer). The domain `ClearFindingDraft` is kept for older callers and tests but is no longer called by the page.
- **Profile page.** A shorter sentence (one sentence, same clues) and a fixed 250-unit row. The earlier list-sized row overflowed onto the Name row.
- **File storage room path.** `event:CaseFolderRetrieved` has a waypoint again (`Filing_Cabinet_1`). Receptionist, desk and Auditor still have none.
- **Check mark.** The drag aim tag shows a green round check (`HudTheme.Check`) when the value fits, with a pop. A correct tap-placement also pops a check on the field (`ValueTypeValidationFeedback.PopCheck`).
- **Page arrows.** The disc is 98 units (was 74), the folder button 150, and `PageEdgeTapZone` adds an invisible touch pad 1.5x the button plus a press squash and spring (folder and book).
- **Smoothness.**
  - Page turns cap each frame step (`DeskMotion.Step`) and skip the content-rebuild frame, so the second half no longer jumps.
  - The desk folder and book always use a stiff exponential follow (they snapped rigidly once moved).
  - The value tag pops when picked up, follows smoothly and leans with the motion. The aim frame uses the finger point (`DragVisualManager.PointerScreen`).
  - Paper windows (Notes, profiles, Findings, annotated papers) lift and lean while dragged and settle with a soft spring and a paper sound when dropped. Notes already flew out of and into their icon.
- **Findings slip and folder.** A loose slip tucks away when the folder closes and pops back when it reopens, instead of blinking.

Internal verification: scratch build 0 errors (29 baseline warnings); Level2Regression 1,253 PASS; GuideRegression 714; ControlsRegression 5,734; ProgressionRegression PASS; DialogueRegression 799; TrainingRegression 513/513; SettingsRegression 93/93; CareerBoardRegression 69/69; IntroCutsceneRegression 29/29; VerifyLevel2Assets PASS; VerifyLevel2Scene PASS (2 known warnings); VerifyGuideTargets 0 missing. No git, no Unity.

Pending user checks:
1. Side Quest Complete: Continue works.
2. No Arthur line covers a lesson box.
3. SKIP PRACTICE at Rosa's desk.
4. A wrong Findings link keeps the boxes and shows a clue.
5. The profile is readable with no overlap.
6. The floor path to the file room shows on the first trip.
7. Green check when a value fits.
8. Bigger page arrows.
9. Smoother page turns and drags.
10. Notes and other papers settle when dropped.
11. Closing and opening the folder with the slip out.


## Annotated paper layering and rows — implemented; user Play Mode pending (2026-10-20)

- **Layering.** Paper windows and 3D document layers now take sorting orders two apart (`FloatingWindow.LayerStep`). Each annotated paper's highlighted-amount band gets its own canvas at `window order + 1`: over its own paper, under any paper brought in front. Before, the band and the 3D face shared one order. The dragged value tag (`DragVisualManager.TopSortingOrder` 30000) and the aim frame (29000) draw over every paper.
- **Rows.** Annotated paper rows are taller (up to 0.05, still ending above the card slots for any row count) with bigger print (0.25 for four or fewer rows). Long values such as "Washing clothes for customers" shrink to fit (minimum 0.15) or wrap, instead of being cut off. This also applies at Rosa's records desk, which uses the same paper.
- **Feel.** An amount that lands on the schedule pops as it turns green. Paper open, close and close-to-icon motion uses the capped frame step (`DeskMotion.Step`), so a slow first capture frame no longer makes papers jump.
- **Findings slip.** Long picks (for example "Payment for: Blanket washing") shrink to fit their box (minimum 21 instead of 29).

Internal verification: scratch build 0 errors; Level2Regression 1,265 PASS; the other suites and Level 2 validators PASS. No git, no Unity.

Pending user checks:
1. Drag a highlighted amount across the papers: the tag stays on top.
2. Bring each paper to the front: its band follows it and is never hidden by its own paper.
3. The rows are bigger and fit inside each paper, here and at Rosa's desk.
4. A long Clue on the Findings slip is fully readable.


## Drag tag on top, bigger value fields — implemented; user Play Mode pending (2026-10-20)

- **Dragged value behind the papers (still reported).** The nested canvas added in `Awake` (while the tag was hidden) did not hold its sorting. `DragVisualManager.KeepOnTop` now moves the tag, the first time it shows, onto its own top-level overlay canvas ("Drag Value Overlay", sorting 30000) with the HUD's `CanvasScaler` settings. Top-level overlay canvases always draw in sorting order, so the tag is above every paper, the folder and the book, wherever the drag starts. The duplicate pop and lean from the last pass were removed: `DragGhostSkin` already animates the tag, and the two fought.
- **Bigger value fields.**
  - **Case folder:** `CaseFolderUI.FitRowHeights` grows each value row into the page's spare room, up to 110 units. Tall custom rows (profile, handwritten summary) keep their size, so nothing runs past the page.
  - **Tax Code Book:** value chips are 68 tall (was 58) with up to 44 pt text. On a page that would still overflow at the smallest captions, they fall back to 58 (`TaxCodeBookUI.FitDescriptions`). This is shared with Level 1's book.

Internal verification: scratch build 0 errors; Level2Regression 1,267 PASS; other suites and validators PASS. No git, no Unity.

Pending user checks:
1. Drag an amount from each annotated paper and from the folder/book: the tag is always on top.
2. Folder rows are taller on every page and stay inside it.
3. Book chips are taller on Sections 1-9 with descriptions still inside the page.


## Outside environment from Level 1, Rosa at Zorbix's spot, model swap — implemented; user run + Play Mode pending (2026-10-20)

User direction: give Level 2 Level 1's new outside environment (the teammate's environment update), keeping the Level 2 office, logic, side quest and data. Leave out Level 1's NPCs. Put Rosa where Zorbix stands in Level 1, with the side-quest table and stall sign close to her and no shop building. Swap Rosa's and Marie's 3D models only.

- New editor tool `Assets/Editor/Level2EnvironmentSync.cs` (+ .meta, unique GUID), menu **TaxVenture > Level 2 > Copy Outside Environment From Level 1**:
  - Replaces Office_Level2's `Env` root with Office_Level1's, using Unity's copy/paste so prefab links stay. Objects named `npc_*` or `Elijah*` are removed from the copy.
  - Matches the transforms of `Environment/Outside`, `Plaza`, `Floor` and `TownProgression` to Level 1.
  - Places `Environment/Outside/Plaza/RosaStallAnchor` at NPC_Zorbix's Level 1 pose.
  - Swaps Level 1 Marie's model for `SM_Chr_Business_Female_03` (same height, feet and idle controller; references to the old model are remapped).
  - Sets `scene_assets.rosaPrefab` and saves both scenes.
  - Safe to re-run. It never touches the office building, Level 2 gameplay or data files.
- `Level2RosaStall`: with the anchor, Rosa stands on it facing its forward, the records desk is 1.7 m to her right and the sign 1.5 m to her left, and no shop is built. Without the anchor, the old plaza search and shop still apply. Rosa's model is fitted to `RosaHeight` 1.62 m (the cora FBX imports large).
- `scene_assets.asset`: `rosaPrefab` now points at `npc_cora_static@Standing Idle.fbx`; the idle controller is unchanged.
- Tests: Level2Regression 1,275 PASS (scene-asset check updated, 7 new checks); VerifyLevel2Assets PASS; the editor tool compiles against UnityEditor.CoreModule.

Pending user steps:
1. Close any open scene changes, then run the menu item once in Unity. Read the summary dialog.
2. Play Level 2: the outside looks like Level 1 with no Level 1 NPCs. Rosa stands at Zorbix's spot wearing Marie's former model, with her desk and sign beside her, and the side quest works.
3. Play Level 1: Marie wears the business-woman model, idles, talks and gives her profile as before.
4. Commit both scenes, `scene_assets.asset` and the new tool.


## Rosa's stall at the town's Building_4 — implemented; user Play Mode pending (2026-10-20)

User feedback after running the environment tool: at Zorbix's spot the sign went into `Building_4 (1)` and Rosa faced the wrong way. The user asked to put the stall at `Env/Town/Building_4`.

- `Level2RosaStall` now places the stall in this order:
  1. the town's `Env/Town/Building_4` (`SceneStorePath`) when the scene has it;
  2. otherwise the `RosaStallAnchor`;
  3. otherwise the old plaza search with its own shop.
- At the town shop:
  - the front is its awning side, measured on the building's own axes from its colliders;
  - the records table stands about 1.6 m out front, and Rosa stands about 1.1 m out, to the other side;
  - the sign tries six spots and takes the first one that touches no solid collider and is at least 1.4 m from Rosa and the table.
- Rosa now faces her records table in both layouts (`FacingTo`). She still turns to the player while talking (`RosaInteractable.FacePlayer`).
- The town's building is only referenced, never moved or destroyed. `RosaStallAnchor` stays in the scene as the fallback.

Checks: scratch build 0 errors; Level2Regression 1,279 PASS; VerifyLevel2Assets PASS. Pending Play Mode: the stall is in front of Building_4 and the sign stands free. If Building_4 has no colliders, the free-spot check cannot see it; then move the sign by nudging the building or report it. Rosa looks at the table until you talk to her.


## Rosa's stall on Building_4's real front, by the door (2026-10-20)

- Cause of the side-wall placement: Building_4's awning sits 56-73% up the building, and the front detection only probed at 55%, so it never saw it. I read the mesh (`Flat_Style_Vehicles/Art/Meshes/Buildings/4.FBX`). It is Z-up, the awning sticks out on the FBX -Y face, and on that face the window and ledge are on the left with the door on the right, as seen from the street.
- `DetectFrontAround` now probes at 60, 66 and 72% of the height. The earlier "flip to the other side" is removed.
- Measurements use the shop's own mesh bounds and axes, not its rotated world box.
- Layout:
  - Rosa stands 1.1 m out, just beside the door, facing her records table;
  - the table is about 2 m out, in front of the window;
  - the sign goes on the first free spot on the window side, clear of Rosa, the table and the doorway.
- Mirrored shops (negative X scale) put the door on the other side automatically.

Level2Regression 1,280 PASS. Pending Play Mode: Rosa is under the awning beside the door, and the table and sign are in front of the shop.

## 2026-10-20: balanced scene lighting and matte toolkit surfaces

Owner/session: Codex settings/fresh-menu follow-up. Status: implemented, internally checked; user visual/Play Mode acceptance pending. Touched Level2OfficeStart.cs, AnnotationPaper.cs and Tests/Level2Regression/Program.cs.

Found two active directionals illuminating every layer (outdoor key 1.15 and office fill 1). Level2OfficeStart now sets the scene-owned key to 0.95 with softer shadows and the fill to 0.28, retaining ambient/sky fill. Toolkit paper/plastic materials are matte, with specular highlights disabled where supported. No scene YAML, global RenderSettings, shared assets or Level 1 lighting was changed.

Scratch compile: 0 errors / 29 baseline warnings; Level2Regression 1,283 PASS (including bounded combined light intensity, scene-local scope and matte materials). Next step: user checks the office, outdoor market toolkit and collected-paper reading views for balanced brightness. Exact values and handoff: Documentation/SettingsActions/FreshMenuAndLighting.md.

## 2026-10-21: T02 folder icon and drag regression repair

Owner/session: Codex folder repair. Status: implemented — internally verified; user Play Mode/feel acceptance pending.

The shared folder icon unconditionally unlocked in Awake, so the empty Level 2 icon could activate the hidden desk model before drawer retrieval. The HUD entrance also activated that model after setting its miniature icon pose, allowing FloatingModel3D.Awake to cache the miniature scale as its drag rest size. Disabled/stored models retained drag targets, leaving a second transform writer alive during subsequent presentation.

Repair:
- HudIconButton starts the Case Folder locked, preserves other icons' existing startup behavior, and unlocks on actual folder opening or completed storage. Drawer retrieval already raises the storage event when the folder reaches the icon.
- CaseFolder3DView initializes the hidden model at its authored pose and parent before detaching it and applying the temporary entrance pose.
- CaseFolder3DDrag completes existing desk motion and restores the authored open rotation/scale before opening again. Existing easing, durations and pointer-follow logic are retained.
- FloatingModel3D clears drag/target flags on disable and when dragging is handed to another owner.
- Level2FolderHandoff restores the icon's UI availability for a resumed folder already placed on the desk, without replaying first-open gameplay events.

Files: Assets/Scripts/UI/HUD/HudIconButton.cs, Assets/Scripts/UI/HUD/CaseFolder3DView.cs, Assets/Scripts/CaseFolder/CaseFolder3DDrag.cs, Assets/Scripts/CaseFolder/FloatingModel3D.cs, Assets/Scripts/Level2/Level2FolderHandoff.cs, Tests/Level2Regression/Program.cs; this pack's T02/SPEC/KNOWN_ISSUES notes. No scene/prefab YAML or assets were changed.

Verification: five targeted source contracts failed before the repair; those now pass, along with a resume contract and two managed production drag-cancellation checks (8 folder checks total). Scratch build: 0 errors / 29 baseline warnings. Level2Regression: 1,291 PASS. AudioRegression: 205 PASS; GuideRegression: 733 PASS; TrainingRegression: 514 PASS. VerifyLevel2Assets: PASS (11,788 unique meta GUIDs; 42 Level 2 scripts and 7 content files). Managed cancellation checks bypass the MonoBehaviour constructor and touch no native Unity objects; source-order checks do not establish visual smoothness. A temporary text-encoding issue in the test update was corrected; the full Level 2 suite passes.

Next: user starts a fresh Office_Level2 run, confirms the empty icon is locked, retrieves Rosa's folder, taps and drags it onto the desk, grabs/releases/reopens it at normal size, stores it and opens it again, and checks an already-placed checkpoint. Also check a normal Level 1 folder open/store/reopen. Unity Play Mode, device rendering and touch feel were not run by Codex.

## 2026-10-21: paper icon/card-transfer mix follow-up

Owner/session: Codex audio mapping follow-up. Status: implemented — internally verified; user Unity listening/feel acceptance pending. User requested paper icon variation, exact card pickup/place/return recordings for value transfers and Side Quest 2, quieter BGM and audible ambience. See AUDIO_TUNING.md for the step-by-step mapping and Unity checks.

Changed AudioManager (document pools, exact-recording API, music 3.5%/ambience 45%), AudioEnvironment (Env ground ancestry), NotesPanelUI, HudProfileCards, CaseFolderUI, Level2TaxCodeBookIcon, Level2AnnotatedPapers, ValueTransferManager, ValueDragSource, IncomeToolkit; Resources/TaxVentureAudioLibrary.asset now assigns two louder ambience playback copies under Assets/AudioManager/MixedAmbience, preserving source WAVs. Tests/AudioRegression has named-variant, icon-variation and action-hook checks. Asset metadata uses new unique GUIDs; scenes and prefabs were not edited.

Scratch build: 0 errors / 29 baseline warnings. AudioRegression: 221 PASS. Level2Regression: 1,291 PASS including all eight folder checks; GuideRegression: 733 PASS. TrainingRegression: 514 PASS; VerifyLevel2Assets: PASS (11,793 unique .meta GUIDs). Source audio measurements verified the originals are quiet and playback-copy peak headroom is safe; no Unity playback/device test was performed. Next: follow AUDIO_TUNING.md in Unity, checking SFX and Music independently and both office/outdoor beds.

## 2026-10-21: T02 reuse Level 1 folder presentation

Owner/session: Codex folder reuse follow-up. Status: implemented — internally verified; user Play Mode pending.

Removed Level2FolderHandoff's custom unstore/desk-reopen path. The first opening still requires the office desk (and a desk-surface drop for dragging), then forwards to the exact OfficeHudRuntime callbacks used in Level 1. Only a successful shared opening records first placement. Stored ownership/count stays 1 while the view is borrowed, so another tap closes it and dragging the open folder onto the icon animates its return. Shared entrance/return durations, easing, model and grab logic are retained.

HudIconButton now synchronizes folder availability on opened/stored/returned events using the real stored count; resume no longer unlocks an empty icon. HudSubmittableIcon gates pointer input and tap/drop dispatch by count as well as availability. CaseFolder3DView also rejects a direct open with count zero. Other document kinds retain their existing availability rules.

Files: Assets/Scripts/Level2/Level2FolderHandoff.cs; Assets/Scripts/UI/HUD/HudIconButton.cs, HudSubmittableIcon.cs, CaseFolder3DView.cs; Tests/Level2Regression/Program.cs and Level2Regression.csproj (InputLegacyModule reference for managed pointer-dispatch checks); this pack's PROGRESS, SPEC, KNOWN_ISSUES and T02. No scene/prefab edits.

Verification: two new source contracts failed before the fix. Scratch compile: 0 errors / 29 baseline warnings. Level2Regression: 1,299 PASS, including 16 folder checks and production managed pointer-down/toggle dispatch for empty/stored/locked icons and unaffected Tax Return availability. GuideRegression: 733 PASS; AudioRegression: 221 PASS; TrainingRegression: 514 PASS. VerifyLevel2Assets: PASS (11,793 unique metadata GUIDs, 42 scripts, 7 content files). An initial managed-pointer test run needed UnityEngine.InputLegacyModule referenced in the runner; after adding that existing Unity assembly the complete suite passes.

Next: user starts Office_Level2 fresh and confirms no badge means taps/drags do nothing; retrieves the folder (badge 1), sits at the office desk, taps open/closed repeatedly, drags out to different desk spots, then drags the open cover back onto the icon. Check full size and smooth animations, initial access blocked at Rosa's desk, checkpoint folder on the real desk with empty icon blocked until stored, and the same store/toggle/drag loop in Level 1. No Unity Play Mode or visual/feel verification was performed.

## 2026-10-21: secure folder withdrawal (user correction)

Owner/session: Codex folder withdrawal and intro routing. Status: implemented — internally verified; user Play Mode pending. Supersedes the previous requirement that dragging a view out retain badge 1.

A successful icon drag-out withdraws the count through FolderToHUDIcon.TryTakeOutForView, without resetting the animated model. Count 0 hides the existing badge and disables pointer/drag dispatch. The existing store reserves the same folder so duplicate withdrawal cannot mint another one; IsFolderStored is false during withdrawal. Dragging an already-previewed cover also withdraws it. Dropping the cover onto the icon runs the existing closing animation, then Restore returns exactly one count through ReturnFromView. A mode-change restore also returns that same folder. An untouched click preview retains the previous tap-to-show/hide behavior. FloatingFolderGrab owns the return, preventing FolderToHUDIcon's ordinary cover handler from prematurely hiding the animated model. Initial Level 2 desk placement is recorded after a successful shared opening even though its drag has now removed the badge.

Changed: Assets/Scripts/CaseFolder/FolderToHUDIcon.cs, Assets/Scripts/UI/HUD/CaseFolder3DView.cs, Assets/Scripts/Level2/Level2FolderHandoff.cs, Tests/Level2Regression/Program.cs and this pack's notes. Cutscene changes are separately recorded in Documentation/IntroCutscene01/PROGRESS.md.

Checks: the new withdrawal contract failed before implementation. Scratch build 0 errors / 29 baseline warnings; Level2Regression 1,305 PASS (22 folder checks including production managed withdrawal/duplicate/return checks), IntroCutscene 44, CareerBoard 72, Audio 222, Guide 733, Settings 133, Controls 5,734, Dialogue 799, Training 514, Progression PASS. VerifyLevel2Assets PASS; VerifyLevel2Scene PASS (two existing warnings); VerifyGuideTargets 17 paths, zero missing.

Next: user retrieves badge 1, drags the folder out, confirms the badge disappears and the icon cannot supply another folder, then returns the open cover to the icon and confirms the animation finishes before badge 1 and interaction return. Repeat and check ordinary click preview, initial desk progression, mode changes and Level 1. Unity visual/feel acceptance remains pending.

## 2026-10-21: shared folder stuck/edge-on viewer repair

Owner/session: Codex complete folder viewer follow-up. Status: implemented — internally verified; user Unity Play Mode pending. User's new screenshot is Office_Level1; this repair applies to the shared viewer used in both office levels.

Root causes: CaseFolder3DView moved the real model while CaseFolderUI.ShowAsWindow also started a flat FloatingWindow animation; camera framing reused a desk-relative pose rather than fitting the printed page. The HUD player hold disables the main physics raycaster, leaving a withdrawn 3D cover unreachable for return. Paper/owner deactivation did not reliably release its hold. Physical desk opening also used a global LockPlayerControls call without an owned disable-time release. A rejected 3D drop-open could fall back to the old flat viewer.

Repair:
- CaseFolderUI.ShowFor3DView/HideFor3DView render the real paper, pause its flat controller, route existing Hide/swipe closure to the 3D owner and release the hold on cleanup. The 3D model remains the single entrance/return animation owner.
- The page faces the camera and fits within 68 percent of both viewport axes, with drop endpoints clamped inside the view. Original parent/local scale/rest pose are restored; animation uses the cached world scale while detached. Mid-entrance grabs settle before computing their drag offset.
- DocumentPhysicsRaycaster temporarily admits only hits under the held folder, keeping cover drags usable while the ordinary world raycaster remains disabled. It is disabled/removed before releasing the hold. Desk-only return handlers are paused during HUD viewing.
- Physical desk Show uses an owned HudWindowLock hold, released on Hide/disable/storage, rather than leaving a global lock behind. Ordinary workstation mode remains managed by CameraController. The disabled flat controller no longer reports itself as an open window to layering.
- View disable, focus loss, paper deactivation and close restore the folder and its withdrawn count. Drop-open rejection is consumed by OfficeHudRuntime, so it cannot launch a second viewer. Existing badge/withdrawal/no-duplicate behavior is retained.

Changed: Assets/Scripts/UI/HUD/CaseFolder3DView.cs, OfficeHudRuntime.cs, FloatingWindow.cs; new DocumentPhysicsRaycaster.cs and unique metadata; Assets/Scripts/UI/CaseFolderUI.cs; Tests/Level2Regression/Program.cs; this pack's PROGRESS/SPEC/KNOWN_ISSUES/T02 and the HUD pack's PROGRESS/KNOWN_ISSUES. No scene/prefab YAML, camera controller, save or case-content changes.

Verification: three new viewer contracts and a rejected-drop fallback check failed before their fixes. Scratch compile: 0 errors / 29 baseline warnings. GuideRegression 733, AudioRegression 222, TrainingRegression 514 and SettingsRegression 133 PASS. VerifyLevel2Assets PASS (11,794 unique metadata GUIDs, 42 Level 2 scripts, 7 content files). Final Level2Regression: 1,324 PASS, including 41 folder checks. Page-fit checks execute production math across landscape/portrait aspect ratios and several fields of view; storage checks execute production managed ownership methods. Pointer filtering/lifecycle checks inspect source contracts, and do not establish native Unity rendering, drag reachability or control restoration.

Next: user stops the existing Play session and allows Unity to import/compile the new raycaster, then starts a fresh Level 1 run. Store the real folder; tap open and close repeatedly; confirm the full page faces the camera. Drag it out, confirm badge 0 and no duplicate withdrawal, return by its cover, and confirm badge 1 only after the return animation. Test existing swipe close, a mid-entrance grab, storing the desk-open folder, switching desk view and focus loss, and a Notes window open at the same time. Confirm movement returns only after the last paper closes. Repeat initial placement and the same loop in Level 2. User performs Play Mode/device checks; none was run by Codex.
