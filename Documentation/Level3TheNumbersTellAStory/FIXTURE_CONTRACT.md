# T01 — Canonical fixture bindings and smallest schema

Revision `L3-2026-v1`, 30 October 2026. Implementation-ready **documentation contract**, with [content-authority release gate](TAX_AUTHORITY.md) pending. No DTO, resource, runtime or save changes were made by T01. Decimal tables below are read by `verify_content.py`; amounts are pesos, no thousands separators in machine-checked cells. CONTENT supplies narrative/dialogue; this contract supplies exact IDs/bindings. Expected values and treatments belong to immutable definitions/validators, never player inventory/checkpoint answer keys.

## Annual contexts

| caseId | clientId | taxpayer | periodStart | periodEnd | taxYear | regimeId |
|---|---|---|---|---|---|---|
| L3-BEA-001 | bea | Beatriz Mendoza | 2026-01-01 | 2026-12-31 | 2026 | PH-IND-GRAD-2023 |
| L3-MARINE-001 | marine-services | Coast Tales Marine Services, Inc. | 2026-01-01 | 2026-12-31 | 2026 | PH-DOMESTIC-ORDINARY |

`bea` retains the existing plaza NPC identity; `marine-services` identifies the taxpayer, while proposed `nina-cruz` identifies its bookkeeper, not the payee/taxpayer. All identity references are fictional/masked (B-ID uses `•••• B26`); no invented official TIN is needed for arithmetic. Case number equals case ID. Record admission/review happens in the story's supplied-annual-packet framing, with no fabricated real completion date before 31 December. Profile/interview facts are printed, not hidden in NPC dialogue.

## Source registry

Each `documentName` below is also the exact top-level `documents[].documentName` binding. Names are stable keys; titles may be localized separately. Type 0 denotes an intentional working paper rendered with plain `record` presentation, **not a ninth fixed supporting-document type or a missing-type warning**. Types 1/2/4/5/7/8 follow SupportingDocuments. Expense and asset schedules use working-paper ledger layouts; Type 4 remains an income record.

| sourceId | caseId | documentName | paperType | reference | periodStart | periodEnd | issuer / party / status |
|---|---|---|---|---|---|---|---|
| B-ID | L3-BEA-001 | B-ID | 1 | ID-B26 | 2026-01-01 | 2026-12-31 | Fictional identity issuer / Bea / identity context, no money |
| B-BG | L3-BEA-001 | B-BG | 2 | BG-B26 | 2026-01-01 | 2026-12-31 | Consultant interview / Bea / client interview notes |
| B-REV | L3-BEA-001 | B-REV | 4 | REV-B26 | 2026-01-01 | 2026-12-31 | Bea studio / Bea / gross earned and collected fees |
| B-EXP | L3-BEA-001 | B-EXP | 0 | EXP-B26 | 2026-01-01 | 2026-12-31 | Bea studio / Bea / annual working cost schedule |
| B-RENT | L3-BEA-001 | B-RENT | 7 | RENT-26 | 2026-01-01 | 2026-12-31 | Plaza Studio Rentals / Bea / business-only lease with paid invoice extracts |
| B-SUB | L3-BEA-001 | B-SUB | 5 | SUB-26 | 2026-01-01 | 2026-12-31 | Island Design Tools / Bea / paid annual service consumed this year |
| B-PRINT | L3-BEA-001 | B-PRINT | 5 | PRINT-26 | 2026-01-01 | 2026-12-31 | Town Print Supply / Bea / paid printing and consumed supplies |
| B-PERSONAL | L3-BEA-001 | B-PERSONAL | 5 | PARTY-26 | 2026-06-15 | 2026-06-15 | Island Party Shop / Bea / paid private birthday purchases |
| B-DRAFT | L3-BEA-001 | B-DRAFT | 0 | DRAFT-B26 | 2026-01-01 | 2026-12-31 | Bea / Bea / rough claim schedule, not an extra transaction |
| B-WHT | L3-BEA-001 | B-WHT | 8 | WHT-B26 | 2026-10-01 | 2026-12-31 | Harbor Menu Client / Bea / supplied creditable withholding certificate |
| C-BG | L3-MARINE-001 | C-BG | 2 | BG-C26 | 2026-01-01 | 2026-12-31 | Consultant interview with Nina / Marine Services / interview notes, operations began 2024-01-01 |
| C-REV | L3-MARINE-001 | C-REV | 4 | REV-C26 | 2026-01-01 | 2026-12-31 | Marine Services / Marine Services / gross supported service revenue |
| C-COST | L3-MARINE-001 | C-COST | 0 | COST-C26 | 2026-01-01 | 2026-12-31 | Marine Services / Marine Services / working cost schedule with retained paid invoice extracts |
| C-TRIP | L3-MARINE-001 | C-TRIP | 5 | TRIP-C26 | 2026-07-01 | 2026-07-01 | Island Holiday Travel / Marine Services, owner's family beneficiaries / paid private withdrawal |
| C-DRAFT | L3-MARINE-001 | C-DRAFT | 0 | AD-26 | 2026-01-01 | 2026-12-31 | Assistance Desk / Marine Services / prepared working schedule |
| C-WHT | L3-MARINE-001 | C-WHT | 8 | WHT-C26 | 2026-01-01 | 2026-12-31 | Supplied payer certificate packet / Marine Services / annual aggregate; provenance gate pending |
| C-ASSET | L3-MARINE-001 | C-ASSET | 0 | ASSET-C26 | 2026-01-01 | 2026-12-31 | Marine Services verified schedule / Marine Services / year-end assets excluding qualifying premises land |

B-WHT is deliberately acquired at the credit step; other B sources at assignment/Bea handover per CONTENT. All C sources arrive at the company appointment after Bea delivery. C-WHT is a packet of same-year certificates, not a claim that a statutory 2307 has an annual aggregate field. Certificate subperiods must be printed on its continuation faces when approved itemization is supplied. No withholding-rate assessment or net-bank reconciliation is introduced. Unspecified actual payment dates remain “paid during stated period”; do not invent precise dates for annual aggregates.

## Monetary rows and transaction identity

`rowId` is stable inside its case; `fieldKey` is the exact printable `semanticKey`. `transactionId` names the underlying economic item, so evidence/claims do not create more costs. `relation` is `-`, `supports:<rowId>`, `claim:<rowId>` or `subset:<rowId>`. Subsets do not add to their parent row. Annual aggregates are identified as groups, not fictitious single bank entries. Printed rows state gross amounts; withholding is separate.

| rowId | sourceId | fieldKey | transactionId | amount | relation |
|---|---|---|---|---:|---|
| R1 | B-REV | BLogoFees | B-LOGO-26 | 500000.00 | - |
| R2 | B-REV | BMenuFees | B-MENU-26 | 400000.00 | - |
| R3 | B-EXP | BRent | RENT-26 | 180000.00 | - |
| R4 | B-EXP | BSubscriptions | SUB-26 | 90000.00 | - |
| R5 | B-EXP | BPrinting | PRINT-26 | 30000.00 | - |
| R6 | B-PERSONAL | BPersonal | PARTY-26 | 24000.00 | - |
| R7 | B-PRINT | BPrintingProof | PRINT-26 | 30000.00 | supports:R5 |
| B-RENT-PROOF | B-RENT | BRentProof | RENT-26 | 180000.00 | supports:R3 |
| B-SUB-PROOF | B-SUB | BSubscriptionProof | SUB-26 | 90000.00 | supports:R4 |
| B-CLAIM-BASE | B-DRAFT | BClaimBase | B-COST-GROUP | 300000.00 | - |
| B-CLAIM-PERSONAL | B-DRAFT | BClaimPersonal | PARTY-26 | 24000.00 | claim:R6 |
| B-CLAIM-PRINT | B-DRAFT | BClaimPrintAgain | PRINT-26 | 30000.00 | claim:R7 |
| B-WHT-INCOME | B-WHT | BWithheldIncome | B-MENU-WHT-26 | 250000.00 | subset:R2 |
| B-WHT-CREDIT | B-WHT | BWithheldCredit | B-CREDIT-26 | 12500.00 | - |
| C-R1 | C-REV | CServiceRevenue | C-SERVICE-26 | 8000000.00 | - |
| C-R2 | C-COST | COperations | C-OPS-26 | 1200000.00 | - |
| C-R3 | C-COST | CPremisesRent | C-RENT-26 | 500000.00 | - |
| C-R4 | C-COST | CAdministration | C-ADMIN-26 | 300000.00 | - |
| C-R5 | C-TRIP | CPersonalTrip | TRIP-C26 | 120000.00 | - |
| C-OPS-PROOF | C-COST | COperationsProof | C-OPS-26 | 1200000.00 | supports:C-R2 |
| C-RENT-PROOF | C-COST | CPremisesRentProof | C-RENT-26 | 500000.00 | supports:C-R3 |
| C-ADMIN-PROOF | C-COST | CAdministrationProof | C-ADMIN-26 | 300000.00 | supports:C-R4 |
| C-DRAFT-OPS | C-DRAFT | CClaimOperations | C-OPS-26 | 1200000.00 | claim:C-R2 |
| C-DRAFT-RENT | C-DRAFT | CClaimRent | C-RENT-26 | 500000.00 | claim:C-R3 |
| C-DRAFT-ADMIN | C-DRAFT | CClaimAdministration | C-DRAFT-ADMIN-GROUP | 420000.00 | - |
| C-DRAFT-TRIP | C-DRAFT | CClaimTripComponent | TRIP-C26 | 120000.00 | claim:C-R5 |
| C-WHT-INCOME | C-WHT | CWithheldIncome | C-SERVICE-WHT-26 | 8000000.00 | subset:C-R1 |
| C-WHT-CREDIT | C-WHT | CWithheldCredit | C-CREDIT-26 | 300000.00 | - |
| C-ASSETS | C-ASSET | CRelevantAssets | C-ASSETS-26 | 80000000.00 | - |

R1/R2 are the two annual fee categories. B-WHT-INCOME is part of R2; its unwithheld remainder is 150,000. R3/R4 retain the linked support but do not extract support as another strip. Only R1–R7 are the seven worksheet strips. B-DRAFT's base groups R3/R4/R5; its total is base + personal + duplicate printing = **354,000**. It does not total the support rows again.

C-COST's continuation faces contain the retained supplier invoice/voucher extracts for operations (Marine Operations Suppliers), rent (Harbor Premises Rentals), administration (Town Office Suppliers), all addressed to Marine Services, paid/current-period, business-only and reasonable. Their group references/amounts are the corresponding transaction IDs/PROOF rows above. This is supplied documentary support, not a claim that interview notes prove eligibility. C-DRAFT administration **420,000 contains 300,000 administration +120,000 trip**; C-DRAFT-TRIP is a component disclosure, not an additional row to add to 420,000. Printed total: 1,200,000 +500,000 +420,000 = **2,120,000**. Corrected total is **2,000,000**. C-ASSET never enters income, costs or credit.

Explicit total bindings (`componentRowIds` is an ordered list; commas below separate IDs). These totals are displayed assertions over rows, not additional economic transactions. The partial administration claim is not expanded a second time when totaling C-DRAFT.

| sourceId | fieldKey | componentRowIds | amount |
|---|---|---|---:|
| B-REV | BRevenueTotal | R1,R2 | 900000.00 |
| B-EXP | BCostTotal | R3,R4,R5 | 300000.00 |
| B-DRAFT | BDraftTotal | B-CLAIM-BASE,B-CLAIM-PERSONAL,B-CLAIM-PRINT | 354000.00 |
| C-REV | CRevenueTotal | C-R1 | 8000000.00 |
| C-COST | CCostTotal | C-R2,C-R3,C-R4 | 2000000.00 |
| C-DRAFT | CDraftTotal | C-DRAFT-OPS,C-DRAFT-RENT,C-DRAFT-ADMIN | 2120000.00 |

Credit edges explicitly distinguish the credit row, the certificate's income basis and its already-counted parent. They are not additive subtotal edges.

| creditRowId | incomeRowId | includedInRowId |
|---|---|---|
| B-WHT-CREDIT | B-WHT-INCOME | R2 |
| C-WHT-CREDIT | C-WHT-INCOME | C-R1 |

## Full annual rule reference

Nonnegative taxable income; choose the first row whose upper bound contains the **unrounded** taxable amount. A lower bound is exclusive except the zero band; an upper bound is inclusive. `-` is unbounded. These are annual taxable-income thresholds, not monthly withholding bands. Constants are generic book content, supported by A1/A2 in TAX_AUTHORITY.

| ruleId | lower | upper | baseTax | rate |
|---|---:|---:|---:|---:|
| IND-0 | 0.00 | 250000.00 | 0.00 | 0.00 |
| IND-15 | 250000.00 | 400000.00 | 0.00 | 0.15 |
| IND-20 | 400000.00 | 800000.00 | 22500.00 | 0.20 |
| IND-25 | 800000.00 | 2000000.00 | 102500.00 | 0.25 |
| IND-30 | 2000000.00 | 8000000.00 | 402500.00 | 0.30 |
| IND-35 | 8000000.00 | - | 2202500.00 | 0.35 |

Corporate constants and supplied facts below are also machine-checked. Rates are fractions; threshold/asset money is in pesos. These are part of the proposed rules/corporateFacts objects, not runtime facts already installed.

| corporateKey | value |
|---|---|
| ordinaryRate | 0.25 |
| smallRate | 0.20 |
| smallIncomeCeiling | 5000000.00 |
| smallAssetCeiling | 100000000.00 |
| eligibilityOperator | AND |
| excludeQualifyingPremisesLand | true |
| assetRowId | C-ASSETS |
| incentiveRegime | none |
| operationsStart | 2024-01-01 |
| birRegistrationDate | 2024-01-01 |
| mcitFollowingYearOffset | 4 |
| mcitRate | 0.02 |
| firstMCITYear | 2028 |

Formula: `baseTax + rate * (taxableIncome - lower)`. IND-0 returns zero. Ordinary corporate rate is 0.25; smaller domestic rate 0.20 requires **income <=5000000.00 AND assets <=100000000.00**, with the qualifying-land exclusion. Missing facts are not “false” defaults; fail readiness. Marine supplies all facts, fails only the income ceiling and has no special incentive regime. First MCIT year = operations commencement year +4 = **2028**, so no MCIT in this packet. No MCIT gross-income formula is taught as total revenue times a percentage.

Separate non-client book example, curriculum acceptance pending: **Practice Design Studio**, taxable income 350,000, IND-15: excess100,000 ×15% = **15,000 tax due**. This illustrates the method in a different band and contains no Bea/company answer. Protected tool example remains 100−20=80−8=72, with no tax, wallet change or grade.

## Expected calculation chains

Field keys below are proposed exact L3 domain keys. Values are hidden answers; acquiring sources/choosing a rule must never auto-populate them. Worksheet totals require explicit player transfer. The proposed L3 adapter may display corresponding legacy float fields, but those floats cannot be the authoritative calculation/grade/save value.

| caseId | field | expected | source / expression |
|---|---|---:|---|
| L3-BEA-001 | grossIncome | 900000.00 | R1 + R2, supported worksheet income total |
| L3-BEA-001 | allowableExpenses | 300000.00 | R3 + R4 + R5, supported worksheet cost total |
| L3-BEA-001 | taxableIncome | 600000.00 | grossIncome - allowableExpenses |
| L3-BEA-001 | excess | 200000.00 | taxableIncome - IND-20.lower |
| L3-BEA-001 | marginalTax | 40000.00 | excess * IND-20.rate |
| L3-BEA-001 | taxDue | 62500.00 | IND-20.baseTax + marginalTax |
| L3-BEA-001 | taxWithheldOrCredits | 12500.00 | B-WHT-CREDIT, linked to B-WHT-INCOME / R2 |
| L3-BEA-001 | finalTaxPayable | 50000.00 | taxDue - taxWithheldOrCredits |
| L3-MARINE-001 | grossIncome | 8000000.00 | C-R1 |
| L3-MARINE-001 | allowableExpenses | 2000000.00 | C-R2 + C-R3 + C-R4; C-R5 excluded |
| L3-MARINE-001 | taxableIncome | 6000000.00 | grossIncome - allowableExpenses |
| L3-MARINE-001 | taxDue | 1500000.00 | taxableIncome * 0.25 |
| L3-MARINE-001 | taxWithheldOrCredits | 300000.00 | C-WHT-CREDIT, linked to C-WHT-INCOME / C-R1 |
| L3-MARINE-001 | finalTaxPayable | 1200000.00 | taxDue - taxWithheldOrCredits |

## Worksheet decisions and scoring

The following twelve IDs are the complete first-check decision set; each is worth one point. Reasons use stable keys with CONTENT's visible labels. The relationship answer requires both target R5 and its source/reference B-EXP / PRINT-26; the invoice R7 is the same transaction. Correct reasons/link can earn credit even if a strip is in the wrong tray. Source identity/printed amount are automatic mechanical invariants, not additional points.

| decisionId | expected |
|---|---|
| dest-R1 | Income |
| dest-R2 | Income |
| dest-R3 | Allowable expenses |
| dest-R4 | Allowable expenses |
| dest-R5 | Allowable expenses |
| dest-R6 | Not deductible |
| dest-R7 | Supporting only |
| reason-R6 | Personal purpose |
| reason-R7 | Already represented |
| related-R7 | R5/B-EXP/PRINT-26 |
| subtotal-income | Exact sum of the player's Income set, with matching source edges |
| subtotal-expenses | Exact sum of the player's Allowable expenses set, with matching source edges |

Check completeness: seven unique strips with mechanically valid same-case identities and amounts; one destination each; two selected reasons; one selected relationship; both valid attached additions. Wrong but mechanically valid destinations, reasons, same-case relationship choices and numeric results are complete and scoreable. Empty fields, malformed expressions or foreign-case sources are incomplete, never a frozen zero. Empty chosen sets accept an explicit zero tape. Reason fields remain accessible regardless of destination. A subtotal must include each selected row exactly once; a matching number from omitted/extra operands is not supported arithmetic. Canonically R5/R7 share a transaction, but if both are chosen as expenses, including both once earns subtotal arithmetic credit; treatment/link feedback owns the duplicate error.

On the first complete deliberate Check freeze correct IDs/wrong IDs/count/tier and the checked draft revision. `correct =12-wrong`; Gold12, Silver10–11, Bronze0–9. Worksheet XP = `max(48,60-wrong)` (48–60); fixed coins60 and trust+2 happen only on corrected acquisition. Rechecks preserve the first score; replay may improve eligible best mastery only. Completion needs every treatment/reason/link correct, both totals supported/current and equal900,000/300,000, regardless of frozen grade.

Fairness examples: only R6 moved to expenses, reasons/link correct, sums900,000/324,000 ⇒ **11/12, Silver,59XP**. Only R7 also deducted with sum330,000 ⇒ **11/12, Silver,59XP**. Both wrong placements with sum354,000 ⇒ **10/12, Silver,58XP**. With those placements but expense tape300,000 ⇒ **9/12, Bronze,57XP** (independent subtotal error). All twelve wrong ⇒0/12,Bronze,48XP. A wrong destination and independently wrong reason/link are distinct authored judgments; descendant taxable/tax/balance changes are not extra worksheet points or penalties.

Main-case review processes earliest root before dependents: source/treatment → totals → taxable income → rule/tax → credit/balance → stamp. Proposed root keys are case-scoped (`R6-treatment`, `R7-treatment`, `trip-treatment`, `income-sum`, `expense-sum`, `taxable-operation`, `rule-selection`, `tax-formula`, `credit-source`, `balance-operation`, `stamp-revision`). A wrong descendant that correctly follows the player's erroneous ancestor is stale/needs correction, not another mistake. Blocked descendants are not judged until parents are corrected; an independent wrong operator/source after that is a new root. Deduplicate root IDs across repeated identical submissions/reloads. Approval requires all dependents correct/current; suppressing a penalty never makes an unsupported answer pass. Worksheet errors already graded at the field stage are not charged again by the main case.

Case XP settings remain exp100, expPerMistake2, expPerRetry1, maxExpLoss15: `100-min(15,2*uniqueNewRoots+retryCount+existingPunctualityLoss)`, minimum85. Retry means a distinct deliberate, complete, changed submission after a returned review; identical repeats, incomplete requests, loads and failed saves do not increment. Return feedback and correction are free of trust penalties. One expense root may cause one mistake debit and one actual retry debit, not separate taxable/tax/balance mistake debits. Hints and time do not create content roots. Preserve the existing punctuality policy within the same cap; no second deadline fine. No production validator is implemented by T01.

## Boundary and representative negative fixtures

Expected raw tax retains exact fractions below a cent. The content checker tests every bracket boundary at -0.01 / exact / +0.01, band identity and continuity. At a ceiling use the ending band; just above use the next band even if rounded tax is identical. TaxDue uses the cents policy from TAX_AUTHORITY.

| boundary | rawBelow | rawAt | rawAbove |
|---:|---:|---:|---:|
| 250000.00 | 0.00 | 0.00 | 0.0015 |
| 400000.00 | 22499.9985 | 22500.00 | 22500.0020 |
| 800000.00 | 102499.9980 | 102500.00 | 102500.0025 |
| 2000000.00 | 402499.9975 | 402500.00 | 402500.0030 |
| 8000000.00 | 2202499.9970 | 2202500.00 | 2202500.0035 |

| input | centsOutput | wholePesoReference |
|---:|---:|---:|
| 1.004 | 1.00 | 1 |
| 1.005 | 1.01 | 1 |
| 1.006 | 1.01 | 1 |
| 1.49 | 1.49 | 1 |
| 1.50 | 1.50 | 2 |
| 1.51 | 1.51 | 2 |

A taxable amount250,000.10 produces exact0.015 tax and cents0.02;250,000.03 produces0.0045 and0.00. At400,000.03 the exact tax22500.006 becomes22500.01. Do not round the 0.006 marginal amount before adding the base. Corporate small-company predicates are checked at5m±0.01 and100m±0.01 independently, plus both exact ceilings; the conjunction matters.

Each wrong example below is independently recomputed from the otherwise correct packet, except `bea-draft` deliberately combines its two authored draft errors. These numeric results must be rejected on support/rule/operation even if a later edit accidentally cancels them.

| fixtureId | wrongTaxable | wrongTaxDue | wrongBalance | root / meaning |
|---|---:|---:|---:|---|
| bea-personal | 576000.00 | 57700.00 | 45200.00 | Includes personal24000 cost only |
| bea-duplicate | 570000.00 | 56500.00 | 44000.00 | Counts printing30000 twice only |
| bea-draft | 546000.00 | 51700.00 | 39200.00 | Draft354000 costs, both wrong claims |
| bea-flat | 600000.00 | 120000.00 | 107500.00 | 20% of all taxable income, no base/excess |
| bea-credit-expense | 587500.00 | 60000.00 | 60000.00 | Credit deducted as cost, no credit afterward |
| bea-credit-both | 587500.00 | 60000.00 | 47500.00 | Credit treated as cost and again as credit |
| bea-extra-income | 850000.00 | 115000.00 | 102500.00 | Adds250000 certificate payment again |
| marine-draft | 5880000.00 | 1470000.00 | 1170000.00 | Costs2120000 include private trip |
| marine-small-rate | 6000000.00 | 1200000.00 | 900000.00 | Applies20% despite income ceiling failure |
| marine-credit-expense | 5700000.00 | 1425000.00 | 1425000.00 | Credit300000 as cost instead of credit |

Other mandatory later validator negatives: correct number with wrong case/year/payee/credit field, reusing B certificate for C, credit duplicated across tapes, missing evidence, same amount from a different transaction, R7 linked to R4, unsupported cancellation to a matching balance, stale rule/tape/stamp and a final number with no supported expression. T01 checks identity/link contracts, not gameplay rejection behavior. Marine correction increases taxable income120,000 and payable30,000; that does not establish intent.

## Existing loading and proposed schema

**Existing, inspected 30 October:** `JsonCaseLoader.LoadLevel/LoadCase` loads `Levels/{levelId}/level` and `Levels/{levelId}/cases/{caseId}` through `Resources.Load<TextAsset>` / `JsonUtility`. Case IDs in level.json are **resource stems**, not necessarily business case IDs (L2 uses `case_rosa_001` with internal `L2-ROSA-001`). `CaseManager` owns load; on error it can fall back to a placeholder. `CaseFactory` copies identity but leaves player computations blank; it does not copy answer keys. `CaseData` and `AnswerKeyDefinition` money are floats; TaxOption has only graduated/8%, TaxpayerType only individual categories. Do not force the corporation into SelfEmployed or invent a serialized enum value. The existing calculator clamps negatives and approximates graduated tax at20%; neither is acceptable as L3 authority.

`DocumentDefinition.fields` already has string `semanticKey`, `displayValue`, `correctValue`, label/type. `ReviewDocumentDefinition` adds source id, documentName, issuer/reference/date, paperType/title/status/totalKey and float lines, but belongs to L2 review. It has no automatic line-ID→semanticKey mapping. `SupportingPaperTypes` knows enum8 but reports it unimplemented; `CaseRecordRules.TypeForPaper` only converts4/5/7. `ClientRecordRow` has label/value only. Existing validators check L2 links/summary totals, not L3's transaction graph, exact money or credit provenance.

**Proposed smallest extension for later tickets:** optional `CaseDefinition.level3` object in each existing case envelope; no extra global loader, rule framework or separate resource catalogue. Two planned case resources are `Levels/Level_03/cases/case_bea_001` and `.../case_marine_001` (no extension in Resources paths), ordered in level.json. L3-only validation branches on `levelId == Level_03` and presence of this object; legacy cases remain on their present validation/factory path. Skip the individual-only default `GradedFields` fallback for L3 and use its explicit decision/field rules; a missing/invalid L3 block must fail closed in isolated development, never become an approved placeholder. T02 owns concrete DTO/loading/checkpoint code; T07/T12 own calculation validation.

| Proposed `level3` member | Serialized shape / requirement |
|---|---|
| schemaVersion, contentRevision | integer1, string `L3-2026-v1`; unknown versions fail validation |
| periodStart, periodEnd, taxpayerKind, regimeId, roundingPolicyId | ISO dates; `individual-professional` or `domestic-corporation`; IDs from this contract and TAX_AUTHORITY |
| corporateFacts | company only: operationsStart2024-01-01; assetsExcludingQualifyingLand `80000000.00`; ordinary/no incentive status explicit; no unknown-as-false defaults |
| sources[] | sourceId, documentName, paperType, reference, periodStart/end, issuer, party, status; use registry above; payer/payee for Type8, not compensation/employer aliases |
| rows[] | rowId, sourceId, fieldKey, transactionId, amount string, relation; registry above; unique same-case keys |
| totals[] | sourceId, fieldKey, componentRowIds list, amount string; six exact bindings above; components must belong to that source and sum exactly |
| creditLinks[] | creditRowId, incomeRowId, includedInRowId; B-WHT-CREDIT→B-WHT-INCOME→R2 and C-WHT-CREDIT→C-WHT-INCOME→C-R1 |
| rules[] | generic ruleId, lower/upper/baseTax/rate decimal strings from table; company rule constants scoped here; no float round-trip |
| worksheetDecisions[] | Bea only: twelve immutable expected decisions from the table, not player selections; company has no second worksheet grade |
| expectedFields[] | field, amount decimal string from expected table; immutable validator answers |

Use serializable fields/lists compatible with JsonUtility, no dictionaries or decimal JSON members. Money wire format is invariant `^-?\d+\.\d{2}$`, rates decimal strings; parse with invariant `decimal` later, validate bounds/overflow and permitted operations before evaluation. Intermediate tape results may have more decimal places and must preserve exact precision/provenance. Bounds for this bounded content: source/output magnitude at most100,000,000.00, rate0..1; safe arithmetic must report overflow, not truncate. The phase/tape engine is later work.

Sources map exactly to top-level document names; rows map exactly to field semantic keys **within that document**, not labels/row indices or parsed currency display strings. Type8 income and credit are two distinct number fields; payer/payee/period/reference are text fields. Summary total fields use additional stable keys `BRevenueTotal`, `BCostTotal`, `BDraftTotal`, `CRevenueTotal`, `CCostTotal`, `CDraftTotal`; totals are assertions over named component rows, not new transactions. Deliberate wrong draft amounts are claim fields, not incorrect copies of originals. Do not put expected treatment or numeric client solutions into book leaves or visible `potentialIssues`.

Load validation must check exact requested/internal case identity, year/period containment, unique IDs, document/field existence, canonical amount agreement, bounded decimal syntax, related-row ownership and shared transaction/amount on support edges, subset<=parent and matching payee/year, credit uniqueness, total components, decision IDs and expected chains. No relation cycles; a support edge is not a counted-cost edge. Do not reuse L2's role=`income` summary sum check on expense/asset papers. Working-paper kind explicitly permits type0 without pretending to implement a source type; Type8 must wait for actual T08 presentation. Book leaves use the existing `LevelBookLeaves` per-level append shape (`sections`, heading/body/clickableValues).

Only player choices, acquired source IDs, source revision/fingerprint, attached tape expressions/edges/results/destinations, selected rule ID and frozen score are saved by the future chapter snapshot. Resolve immutable source facts and expected answers from current content after load. Do not serialize `level3`, `expectedFields`, expected decisions, rule answer selections or answer keys in Bag items/copies/checkpoints. A player's own approved results may appear in the computation copy; that is different from shipping the complete validator key in inventory. Two per-case records remain separate; content revision mismatch requires revalidation/stale state, not silent rebinding. This is a design contract, not a claim that save round trips have been implemented/tested.

T02 can now build the isolated two-case identity/content DTO and resumable state against these bindings, keeping normal Career unavailable. It must not resolve educator approval by inventing ATCs, changing the approved credit or treating internal arithmetic tests as content sign-off. T03 onward implements the interactions in ticket order; T01 creates no resources or scene.
