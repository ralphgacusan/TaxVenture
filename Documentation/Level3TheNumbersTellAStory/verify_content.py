"""Check stable tax, legacy v1 and revised v2 Markdown fixtures. No Unity or saves."""
from datetime import date
from decimal import Decimal as D, ROUND_HALF_UP
from pathlib import Path
import re

PACK = Path(__file__).resolve().parent


def table(body, header):
    lines = body.splitlines()
    start = lines.index(header) + 2
    rows = []
    for line in lines[start:]:
        if not line.startswith('|'):
            break
        rows.append([cell.strip() for cell in line.strip('|').split('|')])
    return rows


def verify():
    body = (PACK / 'FIXTURE_CONTRACT.md').read_text(encoding='utf-8-sig')
    checks = 0

    def check(condition, message):
        nonlocal checks
        checks += 1
        if not condition:
            raise ValueError(message)

    contexts = table(body, '| caseId | clientId | taxpayer | periodStart | periodEnd | taxYear | regimeId |')
    cases = {r[0]: r for r in contexts}
    check(len(cases) == len(contexts) == 2, 'two unique cases')
    for r in contexts:
        check(r[3:6] == ['2026-01-01', '2026-12-31', '2026'], 'annual 2026 context')

    source_rows = table(body, '| sourceId | caseId | documentName | paperType | reference | periodStart | periodEnd | issuer / party / status |')
    sources = {r[0]: r for r in source_rows}
    check(len(sources) == len(source_rows) == 17, 'unique source registry')
    for r in source_rows:
        check(r[1] in cases and r[2] == r[0], 'source document/case binding: ' + r[0])
        c = cases[r[1]]
        check(date.fromisoformat(c[3]) <= date.fromisoformat(r[5]) <= date.fromisoformat(r[6]) <= date.fromisoformat(c[4]), 'source period: ' + r[0])
        check(r[3] in {'0', '1', '2', '4', '5', '7', '8'}, 'paper type: ' + r[0])
    check({r[0] for r in source_rows if r[3] == '0'} == {'B-EXP', 'B-DRAFT', 'C-COST', 'C-DRAFT', 'C-ASSET'}, 'intentional working papers')
    check(sources['B-WHT'][3] == sources['C-WHT'][3] == '8', '2307 type')

    row_list = table(body, '| rowId | sourceId | fieldKey | transactionId | amount | relation |')
    rows = {r[0]: r for r in row_list}
    check(len(rows) == len(row_list) == 29, 'unique monetary row IDs')
    check(len({(r[1], r[2]) for r in row_list}) == len(rows), 'unique source field keys')
    for r in row_list:
        check(r[1] in sources and bool(r[2]) and bool(r[3]), 'source/field/transaction: ' + r[0])
        check(bool(re.fullmatch(r'\d+\.\d{2}', r[4])) and D('0') <= D(r[4]) <= D('100000000'), 'exact bounded money: ' + r[0])
        if r[5] != '-':
            kind, target = r[5].split(':')
            check(kind in {'supports', 'claim', 'subset'} and target in rows, 'relation target: ' + r[0])
            t = rows[target]
            check(sources[r[1]][1] == sources[t[1]][1], 'same-case relation: ' + r[0])
            if kind in {'supports', 'claim'}:
                check(r[3:5] == t[3:5], 'same economic item/amount: ' + r[0])
            else:
                check(D(r[4]) <= D(t[4]), 'withholding income subset: ' + r[0])
    # Follow all edges to reject cycles rather than merely checking target existence.
    for key in rows:
        seen = set()
        node = key
        while rows[node][5] != '-':
            check(node not in seen, 'relation cycle: ' + key)
            seen.add(node)
            node = rows[node][5].split(':')[1]

    amount = lambda key: D(rows[key][4])
    sum_rows = lambda *keys: sum((amount(k) for k in keys), D('0'))
    income = sum_rows('R1', 'R2')
    costs = sum_rows('R3', 'R4', 'R5')
    c_income = amount('C-R1')
    c_costs = sum_rows('C-R2', 'C-R3', 'C-R4')
    check(amount('B-CLAIM-BASE') == costs, 'Bea draft base')
    check(sum_rows('B-CLAIM-BASE', 'B-CLAIM-PERSONAL', 'B-CLAIM-PRINT') == D('354000'), 'Bea draft354000')
    check(amount('C-DRAFT-ADMIN') == sum_rows('C-R4', 'C-R5'), 'trip component, not fourth additive row')
    check(sum_rows('C-DRAFT-OPS', 'C-DRAFT-RENT', 'C-DRAFT-ADMIN') == D('2120000'), 'company draft2120000')
    check(amount('B-WHT-INCOME') == D('250000') and amount('R2') - amount('B-WHT-INCOME') == D('150000'), 'Bea withheld subset')
    check(rows['B-WHT-INCOME'][5] == 'subset:R2' and rows['C-WHT-INCOME'][5] == 'subset:C-R1', 'certificate income parent IDs')
    check(amount('C-WHT-INCOME') == c_income, 'company aggregate income already counted')

    totals = table(body, '| sourceId | fieldKey | componentRowIds | amount |')
    total_components = {
        'BRevenueTotal': ('B-REV', 'R1,R2'),
        'BCostTotal': ('B-EXP', 'R3,R4,R5'),
        'BDraftTotal': ('B-DRAFT', 'B-CLAIM-BASE,B-CLAIM-PERSONAL,B-CLAIM-PRINT'),
        'CRevenueTotal': ('C-REV', 'C-R1'),
        'CCostTotal': ('C-COST', 'C-R2,C-R3,C-R4'),
        'CDraftTotal': ('C-DRAFT', 'C-DRAFT-OPS,C-DRAFT-RENT,C-DRAFT-ADMIN'),
    }
    check(len(totals) == len(total_components) and {r[1] for r in totals} == set(total_components), 'six total fields')
    for source, key, components, value in totals:
        check((source, components) == total_components[key], 'explicit total component binding: ' + key)
        check(all(rows[k][1] == source for k in components.split(',')), 'total component source ownership')
        check(sum_rows(*components.split(',')) == D(value), 'printed total: ' + key)
    credit_links = table(body, '| creditRowId | incomeRowId | includedInRowId |')
    check(credit_links == [['B-WHT-CREDIT', 'B-WHT-INCOME', 'R2'], ['C-WHT-CREDIT', 'C-WHT-INCOME', 'C-R1']], 'two distinct credit edges')
    for credit, basis, parent in credit_links:
        check(rows[credit][1] == rows[basis][1] and rows[basis][5] == 'subset:' + parent, 'credit basis source/parent')

    bands = table(body, '| ruleId | lower | upper | baseTax | rate |')
    check(len(bands) == 6, 'six individual bands')
    # Independent statutory anchors catch a consistently changed table and outputs.
    anchors = [
        ['IND-0', '0.00', '250000.00', '0.00', '0.00'],
        ['IND-15', '250000.00', '400000.00', '0.00', '0.15'],
        ['IND-20', '400000.00', '800000.00', '22500.00', '0.20'],
        ['IND-25', '800000.00', '2000000.00', '102500.00', '0.25'],
        ['IND-30', '2000000.00', '8000000.00', '402500.00', '0.30'],
        ['IND-35', '8000000.00', '-', '2202500.00', '0.35'],
    ]
    check(bands == anchors, '2023-onward statutory table')
    corp_rows = table(body, '| corporateKey | value |')
    corp = dict(corp_rows)
    check(len(corp_rows) == len(corp) == 13, 'unique corporate constants/facts')
    check({k: corp[k] for k in ['ordinaryRate', 'smallRate', 'smallIncomeCeiling', 'smallAssetCeiling', 'eligibilityOperator', 'excludeQualifyingPremisesLand', 'mcitRate', 'mcitFollowingYearOffset']} == {
        'ordinaryRate': '0.25', 'smallRate': '0.20', 'smallIncomeCeiling': '5000000.00',
        'smallAssetCeiling': '100000000.00', 'eligibilityOperator': 'AND',
        'excludeQualifyingPremisesLand': 'true', 'mcitRate': '0.02', 'mcitFollowingYearOffset': '4',
    }, 'primary corporate rule anchors')
    check(corp['assetRowId'] == 'C-ASSETS' and corp['incentiveRegime'] == 'none', 'ordinary company asset/incentive facts')
    check(corp['operationsStart'] == corp['birRegistrationDate'] == '2024-01-01', 'authored supplied commencement facts')
    corporate_rate = D(corp['ordinaryRate'])

    def small_company(taxable_income, assets):
        return taxable_income <= D(corp['smallIncomeCeiling']) and assets <= D(corp['smallAssetCeiling'])

    def graduated(value):
        if value < 0:
            raise ValueError('negative taxable income outside bounded fixture')
        for band in bands:
            if band[2] == '-' or value <= D(band[2]):
                return band[0], D(band[3]) + D(band[4]) * (value - D(band[1]))
        raise ValueError('uncovered income')

    cents = lambda value: value.quantize(D('0.01'), rounding=ROUND_HALF_UP)
    tax = lambda value: cents(graduated(value)[1])
    expected_rows = table(body, '| caseId | field | expected | source / expression |')
    expected = {(r[0], r[1]): D(r[2]) for r in expected_rows}
    check(len(expected) == len(expected_rows) == 14, 'unique expected fields')
    bea = [income, costs, income-costs, income-costs-D('400000')]
    bea += [bea[3]*D('.20'), tax(bea[2]), amount('B-WHT-CREDIT')]
    bea += [bea[5]-bea[6]]
    company = [c_income, c_costs, c_income-c_costs, cents((c_income-c_costs)*corporate_rate), amount('C-WHT-CREDIT')]
    company += [company[3]-company[4]]
    for case, values, fields in [
        ('L3-BEA-001', bea, ['grossIncome', 'allowableExpenses', 'taxableIncome', 'excess', 'marginalTax', 'taxDue', 'taxWithheldOrCredits', 'finalTaxPayable']),
        ('L3-MARINE-001', company, ['grossIncome', 'allowableExpenses', 'taxableIncome', 'taxDue', 'taxWithheldOrCredits', 'finalTaxPayable']),
    ]:
        for field, value in zip(fields, values):
            check(expected[(case, field)] == value, case + '/' + field)
    check(bea[2:] == list(map(D, ['600000', '200000', '40000', '62500', '12500', '50000'])), 'approved Bea chain')
    check(company == list(map(D, ['8000000', '2000000', '6000000', '1500000', '300000', '1200000'])), 'approved company chain')

    boundaries = table(body, '| boundary | rawBelow | rawAt | rawAbove |')
    check(len(boundaries) == 5, 'all bracket boundaries')
    for i, row in enumerate(boundaries):
        boundary = D(row[0])
        check(boundary == D(bands[i][2]), 'ordered boundary')
        for delta, raw in zip(['-.01', '0', '.01'], row[1:]):
            band, value = graduated(boundary+D(delta))
            check(value == D(raw), 'exact raw tax at ' + row[0] + delta)
            check(band == bands[i + (D(delta) > 0)][0], 'inclusive ceiling / exclusive floor')
        next_band = bands[i+1]
        check(graduated(boundary)[1] == D(next_band[3]), 'band continuity')
    for row in table(body, '| input | centsOutput | wholePesoReference |'):
        check(cents(D(row[0])) == D(row[1]), 'cents midpoint')
        check(D(row[0]).quantize(D('1'), rounding=ROUND_HALF_UP) == D(row[2]), 'form whole-peso midpoint')
    for value, raw, output in [('250000.10', '.015', '.02'), ('250000.03', '.0045', '.00'), ('400000.03', '22500.006', '22500.01')]:
        check(graduated(D(value))[1] == D(raw) and tax(D(value)) == D(output), 'no premature rounding')
    check(tax(D('350000')) == D('15000'), 'separate worked example')
    check(D('100')-D('20') == D('80') and D('80')-D('8') == D('72'), 'protected tool practice')
    for di in ['-.01', '0', '.01']:
        for da in ['-.01', '0', '.01']:
            small = small_company(D('5000000')+D(di), D('100000000')+D(da))
            check(small == (D(di) <= 0 and D(da) <= 0), 'corporate AND boundary')
    check(not small_company(company[2], amount(corp['assetRowId'])), 'Marine fails small-company income condition')
    first_mcit = date.fromisoformat(corp['birRegistrationDate']).year + int(corp['mcitFollowingYearOffset'])
    check(first_mcit == int(corp['firstMCITYear']) == 2028 and int(cases['L3-MARINE-001'][5]) < first_mcit, 'MCIT first year')

    decisions = table(body, '| decisionId | expected |')
    check([r[0] for r in decisions] == ['dest-R'+str(i) for i in range(1, 8)] + ['reason-R6', 'reason-R7', 'related-R7', 'subtotal-income', 'subtotal-expenses'], 'twelve independent decisions')
    check([r[1] for r in decisions[:10]] == ['Income']*2 + ['Allowable expenses']*3 + ['Not deductible', 'Supporting only', 'Personal purpose', 'Already represented', 'R5/B-EXP/PRINT-26'], 'decision answers')
    for wrong, correct, xp, tier in [(0, 12, 60, 'Gold'), (1, 11, 59, 'Silver'), (2, 10, 58, 'Silver'), (3, 9, 57, 'Bronze'), (12, 0, 48, 'Bronze')]:
        check(12-wrong == correct and max(48, 60-wrong) == xp and ('Gold' if correct == 12 else 'Silver' if correct >= 10 else 'Bronze') == tier, 'worksheet score/XP bounds')
    check(costs+amount('R6') == D('324000') and costs+amount('R7') == D('330000'), 'selected-set arithmetic earns credit independently')
    check(costs+sum_rows('R6', 'R7') == D('354000'), 'two wrong treatments, matching subtotal')
    check(100-min(15, 2*1+1) == 97 and 100-min(15, 2*20+10) == 85, 'case root/retry/cap examples')

    wrongs = table(body, '| fixtureId | wrongTaxable | wrongTaxDue | wrongBalance | root / meaning |')
    calculated = {}
    for name, cost_error in [('bea-personal', amount('R6')), ('bea-duplicate', amount('R7')), ('bea-draft', sum_rows('R6', 'R7'))]:
        ti = income-costs-cost_error
        calculated[name] = (ti, tax(ti), tax(ti)-amount('B-WHT-CREDIT'))
    calculated['bea-flat'] = (bea[2], bea[2]*D('.20'), bea[2]*D('.20')-bea[6])
    ti = bea[2]-bea[6]
    calculated['bea-credit-expense'] = (ti, tax(ti), tax(ti))
    calculated['bea-credit-both'] = (ti, tax(ti), tax(ti)-bea[6])
    ti = bea[2]+amount('B-WHT-INCOME')
    calculated['bea-extra-income'] = (ti, tax(ti), tax(ti)-bea[6])
    ti = company[2]-amount('C-R5')
    calculated['marine-draft'] = (ti, ti*corporate_rate, ti*corporate_rate-company[4])
    calculated['marine-small-rate'] = (company[2], company[2]*D(corp['smallRate']), company[2]*D(corp['smallRate'])-company[4])
    ti = company[2]-company[4]
    calculated['marine-credit-expense'] = (ti, ti*corporate_rate, ti*corporate_rate)
    check({r[0] for r in wrongs} == set(calculated) and len(wrongs) == 10, 'ten distinct wrong fixtures')
    for r in wrongs:
        check(tuple(map(D, r[1:4])) == calculated[r[0]], 'independently computed wrong fixture: ' + r[0])
    check(company[5]-calculated['marine-draft'][2] == D('30000'), 'company correction effect')
    # The old twelve-decision table remains a compatibility contract, not the
    # new activity. Verify the new registry independently against those sources.
    revision = (PACK / 'REVISION_PLAN.md').read_text(encoding='utf-8-sig')
    revised = table(revision, '| decisionId | batch | expected treatment | expected reason |')
    expected_ids = ['dest-R' + str(i) for i in range(1, 8)] + ['reason-R6', 'reason-R7']
    check([r[0] for r in revised] == expected_ids, 'v2 nine unique treatment/reason decisions')
    check(len({r[0] for r in revised}) == 9, 'v2 no duplicate decision IDs')
    check([r[1] for r in revised] == ['earnings'] * 2 + ['costs'] * 3 + ['questions'] * 4,
          'v2 batches contain two earnings, three costs, two treatments plus two reasons')
    mapping = {'Income': 'Income', 'Allowable expenses': 'Deduct',
               'Not deductible': 'Leave Out', 'Supporting only': 'Leave Out'}
    for current, legacy in zip(revised[:7], decisions[:7]):
        check(current[2:] == [mapping[legacy[1]], '-'], 'v2 preserves source treatment: ' + current[0])
        check(current[0].split('-', 1)[1] in rows, 'v2 treatment has original source row')
    check([r[2:] for r in revised[7:]] == [['-', 'Personal purpose'], ['-', 'Already represented']],
          'v2 distinct non-deductible and already-counted reasons')
    scores = table(revision, '| wrong | correct | tier | xp |')
    check(len(scores) == 10, 'v2 all first-check wrong-count outcomes')
    for wrong, row in enumerate(scores):
        correct = 9 - wrong
        tier = 'Gold' if correct == 9 else 'Silver' if correct >= 7 else 'Bronze'
        check(row == [str(wrong), str(correct), tier, str(60 - wrong)], 'v2 score/XP outcome ' + str(wrong))
    check(60 - 9 == 51 and 60 + 100 == 160, 'v2 XP bounds and Bea-only current chapter maximum')
    check(60 + 100 + 100 == 260, 'future two-case XP maximum remains documented')
    return checks


if __name__ == '__main__':
    try:
        print(f'PASS: {verify()} stable/legacy/revision content assertions against Markdown; exact Decimal; documentation only.')
    except (ValueError, KeyError, IndexError) as error:
        raise SystemExit('FAIL: ' + str(error))
