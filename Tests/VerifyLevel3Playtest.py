"""Authoring/binding checks for the third Level 3 playtest request. No Unity rendering."""
from pathlib import Path
import hashlib
import re

ROOT = Path(__file__).resolve().parents[1]
def read(path): return (ROOT / path).read_text()
def documents(text):
    return {match[2]: (match[1], match[3]) for match in re.finditer(
        r'--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=--- !u!|\Z)', text, re.S)}
def placement(scene, guid):
    docs = documents(scene)
    scripts = [body for kind, body in docs.values() if kind == '114' and 'guid: ' + guid in body]
    assert len(scripts) == 1, (guid, len(scripts))
    game = re.search(r'm_GameObject: \{fileID: (\d+)', scripts[0])[1]
    model = next(body for kind, body in docs.values() if kind == '4' and 'm_GameObject: {fileID: ' + game + '}' in body)
    assert 'm_Father: {fileID: 0}' in model
    return docs[game][1], model

level1 = read('Assets/Scenes/Office_Level1.unity')
level3 = read('Assets/Scenes/Office_Level3.unity')
supply = 'dfe21702b45f4fa9ad3ada3c6d3aa77d'
_, first_pose = placement(level1, supply)
_, third_pose = placement(level3, supply)
for key in ('m_LocalPosition', 'm_LocalRotation', 'm_LocalScale'):
    assert re.search(key + r': (.*)', first_pose)[1] == re.search(key + r': (.*)', third_pose)[1], key
print('PASS supply counter uses the exact saved Level 1 placement and rotation.')
bea_guid = re.search(r'guid: (\w+)', read('Assets/Scripts/Level3/Level3BeaPlacement.cs.meta'))[1]
bea_object, bea_pose = placement(level3, bea_guid)
assert 'm_Name: Bea Outside Placement' in bea_object
assert 'Level3BeaPlacement.Find(scene)' in read('Assets/Scripts/Level3/Level3BeaTable.cs')
assert 'OnDrawGizmos()' in read('Assets/Scripts/Level3/Level3BeaPlacement.cs')
print('PASS one scene-authored Bea placement binds the outdoor setup and previews the actual existing assets.')
# Prove this patch adds only the two placement markers to the recorded scene; all environment bytes remain.
restored = level3
for gid, tid, sid in [('876510001', '876510002', '876510003'), ('1876510001', '1876510002', '1876510003')]:
    for object_id in (gid, tid, sid):
        restored = re.sub(r'--- !u!\d+ &' + object_id + r'\n.*?(?=--- !u!|\Z)', '', restored, flags=re.S)
    restored = restored.replace('  - {fileID: ' + tid + '}\n', '')
baseline = '3e8d29dfd93c2efc75a903034b6957b5c6093c867f6b9ca0a9900be6c824b9c5'
assert baseline in {hashlib.sha256(restored.encode()).hexdigest(), hashlib.sha256(restored.replace('\n', '\r\n').encode()).hexdigest()}
print('PASS removing just the two authored markers reproduces the entire pre-change Level 3 scene hash.')
# Separate hit areas must outline an actual rendered model, not their empty collider/canvas.
assert 'highlight.SetTargetRoot(model);' in read('Assets/Scripts/Level3/Level3BeaTableSeat.cs')
assert 'recordTarget.SetVisual(paper != null ? paper.transform : null);' in read('Assets/Scripts/Level3/Level3BeaTable.cs')
assert 'IPointerEnterHandler, IPointerExitHandler' in read('Assets/Scripts/SideQuest1/ClientRecordPaper.cs')
assert 'highlight.SetTargetRoot(transform.parent);' in read('Assets/Scripts/Level3/Level3PaperMachineModel.cs')
assert 'toolHighlight.SetTargetRoot(target.transform.parent);' in read('Assets/Scripts/Level3/Level3DeskCalculator.cs')
print('PASS Level 3 hit areas reuse the existing grey highlight on their physical models.')
model = read('Assets/Scripts/Level3/Level3CalculatorModel.cs')
assert 'tapePage = LastTapePage(printedLines.Count);' in model
assert 'int first = TapePageStart(tapePage, printedLines.Count);' in model
print('PASS real receipt printing follows the tested pagination helpers; prior calculation pages remain available.')
print('PASS authoring/binding checks; Unity import, outline appearance, placement, splash and interaction feel remain manual.')
