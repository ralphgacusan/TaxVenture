# T09 — Award completion coins once and present the wallet

Status: Planned. Blocked by: [T07 — corrected progression and results](T07-corrected-progression-and-results.md).

References: [Campaign spec](../SPEC.md), [world and feedback](../WORLD_AND_FEEDBACK.md), [verification plan](../VERIFICATION.md), [progress log](../PROGRESS.md).

## Goal and before/after flow

Give the player a clear, earned starting wallet after required story work. Before this ticket, there is no campaign wallet or saved coin balance. After it, completing the required corrected Level 1 activity grants 60 coins once and the corrected approved Marie case grants another 100, for a first-story total of 160. Replays and repeated events never farm coins. Shop purchase logic and optional purchases remain in their later tickets.

## Ownership and mechanics

Extend the same v5 campaign economy record and command boundary used by T02/T07. Keep PlayerProgressManager / SaveFileStore as the one signed save owner. Key each reward claim by the stable tuple `campaignId + levelId + activityId + rewardKind`; never scope it to transientRunId, animation, scene, or presentation identity. Use fixed catalogued values rather than values supplied by UI. A quest reward already claimed remains spent when an unfinished story attempt is restarted. Validate that the current run is a story run, required correction/case approval is complete, the event ID is valid, the claim is unspent, and wallet + reward cannot overflow. A duplicate claim returns no-change. Reject negative amounts and unknown reward IDs. Normalize corrupt negative balances to a safe zero with an explicit repair record; do not infer a balance from old XP, hearts, badges, purchases, or story history.

For each milestone, construct one candidate containing the activity completion/checkpoint changes, wallet delta, and claim ID, persist it atomically, then publish the committed snapshot and coin presentation. If saving fails, the old wallet and claim remain, the checkpoint is retained, no coins are shown or sounded, and the UI offers a retry. Ensure result re-entry, duplicate animation callbacks, relaunch, and replay cannot apply the reward twice. The completion result may display a pending coin amount only if the transaction has not yet committed and clearly labels it as pending; otherwise show the persisted balance.

The inspected current route includes PlayerProgressManager.ApplyLevelResult and Level 1 quest/case milestone owners. Add a wallet presenter beside the existing campaign result/menu presentation; have it observe a committed progress snapshot. A compact “60 coins” / “+100 coins” receipt and persistent readable wallet count are sufficient. After commit, send one grouped coin flight from the completed activity/result to the wallet over roughly 0.4–0.7 seconds, then show the exact grant and actual balance. Never animate one event per coin. Reuse the existing reward select cue; do not play success sound on failure or save retry. The 160 total and fixed values are configuration/rules, not hardcoded presentation text.

## Non-goals

Do not add a shop transaction, item catalogue, wallet spending, selling, loot boxes, real-money purchases, replay income, per-conversation income, optional pickup coins, or coin-based progression gates. Existing progress never supplies campaign coins.

## Acceptance

Internal checks — implementation agent records evidence in ../PROGRESS.md:

- [ ] Fresh campaign wallet initializes at zero; first required quest claim adds 60 and corrected case claim adds 100.
- [ ] Duplicate, replayed, resumed, and repeated presentation events cannot claim either reward twice; restarting an unfinished story after the quest claim cannot farm its reward because the stable claim tuple is unchanged.
- [ ] Candidate write failure preserves old balance and claim list; retry after recovery commits exactly once.
- [ ] Arithmetic overflow, negative amounts, unknown IDs, and non-story modes produce no mutation.
- [ ] Existing XP/hearts/passed records never produce campaign coins.
- [ ] Wallet presentation observes persisted snapshots and emits no success cue before commit.

USER manual acceptance — only the user performs these checks:

- [ ] Complete the corrected activity and case; confirm 60 then 100 coins appear and total 160.
- [ ] Interrupt/relaunch at each reward milestone and verify the balance and receipt do not duplicate.
- [ ] Replay the chapter and confirm there is no additional income.
- [ ] Confirm a low frozen classification tier still earns the first-time activity reward after corrections.

## Handoff

Record fixed reward IDs, values, and save transaction behavior in ../PROGRESS.md. T08 supplies trust independently; later shop tickets may spend this balance only through the same persisted command domain.
