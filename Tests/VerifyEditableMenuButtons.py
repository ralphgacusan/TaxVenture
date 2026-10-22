"""Internal saved-scene contracts; no Unity or player-save access."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
scene = (ROOT / "Assets/Scenes/3D Menu.unity").read_text(encoding="utf-8")
blocks = re.split(r"(?=^--- !u!)", scene, flags=re.M)
ids = [re.match(r"--- !u!\d+ &(-?\d+)", b).group(1) for b in blocks if b.startswith("--- !u!")]
assert len(ids) == len(set(ids)), "Duplicate scene object/component IDs"


def block(fid):
    return next(b for b in blocks if re.match(r"--- !u!\d+ &" + str(fid) + r"(?: stripped)?\n", b))


buttons = block(166793947)
for base, name in [(3100000000, "new_game_button"), (3100000100, "continue_button")]:
    prefab = block(base + 1)
    assert "value: " + name in prefab
    assert "m_TransformParent: {fileID: 166793947}" in prefab
    assert "m_SourcePrefab: {fileID: 100100000, guid: ca47c77aca5cb344390f8c22ff6e3348, type: 3}" in prefab
    assert "m_AddedGameObjects: []" in prefab
    assert "m_IsActive" not in prefab  # Inherits Settings' active Edit Mode state.
    assert "- {fileID: " + str(base + 3) + "}" in buttons
    assert "m_GameObject: {fileID: " + str(base + 2) + "}" in block(base + 4)
    assert "m_Size: {x: 3.26, y: 1, z: 1}" in block(base + 4)
    assert "m_AddedComponents:" in prefab and "UnityEngine.UI.Button" not in block(base + 4)
    assert len(re.findall("addedObject:", prefab)) == 1  # Collider only; no inherited Settings callback.

panel = block(3100000200)
assert "playNote: {fileID: 1569992486}" in panel
assert "newGameNote: {fileID: 3100000002}" in panel
assert "continueNote: {fileID: 3100000102}" in panel
assert "m_GameObject: {fileID: 166793946}" in panel
assert "- component: {fileID: 3100000200}" in block(166793946)
source = (ROOT / "Assets/Scripts/UI/Menu/MainMenuCampaignPanel.cs").read_text(encoding="utf-8")
assert 'Copy(settings, MenuEntry.NewGameButtonName' not in source
assert 'Copy(settings, MenuEntry.ContinueButtonName' not in source
assert '[ExecuteAlways]' in source
assert 'if (!Application.isPlaying) return;' in source
assert 'label.sharedMesh != original.sharedMesh' in source  # Preserve user custom mesh edits.
editor = (ROOT / "Assets/Editor/MainMenuButtonAuthoring.cs").read_text(encoding="utf-8")
assert 'if (newGame == null)' in editor and 'if (resume == null)' in editor
assert 'SaveScene(' not in editor and 'OpenScene(' not in editor
print("PASS: saved Settings duplicates, editable colliders, panel bindings, Edit Mode labels, preserved layout and merge-only editor authoring")

play_preview = block(1010414532)
assert "propertyPath: m_IsActive\n      value: 0" in play_preview
assert "model.ShowPlay" in source  # The runtime model still selects the fresh Play route.
print("PASS: returning-row editor preview excludes overlapping Play; fresh runtime visibility remains model-owned")

assert 'Instantiate(' not in source and 'Authored Play (inactive)' not in source
assert '[SerializeField] private GameObject playNote;' in source
main = (ROOT / "Assets/Scripts/MainMenu.cs").read_text(encoding="utf-8")
for action in ('() => MenuEntry.Play(cameraManager)', 'OpenNewGameConfirmation',
               '() => MenuEntry.Continue(cameraManager)', 'OpenTraining', 'OpenSettings'):
    assert 'Press(note, ' + action + ')' in main
assert 'actionPending || MenuEntry.IsStoryLaunching' in main
assert 'press.Completed?.Invoke();' in source and 'presses.Count != 0) return false;' in source
assert 'Input.GetMouseButtonDown(0)' in main and 'TouchPhase.Began' in main
print("PASS: authored Play retained; five shared press callbacks, input lock, touch/mouse and unchanged completion gate")
