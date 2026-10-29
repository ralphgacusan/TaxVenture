# T27 — Fixed document designs and incremental adoption roadmap

Status: Planned — deferred reference/backlog; implement only when an authored level needs a type. Blocked by: None.

## User scheduling revision — 2026-10-28

The user replaced the all-at-once library build with implementation as needed by each level. This supersedes the earlier requirement to complete eight physical types, two records per type, an isolated all-eight viewer and a Rosa adapter map before T18. T27 is retained so the fixed plan is not lost; it is not the next standalone implementation ticket and does not gate Delivery A or Delivery B. T17's internal gate is complete, with user manual acceptance pending. Next implementation is [T18](T18-level2-campaign-and-inventory.md), then T32, T19, T20, T33 and T21 under the Level 2 continuation amendment.

## Goal and visible accomplishment

Preserve the [supporting document standard](../../SupportingDocuments.md) as the single fixed design reference. All eight categories, shapes, information zones, fields, restrained colors, readable printing and physical interactions stay planned. Build a document presentation only when a real authored case requires it. A later client reuses the same type layout with changed facts; a new level does not require a new type or an advance library build.

## Work and ownership

| Type | Adoption owner / timing |
|---|---|
| 1 — Identity document | Existing Level 1 implementation; reuse it. Passport variant waits for a case requiring it. |
| 2 — Client background sheet | Existing Level 1 implementation; reuse it. |
| 3 — Compensation certificate (2316) | Deferred until a later authored case needs it; absent from Level 1 and not required by Rosa. |
| 4 — Income record / collection sheet | T18, because Rosa needs it. |
| 5 — Invoice / payment record | T18, including Rosa's three distinct transaction records. |
| 6 — Bank statement | T18, for Rosa's bank extract. |
| 7 — Contract / agreement | T18, for Rosa's loan agreement; other variants wait until needed. |
| 8 — Creditable withholding certificate (2307) | Deferred until an authored credit case needs it. |

Each adopting level ticket implements its required physical presentations, canonical source bindings and bounded verification together with the real player flow. Record source IDs, assets, layout decisions, verification evidence and remaining deferred types in PROGRESS. T18 owns the actual D1–D6 mapping and verifies all six separate papers across Types 4–7; do not merge records, change evidence values or rebuild Rosa's packet into Level 1's two-page shell.

Reuse existing physical viewers, extraction/return, safe bounds, focus and same-owner cleanup. Preserve the fixed type layout, readable long values, neutral identity cues and factual paid/unpaid labels. No new framework, packages or speculative models are required. Missing optional facts use a dash or “Not applicable”; table overflow uses another face/page of the same type. Unknown sources must not display another client's record. Missing mappings retain readable fallback access with diagnostics. Interrupted inspection must clean up owned instances without losing papers or changing campaign state.

## Internal checks — AI source/build/regressions only

- [ ] For each adopted type, its owning level ticket verifies stable source identity, relevant long/missing values, overflow and readable fallback behavior.
- [ ] Required physical presentations use the production viewer; repeated open/close, interruption and cleanup preserve source ownership and existing Level 1 behavior.
- [ ] Development fixtures are limited to the current ticket's types and meaningful edge cases, isolated from live saves, rewards and ownership. No two-per-type or all-eight fixture gate remains.
- [ ] Owning tickets record implemented versus deferred types honestly; unneeded types do not block delivery or become claimed implementation.

## USER manual acceptance

- [ ] Inspect only the types introduced by the current level: readable phone framing, recognizable shape, depth, shadows and fixed layout.
- [ ] Check handling, comparison where supported, return, touch and sound in the real level flow; remaining unused types stay planned.

## Non-goals and handoff

No standalone eight-type build, public gallery, speculative future-level assets, extra campaign cases, tax-law expansion or new launch permission. Types 3 and 8 and unused variants remain intentionally deferred, not completed or cancelled. Existing implementation/internal status and pending manual acceptance are unchanged. Next: T18 builds Rosa's required subset under this standard; T21 verifies that integrated subset before enabling Level 2. When a future case needs a deferred type, add its implementation and acceptance to that case's ticket and update this roadmap.
