# Training Hub: plan (approved 2026-10-16, built)

Status: **APPROVED AND BUILT.** This is the original plan, kept for history. Where the user's answers changed it, the **Decisions** below win; the current behaviour is described in `README.md`.

## Decisions (the user's answers to section 10)
1. Scene name: **TutorialHub** (the dead `"TutorialPage"` load is replaced). **Changed 2026-10-17:** the user moved the hub into the 3D Menu (the camera leans to the computer desk, the binder is held); there is no separate scene.
2. Practice runs in the **real Office_Level1 in Practice Mode**, switching off the parts of the level a drill never uses, so it is lighter.
3. **Basics is always open**, even before playing Chapter 1. **Side quest and case cards stay locked until seen in the level**; SKIP TUTORIAL does not open them.
4. Old saves that finished Chapter 1 open every Chapter 1 card (recommended default).
5. and 6. **No rewards, no stars, no Training Rank.** Only the **MASTERED stamp**, earned by finishing a mastery run.
7. Case drills use the **unused `case_002`** (Marco Santos).
8. The guided refresher is **skippable**: after the first practice, a card offers both GUIDED and MASTERY.
9. Concept: **Arthur's training binder**.
10. Card groupings: as proposed.
11. The tutorial button works from the first launch. **Only the Basics tab is shown** at first; Side Quest and Case tabs appear once one of their cards opens.

Changes made while building: the save records only *seen* lessons (no "skipped" list, since skipping does not open cards). `level-complete` is not part of "Your Screen" (a practice run never reaches Level Complete). A small new guide condition `case:correct` makes the Moving Values mastery check real answers.

---

## 0. What I found (facts the plan builds on)

| Area | Finding |
|---|---|
| Tutorial button | `MainMenu.cs` loads the scene `"TutorialPage"` on `tutorial_button`. **That scene does not exist** and is not in Build Settings (which list only 3D Menu, Office_Level1, Office_Level2, Level1_Character_Intro_Cutscene). The button currently does nothing but log an error. Nothing else references it. |
| In-game tutorial | The real tutorial is the **Level Guide** (`Assets/Scripts/UI/Guide/`). It is data-driven from `Resources/Tutorials/Level_01/guide.json`: **27 lessons**, each with pages (text, spotlight `point`, hand `gesture`, `waitFor`). `GuideRuntime` self-bootstraps only in `Office_Level1`. `GuideDirector.EndLesson()` is the single place a lesson finishes; `SkipAll()` marks all lessons finished without calling it. Lessons whose `after` names a missing lesson are kept and the `after` is ignored, so a **subset of lessons can be replayed** by the same director. |
| Legacy tutorial | `UI/Tutorial/TutorialController` + `level1_tutorial.json` (empty) are dead code. Not used by this plan. |
| Side quest | Know Your Taxpayer: talk to 4 plaza clients → collect `TaxpayerProfileWorldPickup` papers → Arthur → corkboard drag-sort (`CorkboardSideQuestManager`, `WrongPlacements`, 2 misses lock for 3 s) → Side Quest Complete card. `OfficeDoorGate` keeps the office shut until it is done. Career lists side quests in `Resources/Career/chapters.json` (`sideQuests`). |
| Case flow (L1) | Receptionist → desk (`Workstation` mode) → drag Case Folder → drag Tax Code Book → turn pages → value transfer (tap or drag, green/red fit) → profiles next to the book → store folder on HUD icon → Exit Desk → drag folder icon onto the Auditor → Audit Summary → Level Complete. There is an **unused `Levels/Level_01/cases/case_002.json`** (Marco Santos, freelance designer) that is not in `level.json`. |
| Save | `PlayerProgressData` v3, signed envelope (`SaveFileStore` HMAC), crash-safe, `Normalize()` repairs everything on load. All fields are additive `JsonUtility` lists. **Gotcha:** `TryDecode` accepts a *plain* (unsigned) file only when `saveVersion < CurrentSaveVersion`; bumping to v4 would let a hand-made plain "v3" file in. The guard must become a fixed "signed since v3" constant. |
| Visual language | Career: cork boards, pinned papers with sway, red strings, rubber stamps, Level 1 medals. Settings: navy/brass hanging control board with pill switches. Shared kit: `HudTheme` palette (navy `PanelFill`, `Brass`, cream `TextPrimary`, gold `Exp`, red `Reputation`, mint `Complete`, cyan `Active`), `HudFontSet` (UI + handwriting fonts), `CareerBoardKit` (geometry, world text, pins, padlock, bar canvas), `CareerBoardButton` (3D brass push button), `CareerBoardTarget` (tappable base), `CareerPinnedPaper` stamp slam, `HudBadgeArt` medals, Arthur portraits. |

---

## 1. Player experience and identity

### The concept: "Arthur's Training Binder"
The tutorial button takes the player to a new, small, warm scene: **Arthur's corner office after hours**. One desk lamp, the office window behind, and on the desk a big **navy cloth ring binder with brass corners**, open toward the camera. This is the hub.

Why a binder:
- It is **original** next to the other two modules: the level select is a *wall of cork and pins*; settings is a *hanging control board*; training is a *book on a desk you flip through*. Different object, different motion (page flips, sleeves, stamps, not swaying pins or cables).
- **Tabs come for free**: the three categories are real **index divider tabs** sticking out of the right edge of the binder (Basics / Side Quest / Case), each a different colour. Tapping one flips the binder to that section with a quick page-turn. This maps the reference screenshot's tab row onto a physical, in-world object.
- It fits the fiction: Arthur trained you on day one; this is **his binder of drills** for you. It ties to the Tax Code Book page-turning the player already knows.
- It is **consistent**: same `HudTheme` navy/brass/cream, same fonts, same brass 3D buttons, same rubber-stamp slam as the career papers, same Arthur portrait and speech style.

### What the player sees
- **Cover row (top of the open binder)**: "TRAINING BINDER", the player's **Training Rank** (see Rewards), and "Mastered 2 / 7".
- **Section page**: up to 3 **drill cards** side by side, each in a clear plastic sleeve (a subtle gloss sheet over a cream card). Mobile-first: cards are big (whole card is the touch target), one row, readable at phone width.
- **Each drill card** (structure inspired by the reference, style our own):
  - Big handwritten number **01 / 02 / 03** and the title (e.g. "Getting Around").
  - A **skill checklist** of what it covers ("Walk · Look around · Follow the arrows"), each line ticked when that in-game lesson has been encountered.
  - **Progress**: "Lessons seen 3 / 3" with a thin HUD-style bar, and a **mastery row of 3 brass stars** (see section 4).
  - **Status corner**: a rubber stamp when mastered (**MASTERED**, green ink, slammed with the career stamp motion), an orange **NEW** sticky tab when freshly unlocked, nothing otherwise.
  - Bottom: brass **PRACTICE** button (or **LOCKED**, disabled).
  - **Locked card**: the sleeve holds a blank grey sheet with a paper-clipped note: "You haven't met this one yet. You'll learn it in Chapter 1." No spoilers beyond the title.
- **Arthur** appears as a small portrait medallion with one speech line at the bottom when the hub opens (e.g. "Back for a refresher, rookie? Pick a drill."), reusing the Guide speech box look. One line, tap to dismiss, never a wall of text.
- **BACK** (brass button, plus Android back) returns to the 3D Menu.

### Motion (only what helps)
- Arrive: camera eases onto the desk (0.5 s), binder opens once.
- Tab tap: one page flip (~0.35 s) and the cards slide into their sleeves. The selected tab sits a little further out and brighter.
- Card press: the card lifts 2-3 mm with a soft shadow (like the career peel), then the PRACTICE button press sinks.
- Coming back from a drill: that card is focused, and its change is celebrated **once** (a new star pops in, the MASTERED stamp slams, the NEW tab peels off). Same "since last visit" idea as the career board, stored in PlayerPrefs as a viewer convenience.
- No idle animations beyond a faint lamp flicker. Reduced motion is respected by keeping every transition short and skippable with a tap.

---

## 2. Information architecture: tabs and cards

The cards group the **real lessons of `Level_01/guide.json`**. A card is unlocked when **any** of its lessons has been encountered (section 3); its checklist shows each lesson.

### Tab 1: BASICS (blue tab)
| # | Card | Lessons reused (guide.json ids) | What it teaches |
|---|---|---|---|
| 01 | **Your Screen** | `hud-tour`, `notes`, `level-complete` | Objective tracker and clock, progress card (EXP, hearts), minimap, Profiles/Case Folder/Settings icons, Notes open/close, reading the Level Complete card. |
| 02 | **Getting Around** | `move`, `look`, `follow` | Joystick walking, swipe to look, ground arrows, goal pin, minimap route. |
| 03 | **People & Papers** | `talk`, `dialogue-tip`, `pickup`, `profiles-icon` | Tap a person to talk, tap the box to read on, pick up a profile paper, open and close Profiles. |

### Tab 2: SIDE QUEST (orange tab), one card per side quest
| # | Card | Lessons reused | Practice |
|---|---|---|---|
| SQ 1 | **Know Your Taxpayer** | `guide-appears`, `corkboard`, `sidequest-card` | Collect the four profiles and sort them on the corkboard. The card shows the side-quest medal (`HudBadgeArt`, same art as career) in a muted "training" tint. |
| SQ 2+ | Future side quests | From their own chapter's guide.json | Driven by `chapters.json` `sideQuests` plus the training catalog; unused spaces show "Not yet found", matching the career board. |

### Tab 3: CASE (green tab)
| # | Card | Lessons reused | What it teaches |
|---|---|---|---|
| 01 | **Your Desk** | `reception`, `to-desk`, `desk`, `case-folder`, `book-pages` | Sit at the desk, drag out the Case Folder and the Tax Code Book, the three fields, turning pages and which section holds which field. |
| 02 | **Moving Values** | `value-transfer`, `value-rejected`, `investigate` | Tap-select and tap-place, drag-place, green/red fit, "fits is not correct", profile → book → folder. |
| 03 | **Hand It In** | `office-hours`, `store-folder`, `exit-desk`, `auditor`, `submit`, `audit-card`, `audit-retry` | Office hours deadline, store the folder on the icon, Exit Desk, drag the folder onto the Auditor, read the Audit Summary, retry after a failure. |

Not mapped on purpose: `welcome`, `to-office` (pure story glue). Future Level 2 mechanics (computer, stamp) get new cards when Level 2 gets guide lessons; until then they are simply not listed (no "coming soon" clutter inside a tab).

### Card states
| State | Rule | Looks like |
|---|---|---|
| **Locked** | No lesson of the card encountered. | Grey blank sheet, clip note, LOCKED button. |
| **New** | Encountered, never practised. | Full card, orange NEW sticky tab, gold pulse on PRACTICE. |
| **Practised** | Finished the refresher at least once. | No tab, star row shows earned stars (0-2). |
| **Mastered** | Earned all 3 stars (in one or across runs, best kept). | Green MASTERED stamp, gold stars. |

Progress per card: **Lessons seen n / m** (from the save) and **★ 0-3** (best mastery).

---

## 3. Unlock rule and save

### Trigger points
1. **Lesson finished in-game**: `GuideDirector.EndLesson()` gets one new line that raises a static notification `GuideDirector.LessonFinished(levelId, lessonId)`. It is observation only; the director's behaviour is unchanged.
2. A new self-bootstrapping `TrainingRecorder` (same pattern as `GuideRuntime`) listens and records `"Level_01/move"` etc. into the save, then saves (one write per lesson; lessons are minutes apart, so this is cheap). It **ignores events during a practice run**.
3. **SKIP TUTORIAL** (`SkipAll`): records every lesson of that level as **skipped**, so cards unlock with a "Skipped: try it here" note instead of staying locked (recommended; see Q3). It needs one more notification line in `SkipAll()`.
4. **Old saves**: a chapter already **completed** in the save counts as having seen all of its lessons. This is *derived* when the hub builds its view, not written into the save, so nothing is rewritten behind the player's back.

### New save fields (additive, version 4)
```csharp
// PlayerProgressData (v4)
public List<string> tutorialsSeen = new List<string>();      // "Level_01/move"
public List<string> tutorialsSkipped = new List<string>();   // "Level_01/corkboard"
public List<TrainingRecord> trainingRecords = new List<TrainingRecord>();

[Serializable] public class TrainingRecord
{
    public string cardId;        // "basics.getting-around"
    public int bestStars;        // 0..3
    public int practiceRuns;     // finished runs
    public int bestSeconds;      // fastest challenge time (0 = none)
}
```
- `Normalize()`: null lists → empty; `CleanIds` on both id lists; a seen id is removed from skipped; records: drop empty ids, clamp stars 0..3, negatives → 0, merge duplicates keeping the best. v3 → v4 upgrade is just "missing lists become empty".
- `CurrentSaveVersion = 4`, and `PlayerProgressManager.TryDecode` changes from `>= CurrentSaveVersion` to a fixed `>= SignedSinceVersion (3)` so plain unsigned files can still only be genuine v1/v2 saves.
- New methods: `RecordTutorialSeen(id)`, `RecordTutorialSkipped(id)`, `RecordTrainingRun(cardId, stars, seconds)`; each returns whether anything changed. **None of them touches EXP, hearts, badges, passed or completed levels.**
- Career totals stay untouched, so `CareerProgress` and the career board are unaffected.

### Regression plan
- New `Tests/TrainingRegression` (same style as `CareerBoardRegression` / `SettingsRegression`, compiled against the scratch Assembly-CSharp):
  - v1, v2, v3 sample saves load, normalize to v4 with empty training lists and **identical** EXP, hearts, levels, records and badges.
  - Normalize repairs: duplicates, blanks, stars out of range, negative values, seen-and-skipped.
  - Hand-made plain file claiming v3 or v4 is still rejected.
  - `RecordTrainingRun` keeps the best and never changes career totals.
  - Catalog: every lesson id in `training.json` exists in its guide.json; every card has a tab; no lesson in two cards; ids unique; card states from sample saves (locked/new/practised/mastered, skipped, completed-chapter fallback).
  - Drill files: every target / trigger parses with `GuideTriggers`; scene paths checked like `VerifyGuideTargets.py`.
- Existing suites must stay green: `ProgressionRegression` (save), `GuideRegression` (guide.json is not edited), `CareerBoardRegression`, `SettingsRegression`, `ControlsRegression`, `DialogueRegression`.

---

## 4. The practice flow

### Where practice happens
**Recommended: the real Office_Level1, opened in Practice Mode** (see Q2 for the alternative). Every mechanic (joystick, HUD, corkboard, desk, value transfer, Auditor) is wired inside that 4.6 MB scene; rebuilding them in a separate room would duplicate fragile scene wiring and drift from the real game. Practice Mode makes it feel like a separate, safe place:
- A thin **"PRACTICE" ribbon** at the top in training-tab colour, and a soft blue-grey vignette, so the player always knows this is not their career run.
- Everything not part of the drill is switched off: other lessons, unrelated objectives, office-hours pressure (unless the drill is about it), unrelated NPC markers. The objective tracker shows the **drill goals** instead.
- The player **starts where the drill starts**: plaza for Basics 02/03 and the side quest, already seated at the desk for Case 01/02, at the desk with a filled folder for Case 03. Setup uses existing public calls only (spawn point, `CameraController` mode, `CaseProgressionManager.StartLevel`, door gate open), listed per drill in its JSON `setup` block.
- **Nothing is saved to the career**: one guard at the save choke point (`PlayerProgressManager.SetPendingResult` / `ApplyLevelResult` return early while a practice session is active), so a practice audit can never touch EXP, hearts, badges or pass lines.

### Two stages per drill (the "mastery" framing)
1. **Refresher**: Arthur's **original lessons, played exactly as in-game** (same pages, same spotlight, hand, your-turn steps), loaded *by reference* from `Level_01/guide.json`. After the first run the player can tap **Skip refresher** to go straight to the challenge.
2. **Mastery challenge**: Arthur says one line ("Now without my hand. Show me."), then the hand and spotlight are off and the player does the task for real with short, clear goals shown as a checklist. Arthur's lines here are *coaching, not instructions*: short "why" tips and one specific hint after a mistake (e.g. "That value fits, but is it right for Marco? Check his profile."). 

**Stars (3 per drill, kept as best):**
- ★ **Done**: finish the challenge.
- ★ **Clean**: no mistakes (no wrong corner, no rejected value, approved first try...).
- ★ **Sharp**: under a par time shown before starting ("Par 0:45"). Optional: it never fails the run.

| Card | Challenge (no hand, no spotlight) |
|---|---|
| Your Screen | "Arthur asks, you tap": 4 quick prompts ("Where do you see ALL of today's steps?" → tap Notes). Clean = no wrong taps. |
| Getting Around | Follow the arrows through 3 practice checkpoints (beacon pins) to the goal. Clean = never strayed off the route for more than a few seconds. |
| People & Papers | Talk to two clients, collect their papers, open Profiles and close it. Clean = no missed taps on the dialogue. |
| Know Your Taxpayer | The full side quest loop: collect four, sort four. Clean = 0 wrong corners (the same rule as the gold badge). |
| Your Desk | Open the folder and the book, then turn to the right section for each field when Arthur names it. |
| Moving Values | Fill all three fields of a **practice case** (recommended: the unused `case_002`, Marco Santos, so the answers are new). Clean = no rejected values and all three correct. |
| Hand It In | Store, exit, hand in to the Auditor before the practice clock. Clean = approved first try. |

### Ending and exit
- When the challenge ends, a **Drill Result card** (HUD reward-card style, but in the binder's navy and training colour) shows the 3 stars filling one by one, the time, the best so far, and two big buttons: **TRY AGAIN** and **BACK TO BINDER**.
- At any time: a **Leave practice** button (top corner, two taps like SKIP TUTORIAL) returns to the hub without a result.
- Returning reopens the hub **on the same tab with that card focused**, then plays its celebration once.

### Reused vs new (no duplicated tutorial content)
| Reused as is | Small, additive changes | New |
|---|---|---|
| Lesson text, pages, targets, gestures (guide.json, read only) | `GuideDirector`: `LessonFinished` / `TutorialSkipped` notifications, and a way to start from a given `GuideScript` | `TrainingSession` (static: active drill, return tab) |
| `GuideDirector`, `GuideCoachView`, `GuideHand`, `ObjectiveGuide` / trail / beacon | `GuideRuntime`: in Practice Mode load the **drill script** instead of guide.json | `TrainingRecorder` (writes seen/skipped) |
| Corkboard, desk, value transfer, Auditor, all gameplay | `PlayerProgressData` v4 fields + Normalize; `PlayerProgressManager` guard + legacy constant | `TrainingCatalog` (pure C#: cards, states) |
| `HudTheme`, fonts, `CareerBoardKit`, `CareerBoardButton`, `CareerBoardTarget`, stamp slam, `HudBadgeArt`, Arthur portraits | `MainMenu.cs`: `"TutorialPage"` → `"TrainingHub"` (one line) | `TrainingDrillScript` (composes reused lessons + challenge pages) |
| | `EditorBuildSettings`: add the hub scene | `PracticeModeRuntime` (setup, isolation, goals, stars, exit) |
| | | `TrainingBinderPresenter` + binder parts (tabs, sleeves, cards) |
| | | Drill Result card |

**Content files (user-editable, with `_readme` notes like the other JSON):**
- `Resources/Tutorials/Training/training.json`: tabs, cards (id, tab, number, title, skill lines, `lessons: ["Level_01/move", ...]`, `drill`), Arthur's hub lines, rank names.
- `Resources/Tutorials/Training/Drills/<drill>.json`: `scene`, `setup`, `refresher` (lesson ids to reuse, with optional `when` overrides so they chain in order), `challenge` (pages in the **same page format as guide.json**, plus `goals` and `parSeconds`). Editing a lesson in guide.json changes both the in-game tutorial and the refresher, because the refresher only *points* at it.

---

## 5. Rewards and recognition

**Recommendation: no EXP, no hearts, no career badges from practice.** Career totals drive unlocks and pass lines (`CareerProgress`); farming them in a sandbox would break that, and "replays never farm EXP" is already a rule of the save.

Recognition instead (cosmetic, motivating, never grindy):
- **Stars and the MASTERED stamp** on each card.
- A **Training Rank** on the binder cover from mastered cards: *Trainee* (0) → *Junior Associate* (2) → *Associate* (4) → *Senior Associate* (all). Names editable in training.json.
- Mastering a card plays the career stamp slam and the achievement sound once, plus one proud line from Arthur.
- No timers that punish, no energy, no daily limits: practice is always free and unlimited.

---

## 6. Technical architecture

### Scenes
- **New `Assets/Scenes/TrainingHub.unity`** (recommended name, see Q1), added to Build Settings. Kept small: camera, light, desk prop and lamp from the office asset packs, a root object with `TrainingBinderPresenter`. The binder is built at runtime from code (like the career boards and settings board) and set up by an editor tool, so the scene YAML stays tiny and is never hand-edited.
- Practice runs load **Office_Level1** with `TrainingSession.Active` set. Office_Level1's YAML is **not edited**.

### Scripts (new, under `Assets/Scripts/Training/` unless noted)
| File | Role |
|---|---|
| `TrainingSession.cs` | Static hand-off between hub and practice (drill id, return tab, last result). Same idea as `MenuEntry.OpenSettings`. |
| `TrainingCatalog.cs` | Pure C#: loads training.json, builds card views (state, seen n/m, stars) from a save. Fully unit-testable. |
| `TrainingRecorder.cs` | Self-bootstrap; listens to the guide notifications; writes seen/skipped. |
| `TrainingDrillScript.cs` | Loads a drill JSON and builds a `GuideScript` (reused lessons + challenge pages). |
| `PracticeModeRuntime.cs` | Self-bootstrap in Office_Level1 when a session is active: setup, isolation, ribbon, goals, stars, result card, exit. |
| `UI/Training/TrainingBinderPresenter.cs` + `TrainingBinderKit.cs`, `TrainingDrillCard.cs`, `TrainingTab.cs` | The hub: binder, tabs, sleeves, cards, input, celebrations. Reuses `CareerBoardKit`/`Button`/`Target`. |
| `UI/Training/TrainingResultCard.cs` | The Drill Result card. |
| `Assets/Editor/TrainingHubSetup.cs` | Tools > TaxVenture > Training Hub > Set Up Scene / Capture Previews (like `SettingsBoardSetup`). |

### Existing code: changed vs read-only
- **Changed (small, additive):** `MainMenu.cs` (scene name), `GuideDirector.cs` (two notifications, start-from-script), `GuideRuntime.cs` (practice script source), `PlayerProgressData.cs` (v4 fields, Normalize), `PlayerProgressManager.cs` (practice guard, legacy constant), `EditorBuildSettings.asset` (hub scene).
- **Read-only:** guide.json, objectives.json, rewards.json, chapters.json, all gameplay (corkboard, desk, value transfer, FSM, Auditor), career board, settings board, Office_Level1/2 scenes.
- Note: `MainMenu.cs`, `EditorBuildSettings.asset` and the 3D Menu are currently modified by the in-game-settings work in the shared checkout. My edits there are one line each and will be made on top of whatever is there, without touching anything else.

### Risks
| Risk | Mitigation |
|---|---|
| Starting Office_Level1 mid-flow (at the desk, profiles already collected) may hit hidden assumptions in the FSM or scene controllers. | Phase it: Basics and the side quest need almost no setup and go first. Case drills each get a setup spike and a fallback ("start at reception" instead of "start seated"). |
| A practice run leaking into the career save. | Single guard at `PlayerProgressManager`, plus the recorder ignores practice; covered by regression tests. |
| Lessons reused out of context (a `when` that never fires). | Drill JSON can override `when`; validation reports a lesson that can never start; tests check every drill. |
| `case_002` may not load cleanly in Level_01 (orphan file). | Verify in T6; fall back to `case_001` with Arthur's "same client, now without help" framing. |
| Save version bump. | Additive fields, Normalize upgrade, signed-legacy constant, v1-v3 fixtures in tests. |
| Shared checkout with uncommitted work. | Touch only this feature's files plus the one-line edits listed above; no commits, no branch switches; CRLF; fresh .meta GUIDs. |

---

## 7. Phased tickets and checks

| Ticket | Scope | Done when |
|---|---|---|
| **T1 Save + recorder** | v4 fields, Normalize, legacy constant, practice guard (inert until T4), `LessonFinished`/`TutorialSkipped` notifications, `TrainingRecorder`. | Playing Level 1 records seen lessons; skip records skipped; old saves load unchanged. |
| **T2 Catalog** | `training.json` with the 7 cards above, `TrainingCatalog`, `Tests/TrainingRegression`. | Tests pass for all card states and save fixtures. |
| **T3 Hub scene (view only)** | `TrainingHub.unity`, binder, tabs, cards, states, Arthur line, BACK, MainMenu route, Build Settings, editor setup + preview capture. | Tutorial button opens the binder; tabs flip; cards show correct states from the save. |
| **T4 Practice Mode core + Basics** | `TrainingSession`, `TrainingDrillScript`, `PracticeModeRuntime`, ribbon/isolation, refresher replay, Leave practice, result card, return-to-tab. Drills: Your Screen, Getting Around, People & Papers. | All three Basics drills playable end to end and back. |
| **T5 Side quest drill** | Know Your Taxpayer drill and its challenge, medal on card. | Full loop with stars; career badge untouched. |
| **T6 Case drills** | Desk setup spike, practice case, Your Desk / Moving Values / Hand It In. | All three playable; no career change after a practice audit. |
| **T7 Celebrate + polish** | Stars/stamp/rank celebrations, NEW tabs, sounds, timings, phone-width checks. | Previews captured; docs complete. |
| **T8 Docs** | `Documentation/TrainingHub/` README, AGENTS, PROGRESS, KNOWN_ISSUES. | Next agent can continue from the docs alone. |

**Checks after every ticket:** scratch compile (`python Tests/make_scratch_build.py`, then the scratch csproj; baseline **0 errors / 29 warnings**), `Tests/TrainingRegression`, plus the existing `ProgressionRegression`, `GuideRegression` (+ `VerifyGuideTargets.py`), `CareerBoardRegression`, `SettingsRegression`, `ControlsRegression`, `DialogueRegression`. **The user does all Play Mode and device checks and all git actions**; I report only what I can verify.

---

## 8. Wording rules
All text is for touch: tap, drag, press and hold, swipe, joystick. Never mouse, click, WASD or keys. Challenge goals are short verbs ("Tap the icon that lists every step").

## 9. Out of scope (for now)
Level 2 mechanics (computer, stamp) until Level 2 has guide lessons; leaderboards; online anything; voice-over.

---

## 10. Open questions (my recommended default in **bold**)

1. **Scene name**: **new `TrainingHub` scene** (and fix the dead `"TutorialPage"` load), or keep the name `TutorialPage`?
2. **Practice location**: **the real Office_Level1 in Practice Mode** (reuses everything, safest), or a separate purpose-built practice room (more "isolated", but a large rebuild of desk/corkboard wiring and a long-term duplication risk)?
3. **SKIP TUTORIAL**: **skipped lessons unlock their cards with a "Skipped: try it here" note**, or they stay locked until actually seen?
4. **Old saves** that already finished Chapter 1: **unlock all Chapter 1 cards**, or require seeing them again?
5. **Rewards**: **no EXP/hearts/badges; stars, MASTERED stamp and Training Rank only**, or a small one-time EXP for the first mastery of each card?
6. **Stars**: **Done / Clean / Sharp (par time, never fails)**, or no timing at all?
7. **Practice case**: **the unused `case_002` (Marco Santos)** for new answers, or the real `case_001`?
8. **Refresher**: **skippable after the first practice**, or always played?
9. **Visual concept**: **Arthur's Training Binder on a desk** (tabs as divider tabs), or an alternative such as a wall of training lockers or a whiteboard with magnets?
10. **Card grouping**: OK with the 3 Basics / 1 Side Quest / 3 Case cards in section 2 (for example, should "People & Papers" sit in Basics as proposed)?
11. **Hub access**: **open from the first launch, showing all cards locked with "learn it in Chapter 1"**, or hide the tutorial button until something is unlocked?
