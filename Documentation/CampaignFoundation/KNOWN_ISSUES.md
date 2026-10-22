# Known issues, decisions and risk register

Status: planning snapshot, 2026-10-21. A risk is not a request for the user to implement scene wiring. Complete unaffected implementation and record exact internal versus manual evidence.

| ID | Fact/risk | Resolution ticket / acceptance boundary |
|---|---|---|
| CF-01 | Existing hearts/totalReputation are accuracy scores and gate old 80% passing; they cannot be relabeled Public Trust | T01/T07/T08; separate data and replace active story gating throughout menu/results/HUD |
| CF-02 | L1 has no full resume snapshot; a Continue label alone would promise unavailable behavior | Field part source-implemented in T04 (2026-10-22); case/clock part T05; user manual pending |
| CF-03 | L1 currently reveals correctness per drop; first complete independent check requires an actual draft/check mechanic | T06; no hidden correct placement hints during drafting |
| CF-04 | Quax final model/portrait absent under that name in inspected source; current guide uses Arthur and overlay tools | T15 configurable actor and physical sample; provisional art explicitly recorded, final visual approval pending |
| CF-05 | Existing writes can mutate memory before Save fails; new economy/consumption cannot do that | T02 command boundary; candidate/commit/publish and failure injection for later commands |
| CF-06 | Pending result is not a persisted currency-claim history; callback-only RunId protection is insufficient | T09 persisted scoped reward/action claims and checkpoint consistency |
| CF-07 | L1 and L2 review clocks differ; a voucher must update real time and persisted clock consistently | T14 small clock adapter; T18 extends L2; never a second unrelated timer |
| CF-08 | Bag/viewers currently use runtime models and L1 can narrow visible profiles after quest; bag must preserve acquired profiles | T10 authoritative collected IDs and read-only historical views without rebinding live CaseManager |
| CF-09 | Level2 remains a legacy heart-score flow until integrated; normal launch would misrepresent campaign readiness | A keeps new-campaign L2 pending; T18-T21 integrate then enable it; preserve isolated development scene |
| CF-10 | Existing Rosa has stamp outcomes but no authored knowing-choice context; an honest wrong stamp is not misconduct | T19 explicit evidence-aware choice and one pending consequence resolution |
| CF-11 | Economy, trust numbers and motion timings are starting fixtures rather than playtested balance | Configurable, internally boundary-tested; user manual session determines tuning, no silent rule changes |
| CF-12 | Historical docs mention obsolete model choices or deleted Temp build helpers | Inspect live source and latest follow-up notes; use current scratch generator and actual csproj; preserve Rosa's current appearance |
| CF-13 | Layer29 tutorial binder and layer30 settings overlay already exist; new bag/shop can conflict with input/cameras | T03/T10/T12/T13 ownership integration, no arbitrary reuse of those layers |
| CF-14 | Existing loan-treatment educational authority remains draft in L2 docs | This pack changes no tax rule; preserve content authority limitation in B handoff |
| CF-15 | Current first intro has a first-view policy and authored text/audio, not necessarily legacy JSON subtitles | Preserve current authored route/policy; no unrequested full cutscene rewrite in T16 |
| CF-16 | Scene/import/Play Mode, target reachability, appearance, phone feel and listening cannot be proven by the AI's allowed checks | USER checklist in VERIFICATION and every ticket; no fabricated acceptance |
| CF-17 | The current loader can normalize old scores/chapters as active data, but the user now wants a fresh campaign | T02/T03 prevent old data from supplying Continue, trust, coins or unlocks; confirmed New Game starts clean with no archive or conversion. Normal updated-campaign save/backup recovery remains required |
| CF-18 | A carry cap without drawer withdrawal could strand paid overflow supplies | T14 explicitly supports confirmed purchase-to-drawer and atomic physical withdrawal up to free carry capacity, including failure/resume/sandbox checks |
| CF-19 | T02 supplied the campaign save state but the menu still used legacy routing | Source-implemented in T03 (2026-10-22): menu routes, Career and Practice gating use the campaign; user manual menu checks pending |
| CF-20 | Settings 'Reset Progress' wrote a record without a campaign | Source-implemented in T03: the settings reset calls StartNewCampaign ('START A NEW STORY?'). PlayerProgressManager.ResetProgress remains but has no caller; user manual check pending |
| CF-23 | Main-menu NEW GAME / CONTINUE are runtime copies of the modelled settings_button with lettered (not modelled) words; size/position come from mesh bounds (Settings paper, corkboard width, PLAY-to-Tutorials pitch). The first T03 panel was rejected from the user's screenshot (2026-10-22) and replaced | USER checks framing/readability on phone, tall and wide layouts; tune the share constants in MainMenuCampaignPanel if needed |
| CF-24 | Continue now opens the progress module; starting Chapter 1 there restarts its opening/office from the start because no Level 1 field checkpoint exists yet | T04/T05 add the L1 checkpoint and resume |
| CF-25 | Direct-Editor and mastery-replay runs still apply MASTERY results (best XP/badges/score hearts) to the save as before; they cannot write story completion or the story checkpoint | Intended for T03 (mastery is replay-safe). Revisit if Editor test runs must be fully isolated from a real campaign's mastery |
| CF-26 | Main menu input remains touch-only (existing MainMenu Input.touch path); the new rows follow it | Same as before T03; editor testing needs touch simulation or Device Simulator |
| CF-27 | PLAY (no started story) replaces an older development, newer-version or unreadable save without a confirmation, as the user specified; only NEW GAME over a started story confirms | Intended (user decision 2026-10-22); T02 still blocks ordinary saving over a newer save until PLAY/New Game |
| CF-28 | Shop entry exists in the progress module but no shop catalogue, prices, purchase or ownership data exist yet; CampaignMenu.ShopUnlocked is always false | T12/T13 add the introduction claim and the shop, then make ShopUnlocked read it |
| CF-29 | Level 1 resume always starts the player at the scene's normal spawn; an open board reopens at its viewpoint, so after finishing it the player is back at that spawn rather than beside the board | T04 design (no saved transforms). USER judges whether a nearer marker is needed |
| CF-30 | Side-quest timing is not resumed (LevelScoreLedger side-quest clock restarts, OfficeHours side-quest seconds not restored) and the optional TownToOfficeTransition indicator is not re-shown on resume | Times are presentation/mastery only; T05 owns clocks |
| CF-31 | Leaving after the side quest resumes the field state but the office case starts over until T05 adds case/form/audit/clock state | T05 |
| CF-32 | A conversation or profile pickup still in progress when the app closes is not saved (only completed milestones) | Intended (stable milestones); the NPC/pickup stays available |
| CF-21 | A save newer than version 5 is now left untouched and ordinary Save() refuses to overwrite it (before T02 it was loaded as-is). In that state normal play cannot save until a confirmed New Game/reset | Intended protection (T02). T03 confirmation text should say the newer save will be replaced; only reachable after an app downgrade |
| CF-22 | CF-05 is fixed for New Game only: StartNewCampaign commits before publishing and restores both files on failure. Other existing mutations (results, checkpoints, tutorials) still change memory before Save() can fail | Later economy/claim commands (T08/T09/T11-T14) must use candidate/commit/publish like StartNewCampaign |

Record new discovered issues with ID, exact source/evidence, owner ticket, impact, workaround and remaining user acceptance. Do not mark a generic risk 'resolved' merely because its ticket document exists.

