# T08 — Regression evidence and final handoff

Status: **PLANNED — awaiting approval**
Dependencies: T07 integrated result. Next: final user review. Estimate: **6k-12k tokens**, excluding manual/device waiting and newly discovered fixes.

## Objective and existing verification seams

Confirm the implemented experience and leave evidence that another agent can trust. Re-inspect actual implementations and reports; never label a proposed feature or stale report as a pass.

Relevant checks: C# compilation and Git diff checks; `Tests/VerifyProfileCards.ps1`; `Tests/ProgressionRegression/ProgressionRegression.csproj` (PlayerProgressData only); `Assets/Editor/CorkboardRegressionValidation.cs`, `PaperDropValidation.cs`, `PaperCollectionValidation.cs`, `PaperTrayValidation.cs`; any focused tests added by T01-T07. Do not assume these cover new dialogue or collision behavior.

## Verification matrix

Record each as PASS / FAIL / NOT RUN, method, date, scene, resolution/device and evidence. Include actual results and reproduction steps, not this checklist alone.

- [ ] Movement joystick/right-side screen-swipe look indoors/outdoors; analog/dead zone; multi-touch; release/cancel/focus loss; desktop fallback; swipes never become unintended world taps.
- [ ] Elijah rig/materials, idle/walk transitions, stop/lock state, facing, foot contact and mobile cost.
- [ ] Camera walls/ceilings/corners/furniture, narrow office space, initial overlap, near-plane clearance and smooth obstruction recovery.
- [ ] Every exploration/first-person interaction entry, exit, cancel, failed start and interruption.
- [ ] Indoor NPC roles, outdoor NPCs, all four taxpayer targets, ordinary completed NPCs and later available client presentation.
- [ ] Start/advance/typewriter/reveal/skip/end/cancel; repeated/busy/empty conversations; gesture duplication and callback counts.
- [ ] Speaker/reverse framing with different heights and tight spaces; bottom text/name/Continue and mobile safe areas.
- [ ] Marker availability/completion/unavailable/active states, shape distinction, range/occlusion and re-enable synchronization.
- [ ] Desk tools and objective notes, including swipe close and correct return ownership.
- [ ] Profiles dropped/collected; tray entrance; corkboard categories and exact free pinning within bounds.
- [ ] Profile icon open, read, drag, select/topmost, edge limits, close/return, rapid toggles and controls restored after all transitions.
- [ ] Case/interview/objective/tutorial progression, successful/retry outcomes, counts and no duplicated rewards/drops.
- [ ] Pause, focus changes, scene unload, different supported screen sizes and actual mobile touch behavior.
- [ ] No new Console errors or missing references; measured performance and known limits recorded.

## Acceptance criteria

- [ ] All feasible automated/Editor checks actually run and their scope/results are recorded.
- [ ] Manual/device checks performed by the user or agent have explicit evidence; unavailable tests remain NOT RUN with concise instructions.
- [ ] No claimed overall regression pass with critical unresolved or untested flows.
- [ ] Every ticket has truthful status, modified file list, evidence, issues and continuation instructions.
- [ ] CURRENT_STATE, IMPLEMENTATION_PLAN decisions, PROGRESS and HANDOFF describe the actual delivered architecture.
- [ ] Existing uncommitted work remains preserved and no Git publishing/history action occurs without authorization.

## Deliverable and stop gate

Provide final file/change summary, test matrix, known issues with severity/reproduction, and exact next action. Resolve only scoped test failures with an explicit bounded work unit; never suppress assertions or edit reports to pass. If user/manual/device acceptance is pending, mark it pending instead of declaring the overhaul complete.

No next-ticket token estimate is necessary when all tickets are accepted; estimate any specific unresolved fix separately. Stop for final user review.

