# Ordered implementation tickets

Status: Planned. Authoring tickets does not implement the game. Date: 2026-10-21.

Delivery A (T01-T17, plus amendments T22-T23 added 2026-10-24) is the current menu/Level1 initiative. Delivery B (T18-T21) is the NEXT Level2 integration. Start T01 only when implementation is requested. No remote tracker issues are created. T01 is a bounded prefactor; all other tickets deliver a visible vertical slice with the necessary persistence, content, presentation and internal verification.

Read [SPEC](SPEC.md), [world and feedback](WORLD_AND_FEEDBACK.md), [verification](VERIFICATION.md), [progress](PROGRESS.md) and [known issues](KNOWN_ISSUES.md). Default execution is numeric order, even where the DAG permits independent work. Dependency freedom is NOT authorization for parallel edits. Source paths are starting points to inspect, not a command to rewrite their entire modules.

| Ticket | Blocked by | Delivery | Visible accomplishment |
|---|---|---|---|
| [T01 — Separate existing score hearts behind a mastery boundary](tickets/T01-mastery-boundary.md) | None | A | Preparatory refactor preserving the current playable baseline |
| [T02 — Initialize and safely save a fresh campaign](tickets/T02-fresh-campaign-and-save.md) | T01 | A | New starting values, durable save/recovery and no imported old progress |
| [T03 — Expose New Game, Continue and explicit run modes](tickets/T03-new-game-and-continue.md) | T02 | A | Menu distinguishes story, replay, practice and safe new starts |
| [T04 — Resume plaza conversations, profiles and fieldwork](tickets/T04-level1-field-resume.md) | T03 | A | Continue restores L1 field progress without repeat pickups |
| [T05 — Resume casework, review time and completion boundaries](tickets/T05-level1-case-and-clock-resume.md) | T04 | A | Inputs, audit corrections and deadlines survive a relaunch |
| [T06 — Make the classification board draft, check and correction based](tickets/T06-classification-first-check.md) | T04 | A | A physical check freezes the first complete grade, corrections remain playable |
| [T07 — Unlock story through corrected work and separate mastery results](tickets/T07-corrected-progression-and-results.md) | T05, T06 | A | Low mastery no longer blocks corrected chapter completion |
| [T08 — Earn Public Trust through completed service and show its reasons](tickets/T08-public-trust-and-follow-through.md) | T07, T22 | A | Heart meter, future target and once-only L1 follow-through; revision: the Auditor's read-only Client Copy is carried and handed to Marie |
| [T09 — Award completion coins once and present the wallet](tickets/T09-completion-coins-and-wallet.md) | T07 | A | Quest/case rewards commit once and have clear coin feedback |
| [T10 — Carry items in a hotbar and manage them in an upgradable bag](tickets/T10-unified-bag-and-documents.md) | T05 | A | HUD icons become a 3-slot hotbar, a Bag icon opens a 5-slot inventory with desk drawer; slots upgrade in the shop |
| [T11 — Collect an island keepsake and usable supply in the world](tickets/T11-world-collectibles-and-keepsakes.md) | T09, T10 | A | Physical optional finds enter inventory and can be displayed/stored |
| [T12 — Buy functional bookmarks at the physical supply counter](tickets/T12-physical-shop-and-bookmarks.md) | T09, T10, T11 | A | One real purchase path connects counter, wallet, ownership, pinning and the Satchel/Belt Pouch slot upgrades |
| [T13 — Share the shop catalogue with the menu and install decor](tickets/T13-menu-shop-and-office-decor.md) | T11, T12 | A | Both shops agree; purchased cup/print persist in the office |
| [T14 — Consume a voucher to extend an eligible review appointment](tickets/T14-appointment-extensions.md) | T05, T11, T12 | A | Confirmation, supply use and +180 active seconds commit together |
| [T15 — Present Mayor Quax and an in-person sample demonstration](tickets/T15-quax-identity-and-demonstrations.md) | T06, T10 | A | REVERTED 2026-10-25 by user decision: the guide stays Arthur (no Quax, practice board or demonstrations) |
| [T16 — Teach the systems and finish L1 as a coherent adventure](tickets/T16-level1-onboarding-and-wrap-up.md) | T08, T09, T11, T13, T14, T15, T23 | A | Opening, bag/trust lesson, case, optional shop and wrap-up connect |
| [T17 — Verify Delivery A and hand off ordered manual acceptance](tickets/T17-delivery-a-verification.md) | T16 | A | Menu/L1 outcome matrix internally checked; user receives exact manual script |
| [T18 — Adapt Rosa's saved investigation to the shared campaign](tickets/T18-level2-campaign-and-inventory.md) | T17 | B | L2 retains its physical flow while using shared bag, wallet and mastery contracts |
| [T19 — Give Rosa an explicit ethical choice with persistent consequences](tickets/T19-rosa-choice-and-trust-consequences.md) | T18 | B | Knowing shortcuts, respectful help and dismissive holds have different trust histories |
| [T20 — Unlock and use the purchased physical comparison stand](tickets/T20-comparison-stand-and-shop-unlock.md) | T18 | B | Two actual source records clamp side by side without supplying answers |
| [T21 — Enable integrated L2 and verify its complete campaign handoff](tickets/T21-delivery-b-verification.md) | T19, T20 | B | L1-to-L2 continuation is enabled only after integrated verification |
| [T22 — Show NPC emotion through animated emotes](tickets/T22-npc-emotion-emotes.md) | None | A (amendment) | Ten shape-distinct emotes as bubbles or peak bursts, tagged per dialogue line |
| [T23 — Visit Marie in the conference room during her case](tickets/T23-marie-consultation-visits.md) | T08, T22 | A (amendment) | Marie waits in the conference room; consultation lines climb a mood ladder with clues, no consequences |

## Dependency and rollout notes

T04/T05 checkpoint work and T06 grading converge in T07. T08 trust and T09 coins both use the saved campaign command boundary, not each other's counters. T10 reuses actual profile/case models before T11 pickups and T12 purchases add ownership. T13 unifies the second shop view; T14 shares review-clock commands without replacing both clock implementations. T15 supplies the physical guide actor/sample; T16 composes the finished systems without awarding from tutorial steps. T17 is Delivery A's internal acceptance gate.

Amendment 2026-10-24: execute T22 (emotes), then the T08 Client Copy and handover revision, then T23 (consultation visits), before continuing with T10. T08's revision depends on T22's alert and heart emotes; T23 depends on the handover and owns Marie's move to the conference room. T16 now also waits for T23, so T17 and T21 cover both amendments.

T18-T20 adapt existing L2 systems without enabling normal campaign launch yet; T21 verifies the whole handoff and then enables it. Preserve old L2 source/scene for isolated development; do not silently launch it with new Public Trust labels over old accuracy-heart state. L3-L5 and ending scenes remain unavailable.

## Ticket completion contract

Every ticket lists goal, player experience, implementation boundaries, mechanics, visuals/motion/feedback, failure/resume, explicit blockers, internal checks, USER-only manual acceptance and next handoff. Mark each completed implementation 'Implemented — internally verified; user manual pending' unless user results exist. T17/T21 do not certify Unity appearance from source inspection. Update this pack's PROGRESS and KNOWN_ISSUES, not historical initiatives' docs. Use [AGENTS](AGENTS.md) for ownership and no-Git/no-AI-Unity rules.

