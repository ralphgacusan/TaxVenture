# Verification and panel acceptance

## Current camera direction — approved 12 November 2026

First-person exploration is the active baseline outdoors and inside every establishment. Keep first-person sprint, existing close task views and directed dialogue shots, smoothed Elijah, all other smoothing/contact work, and the remaining gameplay scope. Third-person exploration, doorway perspective switching, outdoor Elijah body, jumping/button and curb onboarding are stored and disabled. They are **tentative**, owned only by [T22](tickets/T22-tentative-third-person-and-jump-adoption.md), after T21; they do not block T04–T21 or first-person release acceptance. Earlier dated implementation records remain historical evidence, not active camera requirements.


Planned checks for implementation; current authoring evidence is in [PROGRESS](PROGRESS.md). **User owns Unity import, Play Mode, rendered/navigation/touch/listening/device/feel and educator acceptance.** AI owns builders/models/bindings/colliders/content and a concrete reviewable result, source compilation and meaningful internal logic/failure checks. No second Unity launch, actual-save reset/access, Git operation or manual scene assembly assigned as testing.

## Internal verification recipes

Discover installed tools and current source before executing; [AUDIT_NOTES](AUDIT_NOTES.md) lists inspected entry points. Use existing harnesses, not a new testing framework. Generated output stays in `Temp/` or another disposable path, never tracked `Tests/*/bin`. Compile current production before any DLL-dependent suite.

- **Build:** `python3 -B Tests/run_level3_repair.py --build-only` compiles runtime and full Editor against existing imported Bee references without opening Unity. No old warning count is an expected pass; record current errors/warnings and limits.
- **L3/shared inputs/papers:** `python3 -B Tests/run_level3_repair.py` builds and runs L3 plus controls. Add `--level2` only where shared L2 behavior changes; it explicitly skips three native EventSystem comparer checks in a disposable test copy. Those native checks remain user-owned, never counted passed.
- **Dialogue/guide/inventory/save/career/training:** inspect actual suite `.csproj`/Program and its dependencies; build/run selected current sources with disposable output and current runtime references. Existing GuideRegression, DialogueRegression, CampaignFoundationRegression, CampaignFoundationRuntimeRegression, ProgressionRegression, CareerBoardRegression and TrainingRegression cover different seams. Do not run stale DLLs or all suites to inflate a report.
- **Cinematics:** `python3 -B Tests/ChapterCutsceneRegression/run_offline.py`, `python3 -B Tests/ChapterEndingRegression/run_offline.py`, `python3 -B Tests/IntroCutsceneRegression/run_offline.py` when routes/audio/subtitles/continuation change; optional cinematic `--game` compile if suitable.
- **Scoped assets/data:** select relevant `VerifyLevel3Scene.py`, `VerifyLevel3Print.py`, `VerifyGuideTargets.py`, `VerifyOfficeNpcMarkers.py`, `VerifyLevel2Assets.py`, `VerifyMobileInput.py`, `VerifyHudCopyPickup.py`, `VerifyShopAndGuideView.py`, current voice/print/content validators. A source-string pin/geometry calculation alone is not contact, native dispatch, rendering or performance acceptance.
- **Known inherited failures:** inspect R-BASELINE-TESTS before comparing. Record failing assertion/compile path and whether present before changes. Never delete tests, suppress failures or rewrite reports to claim a pass. A build or meaningful check newly broken by this work must be fixed before handoff.

For each changed command test real prerequisites/valid inputs, owner, atomic commit, no-success-before-persistence, failed save, duplicate/stale action, cancellation and quiet reload through existing domain seams. Test mode isolation. Avoid tests mirroring trivial implementation. Fixtures must use disposable signed stores/candidates, not real player progress. Each ticket names its exact boundary cases.

## Acceptance gates

| Gate | Accountable tickets | Observable evidence needed |
|---|---|---|
| G1 — Safe retained paperwork | T01,T08,T13 | Full retained base loadout, optional letter occupying capacity, actual certificate acquisition/attachment/use; no paid capacity, silent deletion, lost annotation or duplicate source |
| G2 — Accepted art/contact pilot | T02; all adopting slices | Side-by-side Arthur/folder/laundry-section at real gameplay distances, deformation/hand anchors, normals versus silhouette diagnosis, imported resource delta and current-phone frame/memory measurements; user accepted |
| G3 — Controls/camera/body | T03,T04,T06,T08,T12,T13,T20 | FP outside/inside with existing fixed task/dialogue views, hands/carried paper, one writer/listener, accessible sprint/doorway/return; no active body/jump; T22 optional checks separate, safe multi-touch/Back/settings/focus and recovery; user video/steps/build/device |
| G4 — Story/service/stages | T04–T14,T20 | Fresh start→L1→actual L2 Bayview→Rosa→actual L3 Bayview→Bea; exact approved packets; supported correction, Auditor retention, actual client copies, limited reveals; L4/5 absent |
| G5 — Teaching/learning | T04,T09,T12,T13 | Arthur visibly reaches/operates real example, player performs verified try, then independent real work; no reward/answer/work mutation from watching, popups or animation; learner comprehension evidence |
| G6 — Dialogue/voice/audio | T05–T19 | Short scripts preserve essential clues/personality/choices; exact line/take revision and required/optional/Elijah branch coverage; guide popups silent; listening, offline/mute/duck/pause/cancel and measured audio memory |
| G7 — Time/results/history | T15,T20 | Clearly labeled synchronized clocks/paused state, cutoff/day/voucher, one50-run-XP obligation/floor/no double charge, correct badge/version explanations; first-check/mastery/Trust/wallet/story completion unchanged |
| G8 — Persistence/modes/continuation | Every changed slice,T20 | Command failure/retry/duplicate/interrupt/reload; accepted/copy/choice/reward/source history; grandfathered old saves; Story versus isolated Replay/Practice/Editor; current cinematic pending/consumed/result order |
| G9 — Content authority | T07,T10,T11,T13,T21 | Dated applicable curriculum sign-off for corporate reference, Rosa simplification, Bea regime/sources/credit/rounding/examples; preserved exact fixtures unless separately reviewed; no Marine-only prerequisite |
| G10 — Android/panel candidate | T02,T21 | Named current phone/OS/RAM/build, both landscape orientations, warmed sustained30/frame pacing, input, memory, transitions, audio/UI costs and all preceding gates accepted; no purchase or optional clue victory gate |

## Current T03 first-person acceptance (12 November 2026)

- [ ] Fresh Level 1 Play: first person outside/inside, no exploration body, no doorway perspective change, no jump button/Space jump/curb lesson.
- [ ] Sprint via extended joystick chevrons or desktop Shift, release to walk; Settings/holds/focus/app suspension cancel it cleanly.
- [ ] Dialogue/desk/corkboard/reader/counter exits restore correct FP look/movement/hands/carried paper; smooth Level 1 dialogue Elijah remains intact.
- [ ] Levels 2–3 retain FP exploration and existing gameplay/task returns, now with sprint; move/look touch works in both landscape orientations.
- [ ] Record build/device/reproduction and G3/G8 evidence. T02 Arthur contact and phone gates stay separate; T22 body/orbit/jump checks are deferred and not required for T21.

## User route and release checklist

- [ ] On a user-chosen fresh test campaign, preserve original intro, then L1 safe controls/Notes chest/Bag/counter, Arthur reference, all four taxpayer orders, independent correction, Marie sources/review/copy/explanation/delivery and ending/result/Career.
- [ ] L1 has ID/background and separate read-only example; no2316, added calculation, Quax-guide revert or answer labels in required profile stories. Community public promises/Kanor/Adrian add context without early scheme proof.
- [ ] Start L2 normally: existing opening → real Bayview classification packet/reference/unsupported-field correction → stage Accepted/overall Open/Auditor retention → Rosa assignment. No interim copy errand/reward/timer.
- [ ] Enter laundry through real first-person doorway threshold, retrieve actual four sources, see meaningful work objects, inspect retained optional unlocked letter chest without reward/gate, complete Arthur toolkit watch→try→real16-decision task.
- [ ] Check each Rosa commitment/unsupported confirmation and actual delayed consequence. Correct schedule/review and physically deliver her copy; same Rosa travels/appears once. Missing letter never prevents completion.
- [ ] Start L3 normally: opening → honest recap/actual Bayview May originals/10,000 maintenance correction → accepted retained stage → Bea appointment. No corporate expenses/computation, ownership reveal or required Marine/Nina case.
- [ ] Enter Bea's modest art room; envelope/inspect/Help now or Later uses current simultaneous splash/camera and waits for acknowledgment plus settled view. Studio remains ambition; physical story objects have clear interaction meaning.
- [ ] Watch Arthur physically operate separate four-slip sample, try it, independently file/check/correct real seven-row machine, collect one summary with frozen badge/history intact. Test accepted-batch read-only and wrong-treatment/reason correction.
- [ ] Do each existing first-use calculator example with Arthur and then player; build real taxable income/tax/credit/payable using exact sources and tapes, acquire certificate with completely full base loadout, attach readable supporting papers.
- [ ] Test repeated substantive returns and stamp-only returns separately, approval/copy pickup, wrong balance answers and earned Trust tiers, actual copy removal/handover, limited finding/message, ending and one Continue. No stale hand-in owner or invisible occupied slot.
- [ ] Inspect every qualified used model at expected distances and actions: smooth-curved shading, deliberately hard silhouettes, consistent palette/material/light, no broken UV/deformation; paper/book/folder/page/feed/clip/stamp/hand-contact readable and controlled.
- [ ] Observe correct/incorrect text/form/sound only at Check/review; neutral handling does not grade. Available→interacted→persisted acquisition→owned feedback remains true under failed write.
- [ ] Every retained required/optional ordinary dialogue and Elijah branch has matching approved voice. Arthur guide/coach popups, labels/records/system notices remain silent. Listen to names/terms, volumes, transitions and all Rosa variants, offline and sound/voices independently off.
- [ ] Confirm Office time versus Active review left labels, paused instruction/dialogue/settings/focus, independent reading consumes time, one50-run-XP obligation after first cutoff, later morning work retained, voucher once/case, no closed-app elapsed charge or Trust/coin loss.
- [ ] Confirm badge-specific meanings and legacy assessment version, gross/net/time versus best mastery, corrected progress despite first mistakes. Old best/results/earned history unchanged.
- [ ] Resume at example, profile pickup, Bayview draft/correction/accepted retention, interior entry, toolkit/machine half-action/check/summary, calculator/example/certificate/tape, hand-in/return/approval/copy/delivery, pending ending/result. No lost work, grants, accepted history invented or duplicate event.
- [ ] Existing underway/completed L2/3 saves continue their client; absent Bayview history stays unrecorded. New L3 can use identity background without claiming past acceptance. No inventory/capacity/choice/settings reset.
- [ ] Practice/Replay/direct Editor cannot change story possessions, Trust, wallet, case/stage/choice/cinematic history or reward claims; legitimate mastery/practice history follows current policy.
- [ ] Back, quick repeated taps, settings, focus loss/app suspension and return at each camera/task/audio transition leave no stuck hold, black screen, silent office, tap-through, offscreen carried paper or unreachable target.
- [ ] No duplicate NPC identity, manager, player body/controller, camera transform writer, listener, guide/save/inventory/reward authority or stale loaded cinematic set.
- [ ] All baseline phone evidence and gates G1–G10 recorded; optional purchases/letter never gate learning/progression/future victory; Levels4/5 remain unavailable and outside implementation.

## Device measurement sheet

Record model/SoC if available, physical RAM, Android/version, build/revision/date, battery/thermal state, screen refresh/resolution and orientation. Use the same test path and settings for baseline/pilot/adopting comparison. Include startup/transition separately from sustained play, a warm-up followed by a repeated full route, and a full three-chapter run for memory/thermal trend. Sample frame-time distribution/p50/p95/p99/worst stalls, approximate CPU/GPU bottleneck, memory and loading/audio/UI spikes, touch response and transition latency using available profiler/device tooling. Do not invent numeric memory/polygon/stall thresholds before T02 measurements; record approved measured ceilings and acceptance rationale there.

At30 FPS frame budget is33.3ms; **targetFrameRate=30 is not a pass**. Explain sustained misses/uneven pacing/thermal degradation and repeat affected checks after bounded fixes. User records actual phone behavior; AI may analyze supplied evidence and implement scoped fixes. Lack of measurements leaves G10 pending.

## Evidence record and statuses

Every handoff records owner/session/date, actual changed paths, commands/results/warnings, precise fixtures/failures, unavailable native checks, screenshots/video/listening/device measurements supplied by user, open blocker IDs and next ticket. Use Planned → In progress → Implemented, internally verified; user manual pending → User accepted. Internal completion never checks user boxes. Re-run only checks justified by new changes/failures. Preserve old initiative history; update this pack for the milestone.
