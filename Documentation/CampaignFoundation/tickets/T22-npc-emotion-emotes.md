# T22 — Show NPC emotion through animated emotes

Status: Planned -> Implemented, internally verified; user manual pending (2026-10-24). Blocked by: None (extends the existing WorldEmote and the shared dialogue flow).

Amendment ticket added 2026-10-24; build order T22, then the [T08 Client Copy revision](T08-public-trust-and-follow-through.md), then [T23](T23-marie-consultation-visits.md).

References: [Campaign spec](../SPEC.md), [world and feedback](../WORLD_AND_FEEDBACK.md), [verification plan](../VERIFICATION.md), [progress log](../PROGRESS.md), [known issues](../KNOWN_ISSUES.md).

## Goal and visible result

NPCs, and sparingly Elijah, show emotion through small animated icons instead of facial animation, so conversations and world reactions feel alive. Before this ticket WorldEmote has four bubble kinds (Cheer, Shrug, Wait, Worry) used only by ConsequenceReactions for corkboard and plaza reactions, and dialogue lines carry no emotion. After it, one emotion system draws ten readable emotions as a bubble (most lines) or a free-floating burst of several icons (peak moments only, Minecraft-villager style), and an authored dialogue line can ask for one.

The player sees, for example: a music note swaying up over Marie as she greets Elijah, a "?" wobbling over a confused taxpayer, a light bulb popping over Elijah when he realises something, and, at a peak moment, several hearts rising and drifting apart over a thankful client. The emote appears when the line starts typing and leaves when the line ends. A line with no tag shows nothing.

## Ownership and mechanics

Extend Assets/Scripts/UI/HUD/WorldEmote.cs into the single emotion system. Do not build a second system. Keep WorldEmote.Show / ShowAt working; the old Kind values become aliases of the new set: Cheer -> Happy (sparkles), Shrug -> Confused ("?"), Wait -> Thinking ("..."), Worry -> Worried (tear). Update ConsequenceReactions and the corkboard/plaza callers to the new look; their timing (including CorkboardSideQuestManager.LockSeconds) is unchanged.

Emotion set; each has a distinct SHAPE so it reads for colour-blind players and on dim screens:

| Emotion | Icon | Idle motion |
|---|---|---|
| Thankful / affection | Heart | Pulse; as a burst, several hearts rise and drift apart |
| Happy / excited | Sparkles | Twinkle and pop |
| Surprise / alert | "!" | Quick pop and a small shake |
| Confused / asking | "?" | Slow side-to-side wobble |
| Thinking / waiting | "..." | Dots appear one by one |
| Idea / realization | Light bulb | Pop and glow pulse |
| Nervous / embarrassed | Sweat drop | Slides down beside the head |
| Worried / sad | Blue tear or small rain cloud | Slow drip |
| Playful annoyance | Anger puff | Two quick pulses (never hostile) |
| Cheerful / relaxed | Music note | Sways upward |

Put the catalogue (stable tag id, display shape, HudTheme colour, idle-motion kind, burst allowed, alias table, optional sound id) in a small pure class with no Unity scene calls, so the tests can check it. JSON tags are lower-case ids such as heart, sparkles, alert, question, thinking, idea, sweat, worried, annoyed, music.

Art: procedural or simple runtime sprites in the HUD style (HudTheme cream, brass, coral red, sky blue), flat and rounded with a soft outline and drop shadow. No downloaded art, no new packages. The bubble variant keeps the current WorldEmote bubble and tail.

Placement: above the character's head bone when an Animator exposes one, otherwise the renderer bounds' top (the current rule), offset slightly to the side so it never covers the face, the dialogue box or buttons; constant on-screen size; hidden off screen or behind the camera; screen-space overlay with raycastTarget off, so it never blocks touches.

Animation (unscaled time): entrance scale 0 -> 1.15 -> 1 in about 0.2 s; the per-emotion idle motion while visible; exit fade and drift up over about 0.25 s. Bursts are 5-8 icons, staggered, lasting about 1.2-1.6 s. Pool the bubbles and burst icons; no per-frame allocations.

Dialogue integration: add optional per-line "emote" (for example "emote": "heart") and "burst" (bool) fields to the dialogue JSON shapes read by DialogueBuilder / CaseDefinition / TownNpcDialogueLibrary, and matching optional fields on DialogueLine. Unknown values are ignored with one warning naming the tag. DialogueUI plays the emote when a line starts typing and clears it when that line ends or the conversation closes. NPC lines show it over the NPC; player lines show it over the conversation-only Elijah visual (ConversationActorVisual). It runs alongside ConversationMotion's nods and tilts and never replaces LineMood. Untagged lines show nothing: no automatic emotes from punctuation.

Rules: at most one emote per line; the same icon always means the same thing; always paired with text; Elijah uses emotes sparingly (mainly idea and alert on key lines), NPCs the full set. Reduced motion: no such setting exists today (SettingsManager stores only Music and SFX). Add a Reduced motion toggle only if it fits the settings board cleanly (keep the icon, drop the idle motion and bursts become a single still icon); otherwise record it in KNOWN_ISSUES (CF-45). Optional very soft per-emotion sound through AudioManager and the SFX setting; never plays when SFX is off; a missing clip means silence, not an error.

## Non-goals

No facial animation, blend shapes, new character models, portrait art changes, automatic emotion detection, chat-bubble text, a second reaction system, new packages or downloaded icons. No change to who speaks, line order, camera shots, DialogueAdvanceGate timing or ConversationMotion behaviour. Authoring emotes for every existing conversation is not required; tag only the lines this ticket and T08/T23 need, plus a small sample set.

## Acceptance

Internal checks — implementation agent records evidence in ../PROGRESS.md:

- [ ] Pure catalogue tests: every emotion has a distinct shape and colour pair, every tag id is unique, unknown tags are rejected, and the four old Kind values map to Happy, Confused, Thinking and Worried.
- [ ] Source contracts: DialogueUI plays an emote on line start only when the line is tagged, clears it on line end/close, and targets the NPC or the conversation Elijah visual by speaker.
- [ ] Source contracts: overlay canvas, raycastTarget off, pooled bubbles, unscaled time, SFX setting respected.
- [ ] JSON loaders accept the new optional fields and still load every existing dialogue; DialogueRegression and the other 11 suites pass; scratch build stays 0 errors, 29 warnings.

USER manual acceptance — only the user performs these checks:

- [ ] Each emotion's readability and motion on a phone, including a dim screen.
- [ ] Placement in shot and reverse shot: never over the face, the dialogue text or buttons.
- [ ] The heart burst feel at a peak moment.
- [ ] Corkboard and plaza reactions in the new look, with unchanged timing.

## Handoff

Record the tag list, the alias table, the reduced-motion decision and any soft sounds in ../PROGRESS.md. Next: the T08 Client Copy and handover revision, which uses the alert emote on the Auditor and the heart burst on Marie; then T23 tags Marie's consultation lines.
