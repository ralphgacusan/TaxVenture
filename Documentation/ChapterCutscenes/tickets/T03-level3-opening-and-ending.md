# T03 — Chapter 3: The File Returns / Plans Beyond the Paper

**Status:** Implemented, internally verified; user manual pending (6 November 2026). T01/T02 presentation reused unchanged. **Next:** T04, with the Bayview receiving-gameplay prerequisite still external. See [T03 evidence](../PROGRESS.md#t03--chapter-3-the-file-returns--plans-beyond-the-paper).

## Goal and observable result

Deliver two standalone **15.000-second scenes**. The opening makes the returning Bayview file recognizable and directs the player to apply income-verification skills. The ending gives Bea's studio ambition personal weight, reinforces careful findings and teases Nina's conflicting versions without launching Level 4.

## Context and current-source warning

Read [production standard](../PRODUCTION_STANDARD.md), [Level 3 and Level 4 story flow](../../CampaignStory/LEVEL_FLOW.md), [mystery boundaries](../../CampaignStory/MYSTERY_AND_EVIDENCE.md) and current Level3 AGENTS/README/dated amendments.

The active L3 implementation has advanced since older story text: delivery/closing and a shared conference-room finale/result flow have dated changes. The most recent inspected AGENTS also specifies Continue-only results, earned delivery-trust tiers and inventory removal of the delivered copy. This ticket must not restore obsolete STAY IN OFFICE routing, fixed +4 grants, an earlier physical-copy inventory bug, or an old independent finale. Cinematics are presentation, not permission to alter gameplay decisions.

The approved future L3 opening includes Bayview's income stage; do not claim it already exists. Bea is the only main client here. Nina/Marine Services is a future L4 appointment, not an activated second L3 case.

## Owned deliverables

| Scene | Stable ID | Assets |
|---|---|---|
| `Assets/Scenes/Cutscenes/CS_Chapter03_Start.unity` | chapter03.start | Authored shots/JSON/subtitle content under `Assets/Cutscenes/Chapter03` |
| `Assets/Scenes/Cutscenes/CS_Chapter03_End.unity` | chapter03.end | Same dedicated folder; T01 shared presentation |

Reuse current Elijah, Arthur, receptionist and Bea identities; no need to create Nina's full actor just to tease her appointment. Reuse Bayview folder identity/composition from earlier scenes and a small sketchbook/envelope/paper set using existing art/geometry.

## Opening — The File Returns

| Interval | Visual/action | Subtitle/audio |
|---|---|---|
| 0.00–4.80 | Match the earlier desk composition. Same Bayview folder, now with a slim new packet. Receptionist settles the records; titles suggest income summary/original entries without displaying solved differences. | No caption needed; room tone and one restrained placement cue. |
| 4.80–10.10 | Medium receptionist/Elijah shot oriented toward the file. Calm, matter-of-fact delivery. | 5.20–9.80 **Receptionist: “Bayview's income records arrived, with the original entries.”** |
| 10.10–15.00 | Over-shoulder Arthur/Elijah toward the workspace; a gentle approach settles before handoff. No amount highlight or arrow resolving the discrepancy. | 10.50–14.50 **Arthur: “Check whether the summary agrees.”** Fade out inside final 0.25 seconds. |

This is a reminder and task, not a second income tutorial. Do not state that the summary is definitely false before the player investigates. No ownership reveal, corporate funding conclusion, CT-07 explanation or ominous tone.

**Live-route prerequisite:** Gameplay after the opening must genuinely provide the Bayview income stage and retained prior work. Otherwise standalone authoring is valid but the opening route stays inactive in T04.

## Ending — Plans Beyond the Paper

Trigger context: Bea's reviewed computation, explanation and actual copy handover are saved; limited finding and Quax message have been acknowledged. Do not count a previewed/dropped copy as delivered. Scene-local paper held by Bea is a visual, not re-added inventory.

| Interval | Visual/action | Subtitle/audio |
|---|---|---|
| 0.00–4.80 | Medium Bea/Elijah shot in the familiar conference-room setting used for her actual finale. Bea holds her already-delivered computation copy beside a small studio/logo sketch. Gentle relief, not a giant celebration. | 0.60–4.60 **Bea: “Now I can plan for that studio.”** |
| 4.80–10.10 | Cut to receptionist/Elijah with the already-read message visible only as a simple prop. Slow, controlled camera shift; no new spoken mayor actor. | 5.20–9.80 **Receptionist: “The mayor wants supported findings, not guesses.”** |
| 10.10–15.00 | Reception insert: Marine Services appointment envelope with two visibly distinct draft tabs. Brief hold after camera settles. | 10.50–14.50 **Receptionist: “Nina sent two versions. She'll explain next time.”** |

Subtitle text is short enough for its cue; keep at most two lines and avoid long formal company titles in dialogue. Prop heading can read **Nina Cruz / Marine Services**. The two tabs foreshadow document versions, not two completed/required L3 corporate cases.

The studio is an aspiration, not a new owned building or purchase unlock granted by the scene. Mayor concern is limited; no claim of intentional sabotage or a town-wide pattern. The future appointment does not unlock or load Level 4.

## Branching

Use one shared ending. It must remain valid for corrected work and all actual earned delivery-trust tiers. Do not add a flawless-performance line, extra moral judgment, new trust tier or company guilt variant. Rosa history remains in gameplay unless a future explicitly approved cue needs it.

## Verification and acceptance

Internal: fixed script master and authored shots 15 seconds; cue/readability checks; stable references/rigs/metadata; no live checkpoint/Bag/trust changes; preview does not trigger L3 startup or L4. Check shared presentation and completion/Skip once-only behavior. Record current actor paths and source assumptions for T04.

Manual: compare return-file composition with L2, verify third-person cinematic view does not alter first-person gameplay rig, readable props/captions, Bea identity/relief, subtle studio sketch and restrained message tone. Inspect Skip/sound-off/focus/safe-area behavior in both scenes.

## Acceptance checklist

- [x] Both exact scenes/IDs and 15-second runtimes delivered.
- [x] Opening introduces the income task without discovering the answer; live-stage dependency recorded.
- [x] Ending acknowledges completed Bea service/finding/message without duplicating those actions or awards.
- [x] Nina is only a future appointment; no Level 4 launch, New Horizon link or unsupported accusation.
- [x] Subtitles/Skip/fades/audio/asset reuse meet shared production standard.
- [x] Latest L3 finale, earned trust, inventory and Continue-only behavior remain untouched.
- [x] Source checks/manifest recorded; manual visual acceptance clearly separated.
- [x] No gameplay/build-route changes; T04 receives read-only context and completion contracts.

**Acceptance boundary:** The checklist records complete authored scenes, internal checks and preserved scope. Unity import, rendered animation/grip/contact, real text readability, phone/safe-area/audio/touch acceptance remain pending. No route or gameplay prerequisite is fulfilled by these standalone scenes.
