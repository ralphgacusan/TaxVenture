"""Milestone T02 visual/contact pilot: static reference, GUID and asset checks (read-only).

Run from the project root:  python3 -B Tests/VerifyT02Pilot.py
Checks that every pilot asset has a unique GUID, that derived models are byte-identical copies whose
.meta differs from the source only in the intended import settings, that the derived controller and
material differ only where stated, that every scene reference resolves to a real asset and file ID,
that the originals are unchanged (SHA-256 recorded before the pilot), and that no gameplay scene, prefab,
resource or build list refers to the pilot. Unity import, rendering and device costs are NOT checked.
"""
import hashlib
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
A = ROOT / 'Assets'
fails = []
passes = 0


def check(ok, what):
    global passes
    if ok:
        passes += 1
    else:
        fails.append(what)
        print('FAIL:', what)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid_of(meta):
    m = re.search(r'^guid: ([0-9a-f]{32})', meta.read_text(errors='replace'), re.M)
    return m.group(1) if m else None


ARTHUR = '3D Models/npc_tutorial_static/npc_tutorial_static1@Standing W_Briefcase Idle.fbx'
WASHER = 'Pandazole_Lowpoly_Asset_Bundle/Pandazole Home Interior/Models/Prop_Washer_01.fbx'
ORIGINALS = {  # recorded 11 November 2026 before any pilot asset existed
    ARTHUR: '420866e5321563aafc6e4da7a15b7ca9fea42a89a33783321aee7daeb13b2c1a',
    ARTHUR + '.meta': 'bb8f8510c1937e12dc45b8241bb87bca89ad3edf7359c971ed5bcf54d4184d9b',
    WASHER: 'b00c358a17ee47ad0a182cdff0985d9493612bb39f44ed2969abe506b50db1c6',
    WASHER + '.meta': '716303e8f14a7c0caee394fba6145be2f31558dc32e904e030f091ab1e7942e1',
    'Pandazole_Lowpoly_Asset_Bundle/Pandazole Home Interior/Prefabs/Prop_Washer_01.prefab': '57c20bebf5f4ebd7ae3d013b5b7a3fc6b248c1e124ca4a492e2a89fa157044b6',
    'Pandazole_Lowpoly_Asset_Bundle/Pandazole Home Interior/Materials/Panda Mat.mat': '3ce11eaae4be06252fcac9e7abe9e6152dd07ea9d3875c5c0f75bb6d176e7f57',
    'AssetIntegration/NpcHumanoid.controller': 'cfa3af009de7b47eae4667b7453c56d6c0cef37076d9a163eeb33454c459781b',
    'Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Folder_Manila_02.prefab': 'add0598c149b72185b6a4a7ccdde33ec4cf54e4eeb3388a23fdaf440a877d1ad',
    'Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Wall_Door_01.prefab': '0ec0a5caedbeabedbd9258f5cb7b2af384425b1317938b61bff3bc1e8911081d',
    'Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Wall_01.prefab': 'a21f2530b863f0c341a41a903489b135607bf24dec5ea2c3c06d599d6859d593',
    'Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab': '62e3b1f196d67651ae28a8aeeadb75e651d176fe8eef9974e15c938965dd97ee',
}
for rel, digest in ORIGINALS.items():
    check((A / rel).exists() and sha(A / rel) == digest, f'original unchanged: {rel}')

PILOT_FILES = [
    'Pilot', 'Pilot/T02', 'Pilot/T02/Derived', 'Pilot/T02/Materials', 'Pilot/T02/Animation', 'Scripts/Pilot', 'Editor/Pilot',
    'Pilot/T02/T02_PilotReview.unity', 'Pilot/T02/Animation/NpcHumanoid_T02Pilot.controller', 'Pilot/T02/Materials/PandaMat_T02_Matte.mat',
    'Pilot/T02/Derived/Arthur_T02_Shading.fbx', 'Pilot/T02/Derived/Arthur_T02_Silhouette.fbx',
    'Pilot/T02/Derived/Washer01_T02_Shading.fbx', 'Pilot/T02/Derived/Washer01_T02_Silhouette.fbx',
    'Scripts/Pilot/T02PilotRules.cs', 'Scripts/Pilot/T02PilotArthur.cs', 'Scripts/Pilot/T02PilotFixture.cs',
    'Editor/Pilot/T02PilotSilhouette.cs', 'Editor/Pilot/T02PilotTreatment.cs', 'Editor/Pilot/T02PilotMenu.cs',
]
all_guids = {}
for meta in A.rglob('*.meta'):
    g = guid_of(meta)
    if g:
        all_guids.setdefault(g, []).append(meta)
pilot_guids = {}
for rel in PILOT_FILES:
    meta = A / (rel + '.meta')
    check(meta.exists() and (A / rel).exists(), f'{rel} and its .meta exist')
    if meta.exists():
        g = guid_of(meta)
        pilot_guids[rel] = g
        check(g is not None and len(all_guids.get(g, [])) == 1, f'{rel}: GUID {g} is unique in the project')
for p in (A / 'Pilot').rglob('*'):
    if p.suffix != '.meta' and not (p.parent / (p.name + '.meta')).exists():
        check(False, f'every file under Assets/Pilot has a .meta ({p.relative_to(A)})')

# Derived models: byte copies; .meta differs only in guid + normals mode/angle/source + userData flag.
INTENDED = re.compile(r'^(guid: |    isReadable: |    normalImportMode: |    normalSmoothAngle: |    normalSmoothingSource: |  userData: )')
for derived, source, angle, flag in [
    ('Pilot/T02/Derived/Arthur_T02_Shading.fbx', ARTHUR, '50', 't02:shading'),
    ('Pilot/T02/Derived/Arthur_T02_Silhouette.fbx', ARTHUR, '50', 't02:silhouette'),
    ('Pilot/T02/Derived/Washer01_T02_Shading.fbx', WASHER, '40', 't02:shading'),
    ('Pilot/T02/Derived/Washer01_T02_Silhouette.fbx', WASHER, '40', 't02:silhouette'),
]:
    check(sha(A / derived) == sha(A / source), f'{derived} is a byte copy of {source} (UVs, rig, identity untouched)')
    src = (A / (source + '.meta')).read_text().splitlines()
    dst = (A / (derived + '.meta')).read_text().splitlines()
    differing = [d for s, d in zip(src, dst) if s != d]
    check(len(src) == len(dst) and all(INTENDED.match(d) for d in differing), f'{derived}.meta changes only guid/normals/userData lines')
    text = '\n'.join(dst)
    check('    normalImportMode: 1' in text and f'    normalSmoothAngle: {angle}' in text and '    normalSmoothingSource: 2' in text and f'  userData: {flag}' in text,
          f'{derived}: normals calculated from angle {angle}, flag {flag}')
    check('fileIdsGeneration: 2' in text, f'{derived}: root GameObject file ID scheme 919132149155446097 applies')
    check(f'    isReadable: {1 if flag == "t02:silhouette" else 0}' in text,
          f'{derived}: silhouette has CPU mesh access before destructive import edits; shading keeps source memory policy')
arthur_meta = (A / 'Pilot/T02/Derived/Arthur_T02_Shading.fbx.meta').read_text()
check('animationType: 3' in arthur_meta and 'avatarSetup: 1' in arthur_meta and arthur_meta.count('guid: a7c6bc6a96f517642b64f32002f2f41f') == 2,
      'Arthur derived copies stay Humanoid with their own avatar and the same remapped Arthur materials')

# Controller and material: only the stated differences.
ctrl_src = (A / 'AssetIntegration/NpcHumanoid.controller').read_text().splitlines()
ctrl_dst = (A / 'Pilot/T02/Animation/NpcHumanoid_T02Pilot.controller').read_text().splitlines()
diff = [(s, d) for s, d in zip(ctrl_src, ctrl_dst) if s != d]
check(len(ctrl_src) == len(ctrl_dst) and sorted(d for _, d in diff) == ['    m_IKPass: 1', '  m_Name: NpcHumanoid_T02Pilot'],
      'pilot controller = the shared NpcHumanoid states and clips, renamed, IK pass on (nothing else)')
mat_src = (A / 'Pandazole_Lowpoly_Asset_Bundle/Pandazole Home Interior/Materials/Panda Mat.mat').read_text().splitlines()
mat_dst = (A / 'Pilot/T02/Materials/PandaMat_T02_Matte.mat').read_text().splitlines()
mdiff = sorted(d for s, d in zip(mat_src, mat_dst) if s != d)
check(len(mat_src) == len(mat_dst) and mdiff == ['    - _Glossiness: 0.18', '    - _Smoothness: 0.18', '  m_Name: PandaMat_T02_Matte'],
      'matte washer material = Panda Mat with lower smoothness only (same shader, texture, colours)')

# Scene references.
scene = (A / 'Pilot/T02/T02_PilotReview.unity').read_text()
fixture_src = (A / 'Scripts/Pilot/T02PilotFixture.cs').read_text()
check(scene.count('--- !u!1 &') == 1 and scene.count('--- !u!114 &') == 1 and 'SceneRoots:' in scene,
      'the review scene holds exactly one object (the fixture); everything else is built at Play')
check(f'm_Script: {{fileID: 11500000, guid: {pilot_guids["Scripts/Pilot/T02PilotFixture.cs"]}, type: 3}}' in scene, 'scene binds T02PilotFixture by its script GUID')


def root_go(prefab):
    t = prefab.read_text()
    for fid, body in re.findall(r'--- !u!4 &(-?\d+)\nTransform:(.*?)(?=\n--- |\Z)', t, re.S):
        if 'm_Father: {fileID: 0}' in body:
            return re.search(r'm_GameObject: \{fileID: (-?\d+)', body).group(1)
    return None


by_guid = {g: metas[0] for g, metas in all_guids.items()}
component = scene[scene.index('--- !u!114 &'):scene.index('--- !u!1660057539')]
for field, fid, g, kind in re.findall(r'^  (\w+): \{fileID: (-?\d+), guid: ([0-9a-f]{32}), type: (\d)\}', component, re.M):
    if field == 'm_Script':
        continue
    meta = by_guid.get(g)
    check(meta is not None, f'scene field {field}: GUID {g} resolves to an asset')
    check(re.search(r'public (GameObject|Material|RuntimeAnimatorController) ' + field + ';', fixture_src) is not None, f'scene field {field} is a serialized fixture field')
    if meta is None:
        continue
    asset = pathlib.Path(str(meta)[:-5])
    if asset.suffix == '.fbx':
        check(fid == '919132149155446097', f'{field}: references the model root of {asset.name}')
    elif asset.suffix == '.prefab':
        check(fid == root_go(asset), f'{field}: references the root GameObject of {asset.name}')
    elif asset.suffix == '.mat':
        check(fid == '2100000', f'{field}: references the material {asset.name}')
    elif asset.suffix == '.controller':
        check(fid == '9100000', f'{field}: references the controller {asset.name}')

# Isolation from the game: no build list, scene, prefab or resource refers to the pilot.
build = (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text()
check(pilot_guids['Pilot/T02/T02_PilotReview.unity'] not in build and 'T02_PilotReview' not in build, 'the pilot scene is not in the game build list')
derived_guids = {pilot_guids[r] for r in PILOT_FILES if r.startswith('Pilot/T02/') and '.' in r.rsplit('/', 1)[-1] and not r.endswith('.unity')}
users = []
for p in list(A.rglob('*.unity')) + list(A.rglob('*.prefab')) + list(A.rglob('*.asset')) + list(A.rglob('*.controller')):
    if 'Pilot/T02' in str(p):
        continue
    t = p.read_text(errors='replace')
    if any(g in t for g in derived_guids):
        users.append(str(p.relative_to(A)))
check(not users, 'no gameplay scene/prefab/resource uses a pilot-derived asset (no migration): ' + ', '.join(users[:5]))
ppm = (A / 'Scripts/RewardsSystem/PlayerProgressManager.cs').read_text()
check(ppm.count('T02PilotIsolation.IsPilotScene') == 2, 'PlayerProgressManager bootstrap has the pilot-scene guard (active scene and first loaded scene)')

print(f'VerifyT02Pilot: {"PASS" if not fails else "FAIL"} ({passes} checks passed, {len(fails)} failed)')
sys.exit(1 if fails else 0)
