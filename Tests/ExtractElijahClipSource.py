"""Milestone T03: export the reference Humanoid data the traversal clips are authored from.

Read-only. Decodes Unity's imported (cached) clips with the T02 type-tree reader and writes
Tests/ElijahClipSource.json (UTF-8/LF):
  - walk: Elijah's own walk ('walking_idle', Elijah_Walking.fbx / Elijah_Exploration_Smooth.fbx),
          all 130 Humanoid float curves as Hermite keys;
  - idle: the shared standing idle ('Standing Idle', npc_jhun_static@Standing Idle.fbx), frame 0.
The cache is located by content (rig + clip name + mesh names), never by a guessed path.
Run from the repository root once the Editor has imported the models:
  python3 -B Tests/ExtractElijahClipSource.py
"""
from pathlib import Path
import json
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'Tests/T02PilotRegression'))
import read_imported_model as R  # noqa: E402


def candidates():
    for path in sorted((ROOT / 'Library/Artifacts').glob('*/*')):
        try:
            data = path.read_bytes()
        except OSError:
            continue
        if b'mixamorig:LeftUpLeg' in data:
            yield path, data


def clip(path, name):
    for cid, value in R.load(path, classes=(74,)).values():
        if cid == 74 and value['m_Name'] == name:
            return {c['attribute']: [[k['time'], k['value'], k['inSlope'], k['outSlope']] for k in c['curve']['@299']]
                    for c in value['m_FloatCurves']}
    return None


def human_scale(path):
    for cid, value in R.load(path, classes=(90,)).values():
        if cid == 90:
            return value['m_Avatar']['m_Human']['data']['m_Scale']
    return None


walk = idle = None
for path, data in candidates():
    if walk is None and b'walking_idle' in data and b'Elijah_Walking.fbx' in data:
        walk = (path, clip(path, 'walking_idle'), human_scale(path))
    if idle is None and b'Standing Idle' in data and b'envelope-chest' in data:
        idle = (path, clip(path, 'Standing Idle'), human_scale(path))
if not walk or not walk[1] or not idle or not idle[1]:
    sys.exit('Imported Elijah walk or shared idle not found in Library/Artifacts (let Unity import first).')

out = {
    'provenance': {
        'walk': {'asset': 'Assets/3D Models/npc_elijah/Elijah_Walking.fbx', 'guid': 'c8feebe21aab3eb41b121dc3ef4321de',
                 'clip': 'walking_idle', 'cache': str(walk[0].relative_to(ROOT)), 'humanScale': walk[2]},
        'idle': {'asset': 'Assets/3D Models/npc_jhun_static/npc_jhun_static@Standing Idle.fbx', 'guid': '627d8de63b4cc4c49bcf990b12b41b38',
                 'clip': 'Standing Idle', 'cache': str(idle[0].relative_to(ROOT)), 'humanScale': idle[2]},
    },
    'walk': walk[1],
    'idle': {name: keys[0][1] for name, keys in idle[1].items()},
}
with open(ROOT / 'Tests/ElijahClipSource.json', 'w', newline='\n', encoding='utf-8') as f:
    json.dump(out, f, indent=0, sort_keys=True)
    f.write('\n')
print('walk curves', len(out['walk']), 'idle values', len(out['idle']), 'from', walk[0].name, idle[0].name)
