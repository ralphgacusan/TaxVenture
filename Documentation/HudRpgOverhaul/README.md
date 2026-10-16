# Office Level 1 HUD — RPG UI/UX Overhaul

Upgrade the Office_Level1 in-game HUD from plain text counters into a cohesive, RPG-style interface. It shows only progression that the game actually tracks. This initiative is separate from `Documentation/WorldAnimationAndInteractionOverhaul` and `Documentation/ImmersiveInteractionOverhaul`; do not edit those folders as part of it.

- Scene in scope: `Assets/Scenes/Office_Level1.unity` only.
- Workflow: the agent implements one phase at a time and runs internal checks (scratch compile + regression suites). **The user does all Play Mode / device checks and all git actions.** A phase is complete only after the user confirms it in Unity.

## 1. Audit — what actually exists (2026-10-15)

### HUD today (`HUD_Canvas`, Screen Space Overlay, CanvasScaler 1920×1080, match 0.5)

| Element | Where | Today | Problem |
|---|---|---|---|
| `PermanentHUD/Level` (`HudStateLabel`) | top-left | Plain "Level 1" text, 36 pt, white | Prototype look; no progression shown |
| `DebugStateLabel_Text` | top-left, below Level | "State: ReceiveCaseState" | Debug output visible to players |
| `PermanentHUD/Timer` (`HudTimer`) | top-center | Plain "00:00:00" | No panel; allocates a string every frame |
| `PermanentHUD/IconButtons` | right column | Folder / Notes / Profiles icons with black 36 pt counters | Counters are raw text |
| Office minimap (Ticket 06, runtime-built) | top-right | Navy + brass panel | Already sets the visual language this HUD reuses |
| `PromptPanel` (`InteractionPromptUI`) | bottom-center | Grey box, "Click to open Computer" | Generic; not mobile-friendly wording |
| `NotesPanel` (`NotesPanelUI`) | floating window | `[DONE]` / `[ ]` text list of tasks | The only objective view; hidden until opened |
| `CaseResultPopupUI`, `SideQuestCompleteUI`, `LevelCompleteUI` | modal | "EXP Earned: 85" style text lines | Rewards shown as plain text |

### Progression data that is real (and may be displayed)

| Data | Source | Notes |
|---|---|---|
| Case EXP (0–100) and Reputation (0–5) | `RewardsCalculator.BuildResult` → `CaseProgressionManager.CalculateAndStoreCaseReward` (called from `ClientInteractable` / `AuditorInteractable`) | Calculated once per case and stored in `LevelRewardAccumulator`. |
| Level EXP / Reputation running total | `CaseProgressionManager.RewardAccumulator` | Session-only until `LevelCompleteUI` applies it. |
| Maximum possible per level | `TotalCasesInLevel × MaxExp(100)` / `× MaxReputation(5)` | Level_01 has **1 case** → 100 EXP, 5 reputation. |
| Outcome tier | `LevelResultData.Branch` (`CorrectNoIssues` = flawless) | Supports a "Flawless" badge without new rules. |
| Career totals | `PlayerProgressManager.CurrentProgress.totalExp / totalReputation / levelsCompleted` | Only present if the run started from the main menu (the manager is `DontDestroyOnLoad`). |
| Case objectives | `TaskListProvider` (11 tasks keyed by FSM state) + `NotesPanelUI` completion set | The game's real objective list. |
| Case number | `CaseProgressionManager.CurrentCaseNumber / TotalCasesInLevel` | |
| Side quest | `TaxpayerProfileCollectionHUD` (n/4 profiles), `TownEvents`, `CorkboardSideQuestManager.BuildResult()` | Rewards are shown in a popup only (see limitations). |
| Elapsed time | `HudTimer` (display) / `LevelTimer` (results) | |

### What does NOT exist (will not be faked)

- **No player "level"/rank system.** EXP is a running total with no level curve. The HUD will not invent levels. The EXP bar measures *this office level's EXP earned out of the maximum available*, which is a true and useful goal ("how much of the 100 EXP have I secured?").
- **No achievements/badges system.** The only badge-worthy facts are the outcome tier (flawless case), side-quest completion and level completion. They are shown at the moments they happen.
- **Talking to NPCs does not earn reputation.** Reputation hearts only come from case results. The heart particles you asked for are therefore tied to the real reputation award: when the case result closes, hearts fly from the result into the HUD heart tracker. They do not appear on ordinary conversations, because that would misrepresent the game state.
- *(Updated 2026-10-16: at your request the level reward system now adds conversation hearts, the side-quest reward and badge, and saves the level result; see REWARDS.md. The audit notes below describe the game before that change.)*
- **Side-quest EXP/Reputation are never added to the accumulator or saved.** `SideQuestCompleteUI` shows them, but nothing applies them. That is a gameplay/progression gap, not a UI one. It is logged in `KNOWN_ISSUES.md` and needs your decision; this UI work does not change it.

## 2. Design direction

One visual language, taken from the existing minimap so that the whole HUD reads as one system:

- **Panels:** deep navy `#121C29` at about 85% opacity, a 2 px brass edge `#CAA965`, procedurally generated rounded corners (radius 10) and a soft drop shadow.
- **Text:** cream `#F5F0E2` for values, slate `#A8B4C0` for captions, and brass for headings, using small caps with letter spacing.
- **Meaning colours:** EXP gold `#F1C26C`, reputation coral `#EC5D62`, complete green `#6EC98C`, active cyan `#64D7FF` (the minimap "You" colour), and locked/empty slate `#3C4856`.
- **Motion:** 0.2–0.6 s ease-out, using unscaled time. Nothing blocks raycasts. Nothing ticks when idle.
- **Respect the view:** HUD clusters stay in the corners and top edge. They fade out during dialogue, modals, settings, results and pause, which is the same rule the minimap uses.

## 3. Phases

| # | Phase | What you will see | Status |
|---:|---|---|---|
| 1 | [HUD foundation + progression card](phases/PHASE-01-FOUNDATION-PROGRESSION.md) | Top-left card: level badge, level name, animated EXP bar and reputation bar (earned / max), career line. Debug and "Level 1" text removed. | Done (user-verified 2026-10-15) |
| 2 | [Objective tracker](phases/PHASE-02-OBJECTIVE-TRACKER.md) | Top-center, one step at a time: the current main objective, the step with a live counter, grouped step segments and the clock. Revision 2: editable per-level objectives (Side Quest 1 → Tax Case), a paper Notes page with checkmarks that holds the player still, and the clipped-value fix. | Rev 2 implemented, awaiting your Unity check |
| 3 | [Notifications + reward fly-to FX](phases/PHASE-03-NOTIFICATIONS-FX.md) | Toasts for objective/EXP/heart/badge/deduction/locked/passing events. Hearts fly from the NPC into the card, then the bars fill. Includes the level reward system ([REWARDS.md](REWARDS.md)): per-level max and passing line, pass-to-unlock, Side Quest 1 first. | Implemented + polished, awaiting your Unity check |
| 4 | [Interaction prompts + HUD icon badges](phases/PHASE-04-PROMPTS-ICONS.md) | Compact verb-chip prompt (with a tap/click glyph) and pill badges replacing the black counters | Implemented, awaiting your Unity check |
| 5 | [Reward screens + final layout pass](phases/PHASE-05-REWARD-SCREENS.md) | Case result, side-quest and level-complete screens get EXP bars, hearts and a Flawless badge. The pass also checks aspect ratios, safe areas and overlaps. | Implemented, awaiting your Unity check |

Each phase can be checked on its own in Play Mode, and each later phase builds on the earlier ones.

## 4. Implementation approach

- **Runtime-built UI** (the same pattern as Ticket 06's minimap). `OfficeHudRuntime` bootstraps itself for Office_Level1 and builds into the existing `HUD_Canvas`. The 4.5 MB scene and your in-progress scene edits are not rewritten. Legacy objects are hidden at runtime, never deleted, so all serialized references survive.
- **Presentation only.** Gameplay classes get read-only accessors and one notification event at most (for example `CaseProgressionManager.OnCaseRewardStored`). Reward maths, FSM transitions and save data are unchanged.
- **Event driven.** The HUD subscribes to `GameStateMachine.OnStateChanged`, `CaseProgressionManager.OnCaseRewardStored`, `TownEvents` and `GameplayEvents`. Per-frame work runs only while a tween is active.

## 5. Files

See `PROGRESS.md` for the modified-file list and verification log of each phase.
