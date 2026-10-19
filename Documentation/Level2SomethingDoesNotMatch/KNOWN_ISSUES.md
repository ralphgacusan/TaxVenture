# Known gaps before implementation

| ID | Gap | Owner/required response |
|---|---|---|
| L2-01 | Previous scene is now Office_Level2_OLD; new Office_Level2 must be created and routed correctly | T01 follows SCENE_REUSE: one-time current-Level1 scene copy, distinct GUID, local specialization, preserve source/_OLD and existing destination work |
| L2-02 | HUD/map/guide/fade/finale/clock have Level1-specific startup restrictions | T01 establishes scoped opt-in, subsequent tickets add content/behavior |
| L2-03 | Existing cabinet action archives; no retrieval behavior is established | T02 adds phase-safe retrieval |
| L2-04 | Existing verdict only checks filled fields; false-issue results are unfinished | T07–09 implement evidence/reason evaluation with legacy fallback |
| L2-05 | Existing badge EXP threshold is incompatible with forgiving Level2 EXP | T06 uses explicit frozen-accuracy tiers with existing art/enum |
| L2-06 | Existing Level1 clock departure and generic late penalties conflict with Level2 cutoff/caps | T10 introduces level-selected behavior; no double deduction |
| L2-07 | Existing career saving is established; active Level2 checkpoint is a new bounded addition | T11 saves through existing owner/store and tests backward compatibility |
| L2-08 | Six documents/two-document comparison may be cramped on phone | T05/T07 implement usable framing; user tests readability in Play Mode |
| L2-09 | User-selected Rosa/store need local wiring and compatible animations | Use SM_Chr_Business_Female_03 from TaxVentureWithCompliance/Characters and Building_4 (1) from TownEnvironment/Env/Town; preserve source assets/instance appearance, avoid T-pose, keep portrait identity consistent |
| L2-10 | Loan-principal primary/course authority needs explicit verification | T01 records authority or clearly reports draft educational rule; no unrelated citation |
| L2-11 | Current Level1 itself has source work awaiting user Play Mode | Preserve live behavior, do not assume unresolved animation/navigation issues are solved |
| L2-12 | Runtime-generated targets are not covered by existing Level1-only scene validator | T12 validates builder contracts and distinguishes user-only verification |

These are known work requirements, not tested defects in a completed Level2. Update with reproducible findings as implementation progresses.

## T01 findings (2026-10-17)

| ID | Finding | Owner/response |
|---|---|---|
| L2-13 | Office_Level2 was created source-only (file copy plus a new `.meta`) because the scene was not open in the editor. Unity must import it on the next refresh. | User: open Unity and check that Office_Level2 imports with no missing references |
| L2-14 | ~~The inherited case folder still sits on the consultant desk; nothing raises `event:CaseFolderRetrieved`.~~ | **Resolved in T02** (drawer retrieval; desk folder hidden until then) |
| L2-15 | Rosa has no conference-room model or portrait yet (`clientId` is empty, so a harmless "no clientId" warning is logged). The conference-room ClientInteractable has no lines for her. | T04 / T11 |
| L2-16 | Level 2 still uses the legacy Level Complete UI and the Level 1 audit lines. CaseFinale, OfficeHours and ConsequenceReactions are Level 1-only. | T09–T11 |
| L2-17 | Loan-principal rule: no Philippine primary source found. Marked educational draft. | Course owner confirms before T06 finalizes E4 grading |
| L2-18 | `Tests/VerifyInteractionOutlines.py` fails on `Assets/Settings/Mobile_Renderer.asset` outline width. T01 did not touch that file. | Owner of the outline settings |
| L2-19 | `SettingsRegression` expected the old Office_Level2 settings button (`SettingsButtonPlaceholder`). It was updated to the copied Level 1 button (`SettingsUI.OpenSettings`, which opens the same in-game board). | Done in T01 |
| L2-20 | The drawer is runtime geometry laid over the cabinet's face. It may overlap the cabinet's own drawer lines, or pick the wrong side if the cabinet's open space is unusual. | User visual check; tune `Level2CaseRetrieval` layout constants |
| L2-21 | The retrieval is not saved: quitting mid-level restarts it. The active Level 2 checkpoint is T11's job. | T11 |
| L2-22 | Opening the folder by drag does not raise `CaseFolderOpened` (existing Level 1 behavior). Level 2 uses `state:ReviewDocumentsState` instead, so the HUD folder icon unlock still follows the Level 1 rule. | Left as is (Level 1 behavior) |
| L2-23 | Case documents had no way to open (`SupportingDocumentsPageLink` targets folder page 5, which no longer exists). Level 2 now prints held review pages as extra case-folder pages after Taxpayer Information. Level 1 is unaffected (one page). | Fixed in the T02 rework. User checks the page fit (S1 has six rows) and readability |
| L2-24 | ~~Long values wrapped into tall rows that did not fit the page.~~ | **Resolved in T02 polish**: Rosa's pages are limited to 5 short rows (checked by Level2Regression) |
| L2-25 | After Rosa's folder is on the desk, the HUD folder icon opens it anywhere again (Level 1 behavior). Only the first placement is restricted. | Ask the user if they want it desk-only for the whole level |
| L2-26 | The stall's spot, the store's front and its size are measured at runtime (no visual pass by the AI). A crowded plaza could force the logged fallback spot. | User checks placement; record the Console transform; tune `Level2RosaStall` constants or fix the spot |
| L2-27 | Rosa has no portrait art (Rosa_Default is unassigned, so no portrait shows) and no talk or gesture clip (idle plus a turn toward the player). | Art follow-up; existing clips only |
| L2-28 | Rosa's two information beats play in a fixed order: the project has no dialogue choices (rule R4). | Design note; acceptable per R4 |
| L2-29 | The six records print as case-folder pages in page 1's style (user direction), with distinct headings rather than distinct paper layouts. The physical sheets on the desk are labelled D1–D6. | Revisit only if wanted |
| L2-30 | Findings now lifts out as a floating working paper; values can transfer from another visible page. Return-to-folder flips to Findings and reattaches. | Implemented 2026-10-18; user checks paper size/readability, focus/dragging, value transfer, return animation and close/reopen lifecycle in Unity |
| L2-31 | The desk stamp set is off in Level 1's current scene (its flow no longer stamps), so Level 2 switches it on at runtime. The stamp set's own prompt still says "Click to stamp" (Level 1 text). | Fixed for Level 2 (T03). The prompt wording is Level 1-owned |
| L2-32 | Desk stamping float spots, hover height and press depth come from the stamps' measured bounds and the camera view (no visual pass by the AI). | User checks the feel; tune the `Level2DeskStamps` / `Level2StampDrag` constants |
| L2-33 | Pre-existing `GetComponent<T>() ?? AddComponent<T>()` in `HudRewardScreen.cs` (lines ~361, ~456) and `OfficeHudRuntime.cs` (~209), from other initiatives. In the Editor this skips the AddComponent when the component is missing. Not changed by Level 2. | Owner of the HUD initiative: replace with explicit null checks |


### Findings visual follow-up (2026-10-18)

L2-30 follow-up: larger raised/tilted loose paper and text are implemented, with attached source/destination interaction blocked. User visual verification is still pending for readability, paper depth, screen fit and return sizing. The paper continues to use the existing UI document layering, with depth conveyed by its rotated face, shaded edges and cast shadow.


### Laundry wording migration implemented (2026-10-18)

Runtime content now uses Rosa's Laundry: clothes washing, a separate paid blanket-washing job and spare washer rental. Documents, mappings, dialogue, book and sign agree, preserving identifiers and amounts. The specific May-total finding and clearer book rule are internally verified; new-player understanding remains a user Play Mode check.


### Findings clarity redesign (2026-10-18)

The previously discussed Findings usability issue now has an implementation: page/statement/principle fields, generic book tips and transferable principles, explicit comparison, visible unsupported/incomplete feedback and a conditional handwritten observation. Actual payment statements are distinguished from metadata. User Play Mode acceptance remains pending for text fit, touch targeting, timing and whether a new player understands the task without being told the answer. This does not complete T05. The later concrete-payment refinement implements the runtime laundry migration.


### Compact automatic Findings polish (2026-10-18)

The latest source replaces the manual button and five added book leaves with automatic checking and two concise leaves. The slip is smaller and derives its angle from the source paper. Transfer diagnosis found that FindingSlot removed shared feedback, and whole-window coverage could disable exposed targets after touching an overlapping book. Source fixes retain shared feedback and live field objects, and opt the slip into per-point hit/focus filtering. Unity verification remains pending for actual pointer routing, aim alignment, angle, readability and feel; no Play Mode success is claimed. Older explicit-button checklists are superseded by the current T03 polish acceptance list.


### Concrete-payment and paper-angle follow-up (2026-10-18)

The first field now requires May total, the payment clue names blanket washing paid in May, and the generic rule describes including separately recorded jobs. The sheet uses stacked label/value text, a stronger backward pitch and tapered geometry with matching control hit rectangles. Internal rules/geometry checks pass. Visual angle, target alignment, touch feel, field fit and new-player comprehension still require user Unity testing.


### Paper clipping and stamp/cabinet repair — 2026-10-18

The reported supported note was constrained to 98 units and inherited shrink-to-fit/truncation. Source now allocates 198 units at a fixed 28-point font and extends the paper/footer. Attached-to-loose size/angle now derives from the actual projection instead of a fixed unrelated pose. Stamp dragging no longer changes depth/leans toward a different plane when aiming; cached model picking, a filtered pointer surface, shared outlines and front-layer compositing are implemented. Pickup scale contention is removed; impact scale is composed in one place. The cabinet face fits one drawer row with a thinner darker panel.

All are internally verified source fixes; user must confirm full handwriting fits at target resolutions, detach/return continuity, stamp center/edge grabs and outlines, tap-only focus, drag over paper/book/notes, press readability and frame pacing, rejected/supported stamp behavior, pause/cancel/close cleanup and cabinet fit/open/close. No Unity visual or performance result is claimed.


### Remaining desk interaction reports addressed in source — 2026-10-18

The larger fixed note box still clipped the user's actual handwriting. It now measures the full conclusion with the real font at the unchanged size and extends the paper/footer. The replacement stand models were outside the original set's highlight/collider hierarchy: they now have persistent desk affordance and direct enlarged body/handle targets. Lifted tools always composite independently of the desk geometry, and do not receive occluding world shadows.

The book now separates finger-follow from safe drop placement and aligns visible model centres when opening. The loose slip settles on a desk drop; unsupported links empty all three fields for retry. Automatic page-turn cancellation/index timing and current-page requests are repaired, and stamp lifting waits for the page turn. Arthur's hint moves above desk/reading papers. Internal checks cannot establish visual success: user must verify the specific screenshots' cases, text fit at target resolution, outline visibility, stamp rendering/contact, centre/handle touches, book grab/open alignment, page turns and desk drop/return timing. Always-visible tool compositing needs a device frame-pacing check while both stamps are out.


### Stamp remains hidden while aiming — tool-plane repair (2026-10-18)

The prior paper-relative plane and world-space press could still place the model behind foreground desk geometry, while the separate UI ink preview remained visible. Source now keeps all held/press model movement near the viewing camera and projects only the visual contact from the real paper. The actual ink and stamp decision retain their original paper-local point. Both slots are raised, and aiming/pressing use readable partial alignment toward the paper instead of an upright-only aim or end-on press. Capture ordering and viewport mapping are explicit.

The proposed CopyFrom/missing-transform cause was disproved against Unity's API; the redundant code/test were removed. These are source changes with internal validation, not a confirmed rendered fix. User must check both complete models at lift, handle/body grabs, movement over the whole folder, pad/preview alignment, readable press/rebound, no table clipping, correct ink and focus with other papers, plus frame pacing.


### Repeated invisible aiming / drag-required pickup follow-up (2026-10-18)

The pickup paths could conflict: custom press-down opens the set while the legacy touch-release world tap toggles it again; dragging suppresses that legacy tap. All pickup paths now use open-only semantics, with real EventSystem stand targets and Input System press edges. The dedicated button still puts stamps away.

Model-bound viewport fitting and near-clip clearance now run during held/press poses, including offset/letterboxed camera rectangles and space for the coach hint. The held layer stays above documents, with stable isolated renderer layers throughout capture rather than transient layer changes. Source and pure bounds tests pass; actual visible rendering is still a user Play Mode check. Verify quick taps, both handles, all folder edges/top, aim/press/rebound, correct ink, book/notes overlap, cancellation, repeated put-away/reopen and scene/desk exit. No rendered success or frame-time claim is made.

### Folder button, highlight and pad contact — 2026-10-19

Both stamps now share one persistent tool capture/layer (29), replacing the earlier separate layers 29/30. Both refresh highlight targets on lift. Projective pad placement and a contact lock keep the pad centre over the paper-local ink anchor through the press pulse/thump. Folder-button opening centres the paper endpoint while retaining the entrance animation; drag placement remains available.

Internal verification passes. User Play Mode is still required to confirm Ready's outline, complete Not Ready visibility while floating/aiming, both actual pad landings, put-away/reopen and frame pacing. Check folder button centering from first placement and later reopen, plus explicit drag placement. No visual success is claimed.


### Stamp overlay camera and exact landing — 2026-10-19

Lifted stamps now render through a stacked URP overlay camera on layer 28, so world geometry (the desk folder) can no longer hide them; the stamp lands physically on the paper with its pad sized to the imprint. Remaining risks, unverified without Play Mode:
- The highlight outline (`InteractionOutlineFeature`) draws per camera with that camera's culling mask and depth; it may not appear on lifted stamps under the overlay camera. Hover/held feedback therefore also uses scale/lift, which does not depend on the outline.
- When a screen paper window (Notes / loose Findings sheet) overlaps the stamps, visibility above it still depends on the Doc3DLayer capture path (`CompositeOverDocuments`, raised every frame while up).
- The stamp camera inserts itself first in the main camera's stack; any other overlay added later draws above the stamps (intended for the settings board).


### T05 income toolkit — 2026-10-19

- Layout and readability are computed from the records desk's built dimensions and an assumed ~60 degree desk camera. The user must confirm cards, tags and paper text are readable on a phone and nothing overlaps Rosa's tray, the cloth bundles or the desk edge.
- While seated, the desk's own tap interaction (`MarketRecordsDesk`, interact layer) and the toolkit (layer 7 triggers, read directly) both exist. A desk tap that re-triggers "sit" only re-requests the open toolkit (no duplicate), but it should be checked.
- Sound uses existing clips only (paper, select, click, achievement); there are no dedicated lid or marker sounds.
- After the tutorial the case stays open showing the finished payslip; Rosa's own entries arrive with T06, so until then the toolkit has nothing more to do.


### T06 Mark the Money! and toolkit feel — 2026-10-19

- All visuals, readability, particle look and timing are unverified in Unity. ParticleSystems use Sprites/Default squares (no texture); if they read poorly, swap for a soft sprite material.
- The scoreboard on the lid's underside is small; check it is readable from the desk seat on a phone. Its medal is a SpriteRenderer using the existing HudBadgeArt sprite.
- Sounds reuse existing clips (paper, select, click, keyboard ticks, achievement); there are no dedicated ink/lid sounds.
- Leaving the desk mid-mastery lets the award finish (it is guarded to run once) while the case closes; the visuals may overlap briefly.
- The quest state lives in the review (`CaseReviewState.challenge`); durable cross-session checkpoint/restore is T11.
- The reward card uses the scene's existing `SideQuestCompleteUI` panel; if it is missing in a scene, the EXP/hearts are still added through the ledger and Arthur's line explains the result.


### T07 office reconciliation — 2026-10-19

- The F2 page has five boxes, the most any folder page has used; check it fits the folder page on a phone without crowding.
- A schedule box checks overlap by temporarily setting aside the line it would replace; this is internal and does not count a revision.
- The office evaluator results are not shown to the player yet; the Auditor uses them in T09. Until T08/T09, the existing hand-in still decides the case as before.
- Book reasons and conclusions share the Review Notes leaf; the preliminary Findings slip ignores conclusions (they are a different semantic key).


### T08-T10 — 2026-10-19

- T10 deviates from "extend OfficeHours": Level 2 has its own `Level2ReviewClock` and chip; Level 1's OfficeHours (Auditor leaves, game-time face) is untouched. Reason: the Level 1 component is built around the Auditor leaving and a side-quest-based allowance, which Level 2 must not do.
- 12 active minutes start at the receptionist and run through the cabinet, stall, quest and office; walking counts as active time. This may be tight: tune `review.clock.allowanceSeconds` after playtests.
- Approval uses the existing CaseCompleteState; the Level 2 finale/Rosa ending and durable checkpoint are T11, so the post-approval sequence is the generic one until then.
- Rosa's reactions after an audit are shown through Arthur's bubble as quoted messages (she is not in the office).
- If an audit dialogue cannot start (another conversation open), the decision still stands and a short line reports it.
- The rollover fade is its own overlay; check it does not hide the guide bubble that follows.


### T11-T12 — 2026-10-19

- Rosa in the finale is the existing CaseFinale visual-only copy of her stall model; the stall Rosa stays at the market (out of view). Check the conference-room framing with this model.
- Resume restores the level score by re-adding Rosa's talk and the quest award to the in-memory ledger; their HUD float animations may play again on load (no extra EXP: the ledger awards each once).
- Resume does not rebuild the Level 1 game-state machine state (e.g. ReviewDocumentsState); objectives catch up from the review instead. If a Level 1 desk interaction depends on that state after a resume, it needs a targeted fix.
- If the app closes between approval and the level result being written, the pending result is flushed on pause/quit by PlayerProgressManager; a resume of an already-approved review is not offered (Usable rejects Completed).
- The checkpoint writes the signed save every 30 s while the clock runs; acceptable for a small save, but watch device I/O.
- Another session changed Mark the Money! to 16 scored decisions (highlight included); worst-run bound still 48 quest EXP + capped case, so a completed run still passes.


## Level 1 consistency follow-up — 2026-10-19

Implemented — internally verified; user Play Mode pending. This supersedes the older separate top-centre review-chip and 16-page folder descriptions.

- Shared OfficeHoursChip and OfficeMorningOverlay now present Level 2 time in the minimap, an analog cutoff chip below progression, and the same next-morning fade/title/day layout and timing. Day one begins 7:30 AM, subsequent days 8:00 AM; 12 active minutes map to two game hours. Auditor remains present. Existing one-time late penalty and case state are retained. Modal/teaching pauses and gesture boundaries prevent rollover during interaction.
- Folder now has three initial pages and six after the quest: Taxpayer Information, Prepared Packet (S1/N1 tabs), Findings (Finding and D1–D6 tabs after collection), Reviewed Schedule, Office Findings (F1/F2 tabs), Final Assessment. Stable records, evidence keys and grading remain intact. The obsolete page-6 document links are suppressed for review cases.
- Records reuse the existing removable Findings paper: attached world-space preview with layered paper edges/shadow, lift, independent floating comparison, selectable source rows, and animated filing. Detached papers use the existing perspective/composited UI system, not new free-standing mesh simulations.
- Annotated papers have a third HUD icon below Notes/Folder, saved-state contents, tap/drag opening, and a reward-card-following preview, flatten, return-to-icon and pulse. The shared SideQuestSplash shows Mark the Money content before the scored challenge.
- Direct Editor Play in Office_Level2 starts fresh and bypasses checkpoint reads/writes. Completing that attempt also preserves the existing checkpoint. Career-board entry and player builds retain normal resume. Existing career best-result saving remains enabled; this is checkpoint isolation, not a separate career-save sandbox.

Pending user checks: direct Play fresh cabinet start; career-board resume; readable S1/N1 and F1/F2 tabs; detach multiple receipts, transfer values and file them; splash and reward-to-icon animation; cutoff while reading/dragging/stamping and next-morning preservation. No Unity Play/device/visual verification was performed.


### Paper desk redesign acceptance — 2026-10-20

Older notes about Prepared Packet tabs, D1–D6 handover, F1/F2 office pages and final-reason selection are superseded by the latest SPEC section. Source changes are being internally verified; user Play Mode is pending. Check all filled schedule rows and the handwritten summary at the target phone size; first-desk splash precedes practice; four annotated papers open/reveal/drag/layer/return; all six supporting papers gather after a three-second hold, file as a group, and re-scatter on a missed drop. Also interrupt holds/filing with settings or folder close, and focus every sheet to verify consistent size. Regression defaults preserve Level 1; revisit its profile windows and office hours in Play Mode.


### Polish and completion pass — 2026-10-20

- Nothing in this pass was seen in Unity. Internal builds, suites and validators only. Sizes (record rows 104/96 units, spread scale up to 0.8, Findings 1.16 x, annotated papers 430 x 505) are first estimates for a 16:9 landscape phone.
- The spread papers now cover much of the folder (they are larger by design). Players drag them aside or gather them. If this feels crowded, lower `MaxLayoutScale` in Level2SupportingStack.
- The floating Tax Code Book (away from the office desk) pauses OpenTaxCodeBookReturnToDesk and TaxCodeBookFloatingModel3D, so it cannot be dragged while floating; tap the icon or swipe the pages closed. Its page-edge taps rely on the book canvas's existing event camera.
- The "darkness" fix covers the floating annotated papers (unlit material). Office and market scene lighting was not changed (no scene edits). If the desk itself still reads too dark, that needs a lighting or post-processing change in Office_Level2.
- A checkpoint saved during the office review under the old record-line schedule has those lines emptied once (spoken note); the player refills them from the annotated papers. A finished review or approved case is not affected.
- D2 (bank statement) and D6 (cash receipt) remain in the case data for old saves; they are not handed out, printed or graded. The legacy F2 fixture (Tests/Level2Regression/LegacyReview.json) still covers the older two-finding rules.
- The loan rule remains an educational draft (see CONTENT), now also stated on the "Is It Income?" cheat sheet.


### Additional Level 2 fixes — 2026-10-20

- Not seen in Unity. The URP Lit `_EMISSION` variant used for self-light is included in Editor builds; other project materials use emission, but confirm the papers stay bright in a player build.
- Only the closed desk book can be dragged into the icon; an open desk book closes first (tap the icon to see the hint). The floating book cannot be dragged around the screen once open; it opens where it was dropped or in the centre.
- Checkpoints from before this change have `taxBookStored` false: the book starts on the desk again and the prompt shows.
- Older checkpoints with D2/D6 still acquired are fine: record pages print handed-over records only.


### Book, annotated paper and folder follow-up — 2026-10-20

- The floating book is moved by dragging its page. A drag that starts on a selectable Review Notes or Review Conclusions value carries that value instead, as intended. Not verified in Unity.
- Always-composited annotated papers and the floating book cost one extra camera render each while visible (the existing Doc3DLayer path); watch performance on device.
- The supporting records are now only visible through the annotated papers. Rosa's D1-D5 data stays in the case (evidence tags, saves).


### Book pose and paper performance — 2026-10-20

- Layers 23-26 are now reserved at runtime for the four collected annotated papers. Record any future use in TagManager.
- A collected paper captures again only when it moves relative to the camera. If a later change alters a paper's 3D content without moving it, call `Doc3DLayer.MarkDirty()`.
- The reading-pose constants (0.6 m depth, 12-degree lean, 74% / 62% fit) are first estimates; tune in `Level2TaxCodeBookIcon` after Play Mode.


### Tax Code Book on the Case Folder system — 2026-10-20

- Storing the open desk book relies on its existing desk drag (TaxCodeBookFloatingModel3D) starting from the page. If that drag does not start with the pages open in Play Mode, drag the closed book instead and report it, so the page drag can be routed to the same hook.


### Records desk, book colour, folder return and audit summary — 2026-10-20

- The red cover picks grey materials by colour (saturation under 0.2, brightness 0.15-0.86). If a page-edge material is grey rather than white, it turns red too; tune the thresholds in `TaxCodeBook3DDrag.ApplyRedCover`.
- The quest has no exit until it is finished. There is no way to leave Rosa's desk mid-quest by design (user direction).
