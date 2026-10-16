"""Run with python Tests/VerifyOfficeNpcMarkers.py; no Unity editor required."""
from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[1]


def guid(script):
    return re.search(r"^guid: (\w+)", (ROOT / (script + ".meta")).read_text(), re.M)[1]


def blocks(text):
    return re.split(r"(?=^--- !u!)", text, flags=re.M)


npc_scripts = [f"Assets/Scripts/Interactables/{name}Interactable.cs"
               for name in ("Client", "Auditor", "Receptionist", "Colleague")]
npc_guids = {guid(script) for script in npc_scripts}
marker_guid = guid("Assets/Scripts/FSM/NPC/NpcStatusMarker.cs")
legacy_guids = {guid("Assets/Scripts/FSM/NPC/NpcVisualIndicator.cs"),
                guid("Assets/Scripts/Interactables/HighlightEffect.cs")}

for script in npc_scripts:
    source = (ROOT / script).read_text()
    assert "HighlightEffect" not in source, script
    assert "RequireComponent(typeof(NpcStatusMarker))" in source, script

checked = 0
for scene in sorted((ROOT / "Assets/Scenes").glob("Office_*.unity")):
    text = scene.read_text()
    documents = blocks(text)
    ids = re.findall(r"^--- !u!\d+ &(\d+)", text, re.M)
    assert len(ids) == len(set(ids)), f"{scene.name}: duplicate object IDs"
    by_id = {re.match(r"--- !u!\d+ &(\d+)", b)[1]: b
             for b in documents if b.startswith("--- !u!")}
    npcs = [b for b in documents if any(f"guid: {g}," in b for g in npc_guids)]
    assert npcs, f"{scene.name}: no office NPCs checked"
    for npc in npcs:
        owner = re.search(r"m_GameObject: \{fileID: (\d+)\}", npc)[1]
        source_id = re.match(r"--- !u!114 &(\d+)", npc)[1]
        component_ids = re.findall(r"component: \{fileID: (\d+)\}", by_id[owner])
        assert all(cid in by_id for cid in component_ids), f"{scene.name}: missing component"
        components = [by_id[cid] for cid in component_ids]
        assert not any(f"guid: {g}," in b for g in legacy_guids for b in components), owner
        markers = [b for b in components if f"guid: {marker_guid}," in b]
        assert len(markers) == 1, f"{scene.name}: NPC {owner} needs exactly one marker"
        assert f"m_GameObject: {{fileID: {owner}}}" in markers[0], owner
        assert f"availabilitySource: {{fileID: {source_id}}}" in markers[0], owner
        assert "markerMaterial: {fileID: 2100000," in markers[0], owner
        checked += 1

assert checked, "No office scenes checked"
print(f"PASS: {checked} office NPCs use overhead markers with valid component references and no legacy visuals.")
