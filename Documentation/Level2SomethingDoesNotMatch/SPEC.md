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
- **T04 stall.** Rosa and the Building_4 store are runtime instances of the originals (Resources `scene_assets`), placed by measured free space beside the plaza. The records arrive as folder pages D1–D6, and the market desk uses the normal desk view.
- **Book leaves.** Level 2 leaves live in `Levels/Level_02/book.json` (`LevelBookLeaves`), shown after the shared book asset. The shared `TaxCodeBookData_Main.asset` is not edited, so Level 1's book is unchanged.

## Goal and scope

Teach recognition of income inclusions, use of supporting records, and justified readiness recommendations. Main question: **Can I rely on this income summary?** Level 1 taught taxpayer identification; Level 2 adds evidence-backed review of prepared work. One main Rosa case, no Ben case. Target duration is a hypothesis: 15–20 minutes, revised from user playtesting, not guaranteed by source checks.

Rosa operates an alterations stall at the market. This retains the market-vendor setting while using service receipts rather than introducing merchandise cost-of-sales calculations. Her small ancillary commission and rental receipts are explicit, ordinary examples. Detailed deductions, rates, final-tax computations, corporations and tax elections remain later-level material.

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
| Preliminary review | Compare initial pages; link referral note to summary; record omission concern | Evidence-linked finding entered; tutorial teaches this once |
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

Tap a fixed readable row to select it, choose `Add finding`, select related record(s), choose a short concern/reason. The first office finding is guided; final review is independent. Findings are paper notes on a dedicated folder page, not a new HUD investigation application.

Each finding has a stable ID, related entry/document IDs, player's conclusion, evidence references, and a short reason ID. Store observations separately from the player's conclusion and separately from answer keys. Status labels describe the player's work: `Needs checking`, `Corrected`, `Explained`; they do not certify truth. The Auditor evaluates support. False findings are graded too.

Two assessable concerns only: omitted commission and apparent service-total mismatch. Loan treatment belongs to income annotation and is retained in office treatment review, not a third mandatory finding. Player may revise a loan annotation before final audit without changing their frozen quest grade.

## Side quest: assessment without answering the case

One unrelated compensation tutorial; four Rosa challenge entries. Each entry has three scored responses: (1) source/type note, (2) treatment note, (3) matching evidence set. Highlighting the relevant line is required interaction but unscored motor practice. Type and treatment remain distinct response slots. Evidence choices can include a two-document bundle where needed; do not increase the count beyond 12.

Allow revising any response before `Check annotations`. The first full submission freezes all 12 answers and their correctness. Show entry-level feedback and concise rule explanations; correction attempts do not lower that score further. Gold/Silver/Bronze is based on the frozen accuracy. All errors must be corrected to complete the educational quest; hints can grow specific, with no additional reward penalty. Correcting does not turn Bronze into Gold.

Quest feedback may explain that a paid commission is income or that a repayable loan is not earned income. It must not say `the summary omitted E2`, `add 5,000`, `the correct total is 20,000`, `the mismatch is cash versus bank`, or `stamp Ready`. Summary pages and finding answers are not displayed in the quest. The final spread is a list of individually annotated records, not a reconciled total. A Gold player still must identify application to the case, transfer values, link records and justify the stamp.

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
