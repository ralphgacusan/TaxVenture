# T20 — Install a functional comparison stand in Rosa's office

Status: Planned. Blocked by: T18.

## Goal

Add optional comparison furniture that presents real owned case records.

## Visible accomplishment

Before: players compare through the current workstation only and own no comparison stand. After: once Rosa's Level 2 review is ready, the player may spend 120 coins on a real comparison stand. It is furnished in the office, can be moved between the authored office anchors, holds two actual pinned source/working papers and opens their existing viewers side-by-side. The workstation's ordinary free comparison remains available whether the stand is locked, unowned, moved or unused.

## Work

Unlock the catalogue item only when Level 2 is ready in the integrated campaign. Its world model is a waist-high dark-wood reading stand with a brass crossbar, two labelled paper rails (“SOURCE” and “WORKING COPY”), visible spring clips/pushpins and a shallow drawer for the player's record choice. It must be reachable by the player and have properly sized colliders; this is a usable furniture feature rather than an icon or HUD substitute. On interaction, the player selects one existing source identity and one existing working-copy identity from records they own or are allowed to inspect in the case. The stand loads the actual corresponding paper presentations into its left/right rails. It shows the original and working revision clearly, retains their distinct document IDs, and lets the player flip/read them. The stand adds no new comparison result, answer, tax treatment or evidence; formal comparison results and case actions still use the current approved workstation commands.

Allow moving the purchased stand only among a small set of authored office anchors (for example, desk-side, window-side and wall-side), using the current furniture placement interaction. The stand travels as a complete furniture object; its pinned papers remain attached or safely return to the drawer before movement. Save the selected anchor and displayed source IDs. Do not allow free physics pushing into a wall or across the level. Use T13's preview listing only after the Level 2 ready prerequisite; selecting the teaser before then shows the prerequisite and cannot reach a purchase confirmation.

### Source starting points

- **Existing:** Level 2's actual paper/viewer and source-transfer paths (`Level2RecordPaper`, `Level2AnnotatedPapers`, workstation review controls), campaign inventory/shop state from T18/T13, and the existing office scene/physical props.
- **Proposed:** comparison-stand catalogue data, owned furniture placement and a presenter over existing paper-view commands. No present comparison-stand component or API is assumed.

Upon opening, the two brass rails swing outward over 0.35 s, a paper selected from the drawer rises and clips to the left rail, and the second paper clips to the right rail; each clip settles in 0.12 s. Tapping either paper brings that real document face-on; closing returns it to its own rail. Moving to another anchor retracts the rails, retains the papers in their slots, then glides the stand over about 0.55 s and settles with a small wood bounce. Provide labels and icons in addition to color. Use one quiet clip cue for each paper actually placed and a soft furniture-settle cue only after save; no sound on failed move/purchase. Respect user effects settings and do not play per frame.

## Persistence and failure rules

Purchase checks the ready prerequisite, 120-coin balance, story mode and unique ownership, then commits debit and ownership together. Placement is saved only at a valid anchor. If purchase or move persistence fails, retain prior wallet, ownership, anchor and visible placement; allow retry without duplicate debit. The comparison stand is furniture and never enters the bag as a consumable. Owned furniture cannot be sold or repeatedly rewarded. Selecting papers creates no copy of tax values and does not mutate the review; source-reading follows the normal active clock policy, and actual comparison/submission still follows existing rules. Free workstation comparison remains available at all times.

## Internal checks — AI source/build/regressions only

- [ ] Catalogue readiness/price and campaign purchase use T13's same shop command; tests cover duplicate, insufficient funds, lock, save failure, anchor persistence and story-mode isolation.
- [ ] Paper selection resolves real distinct IDs/revisions, preserves the free workstation route and emits no approval/finding event from viewing or moving furniture.
- [ ] Static scene checks validate model, colliders, all anchors, attached paper bindings and deterministic restore; compile and affected inventory/document/Level 2 suites pass.

## USER manual acceptance — Unity/device/visual/feel/listen checks

- [ ] Before ready Level 2, inspect the teaser and confirm it is visibly locked; after readiness, buy once and compare two actual Rosa records.
- [ ] Reposition the stand among all authored anchors, save/reload, and compare at the free workstation without buying or opening the stand.
- [ ] Check reachable height, paper clipping, original/working readability, movement, camera framing, touch response, clock behavior and sound on device. No manual scene construction is part of user acceptance.

## Handoff

Next ticket: [T21](T21-delivery-b-verification.md) verifies both Delivery B features before enabling the new-campaign route.

## Non-goals and references

No change to tax rules, paid correct answers, new case documents, exclusive comparison benefits, L3 computer, or unrestricted physics furniture placement. See [SPEC](../SPEC.md), [WORLD_AND_FEEDBACK](../WORLD_AND_FEEDBACK.md), [VERIFICATION](../VERIFICATION.md), and [PROGRESS](../PROGRESS.md).
