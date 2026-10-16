# Progress

Last updated: 2026-10-16

## Status
**Implemented and internally verified. Play Mode and device checks: NOT RUN (the user does these).**

| Part | Status |
|---|---|
| guide.json data model, validation, trigger and target vocabulary | Done |
| Welcome card (title letter pop, objectives from objectives.json, guide portrait, LET'S GO) | Done |
| Coach speech box (portrait medallion, moods, typewriter, tap to continue / your turn, two-tap skip) | Done |
| Spotlight dim with a gliding hole and pulsing frame | Done |
| Animated hand (tap, hold, drag, swipe, joystick; glides between targets; ripples) | Done |
| Hint bubble (non-blocking, also during conversations) | Done |
| Lessons for the whole of Level 1 (30 lessons) | Done |
| Ground arrows with a physics-grid A* route | Done |
| Floating goal marker with edge clamping | Done |
| Minimap goal pin, route dots, pop and glow on a new objective, reminder pops | Done |

## Revision 2 (2026-10-16, after your first Play Mode check)
Your feedback and what changed:
- **Welcome card**: Arthur and his name plate are bigger (490×340, up from 370×257). The button now says **START** and only fades in and breathes very slightly (1.2%) instead of bouncing.
- **Bigger dialogue**: the speech box is 1360×300 (up from 1080×236), text is 40 pt (up from 31), the portrait medallion is 250 (up from 196), and the name plate, chip, footer, skip button and hint bubble are all larger.
- **Mobile wording**: `"platform": "touch"`. The walking page says to use the joystick and the looking page says to swipe; the WASD and mouse texts are gone. `auto` now also counts the Editor as touch.
- **"Void" during the walking page**: fixed. Re-locking the mouse cursor after a tap page made the Editor report one huge mouse movement, which snapped the camera to its ±60° pitch limit (staring at the sky or the floor). `FirstPersonCameraController` now ignores the mouse for 3 frames after it is re-enabled or the cursor lock changes. This also protects against the same jump after conversations and popups. Phones were not affected.
- **Notes lesson**: new hands-on lesson. It says Notes holds ALL objectives and steps, then the player opens Notes, sees the list explained, and closes it. This uses the new `open:<notes|profiles|folder>` condition. `HudProfileCards` gained a read-only `IsShowingCards`.
- **More hands-on steps**: following the arrows, opening and closing Profiles, walking to the guide and talking to him, walking to the office, talking to the receptionist, sitting at the desk, the drag-a-value practice and walking to and talking to the Auditor are now "your turn" pages that wait for the action.
- Checks: scratch compile 0 errors / 23 baseline warnings; GuideRegression **417 PASS** (new: `open:` parsing, touch wording, START, Notes lesson present); scene paths 12/12; Controls, Dialogue, Progression and HudCheck PASS. Play Mode: NOT RUN.

## Revision 3 (2026-10-16)
- **Falling into the void at the start**: fixed. `PlayerMovement.FixedUpdate` is what pins the player's sideways velocity to zero. While a tap page held the player, it was disabled, so the body slid down the spawn slope and through a gap. `GuideDirector.HoldControls` now freezes the Rigidbody (`FreezeAll`, velocity zeroed), the same way conversations do, and restores the original constraints on release. The rev 2 mouse-jump fix stays; it was a separate issue.
- **Welcome portrait**: uses the default mood and is bigger (570×396). It glides in once (no overshoot), then only breathes ±1.5 px slowly, with no rotation.
- **Speech-box medallion**: calmer as well (±1.5 px bob, 5% pop on a mood change instead of 14%).
- **START button**: `guide.json` and the code default both say START. If Unity still shows LET'S GO!, the JSON was not reimported yet (Assets > Refresh).
- Checks: compile 0 errors / 23 baseline warnings; GuideRegression 417 PASS. Play Mode: NOT RUN.

## Revision 4 (2026-10-16)
- **Dialogue always on top**: the coach is now on its own canvas at sorting order **5000**. That is above the paper windows (1100-1900), the profile cards (2650) and drags. The objective marker stays on a separate canvas at 400, under the windows.
- **"Your turn" pages get out of the way**: once the text has been read (3 s after typing ends), or as soon as the player starts doing it (moves, looks, changes mode, opens Notes, Profiles or the Case Folder), the speech box folds into the small non-blocking bubble at the top. The hand and the frame stay. Tap pages (e.g. explaining the open Notes) still show the full box on top, with the window dimmed behind it except the spotlight.
- **Stuck at the Case Folder**: fixed. `GameplayEvents.RaiseCaseFolderOpened` / `RaiseTaxCodeBookFirstOpened` fire only from the old click interactables, not from dragging the folder or book off the pile. These pages now also finish when the open folder or book object becomes active (new condition `active:<target>`). Other gaps closed the same way:
  - Closing Notes and Profiles uses `not:open:notes` and `not:open:profiles` (new `not:` prefix), not a camera-mode guess.
  - The corkboard page also accepts a wrong drop (`corkboard:wrong`).
  - The store-folder lesson also starts after 3 placed values (`value:placed#3`).
- **Goal accuracy**: the goal position, distance, floating pin and the spotlight frame for `waypoint` now come from the goal's visible renderer bounds, not its transform pivot. The Desk's pivot is out in the corridor, which showed "Your desk · 1m" in the wrong place and hid the arrows because the player looked "arrived".
- **Arrows visible and on the ground**:
  - The route search now uses the floor at the player's level (from the ground under the player), not the lowest surface. Before, it could pick hidden geometry under the visible ground, so the arrows were drawn underground.
  - Each arrow is snapped to the ground under it with a raycast.
  - The arrows are bigger (0.8 m) and lifted 6 cm.
- Checks: compile 0 errors / 23 baseline warnings; GuideRegression **422 PASS**; scene paths **13/13** (now including `active:` targets); Controls, Dialogue, Progression and HudCheck PASS. Play Mode: NOT RUN.

## Revision 5 (2026-10-16)
- **Value-transfer pointing at TIN ID**: fixed. `type:CaseFolderFieldRow` chose the row nearest the screen centre, and Name/TIN are rows too. There are two new smart targets:
  - `chip`: a Tax Code value on screen that a still-empty field accepts.
  - `field`: the field that accepts the value in hand (the selected value, else the chip being pointed at), preferring empty ones. It never picks Name or TIN.
  - These use new read-only, non-logging `CaseFolderFieldRow.IsDestination` / `HasValue` / `WouldAccept`.
- **"All three fields filled" too early**: removed the `value:placed#3` shortcut, which counted replaced values. The lesson waits for `case:filled` (the real three answers in `CaseManager.CurrentCase`).
- **Auditor hand-in**: the `auditor` lesson now only walks you to him (`near:3.5`). A new `submit` lesson explains that talking only gives a reminder; you hand in by dragging the Case Folder icon onto him. The hand demonstrates the drag, then waits for `state:AuditSubmittedState`.
- **Arrows not shown at the start**: `trail.waitForGuide: true`. The ground arrows, floating pin and minimap pin stay hidden until the page with `"revealArrows": true` (the first "follow the arrows" page, "Ta-da!"). Skipping the tutorial, or having no unfinished lesson that would reveal them, shows them at once.
- **No walking or looking while a reward/summary card is up** (Audit Summary, Side Quest Complete, Level Complete, Case Result): new `HudModalControlHold`, built by OfficeHudRuntime. It uses the new shared `PlayerControlHold` (reference counted by owner; pauses movement, look and interaction, freezes the body, frees the cursor, and waits for conversations). The guide's tap pages now use the same service, so the two never fight.
- **Drag ghost and drop aim**: the floating value tag now sits just above the finger, with a small brass pointer whose tip is the drop point (`DragGhostSkin` pivot -0.55 → -0.3). New `DropAimIndicator` frames the field under the finger while dragging, using the same UI raycast as the drop: a pulsing green frame with "DROP HERE" when it fits, red with "WON'T FIT" when it doesn't.
- HUD check updated for the new ghost offset (it pinned the old pivot value).
- Checks: compile 0 errors / 23 baseline warnings; GuideRegression **448 PASS**; scene paths 13/13; HudCheck **971 PASS**; Controls, Dialogue and Progression PASS. Play Mode: NOT RUN.

## Revision 6 (2026-10-16)
- **Moving to the next page** (explained, no check):
  - The welcome talk now says "tap anywhere to hear my next line".
  - New `book-pages` lesson: the right-edge arrow turns to the next page of the Tax Code Book and the left-edge arrow goes back. It also says which section holds which field.
- **Investigating with Notes and Profiles anywhere**: new `investigate` lesson at the desk. Notes and Profiles work at the desk too. You open your client's profile next to the book (the page waits for that), learn the profile → book → folder routine, close the profile, and get a reminder that Notes shows what's next.
- **Reward-card talks**: `sidequest-card`, `audit-card` and `level-complete`. Arthur explains each card (EXP, hearts, badge; audit findings and deductions or APPROVED; level totals and unlocking). The level-complete talk closes the tutorial ("that wraps up my tutorial for Level 1…") and asks you to tap Continue to save and go to the level select. The old `case-done` hint was removed.
  - New condition `card:<audit|sidequest|level|result|any>`, new targets `hud:card` and `hud:continue`, and new lesson flags `duringResults` (may show over a card) and `endIfGone` (ends when the card closes).
  - `HudRewardScreen` hid every other overlay canvas, which would have hidden Arthur. Canvases carrying the new `HudKeepVisibleDuringRewards` marker are now exempt, and the coach canvas carries it.
  - `HudRewardScreen` gained read-only `Top` / `Title` / `Card` / `ContinueButton`, plus a static `HoldAutoContinue` that pauses the APPROVED card's 3.5 s auto-continue while Arthur explains it.
- Checks: compile 0 errors / 23 baseline warnings; GuideRegression **556 PASS**; scene paths 13/13 (the book's `LeftEdgeIndicator` / `RightEdgeIndicator` exist in the scene); HudCheck 971, Controls, Dialogue and Progression PASS. Play Mode: NOT RUN.

## Revision 7 (2026-10-16)
- **The guide is "Arthur" everywhere**:
  - His in-world conversations (`TutorialNpcController`, both `DialogueBuilder`s) now use the speaker name Arthur.
  - The objective step says "Talk to Arthur, the tutorial guide." (`objectives.json` and the built-in copy in `TaskListProvider`, kept in sync).
  - The arrow label says Arthur.
  - The DialogueRegression mirror was updated.
- **"Destroyed" NPC faces in conversations**: dialogue close-ups put the camera about 0.5 m from the head centre while the main camera clips everything nearer than 0.3 m, so the low-poly head was sliced open. `DialogueCameraDirector` now:
  - pulls the camera's near plane in to 0.03 m during a conversation and restores it after;
  - keeps close-ups at least 0.55 × character height in front of the face;
  - never places the camera closer than 0.42 × height to the head.
- **Interacting from too far away**: the plaza clients, Arthur and the profile papers were tapped through the EventSystem's physics raycaster, which has no distance limit. The new `InteractionRange.PlayerCanReach` (3 m, measured on the floor plan to the closest collider point) now guards their taps and their hover outlines. The office NPCs and items already go through `Interactor` (2.5 m from the screen centre). The guide's "talk" lesson now triggers at 3 m to match.
- Checks: compile 0 errors / 23 baseline warnings; GuideRegression 556, HudCheck 971, Controls 5,734, Dialogue 796 and Progression all PASS; scene paths 13/13. Play Mode: NOT RUN.

## Files
New:
- `Assets/Scripts/UI/Guide/`: `GuideRuntime`, `GuideScript`, `GuideTriggers`, `GuideTargets`, `GuideDirector`, `GuideCoachView`, `GuideHand`, `GuideWelcomeCard`, `GuideArt`, `GuideTapCatcher`, `ObjectiveGuide`, `GuideGridPath`, `ObjectiveTrail`, `ObjectiveBeacon` (each with a .meta, plus the folder .meta)
- `Assets/Scripts/UI/OfficeMinimapObjective.cs` (+ .meta)
- `Assets/Resources/Tutorials/Level_01/guide.json` (+ .meta, folder .meta)
- `Assets/Resources/Tutorials/Portraits/Arthur-{Default,Wow,Confused,Sad,Angry}.png` (+ .meta): copies of `Assets/AssetIntegration/Arthur (Tutorial NPC)` with fresh GUIDs and a 1024 max size
- `Tests/GuideRegression/` (Program.cs, csproj, .gitignore) and `Tests/VerifyGuideTargets.py`
- This documentation folder

Modified:
- `Assets/Scripts/UI/OfficeMinimap.cs`: creates, ticks and disposes `OfficeMinimapObjective` (8 lines).
- `Assets/Scripts/UI/HUD/HudProfileCards.cs`: read-only `IsShowingCards` (rev 2).
- `Assets/Scripts/Player/FirstPersonCameraController.cs`: ignores the mouse for 3 frames after re-enable or a cursor-lock change (rev 2).
- `Assets/Scripts/Data/DragDrop/CaseFolderFieldRow.cs`: read-only `IsDestination`, `HasValue`, `WouldAccept` (rev 5).
- `Assets/Scripts/UI/DragGhostSkin.cs`: closer to the finger, with a drop pointer (rev 5).
- `Assets/Scripts/UI/HUD/OfficeHudRuntime.cs`: adds `DropAimIndicator` and `HudModalControlHold` (rev 5).
- `Assets/Scripts/UI/HUD/HudRewardScreen.cs`: read-only card info, `HoldAutoContinue`, and keeps `HudKeepVisibleDuringRewards` canvases visible (rev 6). New: `HudKeepVisibleDuringRewards.cs` (+ .meta).
- Rev 7: `DialogueCameraDirector.cs` (near plane during conversations, safe close-up distance), `TutorialNpcController.cs` (speaker Arthur, reach guard), `TaxpayerNpcInteractable.cs` / `TaxpayerProfileWorldPickup.cs` (reach guard), `TaskListProvider.cs` + `objectives.json` (step text). New: `Assets/Scripts/Player/InteractionRange.cs` (+ .meta).
- New in rev 5: `Assets/Scripts/Player/PlayerControlHold.cs`, `Assets/Scripts/UI/HUD/HudModalControlHold.cs`, `Assets/Scripts/UI/HUD/DropAimIndicator.cs` (+ .meta).

## Verification (agent, 2026-10-16)
- Scratch compile (329 runtime scripts): **0 errors / 23 warnings**, the same as the baseline. No warnings come from the new files.
- `Tests/GuideRegression`: **310 checks PASS**. They cover the trigger parser (events, counts, conditions, wait-only kinds, `|` splitting that keeps area paths together, invalid input), target validation, and guide.json content: lesson ids and `after` links, every when/onlyIn/waitFor/point/to, gestures, moods with portraits, portrait files present, every objective step has a waypoint, every waypoint step exists, every `type:` is a real component, and the level opens with the welcome card.
- `Tests/VerifyGuideTargets.py`: **12/12** scene paths exist in Office_Level1.
- Existing suites: Controls **5,734 PASS**, Dialogue **796 PASS**, Progression **PASS**, HudCheck **971 PASS**.
- Layout previews rendered from the code's constants at 1920×1080 (`preview-coach.png`, `preview-welcome.png`). The first welcome-card layout overlapped the guide portrait with the objective rows. This was fixed by placing the card and portrait side by side in one scaled stage.
- **Play Mode / device: NOT RUN.**

## What to check in Play Mode (user)
1. Start Office_Level1. The welcome card appears once the HUD is up. The letters pop in, both objectives slide in, and START closes the card.
2. HUD tour: the spotlight glides over the tracker, progress card, minimap, Notes, Profiles, Folder and Settings, and the hand taps each one. The player can't walk during these pages.
3. Walking and looking pages end after walking 4 m and turning 90°. On a phone, check the joystick and swipe hand animations.
4. The arrows lead to the nearest client still waiting to talk, then to the profile papers, the guide, the office doors, the receptionist, the desk and the Auditor. Watch for arrows going through walls or stopping early (see K1 and K2).
5. Minimap: a pulsing pin and route dots, a pop with a glow on each new objective, and reminder pops while far away.
6. Corkboard and desk lessons: the hand drags from the card to a corner, from the folder pile, and from a value to a field. Check that the spotlight lines up with the 3D objects.
7. SKIP TUTORIAL (tap twice) ends the lessons and the arrows stay.
