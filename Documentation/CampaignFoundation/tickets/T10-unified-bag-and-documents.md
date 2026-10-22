# T10 — Open profiles and case papers through a unified bag

Status: Planned. Blocked by: [T05 — Level 1 case and clock resume](T05-level1-case-and-clock-resume.md).

References: [Campaign spec](../SPEC.md), [world and feedback](../WORLD_AND_FEEDBACK.md), [verification plan](../VERIFICATION.md), [progress log](../PROGRESS.md).

## Goal and before/after flow

Give the player one reliable place to review collected profiles, case files, tools/supplies, and keepsakes without making it a second source of case truth. Before this ticket, records are spread across profile HUDs, the physical case folder, desk papers, and small runtime lists. After it, the player opens a four-section Bag, chooses a collected item, and views its actual document or model through the existing viewer. A small evidence-journal entry lists learned neutral story facts and links to their source records; it is a reading aid, not a future evidence-board game.

## Ownership and mechanics

The inspected Level 1 document seam is CaseData and CaseDefinition for authoritative case facts, TaxpayerProfileCollectionHUD / TaxpayerProfileData for collected profiles, and DocumentViewerManager / DocumentViewerWindow for existing viewer ownership. T05 adds saved Level 1 revision/working-state identity; do not bind the bag to the Level 2 CaseReviewState or copy answer-key fields. Extend the v5 campaign inventory record with stable item IDs, quantities, category, original/working document identity, revision, acquisition source, and optional neutral story-fact links. Do not serialize scene references. Item definitions/catalogues belong in the planned campaign resource folder. Inventory commands remain in the campaign domain and observers only render snapshots.

The four tabs are Profiles, Case Files, Tools & Supplies, and Keepsakes. Show available counts and a clear empty state. Required case documents are unlimited campaign records and can never be sold, discarded, or hidden; preserve original and working copies as distinct IDs and revisions. A profile entry opens the same profile content/model the player collected. A case entry opens its actual viewer with source, revision, and annotations intact. Tools/supplies show quantity and a plain rule description. Keepsakes show their neutral character lore and possible display eligibility. The journal shows only learned neutral facts and explicit source links, with no hidden answer, legal conclusion, Level 5 evidence sorting, or new fact created by opening the UI.

During normal exploration, the player may inspect a document anywhere. While the viewer is open, movement and world taps are locked cleanly; closing returns focus to the prior safe state. Formal calculation, filing, stamping, and finding submission remain at the workstation. Viewing through the bag does not pause an active review clock; existing compulsory guide/settings/dialogue pauses stay as they are. Shop catalogue viewing and owned shop upgrades are outside this ticket. Bookmark purchase/pinning is deferred to the later bookmark ticket.

Construct a small bag/binder shell in the game's existing paper language; no existing Bag asset is assumed. It lifts and fans out four divider tabs, then the selected case pack opens as a paper stack in the existing floating document viewer. Back gathers the sheets and releases movement/document ownership exactly once. Thumbnails match their physical source. If an item is unavailable or stale, show a readable message and retain bag access. Implement the shell lift, tab fan and return with bounded transform motion; reuse existing paper turn/selection sounds. No newly imported animation clips are required. Use large targets, icon-plus-text labels and a visible Back action.

## Non-goals

Do not create a generic RPG scripting system, durability, crafting, trading, selling, required-evidence acquisition, bookmark purchase, shop menu, new legal rules, answer key, multiple item copies that disagree with the case, or Level 5 evidence-board puzzle. Do not pause ordinary review time or make document inspection a formal case action.

## Acceptance

Internal checks — implementation agent records evidence in ../PROGRESS.md:

- [ ] Bag categories resolve only to owned stable IDs and quantities in the same campaign save.
- [ ] Required documents cannot be sold/discarded and original/working revisions stay distinct.
- [ ] Opening a document anywhere shows the same model/content/annotations as its physical source.
- [ ] Movement/world taps are locked while inspecting and restored safely on close; formal actions remain workstation-only.
- [ ] Opening the bag/journal grants no rewards, trust, facts, ownership, or clock pause.
- [ ] Missing/stale document recovery is readable and does not lose the current checkpoint or trap the player.
- [ ] Profile, case, supply, keepsake, and journal states round-trip through save/reload.

USER manual acceptance — only the user performs these checks:

- [ ] Open every bag section, inspect one real document, close it, and confirm control returns to the prior exploration state.
- [ ] Verify original and working paper versions remain distinguishable, including annotations and revisions.
- [ ] Inspect during a live review and confirm the clock continues; try a formal filing action away from the workstation and confirm it is unavailable.
- [ ] Confirm a missing/stale record message is understandable and can be dismissed without losing progress.

## Handoff

Record the viewer adapters, item IDs, and movement/clock ownership in ../PROGRESS.md. T10 completes the document and bag slice; future world-collectible and bookmark tickets may add owned objects through the same catalogue and command seam.
