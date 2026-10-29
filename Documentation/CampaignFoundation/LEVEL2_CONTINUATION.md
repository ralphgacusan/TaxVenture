# Level 2 — A polished continuation of Elijah's work

User decision (2026-10-29): T20 comparison stand is deferred to future work, with its design retained in the T20 ticket. It is not required for Delivery B, T33 or T21. Continue T33 -> T21 after implemented T19. Keep ordinary free workstation comparison available; the unimplemented stand remains unavailable for purchase, including after T21 enables Level 2. T21 verifies that unavailable state rather than requiring stand purchase, placement or persistence.

Status: Implemented and internally verified through T33/T21 (2026-10-29); user manual acceptance pending. Level 2 campaign access is enabled; T20 remains deferred. See [Delivery B handoff](DELIVERY_B_HANDOFF.md). Original direction: 2026-10-28. This governs Delivery B alongside SPEC and supersedes older instructions to preserve literal Level 2 dialogue, tutorial and finale presentation unchanged. Preserve case facts and review rules while improving their delivery.

## Intended experience and user stories

Level 2 feels like the next working day in the same campaign. Elijah already knows how to move, handle a folder, use the Bag and consult the book. Reuse that core, then teach only the tools and decisions new to Rosa's investigation.

1. As a returning player, I want a brief welcome and clear next task, so that I can begin Rosa's work without repeating the first-day lesson.
2. As a returning player, I want my earned items, upgrades, decor, wallet, trust and mastery to continue, so that Level 1 still matters.
3. As an investigator, I want familiar physical folder controls and readable source/working papers, so that new evidence does not require relearning the interface.
4. As a visitor, I want a quieter town with two new optional people and believable routines, so that it feels inhabited without another four-client round.
5. As Rosa's consultant, I want her dialogue, expressions and gestures to reflect the actual case stage, so that her concern and resolution feel connected.
6. As a returning player, I want the correct task and conversation restored after interruption, so that introductions, supplies and rewards never repeat.
7. As a player without optional purchases, I want a complete free route through the investigation, so that equipment helps without becoming a requirement.
8. As a player with a full Bag or failed save, I want safe retry and retrievable records, so that no work or item disappears.

## Opening and storyline

Preserve the current entrance/reception spawn. A short receptionist welcome acknowledges continuing work and explains the existing cutoff once. Lead directly into Rosa's existing case retrieval. There is no initial Arthur talk, new Arthur Reviewed File acquisition/inspection task, four-ID collection, taxpayer-classification corkboard quest, repeated controls/Notes/shop lesson or compulsory purchase. Previously owned Arthur reference remains optional readable inventory; it never becomes Rosa's active case or a mandatory task. Arthur is not a required physical actor in this chapter.

Use the existing investigation order: retrieve/open case -> read summary and record the preliminary concern -> visit Rosa at her stall -> collect and inspect actual records -> annotation/toolkit/book and workstation comparison -> reviewed schedule/findings -> first frozen 12-decision check -> corrections and actual approval -> required client follow-through and ethics resolution -> Chapter 2 result -> Career. Inspect the actual phase machine before changing transitions; do not invent a source handover order or move T19's commitment past its valid decision seam.

Replace old Arthur-led teaching with brief contextual help and numbered book pointers through the existing guide observer, receptionist and Rosa. Teach only unfamiliar tools at their first relevant use, with reread access. A hint never supplies answers, awards progress or freezes independent reading time. Author short dialogue variants for arrival, repeat contact, records pending/received, review underway, justified hold, ethical commitment, correction, approval, follow-through and post-completion. Rosa shows concern, thought and relief without blame for ordinary mistakes. Preserve T19's exact choices, explicit confirmation and trust effects. Chapter 2 resolves Rosa's work rather than repeating Chapter 1's closing copy or launching unfinished Level 3.

## Quieter living town

Keep Rosa at `Environment/Outside/Plaza/RosaStallAnchor`, with the current desk, sign and interaction reach, until her records are handed over; then the same actor waits in the office conference room (consistency amendment below). No shop-building or pavilion relocation. Exclude the copied Level 1 quest crowd from this chapter's active roster. Add exactly two new optional ambient residents for this pass, with distinct names, roles, short conversations and bounded routines authored during implementation. They provide local character, not new cases, required clues, ID pickups, rewards or quest gates. Keep required receptionist, Auditor and functional service/shop actors. The outdoor focus is Rosa plus two newcomers, fewer people than Level 1.

Reuse Level 1's `NpcLife`, `NpcLocomotion`, `NpcAnimationDriver`, `NpcBodyPose`, compatible rigs/clips, emotes, markers and minimap conventions. Rosa gets grounded idle variation, smooth player-facing turns, restrained talk/listen and record-handover gestures, and phase-appropriate emotes while staying at her stall. Residents use simple idle/browse/short stroll or social routines, pausing for conversation and respecting collisions. One owner arbitrates movement, poses, dialogue and pickup; no T-pose, duplicate actors, foot sliding, visible teleport or ambient interruption of a handover. Record any missing rig/clip limitation honestly; static controller data is not visual acceptance.

## Items, shared systems and physical folders

T18 records an actual Level 1 -> Level 2 eligibility table for references/records, consumables, bookmarks, slot upgrades, decor/furniture and chapter-bound quest items. Carry the same campaign ownership, wallet, trust, XP/badges, settings and capacity across entry/resume; no clear/reseed or repeated acquisition cue. Used supplies stay used. Owned bookmarks retain applicable behavior. Eligible vouchers extend Rosa's review by the existing +180 active seconds and consume once. Shared shop ownership prevents duplicate purchases. Purchased decor restores at compatible authored office anchors without repurchase; unavailable anchors retain ownership with clear unplaced feedback. References stay readable; completed Level 1 task objects cannot become Rosa evidence, reactivate old tasks or earn rewards again. Full Bag and failed save leave required records safely retrievable without a mandatory upgrade purchase.

Bring Rosa's active folder, annotations and source/working papers to Level 1's typography, physical depth, page-turn quality, safe front focus/drag bounds, labels, Bag presentation and interrupted return/cleanup. Preserve all six D1–D6 source identities, values, evidence links, annotations and existing packet pages; do not force two pages. Read-only reference data cannot transfer answers to Rosa's editable work. Build only required Types 4–7 under the fixed document standard; Types 3/8 and unused variants remain deferred under T27.

Improve existing seams only where continuation needs it: chapter-aware start/guide/roster configuration, shared item eligibility and decor anchors, instance-bound paper presentation and phase-driven dialogue/restore. Future levels can reuse these contracts with different content, but this does not authorize a new campaign, AI, inventory, dialogue or document framework.

## Delivery, verification and boundaries

Execute [T18](tickets/T18-level2-campaign-and-inventory.md) -> [T32](tickets/T32-level2-town-and-rosa-animation.md) -> [T19](tickets/T19-rosa-choice-and-trust-consequences.md) -> [T20](tickets/T20-comparison-stand-and-shop-unlock.md) -> [T33](tickets/T33-level2-story-flow-and-polish.md) -> [T21](tickets/T21-delivery-b-verification.md), one ticket at a time. T32 depends on T18; T33 depends on T32/T19; T21 waits for all of them. T27 remains a design roadmap, not a gate. All new/revised scope is Planned, not implemented or accepted.

Internal verification covers actual start/scene bindings, absent Level 1 gates, item eligibility and save safety, six-record identity, NPC lifecycle/animation ownership, phase-sensitive dialogue, modal/clock behavior, free completion and chapter gating. T21 checks a real Level 1 -> Career -> Level 2 -> resume -> Chapter 2/Career route with and without optional purchases, then enables campaign entry only after integrated internal readiness. USER-only acceptance covers rendered animation, physical depth, navigation, touch, sound, pacing, device frame time and overall continuity. Existing Level 1 implementation/internal evidence and pending manual status remain unchanged.

## Out of scope

No extra client cases, new corkboard task, mandatory ambient errands, stall relocation, a second Rosa actor, all-eight advance build, new tax rules, changed economy/ethics balancing, whole-town rebuild, new AI framework/packages or future chapter/endings. Documentation changes do not start runtime work, reset actual progress, run Unity or perform Git operations.

## Consistency amendment (2026-10-29)

Level 2 consistency amendment (user request, 2026-10-29; implemented, internally verified, user manual pending; see PROGRESS): Rosa's folder opens on the Level 1 structure (page 1 editable Taxpayer Information, page 2 Supporting Documents with Level 1's physical records: her Client Background notes from the start, D1/D5/D3/D4 after the handover; no Taxpayer Profile page), followed by her working pages. After the record handover the same Rosa actor is placed in the office conference room (phase-based, never in view, no walking yet) for Level 1-style consultations; her stall, records desk and required stall conversation are unchanged. The Auditor hand-in is Level 1's talk-first awaited-folder conversation (no tray audit). Approval continues into Level 1's Client Copy flow and Rosa's physical delivery in the conference room, which calls CommitLevelTwoFollowThrough once. Later tickets build on this; they must not restore the profile page, the tray hand-in, a second Rosa or a second follow-through path.

T32 (2026-10-29, implemented, internally verified; user manual pending): Rosa now stays at her stall through the records handover and the toolkit tutorial and walks to the office when Mark the Money! begins (front desk wait, then the conference room; no route indicator), superseding the amendment's instant placement after the handover. The town is Rosa plus Mang Ernesto and Tess (Levels/Level_02/town_life.json, plaza_life.json).

T19 (2026-10-29): the explicit Rosa choice is inserted after all four records are marked, before the first complete CHECK. Rosa's existing owner handles it during travel or in the room. The unsupported promise requires separate confirmation; Talk later records nothing. All three paths still require corrected work, Auditor approval and the same Client Copy delivery. A neutral ruled review note records the words/status. Existing first-checked checkpoints are not retroactively penalized. T33 is next; campaign launch remains pending until T21. See PROGRESS for executed checks and manual limitations.

## CHECK-to-Rosa follow-up (user approved, 2026-10-29)

Supersedes the earlier departure-at-Challenge timing: Rosa stays at the stall until the explicit choice is committed. A complete first CHECK saves the draft/current record, parks the toolkit and opens her existing physical conversation without grading. Talk later/Back cancels without a choice; terminal callbacks return to the same desk/record after the dialogue camera restores. A separate CHECK tap grades. Unsupported reassurance still requires its separate confirmation. Then Rosa may walk to the office. Existing first-checked/past-field checkpoints retain their route with no retroactive choice; older pre-choice saved room/reception legs restore her at the stall under the load fade. Exit Desk remains available during independent work; only brief tool motions, teaching demos/splashes and result/collection ownership hide it.
