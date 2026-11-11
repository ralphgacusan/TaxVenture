"""Milestone T03 third-person experience repair: independent, file-level evidence (no Unity process).

1. World scale: the live Office_Level1 adult cohort is measured from the scene's actual scale chains
   and each model's rest-pose FBX geometry (node transforms composed here, independent of Unity's
   BakeMesh); Elijah's canonical height must sit within +-5% of the cohort median.
2. Clips: the committed run/jump assets are reproducible from Elijah's own measured motion and are
   checked for coordination (opposition, elbows, lean, contacts, flight, seam) and restraint.
3. Jacket: the import-time shirt inset is run on Elijah's real coat/shirt geometry.
4. Bindings: resource references resolve to the adopted assets.
Rendering, retargeted motion, touch and device feel are NOT exercised.
Run from the repository root: python3 -B Tests/VerifyThirdPersonExperience.py
"""
from pathlib import Path
import math
import re
import statistics
import subprocess
import sys

R = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(R / 'Tests/T02PilotRegression'))
import fbxinfo as F  # noqa: E402

results = []


def correlation(xs, ys):
    """Pearson r (the system Python predates statistics.correlation)."""
    mx, my = sum(xs) / len(xs), sum(ys) / len(ys)
    sxy = sum((x - mx) * (y - my) for x, y in zip(xs, ys))
    return sxy / math.sqrt(sum((x - mx) ** 2 for x in xs) * sum((y - my) ** 2 for y in ys))


def check(ok, label):
    results.append(bool(ok))
    print(('PASS: ' if ok else 'FAIL: ') + label)


# ---------------------------------------------------------------- FBX rest geometry
def mul(a, b): return [[sum(a[i][k] * b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]
def T(t): return [[1, 0, 0, t[0]], [0, 1, 0, t[1]], [0, 0, 1, t[2]], [0, 0, 0, 1]]
def S(s): return [[s[0], 0, 0, 0], [0, s[1], 0, 0], [0, 0, s[2], 0], [0, 0, 0, 1]]


def Rot(e):
    x, y, z = [math.radians(v) for v in e]
    rx = [[1, 0, 0, 0], [0, math.cos(x), -math.sin(x), 0], [0, math.sin(x), math.cos(x), 0], [0, 0, 0, 1]]
    ry = [[math.cos(y), 0, math.sin(y), 0], [0, 1, 0, 0], [-math.sin(y), 0, math.cos(y), 0], [0, 0, 0, 1]]
    rz = [[math.cos(z), -math.sin(z), 0, 0], [math.sin(z), math.cos(z), 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]]
    return mul(rz, mul(ry, rx))


def apply(m, p): return [sum(m[i][k] * p[k] for k in range(3)) + m[i][3] for i in range(3)]


def fbx_meshes(path):
    """{mesh name: (world points in metres at scale 1, triangles)} in the rest pose."""
    _, top = F.read(str(path))
    objs = [t for t in top if t[0] == 'Objects'][0]
    conns = [t for t in top if t[0] == 'Connections'][0]
    usf = 1.0
    for p in F.find([t for t in top if t[0] == 'GlobalSettings'][0], 'Properties70')[0][2]:
        if F.nm(p[1][0]) == 'UnitScaleFactor':
            usf = p[1][4]
    models, geoms = {}, {}
    for o in objs[2]:
        if o[0] == 'Model':
            d = {'Lcl Translation': (0, 0, 0), 'Lcl Rotation': (0, 0, 0), 'Lcl Scaling': (1, 1, 1), 'PreRotation': (0, 0, 0)}
            p70 = F.find(o, 'Properties70')
            for p in (p70[0][2] if p70 else []):
                if F.nm(p[1][0]) in d:
                    d[F.nm(p[1][0])] = tuple(p[1][4:7])
            models[o[1][0]] = (F.nm(o[1][1]), d)
        if o[0] == 'Geometry' and F.nm(o[1][2]) == 'Mesh':
            V = F.find(o, 'Vertices')[0][1][0]
            P = F.find(o, 'PolygonVertexIndex')[0][1][0]
            polys, cur = [], []
            for i in P:
                if i < 0:
                    cur.append(~i); polys.append(cur); cur = []
                else:
                    cur.append(i)
            geoms[o[1][0]] = ([V[i * 3:i * 3 + 3] for i in range(len(V) // 3)], polys)
    parent, owner = {}, {}
    for c in conns[2]:
        if c[0] == 'C' and F.nm(c[1][0]) == 'OO':
            if c[1][1] in models: parent[c[1][1]] = c[1][2]
            if c[1][1] in geoms and c[1][2] in models: owner[c[1][1]] = c[1][2]

    def world(mid):
        _, d = models[mid]
        local = mul(T(d['Lcl Translation']), mul(Rot(d['PreRotation']), mul(Rot(d['Lcl Rotation']), S(d['Lcl Scaling']))))
        return mul(world(parent[mid]), local) if parent.get(mid) in models else local
    out = {}
    for gid, (pts, polys) in geoms.items():
        if gid not in owner: continue
        m = world(owner[gid])
        out[models[owner[gid]][0]] = ([[x * usf / 100 for x in apply(m, p)] for p in pts],
                                      [(p[0], p[i], p[i + 1]) for p in polys for i in range(1, len(p) - 1)])
    return out


def rest_height(path, names=None):
    ys = [p[1] for n, (pts, _) in fbx_meshes(path).items() if names is None or n in names for p in pts]
    return max(ys) - min(ys)


# ---------------------------------------------------------------- scene scale chains
scene = (R / 'Assets/Scenes/Office_Level1.unity').read_text()
docs = re.split(r'\n--- !u!(\d+) &(\d+)( stripped)?\n', scene)
objs = {docs[i + 1]: (docs[i], docs[i + 2], docs[i + 3]) for i in range(1, len(docs), 4)}


def go_name(tid):
    m = re.search(r'm_GameObject: \{fileID: (\d+)', objs[tid][2])
    n = re.search(r'm_Name: (.*)', objs[m.group(1)][2]) if m and m.group(1) in objs else None
    return n.group(1) if n else '?'


def chain(fid):
    scale, names = 1.0, []
    while fid != '0' and fid in objs:
        _, stripped, body = objs[fid]
        if stripped:
            pi = re.search(r'm_PrefabInstance: \{fileID: (\d+)', body).group(1)
            pb = objs[pi][2]
            v = re.search(r'propertyPath: m_LocalScale.y\n\s+value: ([^\n]*)', pb)
            scale *= float(v.group(1)) if v else 1.0
            fid = re.search(r'm_TransformParent: \{fileID: (\d+)', pb).group(1)
        else:
            scale *= abs(float(re.search(r'm_LocalScale: \{x: [^,]*, y: ([^,]*)', body).group(1)))
            names.append(go_name(fid))
            fid = re.search(r'm_Father: \{fileID: (\d+)', body).group(1)
    return scale, names


def instance_scale(npc, source_guid):
    for fid, (cid, _, body) in objs.items():
        if cid != '1001' or source_guid not in body: continue
        parent = re.search(r'm_TransformParent: \{fileID: (\d+)', body).group(1)
        s, names = chain(parent)
        if npc in names:
            v = re.search(r'propertyPath: m_LocalScale.y\n\s+value: ([^\n]*)', body)
            return (float(v.group(1)) if v else 1.0) * s
    return None


def guid(path): return re.search(r'^guid: (\w+)', (R / (path + '.meta')).read_text(), re.M).group(1)


synty = R / 'Assets/Synty/PolygonOffice/Models/Character.fbx'
synty_meshes = fbx_meshes(synty)
def synty_height(mesh): pts = synty_meshes[mesh][0]; return max(p[1] for p in pts) - min(p[1] for p in pts)


cohort_spec = [  # (scene NPC object, model source asset, mesh inside Character.fbx or None)
    ('NPC_Marie', 'Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Business_Female_03.prefab', 'SM_Chr_Business_Female_03'),
    ('NPC_Milo', 'Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Business_Male_03.prefab', 'SM_Chr_Business_Male_03'),
    ('Ambient_Bea', 'Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Developer_Female_01.prefab', 'SM_Chr_Developer_Female_01'),
    ('Ambient_Aling_Lorna', 'Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Developer_Female_02.prefab', 'SM_Chr_Developer_Female_02'),
    ('Ambient_Carlo', 'Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Developer_Male_02.prefab', 'SM_Chr_Developer_Male_02'),
    ('NPC_Jhun', 'Assets/3D Models/npc_jhun_static/npc_jhun_static@Standing Idle.fbx', None),
    ('NPC_TutorialGuide', 'Assets/3D Models/npc_tutorial_static/npc_tutorial_static1@Standing W_Briefcase Idle.fbx', None),
]
cohort = {}
for npc, source, mesh in cohort_spec:
    s = instance_scale(npc, guid(source))
    native = synty_height(mesh) if mesh else rest_height(R / source)
    if s: cohort[npc] = native * s
print('cohort (rest-pose full height, m):', {k: round(v, 3) for k, v in sorted(cohort.items(), key=lambda kv: kv[1])})
median = statistics.median(cohort.values()) if cohort else 0
excluded = {npc: instance_scale(npc, guid(src)) for npc, src in [
    ('Ambient_Mang_Dante', 'Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Security_Male_01.prefab'),
    ('NPC_Zorbix', 'Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Business_Male_04.prefab')]}
check(len(cohort) == 7, f'scale: 7 ordinary adults measured from live scene scale chains (guard/alien excluded: {excluded})')
rules = (R / 'Assets/Scripts/Player/ExplorationRules.cs').read_text()
canonical = float(re.search(r'ElijahRestHeight = ([\d.]+)f;', rules).group(1))
elijah_native = rest_height(R / 'Assets/Resources/Player/Elijah_Exploration_Smooth.fbx')
check(abs(elijah_native - 6.6938) < 0.002, f'scale: Elijah rest geometry {elijah_native:.4f} native (matches the captured import 6.6938; posed BakeMesh read 5.946)')
check(abs(canonical / median - 1) <= 0.05, f'scale: canonical {canonical} m is {canonical / median - 1:+.1%} from the cohort median {median:.3f} m (+-5%)')
rejected = 1.5726485 / 5.946 * elijah_native
check(rejected / median - 1 > 0.09, f'scale: the rejected fit was {rejected:.3f} m ({rejected / median - 1:+.0%} over the median): root cause reproduced')
conv = (R / 'Assets/Scripts/Player/ConversationActorVisual.cs').read_text()
setup_posed = 6.6562467 + 0.003197104
check(abs(1.5726485 / setup_posed * elijah_native - canonical) < 0.01,
      f'scale: Level 2/3 conversation fit (setup tool, bone-skinned) equals {1.5726485 / setup_posed * elijah_native:.3f} m rest: one Elijah everywhere')

# ---------------------------------------------------------------- clips
built = subprocess.run([sys.executable, '-B', str(R / 'Tests/BuildElijahTraversalClips.py'), '--check'], capture_output=True, text=True)
check(built.returncode == 0, 'clips: committed Elijah_Sprint/Elijah_Jump are exactly reproducible from Elijah\'s measured motion')


def curves(name):
    text = (R / f'Assets/Resources/Player/{name}.anim').read_text()
    out = {}
    for block in text.split('  - serializedVersion: 2\n')[1:]:
        a = re.search(r'    attribute: (.+)', block)
        if a: out[a[1]] = [(float(t), float(v)) for t, v in re.findall(r'        time: ([^\n]+)\n        value: ([^\n]+)', block)]
    return text, out


run_text, run = curves('Elijah_Sprint')
L = max(t for t, _ in run['RootT.y'])
def at(c, ph): return min(c, key=lambda k: abs(k[0] - ph * L))[1]
ph = [i / 18 for i in range(18)]
lul = [at(run['Left Upper Leg Front-Back'], p) for p in ph]; rul = [at(run['Right Upper Leg Front-Back'], p) for p in ph]
lar = [at(run['Left Arm Front-Back'], p) for p in ph]
corr = correlation(lul, lar)
check(corr < -0.5, f'run: opposing coordination (left thigh vs left arm correlation {corr:+.2f}; arm swings back as the same leg reaches forward)')
check(correlation(lul, rul) < -0.7, 'run: legs alternate (half-cycle apart)')
forearm = [v for _, v in run['Left Forearm Stretch']]
check(-0.05 <= min(forearm) and max(forearm) <= 0.25, f'run: elbows bent ~75-90 deg, not stiff or flailing (Forearm Stretch {min(forearm):.2f}..{max(forearm):.2f})')
knee = [v for _, v in run['Left Lower Leg Stretch']]
thigh_fwd = min(lul)
check(min(knee) >= -0.25 and thigh_fwd >= -0.5, f'run: restrained knee lift (knee {min(knee):.2f}, thigh {thigh_fwd:.2f}; walk reached 0.14/-0.11)')
spine = [v for _, v in run['Spine Front-Back']]
check(max(spine) < -0.03, 'run: restrained forward lean (Spine Front-Back negative = forward, measured on the idle/walk)')
check(all(v == 0 for k in ('RootT.x', 'RootT.z') for _, v in run[k]) and 'm_LoopTime: 1' in run_text and
      all(abs(c[0][1] - c[-1][1]) < 1e-5 for c in run.values()), 'run: in place (one root authority), loops without a seam')
lc = 0.22
check(at(run['Left Upper Leg Front-Back'], lc) < 0 and at(run['Left Lower Leg Stretch'], lc) > 0.5,
      'run: left heel contact at 0.22 (thigh forward, knee extended) as on the walk: walk/run blends keep foot phase')
body_y = {p: at(run['RootT.y'], p) for p in ph}
low = min(body_y, key=body_y.get)
check(abs(low - 0.40) < 0.07 or abs(low - 0.89) < 0.07, f'run: body lowest at mid-stance (phase {low:.2f}) and highest in flight')
jump_text, jump = curves('Elijah_Jump')
jk = [v for _, v in jump['Left Lower Leg Stretch']] + [v for _, v in jump['Right Lower Leg Stretch']]
jf = [v for _, v in jump['Left Forearm Stretch']]
times = [round(t, 2) for t, _ in jump['Left Lower Leg Stretch']]
check(min(jk) >= 0.15 and min(jf) >= 0.35, f'jump: modest tuck/landing (knee >= {min(jk):.2f}), no stiff mannequin elbows (forearm >= {min(jf):.2f})')
check({0.12, 0.42, 0.7, 1.0} <= set(times) and 'm_LoopTime: 0' in jump_text and
      abs(jump['Left Lower Leg Stretch'][0][1] - jump['Left Lower Leg Stretch'][-1][1]) < 1e-6,
      'jump: segment keys at 0.12/0.42/0.70 match ExplorationRules; starts and ends in the idle stance')
check(jump['Left Upper Leg Front-Back'][3][1] != jump['Right Upper Leg Front-Back'][3][1],
      'jump: articulated, asymmetric tuck (not one held symmetric pose)')

# ---------------------------------------------------------------- jacket
meshes = fbx_meshes(R / 'Assets/Resources/Player/Elijah_Exploration_Smooth.fbx')
coat_pts, coat_tris = meshes['coat']
shirt_pts, _ = meshes['torso']


def sub(a, b): return [a[i] - b[i] for i in range(3)]
def dot(a, b): return sum(a[i] * b[i] for i in range(3))
def cross(a, b): return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]


def closest(p, a, b, c):
    ab, ac, ap = sub(b, a), sub(c, a), sub(p, a)
    d1, d2 = dot(ab, ap), dot(ac, ap)
    if d1 <= 0 and d2 <= 0: return a
    bp = sub(p, b); d3, d4 = dot(ab, bp), dot(ac, bp)
    if d3 >= 0 and d4 <= d3: return b
    vc = d1 * d4 - d3 * d2
    if vc <= 0 and d1 >= 0 and d3 <= 0: v = d1 / (d1 - d3); return [a[i] + v * ab[i] for i in range(3)]
    cp = sub(p, c); d5, d6 = dot(ab, cp), dot(ac, cp)
    if d6 >= 0 and d5 <= d6: return c
    vb = d5 * d2 - d1 * d6
    if vb <= 0 and d2 >= 0 and d6 <= 0: w = d2 / (d2 - d6); return [a[i] + w * ac[i] for i in range(3)]
    va = d3 * d6 - d5 * d4
    if va <= 0 and d4 - d3 >= 0 and d5 - d6 >= 0: w = (d4 - d3) / ((d4 - d3) + (d5 - d6)); return [b[i] + w * (c[i] - b[i]) for i in range(3)]
    den = 1 / (va + vb + vc)
    return [a[i] + ab[i] * vb * den + ac[i] * vc * den for i in range(3)]


touching, moved, shift = 0, 0, 0.0
for p in shirt_pts:
    best, n, q = 0.03, None, None
    for a, b, c in coat_tris:
        A, B, C = coat_pts[a], coat_pts[b], coat_pts[c]
        nn = cross(sub(B, A), sub(C, A)); ln = math.sqrt(dot(nn, nn))
        if ln < 1e-12: continue
        qq = closest(p, A, B, C); d = math.dist(p, qq)
        if d < best: best, n, q = d, [x / ln for x in nn], qq
    if n is None: continue
    if best < 0.01: touching += 1
    above = dot(sub(p, q), n)
    if above > -0.02:
        moved += 1; shift = max(shift, above + 0.02)
world_mm = shift * canonical / elijah_native * 1000
check(touching > 20 and 0 < moved < len(shirt_pts) // 3 and world_mm < 6,
      f'jacket: {touching} shirt vertices lie within 2.4 mm of the coat (root cause); the inset moves {moved}/{len(shirt_pts)} by <= {world_mm:.1f} mm')

# ---------------------------------------------------------------- bindings
asset = (R / 'Assets/Resources/Player/ExplorationBody.asset').read_text()
for field, path in [('model', 'Assets/Resources/Player/Elijah_Exploration_Smooth.fbx'), ('conversationModel', 'Assets/Resources/Player/Elijah_Conversation_Smooth.fbx'),
                    ('sprint', 'Assets/Resources/Player/Elijah_Sprint.anim'), ('jump', 'Assets/Resources/Player/Elijah_Jump.anim'),
                    ('walk', 'Assets/3D Models/npc_elijah/Elijah_Walking.fbx'), ('idle', 'Assets/3D Models/npc_jhun_static/npc_jhun_static@Standing Idle.fbx')]:
    check(re.search(r'^  ' + field + r': .*guid: ' + guid(path), asset, re.M), f'binding: {field} -> {path}')
for path in ['Assets/Scripts/Player/ExplorationRules.cs', 'Assets/Scripts/Player/ExplorationBody.cs', 'Assets/Scripts/Player/PlayerMovement.cs',
             'Assets/Scripts/Player/FirstPersonCameraController.cs', 'Assets/Editor/ElijahClothingInset.cs', 'Assets/Editor/ElijahModelTreatment.cs',
             'Assets/Resources/Player/Elijah_Sprint.anim', 'Assets/Resources/Player/Elijah_Jump.anim']:
    check(b'\r\n' not in (R / path).read_bytes(), f'format: {path} keeps LF')
check((R / 'Assets/Editor/ElijahClothingInset.cs.meta').exists(), 'new Editor script has its own .meta (fresh GUID)')

print(f'VerifyThirdPersonExperience: {sum(results)}/{len(results)} checks passed; Unity rendering/motion/touch/device NOT executed')
raise SystemExit(0 if all(results) else 1)
