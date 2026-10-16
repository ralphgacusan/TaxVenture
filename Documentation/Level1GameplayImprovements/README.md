# Level 1 Gameplay Improvements

The goal is to turn Level 1 from "fill the right boxes" into a small adventure: you learn by listening to people, the clock adds gentle pressure, and the world reacts to what you do. The plan was agreed with the user on 2026-10-16. Phases are built one at a time, and the user checks each phase in Play Mode before the next one starts.

User decisions: hints cost **10 EXP**; a missed deadline leads to a **next-morning retry**; office hours are **revealed by the receptionist** when the player reaches the office.

## Phases

| # | Phase | Status |
|---|---|---|
| 1 | Stories and indirect teaching | **Implemented (2026-10-16)**, awaiting Play Mode check |
| 2 | Office hours (in-game clock and the Auditor's deadline) | **Implemented (2026-10-16)**, awaiting Play Mode check |
| 3 | Consequences (reactions, real-world cost, no brute force, visible passing line) | **Implemented (2026-10-16)**, awaiting Play Mode check |
| 4 | Ask Arthur (hints for 10 EXP at the corkboard and desk) | Planned |
| 5 | Tutorial pass, polish, handoff | Planned |

### Phase 1: Stories and indirect teaching
- **Golden rule:** no text ever names a classification (resident, non-resident, citizen, alien). It tells the story and the player works it out. Tests enforce this.
- Every corkboard corner answers two hidden questions: **is the person Filipino?** and **do they live here most of the year?**
- **Plaza talks** are 6 lines each (down from 10-17). Each is a short life story holding the clues:
  - **Marie:** born here, passport never stamped, salaried BPO job. Her talk also hints at her office case: employee, earning from a job.
  - **Jhun-jhun:** Pinoy engineer on a Qatar contract, home a few weeks a year.
  - **Milo:** from "very far away", settled here six years with house and family, runs his own restaurant.
  - **Zorbix:** visitor from another planet on a 27-day paid project, flight home booked.
- **Profiles** keep the card length (12-13 words). Each one summarises what the person said, as clues only.
- **Arthur's corkboard intro** (8 lines) explains the two clues, then gives one indirect line per kind of taxpayer without naming any. His wrap-up (3 lines) repeats the two clues.
- **Placement feedback:** after every card, Arthur reacts in his bubble with his portrait:
  - **Correct:** why it fits.
  - **Wrong:** a hint aimed at the missed question (passport, home, or both), never the answer.

  If the guide is off, the board's own feedback line shows the same text.
- **Editable:** `Assets/Resources/Levels/Level_01/town.json` holds the talks, profile notes, reactions and Arthur's corkboard talks. It overrides the built-in copy at level load; invalid entries keep the built-in text and log a warning. The answer key (each profile's classification) stays in code.

### Phase 2: Office hours (implemented)
Settings: `Assets/Resources/Levels/Level_01/office_hours.json`.
- **The clock:** the day starts at 7:30 AM. Game time runs at a configurable rate (e.g. 1 real second = 10 game seconds). The clock is shown in the minimap header, and the elapsed timer stays where it is.
- **The deadline:** the receptionist reveals that the Auditor has an emergency and leaves at a set time. A countdown chip appears under the clock, turning amber with 2 minutes left and pulsing red for the last 30 seconds, with warning toasts.
- **Dynamic allowance:** the time allowed is clamped to about 4.5-8 real minutes and depends on how fast the player finished the side quest. All numbers live in `rewards.json`.
- **Missed deadline:** the Auditor leaves and Arthur reacts, then "Next morning, 8:00 AM" with case progress kept and a new, more generous deadline. Only the punctuality bonus is lost.
- **On time:** bonus EXP and an "On Time" stamp on the Audit Summary and Level Complete cards.

### Phase 3: Consequences (implemented)
Content: the consequence fields at the end of `Levels/Level_01/cases/case_001.json`.
- **People react:** Marie (office client) is happy or sad depending on the outcome and returns worried after a failed audit. Plaza NPCs show a cheer or shrug emote when their card is placed.
- **Real-world cost:** each audit mistake gets a one-line explanation from the case JSON.
- **No brute force:**
  - after a failed audit, the Auditor won't take the folder again until at least one answer has changed;
  - his lines get stricter with each try;
  - a corkboard card that has been wrong twice is locked for 3 seconds, with a hint from Arthur.
- **Visible passing line:** the progress card shows "Level 2 needs ★ 80%".

### Phase 4: Ask Arthur (planned)
- An **Ask Arthur** button at the corkboard and the desk only.
- The first hint is vague and the second is more specific; neither gives the answer.
- Each hint costs 10 EXP (set in `rewards.json`).
- A tutorial lesson introduces the button the first time it appears.

### Phase 5: Tutorial pass and polish (planned)
- guide.json lessons for all the new mechanics.
- Polish: a "First try!" stamp, sounds for the deadline warnings, the next-morning fade.
- Documentation, regression suites, and a Play Mode checklist.

## Rules for agents
Follow `Documentation/LevelGuideTutorial/AGENTS.md`, which covers the workflow, the checks and the CRLF/.meta rules. In addition:
- Keep the golden rule above.
- Keep `town.json` in sync with the built-in copy. `Tests/GuideRegression` enforces this, as `HudCheck` does for `objectives.json`.
- The user runs Play Mode and handles git.
