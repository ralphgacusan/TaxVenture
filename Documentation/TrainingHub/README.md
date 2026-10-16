# Tutorial Hub (Arthur's tutorial binder)

The main menu's **tutorial button** opens Arthur's tutorial binder **right in the 3D Menu**: the camera leans down into the eyes of the character standing at the computer desk, and the binder is picked up from the desk right in front of him, resting just above it, facing him. There is no separate scene, so the hub shares the menu's own room and lighting, and BACK straightens up to the main menu again. Every drill card in it lets the player replay a tutorial from the game, first **guided** (Arthur's real in-game lessons, word for word), then as a **mastery** run (goals only, no hand, no spotlight). Finishing a mastery run stamps the card **MASTERED**. Practice never gives EXP, hearts or badges and never touches the career.

Design history and the user's decisions: `PLAN.md`.

## 1. What the player sees

### The binder (in the 3D Menu)
| Part | Contents |
|---|---|
| Left page | **TUTORIAL BINDER**, "Arthur's drills for the new consultant", Arthur's brass medallion with one random greeting on a yellow note, **MASTERED n / 7** with a green bar, a hint that more drills appear as you play (only while a tab is still hidden), and **BACK**. |
| Right page | The open section: its title and colour rule, then up to three **drill cards** stacked as wide strips in clear plastic sleeves. |
| Divider tabs | Coloured tabs sticking out of the right edge: **BASICS** (blue), **SIDE QUEST** (orange), **CASE** (green). The open one sticks out further and is brighter. Tapping a tab turns the page (a quick page flip with a paper sound). |

**A drill card:** big handwritten number (01, 02, 03), title, the skills it covers, "Seen in game n / m" with a small bar, and the best mastery time once there is one. Buttons on the right:
- Never practised: one brass **PRACTICE** button (a guided run).
- Practised before: **GUIDED** (navy) and **MASTERY** (green). This is the "skippable refresher".
- Locked: a grey sheet with a paper-clipped note ("You'll sit at your desk in Chapter 1.") and a padlock. No button.
- New (open, never practised): an orange **NEW** tab on the corner.
- Mastered: a green rubber **MASTERED** stamp under the card number (clear of the text and buttons).

**Opening (the lean)**: tap TUTORIAL and the menu buttons pop away (like settings). The camera moves from the main menu view into the eyes of the character standing at the computer desk (the nearest object whose name starts with `npc_`, at `SM_Prop_Desk_03`), easing its height a little later than the forward move so it reads as bending down ("yumuko"). His model is switched off once the camera is close (we look through his eyes) and back on as the camera leaves. In the second half of the lean the binder is picked up from the desk in front of him: it starts lying flat on the desk, arcs up and tilts toward his eyes, and is held ahead of him above the desk (its lower edge always clears the desk top). The eyes look gently down at it (`viewPitch`, 30 degrees), its top tips away (`binderTilt`, 24 degrees) so it reads in 3D, and as it is picked up the two open halves fold toward you at the spine (`foldAngle`, 11 degrees), like an open binder held in both hands. A faint hand-held sway and fold breathing keep it alive. Without a character, the chair `SM_Prop_Chair_07` gives the viewpoint.

**Framing**: the binder faces his eyes and is sized to fill the view with a margin (whole spread, tabs included), so the desk shows at the edges on every screen shape. It is drawn by a URP overlay camera on its own layer (29) with a short depth range of its own, so the monitor can never cut into it and its thin layers never shimmer. The lighting is the 3D Menu's own; Arthur's medallion is matte so no highlight slides over it.

**Motion**: the cards slide into their sleeves one after another, the MASTERED bar fills, tabs glide out and back (the open one further, hover a little), and a tab turns the page (eases in to the spine, eases out on the other side, lifting slightly).

**Closing**: BACK (or Escape / Android back) lowers the binder, the camera straightens back up to the main menu view, and the menu buttons pop back in. Starting a drill lowers the binder, then loads the level.

**Coming back from a drill**, the 3D Menu opens straight on the binder (the camera already leaning), on the same tab; the card hops once, its NEW tab is gone, and if the run just mastered it the stamp **slams** on with the achievement sound.

### When cards and tabs open
- **Basics** is always open, from the very first launch (even before Chapter 1).
- A **Side Quest** or **Case** card opens once the player has **finished** one of its lessons in the level. A tab with no open card is not shown at all, so at first only BASICS is visible.
- **SKIP TUTORIAL** does not count as seeing the lessons.
- An older save that already completed Chapter 1 opens every Chapter 1 card.

### A practice run (Practice Mode in Office_Level1)
- The real level loads, but quieter: parts the drill never uses are switched off (`hide`), office drills start in the reception with Side Quest 1 counted as done, and the tracker and arrows point at the drill.
- A soft blue-grey vignette and a slim **PRACTICE ribbon** at the bottom centre (under the interaction prompt): a coloured chip (PRACTICE or MASTERY), the card title, **STEP n / m**, the time in a mastery run, and **LEAVE** (tap twice: "SURE?").
- **Guided run**: Arthur's lessons from `guide.json`, exactly as in the level (spotlight, hand, "your turn" pages), with a short drill intro and outro. No SKIP TUTORIAL button.
- **Mastery run**: one intro line, then short goals in Arthur's top bubble ("Open the paper that lists ALL of today's steps."). No hand, no spotlight. Arthur still reacts in the world as usual (corkboard comments, rejected values).
- When the last step ends, a **result card**: DRILL COMPLETE or DRILL MASTERED, the card title, the time (mastery), and the MASTERED stamp slamming in. Buttons: after a guided run **MASTERY RUN** / **BACK TO BINDER**; after a mastery run **BACK TO BINDER** / **TRY AGAIN**.
- Nothing from a practice run reaches the career: `PlayerProgressManager` ignores level results while practising.

## 2. The cards

| Tab | # | Card | In-game lessons (open it / "seen") | Guided run | Mastery goals |
|---|---|---|---|---|---|
| Basics | 01 | Your Screen | hud-tour, notes | the HUD tour and Notes lessons | open Notes, close it, go where the tracker says |
| Basics | 02 | Getting Around | move, look, follow | walking, looking, arrows | walk 6 m, turn 180°, follow the arrows to a client |
| Basics | 03 | People & Papers | talk, pickup, profiles-icon | talk, pick up, Profiles (+ dialogue tip) | collect a profile, read it, put it away, collect another |
| Side Quest | 1 | Know Your Taxpayer | guide-appears, corkboard, sidequest-card | collect four, then Arthur's corkboard lessons | collect four, open the corkboard, sort all four |
| Case | 01 | Your Desk | reception, to-desk, desk, case-folder, book-pages | reception to an open folder and book | get the case, sit, open the folder, open the book |
| Case | 02 | Moving Values | value-transfer, investigate | Marco's case: open everything, value transfer, investigate, fill | fill all three fields **correctly** (`case:correct`) |
| Case | 03 | Hand It In | office-hours, store-folder, exit-desk, auditor, submit, audit-card | fill, store, exit, Auditor, audit card, approval | fill, store, hand in, **approved** |

Case drills 02 and 03 use `Levels/Level_01/cases/case_002.json` (**Marco Santos**, freelance designer) and put his practice profile card in Profiles, so the answers are new.

## 3. Editing (no code needed)

### `Assets/Resources/Tutorials/Training/training.json`
- `title`, `owner`, `greetings` (one is picked at random).
- `tabs`: `id`, `title`, `color` (hex), `alwaysOpen`.
- `cards`: `id`, `tab`, `number`, `title`, `skills`, `lessons` (`Level_01/<guide.json lesson id>`; a lesson belongs to one card only), `drill` (file name in `Drills/`), `lockedNote`.

### `Assets/Resources/Tutorials/Training/Drills/<drill>.json`
| Field | Meaning |
|---|---|
| `id`, `title` | The drill (id = file name). |
| `levelId`, `scene` | `Level_01`, `Office_Level1` (defaults). |
| `start` | `plaza` (the level's normal start) or `office` (Side Quest 1 counts as done, the door opens, the player stands in the reception). |
| `spawn`, `face` | Office starts: the player stands 45% of the way from `spawn` to `face`, on the floor, looking at `face`. |
| `practiceCase` | A case file of the level used instead of its own (`case_002`). |
| `profile` | `id`, `name`, `description`, `classification`, `npcId`: a profile card put in Profiles at the start. |
| `objectivesDone` | objectives.json step triggers ticked at the start. |
| `hide` | Scene paths switched off for the drill (after the level has started). Never hide the reception (the office door gate lives there). |
| `guided`, `mastery` | Each has `goal`, `steps` and `hints`. |

**Steps** play in order: the first starts with the guide, each next waits for the one before. A step is either `{ "use": "move" }` (that lesson copied from the level's `guide.json`, so the in-game wording lives in one place; a `when` on the step replaces the lesson's) or a lesson written in the **guide.json lesson format** (`id`, `title`, `when`, `onlyIn`, `delay`, `duringResults`, `pages`...). A step keeps its own `when` (e.g. `near:3:...`, `event:CaseCompleted`) unless it names a lesson that is not in the drill; then it waits for the previous step. The run ends when its **last step** ends. **Hints** keep their own `when` and never block the chain.

Mastery steps have no `point` (no hand, no spotlight): use `"style": "hint"` pages with a `waitFor` for each goal. All text is for touch.

New guide condition (usable anywhere): **`case:correct`**: every graded case-folder field holds the answer key's value.

**Tools > TaxVenture > Tutorial Hub > Check Content** composes every drill (guided and mastery) against `guide.json` and reports problems in the Console.

## 4. Save (version 4)

`PlayerProgressData` gained two additive lists:
- `tutorialsSeen`: lessons finished in game, as `"Level_01/move"` (`RecordTutorialSeen`).
- `trainingRecords`: per card `practiceRuns`, `masteryRuns`, `bestMasterySeconds`; a card is mastered once `masteryRuns > 0` (`RecordTrainingRun`).

`Normalize()` upgrades v1 to v3 saves (missing lists become empty) and repairs these lists (trims, blanks, duplicates merged, negatives cleared). A plain unsigned file is still accepted only for genuine v1 and v2 saves (`SignedSinceVersion = 3`). Practice never changes EXP, hearts, badges, completed or passed levels.

## 5. Code map

| File | Role |
|---|---|
| `Assets/Scripts/Training/TrainingData.cs` | Data classes (catalog, drill, run, `DrillLesson`), `TrainingCatalog.Build` (card states from a save, read only) and `TrainingDrillScript.Compose` (a drill run into a normal `GuideScript`). Pure; tested outside Unity. |
| `Assets/Scripts/Training/TrainingSession.cs` | Hub ↔ practice hand-off (card, drill, guided/mastery, return tab and card). Loading any other scene ends it. Also `TrainingRecorder`: records finished in-game lessons (never during practice). |
| `Assets/Scripts/Training/PracticeModeRuntime.cs` | Self-bootstraps in the drill's scene while practising: setup, hiding, vignette, ribbon, LEAVE, result card, saving the run. |
| `Assets/Scripts/Training/TrainingBinder3D.cs` | The binder in the 3D Menu: adds itself to the scene, the lean to the desk and back, the overlay camera, menu button pops, binder, tabs, page flip, input, launching drills. Its settings are serialized fields with defaults at the top (`deskName`, `chairName`, `characterPrefix`, `eyeHeight`, `binderReach`, `binderLift`, `viewPitch`, `binderTilt`, `foldAngle`, `backdropDim`). |
| `Assets/Scripts/Training/TrainingBinderCard.cs` | One drill card strip (states, buttons, stamp, NEW tab, celebration). |
| `Assets/Editor/TutorialHubSetup.cs` | Tools > TaxVenture > Tutorial Hub > **Check Content**. |
| `Tests/TrainingRegression` | 513 checks: save v4, card rules, every drill composed against the real guide.json, scene paths, touch wording, wiring, metas, CRLF. |

Small changes outside the feature (all additive):
- `MainMenu.cs`: `tutorial_button` opens `TrainingBinder3D` (was the missing "TutorialPage" scene), and menu touches are ignored while the binder is up.
- `Tests/make_scratch_build.py`: skips scripts deleted since Unity last regenerated its project.
- `GuideDirector.cs`: static `LessonFinished(levelId, lessonId)` raised from `EndLesson` (not from SKIP), `View`, `HasFinished`, and the `case:correct` condition.
- `GuideTriggers.cs`: `case:correct`.
- `GuideRuntime.cs`: plays `TrainingSession.ComposeFor(levelId)` while practising and hides SKIP TUTORIAL; exposes `Practice`.
- `GuideCoachView.cs`: `SetSkipVisible`.
- `OfficeObjectives.cs`: `MarkDone(trigger)`.
- `PlayerProgressData.cs` / `PlayerProgressManager.cs`: version 4 fields, `RecordTutorialSeen`, `RecordTrainingRun`, the practice guard and `SignedSinceVersion`.
