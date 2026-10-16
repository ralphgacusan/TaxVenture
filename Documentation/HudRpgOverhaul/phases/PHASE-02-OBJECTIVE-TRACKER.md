# Phase 2 — Objective tracker

Status: **Revision 9 implemented — awaiting user Play Mode check**

## Goal

Show the game's real case objective at all times, so the player doesn't have to open Notes to know what to do next.

## User decision (2026-10-15)

The task list does not match the real Level 1 flow (see KNOWN_ISSUES K6). The user chose to **keep the current list for now, but make it editable data (JSON)** so they can rewrite it to match the actual flow without code changes.

## What was built

- **Editable task list:** `Assets/Resources/Tasks/task_list.json`.
  - Each entry is `{ "state": "<IGameState class>", "text": "...", "header": false }`; the order is the display order.
  - The file includes notes (fields starting with `_`, which the game ignores) listing every state name and which ones Level 1 actually enters or never enters.
  - Invalid entries are skipped with a Console warning. If nothing valid remains, or the file is missing, the original built-in list is used (`TaskListProvider.GetBuiltInTasks`).
  - The content is identical to the previous hard-coded list (verified by test).
- **`TaskProgress`** (pure, shared by Notes and the tracker) gives each row a status:
  - **Done:** its state was reached, or it is the current state. This is the unchanged Notes rule.
  - **Skipped:** not reached, but a later task was.
  - **Active:** the first unreached task after the latest reached one.
  - **Pending:** everything else.
  - Header rows are never steps. `CaseCompleteState` / `LevelCompleteState` means the case is complete.
- **`HudObjectiveTracker`** (top-center, same navy/brass style and scale as the card):
  - Header row: `OBJECTIVE`, a `CASE 1 / 1` chip, and the clock (`HudTimer`'s time, so it is unchanged).
  - The active task text, up to two lines and auto-sized, with a cyan ring.
  - One bar segment per step, coloured green (done), cyan (active, gently pulsing), light slate (skipped) or dark (pending), plus a `5 / 11` counter.
  - When the step shown is completed: the ring turns into a green check with a pop, the text is struck through, then it slides out and the next step slides in. Animations wait while the HUD is hidden (dialogue, desk, results).
  - At case completion it shows **Case complete** in green.
  - On narrow screens it shrinks just enough to stay clear of the progression card (fits 4:3 at ×1.13).
- **Notes panel rows** use the same rule: `DONE` (green, struck through), `NOW` (blue, bold), `TO DO`, `SKIPPED` (grey). The header is a bold title. The colours are dark enough for the white paper.
- **`HudTimer`:** its time is unchanged. It now rebuilds its text only when the second changes, without allocating, and exposes a read-only `ElapsedSeconds`. Its own on-screen text is hidden through a runtime CanvasGroup (alpha 0) and restored if the HUD is destroyed.

## Files

New: `Assets/Scripts/Data/TaskProgress.cs`, `Assets/Scripts/UI/HUD/HudObjectiveTracker.cs`, `Assets/Resources/Tasks/task_list.json` (all with .meta files, including the `Tasks` folder).
Modified:
- `TaskDefinition.cs`: `IsHeader` field and constructor overload.
- `TaskListProvider.cs`: JSON loading with fallback.
- `NotesPanelUI.cs`: row formatting through `TaskProgress`, and a static `RecordReachedState`.
- `HudTimer.cs`.
- `OfficeHudRuntime.cs`: builds the tracker, hides the timer text and fits the layout.
- `HudTheme.cs`: check sprite, clock formatter, `HudScale`, `Skipped` colour.
- `HudProgressionPanel.cs`: shared `HudScale`; public `Width`.

## Acceptance

- [x] Compile 0 errors / 23 baseline warnings; Controls 5,734, Dialogue 796 and Progression suites PASS; 205 HUD assertions PASS (agent).
- [ ] The tracker shows top-center with `OBJECTIVE`, `CASE 1 / 1` and a running clock, and the old timer text is gone (user).
- [ ] The tracker's step always equals the Notes row marked `NOW`, and the counter equals the number of `DONE` rows (user).
- [ ] Completing a step plays check → strike → slide to the next step once, after any dialogue or desk view closes (user).
- [ ] After the Auditor approves the case, it shows **Case complete** (user).
- [ ] Editing a task's text in `task_list.json` changes Notes and the tracker on the next Play; a bad state name logs a warning and skips that row (user).
- [ ] No overlap with the card, settings or minimap at 1920×1080, 2400×1080 and 1024×768, and no new Console errors (user).

## Manual check (Unity)

1. Play Office_Level1. Top-center should read `OBJECTIVE · CASE 1 / 1 · 00:00:0x`, with the first step "Meet with the receptionist…" and `0 / 11`. If the case load already counted it, you will see "Review the case folder…" and `1 / 11` instead (K7); please tell me which one you get.
2. Talk to the receptionist. After the dialogue closes, step 1 completes (check animation) and the tracker moves on.
3. Open Notes (the HUD icon) and compare: the `NOW` row equals the tracker text.
4. Play through: case folder → client → Tax Code Book → corkboard → stamp → print → Auditor. Watch for **Compute** turning light-slate (skipped) when you reach the corkboard.
5. After the Auditor approves, the tracker shows **Case complete**. With the current list the counter ends at `8 / 11`; this is expected (K6).
6. Optional: change one task's `text` in `task_list.json`, press Play, and confirm the new wording.
7. Resize the Game view through 1920×1080, 2400×1080 and 1024×768.

## Revision 2 (user feedback, 2026-10-15)

Feedback from the first Play Mode check: use the real Level 1 flow, with "Finish Side Quest 1" and "Finish a Tax Case" as the main objectives. The tracker stays one step at a time. Notes becomes a paper page with checkmarks and holds the player still like the taxpayer profile viewer. The screenshot also showed the card values clipped to "0 □".

**Objectives are now editable per level:** `Assets/Resources/Levels/Level_01/objectives.json`.
- Each main objective has a `title` and ordered `steps`. Each step has `text`, a `trigger` and an optional `progress`.
- **Triggers:**
  - `event:<name>`: the side-quest/town and office events listed in the file's `_triggers` note;
  - `state:<FSM state>`;
  - `area:<scene path>`: checked only while it is the current step, so walking in early does not count.
- **`progress: "profiles"`** shows the live taxpayer-profile counter, e.g. `(2/4)`.
- **Validation:** bad steps are skipped with a warning, and a built-in copy is used if the file is unusable.
- **Other scenes are unaffected:** the legacy list `TaskListProvider.GetTasks()` is unchanged. The interim `Assets/Resources/Tasks/task_list.json` from revision 1 was removed.

| Main objective | Step | Trigger |
|---|---|---|
| Finish Side Quest 1 | Explore the plaza and talk to the 4 clients to get their taxpayer profiles (n/4) | `event:AllProfilesCollected` |
| | Talk to the tutorial guide | `event:TutorialGuideTalkedTo` (new notification, raised when the guide's talk opens the corkboard) |
| | Finish Side Quest 1 at the corkboard | `event:SideQuestCompleted` |
| Finish a Tax Case | Go to the office | `area:Environment/OfficeBuilding/Inside/Floors` |
| | Talk to the receptionist | `event:ReceptionistTalkedTo` (GameplayEvents.OnNotesUnlockRequested) |
| | Go to the main office room and finish the tax case | `state:ComplianceAuditState` (case stamped, return printed) |
| | Submit the case to the Auditor in his office | `state:CaseCompleteState` (the Auditor approved it) |

**Progress rule (`TaskProgress` v2):**
- A step is done when its trigger happened.
- The current step is the first unfinished one in list order. A step done early still shows checked.
- A main objective is checked when all its steps are.
- The "skipped" status was removed.

**Shared record:** `OfficeObjectives` is an always-on recorder that both the tracker and the Notes paper render. This also sidesteps K9.

**Tracker:**
- The caption is the current main objective, e.g. `FINISH A TAX CASE`.
- The step text carries the live `(2/4)` counter.
- The segments are grouped per main objective.
- It ends with **All objectives complete**.
- The case chip shows only for multi-case levels.

**Notes paper (`NotesPaperView`):**
- The existing Notes panel restyled at runtime as cream paper: tape, red margin, and ruled lines with the text sitting on them.
- Big checkboxes for main objectives (a green **DONE!** stamp when finished) and small checkboxes for steps.
- Drawn green checkmarks, struck-through done steps, a yellow highlighter with a red ">" on the current step, and the live counter.
- Title `Objectives` with `Level 1 - Tutorial Level | 4 of 7 done`, an X close button and a footer hint.
- The original Notes texts are hidden, not deleted.

**Notes holds the player:** opening it uses the same lock as the taxpayer profile viewer (`CameraController.TryBeginProfileViewer`): no walking, looking or world taps, and the joysticks are hidden. It is released on close. At the desk or computer the player is already still, so no lock is taken. In other scenes Notes stays a free-moving window.

**Clipped values fixed:** the HUD font (Minnie SDF) is wide and has no "…" glyph. Labels now auto-shrink to fit (down to 60%) and never draw an ellipsis.

**New notification hooks (no logic change):** `TownEvents.OnTutorialIntroCompleted` (raised in `TutorialNpcController` right where it marks the side quest started) and `TaxpayerProfileCollectionHUD.RequiredProfiles`.

### Revision 2 acceptance (user)

- [ ] The card values read `0 / 100` and `0 / 5` (no "□").
- [ ] The tracker starts on `FINISH SIDE QUEST 1` with "Explore the plaza and talk to the 4 clients…  (0/4)", and the counter rises with each profile.
- [ ] The guide talk, the corkboard, entering the office, the receptionist, finishing the case (print) and the Auditor approval each tick exactly their step, with the check-and-slide animation.
- [ ] The Notes icon opens the paper: steps checked as above, the current step highlighted, and the side quest marked **DONE!** after the corkboard.
- [ ] With Notes open you cannot walk or look, and the joystick is hidden. X or the icon closes it and restores control.
- [ ] Editing `objectives.json` (e.g. one step's text) shows up on the next Play.

## Revision 3 — fixes from the revision 2 Play Mode check (2026-10-15)

The user reported that the objectives never updated, on either the HUD or the paper, and the screenshot showed boxes instead of `/ ( ) -`.

1. **Objectives not updating (agent bug):**
   - **Cause:** `OfficeObjectives` set `enabled = false` when no `area:` step was current. Unity then ran `OnDisable`, which unsubscribed every objective event, so from the first frame no trigger was recorded. The paper's `(4/4)` came from the live profile count, but the "all collected" event was never heard.
   - **Fix:** the component never toggles `enabled`; an idle check replaces it.
   - **Safety net:** on start it also records anything already true in public game state: all profiles collected, the guide talked to or the side quest finished (`TutorialNpcController` flags, or the profile stack locked to one card), and the current FSM state.
2. **Boxes instead of characters:** the scene HUD font, Minnie SDF, has no real glyphs for `! " # $ % ' ( ) * + - / < = > ? @` or the digits **3** and **8**. They are mapped to a placeholder box. The runtime HUD now loads fonts from `Assets/Resources/HUD/HudFonts.asset` (new `HudFontSet`), with no scene edits:
   - **LiberationSans SDF** for the card and the tracker;
   - **Gloria Hallelujah SDF** (handwriting, already in `Assets/fonts`) for the Notes paper.

**Verification:**
- Compile: 0 errors / 23 baseline warnings.
- HUD check: **611 PASS**. New guards:
  - `OfficeObjectives` source must never assign `enabled`;
  - `HudFonts.asset` must reference LiberationSans and Gloria;
  - both fonts must have real (non-box) glyphs for every character the HUD prints and for all of `objectives.json`;
  - the detector also confirms Minnie lacks `/` and `(`.
- Regression suites unchanged since revision 2 (only HUD files changed). Play Mode NOT RUN.

### Revision 3 checks (user)

- [ ] The card shows `0 / 100` and `0 / 5`, and the tracker and paper show `(0/4)` and `Level 1 - Tutorial Level` with no boxes.
- [ ] Collecting profiles raises `(n/4)` on both. The 4th ticks the plaza step on the tracker (check, then slide) and on the paper.
- [ ] Talking to the guide, then solving the corkboard, ticks each step. The paper shows **DONE!** on Finish Side Quest 1.
- [ ] Entering the office, the receptionist, finishing the case and the Auditor approval tick the remaining steps.
- [ ] If you test from a save point mid-way, steps already finished show as checked when the scene starts.

## Revision 4 — feedback from the revision 3 Play Mode check (2026-10-15)

1. **The Notes icon needed two taps.**
   - **Cause:** the icon had three tap handlers: `Button → NotesPanelUI.Toggle`, plus `FloatingWindowIcon` and `HudSubmittableIcon`, which both toggle the window directly. One tap ran two or three toggles.
   - **Fix:** in Office_Level1, `OfficeHudRuntime` disables the two extra handlers on `NotesIconButton` at runtime (restored on destroy). `NotesPanelUI.Toggle` is the single path, which also refreshes the paper and takes the movement lock.
2. **Settings and the HUD icons fade out during dialogue** (and the case-result popup), with the same fade as the rest of the HUD. They don't take taps while hidden. They stay visible at the desk, where the icons are needed.
3. **"Go to the office" fired too late.** It now completes on the reception floor and doors: `area:Environment/OfficeBuilding/Rooms/Reception/Floor|Environment/OfficeBuilding/Rooms/Reception/Doors`. Area triggers accept several paths joined with `|`. The old `Inside/Floors` path was also ambiguous, because several objects are named `Floors`.
4. **The case folder flow now drives the objectives:**
   - **Store:** drag the desk case folder onto the HUD folder icon (existing `CaseFolder3DDrag`/`FolderToHUDIcon`; the count goes 0 → 1). This checks "Go to the main office room, finish the tax case and drag the case folder to your folder icon" (`event:CaseFolderStored`).
   - **Count 0 blocks submitting:** while the count is 0 the HUD folder icon cannot be dragged out. There is no ghost, no submit, and the refused drag does not open the folder window (`HudSubmittableIcon.CanDragOut`, Case Folder kind only). A tap still opens the folder window as before.
   - **Submit:** drag the icon (count 1) to the Auditor's tray. Approval → `CaseCompleteState` checks "Submit the case folder to the Auditor in his office", and the case/level completes as before.
   - **Failed audit:** the existing retry path returns the folder (count 1 → 0, `FolderToHUDIcon.ResetToUnstored`). The step's new `"undoneBy": "event:CaseFolderReturned"` unchecks the finish-case step, so the tracker and paper go back to it. Repeat failures work too.
   - **New notifications (no logic change):** `GameplayEvents.OnCaseFolderStored` / `OnCaseFolderReturned`, raised by `FolderToHUDIcon`.
   - **Testable record:** `ObjectiveLog` (new, pure) holds the trigger record and the undo rule.

**Verification:**
- Compile: 0 errors / 23 baseline warnings.
- HUD check: **661 PASS**. New checks cover:
  - the store → fail → uncheck → resubmit → fail → resubmit → approve loop;
  - an undo with nothing stored changes nothing (this caught and fixed a needless-refresh bug);
  - area unions and the reception area;
  - the folder events;
  - the drag gate and the single Notes handler, by source check;
  - `undoneBy` matching between the JSON and the built-in copy.
- Controls 5,734 / Dialogue 796 / Progression all PASS.
- Play Mode NOT RUN.

### Revision 4 checks (user)

- [ ] One tap on the Notes icon opens the paper; one tap closes it.
- [ ] During any conversation, Settings and the right-hand icons fade out with the rest of the HUD and come back after it.
- [ ] "Go to the office" ticks as soon as you step into the reception (if not, tell me; a Console warning would say the area was not found).
- [ ] At the desk, dragging the closed case folder onto the HUD folder icon makes it `1` and ticks the finish-case step on the tracker and paper.
- [ ] With the icon at `0`, dragging it does nothing (no ghost).
- [ ] Dragging the icon (at `1`) to the Auditor: on approval, the last step ticks and the case/level completes.
- [ ] On a failed audit, the folder returns to the desk, the icon goes to `0`, and the finish-case step unchecks and becomes current again.

## Revision 5 — paper windows (2026-10-15)

The user asked that:
- the HUD stays visible while the taxpayer profiles or Notes are open;
- Notes, the profiles and the Case Folder behave as windows that can all be open at once, with the touched one on top;
- Notes and the Case Folder animate in and out;
- Notes and the profiles look slightly tilted, with a little paper depth.

1. **HUD stays up:** the paper windows hold the player through the profile-viewer mode, and the HUD and minimap now stay visible in that mode. They still fade for dialogue, results, settings and pause. The minimap change is a one-line visibility rule in `OfficeMinimap`.
2. **One window family** (`FloatingWindow`, which already did multi-open, drag and tap-to-front):
   - **Taxpayer profiles** in Office_Level1 open as paper cards (new `HudProfileCards`), one `FloatingWindow` per profile. They show the same profiles as the 3D viewer (`CollectedProfilesWorldPanel.GetProfilesToShow`, now public), fanned out in a 2×2 layout with individual tilts.
   - **Each card:** a paper stack with depth, a blue "TAXPAYER PROFILE" header with a paperclip and number, the name, the description, a blank classification line (no answer shown) and an X.
   - **Putting cards away:** the profile icon toggles all cards. Dropping a card on the icon or tapping X puts that one away.
   - **Routing:** the icon tap is routed by a new `TaxpayerProfileCollectionHUD.IconClickOverride`. Other scenes keep the 3D viewer.
   - **Shared hold:** new `HudWindowLock`. The first open paper window (Notes or cards) holds the player still and hides the joysticks; the last one to close releases it.
3. **Animations:** an opt-in `FloatingWindow.UsePaperMotion(tilt)`.
   - Open: the window pops in (scale 0.82 → 1 with overshoot, fades in, swings 7° into its resting tilt).
   - Close: it tips 6°, shrinks and fades, then hides.
   - It is used by Notes (−1.6°), the profile cards (±2–3°) and the Case Folder (its own current angle).
   - Windows without paper motion behave exactly as before. A window that is animating closed counts as closed for toggles.
   - Limit: the desk flow's `CaseFolderUI.Show`/`Hide` still toggle the panel directly, so the motion plays when the Case Folder is opened from its HUD icon.
4. **Paper depth:** Notes and the cards are drawn as a front sheet with a soft shadow, a darker paper edge and two offset sheets underneath (`NotesPaperView.BuildPaperStack`). The original Notes panel image is kept as the invisible hit area for dragging and tap-to-front.

**Verification:**
- Compile: 0 errors / 23 baseline warnings.
- HUD check: **672 PASS**. New checks cover the shared hold (counting, null and double-release safety) and source guards for:
  - HUD and minimap visibility while held;
  - paper motion on Notes and the Case Folder;
  - the cards being raycastable `FloatingWindow`s with paper depth;
  - the icon override running before the 3D viewer;
  - toggle and close semantics.
- Controls 5,734 / Dialogue 796 / Progression PASS.
- A mock of the 2×2 card fan at 1920×1080 shows all text readable (spacing widened after the first mock overlapped).
- Play Mode NOT RUN.

### Revision 5 checks (user)

- [ ] Opening the profiles (icon) or Notes keeps the top-left card, the tracker and the minimap on screen. Walking and looking stay frozen and the joystick is hidden.
- [ ] The profile icon fans out paper cards. Each can be dragged and tapped to the front. X or dropping it on the icon puts it away. The icon closes them all.
- [ ] With Notes, the profile cards and the Case Folder (from its icon) all open, tapping any one brings it on top.
- [ ] Notes, cards and the Case Folder pop in with a small swing and tip away when closed.
- [ ] Notes and the cards look slightly tilted, with paper sheets visible underneath.
- [ ] Closing the last paper window gives walking and looking back.

## Revision 6 — open everything together, front layering, fly to/from icons (2026-10-15)

1. **Profiles would not open while Notes was open.**
   - **Cause:** the scene's TaxpayerProfileIcon button calls `CollectedProfilesWorldPanel.OnHudIconClicked` directly, not `TaxpayerProfileCollectionHUD.OnHudIconClicked`. So revision 5's paper cards were never reached: the old 3D viewer still opened, and it refuses while Notes holds the player.
   - **Fix:** `CollectedProfilesWorldPanel.OnHudIconClicked` now asks `IconClickOverride` first (when its 3D viewer is not open). In Office_Level1 the profile cards open together with Notes and the Case Folder.
2. **Front layer across all windows:** new `HudWindowFocus`.
   - **What it does:** on a press it does one UI raycast and brings the topmost touched window to the front. That covers any `FloatingWindow`: Notes, profile cards, the Case Folder, and other document windows.
   - **Why it's needed:** a press on a button, value chip or drag source used to consume the press, so the window didn't come forward.
   - **Side effects:** none. It never consumes the press, so the touched control still works.
3. **Fly from/to the HUD icons** (`FloatingWindow.UsePaperMotion(tilt, icon)`):
   - **Open:** the paper starts at its HUD icon (small, tilted 28° extra, transparent), flies along a soft upward arc to its spot (0.42 s ease-out), grows with a slight overshoot and swings into its resting tilt.
   - **Close:** it flies back along the arc into the icon (0.3 s), shrinking and fading at the end.
   - **Icons:** Notes uses the Notes icon, the Case Folder its folder icon, and the profile cards the profile icon. The cards are staggered 0.07 s apart and land in their fan spots (`OpenWindowWithOffset`).
   - **Mid-flight:** grabbing a window lands it instantly.

**Verification:**
- Compile: 0 errors / 23 baseline warnings.
- HUD check: **679 PASS**, including new guards for the direct-call routing, the focus helper, the fly-from-icon wiring and the mid-flight landing.
- Controls / Dialogue / Progression PASS.
- Play Mode NOT RUN.

### Revision 6 checks (user)

- [ ] With Notes open, tapping the profile icon opens the profile cards. The Case Folder icon also opens on top. All three types can be open at once.
- [ ] Touching any part of any open window (including text, chips or buttons on it) puts that window in front, including mixed combinations (Notes over a card, a card over the Case Folder, and so on).
- [ ] Each window flies out of its HUD icon when opened and back into it when closed. The cards come out one after another.
- [ ] Closing everything gives walking and looking back.

## Revision 7 — card style in the world, Case Folder anywhere, drag in/out (2026-10-16)

1. **Same card style in the world.** New `ProfileCardSkin` (applied from the shared `ProfileCardTextLayout.Apply`) covers the NPC-dropped profile papers and the corkboard side-quest cards (tray and pinned).
   - **How:** a thin world-space canvas in front of the paper face, sized from the paper mesh, using the same `ProfileCardFace` design as the HUD cards.
   - **Original text:** the 3D texts are hidden, not deleted.
   - **Clicks:** it takes no raycasts, so pickup and drag still use the physics colliders.
   - **Feedback tint:** the corkboard's green/red feedback tint is forwarded to the card's paper.
   - **Scope:** Office_Level1 and Play Mode only, so the scene is never modified while editing.
2. **The Case Folder icon did nothing outside the desk.**
   - **Cause:** the icon toggled the panel's `FloatingWindow` directly. That skipped `CaseFolderUI.Show()` (which builds the pages), and the never-activated panel's window had not initialised, so it silently refused to open.
   - **Fix:** new `CaseFolderUI.ShowAsWindow / HideAsWindow / ToggleFromHud`. They render the same pages, activate the panel first, then open with paper motion from the folder icon, and use the shared `HudWindowLock` instead of `LockPlayerControls`.
   - **Closing safely:** `Hide()` (e.g. swipe-to-close) delegates to the window close when the folder was opened from the icon. Dragging the window onto the icon also releases the hold (`FloatingWindow.Closed`).
   - **Desk flow:** `CaseFolderInteractable` / `CaseFolder3DDrag` still use `Show()`, unchanged.
3. **Desk view:** Notes, profile cards, the Case Folder and the Tax Code Book are all `FloatingWindow`s, so they open together and `HudWindowFocus` puts whichever is touched on top. At the desk no hold is taken (the player is already still).
4. **Drag in and out** (second open/close system; tapping still works):
   - **Case Folder:** dragging from the icon (only when the stored count is 1, as before) shows a closed manila folder instead of the old ghost icon (`HudDragItem`). Dropping it on the Auditor's tray submits; dropping anywhere else opens the folder window right there, growing out of the drop point; dropping it back on the icon cancels. Dragging the open folder window onto the icon closes it, shrinking into the icon.
   - **Notes:** drag a small Notes paper out of the icon and drop it to open (`NotesPanelUI.ShowAt`). Drag the Notes window onto the icon to close.
   - **Taxpayer profiles:** dragging out of the icon shows a stack of cards. Dropping it scatters the cards from the drop point to their places.
   - **Profile stack return:** press and hold any card still for 1.2 s (a ring fills after 0.3 s; moving more than 22 px cancels, so normal moving is unaffected). The cards gather into a stack under the finger. Drop the stack on the profile icon to put them all away, or anywhere else to scatter them back.
   - **Tap still works:** the Button click is suppressed automatically once a drag starts (`HudIconDragOpen`).
5. **Plumbing:**
   - `FloatingWindow` gained a `Closed` event, `SetPaperOpenOrigin` (grow out of a drop point) and a shrink-and-fade when dragged onto its icon.
   - `HudSubmittableIcon` gained optional `ClickOverride` / `DropOpenOverride` / `DragVisual`; dropping back on its own icon now cancels instead of opening.
   - `CollectedProfilesWorldPanel.GetProfilesToShow` is public.

**Verification:**
- Compile: 0 errors / 23 baseline warnings.
- HUD check: **702 PASS**. New guards cover the folder window's hold, the activate-before-open order, the Hide routing, the drag visual replacing the ghost, the drop outcomes, the count gate, the drag-out wiring for all three icons, the return-by-drag icons, the long hold (≥ 1 s) with move-cancel, stack put-away and scatter, the skin's scope and raycast safety, the tint forwarding and the card numbering.
- Controls / Dialogue / Progression PASS.
- Play Mode NOT RUN.

### Revision 7 checks (user)

- [ ] NPC-dropped profile papers and the corkboard cards (tray and pinned) look like the HUD profile cards, can still be collected and dragged, and the corkboard still shows its correct/wrong feedback.
- [ ] Outside the desk, tapping the Case Folder icon opens the folder (pages readable), flying out of the icon. Tapping again or dragging the window onto the icon puts it back.
- [ ] At the desk, Notes and profiles open too, and touching any of Notes, profile cards, the Case Folder or the Tax Code Book brings it to the front.
- [ ] Drag out of the Notes icon: a small Notes paper follows the finger and opens where dropped.
- [ ] Drag out of the profile icon: a card stack follows the finger, and on drop the cards scatter to their places. Hold a card still for about 1.2 s: a ring fills, then the cards stack under the finger. Drop the stack on the icon (all close) or elsewhere (they scatter back).
- [ ] With the folder stored (count 1), drag out of the Case Folder icon: a closed folder follows the finger. Drop it on the screen and the folder opens there; on the Auditor's tray it submits; on the icon it cancels.

## Revision 8 — subtle paper, readable cards, stacking, desk layering (2026-10-16)

1. **The paper backs were far too thick (agent bug).** `ProfileCardFace` passed `8 × scale` as the paper-stack unit (it should be `scale`), so the back sheets were 8× too far out, especially on the world and corkboard cards. That is fixed. Every paper (Notes, HUD cards, world cards, the dragged stack) now has the original subtle look: the front page, one page peeking underneath and a thin edge.
2. **Readable profile text:**
   - **World and corkboard cards:** the name and description are 1.6× larger, and the "Classification" footer is dropped, so the description fills the card.
   - **HUD cards:** the description is slightly larger (26 pt, 1.1× boost).
   - **Feedback colour:** the corkboard card stays cream, and its green/red feedback shows as a coloured frame. The old tint multiplied the card by the corkboard's neutral colour, which made the pinned cards pink.
3. **Holding a profile did not stack:** the hold now follows the live pointer (Input System) every frame while pressed, for the 1.2 s timer, the move-cancel and the release. The focus helper also starts the hold when a press lands on a card, so it no longer depends on card pointer events arriving.
4. **Desk layering for all four:**
   - **Why it didn't work:** at the desk, the open Case Folder and Tax Code Book are 3D objects (`OpenCaseFolder` / `OpenTaxCodeBook` world-space canvases), and screen UI always draws over the 3D world.
   - **Desk page touched:** the floating papers (Notes, profile cards, the HUD Case Folder) are tucked behind; they fade to a faint ghost, so the desk page is effectively on top.
   - **Paper or icon touched:** touching a ghosted paper, or any of the three HUD icons, brings them back and puts the touched one on top.
   - **Between the floating papers:** normal front-layering.
   - **Folder vs Tax Code Book:** the two desk documents are kept apart by the desk's slide-on-overlap placement.
5. **Front-layer counter fix:** when the shared sorting counter reached its cap, `FloatingWindow` reset it and returned without raising the touched window, so windows touched after that could stay below others. It now renumbers all windows (keeping their order) and raises the touched one.

**Verification:**
- Compile: 0 errors / 23 baseline warnings.
- HUD check: **715 PASS**, with new guards for the two-page paper, the 8× fix, the text boost and no footer on world cards, the frame feedback, the live-pointer stacking, the sorting reset and the desk tucking.
- Controls / Dialogue / Progression PASS.
- A card mock (HUD 540×330 and world 500×550) confirms the subtle backs and large, readable world text.
- Play Mode NOT RUN.

### Revision 8 checks (user)

- [ ] Notes, the profile cards, the plaza papers and the corkboard cards look like the earlier card you liked: one page underneath, no thick pile.
- [ ] The corkboard cards are cream (not pink), their names and descriptions are easy to read, and correct/wrong feedback shows as a green/red frame.
- [ ] Hold a profile card still for about 1.2 s: a ring fills, then the cards stack under your finger. Drop the stack on the profile icon (all close) or elsewhere (they scatter back).
- [ ] At the desk: touching the Case Folder or Tax Code Book page puts it in front (the floating papers fade back). Touching a faded paper or a HUD icon brings the papers back, with the touched one on top. Floating papers still layer among themselves.

## Revision 9 — real layering for all four, the real 3D Case Folder, corkboard cards (2026-10-16)

1. **Real front/back layering (no fading).** The user rejected revision 8's fading.
   - **New `Doc3DLayer`, one per 3D document (desk Case Folder, desk Tax Code Book):** when the document is in front of an open paper window it overlaps, a hidden camera (a copy of the main camera: same pose, FOV, no post, no AA, like the main camera) renders only that document into a transparent texture. The texture is shown as a screen layer at the document's place in the window order.
   - **Isolation:** the document is moved to private layer 31 only for that one render call and restored in a `finally`, so physics, clicks, outlines and desk placement never see it.
   - **One shared order:** papers and 3D documents share one front-to-back order (`FloatingWindow.TakeTopSortingOrder`; the renumber covers both).
   - **`HudWindowFocus`:** a press brings to the front whichever is in front at that point, either an open window under the finger or a 3D document actually hit there. A paper lying under a 3D document that is in front of it lets touches through to the document.
   - **Coming forward pops:** windows scale 1 → 1.045 → 1 (`FloatingWindow.PaperPop`, now also on direct touch); 3D documents pop their layer.
2. **Profile hold-to-stack did nothing.**
   - **Likely cause:** revision 8 polled the Input System for "still pressed". When that read fails (Device Simulator, simulated touch, pen), the hold was released on its first frame.
   - **Fix:** the hold now uses only the card's own pointer events and the press's live `PointerEventData` position (kept current by the UI input module).
   - **Console logs** mark the hold completing, the stack being put away and the stack scattering.
3. **Side-quest cards (plaza papers, corkboard tray and pins):**
   - **Shape:** the same landscape shape as the HUD cards (540:330), drawn 1.6× the old paper's width for readability.
   - **Layout:** compact, with a slimmer header, no paperclip and no footer, and 1.6× text.
   - **Old paper:** the square paper mesh is hidden (`forceRenderingOff`) and its box collider resized to the card, so grabbing matches the card.
4. **Corkboard result:** a thick frame (4.5% of the width) in muted green (88,135,92) when the card is in the right area and muted red (170,78,72) when not; no frame when unplaced. `ProfileDragCard.SetColor` now reports correct / wrong / neutral by comparing its own colours. The card itself stays cream.
5. **The Case Folder from the icon showed the old flat panel.** New `CaseFolder3DView` shows the REAL desk folder:
   - **Opening:** a tap or drop moves the actual `OpenCaseFolder` 3D model (with its pages, rendered via the world `CaseFolderUI.ShowAsWindow`) in front of the camera. It is posed exactly as seen from `DeskViewpoint` (the same lean), at the drop point if dragged, and flies out of the icon.
   - **Moving and closing:** grab it by its cover to move it; drop it on the icon (or tap the icon) to fly it back.
   - **Restore:** parent, pose, scale, visibility and the `FloatingModel3D` drag setting are restored exactly (new read-only `FloatingModel3D.IsDraggingEnabled`).
   - **Already open on the desk:** it is not moved; it is brought forward.
   - **Changing camera mode** (e.g. sitting down at the desk) restores it first.
   - **The old HUD `CaseFolderPanel`** is no longer opened by the icon.

**Verification:**
- Compile: 0 errors / 23 baseline warnings.
- HUD check: **730 PASS**. Superseded guards were updated, and new guards cover the private-layer render with guaranteed restore, rendering only when needed, the transparent no-post copy, the shared order, touch resolution and pass-through, the popping touch, the folder view's non-move / exact restore / desk framing / mode exit / pages, the card shape and collider, the result colours and the correct/wrong mapping.
- Controls / Dialogue / Progression PASS.
- Play Mode NOT RUN.

### Revision 9 checks (user)

- [ ] At the desk, with Notes and profile cards open over the Case Folder and Tax Code Book, touching either desk document brings it IN FRONT (fully opaque, small pop), and touching a paper brings that paper in front. Any order of the four works.
- [ ] The desk document copy lines up exactly with the real one (no offset or colour shift). Tell me if it shows a dark or opaque background instead of transparency.
- [ ] Hold a profile card still for about 1.2 s: the ring fills, the cards stack under your finger, and dropping them on the profile icon puts them away. If not, please send the `[ProfileCards]` Console lines.
- [ ] Plaza and corkboard cards have the HUD card's landscape shape with large readable text, and can still be collected and dragged.
- [ ] Corkboard: the right area shows a thick muted-green frame, the wrong area a thick muted-red frame.
- [ ] Case Folder icon (tap, or drag out and drop): the real 3D desk folder flies out and its pages are readable. Drop it on the icon (or tap the icon) to put it back. Sitting at the desk afterwards shows the folder in its normal place.
