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
| L2-30 | The finding boxes rely on tap-to-pick on another page then tap-to-place (or drag on the same page). Selection is held across page turns. | User checks the feel; Arthur explains it |
| L2-31 | The desk stamp set is off in Level 1's current scene (its flow no longer stamps), so Level 2 switches it on at runtime. The stamp set's own prompt still says "Click to stamp" (Level 1 text). | Fixed for Level 2 (T03). The prompt wording is Level 1-owned |
| L2-32 | Desk stamping float spots, hover height and press depth come from the stamps' measured bounds and the camera view (no visual pass by the AI). | User checks the feel; tune the `Level2DeskStamps` / `Level2StampDrag` constants |
| L2-33 | Pre-existing `GetComponent<T>() ?? AddComponent<T>()` in `HudRewardScreen.cs` (lines ~361, ~456) and `OfficeHudRuntime.cs` (~209), from other initiatives. In the Editor this skips the AddComponent when the component is missing. Not changed by Level 2. | Owner of the HUD initiative: replace with explicit null checks |
