"""Milestone T02: qualify ASSET_REFERENCE_INVENTORY.csv rows against actual saved/runtime use (read-only on Assets).

Run from the project root:  python3 -B Tests/QualifyT02Inventory.py [--write]
For every row: is the asset reached from an ACTIVE object in a build-list scene (saved hierarchy: the
object and all its parents active; prefab-instance root not switched off), only from inactive objects,
from a Resources asset that runtime code loads (scene_assets, shop_assets, cutscene catalogues), only from
non-build scenes, or not at all. Prefab nesting and material->texture links are followed. Runtime startup
hides done by code are NOT traced (stated in the evidence column). --write adds/updates the columns
use_status, use_evidence and t02_disposition and appends rows for pilot staging assets not yet listed.
"""
import csv, pathlib, re, sys, collections

ROOT = pathlib.Path(__file__).resolve().parents[1]
A = ROOT / 'Assets'
CSV = ROOT / 'Documentation/Level1To3AdventureMilestone/ASSET_REFERENCE_INVENTORY.csv'
GUID_RE = re.compile(r'guid: ([0-9a-f]{32})')
guid_path, path_guid = {}, {}
for meta in A.rglob('*.meta'):
    m = re.search(r'^guid: ([0-9a-f]{32})', meta.read_text(errors='replace'), re.M)
    if m:
        p = str(meta)[:-5]
        rel = 'Assets/' + str(pathlib.Path(p).relative_to(A))
        guid_path[m.group(1)] = rel
        path_guid[rel] = m.group(1)

build = [l.split('path: ', 1)[1].strip() for l in (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text().splitlines() if 'path: ' in l]
enabled = set()
lines = (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text().splitlines()
for i, l in enumerate(lines):
    if 'enabled: 1' in l and i + 1 < len(lines) and 'path: ' in lines[i + 1]:
        enabled.add(lines[i + 1].split('path: ', 1)[1].strip())


def docs(text):
    parts = re.split(r'\n--- !u!(\d+) &(-?\d+)( stripped)?\n', text)
    return {parts[i + 1]: (parts[i], parts[i + 3]) for i in range(1, len(parts) - 1, 4)}


def scene_refs(path):
    """GUIDs referenced by active and by inactive objects of one scene (saved hierarchy)."""
    d = docs((ROOT / path).read_text(errors='replace'))
    go_active, tr_go, tr_parent, go_tr = {}, {}, {}, {}
    for fid, (cls, body) in d.items():
        if cls == '1':
            go_active[fid] = 'm_IsActive: 0' not in body
        if cls in ('4', '224'):
            g = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', body); f = re.search(r'm_Father: \{fileID: (-?\d+)\}', body)
            if g: tr_go[fid] = g.group(1); go_tr[g.group(1)] = fid
            if f: tr_parent[fid] = f.group(1)
    inst_parent, inst_off = {}, set()
    for fid, (cls, body) in d.items():
        if cls == '1001':
            p = re.search(r'm_TransformParent: \{fileID: (-?\d+)\}', body)
            inst_parent[fid] = p.group(1) if p else '0'
            for m in re.finditer(r'target: \{fileID: (-?\d+), guid: ([0-9a-f]{32}), type: 3\}\s*\n\s*propertyPath: m_IsActive\s*\n\s*value: 0', body):
                src = guid_path.get(m.group(2), '')
                if src.endswith('.prefab'):
                    t = (ROOT / src).read_text(errors='replace')
                    root = next((re.search(r'm_GameObject: \{fileID: (-?\d+)', b).group(1) for f2, (c2, b) in docs(t).items() if c2 == '4' and 'm_Father: {fileID: 0}' in b), None)
                    if root == m.group(1): inst_off.add(fid)
                elif src: inst_off.add(fid) if m.group(1) in ('919132149155446097', '100100000') else None
    memo = {}

    def transform_active(t, depth=0):
        if t in ('0', None) or depth > 200: return True
        if t in memo: return memo[t]
        g = tr_go.get(t)
        cls, body = d.get(t, ('', ''))
        own = go_active.get(g, True)
        if g is None:  # stripped transform of a prefab instance
            pi = re.search(r'm_PrefabInstance: \{fileID: (-?\d+)\}', body)
            res = instance_active(pi.group(1), depth + 1) if pi else True
        else:
            res = own and transform_active(tr_parent.get(t, '0'), depth + 1)
        memo[t] = res
        return res

    def instance_active(i, depth=0):
        if i in inst_off: return False
        return transform_active(inst_parent.get(i, '0'), depth + 1)

    active, inactive = collections.defaultdict(set), collections.defaultdict(set)
    for fid, (cls, body) in d.items():
        refs = set(GUID_RE.findall(body))
        if not refs: continue
        if cls == '1001':
            on = instance_active(fid)
        else:
            g = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', body)
            if not g: continue
            t = go_tr.get(g.group(1))
            on = go_active.get(g.group(1), True) and (transform_active(t) if t else True)
        for r in refs:
            (active if on else inactive)[r].add(path)
    return active, inactive


def closure(seed_guids):
    """Follow prefab nesting, prefab/model/material/controller/asset links to textures (anything a used asset pulls in)."""
    seen, stack = set(), list(seed_guids)
    while stack:
        g = stack.pop()
        if g in seen: continue
        seen.add(g)
        p = guid_path.get(g, '')
        if p.endswith(('.prefab', '.mat', '.controller', '.asset', '.overrideController')):
            try: stack.extend(set(GUID_RE.findall((ROOT / p).read_text(errors='replace'))))
            except Exception: pass
        if p.endswith(('.fbx', '.FBX')):
            meta = (ROOT / (p + '.meta')).read_text(errors='replace')
            stack.extend(set(GUID_RE.findall(meta)) - {g})
    return seen


status, evidence = {}, collections.defaultdict(list)
active_roots, inactive_roots = collections.defaultdict(set), collections.defaultdict(set)
for scene in sorted(set(build)):
    a, i = scene_refs(scene)
    tag = scene if scene in enabled else scene + ' (disabled in build list)'
    for g, s in a.items(): active_roots[g].add(tag)
    for g, s in i.items(): inactive_roots[g].add(tag)
resources = {}
for res in (A / 'Resources').rglob('*'):
    if res.suffix in ('.asset', '.prefab', '.mat', '.controller') and res.is_file():
        resources['Assets/' + str(res.relative_to(A))] = set(GUID_RE.findall(res.read_text(errors='replace')))
# Resources loaded by path from code (Resources.Load("Campaign/...")): the file itself is a runtime resource.
code = '\n'.join(f.read_text(errors='replace') for f in (A / 'Scripts').rglob('*.cs'))
literals = set(re.findall(r'"([A-Za-z0-9_ ./-]+)"', code))
for res in (A / 'Resources').rglob('*'):
    if not res.is_file() or res.suffix == '.meta': continue
    key = str(res.relative_to(A / 'Resources').with_suffix(''))
    if key in literals:
        rel = 'Assets/' + str(res.relative_to(A))
        resources.setdefault(rel + ' (Resources.Load by path)', set()).add(path_guid.get(rel, ''))
non_build = collections.defaultdict(set)
for sc in A.rglob('*.unity'):
    rel = 'Assets/' + str(sc.relative_to(A))
    if rel in build or 'Pilot/T02' in rel: continue
    for g in set(GUID_RE.findall(sc.read_text(errors='replace'))): non_build[g].add(rel)

used_active, used_inactive = collections.defaultdict(set), collections.defaultdict(set)
for g, scenes in active_roots.items():
    for c in closure([g]): used_active[c] |= {('scene', s, guid_path.get(g, g)) for s in scenes}
for g, scenes in inactive_roots.items():
    for c in closure([g]): used_inactive[c] |= {('scene', s, guid_path.get(g, g)) for s in scenes}
used_resource = collections.defaultdict(set)
for res, refs in resources.items():
    for g in refs:
        for c in closure([g]): used_resource[c].add(res)
nb = collections.defaultdict(set)
for g, scenes in non_build.items():
    for c in closure([g]): nb[c] |= scenes

PILOT = {
    'Assets/3D Models/npc_tutorial_static/npc_tutorial_static1@Standing W_Briefcase Idle.fbx': 'T02 pilot subject: derived Arthur_T02_Shading/Silhouette copies under Assets/Pilot/T02/Derived; original retained unchanged; adoption waits for user acceptance + measured cost',
    'Assets/Synty/PolygonOffice/Models/SM_Prop_Folder_Manila_02.fbx': 'T02 pilot contact subject (handled closed folder); diagnosed intentional hard edges, no geometry/material change; original used in every bay',
    'Assets/Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Folder_Manila_02.prefab': 'T02 pilot contact subject; unchanged original instanced in the review fixture',
    'Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Home Interior/Models/Prop_Washer_01.fbx': 'T02 laundry work-surface candidate (not yet used in gameplay): derived Washer01_T02_Shading/Silhouette copies; T08 decides adoption',
    'Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Home Interior/Prefabs/Prop_Washer_01.prefab': 'T02 laundry staging (pilot only); T08 decides adoption',
    'Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Home Interior/Materials/Panda Mat.mat': 'T02 diagnosis: URP Lit smoothness 0.5 reads glossy; derived PandaMat_T02_Matte (0.18) compared in bays B/C; original unchanged',
    'Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Wall_Door_01.prefab': 'T02 laundry doorway staging (pilot only): deliberate hard architecture, no treatment',
    'Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Wall_01.prefab': 'T02 laundry wall staging (pilot only): deliberate hard architecture, no treatment',
    'Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab': 'T02 laundry floor staging (pilot only): no treatment',
    'Assets/Synty/PolygonOffice/Prefabs/Props/Desk Props/SM_Prop_Folder_Holder_02.prefab': 'T02 staging: upright rest for the hook grip (scaled 0.7 in the fixture only)',
    'Assets/AssetIntegration/NpcHumanoid.controller': 'T02: shared states/clips reused through the derived NpcHumanoid_T02Pilot.controller (IK pass on); original unchanged',
}


def classify(path):
    g = path_guid.get(path)
    if g is None: return 'missing', 'no .meta GUID found', 'unknown activity: blocks this asset\'s migration'
    if g in used_active:
        ev = sorted({f'{s} via {via}' for _, s, via in used_active[g]})[:3]
        return 'used:active-scene', '; '.join(ev) + ' (saved hierarchy; runtime startup hides and prefab-internal child activity not traced)', 'qualified used: individual review by its owning slice after pilot acceptance'
    if g in used_resource:
        return 'used:runtime-resource', '; '.join(sorted(used_resource[g])[:3]) + ' (loaded by runtime builders)', 'qualified used: individual review by its owning slice after pilot acceptance'
    if g in used_inactive:
        ev = sorted({f'{s} via {via}' for _, s, via in used_inactive[g]})[:3]
        return 'inactive:saved-hierarchy', '; '.join(ev), 'retain/no migration (inactive; a runtime enable would need proof first)'
    if g in nb:
        return 'deferred:non-build-scene', '; '.join(sorted(nb[g])[:3]), 'retain/no migration'
    return 'unreached', 'not referenced by build scenes, Resources or other scenes', 'retain/no migration'


rows = list(csv.DictReader(open(CSV, encoding='utf-8')))
fields = list(rows[0].keys())
for extra in ('use_status', 'use_evidence', 't02_disposition'):
    if extra not in fields: fields.append(extra)
counts = collections.Counter()
listed = {r['asset'] for r in rows}
for r in rows:
    st, ev, disp = classify(r['asset'])
    if r['asset'] in PILOT: disp = PILOT[r['asset']] + ' | ' + disp
    r['use_status'], r['use_evidence'], r['t02_disposition'] = st, ev, disp
    counts[st] += 1
added = 0
for path, note in PILOT.items():
    if path in listed: continue
    st, ev, disp = classify(path)
    row = {k: '' for k in fields}
    row.update({'asset': path, 'kind': path.rsplit('.', 1)[-1].lower(), 'reached_from': 'Assets/Pilot/T02/T02_PilotReview.unity (T02 pilot fixture)',
                'qualification': 'added by T02 qualification', 'use_status': st, 'use_evidence': ev, 't02_disposition': note + ' | ' + disp,
                'primary_owner': 'T02 pilot; adopting slice decides', 'acceptance': 'user pilot appearance/contact + measured current-phone cost'})
    rows.append(row); added += 1
print('rows', len(rows), 'added', added, dict(counts))
by_kind = collections.Counter((r['kind'], r['use_status']) for r in rows)
for k in sorted(by_kind): print('  ', k, by_kind[k])
if '--write' in sys.argv:
    with open(CSV, 'w', encoding='utf-8', newline='') as f:
        w = csv.DictWriter(f, fieldnames=fields, lineterminator='\n')
        w.writeheader(); w.writerows(rows)
    print('written', CSV)
