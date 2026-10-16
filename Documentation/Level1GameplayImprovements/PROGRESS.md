# Progress

## Phase 1: Stories and indirect teaching (implemented 2026-10-16; Play Mode NOT RUN)

### Files
New:
- `Assets/Scripts/SideQuest1/CorkboardTeaching.cs`: pure C#. Contains the two-question logic (`IsFilipino`, `LivesHere`, `Compare` → None / Citizenship / Residence / Both), Arthur's intro and wrap-up (`BuildIntro`, `BuildDone`), and each profile's reactions (`ReactionFor`), with overrides.
- `Assets/Scripts/SideQuest1/TownContentLoader.cs`: on Office_Level1 load, reads `Resources/Levels/Level_01/town.json` and applies it. Other scenes go back to the built-in text.
- `Assets/Scripts/UI/Guide/GuideVoice.cs`: a hook so gameplay code can have Arthur say a line in his bubble (registered by GuideDirector).
- `Assets/Resources/Levels/Level_01/town.json`: the editable copy of all of the above.

Modified:
- `TownNpcDialogueLibrary.cs`: new 6-line talks, plus `SetOverride` / `ClearOverrides`. Unknown NPC ids can never be made "authored".
- `TaxpayerProfileData.cs`: new descriptions, plus `SetDescription` / `ClearOverrides`. Classifications are unchanged.
- `TutorialNpcController.cs`: Arthur's intro and wrap-up come from `CorkboardTeaching`.
- `CorkboardSideQuestManager.cs`: Arthur reacts to every placement. When the bubble shows, the board's own line keeps the short stats; otherwise the reaction goes there.
- `GuideDirector.cs`: `Say()` drives Arthur's bubble for a few seconds. It works after a tutorial skip, doesn't get wiped by a page, and pauses "your turn" folding while it shows.
- `guide.json`: the generic `corkboard-wrong` hint lesson was removed (Arthur's reactions replace it), and the corkboard lesson wording was updated.
- Tests: `DialogueRegression` (uses the production Arthur talks; adds the golden-rule, length and two-question checks; csproj now compiles `CorkboardTeaching.cs`) and `GuideRegression` (checks `town.json` against the built-in copy and for label-free text).

### Checks (agent)
- Scratch compile: 0 errors / 23 baseline warnings.
- DialogueRegression **799 PASS**; GuideRegression **644 PASS**; HudCheck **971 PASS**; Controls **5,734 PASS**; Progression PASS; scene paths 13/13.
- **Play Mode: NOT RUN.**

### What to check in Play Mode
1. The plaza talks are short and natural, and each one tells its story clearly.
2. The profile cards fit their layout and help without naming the answer.
3. Arthur's corkboard intro makes the two clues clear.
4. Drop a card in each wrong corner: the hint points at the missed clue (passport, home, or both), and a correct drop explains why. The bubble shows Arthur's portrait (confused for a wrong drop, wow for a correct one).
5. Edit a line in `town.json`, press Play, and see the change.

## Phase 2: Office hours (implemented 2026-10-16; Play Mode NOT RUN)

### How it plays
- **The clock:** the day starts at **7:30 AM**. One real second is 10 game seconds. The time shows in the **minimap header**, where "OFFICE" used to be. The clock runs only while the player can play: it waits during Arthur's tap pages, reward cards and pause, and while the next-morning fade plays.
- **The deadline:** the receptionist's first talk ends with two extra lines. The Auditor has a family emergency and leaves at a set time.
  - **Time allowed** = 300 s + 0.4 × (side-quest time − 360 s), kept between 270 s and 480 s real. A quick side quest gives a tighter deadline, a slow one more room. The leaving time is rounded up to the next 5 minutes.
  - **Countdown chip** (top-left, under the progress card; it moves to the corner at the desk): a clock face, "AUDITOR LEAVES 9:20 AM" and "N min left". It turns amber at 2 real minutes left and pulses red in the last 30 seconds.
  - **Arthur warns** at 15 and 5 game minutes left.
- **Missed deadline:** never cuts a conversation, a card or an audit in progress. Arthur says the Auditor went home, then the screen fades to "NEXT MORNING · DAY 2 · 8:00 AM". The case is kept and the new deadline is the time allowed × 1.4. Only the on-time bonus is lost. This can repeat with no fail.
- **On time:** handing in the case on day 1 before the deadline gives **+20 EXP**. It's a Bonus score entry: the toast says "On-time bonus", the level maximum isn't raised, and it counts toward passing.
  - The APPROVED Audit Summary shows "Office hours: On time +20 EXP" (or the day number).
  - The Level Complete card shows "Office hours: On time ✓" (or "Finished on day N").
- **Tutorial:** the HUD tour's minimap page mentions the time of day. A new `office-hours` lesson after the receptionist points at the chip, then the minimap clock, and explains the bonus and the next-morning safety net.

### Files
New:
- `Assets/Scripts/Data/OfficeHoursMath.cs` (pure: parse and format times, the allowance, the deadline, minutes left, warning crossings)
- `Assets/Scripts/UI/HUD/OfficeHours.cs` (self-bootstrapping runtime)
- `Assets/Scripts/UI/HUD/OfficeHoursChip.cs`
- `Assets/Resources/Levels/Level_01/office_hours.json`
- `Tests/make_scratch_build.py` (compile setup; it now also picks up scripts Unity hasn't imported yet)

Modified:
- `OfficeMinimapHUD.cs`: `SetTitle`
- `ReceptionistInteractable.cs`: appends the deadline lines
- `LevelScoreLedger.cs`: `ScoreSource.Bonus` and `AddBonus`
- `HudFeedback.cs`: bonus toast
- `AuditSummaryPopupUI.cs` / `LevelCompleteUI.cs`: Office hours fact
- `guide.json`: minimap text and the office-hours lesson

### Checks (agent)
- Compile 0 errors / 29 baseline warnings.
- GuideRegression **680 PASS** (36 new Office Hours checks); DialogueRegression 799, Controls 5,734 and Progression all PASS; scene paths 13/13.
- **Play Mode: NOT RUN.**

### What to check in Play Mode
1. The minimap shows 7:30 AM and the time moves while you play. It doesn't move during Arthur's tap pages or reward cards.
2. The receptionist names the leaving time. The chip appears, and Arthur's office-hours lesson points at it.
3. Finish fast: the APPROVED card shows "On time +20 EXP" and an "On-time bonus" toast appears.
4. To test a miss quickly, set `"baseSeconds": 40, "minSeconds": 30` in office_hours.json. Check the warnings, the amber and red chip, the next-morning fade (DAY 2 · 8:00 AM), that work is kept, and that there's no bonus.

## Phase 3: Consequences (implemented 2026-10-16; Play Mode NOT RUN)

### How it plays
- **Corkboard reactions:** every placed card gets a bubble:
  - a green "!" with a spinning star when it's right;
  - an amber "?" when it's wrong;
  - a grey "..." when the card is locked.
- **Corkboard lock:** every 2nd wrong drop of the same card locks it for **3 s**. Arthur starts his hint with "Hold on! Let's re-read X's card for a moment."
- **Plaza cheer:** once the side quest is solved and you're walking again, the four plaza clients cheer.
- **Marie (the case client):**
  - **After a failed audit,** once the Audit Summary closes, her sad portrait shows in the bubble ("Something's off with my file?…"). Her conference-room marker turns "available", and talking to her gives her story again as clues: one job, a salary every payday, has lived here all her life. Once per failure.
  - **After approval,** her happy thank-you bubble shows over the closing cards.
- **Real-world cost:** a failed Audit Summary now says "Why it matters: …" for the first mistake, and "Your answers were cleared…" moves to the footer. The lines come from `mistakeCosts` in the case JSON and never contain an answer.
- **No brute force:**
  - Handing in exactly the same three answers that already failed is refused: the Auditor says so and hands the folder back. It has to be stored and submitted again, which costs Office Hours time.
  - The Auditor's closing line after each failed audit gets stricter (`auditorRetryLines`; the last line repeats).
- **Passing made visible:** the progress card shows "Tutorial Level · Lv 2 needs 80%" next to the level name, which turns green ("Lv 2 unlocked!") once passed. The bars already had the passing tick.
- **Tutorial:** the progress-card tour page mentions the note, and the `audit-retry` lesson now says Marie is worried (talk to her) and that the Auditor won't re-check the same answers.

### Files
New:
- `Assets/Scripts/UI/HUD/WorldEmote.cs` (reaction bubbles)
- `Assets/Scripts/UI/HUD/ConsequenceReactions.cs` (self-bootstrapping)

Modified:
- `ComplianceIssue.cs` (`FieldKey`) and `ComplianceChecker.cs` (passes the field)
- `CaseDefinition.cs` (`clientAfterFailedAudit`, client bubbles and portraits, `mistakeCosts` / `MistakeCostDefinition`, `auditorRetryLines`, `auditorSameAnswersLine`)
- `AuditorInteractable.cs` (remembers the failed answers and the failure count; refuses identical answers; stricter lines)
- `AuditSummaryPopupUI.cs` ("Why it matters")
- `ClientInteractable.cs` (worried talk for the conference-room client; `IsCaseClient`, `IsWorried`)
- `ProfileDragCard.cs` (`LockedUntil`)
- `CorkboardSideQuestManager.cs` (`CardPlaced` event, lock after `MissesBeforeLock`)
- `GuideDirector.cs` (`portrait:<id>` moods, so the bubble can show the client)
- `HudProgressionPanel.cs` (passing caption)
- `case_001.json`, `guide.json`

### Checks (agent)
- Compile 0 errors / 29 baseline warnings.
- GuideRegression **701 PASS** (21 new: Marie's talk is two-sided and gives clues not answers, every cost line exists and never contains an answer, both bubbles, stricter retry lines, the same-answer line, the field-key plumbing, the corkboard lock constants).
- DialogueRegression 799, Controls 5,734 and Progression all PASS; scene paths 13/13.
- **Play Mode: NOT RUN.**

### What to check in Play Mode
1. Corkboard: a right drop shows a cheer bubble on the card and a wrong one a "?". The second wrong drop of the same card shows "..." and the card can't be picked up for 3 s.
2. After the side quest, back on your feet: the plaza clients cheer.
3. Fail the audit on purpose: the card shows "Why it matters: …" and Marie's sad bubble appears. Talk to her in the conference room for the clues.
4. Resubmit the identical answers: the Auditor refuses and the folder comes back. Fail again with different answers: his line is stricter.
5. Pass: Marie's happy thank-you bubble. The progress card reads "Lv 2 needs 80%", then "Lv 2 unlocked!".

### Phase 2 follow-up (2026-10-16): next morning at the office front door
- When the deadline is missed, the player is moved while the screen is dark. First, open windows are closed (Notes, profile cards, the 3D case folder view) and the desk view is left. Then the player is stood just outside the office front door, facing it, ready to walk in.
- The case progress, the stored folder and the finished side quest are all kept; nothing restarts.
- **Placement:** `frontDoorDistance` metres out from the reception doors, away from the reception floor. It steps further out if something blocks the spot, and measures the ground from just above the door's base, so an awning is never used.
- **Override:** set `nextMorningSpawn` to any scene object's path to use its exact position and facing. `nextMorningAtFrontDoor: false` turns the move off.
- Arthur's next-morning line now says "You're back at the office door…".
- Code: `OfficeHours.ReturnToFrontDoor` / `TryFrontDoorSpot`, and `HudProfileCards.CloseCards()`.
- The user asked to keep the **30-second test deadline** in `office_hours.json` for now. Restore `baseSeconds 300, factor 0.4, minSeconds 270, maxSeconds 480` before release.
- Checks: compile 0 errors; GuideRegression 701 PASS. Play Mode: NOT RUN.

## Phase 3 follow-up (2026-10-16): late penalty and the client finale (Play Mode NOT RUN)

### Late case = deduction
- Finishing the case on day 2 or later takes off **15 EXP and 1 heart per day late**, up to 3 days (`lateExpPerDay`, `lateHeartsPerDay`, `lateMaxDays` in office_hours.json). This is on top of losing the on-time bonus.
- It's a new `ScoreSource.Penalty` entry (`LevelScoreLedger.AddPenalty`), capped so totals never go below zero, and it shows a "Late case" deduction toast.
- The APPROVED Audit Summary shows "Day N · late -15 EXP"; Level Complete shows "Late (day N) -X EXP -Y hearts".

### End-of-level scene with the client (`CaseFinale`)
1. After the last case's Audit Summary, the screen fades to black.
2. While it's dark, a separate camera (above the main one) is placed in the **conference room**. A **visual-only copy of the plaza Marie model** (`modelPath`) stands at the conference client's spot. The copy is built under an inactive parent, then its scripts, colliders, rigidbodies and the profile paper are removed before anything runs; the Animator keeps the idle clip. The real conference client and its marker are hidden for the shot.
3. **The shot:** waist-up, from the room toward her, with the room as the background. Marie stands at `screenLeft` = 25% across the screen. Clear angles are tried in turn (0°, ±20°, ±40°, ±60° at three distances) until nothing blocks the view.
4. The **Level Complete card is unchanged, only placed on the right half** (`HudRewardScreen.PreferSideLayout`). Its backdrop is lighter so the room shows through.
5. Marie **talks**, using the same `ConversationMotion` head and chest choreography as conversations, layered on her idle. A **speech bubble** over her head has her name and typed lines.
6. **Her lines depend on the result** (`CaseFinaleRules`):
   - **great:** first try and on time;
   - **good:** one slip (one failed audit, or one day late);
   - **okay:** more slips.
7. The lines and staging are in `clientFinale` in case_001.json. Her old "approved" bubble is skipped when the finale runs, and Arthur's level-complete talk now waits 9 s so Marie speaks first.
8. Everything is removed when the card closes (Continue leaves the scene anyway).
- If staging fails (a path is missing), the card still appears, but on the right side.

### Files
New:
- `Assets/Scripts/UI/HUD/CaseFinale.cs`
- `Assets/Scripts/Data/CaseFinaleRules.cs`

Modified:
- `HudRewardScreen.cs` (`PreferSideLayout`)
- `LevelScoreLedger.cs` (`Penalty`, `AddPenalty`)
- `HudFeedback.cs`
- `OfficeHours.cs` (late penalty, `LateExpLost` / `LateHeartsLost`)
- `AuditSummaryPopupUI.cs`, `LevelCompleteUI.cs`
- `AuditorInteractable.cs` (`FailedAudits`)
- `CaseDefinition.cs` (`ClientFinaleDefinition`)
- `ConsequenceReactions.cs`
- `case_001.json`, `office_hours.json`, `guide.json`
- `Tests/VerifyGuideTargets.py` (also checks the finale and front-door paths)

### Checks (agent)
- Compile 0 errors.
- GuideRegression **715 PASS** (new: the finale tiers, the penalty and its cap, Marie's lines per tier never giving the answer, `screenLeft` on the left half).
- Scene paths **16/16**; Dialogue, Controls and Progression PASS.
- **Play Mode: NOT RUN.** The shot framing is computed from the room and model bounds, so check it by eye and tune `fieldOfView` / `screenLeft` / `spotPath` if needed.

### Finale fix (2026-10-16): Marie was on the roof; no bubble
- **Cause:** her height came from the plaza model's pivot offset, which fell back to 3 m. That lifted her onto the conference-room roof, and the camera followed her outside. The bubble was then pushed off the top of the screen.
- **Fix:**
  - She now stands on the room's floor (`clientFinale.floorPath` = ConferenceRoom/Floors, top surface). The spot search starts just above that floor, so a table top or the roof is never used.
  - Her visible feet (renderer bounds) are set exactly on it, whatever the model's pivot.
- **Framing:** a bit farther back, with her head at about 70% of the screen height. Waist to head top fills 65% of the frame, at least 1.1 m from her; `distanceScale` tunes it.
- The **bubble is clamped** inside the left half of the screen, so it's always visible.
- Checks: compile 0 errors; GuideRegression 715 PASS; scene paths 17/17. Play Mode: NOT RUN.

## Polish (2026-10-16): one font, the Side Quest 1 title card, release timings
- **One font:** the new `FontConsistency` (all scenes, runtime only) switches every TMP text that still uses **Minnie SDF** to the HUD font (`HudTheme.Font`, LiberationSans, also TMP's default).
  - It sweeps on scene load, every second for 20 s, then every 4 s, so panels created later are caught.
  - Scenes and prefabs are not rewritten (`SettingsPanel.prefab` and 7 scenes still reference Minnie; it now also fixes HUD K11's missing glyphs).
  - Corkboard text (board, cards, slots) is enlarged ×1.25 and made bold when it switches. The board's own feedback line is now 24 pt bold.
- **Side Quest 1 title card** (`SideQuestSplash`): it appears right after Arthur's corkboard talk, over the dimmed board.
  - Contents: cinematic top and bottom bands, the side-quest badge medal popping in, "SIDE QUEST 1", the big title "Know Your Taxpayer" with the letters popping in (white with a blue outline and glow), a brass divider, a short description, the instruction in brass capitals, and a pulsing "TAP TO CONTINUE".
  - Tap anywhere, or press Space/Enter after 0.9 s, to continue.
  - While it shows, the board can't be touched, the Office Hours clock waits, and the guide's corkboard lesson waits.
  - Its text is under `sideQuestSplash` in `town.json`.
- **Office hours back on the release timings** (300 / 0.4 / 270 / 480); GuideRegression now checks this.
- Checks: compile 0 errors; GuideRegression **719 PASS**; scene paths 17/17; Dialogue, Controls and Progression PASS. Play Mode: NOT RUN.
- **Title card fix:** the backdrop is now 92% opaque (it was 74%), so the board, the corner labels and the HUD no longer compete with the text. The canvas sort order is now 32000, above everything.
  - **Fade in (0.5 s):** the backdrop fades in first, then the content.
  - **Fade out (0.5 s):** the content fades out first, then the backdrop.
- **Bare board behind the title card, then a reveal:** when the side quest opens with a title card (`SideQuestSplash.Active`), `CorkboardSideQuestManager` holds back the corner labels (`Slot_*/Text`, scaled to 0), the dividers, the prompt panel and the profile cards. Only the corkboard shows behind the card.
  - When the card has fully faded out it calls `ReleaseReveal()`, which plays the reveal in order:
    1. the labels pop in one by one (60% → full size with a gentle overshoot, 0.08 s apart);
    2. the dividers grow out from the board's centre;
    3. the prompt panel pops up;
    4. the existing fly-in of the profile cards from the profiles icon starts.
  - **Safeguards:** a 30 s timeout releases the reveal if the card never closes, and cancelling or clearing the board always restores the labels.
  - Arthur's corkboard lesson now waits 2 s, so the reveal plays first.
  - Checks: compile 0 errors; GuideRegression 719 and Dialogue 799 PASS. Play Mode: NOT RUN.
- **Arthur's "Right on time!" moved:** he now says it the moment the folder reaches the Auditor before the deadline (on entering `AuditSubmittedState`, once per run), not at case completion, which appeared over the Level Complete card. New wording: "…If it's approved, that's an on-time bonus!" The bonus itself is still added on approval. Compile 0 errors; GuideRegression 719 PASS.

## Corrections (2026-10-16)
- **Receptionist:** a short welcome. She says welcome, that word from the plaza is good, and that Marie (from outside) is your first case with her folder already on your desk. You answer, then she gives one line about the Auditor leaving at {deadline}. That's 5 lines in total, down from 3 old lines plus 2 Auditor lines. Her portrait is now "default" instead of Marie's face. Edited in `case_001.json` (`receptionistGreeting`) and `office_hours.json` (`receptionistLines`).
- **Level Complete card after the fade:** the finale sets `HudRewardScreen.HoldReveal`. The card is built but stays invisible with its entry animation not started. It is released once the conference room has faded in, then animates in as usual, and Marie starts talking 0.6 s later.
- **Card tutorials wait:**
  - Side Quest Complete and Audit Summary lessons now wait 3 s; Level Complete still waits 9 s, now counted from when the card actually appears (`card:` conditions require `HudRewardScreen.IsRevealed`).
  - While a card lesson is waiting, the card's auto-continue waits too, so the APPROVED card no longer leaves before Arthur explains it.
- **Bigger card text:** all 13 label types on the result cards are 15% larger (`HudRewardScreen.TextScale`), with the same layout. Fact chips shrink to fit if the larger text would make them wider than the card.
- Checks: compile 0 errors; GuideRegression 719, Dialogue 799, Controls and Progression all PASS; scene paths 17/17. Play Mode: NOT RUN.
- **Result-card lessons are one page each:** `sidequest-card`, `audit-card` and `level-complete` show a single tap-to-continue page 0.5 s after their card appears. It points at the card or its button and says what to do. One tap and Arthur is gone; the card's auto-continue waits only while he shows. GuideRegression enforces one page with no waitFor (685 PASS).

## Transitions (2026-10-16)
- **Level Complete card waits for the room:** the card now holds itself whenever `CaseFinale.WillStage` is true (the current case ends the level and the client finale is enabled), even if it is built before the finale starts. It stays invisible with its entry animation not started until `CaseFinale.CardReleased`.
  - The finale releases it halfway through the conference-room fade-in (black 1 → 0.5 over 0.4 s, release, 0.5 → 0 over 0.4 s), so the card pops in as the room appears.
  - If staging fails, the card is released once the black is gone.
  - If the card is somehow never released, it gives up waiting after 10 s.
- **Level fade-in:** the new `SceneFade` (Office_Level1) starts black and fades in over 0.9 s after the first frames.
- **Leaving fades out:** Level Complete's Continue now saves the result once, then `SceneFade.FadeOutThenLoad` fades to black over 0.6 s and loads the 3D Menu level select. Double presses are ignored, and the next scene is untouched.
- Checks: compile 0 errors; GuideRegression 685, Dialogue, Controls and Progression PASS. Play Mode: NOT RUN.
- **Empty card before the finale (traced):** `HudRewardScreen`'s fading `group` sits on the card's inner content only. The backdrop, card frame, halo and fill were outside it, so a held Level Complete card showed as an empty frame, first before the audit fade-out and again during the room fade-in.
  - **Fix:** a new `panelGroup` on the whole panel. A held card is completely invisible and doesn't block touches (`ShowPanel(false)`). On release the whole panel turns on in the same frame the card's own pop-in starts.
  - Other cards are unchanged (they are shown at Build).
  - Compile 0 errors; GuideRegression 685 PASS. Play Mode: NOT RUN.
- **Empty card, second fix (stronger):** the empty frame was still seen (on the right, with only CONTINUE).
  - A held Level Complete card now has its backdrop and its whole `Reward Card` object **switched off** (`SetActive(false)`), not only faded, so no other script's alpha can bring the empty frame back.
  - It also re-holds itself (and restarts its entry animation) whenever the finale is running and hasn't released it (`CaseFinale.HoldsCards`), not only when this is decided at build time. On release, everything switches on in the same frame the card's pop-in starts.
  - Compile 0 errors; GuideRegression 685 PASS. Play Mode: NOT RUN.
- **Office door gate:** new `OfficeDoorGate` (SideQuest1, self-bootstrapping in Office_Level1).
  - With `requireSideQuestBeforeOffice: true`, the doors named in rewards.json `officeDoors` (default `Office_Entrance_Door_1`, `Office_Entrance_Door_2`) start shut.
  - Each leaf turns on its hinge until its free edge points at the other leaf's hinge. An invisible `BoxCollider` ("Office Entrance Barrier", Ignore Raycast layer, not named "door" so the guide path treats it as a wall) fills the doorway.
  - Within 2.2 m of it, `LevelGate.NotifyBlocked` shows "The office is closed. Finish Side Quest 1 first" (at most every 6 s).
  - When `LevelGate.CaseAllowed` turns true (side quest completed), the doors swing open over 1.1 s to their scene pose (or 90 degrees inward if they were placed shut), and the wall drops.
  - Fallback if the names are not found: the outermost door pair under Reception/Doors, with a warning.
  - `LevelGate.Reset` now also takes the door names; `LevelGate.Configured` lets the gate wait for the level to start.
  - Compile 0 errors; GuideRegression 685 PASS. Play Mode: NOT RUN.
