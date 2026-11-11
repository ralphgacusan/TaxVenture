"""Milestone T02: export the pilot source FBX meshes (control points, polygons, per-corner UV, skin weights) to Temp/T02Pilot/source_meshes.json for T02PilotRegression. Read-only on Assets.

Run from the project root: python3 -B Tests/T02PilotRegression/export_source_meshes.py
"""
import sys, json, collections
import pathlib
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent)); import fbxinfo as F
def export(path):
    ver, top = F.read(path)
    objs = [t for t in top if t[0] == 'Objects'][0][2]
    conns = [t for t in top if t[0] == 'Connections'][0][2]
    byId = {o[1][0]: o for o in objs}
    child = collections.defaultdict(list)
    for c in conns:
        if c[1][0] in (b'OO', b'OP'): child[c[1][2]].append(c[1][1])
    meshes = []
    for gid, o in byId.items():
        if o[0] != 'Geometry' or F.nm(o[1][2]) != 'Mesh': continue
        V = F.find(o, 'Vertices')[0][1][0]; P = F.find(o, 'PolygonVertexIndex')[0][1][0]
        polys = []; cur = []
        for i in P:
            if i < 0: cur.append(~i); polys.append(cur); cur = []
            else: cur.append(i)
        uvs = []
        ul = F.find(o, 'LayerElementUV')
        if ul:
            mapping = F.find(ul[0], 'MappingInformationType')[0][1][0]; ref = F.find(ul[0], 'ReferenceInformationType')[0][1][0]
            UV = F.find(ul[0], 'UV')[0][1][0]; idx = F.find(ul[0], 'UVIndex'); idx = idx[0][1][0] if idx else None
            k = 0
            for poly in polys:
                for cp in poly:
                    if mapping == b'ByPolygonVertex': j = idx[k] if (idx and ref == b'IndexToDirect') else k
                    else: j = idx[cp] if (idx and ref == b'IndexToDirect') else cp
                    uvs.append([UV[j*2], UV[j*2+1]]); k += 1
        weights = collections.defaultdict(list)
        for d in child[gid]:
            if byId.get(d, ('',))[0] != 'Deformer': continue
            for bi, cl in enumerate(child[d]):
                c = byId.get(cl); w = F.find(c, 'Weights') if c else None; ix = F.find(c, 'Indexes') if c else None
                if w and ix:
                    for a, b in zip(ix[0][1][0], w[0][1][0]): weights[a].append([bi, b])
        meshes.append({'name': F.nm(o[1][1]), 'points': V, 'polygons': polys, 'uvs': uvs,
                       'weights': {str(k): v for k, v in weights.items()}})
    return meshes
ROOT = pathlib.Path(__file__).resolve().parents[2]
SOURCES = {
    'elijah_exploration': 'Assets/3D Models/npc_elijah/Elijah_Walking.fbx',
    'elijah_conversation': 'Assets/3D Models/npc_elijah/npc_elijah_Idle.fbx',
    'arthur': 'Assets/3D Models/npc_tutorial_static/npc_tutorial_static1@Standing W_Briefcase Idle.fbx',
    'washer': 'Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Home Interior/Models/Prop_Washer_01.fbx',
    'folder': 'Assets/Synty/PolygonOffice/Models/SM_Prop_Folder_Manila_02.fbx',
}
target = ROOT / 'Temp/T02Pilot/source_meshes.json'
target.parent.mkdir(parents=True, exist_ok=True)
out = {label: export(str(ROOT / path)) for label, path in SOURCES.items()}
json.dump(out, open(target, 'w'))
print({k: [(m['name'], len(m['polygons'])) for m in v] for k, v in out.items()})
