"""Level 2 asset hygiene (T12): every .meta GUID under Assets is unique, every
Level 2 script has a .meta, and Level 2 scripts/content use CRLF line endings.
Run from the project root: python Tests/VerifyLevel2Assets.py"""
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ASSETS = os.path.join(ROOT, 'Assets')
GUID = re.compile(r'^guid: ([0-9a-f]{32})', re.M)

problems = []
seen = {}
for folder, _, files in os.walk(ASSETS):
    for name in files:
        if not name.endswith('.meta'):
            continue
        path = os.path.join(folder, name)
        with open(path, encoding='utf-8', errors='ignore') as f:
            m = GUID.search(f.read())
        if not m:
            continue
        guid = m.group(1)
        if guid in seen:
            problems.append(f'duplicate GUID {guid}: {seen[guid]} and {os.path.relpath(path, ROOT)}')
        else:
            seen[guid] = os.path.relpath(path, ROOT)

LEVEL2_SCRIPTS = [os.path.join(ASSETS, 'Scripts', 'Level2')]
EXTRA = ['IncomeAnnotation.cs', 'IncomeChallenge.cs', 'OfficeReview.cs', 'FinalAssessment.cs', 'FinalAudit.cs',
         'ReviewClock.cs', 'Level2Checkpoint.cs', 'CaseReviewState.cs', 'CaseReviewValidator.cs']
scripts = []
for folder in LEVEL2_SCRIPTS:
    for name in os.listdir(folder):
        if name.endswith('.cs'):
            scripts.append(os.path.join(folder, name))
scripts += [os.path.join(ASSETS, 'Scripts', 'Data', n) for n in EXTRA]
content = [os.path.join(ASSETS, 'Resources', 'Levels', 'Level_02', n) for n in
           ('level.json', 'rewards.json', 'objectives.json', 'book.json', 'office_start.json', os.path.join('cases', 'case_rosa_001.json'))]
content.append(os.path.join(ASSETS, 'Resources', 'Tutorials', 'Level_02', 'guide.json'))

for path in scripts + content:
    rel = os.path.relpath(path, ROOT)
    if not os.path.exists(path):
        problems.append(f'missing {rel}')
        continue
    if not os.path.exists(path + '.meta'):
        problems.append(f'no .meta for {rel}')
    data = open(path, 'rb').read()
    crlf = data.count(b'\r\n')
    lf = data.count(b'\n') - crlf
    if lf > 0:
        problems.append(f'{rel}: {lf} LF-only line endings (project text uses CRLF)')

for p in problems:
    print('PROBLEM', p)
print(f'VerifyLevel2Assets: {"PASS" if not problems else "FAIL"}; {len(seen)} .meta GUIDs unique; '
      f'{len(scripts)} Level 2 scripts and {len(content)} content files checked')
sys.exit(1 if problems else 0)
