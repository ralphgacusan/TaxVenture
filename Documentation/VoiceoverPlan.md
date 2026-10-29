# TaxVenture — Future voiceover plan

Status: DEFERRED — future reference only, not implemented. User decision: 2026-10-28.

Voiceover is postponed until the user explicitly resumes it. This document preserves the initial proposal; it does not create a current ticket, block Level 2, change campaign dependencies, authorize paid generation or require recording now. No voice playback code, generated clips or new voice settings have been implemented by this planning work. Research was checked on 2026-10-28; recheck provider models, features, prices and licensing before resuming.

## Confirmed user choices

- Current English dialogue, with clear Filipino names and tax terms.
- Voice Elijah as well as the NPCs.
- Voiceover is now deferred. Continue the existing Level 2 schedule independently; resume this proposal only when requested.

## Intended approach

Generate voices online during development with ElevenLabs, then bundle approved audio clips into Unity for offline playback. This is prerecorded dialogue, not a live text-to-speech engine inside the game. Generate/import batches and match clips to dialogue lines automatically; the user does not need to send or attach every recording individually.

## Inspected project starting points (recheck when resumed)

- [DialogueLine](../Assets/Scripts/Data/DialogueLine.cs) contains text, speaker/name, camera hint, emote and awaited-action metadata, but no stable recording ID or clip reference.
- [DialogueSession](../Assets/Scripts/UI/DialogueSession.cs) snapshots lines on start and insertion; future voice metadata must survive both paths.
- [DialogueUI](../Assets/Scripts/UI/DialogueUI.cs) exposes line-change/end events and separates typewriter reveal from advance. It also supports action-wait lines and dynamically inserted responses. Playback must not bypass these rules or wait until terminal camera cleanup to stop a cancelled voice.
- [AudioManager](../Assets/Scripts/AudioManager/AudioManager.cs) has music/SFX settings and disabled legacy dialogue-voice methods. Extend the existing audio owner with a dedicated speech source/settings, rather than sending voice through overlapping one-shot effects.
- [Level2Say](../Assets/Scripts/Level2/Level2Say.cs) uses the separate guide-message path. Ordinary conversation, guide bubbles, notices and cutscenes need separate coverage decisions; one dialogue event does not automatically voice all text.

## Verified provider facts

ElevenLabs supports generation for a selected voice through [Text to Speech](https://elevenlabs.io/docs/api-reference/text-to-speech/convert). [Text to Dialogue](https://elevenlabs.io/docs/api-reference/text-to-dialogue/convert) accepts multi-speaker turns and returns an audio response; separate TTS clips are a better fit for individually skipped/branched game lines. This workflow recommendation is an engineering decision, not a provider limitation on possible editing.

Current official docs include v4 as well as v3; do not freeze the plan around v3 being the newest model. Expressive tags and model/voice settings affect delivery, but voice quality and pronunciation must be auditioned. See [generation guide](https://elevenlabs.io/docs/eleven-creative/playground/text-to-speech) and [pronunciation guidance](https://elevenlabs.io/docs/overview/capabilities/text-to-speech/best-practices). Specific Filipino names, acronyms and money values remain listening checks, not guaranteed output quality.

For release, [ElevenLabs licensing guidance](https://elevenlabs.io/docs/help-center/legal/can-i-publish-the-content-i-generate-on-the-platform) says free-plan output is not commercially licensed; eligible paid-plan output has commercial rights subject to terms, and Beta Services have additional restrictions. Record generation date, model/voice, settings and license context for approved clips. No subscription purchase or generation spend is authorized by this planning step.

## Proposed execution — only after resuming

1. Build a pilot script from actual current Level 1 dialogue: Elijah, Arthur and Marie, roughly six lines each. Cover neutral/warm/concerned/relieved delivery, names, tax terms and any real amounts/acronyms. Keep pronunciation-only spellings and acting tags separate from displayed text. Fix one character voice identity across moods and chapters.
2. Audition a small number of candidate voices in ElevenLabs' web interface; listen before bulk generation. Select model/voice/settings by the actual lines. Keep approved takes and source text. Use a paid eligible plan for anything intended as release audio; decide account access and a pilot credit cap before generation.
3. Integrate local playback into the existing dialogue lifecycle. Add explicit stable line IDs for voiced lines and a small ID-to-clip catalogue, with source-text revision/hash and language to detect stale recordings. Missing/unapproved/mismatched clips fall back to normal text. Cover snapshot and inserted-line paths, not only the basic builder.
4. Play one speech clip per committed visible line. Preserve manual advancement: reveal tap completes text while speech continues; actual next-line advance stops/replaces speech. Stop immediately on cancellation, scene exit and owner loss; pause/resume coherently on app interruption/settings. Voice endings never complete quests, action-waits, rewards or clock rules. Keep subtitles always available and use an independent persisted voice volume (zero mutes). Do not couple speech duration to typewriter character timing.
5. Keep existing talking gestures/emotes. Defer phoneme lip-sync, word timestamps, automatic advancement, document read-aloud, live synthesis and a new audio/dialogue framework. Account for guide bubbles separately later; avoid simultaneous guide speech over a foreground conversation.
6. Verify cancellation/rapid taps/repeated setup/insertion/missing clips, mute/settings, awaited actions, text revisions and save isolation internally. USER checks voice identity, pronunciation, emotional delivery, loudness against music/effects, Android offline playback, interruptions, package size and memory behavior. Keep approved source masters, choose Unity import/compression from a measured pilot, and load only the needed chapter audio rather than preloading all chapters.
7. Expand to remaining stable Level 1 NPC dialogue after pilot acceptance. Implement the shared playback foundation when this work is resumed, independently of current ticket numbering. Record Rosa and new Level 2 residents only after their revised dialogue is stable, using the as-built content if Level 2 is already complete. Voiceover does not gate T18, T33, T21 or campaign enablement under this deferred plan. Any future release requirement must be explicitly scheduled when resumed.

## Responsibilities when resumed

Implementation work: prepare/export dialogue, build batch-generation/import tooling where provider access permits, match clips to stable line IDs, integrate local Unity playback and run internal verification.

User participation: establish ElevenLabs account/access securely, choose character voices, approve the generation budget, listen to candidate takes and perform Unity/device/audio acceptance. Never put an API key in chat, source control or the shipped game. Start with the website for auditions; automate production batches only when access and spending are established.

## Open production decisions

Exact character voices/model, ElevenLabs account/plan and credit cap, release audio coverage (conversations versus guide/cutscenes), final import format and measured storage budget. Resolve these when work resumes; no audition script, audio generation or implementation is requested now. Do not assume today's free allowance or subscription price will apply later. No credentials belong in the Unity project or Android build; players use local clips with no runtime provider calls.
