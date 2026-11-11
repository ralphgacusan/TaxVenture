# T04 — Stage Bea’s compact records and the paper-machine table

Status: **Implemented, internally verified; user manual pending**. Blocked by: T02.

Priority: P1. Phase: B — Outdoor admission. Estimated scope: S–M; one fresh agent context. If real call-site work exceeds this bound, record a bounded split before adding unrelated framework work.

## Goal and visible accomplishment

Bea hands over her existing originals once. At her outdoor table, one readable record and a compact machine workspace replace the nine-paper fan and oversized worksheet. This slice prepares the machine staging; T05 supplies filing and T06 checking/output. Read [SIDE_QUEST_MACHINE](../SIDE_QUEST_MACHINE.md) in full; its latest feed-first order supersedes direct-to-bin placement.

Revision: **3 November 2026 — approved paper-machine interaction**. Status remains Planned; source presence is reuse evidence, not acceptance. Retain source/save identities and work serially from the current continuation.

## Existing systems to reuse

Existing source paths relative to repository root; inspect live callbacks/content before changing them. Narrow Level_03 adapters only; no duplicate folder/stamp/calculator/input/save/shop framework.

- `Assets/Scripts/Level3/Level3BeaTable.cs`
- `Assets/Scripts/Level3/Level3BeaInteractable.cs`
- `Assets/Scripts/Level3/Level3RecordRules.cs`
- `Assets/Scripts/Level3/Level3BeaOfficeVisit.cs`
- `Assets/Scripts/SideQuest1/ClientRecordPaper.cs`
- `Assets/Resources/Levels/Level_03/town_life.json`

## Work and interaction mechanics

1. Keep the existing Bea actor, table, seat, envelope, mat and office route. Short introduction: she kept receipts and wants help deciding what counts. No answer-giving lecture or repeated acquisition.
2. Acquire the existing nine originals with CommitBeaRecords; keep B-ID/B-BG as context, seven activity rows R1–R7, and B-WHT excluded until office T08. Slips are views of originals, never consumed originals or extra inventory entries.
3. Replace the nine-paper fan with a compact stack/envelope and one selected original reader. Show title, prominent amount, purpose/year/reference; technical IDs stay in optional detail. Reopening never creates another record.
4. Stage the stand left and compact machine area right, with a small base review rack and clear table edges/world context. Keep guide/hotbar/Leave Table unobstructed. Do not inherit the old 0.8-screen-height lifted board or stage an outdoor calculator.
5. Inspect live Synty CardRoller/printer/paper candidates and Level3DeskProps; record the chosen asset/build seam and material references. Build only the minimal passive footprint/stand needed here, leaving machine interactions to T05. No speculative claim that imported props have working lever/roller rigs.
6. Bind current seat/camera/viewer/input owners. Inspect original temporarily replaces the focused card view; return restores it. Cancel, leave/return and write failure retain source availability with one owner and no stuck locks.
7. End introduction with the clear invitation to prepare Bea’s records at the machine. T04 has no auto-placement, score/reward, fake working filing or duplicate temporary board. Preserve T03’s familiar folder/calculator improvements.

## Assets, motion and feedback

Follow [machine visual/motion contract](../SIDE_QUEST_MACHINE.md) and [world contract](../WORLD_AND_FEEDBACK.md). Same existing low-poly scene, paper/font/material vocabulary and restrained sound/settings; no oversized navy/brass board, new art theme, full-screen flat substitute or auto-shrunk labels. Real anchored 3D bodies, reachable targets and correct slot occlusion are required in this slice. Implementer binds everything; user checks actual Unity/phone rendering. The linked storyboard is a mechanical reference, not final visual acceptance.

## Integration requirements

Preserve shared L1/L2 behavior, existing serialized identities/source scenes and all campaign possessions. Keep current exact-decimal L3 rules/provenance; no float prototype bridge. Use existing input/dialogue/FSM/guide/Bag/reward owners. Career3 stays gated until T18, L4/L5 remain unavailable. No remote ticket, package, scene recopy or global cleanup.

## Persistence and failure rules

Candidate -> signed persist -> publish before success effects. Validate mode/case/source/revision/prerequisites/action IDs; duplicates do not award. Preserve legacy grades/claims/approved snapshots/copies and quiet restores. Failure keeps prior state/source/copy retryable. Clear only owned pointer/selection/locks on Back/settings/disable/focus/camera return; independent work can be left. Practice/replay/Editor cannot mutate live story state.

## Internal checks — AI source/build/regressions only

- [x] Compact source stack replaces fan; one original reader preserves acquired identities and excludes B-WHT.
- [x] Passive stand/machine footprint respects table/camera/HUD ownership; no outdoor calculator or board reactivation. (Layout/source only; camera framing and HUD clearance are user checks.)
- [x] Bea source set acquired once; certificate excluded; prior papers/Bag remain readable and case-scoped.
- [x] Cancel/write failure publishes no acquisition; resume restores quiet paper availability.
- [x] One active source viewer and existing actor/route ownership; activity admission does not grade or auto-place.
- [x] Concise runtime dialogue/objectives/guide agree with revised assignment and no budget-practice requirement.
- [x] Build actual changed production paths and run targeted command/save/failure/mode checks plus affected shared regressions; record commands/counts/warnings, not historical passes.

## USER manual acceptance — Unity/device/visual/feel/listening only

- [ ] See Bea/table/world around one readable record; compact stack and stand never cover most of game view.
- [ ] Read originals, cancel, walk away/return and open settings without source loss, clipping or competing input owners.
- [ ] Buildup: notice, talk, envelope slide, open, inspect, Help Bea/Later and re-entry read clearly and briskly (about 60–90 s), sound on and off.

## Handoff and completion definition

Deliver the goal above through its real entry point, including persistence, presentation and checked limitations. Update [PROGRESS](../PROGRESS.md)/[KNOWN_ISSUES](../KNOWN_ISSUES.md) with actual evidence and remaining user checks; do not mark implemented for documentation presence. User owns Unity and every Git operation.

### Evidence — 4 November 2026 (internal only)

- Acquisition unchanged and reused: talk completion → `CommitBeaRecords` (candidate, signed write, publish); B-WHT stays out (T08). Failure keeps Bea's records and the same talk.
- `Level3BeaTable`: nine existing `ClientRecordPaper`s in one pile on the opened envelope (back left). One reader: tap the stack (its top paper; a read paper goes to the bottom) or the stand card; taking one out returns the other; tap again or drop on the table to return. Leave/disable gather; settings/dialogue/pause/camera moves disable touches; focus loss cancels drags. No worksheet board or calculator is built at the table (`Level3DeskCalculator.AwayAtBeaTable` is now always false).
- New pure `Level3TableStaging` (layout reserving each part's turn toward the seat, stand-card row/face, one-reader stack) and `Level3PaperMachineModel` (stand + card, passive machine: body, slot lip, rollers, dial with three labels, lever hinge, two blank windows, output ledge, three-slot rack; no colliders). Synty CardRoller (single mesh) and printer1.fbx (camera/light inside) inspected and not used.
- Copy: shorter introduction ending with the machine invitation; guide/objective reworded; built-in Bea mirrors town_life.json.
- Runtime/full Editor 0 errors (21/10 inherited warnings); Level3Regression 1407/1407 (60 revised-T04 checks); Controls 5734; Level2 scratch 1580 (3 native skips); print geometry 65 PASS. Details and the manual route: [PROGRESS](../PROGRESS.md), [KNOWN_ISSUES](../KNOWN_ISSUES.md) L3-R43–L3-R48.

Direct dependents: T05. Continue the revised index, retaining prior working changes and historical results.

### Table input/readability follow-up — 4 November

User reports only cards unclickable; Settings/Leave Table work. Level3-only direct rectangle EventSystem raycasting now serves stand/original touch surfaces through existing reader/drag handlers. View is slightly closer, card/supporting text and machine labels enlarged; full source details retain readable lines. Evidence and manual route: [PROGRESS](../PROGRESS.md), L3-R49/L3-R50 in [KNOWN_ISSUES](../KNOWN_ISSUES.md). Runtime/full Editor0 errors;1421/1421 Level3, Controls5734, Level2 scratch1580 (3 native comparer skips), shipped-font72 PASS. Native click/reader/phone/readability acceptance remains pending. T05 machine actions remain unimplemented.

### Buildup follow-up — 4 November (same ticket)

Bounded lead-in before the seated table: her sketch beside the envelope and one thinking emote on approach → two short turns (studio goal, receipts) → saved `CommitBeaRecords` → envelope slides into reach → "Open Bea's records" (flap, stack, logo-work original forward) → "Inspect a record" (existing reader, standing, `HudWindowLock`) → her organizer line, Help Bea / Later → existing seat; the stand card arrives once the camera settles. Session-only `Level3TableStaging.Encounter`: no new save field, reward, filing or clock start; restores/legacy saves skip to a concise continuation (Help Bea first in her questions). Editor fixtures Bea Encounter / Bea Table Restored. Runtime/Editor 0 errors; Level3 1470/1470 (49 buildup); Controls 5734; Level2 1580 (3 native skips). Manual script: [PROGRESS](../PROGRESS.md); limits L3-R51–L3-R57. Next remains T05.
