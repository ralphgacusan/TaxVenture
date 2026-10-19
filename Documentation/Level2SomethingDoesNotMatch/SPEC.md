# Playable specification

## Scene baseline

Create a new Office_Level2 from one controlled copy of current saved Office_Level1, preserving shared asset references and source scenes. Specialize only the copied level behavior and local additions. SCENE_REUSE.md defines the exact procedure, rerun safeguards and menu/build integration. Office_Level2_OLD remains a reference, not the implementation base.

### T01 implementation notes (2026-10-17)

- **Scene copy.** Unity was open on `3D Menu`, not Office_Level1. So the one-time copy used SCENE_REUSE's source-only fallback: the saved file was copied byte for byte and given a new unique `.meta` GUID. Unity imports it on its next refresh.
- **No serialized Level2Gameplay root.** `Level2OfficeStart` builds the `Level2Gameplay` root at runtime, once per load (same self-bootstrap as the HUD and guide), and carries `SetupVersion`. Later tickets add their parts under it. This avoids hand-editing scene YAML.
- **Level opt-in.** The HUD, minimap, guide and fade are switched on through `level.json` `officeFeatures`, via `OfficeLevelProfile`/`OfficeLevelCatalog`. Office_Level1 always gets them all. Finale, office hours, consequences, the door gate, the splash and profile skins stay Level 1-only until their own tickets.
- **Case file.** Rosa's case is `cases/case_rosa_001.json` (case id `L2-ROSA-001`). The old placeholder `case_001`/`case_002` files are kept on disk but no longer referenced.
- **T02 retrieval (user direction).** The retrieved folder flies into the HUD Case Folder icon. The player puts it on the desk from the icon in desk view; it cannot be opened from the icon before that. The packet pages are extra pages of the case folder after Taxpayer Information, turned with the folder's page edges. The Auditor refuses a hand-in until the folder has been on the desk, so retrieval and hand-in stay separate actions.
- **T03 finding and initial stamp.** The finding note is a "Findings" case-folder page whose boxes accept groups of held lines (no exact-key hint). The desk stamps record the initial hold through `StampUI.StampOverride`, separate from the final assessment.
- **T03 removable sheet (user direction, 2026-10-18).** The Findings fields print on a taped working paper that lifts into the existing floating-paper system. It stays editable beside other folder pages and shares their focus order. Dropping it inside any visible folder page flips to Findings and reattaches it with paper motion; all content remains in the same review state. This replaces repeated page switching for comparison without adding another data or input system.
- **T04 stall.** Rosa and the Building_4 store are runtime instances of the originals (Resources `scene_assets`), placed by measured free space beside the plaza. The records arrive as folder pages D1–D6, and the market desk uses the normal desk view.
- **Book leaves.** Level 2 leaves live in `Levels/Level_02/book.json` (`LevelBookLeaves`), shown after the shared book asset. The shared `TaxCodeBookData_Main.asset` is not edited, so Level 1's book is unchanged.

## Goal and scope

Teach recognition of income inclusions, use of supporting records, and justified readiness recommendations. Main question: **Can I rely on this income summary?** Level 1 taught taxpayer identification; Level 2 adds evidence-backed review of prepared work. One main Rosa case, no Ben case. Target duration is a hypothesis: 15–20 minutes, revised from user playtesting, not guaranteed by source checks.

Rosa operates Rosa's Laundry at Plaza Market, Stall 04: customers pay her to wash clothes. This preserves the service-business setup without introducing merchandise cost-of-sales calculations. The other payments are explained as money from a guesthouse for a separate blanket-washing job, and money from someone using her spare washing machine for a few days. Detailed deductions, rates, final-tax computations, corporations and tax elections remain later-level material.

## User stories

1. As a player, I want a short cabinet task so I begin with a concrete responsibility.
2. As a player, I want familiar tools and prefilled identity facts so I build on Level 1.
3. As a player, I want to compare a summary with a source note so I discover a reason to investigate.
4. As a player, I want to record evidence and a preliminary hold so my judgment has a stated basis.
5. As a player, I want to meet Rosa and inspect believable records so the investigation concerns a person.
6. As a player, I want one guided example and independent income decisions so I learn before being assessed.
7. As a player, I want a scored badge and explanatory corrections so errors teach without erasing progress.
8. As a player, I want classification feedback to leave reconciliation to me so returning to the office remains meaningful.
9. As a player, I want to correct the omission and explain a mismatch so both carelessness and unnecessary holds matter.
10. As a player, I want a physical stamp with visible ink and a reason so the decision feels deliberate.
11. As a player, I want a justified hold to be acknowledged so caution is not automatically punished.
12. As a player, I want feedback, preserved documents and recoverable rewards so I can correct committed mistakes.
13. As a player, I want a deadline without losing my case so time matters without a restart.
14. As a returning player, I want career progress and best badge saved so replay cannot farm rewards.
15. As a player, I want a resumable Level 2 checkpoint so a session interruption does not erase the investigation.
16. As a player, I want a clear client ending and restrained story clue so completion has emotional and narrative value.

## Flow and checkpoints

| Phase | Player action and visible accomplishment | Transition condition |
|---|---|---|
| Office welcome | Receptionist assigns Rosa and announces review cutoff | Short conversation finished |
| Retrieve folder | Open labeled drawer, take folder, settle at existing desk | Folder possessed; only one case exists |
| Preliminary review | Compare initial pages; link the paid blanket-washing note to May total; record omission concern | Evidence-linked finding entered; tutorial teaches this once |
| Preliminary stamp | Place Not Ready with unresolved-payment reason | Guided valid assessment recorded; no audit score or case completion |
| Market visit | Walk to plaza-side stall, ask two short questions, collect envelope | Rosa encounter finished; documents accessible |
| Tutorial | Guide demonstrates an unrelated compensation example, player repeats interaction | Hands-on source/type/treatment/evidence example complete |
| Challenge | Review four Rosa entries; make 12 graded decisions; review spread; submit | Complete answers only, not correct answers, allow first submit |
| Feedback/correction | See tier from first submission; correct mistaken classifications with rule hints | All four annotations correctly completed; first score remains frozen |
| Quest reward | Toolkit packs away; Bronze/Silver/Gold card and income list awarded | Award once; office objective activated |
| Office reconciliation | Transfer supported values; fix omission; explain payment-method mismatch | Player records conclusions, even wrong ones; no correctness gate |
| Final assessment | Select reason and physically stamp; store folder | Structurally complete packet, no hidden correct-answer requirement |
| Auditor | Submit folder by existing drag interaction | Evaluate evidence, summary, reason and verdict |
| Correction loop | Repair highlighted concern; restamp changed packet; resubmit | Changed work required; documents/quest retained |
| Completion | Accepted packet, explain outcome to Rosa, record Desk provenance, finale/card | Case reward once; career/save/next-level gate updated |

No arbitrary second trip. Rosa supplies all six supporting documents during the first visit. Follow-up after a hold occurs at the office using collected records. Rosa's completion dialogue uses the existing conference-room/finale staging and the same character identity; do not place two simultaneously active Rosas.

## New reusable finding mechanic

The removable Findings slip has exactly three fields: `Entry to check`, `Clue`, and `Reason`. The first field identifies May total (label and amount), the entry affected by the separate payment. Another document provides a meaningful statement, while the Tax Code Book's Review Notes page supplies the principle through the existing value-source chips. Attached paper is a preview; detach it before editing. The sheet explains what an observation is, and the book's Reading Records page offers general reasoning tips without identifying a client's answer.

Each finding has a stable ID, related entry/document IDs, player's conclusion, evidence references, and a short reason ID. Store observations separately from the player's conclusion and separately from answer keys. Status labels describe the player's work: `Needs checking`, `Corrected`, `Explained`; they do not certify truth. The Auditor evaluates support. False findings are graded too.

Two assessable concerns only: omitted blanket-washing payment and apparent laundry-payment total mismatch. Loan treatment belongs to income annotation and is retained in office treatment review, not a third mandatory finding. Player may revise a loan annotation before final audit without changing their frozen quest grade.

## Side quest: assessment without answering the case

One unrelated compensation tutorial; four Rosa challenge entries. Each entry has three scored responses: (1) source/type note, (2) treatment note, (3) matching evidence set. Highlighting the relevant line is required interaction but unscored motor practice. Type and treatment remain distinct response slots. Evidence choices can include a two-document bundle where needed; do not increase the count beyond 12.

Allow revising any response before `Check annotations`. The first full submission freezes all 12 answers and their correctness. Show entry-level feedback and concise rule explanations; correction attempts do not lower that score further. Gold/Silver/Bronze is based on the frozen accuracy. All errors must be corrected to complete the educational quest; hints can grow specific, with no additional reward penalty. Correcting does not turn Bronze into Gold.

Quest feedback may explain that a payment for a separate blanket-washing job is income or that a repayable loan is not earned income. It must not say `the summary omitted E2`, `add 5,000`, `the correct total is 20,000`, `the mismatch is cash versus bank`, or `stamp Ready`. Summary pages and finding answers are not displayed in the quest. The final spread is a list of individually annotated records, not a reconciled total. A Gold player still must identify application to the case, transfer values, link records and justify the stamp.

Completion award is presented once after corrections. Score/badge may be previewed on first check; do not award twice. The collected list is named `Annotated Income List`; only an accepted office packet is labeled verified.

## Summary review and readiness

Correct the prepared May summary from 15,000 to 20,000 through existing value-transfer interactions. Total calculations are automatic from the PLAYER'S selected entries; never from the hidden answer key. Prior months are a supplied, verified aggregate of 120,000; the supported annual income schedule becomes 140,000. These figures represent a simplified income schedule, not taxable income, tax due, or an official return computation. See CONTENT for bounded educational simplifications.

Final readiness requires the correct income schedule, supported resolution of both concerns, complete existing required case fields and a compatible reason. Filling all slots is insufficient. The player may submit a complete but wrong packet; validation must not reveal correctness before audit.

The stamps retain `Ready for Filing` / `Not Ready for Filing` names for continuity. Here `Ready for Filing` means the consultant recommends the reviewed work packet for the Auditor's final filing process. Nothing is filed automatically by stamping. No new computation or manual form-encoding system is introduced. Existing preparation/print steps may be reused only if genuinely active in Level 1 and if generated values reflect the corrected schedule; do not activate dormant legacy milestones merely because classes exist.

Preliminary assessment and final assessment are separate records using the same two physical stamps, not four stamps. Preliminary stamping does not set terminal filing state or trigger PrepareReturn/ComplianceAudit. A final stamp is tied to packet revision; any later change to graded answers/evidence/summary invalidates it with `Review changed — stamp again`, retaining its old imprint in a small history area. Cosmetic page turns do not invalidate it.

## Audit outcome contract

Evaluate independently: omission fixed, mismatch explanation supported, income treatment correct, inherited required fields correct, readiness verdict correct for the current packet, reason supported. A correct Not Ready identifies a real remaining defect in that packet. It is an acknowledged interim hold, not an accepted completed case and not an audit mistake. No repeated hold bonus or penalty; use collected records to continue.

Ready plus any real unresolved defect returns targeted corrections. Not Ready with only an explained/nonblocking difference is an unsupported hold. Ready on a correct packet with an unrelated reason is a reason error. A missing mandatory slot is an incomplete interaction (prompt before submission), not a penalized tax mistake. Unchanged rejected/held packets cannot be repeatedly submitted; a meaningful edit is required.

Do not count one underlying defect multiple times as a wrong total, omission, verdict and reason. Use distinct root cause IDs and cap deductions as defined in REWARDS_AND_VALIDATION. Auditor feedback points to record/page and rule without editing it for the player. Incorrect approval is intercepted before actual simulated filing; Rosa experiences delay/rework, not an invented legal penalty.

## Narrative and timing

Rosa asks whether this can be finished today, not for deception. Correct care produces worry then relief; an unsupported hold produces understandable frustration; premature approval produces initial relief then disappointment. No permanent trust simulation.

The summary bears `Prepared with assistance: Taxpayer Assistance Desk`. After review Rosa confirms provenance. The saved fact is `Rosa says the Desk prepared the original summary; one omission was found`. Do not infer deliberate concealment or identify a villain.

Deadline is announced at opening; default allowance is 12 real active minutes, exposed in Level 2 data. Pause during compulsory tutorial, conversations, blocking result cards and menu; not while independently reading/solving documents. Warnings at 2 minutes and 30 seconds. The Auditor remains present. At cutoff, finish an in-flight gesture atomically, preserve work, fade to next morning at the consultant desk, reset a generous 12 active minutes and lose punctuality eligibility once. Only the first miss changes rewards; repeated days cannot bankrupt or reset the player. See reward caps. Successful on-time final approval earns the existing-style On Time recognition, not repeatable stamp EXP.

## Out of scope

Second case, corporate assessment, intentional fraud verdict, final withholding/capital-gain exercises, cost of sales, new tax-rate calculations, VAT/percentage-tax tasks, separate trust/currency systems, random document generation, freehand handwriting, multi-client schedules, new locomotion or full NPC walking AI. Later levels reuse the evidence/finding/reason/revision contracts with new data.


### Findings presentation refinement — 2026-10-18

Per user Play Mode feedback, the detached working sheet is larger and raised with tilt, stronger shadow, visible edges and enlarged text. Attached paper is a preview only: it cannot provide or receive value transfers. The player lifts it before selecting its reason or filling its fields; transfer eligibility ends as soon as return animation starts. Reattaching preserves finding state and restores the fitted folder preview.


### Approved business simplification — 2026-10-18

The user approved Rosa's Laundry as the replacement business in documentation. Use laundry payments (12,000), payment for a separate blanket-washing job (5,000), spare washing machine rental (3,000), and the same repayable loan (20,000). Preserve the initial subtotal 15,000, supported May total 20,000, prior aggregate 120,000 and annual schedule 140,000. The omission, bank/cash mismatch, six records, four entries, twelve graded decisions, evidence IDs, rewards and ticket order remain the same. Keep the selected character/store and reinterpret cloth bundles as folded laundry; a new building or equipment model is not required by this content change.

The subsequent user request authorizes runtime implementation: case documents, dialogue, signage, book wording and regression expectations now use laundry work and a separate paid blanket job. Historical PROGRESS entries retain their original wording. IDs and monetary fixtures remain stable.


### Automatic link checking and compact presentation — 2026-10-18

This latest user direction supersedes the manual Compare button and longer five-leaf book design. The slip checks itself after a field changes. Partial drafts show `Add three details`; a complete unsupported link shows `No link yet` and one short explanation. A supported link shows `Needs checking` and reveals a short handwritten note in the Notes font. No manual check button, discrepancy headline or lengthy instructions remain.

Only a supported link raises FindingRecorded once; three filled boxes alone do not advance the objective. The initial rule requires S1's `SummarySubtotal` (May total), the meaningful payment statement `ReferralNote` in N1 (Payment for: Blanket washing), and reason `unaccounted-payment`. Optional `concernKeys` restrict this case to the reported total; absent keys preserve legacy page-level rules. Reference/contact/location metadata does not explain an omission. Changed data invalidates confirmation and is checked again automatically; a stamp cannot bypass validation. Checks create no reward, penalty, packet revision, final assessment or automatic correction.

Exactly two level-specific book leaves remain: **Reading Records** (brief date/scope/duplicate/purpose clues) and **Review Notes** (three selectable reasons with one short explanation each). Headings contain no level number or client-specific answer. Shared book pages remain intact. Reasons use stable IDs under FindingReason; only display copy changes.

The detached slip is about 564 x 639 reference units, reduced from 672 x 896. Its visual plane derives from the source folder relative to the viewing camera, including transform scale signs. It retains paper depth, large field type, touch targets and a brief handwriting reveal. Attached editing remains blocked. A new opt-in per-point raycast filter keeps exposed slip fields usable when a higher book overlaps another part of it; actual covered points still pass through to the book. Existing windows retain their prior default behavior.

Transfer feedback reuses ValueTypeValidationFeedback, DropFeedbackFX, ValueHoverFeedback and the existing DropAimIndicator. Field objects stay alive through value refresh so drop feedback can finish. Tap-source/tap-field and source drag/drop both use ValueTransferManager; category-compatible placement is separate from whether the completed link makes sense. User Play Mode is required to assess feel and readability.


### Concrete-entry and perspective refinement — implemented 2026-10-18

Always stack the field caption above its value; never switch to a same-line caption for short text. Use Entry to check for the reported May total, Clue for the separate paid-job statement, and Reason for the generic book principle. Validate automatically on edits; wrong summary rows cannot pass the preliminary stamp. The handwritten supported explanation remains conditional.

The loose paper uses a modest positive backward pitch (26–34 degrees), source roll clamped within four degrees and a gentle mesh taper (0.92 at top, 1.03 at bottom). A shared width function also sizes and positions field transforms, keeping actual hit rectangles aligned with their visible controls. Existing depth, focus, transfer feedback and reattachment remain. This is an overlay-paper perspective treatment; user Unity visual/feel verification is pending.


### Paper continuity and stamp interaction repair — 2026-10-18

This supersedes the fixed 564 x 639 loose slip and prescribed 26–34 degree tilt. The paper design is 600 x 780 reference units (attached preview 480 x 624). The handwritten block expands from 98 to 198 units with font size fixed at 28; the lower paper, rule and footer extend accordingly. The loose scale derives from the actual attached projection, lifting by only 6 percent. Preserve the source plane and ease between it and a three-degree peel, rather than snapping to a preset angle. Return interpolates from the current visual pose to the projected folder pose.

A stamp tap focuses without pressing. Crossing an eight-pixel drag threshold starts movement, preserving the grabbed screen offset. The held body remains on its home camera plane; only the ink preview follows the paper. At release, align/press/rebound/home use uniform perspective-depth compensation, and a single scale owner composes a subtle impact pulse. No competing pickup-scale coroutine remains. Cached model bounds exclude text geometry; shared HighlightEffect supplies hover/grab feedback. A small filtered pointer surface consumes underlying paper gestures at the same visible grab area. Existing Doc3DLayer handles front ordering only while lifted; its temporary resources are released on return/disable. Modal/pause and canceled touches cannot submit an unintended stamp. Rules, ink commits and rewards remain unchanged.

The runtime cabinet front now fits the selected four-row cabinet's second drawer rather than a small floating panel across the authored face. Thin inset depth, matching darker metal and a fitted collider replace the protruding plate. Asset/scene originals are preserved.

The shared Doc3DLayer extension is default-off: only held stamps opt into covering an already-composited document below them, and only on overlap. Existing paper layers preserve their behavior. Stamp model lists and projected grab rectangles are cached; no full model scan occurs for every pointer query.


### Measured notes and desk interaction follow-up — 2026-10-18

This supersedes the fixed handwriting box: measure the full supported conclusion with the Notes handwriting font at size 28 and width 475, use its preferred height plus 24 units of padding (minimum 198), and extend the paper, rule, footer, attached fit and loose/return sizing together. Never shrink this note. An unsupported complete link shows No link yet and clears the editable draft's three fields together, invalidates confirmation and allows retry; stamped findings cannot be cleared. The printed face eases into a small settle motion when dropped outside a folder.

Replacement stand stamps now get their own desk affordance/highlight and projected handle/body targets, expanded by at least 24 screen pixels. Poll these targets before lifting; suppress a second toggle in the same frame from the legacy set callback. Lifted stamps opt into always-visible isolated compositing, so desk models cannot hide the tool or its press; disable received shadows on the tool. Existing documents retain the compositor default. Automatic folder turns cancel/restore any previous turn, swap index/content together at the edge-on midpoint, ignore manual arrows during a sequence and complete before stamps lift. Requesting the current page refreshes it and completes immediately without leaving a stale coroutine handle.

The tax book's held position is separate from the clamped resting target. Project movement on the lifted plane, compensate for the lift once and retain the initial grab offset; after the movement threshold, follow directly at fixed rotation. Opening aligns the visible closed/open mesh centres while preserving the reading depth; it does not align different authored pivots to the release finger. Arthur's hint bubble eases upward to a 24-unit top inset in desk and profile-reading modes, returning to its usual position elsewhere.

The loose paper retains a minimum 0.94 visual scale when the measured note makes the fitted folder preview taller. This prevents extra writing room from indirectly shrinking the detached handwriting; the attached preview still fits its folder.


### Stamp tool-plane aiming repair — 2026-10-18

Keep floating, dragging and the press/rebound model in a near-camera tool plane at 1.6 times the viewing camera near-clip distance. Positioning only relative to the paper can leave the body inside the table's foreground geometry; pressing into paper/world depth reintroduces that path. Project the real paper contact into the tool plane for the animation; commit ink and validation at the original paper-local point. Preserve uniform perspective scale compensation.

The floating slots are 0.69 and 0.45 viewport height, lifting the lower stamp above the desk lip. During valid aiming, interpolate 38 percent toward the paper-normal orientation and position the pad just above the indicated imprint. Release uses a readable 60-percent orientation, a short wind-up/press/rebound, and returns home. Outside the paper retain the original grab offset and upright pose. The isolated document renderer captures after tool movement (execution order 500), renders its texture across the full render target and displays it in the main camera's viewport. Existing document layering remains.

Correction to investigation: Unity's Camera.CopyFrom already copies the transform, as stated in the official Unity 6 API documentation (https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera.CopyFrom.html). The initial missing-transform hypothesis was disproved; redundant transform assignment/source assertion was removed. No such root cause or visual success is claimed.


### Stamp tap pickup and viewport containment — 2026-10-18

Desk stamp pickup is an idempotent OpenFromDesk action. The transparent EventSystem hit targets over each real model and the Input System mouse/touch press path use the same expanded projected model bounds. The legacy world tap (touch release) also calls OpenFromDesk, so one gesture cannot open on down and toggle closed on release. Put Stamps Away explicitly closes the set. A same-frame guard remains.

Held/press models fit their full primitive bounds into the viewing camera pixelRect, rather than the full Screen rectangle. Enforce near-clip clearance and reserve the upper 18 percent for the coach hint. If a model would leave the viewport, correct its visual position while retaining the original paper-local ink point. Re-evaluate after pose/scale changes; invalidate cached hit bounds when corrected. During hold/press, keep the tool's document layer in front without a focus-size pop.

Runtime stamp renderers use two stable isolated layers (Ready 30, Not Ready 29; verified unused in TagManager), while root colliders remain on the existing interaction layer. They opt into PersistentRenderLayer instead of per-render layer swapping. Restore model layers when returning/disabling/destroying the tool layer. Ordinary documents keep temporary layer isolation and animated focus by default. No TagManager, scene or prefab mutation is required.

### Folder button and stamp contact follow-up — 2026-10-19

At the desk, tapping the Case Folder icon places/opens the folder with its paper centred in the camera viewport; dragging still uses the player's drop position. Initial placement still uses the existing retrieval/placement commit, and outside-desk gating remains. Only the tap endpoint changes: existing entrance timing, easing, scale and rotation stay intact.

Both lifted stamps share one isolated tool capture on layer 29, above document windows, and refresh the same highlight targets. Their rubber-pad centre projects onto the indicator/ink point at impact. The contact remains anchored during the uniform impact pulse and paper thump; validation and ink coordinates still use the real paper-local point. Implemented and internally verified; rendered visibility, outlines and feel await user Play Mode.


### Stamp overlay camera and exact landing — 2026-10-19

Supersedes the tool-plane / capture-ordering notes above for visibility. While up or flying back, stamp renderers are on layer 28, excluded from the main camera and drawn by a URP Overlay camera stacked first on it, so they are always above the 3D world (folder, book, desk). The Doc3DLayer capture remains only for screen paper windows. The press moves the real model onto the paper: rubber face centre on the imprint's paper-local point, pad parallel to the page, rotated by the imprint angle, sized so the pad covers the imprint. While aiming, the stamp hovers just past the ghost (leaned back) so the ghost stays visible; touch aims above the finger. The decision and ink happen once at impact at the ghost's point; animation still never decides anything.


### T05 toolkit and single annotation view — 2026-10-19

One view (`AnnotationPaper` + `IncomeToolkit`) and one set of actions (`AnnotationActions`) serve the tutorial and, from T06, the scored entries. A note card only fits its own kind of place; fitting says nothing about correctness, so there is no answer-key drop validation. The tutorial is practice: the guide's demonstration is presentation only, the player's practice work is stored apart from the reviewed packet (`tutorialAnnotation`, no packet revision), and completion requires the player's own work to repeat the demonstration. Rationale: the guide stays observation-only, and T06 can freeze first-submission answers without tutorial state leaking into scoring.


### T06 scoring and feel — 2026-10-19

Mark the Money! uses the same toolkit view and `AnnotationActions` as the tutorial, plus `IncomeChallenge` for checking. The badge comes from first-check accuracy (`rewards.json` goldAtCorrect/silverAtCorrect), never from the forgiving EXP ratio; EXP is 60 minus first-check wrong with a 48 floor; 3 hearts on completion. After the first check, right notes lock and wrong notes are flagged until changed, which makes correction a focused game loop without revealing anything about the office summary. Fitting places light up only while a card is held and never mean "correct"; correctness appears only after CHECK.


### T07 office reconciliation — 2026-10-19

The reviewed schedule is built from record lines, each tagged with the payments it contains (moneyIds). Counting the same payment twice is refused structurally and explained ("already counted in ..."), which satisfies "D6 cannot silently add an independent row" without revealing any tax answer. Other choices, including the loan, are accepted so a complete wrong review can reach the Auditor. Totals are computed from the player's lines only. F1 continues the initial finding (original record + conclusion); F2 is an open comparison prompt. The evaluator's root causes are distinct per defect and the F1 conclusion without the scheduled payment is the same defect as the omission (counted once).


### T08-T10 decisions — 2026-10-19

Final stamp: structural prerequisites only (office review complete + reason); commit at impact to the packet revision; graded changes (including the reason) raise the revision and make the stamp stale. Audit: one recorded outcome per admitted hand-in, decided before the dialogue; defects are office root causes plus inherited field issues; a verdict error caused by a defect is not charged separately; a falsely held explained difference is one `false-hold` root; a hold must name a present defect (else reason error). Case penalties use data caps (15 EXP / 1 heart); Level 1 keeps uncapped rules. Cutoff: active-time clock with a pause predicate, a deferred rollover that never interrupts an action, and a single late consequence inside the caps.


## Level 1 consistency follow-up — 2026-10-19

Implemented — internally verified; user Play Mode pending. This supersedes the older separate top-centre review-chip and 16-page folder descriptions.

- Shared OfficeHoursChip and OfficeMorningOverlay now present Level 2 time in the minimap, an analog cutoff chip below progression, and the same next-morning fade/title/day layout and timing. Day one begins 7:30 AM, subsequent days 8:00 AM; 12 active minutes map to two game hours. Auditor remains present. Existing one-time late penalty and case state are retained. Modal/teaching pauses and gesture boundaries prevent rollover during interaction.
- Folder now has three initial pages and six after the quest: Taxpayer Information, Prepared Packet (S1/N1 tabs), Findings (Finding and D1–D6 tabs after collection), Reviewed Schedule, Office Findings (F1/F2 tabs), Final Assessment. Stable records, evidence keys and grading remain intact. The obsolete page-6 document links are suppressed for review cases.
- Records reuse the existing removable Findings paper: attached world-space preview with layered paper edges/shadow, lift, independent floating comparison, selectable source rows, and animated filing. Detached papers use the existing perspective/composited UI system, not new free-standing mesh simulations.
- Annotated papers have a third HUD icon below Notes/Folder, saved-state contents, tap/drag opening, and a reward-card-following preview, flatten, return-to-icon and pulse. The shared SideQuestSplash shows Mark the Money content before the scored challenge.
- Direct Editor Play in Office_Level2 starts fresh and bypasses checkpoint reads/writes. Completing that attempt also preserves the existing checkpoint. Career-board entry and player builds retain normal resume. Existing career best-result saving remains enabled; this is checkpoint isolation, not a separate career-save sandbox.

Pending user checks: direct Play fresh cabinet start; career-board resume; readable S1/N1 and F1/F2 tabs; detach multiple receipts, transfer values and file them; splash and reward-to-icon animation; cutoff while reading/dragging/stamping and next-morning preservation. No Unity Play/device/visual verification was performed.


## Paper desk redesign — 2026-10-20

This supersedes the previous tabbed prepared packet, single annotation list, two office findings and selectable final reason.

- Objectives retain all 13 milestones in three short chapters (five, five, three steps). Notes keeps Level 1's paper, handwriting, checkmarks and active highlight at readable size; arrows browse chapters and progress selects the active chapter.
- The folder has separate Summary and Payment Note pages. Supporting Documents contains a cover and the detachable Findings slip plus D1–D5 after collection. D6 remains readable in legacy state/data but is no longer handed out or offered as a challenge choice; its receipt reference is included in D1.
- Drag/tap the cover to spread the papers. Hold any loose sheet for three seconds to gather them, then drop on the folder to file all together; release elsewhere to spread again. Focus preserves paper scale. Hiding the folder/settings cancels held gestures and interrupted filing. Cutoff waits for group animation to finish.
- The first records-desk visit shows the shared Level 1 splash before the tutorial; the dismissed intro is saved. Existing tutorial-complete checkpoints skip it.
- Reviewed Schedule contains the unlisted-payment source and conclusion below totals. The separate Office Findings page is removed. Final Assessment displays a handwritten summary of submitted work and accepts a final stamp without a final reason. Preliminary finding reasons remain. Auditor correctness checks, double-count refusal and stale-stamp detection remain; old cases default to the legacy rules.
- Annotated rewards are four independently draggable paper windows with source/annotation reveal and coordinated icon opening/return. Their rendering uses the quest's actual AnnotationPaper model, with shared document focus/compositing.

User Play Mode acceptance remains required for paper readability, touch holds, layering, animation feel, interrupted filing and saved-state resume. No Unity Play or device checks are claimed.


## Polish and completion pass — 2026-10-20

Implemented — internally verified; user Play Mode pending. User direction: simplify the case, make the Side Quest 2 annotated paper the knowledge and value source, add the Tax Code Book as a learning reference, and fix readability. Flow stays Case -> Side Quest -> Case. Interpretations confirmed with the user in chat: (1) "two listed lines + restore the missing entry" means the schedule has the summary's two listed lines plus one Missing entry; (2) Supporting Documents = Findings + four records, with the bank statement dropped; (3) schedule values come from the annotated papers only.

- **Records.** Rosa hands over D1 (clothes collection, 12,000: listed), D4 (washer rental, 3,000: listed), D3 (blanket job, 5,000: the missing payment) and D5 (loan agreement, 20,000: explains the confusing deposit and memo). Each prints at most four rows in a larger record style (104-unit rows, 40-point type), and each record paper is as tall as its rows. D2 and D6 stay in data for old saves only and are never handed out or printed (Level2RecordPaper prints handover ids only).
- **Mark the Money!** Same four entries, 16 decisions and scoring. Lines are shorter (one money line each), every entry offers the same four records, and the clip holds one record: E4's evidence is D5. The tutorial's stale "build ends here" line is replaced.
- **Reviewed Schedule.** Three lines captioned from data (`scheduleSlotLabels`: Listed: Clothes washing, Listed: Washer rental, Missing entry), totals, then one conclusion (F1 now has only a `conclusion` part). This removes the overflow seen at 4 lines + totals + 2 F1 boxes. Sources are `scheduleSources[].entryId` E1-E4; the loan (E4) is accepted on a line and graded by the Auditor (omission + not-income-counted), never refused for correctness. Record rows are refused with a pointer to the annotated papers. Tapping an empty line with nothing picked up brings the annotated papers out (`OpenForReference`), laid out against the left edge when the folder is open. Older checkpoints' record-line rows and any hidden fourth line are emptied once (`OfficeReview.DropRetiredRows`), with a spoken note.
- **Annotated papers as value source.** `AnnotationPaperCollectionFace` lays an `AnnotatedValueRow` (IDataValueSource + existing ValueDragSource / tap selection) over the saved highlighted line, measured from the real 3D row each frame. It exists only while the office review can be edited and the annotations are showing (they open showing during the review). Blue pulse = can be used, amber = picked up, green = already on the schedule. The papers render with the same model but unlit (`ToolkitBuilder.Unlit`), fixing their grey look; they are larger (430 x 505).
- **Tax Code Book.** `Level2TaxCodeBookIcon` adds a drawn book icon at y -300 (Notes -20, Case Folder -160, Annotated Papers moved to -440). At the office desk with the book in view it opens the desk book (`TaxCodeBook3DDrag.OpenFromHud`) and a second tap returns it; anywhere else the open book flies out of the icon and floats with the desk framing, its desk-only drag/return handlers paused and restored. It follows the other paper icons' modal rules. Level 2 leaves are now three cheat sheets (The Cards, Is It Income?, Checking a Summary), then Review Notes (three reasons; the bank/cash reason is retired) and Review Conclusions. The book opens once on the first cheat sheet, then on the last page read; Level 1 still opens on page one. A leaf that would overflow its body box shrinks (to two thirds at most); text that fits is unchanged.
- **Supporting stack layout.** With three or more papers, Findings gets its own column at 1.16 x, and the records sit in a 2 x 2 grid. The spread scales up to 0.8 (was capped at 0.62) and scatters with a light overshoot.
- **Rationale.** One decision remains at the end (which payment restores the summary, and that the loan stays out), supported by the annotations the player already made and the book's rules. The answer is never printed: hints and cheat sheets state concepts, not Rosa's values.


## Additional Level 2 fixes — 2026-10-20 (supersedes parts of the polish pass above)

Implemented — internally verified; user Play Mode pending.

- **Book pages.** The three Level 2 reference leaves are now Section 4 – Income Types, Section 5 – Income Treatment and Section 6 – Checking a Summary, followed by Section 7 – Review Notes and Section 8 – Review Conclusions. The headings follow the shared book's style, and the word "cheat sheet" is gone. Each reference term is printed the same way as the shared pages' values: a highlighted label with its explanation underneath, using the shared inline-description layout and its larger type. The terms carry `readOnly: true` (new `TaxCodeClickableValue.readOnly`), which hides the grip and turns off pick-up.
- **Findings paper.** The Supporting Documents cover and stack presentation are removed. The folder page is titled "Findings" and holds the actual Findings paper, attached at about 60% of the page (`AttachedWidthFit`/`AttachedHeightFit`) so the page around it stays visible. Rosa's four records are ordinary read-only folder pages after Findings (rows still work as clues). Lifting plays a peel-up with a soft overshoot and a growing shadow. Loose, the paper is a little larger (0.8). Dropping it anywhere on an open folder files it, with no hold needed: it glides toward the folder and hovers while the folder turns to Findings, then settles onto that page and attaches. If the folder closes mid-way, it stays loose.
- **Final Assessment.** The handwritten case summary is now the last row.
- **Tax Code Book icon.** The icon starts empty (greyed). While the desk book is in view, the icon pulses and shows a "Drag the book here" tag. Arthur's first-finding lesson also asks for this. Dragging the closed desk book onto the icon flies it in (`TaxCodeBook3DDrag.TryStoreAt` hook) and parks the desk copy hidden; `CaseReviewState.taxBookStored` is saved. Once stored, the icon behaves like the others: a tap opens the real book in the centre, a second tap flies it back, and dragging out opens it at the drop point. This works while idle, walking, at the office desk and at Rosa's records desk. An empty icon cannot be dragged out, and tapping it explains where the book is. Desk resets and leaving a desk never put a stored book back (`IsStoredElsewhere`). The review cutoff waits for the book's animations.
- **Annotated papers.** Each collected paper is the Side Quest's AnnotationPaper with the player's highlight and the same TYPE/TREATMENT/RECORD cards attached in their places, built by the extracted `IncomeToolkit.BuildCardVisual`. Mastered work shows the green ticks and locks. There is no flip and no slot-text substitute. They are lit as on the desk with a little self-light (`ToolkitBuilder.SelfLight`, URP Lit emission) and a soft two-step drop shadow, replacing the flat unlit look. They open in a single row across the screen, overlapping evenly only when the screen is narrow. Each is a normal paper window: tap to bring it to the front, drag it anywhere. The office value bands over the highlighted amounts are unchanged.


## Book, annotated paper and folder follow-up — 2026-10-20 (supersedes the matching points above)

Implemented — internally verified; user Play Mode pending.

- **Tax Code Book.** The "Drag the book here" tag and the icon pulse are removed. Arthur now mentions, as a clue, that the book can slide onto its icon and might come in handy. A held book (the desk book while dragged, the floating book while moved) is red-tinted through renderer property blocks (`TaxCodeBook3DDrag.SetHeldTint`), and the tint is always cleared. The floating book can now be moved: a `FloatingBookGrab` on its printed page takes drags that the values and page edges do not use, and releasing it over the icon puts it away. While out, the book's Doc3DLayer always composites, so the HUD and desk never cover it. The book always opens on page 1 (the earlier first-leaf rule is removed).
- **Annotated papers.** Every collected paper's layer always composites (`AlwaysComposite`). Before this, a paper that overlapped no other window, typically E1 at the left of the row, was drawn in the 3D world, so the HUD panels and desk geometry covered it. Papers are larger (462 x 539, up to 62% of the screen height). Rows on records with at most four lines print at 0.205 instead of 0.17, in rebalanced label/value columns, and the row labels are shortened (Paid in cash, Rented, Bank credit, Description). Highlighter ink and hover bands draw before the text (render queue 2980) at lower opacity, so they mark a line without covering it; this also applies at the market desk. The office value band is now mostly a frame. Picking up a highlighted amount (tap or drag) turns an open folder to the Reviewed Schedule and brings it in front of the papers, so the drop lands on its lines.
- **Case Folder.** The record pages (Clothes Collections to Loan Agreement) are removed. The folder is Taxpayer Information, Summary, Payment Note, Findings, Reviewed Schedule and Final Assessment.
- **Reviewed Schedule.** The captions are "Summary line 1", "Summary line 2" and "Missing entry". They never name the work or an amount; the player matches the annotated papers to the summary.


## Book pose, red book and paper-drag performance — 2026-10-20

Implemented — internally verified; user Play Mode pending.

- **Cause of the suspended, upside-down, oversized book.** The floating pose copied the open book's desk transform relative to the desk viewpoint. That stored transform is not a reading pose, so away from the desk the book came out flipped and too close. The book's pose is now built from its page canvas: the page faces the camera upright with a 12-degree lean back, at a 0.6 m reading depth. The book is scaled so its mesh and page fit 74% of the view height and 62% of its width, and it is centred on the screen or on the drop point. The fly in/out, toggle, page-1 opening and storing are unchanged.
- **Dragging.** The grip (`FloatingBookGrab`) now sits on the book root, so any drag on the page that a value, page edge or list scroller does not use bubbles to it, as with the floating Case Folder. The book stays red-tinted while held, and a drop on the icon stores it.
- **Red book.** The stored-book icon art and the drag-out ghost are red, matching the desk book.
- **Cause of the paper drag lag.** Each always-composited annotated paper ran a full Doc3DLayer pass every frame: it gathered and swapped the layers of a few hundred child objects, projected every renderer's bounds, and re-rendered a full-screen capture, four times a frame. New opt-in Doc3DLayer options remove this for the papers. `PersistentRenderLayer` gives each paper its own spare layer (23-26, excluded from the main camera, so there are no swaps and no second world copy). `FootprintRect` takes the footprint from the paper window instead of a renderer scan. `RenderOnlyWhenChanged` reuses the last capture until the paper moves relative to the camera. A still paper costs no render; only the dragged one re-captures. Window dragging itself was already direct (no smoothing). Click-to-front and layering are unchanged. Other documents keep the default behaviour.


## Tax Code Book on the Case Folder system — 2026-10-20 (supersedes the page-based reading pose)

Implemented — internally verified; user Play Mode pending. User direction: reuse the Case Folder's icon and floating system.

- **Root cause of the tilted, seen-from-below book.** Both earlier poses were placed once, in world space, for the camera's rotation at the moment the book opened. Opening the pages (`TaxCodeBookUI.Show`) locks the player controls, and the camera then settles level, leaving the world-placed book tilted, from below or upside down.
- **Now.** `Level2TaxCodeBookIcon` uses CaseFolder3DView's framing: the open book's desk pose relative to `Desk/DeskViewpoint`, at its desk scale, with the printed page centred on screen (or at the drop point). The pose is held camera-local and re-applied every LateUpdate (execution order 450, before Doc3DLayer at 500), so the book stays upright in front of the player whatever the camera does. Tap to fly out and in, drag out of the icon to open at the drop point, drag the page to move it (red tint), and drop on the icon to store. The open desk book can also be dragged onto the icon: `TaxCodeBookFloatingModel3D.TryStoreAt`, then `TaxCodeBook3DDrag.ParkOpenBook`, with the same red tint while held. It always opens on page 1.


## Records desk, book colour, folder return and audit summary — 2026-10-20

Implemented — internally verified; user Play Mode pending.

- **Mark the Money! desk.** Exit Desk is hidden and inert while the practice or the challenge is under way (`WorkstationUI.ExitBlocked`, set by IncomeToolkit). It returns once the quest is done. At the records desk, Arthur's hint bubble sits at the bottom centre (`GuideCoachView.HintAtBottom`). The clipboard paper and its tabs move 0.04 m, and the highlighter 0.06 m, toward the top of the view so the bubble never covers them. Both hooks are released when the toolkit is disabled; Level 1 never sets them.
- **Tax Code Book colour.** The grey cover parts (side and spine) get the cover red once at start-up through per-slot property blocks (`TaxCodeBook3DDrag.ApplyRedCover`); white pages and coloured parts are untouched. The hold tint no longer changes colour, so touching or dragging the book never flickers grey to red.
- **Case Folder back onto the desk.** When the folder was stored back into its icon after its first placement, tapping the icon or dropping it on the office desk while seated there puts it back on the desk, as in Level 1. Only the first placement records the review step. Elsewhere the icon keeps its floating view.
- **Audit Summary.** After the Auditor's lines, Level 2 shows Office Level 1's Audit Summary card (`AuditSummaryPopupUI.ShowScreen` through `AuditorInteractable.ShowLevelSummary`). On approval it shows APPROVED with the May and year totals, returned reviews and review-cutoff timing, and continues by itself; the case reward and level completion follow when it closes. A hold or a returned review shows the same card with what to fix ("Review the case"), then returns the folder.


## Side quest finish and Auditor order — 2026-10-20

Implemented — internally verified; user Play Mode pending.

- **Exit Desk over the reward card.** After the quest the Exit Desk button sat on top of the Side Quest Complete card's Continue, so tapping Continue left the desk. Exit Desk now also stays hidden while that card or the annotated-paper collection is on screen (`HoldingExitAfterQuest`). It appears once both are gone.
- **No tiny cards in the case.** Cards kept in the toolkit case used to sit there at 45% size, visible at the start and after packing up. They now shrink to 2% as they fly in and their body is hidden while stowed; dealing them out grows them back as before.
- **Auditor before the summary.** If another conversation was still closing at hand-in, the Level 2 audit skipped the Auditor's lines and showed the result at once. It now waits up to 10 s for the dialogue system, then the Auditor explains the audit, including a line with the May and year totals, and only then does the Audit Summary appear. Arthur's separate "On time!" line is removed; the summary's "Review cutoff" row shows it.


## Leaving the records desk and the reasons page — 2026-10-20

- Continue on Side Quest Complete now collects the four annotated papers into their icon and then leaves Rosa's records desk automatically (`IncomeToolkit.LeaveDeskAfterCollection`, waiting up to 8 s for the collection). Exit Desk stays hidden until then.
- Book Section 7 is titled "Finding Reasons" (was Review Notes). The hint lines that point to it say Finding Reasons, and conclusions point to Review Conclusions.


## Book back onto the desk; summary after the dialogue closes — 2026-10-20

- Seated at the office desk, the carried Tax Code Book can return to its desk stack. Drag it out of its icon and drop it on the desk (within 18% of the screen height of its spot, or anywhere on the desk surface), or release the floating book there. It settles closed in its original spot (`TaxCodeBook3DDrag.UnparkClosedBook`) and the icon empties (`taxBookStored` false). It can be stored again by dragging it onto the icon.
- The Audit Summary (and a returned folder) now waits until the Auditor's dialogue session has ended and every dialogue box has left the screen (up to 3 s, plus 0.25 s), so it never shows while his last line is still on screen.
