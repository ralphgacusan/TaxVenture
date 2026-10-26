# T24 — Start the plaza rounds with Jhun-jhun's records

Status: Planned. Blocked by: T16.

Implementation evidence: original scope implemented and internally verified on 2026-10-26, user manual acceptance pending; see PROGRESS. The planning header and original acceptance describe that historical scope, not a new implementation request.

**Superseded opening design:** [T30](T30-arthur-reviewed-file-and-opening-route.md) replaces the Jhun-jhun-first gate and bench sleeve with Arthur's collectible reviewed file; [T31](T31-physical-record-extraction-and-reattachment.md) completes its physical paper controls. Execute T30 then T31 before T25. Preserve T24's reusable work and history; do not rebuild its old route or mark the new design implemented.

## Goal and visible accomplishment

Give Elijah a small client job before the four-profile board. Jhun-jhun presents two readable physical records; Elijah inspects them, hears a short observation from Arthur, and files Jhun-jhun's profile. The other three clients then become available in any order. This is the first playable slice of the [supporting document standard](../../SupportingDocuments.md), not a separate sample scene.

## Player flow and mechanics

1. Keep the existing intro, move/look, Notes chest, and Supply Counter order. Once the plaza rounds begin, the objective and trail point specifically to Jhun-jhun. Other clients remain visible, with neutral waiting availability; early taps cannot complete their profile handovers. Do not add blocking fences, vanish NPCs, or replay their unavailable line on every tap.
2. Jhun-jhun is preparing for his departure. Use his existing citizenship and overseas home/work facts. During his conversation he presents a copy sleeve at a measured nearby surface or a stable handover point. Two tabs identify **Identity** and **Background**, with icons and labels.
3. Tap either record to lift it into close-up; inspect and return each in either order. Identity explicitly states citizenship. Background records his present overseas home/work and brief visits. No passport-stamp decoding, day-count exercise, selectable answer, attachment puzzle, or formal category label appears here.
4. After both records have actually been presented in a settled inspection view and returned, Arthur gives at most two short contextual lines: for example, “His citizenship and where he lives are two different details. Keep both with his record.” Do not choose a board corner or place his profile for the player.
5. Collect the existing physical profile once. That completed first handover opens the other three conversations. Their existing concise talks and pickups stay intact; no compulsory supporting-paper routine is added to each.

Use “Meet Jhun-jhun”, “Look over his records”, and “Collect his profile” as short current-step wording. Replace displaced introductory explanation rather than stacking it. Retain Arthur's later deliberate in-person meeting after all four profiles.

## Implementation boundaries and presentation

Inspect `TaxpayerNpcInteractable`, `TaxpayerProfileWorldPickup`, `TownNpcDialogueLibrary`, `town.json`, `objectives.json`, `guide.json`, `Level1FieldCheckpointer`, profile/bag viewers, and existing dialogue/input ownership. Resolve actual current checkpoint type rather than assuming a filename. Add only the narrow document/inspection adaptation needed for these two real records. Stable source IDs associate papers with Jhun-jhun; do not copy answer keys into inventory.

Build the rounded ID card with silhouette portrait and cream background sheet from the standard. Handover 0.4–0.6 s; lift/turn 0.25–0.4 s; return 0.2–0.3 s, as configurable feel targets. Hold readable faces still. A sleeve can use existing compatible props; no new furniture set or animated NPC rig is necessary. Record actual anchors and supplied sound mappings.

## Failure and resume

Use the existing signed checkpoint owner for stable presented/returned record IDs and first-client handover state. No camera pose, sentence offset, or animation frame is saved. Cancelling a conversation cannot retire the client or lose his papers. Repeated taps, app loss, and save failure cannot duplicate profiles or unlock the other handovers prematurely. A previously progressed compatible campaign keeps its acquired profiles and route position; apply the new first-client prerequisite only where it remains relevant, without resetting or relocking completed work. Restore at a safe idle pose with pending records available.

## Internal checks — AI source/build/regressions only

- [ ] Actual record presentations and profile acquisition drive progression; advancing Arthur's guide pages alone cannot complete the route.
- [ ] Both inspection orders, cancellation/retry, early other-client taps, checkpoint round trip, failed save, and already-progressed resume are covered.
- [ ] No added reward/trust/coin source, board answer leak, or normal Level 2 launch; affected build/content/input checks pass.

## USER manual acceptance

- [ ] Jhun-jhun is naturally the first stop; records are readable, reachable, on theme, and return smoothly.
- [ ] Both records feel purposeful; the other three visits remain brief and freely ordered afterward.
- [ ] Relaunch mid-handover and after profile collection; no lost paper, repeated award, or stuck modal.

## Non-goals and handoff

No Quax, practice board, fifth client, automatic classification, shop reorder, clock redesign, or runtime Level 2 edits. Record bindings, save defaults, actual paths and evidence in PROGRESS. Next: [T25](T25-marie-desk-record-connection.md).
