# T02 — Retrieve Rosa's folder and inspect prepared work

Status: User accepted (2026-10-17). Blocked by: T01.

## Visible accomplishment

The player opens a labeled cabinet drawer, takes Rosa's physical folder, settles at the familiar desk and compares three readable initial pages.

## Work

Use existing cabinet/folder/desk geometry and interactions. Existing `FilingCabinetInteractable` archives a case: add a narrow retrieval action selected by Level2 phase without altering the archive contract. A clearly labeled market-client drawer is the target; no search puzzle. Animate drawer travel and folder lift; folder acquisition happens once, no case completion or reward.

Present cover, S1 prepared summary and N1 payment note from CONTENT using existing paper/book systems. Identity facts are prefilled and visibly ordinary. S1 subtotal 15,000 from laundry payments 12,000 and spare washing machine rental 3,000, with no blanket-washing payment row; N1 records a separate blanket-washing job paid in May, reference CM-0526, with the original record at Rosa's shop. Do not mark the missing income in red automatically. Player may inspect pages and book; unretrieved supporting documents are inaccessible.

Allow desk exit/reentry and HUD folder storage through existing ownership. Add state-based objective/guide targets so document opening works with both tap and drag, not only legacy events. Keep the folder's existing final hand-in action distinct from retrieval.

## User direction (2026-10-17)

The folder flies into the HUD folder icon. It is put on the desk from the icon, in desk view only. The pages are real case-folder pages turned like the Tax Code Book. See the PROGRESS section "T02 rework".

## Acceptance

Internal status (source checks only; each box also needs the user's Play Mode check):

- [x] Drawer opens, folder acquired once, correct objective progresses. *(Level2Regression; motion and feel pending)*
- [x] Retrieval cannot archive, audit, grant case EXP or advance case index. *(source and state tests)*
- [x] Initial paper pages show exact fixture values and provenance without an answer label. *(T01 fixtures; readability pending)*
- [x] Book and folder remain usable; supporting records cannot be accessed early. *(D1–D6 are not in the case documents)*
- [x] Leaving/reentering preserves folder and page state, without duplicate folder instances. *(existing desk folder restored, never cloned; in-game check pending)*

## Verification/handoff

Scratch compile, acquisition/progression tests and content/anchor validation. Check archive behavior remains available in its own legacy phase. User later checks drawer motion, paper legibility, reach and desk camera. Record final cabinet anchor and motion values. Next T03.


### Cabinet fit follow-up — 2026-10-18

The runtime face now covers the selected cabinet's second drawer row, with a thinner darker metal front and collider fitted to that face. This replaces the smaller protruding drawer panel in the user's screenshot. Scene and prefab originals are untouched; retrieval commit and animation remain the same.

- [ ] User checks drawer seam/label/handle fit, outline, slide/folder lift and repeat/resume empty state in Unity.


Book follow-up (2026-10-18): held movement uses the lifted plane and original grab offset without post-threshold smoothing/tilt drift; safe resting placement is stored separately. The open mesh centre aligns with the closed mesh's dropped centre at the preserved reading depth. User checks centre/edge grabs and drops across the table, near other items, and open/close/return in both levels.

### Folder button endpoint — 2026-10-19

Implemented — internally verified; user Play Mode pending. At the desk, the folder button also commits initial placement and opens the paper centrally. Outside-desk access stays gated. Dragging still places it at the drop position. Existing entrance timing/easing/rotation/scale are preserved. Guide wording now supports tapping.

- [ ] User checks initial button placement and later reopen are centred, entrance animation remains the same, and drag placement still follows the chosen spot.



### Consistency follow-up (2026-10-19)

Implemented — internally verified; user Play Mode pending. See SPEC and PROGRESS, “Level 1 consistency follow-up”, for the current six-page folder, shared clock/splash, receipt papers, annotated HUD collection and direct-Editor checkpoint policy. Earlier implementation descriptions above remain historical.
