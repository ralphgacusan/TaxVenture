# T08 — Inspect the 2307 credit and compute Bea’s remaining balance

Status: **Implemented, internally verified; user manual pending**. Blocked by: T07.

Priority: P1. Phase: C — Credit and balance. Estimated scope: M; target one fresh agent context. If unexpected shared refactoring is required, document and split it before broadening this ticket.

## Goal and visible accomplishment

Players acquire a real Type 8 source, distinguish credit from expense and attach the remaining 50,000 balance without duplicating certificate income.

This is local Level3 numbering. Read [AGENTS](../AGENTS.md), [SPEC](../SPEC.md), [CONTENT](../CONTENT.md), [source map](../REUSE_AND_ARCHITECTURE.md), [presentation](../WORLD_AND_FEEDBACK.md), [rewards](../REWARDS_AND_VALIDATION.md), [verification](../VERIFICATION.md) and this ticket before execution. Implemented 31 October 2026 on an implementation instruction; evidence in [PROGRESS](../PROGRESS.md), limitations in [KNOWN_ISSUES](../KNOWN_ISSUES.md).

## Existing systems to reuse

Verified existing paths relative to repository root; recheck live behavior rather than rely on stale comments:

- `Assets/Scripts/SideQuest1/SupportingDocumentFace.cs`
- `Assets/Scripts/SideQuest1/ClientRecordPaper.cs`
- `Assets/Scripts/UI/SupportingDocumentsPageLink.cs`
- `Assets/Scripts/UI/DialogueSession.cs`
- `Assets/Scripts/UI/TaxCodeBookUI.cs`

New L3 components/resources mentioned below are proposed outputs, not verified existing APIs. Prefer existing owners/adapters and level configuration; do not copy global frameworks.

## Work and interaction mechanics

1. Verify current Type 8 implementation, then add only the missing 2307 face/body/source mapping needed by B-WHT and later C-WHT. Follow fixed layout: blue header, payer/payee/period, income payment/withheld amounts/reference, clear purpose; physical paper thickness and source attachment preserved.
2. Bea’s office credit-step conversation hands over B-WHT only at valid phase, with actual acquisition saved before source is viewable. Read250,000 already included in R2 and12,500 credit. No additional250,000 revenue strip or withholding-rate exercise.
3. Implement CONTENT’s credit-versus-expense explanation choices through existing UI: wrong answer opens generic book help, retains draft, no trust/ethics penalty; inspect returns to same context. A correct dialogue answer never auto-fills credit.
4. Player selects certificate withholding, links Credits, computes62,500−12,500=50,000, and attaches Remaining Payable tape. Permit content mistakes for review; currency calculations use T01 exact policy, no silent negative clamp. Credit-only edits dirty balance/stamp, preserving base/tax tapes.
5. Save acquisition, explanation state, source references, calculation revision and tapes under Bea subrecord. Same presentation supports company certificate later; Type 3 salary certificate or all-eight gallery is not a prerequisite. No approval/reward until actual T09 review.

## Assets, motion and feedback

New Type 8 presentation only if absent, reusing standard body/face builders. Existing client/source handover/printing and generic credit reference; no realistic official form reproduction or new portrait.

Follow WORLD_AND_FEEDBACK and fixed document types. Bind every new world object to actual 3D body, collider/target, correct source/record identity and existing camera/input/audio ownership. Record resolved anchors/clip names and any unsupported asset assumption. No user scene-assembly chores or claims of rendered acceptance from declarations.

## Integration requirements

Source identity250,000 subset of400,000 and same tax year must be explicit. Keep reference/client documents read-only. T09 receives current supported credit/balance facts and dependency status.

Preserve current L1/L2 scenes, source prefabs/GUIDs, signed save owner, mode guards, input/FSM/dialogue/audio frameworks and Android controls. Normal L3 Career remains unavailable until T18. No L4 filing, ending, stand, drawer or global archive feature is added.

## Persistence and failure rules

Use the existing candidate→save→publish command boundary and stable chapter/case/action IDs. Validate actual prerequisites/revision/source before committing. Failed write leaves prior state, source/copy and retry available, with no success effect. Quiet restore does not raise acquisition/reward/choice/consumption events. Back, repeated input, disable and app focus interruption settle pointer state and release only owned holds. Story/replay/practice/Editor distinctions protect live campaign data.

## Internal checks — AI source/build/regressions only

- [x] B-WHT actual admission/acquisition/view/return/Bag link and Type 8 field mapping are correct and recoverable. (Handed over only at the credit step, saved before it shows, one action id, a repeat or resume is quiet; prints as Type 8 with payer, payee, period, reference, purpose, the income payment with where it is counted and the tax withheld band; listed in the Case Folder by source id; never on her table; the company's packet prints with the same builder.)
- [x] Credit entry stays separate from expense/revenue;250,000 not duplicated; result50,000 and current source/year required. (Tax credits takes only the acquired certificate's printed amounts; the 250,000 is never a total choice, a sheet source or a strip; 62,500 − 12,500 = 50,000 supported; another payee or a period outside the year never supports.)
- [x] Wrong explanations/missing source/manual value/cross-case certificate cannot grant approval or penalize trust. (A wrong answer saves nothing and opens Section 9; no linked credit, a typed balance or another case's certificate is refused or kept unsupported; nothing in the credit step touches trust, ethics, coins or a grade; there is no approval before T09.)
- [x] Credit-only edit preserves unrelated calculations; failure/reload does not duplicate paper or reset draft. (A new credit marks only the payable stale and moves the revision; signed round trip; a T07 save resumes with the certificate still due; an injected write failure keeps disk and live state and the handover can be retried once.)
- [x] Run targeted behavioral/serialization/failure checks and affected existing regressions named in VERIFICATION for the actual changes. Build changed production paths before tests that load their DLL. Record exact commands/results/warnings; no Unity/editor launch or assumed pass count. (Commands and counts in PROGRESS; suites built out of tree; mutation runs recorded.)

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] User checks Type 8 recognizable layout, labels/amounts/period readability, depth/extraction, accessible dialogue and source return on phone.
- [ ] Inspect applicable actual imported references, 3D bodies, paper text, targets, motion, camera/control recovery and Android layout. Manual acceptance remains pending until the user reports it; static tests are not a substitute.

## Handoff and completion definition

Complete but unreviewed Bea computation with supported certificate and payable tape. Next T09.

Direct dependents: T09. Record actual changed source/content/assets, internal evidence, preserved baseline limitations and remaining user checks in [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md). Mark implemented/internal-verified only after real entry points and checks work; Git operations and user acceptance remain user-owned. No commits, scene edits or gameplay implementation are performed by authoring this ticket.
