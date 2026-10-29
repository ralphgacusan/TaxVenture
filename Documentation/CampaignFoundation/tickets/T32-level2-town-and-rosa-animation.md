# T32 — A quieter Level 2 town and animated Rosa

Status: Implemented with a user-requested expanded cast; Unity manual acceptance pending. Depends on T18.

## Goal and visible accomplishment

Give Rosa's chapter the living-world quality of Level 1 without repeating its opening crowd or quests. Follow the [continuation amendment](../LEVEL2_CONTINUATION.md). The player meets an attentive, animated Rosa at her familiar stall, with a small plaza cast. The initial two optional residents are extended by user direction to include Arthur walking the plaza, Mang Dante reusing Level 1's ordered visit behavior, Marie in an optional pair chat, and a Rosa customer who arrives during the Market Visit and leaves when Elijah starts talking to Rosa. The office keeps required service actors with the same lowered-arm/talking-pose quality as Level 1, reusing existing compatible animation or static-arm support instead of leaving copied T-poses. All scene assembly, assets, bindings and deterministic placement are implementation work, not user setup.

## Work and bounded cast

Inspect `Level2OfficeStart`, `Levels/Level_02/office_start.json`, `Level2RosaStall`, the actual Office_Level2 scene and current actor construction. Preserve `Environment/Outside/Plaza/RosaStallAnchor`, desk, sign and reach. Keep copied Level 1 plaza clients, Arthur opening and corkboard quest inactive; new actors must not be parented under a hidden ancestor. Level 1's own scene remains intact.

The user-requested expanded outdoor cast is Ernesto and Tess, Arthur, Mang Dante, Marie, and Rosa's customer Lina, in addition to Rosa. Reuse the Level 1-compatible models, animation clips, `NpcLife` movement/social routines and interaction components. Arthur patrols a small loop; Dante visits available Level 2 plaza people in order; Marie chats with Ernesto; Lina walks to Rosa when the case enters Market Visit, waits with her, then returns to the plaza edge after a player conversation with Rosa. All cast members remain independently interactable. Keep placement clear of Rosa's records, the office entrance and required routes. Keep the copied Level 1 quest crowd inactive and do not copy the full Level 1 population. Short optional talks support the community and laundry story without revealing case answers or creating required clues, rewards, items, trust or progress.

Reuse `NpcLife`, `NpcLocomotion`, `NpcAnimationDriver`, `NpcBodyPose` and compatible existing rigs. Configure bounded idle/browse/short stroll or social activities with grounded feet, smooth turns, collision awareness and conversation pauses. Markers/minimap people follow shared availability conventions without becoming compulsory objective dots. One owner controls each actor's activity, movement and pose; dialogue takes priority over ambient behavior.

Rosa receives idle variation, player-facing turns, restrained speaking/listening gestures, record-handover motion and phase-appropriate emotes while remaining at the current stall. A pending pickup or conversation cannot let an idle routine drag records away or repeat acquisition. Reuse the existing effects setting and one-shot cues; animation completion never awards items or advances the case. Rig limitations need explicit safe non-T-pose fallback and a named remaining art issue, not an unsupported claim of finished animation.

## Failure and resume

Repeated setup/reload must produce one actor per stable identity with no duplicate runtime roots, mesh/pose owners or subscriptions. Restore case phase before talk availability; optional talks, arrivals and animation events are observers. Interrupted conversations/handover release ownership cleanly and leave the record retrievable once. Scene disposal cleans only owned instances. Missing route/clip data cannot block the investigation or use visible teleport as normal behavior.

## Internal checks — AI source/build/regressions only

- [ ] Static scene/runtime checks validate Rosa's preserved anchor, the expanded cast identities, active hierarchy and usable collider bindings; Level 1 quest actors/objectives stay inactive in Level 2.
- [ ] Lifecycle and interruption checks cover repeated setup, dialogue/handover ownership, phase restore and disposal without duplicate records, rewards or subscriptions.
- [ ] Compile and affected NPC/dialogue/Level 2 regressions pass or precise blockers are recorded; document actual assets and rig constraints. No static check proves rendered motion.

## USER manual acceptance

- [ ] Explore the quieter plaza; residents feel optional and do not obstruct Rosa, doors or required routes. Check Arthur's loop, Dante's visits, Marie's pair chat, and Lina's arrival/departure around Rosa's interaction.
- [ ] Talk, interrupt, collect records and resume. Inspect Rosa's idle, grounding, turn, gesture/emote and handover plus resident routines and conversation pauses.
- [ ] Check no T-pose, sliding, duplicates, visible teleport, clipping or misleading markers; verify phone reach, navigation, sound and device frame time.

## Non-goals and handoff

No new case, reward, population simulation, pathfinding framework, forced talks, stall relocation, a second Rosa or Level 1 scene rewrite (her phase-based conference-room placement already exists; walking between them is yours to add through the same owner, Level2RosaConsultation). Record final cast, routines, actual bindings and pending manual checks. Next in the chosen execution order: [T19](T19-rosa-choice-and-trust-consequences.md), then T20 and [T33](T33-level2-story-flow-and-polish.md). T33 coordinates story polish over this cast; T21 gates the integrated chapter.

## Consistency amendment (2026-10-29)

Level 2 consistency amendment (user request, 2026-10-29; implemented, internally verified, user manual pending; see PROGRESS): Rosa's folder opens on the Level 1 structure (page 1 editable Taxpayer Information, page 2 Supporting Documents with Level 1's physical records: her Client Background notes from the start, D1/D5/D3/D4 after the handover; no Taxpayer Profile page), followed by her working pages. After the record handover the same Rosa actor is placed in the office conference room (phase-based, never in view, no walking yet) for Level 1-style consultations; her stall, records desk and required stall conversation are unchanged. The Auditor hand-in is Level 1's talk-first awaited-folder conversation (no tray audit). Approval continues into Level 1's Client Copy flow and Rosa's physical delivery in the conference room, which calls CommitLevelTwoFollowThrough once. Later tickets build on this; they must not restore the profile page, the tray hand-in, a second Rosa or a second follow-through path. For T32: animate the one Rosa in both places through Level2RosaConsultation; replace the instant out-of-view placement with a walk only if it never shows a teleport or interrupts a conversation.
