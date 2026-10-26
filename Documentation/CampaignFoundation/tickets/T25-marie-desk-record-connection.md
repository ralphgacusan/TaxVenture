# T25 — Open Marie's two-page case and supporting records

Status: Planned. Blocked by: T31.

## Goal and visible accomplishment

Marie uses the same familiar two-page folder shell as Arthur's reviewed example, but her page-one case entries remain the player's responsibility. Her actual identity, employment background and compensation certificate are attached on page two and can be extracted, compared and returned with T31's physical 3D controls. No separate desk sleeve or new inspection framework.

## Player flow and mechanics

1. Preserve reception, case handover, conference-room visits and workstation entry. Opening Marie's folder shows **Client Information**: existing identifying details and the existing three editable entries, initially unanswered where the current case requires. Do not prefill them from Arthur's sample or change answer keys.
2. Page two is **Supporting Records**, containing her existing Valid Government ID (Type 1), Employment Records presented as Client Background Sheet (Type 2), and compensation certificate 2316 (Type 3). Keep all canonical facts and document IDs; no second employment source or invented fourth document. Use clear attachments/titles and a compact three-item arrangement that is recognizable at rest, with full readable detail on extraction.
3. For the brief first-desk connection, direct attention to the background and 2316. The ID remains available without a third compulsory inspection. Extract either work paper, then the other, using T31's shared tap/drag, front focus, safe screen bounds, return and optional two-second grouping. No paid comparison stand or bookmark requirement.
4. Arthur gives at most two short contextual lines, such as “These are Marie's work records. Check her job details against the payments her employer recorded, then use the book for her file.” Never speak her final entries. Keep his bubble clear of papers and let the player read without a timer.
5. Return the work papers and continue the existing book-to-field choice/transfer. This replaces the duplicated folder explanation; do not repeat Arthur's opening file routine, require advanced stacking, or add another result card.

The two pages are the visible Level 1 case shell. Do not globally trim other levels to two pages. Inspect current CaseFolderUI.BuildPages: do not assume it still contains six pages because of old inspector labels or a hardcoded SupportingDocumentsPageLink index. Resolve supporting pages by stable page identity/role, scoped to each folder. If any live case-only action currently resides elsewhere, preserve its legitimate path without changing approvals/forms/calculations. The file must still store, hand in, return after a failed review and finish normally.

## Source, identity and appearance

Inspect case_001.json, DocumentDataProvider, SupportingDocumentsPageLink, CaseFolderUI/CaseFolder3DView, T30's per-instance read-only source, T31's attachment/floating controls, guide.json, ClientConsultation, case checkpoint, bag and input locks. Reuse source values and semantic IDs. Page decorators, detached records and cached pages belong to their own file; do not use a global sample/current-case swap. Cross-folder drops or sample-to-Marie value transfers are refused. Existing ordinary Tax Code Book transfers into Marie remain valid.

Marie retains the manila active-case appearance and existing icons/ghosts. Arthur retains teal REVIEWED across every form. Both use the same 3D folder opening/page-turn controls, physical holders, depth, shadows and paper motion. Model a cream background sheet and stylized 2316 with readable printing and restrained blue header. Preserve all compensation/withholding amounts; this is not a new computation objective.

## Failure, timing and resume

Persist the first-desk preparation only through the existing case checkpoint boundary after the actual two work records have been presented settled and returned. Guide-page advancement cannot substitute. Interrupted views resume attached, with unfinished preparation available. A compatible case with existing entries, audit work or approval cannot be relocked or reset by the new presentation. Failed persistence remains retryable; reading/returning changes no answers, rewards, tier or deadline.

Use the existing bounded compulsory-guide pause only during the first guided presentation. Independent rereading of either folder follows current active review time. No moved deadline trigger, free extension or perpetual pause. Closing/storing gathers detached papers once; future audit correction can reopen them.

## Internal checks — AI source/build/regressions only

- [ ] Marie has exactly two visible Level 1 pages with all three existing source records attached/accessed; no hardcoded page-six dependency remains on this route.
- [ ] Only work/background and 2316 are compulsory views; the same per-folder extraction/return controls work for all three.
- [ ] Sample/Marie source isolation, rejected cross-folder transfers, legitimate book transfers, close/resume and failed save have coverage.
- [ ] Existing required entries, audit/correction, bag ownership, clock and finale checks/build pass; Level 2 retains its current page structure.

## USER manual acceptance

- [ ] Recognize the same folder interaction as Arthur's, with two pages and a distinct manila active case.
- [ ] Extract ID/background/2316, compare the two work papers, focus, group optionally, return and close; readable 3D props with no clipping.
- [ ] Arthur is brief and does not supply answers. Complete an imperfect then corrected case and client handover; reopen both folders without mixed data.

## Non-goals and handoff

No separate desk sleeve, new required tax field, third mandatory work-paper inspection, new legal rule, reward, future case or finale replacement. Record actual page IDs, source bindings, source-removal compatibility and internal versus USER findings in PROGRESS. Next: [T26](T26-corkboard-reference-and-plaza-pacing.md).
