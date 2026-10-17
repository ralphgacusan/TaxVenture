# Progress and handoff

Updated 2026-10-17.

## Current state

T01 and T02: T02 is user accepted. T03 and T04 are implemented and internally verified; the user tests both in one Play Mode run. T05 is next. See the handoff sections below.

| Ticket | Owner | Source/internal status | User Play Mode |
|---|---|---|---|
| 01 | Claude (session 2026-10-17) | Implemented — internally verified | Partly: user reached the empty cabinet (expected before T02) |
| 02 | Claude (session 2026-10-17) | User accepted | PASS (2026-10-17): drawer, flight to the HUD icon, drop-to-open on the desk, bottom page buttons and page text all work |
| 03 | Claude (session 2026-10-17) | Implemented — internally verified | Pending (tested together with T04) |
| 04 | Claude (session 2026-10-17) | Implemented — internally verified | Pending (tested together with T03) |
| 05 | Unassigned | Planned | Not run |
| 06 | Unassigned | Planned | Not run |
| 07 | Unassigned | Planned | Not run |
| 08 | Unassigned | Planned | Not run |
| 09 | Unassigned | Planned | Not run |
| 10 | Unassigned | Planned | Not run |
| 11 | Unassigned | Planned | Not run |
| 12 | Unassigned | Planned | Not run |

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
