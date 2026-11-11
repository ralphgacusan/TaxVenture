# T02 — Chapter 2: An Ordinary File / A Familiar Name

**Status:** Implemented, internally verified; user manual pending (6 November 2026). **Standalone dependency:** T01 presentation reused unchanged. **Next:** T03 (not implemented here). **Production wiring:** T04 only. See [T02 evidence and preview instructions](../PROGRESS.md#t02--chapter-2-an-ordinary-file--a-familiar-name).

## Goal and observable result

Deliver **two standalone 15.000-second scenes**: a routine introduction to Marco and Bayview, and a relationship-aware Rosa ending that introduces CoastCare's recurring identity and Bea's next appointment. Reuse T01's presentation; no second subtitle/Skip implementation.

## Required context and boundaries

Read [production standard](../PRODUCTION_STANDARD.md), [chapter flow](../../CampaignStory/LEVEL_FLOW.md), [mystery](../../CampaignStory/MYSTERY_AND_EVIDENCE.md) and [Rosa choice history](../../CampaignStory/CHOICES_TRUST_AND_ENDINGS.md). For eventual gameplay edits read CampaignFoundation AGENTS and the current L2 continuation/T19, not older fixed dialogue.

Bayview's L2 stage identifies the company using L1 skills. Rosa's chapter teaches income recognition/reconciliation; the scene must not reveal the missing payment's answer. A CoastCare footer proves service identity, not sabotage. No New Horizon link, company villain shot, CT-07 explanation or manufactured crime warning here.

## Deliverables and ownership

| Scene | Stable ID | Assets |
|---|---|---|
| `Assets/Scenes/Cutscenes/CS_Chapter02_Start.unity` | chapter02.start | Dedicated authored shots/JSON/content under `Assets/Cutscenes/Chapter02` |
| `Assets/Scenes/Cutscenes/CS_Chapter02_End.unity` | chapter02.end | Same dedicated chapter folder, shared overlay from T01 |

Use current Elijah/receptionist/Rosa identities. Marco is male, careful/reserved: choose a distinct existing male model/rig, document the asset mapping, and keep it stable in future scenes. Do not reuse Arthur's recognizable identity and call it Marco. No new downloaded model or unique facial system.

## Opening — An Ordinary File

| Interval | Visual/action | Subtitle/audio |
|---|---|---|
| 0.00–4.80 | Fade into medium reception two-shot: Marco and Elijah, blue-gray Bayview folder between them. Marco settles an envelope onto the counter using a compatible simple action. Establish ordinary office geography. | No first cue required. Quiet room tone and one paper cue; give new character/file visual context before text. |
| 4.80–10.10 | Cut slightly closer to Marco/Elijah with the folder still in frame. Gentle move inward; Marco is matter-of-fact, not nervous or sinister. | 5.20–9.80 **Marco: “Bayview's first records. More will follow.”** |
| 10.10–15.00 | Cut to receptionist indicating the prepared workspace/file. A controlled pan or stable over-shoulder composition guides the next action. Fade 14.75–15.00. | 10.50–14.50 **Receptionist: “Start with the company details.”** |

Meaning: this is a real identified company and a partial staged packet. Do not hide essential background or auto-fill classification. Closing camera should orient the handoff but must not imply the scene itself performs the stage.

**Live-route prerequisite:** The Bayview classification stage must genuinely be receivable next. Author standalone now; T04 must not route this opening into Rosa-only gameplay while promising company details. Record that external dependency rather than creating Bayview mechanics inside this ticket.

## Ending — A Familiar Name

Trigger context for later wiring: Rosa's corrected review, actual delivered copy and service-preparer identification have occurred. The scene does not perform any of them.

| Interval | Visual/action | Subtitle/audio |
|---|---|---|
| 0.00–4.80 | Medium Rosa/Elijah shot in the conference room. Rosa gathers the already-delivered copy; restrained relief/reserve matches the selected line. | 0.60–4.60 Rosa line chosen from read-only context below. |
| 4.80–10.10 | Cut to a reviewed-paper insert. Brief readable footer **Prepared with CoastCare Tax Assistance**; Elijah/local scene prop settles beside the folder. No red highlight, accusation, zoom snap or reveal sting. | No spoken cue; light paper/room sound. This reinforces a previously established fact, not an automatic deduction. |
| 10.10–15.00 | Reception insert: simple appointment note **Beatriz Mendoza — Freelance Artist** beside a closed receipt envelope. Match desk lighting and leave a clean reading hold. | 10.50–14.50 **Receptionist: “Bea's next. She's bringing her receipts.”** |

### Read-only Rosa variants, same shot timing

| Committed history | Cue 0.60–4.60 |
|---|---|
| Responsible help and actual completed follow-through | **Rosa: “You checked it with me. Thank you.”** |
| Unsupported promise actually reversed/resolved through existing consequence | **Rosa: “Next time, please check before you promise.”** |
| Dismissive branch followed by corrected completed service | **Rosa: “Thank you for finishing the review.”** |
| Missing/legacy/isolated preview context | **Rosa: “Thank you for explaining the corrected copy.”** |

Never select responsible history merely because trust is high; use the actual saved choice/consequence context. Do not fabricate a commitment for older saves or read/write a real campaign during preview. The scene does not add/remove hearts, judge first-attempt accuracy or replay the penalty. One quiet alternative pose can accompany the line, not separate complete scenes.

Kanor's letter is not shown unless later separately designed as an actual owned-context prop; this ticket does not need it. Optional chest discovery is not a variant or ending requirement.

## Verification and acceptance

Internal: both authored script masters exactly 15 seconds; cues remain in bounds for all Rosa variants; references/rigs/bindings valid; shared overlay used; finish/Skip once-only; no save, penalty, handover or grade calls. Record both scene asset manifests and dependencies. Verify preview default does not inspect live save state.

Manual: check Marco identity/ordinary demeanor, visually readable company folder and CoastCare footer, clean camera geography, Rosa variant tone, no tiny appointment note, comfortable subtitle holds and safe-area Skip. Review no-sound story comprehension and early/mid/late Skip for both scenes.

## Acceptance checklist

- [x] Both exact scenes/IDs delivered with fixed 15-second presentation each.
- [x] Opening establishes staged company review without teaching/solving classification or revealing ownership.
- [x] Ending follows completed service and known preparer identity; Rosa variants reflect real history or safe neutral fallback.
- [x] First-view Skip and translucent-background subtitles meet shared standard; original intro unchanged.
- [x] Existing models/props/animations reused with only local cinematic additions; all bindings/metadata documented.
- [x] Internal evidence recorded, manual visual/device status separate.
- [x] Bayview live-route prerequisite explicitly handed to T04; no gameplay/build settings modified here.

**Acceptance boundary:** Checks above record authored content and internal verification, not rendered/device acceptance. User Unity import, Play Mode, animation/contact, sound-off readability, Skip/touch/focus and phone checks remain pending. These scenes have no live routes. The opening needs a real Bayview classification stage; the ending needs actual corrected delivery, preparer identification, and committed Rosa history. The implementation uses T01’s authored-shot player, not Timeline/Cinemachine.
