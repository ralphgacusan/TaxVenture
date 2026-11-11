"""Milestone T03: author Elijah's in-place Humanoid run and jump from his own measured motion.

Writes only Assets/Resources/Player/Elijah_Sprint.anim (the outer-ring run) and Elijah_Jump.anim
(existing GUIDs kept). Source data: Tests/ElijahClipSource.json (Tests/ExtractElijahClipSource.py),
i.e. Elijah's imported walk (all 130 Humanoid curves) and the shared idle's standing pose.

Muscle conventions are taken from that data, not assumed: on the walk, the leg whose foot goal
is ahead has the LOWER 'Upper Leg Front-Back'; the arm opposite the forward leg has the lower
'Arm Front-Back' (negative = forward for both); 'Lower Leg/Forearm Stretch' 1 = straight;
'Foot Up-Down' is negative at toe-off. Heel contacts (foot goal furthest ahead): left 0.22, right 0.71.

RUN (0.60 s cycle, loops): each leg replays the walk's own leg motion with its stance shortened
from the walk's ~57-61% to 36% of the cycle (a flight phase appears), hip flexion amplified,
restrained swing knee flexion and stance absorption added. Each arm replays the same warped
timing as its own leg (the walk's opposition is kept exactly) with a larger swing and ~80 deg elbows; trunk leans slightly forward with more counter-rotation;
the body is lowest at mid-stance and highest in flight. Contacts stay at 0.22/0.71, so the
body's shared walk/run phase blends without a foot-phase jump. In place (RootT.xz = 0).

JUMP (1.0 s, segments per ExplorationRules): 0-0.12 windup crouch, 0.12-0.42 push-off, rise and
an asymmetric tuck to the apex, 0.42-0.70 legs extend toward the floor, 0.70-1.0 modest landing
compression and recovery to the idle stance. Monotone tangents (no overshoot).

Run from the repository root: python3 -B Tests/BuildElijahTraversalClips.py [--check]
--check verifies the committed clips match what this script would write.
"""
from pathlib import Path
import bisect
import json
import math
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = json.loads((ROOT / 'Tests/ElijahClipSource.json').read_text())
WALK = SOURCE['walk']
IDLE = SOURCE['idle']
TEMPLATE = (ROOT / 'Assets/3D Models/npc_tutorial_static/Standing W_Briefcase Idle.anim').read_text()
WALK_LEN = max(k[0] for keys in WALK.values() for k in keys)
LEFT_CONTACT, RIGHT_CONTACT = 0.22, 0.71  # = ExplorationRules.LeftContact/RightContact
RUN_STANCE = 0.36
RUN_LEN = 0.60
SIDES = ('Left', 'Right')
# IK goals from a walk/idle would pin the feet; the body uses muscles (foot IK is off for run/jump).
ATTRIBUTES = [a for a in WALK if not re.match(r'(Left|Right)(Foot|Hand)[TQ]\.', a)]


def hermite(keys, t):
    if len(keys) == 1:
        return keys[0][1]
    times = [k[0] for k in keys]
    i = max(0, min(bisect.bisect_right(times, t) - 1, len(keys) - 2))
    t0, v0, _, o0 = keys[i]
    t1, v1, i1, _ = keys[i + 1]
    h = t1 - t0
    s = min(1.0, max(0.0, (t - t0) / h)) if h > 0 else 0.0
    return ((2 * s ** 3 - 3 * s ** 2 + 1) * v0 + (s ** 3 - 2 * s ** 2 + s) * h * o0 +
            (-2 * s ** 3 + 3 * s ** 2) * v1 + (s ** 3 - s ** 2) * h * i1)


def walk(name, phase):
    return hermite(WALK[name], (phase % 1.0) * WALK_LEN)


def phase_of(name, pick):
    samples = [(walk(name, i / 240), i / 240) for i in range(240)]
    return pick(samples)[1]


# Measured on the walk: heel contact (foot goal furthest ahead) and toe-off (toes most curled, a little earlier).
CONTACT = {'Left': phase_of('LeftFootT.z', max), 'Right': phase_of('RightFootT.z', max)}
TOE_OFF = {s: (phase_of(f'{s} Toes Up-Down', min) - 0.03) % 1.0 for s in SIDES}
WALK_STANCE = {s: (TOE_OFF[s] - CONTACT[s]) % 1.0 for s in SIDES}
ARM_BACK = {s: phase_of(f'{s} Arm Front-Back', max) for s in SIDES}
assert abs(CONTACT['Left'] - LEFT_CONTACT) < 0.01 and abs(CONTACT['Right'] - RIGHT_CONTACT) < 0.01, CONTACT
LEG_MUSCLES = ('Upper Leg Front-Back', 'Upper Leg In-Out', 'Upper Leg Twist In-Out', 'Lower Leg Stretch',
               'Lower Leg Twist In-Out', 'Foot Up-Down', 'Foot Twist In-Out', 'Toes Up-Down')


def warped(side, phase):
    """Run phase -> the walk phase whose leg pose it replays (shorter stance, longer swing)."""
    contact = LEFT_CONTACT if side == 'Left' else RIGHT_CONTACT
    p = (phase - contact) % 1.0
    stance = WALK_STANCE[side]
    q = p / RUN_STANCE * stance if p < RUN_STANCE else stance + (p - RUN_STANCE) / (1 - RUN_STANCE) * (1 - stance)
    return p, (CONTACT[side] + q) % 1.0


def run(name, phase):
    phase %= 1.0
    for side in SIDES:
        if name.startswith(side + ' ') and any(name.endswith(m) for m in LEG_MUSCLES):
            p, at = warped(side, phase)
            v = walk(name, at)
            if name.endswith('Upper Leg Front-Back'):
                ref = 0.45
                return ref + (v - ref) * (1.55 if v < ref else 1.10)
            if name.endswith('Lower Leg Stretch'):
                if p < RUN_STANCE:
                    return v - 0.12 * math.sin(math.pi * p / RUN_STANCE)
                return v - 0.32 * math.sin(math.pi * (p - RUN_STANCE) / (1 - RUN_STANCE)) ** 1.2
            return v
        if name.startswith(side + ' ') and any(name.endswith(m) for m in ('Arm Front-Back', 'Arm Down-Up', 'Arm Twist In-Out', 'Forearm Stretch')):
            # Each arm keeps the walk's opposition to its own leg: it replays the same warped timing.
            _, at = warped(side, phase)
            if name.endswith('Arm Front-Back'):
                return 0.30 + (walk(name, at) - 0.355) * 1.6
            if name.endswith('Arm Down-Up'):
                return walk(name, at) + 0.06
            if name.endswith('Forearm Stretch'):
                return 0.10 + 0.07 * math.cos(2 * math.pi * (at - ARM_BACK[side]))
            return walk(name, at)
    if name == 'RootT.y':
        return 0.925 - 0.025 * math.cos(4 * math.pi * (phase - (LEFT_CONTACT + RUN_STANCE / 2)))
    if name in ('RootT.x', 'RootT.z', 'RootQ.x', 'RootQ.y', 'RootQ.z'):
        return 0.0
    if name == 'RootQ.w':
        return 1.0
    lean = {'Spine Front-Back': -0.07, 'Chest Front-Back': -0.05, 'UpperChest Front-Back': -0.02}
    if name in lean:
        return walk(name, phase) + lean[name]
    if name in ('Spine Twist Left-Right', 'Chest Twist Left-Right', 'UpperChest Twist Left-Right'):
        return walk(name, phase) * 1.4
    return walk(name, phase)


# ----------------------------------------------------------------------------- jump key poses
BASE = dict(IDLE)
BASE.update({'RootT.x': 0.0, 'RootT.z': 0.0, 'RootQ.x': 0.0, 'RootQ.y': 0.0, 'RootQ.z': 0.0, 'RootQ.w': 1.0})


def pose(both=None, left=None, right=None, **rel):
    """Absolute muscle values (both sides, or per side) plus offsets from the idle (rel)."""
    out = {}
    for side in SIDES:
        for k, v in (both or {}).items():
            out[f'{side} {k}'] = v
    for k, v in (left or {}).items():
        out[f'Left {k}'] = v
    for k, v in (right or {}).items():
        out[f'Right {k}'] = v
    for k, v in rel.items():
        name = k.replace('_', ' ').replace('RootT y', 'RootT.y').replace('Front Back', 'Front-Back')
        out[name] = BASE[name] + v
    return out


STAND = {}
CROUCH = pose({'Upper Leg Front-Back': 0.10, 'Lower Leg Stretch': 0.48, 'Foot Up-Down': 0.12, 'Arm Front-Back': 0.55,
               'Arm Down-Up': -0.45, 'Forearm Stretch': 0.60}, Spine_Front_Back=-0.15, Chest_Front_Back=-0.08, RootT_y=-0.05)
PUSH = pose({'Arm Front-Back': -0.25, 'Arm Down-Up': -0.15, 'Forearm Stretch': 0.55, 'Toes Up-Down': -0.2},
            {'Upper Leg Front-Back': 0.62, 'Lower Leg Stretch': 0.95, 'Foot Up-Down': -0.45},
            {'Upper Leg Front-Back': 0.58, 'Lower Leg Stretch': 0.92, 'Foot Up-Down': -0.40},
            Spine_Front_Back=-0.05, Chest_Front_Back=-0.03)
TUCK = pose({'Arm Down-Up': -0.10, 'Forearm Stretch': 0.40},
            {'Upper Leg Front-Back': -0.20, 'Lower Leg Stretch': 0.22, 'Foot Up-Down': -0.20, 'Arm Front-Back': -0.20},
            {'Upper Leg Front-Back': -0.02, 'Lower Leg Stretch': 0.40, 'Foot Up-Down': -0.25, 'Arm Front-Back': -0.35},
            Spine_Front_Back=-0.10, Chest_Front_Back=-0.05)
APEX = pose({'Arm Down-Up': -0.08, 'Forearm Stretch': 0.45},
            {'Upper Leg Front-Back': -0.12, 'Lower Leg Stretch': 0.30, 'Foot Up-Down': -0.15, 'Arm Front-Back': -0.15},
            {'Upper Leg Front-Back': 0.08, 'Lower Leg Stretch': 0.45, 'Foot Up-Down': -0.20, 'Arm Front-Back': -0.28},
            Spine_Front_Back=-0.08, Chest_Front_Back=-0.04)
DESCEND = pose({'Foot Up-Down': -0.05, 'Arm Front-Back': -0.10, 'Arm Down-Up': -0.12, 'Forearm Stretch': 0.55},
               {'Upper Leg Front-Back': 0.25, 'Lower Leg Stretch': 0.62}, {'Upper Leg Front-Back': 0.35, 'Lower Leg Stretch': 0.70},
               Spine_Front_Back=-0.06)
REACH = pose({'Foot Up-Down': 0.05, 'Arm Front-Back': 0.0, 'Arm Down-Up': -0.22, 'Forearm Stretch': 0.60},
             {'Upper Leg Front-Back': 0.38, 'Lower Leg Stretch': 0.80}, {'Upper Leg Front-Back': 0.42, 'Lower Leg Stretch': 0.82},
             Spine_Front_Back=-0.05)
COMPRESS = pose({'Upper Leg Front-Back': 0.15, 'Lower Leg Stretch': 0.50, 'Foot Up-Down': 0.15, 'Arm Front-Back': 0.05,
                 'Arm Down-Up': -0.32, 'Forearm Stretch': 0.62}, Spine_Front_Back=-0.14, Chest_Front_Back=-0.06, RootT_y=-0.045)
JUMP_KEYS = [(0.00, STAND), (0.12, CROUCH), (0.17, PUSH), (0.30, TUCK), (0.42, APEX), (0.55, DESCEND),
             (0.68, REACH), (0.70, REACH), (0.78, COMPRESS), (1.00, STAND)]
JUMP_SEGMENTS = {'takeoff': 0.12, 'apex': 0.42, 'contact': 0.70}


def jump_keys(name):
    return [(t, p.get(name, BASE[name])) for t, p in JUMP_KEYS]


def monotone_slopes(points):
    """Fritsch-Carlson: no overshoot between authored poses (holds stay holds)."""
    n = len(points)
    d = [(points[i + 1][1] - points[i][1]) / (points[i + 1][0] - points[i][0]) for i in range(n - 1)]
    m = [d[0]] + [0.0 if d[i - 1] * d[i] <= 0 else (d[i - 1] + d[i]) / 2 for i in range(1, n - 1)] + [d[-1]]
    m[0] = m[-1] = 0.0
    for i in range(n - 1):
        if d[i] == 0:
            m[i] = m[i + 1] = 0.0
            continue
        a, b = m[i] / d[i], m[i + 1] / d[i]
        if a * a + b * b > 9:
            s = 3 / math.sqrt(a * a + b * b)
            m[i], m[i + 1] = s * a * d[i], s * b * d[i]
    return m


def curve_text(attribute, keys):
    out = '  - serializedVersion: 2\n    curve:\n      serializedVersion: 2\n      m_Curve:\n'
    for t, v, slope in keys:
        out += (f'      - serializedVersion: 3\n        time: {t:.8g}\n        value: {v:.8g}\n'
                f'        inSlope: {slope:.8g}\n        outSlope: {slope:.8g}\n        tangentMode: 0\n'
                '        weightedMode: 0\n        inWeight: 0.33333334\n        outWeight: 0.33333334\n')
    return out + (f'      m_PreInfinity: 2\n      m_PostInfinity: 2\n      m_RotationOrder: 4\n    attribute: {attribute}\n'
                  '    path: \n    classID: 95\n    script: {fileID: 0}\n    flags: 0\n')


def clip_text(name, length, loop, curves):
    head = TEMPLATE[:TEMPLATE.index('  m_FloatCurves:')].replace('m_Name: Standing W_Briefcase Idle', 'm_Name: ' + name)
    tail = TEMPLATE[TEMPLATE.index('  m_PPtrCurves:'):]
    tail = tail.replace('m_StopTime: 14.333334', f'm_StopTime: {length}').replace('m_LoopTime: 1', f'm_LoopTime: {int(loop)}')
    # The template's editor curves describe the idle; Unity rebuilds them from m_FloatCurves.
    tail = re.sub(r'  m_EditorCurves:\n(?:  [- ].*\n)*?(?=  m_EulerEditorCurves:)', '  m_EditorCurves: []\n', tail)
    return head + '  m_FloatCurves:\n' + ''.join(curve_text(a, k) for a, k in curves) + tail


def build():
    steps = 18
    run_curves = []
    for a in ATTRIBUTES:
        keys = []
        for i in range(steps + 1):
            ph = i / steps
            h = 1e-4
            slope = (run(a, ph + h) - run(a, ph - h)) / (2 * h) / RUN_LEN  # periodic central tangent, per second
            keys.append((ph * RUN_LEN, run(a, ph), slope))
        run_curves.append((a, keys))
    jump_curves = []
    for a in ATTRIBUTES:
        pts = jump_keys(a)
        slopes = monotone_slopes(pts)
        jump_curves.append((a, [(t, v, s) for (t, v), s in zip(pts, slopes)]))
    return {'Elijah_Sprint': clip_text('Elijah_Sprint', RUN_LEN, True, run_curves),
            'Elijah_Jump': clip_text('Elijah_Jump', 1.0, False, jump_curves)}


if __name__ == '__main__':
    clips = build()
    print('measured: contact', {k: round(v, 3) for k, v in CONTACT.items()}, 'toe-off', {k: round(v, 3) for k, v in TOE_OFF.items()},
          'walk stance', {k: round(v, 3) for k, v in WALK_STANCE.items()})
    stale = []
    for name, text in clips.items():
        path = ROOT / f'Assets/Resources/Player/{name}.anim'
        if '--check' in sys.argv:
            if path.read_text() != text:
                stale.append(name)
        else:
            with open(path, 'w', newline='\n') as f:
                f.write(text)
            print(name, len(ATTRIBUTES), 'Humanoid curves written')
    if stale:
        sys.exit('stale clip(s): ' + ', '.join(stale))
