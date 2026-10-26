# T30 — Replace T24's first-client routine with Arthur's reviewed file

Status: Planned -> Implemented, internally verified; user manual pending (2026-10-26; evidence in PROGRESS.md "T30"). Blocked by: T24.

## Goal and visible accomplishment

After the Supply Counter introduction, Elijah meets Arthur in person, collects his distinct reviewed file, and uses it to connect a former client's records to completed entries. Then all four existing plaza clients are available in any order and drop only their existing taxpayer profiles. This is the approved follow-up to implemented T24; preserve its history and reusable work, but replace its Jhun-jhun-first gate and bench sleeve. T31 finishes the physical paper interaction before T25 starts.

## Exact opening route

1. Preserve the intro, controls, Notes and Supply Counter order. Replace the first Jhun-jhun objective/waypoint with **Meet Arthur**. Create an **Arthur Start Placement** scene marker using the same editable position/yaw approach as `SupplyCounterPlacement`, with a visible editor gizmo. Resolve the real model, reach/collider and current root; no guessed coordinates or manual scene assembly left to the user.
2. Arthur becomes visible at that marker **after the Supply Counter introduction is complete** (2026-10-26 user refinement). Keep him hidden before that point; continued runs with an already-started Arthur route keep him available. Proximity or a guide page does not start the encounter: the player deliberately clicks him and enters the existing owner-scoped dialogue. Example: “There you are, Elijah. Before you meet today's clients, take a look at a file I handled.” Keep the opening at two short lines or fewer.
3. On completed dialogue, Arthur presents **Arthur's Reviewed File** using the Auditor Client Copy's lift/drop/hover choreography. The floating pickup is a separate reference item, never a Client Copy, taxpayer profile or active case. A cancelled/rejected dialogue grants nothing. Click to collect once through the existing campaign command boundary; the folder glides toward the Bag after the commit.
4. Objective: **Look over Arthur's file**. Briefly emphasize the existing Bag/item, then let the player open it. No second pickup or second handover after inspection. Page 1 shows a distinct fictional former client's basic details and the existing three completed entries: classification, taxpayer type, income source. Label **REVIEWED · READ ONLY**. Page 2 is **Supporting Records**, with attached Type 1 identity and Type 2 background documents. Use explicit citizenship, living history, work arrangement and income facts; consistent authored entries, no new tax rules or hidden answer-key access.
5. Guide attention through actual opened records and the completed page. Arthur has at most four short contextual comments across the whole inspection, one visible at a time. Example: “His ID records his citizenship. The background tells us where he lives.” On returning to page 1, connect those facts to the entry; similarly connect work arrangement and earnings to the two other entries. A brief field emphasis is allowed in this reference file only. No four-client answer reveal, answer selection, repeated definitions, or player-facing lesson/quiz/assessment framing.
6. After both real records were presented settled, returned, and the completed first page revisited, Arthur says: “Keep that handy. Now meet the four clients and collect their profiles.” Store/close ends the bounded sequence. The trail now selects any available client. Jhun-jhun, Marie, Milo and Zorbix retain their ordinary concise talks and profile-only drops; remove the added bench line/waiting first-client routine. No supporting papers need to be collected from them.

## Folder and inventory contract

- Two visible pages in the reference file: **Client Information** then **Supporting Records**. Reuse familiar folder mesh/page-turn/close gestures. The model/values are instance-bound; never temporarily rebind `CaseManager.CurrentCase` to display the sample. Do not add a second global case/save owner.
- Muted teal covers, cream pages, brass **REVIEWED** tab and sample client's name. One presentation identity supplies world pickup, closed/open models, drag ghost, bag icon/preview and store/return motion. Text/icon labeling distinguishes it from Marie's manila folder as well as color.
- One non-discardable, non-sellable reference item in the existing Case Files bag category. Supporting sheets are contents, not extra slots. It can be read from the bag during exploration, at the board and at the desk; never submitted, stamped, edited or used as a value-transfer source. Reopening it does not replay Arthur's introduction or affect Marie.
- Increase starting bag capacity by one and every bag-upgrade capacity by one, including maximum; hotbar and prices unchanged. Inspect live configuration rather than assuming old 5/12 values: if still 5/12, targets become 6/13. Apply once to fresh and existing campaigns; preserve upgrade deltas/ownership and item positions. Never increment capacity on each load. Full-bag handling uses the existing safe make-room path; the required file cannot vanish or block recovery.

## Reuse, actor lifetime and resume

Inspect `FirstClientRound/Rules`, `ClientRecordSleeve`, `ClientRecordPlacement`, `SupportingDocumentFace`, `TutorialNpcController`, `ClientCopyPickup/Handover`, `CampaignInventory`, `CaseFolderUI/CaseFolder3DView`, `guide.json`, `town.json`, objectives and current signed checkpoints. Reuse T24's IDs/presentation events where appropriate but keep the sample's source IDs separate from Jhun-jhun's historical records. Sample completed values are authored read-only source data, not persisted copies of Marie's answer key.

Arthur is one logical actor with start/board availability, not two visible copies. Keep him near the start through the opening; at the later board stage reuse him or relocate safely outside an active view/interaction. No visible teleport, automatic conversation, new walking AI or duplicate portrait/marker owner. Practice/direct-Editor routes remain isolated.

Save stable intro-completed, file-owned, record-presented/returned, first-page-revisited and rounds-open milestones via the current owner. Animations and guide pages do not grant items or complete steps. Interrupted dialogue/pickup/view and failed save retain retryable state. Compatible post-T24/progressed runs retain profiles, score, case work and unlocks; retire obsolete bench/gate logic without forcing replay or granting a sample file silently just because an old record flag exists. Provide Arthur's one-time collectible opportunity without relocking later work. Restore props settled; never save sentence offsets or paper transforms.

## Internal checks — AI source/build/regressions only

- [ ] Actual talk/collection/record events drive the route; all four profiles remain freely ordered afterward; old bench/waiting path is inactive.
- [ ] Sample and active case remain isolated across opening, transfer attempts, Auditor hand-in, close/reopen and resume; all icon/ghost/model forms resolve one identity.
- [ ] Acquisition and capacity are once-only; upgrade deltas, failed write, full bag, cancellation and progressed compatible checkpoints have coverage.
- [ ] Existing guide, inventory, campaign and dialogue checks/build pass; no new rewards, tax rules or Level 2 launch.

## USER manual acceptance

- [ ] Move the placement marker; Arthur's pose, arrows and click target follow it. Meet him after the Supply Counter; no duplicate actor later.
- [ ] Floating teal file, drag ghost, bag icon and open cover match. Collect, inspect both pages, close and reread later.
- [ ] The completed example explains its evidence naturally, takes about two minutes, and the four regular clients still drop profiles only.
- [ ] Resume mid-sequence and from an existing progressed save; no lost file, repeated reward or reset case.

## Non-goals and handoff

No fifth client, practice board, second case to complete, mandatory stacking lesson, new slot tutorial, shop reorder, clock change or return of T15's Quax demonstration. The reviewed example is explicitly authorized by this revision; earlier bans on any sample file do not apply to it. T30 is a functional route, not final paper-art acceptance: T31 is mandatory for finished 3D props and controls. Record actual anchors, identity/color mapping, capacity configuration, source IDs and save defaults in PROGRESS. Next: [T31](T31-physical-record-extraction-and-reattachment.md), then T25.
