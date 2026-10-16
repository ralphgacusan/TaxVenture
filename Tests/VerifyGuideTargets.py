"""Checks that every scene path in Resources/Tutorials/Level_01/guide.json
(world: targets and waypoint targets) exists in Office_Level1.unity.
Usage: python Tests/VerifyGuideTargets.py   (from the project root)
Paths inside prefab instances may not be visible in the scene file; those are
reported as WARN, not FAIL."""
import json, re, sys

SCENE = 'Assets/Scenes/Office_Level1.unity'
GUIDE = 'Assets/Resources/Tutorials/Level_01/guide.json'

text = open(SCENE, encoding='utf-8', errors='ignore').read()
docs = {}
for d in re.split(r'\n--- !u!', text):
    m = re.match(r'(\d+) &(-?\d+)', d)
    if m:
        docs[m.group(2)] = (m.group(1), d)

transform_of = {}
for k, (t, d) in docs.items():
    if t in ('4', '224'):
        g = re.search(r'm_GameObject: \{fileID: (\d+)', d)
        if g:
            transform_of[g.group(1)] = k

def name(gid):
    d = docs.get(gid)
    m = re.search(r'm_Name: (.*)', d[1]) if d else None
    return m.group(1).strip() if m else None

paths = set()
for gid in transform_of:
    parts, cur, ok = [], gid, True
    for _ in range(40):
        parts.append(name(cur))
        p = re.search(r'm_Father: \{fileID: (\d+)', docs[transform_of[cur]][1])
        if not p or p.group(1) == '0':
            break
        pg = re.search(r'm_GameObject: \{fileID: (\d+)', docs.get(p.group(1), ('', ''))[1])
        if not pg:
            ok = False
            break
        cur = pg.group(1)
    if ok and None not in parts:
        paths.add('/'.join(reversed(parts)))

# Prefab instance roots: their names come from modifications.
prefab_names = set(re.findall(r"propertyPath: m_Name\s+value: (.+)", text))

guide = json.load(open(GUIDE, encoding='utf-8'))
wanted = set()
for w in guide.get('waypoints', []):
    for tok in w['target'].split('|'):
        if not tok.startswith('type:'):
            wanted.add(tok.strip())
for lesson in guide.get('lessons', []):
    for page in lesson.get('pages', []):
        for key in ('point', 'to'):
            v = page.get(key) or ''
            if v.startswith('world:'):
                wanted.add(v[6:])
        for key in ('waitFor',):
            for part in (page.get(key) or '').split('|'):
                if part.startswith('active:world:'):
                    wanted.add(part[len('active:world:'):])
    for part in (lesson.get('when') or '').split('|'):
        if part.startswith('active:world:'):
            wanted.add(part[len('active:world:'):])

# Office Hours front door and the case finale staging.
oh = json.load(open('Assets/Resources/Levels/Level_01/office_hours.json', encoding='utf-8'))
for key in ('frontDoor', 'frontDoorInside', 'nextMorningSpawn'):
    if oh.get(key):
        wanted.add(oh[key])
case = json.load(open('Assets/Resources/Levels/Level_01/cases/case_001.json', encoding='utf-8'))
for key in ('modelPath', 'spotPath', 'roomPath', 'floorPath'):
    v = (case.get('clientFinale') or {}).get(key)
    if v:
        wanted.add(v)

fails = warns = 0
for p in sorted(wanted):
    if p in paths:
        print('OK   ', p)
    elif p.split('/')[-1] in prefab_names:
        warns += 1
        print('WARN ', p, '(inside a prefab instance; checked by name only)')
    else:
        fails += 1
        print('FAIL ', p)
print(f'{len(wanted)} paths, {fails} missing, {warns} by name only')
sys.exit(1 if fails else 0)
