# Backend polish: known issues and limits

| # | Issue | Why / workaround |
|---|---|---|
| B1 | The save signature key is built into the game. Someone who decompiles it can forge a save. | No offline save can stop that. The check stops casual editing. Don't change the key in `SaveFileStore`, or every signed save will load from its backup or be rejected. |
| B2 | A plain (unsigned) file with `saveVersion` below 3 is still accepted as an old save. | This keeps real version 1 and 2 saves working. A hand-made file could pretend to be old, but `Normalize()` still caps its records at their maximums. |
| B3 | The on-time bonus adds nothing to a perfect run (it is already full). | Intended: the score can't pass 100%. The Level Complete card still shows "On time ✓". |
| B4 | A gradeable field needs one line in each switch of `CaseFieldRules`, plus the field in `CaseData` and `AnswerKeyDefinition`. | `CaseData` is typed, so the key-to-field mapping must be code. Which fields a case grades is JSON. |
| B5 | `TaxComputationCalculator` still uses flat prototype rates (8% / 20%). | Not used by Level 1. Move them into JSON when a level computes tax. |
| B6 | The pending-result save on pause or quit runs only if `LevelCompleteUI` showed the card. | That is the only place a level result exists. |
