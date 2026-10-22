# T11 — Collect world records and optional keepsakes

Status: Planned. Blocked by: T09, T10.

## Goal

Keep required records and optional world finds in one durable campaign bag.

## Visible accomplishment

Before: profile pickups can be missed or lost between scenes. After: during Level 1, the player meets four taxpayers and physically collects their real profile cards. An optional Coast Tales postcard and one appointment voucher can also be picked up in the world. The bag shows these actual objects as owned records and keepsakes after a scene change or app restart. Required papers remain available from their normal handover even if the player walks past an optional object.

## Work

Connect the four existing profile pickup paths to the campaign inventory so collection has one durable owner. Preserve the existing short profile text and taxpayer identity; an owned profile is a neutral record and must not expose classification answer keys or other case-only values. Add the Coast Tales postcard as optional, light character lore with a clearly visible office display slot. Add one optional voucher at the public consultation supply point only after the relevant introduction. Register each acquisition with a stable campaign-scoped action ID and claimed-item ID. Re-entry removes a claimed pickup or presents it as collected; it must never mint another copy. A required profile may not disappear before the game's existing dialogue or case handover has made its content available.

### Source starting points

- **Existing:** `Assets/Scripts/SideQuest1/TaxpayerProfileData.cs`, `TaxpayerProfileWorldPickup.cs`, `TaxpayerProfileCollectionHUD.cs`, and the signed-save owner in `Assets/Scripts/RewardsSystem/PlayerProgressData.cs` / `PlayerProgressManager.cs`.
- **Proposed:** add typed campaign item definitions and one acquisition command at the shared campaign boundary from SPEC. Do not treat these proposed definition/command names as existing symbols. Persist stable IDs and safe display snapshots only; adapt the current profile pickup rather than creating a second profile system.

### Physical presentation and feedback

Each profile is a palm-sized, stiff cream card with the taxpayer's portrait or established identity image, name, one-sentence clue summary, a coloured corner stripe plus a distinct icon, and a paper edge that remains readable in the bag. On a valid touch, the card tips toward the camera, lifts about 8 cm, turns face-forward, then glides to the collection journal and leaves the source spot clear; total motion is about 0.45 s and can be skipped by a second tap. The postcard uses a coastal blue border, one island illustration and a two-sentence neutral memory; the voucher is a folded green ticket reading “ONE REVIEW EXTENSION” with a large 180-second benefit printed on it. Each rests in a visibly separate physical slot. Successful collection uses one quiet paper pickup followed by one card-place cue; optional keepsake collection gets one short warm sparkle, never a reward fanfare. Route one-shot effects through the existing AudioManager/effects-volume setting, do not replay from Update, and never stack repeats while a card is held.

### Persistence and failure rules

The acquisition transaction validates story mode, item identity and the existing claim before changing ownership. It commits claim, bag/drawer quantity and pickup-complete fact together. If the two-voucher carry limit is full, the pickup routes the voucher to the office supply drawer and says so before collecting; T14 makes drawer retrieval usable. On write failure, in-memory ownership and claim remain unchanged, no success animation or SFX plays, and the object remains touchable for retry. Repeated touches and restore do not grant another item. Optional acquisition grants no coins, trust, XP, badge or answer hint. A direct Editor/practice/mastery run may show isolated temporary props but cannot consume or change campaign state.

## Internal checks — AI source/build/regressions only

- [ ] All four real profile records and the two optional item definitions resolve from the same campaign inventory; no answer-key field enters a player snapshot.
- [ ] Save round-trip, duplicate touch, scene re-entry, failed-write retry and non-story-mode cases preserve one claim and one copy.
- [ ] Static scene/content inspection verifies each pickup has a collider, stable ID, deterministic placement and a safe handover path; compile plus affected inventory/save suites pass.

## USER manual acceptance — Unity/device/visual/feel/listen checks

- [ ] In Play Mode, collect each profile, leave/re-enter, and confirm its physical source card and bag entry tell the same story.
- [ ] Skip both optional pickups, finish the required handovers, then collect the postcard and voucher and inspect their physical slots.
- [ ] On the target touch device, check card text, pickup reach, the 0.65 s motion, journal landing, paper sound and effects-volume control. No manual scene construction is part of acceptance.

## Handoff

Next ticket: [T12](T12-physical-shop-and-bookmarks.md) adds the shared physical counter and bookmarks.

## Non-goals and references

No shop purchase, bookmark system, new profile rules, randomized loot, repeatable pickup rewards, new tax content, or manual Unity scene assembly. See [SPEC](../SPEC.md), [WORLD_AND_FEEDBACK](../WORLD_AND_FEEDBACK.md), [VERIFICATION](../VERIFICATION.md), and [PROGRESS](../PROGRESS.md).
