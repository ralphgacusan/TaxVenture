# Chapter cutscenes — Levels 1–3 ticket pack

Created **6 November 2026**. Current implementation: **T01–T03 implemented, internally verified; user manual pending**. T04’s three ending routes are now implemented and internally verified; user manual acceptance is pending. The user subsequently requested normal Level 2/3 entry playback: both openings now run before the existing office gameplay on fresh Story entry and career-board Replay. All five authored scenes are registered. Mid-chapter Story checkpoints resume directly. Receiving Bayview gameplay remains deferred; this playback change does not implement it. See [PROGRESS](PROGRESS.md).

The user explicitly requested this granularity: one ticket per chapter, followed by one integration ticket. Level 1's existing intro already counts as its opening. Do not create or replace it.

Cutscene speech is now implemented using the 14 user-supplied recordings; Unity/device listening acceptance is pending. See [voiceover implementation evidence](PROGRESS.md#cutscene-voiceover--6-november-2026) and [recording script](VOICEOVER_LINES.md).

## Ticket order

| Ticket | Deliverable | Blocked by |
|---|---|---|
| [T01 — Level 1 ending](tickets/T01-level1-ending.md) | A First Case Closed; smallest reusable cinematic presentation needed to run it standalone | None for standalone authoring |
| [T02 — Level 2 opening and ending](tickets/T02-level2-opening-and-ending.md) | An Ordinary File; A Familiar Name | T01 presentation contract |
| [T03 — Level 3 opening and ending](tickets/T03-level3-opening-and-ending.md) | The File Returns; Plans Beyond the Paper | T01 and T02 contracts/content continuity |
| [T04 — Campaign wiring](tickets/T04-campaign-wiring-and-verification.md) | Real entry/closing hooks, save-safe return, skip/resume/mode handling and integrated verification | T01–T03; applicable narrative/gameplay prerequisites |

Read [PRODUCTION_STANDARD](PRODUCTION_STANDARD.md) and the selected ticket before work. The standard is part of every ticket's acceptance criteria. Detailed story context: [approved campaign](../CampaignStory/README.md), [level flow](../CampaignStory/LEVEL_FLOW.md), [characters](../CampaignStory/WORLD_AND_CHARACTERS.md), [mystery boundaries](../CampaignStory/MYSTERY_AND_EVIDENCE.md).

## Exact new scene names

| Stable ID | Scene asset | Human title |
|---|---|---|
| chapter01.end | `Assets/Scenes/Cutscenes/CS_Chapter01_End.unity` | A First Case Closed |
| chapter02.start | `Assets/Scenes/Cutscenes/CS_Chapter02_Start.unity` | An Ordinary File |
| chapter02.end | `Assets/Scenes/Cutscenes/CS_Chapter02_End.unity` | A Familiar Name |
| chapter03.start | `Assets/Scenes/Cutscenes/CS_Chapter03_Start.unity` | The File Returns |
| chapter03.end | `Assets/Scenes/Cutscenes/CS_Chapter03_End.unity` | Plans Beyond the Paper |

Existing opening stays `Assets/Scenes/Cutscene2.0.unity`; `Cutscene01Runtime.SceneName` currently names it. No CS_Chapter01_Start scene. End scenes return to their own chapter continuation/results, not directly into the next chapter.

## Asset ownership for parallel bug-fix work

- T01–T03 own new scene assets above and new dedicated `Assets/Cutscenes/Chapter01`, `Chapter02`, `Chapter03` folders. Put minimal shared presentation under `Assets/Cutscenes/Shared` and narrowly scoped scripts under `Assets/Scripts/Cutscenes/Chapters`.
- First ticket establishes the small shared contract; following tickets reuse it. This is sequential ownership, not three agents simultaneously editing shared presentation.
- Reference existing models, clips, fonts, materials and props. Use scene-local overrides/instances. Never apply changes to shared source prefabs, import settings, controllers, materials, gameplay scenes or the original intro during authoring.
- T04 alone owns necessary changes to actual route/checkpoint/result call sites and new-scene build registration. Review fresh source before integration because another agent may be fixing bugs.
- If a shared asset edit seems necessary, identify the exact need and isolate a dedicated copy inside the cinematic folder where practical. Do not silently overwrite another agent's work.
- User owns all Git operations and Unity visual/device acceptance under existing project conventions. No Git commands, Unity launches/automation, downloaded art or package changes are part of authoring these tickets.

## Important story dependency

Bayview's staged gameplay is approved narrative but remains deferred. **The later user request explicitly activates Chapter 2/3 openings before the existing Office_Level2/Office_Level3 flows.** This supersedes the earlier requirement to leave them inactive until Bayview gameplay exists. Their authored dialogue is unchanged. The handoff does not create company assignments, mark Bayview work complete, or replace the current Rosa/Bea gameplay. Direct cinematic Play remains a standalone preview; only a menu launch owns the office continuation.

The Level 2 CoastCare ending must follow a real preparer-identification beat, and Level 3 ending follows Bea delivery, finding and mayor-message acknowledgment. Follow current state, not a timer or guessed progression.

## Implementation evidence

When executing a ticket, create/update a bounded PROGRESS record in this folder with paths, chosen asset inventory, subtitle/shot timing, internal command results, remaining manual checks and next ticket. Keep status explicit: Planned → In progress → Implemented, internally verified; user manual pending → User accepted. Cinematic quality requires actual rendered review; a source check is not a visual pass.

No tracker publication or production implementation is authorized merely by these files. User chooses the execution ticket later.
