# T01 — Chapter 1: A First Case Closed

**Status:** Implemented, internally verified; user manual pending (6 Nov 2026, see [PROGRESS](../PROGRESS.md)). **Blocked by:** None for standalone authoring. **Next:** T02. **Production wiring:** T04 only.

## Goal and observable result

Deliver one polished, standalone **15.000-second ending scene**, plus only the small reusable presentation layer needed to play it with timed subtitles and upper-right Skip. The existing Level 1 intro remains unchanged and continues to count as the chapter opening.

After Marie's actual gameplay handover, the player should feel that one person has been helped and that Elijah now has another ordinary professional responsibility. Bayview is a quiet next-case tease, not an ominous reveal.

## Required context

Read [production standard](../PRODUCTION_STANDARD.md), [story characters](../../CampaignStory/WORLD_AND_CHARACTERS.md) and [Level 1 flow](../../CampaignStory/LEVEL_FLOW.md). Inspect `Cutscene2.0`, `Cutscene01Runtime`, current Marie/Arthur/Elijah appearances, existing conference room/reception props and the applicable intro instructions before considering any reference change.

Current first-client route: actual Marie copy delivery and saved follow-through precede the post-handover result flow. Do not replay the handover or claim official tax payment in dialogue. Relevant integration references for T04 include `ClientCopyHandover`, `CaseFinale`, `LevelCompleteUI.ShowAfterHandover`, `PlayerProgressManager.CommitLevelOneWrapUp`. Verify names/behavior against current source.

## Owned deliverables

- Scene: `Assets/Scenes/Cutscenes/CS_Chapter01_End.unity`.
- Stable ID: `chapter01.end`; editorial title: **A First Case Closed**.
- Dedicated authored Timeline/curves/subtitle data under `Assets/Cutscenes/Chapter01`.
- Minimal shared cinematic overlay/player under the ownership areas in [README](../README.md), with stable return outcome and no gameplay mutations.
- Unique Unity metadata for new assets, inspectable bindings and an asset manifest with source model/clip paths.
- Local progress/known limitations and exact manual review steps.

Do not create a new opening, shorten the original, modify its subtitles/Skip policy, add the new scene to campaign routes/build settings yet, or change a shared actor/prefab/Animator Controller.

## 15-second storyboard

| Interval | Shot, action and composition | Subtitle/audio |
|---|---|---|
| 0.00–4.80 | Fade into a medium Marie/Elijah composition in the familiar conference room. Marie already holds her delivered copy. A restrained relaxed posture and glance at the paper communicates relief. Gentle short push, enough space below for subtitles. | 0.60–4.60 **Marie: “Thank you. I understand it now.”** Quiet room tone. No simulated delivery event. |
| 4.80–10.10 | Intentional cut to Arthur beside Elijah, angled toward Marie leaving the conversation. Arthur gives a subtle nod/turn; Elijah's response is attentive rather than a large celebration. | 5.20–9.80 **Arthur: “Good work. Another file is waiting.”** |
| 10.10–15.00 | Cut to reception/desk insert. A receptionist/local hand-compatible pose places or settles one plain blue-gray folder; title **Bayview Property Services** is readable. Settle the camera and hold the prop before fade at 14.75. | Silent story beat; one quiet paper-placement cue. No suspense sting, ownership logo or unsupported assignment completion. |

The folder is a future appointment, not a collectible acquired by this scene. Its placement need not show detailed fingers: frame a clean surface contact using compatible assets and a cut, without levitation or clipping. Keep the same Bayview folder look for T02/T03.

## Cinematic implementation notes

Reuse actual Marie and Arthur identities and the office design. Use a limited set copy/instances that do not bootstrap gameplay, rewards or client AI. If a walking departure is not clean, keep Marie in the establishing shot and use Arthur's angle to imply the exchange has ended; no need for a new walk rig.

Build the shared overlay with TMP, two-line white text on translucent dark background, no typewriter, and the same themed Skip button as the intro. For this new scene Skip is available on first viewing. Start/end fades are inside 15 seconds. The presentation outcome is natural/skipped/aborted, not a save or scene destination.

A standalone preview completes in a safe finished state; it cannot call the old runtime's Finish, write `levelOneIntroSeen`, grant trust, create a copy, mark chapter complete or load Office_Level1. Gameplay continuation is supplied later by T04.

## Internal verification

- Inspect master duration and all tracks/subtitle/audio intervals; no tail extends beyond 15 seconds.
- Compile relevant new source against the installed project/harness; verify once-only natural/Skip outcome and cleanup.
- Check existing-model/clip/material/font references and unique metadata; absent second intro Timeline is not a dependency.
- Check preview does not invoke case/reward/save/route commands.

## User visual acceptance

1. Play standalone: three distinct compositions clearly show relief → acknowledgment → next file.
2. Compare style to current intro; inspect easing, eyelines, model scale, clean hands/paper contact and low-poly lighting consistency.
3. Confirm captions are comfortably readable with sound off and Skip matches the intro's upper-right appearance on phone safe areas.
4. Natural run is 15 seconds; Skip at early/middle/late times exits cleanly once; no hanging overlay/music.

## Acceptance checklist

- [ ] Exactly one new scene with specified name/ID and 15-second authored presentation.
- [ ] Both subtitle cues and silent folder hint match the meaning/timing above.
- [ ] Marie's copy is already delivered; no duplicated gameplay action or premature Bayview/New Horizon connection.
- [ ] Shared presentation has first-view Skip and once-only read-only completion contract; original intro untouched.
- [ ] Models/animations/props reused safely; needed local cinematic curves are present and bound.
- [ ] Internal checks and asset manifest recorded; manual cinematic acceptance separately pending or evidenced.
- [ ] No gameplay/build routing edits in this ticket; handoff identifies the exact completion event contract for T04.
