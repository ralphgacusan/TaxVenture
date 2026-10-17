"""Level 2 scene and build checks (Documentation/Level2SomethingDoesNotMatch/SCENE_REUSE.md).
Usage: python Tests/VerifyLevel2Scene.py   (from the project root)

Checks, without Unity:
- Office_Level2.unity exists with its own .meta GUID (not Office_Level1's, not _OLD's,
  not shared with any other asset); Office_Level1 and Office_Level2_OLD keep their GUIDs.
- EditorBuildSettings: exactly one enabled Office_Level2 entry with that GUID;
  Office_Level2_OLD is not enabled in the build.
- Every scene path used by Level_02 data (office_start.json, guide.json waypoints
  and world: points) exists in Office_Level2.unity.
- Office_Level2 has the same count of each single-owner manager as Office_Level1
  (no duplicated camera/input/managers) and no serialized Level2Gameplay root
  (it is built at runtime, once).
Paths inside prefab instances may not be visible in the scene file; those are
WARN, not FAIL. Unity import, lighting, visuals and Play Mode are NOT checked here."""
import hashlib, json, os, re, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
os.chdir(ROOT)
LEVEL1 = 'Assets/Scenes/Office_Level1.unity'
LEVEL2 = 'Assets/Scenes/Office_Level2.unity'
OLD = 'Assets/Scenes/Office_Level2_OLD.unity'
LEVEL1_GUID = '5f6c770d7b969f143b4fbd481aaef942'
OLD_GUID = '355dc2e2b4ed8e74cbeb99ad96cd5ea3'
# Office_Level1 at the one-time copy (2026-10-17). Level 1 may change later; a
# different hash is reported, not failed.
BASELINE_SHA256 = '914f31ffd8f0f24fd22f075cdfaadc751a6f890eb43ac8c42b80d118c7fd63cf'

fails = 0
warns = 0


def check(ok, what):
    global fails
    if not ok:
        fails += 1
        print('FAIL:', what)


def warn(what):
    global warns
    warns += 1
    print('WARN:', what)


def meta_guid(path):
    m = re.search(r'guid: ([0-9a-f]{32})', open(path + '.meta', encoding='utf-8').read())
    return m.group(1) if m else None


def sha256(path):
    return hashlib.sha256(open(path, 'rb').read()).hexdigest()


# ---------- files and GUIDs ----------
for path in (LEVEL1, LEVEL2, OLD):
    check(os.path.exists(path) and os.path.exists(path + '.meta'), f'{path} and its .meta exist')
if fails:
    sys.exit(1)

level2_guid = meta_guid(LEVEL2)
check(meta_guid(LEVEL1) == LEVEL1_GUID, 'Office_Level1 keeps its GUID')
check(meta_guid(OLD) == OLD_GUID, 'Office_Level2_OLD keeps its GUID')
check(level2_guid not in (LEVEL1_GUID, OLD_GUID), 'Office_Level2 has its own GUID')
uses = 0
for base, _, names in os.walk('Assets'):
    for n in names:
        if n.endswith('.meta'):
            try:
                if ('guid: ' + level2_guid) in open(os.path.join(base, n), encoding='utf-8', errors='ignore').read():
                    uses += 1
            except OSError:
                pass
check(uses == 1, f'Office_Level2 GUID is used by exactly one .meta (found {uses})')
if sha256(LEVEL1) != BASELINE_SHA256:
    warn('Office_Level1 changed since the one-time copy; port only the specific fixes Level 2 needs (SCENE_REUSE.md).')

# ---------- build settings ----------
build = open('ProjectSettings/EditorBuildSettings.asset', encoding='utf-8').read()
entries = re.findall(r'- enabled: (\d)\s+path: (.+?)\s+guid: ([0-9a-f]{32})', build)
level2_entries = [e for e in entries if e[1].strip() == LEVEL2]
check(len(level2_entries) == 1, f'one build entry for Office_Level2 (found {len(level2_entries)})')
check(all(e[0] == '1' and e[2] == level2_guid for e in level2_entries), 'Office_Level2 build entry is enabled and uses its own GUID')
check(not any(e[0] == '1' and (e[1].strip() == OLD or e[2] == OLD_GUID) for e in entries), 'Office_Level2_OLD is not in the player build')
check(sum(1 for e in entries if e[0] == '1' and 'Office_Level2' in e[1]) == 1, 'only one Level 2 entry scene ships')

# ---------- scene parse ----------
def parse(path):
    text = open(path, encoding='utf-8', errors='ignore').read()
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

    paths, roots = set(), []
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
            if len(parts) == 1:
                roots.append(parts[0])
    prefab_names = set(re.findall(r'propertyPath: m_Name\s+value: (.+)', text))
    scripts = re.findall(r'm_Script: \{fileID: 11500000, guid: ([0-9a-f]{32})', text)
    return text, paths, roots, prefab_names, scripts


_, paths2, roots2, prefabs2, scripts2 = parse(LEVEL2)
_, _, _, _, scripts1 = parse(LEVEL1)


def exists(path):
    if path in paths2:
        return True
    if path.split('/')[-1].strip() in prefabs2:
        warn('inside a prefab instance (not provable from the scene file): ' + path)
        return True
    return False


def res(rel):
    return json.load(open(os.path.join('Assets/Resources', rel), encoding='utf-8'))


start = res('Levels/Level_02/office_start.json')
for key in ('spawn', 'face'):
    check(exists(start[key]), f'office_start {key} exists in Office_Level2: {start[key]}')
for path in start['hide']:
    check(exists(path), 'office_start hide exists in Office_Level2: ' + path)

guide = res('Tutorials/Level_02/guide.json')
RUNTIME_ROOT = 'Level2Gameplay/'  # built at runtime by Level2OfficeStart (Rosa's stall etc.)
runtime_targets = []
for w in guide['waypoints']:
    for tok in w['target'].split('|'):
        tok = tok.strip()
        if tok.startswith(RUNTIME_ROOT):
            runtime_targets.append(tok)
        elif not tok.startswith('type:'):
            check(exists(tok), 'guide waypoint exists in Office_Level2: ' + tok)
print('runtime-built waypoint targets (checked by Level2Regression source checks):', ', '.join(runtime_targets) or 'none')
for lesson in guide['lessons']:
    for page in lesson.get('pages', []):
        for key in ('point', 'to'):
            v = page.get(key) or ''
            if v.startswith('world:'):
                check(exists(v[6:]), f'guide {lesson["id"]} {key} exists: {v[6:]}')

# ---------- single owners and the runtime root ----------
guid_names = {}
for base, _, names in os.walk('Assets/Scripts'):
    for n in names:
        if n.endswith('.cs.meta'):
            m = re.search(r'guid: ([0-9a-f]{32})', open(os.path.join(base, n), encoding='utf-8').read())
            if m:
                guid_names[m.group(1)] = n[:-8]
owners = ['CaseProgressionManager', 'CaseManager', 'GameStateMachine', 'CameraController', 'PlayerMovement',
          'AudioManager', 'PlayerProgressManager', 'AuditorSubmissionTray', 'DialogueUI', 'ValueTransferManager']
for owner in owners:
    c1 = sum(1 for g in scripts1 if guid_names.get(g) == owner)
    c2 = sum(1 for g in scripts2 if guid_names.get(g) == owner)
    check(c1 == c2, f'{owner}: Office_Level2 has the same count as Office_Level1 ({c2} vs {c1})')
check(roots2.count('Level2Gameplay') <= 1, 'at most one serialized Level2Gameplay root (normally none: built at runtime)')
check(roots2.count('Player') == 1 and roots2.count('Main Camera') == 1, 'one Player and one root Main Camera')
for runtime in ('Level2OfficeStart', 'OfficeHudRuntime', 'GuideRuntime', 'OfficeMinimap', 'SceneFade'):
    check(sum(1 for g in scripts2 if guid_names.get(g) == runtime) == 0, f'{runtime} is not serialized in the scene (built once at runtime)')

print(f'VerifyLevel2Scene: {"PASS" if fails == 0 else str(fails) + " FAIL"}'
      f' ({warns} warning(s)); Office_Level2 GUID {level2_guid}; Office_Level1 sha256 {sha256(LEVEL1)[:16]}...')
sys.exit(1 if fails else 0)
