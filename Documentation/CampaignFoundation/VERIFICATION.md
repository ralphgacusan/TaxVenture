# Internal verification and user acceptance

This task initially creates DOCUMENTATION ONLY. Its internal checks are local-link resolution, ticket schema, dependency DAG, scope/status consistency and numerical fixture validation. Do not run gameplay builds merely because Markdown was created. No manual checks have been performed or implied by this pack.

## Ownership of verification

AI implementer: source/data review, scratch builds, pure/high-level regression tests, static scene/content/asset validators, failure-injection and save round trips. USER: opening/importing Unity assets, Play Mode, visual framing, object placement, animations, locomotion/navigation, touch/device/performance, screenshots and listening. No AI Unity editor launch, Play Mode automation, scene capture or device operation. A source assertion for an animation declaration does not prove that it played. Do not leave model placement/colliders/UnityEvents as user implementation chores.

Per-ticket evidence has separate Source implemented / Internal verified / User manual accepted statuses. Use unchecked acceptance boxes until the relevant work has actually been done. Record exact commands, outcomes, warnings and environment blockers in PROGRESS, not copied historical pass counts. If no engine/runtime check is available, mark that limitation rather than substitute a fake screenshot or claim imported scenes.

## Commands for later implementation (not run for authoring this pack)

From the project root: python Tests/make_scratch_build.py; dotnet build Temp/ScratchBuild/Scratch.csproj. These compile against installed Unity DLLs, not a Unity import or Play Mode. Check the generator includes newly created runtime sources. Use native project/editor compile checks for any editor setup code without launching Unity.

Run affected existing suites by inspecting each current csproj first: Tests/ProgressionRegression, CareerBoardRegression, SettingsRegression, IntroCutsceneRegression, GuideRegression, TrainingRegression, ControlsRegression, DialogueRegression, AudioRegression and Level2Regression. The latter currently depends on a built runtime DLL. Use dotnet run --project <existing-project-path>; --no-restore only when restored artifacts are valid. Never reuse a deleted Temp/HudOverhaul check or hardcode old suite totals.

Focused new coverage should follow the repository's dependency-light console-test convention (a proposed Tests/CampaignFoundationRegression is acceptable); exercise real high-level production commands, serialization and presentation snapshots. Test a persisted operation by reloading the save, not by looking for a string in source. Keep source-contract checks for actual binding risks, not mirrors of every code line. No new test framework/package.

Use relevant static validators after source changes: python Tests/VerifyGuideTargets.py; python Tests/VerifyLevel2Assets.py and Tests/VerifyLevel2Scene.py for Delivery B/shared impacts; existing HUD/profile/interaction validators where their contracts changed. Expand validators for new deterministic roots, IDs, .meta uniqueness and actual target bindings. Static scene paths and runtime-created targets must be reported separately. Existing warnings must be compared with the current baseline rather than erased from logs.

## Required behavior matrix

| Area | Internal cases | User manual checks |
|---|---|---|
| Fresh campaign/save | No save; exact starting defaults; updated campaign reload; incompatible old data cannot supply Continue or awards; confirmed New Game replaces save generations; cancellation/write failure; ordinary updated-save backup recovery; signature/future-format rejection | Clear fresh-start notice and confirmation, settings retained, no imported old achievements or archive screen |
| Menu | No campaign; intro interrupted; active field/case checkpoint; pending completion; completed A with L2 unavailable; New Game cancel/confirm; settings kept | Reachability, clear labels, camera transitions, no tap-through or blank subtitle |
| L1 checkpoint | Every profile count; board partial/full/frozen/correcting; case inputs; failed/approved audit; clock day/remaining; shop/finale; pause/quit | Relaunch at stable spawn, correct papers and objective visible, no stuck controls |
| Grading/progression | Incomplete check refused; Gold4/Silver3/Bronze0-2; corrected board required; frozen snapshot unchanged; low XP still corrected story completion; unsupported chapter never launched | Check-board readability/feel; feedback after check only; corrections understandable |
| Modes | Story vs replay/practice/direct-editor; temporary inventory; no active checkpoint replacement; no real supply consumption | Mode labels and replay confirmation explain what changes |
| Trust | Start40; milestone once; ordinary wrong answers no change; L1+2+4=46; clamping; score hearts never feed it | Actual reason text and animation; future target understood, not a passed-level marker |
| Wallet | +60 quest/+100 case; claims persisted before presentation; repeated callback/result/pause; no conversation/replay farming; arithmetic bounds | Coin cues and receipt match actual wallet |
| Bag | Same source IDs from original viewer; no hidden answer-key fields; duplicate pickup; originals vs working revisions; completed case files read-only; bookmarks | Paper layering/readability, tabs, Back, inspection during exploration, gesture conflicts |
| Shop | Shared stock/price/ownership; insufficient funds; rapid double confirm; failed save; locked computer/stand; purchase optional | Counter reachable, previews honest, menu/counter agree, installed objects visible |
| Supplies | Carry2; confirmed overflow purchase/pickup to drawer; atomic drawer withdrawal up to carry capacity; consume once/case; +180 active seconds; before cutoff only; rollback; restore at cutoff; app loss; sandbox isolation | Retrieve stored supplies physically, clear destination/effect/confirmation, clock cue, no incorrect statutory-deadline language |
| Collectibles | Claimed pickup absent on reload; supply overflow; same keepsake not regranted; display placement retained; mandatory handover intact | Discoverable/physical, not a pixel hunt, lore and bag location readable |
| Guide/input/audio | Sample fixture excluded; guide never awards; actual actor bindings; cleanup; SFX gated across new events | Quax stands/gestures correctly, never clips/solves the actual task; SFX OFF listening |
| B: Rosa | Existing frozen 12-answer grading/checkpoint restored; coins/service once; knowing vs ordinary-error branch; pending consequence reload | Existing investigation retained, choice wording and reactions believable |
| B: stand | Purchase unlock only after B ready; two source panes same revisions; free comparison exists | Physical stand fit, reading focus, no clipping or answer highlight |

## Delivery A manual script (USER only)

1. Open a fresh install or an isolated test fixture with incompatible old progress: Continue cannot import the old campaign and no archive screen appears. Cancel New Game and confirm no replacement occurs; confirm it and verify the fresh starting values and retained audio/settings. Use fixtures for destructive checks, not the user's real save.
2. Start New Game. Check intro route/skip policy and personally approach Quax. Confirm Public Trust 40/100, future target65, wallet0 and contextual bag tutorial. Inspect first profile, close, continue moving.
3. Collect two profiles; exit/relaunch through Continue. Confirm same two, same next objective and no extra reward. Finish collecting; deliberately rearrange draft board without correctness feedback.
4. Submit incomplete board (blocked), then a complete imperfect board. Read frozen tier. Correct all; verify +60 coins once and +2 trust for completed service; replaying the result cannot award again.
5. Before office review, collect optional postcard and supply voucher; inspect, pin if eligible, display later. Test a full carry/drawer fixture supplied by implementer without editing production save. Bag reading consumes review time; compulsory lesson/settings/shop do not.
6. Start Marie's case. Enter partial work, return to menu and relaunch; confirm inputs, outstanding correction and clock restore. Make an honest submitted mistake; public trust must not drop. Correct it and complete review.
7. Exercise one voucher on an eligible attempt. Read confirmation, see clock gain and supply decrement. Relaunch and verify neither resets. A second use in that case is refused without losing a voucher. Missing cutoff preserves work and permits next morning.
8. Resolve Marie's case and client follow-through. Check total completion coins160 and L1 trust46 before optional spending; low mastery does not block corrected completion. Visit counter introduction; choose Back without buying and still finish chapter.
9. Buy bookmarks/decor (in a suitable funded campaign). Inspect installed tab/prop. Return to main-menu shop; price/ownership/wallet match. Try insufficient funds, rapid repeated purchase, locked stand/computer and cancellation.
10. Replay mastery/practice and leave: live wallet/trust/story/supplies unchanged. Continue resumes the updated story with earned badges and possessions retained. Confirmed New Game resets the active campaign but retains settings. L2 launch remains unavailable in A; its existing scene is not silently entered.
11. Test phone/tall/wide layouts and Android back, scene fades, repeated openings, SFX/music OFF and ON. Inspect Quax actor/portrait match, sample demonstrations, paper/coin/voucher/decor motions and all collider reachability. Record visual defects separately from domain results.

## Delivery B additional manual script (USER only)

1. Enter ready L2 from the completed L1 campaign, confirm same wallet/trust/owned decor and one Rosa case. Existing opening, retrieval, physical toolkit, annotated paper and review flow remain intact.
2. Resume before/after the quest check, paper collection, stamp, audit and cutoff. Verify frozen tier, records, purchases, +60/+100 claims and consequences are unchanged by reentry.
3. On separate New Games, choose responsible follow-through, dismissive hold and explicit knowingly unsupported approval. Check initial reaction, eventual review/correction and actual trust reasons. Honest errors must not enter the misconduct branch. Observe pending reassurance resolves once after reload.
4. Unlock/buy the comparison stand through one shop, use it in the other scene. Compare actual records side by side; no answers are filled automatically. Free comparison remains usable without buying.
5. Finish Rosa, inspect provenance story fact and totals, then career. L3 remains coming-soon. Replay cannot replace the campaign's first story choices or farm rewards. Record exact outcome and presentation acceptance.

## Handoff format

Each completed ticket reports: visible behavior; exact source/content/assets changed; internal commands and results; current baseline warnings; missing source/art/content limitations; USER manual steps pending; next ticket. Never mark 'fully accepted' until user results are supplied. T17/T21 audit the full pack against as-built behavior; update this initiative's docs only. Git operations remain user-owned.
