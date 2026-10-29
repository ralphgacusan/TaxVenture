"""Documentation-only verification. Never opens Unity or edits production files."""
from pathlib import Path
from urllib.parse import unquote
import re
import sys

PACK = Path(__file__).resolve().parent
ROOT = PACK.parents[1]
EXPECTED = {
    'T01': [], 'T02': ['T01'], 'T03': ['T02'],
    'T04': ['T02', 'T03'], 'T05': ['T04'], 'T06': ['T03', 'T05'],
    'T07': ['T01', 'T06'], 'T08': ['T07'], 'T09': ['T08'],
    'T10': ['T09'], 'T11': ['T10'], 'T12': ['T01', 'T09', 'T11'],
    'T13': ['T06', 'T12'], 'T14': ['T10', 'T12', 'T13'],
    'T15': ['T14'], 'T16': ['T06', 'T12', 'T14'],
    'T17': ['T13', 'T14', 'T15', 'T16'], 'T18': ['T17'],
}
REQUIRED = [
    'README.md', 'AGENTS.md', 'REUSE_AND_ARCHITECTURE.md', 'SPEC.md',
    'CONTENT.md', 'SCENE_REUSE.md', 'WORLD_AND_FEEDBACK.md',
    'REWARDS_AND_VALIDATION.md', 'VERIFICATION.md', 'PROGRESS.md',
    'KNOWN_ISSUES.md', 'TICKETS.md',
]
HEADINGS = [
    'Goal and visible accomplishment', 'Existing systems to reuse',
    'Work and interaction mechanics', 'Assets, motion and feedback',
    'Integration requirements', 'Persistence and failure rules',
    'Internal checks — AI source/build/regressions only',
    'USER manual acceptance — Unity/device/visual/feel/listening only',
    'Handoff and completion definition',
]
errors = []
for name in REQUIRED:
    if not (PACK / name).is_file():
        errors.append(f'Missing document: {name}')

bodies = {}
link_count = source_count = 0
for path in PACK.rglob('*.md'):
    raw = path.read_bytes()
    try:
        body = raw.decode('utf-8-sig')
    except UnicodeError as error:
        errors.append(f'{path.name}: {error}')
        continue
    bodies[path] = body
    if b'\n' in raw.replace(b'\r\n', b''):
        errors.append(f'{path.name}: non-CRLF newline')
    if re.search(r'(?m)^\+?<{7}|^={7}$|^>{7}', body):
        errors.append(f'{path.name}: conflict marker')
    for target in re.findall(r'\[[^\]\n]+\]\(([^)\n]+)\)', body):
        target = target.strip().strip('<>')
        if re.match(r'^[a-zA-Z][\w+.-]*:', target) or target.startswith('#'):
            continue
        target = unquote(target.split('#', 1)[0])
        link_count += 1
        if not (path.parent / target).resolve().exists():
            errors.append(f'{path.relative_to(PACK)}: broken link {target}')

actual = {}
for path in sorted((PACK / 'tickets').glob('T*.md')):
    match = re.fullmatch(r'(T\d{2})-[a-z0-9-]+\.md', path.name)
    if not match:
        errors.append(f'Bad ticket filename: {path.name}')
        continue
    key = match.group(1)
    if key in actual:
        errors.append(f'Duplicate ticket {key}')
    actual[key] = path
    body = bodies.get(path, '')
    if not body.startswith(f'# {key} — '):
        errors.append(f'{key}: wrong heading')
    if 'Status: **Planned**.' not in body:
        errors.append(f'{key}: unearned status or missing Planned')
    for field in ['Priority:', 'Phase:', 'Estimated scope:']:
        if field not in body:
            errors.append(f'{key}: missing {field}')
    for heading in HEADINGS:
        if f'## {heading}' not in body:
            errors.append(f'{key}: missing section {heading}')
    match = re.search(r'Blocked by: ([^.]+)\.', body)
    deps = re.findall(r'T\d{2}', match.group(1)) if match else None
    if deps != EXPECTED.get(key):
        errors.append(f'{key}: dependencies {deps}, expected {EXPECTED.get(key)}')
    if '- [x]' in body.lower():
        errors.append(f'{key}: prechecked acceptance before implementation')
    if body.count('- [ ]') < 6:
        errors.append(f'{key}: missing atomic acceptance checks')
    source_block = body.split('## Existing systems to reuse', 1)[-1].split('## Work', 1)[0]
    for source in re.findall(r'^- `([^`]+)`', source_block, re.M):
        source_count += 1
        if not (ROOT / source).is_file():
            errors.append(f'{key}: claimed existing source missing: {source}')
    if len(body.split()) > 1250:
        errors.append(f'{key}: review oversized ticket (>1250 words)')
    for dependent in re.findall(r'T\d{2}', body.split('Direct dependents:', 1)[-1].split('.', 1)[0]):
        if key not in EXPECTED.get(dependent, []):
            errors.append(f'{key}: inconsistent downstream {dependent}')

if set(actual) != set(EXPECTED):
    errors.append(f'Ticket set mismatch: expected18, got {len(actual)}')

visited, active = set(), set()
def visit(key):
    if key in active:
        errors.append(f'Dependency cycle at {key}')
        return
    if key in visited:
        return
    active.add(key)
    for dep in EXPECTED[key]:
        if dep not in EXPECTED:
            errors.append(f'{key}: unknown blocker {dep}')
        elif int(dep[1:]) >= int(key[1:]):
            errors.append(f'{key}: recommended numbering not topological')
        else:
            visit(dep)
    active.remove(key)
    visited.add(key)
for key in EXPECTED:
    visit(key)

index = bodies.get(PACK / 'TICKETS.md', '')
for key, path in actual.items():
    if f'(tickets/{path.name})' not in index:
        errors.append(f'{key}: absent from master index')

# Fixtures check the documented arithmetic, not the future gameplay implementation.
from decimal import Decimal as D
fixtures = {
    'worksheet decisions': (7 + 2 + 1 + 2, 12),
    'chapter coins': (60 + 100 + 100, 260),
    'max mastery XP': (60 + 100 + 100, 260),
    'service trust': (2 + 4 + 4, 10),
    'responsible history': (46 + 6 + 2 + 10, 64),
    'unsupported history': (46 + 6 - 8 + 10, 54),
    'dismissive history': (46 + 6 - 3 + 10, 59),
    'earned through L3': (160 + 160 + 260, 580),
    'existing permanent products': (40 + 50 + 90 + 60 + 100 + 30 + 60, 430),
    'Bea due': (D(22500) + D('.20') * (D(600000) - D(400000)), D(62500)),
    'Bea balance': (D(62500) - D(12500), D(50000)),
    'company balance': ((D(8000000) - D(2000000)) * D('.25') - D(300000), D(1200000)),
}
for name, (actual_value, expected) in fixtures.items():
    if actual_value != expected:
        errors.append(f'Bad documentation fixture {name}: {actual_value} != {expected}')

if errors:
    print('\n'.join('FAIL: ' + message for message in errors))
    sys.exit(1)
words = sum(len(bodies[path].split()) for path in actual.values())
print(f'PASS: {len(actual)} planned tickets; dependency DAG/order; {link_count} local links; '
      f'{source_count} existing source references; {len(fixtures)} arithmetic fixtures; '
      f'{words} ticket words. Documentation only; no gameplay/Unity verification.')
