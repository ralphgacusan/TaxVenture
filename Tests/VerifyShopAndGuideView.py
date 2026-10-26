"""Source/scene binding regressions; visual behavior still needs Unity Play Mode."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
turn = (root / 'Assets/Scripts/Player/ViewTurn.cs').read_text()
counter = (root / 'Assets/Scripts/Interactables/SupplyCounter.cs').read_text()

# Level 1 has two look controllers: only the one on the rendered camera
# owns its rotation. Updating an arbitrary controller makes the view snap back.
assert 'main.GetComponent<FirstPersonCameraController>()' in turn, \
    'Guided turns must retain angles on the controller attached to the rendered camera'
assert 'FindAnyObjectByType<FirstPersonCameraController>' not in turn
assert 'view.SetLook(yaw, pitch);' in turn
assert 'Material wood = DecorModels.Solid(' in counter, \
    'Signposts must use plain wood, not the counter texture atlas'
assert 'SurfaceMaterial(' not in counter
assert 'Texture2D.whiteTexture' in (root / 'Assets/Scripts/Interactables/DecorModels.cs').read_text()
print('PASS: guide camera ownership and plain wood signpost bindings (static checks).')
