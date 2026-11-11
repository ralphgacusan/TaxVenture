"""Offline geometry evidence, not a TMP render or phone readability test.

Reads the shipped HUD font's actual glyph/face metrics and the dimensions
used by FixedPrint. Checks advance, bold allowance and complete line height
in meters, including the largest permitted tape amount. No Unity launch.
"""
import ast
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]


def constants(name):
    result = {}
    source = (ROOT / 'Assets/Scripts/Level3' / name).read_text()
    for key, expression in re.findall(r'const (?:float|int) (\w+) = ([^;]+);', source):
        expression = re.sub(r'(\d)f\b', r'\1', expression)
        tree = ast.parse(expression, mode='eval')
        if all(isinstance(n, (ast.Expression, ast.BinOp, ast.Constant, ast.Name, ast.Load, ast.Mult, ast.Add, ast.Sub, ast.Div)) for n in ast.walk(tree)):
            try:
                result[key] = eval(compile(tree, '<dimensions>', 'eval'), {'__builtins__': {}}, result)
            except NameError:
                pass
    return result


hud = (ROOT / 'Assets/Resources/HUD/HudFonts.asset').read_text()
guid = re.search(r'uiFont: .*guid: (\w+)', hud)[1]
font_meta = next(p for p in (ROOT / 'Assets').rglob('*.asset.meta') if 'guid: ' + guid in p.read_text())
font = font_meta.with_suffix('').read_text()
glyphs = {int(i): float(a) for i, a in re.findall(r'- m_Index: (\d+).*?m_HorizontalAdvance: ([\d.]+)', font, re.S)}
chars = {int(u): int(i) for u, i in re.findall(r'm_Unicode: (\d+)\s+m_GlyphIndex: (\d+)', font)}
point = float(re.search(r'm_PointSize: ([\d.]+)', font)[1])
line = float(re.search(r'm_LineHeight: ([\d.]+)', font)[1])
scale = constants('Level3DeskProps.cs')['PrintScale']
card = constants('Level3PracticeCardModel.cs')
tape = constants('Level3CalculatorModel.cs')


checks = 0


def check(text, points, width, height, lines=1):
    global checks
    # Installed TextMeshPro.GenerateTextMesh uses 0.1 in perspective mode.
    # Reserve 4% beyond advance for bold face expansion/padding.
    advance = sum(glyphs[chars[ord(c)]] for c in text) * points / point * 0.1 * scale * 1.04
    full_height = line * points / point * 0.1 * scale * lines
    assert advance <= width and full_height <= height, (text, advance, width, full_height, height)
    checks += 1
    print(f'PASS {text}: {advance:.5f}/{width:.5f}m wide, {full_height:.5f}/{height:.5f}m high')


# Teaching pass (6 November 2026): the three example cards (taxable income, Practice Design Studio tax table,
# withholding) print on the same clipboard at the same sizes; longer cards hold more rows, type never shrinks.
for value in ('10,000.00', '2,000.00', '350,000.00', '250,000.00', '15%', '0.00', '15,000.00', '20,000.00'):
    check(value, card['AmountPoints'], card['AmountWidth'], card['AmountHeight'])
for value in ('8,000.00', '15,000.00', '13,000.00'):
    check(value, card['ResultPoints'], card['ResultWidth'], card['ResultHeight'])
for value in ('10,000.00', '- 2,000.00', '= 8,000.00', '350,000.00', '- 250,000.00', '× 15.00%', '+ 0.00',
              '= 15,000.00', '15,000.00', '= 13,000.00', '- 999,999,999,999.123456'):
    check(value, tape['TapeMinPoints'] if '999,999' in value else tape['TapePoints'], tape['TapePrintWidth'], tape['TapePageHeight'], tape['TapeLinesShown'])
for text in ('Income', 'Costs (supported)', 'Taxable income', 'Threshold', 'Rate', 'Base tax', 'Tax due', 'Income payment', 'Tax withheld', 'Still to pay'):
    check(text, card['LabelPoints'], card['AmountWidth'], card['LabelHeight'])
for text in ("Not Bea's · No score", 'Band: over 250,000 to 400,000', 'A credit, not a cost'):
    check(text, card['NotePoints'], card['AmountWidth'], card['NoteHeight'])
for title in ('EXAMPLE', 'TAXABLE INCOME', 'DESIGN STUDIO', 'WITHHOLDING'):
    check(title, 105.0, card['AmountWidth'], 0.025 / 2)
# Compact rows (three or four amounts): the label and amount lines' printed heights fit inside one pitch.
label_h = line * card['LabelPoints'] / point * 0.1 * scale
amount_h = line * card['AmountPoints'] / point * 0.1 * scale
assert (0.0065 + label_h / 2) + (0.0085 + amount_h / 2) <= card['CompactPitch'], ('compact rows', label_h, amount_h)
checks += 1
print(f'PASS compact lesson row {(0.0065 + label_h / 2) + (0.0085 + amount_h / 2):.4f}/{card["CompactPitch"]:.4f}m')


def wrapped_lines(text):
    """Measure complete guidance with real font advances, not a character budget."""
    result = []
    def advance(text):
        return sum(glyphs[chars[ord(c)]] for c in text) * card['MessagePoints'] / point * 0.1 * scale * 1.04
    for paragraph in text.split('\n'):
        current = ''
        for word in paragraph.split():
            next_line = (current + ' ' + word).strip()
            if current and advance(next_line) > card['AmountWidth']:
                result.append(current)
                current = word
            else:
                current = next_line
        result.append(current)
    return result


messages = [
    'Tap amount, then display;\nor drag. Move by the edges.',
    'Use - and both amounts;\nthen press =.',
    'Taxable income - threshold =,\n× rate =, + base tax =.',
    'Tax due - tax withheld, then =.\nThe payment is already income.',
    "Done. The rate taxed only\nthe excess. Now Bea's band.",
    "Done. A credit comes off tax,\nnot income. Now Bea's credit.",
    'Follow the steps;\npress = after each one.',
    'Choose -, then tax withheld;\nthen press =.',
    'Keep going: next step,\nthen = again.',
    'Watch Arthur first.',
    'Not yet. Try again;\nthe tape is still here.',
    'Taxable income is income minus supported costs. Start from Income.',
    'Multiply the excess by the rate: × Rate, then =.',
    'Apply the rate only to the excess: subtract the threshold first.',
    "Add the band's base tax last, even when it is 0: + Base tax, then =.",
    'Follow the order: taxable income - threshold =, × rate =, + base tax =.',
    'The income payment is already in income. Take away only the tax withheld.',
    'Withheld tax is already paid: take it away from tax due.',
    'Start from tax due, then take away the tax withheld.',
    'Drag tape into the field;\nor tap tape, then field.',
    "Done. Bea's sheet comes up next.",
    'Tap the amounts on the card, so the tape shows where each one came from.',
    "Couldn't save. The example tape is still on the calculator: tap the slot again.",
    'Work it out on the calculator: use the card\'s amounts and an operation, then press =.',
]
for message in messages:
    rows = wrapped_lines(message)
    for row in rows:
        check(row, card['MessagePoints'], card['AmountWidth'], card['MessageHeight'], len(rows))
# Revised T04: Bea's stand card and the passive machine labels, from the case's own printed values.
# The peso sign is not in the baked atlas (dynamic fallback, as on the record papers): it is measured
# with the wider 'P' advance, so the real line is no wider than this check.
import json
machine = constants('Level3PaperMachineModel.cs')
case = json.loads((ROOT / 'Assets/Resources/Levels/Level_03/cases/case_bea_001.json').read_text(encoding='utf-8'))
content = case['level3']
documents = {d['documentName']: d for d in case['documents']}
for row in [r for r in content['rows'] if r['rowId'] in ('R1', 'R2', 'R3', 'R4', 'R5', 'R6', 'R7')]:
    source = next(x for x in content['sources'] if x['sourceId'] == row['sourceId'])
    field = next(f for f in documents[source['documentName']]['fields'] if f.get('semanticKey') == row['fieldKey'])
    period = source['periodStart'] if source['periodStart'] == source['periodEnd'] else str(content['taxYear'])
    status = source['status'].strip()
    check(field['label'], machine['TitlePoints'], machine['CardTextWidth'], machine['TitleHeight'])
    check(field['displayValue'].replace('\u20b1', 'P'), machine['AmountPoints'], machine['CardTextWidth'], machine['AmountHeight'])
    check(status[0].upper() + status[1:], machine['PurposePoints'], machine['CardTextWidth'], machine['PurposeHeight'])
    for detail_line in (source['issuer'], ' \u00b7 '.join([period, source['reference']])):
        check(detail_line, machine['DetailsPoints'], machine['CardTextWidth'], machine['DetailsHeight'], 2)
for position in ('Record 1 of 2', 'Record 3 of 3'):
    check(position, machine['PositionPoints'], machine['CardTextWidth'], machine['PositionHeight'])
check('INSPECT ORIGINAL', machine['TabPoints'], machine['CardTextWidth'], machine['TabHeight'])
for label in ('Income', 'Deduct', 'Leave Out'):
    check(label, machine['DialLabelPoints'], machine['DialLabelWidth'], machine['DialLabelHeight'])
for label in ('INCOME', 'DEDUCTIONS'):
    check(label, machine['WindowLabelPoints'], machine['WindowWidth'], machine['WindowLabelHeight'])
# T05 (4 November 2026): the working machine's labels, the note card, the Why? card and the review tabs,
# from the shipped constants and the case's own printed values (never auto-shrunk).
for label in ('Income', 'Deduct', 'Leave Out'):
    check('Chosen: ' + label, machine['ChosenPoints'], machine['ChosenWidth'], machine['ChosenHeight'])
for value in ('P1,254,000.00', 'P900,000.00', 'P0.00'):
    check(value, machine['WindowValuePoints'], machine['WindowWidth'] - 0.006, machine['WindowValueHeight'])
check('WHY?', machine['WhyLabelPoints'], 0.06, 0.024)
reasons = ('Business purpose', 'Personal purpose', 'Already represented', 'Different reporting period', 'Need supporting record')
for reason in reasons + ('Choose a reason',):
    check(reason, machine['WhyValuePoints'], machine['WhyValueWidth'], 0.018)
for reason in reasons:
    check(reason, machine['WhyOptionPoints'], machine['CardTextWidth'] - 0.008, machine['WhyOptionHeight'] - 0.004)
check('WHY?', machine['WhyTitlePoints'], machine['CardTextWidth'], machine['WhyTitleHeight'])
check('CLOSE', machine['TabPoints'], 0.09, machine['WhyCloseHeight'])
check('COMPARE', machine['TabPoints'], machine['NoteHalfTabWidth'] - 0.008, machine['NoteTabHeight'])
for split_word in ('INSPECT', 'ORIGINAL'):
    check(split_word, machine['NoteSplitPoints'], machine['NoteHalfTabWidth'] - 0.008, machine['NoteTabHeight'] / 2)
check('INSPECT ORIGINAL', machine['TabPoints'], machine['CardTextWidth'] - 0.016 + 0.008, machine['NoteTabHeight'])
titles = []
for row in [r for r in content['rows'] if r['rowId'] in ('R1', 'R2', 'R3', 'R4', 'R5', 'R6', 'R7')]:
    source = next(x for x in content['sources'] if x['sourceId'] == row['sourceId'])
    titles.append(next(f for f in documents[source['documentName']]['fields'] if f.get('semanticKey') == row['fieldKey'])['label'])
for title in titles:
    check(title, machine['PurposePoints'], machine['CardTextWidth'], machine['PurposeHeight'])
    words, lines, current = title.split(), [], ''
    for word in words:
        trial = (current + ' ' + word).strip()
        width = sum(glyphs[chars[ord(c)]] for c in trial) * machine['RackTitlePoints'] / point * 0.1 * scale * 1.04
        if current and width > machine['RackTitleWidth']:
            lines.append(current)
            current = word
        else:
            current = trial
    lines.append(current)
    assert len(lines) <= 2, (title, lines)
    for line_text in lines:
        check(line_text, machine['RackTitlePoints'], machine['RackTitleWidth'], machine['RackTitleHeight'], len(lines))
for label in ('Income', 'Deduct', 'Leave Out'):
    check(label, machine['RackCategoryPoints'], machine['RackTitleWidth'], machine['RackCategoryHeight'])
rows_height = sum(machine[k] for k in ('CardTop', 'PositionHeight', 'TitleHeight', 'AmountHeight', 'PurposeHeight', 'DetailsHeight')) + machine['CardRowGap'] * 4
assert rows_height + machine['TabHeight'] + 0.008 <= machine['CardHeight'], (rows_height, machine['CardHeight'])
checks += 1
print(f'PASS card rows {rows_height + machine["TabHeight"] + 0.008:.4f}/{machine["CardHeight"]:.4f}m high, tab clear of the text')
# Revised T06 (5 November 2026): the batch card, the review mark, the note card's look-again line,
# the folded summary's front and the summary paper's rows (the existing record face, UI units).
def advance_m(text, points):
    return sum(glyphs[chars[ord('P' if c == '\u20b1' else c)]] for c in text) * points / point * 0.1 * scale * 1.04
def wrap_m(text, points, width):
    out = []
    for paragraph in text.split('\n'):
        current = ''
        for word in paragraph.split():
            trial = (current + ' ' + word).strip()
            if current and advance_m(trial, points) > width:
                out.append(current)
                current = word
            else:
                current = trial
        out.append(current)
    return out
def check_wrapped(text, points, width, height):
    rows = wrap_m(text, points, width)
    for row in rows:
        check(row.replace('\u20b1', 'P'), points, width, height, len(rows))
for key in ('CHECK BATCH', 'CHECK AGAIN', 'COLLECT SUMMARY', 'CLOSE'):
    check(key, machine['BatchKeyPoints'], machine['CardTextWidth'], machine['BatchKeyHeight'])
for position in ('Batch 1 of 3', 'Batch 3 of 3', 'All batches checked', 'Read only'):
    check(position, machine['PositionPoints'], machine['CardTextWidth'], machine['PositionHeight'])
for title in ('Earnings', 'Business costs', 'Questionable records', "Bea's summary", 'Income total', 'Deductions total'):
    check(title, machine['TitlePoints'], machine['CardTextWidth'], machine['TitleHeight'])
for status in ('3 of 3 filed', '1 of 2 right \u00b7 1 marked ?', '3 of 4 right \u00b7 1 marked ?', 'Refiled. Check again to confirm.', 'All 4 right \u00b7 accepted',
               'Ready on the machine\'s ledge', 'Still on the machine\'s ledge', 'P1,254,000.00 \u00b7 7 records'):
    check(status, machine['BatchStatusPoints'], machine['CardTextWidth'], machine['BatchStatusHeight'])
hints = json.loads((ROOT / 'Assets/Resources/Levels/Level_03/worksheet.json').read_text(encoding='utf-8'))['hints']
labels = {r['rowId']: titles[i] for i, r in enumerate([r for r in content['rows'] if r['rowId'] in ('R1', 'R2', 'R3', 'R4', 'R5', 'R6', 'R7')])}
bodies = ['Ready to check. A tab brings a record back first.', 'Your first check stays as it was.',
          'Its records are read only now. Tap a total to see what\'s in it.', 'Tap the folded card to read it. Collect it for her office visit.',
          "Couldn't save the check, so nothing was graded. Your records are as they were: tap CHECK BATCH again.",
          'Nothing has changed since your last check.', "Bea's records changed meanwhile. Look at the tabs, then check again."]
for hint in hints:
    if not hint.get('brief'):
        continue
    for decision in hint['decisions']:
        row = decision.split('-')[1]
        label = labels[row] + (' (reason)' if decision.startswith('reason-') else '')
        bodies.append(label + ': ' + hint['brief'] + '\nTap a ? tab to look again.')
        bodies.append(label + ': ' + hint['lines'][1])
        # The note card's two-line look-again line (where to look, never the answer).
        rows = wrap_m('Look again: ' + hint['brief'], machine['NoteHintPoints'], machine['CardTextWidth'])
        assert len(rows) <= 2, rows
        for line_text in rows:
            check(line_text, machine['NoteHintPoints'], machine['CardTextWidth'], machine['NoteHintHeight'], len(rows))
# Teaching pass (6 November 2026): the practice packet's stand cards and its note-card "Look again" lines,
# read from the shipped Level3SortingPractice source (same machine card constants as Bea's records).
practice_src = (ROOT / 'Assets/Scripts/Level3/Level3SortingPractice.cs').read_text(encoding='utf-8')
slips = re.findall(r'Title = "([^"]+)", Amount = "(\d+)", Purpose = "([^"]+)"', practice_src)
assert len(slips) == 4, slips
for title, amount, purpose in slips:
    check(title, machine['TitlePoints'], machine['CardTextWidth'], machine['TitleHeight'])
    check('P' + f'{int(amount):,}' + '.00', machine['AmountPoints'], machine['CardTextWidth'], machine['AmountHeight'])
    check(purpose, machine['PurposePoints'], machine['CardTextWidth'], machine['PurposeHeight'])
for position in ('Practice 1 of 4', 'Practice 4 of 4'):
    check(position, machine['PositionPoints'], machine['CardTextWidth'], machine['PositionHeight'])
check(re.search(r'Details = "([^"]+)"', practice_src)[1], machine['DetailsPoints'], machine['CardTextWidth'], machine['DetailsHeight'], 2)
# Follow-up (Arthur first): practice tips are Arthur's bubble lines now, not note-card print, so they are not measured here.
ledger = '\n'.join(t + ' P' + v for t, v in [('Menu illustration fees', '400,000.00'), ('Printing and supplies', '30,000.00'), ('Design subscriptions', '90,000.00'),
                                             ('Birthday dinner, decor', '24,000.00')]) + '\nand 3 more'
for body in bodies + [ledger.replace('P30', 'P30') + '']:
    rows = wrap_m(body, machine['BatchBodyPoints'], machine['CardTextWidth'])
    line_h = advance_m('', 1) or 0
    full = len(rows) * line * machine['BatchBodyPoints'] / point * 0.1 * scale
    assert full <= machine['BatchBodyHeight'], (body, rows, full)
    for row_text in rows:
        assert advance_m(row_text, machine['BatchBodyPoints']) <= machine['CardTextWidth'], row_text
    checks += 1
    print(f'PASS batch body {len(rows)} line(s), {full:.4f}/{machine["BatchBodyHeight"]:.4f}m: {body[:48]!r}')
check('?', machine['MarkPoints'], machine['MarkDiameter'], machine['MarkDiameter'])
check_wrapped("Bea's Income & Expense Summary", machine['SummaryTitlePoints'], machine['SummaryTextWidth'], machine['SummaryTitleHeight'])
for label in ('Income', 'Supported deductions'):
    check(label, machine['SummaryLabelPoints'], machine['SummaryTextWidth'], machine['SummaryLabelHeight'])
for value in ('P900,000.00', 'P300,000.00', 'P1,254,000.00'):
    check(value, machine['SummaryAmountPoints'], machine['SummaryTextWidth'], machine['SummaryAmountHeight'])
front = 0.007 + machine['SummaryTitleHeight'] + 0.005 + 2 * (machine['SummaryLabelHeight'] + machine['SummaryAmountHeight']) + 0.003
assert front <= machine['SummaryHeight'], front
checks += 1
print(f'PASS summary front {front:.4f}/{machine["SummaryHeight"]:.4f}m high')
batch_rows = machine['CardTop'] + machine['PositionHeight'] + machine['TitleHeight'] + machine['BatchStatusHeight'] + machine['BatchBodyHeight'] + 4 * machine['CardRowGap'] + 0.001
assert batch_rows + machine['BatchKeyHeight'] + 0.006 <= machine['CardHeight'], batch_rows
checks += 1
print(f'PASS batch card rows {batch_rows + machine["BatchKeyHeight"] + 0.006:.4f}/{machine["CardHeight"]:.4f}m high, key clear of the text')
# The summary paper on the existing record face (SupportingDocumentFace: 700 x 980 units, inset 40; row label 22 pt
# bold capitals with 3 spacing; value 34 pt one line). Units: font units scaled by size / pointSize.
def ui_width(text, size, spacing=0.0):
    return sum(glyphs[chars[ord('P' if c == '\u20b1' else c)]] for c in text) * size / point * 1.04 + len(text) * size * spacing / 100.0
# The summary's two faces share the same large two-line header. Read the actual layout constants.
face_source = (ROOT / 'Assets/Scripts/SideQuest1/SupportingDocumentFace.cs').read_text()
header = {key: float(value) for key, value in re.findall(r'const float (Summary\w+) = ([0-9.]+)f;', face_source)}
assert 'SummaryTitlePoints' in header, 'Summary needs a bounded shared front/back header, not ellipsis/overflow'
for title_line in ('INCOME & EXPENSE', 'SUMMARY'):
    assert ui_width(title_line, header['SummaryTitlePoints']) <= header['SummaryTitleWidth'], title_line
    checks += 1
assert line / point * header['SummaryTitlePoints'] * 2 <= header['SummaryTitleHeight']
assert 14 + header['SummaryTitleHeight'] <= 112
assert header['SummaryBodyTop'] >= header['SummaryHeaderHeight'] + 12
assert face_source.count('BuildSummaryHeader(root, r,') == 2
checks += 4
print('PASS summary shared header: full title in two large lines, separate front/back marker, clear of body')
summary_rows = [('Income', 'P900,000.00'), ('Supported deductions', 'P300,000.00'),
                ('Income \u00b7 REV-B26', 'Logo fees P500,000.00'), ('Income \u00b7 REV-B26', 'Menu illustration fees P400,000.00'),
                ('Deduction \u00b7 EXP-B26 \u00b7 RENT-26', 'Studio rent P180,000.00'), ('Deduction \u00b7 EXP-B26 \u00b7 SUB-26', 'Design subscriptions P90,000.00'),
                ('Deduction \u00b7 EXP-B26 \u00b7 PRINT-26', 'Printing and supplies P30,000.00'),
                ('Left out \u00b7 Personal purpose', 'Birthday dinner, decor P24,000.00'), ('Left out \u00b7 Already represented', 'Printing bill, paid P30,000.00')]
for label, value in summary_rows:
    lw, vw = ui_width(label.upper(), 22, 3), ui_width(value, 34)
    assert lw <= 620 and vw <= 620, (label, lw, value, vw)
    checks += 1
    print(f'PASS summary paper row {lw:.0f}/620 label, {vw:.0f}/620 value units: {value}')
for label, value in summary_rows[:2]:
    assert ui_width(label.upper(), header['SummaryAmountLabelPoints'], 3) <= 572
    assert ui_width(value, header['SummaryAmountValuePoints']) <= 572
    assert line / point * header['SummaryAmountValuePoints'] <= 60
    checks += 3
# Computation uses page 1's column/font, with explicitly stacked text and 12-unit vertical padding.
ink_source = (ROOT / 'Assets/Scripts/UI/PaperInkStyle.cs').read_text()
field_points = float(re.search(r'FieldFontSize = ([0-9.]+)f', ink_source)[1])
assert field_points == float(re.search(r'm_fontSize: ([0-9.]+)', (ROOT / 'Assets/Prefabs/CaseFolderFieldRow.prefab').read_text())[1])
field_width = 760 - 18 - 40  # Padded form text and existing grip clearance.
field_height = 128 - 24
for caption, value in [('Tax due', 'P62,500.00'), ('Tax credit', 'Place amount'),
                       ('Taxable income', 'Attach result here'), ('Taxable income', 'P600,000.00'),
                       ('Annual band', '400,000 - 800,000'), ('Remaining payable', 'P50,000.00')]:
    label_width = ui_width(caption, field_points * .78)
    value_width = ui_width(value, field_points)
    stacked = True
    height = line / point * field_points * 1.78
    assert max(label_width, value_width) <= field_width and height <= field_height, (caption, value, height)
    checks += 1
    print(f'PASS native folder field: {caption}, {"stacked" if stacked else "inline"}, {height:.0f}/{field_height} units high')
# T09: the Auditor's mark tags a caption; the longest tagged caption still fits beside the grip.
page_source = (ROOT / 'Assets/Scripts/Level3/Level3ComputationPage.cs').read_text(encoding='utf-8')
mark_suffix = re.search(r'MarkSuffix = "([^"]+)"', page_source)[1]
for caption in ('Taxable income', 'Annual band', 'Tax due', 'Tax credit', 'Remaining payable', 'Remaining payable · Recalculate'):
    tagged = caption if 'Recalculate' in caption else caption + mark_suffix
    tagged_width = ui_width(tagged, field_points * .78)
    assert tagged_width <= field_width, (tagged, tagged_width, field_width)
    checks += 1
    print(f'PASS tagged folder caption: {tagged}, {tagged_width:.0f}/{field_width} units')
# T09: the leaf's one note shows the Auditor's short line (28 pt, wrapped in 760 x 120 units).
review_roots = json.loads((ROOT / 'Assets/Resources/Levels/Level_03/review.json').read_text(encoding='utf-8'))['roots']
for root_note in review_roots:
    rows, current = 1, ''
    for word in ('Auditor: ' + root_note['line']).split():
        trial = (current + ' ' + word).strip()
        if ui_width(trial, 28) > 760 and current:
            rows, current = rows + 1, word
        else:
            current = trial
    assert rows * line / point * 28 <= 120, (root_note['id'], rows)
    checks += 1
print(f'PASS Auditor note on the leaf: {len(review_roots)} spoken lines fit 760 x 120 at 28 pt')
# Band destination keeps its full range visible in the same native stacked field.
for label in ('Up to 250,000', 'Over 250,000 to 400,000', 'Over 400,000 to 800,000'):
    assert ui_width(label, field_points) <= field_width, label
    checks += 1
# Newly revealed rule parts: whole printed amounts retain exact decimal payloads, with grip clearance.
for caption, value in [('Threshold', 'P400,000'), ('Rate', '20%'), ('Base tax', 'P22,500')]:
    assert ui_width(caption, 34 * .78) <= 240 - 58
    assert ui_width(value, 34) <= 240 - 58
    assert line / point * 34 * 1.78 <= 96 - 24
    checks += 3
assert 172 + 128 < 488 and 488 + 128 < 636  # Both leaves clear status/footer.
checks += 1
# Check each actual Chapter 3 leaf using the shared book's bounded caption/chip fit.
# Section 7 contains all six selectable annual bands and one read-only explanation.
import json
book_sections = json.loads((ROOT / 'Assets/Resources/Levels/Level_03/book.json').read_text())['sections']
annual_leaf = next(section for section in book_sections if section['heading'].startswith('Section 7 '))
assert sum(not value['readOnly'] for value in annual_leaf['clickableValues']) == 6
checks += 1
for section in book_sections:
    paragraphs = section['body'].split('\n\n')
    assert len(paragraphs) == len(section['clickableValues']), section['heading']
    fitted = None
    for chip_height in (68, 58):
        for caption_points in range(36, 25, -2):
            book_height = len(paragraphs) * (chip_height + 14 + 12)
            for paragraph in paragraphs:
                rows, current = 1, ''
                # Minus uses the shipped font's hyphen width as an explicit fallback estimate.
                for word in paragraph.replace('−', '-').split():
                    trial = (current + ' ' + word).strip()
                    if current and ui_width(trial, caption_points) > 700 - 52:
                        rows, current = rows + 1, word
                    else:
                        current = trial
                book_height += rows * line / point * caption_points
            if book_height <= 860:
                fitted = (book_height, caption_points, chip_height)
                break
        if fitted is not None:
            break
    assert fitted is not None, (section['heading'], book_height)
    checks += 1
    print(f'PASS {section["heading"]}: {fitted[0]:.0f}/860 units, {fitted[1]} pt captions, {fitted[2]}-unit chips (not a render)')
# T10: Bea's docked copy card (980 wide; rows 916 x 92; label 520 at 30 pt, amount 352 at 34 pt bold, no auto-size).
for label, amount in [('Tax due', 'P62,500'), ('Tax credit (withheld)', 'P12,500'), ('Remaining payable', 'P50,000')]:
    assert ui_width(label, 30) <= 520 - 8, label
    assert ui_width(amount, 34) * 1.08 <= 980 - 64 - 540 - 24, amount  # bold allowance
    assert line / point * 34 <= 92 - 12
    checks += 3
assert ui_width("BEA'S CLIENT COPY \u00b7 TAX AND PAYMENT", 24) * 1.08 <= 700
checks += 1
print('PASS Bea copy card: three figure rows and heading fit at fixed 30/34/24 pt (not a render)')
# T14: Bea's docked finding card (rows 916 x 78, label stretched 16 in: 884 x 46 at up to 30 pt, auto-size never needed),
# its 24 pt heading, and the shared chapter Continue action (380 x 76, up to 32 pt bold, 6 spacing).
town = json.loads((ROOT / 'Assets/Resources/Levels/Level_03/town_life.json').read_text(encoding='utf-8'))
for choice in town['bea']['findingChoices']:
    assert ui_width(choice['text'], 30) <= 916 - 32, choice['text']
    checks += 1
assert ui_width(town['bea']['findingReview'], 30) <= 916 - 32 and line / point * 30 <= 78 - 32
assert ui_width(town['bea']['findingTitle'], 24) * 1.08 <= 700
assert ui_width('CONTINUE', 32, 6) * 1.08 <= 380 - 20 and line / point * 32 <= 76 - 20
checks += 3
print('PASS Bea finding card: three statements and Review her records fit one line at 30 pt; heading and shared CONTINUE fit (not a render)')
print(f'PASS: {checks} shipped-font geometry checks; actual mesh/render/device acceptance remains pending.')
