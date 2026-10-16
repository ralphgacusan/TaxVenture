# Known gaps before implementation

| ID | Gap | Owner/required response |
|---|---|---|
| L2-01 | Previous scene is now Office_Level2_OLD; new Office_Level2 must be created and routed correctly | T01 follows SCENE_REUSE: one-time current-Level1 scene copy, distinct GUID, local specialization, preserve source/_OLD and existing destination work |
| L2-02 | HUD/map/guide/fade/finale/clock have Level1-specific startup restrictions | T01 establishes scoped opt-in, subsequent tickets add content/behavior |
| L2-03 | Existing cabinet action archives; no retrieval behavior is established | T02 adds phase-safe retrieval |
| L2-04 | Existing verdict only checks filled fields; false-issue results are unfinished | T07–09 implement evidence/reason evaluation with legacy fallback |
| L2-05 | Existing badge EXP threshold is incompatible with forgiving Level2 EXP | T06 uses explicit frozen-accuracy tiers with existing art/enum |
| L2-06 | Existing Level1 clock departure and generic late penalties conflict with Level2 cutoff/caps | T10 introduces level-selected behavior; no double deduction |
| L2-07 | Existing career saving is established; active Level2 checkpoint is a new bounded addition | T11 saves through existing owner/store and tests backward compatibility |
| L2-08 | Six documents/two-document comparison may be cramped on phone | T05/T07 implement usable framing; user tests readability in Play Mode |
| L2-09 | User-selected Rosa/store need local wiring and compatible animations | Use SM_Chr_Business_Female_03 from TaxVentureWithCompliance/Characters and Building_4 (1) from TownEnvironment/Env/Town; preserve source assets/instance appearance, avoid T-pose, keep portrait identity consistent |
| L2-10 | Loan-principal primary/course authority needs explicit verification | T01 records authority or clearly reports draft educational rule; no unrelated citation |
| L2-11 | Current Level1 itself has source work awaiting user Play Mode | Preserve live behavior, do not assume unresolved animation/navigation issues are solved |
| L2-12 | Runtime-generated targets are not covered by existing Level1-only scene validator | T12 validates builder contracts and distinguishes user-only verification |

These are known work requirements, not tested defects in a completed Level2. Update with reproducible findings as implementation progresses.
