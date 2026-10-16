# Phase 3 — Contextual notifications + reward fly-to FX

Status: **Implemented — awaiting user Play Mode check**

## Goal

Make progress *felt*: small, restrained toasts and rewards that travel into the HUD element they update.

## Scope

- `HudToastQueue`: up to 2 visible toasts below the objective tracker. Each has an icon, a title and a value (`+85 EXP`, `Objective complete`, `Profile collected 2/4`, `Side quest complete`). They slide and fade, merge duplicates, and wait while the HUD is hidden.
- `HudRewardFlyFX`: a pooled UI particle burst (no allocations after warm-up).
  - **Reputation hearts:** when the case-result popup closes (the HUD becomes visible again), hearts spawn at the screen centre (where the popup was), arc to the card's heart row, and each landing fills one heart with a pop.
  - **EXP sparks:** gold sparks fly to the EXP bar, and the bar fill starts on arrival.
  - Profile pickup: a small card icon flies from the pickup's screen point to the profile HUD icon (the existing `TryGetCollectionScreenPoint`).
- Events: `CaseProgressionManager.OnCaseRewardStored`, `GameStateMachine.OnStateChanged` (task completion), `TownEvents.OnProfileCollected / OnSideQuestCompleted`.
- An achievement SFX only for side-quest/level milestones (the popups already play it for case results; no double audio).

## Explicit non-goal

No hearts for ordinary NPC conversations: reputation is not earned there (see README §1).

## Acceptance

- Every toast or particle corresponds to a real event and value; the totals after the FX equal the accumulator.
- The FX never blocks input (`raycastTarget = false`) and never plays while the HUD is hidden.

## What was built (2026-10-16)

- **`HudToastQueue`** (inside the HUD's fading root, under the objective tracker):
  - **Look:** a navy/brass card with a coloured badge icon (green check = objective, brass check = main objective, gold star = EXP, coral heart = reputation, blue card = profile), a caption and a value.
  - **Capacity:** at most two visible; more wait in a queue.
  - **Motion:** a toast slides in (0.25 s), holds for 2.4 s (3.2 s for a main objective), then lifts out (0.3 s); the second moves up.
  - **Merging:** same-kind notices merge (e.g. two EXP gains become one "+N EXP").
  - **Hidden HUD:** timers only run while the HUD is shown, so notices raised during dialogue or popups are seen afterwards.
  - **Touches:** never blocked.
- **`HudRewardFlyFX`** (its own overlay canvas):
  - Pooled hearts and stars burst out, then fly along a curved path into their target, shrinking as they go.
  - Each landing bumps the target, and a pulse helper bumps icons.
  - No allocations after warm-up, and idle when nothing is flying.
- **`HudFeedback`**, which turns real game events into notices:
  - **Case reward** (`OnCaseRewardStored`): "+EXP" and "+Reputation" toasts with the stored values. Once the HUD is visible again (the result popup closed), hearts (one per reputation point, max 8) fly into the reputation bar and stars (EXP/12, 3–10) into the EXP bar. The progression card holds its fill (new `HoldFillFor`) until they land.
  - **Objectives** (`OfficeObjectives.Changed`): "Objective complete" for each newly finished step, and "Main objective complete" for a finished main objective. Nothing is announced for steps already done at load.
  - **Profile collected** (`TownEvents.OnProfileCollected`): "Profile collected — Name (n/4)". The profile icon pulses as the paper (which already flies itself) arrives.
- **`HudTheme.Star`:** a procedural star sprite.
- **Not added (by design):** hearts for ordinary NPC conversations, because talking does not earn reputation in the game (K4). No extra sounds; the existing popups already play the achievement sound.

## Verification

- Compile: 0 errors / 23 baseline warnings.
- HUD check: **746 PASS**. New guards cover notices only from real events with real values, no conversation hearts, no announcements at load, the bars waiting for the effect, toasts waiting while hidden, max 2 + queue, merging, no touch blocking, pooled and idle effects, scale-restoring pulses, the star sprite and installation.
- Controls / Dialogue / Progression PASS.
- Mock-up: toasts sit under the tracker without crowding it.
- Play Mode NOT RUN.

## Manual check (Unity)

1. Collect a plaza profile: a "Profile collected — Name (n/4)" toast appears under the tracker, and the profile icon bumps as the paper flies in.
2. Finish each objective step: an "Objective complete" toast (the tracker's own check animation still plays). After the corkboard: "Main objective complete — Finish Side Quest 1".
3. Finish the case: "+EXP" and "+Reputation" toasts appear after the result popup closes, then hearts and stars fly into the bars and the bars fill as they land. The values match the popup.
4. During dialogue no toast or effect shows; they appear when it ends.

## Polish + reward system (2026-10-16)

Asked for: per-level maximum and passing score, deductions, conversation hearts, the side-quest badge and hearts, the case reward, saving with pass-to-unlock, and Side Quest 1 before the office. Rules and numbers: `../REWARDS.md`.

What changed in the HUD:
- The **progression card** shows the whole-level score (Level 1: 150 EXP / 12 hearts), not only the case. Each bar has a slim **passing tick** at 80% that turns green once reached. Earned **badges** appear as brass medallions on the name row and pulse when added.
- **New notices:** "+1 Heart — Client conversation"; "Side quest reward" (+EXP, +hearts); "Badge earned — Know Your Taxpayer"; "Wrong placements −10 EXP −1 heart"; "Auditor found 2 mistakes — Case reward −40 EXP −3 hearts"; "Locked — Finish Side Quest 1 first"; "Passing score reached".
- **Toast polish:** a coloured accent edge per kind, a small settle pop on arrival (and on a merge), values that auto-size instead of clipping, and longer holds for badges and passing.
- **Hearts fly from the source:** the conversation heart from that NPC, the side-quest hearts and stars from the guide, case rewards from the screen centre.

### Manual check (Unity)

1. Talk to a plaza client. After the talk: "+1 Heart" toast and a heart floats from that client into the hearts bar (x / 12). Talking to the same client again gives nothing.
2. Before finishing the corkboard, walk into the reception: "Side Quest 1 first" notice once. Click the receptionist, the desk or the Case Folder icon: "Locked — Finish Side Quest 1 first", nothing opens.
3. Finish the corkboard with some wrong placements. After the guide's talk: the Side Quest Complete panel shows EXP x / 50, hearts x / 3, the badge and the wrong-placement count; toasts for the reward, the badge and the deduction; hearts and stars fly from the guide; a medallion appears on the card.
4. Submit the case with a mistake: "Auditor found n mistakes — Case reward −…" notice. Fix and resubmit: the case reward is reduced accordingly (e.g. 1 mistake → 75 EXP, 3 hearts).
5. When the totals cross 80% of both bars, "Passing score reached" shows and both ticks turn green.
6. Level Complete shows EXP x / 150 (%), hearts x / 12 (%), PASSED or "Below passing… replay", and the badge. From the main menu: a passing run unlocks Level 2; a run below passing does not (an earlier pass is never lost).
7. Change `passingPercent` in `rewards.json` (e.g. 100) and replay: the ticks move and the pass result follows.
