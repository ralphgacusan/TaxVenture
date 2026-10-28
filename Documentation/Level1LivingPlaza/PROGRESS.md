# Progress

## 2026-10-28: page-turn, HUD and reward presentation polish

Implemented at the user's request; Unity visual acceptance remains pending:
- Shared folder/tax-book print fades from 34 to 76 degrees before edge-on compression. Page copies resolve and restore their RectTransform layout after removing layout drivers, ignore parent layout, and have a subtle paper shadow. Delayed page commit, rapid navigation and close/disable completion stay intact.
- The progression HUD has a distinct coin icon and wallet row with a counting balance, EXP star, and REP/TRUST captions. Label/bar spacing is preserved and level captions refresh from current data.
- Case, chapter and side-quest reward cards use coin/heart tiles with separate earned and balance values instead of arrow strings. Only positive applied grants fly. Coins land at the wallet icon after the HUD returns; new case commits queue once, while chapter aggregate summaries and resumed results do not replay grants.
- Office hours awards no on-time bonus. Late penalties, day recovery and maximum late days remain unchanged. Updated the audit/result wording, settings notes and the one guide sentence promising a bonus; retained legacy public fields/state compatibility without awarding anything.
- Focused checks: `VerifyHudRewardPresentation.py` (12 checks) and `VerifyOfficeHoursConsequences.py` pass. Page-turn suite passes 77/77. Independent review found and verified the fix for a coin-only grant incorrectly emitting a default zero-value trust heart.
- Scratch compile: 0 errors / 29 existing warnings. Regression results: CampaignFoundation 675/675, Guide 907, Level2 1332 plus 41 folder checks, CampaignFoundationRuntime 16/16, Dialogue 1098 assertions, and Progression PASS. GuideTargets (14 paths), OfficeNpcMarkers (24), Level2Assets (11,881 unique GUIDs), and HudIcons pass.
- Modified runtime files: `UI/Books/PageTurnStage.cs`, `UI/Books/PageTurnMath.cs`, `UI/HUD/HudTheme.cs`, `UI/HUD/HudProgressionPanel.cs`, `UI/HUD/HudFeedback.cs`, `UI/HUD/HudRewardScreen.cs`, `UI/HUD/OfficeHours.cs`, `UI/LevelCompleteUI.cs`, `UI/AuditSummaryPopupUI.cs`, `SideQuest1/SideQuestCompleteUI.cs`; office-hours JSON and the single guide sentence. No scene changes were made for this request.
- The on-time-removal worker's patch tool timed out before saving. The root completed that bounded edit and verified it. Animation and reward workers completed; independent review has no outstanding material findings.

Next: user Play Mode acceptance in both folder/book directions, rapid taps, boundaries and closing mid-turn; attached records; side-quest/case/chapter card fit; coin flight and balance timing after closing the card; on-time submission and late-day penalties. Attached 3D records hide as a group at 76 degrees rather than individually fading. No new ticket was started.

## 2026-10-27: deferred page reveal, plaza discovery and corkboard polish

Implemented at the user's request; visual acceptance remains pending:
- Folder and Tax Code Book commit destination print and page index after the leaf lands. Forward turns show source print over a blank backing; backward turns cover source print with a blank leaf. Rapid navigation settles the pending turn before selecting the next page; closing/disable commits once and clears copies immediately.
- Red regression: two page-reveal/completion contracts failed before the fix; the same Level1 suite passed 75/75 afterward. This is a source-contract check, not a rendered visual reproduction.
- Removed only the four-ID collection waypoint. Blue trails, beacons and minimap objectives still lead to Arthur initially and after all four IDs. The exploration objective and fallback now invite hearing the plaza's stories rather than naming the four clients.
- Ambient people now receive the same triangle/check markers and generic minimap person icons. Successful conversations set a scene-local check; cancellation does not. No ambient reward, profile, save or quest progress was added. Arthur shows a completed check after his first talk and becomes available again for the board.
- Arthur's post-file send-off playfully hints that friendly conversations might provide something useful. His board introduction explains four categories, flip/drag/CATEGORIES controls, the fourth-card check, and Gold 4 / Silver 2 / Bronze 0-1 in four short screens, without client assignments. Removed the redundant medal tutorial page.
- Right-look tutorial now refers to the whole scenery of Coast Tales.
- CATEGORIES scales/fades in (0.26 s) and out (0.18 s), retains the input blocker through closing, reverses from its current pose and clears safely when disabled.
- Updated the authored contract tests, affected README text and explicit user-requested exceptions in the plaza/guide instructions. Preserved all unrelated changes and ran no Git or Unity commands.

### Final internal verification
- Scratch compile: **0 errors / 29 warnings**, the same warning set as the earlier compile (MSB3245 x6; CS0105 x1, CS0618 x12, CS0184 x1, CS0649 x2, CS0414 x7).
- All eight suites exit 0: **Level1 75/75**, **CampaignFoundation 675/675**, **Dialogue 1098 assertions**, **Guide 907**, **CampaignFoundationRuntime 16/16**, **Level2 1332**, **Controls 5734**, and **Progression PASS**.
- All three validators pass: **GuideTargets 14 paths / 0 missing**, **OfficeNpcMarkers 24**, **Level2Assets 11,881 unique GUIDs**.
- Test outputs are in `Temp/CampaignChecks`; tracked suite `bin` folders were not rebuilt. Source-contract/static review found no material page-turn or modal lifecycle issue. These checks do not prove visual appearance or touch feel.
- New implementation-agent sessions failed to initialize; the root completed the animation implementations directly, with the existing verification agent checking the integration and tests. No required implementation agent remained running.
- **Implemented and internally verified; user Play Mode / visual acceptance pending.**

Play Mode checklist: turn forward/back in folder, reviewed file and book; try rapid taps, page boundaries, closing mid-turn and attached records. Test CATEGORIES opening/closing/reopening without card touch-through. Check triangles/checks for every plaza person, cancelled chats, unguided exploration, and Arthur's returning blue route after four IDs. Listen/read both revised Arthur talks and the Coast Tales look prompt.
## 2026-10-27: Arthur grounding and longer plaza routines

Follow-up requested after Arthur appeared above the plaza:
- Replace animated culling-bound grounding with a baked geometry measurement; retain the sole offset in locomotion and ground restored corkboard placement too.
- Give Milo and Zorbix distinct looping routes, periodic short meet-and-wave greetings, player-conversation interruption and repeatable small talk after their profiles are collected.
- Keep clients stationary throughout a pending profile handover, including held, drop and collection animation states.
- Give Mang Dante an ordered visit list covering the eight other plaza characters, with brief ambient chats, busy-target skipping and a plaza boundary.
- Reuse the shared looping walk/stroll animation. Longer movement comes from routes, without changing cutscene clips or character models.

### Follow-up verification after interruption recovery
- Completed missing cross-file wave, handover/collection state, repeatable small-talk integration and bounded social-approach helper.
- Wired each patrol's home to its existing starting route marker. Kept long-distance pair chat disabled: the greeting owns their conversation only when they meet.
- Fixed meeting admission during a partner's post-dialogue suspension; a recently interrupted walker finishes its farewell delay before another meeting starts.
- Scratch runtime compile: **0 errors / 29 warnings**. These comprise six MSB3245 missing-reference warnings and 23 C# warnings (CS0105 x1, CS0618 x12, CS0184 x1, CS0649 x2, CS0414 x7).
- **Level1LivingPlazaRegression: 75/75 PASS**, including authored routes, meeting/visit wiring, grounding and pending-profile contracts, reservation ownership, approach deadlines, cancellation, facing and post-dialogue admission guards. Social behavior coverage is source-contract/static coverage, not a Unity simulation.
- Other required suites: CampaignFoundation **671/671**, Dialogue **1099 assertions**, Guide **907**, CampaignFoundationRuntime **16/16**, Level2 **1332**; all PASS.
- Required validators: VerifyGuideTargets **17/17**, VerifyOfficeNpcMarkers **24/24**, VerifyLevel2Assets **PASS / 11,880 unique GUIDs**.
- Suites built into `Temp/CampaignChecks`; tracked `Tests/*/bin` outputs were not rebuilt. Build intermediates under `Tests/*/obj` may refresh.
- **Play Mode and visual validation NOT RUN.** The checklist below remains for the user; compilation does not establish route clearance, wave appearance or animated sole contact.

### Follow-up Play Mode checks
1. Reload `Office_Level1` from disk before saving the scene. On a fresh run, Arthur's soles should touch the plaza floor at `Arthur Start Placement`; repeat after continuing a save with his reviewed file already collected.
2. Watch Milo and Zorbix start at different spots, walk their loops, meet, wave briefly and separate. Interrupt each during a walk, an approach to the meeting and the wave; the other must resume and neither may remain frozen.
3. Finish each client's quest talk. Their profile must remain beside them until collected (including interrupted pickup animations). After collection, they should resume wandering. Later taps should start small talk with no additional reward.
4. Follow Dante through multiple visits. He should approach one person, pause nearby, then move on. Tap him or his target during an approach/chat. He must skip a busy target and never follow Marie into the office.
5. Confirm the wave pose and shared walking clip look natural on both models. Compile/source checks cannot validate foot sliding, hand pose, obstacles or visual spacing.

## 2026-10-27: implemented, internally verified (Play Mode NOT RUN)

Everything in `README.md` is implemented and wired into `Office_Level1.unity`. The user has not checked it in Play Mode yet.

### Files
**New scripts**
- `Assets/Scripts/FSM/NPC/NpcLife.cs`: priority model and activities
- `Assets/Scripts/FSM/NPC/NpcLocomotion.cs`: route walking, spacing, staged fallback
- `Assets/Scripts/FSM/NPC/NpcBodyPose.cs`: procedural sit, reach and phone
- `Assets/Scripts/FSM/NPC/StaticArmRig.cs`: T-pose arm rig
- `Assets/Scripts/SideQuest1/AmbientNpcInteractable.cs`
- `Assets/Scripts/SideQuest1/PlazaLifeContent.cs`
- `Assets/Scripts/SideQuest1/MarieOfficeRoute.cs`
- `Assets/Scripts/SideQuest1/MarieTripRules.cs` (pure)
- `Assets/Scripts/UI/Books/PageTurnStage.cs`
- `Assets/Scripts/UI/Books/PageTurnMath.cs` (pure)

All new scripts have `.meta` files with fresh GUIDs.

**New assets**
- `Assets/AssetIntegration/NpcHumanoid.controller` (+ meta)
- `Assets/Resources/Levels/Level_01/plaza_life.json` (+ meta)

**Changed scripts**
- `NpcAnimationDriver.cs`:
  - stroll pace;
  - per-state clip speed measured without per-frame allocation;
  - `IsSpeaking`, `SetAmbientConversation`, `ResetMotion`;
  - bones and idle state applied once a model revealed later starts playing.
- `NpcIdlePacing.cs`: steps aside where an `NpcLife` exists.
- `TaxpayerNpcInteractable.cs`: read-only `IsWaitingForRound`.
- `ClientConsultation.cs`: Marie walks instead of the hidden teleport; deferred resume; travel lines.
- `TutorialNpcController.cs`: `WalkToCorkboard`, `PlaceAtCorkboard`; `ReturnToCorkboard` keeps `NpcLife` in step.
- `ArthurOpening.cs`: walk after the file lands; resume places him at the board.
- `CaseFolderUI.cs`, `TaxCodeBookUI.cs`: staged page turns; `SurfaceFor`.
- `AttachedRecordsPage.cs`: rides the surface printing its page.
- `DeskMotion.cs`: the unused single-surface `TurnPage` was removed (`PageFlipSeconds` kept).

**Import settings**
- `npc_receptionist_accessories.fbx.meta`, `npc_auditor_static.fbx.meta`: `isReadable: 1`.

**Scene (`Office_Level1.unity`, LF, backup kept outside the project)**
- New root `Level 1 NPC Life` with its markers and `Marie Route`.
- `Environment/Outside/Plaza/AmbientNPCs` with four ambient NPCs (Synty prefab instances; the source prefabs are unchanged).
- `NpcLocomotion` + `NpcLife` on Marie, Jhun, Milo, Zorbix and Arthur; `NpcBodyPose` on Marie and Jhun.
- `StaticArmRig` on the receptionist and the Auditor.
- Arthur's rigged twin added and `visualRoot` re-pointed to it; the static copy is set inactive.
- Shared controller on Marie, Jhun, Milo, Zorbix and Arthur. Driver idle names: Marie `Idle Relaxed`, Arthur `Briefcase Idle` (+ Walking).

**Tests**
- New `Tests/Level1LivingPlazaRegression` (csproj + Program.cs).
- Updated checks that encoded the replaced designs:
  - `Tests/CampaignFoundationRegression/Program.cs`: Marie's resume and move, records on their page.
  - `Tests/Level2Regression/Program.cs`: leafing uses the staged turn.

### Checks run (agent, 2026-10-27)
- **Scratch compile:** runtime 0 errors / 23 warnings (the baseline; none from new files). Editor scripts against the new runtime: 0 errors.
- **Level1LivingPlazaRegression: 64/64 PASS.** It covers:
  - Marie's trip rules for every event order (including a hand-over finished before she arrives) and every resume combination;
  - page-turn timing at 12-144 fps, the slow-frame cap, leaf front/back and fades;
  - page-turn source contracts: commit before motion, completion on interrupt, graphics-only copies, rapid input, page boundaries;
  - ambient content: exactly four characters, brief, varied personalities, no classification or non-touch words, valid emotes;
  - scene wiring: exactly four ambient NPCs matching the JSON, no protected model reused, no marker/profile/talk on ambient NPCs, the original four npcIds and pickups intact, Arthur's twin, shared controller on nine models, unique ids and no dangling refs;
  - the controller: Humanoid clips only, looping walk, slower stroll;
  - "talk-only / no progress / no per-frame planning / priority order" source contracts.
- **Mutation check:** breaking the "hand-over already done" rule in a copy of `MarieTripRules` is caught (1 of 64 FAIL). The copy was deleted.
- **Other suites, all PASS:**

  | Suite | Result |
  |---|---|
  | CampaignFoundation | 671/671 |
  | Dialogue | 1099 assertions |
  | Controls | 5,734 |
  | Progression | PASS |
  | Guide | 907 |
  | CampaignFoundationRuntime | 16/16 |
  | Level2 | 1332 |
  | Training | 506 |
  | Settings | 138 |
  | CareerBoard | 72 |
  | Audio | 259 |
  | IntroCutscene | 44 |

- **Python scene checks:**
  - `VerifyGuideTargets.py` 17/17 paths.
  - `VerifyOfficeNpcMarkers.py`, `VerifySupplyCounterInteraction.py`, `VerifyShopAndGuideView.py`, `VerifyHudIcons.py`, `VerifyEditableMenuButtons.py`: PASS.
  - `VerifyLevel2Scene.py`: PASS (2 pre-existing warnings).
  - `VerifyLevel2Assets.py`: PASS, 11,880 unique `.meta` GUIDs.
  - `VerifyInteractionOutlines.py`: **fails on `PC_Renderer.asset` "missing outline feature subasset"**. That renderer asset was not touched here; the check was already known to fail (KNOWN_ISSUES).
- No tracked `Tests/*/bin` file was rebuilt (suites were built to `Temp/CampaignChecks`). Building with `-o` still refreshes `Tests/*/obj` intermediates.
- **Play Mode, device, rendering, animation look and feel: NOT RUN.**

### What to check in Play Mode
Reload `Office_Level1` in the editor first (the scene changed on disk). Then:
1. **No T-poses.** Arthur shows the briefcase idle. The receptionist and Auditor have lowered arms with no visible tearing at the shoulders. Their forearms move a little while they talk.
2. **Ambient NPCs.**
   - Lorna stands at the Supply Counter's customer side (not in front of the centre), browses and reaches now and then; she's not in the way of the shop tap.
   - Dante strolls the forecourt and pauses.
   - Bea and Carlo face each other and take turns; Carlo sits on the bench without clipping through the seat or backrest.
   - Tap each one: a short talk, the next one on the next tap, no marker, nothing awarded.
3. **Jhun** sits on the bench facing the plaza. Once the rounds are open, walking close makes him stand and turn to you. His talk and profile drop work as before. He sits back down after you leave.
4. **Marie.**
   - Before the rounds she strolls with her hand at her ear; her waiting line still plays.
   - After the rounds open she stands at her spot; her talk and profile work.
   - When Side Quest 1 completes she walks through the front doors (after they open), waits by the front desk facing the receptionist, and walks through the inner doors and the conference-room door to her spot once you finish the receptionist talk.
   - Also try talking to the receptionist before she arrives: she should continue to the room after a short check-in.
   - Tap her on the way: one line, a pause, then she walks on.
   - Consultations in the room work as before.
5. **Arthur.** After you collect his reviewed file he walks from his start spot to the corkboard spot and idles there; the board still opens only from his talk after the four profiles.
6. **Resume.** Save and continue at each stage (plaza / after the side quest / after the receptionist): Marie appears plaza / front desk / conference room; Arthur appears at the corkboard once his file was collected.
7. **Page turns** (desk book and folder, and the Level 2 folder).
   - Forward: the old page lifts off and the next page is already underneath.
   - Backward: the previous page comes back over and lands.
   - Rapid taps, the first and last page, and closing mid-turn leave a clean page with the right page number and arrows.
   - The attached records on Marie's records page ride the turning page.
8. If anything walks through a wall or blocks a door, move the matching marker under `Level 1 NPC Life` and report it.
