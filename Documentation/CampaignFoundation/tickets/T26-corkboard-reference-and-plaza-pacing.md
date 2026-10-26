# T26 — Make the corkboard follow naturally from the client rounds

Status: Planned. Blocked by: T25.

## Goal and visible accomplishment

Elijah arrives at the existing four-profile board after inspecting Arthur's reviewed example and meeting four clients in any order. Arthur gives a reminder instead of describing each answer pattern. Short category references remain available while sorting. The two new document moments replace existing explanation and fit the first morning without adding another activity.

## Player flow and mechanics

Keep the four collected profile cards and Arthur's deliberate later meeting. This is a follow-up with the same Arthur already met after the Supply Counter, not another introduction. His board introduction is at most three short lines: acknowledge the records, remind Elijah to consider citizenship and living situation, and explain that the full board will be checked. Remove the four near-answer biographies from the current introduction. Preserve the short heads-up before the fourth placement and existing correction guidance.

Add a small readable reference strip or corner information control that opens the four neutral category meanings. Keep formal board category names available. Describe each category in one concise sentence based only on the board's existing authored distinctions. This reference explains vocabulary; it never names a client, references an answer-key assignment, singles out a destination for a held profile, or changes with a draft placement. It is available before and after the first check and closes without moving cards.

Keep Arthur's Reviewed File available through the existing Bag while sorting. Its distinct former client is an example, never a solved profile for one of the four board clients. The four clients give only their existing profile cards; remove Jhun-jhun-specific supporting-record access, first-client priority and bench guidance. Do not fabricate papers for the others. Preserve binder history even when the post-quest active display narrows to Marie. Optional reference reading cannot move/check cards or modify Marie's case.

## Tutorial and objective consolidation

Audit the full active `guide.json` route, not just two snippets. Keep necessary move/look, Notes, Supply Counter and pickup gestures. After the counter point to Arthur's start marker, complete his reviewed-file route, then select any of the four clients. Reuse the folder/page/record gestures introduced there when Marie's case opens; do not explain them twice. Merge or remove the explanatory pages displaced by the T30/T31 follow-up and T25. Present help at the moment of use with one target at a time. No additional welcome panel, reward card, chest, navigation detour, or compulsory fourth-document exercise.

Player-facing prose uses job language: records, client, file, organize, compare, check, and next job. Do not introduce “teaching”, “learning”, “lesson”, “quiz”, “test”, or “assessment” in new or rewritten guidance. Existing required tool names are not globally renamed. Target approximately 2–3 minutes total for both new document moments at an ordinary reading pace; this is a user-playtest hypothesis, not a countdown or forced dwell. Compare replaced and added pages/actions in the handoff.

## Implementation and failure boundaries

Inspect `CorkboardTeaching`, `CorkboardSideQuestManager`, `ProfileDragCard`, slot labels, profile viewer/bag adapters, `town.json`, `objectives.json`, `guide.json`, their fallback copies, and field resume. Use the existing current objective progression and input owner. A category-reference overlay cannot swallow a pending drag, trigger an automatic check by closing, or expose a world tap behind its close button.

Preserve neutral draft feedback, the 0.45 s full-board check, frozen first result, hints after checking, and required corrections. No new lever, stamp, manual check command, shortcut completion or revised accuracy thresholds. On resume, restore the draft and reference availability without replaying introductory guidance or a check sound.

## Internal checks — AI source/build/regressions only

- [ ] Reference definitions are static, client-independent and consistent with current board distinctions; no pre-check correctness leak.
- [ ] Neutral draft, auto-check/frozen tier, corrections, binder history and cancelled reference-view behavior remain covered.
- [ ] Active/fallback content agrees; obsolete replacement pages do not still run; affected guide/board/build checks pass.

## USER manual acceptance

- [ ] The route feels like gathering records then organizing them; Arthur does not explain every client again.
- [ ] References and papers are readable without hiding board destinations; reference use does not move/check cards.
- [ ] Record actual combined document-moment time and note any repetitive or intrusive guidance.

## Non-goals and handoff

No board redesign, new challenge, new classification law, shop reorder, all-client evidence routine, or result economy change. Record before/after page counts and route milestones in PROGRESS. Next: [T27](T27-eight-document-template-library.md).
