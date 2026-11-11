# Milestone behavior and shared contracts

## Current camera direction — approved 12 November 2026

First-person exploration is the active baseline outdoors and inside every establishment. Keep first-person sprint, existing close task views and directed dialogue shots, smoothed Elijah, all other smoothing/contact work, and the remaining gameplay scope. Third-person exploration, doorway perspective switching, outdoor Elijah body, jumping/button and curb onboarding are stored and disabled. They are **tentative**, owned only by [T22](tickets/T22-tentative-third-person-and-jump-adoption.md), after T21; they do not block T04–T21 or first-person release acceptance. Earlier dated implementation records remain historical evidence, not active camera requirements.


Status: **T01–T03 implemented, user manual pending; T04–T21 Planned; final T22 tentative/deferred.** Original 21-ticket granularity approved 11 November; first-person amendment and tentative T22 approved 12 November 2026. [DECISIONS](DECISIONS.md) records product approval and remaining evidence gates. [EXPERIENCE_FLOW](EXPERIENCE_FLOW.md) specifies every level segment. [CURRENT_STATE](CURRENT_STATE.md) identifies existing owners/amendments. This specification authorizes no production execution by itself.

## Scope and invariants

Deliver the approved Level1–3 adventure, actual Bayview opening stages, missing community story, shorter scripts followed by complete retained dialogue voice, smoother used assets, first-person exploration with sprint, accessible movement, distinct interiors, embodied Arthur, readable physical paperwork, feedback and clock/result clarity. Preserve five-level reveal order and current Rosa/Bea fixtures. No Level4/5 gameplay, required Marine/Nina case, new morality menu, precision platforming/combat/stamina, required purchase, runtime TTS/lip-sync, alternate save/inventory/framework, broad environment regeneration or actual save reset.

One week means a planning target of 18 November 2026, time unspecified. It is not a feasibility or performance guarantee. Missing art/voice/content/device evidence blocks its named acceptance branch; it does not authorize a silent scope cut. Work is serial under repository ownership rules.

## Camera, movement and mobile input

1. First-person exploration throughout Levels 1–3, including streets and all establishments. Preserve serialized camera modes, current first-person interaction rays, the player Rigidbody, one camera writer/listener and existing input/hold ownership. No outdoor exploration body or doorway perspective switching in the active baseline.
2. Preserve desk, interview, corkboard, reader and dialogue framing; restore the prior valid first-person pose/look and hands/carried-paper state on exit. A newer hold wins. Back/settings/focus/disable clear owned pointers and pending gestures; closing taps cannot activate the world.
3. Retain first-person sprint in `Office_Level1`, `Office_Level2` and `Office_Level3`: held joystick extension with chevrons/RUN feedback, 1.4× existing first-person walking pace, or Shift for desktop testing. Cancel on release, control holds, transitions, pause and focus loss; no stamina or latch.
4. Disable jumping from both touch and keyboard/public requests. Do not create a jump button, exploration body or practice curb; no jump guide page. Retain the `run-jump` lesson ID for saved-completion compatibility, now a silent optional sprint-only hint.
5. Keep the smoothed Elijah conversation model and calibrated fit in its existing Level 1 scope independently of camera adoption. Preserve derived models, original assets, contact demonstrations, smoothing qualifications and all other feature tickets.
6. Required routes and optional letter access remain walkable with base tools and no purchase/precision gate. Keep furniture, thresholds and Arthur's routes collision-clear. Safe-area layout uses left movement/right look with Bag, tools and interaction targets unobstructed; preserve tap alternatives and readable text/symbols.
7. Store third-person orbit/collision, doorway hysteresis, outdoor body/hands/carry, player-root targeting, jump/landing/recovery, clips/button and practice-curb code behind `ExplorationBody.asset` → `mixedCameraEnabled: 0`. The missing-resource default is also off. Tentative T22 follows T21; later explicit selection and native acceptance are required before rollout. Its future requirements do not block the first-person baseline.

## Arthur and action ownership

Reuse the existing actor and NpcLife/Locomotion/AnimationDriver, guide content/signals, task commands and checkpoints. Do not spawn the ambient Level2 Arthur and a second teaching Arthur: the same identity is temporarily assigned to the relevant station. Scene-local L3 presence likewise has one authority; do not revive Quax as guide.

| Segment | Join/travel/wait contract | Demonstration and player handoff |
|---|---|---|
| L1 controls/Notes/counter | Short authored lead route, stop at target, wait off route; player may look around | Point at the actual control/object; actual player movement/collection/browse verifies progress |
| L1 reviewed file/board | Stop beside file/board and face both player and evidence; follow only on this introductory leg | Existing former-client source, never Marie; grasp/lift/point/return, then player reads/handles; board remains independent graded work |
| L2 toolkit | Reassign same Arthur from optional plaza routine at a safe boundary; doorway route to laundry station, wait clear | Existing unrelated8,000 salary sample; demonstrate highlighting/cards, release tool, player repeats, then Rosa's own work |
| L3 machine | Join beside Bea's real machine after encounter; wait out of next-target line | Existing four-slip practice packet; grasp/feed/select/lever/reset; player does same packet; real seven rows remain independent |
| L3 calculator | Join beside desk only while a first-use example is due; subsequent help can reuse position | Existing separate example calculator and10,000−2,000, Design Studio tax-band,15,000−2,000 credit examples; player tries each; case calculator untouched |

Follow/lead routes plan only per leg/replan-on-stuck, use paired doorway points, stop/turn before reaching, wait at clear authored points and recover unseen through the current bounded staging seam. If player sprints/explores away, Arthur waits/rejoins when relevant; no permanent following or repeated callouts. Resume places a valid settled state before reveal, never teleports a visible actor. Arriving, pointing, animation completion or a popup advance never fulfills a domain task.

Every station defines actual body position, shoulder/elbow/hand origins, facing, object grip/contact anchors and a camera framing that exposes the next player target. A transfer has visible reach→grasp→attach→carry→place→release; source ownership changes through the command, not an animation event. Match animation foot speed, turns and stop to actual locomotion. Sample props restore to their own owners; examples cannot overwrite live work, reward, add inventory or change frozen first results. Existing saved demo/teach markers skip completed instruction. Replay/help is reward-free and never automatically solves graded work. Guide/coach popups remain silent per user decision.

## Papers, sources, service and persistence

Use current folder, book, paper types, calculator, stamps, inventory and campaign write owners. L1 has ID/background only, no2316. Rosa's live four-record/16-decision packet supersedes the original six-record exercise. L3 retains exact-decimal source/case/year/row/revision provenance, current nine-decision assessment and preserved legacy12-decision history.

T01 provides deliberate full-Bag certificate→existing supporting-slot acquisition, retaining all earlier possessions. This is not a new inventory or a hidden value transfer. Detaching with no ordinary space refuses safely and leaves the readable attached original. An optional letter may occupy a normal retained slot without blocking required paperwork; a full-Bag optional pickup can remain available until room is deliberately made. No eviction, forced consumption, paid slot or speculative archive.

| Situation | Player-visible response | State / commit | Recovery |
|---|---|---|---|
| Pick up/attach/deliver a legitimate paper | Reach/neutral cue; show ownership only after save | Existing owner validates mode, case/source, revision, prerequisites and action identity; candidate→signed persist→publish | Failure keeps old state/paper/copy available with retry; no success flight |
| Invalid/foreign/unsupported mechanical target | Concise neutral refusal/return | No ownership/value change | Same source remains accessible; no grade penalty for missed physical aim |
| Plausible wrong same-case answer | Show neutral draft, no correctness sound | Save ordinary draft through task owner | Only deliberate Check/Auditor judges it; correction stays possible |
| Duplicate callback or late animation event | Already committed settled state | Idempotent current command/claim; no second reward/write of outcome | Quiet restore; ignore stale revision/owner |
| Cancel/Back/Settings/focus during held input | Stop owned gesture, settle props; preserve committed work | No implicit task completion; transient half-feed/grip/angles are not saved | Close detail first; valid saved source/work restored without replaying grant |
| Real action saved, cosmetic motion interrupted | Settle to committed ownership/result | Never recommit on motion/voice ending | Restore canonical paper/tool placement and next legitimate task |
| Hand-in/approval/client copy | Actual folder leaves inventory when owned by Auditor/desk; approved copy deliberate pickup and delivery | Preserve latest L3 signed candidate/removal rules and existing per-chapter completion seams | Latest full editable-packet reset for substantive L3 returns; stamp-only retains values/tapes and advances attempt revision |
| Story/Replay/Practice/Editor | Accurate current mode and isolated objects/results | Only Story writes campaign possessions/Trust/coins/stage history; existing mastery/practice rules remain | No live story contamination; no second save file or reward ledger |

Bayview uses the approved D4 packet. Minimal additive stage data retains identity/content revision, source facts/provenance, current draft/review/corrections, stage acceptance and compact confirmed/question/next-record note in the **existing signed campaign owner**. Use existing level checkpoint seams for in-flight work; no speculative five-level evidence framework. Existing progressed clients/chapters are grandfathered as D5 specifies. Absent history is unrecorded, never failed or Accepted. Current income work may receive identity background without claiming past acceptance.

Main clients require approval→explanation→actual handover→proper closure. Bayview interim stages require review/correction→Accepted/overall Open→Auditor retention→main assignment. No interim corporate copy errand/reward. CoastCare footer and CT-07 are limited context; no early ownership/intent proof, reopened completed personal cases, optional-clue ending gate or fabricated later evidence.

## Clocks, punctuality and results

Reuse each current clock as source of truth; synchronize office-time and active-remaining displays from it, with explicit labels **Office time** and **Active review left**. A paused indication appears while compulsory teaching/dialogue/settings/cinematic/blocking transition/app suspension pauses it; independent exploration/records/work consumes active time. Keep existing allowances, starts and vouchers unless the specific integration requires the equivalent current real-action boundary. Bayview opening stages are untimed and do not start Rosa/Bea's clock early. No closed-app wall-clock deduction.

The first missed main-client cutoff persists one versioned punctuality **obligation of50 XP** for that chapter attempt; it replaces existing late-XP deductions. Compute net run XP as `max(0, gross run learning XP − (late ? 50 : 0))`, including later-earned case XP. Do not use the current `AddPenalty` early clamp to discard an unpaid portion when little XP has yet been earned. No double subtraction in case caps/results, no second debit on later mornings, retries, load or duplicate cutoff callback. Mark time state before next-morning presentation; wait for owned action cleanup; failed persistence leaves a reachable closed-window retry and old work.

Frozen first-check decisions/tier, existing best mastery, story completion/pass eligibility, historical results/versions, coins and Public Trust remain unchanged. Display gross learning result, punctuality−50 and net run XP separately; all visible campaign hearts remain Public Trust. Ordinary case-error deductions retain current rules independently; do not reinterpret them as lateness. Legacy missed flags cannot be recharged retroactively. Mastery/replay comparisons use the protected mastery result, not this new time debit.

Badge legends follow actual assessment version: L1 uses its existing first-check classification outcome; L2 Gold16/16, Silver14–15/16, Bronze0–13/16; revised L3 Gold9/9, Silver7–8/9, Bronze0–6/9. Legacy L3 earned12-decision results keep their original version/legend. Explain corrections permit progress without rewriting the first result. No invented academic percentage or common threshold across activities.

## Art, animation, audio and feedback

Follow the [asset/contact plan](ASSET_AND_ANIMATION_PLAN.md), with individual treatment and an accepted character/handled-prop/room pilot before broad migration. Keep character identities, clothing, palette and theme; smoother stylized shapes, not photorealism. Separate normal treatment, silhouette geometry, material/light and rig repair. Architectural hard edges remain deliberate. No runtime subdivision, library-wide smoothing, palette-UV breakage or duplicated shared meshes for every scene.

Cream/matte papers, dark readable ink, existing muted paper-type accents and manila folder identity remain; reviewed example stays visibly distinct. Existing HUD palette/fonts and neutral interaction highlights remain. Outdoor warm daylight/quiet indoor fill, bounded shadows/occlusion and distinct laundry/art-room cues must fit one game. Avoid new render passes, harsh flashes, constant particles/sway and misleading interactable set dressing.

Each slice owns restrained pick/place/carry/handover, page-turn, door/footstep, tool, acquisition, checked-correct/incorrect, audit-return/approval, reward/badge, clock and room-ambience cues. Reuse AudioManager's library/settings first. Neutral drafts use neutral sound/form; only actual Check/review differentiates correct/incorrect. Use text/symbol/form as well as color/sound, with one concise correction action. Acquisition effects follow persistence: available→interacted→successfully owned→settled. No correctness leak from neutral machine filing, Arthur gesture or collectible animation.

## Dialogue and speech

Every playable slice shortens its required and optional scripts before its chapter's recording pass. Normal turns≤30 words/1–2 sentences, routine required exchange2–4 turns before action, Arthur prompts≤14, Auditor return≤24; essential facts/rules retain paged detail or a justified recorded exception. Cut repetition before adding pages. Never hide the required clue/objective or flatten character emotion into generic commands.

Voice all retained ordinary dialogue, optional branches, Elijah and ordinary Arthur conversation. **Arthur guide/coach popups are exempt and silent**, including current demo tips. System notices/labels/documents remain silent; no menu/number read-aloud requirement. Do not move ordinary dialogue into guide popups to evade coverage. Existing intro/cinematic speech remains and is revalidated if scripts change.

Extend DialogueLine/DialogueSession snapshots and inserted-line paths with stable line identity and exact text/speaker/revision metadata, through existing dialogue/audio ownership. Manifest approval records line/branch/speaker/revision, take/source provenance and clip import/load policy. Current cinematic catalogues are smaller and have no general approval field; add only the needed shared capability, never assume the old proposal's approval metadata already exists. Missing/stale/unapproved clips fall back to readable text and a development diagnostic; final coverage acceptance rejects missing retained dialogue takes.

One foreground dialogue speech owner. Reveal tap completes text while speech may continue; a separate actual advance stops/replaces it; awaited actions remain gameplay-owned. Stop immediately on cancellation/owner loss/scene exit, pause/resume on settings/focus, avoid restarting a muted consumed line halfway through. Duck current music/ambience through existing owner and restore on all terminal paths. Voice completion never advances/grades/acquires. Dynamic spoken lines must have bounded authored variants with exact text/take matching; move variable clocks/amount displays into labeled silent adjacent UI when needed without deleting dialogue meaning. No approximate take over mismatched subtitles, live synthesis or credentials.

Use user-supplied prerecorded workflow matching the current ElevenLabs-style cinematics, with no spending authorized. First pilot determines compression/import/loading/audio residency; load bounded chapter sets and release references on teardown. No universal format/memory/quality budget is invented before measurement. Retain usable rights/provenance evidence; the plan makes no new provider-licensing claim.

## Performance and acceptance

Target sustained evenly paced30 FPS on the user's current phone, both existing landscape orientations. Exact model/OS/RAM and measurement are pending. Baseline/pilot captures frame distribution and stalls, CPU/GPU cause, warm behavior, memory, transition latency, active/skinned actor load, mesh/material/texture, audio and document/UI redraw. Use measured pilot values to set ceilings and compare adopting slices; do not treat source-FBX counts, desktop timing or the current60→30 governor as release evidence.

Prefer shared variants/materials, simple colliders, scoped room/exterior visibility, compatible clipped/cullable animators, bounded nearby NPC behavior, cached references and event-driven redraw/path updates. Never per-frame global search/pathfinding, unbounded effects, offscreen full animation or speculative Addressables/streaming/package adoption. If a pilot exceeds cost, use cheaper shading/detail/visibility/contact solutions before sacrificing readability or required interaction.

Ticket completion requires its binary internal checks plus separately recorded user manual evidence. [VERIFICATION](VERIFICATION.md) owns exact release gates; no all-purpose final-polish ticket absorbs missing per-slice work.
