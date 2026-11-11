# T01 — Finish required paperwork with a full base Bag

12 November camera amendment: first-person exploration is the release baseline; third-person/jump adoption is tentative T22 only. This ticket's inventory or appearance/contact/device scope stays intact. Pilot Explore/Orbit inspection cameras remain available for asset review.

## Status and readiness

**Implemented, internally verified; user manual pending** (11 November 2026, AI implementation session). Unity import, Play Mode, rendered/touch/listening and device acceptance (G1, G8) have not been performed and remain the user's. Evidence and exact paths: [Implementation record](#implementation-record--11-november-2026) below and [PROGRESS](../PROGRESS.md).

## Priority and relative size/risk

P0. Size: **M**, bounded to one fresh implementation context. High: atomic acquisition, attachment and retained ownership. If an observed prerequisite makes this bound false, record the exact bounded substep/blocker; do not absorb unrelated systems.

## Blocked by

None (ticket prerequisites). External decisions/assets/evidence: None to begin. User native acceptance remains required (G1).

## Direct dependents

[T08](T08-rosa-laundry-retrieval-and-kanor-letter.md), [T13](T13-bea-calculation-and-arthur-desk-examples.md)

## Goal and visible accomplishment

A player with every retained item and base equipment can deliberately acquire, read and use Bea's certificate without buying capacity or discarding a possession.

## Current behavior and evidence

L3-R106 documents ten simultaneous items for nine base slots. Existing attachment commands remove separate paper slots, but ordinary acquisition can refuse before attachment. This gap is established in CURRENT_STATE; it is not a fresh Unity reproduction. Read [CURRENT_STATE](../CURRENT_STATE.md), [AUDIT_NOTES](../AUDIT_NOTES.md) and the actual reuse paths below. Existing internal evidence is historical; no static declaration establishes Unity behavior.

## Existing systems/assets to reuse

- [CampaignInventory.cs](../../../Assets/Scripts/RewardsSystem/CampaignInventory.cs)
- [HudHotbar.cs](../../../Assets/Scripts/UI/HUD/HudHotbar.cs)
- [Level3CertificateRules.cs](../../../Assets/Scripts/Level3/Level3CertificateRules.cs)
- [Level3Checkpointer.cs](../../../Assets/Scripts/Level3/Level3Checkpointer.cs)

## Scope and explicit exclusions

Implement the existing-folder acquisition alternative and its actual paper/source presentation. Test an extra retained letter-slot fixture. Exclude capacity-price changes, evictions, forced voucher consumption, new inventory/archive/reward systems and automatic financial value entry. Follow [SPEC](../SPEC.md), [approved decisions](../DECISIONS.md) and [asset/contact plan](../ASSET_AND_ANIMATION_PLAN.md); no production execution, Git, Unity launch, paid generation/purchase or actual-save reset is authorized merely by this ticket.

## Player journey and entry/exit

L3-D; full-loadout regression across L1–3. After Bea legitimately presents the certificate, a full-Bag player deliberately chooses Clip to Bea's folder; the paper settles into the existing supporting slot and opens for reading/source selection. Roomy-Bag pickup remains. Exit with the genuine acquired/attached source, not a silently filled credit.

## Mechanics and domain logic

Validate current mode/case/source/available certificate/folder eligibility; combine acquisition and attachment in the existing signed candidate. If no legitimate folder destination is available, keep paper retrievable with a concise retry. Detach with no ordinary space refuses and keeps attached original readable. Duplicate callbacks cannot grant twice.

| Situation | Player-visible response | State/commit | Recovery |
|---|---|---|---|
| Valid deliberate action | Neutral handling, then actual result | Existing owner validates candidate and persists before publishing | Settle to committed state |
| Save fails or command refused | Concise actionable retry; no success effect | Previous durable work/ownership unchanged | Source/task stays reachable |
| Duplicate/stale callback | Existing settled result, no second grant | Current action/case/source/revision guards | Ignore stale cosmetic completion |
| Cancel/interrupt/reload | Release only owned input; retain saved work | Never complete from voice/gesture/arrival | Restore canonical state, not animation frame |

## Visual/environment/interaction specification

Use the actual certificate body/face and current supporting-page holder, highlight and pickup/clip cue; no invisible grant or fake empty tile. Expose the deliberate alternative at the certificate, not a remote menu reward.

## Animation and physical contact

Reach/lift/clip/settle follow the real acquired/attached result; interrupted cosmetics settle to published state. Final articulated hand treatment uses the approved pilot when adopted; current paper motion remains reviewable immediately.

## Audio/dialogue/voice

One neutral paper/clip cue after success; concise retry on failed save. No new dialogue, voice take or correctness sound required.

## Camera/mobile/accessibility

Existing certificate reader/folder close view; preserve actual return owner and tap/drag paths, safe-area targets and closing-tap suppression.

## Data/interface/persistence

Only minimum extension to existing certificate/inventory command capability. Preserve source IDs, exact value provenance, items/quantities/slots/pins/capacities and current story-mode guards. Use verified owners; introduce no speculative public class/API to match this prose.

## Failure/cancellation/interruption/reload

Failed write changes neither inventory nor acquired source. Cancel before command leaves paper available; reload after commit restores one attached original. Repeated detach/reacquire and late callbacks cannot lose/duplicate it. See SPEC's shared command boundary and [mode/restore verification](../VERIFICATION.md).

## Performance/resources

Reuse existing paper mesh/face and attachment slot; no persistent extra camera, per-frame search or copied certificate model family.

## Internal checks — AI only

- [x] Disposable signed full-loadout fixture reproduces refusal before change, then acquisition→attachment→read/use succeeds without purchase/deletion; additional retained letter-slot fixture also succeeds. (Refusal: baseline T13 check passed on unchanged source before edits and the new reproduction check on the unchanged ordinary path; see record.)
- [x] Inject failed write, duplicate/stale callback, no-folder prerequisite, full-capacity detach and reload; assert exact source/ownership and unchanged wallet/Trust/grades.
- [x] Compile changed runtime/Editor paths; run affected current inventory/L3/shared-paper checks using VERIFICATION recipes, not real saves.

## USER Unity/device acceptance

- [ ] With base Bag full, take/clip/read/use the actual certificate, then try detach with no room; no hidden slot, lost item or premature credit entry.
- [ ] Try tap/drag, Back/Settings/focus and reload; paper remains visible/reachable and returns to correct owner.
- [ ] Record actual build/device/steps/evidence for applicable gates **G1, G8** in [VERIFICATION](../VERIFICATION.md); source compilation cannot check this box. AI delivers assets/builders/bindings; user is not assigned manual scene assembly.

## Completion definition

The visible goal works through its real entry/exit with the stated source, presentation, ownership and recovery rules; relevant internal checks pass or precise inherited limitations are recorded. Mark **Implemented, internally verified; user manual pending** only after actual implementation/checks. Mark **User accepted** only on recorded user evidence. Required unresolved asset/content/voice/device gates remain explicit; no placeholder or documentation-only completion.

## Handoff/update requirements

Update this ticket's actual status/checks, [PROGRESS](../PROGRESS.md), [KNOWN_ISSUES](../KNOWN_ISSUES.md) and the new pack's affected contract/inventory/coverage when needed. Record owner/session/date, modified paths, exact commands/results/warnings/fixtures, failures and unperformed native checks, actual supplied acceptance and precise next dependent/blocker. Preserve old initiative histories and existing user work; follow applicable AGENTS and the user's Git/Unity ownership. Scope stays Levels1–3.


## Implementation record — 11 November 2026

Owner: AI implementation session (Claude), user-selected T01 only. No Git, Unity launch/import/automation, real-save access/reset, ADB, packages, purchases, paid generation or contact. No scene, prefab, Resources JSON or new asset; no new `.meta`.

### Player-visible result

- **Full bar and Bag:** Bea's offered certificate still floats beside her. A tap writes nothing and shows one short line plus a **CLIP TO BEA'S FOLDER** plate under the actual paper (HUD palette/font, world-space, faces the view). A deliberate tap on the plate, within reach, saves the handover and the folder attachment together; only then does the paper fly to the carried folder (existing pickup flight and paper cue) and dock in the existing Supporting Documents certificate slot. Arthur's silent bubble says where it went.
- **Reading/use while clipped:** with no free slot, taking it out of the folder reads it where it is clipped; its existing amount bands work at the desk; putting it down (or dropping it on the folder) settles it back into its slot. A bar/Bag drop says "Your bar and Bag are full, so it stays clipped in Bea's folder. Make room to unclip it." No invisible grant, empty tile or second copy.
- **Roomy Bag:** unchanged ordinary pickup into its own slot; the plate never appears and disappears if room is made.
- **Ineligible folder** (left on the desk/not carried): "Bea's folder isn't with you. Take it from your desk, then clip the certificate." The paper stays available.
- **Unclip with room:** unchanged (detach, then the existing quiet Bag recovery). Unclip without room is refused for the certificate and the summary; both stay attached and are never lost.
- Nothing fills in the credit, explanation or balance; the player still links the credit on the Computation page.

### Data/command path (existing owners only)

`Level3CertificateRules.ClipToFolder` = existing `HandOver` + existing `Level3ReviewRules.SetAttachment` on one candidate copy, plus action id `Level_03/bea-certificate-clipped`; refuses before mutation outside the credit step or without the carried folder; repeats are `Duplicate`. `Level3Checkpointer.CommitCertificateToFolder` → story `PlayerProgressManager.CommitLevelThreeCertificate` (one signed candidate; `Level3CertificateRules.ApplyToInventory` keeps the certificate's readable pages on the owned folder record instead of a slot; story requires the folder be owned) → publish. Non-story runs touch only their local inventory. `SetAttachment` takes the carried inventory from `Level3CaseFolder.SetAttached` and refuses an unclip with no slot (`Level3ReviewRules.RoomFor`, also now behind `HasRoom`).

### Changed files

- `Assets/Scripts/Level3/Level3CertificateRules.cs` — clip command, eligibility, `Clipped`, `ApplyToInventory`, shared reference-page builder, lines.
- `Assets/Scripts/Level3/Level3ReviewRules.cs` — `RoomFor`, `RefuseUnclipFull`, optional inventory on `SetAttachment`.
- `Assets/Scripts/Level3/Level3Checkpointer.cs` — `CommitCertificateToFolder`.
- `Assets/Scripts/RewardsSystem/PlayerProgressManager.cs` — `CommitLevelThreeCertificate` uses `ApplyToInventory`.
- `Assets/Scripts/Level3/Level3CaseFolder.cs` — `SetAttached` passes the carried inventory.
- `Assets/Scripts/Level3/Level3CertificatePaper.cs` — full-Bag offer, clip path, read-in-place, settle-back, and the `Level3CertificateClipTag` component (in this file; added at runtime, no asset).
- `Tests/Level3Regression/Program.cs` — "Milestone T01" section (29 checks).

Reuse paths inspected and left unchanged: `CampaignInventory.cs`, `HudHotbar.cs`, `ClientCopyPickup.cs`, `ClientRecordPaper.cs`, `Level3SummaryDocument.cs`. All edited C# kept CRLF (verified 0 bare LF).

### Commands and results

- Baseline before edits: `python3 -B Tests/run_level3_repair.py` → runtime 0 errors/21 warnings, Editor 0/10, **Level3Regression 2826/2826**, controls 5734. T13's full-Bag refusal check passed and printed "10 for 9 base slots" (the pre-change L3-R106 reproduction on unchanged source).
- After edits: `--build-only` → runtime 0/21, Editor 0/10 (same counts). `--suites` → **Level3Regression 2855/2855** (Milestone T01 29/29), controls 5734. First post-edit run had 3 test-side failures (null printed line in an existing page, reused summary action id, an index matching an earlier `return null`); tests corrected, no production change.
- Mutation probe (disposable: mutated copy of `Level3ReviewRules.cs` compiled in the scratchpad, suite run from `Temp/M01Mutation`, both deleted; Assets untouched): `RoomFor` forced true → 7 failures (5 T01, T13 full-Bag, existing full-inventory recovery).
- `Tests/VerifyHudCopyPickup.py` PASS (6). `Tests/VerifyLevel3Scene.py` PASS, 4 inherited scene/prefab warnings, scene unchanged. `Tests/VerifyGuideTargets.py` 14 paths/0 missing. Level2 suite not rerun: no shared L2 behaviour changed.

Fixtures (all in-memory or a disposable `SaveFileStore` under the system temp folder): full base loadout (every Chapter 1/2 item, book returned, summary in Bag); roomy base and upgraded; folder not carried/on desk; pre-tax-due and post-handover states; failed write via blocked temp path then retry and signed reload; repeated clip/unclip/reacquire; returned review; extra letter slot using a disposable `fixture_kanor_letter` catalogue entry with the postcard on display.

### Limits and remaining dependencies

- Not run: Unity import/compile in the Editor, Play Mode, plate rendering/readability/reach, flight/dock motion, touch/drag, audio, device. Plate size (0.42 × 0.10 m, 32-unit text) and offset are unmeasured in a real view.
- Fixture "letter + postcard carried + book already returned in Chapter 2" refuses the **summary** (not the certificate) — outside T01; recorded as M01-SUMMARY for T08.
- Cancellation/interruption are covered by command-level checks and the existing paper guards (Settings/dialogue/focus/Back), not native input.

### USER Unity/device acceptance steps

1. Continue a completed-Chapter-2 base-capacity save that kept every item (or fill bar + Bag). Return the desk book, collect Bea's summary, reach tax due, meet Bea in the conference room.
2. Tap her certificate: one line, plate visible under the paper, Bag unchanged. Open Settings and return; walk away and back: paper and plate still there.
3. Tap the plate: paper flies to the folder; Notes ticks the certificate step; open the folder → Supporting Documents shows it in its slot.
4. At the desk tap it: it lifts and reads; drag its tax-withheld band to Tax credits; put it down: it returns to the folder. Drop it on the Bag: refusal line, it returns.
5. Clip the summary to the folder, then unclip the certificate: it goes to a Bag slot; reclip; then try to unclip the summary with the Bag full (refused).
6. Reload with it clipped: exactly one certificate in the folder, none in the Bag. Repeat step 1–3 with room in the Bag: ordinary pickup, no plate.
7. Record build/device/steps for G1/G8 in VERIFICATION.

Next: T01 user acceptance. T08 (letter pickup capacity, M01-SUMMARY) and T13 are its direct dependents; no other ticket was started.
