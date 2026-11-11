"""Level 3 scene and build checks (Documentation/Level3TheNumbersTellAStory/SCENE_REUSE.md, T02).
Usage: python Tests/VerifyLevel3Scene.py   (from the project root)

Checks, without Unity:
- Office_Level3.unity exists with its own .meta GUID, used by no other .meta;
  Office_Level1/Office_Level2 keep their GUIDs. The user-requested supply
  and Bea placement markers are authored; Level3Gameplay is built at runtime.
- Revision (T02, 3 November 2026): Office_Level3 is the authorized continuing
  scene, edited and saved since its one-time copy, so byte equality with the
  saved Office_Level2 is no longer a contract. Each scene's hash is compared
  with the baseline recorded at the revision; a difference is reported as WARN
  (a later saved edit), never "fixed" and never a pass/fail by itself. The
  structural contracts below (GUIDs, script set, owners, runtime roots, paths,
  build order, Career gate) are what is checked.
- Every script GUID in Office_Level3 resolves to a script in Assets or a package
  (no dangling script reference).
- Every scene path Level_03 data uses (office_start.json spawn/face/hide) exists.
- Single-owner managers keep Office_Level2's counts; Level3OfficeStart and the
  office runtimes are not serialized; no serialized Level3Gameplay/Level2Gameplay.
- Build settings: Office_Level3, when registered (Level3SceneSetup does it on
  import), is one enabled entry with its GUID AFTER every existing entry; the
  existing entries keep their order. Not registered yet = WARN (import pending).
- Career Chapter 3 routes to the enabled scene for the user-requested APK playtest.
Unity import, lighting, visuals, controls and Play Mode are NOT checked here."""
import hashlib, json, os, re, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
os.chdir(ROOT)
LEVEL1 = 'Assets/Scenes/Office_Level1.unity'
LEVEL2 = 'Assets/Scenes/Office_Level2.unity'
LEVEL3 = 'Assets/Scenes/Office_Level3.unity'
LEVEL1_GUID = '5f6c770d7b969f143b4fbd481aaef942'
LEVEL2_GUID = '00c1e5f98fce4a819da741bdd9d51819'
LEVEL3_GUID = '844c604a279e4b66ae226bf66f1dd99a'
# Historical: the immediate pre-T02 Office_Level2 (30 October 2026), the saved source of the one-time copy.
COPY_SOURCE = 'f5b8538e20841926228b4f84bb5447db9c1c4aa488b65e73f435f06343db19e7'
# Saved scenes recorded at the T02 revision (3 November 2026), before any revised work; later edits are WARN only.
LEVEL1_BASELINE = 'f4564ecd2513cb2b5e8cf81e2ffe92a075649f591ad52024674a44d24c675275'
LEVEL2_BASELINE = '0a46b8d6d82caf34f60f3440ddcf679b5ec1957c83550031b71cc6624f9cd433'
LEVEL3_BASELINE = '3e8d29dfd93c2efc75a903034b6957b5c6093c867f6b9ca0a9900be6c824b9c5'
# Build order before T02 (Level3SceneSetup only appends after these).
BUILD_BEFORE = ['Assets/Scenes/3D Menu.unity', LEVEL1, LEVEL2, 'Assets/Scenes/Cutscene2.0.unity']

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


# ---------- files, GUIDs and hashes ----------
for path in (LEVEL1, LEVEL2, LEVEL3):
    check(os.path.exists(path) and os.path.exists(path + '.meta'), f'{path} and its .meta exist')
if fails:
    sys.exit(1)

check(meta_guid(LEVEL1) == LEVEL1_GUID, 'Office_Level1 keeps its GUID')
check(meta_guid(LEVEL2) == LEVEL2_GUID, 'Office_Level2 keeps its GUID')
check(meta_guid(LEVEL3) == LEVEL3_GUID, 'Office_Level3 has its recorded GUID')
check(LEVEL3_GUID not in (LEVEL1_GUID, LEVEL2_GUID), 'Office_Level3 GUID differs from both source scenes')
meta_guids = {}
for base in ('Assets', 'Packages'):
    for d, _, names in os.walk(base):
        for n in names:
            if n.endswith('.meta'):
                try:
                    m = re.search(r'guid: ([0-9a-f]{32})', open(os.path.join(d, n), encoding='utf-8', errors='ignore').read())
                except OSError:
                    continue
                if m:
                    meta_guids.setdefault(m.group(1), []).append(os.path.join(d, n))
check(len(meta_guids.get(LEVEL3_GUID, [])) == 1, f'Office_Level3 GUID is used by exactly one .meta (found {len(meta_guids.get(LEVEL3_GUID, []))})')
dupes = {g: p for g, p in meta_guids.items() if len(p) > 1 and any('Level3' in x or 'Level_03' in x for x in p)}
check(not dupes, 'no new Level 3 asset shares a GUID: ' + str(dupes))
for label, path, baseline in (('Office_Level1', LEVEL1, LEVEL1_BASELINE), ('Office_Level2', LEVEL2, LEVEL2_BASELINE),
                               ('Office_Level3', LEVEL3, LEVEL3_BASELINE)):
    if sha256(path) != baseline:
        warn(f'{label} differs from its 3 November 2026 revision baseline (a later saved edit; not checked here)')
level3_hash = sha256(LEVEL3)

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


text3, paths3, roots3, prefabs3, scripts3 = parse(LEVEL3)
_, _, _, _, scripts2 = parse(LEVEL2)


def exists(path):
    if path in paths3:
        return True
    if path.split('/')[-1].strip() in prefabs3:
        warn('inside a prefab instance (not provable from the scene file): ' + path)
        return True
    return False


# ---------- script references ----------
package_guids = set()
for base in ('Library/PackageCache',):
    if os.path.isdir(base):
        for d, _, names in os.walk(base):
            for n in names:
                if n.endswith('.cs.meta') or n.endswith('.dll.meta'):
                    try:
                        m = re.search(r'guid: ([0-9a-f]{32})', open(os.path.join(d, n), encoding='utf-8', errors='ignore').read())
                    except OSError:
                        continue
                    if m:
                        package_guids.add(m.group(1))
unresolved = sorted({g for g in scripts3 if g not in meta_guids and g not in package_guids})
# A missing script both source scenes already carry is inherited, not new (reported, never "fixed" here).
inherited = [g for g in unresolved if g in scripts2]
for g in inherited:
    ident = re.search(r'guid: ' + g + r', type: 3\}\s+m_Name: .*\s+m_EditorClassIdentifier: (\S+)', text3)
    warn(f'inherited missing script {g} ({ident.group(1) if ident else "unknown class"}), also in Office_Level2')
new_unresolved = [g for g in unresolved if g not in inherited]
if new_unresolved and not package_guids:
    warn('Library/PackageCache not readable; package script GUIDs not checked: ' + ', '.join(new_unresolved))
else:
    check(not new_unresolved, 'every script GUID Office_Level3 adds resolves to a script (unresolved: ' + ', '.join(new_unresolved) + ')')
authored_placements = [meta_guid('Assets/Scripts/Interactables/SupplyCounterPlacement.cs'),
                       meta_guid('Assets/Scripts/Level3/Level3BeaPlacement.cs')]
check(sorted(scripts3) == sorted(scripts2 + authored_placements),
      'Office_Level3 retains Office_Level2 script components and adds only the two authorized placement markers')


def res(rel):
    return json.load(open(os.path.join('Assets/Resources', rel), encoding='utf-8-sig'))


start = res('Levels/Level_03/office_start.json')
for key in ('spawn', 'face', 'fallbackSpawn'):
    check(exists(start[key]), f'office_start {key} exists in Office_Level3: {start[key]}')
for path in start['hide']:
    check(exists(path), 'office_start hide exists in Office_Level3: ' + path)
start2 = res('Levels/Level_02/office_start.json')
check(start['spawn'] == start2['spawn'] and start['face'] == start2['face'] and start['blend'] == start2['blend'],
      'Level 3 reuses Level 2\'s reception placement (spawn, face, blend)')

town = res('Levels/Level_03/town_life.json')
check(exists(town['bea']['anchorPath']), "Bea worktable anchor (her Level 1 bench) exists in Office_Level3: " + town['bea']['anchorPath'])
# T08: Bea waits at the conference room's client spot (the same spot Level 2's Rosa uses); her route matches Rosa's doorway points.
check(exists(town['bea']['officeSpotPath']), "Bea's office spot (conference room client) exists in Office_Level3: " + town['bea']['officeSpotPath'])
check(exists('Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk'), "the consultant desk (Bea's computation sheet and certificate tab) exists in Office_Level3")
# Revised T03: Level3CaseFolder carries the real desk folder (its pages and store) and can put it back on the desk stack.
check(exists('Environment/OfficeBuilding/Rooms/ConsultantOffice/InteractiveFolderObjects/OpenCaseFolder'), 'the real desk Case Folder (Level3CaseFolder) exists in Office_Level3')
check(exists('Environment/OfficeBuilding/Rooms/ConsultantOffice/Desk/StackedCaseFolder'), 'the desk folder stack (put back on the desk) exists in Office_Level3')
rosa = res('Levels/Level_02/town_life.json')['rosa']
rosa_points = [(p['x'], p['z']) for p in rosa['toReception'][:-1] + rosa['toRoom']]
check([(p['x'], p['z']) for p in town['bea']['toOffice']] == rosa_points,
      "Bea's toOffice route is Level 2 Rosa's walked doorway route (Office_Level3 keeps Office_Level2's office)")
guide = res('Tutorials/Level_03/guide.json')
check(all(w['target'].startswith('Level3Gameplay/') for w in guide.get('waypoints', [])),
      "Level 3 guide arrows only lead to new runtime places (Bea table), never into the known office")

# ---------- single owners and runtime roots ----------
guid_names = {}
for base, _, names in os.walk('Assets'):
    for n in names:
        if n.endswith('.cs.meta'):
            m = re.search(r'guid: ([0-9a-f]{32})', open(os.path.join(base, n), encoding='utf-8', errors='ignore').read())
            if m:
                guid_names[m.group(1)] = n[:-8]
owners = ['CaseProgressionManager', 'CaseManager', 'GameStateMachine', 'CameraController', 'FirstPersonCameraController',
          'PlayerMovement', 'Interactor', 'AudioManager', 'PlayerProgressManager', 'AuditorSubmissionTray', 'DialogueUI',
          'ValueTransferManager', 'VirtualJoystick', 'MobileLookArea', 'ReceptionistInteractable']
counts = {}
for owner in owners:
    c2 = sum(1 for g in scripts2 if guid_names.get(g) == owner)
    c3 = sum(1 for g in scripts3 if guid_names.get(g) == owner)
    counts[owner] = c3
    check(c2 == c3, f'{owner}: Office_Level3 keeps Office_Level2\'s count ({c3} vs {c2})')
for runtime in ('Level3OfficeStart', 'Level3Checkpointer', 'Level2OfficeStart', 'OfficeHudRuntime', 'GuideRuntime', 'OfficeMinimap', 'SceneFade'):
    check(sum(1 for g in scripts3 if guid_names.get(g) == runtime) == 0, f'{runtime} is not serialized in Office_Level3 (built once at runtime)')
check('Level3Gameplay' not in roots3 and 'Level2Gameplay' not in roots3, 'no serialized Level3Gameplay/Level2Gameplay root')
check(roots3.count('Player') == 1 and roots3.count('Main Camera') == 1, 'one Player and one root Main Camera')
check(roots3.count('EventSystem') <= 1, 'at most one root EventSystem')

# ---------- build settings ----------
build = open('ProjectSettings/EditorBuildSettings.asset', encoding='utf-8').read()
entries = re.findall(r'- enabled: (\d)\s+path: (.+?)\s+guid: ([0-9a-f]{32})', build)
paths = [e[1].strip() for e in entries]
level3_entries = [e for e in entries if e[1].strip() == LEVEL3]
check(paths[:len(BUILD_BEFORE)] == BUILD_BEFORE, 'existing build entries keep their order (no index shift)')
if not level3_entries:
    warn('Office_Level3 is not in EditorBuildSettings yet: Level3SceneSetup appends it when Unity imports the project (pending user import).')
else:
    check(len(level3_entries) == 1 and level3_entries[0][0] == '1' and level3_entries[0][2] == LEVEL3_GUID,
          'one enabled Office_Level3 build entry with its own GUID')
    check(paths.index(LEVEL3) >= len(BUILD_BEFORE), 'Office_Level3 is appended after the existing entries')

# ---------- career gate ----------
chapters = res('Career/chapters.json')['chapters']
chapter3 = [c for c in chapters if c.get('id') == 'Level_03']
check(len(chapter3) == 1 and chapter3[0].get('comingSoon') is False and chapter3[0].get('scene') == 'Office_Level3',
      'Career Chapter 3 routes to Office_Level3 for APK playtesting')

print(f'VerifyLevel3Scene: {"PASS" if fails == 0 else str(fails) + " FAIL"} ({warns} warning(s)); '
      f'Office_Level3 GUID {LEVEL3_GUID}; current sha256 {level3_hash[:16]}... (copied from {COPY_SOURCE[:16]}...); '
      f'GameStateMachine components {counts.get("GameStateMachine")} (inherited from Office_Level2)')
sys.exit(1 if fails else 0)
