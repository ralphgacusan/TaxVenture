"""Run with python Tests/VerifyInteractionOutlines.py; no Unity editor required."""
from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def guid(path):
    return re.search(r"^guid: (\w+)", read(path + ".meta"), re.M)[1]


def unity_docs(text):
    return re.split(r"(?=^--- !u!\d+ &)", text, flags=re.M)


feature_script = "Assets/Scripts/Interactables/InteractionOutlineFeature.cs"
highlight_script = "Assets/Scripts/Interactables/HighlightEffect.cs"
feature_guid = guid(feature_script)
shader_guid = guid("Assets/InteractionOutline.shader")
shader = read("Assets/InteractionOutline.shader")
feature = read(feature_script)
highlight = read(highlight_script)

renderer_assets = [
    "Assets/Settings/PC_Renderer.asset",
    "Assets/Settings/Mobile_Renderer.asset",
    "Assets/EmaceArt/Roadside Tales Bridges/Post Processing_profile/Settings/"
    "New Universal Render Pipeline Asset_Renderer.asset",
]
for path in renderer_assets:
    text = read(path)
    docs = unity_docs(text)
    feature_doc = next((doc for doc in docs if "m_Name: InteractionOutline\n" in doc), None)
    assert feature_doc, f"{path}: missing outline feature subasset"
    assert f"guid: {feature_guid}, type: 3" in feature_doc, f"{path}: wrong feature script"
    assert "m_Active: 1" in feature_doc, f"{path}: outline feature is inactive"
    assert f"guid: {shader_guid}, type: 3" in feature_doc, f"{path}: wrong shader reference"
    assert "outlineColor: {r: 0.68, g: 0.7, b: 0.72, a: 0.58}" in feature_doc, (
        f"{path}: wrong outline color or alpha"
    )
    assert "widthPixels: 5" in feature_doc, f"{path}: wrong outline width"
    feature_id = re.match(r"--- !u!\d+ &(\d+)", feature_doc)[1]
    renderer_doc = next(doc for doc in docs if "m_RendererFeatures:" in doc)
    feature_list = renderer_doc.split("m_RendererFeatures:", 1)[1].split("m_RendererFeatureMap:", 1)[0]
    assert f"fileID: {feature_id}" in feature_list, f"{path}: feature is not listed in m_RendererFeatures"

assert re.search(r'Shader\s+"Hidden/TaxVenture/InteractionOutline"', shader)
assert shader.count("\n        Pass\n") == 2, "outline shader should contain mask and composite passes"
assert re.search(r"private Color outlineColor = new Color\(0\.68f, 0\.70f, 0\.72f, 0\.58f\)", feature)
assert re.search(r"private float widthPixels = 5f", feature)

material_write = re.compile(r"\.(?:shared)?materials?\s*(?:\[[^\]]+\]\s*)?=(?!=)")
assert not material_write.search(highlight), "HighlightEffect assigns a renderer material"
assert "DrawRenderer" in feature and "HighlightEffect.ActiveEffects" in feature

# Persistent hints must survive legacy hover exit and work without mouse hover on touch screens.
assert "focused || requested || affordance" in highlight
npc_marker = read("Assets/Scripts/FSM/NPC/NpcStatusMarker.cs")
assert "bodyOutline.SetAffordance(targetShape != NpcMarkerShape.None)" in npc_marker
assert "bodyOutline.SetAffordance(false)" in npc_marker
assert "bodyOutline.RefreshTargets()" in npc_marker
assert "RefreshVisualTargets()" in read("Assets/Controller/NPC/ConferenceRoomClient.cs")
desk_hint = read("Assets/Scripts/Interactables/DeskItemHighlight.cs")
assert "SetAffordance(true)" in desk_hint and "SetAffordance(false)" in desk_hint
assert "private void OnEnable()" in desk_hint and "if (showRequested)" in desk_hint
assert "showRequested = true" in desk_hint and "showRequested = false" in desk_hint
for name in ("CaseFolder", "TaxCodeBook", "Stamps", "StampSet", "Computer"):
    source = read(f"Assets/Scripts/Interactables/{name}Interactable.cs")
    unfocus = source.split("public void OnUnfocus()", 1)[1].split("public void OnInteract()", 1)[0]
    assert "Unhighlight()" in unfocus and "CameraMode.Workstation" not in unfocus, (
        f"{name}: hover exit must release its request even while desk hints are active"
    )

# The visible closed folder/book must be included in the initial workstation hints.
office_docs = unity_docs(read("Assets/Scenes/Office_Level1.unity"))
camera_doc = next(doc for doc in office_docs if "  deskItemHighlights:" in doc)
desk_guid = guid("Assets/Scripts/Interactables/DeskItemHighlight.cs")
for script in ("Assets/Scripts/CaseFolder/CaseFolder3DDrag.cs",
               "Assets/Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs"):
    source_doc = next(doc for doc in office_docs if f"guid: {guid(script)}," in doc)
    owner_id = re.search(r"m_GameObject: \{fileID: (\d+)\}", source_doc)[1]
    hint_doc = next((doc for doc in office_docs if f"guid: {desk_guid}," in doc and
                     f"m_GameObject: {{fileID: {owner_id}}}" in doc), None)
    assert hint_doc, f"{script}: visible desk item has no initial outline hint"
    hint_id = re.match(r"--- !u!\d+ &(\d+)", hint_doc)[1]
    hint_array = re.search(r"(?m)^  deskItemHighlights:\n(?:  - .*\n)*", camera_doc)[0]
    assert f"fileID: {hint_id}" in hint_array, f"{script}: initial desk hint is not wired"

routers = [
    "Assets/Scripts/Player/Interactor.cs",
    "Assets/Scripts/Player/WorkstationInteractor.cs",
]
for path in routers:
    source = read(path)
    assert "HighlightEffect.For(" in source, f"{path}: focused target is not registered"
    assert "SetFocused(true)" in source, f"{path}: focus entry does not enable outline"
    assert "SetFocused(false)" in source, f"{path}: focus exit does not clear outline"

pointer_sources = [
    "Assets/Scripts/SideQuest1/TaxpayerNpcInteractable.cs",
    "Assets/Scripts/SideQuest1/TutorialNpcController.cs",
    "Assets/Scripts/SideQuest1/TaxpayerProfileWorldPickup.cs",
    "Assets/Scripts/SideQuest1/ProfileDragCard.cs",
    "Assets/Scripts/SideQuest1/CollectedProfilesWorldPanel.cs",
]
drag_sources = [
    "Assets/Scripts/UI/Stamp3DDrag.cs",
    "Assets/Scripts/CaseFolder/FloatingModel3D.cs",
    "Assets/Scripts/CaseFolder/CaseFolder3DDrag.cs",
    "Assets/Scripts/TaxCodeBook/TaxCodeBookFloatingModel3D.cs",
    "Assets/Scripts/TaxCodeBook/TaxCodeBook3DDrag.cs",
]
for path in pointer_sources + drag_sources:
    source = read(path)
    assert "HighlightEffect.For(" in source, f"{path}: target is not registered"
    if path in pointer_sources:
        assert ".SetFocused(" in source, f"{path}: no focus state is applied"
        assert "SetFocused(false)" in source, f"{path}: focus exit does not clear outline"
    else:
        assert "SetOutlineFocused(false)" in source, f"{path}: drag exit does not clear outline"
        assert "highlightEffect.SetFocused(focused)" in source, f"{path}: outline state is not forwarded"
        calls = re.findall(r"\bSetOutlineFocused\(\s*([\s\S]*?)\s*\);", source)
        assert any(argument.strip() != "false" for argument in calls), (
            f"{path}: no interaction state turns the outline on"
        )

print(
    f"PASS: {len(renderer_assets)} renderer assets, shared outline behavior, "
    f"persistent desk/NPC hints, {len(routers)} focus routers, {len(pointer_sources)} pointer sources, "
    f"and {len(drag_sources)} 3D drag sources checked."
)
