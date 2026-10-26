# T15 — Establish Quax and show brief world demonstrations

Status: Planned. Blocked by: T06, T10.

Revision 2026-10-25 (REVERTED): at the user's request the whole T15 implementation was removed; the Level 1 guide stays Arthur and there is no practice board or demonstration seam. See PROGRESS 'T15 reverted'.

## Goal

Give the existing guide a consistent Quax identity and bounded demonstrations.

## Visible accomplishment

Before: guide identity and object demonstrations are not consistently part of the new route. After: the guide introduces himself as Mayor Quax, is identifiable as the same guide in the plaza and office, and demonstrates a few physical interactions the first time they matter. Each demonstration uses a nearby object and lets the player take over. Quax does not walk around autonomously, complete the task, choose an answer, or alter trust/economy.

## Work

Inspect the configured guide actor and its presentation before choosing a look. The project has no confirmed Quax-named asset: the existing configurable guide actor and currently available character may serve as an explicitly provisional visual while identity, name, dialogue and mannerisms change to Quax. Record that asset limitation; never describe an existing model as supplied Quax art or make placeholder appearance the final art acceptance. Do not begin a broad character replacement. Update first-time introduction and short authored demo copy to say “Mayor Quax” consistently while retaining their role as a guide who observes gameplay.

Choose bounded demonstrations that use this ticket's built prerequisites: present a fifth, clearly labeled practice profile on a separate sample board and demonstrate its Check action; show how to inspect a paper and put it away. Supply-counter introduction is composed in T16 once that counter exists; T15 provides the reusable presentation hook but does not depend on an unbuilt shop. Demo props are separate fixtures and never enter required taxpayer slots or grading. Each demo is a short, skippable, one-time choreography around the actor/object interaction, with a glance, point, open hand, or tap. Explain the practice example without revealing required taxpayer classifications, case income answers, final stamps or future evidence. A demonstration may clarify where to act, never act as the player.

### Source starting points

- **Existing:** current configured guide/town interaction code in `Assets/Scripts/SideQuest1/TutorialNpcController.cs`, `TownNpcDialogueLibrary.cs`, `CorkboardTeaching.cs`, existing dialogue/presentation pause flow, and Level 2's reusable physical paper/tool presentation.
- **Proposed:** a small authored demonstration-state/trigger seam tied to those existing interactions. No new dialogue framework, pathfinding controller, autonomous movement system or Quax art asset is assumed.

Revision 2026-10-25 (implementation notes, see [PROGRESS](../PROGRESS.md) and KNOWN_ISSUES CF-04, CF-92 to CF-99): the configured actor is `NPC_TutorialGuide` showing the unrigged `npc_tutorial_static1` mesh, which has no arm, so the 0.3 s raise becomes a forward lean plus a short trail of brass dots from his chest to the object (the camera-safe point this ticket allows); a gold MAYOR badge is pinned on his vest; the existing guide portraits stay as the provisional look. The practice board's areas ask one clue only ("lives here most of the year" or "away"), so a right practice check settles no real card. Both demonstrations happen at his plaza spot after the four profiles, before the corkboard talk (he is not visible earlier; T16 owns the first approach). There is no CHECK action to press since the T06 revision: the practice board checks itself, like the real one. The Side Quest drill does not reuse the demonstrations, because its fixed lesson chain would depend on where the board can stand.

Use the current model's simplest supported idle/gesture motion. For a demonstration, Quax turns head/body toward the named object over 0.25 s, raises one arm for 0.3 s, holds 0.5 s, and lowers it over 0.25 s; the highlighted object performs its own short physical nudge/flip. Restore the actor's original pose and release modal/player-control ownership on skip, exit, scene unload or failure. An existing friendly-select one-shot may play on the object highlight; route it through the effects-volume setting, at most once per demo, never as a loop. No facial expression or body motion may be claimed until the target manual visual check confirms it renders on the chosen actor.

## Persistence and failure rules

Identity text and actor styling do not grant anything. First-time demonstration flags may be recorded only after their trigger begins and must not unlock a reward, purchase, story transition or check result. Reentry after an interrupted demo offers the same demo again or the player's already-earned skip route; it cannot raise gameplay events. The actor is a presenter and reads snapshots/results only. Collection, check, purchase, trust and progression stay with their existing player/gameplay commands. If the current actor cannot perform the specified pose, use a brief camera-safe point or authored text callout with the same object focus, and keep visual limitations documented.

## Internal checks — AI source/build/regressions only

- [ ] Authored guide dialogue and triggers use Quax identity; source inspection finds no demo path that submits a profile, spends, awards, or changes trust.
- [ ] State restoration and skip/disable/unload cases release controls and do not duplicate interactions; build and relevant guide/dialogue regressions pass.
- [ ] Static scene inspection identifies the actual configured actor/model and records its provisional status and demo-object anchors.

## USER manual acceptance — Unity/device/visual/feel/listen checks

- [ ] Play the first meeting and each demo; confirm the same named guide is visible and players can take over immediately.
- [ ] Skip/reenter each demo, rotate/approach the object and verify no blocking control or surprise answer hint appears.
- [ ] Check actor pose, object focus, text, animation readability, touch response and cue volume/quality in Unity and on device. No scene construction is delegated to the user.

## Handoff

Next ticket: [T16](T16-level1-onboarding-and-wrap-up.md) connects the authored guide and objects into the full Level 1 route.

## Non-goals and references

No supplied Quax asset claim, character overhaul, new dialogue/pathfinding framework, autonomous task completion, new answer reveal, or trust award. See [SPEC](../SPEC.md), [WORLD_AND_FEEDBACK](../WORLD_AND_FEEDBACK.md), [VERIFICATION](../VERIFICATION.md), and [PROGRESS](../PROGRESS.md).
